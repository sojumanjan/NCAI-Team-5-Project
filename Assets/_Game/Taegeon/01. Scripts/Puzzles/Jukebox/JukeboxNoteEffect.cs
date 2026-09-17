using System.Collections;
using UnityEngine;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "JukeboxNoteEffect")]
public sealed class JukeboxNoteEffect : MonoBehaviour
{
    [SerializeField] private Transform noteVisual;
    [SerializeField] private Renderer noteRenderer;
    private Vector3 origin;
    private Vector3 originalScale;
    private Coroutine animation;
    private MaterialPropertyBlock properties;
    private void Awake()
    {
        if (noteVisual == null) return;
        origin = noteVisual.localPosition;
        originalScale = noteVisual.localScale;
        properties = new MaterialPropertyBlock();
        noteVisual.gameObject.SetActive(false);
    }
    public void Show(Color color, float duration)
    {
        if (noteVisual == null || !isActiveAndEnabled) return;
        if (animation != null) StopCoroutine(animation);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        properties.SetColor("_EmissionColor", color * .8f);
        noteRenderer.SetPropertyBlock(properties);
        noteVisual.gameObject.SetActive(true);
        animation = StartCoroutine(FloatNote(Mathf.Max(duration, .25f)));
    }
    private IEnumerator FloatNote(float duration)
    {
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            noteVisual.localPosition = origin + Vector3.up * (.45f * t);
            float scale = Mathf.Min(1f, t * 8f + .65f) * (1f - .25f * t);
            noteVisual.localScale = originalScale * scale;
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        Hide();
    }
    public void Hide()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        if (noteVisual == null) return;
        noteVisual.gameObject.SetActive(false);
        noteVisual.localPosition = origin;
        noteVisual.localScale = originalScale;
    }
    private void OnDisable() => Hide();
}
}
