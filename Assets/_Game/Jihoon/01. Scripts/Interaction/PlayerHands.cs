using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player's single pair of hands: one item at a time, picked up and dropped with left
/// click. Deliberately separate from <see cref="PlayerInteractor"/> because the design
/// splits the two verbs — 좌클릭 for carrying things, E for operating stations.
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
    [Tooltip("조준한 표면 위에 놓을 때 살짝 띄우는 높이 (m). 바닥에 파묻히는 걸 막습니다.")]
    [SerializeField] private float dropSurfaceOffset = 0.05f;

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

    /// <summary>Where items sit when carried. Stations can use it to aim animations.</summary>
    public Transform HoldAnchor => holdAnchor;

    /// <summary>Fires whenever the hands change contents. Null means the hands are now empty.</summary>
    public event Action<ItemData> HeldChanged;

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

        // One button for both verbs: full hands put down, empty hands pick up.
        if (IsHolding)
        {
            Drop();
        }
        else
        {
            TryPickAimed();
        }
    }

    // ---------------------------------------------------------------- picking

    /// <summary>Tries to take whatever the crosshair is over. Returns false if there was nothing.</summary>
    public bool TryPickAimed()
    {
        if (IsHolding)
        {
            return false;
        }

        IPickable pickable = interactor.GetAimed<IPickable>();
        if (pickable == null || !pickable.CanPick(this))
        {
            return false;
        }

        GameObject given = pickable.Pick(this);
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
    /// Puts an item straight into the hands, bypassing the aim check. Stations use this to
    /// hand over a finished dish.
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

    /// <summary>
    /// Removes the held item and gives it to the caller, still deactivated. Stations use
    /// this to consume an ingredient. Returns null if the hands were empty.
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

    /// <summary>Destroys whatever is being held. For stations that absorb an ingredient.</summary>
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
        _held = null;

        dropped.transform.SetParent(null, true);
        dropped.transform.SetPositionAndRotation(ResolveDropPosition(), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        dropped.SetCarried(false);

        HeldChanged?.Invoke(null);
    }

    private Vector3 ResolveDropPosition()
    {
        // Prefer the surface under the crosshair so putting things on counters feels aimed.
        if (interactor.HasHit)
        {
            return interactor.LastHitPoint + Vector3.up * dropSurfaceOffset;
        }

        Transform origin = interactor.RayOrigin != null ? interactor.RayOrigin : transform;
        return origin.position + origin.forward * dropForwardDistance;
    }

    // ---------------------------------------------------------------- internals

    private void Attach(WorldItem worldItem)
    {
        _held = worldItem;

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
