using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 요리 미니게임 한 판이 어디쯤 와 있는지.
///
/// 이름에 Cooking을 붙인 이유는 이 프로젝트가 네임스페이스 없이 전역 하나를 쓰기 때문이다.
/// 처음엔 SessionState였다가 에디터 타입과 부딪혔고, 그 다음 이름은 다른 팀원의 미니게임
/// 흐름 관리자와 부딪혔다. 공용이 될 법한 이름은 피한다.
/// </summary>
public enum CookingSessionState
{
    Ready,
    Running,
    Ended,
}

/// <summary>
/// 한 판을 지휘한다. 시계를 돌리고, 손님을 들이고, 21시가 되면 평점을 보고 승패를 매긴다.
///
/// 판단이 여기 모여 있는 이유: DayClock은 점수를 모르고 RatingService는 시간을 모른다.
/// 둘을 아는 쪽이 하나는 있어야 하는데, 그게 여기다.
/// </summary>
public class MiniGameSession : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("영업시간을 세는 시계.")]
    [SerializeField] private DayClock clock;

    [Tooltip("평점을 들고 있는 쪽.")]
    [SerializeField] private RatingService rating;

    [Tooltip("손님을 보내는 쪽.")]
    [SerializeField] private CustomerSpawner spawner;

    [Header("클리어 조건")]
    [Tooltip("영업이 끝난 시점에 평점이 이 값 이상이면 클리어.")]
    [SerializeField] private float clearRating = 4f;

    [Header("종료 처리")]
    [Tooltip("끝나면 Time.timeScale을 0으로. 인내심과 조리가 전부 멈춥니다.")]
    [SerializeField] private bool freezeTimeOnEnd = true;

    [Tooltip("끝날 때 꺼야 할 플레이어 스크립트들. 시점 조작은 deltaTime을 안 쓰므로 " +
             "timeScale 0으로는 안 멈춥니다 — 여기에 꼭 넣으세요.")]
    [SerializeField] private MonoBehaviour[] disableOnEnd;

    [Tooltip("커서를 풀기 위해 필요합니다.")]
    [SerializeField] private PlayerControllerJihoon player;

    [Header("시작")]
    [Tooltip("켜면 플레이와 동시에 영업을 시작합니다. 튜토리얼이 붙으면 끄고 StartDay()를 부르세요.")]
    [SerializeField] private bool startOnPlay = true;

    /// <summary>지금 판의 상태.</summary>
    public CookingSessionState State { get; private set; } = CookingSessionState.Ready;

    /// <summary>끝난 뒤의 결과. 끝나기 전에는 기본값.</summary>
    public CookingResult Result { get; private set; }

    /// <summary>영업이 끝나고 승패가 정해졌을 때 한 번.</summary>
    public event Action<CookingResult> SessionEnded;

    // ---------------------------------------------------------------- 수명주기

    private void Awake()
    {
        if (clock == null || rating == null || spawner == null)
        {
            Debug.LogError($"{nameof(MiniGameSession)}: 시계 · 평점 · 스포너 참조가 모두 필요합니다.", this);
            enabled = false;
            return;
        }

        // 이전 판에서 0으로 얼려둔 채 씬을 다시 열 수 있다.
        Time.timeScale = 1f;
    }

    private void OnEnable() => clock.DayEnded += HandleDayEnded;

    private void OnDisable() => clock.DayEnded -= HandleDayEnded;

    private void Start()
    {
        if (startOnPlay)
        {
            StartDay();
        }
    }

    // ---------------------------------------------------------------- 진행

    /// <summary>영업을 시작한다. 튜토리얼이 끝난 뒤 불러도 된다.</summary>
    public void StartDay()
    {
        if (State == CookingSessionState.Running)
        {
            return;
        }

        State = CookingSessionState.Running;
        Time.timeScale = 1f;

        clock.Begin();
        spawner.Begin();
    }

    private void HandleDayEnded()
    {
        if (State == CookingSessionState.Ended)
        {
            return;
        }

        State = CookingSessionState.Ended;

        spawner.Pause();
        Result = BuildResult();

        LockDownPlayer();
        ReportToHub();
        SessionEnded?.Invoke(Result);
    }

    private CookingResult BuildResult()
    {
        bool cleared = rating.Rating >= clearRating;

        return new CookingResult(cleared, rating.Rating, clearRating,
                                 rating.CorrectCount, rating.ResolvedCount);
    }

    /// <summary>
    /// 허브에 결과를 알린다. 재시도 버튼을 누르든 돌아가기를 누르든 결과는 이미 나왔으므로,
    /// 버튼이 아니라 영업이 끝나는 순간에 보고한다.
    /// </summary>
    private void ReportToHub()
    {
        GameFlow flow = GameFlow.Instance;
        if (flow == null)
        {
            return;
        }

        // 평점을 0~1로 환산해 넘긴다. 허브는 평점이 몇 점 만점인지 알 필요가 없다.
        // 어느 미니게임인지는 씬 이름으로 알아내므로 인스펙터에 꽂을 것이 없다.
        flow.ReportCurrent(new MiniGameResult(Result.Cleared, rating.Normalized));
    }

    /// <summary>움직임과 시점을 멈추고 커서를 돌려준다.</summary>
    private void LockDownPlayer()
    {
        if (disableOnEnd != null)
        {
            foreach (MonoBehaviour script in disableOnEnd)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }

        if (player != null)
        {
            player.SetCursorLocked(false);
        }

        if (freezeTimeOnEnd)
        {
            Time.timeScale = 0f;
        }
    }

    // ---------------------------------------------------------------- 결과 화면 버튼

    /// <summary>같은 씬을 다시 연다. 씬이 Build Settings에 등록돼 있어야 동작한다.</summary>
    public void Retry()
    {
        Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        if (current.buildIndex < 0)
        {
            Debug.LogError($"'{current.name}' 씬이 Build Settings에 없어 다시 시작할 수 없습니다. " +
                           "File > Build Profiles 에서 추가해주세요.", this);
            return;
        }

        SceneManager.LoadScene(current.buildIndex);
    }

    /// <summary>메인 화면으로 돌아간다. 결과는 영업이 끝날 때 이미 보고했다.</summary>
    public void ReturnToHub()
    {
        GameFlow flow = GameFlow.Instance;
        if (flow == null)
        {
            return;
        }

        flow.ReturnToMain();
    }

    private void OnValidate()
    {
        clearRating = Mathf.Max(0f, clearRating);
    }
}
