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
    private bool _groupResolved;

    /// <summary>
    /// CanvasGroup을 Awake가 아니라 쓸 때 찾는다.
    ///
    /// 예전에는 Awake에서 찾으면서 Hide()까지 했는데, root가 비어 있으면 그 Hide가 자기
    /// GameObject를 끈다. 부모의 첫 Redraw가 먼저 돌아 이 줄을 꺼버리면 Awake는 실행되지
    /// 못한 채 보류되고, 나중에 Show()의 SetActive(true) 순간에야 실행돼 방금 켠 줄을 도로
    /// 끈다. 그래서 프롬프트가 처음 몇 번은 안 떴다. 초기 숨김은 InteractionPromptUI가 한다.
    /// </summary>
    private CanvasGroup Group
    {
        get
        {
            if (!_groupResolved)
            {
                _group = GetComponent<CanvasGroup>();
                _groupResolved = true;
            }

            return _group;
        }
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

        CanvasGroup group = Group;
        if (group != null)
        {
            group.alpha = enabled ? 1f : disabledAlpha;
        }
    }

    /// <summary>Switches the line off.</summary>
    public void Hide()
    {
        Target.SetActive(false);
    }

    private GameObject Target => root != null ? root : gameObject;
}
