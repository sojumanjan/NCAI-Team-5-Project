using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 곳곳에서 반짝이가 톡톡 피었다 사라진다. 화면 전체를 덮는 RectTransform에 붙인다.
/// 방이 깨끗해진 순간 잠깐(Spawn Duration) "방이 되살아났다"를 반짝임으로 보여준다.
///
/// 꽃잎(PetalRain)과 같은 이유로 Update에서 직접 돌린다. 수십 개가 끝없이 생겼다 사라지는 걸
/// 트윈으로 쪼개면 트윈이 계속 쌓인다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapTwinkle : MonoBehaviour
{
    [Header("양")]
    [Tooltip("1초에 새로 피는 반짝이 수.")]
    [SerializeField] private float twinklesPerSecond = 9f;

    [Tooltip("시작하자마자 한꺼번에 피울 반짝이 수. 방이 바뀌는 순간 확 빛나 보이게.")]
    [SerializeField] private int burstCount = 18;

    [Tooltip("시작한 뒤 새 반짝이를 피우는 시간 (초). 지나면 새로 피우는 것만 멈추고, 떠 있던 것은 끝까지 사라집니다.")]
    [SerializeField] private float spawnDuration = 1f;

    [Header("모양")]
    [Tooltip("반짝이 크기 범위 (캔버스 픽셀).")]
    [SerializeField] private Vector2 sizeRange = new Vector2(20f, 46f);

    [Tooltip("반짝이 하나가 피었다 사라지는 시간 범위 (초).")]
    [SerializeField] private Vector2 lifetime = new Vector2(0.6f, 1.2f);

    [Tooltip("피어 있는 동안 도는 각도 (도).")]
    [SerializeField] private float spin = 90f;

    [Tooltip("화면 가장자리에서 이만큼 안쪽에만 피웁니다 (캔버스 픽셀).")]
    [SerializeField] private float edgeMargin = 40f;

    [Tooltip("무작위로 골라 입힐 색.")]
    [SerializeField] private Color[] colors =
    {
        new Color(1f, 0.98f, 0.85f, 1f),
        new Color(1f, 0.93f, 0.65f, 1f),
        new Color(1f, 1f, 1f, 1f),
    };

    private struct Twinkle
    {
        public RectTransform Rect;
        public Image Image;
        public float Age;
        public float Life;
        public float Spin;
        public float BaseAngle;
    }

    private RectTransform _root;
    private Sprite _sprite;
    private readonly List<Twinkle> _alive = new();
    private readonly Stack<Image> _pool = new();
    private bool _running;
    private float _spawnDebt;
    private float _spawnTimeLeft;

    private void Awake()
    {
        _root = (RectTransform)transform;
        _sprite = UIProceduralSprite.Sparkle();
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_sprite);
    }

    /// <summary>반짝이기 시작한다. 처음 한 번은 한꺼번에 여럿 피운다.</summary>
    public void StartTwinkle()
    {
        _running = true;
        _spawnDebt = 0f;
        _spawnTimeLeft = spawnDuration;

        for (int i = 0; i < burstCount; i++)
        {
            Spawn();
        }
    }

    /// <summary>새로 피우는 것만 멈춘다. 피어 있던 것은 끝까지 사라진다.</summary>
    public void StopTwinkle()
    {
        _running = false;
    }

    /// <summary>화면의 반짝이를 전부 치운다. 흰 화면이나 검은 화면 뒤에서 부른다.</summary>
    public void Clear()
    {
        _running = false;
        foreach (Twinkle twinkle in _alive)
        {
            Recycle(twinkle.Image);
        }

        _alive.Clear();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        if (_running)
        {
            // 방이 바뀌는 순간만 반짝인다. 엔딩으로 넘어갈 때까지 계속 피우면 여운 내내 화면이 어수선하다.
            float spawnDt = Mathf.Min(dt, _spawnTimeLeft);
            _spawnTimeLeft -= dt;

            _spawnDebt += twinklesPerSecond * spawnDt;
            while (_spawnDebt >= 1f)
            {
                _spawnDebt -= 1f;
                Spawn();
            }

            if (_spawnTimeLeft <= 0f)
            {
                _running = false;
            }
        }

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            Twinkle twinkle = _alive[i];
            twinkle.Age += dt;
            float t = twinkle.Age / twinkle.Life;

            if (t >= 1f)
            {
                Recycle(twinkle.Image);
                _alive.RemoveAt(i);
                continue;
            }

            // 빠르게 피고 천천히 지는 모양. 대칭으로 커졌다 작아지면 깜빡임이 아니라 맥박처럼 보인다.
            float bloom = t < 0.3f ? Mathf.SmoothStep(0f, 1f, t / 0.3f) : Mathf.SmoothStep(1f, 0f, (t - 0.3f) / 0.7f);
            twinkle.Rect.localScale = Vector3.one * bloom;
            twinkle.Rect.localRotation = Quaternion.Euler(0f, 0f, twinkle.BaseAngle + twinkle.Spin * t);

            _alive[i] = twinkle;
        }
    }

    private void Spawn()
    {
        Rect area = _root.rect;
        float margin = Mathf.Min(edgeMargin, area.width * 0.5f, area.height * 0.5f);

        Image image = Take();
        RectTransform rect = image.rectTransform;
        float size = Random.Range(sizeRange.x, sizeRange.y);

        rect.anchoredPosition = new Vector2(Random.Range(area.xMin + margin, area.xMax - margin),
                                            Random.Range(area.yMin + margin, area.yMax - margin));
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.zero;
        image.color = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

        _alive.Add(new Twinkle
        {
            Rect = rect,
            Image = image,
            Age = 0f,
            Life = Random.Range(lifetime.x, lifetime.y),
            Spin = spin * (Random.value < 0.5f ? -1f : 1f),
            BaseAngle = Random.Range(0f, 45f),
        });
    }

    private Image Take()
    {
        if (_pool.Count > 0)
        {
            Image pooled = _pool.Pop();
            pooled.gameObject.SetActive(true);
            return pooled;
        }

        var go = new GameObject("Twinkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.sprite = _sprite;
        image.raycastTarget = false;
        return image;
    }

    private void Recycle(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(false);
        _pool.Push(image);
    }

    private void OnValidate()
    {
        twinklesPerSecond = Mathf.Max(0f, twinklesPerSecond);
        burstCount = Mathf.Max(0, burstCount);
        spawnDuration = Mathf.Max(0f, spawnDuration);
        sizeRange.x = Mathf.Max(1f, sizeRange.x);
        sizeRange.y = Mathf.Max(sizeRange.x, sizeRange.y);
        lifetime.x = Mathf.Max(0.05f, lifetime.x);
        lifetime.y = Mathf.Max(lifetime.x, lifetime.y);
        edgeMargin = Mathf.Max(0f, edgeMargin);
    }
}
