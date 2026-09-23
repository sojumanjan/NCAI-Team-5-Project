using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    /// <summary>
    /// 메뉴 프리팹 내부에서 화면 밝기를 보정하고 적용하기 버튼으로 설정을 저장합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubBrightnessBindings : MonoBehaviour
    {
        #region 설정 및 상태

        private const string PreferenceKey = "Taegeon.Hub.Brightness";

        [Header("설정 창 연결")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private GameObject settingsPanel;

        [SerializeField] private Image brightnessOverlay;
        private GameObject overlayRoot;
        private float appliedBrightness;
        private float pendingBrightness;
        private bool panelWasVisible;

        #endregion

        #region 초기화 및 창 상태

        /// <summary>
        /// 저장된 밝기를 불러와 화면 보정 레이어를 준비합니다.
        /// </summary>
        private void Awake()
        {
            appliedBrightness = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 0.5f));
            CreateOverlay();
            ResetPendingBrightness();
            panelWasVisible = settingsPanel != null && settingsPanel.activeInHierarchy;
        }

        /// <summary>
        /// 설정 창이 열리거나 닫히면 적용하지 않은 변경을 초기화합니다.
        /// </summary>
        private void LateUpdate()
        {
            bool visible = settingsPanel != null && settingsPanel.activeInHierarchy;
            if (visible != panelWasVisible)
            {
                if (!visible) ResetPendingBrightness();
                panelWasVisible = visible;
            }
        }

        /// <summary>
        /// 실제 적용된 밝기를 슬라이더와 임시 설정에 표시합니다.
        /// </summary>
        private void ResetPendingBrightness()
        {
            pendingBrightness = appliedBrightness;
            UpdateOverlay();
            if (brightnessSlider != null)
            {
                brightnessSlider.SetValueWithoutNotify(appliedBrightness);
            }
        }

        #endregion

        #region 보정 화면 생명주기
        /// <summary>설정 관리자가 다시 활성화되면 저장된 밝기를 표시합니다.</summary>
        private void OnEnable()
        {
            if (overlayRoot != null) overlayRoot.SetActive(true);
            ResetPendingBrightness();
        }

        /// <summary>설정 관리자가 비활성화되면 미적용 값을 취소하고 보정 화면을 숨깁니다.</summary>
        private void OnDisable()
        {
            ResetPendingBrightness();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        #endregion

        #region 밝기 편집 및 적용

        /// <summary>
        /// 밝기를 저장하지 않고 화면에 즉시 미리 표시합니다.
        /// </summary>
        public void SetBrightness(float value)
        {
            pendingBrightness = Mathf.Clamp01(value);
            UpdateOverlay();
        }

        /// <summary>
        /// 선택한 밝기를 화면에 반영하고 저장합니다.
        /// </summary>
        public void ApplyBrightness()
        {
            appliedBrightness = pendingBrightness;
            UpdateOverlay();
            PlayerPrefs.SetFloat(PreferenceKey, appliedBrightness);
            PlayerPrefs.Save();
        }

        #endregion

        #region 화면 보정

        /// <summary>
        /// UI 클릭을 막지 않는 전체 화면 보정 레이어를 생성합니다.
        /// </summary>
        private void CreateOverlay()
        {
            var menuCanvas = GetComponentInParent<Canvas>();
            if (menuCanvas == null)
            {
                Debug.LogWarning("밝기 관리자는 Menu Canvas 안에 배치해 주세요.", this);
                return;
            }
            if (brightnessOverlay == null)
            {
                var existing = menuCanvas.transform.Find("Brightness Overlay/Brightness Tint");
                if (existing != null) brightnessOverlay = existing.GetComponent<Image>();
            }
            if (brightnessOverlay == null)
            {
                var layer = new GameObject("Brightness Overlay", typeof(RectTransform), typeof(Canvas));
                layer.transform.SetParent(menuCanvas.transform, false);
                var shade = new GameObject("Brightness Tint", typeof(RectTransform), typeof(Image));
                shade.transform.SetParent(layer.transform, false);
                brightnessOverlay = shade.GetComponent<Image>();
            }
            var canvas = brightnessOverlay.GetComponentInParent<Canvas>();
            overlayRoot = canvas.gameObject;
            var layerRect = canvas.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.localScale = Vector3.one;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32760;
            var rect = brightnessOverlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            brightnessOverlay.raycastTarget = false;
        }

        /// <summary>
        /// 기본 화면을 기준으로 어둡거나 밝은 반투명 색상을 적용합니다.
        /// </summary>
        private void UpdateOverlay()
        {
            if (brightnessOverlay == null) return;

            // 양 끝에서도 화면 내용과 설정 버튼을 알아볼 수 있도록 보정량을 제한합니다.
            brightnessOverlay.color = pendingBrightness < 0.5f
                ? new Color(0f, 0f, 0f, (0.5f - pendingBrightness) * 1.1f)
                : new Color(1f, 1f, 1f, (pendingBrightness - 0.5f) * 0.5f);
        }

        #endregion
    }
}
