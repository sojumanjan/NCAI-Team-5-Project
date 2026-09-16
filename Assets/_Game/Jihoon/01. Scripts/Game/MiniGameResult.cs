/// <summary>
/// 미니게임 한 판의 결과.
///
/// 허브와 주고받을 유일한 창구다. 9단계에서 허브 연동을 붙일 때 이 구조체만 넘기면 되도록,
/// 다른 시스템이 결과를 직접 캐묻지 않게 여기로 모아둔다.
/// </summary>
public readonly struct MiniGameResult
{
    /// <summary>목표 평점을 넘겼는지.</summary>
    public readonly bool Cleared;

    /// <summary>영업 종료 시점의 평점.</summary>
    public readonly float FinalRating;

    /// <summary>클리어 기준선. UI가 "4.0 / 5.0" 식으로 보여줄 때 쓴다.</summary>
    public readonly float RequiredRating;

    /// <summary>주문대로 만들어 준 횟수.</summary>
    public readonly int CorrectCount;

    /// <summary>끝난 주문 전체. 오답과 이탈을 포함한다.</summary>
    public readonly int ResolvedCount;

    /// <summary>허브로 가져갈 비료.</summary>
    public readonly int Fertilizer;

    public MiniGameResult(bool cleared, float finalRating, float requiredRating,
                          int correctCount, int resolvedCount, int fertilizer)
    {
        Cleared = cleared;
        FinalRating = finalRating;
        RequiredRating = requiredRating;
        CorrectCount = correctCount;
        ResolvedCount = resolvedCount;
        Fertilizer = fertilizer;
    }

    /// <summary>오답과 이탈을 합친 횟수.</summary>
    public int FailedCount => ResolvedCount - CorrectCount;
}
