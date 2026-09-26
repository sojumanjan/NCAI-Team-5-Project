// 허브 옵션 창에 "게임 스킵" 탭을 더해, 한 번이라도 들어가 본 미니게임을 클리어로 건너뛰게 해주는 컴포넌트
using System;
using Taegeon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 창 탭 전환은 공용 프리팹의 OptionsMenuController가 두 탭만 알고 한다. 그 코드와 프리팹은 미니게임들도
/// 쓰므로 고치지 않고, 허브 씬에만 세 번째 탭을 얹는다 — 스킵 탭을 누르면 두 패널을 끄고 스킵 화면을 켜고,
/// 기존 탭을 누르면 스킵 화면만 치운다. 나머지는 원래 컨트롤러가 그대로 한다.
///
/// 옵션 창(Option) 안의 오브젝트에 붙인다. 창이 닫히면 같이 꺼지며 스킵 화면을 치워, 다음에 열 때 원래 탭으로 시작한다.
///
/// 건너뛰기는 실제로 깨고 돌아온 것과 똑같이 GameFlow에 클리어를 보고한 뒤 허브의 클리어 연출을 바로 튼다.
/// 오브젝트 변신·보상·씨앗 진화·엔딩까지 같은 길을 탄다.
/// </summary>
public class HubGameSkipMenu : MonoBehaviour
{
    [Serializable]
    private class Row
    {
        [Tooltip("이 줄이 가리키는 허브 오브젝트.")]
        public MiniGameEntry entry;

        [Tooltip("오브젝트 그림을 띄울 칸. 허브 오브젝트와 똑같이 클리어 전/후 그림으로 바뀝니다.")]
        public Image icon;

        [Tooltip("게임 이름 글자. 코드가 바꾸지 않으니 여기 적은 이름이 그대로 나오고, 확인 창에도 이 이름이 들어갑니다.")]
        public TMP_Text title;

        [Tooltip("지금 건너뛸 수 있는지 알려주는 글자.")]
        public TMP_Text status;

        public Button skipButton;
    }

    [Header("탭")]
    [SerializeField] private Button skipTabButton;

    [Tooltip("스킵 탭이 선택됐을 때의 모양.")]
    [SerializeField] private GameObject tabSelectedView;

    [Tooltip("스킵 탭이 선택되지 않았을 때의 모양.")]
    [SerializeField] private GameObject tabNormalView;

    [Tooltip("원래 있던 탭 버튼들 (일반, 게임 방법). 누르면 스킵 화면을 치웁니다.")]
    [SerializeField] private Button[] otherTabButtons;

    [Tooltip("원래 있던 탭 화면들 (Setting Panel, Tooltip Panel). 스킵 탭을 누르면 끕니다.")]
    [SerializeField] private GameObject[] otherPanels;

    [Tooltip("스킵 화면 전체. 배경 그림에 그려진 '일반' 선택 표시를 덮는 조각도 여기 넣어 둡니다.")]
    [SerializeField] private GameObject page;

    [Header("게임 목록")]
    [SerializeField] private Row[] rows;

    [SerializeField] private string clearedText = "이미 클리어했어요";
    [SerializeField] private string lockedText = "최소 한 번 플레이 한 이후에 스킵할 수 있어요.";
    [SerializeField] private string readyText = "건너뛰면 클리어한 것으로 쳐요";

    [Header("확인 창")]
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private TMP_Text confirmMessage;

    [Tooltip("{0} 자리에 게임 이름이 들어갑니다.")]
    [SerializeField] private string confirmFormat = "{0}\n건너뛸까요?";

    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("연결")]
    [Tooltip("옵션 창을 닫고 시간을 되돌릴 공용 메뉴. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private MenuEscapeToggle menu;

    [Tooltip("건너뛴 뒤 틀 허브 클리어 연출. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private HubClearReveal reveal;

    private MiniGameEntry _pending;

    private void Awake()
    {
        if (menu == null) menu = FindAnyObjectByType<MenuEscapeToggle>();
        if (reveal == null) reveal = FindAnyObjectByType<HubClearReveal>();

        // 이 컴포넌트는 버튼들과 수명이 같아 한 번만 걸고 떼지 않는다.
        if (skipTabButton != null) skipTabButton.onClick.AddListener(ShowPage);

        if (otherTabButtons != null)
        {
            foreach (Button tab in otherTabButtons)
            {
                if (tab != null) tab.onClick.AddListener(HidePage);
            }
        }

        if (rows != null)
        {
            foreach (Row row in rows)
            {
                if (row != null && row.skipButton != null)
                {
                    Row captured = row;
                    row.skipButton.onClick.AddListener(() => AskSkip(captured));
                }
            }
        }

        if (yesButton != null) yesButton.onClick.AddListener(ConfirmSkip);
        if (noButton != null) noButton.onClick.AddListener(CloseConfirm);

        HidePage();
    }

    private void OnDisable()
    {
        HidePage();
    }

    private void ShowPage()
    {
        if (otherPanels != null)
        {
            foreach (GameObject panel in otherPanels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        if (page != null) page.SetActive(true);
        SetTabSelected(true);
        CloseConfirm();
        RefreshRows();
    }

    private void HidePage()
    {
        if (page != null) page.SetActive(false);
        SetTabSelected(false);
        CloseConfirm();
    }

    private void SetTabSelected(bool selected)
    {
        if (tabSelectedView != null) tabSelectedView.SetActive(selected);
        if (tabNormalView != null) tabNormalView.SetActive(!selected);
    }

    private void RefreshRows()
    {
        GameFlow flow = GameFlow.Instance;
        // 클리어 연출이 도는 중에 또 보고하면 연출 하나가 통째로 묻힌다.
        bool busy = reveal != null && reveal.IsPlaying;

        foreach (Row row in rows)
        {
            if (row == null || row.entry == null) continue;

            MiniGameDefinition game = row.entry.MiniGame;
            bool cleared = flow != null && flow.IsCleared(game);
            bool visited = flow != null && flow.HasVisited(game);

            if (row.icon != null) row.icon.sprite = ViewSprite(cleared ? row.entry.ClearedView : row.entry.NotClearedView, row.icon.sprite);
            if (row.status != null) row.status.text = cleared ? clearedText : visited ? readyText : lockedText;
            if (row.skipButton != null) row.skipButton.interactable = !cleared && visited && !busy;
        }
    }

    // 그림을 따로 들고 있지 않고 허브 오브젝트의 것을 가져온다. 오브젝트 그림을 바꾸면 여기도 따라 바뀐다.
    private static Sprite ViewSprite(GameObject view, Sprite fallback)
    {
        Image image = view != null ? view.GetComponent<Image>() : null;
        return image != null && image.sprite != null ? image.sprite : fallback;
    }

    private void AskSkip(Row row)
    {
        if (row.entry == null || row.entry.MiniGame == null) return;

        _pending = row.entry;
        // 줄에 직접 적어 둔 이름을 따른다. 비어 있을 때만 게임 설정의 이름으로 채운다.
        string gameName = row.title != null && !string.IsNullOrWhiteSpace(row.title.text) ? row.title.text : row.entry.MiniGame.DisplayName;
        if (confirmMessage != null) confirmMessage.text = string.Format(confirmFormat, gameName);
        if (confirmRoot != null) confirmRoot.SetActive(true);
    }

    private void CloseConfirm()
    {
        _pending = null;
        if (confirmRoot != null) confirmRoot.SetActive(false);
    }

    private void ConfirmSkip()
    {
        MiniGameEntry entry = _pending;
        GameFlow flow = GameFlow.Instance;
        CloseConfirm();

        if (entry == null || flow == null) return;

        // 점수는 어디서도 쓰지 않는다. 0으로 남겨 두면 나중에 점수를 쓰게 돼도 건너뛴 게임이 구분된다.
        flow.Report(entry.MiniGame, new MiniGameResult(true, 0f));

        // 창을 먼저 닫아 멈춘 시간을 되돌려야 연출이 흐른다.
        if (menu != null) menu.Resume();
        if (reveal != null) reveal.PlayPending(false);
    }
}
