using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Which lane a new customer walks up to.</summary>
public enum SpotPickMode
{
    /// <summary>Any free lane, chosen at random. Spreads customers around the counter.</summary>
    Random,

    /// <summary>The first free lane in the array. Fills left to right, predictable for testing.</summary>
    FirstFree,
}

/// <summary>
/// Sends customers to the counter on a timer. Owns the prefab, the pacing and the
/// difficulty knobs, so the lanes themselves stay dumb — a <see cref="ServingSpot"/> only
/// knows whether it is free.
///
/// Every number here is meant to be played with. Nothing about the pacing is baked into
/// code: interval, patience, how many at once and how hard it ramps are all fields.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("손님을 보낼 자리들. 보통 카운터의 레인 3개.")]
    [SerializeField] private ServingSpot[] spots;

    [Tooltip("손님 프리팹. 여러 개 넣으면 매번 랜덤으로 고릅니다.")]
    [SerializeField] private GameObject[] customerPrefabs;

    [Header("타이밍")]
    [Tooltip("시작 후 첫 손님까지 걸리는 시간 (초).")]
    [SerializeField] private float firstSpawnDelay = 2f;

    [Tooltip("손님 사이 간격 (초). 이 범위에서 매번 랜덤으로 뽑습니다. x = 최소, y = 최대.")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(6f, 10f);

    [Tooltip("자리가 다 찼을 때 다시 시도하기까지의 간격 (초).")]
    [SerializeField] private float retryInterval = 1f;

    [Tooltip("켜면 플레이와 동시에 손님을 보내기 시작합니다.")]
    [SerializeField] private bool spawnOnStart = true;

    [Header("인원")]
    [Tooltip("동시에 카운터에 있을 수 있는 최대 인원. 자리 수보다 클 수 없습니다.")]
    [SerializeField] private int maxConcurrent = 3;

    [Tooltip("하루에 보낼 총 손님 수. 0이면 무제한입니다.")]
    [SerializeField] private int totalCustomers;

    [Tooltip("빈 자리를 고르는 방식.")]
    [SerializeField] private SpotPickMode pickMode = SpotPickMode.Random;

    [Header("주문 개수")]
    [Tooltip("주문 개수를 뽑는 가중치. 첫 칸이 1개, 둘째가 2개, 셋째가 3개일 확률입니다. " +
             "UI 칸이 3개뿐이라 그 이상은 무시됩니다.")]
    [SerializeField] private float[] orderCountWeights = { 50f, 35f, 15f };

    [Tooltip("주문 개수에 따라 인내심에 곱할 배율. 첫 칸이 1개, 둘째가 2개, 셋째가 3개입니다. " +
             "많이 시킨 손님은 그만큼 더 기다려줍니다.")]
    [SerializeField] private float[] patienceMultipliers = { 1f, 1.5f, 2f };

    [Header("인내심")]
    [Tooltip("손님이 기다려주는 시간 (초). 이 범위에서 매번 랜덤으로 뽑습니다.")]
    [SerializeField] private Vector2 patienceRange = new Vector2(25f, 40f);

    [Header("난이도 상승")]
    [Tooltip("손님이 한 명 올 때마다 간격에 곱할 비율. 1이면 그대로, 0.97이면 점점 빨라집니다.")]
    [Range(0.8f, 1f)]
    [SerializeField] private float intervalDecay = 1f;

    [Tooltip("간격이 이 아래로는 내려가지 않습니다 (초).")]
    [SerializeField] private float minInterval = 3f;

    [Tooltip("손님이 한 명 올 때마다 인내심에 곱할 비율. 1이면 그대로.")]
    [Range(0.8f, 1f)]
    [SerializeField] private float patienceDecay = 1f;

    [Tooltip("인내심이 이 아래로는 내려가지 않습니다 (초).")]
    [SerializeField] private float minPatience = 12f;

    /// <summary>손님 머리 위와 벽 메뉴판에 준비된 칸 수. 이보다 많이 시킬 수는 없다.</summary>
    public const int MAX_ORDER_ITEMS = 3;

    private readonly List<ServingSpot> _free = new();
    private float _timer;

    /// <summary>How many customers have been sent so far.</summary>
    public int SpawnedCount { get; private set; }

    /// <summary>False while paused, or once the day's quota has been sent.</summary>
    public bool IsSpawning { get; private set; }

    /// <summary>True when the quota is used up and no customers remain at the counter.</summary>
    public bool IsFinished =>
        totalCustomers > 0 && SpawnedCount >= totalCustomers && OccupiedCount() == 0;

    /// <summary>Fires for each customer sent, with the running total.</summary>
    public event Action<int> CustomerSpawned;

    /// <summary>Fires once when the last customer of the day has left.</summary>
    public event Action AllCustomersDone;

    private bool _finishedReported;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (spots == null || spots.Length == 0)
        {
            Debug.LogError($"{nameof(CustomerSpawner)}: no serving spots assigned.", this);
            enabled = false;
            return;
        }

        if (customerPrefabs == null || customerPrefabs.Length == 0)
        {
            Debug.LogError($"{nameof(CustomerSpawner)}: no customer prefab assigned.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            Begin();
        }
    }

    private void Update()
    {
        if (!_finishedReported && IsFinished)
        {
            _finishedReported = true;
            IsSpawning = false;
            AllCustomersDone?.Invoke();
            return;
        }

        if (!IsSpawning)
        {
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        // Quota reached: stop asking, but keep running so IsFinished can fire once the
        // customers still at the counter have been dealt with.
        if (totalCustomers > 0 && SpawnedCount >= totalCustomers)
        {
            IsSpawning = false;
            return;
        }

        if (TrySpawnOne())
        {
            _timer = NextInterval();
        }
        else
        {
            // Counter is full. Check back shortly rather than burning the whole interval.
            _timer = Mathf.Max(0.1f, retryInterval);
        }
    }

    // ---------------------------------------------------------------- control

    /// <summary>Starts sending customers. The day clock will call this in step 6.</summary>
    public void Begin()
    {
        IsSpawning = true;
        _timer = Mathf.Max(0f, firstSpawnDelay);
    }

    /// <summary>Stops sending customers. Anyone already at the counter stays.</summary>
    public void Pause() => IsSpawning = false;

    /// <summary>Resumes without resetting the difficulty ramp.</summary>
    public void Resume()
    {
        IsSpawning = true;
        _timer = Mathf.Max(_timer, 0f);
    }

    /// <summary>Sends one customer immediately, ignoring the timer. Handy while tuning.</summary>
    [ContextMenu("지금 한 명 보내기")]
    public void SpawnNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("플레이 중에만 소환됩니다.", this);
            return;
        }

        TrySpawnOne();
    }

    // ---------------------------------------------------------------- spawning

    private bool TrySpawnOne()
    {
        if (OccupiedCount() >= EffectiveMaxConcurrent())
        {
            return false;
        }

        ServingSpot spot = PickSpot();
        if (spot == null)
        {
            return false;
        }

        GameObject prefab = customerPrefabs[UnityEngine.Random.Range(0, customerPrefabs.Length)];
        // 개수를 먼저 뽑아야 한다. 인내심이 그 개수에 딸려 있기 때문이다.
        int orderCount = NextOrderCount();

        if (!spot.TrySeatCustomer(prefab, NextPatience(orderCount), orderCount))
        {
            return false;
        }

        SpawnedCount++;
        CustomerSpawned?.Invoke(SpawnedCount);
        return true;
    }

    private ServingSpot PickSpot()
    {
        _free.Clear();

        foreach (ServingSpot spot in spots)
        {
            if (spot != null && spot.IsFree)
            {
                if (pickMode == SpotPickMode.FirstFree)
                {
                    return spot;
                }

                _free.Add(spot);
            }
        }

        if (_free.Count == 0)
        {
            return null;
        }

        return _free[UnityEngine.Random.Range(0, _free.Count)];
    }

    private int OccupiedCount()
    {
        int count = 0;
        foreach (ServingSpot spot in spots)
        {
            if (spot != null && spot.HasCustomer)
            {
                count++;
            }
        }

        return count;
    }

    private int EffectiveMaxConcurrent()
    {
        return Mathf.Clamp(maxConcurrent, 1, spots.Length);
    }

    /// <summary>Interval for the next customer, shortened by however far the ramp has gone.</summary>
    private float NextInterval()
    {
        float ramp = Mathf.Pow(intervalDecay, SpawnedCount);
        float raw = UnityEngine.Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        return Mathf.Max(minInterval, raw * ramp);
    }

    /// <summary>
    /// 다음 손님이 시킬 메뉴 개수. 가중치 배열에서 룰렛으로 뽑는다.
    /// 가중치를 비워두면 1개다 — 설정 실수로 주문이 안 나오는 것보다 낫다.
    /// </summary>
    private int NextOrderCount()
    {
        if (orderCountWeights == null || orderCountWeights.Length == 0)
        {
            return 1;
        }

        int slots = Mathf.Min(orderCountWeights.Length, MAX_ORDER_ITEMS);

        float total = 0f;
        for (int i = 0; i < slots; i++)
        {
            total += Mathf.Max(0f, orderCountWeights[i]);
        }

        if (total <= 0f)
        {
            return 1;
        }

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < slots; i++)
        {
            roll -= Mathf.Max(0f, orderCountWeights[i]);
            if (roll <= 0f)
            {
                return i + 1;
            }
        }

        return slots;
    }

    /// <summary>
    /// 다음 손님의 인내심. 난이도 램프로 깎은 뒤, 주문 개수만큼 늘려준다.
    /// 세 개를 시킨 손님에게 한 개짜리와 같은 시간을 주면 사실상 불가능한 주문이 된다.
    /// </summary>
    private float NextPatience(int orderCount)
    {
        float ramp = Mathf.Pow(patienceDecay, SpawnedCount);
        float raw = UnityEngine.Random.Range(patienceRange.x, patienceRange.y);
        float baseline = Mathf.Max(minPatience, raw * ramp);

        return baseline * PatienceMultiplier(orderCount);
    }

    /// <summary>개수에 해당하는 배율. 표가 짧으면 마지막 칸으로 버틴다.</summary>
    private float PatienceMultiplier(int orderCount)
    {
        if (patienceMultipliers == null || patienceMultipliers.Length == 0)
        {
            return 1f;
        }

        int index = Mathf.Clamp(orderCount - 1, 0, patienceMultipliers.Length - 1);
        return Mathf.Max(0.1f, patienceMultipliers[index]);
    }

    // ---------------------------------------------------------------- editor

    private void OnValidate()
    {
        firstSpawnDelay = Mathf.Max(0f, firstSpawnDelay);
        retryInterval = Mathf.Max(0.1f, retryInterval);
        maxConcurrent = Mathf.Max(1, maxConcurrent);
        totalCustomers = Mathf.Max(0, totalCustomers);
        minInterval = Mathf.Max(0.5f, minInterval);
        minPatience = Mathf.Max(1f, minPatience);

        if (orderCountWeights != null)
        {
            for (int i = 0; i < orderCountWeights.Length; i++)
            {
                orderCountWeights[i] = Mathf.Max(0f, orderCountWeights[i]);
            }
        }

        if (patienceMultipliers != null)
        {
            for (int i = 0; i < patienceMultipliers.Length; i++)
            {
                patienceMultipliers[i] = Mathf.Max(0.1f, patienceMultipliers[i]);
            }
        }

        spawnIntervalRange = SortedRange(spawnIntervalRange, 0.5f);
        patienceRange = SortedRange(patienceRange, 1f);
    }

    private static Vector2 SortedRange(Vector2 range, float floor)
    {
        float min = Mathf.Max(floor, range.x);
        float max = Mathf.Max(floor, range.y);
        return min <= max ? new Vector2(min, max) : new Vector2(max, min);
    }
}
