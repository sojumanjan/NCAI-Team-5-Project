using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisFallSequencer : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("게임이 새로 시작될 때마다(최초 시작/사망 재시작/처음부터) 이 중 하나를 랜덤으로 골라 진행한다.")]
    [SerializeField] private List<TetrisFallSequence> possibleSequences;

    private TetrisFallSequence activeSequence;

    [Header("Spawn")]
    [SerializeField] private Transform arenaOrigin;
    [Tooltip("레인 0 셀 중심의 X좌표 (arenaOrigin 기준 로컬 오프셋, 큐브 피벗이 중심이므로 half-width 보정 포함)")]
    [SerializeField] private float laneOriginX = -9f;
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float spawnHeight = 45f;
    [SerializeField] private float fallSpeed = 3f;
    [SerializeField] private LayerMask landingMask;
    [SerializeField] private SoundData landSound;

    [Header("Timing")]
    [Tooltip("블록이 착지한 후 다음 블록이 생성되기까지의 대기 시간 (초)")]
    [SerializeField] private float delayAfterLanding = 1f;

    private int nextEntryIndex;
    private FallingBlock currentBlock;
    private bool hasSequenceStarted;
    private bool isPaused;

    private readonly List<GameObject> spawnedBlocks = new List<GameObject>();

    public FallEntry NextEntry => activeSequence != null && nextEntryIndex < activeSequence.Entries.Count ? activeSequence.Entries[nextEntryIndex] : null;

    private void OnEnable()
    {
        // 카운트다운 등 외부 연출이 끝난 뒤 StartSequence()가 호출될 때까지 대기한다.
        hasSequenceStarted = false;
        isPaused = false;
        nextEntryIndex = 0;
        currentBlock = null;
    }

    public void StartSequence()
    {
        if (hasSequenceStarted)
        {
            return;
        }

        hasSequenceStarted = true;
        activeSequence = PickRandomSequence();
        SpawnNext();
    }

    private TetrisFallSequence PickRandomSequence()
    {
        if (possibleSequences == null || possibleSequences.Count == 0)
        {
            Debug.LogError("TetrisFallSequencer: possibleSequences가 비어있어 시퀀스를 시작할 수 없습니다.");
            return null;
        }

        return possibleSequences[Random.Range(0, possibleSequences.Count)];
    }

    public void SetPaused(bool isPaused)
    {
        this.isPaused = isPaused;
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
        hasSequenceStarted = false;
    }

    private void Update()
    {
        if (!hasSequenceStarted || isPaused)
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
        if (activeSequence == null || nextEntryIndex >= activeSequence.Entries.Count)
        {
            return;
        }

        FallEntry entry = activeSequence.Entries[nextEntryIndex];
        nextEntryIndex++;

        if (entry.Prefab == null)
        {
            Debug.LogWarning($"TetrisFallSequencer: entry index {nextEntryIndex - 1} has no prefab assigned");
            return;
        }

        Vector3 spawnPosition = arenaOrigin.position + new Vector3(laneOriginX + entry.Lane * laneWidth, spawnHeight, 0f);
        GameObject instance = Instantiate(entry.Prefab, spawnPosition, entry.Prefab.transform.rotation, arenaOrigin);
        spawnedBlocks.Add(instance);

        FallingBlock fallingBlock = instance.GetComponent<FallingBlock>();
        if (fallingBlock == null)
        {
            fallingBlock = instance.AddComponent<FallingBlock>();
        }
        fallingBlock.SetFallSpeed(fallSpeed);
        fallingBlock.SetLandingMask(landingMask);
        fallingBlock.SetLandSound(landSound);

        currentBlock = fallingBlock;
    }
}
