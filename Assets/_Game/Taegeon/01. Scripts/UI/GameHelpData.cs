using UnityEngine;

namespace Taegeon
{
    /// <summary>
    /// 게임별 목표와 조작법, 진행 설명 및 안내 이미지를 보관합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameHelp_", menuName = "Game Help/Game Help Data")]
    public sealed class GameHelpData : ScriptableObject
    {
        #region 게임 방법 데이터

        [Header("게임 제목")]
        [SerializeField] private string gameTitle;

        [Header("게임 목표")]
        [TextArea(2, 5)]
        [SerializeField] private string objective;

        [Header("조작 방법")]
        [TextArea(3, 8)]
        [SerializeField] private string controls;

        [Header("진행 방법")]
        [TextArea(3, 10)]
        [SerializeField] private string instructions;

        [Header("설명 이미지 (선택)")]
        [SerializeField] private Sprite helpImage;

        #endregion

        #region 데이터 조회

        public string GameTitle => gameTitle;
        public string Objective => objective;
        public string Controls => controls;
        public string Instructions => instructions;
        public Sprite HelpImage => helpImage;

        #endregion
    }
}
