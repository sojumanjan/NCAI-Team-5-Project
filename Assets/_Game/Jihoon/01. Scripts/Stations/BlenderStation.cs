using UnityEngine;

/// <summary>
/// 블렌더. 맞는 재료를 넣고 바라본 채 E를 누르고 있어야 한다. 손을 떼거나 시선이 벗어나면
/// 진행도가 그대로 0으로 떨어진다. 다 채우면 커피머신의 컵처럼 음료가 튀어나온다.
///
/// 이 동작은 전부 <see cref="StationDriveMode.HoldToRun"/>에서 온다 — 조준 쪽의 홀드
/// 타이머가 곧 블렌딩 타이머이고, 떼거나 포커스를 잃으면 알아서 취소된다. 흔들림은
/// <see cref="StationBase"/>가 처리하므로 이 클래스는 색만 담당한다.
///
/// 컴포넌트에서 Drive Mode를 HoldToRun, Kind를 Blender로 두면 된다. 블렌딩 시간은 여기가
/// 아니라 레시피의 Duration에서 오므로 음료마다 다르게 줄 수 있다.
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
        // 손을 뗐거나 시선이 벗어났다. 타이머는 조준 쪽이 이미 0으로 돌렸으니 색만 되돌린다.
        // 진행분은 남기지 않는다 — 다음 시도는 처음부터다.
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
