using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "LetterDialPuzzle")]
public sealed class LetterDialPuzzle : MonoBehaviour
{
    #region 참조 및 설정

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

    #endregion

    #region 초기화 및 입력

    /// <summary>
    /// 원판과 문자 배치의 초기 회전을 저장합니다.
    /// </summary>
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

    /// <summary>
    /// 클릭한 원판을 찾아 회전을 요청합니다.
    /// </summary>
    private void Update()
    {
        if (IsSolved || inputCamera == null || !inputCamera.isActiveAndEnabled || !ReadPress(out Vector2 position)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (!Physics.Raycast(inputCamera.ScreenPointToRay(position), out RaycastHit hit, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        for (int i = 0; i < 3; i++)
            if (hit.transform == rings[i] || hit.transform.IsChildOf(rings[i]))
            { RotateRing(i); return; }
    }

    #endregion

    #region 원판 회전

    /// <summary>
    /// 조작 가능한 원판을 한 칸 회전시킵니다.
    /// </summary>
    public void RotateRing(int index)
    {
        if (!isActiveAndEnabled || IsSolved || index < 0 || index >= 3 || turning[index]) return;
        StartCoroutine(Turn(index));
    }

    /// <summary>
    /// 원판을 부드럽게 회전하고 정답을 확인합니다.
    /// </summary>
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

    /// <summary>
    /// 문자의 방향을 유지하며 원판 회전을 적용합니다.
    /// </summary>
    private void SetAngle(int i, float angle)
    {
        rings[i].localRotation = initialRotations[i] * Quaternion.AngleAxis(angle, Vector3.up);
        // 문자를 반대 방향으로 회전시켜 읽는 방향을 유지합니다.
        // 각 문자 부모의 좌표계를 기준으로 역회전을 적용합니다.
        for (int j = 0; j < anchors[i].Length; j++)
            anchors[i][j].localRotation = Quaternion.AngleAxis(-angle, Vector3.up) * anchorRotations[i][j];
    }

    #endregion

    #region 정답 확인 및 초기화

    /// <summary>
    /// 원판 조합의 정답 여부와 안내 문구를 갱신합니다.
    /// </summary>
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

    /// <summary>
    /// 원판을 최초 배치로 되돌립니다.
    /// </summary>
    public void ResetPuzzle()
    {
        StopAllCoroutines();
        for (int i = 0; i < 3; i++) { steps[i] = 0; turning[i] = false; SetAngle(i, 0); }
        IsSolved = false;
        RefreshStatus();
    }

    /// <summary>
    /// 회전 연출을 중지하고 현재 칸에 정렬합니다.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        if (initialRotations == null) return;
        for (int i = 0; i < 3; i++) { turning[i] = false; SetAngle(i, steps[i] * 90f); }
    }

    #endregion

    #region 포인터 입력

    /// <summary>
    /// 마우스나 터치의 누름 위치를 읽습니다.
    /// </summary>
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
    #endregion

}
}
