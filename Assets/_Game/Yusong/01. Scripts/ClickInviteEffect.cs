using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class ClickInviteEffect : MonoBehaviour
{
    [SerializeField] private float scaleAmplitude = 0.12f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float glowAmount = 0.35f;
    [SerializeField] private UITheme theme;
    [SerializeField] private float labelOffsetY = 55f;
    [SerializeField] private string labelText = "Click!";

    private RectTransform rt;
    private Image image;
    private Vector3 baseScale;
    private Color baseColor;
    private GameObject labelObject;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        baseScale = rt.localScale;
        if (image != null) baseColor = image.color;

        CreateClickLabel();
    }

    private void CreateClickLabel()
    {
        labelObject = new GameObject("ClickLabel", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);

        var labelRt = labelObject.GetComponent<RectTransform>();
        labelRt.sizeDelta = new Vector2(84f, 28f);
        labelRt.anchoredPosition = new Vector2(0f, labelOffsetY);

        var bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(labelObject.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        bgImage.raycastTarget = false;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(labelObject.transform, false);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = labelText;
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.9f, 0.2f, 1f);
        text.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) text.font = theme.primaryFont;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        rt.localScale = baseScale * (1f + scaleAmplitude * t);

        if (image != null)
        {
            image.color = Color.Lerp(baseColor, Color.white, glowAmount * t);
        }
    }

    private void OnDisable()
    {
        if (rt != null) rt.localScale = baseScale;
        if (image != null) image.color = baseColor;
        if (labelObject != null) labelObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (labelObject != null) labelObject.SetActive(true);
    }
}
}
