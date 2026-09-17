using UnityEngine;

/// <summary>
/// 반죽 보울. 조작은 블렌더와 같다 — 재료를 다 넣고 바라본 채 E를 누르고 있으면 흔들리다가
/// 반죽이 나온다. 손을 떼거나 시선이 벗어나면 진행도가 0으로 떨어진다.
///
/// 다른 점은 만들어내는 것이다. 여기서 나오는 반죽은 Dish가 아니라 Ingredient이고, 그대로
/// 오븐에 들어간다. 그래서 이 기구는 연쇄 레시피의 첫 칸이다 — 손님에게 낼 수 없다.
/// 재료 검사는 <see cref="StationBase.CanReceive"/>가 Category를 보지 않고 "어떤 레시피에
/// 쓰이는가"만 보기 때문에, 반죽을 오븐이 받는 데 따로 손댈 것이 없다.
///
/// 컴포넌트 설정: Kind = Bowl, Drive Mode = HoldToRun. 반죽 시간은 레시피의 Duration에서
/// 오므로 빵 종류마다 다르게 줄 수 있다.
/// </summary>
public class BowlStation : StationBase
{
    [Header("색 피드백")]
    [Tooltip("색을 바꿀 렌더러. 비워두면 자기 자신에서 찾습니다.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("비어 있을 때.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("재료가 들어갔지만 아직 안 반죽할 때.")]
    [SerializeField] private Color loadedColor = new Color(0.95f, 0.88f, 0.7f);

    [Tooltip("반죽 중. 진행도에 따라 loadedColor에서 이 색으로 물듭니다.")]
    [SerializeField] private Color kneadingColor = new Color(0.85f, 0.6f, 0.35f);

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
        ApplyColor(Color.Lerp(loadedColor, kneadingColor, normalized));
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        // 손을 뗐거나 시선이 벗어났다. 타이머는 조준 쪽이 이미 0으로 돌렸으니 색만 되돌린다.
        base.OnHoldCanceled(interactor);
        ApplyRestingColor();
    }

    protected override void OnCompleted(RecipeData recipe) => ApplyRestingColor();

    protected override void OnOutputTaken(ItemData item) => ApplyRestingColor();

    // ---------------------------------------------------------------- 색

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
