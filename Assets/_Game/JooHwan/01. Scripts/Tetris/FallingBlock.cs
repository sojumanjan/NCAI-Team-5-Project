using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingBlock : MonoBehaviour
{
    // 사망 팝업 등으로 게임 전체가 일시정지될 때, 모든 FallingBlock 인스턴스가 함께 멈춘다.
    public static bool IsGlobalPaused;

    [SerializeField] private LayerMask landingMask;
    [SerializeField] private float landingCheckThickness = 0.1f;

    [Header("Pinning (끼임 판정)")]
    [Tooltip("플레이어 캡슐이 셀에 파묻힌 비율(침투 깊이 / 캡슐 높이)이 이 값 이상이면 즉시 사망 처리한다.")]
    [SerializeField] private float pinDeathPenetrationRatio = 0.8f;
    [Tooltip("블록이 착지(바닥 도달)하기까지 이 거리 이내로 남았고, 플레이어가 그 바로 아래에 있으면 위험 경고(비네트)를 시작한다.")]
    [SerializeField] private float dangerWarningDistance = 10f;

    private float fallSpeed = 3f;
    private Rigidbody rb;
    private bool isLanded;

    private Collider[] cellColliders;

    private static PlayerController player;

    // 씬에 동시에 존재하는 모든 블록 중 가장 위험한(가장 큰) 값이 매 프레임 비네트에 반영된다.
    private static float highestDangerRatioThisFrame;
    private static int lastDangerFrame = -1;

    private bool isPlayerOverlapping;
    private float currentPinPenetrationRatio;

    public bool IsLanded => isLanded;

    /// <summary>
    /// 현재 플레이어가 이 블록에 얼마나 파묻혀 있는지(0~1). 비네트 등 사망 피드백 연출에서 참조한다.
    /// </summary>
    public float CurrentPinPenetrationRatio => currentPinPenetrationRatio;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        cellColliders = GetComponentsInChildren<Collider>();

        // 낙하 중에는 트리거로 두어 플레이어를 물리적으로 밀어내지 않고 통과시킨다.
        SetCellsTrigger(true);

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }
    }

    private void FixedUpdate()
    {
        if (IsGlobalPaused)
        {
            return;
        }

        // 씬에 여러 블록이 동시에 존재할 수 있으므로, 새 프레임이 시작될 때(첫 블록이 실행될 때)
        // 한 번만 최댓값을 리셋한다. 순서와 무관하게 모든 블록이 계산을 마친 뒤 마지막에 적용된다.
        if (Time.frameCount != lastDangerFrame)
        {
            lastDangerFrame = Time.frameCount;
            highestDangerRatioThisFrame = 0f;
        }

        // Kinematic Rigidbody의 트리거 콜라이더가 정지한 CharacterController 쪽으로 다가오는 경우
        // OnTriggerEnter가 호출되지 않는 Unity의 제약 때문에, 이벤트에 의존하지 않고
        // 매 프레임 직접 침투 깊이를 검사한다.
        UpdatePinPenetration();
        UpdateDangerRatio();

        if (!isLanded)
        {
            rb.MovePosition(rb.position + Vector3.down * fallSpeed * Time.fixedDeltaTime);

            // 착지 검사는 겹침 여부와 무관하게 항상 실행한다.
            // (겹친 상태에서 착지를 보류하면, 침투가 사망 임계값에 못 미친 채 유지될 경우
            // 블록이 영원히 멈추지 못하고 바닥까지 뚫고 내려가 버린다)
            CheckLanding();
        }

        if (TetrisDangerVignette.Instance != null)
        {
            TetrisDangerVignette.Instance.SetDangerRatio(highestDangerRatioThisFrame);
        }
    }

    public void SetFallSpeed(float speed)
    {
        fallSpeed = speed;
    }

    public void SetLandingMask(LayerMask mask)
    {
        landingMask = mask;
    }

    private void SetCellsTrigger(bool isTrigger)
    {
        foreach (var cell in cellColliders)
        {
            cell.isTrigger = isTrigger;
        }
    }

    /// <summary>
    /// 블록에 플레이어가 겹쳐있는지, 얼마나 겹쳐있는지를 셀별로 직접 계산한다.
    /// 캡슐 높이 대비 비율로 정규화해 임계값(pinDeathPenetrationRatio) 이상이면 즉시 사망시킨다.
    /// </summary>
    private void UpdatePinPenetration()
    {
        if (player == null || player.PinPenetrationProbe == null)
        {
            return;
        }

        CapsuleCollider probe = player.PinPenetrationProbe;
        Transform probeTransform = probe.transform;

        float deepestPenetration = 0f;
        Bounds probeBounds = probe.bounds;

        foreach (var cell in cellColliders)
        {
            // ComputePenetration은 실제로 겹치지 않는 콜라이더 쌍에 대해서는 결과가 정의되지 않아
            // NaN이나 극단적인 값을 반환할 수 있으므로, 대략적인 바운딩 박스가 겹칠 때만 호출한다.
            if (!cell.bounds.Intersects(probeBounds))
            {
                continue;
            }

            bool hasHit = Physics.ComputePenetration(
                probe, probeTransform.position, probeTransform.rotation,
                cell, cell.transform.position, cell.transform.rotation,
                out _, out float distance);

            if (hasHit && distance > deepestPenetration)
            {
                deepestPenetration = distance;
            }
        }

        isPlayerOverlapping = deepestPenetration > 0f;
        currentPinPenetrationRatio = isPlayerOverlapping ? Mathf.Clamp01(deepestPenetration / probe.height) : 0f;

        if (currentPinPenetrationRatio >= pinDeathPenetrationRatio)
        {
            IsGlobalPaused = true;
            SharedGameplayManager.Instance.OnPlayerPinned();
        }
    }

    /// <summary>
    /// 위험도(0~1, TetrisDangerVignette로 전달)를 계산해 이번 프레임의 최댓값에 반영한다.
    /// 0.3~0.5: 아직 접촉 전, 착지까지 dangerWarningDistance 이내로 남았고 플레이어가 그 바로 아래 있는 경우 (거리에 반비례)
    /// 0.5~0.7: 접촉 후, 침투율(0~pinDeathPenetrationRatio)에 비례
    /// 두 구간이 접촉 시점(거리 0 = 침투 0)에서 정확히 0.5로 이어진다.
    /// 시작값을 0.3으로 잡은 것은, 0에서부터 서서히 올리면 초반 체감이 너무 약하기 때문이다.
    /// </summary>
    private void UpdateDangerRatio()
    {
        if (player == null)
        {
            return;
        }

        float ratio;

        if (isPlayerOverlapping && !isLanded)
        {
            // 낙하 중인 블록에 짓눌리는 경우에만 위험도를 올린다.
            // 착지해 정지한 발판에 살짜 겹치는(부딪히는) 것은 정상적인 상황이라 대상이 아니다.
            float penetrationProgress = Mathf.Clamp01(currentPinPenetrationRatio / pinDeathPenetrationRatio);
            ratio = Mathf.Lerp(0.5f, 0.7f, penetrationProgress);
        }
        else if (!isPlayerOverlapping && !isLanded)
        {
            // 사전 경고는 "블록이 아직 내려오고 있어 곧 닿는다"는 의미이므로,
            // 이미 착지해 정지한 블록에는 적용하지 않는다 (그 옆에 서 있어도 더 이상 다가오지 않음).
            float nearestDistance = GetNearestLandingDistanceUnderPlayer();
            if (nearestDistance < 0f)
            {
                return;
            }

            float approachProgress = 1f - Mathf.Clamp01(nearestDistance / dangerWarningDistance);
            ratio = Mathf.Lerp(0.3f, 0.5f, approachProgress);
        }
        else
        {
            return;
        }

        if (ratio > highestDangerRatioThisFrame)
        {
            highestDangerRatioThisFrame = ratio;
        }
    }

    /// <summary>
    /// 플레이어가 셀의 수평(X,Z) 범위 안에 있고 그 아래에 있는 셀들 중,
    /// 셀 바닥에서 플레이어 머리(캡슐 최상단)까지 남은 거리가 가장 짧은 값을 반환한다.
    /// (블록이 "바닥에 닿기까지"가 아니라 "플레이어 몸에 닿기까지" 남은 거리를 재는 것이 핵심)
    /// 해당하는 셀이 없거나 dangerWarningDistance보다 멀면 음수를 반환한다.
    /// </summary>
    private float GetNearestLandingDistanceUnderPlayer()
    {
        if (player.PinPenetrationProbe == null)
        {
            return -1f;
        }

        Vector3 playerPos = player.transform.position;
        float playerHeadY = player.PinPenetrationProbe.bounds.max.y;
        float nearest = -1f;

        foreach (var cell in cellColliders)
        {
            Bounds bounds = cell.bounds;

            bool isUnderneath = playerPos.x >= bounds.min.x && playerPos.x <= bounds.max.x
                && playerPos.z >= bounds.min.z && playerPos.z <= bounds.max.z
                && playerPos.y < bounds.min.y;

            if (!isUnderneath)
            {
                continue;
            }

            float distance = bounds.min.y - playerHeadY;
            if (distance >= 0f && distance <= dangerWarningDistance && (nearest < 0f || distance < nearest))
            {
                nearest = distance;
            }
        }

        return nearest;
    }

    private void CheckLanding()
    {
        foreach (var cell in cellColliders)
        {
            Bounds bounds = cell.bounds;

            Vector3 halfExtents = new Vector3(bounds.extents.x * 0.95f, landingCheckThickness * 0.5f, bounds.extents.z * 0.95f);
            Vector3 checkCenter = new Vector3(bounds.center.x, bounds.min.y - landingCheckThickness * 0.5f, bounds.center.z);

            Collider[] hits = Physics.OverlapBox(checkCenter, halfExtents, transform.rotation, landingMask);
            foreach (var hit in hits)
            {
                if (System.Array.IndexOf(cellColliders, hit) >= 0)
                {
                    continue;
                }

                // 착지 표면(부딪힌 콜라이더 윗면)과 셀 바닥면 사이의 오차만큼
                // 블록 전체를 들어올려, 겹치거나 뜬 상태로 멈추지 않게 보정한다.
                float surfaceTopY = hit.bounds.max.y;
                float cellBottomY = bounds.min.y;
                float correction = surfaceTopY - cellBottomY;

                rb.MovePosition(rb.position + Vector3.up * correction);

                Land();
                return;
            }
        }
    }

    private void Land()
    {
        isLanded = true;

        CameraShake.ShakeAll();

        // 착지하는 순간 플레이어가 이미 겹쳐 있다면, 그 깊이가 사망 임계값에 못 미쳤더라도
        // 바닥과 블록 사이에 완전히 끼인 것으로 간주해 즉시 사망 처리한다.
        // (셀이 솔리드로 전환된 뒤 겹침이 남아있으면 물리 엔진이 플레이어를 강제로 튕겨내기 때문에,
        // 그 전에 게임을 멈춰 튕김이 발생할 기회 자체를 없앤다)
        if (isPlayerOverlapping)
        {
            IsGlobalPaused = true;
            SharedGameplayManager.Instance.OnPlayerPinned();
            return;
        }

        FinishLanding();
    }

    private void FinishLanding()
    {
        // 착지 완료 시점부터 실제 발판(솔리드)이 되어야 플레이어가 밟고 올라설 수 있다.
        SetCellsTrigger(false);

        int landingLayer = LayerMask.NameToLayer("TetrisLanding");
        foreach (var cell in cellColliders)
        {
            cell.gameObject.layer = landingLayer;
        }
    }
}
