using UnityEngine;

/// <summary>
/// 테트리스 EXIT 지점(문 구멍 앞)에 배치되는 트리거.
/// 플레이어가 닿으면 TetrisGameManager에 클리어를 알린다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TetrisExitTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private TetrisGameManager tetrisGameManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        var playerController = other.GetComponent<PlayerController>();
        tetrisGameManager.HandleExitReached(playerController);
    }
}
