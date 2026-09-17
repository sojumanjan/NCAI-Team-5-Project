using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 팩맨 미로 입구 자리에 위치한 벽. 평소에는 메쉬 없이 콜라이더(막는 용도, non-trigger)만 남아
/// (문 너머로 미로 안쪽이 보이되) 통과는 막는다. 감지는 별도 자식 트리거(interactTrigger)가 담당한다.
/// 플레이어가 앞에서 상호작용(F)하면 미로 중앙 쪽으로 순간이동시키고,
/// 이 벽은 메쉬를 다시 표시해 완전히 막는다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PacmanEntranceWall : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Transform teleportTarget;
    [SerializeField] private Collider interactTrigger;
    [SerializeField] private string playerTag = "Player";

    private MeshRenderer meshRenderer;
    private InputAction interactAction;
    private bool playerInRange;
    private bool consumed;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        var playerMap = inputActions.FindActionMap("Player");
        interactAction = playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
    }

    private void Update()
    {
        if (!playerInRange || consumed)
        {
            return;
        }

        if (interactAction.WasPressedThisFrame())
        {
            Enter();
        }
    }

    private void Enter()
    {
        consumed = true;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }

        // 벽을 다시 막아 뒤로 돌아갈 수 없게 한다.
        meshRenderer.enabled = true;

        // 텔레포트는 화면이 완전히 어두워진 뒤(FadeOut 완료 후) 처리되도록 MiniGameFlowManager에 위임한다.
        MiniGameFlowManager.Instance.StartPacman(teleportTarget);
    }

    /// <summary>interactTrigger(자식 오브젝트)가 감지를 대신 넘겨준다.</summary>
    public void NotifyPlayerEnter()
    {
        if (consumed)
        {
            return;
        }

        playerInRange = true;

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }

    public void NotifyPlayerExit()
    {
        playerInRange = false;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }
}
