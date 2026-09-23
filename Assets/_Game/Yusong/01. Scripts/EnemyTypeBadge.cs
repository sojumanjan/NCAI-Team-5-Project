using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public enum EnemyBadgeType
{
    Flame,
    Star
}

// 3HP/2HP처럼 겉모습을 공유하는 적도 계열(폭발/특수)을 한눈에 구분하게 해주는 표시.
// 아트 아이콘이 나오기 전까지는 도형을 런타임에 직접 그려서 쓰고, PNG가 생기면 overrideSprite만 채우면 된다.
[RequireComponent(typeof(RectTransform))]
public class EnemyTypeBadge : MonoBehaviour
{
    [SerializeField] private EnemyBadgeType badgeType = EnemyBadgeType.Flame;
    [SerializeField] private Sprite overrideSprite;
    [SerializeField, Range(0.1f, 1f)] private float sizeRatio = 0.45f;
    [SerializeField] private Vector2 anchorPoint = new Vector2(0.88f, 0.88f);

    private const int TEX_SIZE = 64;
    private const int SUPERSAMPLE = 4;
    private const float OUTLINE_INSIDE = 0.05f;
    private const float OUTLINE_OUTSIDE = 0.035f;

    // 적마다 새로 그리면 스폰할 때마다 렉이 생기므로, 종류별로 한 번만 만들어 모든 적이 공유한다.
    private static Sprite flameSprite;
    private static Sprite starSprite;

    private Image ownerImage;
    private Image badgeImage;

    private void Awake()
    {
        ownerImage = GetComponent<Image>();
        var ownerRt = (RectTransform)transform;

        var go = new GameObject("TypeBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(ownerRt, false);
        rt.anchorMin = anchorPoint;
        rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        float side = Mathf.Min(ownerRt.rect.width, ownerRt.rect.height) * sizeRatio;
        rt.sizeDelta = new Vector2(side, side);

        badgeImage = go.GetComponent<Image>();
        badgeImage.sprite = overrideSprite != null ? overrideSprite : GetGeneratedSprite(badgeType);
        badgeImage.preserveAspect = true;
        // 표시가 클릭을 가로채면 적 모서리를 눌렀을 때 공격이 안 먹힌다.
        badgeImage.raycastTarget = false;
    }

    // 사망 연출(파편 소멸 등)은 본체 알파만 줄이므로, 표시만 덩그러니 남지 않게 따라간다.
    private void LateUpdate()
    {
        if (ownerImage == null || badgeImage == null) return;

        Color c = badgeImage.color;
        c.a = ownerImage.color.a;
        badgeImage.color = c;
    }

    private static Sprite GetGeneratedSprite(EnemyBadgeType type)
    {
        if (type == EnemyBadgeType.Flame)
        {
            if (flameSprite == null) flameSprite = BuildFlame();
            return flameSprite;
        }

        if (starSprite == null) starSprite = BuildStar();
        return starSprite;
    }

    private static Sprite BuildStar()
    {
        var points = new List<Vector2>(10);
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float radius = i % 2 == 0 ? 0.47f : 0.2f;
            points.Add(new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.46f + Mathf.Sin(angle) * radius));
        }

        return Rasterize(points, null,
            new Color(1f, 0.85f, 0.2f, 1f),
            new Color(0.4f, 0.22f, 0.05f, 1f),
            Color.clear);
    }

    private static Sprite BuildFlame()
    {
        List<Vector2> outer = BuildDrop(new Vector2(0.5f, 0.5f), 0.45f, 48);
        List<Vector2> core = BuildDrop(new Vector2(0.5f, 0.38f), 0.24f, 32);

        return Rasterize(outer, core,
            new Color(1f, 0.45f, 0.08f, 1f),
            new Color(0.45f, 0.12f, 0.02f, 1f),
            new Color(1f, 0.86f, 0.3f, 1f));
    }

    // 위쪽이 뾰족하고 아래가 둥근 물방울 곡선 — 불꽃 실루엣으로 쓰기 좋다.
    private static List<Vector2> BuildDrop(Vector2 center, float size, int segments)
    {
        var points = new List<Vector2>(segments);
        for (int i = 0; i < segments; i++)
        {
            float t = i * Mathf.PI * 2f / segments;
            float x = size * Mathf.Sin(t) * Mathf.Sin(t * 0.5f);
            float y = size * Mathf.Cos(t);
            points.Add(center + new Vector2(x, y));
        }
        return points;
    }

    private static Sprite Rasterize(List<Vector2> outer, List<Vector2> core, Color fill, Color outline, Color coreColor)
    {
        var tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

        var pixels = new Color[TEX_SIZE * TEX_SIZE];
        float sampleCount = SUPERSAMPLE * SUPERSAMPLE;

        for (int py = 0; py < TEX_SIZE; py++)
        {
            for (int px = 0; px < TEX_SIZE; px++)
            {
                // 픽셀 하나를 여러 점으로 나눠 찍어 평균 내야 작은 크기에서도 가장자리가 계단지지 않는다.
                Color sum = Color.clear;
                for (int sy = 0; sy < SUPERSAMPLE; sy++)
                {
                    for (int sx = 0; sx < SUPERSAMPLE; sx++)
                    {
                        var p = new Vector2(
                            (px + (sx + 0.5f) / SUPERSAMPLE) / TEX_SIZE,
                            (py + (sy + 0.5f) / SUPERSAMPLE) / TEX_SIZE);

                        bool inside = Contains(outer, p);
                        float dist = DistanceToEdges(outer, p);

                        if (inside && dist > OUTLINE_INSIDE)
                        {
                            sum += core != null && Contains(core, p) ? coreColor : fill;
                        }
                        else if (inside || dist <= OUTLINE_OUTSIDE)
                        {
                            sum += outline;
                        }
                    }
                }

                Color c = sum / sampleCount;
                if (c.a > 0f)
                {
                    c.r /= c.a;
                    c.g /= c.a;
                    c.b /= c.a;
                }
                pixels[py * TEX_SIZE + px] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    private static bool Contains(List<Vector2> poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static float DistanceToEdges(List<Vector2> poly, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[j];
            Vector2 ab = poly[i] - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            float d = (a + ab * t - p).sqrMagnitude;
            if (d < best) best = d;
        }
        return Mathf.Sqrt(best);
    }
}
}
