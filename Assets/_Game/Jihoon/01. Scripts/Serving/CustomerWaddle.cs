using DG.Tweening;
using UnityEngine;

/// <summary>
/// 걷는 동안 몸을 좌우로 기울이고 살짝 통통 튀게 만든다. 손님 모델에 애니메이션이 없어서
/// 가만히 미끄러져 오는 것을 가리는 용도다.
///
/// 손님 루트가 아니라 <see cref="model"/>로 지정한 자식을 움직인다. 루트는
/// <see cref="Customer"/>가 매 프레임 위치와 회전을 덮어쓰기 때문에, 거기에 트윈을 걸면
/// 서로 밀어내며 떨린다.
///
/// 상태가 바뀔 때마다 트윈을 새로 만든다. 일시정지해뒀다 되살리면 내부 경과 시간이 남아
/// 이상한 자세에서 튀어 시작하는데, 손님 한 명이 걷기/멈춤을 두어 번밖에 안 하므로
/// 다시 만드는 편이 싸고 확실하다.
/// </summary>
public class CustomerWaddle : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("걷는지 볼 손님. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private Customer customer;

    [Tooltip("실제로 흔들 모델. 손님 루트를 넣으면 이동과 싸우므로 반드시 자식을 넣으세요.")]
    [SerializeField] private Transform model;

    [Header("뒤뚱거림")]
    [Tooltip("좌우로 기우는 각도 (도).")]
    [SerializeField] private float tiltAngle = 8f;

    [Tooltip("기울 축. 보통 앞뒤 축을 중심으로 굴려야 좌우로 기웁니다.")]
    [SerializeField] private Vector3 tiltAxis = Vector3.forward;

    [Tooltip("한쪽으로 기우는 데 걸리는 시간 (초). 짧을수록 종종걸음.")]
    [SerializeField] private float stepDuration = 0.28f;

    [Tooltip("위아래로 통통 뛰는 높이 (m). 0이면 기울기만 합니다.")]
    [SerializeField] private float bobHeight = 0.04f;

    [Tooltip("멈출 때 원래 자세로 돌아가는 시간 (초).")]
    [SerializeField] private float settleDuration = 0.18f;

    private Quaternion _restRotation;
    private Vector3 _restPosition;
    private Tween _tilt;
    private Tween _bob;
    private bool _walking;

    private void Awake()
    {
        if (customer == null)
        {
            customer = GetComponentInParent<Customer>();
        }

        if (model == null)
        {
            model = FindModel();
        }

        if (customer == null)
        {
            Debug.LogError($"{nameof(CustomerWaddle)} on '{name}': {nameof(Customer)}를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        CaptureRest();
    }

    /// <summary>
    /// 흔들 모델을 바꾼다. 손님 외형이 여러 벌이라 어느 몸이 켜져 있는지는
    /// <see cref="CustomerAppearance"/>가 정하고, 여기는 그걸 따라간다.
    /// </summary>
    public void SetModel(Transform next)
    {
        if (next == model)
        {
            return;
        }

        KillTweens();

        // 쓰던 몸은 원래 자세로 돌려놓는다. 기울어진 채로 꺼지면 다시 켤 때 그대로다.
        if (model != null)
        {
            model.localRotation = _restRotation;
            model.localPosition = _restPosition;
        }

        model = next;
        CaptureRest();

        // 다음 Update에서 걷는지 다시 판단하게 만든다.
        _walking = false;
    }

    private void CaptureRest()
    {
        if (model == null)
        {
            return;
        }

        _restRotation = model.localRotation;
        _restPosition = model.localPosition;
    }

    private void OnDisable()
    {
        _walking = false;
        KillTweens();
    }

    private void OnDestroy()
    {
        // 트윈은 대상보다 오래 산다. 파괴된 트랜스폼에 쓰려 들면 씬을 내릴 때 터진다.
        KillTweens();
    }

    private void Update()
    {
        if (model == null)
        {
            return;
        }

        SetWalking(customer.IsWalking);
    }

    private void SetWalking(bool walking)
    {
        if (walking == _walking)
        {
            return;
        }

        _walking = walking;
        KillTweens();

        if (walking)
        {
            StartWaddle();
        }
        else
        {
            SettleToRest();
        }
    }

    private void StartWaddle()
    {
        Vector3 axis = tiltAxis.sqrMagnitude > 0.0001f ? tiltAxis.normalized : Vector3.forward;

        Quaternion left = _restRotation * Quaternion.AngleAxis(tiltAngle, axis);
        Quaternion right = _restRotation * Quaternion.AngleAxis(-tiltAngle, axis);

        // 한쪽 끝에서 시작해 반대쪽으로 요요. 가운데(원래 자세)를 그냥 지나간다.
        _tilt = model.DOLocalRotateQuaternion(right, stepDuration)
                     .From(left)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetLink(gameObject);

        if (bobHeight <= 0f)
        {
            return;
        }

        // 기울기의 두 배 빈도로 뛴다. 한 걸음마다 한 번 올라오는 셈.
        _bob = model.DOLocalMoveY(_restPosition.y + bobHeight, stepDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
    }

    private void SettleToRest()
    {
        _tilt = model.DOLocalRotateQuaternion(_restRotation, settleDuration)
                     .SetEase(Ease.OutQuad)
                     .SetLink(gameObject);

        _bob = model.DOLocalMoveY(_restPosition.y, settleDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
    }

    private void KillTweens()
    {
        if (_tilt != null)
        {
            _tilt.Kill();
            _tilt = null;
        }

        if (_bob != null)
        {
            _bob.Kill();
            _bob = null;
        }
    }

    /// <summary>자식 중 메시가 달린 첫 가지. 머리 위 캔버스는 흔들면 안 되므로 건너뛴다.</summary>
    private Transform FindModel()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child.GetComponent<Canvas>() == null && child.GetComponentInChildren<Renderer>(true) != null)
            {
                return child;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        tiltAngle = Mathf.Max(0f, tiltAngle);
        stepDuration = Mathf.Max(0.05f, stepDuration);
        bobHeight = Mathf.Max(0f, bobHeight);
        settleDuration = Mathf.Max(0.02f, settleDuration);
    }
}
