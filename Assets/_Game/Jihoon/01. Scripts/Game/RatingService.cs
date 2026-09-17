using System;
using UnityEngine;

/// <summary>
/// The shop's star rating. Starts at 1.0, moves on every resolved order, and broadcasts
/// the new value for the UI.
///
/// Deliberately small: it owns a number and nothing else. It does not decide when the day
/// ends or whether the player won — that is the day clock's job in step 6 — and it never
/// touches the UI, which subscribes instead.
///
/// It listens to the three <see cref="ServingSpot"/>s rather than to customers, because
/// customers are spawned at runtime and cannot be wired up in the inspector.
/// </summary>
public class RatingService : MonoBehaviour
{
    [Header("구독 대상")]
    [Tooltip("카운터의 레인 3개. 여기에 연결된 자리에서 나온 결과만 집계합니다.")]
    [SerializeField] private ServingSpot[] spots;

    [Header("평점")]
    [Tooltip("시작 평점.")]
    [SerializeField] private float startingRating = 1f;

    [Tooltip("평점 하한.")]
    [SerializeField] private float minRating;

    [Tooltip("평점 상한. 이 값에 도달하면 목표 달성입니다.")]
    [SerializeField] private float maxRating = 5f;

    [Header("증감 — 주문 메뉴 개수별")]
    [Tooltip("주문대로 다 만들어 줬을 때. 첫 칸이 1개짜리 주문, 둘째가 2개, 셋째가 3개입니다.")]
    [SerializeField] private float[] correctDeltas = { 0.2f, 0.3f, 0.4f };

    [Tooltip("주문에 없는 음식을 줬을 때. 개수가 많을수록 크게 깎입니다.")]
    [SerializeField] private float[] wrongDeltas = { -0.2f, -0.3f, -0.4f };

    [Tooltip("손님이 기다리다 떠났을 때.")]
    [SerializeField] private float[] abandonedDeltas = { -0.2f, -0.3f, -0.4f };

    /// <summary>Current rating, clamped between the configured bounds.</summary>
    public float Rating { get; private set; }

    /// <summary>Lowest possible rating, for UI bars.</summary>
    public float MinRating => minRating;

    /// <summary>Highest possible rating, for UI bars and the win check.</summary>
    public float MaxRating => maxRating;

    /// <summary>How many orders were served exactly right.</summary>
    public int CorrectCount { get; private set; }

    /// <summary>How many orders ended at all, correct or not.</summary>
    public int ResolvedCount { get; private set; }

    /// <summary>Rating as 0..1, for filling a bar.</summary>
    public float Normalized =>
        Mathf.Approximately(maxRating, minRating)
            ? 0f
            : Mathf.Clamp01((Rating - minRating) / (maxRating - minRating));

    /// <summary>Fires whenever the rating changes. Arguments are (new value, delta applied).</summary>
    public event Action<float, float> RatingChanged;

    /// <summary>
    /// Re-broadcast of every resolved order, so UI can subscribe to one object instead of
    /// hunting down all three lanes.
    /// </summary>
    public event Action<ServingSpot, OrderResult, float> OrderResolved;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        Rating = Mathf.Clamp(startingRating, minRating, maxRating);
    }

    private void OnEnable()
    {
        foreach (ServingSpot spot in Spots())
        {
            spot.OrderResolved += HandleOrderResolved;
        }
    }

    private void OnDisable()
    {
        foreach (ServingSpot spot in Spots())
        {
            spot.OrderResolved -= HandleOrderResolved;
        }
    }

    private void Start()
    {
        // Let the UI draw the starting value without special-casing its first frame.
        RatingChanged?.Invoke(Rating, 0f);
    }

    // ---------------------------------------------------------------- scoring

    private void HandleOrderResolved(ServingSpot spot, OrderResult result, int orderSize)
    {
        float delta = DeltaFor(result, orderSize);

        ResolvedCount++;
        if (result == OrderResult.Correct)
        {
            CorrectCount++;
        }

        if (!Mathf.Approximately(delta, 0f))
        {
            Rating = Mathf.Clamp(Rating + delta, minRating, maxRating);
            RatingChanged?.Invoke(Rating, delta);
        }

        OrderResolved?.Invoke(spot, result, delta);
    }

    private float DeltaFor(OrderResult result, int orderSize)
    {
        switch (result)
        {
            case OrderResult.Correct:
                return Pick(correctDeltas, orderSize);
            case OrderResult.Wrong:
                return Pick(wrongDeltas, orderSize);
            case OrderResult.Abandoned:
                return Pick(abandonedDeltas, orderSize);
            default:
                return 0f;
        }
    }

    /// <summary>개수에 해당하는 값. 표가 짧으면 마지막 칸으로 버틴다.</summary>
    private static float Pick(float[] table, int orderSize)
    {
        if (table == null || table.Length == 0)
        {
            return 0f;
        }

        int index = Mathf.Clamp(orderSize - 1, 0, table.Length - 1);
        return table[index];
    }

    private ServingSpot[] Spots()
    {
        return spots ?? Array.Empty<ServingSpot>();
    }

    private void OnValidate()
    {
        maxRating = Mathf.Max(minRating + 0.1f, maxRating);
        startingRating = Mathf.Clamp(startingRating, minRating, maxRating);
    }
}
