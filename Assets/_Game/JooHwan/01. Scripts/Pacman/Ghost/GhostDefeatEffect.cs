using System.Collections;
using UnityEngine;

/// <summary>
/// 고스트 처치 연출: 몸이 노이즈 섞인 알파로 서서히 사라지는 디졸브 + 고스트 색과 같은 파티클.
/// 전용 디졸브 셰이더 없이, URP/Lit의 알파(투명 렌더 큐)를 흔들며 줄이는 방식으로 대체한다.
/// </summary>
public class GhostDefeatEffect : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float dissolveDuration = 0.9f;
    [SerializeField] private float noiseFrequency = 18f;
    [SerializeField] private float noiseAmplitude = 0.35f;

    private Material materialInstance;
    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        if (bodyRenderer != null)
        {
            materialInstance = bodyRenderer.material;
            originalColor = materialInstance.GetColor("_BaseColor");
        }

        originalScale = transform.localScale;
    }

    /// <summary>
    /// 처치 연출을 재생한 뒤 onComplete를 호출한다 (onComplete에서 실제 SetActive(false) 등을 처리).
    /// </summary>
    public void Play(System.Action onComplete)
    {
        StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(System.Action onComplete)
    {
        SpawnParticles();

        // URP/Lit을 그대로 쓰면서, 알파 값을 노이즈처럼 흔들며 0으로 줄이고
        // 동시에 스케일을 축소시켜 "분해되어 사라지는" 느낌을 낸다.
        SetTransparentMode();

        // 처치 순간엔 대개 발광(_EmissionColor) 중이라, 그 발광이 남아있으면 알파를
        // 아무리 줄여도 밝게 빛나 보여 디졸브가 거의 안 보인다. 시작 시점의 발광색을
        // 읽어와 알파와 같은 비율로 함께 줄여야 실제로 투명해지는 게 눈에 보인다.
        Color startEmissionColor = materialInstance != null ? materialInstance.GetColor("_EmissionColor") : Color.black;

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dissolveDuration;

            // PerlinNoise는 0~1 범위라 그대로 곱하면 항상 양수만 더해져 알파를 위로만 밀어올린다
            // (초반엔 Clamp01에 의해 그냥 1로 뭉개져 티가 안 남). -1~1로 중심을 맞춰야
            // 페이드 곡선을 실제로 위아래로 흔들어 지글거리는 느낌이 난다.
            float noise = (Mathf.PerlinNoise(Time.time * noiseFrequency, 0f) - 0.5f) * 2f * noiseAmplitude;
            float alpha = Mathf.Clamp01((1f - t) + noise);

            if (materialInstance != null)
            {
                Color c = originalColor;
                c.a = alpha;
                materialInstance.SetColor("_BaseColor", c);
                materialInstance.SetColor("_EmissionColor", startEmissionColor * alpha);
            }

            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.6f, t);

            yield return null;
        }

        onComplete?.Invoke();
    }

    /// <summary>재시작으로 되살아날 때 원래 외형(불투명, 원래 크기)으로 되돌린다.</summary>
    public void ResetVisual()
    {
        if (materialInstance != null)
        {
            materialInstance.SetColor("_BaseColor", originalColor);

            // 디졸브 중 낮춰뒀던 발광을 꺼둔다. Ghost가 다시 활성화되면 UpdatePulseVisual이
            // 매 프레임 갱신하지만, 그 전까지(비활성 상태로 대기하는 동안) 어두운 발광이 남지 않게 한다.
            materialInstance.SetColor("_EmissionColor", Color.black);
        }

        transform.localScale = originalScale;
        SetOpaqueMode();
    }

    private void SpawnParticles()
    {
        if (particlePrefab == null)
        {
            return;
        }

        // 고스트 루트(발밑)가 아니라 실제 몸통 위치(bodyRenderer 중심)에서 터뜨려야
        // 바닥/벽에 가려지지 않고 제대로 보인다.
        Vector3 spawnPosition = bodyRenderer != null ? bodyRenderer.bounds.center : transform.position;

        GameObject instance = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
        var ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = originalColor;
        }

        // main.startColor는 셰이더가 파티클 vertex color를 곱해야만 반영되므로,
        // 머테리얼 _BaseColor도 직접 인스턴스화해서 고스트 색으로 맞춘다.
        var psRenderer = instance.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            var matInstance = new Material(psRenderer.sharedMaterial);
            matInstance.SetColor("_BaseColor", originalColor);
            psRenderer.material = matInstance;
        }

        Destroy(instance, 3f);
    }

    private void SetTransparentMode()
    {
        if (materialInstance == null)
        {
            return;
        }

        materialInstance.SetFloat("_Surface", 1f);
        materialInstance.SetFloat("_Blend", 0f);
        materialInstance.SetOverrideTag("RenderType", "Transparent");
        materialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        materialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        materialInstance.SetInt("_ZWrite", 0);
        materialInstance.DisableKeyword("_ALPHATEST_ON");
        materialInstance.EnableKeyword("_ALPHABLEND_ON");
        materialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        materialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetOpaqueMode()
    {
        if (materialInstance == null)
        {
            return;
        }

        materialInstance.SetFloat("_Surface", 0f);
        materialInstance.SetOverrideTag("RenderType", "Opaque");
        materialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        materialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        materialInstance.SetInt("_ZWrite", 1);
        materialInstance.DisableKeyword("_ALPHATEST_ON");
        materialInstance.DisableKeyword("_ALPHABLEND_ON");
        materialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        materialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }
}
