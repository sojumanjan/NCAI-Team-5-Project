using UnityEngine;

/// <summary>
/// Test interactable that exercises both interaction modes:
///   hold  — brew a coffee (the machine tints toward the brewing colour as you hold)
///   press — take the finished coffee, which resets the machine
///
/// Colour is driven through a MaterialPropertyBlock so it does not create a material
/// instance per machine, and the shared material asset is never modified.
/// </summary>
public class CoffeeMachine : InteractableBase
{
    [Header("커피 머신")]
    [Tooltip("커피를 내리는 데 걸리는 시간 (초). 길게 누르고 있어야 합니다.")]
    [SerializeField] private float brewSeconds = 2f;

    [Tooltip("아직 안 내렸을 때 문구.")]
    [SerializeField] private string brewPrompt = "커피 내리기 (길게 누르기)";

    [Tooltip("다 내려졌을 때 문구.")]
    [SerializeField] private string takePrompt = "커피 가져가기";

    [Header("색 피드백")]
    [Tooltip("색을 바꿀 렌더러. 비워두면 자기 자신에서 찾습니다.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("평소 색.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("내리는 중 색. 진행도에 따라 평소 색에서 이 색으로 섞입니다.")]
    [SerializeField] private Color brewingColor = new Color(1f, 0.55f, 0.15f);

    [Tooltip("완성됐을 때 색.")]
    [SerializeField] private Color readyColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("조준했을 때 살짝 밝아지는 정도. 0이면 하이라이트 없음.")]
    [Range(0f, 1f)]
    [SerializeField] private float focusHighlight = 0.25f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;
    private bool _ready;
    private bool _focused;

    /// <summary>True once a coffee has finished brewing and is waiting to be taken.</summary>
    public bool HasCoffee => _ready;

    // The prompt and the hold time both depend on which half of the cycle we are in.
    public override string Prompt => _ready ? takePrompt : brewPrompt;

    public override float HoldDuration => _ready ? 0f : Mathf.Max(0f, brewSeconds);

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        _block = new MaterialPropertyBlock();
        ApplyColor(idleColor);
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (_ready)
        {
            // Player took the cup — back to idle, ready to brew again.
            _ready = false;
            ApplyColor(idleColor);
        }
        else
        {
            _ready = true;
            ApplyColor(readyColor);
        }
    }

    public override void OnHoldProgress(PlayerInteractor interactor, float normalized)
    {
        ApplyColor(Color.Lerp(idleColor, brewingColor, normalized));
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        // Let go early: the brew is abandoned, no partial credit.
        ApplyColor(CurrentRestingColor());
    }

    public override void OnFocusEnter(PlayerInteractor interactor)
    {
        _focused = true;
        ApplyColor(CurrentRestingColor());
    }

    public override void OnFocusExit(PlayerInteractor interactor)
    {
        _focused = false;
        ApplyColor(CurrentRestingColor());
    }

    private Color CurrentRestingColor()
    {
        Color baseColor = _ready ? readyColor : idleColor;
        return _focused ? Color.Lerp(baseColor, Color.white, focusHighlight) : baseColor;
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null || _block == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(_block);
        // URP Lit uses _BaseColor; setting _Color too keeps built-in/unlit shaders working.
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_block);
    }

    private void OnValidate()
    {
        brewSeconds = Mathf.Max(0.05f, brewSeconds);
    }
}
