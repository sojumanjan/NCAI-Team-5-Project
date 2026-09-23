using System.Collections;
using UnityEngine;

/// <summary>
/// 고스트 처치 연출: 몸이 노이즈 섞인 알파로 서서히 사라지는 디졸브 + 고스트 색과 같은 파티클.
/// 전용 디졸브 셰이더 없이, URP/Lit의 알파(투명 렌더 큐)를 흔들며 줄이는 방식으로 대체한다.
/// </summary>
public class GhostDefeatEffect : MonoBehaviour
{
    [Tooltip("파티클 발생 위치와 파티클 색상 기준으로만 쓰인다(단일 대표 렌더러). " +
        "실제 디졸브(투명화)는 아래 dissolveRenderers 전부에 적용된다.")]
    [SerializeField] private Renderer bodyRenderer;
    [Tooltip("처치 시 디졸브(알파 감소) 효과를 적용할 렌더러 전부. 비워두면 자식의 모든 Renderer를 자동으로 사용한다 " +
        "(예: ladybug처럼 몸통/다리/머리가 서로 다른 서브메시로 나뉜 모델).")]
    [SerializeField] private Renderer[] dissolveRenderers;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float dissolveDuration = 0.9f;
    [SerializeField] private float noiseFrequency = 18f;
    [SerializeField] private float noiseAmplitude = 0.35f;

    private Material[] materialInstances;
    private Color[] originalColors;
    private Material materialInstance; // 파티클 색상 산출 등 대표 색이 필요한 곳에서 사용
    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        if (dissolveRenderers == null || dissolveRenderers.Length == 0)
        {
            // MeshRenderer만 대상으로 한다. GetComponentsInChildren<Renderer>는
            // GlowAura의 ParticleSystemRenderer까지 잡아버려, 디졸브 중 발광 오라
            // 파티클의 렌더 모드/색상까지 같이 흔들려버리는 부작용이 있었다.
            dissolveRenderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        materialInstances = new Material[dissolveRenderers.Length];
        originalColors = new Color[dissolveRenderers.Length];

        for (int i = 0; i < dissolveRenderers.Length; i++)
        {
            if (dissolveRenderers[i] == null)
            {
                continue;
            }

            materialInstances[i] = dissolveRenderers[i].material;
            originalColors[i] = materialInstances[i].GetColor("_BaseColor");
        }

        if (bodyRenderer != null)
        {
            materialInstance = bodyRenderer.material;
            originalColor = materialInstance.GetColor("_BaseColor");
        }
        else if (materialInstances.Length > 0 && materialInstances[0] != null)
        {
            // 대표 렌더러가 따로 지정되지 않았다면 첫 번째 디졸브 렌더러 색을 파티클 색 기준으로 쓴다.
            materialInstance = materialInstances[0];
            originalColor = originalColors[0];
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
        // (서브메시마다 발광 색이 다를 수 있으므로 렌더러별로 각자 시작 발광색을 읽어둔다)
        var startEmissionColors = new Color[materialInstances.Length];
        for (int i = 0; i < materialInstances.Length; i++)
        {
            startEmissionColors[i] = materialInstances[i] != null
                ? materialInstances[i].GetColor("_EmissionColor")
                : Color.black;
        }

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

            for (int i = 0; i < materialInstances.Length; i++)
            {
                if (materialInstances[i] == null)
                {
                    continue;
                }

                Color c = originalColors[i];
                c.a = alpha;
                materialInstances[i].SetColor("_BaseColor", c);
                materialInstances[i].SetColor("_EmissionColor", startEmissionColors[i] * alpha);
            }

            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.6f, t);

            yield return null;
        }

        onComplete?.Invoke();
    }

    /// <summary>재시작으로 되살아날 때 원래 외형(불투명, 원래 크기)으로 되돌린다.</summary>
    public void ResetVisual()
    {
        for (int i = 0; i < materialInstances.Length; i++)
        {
            if (materialInstances[i] == null)
            {
                continue;
            }

            materialInstances[i].SetColor("_BaseColor", originalColors[i]);

            // 디졸브 중 낮춰뒀던 발광을 꺼둔다. Ghost가 다시 활성화되면 UpdatePulseVisual이
            // 매 프레임 갱신하지만, 그 전까지(비활성 상태로 대기하는 동안) 어두운 발광이 남지 않게 한다.
            materialInstances[i].SetColor("_EmissionColor", Color.black);
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
        for (int i = 0; i < materialInstances.Length; i++)
        {
            Material m = materialInstances[i];
            if (m == null)
            {
                continue;
            }

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private void SetOpaqueMode()
    {
        for (int i = 0; i < materialInstances.Length; i++)
        {
            Material m = materialInstances[i];
            if (m == null)
            {
                continue;
            }

            m.SetFloat("_Surface", 0f);
            m.SetOverrideTag("RenderType", "Opaque");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }
}
