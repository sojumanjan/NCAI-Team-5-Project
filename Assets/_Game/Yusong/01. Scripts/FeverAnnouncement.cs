using System.Collections;
using UnityEngine;
using TMPro;

public class FeverAnnouncement : MonoBehaviour
{
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchInDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private UITheme theme;

    private RectTransform rt;
    private TextMeshProUGUI text;
    private Color baseColor;
    private Coroutine routine;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        text = GetComponent<TextMeshProUGUI>();

        if (theme != null)
        {
            text.color = theme.feverAnnouncementColor;
            if (theme.primaryFont != null) text.font = theme.primaryFont;
        }

        baseColor = text.color;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        Color c = baseColor;
        c.a = 1f;
        text.color = c;

        float t = 0f;
        while (t < punchInDuration)
        {
            t += Time.deltaTime;
            float p = t / punchInDuration;
            float scale = Mathf.Lerp(0.2f, punchScale, EaseOutBack(p));
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        rt.localScale = Vector3.one * punchScale;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float p = t / fadeOutDuration;
            c.a = Mathf.Lerp(1f, 0f, p);
            text.color = c;
            rt.localScale = Vector3.one * Mathf.Lerp(punchScale, punchScale * 1.15f, p);
            yield return null;
        }

        gameObject.SetActive(false);
        routine = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
