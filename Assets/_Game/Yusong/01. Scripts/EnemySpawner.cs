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
    [SerializeField] private float wave1SpeedReduction = 20f;

    [Header("Yellow Pacing")]
    [SerializeField] private int yellowPairSize = 2;
    [SerializeField] private float yellowSlotJitterFraction = 0.25f;
    [SerializeField] private float yellowPairSeparation = 220f;
    [SerializeField] private float yellowCrowdMixJitter = 100f;
    [SerializeField] private float densityClusterRadius = 200f;

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
            countdownTimer.WaveEnding += HandleWaveEnding;
        }
    }

    private void OnDisable()
    {
        if (countdownTimer != null)
        {
            countdownTimer.WaveStarted -= HandleWaveStarted;
            countdownTimer.WaveEnding -= HandleWaveEnding;
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void HandleWaveEnding()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.GetComponent<EnemyHealth>() != null)
            {
                Destroy(child.gameObject);
            }
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
        Vector2 lastNormalSpawnPosition = Vector2.zero;
        bool hasNormalSpawnPosition = false;

        foreach (var evt in events)
        {
            float wait = evt.time - elapsed;
            if (wait > 0f) yield return new WaitForSeconds(wait);
            elapsed = evt.time;

            if (evt.isYellowPair)
            {
                // Spawn from the perimeter like every other enemy (so it's visible immediately
                // instead of appearing already overlapped by the crowd), but aim it toward
                // whichever direction currently has the densest cluster of enemies.
                Vector2 anchor;
                float densestAngle;
                if (TryFindDensestDirection(parent, out densestAngle))
                {
                    anchor = PerimeterPointAtAngle(rect, densestAngle);
                }
                else
                {
                    anchor = hasNormalSpawnPosition
                        ? lastNormalSpawnPosition + UnityEngine.Random.insideUnitCircle * yellowCrowdMixJitter
                        : RandomPerimeterPoint(rect);
                }

                if (evt.prefabs.Count >= 2)
                {
                    // Spread the pair apart (up to just under the explosion radius) instead of
                    // stacking them, so a chain reaction sweeps a wider area rather than one spot.
                    Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                    for (int k = 0; k < evt.prefabs.Count; k++)
                    {
                        var prefab = evt.prefabs[k];
                        if (prefab == null) continue;
                        Vector2 offset = dir * (yellowPairSeparation * 0.5f) * (k == 0 ? 1f : -1f);
                        var enemy = SpawnEnemy(prefab, parent, wave);
                        enemy.anchoredPosition = anchor + offset;
                    }
                }
                else
                {
                    foreach (var prefab in evt.prefabs)
                    {
                        if (prefab == null) continue;
                        var enemy = SpawnEnemy(prefab, parent, wave);
                        enemy.anchoredPosition = anchor + UnityEngine.Random.insideUnitCircle * (yellowPairSeparation * 0.25f);
                    }
                }
            }
            else
            {
                foreach (var prefab in evt.prefabs)
                {
                    if (prefab == null) continue;
                    var enemy = SpawnEnemy(prefab, parent, wave);
                    Vector2 spawnPos = RandomPerimeterPoint(rect);
                    enemy.anchoredPosition = spawnPos;
                    lastNormalSpawnPosition = spawnPos;
                    hasNormalSpawnPosition = true;
                }
            }
        }

        spawnRoutine = null;
    }

    private RectTransform SpawnEnemy(RectTransform prefab, RectTransform parent, int wave)
    {
        var enemy = Instantiate(prefab, parent);

        if (wave == 1 && wave1SpeedReduction > 0f)
        {
            var mover = enemy.GetComponent<EnemyMover>();
            if (mover != null) mover.AdjustSpeed(-wave1SpeedReduction);
        }

        return enemy;
    }

    private bool TryFindDensestDirection(RectTransform parent, out float angle)
    {
        angle = 0f;
        int bestScore = -1;
        bool found = false;

        for (int i = 0; i < parent.childCount; i++)
        {
            var candidate = parent.GetChild(i);
            if (candidate.GetComponent<EnemyMover>() == null || candidate.name.Contains("Boss")) continue;

            Vector2 candidatePos = candidate.GetComponent<RectTransform>().anchoredPosition;
            int score = 0;

            for (int j = 0; j < parent.childCount; j++)
            {
                var other = parent.GetChild(j);
                if (other.GetComponent<EnemyMover>() == null || other.name.Contains("Boss")) continue;

                Vector2 otherPos = other.GetComponent<RectTransform>().anchoredPosition;
                if (Vector2.Distance(candidatePos, otherPos) <= densityClusterRadius) score++;
            }

            if (score > bestScore)
            {
                bestScore = score;
                angle = Mathf.Atan2(candidatePos.y, candidatePos.x);
                found = true;
            }
        }

        return found;
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
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        return PerimeterPointAtAngle(rect, angle);
    }

    private Vector2 PerimeterPointAtAngle(Rect rect, float angle)
    {
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float tx = Mathf.Approximately(cos, 0f) ? float.MaxValue : halfWidth / Mathf.Abs(cos);
        float ty = Mathf.Approximately(sin, 0f) ? float.MaxValue : halfHeight / Mathf.Abs(sin);
        float t = Mathf.Min(tx, ty) + spawnOffset;

        return new Vector2(cos * t, sin * t);
    }
}
