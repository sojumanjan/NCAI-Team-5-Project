using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class RadioFrequencyPuzzle : MonoBehaviour
{
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

    private void Awake()
    {
        frequencySlider.onValueChanged.AddListener(OnSliderChanged);
        amplitudeSlider.onValueChanged.AddListener(OnSliderChanged);
        resetButton.onClick.AddListener(ResetPuzzle);
        ResetPuzzle();
    }
    public void ResetPuzzle()
    {
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
    private void OnSliderChanged(float value) { matchingTime = 0; RefreshSignals(); }
    private bool Matches()
    {
        return Mathf.RoundToInt(frequencySlider.value) == TargetFrequency &&
               Mathf.RoundToInt(amplitudeSlider.value) == TargetAmplitude;
    }
    private void RefreshSignals()
    {
        playerWave.SetSignal(frequencySlider.value, amplitudeSlider.value / 10f);
        frequencyText.text = "주파수  " + frequencySlider.value.ToString("0") + " Hz";
        amplitudeText.text = "진폭  " + (amplitudeSlider.value / 10f).ToString("0.0");
        lockLight.color = Matches() ? new Color(1f, .72f, .2f) : new Color(.25f, .32f, .3f);
        statusText.text = Matches() ? "신호 확인 중..." : "두 슬라이더로 파형의 간격과 높이를 맞추세요.";
        statusText.color = new Color(.76f, .87f, .83f);
    }
    private void Update()
    {
        if (IsSolved) return;
        if (!Matches()) { matchingTime = 0; return; }
        matchingTime += Time.unscaledDeltaTime;
        if (matchingTime < .8f) return;
        IsSolved = true;
        frequencySlider.interactable = amplitudeSlider.interactable = false;
        lockLight.color = new Color(.35f, 1f, .55f);
        playerWave.color = new Color(.35f, 1f, .65f);
        statusText.text = "주파수 동기화 성공!";
        statusText.color = new Color(.35f, 1f, .65f);
        onSignalMatched.Invoke();
    }
    private void OnDestroy()
    {
        if (frequencySlider != null) frequencySlider.onValueChanged.RemoveListener(OnSliderChanged);
        if (amplitudeSlider != null) amplitudeSlider.onValueChanged.RemoveListener(OnSliderChanged);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetPuzzle);
    }
}