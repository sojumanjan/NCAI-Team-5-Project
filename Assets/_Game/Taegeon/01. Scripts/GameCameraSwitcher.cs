using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "GameCameraSwitcher")]
[DefaultExecutionOrder(-1000)]
public sealed class GameCameraSwitcher : MonoBehaviour
{
    #region 참조 및 설정

    [SerializeField] private Camera[] gameCameras = new Camera[4];
    [SerializeField] private GraphicRaycaster[] radioRaycasters;
    [SerializeField] private Transform jukebox;
    [SerializeField] private Text cameraLabel;
    [SerializeField] private FirstPersonExplorer explorer;
    [SerializeField] private EscapeRoomProgress progression;
    [SerializeField] private MinigameEntryZone[] entryZones;
    private int nearbyMask = -1;
    private readonly string[] names = { "주크박스", "룬 원판", "슬라이딩 퍼즐", "라디오" };
    public int CurrentIndex { get; private set; } = -1;

    #endregion

    #region 카메라 초기화 및 전환 요청

    /// <summary>
    /// 진행 관리자를 연결하고 초기 카메라를 선택합니다.
    /// </summary>
    private void Awake()
    {
        if (progression == null) progression = FindFirstObjectByType<EscapeRoomProgress>();
        if (explorer != null) ReturnToPlayer();
        else SwitchTo(0);
    }

    /// <summary>
    /// 미니게임 화면에서 1인칭 시점으로 복귀합니다.
    /// </summary>
    public void ReturnToPlayer()
    {
        if (explorer != null) SetView(-1);
    }

    /// <summary>
    /// 입장 조건을 만족한 게임의 카메라로 전환합니다.
    /// </summary>
    public void SwitchTo(int index)
    {
        if (index < 0 || index >= gameCameras.Length || gameCameras[index] == null) return;
        if (!CanEnterGame(index)) return;
        SetView(index);
    }

    #endregion

    #region 게임 접근 조건

    /// <summary>
    /// 잠금 상태와 접근 영역을 확인해 게임 입장을 판단합니다.
    /// </summary>
    public bool CanEnterGame(int index)
    {
        if (progression != null && !progression.IsGameUnlocked(index)) return false;
        return IsInEntryZone(index);
    }

    /// <summary>
    /// 플레이어가 해당 게임의 접근 영역에 있는지 확인합니다.
    /// </summary>
    private bool IsInEntryZone(int index)
    {
        if (explorer == null) return true;
        if (entryZones == null) return false;
        foreach (var zone in entryZones)
            if (zone != null && zone.GameIndex == index && zone.Contains(explorer)) return true;
        return false;
    }

    #endregion

    #region 안내 및 카메라 적용

    /// <summary>
    /// 주변 게임의 시작 키와 잠금 안내를 표시합니다.
    /// </summary>
    private void UpdateNearbyHint()
    {
        if (explorer == null || CurrentIndex != -1 || cameraLabel == null) return;
        string hint = "";
        for (int i=0;i<4;i++)
        {
            if (!IsInEntryZone(i)) continue;
            hint += CanEnterGame(i) ? "["+(i+1)+"] "+names[i]+" 시작   "
                : names[i]+" 잠김 · "+i+"번 열쇠로 옆 서랍을 여세요   ";
        }
        if (hint.Length == 0) hint = "게임 앞 원 안으로 이동하세요";
        cameraLabel.text = hint+"\nWASD 이동 · 마우스 시점 · E 상호작용 · 0 이동 시점";
    }

    /// <summary>
    /// 카메라와 오디오 및 입력 대상을 함께 전환합니다.
    /// </summary>
    private void SetView(int index)
    {
        for (int i = 0; i < gameCameras.Length; i++)
        {
            if (gameCameras[i] == null) continue;
            bool active = i == index;
            gameCameras[i].enabled = active;
            gameCameras[i].tag = active ? "MainCamera" : "Untagged";
            var listener = gameCameras[i].GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }
        if (radioRaycasters != null)
            foreach (var raycaster in radioRaycasters)
                if (raycaster != null) raycaster.enabled = index == 3;
        if (explorer != null) explorer.SetViewActive(index == -1);
        CurrentIndex = index;
        nearbyMask = -1;
        if (cameraLabel != null)
            cameraLabel.text = explorer != null
                ? "0 이동 시점으로 돌아가기 · 게임 앞 원 안에서 해당 번호로 시작\n"
                    + (index < 0 ? "WASD 이동 · 마우스 시점 · Shift 달리기 · Esc 커서 해제" : "현재: " + names[index])
                : "1 주크박스  |  2 룬 원판  |  3 슬라이딩 퍼즐  |  4 라디오\n현재: " + names[index];
    }

    #endregion

    #region 키 입력 및 오디오

    /// <summary>
    /// 숫자 키로 요청한 카메라 전환을 처리합니다.
    /// </summary>
    private void Update()
    {
        UpdateNearbyHint();
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame) ReturnToPlayer();
        else if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) SwitchTo(0);
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) SwitchTo(1);
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) SwitchTo(2);
        else if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) SwitchTo(3);
#else
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) ReturnToPlayer();
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchTo(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchTo(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchTo(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchTo(3);
#endif
    }

    /// <summary>
    /// 주크박스 화면을 벗어나면 해당 소리를 음소거합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (jukebox == null) return;
        foreach (var source in jukebox.GetComponentsInChildren<AudioSource>())
            source.mute = CurrentIndex != 0;
    }
    #endregion

}
}
