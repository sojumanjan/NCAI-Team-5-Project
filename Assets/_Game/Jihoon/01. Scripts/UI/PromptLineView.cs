using TMPro;
using UnityEngine;

/// <summary>
/// One line of the prompt panel, e.g. "[E] 작동시키기".
///
/// Put this on a line object in the Canvas and wire its pieces. The panel keeps a fixed
/// set of these and switches them on and off, so everything about how a line looks —
/// font, size, spacing, colour — is authored in the scene, not in code.
///
/// There is no progress bar here on purpose: a hold already reads clearly from the
/// station itself (colour, shaking, the dish popping out), and a second gauge on the
/// crosshair was just noise. The key label still says "[E 홀드]" so the player knows to
/// keep the button down.
/// </summary>
public class PromptLineView : MonoBehaviour
{
    [Header("구성")]
    [Tooltip("이 줄 전체. 비워두면 이 오브젝트를 켜고 끕니다.")]
    [SerializeField] private GameObject root;

    [Tooltip("키 표시. \"[E]\", \"[좌클릭]\".")]
    [SerializeField] private TMP_Text keyText;

    [Tooltip("행동 설명. \"원두 넣기\".")]
    [SerializeField] private TMP_Text labelText;

    [Header("흐리게")]
    [Tooltip("사용 불가일 때 투명도. CanvasGroup이 있어야 적용됩니다.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float disabledAlpha = 0.4f;

    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        Hide();
    }

    /// <summary>Shows this line with the given content.</summary>
    public void Show(string key, string label, bool enabled)
    {
        Target.SetActive(true);

        if (keyText != null)
        {
            keyText.text = key;
        }

        if (labelText != null)
        {
            labelText.text = label;
        }

        if (_group != null)
        {
            _group.alpha = enabled ? 1f : disabledAlpha;
        }
    }

    /// <summary>Switches the line off.</summary>
    public void Hide()
    {
        Target.SetActive(false);
    }

    private GameObject Target => root != null ? root : gameObject;
}
