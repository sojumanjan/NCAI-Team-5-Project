using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 고정된 웨이포인트 루프를 계속 순환한다 (4구역 순찰형 고스트가 사용).
/// </summary>
public class GhostPatrolLoop : MonoBehaviour, IGhostMovementSource
{
    [SerializeField] private Transform[] waypoints;

    private int currentIndex;

    private void Reset()
    {
        waypoints = new Transform[0];
    }

    public void BeginPatrol(NavMeshAgent agent)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        // 재시작 시 죽었던 자리가 아니라 원래 순찰 시작 지점으로 되돌아가야 하므로 워프한다.
        currentIndex = 0;
        agent.Warp(waypoints[currentIndex].position);
        agent.SetDestination(waypoints[currentIndex].position);
    }

    public void TickPatrol(NavMeshAgent agent)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentIndex].position);
        }
    }

    public void ResumePatrolFromCurrentPosition(NavMeshAgent agent)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(agent.transform.position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentIndex = closestIndex;
        agent.SetDestination(waypoints[currentIndex].position);
    }
}
