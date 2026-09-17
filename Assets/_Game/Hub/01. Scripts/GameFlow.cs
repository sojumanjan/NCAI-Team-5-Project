using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 메인 화면과 미니게임 사이를 오가고, 무엇을 깼는지 기억한다. 프로젝트 전체에 **하나만** 둔다.
///
/// ScriptableObject인 이유는 팀 작업 방식 때문이다. 여섯 명이 각자 자기 씬에서 Play를 눌러
/// 작업하는데, DontDestroyOnLoad 싱글톤이면 메인 화면을 거쳐 들어가야만 존재한다. 에셋이면
/// 어느 씬에서 시작하든 인스펙터에 연결만 돼 있으면 그대로 동작한다.
///
/// 생성: Assets &gt; Create &gt; Hub &gt; Game Flow → Hub/06. Data 에 하나만.
/// </summary>
[CreateAssetMenu(fileName = "GameFlow", menuName = "Hub/Game Flow")]
public class GameFlow : ScriptableObject
{
    [Header("메인 화면")]
#if UNITY_EDITOR
    [Tooltip("메인 화면 씬을 끌어다 놓으세요.")]
    [SerializeField] private SceneAsset mainScene;
#endif

    [Tooltip("돌아갈 메인 화면 씬 이름. 위에 씬을 넣으면 자동으로 채워집니다.")]
    [SerializeField] private string mainSceneName;

    [Header("미니게임")]
    [Tooltip("이 게임에 들어 있는 미니게임들. 각자 만든 Definition 에셋을 여기 모읍니다.")]
    [SerializeField] private MiniGameDefinition[] miniGames;

    // 직렬화하지 않는 게 핵심이다. 에셋에 저장되면 에디터에서 한 번 깬 기록이 남아,
    // 다음에 Play를 눌러도 이미 클리어한 상태로 시작한다.
    [NonSerialized] private readonly HashSet<MiniGameDefinition> _cleared = new();
    [NonSerialized] private MiniGameDefinition _current;

    /// <summary>등록된 미니게임들. 메인 화면이 목록을 그릴 때 쓴다.</summary>
    public IReadOnlyList<MiniGameDefinition> MiniGames =>
        miniGames ?? Array.Empty<MiniGameDefinition>();

    /// <summary>지금 들어가 있는 미니게임. 메인 화면에 있으면 null.</summary>
    public MiniGameDefinition Current => _current;

    /// <summary>깬 미니게임 수. 씨앗이 얼마나 자랐는지가 이 숫자다.</summary>
    public int ClearedCount => _cleared.Count;

    /// <summary>미니게임 하나가 끝날 때마다. 성공이든 실패든 한 번.</summary>
    public event Action<MiniGameDefinition, MiniGameResult> Reported;

    /// <summary>클리어 목록이 바뀌었을 때. 허브의 성장 연출이 여기를 듣는다.</summary>
    public event Action ProgressChanged;

    // ---------------------------------------------------------------- 진행 상태

    public bool IsCleared(MiniGameDefinition game) => game != null && _cleared.Contains(game);

    /// <summary>
    /// 미니게임이 끝났다고 알린다. 각 미니게임이 결과 화면을 띄우는 시점에 한 번 부르면 된다.
    ///
    /// 실패는 기록을 지우지 않는다. 한 번 깬 것을 재도전에서 말아먹었다고 되돌리면
    /// 플레이어만 억울하다.
    /// </summary>
    public void Report(MiniGameDefinition game, MiniGameResult result)
    {
        if (game == null)
        {
            Debug.LogError($"{name}: Definition 없이 결과를 보고했습니다. 인스펙터 연결을 확인하세요.", this);
            return;
        }

        bool changed = result.Cleared && _cleared.Add(game);

        Reported?.Invoke(game, result);

        if (changed)
        {
            ProgressChanged?.Invoke();
        }
    }

    /// <summary>처음부터 다시. 새 게임을 시작할 때 메인 화면이 부른다.</summary>
    public void ResetRun()
    {
        if (_cleared.Count == 0)
        {
            return;
        }

        _cleared.Clear();
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

        _current = game;
        Go(game.SceneName);
    }

    /// <summary>메인 화면으로 돌아간다.</summary>
    public void ReturnToMain()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError($"{name}: 메인 화면 씬이 지정되지 않았습니다.", this);
            return;
        }

        _current = null;
        Go(mainSceneName);
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
