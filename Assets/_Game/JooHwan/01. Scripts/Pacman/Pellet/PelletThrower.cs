using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 파워펠릿 소지/투척을 담당한다.
/// 최대 2개(손에 든 1개 + 여분 1개) 소지, 좌클릭(Throw)으로 조준 방향에 발사.
/// 빗나간 펠릿은 회수 불가(소멸)하며, 명중 판정은 ThrownPellet이 처리한다.
/// 소지 개수만큼 handSlots(양손 위치)에 시각적으로 펠릿을 들고 있는 모습을 보여준다.
/// </summary>
public class PelletThrower : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private GameObject thrownPelletPrefab;
    [SerializeField] private GameObject heldPelletVisualPrefab;
    [SerializeField] private Transform[] handSlots;
    [SerializeField] private int maxCarry = 2;
    [SerializeField] private float throwForce = 20f;
    [Tooltip("카메라(throwOrigin) 위치 그대로 스폰하면 Player 자신의 CharacterController와 겹쳐 즉시 충돌 판정이 나므로, 앞으로 이만큼 띄워서 스폰한다.")]
    [SerializeField] private float spawnForwardOffset = 0.8f;
    [Tooltip("던지는 순간 재생되는 사운드.")]
    [SerializeField] private SoundData throwSound;

    private InputAction throwAction;
    private PlayerController playerController;
    private Collider[] ownColliders;
    private int carriedCount;
    private readonly System.Collections.Generic.List<GameObject> heldVisuals = new System.Collections.Generic.List<GameObject>();

    public int CarriedCount => carriedCount;

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        throwAction = playerMap.FindAction("Throw");
        playerController = GetComponent<PlayerController>();

        // CharacterController와 테트리스 끼임판정용 트리거 콜라이더까지, 플레이어에 달린
        // 콜라이더를 전부 모아둔다. 던진 펠릿이 스폰 직후 자기 자신과 부딪혀 바로 사라지는 것을 막는 데 쓴다.
        ownColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        throwAction.Enable();
    }

    private void OnDisable()
    {
        throwAction.Disable();
    }

    private void Update()
    {
        if (carriedCount <= 0)
        {
            return;
        }

        // 일시정지/사망 팝업 등으로 조작이 잠긴 동안에는, 패널의 버튼을 클릭하는 좌클릭이
        // 그대로 Throw 액션으로도 들어와 펠릿이 튀어나가므로 잠금 중엔 무시한다.
        if (playerController != null && playerController.AreControlsLocked)
        {
            return;
        }

        if (throwAction.WasPressedThisFrame())
        {
            Throw();
        }
    }

    /// <summary>PowerPellet이 습득 시도할 때 호출한다. 여유가 있으면 true.</summary>
    public bool TryPickup()
    {
        if (carriedCount >= maxCarry)
        {
            return false;
        }

        carriedCount++;
        UpdateHeldVisuals();
        return true;
    }

    /// <summary>팩맨 재시작 시 손에 들고 있던 파워펠릿을 모두 비운다.</summary>
    public void ResetCarried()
    {
        carriedCount = 0;
        UpdateHeldVisuals();
    }

    private void Throw()
    {
        carriedCount--;
        UpdateHeldVisuals();
        AudioManager.PlayAt(throwSound, throwOrigin.position);

        Vector3 spawnPosition = throwOrigin.position + throwOrigin.forward * spawnForwardOffset;
        GameObject pelletInstance = Instantiate(thrownPelletPrefab, spawnPosition, throwOrigin.rotation);
        var thrown = pelletInstance.GetComponent<ThrownPellet>();

        // 스폰 위치 오프셋만으로는, 던지면서 같은 방향으로 계속 이동하면 플레이어가 곧바로
        // 따라붙어 부딪히는 것까지는 못 막는다. 물리적으로 아예 서로 충돌하지 않게 막아둔다.
        foreach (Collider pelletCollider in pelletInstance.GetComponentsInChildren<Collider>())
        {
            foreach (Collider playerCollider in ownColliders)
            {
                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(pelletCollider, playerCollider, true);
                }
            }
        }

        thrown.Launch(throwOrigin.forward * throwForce);
    }

    /// <summary>소지 개수만큼 손 위치에 펠릿 시각 오브젝트를 생성/제거한다.</summary>
    private void UpdateHeldVisuals()
    {
        if (heldPelletVisualPrefab == null || handSlots == null || handSlots.Length == 0)
        {
            return;
        }

        while (heldVisuals.Count < carriedCount)
        {
            Transform slot = handSlots[heldVisuals.Count % handSlots.Length];
            GameObject visual = Instantiate(heldPelletVisualPrefab, slot);
            visual.transform.localPosition = Vector3.zero;
            heldVisuals.Add(visual);
        }

        while (heldVisuals.Count > carriedCount)
        {
            int lastIndex = heldVisuals.Count - 1;
            Destroy(heldVisuals[lastIndex]);
            heldVisuals.RemoveAt(lastIndex);
        }
    }
}
