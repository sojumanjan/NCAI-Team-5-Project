using UnityEngine;

/// <summary>
/// 테트리스/팩맨이 공유하는 플레이어·카메라·사망 팝업을 다루는 공용 실행 계층.
/// MiniGameFlowManager가 "언제 어떤 상태로 전환할지" 결정하면, 이 클래스가 실제로
/// 그 공용 자원(1인칭 카메라, 조작, 관전 전환, 사망 팝업)을 켜고 끈다.
/// 각 미니게임 전용 세부 로직은 TetrisGameManager/PacmanGameManager에 위임한다.
/// </summary>
public class SharedGameplayManager : MonoBehaviour
{
    public static SharedGameplayManager Instance { get; private set; }

    [SerializeField] private GameObject deathPopupRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private PacmanGameManager pacmanGameManager;
    [SerializeField] private GameSelectUI gameSelectUI;
    [SerializeField] private Vector3 playerStartPosition;

    private CharacterController playerCharacterController;

    private void Awake()
    {
        Instance = this;
        playerCharacterController = playerController.GetComponent<CharacterController>();
        playerStartPosition = playerController.transform.position;
    }

    /// <summary>
    /// 씬 오브젝트는 항상 활성 상태를 유지하고, 이 메서드로 테트리스/팩맨 진행 여부만 켜고 끈다.
    /// (테트리스 <-> 팩맨 전환 시 SetActive 대신 사용)
    /// </summary>
    public void SetGameActive(bool isActive)
    {
        playerController.enabled = isActive;
        cameraRig.enabled = isActive;

        // 관전 모드로 들어간 채 사망/재시작 등으로 재진입하면 카메라(관전)와
        // 조작 가능 여부(1인칭 기준으로 막 켜짐)가 서로 어긋난 상태가 될 수 있으므로,
        // 게임이 (재)활성화될 때마다 항상 1인칭으로 리셋한다.
        if (isActive)
        {
            cameraRig.ResetToFirstPerson();
        }

        if (!isActive)
        {
            deathPopupRoot.SetActive(false);
        }
    }

    public void OnPlayerPinned()
    {
        playerController.SetControlsLocked(true);
        deathPopupRoot.SetActive(true);
    }

    public void ShowDeathPopupForPacman()
    {
        playerController.SetControlsLocked(true);
        deathPopupRoot.SetActive(true);
    }

    /// <summary>
    /// 사망 팝업(DeathPopup)은 테트리스/팩맨 공용이라, 재시작 버튼이 눌리면
    /// 현재 어느 게임이 진행 중이었는지에 따라 재시작 대상을 나눈다.
    /// </summary>
    public void OnClickRestart()
    {
        deathPopupRoot.SetActive(false);

        if (MiniGameFlowManager.Instance.CurrentState == MiniGameState.Pacman)
        {
            pacmanGameManager.RestartPacmanFromDeath();
            return;
        }

        tetrisGameManager.RestartFromDeath(playerController, () => RespawnPlayer());
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
        Debug.Log("[SharedGameplayManager] Exit to hub requested (not implemented yet)");
    }
}
