using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How the processing timer is driven. This is the only real difference between the appliances.</summary>
public enum StationDriveMode
{
    /// <summary>E to start, then it runs on its own. You can walk away. (오븐)</summary>
    PressAndWait,

    /// <summary>E to start, but the timer only advances while you keep looking at it. (커피 머신)</summary>
    PressAndStay,

    /// <summary>Hold E; the hold itself is the progress. Blocks you completely. (블렌더)</summary>
    HoldToRun,
}

/// <summary>Where a station is in its cycle.</summary>
public enum StationState
{
    /// <summary>Accepting ingredients, nothing cooking.</summary>
    Idle,

    /// <summary>Cooking.</summary>
    Processing,

    /// <summary>Finished dish sitting on the station, waiting to be taken.</summary>
    Done,
}

/// <summary>
/// Shared behaviour for every appliance: hold ingredients, match a recipe, run a timer,
/// produce a dish, hand it over.
///
/// A station wears three hats at once, which is the whole reason the interaction verbs
/// were split into interfaces:
///   IItemReceiver  — 좌클릭 with full hands: put an ingredient in
///   IInteractable  — E: run it
///   IItemSource    — 좌클릭 with empty hands: take the dish out (or pull an ingredient back)
///
/// Subclasses exist to add feedback (colour, sound, animation) through the On... hooks;
/// none of them need to touch the state machine.
/// </summary>
public abstract class StationBase : InteractableBase, IItemSource, IItemReceiver
{
    // ---------------------------------------------------------------- inspector

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

    [Header("문구")]
    [Tooltip("재료가 하나도 없을 때.")]
    [SerializeField] private string emptyPrompt = "재료를 넣으세요";

    [Tooltip("재료 조합이 어떤 레시피에도 맞지 않을 때.")]
    [SerializeField] private string wrongPrompt = "재료 조합이 맞지 않음";

    [Tooltip("작동 가능할 때.")]
    [SerializeField] private string startPrompt = "작동시키기";

    [Tooltip("조리 중일 때.")]
    [SerializeField] private string busyPrompt = "조리 중...";

    // ---------------------------------------------------------------- state

    private readonly List<ItemData> _loaded = new();
    private RecipeData _pending;      // recipe that the current load would make, if any
    private RecipeData _active;       // recipe currently cooking
    private float _timer;
    private WorldItem _output;
    private bool _focused;

    /// <summary>Current phase of the cycle.</summary>
    public StationState State { get; private set; } = StationState.Idle;

    /// <summary>Ingredients sitting in the station right now.</summary>
    public IReadOnlyList<ItemData> Loaded => _loaded;

    /// <summary>Cooking progress from 0 to 1. Zero unless processing.</summary>
    public float Progress01 =>
        State == StationState.Processing && _active != null
            ? Mathf.Clamp01(_timer / _active.Duration)
            : 0f;

    public StationKind Kind => kind;

    /// <summary>Fires on every state change, for UI.</summary>
    public event Action<StationState> StateChanged;

    /// <summary>Fires while cooking, 0 to 1.</summary>
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

                // Nothing for E to do once the dish is ready — taking it is a left click,
                // and the left-click prompt already says so. An empty string drops the
                // line from the prompt list entirely.
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

    // For HoldToRun the interactor's own hold timer *is* the cooking timer, so we report
    // the recipe duration and let Interact fire when the hold completes.
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
            // The hold already took the full duration, so it is finished on arrival.
            Complete();
            return;
        }

        SetState(StationState.Processing);
        OnProcessingStarted(_active);
    }

    public override void OnHoldProgress(PlayerInteractor interactor, float normalized)
    {
        // Only HoldToRun stations get here, since everyone else reports HoldDuration 0.
        OnProcessingProgress(normalized);
        ProgressChanged?.Invoke(normalized);
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        OnProcessingProgress(0f);
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
        return State == StationState.Idle
               && _output == null
               && item != null
               && item.Category == ItemCategory.Ingredient
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

        // The station only tracks what went in; the physical object is gone.
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
        // Finished dish first.
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

        // Otherwise pull the last ingredient back out — the way to undo a mistake.
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

    // ---------------------------------------------------------------- lifecycle

    protected virtual void Awake()
    {
        if (recipeBook == null)
        {
            Debug.LogError($"{name}: Recipe Book is not assigned, so this station can never cook.", this);
        }
    }

    protected virtual void Update()
    {
        if (State != StationState.Processing || _active == null)
        {
            return;
        }

        // "자리를 지켜야 함" — the coffee machine stops the moment you look away.
        if (driveMode == StationDriveMode.PressAndStay && !_focused)
        {
            return;
        }

        _timer += Time.deltaTime;

        float progress = Mathf.Clamp01(_timer / _active.Duration);
        OnProcessingProgress(progress);
        ProgressChanged?.Invoke(progress);

        if (_timer >= _active.Duration)
        {
            Complete();
        }
    }

    // ---------------------------------------------------------------- internals

    private void Complete()
    {
        RecipeData recipe = _active;
        _active = null;
        _timer = 0f;
        _loaded.Clear();
        RecomputePending();

        if (recipe != null && recipe.Output != null && recipe.Output.WorldPrefab != null)
        {
            Transform origin = outputPoint != null ? outputPoint : transform;

            // Left unparented on purpose: appliances are non-uniformly scaled cubes, and
            // attaching to one squashes the dish no matter which parenting mode is used.
            GameObject spawned = Instantiate(recipe.Output.WorldPrefab,
                                             origin.position,
                                             Quaternion.Euler(0f, origin.eulerAngles.y, 0f));
            spawned.name = recipe.Output.DisplayName;

            _output = spawned.GetComponent<WorldItem>();
            if (_output != null)
            {
                // Parked, not carried: physics off so it sits still and does not block aim.
                _output.SetCarried(true);
                SetState(StationState.Done);
            }
            else
            {
                Debug.LogError($"Output prefab '{spawned.name}' has no {nameof(WorldItem)}.", this);
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

    // ---------------------------------------------------------------- subclass hooks

    /// <summary>An ingredient went in.</summary>
    protected virtual void OnIngredientReceived(ItemData item) { }

    /// <summary>An ingredient was pulled back out.</summary>
    protected virtual void OnIngredientReturned(ItemData item) { }

    /// <summary>Cooking started. Not called for HoldToRun, which completes instantly.</summary>
    protected virtual void OnProcessingStarted(RecipeData recipe) { }

    /// <summary>Cooking progress, 0 to 1.</summary>
    protected virtual void OnProcessingProgress(float normalized) { }

    /// <summary>Cooking finished and the dish exists.</summary>
    protected virtual void OnCompleted(RecipeData recipe) { }

    /// <summary>The player took the finished dish.</summary>
    protected virtual void OnOutputTaken(ItemData item) { }

    /// <summary>E was pressed but nothing could start. Good place for a buzz.</summary>
    protected virtual void OnStartRejected() { }

    protected virtual void OnValidate()
    {
        maxIngredients = Mathf.Max(1, maxIngredients);
    }
}
