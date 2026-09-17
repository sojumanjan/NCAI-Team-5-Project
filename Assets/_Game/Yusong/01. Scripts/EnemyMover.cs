using UnityEngine;

namespace Yusong
{
public class EnemyMover : MonoBehaviour
{
    [SerializeField] private float speed = 120f;
    [SerializeField] private Vector2 target = Vector2.zero;
    [SerializeField] private float hitRadius = 80f;
    [SerializeField] private int damageOnHit = 1;

    private RectTransform rt;
    private bool hasHit;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public void AdjustSpeed(float delta)
    {
        speed = Mathf.Max(0f, speed + delta);
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
            PlayerHealth.Instance.TakeDamage(damageOnHit);
        }

        Destroy(gameObject);
    }
}
}
