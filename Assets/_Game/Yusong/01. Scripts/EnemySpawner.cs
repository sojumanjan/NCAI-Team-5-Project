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
    [SerializeField] private EnemyEntry[] extraEnemyTypesFromWave2;
    [SerializeField] private float spawnOffset = 60f;
    [SerializeField] private float spawnDuration = 60f;

    [Header("Yellow Pacing")]
    [SerializeField] private int yellowPairSize = 2;
    [SerializeField] private float yellowSlotJitterFraction = 0.25f;
    [SerializeField] private float yellowPairSpread = 50f;

    private struct SpawnEvent
    {
        public float time;
        public List<RectTransform> prefabs;
        public bool isYellowPair;
    }

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

    [SerializeField] private int lastConfiguredWave = 2;

    private void HandleWaveStarted(int wave)
    {
        if (wave > lastConfiguredWave) return;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnOverTime(wave));
    }

    private IEnumerator SpawnOverTime(int wave)
    {
        var normalQueue = new List<RectTransform>();
        var yellowQueue = new List<RectTransform>();

        CollectEntries(enemyTypes, normalQueue, yellowQueue);
        if (wave >= 2 && extraEnemyTypesFromWave2 != null)
        {
            CollectEntries(extraEnemyTypesFromWave2, normalQueue, yellowQueue);
        }

        Shuffle(normalQueue);
        Shuffle(yellowQueue);

        var events = new List<SpawnEvent>();

        if (normalQueue.Count > 0)
        {
            float interval = spawnDuration / normalQueue.Count;
            for (int i = 0; i < normalQueue.Count; i++)
            {
                events.Add(new SpawnEvent
                {
                    time = interval * i,
                    prefabs = new List<RectTransform> { normalQueue[i] },
                    isYellowPair = false
                });
            }
        }

        if (yellowQueue.Count > 0)
        {
            int slotCount = Mathf.CeilToInt((float)yellowQueue.Count / yellowPairSize);
            float slotInterval = spawnDuration / slotCount;
            int idx = 0;

            for (int s = 0; s < slotCount; s++)
            {
                var group = new List<RectTransform>();
                for (int p = 0; p < yellowPairSize && idx < yellowQueue.Count; p++, idx++)
                {
                    group.Add(yellowQueue[idx]);
                }

                float jitter = UnityEngine.Random.Range(-yellowSlotJitterFraction, yellowSlotJitterFraction) * slotInterval;
                float t = Mathf.Clamp(slotInterval * (s + 0.5f) + jitter, 0f, spawnDuration - 0.01f);

                events.Add(new SpawnEvent
                {
                    time = t,
                    prefabs = group,
                    isYellowPair = true
                });
            }
        }

        if (events.Count == 0) yield break;

        events.Sort((a, b) => a.time.CompareTo(b.time));

        var parent = (RectTransform)transform;
        Rect rect = parent.rect;
        float elapsed = 0f;

        foreach (var evt in events)
        {
            float wait = evt.time - elapsed;
            if (wait > 0f) yield return new WaitForSeconds(wait);
            elapsed = evt.time;

            if (evt.isYellowPair)
            {
                Vector2 basePos = RandomPerimeterPoint(rect);
                foreach (var prefab in evt.prefabs)
                {
                    if (prefab == null) continue;
                    var enemy = Instantiate(prefab, parent);
                    enemy.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * yellowPairSpread;
                }
            }
            else
            {
                foreach (var prefab in evt.prefabs)
                {
                    if (prefab == null) continue;
                    var enemy = Instantiate(prefab, parent);
                    enemy.anchoredPosition = RandomPerimeterPoint(rect);
                }
            }
        }

        spawnRoutine = null;
    }

    private static void CollectEntries(EnemyEntry[] entries, List<RectTransform> normalQueue, List<RectTransform> yellowQueue)
    {
        foreach (var entry in entries)
        {
            bool isYellow = entry.prefab != null && entry.prefab.name.Contains("Yellow");

            for (int i = 0; i < entry.count; i++)
            {
                (isYellow ? yellowQueue : normalQueue).Add(entry.prefab);
            }
        }
    }

    private static void Shuffle(List<RectTransform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
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
