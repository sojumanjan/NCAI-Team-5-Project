using DG.Tweening;
using UnityEngine;

/// <summary>
/// 손님이 어떤 몸으로 보일지 정한다. 평상시에는 준비된 외형 중 하나를 무작위로 골라 손님이
/// 매번 같아 보이지 않게 하고, 주문을 망치거나 기다리다 지치면 화난 몸으로 갈아입는다.
///
/// 외형을 <see cref="Customer"/>에 넣지 않은 이유는 그쪽이 이동과 인내심만 알면 되기
/// 때문이다. 여기는 <see cref="Customer.IsAngry"/> 하나만 읽는 순수한 뷰다 —
/// 인내심 게이지와 같은 분업이다.
///
/// 몸들은 손님 루트의 자식으로 나란히 두고, 전부 같은 위치·크기로 맞춰두면 된다.
/// </summary>
public class CustomerAppearance : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("상태를 볼 손님. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private Customer customer;

    [Tooltip("뒤뚱거림. 켜진 몸을 흔들도록 알려줍니다. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private CustomerWaddle waddle;

    [Header("외형")]
    [Tooltip("평상시 몸들. 이 중 하나를 무작위로 골라 켭니다.")]
    [SerializeField] private Transform[] normalModels;

    [Tooltip("화났을 때의 몸. 실패하거나 손님이 지쳐 떠날 때 이걸로 바뀝니다.")]
    [SerializeField] private Transform angryModel;

    [Header("화내는 연출")]
    [Tooltip("제자리에서 튕길 때 커지는 배율. 1.15면 15% 부풀었다 돌아옵니다.")]
    [SerializeField] private float bounceScale = 1.15f;

    [Tooltip("한 번 튕기는 데 걸리는 시간 (초). 짧을수록 다급합니다.")]
    [SerializeField] private float bounceDuration = 0.16f;

    [Tooltip("튕기는 곡선. OutBack이면 끝에서 한 번 더 튀어 만화 같습니다.")]
    [SerializeField] private Ease bounceEase = Ease.OutBack;

    [Header("기뻐하는 연출")]
    [Tooltip("만족했을 때 부푸는 배율. 화날 때보다 작게 두면 들뜬 정도로 읽힙니다.")]
    [SerializeField] private float happyScale = 1.08f;

    [Tooltip("한 번 부풀었다 돌아오는 시간 (초). 길수록 느긋합니다.")]
    [SerializeField] private float happyDuration = 0.32f;

    [Tooltip("기뻐하는 곡선. InOutSine이면 숨 쉬듯 부드럽습니다.")]
    [SerializeField] private Ease happyEase = Ease.InOutSine;

    private Transform _active;
    private Vector3 _restScale = Vector3.one;
    private bool _angryShown;

    // 무엇으로 튕기고 있는지까지 기억해야 한다. 참/거짓만 보면 화내기와 기뻐하기가 서로
    // 갈아탈 때 트윈이 바뀌지 않는다.
    private BounceKind _bouncing = BounceKind.None;
    private Tween _bounce;

    private enum BounceKind
    {
        None,
        Angry,
        Happy,
    }

    /// <summary>지금 켜져 있는 몸.</summary>
    public Transform ActiveModel => _active;

    private void Awake()
    {
        if (customer == null)
        {
            customer = GetComponentInParent<Customer>();
        }

        if (waddle == null)
        {
            waddle = GetComponent<CustomerWaddle>();
        }

        if (customer == null)
        {
            Debug.LogError($"{nameof(CustomerAppearance)} on '{name}': {nameof(Customer)}를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        // 첫 프레임부터 하나만 보여야 하므로 Awake에서 고른다.
        Show(PickNormal());
    }

    private void Start()
    {
        // 뒤뚱거림 연결은 Start에서. Awake 순서는 보장되지 않아, 상대가 아직 자기 필드를
        // 정리하기 전에 알려주면 덮어써질 수 있다.
        NotifyWaddle();
    }

    private void OnDestroy()
    {
        // 트윈은 대상보다 오래 산다. 파괴된 트랜스폼에 쓰려 들면 씬을 내릴 때 터진다.
        StopBounce();
    }

    private void Update()
    {
        if (!_angryShown && customer.IsAngry)
        {
            _angryShown = true;

            if (angryModel != null)
            {
                Show(angryModel);
                NotifyWaddle();
            }
        }

        if (customer.IsFuming)
        {
            SetBouncing(BounceKind.Angry);
        }
        else if (customer.IsHappy)
        {
            SetBouncing(BounceKind.Happy);
        }
        else
        {
            SetBouncing(BounceKind.None);
        }
    }

    // ---------------------------------------------------------------- 화내는 연출

    /// <summary>
    /// 제자리에 머무는 동안 몸을 통통 부풀린다. 지속 시간을 따로 두지 않고 손님의 단계를
    /// 그대로 따라가므로, 얼마나 오래 하는지는 손님 쪽 값 하나로 정해진다.
    /// </summary>
    private void SetBouncing(BounceKind kind)
    {
        if (kind == _bouncing)
        {
            return;
        }

        _bouncing = kind;
        StopBounce();

        if (kind == BounceKind.None || _active == null)
        {
            return;
        }

        float scale = kind == BounceKind.Angry ? bounceScale : happyScale;
        float duration = kind == BounceKind.Angry ? bounceDuration : happyDuration;
        Ease ease = kind == BounceKind.Angry ? bounceEase : happyEase;

        if (scale <= 1f || duration <= 0f)
        {
            return;
        }

        _restScale = _active.localScale;
        _bounce = _active.DOScale(_restScale * scale, duration)
                         .SetEase(ease)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetLink(gameObject);
    }

    private void StopBounce()
    {
        if (_bounce == null)
        {
            return;
        }

        _bounce.Kill();
        _bounce = null;

        if (_active != null)
        {
            _active.localScale = _restScale;
        }
    }

    // ---------------------------------------------------------------- 내부

    private Transform PickNormal()
    {
        if (normalModels == null || normalModels.Length == 0)
        {
            return null;
        }

        // 비어 있는 칸을 건너뛰고 세야, 인스펙터에 null이 섞여 있어도 아무도 안 나오는
        // 일이 생기지 않는다.
        int usable = 0;
        foreach (Transform model in normalModels)
        {
            if (model != null)
            {
                usable++;
            }
        }

        if (usable == 0)
        {
            return null;
        }

        int chosen = Random.Range(0, usable);
        foreach (Transform model in normalModels)
        {
            if (model == null)
            {
                continue;
            }

            if (chosen == 0)
            {
                return model;
            }

            chosen--;
        }

        return null;
    }

    /// <summary>하나만 켜고 나머지는 전부 끈다.</summary>
    private void Show(Transform target)
    {
        // 몸을 갈아입기 전에 튕김을 정리해야 이전 몸이 부푼 채로 꺼지지 않는다. 종류까지
        // 비워야 새 몸에서 같은 연출이 다시 시작된다.
        StopBounce();
        _bouncing = BounceKind.None;

        _active = target;
        _restScale = target != null ? target.localScale : Vector3.one;

        if (normalModels != null)
        {
            foreach (Transform model in normalModels)
            {
                if (model != null)
                {
                    model.gameObject.SetActive(model == target);
                }
            }
        }

        if (angryModel != null)
        {
            angryModel.gameObject.SetActive(angryModel == target);
        }
    }

    private void NotifyWaddle()
    {
        if (waddle != null && _active != null)
        {
            waddle.SetModel(_active);
        }
    }
}
