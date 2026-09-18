using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Yusong
{
public class CountdownTimer : MonoBehaviour
{
    private enum State
    {
        Counting,
        ShowingWaveEnd,
        PreNextWave,
        ShowingWaveStart,
        Finished
    }

    [SerializeField] private float startSeconds = 60f;
    [SerializeField] private float wave0Seconds = 15f;
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private GameObject tutorialSpotlight;
    [SerializeField] private GameObject introExplainGroup;
    [SerializeField] private RectTransform enemyHighlightCircle;
    [SerializeField] private RectTransform enemyCalloutText;
    [SerializeField] private RectTransform comboGaugeTarget;
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

    [Header("Final Wave Boss")]
    [SerializeField] private RectTransform bossPrefab;
    [SerializeField] private float bossSpawnAtRemaining = 45f;
    [SerializeField] private float bossWarningLeadTime = 5f;
    [SerializeField] private float bossAnnounceDuration = 2f;
    [SerializeField] private float bossSpawnOffset = 60f;

    [Header("Game Clear")]
    [SerializeField] private GameObject gameOverScreen;

    public event System.Action<int> WaveStarted;
    public event System.Action WaveEnding;

    public static bool IsWaveActive { get; private set; }

    private TextMeshProUGUI timerText;
    private State state;
    private float remaining;
    private float preNextWaveRemaining;
    private float stateTimer;
    private int currentWave = 0;
    private bool secondaryWarningStarted;
    private bool secondaryObjectSpawned;
    private Vector2 pendingSecondaryPosition;
    private RectTransform activeMarker;
    private float secondaryAnnounceTimer;
    private bool bossWarningStarted;
    private bool bossSpawned;
    private Vector2 pendingBossPosition;
    private RectTransform activeBossMarker;
    private float bossAnnounceTimer;
    private bool waitingForIntroClick;
    private bool pausedForObjectHighlight;
    private bool pausedForComboGaugeHighlight;
    private List<RectTransform> highlightedObjects;
    private List<Transform> highlightedObjectsOriginalParents;

    public bool IsWaitingForIntroClick => waitingForIntroClick;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();

        if (theme != null && theme.primaryFont != null)
        {
            timerText.font = theme.primaryFont;
        }
    }

    private void Start()
    {
        pausedForObjectHighlight = false;
        pausedForComboGaugeHighlight = false;
        highlightedObjects = null;
        highlightedObjectsOriginalParents = null;
        if (introExplainGroup != null) introExplainGroup.SetActive(true);
        if (enemyHighlightCircle != null) enemyHighlightCircle.gameObject.SetActive(false);
        if (enemyCalloutText != null) enemyCalloutText.gameObject.SetActive(false);
        if (tutorialSpotlight != null) tutorialSpotlight.SetActive(true);
        timerText.text = Mathf.CeilToInt(wave0Seconds).ToString();
        Time.timeScale = 0f;
        waitingForIntroClick = true;
    }

    private void Update()
    {
        if (waitingForIntroClick)
        {
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!clicked && Touchscreen.current != null) clicked = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (clicked)
            {
                waitingForIntroClick = false;

                // Move the object(s) back to their real parent *before* the spotlight that
                // currently holds them gets deactivated below, otherwise they get deactivated
                // right along with it and are stuck there forever (enemies never destroyed).
                if (pausedForObjectHighlight && highlightedObjects != null)
                {
                    for (int i = 0; i < highlightedObjects.Count; i++)
                    {
                        RectTransform obj = highlightedObjects[i];
                        Transform originalParent = highlightedObjectsOriginalParents[i];
                        if (obj == null || originalParent == null) continue;

                        Vector3 worldPos = obj.TransformPoint(obj.rect.center);
                        obj.SetParent(originalParent, false);
                        obj.localScale = Vector3.one;
                        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)originalParent, screenPoint, null, out Vector2 restoredPoint))
                        {
                            obj.anchoredPosition = restoredPoint;
                        }
                    }
                }
                highlightedObjects = null;
                highlightedObjectsOriginalParents = null;

                if (tutorialSpotlight != null) tutorialSpotlight.SetActive(false);
                Time.timeScale = 1f;

                bool wasHighlightPause = pausedForObjectHighlight || pausedForComboGaugeHighlight;
                pausedForObjectHighlight = false;
                pausedForComboGaugeHighlight = false;

                if (wasHighlightPause)
                {
                    if (enemyHighlightCircle != null) enemyHighlightCircle.gameObject.SetActive(false);
                    if (enemyCalloutText != null) enemyCalloutText.gameObject.SetActive(false);
                }
                else
                {
                    StartWave();
                }
            }
            return;
        }

        IsWaveActive = state == State.Counting;

        switch (state)
        {
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

    private void TickCounting()
    {
        if (remaining <= 0f) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        bool isFinalWave = currentWave >= totalWaves;

        if (isFinalWave && !bossWarningStarted && remaining <= bossSpawnAtRemaining + bossWarningLeadTime)
        {
            bossWarningStarted = true;
            pendingBossPosition = RollBossPosition();
            SpawnBossPositionMarker(pendingBossPosition);
        }

        if (isFinalWave && !bossSpawned && remaining <= bossSpawnAtRemaining)
        {
            SpawnBoss(pendingBossPosition);
            bossSpawned = true;
            DestroyBossPositionMarker();
            bossAnnounceTimer = bossAnnounceDuration;
        }

        bool secondaryEligibleWave = currentWave >= 1 && currentWave <= secondaryObjectLastConfiguredWave;

        if (secondaryEligibleWave && !secondaryWarningStarted
            && remaining <= secondaryObjectSpawnAtRemaining + secondaryObjectWarningLeadTime)
        {
            secondaryWarningStarted = true;
            pendingSecondaryPosition = RollSecondaryPosition();
            SpawnPositionMarker(pendingSecondaryPosition);
        }

        if (secondaryEligibleWave && !secondaryObjectSpawned && remaining <= secondaryObjectSpawnAtRemaining)
        {
            SpawnSecondaryObject(pendingSecondaryPosition);
            secondaryObjectSpawned = true;
            DestroyPositionMarker();
            secondaryAnnounceTimer = secondaryAnnounceDuration;
        }

        if (bossAnnounceTimer > 0f)
        {
            bossAnnounceTimer -= Time.deltaTime;
            timerText.text = "파이널 보스 등장!";
        }
        else if (isFinalWave && bossWarningStarted && !bossSpawned)
        {
            timerText.text = "보스가 접근하고 있습니다!";
        }
        else if (secondaryAnnounceTimer > 0f)
        {
            secondaryAnnounceTimer -= Time.deltaTime;
            timerText.text = "이름미정이 생성되었습니다!";
        }
        else if (secondaryEligibleWave && secondaryWarningStarted && !secondaryObjectSpawned)
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
            WaveEnding?.Invoke();
        }
    }

    private Vector2 RollBossPosition()
    {
        var rectTransform = secondaryObjectParent as RectTransform;
        if (rectTransform == null) return Vector2.zero;

        Rect rect = rectTransform.rect;
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float tx = Mathf.Approximately(cos, 0f) ? float.MaxValue : halfWidth / Mathf.Abs(cos);
        float ty = Mathf.Approximately(sin, 0f) ? float.MaxValue : halfHeight / Mathf.Abs(sin);
        float t = Mathf.Min(tx, ty) + bossSpawnOffset;

        return new Vector2(cos * t, sin * t);
    }

    private void SpawnBossPositionMarker(Vector2 position)
    {
        if (spawnMarkerPrefab == null || secondaryObjectParent == null) return;

        activeBossMarker = Instantiate(spawnMarkerPrefab, secondaryObjectParent);
        activeBossMarker.anchoredPosition = position;
    }

    private void DestroyBossPositionMarker()
    {
        if (activeBossMarker != null)
        {
            Destroy(activeBossMarker.gameObject);
            activeBossMarker = null;
        }
    }

    private void SpawnBoss(Vector2 position)
    {
        if (bossPrefab == null || secondaryObjectParent == null) return;

        var boss = Instantiate(bossPrefab, secondaryObjectParent);
        boss.anchoredPosition = position;
    }

    private void TriggerGameClear()
    {
        if (ScoreManager.Instance != null && PlayerHealth.Instance != null)
        {
            ScoreManager.Instance.AddRemainingHpScore(PlayerHealth.Instance.CalculateRemainingHpScore());
        }

        if (gameOverScreen != null)
        {
            var screen = gameOverScreen.GetComponent<GameOverScreen>();
            if (screen != null) screen.Show("CLEAR!");
            else gameOverScreen.SetActive(true);
        }

        var raycaster = GetComponentInParent<Canvas>()?.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        if (PlayerHealth.Instance != null)
        {
            float score01 = (float)PlayerHealth.Instance.CurrentHealth / PlayerHealth.Instance.MaxHealth;
            GameFlow.Instance?.ReportCurrent(new MiniGameResult(true, score01));
        }
        else
        {
            GameFlow.Instance?.ReportCurrent(new MiniGameResult(true, 1f));
        }

        Time.timeScale = 0f;
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
            TriggerGameClear();
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

    public void PauseForEnemyHighlight(RectTransform enemyRect)
    {
        if (enemyRect == null) return;
        PauseForObjectsHighlight(new List<RectTransform> { enemyRect }, "접근하는 적군을 클릭을 통해 제거할 수 있습니다");
    }

    public void PauseForGroupHighlight(List<RectTransform> group, string message)
    {
        PauseForObjectsHighlight(group, message);
    }

    public void PauseForComboGaugeHighlight(string message)
    {
        if (comboGaugeTarget == null) return;
        Vector3 worldPos = comboGaugeTarget.TransformPoint(comboGaugeTarget.rect.center);
        ShowHighlightPause(worldPos, message);
        pausedForComboGaugeHighlight = true;
    }

    private void PauseForObjectsHighlight(List<RectTransform> objects, string message)
    {
        if (objects == null || objects.Count == 0) return;
        if (tutorialSpotlight == null) return;
        if (introExplainGroup != null) introExplainGroup.SetActive(false);

        RectTransform spotlightRect = (RectTransform)tutorialSpotlight.transform;
        highlightedObjects = new List<RectTransform>();
        highlightedObjectsOriginalParents = new List<Transform>();

        // Bring each object above the dim overlay/highlight circle for this beat (they normally
        // sit behind it, layered under the field) so they stay clearly visible instead of being
        // washed out underneath a bright highlight. worldPositionStays is left false and the
        // position is set explicitly afterwards to avoid corrupting scale.
        Vector2 localPointSum = Vector2.zero;
        int placedCount = 0;

        foreach (var obj in objects)
        {
            if (obj == null) continue;

            Vector3 worldPos = obj.TransformPoint(obj.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(spotlightRect, screenPoint, null, out Vector2 localPoint)) continue;

            highlightedObjects.Add(obj);
            highlightedObjectsOriginalParents.Add(obj.parent);

            obj.SetParent(spotlightRect, false);
            obj.localScale = Vector3.one;
            obj.anchoredPosition = localPoint;
            obj.SetAsLastSibling();

            localPointSum += localPoint;
            placedCount++;
        }

        if (placedCount == 0) return;

        // Center the highlight circle on the group's centroid so a single-object call and a
        // multi-object call share the exact same positioning path.
        ShowHighlightPauseAtLocalPoint(localPointSum / placedCount, message);
        pausedForObjectHighlight = true;
    }

    private void ShowHighlightPause(Vector3 worldPos, string message)
    {
        if (tutorialSpotlight == null) return;
        RectTransform spotlightRect = (RectTransform)tutorialSpotlight.transform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(spotlightRect, screenPoint, null, out Vector2 localPoint))
        {
            ShowHighlightPauseAtLocalPoint(localPoint, message);
        }
    }

    private void ShowHighlightPauseAtLocalPoint(Vector2 localPoint, string message)
    {
        if (waitingForIntroClick) return;

        if (enemyHighlightCircle != null && tutorialSpotlight != null)
        {
            // Clamp so the circle never runs off-screen for targets near an edge/corner
            // (e.g. the combo gauge, which sits in the top-left).
            RectTransform spotlightRect = (RectTransform)tutorialSpotlight.transform;
            Rect bounds = spotlightRect.rect;
            float halfW = enemyHighlightCircle.sizeDelta.x * 0.5f;
            float halfH = enemyHighlightCircle.sizeDelta.y * 0.5f;
            const float margin = 10f;
            localPoint.x = Mathf.Clamp(localPoint.x, bounds.xMin + halfW + margin, bounds.xMax - halfW - margin);
            localPoint.y = Mathf.Clamp(localPoint.y, bounds.yMin + halfH + margin, bounds.yMax - halfH - margin);

            enemyHighlightCircle.anchoredPosition = localPoint;
            enemyHighlightCircle.gameObject.SetActive(true);

            if (enemyCalloutText != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var calloutTmp = enemyCalloutText.GetComponent<TextMeshProUGUI>();
                    if (calloutTmp != null) calloutTmp.text = message;

                    // Keep it horizontally centered on screen (the target can be near a side edge)
                    // and only follow the circle's height.
                    float gapBelowCircle = enemyHighlightCircle.sizeDelta.y * 0.5f + 20f;
                    enemyCalloutText.anchoredPosition = new Vector2(0f, localPoint.y - gapBelowCircle);
                    enemyCalloutText.gameObject.SetActive(true);
                }
                else
                {
                    enemyCalloutText.gameObject.SetActive(false);
                }
            }
        }

        if (tutorialSpotlight != null) tutorialSpotlight.SetActive(true);
        Time.timeScale = 0f;
        waitingForIntroClick = true;
    }

    private void StartWave()
    {
        if (ComboManager.Instance != null) ComboManager.Instance.ResetState();

        remaining = currentWave == 0 ? wave0Seconds : startSeconds;
        state = State.Counting;
        secondaryWarningStarted = false;
        secondaryObjectSpawned = false;
        secondaryAnnounceTimer = 0f;
        DestroyPositionMarker();
        bossWarningStarted = false;
        bossSpawned = false;
        bossAnnounceTimer = 0f;
        DestroyBossPositionMarker();
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
        if (wave == 0) return "0 WAVE";
        return wave >= totalWaves ? "Final Wave" : wave + " WAVE";
    }
}
}
