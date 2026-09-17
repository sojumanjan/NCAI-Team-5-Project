using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "FirstPersonExplorer")]
[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonExplorer : MonoBehaviour
{
    #region 참조 및 설정

    [SerializeField] private Camera viewCamera;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    private CharacterController controller;
    private float pitch;
    private float verticalSpeed;
    private Vector3 spawnPosition;
    public bool ViewActive { get; private set; }
    public Camera ViewCamera => viewCamera;

    #endregion

    #region 시점 준비 및 전환

    /// <summary>
    /// 이동 컨트롤러와 복귀 위치를 준비합니다.
    /// </summary>
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
    }

    /// <summary>
    /// 1인칭 시점과 해당 카메라의 활성 상태를 전환합니다.
    /// </summary>
    public void SetViewActive(bool active)
    {
        ViewActive = active;
        if (viewCamera != null)
        {
            viewCamera.enabled = active;
            viewCamera.tag = active ? "MainCamera" : "Untagged";
            var listener = viewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }
        SetCursor(active);
    }

    /// <summary>
    /// 마우스 커서의 고정과 표시 상태를 설정합니다.
    /// </summary>
    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    #endregion

    #region 이동 및 시점 입력

    /// <summary>
    /// 이동과 시점 조작 입력을 처리합니다.
    /// </summary>
    private void Update()
    {
        if (!ViewActive || viewCamera == null) return;
        Vector2 movement = Vector2.zero;
        Vector2 look = Vector2.zero;
        bool run = false;
        bool release = false;
        bool capture = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k != null)
        {
            movement.x = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
            movement.y = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
            run = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            release = k.escapeKey.wasPressedThisFrame;
        }
        if (m != null)
        {
            look = m.delta.ReadValue();
            capture = m.leftButton.wasPressedThisFrame;
        }
#else
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 12f;
        run = Input.GetKey(KeyCode.LeftShift);
        release = Input.GetKeyDown(KeyCode.Escape);
        capture = Input.GetMouseButtonDown(0);
#endif
        if (release) SetCursor(false);
        else if (capture && Cursor.lockState != CursorLockMode.Locked) { SetCursor(true); look = Vector2.zero; }
        if (Cursor.lockState != CursorLockMode.Locked) { movement = Vector2.zero; look = Vector2.zero; }
        ApplyMotion(movement, look, run, Time.deltaTime);
    }

    /// <summary>
    /// 캐릭터 이동과 중력 및 시점 회전을 적용합니다.
    /// </summary>
    private void ApplyMotion(Vector2 movement, Vector2 look, bool run, float deltaTime)
    {
        transform.Rotate(0f, look.x * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -80f, 80f);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
        verticalSpeed += -20f * deltaTime;
        var input = Vector2.ClampMagnitude(movement, 1f);
        var velocity = (transform.right * input.x + transform.forward * input.y) * (run ? runSpeed : walkSpeed);
        velocity.y = verticalSpeed;
        controller.Move(velocity * deltaTime);
        if (transform.position.y < -10)
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            verticalSpeed = 0;
        }
    }

    #endregion

    #region 조작 종료 처리

    /// <summary>
    /// 창이 비활성화되면 커서 고정을 해제합니다.
    /// </summary>
    private void OnApplicationFocus(bool focus) { if (!focus) SetCursor(false); }
    /// <summary>
    /// 1인칭 조작 종료 시 커서 고정을 해제합니다.
    /// </summary>
    private void OnDisable() { if (ViewActive) SetCursor(false); }
    #endregion

}
}
