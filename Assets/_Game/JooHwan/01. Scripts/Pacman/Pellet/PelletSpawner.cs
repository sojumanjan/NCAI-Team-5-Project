using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 맵 위 파워펠릿을 관리한다. 5개(<see cref="pelletCount"/>)를 항상 유지하며,
/// 하나가 습득되면 그 즉시 파괴하고 새 무작위 위치에 1개를 다시 스폰한다.
/// (기존엔 4모서리+중앙에 고정 배치해두고 5개 다 없어져야 한꺼번에 리젠하는 방식이었으나,
/// 고스트가 적게 남을수록 남은 펠릿을 구하기 어려워지는 문제가 있어 개별 즉시 재스폰으로 변경)
/// </summary>
public class PelletSpawner : MonoBehaviour
{
    public static PelletSpawner Instance { get; private set; }

    [Header("스폰 설정")]
    [SerializeField] private PowerPellet pelletPrefab;
    [SerializeField] private Transform pelletParent;
    [SerializeField] private int pelletCount = 5;
    [Tooltip("바닥 위로 띄우는 높이 (기존 배치 펠릿의 바닥 대비 오프셋과 동일하게 맞춤)")]
    [SerializeField] private float spawnHeight = 1f;

    [Header("위치 판정")]
    [Tooltip("무작위 (x,z) 좌표를 뽑을 범위의 기준이 되는 바닥 콜라이더. 이 Bounds 안에서만 좌표를 뽑는다.")]
    [SerializeField] private Collider floorCollider;
    [Tooltip("뽑은 좌표 주변에서 NavMesh(걸어다닐 수 있는 영역)를 찾는 반경.")]
    [SerializeField] private float navMeshSampleRadius = 2f;
    [Tooltip("이 레이어(벽)와 이 거리 이내면 스폰 위치로 쓰지 않는다 (벽에 파묻히거나 바짝 붙어 보이는 것 방지).")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float wallClearance = 1f;
    [Tooltip("이미 존재하는 다른 파워펠릿과 이 거리 이내면 스폰 위치로 쓰지 않는다 (겹쳐 스폰되는 것 방지).")]
    [SerializeField] private float minDistanceBetweenPellets = 3f;
    [SerializeField] private int maxSpawnAttempts = 30;

    private readonly List<PowerPellet> activePellets = new List<PowerPellet>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RegenerateAll();
    }

    public void HandlePelletPickedUp(PowerPellet pellet)
    {
        activePellets.Remove(pellet);
        Destroy(pellet.gameObject);

        SpawnOne();
    }

    /// <summary>팩맨 재시작(사망 후 재시작 등) 시 현재 펠릿을 전부 없애고 새 위치로 다시 채운다.</summary>
    public void RegenerateAll()
    {
        foreach (var pellet in activePellets)
        {
            if (pellet != null)
            {
                Destroy(pellet.gameObject);
            }
        }
        activePellets.Clear();

        for (int i = 0; i < pelletCount; i++)
        {
            SpawnOne();
        }
    }

    /// <summary>가장 가까운 파워펠릿의 위치. 남은 펠릿이 없으면 null.</summary>
    public Vector3? GetNearestPelletPosition(Vector3 fromPosition)
    {
        bool found = false;
        float nearestSqrDistance = float.MaxValue;
        Vector3 nearestPosition = Vector3.zero;

        foreach (var pellet in activePellets)
        {
            if (pellet == null)
            {
                continue;
            }

            float sqrDistance = (pellet.transform.position - fromPosition).sqrMagnitude;
            if (!found || sqrDistance < nearestSqrDistance)
            {
                found = true;
                nearestSqrDistance = sqrDistance;
                nearestPosition = pellet.transform.position;
            }
        }

        return found ? nearestPosition : (Vector3?)null;
    }

    private void SpawnOne()
    {
        if (!TryFindSpawnPosition(out Vector3 position))
        {
            Debug.LogWarning("PelletSpawner: 유효한 스폰 위치를 찾지 못해 이번 스폰을 건너뜁니다.");
            return;
        }

        PowerPellet spawned = Instantiate(pelletPrefab, position, pelletPrefab.transform.rotation, pelletParent);
        activePellets.Add(spawned);
    }

    private bool TryFindSpawnPosition(out Vector3 result)
    {
        Bounds bounds = floorCollider.bounds;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 samplePoint = new Vector3(randomX, bounds.max.y, randomZ);

            if (!NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                continue;
            }

            Vector3 candidate = hit.position + Vector3.up * spawnHeight;

            if (Physics.CheckSphere(candidate, wallClearance, wallMask))
            {
                continue;
            }

            if (IsTooCloseToExistingPellet(candidate))
            {
                continue;
            }

            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsTooCloseToExistingPellet(Vector3 candidate)
    {
        float minSqrDistance = minDistanceBetweenPellets * minDistanceBetweenPellets;

        foreach (var pellet in activePellets)
        {
            if (pellet == null)
            {
                continue;
            }

            if ((pellet.transform.position - candidate).sqrMagnitude < minSqrDistance)
            {
                return true;
            }
        }

        return false;
    }
}
