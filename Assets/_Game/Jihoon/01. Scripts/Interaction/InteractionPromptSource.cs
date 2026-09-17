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

    [Header("문구")]
    [Tooltip("아이템을 집을 때 이름 뒤에 붙는 말.")]
    [SerializeField] private string takeSuffix = "집기";

    [Tooltip("아이템을 넣을 때 이름 뒤에 붙는 말.")]
    [SerializeField] private string putSuffix = "넣기";

    [Tooltip("놓을 곳이 없을 때 뜨는 문구. 비워두면 표시하지 않습니다.")]
    [SerializeField] private string dropLabel = "내려놓기";

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
                return $"{Name(action.Source.ProvidedItem)} {takeSuffix}";

            case LeftClickKind.Put:
                return $"{Name(hands.HeldItem)} {putSuffix}";

            case LeftClickKind.Click:
                return action.Click.ClickPrompt;

            case LeftClickKind.Drop:
                return dropLabel;

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

    // ---------------------------------------------------------------- helpers

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
