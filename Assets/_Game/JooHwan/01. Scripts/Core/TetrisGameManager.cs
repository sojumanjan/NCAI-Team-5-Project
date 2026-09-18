using UnityEngine;

/// <summary>
/// 테트리스 전용 세부 로직(낙하 시퀀스, 클리어 판정, 관전 카메라 허용)을 담당한다.
/// 테트리스와 팩맨이 공유하는 플레이어/카메라/사망 팝업 자체는 SharedGameplayManager가 관리한다.
/// </summary>
public class TetrisGameManager : MonoBehaviour
{
    [SerializeField] private TetrisFallSequencer fallSequencer;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameObject clearTextRoot;

    private bool isCleared;

    public bool IsCleared => isCleared;

    private void Awake()
    {
        overviewCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// 팩맨은 아직 전용 카메라가 없어, 테트리스의 1인칭 카메라/플레이어 조작을 그대로 들고 간다.
    /// 낙하 로직(IsGlobalPaused 등)만 멈추고, 카메라/조작/관전 전환 여부는 그대로 유지한다.
    /// </summary>
    public void SetTetrisGameplayPaused(bool isPaused)
    {
        FallingBlock.IsGlobalPaused = isPaused;
        fallSequencer.SetPaused(isPaused);
    }

    /// <summary>
    /// 관전 카메라 전환(Tab)은 테트리스 전용이다 (팩맨에는 관전 시점이 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출한다.
    /// </summary>
    public void SetCameraSwitchAllowed(bool isAllowed)
    {
        cameraRig.SetCameraSwitchAllowed(isAllowed);
    }

    /// <summary>
    /// MiniGameFlowManager가 페이드/카운트다운을 모두 마친 뒤 호출한다.
    /// (재시작은 RestartFromDeath, 최초 시작은 이 메서드로 나뉜다)
    /// </summary>
    public void StartSequenceForNewGame()
    {
        fallSequencer.StartSequence();
    }

    /// <summary>
    /// EXIT 트리거(TetrisExitTrigger)가 플레이어 도달을 감지하면 호출한다.
    /// 별도 성공 UI 없이 CLEAR 텍스트만 짧게 보여준 뒤 조작을 해제한다.
    /// </summary>
    public void HandleExitReached(PlayerController playerController)
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;

        FallingBlock.IsGlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(true);

        if (clearTextRoot != null)
        {
            StartCoroutine(ShowClearTextRoutine(playerController));
        }
    }

    private System.Collections.IEnumerator ShowClearTextRoutine(PlayerController playerController)
    {
        clearTextRoot.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        clearTextRoot.SetActive(false);

        // 클리어 후에는 문 앞까지 자유롭게 이동할 수 있어야 하므로 조작을 다시 푼다.
        playerController.SetControlsLocked(false);
    }

    /// <summary>
    /// 디버그 전용: CLEAR 연출 없이 즉시 "클리어됨" 상태로 만든다.
    /// (팩맨 진입 직전 상태부터 테스트할 때 사용)
    /// </summary>
    public void DebugForceClearedState(PlayerController playerController)
    {
        isCleared = true;
        FallingBlock.IsGlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(false);
    }

    /// <summary>
    /// 사망 팝업의 재시작 버튼이 눌렸을 때, 현재 상태가 테트리스면 SharedGameplayManager가 호출한다.
    /// respawnPlayer는 실제 리스폰 위치 이동(SharedGameplayManager가 담당)을 위한 콜백이다.
    /// </summary>
    public void RestartFromDeath(PlayerController playerController, System.Action respawnPlayer)
    {
        fallSequencer.ResetSequence();
        respawnPlayer();

        FallingBlock.IsGlobalPaused = false;

        // 카운트다운(3,2,1)이 보이는 동안에도 플레이어는 바로 움직일 수 있어야 하므로,
        // 조작 잠금은 카운트다운을 재생하기 전에 미리 풀어둔다.
        playerController.SetControlsLocked(false);

        countdownUI.Play(countdownStartFrom, () =>
        {
            fallSequencer.StartSequence();
        });
    }
}
