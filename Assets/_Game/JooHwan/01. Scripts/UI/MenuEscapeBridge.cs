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

        if (playerController != null)
        {
            playerController.SetControlsLocked(false);
        }

        if (MiniGameFlowManager.Instance != null)
        {
            MiniGameFlowManager.Instance.SetGhostsPaused(false);
        }

        if (tetrisGameManager != null && MiniGameFlowManager.Instance != null)
        {
            tetrisGameManager.SetTetrisGameplayPaused(MiniGameFlowManager.Instance.CurrentState != MiniGameState.Tetris);
        }
    }
}
