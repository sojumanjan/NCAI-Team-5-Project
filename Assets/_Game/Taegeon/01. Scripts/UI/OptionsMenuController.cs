using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OptionsMenuController : MonoBehaviour
{
    #region 참조 및 설정

    [Header("Buttons")]
    [SerializeField] private Button menuButton;
    [SerializeField] private Button graphicsButton;
    [SerializeField] private Button audioButton;

    [Header("Windows")]
    [SerializeField] private GameObject optionWindow;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;

    private bool audioSelected;

    #endregion

    #region 초기화 및 이벤트 연결

    /// <summary>
    /// 옵션 창을 닫힌 상태로 준비합니다.
    /// </summary>
    private void Awake()
    {
        Close();
    }

    /// <summary>
    /// 옵션 창과 탭 버튼에 이벤트를 연결합니다.
    /// </summary>
    private void OnEnable()
    {
        if (menuButton != null) menuButton.onClick.AddListener(Toggle);
        if (graphicsButton != null) graphicsButton.onClick.AddListener(ShowGraphics);
        if (audioButton != null) audioButton.onClick.AddListener(ShowAudio);
    }

    /// <summary>
    /// 버튼 이벤트를 해제하고 옵션 창을 닫습니다.
    /// </summary>
    private void OnDisable()
    {
        if (menuButton != null) menuButton.onClick.RemoveListener(Toggle);
        if (graphicsButton != null) graphicsButton.onClick.RemoveListener(ShowGraphics);
        if (audioButton != null) audioButton.onClick.RemoveListener(ShowAudio);
        Close();
    }

    #endregion

    #region 옵션 창 제어

    /// <summary>
    /// 옵션 창의 열림 상태를 전환합니다.
    /// </summary>
    public void Toggle()
    {
        if (optionWindow == null) return;
        if (optionWindow.activeSelf) Close();
        else Open();
    }

    /// <summary>
    /// 선택한 탭으로 옵션 창을 엽니다.
    /// </summary>
    public void Open()
    {
        if (optionWindow == null) return;
        ApplyTab();
        optionWindow.SetActive(true);
    }

    /// <summary>
    /// 옵션 창과 하위 패널을 닫습니다.
    /// </summary>
    public void Close()
    {
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (optionWindow != null) optionWindow.SetActive(false);
    }

    #endregion

    #region 설정 탭 전환

    /// <summary>
    /// 그래픽 설정 탭으로 전환합니다.
    /// </summary>
    public void ShowGraphics()
    {
        audioSelected = false;
        ApplyTab();
    }

    /// <summary>
    /// 오디오 설정 탭으로 전환합니다.
    /// </summary>
    public void ShowAudio()
    {
        audioSelected = true;
        ApplyTab();
    }

    /// <summary>
    /// 현재 선택한 설정 패널만 표시합니다.
    /// </summary>
    private void ApplyTab()
    {
        // 선택한 패널을 표시하기 전에 다른 패널을 숨깁니다.
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
    #endregion

}
