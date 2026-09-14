using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [SerializeField] private float speed = 120f;
    [SerializeField] private Vector2 target = Vector2.zero;
    [SerializeField] private float hitRadius = 80f;

    private RectTransform rt;
    private bool hasHit;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (hasHit) return;

        rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, target, speed * Time.deltaTime);

        if (Vector2.Distance(rt.anchoredPosition, target) <= hitRadius)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        hasHit = true;

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.TakeDamage(1);
        }

        Destroy(gameObject);
    }
}
