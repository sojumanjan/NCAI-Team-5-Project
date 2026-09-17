using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Jump")]
    [SerializeField] private bool jumpEnabled = true;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("References")]
    [Tooltip("실제 회전을 적용할 대상. 카메라 흔들림 연출을 쓰는 경우 카메라의 부모(피벗)를 지정한다.")]
    [SerializeField] private Transform cameraPivot;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction interactAction;

    private Vector3 verticalVelocity;
    private float pitch;
    private bool controlsLocked;

    public bool ControlsLocked => controlsLocked;

    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Interact(F)는 RewardBoxTrigger/PacmanEntranceWall 등 여러 컴포넌트가 같은 액션을 구독하므로,
        // 여기서 액션 자체를 잠그면 각 컴포넌트를 따로 고칠 필요 없이 일괄 차단된다.
        if (locked)
        {
            interactAction.Disable();
        }
        else
        {
            interactAction.Enable();
        }
    }

    /// <summary>
    /// 점프 가능 여부를 런타임에 바꾼다 (예: 팩맨에서는 점프/등반이 없어야 함).
    /// 활성화된 상태에서는 즉시 입력 액션도 함께 Enable/Disable한다.
    /// </summary>
    public void SetJumpEnabled(bool enabled)
    {
        jumpEnabled = enabled;

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (jumpEnabled)
        {
            jumpAction.Enable();
        }
        else
        {
            jumpAction.Disable();
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");
        jumpAction = playerMap.FindAction("Jump");
        interactAction = playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();

        if (jumpEnabled)
        {
            jumpAction.Enable();
        }

        // controlsLocked 상태(예: 사망 팝업)를 그대로 유지한 채 재활성화되어야 하므로,
        // 여기서 커서를 무조건 잠그지 않고 현재 잠금 상태를 다시 적용한다.
        SetControlsLocked(controlsLocked);
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        interactAction.Disable();
    }

    private void Update()
    {
        if (controlsLocked)
        {
            return;
        }

        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        Vector2 lookDelta = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        transform.Rotate(Vector3.up * lookDelta.x);

        pitch = Mathf.Clamp(pitch - lookDelta.y, minPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded)
        {
            verticalVelocity.y = -1f;

            if (jumpEnabled && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * moveSpeed + verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
