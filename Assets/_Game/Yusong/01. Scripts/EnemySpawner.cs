using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yusong
{
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
    [SerializeField] private float wave2PlusSpeedReduction = 10f;

    [Header("Wave 0 Tutorial")]
    [SerializeField] private RectTransform wave0PracticeEnemyPrefab;
    [SerializeField] private RectTransform wave0YellowTrianglePrefab;
    [SerializeField] private RectTransform wave0RedTrianglePrefab;
    [SerializeField] private RectTransform wave0BlackSquarePrefab;
    [SerializeField] private RectTransform[] wave0FeverPrefabs;
    [SerializeField] private int wave0FeverClusterSize = 4;
    [SerializeField] private float wave0FeverClusterSpread = 80f;
    [SerializeField] private float wave0FeverClusterInterval = 1.2f;

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
        if (wave == 0)
        {
            SpawnWave0PracticeEnemy();
            return;
        }

        if (wave > lastConfiguredWave) return;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnOverTime(wave));
    }

    private void SpawnWave0PracticeEnemy()
    {
        if (wave0PracticeEnemyPrefab == null) return;

        var parent = (RectTransform)transform;
        Rect rect = parent.rect;

        // Fixed spawn point and approach direction (straight in from the left) so the
        // tutorial beat below can reliably tell when the enemy has fully entered view.
        var enemy = SpawnEnemy(wave0PracticeEnemyPrefab, parent, 0);
        enemy.anchoredPosition = PerimeterPointAtAngle(rect, Mathf.PI);

        StartCoroutine(WatchWave0EnemyBecomeVisible(enemy, rect));
    }

    private IEnumerator WatchWave0EnemyBecomeVisible(RectTransform enemy, Rect rect)
    {
        float visibleThresholdX = rect.xMin + enemy.sizeDelta.x * 0.5f;

        while (enemy != null && enemy.anchoredPosition.x < visibleThresholdX)
        {
            yield return null;
        }

        if (enemy == null) yield break;

        if (enemy.GetComponent<ClickInviteEffect>() == null)
        {
            enemy.gameObject.AddComponent<ClickInviteEffect>();
        }
        if (countdownTimer != null) countdownTimer.PauseForEnemyHighlight(enemy);

        // Wait for the player to actually destroy this enemy, then call out the combo gauge.
        while (enemy != null)
        {
            yield return null;
        }

        if (countdownTimer != null)
        {
            countdownTimer.PauseForComboGaugeHighlight("적군을 제거하면 콤보게이지를 얻을 수 있습니다!");
        }

        // Wait for that pause to be dismissed before starting the next tutorial beat.
        while (countdownTimer != null && countdownTimer.IsWaitingForIntroClick)
        {
            yield return null;
        }

        SpawnWave0Group();
    }

    private void SpawnWave0Group()
    {
        if (wave0YellowTrianglePrefab == null || wave0RedTrianglePrefab == null) return;

        var parent = (RectTransform)transform;
        Rect rect = parent.rect;

        // Fixed spawn point and approach direction (straight in from the right, mirroring the
        // first practice enemy's left-side entry) so all four arrive together and the visibility
        // check below is reliable. The yellow entity spawns at the center of the cluster with the
        // three red ones arranged around it (top-left, top-right, bottom), tight enough that the
        // whole group still fits inside the shared highlight circle once it re-appears.
        Vector2 spawnPoint = PerimeterPointAtAngle(rect, 0f);

        var group = new List<RectTransform>();

        var yellow = SpawnEnemy(wave0YellowTrianglePrefab, parent, 0);
        yellow.anchoredPosition = spawnPoint;
        group.Add(yellow);

        Vector2[] redOffsets = { new Vector2(-55f, 45f), new Vector2(55f, 45f), new Vector2(0f, -60f) };
        foreach (var offset in redOffsets)
        {
            var red = SpawnEnemy(wave0RedTrianglePrefab, parent, 0);
            red.anchoredPosition = spawnPoint + offset;
            group.Add(red);
        }

        StartCoroutine(WatchWave0GroupBecomeVisible(group, yellow, rect));
    }

    private IEnumerator WatchWave0GroupBecomeVisible(List<RectTransform> group, RectTransform yellow, Rect rect)
    {
        while (true)
        {
            bool anyAlive = false;
            bool allVisible = true;

            foreach (var enemy in group)
            {
                if (enemy == null) continue;
                anyAlive = true;

                float visibleThresholdX = rect.xMax - enemy.sizeDelta.x * 0.5f;
                if (enemy.anchoredPosition.x > visibleThresholdX) allVisible = false;
            }

            if (!anyAlive) yield break;
            if (allVisible) break;

            yield return null;
        }

        group.RemoveAll(e => e == null);
        if (group.Count == 0) yield break;

        if (yellow != null && yellow.GetComponent<ClickInviteEffect>() == null)
        {
            yellow.gameObject.AddComponent<ClickInviteEffect>();
        }

        if (countdownTimer != null)
        {
            countdownTimer.PauseForGroupHighlight(group,
                "노란 적군은 다른 적군보다 강하지만, 제거하면 자신의 주변에 데미지를 전이시킵니다");
        }

        // Wait for that pause to be dismissed before starting the next tutorial beat.
        while (countdownTimer != null && countdownTimer.IsWaitingForIntroClick)
        {
            yield return null;
        }

        // Only bring in the black square once the whole group has actually been cleared.
        while (group.Exists(e => e != null))
        {
            yield return null;
        }

        SpawnWave0BlackSquare();
    }

    private void SpawnWave0BlackSquare()
    {
        if (wave0BlackSquarePrefab == null) return;

        var parent = (RectTransform)transform;
        Rect rect = parent.rect;

        // Enters from the opposite side of the group that just arrived (left, mirroring the very
        // first practice enemy's entry) so the tutorial keeps alternating sides.
        var enemy = SpawnEnemy(wave0BlackSquarePrefab, parent, 0);
        enemy.anchoredPosition = PerimeterPointAtAngle(rect, Mathf.PI);

        StartCoroutine(WatchWave0BlackSquareBecomeVisible(enemy, rect));
    }

    private IEnumerator WatchWave0BlackSquareBecomeVisible(RectTransform enemy, Rect rect)
    {
        float visibleThresholdX = rect.xMin + enemy.sizeDelta.x * 0.5f;

        while (enemy != null && enemy.anchoredPosition.x < visibleThresholdX)
        {
            yield return null;
        }

        if (enemy == null) yield break;

        if (countdownTimer != null)
        {
            countdownTimer.PauseForGroupHighlight(new List<RectTransform> { enemy },
                "검은 적군은 다른 적군에 비해 유달리 민첩하지만, 점수와 콤보를 2배로 제공합니다");
        }

        // Wait for that pause to be dismissed, then for the player to actually destroy it,
        // before calling out the fever time mechanic on the combo gauge.
        while (countdownTimer != null && countdownTimer.IsWaitingForIntroClick)
        {
            yield return null;
        }

        while (enemy != null)
        {
            yield return null;
        }

        if (countdownTimer != null)
        {
            countdownTimer.PauseForComboGaugeHighlight(
                "일정 콤보에 도달하면 피버타임에 진입합니다\n피버타임에는 점수 보너스와 공격 전이가 적용됩니다!");
        }

        // Wait for that pause to be dismissed, then force fever time right away (instead of
        // waiting for a real combo streak) and keep feeding the player enemies to click through
        // it for however long is left in Wave 0.
        while (countdownTimer != null && countdownTimer.IsWaitingForIntroClick)
        {
            yield return null;
        }

        if (ComboManager.Instance != null) ComboManager.Instance.ForceActivateAoe();

        StartCoroutine(SpawnWave0FeverFiller());
    }

    private IEnumerator SpawnWave0FeverFiller()
    {
        if (wave0FeverPrefabs == null || wave0FeverPrefabs.Length == 0) yield break;

        var parent = (RectTransform)transform;

        // Small tight clusters (well within the fever AOE radius) so a single click during
        // fever chain-clears the whole group, the way fever time is meant to feel. Each cluster
        // mixes shapes/colors (normal, black, yellow) instead of a single repeated enemy type.
        while (CountdownTimer.IsWaveActive)
        {
            Vector2 anchor = RandomPerimeterPoint(parent.rect);

            for (int i = 0; i < wave0FeverClusterSize; i++)
            {
                var prefab = wave0FeverPrefabs[UnityEngine.Random.Range(0, wave0FeverPrefabs.Length)];
                if (prefab == null) continue;

                var enemy = SpawnEnemy(prefab, parent, 0);
                enemy.anchoredPosition = anchor + UnityEngine.Random.insideUnitCircle * wave0FeverClusterSpread;
            }

            yield return new WaitForSeconds(wave0FeverClusterInterval);
        }
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

        float reduction = wave == 1 ? wave1SpeedReduction : wave2PlusSpeedReduction;
        if (reduction > 0f)
        {
            var mover = enemy.GetComponent<EnemyMover>();
            if (mover != null) mover.AdjustSpeed(-reduction);
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
}
