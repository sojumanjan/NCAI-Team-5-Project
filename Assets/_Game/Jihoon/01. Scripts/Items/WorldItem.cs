using UnityEngine;

/// <summary>
/// 월드에 실제로 놓여 있는 <see cref="ItemData"/>의 실물. 모든 아이템 프리팹에 붙는다.
/// 집으면 손에 부모로 붙고 물리가 꺼지며, 내려놓으면 물리가 돌아온다.
///
/// 새로 만들어 넘기지 않고 자기 자신을 넘기는 유일한 <see cref="IItemSource"/>다.
/// 인터페이스 이름이 "집힌다"가 아니라 "내준다"인 이유가 바로 이 예외 때문이다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IItemSource
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

    /// <summary>이 오브젝트가 무엇인지. 레시피는 이걸 참조로 비교한다.</summary>
    public ItemData Item => item;

    /// <summary>손이나 스테이션에 붙어 있는 동안 참. 월드에 놓여 있으면 거짓.</summary>
    public bool IsCarried { get; private set; }

    public Vector3 HeldPositionOffset => heldPositionOffset;

    public Quaternion HeldRotationOffset => Quaternion.Euler(heldRotationOffset);

    // ---------------------------------------------------------------- IItemSource

    public ItemData ProvidedItem => item;

    public bool CanProvide(PlayerHands hands) => item != null && !IsCarried;

    public GameObject Provide(PlayerHands hands) => gameObject;

    // ---------------------------------------------------------------- 수명주기

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
        _rigidbody = GetComponent<Rigidbody>();

        if (item == null)
        {
            Debug.LogError($"'{name}'의 {nameof(WorldItem)}에 ItemData가 연결되지 않았습니다.", this);
        }
    }

    /// <summary>
    /// "무언가에 들려 있음"과 "월드에 놓여 있음"을 오간다. 들린 아이템은 아무것과도
    /// 충돌하면 안 된다 — 플레이어를 밀어내고 자기 조준 레이까지 가로막는다.
    /// 스테이션이 완성품을 카운터에 얹어둘 때도 이걸 쓴다.
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
