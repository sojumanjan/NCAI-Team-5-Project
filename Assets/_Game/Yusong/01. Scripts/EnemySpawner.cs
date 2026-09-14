using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private struct EnemyEntry
    {
        public RectTransform prefab;
        public int count;
    }

    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private EnemyEntry[] enemyTypes;
    [SerializeField] private float spawnOffset = 60f;
    [SerializeField] private float spawnDuration = 60f;

    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        if (countdownTimer != null)
        {
            countdownTimer.WaveStarted += HandleWaveStarted;
        }
    }

    private void OnDisable()
    {
        if (countdownTimer != null)
        {
            countdownTimer.WaveStarted -= HandleWaveStarted;
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void HandleWaveStarted(int wave)
    {
        if (wave != 1) return;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnOverTime());
    }

    private IEnumerator SpawnOverTime()
    {
        var queue = new List<RectTransform>();
        foreach (var entry in enemyTypes)
        {
            for (int i = 0; i < entry.count; i++)
            {
                queue.Add(entry.prefab);
            }
        }

        for (int i = queue.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = queue[i];
            queue[i] = queue[j];
            queue[j] = temp;
        }

        if (queue.Count == 0) yield break;

        float interval = spawnDuration / queue.Count;
        var parent = (RectTransform)transform;
        Rect rect = parent.rect;

        foreach (var prefab in queue)
        {
            if (prefab != null)
            {
                var enemy = Instantiate(prefab, parent);
                enemy.anchoredPosition = RandomPerimeterPoint(rect);
            }

            yield return new WaitForSeconds(interval);
        }

        spawnRoutine = null;
    }

    private Vector2 RandomPerimeterPoint(Rect rect)
    {
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float tx = Mathf.Approximately(cos, 0f) ? float.MaxValue : halfWidth / Mathf.Abs(cos);
        float ty = Mathf.Approximately(sin, 0f) ? float.MaxValue : halfHeight / Mathf.Abs(sin);
        float t = Mathf.Min(tx, ty) + spawnOffset;

        return new Vector2(cos * t, sin * t);
    }
}
