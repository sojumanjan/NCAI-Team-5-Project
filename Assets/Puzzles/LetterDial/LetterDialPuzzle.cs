using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class LetterDialPuzzle : MonoBehaviour
{
    [SerializeField] private Transform[] rings = new Transform[3];
    [SerializeField] private Text statusText;
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float turnSeconds = 0.25f;
    private readonly int[] steps = new int[3];
    private readonly int[] answer = { 2, 1, 1 };
    private readonly bool[] turning = new bool[3];
    private Quaternion[] initialRotations;
    private Quaternion[][] anchorRotations;
    private Transform[][] anchors;
    public bool IsSolved { get; private set; }

    private void Awake()
    {
        initialRotations = new Quaternion[3];
        anchors = new Transform[3][];
        anchorRotations = new Quaternion[3][];
        for (int i = 0; i < 3; i++)
        {
            if (rings[i] == null) { enabled = false; return; }
            initialRotations[i] = rings[i].localRotation;
            Transform parent = rings[i].Find("Letter Anchors");
            anchors[i] = new Transform[parent.childCount];
            anchorRotations[i] = new Quaternion[parent.childCount];
            for (int j = 0; j < parent.childCount; j++)
            {
                anchors[i][j] = parent.GetChild(j);
                anchorRotations[i][j] = anchors[i][j].localRotation;
            }
        }
        if (inputCamera == null) inputCamera = Camera.main;
        RefreshStatus();
    }

    private void Update()
    {
        if (IsSolved || inputCamera == null || !ReadPress(out Vector2 position)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (!Physics.Raycast(inputCamera.ScreenPointToRay(position), out RaycastHit hit, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        for (int i = 0; i < 3; i++)
            if (hit.transform == rings[i] || hit.transform.IsChildOf(rings[i]))
            { RotateRing(i); return; }
    }

    public void RotateRing(int index)
    {
        if (!isActiveAndEnabled || IsSolved || index < 0 || index >= 3 || turning[index]) return;
        StartCoroutine(Turn(index));
    }

    private IEnumerator Turn(int i)
    {
        turning[i] = true;
        int next = steps[i] + 1;
        float from = steps[i] * 90f, to = next * 90f, elapsed = 0f;
        while (elapsed < turnSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, turnSeconds)));
            SetAngle(i, Mathf.Lerp(from, to, t));
            yield return null;
        }
        steps[i] = next % 4;
        SetAngle(i, steps[i] * 90f);
        turning[i] = false;
        RefreshStatus();
    }

    private void SetAngle(int i, float angle)
    {
        rings[i].localRotation = initialRotations[i] * Quaternion.AngleAxis(angle, Vector3.up);
        // Counter-rotate the letter anchors, preserving readable upright symbols.
        // Apply the inverse turn in each anchor's parent space.
        for (int j = 0; j < anchors[i].Length; j++)
            anchors[i][j].localRotation = Quaternion.AngleAxis(-angle, Vector3.up) * anchorRotations[i][j];
    }

    private void RefreshStatus()
    {
        if (turning[0] || turning[1] || turning[2]) return;
        IsSolved = steps[0] == answer[0] && steps[1] == answer[1] && steps[2] == answer[2];
        if (statusText != null)
        {
            statusText.text = IsSolved ? "정답입니다!  퍼즐을 해결했습니다." : "원판을 클릭하면 시계 방향으로 한 칸 회전합니다.";
            statusText.color = IsSolved ? new Color(.65f, 1f, .68f) : new Color(.85f, .85f, .72f);
        }
    }

    public void ResetPuzzle()
    {
        StopAllCoroutines();
        for (int i = 0; i < 3; i++) { steps[i] = 0; turning[i] = false; SetAngle(i, 0); }
        IsSolved = false;
        RefreshStatus();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (initialRotations == null) return;
        for (int i = 0; i < 3; i++) { turning[i] = false; SetAngle(i, steps[i] * 90f); }
    }

    private static bool ReadPress(out Vector2 position)
    {
        position = default;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        { position = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { position = Mouse.current.position.ReadValue(); return true; }
#else
        if (Input.GetMouseButtonDown(0)) { position = Input.mousePosition; return true; }
#endif
        return false;
    }
}
