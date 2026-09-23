using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 전체에서 하나뿐인 소리 담당. 팀원은 이 클래스의 static 함수만 부르면 된다.
///
/// 씬에 놓지 않아도 스스로 뜬다. 여섯 명이 각자 자기 씬에서 Play를 누르는 프로젝트라,
/// "각자 씬에 이걸 배치하세요"로 가면 한 명만 빠뜨려도 그 씬은 통째로 무음이 된다.
/// Resources에 둔 프리팹을 첫 씬이 열리기 전에 띄우고 씬이 바뀌어도 살려둔다 —
/// GameFlow가 Resources에서 자기를 찾는 것과 같은 이유다.
///
/// 소리 목록은 여기에 없다. 무엇을 재생할지는 <see cref="SoundData"/> 에셋이 들고 있고,
/// 매니저는 그걸 받아 울리기만 한다. 그래서 팀원이 소리를 추가해도 이 파일은 그대로다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string RESOURCE_NAME = "AudioManager";
    private const string PREFS_PREFIX = "Audio.Volume.";
    private const float MIN_DECIBEL = -80f;

    // ---------------------------------------------------------------- 인스펙터

    [Header("믹서")]
    [Tooltip("공용 믹서. 비워두면 볼륨을 코드에서 직접 곱합니다.")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("BGM이 흐를 그룹.")]
    [SerializeField] private AudioMixerGroup bgmGroup;

    [Tooltip("효과음이 흐를 그룹.")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Tooltip("UI 소리가 흐를 그룹.")]
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("믹서 노출 파라미터 이름")]
    [Tooltip("믹서에서 Expose 한 이름과 정확히 같아야 합니다.")]
    [SerializeField] private string masterParam = "MasterVolume";

    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";
    [SerializeField] private string uiParam = "UIVolume";

    [Header("동시 재생")]
    [Tooltip("한 번에 울릴 수 있는 효과음 수. 넘치면 가장 오래된 것을 빼앗습니다.")]
    [SerializeField] private int voiceCount = 24;

    [Header("배경음")]
    [Tooltip("곡이 바뀔 때 겹치며 넘어가는 시간 (초).")]
    [SerializeField] private float bgmFadeSeconds = 1f;

    // ---------------------------------------------------------------- 상태

    private static AudioManager _instance;
    private static bool _quitting;

    private Voice[] _voices;
    private int _nextVoice;

    // 같은 소리가 한 프레임에 여러 번 요청되면 소리가 찢어진다. 마지막으로 울린 시각을 재둔다.
    private readonly Dictionary<SoundData, float> _lastPlayed = new();

    private readonly float[] _volumes = { 1f, 1f, 1f, 1f };

    private AudioSource _bgmA;
    private AudioSource _bgmB;
    private bool _bgmUsingA;
    private SoundData _bgmPlaying;
    private float _bgmFadeLeft;

    // 이번 전환에 쓸 페이드 길이. 프리팹 값이 아니라 여기를 보는 이유는 부르는 쪽에서
    // 길이를 정할 수 있어야 하기 때문이다 — 타이틀에서 나갈 때처럼.
    private float _bgmFadeTotal = 1f;

    /// <summary>풀에 담긴 소스 하나와, 그 자리가 몇 번째로 쓰였는지.</summary>
    private class Voice
    {
        public AudioSource Source;
        public int Generation;
        public SoundData Data;
        public Transform Follow;

        // Follow가 null인 것만으로는 "따라다니라고 시킨 적 없음"과 "대상이 파괴됨"을
        // 구분할 수 없다. 전자는 그대로 두어야 하고 후자는 멈춰야 한다.
        public bool Following;
        public float BaseVolume;
        public float FadeLeft;
        public float FadeTotal;
    }

    // ---------------------------------------------------------------- 자동 기동

    /// <summary>
    /// 첫 씬이 열리기 전에 스스로 뜬다. 어느 씬에서 Play를 눌러도 소리가 난다.
    ///
    /// 에디터에서는 도메인이 살아남을 수 있어 지난 판의 인스턴스 참조가 남는다. 그래서
    /// 참조부터 비운다 — GameFlow의 ResetOnPlay와 같은 이유다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _instance = null;
        _quitting = false;

        EnsureInstance();
    }

    /// <summary>하나뿐인 매니저. 없으면 Resources에서 띄운다.</summary>
    public static AudioManager Instance
    {
        get
        {
            EnsureInstance();
            return _instance;
        }
    }

    private static void EnsureInstance()
    {
        if (_instance != null || _quitting)
        {
            return;
        }

        // 누군가 씬에 직접 놓아뒀을 수 있다. 그걸 먼저 쓴다.
        _instance = FindFirstObjectByType<AudioManager>();
        if (_instance != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(RESOURCE_NAME);
        if (prefab == null)
        {
            Debug.LogError($"Resources 폴더에서 '{RESOURCE_NAME}' 프리팹을 찾지 못했습니다. " +
                           "소리가 나지 않습니다.");
            return;
        }

        GameObject spawned = Instantiate(prefab);
        spawned.name = RESOURCE_NAME;
        _instance = spawned.GetComponent<AudioManager>();
    }

    private void Awake()
    {
        // 씬을 오갈 때마다 하나씩 늘어나면 소리가 겹쳐 울린다.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildVoices();
        LoadVolumes();
    }

    private void OnApplicationQuit()
    {
        // 종료 중에 소리를 요청하면 매니저를 새로 만들어 씬에 유령이 남는다.
        _quitting = true;
    }

    private void BuildVoices()
    {
        _voices = new Voice[Mathf.Max(1, voiceCount)];

        for (int i = 0; i < _voices.Length; i++)
        {
            var go = new GameObject($"Voice {i}");
            go.transform.SetParent(transform, false);

            _voices[i] = new Voice
            {
                Source = CreateSource(go),
                Generation = 0,
            };
        }

        _bgmA = CreateSource(NewChild("BGM A"));
        _bgmB = CreateSource(NewChild("BGM B"));
        _bgmA.loop = true;
        _bgmB.loop = true;
    }

    private GameObject NewChild(string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go;
    }

    private static AudioSource CreateSource(GameObject host)
    {
        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    // ---------------------------------------------------------------- 팀원이 부르는 함수

    /// <summary>화면 어디서나 같게 들리는 소리. UI와 알림에 쓴다.</summary>
    public static SoundHandle Play(SoundData data)
    {
        AudioManager m = Instance;
        return m != null ? m.PlayInternal(data, null, Vector3.zero, false) : SoundHandle.None;
    }

    /// <summary>월드의 한 지점에서 나는 소리. 그 자리에 고정된다.</summary>
    public static SoundHandle PlayAt(SoundData data, Vector3 position)
    {
        AudioManager m = Instance;
        return m != null ? m.PlayInternal(data, null, position, true) : SoundHandle.None;
    }

    /// <summary>움직이는 물체를 따라다니는 소리. 돌아가는 믹서나 걷는 손님에 쓴다.</summary>
    public static SoundHandle PlayAttached(SoundData data, Transform follow)
    {
        AudioManager m = Instance;
        if (m == null)
        {
            return SoundHandle.None;
        }

        Vector3 at = follow != null ? follow.position : Vector3.zero;
        return m.PlayInternal(data, follow, at, true);
    }

    /// <summary>
    /// 배경음을 바꾼다. 이미 같은 곡이 흐르고 있으면 아무 일도 하지 않는다.
    ///
    /// 허브와 미니게임을 오갈 때 허브 BGM이 매번 처음부터 다시 시작하지 않게 하려는 것이다.
    /// 각자 씬에서 시작할 때 한 번 부르면 된다.
    /// </summary>
    public static void PlayBGM(SoundData data)
    {
        AudioManager m = Instance;
        if (m != null)
        {
            m.PlayBGMInternal(data, -1f);
        }
    }

    /// <summary>
    /// 배경음을 끈다. 기본은 곧바로 끄고, 초를 주면 그만큼 사그라든다.
    ///
    /// 기본을 0으로 둔 이유는 "끈다"가 대개 씬을 넘기기 직전이기 때문이다. 그때 남은
    /// 페이드는 다음 씬으로 넘어가지 못하고 잘려서, 길게 잡아둬도 들리지 않는다.
    /// 화면 연출과 맞춰 사그라뜨리고 싶을 때만 시간을 준다.
    /// </summary>
    public static void StopBGM(float fadeSeconds = 0f)
    {
        AudioManager m = Instance;
        if (m != null)
        {
            m.PlayBGMInternal(null, fadeSeconds);
        }
    }

    /// <summary>갈래별 볼륨을 0~1로 정한다. 옵션 슬라이더가 부를 함수다.</summary>
    public static void SetVolume(SoundCategory category, float value01)
    {
        AudioManager m = Instance;
        if (m != null)
        {
            m.SetVolumeInternal(VolumeSlot(category), value01);
        }
    }

    /// <summary>전체 볼륨.</summary>
    public static void SetMasterVolume(float value01)
    {
        AudioManager m = Instance;
        if (m != null)
        {
            m.SetVolumeInternal(0, value01);
        }
    }

    public static float GetVolume(SoundCategory category)
    {
        AudioManager m = Instance;
        return m != null ? m._volumes[VolumeSlot(category)] : 1f;
    }

    public static float GetMasterVolume()
    {
        AudioManager m = Instance;
        return m != null ? m._volumes[0] : 1f;
    }

    // ---------------------------------------------------------------- 재생

    private SoundHandle PlayInternal(SoundData data, Transform follow, Vector3 position, bool positioned)
    {
        if (data == null)
        {
            return SoundHandle.None;
        }

        AudioClip clip = data.PickClip();
        if (clip == null)
        {
            Debug.LogWarning($"{data.name}: 클립이 비어 있어 재생하지 못했습니다.", data);
            return SoundHandle.None;
        }

        if (!PassesInterval(data))
        {
            return SoundHandle.None;
        }

        int index = TakeVoice();
        Voice voice = _voices[index];
        AudioSource source = voice.Source;

        voice.Generation++;
        voice.Data = data;
        voice.Follow = follow;
        voice.Following = follow != null;
        voice.FadeLeft = 0f;
        voice.FadeTotal = 0f;
        voice.BaseVolume = data.Volume * FallbackVolume(data.Category);

        source.transform.position = positioned ? position : transform.position;

        source.clip = clip;
        source.loop = data.Loop;
        source.volume = voice.BaseVolume;
        source.pitch = data.PickPitch();
        source.spatialBlend = positioned ? data.SpatialBlend : 0f;
        source.maxDistance = data.MaxDistance;
        source.outputAudioMixerGroup = GroupFor(data.Category);
        source.Play();

        return new SoundHandle(index, voice.Generation);
    }

    /// <summary>
    /// 빈 자리를 찾는다. 전부 차 있으면 가장 오래전에 시작한 것을 빼앗는다.
    ///
    /// 소리가 안 나는 것보다 오래된 소리가 끊기는 쪽이 낫다. 어차피 스물넉 대가 동시에
    /// 울리는 상황이면 하나쯤 사라져도 아무도 모른다.
    /// </summary>
    private int TakeVoice()
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            if (!_voices[i].Source.isPlaying)
            {
                return i;
            }
        }

        int stolen = _nextVoice;
        _nextVoice = (_nextVoice + 1) % _voices.Length;

        _voices[stolen].Source.Stop();
        return stolen;
    }

    private bool PassesInterval(SoundData data)
    {
        float interval = data.MinInterval;
        if (interval <= 0f)
        {
            return true;
        }

        // Time.time은 일시정지에 영향을 받는다. 결과 화면에서 시간을 멈춰도 UI 소리는
        // 나야 하므로 실시간을 쓴다.
        float now = Time.unscaledTime;

        if (_lastPlayed.TryGetValue(data, out float last) && now - last < interval)
        {
            return false;
        }

        _lastPlayed[data] = now;
        return true;
    }

    private void PlayBGMInternal(SoundData data, float fadeSeconds)
    {
        if (data == _bgmPlaying)
        {
            return;
        }

        _bgmPlaying = data;

        AudioSource fadingOut = _bgmUsingA ? _bgmA : _bgmB;
        AudioSource fadingIn = _bgmUsingA ? _bgmB : _bgmA;
        _bgmUsingA = !_bgmUsingA;

        if (data != null)
        {
            AudioClip clip = data.PickClip();
            if (clip != null)
            {
                fadingIn.clip = clip;
                fadingIn.volume = 0f;
                fadingIn.pitch = data.PickPitch();
                fadingIn.spatialBlend = 0f;
                fadingIn.outputAudioMixerGroup = GroupFor(SoundCategory.BGM);
                fadingIn.Play();
            }
        }

        // 음수면 프리팹에 잡아둔 기본 길이를 쓴다.
        _bgmFadeTotal = Mathf.Max(0.01f, fadeSeconds >= 0f ? fadeSeconds : bgmFadeSeconds);
        _bgmFadeLeft = _bgmFadeTotal;

        // 첫 곡이면 페이드할 상대가 없다. 그냥 끈다.
        if (!fadingOut.isPlaying)
        {
            fadingOut.volume = 0f;
        }
    }

    // ---------------------------------------------------------------- 갱신

    // 따라다니는 소리는 카메라가 움직인 뒤에 자리를 잡아야 한 프레임 밀리지 않는다.
    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        UpdateVoices(dt);
        UpdateBGM(dt);
    }

    private void UpdateVoices(float dt)
    {
        foreach (Voice voice in _voices)
        {
            if (!voice.Source.isPlaying)
            {
                voice.Follow = null;
                voice.Following = false;
                continue;
            }

            if (voice.Following)
            {
                // 따라다니던 대상이 사라졌다. 반복되는 소리는 그대로 두면 영영 울린다.
                if (voice.Follow == null)
                {
                    voice.Source.Stop();
                    voice.Following = false;
                    continue;
                }

                voice.Source.transform.position = voice.Follow.position;
            }

            if (voice.FadeTotal > 0f)
            {
                voice.FadeLeft -= dt;

                if (voice.FadeLeft <= 0f)
                {
                    voice.Source.Stop();
                    voice.FadeTotal = 0f;
                    continue;
                }

                voice.Source.volume = voice.BaseVolume * (voice.FadeLeft / voice.FadeTotal);
            }
        }
    }

    private void UpdateBGM(float dt)
    {
        if (_bgmFadeLeft <= 0f)
        {
            return;
        }

        _bgmFadeLeft -= dt;

        float total = Mathf.Max(0.01f, _bgmFadeTotal);
        float t = Mathf.Clamp01(1f - (_bgmFadeLeft / total));

        AudioSource fadingIn = _bgmUsingA ? _bgmA : _bgmB;
        AudioSource fadingOut = _bgmUsingA ? _bgmB : _bgmA;

        float target = _bgmPlaying != null ? _bgmPlaying.Volume * FallbackVolume(SoundCategory.BGM) : 0f;

        fadingIn.volume = target * t;
        fadingOut.volume = fadingOut.volume * (1f - t);

        if (_bgmFadeLeft > 0f)
        {
            return;
        }

        _bgmFadeLeft = 0f;
        fadingIn.volume = target;
        fadingOut.Stop();
        fadingOut.volume = 0f;
    }

    // ---------------------------------------------------------------- 핸들이 부르는 함수

    internal static bool IsVoicePlaying(int index, int generation)
    {
        AudioManager m = _instance;
        if (m == null || m._voices == null || index < 0 || index >= m._voices.Length)
        {
            return false;
        }

        Voice voice = m._voices[index];
        return voice.Generation == generation && voice.Source.isPlaying;
    }

    internal static void StopVoice(int index, int generation, float fadeSeconds)
    {
        AudioManager m = _instance;
        if (m == null || m._voices == null || index < 0 || index >= m._voices.Length)
        {
            return;
        }

        Voice voice = m._voices[index];

        // 세대가 다르면 이 자리는 이미 남의 소리다. 건드리면 엉뚱한 게 꺼진다.
        if (voice.Generation != generation)
        {
            return;
        }

        if (fadeSeconds <= 0f)
        {
            voice.Source.Stop();
            voice.Follow = null;
            voice.Following = false;
            voice.FadeTotal = 0f;
            return;
        }

        voice.FadeTotal = fadeSeconds;
        voice.FadeLeft = fadeSeconds;
    }

    // ---------------------------------------------------------------- 볼륨

    private static int VolumeSlot(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.BGM: return 1;
            case SoundCategory.UI: return 3;
            default: return 2;
        }
    }

    private void SetVolumeInternal(int slot, float value01)
    {
        float value = Mathf.Clamp01(value01);
        _volumes[slot] = value;

        PlayerPrefs.SetFloat(PREFS_PREFIX + slot, value);

        ApplyToMixer(slot, value);
    }

    private void LoadVolumes()
    {
        for (int slot = 0; slot < _volumes.Length; slot++)
        {
            _volumes[slot] = PlayerPrefs.GetFloat(PREFS_PREFIX + slot, 1f);
            ApplyToMixer(slot, _volumes[slot]);
        }
    }

    private void ApplyToMixer(int slot, float value)
    {
        if (mixer == null)
        {
            return;
        }

        string param = ParamFor(slot);
        if (string.IsNullOrEmpty(param))
        {
            return;
        }

        // 사람 귀는 로그로 듣는다. 0.5를 그대로 넣으면 절반이 아니라 거의 그대로 들린다.
        float decibel = value <= 0.0001f ? MIN_DECIBEL : Mathf.Log10(value) * 20f;
        mixer.SetFloat(param, decibel);
    }

    private string ParamFor(int slot)
    {
        switch (slot)
        {
            case 0: return masterParam;
            case 1: return bgmParam;
            case 2: return sfxParam;
            case 3: return uiParam;
            default: return null;
        }
    }

    /// <summary>
    /// 믹서가 없을 때 쓸 배율. 믹서가 있으면 거기서 이미 줄이므로 1을 돌려준다.
    /// 두 곳에서 같이 줄이면 볼륨 절반이 4분의 1이 된다.
    /// </summary>
    private float FallbackVolume(SoundCategory category)
    {
        if (mixer != null)
        {
            return 1f;
        }

        return _volumes[0] * _volumes[VolumeSlot(category)];
    }

    private AudioMixerGroup GroupFor(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.BGM: return bgmGroup;
            case SoundCategory.UI: return uiGroup;
            default: return sfxGroup;
        }
    }

    private void OnValidate()
    {
        voiceCount = Mathf.Clamp(voiceCount, 1, 64);
        bgmFadeSeconds = Mathf.Max(0f, bgmFadeSeconds);
    }
}
