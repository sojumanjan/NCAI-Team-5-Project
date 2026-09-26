// 허브 오브젝트에 마우스가 올라오면 보이는 그림(과 그 위 호버 외곽선)을 살짝 키우는 컴포넌트
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 호버 외곽선(UniformImageHoverOutline)과 같은 조건으로 커진다 — 버튼이 눌릴 수 있을 때만.
/// 외곽선은 그림 자신의 머티리얼로 그려지므로 그림을 키우면 선도 같이 커진다.
///
/// 버튼 루트가 아니라 그림들만 키우는 이유: 루트는 클리어 연출(HubClearReveal)이 크기를 쥐고 흔들어서,
/// 여기서도 건드리면 서로 크기를 덮어쓴다.
/// </summary>
[RequireComponent(typeof(Button))]
public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("같이 커질 그림들. 클리어 전/후 그림을 둘 다 넣어 두면 어느 쪽이 보이든 커집니다.")]
    [SerializeField] private RectTransform[] targets;

    [Tooltip("마우스가 올라왔을 때의 배율.")]
    [SerializeField] private float hoverScale = 1.1f;

    [Tooltip("목표 크기에 거의 닿기까지 걸리는 시간 (초).")]
    [SerializeField] private float responseTime = 0.1f;

    private Button _button;
    private Vector3[] _restScales;
    private bool _hovered;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _restScales = new Vector3[targets != null ? targets.Length : 0];
        for (int i = 0; i < _restScales.Length; i++)
        {
            _restScales[i] = targets[i] != null ? targets[i].localScale : Vector3.one;
        }
    }

    private void Update()
    {
        float factor = _hovered && _button.IsInteractable() ? hoverScale : 1f;
        // 옵션 창이 시간을 멈춘 동안에도 마우스를 떼면 원래 크기로 돌아와야 한다.
        float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, responseTime) * 3f);

        for (int i = 0; i < _restScales.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].localScale = Vector3.Lerp(targets[i].localScale, _restScales[i] * factor, t);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void OnDisable()
    {
        _hovered = false;

        for (int i = 0; i < _restScales.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].localScale = _restScales[i];
            }
        }
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(0.01f, hoverScale);
        responseTime = Mathf.Max(0.01f, responseTime);
    }
}
