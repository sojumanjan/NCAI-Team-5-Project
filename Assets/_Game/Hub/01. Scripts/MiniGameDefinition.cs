using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 미니게임 하나의 신원 — 이름, 아이콘, 어느 씬인지.
///
/// 열거형 대신 에셋으로 만든 이유는 머지 충돌 때문이다. `enum MiniGameId { ... }`로 두면
/// 공유 파일 하나를 넷이 동시에 고치게 된다. 에셋은 각자 자기 폴더에 만들면 되므로 파일이
/// 겹치지 않고, 충돌이 구조적으로 생길 수 없다.
///
/// 생성: Assets &gt; Create &gt; Hub &gt; MiniGame Definition
/// 자기 폴더의 06. Data 안에 두세요.
/// </summary>
[CreateAssetMenu(fileName = "MiniGame_", menuName = "Hub/MiniGame Definition")]
public class MiniGameDefinition : ScriptableObject
{
    [Header("표시")]
    [Tooltip("메인 화면에 보일 이름. 비워두면 에셋 파일 이름을 씁니다.")]
    [SerializeField] private string displayName;

    [Tooltip("메인 화면 아이콘. (선택)")]
    [SerializeField] private Sprite icon;

    [Header("씬")]
#if UNITY_EDITOR
    [Tooltip("자기 미니게임 씬을 끌어다 놓으세요. 아래 이름이 자동으로 채워집니다.")]
    [SerializeField] private SceneAsset scene;
#endif

    [Tooltip("실제로 로드할 씬 이름. 위에 씬을 넣으면 자동으로 채워지니 직접 고치지 마세요.")]
    [SerializeField] private string sceneName;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public Sprite Icon => icon;

    /// <summary>로드할 씬 이름. Build Settings에 등록돼 있어야 동작한다.</summary>
    public string SceneName => sceneName;

#if UNITY_EDITOR
    // 씬 이름을 손으로 적게 하면 오타와 이름 변경에 그대로 당한다. 에셋을 끌어다 놓으면
    // 여기서 이름을 베껴 두므로, 빌드에서는 문자열만 쓰면서도 에디터에서는 안전하다.
    private void OnValidate()
    {
        if (scene != null)
        {
            sceneName = scene.name;
        }
    }
#endif
}
