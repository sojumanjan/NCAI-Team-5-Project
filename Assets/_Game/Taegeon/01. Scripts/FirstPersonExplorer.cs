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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
    }

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

    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

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

    private void OnApplicationFocus(bool focus) { if (!focus) SetCursor(false); }
    private void OnDisable() { if (ViewActive) SetCursor(false); }
}
}
