using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벽 메뉴판의 한 레인. 그 자리 손님이 시킨 메뉴 1~3개와 각각의 재료를 보여준다.
///
/// 재료 목록은 여기에 적지 않고 레시피에서 끌어온다. 기구가 실제로 원하는 것과 어긋날 수가
/// 없고, 레시피에 재료를 추가하면 메뉴판도 따라온다.
///
/// <see cref="ServingSpot"/> 하나당 하나씩 붙인다.
/// </summary>
public class OrderSlotUI : MonoBehaviour
{
    /// <summary>메뉴 한 칸. 손님이 1개만 시키면 나머지 칸은 꺼진다.</summary>
    [Serializable]
    private class MenuColumn
    {
        [Tooltip("이 칸 전체. 쓰지 않을 때 꺼집니다.")]
        public GameObject root;

        [Tooltip("완성품 아이콘.")]
        public Image dishIcon;

        [Tooltip("메뉴 이름.")]
        public TMP_Text nameText;

        [Tooltip("재료 아이콘. 레시피 재료 수만큼 위에서부터 채우고 나머지는 끕니다.")]
        public Image[] ingredientIcons;
    }

    [Header("참조")]
    [Tooltip("이 줄이 보여줄 레인.")]
    [SerializeField] private ServingSpot spot;

    [Tooltip("주문한 메뉴의 레시피를 찾는 데 씁니다.")]
    [SerializeField] private RecipeBook recipeBook;

    [Header("구성")]
    [Tooltip("이 칸 전체. 손님이 없으면 꺼집니다. 비워두면 이 오브젝트를 켜고 끕니다.")]
    [SerializeField] private GameObject root;

    [Tooltip("메뉴 칸들. 위에서부터 순서대로 채웁니다.")]
    [SerializeField] private MenuColumn[] menus;

    [Header("아이콘 없을 때")]
    [Tooltip("ItemData에 Icon이 아직 없을 때 대신 칠할 색. 자리를 눈으로 확인하는 용도입니다.")]
    [SerializeField] private Color placeholderColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("이미 낸 메뉴")]
    [Tooltip("나온 메뉴를 흐리게 만드는 정도. 0이면 그대로, 1이면 거의 안 보입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float servedFade = 0.75f;

    [Header("인내심 게이지")]
    [Tooltip("줄어드는 바. Image Type을 Filled로 두세요. (선택)")]
    [SerializeField] private Image patienceFill;

    [Tooltip("여유 있을 때 색.")]
    [SerializeField] private Color patienceCalmColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("다 닳아갈 때 색.")]
    [SerializeField] private Color patienceAngryColor = new Color(0.95f, 0.35f, 0.35f);

    // 빈 칸은 자기 자신을 끄기도 한다. OnEnable에 구독을 걸면 그 순간 구독이 끊겨
    // 다시는 켜지지 못하므로 Awake/OnDestroy를 쓴다.
    private void Awake()
    {
        if (spot == null)
        {
            Debug.LogError($"{nameof(OrderSlotUI)} on '{name}': Spot is not assigned.", this);
            enabled = false;
            return;
        }

        spot.OrderPlaced += HandlePlaced;
        spot.OrderProgress += HandleProgress;
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
        spot.OrderProgress -= HandleProgress;
        spot.OrderResolved -= HandleResolved;
    }

    /// <summary>
    /// 게이지는 매 프레임 움직이므로 이벤트 대신 폴링한다. 세 줄이 float 하나씩 읽는 쪽이
    /// 구독 배선보다 싸다.
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

    private void HandlePlaced(ServingSpot _, System.Collections.Generic.IReadOnlyList<ItemData> orders) => Show();

    private void HandleProgress(ServingSpot _) => Show();

    private void HandleResolved(ServingSpot _, OrderResult __, int ___) => Clear();

    // ---------------------------------------------------------------- 그리기

    private void Show()
    {
        Target.SetActive(true);

        var orders = spot.CurrentOrders;

        if (menus != null)
        {
            for (int i = 0; i < menus.Length; i++)
            {
                MenuColumn column = menus[i];
                if (column == null)
                {
                    continue;
                }

                bool used = orders != null && i < orders.Count;

                if (column.root != null)
                {
                    column.root.SetActive(used);
                }

                if (used)
                {
                    DrawColumn(column, orders[i], spot.IsServed(i));
                }
            }
        }
    }

    private void DrawColumn(MenuColumn column, ItemData order, bool served)
    {
        // 이미 낸 메뉴는 지우지 않고 흐리게 남긴다. 무엇을 시켰는지는 계속 보여야 한다.
        float alpha = served ? 1f - servedFade : 1f;

        if (column.nameText != null)
        {
            column.nameText.text = order != null ? order.DisplayName : "???";
            column.nameText.color = WithAlpha(column.nameText.color, alpha);
        }

        ApplyIcon(column.dishIcon, order, alpha);
        ShowIngredients(column, order, alpha);
    }

    /// <summary>이 메뉴를 만드는 레시피에서 재료 칸을 채운다.</summary>
    private void ShowIngredients(MenuColumn column, ItemData order, float alpha)
    {
        if (column.ingredientIcons == null || column.ingredientIcons.Length == 0)
        {
            return;
        }

        RecipeData recipe = recipeBook != null ? recipeBook.FindByOutput(order) : null;
        int count = recipe != null ? recipe.InputCount : 0;

        for (int i = 0; i < column.ingredientIcons.Length; i++)
        {
            Image slot = column.ingredientIcons[i];
            if (slot == null)
            {
                continue;
            }

            bool used = i < count;
            slot.gameObject.SetActive(used);

            if (used)
            {
                ApplyIcon(slot, recipe.Inputs[i], alpha);
            }
        }
    }

    /// <summary>
    /// 아이콘을 넣는다. 스프라이트가 아직 없으면 단색 블록으로 대신해서, 그림이 없어도
    /// 배치를 판단할 수 있게 한다.
    /// </summary>
    private void ApplyIcon(Image image, ItemData item, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Sprite icon = item != null ? item.Icon : null;
        image.sprite = icon;

        Color color = icon != null ? Color.white : placeholderColor;
        image.color = WithAlpha(color, color.a * alpha);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void Clear()
    {
        Target.SetActive(false);
    }

    private GameObject Target => root != null ? root : gameObject;
}
