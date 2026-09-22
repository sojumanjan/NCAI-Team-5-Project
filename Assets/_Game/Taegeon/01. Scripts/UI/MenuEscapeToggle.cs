using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
    /// <summary>일시정지 화면과 설정 창, 게임 재개 및 종료를 관리합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuEscapeToggle : MonoBehaviour
    {
        #region 화면 참조 및 상태

        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject inputBlocker;
        [SerializeField] private string titleSceneName = "Main_Logo";

        private bool isPaused;
        private float previousTimeScale;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        #endregion

        #region 초기화 및 입력

        /// <summary>캔버스는 켜 둔 채 일시정지와 설정 화면만 숨깁니다.</summary>
        private void Awake()
        {
            if (menuRoot == null)
            {
                var child = transform.Find("MenuRoot");
                if (child != null) menuRoot = child.gameObject;
            }
            if (pauseRoot == null)
            {
                var child = transform.Find("Pause Root");
                if (child != null) pauseRoot = child.gameObject;
            }
            HideWindows();
        }

        /// <summary>게임 시간이 멈춘 상태에서도 ESC 입력을 처리합니다.</summary>
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Toggle();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
#endif
        }

        /// <summary>캔버스가 꺼지거나 씬을 나갈 때 일시정지 상태를 해제합니다.</summary>
        private void OnDisable()
        {
            Resume();
        }

        #endregion

        #region 일시정지 및 설정 화면

        /// <summary>ESC로 일시정지를 열거나 설정에서 돌아오고 게임을 재개합니다.</summary>
        public void Toggle()
        {
            if (!isPaused) ShowPause();
            else if (menuRoot != null && menuRoot.activeSelf) Close();
            else Resume();
        }

        /// <summary>현재 시간과 커서 상태를 기억하고 게임을 멈춥니다.</summary>
        private void BeginPause()
        {
            if (isPaused) return;
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (inputBlocker != null) inputBlocker.SetActive(true);
        }

        /// <summary>게임을 멈추고 일시정지 버튼 목록을 표시합니다.</summary>
        public void ShowPause()
        {
            BeginPause();
            if (menuRoot != null) menuRoot.SetActive(false);
            if (pauseRoot != null) pauseRoot.SetActive(true);
        }

        /// <summary>일시정지를 유지하면서 기존 설정 창을 표시합니다.</summary>
        public void Open()
        {
            if (menuRoot == null) return;
            BeginPause();
            if (pauseRoot != null) pauseRoot.SetActive(false);
            var panel = menuRoot.transform.Find("Option/Setting Panel");
            var otherPanel = menuRoot.transform.Find("Option/Tooltip Panel");
            if (otherPanel != null) otherPanel.gameObject.SetActive(false);
            if (panel != null) panel.gameObject.SetActive(true);
            menuRoot.SetActive(true);
        }

        /// <summary>설정 창을 닫고 일시정지 화면으로 돌아갑니다.</summary>
        public void Close()
        {
            ShowPause();
        }

        /// <summary>화면을 닫고 일시정지 전의 시간과 커서를 복원합니다.</summary>
        public void Resume()
        {
            HideWindows();
            if (!isPaused) return;
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            isPaused = false;
        }

        /// <summary>일시정지와 설정 화면 및 배경 클릭 차단을 숨깁니다.</summary>
        private void HideWindows()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (inputBlocker != null) inputBlocker.SetActive(false);
        }

        #endregion

        #region 타이틀 이동 및 종료

        /// <summary>게임 시간을 정상화하고 타이틀 씬으로 이동합니다.</summary>
        public void ReturnToTitle()
        {
            if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
            {
                Debug.LogError("타이틀 씬이 빌드 목록에 없습니다: " + titleSceneName, this);
                return;
            }
            Resume();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(titleSceneName);
        }

        /// <summary>빌드에서는 게임을 종료하고 에디터에서는 재생을 종료합니다.</summary>
        public void QuitGame()
        {
            Resume();
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
