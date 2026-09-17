using UnityEngine;

/// <summary>
/// 투척된 파워펠릿. 발사 방향으로 날아가다 무엇에든 부딪히면 사라진다.
/// 고스트에 맞았을 때의 처치 판정(타이밍 유효 여부)은 고스트 쪽 컴포넌트가 담당하고,
/// 여기서는 충돌 통지만 넘긴다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrownPellet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 고스트 콜라이더가 트리거이므로(플레이어 피격 감지용), 이 경로로도 명중을 잡아야 한다.
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // 고스트에 맞았다면 고스트 쪽에서 처치 판정을 처리하도록 알린다.
        var ghost = hitObject.GetComponentInParent<GhostHittable>();
        if (ghost != null)
        {
            ghost.HandlePelletHit();
        }

        // 명중/빗나감 상관없이 펠릿은 소멸한다 (회수 불가).
        Destroy(gameObject);
    }
}
