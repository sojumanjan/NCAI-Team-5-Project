using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 메인 허브와 미니게임 사이를 오가고, 무엇을 깼는지 기억한다. 프로젝트 전체에 하나만 둔다.
///
/// ScriptableObject인 이유는 씬을 넘나들어야 하기 때문이다. MonoBehaviour는 씬이 내려가면
/// 같이 죽지만 에셋은 로드된 채 남는다. DontDestroyOnLoad가 필요 없고, 여섯 명이 각자
/// 자기 씬에서 Play를 눌러도 똑같이 동작한다.
///
/// 결과는 <see cref="MiniGameDefinition"/>을 키로 여기 모인다. Definition 쪽에는 이름·씬 같은
/// 변하지 않는 설정만 있고, 클리어와 점수는 전부 이쪽 메모리에만 산다.
///
/// 에셋은 반드시 Resources 폴더 안에 GameFlow.asset 이름으로 두어야 <see cref="Instance"/>가 찾는다.
/// </summary>
[CreateAssetMenu(fileName = "GameFlow", menuName = "Hub/Game Flow")]
public class GameFlow : ScriptableObject
{
    private const string RESOURCE_NAME = "GameFlow";

    [Header("메인 허브")]
#if UNITY_EDITOR
    [Tooltip("허브 방 씬을 끌어다 놓으세요.")]
    [SerializeField] private SceneAsset mainScene;
#endif

    [Tooltip("돌아갈 허브 씬 이름. 위에 씬을 넣으면 자동으로 채워집니다.")]
    [SerializeField] private string mainSceneName;

    [Header("미니게임")]
    [Tooltip("이 게임에 들어 있는 미니게임들. 여기 등록된 씬만 결과를 보고할 수 있습니다.")]
    [SerializeField] private MiniGameDefinition[] miniGames;

    // 아래 넷은 전부 런타임 전용이다. 직렬화하면 에디터에서 한 번 깬 기록이 에셋에 남는다.
    [NonSerialized] private readonly Dictionary<MiniGameDefinition, MiniGameResult> _results = new();
    [NonSerialized] private MiniGameDefinition _pendingGame;
    [NonSerialized] private MiniGameResult _pendingResult;
    [NonSerialized] private bool _hasPending;

    private static GameFlow _instance;

    /// <summary>미니게임 하나가 끝날 때마다. 같은 씬에 있는 쪽만 들을 수 있다.</summary>
    public event Action<MiniGameDefinition, MiniGameResult> Reported;

    /// <summary>클리어 목록이 바뀌었을 때.</summary>
    public event Action ProgressChanged;

    /// <summary>
    /// 어디서든 부를 수 있는 하나뿐인 진행 상태. 인스펙터 연결이 필요 없다.
    /// 새로 만드는 게 아니라 Resources에 있는 에셋을 읽어 캐시할 뿐이다.
    /// </summary>
    public static GameFlow Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameFlow>(RESOURCE_NAME);

                if (_instance == null)
                {
                    Debug.LogError($"Resources 폴더에서 '{RESOURCE_NAME}' 에셋을 찾지 못했습니다. " +
                                   "GameFlow.asset이 Resources 폴더 안에 있어야 합니다.");
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 플레이를 시작할 때마다 지난 기록을 지운다.
    ///
    /// NonSerialized는 디스크 저장만 막는다. 에디터에서는 에셋이 로드된 채 남아 있어서,
    /// Domain Reload를 꺼두면 지난 판의 클리어 기록이 그대로 따라온다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _instance = null;

        GameFlow flow = Resources.Load<GameFlow>(RESOURCE_NAME);
        if (flow != null)
        {
            flow.ResetRun();
        }
    }

    // ---------------------------------------------------------------- 조회

    /// <summary>등록된 미니게임들. 허브가 목록을 그릴 때 쓴다.</summary>
    public IReadOnlyList<MiniGameDefinition> MiniGames =>
        miniGames ?? Array.Empty<MiniGameDefinition>();

    /// <summary>지금 열려 있는 씬에 해당하는 미니게임. 허브에 있거나 미등록 씬이면 null.</summary>
    public MiniGameDefinition CurrentDefinition => FindByScene(SceneManager.GetActiveScene().name);

    /// <summary>깬 미니게임 수. 씨앗이 얼마나 자랐는지가 이 숫자다.</summary>
    public int ClearedCount
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<MiniGameDefinition, MiniGameResult> pair in _results)
            {
                if (pair.Value.Cleared)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool IsCleared(MiniGameDefinition game) =>
        game != null && _results.TryGetValue(game, out MiniGameResult result) && result.Cleared;

    /// <summary>이 미니게임의 최고 기록. 아직 안 했으면 false.</summary>
    public bool TryGetResult(MiniGameDefinition game, out MiniGameResult result)
    {
        if (game != null)
        {
            return _results.TryGetValue(game, out result);
        }

        result = default;
        return false;
    }

    /// <summary>이 미니게임의 점수 0~1. 아직 안 했으면 0.</summary>
    public float Score01(MiniGameDefinition game) =>
        TryGetResult(game, out MiniGameResult result) ? result.Score01 : 0f;

    // ---------------------------------------------------------------- 보고

    /// <summary>
    /// 지금 씬의 미니게임이 끝났다고 알린다. 팀원이 부를 함수는 이것 하나면 된다 —
    /// 씬 이름으로 자기가 누군지 알아내므로 인스펙터에 꽂을 게 없다.
    /// </summary>
    public void ReportCurrent(MiniGameResult result)
    {
        MiniGameDefinition game = CurrentDefinition;

        if (game == null)
        {
            Debug.LogError($"'{SceneManager.GetActiveScene().name}' 씬이 {name}의 미니게임 목록에 없어 " +
                           "결과를 기록하지 못했습니다. Definition의 Scene 칸과 목록 등록을 확인하세요.", this);
            return;
        }

        Report(game, result);
    }

    /// <summary>
    /// 결과를 기록한다. 재도전은 더 좋은 쪽을 남긴다 — 한 번 깬 것을 다시 하다 망쳤다고
    /// 기록이 지워지면 플레이어만 억울하다.
    /// </summary>
    public void Report(MiniGameDefinition game, MiniGameResult result)
    {
        if (game == null)
        {
            Debug.LogError($"{name}: Definition 없이 결과를 보고했습니다.", this);
            return;
        }

        bool wasCleared = IsCleared(game);

        if (_results.TryGetValue(game, out MiniGameResult old))
        {
            result = new MiniGameResult(old.Cleared || result.Cleared,
                                        Mathf.Max(old.Score01, result.Score01));
        }

        _results[game] = result;

        // 허브는 이 순간 로드돼 있지 않다. 이벤트로는 닿지 않으니 남겨뒀다가
        // 허브가 켜질 때 가져가게 한다.
        _pendingGame = game;
        _pendingResult = result;
        _hasPending = true;

        Reported?.Invoke(game, result);

        if (!wasCleared && result.Cleared)
        {
            ProgressChanged?.Invoke();
        }
    }

    /// <summary>
    /// 방금 끝내고 돌아온 결과를 한 번만 꺼내준다. 두 번째 호출부터는 false다.
    /// 허브가 성장 연출을 딱 한 번만 재생하게 해준다.
    /// </summary>
    public bool TryConsumeLastResult(out MiniGameDefinition game, out MiniGameResult result)
    {
        game = _pendingGame;
        result = _pendingResult;

        bool had = _hasPending;
        _hasPending = false;
        _pendingGame = null;

        return had;
    }

    /// <summary>처음부터 다시.</summary>
    public void ResetRun()
    {
        _results.Clear();
        _pendingGame = null;
        _hasPending = false;

        ProgressChanged?.Invoke();
    }

    // ---------------------------------------------------------------- 이동

    /// <summary>미니게임으로 들어간다.</summary>
    public void LoadMiniGame(MiniGameDefinition game)
    {
        if (game == null || string.IsNullOrWhiteSpace(game.SceneName))
        {
            Debug.LogError($"{name}: 들어갈 씬이 지정되지 않았습니다.", this);
            return;
        }

        Go(game.SceneName);
    }

    /// <summary>허브로 돌아간다.</summary>
    public void ReturnToMain()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError($"{name}: 허브 씬이 지정되지 않았습니다.", this);
            return;
        }

        Go(mainSceneName);
    }

    // ---------------------------------------------------------------- 내부

    private MiniGameDefinition FindByScene(string sceneName)
    {
        if (miniGames == null || string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        foreach (MiniGameDefinition game in miniGames)
        {
            if (game != null && game.SceneName == sceneName)
            {
                return game;
            }
        }

        return null;
    }

    private void Go(string sceneName)
    {
        // 결과 화면에서 시간을 멈춰두는 미니게임이 있다. 그대로 넘어가면 다음 씬이 얼어붙는다.
        Time.timeScale = 1f;

        if (SceneUtility.GetBuildIndexByScenePath(sceneName) < 0)
        {
            Debug.LogError($"'{sceneName}' 씬이 Build Settings에 없습니다. " +
                           "File > Build Profiles 에서 추가해주세요.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainScene != null)
        {
            mainSceneName = mainScene.name;
        }
    }
#endif
}
