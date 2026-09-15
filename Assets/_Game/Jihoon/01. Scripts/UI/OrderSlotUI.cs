using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row of the order board: what the customer at this lane wants.
/// Bind one per <see cref="ServingSpot"/> — three rows, three lanes.
///
/// Without this the game is unplayable, since nothing else tells the player what to cook.
/// </summary>
public class OrderSlotUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("이 줄이 보여줄 레인.")]
    [SerializeField] private ServingSpot spot;

    [Header("구성")]
    [Tooltip("이 줄 전체. 손님이 없으면 꺼집니다. 비워두면 이 오브젝트를 켜고 끕니다.")]
    [SerializeField] private GameObject root;

    [Tooltip("메뉴 이름.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("메뉴 아이콘. ItemData의 Icon을 씁니다. (선택)")]
    [SerializeField] private Image iconImage;

    [Header("상태 표시")]
    [Tooltip("주문 수락 전에 켜질 표시. \"포스기에서 수락\" 같은 것. (선택)")]
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
    // down and the row could never come back.
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
    /// The gauge moves every frame, so it is polled rather than event-driven — three rows
    /// reading one float is cheaper than the subscription plumbing would be.
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

    private void Show(ItemData order, bool accepted)
    {
        Target.SetActive(true);

        if (nameText != null)
        {
            nameText.text = order != null ? order.DisplayName : "???";
        }

        if (iconImage != null)
        {
            Sprite icon = order != null ? order.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (awaitingBadge != null)
        {
            awaitingBadge.SetActive(!accepted);
        }

        if (acceptedBadge != null)
        {
            acceptedBadge.SetActive(accepted);
        }
    }

    private void Clear()
    {
        Target.SetActive(false);
    }

    private GameObject Target => root != null ? root : gameObject;
}
