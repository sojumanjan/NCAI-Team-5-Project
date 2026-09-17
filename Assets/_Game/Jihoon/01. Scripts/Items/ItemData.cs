using UnityEngine;

/// <summary>
/// 이게 어떤 종류인지. 레시피 판정에는 Ingredient와 Dish만 쓰이고, Container는 컵이나
/// 반죽처럼 상태는 지니지만 그 자체로는 음식이 아닌 것들을 위해 있다.
/// </summary>
public enum ItemCategory
{
    Ingredient,
    Dish,
    Container,
}

/// <summary>
/// 들고 다닐 수 있는 것 하나의 정의 — 원두, 우유, 아메리카노 같은.
///
/// 아이템의 정체성은 오직 여기에 있다. 레시피는 이 에셋을 참조로 비교하므로, 재료나
/// 메뉴를 추가하는 일은 코드 수정이 아니라 에셋 생성이다.
///
/// 생성: Assets > Create > Cooking > Item Data
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
