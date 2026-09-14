using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    private enum State
    {
        PreGame,
        Counting,
        ShowingWaveEnd,
        PreNextWave,
        ShowingWaveStart,
        Finished
    }

    [SerializeField] private float startSeconds = 60f;
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private float preGameSeconds = 3f;
    [SerializeField] private float waveEndMessageDuration = 2f;
    [SerializeField] private float preNextWaveSeconds = 5f;
    [SerializeField] private float waveStartMessageDuration = 2f;

    public event System.Action<int> WaveStarted;

    private TextMeshProUGUI timerText;
    private State state;
    private float remaining;
    private float preNextWaveRemaining;
    private float stateTimer;
    private int currentWave = 1;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        state = State.PreGame;
        preNextWaveRemaining = preGameSeconds;
        UpdateCountdownMessage(preNextWaveRemaining, "{0}초 후 게임이 시작됩니다..");
    }

    private void Update()
    {
        switch (state)
        {
            case State.PreGame:
                TickPreGame();
                break;
            case State.Counting:
                TickCounting();
                break;
            case State.ShowingWaveEnd:
                TickWaveEnd();
                break;
            case State.PreNextWave:
                TickPreNextWave();
                break;
            case State.ShowingWaveStart:
                TickWaveStart();
                break;
            case State.Finished:
                break;
        }
    }

    private void TickPreGame()
    {
        preNextWaveRemaining = Mathf.Max(0f, preNextWaveRemaining - Time.deltaTime);
        UpdateCountdownMessage(preNextWaveRemaining, "{0}초 후 게임이 시작됩니다..");

        if (preNextWaveRemaining <= 0f)
        {
            state = State.ShowingWaveStart;
            stateTimer = waveStartMessageDuration;
            timerText.text = currentWave + " WAVE 시작!";
        }
    }

    private void TickCounting()
    {
        if (remaining <= 0f) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        UpdateCountingText();

        if (remaining <= 0f)
        {
            state = State.ShowingWaveEnd;
            stateTimer = waveEndMessageDuration;
            timerText.text = currentWave + " WAVE 종료!";
        }
    }

    private void TickWaveEnd()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (currentWave >= totalWaves)
        {
            state = State.Finished;
            return;
        }

        state = State.PreNextWave;
        preNextWaveRemaining = preNextWaveSeconds;
        UpdateCountdownMessage(preNextWaveRemaining, "{0}초 후에 다음 웨이브가 시작됩니다..");
    }

    private void TickPreNextWave()
    {
        preNextWaveRemaining = Mathf.Max(0f, preNextWaveRemaining - Time.deltaTime);
        UpdateCountdownMessage(preNextWaveRemaining, "{0}초 후에 다음 웨이브가 시작됩니다..");

        if (preNextWaveRemaining <= 0f)
        {
            currentWave++;
            state = State.ShowingWaveStart;
            stateTimer = waveStartMessageDuration;
            timerText.text = currentWave + " WAVE 시작!";
        }
    }

    private void TickWaveStart()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        StartWave();
    }

    private void StartWave()
    {
        remaining = startSeconds;
        state = State.Counting;
        UpdateCountingText();
        WaveStarted?.Invoke(currentWave);
    }

    private void UpdateCountdownMessage(float remainingTime, string template)
    {
        int secondsLeft = Mathf.CeilToInt(remainingTime);
        timerText.text = string.Format(template, secondsLeft);
    }

    private void UpdateCountingText()
    {
        timerText.text = Mathf.CeilToInt(remaining).ToString();
    }
}
