/// <summary>
/// 받는 쪽이 프롬프트 문구를 직접 정하고 싶을 때 <see cref="IItemReceiver"/>와 함께 단다.
///
/// 기본 문구는 "○○ 넣기"인데, 이게 맞는 건 기구뿐이다. 쓰레기통은 버리는 것이고 재료통은
/// 되돌려 놓는 것이라, 같은 말을 쓰면 플레이어가 무슨 일이 일어날지 잘못 짐작한다.
///
/// 선택 사항이다. 달지 않으면 예전처럼 기본 문구가 나온다.
/// </summary>
public interface IReceiverPrompt
{
    /// <summary>이 대상에 넣을 때 보여줄 문구. 비어 있으면 기본 문구를 씁니다.</summary>
    string ReceivePrompt { get; }
}
