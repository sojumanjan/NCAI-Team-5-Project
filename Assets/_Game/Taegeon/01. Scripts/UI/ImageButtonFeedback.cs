using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ImageButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    #region 참조 및 설정

    [SerializeField] private RectTransform visual;
    [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.04f;
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.98f;
    [SerializeField, Min(0.01f)] private float responseTime = 0.10f;
    private Button button;
    private Vector3 restScale;
    private bool hovered;
    private bool pressed;
    private float submitUntil;

    #endregion

    #region 효과 초기화 및 갱신

    /// <summary>
    /// 버튼 효과에 필요한 참조와 기본 크기를 준비합니다.
    /// </summary>
    private void Awake()
    {
        button = GetComponent<Button>();
        if (visual == null) visual = (RectTransform)transform;
        restScale = visual.localScale;
        var hitImage = GetComponent<Image>();
        if (hitImage != null && hitImage.sprite != null && hitImage.sprite.texture.isReadable)
            hitImage.alphaHitTestMinimumThreshold = 0.15f;
    }

    /// <summary>
    /// 버튼 입력 상태에 맞춰 시각 요소의 크기를 보간합니다.
    /// </summary>
    private void Update()
    {
        bool available = button != null && button.IsActive() && button.IsInteractable();
        float factor = available ? ((pressed || Time.unscaledTime < submitUntil) ? pressedScale : (hovered ? hoverScale : 1f)) : 1f;
        visual.localScale = Vector3.Lerp(visual.localScale, restScale * factor,
            1f - Mathf.Exp(-Time.unscaledDeltaTime / responseTime * 3f));
    }

    #endregion

    #region 입력 상태 정리

    /// <summary>
    /// 입력 상태와 버튼 크기를 초기화합니다.
    /// </summary>
    private void OnDisable()
    {
        hovered = pressed = false;
        submitUntil = 0f;
        if (visual != null) visual.localScale = restScale;
    }

    /// <summary>
    /// 창이 비활성화되면 남아 있는 입력 상태를 해제합니다.
    /// </summary>
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { hovered = pressed = false; submitUntil = 0f; }
    }

    #endregion

    #region 버튼 입력 이벤트

    /// <summary>
    /// 포인터가 버튼 위에 올라온 상태로 전환합니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    /// <summary>
    /// 포인터가 버튼을 벗어난 상태로 전환합니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    /// <summary>
    /// 누를 수 있는 버튼의 왼쪽 클릭을 감지합니다.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && button.IsInteractable()) pressed = true;
    }
    /// <summary>
    /// 왼쪽 클릭의 누름 상태를 해제합니다.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) pressed = false;
    }
    /// <summary>
    /// 키보드나 패드 선택 시 짧은 누름 효과를 표시합니다.
    /// </summary>
    public void OnSubmit(BaseEventData eventData)
    {
        if (button.IsInteractable()) submitUntil = Time.unscaledTime + 0.12f;
    }
    #endregion

}
