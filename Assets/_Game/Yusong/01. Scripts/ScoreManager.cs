using UnityEngine;
using TMPro;

namespace Yusong
{
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public int TotalScore => totalScore;
    public int EnemyKillScore => enemyKillScore;
    public int FeverBonusScore => feverBonusScore;
    public int SecondaryBonusScore => secondaryBonusScore;
    public int RemainingHpScore => remainingHpScore;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform scorePopupPrefab;
    [SerializeField] private Color gainColor = new Color(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private Color lossColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float emphasisScale = 1.6f;
    [SerializeField] private UITheme theme;

    private Color emphasisColor;
    private int totalScore;
    private int enemyKillScore;
    private int feverBonusScore;
    private int secondaryBonusScore;
    private int remainingHpScore;

    private void Awake()
    {
        Instance = this;

        emphasisColor = gainColor;

        if (theme != null)
        {
            gainColor = theme.scoreGainColor;
            lossColor = theme.scoreLossColor;
            emphasisColor = theme.secondarySuccessColor;

            if (theme.primaryFont != null && scoreText != null)
            {
                scoreText.font = theme.primaryFont;
            }
        }

        UpdateText();
    }

    public void AddEnemyKillScore(int baseAmount, int feverBonusAmount, Vector2 popupPosition, Transform popupParent)
    {
        int total = baseAmount + feverBonusAmount;
        totalScore += total;
        enemyKillScore += baseAmount;
        feverBonusScore += feverBonusAmount;
        UpdateText();
        SpawnPopup(total, popupPosition, popupParent, false);
    }

    public void AddSecondaryScore(int amount, Vector2 popupPosition, Transform popupParent, bool emphasize)
    {
        totalScore += amount;
        secondaryBonusScore += amount;
        UpdateText();
        SpawnPopup(amount, popupPosition, popupParent, emphasize);
    }

    public void AddRemainingHpScore(int amount)
    {
        totalScore += amount;
        remainingHpScore += amount;
        UpdateText();
    }

    private void SpawnPopup(int amount, Vector2 position, Transform parent, bool emphasize)
    {
        if (scorePopupPrefab == null || parent == null || amount == 0) return;

        var popup = Instantiate(scorePopupPrefab, parent);
        popup.anchoredPosition = position;

        string text = (amount > 0 ? "+" : "") + amount;
        Color color = amount > 0 ? gainColor : lossColor;
        float scale = 1f;

        if (emphasize)
        {
            color = amount > 0 ? emphasisColor : lossColor;
            scale = emphasisScale;
        }

        var comboPopup = popup.GetComponent<ComboPopup>();
        comboPopup.SetTextAndColor(text, color, scale);
    }

    private void UpdateText()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE : " + totalScore;
        }
    }
}
}
