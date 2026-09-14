using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float graceDuration = 3f;

    private int comboCount;
    private float timer;
    private bool active;

    private void Awake()
    {
        Instance = this;
        SetVisible(false);
    }

    private void Update()
    {
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

    public void RegisterKill()
    {
        comboCount++;
        timer = graceDuration;
        active = true;

        SetVisible(true);
        gaugeFill.fillAmount = 1f;
        comboText.text = comboCount + "!";
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
