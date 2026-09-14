using UnityEngine;

/// <summary>
/// The coffee machine. All of the actual logic lives in <see cref="StationBase"/>; this
/// subclass only adds colour feedback so the state is readable without any UI yet.
///
/// Every appliance gets a class like this — small, and only about how it looks and sounds.
/// </summary>
public class CoffeeStation : StationBase
{
    [Header("색 피드백")]
    [Tooltip("색을 바꿀 렌더러. 비워두면 자기 자신에서 찾습니다.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("비어 있을 때.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("재료가 들어갔지만 아직 안 돌릴 때.")]
    [SerializeField] private Color loadedColor = new Color(0.55f, 0.75f, 1f);

    [Tooltip("조리 중. 진행도에 따라 loadedColor에서 이 색으로 물듭니다.")]
    [SerializeField] private Color brewingColor = new Color(1f, 0.55f, 0.15f);

    [Tooltip("완성됐을 때.")]
    [SerializeField] private Color readyColor = new Color(0.35f, 0.9f, 0.4f);

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

    protected override void OnProcessingStarted(RecipeData recipe) => ApplyColor(loadedColor);

    protected override void OnProcessingProgress(float normalized)
    {
        ApplyColor(Color.Lerp(loadedColor, brewingColor, normalized));
    }

    protected override void OnCompleted(RecipeData recipe) => ApplyRestingColor();

    protected override void OnOutputTaken(ItemData item) => ApplyRestingColor();

    private void ApplyRestingColor()
    {
        switch (State)
        {
            case StationState.Done:
                ApplyColor(readyColor);
                break;
            case StationState.Idle when Loaded.Count > 0:
                ApplyColor(loadedColor);
                break;
            default:
                ApplyColor(idleColor);
                break;
        }
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null || _block == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(_block);
        // URP Lit uses _BaseColor; _Color keeps built-in/unlit shaders working too.
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_block);
    }
}
