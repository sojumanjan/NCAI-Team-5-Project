using DG.Tweening;
using UnityEngine;

/// <summary>
/// 가게 셔터를 여닫아 하루를 열고 닫는 스위치. 가게 안의 버튼 오브젝트에 붙인다.
///
/// 플레이와 동시에 영업이 시작되면 재료 위치를 익힐 틈도 없이 손님이 들이닥친다. 그래서
/// 시계를 09:00에 세워둔 채 준비 시간을 주고, 플레이어가 준비됐다고 선언할 때 하루가
/// 시작된다.
///
/// 끝도 같은 자리에서 맺는다. 21시가 되면 새 손님만 끊기고 판은 계속 돌아가는데,
/// 남은 손님을 다 보낸 뒤 이 버튼을 다시 눌러야 셔터가 내려오고 결과가 나온다. 시계가
/// 21시를 치자마자 화면을 덮어버리면 마지막 손님이 공중에서 사라진다.
///
/// 셔터는 <b>위아래로 움직인다.</b> 예전엔 피벗의 Y 스케일을 줄여 말려 올라가는 흉내를
/// 냈는데, 그러면 자식들이 같이 찌그러졌다. 어차피 밖으로 나갈 수 없으니 천장 뒤로
/// 밀어 올리는 편이 깨끗하다.
///
/// 좌클릭 동사를 쓰는 이유는 손에 무엇이 들려 있든 동작해야 하기 때문이다. E는 조리기구가
/// 쓰고 있고, 리졸버가 IClickTarget을 '내려놓기'보다 먼저 보기 때문에 재료를 든 채 눌러도
/// 바닥에 떨어뜨리지 않는다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopOpener : MonoBehaviour, IClickTarget
{
    /// <summary>셔터가 지금 움직이는 중인지.</summary>
    private enum Motion
    {
        Idle,
        Opening,
        Closing,
    }

    [Header("참조")]
    [Tooltip("하루를 시작하고 끝낼 세션.")]
    [SerializeField] private MiniGameSession session;

    [Tooltip("움직일 셔터. 이 트랜스폼을 통째로 들어 올립니다. 셔터 본체가 아니라 피벗입니다.")]
    [SerializeField] private Transform shutterPivot;

    [Tooltip("난이도를 골라야 셔터를 올릴 수 있습니다. 비워두면 씬에서 찾고, 씬에도 없으면 막지 않습니다.")]
    [SerializeField] private CookingDifficulty difficulty;

    [Header("셔터")]
    [Tooltip("열릴 때 들어 올릴 높이 (m). 셔터가 천장 뒤로 완전히 숨을 만큼 주세요.")]
    [SerializeField] private float liftHeight = 7.4f;

    [Tooltip("열리는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 6f;

    [Tooltip("닫히는 데 걸리는 시간 (초). 하루가 끝난 뒤라 열 때보다 짧은 편이 낫습니다.")]
    [SerializeField] private float closeDuration = 3f;

    [Tooltip("움직이는 가속 곡선.")]
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Tooltip("셔터가 다 닫히고 결과 화면이 뜨기까지의 뜸 (초). 0이면 곧바로 뜹니다.")]
    [SerializeField] private float resultDelay = 1f;

    [Header("스위치")]
    [Tooltip("딸깍 넘어가는 스위치 손잡이의 축. 로컬 X 회전으로 올림·내림을 나타냅니다. 비워두면 움직이지 않습니다.")]
    [SerializeField] private Transform switchPivot;

    [Tooltip("셔터를 올릴 때 스위치의 로컬 X 각도.")]
    [SerializeField] private float switchOpenedAngle = 220f;

    [Tooltip("셔터를 내릴 때(그리고 처음) 스위치의 로컬 X 각도.")]
    [SerializeField] private float switchClosedAngle = 320f;

    [Tooltip("스위치가 넘어가는 시간 (초). 짧을수록 딸깍 합니다.")]
    [SerializeField] private float switchDuration = 0.08f;

    [Header("소리")]
    [Tooltip("셔터가 올라갈 때.")]
    [SerializeField] private SoundData shutterSound;

    [Tooltip("셔터가 내려올 때. 비워두면 올라갈 때 소리를 그대로 씁니다.")]
    [SerializeField] private SoundData closeSound;

    [Tooltip("스위치를 딸깍 넘길 때. 셔터 소리와 함께 납니다.")]
    [SerializeField] private SoundData switchSound;

    [Tooltip("셔터 소리가 나는 자리. 셔터 아래쪽이나 입구에 둔 빈 오브젝트를 넣으세요. " +
             "비워두면 셔터가 닫혀 있을 때의 피벗 자리에서 납니다.")]
    [SerializeField] private Transform soundPoint;

    [Header("문구")]
    [Tooltip("아직 시작 전일 때.")]
    [SerializeField] private string readyPrompt = "장사 시작하기";

    [Tooltip("시작 전인데 난이도를 아직 고르지 않았을 때.")]
    [SerializeField] private string needDifficultyPrompt = "난이도를 먼저 선택하세요";

    [Tooltip("셔터가 올라가는 동안. 비워두면 줄이 사라집니다.")]
    [SerializeField] private string openingPrompt = "셔터 여는 중...";

    [Tooltip("영업 중이라 아직 닫을 수 없을 때.")]
    [SerializeField] private string busyPrompt = "아직 영업이 끝나지 않았습니다";

    [Tooltip("마감했지만 손님이 아직 카운터에 있을 때.")]
    [SerializeField] private string waitingPrompt = "손님이 남아 있습니다";

    [Tooltip("이제 닫아도 될 때.")]
    [SerializeField] private string closePrompt = "영업 종료하기";

    [Tooltip("셔터가 내려오는 동안. 비워두면 줄이 사라집니다.")]
    [SerializeField] private string closingPrompt = "셔터 닫는 중...";

    private float _closedLocalY;

    // 닫을 때 피벗은 7.4m 위 천장 뒤에 있어 그 자리에서 틀면 들리는 거리를 벗어난다. 닫힌 자리를 기억해 둔다.
    private Vector3 _closedSoundPosition;

    private Motion _motion = Motion.Idle;
    private bool _opened;
    private bool _closed;
    private Tween _tween;
    private Tween _switchTween;

    // X가 90°를 넘으면 localEulerAngles가 (320,180,180)처럼 다른 표현으로 돌아온다. 매번 읽으면 Y·Z가 뒤집혀
    // 닫을 때 제자리에서 안 움직이므로, 처음 Y·Z를 기억해 두고 X만 바꾼다.
    private Vector2 _switchBaseYZ;

    /// <summary>
    /// 셔터를 올리기 시작했는지. 올라가는 중도 포함한다 — 튜토리얼 쪽지가 이걸 보고
    /// 접히는데, 다 올라갈 때까지(6초) 기다리면 눌렀는데 안 먹힌 것처럼 보인다.
    /// </summary>
    public bool HasOpened => _motion == Motion.Opening || _opened;

    /// <summary>셔터를 내리기 시작했는지. <see cref="HasOpened"/>와 같은 이유로 내려가는 중도 포함한다.</summary>
    public bool HasClosed => _motion == Motion.Closing || _closed;

    private bool DifficultyChosen => difficulty == null || difficulty.HasSelection;

    // ---------------------------------------------------------------- 수명주기

    private void Awake()
    {
        if (session == null)
        {
            session = FindFirstObjectByType<MiniGameSession>();
        }

        if (difficulty == null)
        {
            difficulty = FindAnyObjectByType<CookingDifficulty>();
        }

        if (session == null || shutterPivot == null)
        {
            Debug.LogError($"{nameof(ShopOpener)} on '{name}': 세션과 셔터 피벗이 필요합니다.", this);
            enabled = false;
            return;
        }

        // 닫힌 자세를 씬에서 읽어둔다. 에디터에서 셔터 위치를 옮겨도 그게 곧 닫힌 상태다.
        _closedLocalY = shutterPivot.localPosition.y;
        SetLocalY(_closedLocalY);
        _closedSoundPosition = shutterPivot.position;

        // 씬에서 손잡이를 어느 쪽에 두었든 셔터는 닫힌 채 시작하므로 내림 쪽에 맞춘다.
        if (switchPivot != null)
        {
            Vector3 euler = switchPivot.localEulerAngles;
            _switchBaseYZ = new Vector2(euler.y, euler.z);
        }

        SetSwitch(switchClosedAngle, false);
    }

    private Vector3 SoundPosition => soundPoint != null ? soundPoint.position : _closedSoundPosition;

    private void OnDestroy()
    {
        // 트윈은 대상보다 오래 산다. 파괴된 트랜스폼에 쓰려 들면 씬을 내릴 때 터진다.
        _tween?.Kill();
        _tween = null;
        _switchTween?.Kill();
        _switchTween = null;
    }

    // ---------------------------------------------------------------- IClickTarget

    public string ClickPrompt
    {
        get
        {
            if (_motion == Motion.Opening)
            {
                return openingPrompt;
            }

            if (_motion == Motion.Closing || _closed)
            {
                return _closed ? string.Empty : closingPrompt;
            }

            if (session == null)
            {
                return string.Empty;
            }

            switch (session.State)
            {
                case CookingSessionState.Ready:
                    return DifficultyChosen ? readyPrompt : needDifficultyPrompt;

                case CookingSessionState.Running:
                    return busyPrompt;

                case CookingSessionState.Closing:
                    return session.AnyCustomerAtCounter ? waitingPrompt : closePrompt;

                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// 리졸버는 이게 거짓이면 문구까지 통째로 감춘다. 그래서 "아직 영업이 끝나지
    /// 않았습니다" 같은 안내도 여기서 참을 돌려줘야 화면에 뜬다. 실제로 먹히는지는
    /// <see cref="OnClick"/>가 다시 판단한다 — 눌러도 아무 일이 없는 건 괜찮지만,
    /// 버튼 앞에서 아무 말도 안 해주는 건 안 된다.
    /// </summary>
    public bool CanClick(PlayerHands hands)
    {
        if (_motion != Motion.Idle || _closed || session == null)
        {
            return false;
        }

        return session.State == CookingSessionState.Ready
               || session.State == CookingSessionState.Running
               || session.State == CookingSessionState.Closing;
    }

    public void OnClick(PlayerHands hands)
    {
        if (!CanClick(hands))
        {
            return;
        }

        if (session.State == CookingSessionState.Ready)
        {
            // CanClick은 참으로 둔다. 거짓이면 "난이도를 먼저 선택하세요"까지 감춰져 왜 안 열리는지 모른다.
            if (DifficultyChosen)
            {
                OpenShutter();
            }

            return;
        }

        if (session.State == CookingSessionState.Closing && !session.AnyCustomerAtCounter)
        {
            CloseShutter();
        }
    }

    // ---------------------------------------------------------------- 셔터

    /// <summary>셔터를 올리고, 다 올라간 뒤에 하루를 시작한다.</summary>
    public void OpenShutter()
    {
        _motion = Motion.Opening;

        SetSwitch(switchOpenedAngle, true);
        PlaySwitchSound();
        AudioManager.PlayAt(shutterSound, SoundPosition);

        _tween?.Kill();
        _tween = shutterPivot.DOLocalMoveY(_closedLocalY + liftHeight, duration)
                             .SetEase(ease)
                             .SetLink(gameObject)
                             .OnComplete(BeginDay);
    }

    /// <summary>셔터를 내리고, 다 내려온 뒤에 하루를 끝낸다.</summary>
    public void CloseShutter()
    {
        _motion = Motion.Closing;

        SetSwitch(switchClosedAngle, true);
        PlaySwitchSound();
        AudioManager.PlayAt(closeSound != null ? closeSound : shutterSound, SoundPosition);

        _tween?.Kill();
        _tween = shutterPivot.DOLocalMoveY(_closedLocalY, closeDuration)
                             .SetEase(ease)
                             .SetLink(gameObject)
                             .OnComplete(FinishDay);
    }

    private void BeginDay()
    {
        _tween = null;
        _motion = Motion.Idle;
        _opened = true;

        // 셔터가 다 열린 다음에야 손님을 받는다. 올라가는 동안 첫 손님이 걸어 들어오면
        // 문을 통과하는 그림이 된다.
        session.StartDay();
    }

    private void FinishDay()
    {
        _tween = null;
        _motion = Motion.Idle;
        _closed = true;

        // 셔터가 쿵 하고 닿자마자 결과창이 덮치면 닫히는 걸 본 느낌이 안 남는다.
        // 한 박자 쉬고 띄운다.
        if (resultDelay <= 0f)
        {
            session.EndDay();
            return;
        }

        DOVirtual.DelayedCall(resultDelay, session.EndDay).SetLink(gameObject);
    }

    // 스위치는 셔터가 움직이기 시작하는 순간 넘어간다. 다 올라간 뒤에 넘어가면 눌렀는데 안 먹힌 것처럼 보인다.
    private void SetSwitch(float angle, bool animate)
    {
        if (switchPivot == null)
        {
            return;
        }

        Quaternion target = Quaternion.Euler(angle, _switchBaseYZ.x, _switchBaseYZ.y);
        _switchTween?.Kill();

        if (!animate || switchDuration <= 0f)
        {
            switchPivot.localRotation = target;
            return;
        }

        _switchTween = switchPivot.DOLocalRotateQuaternion(target, switchDuration).SetEase(Ease.OutQuad).SetLink(switchPivot.gameObject);
    }

    private void PlaySwitchSound()
    {
        if (switchSound != null)
        {
            AudioManager.PlayAt(switchSound, transform.position);
        }
    }

    private void SetLocalY(float y)
    {
        Vector3 position = shutterPivot.localPosition;
        position.y = y;
        shutterPivot.localPosition = position;
    }

    private void OnValidate()
    {
        liftHeight = Mathf.Max(0.1f, liftHeight);
        resultDelay = Mathf.Max(0f, resultDelay);
        duration = Mathf.Max(0.1f, duration);
        closeDuration = Mathf.Max(0.1f, closeDuration);
        switchDuration = Mathf.Max(0f, switchDuration);
    }
}
