using UnityEngine;
using UnityEngine.UI;

// 원본 텍스처와 고정 가장자리를 유지하며 잎 메시를 변형합니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class UILeafWind : BaseMeshEffect
{
    #region 참조 및 설정

    public enum AttachmentEdge { Bottom, Top, Right }
    [SerializeField] private AttachmentEdge attachment = AttachmentEdge.Bottom;
    [SerializeField, Range(0f, 0.06f)] private float strength = 0.018f;
    [SerializeField, Min(0.05f)] private float speed = 0.8f;
    [SerializeField] private float phase;
    private const int Columns = 12;
    private const int Rows = 16;

    #endregion

    #region 에디터 갱신 및 잎 메시 효과
#if UNITY_EDITOR
    private double nextEditorRefresh;

    /// <summary>
    /// 에디터에서도 잎 효과가 갱신되도록 연결합니다.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        UnityEditor.EditorApplication.update -= RefreshEditorPlayback;
        UnityEditor.EditorApplication.update += RefreshEditorPlayback;
    }

    /// <summary>
    /// 에디터의 잎 효과 갱신 연결을 해제합니다.
    /// </summary>
    protected override void OnDisable()
    {
        UnityEditor.EditorApplication.update -= RefreshEditorPlayback;
        base.OnDisable();
    }

    /// <summary>
    /// 에디터 재생 중 잎 효과의 갱신 주기를 유지합니다.
    /// </summary>
    private void RefreshEditorPlayback()
    {
        if (!Application.isPlaying || UnityEditor.EditorApplication.isPaused || !IsActive()) return;
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        if (now < nextEditorRefresh) return;
        nextEditorRefresh = now + 1.0 / 30.0;
        graphic.SetVerticesDirty();
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    /// <summary>
    /// 활성화된 잎 메시를 다시 그리도록 요청합니다.
    /// </summary>
    private void Update()
    {
        if (IsActive()) graphic.SetVerticesDirty();
    }

    /// <summary>
    /// 고정된 가장자리를 유지하며 잎 메시를 바람처럼 변형합니다.
    /// </summary>
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount < 4) return;
        UIVertex vertex = UIVertex.simpleVert;
        mesh.PopulateUIVertex(ref vertex, 0);
        Color32 color = vertex.color;
        float left = vertex.position.x, right = left, bottom = vertex.position.y, top = bottom;
        Vector2 uvMin = vertex.uv0, uvMax = vertex.uv0;
        for (int k = 1; k < 4; k++)
        {
            mesh.PopulateUIVertex(ref vertex, k);
            left = Mathf.Min(left, vertex.position.x); right = Mathf.Max(right, vertex.position.x);
            bottom = Mathf.Min(bottom, vertex.position.y); top = Mathf.Max(top, vertex.position.y);
            uvMin = Vector2.Min(uvMin, vertex.uv0); uvMax = Vector2.Max(uvMax, vertex.uv0);
        }
        float width = right - left, height = top - bottom;
        // 흔들림은 게임 시간 배율과 무관하게 이어집니다.
        float time = Application.isPlaying
            ? (float)((Time.realtimeSinceStartupAsDouble * speed + phase) % (System.Math.PI * 200.0)) : 0f;
        mesh.Clear();
        for (int row = 0; row <= Rows; row++)
        for (int col = 0; col <= Columns; col++)
        {
            float u = col / (float)Columns, v = row / (float)Rows;
            float distance = attachment == AttachmentEdge.Top ? 1f - v :
                attachment == AttachmentEdge.Right ? 1f - u : v;
            float weight = distance * distance;
            float breeze = Application.isPlaying
                ? Mathf.Sin(time - distance * 0.7f) + 0.28f * Mathf.Sin(time * 1.71f + u * 1.9f + phase)
                : 0f;
            float bend = (attachment == AttachmentEdge.Right ? width : height) * strength * weight * breeze;
            float flutter = Application.isPlaying ? Mathf.Sin(time * 1.23f + distance * 2f) * weight : 0f;
            vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(Mathf.Lerp(left, right, u), Mathf.Lerp(bottom, top, v), 0f);
            if (attachment == AttachmentEdge.Right)
                vertex.position += new Vector3(bend * 0.18f, bend, 0f);
            else
                vertex.position += new Vector3(bend, width * strength * 0.12f * flutter, 0f);
            vertex.uv0 = new Vector2(Mathf.Lerp(uvMin.x, uvMax.x, u), Mathf.Lerp(uvMin.y, uvMax.y, v));
            mesh.AddVert(vertex);
        }
        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Columns; col++)
        {
            int i = row * (Columns + 1) + col;
            mesh.AddTriangle(i, i + Columns + 1, i + 1);
            mesh.AddTriangle(i + 1, i + Columns + 1, i + Columns + 2);
        }
    }
    #endregion

}
