using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ClimbController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform handLeft;
    [SerializeField] private Transform handRight;

    [Header("Detection")]
    [SerializeField] private float forwardCheckDistance = 0.7f;
    [SerializeField] private float ledgeCheckHeight = 3f;
    [SerializeField] private float minLedgeHeight = 0.5f;
    [SerializeField] private float maxLedgeHeight = 3f;
    [SerializeField] private LayerMask climbableMask = ~0;

    [Header("Climb Motion")]
    [SerializeField] private float reachDuration = 0.2f;
    [SerializeField] private float pullUpDuration = 0.3f;
    [SerializeField] private float forwardSettleDistance = 0.6f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;

    private Vector3 handLeftRestLocalPos;
    private Vector3 handRightRestLocalPos;

    private bool isClimbing;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");
        jumpAction = playerMap.FindAction("Jump");

        handLeftRestLocalPos = handLeft.localPosition;
        handRightRestLocalPos = handRight.localPosition;
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        // 1차 Jump 입력은 PlayerController가 처리하는 일반 점프.
        // 공중에 뜬 상태에서의 2차 Jump 입력만 등반 트리거로 취급한다.
        if (isClimbing || controller.isGrounded)
        {
            return;
        }

        if (TryGetLedgeTarget(out Vector3 ledgeStandPosition, out Vector3 ledgeEdgePosition))
        {
            StartClimb(ledgeStandPosition, ledgeEdgePosition);
        }
    }

    private bool TryGetLedgeTarget(out Vector3 standPosition, out Vector3 edgePosition)
    {
        standPosition = Vector3.zero;
        edgePosition = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 feetPosition = transform.position - new Vector3(0f, controller.height * 0.5f, 0f);

        Vector3 lowOrigin = feetPosition + Vector3.up * 0.1f;
        bool blockedLow = Physics.Raycast(lowOrigin, forward, forwardCheckDistance, climbableMask);
        if (!blockedLow)
        {
            return false;
        }

        Vector3 highOrigin = feetPosition + Vector3.up * ledgeCheckHeight;
        bool blockedHigh = Physics.Raycast(highOrigin, forward, forwardCheckDistance, climbableMask);
        if (blockedHigh)
        {
            return false;
        }

        Vector3 downOrigin = highOrigin + forward * forwardCheckDistance;
        if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit hit, ledgeCheckHeight, climbableMask))
        {
            return false;
        }

        float ledgeHeight = hit.point.y - feetPosition.y;
        if (ledgeHeight < minLedgeHeight || ledgeHeight > maxLedgeHeight)
        {
            return false;
        }

        standPosition = hit.point + forward * forwardSettleDistance + Vector3.up * (controller.height * 0.5f + 0.05f);
        edgePosition = hit.point;
        return true;
    }

    private void StartClimb(Vector3 standPosition, Vector3 edgePosition)
    {
        isClimbing = true;

        // CharacterController는 켜둔 채로 유지해 이동 중에도 벽 충돌 감지를 받는다.
        playerController.enabled = false;
        moveAction.Disable();
        lookAction.Disable();

        Vector3 moveStart = transform.position;
        float moveProgress = 0f;

        Sequence climbSequence = DOTween.Sequence();
        climbSequence.Append(handLeft.DOLocalMove(handLeft.parent.InverseTransformPoint(edgePosition) + Vector3.left * 0.15f, reachDuration).SetEase(Ease.OutQuad));
        climbSequence.Join(handRight.DOLocalMove(handRight.parent.InverseTransformPoint(edgePosition) + Vector3.right * 0.15f, reachDuration).SetEase(Ease.OutQuad));
        climbSequence.Append(DOTween.To(() => moveProgress, t => moveProgress = t, 1f, pullUpDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                Vector3 targetPosition = Vector3.Lerp(moveStart, standPosition, moveProgress);
                Vector3 delta = targetPosition - transform.position;
                controller.Move(delta);
            }));
        climbSequence.Join(handLeft.DOLocalMove(handLeftRestLocalPos, pullUpDuration));
        climbSequence.Join(handRight.DOLocalMove(handRightRestLocalPos, pullUpDuration));
        climbSequence.OnComplete(EndClimb);
    }

    private void EndClimb()
    {
        playerController.enabled = true;
        moveAction.Enable();
        lookAction.Enable();

        isClimbing = false;
    }
}
