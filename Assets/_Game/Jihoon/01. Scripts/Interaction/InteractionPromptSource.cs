using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns "what am I aiming at" into "what can I do about it". Put this on the Player next
/// to <see cref="PlayerInteractor"/> and <see cref="PlayerHands"/>.
///
/// The order it checks things in mirrors <see cref="PlayerHands"/> exactly — if the prompt
/// says 넣기 then left click must actually put the item in, so the two must never drift.
/// Any change to one belongs in the other.
///
/// It produces data, not text on screen: the UI subscribes and decides how to draw it.
/// </summary>
public class InteractionPromptSource : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("조준 대상. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private PlayerInteractor interactor;

    [Tooltip("손 상태. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private PlayerHands hands;

    [Tooltip("놓기 미리보기. 비워두면 같은 오브젝트에서 찾고, 없으면 회전 안내를 띄우지 않습니다.")]
    [SerializeField] private PlacementPreview preview;

    [Header("문구")]
    [Tooltip("아이템을 집을 때 이름 뒤에 붙는 말.")]
    [SerializeField] private string takeSuffix = "집기";

    [Tooltip("아이템을 넣을 때 이름 뒤에 붙는 말.")]
    [SerializeField] private string putSuffix = "넣기";

    [Tooltip("놓을 곳이 없을 때 뜨는 문구. 비워두면 표시하지 않습니다.")]
    [SerializeField] private string dropLabel = "내려놓기";

    [Tooltip("놓기 직전에 물건을 돌릴 수 있을 때 뜨는 문구.")]
    [SerializeField] private string rotateLabel = "돌리기";

    [Header("상하는 음식")]
    [Tooltip("남은 시간 표기. {0}이 초입니다. 비워두면 표시하지 않습니다.")]
    [SerializeField] private string freshnessFormat = "  (상하기까지 {0}초)";

    [Tooltip("이미 상했을 때 붙일 말.")]
    [SerializeField] private string spoiledLabel = "  (상함)";

    private readonly List<ActionPrompt> _prompts = new();
    private readonly List<ActionPrompt> _previous = new();

    /// <summary>What the player can do right now. Rebuilt every frame, may be empty.</summary>
    public IReadOnlyList<ActionPrompt> Prompts => _prompts;

    /// <summary>Fires only when the list actually changes, not every frame.</summary>
    public event Action<IReadOnlyList<ActionPrompt>> PromptsChanged;

    private void Awake()
    {
        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        if (hands == null)
        {
            hands = GetComponent<PlayerHands>();
        }

        if (preview == null)
        {
            preview = GetComponent<PlacementPreview>();
        }

        if (interactor == null || hands == null)
        {
            Debug.LogError($"{nameof(InteractionPromptSource)}: needs {nameof(PlayerInteractor)} " +
                           $"and {nameof(PlayerHands)} on the same object.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Rebuild();

        if (!SameAsPrevious())
        {
            _previous.Clear();
            _previous.AddRange(_prompts);
            PromptsChanged?.Invoke(_prompts);
        }
    }

    // ---------------------------------------------------------------- building

    private void Rebuild()
    {
        _prompts.Clear();

        if (!interactor.HasHit)
        {
            return;
        }

        AddLeftClickPrompt();
        AddInteractPrompt();
        AddRotatePrompt();
    }

    private void AddLeftClickPrompt()
    {
        // Ask the resolver what left click does, then only put it into words. The choice
        // itself is never made here, so the prompt cannot describe the wrong action.
        LeftClickAction action = InteractionResolver.Resolve(interactor, hands);
        string label = DescribeLeftClick(action);

        if (!string.IsNullOrEmpty(label))
        {
            _prompts.Add(new ActionPrompt(InputVerb.LeftClick, label, true));
        }
    }

    private string DescribeLeftClick(LeftClickAction action)
    {
        switch (action.Kind)
        {
            case LeftClickKind.Take:
                // 조준한 것이 상하는 음식이면 남은 시간을 붙인다. 들고 있지 않아도 보여야
                // 바닥에 둔 접시를 쓸 수 있는지 없는지 판단할 수 있다.
                return $"{Name(action.Source.ProvidedItem)} {takeSuffix}"
                       + Freshness(interactor.GetAimed<PerishableDish>());

            case LeftClickKind.Put:
                // 받는 쪽이 할 말을 정해뒀으면 그걸 쓴다. 쓰레기통은 "넣기"가 아니라 "버리기"다.
                return action.Receiver is IReceiverPrompt custom && !string.IsNullOrEmpty(custom.ReceivePrompt)
                    ? custom.ReceivePrompt
                    : $"{Name(hands.HeldItem)} {putSuffix}";

            case LeftClickKind.Click:
                return action.Click.ClickPrompt;

            case LeftClickKind.Drop:
                return dropLabel + Freshness(Held());

            default:
                return null;
        }
    }

    private void AddInteractPrompt()
    {
        IInteractable interactable = interactor.Current;
        if (interactable == null)
        {
            return;
        }

        string label = interactable.Prompt;
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        _prompts.Add(new ActionPrompt(InputVerb.Interact,
                                      label,
                                      interactable.CanInteract(interactor),
                                      interactable.HoldDuration));
    }

    /// <summary>
    /// 회전은 물건을 들고 얹을 수 있는 면을 조준했을 때만 먹는다. 그 판단은 미리보기가
    /// 이미 내리고 있으므로 여기서 다시 계산하지 않고 물어본다.
    /// </summary>
    private void AddRotatePrompt()
    {
        if (preview == null || !preview.CanRotate || string.IsNullOrEmpty(rotateLabel))
        {
            return;
        }

        _prompts.Add(new ActionPrompt(InputVerb.Rotate, rotateLabel, true));
    }

    // ---------------------------------------------------------------- helpers

    private PerishableDish Held()
    {
        return hands.HeldObject != null ? hands.HeldObject.GetComponent<PerishableDish>() : null;
    }

    /// <summary>남은 신선도를 문구 뒤에 붙일 조각으로 만든다. 해당 없으면 빈 문자열.</summary>
    private string Freshness(PerishableDish dish)
    {
        if (dish == null)
        {
            return string.Empty;
        }

        if (dish.IsSpoiled)
        {
            return spoiledLabel;
        }

        // 아직 기구에서 꺼내기 전이면 시간이 흐르지 않는다. 그때까지 숫자를 보여주면
        // 줄어들지 않는 카운터가 붙어 있어 오히려 헷갈린다.
        if (!dish.IsCounting || string.IsNullOrEmpty(freshnessFormat))
        {
            return string.Empty;
        }

        return string.Format(freshnessFormat, Mathf.CeilToInt(dish.Remaining));
    }

    private static string Name(ItemData item) => item != null ? item.DisplayName : "아이템";

    private bool SameAsPrevious()
    {
        if (_prompts.Count != _previous.Count)
        {
            return false;
        }

        for (int i = 0; i < _prompts.Count; i++)
        {
            if (!_prompts[i].Equals(_previous[i]))
            {
                return false;
            }
        }

        return true;
    }
}
