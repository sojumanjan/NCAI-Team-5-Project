using System;
using UnityEngine;

/// <summary>
/// 영업시간만 센다. 09시에서 21시까지를 실시간 몇 분에 눌러 담을지가 전부이고,
/// 승패나 점수는 일부러 모른다 — 그 판단은 MiniGameSession의 몫.
/// </summary>
public class DayClock : MonoBehaviour
{
    [Header("하루 길이")]
    [Tooltip("영업 시작 시각 (시).")]
    [SerializeField] private int startHour = 9;

    [Tooltip("영업 종료 시각 (시).")]
    [SerializeField] private int endHour = 21;

    [Tooltip("이 시간(초) 동안 시작 시각에서 종료 시각까지 흐른다. 300이면 5분.")]
    [SerializeField] private float dayLengthSeconds = 300f;

    private float _elapsed;

    /// <summary>시계가 흐르는 중인지.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>0에서 1. UI 진행 바에 그대로 쓴다.</summary>
    public float Progress01 =>
        dayLengthSeconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / dayLengthSeconds);

    /// <summary>영업 시작부터 흐른 실제 초.</summary>
    public float ElapsedSeconds => _elapsed;

    /// <summary>남은 실제 초.</summary>
    public float RemainingSeconds => Mathf.Max(0f, dayLengthSeconds - _elapsed);

    /// <summary>게임 안 시각. 9.5면 09:30.</summary>
    public float CurrentHourFloat => Mathf.Lerp(startHour, endHour, Progress01);

    /// <summary>게임 안 시각의 시 부분.</summary>
    public int Hour => Mathf.Clamp(Mathf.FloorToInt(CurrentHourFloat), startHour, endHour);

    /// <summary>게임 안 시각의 분 부분.</summary>
    public int Minute
    {
        get
        {
            // 21:00에 정확히 닿았을 때 22:00처럼 넘어가지 않도록 마지막 프레임을 눌러둔다.
            if (Progress01 >= 1f)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.FloorToInt((CurrentHourFloat - Hour) * 60f), 0, 59);
        }
    }

    /// <summary>"09:00" 형태 문자열.</summary>
    public string TimeText => $"{Hour:00}:{Minute:00}";

    /// <summary>매 프레임 진행률(0~1)을 알린다.</summary>
    public event Action<float> Ticked;

    /// <summary>종료 시각에 닿았을 때 한 번.</summary>
    public event Action DayEnded;

    // ---------------------------------------------------------------- 제어

    /// <summary>처음부터 다시 시작한다.</summary>
    public void Begin()
    {
        _elapsed = 0f;
        IsRunning = true;
        Ticked?.Invoke(Progress01);
    }

    /// <summary>흐름만 멈춘다. 경과 시간은 남는다.</summary>
    public void Pause() => IsRunning = false;

    /// <summary>멈춘 지점부터 이어서.</summary>
    public void Resume() => IsRunning = true;

    /// <summary>
    /// 게임 안 시각을 <paramref name="hours"/>시간만큼 앞으로 당긴다. 종료 시각을 넘기면
    /// Update와 똑같은 경로로 하루를 끝내므로, 결과 화면까지 정상적으로 이어진다.
    /// 5분짜리 하루를 매번 다 기다리지 않고 후반부를 확인하려고 둔 구멍이다.
    /// </summary>
    public void SkipHours(float hours)
    {
        if (!IsRunning || hours <= 0f)
        {
            return;
        }

        int span = Mathf.Max(1, endHour - startHour);
        _elapsed += dayLengthSeconds / span * hours;

        if (_elapsed < dayLengthSeconds)
        {
            Ticked?.Invoke(Progress01);
            return;
        }

        _elapsed = dayLengthSeconds;
        IsRunning = false;
        Ticked?.Invoke(Progress01);
        DayEnded?.Invoke();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        Ticked?.Invoke(Progress01);

        if (_elapsed < dayLengthSeconds)
        {
            return;
        }

        _elapsed = dayLengthSeconds;
        IsRunning = false;
        DayEnded?.Invoke();
    }

    private void OnValidate()
    {
        endHour = Mathf.Max(startHour + 1, endHour);
        dayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
    }
}
