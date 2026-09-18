using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }
    public static bool IsAoeActive => Instance != null && Instance.aoeActive;
    public static float AoeRadius => Instance != null ? Instance.aoeRadius : 0f;

    [SerializeField] private GameObject root;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float graceDuration = 3f;
    [SerializeField] private RectTransform comboPopupPrefab;

    [Header("AOE Power-up")]
    [SerializeField] private int aoeComboThreshold = 20;
    [SerializeField] private float aoeDuration = 10f;
    [SerializeField] private float aoeRadius = 180f;
    [SerializeField] private Color aoeGaugeColor = new Color(1f, 0.1f, 0.6f, 1f);
    [SerializeField] private GameObject fireOverlay;
    [SerializeField] private FeverAnnouncement feverAnnouncement;
    [SerializeField] private UITheme theme;

    [Header("Big Stack Emphasis (e.g. boss kill)")]
    [SerializeField] private float comboEmphasisScale = 1.8f;
    [SerializeField] private Color comboEmphasisColor = new Color(1f, 0.55f, 0f, 1f);
    [SerializeField] private float gaugePunchScale = 1.3f;
    [SerializeField] private float gaugePunchDuration = 0.25f;

    private int comboCount;
    private float timer;
    private bool active;

    private bool aoeActive;
    private float aoeTimer;
    private Color normalGaugeColor;
    private Coroutine gaugePunchRoutine;

    private void Awake()
    {
        Instance = this;

        if (theme != null)
        {
            gaugeFill.color = theme.comboGaugeNormalColor;
            aoeGaugeColor = theme.comboGaugeFeverColor;

            if (theme.primaryFont != null && comboText != null)
            {
                comboText.font = theme.primaryFont;
            }
        }

        normalGaugeColor = gaugeFill.color;
        SetVisible(false);
    }

    private void Update()
    {
        if (aoeActive)
        {
            if (!CountdownTimer.IsWaveActive) return;

            aoeTimer -= Time.deltaTime;

            if (aoeTimer <= 0f)
            {
                DeactivateAoe();
                return;
            }

            gaugeFill.fillAmount = aoeTimer / aoeDuration;
            return;
        }

        if (!active) return;
        if (!CountdownTimer.IsWaveActive) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            gaugeFill.fillAmount = 0f;
            ResetCombo();
            return;
        }

        gaugeFill.fillAmount = timer / graceDuration;
    }

    public int RegisterKill(Vector2 popupPosition, Transform popupParent, int stacks = 1)
    {
        comboCount += stacks;
        comboText.text = comboCount + " Combo!";
        SetVisible(true);

        SpawnPopup(popupPosition, popupParent, stacks);

        if (stacks > 1)
        {
            PlayGaugePunch();
        }

        if (!aoeActive)
        {
            timer = graceDuration;
            active = true;
            gaugeFill.fillAmount = 1f;

            if (comboCount >= aoeComboThreshold)
            {
                ActivateAoe();
            }
        }

        return comboCount;
    }

    public void ForceActivateAoe()
    {
        if (aoeActive) return;
        ActivateAoe();
    }

    private void ActivateAoe()
    {
        aoeActive = true;
        aoeTimer = aoeDuration;
        active = false;

        gaugeFill.color = aoeGaugeColor;
        gaugeFill.fillAmount = 1f;
        SetVisible(true);

        if (fireOverlay != null) fireOverlay.SetActive(true);
        if (feverAnnouncement != null) feverAnnouncement.Show();
    }

    private void DeactivateAoe()
    {
        aoeActive = false;
        gaugeFill.color = normalGaugeColor;

        if (fireOverlay != null) fireOverlay.SetActive(false);

        ResetCombo();
    }

    public void ResetState()
    {
        aoeActive = false;
        aoeTimer = 0f;
        gaugeFill.color = normalGaugeColor;

        if (fireOverlay != null) fireOverlay.SetActive(false);
        if (feverAnnouncement != null) feverAnnouncement.HideImmediate();

        ResetCombo();
    }

    private void SpawnPopup(Vector2 position, Transform parent, int stacks)
    {
        if (comboPopupPrefab == null || parent == null) return;

        var popup = Instantiate(comboPopupPrefab, parent);
        popup.anchoredPosition = position;

        var comboPopup = popup.GetComponent<ComboPopup>();
        if (stacks > 1)
        {
            comboPopup.SetTextAndColor("+" + stacks + " COMBO!", comboEmphasisColor, comboEmphasisScale);
        }
        else
        {
            comboPopup.SetText("+" + stacks);
        }
    }

    private void PlayGaugePunch()
    {
        if (root == null) return;

        if (gaugePunchRoutine != null) StopCoroutine(gaugePunchRoutine);
        gaugePunchRoutine = StartCoroutine(GaugePunchRoutine());
    }

    private IEnumerator GaugePunchRoutine()
    {
        var rootRt = root.GetComponent<RectTransform>();
        if (rootRt == null) yield break;

        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = baseScale * gaugePunchScale;
        float half = gaugePunchDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            rootRt.localScale = Vector3.LerpUnclamped(baseScale, targetScale, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            rootRt.localScale = Vector3.LerpUnclamped(targetScale, baseScale, t / half);
            yield return null;
        }

        rootRt.localScale = baseScale;
        gaugePunchRoutine = null;
    }

    private void ResetCombo()
    {
        comboCount = 0;
        active = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        root.SetActive(visible);
    }
}
}
