using UnityEngine;

/// <summary>
/// 영업시간이 흐르는 만큼 해를 돌리고, 저녁 한때만 노을색을 입힌다.
///
/// 각도를 시작과 끝으로 적지 않고 <b>아무 두 시각의 각도</b>로 받는다. 그렇게 잡는 게
/// 실제로 하는 일이기 때문이다 — 씬 뷰에서 해를 돌려 "이쯤이 저녁"을 찾아낸 다음 그 값을
/// 적으면, 나머지 시간대는 알아서 앞뒤로 늘어난다. 9시 각도를 손으로 역산할 일이 없다.
/// </summary>
public class DayLighting : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("시간을 읽어올 시계. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private DayClock clock;

    [Tooltip("돌릴 해. 비워두면 씬의 Directional Light를 찾습니다.")]
    [SerializeField] private Light sun;

    [Header("기준 각도")]
    [Tooltip("첫 번째로 잡아둔 시각.")]
    [SerializeField] private float firstHour = 18f;

    [Tooltip("그 시각의 해 각도.")]
    [SerializeField] private Vector3 firstAngles = new Vector3(8.572f, -20.771f, -8.839f);

    [Tooltip("두 번째로 잡아둔 시각.")]
    [SerializeField] private float secondHour = 20f;

    [Tooltip("그 시각의 해 각도.")]
    [SerializeField] private Vector3 secondAngles = new Vector3(-19.208f, -16.373f, -9.259f);

    [Header("노을")]
    [Tooltip("끄면 색을 전혀 건드리지 않습니다. 씬에 잡아둔 해 색이 그대로 유지됩니다.")]
    [SerializeField] private bool tintSunset = true;

    [Tooltip("노을이 가장 짙을 때의 색.")]
    [SerializeField] private Color sunsetColor = new Color32(255, 177, 71, 255);

    [Tooltip("물들기 시작하는 시각.")]
    [SerializeField] private float sunsetStartHour = 18f;

    [Tooltip("가장 짙어지는 시각.")]
    [SerializeField] private float sunsetPeakHour = 19f;

    [Tooltip("원래 색으로 다 돌아오는 시각.")]
    [SerializeField] private float sunsetEndHour = 20f;

    [Header("미리보기")]
    [Tooltip("플레이하지 않고 확인할 시각. 톱니바퀴 메뉴에서 적용합니다.")]
    [SerializeField] private float previewHour = 9f;

    private bool _subscribed;

    // 기준 색은 씬에서 읽는다. 코드에 흰색을 박아두면 아티스트가 해 색을 바꾼 순간
    // 노을이 끝날 때마다 그 흰색으로 덮여버린다.
    private Color _baseColor = Color.white;
    private bool _baseCaptured;

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

        CaptureBase();
    }

    private void CaptureBase()
    {
        if (!_baseCaptured && sun != null)
        {
            _baseColor = sun.color;
            _baseCaptured = true;
        }
    }

    private void OnEnable()
    {
        if (clock != null && !_subscribed)
        {
            clock.Ticked += ApplyProgress;
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (clock != null && _subscribed)
        {
            clock.Ticked -= ApplyProgress;
            _subscribed = false;
        }
    }

    private void Start()
    {
        // 시계는 셔터를 올려야 흐르기 시작한다. 그 전에도 아침 각도로는 서 있어야 한다.
        ApplyProgress(clock != null ? clock.Progress01 : 0f);
    }

    /// <summary>시계가 매 프레임 부른다.</summary>
    private void ApplyProgress(float progress01)
    {
        ApplyHour(clock.CurrentHourFloat);
    }

    /// <summary>그 시각의 각도와 색으로 해를 세운다.</summary>
    public void ApplyHour(float hour)
    {
        if (sun == null)
        {
            return;
        }

        CaptureBase();

        sun.transform.rotation = Quaternion.Euler(AnglesAt(hour));

        if (tintSunset)
        {
            sun.color = ColorAt(hour);
        }
    }

    /// <summary>
    /// 그 시각의 해 색. 노을 구간 밖에서는 씬에 잡아둔 색 그대로다.
    ///
    /// 물들었다가 되돌아오는 산 모양으로 간다. 한 번 물든 채로 끝내지 않는 이유는,
    /// 노을이 짙은 건 해가 지평선에 걸린 잠깐뿐이고 그 뒤로는 그냥 어두워지기 때문이다.
    /// </summary>
    public Color ColorAt(float hour)
    {
        if (hour <= sunsetStartHour || hour >= sunsetEndHour)
        {
            return _baseColor;
        }

        if (hour < sunsetPeakHour)
        {
            float up = Mathf.InverseLerp(sunsetStartHour, sunsetPeakHour, hour);
            return Color.Lerp(_baseColor, sunsetColor, up);
        }

        float down = Mathf.InverseLerp(sunsetPeakHour, sunsetEndHour, hour);
        return Color.Lerp(sunsetColor, _baseColor, down);
    }

    /// <summary>
    /// 두 기준점을 잇는 직선 위의 각도. 기준점 바깥이면 그대로 늘려 쓴다 —
    /// 18시와 20시만 잡아줘도 9시가 나오는 건 그래서다.
    /// </summary>
    public Vector3 AnglesAt(float hour)
    {
        float span = secondHour - firstHour;
        if (Mathf.Approximately(span, 0f))
        {
            return firstAngles;
        }

        return firstAngles + (secondAngles - firstAngles) * ((hour - firstHour) / span);
    }

    [ContextMenu("미리보기 시각으로 해 돌리기")]
    private void PreviewNow()
    {
        if (sun == null)
        {
            sun = RenderSettings.sun;
        }

        ApplyHour(previewHour);
    }

    [ContextMenu("기준 색을 지금 해 색으로 다시 잡기")]
    private void RecaptureBase()
    {
        _baseCaptured = false;
        CaptureBase();
        Debug.Log($"{name} 기준 해 색 = #{ColorUtility.ToHtmlStringRGB(_baseColor)}", this);
    }

    private void OnValidate()
    {
        sunsetPeakHour = Mathf.Max(sunsetStartHour, sunsetPeakHour);
        sunsetEndHour = Mathf.Max(sunsetPeakHour, sunsetEndHour);
    }

    [ContextMenu("시간대별 각도 찍어보기")]
    private void LogAngles()
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine($"{name} 시간대별 해 각도");

        for (float h = 9f; h <= 21f; h += 1f)
        {
            Vector3 angles = AnglesAt(h);
            Vector3 forward = Quaternion.Euler(angles) * Vector3.forward;
            Color c = ColorAt(h);
            text.AppendLine($"{h:00}:00  {angles:F2}  높이 {-forward.y:F2}  색 #{ColorUtility.ToHtmlStringRGB(c)}{(forward.y >= 0f ? "  지평선 아래" : string.Empty)}");
        }

        Debug.Log(text.ToString(), this);
    }
}
