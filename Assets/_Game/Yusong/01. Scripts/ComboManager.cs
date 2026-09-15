using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private int comboCount;
    private float timer;
    private bool active;

    private bool aoeActive;
    private float aoeTimer;
    private Color normalGaugeColor;

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

    public int RegisterKill(Vector2 popupPosition, Transform popupParent)
    {
        comboCount++;
        comboText.text = comboCount + "!";
        SetVisible(true);

        SpawnPopup(popupPosition, popupParent);

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

    private void SpawnPopup(Vector2 position, Transform parent)
    {
        if (comboPopupPrefab == null || parent == null) return;

        var popup = Instantiate(comboPopupPrefab, parent);
        popup.anchoredPosition = position;
        popup.GetComponent<ComboPopup>().SetText(comboCount + "!");
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
