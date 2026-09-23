using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면에서 미니게임으로 들어가는 문. 버튼의 OnClick에 <see cref="Enter"/>를 걸거나,
/// 방 안의 사물에 붙여 클릭 처리에서 불러도 된다.
///
/// 들어갈 때 화면을 검게 덮은 뒤 넘어간다. 미니게임 씬은 검은 화면에서 시작한다고 팀이 약속했고,
/// 거기서 걷어내는 입장 연출은 각 미니게임이 맡는다.
/// </summary>
public class MiniGameEntry : MonoBehaviour
{
    // 허브 UI(최대 100)와 옵션 창(21)까지 전부 덮어야 암전이 끊기지 않는다.
    private const int COVER_SORTING_ORDER = 1000;

    [Header("전환")]
    [Tooltip("플레이하기를 누른 뒤 화면이 검게 덮이는 시간 (초). 배경음도 같은 시간 동안 사그라듭니다.")]
    [SerializeField] private float fadeOutSeconds = 1.2f;

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

    // 문이 여러 개라 덮이는 동안 다른 문을 눌러도 씬 로드가 두 번 걸리지 않게 함께 막는다.
    private static bool _entering;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => _entering = false;

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
        if (!enabled || _entering)
        {
            return;
        }

        GameFlow current = Flow;
        if (current == null)
        {
            return;
        }

        // 빌드에 없는 씬이면 덮은 뒤 넘어가지 못해 검은 화면에 갇힌다. 덮지 않고 GameFlow가 에러만 남기게 둔다.
        if (string.IsNullOrWhiteSpace(miniGame.SceneName) || !Application.CanStreamedLevelBeLoaded(miniGame.SceneName))
        {
            current.LoadMiniGame(miniGame);
            return;
        }

        _entering = true;
        StartCoroutine(FadeOutThenLoad(current));
    }

    private IEnumerator FadeOutThenLoad(GameFlow current)
    {
        // 허브 캔버스 안에 만들면 중첩 캔버스가 되어 정렬 순서가 무시된다. 씬 최상위에 둔다.
        GameObject coverRoot = ScreenInputBlocker.Create(null, "MiniGameEntryCover");
        coverRoot.GetComponent<Canvas>().sortingOrder = COVER_SORTING_ORDER;
        coverRoot.GetComponentInChildren<Image>(true).color = Color.black;

        CanvasGroup group = coverRoot.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        coverRoot.SetActive(true);

        // 화면과 같이 사그라들게 한다. 다 검어진 뒤에 끄면 소리만 뚝 끊긴다.
        AudioManager.StopBGM(fadeOutSeconds);

        float elapsed = 0f;
        float total = Mathf.Max(0.01f, fadeOutSeconds);

        while (elapsed < total)
        {
            // 옵션 창이 시간을 0으로 눌러둔 채일 수 있다. 게임 시간으로 재면 영원히 덮이지 않는다.
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / total);
            yield return null;
        }

        group.alpha = 1f;

        // static이라 허브로 돌아와도 남는다. 풀지 않으면 다음부터 어떤 문도 열리지 않는다.
        _entering = false;
        current.LoadMiniGame(miniGame);
    }

    private void OnValidate()
    {
        fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
    }
}
