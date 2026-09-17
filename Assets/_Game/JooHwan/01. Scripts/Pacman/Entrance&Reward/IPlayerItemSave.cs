/// <summary>
/// 플레이어 아이템 획득 정보 저장을 담당하는 외부 스크립트(PlayerData 등)가 구현할 인터페이스.
/// 이 미니게임 쪽은 표시만 담당하고, 실제 저장/조회는 이 인터페이스를 통해 위임한다.
/// </summary>
public interface IPlayerItemSave
{
    bool HasItem(string itemId);
    void SetItemGain(string itemId);
}
