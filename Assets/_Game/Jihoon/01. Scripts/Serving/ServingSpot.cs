using System;
using UnityEngine;

/// <summary>Where one lane of the counter is in its cycle.</summary>
public enum ServingSpotState
{
    /// <summary>No customer. Nothing to do here.</summary>
    Free,

    /// <summary>Customer has arrived and stated their order; the player has not taken it yet.</summary>
    AwaitingAccept,

    /// <summary>Order accepted. The kitchen owes them a dish.</summary>
    Accepted,
}

/// <summary>How an order ended. Everything that moves the rating is one of these.</summary>
public enum OrderResult
{
    /// <summary>Served exactly what was asked for.</summary>
    Correct,

    /// <summary>Served, but the wrong dish.</summary>
    Wrong,

    /// <summary>Customer ran out of patience and left. Raised from step 5 onward.</summary>
    Abandoned,
}

/// <summary>
/// One lane of the counter: a POS terminal, a tray, and the spot the customer stands on.
/// There are three of these, so three customers can be served at once.
///
/// This is the coordinator — the terminal and the tray are thin and just forward to it, so
/// all the ordering rules live in one file. It is also the single place anything outside
/// the lane listens to: customers are spawned at runtime and cannot be wired up in the
/// inspector, so the lane reports on their behalf through <see cref="OrderResolved"/>.
/// </summary>
public class ServingSpot : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("이 자리의 포스기.")]
    [SerializeField] private PosTerminal terminal;

    [Tooltip("이 자리의 트레이.")]
    [SerializeField] private ServingTray tray;

    [Tooltip("손님이 서 있을 위치. 카운터 바깥쪽에, forward가 카운터를 향하게 두세요.")]
    [SerializeField] private Transform customerStand;

    [Tooltip("손님이 등장하고 떠나갈 위치.")]
    [SerializeField] private Transform exitPoint;

    [Header("주문")]
    [Tooltip("주문 가능한 메뉴를 여기서 뽑습니다. 레시피북의 완성품 목록을 씁니다.")]
    [SerializeField] private RecipeBook recipeBook;

    [Header("테스트")]
    [Tooltip("손님 프리팹. 아래 옵션이나 컨텍스트 메뉴로 소환합니다.")]
    [SerializeField] private GameObject customerPrefab;

    [Tooltip("켜면 플레이 시작과 동시에 손님 한 명이 옵니다. 손님 큐는 5단계에서 붙습니다.")]
    [SerializeField] private bool spawnOnStart = true;

    private Customer _customer;

    /// <summary>Current phase of this lane.</summary>
    public ServingSpotState State { get; private set; } = ServingSpotState.Free;

    /// <summary>What the current customer asked for, or null.</summary>
    public ItemData CurrentOrder { get; private set; }

    /// <summary>Where the customer stands.</summary>
    public Transform CustomerStand => customerStand;

    /// <summary>Where the customer walks off to.</summary>
    public Transform ExitPoint => exitPoint;

    /// <summary>True while a customer is present.</summary>
    public bool HasCustomer => _customer != null;

    /// <summary>Fires when a customer states their order.</summary>
    public event Action<ServingSpot, ItemData> OrderPlaced;

    /// <summary>Fires when the player accepts the order at the terminal.</summary>
    public event Action<ServingSpot, ItemData> OrderAccepted;

    /// <summary>
    /// Fires once per order, however it ended. This is the one event the rating system and
    /// the day's dish counter listen to — a single hook instead of one per failure mode.
    /// </summary>
    public event Action<ServingSpot, OrderResult> OrderResolved;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (terminal != null)
        {
            terminal.Bind(this);
        }

        if (tray != null)
        {
            tray.Bind(this);
        }

        if (customerStand == null)
        {
            Debug.LogError($"{name}: Customer Stand is not assigned.", this);
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnCustomer();
        }
    }

    // ---------------------------------------------------------------- customer flow

    /// <summary>Puts a customer at this lane. Does nothing if it is already busy.</summary>
    [ContextMenu("손님 소환")]
    public void SpawnCustomer()
    {
        if (_customer != null || customerPrefab == null || customerStand == null)
        {
            return;
        }

        Vector3 spawnAt = exitPoint != null ? exitPoint.position : customerStand.position;
        GameObject go = Instantiate(customerPrefab, spawnAt, Quaternion.identity);

        Customer customer = go.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogError($"Customer prefab '{go.name}' has no {nameof(Customer)}.", this);
            Destroy(go);
            return;
        }

        _customer = customer;
        customer.Arrive(this, PickOrder());
    }

    private ItemData PickOrder()
    {
        return recipeBook != null ? recipeBook.GetRandomOutput() : null;
    }

    /// <summary>Called by the customer once they reach the counter.</summary>
    public void OnCustomerReady(Customer customer, ItemData wanted)
    {
        if (customer != _customer)
        {
            return;
        }

        CurrentOrder = wanted;
        State = ServingSpotState.AwaitingAccept;
        OrderPlaced?.Invoke(this, wanted);
    }

    /// <summary>
    /// The customer gave up waiting. Step 5 calls this from the patience timer; nothing
    /// calls it yet.
    /// </summary>
    public void AbandonOrder()
    {
        if (State == ServingSpotState.Free)
        {
            return;
        }

        Resolve(OrderResult.Abandoned);
    }

    // ---------------------------------------------------------------- terminal

    /// <summary>True when clicking the POS would do something.</summary>
    public bool CanAcceptOrder() => State == ServingSpotState.AwaitingAccept;

    /// <summary>The player clicked the POS. Takes the order so the tray starts accepting.</summary>
    public void AcceptOrder()
    {
        if (!CanAcceptOrder())
        {
            return;
        }

        State = ServingSpotState.Accepted;
        OrderAccepted?.Invoke(this, CurrentOrder);
    }

    // ---------------------------------------------------------------- tray

    /// <summary>
    /// True when this dish may go on the tray. Ingredients are refused outright, and
    /// nothing is accepted until the order has been taken at the terminal.
    /// </summary>
    public bool CanReceiveDish(ItemData dish)
    {
        return State == ServingSpotState.Accepted
               && dish != null
               && dish.Category == ItemCategory.Dish;
    }

    /// <summary>
    /// A dish landed on the tray. A wrong dish is still accepted — the penalty is the
    /// point, and refusing it would leave the player stuck holding it.
    /// </summary>
    public void DeliverDish(WorldItem dish)
    {
        if (dish == null || !CanReceiveDish(dish.Item))
        {
            return;
        }

        bool correct = dish.Item == CurrentOrder;

        // Park the dish on the tray before resolving, so the visual and the score line up.
        if (tray != null)
        {
            tray.PlaceAndClear(dish);
        }
        else
        {
            Destroy(dish.gameObject);
        }

        Resolve(correct ? OrderResult.Correct : OrderResult.Wrong);
    }

    // ---------------------------------------------------------------- internals

    private void Resolve(OrderResult result)
    {
        if (_customer != null)
        {
            _customer.Leave();
            _customer = null;
        }

        CurrentOrder = null;
        State = ServingSpotState.Free;

        OrderResolved?.Invoke(this, result);
    }

    // ---------------------------------------------------------------- gizmos

    private void OnDrawGizmosSelected()
    {
        if (customerStand != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(customerStand.position, 0.3f);
            Gizmos.DrawRay(customerStand.position, customerStand.forward);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(exitPoint.position, 0.3f);

            if (customerStand != null)
            {
                Gizmos.DrawLine(exitPoint.position, customerStand.position);
            }
        }
    }
}
