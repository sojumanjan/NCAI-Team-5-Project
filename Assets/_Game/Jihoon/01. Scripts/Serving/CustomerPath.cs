using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손님이 밟고 지나갈 경유지 목록. 카운터까지 일직선으로 걸어오는 게 부자연스러워서 둔다.
///
/// <see cref="ServingSpot"/>에 배열로 박지 않고 별도 컴포넌트로 뺀 이유는, 입구가 하나라
/// 레인 세 개가 같은 진입로를 공유해야 하기 때문이다. 경로를 고칠 때 세 군데를 똑같이
/// 고치는 실수를 없앤다.
///
/// 배치: 빈 오브젝트에 이걸 붙이고, 경유지를 자식으로 순서대로 깔면 된다. Points를 비워두면
/// 자식을 하이어라키 순서대로 쓴다.
/// </summary>
public class CustomerPath : MonoBehaviour
{
    [Tooltip("지나갈 순서대로의 경유지. 비워두면 자식 오브젝트를 하이어라키 순서대로 씁니다.")]
    [SerializeField] private Transform[] points;

    [Header("기즈모")]
    [Tooltip("씬 뷰에 그릴 경유지 구체 크기 (m).")]
    [SerializeField] private float gizmoRadius = 0.25f;

    [Tooltip("경로 색.")]
    [SerializeField] private Color gizmoColor = new Color(0.3f, 0.9f, 1f);

    private readonly List<Transform> _resolved = new();

    /// <summary>지나갈 순서대로의 경유지. 비어 있을 수 있다.</summary>
    public IReadOnlyList<Transform> Points
    {
        get
        {
            Resolve();
            return _resolved;
        }
    }

    private void Resolve()
    {
        _resolved.Clear();

        if (points != null && points.Length > 0)
        {
            foreach (Transform point in points)
            {
                if (point != null)
                {
                    _resolved.Add(point);
                }
            }

            return;
        }

        // 배열이 비었으면 자식을 순서대로. 경유지를 늘릴 때 배열까지 손보지 않아도 되게.
        for (int i = 0; i < transform.childCount; i++)
        {
            _resolved.Add(transform.GetChild(i));
        }
    }

    /// <summary>경유지 좌표를 <paramref name="into"/>에 담는다. 매 프레임 호출용은 아니다.</summary>
    public void AppendPositions(List<Vector3> into, bool reversed)
    {
        Resolve();

        if (reversed)
        {
            for (int i = _resolved.Count - 1; i >= 0; i--)
            {
                into.Add(_resolved[i].position);
            }

            return;
        }

        foreach (Transform point in _resolved)
        {
            into.Add(point.position);
        }
    }

    /// <summary>첫 경유지. 손님이 태어나는 자리로 쓴다. 비어 있으면 null.</summary>
    public Transform First
    {
        get
        {
            Resolve();
            return _resolved.Count > 0 ? _resolved[0] : null;
        }
    }

    [ContextMenu("자식으로 채우기")]
    private void FillFromChildren()
    {
        points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }

    private void OnDrawGizmos()
    {
        Resolve();
        if (_resolved.Count == 0)
        {
            return;
        }

        Gizmos.color = gizmoColor;

        for (int i = 0; i < _resolved.Count; i++)
        {
            Gizmos.DrawWireSphere(_resolved[i].position, gizmoRadius);

            if (i + 1 < _resolved.Count)
            {
                Gizmos.DrawLine(_resolved[i].position, _resolved[i + 1].position);
            }
        }

        // 시작점만 채워서 진행 방향이 한눈에 보이게.
        Gizmos.DrawSphere(_resolved[0].position, gizmoRadius * 0.5f);
    }
}
