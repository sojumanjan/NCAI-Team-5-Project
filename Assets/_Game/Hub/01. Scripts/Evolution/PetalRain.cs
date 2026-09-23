using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 위에서 꽃잎이 흔들리며 떨어진다. 화면 전체를 덮는 RectTransform에 붙인다.
/// 씨앗이 다 자란 뒤 여운부터 엔딩 크레딧까지 계속 흩날린다.
///
/// DOTween 대신 Update에서 직접 움직이는 이유: 꽃잎마다 좌우 흔들림·회전·낙하가 제각각 끝없이
/// 이어져야 해서, 트윈으로 쪼개면 수십 개가 계속 생겼다 죽는다. 한 곳에서 돌리는 편이 싸고 단순하다.
///
/// 게임 시간으로 움직인다. 엔딩 중 옵션 창을 열어 시간이 멈추면 꽃잎도 멈춰야 어색하지 않다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PetalRain : MonoBehaviour
{
    [Header("양")]
    [Tooltip("1초에 새로 떨어지기 시작하는 꽃잎 수.")]
    [SerializeField] private float petalsPerSecond = 3.5f;

    [Tooltip("시작하자마자 화면 곳곳에 미리 흩어 둘 꽃잎 수. 0이면 위에서부터 차례로 내려옵니다.")]
    [SerializeField] private int prewarmCount = 6;

    [Header("모양")]
    [Tooltip("꽃잎 크기 범위 (캔버스 픽셀).")]
    [SerializeField] private Vector2 sizeRange = new Vector2(22f, 40f);

    [Tooltip("무작위로 골라 입힐 꽃잎 색.")]
    [SerializeField] private Color[] colors =
    {
        new Color(1f, 0.78f, 0.84f, 0.95f),
        new Color(1f, 0.86f, 0.9f, 0.95f),
        new Color(0.98f, 0.7f, 0.78f, 0.95f),
        new Color(1f, 0.94f, 0.95f, 0.95f),
    };

    [Header("움직임")]
    [Tooltip("떨어지는 속도 범위 (캔버스 픽셀/초).")]
    [SerializeField] private Vector2 fallSpeed = new Vector2(70f, 130f);

    [Tooltip("좌우로 흔들리는 폭 (캔버스 픽셀).")]
    [SerializeField] private Vector2 swayAmount = new Vector2(20f, 60f);

    [Tooltip("좌우로 흔들리는 빠르기 (1초당 왕복 횟수).")]
    [SerializeField] private Vector2 swayFrequency = new Vector2(0.3f, 0.7f);

    [Tooltip("바람에 밀리는 가로 속도 (캔버스 픽셀/초). 음수면 왼쪽으로.")]
    [SerializeField] private float wind = 25f;

    [Tooltip("회전 속도 범위 (도/초).")]
    [SerializeField] private Vector2 spinSpeed = new Vector2(40f, 120f);

    private struct Petal
    {
        public RectTransform Rect;
        public Vector2 Origin;
        public float Fall;
        public float SwayAmount;
        public float SwayFrequency;
        public float Phase;
        public float Spin;
        public float Age;
    }

    private RectTransform _root;
    private Sprite _sprite;
    private readonly List<Petal> _alive = new();
    private readonly Stack<Image> _pool = new();
    private bool _raining;
    private float _spawnDebt;

    public bool IsRaining => _raining;

    private void Awake()
    {
        _root = (RectTransform)transform;
        _sprite = UIProceduralSprite.Petal();
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_sprite);
    }

    /// <summary>흩날리기 시작한다.</summary>
    public void StartRain()
    {
        if (_raining)
        {
            return;
        }

        _raining = true;
        _spawnDebt = 0f;

        // 위에서 첫 꽃잎이 내려올 때까지 몇 초 텅 비면 "시작했다"가 안 느껴진다.
        Rect area = _root.rect;
        for (int i = 0; i < prewarmCount; i++)
        {
            Spawn(Random.Range(area.yMin, area.yMax));
        }
    }

    /// <summary>새 꽃잎을 그만 뿌린다. 떨어지던 것은 끝까지 떨어진다.</summary>
    public void StopRain()
    {
        _raining = false;
    }

    /// <summary>화면의 꽃잎을 전부 치운다. 엔딩에서 방으로 돌아갈 때 검은 화면 뒤에서 부른다.</summary>
    public void Clear()
    {
        _raining = false;
        foreach (Petal petal in _alive)
        {
            Recycle(petal.Rect);
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

        if (_raining)
        {
            _spawnDebt += petalsPerSecond * dt;
            while (_spawnDebt >= 1f)
            {
                _spawnDebt -= 1f;
                Spawn(_root.rect.yMax + sizeRange.y);
            }
        }

        float bottom = _root.rect.yMin - sizeRange.y;

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            Petal petal = _alive[i];
            petal.Age += dt;
            petal.Origin += new Vector2(wind, -petal.Fall) * dt;

            float sway = Mathf.Sin((petal.Age * petal.SwayFrequency + petal.Phase) * Mathf.PI * 2f);
            petal.Rect.anchoredPosition = petal.Origin + new Vector2(sway * petal.SwayAmount, 0f);

            // 가로 배율을 흔들림에 맞춰 줄였다 늘리면 꽃잎이 뒤집히며 떨어지는 것처럼 보인다.
            petal.Rect.localRotation = Quaternion.Euler(0f, 0f, petal.Rect.localEulerAngles.z + petal.Spin * dt);
            petal.Rect.localScale = new Vector3(Mathf.Lerp(0.35f, 1f, Mathf.Abs(sway)), 1f, 1f);

            if (petal.Origin.y < bottom)
            {
                Recycle(petal.Rect);
                _alive.RemoveAt(i);
                continue;
            }

            _alive[i] = petal;
        }
    }

    private void Spawn(float y)
    {
        Rect area = _root.rect;

        // 바람에 밀려 들어오는 몫까지 생각해 바람 반대쪽으로 조금 더 넓게 뿌린다.
        float windMargin = Mathf.Abs(wind) * 4f;
        float xMin = area.xMin - (wind > 0f ? windMargin : 0f);
        float xMax = area.xMax + (wind < 0f ? windMargin : 0f);

        Image image = Take();
        RectTransform rect = image.rectTransform;
        float size = Random.Range(sizeRange.x, sizeRange.y);
        rect.sizeDelta = new Vector2(size * 0.7f, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        image.color = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

        _alive.Add(new Petal
        {
            Rect = rect,
            Origin = new Vector2(Random.Range(xMin, xMax), y),
            Fall = Random.Range(fallSpeed.x, fallSpeed.y),
            SwayAmount = Random.Range(swayAmount.x, swayAmount.y),
            SwayFrequency = Random.Range(swayFrequency.x, swayFrequency.y),
            Phase = Random.value,
            Spin = Random.Range(spinSpeed.x, spinSpeed.y) * (Random.value < 0.5f ? -1f : 1f),
            Age = 0f,
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

        var go = new GameObject("Petal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.sprite = _sprite;
        image.raycastTarget = false;
        return image;
    }

    private void Recycle(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.gameObject.SetActive(false);
        _pool.Push(rect.GetComponent<Image>());
    }

    private void OnValidate()
    {
        petalsPerSecond = Mathf.Max(0f, petalsPerSecond);
        prewarmCount = Mathf.Max(0, prewarmCount);
        sizeRange.x = Mathf.Max(1f, sizeRange.x);
        sizeRange.y = Mathf.Max(sizeRange.x, sizeRange.y);
        fallSpeed.x = Mathf.Max(1f, fallSpeed.x);
        fallSpeed.y = Mathf.Max(fallSpeed.x, fallSpeed.y);
        swayAmount.y = Mathf.Max(swayAmount.x, swayAmount.y);
        swayFrequency.y = Mathf.Max(swayFrequency.x, swayFrequency.y);
        spinSpeed.y = Mathf.Max(spinSpeed.x, spinSpeed.y);
    }
}
