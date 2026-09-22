using UnityEngine;

/// <summary>
/// 이 씬에 들어오면 배경음을 튼다. 씬 아무 오브젝트에나 붙이고 곡 하나만 꽂으면 끝이다.
///
/// 팀원 다섯 명이 각자 같은 세 줄을 쓰게 두느니 공용으로 하나 둔다. 씬마다 곡이 다른 것
/// 말고는 할 일이 없는 작업이라, 코드가 아니라 인스펙터 칸이 되는 편이 맞다.
///
/// 같은 곡이 이미 흐르고 있으면 <see cref="AudioManager.PlayBGM"/>이 알아서 무시하므로,
/// 허브와 미니게임을 오가도 곡이 처음부터 다시 시작하지 않는다.
/// </summary>
public class SceneBgm : MonoBehaviour
{
    [Tooltip("이 씬에서 흐를 곡. Category를 BGM으로 만든 SoundData를 넣으세요.")]
    [SerializeField] private SoundData bgm;

    [Tooltip("씬에 들어오자마자 틀지 여부. 끄면 다른 스크립트가 Play()를 부를 때까지 조용합니다.")]
    [SerializeField] private bool playOnStart = true;

    // Awake가 아니라 Start인 이유: AudioManager가 저장된 볼륨을 읽기 전에 곡을 틀면
    // 첫 곡만 지난 설정으로 흐른다.
    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    /// <summary>이 씬의 곡을 튼다.</summary>
    public void Play()
    {
        AudioManager.PlayBGM(bgm);
    }

    private void OnValidate()
    {
        if (bgm != null && bgm.Category != SoundCategory.BGM)
        {
            Debug.LogWarning($"{name}: '{bgm.name}'의 Category가 BGM이 아닙니다. " +
                             "BGM 슬라이더로 조절되지 않습니다.", this);
        }
    }
}
