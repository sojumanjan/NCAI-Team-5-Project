/// <summary>
/// 요리 미니게임 결과 화면에 필요한 숫자들. 허브로 넘어가는 <see cref="MiniGameResult"/>와
/// 일부러 분리했다.
///
/// 평점이니 요구 평점이니 하는 건 이 장르에서만 뜻이 있는 말이다. 공유 구조체에 넣으면
/// 리듬게임이나 탈출 게임 담당자가 쓰지도 않을 필드를 0으로 채우게 된다. 공유되는 건
/// "깼는가"뿐이고, 나머지는 여기 남는다.
/// </summary>
public readonly struct CookingResult
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

    public CookingResult(bool cleared, float finalRating, float requiredRating,
                         int correctCount, int resolvedCount)
    {
        Cleared = cleared;
        FinalRating = finalRating;
        RequiredRating = requiredRating;
        CorrectCount = correctCount;
        ResolvedCount = resolvedCount;
    }

    /// <summary>오답과 이탈을 합친 횟수.</summary>
    public int FailedCount => ResolvedCount - CorrectCount;
}
