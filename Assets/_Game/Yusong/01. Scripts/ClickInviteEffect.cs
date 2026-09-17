using UnityEngine;
using UnityEngine.UI;

public class ClickInviteEffect : MonoBehaviour
{
    [SerializeField] private float scaleAmplitude = 0.12f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float glowAmount = 0.35f;

    private RectTransform rt;
    private Image image;
    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        baseScale = rt.localScale;
        if (image != null) baseColor = image.color;
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
    }
}
