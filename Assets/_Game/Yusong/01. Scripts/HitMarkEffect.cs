using UnityEngine;
using UnityEngine.UI;

public class HitMarkEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float startScale = 0.4f;
    [SerializeField] private float endScale = 1.1f;

    private RectTransform rt;
    private Image image;
    private Color startColor;
    private float age;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        startColor = image.color;
        rt.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);

        float scale = Mathf.Lerp(startScale, endScale, t);
        rt.localScale = Vector3.one * scale;

        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        image.color = c;

        if (age >= duration)
        {
            Destroy(gameObject);
        }
    }
}
