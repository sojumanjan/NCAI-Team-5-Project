using UnityEngine;

/// <summary>
/// 허브 방에서 씨앗이 울타리 안을 혼자 돌아다니게 한다. CharacterRoot에 붙인다.
///
/// CharacterRoot를 움직이는 이유: 진화 연출(EvolutionController)이 줌·흔들림·스케일 팝을
/// 전부 CharacterRoot에 건다. 그림만 따로 옮기면 연출과 흰색 덮개가 제자리에 남는다.
/// 그래서 울타리는 반드시 CharacterRoot 바깥에 있어야 한다. 자식으로 두면 같이 끌려간다.
///
/// 울타리 판정을 Collider2D.OverlapPoint에 맡기는 이유: 모양을 코드에 적어두지 않으면
/// 캡슐이든 폴리곤이든 씬에서 콜라이더만 고쳐 그리면 끝난다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SeedWanderer : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("돌아다닐 범위. CharacterRoot 바깥에 둔 콜라이더여야 합니다.")]
    [SerializeField] private Collider2D boundary;

    [Tooltip("울타리 안에 있어야 하는 기준점. 씨앗 그림(CharacterImage)을 넣습니다. 비워두면 이 오브젝트.")]
    [SerializeField] private RectTransform body;

    [Tooltip("진화 연출 중엔 멈춥니다. 비워두면 EvolutionController.Instance를 씁니다.")]
    [SerializeField] private EvolutionController evolution;

    [Header("이동")]
    [Tooltip("걷는 속도 (캔버스 픽셀/초).")]
    [SerializeField] private float moveSpeed = 120f;

    [Tooltip("한 번에 이만큼은 걸어야 목적지로 인정합니다. 짧으면 제자리에서 꼼지락대는 것처럼 보입니다.")]
    [SerializeField] private float minTravel = 80f;

    [Tooltip("멈춰서 쉬는 시간 범위 (초).")]
    [SerializeField] private Vector2 idleRange = new Vector2(1.5f, 4f);

    [Header("통통 튀기")]
    [Tooltip("튀어오르는 높이 (캔버스 픽셀). 0이면 미끄러지듯 걷습니다.")]
    [SerializeField] private float hopHeight = 10f;

    [Tooltip("1초에 튀는 횟수.")]
    [SerializeField] private float hopsPerSecond = 3f;

    [Header("뒤뚱거리기")]
    [Tooltip("한 번 튈 때 좌우로 기우는 최대 각도 (도). 0이면 기울지 않습니다.")]
    [SerializeField] private float waddleAngle = 8f;

    [Tooltip("켜면 발밑(그림 아래쪽 가운데)을 축으로 기웁니다. 끄면 그림 한가운데를 축으로 돕니다.")]
    [SerializeField] private bool waddleAroundFeet = true;

    [Header("목적지 찾기")]
    [Tooltip("울타리 안쪽 점을 찾기 위해 뽑아볼 횟수. 울타리가 가늘고 길수록 늘립니다.")]
    [SerializeField] private int sampleAttempts = 30;

    private enum State
    {
        Idle,
        Walking,
        Paused,
    }

    private RectTransform _rect;
    private RectTransform _parent;
    private State _state;

    private float _idleUntil;

    // 튀는 높이를 뺀 발바닥 위치. 표시 위치에 높이를 더해 쓰므로 둘을 따로 들고 있어야
    // 튀는 도중의 높이가 다음 걸음의 출발점에 섞여 들지 않는다.
    private Vector2 _ground;
    private Vector2 _from;
    private Vector2 _to;
    private float _walkTime;
    private float _walkDuration;
    private int _hopCount;

    // 이 오브젝트 기준점에서 본 발밑 위치. 기준점이 그림 한가운데라 그냥 돌리면 팽이처럼 돈다.
    private Vector2 _feet;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _parent = _rect.parent as RectTransform;

        if (body == null)
        {
            body = _rect;
        }

        if (boundary == null || _parent == null)
        {
            Debug.LogError($"{nameof(SeedWanderer)} on '{name}': 울타리 콜라이더와 RectTransform 부모가 필요합니다.", this);
            enabled = false;
            return;
        }

        if (boundary.transform.IsChildOf(_rect))
        {
            Debug.LogError($"{nameof(SeedWanderer)} on '{name}': 울타리 '{boundary.name}'가 이 오브젝트의 자식이라 같이 움직입니다. " +
                           "CharacterRoot 바깥으로 빼주세요.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        _ground = _rect.anchoredPosition;
        BeginIdle();
    }

    private void Update()
    {
        if (IsEvolutionPlaying())
        {
            if (_state != State.Paused)
            {
                // 연출은 회전을 건드리지 않는다. 기운 채로 멈추면 진화 내내 비스듬히 서 있게 된다.
                _rect.localRotation = Quaternion.identity;
                _state = State.Paused;
            }

            return;
        }

        if (_state == State.Paused)
        {
            // 연출이 끝나며 되돌려놓은 자리를 새 출발점으로 삼는다. 멈추기 전 값을 쓰면 순간이동한다.
            _ground = _rect.anchoredPosition;
            BeginIdle();
            return;
        }

        if (_state == State.Idle)
        {
            if (Time.time >= _idleUntil)
            {
                TryBeginWalk();
            }

            return;
        }

        TickWalk();
    }

    private bool IsEvolutionPlaying()
    {
        EvolutionController controller = evolution != null ? evolution : EvolutionController.Instance;
        return controller != null && controller.IsPlaying;
    }

    private void BeginIdle()
    {
        _state = State.Idle;
        _idleUntil = Time.time + Random.Range(idleRange.x, idleRange.y);
    }

    private void TryBeginWalk()
    {
        if (!TryPickDestination(out Vector2 destination))
        {
            // 이번엔 못 찾았어도 한 번 더 쉬었다 다시 뽑으면 된다. 멈춰 있는 게 울타리를 넘는 것보다 낫다.
            BeginIdle();
            return;
        }

        _from = _ground;
        _to = destination;
        _walkTime = 0f;
        _walkDuration = Vector2.Distance(_from, _to) / Mathf.Max(1f, moveSpeed);

        // 튀는 횟수를 걸음 길이에 맞춰 정수로 떨어뜨린다. 그래야 출발과 도착 순간 모두 발이 땅에 닿아 있다.
        _hopCount = Mathf.Max(1, Mathf.RoundToInt(_walkDuration * hopsPerSecond));
        _feet = waddleAroundFeet ? FeetOffset() : Vector2.zero;
        _state = State.Walking;
    }

    /// <summary>
    /// 그림 아래쪽 가운데를 이 오브젝트 부모 단위로 잰 값. 걸음마다 새로 재는 이유는 진화로 그림이
    /// 바뀌거나 커질 수 있어서다.
    /// </summary>
    private Vector2 FeetOffset()
    {
        Rect r = body.rect;
        Vector3 feetWorld = body.TransformPoint(new Vector3(r.center.x, r.yMin, 0f));
        Vector2 local = _rect.InverseTransformPoint(feetWorld);
        return Vector2.Scale(local, _rect.localScale);
    }

    private void TickWalk()
    {
        _walkTime += Time.deltaTime;
        float t = Mathf.Clamp01(_walkTime / Mathf.Max(0.01f, _walkDuration));

        // 출발과 도착에서 속도를 줄여야 목적지에 '멈춰 선' 느낌이 난다.
        _ground = Vector2.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t));

        // 한 번 튈 때마다 부호가 바뀌는 값 하나로 높이와 기울기를 같이 낸다. 공중에서 가장 기울고
        // 땅에 닿는 순간 똑바로 서므로, 튀는 박자와 뒤뚱거리는 박자가 어긋날 수 없다.
        float swing = Mathf.Sin(t * _hopCount * Mathf.PI);
        float hop = Mathf.Abs(swing) * hopHeight;

        Quaternion tilt = Quaternion.Euler(0f, 0f, swing * waddleAngle);

        // 기준점을 축으로 돈 만큼 되밀어서 발밑이 제자리에 남게 한다.
        Vector2 pivotFix = _feet - (Vector2)(tilt * _feet);

        _rect.localRotation = tilt;
        _rect.anchoredPosition = _ground + Vector2.up * hop + pivotFix;

        if (t >= 1f)
        {
            _rect.localRotation = Quaternion.identity;
            _rect.anchoredPosition = _ground;
            BeginIdle();
        }
    }

    /// <summary>
    /// 울타리 안쪽의 무작위 점을 골라, 기준점(body)이 그 점에 오도록 하는 이 오브젝트의 위치를 돌려준다.
    /// </summary>
    private bool TryPickDestination(out Vector2 destination)
    {
        destination = _ground;

        // 캔버스 스케일러가 해상도에 맞춰 크기를 바꾸면 콜라이더의 물리 쪽 위치가 한 박자 늦게 따라온다.
        // 목적지를 뽑는 순간만큼은 최신 모양으로 재야 울타리 밖 점을 안쪽이라고 믿지 않는다.
        Physics2D.SyncTransforms();

        // 그림은 CharacterRoot 기준점에서 멀찍이 떨어져 있을 수 있다(지금 씬은 524px). 기준점이 아닌
        // 그림 위치로 판정해야 울타리가 그린 대로 먹는다.
        // 쉬는 중에만 불리므로 지금 그림 위치가 곧 발바닥 위치다.
        Vector2 bodyNow = _parent.InverseTransformPoint(body.position);

        Bounds area = boundary.bounds;
        float minTravelSqr = minTravel * minTravel;

        for (int i = 0; i < sampleAttempts; i++)
        {
            Vector2 world = new Vector2(Random.Range(area.min.x, area.max.x),
                                        Random.Range(area.min.y, area.max.y));

            if (!boundary.OverlapPoint(world))
            {
                continue;
            }

            Vector2 local = _parent.InverseTransformPoint(world);
            if ((local - bodyNow).sqrMagnitude < minTravelSqr)
            {
                continue;
            }

            // 부모 로컬에서 잰 차이를 그대로 anchoredPosition에 더한다. 앵커가 고정인 한 둘은 같은 만큼 움직인다.
            destination = _ground + (local - bodyNow);
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(1f, moveSpeed);
        minTravel = Mathf.Max(0f, minTravel);
        idleRange.x = Mathf.Max(0f, idleRange.x);
        idleRange.y = Mathf.Max(idleRange.x, idleRange.y);
        hopHeight = Mathf.Max(0f, hopHeight);
        hopsPerSecond = Mathf.Max(0.1f, hopsPerSecond);
        waddleAngle = Mathf.Clamp(waddleAngle, 0f, 45f);
        sampleAttempts = Mathf.Max(1, sampleAttempts);
    }
}
