using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class SlidingKeyPuzzle : MonoBehaviour
{
    [Serializable] public sealed class Block
    {
        public Transform visual;
        public bool horizontal;
        public int length = 2;
        public int column;
        public int row;
    }
    [SerializeField] private Block[] blocks;
    [SerializeField] private Camera inputCamera;
    [SerializeField] private Text statusText;
    [SerializeField] private Transform resetButton;
    [SerializeField] private float cellSize = 0.85f;
    [SerializeField] private float slideSeconds = 0.22f;
    [SerializeField] private UnityEvent onKeyRecovered = new UnityEvent();
    public bool HasKey { get; private set; }
    public bool IsMoving { get; private set; }
    public int MoveCount { get; private set; }
    private int[] positions;
    private int selected = -1;
    private Vector3 dragStart;
    private bool dragging;

    private void Awake()
    {
        if (inputCamera == null) inputCamera = Camera.main;
        positions = new int[blocks.Length];
        ResetPuzzle();
    }

    public void ResetPuzzle()
    {
        if (positions == null) return;
        StopAllCoroutines();
        HasKey = false; IsMoving = false; MoveCount = 0; selected = -1; dragging = false;
        for (int i = 0; i < blocks.Length; i++)
        {
            positions[i] = blocks[i].horizontal ? blocks[i].column : blocks[i].row;
            blocks[i].visual.localPosition = BlockPosition(i);
        }
        SetStatus("금색 열쇠 블록을 오른쪽 출구로 꺼내세요.");
    }

    private Vector3 BlockPosition(int i)
    {
        var b = blocks[i];
        float column = b.horizontal ? positions[i] : b.column;
        float row = b.horizontal ? b.row : positions[i];
        return new Vector3((column + (b.horizontal ? b.length - 1 : 0) * .5f - 2.5f) * cellSize,
            (2.5f - row - (b.horizontal ? 0 : b.length - 1) * .5f) * cellSize, -.24f);
    }

    private int Occupant(int column, int row, int exclude)
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            if (i == exclude) continue;
            var b = blocks[i];
            int x = b.horizontal ? positions[i] : b.column;
            int y = b.horizontal ? b.row : positions[i];
            if (column >= x && column < x + (b.horizontal ? b.length : 1) &&
                row >= y && row < y + (b.horizontal ? 1 : b.length)) return i;
        }
        return -1;
    }

    public bool TryMove(int index, int requestedSteps)
    {
        if (!isActiveAndEnabled || positions == null || HasKey || IsMoving ||
            index < 0 || index >= blocks.Length || requestedSteps == 0) return false;
        var b = blocks[index];
        int direction = Math.Sign(requestedSteps), allowed = 0;
        int distance = (int)Math.Min(Math.Abs((long)requestedSteps), 8L);
        for (int d = 1; d <= distance; d++)
        {
            int candidate = positions[index] + direction * d;
            if (candidate < 0) break;
            bool keyExit = index == 0 && b.horizontal && b.row == 2 && direction > 0;
            if (candidate + b.length > 6 && !keyExit) break;
            if (candidate > 6) break;
            int front = direction > 0 ? candidate + b.length - 1 : candidate;
            int x = b.horizontal ? front : b.column, y = b.horizontal ? b.row : front;
            if (x >= 0 && x < 6 && y >= 0 && y < 6 && Occupant(x, y, index) >= 0) break;
            allowed = direction * d;
        }
        if (allowed == 0) { SetStatus("길이 막혀 있습니다. 다른 블록을 먼저 밀어보세요."); return false; }
        StartCoroutine(Slide(index, allowed));
        return true;
    }

    private IEnumerator Slide(int index, int delta)
    {
        IsMoving = true;
        Vector3 from = blocks[index].visual.localPosition;
        positions[index] += delta;
        Vector3 to = BlockPosition(index);
        float elapsed = 0;
        while (elapsed < slideSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, slideSeconds)));
            blocks[index].visual.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
        blocks[index].visual.localPosition = to;
        MoveCount++; IsMoving = false;
        if (index == 0 && positions[0] >= 6)
        {
            HasKey = true; selected = -1; dragging = false;
            SetStatus("퍼즐 해결!  이동 시점에서 생성된 열쇠를 획득하세요.");
            onKeyRecovered.Invoke();
        }
        else SetStatus("이동 " + MoveCount + "회  |  블록을 길이 방향으로 드래그하세요.");
    }

private void Update()
    {
        if (inputCamera == null || !inputCamera.isActiveAndEnabled) { dragging = false; return; }
        ReadPointer(out Vector2 pointer, out bool down, out bool up);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) { ResetPuzzle(); return; }
#else
        if (Input.GetKeyDown(KeyCode.R)) { ResetPuzzle(); return; }
#endif
        if (down) BeginDragAt(pointer);
        if (up) EndDragAt(pointer);
    }

    private bool PointerOnBoard(Vector2 screen, out Vector3 local)
    {
        var plane = new Plane(transform.forward, transform.TransformPoint(new Vector3(0, 0, -.24f)));
        Ray ray = inputCamera.ScreenPointToRay(screen);
        if (plane.Raycast(ray, out float distance)) { local = transform.InverseTransformPoint(ray.GetPoint(distance)); return true; }
        local = default; return false;
    }

    private static void ReadPointer(out Vector2 point, out bool down, out bool up)
    {
        point = default; down = up = false;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed ||
            Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        {
            var touch = Touchscreen.current.primaryTouch;
            point = touch.position.ReadValue(); down = touch.press.wasPressedThisFrame; up = touch.press.wasReleasedThisFrame; return;
        }
        if (Mouse.current != null)
        {
            point = Mouse.current.position.ReadValue();
            down = Mouse.current.leftButton.wasPressedThisFrame; up = Mouse.current.leftButton.wasReleasedThisFrame;
        }
#else
        point = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0);
#endif
    }
    private void SetStatus(string text) { if (statusText != null) statusText.text = text; }
    private void OnDisable() { if (positions != null) ResetPuzzle(); }


public void EndDragAt(Vector2 pointer)
    {
        if (!dragging) return;
        dragging = false;
        if (PointerOnBoard(pointer, out Vector3 end))
        {
            float amount = blocks[selected].horizontal ? end.x - dragStart.x : dragStart.y - end.y;
            TryMove(selected, Mathf.RoundToInt(amount / cellSize));
        }
    }


public void BeginDragAt(Vector2 pointer)
    {
        if (inputCamera == null || !inputCamera.isActiveAndEnabled || IsMoving) return;
        dragging = false;
        if (!Physics.Raycast(inputCamera.ScreenPointToRay(pointer), out RaycastHit hit, 100f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        if (resetButton != null && (hit.transform == resetButton || hit.transform.IsChildOf(resetButton)))
        { ResetPuzzle(); return; }
        if (HasKey) return;
        for (int i = 0; i < blocks.Length; i++)
            if (hit.transform == blocks[i].visual || hit.transform.IsChildOf(blocks[i].visual))
            {
                selected = i; dragging = PointerOnBoard(pointer, out dragStart);
                SetStatus((i == 0 ? "열쇠 블록" : "블록 " + i) +
                    (blocks[i].horizontal ? " : 좌우로 드래그" : " : 위아래로 드래그"));
                return;
            }
    }
}
