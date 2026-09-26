using DG.Tweening;
using UnityEngine;

namespace Yusong
{
public class EnemyMover : MonoBehaviour
{
    // 뒤뚱거림 정도는 적 프리팹 12종마다 두면 맞추기가 번거로워, EnemySpawner 인스펙터 한 곳에서 정해 여기 넣는다.
    // 보스처럼 스포너 밖에서 생기는 적도 같은 값을 쓴다.
    public static float WaddleAngle = 4f;
    public static float WaddleStepDuration = 0.3f;

    [SerializeField] private float speed = 120f;
    [SerializeField] private Vector2 target = Vector2.zero;
    [SerializeField] private float hitRadius = 80f;
    [SerializeField] private int damageOnHit = 1;

    private RectTransform rt;
    private bool hasHit;
    private Tween waddle;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (WaddleAngle <= 0f || WaddleStepDuration <= 0f) return;

        // 좌우로 번갈아 기우는 것을 반복한다. 게임 시간으로 돌아서 튜토리얼·일시정지 때는 같이 멈춘다.
        rt.localRotation = Quaternion.Euler(0f, 0f, -WaddleAngle);
        waddle = rt.DOLocalRotate(new Vector3(0f, 0f, WaddleAngle), WaddleStepDuration)
                   .SetEase(Ease.InOutSine)
                   .SetLoops(-1, LoopType.Yoyo)
                   .SetLink(gameObject);

        // 모두 같은 박자로 흔들리면 군무처럼 보인다. 적마다 흔들리는 시점을 흩어 둔다.
        waddle.Goto(Random.value * WaddleStepDuration * 2f, true);
    }

    // 웨이브가 끝나 사라지는 동안처럼 움직임을 멈출 때 흔들림도 같이 멈춘다.
    private void OnDisable() => waddle?.Pause();

    private void OnEnable() => waddle?.Play();

    public void AdjustSpeed(float delta)
    {
        speed = Mathf.Max(0f, speed + delta);
    }

    private void Update()
    {
        if (hasHit) return;

        rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, target, speed * Time.deltaTime);

        if (Vector2.Distance(rt.anchoredPosition, target) <= hitRadius)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        hasHit = true;

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.TakeDamage(damageOnHit);
        }

        Destroy(gameObject);
    }
}
}
