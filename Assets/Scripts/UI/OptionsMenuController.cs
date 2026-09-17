using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OptionsMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button menuButton;
    [SerializeField] private Button graphicsButton;
    [SerializeField] private Button audioButton;

    [Header("Windows")]
    [SerializeField] private GameObject optionWindow;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;

    private bool audioSelected;

    private void Awake()
    {
        Close();
    }

    private void OnEnable()
    {
        if (menuButton != null) menuButton.onClick.AddListener(Toggle);
        if (graphicsButton != null) graphicsButton.onClick.AddListener(ShowGraphics);
        if (audioButton != null) audioButton.onClick.AddListener(ShowAudio);
    }

    private void OnDisable()
    {
        if (menuButton != null) menuButton.onClick.RemoveListener(Toggle);
        if (graphicsButton != null) graphicsButton.onClick.RemoveListener(ShowGraphics);
        if (audioButton != null) audioButton.onClick.RemoveListener(ShowAudio);
        Close();
    }

    public void Toggle()
    {
        if (optionWindow == null) return;
        if (optionWindow.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (optionWindow == null) return;
        ApplyTab();
        optionWindow.SetActive(true);
    }

    public void Close()
    {
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (optionWindow != null) optionWindow.SetActive(false);
    }

    public void ShowGraphics()
    {
        audioSelected = false;
        ApplyTab();
    }

    public void ShowAudio()
    {
        audioSelected = true;
        ApplyTab();
    }

    private void ApplyTab()
    {
        // Hide the other panel before showing the selected one.
        if (audioSelected)
        {
            if (graphicsPanel != null) graphicsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(true);
        }
        else
        {
            if (audioPanel != null) audioPanel.SetActive(false);
            if (graphicsPanel != null) graphicsPanel.SetActive(true);
        }
    }
}
