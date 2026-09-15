using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text countdownText;
    [SerializeField] private float secondsPerCount = 1f;

    public void Play(int startFrom, Action onComplete)
    {
        StartCoroutine(CountdownRoutine(startFrom, onComplete));
    }

    private IEnumerator CountdownRoutine(int startFrom, Action onComplete)
    {
        root.SetActive(true);

        for (int i = startFrom; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(secondsPerCount);
        }

        root.SetActive(false);
        onComplete?.Invoke();
    }
}
