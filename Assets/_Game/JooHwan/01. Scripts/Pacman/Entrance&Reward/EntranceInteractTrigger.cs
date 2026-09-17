using UnityEngine;

/// <summary>
/// PacmanEntranceWall의 감지 전용 자식 트리거. 벽의 막는 콜라이더(non-trigger)와
/// 분리해, 상호작용 범위 판정만 담당한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EntranceInteractTrigger : MonoBehaviour
{
    [SerializeField] private PacmanEntranceWall entranceWall;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            entranceWall.NotifyPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            entranceWall.NotifyPlayerExit();
        }
    }
}
