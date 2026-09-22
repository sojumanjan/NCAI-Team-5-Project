using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI victoryHeadlineText;
    [SerializeField] private TextMeshProUGUI defeatHeadlineText;
    [SerializeField] private TextMeshProUGUI enemyKillScoreText;
    [SerializeField] private TextMeshProUGUI feverBonusScoreText;
    [SerializeField] private TextMeshProUGUI secondaryBonusScoreText;
    [SerializeField] private TextMeshProUGUI remainingHpScoreText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI retryButtonLabel;
    [SerializeField] private TextMeshProUGUI mainMenuButtonLabel;
    [SerializeField] private float revealInterval = 0.5f;
    [SerializeField] private UITheme theme;

    private Coroutine revealRoutine;

    private void Awake()
    {
        if (theme == null) return;

        var bg = GetComponent<Image>();
        if (bg != null) bg.color = theme.gameOverBackgroundColor;

        // 색은 여기서 고정하지 않는다 — CLEAR/GAME OVER 중 뭐가 먼저 뜨느냐에 따라
        // Show()가 정한 색을 Awake()가 덮어써버리는 순서 문제가 있었다. 폰트만 여기서 맞춘다.
        ApplyFont(gameOverText);

        ApplyFont(victoryHeadlineText);
        ApplyFont(defeatHeadlineText);
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

        if (gameOverText != null)
        {
            gameOverText.text = headline;
            if (theme != null)
            {
                gameOverText.color = isVictory ? theme.clearTextColor : theme.gameOverTextColor;
            }
        }

        if (victoryHeadlineText != null)
        {
            victoryHeadlineText.gameObject.SetActive(isVictory);
            if (isVictory)
            {
                victoryHeadlineText.text = "정화 성공!";
                if (theme != null) victoryHeadlineText.color = theme.victoryTextColor;
            }
        }

        if (defeatHeadlineText != null)
        {
            defeatHeadlineText.gameObject.SetActive(!isVictory);
            if (!isVictory)
            {
                defeatHeadlineText.text = "정화 실패..";
                if (theme != null) defeatHeadlineText.color = theme.defeatHeadlineColor;
            }
        }

        gameObject.SetActive(true);

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealScoreBreakdown());
    }

    private IEnumerator RevealScoreBreakdown()
    {
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
        GameFlow.Instance?.ReturnToMain();
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
