using UnityEngine;

/// <summary>
/// 미니게임 한 판이 끝났을 때 허브로 넘어오는 값. 장르가 넷 다 달라서 일부러 최소한만 담는다.
///
/// 평점·콤보·남은 시간 같은 건 여기 넣지 않는다. 리듬게임에 "요구 평점"이 무슨 의미겠는가.
/// 그런 값은 각자 씬 안에서만 쓰고, 공유되는 건 "깼는가"와 "얼마나 잘 깼는가" 둘뿐이다.
///
/// 에셋이 아니다. 게임이 끝나는 순간 값으로 만들어 넘기면 된다.
/// </summary>
public readonly struct MiniGameResult
{
    /// <summary>클리어 여부. 씨앗이 자라는 기준이자 이 구조체의 존재 이유.</summary>
    public readonly bool Cleared;

    /// <summary>얼마나 잘했는지 0~1. 성장 연출 세기 같은 데 쓸 수 있고, 안 쓰면 0이어도 된다.</summary>
    public readonly float Score01;

    public MiniGameResult(bool cleared, float score01 = 0f)
    {
        Cleared = cleared;
        Score01 = Mathf.Clamp01(score01);
    }

    public override string ToString() => $"{(Cleared ? "클리어" : "실패")} ({Score01:0.00})";
}
