using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class SceneButtonLoader : MonoBehaviour
{
    [SerializeField, Tooltip("이동할 씬 이름 또는 Assets/.../SceneName.unity 경로. Build Profiles의 Scene List에 등록된 씬을 입력하세요. 비워두면 이동하지 않습니다.")]
    private string sceneName = "";

    private Button button;
    private bool isLoading;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(LoadTargetScene);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(LoadTargetScene);
    }

    public void LoadTargetScene()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || isLoading) return;
        string target = sceneName == null ? "" : sceneName.Trim();
        if (target.Length == 0) return;

        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogWarning("씬을 불러올 수 없습니다: " + target +
                ". 씬 이름과 Build Profiles > Scene List 등록 여부를 확인하세요.", this);
            return;
        }

        isLoading = true;
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
            if (operation == null)
            {
                isLoading = false;
                return;
            }
            operation.completed += _ => { if (this != null) isLoading = false; };
        }
        catch (System.Exception exception)
        {
            isLoading = false;
            Debug.LogException(exception, this);
        }
    }
}
