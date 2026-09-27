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

    /// <summary>
    /// 애니메이션 없이 즉시 완전히 검은 상태로 만든다. 씬이 막 로드된 시점처럼,
    /// 이미 다른 화면(메인씬 등)이 검게 가려둔 상태를 이어받을 때 쓴다 —
    /// 여기서 다시 FadeOut(0→1)을 재생하면 잠깐 훤히 보였다가 어두워지는 어색한 깜빡임이 생긴다.
    /// </summary>
    public void SnapToBlack()
    {
        if (activeFade != null)
        {
            StopCoroutine(activeFade);
            activeFade = null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
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
