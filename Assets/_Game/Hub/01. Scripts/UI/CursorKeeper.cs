using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 커스텀 마우스 커서가 기본 화살표로 돌아가지 않게 지킨다. 게임이 시작되면 스스로 만들어지고
/// 씬을 넘어가도 남는다. 씬에 배치할 필요가 없다.
///
/// Player Settings의 Default Cursor는 게임을 켤 때 한 번만 입혀진다. 그런데 1인칭 미니게임이
/// 마우스를 잠갔다 풀거나 씬이 바뀌면, OS나 브라우저가 커서를 기본 화살표로 되돌리곤 한다
/// (웹은 마우스 잠금이 풀릴 때 브라우저가 커서 모양을 초기화한다). 그래서 그런 순간마다 다시 입힌다.
///
/// 매 프레임 입히지 않는 이유: SetCursor는 가볍지 않고, 잠겨 있는 동안엔 입혀도 보이지 않는다.
/// </summary>
public class CursorKeeper : MonoBehaviour
{
    private static CursorKeeper _instance;

    private CursorSettings _settings;
    private bool _wasFree;

    // 도메인 리로드를 꺼둔 에디터에서 지난 플레이의 참조가 남아 새로 만들지 못하는 일을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
        {
            return;
        }

        CursorSettings settings = CursorSettings.Load();
        if (settings == null || settings.Texture == null)
        {
            // 설정이 없으면 Player Settings 커서만 쓰는 예전 동작 그대로 둔다.
            return;
        }

        var go = new GameObject(nameof(CursorKeeper));
        DontDestroyOnLoad(go);

        _instance = go.AddComponent<CursorKeeper>();
        _instance._settings = settings;
        _instance.Apply();
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    // 잠금이 풀려 커서가 다시 보이는 바로 그 순간을 잡는다. 누가 풀었든 상관없이 여기서 본다.
    private void LateUpdate()
    {
        bool free = Cursor.lockState != CursorLockMode.Locked && Cursor.visible;
        if (free && !_wasFree)
        {
            Apply();
        }

        _wasFree = free;
    }

    // 다른 창이나 탭에 다녀오면 브라우저·OS가 커서를 바꿔놓았을 수 있다.
    private void OnApplicationFocus(bool focused)
    {
        if (focused)
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (_settings != null && _settings.Texture != null)
        {
            Cursor.SetCursor(_settings.Texture, _settings.Hotspot, CursorMode.Auto);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
