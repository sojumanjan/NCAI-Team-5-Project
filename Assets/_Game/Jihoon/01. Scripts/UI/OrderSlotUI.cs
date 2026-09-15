using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One column of the menu board on the wall: what the customer at this lane ordered, and
/// what it is made of.
///
/// The ingredient list is looked up from the recipe rather than authored here, so it can
/// never disagree with what the station actually wants. Add an ingredient to a recipe and
/// the board shows it.
///
/// Bind one per <see cref="ServingSpot"/>.
/// </summary>
public class OrderSlotUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("이 줄이 보여줄 레인.")]
    [SerializeField] private ServingSpot spot;

    [Tooltip("주문한 메뉴의 레시피를 찾는 데 씁니다.")]
    [SerializeField] private RecipeBook recipeBook;

    [Header("구성")]
    [Tooltip("이 칸 전체. 손님이 없으면 꺼집니다. 비워두면 이 오브젝트를 켜고 끕니다.")]
    [SerializeField] private GameObject root;

    [Tooltip("완성품 아이콘.")]
    [SerializeField] private Image dishIcon;

    [Tooltip("메뉴 이름.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("재료 아이콘 슬롯. 레시피 재료 수만큼 위에서부터 채우고 나머지는 끕니다.")]
    [SerializeField] private Image[] ingredientIcons;

    [Header("아이콘 없을 때")]
    [Tooltip("ItemData에 Icon이 아직 없을 때 대신 칠할 색. 자리를 눈으로 확인하는 용도입니다.")]
    [SerializeField] private Color placeholderColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("상태 표시")]
    [Tooltip("주문 수락 전에 켜질 표시. (선택)")]
    [SerializeField] private GameObject awaitingBadge;

    [Tooltip("수락한 뒤 켜질 표시. (선택)")]
    [SerializeField] private GameObject acceptedBadge;

    [Header("인내심 게이지")]
    [Tooltip("줄어드는 바. Image Type을 Filled로 두세요. (선택)")]
    [SerializeField] private Image patienceFill;

    [Tooltip("여유 있을 때 색.")]
    [SerializeField] private Color patienceCalmColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("다 닳아갈 때 색.")]
    [SerializeField] private Color patienceAngryColor = new Color(0.95f, 0.35f, 0.35f);

    // Subscribed in Awake rather than OnEnable on purpose: an empty slot hides itself, and
    // if that means deactivating this very object, OnDisable would tear the subscription
    // down and the column could never come back.
    private void Awake()
    {
        if (spot == null)
        {
            Debug.LogError($"{nameof(OrderSlotUI)} on '{name}': Spot is not assigned.", this);
            enabled = false;
            return;
        }

        spot.OrderPlaced += HandlePlaced;
        spot.OrderAccepted += HandleAccepted;
        spot.OrderResolved += HandleResolved;

        Clear();
    }

    private void OnDestroy()
    {
        if (spot == null)
        {
            return;
        }

        spot.OrderPlaced -= HandlePlaced;
        spot.OrderAccepted -= HandleAccepted;
        spot.OrderResolved -= HandleResolved;
    }

    /// <summary>
    /// The gauge moves every frame, so it is polled rather than event-driven — three
    /// columns reading one float is cheaper than the subscription plumbing would be.
    /// </summary>
    private void Update()
    {
        if (patienceFill == null || !Target.activeSelf)
        {
            return;
        }

        float remaining = spot.IsCustomerWaiting ? spot.Patience01 : 1f;
        patienceFill.fillAmount = remaining;
        patienceFill.color = Color.Lerp(patienceAngryColor, patienceCalmColor, remaining);
    }

    private void HandlePlaced(ServingSpot _, ItemData order) => Show(order, accepted: false);

    private void HandleAccepted(ServingSpot _, ItemData order) => Show(order, accepted: true);

    private void HandleResolved(ServingSpot _, OrderResult __) => Clear();

    // ---------------------------------------------------------------- drawing

    private void Show(ItemData order, bool accepted)
    {
        Target.SetActive(true);

        if (nameText != null)
        {
            nameText.text = order != null ? order.DisplayName : "???";
        }

        ApplyIcon(dishIcon, order);
        ShowIngredients(order);

        if (awaitingBadge != null)
        {
            awaitingBadge.SetActive(!accepted);
        }

        if (acceptedBadge != null)
        {
            acceptedBadge.SetActive(accepted);
        }
    }

    /// <summary>Fills the ingredient column from the recipe that makes this dish.</summary>
    private void ShowIngredients(ItemData order)
    {
        if (ingredientIcons == null || ingredientIcons.Length == 0)
        {
            return;
        }

        RecipeData recipe = recipeBook != null ? recipeBook.FindByOutput(order) : null;
        int count = recipe != null ? recipe.InputCount : 0;

        for (int i = 0; i < ingredientIcons.Length; i++)
        {
            Image slot = ingredientIcons[i];
            if (slot == null)
            {
                continue;
            }

            bool used = i < count;
            slot.gameObject.SetActive(used);

            if (used)
            {
                ApplyIcon(slot, recipe.Inputs[i]);
            }
        }
    }

    /// <summary>
    /// Shows an item's icon, or a flat placeholder block while the art is still missing —
    /// so the layout can be judged before any sprite exists.
    /// </summary>
    private void ApplyIcon(Image image, ItemData item)
    {
        if (image == null)
        {
            return;
        }

        Sprite icon = item != null ? item.Icon : null;
        image.sprite = icon;
        image.color = icon != null ? Color.white : placeholderColor;
    }

    private void Clear()
    {
        Target.SetActive(false);
    }

    private GameObject Target => root != null ? root : gameObject;
}
