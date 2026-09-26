// 그림 파일 없이 모서리가 둥근 사각형을 그리는 UI 그래픽 (옵션 창의 탭 모양 등)
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 창의 탭은 배경 그림에 통째로 그려져 있어 탭 하나만 따로 떼어 쓸 그림이 없다. 같은 색·같은 둥글기로 직접 그린다.
/// 가장자리에 1단위 폭의 투명 띠를 둘러 계단 없이 부드럽게 보이게 한다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UIRoundedRect : MaskableGraphic
{
    [Tooltip("모서리 반지름 (이 오브젝트의 로컬 단위). 짧은 변의 절반을 넘으면 절반으로 줄입니다.")]
    [SerializeField] private float radius = 30f;

    [Tooltip("왼쪽 두 모서리를 둥글게.")]
    [SerializeField] private bool roundLeft = true;

    [Tooltip("오른쪽 두 모서리를 둥글게. 탭처럼 패널에 붙는 쪽은 끈다.")]
    [SerializeField] private bool roundRight = true;

    [Tooltip("가장자리를 부드럽게 흐리는 폭 (로컬 단위).")]
    [SerializeField] private float feather = 1f;

    private const int SEGMENTS = 10;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        float r = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        float rLeft = roundLeft ? r : 0f;
        float rRight = roundRight ? r : 0f;

        // 오른쪽 위부터 반시계 방향으로 네 모서리를 돈다.
        var points = new System.Collections.Generic.List<Vector2>();
        var normals = new System.Collections.Generic.List<Vector2>();
        AddCorner(points, normals, new Vector2(rect.xMax, rect.yMax), rRight, 0f, new Vector2(1f, 1f));
        AddCorner(points, normals, new Vector2(rect.xMin, rect.yMax), rLeft, 90f, new Vector2(-1f, 1f));
        AddCorner(points, normals, new Vector2(rect.xMin, rect.yMin), rLeft, 180f, new Vector2(-1f, -1f));
        AddCorner(points, normals, new Vector2(rect.xMax, rect.yMin), rRight, 270f, new Vector2(1f, -1f));

        Color32 solid = color;
        Color32 clear = color;
        clear.a = 0;
        float half = Mathf.Max(0f, feather) * 0.5f;

        vh.AddVert(rect.center, solid, Vector2.zero);
        int count = points.Count;
        for (int i = 0; i < count; i++)
        {
            vh.AddVert(points[i] - normals[i] * half, solid, Vector2.zero);
            vh.AddVert(points[i] + normals[i] * half, clear, Vector2.zero);
        }

        for (int i = 0; i < count; i++)
        {
            int inner = 1 + i * 2;
            int nextInner = 1 + (i + 1) % count * 2;
            vh.AddTriangle(0, inner, nextInner);

            if (half > 0f)
            {
                vh.AddTriangle(inner, inner + 1, nextInner + 1);
                vh.AddTriangle(inner, nextInner + 1, nextInner);
            }
        }
    }

    /// <summary>모서리 하나의 호를 점으로 찍는다. 반지름이 0이면 꼭짓점 하나만 찍는다.</summary>
    private static void AddCorner(System.Collections.Generic.List<Vector2> points, System.Collections.Generic.List<Vector2> normals,
                                  Vector2 corner, float r, float startAngle, Vector2 sign)
    {
        if (r <= 0f)
        {
            points.Add(corner);
            // 직각 꼭짓점은 가로·세로로 똑같이 밀어야 두 변 모두 같은 폭으로 흐려진다.
            normals.Add(sign);
            return;
        }

        Vector2 center = corner - sign * r;
        for (int s = 0; s <= SEGMENTS; s++)
        {
            float angle = (startAngle + 90f * s / SEGMENTS) * Mathf.Deg2Rad;
            var normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points.Add(center + normal * r);
            normals.Add(normal);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
        feather = Mathf.Max(0f, feather);
        base.OnValidate();
    }
#endif
}
