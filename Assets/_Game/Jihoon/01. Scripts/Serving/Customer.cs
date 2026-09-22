using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A customer: walks to the counter, states an order, waits with a shrinking patience
/// meter, then walks off — served or fed up.
///
/// Movement is a straight walk with no pathfinding, which is enough for a counter you can
/// reach in a straight line. Patience only runs once they have actually arrived, so a long
/// walk never costs them time.
/// </summary>
public class Customer : MonoBehaviour
{
    private enum Phase
    {
        Idle,
        WalkingIn,
        Ordering,

        /// <summary>화가 나서 그 자리에 서 있는 동안. 씩씩대는 연출을 보여주고 나서 떠난다.</summary>
        Fuming,

        /// <summary>주문을 제대로 받아 기뻐하는 동안. 하트를 띄우고 나서 떠난다.</summary>
        Happy,

        Leaving,
    }

    [Header("이동")]
    [Tooltip("걷는 속도 (m/s).")]
    [SerializeField] private float moveSpeed = 2.2f;

    [Tooltip("최종 목적지에 이만큼 가까워지면 도착으로 봅니다 (m).")]
    [SerializeField] private float arriveDistance = 0.15f;

    [Tooltip("중간 경유지 통과 판정 반경 (m). 도착 판정보다 넉넉하게 둬야 모서리를 " +
             "자연스럽게 깎고 지나갑니다.")]
    [SerializeField] private float waypointDistance = 0.35f;

    [Tooltip("회전 속도 (deg/sec). 진행 방향을 바라봅니다. 낮출수록 크게 돕니다.")]
    [SerializeField] private float turnSpeed = 240f;

    [Tooltip("코너를 이만큼 앞두고 다음 구간 쪽으로 미리 몸을 틀기 시작합니다 (m). " +
             "0이면 경유지에 닿은 뒤에야 도는 예전 동작입니다.")]
    [SerializeField] private float cornerLookAhead = 1.5f;

    [Tooltip("목표 지점보다 얼마나 위에 설지 (m). 프리팹 원점이 몸 가운데면 키의 절반을 넣으세요.")]
    [SerializeField] private float groundOffset = 1f;

    [Header("소리")]
    [Tooltip("카운터에 도착해 주문을 말할 때 나는 소리. 비워두면 조용히 도착합니다.")]
    [SerializeField] private SoundData orderSound;

    [Tooltip("주문을 제대로 받고 떠날 때.")]
    [SerializeField] private SoundData successSound;

    [Tooltip("주문이 틀렸거나 기다리다 지쳐 떠날 때.")]
    [SerializeField] private SoundData failSound;

    [Header("인내심")]
    [Tooltip("스포너가 값을 주지 않았을 때 쓸 기본 인내심 (초).")]
    [SerializeField] private float defaultPatience = 30f;

    [Header("화났을 때")]
    [Tooltip("돌아서기 전에 제자리에서 화를 내는 시간 (초). 0이면 바로 떠납니다.")]
    [SerializeField] private float angryPauseSeconds = 2f;

    [Header("주문에 만족했을 때")]
    [Tooltip("돌아서기 전에 제자리에서 기뻐하는 시간 (초). 0이면 바로 떠납니다. " +
             "하트 연출이 이 시간 안에 끝나도록 맞추세요.")]
    [SerializeField] private float happyPauseSeconds = 2f;

    private ServingSpot _spot;
    private Phase _phase = Phase.Idle;

    // 경유지를 순서대로 밟는다. 마지막 칸이 진짜 목적지이고, 그 앞은 전부 지나가는 길이다.
    private readonly List<Vector3> _route = new();
    private int _leg;
    private float _patienceMax;
    private float _patienceLeft;
    private bool _gaveUp;

    // 화내는 시간과 기뻐하는 시간은 같은 "제자리에 머무는 동안"이라 타이머를 나눌 이유가 없다.
    private float _pauseTimer;

    private readonly List<ItemData> _orders = new();

    /// <summary>What this customer wants. 1~3개.</summary>
    public IReadOnlyList<ItemData> Orders => _orders;

    /// <summary>주문 중 <paramref name="index"/>번째가 이미 나왔는지. 레인이 답한다.</summary>
    public bool IsServed(int index) => _spot != null && _spot.IsServed(index);

    /// <summary>Patience remaining, 1 when fresh and 0 when they walk out.</summary>
    public float Patience01 => _patienceMax > 0f ? Mathf.Clamp01(_patienceLeft / _patienceMax) : 1f;

    /// <summary>True only while standing at the counter waiting. Patience runs in this phase alone.</summary>
    public bool IsWaiting => _phase == Phase.Ordering;

    /// <summary>걷는 중인지. 걸을 때만 뒤뚱거리면 되므로 연출 쪽에서 본다.</summary>
    public bool IsWalking => _phase == Phase.WalkingIn || _phase == Phase.Leaving;

    /// <summary>
    /// 기분이 상한 채로 떠나는지. 주문을 망쳤거나 기다리다 지친 경우다.
    /// 한 번 참이 되면 다시 거짓이 되지 않는다 — 돌이킬 방법이 없는 결말이라서.
    /// </summary>
    public bool IsAngry { get; private set; }

    /// <summary>화를 내며 제자리에 서 있는 동안. 연출 쪽이 이때 튕기는 트윈을 돌린다.</summary>
    public bool IsFuming => _phase == Phase.Fuming;

    /// <summary>주문을 제대로 받고 제자리에서 기뻐하는 동안. 하트 연출이 이걸 보고 터진다.</summary>
    public bool IsHappy => _phase == Phase.Happy;

    // ---------------------------------------------------------------- flow

    /// <summary>
    /// Sends the customer to the counter. A patience of zero or less falls back to the
    /// prefab's own default, so a customer dropped into the scene by hand still behaves.
    /// </summary>
    public void Arrive(ServingSpot spot, IReadOnlyList<ItemData> orders, float patienceSeconds = 0f)
    {
        _spot = spot;

        _orders.Clear();
        if (orders != null)
        {
            foreach (ItemData order in orders)
            {
                _orders.Add(order);
            }
        }

        _patienceMax = patienceSeconds > 0f ? patienceSeconds : defaultPatience;
        _patienceLeft = _patienceMax;

        if (spot == null || spot.CustomerStand == null)
        {
            Debug.LogError($"{name}: cannot arrive, the spot has no customer stand.", this);
            return;
        }

        BuildRoute(spot.EntryPath, false, spot.CustomerStand.position);
        _phase = Phase.WalkingIn;

        // Stand on the floor rather than in it. Spawn and stand points sit at ground
        // level, but the prefab's origin is usually its middle, so it needs lifting.
        SnapToGroundHeight();
    }

    /// <summary>
    /// 손님을 내보낸다. 목적지에 닿으면 스스로 사라진다.
    /// <paramref name="angry"/>는 연출용이다 — 판정은 이미 레인이 끝냈다.
    /// </summary>
    public void Leave(bool angry = false)
    {
        IsAngry = IsAngry || angry;

        // 손님 자리에서 난다. 레인이 셋이라 어느 쪽에서 난 소리인지 들려야 한다.
        AudioManager.PlayAt(IsAngry ? failSound : successSound, transform.position);

        if (_spot == null)
        {
            Destroy(gameObject);
            return;
        }

        // 어느 쪽이든 바로 등을 돌리지 않는다. 제자리에서 한마디 하고 나간다.
        if (IsAngry && angryPauseSeconds > 0f)
        {
            _pauseTimer = angryPauseSeconds;
            _phase = Phase.Fuming;
            return;
        }

        if (!IsAngry && happyPauseSeconds > 0f)
        {
            _pauseTimer = happyPauseSeconds;
            _phase = Phase.Happy;
            return;
        }

        BeginExit();
    }

    /// <summary>실제로 등을 돌려 걸어 나가기 시작한다.</summary>
    private void BeginExit()
    {
        // 이 순간까지 레인이 이 손님을 붙잡고 있었다. 이제야 자리가 빈다 — 화내는 2초 동안
        // 자리를 내주면 새 손님이 같은 지점으로 걸어와 겹친다.
        if (_spot != null)
        {
            _spot.OnCustomerDeparted(this);
        }

        _route.Clear();

        // 나갈 길을 따로 안 깔았으면 들어온 길을 거꾸로 되짚는다.
        if (_spot.ExitPath != null)
        {
            _spot.ExitPath.AppendPositions(_route, _spot.ReverseExitPath);
        }

        // ExitPoint는 '사라지는 지점'이다. 경로가 이미 문 밖까지 데려다주면 없어도 된다.
        if (_spot.ExitPoint != null)
        {
            _route.Add(_spot.ExitPoint.position);
        }

        if (_route.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        _leg = 0;
        _phase = Phase.Leaving;
    }

    /// <summary>경유지들 뒤에 최종 목적지를 붙여 이번 구간의 경로를 만든다.</summary>
    private void BuildRoute(CustomerPath path, bool reversed, Vector3 destination)
    {
        _route.Clear();

        if (path != null)
        {
            path.AppendPositions(_route, reversed);
        }

        _route.Add(destination);
        _leg = 0;
    }

    private void Update()
    {
        switch (_phase)
        {
            case Phase.WalkingIn:
                if (!FollowRoute())
                {
                    _phase = Phase.Ordering;
                    FaceCounter();

                    // 주문을 알리는 소리는 손님 자리에서 난다. SO의 Spatial Blend가 0이면
                    // 어차피 어디서나 같게 들리므로, 위치를 넘겨두면 나중에 3D로 바꿀 때
                    // 코드를 고칠 일이 없다.
                    AudioManager.PlayAt(orderSound, transform.position);

                    _spot.OnCustomerReady(this, _orders);
                }
                break;

            case Phase.Ordering:
                TickPatience();
                break;

            case Phase.Fuming:
            case Phase.Happy:
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0f)
                {
                    BeginExit();
                }
                break;

            case Phase.Leaving:
                if (!FollowRoute())
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    // ---------------------------------------------------------------- patience

    private void TickPatience()
    {
        if (_gaveUp || _patienceMax <= 0f)
        {
            return;
        }

        _patienceLeft -= Time.deltaTime;

        if (_patienceLeft > 0f)
        {
            return;
        }

        // Guarded because AbandonOrder calls straight back into Leave(), and a second
        // call would report the same customer twice and double the rating penalty.
        _gaveUp = true;
        _patienceLeft = 0f;

        if (_spot != null)
        {
            _spot.AbandonOrder();
        }
        else
        {
            Leave(angry: true);
        }
    }

    // ---------------------------------------------------------------- movement

    /// <summary>
    /// 경로를 한 프레임 진행한다. 아직 갈 길이 남았으면 true.
    ///
    /// 중간 경유지는 넉넉한 반경으로 통과 처리해서 모서리에 딱 붙지 않고 흘러가게 한다.
    /// 마지막 한 칸만 정확히 도달해야 한다 — 손님이 서는 자리이기 때문이다.
    /// </summary>
    private bool FollowRoute()
    {
        while (_leg < _route.Count)
        {
            bool last = _leg == _route.Count - 1;
            float reach = last ? arriveDistance : Mathf.Max(arriveDistance, waypointDistance);

            if (StepToward(_route[_leg], reach))
            {
                return true;
            }

            // 이번 칸은 끝. 다음 칸으로 넘어가되, 겹쳐 놓인 경유지는 같은 프레임에 흘려보낸다.
            _leg++;
        }

        return false;
    }

    /// <summary>Moves one frame toward the target. Returns true while still travelling.</summary>
    private bool StepToward(Vector3 destination, float reach)
    {
        Vector3 flat = destination - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude <= reach * reach)
        {
            return false;
        }

        // Walk on the plane at the destination's height plus the standing offset, so the
        // customer neither sinks nor floats as it crosses the floor.
        float walkHeight = destination.y + groundOffset;

        transform.position = Vector3.MoveTowards(transform.position,
                                                 new Vector3(destination.x, walkHeight, destination.z),
                                                 moveSpeed * Time.deltaTime);

        Vector3 heading = DesiredHeading(flat);
        if (heading.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(heading, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        return true;
    }

    /// <summary>
    /// 지금 바라봐야 할 방향. 코너가 가까워질수록 다음 구간 쪽으로 미리 기운다.
    ///
    /// 경유지에 닿은 뒤에 방향을 바꾸면 제자리에서 홱 도는 모양이 된다. 사람은 모퉁이에
    /// 도착하기 전부터 몸을 틀기 시작하므로, 남은 거리에 따라 두 방향을 섞는다.
    /// </summary>
    private Vector3 DesiredHeading(Vector3 towardCurrent)
    {
        if (cornerLookAhead <= 0f || _leg + 1 >= _route.Count)
        {
            return towardCurrent.normalized;
        }

        Vector3 nextLeg = _route[_leg + 1] - _route[_leg];
        nextLeg.y = 0f;

        if (nextLeg.sqrMagnitude < 0.0001f)
        {
            return towardCurrent.normalized;
        }

        float remaining = towardCurrent.magnitude;
        float blend = 1f - Mathf.Clamp01(remaining / cornerLookAhead);

        return Vector3.Slerp(towardCurrent.normalized, nextLeg.normalized, blend);
    }

    private void OnValidate()
    {
        angryPauseSeconds = Mathf.Max(0f, angryPauseSeconds);
        happyPauseSeconds = Mathf.Max(0f, happyPauseSeconds);
        arriveDistance = Mathf.Max(0.01f, arriveDistance);
        waypointDistance = Mathf.Max(arriveDistance, waypointDistance);
        cornerLookAhead = Mathf.Max(0f, cornerLookAhead);
        turnSpeed = Mathf.Max(1f, turnSpeed);
    }

    private void FaceCounter()
    {
        if (_spot == null || _spot.CustomerStand == null)
        {
            return;
        }

        // The stand point's forward is authored to face the counter.
        transform.rotation = Quaternion.LookRotation(_spot.CustomerStand.forward, Vector3.up);
    }

    /// <summary>Lifts the customer to standing height above its current target point.</summary>
    private void SnapToGroundHeight()
    {
        if (_route.Count == 0)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = _route[0].y + groundOffset;
        transform.position = position;
    }
}
