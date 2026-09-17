using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영업이 끝난 뒤의 결과 화면. 세션이 넘겨주는 MiniGameResult만 읽고, 판정은 하지 않는다.
///
/// 이 컴포넌트는 자기가 켜야 할 panel 위에 붙이면 안 된다. 꺼진 오브젝트는 Awake조차 돌지
/// 않아 구독을 못 하고, 그러면 영영 켜지지 않는다. 항상 살아 있는 Canvas에 둔다.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("결과를 알려줄 세션.")]
    [SerializeField] private MiniGameSession session;

    [Tooltip("결과 화면 전체. 평소에는 꺼져 있다가 종료 시 켜집니다.")]
    [SerializeField] private GameObject panel;

    [Header("표시")]
    [Tooltip("\"영업 종료\" 같은 큰 제목.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("성공 시 제목.")]
    [SerializeField] private string clearedTitle = "장사 성공!";

    [Tooltip("실패 시 제목.")]
    [SerializeField] private string failedTitle = "장사 실패...";

    [Tooltip("성공 시 제목 색.")]
    [SerializeField] private Color clearedColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("실패 시 제목 색.")]
    [SerializeField] private Color failedColor = new Color(0.95f, 0.35f, 0.35f);

    [Header("수치")]
    [Tooltip("최종 평점. {0}=평점, {1}=목표.")]
    [SerializeField] private TMP_Text ratingText;

    [SerializeField] private string ratingFormat = "평점  {0:0.0} / {1:0.0}";

    [Tooltip("만든 개수. {0}=정답, {1}=전체.")]
    [SerializeField] private TMP_Text countText;

    [SerializeField] private string countFormat = "성공한 주문  {0} / {1}";

    [Header("버튼")]
    [Tooltip("다시 하기. 씬을 새로 엽니다.")]
    [SerializeField] private Button retryButton;

    [Tooltip("허브로 돌아가기. 9단계에서 연결되며 지금은 로그만 남깁니다.")]
    [SerializeField] private Button hubButton;

    private void Awake()
    {
        if (session == null)
        {
            Debug.LogError($"{nameof(ResultUI)}: 세션 참조가 없습니다.", this);
            enabled = false;
            return;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(session.Retry);
        }

        if (hubButton != null)
        {
            hubButton.onClick.AddListener(session.ReturnToHub);
        }

        session.SessionEnded += Show;
    }

    // 구독을 OnEnable이 아니라 Awake에 두는 이유는 위의 주석과 같다. 이 오브젝트가
    // 어떤 이유로든 꺼졌다 켜지는 상황에서도 구독이 끊기지 않는다.
    private void OnDestroy()
    {
        if (session != null)
        {
            session.SessionEnded -= Show;
        }
    }

    private void Show(MiniGameResult result)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = result.Cleared ? clearedTitle : failedTitle;
            titleText.color = result.Cleared ? clearedColor : failedColor;
        }

        if (ratingText != null)
        {
            ratingText.text = string.Format(ratingFormat, result.FinalRating, result.RequiredRating);
        }

        if (countText != null)
        {
            countText.text = string.Format(countFormat, result.CorrectCount, result.ResolvedCount);
        }
    }
}
