using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Taegeon
{
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "EscapeRoomProgress")]
public sealed class EscapeRoomProgress : MonoBehaviour
{

    [Header("액자 조각 복원 연출")]
    [SerializeField] private Camera restorationCamera;
    [SerializeField] private SoundData frameInsertSound;
    private SoundHandle frameInsertHandle = SoundHandle.None;
    [SerializeField, Min(.1f)] private float pieceInsertSeconds = .85f;
    [SerializeField, Min(0f)] private float restorationHoldSeconds = .7f;
    private bool restoringPiece;
    private Coroutine restorationRoutine;
    private Transform animatedPiece;
    private Vector3 pieceRestPosition, pieceRestScale;
    private bool previousPlayerEnabled, previousSwitcherEnabled, previousCameraEnabled;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    private System.Collections.IEnumerator RestoreFramePiece(int index)
    {
        if (restorationCamera == null) { framePieces[index].SetActive(true); yield break; }
        restoringPiece = true;
        animatedPiece = framePieces[index].transform;
        pieceRestPosition = animatedPiece.localPosition;
        pieceRestScale = animatedPiece.localScale;
        previousPlayerEnabled = player.enabled;
        previousSwitcherEnabled = switcher.enabled;
        previousCameraEnabled = player.ViewCamera.enabled;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        player.enabled = false;
        switcher.enabled = false;
        player.ViewCamera.enabled = false;
        restorationCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        prompt.text = "";
        try
        {
            yield return new WaitForSeconds(.2f);
            Vector3 finalWorld = animatedPiece.position;
            Vector3 start = finalWorld + (restorationCamera.transform.position - finalWorld).normalized * .8f + Vector3.up * .35f;
            animatedPiece.gameObject.SetActive(true);
            if (frameInsertSound != null) frameInsertHandle = AudioManager.PlayAttached(frameInsertSound, animatedPiece);
            float elapsed = 0;
            while (elapsed < pieceInsertSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, pieceInsertSeconds)));
                animatedPiece.position = Vector3.Lerp(start, finalWorld, t);
                animatedPiece.localScale = pieceRestScale * Mathf.Lerp(.75f, 1f, t);
                yield return null;
            }
            animatedPiece.localPosition = pieceRestPosition;
            animatedPiece.localScale = pieceRestScale;
            yield return new WaitForSeconds(restorationHoldSeconds);
        }
        finally { EndRestorationView(); }
        restorationRoutine = null;
    }

    private void EndRestorationView()
    {
        if (!restoringPiece) return;
        frameInsertHandle.Stop();
        if (animatedPiece != null)
        {
            animatedPiece.localPosition = pieceRestPosition;
            animatedPiece.localScale = pieceRestScale;
            animatedPiece.gameObject.SetActive(true);
        }
        if (restorationCamera != null) restorationCamera.enabled = false;
        if (player != null)
        {
            player.ViewCamera.enabled = previousCameraEnabled;
            player.enabled = previousPlayerEnabled;
        }
        if (switcher != null) switcher.enabled = previousSwitcherEnabled;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        restoringPiece = false;
        animatedPiece = null;
    }

    private void OnDisable()
    {
        keyInsertHandle.Stop();
        foreach (var sound in drawerSoundHandles) sound.Stop();
        starterBoxSoundHandle.Stop();
        if (restorationRoutine != null) StopCoroutine(restorationRoutine);
        restorationRoutine = null;
        EndRestorationView();
    }

    #region 테스트 클리어 설정

    [Header("테스트 클리어 (실행 중 체크하면 보상 생성)")]
    [SerializeField, InspectorName("1번 주크박스 클리어")]
    [Tooltip("부품 장착 여부와 관계없이 클리어 보상을 한 번 지급합니다. 체크를 해제해도 지급 기록은 초기화되지 않습니다.")]
    private bool testClearJukebox = false;
    [SerializeField] private SoundData minigameClearSound;
    [SerializeField] private SoundData keyInsertSound;
    [SerializeField] private SoundData drawerOpenSound;
    [SerializeField] private SoundData chestOpenSound;
    [SerializeField] private SoundData fireplaceOpenSound;
    private readonly bool[] drawerSoundPlayed = new bool[4];
    private readonly SoundHandle[] drawerSoundHandles = new SoundHandle[4];
    private SoundHandle keyInsertHandle = SoundHandle.None;
    [SerializeField, InspectorName("2번 룬 원판 클리어")]
    [Tooltip("부품 장착 여부와 관계없이 클리어 보상을 한 번 지급합니다.")]
    private bool testClearDial = false;
    [SerializeField, InspectorName("3번 슬라이딩 클리어")]
    [Tooltip("부품 장착 여부와 관계없이 클리어 보상을 한 번 지급합니다.")]
    private bool testClearSliding = false;
    [SerializeField, InspectorName("4번 라디오 클리어")]
    [Tooltip("부품 장착 여부와 관계없이 클리어 보상을 한 번 지급합니다.")]
    private bool testClearRadio = false;

    /// <summary>
    /// 해당 게임의 테스트용 클리어 설정을 확인합니다.
    /// </summary>
    private bool IsTestCleared(int index)
    {
        switch (index)
        {
            case 0: return testClearJukebox;
            case 1: return testClearDial;
            case 2: return testClearSliding;
            case 3: return testClearRadio;
            default: return false;
        }
    }

    #endregion

    #region 시작 상자 흔들림

    [Header("시작 부품 상자 연출")]
    [SerializeField, InspectorName("상자 흔들기")] private bool shakeStarterBox = true;
    [SerializeField] private SoundData starterBoxSound;
    private SoundHandle starterBoxSoundHandle = SoundHandle.None;
    [SerializeField, Range(0f, 15f), InspectorName("좌우 기울기")] private float starterShakeAngle = 7f;
    [SerializeField, Range(0f, .2f), InspectorName("좌우 이동 폭")] private float starterShakeDistance = .07f;
    private Vector3 starterRestPosition;
    private Quaternion starterRestRotation;
    private float starterShakeTime;

    /// <summary>열기 전에는 상자를 짧게 흔들고, 열면 원래 자세로 부드럽게 되돌립니다.</summary>
    private void UpdateStarterBoxMotion(float deltaTime)
    {
        if (!usePartProgression || starterBox == null) { starterBoxSoundHandle.Stop(); return; }
        if (starterOpened || !shakeStarterBox) starterBoxSoundHandle.Stop();
        else if (starterBoxSound != null && !starterBoxSoundHandle.IsPlaying)
            starterBoxSoundHandle = AudioManager.PlayAttached(starterBoxSound, starterBox);
        if (starterOpened || !shakeStarterBox)
        {
            starterBox.localPosition = Vector3.MoveTowards(starterBox.localPosition, starterRestPosition, deltaTime * .8f);
            starterBox.localRotation = Quaternion.RotateTowards(starterBox.localRotation, starterRestRotation, deltaTime * 70f);
            return;
        }

        // 짧은 흔들림과 쉼을 반복해 가챠 상자처럼 주의를 끕니다.
        starterShakeTime += deltaTime;
        float phase = Mathf.Repeat(starterShakeTime, 2.4f);
        float wave = phase < 1.1f
            ? Mathf.Sin(phase / 1.1f * Mathf.PI * 6f) * Mathf.Sin(phase / 1.1f * Mathf.PI)
            : 0f;
        float angle = wave * starterShakeAngle;
        // 아래쪽 모서리가 책상에 파묻히지 않도록 기울어진 만큼 살짝 들어 올립니다.
        float lift = Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad)) * .8f;
        starterBox.localPosition = starterRestPosition
            + starterRestRotation * new Vector3(wave * starterShakeDistance, lift, 0f);
        starterBox.localRotation = starterRestRotation * Quaternion.Euler(0f, 0f, angle);
    }

    #endregion

    #region 부품 수리 설정

    [System.Serializable]
    public sealed class RepairPart
    {
        public string displayName;
        public Transform machineRoot;
        public Transform socket;
        public GameObject installedVisual;
        public GameObject emptySocket;
        public GameObject pickupVisual;
    }

    [SerializeField] private bool usePartProgression;
    [SerializeField] private RepairPart[] repairParts = new RepairPart[4];
    [SerializeField] private Transform starterBox;
    [SerializeField] private Transform starterAim;
    [SerializeField] private Transform starterLid;
    [SerializeField] private Vector3[] drawerOpenOffsets;
    [SerializeField] private Vector3[] drawerOpenAngles;
    [SerializeField] private Text hintText;
    [SerializeField] private HintNoteOverlay hintNote;
    [SerializeField] private string[] hidingHints = {
        "세 봉우리 위에 떠 있는 둥근 태양. 그 그림 속 빛의 중심을 살펴보자.",
        "모든 이야기들이 모여 있는 곳의 가장 아래 칸을 살펴보자.",
        "파도를 그리는 작은 기계 곁, 나무 책상의 서랍이 비밀을 품고 있다.",
        "모든 여정의 끝에는 이것이 필요하다. 언제나 기대를 품게 되는 물건을 살펴보자."
    };
    private readonly bool[] partCarried = new bool[4];
    private readonly bool[] partInstalled = new bool[4];
    private readonly Quaternion[] closedDrawerRotations = new Quaternion[4];
    private Quaternion starterClosed;
    private bool starterOpened;

    #endregion

    #region 참조 및 설정

    [SerializeField] private ColorMemoryGame memory;
    [SerializeField] private LetterDialPuzzle dial;
    [SerializeField] private SlidingKeyPuzzle sliding;
    [SerializeField] private RadioFrequencyPuzzle radio;
    [SerializeField] private FirstPersonExplorer player;
    [SerializeField] private GameCameraSwitcher switcher;
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform[] keySpawns;
    [SerializeField] private Transform[] lockRoots;
    [SerializeField] private Transform[] lockAimPoints;
    [SerializeField] private Transform[] drawers;
    [SerializeField] private GameObject[] loosePieces;
    [SerializeField] private GameObject[] framePieces;
    [SerializeField] private Text[] lockLabels;
    [SerializeField] private Text frameCounter;
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Transform escapeBook;
    private Quaternion leftDoorClosedRotation, rightDoorClosedRotation;
    [SerializeField] private Text prompt;
    [SerializeField] private Text victoryText;
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private float aimHalfAngle = 16f;
    private readonly bool[] rewarded = new bool[4];
    private readonly bool[] carried = new bool[4];
    private readonly bool[] used = new bool[4];
    private readonly bool[] collected = new bool[4];
    private readonly GameObject[] worldKeys = new GameObject[4];
    private readonly Vector3[] closedDrawers = new Vector3[4];
    private readonly string[] names = { "주크박스", "룬 원판", "슬라이딩", "라디오" };
    private readonly Color[] colors = { new Color(.35f,.9f,.85f), new Color(1,.75f,.25f),
        new Color(.35f,.65f,1), new Color(1,.45f,.25f) };
    private Vector3 leftClosed, rightClosed;
    private float opening;
    private string notice;
    private float noticeUntil;
    private bool ending;
    [SerializeField] private GameObject resultPanel;
    private bool returningToMain;
    public int PieceCount { get { int n=0; foreach(bool b in collected) if(b)n++; return n; } }
    public bool DoorOpen => opening >= 1f;
    public bool Escaped { get; private set; }
    #endregion

    #region 게임 잠금 확인

    /// <summary>
    /// 부품 장착 여부로 해당 미니게임의 입장 가능 상태를 확인합니다.
    /// </summary>
    public bool IsGameUnlocked(int index) => index >= 0 && index < 4 && (usePartProgression ? partInstalled[index] : (index == 0 || used[index-1]));

    #endregion

    #region 초기화 및 진행 갱신

    /// <summary>
    /// 보상과 액자의 초기 상태를 준비합니다.
    /// </summary>
    private void Awake()
    {
        leftClosed = leftDoor.localPosition;
        leftDoorClosedRotation = leftDoor.localRotation;
        rightDoorClosedRotation = rightDoor.localRotation;
        rightClosed = rightDoor.localPosition;
        victoryText.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        for (int i=0;i<4;i++)
        {
            closedDrawers[i] = drawers[i].localPosition;
            closedDrawerRotations[i] = drawers[i].localRotation;
            loosePieces[i].SetActive(false);
            framePieces[i].SetActive(false);
        }
        if (usePartProgression)
        {
            starterClosed = starterLid.localRotation;
            starterRestPosition = starterBox.localPosition;
            starterRestRotation = starterBox.localRotation;
            for (int i = 0; i < 4; i++)
            {
                repairParts[i].installedVisual.SetActive(false);
                repairParts[i].emptySocket.SetActive(true);
                repairParts[i].pickupVisual.SetActive(false);
            }
            hintText.gameObject.SetActive(false);
        }
        ApplyGameLocks();
        RefreshDisplays();
    }

    /// <summary>
    /// 진행 단계에 맞춰 미니게임 조작을 허용합니다.
    /// </summary>
    private void ApplyGameLocks()
    {
        // 수리에 필요한 부품이 장착되기 전까지 조작을 막습니다.
        if (usePartProgression) memory.enabled = IsGameUnlocked(0);
        dial.enabled = IsGameUnlocked(1);
        sliding.enabled = IsGameUnlocked(2);
        radio.enabled = IsGameUnlocked(3);
    }

    /// <summary>
    /// 보상과 상호작용을 갱신하고 탈출 완료를 확인합니다.
    /// </summary>
    private void Update()
    {
        if (ending || restoringPiece) return;
        UpdateStarterBoxMotion(Time.deltaTime);
        PollRewards();
        UpdateDoor(Time.deltaTime);
        for (int i=0;i<4;i++)
        {
            Vector3 offset = usePartProgression && drawerOpenOffsets != null && drawerOpenOffsets.Length > i
                ? drawerOpenOffsets[i] : Vector3.back * .65f;
            Vector3 target=closedDrawers[i]+(used[i]?offset:Vector3.zero);
            if (used[i] && !drawerSoundPlayed[i] && Time.deltaTime > 0f)
            {
                drawerSoundPlayed[i] = true;
                SoundData openingSound = i == 0 ? fireplaceOpenSound : i == 3 ? chestOpenSound : drawerOpenSound;
                if (openingSound != null) drawerSoundHandles[i] = AudioManager.PlayAttached(openingSound, drawers[i]);
            }
            if (usePartProgression && drawerOpenAngles != null && drawerOpenAngles.Length > i)
                drawers[i].localRotation = Quaternion.RotateTowards(drawers[i].localRotation,
                    closedDrawerRotations[i] * Quaternion.Euler(used[i] ? drawerOpenAngles[i] : Vector3.zero), Time.deltaTime * 100f);
            drawers[i].localPosition=Vector3.MoveTowards(drawers[i].localPosition,target,Time.deltaTime*1.2f);
        }
        if (usePartProgression && starterOpened)
            starterLid.localRotation = Quaternion.RotateTowards(starterLid.localRotation,
                starterClosed * Quaternion.Euler(105f, 0f, 0f), Time.deltaTime * 100f);
        RefreshPrompt();
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) ToggleHints();
        if (Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
#else
        if (Input.GetKeyDown(KeyCode.H)) ToggleHints();
        if (Input.GetKeyDown(KeyCode.E)) TryInteract();
#endif
    }


    #endregion

    #region 결과 전송 및 허브 복귀

    /// <summary>
    /// 게임 결과를 한 번 전송하고 허브 복귀를 예약합니다.
    /// </summary>
    public void FinishGame(bool cleared, float score01)
    {
        if (ending) return;
        var flow = global::GameFlow.Instance;
        if (flow == null || flow.CurrentDefinition == null)
        {
            Debug.LogError("방탈출 결과를 등록할 GameFlow 또는 현재 씬의 Definition이 없습니다.", this);
            return;
        }

        if (hintNote != null) hintNote.Close();
        ending = true;
        starterBoxSoundHandle.Stop();
        Escaped = cleared;
        prompt.text = "";
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        victoryText.gameObject.SetActive(true);
        victoryText.text = cleared
            ? "탈출 성공!\n아래 버튼을 눌러 메인 허브로 돌아가세요."
            : "게임 종료\n아래 버튼을 눌러 메인 허브로 돌아가세요.";
        player.enabled = false;
        switcher.enabled = false;
        foreach (var canvas in switcher.GetComponentsInChildren<Canvas>()) canvas.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        flow.ReportCurrent(new global::MiniGameResult(cleared, Mathf.Clamp01(score01)));

    }

    /// <summary>
    /// 결과 안내 후 메인 허브로 돌아갑니다.
    /// </summary>
    public void ReturnToMainFromResult()
    {
        if (!ending || returningToMain) return;
        var flow = global::GameFlow.Instance;
        if (flow == null) return;
        returningToMain = true;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        flow.ReturnToMain();
    }

    #endregion

    #region 열쇠 보상 생성

    /// <summary>
    /// 실제 클리어 또는 테스트 설정에 따라 보상과 단서를 한 번만 지급합니다.
    /// </summary>
    private void PollRewards()
    {
        for(int i=0;i<4;i++)
        {
            bool solved=i==0?memory.IsSolved:i==1?dial.IsSolved:i==2?sliding.HasKey:radio.IsSolved;
            // 테스트 클리어는 부품 잠금을 건너뛰되 기존 보상 지급 경로를 그대로 사용합니다.
            if (rewarded[i] || (!IsTestCleared(i) && (!IsGameUnlocked(i) || !solved))) continue;
            rewarded[i]=true;
            if (minigameClearSound != null) AudioManager.PlayAttached(minigameClearSound, transform);
            worldKeys[i]=Instantiate(keyPrefab,keySpawns[i].position,keySpawns[i].rotation);
            worldKeys[i].name=(i+1)+"번 액자 열쇠";
            if (usePartProgression && i < 3) repairParts[i + 1].pickupVisual.SetActive(true);
            ColorKey(worldKeys[i],i);
            RefreshHintText();
            ShowNotice(names[i]+" 클리어!  0번으로 돌아가 보상을 바라보고 E · 노란 쪽지 [H]");
        }
    }

    /// <summary>
    /// 열쇠 보석에 게임별 식별 색상을 적용합니다.
    /// </summary>
    private void ColorKey(GameObject key,int index)
    {
        foreach(var r in key.GetComponentsInChildren<Renderer>())
        {
            if(r.name!="Colored gem")continue;
            var block=new MaterialPropertyBlock();
            block.SetColor("_BaseColor",colors[index]);block.SetColor("_Color",colors[index]);
            r.SetPropertyBlock(block);
        }
    }

    #endregion

    #region 대상 탐색 및 상호작용

    /// <summary>
    /// 시야와 거리 및 장애물을 기준으로 상호작용 가능 여부를 확인합니다.
    /// </summary>
    private bool CanSee(Vector3 point,Transform target, float maxDistance = -1f)
    {
        if(!player.ViewActive||!player.enabled||ending)return false;
        var camera=player.ViewCamera;
        Vector3 delta=point-camera.transform.position;
        if(delta.magnitude>(maxDistance > 0f ? maxDistance : interactDistance)||Vector3.Angle(camera.transform.forward,delta)>aimHalfAngle)return false;
        foreach(var hit in Physics.RaycastAll(camera.transform.position,delta.normalized,delta.magnitude,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.GetComponentInParent<FirstPersonExplorer>()==player)continue;
            if(target!=null&&(hit.transform==target||hit.transform.IsChildOf(target)))continue;
            if(hit.distance<delta.magnitude-.06f)return false;
        }
        return true;
    }

    /// <summary>
    /// 화면 중앙에 가까운 상호작용 대상을 선택합니다.
    /// </summary>
    private int FindInteraction(out int kind)
    {
        kind=-1;int selected=-1;float best=float.MaxValue;
        if (PieceCount == 4 && DoorOpen && escapeBook != null && CanSee(escapeBook.position, escapeBook))
        { kind = 5; return 0; }
        // 보유한 부품은 해당 게임의 입장 존 안에서 시선과 관계없이 장착합니다.
        if (usePartProgression && !ending && !restoringPiece && player != null && player.enabled && player.ViewActive && switcher != null)
        {
            for (int i = 0; i < repairParts.Length; i++)
            {
                if (partCarried[i] && !partInstalled[i] && switcher.IsInEntryZone(i))
                {
                    kind = 3;
                    return i;
                }
            }
        }
        for(int category=0;category<(usePartProgression?5:3);category++)
        for(int i=0;i<4;i++)
        {
            Transform target=null;Vector3 point=Vector3.zero;
            if(category==0){if(worldKeys[i]==null||carried[i]||used[i])continue;target=worldKeys[i].transform;point=target.position;}
            else if(category==1){if(!used[i]||collected[i]||!loosePieces[i].activeInHierarchy)continue;target=lockRoots[i];point=loosePieces[i].transform.position;}
            else if(category==2){if(used[i])continue;target=lockRoots[i];point=lockAimPoints[i].position;}
            else if(category==3)
            {
                // 부품 장착은 위의 입장 존 판정에서만 선택합니다.
                continue;
                
            }
            else
            {
                if(i!=0 || partCarried[0] || partInstalled[0])continue;
                target=starterBox;point=starterOpened?repairParts[0].pickupVisual.transform.position:starterAim.position;
            }
            // 높은 벽난로 액자는 바닥에서 바라보고 조작할 수 있습니다.
            float range = usePartProgression && i == 0 && (category == 1 || category == 2) ? 7f : interactDistance;
            if(!CanSee(point,target,range))continue;
            float angle=Vector3.Angle(player.ViewCamera.transform.forward,point-player.ViewCamera.transform.position);
            if(angle<best){best=angle;selected=i;kind=category;}
        }
        return selected;
    }

    /// <summary>
    /// 바라보는 열쇠나 액자 조각 또는 잠금장치와 상호작용합니다.
    /// </summary>
    public bool TryInteract()
    {
        if (restoringPiece || ending) return false;
        if (hintNote != null && hintNote.IsOpen) return false;
        int kind;int index=FindInteraction(out kind);
        if(index<0)return false;
        if (kind == 5) { FinishGame(true, 1f); return ending; }
        if(kind==0)
        {
            carried[index]=true;
            worldKeys[index].SetActive(false);
            Destroy(worldKeys[index]);worldKeys[index]=null;
            if (usePartProgression)
            {
                if (index < 3)
                {
                    partCarried[index + 1] = true;
                    repairParts[index + 1].pickupVisual.SetActive(false);
                }
                ShowNotice((index < 3 ? repairParts[index + 1].displayName + " + " : "")
                    + "액자 열쇠 획득!  [H]를 눌러 노란 쪽지를 살펴보세요.");
                RefreshHintText();
            }
            else ShowNotice((index+1)+"번 열쇠 획득! 서랍에 사용하세요.");
        }
        else if(kind==1)
        {
            collected[index]=true;loosePieces[index].SetActive(false);
            restorationRoutine = StartCoroutine(RestoreFramePiece(index));
            RefreshDisplays();
            ShowNotice(PieceCount==4?"Leap 액자 완성! 열린 옷장 안의 책을 바라보고 [E]를 누르세요.":"액자 조각 획득 · "+PieceCount+" / 4  — 탈출방 액자에 복원되었습니다.");
        }
        else if(kind==3)
        {
            if(!partCarried[index]){ShowNotice(GetRepairHint(index));return false;}
            partCarried[index]=false;partInstalled[index]=true;
            repairParts[index].installedVisual.SetActive(true);
            repairParts[index].emptySocket.SetActive(false);
            ApplyGameLocks();
            ShowNotice(repairParts[index].displayName+" 장착 완료! 주변 원 안에서 ["+(index+1)+"]로 플레이하세요.");
        }
        else if(kind==4)
        {
            if(!starterOpened)
            {
                starterOpened=true;starterBoxSoundHandle.Stop();repairParts[0].pickupVisual.SetActive(true);
                ShowNotice("상자 안에 음표가 새겨진 버튼이 있습니다. 바라보고 [E]로 집어 드세요.");
            }
            else
            {
                partCarried[0]=true;repairParts[0].pickupVisual.SetActive(false);
                ShowNotice("음표 버튼 획득. 이 모양이 들어갈 빈자리를 찾아보세요.");
            }
        }
        else
        {
            if(!carried[index]){ShowNotice(usePartProgression?"맞는 열쇠가 없습니다. 클리어 후 받은 단서를 살펴보세요. [H]":(index+1)+"번 열쇠가 필요합니다.");return false;}
            carried[index]=false;used[index]=true;
            if (keyInsertSound != null) keyInsertHandle = AudioManager.PlayAttached(keyInsertSound, lockRoots[index]);
            loosePieces[index].SetActive(true);
            // 사용한 열쇠는 소비하여 열린 서랍이나 조각과 겹치지 않게 합니다.
            ApplyGameLocks();RefreshDisplays();
            ShowNotice("숨겨진 보관함이 열렸습니다! 액자 조각을 바라보고 [E]");
        }
        RefreshHintText();
        return true;
    }

    #endregion

    #region 안내 표시 및 탈출문

    /// <summary>
    /// 액자 복원 수와 서랍 안내를 갱신합니다.
    /// </summary>
    private void RefreshDisplays()
    {
        frameCounter.text="LEAP · 액자 복원 "+PieceCount+" / 4";
        if (usePartProgression) return;
        for(int i=0;i<4;i++)lockLabels[i].text=
            used[i]?(collected[i]?"조각 획득 완료":"열림 · 액자 조각을 획득하세요"):
            (i<3?names[i+1]+" 잠금장치":"마지막 액자 서랍")+"\n"+(i+1)+"번 열쇠 필요";
    }

    /// <summary>
    /// 현재 대상에 맞는 상호작용 안내를 표시합니다.
    /// </summary>
    private void RefreshPrompt()
    {
        if (hintNote != null && hintNote.IsOpen) { prompt.text = ""; return; }
        int kind;int index=FindInteraction(out kind);
        if(index>=0)
        {
            if(kind==5)prompt.text="[E] 옷장 속 책을 펼쳐 탈출하기";
            else if(kind==0)prompt.text=usePartProgression
                ? "[E] " + (index < 3 ? repairParts[index+1].displayName + " + " : "") + "액자 열쇠 받기"
                : "[E] "+(index+1)+"번 열쇠 획득";
            else if(kind==1)prompt.text="[E] 액자 조각 획득 · "+PieceCount+" / 4";
            else if(kind==3)prompt.text=partCarried[index]?"[E] "+repairParts[index].displayName+" 끼우기":GetRepairHint(index);
            else if(kind==4)prompt.text=starterOpened?"[E] 음표 버튼 집기":"[E] 작은 상자 열기";
            else if(usePartProgression)prompt.text=carried[index]?"[E] 열쇠를 끼워 보관함 열기":"작은 열쇠구멍이 있다 · 단서 보기 [H]";
            else prompt.text=carried[index]?"[E] "+(index+1)+"번 열쇠 사용 — 서랍 열기":
                "잠김 · "+names[index]+"의 "+(index+1)+"번 열쇠가 필요합니다";
        }
        else prompt.text=Time.unscaledTime<noticeUntil?notice:"";
    }
    /// <summary>
    /// 미니게임의 빈 부품 자리에 대한 안내를 반환합니다.
    /// </summary>
    public string GetRepairHint(int index)
    {
        if (!usePartProgression || index < 0 || index >= 4) return "잠금장치를 확인하세요.";
        return partCarried[index] ? "해당 게임 앞 원 안에서 [E]로 " + repairParts[index].displayName + " 장착"
            : new[] { "음표 버튼 하나가 빠져 있다.", "안쪽 그림 원판이 빠져 있다.", "열쇠 무늬 블록 자리가 비어 있다.", "안테나가 없어 신호를 받을 수 없다." }[index];
    }

    /// <summary>
    /// 지금까지 얻은 장소 단서를 다시 보거나 닫습니다.
    /// </summary>
    private void ToggleHints()
    {
        if (!usePartProgression || ending || hintText == null) return;
        RefreshHintText();
        if (hintNote != null)
        {
            if (hintNote.IsOpen) hintNote.Close();
            else hintNote.Open();
        }
        else hintText.gameObject.SetActive(!hintText.gameObject.activeSelf);
    }

    /// <summary>
    /// 획득한 장소 단서와 액자 회수 상태를 갱신합니다.
    /// </summary>
    private void RefreshHintText()
    {
        if (!usePartProgression || hintText == null) return;
        var text = new System.Text.StringBuilder();
        bool any = false;
        for (int i = 0; i < 4; i++)
        {
            if (!rewarded[i]) continue;
            any = true;
            text.Append("\n").Append(names[i]).Append(collected[i] ? " · 액자 회수 완료" : carried[i] ? " · 열쇠 소지" : used[i] ? " · 보관함 열림" : " · 보상 수령 전");
            text.Append("\n").Append(hidingHints[i]).Append("\n");
        }
        if (!any) text.Append("\n먼저 상자를 살펴보고, 발견한 부품에 맞는 빈자리를 찾아보세요.");
        hintText.text = text.ToString();
        if (hintNote != null) hintNote.SetText(hintText.text);
    }

    /// <summary>
    /// 일정 시간 표시할 알림을 등록합니다.
    /// </summary>
    private void ShowNotice(string text){notice=text;noticeUntil=Time.unscaledTime+12f;}
    /// <summary>
    /// 액자 완성 후 탈출문을 부드럽게 엽니다.
    /// </summary>
    private void UpdateDoor(float dt)
    {
        if(PieceCount!=4||opening>=1)return;
        opening=Mathf.Min(1,opening+dt/2);
        float t=Mathf.SmoothStep(0,1,opening);
        leftDoor.localRotation=leftDoorClosedRotation * Quaternion.Euler(0, 110f*t, 0);
        rightDoor.localRotation=rightDoorClosedRotation * Quaternion.Euler(0, -110f*t, 0);
    }
    #endregion

}
}

