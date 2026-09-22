using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public class SecondaryObjectController : MonoBehaviour
{
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float hitRadius = 80f;
    [SerializeField] private int hitsToDestroy = 3;
    [SerializeField] private int destroyedPenalty = 2000;
    [SerializeField] private int survivedBonusPerHp = 1000;
    [SerializeField] private Color hitColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private int flashBlinks = 2;
    [SerializeField] private UITheme theme;
    [SerializeField] private Image[] hpPips;

    [Header("Success Effect")]
    [SerializeField] private RectTransform successRingPrefab;
    [SerializeField] private float successRingRadius = 130f;
    [SerializeField] private Color successColor = new Color(0.55f, 1f, 0.35f, 1f);
    [SerializeField] private float successAnimDuration = 0.35f;
    [SerializeField] private float successScalePunch = 1.4f;

    [Header("Survival Warning")]
    [SerializeField] private float warningLeadTime = 3f;
    [SerializeField] private float warningBlinkInterval = 0.15f;

    [Header("Sound")]
    [SerializeField] private SoundData survivedSound;

    private RectTransform rt;
    private Image image;
    private Color originalColor;
    private Coroutine flashRoutine;
    private Coroutine warningRoutine;
    private int hitsTaken;
    private float age;
    private bool resolved;
    private bool warningStarted;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        // Sprite is intentionally left as whatever is set on the Image in the prefab —
        // 다슬이 has its own distinct design, unlike the generic round enemies that
        // share theme.circleSprite.

        originalColor = image.color;
        UpdateHpText();
    }

    private void Update()
    {
        if (resolved) return;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            ExpireNormally();
            return;
        }

        if (!warningStarted && lifetime - age <= warningLeadTime)
        {
            warningStarted = true;
            warningRoutine = StartCoroutine(PlayWarningBlink());
        }

        Transform parent = transform.parent;
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child == transform) continue;

            var mover = child.GetComponent<EnemyMover>();
            if (mover == null) continue;

            var enemyRt = child.GetComponent<RectTransform>();
            if (Vector2.Distance(enemyRt.anchoredPosition, rt.anchoredPosition) <= hitRadius)
            {
                Destroy(child.gameObject);
                TakeHit();

                if (resolved) return;
            }
        }
    }

    private void TakeHit()
    {
        hitsTaken++;
        UpdateHpText();

        if (hitsTaken >= hitsToDestroy)
        {
            DestroyByHits();
            return;
        }

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());
    }

    private void UpdateHpText()
    {
        int remaining = Mathf.Max(hitsToDestroy - hitsTaken, 0);

        if (hpPips != null)
        {
            Color onColor = theme != null ? theme.secondaryHpGaugeColor : successColor;
            Color offColor = theme != null ? theme.hpPipOffColor : new Color(1f, 1f, 1f, 0.25f);

            for (int i = 0; i < hpPips.Length; i++)
            {
                if (hpPips[i] == null) continue;
                hpPips[i].color = i < remaining ? onColor : offColor;
            }
        }
    }

    private void DestroyByHits()
    {
        resolved = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddSecondaryScore(-destroyedPenalty, rt.anchoredPosition, transform.parent, false);
        }

        Destroy(gameObject);
    }

    private void ExpireNormally()
    {
        resolved = true;

        int remainingHp = Mathf.Max(hitsToDestroy - hitsTaken, 0);
        int bonus = survivedBonusPerHp * remainingHp;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddSecondaryScore(bonus, rt.anchoredPosition, transform.parent, true);
        }

        SpawnSuccessRing();
        AudioManager.Play(survivedSound);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        StartCoroutine(PlaySuccessAnimation());
    }

    private IEnumerator PlayWarningBlink()
    {
        Color blinkColor = theme != null ? theme.secondarySuccessColor : successColor;

        while (!resolved)
        {
            image.color = blinkColor;
            yield return new WaitForSeconds(warningBlinkInterval);
            if (resolved) yield break;
            image.color = originalColor;
            yield return new WaitForSeconds(warningBlinkInterval);
        }
    }

    private void SpawnSuccessRing()
    {
        if (successRingPrefab == null || transform.parent == null) return;

        var ring = Instantiate(successRingPrefab, transform.parent);
        ring.anchoredPosition = rt.anchoredPosition;
        ring.sizeDelta = new Vector2(successRingRadius * 2f, successRingRadius * 2f);

        var ringImage = ring.GetComponent<Image>();
        if (ringImage != null)
        {
            Color c = theme != null ? theme.secondarySuccessColor : successColor;
            c.a = 0.55f;
            ringImage.color = c;
        }
    }

    private IEnumerator PlaySuccessAnimation()
    {
        Color targetColor = theme != null ? theme.secondarySuccessColor : successColor;
        Color startFlashColor = image.color;
        Vector3 startScale = rt.localScale;
        Vector3 targetScale = startScale * successScalePunch;

        float t = 0f;
        while (t < successAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / successAnimDuration);
            image.color = Color.Lerp(startFlashColor, targetColor, p);
            rt.localScale = Vector3.Lerp(startScale, targetScale, p);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator FlashHit()
    {
        for (int i = 0; i < flashBlinks; i++)
        {
            image.color = hitColor;
            yield return new WaitForSeconds(flashInterval);
            image.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
    }
}
}
