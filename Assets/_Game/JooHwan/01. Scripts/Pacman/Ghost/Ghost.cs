using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 고스트 공통 로직. NavMeshAgent로 이동하며 순찰/추격 상태를 오가고,
/// 발광 주기(공격 신호)에 따라 파워펠릿 피격 유효 여부가 바뀐다.
/// 순찰 경로 자체는 GhostPatrolLoop/GhostPingPong 같은 별도 컴포넌트가 제공한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Ghost : MonoBehaviour
{
    [Header("이동 속도")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [Tooltip("추격 시작 순간 목표 속도(chaseSpeed)까지 가속하는 속도. NavMeshAgent 기본 acceleration(8)로는 speed를 아무리 올려도 서서히 가속되어 '순간적으로 확 쫓아오는' 느낌이 나지 않는다. 순찰 가속도보다 훨씬 크게 잡아야 즉각적으로 느껴진다.")]
    [SerializeField] private float chaseAcceleration = 200f;
    [SerializeField] private float patrolAcceleration = 8f;

    [Header("플레이어 탐지")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float detectAngle = 100f;
    [SerializeField] private float loseSightRange = 9f;
    [Tooltip("이 레이어(벽)에 시야가 가로막히면 거리/각도 조건을 만족해도 플레이어를 인식하지 못한다.")]
    [SerializeField] private LayerMask wallMask;
    [Tooltip("시야 판정용 Linecast의 시작/끝 높이 오프셋. 바닥에 딱 붙은 높이로 쏘면 벽 하단 경계에서 오탐이 생길 수 있어 몸통 높이만큼 띄운다.")]
    [SerializeField] private float sightHeightOffset = 1f;

    [Header("피격 후 스턴")]
    [Tooltip("플레이어를 접촉 피격한 직후 그 자리에 완전히 멈춰 서는 시간. 가속이 붙은 추격을 플레이어가 따돌릴 틈을 준다.")]
    [SerializeField] private float hitStunDuration = 1f;

    [Header("발광(공격 신호) 주기")]
    [Tooltip("평소 약한 발광 상태로 유지되는 시간 (이 동안은 피격 무효)")]
    [SerializeField] private float dimDuration = 3f;
    [Tooltip("강한 발광 상태로 유지되는 시간 (이 동안만 피격 유효)")]
    [SerializeField] private float glowDuration = 1.5f;

    [Header("비주얼")]
    [SerializeField] private Renderer bodyRenderer;
    [Tooltip("발광(공격 신호) 중 사용할 이 고스트의 기준색(보통 몸통 텍스처의 실제 색과 같은 톤으로 맞춘다). " +
        "평소(비발광) 상태는 이 색과 무관하게 _EmissionColor를 꺼서 텍스처 원본 색이 그대로 보이게 한다.")]
    [SerializeField] private Color glowBaseColor = new Color(0.9f, 0.42f, 0.41f, 1f);
    [Tooltip("발광(공격 신호) 중 고스트 고유 색조(Hue)는 그대로 유지하고, 채도/명도만 이 값으로 끌어올려 '자기 색 그대로 더 밝고 진하게' 빛나게 한다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowSaturation = 0.9f;
    [Range(0f, 3f)]
    [SerializeField] private float glowBrightness = 2.2f;
    [SerializeField] private GhostDefeatEffect defeatEffect;
    [Tooltip("발광(타격 가능) 중 켜지는 오라 파티클. 고스트 고유색(glowBaseColor)으로 재생한다.")]
    [SerializeField] private ParticleSystem glowAura;

    [Header("사운드")]
    [Tooltip("활성 상태(처치 전)인 동안 상시 재생되는 이동음. 플레이어와의 거리에 따라 볼륨이 줄어든다.")]
    [SerializeField] private SoundData crackleSound;
    [Tooltip("파워펠릿에 맞아 실제로 처치되는 순간 재생되는 사운드.")]
    [SerializeField] private SoundData defeatSound;
    [Tooltip("발광(공격 가능) 중에만 재생되는 경고음. 발광이 끝나는 순간 재생 중이어도 강제로 멈춘다.")]
    [SerializeField] private SoundData glowWarningSound;

    private NavMeshAgent agent;
    private Transform player;
    private IGhostMovementSource movementSource;
    private Collider ghostCollider;

    private bool isChasing;
    private bool isGlowing;
    private bool isActive;
    private bool isDefeated;
    private bool isStunned;
    private Vector3 lastKnownPlayerPosition;
    private Material bodyMaterialInstance;
    private Coroutine glowCycleCoroutine;
    private Coroutine hitStunCoroutine;
    private SoundHandle crackleHandle;
    private SoundHandle glowWarningHandle;

    /// <summary>처치된 순간(디졸브 연출 시작 시점)에 발생한다. 전멸 판정에 사용한다.</summary>
    public System.Action<Ghost> Defeated;

    public bool IsDefeated => isDefeated;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        movementSource = GetComponent<IGhostMovementSource>();
        ghostCollider = GetComponent<Collider>();

        if (bodyRenderer != null)
        {
            bodyMaterialInstance = bodyRenderer.material;
        }

        // 평소(비발광) 상태에서는 텍스처 원본 색이 그대로 보이도록 발광을 꺼둔다.
        if (bodyMaterialInstance != null)
        {
            bodyMaterialInstance.SetColor("_EmissionColor", Color.black);
            bodyMaterialInstance.DisableKeyword("_EMISSION");
        }

        if (glowAura != null)
        {
            ParticleSystem.MainModule auraMain = glowAura.main;
            auraMain.startColor = glowBaseColor;

            var auraRenderer = glowAura.GetComponent<ParticleSystemRenderer>();
            if (auraRenderer != null && auraRenderer.sharedMaterial != null)
            {
                var matInstance = new Material(auraRenderer.sharedMaterial);
                matInstance.SetColor("_BaseColor", glowBaseColor);
                auraRenderer.material = matInstance;
            }
        }

        // ladybug 모델은 _BaseColor가 흰색으로 남아있어, GhostDefeatEffect가 그대로 읽으면
        // 처치 파티클이 전부 흰색으로 나온다. 발광과 동일한 고유색(glowBaseColor)을 넘겨준다.
        if (defeatEffect != null)
        {
            defeatEffect.SetDefeatColor(glowBaseColor);
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
        agent.acceleration = patrolAcceleration;

        // 팩맨 상태가 활성화되기 전(예: 테트리스 클리어 직후, 문 앞 복도)에는
        // 고스트가 움직이면 안 되므로, 기본값은 정지 상태로 시작한다.
        SetActive(false);
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        UpdatePulseVisual();

        // 스턴 중에는 탐지/추격 자체를 멈춰 그 자리에 완전히 서 있게 한다.
        if (isStunned)
        {
            return;
        }

        UpdateDetection();

        if (!isChasing && movementSource != null)
        {
            movementSource.TickPatrol(agent);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitPlayer(other);
    }

    /// <summary>
    /// 플레이어가 가만히 있어 겹친 상태가 그대로 유지되면 OnTriggerEnter는 최초 한 번만 불리고
    /// 다시는 안 들어온다. 그래서 스턴이 끝난 뒤에도 계속 겹쳐있으면 다시 공격이 들어가도록
    /// 매 프레임(물리 틱) 겹침을 추가로 검사한다. 실제 피격 간격은 PacmanPlayerHealth의
    /// 무적시간이, 이번 히트가 유효한지는 스턴 상태가 각각 걸러준다.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        TryHitPlayer(other);
    }

    private void TryHitPlayer(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 스턴 중에는 공격 판정 자체가 없다. 스턴이 끝난 뒤 여전히 겹쳐있으면
        // OnTriggerStay가 이어서 호출되므로, 그 시점에 다시 정상적으로 공격이 들어간다.
        // isActive도 함께 봐야 하는 이유: SetGhostsPaused(true)(사망/일시정지)로 멈춘 동안에도
        // 스턴 코루틴 자체는 시간이 지나면 isStunned를 false로 되돌리므로, 이걸로만 막으면
        // 멈춘 고스트에 계속 깔려있을 때 공격 판정이 다시 열려버린다.
        if (isStunned || !isActive)
        {
            return;
        }

        // 발광 상태와 무관하게 접촉하면 플레이어가 피격당한다.
        var playerHealth = other.GetComponentInParent<PacmanPlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeHit();
            StartHitStun();
        }
    }

    /// <summary>
    /// 플레이어를 피격한 직후, 가속이 붙은 추격에서 플레이어가 벗어날 틈을 주기 위해
    /// 고스트를 그 자리에 완전히 멈춰 세운다. 스턴이 끝나면 무조건 순찰 상태로 복귀하며,
    /// 이후 탐지 범위 내에 있으면 다시 추격으로 전환될 수 있다.
    /// </summary>
    private void StartHitStun()
    {
        if (hitStunCoroutine != null)
        {
            StopCoroutine(hitStunCoroutine);
        }

        hitStunCoroutine = StartCoroutine(HitStunRoutine());
    }

    private IEnumerator HitStunRoutine()
    {
        isStunned = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        StopChase();

        yield return new WaitForSeconds(hitStunDuration);

        // 스턴 도중 SetGhostsPaused(true)(팩맨 사망/ESC 일시정지 등)로 비활성화됐을 수 있다.
        // 그 경우 스턴이 끝났다고 무조건 움직임을 풀어버리면 일시정지가 무시되므로,
        // 현재도 여전히 활성 상태일 때만 다시 움직이게 한다.
        agent.isStopped = !isActive;
        isStunned = false;
        hitStunCoroutine = null;
    }

    /// <summary>
    /// 팩맨 게임 활성 여부에 맞춰 고스트의 이동/발광 사이클을 켜고 끈다.
    /// MiniGameFlowManager가 Pacman 상태로 전환될 때 호출한다.
    /// shouldEnterIdlePose가 true면(팩맨 종료 등) 대기 지점으로 워프해 자세를 잡고,
    /// false면(파워펠릿에 처치된 순간 등) 위치는 그대로 둔 채 이동/발광만 멈춘다.
    /// </summary>
    public void SetActive(bool isActive, bool shouldEnterIdlePose = true)
    {
        this.isActive = isActive;
        agent.isStopped = !isActive;

        if (isActive)
        {
            if (glowCycleCoroutine == null)
            {
                glowCycleCoroutine = StartCoroutine(GlowCycleRoutine());
            }

            if (!crackleHandle.IsPlaying)
            {
                crackleHandle = AudioManager.PlayAttached(crackleSound, transform);
            }
        }
        else
        {
            isChasing = false;
            isGlowing = false;
            crackleHandle.Stop();
            glowWarningHandle.Stop();

            if (glowCycleCoroutine != null)
            {
                StopCoroutine(glowCycleCoroutine);
                glowCycleCoroutine = null;
            }

            if (hitStunCoroutine != null)
            {
                StopCoroutine(hitStunCoroutine);
                hitStunCoroutine = null;
            }
            isStunned = false;

            if (glowAura != null)
            {
                glowAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (shouldEnterIdlePose)
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

    /// <summary>파워펠릿에 맞았을 때 호출된다. 발광 중(타이밍 유효)일 때만 처치된다.</summary>
    public void HandlePelletHit()
    {
        if (!isGlowing || isDefeated)
        {
            return;
        }

        isDefeated = true;
        Defeated?.Invoke(this);
        AudioManager.PlayAt(defeatSound, transform.position);

        // 디졸브 연출(수 초)이 끝날 때까지 GameObject는 활성 상태로 남아있어야 하지만,
        // 그동안 콜라이더까지 살아있으면 연출 중에 플레이어가 지나가다 피격당하므로 즉시 꺼준다.
        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        // 맞은 즉시 움직임/추격/충돌은 멈추되, 화면에서는 디졸브 연출이 끝난 뒤에 사라진다.
        // 죽은 그 자리에서 연출이 재생되어야 하므로, 대기 지점으로 워프하는 EnterIdleState는 건너뛴다.
        SetActive(false, shouldEnterIdlePose: false);

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
            // 벽에 가려 시야가 끊긴 동안에는 "마지막으로 실제로 보였던 위치"를 갱신하지 않고
            // 그 지점으로만 이동한다. 시야가 다시 확보되면 실시간 위치로 다시 갱신된다.
            if (HasLineOfSight())
            {
                lastKnownPlayerPosition = player.position;
            }
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
        return angle <= detectAngle * 0.5f && HasLineOfSight();
    }

    /// <summary>벽 레이어에 시야가 가로막히지 않았는지 검사한다. 거리/각도 조건과 별개로 항상 만족해야 한다.</summary>
    private bool HasLineOfSight()
    {
        Vector3 eyePosition = transform.position + Vector3.up * sightHeightOffset;
        Vector3 targetPosition = player.position + Vector3.up * sightHeightOffset;
        return !Physics.Linecast(eyePosition, targetPosition, wallMask);
    }

    private void StartChase()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
    }

    private void StopChase()
    {
        isChasing = false;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;

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
            // 클립 길이가 발광 지속시간보다 길 수 있으므로, 재생 여부와 무관하게 강제로 멈춘다.
            glowWarningHandle.Stop();
            yield return new WaitForSeconds(dimDuration);

            isGlowing = true;
            if (glowAura != null)
            {
                glowAura.Play();
            }
            glowWarningHandle = AudioManager.PlayAttached(glowWarningSound, transform);
            yield return new WaitForSeconds(glowDuration);
        }
    }

    private void UpdatePulseVisual()
    {
        if (bodyMaterialInstance == null)
        {
            return;
        }

        if (!isGlowing)
        {
            // 평소(비발광) 상태에서는 발광 자체를 꺼서 텍스처 원본 색이 그대로 보이게 한다.
            bodyMaterialInstance.SetColor("_EmissionColor", Color.black);
            return;
        }

        // 파스텔처럼 채도가 낮은 색을 그대로 밝기만 높이면 흰색으로 날아가 버리므로,
        // HSV로 변환해 색조(Hue)는 완전히 그대로 유지한 채 채도/명도만 끌어올려
        // "이 고스트만의 색 그대로 더 밝고 진하게" 빛나게 한다.
        Color.RGBToHSV(glowBaseColor, out float h, out float s, out float v);

        Color baseColor = Color.HSVToRGB(h, glowSaturation, 1f);
        baseColor *= glowBrightness;
        baseColor.a = 1f;

        bodyMaterialInstance.SetColor("_EmissionColor", baseColor);
        bodyMaterialInstance.EnableKeyword("_EMISSION");
    }
}
