using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "SlidingKeyPuzzle")]
public sealed class SlidingKeyPuzzle : MonoBehaviour
{
    #region 참조 및 설정

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

    #endregion

    #region 퍼즐 초기화

    /// <summary>
    /// 블록 위치 데이터를 준비하고 퍼즐을 초기화합니다.
    /// </summary>
    private void Awake()
    {
        if (inputCamera == null) inputCamera = Camera.main;
        positions = new int[blocks.Length];
        ResetPuzzle();
    }

    /// <summary>
    /// 블록 배치와 이동 기록을 최초 상태로 되돌립니다.
    /// </summary>
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

    #endregion

    #region 격자 계산 및 블록 이동

    /// <summary>
    /// 격자 좌표를 블록의 로컬 위치로 변환합니다.
    /// </summary>
    private Vector3 BlockPosition(int i)
    {
        var b = blocks[i];
        float column = b.horizontal ? positions[i] : b.column;
        float row = b.horizontal ? b.row : positions[i];
        return new Vector3((column + (b.horizontal ? b.length - 1 : 0) * .5f - 2.5f) * cellSize,
            (2.5f - row - (b.horizontal ? 0 : b.length - 1) * .5f) * cellSize, -.24f);
    }

    /// <summary>
    /// 지정한 칸을 차지하는 다른 블록을 찾습니다.
    /// </summary>
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

    /// <summary>
    /// 충돌과 출구 조건을 확인해 가능한 만큼 블록을 이동시킵니다.
    /// </summary>
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

    /// <summary>
    /// 블록 이동을 연출하고 열쇠 블록의 탈출을 판정합니다.
    /// </summary>
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

    #endregion

    #region 입력 및 좌표 변환

    /// <summary>
    /// 초기화와 블록 드래그 입력을 처리합니다.
    /// </summary>
private void Update()
    {
        if (HintNoteOverlay.IsAnyOpen) return;
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

    /// <summary>
    /// 화면 위치를 퍼즐판의 로컬 좌표로 변환합니다.
    /// </summary>
    private bool PointerOnBoard(Vector2 screen, out Vector3 local)
    {
        var plane = new Plane(transform.forward, transform.TransformPoint(new Vector3(0, 0, -.24f)));
        Ray ray = inputCamera.ScreenPointToRay(screen);
        if (plane.Raycast(ray, out float distance)) { local = transform.InverseTransformPoint(ray.GetPoint(distance)); return true; }
        local = default; return false;
    }

    /// <summary>
    /// 마우스나 터치의 위치와 누름 상태를 읽습니다.
    /// </summary>
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
    #endregion

    #region 안내 및 상태 정리

    /// <summary>
    /// 퍼즐 진행 안내를 표시합니다.
    /// </summary>
    private void SetStatus(string text) { if (statusText != null) statusText.text = text; }
    /// <summary>
    /// 비활성화된 퍼즐을 최초 상태로 되돌립니다.
    /// </summary>
    private void OnDisable() { if (positions != null) ResetPuzzle(); }


    #endregion

    #region 블록 드래그

    /// <summary>
    /// 드래그한 거리를 계산해 블록 이동을 요청합니다.
    /// </summary>
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


    /// <summary>
    /// 클릭한 블록을 선택하거나 초기화 버튼을 처리합니다.
    /// </summary>
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
    #endregion

}
}
