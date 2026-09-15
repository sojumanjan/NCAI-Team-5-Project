using UnityEngine;

/// <summary>
/// The blender. Load it with the right ingredients, then hold E while looking at it; let
/// go or look away and the progress drops straight back to zero. When the bar fills, the
/// drink pops out the same way the coffee machine's cup does.
///
/// All of that comes from <see cref="StationDriveMode.HoldToRun"/> — the interactor's own
/// hold timer is the blend timer, and it already cancels on release and on losing focus.
/// This class only adds the shaking and the colour so the state is readable.
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

    [Header("흔들림")]
    [Tooltip("가는 동안 흔들 부분. 비워두면 흔들지 않습니다.")]
    [SerializeField] private Transform shakeTarget;

    [Tooltip("흔들림 크기 (m).")]
    [SerializeField] private float shakeAmount = 0.02f;

    [Tooltip("흔들림 속도.")]
    [SerializeField] private float shakeSpeed = 40f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;
    private Vector3 _shakeOrigin;

    protected override void Awake()
    {
        base.Awake();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (shakeTarget != null)
        {
            _shakeOrigin = shakeTarget.localPosition;
        }

        _block = new MaterialPropertyBlock();
        ApplyRestingColor();
    }

    protected override void OnIngredientReceived(ItemData item) => ApplyRestingColor();

    protected override void OnIngredientReturned(ItemData item) => ApplyRestingColor();

    protected override void OnProcessingProgress(float normalized)
    {
        ApplyColor(Color.Lerp(loadedColor, blendingColor, normalized));
        Shake(normalized);
    }

    public override void OnHoldCanceled(PlayerInteractor interactor)
    {
        // Let go or looked away — the interactor already zeroed the timer, so just undo
        // the visuals. Nothing is kept: the next attempt starts from scratch.
        base.OnHoldCanceled(interactor);
        StopShaking();
        ApplyRestingColor();
    }

    protected override void OnCompleted(RecipeData recipe)
    {
        StopShaking();
        ApplyRestingColor();
    }

    protected override void OnOutputTaken(ItemData item) => ApplyRestingColor();

    // ---------------------------------------------------------------- visuals

    private void Shake(float strength)
    {
        if (shakeTarget == null || shakeAmount <= 0f)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount * strength;
        shakeTarget.localPosition = _shakeOrigin + new Vector3(offset, 0f, offset * 0.5f);
    }

    private void StopShaking()
    {
        if (shakeTarget != null)
        {
            shakeTarget.localPosition = _shakeOrigin;
        }
    }

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
