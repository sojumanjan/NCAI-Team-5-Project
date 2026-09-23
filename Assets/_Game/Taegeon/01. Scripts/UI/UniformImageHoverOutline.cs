using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;

[RequireComponent(typeof(Button))]
public sealed class UniformImageHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image[] images;
    [SerializeField] private Material outlineTemplate;
    [SerializeField, Range(1f, 8f)] private float outlinePixels = 3f;
    private Material[] materials;
    private Material[] originals;
    private Button button;
    private bool hovered;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (images == null || outlineTemplate == null) return;
        materials = new Material[images.Length];
        originals = new Material[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            originals[i] = images[i].material;
            materials[i] = new Material(outlineTemplate);
            images[i].material = materials[i];
        }
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (materials == null) return;
        bool show = hovered && button != null && button.IsInteractable();
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || materials[i] == null) continue;
            var sprite = images[i].overrideSprite;
            Vector4 uv = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0, 0, 1, 1);
            float width = show && sprite != null ? outlinePixels : 0;
            materials[i].SetVector("_UVRect", uv);
            materials[i].SetFloat("_OutlineWidth", width);
            var rendered = images[i].materialForRendering;
            if (rendered != materials[i])
            {
                rendered.SetVector("_UVRect", uv);
                rendered.SetFloat("_OutlineWidth", width);
            }
        }
    }

    public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
    private void OnDisable() { hovered = false; Refresh(); }
    private void OnApplicationFocus(bool focus) { if (!focus) { hovered = false; Refresh(); } }

    private void OnDestroy()
    {
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
        {
            if (images[i] != null) images[i].material = originals[i];
            if (materials[i] != null) Destroy(materials[i]);
        }
    }
}
