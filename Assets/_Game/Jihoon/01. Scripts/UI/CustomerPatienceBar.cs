using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The bar over a customer's head. Lives on the customer prefab and reads
/// <see cref="Customer.Patience01"/> — the value stays the customer's business, this is
/// only the view, the same split as RatingService/RatingUI and ServingSpot/OrderSlotUI.
///
/// Shown from the moment they reach the counter until they are served or walk out. It is
/// deliberately one unbroken timer across "waiting to order" and "waiting for the drink",
/// so accepting an order at 10% leaves the player genuinely scrambling.
/// </summary>
public class CustomerPatienceBar : MonoBehaviour
{
    /// <summary>머리 위 메뉴 한 칸. 손님이 1개만 시키면 나머지는 꺼진다.</summary>
    [Serializable]
    private class MenuSlot
    {
        [Tooltip("이 칸 전체. 쓰지 않을 때 꺼집니다.")]
        public GameObject root;

        [Tooltip("메뉴 아이콘.")]
        public Image icon;

        [Tooltip("메뉴 이름. (선택)")]
        public TMP_Text label;
    }

    [Header("참조")]
    [Tooltip("표시할 손님. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private Customer customer;

    [Tooltip("끄고 켤 Canvas. 비워두면 이 오브젝트의 Canvas를 씁니다.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("줄어드는 바. Image Type을 Filled로 두세요.")]
    [SerializeField] private Image fillImage;

    [Header("주문 메뉴")]
    [Tooltip("게이지 위에 뜰 메뉴 칸들. 위에서부터 순서대로 채웁니다.")]
    [SerializeField] private MenuSlot[] menuSlots;

    [Tooltip("Icon이 아직 없을 때 아이콘 자리에 칠할 색.")]
    [SerializeField] private Color iconPlaceholderColor = new Color(1f, 1f, 1f, 0.3f);

    [Tooltip("이미 나온 메뉴를 흐리게 만드는 정도. 0이면 그대로, 1이면 거의 안 보입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float servedFade = 0.75f;

    [Header("표시")]
    [Tooltip("인내심이 이 비율 아래일 때부터 보입니다. 1이면 도착하자마자 항상 표시.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float showBelow = 1f;

    [Tooltip("손님 원점 기준 높이 (m). 캡슐이면 머리 위가 1.3쯤입니다.")]
    [SerializeField] private float heightOffset = 1.3f;

    [Tooltip("여유 있을 때 색.")]
    [SerializeField] private Color calmColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("다 닳아갈 때 색.")]
    [SerializeField] private Color angryColor = new Color(0.95f, 0.35f, 0.35f);

    [Header("빌보드")]
    [Tooltip("항상 카메라를 향하게 합니다.")]
    [SerializeField] private bool billboard = true;

    private Camera _camera;

    private void Awake()
    {
        if (customer == null)
        {
            customer = GetComponentInParent<Customer>();
        }

        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (customer == null || canvas == null)
        {
            Debug.LogError($"{nameof(CustomerPatienceBar)} on '{name}': needs a {nameof(Customer)} " +
                           "in its parents and a Canvas on this object.", this);
            enabled = false;
            return;
        }

        SetVisible(false);
    }

    // LateUpdate so the bar is squared up after the camera has finished moving for the
    // frame; doing it in Update leaves it a frame behind and the bar visibly swims.
    private void LateUpdate()
    {
        float remaining = customer.Patience01;
        bool visible = customer.IsWaiting && remaining <= showBelow;

        SetVisible(visible);

        if (!visible)
        {
            return;
        }

        UpdateOrder();
        UpdateFill(remaining);
        UpdatePose();
    }

    /// <summary>
    /// 이 손님이 시킨 것들을 그린다. 이미 나온 메뉴는 지우지 않고 흐리게 남긴다 —
    /// 몇 개를 시켰고 몇 개가 남았는지가 한눈에 보여야 한다.
    /// </summary>
    private void UpdateOrder()
    {
        if (menuSlots == null)
        {
            return;
        }

        var orders = customer.Orders;

        for (int i = 0; i < menuSlots.Length; i++)
        {
            MenuSlot slot = menuSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool used = orders != null && i < orders.Count;

            if (slot.root != null)
            {
                slot.root.SetActive(used);
            }

            if (!used)
            {
                continue;
            }

            ItemData order = orders[i];
            float alpha = customer.IsServed(i) ? 1f - servedFade : 1f;

            if (slot.label != null)
            {
                slot.label.text = order != null ? order.DisplayName : string.Empty;
                slot.label.color = WithAlpha(slot.label.color, alpha);
            }

            if (slot.icon == null)
            {
                continue;
            }

            Sprite icon = order != null ? order.Icon : null;
            slot.icon.sprite = icon;

            // 그림이 아직 없으면 단색 블록으로 대신해서 자리와 크기를 확인할 수 있게 한다.
            Color color = icon != null ? Color.white : iconPlaceholderColor;
            slot.icon.color = WithAlpha(color, color.a * alpha);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void UpdateFill(float remaining)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = remaining;
        fillImage.color = Color.Lerp(angryColor, calmColor, remaining);
    }

    private void UpdatePose()
    {
        Transform anchor = customer.transform;
        transform.position = anchor.position + Vector3.up * heightOffset;

        if (!billboard)
        {
            return;
        }

        Camera cam = ResolveCamera();
        if (cam == null)
        {
            return;
        }

        // Match the camera's facing rather than looking at it. LookAt tilts the bar when
        // the player stands close and looks up, which reads as broken in first person.
        transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    private Camera ResolveCamera()
    {
        // Camera.main is a tagged search, so it is cached — but re-resolved if the camera
        // is destroyed, which happens when a scene unloads underneath a pooled customer.
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        return _camera;
    }

    /// <summary>
    /// Toggles the Canvas rather than the GameObject. Deactivating this object would stop
    /// LateUpdate, and the bar could then never turn itself back on.
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (canvas != null && canvas.enabled != visible)
        {
            canvas.enabled = visible;
        }
    }

    private void OnValidate()
    {
        heightOffset = Mathf.Max(0f, heightOffset);
    }
}
