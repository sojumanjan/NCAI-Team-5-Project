using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    /// <summary>현재 씬의 감도 연결을 찾아 미리보기와 적용 및 취소를 처리합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuSensitivityBindings : MonoBehaviour
    {
        #region UI 참조 및 상태
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_InputField numberInput;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button applyButton;
        private MouseSensitivityTarget target;
        private float appliedValue;
        private bool previewing;
        #endregion

        #region 창 열기 및 닫기
        /// <summary>씬의 감도 연결을 찾고 지원 여부에 따라 입력을 설정합니다.</summary>
        private void OnEnable()
        {
            MouseSensitivityTarget.EnsurePlayerControllerTarget(gameObject.scene);
            target = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<MouseSensitivityTarget>(true))
            {
                if (candidate.isActiveAndEnabled && candidate.IsReady)
                {
                    target = candidate;
                    break;
                }
            }
            bool available = target != null;
            if (sensitivitySlider != null)
            {
                sensitivitySlider.interactable = available;
                sensitivitySlider.SetValueWithoutNotify(available ? target.ReadNormalized() : 0.5f);
                sensitivitySlider.onValueChanged.AddListener(PreviewSensitivity);
            }
            if (numberInput != null) numberInput.interactable = available;
            if (statusText != null) statusText.text = available ? string.Empty : "이 게임에서는 감도를 사용하지 않아요";
            if (available) appliedValue = target.ReadValue();
            previewing = false;
            if (applyButton != null) applyButton.onClick.AddListener(ApplySensitivity);
        }

        /// <summary>입력 연결을 해제하고 적용하지 않은 감도를 복원합니다.</summary>
        private void OnDisable()
        {
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(PreviewSensitivity);
            if (applyButton != null) applyButton.onClick.RemoveListener(ApplySensitivity);
            if (previewing && target != null) target.Restore(appliedValue);
            previewing = false;
        }
        #endregion

        #region 미리보기 및 적용
        /// <summary>슬라이더를 움직이는 동안 실제 감도를 미리 변경합니다.</summary>
        private void PreviewSensitivity(float value)
        {
            if (target == null) return;
            target.Preview(value);
            previewing = true;
        }

        /// <summary>현재 감도를 저장하고 취소 시 복원 기준을 갱신합니다.</summary>
        public void ApplySensitivity()
        {
            if (target == null) return;
            target.Save();
            appliedValue = target.ReadValue();
            previewing = false;
        }
        #endregion
    }
}
