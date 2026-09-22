using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 고스트 전멸 클리어 보상 상자. 평소엔 비활성 상태로 숨어 있다가
/// MiniGameFlowManager가 SetVisible(true)로 맵 중앙에 등장시킨다.
/// 범위 안에서 상호작용(F)하면 아이템 획득 여부를 IPlayerItemSave에 위임해 판단하고,
/// 신규 획득이면 저장까지 요청한 뒤 RewardPopupUI로 결과만 표시한다.
/// </summary>
public class RewardBoxTrigger : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string itemId = "seed_tetris_pacman_reward";
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private RewardPopupUI rewardPopupUI;
    [SerializeField] private PlayerController playerController;

    private InputAction interactAction;
    private bool isPlayerInRange;
    private bool isOpened;

    private void Awake()
    {
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
        if (!isPlayerInRange || isOpened)
        {
            return;
        }

        if (interactAction.WasPressedThisFrame())
        {
            Open();
        }
    }

    private void Open()
    {
        isOpened = true;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }

        // 패널이 뜨는 동안 마우스로 버튼을 누를 수 있어야 하므로, 사망 팝업과 동일하게 조작을 잠그고 커서를 보이게 한다.
        if (playerController != null)
        {
            playerController.SetControlsLocked(true);
        }

        var itemSave = PlayerItemSaveLocator.Get();

        if (itemSave.HasItem(itemId))
        {
            rewardPopupUI.ShowAlreadyCleared();
            return;
        }

        itemSave.SetItemGain(itemId);
        rewardPopupUI.ShowItemGained();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isOpened)
        {
            return;
        }

        isPlayerInRange = true;

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        isPlayerInRange = false;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }
}
