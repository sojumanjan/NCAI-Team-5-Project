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
    [Tooltip("1인칭 시점의 파스텔톤 환경광 색상 (허브 분위기와 통일)")]
    [SerializeField] private Color firstPersonAmbientColor = new Color(0.55f, 0.5f, 0.42f, 1f);
    [Tooltip("관전(전략) 시점의 더 밝은 환경광 색상 — 블록 색 구분이 잘 보여야 함")]
    [SerializeField] private Color overviewAmbientColor = new Color(0.85f, 0.82f, 0.75f, 1f);

    private InputAction cameraSwitchAction;
    private bool isOverviewActive;
    private bool isCameraSwitchAllowed = true;

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        cameraSwitchAction = playerMap.FindAction("CameraSwitch");

        ApplyCameraState(false);
    }

    private void OnEnable()
    {
        cameraSwitchAction.performed += ToggleCamera;

        if (isCameraSwitchAllowed)
        {
            cameraSwitchAction.Enable();
        }
    }

    private void OnDisable()
    {
        cameraSwitchAction.performed -= ToggleCamera;
        cameraSwitchAction.Disable();
    }

    /// <summary>
    /// 관전 전환(Tab)은 테트리스에서만 의미가 있다 (팩맨에는 관전 시점이 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출해, 팩맨 중엔 Tab 입력 자체를 막는다.
    /// false로 바뀌는 순간 이미 관전 중이었다면 1인칭으로 강제 복귀시킨다.
    /// </summary>
    public void SetCameraSwitchAllowed(bool isAllowed)
    {
        isCameraSwitchAllowed = isAllowed;

        if (isAllowed)
        {
            cameraSwitchAction.Enable();
        }
        else
        {
            cameraSwitchAction.Disable();

            if (isOverviewActive)
            {
                ApplyCameraState(false);
            }
        }
    }

    /// <summary>
    /// 테트리스/팩맨 게임이 (재)활성화될 때마다 호출한다. 관전 모드로 들어간 채
    /// 사망/재시작 등으로 다시 진입하면 카메라와 조작이 서로 어긋난 상태로 남을 수 있으므로,
    /// 매번 1인칭 상태로 강제 리셋한다.
    /// </summary>
    public void ResetToFirstPerson()
    {
        if (isOverviewActive)
        {
            ApplyCameraState(false);
        }
    }

    private void ToggleCamera(InputAction.CallbackContext context)
    {
        // 일시정지/사망 팝업 등으로 조작이 잠긴 동안에는, 패널의 버튼을 클릭하는 좌클릭이
        // 그대로 CameraSwitch 액션으로도 들어와 시점이 전환되므로 잠금 중엔 무시한다.
        if (playerController.AreControlsLocked)
        {
            return;
        }

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

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = isOverviewActive ? overviewAmbientColor : firstPersonAmbientColor;
    }
}
