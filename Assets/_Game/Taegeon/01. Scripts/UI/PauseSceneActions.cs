using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taegeon
{
    /// <summary>일시정지 메뉴에서 현재 게임 재시작과 메인 허브 복귀를 처리합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class PauseSceneActions : MonoBehaviour
    {
        #region 메뉴 연결
        [SerializeField] private MenuEscapeToggle pauseMenu;
        #endregion

        #region 씬 전환
        /// <summary>일시정지를 해제하고 현재 씬을 처음부터 다시 불러옵니다.</summary>
        public void RestartScene()
        {
            string path = gameObject.scene.path;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("현재 씬이 빌드 목록에 없어 다시 시작할 수 없습니다.", this);
                return;
            }
            PrepareTransition();
            SceneManager.LoadScene(path);
        }

        /// <summary>일시정지를 해제하고 공통 게임 흐름을 통해 메인 허브로 돌아갑니다.</summary>
        public void ReturnToMain()
        {
            PrepareTransition();
            GameFlow.Instance.ReturnToMain();
        }

        /// <summary>씬 이동 전 시간과 커서 상태를 정상화합니다.</summary>
        private void PrepareTransition()
        {
            if (pauseMenu != null) pauseMenu.Resume();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        #endregion
    }
}
