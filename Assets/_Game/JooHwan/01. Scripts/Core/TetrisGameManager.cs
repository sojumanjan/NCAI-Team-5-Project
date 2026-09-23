using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테트리스 전용 세부 로직(낙하 시퀀스, 클리어 판정, 관전 카메라 허용)을 담당한다.
/// 테트리스와 팩맨이 공유하는 플레이어/카메라/사망 팝업 자체는 SharedGameplayManager가 관리한다.
/// </summary>
public class TetrisGameManager : MonoBehaviour
{
    [SerializeField] private TetrisFallSequencer fallSequencer;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private int countdownStartFrom = 3;
    [SerializeField] private GameObject clearTextRoot;
    [Tooltip("사망 후 재시작 시 카운트다운 전에 먼저 보여줄 테트리스 설명 UI.")]
    [SerializeField] private PreGameDescriptionUI tetrisDescriptionUI;
    [Tooltip("사망 후 재시작 시 팩맨 재시작과 동일하게 화면을 어둡게 했다가 다시 밝히기 위한 페이드 캔버스.")]
    [SerializeField] private FadeCanvas fadeCanvas;

    [Header("Clear Feedback")]
    [SerializeField] private DeathFlashOverlay screenFlashOverlay;
    [SerializeField] private Color clearFlashColor = new Color(1f, 0.95f, 0.6f, 0.55f);
    [SerializeField] private float clearFlashDuration = 0.5f;
    [SerializeField] private float clearShakeDuration = 0.3f;
    [SerializeField] private float clearShakePositionAmplitude = 0.15f;
    [SerializeField] private float clearShakeRotationAmplitude = 3f;
    [Tooltip("CLEAR 텍스트가 작게 시작해서 커지며 나타나는 시간")]
    [SerializeField] private float clearTextPopDuration = 0.3f;
    [Tooltip("CLEAR 텍스트가 유지되는 시간 (팝업 애니메이션 이후, 페이드아웃 전)")]
    [SerializeField] private float clearTextHoldDuration = 1f;
    [Tooltip("CLEAR 텍스트가 사라질 때 페이드아웃되는 시간")]
    [SerializeField] private float clearTextFadeOutDuration = 0.3f;

    private bool isCleared;

    public bool IsCleared => isCleared;

    private void Awake()
    {
        overviewCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// 팩맨은 아직 전용 카메라가 없어, 테트리스의 1인칭 카메라/플레이어 조작을 그대로 들고 간다.
    /// 낙하 로직(IsGlobalPaused 등)만 멈추고, 카메라/조작/관전 전환 여부는 그대로 유지한다.
    /// </summary>
    public void SetTetrisGameplayPaused(bool isPaused)
    {
        FallingBlock.IsGlobalPaused = isPaused;
        fallSequencer.SetPaused(isPaused);

        // 일시정지 순간 FallingBlock.FixedUpdate가 멈춰 비네트 값 갱신도 함께 멈추므로,
        // 위험 상태에서 정지했을 때 그 값이 화면에 그대로 남지 않도록 즉시 초기화한다.
        if (isPaused && TetrisDangerVignette.Instance != null)
        {
            TetrisDangerVignette.Instance.SetDangerRatio(0f);
        }
    }

    /// <summary>
    /// 관전 카메라 전환(Tab)은 테트리스 전용이다 (팩맨에는 관전 시점이 없음).
    /// MiniGameFlowManager가 팩맨 진입/이탈 시 호출한다.
    /// </summary>
    public void SetCameraSwitchAllowed(bool isAllowed)
    {
        cameraRig.SetCameraSwitchAllowed(isAllowed);
    }

    /// <summary>
    /// MiniGameFlowManager가 페이드/카운트다운을 모두 마친 뒤 호출한다.
    /// (재시작은 RestartFromDeath, 최초 시작은 이 메서드로 나뉜다)
    /// </summary>
    public void StartSequenceForNewGame()
    {
        // 설명 UI~카운트다운 구간에 ESC로 일시정지를 열었다 닫으면, MenuEscapeBridge가
        // "카운트다운이 아직 끝나지 않았다"고 판단해 SetTetrisGameplayPaused(false) 호출을
        // 건너뛰므로 IsGlobalPaused가 true인 채로 남을 수 있다. 카운트다운이 정식으로 끝나
        // 실제 낙하를 시작하는 이 시점에 명시적으로 풀어, 그 잔여 잠금 때문에 블록이 계속
        // 멈춰있는 일이 없게 한다.
        SetTetrisGameplayPaused(false);
        fallSequencer.StartSequence();
    }

    /// <summary>
    /// EXIT 트리거(TetrisExitTrigger)가 플레이어 도달을 감지하면 호출한다.
    /// 별도 성공 UI 없이 CLEAR 텍스트만 짧게 보여준 뒤 조작을 해제한다.
    /// </summary>
    public void HandleExitReached(PlayerController playerController)
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;

        FallingBlock.IsGlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(true);

        // 사망 플래시(어두운 빨강)와 대비되는 밝은 색으로, 성공했다는 느낌을 즉시 전달한다.
        if (screenFlashOverlay != null)
        {
            screenFlashOverlay.Flash(clearFlashColor, clearFlashDuration);
        }

        CameraShake.ShakeAll(clearShakeDuration, clearShakePositionAmplitude, clearShakeRotationAmplitude);

        if (clearTextRoot != null)
        {
            StartCoroutine(ShowClearTextRoutine(playerController));
        }
    }

    private System.Collections.IEnumerator ShowClearTextRoutine(PlayerController playerController)
    {
        var rectTransform = clearTextRoot.GetComponent<RectTransform>();
        var text = clearTextRoot.GetComponent<Text>();

        rectTransform.localScale = Vector3.zero;
        if (text != null)
        {
            Color startColor = text.color;
            startColor.a = 1f;
            text.color = startColor;
        }

        clearTextRoot.SetActive(true);

        yield return rectTransform
            .DOScale(Vector3.one, clearTextPopDuration)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        yield return new WaitForSeconds(clearTextHoldDuration);

        if (text != null)
        {
            yield return text.DOFade(0f, clearTextFadeOutDuration).WaitForCompletion();
        }

        clearTextRoot.SetActive(false);

        // 클리어 후에는 문 앞까지 자유롭게 이동할 수 있어야 하므로 조작을 다시 푼다.
        playerController.SetControlsLocked(false);
    }

    /// <summary>
    /// 디버그 전용: CLEAR 연출 없이 즉시 "클리어됨" 상태로 만든다.
    /// (팩맨 진입 직전 상태부터 테스트할 때 사용)
    /// </summary>
    public void DebugForceClearedState(PlayerController playerController)
    {
        isCleared = true;
        FallingBlock.IsGlobalPaused = true;
        fallSequencer.SetPaused(true);
        playerController.SetControlsLocked(false);
    }

    /// <summary>
    /// 사망 팝업의 재시작 버튼이 눌렸을 때, 현재 상태가 테트리스면 SharedGameplayManager가 호출한다.
    /// respawnPlayer는 실제 리스폰 위치 이동(SharedGameplayManager가 담당)을 위한 콜백이다.
    /// </summary>
    public void RestartFromDeath(PlayerController playerController, System.Action respawnPlayer)
    {
        // 팩맨 재시작(MiniGameFlowManager.RestartPacman)과 동일하게, 화면이 완전히 어두워진 뒤에
        // 리스폰/리셋을 처리하고 다시 밝아지면서 설명 UI/카운트다운으로 이어지게 한다.
        fadeCanvas.FadeOut(() =>
        {
            fallSequencer.ResetSequence();
            respawnPlayer();

            FallingBlock.IsGlobalPaused = false;

            // 끼임 사망 시 비네트가 최대치 근처에서 멈춘 채 남아있을 수 있으므로, 재시작 시 명시적으로 초기화한다.
            if (TetrisDangerVignette.Instance != null)
            {
                TetrisDangerVignette.Instance.SetDangerRatio(0f);
            }

            // 카운트다운(3,2,1)이 보이는 동안에도 플레이어는 바로 움직일 수 있어야 하므로,
            // 조작 잠금은 카운트다운을 재생하기 전에 미리 풀어둔다.
            playerController.SetControlsLocked(false);

            fadeCanvas.FadeIn(() =>
            {
                // StartSequenceForNewGame과 동일한 이유로, 설명 UI~카운트다운 구간에 ESC를 열었다 닫아
                // IsGlobalPaused가 잠긴 채로 남는 경우에 대비해 카운트다운이 끝나는 시점에 명시적으로 푼다.
                System.Action startSequence = () => countdownUI.Play(countdownStartFrom, () =>
                {
                    SetTetrisGameplayPaused(false);
                    fallSequencer.StartSequence();
                });

                if (tetrisDescriptionUI != null)
                {
                    tetrisDescriptionUI.Show(startSequence);
                }
                else
                {
                    startSequence();
                }
            });
        });
    }
}
