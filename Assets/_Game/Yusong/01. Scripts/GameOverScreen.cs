using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class GameOverScreen : MonoBehaviour
{
    [Header("제목 — 켜고 끄기만 합니다. 글자·색·폰트는 각 오브젝트에서 직접 바꾸세요.")]
    [Tooltip("클리어했을 때 켤 큰 제목 (CLEAR!).")]
    [SerializeField] private GameObject clearTitle;
    [Tooltip("게임 오버일 때 켤 큰 제목 (GAME OVER).")]
    [SerializeField] private GameObject gameOverTitle;
    [SerializeField] private TextMeshProUGUI victoryHeadlineText;
    [SerializeField] private TextMeshProUGUI defeatHeadlineText;

    [Tooltip("클리어했을 때만 헤드라인 다음에 뜨는 보상 표시.")]
    [SerializeField] private GameObject reward;

    [Header("등장 연출")]
    [Tooltip("제목 → 헤드라인 → 보상이 하나씩 뜨는 간격 (초).")]
    [SerializeField] private float introStepInterval = 0.4f;
    [Tooltip("하나가 톡 튀어나오는 시간 (초).")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private TextMeshProUGUI enemyKillScoreText;
    [SerializeField] private TextMeshProUGUI feverBonusScoreText;
    [SerializeField] private TextMeshProUGUI secondaryBonusScoreText;
    [SerializeField] private TextMeshProUGUI remainingHpScoreText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI retryButtonLabel;
    [SerializeField] private TextMeshProUGUI mainMenuButtonLabel;
    [SerializeField] private float revealInterval = 0.5f;
    [SerializeField] private UITheme theme;

    [Header("결과 화면이 뜨면 숨길 인게임 UI")]
    [Tooltip("남은 시간, 점수, 콤보 게이지, 피버 문구·불꽃 등. 결과 화면보다 위에 그려져 가려버리는 것들입니다. " +
             "끄지 않고 투명하게 만듭니다 — 남은 시간 글자에는 웨이브 진행 스크립트가 붙어 있어 끄면 같이 멈춥니다.")]
    [SerializeField] private GameObject[] hideOnShow;

    [Header("메인 허브로 돌아가기")]
    [Tooltip("버튼을 누른 뒤 화면이 검게 덮이는 시간 (초). 배경음도 같은 시간 동안 사그라듭니다.")]
    [SerializeField] private float hubFadeOutSeconds = 1.5f;

    private Coroutine revealRoutine;
    private bool leaving;

    private void Awake()
    {
        if (theme == null) return;

        var bg = GetComponent<Image>();
        if (bg != null) bg.color = theme.gameOverBackgroundColor;

        // 제목·헤드라인은 인스펙터에서 직접 꾸미도록 폰트도 건드리지 않는다. 점수 줄과 버튼 글자만 테마 폰트로 맞춘다.
        ApplyFont(retryButtonLabel);
        ApplyFont(mainMenuButtonLabel);
        ApplyFont(enemyKillScoreText);
        ApplyFont(feverBonusScoreText);
        ApplyFont(secondaryBonusScoreText);
        ApplyFont(remainingHpScoreText);
        ApplyFont(totalScoreText);
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (text != null && theme.primaryFont != null)
        {
            text.font = theme.primaryFont;
        }
    }

    public void Show(string headline)
    {
        bool isVictory = headline == "CLEAR!";

        // 전부 꺼 둔 채 시작해서 제목 → 헤드라인 → 보상 순서로 하나씩 켠다.
        SetShown(clearTitle, false);
        SetShown(gameOverTitle, false);
        SetShown(victoryHeadlineText, false);
        SetShown(defeatHeadlineText, false);
        SetShown(reward, false);

        gameObject.SetActive(true);
        HideInGameUI();

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealScoreBreakdown(isVictory));
    }

    private static void SetShown(Component target, bool shown)
    {
        if (target != null) target.gameObject.SetActive(shown);
    }

    private static void SetShown(GameObject target, bool shown)
    {
        if (target != null) target.SetActive(shown);
    }

    /// <summary>켜면서 톡 튀어나오게 한다. 결과 화면은 시간이 멈춰 있어 실제 시간으로 돌린다.</summary>
    private void PopIn(GameObject target)
    {
        if (target == null) return;

        Transform t = target.transform;
        Vector3 baseScale = t.localScale == Vector3.zero ? Vector3.one : t.localScale;
        t.DOKill();

        target.SetActive(true);
        t.localScale = Vector3.zero;
        t.DOScale(baseScale, popDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(target);
    }

    private IEnumerator RevealScoreBreakdown(bool isVictory)
    {
        PopIn(isVictory ? clearTitle : gameOverTitle);

        yield return new WaitForSecondsRealtime(introStepInterval);
        PopIn(isVictory ? (victoryHeadlineText != null ? victoryHeadlineText.gameObject : null)
                        : (defeatHeadlineText != null ? defeatHeadlineText.gameObject : null));

        if (isVictory && reward != null)
        {
            yield return new WaitForSecondsRealtime(introStepInterval);
            PopIn(reward);
        }

        int enemyKill = 0;
        int feverBonus = 0;
        int secondaryBonus = 0;
        int remainingHp = 0;
        int total = 0;

        if (ScoreManager.Instance != null)
        {
            enemyKill = ScoreManager.Instance.EnemyKillScore;
            feverBonus = ScoreManager.Instance.FeverBonusScore;
            secondaryBonus = ScoreManager.Instance.SecondaryBonusScore;
            remainingHp = ScoreManager.Instance.RemainingHpScore;
            total = ScoreManager.Instance.TotalScore;
        }

        HideAndClear(enemyKillScoreText);
        HideAndClear(feverBonusScoreText);
        HideAndClear(secondaryBonusScoreText);
        HideAndClear(remainingHpScoreText);
        HideAndClear(totalScoreText);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(enemyKillScoreText, "적 처치로 얻은 점수 : " + enemyKill);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(feverBonusScoreText, "피버타임 보너스 점수 : " + feverBonus);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(secondaryBonusScoreText, "다슬이 보호 보너스 점수 : " + secondaryBonus);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(remainingHpScoreText, "잔여 체력 점수 : " + remainingHp);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(totalScoreText, "최종 스코어 : " + total);

        revealRoutine = null;
    }

    public void OnRetryClicked()
    {
        CountdownTimer.SkipTutorial = true;

        GameFlow flow = GameFlow.Instance;
        if (flow != null)
        {
            flow.LoadMiniGame(flow.CurrentDefinition);
        }
    }

    public void OnMainMenuClicked()
    {
        GameFlow flow = GameFlow.Instance;
        if (flow == null || leaving) return;

        leaving = true;
        StartCoroutine(FadeOutThenReturn(flow));
    }

    // 다시하기는 씬을 새로 열어 복구되므로 되돌릴 필요가 없다.
    private void HideInGameUI()
    {
        if (hideOnShow == null) return;

        foreach (GameObject target in hideOnShow)
        {
            if (target == null) continue;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    /// <summary>허브는 검은 화면에서 시작해 걷어내므로, 이쪽이 검게 덮은 채 넘겨야 화면이 끊기지 않는다.</summary>
    private IEnumerator FadeOutThenReturn(GameFlow flow)
    {
        // 결과 화면 위(옵션 창 포함)까지 전부 덮어야 한다. 허브·쿠킹의 암전과 같은 순서(1000)를 쓴다.
        GameObject coverRoot = ScreenInputBlocker.Create(null, "ReturnToHubCover");
        coverRoot.GetComponent<Canvas>().sortingOrder = 1000;
        coverRoot.GetComponentInChildren<Image>(true).color = Color.black;

        CanvasGroup group = coverRoot.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        coverRoot.SetActive(true);

        AudioManager.StopBGM(hubFadeOutSeconds);

        float elapsed = 0f;
        float total = Mathf.Max(0.01f, hubFadeOutSeconds);

        while (elapsed < total)
        {
            // 결과 화면은 timeScale 0으로 멈춰 있다. 게임 시간으로 재면 영원히 덮이지 않는다.
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / total);
            yield return null;
        }

        group.alpha = 1f;
        flow.ReturnToMain();
    }

    private void HideAndClear(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.text = "";
        text.gameObject.SetActive(false);
    }

    private void RevealLine(TextMeshProUGUI text, string value)
    {
        if (text == null) return;
        text.text = value;
        text.gameObject.SetActive(true);
    }
}
}
