using System;
using UnityEngine;

/// <summary>
/// 허브 연출용 모양(빛, 고리, 먼지 구름, 반짝이)을 코드로 그려 스프라이트로 만든다.
///
/// 아트 에셋을 기다리지 않고 유니티 안에서 연출을 끝내기 위한 것. 흰색으로 그려두고
/// 색은 Image.color로 입히므로 하나의 모양을 여러 색으로 돌려쓸 수 있다.
///
/// 런타임에 만든 텍스처는 씬이 내려가도 저절로 지워지지 않는다. 만든 쪽이 OnDestroy에서
/// <see cref="Release"/>를 불러야 한다.
/// </summary>
public static class UIProceduralSprite
{
    /// <summary>가운데가 밝고 가장자리로 부드럽게 사라지는 빛.</summary>
    public static Sprite SoftDot(int size = 64) =>
        Create(size, (x, y) =>
        {
            float t = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y));
            return t * t;
        });

    /// <summary>테두리 근처만 밝은 고리. 안쪽을 살짝 채워야 퍼질 때 빈 동그라미보다 빛처럼 보인다.</summary>
    public static Sprite Ring(int size = 256) =>
        Create(size, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f)
            {
                return 0f;
            }

            float band = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.07f, 2f));
            float inner = r < 0.86f ? 0.12f * (r / 0.86f) : 0f;
            return band + inner;
        });

    /// <summary>
    /// 뭉게뭉게한 먼지 구름. 동그라미 하나면 비눗방울처럼 보여서 크기가 다른 덩어리 여럿을 겹친다.
    /// </summary>
    public static Sprite Cloud(int size = 128)
    {
        // (x, y, 반지름). 전부 -1~1 안에 들어가야 가장자리가 잘리지 않는다.
        Vector3[] lobes =
        {
            new Vector3(0f, -0.12f, 0.55f),
            new Vector3(-0.42f, -0.05f, 0.42f),
            new Vector3(0.42f, -0.02f, 0.44f),
            new Vector3(-0.15f, 0.32f, 0.42f),
            new Vector3(0.24f, 0.28f, 0.38f),
        };

        return Create(size, (x, y) =>
        {
            float alpha = 0f;
            foreach (Vector3 lobe in lobes)
            {
                float d = Mathf.Sqrt((x - lobe.x) * (x - lobe.x) + (y - lobe.y) * (y - lobe.y));

                // 가장자리만 살짝 흐리게. 너무 흐리면 먼지가 물체를 못 가려서 바꿔치기가 들킨다.
                alpha = Mathf.Max(alpha, Mathf.InverseLerp(lobe.z, lobe.z * 0.7f, d));
            }

            return alpha;
        });
    }

    /// <summary>네 갈래 반짝이. 가운데에 작은 빛을 더해야 멀리서도 점으로 뭉개지지 않는다.</summary>
    public static Sprite Sparkle(int size = 64) =>
        Create(size, (x, y) =>
        {
            float star = Mathf.Pow(Mathf.Abs(x), 0.5f) + Mathf.Pow(Mathf.Abs(y), 0.5f);
            float body = Mathf.InverseLerp(1f, 0.55f, star);
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 3f);
            return Mathf.Max(body, core);
        });

    /// <summary>꽃잎. 양 끝이 뾰족한 렌즈 모양이라 돌면서 떨어질 때 납작해졌다 넓어졌다 하며 꽃잎처럼 보인다.</summary>
    public static Sprite Petal(int size = 64) =>
        Create(size, (x, y) =>
        {
            float halfWidth = 0.55f * Mathf.Pow(Mathf.Max(0f, 1f - y * y), 0.7f);
            float edge = Mathf.InverseLerp(halfWidth, halfWidth - 0.12f, Mathf.Abs(x));

            // 한쪽 끝을 살짝 옅게 해서 꽃받침 쪽과 끝 쪽이 구분되게 한다.
            float tint = Mathf.Lerp(0.75f, 1f, (y + 1f) * 0.5f);
            return edge * tint;
        });

    /// <summary>아래를 가리키는 작은 삼각형. "눌러서 넘기기" 표시용.</summary>
    public static Sprite TriangleDown(int size = 64)
    {
        Vector2 a = new Vector2(-0.8f, 0.55f);
        Vector2 b = new Vector2(0.8f, 0.55f);
        Vector2 c = new Vector2(0f, -0.7f);

        return Create(size, (x, y) =>
        {
            var p = new Vector2(x, y);

            // 세 변 안쪽으로 얼마나 들어와 있는지. 가장 가까운 변 기준으로 가장자리를 부드럽게 깎는다.
            float inside = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
            return Mathf.InverseLerp(0f, 0.08f, inside);
        });
    }

    /// <summary>선분 from→to의 오른쪽(시계 방향 안쪽)으로 떨어진 거리. 바깥이면 음수.</summary>
    private static float EdgeDistance(Vector2 p, Vector2 from, Vector2 to)
    {
        Vector2 edge = to - from;
        Vector2 inward = new Vector2(edge.y, -edge.x).normalized;
        return Vector2.Dot(p - from, inward);
    }

    /// <summary>만든 스프라이트와 텍스처를 지운다.</summary>
    public static void Release(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(sprite.texture);
        UnityEngine.Object.Destroy(sprite);
    }

    /// <summary>x, y는 -1~1 (가운데 0). 흰색에 알파만 채운다.</summary>
    private static Sprite Create(int size, Func<float, float, float> alphaAt)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };

        var pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f - half) / half;
                float y = (py + 0.5f - half) / half;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaAt(x, y)) * 255f);
                pixels[py * size + px] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
