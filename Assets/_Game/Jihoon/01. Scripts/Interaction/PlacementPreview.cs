using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;

/// <summary>
/// Shows a translucent copy of the held item where it would land, and lets the player spin
/// it before letting go. Put this on the Player next to <see cref="PlayerHands"/>.
///
/// The ghost is a stripped clone of the held object itself — no colliders, no physics, no
/// scripts — so it always matches what will actually appear, including any art changes.
/// 프리팹이 아니라 손에 든 실물에서 복제하는 이유: 재료통처럼 프리팹이 없는 것도 옮길 수
/// 있어야 하고, 실물에서 뜨면 고스트가 실제로 놓일 것과 어긋날 수가 없다.
/// It reads the landing spot from <see cref="PlayerHands.GetDropPosition"/>, the same
/// method the real drop uses, so the preview cannot disagree with the result.
/// </summary>
public class PlacementPreview : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("손. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private PlayerHands hands;

    [Tooltip("조준 대상. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private PlayerInteractor interactor;

    [Header("회전")]
    [Tooltip("반시계 회전 키. None이면 그 방향으로는 돌지 않습니다.")]
    [SerializeField] private Key rotateLeftKey = Key.None;

    [Tooltip("시계 회전 키. E는 상호작용 키라 겹치므로 피하세요.")]
    [SerializeField] private Key rotateRightKey = Key.R;

    [Tooltip("톡 눌렀을 때 한 번에 도는 각도. 0이면 누른 즉시 연속 회전만 합니다.")]
    [SerializeField] private float rotationStep = 15f;

    [Tooltip("계속 누르고 있을 때의 회전 속도 (deg/sec).")]
    [SerializeField] private float rotationSpeed = 120f;

    [Tooltip("누르고 이만큼 지나면 연속 회전으로 넘어갑니다 (초).")]
    [SerializeField] private float holdRepeatDelay = 0.25f;

    [Header("모양")]
    [Tooltip("고스트에 쓸 머티리얼. 비워두면 반투명 머티리얼을 코드로 만듭니다.")]
    [SerializeField] private Material ghostMaterial;

    [Tooltip("코드로 만들 때 쓸 색과 투명도.")]
    [SerializeField] private Color ghostColor = new Color(0.4f, 0.9f, 1f, 0.35f);

    [Tooltip("내려놓을 수 없는 상황에서는 고스트를 숨깁니다.")]
    [SerializeField] private bool hideWhenNotDroppable = true;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    /// <summary>
    /// 지금 회전 키가 먹히는지. 프롬프트가 이걸 보고 안내 줄을 띄운다.
    ///
    /// 실제로 입력을 읽는 조건을 그대로 내보낸다. 조건을 두 군데에 따로 적으면 "뜨는데
    /// 안 돌아가는" 프롬프트가 생기는데, 그건 없는 것보다 나쁘다.
    /// </summary>
    public bool CanRotate { get; private set; }

    private GameObject _ghost;
    private WorldItem _ghostFor;
    private Material _runtimeMaterial;
    private float _holdTime;

    private void Awake()
    {
        if (hands == null)
        {
            hands = GetComponent<PlayerHands>();
        }

        if (interactor == null)
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        if (hands == null || interactor == null)
        {
            Debug.LogError($"{nameof(PlacementPreview)}: needs {nameof(PlayerHands)} and " +
                           $"{nameof(PlayerInteractor)} on the same object.", this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        CanRotate = false;
        DestroyGhost();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }

    private void Update()
    {
        WorldItem held = hands.HeldObject;

        // The ghost is built once per item and then shown or hidden. Destroying and
        // respawning it instead would leak a clone into the scene every single frame.
        EnsureGhost(held);

        if (_ghost == null)
        {
            CanRotate = false;
            return;
        }

        // Declared up front: the short-circuit below leaves the out parameters unassigned
        // when the held check fails, which the compiler will not accept inline.
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        bool freeRotation = false;

        bool show = held != null && TryGetPreviewPose(out position, out rotation, out freeRotation);

        if (_ghost.activeSelf != show)
        {
            _ghost.SetActive(show);
        }

        if (!show)
        {
            CanRotate = false;
            _holdTime = 0f;
            return;
        }

        CanRotate = freeRotation;

        // Snapped placements decide their own angle, so the rotate keys are only live
        // while the item would land freely on the world.
        if (freeRotation)
        {
            ReadRotationInput();
            rotation = hands.GetDropRotation();
        }
        else
        {
            _holdTime = 0f;
        }

        _ghost.transform.SetPositionAndRotation(position, rotation);
    }

    // ---------------------------------------------------------------- where it lands

    /// <summary>
    /// Asks the resolver what left click would do and turns that into a pose for the
    /// ghost. Receivers that expose a snap point get previewed at that exact spot.
    /// </summary>
    private bool TryGetPreviewPose(out Vector3 position, out Quaternion rotation, out bool freeRotation)
    {
        position = default;
        rotation = default;
        freeRotation = false;

        LeftClickAction action = InteractionResolver.Resolve(interactor, hands);

        if (action.Kind == LeftClickKind.Put)
        {
            return action.Receiver is IPlacementTarget target
                   && target.TryGetPlacement(hands.HeldItem, out position, out rotation);
        }

        if (action.Kind != LeftClickKind.Drop && hideWhenNotDroppable)
        {
            return false;
        }

        // 얹을 수 있는 면이 아니면 물건은 그 자리에 머무르지 않고 떨어진다. 허공이 그렇고,
        // 벽도 마찬가지다. 그 자리를 고스트로 그리면 붙어 있을 것처럼 보여 거짓말이 된다.
        if (!hands.HasPlaceableSurface)
        {
            return false;
        }

        position = hands.GetDropPosition();
        rotation = hands.GetDropRotation();
        freeRotation = true;
        return true;
    }

    // ---------------------------------------------------------------- rotation

    private void ReadRotationInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float direction = Direction(Resolve(keyboard, rotateLeftKey), -1f)
                          + Direction(Resolve(keyboard, rotateRightKey), 1f);

        if (Mathf.Approximately(direction, 0f))
        {
            _holdTime = 0f;
            return;
        }

        _holdTime += Time.deltaTime;

        // A tap turns by one step; keeping the key down rolls into a smooth spin.
        if (_holdTime >= holdRepeatDelay)
        {
            hands.AddDropYaw(direction * rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// None으로 비워둔 방향은 조회 자체를 건너뛴다. Keyboard의 인덱서에 Key.None을 넣으면
    /// 범위를 벗어난 접근이 되어 터진다.
    /// </summary>
    private static KeyControl Resolve(Keyboard keyboard, Key key)
    {
        return key == Key.None ? null : keyboard[key];
    }

    /// <summary>Returns the sign while the key is down, and applies the tap step on press.</summary>
    private float Direction(KeyControl key, float sign)
    {
        if (key == null || !key.isPressed)
        {
            return 0f;
        }

        if (key.wasPressedThisFrame && rotationStep > 0f)
        {
            hands.AddDropYaw(sign * rotationStep);
        }

        return sign;
    }

    // ---------------------------------------------------------------- ghost

    private void EnsureGhost(WorldItem held)
    {
        if (_ghostFor == held && _ghost != null)
        {
            return;
        }

        DestroyGhost();

        if (held == null)
        {
            return;
        }

        // 손에 든 실물을 그대로 복제한다. 복제본은 손의 자식으로 태어나므로 바로 떼어내지
        // 않으면 시점을 따라 흔들린다.
        _ghost = Instantiate(held.gameObject);
        _ghost.transform.SetParent(null, true);
        _ghost.transform.localScale = held.transform.lossyScale;
        _ghost.name = $"[Ghost] {held.name}";
        _ghostFor = held;

        StripForDisplay(_ghost);
        ApplyGhostMaterial(_ghost);
    }

    /// <summary>Makes the clone inert so only its renderers do anything.</summary>
    private static void StripForDisplay(GameObject root)
    {
        // Disabled, not destroyed. Destroy is deferred to the end of the frame, so a
        // [RequireComponent] script is still attached when the collider removal happens
        // and Unity refuses it — which spams "Can't remove BoxCollider because WorldItem
        // depends on it" once per ghost.
        foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            script.enabled = false;
        }

        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    private void ApplyGhostMaterial(GameObject root)
    {
        Material material = ghostMaterial != null ? ghostMaterial : GetRuntimeMaterial();
        if (material == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    /// <summary>Builds a plain transparent URP material so the component works unconfigured.</summary>
    private Material GetRuntimeMaterial()
    {
        if (_runtimeMaterial != null)
        {
            return _runtimeMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogWarning($"{nameof(PlacementPreview)}: URP Unlit shader not found. " +
                             "Assign a Ghost Material instead.", this);
            return null;
        }

        _runtimeMaterial = new Material(shader) { name = "Placement Ghost (Runtime)" };
        _runtimeMaterial.SetFloat(SurfaceId, 1f); // transparent
        _runtimeMaterial.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
        _runtimeMaterial.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        _runtimeMaterial.SetFloat(ZWriteId, 0f);
        _runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _runtimeMaterial.renderQueue = (int)RenderQueue.Transparent;
        _runtimeMaterial.SetColor(BaseColorId, ghostColor);
        _runtimeMaterial.SetColor(ColorId, ghostColor);

        return _runtimeMaterial;
    }

    private void DestroyGhost()
    {
        if (_ghost != null)
        {
            Destroy(_ghost);
        }

        _ghost = null;
        _ghostFor = null;
    }
}
