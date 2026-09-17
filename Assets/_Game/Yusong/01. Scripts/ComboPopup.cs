using UnityEngine;
using TMPro;

namespace Yusong
{
public class ComboPopup : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float riseDistance = 60f;
    [SerializeField] private UITheme theme;

    private RectTransform rt;
    private TextMeshProUGUI text;
    private Vector2 startPos;
    private Color startColor;
    private float age;

    private bool initialized;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        text = GetComponent<TextMeshProUGUI>();

        if (theme != null && theme.primaryFont != null)
        {
            text.font = theme.primaryFont;
        }

        startColor = text.color;
    }

    public void SetText(string value)
    {
        text.text = value;
        startPos = rt.anchoredPosition;
        initialized = true;
    }

    public void SetTextAndColor(string value, Color color)
    {
        SetTextAndColor(value, color, 1f);
    }

    public void SetTextAndColor(string value, Color color, float scale)
    {
        text.text = value;
        text.color = color;
        startColor = color;
        startPos = rt.anchoredPosition;
        rt.localScale = Vector3.one * scale;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);

        rt.anchoredPosition = startPos + Vector2.up * (riseDistance * t);

        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        text.color = c;

        if (age >= duration)
        {
            Destroy(gameObject);
        }
    }
}
}
