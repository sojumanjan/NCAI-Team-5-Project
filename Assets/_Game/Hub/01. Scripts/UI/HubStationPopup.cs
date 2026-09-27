// 스테이션 안내 창이 열릴 때 누른 스테이션 그림이 제자리에서 떠올라 창 위로 옮겨 가고, 아래 패널이 페이드로 나오는 연출
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 안내 창 캔버스 루트에 붙인다. 창은 HubGameTooltip이 SetActive로 켜므로 켜지는 순간(OnEnable) 연출을 튼다.
/// 닫기 버튼은 이 컴포넌트의 <see cref="Close"/>에 연결해야 거꾸로 돌아가며 닫힌다. 시작하기는 곧바로 화면이 검게 덮이므로 그냥 꺼진다.
///
/// 방에 놓인 스테이션 그림은 비율이 늘어난 채로 그려져 있고, 창의 그림은 원래 비율로 틀 안에 맞춰 그린다.
/// 날아가는 동안에는 비율 맞춤을 끄고 크기만 옮겨, 출발할 때는 방의 모습과 똑같고 도착할 때는 창의 모습과 똑같게 한다.
/// </summary>
public class HubStationPopup : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("이 창이 가리키는 허브 오브젝트. 그림이 여기서 출발합니다.")]
    [SerializeField] private MiniGameEntry entry;

    [Tooltip("창 위쪽 스테이션 그림.")]
    [SerializeField] private RectTransform sprite;

    [Tooltip("아래 설명 패널 묶음.")]
    [SerializeField] private CanvasGroup panel;

    [Tooltip("화면 전체를 덮는 어두운 막.")]
    [SerializeField] private Image dim;

    [Header("연출")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.55f;
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Tooltip("그림이 움직이기 시작하고 몇 초 뒤에 패널이 나타나기 시작할지.")]
    [SerializeField] private float panelDelay = 0.2f;
    [SerializeField] private float panelFadeDuration = 0.3f;

    [Tooltip("패널이 이만큼 아래에서 떠오르며 나타납니다.")]
    [SerializeField] private float panelRise = 30f;

    [Tooltip("닫을 때 거꾸로 돌아가는 시간 (초).")]
    [SerializeField] private float closeDuration = 0.3f;

    private Image _spriteImage;
    private Vector2 _spritePos;
    private Vector2 _spriteSize;
    private bool _spriteAspect;
    private Vector2 _panelPos;
    private Sequence _sequence;
    private bool _closing;

    private void Awake()
    {
        if (sprite != null)
        {
            _spriteImage = sprite.GetComponent<Image>();
            _spritePos = sprite.anchoredPosition;
            _spriteSize = sprite.sizeDelta;
            _spriteAspect = _spriteImage != null && _spriteImage.preserveAspect;
        }

        if (panel != null) _panelPos = ((RectTransform)panel.transform).anchoredPosition;
    }

    private void OnEnable()
    {
        _closing = false;
        HideAll();

        // 꺼져 있던 캔버스는 켜진 첫 프레임에 아직 화면 크기가 잡히지 않아 좌표를 옮길 수 없다. 한 프레임 기다린다.
        StartCoroutine(OpenNextFrame());
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        ResetToRest();
    }

    /// <summary>닫기 버튼에 연결한다. 그림이 제자리로 돌아간 뒤 창이 꺼진다.</summary>
    public void Close()
    {
        if (_closing || !isActiveAndEnabled) return;
        _closing = true;

        _sequence?.Kill();
        if (panel != null)
        {
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        if (panel != null) _sequence.Insert(0f, panel.DOFade(0f, closeDuration * 0.5f).SetEase(Ease.InQuad));
        if (dim != null) _sequence.Insert(0f, dim.DOFade(0f, closeDuration).SetEase(Ease.InQuad));

        if (sprite != null && TryGetStationRect(out Vector2 pos, out Vector2 size))
        {
            BeginFlight();
            _sequence.Insert(0f, sprite.DOAnchorPos(pos, closeDuration).SetEase(Ease.InOutCubic));
            _sequence.Insert(0f, sprite.DOSizeDelta(size, closeDuration).SetEase(Ease.InOutCubic));
        }

        _sequence.OnComplete(() => gameObject.SetActive(false));
    }

    private IEnumerator OpenNextFrame()
    {
        yield return null;

        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        if (dim != null) _sequence.Insert(0f, dim.DOFade(dimAlpha, moveDuration).SetEase(Ease.OutQuad));

        if (sprite != null)
        {
            sprite.localScale = Vector3.one;

            if (TryGetStationRect(out Vector2 pos, out Vector2 size))
            {
                BeginFlight();
                sprite.anchoredPosition = pos;
                sprite.sizeDelta = size;
                _sequence.Insert(0f, sprite.DOAnchorPos(_spritePos, moveDuration).SetEase(moveEase));
                _sequence.Insert(0f, sprite.DOSizeDelta(FittedSize(), moveDuration).SetEase(moveEase));
            }
        }

        if (panel != null)
        {
            var rect = (RectTransform)panel.transform;
            rect.anchoredPosition = _panelPos + Vector2.down * panelRise;
            _sequence.Insert(panelDelay, panel.DOFade(1f, panelFadeDuration).SetEase(Ease.OutQuad));
            _sequence.Insert(panelDelay, rect.DOAnchorPos(_panelPos, panelFadeDuration).SetEase(Ease.OutCubic));
        }

        _sequence.OnComplete(ResetToRest);
    }

    private void HideAll()
    {
        if (dim != null) SetAlpha(dim, 0f);

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }

        // 좌표를 잡기 전 한 프레임 동안 창 위치에 그림이 번쩍 보이지 않게 한다.
        if (sprite != null) sprite.localScale = Vector3.zero;
    }

    /// <summary>날아가는 동안에는 비율 맞춤을 끄고 사각형 자체를 옮긴다.</summary>
    private void BeginFlight()
    {
        if (_spriteImage != null) _spriteImage.preserveAspect = false;
    }

    /// <summary>연출이 끝나거나 창이 꺼질 때 편집해 둔 모습 그대로 되돌린다.</summary>
    private void ResetToRest()
    {
        if (sprite != null)
        {
            sprite.localScale = Vector3.one;
            sprite.anchoredPosition = _spritePos;
            sprite.sizeDelta = _spriteSize;
        }

        if (_spriteImage != null) _spriteImage.preserveAspect = _spriteAspect;

        if (panel != null)
        {
            ((RectTransform)panel.transform).anchoredPosition = _panelPos;
            if (!_closing)
            {
                panel.alpha = 1f;
                panel.interactable = true;
                panel.blocksRaycasts = true;
            }
        }

        if (dim != null && !_closing) SetAlpha(dim, dimAlpha);
    }

    /// <summary>창 틀 안에 그림 원래 비율로 맞춘 크기. 비율 맞춤을 켰을 때 실제로 그려지는 크기와 같다.</summary>
    private Vector2 FittedSize()
    {
        if (!_spriteAspect || _spriteImage == null || _spriteImage.sprite == null) return _spriteSize;

        Vector2 native = _spriteImage.sprite.rect.size;
        if (native.x <= 0f || native.y <= 0f) return _spriteSize;

        return native * Mathf.Min(_spriteSize.x / native.x, _spriteSize.y / native.y);
    }

    /// <summary>방에 보이는 스테이션 그림이 이 창의 좌표로 어디에 얼마만 한 크기로 있는지. 호버로 커진 크기까지 그대로 잰다.</summary>
    private bool TryGetStationRect(out Vector2 position, out Vector2 size)
    {
        position = Vector2.zero;
        size = Vector2.zero;

        if (entry == null || sprite == null || !(sprite.parent is RectTransform parent)) return false;

        GameObject view = entry.IsCleared ? entry.ClearedView : entry.NotClearedView;
        if (view == null || !(view.transform is RectTransform source)) return false;

        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.rootCanvas.worldCamera
            : null;
        Canvas ownCanvas = GetComponentInParent<Canvas>();
        Camera ownCamera = ownCanvas != null && ownCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? ownCanvas.rootCanvas.worldCamera
            : null;

        var corners = new Vector3[4];
        source.GetWorldCorners(corners);

        // 방의 그림이 비율 맞춤을 켜 두었다면 사각형이 아니라 실제로 그려진 부분에서 출발해야 한다.
        Image sourceImage = source.GetComponent<Image>();
        if (sourceImage != null && sourceImage.preserveAspect && sourceImage.sprite != null)
        {
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Vector3 extent = (corners[2] - corners[0]) * 0.5f;
            Vector2 native = sourceImage.sprite.rect.size;
            float k = Mathf.Min(Mathf.Abs(extent.x) * 2f / native.x, Mathf.Abs(extent.y) * 2f / native.y);
            extent = new Vector3(native.x * k * 0.5f * Mathf.Sign(extent.x), native.y * k * 0.5f * Mathf.Sign(extent.y), 0f);
            corners[0] = center - extent;
            corners[2] = center + extent;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[0]), ownCamera, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[2]), ownCamera, out Vector2 max))
        {
            return false;
        }

        // 부모 기준 좌표를 이 그림의 앵커 기준 좌표로 바꾼다. 앵커가 한 점일 때만 성립한다(지금 배치가 그렇다).
        Vector2 anchorPoint = Vector2.Scale(parent.rect.size, sprite.anchorMin - parent.pivot);
        size = max - min;
        position = (min + max) * 0.5f - anchorPoint + Vector2.Scale(size, sprite.pivot - new Vector2(0.5f, 0.5f));
        return true;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void OnValidate()
    {
        moveDuration = Mathf.Max(0.01f, moveDuration);
        panelFadeDuration = Mathf.Max(0.01f, panelFadeDuration);
        closeDuration = Mathf.Max(0.01f, closeDuration);
        panelDelay = Mathf.Max(0f, panelDelay);
    }
}
