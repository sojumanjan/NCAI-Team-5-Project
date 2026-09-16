using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면에 영업 시각을 띄운다. DayClock을 구독만 하고 시간 계산은 하지 않는다.
/// </summary>
public class DayClockUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("표시할 시계.")]
    [SerializeField] private DayClock clock;

    [Header("표시")]
    [Tooltip("\"09:00\" 이 들어갈 텍스트.")]
    [SerializeField] private TMP_Text timeText;

    [Tooltip("하루 진행 바. Image Type을 Filled로. (선택)")]
    [SerializeField] private Image progressFill;

    [Tooltip("영업 초반 색.")]
    [SerializeField] private Color earlyColor = new Color(0.45f, 0.75f, 1f);

    [Tooltip("마감이 가까울 때 색.")]
    [SerializeField] private Color lateColor = new Color(1f, 0.6f, 0.25f);

    private void Awake()
    {
        if (clock == null)
        {
            Debug.LogError($"{nameof(DayClockUI)}: 시계 참조가 없습니다.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        clock.Ticked += Redraw;
        Redraw(clock.Progress01);
    }

    private void OnDisable() => clock.Ticked -= Redraw;

    private void Redraw(float progress)
    {
        if (timeText != null)
        {
            timeText.text = clock.TimeText;
        }

        if (progressFill == null)
        {
            return;
        }

        progressFill.fillAmount = progress;
        progressFill.color = Color.Lerp(earlyColor, lateColor, progress);
    }
}
