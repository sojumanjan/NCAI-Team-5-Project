using UnityEngine;

/// <summary>
/// Idle decoration for the main logo screen: drifts a UI graphic across the canvas.
/// It starts just outside the screen, travels to another point outside the opposite
/// side, and spins the whole way. When it arrives it picks a fresh route and repeats.
/// Attach to the Character object (or any RectTransform child of the Canvas).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MainLogoCharacter : MonoBehaviour
{
    [Header("이동")]
    [Tooltip("이동 속도 범위 (px/sec). 한 번 지나갈 때마다 이 범위에서 랜덤으로 뽑습니다.")]
    [SerializeField] private Vector2 moveSpeedRange = new Vector2(250f, 500f);

    [Header("회전")]
    [Tooltip("회전 속도 범위 (deg/sec).")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(40f, 140f);

    [Tooltip("켜면 시계/반시계 방향을 매번 랜덤으로 정합니다.")]
    [SerializeField] private bool randomizeSpinDirection = true;

    [Header("출발 / 도착")]
    [Tooltip("화면 밖으로 얼마나 더 멀리서 출발할지 (px). 스프라이트 크기보다 크게 두면 튀어나오는 게 안 보입니다.")]
    [SerializeField] private float offscreenMargin = 300f;

    [Tooltip("출발점과 도착점에 더해지는 랜덤 오프셋 (px). x, y 각각 ± 범위입니다.")]
    [SerializeField] private Vector2 randomOffset = new Vector2(200f, 200f);

    [Tooltip("반대편으로 건너갈 때 허용할 각도 흔들림 (deg). 0이면 항상 정확히 맞은편으로 갑니다.")]
    [Range(0f, 80f)]
    [SerializeField] private float crossingSpreadAngle = 35f;

    [Header("타이밍")]
    [Tooltip("한 번 지나간 뒤 다시 나타나기까지의 대기 시간 범위 (sec).")]
    [SerializeField] private Vector2 respawnDelayRange = new Vector2(0f, 1.5f);

    private RectTransform _rect;
    private RectTransform _area;    // coordinate space we travel in (the parent, normally the Canvas)
    private Vector2 _start;
    private Vector2 _end;
    private float _speed;
    private float _spin;
    private float _travelled;
    private float _tripLength;
    private float _waitTimer;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _area = _rect.parent as RectTransform;

        if (_area == null)
        {
            Debug.LogError($"{nameof(MainLogoCharacter)} needs a RectTransform parent (put it under the Canvas).", this);
            enabled = false;
            return;
        }

        // The centre-relative math below only holds for a point anchor, not a stretched one.
        if (_rect.anchorMin != _rect.anchorMax)
        {
            Debug.LogWarning($"{nameof(MainLogoCharacter)} expects a point anchor, not a stretched one. " +
                             "Set the anchor to a single point (e.g. centre) or it will drift oddly.", this);
        }
    }

    private void OnEnable()
    {
        BeginTrip();
    }

    private void Update()
    {
        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.deltaTime;
            return;
        }

        _travelled += _speed * Time.deltaTime;
        float progress = _tripLength > 0f ? Mathf.Clamp01(_travelled / _tripLength) : 1f;

        SetCentredPosition(Vector2.Lerp(_start, _end, progress));
        _rect.Rotate(0f, 0f, _spin * Time.deltaTime);

        if (progress >= 1f)
        {
            BeginTrip();
        }
    }

    /// <summary>Picks a new off-screen start, a new off-screen exit, and fresh speeds.</summary>
    private void BeginTrip()
    {
        if (_area == null)
        {
            return;
        }

        // A circle that encloses the whole canvas, pushed out by the margin, so both
        // ends of the route are guaranteed to sit off-screen whatever the aspect ratio.
        float radius = _area.rect.size.magnitude * 0.5f + offscreenMargin;

        float entryAngle = Random.Range(0f, 360f);
        float exitAngle = entryAngle + 180f + Random.Range(-crossingSpreadAngle, crossingSpreadAngle);

        _start = PointOnCircle(entryAngle, radius) + RandomOffset();
        _end = PointOnCircle(exitAngle, radius) + RandomOffset();

        _speed = Random.Range(moveSpeedRange.x, moveSpeedRange.y);
        _spin = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        if (randomizeSpinDirection && Random.value < 0.5f)
        {
            _spin = -_spin;
        }

        _tripLength = Vector2.Distance(_start, _end);
        _travelled = 0f;
        _waitTimer = Random.Range(respawnDelayRange.x, respawnDelayRange.y);

        SetCentredPosition(_start);
    }

    private static Vector2 PointOnCircle(float angleDegrees, float radius)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
    }

    private Vector2 RandomOffset()
    {
        return new Vector2(Random.Range(-randomOffset.x, randomOffset.x),
                           Random.Range(-randomOffset.y, randomOffset.y));
    }

    /// <summary>Places the graphic using coordinates measured from the centre of the area.</summary>
    private void SetCentredPosition(Vector2 centred)
    {
        // anchoredPosition is measured from the anchor, so shift by however far the
        // anchor sits from the centre. Lets the component work on any point anchor.
        Vector2 anchorFromCentre = Vector2.Scale(_area.rect.size,
                                                 _rect.anchorMin - new Vector2(0.5f, 0.5f));
        _rect.anchoredPosition = centred - anchorFromCentre;
    }

    private void OnValidate()
    {
        moveSpeedRange = SortedRange(moveSpeedRange, 0f);
        rotationSpeedRange = SortedRange(rotationSpeedRange, 0f);
        respawnDelayRange = SortedRange(respawnDelayRange, 0f);
        offscreenMargin = Mathf.Max(0f, offscreenMargin);
        randomOffset = new Vector2(Mathf.Max(0f, randomOffset.x), Mathf.Max(0f, randomOffset.y));
    }

    /// <summary>Keeps a min/max pair in order and above a floor, so the inspector cannot invert it.</summary>
    private static Vector2 SortedRange(Vector2 range, float floor)
    {
        float min = Mathf.Max(floor, range.x);
        float max = Mathf.Max(floor, range.y);
        return min <= max ? new Vector2(min, max) : new Vector2(max, min);
    }
}
