using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-1000)]
public sealed class GameCameraSwitcher : MonoBehaviour
{
    [SerializeField] private Camera[] gameCameras = new Camera[4];
    [SerializeField] private GraphicRaycaster[] radioRaycasters;
    [SerializeField] private Transform jukebox;
    [SerializeField] private Text cameraLabel;
    private readonly string[] names = { "주크박스", "룬 원판", "슬라이딩 퍼즐", "라디오" };
    public int CurrentIndex { get; private set; } = -1;

    private void Awake() => SwitchTo(0);

    public void SwitchTo(int index)
    {
        if (index < 0 || index >= gameCameras.Length || gameCameras[index] == null) return;
        for (int i = 0; i < gameCameras.Length; i++)
        {
            if (gameCameras[i] == null) continue;
            bool active = i == index;
            gameCameras[i].enabled = active;
            gameCameras[i].tag = active ? "MainCamera" : "Untagged";
            var listener = gameCameras[i].GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }
        foreach (var raycaster in radioRaycasters)
            if (raycaster != null) raycaster.enabled = index == 3;
        CurrentIndex = index;
        if (cameraLabel != null)
            cameraLabel.text = "1 주크박스   |   2 룬 원판   |   3 슬라이딩 퍼즐   |   4 라디오\n현재: " + names[index];
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) SwitchTo(0);
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) SwitchTo(1);
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) SwitchTo(2);
        else if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) SwitchTo(3);
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchTo(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchTo(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchTo(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchTo(3);
#endif
    }

    private void LateUpdate()
    {
        if (jukebox == null) return;
        foreach (var source in jukebox.GetComponentsInChildren<AudioSource>())
            source.mute = CurrentIndex != 0;
    }
}