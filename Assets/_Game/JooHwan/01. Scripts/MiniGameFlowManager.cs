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

    [SerializeField] private GameObject gameSelectUIRoot;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private FadeCanvas fadeCanvas;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameObject selectSceneCamera;
    [SerializeField] private Ghost[] pacmanGhosts;
    [SerializeField] private GameObject heartsUIRoot;
    [SerializeField] private PelletSpawner pelletSpawner;

    [Header("Debug (팩맨 로직 작업 중 임시 사용)")]
    [Tooltip("체크하면 시작 시 인트로를 건너뛰고, 테트리스를 이미 클리어한 직후(문 앞 복도, 팩맨은 아직 비활성) 상태로 진입한다. 팩맨 구현이 끝나면 반드시 해제할 것.")]
    [SerializeField] private bool debugStartInPacman = false;
    [SerializeField] private Transform debugPacmanStartPoint;

    private MiniGameState currentState = MiniGameState.MainUI;

    public MiniGameState CurrentState => currentState;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (debugStartInPacman)
        {
            // 테트리스는 이미 클리어했고 아직 팩맨에는 진입하지 않은 상태(문 앞 복도)에서 시작한다.
            // 즉 상태 자체는 Tetris로 유지하되(팩맨 로직은 비활성), 낙하만 멈춘 클리어 상태로 만든다.
            ApplyState(MiniGameState.Tetris, playCountdown: false);
            tetrisGameManager.DebugForceClearedState();

            if (debugPacmanStartPoint != null)
            {
                TeleportPlayer(debugPacmanStartPoint);
            }

            return;
        }

        ApplyState(MiniGameState.MainUI, playCountdown: false);
    }

    public void ShowGameSelect()
    {
        SwitchTo(MiniGameState.MainUI, playCountdown: false);
    }

    public void StartTetris()
    {
        SwitchTo(MiniGameState.Tetris, playCountdown: true);
    }

    /// <summary>
    /// teleportTarget이 주어지면, 화면이 완전히 어두워진 뒤(FadeOut 완료 후) 플레이어를 그 위치로 옮긴다.
    /// (화면이 밝은 상태에서 순간이동이 보이지 않도록 페이드 콜백 안에서 처리한다)
    /// </summary>
    public void StartPacman(Transform teleportTarget = null)
    {
        SwitchTo(MiniGameState.Pacman, playCountdown: true, teleportTarget);
    }

    /// <summary>
    /// 팩맨에서 사망(목숨 소진) 시 호출. 테트리스로 돌아가지 않고 팩맨만 같은 방식(페이드+카운트다운)으로 재시작한다.
    /// 몇 마리가 죽었든, 파워펠릿을 얼마나 먹었든 전부 처음 상태로 되돌린다.
    /// </summary>
    public void RestartPacman(Transform teleportTarget, PlayerHealth playerHealth)
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

        SwitchTo(MiniGameState.Pacman, playCountdown: true, teleportTarget);
    }

    private void ResetAllGhosts()
    {
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

    private void SwitchTo(MiniGameState target, bool playCountdown, Transform teleportTarget = null)
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

            ApplyState(target, playCountdown);
        });
    }

    private void TeleportPlayer(Transform target)
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
            player.transform.position = target.position;
            controller.enabled = true;
        }
        else
        {
            player.transform.position = target.position;
        }
    }

    /// <summary>
    /// 대상 상태에 맞춰 UI/카메라/각 게임의 진행 여부를 일괄 적용한다.
    /// </summary>
    private void ApplyState(MiniGameState target, bool playCountdown)
    {
        currentState = target;

        gameSelectUIRoot.SetActive(target == MiniGameState.MainUI);
        selectSceneCamera.SetActive(target == MiniGameState.MainUI);

        if (target == MiniGameState.MainUI)
        {
            // 메인 UI에서는 어느 게임의 PlayerController도 활성화되지 않아
            // 커서 상태를 아무도 갱신하지 않으므로, 여기서 직접 마우스를 보이게 한다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 팩맨은 아직 전용 카메라가 없어, 테트리스의 1인칭 카메라/조작을 그대로 들고 간다.
        // (관전 카메라 전환만 막고, 낙하 로직은 별도로 정지시킨다)
        tetrisGameManager.SetGameActive(target == MiniGameState.Tetris || target == MiniGameState.Pacman);
        tetrisGameManager.SetTetrisGameplayPaused(target != MiniGameState.Tetris);

        // 팩맨이 아닌 상태로 전환될 때는(테트리스 클리어 직후 복도 등) 고스트를 즉시 멈춘다.
        // 팩맨 상태로 전환될 때는 카운트다운(3,2,1)이 끝난 뒤(OnGameplayReady)에야 움직이기 시작해야 하므로
        // 여기서는 끄기만 하고, 켜는 시점은 OnGameplayReady로 미룬다.
        if (target != MiniGameState.Pacman)
        {
            SetGhostsActive(false);
        }

        // 에임 포인터(크로스헤어)와 목숨 하트 UI는 팩맨 전용이라 테트리스에서는 보이면 안 된다.
        tetrisGameManager.SetCrosshairAllowed(target == MiniGameState.Pacman);

        if (heartsUIRoot != null)
        {
            heartsUIRoot.SetActive(target == MiniGameState.Pacman);
        }

        fadeCanvas.FadeIn(() =>
        {
            if (playCountdown)
            {
                countdownUI.Play(countdownStartFrom, () => OnGameplayReady(target));
            }
        });
    }

    private void OnGameplayReady(MiniGameState target)
    {
        if (target == MiniGameState.Tetris)
        {
            tetrisGameManager.StartSequenceForNewGame();
        }
        else if (target == MiniGameState.Pacman)
        {
            SetGhostsActive(true);
        }
    }

    private void SetGhostsActive(bool active)
    {
        if (pacmanGhosts == null)
        {
            return;
        }

        foreach (var ghost in pacmanGhosts)
        {
            if (ghost != null)
            {
                ghost.SetActive(active);
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
