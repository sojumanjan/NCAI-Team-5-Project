using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
// 광역 공격·폭발 때 퍼져나가는 연출. 꽉 찬 원판은 "범위 표시"처럼 보이고 배경색과 섞여 탁해져서,
// 가운데가 빈 충격파 고리 + 안쪽 흰 테두리 + 바깥으로 튀는 물방울로 표현한다.
// 고리 이미지는 코드로 그려 공유하고, 아트가 나오면 overrideRingSprite만 채우면 된다.
[RequireComponent(typeof(Image))]
public class RingBurstEffect : MonoBehaviour
{
    [SerializeField] private Sprite overrideRingSprite;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float startScale = 0.55f;
    [SerializeField] private float endScale = 1.05f;
    [SerializeField, Range(0f, 1f)] private float highlightAlpha = 0.85f;
    [SerializeField] private int dropletCount = 10;
    [SerializeField] private float dropletSize = 26f;
    [SerializeField, Range(0f, 1f)] private float dropletTravel = 0.35f;

    private const int RING_TEX_SIZE = 256;
    private const int DROPLET_TEX_SIZE = 64;
    // 고리 중심을 이미지 가장자리 근처에 둬야 이미지 크기(= 실제 공격 반경 × 2)와 눈에 보이는 고리가 일치한다.
    private const float RING_RADIUS = 0.86f;

    private static Sprite ringSprite;
    private static Sprite highlightSprite;
    private static Sprite dropletSprite;

    private RectTransform rt;
    private Image image;
    private Image highlight;
    private Color baseColor;
    private float age;
    private bool started;

    private RectTransform[] droplets;
    private Image[] dropletImages;
    private Vector2[] dropletDirs;
    private float[] dropletSpeeds;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.sprite = overrideRingSprite != null ? overrideRingSprite : GetRingSprite();
        image.raycastTarget = false;
        rt.localScale = Vector3.one * startScale;
    }

    // 생성하는 쪽(EnemyHealth, SecondaryObjectController)이 Instantiate 직후에 테마 색을 칠하므로 색은 Start에서 읽는다.
    private void Start()
    {
        baseColor = image.color;

        highlight = CreateChildImage("Highlight", GetHighlightSprite());
        var hrt = highlight.rectTransform;
        hrt.anchorMin = Vector2.zero;
        hrt.anchorMax = Vector2.one;
        hrt.offsetMin = Vector2.zero;
        hrt.offsetMax = Vector2.zero;

        int count = Mathf.Max(0, dropletCount);
        droplets = new RectTransform[count];
        dropletImages = new Image[count];
        dropletDirs = new Vector2[count];
        dropletSpeeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            var img = CreateChildImage("Droplet", GetDropletSprite());
            var drt = img.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            // 부모 고리가 커지면서 물방울도 같이 커지므로, 크기는 시작 배율 기준으로 되돌려 둔다.
            drt.sizeDelta = Vector2.one * (dropletSize * Random.Range(0.7f, 1.2f) / Mathf.Max(0.01f, startScale));

            float angle = (i + Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / Mathf.Max(1, count);
            dropletDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dropletSpeeds[i] = Random.Range(0.6f, 1.2f);

            droplets[i] = drt;
            dropletImages[i] = img;
        }

        started = true;
        Apply(0f);
    }

    private void Update()
    {
        if (!started) return;

        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age / duration);
        Apply(t);

        if (age >= duration) Destroy(gameObject);
    }

    private void Apply(float t)
    {
        // 처음엔 빠르게 퍼지고 끝에서 느려져야 충격파처럼 보인다.
        float eased = 1f - (1f - t) * (1f - t) * (1f - t);
        rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

        float fade = Mathf.Pow(1f - t, 1.5f);

        Color c = baseColor;
        c.a = baseColor.a * fade;
        image.color = c;

        if (highlight != null) highlight.color = new Color(1f, 1f, 1f, highlightAlpha * fade);

        if (droplets == null) return;

        float ringHalf = rt.rect.width * 0.5f * RING_RADIUS;
        Color dropColor = Color.Lerp(baseColor, Color.white, 0.45f);
        for (int i = 0; i < droplets.Length; i++)
        {
            float dist = ringHalf * (0.9f + dropletTravel * eased * dropletSpeeds[i]);
            droplets[i].anchoredPosition = dropletDirs[i] * dist;
            Color dc = dropColor;
            dc.a = fade;
            dropletImages[i].color = dc;
        }
    }

    private Image CreateChildImage(string childName, Sprite sprite)
    {
        var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // ---------------------------------------------------------------- 이미지 생성

    private static Sprite GetRingSprite()
    {
        if (ringSprite == null) ringSprite = BuildRadial(RING_TEX_SIZE, RingAlpha);
        return ringSprite;
    }

    private static Sprite GetHighlightSprite()
    {
        if (highlightSprite == null) highlightSprite = BuildRadial(RING_TEX_SIZE, HighlightAlpha);
        return highlightSprite;
    }

    private static Sprite GetDropletSprite()
    {
        if (dropletSprite == null) dropletSprite = BuildRadial(DROPLET_TEX_SIZE, DropletAlpha);
        return dropletSprite;
    }

    // 굵은 고리 + 은은한 안쪽 채움. 색은 흰색으로 그리고 Image 색으로 물들인다.
    private static float RingAlpha(float d)
    {
        float band = Mathf.Exp(-Mathf.Pow((d - RING_RADIUS) / 0.07f, 2f));
        float inner = d < RING_RADIUS ? 0.14f * Mathf.SmoothStep(0.25f, RING_RADIUS, d) : 0f;
        return Mathf.Clamp01(Mathf.Max(band, inner));
    }

    // 고리 안쪽을 따라 도는 얇은 흰 테두리 — 배경이 비쳐도 탁해지지 않고 또렷하게 보이게 한다.
    private static float HighlightAlpha(float d)
    {
        return Mathf.Exp(-Mathf.Pow((d - (RING_RADIUS - 0.03f)) / 0.018f, 2f));
    }

    private static float DropletAlpha(float d)
    {
        return 1f - Mathf.SmoothStep(0.55f, 1f, d);
    }

    private static Sprite BuildRadial(int size, System.Func<float, float> alphaAt)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

        var pixels = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, d >= 1f ? 0f : alphaAt(d));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }
}
}
