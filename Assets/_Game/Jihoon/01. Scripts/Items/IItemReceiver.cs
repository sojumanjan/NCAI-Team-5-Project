/// <summary>
/// 플레이어의 든 손에서 아이템을 받아가는 것 (좌클릭). <see cref="IItemSource"/>의 거울상.
///
/// 이게 없으면 손에 뭘 든 채 좌클릭하는 건 "바닥에 내려놓기"밖에 될 수 없어서, 원두를
/// 커피머신에 넣는 일 자체가 불가능하다. 스테이션이 재료를 받으려고 구현하고, 서빙
/// 카운터가 완성품을 받으려고 구현한다.
/// </summary>
public interface IItemReceiver
{
    /// <summary>
    /// 지금 이 아이템을 받을 수 있는지. 실제로 가져가기 전에 묻기 때문에, 조용히 떨어뜨리는
    /// 대신 거절을 표시해 줄 수 있다.
    /// </summary>
    bool CanReceive(ItemData item, PlayerHands hands);

    /// <summary>
    /// 아이템을 가져간다. 이 시점에 손은 이미 놓았으므로, 파괴하든 보관하든 받은 쪽 책임이다.
    /// </summary>
    void Receive(WorldItem item, PlayerHands hands);
}
