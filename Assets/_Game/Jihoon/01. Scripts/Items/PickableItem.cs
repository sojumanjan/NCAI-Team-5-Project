using UnityEngine;

/// <summary>
/// 좌클릭으로 집히는 물건. 바닥이나 조리대에 놓인 재료·완성품에 붙인다.
///
/// <see cref="WorldItem"/>에서 이 역할만 떼어낸 이유는 디스펜서 때문이다. 재료통도 옮길 수
/// 있어야 하니 WorldItem이 필요한데, 통의 좌클릭은 통을 집는 게 아니라 내용물을 꺼내는
/// 것이다. WorldItem이 소스까지 겸하면 통 하나에 <see cref="IItemSource"/>가 둘이 되고,
/// 조준 판정은 그중 하나만 돌려주므로 어느 쪽이 걸릴지 컴포넌트 순서에 달리게 된다.
///
/// 그래서 규칙은 이렇게 갈린다 — 집히는 물건에는 이걸 붙이고, 통에는 붙이지 않는다.
/// </summary>
[RequireComponent(typeof(WorldItem))]
public class PickableItem : MonoBehaviour, IItemSource
{
    private WorldItem _worldItem;

    private void Awake()
    {
        _worldItem = GetComponent<WorldItem>();
    }

    public ItemData ProvidedItem => _worldItem != null ? _worldItem.Item : null;

    public bool CanProvide(PlayerHands hands)
    {
        return _worldItem != null && _worldItem.Item != null && !_worldItem.IsCarried;
    }

    /// <summary>새로 만들지 않고 자기 자신을 넘긴다. 월드에 이미 존재하는 물건이므로.</summary>
    public GameObject Provide(PlayerHands hands) => gameObject;
}
