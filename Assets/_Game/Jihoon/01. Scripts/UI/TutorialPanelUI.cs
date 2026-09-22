using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 처음 온 사람에게 만드는 순서를 시켜보는 쪽지. 기본 P로 접고 편다.
///
/// 읽히는 설명서가 아니라 <b>따라 하는 차례표</b>다. 한 줄씩 시키고, 실제로 그 일이
/// 일어났는지 게임 상태를 보고 판단해서 다음 줄로 넘어간다. 버튼을 눌러 넘기게 하면
/// 읽지 않고 연타하기 때문에 아무것도 배우지 못한다.
///
/// 막지는 않는다. 다 무시하고 셔터를 열어도 되고, 순서를 건너뛰어 먼저 해버려도 그 줄은
/// 그대로 통과한다 — 아는 사람을 붙잡아두지 않으려는 것이다.
///
/// 조건은 기구를 하나하나 지목하지 않고 <see cref="StationKind"/>로 본다. 오븐과
/// 커피머신이 두 대씩이라, 어느 쪽으로 가든 같게 처리되어야 한다.
/// </summary>
public class TutorialPanelUI : MonoBehaviour
{
    /// <summary>한 줄이 끝났다고 볼 조건.</summary>
    public enum StepGoal
    {
        /// <summary>그 물건을 손에 들면 끝.</summary>
        Holding,

        /// <summary>그 기구에 재료가 전부 들어가면 끝.</summary>
        StationLoaded,

        /// <summary>그 기구가 돌기 시작하면 끝. 홀드식은 완성까지 한 번에 간다.</summary>
        StationRunning,

        /// <summary>셔터를 올리면 끝. 다 올라갈 때까지 기다리지 않는다.</summary>
        ShopOpened,

        /// <summary>마감 시각이 지나 새 손님이 끊기면 끝.</summary>
        ShopClosing,

        /// <summary>셔터를 내리면 끝. 다 내려갈 때까지 기다리지 않는다.</summary>
        ShopClosed,
    }

    [Serializable]
    public class Step
    {
        [TextArea(1, 3)]
        [Tooltip("시킬 일.")]
        public string text;

        [Tooltip("무엇을 보고 끝났다고 판단할지.")]
        public StepGoal goal = StepGoal.Holding;

        [Tooltip("어떤 기구를 볼지. Holding일 때는 쓰지 않습니다.")]
        public StationKind station;

        [Tooltip("손에 들 것, 또는 기구에 들어가야 할 재료들.")]
        public ItemData[] items;
    }

    [Serializable]
    public class Sequence
    {
        [Tooltip("이 묶음의 제목.")]
        public string title;

        public Step[] steps;
    }

    [Header("참조")]
    [Tooltip("펼쳤을 때 보일 것. 이 오브젝트 자신을 넣으면 안 됩니다 — 꺼지는 순간 " +
             "Update가 멈춰 다시 켤 수 없습니다.")]
    [SerializeField] private GameObject openView;

    [Tooltip("접었을 때 보일 것. 보통 [P]로 다시 열 수 있다고 알려주는 작은 조각입니다. " +
             "비워두면 접었을 때 아무것도 안 보입니다.")]
    [SerializeField] private GameObject closedView;

    [Tooltip("차례를 그릴 글자.")]
    [SerializeField] private TMP_Text bodyText;

    [Tooltip("묶음 제목을 그릴 글자. 비워두면 제목을 표시하지 않습니다.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("손 상태를 볼 대상. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private PlayerHands hands;

    [Header("열고 닫기")]
    [Tooltip("접고 펴는 키.")]
    [SerializeField] private Key toggleKey = Key.P;

    [Tooltip("시작하자마자 펼쳐둘지.")]
    [SerializeField] private bool openOnStart = true;

    [Tooltip("마지막 줄까지 끝내면 스스로 접습니다. P로 다시 펼 수는 있습니다.")]
    [SerializeField] private bool hideWhenDone = true;

    [Header("차례")]
    [SerializeField] private Sequence[] sequences;

    [Header("모양")]
    [Tooltip("이미 끝낸 줄의 색. 지우지 않고 흐리게만 남깁니다.")]
    [SerializeField] private Color doneColor = new Color(0.72f, 0.72f, 0.72f, 0.4f);

    [Tooltip("줄 번호 형식. {0}이 번호입니다. 비워두면 번호를 붙이지 않습니다.")]
    [SerializeField] private string numberFormat = "{0}. ";

    [Header("문구")]
    [Tooltip("전부 끝냈을 때 보여줄 말.")]
    [SerializeField] private string allDoneText = "모두 끝냈습니다. 셔터를 열고 장사를 시작하세요.";

    [Tooltip("전부 끝냈을 때의 제목. 비워두면 제목 줄이 사라집니다.")]
    [SerializeField] private string allDoneTitle = "";

    private readonly StringBuilder _builder = new();
    private readonly List<StationBase> _stations = new();

    private ShopOpener _shopOpener;
    private MiniGameSession _session;

    private int _sequence;
    private int _step;
    private bool _finished;

    private void Awake()
    {
        if (openView == null || bodyText == null)
        {
            Debug.LogError($"{nameof(TutorialPanelUI)} on '{name}': 펼친 패널과 글자가 모두 필요합니다.", this);
            enabled = false;
            return;
        }

        if (openView == gameObject || closedView == gameObject)
        {
            Debug.LogError($"{nameof(TutorialPanelUI)} on '{name}': 이 오브젝트 자신을 넣으면 " +
                           "꺼진 뒤 다시 켤 수 없습니다. 자식 오브젝트를 넣으세요.", this);
            enabled = false;
            return;
        }

        if (hands == null)
        {
            hands = FindFirstObjectByType<PlayerHands>();
        }

        _stations.AddRange(FindObjectsByType<StationBase>(FindObjectsSortMode.None));
        _shopOpener = FindFirstObjectByType<ShopOpener>();
        _session = FindFirstObjectByType<MiniGameSession>();

        SkipEmpty();
        Redraw();
        SetOpen(openOnStart);
    }

    private void Update()
    {
        ReadToggle();

        if (_finished)
        {
            return;
        }

        // 접혀 있어도 계속 본다. 쪽지를 닫아두고 만든 사람도 진도가 나가야 한다.
        if (CurrentStep() != null && IsDone(CurrentStep()))
        {
            Advance();
        }
    }

    private void ReadToggle()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }
    }

    /// <summary>지금 펼쳐져 있는지.</summary>
    public bool IsOpen => openView != null && openView.activeSelf;

    /// <summary>펼쳐져 있으면 접고, 접혀 있으면 편다.</summary>
    public void Toggle() => SetOpen(!IsOpen);

    /// <summary>
    /// 펼치거나 접는다. 패널을 통째로 지우지 않고 <b>둘 중 하나만</b> 켠다 —
    /// 접힌 쪽이 "P로 다시 열 수 있다"를 계속 알려줘야, 한 번 접은 사람이 방법을 잊지 않는다.
    /// </summary>
    public void SetOpen(bool open)
    {
        if (openView != null)
        {
            openView.SetActive(open);
        }

        if (closedView != null)
        {
            closedView.SetActive(!open);
        }
    }

    // ---------------------------------------------------------------- 진행

    private Step CurrentStep()
    {
        if (sequences == null || _sequence >= sequences.Length)
        {
            return null;
        }

        Sequence sequence = sequences[_sequence];
        if (sequence == null || sequence.steps == null || _step >= sequence.steps.Length)
        {
            return null;
        }

        return sequence.steps[_step];
    }

    private void Advance()
    {
        _step++;
        SkipEmpty();
        Redraw();
    }

    /// <summary>비어 있는 묶음이나 줄을 건너뛰고, 끝에 닿으면 마무리한다.</summary>
    private void SkipEmpty()
    {
        while (sequences != null && _sequence < sequences.Length)
        {
            Sequence sequence = sequences[_sequence];
            int count = sequence != null && sequence.steps != null ? sequence.steps.Length : 0;

            if (_step < count)
            {
                return;
            }

            _sequence++;
            _step = 0;
        }

        Finish();
    }

    private void Finish()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;

        if (hideWhenDone)
        {
            SetOpen(false);
        }
    }

    // ---------------------------------------------------------------- 판정

    private bool IsDone(Step step)
    {
        switch (step.goal)
        {
            case StepGoal.Holding:
                return hands != null && step.items != null && step.items.Length > 0
                       && hands.HeldItem == step.items[0];

            case StepGoal.StationLoaded:
                return AnyStation(step.station, s => HasAll(s, step.items));

            case StepGoal.StationRunning:
                // 홀드식 기구는 Processing을 거치지 않고 곧장 Done으로 간다. 둘 다 "돌렸다"로 본다.
                return AnyStation(step.station,
                                  s => s.State == StationState.Processing || s.State == StationState.Done);

            case StepGoal.ShopOpened:
                return _shopOpener != null && _shopOpener.HasOpened;

            case StepGoal.ShopClosing:
                // Ended까지 통과로 보는 이유: 이 줄을 건너뛴 채 하루가 끝나버리면
                // 영영 안 끝나는 줄 하나가 남는다.
                return _session != null
                       && (_session.State == CookingSessionState.Closing
                           || _session.State == CookingSessionState.Ended);

            case StepGoal.ShopClosed:
                return _shopOpener != null && _shopOpener.HasClosed;

            default:
                return false;
        }
    }

    private bool AnyStation(StationKind kind, Func<StationBase, bool> test)
    {
        foreach (StationBase station in _stations)
        {
            if (station != null && station.Kind == kind && test(station))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAll(StationBase station, ItemData[] items)
    {
        if (items == null || items.Length == 0)
        {
            return false;
        }

        IReadOnlyList<ItemData> loaded = station.Loaded;

        foreach (ItemData needed in items)
        {
            if (needed == null)
            {
                continue;
            }

            bool found = false;
            foreach (ItemData have in loaded)
            {
                if (have == needed)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------- 그리기

    private void Redraw()
    {
        _builder.Clear();

        if (_finished || sequences == null || _sequence >= sequences.Length)
        {
            SetTitle(allDoneTitle);
            bodyText.text = allDoneText;
            return;
        }

        Sequence sequence = sequences[_sequence];
        SetTitle(sequence.title);

        // 색을 줄마다 다르게 하려면 리치 텍스트가 켜져 있어야 한다. 꺼져 있으면 태그가
        // 글자 그대로 찍혀 나온다.
        bodyText.richText = true;

        string dimOpen = "<color=#" + ColorUtility.ToHtmlStringRGBA(doneColor) + ">";
        int number = 0;

        for (int i = 0; i < sequence.steps.Length; i++)
        {
            Step step = sequence.steps[i];
            if (step == null || string.IsNullOrEmpty(step.text))
            {
                continue;
            }

            number++;

            string label = string.IsNullOrEmpty(numberFormat)
                ? step.text
                : string.Format(numberFormat, number) + step.text;

            // 끝낸 줄도 지우지 않고 흐리게만 남긴다. 방금 뭘 했는지가 보여야 흐름이 읽힌다.
            _builder.AppendLine(i < _step ? dimOpen + label + "</color>" : label);
        }

        bodyText.text = _builder.ToString().TrimEnd();
    }

    private void SetTitle(string value)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.text = value;
        titleText.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }
}
