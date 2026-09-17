using UnityEngine.AI;

/// <summary>
/// Ghost가 순찰 이동을 위임하는 공통 인터페이스.
/// GhostPatrolLoop(구역 루프)와 GhostPingPong(통로 왕복)이 각각 구현한다.
/// </summary>
public interface IGhostMovementSource
{
    /// <summary>순찰 시작 시(또는 씬 시작 시) 최초 목적지를 설정한다.</summary>
    void BeginPatrol(NavMeshAgent agent);

    /// <summary>매 프레임 호출되어, 목적지 도달 시 다음 목적지로 갱신한다.</summary>
    void TickPatrol(NavMeshAgent agent);

    /// <summary>추격이 끝난 뒤, 현재 위치에서 가장 가까운 경로 지점부터 순찰을 재개한다.</summary>
    void ResumePatrolFromCurrentPosition(NavMeshAgent agent);
}
