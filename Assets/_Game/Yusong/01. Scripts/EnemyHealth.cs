using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int hitsToDestroy = 1;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private RectTransform sparkPrefab;
    [SerializeField] private int sparkCount = 8;
    [SerializeField] private float sparkSpeed = 400f;
    [SerializeField] private float sparkLifetime = 0.25f;
    [SerializeField] private RectTransform hitMarkPrefab;

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
        originalColor = image.color;
        originalScale = rt.localScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDestroyed) return;

        hitsTaken++;
        SpawnSparks();
        SpawnHitMark();

        if (hitsTaken >= hitsToDestroy)
        {
            isDestroyed = true;

            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.RegisterKill();
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
}
