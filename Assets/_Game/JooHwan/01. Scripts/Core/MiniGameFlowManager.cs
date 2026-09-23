using UnityEngine;

/// <summary>
/// 테트리스/팩맨은 하나로 이어지는 연속 흐름이라, 두 미니게임 오브젝트는 SetActive로 껐다 켜지 않고
/// 항상 활성 상태를 유지한다. 대신 각 게임의 GameManager가 제공하는 SetGameActive(bool)로
/// "진행 중" 여부(카메라/조작/게임 로직)만 켜고 끈다.
/// 상태는 메인 UI(둘 다 정지) -> 테트리스(테트리스 활성) -> 팩맨(팩맨 활성) 순으로 전환된다.
/// </summary>
public enum MiniGameState
{
    MainUI,
    Tetris,
    Pacman
}

public class MiniGameFlowManager : MonoBehaviour
{
    public static MiniGameFlowManager Instance { get; private set; }

    [SerializeField] private SharedGameplayManager sharedGameplayManager;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private PacmanGameManager pacmanGameManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private FadeCanvas fadeCanvas;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [Tooltip("카운트다운 시작 전에 보여줄 게임 설명 UI. 확인 버튼을 눌러야 카운트다운이 시작된다.")]
    [SerializeField] private PreGameDescriptionUI tetrisDescriptionUI;
    [SerializeField] private PreGameDescriptionUI pacmanDescriptionUI;
    [SerializeField] private Ghost[] pacmanGhosts;
    [SerializeField] private GameObject heartsUIRoot;
    [SerializeField] private PelletSpawner pelletSpawner;
    [Tooltip("고스트 5마리 전멸 시 맵 중앙에 등장시킬 보상 상자")]
    [SerializeField] private GameObject rewardBox;

    [Header("Debug (팩맨 로직 작업 중 임시 사용)")]
    [Tooltip("체크하면 시작 시 인트로를 건너뛰고, 테트리스를 이미 클리어한 직후(문 앞 복도, 팩맨은 아직 비활성) 상태로 진입한다. 팩맨 구현이 끝나면 반드시 해제할 것.")]
    [SerializeField] private bool isDebugStartInPacman = false;
    [SerializeField] private Transform debugPacmanStartPoint;

    [Header("Debug (테트리스 클리어 연출 확인용 임시 사용)")]
    [Tooltip("체크하면 시작 시 EXIT 트리거 바로 아래에 임시 디딤대를 만들고 플레이어를 그 위로 옮겨, 실제로 EXIT을 밟아 클리어 연출(플래시/셰이크/텍스트)이 재생되는 것을 바로 확인할 수 있게 한다. 연출 확인이 끝나면 반드시 해제할 것.")]
    [SerializeField] private bool isDebugStartAtTetrisClear = false;
    [SerializeField] private TetrisExitTrigger debugExitTrigger;

    private GameObject debugClearTestPlatform;

    private MiniGameState currentState = MiniGameState.MainUI;

    public MiniGameState CurrentState => currentState;

    private void Awake()
    {
        Instance = this;

        if (pacmanGhosts != null)
        {
            foreach (var ghost in pacmanGhosts)
            {
                if (ghost != null)
                {
                    ghost.Defeated += HandleGhostDefeated;
                }
            }
        }
    }

    private void Start()
    {
        if (isDebugStartAtTetrisClear && debugExitTrigger != null)
        {
            // 실제로 EXIT 트리거를 밟게 만들어 HandleExitReached()의 정식 클리어 연출
            // (플래시/셰이크/텍스트 등)이 그대로 재생되는지 바로 확인할 수 있게 한다.
            // 낙하 시퀀스는 필요 없으므로 시작하지 않고, 그 자리에 서 있을 임시 디딤대만 만든다.
            ApplyState(MiniGameState.Tetris, shouldPlayCountdown: false);
            FallingBlock.IsGlobalPaused = true;

            SpawnDebugClearTestPlatform();
            return;
        }

        if (isDebugStartInPacman)
        {
            // 테트리스는 이미 클리어했고 아직 팩맨에는 진입하지 않은 상태(문 앞 복도)에서 시작한다.
            // 즉 상태 자체는 Tetris로 유지하되(팩맨 로직은 비활성), 낙하만 멈춘 클리어 상태로 만든다.
            ApplyState(MiniGameState.Tetris, shouldPlayCountdown: false);
            tetrisGameManager.DebugForceClearedState(playerController);

            if (debugPacmanStartPoint != null)
            {
                TeleportPlayer(debugPacmanStartPoint);
            }

            return;
        }

        // 게임시작/게임설명 인트로 UI(IntroUI)를 없애고, 씬 진입 즉시 테트리스 카운트다운으로 들어간다.
        // (기존에는 여기서 MainUI 상태로 대기하다가 GameSelectUI의 "게임시작" 버튼으로 StartTetris를 불렀다)
        StartTetris();
    }

    public void StartTetris()
    {
        SwitchTo(MiniGameState.Tetris, shouldPlayCountdown: true);
    }

    /// <summary>
    /// teleportTarget이 주어지면, 화면이 완전히 어두워진 뒤(FadeOut 완료 후) 플레이어를 그 위치로 옮긴다.
    /// (화면이 밝은 상태에서 순간이동이 보이지 않도록 페이드 콜백 안에서 처리한다)
    /// </summary>
    public void StartPacman(Transform teleportTarget = null)
    {
        SwitchTo(MiniGameState.Pacman, shouldPlayCountdown: true, teleportTarget);
    }

    /// <summary>
    /// 팩맨에서 사망(목숨 소진) 시 호출. 테트리스로 돌아가지 않고 팩맨만 같은 방식(페이드+카운트다운)으로 재시작한다.
    /// 몇 마리가 죽었든, 파워펠릿을 얼마나 먹었든 전부 처음 상태로 되돌린다.
    /// </summary>
    public void RestartPacman(Transform teleportTarget, PacmanPlayerHealth playerHealth)
    {
        if (playerHealth != null)
        {
            playerHealth.ResetLives();

            var pelletThrower = playerHealth.GetComponent<PelletThrower>();
            if (pelletThrower != null)
            {
                pelletThrower.ResetCarried();
            }
        }

        if (pelletSpawner != null)
        {
            pelletSpawner.RegenerateAll();
        }

        ResetAllGhosts();

        SwitchTo(MiniGameState.Pacman, shouldPlayCountdown: true, teleportTarget);
    }

    private void ResetAllGhosts()
    {
        if (rewardBox != null)
        {
            rewardBox.SetActive(false);
        }

        if (pacmanGhosts == null)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null)
            {
                ghost.ResetForRestart();
            }
        }
    }

    /// <summary>
    /// 고스트가 처치될 때마다 호출된다. 5마리(pacmanGhosts 전부)가 모두 처치되면
    /// 클리어 조건 달성으로 보고 보상 상자를 맵 중앙에 등장시킨다.
    /// </summary>
    private void HandleGhostDefeated(Ghost defeatedGhost)
    {
        if (pacmanGhosts == null || rewardBox == null)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null && !ghost.IsDefeated)
            {
                return;
            }
        }

        rewardBox.SetActive(true);
    }

    private void SwitchTo(MiniGameState target, bool shouldPlayCountdown, Transform teleportTarget = null)
    {
        fadeCanvas.FadeOut(() =>
        {
            // 화면이 완전히 어두워진 직후(카운트다운 전)에 플레이어 텔레포트와 고스트 워프를 함께 처리한다.
            if (teleportTarget != null)
            {
                TeleportPlayer(teleportTarget);
            }

            if (target == MiniGameState.Pacman)
            {
                PrepareGhostsForPacman();
            }

            ApplyState(target, shouldPlayCountdown);
        });
    }

    private void TeleportPlayer(Transform target)
    {
        TeleportPlayer(target.position);
    }

    private void TeleportPlayer(Vector3 position)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }
        else
        {
            player.transform.position = position;
        }
    }

    /// <summary>
    /// EXIT 트리거 바로 아래에 임시 디딤대(Cube)를 만들고 플레이어를 그 위로 옮겨,
    /// 실제로 EXIT 콜라이더를 통과시켜 정식 클리어 연출을 그대로 재생시킨다.
    /// 디버그 전용이라 씬에 영구적으로 남기지 않고 코드로만 생성한다.
    /// </summary>
    private const float DebugClearPlatformExtraDepth = 3f;
    [Tooltip("디버그 클리어 테스트 디딤대가 뒤쪽(진행 방향 반대) 벽을 뚫지 않도록 남겨둘 여유 거리.")]
    [SerializeField] private float debugClearPlatformWallMargin = 0.5f;

    private void SpawnDebugClearTestPlatform()
    {
        Bounds exitBounds = debugExitTrigger.GetComponent<Collider>().bounds;

        // 아레나 뒤쪽 벽(Floor 바운즈의 -Z 끝)을 뚫지 않도록, 추가하려는 깊이(큐브 한 칸)를
        // 실제로 남아있는 여유 거리로 제한한다.
        float floorMinZ = float.MinValue;
        Transform floorTransform = debugExitTrigger.transform.parent != null ? debugExitTrigger.transform.parent.Find("Floor") : null;
        if (floorTransform != null && floorTransform.TryGetComponent(out Collider floorCollider))
        {
            floorMinZ = floorCollider.bounds.min.z;
        }

        float availableDepth = floorMinZ > float.MinValue ? exitBounds.min.z - floorMinZ - debugClearPlatformWallMargin : DebugClearPlatformExtraDepth;
        float extraDepth = Mathf.Clamp(availableDepth, 0f, DebugClearPlatformExtraDepth);

        // EXIT 트리거 안쪽으로 바로 텔레포트되면 걸어갈 필요 없이 즉시 클리어되어 버리므로,
        // 진행 방향(-Z)으로 여유가 허용하는 만큼(최대 큐브 한 칸, 3유닛) 더 길게 만들어 그 뒤쪽에서 걸어오게 한다.
        float platformDepth = exitBounds.size.z + extraDepth;
        float platformCenterZ = exitBounds.center.z - extraDepth * 0.5f;

        debugClearTestPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugClearTestPlatform.name = "DebugClearTestPlatform";
        debugClearTestPlatform.transform.position = new Vector3(exitBounds.center.x, exitBounds.min.y - 0.5f, platformCenterZ);
        debugClearTestPlatform.transform.localScale = new Vector3(exitBounds.size.x, 1f, platformDepth);

        Vector3 spawnPosition = new Vector3(exitBounds.center.x, exitBounds.min.y - 0.5f + 1.5f, exitBounds.center.z - extraDepth);
        TeleportPlayer(spawnPosition);
    }

    /// <summary>
    /// 대상 상태에 맞춰 UI/카메라/각 게임의 진행 여부를 일괄 적용한다.
    /// </summary>
    private void ApplyState(MiniGameState target, bool shouldPlayCountdown)
    {
        currentState = target;

        if (target == MiniGameState.MainUI)
        {
            // 메인 UI에서는 어느 게임의 PlayerController도 활성화되지 않아
            // 커서 상태를 아무도 갱신하지 않으므로, 여기서 직접 마우스를 보이게 한다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 팩맨은 아직 전용 카메라가 없어, 테트리스의 1인칭 카메라/조작을 그대로 들고 간다.
        // (관전 카메라 전환만 막고, 낙하 로직은 별도로 정지시킨다)
        sharedGameplayManager.SetGameActive(target == MiniGameState.Tetris || target == MiniGameState.Pacman);
        tetrisGameManager.SetTetrisGameplayPaused(target != MiniGameState.Tetris);
        tetrisGameManager.SetCameraSwitchAllowed(target == MiniGameState.Tetris);

        // 팩맨이 아닌 상태로 전환될 때는(테트리스 클리어 직후 복도 등) 고스트를 즉시 멈춘다.
        // 팩맨 상태로 전환될 때는 카운트다운(3,2,1)이 끝난 뒤(OnGameplayReady)에야 움직이기 시작해야 하므로
        // 여기서는 끄기만 하고, 켜는 시점은 OnGameplayReady로 미룬다.
        if (target != MiniGameState.Pacman)
        {
            SetGhostsActive(isActive: false);
        }

        // 에임 포인터(크로스헤어)와 목숨 하트 UI는 팩맨 전용이라 테트리스에서는 보이면 안 된다.
        pacmanGameManager.SetCrosshairAllowed(target == MiniGameState.Pacman);

        // 팩맨에는 점프 지형이 없으므로 비활성화한다.
        // (같은 Jump 액션을 공유하는 ClimbController도 jumpAction이 Disable되면 자동으로 트리거되지 않는다)
        pacmanGameManager.SetJumpAllowed(target != MiniGameState.Pacman);

        if (heartsUIRoot != null)
        {
            heartsUIRoot.SetActive(target == MiniGameState.Pacman);
        }

        fadeCanvas.FadeIn(() =>
        {
            if (shouldPlayCountdown)
            {
                PlayCountdownWithDescription(target, () => HandleGameplayReady(target));
            }
        });
    }

    /// <summary>
    /// 카운트다운 시작 전에 대상 게임에 맞는 설명 UI를 먼저 보여주고, 확인을 눌러야 카운트다운을 재생한다.
    /// 설명 UI가 연결되어 있지 않으면 기존처럼 곧바로 카운트다운을 재생한다.
    /// </summary>
    private void PlayCountdownWithDescription(MiniGameState target, System.Action onCountdownComplete)
    {
        PreGameDescriptionUI descriptionUI = target == MiniGameState.Tetris ? tetrisDescriptionUI
            : target == MiniGameState.Pacman ? pacmanDescriptionUI
            : null;

        if (descriptionUI != null)
        {
            descriptionUI.Show(() => countdownUI.Play(countdownStartFrom, onCountdownComplete));
        }
        else
        {
            countdownUI.Play(countdownStartFrom, onCountdownComplete);
        }
    }

    private void HandleGameplayReady(MiniGameState target)
    {
        if (target == MiniGameState.Tetris)
        {
            tetrisGameManager.StartSequenceForNewGame();
        }
        else if (target == MiniGameState.Pacman)
        {
            SetGhostsActive(isActive: true);
        }
    }

    private void SetGhostsActive(bool isActive)
    {
        if (pacmanGhosts == null)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null)
            {
                ghost.SetActive(isActive);
            }
        }
    }

    /// <summary>
    /// 일시정지 패널이 열려있는 동안 고스트의 이동/추격/발광만 멈춘다 (위치는 그대로 유지).
    /// 팩맨 상태가 아니거나(고스트가 이미 꺼져있음) 이미 처치된 고스트는 건드리지 않는다.
    /// PauseUI가 호출한다.
    /// </summary>
    public void SetGhostsPaused(bool isPaused)
    {
        if (pacmanGhosts == null || currentState != MiniGameState.Pacman)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null && !ghost.IsDefeated)
            {
                ghost.SetActive(!isPaused, shouldEnterIdlePose: false);
            }
        }
    }

    /// <summary>
    /// 화면이 어두운 상태(페이드아웃 직후)에 호출해, 고스트를 대기 위치에서 순찰 시작 위치로
    /// 미리 옮겨둔다 (아직 움직이기 시작하지는 않음 - 카운트다운 후 SetGhostsActive(true)가 실제로 시작시킨다).
    /// </summary>
    private void PrepareGhostsForPacman()
    {
        if (pacmanGhosts == null)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null)
            {
                ghost.PrepareForPacman();
            }
        }
    }
}
