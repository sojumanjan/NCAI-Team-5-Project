using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class MinigameEntryZone : MonoBehaviour
{
    [SerializeField, Range(0, 3)] private int gameIndex;
    private readonly Collider[] overlaps = new Collider[64];
    public int GameIndex => gameIndex;

    public bool Contains(FirstPersonExplorer player)
    {
        if (player == null || !isActiveAndEnabled) return false;
        var sphere = GetComponent<SphereCollider>();
        if (!sphere.enabled) return false;
        Vector3 scale = transform.lossyScale;
        float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 center = transform.TransformPoint(sphere.center);
        int count = Physics.OverlapSphereNonAlloc(center, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        // A crowded area must not hide the player because the reusable buffer filled up.
        var hits = count == overlaps.Length
            ? Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore)
            : overlaps;
        int length = hits == overlaps ? count : hits.Length;
        for (int i = 0; i < length; i++)
            if (hits[i] != null && hits[i].GetComponentInParent<FirstPersonExplorer>() == player)
                return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        var sphere = GetComponent<SphereCollider>();
        Gizmos.color = new Color(1f, .7f, .15f, .5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
    }
}
