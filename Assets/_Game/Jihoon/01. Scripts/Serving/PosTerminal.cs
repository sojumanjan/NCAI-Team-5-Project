using UnityEngine;

/// <summary>
/// The POS terminal for one lane. Left click it to take the waiting customer's order.
///
/// Nothing enters the hands, so this is an <see cref="IClickTarget"/> rather than an item
/// verb. It holds no state of its own — <see cref="ServingSpot"/> owns that, and the
/// colour here just mirrors the lane's events.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PosTerminal : MonoBehaviour, IClickTarget
{
    [Header("문구")]
    [Tooltip("조준했을 때 화면에 뜰 문구.")]
    [SerializeField] private string clickPrompt = "주문 수락";

    [Header("색 피드백")]
    [Tooltip("색을 바꿀 렌더러. 비워두면 자기 자신에서 찾습니다.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("손님이 없을 때.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("주문이 들어와서 수락을 기다릴 때.")]
    [SerializeField] private Color awaitingColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("주문을 수락해서 조리해야 할 때.")]
    [SerializeField] private Color acceptedColor = new Color(0.4f, 0.7f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;
    private ServingSpot _spot;

    public string ClickPrompt => clickPrompt;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        _block = new MaterialPropertyBlock();
        ApplyColor(idleColor);
    }

    /// <summary>Called by the owning spot during Awake.</summary>
    public void Bind(ServingSpot spot)
    {
        _spot = spot;
        _spot.OrderPlaced += (_, __) => ApplyColor(awaitingColor);
        _spot.OrderAccepted += (_, __) => ApplyColor(acceptedColor);
        _spot.OrderResolved += (_, __) => ApplyColor(idleColor);
    }

    public bool CanClick(PlayerHands hands) => _spot != null && _spot.CanAcceptOrder();

    public void OnClick(PlayerHands hands) => _spot.AcceptOrder();

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null || _block == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_block);
    }
}
