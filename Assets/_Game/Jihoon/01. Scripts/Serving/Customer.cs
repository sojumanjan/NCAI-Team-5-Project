using UnityEngine;

/// <summary>
/// A customer: walks to the counter, states an order, waits, then walks off once served.
///
/// Movement is a straight walk with no pathfinding, which is enough for a counter you can
/// reach in a straight line. Patience is deliberately not here yet — that is step 5.
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

    private ServingSpot _spot;
    private Phase _phase = Phase.Idle;
    private Vector3 _target;

    /// <summary>What this customer wants. Null until they reach the counter.</summary>
    public ItemData Order { get; private set; }

    /// <summary>Sends the customer to the counter with an order in mind.</summary>
    public void Arrive(ServingSpot spot, ItemData order)
    {
        _spot = spot;
        Order = order;

        if (spot == null || spot.CustomerStand == null)
        {
            Debug.LogError($"{name}: cannot arrive, the spot has no customer stand.", this);
            return;
        }

        _target = spot.CustomerStand.position;
        _phase = Phase.WalkingIn;
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
        if (_phase != Phase.WalkingIn && _phase != Phase.Leaving)
        {
            return;
        }

        if (StepToward(_target))
        {
            return;
        }

        if (_phase == Phase.WalkingIn)
        {
            _phase = Phase.Ordering;
            FaceCounter();
            _spot.OnCustomerReady(this, Order);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Moves one frame toward the target. Returns true while still travelling.</summary>
    private bool StepToward(Vector3 destination)
    {
        Vector3 flat = destination - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            return false;
        }

        transform.position = Vector3.MoveTowards(transform.position,
                                                 new Vector3(destination.x, transform.position.y, destination.z),
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
}
