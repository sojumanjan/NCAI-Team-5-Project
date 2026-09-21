/// <summary>
/// 재생 중인 소리 하나를 가리키는 표. 반복되는 소리를 멈추려면 이걸 들고 있어야 한다.
///
/// AudioSource를 직접 들고 있으면 안 되는 이유가 있다. 매니저는 소스를
/// 돌려쓰기 때문에, 내 소리가 끝난 뒤 그 소스는 남의 소리를 내고 있을 수 있다. 그걸 모르고
/// 멈추면 엉뚱한 소리가 꺼진다 — 원인을 찾기 거의 불가능한 종류의 버그다.
///
/// 그래서 자리 번호와 함께 '몇 번째로 쓰인 자리인지'를 같이 들고 다닌다. 세대가 다르면
/// 이미 남의 것이므로 아무 일도 하지 않는다.
/// </summary>
public readonly struct SoundHandle
{
    private readonly int _index;
    private readonly int _generation;

    internal SoundHandle(int index, int generation)
    {
        _index = index;
        _generation = generation;
    }

    /// <summary>아무것도 가리키지 않는 표. 재생에 실패하면 이게 돌아온다.</summary>
    public static SoundHandle None => new SoundHandle(-1, 0);

    /// <summary>진짜 자리를 가리키고 있는지. 아직 울리고 있다는 뜻은 아니다.</summary>
    public bool IsValid => _index >= 0;

    /// <summary>지금도 울리고 있는지.</summary>
    public bool IsPlaying => AudioManager.IsVoicePlaying(_index, _generation);

    /// <summary>멈춘다. 이미 끝났거나 자리가 남에게 넘어갔으면 아무 일도 없다.</summary>
    public void Stop() => AudioManager.StopVoice(_index, _generation, 0f);

    /// <summary>서서히 줄이며 멈춘다.</summary>
    public void FadeOut(float seconds) => AudioManager.StopVoice(_index, _generation, seconds);
}
