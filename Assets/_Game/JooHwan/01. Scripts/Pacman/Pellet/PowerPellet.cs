using UnityEngine;

/// <summary>
/// 맵에 배치되는 파워펠릿. 플레이어가 닿으면 습득되어 사라지고,
/// PelletSpawner에게 습득 사실을 알린다 (전부 습득되면 리젠 판단용).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerPellet : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        var thrower = other.GetComponent<PelletThrower>();
        if (thrower == null || !thrower.TryPickup())
        {
            return;
        }

        PelletSpawner.Instance.OnPelletPickedUp(this);
        gameObject.SetActive(false);
    }
}
