using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "ColorMemoryGame")]
public sealed class ColorMemoryGame : MonoBehaviour
{
    #region 참조 및 설정

    private const int TotalStages = 5;
    private const int CubeCount = 5;

    [Header("Drag the five existing scene cubes here, in order")]
    [SerializeField] private MemoryCube[] cubes = new MemoryCube[CubeCount];
    [SerializeField] private Camera inputCamera;
    [SerializeField] private Collider playButton;
    [Tooltip("Optional. If empty, one Canvas and Text are created automatically.")]
    [SerializeField] private Text stateText;

    [Header("Five fixed cube colors")]
    [SerializeField] private Color[] colors =
    {
        new Color(0.9f, 0.12f, 0.16f),
        new Color(0.12f, 0.45f, 0.95f),
        new Color(0.15f, 0.8f, 0.3f),
        new Color(1f, 0.72f, 0.05f),
        new Color(0.65f, 0.2f, 0.9f)
    };

    [Header("Difficulty: stage 1 to stage 5")]
    [Min(1)] [SerializeField] private int initialPatternLength = 3;
    [Min(1)] [SerializeField] private int addedNotesPerStage = 1;
    [Min(0.1f)] [SerializeField] private float firstNoteSeconds = 0.65f;
    [Min(0.1f)] [SerializeField] private float lastNoteSeconds = 0.33f;
    [Min(0.05f)] [SerializeField] private float firstGapSeconds = 0.25f;
    [Min(0.05f)] [SerializeField] private float lastGapSeconds = 0.13f;
    [Min(0.05f)] [SerializeField] private float pressFeedbackSeconds = 0.18f;
    [Min(0.1f)] [SerializeField] private float resultSeconds = 1.2f;

    private enum GameState { Idle, Showing, Input, Success, Failure, Complete }
    private GameState state;
    public bool IsSolved => state == GameState.Complete;
    private readonly List<int> pattern = new List<int>();
    private int stage;
    private int inputIndex;
    private bool initialized;
    private bool started;
    private GameObject generatedCanvas;

    #endregion

    #region 게임 준비 및 시작

    /// <summary>
    /// 게임을 PLAY 버튼 입력 대기 상태로 준비합니다.
    /// </summary>
    private void Start()
    {
        started = true;
        RestartGame();
    }

    /// <summary>
    /// 재활성화된 게임을 시작 대기 상태로 초기화합니다.
    /// </summary>
    private void OnEnable()
    {
        if (started) RestartGame();
    }

    // UI 버튼이나 외부 스크립트에서도 초기화를 요청할 수 있습니다.
    /// <summary>
    /// 라운드와 패턴을 초기화하고 PLAY 입력을 기다립니다.
    /// </summary>
    public void RestartGame()
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        state = GameState.Idle;
        if (!ValidateSetup()) return;
        if (!initialized)
        {
            for (int i = 0; i < CubeCount; i++) cubes[i].Initialize(colors[i]);
            if (stateText == null) CreateStateText();
            stateText.raycastTarget = false;
            initialized = true;
        }
        ResetCubes();
        stage = 1;
        pattern.Clear();
        inputIndex = 0;
        SetStatus("Press PLAY to start");
    }

    /// <summary>
    /// 대기 상태에서 첫 라운드를 시작합니다.
    /// </summary>
    public void PlayGame()
    {
        if (!isActiveAndEnabled || !initialized || state != GameState.Idle) return;
        StartCoroutine(RunGame());
    }

    #endregion

    #region 필수 참조 확인

    /// <summary>
    /// 음 버튼과 카메라의 필수 연결을 확인합니다.
    /// </summary>
    private bool ValidateSetup()
    {
        if (inputCamera == null) inputCamera = Camera.main;
        if (inputCamera == null) return SetupError("Assign Input Camera or tag a camera MainCamera.");
        if (cubes == null || cubes.Length != CubeCount || colors == null || colors.Length != CubeCount)
            return SetupError("Cubes and Colors must each contain exactly five entries.");
        var unique = new HashSet<MemoryCube>();
        foreach (MemoryCube cube in cubes)
        {
            if (cube == null || !unique.Add(cube))
                return SetupError("Assign five different MemoryCube components.");
            Renderer renderer = cube.GetComponent<Renderer>();
            Collider collider = cube.GetComponent<Collider>();
            if (!cube.isActiveAndEnabled || renderer == null || renderer.sharedMaterial == null ||
                collider == null || !collider.enabled || collider.isTrigger)
                return SetupError("Each cube needs an active MemoryCube, a material, and an enabled non-trigger collider.");
            Material material = renderer.sharedMaterial;
            if (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color"))
                return SetupError("Use a material with _BaseColor or _Color (for example URP Lit or Standard).");
        }
        return true;
    }

    /// <summary>
    /// 게임 설정 오류를 콘솔과 상태 문구에 알립니다.
    /// </summary>
    private bool SetupError(string message)
    {
        Debug.LogError("ColorMemoryGame: " + message, this);
        if (stateText != null) stateText.text = "Setup error - see Console";
        return false;
    }

    #endregion

    #region 라운드 진행

    /// <summary>
    /// 라운드를 진행하며 실패한 패턴은 그대로 재생합니다.
    /// </summary>
    private IEnumerator RunGame()
    {
        while (stage <= TotalStages)
        {
            state = GameState.Showing;
            SetStatus("Watch");
            yield return new WaitForSecondsRealtime(0.8f);

            if (pattern.Count == 0)
            {
                int length = Mathf.Max(1, initialPatternLength) + (stage - 1) * Mathf.Max(1, addedNotesPerStage);
                for (int i = 0; i < length; i++) pattern.Add(Random.Range(0, CubeCount));
            }

            float difficulty = (stage - 1f) / (TotalStages - 1f);
            float noteDuration = Mathf.Max(0.1f, Mathf.Lerp(firstNoteSeconds, lastNoteSeconds, difficulty));
            float gap = Mathf.Max(0.05f, Mathf.Lerp(firstGapSeconds, lastGapSeconds, difficulty));
            foreach (int cubeIndex in pattern)
            {
                cubes[cubeIndex].PlayFeedback(noteDuration);
                yield return new WaitForSecondsRealtime(noteDuration);
                cubes[cubeIndex].ResetFeedback();
                yield return new WaitForSecondsRealtime(gap);
            }

            inputIndex = 0;
            // 시범 재생의 마지막 클릭이 정답 입력으로 처리되지 않도록 한 프레임 기다립니다.
            yield return null;
            state = GameState.Input;
            SetStatus("Your turn");
            while (state == GameState.Input) yield return null;

            bool success = state == GameState.Success;
            SetStatus(success ? "Success" : "Failed - replay same pattern");
            yield return new WaitForSecondsRealtime(Mathf.Max(pressFeedbackSeconds, resultSeconds));
            ResetCubes();
            if (success) { stage++; pattern.Clear(); }
            // 틀린 라운드는 기존 패턴을 그대로 재생합니다.
        }
        stage = TotalStages;
        state = GameState.Complete;
        SetStatus("Success - All clear!");
    }

    #endregion

    #region 버튼 입력 및 정답 판정

    /// <summary>
    /// 주크박스 화면의 클릭 입력을 전달합니다.
    /// </summary>
    private void Update()
    {
        if (HintNoteOverlay.IsAnyOpen) return;
        if (inputCamera == null || !inputCamera.isActiveAndEnabled) return;
        if (TryGetPress(out Vector2 screenPosition)) HandlePointerPress(screenPosition);
    }

    /// <summary>
    /// 클릭한 PLAY 버튼이나 음 버튼을 처리합니다.
    /// </summary>
    public void HandlePointerPress(Vector2 screenPosition)
    {
        if (inputCamera == null || !inputCamera.isActiveAndEnabled) return;
        if (state != GameState.Idle && state != GameState.Input) return;
        Ray ray = inputCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        if (hit.collider == playButton) { PlayGame(); return; }
        if (state != GameState.Input) return;
        MemoryCube clicked = hit.collider.GetComponentInParent<MemoryCube>();
        for (int i = 0; i < CubeCount; i++)
        {
            if (cubes[i] != clicked) continue;
            PressCube(i);
            return;
        }
    }

    /// <summary>
    /// 선택한 음이 현재 패턴과 일치하는지 판정합니다.
    /// </summary>
    public void PressCube(int cubeIndex)
    {
        if (state != GameState.Input || cubeIndex < 0 || cubeIndex >= CubeCount) return;
        cubes[cubeIndex].PlayFeedback(Mathf.Max(0.05f, pressFeedbackSeconds));
        if (cubeIndex != pattern[inputIndex])
        {
            state = GameState.Failure;
            return;
        }
        inputIndex++;
        if (inputIndex == pattern.Count) state = GameState.Success;
    }

    /// <summary>
    /// 마우스나 터치의 누름 위치를 읽습니다.
    /// </summary>
    private static bool TryGetPress(out Vector2 position)
    {
        position = default;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                position = touch.position;
                return true;
            }
            return false;
        }
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif
        return false;
    }

    #endregion

    #region 상태 표시 및 정리

    /// <summary>
    /// 현재 라운드와 진행 상태를 표시합니다.
    /// </summary>
    private void SetStatus(string status)
    {
        stateText.text = $"Stage {stage} / {TotalStages}\n{status}";
    }

    /// <summary>
    /// 상태 표시가 없을 때 안내 UI를 생성합니다.
    /// </summary>
    private void CreateStateText()
    {
        generatedCanvas = new GameObject("Memory Game UI", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = generatedCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = generatedCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("State Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(generatedCanvas.transform, false);
        stateText = textObject.GetComponent<Text>();
#if UNITY_2022_2_OR_NEWER
        stateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        stateText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        stateText.fontSize = 42;
        stateText.alignment = TextAnchor.MiddleCenter;
        stateText.color = Color.white;
        stateText.raycastTarget = false;
        RectTransform rect = stateText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -30f);
        rect.sizeDelta = new Vector2(1000f, 140f);
        textObject.GetComponent<Outline>().effectColor = Color.black;
    }

    /// <summary>
    /// 모든 음 버튼을 기본 표시 상태로 되돌립니다.
    /// </summary>
    private void ResetCubes()
    {
        if (cubes == null) return;
        foreach (MemoryCube cube in cubes) if (cube != null) cube.ResetFeedback();
    }

    /// <summary>
    /// 게임 진행과 버튼 효과를 중지합니다.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        state = GameState.Idle;
        ResetCubes();
    }

    /// <summary>
    /// 실행 중 생성한 상태 UI를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (generatedCanvas != null) Destroy(generatedCanvas);
    }
    #endregion

}
}
