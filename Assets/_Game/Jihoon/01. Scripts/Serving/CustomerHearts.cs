using DG.Tweening;
using UnityEngine;

/// <summary>
/// 주문을 제대로 받은 손님 머리 위로 하트를 몇 개 띄운다.
///
/// 파티클 시스템 대신 스프라이트를 복제해 트윈으로 날리는 이유는, 프리팹에 이미 붙어 있는
/// 하트 그림을 그대로 쓰기 위해서다. 머티리얼·렌더 순서·URP 설정을 따로 맞출 일이 없고,
/// 개수·속도·퍼지는 정도를 인스펙터 숫자 몇 개로 바로 만질 수 있다.
///
/// <see cref="CustomerAppearance"/>와 같은 분업이다 — 손님의 단계 하나만 읽는 순수한 뷰라서
/// 이 컴포넌트를 떼어내도 서빙 로직은 그대로 돈다.
/// </summary>
public class CustomerHearts : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("상태를 볼 손님. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private Customer customer;

    [Tooltip("복제할 하트. 프리팹에 붙여둔 Heart를 넣으세요. 원본은 시작할 때 꺼집니다.")]
    [SerializeField] private SpriteRenderer heartTemplate;

    [Header("개수와 간격")]
    [Tooltip("한 번에 띄울 하트 수.")]
    [SerializeField] private int heartCount = 5;

    [Tooltip("하트끼리 터지는 간격 (초). 한꺼번에 나오면 뭉쳐 보입니다.")]
    [SerializeField] private float spawnInterval = 0.12f;

    [Tooltip("하트 하나가 뜨고 사라지기까지 (초). 마지막 하트가 손님의 기뻐하는 시간 " +
             "안에 끝나도록 맞추세요.")]
    [SerializeField] private float riseDuration = 1.3f;

    [Header("날아가는 방향")]
    [Tooltip("위로 떠오르는 높이 (m).")]
    [SerializeField] private float riseHeight = 1.2f;

    [Tooltip("화면 오른쪽으로 밀려나는 거리 (m). 음수면 왼쪽입니다.")]
    [SerializeField] private float sideDrift = 0.8f;

    [Tooltip("하트마다 목적지를 흩뜨리는 정도 (m). 0이면 전부 같은 곳으로 갑니다.")]
    [SerializeField] private float spread = 0.35f;

    [Tooltip("터지기 시작하는 지점을 흩뜨리는 정도 (m).")]
    [SerializeField] private float startJitter = 0.12f;

    [Header("크기와 투명도")]
    [Tooltip("최대로 커졌을 때의 배율. 원본 크기 기준입니다.")]
    [SerializeField] private float peakScale = 1f;

    [Tooltip("튀어나오며 커지는 시간 (초).")]
    [SerializeField] private float popDuration = 0.25f;

    [Tooltip("전체 시간 중 언제부터 흐려지기 시작할지 (0~1). 0.35면 3분의 1쯤 지나서부터입니다.")]
    [Range(0f, 0.95f)]
    [SerializeField] private float fadeStart01 = 0.35f;

    [Tooltip("떠오르는 곡선. OutCubic이면 처음에 빠르게 솟았다가 잦아듭니다.")]
    [SerializeField] private Ease riseEase = Ease.OutCubic;

    [Tooltip("기울어지는 최대 각도. 하트마다 무작위로 조금씩 눕습니다.")]
    [SerializeField] private float tiltDegrees = 20f;

    private bool _burst;
    private Camera _camera;

    private void Awake()
    {
        if (customer == null)
        {
            customer = GetComponentInParent<Customer>();
        }

        if (customer == null || heartTemplate == null)
        {
            Debug.LogError($"{nameof(CustomerHearts)} on '{name}': 손님과 하트 원본이 모두 필요합니다.", this);
            enabled = false;
            return;
        }

        // 원본은 복제용 틀일 뿐이다. 켜둔 채로 두면 손님이 하트를 하나 달고 걸어 다닌다.
        heartTemplate.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 한 손님은 한 번만 기뻐한다. 단계를 매 프레임 보고 있으므로 걸쇠가 필요하다.
        if (_burst || !customer.IsHappy)
        {
            return;
        }

        _burst = true;
        Burst();
    }

    // ---------------------------------------------------------------- 연출

    /// <summary>하트를 순서대로 하나씩 띄운다.</summary>
    public void Burst()
    {
        for (int i = 0; i < heartCount; i++)
        {
            LaunchOne(i * spawnInterval);
        }
    }

    private void LaunchOne(float delay)
    {
        Camera cam = ResolveCamera();

        // 카메라 기준 오른쪽을 쓴다. 손님이 어느 쪽을 보고 서 있든 플레이어 눈에는 항상
        // 오른쪽 위로 퍼지는 그림이 된다.
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

        Vector3 origin = heartTemplate.transform.position + Random.insideUnitSphere * startJitter;
        Vector3 target = origin
                         + Vector3.up * riseHeight
                         + right * sideDrift
                         + right * Random.Range(-spread, spread)
                         + Vector3.up * Random.Range(-spread, spread);

        GameObject heart = Instantiate(heartTemplate.gameObject, origin, Quaternion.identity);
        heart.name = "Heart (Burst)";
        heart.SetActive(true);

        Transform t = heart.transform;

        // 손님의 자식으로 두지 않는다. 하트가 다 뜨기 전에 손님이 걸어 나가기 시작하면
        // 하트가 손님을 따라다니고, 손님이 사라질 때 같이 지워진다.
        Vector3 fullScale = heartTemplate.transform.lossyScale * peakScale;
        t.localScale = Vector3.zero;

        // 각도를 한 번만 맞추면 플레이어가 고개를 돌리는 순간 비스듬해진다. 하트가 손님보다
        // 오래 살 수 있으므로 자기가 직접 들고 다니게 한다.
        SpriteBillboard billboard = heart.GetComponent<SpriteBillboard>();
        if (billboard == null)
        {
            billboard = heart.AddComponent<SpriteBillboard>();
        }

        billboard.SetTilt(Random.Range(-tiltDegrees, tiltDegrees));

        SpriteRenderer sprite = heart.GetComponent<SpriteRenderer>();

        Sequence seq = DOTween.Sequence().SetLink(heart);
        seq.Insert(delay, t.DOScale(fullScale, popDuration).SetEase(Ease.OutBack));
        seq.Insert(delay, t.DOMove(target, riseDuration).SetEase(riseEase));

        if (sprite != null)
        {
            float fadeAt = riseDuration * fadeStart01;
            seq.Insert(delay + fadeAt, sprite.DOFade(0f, Mathf.Max(0.01f, riseDuration - fadeAt))
                                             .SetEase(Ease.InQuad));
        }

        seq.OnComplete(() => Destroy(heart));
    }

    private Camera ResolveCamera()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        return _camera;
    }

    private void OnValidate()
    {
        heartCount = Mathf.Max(0, heartCount);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        riseDuration = Mathf.Max(0.05f, riseDuration);
        popDuration = Mathf.Clamp(popDuration, 0.01f, riseDuration);
        peakScale = Mathf.Max(0.01f, peakScale);
        spread = Mathf.Max(0f, spread);
        startJitter = Mathf.Max(0f, startJitter);
    }
}
