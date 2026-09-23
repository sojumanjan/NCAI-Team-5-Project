using System.Collections;
using TMPro;
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

    [Header("Sound")]
    [SerializeField] private SoundData hitSound;

    [Header("Heal Effect")]
    [SerializeField] private RectTransform healPopupPrefab;
    [SerializeField] private Color healColor = new Color(0.45f, 1f, 0.55f, 1f);
    [SerializeField] private float healPopupOffsetY = 70f;
    [SerializeField] private float healPopupScale = 1.3f;
    [SerializeField] private float healPipPunchScale = 1.8f;
    [SerializeField] private float healPipPunchDuration = 0.4f;
    // 공용 폰트는 아틀라스 여백이 0이라 TMP 테두리를 그릴 수 없다 — 여백이 있는 폰트로 바꿔서 테두리를 넣는다.
    [SerializeField] private TMP_FontAsset healPopupFont;
    [SerializeField] private Color healOutlineColor = new Color(0.02f, 0.22f, 0.08f, 1f);
    [SerializeField, Range(0f, 1f)] private float healOutlineWidth = 0.45f;
    [SerializeField, Range(0f, 1f)] private float healOutlineDilate = 0.35f;

    // 팝업마다 머티리얼을 새로 만들면 TMP가 지워주지 않아 쌓이므로, 하나만 만들어 모든 회복 팝업이 공유한다.
    private static Material healOutlineMaterial;

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
            healColor = theme.playerHealColor;
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
        AudioManager.Play(hitSound);

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

    // 체력이 가득이면 회복할 게 없으므로 이펙트도 띄우지 않는다 — 안 오른 체력에 "+1"이 뜨면 혼란스럽다.
    public bool Heal(int amount)
    {
        if (IsGameOver || amount <= 0 || currentHealth >= maxHealth) return false;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHpText();

        SpawnHealPopup(currentHealth - before);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashColor(healColor));

        if (hpPips != null)
        {
            for (int i = before; i < currentHealth && i < hpPips.Length; i++)
            {
                if (hpPips[i] != null) StartCoroutine(PunchPip(hpPips[i].rectTransform));
            }
        }

        return true;
    }

    private void SpawnHealPopup(int healed)
    {
        if (healPopupPrefab == null || transform.parent == null) return;

        var popup = Instantiate(healPopupPrefab, transform.parent);
        popup.anchoredPosition = ((RectTransform)transform).anchoredPosition + Vector2.up * healPopupOffsetY;

        var comboPopup = popup.GetComponent<ComboPopup>();
        if (comboPopup != null) comboPopup.SetTextAndColor("+" + healed + " HP", healColor, healPopupScale);

        var tmp = popup.GetComponent<TextMeshProUGUI>();
        if (tmp != null) ApplyHealOutline(tmp);
    }

    private void ApplyHealOutline(TextMeshProUGUI tmp)
    {
        if (healPopupFont != null) tmp.font = healPopupFont;
        // 팝업 프리팹은 볼드인데, 볼드 굵기까지 겹치면 폰트 여백을 넘어가 글자 사각형이 비친다 — 굵기는 dilate로만 준다.
        tmp.fontStyle &= ~FontStyles.Bold;

        Material source = tmp.font.material;
        // 동적 폰트는 아틀라스가 바뀔 수 있어서, 원본과 텍스처·셰이더가 달라지면 다시 만든다.
        if (healOutlineMaterial == null || healOutlineMaterial.mainTexture != source.mainTexture || healOutlineMaterial.shader != source.shader)
        {
            healOutlineMaterial = new Material(source);
            healOutlineMaterial.name = source.name + " (Heal Outline)";
            // 모바일용 SDF 셰이더는 이 키워드를 켜야 테두리를 그린다.
            healOutlineMaterial.EnableKeyword("OUTLINE_ON");
        }

        healOutlineMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, healOutlineDilate);
        healOutlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, healOutlineWidth);
        healOutlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, healOutlineColor);
        // 값을 직접 넣으면 TMP가 두께 배율을 다시 계산하지 않아 글자 사각형 여백이 모자라고, 글자 뒤에 네모가 비친다.
        ShaderUtilities.UpdateShaderRatios(healOutlineMaterial);

        tmp.fontSharedMaterial = healOutlineMaterial;
        tmp.UpdateMeshPadding();
    }

    private IEnumerator PunchPip(RectTransform pip)
    {
        Vector3 baseScale = Vector3.one;
        float half = healPipPunchDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            pip.localScale = Vector3.LerpUnclamped(baseScale, baseScale * healPipPunchScale, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            pip.localScale = Vector3.LerpUnclamped(baseScale * healPipPunchScale, baseScale, t / half);
            yield return null;
        }

        pip.localScale = baseScale;
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

        // 캔버스 레이캐스터 자체를 끄면 결과 화면의 다시하기/메인메뉴 버튼도 같이 막혀버린다.
        // GameOverScreen 배경 이미지가 raycastTarget=true라 뒤쪽 게임 요소는 어차피 가려진다.
        GameFlow.Instance?.ReportCurrent(new MiniGameResult(false, 0f));

        Time.timeScale = 0f;
    }

    private IEnumerator FlashHit()
    {
        return FlashColor(hitColor);
    }

    private IEnumerator FlashColor(Color flashColor)
    {
        for (int i = 0; i < flashBlinks; i++)
        {
            image.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            image.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
    }
}
}
