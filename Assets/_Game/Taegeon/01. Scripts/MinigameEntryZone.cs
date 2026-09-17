using UnityEngine;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "MinigameEntryZone")]
[RequireComponent(typeof(SphereCollider))]
public sealed class MinigameEntryZone : MonoBehaviour
{
    #region 참조 및 설정

    [SerializeField, Range(0, 3)] private int gameIndex;
    private readonly Collider[] overlaps = new Collider[64];
    public int GameIndex => gameIndex;

    #endregion

    #region 접근 판정

    /// <summary>
    /// 플레이어가 원형 접근 영역과 겹치는지 확인합니다.
    /// </summary>
    public bool Contains(FirstPersonExplorer player)
    {
        if (player == null || !isActiveAndEnabled) return false;
        var sphere = GetComponent<SphereCollider>();
        if (!sphere.enabled) return false;
        Vector3 scale = transform.lossyScale;
        float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 center = transform.TransformPoint(sphere.center);
        int count = Physics.OverlapSphereNonAlloc(center, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        // 감지 배열이 가득 차면 다시 조회해 플레이어 누락을 방지합니다.
        var hits = count == overlaps.Length
            ? Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore)
            : overlaps;
        int length = hits == overlaps ? count : hits.Length;
        for (int i = 0; i < length; i++)
            if (hits[i] != null && hits[i].GetComponentInParent<FirstPersonExplorer>() == player)
                return true;
        return false;
    }

    #endregion

    #region 에디터 범위 표시

    /// <summary>
    /// 선택한 접근 영역의 범위를 에디터에 표시합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        var sphere = GetComponent<SphereCollider>();
        Gizmos.color = new Color(1f, .7f, .15f, .5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
    }
    #endregion

}
}
