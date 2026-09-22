using UnityEngine;

/// <summary>
/// 팩맨 전용 세부 로직(크로스헤어, 점프 허용, 사망/재시작)을 담당한다.
/// 테트리스와 공유하는 플레이어/카메라/사망 팝업 자체는 SharedGameplayManager가 관리하고,
/// 이 클래스는 "팩맨일 때만" 필요한 판단과 재시작 처리를 담당한다.
/// </summary>
public class PacmanGameManager : MonoBehaviour
{
    public static PacmanGameManager Instance { get; private set; }

    [SerializeField] private PlayerController playerController;
    [SerializeField] private CrosshairController crosshairController;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 에임 포인터(크로스헤어)는 팩맨 상태에서만 보여야 한다 (테트리스에는 조준 요소가 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출한다.
    /// </summary>
    public void SetCrosshairAllowed(bool isAllowed)
    {
        crosshairController.SetAllowed(isAllowed);
    }

    /// <summary>
    /// 팩맨에는 점프 지형이 없으므로, 팩맨 상태에서는 비활성화한다.
    /// ClimbController도 같은 Jump 액션을 구독하므로 별도 처리 없이 함께 막힌다.
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출한다.
    /// </summary>
    public void SetJumpAllowed(bool isAllowed)
    {
        playerController.SetJumpEnabled(isAllowed);
    }

    /// <summary>
    /// 팩맨에서 목숨이 0이 되었을 때 PacmanPlayerHealth가 호출한다.
    /// 테트리스 사망 팝업과 동일한 UI를 재사용하되, 실제 팝업 표시는 SharedGameplayManager가 담당한다.
    /// </summary>
    public void ShowDeathPopupForPacman()
    {
        SharedGameplayManager.Instance.ShowDeathPopupForPacman();
    }

    /// <summary>
    /// 목숨이 남아있는 일반 피격(고스트 접촉) 시 PacmanPlayerHealth가 호출한다.
    /// 사망 팝업 없이, 사망보다 약한 흔들림/플래시 피드백만 재생한다.
    /// </summary>
    public void OnPlayerHit()
    {
        SharedGameplayManager.Instance.PlayHitFeedback();
    }

    /// <summary>
    /// 사망 팝업의 재시작 버튼이 눌렸을 때, 현재 상태가 팩맨이면 SharedGameplayManager가 호출한다.
    /// </summary>
    public void RestartPacmanFromDeath()
    {
        playerController.SetControlsLocked(false);
        var playerHealth = playerController.GetComponent<PacmanPlayerHealth>();
        MiniGameFlowManager.Instance.RestartPacman(playerHealth.PacmanRestartPoint, playerHealth);
    }
}
