using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ImageButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    [SerializeField] private RectTransform visual;
    [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.04f;
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.98f;
    [SerializeField, Min(0.01f)] private float responseTime = 0.10f;
    private Button button;
    private Vector3 restScale;
    private bool hovered;
    private bool pressed;
    private float submitUntil;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (visual == null) visual = (RectTransform)transform;
        restScale = visual.localScale;
        var hitImage = GetComponent<Image>();
        if (hitImage != null && hitImage.sprite != null && hitImage.sprite.texture.isReadable)
            hitImage.alphaHitTestMinimumThreshold = 0.15f;
    }

    private void Update()
    {
        bool available = button != null && button.IsActive() && button.IsInteractable();
        float factor = available ? ((pressed || Time.unscaledTime < submitUntil) ? pressedScale : (hovered ? hoverScale : 1f)) : 1f;
        visual.localScale = Vector3.Lerp(visual.localScale, restScale * factor,
            1f - Mathf.Exp(-Time.unscaledDeltaTime / responseTime * 3f));
    }

    private void OnDisable()
    {
        hovered = pressed = false;
        submitUntil = 0f;
        if (visual != null) visual.localScale = restScale;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { hovered = pressed = false; submitUntil = 0f; }
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && button.IsInteractable()) pressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) pressed = false;
    }
    public void OnSubmit(BaseEventData eventData)
    {
        if (button.IsInteractable()) submitUntil = Time.unscaledTime + 0.12f;
    }
}
