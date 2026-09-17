using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC(Pause 액션)로 여닫는 일시정지 패널. 테트리스/팩맨 공통으로 사용한다.
/// 현재는 "계속하기"만 실제로 동작하고, "게임 설명"/"메인씬으로"는 버튼 자리만 마련해둔 상태다.
/// </summary>
public class PauseUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject root;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CountdownUI countdownUI;

    private InputAction pauseAction;
    private bool isPaused;

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        pauseAction = playerMap.FindAction("Pause");
    }

    private void OnEnable()
    {
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.Disable();
    }

    private void Update()
    {
        if (!pauseAction.WasPressedThisFrame())
        {
            return;
        }

        if (isPaused)
        {
            OnClickResume();
        }
        else
        {
            Pause();
        }
    }

    private void Pause()
    {
        // 이미 사망 팝업 등 다른 패널이 떠 있는 상태(조작이 잠긴 상태)에서는 일시정지를 겹쳐 열지 않는다.
        if (MiniGameFlowManager.Instance.CurrentState == MiniGameState.MainUI)
        {
            return;
        }

        // 카운트다운(3,2,1) 중에는 아직 실제 게임 로직이 시작되지 않아 멈출 대상이 없고,
        // 카운트다운 자체는 코루틴이라 일시정지해도 계속 흘러가 버리므로 아예 무시한다.
        if (countdownUI.IsPlaying)
        {
            return;
        }

        isPaused = true;
        root.SetActive(true);

        tetrisGameManager.SetTetrisGameplayPaused(true);
        MiniGameFlowManager.Instance.SetGhostsPaused(true);
        playerController.SetControlsLocked(true);
    }

    public void OnClickResume()
    {
        isPaused = false;
        root.SetActive(false);

        playerController.SetControlsLocked(false);
        MiniGameFlowManager.Instance.SetGhostsPaused(false);

        // 테트리스 낙하는 팩맨 상태일 때는 원래도 멈춰 있어야 하므로, 현재 상태를 다시 물어 정확히 되돌린다.
        tetrisGameManager.SetTetrisGameplayPaused(MiniGameFlowManager.Instance.CurrentState != MiniGameState.Tetris);
    }
}
