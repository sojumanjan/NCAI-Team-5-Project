using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
    /// <summary>
    /// 연결된 게임 방법 데이터를 공통 안내 화면에 표시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHelpView : MonoBehaviour
    {
        #region 데이터 및 UI 참조

        [Header("게임별 설명 데이터")]
        [Tooltip("현재 게임에 해당하는 GameHelpData 에셋을 연결합니다.")]
        [SerializeField] private GameHelpData helpData;

        [Header("설명을 표시할 TMP 텍스트")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text controlsText;
        [SerializeField] private TMP_Text instructionsText;

        [Header("설명 이미지 (선택)")]
        [SerializeField] private Image helpImage;

        #endregion

        #region 화면 갱신

        /// <summary>
        /// 게임 방법 창이 열릴 때 연결된 설명을 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// 설명 데이터를 교체하고 화면에 즉시 반영합니다.
        /// </summary>
        public void SetData(GameHelpData data)
        {
            helpData = data;
            Refresh();
        }

        /// <summary>
        /// 설명과 이미지를 갱신하고 데이터가 없으면 기존 표시를 비웁니다.
        /// </summary>
        public void Refresh()
        {
            if (titleText != null)
                titleText.text = helpData != null ? helpData.GameTitle : string.Empty;
            if (objectiveText != null)
                objectiveText.text = helpData != null ? helpData.Objective : string.Empty;
            if (controlsText != null)
                controlsText.text = helpData != null ? helpData.Controls : string.Empty;
            if (instructionsText != null)
                instructionsText.text = helpData != null ? helpData.Instructions : string.Empty;

            if (helpImage != null)
            {
                helpImage.sprite = helpData != null ? helpData.HelpImage : null;
                helpImage.preserveAspect = true;
                // 이미지가 없는 게임은 빈 사각형이 표시되지 않도록 숨깁니다.
                helpImage.enabled = helpImage.sprite != null;
            }
        }

        #endregion
    }
}
