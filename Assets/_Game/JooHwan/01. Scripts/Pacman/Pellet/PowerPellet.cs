using UnityEngine;

/// <summary>
/// 맵에 배치되는 파워펠릿. 플레이어가 닿으면 습득되어 사라지고,
/// PelletSpawner에게 습득 사실을 알린다 (전부 습득되면 리젠 판단용).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerPellet : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [Tooltip("바닥에 놓인 상태에서의 Y축 회전 속도(도/초). 습득/투척 후에는 이 오브젝트가 비활성화되므로 자동으로 멈춘다.")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("플레이어가 습득에 성공한 순간 재생되는 사운드.")]
    [SerializeField] private SoundData pickupSound;

    [Header("발광")]
    [Tooltip("발광을 적용할 렌더러. 고스트와 달리 주기 없이 항상 약하게 빛난다.")]
    [SerializeField] private Renderer bodyRenderer;
    [Tooltip("발광 세기. 각 머티리얼의 원래 색(_BaseColor/color)에 이 값을 곱해 은은하게만 밝힌다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowIntensity = 0.3f;

    private void Awake()
    {
        ApplyConstantGlow();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 고스트의 발광 주기(GlowCycleRoutine)와 달리, 파워펠릿은 켜져있는 동안 항상 약하게 빛난다.
    /// 머티리얼별 원래 색을 그대로 광원 색으로 써서, 렌더러가 여러 머티리얼을 가져도 하드코딩 없이 맞는다.
    /// </summary>
    private void ApplyConstantGlow()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        foreach (Material material in bodyRenderer.materials)
        {
            if (!material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            Color baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            material.SetColor("_EmissionColor", baseColor * glowIntensity);
            material.EnableKeyword("_EMISSION");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        var thrower = other.GetComponent<PelletThrower>();
        if (thrower == null || !thrower.TryPickup())
        {
            return;
        }

        AudioManager.PlayAt(pickupSound, transform.position);
        PelletSpawner.Instance.HandlePelletPickedUp(this);
        gameObject.SetActive(false);
    }
}
