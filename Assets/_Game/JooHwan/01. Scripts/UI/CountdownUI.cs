using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text countdownText;
    [SerializeField] private float secondsPerCount = 1f;
    [Tooltip("3,2,1과 시작 신호음이 전부 하나로 이어져 있는 카운트다운 사운드. 시작 시 한 번만 재생한다.")]
    [SerializeField] private SoundData countdownSound;
    [Tooltip("카운트다운 사운드 전용 재생기. 공용 AudioManager 풀은 재생 중 일시정지를 지원하지 않아서 별도로 둔다.")]
    [SerializeField] private AudioSource countdownAudioSource;

    private bool isPaused;

    public bool IsPlaying => root.activeSelf;

    public void Play(int startFrom, Action onComplete)
    {
        isPaused = false;
        StartCoroutine(CountdownRoutine(startFrom, onComplete));
    }

    private IEnumerator CountdownRoutine(int startFrom, Action onComplete)
    {
        root.SetActive(true);
        PlayCountdownSound();

        for (int i = startFrom; i > 0; i--)
        {
            countdownText.text = i.ToString();

            // WaitForSeconds는 일시정지 도중에도 그냥 흘러가 버리므로, 매 프레임 직접 잔여 시간을
            // 깎아가는 방식으로 바꿔서 isPaused인 동안은 숫자가 멈춰있게 한다.
            float remaining = secondsPerCount;
            while (remaining > 0f)
            {
                if (!isPaused)
                {
                    remaining -= Time.deltaTime;
                }
                yield return null;
            }
        }

        root.SetActive(false);
        onComplete?.Invoke();
    }

    private void PlayCountdownSound()
    {
        if (countdownAudioSource == null || countdownSound == null)
        {
            return;
        }

        AudioClip clip = countdownSound.PickClip();
        if (clip == null)
        {
            return;
        }

        countdownAudioSource.clip = clip;
        countdownAudioSource.volume = countdownSound.Volume;
        countdownAudioSource.pitch = countdownSound.PickPitch();
        countdownAudioSource.Play();
    }

    /// <summary>
    /// ESC 일시정지/재개 시 PauseUI가 호출한다. 카운트다운이 재생 중이 아니어도(이미 끝났어도)
    /// 안전하게 호출할 수 있다 — 그 경우 화면/사운드 모두 건드릴 대상이 없어 아무 일도 없다.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;

        if (paused)
        {
            if (countdownAudioSource != null && countdownAudioSource.isPlaying)
            {
                countdownAudioSource.Pause();
            }
        }
        else
        {
            if (countdownAudioSource != null)
            {
                countdownAudioSource.UnPause();
            }
        }
    }
}
