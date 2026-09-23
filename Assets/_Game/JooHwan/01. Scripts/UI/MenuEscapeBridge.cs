using UnityEngine;

/// <summary>
/// Taegeon.MenuEscapeToggle(공용 Menu Canvas 프리팹, 수정 불가)이 Pause Root/MenuRoot를 켜고 끄는
/// 순간을 매 프레임 감시해서, 기존 PauseUI가 하던 개별 게임 로직 제어(테트리스 낙하 정지,
/// 고스트 정지, 플레이어 조작 잠금)를 대신 따라가게 한다.
/// MenuEscapeToggle은 Time.timeScale=0으로 전역 정지도 함께 수행하므로, 이 브릿지는 그것만으로는
/// 멈추지 않는 개별 시스템(Time.timeScale과 무관하게 동작하는 로직)을 보정하는 역할이다.
/// </summary>
public class MenuEscapeBridge : MonoBehaviour
{
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CountdownUI countdownUI;
    [Tooltip("카운트다운 전 게임 설명 UI. 이 UI가 떠서 확인을 기다리는 동안에는 ESC로 메뉴를 닫아도 " +
        "조작 잠금을 풀면 안 된다 (풀면 설명 UI가 떠 있는 채로 플레이어가 움직이고 커서도 다시 사라진다).")]
    [SerializeField] private PreGameDescriptionUI tetrisDescriptionUI;
    [SerializeField] private PreGameDescriptionUI pacmanDescriptionUI;
    [Tooltip("사망 팝업(재시작/게임설명/메인화면 버튼). 이 팝업이 떠 있는 동안에는 ESC로 메뉴를 닫아도 " +
        "조작 잠금/낙하를 풀면 안 된다 (풀면 사망 팝업이 떠 있는 채로 플레이어가 움직이고 블록이 떨어진다).")]
    [SerializeField] private GameObject deathPopupRoot;

    private bool wasPaused;

    private void Update()
    {
        bool isPaused = (pauseRoot != null && pauseRoot.activeSelf) || (menuRoot != null && menuRoot.activeSelf);

        if (isPaused == wasPaused)
        {
            return;
        }

        wasPaused = isPaused;

        if (isPaused)
        {
            ApplyPause();
        }
        else
        {
            ApplyResume();
        }
    }

    private void ApplyPause()
    {
        // 메인 메뉴(게임 시작 전) 상태에서는 애초에 잠글 조작/낙하/고스트가 없고,
        // PlayerController.SetControlsLocked가 커서를 강제로 잠가(Cursor.lockState=Locked, invisible)
        // 버려서 메뉴의 마우스 클릭 자체가 막혀버린다. 이 상태에서는 아무 것도 건드리지 않는다.
        if (MiniGameFlowManager.Instance != null && MiniGameFlowManager.Instance.CurrentState == MiniGameState.MainUI)
        {
            return;
        }

        if (tetrisGameManager != null)
        {
            tetrisGameManager.SetTetrisGameplayPaused(true);
        }

        if (MiniGameFlowManager.Instance != null)
        {
            MiniGameFlowManager.Instance.SetGhostsPaused(true);
        }

        if (playerController != null)
        {
            playerController.SetControlsLocked(true);
        }
    }

    private void ApplyResume()
    {
        if (MiniGameFlowManager.Instance != null && MiniGameFlowManager.Instance.CurrentState == MiniGameState.MainUI)
        {
            return;
        }

        // 카운트다운 전 설명 UI가 확인을 기다리는 중이거나, 사망 팝업(재시작/게임설명/메인화면 버튼)이
        // 떠 있는 중이라면 그 UI 자신이 조작 잠금을 관리하는 중이므로, 여기서 임의로 풀면 그 UI가
        // 떠 있는 채로 조작이 풀리고(플레이어가 움직이고) 낙하도 재개돼버린다.
        bool isDescriptionUIWaiting = (tetrisDescriptionUI != null && tetrisDescriptionUI.IsVisible)
            || (pacmanDescriptionUI != null && pacmanDescriptionUI.IsVisible);
        bool isDeathPopupShowing = deathPopupRoot != null && deathPopupRoot.activeSelf;
        if (isDescriptionUIWaiting || isDeathPopupShowing)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.SetControlsLocked(false);
        }

        // 카운트다운(3,2,1)이 아직 끝나지 않았다면, 고스트와 테트리스 낙하 둘 다 아직 움직이면 안 되는
        // 상태다. SetGhostsPaused(false)/SetTetrisGameplayPaused(false)를 그대로 부르면 카운트다운
        // 진행 중에 ESC를 닫는 것만으로 고스트/낙하가 곧바로(HandleGameplayReady를 거치지 않고)
        // 시작돼버리므로, 카운트다운이 끝난 뒤 정식 흐름이 활성화할 때까지 이 호출들만 건너뛴다
        // (플레이어 조작 잠금 해제는 카운트다운 중에도 가능해야 하므로 그대로 진행).
        bool isCountdownPlaying = countdownUI != null && countdownUI.IsPlaying;

        if (!isCountdownPlaying && MiniGameFlowManager.Instance != null)
        {
            MiniGameFlowManager.Instance.SetGhostsPaused(false);
        }

        if (!isCountdownPlaying && tetrisGameManager != null && MiniGameFlowManager.Instance != null)
        {
            tetrisGameManager.SetTetrisGameplayPaused(MiniGameFlowManager.Instance.CurrentState != MiniGameState.Tetris);
        }
    }
}
