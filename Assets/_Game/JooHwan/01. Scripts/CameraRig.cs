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

    [Header("Lighting")]
    [Tooltip("1인칭(게임기 내부) 시점의 어두운 환경광 색상")]
    [SerializeField] private Color firstPersonAmbientColor = new Color(0.03f, 0.03f, 0.06f, 1f);
    [Tooltip("관전(전략) 시점의 밝은 환경광 색상 — 블록 색 구분이 잘 보여야 함")]
    [SerializeField] private Color overviewAmbientColor = new Color(0.6f, 0.6f, 0.65f, 1f);

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

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = overviewActive ? overviewAmbientColor : firstPersonAmbientColor;
    }
}
