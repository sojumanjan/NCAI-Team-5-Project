using UnityEngine;
using UnityEngine.UI;

public class FireOverlayEffect : MonoBehaviour
{
    [SerializeField] private float baseAlpha = 0.55f;
    [SerializeField] private float flickerAmount = 0.2f;
    [SerializeField] private float flickerSpeed = 6f;

    private Image image;
    private Color baseColor;
    private float noiseOffset;

    private void Awake()
    {
        image = GetComponent<Image>();
        baseColor = image.color;
        noiseOffset = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
        float alpha = baseAlpha + (noise - 0.5f) * 2f * flickerAmount;

        Color c = baseColor;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }
}
