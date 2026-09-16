using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player's single pair of hands: one item at a time, driven by left click.
/// Deliberately separate from <see cref="PlayerInteractor"/> because the design splits the
/// verbs — 좌클릭 for moving things around, E for operating stations.
///
/// Left click means three different things depending on context:
///   empty hands, aiming at an IItemSource    → take it
///   full hands,  aiming at an IItemReceiver  → put it in
///   full hands,  anything else               → put it down
///
/// One slot is a rule, not a limitation to work around: it is what forces the player to
/// walk back and forth, which is the whole game.
/// </summary>
public class PlayerHands : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("아이템이 붙을 위치. 카메라 자식으로 두면 화면에 들고 있는 것처럼 보입니다.")]
    [SerializeField] private Transform holdAnchor;

    [Tooltip("조준 대상을 알기 위해 참조. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private PlayerInteractor interactor;

    [Header("내려놓기")]
    [Tooltip("표면에 놓을 때 바닥을 띄우는 여유 높이 (m). 물체 크기는 자동 보정되므로 " +
             "표면에 딱 붙이려면 0에 가깝게 두세요.")]
    [SerializeField] private float dropSurfaceOffset = 0.001f;

    [Tooltip("아무것도 조준하지 않았을 때 몸 앞 어느 거리에 놓을지 (m).")]
    [SerializeField] private float dropForwardDistance = 1.2f;

    [Header("입력 (비워두면 마우스 좌클릭 자동 생성)")]
    [SerializeField] private InputActionProperty pickInput;

    private InputAction _pick;
    private bool _ownsAction;
    private WorldItem _held;

    /// <summary>True while something is in the player's hands.</summary>
    public bool IsHolding => _held != null;

    /// <summary>What is being carried, or null.</summary>
    public ItemData HeldItem => _held != null ? _held.Item : null;

    /// <summary>The carried object itself. Stations need this to consume it.</summary>
    public WorldItem HeldObject => _held;

    /// <summary>Where items sit when carried.</summary>
    public Transform HoldAnchor => holdAnchor;

    /// <summary>
    /// 조준 담당. 리시버가 "지금 내 어느 부분을 보고 있나"를 되물을 수 있어야 해서 열어둔다 —
    /// 오븐은 입구를 볼 때만 재료를 받고, 문을 볼 때는 문에 양보해야 한다.
    /// </summary>
    public PlayerInteractor Interactor => interactor;

    /// <summary>Fires whenever the hands change contents. Null means the hands are now empty.</summary>
    public event Action<ItemData> HeldChanged;

    /// <summary>
    /// Extra spin applied when putting the item down, in degrees. Driven by the placement
    /// preview's rotate keys; reset whenever the hands change contents.
    /// </summary>
    public float DropYaw { get; private set; }

    /// <summary>Turns the item that is about to be put down.</summary>
    public void AddDropYaw(float degrees)
    {
        DropYaw = Mathf.Repeat(DropYaw + degrees, 360f);
    }

    /// <summary>
    /// Where the held item would land if dropped right now. The preview and the actual
    /// drop both read this, so the ghost can never point at the wrong spot.
    /// </summary>
    public Vector3 GetDropPosition() => ResolveDropPosition();

    /// <summary>How the held item would be oriented if dropped right now.</summary>
    public Quaternion GetDropRotation()
    {
        return Quaternion.Euler(0f, transform.eulerAngles.y + DropYaw, 0f);
    }

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        if (holdAnchor == null)
        {
            Debug.LogError($"{nameof(PlayerHands)}: Hold Anchor is not assigned.", this);
            enabled = false;
            return;
        }

        if (interactor == null)
        {
            Debug.LogError($"{nameof(PlayerHands)}: no {nameof(PlayerInteractor)} found.", this);
            enabled = false;
            return;
        }

        InputAction assigned = pickInput.action;
        if (assigned != null && assigned.bindings.Count > 0)
        {
            _pick = assigned;
        }
        else
        {
            _ownsAction = true;
            _pick = new InputAction("Pick", InputActionType.Button);
            _pick.AddBinding("<Mouse>/leftButton");
            _pick.AddBinding("<Gamepad>/rightShoulder");
        }
    }

    private void OnEnable() => _pick?.Enable();

    private void OnDisable() => _pick?.Disable();

    private void OnDestroy()
    {
        if (_ownsAction)
        {
            _pick?.Dispose();
        }
    }

    private void Update()
    {
        if (_pick == null || !_pick.WasPressedThisFrame())
        {
            return;
        }

        // The resolver owns the priority order; this just carries out its verdict, so the
        // prompt and the placement ghost always describe exactly what happens here.
        LeftClickAction action = InteractionResolver.Resolve(interactor, this);

        switch (action.Kind)
        {
            case LeftClickKind.Take:
                TakeFrom(action.Source);
                break;

            case LeftClickKind.Put:
                GiveTo(action.Receiver);
                break;

            case LeftClickKind.Click:
                action.Click.OnClick(this);
                break;

            case LeftClickKind.Drop:
                Drop();
                break;
        }
    }

    // ---------------------------------------------------------------- taking

    /// <summary>Takes from a source the resolver already approved.</summary>
    public bool TakeFrom(IItemSource source)
    {
        if (IsHolding || source == null)
        {
            return false;
        }

        GameObject given = source.Provide(this);
        if (given == null)
        {
            return false;
        }

        WorldItem worldItem = given.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            Debug.LogError($"'{given.name}' was handed over but has no {nameof(WorldItem)}. Destroying it.", given);
            Destroy(given);
            return false;
        }

        Attach(worldItem);
        return true;
    }

    /// <summary>
    /// Puts an item straight into the hands, bypassing the aim check. For scripted handovers.
    /// </summary>
    public bool TryGive(WorldItem worldItem)
    {
        if (worldItem == null || IsHolding)
        {
            return false;
        }

        Attach(worldItem);
        return true;
    }

    // ---------------------------------------------------------------- giving

    /// <summary>Hands the held item to a receiver the resolver already approved.</summary>
    public bool GiveTo(IItemReceiver receiver)
    {
        if (!IsHolding || receiver == null)
        {
            return false;
        }

        WorldItem given = TakeHeld();
        if (given == null)
        {
            return false;
        }

        // The receiver owns the object now, including destroying it.
        receiver.Receive(given, this);
        return true;
    }

    /// <summary>
    /// Removes the held item and gives it to the caller. Returns null if the hands were empty.
    /// </summary>
    public WorldItem TakeHeld()
    {
        if (_held == null)
        {
            return null;
        }

        WorldItem taken = _held;
        _held = null;
        taken.transform.SetParent(null, true);

        HeldChanged?.Invoke(null);
        return taken;
    }

    /// <summary>Destroys whatever is being held.</summary>
    public void ConsumeHeld()
    {
        WorldItem taken = TakeHeld();
        if (taken != null)
        {
            Destroy(taken.gameObject);
        }
    }

    // ---------------------------------------------------------------- dropping

    /// <summary>Puts the held item down, on the aimed surface if there is one.</summary>
    public void Drop()
    {
        if (_held == null)
        {
            return;
        }

        WorldItem dropped = _held;

        // 자세를 먼저 구한다. _held를 비운 뒤에 부르면 바닥 보정이 0으로 계산돼서
        // 고스트가 가리킨 곳보다 물체가 파묻히고, 콜라이더가 켜지며 위로 튄다.
        Vector3 position = GetDropPosition();
        Quaternion rotation = GetDropRotation();

        _held = null;

        dropped.transform.SetParent(null, true);
        dropped.transform.SetPositionAndRotation(position, rotation);
        dropped.SetCarried(false);

        // 표면에 얹은 경우에만 고정한다. 허공에서 놓은 건 떨어지는 게 맞다.
        if (interactor.HasHit)
        {
            dropped.RestOnSurface();
        }

        DropYaw = 0f;
        HeldChanged?.Invoke(null);
    }

    private Vector3 ResolveDropPosition()
    {
        // Prefer the surface under the crosshair so putting things on counters feels aimed.
        if (interactor.HasHit)
        {
            // 피벗이 아니라 물체의 바닥을 표면에 맞춘다. 프리팹 피벗이 메시 한가운데인
            // 경우가 많아, 이 보정이 없으면 고스트가 파묻혀 보이고 실제로 놓는 순간
            // 콜라이더가 켜지며 물리가 위로 튕겨낸다.
            float clearance = _held != null ? _held.GetPivotToBottom(GetDropRotation()) : 0f;
            return interactor.LastHitPoint + Vector3.up * (clearance + dropSurfaceOffset);
        }

        Transform origin = interactor.RayOrigin != null ? interactor.RayOrigin : transform;
        return origin.position + origin.forward * dropForwardDistance;
    }

    // ---------------------------------------------------------------- internals

    private void Attach(WorldItem worldItem)
    {
        _held = worldItem;
        DropYaw = 0f;

        worldItem.SetCarried(true);
        worldItem.transform.SetParent(holdAnchor, false);
        worldItem.transform.SetLocalPositionAndRotation(worldItem.HeldPositionOffset,
                                                        worldItem.HeldRotationOffset);

        HeldChanged?.Invoke(worldItem.Item);
    }

    private void OnValidate()
    {
        dropSurfaceOffset = Mathf.Max(0f, dropSurfaceOffset);
        dropForwardDistance = Mathf.Max(0.1f, dropForwardDistance);
    }
}
