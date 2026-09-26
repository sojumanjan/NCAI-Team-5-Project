using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    [Header("Center Intro")]
    [Tooltip("인트로가 끝난 직후 가장 먼저 짚어줄 중앙 수원. 비워두면 이 단계 없이 바로 남은 시간·점수 안내로 갑니다.")]
    [SerializeField] private RectTransform centerIntroTarget;
    [TextArea(1, 3)]
    [SerializeField] private string centerIntroMessage = "가운데 수원을 지켜주세요!\n수원의 체력이 모두 사라지면 게임이 끝납니다";
    [Tooltip("인트로가 끝나고 수원이 뿅 튀어나오는 순간 나는 소리.")]
    [SerializeField] private SoundData waterGenerateSound;

    [Header("Tutorial Transitions")]
    [Tooltip("스포트라이트의 어두운 막. 켤 때는 이것만 페이드 인합니다 — 전체를 페이드하면 막 위로 올린 수원·적이 같이 투명해졌다 나타납니다.")]
    [SerializeField] private UnityEngine.UI.Image dimOverlay;
    [Tooltip("\"아무 곳이나 클릭하면…\" 문구. 스포트라이트가 떠 있는 동안 천천히 깜빡입니다. 비워두면 깜빡이지 않습니다.")]
    [SerializeField] private RectTransform clickHint;
    [Tooltip("어두운 막이 켜지고 꺼지는 시간 (초).")]
    [SerializeField] private float spotlightFadeDuration = 0.3f;
    [Tooltip("하이라이트 원이 톡 튀어나오는 시간 (초).")]
    [SerializeField] private float highlightPopDuration = 0.35f;
    [Tooltip("튀어나온 뒤 숨 쉬듯 커지는 배율. 1이면 가만히 있습니다.")]
    [SerializeField] private float highlightPulseScale = 1.06f;
    [Tooltip("숨 한 번(커졌다 작아짐)의 시간 (초).")]
    [SerializeField] private float highlightPulsePeriod = 1.4f;
    [Tooltip("안내 글이 아래에서 올라오는 거리 (캔버스 픽셀).")]
    [SerializeField] private float calloutRise = 24f;
    [Tooltip("안내 글이 떠오르는 시간 (초).")]
    [SerializeField] private float calloutFadeDuration = 0.3f;
    [Tooltip("남은 시간·점수 안내의 원과 글이 하나씩 차례로 뜨는 간격 (초).")]
    [SerializeField] private float explainStagger = 0.1f;
    [Tooltip("사라지는 안내가 흐려지는 시간 (초).")]
    [SerializeField] private float elementHideDuration = 0.15f;
    [Tooltip("새 안내가 뜬 직후 클릭을 받지 않는 시간 (초). 연타로 안내를 보지도 않고 넘기는 것을 막습니다.")]
    [SerializeField] private float stepInputDelay = 0.35f;

    [Header("Debug")]
    [Tooltip("누르면 지금 웨이브를 바로 끝냅니다. 에디터에서만 동작합니다.")]
    [SerializeField] private Key debugEndWaveKey = Key.F2;

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

    // 수원은 적과 달리 판 내내 필드에 남는다. 부모만 되돌리면 형제 순서가 맨 뒤로 밀려 그리는 순서가 바뀌므로 전부 기억해 둔다.
    private bool pausedForCenterIntro;

    // 클릭 대기는 Start에서 바로 켜지지만 첫 안내는 인트로 페이드가 끝나야 뜬다. 그 사이 클릭을 받으면
    // 안내를 보지도 않고 넘긴 것으로 처리돼 웨이브가 곧장 시작되므로, 첫 안내가 뜰 때까지 클릭을 무시한다.
    private bool introStepsReady;

    private CanvasGroup spotlightGroup;
    private float stepInputUnlockTime;
    private float dimAlpha = -1f;

    // 트윈이 중간에 끊기면 크기·위치가 어중간하게 남는다. 처음 모습을 기억해 두고 매번 거기서 다시 시작한다.
    private readonly Dictionary<RectTransform, Vector3> baseScales = new Dictionary<RectTransform, Vector3>();
    private readonly Dictionary<RectTransform, Vector2> restPositions = new Dictionary<RectTransform, Vector2>();
    private Transform centerIntroParent;
    private int centerIntroSiblingIndex;
    private Vector2 centerIntroAnchoredPosition;
    private Vector3 centerIntroScale;

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

        // 수원은 인트로 페이드가 끝나는 순간 톡 튀어나온다. 그 전까지는 크기 0으로 숨겨 둔다.
        // 원래 크기를 먼저 기억해야 한다 — 0으로 만든 뒤에 재면 0이 기준이 되어 영영 안 나온다.
        if (centerIntroTarget != null)
        {
            BaseScaleOf(centerIntroTarget);
            centerIntroTarget.localScale = Vector3.zero;
        }

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
            StartCoroutine(PopCenterAfterIntroFade());
            return;
        }

        pausedForObjectHighlight = false;
        pausedForComboGaugeHighlight = false;
        pausedForCenterIntro = false;
        introStepsReady = false;
        highlightedObjects = null;
        highlightedObjectsOriginalParents = null;
        // 수원 안내가 있으면 그게 먼저다. 남은 시간·점수 안내는 수원 안내를 넘긴 뒤에 켠다.
        if (introExplainGroup != null) introExplainGroup.SetActive(centerIntroTarget == null);
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

        if (centerIntroTarget != null)
        {
            // 수원이 제자리에서 다 튀어나온 뒤에 막을 깔고 설명한다. 막과 함께 나오면 튀어나오는 순간이 가려진다.
            PopCenter();
            yield return new WaitForSecondsRealtime(highlightPopDuration);

            // 스포트라이트를 먼저 켠 뒤에 옮긴다. 꺼진 부모 밑으로 옮기면 수원이 잠깐 비활성이 되어 OnDisable이 돈다.
            ShowSpotlight();
            ShowCenterIntro();
        }
        else
        {
            ShowSpotlight();
            AnimateExplainIn();
        }

        // 페이드가 끝나는 프레임에 눌린 클릭까지 이번 안내를 넘기지 않게, 다음 프레임부터 받는다.
        yield return null;
        introStepsReady = true;
    }

    // 다시하기는 튜토리얼 없이 곧장 시작하지만, 수원이 튀어나오는 첫인상은 같게 맞춘다.
    private IEnumerator PopCenterAfterIntroFade()
    {
        while (IntroFadeIn.IsPlaying) yield return null;
        PopCenter();
    }

    private void PopCenter()
    {
        if (centerIntroTarget == null) return;

        PopIn(centerIntroTarget, 0f, false);
        if (waterGenerateSound != null) AudioManager.Play(waterGenerateSound);
    }

    /// <summary>수원을 어두운 막 위로 올리고, 적 소개와 같은 원·안내 글로 짚어준다.</summary>
    private void ShowCenterIntro()
    {
        // 수원은 이미 튀어나와 있다. 트윈이 끝나기 직전일 수 있으니 멈추고 원래 크기로 확정한다.
        centerIntroTarget.DOKill();
        centerIntroTarget.localScale = BaseScaleOf(centerIntroTarget);

        if (tutorialSpotlight == null) return;
        RectTransform spotlightRect = (RectTransform)tutorialSpotlight.transform;

        Vector3 worldPos = centerIntroTarget.TransformPoint(centerIntroTarget.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(spotlightRect, screenPoint, null, out Vector2 localPoint)) return;

        centerIntroParent = centerIntroTarget.parent;
        centerIntroSiblingIndex = centerIntroTarget.GetSiblingIndex();
        centerIntroAnchoredPosition = centerIntroTarget.anchoredPosition;
        // 지금은 0으로 숨겨져 있으니 Start에서 기억해 둔 원래 크기를 쓴다.
        centerIntroScale = BaseScaleOf(centerIntroTarget);

        centerIntroTarget.SetParent(spotlightRect, false);
        centerIntroTarget.anchoredPosition = localPoint;
        centerIntroTarget.SetAsLastSibling();

        PlaceHighlight(localPoint, centerIntroMessage);
        pausedForCenterIntro = true;
    }

    private void EndCenterIntro()
    {
        pausedForCenterIntro = false;

        if (centerIntroTarget != null && centerIntroParent != null)
        {
            // 튀어나오던 도중에 클릭하면 트윈이 크기를 계속 덮어쓴다. 멈추고 원래 크기로 되돌린다.
            centerIntroTarget.DOKill();
            centerIntroTarget.SetParent(centerIntroParent, false);
            centerIntroTarget.SetSiblingIndex(centerIntroSiblingIndex);
            centerIntroTarget.anchoredPosition = centerIntroAnchoredPosition;
            centerIntroTarget.localScale = centerIntroScale;
        }

        HideElement(enemyHighlightCircle);
        HideElement(enemyCalloutText);

        // 스포트라이트와 멈춘 시간은 그대로 두고 다음 안내(남은 시간·점수)로 넘어간다.
        if (introExplainGroup != null) introExplainGroup.SetActive(true);
        AnimateExplainIn();
        LockStepInput();
    }

    // ---------------------------------------------------------------- 튜토리얼 연출
    // 전부 실제 시간으로 돈다. 튜토리얼 동안은 timeScale이 0이라 게임 시간으로 재면 영영 움직이지 않는다.
    // 진행 로직(멈춤·클릭 대기·다음 단계)은 건드리지 않고 보이는 모습만 부드럽게 바꾼다.

    private void ShowSpotlight()
    {
        if (tutorialSpotlight == null) return;
        if (spotlightGroup == null) spotlightGroup = GroupOf(tutorialSpotlight.transform);

        bool wasHidden = !tutorialSpotlight.activeSelf;
        tutorialSpotlight.SetActive(true);

        spotlightGroup.DOKill();
        spotlightGroup.blocksRaycasts = true;

        if (dimOverlay != null)
        {
            if (dimAlpha < 0f) dimAlpha = dimOverlay.color.a;

            // 걷히던 도중에 다시 켜지면 전체 투명도가 어중간하게 남아 있다. 전체는 바로 되돌리고 막만 다시 짙어진다.
            spotlightGroup.alpha = 1f;
            dimOverlay.DOKill();
            if (wasHidden) SetAlpha(dimOverlay, 0f);
            dimOverlay.DOFade(dimAlpha, spotlightFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        else
        {
            if (wasHidden) spotlightGroup.alpha = 0f;
            spotlightGroup.DOFade(1f, spotlightFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (clickHint != null)
        {
            CanvasGroup hint = GroupOf(clickHint);
            hint.DOKill();
            hint.alpha = 1f;
            hint.DOFade(0.45f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        LockStepInput();
    }

    /// <summary>어두운 막을 흐리게 걷고, 다 걷히면 끈다. 걷히는 동안엔 클릭을 막지 않아 곧바로 적을 누를 수 있다.</summary>
    private void HideSpotlight()
    {
        if (tutorialSpotlight == null || !tutorialSpotlight.activeSelf) return;
        if (spotlightGroup == null) spotlightGroup = GroupOf(tutorialSpotlight.transform);

        spotlightGroup.DOKill();
        if (dimOverlay != null) dimOverlay.DOKill();
        spotlightGroup.blocksRaycasts = false;
        spotlightGroup.DOFade(0f, spotlightFadeDuration).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() =>
        {
            tutorialSpotlight.SetActive(false);
            spotlightGroup.alpha = 1f;
            spotlightGroup.blocksRaycasts = true;
            ResetTutorialElements();
        });
    }

    /// <summary>막이 다 걷힌 뒤 원·글을 끄고 반복 트윈을 멈춘다. 다음에 켤 때 처음 모습에서 시작해야 한다.</summary>
    private void ResetTutorialElements()
    {
        foreach (RectTransform rt in new[] { enemyHighlightCircle, enemyCalloutText })
        {
            if (rt == null) continue;
            rt.DOKill();
            rt.gameObject.SetActive(false);
            RestoreLook(rt);
        }

        if (clickHint != null) GroupOf(clickHint).DOKill();

        if (introExplainGroup != null)
        {
            foreach (Transform child in introExplainGroup.transform)
            {
                var rt = child as RectTransform;
                if (rt == null) continue;
                rt.DOKill();
                RestoreLook(rt);
            }
        }
    }

    /// <summary>남은 시간·점수 안내의 원과 글을 차례로 띄운다. 원은 톡 튀어나오고, 글은 아래에서 올라온다.</summary>
    private void AnimateExplainIn()
    {
        if (introExplainGroup == null || !introExplainGroup.activeInHierarchy) return;

        int order = 0;
        foreach (Transform child in introExplainGroup.transform)
        {
            var rt = child as RectTransform;
            if (rt == null || !rt.gameObject.activeSelf) continue;

            float delay = explainStagger * order++;
            if (rt.GetComponent<TMP_Text>() != null) FadeRiseIn(rt, RestOf(rt), delay);
            else PopIn(rt, delay);
        }
    }

    private void PopIn(RectTransform rt, float delay, bool pulse = true)
    {
        Vector3 baseScale = BaseScaleOf(rt);
        rt.DOKill();
        rt.localScale = Vector3.zero;

        // 흐리게 사라지던 도중에 다시 불리면 반투명하게 남는다.
        var existing = rt.GetComponent<CanvasGroup>();
        if (existing != null) existing.alpha = 1f;

        DOTween.Sequence()
            .SetTarget(rt)
            .SetUpdate(true)
            .SetLink(rt.gameObject)
            .AppendInterval(delay)
            .Append(rt.DOScale(baseScale, highlightPopDuration).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                if (!pulse || highlightPulseScale <= 1f) return;
                rt.DOScale(baseScale * highlightPulseScale, highlightPulsePeriod * 0.5f)
                  .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(rt.gameObject);
            });
    }

    private void FadeRiseIn(RectTransform rt, Vector2 rest, float delay)
    {
        rt.DOKill();
        RestoreScale(rt);
        CanvasGroup group = GroupOf(rt);
        group.alpha = 0f;
        rt.anchoredPosition = rest - new Vector2(0f, calloutRise);

        DOTween.Sequence()
            .SetTarget(rt)
            .SetUpdate(true)
            .SetLink(rt.gameObject)
            .AppendInterval(delay)
            .Append(rt.DOAnchorPos(rest, calloutFadeDuration).SetEase(Ease.OutCubic))
            .Join(group.DOFade(1f, calloutFadeDuration * 0.8f).SetEase(Ease.OutQuad));
    }

    /// <summary>살짝 작아지며 흐려진 뒤 꺼진다. 다 꺼지면 처음 모습으로 돌려놓는다.</summary>
    private void HideElement(RectTransform rt)
    {
        if (rt == null || !rt.gameObject.activeSelf) return;

        Vector3 baseScale = BaseScaleOf(rt);
        rt.DOKill();
        CanvasGroup group = GroupOf(rt);

        DOTween.Sequence()
            .SetTarget(rt)
            .SetUpdate(true)
            .SetLink(rt.gameObject)
            .Append(group.DOFade(0f, elementHideDuration).SetEase(Ease.InQuad))
            .Join(rt.DOScale(baseScale * 0.85f, elementHideDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                rt.gameObject.SetActive(false);
                RestoreLook(rt);
            });
    }

    private void RestoreLook(RectTransform rt)
    {
        RestoreScale(rt);
        var group = rt.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 1f;
        if (restPositions.TryGetValue(rt, out Vector2 rest)) rt.anchoredPosition = rest;
    }

    private void RestoreScale(RectTransform rt)
    {
        if (baseScales.TryGetValue(rt, out Vector3 scale)) rt.localScale = scale;
    }

    private Vector3 BaseScaleOf(RectTransform rt)
    {
        if (!baseScales.TryGetValue(rt, out Vector3 scale))
        {
            scale = rt.localScale;
            baseScales[rt] = scale;
        }

        return scale;
    }

    private Vector2 RestOf(RectTransform rt)
    {
        if (!restPositions.TryGetValue(rt, out Vector2 rest))
        {
            rest = rt.anchoredPosition;
            restPositions[rt] = rest;
        }

        return rest;
    }

    private static void SetAlpha(UnityEngine.UI.Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private static CanvasGroup GroupOf(Component c)
    {
        var group = c.GetComponent<CanvasGroup>();
        return group != null ? group : c.gameObject.AddComponent<CanvasGroup>();
    }

    private void LockStepInput()
    {
        stepInputUnlockTime = Time.unscaledTime + stepInputDelay;
    }

    private void Update()
    {
        if (waitingForIntroClick)
        {
            // ESC 공용 메뉴가 열려 있을 때(또는 방금 닫힌 프레임)의 클릭은 메뉴 버튼을 누른 것이라 튜토리얼 진행으로 치지 않는다.
            // 안 막으면 메뉴 뒤에서 튜토리얼이 넘어가 버리고, 재개 시 메뉴가 timeScale을 0으로 되돌려 게임이 멈춘 채 남는다.
            if (IsSharedMenuOpen()) lastSharedMenuOpenFrame = Time.frameCount;
            bool blockedByMenu = Time.frameCount - lastSharedMenuOpenFrame <= 1;

            if (!introStepsReady || Time.unscaledTime < stepInputUnlockTime) return;

            bool clicked = !blockedByMenu && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!clicked && !blockedByMenu && Touchscreen.current != null) clicked = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (clicked && pausedForCenterIntro)
            {
                // 아직 튜토리얼이 끝난 게 아니므로 클릭 대기는 풀지 않는다. 다음 클릭은 다음 프레임부터 받는다.
                EndCenterIntro();
                return;
            }

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

                // 원·글은 막과 함께 흐려지다가 막이 다 걷히면 꺼진다(HideSpotlight → ResetTutorialElements).
                HideSpotlight();
                Time.timeScale = 1f;

                bool wasHighlightPause = pausedForObjectHighlight || pausedForComboGaugeHighlight;
                pausedForObjectHighlight = false;
                pausedForComboGaugeHighlight = false;

                if (!wasHighlightPause)
                {
                    StartWave();
                }
            }
            return;
        }

#if UNITY_EDITOR
        if (state == State.Counting && Keyboard.current != null && Keyboard.current[debugEndWaveKey].wasPressedThisFrame)
        {
            DebugEndWaveNow();
        }
#endif

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

#if UNITY_EDITOR
    /// <summary>
    /// 디버그: 지금 웨이브를 바로 끝낸다. 남은 시간을 0이 아니라 아주 조금만 남겨, 다음 프레임에 평소와 같은
    /// 길(웨이브 종료 → 다음 웨이브 또는 클리어)로 끝나게 한다 — 0으로 두면 카운트가 이미 끝난 것으로 보고 멈춘다.
    /// 끝나는 그 프레임에 보스·다슬이가 튀어나오지 않도록 등장도 이미 지나간 것으로 친다.
    /// </summary>
    private void DebugEndWaveNow()
    {
        if (remaining <= 0f) return;

        DestroyBossPositionMarker();
        DestroyPositionMarker();
        bossWarningStarted = true;
        bossSpawned = true;
        secondaryWarningStarted = true;
        secondaryObjectSpawned = true;
        bossAnnounceTimer = 0f;
        secondaryAnnounceTimer = 0f;

        remaining = 0.0001f;
        Debug.Log($"[YusongDebug] {debugEndWaveKey}: {GetWaveLabel(currentWave)} 즉시 종료", this);
    }
#endif

    public void PauseForEnemyHighlight(RectTransform enemyRect)
    {
        if (enemyRect == null) return;
        PauseForObjectsHighlight(new List<RectTransform> { enemyRect },
            "접근하는 적군을 클릭해서 제거하세요\n적군이 수원에 닿으면 체력이 줄어듭니다");
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

        ShowSpotlight();
        PlaceHighlight(localPoint, message);

        Time.timeScale = 0f;
        waitingForIntroClick = true;
    }

    // 원과 안내 글만 놓는다. 멈춤·클릭 대기는 부르는 쪽이 정한다 — 수원 안내는 이미 인트로 클릭을 기다리는 중에 불린다.
    private void PlaceHighlight(Vector2 localPoint, string message, float delay = 0f)
    {
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
            PopIn(enemyHighlightCircle, delay);

            if (enemyCalloutText != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var calloutTmp = enemyCalloutText.GetComponent<TextMeshProUGUI>();
                    if (calloutTmp != null) calloutTmp.text = message;

                    // Keep it horizontally centered on screen (the target can be near a side edge)
                    // and only follow the circle's height.
                    float gapBelowCircle = enemyHighlightCircle.sizeDelta.y * 0.5f + 20f;
                    enemyCalloutText.gameObject.SetActive(true);
                    // 원이 먼저 튀어나오고 글이 한 박자 뒤에 올라와야 시선이 원 → 글 순서로 간다.
                    FadeRiseIn(enemyCalloutText, new Vector2(0f, localPoint.y - gapBelowCircle), delay + highlightPopDuration * 0.4f);
                }
                else
                {
                    enemyCalloutText.gameObject.SetActive(false);
                }
            }
        }

        LockStepInput();
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
