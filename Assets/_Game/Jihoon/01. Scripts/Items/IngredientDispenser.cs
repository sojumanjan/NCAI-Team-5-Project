using UnityEngine;

/// <summary>
/// 재료 하나를 무한히 내주는 곳 — 커피머신 옆 원두통, 반죽대 위 밀가루, 냉장고의 각 칸.
/// 조준하고 좌클릭하면 새 사본이 손에 들어온다. 디스펜서 자체는 절대 들리지 않는다.
///
/// 하나가 재료 하나만 담당하므로, 냉장고는 메뉴를 가진 오브젝트 하나가 아니라 이것 여러
/// 개다. 그래야 "재료 보관 위치"가 코드가 아니라 오브젝트 배치 문제로 남는다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IngredientDispenser : MonoBehaviour, IItemSource
{
    [Header("보관 재료")]
    [Tooltip("여기서 꺼낼 재료.")]
    [SerializeField] private ItemData item;

    public ItemData ProvidedItem => item;

    private void Awake()
    {
        if (item == null)
        {
            Debug.LogError($"'{name}'의 {nameof(IngredientDispenser)}에 ItemData가 없습니다.", this);
        }
        else if (item.WorldPrefab == null)
        {
            Debug.LogError($"ItemData '{item.name}'에 World Prefab이 없어 '{name}'이 꺼낼 수 없습니다.", this);
        }
    }

    public bool CanProvide(PlayerHands hands) => item != null && item.WorldPrefab != null;

    public GameObject Provide(PlayerHands hands)
    {
        if (!CanProvide(hands))
        {
            return null;
        }

        // 손 위치에서 바로 만든다. 어차피 같은 프레임에 손이 부모로 잡고 위치를 덮어쓰므로
        // 다른 곳에서 만들 이유가 없고, 냉장고 안쪽에서 만들면 콜라이더가 한 프레임 동안
        // 살아 있어 엉뚱한 겹침이 잡힌다.
        Transform at = hands != null && hands.HoldAnchor != null ? hands.HoldAnchor : transform;

        GameObject spawned = Instantiate(item.WorldPrefab, at.position, at.rotation);
        spawned.name = item.DisplayName;

        return spawned;
    }
}
