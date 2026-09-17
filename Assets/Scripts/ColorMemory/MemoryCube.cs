using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(BoxCollider))]
public sealed class MemoryCube : MonoBehaviour
{
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

    public void PlayFeedback(float duration)
    {
        if (runtimeMaterial == null) return;
        if (flash != null) StopCoroutine(flash);
        // Stop the previous note so rapidly repeated presses remain distinct.
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

private IEnumerator Flash(float duration)
    {
        runtimeMaterial.SetColor(colorProperty, baseColor);
        yield return new WaitForSecondsRealtime(duration);
        runtimeMaterial.SetColor(colorProperty, idleColor);
        audioSource.Stop();
        flash = null;
    }

public void ResetFeedback()
    {
        if (flash != null) StopCoroutine(flash);
        flash = null;
        if (runtimeMaterial != null) runtimeMaterial.SetColor(colorProperty, idleColor);
        if (audioSource != null) audioSource.Stop();
        var noteEffect = GetComponent<JukeboxNoteEffect>();
        if (noteEffect != null) noteEffect.Hide();
    }

    private void OnDisable() => ResetFeedback();

    private void OnDestroy()
    {
        if (runtimeMaterial == null) return;
        if (cubeRenderer != null) cubeRenderer.sharedMaterial = originalMaterial;
        Destroy(runtimeMaterial);
    }
}
