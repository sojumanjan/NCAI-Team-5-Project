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
    [SerializeField] private GameObject crosshair;

    [Header("Lighting")]
    [Tooltip("1인칭 시점의 파스텔톤 환경광 색상 (허브 분위기와 통일)")]
    [SerializeField] private Color firstPersonAmbientColor = new Color(0.55f, 0.5f, 0.42f, 1f);
    [Tooltip("관전(전략) 시점의 더 밝은 환경광 색상 — 블록 색 구분이 잘 보여야 함")]
    [SerializeField] private Color overviewAmbientColor = new Color(0.85f, 0.82f, 0.75f, 1f);

    private InputAction cameraSwitchAction;
    private bool isOverviewActive;
    private bool isCrosshairAllowed;

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

    private void ApplyCameraState(bool isOverviewActive)
    {
        this.isOverviewActive = isOverviewActive;

        firstPersonCamera.gameObject.SetActive(!isOverviewActive);
        overviewCamera.gameObject.SetActive(isOverviewActive);

        playerController.enabled = !isOverviewActive;

        foreach (var occluder in overviewOccluders)
        {
            occluder.enabled = !isOverviewActive;
        }

        overviewUI.SetActive(isOverviewActive);

        UpdateCrosshairVisibility();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = isOverviewActive ? overviewAmbientColor : firstPersonAmbientColor;
    }

    /// <summary>
    /// 크로스헤어(에임 포인터)는 팩맨 상태에서만 필요하다 (테트리스에는 조준 요소가 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 이 값을 갱신한다.
    /// </summary>
    public void SetCrosshairAllowed(bool isAllowed)
    {
        isCrosshairAllowed = isAllowed;
        UpdateCrosshairVisibility();
    }

    private void UpdateCrosshairVisibility()
    {
        if (crosshair != null)
        {
            crosshair.SetActive(isCrosshairAllowed && !isOverviewActive);
        }
    }
}
