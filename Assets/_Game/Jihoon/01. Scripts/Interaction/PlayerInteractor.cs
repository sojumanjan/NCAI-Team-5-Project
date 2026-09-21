using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Finds whatever the player is aiming at and drives it. Put this on the Player next to
/// <see cref="PlayerControllerJihoon"/>.
///
/// It deliberately knows nothing about doors, machines or items: it reports focus and
/// input to whatever <see cref="IInteractable"/> it hits, and that object does the work.
/// Adding a new kind of interactable never means editing this file.
///
/// It also publishes the raw collider it is looking at, so other systems can ask their
/// own questions about the aim target — <see cref="PlayerHands"/> uses that to find
/// <see cref="IPickable"/> without casting a second ray.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    private const int HitBufferSize = 12;

    // ---------------------------------------------------------------- inspector

    [Header("참조")]
    [Tooltip("레이를 쏘는 기준. 비워두면 자식 카메라를 자동으로 찾습니다.")]
    [SerializeField] private Transform rayOrigin;

    [Header("조준 판정")]
    [Tooltip("상호작용 가능 거리 (m).")]
    [SerializeField] private float interactDistance = 3f;

    [Tooltip("구체 반경 (m). 클수록 작은 물체도 쉽게 잡히고, 0이면 순수 레이캐스트입니다.")]
    [SerializeField] private float sphereRadius = 0.15f;

    [Tooltip("캐스트에 포함할 레이어. 벽·바닥도 포함해야 벽 너머 물체가 안 잡힙니다.")]
    [SerializeField] private LayerMask interactableMask = ~0;

    [Tooltip("트리거 콜라이더를 포함할지 여부.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("입력 (비워두면 E / 게임패드 X 자동 생성)")]
    [SerializeField] private InputActionProperty interactInput;

    // ---------------------------------------------------------------- state

    private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
    private InputAction _interact;
    private bool _ownsAction;

    private IInteractable _current;
    private float _holdTimer;
    private bool _holdConsumed;

    /// <summary>What the player is aiming at right now, or null.</summary>
    public IInteractable Current => _current;

    /// <summary>The collider under the crosshair, interactable or not. Null when aiming at nothing.</summary>
    public Collider CurrentCollider { get; private set; }

    /// <summary>Hold progress from 0 to 1. Stays 0 for instant interactables.</summary>
    public float HoldProgress01 { get; private set; }

    /// <summary>Where the last successful cast landed. Useful for placing items or world-space UI.</summary>
    public Vector3 LastHitPoint { get; private set; }

    /// <summary>
    /// 마지막으로 맞은 표면이 향한 방향. 바닥인지 벽인지 여기서만 구분할 수 있다 —
    /// 조준점만으로는 조리대 상판과 벽면이 똑같아 보인다.
    /// </summary>
    public Vector3 LastHitNormal { get; private set; } = Vector3.up;

    /// <summary>True while the crosshair is over any surface within reach.</summary>
    public bool HasHit => CurrentCollider != null;

    /// <summary>The origin the aim ray is cast from — normally the camera.</summary>
    public Transform RayOrigin => rayOrigin;

    /// <summary>How far the player can reach, in metres.</summary>
    public float InteractDistance => interactDistance;

    /// <summary>Fires when the aimed-at object changes. The argument is null when nothing is aimed at.</summary>
    public event Action<IInteractable> FocusChanged;

    /// <summary>Fires the moment an interaction actually runs.</summary>
    public event Action<IInteractable> Interacted;

    /// <summary>Fires every frame the hold progress changes, 0 to 1.</summary>
    public event Action<float> HoldProgressChanged;

    /// <summary>
    /// Looks for a component of type T on the thing under the crosshair, searching upward
    /// from the collider so meshes nested under the logic object still resolve.
    /// </summary>
    public T GetAimed<T>() where T : class
    {
        return CurrentCollider != null ? CurrentCollider.GetComponentInParent<T>() : null;
    }

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (rayOrigin == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            rayOrigin = cam != null ? cam.transform : null;
        }

        if (rayOrigin == null)
        {
            Debug.LogError($"{nameof(PlayerInteractor)}: no Ray Origin and no child Camera found.", this);
            enabled = false;
            return;
        }

        InputAction assigned = interactInput.action;
        if (assigned != null && assigned.bindings.Count > 0)
        {
            _interact = assigned;
        }
        else
        {
            _ownsAction = true;
            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonWest");
        }
    }

    private void OnEnable() => _interact?.Enable();

    private void OnDisable()
    {
        _interact?.Disable();
        ClearFocus();
    }

    private void OnDestroy()
    {
        if (_ownsAction)
        {
            _interact?.Dispose();
        }
    }

    private void Update()
    {
        UpdateFocus();
        UpdateInteraction(Time.deltaTime);
    }

    // ---------------------------------------------------------------- focus

    private void UpdateFocus()
    {
        IInteractable found = FindTarget();

        if (ReferenceEquals(found, _current))
        {
            return;
        }

        _current?.OnFocusExit(this);
        CancelHold();

        _current = found;
        _current?.OnFocusEnter(this);
        FocusChanged?.Invoke(_current);
    }

    private IInteractable FindTarget()
    {
        // Cast against walls and floors too, not just interactables. The nearest hit wins,
        // so a crate behind a wall is correctly rejected instead of being reachable.
        int count = Physics.SphereCastNonAlloc(rayOrigin.position, Mathf.Max(0f, sphereRadius),
                                               rayOrigin.forward, _hits, interactDistance,
                                               interactableMask, triggerInteraction);

        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];

            // distance 0 means the cast started already overlapping — almost always the
            // player's own capsule. Either way there is no usable surface to report.
            if (hit.distance <= 0f || hit.collider == null)
            {
                continue;
            }

            // Skips the player, and anything currently held in their hands.
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearest = i;
            }
        }

        if (nearest < 0)
        {
            CurrentCollider = null;
            return null;
        }

        CurrentCollider = _hits[nearest].collider;
        LastHitPoint = _hits[nearest].point;
        LastHitNormal = _hits[nearest].normal;

        // The collider usually sits on a child mesh, so search upwards from it.
        return CurrentCollider.GetComponentInParent<IInteractable>();
    }

    private void ClearFocus()
    {
        CurrentCollider = null;

        if (_current == null)
        {
            return;
        }

        CancelHold();
        _current.OnFocusExit(this);
        _current = null;
        FocusChanged?.Invoke(null);
    }

    // ---------------------------------------------------------------- interaction

    private void UpdateInteraction(float dt)
    {
        if (_current == null || _interact == null || !_current.CanInteract(this))
        {
            CancelHold();
            return;
        }

        bool held = _interact.IsPressed();

        if (!held)
        {
            // Releasing re-arms the interactable so a held key cannot fire it twice.
            _holdConsumed = false;
            CancelHold();
            return;
        }

        float duration = _current.HoldDuration;

        if (duration <= 0f)
        {
            if (_interact.WasPressedThisFrame())
            {
                Fire();
            }
            return;
        }

        if (_holdConsumed)
        {
            return;
        }

        _holdTimer += dt;
        SetHoldProgress(Mathf.Clamp01(_holdTimer / duration));
        _current.OnHoldProgress(this, HoldProgress01);

        if (HoldProgress01 >= 1f)
        {
            _holdConsumed = true;
            Fire();
            ResetHoldTimer();
        }
    }

    private void Fire()
    {
        IInteractable target = _current;
        target.Interact(this);
        Interacted?.Invoke(target);
    }

    private void CancelHold()
    {
        if (_holdTimer <= 0f)
        {
            return;
        }

        _current?.OnHoldCanceled(this);
        ResetHoldTimer();
    }

    private void ResetHoldTimer()
    {
        _holdTimer = 0f;
        SetHoldProgress(0f);
    }

    private void SetHoldProgress(float value)
    {
        if (Mathf.Approximately(HoldProgress01, value))
        {
            return;
        }

        HoldProgress01 = value;
        HoldProgressChanged?.Invoke(value);
    }

    // ---------------------------------------------------------------- editor helpers

    private void OnValidate()
    {
        interactDistance = Mathf.Max(0.1f, interactDistance);
        sphereRadius = Mathf.Max(0f, sphereRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = rayOrigin != null ? rayOrigin : transform;
        Gizmos.color = Application.isPlaying && _current != null ? Color.green : Color.grey;

        Vector3 end = origin.position + origin.forward * interactDistance;
        Gizmos.DrawLine(origin.position, end);

        if (sphereRadius > 0f)
        {
            Gizmos.DrawWireSphere(end, sphereRadius);
        }
    }
}
