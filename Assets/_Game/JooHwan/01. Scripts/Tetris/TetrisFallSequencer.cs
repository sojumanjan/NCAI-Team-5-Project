using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisFallSequencer : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TetrisFallSequence sequence;

    [Header("Spawn")]
    [SerializeField] private Transform arenaOrigin;
    [Tooltip("레인 0 셀 중심의 X좌표 (arenaOrigin 기준 로컬 오프셋, 큐브 피벗이 중심이므로 half-width 보정 포함)")]
    [SerializeField] private float laneOriginX = -9f;
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float spawnHeight = 45f;
    [SerializeField] private float fallSpeed = 3f;
    [SerializeField] private LayerMask landingMask;

    [Header("Timing")]
    [Tooltip("블록이 착지한 후 다음 블록이 생성되기까지의 대기 시간 (초)")]
    [SerializeField] private float delayAfterLanding = 1f;

    private int nextEntryIndex;
    private FallingBlock currentBlock;
    private bool sequenceStarted;
    private bool isPaused;

    private readonly List<GameObject> spawnedBlocks = new List<GameObject>();

    public FallEntry NextEntry => nextEntryIndex < sequence.entries.Count ? sequence.entries[nextEntryIndex] : null;

    private void OnEnable()
    {
        // 카운트다운 등 외부 연출이 끝난 뒤 StartSequence()가 호출될 때까지 대기한다.
        sequenceStarted = false;
        isPaused = false;
        nextEntryIndex = 0;
        currentBlock = null;
    }

    public void StartSequence()
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;
        SpawnNext();
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public void ResetSequence()
    {
        StopAllCoroutines();

        foreach (var block in spawnedBlocks)
        {
            if (block != null)
            {
                Destroy(block);
            }
        }
        spawnedBlocks.Clear();

        nextEntryIndex = 0;
        currentBlock = null;
        isPaused = false;
        sequenceStarted = false;
    }

    private void Update()
    {
        if (!sequenceStarted || isPaused)
        {
            return;
        }

        if (currentBlock != null && currentBlock.IsLanded)
        {
            currentBlock = null;
            StartCoroutine(SpawnNextAfterDelay());
        }
    }

    private IEnumerator SpawnNextAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterLanding);
        SpawnNext();
    }

    private void SpawnNext()
    {
        if (nextEntryIndex >= sequence.entries.Count)
        {
            return;
        }

        FallEntry entry = sequence.entries[nextEntryIndex];
        nextEntryIndex++;

        if (entry.prefab == null)
        {
            Debug.LogWarning($"TetrisFallSequencer: entry index {nextEntryIndex - 1} has no prefab assigned");
            return;
        }

        Vector3 spawnPosition = arenaOrigin.position + new Vector3(laneOriginX + entry.lane * laneWidth, spawnHeight, 0f);
        GameObject instance = Instantiate(entry.prefab, spawnPosition, entry.prefab.transform.rotation, arenaOrigin);
        spawnedBlocks.Add(instance);

        FallingBlock fallingBlock = instance.GetComponent<FallingBlock>();
        if (fallingBlock == null)
        {
            fallingBlock = instance.AddComponent<FallingBlock>();
        }
        fallingBlock.SetFallSpeed(fallSpeed);
        fallingBlock.SetLandingMask(landingMask);

        currentBlock = fallingBlock;
    }
}
