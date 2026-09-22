using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스를 따라 UI 조각이 살짝 밀린다. 움직이고 싶은 RectTransform마다 하나씩 붙인다.
///
/// 얼마나 밀지를 픽셀로 적어두지 않는 것이 이 스크립트의 요점이다. 배경처럼 캔버스보다
/// 큰 그림은 <b>넘치는 만큼</b>이 곧 움직일 수 있는 전부라서, 크기에서 직접 뽑아내면
/// 나중에 배경 스케일을 바꿔도 안쪽이 드러나는 일이 구조적으로 생기지 않는다.
/// 숫자를 인스펙터에 적어두면 스케일을 건드린 날 바로 어긋난다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MouseParallax : MonoBehaviour
{
    /// <summary>움직일 수 있는 범위를 무엇으로 정할지.</summary>
    public enum Limit
    {
        /// <summary>부모보다 넘치는 만큼. 배경처럼 화면을 덮어야 하는 것에 씁니다.</summary>
        CoverParent,

        /// <summary>아래에 적은 픽셀만큼. 부모보다 작은 조각에 씁니다.</summary>
        Fixed,
    }

    [Header("범위")]
    [Tooltip("움직일 수 있는 폭을 무엇으로 정할지.")]
    [SerializeField] private Limit limit = Limit.CoverParent;

    [Tooltip("Fixed일 때 좌우·상하로 움직일 최대 픽셀.")]
    [SerializeField] private Vector2 fixedRoom = new Vector2(24f, 14f);

    [Tooltip("그 폭을 얼마나 쓸지. 1이면 끝까지.")]
    [Range(0f, 1f)]
    [SerializeField] private float strength = 1f;

    [Header("움직임")]
    [Tooltip("따라오는 데 걸리는 시간 (초). 클수록 늘어집니다.")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("켜면 마우스 반대쪽으로 밀립니다.")]
    [SerializeField] private bool invert;

    private RectTransform _rect;
    private RectTransform _parent;
    private Vector2 _rest;
    private Vector2 _drift;
    private Vector2 _velocity;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _parent = _rect.parent as RectTransform;

        if (_parent == null)
        {
            Debug.LogError($"{nameof(MouseParallax)} on '{name}': 부모가 RectTransform이어야 합니다.", this);
            enabled = false;
            return;
        }

        _rest = _rect.anchoredPosition;

        // 씬에 놓인 자리가 정중앙에서 얼마나 어긋나 있는지 재둔다. 어긋난 쪽은 여유가
        // 그만큼 적으므로, 양쪽에서 이 값을 빼야 한쪽 끝에서 가장자리가 드러나지 않는다.
        Vector2 centerNow = _parent.InverseTransformPoint(_rect.TransformPoint(_rect.rect.center));
        Vector2 offset = centerNow - _parent.rect.center;
        _drift = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
    }

    private void Update()
    {
        Vector2 aim = ReadMouse();
        if (invert)
        {
            aim = -aim;
        }

        Vector2 target = _rest + Vector2.Scale(aim, Room()) * strength;

        // UI 움직임은 게임 시간과 상관이 없어야 한다. 일시정지를 붙여도 똑같이 반응한다.
        _rect.anchoredPosition = Vector2.SmoothDamp(_rect.anchoredPosition, target, ref _velocity,
                                                    smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    /// <summary>이번 축으로 움직일 수 있는 최대 픽셀.</summary>
    private Vector2 Room()
    {
        if (limit == Limit.Fixed)
        {
            return fixedRoom;
        }

        Vector3 scale = _rect.localScale;
        Vector2 size = new Vector2(_rect.rect.width * scale.x, _rect.rect.height * scale.y);
        Vector2 slack = (size - _parent.rect.size) * 0.5f;

        return Vector2.Max(slack - _drift, Vector2.zero);
    }

    /// <summary>화면 안 마우스 위치를 -1에서 1로. 창 밖으로 나가도 범위를 넘지 않는다.</summary>
    private static Vector2 ReadMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return Vector2.zero;
        }

        Vector2 p = mouse.position.ReadValue();

        return new Vector2(Mathf.Clamp(p.x / Screen.width * 2f - 1f, -1f, 1f),
                           Mathf.Clamp(p.y / Screen.height * 2f - 1f, -1f, 1f));
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0.01f, smoothTime);
        fixedRoom = Vector2.Max(fixedRoom, Vector2.zero);
    }
}
