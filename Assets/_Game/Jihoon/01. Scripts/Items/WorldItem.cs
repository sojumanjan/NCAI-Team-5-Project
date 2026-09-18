using UnityEngine;

/// <summary>
/// 월드에 실제로 놓여 있는 <see cref="ItemData"/>의 실물. 들고 다닐 수 있는 모든 것에 붙는다.
/// 집으면 손에 부모로 붙고 물리가 꺼지며, 내려놓으면 물리가 돌아온다.
///
/// 좌클릭으로 집히는 역할은 일부러 여기에 없다. 그건 <see cref="PickableItem"/>이 맡는다.
/// 디스펜서는 옮길 수 있으면서도 좌클릭은 내용물을 꺼내야 하는데, 이 클래스가
/// <see cref="IItemSource"/>까지 겸하면 한 오브젝트에 소스가 둘이 되어 어느 쪽이 잡힐지
/// 컴포넌트 순서에 달리게 된다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour
{
    [Header("아이템")]
    [Tooltip("이 오브젝트가 어떤 아이템인지. 반드시 지정해야 합니다.")]
    [SerializeField] private ItemData item;

    [Header("손에 들었을 때")]
    [Tooltip("손 기준 위치 보정. 프리팹마다 중심이 달라서 미세 조정이 필요합니다.")]
    [SerializeField] private Vector3 heldPositionOffset;

    [Tooltip("손 기준 회전 보정.")]
    [SerializeField] private Vector3 heldRotationOffset;

    [Header("내려놓을 때")]
    [Tooltip("월드에 놓일 때의 회전 보정. 모델의 '똑바로 선' 자세가 X 0이 아닐 때 씁니다. " +
             "냉장고 안 재료통처럼 부모에게서 회전을 물려받아 서 있던 물건이 여기 해당합니다.")]
    [SerializeField] private Vector3 placedRotationOffset;

    private Collider[] _colliders;
    private Rigidbody _rigidbody;

    private bool _boundsResolved;
    private bool _hasBounds;
    private Bounds _localBounds;

    /// <summary>이 오브젝트가 무엇인지. 레시피는 이걸 참조로 비교한다.</summary>
    public ItemData Item => item;

    /// <summary>손이나 스테이션에 붙어 있는 동안 참. 월드에 놓여 있으면 거짓.</summary>
    public bool IsCarried { get; private set; }

    public Vector3 HeldPositionOffset => heldPositionOffset;

    public Quaternion HeldRotationOffset => Quaternion.Euler(heldRotationOffset);

    /// <summary>
    /// 내려놓을 때 바라보는 방향 위에 덧씌울 회전. 놓는 자세는 yaw만 플레이어를 따라가는데,
    /// 모델에 따라 그것만으로는 눕거나 뒤집힌다.
    /// </summary>
    public Quaternion PlacedRotationOffset => Quaternion.Euler(placedRotationOffset);

    // ---------------------------------------------------------------- 놓을 자리

    /// <summary>
    /// 조준한 표면에 얹은 물건을 그 자리에 고정한다. 콜라이더는 살아 있어서 다시 집을 수
    /// 있고 플레이어를 막지만, 물리 시뮬레이션에서는 빠진다.
    ///
    /// 물리를 남겨두면 콜라이더가 켜지는 순간 손에 들려 따라다니던 키네마틱 바디의 속도와
    /// 분리 계산이 겹쳐 물건이 위로 튄다. 게다가 카운터에 올려둔 오렌지가 굴러 떨어지는
    /// 것도 이 게임에선 사고일 뿐이다 — 놓은 자리에 그대로 있는 편이 옳다.
    ///
    /// 허공에서 놓을 때는 부르지 않는다. 그건 떨어져야 한다.
    /// </summary>
    public void RestOnSurface()
    {
        if (_rigidbody == null)
        {
            return;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;

        // 키네마틱이어도 다른 물체는 이걸 밀어낼 수 없어야 하므로 충돌 감지는 켜 둔다.
        _rigidbody.detectCollisions = true;
    }

    /// <summary>
    /// 피벗에서 물체 바닥까지의 높이. 프리팹 피벗이 메시 한가운데인 경우가 많아서, 이걸
    /// 더하지 않고 표면 좌표에 그대로 놓으면 절반이 파묻힌다 — 그리고 콜라이더가 켜지는
    /// 순간 물리가 밀어내며 위로 튄다. 놓을 때의 회전에 따라 값이 달라지므로 인자로 받는다.
    /// </summary>
    public float GetPivotToBottom(Quaternion rotation)
    {
        if (!TryGetLocalBounds(out Bounds local))
        {
            return 0f;
        }

        // worldToLocalMatrix로 구한 로컬 좌표라 스케일이 빠져 있다. 월드 높이를 알려면
        // 놓일 때의 스케일을 다시 곱해야 한다.
        Vector3 scale = transform.lossyScale;
        float lowest = float.MaxValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 world = rotation * Vector3.Scale(scale, Corner(local, i));
            lowest = Mathf.Min(lowest, world.y);
        }

        return Mathf.Max(0f, -lowest);
    }

    /// <summary>
    /// 벽처럼 세로로 선 면에 대고 놓을 때, 면에서 이만큼 떼어놓아야 물체가 박히지 않는다.
    /// 수평 반경만 본다 — 벽을 밀어내는 방향은 항상 수평이기 때문이다.
    /// </summary>
    public float GetPlacementRadius()
    {
        if (!TryGetLocalBounds(out Bounds local))
        {
            return 0f;
        }

        Vector3 scale = transform.lossyScale;
        Vector3 extents = local.extents;

        return Mathf.Max(Mathf.Abs(extents.x * scale.x), Mathf.Abs(extents.z * scale.z));
    }

    /// <summary>렌더러 전부를 감싸는, 이 오브젝트 로컬 공간의 박스. 한 번만 계산한다.</summary>
    private bool TryGetLocalBounds(out Bounds bounds)
    {
        if (!_boundsResolved)
        {
            _boundsResolved = true;

            Matrix4x4 toRoot = transform.worldToLocalMatrix;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Matrix4x4 toLocal = toRoot * renderer.transform.localToWorldMatrix;
                Bounds source = renderer.localBounds;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 point = toLocal.MultiplyPoint3x4(Corner(source, i));

                    if (_hasBounds)
                    {
                        _localBounds.Encapsulate(point);
                    }
                    else
                    {
                        _localBounds = new Bounds(point, Vector3.zero);
                        _hasBounds = true;
                    }
                }
            }
        }

        bounds = _localBounds;
        return _hasBounds;
    }

    private static Vector3 Corner(Bounds bounds, int index)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new Vector3(
            center.x + ((index & 1) == 0 ? -extents.x : extents.x),
            center.y + ((index & 2) == 0 ? -extents.y : extents.y),
            center.z + ((index & 4) == 0 ? -extents.z : extents.z));
    }

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
