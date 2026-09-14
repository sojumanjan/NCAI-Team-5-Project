using UnityEngine;

/// <summary>
/// What kind of thing this is. Recipes only ever match on Ingredient and Dish;
/// Container exists for cups and dough that carry state but are not themselves food.
/// </summary>
public enum ItemCategory
{
    Ingredient,
    Dish,
    Container,
}

/// <summary>
/// Definition of one carryable thing — 원두, 우유, 아메리카노, and so on.
///
/// This is the single source of truth for an item's identity. Recipes compare these
/// assets by reference, so adding a new ingredient or menu item is a matter of creating
/// an asset, not editing code.
///
/// Create via: Assets > Create > Cooking > Item Data
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Cooking/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("정보")]
    [Tooltip("UI에 표시할 이름. 비워두면 에셋 파일 이름을 씁니다.")]
    [SerializeField] private string displayName;

    [Tooltip("재료 / 완성 요리 / 그릇. 레시피 판정에 쓰입니다.")]
    [SerializeField] private ItemCategory category = ItemCategory.Ingredient;

    [Header("표현")]
    [Tooltip("월드에 생성될 프리팹. WorldItem 컴포넌트가 붙어 있어야 합니다.")]
    [SerializeField] private GameObject worldPrefab;

    [Tooltip("주문표·UI용 아이콘 (선택).")]
    [SerializeField] private Sprite icon;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public ItemCategory Category => category;

    public GameObject WorldPrefab => worldPrefab;

    public Sprite Icon => icon;

    public override string ToString() => DisplayName;
}
