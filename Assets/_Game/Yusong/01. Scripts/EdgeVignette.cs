using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
// 필드 경계를 검은 선 대신 화면 가장자리를 은은하게 어둡게 해서 보여준다.
// 그라데이션 텍스처를 코드로 만들어서 별도 이미지 에셋이 필요 없다.
[RequireComponent(typeof(Image))]
public class EdgeVignette : MonoBehaviour
{
    [SerializeField] private Color edgeColor = new Color(0.03f, 0.14f, 0.16f, 0.45f);
    [SerializeField, Range(0.01f, 0.5f)] private float bandX = 0.14f;
    [SerializeField, Range(0.01f, 0.5f)] private float bandY = 0.18f;

    // 화면 전체에 늘려 쓰는 부드러운 그라데이션이라 작은 해상도로 충분하다(쌍선형 보간으로 매끄럽게 늘어남).
    private const int TEX_WIDTH = 128;
    private const int TEX_HEIGHT = 72;

    private Texture2D texture;
    private Sprite sprite;

    private void Awake()
    {
        var image = GetComponent<Image>();
        sprite = BuildSprite();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        // 화면 전체를 덮으므로 클릭을 막으면 적을 누를 수 없게 된다.
        image.raycastTarget = false;
    }

    // 씬을 다시 불러올 때마다 새로 만들기 때문에, 이전 것을 치우지 않으면 메모리에 계속 쌓인다.
    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }

    private Sprite BuildSprite()
    {
        texture = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var pixels = new Color[TEX_WIDTH * TEX_HEIGHT];
        for (int y = 0; y < TEX_HEIGHT; y++)
        {
            float v = (y + 0.5f) / TEX_HEIGHT;
            float ey = EdgeWeight(Mathf.Min(v, 1f - v), bandY);

            for (int x = 0; x < TEX_WIDTH; x++)
            {
                float u = (x + 0.5f) / TEX_WIDTH;
                float ex = EdgeWeight(Mathf.Min(u, 1f - u), bandX);

                // 두 축을 곱으로 합쳐야 모서리가 두 배로 진해지지 않고 자연스럽게 이어진다.
                float weight = 1f - (1f - ex) * (1f - ey);

                Color c = edgeColor;
                c.a = edgeColor.a * weight;
                pixels[y * TEX_WIDTH + x] = c;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        return Sprite.Create(texture, new Rect(0f, 0f, TEX_WIDTH, TEX_HEIGHT), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float EdgeWeight(float distanceFromEdge, float band)
    {
        float t = Mathf.Clamp01(distanceFromEdge / band);
        return 1f - t * t * (3f - 2f * t);
    }
}
}
