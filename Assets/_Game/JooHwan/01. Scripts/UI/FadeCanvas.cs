using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeCanvas : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    private Coroutine activeFade;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeOut(Action onComplete = null)
    {
        StartFade(0f, 1f, onComplete);
    }

    public void FadeIn(Action onComplete = null)
    {
        StartFade(1f, 0f, onComplete);
    }

    private void StartFade(float from, float to, Action onComplete)
    {
        if (activeFade != null)
        {
            StopCoroutine(activeFade);
        }

        activeFade = StartCoroutine(FadeRoutine(from, to, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, Action onComplete)
    {
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0.99f;

        activeFade = null;
        onComplete?.Invoke();
    }
}
