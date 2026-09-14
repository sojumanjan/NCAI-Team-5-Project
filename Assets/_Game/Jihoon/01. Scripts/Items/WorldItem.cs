using UnityEngine;

/// <summary>
/// A physical instance of an <see cref="ItemData"/> sitting in the world. Goes on every
/// item prefab. Picking it up parents it to the player's hand and switches off its
/// physics; dropping it puts the physics back.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IPickable
{
    [Header("아이템")]
    [Tooltip("이 오브젝트가 어떤 아이템인지. 반드시 지정해야 합니다.")]
    [SerializeField] private ItemData item;

    [Header("손에 들었을 때")]
    [Tooltip("손 기준 위치 보정. 프리팹마다 중심이 달라서 미세 조정이 필요합니다.")]
    [SerializeField] private Vector3 heldPositionOffset;

    [Tooltip("손 기준 회전 보정.")]
    [SerializeField] private Vector3 heldRotationOffset;

    private Collider[] _colliders;
    private Rigidbody _rigidbody;

    public ItemData Item => item;

    /// <summary>True while the item is parented to a hand rather than lying in the world.</summary>
    public bool IsCarried { get; private set; }

    public Vector3 HeldPositionOffset => heldPositionOffset;

    public Quaternion HeldRotationOffset => Quaternion.Euler(heldRotationOffset);

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
        _rigidbody = GetComponent<Rigidbody>();

        if (item == null)
        {
            Debug.LogError($"{nameof(WorldItem)} on '{name}' has no ItemData assigned.", this);
        }
    }

    public bool CanPick(PlayerHands hands) => item != null && !IsCarried;

    public GameObject Pick(PlayerHands hands) => gameObject;

    /// <summary>
    /// Switches between "in someone's hand" and "lying in the world". Carried items must
    /// not collide with anything, or they shove the player around and block their own
    /// aim raycast.
    /// </summary>
    public void SetCarried(bool carried)
    {
        IsCarried = carried;

        foreach (Collider col in _colliders)
        {
            if (col != null)
            {
                col.enabled = !carried;
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = carried;
            _rigidbody.detectCollisions = !carried;

            if (!carried)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
