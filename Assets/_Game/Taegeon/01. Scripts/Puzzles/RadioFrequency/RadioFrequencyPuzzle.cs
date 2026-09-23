using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RadioFrequencyPuzzle")]
public sealed class RadioFrequencyPuzzle : MonoBehaviour
{
    #region 참조 및 설정

    [SerializeField] private Slider frequencySlider;
    [SerializeField] private Slider amplitudeSlider;
    [SerializeField] private FrequencyWaveGraphic targetWave;
    [SerializeField] private FrequencyWaveGraphic playerWave;
    [SerializeField] private Text frequencyText;
    [SerializeField] private Text amplitudeText;
    [SerializeField] private Text statusText;
    [SerializeField] private Image lockLight;
    [SerializeField] private Button resetButton;
    [SerializeField] private UnityEvent onSignalMatched = new UnityEvent();
    public bool IsSolved { get; private set; }
    public int TargetFrequency { get; private set; }
    public int TargetAmplitude { get; private set; }
    private float matchingTime;
    [SerializeField] private SoundData tuningSound;
    private SoundHandle tuningHandle = SoundHandle.None;
    private float tuningStopTime;

    #endregion

    #region 이벤트 연결 및 초기화

    /// <summary>
    /// 슬라이더와 초기화 버튼의 입력을 연결합니다.
    /// </summary>
    private void Awake()
    {
        frequencySlider.onValueChanged.AddListener(OnTuningChanged);
        amplitudeSlider.onValueChanged.AddListener(OnTuningChanged);
        resetButton.onClick.AddListener(ResetPuzzle);
        ResetPuzzle();
    }
    /// <summary>
    /// 새 목표 신호를 만들고 조작 상태를 초기화합니다.
    /// </summary>
    public void ResetPuzzle()
    {
        tuningHandle.Stop();
        IsSolved = false; matchingTime = 0;
        TargetFrequency = Random.Range(3, 9);
        TargetAmplitude = Random.Range(4, 10);
        frequencySlider.interactable = amplitudeSlider.interactable = true;
        frequencySlider.SetValueWithoutNotify(1);
        amplitudeSlider.SetValueWithoutNotify(2);
        targetWave.SetSignal(TargetFrequency, TargetAmplitude / 10f);
        playerWave.color = new Color(.95f, .68f, .25f);
        RefreshSignals();
    }
    #endregion

    #region 신호 조절 및 표시

    /// <summary>
    /// 슬라이더 변경에 맞춰 신호와 일치 시간을 갱신합니다.
    /// </summary>
    private void OnTuningChanged(float value)
    {
        OnSliderChanged(value);
        if (!isActiveAndEnabled || IsSolved || tuningSound == null) return;
        tuningStopTime = Time.unscaledTime + .15f;
        if (!tuningHandle.IsPlaying) tuningHandle = AudioManager.PlayAttached(tuningSound, transform);
    }

    private void OnDisable() { tuningHandle.Stop(); }

    private void OnSliderChanged(float value) { matchingTime = 0; RefreshSignals(); }
    /// <summary>
    /// 현재 주파수와 진폭이 목표 신호와 같은지 확인합니다.
    /// </summary>
    private bool Matches()
    {
        return Mathf.RoundToInt(frequencySlider.value) == TargetFrequency &&
               Mathf.RoundToInt(amplitudeSlider.value) == TargetAmplitude;
    }
    /// <summary>
    /// 조절 중인 파형과 수치 안내를 갱신합니다.
    /// </summary>
    private void RefreshSignals()
    {
        playerWave.SetSignal(frequencySlider.value, amplitudeSlider.value / 10f);
        frequencyText.text = "주파수  " + frequencySlider.value.ToString("0") + " Hz";
        amplitudeText.text = "진폭  " + (amplitudeSlider.value / 10f).ToString("0.0");
        lockLight.color = Matches() ? new Color(1f, .72f, .2f) : new Color(.25f, .32f, .3f);
        statusText.text = Matches() ? "신호 확인 중..." : "두 슬라이더로 파형의 간격과 높이를 맞추세요.";
        statusText.color = new Color(.76f, .87f, .83f);
    }
    #endregion

    #region 클리어 판정 및 정리

    /// <summary>
    /// 목표 신호가 일정 시간 유지되면 클리어를 처리합니다.
    /// </summary>
    private void Update()
    {
        if (Time.unscaledTime >= tuningStopTime) tuningHandle.Stop();
        if (IsSolved) return;
        if (!Matches()) { matchingTime = 0; return; }
        matchingTime += Time.unscaledDeltaTime;
        if (matchingTime < .8f) return;
        IsSolved = true;
        tuningHandle.Stop();
        frequencySlider.interactable = amplitudeSlider.interactable = false;
        lockLight.color = new Color(.35f, 1f, .55f);
        playerWave.color = new Color(.35f, 1f, .65f);
        statusText.text = "주파수 동기화 성공!";
        statusText.color = new Color(.35f, 1f, .65f);
        onSignalMatched.Invoke();
    }
    /// <summary>
    /// 등록했던 UI 이벤트를 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (frequencySlider != null) frequencySlider.onValueChanged.RemoveListener(OnTuningChanged);
        if (amplitudeSlider != null) amplitudeSlider.onValueChanged.RemoveListener(OnTuningChanged);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetPuzzle);
    }
    #endregion

}
}
