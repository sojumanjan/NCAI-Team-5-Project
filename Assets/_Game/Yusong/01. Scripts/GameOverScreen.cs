using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI enemyKillScoreText;
    [SerializeField] private TextMeshProUGUI feverBonusScoreText;
    [SerializeField] private TextMeshProUGUI secondaryBonusScoreText;
    [SerializeField] private TextMeshProUGUI remainingHpScoreText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private float revealInterval = 0.5f;
    [SerializeField] private UITheme theme;

    private Coroutine revealRoutine;

    private void Awake()
    {
        if (theme == null) return;

        var bg = GetComponent<Image>();
        if (bg != null) bg.color = theme.gameOverBackgroundColor;

        if (gameOverText != null)
        {
            gameOverText.color = theme.gameOverTextColor;
            if (theme.primaryFont != null) gameOverText.font = theme.primaryFont;
        }

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
        if (gameOverText != null)
        {
            gameOverText.text = headline;
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
        RevealLine(secondaryBonusScoreText, "이름미정 보호 보너스 점수 : " + secondaryBonus);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(remainingHpScoreText, "잔여 체력 점수 : " + remainingHp);

        yield return new WaitForSecondsRealtime(revealInterval);
        RevealLine(totalScoreText, "최종 스코어 : " + total);

        revealRoutine = null;
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
