using UnityEngine;

/// <summary>
/// 메인 화면에서 미니게임으로 들어가는 문. 버튼의 OnClick에 <see cref="Enter"/>를 걸거나,
/// 방 안의 사물에 붙여 클릭 처리에서 불러도 된다.
/// </summary>
public class MiniGameEntry : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워두면 Resources의 GameFlow를 자동으로 씁니다. 이미 꽂아둔 것이 있으면 그걸 씁니다.")]
    [SerializeField] private GameFlow flow;

    [Tooltip("이 문이 들어갈 미니게임.")]
    [SerializeField] private MiniGameDefinition miniGame;

    [Header("클리어 표시 (선택)")]
    [Tooltip("이미 깼을 때 켤 오브젝트.")]
    [SerializeField] private GameObject clearedView;

    [Tooltip("아직 못 깼을 때 켤 오브젝트.")]
    [SerializeField] private GameObject notClearedView;

    /// <summary>이 문이 가리키는 미니게임. 메인 화면 UI가 이름·아이콘을 그릴 때 쓴다.</summary>
    public MiniGameDefinition MiniGame => miniGame;

    /// <summary>이미 깬 미니게임인지. 잠금 표시나 체크 표시에 쓴다.</summary>
    public bool IsCleared => Flow != null && Flow.IsCleared(miniGame);

    /// <summary>깼을 때 켜지는 모습. 허브 연출이 바꿔치기 순간을 잡을 때 쓴다.</summary>
    public GameObject ClearedView => clearedView;

    /// <summary>못 깼을 때 켜지는 모습.</summary>
    public GameObject NotClearedView => notClearedView;

    private GameFlow Flow => flow != null ? flow : GameFlow.Instance;

    private void Awake()
    {
        if (miniGame == null)
        {
            Debug.LogError($"{nameof(MiniGameEntry)} on '{name}': Definition을 연결하세요.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        GameFlow current = Flow;
        if (current != null)
        {
            current.ProgressChanged += RefreshClearMark;
        }

        RefreshClearMark();
    }

    // 구독 해제가 필수다. GameFlow는 씬보다 오래 사는 에셋이라, 떼지 않으면 파괴된 허브
    // 오브젝트를 계속 붙잡고 있다가 다음 허브에서 예외를 던진다.
    private void OnDisable()
    {
        GameFlow current = Flow;
        if (current != null)
        {
            current.ProgressChanged -= RefreshClearMark;
        }
    }

    /// <summary>
    /// 클리어 표시를 지금 상태에 맞춘다.
    ///
    /// 허브는 미니게임이 결과를 보고하는 순간 로드돼 있지 않아 이벤트를 못 받는다.
    /// 그래서 이벤트만 믿지 않고 켜질 때마다 직접 읽는다.
    /// </summary>
    public void RefreshClearMark() => ShowClearMark(IsCleared);

    /// <summary>
    /// 실제 기록과 상관없이 겉모습만 바꾼다. 방금 깨고 돌아온 오브젝트를 연출 전까지
    /// 못 깬 모습으로 붙잡아 둘 때 쓴다.
    /// </summary>
    public void ShowClearMark(bool cleared)
    {
        if (clearedView != null)
        {
            clearedView.SetActive(cleared);
        }

        if (notClearedView != null)
        {
            notClearedView.SetActive(!cleared);
        }
    }

    /// <summary>들어간다. Button OnClick에 그대로 연결할 수 있다.</summary>
    public void Enter()
    {
        if (!enabled)
        {
            return;
        }

        GameFlow current = Flow;
        if (current != null)
        {
            current.LoadMiniGame(miniGame);
        }
    }
}
