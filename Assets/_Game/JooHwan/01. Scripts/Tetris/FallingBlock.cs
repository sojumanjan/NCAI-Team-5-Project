using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingBlock : MonoBehaviour
{
    // 사망 팝업 등으로 게임 전체가 일시정지될 때, 모든 FallingBlock 인스턴스가 함께 멈춘다.
    public static bool IsGlobalPaused;

    [SerializeField] private LayerMask landingMask;
    [SerializeField] private float landingCheckThickness = 0.1f;

    [Header("Pinning (끼임 판정)")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pinDeathDelay = 0.4f;

    private float fallSpeed = 3f;
    private Rigidbody rb;
    private bool isLanded;

    private Collider[] cellColliders;
    private BoxCollider pinTrigger;

    private float playerOverlapTimer;
    private bool playerIsOverlapping;

    public bool IsLanded => isLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        cellColliders = GetComponentsInChildren<Collider>();

        SetupPinTrigger();
    }

    private void FixedUpdate()
    {
        if (IsGlobalPaused)
        {
            return;
        }

        UpdatePinTimer();

        if (isLanded)
        {
            return;
        }

        rb.MovePosition(rb.position + Vector3.down * fallSpeed * Time.fixedDeltaTime);

        CheckLanding();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerIsOverlapping = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerIsOverlapping = false;
        playerOverlapTimer = 0f;

        // 착지는 끝났는데 겹침 때문에 미뤄뒀던 레이어 전환을,
        // Player가 빠져나간 시점에 뒤늦게 적용한다.
        if (isLanded)
        {
            FinishLanding();
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

    private void SetupPinTrigger()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds();

        foreach (var col in cellColliders)
        {
            Vector3 localCenter = transform.InverseTransformPoint(col.bounds.center);
            Vector3 localSize = Vector3.Scale(col.bounds.size, InverseScale(transform.lossyScale));

            if (!hasBounds)
            {
                bounds = new Bounds(localCenter, localSize);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(new Bounds(localCenter, localSize));
            }
        }

        pinTrigger = gameObject.AddComponent<BoxCollider>();
        pinTrigger.isTrigger = true;
        pinTrigger.center = bounds.center;
        pinTrigger.size = bounds.size;

        int pinLayer = LayerMask.NameToLayer("TetrisPinTrigger");
        if (pinLayer >= 0)
        {
            // pinTrigger 전용 레이어를 별도 자식 오브젝트에 부여해,
            // 등반 Raycast·착지 OverlapBox가 이 콜라이더를 대상으로 삼지 않게 한다.
            GameObject pinTriggerHost = new GameObject("PinTrigger");
            pinTriggerHost.layer = pinLayer;
            pinTriggerHost.transform.SetParent(transform, false);

            DestroyImmediate(pinTrigger);
            pinTrigger = pinTriggerHost.AddComponent<BoxCollider>();
            pinTrigger.isTrigger = true;
            pinTrigger.center = bounds.center;
            pinTrigger.size = bounds.size;
        }
    }

    private static Vector3 InverseScale(Vector3 scale)
    {
        return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }

    private void UpdatePinTimer()
    {
        if (!playerIsOverlapping)
        {
            return;
        }

        playerOverlapTimer += Time.fixedDeltaTime;

        if (playerOverlapTimer >= pinDeathDelay)
        {
            IsGlobalPaused = true;
            SharedGameplayManager.Instance.OnPlayerPinned();
            playerOverlapTimer = 0f;
            playerIsOverlapping = false;
        }
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

        if (playerIsOverlapping)
        {
            // 착지 순간 Player와 몸이 겹쳐있다면 그대로 레이어를 바꾸지 않고
            // 끼임 사망 판정에 맡긴다 (급격한 물리 밀어내기로 인한 이탈 방지).
            return;
        }

        FinishLanding();
    }

    private void FinishLanding()
    {
        int landingLayer = LayerMask.NameToLayer("TetrisLanding");
        foreach (var cell in cellColliders)
        {
            cell.gameObject.layer = landingLayer;
        }

        // 착지가 끝나 안전한 발판이 되었으므로, 더 이상 끼임 판정 대상이 아니다.
        if (pinTrigger != null)
        {
            pinTrigger.enabled = false;
        }
    }
}
