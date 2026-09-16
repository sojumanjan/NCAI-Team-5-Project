using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>한 판이 어디쯤 와 있는지.</summary>
public enum MiniGameState
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

    [Header("보상")]
    [Tooltip("주문대로 만든 음식 하나당 비료.")]
    [SerializeField] private int fertilizerPerCorrect = 5;

    [Tooltip("클리어했을 때 추가로 주는 비료.")]
    [SerializeField] private int clearBonus = 50;

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
    public MiniGameState State { get; private set; } = MiniGameState.Ready;

    /// <summary>끝난 뒤의 결과. 끝나기 전에는 기본값.</summary>
    public MiniGameResult Result { get; private set; }

    /// <summary>영업이 끝나고 승패가 정해졌을 때 한 번.</summary>
    public event Action<MiniGameResult> SessionEnded;

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
        if (State == MiniGameState.Running)
        {
            return;
        }

        State = MiniGameState.Running;
        Time.timeScale = 1f;

        clock.Begin();
        spawner.Begin();
    }

    private void HandleDayEnded()
    {
        if (State == MiniGameState.Ended)
        {
            return;
        }

        State = MiniGameState.Ended;

        spawner.Pause();
        Result = BuildResult();

        LockDownPlayer();
        SessionEnded?.Invoke(Result);
    }

    private MiniGameResult BuildResult()
    {
        bool cleared = rating.Rating >= clearRating;

        int fertilizer = rating.CorrectCount * fertilizerPerCorrect;
        if (cleared)
        {
            fertilizer += clearBonus;
        }

        return new MiniGameResult(cleared, rating.Rating, clearRating,
                                  rating.CorrectCount, rating.ResolvedCount, fertilizer);
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

    /// <summary>
    /// 허브 복귀. 9단계에서 허브 씬 로드로 바꾼다 — 그때 <see cref="Result"/>를 넘기면 된다.
    /// </summary>
    public void ReturnToHub()
    {
        Debug.Log($"[미구현] 허브 복귀. 비료 {Result.Fertilizer}, 클리어 {Result.Cleared}", this);
    }

    private void OnValidate()
    {
        clearRating = Mathf.Max(0f, clearRating);
        fertilizerPerCorrect = Mathf.Max(0, fertilizerPerCorrect);
        clearBonus = Mathf.Max(0, clearBonus);
    }
}
