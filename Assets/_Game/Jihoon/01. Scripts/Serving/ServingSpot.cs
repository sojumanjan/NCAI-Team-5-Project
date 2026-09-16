using System;
using UnityEngine;

/// <summary>카운터 한 레인이 어느 단계에 있는지.</summary>
public enum ServingSpotState
{
    /// <summary>손님이 없다. 할 일도 없다.</summary>
    Free,

    /// <summary>손님이 도착해 주문을 말했지만 아직 수락 전.</summary>
    AwaitingAccept,

    /// <summary>주문 수락됨. 주방이 음식을 빚졌다.</summary>
    Accepted,
}

/// <summary>주문이 어떻게 끝났는지. 평점을 움직이는 모든 경우가 여기 들어 있다.</summary>
public enum OrderResult
{
    /// <summary>주문대로 정확히 만들어 줬다.</summary>
    Correct,

    /// <summary>주긴 줬는데 다른 음식이었다.</summary>
    Wrong,

    /// <summary>손님이 기다리다 지쳐 떠났다.</summary>
    Abandoned,
}

/// <summary>
/// 카운터의 한 레인: 포스기, 트레이, 손님이 설 자리. 셋이 있어서 동시에 세 명을 받는다.
///
/// 이 클래스가 조정자다. 포스기와 트레이는 얇게 두고 전부 여기로 넘기므로, 주문 규칙이
/// 한 파일에만 있다. 레인 밖에서 듣는 창구도 여기 하나다 — 손님은 런타임에 생성돼서
/// 인스펙터로 연결할 수 없기 때문에, 레인이 <see cref="OrderResolved"/>로 대신 알린다.
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

    private Customer _customer;

    /// <summary>이 레인의 현재 단계.</summary>
    public ServingSpotState State { get; private set; } = ServingSpotState.Free;

    /// <summary>지금 손님이 시킨 것. 없으면 null.</summary>
    public ItemData CurrentOrder { get; private set; }

    /// <summary>손님이 서는 자리.</summary>
    public Transform CustomerStand => customerStand;

    /// <summary>손님이 걸어 나가는 곳.</summary>
    public Transform ExitPoint => exitPoint;

    /// <summary>손님이 있는지.</summary>
    public bool HasCustomer => _customer != null;

    /// <summary>새 손님을 보낼 수 있는지.</summary>
    public bool IsFree => _customer == null;

    /// <summary>기다리는 손님의 인내심 1~0. 아무도 없으면 0.</summary>
    public float Patience01 => _customer != null ? _customer.Patience01 : 0f;

    /// <summary>손님이 실제로 서서 기다리는 동안에만 참.</summary>
    public bool IsCustomerWaiting => _customer != null && _customer.IsWaiting;

    /// <summary>손님이 주문을 말했을 때.</summary>
    public event Action<ServingSpot, ItemData> OrderPlaced;

    /// <summary>플레이어가 포스기에서 주문을 수락했을 때.</summary>
    public event Action<ServingSpot, ItemData> OrderAccepted;

    /// <summary>
    /// 주문 하나가 어떻게 끝났든 한 번 발생한다. 평점 시스템과 하루 집계가 듣는 유일한
    /// 창구 — 실패 유형마다 이벤트를 두지 않고 하나로 합쳤다.
    /// </summary>
    public event Action<ServingSpot, OrderResult> OrderResolved;

    // ---------------------------------------------------------------- 수명주기

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
            Debug.LogError($"{name}: 손님 대기 위치(Customer Stand)가 연결되지 않았습니다.", this);
        }
    }

    // ---------------------------------------------------------------- 손님 흐름

    /// <summary>
    /// 이 레인으로 손님을 보낸다. 프리팹과 타이밍을 쥔 <see cref="CustomerSpawner"/>가 호출한다.
    /// 레인은 자기가 비었는지만 안다. 이미 차 있거나 설정이 덜 됐으면 false.
    /// </summary>
    public bool TrySeatCustomer(GameObject customerPrefab, float patienceSeconds)
    {
        if (!IsFree || customerPrefab == null || customerStand == null)
        {
            return false;
        }

        Vector3 spawnAt = exitPoint != null ? exitPoint.position : customerStand.position;
        GameObject go = Instantiate(customerPrefab, spawnAt, Quaternion.identity);

        Customer customer = go.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogError($"손님 프리팹 '{go.name}'에 {nameof(Customer)}가 없습니다.", this);
            Destroy(go);
            return false;
        }

        _customer = customer;
        customer.Arrive(this, PickOrder(), patienceSeconds);
        return true;
    }

    private ItemData PickOrder()
    {
        return recipeBook != null ? recipeBook.GetRandomOutput() : null;
    }

    /// <summary>손님이 카운터에 도착하면 손님 쪽에서 부른다.</summary>
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

    /// <summary>손님이 기다리다 포기했다. 인내심 타이머가 0이 되면 손님 쪽에서 부른다.</summary>
    public void AbandonOrder()
    {
        if (State == ServingSpotState.Free)
        {
            return;
        }

        Resolve(OrderResult.Abandoned);
    }

    // ---------------------------------------------------------------- 포스기

    /// <summary>포스기를 눌렀을 때 할 일이 있는지.</summary>
    public bool CanAcceptOrder() => State == ServingSpotState.AwaitingAccept;

    /// <summary>플레이어가 포스기를 눌렀다. 주문을 받아야 트레이가 음식을 받기 시작한다.</summary>
    public void AcceptOrder()
    {
        if (!CanAcceptOrder())
        {
            return;
        }

        State = ServingSpotState.Accepted;
        OrderAccepted?.Invoke(this, CurrentOrder);
    }

    // ---------------------------------------------------------------- 트레이

    /// <summary>
    /// 이 음식을 트레이에 올릴 수 있는지. 재료는 아예 거부하고, 포스기에서 주문을 받기
    /// 전에는 무엇도 받지 않는다.
    /// </summary>
    public bool CanReceiveDish(ItemData dish)
    {
        return State == ServingSpotState.Accepted
               && dish != null
               && dish.Category == ItemCategory.Dish;
    }

    /// <summary>
    /// 음식이 트레이에 올라왔다. 틀린 음식도 받는다 — 감점이 목적이고, 거부하면 플레이어가
    /// 그걸 들고 오도 가도 못 한다.
    /// </summary>
    public void DeliverDish(WorldItem dish)
    {
        if (dish == null || !CanReceiveDish(dish.Item))
        {
            return;
        }

        bool correct = dish.Item == CurrentOrder;

        // 판정 전에 먼저 얹어야 화면과 점수가 같은 타이밍에 움직인다.
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

    // ---------------------------------------------------------------- 내부

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

    // ---------------------------------------------------------------- 기즈모

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
