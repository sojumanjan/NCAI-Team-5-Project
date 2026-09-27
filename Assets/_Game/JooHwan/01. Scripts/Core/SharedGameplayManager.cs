using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("사망 팝업의 문구. 테트리스/팩맨 중 어느 쪽에서 죽었는지에 따라 내용을 바꿔 넣는다.")]
    [SerializeField] private Text deathPopupTitleText;
    private const string TetrisDeathMessage = "블록 사이에 끼였어요!";
    private const string PacmanDeathMessage = "무당벌레를 몰아내는 데 실패했어요!";
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private TetrisGameManager tetrisGameManager;
    [SerializeField] private PacmanGameManager pacmanGameManager;
    [SerializeField] private Vector3 playerStartPosition;

    [Header("사운드")]
    [Tooltip("피격 순간의 물리적 타격음. 팩맨 일반 피격은 이것만, 테트리스/팩맨 사망 시에는 이 다음에 gameOverSound가 이어진다.")]
    [SerializeField] private SoundData hitSound;
    [Tooltip("사망(테트리스 끼임 / 팩맨 목숨 소진)이 확정된 순간 hitSound 바로 뒤에 이어서 재생되는 별도 신호음.")]
    [SerializeField] private SoundData gameOverSound;
    [Tooltip("hitSound 재생 후 gameOverSound가 이어지기까지의 간격 (초).")]
    [SerializeField] private float gameOverSoundDelay = 0.2f;

    [Header("허브로 돌아가기")]
    [Tooltip("허브로 넘어가기 전에 화면을 덮을 페이드. 비워두면 덮지 않고 바로 넘어갑니다.")]
    [SerializeField] private FadeCanvas exitFade;
    [Tooltip("화면이 다 덮이기까지 걸리는 시간 (초). 배경음도 같은 시간 동안 사그라듭니다. 다른 미니게임과 같은 1.2초.")]
    [SerializeField] private float exitFadeSeconds = 1.2f;

    private CharacterController playerCharacterController;
    private bool isExitingToHub;

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

    [Tooltip("사망 판정 후 재시작 팝업이 뜨기까지의 지연 시간. 죽었다는 사실을 인지할 틈도 없이 팝업이 바로 뜨는 것을 방지한다.")]
    [SerializeField] private float deathPopupDelay = 0.6f;

    [Header("Death Feedback")]
    [Tooltip("사망 시 카메라 흔들림 지속 시간. deathPopupDelay와 비슷하게 맞춰 팝업이 뜰 때까지 흔들리게 한다.")]
    [SerializeField] private float deathShakeDuration = 0.6f;
    [SerializeField] private float deathShakePositionAmplitude = 0.3f;
    [SerializeField] private float deathShakeRotationAmplitude = 6f;
    [SerializeField] private DeathFlashOverlay deathFlashOverlay;

    public void OnPlayerPinned()
    {
        if (deathPopupTitleText != null)
        {
            deathPopupTitleText.text = TetrisDeathMessage;
        }

        playerController.SetControlsLocked(true);
        CameraShake.ShakeAll(deathShakeDuration, deathShakePositionAmplitude, deathShakeRotationAmplitude);
        deathFlashOverlay.Flash();
        StartCoroutine(PlayDeathSoundSequence());
        StartCoroutine(ShowDeathPopupAfterDelay());
    }

    public void ShowDeathPopupForPacman()
    {
        if (deathPopupTitleText != null)
        {
            deathPopupTitleText.text = PacmanDeathMessage;
        }

        // 목숨이 소진된 순간 고스트도 함께 멈춘다. 안 그러면 팝업이 뜨는 지연시간(deathPopupDelay) 동안
        // 계속 쫓아오며 공격 판정을 내고(피격음/게임오버음이 반복 재생됨), 이동음(크랙클)도 계속 들린다.
        // SetGhostsPaused는 위치는 그대로 둔 채 이동/발광/사운드만 멈추므로(SetActive(false) 경유) 이 용도에 맞다.
        MiniGameFlowManager.Instance.SetGhostsPaused(true);

        playerController.SetControlsLocked(true);
        CameraShake.ShakeAll(deathShakeDuration, deathShakePositionAmplitude, deathShakeRotationAmplitude);
        deathFlashOverlay.Flash();
        StartCoroutine(PlayDeathSoundSequence());
        StartCoroutine(ShowDeathPopupAfterDelay());
    }

    /// <summary>피격 타격음이 먼저 나고, 짧은 간격 뒤에 게임오버 신호음이 이어지도록 재생한다.</summary>
    private System.Collections.IEnumerator PlayDeathSoundSequence()
    {
        AudioManager.Play(hitSound);

        // 고정된 간격만 기다리면 히트 사운드 자체 길이보다 짧을 경우 겹쳐 들린다.
        // 히트 사운드가 실제로 끝날 때까지 기다린 뒤, 추가 여유(gameOverSoundDelay)만큼 더 기다린다.
        float hitClipLength = hitSound != null ? (hitSound.PickClip() != null ? hitSound.PickClip().length : 0f) : 0f;
        yield return new WaitForSeconds(hitClipLength + gameOverSoundDelay);

        AudioManager.Play(gameOverSound);
    }

    /// <summary>
    /// 목숨이 남아있는 일반 피격(고스트 접촉) 시 PacmanGameManager가 호출한다.
    /// 사망 팝업/플래시 없이, 사망과 동일한 강도의 흔들림만 재생한다.
    /// </summary>
    public void PlayHitFeedback()
    {
        CameraShake.ShakeAll(deathShakeDuration, deathShakePositionAmplitude, deathShakeRotationAmplitude);
        AudioManager.Play(hitSound);
    }

    private System.Collections.IEnumerator ShowDeathPopupAfterDelay()
    {
        yield return new WaitForSeconds(deathPopupDelay);
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

    /// <summary>
    /// 사망 팝업/보상(클리어) 팝업 양쪽의 "메인씬으로" 버튼이 공통으로 부른다.
    /// 사망 팝업이 떠 있는 상태로 호출됐다면 실패로, 그렇지 않다면(보상 팝업 경로) 클리어로 보고한다.
    /// </summary>
    public void OnClickExitToHub()
    {
        // 덮이는 동안 버튼을 또 누르면 결과가 두 번 보고되고 씬도 두 번 넘어가려 한다.
        if (isExitingToHub) return;
        isExitingToHub = true;

        bool cleared = !deathPopupRoot.activeSelf;
        GameFlow.Instance.ReportCurrent(new MiniGameResult(cleared, cleared ? 1f : 0f));

        // 허브는 검은 화면에서 밝아지며 시작하므로, 이쪽이 검게 덮은 채 넘겨야 화면이 끊기지 않는다.
        if (exitFade == null)
        {
            GameFlow.Instance.ReturnToMain();
            return;
        }

        AudioManager.StopBGM(exitFadeSeconds);
        exitFade.FadeOut(exitFadeSeconds, () => GameFlow.Instance.ReturnToMain());
    }
}
