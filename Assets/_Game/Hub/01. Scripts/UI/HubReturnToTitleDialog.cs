// 허브에서 "메인 메뉴로 돌아가시겠습니까?"를 묻고, 예를 누르면 화면을 덮은 뒤 타이틀 씬으로 보내는 확인 창
using DG.Tweening;
using Taegeon;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 확인 창 루트에 붙인다. 루트는 씬에서 꺼 둔 채 두고, 허브의 메인 메뉴 버튼이 <see cref="Open"/>을 부른다.
/// 모양(글자·그림·위치)은 자식 오브젝트에서 직접 꾸미면 된다. 이 스크립트는 열고 닫는 연출과 이동만 맡는다.
///
/// 열려 있는 동안 공용 Menu Canvas의 ESC를 잠시 끈다. 안 끄면 ESC 한 번에 이 창이 닫히면서 설정 창까지 열린다.
/// </summary>
public class HubReturnToTitleDialog : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("창 전체의 투명도. 비워두면 이 오브젝트에서 찾거나 붙입니다.")]
    [SerializeField] private CanvasGroup group;

    [Tooltip("톡 튀어나올 창 본체.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("열려 있는 동안 ESC를 잠시 끌 공용 메뉴. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private MenuEscapeToggle sharedMenu;

    [Header("이동")]
    [Tooltip("예를 누르면 갈 씬. Build Settings에 들어 있어야 합니다.")]
    [SerializeField] private string titleSceneName = "Main_Logo";

    [Tooltip("예를 누른 뒤 화면이 검게 덮이는 시간 (초). 배경음도 같은 시간 동안 사그라듭니다.")]
    [SerializeField] private float fadeOutSeconds = 1f;

    [Header("연출")]
    [Tooltip("창이 열리고 닫히는 시간 (초).")]
    [SerializeField] private float popDuration = 0.25f;

    private bool _open;
    private bool _leaving;
    private Vector3 _panelScale = Vector3.one;

    private void Awake()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        if (panel != null) _panelScale = panel.localScale;

        if (sharedMenu == null) sharedMenu = FindAnyObjectByType<MenuEscapeToggle>();
    }

    /// <summary>창을 연다. 메인 메뉴 버튼의 OnClick에 연결한다.</summary>
    public void Open()
    {
        if (_open || _leaving) return;

        gameObject.SetActive(true);
        _open = true;

        if (sharedMenu != null) sharedMenu.enabled = false;

        group.DOKill();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;
        // 허브가 멈춰 있지 않더라도, 옵션 창이 시간을 멈춘 뒤에 열릴 수 있어 실제 시간으로 돌린다.
        group.DOFade(1f, popDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);

        if (panel != null)
        {
            panel.DOKill();
            panel.localScale = _panelScale * 0.85f;
            panel.DOScale(_panelScale, popDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }
    }

    /// <summary>아니오. 창을 닫고 허브로 돌아간다.</summary>
    public void Close()
    {
        if (!_open || _leaving) return;

        _open = false;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (panel != null)
        {
            panel.DOKill();
            panel.DOScale(_panelScale * 0.9f, popDuration * 0.8f).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
        }

        group.DOKill();
        group.DOFade(0f, popDuration * 0.8f).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
        {
            if (panel != null) panel.localScale = _panelScale;
            gameObject.SetActive(false);
        });

        if (sharedMenu != null) sharedMenu.enabled = true;
    }

    /// <summary>예. 화면을 검게 덮은 뒤 타이틀 씬으로 간다.</summary>
    public void Confirm()
    {
        if (!_open || _leaving) return;

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError($"{nameof(HubReturnToTitleDialog)}: '{titleSceneName}' 씬이 Build Settings에 없습니다.", this);
            return;
        }

        _leaving = true;
        group.interactable = false;

        // 이 창과 옵션 창 위까지 전부 덮어야 한다. 다른 화면 전환 덮개와 같은 순서(1000)를 쓴다.
        GameObject coverRoot = ScreenInputBlocker.Create(null, "ReturnToTitleCover");
        coverRoot.GetComponent<Canvas>().sortingOrder = 1000;
        coverRoot.GetComponentInChildren<Image>(true).color = Color.black;

        CanvasGroup cover = coverRoot.AddComponent<CanvasGroup>();
        cover.alpha = 0f;
        coverRoot.SetActive(true);

        AudioManager.StopBGM(fadeOutSeconds);

        cover.DOFade(1f, Mathf.Max(0.01f, fadeOutSeconds)).SetEase(Ease.Linear).SetUpdate(true).SetLink(coverRoot).OnComplete(() =>
        {
            // 옵션 창 등이 시간을 멈춘 채 넘어가면 타이틀이 얼어붙는다.
            Time.timeScale = 1f;
            SceneManager.LoadScene(titleSceneName);
        });
    }

    private void Update()
    {
        if (!_open || _leaving) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDisable()
    {
        // 창이 열린 채 씬을 떠나거나 꺼져도 공용 메뉴의 ESC가 꺼진 채 남지 않게 한다.
        if (sharedMenu != null && !_leaving) sharedMenu.enabled = true;
    }

    private void OnValidate()
    {
        fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
        popDuration = Mathf.Max(0.01f, popDuration);
    }
}
