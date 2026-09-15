using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [SerializeField] private int maxHealth = 5;
    [SerializeField] private Color hitColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private int flashBlinks = 2;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private UITheme theme;
    [SerializeField] private Image[] hpPips;

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
            if (theme.circleSprite != null) image.sprite = theme.circleSprite;
        }

        originalColor = image.color;
        UpdateHpText();
    }

    public void TakeDamage(int amount)
    {
        if (IsGameOver) return;

        currentHealth -= amount;
        UpdateHpText();

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
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

    private void TriggerGameOver()
    {
        IsGameOver = true;

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }

        var raycaster = GetComponentInParent<Canvas>()?.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

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
