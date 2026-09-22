using UnityEngine;

namespace Taegeon
{
    /// <summary>
    /// 메인 씬의 배경음과 버튼 효과음을 공용 오디오 매니저에 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubSoundBindings : MonoBehaviour
    {
        #region 사운드 설정

        [Header("메인 화면 사운드")]
        [Tooltip("씬 시작 시 재생할 배경음 SO입니다.")]
        [SerializeField] private SoundData backgroundSound;

        [Tooltip("버튼 클릭 시 재생할 효과음 SO입니다.")]
        [SerializeField] private SoundData buttonClick;

        #endregion

        #region 사운드 재생

        /// <summary>
        /// 메인 씬이 시작되면 배경음을 재생합니다.
        /// </summary>
        private void Start()
        {
            if (backgroundSound != null)
            {
                AudioManager.PlayBGM(backgroundSound);
            }
        }

        /// <summary>
        /// 버튼 클릭 효과음을 재생합니다.
        /// </summary>
        public void PlayButtonClick()
        {
            if (buttonClick != null)
            {
                AudioManager.Play(buttonClick);
            }
        }

        #endregion
    }
}
