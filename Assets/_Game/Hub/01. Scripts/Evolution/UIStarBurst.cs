using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 실제 ParticleSystem 대신, 미리 배치해둔 별 모양 UI Image 여러 개를 중심에서 사방으로
/// 흩뿌리듯 퍼뜨리는 연출. Screen Space - Overlay Canvas는 카메라로 렌더링되지 않아
/// 월드 스페이스 ParticleSystem이 보이지 않으므로, UI 좌표계 안에서 직접 흉내낸다.
/// </summary>
public class UIStarBurst : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private float minDistance = 80f;
    [SerializeField] private float maxDistance = 160f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float startScale = 0.6f;

    private RectTransform[] starTransforms;
    private Vector2[] originalAnchoredPositions;

    /// <summary>burst 애니메이션 전체 길이. EvolutionController가 다음 단계로 넘어가기 전 대기 시간으로 쓴다.</summary>
    public float Duration => duration;

    private void Awake()
    {
        starTransforms = new RectTransform[stars.Length];
        originalAnchoredPositions = new Vector2[stars.Length];

        for (int i = 0; i < stars.Length; i++)
        {
            starTransforms[i] = stars[i].rectTransform;
            originalAnchoredPositions[i] = starTransforms[i].anchoredPosition;
            stars[i].gameObject.SetActive(false);
        }
    }

    public void Play()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            PlayStar(i);
        }
    }

    private void PlayStar(int index)
    {
        RectTransform rect = starTransforms[index];
        Image image = stars[index];

        Vector2 center = originalAnchoredPositions[index];
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minDistance, maxDistance);
        Vector2 targetPosition = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        rect.anchoredPosition = center;
        rect.localScale = Vector3.one * startScale;

        Color color = image.color;
        color.a = 1f;
        image.color = color;

        image.gameObject.SetActive(true);

        rect.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutQuad);
        rect.DOScale(0f, duration).SetEase(Ease.InQuad);
        image.DOFade(0f, duration).OnComplete(() =>
        {
            image.gameObject.SetActive(false);
            rect.anchoredPosition = center;
        });
    }
}
