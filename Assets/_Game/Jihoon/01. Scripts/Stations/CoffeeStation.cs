using UnityEngine;

/// <summary>
/// 커피 머신. 실제 로직은 전부 <see cref="StationBase"/>에 있고, 이 하위 클래스는 UI 없이도
/// 상태가 읽히도록 색 연출만 얹는다.
///
/// 기구마다 이런 클래스를 하나씩 둔다. 작고, 보이고 들리는 것만 담당한다.
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
        // URP Lit은 _BaseColor를 쓴다. _Color도 같이 넣어 빌트인·언릿 셰이더까지 대응한다.
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_block);
    }
}
