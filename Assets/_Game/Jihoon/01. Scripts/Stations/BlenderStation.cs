using UnityEngine;

/// <summary>
/// The blender. Load it with the right ingredients, then hold E while looking at it; let
/// go or look away and the progress drops straight back to zero. When the bar fills, the
/// drink pops out the same way the coffee machine's cup does.
///
/// All of that comes from <see cref="StationDriveMode.HoldToRun"/> — the interactor's own
/// hold timer is the blend timer, and it already cancels on release and on losing focus.
/// Shaking is handled by <see cref="StationBase"/>; this class only adds the colour.
///
/// Set Drive Mode to HoldToRun and Kind to Blender on the component. The blend time comes
/// from the recipe's Duration, not from here, so different drinks can take different times.
/// </summary>
public class BlenderStation : StationBase
{
    [Header("색 피드백")]
    [Tooltip("색을 바꿀 렌더러. 비워두면 자기 자신에서 찾습니다.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("비어 있을 때.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("재료가 들어갔지만 아직 안 돌릴 때.")]
    [SerializeField] private Color loadedColor = new Color(0.8f, 0.6f, 1f);

    [Tooltip("가는 중. 진행도에 따라 loadedColor에서 이 색으로 물듭니다.")]
    [SerializeField] private Color blendingColor = new Color(1f, 0.35f, 0.55f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;

    protected override void Awake()
    {
        base.Awake();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        _block = new MaterialPropertyBlock();
        ApplyRestingColor();
    }

    protected override void OnIngredientReceived(ItemData item) => ApplyRestingColor();

    protected override void OnIngredientReturned(ItemData item) => ApplyRestingColor();

    protected override void OnProcessingProgress(float normalized)
    {
        ApplyColor(Color.Lerp(loadedColor, blendingColor, normalized));
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        // Let go or looked away — the interactor already zeroed the timer, so just undo
        // the colour. Nothing is kept: the next attempt starts from scratch.
        base.OnHoldCanceled(interactor);
        ApplyRestingColor();
    }

    protected override void OnCompleted(RecipeData recipe) => ApplyRestingColor();

    protected override void OnOutputTaken(ItemData item) => ApplyRestingColor();

    // ---------------------------------------------------------------- colour

    private void ApplyRestingColor()
    {
        if (State == StationState.Idle && Loaded.Count > 0)
        {
            ApplyColor(loadedColor);
            return;
        }

        ApplyColor(idleColor);
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null || _block == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_block);
    }
}
