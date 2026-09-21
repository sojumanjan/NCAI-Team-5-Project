using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the list of available actions near the crosshair.
///
/// Uses a fixed set of <see cref="PromptLineView"/>s placed in the scene rather than
/// spawning them, so the whole look is authored in the Canvas and nothing allocates at
/// runtime. Two lines is enough for everything in the game today; extra prompts beyond
/// the supplied lines are simply not drawn.
///
/// Purely event-driven — it only redraws when the prompt list actually changes, so there
/// is no per-frame work here at all.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("프롬프트를 만들어내는 쪽. Player에 붙어 있습니다.")]
    [SerializeField] private InteractionPromptSource source;

    [Header("줄")]
    [Tooltip("미리 배치해둔 줄들. 위에서부터 순서대로 채웁니다. 보통 2개면 충분합니다.")]
    [SerializeField] private PromptLineView[] lines;

    [Header("키 표시")]
    [Tooltip("좌클릭을 뭐라고 쓸지.")]
    [SerializeField] private string leftClickKey = "[좌클릭]";

    [Tooltip("상호작용 키를 뭐라고 쓸지. 키 바인딩을 바꾸면 여기도 바꾸세요.")]
    [SerializeField] private string interactKey = "[E]";

    [Tooltip("길게 눌러야 하는 행동일 때 대신 쓸 표기. 위 표기를 통째로 대체합니다.")]
    [SerializeField] private string interactHoldKey = "[E 홀드]";

    [Tooltip("놓기 직전 회전 키를 뭐라고 쓸지. PlacementPreview의 회전 키와 맞추세요.")]
    [SerializeField] private string rotateKey = "[R]";

    private void Awake()
    {
        if (source == null || lines == null || lines.Length == 0)
        {
            Debug.LogError($"{nameof(InteractionPromptUI)}: Source and at least one line are required.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (source != null)
        {
            source.PromptsChanged += Redraw;
            Redraw(source.Prompts);
        }
    }

    private void OnDisable()
    {
        if (source != null)
        {
            source.PromptsChanged -= Redraw;
        }
    }

    private void Redraw(IReadOnlyList<ActionPrompt> prompts)
    {
        int count = prompts != null ? prompts.Count : 0;

        for (int i = 0; i < lines.Length; i++)
        {
            PromptLineView line = lines[i];
            if (line == null)
            {
                continue;
            }

            if (i >= count)
            {
                line.Hide();
                continue;
            }

            ActionPrompt prompt = prompts[i];
            line.Show(KeyLabel(prompt), prompt.Label, prompt.Enabled);
        }
    }

    private string KeyLabel(ActionPrompt prompt)
    {
        switch (prompt.Verb)
        {
            case InputVerb.Rotate:
                return rotateKey;

            case InputVerb.Interact:
                // Hold gets its own complete string rather than a suffix, so the brackets
                // can sit around the whole thing: "[E 홀드]" instead of "[E] 홀드".
                return prompt.IsHold ? interactHoldKey : interactKey;

            default:
                return leftClickKey;
        }
    }
}
