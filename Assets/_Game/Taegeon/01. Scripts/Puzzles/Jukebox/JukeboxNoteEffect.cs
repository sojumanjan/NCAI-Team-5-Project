using System.Collections;
using UnityEngine;

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "JukeboxNoteEffect")]
public sealed class JukeboxNoteEffect : MonoBehaviour
{
    #region 참조 및 설정

    [SerializeField] private Transform noteVisual;
    [SerializeField] private Renderer noteRenderer;
    private Vector3 origin;
    private Vector3 originalScale;
    private Coroutine animation;
    private MaterialPropertyBlock properties;
    #endregion

    #region 음표 준비

    /// <summary>
    /// 음표의 초기 위치와 표시 상태를 준비합니다.
    /// </summary>
    private void Awake()
    {
        if (noteVisual == null) return;
        origin = noteVisual.localPosition;
        originalScale = noteVisual.localScale;
        properties = new MaterialPropertyBlock();
        noteVisual.gameObject.SetActive(false);
    }
    #endregion

    #region 음표 표시 및 연출

    /// <summary>
    /// 지정한 색상으로 떠오르는 음표 효과를 시작합니다.
    /// </summary>
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
    /// <summary>
    /// 음표를 위로 이동시킨 뒤 숨깁니다.
    /// </summary>
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
    #endregion

    #region 음표 종료 및 복원

    /// <summary>
    /// 음표 효과를 종료하고 위치와 크기를 복원합니다.
    /// </summary>
    public void Hide()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        if (noteVisual == null) return;
        noteVisual.gameObject.SetActive(false);
        noteVisual.localPosition = origin;
        noteVisual.localScale = originalScale;
    }
    /// <summary>
    /// 오브젝트 비활성화 시 음표 효과를 종료합니다.
    /// </summary>
    private void OnDisable() => Hide();
    #endregion

}
}
