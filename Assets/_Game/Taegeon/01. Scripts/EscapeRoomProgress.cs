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
    [SerializeField] private BoxCollider escapeArea;
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
    [SerializeField, Min(0f)] private float returnToHubDelay = 2f;
    public int PieceCount { get { int n=0; foreach(bool b in collected) if(b)n++; return n; } }
    public bool DoorOpen => opening >= 1f;
    public bool Escaped { get; private set; }
    #endregion

    #region 게임 잠금 확인

    /// <summary>
    /// 이전 열쇠 사용 여부로 해당 미니게임의 잠금 상태를 확인합니다.
    /// </summary>
    public bool IsGameUnlocked(int index) => index >= 0 && index < 4 && (index == 0 || used[index-1]);

    #endregion

    #region 초기화 및 진행 갱신

    /// <summary>
    /// 보상과 액자의 초기 상태를 준비합니다.
    /// </summary>
    private void Awake()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;
        victoryText.gameObject.SetActive(false);
        for (int i=0;i<4;i++)
        {
            closedDrawers[i] = drawers[i].localPosition;
            loosePieces[i].SetActive(false);
            framePieces[i].SetActive(false);
        }
        ApplyGameLocks();
        RefreshDisplays();
    }

    /// <summary>
    /// 진행 단계에 맞춰 미니게임 조작을 허용합니다.
    /// </summary>
    private void ApplyGameLocks()
    {
        // 이전 게임의 열쇠로 잠금을 해제하기 전까지 조작을 막습니다.
        dial.enabled = IsGameUnlocked(1);
        sliding.enabled = IsGameUnlocked(2);
        radio.enabled = IsGameUnlocked(3);
    }

    /// <summary>
    /// 보상과 상호작용을 갱신하고 탈출 완료를 확인합니다.
    /// </summary>
    private void Update()
    {
        if (ending) return;
        PollRewards();
        UpdateDoor(Time.deltaTime);
        for (int i=0;i<4;i++)
        {
            Vector3 target=closedDrawers[i]+(used[i]?Vector3.back*.65f:Vector3.zero);
            drawers[i].localPosition=Vector3.MoveTowards(drawers[i].localPosition,target,Time.deltaTime*1.2f);
        }
        if (DoorOpen && player.ViewActive && escapeArea.bounds.Contains(player.transform.position))
        {
            FinishGame(true, 1f);
            return;
        }
        RefreshPrompt();
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
#else
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

        ending = true;
        Escaped = cleared;
        prompt.text = "";
        victoryText.gameObject.SetActive(true);
        victoryText.text = cleared
            ? "탈출 성공!\n잠시 후 메인 허브로 돌아갑니다."
            : "게임 종료\n잠시 후 메인 허브로 돌아갑니다.";
        player.enabled = false;
        switcher.enabled = false;
        foreach (var canvas in switcher.GetComponentsInChildren<Canvas>()) canvas.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        flow.ReportCurrent(new global::MiniGameResult(cleared, Mathf.Clamp01(score01)));
        StartCoroutine(ReturnToHub());
    }

    /// <summary>
    /// 결과 안내 후 메인 허브로 돌아갑니다.
    /// </summary>
    private System.Collections.IEnumerator ReturnToHub()
    {
        yield return new WaitForSecondsRealtime(returnToHubDelay);
        var flow = global::GameFlow.Instance;
        if (flow != null) flow.ReturnToMain();
    }

    #endregion

    #region 열쇠 보상 생성

    /// <summary>
    /// 클리어한 미니게임의 열쇠를 한 번만 생성합니다.
    /// </summary>
    private void PollRewards()
    {
        for(int i=0;i<4;i++)
        {
            bool solved=i==0?memory.IsSolved:i==1?dial.IsSolved:i==2?sliding.HasKey:radio.IsSolved;
            if (!IsGameUnlocked(i)||!solved||rewarded[i])continue;
            rewarded[i]=true;
            worldKeys[i]=Instantiate(keyPrefab,keySpawns[i].position,keySpawns[i].rotation);
            worldKeys[i].name=(i+1)+"번 열쇠";
            ColorKey(worldKeys[i],i);
            ShowNotice(names[i]+" 클리어!  0번으로 돌아가 열쇠를 바라보고 E");
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
    private bool CanSee(Vector3 point,Transform target)
    {
        if(!player.ViewActive||!player.enabled||ending)return false;
        var camera=player.ViewCamera;
        Vector3 delta=point-camera.transform.position;
        if(delta.magnitude>interactDistance||Vector3.Angle(camera.transform.forward,delta)>aimHalfAngle)return false;
        foreach(var hit in Physics.RaycastAll(camera.transform.position,delta.normalized,delta.magnitude,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.GetComponentInParent<FirstPersonExplorer>()==player)continue;
            if(target!=null&&(hit.transform==target||hit.transform.IsChildOf(target)))continue;
            if(hit.distance<delta.magnitude-.06f)return false;
        }
        return true;
    }

    // 화면 중앙에 가장 가까운 대상 하나만 선택합니다.
    /// <summary>
    /// 화면 중앙에 가까운 상호작용 대상을 선택합니다.
    /// </summary>
    private int FindInteraction(out int kind)
    {
        kind=-1;int selected=-1;float best=float.MaxValue;
        for(int category=0;category<3;category++)
        for(int i=0;i<4;i++)
        {
            Transform target=null;Vector3 point=Vector3.zero;
            if(category==0){if(worldKeys[i]==null||carried[i]||used[i])continue;target=worldKeys[i].transform;point=target.position;}
            else if(category==1){if(!used[i]||collected[i]||!loosePieces[i].activeInHierarchy)continue;target=lockRoots[i];point=loosePieces[i].transform.position;}
            else {if(used[i])continue;target=lockRoots[i];point=lockAimPoints[i].position;}
            if(!CanSee(point,target))continue;
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
        int kind;int index=FindInteraction(out kind);
        if(index<0)return false;
        if(kind==0)
        {
            carried[index]=true;
            worldKeys[index].SetActive(false);
            Destroy(worldKeys[index]);worldKeys[index]=null;
            ShowNotice((index+1)+"번 열쇠 획득! "+(index<3?names[index+1]+" 옆 서랍":"탈출방 액자 아래 서랍")+"에 사용하세요.");
        }
        else if(kind==1)
        {
            collected[index]=true;loosePieces[index].SetActive(false);framePieces[index].SetActive(true);
            RefreshDisplays();
            ShowNotice(PieceCount==4?"Leap 액자 완성! 탈출방 문을 지나 나가세요.":"액자 조각 획득 · "+PieceCount+" / 4  — 탈출방 액자에 복원되었습니다.");
        }
        else
        {
            if(!carried[index]){ShowNotice((index+1)+"번 열쇠가 필요합니다. "+names[index]+"을 먼저 클리어하세요.");return false;}
            carried[index]=false;used[index]=true;
            loosePieces[index].SetActive(true);
            var key=Instantiate(keyPrefab,lockAimPoints[index].position,lockAimPoints[index].rotation);
            key.name="Used key "+(index+1);key.transform.localScale=Vector3.one*.35f;
            key.transform.SetParent(lockRoots[index],true);
            foreach(var c in key.GetComponentsInChildren<Collider>())c.enabled=false;
            ColorKey(key,index);
            ApplyGameLocks();RefreshDisplays();
            ShowNotice((index<3?names[index+1]+" 잠금 해제! ":"마지막 서랍이 열렸습니다! ")+"서랍 속 액자 조각을 바라보고 E");
        }
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
        for(int i=0;i<4;i++)lockLabels[i].text=
            used[i]?(collected[i]?"조각 획득 완료":"열림 · 액자 조각을 획득하세요"):
            (i<3?names[i+1]+" 잠금장치":"마지막 액자 서랍")+"\n"+(i+1)+"번 열쇠 필요";
    }

    /// <summary>
    /// 현재 대상에 맞는 상호작용 안내를 표시합니다.
    /// </summary>
    private void RefreshPrompt()
    {
        int kind;int index=FindInteraction(out kind);
        if(index>=0)
        {
            if(kind==0)prompt.text="[E] "+(index+1)+"번 열쇠 획득";
            else if(kind==1)prompt.text="[E] 액자 조각 획득 · "+PieceCount+" / 4";
            else prompt.text=carried[index]?"[E] "+(index+1)+"번 열쇠 사용 — 서랍 열기":
                "잠김 · "+names[index]+"의 "+(index+1)+"번 열쇠가 필요합니다";
        }
        else prompt.text=Time.unscaledTime<noticeUntil?notice:"";
    }
    /// <summary>
    /// 일정 시간 표시할 알림을 등록합니다.
    /// </summary>
    private void ShowNotice(string text){notice=text;noticeUntil=Time.unscaledTime+7f;}
    /// <summary>
    /// 액자 완성 후 탈출문을 부드럽게 엽니다.
    /// </summary>
    private void UpdateDoor(float dt)
    {
        if(PieceCount!=4||opening>=1)return;
        opening=Mathf.Min(1,opening+dt/2);
        float t=Mathf.SmoothStep(0,1,opening);
        leftDoor.localPosition=leftClosed+Vector3.left*3.1f*t;
        rightDoor.localPosition=rightClosed+Vector3.right*3.1f*t;
    }
    #endregion

}
}

