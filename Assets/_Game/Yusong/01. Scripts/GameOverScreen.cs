using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private UITheme theme;

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
    }
}
