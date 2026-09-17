using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class ColorMemoryGame : MonoBehaviour
{
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

    private void Start()
    {
        started = true;
        RestartGame();
    }

    private void OnEnable()
    {
        if (started) RestartGame();
    }

    // Can also be wired to a UI Button or invoked by another script.
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

    public void PlayGame()
    {
        if (!isActiveAndEnabled || !initialized || state != GameState.Idle) return;
        StartCoroutine(RunGame());
    }

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

    private bool SetupError(string message)
    {
        Debug.LogError("ColorMemoryGame: " + message, this);
        if (stateText != null) stateText.text = "Setup error - see Console";
        return false;
    }

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
            // Clear any click from the demonstration's final frame before accepting input.
            yield return null;
            state = GameState.Input;
            SetStatus("Your turn");
            while (state == GameState.Input) yield return null;

            bool success = state == GameState.Success;
            SetStatus(success ? "Success" : "Failed - replay same pattern");
            yield return new WaitForSecondsRealtime(Mathf.Max(pressFeedbackSeconds, resultSeconds));
            ResetCubes();
            if (success) { stage++; pattern.Clear(); }
            // A failed round replays the same pattern.
        }
        stage = TotalStages;
        state = GameState.Complete;
        SetStatus("Success - All clear!");
    }

    private void Update()
    {
        if (inputCamera == null || !inputCamera.isActiveAndEnabled) return;
        if (TryGetPress(out Vector2 screenPosition)) HandlePointerPress(screenPosition);
    }

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

    private void SetStatus(string status)
    {
        stateText.text = $"Stage {stage} / {TotalStages}\n{status}";
    }

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

    private void ResetCubes()
    {
        if (cubes == null) return;
        foreach (MemoryCube cube in cubes) if (cube != null) cube.ResetFeedback();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        state = GameState.Idle;
        ResetCubes();
    }

    private void OnDestroy()
    {
        if (generatedCanvas != null) Destroy(generatedCanvas);
    }
}

