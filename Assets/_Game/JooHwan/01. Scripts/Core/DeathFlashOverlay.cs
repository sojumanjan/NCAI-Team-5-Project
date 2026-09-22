using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사망/클리어처럼 순간적인 임팩트가 필요할 때 화면 전체를 짧게 물들이는 단발성 플래시.
/// 화면 전체를 덮는 Image의 색과 알파를 즉시 올렸다가 서서히 0으로 되돌린다.
/// 기본 Flash()는 사망 전용 색(어두운 빨강)을 쓰고, 클리어 등 다른 상황은 색을 직접 지정한다.
/// </summary>
[RequireComponent(typeof(Image))]
public class DeathFlashOverlay : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(0.4f, 0.02f, 0.02f, 0.6f);
    [SerializeField] private float fadeOutDuration = 0.4f;

    private Image image;
    private Coroutine flashRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        SetColor(Color.clear);
    }

    public void Flash()
    {
        Flash(flashColor, fadeOutDuration);
    }

    /// <summary>
    /// 색과 지속 시간을 직접 지정해 플래시를 재생한다 (예: 클리어 시 밝은 색).
    /// </summary>
    public void Flash(Color color, float duration)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color peakColor, float duration)
    {
        SetColor(peakColor);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetColor(Color.Lerp(peakColor, Color.clear, elapsed / duration));
            yield return null;
        }

        SetColor(Color.clear);
    }

    private void SetColor(Color color)
    {
        image.color = color;
    }
}
