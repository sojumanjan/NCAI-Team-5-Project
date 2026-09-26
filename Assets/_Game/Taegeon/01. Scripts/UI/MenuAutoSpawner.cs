using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Taegeon
{
    /// <summary>
    /// EventSystem이 있는 씬에 공통 메뉴가 없을 때만 자동으로 생성합니다.
    /// </summary>
    public sealed class MenuAutoSpawner : ScriptableObject
    {
        #region 자동 생성 설정

        private const string ResourcePath = "TaegeonMenuAutoSpawner";

        [SerializeField] private GameObject menuPrefab;
        [SerializeField] private string[] excludedScenes = { "Main_Logo" };

        #endregion

        #region 씬 로드 감지

        /// <summary>
        /// 재생을 시작할 때 중복 없이 씬 로드 이벤트를 연결합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// 첫 씬에서도 메뉴 생성 조건을 확인합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CheckInitialScene()
        {
            EnsureMenu(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// 새로 로드한 씬의 메뉴 생성 조건을 확인합니다.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureMenu(scene);
        }

        #endregion

        #region 조건 확인 및 메뉴 생성

        /// <summary>
        /// 제외 씬과 기존 메뉴를 확인하고 입력 가능한 씬에만 메뉴를 생성합니다.
        /// </summary>
        private static void EnsureMenu(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            MouseSensitivityTarget.EnsurePlayerControllerTarget(scene);

            var settings = Resources.Load<MenuAutoSpawner>(ResourcePath);
            if (settings == null || settings.menuPrefab == null) return;
            if (settings.excludedScenes != null)
            {
                foreach (string sceneName in settings.excludedScenes)
                    if (scene.name == sceneName) return;
            }

            bool hasEventSystem = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                // 비활성 메뉴도 이미 배치된 메뉴로 인정하여 중복 생성을 막습니다.
                if (root.GetComponentInChildren<MenuEscapeToggle>(true) != null) return;
                foreach (var eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                    if (eventSystem.isActiveAndEnabled) hasEventSystem = true;
            }

            // EventSystem은 자동 생성하지 않으며, 없는 씬에는 메뉴도 추가하지 않습니다.
            if (!hasEventSystem) return;

            var menu = Instantiate(settings.menuPrefab);
            menu.name = settings.menuPrefab.name;
            SceneManager.MoveGameObjectToScene(menu, scene);
        }

        #endregion
    }
}
