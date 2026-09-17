using System.Collections;
using UnityEngine;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "MemoryCube")]
[RequireComponent(typeof(Renderer), typeof(BoxCollider))]
public sealed class MemoryCube : MonoBehaviour
{
    #region 참조 및 설정

    [Tooltip("Optional: add this cube's note later.")]
    [SerializeField] private AudioClip note;

    private Renderer cubeRenderer;
    private Material originalMaterial;
    private Material runtimeMaterial;
    private Color baseColor;
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private int colorProperty;
    private Coroutine flash;
    private AudioSource audioSource;

    #endregion

    #region 버튼 초기화

    /// <summary>
    /// 버튼의 음 재생과 색상 효과를 준비합니다.
    /// </summary>
    public void Initialize(Color color)
    {
        if (runtimeMaterial == null)
        {
            cubeRenderer = GetComponent<Renderer>();
            originalMaterial = cubeRenderer.sharedMaterial;
            runtimeMaterial = new Material(originalMaterial);
            cubeRenderer.sharedMaterial = runtimeMaterial;
            colorProperty = Shader.PropertyToID(runtimeMaterial.HasProperty("_BaseColor")
                ? "_BaseColor" : "_Color");
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
        baseColor = color;
        ResetFeedback();
    }

    #endregion

    #region 음과 색상 효과

    /// <summary>
    /// 버튼의 소리와 색상 및 음표 효과를 재생합니다.
    /// </summary>
    public void PlayFeedback(float duration)
    {
        if (runtimeMaterial == null) return;
        if (flash != null) StopCoroutine(flash);
        // 연속 입력의 음이 겹치지 않도록 이전 소리를 끊습니다.
        audioSource.Stop();
        if (note != null)
        {
            audioSource.clip = note;
            audioSource.Play();
        }
        flash = StartCoroutine(Flash(duration));
        var noteEffect = GetComponent<JukeboxNoteEffect>();
        if (noteEffect != null) noteEffect.Show(baseColor, duration);
    }

    /// <summary>
    /// 일정 시간 버튼을 밝힌 뒤 기본 색상으로 되돌립니다.
    /// </summary>
private IEnumerator Flash(float duration)
    {
        runtimeMaterial.SetColor(colorProperty, baseColor);
        yield return new WaitForSecondsRealtime(duration);
        runtimeMaterial.SetColor(colorProperty, idleColor);
        audioSource.Stop();
        flash = null;
    }

    /// <summary>
    /// 재생 중인 효과를 중지하고 버튼을 초기 상태로 되돌립니다.
    /// </summary>
public void ResetFeedback()
    {
        if (flash != null) StopCoroutine(flash);
        flash = null;
        if (runtimeMaterial != null) runtimeMaterial.SetColor(colorProperty, idleColor);
        if (audioSource != null) audioSource.Stop();
        var noteEffect = GetComponent<JukeboxNoteEffect>();
        if (noteEffect != null) noteEffect.Hide();
    }

    #endregion

    #region 효과 및 리소스 정리

    /// <summary>
    /// 버튼 비활성화 시 재생 중인 효과를 종료합니다.
    /// </summary>
    private void OnDisable() => ResetFeedback();

    /// <summary>
    /// 생성한 머티리얼을 해제하고 원본을 복원합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (runtimeMaterial == null) return;
        if (cubeRenderer != null) cubeRenderer.sharedMaterial = originalMaterial;
        Destroy(runtimeMaterial);
    }
    #endregion

}
}
