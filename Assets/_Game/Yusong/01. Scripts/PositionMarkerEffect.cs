using UnityEngine;
using UnityEngine.UI;

public class PositionMarkerEffect : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.55f;

    private Image image;
    private Color baseColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        baseColor = image.color;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        Color c = baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        image.color = c;
    }
}
