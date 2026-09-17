using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yusong
{
public class GameGuide : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private TextMeshProUGUI nextButtonLabel;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private UITheme theme;

    private int currentPage;

    private void Awake()
    {
        if (theme != null)
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = theme.gameOverBackgroundColor;
        }

        if (prevButton != null) prevButton.onClick.AddListener(GoToPrevious);
        if (nextButton != null) nextButton.onClick.AddListener(GoToNextOrFinish);
        if (skipButton != null) skipButton.onClick.AddListener(Dismiss);

        currentPage = 0;
        ShowPage(0);
        Time.timeScale = 0f;
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == index);
        }

        if (pageIndicatorText != null) pageIndicatorText.text = (index + 1) + " / " + pages.Length;
        if (prevButton != null) prevButton.gameObject.SetActive(index > 0);
        if (nextButtonLabel != null) nextButtonLabel.text = index == pages.Length - 1 ? "시작하기" : "다음";
    }

    private void GoToPrevious()
    {
        if (currentPage <= 0) return;
        currentPage--;
        ShowPage(currentPage);
    }

    private void GoToNextOrFinish()
    {
        if (currentPage >= pages.Length - 1)
        {
            Dismiss();
            return;
        }

        currentPage++;
        ShowPage(currentPage);
    }

    private void Dismiss()
    {
        if (ComboManager.Instance != null) ComboManager.Instance.ResetState();

        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
}
