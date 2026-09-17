using UnityEngine;

/// <summary>
/// E로 들어 옮길 수 있는 물건. 재료통처럼 좌클릭이 이미 다른 일을 맡고 있어서 집기 동사를
/// 내줄 수 없는 것들에 붙인다.
///
/// 들린 뒤에는 일반 아이템과 완전히 같다 — 고스트로 놓일 자리가 보이고, R로 돌리고,
/// 좌클릭으로 내려놓는다. 그 기계는 <see cref="PlayerHands"/>에 이미 있으므로 여기서는
/// 손에 넘기기만 한다.
/// </summary>
[RequireComponent(typeof(WorldItem))]
public class CarryHandle : InteractableBase
{
    [Header("옮기기")]
    [Tooltip("손이 차 있어서 들 수 없을 때의 문구. 비워두면 그 상황에서 줄이 사라집니다.")]
    [SerializeField] private string handsFullPrompt = "손이 비어야 합니다";

    private WorldItem _worldItem;

    private void Awake()
    {
        _worldItem = GetComponent<WorldItem>();
        prompt = string.IsNullOrEmpty(prompt) ? "옮기기" : prompt;
    }

    public override string Prompt
    {
        get
        {
            PlayerHands hands = FindHands();
            if (hands != null && hands.IsHolding)
            {
                return handsFullPrompt;
            }

            return prompt;
        }
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        if (!base.CanInteract(interactor) || _worldItem == null || _worldItem.IsCarried)
        {
            return false;
        }

        PlayerHands hands = FindHands(interactor);
        return hands != null && !hands.IsHolding;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        PlayerHands hands = FindHands(interactor);
        if (hands == null)
        {
            return;
        }

        hands.TryGive(_worldItem);
    }

    // 손은 조준기와 같은 오브젝트에 산다. Prompt는 인자를 받지 않아 마지막으로 본 조준기를
    // 기억해 두고 쓴다 — 프롬프트 한 줄 때문에 씬 전체를 뒤질 이유는 없다.
    private PlayerInteractor _lastInteractor;

    public override void OnFocusEnter(PlayerInteractor interactor)
    {
        base.OnFocusEnter(interactor);
        _lastInteractor = interactor;
    }

    private PlayerHands FindHands(PlayerInteractor interactor = null)
    {
        PlayerInteractor source = interactor != null ? interactor : _lastInteractor;
        return source != null ? source.GetComponent<PlayerHands>() : null;
    }
}
