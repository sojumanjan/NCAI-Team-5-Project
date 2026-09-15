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

    [Header("Secondary Object")]
    [SerializeField] private RectTransform secondaryObjectPrefab;
    [SerializeField] private Transform secondaryObjectParent;
    [SerializeField] private float secondaryObjectMinDistance = 90f;
    [SerializeField] private float secondaryObjectMaxDistance = 160f;
    [SerializeField] private float secondaryObjectSpawnAtRemaining = 30f;
    [SerializeField] private float secondaryObjectWarningLeadTime = 5f;
    [SerializeField] private RectTransform spawnMarkerPrefab;
    [SerializeField] private float secondaryAnnounceDuration = 1.5f;
    [SerializeField] private int secondaryObjectLastConfiguredWave = 2;
    [SerializeField] private UITheme theme;

    public event System.Action<int> WaveStarted;

    private TextMeshProUGUI timerText;
    private State state;
    private float remaining;
    private float preNextWaveRemaining;
    private float stateTimer;
    private int currentWave = 1;
    private bool secondaryWarningStarted;
    private bool secondaryObjectSpawned;
    private Vector2 pendingSecondaryPosition;
    private RectTransform activeMarker;
    private float secondaryAnnounceTimer;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();

        if (theme != null && theme.primaryFont != null)
        {
            timerText.font = theme.primaryFont;
        }

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
            timerText.text = GetWaveLabel(currentWave) + " 시작!";
        }
    }

    private void TickCounting()
    {
        if (remaining <= 0f) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        if (currentWave <= secondaryObjectLastConfiguredWave && !secondaryWarningStarted
            && remaining <= secondaryObjectSpawnAtRemaining + secondaryObjectWarningLeadTime)
        {
            secondaryWarningStarted = true;
            pendingSecondaryPosition = RollSecondaryPosition();
            SpawnPositionMarker(pendingSecondaryPosition);
        }

        if (currentWave <= secondaryObjectLastConfiguredWave && !secondaryObjectSpawned && remaining <= secondaryObjectSpawnAtRemaining)
        {
            SpawnSecondaryObject(pendingSecondaryPosition);
            secondaryObjectSpawned = true;
            DestroyPositionMarker();
            secondaryAnnounceTimer = secondaryAnnounceDuration;
        }

        if (secondaryAnnounceTimer > 0f)
        {
            secondaryAnnounceTimer -= Time.deltaTime;
            timerText.text = "이름미정이 생성되었습니다!";
        }
        else if (currentWave <= secondaryObjectLastConfiguredWave && secondaryWarningStarted && !secondaryObjectSpawned)
        {
            timerText.text = "잠시 후에 이름미정이 생성됩니다";
        }
        else
        {
            UpdateCountingText();
        }

        if (remaining <= 0f)
        {
            state = State.ShowingWaveEnd;
            stateTimer = waveEndMessageDuration;
            timerText.text = GetWaveLabel(currentWave) + " 종료!";
        }
    }

    private Vector2 RollSecondaryPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(secondaryObjectMinDistance, secondaryObjectMaxDistance);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private void SpawnPositionMarker(Vector2 position)
    {
        if (spawnMarkerPrefab == null || secondaryObjectParent == null) return;

        activeMarker = Instantiate(spawnMarkerPrefab, secondaryObjectParent);
        activeMarker.anchoredPosition = position;
    }

    private void DestroyPositionMarker()
    {
        if (activeMarker != null)
        {
            Destroy(activeMarker.gameObject);
            activeMarker = null;
        }
    }

    private void SpawnSecondaryObject(Vector2 position)
    {
        if (secondaryObjectPrefab == null || secondaryObjectParent == null) return;

        var obj = Instantiate(secondaryObjectPrefab, secondaryObjectParent);
        obj.anchoredPosition = position;
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
            timerText.text = GetWaveLabel(currentWave) + " 시작!";
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
        secondaryWarningStarted = false;
        secondaryObjectSpawned = false;
        secondaryAnnounceTimer = 0f;
        DestroyPositionMarker();
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

    private string GetWaveLabel(int wave)
    {
        return wave >= totalWaves ? "Final Wave" : wave + " WAVE";
    }
}
