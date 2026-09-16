using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>조리 타이머를 무엇이 밀어주는가. 기구들 사이의 유일한 실질적 차이다.</summary>
public enum StationDriveMode
{
    /// <summary>E로 시작한 뒤 알아서 돈다. 자리를 떠도 된다. (오븐)</summary>
    PressAndWait,

    /// <summary>E로 시작하되 바라보는 동안에만 진행한다. (커피 머신)</summary>
    PressAndStay,

    /// <summary>E를 누르고 있는 것 자체가 진행이다. 그동안 아무것도 못 한다. (블렌더)</summary>
    HoldToRun,
}

/// <summary>스테이션이 한 사이클 중 어디쯤인지.</summary>
public enum StationState
{
    /// <summary>재료를 받는 중. 조리는 안 하고 있다.</summary>
    Idle,

    /// <summary>조리 중.</summary>
    Processing,

    /// <summary>완성품이 올라와 있고 회수를 기다린다.</summary>
    Done,
}

/// <summary>
/// 모든 기구가 공유하는 동작: 재료를 담고, 레시피를 맞추고, 타이머를 돌리고, 완성품을 내준다.
///
/// 스테이션은 모자를 셋 쓴다. 상호작용 동사를 인터페이스로 쪼갠 이유가 바로 이것이다.
///   IItemReceiver  — 든 손 좌클릭: 재료를 넣는다
///   IInteractable  — E: 작동시킨다
///   IItemSource    — 빈 손 좌클릭: 완성품을 꺼낸다 (또는 재료를 되돌려 받는다)
///
/// 하위 클래스는 On... 훅으로 연출(색·소리·애니메이션)만 얹는다. 상태 기계는 건드릴 일이 없다.
/// </summary>
public abstract class StationBase : InteractableBase, IItemSource, IItemReceiver
{
    // ---------------------------------------------------------------- 인스펙터

    [Header("스테이션 설정")]
    [Tooltip("이 스테이션의 종류. 같은 종류의 레시피만 처리합니다.")]
    [SerializeField] private StationKind kind;

    [Tooltip("진행 방식. 커피 머신은 PressAndStay, 오븐은 PressAndWait, 블렌더는 HoldToRun.")]
    [SerializeField] private StationDriveMode driveMode = StationDriveMode.PressAndStay;

    [Tooltip("모든 레시피가 담긴 레시피북. 씬의 모든 스테이션이 같은 걸 참조합니다.")]
    [SerializeField] private RecipeBook recipeBook;

    [Tooltip("동시에 넣어둘 수 있는 재료 최대 개수.")]
    [SerializeField] private int maxIngredients = 4;

    [Header("위치")]
    [Tooltip("완성품이 놓일 위치. 비워두면 스테이션 자신의 위치를 씁니다.")]
    [SerializeField] private Transform outputPoint;

    [Header("흔들림")]
    [Tooltip("조리 중에 흔들 부분. 비워두면 흔들지 않습니다. 보통 이 오브젝트 자신을 넣습니다.")]
    [SerializeField] private Transform shakeTarget;

    [Tooltip("흔들림 크기 (m). 0.02면 2cm 정도로 은근합니다.")]
    [SerializeField] private float shakeAmount = 0.02f;

    [Tooltip("흔들림 속도. 클수록 잘게 떱니다.")]
    [SerializeField] private float shakeSpeed = 40f;

    [Header("문구")]
    [Tooltip("재료가 하나도 없을 때.")]
    [SerializeField] private string emptyPrompt = "재료를 넣으세요";

    [Tooltip("재료 조합이 어떤 레시피에도 맞지 않을 때.")]
    [SerializeField] private string wrongPrompt = "재료 조합이 맞지 않음";

    [Tooltip("작동 가능할 때.")]
    [SerializeField] private string startPrompt = "작동시키기";

    [Tooltip("조리 중일 때.")]
    [SerializeField] private string busyPrompt = "조리 중...";

    // ---------------------------------------------------------------- 상태

    private readonly List<ItemData> _loaded = new();
    private RecipeData _pending;      // 지금 담긴 재료로 만들 수 있는 레시피
    private RecipeData _active;       // 지금 조리 중인 레시피
    private float _timer;
    private WorldItem _output;
    private bool _focused;
    private Vector3 _shakeOrigin;

    /// <summary>지금 사이클의 단계.</summary>
    public StationState State { get; private set; } = StationState.Idle;

    /// <summary>현재 담겨 있는 재료들.</summary>
    public IReadOnlyList<ItemData> Loaded => _loaded;

    /// <summary>조리 진행도 0~1. 조리 중이 아니면 0.</summary>
    public float Progress01 =>
        State == StationState.Processing && _active != null
            ? Mathf.Clamp01(_timer / _active.Duration)
            : 0f;

    public StationKind Kind => kind;

    /// <summary>상태가 바뀔 때마다. UI용.</summary>
    public event Action<StationState> StateChanged;

    /// <summary>조리 중 진행도 0~1.</summary>
    public event Action<float> ProgressChanged;

    // ---------------------------------------------------------------- IInteractable

    public override string Prompt
    {
        get
        {
            switch (State)
            {
                case StationState.Processing:
                    return busyPrompt;

                // 완성 후에는 E가 할 일이 없다. 회수는 좌클릭 담당이고 그쪽 프롬프트가 이미
                // 안내한다. 빈 문자열을 주면 프롬프트 목록에서 줄 자체가 빠진다.
                case StationState.Done:
                    return string.Empty;

                default:
                    if (_loaded.Count == 0)
                    {
                        return emptyPrompt;
                    }
                    return _pending != null ? startPrompt : wrongPrompt;
            }
        }
    }

    // HoldToRun은 조준 쪽의 홀드 타이머가 곧 조리 타이머다. 그래서 레시피 시간을 그대로
    // 넘기고, 홀드가 끝나면 Interact가 불리게 둔다.
    public override float HoldDuration =>
        driveMode == StationDriveMode.HoldToRun && State == StationState.Idle && _pending != null
            ? _pending.Duration
            : 0f;

    public override bool CanInteract(PlayerInteractor interactor)
    {
        return base.CanInteract(interactor) && State == StationState.Idle && _pending != null;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (State != StationState.Idle || _pending == null)
        {
            OnStartRejected();
            return;
        }

        _active = _pending;
        _timer = 0f;

        if (driveMode == StationDriveMode.HoldToRun)
        {
            // 홀드로 이미 전체 시간을 채웠으므로 도착하자마자 완성이다.
            Complete();
            return;
        }

        SetState(StationState.Processing);
        OnProcessingStarted(_active);
    }

    public override void OnHoldProgress(PlayerInteractor interactor, float normalized)
    {
        // HoldToRun만 여기 들어온다. 나머지는 HoldDuration을 0으로 보고하기 때문.
        OnProcessingProgress(normalized);
        ApplyShake(normalized);
        ProgressChanged?.Invoke(normalized);
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        OnProcessingProgress(0f);
        ApplyShake(0f);
        ProgressChanged?.Invoke(0f);
    }

    public override void OnFocusEnter(PlayerInteractor interactor)
    {
        base.OnFocusEnter(interactor);
        _focused = true;
    }

    public override void OnFocusExit(PlayerInteractor interactor)
    {
        base.OnFocusExit(interactor);
        _focused = false;
    }

    // ---------------------------------------------------------------- IItemReceiver

    public bool CanReceive(ItemData item, PlayerHands hands)
    {
        // Category 검사를 일부러 넣지 않는다. AnyAccepts가 이미 "어떤 레시피의 재료로 쓰이는
        // 것"만 통과시키고, Ingredient를 강제하면 연쇄 레시피의 중간 산출물(Container)이
        // 막힌다 — 반죽대에서 나온 반죽이 오븐에 못 들어가게 된다.
        return State == StationState.Idle
               && _output == null
               && item != null
               && _loaded.Count < maxIngredients
               && recipeBook != null
               && recipeBook.AnyAccepts(kind, item, _loaded);
    }

    public void Receive(WorldItem item, PlayerHands hands)
    {
        if (item == null)
        {
            return;
        }

        ItemData data = item.Item;
        _loaded.Add(data);

        // 스테이션은 무엇이 들어갔는지만 기억한다. 실물은 사라진다.
        Destroy(item.gameObject);

        RecomputePending();
        OnIngredientReceived(data);
    }

    // ---------------------------------------------------------------- IItemSource

    public ItemData ProvidedItem
    {
        get
        {
            if (_output != null)
            {
                return _output.Item;
            }

            return State == StationState.Idle && _loaded.Count > 0 ? _loaded[_loaded.Count - 1] : null;
        }
    }

    public bool CanProvide(PlayerHands hands)
    {
        return _output != null || (State == StationState.Idle && _loaded.Count > 0);
    }

    public GameObject Provide(PlayerHands hands)
    {
        // 완성품이 있으면 그것부터.
        if (_output != null)
        {
            WorldItem taken = _output;
            _output = null;

            taken.transform.SetParent(null, true);
            ItemData data = taken.Item;

            SetState(StationState.Idle);
            OnOutputTaken(data);
            return taken.gameObject;
        }

        // 없으면 마지막 재료를 도로 꺼내준다. 잘못 넣었을 때의 되돌리기 수단.
        if (State == StationState.Idle && _loaded.Count > 0)
        {
            int last = _loaded.Count - 1;
            ItemData data = _loaded[last];
            _loaded.RemoveAt(last);
            RecomputePending();

            if (data == null || data.WorldPrefab == null)
            {
                return null;
            }

            Transform origin = outputPoint != null ? outputPoint : transform;
            GameObject spawned = Instantiate(data.WorldPrefab, origin.position, origin.rotation);
            spawned.name = data.DisplayName;

            OnIngredientReturned(data);
            return spawned;
        }

        return null;
    }

    // ---------------------------------------------------------------- 수명주기

    protected virtual void Awake()
    {
        if (recipeBook == null)
        {
            Debug.LogError($"{name}: 레시피북이 연결되지 않아 이 스테이션은 아무것도 만들 수 없습니다.", this);
        }

        if (shakeTarget != null)
        {
            _shakeOrigin = shakeTarget.localPosition;
        }
    }

    protected virtual void Update()
    {
        if (State != StationState.Processing || _active == null)
        {
            return;
        }

        // 명세의 "자리를 지켜야 함" — 커피 머신은 고개를 돌리는 순간 멈춘다.
        if (driveMode == StationDriveMode.PressAndStay && !_focused)
        {
            return;
        }

        _timer += Time.deltaTime;

        float progress = Mathf.Clamp01(_timer / _active.Duration);
        OnProcessingProgress(progress);
        ApplyShake(progress);
        ProgressChanged?.Invoke(progress);

        if (_timer >= _active.Duration)
        {
            Complete();
        }
    }

    // ---------------------------------------------------------------- 내부

    private void Complete()
    {
        RecipeData recipe = _active;
        _active = null;
        _timer = 0f;
        ApplyShake(0f);
        _loaded.Clear();
        RecomputePending();

        if (recipe != null && recipe.Output != null && recipe.Output.WorldPrefab != null)
        {
            Transform origin = outputPoint != null ? outputPoint : transform;

            // 일부러 부모로 붙이지 않는다. 기구들이 비균일 스케일 큐브라, 어떤 부모 모드를
            // 써도 완성품이 찌그러진다.
            GameObject spawned = Instantiate(recipe.Output.WorldPrefab,
                                             origin.position,
                                             Quaternion.Euler(0f, origin.eulerAngles.y, 0f));
            spawned.name = recipe.Output.DisplayName;

            _output = spawned.GetComponent<WorldItem>();
            if (_output != null)
            {
                // 들린 게 아니라 얹힌 상태. 물리를 꺼야 가만히 있고 조준도 안 가린다.
                _output.SetCarried(true);
                SetState(StationState.Done);
            }
            else
            {
                Debug.LogError($"완성품 프리팹 '{spawned.name}'에 {nameof(WorldItem)}이 없습니다.", this);
                Destroy(spawned);
                SetState(StationState.Idle);
            }
        }
        else
        {
            SetState(StationState.Idle);
        }

        OnCompleted(recipe);
    }

    /// <summary>
    /// 조리 중 기계를 떨게 한다. 세기가 진행도를 따라가서 끝으로 갈수록 강해진다.
    /// 0을 넘기면 원위치로 스냅.
    /// </summary>
    private void ApplyShake(float strength)
    {
        if (shakeTarget == null || shakeAmount <= 0f)
        {
            return;
        }

        if (strength <= 0f)
        {
            shakeTarget.localPosition = _shakeOrigin;
            return;
        }

        float offset = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount * strength;
        shakeTarget.localPosition = _shakeOrigin + new Vector3(offset, 0f, offset * 0.5f);
    }

    private void RecomputePending()
    {
        _pending = recipeBook != null ? recipeBook.FindMatch(kind, _loaded) : null;
    }

    private void SetState(StationState next)
    {
        if (State == next)
        {
            return;
        }

        State = next;
        StateChanged?.Invoke(next);
    }

    // ---------------------------------------------------------------- 하위 클래스 훅

    /// <summary>재료가 하나 들어왔다.</summary>
    protected virtual void OnIngredientReceived(ItemData item) { }

    /// <summary>재료를 도로 꺼냈다.</summary>
    protected virtual void OnIngredientReturned(ItemData item) { }

    /// <summary>조리를 시작했다. 즉시 완성되는 HoldToRun에서는 불리지 않는다.</summary>
    protected virtual void OnProcessingStarted(RecipeData recipe) { }

    /// <summary>조리 진행도 0~1.</summary>
    protected virtual void OnProcessingProgress(float normalized) { }

    /// <summary>조리가 끝나 완성품이 생겼다.</summary>
    protected virtual void OnCompleted(RecipeData recipe) { }

    /// <summary>플레이어가 완성품을 가져갔다.</summary>
    protected virtual void OnOutputTaken(ItemData item) { }

    /// <summary>E를 눌렀지만 시작할 수 없었다. 실패음을 넣기 좋은 자리.</summary>
    protected virtual void OnStartRejected() { }

    protected virtual void OnValidate()
    {
        maxIngredients = Mathf.Max(1, maxIngredients);
    }
}
