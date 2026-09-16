using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int hitsToDestroy = 1;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private int comboStacksOnKill = 1;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private RectTransform sparkPrefab;
    [SerializeField] private int sparkCount = 8;
    [SerializeField] private float sparkSpeed = 400f;
    [SerializeField] private float sparkLifetime = 0.25f;
    [SerializeField] private RectTransform hitMarkPrefab;
    [SerializeField] private RectTransform aoeRingPrefab;

    [Header("Explosive (yellow variant)")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionRadius = 150f;
    [SerializeField] private float feverExplosionRadiusMultiplier = 1.5f;
    [SerializeField] private RectTransform explosionRingPrefab;

    [Header("Theme")]
    [SerializeField] private UITheme theme;
    [SerializeField] private EnemyShape shape = EnemyShape.Circle;

    [Header("Health Bar (optional, e.g. boss)")]
    [SerializeField] private Image healthBarFill;

    private Image image;
    private RectTransform rt;
    private Color originalColor;
    private Vector3 originalScale;
    private int hitsTaken;
    private bool isDestroyed;
    private Coroutine hitRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        originalScale = rt.localScale;

        if (theme != null)
        {
            hitFlashColor = theme.enemyHitFlashColor;
            ApplyShapeSprite();

            if (healthBarFill != null)
            {
                healthBarFill.color = theme.bossHealthBarColor;
            }
        }

        originalColor = image.color;
        UpdateHealthBar();
    }

    private void ApplyShapeSprite()
    {
        Sprite sprite = shape switch
        {
            EnemyShape.Circle => theme.circleSprite,
            EnemyShape.Triangle => theme.triangleSprite,
            EnemyShape.Square => theme.squareSprite,
            _ => null
        };

        if (sprite != null)
        {
            image.sprite = sprite;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDestroyed) return;

        if (ComboManager.IsAoeActive)
        {
            TriggerAoe();
        }
        else
        {
            ApplyHit();
        }
    }

    private void TriggerAoe()
    {
        Vector2 center = rt.anchoredPosition;
        Transform parent = transform.parent;
        float radius = ComboManager.AoeRadius;

        SpawnAoeRing(center, radius);

        var targets = new List<EnemyHealth>();
        foreach (Transform child in parent)
        {
            var eh = child.GetComponent<EnemyHealth>();
            if (eh != null && !eh.isDestroyed)
            {
                float dist = Vector2.Distance(eh.rt.anchoredPosition, center);
                if (dist <= radius)
                {
                    targets.Add(eh);
                }
            }
        }

        foreach (var target in targets)
        {
            target.ApplyHit();
        }
    }

    private void TriggerExplosion()
    {
        Vector2 center = rt.anchoredPosition;
        Transform parent = transform.parent;
        float radius = ComboManager.IsAoeActive ? explosionRadius * feverExplosionRadiusMultiplier : explosionRadius;

        SpawnExplosionRing(center, radius);

        var targets = new List<EnemyHealth>();
        foreach (Transform child in parent)
        {
            if (child == transform) continue;

            var eh = child.GetComponent<EnemyHealth>();
            if (eh != null && !eh.isDestroyed)
            {
                float dist = Vector2.Distance(eh.rt.anchoredPosition, center);
                if (dist <= radius)
                {
                    targets.Add(eh);
                }
            }
        }

        foreach (var target in targets)
        {
            target.ReceiveSplashDamage(explosionDamage);
        }
    }

    public void ReceiveSplashDamage(int hits)
    {
        for (int i = 0; i < hits; i++)
        {
            if (isDestroyed) break;
            ApplyHit();
        }
    }

    private void ApplyHit()
    {
        if (isDestroyed) return;

        hitsTaken++;
        SpawnSparks();
        SpawnHitMark();
        UpdateHealthBar();

        if (hitsTaken >= hitsToDestroy)
        {
            isDestroyed = true;

            bool bonusActive = ComboManager.IsAoeActive;

            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.RegisterKill(rt.anchoredPosition, transform.parent, comboStacksOnKill);
            }

            if (ScoreManager.Instance != null)
            {
                int feverBonus = bonusActive ? scoreValue : 0;
                Vector2 scorePopupPos = rt.anchoredPosition + new Vector2(-40f, -15f);
                ScoreManager.Instance.AddEnemyKillScore(scoreValue, feverBonus, scorePopupPos, transform.parent);
            }

            if (isExplosive)
            {
                TriggerExplosion();
            }

            Destroy(gameObject);
            return;
        }

        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(HitFeedback());
    }

    private IEnumerator HitFeedback()
    {
        image.color = hitFlashColor;

        Vector3 targetScale = originalScale * punchScale;
        float half = punchDuration * 0.5f;
        bool colorReverted = false;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.LerpUnclamped(originalScale, targetScale, t / half);

            if (!colorReverted && t >= flashDuration)
            {
                image.color = originalColor;
                colorReverted = true;
            }

            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.LerpUnclamped(targetScale, originalScale, t / half);
            yield return null;
        }

        rt.localScale = originalScale;
        image.color = originalColor;
        hitRoutine = null;
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        healthBarFill.fillAmount = 1f - (float)hitsTaken / hitsToDestroy;
    }

    private void SpawnSparks()
    {
        if (sparkPrefab == null) return;

        Transform parent = transform.parent;
        Vector2 origin = rt.anchoredPosition;

        for (int i = 0; i < sparkCount; i++)
        {
            float angle = (360f / sparkCount) * i + Random.Range(-10f, 10f);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            var spark = Instantiate(sparkPrefab, parent);
            spark.anchoredPosition = origin;
            spark.GetComponent<SparkParticle>().Init(direction, sparkSpeed, sparkLifetime);
        }
    }

    private void SpawnHitMark()
    {
        if (hitMarkPrefab == null) return;

        var mark = Instantiate(hitMarkPrefab, transform.parent);
        mark.anchoredPosition = rt.anchoredPosition;
    }

    private void SpawnAoeRing(Vector2 position, float radius)
    {
        if (aoeRingPrefab == null) return;

        var ring = Instantiate(aoeRingPrefab, transform.parent);
        ring.anchoredPosition = position;
        ring.sizeDelta = new Vector2(radius * 2f, radius * 2f);

        if (theme != null)
        {
            var ringImage = ring.GetComponent<Image>();
            if (ringImage != null) ringImage.color = theme.aoeRingColor;
        }
    }

    private void SpawnExplosionRing(Vector2 position, float radius)
    {
        if (explosionRingPrefab == null) return;

        var ring = Instantiate(explosionRingPrefab, transform.parent);
        ring.anchoredPosition = position;
        ring.sizeDelta = new Vector2(radius * 2f, radius * 2f);

        if (theme != null)
        {
            var ringImage = ring.GetComponent<Image>();
            if (ringImage != null) ringImage.color = theme.explosionRingColor;
        }
    }
}
