using UnityEngine;
using UnityEngine.UI;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "FrequencyWaveGraphic")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FrequencyWaveGraphic : MaskableGraphic
{
    #region 참조 및 설정

    [SerializeField] private float frequency = 6;
    [SerializeField] private float amplitude = .7f;
    #endregion

    #region 신호 설정

    /// <summary>
    /// 표시할 주파수와 진폭을 설정합니다.
    /// </summary>
    public void SetSignal(float hz, float level)
    {
        frequency = hz; amplitude = level; SetVerticesDirty();
    }
    #endregion

    #region 파형 메시 생성

    /// <summary>
    /// 눈금과 사인 파형을 UI 메시로 그립니다.
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        Color grid = new Color(.13f, .29f, .28f, .65f);
        for (int i = 0; i <= 10; i++)
        {
            float x = Mathf.Lerp(r.xMin, r.xMax, i / 10f);
            Line(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 1, grid);
        }
        for (int i = 0; i <= 4; i++)
        {
            float y = Mathf.Lerp(r.yMin, r.yMax, i / 4f);
            Line(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), i == 2 ? 2 : 1, grid);
        }
        Vector2 last = new Vector2(r.xMin, r.center.y);
        for (int i = 1; i <= 240; i++)
        {
            float t = i / 240f;
            Vector2 next = new Vector2(Mathf.Lerp(r.xMin, r.xMax, t),
                r.center.y + Mathf.Sin(t * frequency * Mathf.PI * 2) * amplitude * r.height * .43f);
            Line(vh, last, next, 4, color);
            last = next;
        }
    }
    /// <summary>
    /// 두 지점 사이에 두께가 있는 선을 추가합니다.
    /// </summary>
    private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * width * .5f;
        int start = vh.currentVertCount;
        vh.AddVert(a - n, tint, Vector2.zero); vh.AddVert(a + n, tint, Vector2.zero);
        vh.AddVert(b + n, tint, Vector2.zero); vh.AddVert(b - n, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
    }
    #endregion

}
}
