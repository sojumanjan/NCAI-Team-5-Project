using System.Collections;
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
    [SerializeField] private float wave0Seconds = 25f;
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
    [SerializeField] private RectTransform secondaryMarkerPrefab;
    [SerializeField] private float secondaryAnnounceDuration = 1.5f;
    [SerializeField] private int secondaryObjectLastConfiguredWave = 2;
    [SerializeField] private UITheme theme;

    [Header("Final Wave Boss")]
    [SerializeField] private RectTransform bossPrefab;
    [SerializeField] private float bossSpawnAtRemaining = 45f;
    [SerializeField] private float bossWarningLeadTime = 5f;
    [SerializeField] private float bossAnnounceDuration = 2f;
    [SerializeField] private float bossSpawnOffset = 60f;
    [SerializeField] private float bossMarkerCornerInset = 120f;

    [Header("Game Clear")]
    [SerializeField] private GameObject gameOverScreen;

    [Header("Sound")]
    [SerializeField] private SoundData mainBgm;
    [SerializeField] private SoundData waveStartSound;
    [SerializeField] private SoundData bossAppearSound;
    [SerializeField] private SoundData secondaryAppearSound;

    public event System.Action<int> WaveStarted;
    public event System.Action WaveEnding;
    public static event System.Action GameCleared;

    public static bool IsWaveActive { get; private set; }
    public static bool IsTutorialWave { get; private set; } = true;

    // "다시하기"로 씬을 다시 로드할 때 이 값을 true로 세팅해두면, 이번 Start()에서는
    // 튜토리얼(0 웨이브)을 건너뛰고 바로 1 웨이브부터 시작한다. static이라 씬 리로드에도 살아남는다.
    public static bool SkipTutorial;

    private TextMeshProUGUI timerText;
    private Color normalTextColor;

    private Taegeon.MenuEscapeToggle sharedMenu;
    private GameObject sharedMenuPauseRoot;
    private GameObject sharedMenuSettingsRoot;
    private int nextSharedMenuSearchFrame;
    private int lastSharedMenuOpenFrame = -10;
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
    private Vector2 pendingBossMarkerPosition;
    private RectTransform activeBossMarker;
    private float bossAnnounceTimer;
    private bool waitingForIntroClick;
    private bool pausedForObjectHighlight;
    private bool pausedForComboGaugeHighlight;
    private List<RectTransform> highlightedObjects;
    private List<Transform> highlightedObjectsOriginalParents;

    public bool IsWaitingForIntroClick => waitingForIntroClick;
    public int CurrentWave => currentWave;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        normalTextColor = timerText.color;

        if (theme != null && theme.primaryFont != null)
        {
            timerText.font = theme.primaryFont;
        }
    }

    private void Start()
    {
        AudioManager.PlayBGM(mainBgm);

        if (SkipTutorial)
        {
            SkipTutorial = false;
            currentWave = 1;
            waitingForIntroClick = false;
            if (tutorialSpotlight != null) tutorialSpotlight.SetActive(false);
            Time.timeScale = 1f;
            // 다시하기 경로는 "WAVE 시작!" 안내 없이 바로 시작하므로 여기서 따로 울려준다.
            AudioManager.Play(waveStartSound);
            StartWave();
            return;
        }

        pausedForObjectHighlight = false;
        pausedForComboGaugeHighlight = false;
        highlightedObjects = null;
        highlightedObjectsOriginalParents = null;
        if (introExplainGroup != null) introExplainGroup.SetActive(true);
        if (enemyHighlightCircle != null) enemyHighlightCircle.gameObject.SetActive(false);
        if (enemyCalloutText != null) enemyCalloutText.gameObject.SetActive(false);
        timerText.text = Mathf.CeilToInt(wave0Seconds).ToString();
        Time.timeScale = 0f;
        waitingForIntroClick = true;

        // 인트로 암전 연출이 화면을 다 보여주기 전에 튜토리얼 암전(DimOverlay)이 먼저
        // 켜지면 페이드가 거의 안 보이게 되므로, 인트로가 끝난 뒤에 켠다.
        StartCoroutine(ActivateTutorialSpotlightAfterIntroFade());
    }

    private IEnumerator ActivateTutorialSpotlightAfterIntroFade()
    {
        while (IntroFadeIn.IsPlaying)
        {
            yield return null;
        }

        if (tutorialSpotlight != null) tutorialSpotlight.SetActive(true);
    }

    private void Update()
    {
        if (waitingForIntroClick)
        {
            // ESC 공용 메뉴가 열려 있을 때(또는 방금 닫힌 프레임)의 클릭은 메뉴 버튼을 누른 것이라 튜토리얼 진행으로 치지 않는다.
            // 안 막으면 메뉴 뒤에서 튜토리얼이 넘어가 버리고, 재개 시 메뉴가 timeScale을 0으로 되돌려 게임이 멈춘 채 남는다.
            if (IsSharedMenuOpen()) lastSharedMenuOpenFrame = Time.frameCount;
            bool blockedByMenu = Time.frameCount - lastSharedMenuOpenFrame <= 1;

            bool clicked = !blockedByMenu && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!clicked && !blockedByMenu && Touchscreen.current != null) clicked = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

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
            SpawnBossPositionMarker(pendingBossMarkerPosition);
        }

        if (isFinalWave && !bossSpawned && remaining <= bossSpawnAtRemaining)
        {
            SpawnBoss(pendingBossPosition);
            AudioManager.Play(bossAppearSound);
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
            AudioManager.Play(secondaryAppearSound);
            secondaryObjectSpawned = true;
            DestroyPositionMarker();
            secondaryAnnounceTimer = secondaryAnnounceDuration;
        }

        bool bossAlert = bossAnnounceTimer > 0f || (isFinalWave && bossWarningStarted && !bossSpawned);
        // 평소 안내와 같은 색이면 보스 경고를 그냥 흘려보기 쉬워서, 보스 문구가 떠 있는 동안만 경고색으로 바꾼다.
        timerText.color = bossAlert ? BossAlertColor : normalTextColor;

        if (bossAnnounceTimer > 0f)
        {
            bossAnnounceTimer -= Time.deltaTime;
            timerText.text = "보스 등장!";
        }
        else if (isFinalWave && bossWarningStarted && !bossSpawned)
        {
            timerText.text = "보스가 접근하고 있습니다!";
        }
        else if (secondaryAnnounceTimer > 0f)
        {
            secondaryAnnounceTimer -= Time.deltaTime;
            timerText.text = "다슬이가 나타났습니다!";
        }
        else if (secondaryEligibleWave && secondaryWarningStarted && !secondaryObjectSpawned)
        {
            timerText.text = "잠시 후 다슬이가 정화를 돕기 위해 나타납니다..";
        }
        else
        {
            UpdateCountingText();
        }

        if (remaining <= 0f)
        {
            timerText.color = normalTextColor;
            state = State.ShowingWaveEnd;
            stateTimer = waveEndMessageDuration;
            timerText.text = GetWaveLabel(currentWave) + " 종료!";
            WaveEnding?.Invoke();
        }
    }

    // 수원(필드 중앙)에서 가장 먼 곳이 네 꼭짓점이라, 그중 한 곳에서만 등장시켜 보스가 다가오는 시간을 최대로 확보한다.
    // 공용 메뉴는 씬이 뜬 뒤 자동으로 생성되므로 필요할 때 찾는다. 없는 씬이면 매 프레임 찾지 않도록 간격을 둔다.
    private bool IsSharedMenuOpen()
    {
        if (sharedMenu == null)
        {
            if (Time.frameCount < nextSharedMenuSearchFrame) return false;
            nextSharedMenuSearchFrame = Time.frameCount + 30;

            sharedMenu = FindFirstObjectByType<Taegeon.MenuEscapeToggle>(FindObjectsInactive.Include);
            if (sharedMenu == null) return false;

            Transform pause = sharedMenu.transform.Find("Pause Root");
            Transform settings = sharedMenu.transform.Find("MenuRoot");
            sharedMenuPauseRoot = pause != null ? pause.gameObject : null;
            sharedMenuSettingsRoot = settings != null ? settings.gameObject : null;
        }

        return (sharedMenuPauseRoot != null && sharedMenuPauseRoot.activeInHierarchy)
            || (sharedMenuSettingsRoot != null && sharedMenuSettingsRoot.activeInHierarchy);
    }

    private Color BossAlertColor =>theme != null ? theme.bossAlertTextColor : new Color(0.9f, 0.1f, 0.08f, 1f);

    private Vector2 RollBossPosition()
    {
        var rectTransform = secondaryObjectParent as RectTransform;
        if (rectTransform == null) return Vector2.zero;

        Rect rect = rectTransform.rect;
        float sx = Random.value < 0.5f ? -1f : 1f;
        float sy = Random.value < 0.5f ? -1f : 1f;

        Vector2 corner = rect.center + new Vector2(sx * rect.width * 0.5f, sy * rect.height * 0.5f);
        Vector2 outward = (corner - rect.center).normalized;

        // 경고 마커는 필드 밖(실제 등장 지점)에 두면 마스크에 잘려 안 보이므로, 같은 꼭짓점의 필드 안쪽에 둔다.
        pendingBossMarkerPosition = corner - new Vector2(sx, sy) * bossMarkerCornerInset;

        return corner + outward * bossSpawnOffset;
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

        // 캔버스 레이캐스터 자체를 끄면 결과 화면의 다시하기/메인메뉴 버튼도 같이 막혀버린다.
        // GameOverScreen 배경 이미지가 raycastTarget=true라 뒤쪽 게임 요소는 어차피 가려진다.

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
        GameCleared?.Invoke();
    }

    private Vector2 RollSecondaryPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(secondaryObjectMinDistance, secondaryObjectMaxDistance);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private void SpawnPositionMarker(Vector2 position)
    {
        if (secondaryMarkerPrefab == null || secondaryObjectParent == null) return;

        activeMarker = Instantiate(secondaryMarkerPrefab, secondaryObjectParent);
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
            AudioManager.Play(waveStartSound);
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
        PauseForObjectsHighlight(new List<RectTransform> { enemyRect },
            "접근하는 적군을 클릭을 통해 제거할 수 있습니다\n적군이 중앙에 도달하면 체력이 줄어들고, 체력이 모두 소진되면 게임이 종료됩니다");
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

        IsTutorialWave = currentWave == 0;

        // Wave 0 is the tutorial — whatever score/damage the player picked up while practicing
        // shouldn't carry into the real run that starts at Wave 1.
        if (currentWave == 1)
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.ResetHealth();
        }

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
        if (wave == 0) return "튜토리얼 WAVE";
        return wave >= totalWaves ? "Final Wave" : wave + " WAVE";
    }
}
}
