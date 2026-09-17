using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오븐. 다른 기구와 달리 속이 들여다보이고 문이 있어서, 재료가 들어간 것이 눈에 보여야 한다.
///
/// 한 사이클:
///   문 열기 → 입구 좌클릭으로 재료를 슬롯에 넣기 → 문 닫기 → E로 작동
///   → 레시피 시간만큼 굽기 → 문 열기 → 입구 좌클릭으로 완성품 꺼내기
///
/// 좌클릭을 입구 콜라이더로 제한하는 이유: 조준 판정이 GetComponentInParent라 문 콜라이더
/// 에서도 이 스테이션이 잡힌다. 그대로 두면 리졸버가 Put을 Click보다 먼저 보기 때문에,
/// 재료를 든 채 문을 볼 때 문이 열리는 대신 재료가 빨려 들어간다.
///
/// 컴포넌트 설정: Kind = Oven, Drive Mode = PressAndWait.
/// </summary>
public class OvenStation : StationBase, IPlacementTarget
{
    [Header("오븐")]
    [Tooltip("오븐 문. 열려 있어야 재료를 넣고 꺼낼 수 있고, 닫혀 있어야 작동합니다.")]
    [SerializeField] private OpenableDoor door;

    [Tooltip("재료를 넣고 꺼내는 입구. 이 콜라이더를 조준했을 때만 좌클릭이 통합니다.")]
    [SerializeField] private Collider entranceCollider;

    [Tooltip("재료가 놓일 자리. 순서대로 채워지며, 첫 칸이 완성품이 나오는 자리이기도 합니다.")]
    [SerializeField] private Transform[] slots;

    [Tooltip("작동 중에는 문을 잠급니다.")]
    [SerializeField] private bool lockDoorWhileRunning = true;

    [Header("문구")]
    [Tooltip("재료는 맞지만 문이 열려 있어 작동시킬 수 없을 때.")]
    [SerializeField] private string doorOpenPrompt = "문을 닫으세요";

    private readonly List<WorldItem> _parked = new();

    // ---------------------------------------------------------------- 수명주기

    protected override void Awake()
    {
        base.Awake();

        if (door == null)
        {
            door = GetComponentInChildren<OpenableDoor>(true);
        }

        if (door == null)
        {
            Debug.LogError($"{name}: 오븐 문이 연결되지 않았습니다. 문 없이는 여닫기 판정을 할 수 없습니다.", this);
        }

        if (entranceCollider == null)
        {
            Debug.LogError($"{name}: 입구 콜라이더가 없습니다. 재료를 넣을 수 없습니다.", this);
        }

        if (slots == null || slots.Length == 0)
        {
            Debug.LogError($"{name}: 재료 슬롯이 하나도 없습니다.", this);
        }

        SetDoorLocked(false);
    }

    // ---------------------------------------------------------------- IInteractable

    public override string Prompt
    {
        // 재료는 맞는데 문이 열려 있는 상황이 이 기구에만 있다. 그때 "작동시키기"를 띄워두면
        // E를 눌러도 아무 일이 안 일어나는 이유를 플레이어가 알 수 없다.
        get
        {
            if (State == StationState.Idle && HasPendingRecipe && IsDoorOpen)
            {
                return doorOpenPrompt;
            }

            return base.Prompt;
        }
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        return base.CanInteract(interactor) && !IsDoorOpen;
    }

    // ---------------------------------------------------------------- 좌클릭 범위

    public override bool CanReceive(ItemData item, PlayerHands hands)
    {
        return IsAimingAtEntrance(hands) && IsDoorOpen && base.CanReceive(item, hands);
    }

    public override bool CanProvide(PlayerHands hands)
    {
        return IsAimingAtEntrance(hands) && IsDoorOpen && base.CanProvide(hands);
    }

    /// <summary>문이 아니라 입구를 겨누고 있는지. 이 구분이 문과 오븐의 좌클릭을 갈라놓는다.</summary>
    private bool IsAimingAtEntrance(PlayerHands hands)
    {
        if (entranceCollider == null || hands == null || hands.Interactor == null)
        {
            return false;
        }

        return hands.Interactor.CurrentCollider == entranceCollider;
    }

    private bool IsDoorOpen => door == null || door.IsOpen;

    // ---------------------------------------------------------------- IPlacementTarget

    public bool TryGetPlacement(ItemData item, out Vector3 position, out Quaternion rotation)
    {
        Transform slot = SlotAt(Loaded.Count);
        if (slot == null)
        {
            position = default;
            rotation = default;
            return false;
        }

        position = slot.position;
        rotation = slot.rotation;
        return true;
    }

    // ---------------------------------------------------------------- 재료 실물

    protected override Transform OutputOrigin
    {
        // 완성품은 재료를 넣었던 첫 칸에서 나온다.
        get
        {
            Transform first = SlotAt(0);
            return first != null ? first : base.OutputOrigin;
        }
    }

    protected override void OnIngredientObjectReceived(WorldItem item, int index)
    {
        Transform slot = SlotAt(index);
        if (slot == null)
        {
            base.OnIngredientObjectReceived(item, index);
            return;
        }

        // 들린 것도 떨어지는 것도 아닌 '얹힌' 상태. 콜라이더를 꺼야 입구 조준을 가리지 않는다.
        item.SetCarried(true);

        // 부모로 붙이지 않는다. 오븐 모델은 스케일이 1이 아니라 자식으로 넣으면 재료가
        // 늘어나고, 오븐은 움직이지 않으니 월드 좌표로 두는 편이 정직하다.
        item.transform.SetParent(null, true);
        item.transform.SetPositionAndRotation(slot.position, slot.rotation);

        _parked.Add(item);
    }

    protected override GameObject TakeBackIngredientObject(int index)
    {
        if (index < 0 || index >= _parked.Count)
        {
            return null;
        }

        WorldItem item = _parked[index];
        _parked.RemoveAt(index);

        return item != null ? item.gameObject : null;
    }

    protected override void ClearIngredientObjects()
    {
        foreach (WorldItem item in _parked)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        _parked.Clear();
    }

    // ---------------------------------------------------------------- 문 잠금

    protected override void OnProcessingStarted(RecipeData recipe) => SetDoorLocked(true);

    protected override void OnCompleted(RecipeData recipe) => SetDoorLocked(false);

    private void SetDoorLocked(bool locked)
    {
        if (door != null)
        {
            door.Locked = locked && lockDoorWhileRunning;
        }
    }

    // ---------------------------------------------------------------- 내부

    private Transform SlotAt(int index)
    {
        if (slots == null || slots.Length == 0 || index < 0)
        {
            return null;
        }

        // 슬롯보다 재료가 많으면 마지막 칸에 겹쳐 둔다. 칸이 모자란 건 배치 실수지만,
        // 그 때문에 재료가 원점에 생기는 것보다는 눈에 띄는 편이 낫다.
        return slots[Mathf.Min(index, slots.Length - 1)];
    }

    private void OnDrawGizmosSelected()
    {
        if (slots != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f);
            foreach (Transform slot in slots)
            {
                if (slot != null)
                {
                    Gizmos.DrawWireCube(slot.position, new Vector3(0.25f, 0.25f, 0.25f));
                }
            }
        }

        if (entranceCollider != null)
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f);
            Gizmos.DrawWireCube(entranceCollider.bounds.center, entranceCollider.bounds.size);
        }
    }
}
