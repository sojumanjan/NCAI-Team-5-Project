using UnityEngine;

/// <summary>
/// 영업시간이 흐르는 만큼 바깥 시간도 흐르게 한다. 해를 돌리고, 색을 식히고,
/// 어두워지는 만큼 실내등을 올린다.
///
/// 플레이어는 하루 내내 실내에 있어서 하늘을 거의 못 본다. 그래서 여기서 진짜 일을
/// 하는 건 스카이박스가 아니라 <b>셔터 밖에서 들어오는 빛의 색</b>과 <b>실내등</b>이다.
/// 특히 실내등은 연출이 아니라 안전장치다 — 20시에 바깥이 완전히 꺼져도 손이 보여야
/// 요리를 계속할 수 있다.
///
/// 색과 세기를 전부 Gradient·Curve로 뺀 이유는 눈으로 보고 잡는 값이기 때문이다.
/// 코드에 숫자로 박아두면 한 번 고칠 때마다 컴파일을 기다려야 한다.
/// </summary>
public class DayLighting : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("시간을 읽어올 시계. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private DayClock clock;

    [Tooltip("바깥 해. 비워두면 씬의 Directional Light를 찾습니다.")]
    [SerializeField] private Light sun;

    [Tooltip("저녁에 켜질 실내등들. 원래 세기에 곱하는 방식이라 씬 값은 안 바뀝니다.")]
    [SerializeField] private Light[] interiorLights;

    [Header("해의 높이")]
    [Tooltip("영업 시작 때의 고도각. 0이 지평선, 90이 머리 위, 180이 반대쪽 지평선입니다.")]
    [SerializeField] private float startPitch = 45f;

    [Tooltip("영업 종료 때의 고도각. 180을 넘으면 해가 지평선 아래로 내려갑니다.")]
    [SerializeField] private float endPitch = 200f;

    [Header("해의 색과 세기")]
    [Tooltip("시간에 따른 햇빛 색. 0이 아침, 1이 밤입니다.")]
    [SerializeField] private Gradient sunColor = DefaultSunColor();

    [Tooltip("시간에 따른 햇빛 세기 배율.")]
    [SerializeField] private AnimationCurve sunIntensity = DefaultSunIntensity();

    [Tooltip("한낮 기준 햇빛 세기. 위 곡선에 곱합니다.")]
    [SerializeField] private float sunMaxIntensity = 1f;

    [Header("실내등")]
    [Tooltip("시간에 따라 각 실내등의 원래 세기에 곱할 배율.")]
    [SerializeField] private AnimationCurve interiorIntensity = DefaultInteriorIntensity();

    [Header("앰비언트")]
    [Tooltip("하늘에서 실내 전체 톤을 다시 굽는 횟수 (초당). 매 프레임 하면 프레임이 떨어집니다.")]
    [SerializeField] private float ambientUpdatesPerSecond = 4f;

    private float[] _interiorBase;
    private float _ambientTimer;
    private bool _subscribed;

    private void Awake()
    {
        if (clock == null)
        {
            clock = FindFirstObjectByType<DayClock>();
        }

        if (sun == null)
        {
            sun = RenderSettings.sun;
        }

        if (clock == null || sun == null)
        {
            Debug.LogError($"{nameof(DayLighting)} on '{name}': 시계와 해가 모두 필요합니다.", this);
            enabled = false;
            return;
        }

        CacheInteriorBase();
    }

    /// <summary>
    /// 실내등의 원래 세기를 기억해둔다. 매번 배율을 곱하면 값이 계속 줄어들어
    /// 몇 초 만에 0이 된다.
    /// </summary>
    private void CacheInteriorBase()
    {
        if (interiorLights == null)
        {
            _interiorBase = new float[0];
            return;
        }

        _interiorBase = new float[interiorLights.Length];
        for (int i = 0; i < interiorLights.Length; i++)
        {
            _interiorBase[i] = interiorLights[i] != null ? interiorLights[i].intensity : 0f;
        }
    }

    private void OnEnable()
    {
        if (clock != null && !_subscribed)
        {
            clock.Ticked += Apply;
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (clock != null && _subscribed)
        {
            clock.Ticked -= Apply;
            _subscribed = false;
        }
    }

    private void Start()
    {
        // 시계는 셔터를 올려야 흐르기 시작한다. 그 전에도 아침으로는 보여야 하니
        // 한 번 찍어둔다.
        Apply(clock != null ? clock.Progress01 : 0f);
        UpdateAmbient();
    }

    /// <summary>진행률 0~1을 조명에 반영한다. 시계가 매 프레임 부른다.</summary>
    public void Apply(float progress01)
    {
        float t = Mathf.Clamp01(progress01);

        Vector3 angles = sun.transform.eulerAngles;
        sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(startPitch, endPitch, t), angles.y, angles.z);

        sun.color = sunColor.Evaluate(t);
        sun.intensity = Mathf.Max(0f, sunIntensity.Evaluate(t) * sunMaxIntensity);

        float indoor = Mathf.Max(0f, interiorIntensity.Evaluate(t));
        for (int i = 0; i < _interiorBase.Length; i++)
        {
            if (interiorLights[i] != null)
            {
                interiorLights[i].intensity = _interiorBase[i] * indoor;
            }
        }

        TickAmbient();
    }

    /// <summary>
    /// 하늘이 바뀌어도 실내에 깔리는 간접광은 따로 구워야 따라온다. 그런데 이게 무거워서
    /// 매 프레임 부르면 눈에 띄게 느려진다. 5분에 걸친 변화라 초당 몇 번이면 계단이 안 보인다.
    /// </summary>
    private void TickAmbient()
    {
        if (ambientUpdatesPerSecond <= 0f)
        {
            return;
        }

        _ambientTimer += Time.deltaTime;

        float step = 1f / ambientUpdatesPerSecond;
        if (_ambientTimer < step)
        {
            return;
        }

        _ambientTimer = 0f;
        UpdateAmbient();
    }

    private void UpdateAmbient()
    {
        if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Skybox)
        {
            DynamicGI.UpdateEnvironment();
        }
    }

    // ---------------------------------------------------------------- 기본값

    private static Gradient DefaultSunColor()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1.00f, 0.95f, 0.85f), 0.00f), // 아침
                new GradientColorKey(new Color(1.00f, 0.98f, 0.92f), 0.40f), // 한낮
                new GradientColorKey(new Color(1.00f, 0.85f, 0.62f), 0.66f), // 늦은 오후
                new GradientColorKey(new Color(1.00f, 0.50f, 0.20f), 0.78f), // 노을 (18시 무렵)
                new GradientColorKey(new Color(0.32f, 0.28f, 0.48f), 0.90f), // 땅거미
                new GradientColorKey(new Color(0.10f, 0.12f, 0.26f), 1.00f), // 밤
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

        return gradient;
    }

    private static AnimationCurve DefaultSunIntensity()
    {
        return new AnimationCurve(
            new Keyframe(0.00f, 0.85f),
            new Keyframe(0.40f, 1.00f),
            new Keyframe(0.66f, 0.85f),
            new Keyframe(0.78f, 0.55f),
            new Keyframe(0.92f, 0.00f),
            new Keyframe(1.00f, 0.00f));
    }

    private static AnimationCurve DefaultInteriorIntensity()
    {
        return new AnimationCurve(
            new Keyframe(0.00f, 0.50f),
            new Keyframe(0.60f, 0.55f),
            new Keyframe(0.78f, 0.80f),
            new Keyframe(0.92f, 1.00f),
            new Keyframe(1.00f, 1.00f));
    }

    private void OnValidate()
    {
        sunMaxIntensity = Mathf.Max(0f, sunMaxIntensity);
        ambientUpdatesPerSecond = Mathf.Max(0f, ambientUpdatesPerSecond);
    }
}
