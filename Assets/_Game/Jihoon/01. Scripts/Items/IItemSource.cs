using UnityEngine;

/// <summary>
/// 플레이어의 빈 손에 아이템을 넘겨줄 수 있는 것 (좌클릭).
///
/// 이름을 "집힌다"가 아니라 "내준다"로 지은 건 일부러다. 구현체 대부분은 자기 자신이
/// 들리지 않는다. 테이블에 놓인 <see cref="PickableItem"/>은 마침 자기를 넘길 뿐이고,
/// <see cref="IngredientDispenser"/>는 새로 만들어 넘기며, <see cref="StationBase"/>는
/// 방금 완성한 음식을 넘긴다. 플레이어의 손은 셋을 구분하지 못한다.
///
/// 반대 방향인 <see cref="IItemReceiver"/>와 짝이다.
/// </summary>
public interface IItemSource
{
    /// <summary>손에 들어올 것. 집기 전에 프롬프트를 만들 때 쓴다.</summary>
    ItemData ProvidedItem { get; }

    /// <summary>지금 내줄 게 없으면 false.</summary>
    bool CanProvide(PlayerHands hands);

    /// <summary>
    /// 넘겨준다. 손에 들어갈 GameObject를 반환한다 — 자기 자신이든, 새로 만든 인스턴스든.
    /// 거절하려면 null.
    /// </summary>
    GameObject Provide(PlayerHands hands);
}
