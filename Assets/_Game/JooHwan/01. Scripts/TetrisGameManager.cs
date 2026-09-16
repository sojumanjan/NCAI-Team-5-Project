using UnityEngine;

public class TetrisGameManager : MonoBehaviour
{
    public static TetrisGameManager Instance { get; private set; }

    [SerializeField] private GameObject deathPopupRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TetrisFallSequencer fallSequencer;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Vector3 playerStartPosition;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameSelectUI gameSelectUI;
    [SerializeField] private GameObject clearTextRoot;

    private CharacterController playerCharacterController;
    private bool isCleared;

    public bool IsCleared => isCleared;

    private void Awake()
    {
        Instance = this;
        playerCharacterController = playerController.GetComponent<CharacterController>();
        playerStartPosition = playerController.transform.position;
    }

    /// <summary>
    /// 씬 오브젝트는 항상 활성 상태를 유지하고, 이 메서드로 테트리스 진행 여부만 켜고 끈다.
    /// (테트리스 <-> 팩맨 전환 시 SetActive 대신 사용)
    /// </summary>
    public void SetGameActive(bool active)
    {
        firstPersonCamera.gameObject.SetActive(active);
        overviewCamera.gameObject.SetActive(false);

        playerController.enabled = active;
        cameraRig.enabled = active;

        FallingBlock.GlobalPaused = !active;
        fallSequencer.SetPaused(!active);

        if (!active)
        {
            deathPopupRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 팩맨은 아직 전용 카메라가 없어, 테트리스의 1인칭 카메라/플레이어 조작을 그대로 들고 간다.
    /// 낙하 로직(GlobalPaused 등)만 멈추고, 카메라/조작/관전 전환 여부는 그대로 유지한다.
    /// </summary>
    public void SetTetrisGameplayPaused(bool paused)
    {
        FallingBlock.GlobalPaused = paused;
        fallSequencer.SetPaused(paused);
    }

    /// <summary>
    /// 에임 포인터(크로스헤어)는 팩맨 상태에서만 보여야 한다 (테트리스에는 조준 요소가 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출한다.
    /// </summary>
    public void SetCrosshairAllowed(bool allowed)
    {
        cameraRig.SetCrosshairAllowed(allowed);
    }

    /// <summary>
    /// MiniGameFlowManager가 페이드/카운트다운을 모두 마친 뒤 호출한다.
    /// (재시작은 OnClickRestart, 최초 시작은 이 메서드로 나뉜다)
    /// </summary>
    public void StartSequenceForNewGame()
    {
        fallSequencer.StartSequence();
    }

    /// <summary>
    /// EXIT 트리거(TetrisExitTrigger)가 플레이어 도달을 감지하면 호출한다.
    /// 별도 성공 UI 없이 CLEAR 텍스트만 짧게 보여준 뒤 조작을 해제한다.
    /// </summary>
    public void OnExitReached()
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;

        FallingBlock.GlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(true);

        if (clearTextRoot != null)
        {
            StartCoroutine(ShowClearTextRoutine());
        }
    }

    private System.Collections.IEnumerator ShowClearTextRoutine()
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
    public void DebugForceClearedState()
    {
        isCleared = true;
        FallingBlock.GlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(false);
    }

    public void OnPlayerPinned()
    {
        FallingBlock.GlobalPaused = true;
        playerController.SetControlsLocked(true);
        deathPopupRoot.SetActive(true);
    }

    /// <summary>
    /// 팩맨에서 목숨이 0이 되었을 때 PlayerHealth가 호출한다.
    /// 테트리스 사망 팝업과 동일한 UI를 재사용하되, 재시작은 OnClickRestart가 상태를 보고 팩맨 쪽으로 분기한다.
    /// </summary>
    public void ShowDeathPopupForPacman()
    {
        playerController.SetControlsLocked(true);
        deathPopupRoot.SetActive(true);
    }

    /// <summary>
    /// 사망 팝업(DeathPopup)은 테트리스/팩맨 공용이라, 재시작 버튼이 눌리면
    /// 현재 어느 게임이 진행 중이었는지에 따라 재시작 대상을 나눈다.
    /// </summary>
    public void OnClickRestart()
    {
        deathPopupRoot.SetActive(false);

        if (MiniGameFlowManager.Instance.CurrentState == MiniGameState.Pacman)
        {
            RestartPacmanFromDeath();
            return;
        }

        fallSequencer.ResetSequence();
        RespawnPlayer();

        FallingBlock.GlobalPaused = false;

        // 카운트다운(3,2,1)이 보이는 동안에도 플레이어는 바로 움직일 수 있어야 하므로,
        // 조작 잠금은 카운트다운을 재생하기 전에 미리 풀어둔다.
        playerController.SetControlsLocked(false);

        countdownUI.Play(countdownStartFrom, () =>
        {
            fallSequencer.StartSequence();
        });
    }

    private void RestartPacmanFromDeath()
    {
        playerController.SetControlsLocked(false);
        var playerHealth = playerController.GetComponent<PlayerHealth>();
        MiniGameFlowManager.Instance.RestartPacman(playerHealth.PacmanRestartPoint, playerHealth);
    }

    private void RespawnPlayer()
    {
        playerCharacterController.enabled = false;
        playerController.transform.position = playerStartPosition;
        playerCharacterController.enabled = true;
    }

    public void OnClickShowDescription()
    {
        // GameSelectUI의 설명 팝업을 재사용한다.
        gameSelectUI.OnClickDescription();
    }

    public void OnClickExitToHub()
    {
        // 허브 씬이 아직 없어 자리만 마련해둔다.
        Debug.Log("[TetrisGameManager] Exit to hub requested (not implemented yet)");
    }
}
