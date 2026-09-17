using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public class BossIndicator : MonoBehaviour
{
    [SerializeField] private Transform fieldParent;
    [SerializeField] private float distanceFromCenter = 70f;
    [SerializeField] private float pulseScaleMin = 0.9f;
    [SerializeField] private float pulseScaleMax = 1.35f;
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private UITheme theme;
    [SerializeField] private Color indicatorColor = new Color(1f, 0.55f, 0f, 1f);

    private RectTransform rt;
    private Image image;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        if (theme != null)
        {
            indicatorColor = theme.bossIndicatorColor;
            if (theme.triangleSprite != null) image.sprite = theme.triangleSprite;
        }

        image.color = indicatorColor;
        SetVisible(false);
    }

    private void Update()
    {
        Transform boss = FindBoss();

        if (boss == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        Vector2 bossPos = boss.GetComponent<RectTransform>().anchoredPosition;
        Vector2 dir = bossPos.sqrMagnitude > 0.0001f ? bossPos.normalized : Vector2.up;

        rt.anchoredPosition = dir * distanceFromCenter;
        rt.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, dir));

        float pulse = Mathf.Lerp(pulseScaleMin, pulseScaleMax, (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
        rt.localScale = Vector3.one * pulse;
    }

    private Transform FindBoss()
    {
        if (fieldParent == null) return null;

        for (int i = 0; i < fieldParent.childCount; i++)
        {
            var child = fieldParent.GetChild(i);
            if (child == transform) continue;
            if (child.GetComponent<EnemyMover>() == null) continue;
            if (child.name.Contains("Boss")) return child;
        }

        return null;
    }

    private void SetVisible(bool visible)
    {
        if (image != null) image.enabled = visible;
    }
}
}
