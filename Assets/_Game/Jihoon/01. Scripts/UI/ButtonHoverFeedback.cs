using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스를 올리면 버튼이 살짝 커진다. 버튼 오브젝트에 붙인다.
///
/// 어두워지는 쪽은 여기서 안 건드린다. 버튼에 이미 Color Tint 트랜지션이 있어서,
/// Highlighted Color만 내리면 같은 일을 한다. 색을 두 군데서 만지면 어느 쪽이 이겼는지
/// 나중에 아무도 모른다.
///
/// 트윈을 언스케일드로 돌리는 이유: UI 반응은 게임 시간과 상관이 없어야 한다. 결과 화면이
/// 시간을 멈추든 말든, 일시정지를 붙이든, 버튼은 똑같이 반응해야 한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("크기")]
    [Tooltip("마우스를 올렸을 때의 배율.")]
    [SerializeField] private float hoverScale = 1.15f;

    [Tooltip("커지고 작아지는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 0.12f;

    [Tooltip("가속 곡선.")]
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("대상")]
    [Tooltip("누를 수 없는 버튼은 반응하지 않게 합니다. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private Selectable target;

    private Vector3 _baseScale = Vector3.one;
    private Tween _tween;

    private void Awake()
    {
        _baseScale = transform.localScale;

        if (target == null)
        {
            target = GetComponent<Selectable>();
        }
    }

    private void OnDisable()
    {
        // 커지는 도중에 패널이 꺼지면 1.15배로 굳은 채 다시 켜진다.
        _tween?.Kill();
        _tween = null;
        transform.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target != null && !target.IsInteractable())
        {
            return;
        }

        ScaleTo(_baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData) => ScaleTo(_baseScale);

    private void ScaleTo(Vector3 scale)
    {
        _tween?.Kill();
        _tween = transform.DOScale(scale, duration)
                          .SetEase(ease)
                          .SetUpdate(true)
                          .SetLink(gameObject);
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(0.1f, hoverScale);
        duration = Mathf.Max(0.01f, duration);
    }
}
