using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person character controller: look, walk/sprint/crouch, jump and gravity.
///
/// Expected hierarchy — the capsule's feet sit on the object's origin:
///   Player          (CharacterController + this script)
///   └ CameraPivot   (pitch is applied here; put a Camera or CinemachineCamera under it)
///
/// Input is optional in the inspector: leave an action empty and the controller builds a
/// sensible default binding for it, so the prefab works with nothing wired up.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerControllerJihoon : MonoBehaviour
{
    // ---------------------------------------------------------------- references

    [Header("참조")]
    [Tooltip("상하 시점(pitch)이 적용될 트랜스폼. 이 아래에 카메라를 둡니다.")]
    [SerializeField] private Transform cameraPivot;

    // ---------------------------------------------------------------- look

    [Header("시점 — 감도")]
    [Tooltip("마우스 감도 (x = 좌우, y = 상하).")]
    [SerializeField] private Vector2 mouseSensitivity = new Vector2(0.12f, 0.12f);

    [Tooltip("게임패드 감도 (deg/sec). 마우스와 달리 프레임 시간에 비례합니다.")]
    [SerializeField] private Vector2 gamepadSensitivity = new Vector2(220f, 160f);

    [Tooltip("상하 반전.")]
    [SerializeField] private bool invertY;

    [Header("시점 — 제한 / 감쇠")]
    [Tooltip("올려다볼 수 있는 최대 각도.")]
    [Range(-90f, 0f)]
    [SerializeField] private float pitchMin = -89f;

    [Tooltip("내려다볼 수 있는 최대 각도.")]
    [Range(0f, 90f)]
    [SerializeField] private float pitchMax = 89f;

    [Tooltip("0이면 즉각 반응(권장). 올릴수록 부드럽지만 입력이 늦게 따라옵니다.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float lookSmoothing;

    // ---------------------------------------------------------------- move

    [Header("이동 속도")]
    [Tooltip("기본 걷기 속도 (m/s).")]
    [SerializeField] private float walkSpeed = 4.5f;

    [Tooltip("달리기 속도 (m/s).")]
    [SerializeField] private float sprintSpeed = 7.5f;

    [Tooltip("앉은 상태 속도 (m/s).")]
    [SerializeField] private float crouchSpeed = 2f;

    [Header("이동 반응")]
    [Tooltip("목표 속도까지 붙는 가속도 (m/s²). 크면 즉각적입니다.")]
    [SerializeField] private float acceleration = 55f;

    [Tooltip("멈출 때의 감속도 (m/s²).")]
    [SerializeField] private float deceleration = 65f;

    [Tooltip("공중에서의 조작 정도. 0 = 조작 불가, 1 = 지상과 동일.")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.35f;

    // ---------------------------------------------------------------- jump & gravity

    [Header("점프 / 중력")]
    [Tooltip("점프 최고 높이 (m). 중력값에 맞춰 필요한 속도를 자동 계산합니다.")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Tooltip("중력 가속도 (음수).")]
    [SerializeField] private float gravity = -22f;

    [Tooltip("낙하 속도 상한 (m/s, 양수로 입력).")]
    [SerializeField] private float terminalVelocity = 55f;

    [Tooltip("발판에서 떨어진 뒤에도 점프를 허용하는 유예 시간 (sec).")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("착지 직전에 누른 점프를 기억하는 시간 (sec).")]
    [SerializeField] private float jumpBuffer = 0.12f;

    [Tooltip("접지 상태를 유지하기 위해 바닥으로 눌러주는 힘. 경사면에서 튀는 걸 막습니다.")]
    [SerializeField] private float groundStickForce = 4f;

    // ---------------------------------------------------------------- crouch

    [Header("앉기")]
    [Tooltip("서 있을 때 캡슐 높이 (m).")]
    [SerializeField] private float standHeight = 1.8f;

    [Tooltip("앉았을 때 캡슐 높이 (m).")]
    [SerializeField] private float crouchHeight = 1.1f;

    [Tooltip("서기/앉기 전환 속도. 클수록 빠릅니다.")]
    [SerializeField] private float crouchTransitionSpeed = 12f;

    [Tooltip("켜면 누를 때마다 토글, 끄면 누르고 있는 동안만 앉습니다.")]
    [SerializeField] private bool toggleCrouch;

    [Tooltip("눈높이 = 캡슐 높이 × 이 비율.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float eyeHeightRatio = 0.92f;

    // ---------------------------------------------------------------- ground check

    [Header("접지 판정")]
    [Tooltip("바닥으로 인정할 레이어.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Tooltip("발밑으로 내려 쏘는 판정 거리 (m).")]
    [SerializeField] private float groundCheckDistance = 0.25f;

    [Tooltip("머리 위 장애물 판정에 주는 여유 (m). 천장 아래에서 못 일어서게 합니다.")]
    [SerializeField] private float ceilingCheckPadding = 0.05f;

    // ---------------------------------------------------------------- head bob

    [Header("헤드 밥")]
    [Tooltip("걸을 때 카메라 흔들림 사용.")]
    [SerializeField] private bool headBobEnabled = true;

    [Tooltip("흔들림 크기 (m).")]
    [SerializeField] private float headBobAmplitude = 0.045f;

    [Tooltip("흔들림 속도 (걸음/초).")]
    [SerializeField] private float headBobFrequency = 9f;

    [Tooltip("달릴 때 흔들림 배율.")]
    [SerializeField] private float headBobSprintMultiplier = 1.4f;

    // ---------------------------------------------------------------- cursor

    [Header("커서")]
    [Tooltip("시작할 때 커서를 화면 중앙에 고정하고 숨깁니다.")]
    [SerializeField] private bool lockCursorOnStart = true;

    // ---------------------------------------------------------------- input

    [Header("입력 (비워두면 기본 바인딩 자동 생성)")]
    [SerializeField] private InputActionProperty moveInput;
    [SerializeField] private InputActionProperty lookInput;
    [SerializeField] private InputActionProperty jumpInput;
    [SerializeField] private InputActionProperty sprintInput;
    [SerializeField] private InputActionProperty crouchInput;

    // ---------------------------------------------------------------- runtime state

    private CharacterController _controller;
    private InputAction _move;
    private InputAction _look;
    private InputAction _jump;
    private InputAction _sprint;
    private InputAction _crouch;
    private bool _ownsActions;

    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    private float _yaw;
    private float _pitch;
    private Vector2 _lookVelocity;      // smoothing scratch
    private Vector2 _smoothedLook;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _targetHeight;
    private float _bobTimer;
    private bool _crouchHeld;
    private bool _crouchToggled;

    /// <summary>True while the capsule is shrunk, whether by hold or toggle.</summary>
    public bool IsCrouching => _targetHeight < standHeight - 0.01f;

    /// <summary>True while grounded, including the coyote-time grace period.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>Current horizontal speed in m/s, handy for animation or UI.</summary>
    public float CurrentSpeed => _horizontalVelocity.magnitude;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            Debug.LogError($"{nameof(PlayerControllerJihoon)}: Camera Pivot is not assigned.", this);
            enabled = false;
            return;
        }

        _targetHeight = standHeight;
        ApplyHeight(standHeight);

        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = cameraPivot.localEulerAngles.x;
        if (_pitch > 180f)
        {
            _pitch -= 360f;
        }

        ResolveActions();
    }

    private void OnEnable()
    {
        EnableActions(true);
    }

    private void OnDisable()
    {
        EnableActions(false);
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    private void OnDestroy()
    {
        if (!_ownsActions)
        {
            return;
        }

        // Actions we built ourselves are not owned by an asset, so dispose them.
        _move?.Dispose();
        _look?.Dispose();
        _jump?.Dispose();
        _sprint?.Dispose();
        _crouch?.Dispose();
    }

    /// <summary>
    /// 일시정지 중인지. 메뉴가 Time.timeScale을 0으로 눌러두는 것을 신호로 본다.
    ///
    /// 공용 메뉴(MenuEscapeToggle)에 따로 플래그가 없어서 timeScale을 본다. 태건님
    /// 컨트롤러도 같은 신호를 쓰고 있어 팀에서 이미 통하는 약속이다.
    ///
    /// 이동은 dt가 0이라 알아서 멈추지만 <b>시점은 안 멈춘다</b>. 마우스 델타는 프레임
    /// 시간과 무관해서, 메뉴를 띄워둔 채 마우스를 움직이면 뒤에서 화면이 계속 돌아간다.
    /// </summary>
    public static bool IsPaused => Time.timeScale <= 0f;

    private void Update()
    {
        if (IsPaused)
        {
            // 메뉴가 닫히는 순간 그동안 쌓인 델타가 한 번에 들어와 화면이 튀지 않도록 비운다.
            _smoothedLook = Vector2.zero;
            _lookVelocity = Vector2.zero;
            return;
        }

        float dt = Time.deltaTime;

        UpdateLook(dt);
        UpdateCrouch(dt);
        UpdateGrounded();
        UpdateJump(dt);
        UpdateMove(dt);
        UpdateHeadBob(dt);
    }

    // ---------------------------------------------------------------- look

    private void UpdateLook(float dt)
    {
        Vector2 raw = _look?.ReadValue<Vector2>() ?? Vector2.zero;

        // Mouse delta is already per-frame movement; a stick is a rate, so only the
        // stick gets scaled by frame time. Mixing them up is the usual cause of
        // "sensitivity changes with framerate".
        bool fromStick = _look?.activeControl?.device is Gamepad;
        Vector2 scaled = fromStick
            ? new Vector2(raw.x * gamepadSensitivity.x, raw.y * gamepadSensitivity.y) * dt
            : new Vector2(raw.x * mouseSensitivity.x, raw.y * mouseSensitivity.y);

        if (lookSmoothing > 0f)
        {
            _smoothedLook = Vector2.SmoothDamp(_smoothedLook, scaled, ref _lookVelocity, lookSmoothing);
        }
        else
        {
            _smoothedLook = scaled;
            _lookVelocity = Vector2.zero;
        }

        _yaw += _smoothedLook.x;
        _pitch += invertY ? _smoothedLook.y : -_smoothedLook.y;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ---------------------------------------------------------------- crouch

    private void UpdateCrouch(float dt)
    {
        bool pressed = _crouch != null && _crouch.WasPressedThisFrame();
        _crouchHeld = _crouch != null && _crouch.IsPressed();

        if (toggleCrouch)
        {
            if (pressed)
            {
                _crouchToggled = !_crouchToggled;
            }
        }
        else
        {
            _crouchToggled = _crouchHeld;
        }

        // Refuse to stand back up while something is directly overhead.
        bool wantsToStand = !_crouchToggled;
        if (wantsToStand && HasCeilingAbove())
        {
            wantsToStand = false;
            if (toggleCrouch)
            {
                _crouchToggled = true;
            }
        }

        _targetHeight = wantsToStand ? standHeight : crouchHeight;

        float next = Mathf.MoveTowards(_controller.height, _targetHeight,
                                       crouchTransitionSpeed * dt);
        if (!Mathf.Approximately(next, _controller.height))
        {
            ApplyHeight(next);
        }
    }

    private void ApplyHeight(float height)
    {
        // Keep the capsule's feet on the object origin so the transform sits on the floor.
        _controller.height = height;
        _controller.center = new Vector3(0f, height * 0.5f, 0f);
        cameraPivot.localPosition = new Vector3(cameraPivot.localPosition.x,
                                                height * eyeHeightRatio,
                                                cameraPivot.localPosition.z);
    }

    private bool HasCeilingAbove()
    {
        float radius = Mathf.Max(0.01f, _controller.radius - _controller.skinWidth);
        // Cast from the crouched head up to where the standing head would be.
        Vector3 origin = transform.position + Vector3.up * (_controller.height - radius);
        float distance = Mathf.Max(0f, standHeight - _controller.height) + ceilingCheckPadding;

        return distance > 0f && Physics.SphereCast(origin, radius, Vector3.up, out _,
                                                   distance, groundMask,
                                                   QueryTriggerInteraction.Ignore);
    }

    // ---------------------------------------------------------------- ground & jump

    private void UpdateGrounded()
    {
        bool grounded = _controller.isGrounded;

        if (!grounded)
        {
            // isGrounded misses shallow contacts, so confirm with a short cast.
            float radius = Mathf.Max(0.01f, _controller.radius - _controller.skinWidth);
            Vector3 origin = transform.position + Vector3.up * radius;
            grounded = Physics.SphereCast(origin, radius, Vector3.down, out _,
                                          groundCheckDistance, groundMask,
                                          QueryTriggerInteraction.Ignore);
        }

        IsGrounded = grounded;
    }

    private void UpdateJump(float dt)
    {
        _coyoteTimer = IsGrounded ? coyoteTime : _coyoteTimer - dt;

        if (_jump != null && _jump.WasPressedThisFrame())
        {
            _jumpBufferTimer = jumpBuffer;
        }
        else
        {
            _jumpBufferTimer -= dt;
        }

        if (IsGrounded && _verticalVelocity < 0f)
        {
            // Small constant push so the controller keeps hugging ramps and steps.
            _verticalVelocity = -groundStickForce;
        }

        bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f && !HasCeilingAbove();
        if (canJump)
        {
            // v = sqrt(2 g h) — reach exactly jumpHeight under the configured gravity.
            _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0f, jumpHeight));
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }
        else
        {
            _verticalVelocity += gravity * dt;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -Mathf.Abs(terminalVelocity));
        }
    }

    // ---------------------------------------------------------------- movement

    private void UpdateMove(float dt)
    {
        Vector2 input = _move?.ReadValue<Vector2>() ?? Vector2.zero;
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 wish = transform.right * input.x + transform.forward * input.y;
        Vector3 target = wish * CurrentTargetSpeed();

        float rate = target.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        if (!IsGrounded)
        {
            rate *= airControl;
        }

        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);

        Vector3 motion = _horizontalVelocity + Vector3.up * _verticalVelocity;
        _controller.Move(motion * dt);
    }

    private float CurrentTargetSpeed()
    {
        if (IsCrouching)
        {
            return crouchSpeed;
        }

        bool sprinting = _sprint != null && _sprint.IsPressed();
        return sprinting ? sprintSpeed : walkSpeed;
    }

    // ---------------------------------------------------------------- head bob

    private void UpdateHeadBob(float dt)
    {
        float baseEye = _controller.height * eyeHeightRatio;

        if (!headBobEnabled)
        {
            cameraPivot.localPosition = new Vector3(0f, baseEye, 0f);
            return;
        }

        float speed = CurrentSpeed;
        if (IsGrounded && speed > 0.1f)
        {
            float strength = speed > walkSpeed + 0.1f ? headBobSprintMultiplier : 1f;
            _bobTimer += dt * headBobFrequency * strength;
            float offset = Mathf.Sin(_bobTimer) * headBobAmplitude * strength;
            cameraPivot.localPosition = new Vector3(0f, baseEye + offset, 0f);
        }
        else
        {
            _bobTimer = 0f;
            // Ease back to the neutral eye position instead of snapping.
            Vector3 current = cameraPivot.localPosition;
            cameraPivot.localPosition = Vector3.Lerp(current, new Vector3(0f, baseEye, 0f),
                                                     1f - Mathf.Exp(-12f * dt));
        }
    }

    // ---------------------------------------------------------------- cursor

    /// <summary>Locks or releases the cursor. Call this from a pause menu.</summary>
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // ---------------------------------------------------------------- input plumbing

    private void ResolveActions()
    {
        _ownsActions = false;

        _move = Resolve(moveInput, BuildMoveAction);
        _look = Resolve(lookInput, BuildLookAction);
        _jump = Resolve(jumpInput, BuildButtonAction, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
        _sprint = Resolve(sprintInput, BuildButtonAction, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
        _crouch = Resolve(crouchInput, BuildButtonAction, "Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
    }

    private InputAction Resolve(InputActionProperty property, System.Func<InputAction> fallback)
    {
        InputAction assigned = property.action;
        if (assigned != null && assigned.bindings.Count > 0)
        {
            return assigned;
        }

        _ownsActions = true;
        return fallback();
    }

    private InputAction Resolve(InputActionProperty property,
                                System.Func<string, string, string, InputAction> fallback,
                                string name, string keyboard, string gamepad)
    {
        InputAction assigned = property.action;
        if (assigned != null && assigned.bindings.Count > 0)
        {
            return assigned;
        }

        _ownsActions = true;
        return fallback(name, keyboard, gamepad);
    }

    private static InputAction BuildMoveAction()
    {
        var action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        action.AddBinding("<Gamepad>/leftStick");
        return action;
    }

    private static InputAction BuildLookAction()
    {
        var action = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        action.AddBinding("<Mouse>/delta");
        action.AddBinding("<Gamepad>/rightStick");
        return action;
    }

    private static InputAction BuildButtonAction(string name, string keyboard, string gamepad)
    {
        var action = new InputAction(name, InputActionType.Button);
        action.AddBinding(keyboard);
        action.AddBinding(gamepad);
        return action;
    }

    private void EnableActions(bool enable)
    {
        SetEnabled(_move, enable);
        SetEnabled(_look, enable);
        SetEnabled(_jump, enable);
        SetEnabled(_sprint, enable);
        SetEnabled(_crouch, enable);
    }

    private static void SetEnabled(InputAction action, bool enable)
    {
        if (action == null)
        {
            return;
        }

        if (enable)
        {
            action.Enable();
        }
        else
        {
            action.Disable();
        }
    }

    // ---------------------------------------------------------------- editor helpers

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        crouchSpeed = Mathf.Clamp(crouchSpeed, 0f, walkSpeed);
        acceleration = Mathf.Max(0.01f, acceleration);
        deceleration = Mathf.Max(0.01f, deceleration);
        standHeight = Mathf.Max(0.5f, standHeight);
        crouchHeight = Mathf.Clamp(crouchHeight, 0.3f, standHeight);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        gravity = Mathf.Min(-0.01f, gravity);
        terminalVelocity = Mathf.Max(1f, terminalVelocity);
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
        headBobFrequency = Mathf.Max(0f, headBobFrequency);
        headBobAmplitude = Mathf.Max(0f, headBobAmplitude);
    }

    private void OnDrawGizmosSelected()
    {
        var controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            return;
        }

        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * radius - Vector3.up * groundCheckDistance,
                              radius);
    }
}
