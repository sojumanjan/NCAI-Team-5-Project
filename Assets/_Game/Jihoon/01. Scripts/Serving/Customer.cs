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
        Leaving,
    }

    [Header("이동")]
    [Tooltip("걷는 속도 (m/s).")]
    [SerializeField] private float moveSpeed = 2.2f;

    [Tooltip("목적지에 이만큼 가까워지면 도착으로 봅니다 (m).")]
    [SerializeField] private float arriveDistance = 0.15f;

    [Tooltip("회전 속도 (deg/sec). 진행 방향을 바라봅니다.")]
    [SerializeField] private float turnSpeed = 540f;

    [Tooltip("목표 지점보다 얼마나 위에 설지 (m). 프리팹 원점이 몸 가운데면 키의 절반을 넣으세요.")]
    [SerializeField] private float groundOffset = 1f;

    [Header("인내심")]
    [Tooltip("스포너가 값을 주지 않았을 때 쓸 기본 인내심 (초).")]
    [SerializeField] private float defaultPatience = 30f;

    private ServingSpot _spot;
    private Phase _phase = Phase.Idle;
    private Vector3 _target;
    private float _patienceMax;
    private float _patienceLeft;
    private bool _gaveUp;

    /// <summary>What this customer wants.</summary>
    public ItemData Order { get; private set; }

    /// <summary>Patience remaining, 1 when fresh and 0 when they walk out.</summary>
    public float Patience01 => _patienceMax > 0f ? Mathf.Clamp01(_patienceLeft / _patienceMax) : 1f;

    /// <summary>True only while standing at the counter waiting. Patience runs in this phase alone.</summary>
    public bool IsWaiting => _phase == Phase.Ordering;

    // ---------------------------------------------------------------- flow

    /// <summary>
    /// Sends the customer to the counter. A patience of zero or less falls back to the
    /// prefab's own default, so a customer dropped into the scene by hand still behaves.
    /// </summary>
    public void Arrive(ServingSpot spot, ItemData order, float patienceSeconds = 0f)
    {
        _spot = spot;
        Order = order;

        _patienceMax = patienceSeconds > 0f ? patienceSeconds : defaultPatience;
        _patienceLeft = _patienceMax;

        if (spot == null || spot.CustomerStand == null)
        {
            Debug.LogError($"{name}: cannot arrive, the spot has no customer stand.", this);
            return;
        }

        _target = spot.CustomerStand.position;
        _phase = Phase.WalkingIn;

        // Stand on the floor rather than in it. Spawn and stand points sit at ground
        // level, but the prefab's origin is usually its middle, so it needs lifting.
        SnapToGroundHeight();
    }

    /// <summary>Sends the customer away. The object destroys itself on arrival.</summary>
    public void Leave()
    {
        if (_spot != null && _spot.ExitPoint != null)
        {
            _target = _spot.ExitPoint.position;
            _phase = Phase.Leaving;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        switch (_phase)
        {
            case Phase.WalkingIn:
                if (!StepToward(_target))
                {
                    _phase = Phase.Ordering;
                    FaceCounter();
                    _spot.OnCustomerReady(this, Order);
                }
                break;

            case Phase.Ordering:
                TickPatience();
                break;

            case Phase.Leaving:
                if (!StepToward(_target))
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
            Leave();
        }
    }

    // ---------------------------------------------------------------- movement

    /// <summary>Moves one frame toward the target. Returns true while still travelling.</summary>
    private bool StepToward(Vector3 destination)
    {
        Vector3 flat = destination - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            return false;
        }

        // Walk on the plane at the destination's height plus the standing offset, so the
        // customer neither sinks nor floats as it crosses the floor.
        float walkHeight = destination.y + groundOffset;

        transform.position = Vector3.MoveTowards(transform.position,
                                                 new Vector3(destination.x, walkHeight, destination.z),
                                                 moveSpeed * Time.deltaTime);

        Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        return true;
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
        Vector3 position = transform.position;
        position.y = _target.y + groundOffset;
        transform.position = position;
    }
}
