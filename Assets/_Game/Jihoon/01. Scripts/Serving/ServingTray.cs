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
public class ServingTray : MonoBehaviour, IItemReceiver, IPlacementTarget
{
    [Header("놓이는 위치")]
    [Tooltip("음식이 올라갈 위치. 비워두면 트레이 자신의 위치를 씁니다.")]
    [SerializeField] private Transform dishPoint;

    [Tooltip("그 위치에서 얼마나 띄울지 (m). 트레이 표면에 얹히도록 조절하세요.")]
    [SerializeField] private float dishHeightOffset = 0.05f;

    [Tooltip("음식이 트레이에 남아 있는 시간 (초). 손님이 가져갔다는 연출입니다.")]
    [SerializeField] private float clearDelay = 1.2f;

    private ServingSpot _spot;

    /// <summary>Called by the owning spot during Awake.</summary>
    public void Bind(ServingSpot spot) => _spot = spot;

    public bool CanReceive(ItemData item, PlayerHands hands)
    {
        // 상한 접시는 아예 올리지 못한다. 여기서 막아야 리졸버가 Put 대신 내려놓기로 넘기고
        // 프롬프트도 같이 사라져서, 낼 수 있는 것처럼 보였다가 감점당하는 일이 없다.
        if (IsSpoiled(hands))
        {
            return false;
        }

        return _spot != null && _spot.CanReceiveDish(item);
    }

    private static bool IsSpoiled(PlayerHands hands)
    {
        if (hands == null || hands.HeldObject == null)
        {
            return false;
        }

        PerishableDish perishable = hands.HeldObject.GetComponent<PerishableDish>();
        return perishable != null && perishable.IsSpoiled;
    }

    public void Receive(WorldItem item, PlayerHands hands)
    {
        // The spot does the judging and then calls PlaceAndClear below.
        _spot.DeliverDish(item);
    }

    // ---------------------------------------------------------------- IPlacementTarget

    public bool TryGetPlacement(ItemData item, out Vector3 position, out Quaternion rotation)
    {
        if (_spot == null || !_spot.CanReceiveDish(item))
        {
            position = default;
            rotation = default;
            return false;
        }

        GetDishPose(out position, out rotation);
        return true;
    }

    /// <summary>
    /// The one definition of where a dish sits on this tray. Both the ghost and the real
    /// placement read it, so the preview always lands where the dish will.
    /// </summary>
    private void GetDishPose(out Vector3 position, out Quaternion rotation)
    {
        Transform target = dishPoint != null ? dishPoint : transform;
        position = target.position + Vector3.up * dishHeightOffset;
        rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
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

        dish.SetCarried(true);

        // Deliberately left unparented. The tray is a squashed cube, and no parenting mode
        // survives that: worldPositionStays only preserves scale for the rotation it had at
        // the moment of attachment, so setting the rotation afterwards shears the dish
        // again. The tray never moves, so a plain world placement is the honest fix.
        dish.transform.SetParent(null, true);

        GetDishPose(out Vector3 position, out Quaternion rotation);
        dish.transform.SetPositionAndRotation(position, rotation);

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
