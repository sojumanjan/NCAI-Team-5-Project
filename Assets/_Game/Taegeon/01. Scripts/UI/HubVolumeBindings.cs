using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    /// <summary>
    /// 음량을 미리 듣고 적용하거나 창을 닫아 변경을 취소합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubVolumeBindings : MonoBehaviour
    {
        #region 슬라이더 참조 및 설정

        [Header("음량 슬라이더")]
        [SerializeField] private Slider backgroundMusicSlider;
        [SerializeField] private Slider buttonSoundSlider;

        // 공용 AudioManager의 저장 키와 일치시켜 미리보기 값이 저장되지 않게 합니다.
        private const string BackgroundPreferenceKey = "Audio.Volume.1";
        private const string ButtonPreferenceKey = "Audio.Volume.3";

        private float appliedBackgroundVolume;
        private float appliedButtonVolume;
        private float pendingBackgroundVolume;
        private float pendingButtonVolume;
        private bool hasPreview;

        #endregion

        #region 설정 창 상태

        /// <summary>
        /// 설정 창이 열리면 현재 음량을 취소 시 복원할 기준으로 준비합니다.
        /// </summary>
        private void OnEnable()
        {
            appliedBackgroundVolume = AudioManager.GetVolume(SoundCategory.BGM);
            appliedButtonVolume = AudioManager.GetVolume(SoundCategory.UI);
            pendingBackgroundVolume = appliedBackgroundVolume;
            pendingButtonVolume = appliedButtonVolume;
            hasPreview = false;
            RefreshSliders();
        }

        /// <summary>
        /// 창을 닫으면 적용하지 않은 음량을 마지막 적용값으로 복원합니다.
        /// </summary>
        private void OnDisable()
        {
            if (!hasPreview) return;
            PreviewVolume(SoundCategory.BGM, appliedBackgroundVolume, BackgroundPreferenceKey);
            PreviewVolume(SoundCategory.UI, appliedButtonVolume, ButtonPreferenceKey);
            pendingBackgroundVolume = appliedBackgroundVolume;
            pendingButtonVolume = appliedButtonVolume;
            hasPreview = false;
            RefreshSliders();
        }

        /// <summary>
        /// 임시 음량을 슬라이더에 표시합니다.
        /// </summary>
        private void RefreshSliders()
        {
            if (backgroundMusicSlider != null)
                backgroundMusicSlider.SetValueWithoutNotify(pendingBackgroundVolume);
            if (buttonSoundSlider != null)
                buttonSoundSlider.SetValueWithoutNotify(pendingButtonVolume);
        }

        #endregion

        #region 음량 미리보기 및 적용

        /// <summary>
        /// 배경 음악 음량을 저장하지 않고 미리 재생합니다.
        /// </summary>
        public void SetBackgroundVolume(float volume)
        {
            pendingBackgroundVolume = Mathf.Clamp01(volume);
            PreviewVolume(SoundCategory.BGM, pendingBackgroundVolume, BackgroundPreferenceKey);
            hasPreview = true;
        }

        /// <summary>
        /// 버튼 효과음의 음량을 저장하지 않고 미리 반영합니다.
        /// </summary>
        public void SetButtonVolume(float volume)
        {
            pendingButtonVolume = Mathf.Clamp01(volume);
            PreviewVolume(SoundCategory.UI, pendingButtonVolume, ButtonPreferenceKey);
            hasPreview = true;
        }

        /// <summary>
        /// 공용 매니저의 저장값은 유지하면서 실제 재생 음량만 바꿉니다.
        /// </summary>
        private void PreviewVolume(SoundCategory category, float volume, string preferenceKey)
        {
            bool hadValue = PlayerPrefs.HasKey(preferenceKey);
            float savedValue = PlayerPrefs.GetFloat(preferenceKey, 1f);
            AudioManager.SetVolume(category, volume);
            if (hadValue) PlayerPrefs.SetFloat(preferenceKey, savedValue);
            else PlayerPrefs.DeleteKey(preferenceKey);
        }

        /// <summary>
        /// 미리 듣던 음량을 저장하고 이후 취소의 기준으로 확정합니다.
        /// </summary>
        public void ApplyVolumes()
        {
            appliedBackgroundVolume = pendingBackgroundVolume;
            appliedButtonVolume = pendingButtonVolume;
            AudioManager.SetVolume(SoundCategory.BGM, appliedBackgroundVolume);
            AudioManager.SetVolume(SoundCategory.UI, appliedButtonVolume);
            PlayerPrefs.Save();
            hasPreview = false;
        }

        #endregion
    }
}
