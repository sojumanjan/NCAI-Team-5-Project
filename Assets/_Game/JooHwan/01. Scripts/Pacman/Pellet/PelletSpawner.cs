using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵에 배치된 파워펠릿(4모서리 + 중앙, 총 5개)을 관리한다.
/// 전부 습득되어 0개가 되면 전체를 리젠한다.
/// </summary>
public class PelletSpawner : MonoBehaviour
{
    public static PelletSpawner Instance { get; private set; }

    [SerializeField] private List<PowerPellet> pellets = new List<PowerPellet>();

    private int remainingCount;

    private void Awake()
    {
        Instance = this;
        remainingCount = pellets.Count;
    }

    public void HandlePelletPickedUp(PowerPellet pellet)
    {
        remainingCount--;

        if (remainingCount <= 0)
        {
            RegenerateAll();
        }
    }

    /// <summary>팩맨 재시작(사망 후 재시작 등) 시 맵의 모든 파워펠릿을 원래 상태로 되돌린다.</summary>
    public void RegenerateAll()
    {
        foreach (var pellet in pellets)
        {
            pellet.gameObject.SetActive(true);
        }

        remainingCount = pellets.Count;
    }
}
