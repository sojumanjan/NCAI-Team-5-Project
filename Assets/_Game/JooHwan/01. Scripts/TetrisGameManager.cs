using UnityEngine;

public class TetrisGameManager : MonoBehaviour
{
    public static TetrisGameManager Instance { get; private set; }

    [SerializeField] private GameObject deathPopupRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TetrisFallSequencer fallSequencer;
    [SerializeField] private Vector3 playerStartPosition;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameSelectUI gameSelectUI;

    private CharacterController playerCharacterController;

    private void Awake()
    {
        Instance = this;
        playerCharacterController = playerController.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        deathPopupRoot.SetActive(false);
        FallingBlock.GlobalPaused = false;

        if (playerController != null)
        {
            playerStartPosition = playerController.transform.position;
        }
    }

    public void OnPlayerPinned()
    {
        FallingBlock.GlobalPaused = true;
        playerController.SetControlsLocked(true);
        deathPopupRoot.SetActive(true);
    }

    public void OnClickRestart()
    {
        deathPopupRoot.SetActive(false);

        fallSequencer.ResetSequence();
        RespawnPlayer();

        FallingBlock.GlobalPaused = false;

        // 카운트다운(3,2,1)이 보이는 동안에도 플레이어는 바로 움직일 수 있어야 하므로,
        // 조작 잠금은 카운트다운을 재생하기 전에 미리 풀어둔다.
        playerController.SetControlsLocked(false);

        countdownUI.Play(countdownStartFrom, () =>
        {
            fallSequencer.StartSequence();
        });
    }

    private void RespawnPlayer()
    {
        playerCharacterController.enabled = false;
        playerController.transform.position = playerStartPosition;
        playerCharacterController.enabled = true;
    }

    public void OnClickShowDescription()
    {
        // GameSelectUI의 설명 팝업을 재사용한다.
        gameSelectUI.OnClickDescription();
    }

    public void OnClickExitToHub()
    {
        // 허브 씬이 아직 없어 자리만 마련해둔다.
        Debug.Log("[TetrisGameManager] Exit to hub requested (not implemented yet)");
    }
}
