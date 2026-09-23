using UnityEngine;

/// <summary>
/// 맵에 배치되는 파워펠릿. 플레이어가 닿으면 습득되어 사라지고,
/// PelletSpawner에게 습득 사실을 알린다 (전부 습득되면 리젠 판단용).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerPellet : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [Tooltip("바닥에 놓인 상태에서의 Y축 회전 속도(도/초). 습득/투척 후에는 이 오브젝트가 비활성화되므로 자동으로 멈춘다.")]
    [SerializeField] private float rotationSpeed = 90f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

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

        PelletSpawner.Instance.HandlePelletPickedUp(this);
        gameObject.SetActive(false);
    }
}
