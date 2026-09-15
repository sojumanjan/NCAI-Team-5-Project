using UnityEngine;

public class MiniGameFlowManager : MonoBehaviour
{
    public static MiniGameFlowManager Instance { get; private set; }

    [SerializeField] private GameObject gameSelectUIRoot;
    [SerializeField] private GameObject tetrisRoot;
    [SerializeField] private GameObject pacmanRoot;
    [SerializeField] private FadeCanvas fadeCanvas;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameObject selectSceneCamera;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gameSelectUIRoot.SetActive(true);
        tetrisRoot.SetActive(false);
        selectSceneCamera.SetActive(true);

        if (pacmanRoot != null)
        {
            pacmanRoot.SetActive(false);
        }
    }

    public void ShowGameSelect()
    {
        SwitchTo(gameSelectUIRoot, false);
    }

    public void StartTetris()
    {
        SwitchTo(tetrisRoot, true);
    }

    public void StartPacman()
    {
        SwitchTo(pacmanRoot, true);
    }

    private void SwitchTo(GameObject target, bool playCountdown)
    {
        SetPlayerControlsLocked(true);

        fadeCanvas.FadeOut(() =>
        {
            gameSelectUIRoot.SetActive(target == gameSelectUIRoot);
            tetrisRoot.SetActive(target == tetrisRoot);
            selectSceneCamera.SetActive(target == gameSelectUIRoot);

            if (pacmanRoot != null)
            {
                pacmanRoot.SetActive(target == pacmanRoot);
            }

            fadeCanvas.FadeIn(() =>
            {
                // 카운트다운 화면(3,2,1)이 보이는 동안에도 플레이어는 바로 움직일 수 있어야 하므로,
                // 조작 잠금은 카운트다운을 재생하기 전에 미리 풀어둔다.
                SetPlayerControlsLocked(false);

                if (playCountdown)
                {
                    countdownUI.Play(countdownStartFrom, () => OnGameplayReady(target));
                }
            });
        });
    }

    private void OnGameplayReady(GameObject target)
    {
        if (target == tetrisRoot)
        {
            var sequencer = tetrisRoot.GetComponentInChildren<TetrisFallSequencer>();
            if (sequencer != null)
            {
                sequencer.StartSequence();
            }
        }
    }

    private void SetPlayerControlsLocked(bool locked)
    {
        // 대상 루트가 아직 비활성 상태일 수 있어 FindGameObjectWithTag로는 찾지 못한다.
        // tetrisRoot/pacmanRoot 계층에서 직접(비활성 포함) 탐색한다.
        SetPlayerControlsLockedInRoot(tetrisRoot, locked);
        SetPlayerControlsLockedInRoot(pacmanRoot, locked);
    }

    private void SetPlayerControlsLockedInRoot(GameObject root, bool locked)
    {
        if (root == null)
        {
            return;
        }

        var playerController = root.GetComponentInChildren<PlayerController>(true);
        if (playerController != null)
        {
            playerController.SetControlsLocked(locked);
        }
    }
}
