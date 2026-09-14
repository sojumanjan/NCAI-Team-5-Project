using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRig : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Cameras")]
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera overviewCamera;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [Header("Occluders")]
    [SerializeField] private Renderer[] overviewOccluders;

    [Header("UI")]
    [SerializeField] private GameObject overviewUI;

    private InputAction cameraSwitchAction;
    private bool isOverviewActive;

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        cameraSwitchAction = playerMap.FindAction("CameraSwitch");

        ApplyCameraState(false);
    }

    private void OnEnable()
    {
        cameraSwitchAction.Enable();
        cameraSwitchAction.performed += OnCameraSwitchPerformed;
    }

    private void OnDisable()
    {
        cameraSwitchAction.performed -= OnCameraSwitchPerformed;
        cameraSwitchAction.Disable();
    }

    private void OnCameraSwitchPerformed(InputAction.CallbackContext context)
    {
        ToggleCamera();
    }

    private void ToggleCamera()
    {
        ApplyCameraState(!isOverviewActive);
    }

    private void ApplyCameraState(bool overviewActive)
    {
        isOverviewActive = overviewActive;

        firstPersonCamera.gameObject.SetActive(!overviewActive);
        overviewCamera.gameObject.SetActive(overviewActive);

        playerController.enabled = !overviewActive;

        foreach (var occluder in overviewOccluders)
        {
            occluder.enabled = !overviewActive;
        }

        overviewUI.SetActive(overviewActive);
    }
}
