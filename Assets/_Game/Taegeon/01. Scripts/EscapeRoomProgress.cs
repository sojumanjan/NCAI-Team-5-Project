using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class EscapeRoomProgress : MonoBehaviour
{
    [SerializeField] private ColorMemoryGame memory;
    [SerializeField] private LetterDialPuzzle dial;
    [SerializeField] private SlidingKeyPuzzle sliding;
    [SerializeField] private RadioFrequencyPuzzle radio;
    [SerializeField] private FirstPersonExplorer player;
    [SerializeField] private GameCameraSwitcher switcher;
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform[] keySpawns;
    [SerializeField] private Transform[] sockets;
    [SerializeField] private Transform receiverAim;
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private BoxCollider escapeArea;
    [SerializeField] private Text prompt;
    [SerializeField] private Text victoryText;
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private float aimHalfAngle = 16f;
    private readonly bool[] rewarded = new bool[4];
    private readonly bool[] carried = new bool[4];
    private readonly bool[] inserted = new bool[4];
    private readonly GameObject[] worldKeys = new GameObject[4];
    private readonly string[] keyNames = { "선율", "룬", "슬라이딩", "신호" };
    private readonly Color[] colors = {
        new Color(.35f,.9f,.85f), new Color(1f,.75f,.25f),
        new Color(.35f,.65f,1f), new Color(1f,.45f,.25f) };
    private Vector3 leftClosed, rightClosed;
    private float opening;
    private string notice;
    private float noticeUntil;
    public int InsertedCount { get { int n = 0; foreach (bool b in inserted) if (b) n++; return n; } }
    public bool DoorOpen => opening >= 1f;
    public bool Escaped { get; private set; }

    private void Awake()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;
        victoryText.gameObject.SetActive(false);
    }

    private void Update()
    {
        PollRewards();
        UpdateDoor(Time.deltaTime);
        if (Escaped) return;
        if (DoorOpen && escapeArea.bounds.Contains(player.transform.position))
        {
            Escaped = true;
            prompt.text = "";
            victoryText.gameObject.SetActive(true);
            victoryText.text = "탈출 성공!\n네 개의 열쇠로 마지막 문을 열었습니다.";
            player.enabled = false;
            switcher.enabled = false;
            foreach (var canvas in switcher.GetComponentsInChildren<Canvas>()) canvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        RefreshPrompt();
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        pressed = Input.GetKeyDown(KeyCode.E);
#endif
        if (pressed) TryInteract();
    }

    private void PollRewards()
    {
        bool[] solved = { memory.IsSolved, dial.IsSolved, sliding.HasKey, radio.IsSolved };
        for (int i = 0; i < 4; i++)
        {
            if (!solved[i] || rewarded[i]) continue;
            rewarded[i] = true;
            worldKeys[i] = Instantiate(keyPrefab, keySpawns[i].position, keySpawns[i].rotation);
            worldKeys[i].name = keyNames[i] + " 열쇠";
            ColorKey(worldKeys[i], i);
            ShowNotice((i + 1) + "번 열쇠가 생성되었습니다!  0번으로 돌아가 열쇠를 바라보고 E");
        }
    }

    private void ColorKey(GameObject key, int index)
    {
        foreach (var renderer in key.GetComponentsInChildren<Renderer>())
        {
            if (renderer.name != "Colored gem") continue;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", colors[index]);
            block.SetColor("_Color", colors[index]);
            renderer.SetPropertyBlock(block);
        }
    }

    private bool CanSee(Vector3 point, Transform target)
    {
        if (!player.ViewActive || !player.enabled || Escaped) return false;
        var camera = player.ViewCamera;
        Vector3 delta = point - camera.transform.position;
        if (delta.magnitude > interactDistance || Vector3.Angle(camera.transform.forward, delta) > aimHalfAngle) return false;
        foreach (var hit in Physics.RaycastAll(camera.transform.position, delta.normalized, delta.magnitude,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<FirstPersonExplorer>() == player) continue;
            if (target != null && (hit.transform == target || hit.transform.IsChildOf(target))) continue;
            if (hit.distance < delta.magnitude - .06f) return false;
        }
        return true;
    }

    private int AimedKey()
    {
        int selected = -1;
        float best = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            if (worldKeys[i] == null || carried[i] || inserted[i]) continue;
            if (!CanSee(worldKeys[i].transform.position, worldKeys[i].transform)) continue;
            float angle = Vector3.Angle(player.ViewCamera.transform.forward,
                worldKeys[i].transform.position - player.ViewCamera.transform.position);
            if (angle < best) { best = angle; selected = i; }
        }
        return selected;
    }

    public bool TryInteract()
    {
        if (Escaped || !player.ViewActive || !player.enabled) return false;
        int key = AimedKey();
        if (key >= 0)
        {
            carried[key] = true;
            Destroy(worldKeys[key]);
            worldKeys[key] = null;
            ShowNotice(keyNames[key] + " 열쇠 획득!  탈출방 받침대에 꽂으세요.");
            return true;
        }
        if (!CanSee(receiverAim.position, null)) return false;
        for (int i = 0; i < 4; i++)
        {
            if (!carried[i] || inserted[i]) continue;
            carried[i] = false;
            inserted[i] = true;
            var visual = Instantiate(keyPrefab, sockets[i].position + Vector3.up * .43f, sockets[i].rotation);
            visual.name = "Inserted key " + (i + 1);
            visual.transform.SetParent(sockets[i], true);
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
            ColorKey(visual, i);
            ShowNotice(InsertedCount == 4 ? "모든 열쇠를 꽂았습니다! 열린 문으로 나가세요." : "열쇠 장착 " + InsertedCount + " / 4");
            return true;
        }
        ShowNotice(InsertedCount == 4 ? "열린 문으로 나가세요." : "먼저 미니게임 열쇠를 획득하세요.");
        return false;
    }

    private void RefreshPrompt()
    {
        int key = AimedKey();
        if (key >= 0) { prompt.text = "[E] " + keyNames[key] + " 열쇠 획득"; return; }
        if (CanSee(receiverAim.position, null))
        {
            bool hasKey = false;
            foreach (bool b in carried) hasKey |= b;
            prompt.text = "장착 " + InsertedCount + " / 4  ·  " +
                (InsertedCount == 4 ? "열린 문으로 나가세요" : hasKey ? "[E] 열쇠 꽂기" : "획득한 열쇠가 필요합니다");
            return;
        }
        prompt.text = Time.unscaledTime < noticeUntil ? notice : "";
    }

    private void ShowNotice(string message) { notice = message; noticeUntil = Time.unscaledTime + 6f; }

    private void UpdateDoor(float dt)
    {
        if (InsertedCount != 4 || opening >= 1f) return;
        opening = Mathf.Min(1f, opening + dt / 2f);
        float t = Mathf.SmoothStep(0, 1, opening);
        leftDoor.localPosition = leftClosed + Vector3.left * 3.1f * t;
        rightDoor.localPosition = rightClosed + Vector3.right * 3.1f * t;
    }
}
