using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 두 지점(통로 양 끝) 사이를 왕복한다 (테트리스-팩맨 연결 통로의 고스트가 사용).
/// 팩맨 진입 전에는 idleWaitPoint에서 플레이어 쪽을 바라보며 대기하다가,
/// 상호작용으로 팩맨이 시작되면 원래 왕복 시작 지점(pointA)으로 옮겨져 순찰을 시작한다.
/// </summary>
public class GhostPingPong : MonoBehaviour, IGhostMovementSource
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("대기 연출 (팩맨 진입 전)")]
    [Tooltip("팩맨 진입 전 이 고스트가 서서 기다릴 위치. 비워두면 대기 연출 없이 pointA에서 바로 시작.")]
    [SerializeField] private Transform idleWaitPoint;
    [Tooltip("대기 중 바라볼 대상(테트리스 쪽, 플레이어가 오는 방향).")]
    [SerializeField] private Transform idleLookTarget;

    private bool isMovingToB = true;

    /// <summary>
    /// 팩맨 진입 전 호출: 대기 지점으로 이동시키고 지정된 방향을 바라보게 한 뒤 완전히 정지시킨다.
    /// </summary>
    public void EnterIdleState(NavMeshAgent agent)
    {
        if (idleWaitPoint == null)
        {
            return;
        }

        agent.Warp(idleWaitPoint.position);

        if (idleLookTarget != null)
        {
            Vector3 lookDir = idleLookTarget.position - idleWaitPoint.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                agent.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    public void BeginPatrol(NavMeshAgent agent)
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        // 대기 지점에 있었더라도, 팩맨이 시작되면 원래 순찰 시작 지점으로 옮겨서 진행한다.
        agent.Warp(pointA.position);

        isMovingToB = true;
        agent.SetDestination(pointB.position);
    }

    public void TickPatrol(NavMeshAgent agent)
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isMovingToB = !isMovingToB;
            agent.SetDestination(isMovingToB ? pointB.position : pointA.position);
        }
    }

    public void ResumePatrolFromCurrentPosition(NavMeshAgent agent)
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        float distToA = Vector3.Distance(agent.transform.position, pointA.position);
        float distToB = Vector3.Distance(agent.transform.position, pointB.position);

        isMovingToB = distToA < distToB;
        agent.SetDestination(isMovingToB ? pointB.position : pointA.position);
    }
}
