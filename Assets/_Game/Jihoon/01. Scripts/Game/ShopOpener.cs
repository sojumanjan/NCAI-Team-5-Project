using DG.Tweening;
using UnityEngine;

/// <summary>
/// 가게 셔터를 올려 장사를 시작하는 스위치. 가게 안의 버튼 오브젝트에 붙인다.
///
/// 플레이와 동시에 영업이 시작되면 재료 위치를 익힐 틈도 없이 손님이 들이닥친다. 그래서
/// 시계를 09:00에 세워둔 채 준비 시간을 주고, 플레이어가 준비됐다고 선언할 때 하루가
/// 시작된다. <see cref="MiniGameSession.StartDay"/>는 원래 이 용도로 열어둔 입구다.
///
/// 좌클릭 동사를 쓰는 이유는 손에 무엇이 들려 있든 동작해야 하기 때문이다. E는 조리기구가
/// 쓰고 있고, 리졸버가 IClickTarget을 '내려놓기'보다 먼저 보기 때문에 재료를 든 채 눌러도
/// 바닥에 떨어뜨리지 않는다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopOpener : MonoBehaviour, IClickTarget
{
    [Header("참조")]
    [Tooltip("하루를 시작시킬 세션.")]
    [SerializeField] private MiniGameSession session;

    [Tooltip("올라갈 셔터. 이 트랜스폼의 Y 스케일을 줄입니다. 셔터 본체가 아니라 피벗입니다.")]
    [SerializeField] private Transform shutterPivot;

    [Header("셔터")]
    [Tooltip("다 열렸을 때의 Y 스케일. 0으로 두면 메시가 완전히 사라져 부자연스럽습니다.")]
    [SerializeField] private float openScaleY = 0.05f;

    [Tooltip("열리는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 6f;

    [Tooltip("올라가는 가속 곡선.")]
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Header("소리")]
    [Tooltip("셔터가 올라갈 때.")]
    [SerializeField] private SoundData shutterSound;

    [Header("문구")]
    [Tooltip("아직 시작 전일 때.")]
    [SerializeField] private string readyPrompt = "장사 시작하기";

    [Tooltip("셔터가 올라가는 동안. 비워두면 줄이 사라집니다.")]
    [SerializeField] private string openingPrompt = "셔터 여는 중...";

    private float _closedScaleY = 1f;
    private bool _opening;
    private bool _opened;
    private Tween _tween;

    // ---------------------------------------------------------------- 수명주기

    private void Awake()
    {
        if (session == null)
        {
            session = FindFirstObjectByType<MiniGameSession>();
        }

        if (session == null || shutterPivot == null)
        {
            Debug.LogError($"{nameof(ShopOpener)} on '{name}': 세션과 셔터 피벗이 필요합니다.", this);
            enabled = false;
            return;
        }

        // 닫힌 자세를 씬에서 읽어둔다. 에디터에서 셔터 높이를 조절해도 그게 곧 닫힌 상태다.
        _closedScaleY = shutterPivot.localScale.y;

        SetShutterScaleY(_closedScaleY);
    }

    private void OnDestroy()
    {
        // 트윈은 대상보다 오래 산다. 파괴된 트랜스폼에 쓰려 들면 씬을 내릴 때 터진다.
        _tween?.Kill();
        _tween = null;
    }

    // ---------------------------------------------------------------- IClickTarget

    public string ClickPrompt
    {
        get
        {
            if (_opened)
            {
                return string.Empty;
            }

            return _opening ? openingPrompt : readyPrompt;
        }
    }

    public bool CanClick(PlayerHands hands)
    {
        return !_opening
               && !_opened
               && session != null
               && session.State == CookingSessionState.Ready;
    }

    public void OnClick(PlayerHands hands)
    {
        if (!CanClick(hands))
        {
            return;
        }

        OpenShutter();
    }

    // ---------------------------------------------------------------- 셔터

    /// <summary>셔터를 올리고, 다 올라간 뒤에 하루를 시작한다.</summary>
    public void OpenShutter()
    {
        _opening = true;

        AudioManager.PlayAt(shutterSound, shutterPivot.position);

        _tween?.Kill();
        _tween = shutterPivot.DOScaleY(openScaleY, duration)
                             .SetEase(ease)
                             .SetLink(gameObject)
                             .OnComplete(BeginDay);
    }

    private void BeginDay()
    {
        _tween = null;
        _opening = false;
        _opened = true;

        // 셔터가 다 열린 다음에야 손님을 받는다. 올라가는 동안 첫 손님이 걸어 들어오면
        // 문을 통과하는 그림이 된다.
        session.StartDay();
    }

    private void SetShutterScaleY(float y)
    {
        Vector3 scale = shutterPivot.localScale;
        scale.y = y;
        shutterPivot.localScale = scale;
    }

    private void OnValidate()
    {
        openScaleY = Mathf.Max(0.001f, openScaleY);
        duration = Mathf.Max(0.1f, duration);
    }
}
