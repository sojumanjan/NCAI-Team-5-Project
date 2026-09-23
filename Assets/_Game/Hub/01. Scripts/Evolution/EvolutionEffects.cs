using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씨앗 진화에 곁들이는 화면 이펙트. EvolutionController가 두 순간에 부른다.
///  - 흰빛이 차오르는 동안: 빛 알갱이들이 화면 가장자리에서 씨앗으로 빨려 든다 (<see cref="PlayGather"/>).
///  - 진화하는 순간: 화면이 번쩍이고 씨앗 중심에서 빛의 고리가 퍼진다 (<see cref="PlayBurst"/>).
///
/// 모양(동그란 빛, 고리)은 텍스처를 코드로 그려서 쓴다. 따로 임포트할 스프라이트가 없어야
/// 아트 작업을 기다리지 않고 바로 조정할 수 있다.
///
/// 자기 Canvas(override sorting)로 방 UI보다 위, 옵션 창보다 아래에 그린다. 같은 캔버스 안에
/// 두면 뒤에 오는 UI(툴팁·버튼)가 플래시를 가린다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EvolutionEffects : MonoBehaviour
{
    [Header("그리기 순서")]
    [Tooltip("방 캔버스(0)보다 위, 옵션 창(21)보다 아래.")]
    [SerializeField] private int sortingOrder = 10;

    [Header("빛 모으기")]
    [Tooltip("모여드는 빛 알갱이 수.")]
    [SerializeField] private int gatherCount = 36;

    [Tooltip("알갱이가 출발하는 거리 (캔버스 픽셀). 화면 밖에서 들어오게 크게 둡니다.")]
    [SerializeField] private float gatherRadius = 1000f;

    [Tooltip("알갱이 하나가 씨앗까지 날아가는 시간 범위 (초).")]
    [SerializeField] private Vector2 gatherLifetime = new Vector2(0.5f, 0.9f);

    [Tooltip("알갱이 크기 범위 (캔버스 픽셀).")]
    [SerializeField] private Vector2 gatherSize = new Vector2(16f, 34f);

    [SerializeField] private Color gatherColor = new Color(1f, 0.97f, 0.85f, 1f);

    [Header("화면 플래시")]
    [Tooltip("번쩍일 때 가장 진한 불투명도.")]
    [Range(0f, 1f)]
    [SerializeField] private float flashPeak = 0.85f;

    [SerializeField] private float flashInDuration = 0.06f;
    [SerializeField] private float flashOutDuration = 0.45f;
    [SerializeField] private Color flashColor = Color.white;

    [Header("충격파 고리")]
    [Tooltip("퍼져 나가는 고리 수. 여럿이면 조금씩 늦게 따라 나갑니다.")]
    [SerializeField] private int ringCount = 2;

    [Tooltip("고리 사이 간격 (초).")]
    [SerializeField] private float ringStagger = 0.1f;

    [Tooltip("고리 시작 지름 (캔버스 픽셀).")]
    [SerializeField] private float ringStartSize = 120f;

    [Tooltip("고리가 사라질 때의 지름 (캔버스 픽셀).")]
    [SerializeField] private float ringEndSize = 1400f;

    [SerializeField] private float ringDuration = 0.6f;
    [SerializeField] private Color ringColor = new Color(1f, 0.98f, 0.9f, 1f);

    private RectTransform _root;
    private Image _flash;
    private Sprite _dotSprite;
    private Sprite _ringSprite;
    private readonly List<Image> _dotPool = new();
    private readonly List<Image> _ringPool = new();
    private Coroutine _gathering;

    private void Awake()
    {
        _root = (RectTransform)transform;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        _dotSprite = UIProceduralSprite.SoftDot();
        _ringSprite = UIProceduralSprite.Ring();

        _flash = CreateImage("Flash", null);
        RectTransform flashRect = _flash.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        SetAlpha(_flash, flashColor, 0f);
        _flash.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_dotSprite);
        UIProceduralSprite.Release(_ringSprite);
    }

    // ---------------------------------------------------------------- 빛 모으기

    /// <summary>
    /// <paramref name="duration"/>초 동안 빛 알갱이를 <paramref name="target"/> 쪽으로 모은다.
    /// 마지막 알갱이가 딱 끝 무렵에 닿도록 출발 시각을 앞당겨 뿌린다.
    /// </summary>
    public void PlayGather(RectTransform target, float duration)
    {
        StopGather();
        if (target != null && gatherCount > 0)
        {
            _gathering = StartCoroutine(Gather(target, duration));
        }
    }

    public void StopGather()
    {
        if (_gathering != null)
        {
            StopCoroutine(_gathering);
            _gathering = null;
        }
    }

    private IEnumerator Gather(RectTransform target, float duration)
    {
        // 날아가는 시간만큼 먼저 끝나야 마지막 알갱이도 진화 순간 전에 도착한다.
        float window = Mathf.Max(0f, duration - gatherLifetime.y);
        float interval = gatherCount > 1 ? window / (gatherCount - 1) : 0f;

        for (int i = 0; i < gatherCount; i++)
        {
            SpawnDot(LocalOf(target));

            if (interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        _gathering = null;
    }

    private void SpawnDot(Vector2 center)
    {
        Image dot = Take(_dotPool, "GatherDot", _dotSprite);
        RectTransform rect = dot.rectTransform;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 start = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * gatherRadius * Random.Range(0.6f, 1f);
        float life = Random.Range(gatherLifetime.x, gatherLifetime.y);
        float size = Random.Range(gatherSize.x, gatherSize.y);

        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.one;
        SetAlpha(dot, gatherColor, 0f);

        // 끝에서 빨라져야 "빨려 든다". 일정 속도면 그냥 모여드는 것처럼 보인다.
        rect.DOAnchorPos(center, life).SetEase(Ease.InQuad).SetLink(dot.gameObject)
            .OnComplete(() => dot.gameObject.SetActive(false));
        rect.DOScale(0.3f, life).SetEase(Ease.InQuad).SetLink(dot.gameObject);
        dot.DOFade(gatherColor.a, life * 0.3f).SetLink(dot.gameObject);
    }

    // ---------------------------------------------------------------- 진화 순간

    /// <summary>화면을 번쩍이고 <paramref name="target"/> 중심에서 고리를 퍼뜨린다.</summary>
    public void PlayBurst(RectTransform target)
    {
        StopGather();
        Flash();

        if (target == null)
        {
            return;
        }

        Vector2 center = LocalOf(target);
        for (int i = 0; i < ringCount; i++)
        {
            SpawnRing(center, i * ringStagger);
        }
    }

    private void Flash()
    {
        _flash.DOKill();
        _flash.gameObject.SetActive(true);
        SetAlpha(_flash, flashColor, 0f);

        DOTween.Sequence()
               .Append(_flash.DOFade(flashPeak, flashInDuration))
               .Append(_flash.DOFade(0f, flashOutDuration).SetEase(Ease.OutQuad))
               .OnComplete(() => _flash.gameObject.SetActive(false))
               .SetLink(_flash.gameObject);
    }

    private void SpawnRing(Vector2 center, float delay)
    {
        Image ring = Take(_ringPool, "ShockRing", _ringSprite);
        RectTransform rect = ring.rectTransform;

        rect.anchoredPosition = center;
        rect.sizeDelta = new Vector2(ringStartSize, ringStartSize);
        rect.localScale = Vector3.one;
        SetAlpha(ring, ringColor, 0f);

        // 크기는 sizeDelta로 키운다. 스케일로 키우면 고리 두께까지 같이 굵어져 뭉개진다.
        DOTween.Sequence()
               .AppendInterval(delay)
               .AppendCallback(() => SetAlpha(ring, ringColor, ringColor.a))
               .Append(rect.DOSizeDelta(new Vector2(ringEndSize, ringEndSize), ringDuration).SetEase(Ease.OutCubic))
               .Join(ring.DOFade(0f, ringDuration).SetEase(Ease.InQuad))
               .OnComplete(() => ring.gameObject.SetActive(false))
               .SetLink(ring.gameObject);
    }

    // ---------------------------------------------------------------- 공통

    /// <summary>화면 어디에 있든 그 대상의 위치를 이 캔버스 좌표로. 씨앗은 방 캔버스에 있고 이펙트는 여기 있다.</summary>
    private Vector2 LocalOf(RectTransform target)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
        return local - _root.rect.center;
    }

    private Image Take(List<Image> pool, string label, Sprite sprite)
    {
        foreach (Image image in pool)
        {
            if (!image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(true);
                image.DOKill();
                image.rectTransform.DOKill();
                return image;
            }
        }

        Image created = CreateImage(label, sprite);
        pool.Add(created);
        return created;
    }

    private Image CreateImage(string label, Sprite sprite)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static void SetAlpha(Image image, Color baseColor, float alpha)
    {
        baseColor.a = alpha;
        image.color = baseColor;
    }

    private void OnValidate()
    {
        gatherCount = Mathf.Max(0, gatherCount);
        gatherRadius = Mathf.Max(0f, gatherRadius);
        gatherLifetime.x = Mathf.Max(0.05f, gatherLifetime.x);
        gatherLifetime.y = Mathf.Max(gatherLifetime.x, gatherLifetime.y);
        gatherSize.x = Mathf.Max(1f, gatherSize.x);
        gatherSize.y = Mathf.Max(gatherSize.x, gatherSize.y);
        flashInDuration = Mathf.Max(0.01f, flashInDuration);
        flashOutDuration = Mathf.Max(0.01f, flashOutDuration);
        ringCount = Mathf.Max(0, ringCount);
        ringStagger = Mathf.Max(0f, ringStagger);
        ringStartSize = Mathf.Max(1f, ringStartSize);
        ringEndSize = Mathf.Max(ringStartSize, ringEndSize);
        ringDuration = Mathf.Max(0.01f, ringDuration);
    }
}
