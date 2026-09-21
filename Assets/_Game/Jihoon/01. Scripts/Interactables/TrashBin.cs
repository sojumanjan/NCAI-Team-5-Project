using UnityEngine;

/// <summary>
/// 무엇이든 받아서 없애는 통. 쓰레기통 오브젝트에 붙인다.
///
/// 상한 것만 받게 하지 않는 이유는 규칙이 하나일수록 헷갈리지 않기 때문이다. 잘못 뽑은
/// 재료, 잘못 만든 요리, 손이 꽉 차서 치우고 싶은 것 — 이유를 가릴 필요가 없다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrashBin : MonoBehaviour, IItemReceiver, IReceiverPrompt
{
    [Header("문구")]
    [Tooltip("들고 조준했을 때 뜰 말.")]
    [SerializeField] private string prompt = "버리기";

    [Header("소리")]
    [Tooltip("버릴 때 나는 소리. (선택)")]
    [SerializeField] private SoundData throwSound;

    public string ReceivePrompt => prompt;

    public bool CanReceive(ItemData item, PlayerHands hands) => item != null;

    public void Receive(WorldItem item, PlayerHands hands)
    {
        if (item == null)
        {
            return;
        }

        AudioManager.PlayAt(throwSound, transform.position);
        Destroy(item.gameObject);
    }
}
