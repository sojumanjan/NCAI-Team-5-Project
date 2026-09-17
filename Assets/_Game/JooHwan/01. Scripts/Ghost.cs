using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 고스트 공통 로직. NavMeshAgent로 이동하며 순찰/추격 상태를 오가고,
/// 발광 주기(공격 신호)에 따라 파워펠릿 피격 유효 여부가 바뀐다.
/// 순찰 경로 자체는 GhostPatrolLoop/GhostPingPong 같은 별도 컴포넌트가 제공한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Ghost : GhostHittable
{
    [Header("이동 속도")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("플레이어 탐지")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float detectAngle = 100f;
    [SerializeField] private float loseSightRange = 9f;

    [Header("발광(공격 신호) 주기")]
    [Tooltip("평소 약한 발광 상태로 유지되는 시간 (이 동안은 피격 무효)")]
    [SerializeField] private float dimDuration = 3f;
    [Tooltip("강한 발광 상태로 유지되는 시간 (이 동안만 피격 유효)")]
    [SerializeField] private float glowDuration = 1.5f;

    [Header("비주얼")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Color dimColor = new Color(0.6f, 0.15f, 0.15f, 1f);
    [Tooltip("발광(공격 신호) 중 고스트 고유 색조(Hue)는 그대로 유지하고, 채도/명도만 이 값으로 끌어올려 '자기 색 그대로 더 밝고 진하게' 빛나게 한다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowSaturation = 0.9f;
    [Range(0f, 3f)]
    [SerializeField] private float glowBrightness = 2.2f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private GhostDefeatEffect defeatEffect;
    [Tooltip("발광(타격 가능) 중 켜지는 오라 파티클. 몸이 전부 노란빛으로 바뀌어도 이 색으로 어느 고스트인지 구별할 수 있게, 고스트 원래 색으로 재생한다.")]
    [SerializeField] private ParticleSystem glowAura;

    private NavMeshAgent agent;
    private Transform player;
    private IGhostMovementSource movementSource;
    private Collider ghostCollider;

    private bool isChasing;
    private bool isGlowing;
    private bool isActive;
    private bool isDefeated;
    private Vector3 lastKnownPlayerPosition;
    private Material bodyMaterialInstance;
    private Coroutine glowCycleCoroutine;
    private Color originalBodyColor;

    /// <summary>처치된 순간(디졸브 연출 시작 시점)에 호출된다. 전멸 판정에 사용한다.</summary>
    public System.Action<Ghost> OnDefeated;

    public bool IsDefeated => isDefeated;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        movementSource = GetComponent<IGhostMovementSource>();
        ghostCollider = GetComponent<Collider>();

        if (bodyRenderer != null)
        {
            bodyMaterialInstance = bodyRenderer.material;
            originalBodyColor = bodyMaterialInstance.GetColor("_BaseColor");
        }

        if (glowAura != null)
        {
            var auraMain = glowAura.main;
            auraMain.startColor = originalBodyColor;

            var auraRenderer = glowAura.GetComponent<ParticleSystemRenderer>();
            if (auraRenderer != null && auraRenderer.sharedMaterial != null)
            {
                var matInstance = new Material(auraRenderer.sharedMaterial);
                matInstance.SetColor("_BaseColor", originalBodyColor);
                auraRenderer.material = matInstance;
            }
        }
    }

    private void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
        }

        agent.speed = patrolSpeed;

        // 팩맨 상태가 활성화되기 전(예: 테트리스 클리어 직후, 문 앞 복도)에는
        // 고스트가 움직이면 안 되므로, 기본값은 정지 상태로 시작한다.
        SetActive(false);
    }

    /// <summary>
    /// 팩맨 게임 활성 여부에 맞춰 고스트의 이동/발광 사이클을 켜고 끈다.
    /// MiniGameFlowManager가 Pacman 상태로 전환될 때 호출한다.
    /// enterIdlePose가 true면(팩맨 종료 등) 대기 지점으로 워프해 자세를 잡고,
    /// false면(파워펠릿에 처치된 순간 등) 위치는 그대로 둔 채 이동/발광만 멈춘다.
    /// </summary>
    public void SetActive(bool active, bool enterIdlePose = true)
    {
        isActive = active;
        agent.isStopped = !active;

        if (active)
        {
            if (glowCycleCoroutine == null)
            {
                glowCycleCoroutine = StartCoroutine(GlowCycleRoutine());
            }
        }
        else
        {
            isChasing = false;
            isGlowing = false;

            if (glowCycleCoroutine != null)
            {
                StopCoroutine(glowCycleCoroutine);
                glowCycleCoroutine = null;
            }

            if (glowAura != null)
            {
                glowAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (enterIdlePose)
            {
                // GhostPingPong처럼 "대기 지점에서 플레이어 쪽을 바라보며 서 있는" 연출을 지원하는
                // 이동 소스라면, 비활성화 시 그 자세를 취하게 한다 (없으면 아무 동작 안 함).
                var pingPong = movementSource as GhostPingPong;
                if (pingPong != null)
                {
                    pingPong.EnterIdleState(agent);
                }
            }
        }
    }

    /// <summary>
    /// 팩맨 진입이 확정된 시점(플레이어 텔레포트와 같은 타이밍, 카운트다운 전)에 호출한다.
    /// 실제로 움직이기 시작하지는 않고, 대기 위치에서 순찰 시작 위치로 워프만 해둔다.
    /// 화면이 어두운 상태(FadeOut 직후)에 호출되어야 워프가 보이지 않는다.
    /// </summary>
    public void PrepareForPacman()
    {
        if (movementSource != null)
        {
            movementSource.BeginPatrol(agent);
        }

        // BeginPatrol이 SetDestination까지 호출하지만, 아직 실제로 움직이면 안 되므로 다시 멈춘다.
        agent.isStopped = true;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        UpdateDetection();
        UpdatePulseVisual();

        if (!isChasing && movementSource != null)
        {
            movementSource.TickPatrol(agent);
        }
    }

    private void UpdateDetection()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isChasing)
        {
            if (distance <= detectRange && IsPlayerInSight())
            {
                StartChase();
            }
        }
        else
        {
            lastKnownPlayerPosition = player.position;
            agent.SetDestination(lastKnownPlayerPosition);

            if (distance > loseSightRange)
            {
                StopChase();
            }
        }
    }

    private bool IsPlayerInSight()
    {
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        return angle <= detectAngle * 0.5f;
    }

    private void StartChase()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
    }

    private void StopChase()
    {
        isChasing = false;
        agent.speed = patrolSpeed;

        if (movementSource != null)
        {
            movementSource.ResumePatrolFromCurrentPosition(agent);
        }
    }

    private IEnumerator GlowCycleRoutine()
    {
        while (true)
        {
            isGlowing = false;
            if (glowAura != null)
            {
                glowAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            yield return new WaitForSeconds(dimDuration);

            isGlowing = true;
            if (glowAura != null)
            {
                glowAura.Play();
            }
            yield return new WaitForSeconds(glowDuration);
        }
    }

    private void UpdatePulseVisual()
    {
        if (bodyMaterialInstance == null)
        {
            return;
        }

        Color baseColor;

        if (isGlowing)
        {
            // 파스텔처럼 채도가 낮은 색을 그대로 밝기만 높이면 흰색으로 날아가 버리므로,
            // HSV로 변환해 색조(Hue)는 완전히 그대로 유지한 채 채도/명도만 끌어올려
            // "이 고스트만의 색 그대로 더 밝고 진하게" 빛나게 한다.
            Color.RGBToHSV(originalBodyColor, out float h, out float s, out float v);

            baseColor = Color.HSVToRGB(h, glowSaturation, 1f);
            baseColor *= glowBrightness;
            baseColor.a = 1f;
        }
        else
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            baseColor = Color.Lerp(dimColor * 0.6f, dimColor, pulse);
        }

        bodyMaterialInstance.SetColor("_EmissionColor", baseColor);
        bodyMaterialInstance.EnableKeyword("_EMISSION");
    }

    /// <summary>파워펠릿에 맞았을 때 호출된다. 발광 중(타이밍 유효)일 때만 처치된다.</summary>
    public override void OnHitByPellet()
    {
        if (!isGlowing || isDefeated)
        {
            return;
        }

        isDefeated = true;
        OnDefeated?.Invoke(this);

        // 디졸브 연출(수 초)이 끝날 때까지 GameObject는 활성 상태로 남아있어야 하지만,
        // 그동안 콜라이더까지 살아있으면 연출 중에 플레이어가 지나가다 피격당하므로 즉시 꺼준다.
        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        // 맞은 즉시 움직임/추격/충돌은 멈추되, 화면에서는 디졸브 연출이 끝난 뒤에 사라진다.
        // 죽은 그 자리에서 연출이 재생되어야 하므로, 대기 지점으로 워프하는 EnterIdleState는 건너뛴다.
        SetActive(false, enterIdlePose: false);

        if (defeatEffect != null)
        {
            defeatEffect.Play(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 팩맨 재시작(사망 팝업의 재시작, 또는 팩맨 최초 진입) 시 호출한다.
    /// 처치되어 있었다면 되살리고, 대기 자세로 되돌린다.
    /// </summary>
    public void ResetForRestart()
    {
        isDefeated = false;
        gameObject.SetActive(true);

        if (ghostCollider != null)
        {
            ghostCollider.enabled = true;
        }

        if (defeatEffect != null)
        {
            defeatEffect.ResetVisual();
        }

        SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 발광 상태와 무관하게 접촉하면 플레이어가 피격당한다.
        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeHit();
        }
    }
}
