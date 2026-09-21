using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    public static bool IsGameOver { get; private set; }
    public static event System.Action Damaged;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [SerializeField] private int maxHealth = 5;
    [SerializeField] private Color hitColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private int flashBlinks = 2;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private UITheme theme;
    [SerializeField] private Image[] hpPips;
    [SerializeField] private int remainingHpScoreMultiplier = 1000;

    private Image image;
    private Color originalColor;
    private int currentHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Instance = this;
        IsGameOver = false;
        image = GetComponent<Image>();
        currentHealth = maxHealth;

        if (theme != null)
        {
            hitColor = theme.playerHitColor;
        }
        // Sprite is intentionally left as whatever is set on the Image in the prefab —
        // this object has its own distinct "water source" design, unlike the generic
        // round enemies that share theme.circleSprite.

        originalColor = image.color;
        UpdateHpText();
    }

    public void TakeDamage(int amount)
    {
        if (IsGameOver) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHpText();

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());

        Damaged?.Invoke();

        // Dying during the Wave 0 tutorial shouldn't end the run — health just clamps at 0
        // and gets reset once the real game starts at Wave 1.
        if (currentHealth <= 0 && !CountdownTimer.IsTutorialWave)
        {
            TriggerGameOver();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHpText();
    }

    private void UpdateHpText()
    {
        int remaining = Mathf.Max(currentHealth, 0);

        if (hpPips != null)
        {
            Color onColor = theme != null ? theme.centerHpGaugeColor : new Color(0.9f, 0.25f, 0.25f, 1f);
            Color offColor = theme != null ? theme.hpPipOffColor : new Color(1f, 1f, 1f, 0.25f);

            for (int i = 0; i < hpPips.Length; i++)
            {
                if (hpPips[i] == null) continue;
                hpPips[i].color = i < remaining ? onColor : offColor;
            }
        }
    }

    public int CalculateRemainingHpScore()
    {
        return currentHealth * remainingHpScoreMultiplier;
    }

    private void TriggerGameOver()
    {
        IsGameOver = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddRemainingHpScore(CalculateRemainingHpScore());
        }

        if (gameOverScreen != null)
        {
            var screen = gameOverScreen.GetComponent<GameOverScreen>();
            if (screen != null) screen.Show("GAME OVER");
            else gameOverScreen.SetActive(true);
        }

        var raycaster = GetComponentInParent<Canvas>()?.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        GameFlow.Instance?.ReportCurrent(new MiniGameResult(false, 0f));

        Time.timeScale = 0f;
    }

    private IEnumerator FlashHit()
    {
        for (int i = 0; i < flashBlinks; i++)
        {
            image.color = hitColor;
            yield return new WaitForSeconds(flashInterval);
            image.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
    }
}
}
