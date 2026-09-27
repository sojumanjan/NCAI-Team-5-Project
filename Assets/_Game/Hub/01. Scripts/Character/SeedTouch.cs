// 허브에서 씨앗을 클릭하면 제자리에서 뽀잉뽀잉 두 번 뛰고, 신난 표정과 함께 하트를 오른쪽 위로 뿌리는 반응
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 씨앗 그림(CharacterImage)에 붙인다. 걷기는 CharacterRoot를 움직이므로 점프는 그 안의 그림만 움직여 서로 부딪히지 않는다.
/// 진화·인트로·엔딩·클리어 연출 중에는 그쪽이 그림 크기와 표정을 쥐고 있어서 반응하지 않는다.
///
/// 크기를 누르고 늘일 때 발밑을 붙잡아 둔다. 가운데 기준으로 줄이면 공중에서 쪼그라드는 것처럼 보인다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SeedTouch : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [Tooltip("표정을 바꿔 줄 컴포넌트 (CharacterRoot). 비워두면 씬에서 찾습니다.")]
    [SerializeField] private SeedExpression expression;

    [Tooltip("클리어 연출 중엔 반응하지 않습니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private HubClearReveal reveal;

    [Tooltip("하트를 띄울 부모. 비워두면 씨앗(CharacterRoot)의 부모에, 씨앗 바로 위 순서로 띄웁니다.")]
    [SerializeField] private RectTransform heartLayer;

    [Tooltip("만졌을 때 재생할 소리.")]
    [SerializeField] private SoundData sound;

    [Header("반응")]
    [Tooltip("한 번 반응한 뒤 다시 반응하기까지 (초).")]
    [SerializeField] private float cooldown = 3f;

    [Tooltip("신난 표정을 유지하는 시간 (초).")]
    [SerializeField] private float happyDuration = 1.5f;

    [Header("점프")]
    [Tooltip("첫 번째 점프 높이. 두 번째는 이 높이의 절반쯤으로 뜁니다.")]
    [SerializeField] private float jumpHeight = 28f;

    [SerializeField, Range(0f, 1f)] private float secondJumpRatio = 0.55f;

    [Tooltip("점프 전체 시간 (초).")]
    [SerializeField] private float jumpDuration = 0.8f;

    [Tooltip("땅에 닿을 때 눌리는 정도.")]
    [SerializeField, Range(0f, 0.4f)] private float squash = 0.12f;

    [Tooltip("뛸 때마다 좌우로 번갈아 기우는 최대 각도 (도). 발밑을 축으로 기웁니다.")]
    [SerializeField, Range(0f, 30f)] private float tiltAngle = 8f;

    [Header("하트")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Vector2Int heartCount = new Vector2Int(3, 5);
    [SerializeField] private Vector2 heartSize = new Vector2(35.2f, 48f);

    [Tooltip("하트가 날아가는 거리 범위.")]
    [SerializeField] private Vector2 heartDistance = new Vector2(110f, 170f);

    [Tooltip("하트가 날아가는 방향 범위 (도). 0이 오른쪽, 90이 위쪽.")]
    [SerializeField] private Vector2 heartAngle = new Vector2(30f, 75f);

    [SerializeField] private float heartDuration = 0.9f;

    [Tooltip("하트끼리 나오는 간격 (초).")]
    [SerializeField] private float heartStagger = 0.08f;

    private RectTransform _rect;
    private Vector2 _basePos;
    private Vector3 _baseScale;
    private Quaternion _baseRotation;
    private float _readyAt;
    private Coroutine _jump;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        if (expression == null) expression = FindAnyObjectByType<SeedExpression>();
        if (reveal == null) reveal = FindAnyObjectByType<HubClearReveal>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Time.time < _readyAt || IsBusy()) return;

        _readyAt = Time.time + cooldown;

        if (sound != null) AudioManager.Play(sound);
        if (expression != null) expression.ShowHappyFor(happyDuration);

        if (_jump != null) StopJump();
        _basePos = _rect.anchoredPosition;
        _baseScale = _rect.localScale;
        _baseRotation = _rect.localRotation;
        _jump = StartCoroutine(Jump());

        SpawnHearts();
    }

    private bool IsBusy()
    {
        if (expression != null && expression.IsBusy()) return true;
        return reveal != null && reveal.IsPlaying;
    }

    private IEnumerator Jump()
    {
        // 두 번 뛴다. 앞의 짧은 구간은 뛰기 전에 웅크리는 시간이다.
        float total = Mathf.Max(0.05f, jumpDuration);
        const float CROUCH = 0.1f;
        const float SPLIT = 0.6f;

        float height = _rect.rect.height * Mathf.Abs(_baseScale.y);
        float elapsed = 0f;

        while (elapsed < total)
        {
            // 연출이 끼어들면 그쪽에 그림을 넘겨준다.
            if (IsBusy())
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / total);

            float lift = 0f;
            float stretch;
            float tilt = 0f;

            if (t < CROUCH)
            {
                // 뛰기 전에 살짝 웅크린다.
                stretch = -squash * Mathf.Sin(t / CROUCH * Mathf.PI * 0.5f);
            }
            else
            {
                float air = (t - CROUCH) / (1f - CROUCH);
                bool first = air < SPLIT;
                float u = first ? air / SPLIT : (air - SPLIT) / (1f - SPLIT);
                float amp = first ? 1f : secondJumpRatio;

                // 포물선으로 뜨고, 공중에서는 세로로 늘었다가 땅에 가까울수록 눌린다.
                // 눌림은 두 점프 모두 같은 세기라 점프와 점프 사이, 웅크림과 첫 점프 사이가 끊기지 않고 이어진다.
                lift = jumpHeight * amp * 4f * u * (1f - u);
                float airborne = Mathf.Sin(u * Mathf.PI);
                float nearGround = 1f - Mathf.Clamp01(Mathf.Min(u, 1f - u) * 6f);

                // 마지막 착지는 눌렸다가 풀리며 끝나야 제자리에 딱 멈춘 느낌이 난다.
                if (!first && u > 0.5f)
                {
                    float k = Mathf.InverseLerp(0.5f, 1f, u);
                    nearGround *= 1f - k * k * (3f - 2f * k);
                }

                stretch = squash * (0.7f * amp * airborne - nearGround);

                // 첫 점프는 한쪽, 두 번째는 반대쪽으로. 공중에서 가장 기울고 땅에 닿을 땐 똑바로 선다.
                tilt = tiltAngle * airborne * (first ? 1f : -0.8f);
            }

            float sy = 1f + stretch;
            float sx = 1f - stretch * 0.6f;
            _rect.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);

            // 세로로 줄어든 만큼 내려서 발밑을 제자리에 둔다.
            float keepFeet = (sy - 1f) * height * _rect.pivot.y;

            // 기준점이 그림 가운데라 그냥 돌리면 팽이처럼 돈다. 돈 만큼 되밀어 발밑을 축으로 기울게 한다.
            Quaternion rotation = Quaternion.Euler(0f, 0f, tilt);
            Vector2 feet = Vector2.down * (height * sy * _rect.pivot.y);
            Vector2 pivotFix = feet - (Vector2)(rotation * feet);

            _rect.localRotation = _baseRotation * rotation;
            _rect.anchoredPosition = _basePos + Vector2.up * (lift + keepFeet) + pivotFix;

            yield return null;
        }

        StopJump();
    }

    private void StopJump()
    {
        if (_jump != null)
        {
            StopCoroutine(_jump);
            _jump = null;
        }

        _rect.anchoredPosition = _basePos;
        _rect.localScale = _baseScale;
        _rect.localRotation = _baseRotation;
    }

    private void OnDisable()
    {
        if (_jump != null) StopJump();
    }

    private void SpawnHearts()
    {
        if (heartSprite == null) return;

        RectTransform root = _rect.parent as RectTransform;
        RectTransform layer = heartLayer != null ? heartLayer : root != null ? root.parent as RectTransform : null;
        if (layer == null) return;

        // 씨앗이 좌우로 뒤집혀 있어도 화면 기준 오른쪽 위에서 나오게, 화면에 보이는 사각형으로 잰다.
        var corners = new Vector3[4];
        _rect.GetWorldCorners(corners);
        Vector3 min = Vector3.Min(corners[0], corners[2]);
        Vector3 max = Vector3.Max(corners[0], corners[2]);
        Vector3 startWorld = new Vector3(Mathf.Lerp(min.x, max.x, 0.7f), Mathf.Lerp(min.y, max.y, 0.85f), max.z);
        Vector2 start = layer.InverseTransformPoint(startWorld);

        int count = Random.Range(heartCount.x, heartCount.y + 1);
        int order = heartLayer == null && root != null ? root.GetSiblingIndex() + 1 : layer.childCount;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Heart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(layer, false);
            rect.SetSiblingIndex(Mathf.Min(order, layer.childCount - 1));

            var image = go.GetComponent<Image>();
            image.sprite = heartSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            float size = Random.Range(heartSize.x, heartSize.y);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // 오른쪽 위로 부채꼴처럼 퍼진다. 순서대로 각도를 나눠 줘야 겹쳐 뭉치지 않는다.
            float slot = count > 1 ? (float)i / (count - 1) : 0.5f;
            float angle = Mathf.Lerp(heartAngle.x, heartAngle.y, slot) + Random.Range(-6f, 6f);
            float distance = Random.Range(heartDistance.x, heartDistance.y);
            Vector2 end = start + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;

            rect.anchoredPosition = start - Vector2.Scale(layer.rect.size, rect.anchorMin - layer.pivot);
            Vector2 endPos = end - Vector2.Scale(layer.rect.size, rect.anchorMin - layer.pivot);
            rect.localScale = Vector3.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));

            float delay = i * heartStagger;
            DOTween.Sequence()
                   .SetLink(go)
                   .Insert(delay, rect.DOScale(1f, heartDuration * 0.3f).SetEase(Ease.OutBack))
                   .Insert(delay, rect.DOAnchorPos(endPos, heartDuration).SetEase(Ease.OutCubic))
                   .Insert(delay + heartDuration * 0.55f, image.DOFade(0f, heartDuration * 0.45f).SetEase(Ease.InQuad))
                   .OnComplete(() => Destroy(go));
        }
    }

    private void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        happyDuration = Mathf.Max(0f, happyDuration);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        jumpDuration = Mathf.Max(0.05f, jumpDuration);
        heartCount.x = Mathf.Max(0, heartCount.x);
        heartCount.y = Mathf.Max(heartCount.x, heartCount.y);
        heartDuration = Mathf.Max(0.05f, heartDuration);
    }
}
