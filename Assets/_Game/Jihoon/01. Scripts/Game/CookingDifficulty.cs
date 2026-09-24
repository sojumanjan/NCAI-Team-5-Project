// 요리 미니게임의 난이도 3단계 수치를 들고, 고른 단계를 셔터가 열리기 전에 각 시스템에 밀어 넣는 컴포넌트
using System;
using UnityEngine;

public enum CookingDifficultyLevel
{
    Easy,
    Normal,
    Hard,
}

/// <summary>한 난이도의 수치 묶음. 기본값은 난이도를 넣기 전의 씬 수치(= 보통)다.</summary>
[Serializable]
public class CookingDifficultyPreset
{
    [Header("손님")]
    [Tooltip("다음 손님까지의 간격 (초). 방금 온 손님의 주문 개수별: 첫 칸 1개, 둘째 2개, 셋째 3개.")]
    public Vector2[] spawnIntervalByOrderCount =
    {
        new Vector2(12f, 16f),
        new Vector2(22f, 25f),
        new Vector2(30f, 35f),
    };

    [Tooltip("손님이 기다려주는 시간 (초). 이 범위에서 매번 랜덤.")]
    public Vector2 patienceRange = new Vector2(35f, 45f);

    [Tooltip("주문 개수 1·2·3개가 나올 가중치.")]
    public float[] orderCountWeights = { 40f, 40f, 15f };

    [Header("평점")]
    [Tooltip("시작 평점.")]
    public float startingRating = 1f;

    [Tooltip("영업이 끝났을 때 이 평점 이상이면 클리어.")]
    public float clearRating = 4f;

    [Tooltip("주문을 맞췄을 때. 주문 개수 1·2·3개별.")]
    public float[] correctDeltas = { 0.3f, 0.4f, 0.5f };

    [Tooltip("틀린 음식을 줬을 때. 주문 개수 1·2·3개별.")]
    public float[] wrongDeltas = { -0.2f, -0.2f, -0.2f };

    [Tooltip("손님이 기다리다 떠났을 때. 주문 개수 1·2·3개별.")]
    public float[] abandonedDeltas = { -0.2f, -0.2f, -0.2f };

    [Header("영업 시간")]
    [Tooltip("09시에서 21시까지 흐르는 실제 시간 (초).")]
    public float dayLengthSeconds = 350f;
}

/// <summary>
/// 난이도는 영업을 시작하기 전에만 바꿀 수 있다. 영업 중에 바꾸면 이미 쌓인 평점의 기준이 흔들린다.
///
/// 씬을 열 때마다 아무것도 고르지 않은 채 시작하고, 하나를 골라야 셔터가 열린다. 다시하기도 씬을 새로
/// 여는 것이라 다시 골라야 한다. 고르기 전에도 수치는 보통으로 채워 둔다 — 평점 UI가 첫 화면부터
/// 그릴 값이 있어야 해서다.
/// </summary>
public class CookingDifficulty : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MiniGameSession session;
    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private RatingService rating;
    [SerializeField] private DayClock clock;

    [Tooltip("셔터가 올라가기 시작하면 잠급니다. 셔터가 다 열려야 영업이 시작돼서, 그 사이에 바꾸지 못하게.")]
    [SerializeField] private ShopOpener opener;

    [Header("난이도별 수치")]
    [SerializeField] private CookingDifficultyPreset easy = new();
    [SerializeField] private CookingDifficultyPreset normal = new();
    [SerializeField] private CookingDifficultyPreset hard = new();

    /// <summary>지금 수치가 들어가 있는 난이도. 아직 고르지 않았으면 보통이지만 <see cref="HasSelection"/>은 거짓이다.</summary>
    public CookingDifficultyLevel Current { get; private set; } = CookingDifficultyLevel.Normal;

    /// <summary>플레이어가 난이도를 하나라도 골랐는지. 셔터와 튜토리얼이 본다.</summary>
    public bool HasSelection { get; private set; }

    /// <summary>이 난이도가 골라져 있는지. 버튼이 눌린 모습을 정할 때 쓴다.</summary>
    public bool IsSelected(CookingDifficultyLevel level) => HasSelection && Current == level;

    /// <summary>지금 난이도를 바꿀 수 있는지.</summary>
    public bool CanChange =>
        session != null && session.State == CookingSessionState.Ready && (opener == null || !opener.HasOpened);

    /// <summary>난이도가 바뀌었을 때. 버튼들이 눌린 모습을 맞춘다.</summary>
    public event Action<CookingDifficultyLevel> Changed;

    private void Awake()
    {
        Apply(CookingDifficultyLevel.Normal);
    }

    /// <summary>난이도를 고른다. 바꿀 수 없는 때면 무시한다. 이미 고른 것을 다시 고르면 아무 일도 없다.</summary>
    public void Select(CookingDifficultyLevel level)
    {
        if (!CanChange || IsSelected(level))
        {
            return;
        }

        HasSelection = true;
        Apply(level);
    }

    private void Apply(CookingDifficultyLevel level)
    {
        Current = level;
        CookingDifficultyPreset preset = PresetFor(level);

        if (spawner != null)
        {
            spawner.ApplyDifficulty(preset.spawnIntervalByOrderCount, preset.patienceRange, preset.orderCountWeights);
        }

        if (rating != null)
        {
            rating.ApplyDifficulty(preset.startingRating, preset.correctDeltas, preset.wrongDeltas, preset.abandonedDeltas);
        }

        if (clock != null)
        {
            clock.SetDayLength(preset.dayLengthSeconds);
        }

        if (session != null)
        {
            session.SetClearRating(preset.clearRating);
        }

        Changed?.Invoke(level);
    }

    private CookingDifficultyPreset PresetFor(CookingDifficultyLevel level)
    {
        switch (level)
        {
            case CookingDifficultyLevel.Easy:
                return easy;
            case CookingDifficultyLevel.Hard:
                return hard;
            default:
                return normal;
        }
    }
}
