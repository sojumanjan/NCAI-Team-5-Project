using System.Collections;
using UnityEngine;

/// <summary>
/// The tray for one lane. Left click it while holding a finished dish to serve the
/// customer. Ingredients bounce off, and so does anything offered before the order has
/// been taken at the POS.
///
/// An <see cref="IItemReceiver"/>, because something does leave the player's hands.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ServingTray : MonoBehaviour, IItemReceiver
{
    [Header("놓이는 위치")]
    [Tooltip("음식이 올라갈 위치. 비워두면 트레이 자신의 위치를 씁니다.")]
    [SerializeField] private Transform dishPoint;

    [Tooltip("음식이 트레이에 남아 있는 시간 (초). 손님이 가져갔다는 연출입니다.")]
    [SerializeField] private float clearDelay = 1.2f;

    private ServingSpot _spot;

    /// <summary>Called by the owning spot during Awake.</summary>
    public void Bind(ServingSpot spot) => _spot = spot;

    public bool CanReceive(ItemData item, PlayerHands hands)
    {
        return _spot != null && _spot.CanReceiveDish(item);
    }

    public void Receive(WorldItem item, PlayerHands hands)
    {
        // The spot does the judging and then calls PlaceAndClear below.
        _spot.DeliverDish(item);
    }

    /// <summary>
    /// Parks the served dish on the tray for a moment, then removes it. Called by the spot
    /// so the visual and the scoring stay in step.
    /// </summary>
    public void PlaceAndClear(WorldItem dish)
    {
        if (dish == null)
        {
            return;
        }

        Transform target = dishPoint != null ? dishPoint : transform;
        dish.SetCarried(true);
        dish.transform.SetParent(target, false);
        dish.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        StartCoroutine(ClearAfterDelay(dish.gameObject));
    }

    private IEnumerator ClearAfterDelay(GameObject dish)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, clearDelay));

        if (dish != null)
        {
            Destroy(dish);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = dishPoint != null ? dishPoint : transform;
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireCube(target.position, new Vector3(0.3f, 0.05f, 0.3f));
    }
}
