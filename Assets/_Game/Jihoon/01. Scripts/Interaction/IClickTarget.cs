/// <summary>
/// 손에 아무것도 들어오거나 나가지 않는 순수한 좌클릭.
///
/// 좌클릭 동사 셋 중 마지막이다. <see cref="IItemSource"/>는 뭔가를 내주고,
/// <see cref="IItemReceiver"/>는 가져가며, 이건 클릭됐다는 사실만 알린다 — 포스기에서
/// 주문을 받거나, 스위치를 내리거나, 벨을 누르는 것.
///
/// <see cref="PlayerHands"/>는 아이템 동사들이 거절한 뒤에야 이걸 시도한다. 그래서 한
/// 물체가 둘 다 구현해도 안전하다 (음식을 내주면서, 빈손일 땐 클릭에 반응하는 스테이션처럼).
/// </summary>
public interface IClickTarget
{
    /// <summary>
    /// 프롬프트에 띄울 문구. 예: "주문 수락". 나머지 두 좌클릭 동사는 관련된 아이템에서
    /// 문구를 만들어낼 수 있지만, 순수 클릭은 무엇이든 될 수 있어서 스스로 말해야 한다.
    /// </summary>
    string ClickPrompt { get; }

    /// <summary>지금 클릭해도 아무 일이 없으면 false.</summary>
    bool CanClick(PlayerHands hands);

    /// <summary>클릭됐다. 손은 그대로다.</summary>
    void OnClick(PlayerHands hands);
}
