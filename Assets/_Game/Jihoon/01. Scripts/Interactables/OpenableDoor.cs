using System;
using DG.Tweening;
using UnityEngine;

/// <summary>How a door part moves when it opens.</summary>
public enum DoorMotion
{
    /// <summary>Swings around its own pivot. Offset is a local euler delta.</summary>
    Rotate,

    /// <summary>Slides. Offset is a local position delta.</summary>
    Slide,
}

/// <summary>
/// One moving piece of a door. The closed pose is read off the transform at startup, so
/// you only author the offset — whatever the artist set in the scene stays the closed state.
/// </summary>
[Serializable]
public class DoorPart
{
    [Tooltip("인스펙터에서 알아보기 위한 이름. 동작에는 영향 없습니다.")]
    public string label = "Door";

    [Tooltip("움직일 트랜스폼.")]
    public Transform target;

    [Tooltip("회전인지 이동인지.")]
    public DoorMotion motion = DoorMotion.Rotate;

    [Tooltip("열렸을 때의 변화량. Rotate면 로컬 오일러 각도, Slide면 로컬 위치 차이입니다.")]
    public Vector3 openOffset;

    // Captured at startup rather than authored, so nudging the door in the scene
    // automatically becomes the new closed pose.
    //
    // Rotation is kept as a quaternion, not euler. localEulerAngles wraps into 0..360, so
    // a door opened to -130 reads back as 230, and closing to 0 then takes the shorter way
    // round — swinging further outward instead of retracing its path.
    [NonSerialized] public Quaternion ClosedRotation;
    [NonSerialized] public Vector3 ClosedPosition;
}

/// <summary>
/// A prop you left click to open and close — the fridge, and later the oven. Several parts
/// can move at once, each rotating or sliding by its own offset.
///
/// An <see cref="IClickTarget"/>: nothing enters or leaves the player's hands, so it is the
/// plain-click verb, and it shows a prompt like everything else.
/// </summary>
[RequireComponent(typeof(Collider))]
public class OpenableDoor : MonoBehaviour, IClickTarget
{
    [Header("문구")]
    [Tooltip("닫혀 있을 때 표시할 문구.")]
    [SerializeField] private string openPrompt = "열기";

    [Tooltip("열려 있을 때 표시할 문구.")]
    [SerializeField] private string closePrompt = "닫기";

    [Tooltip("잠겨 있을 때 표시할 문구.")]
    [SerializeField] private string lockedPrompt = "지금은 열 수 없음";

    [Header("움직이는 부분")]
    [Tooltip("문을 이루는 조각들. 하나의 클릭으로 전부 같이 움직입니다.")]
    [SerializeField] private DoorPart[] parts;

    [Header("트윈")]
    [Tooltip("여닫는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 0.45f;

    [Tooltip("열릴 때 가속 곡선.")]
    [SerializeField] private Ease openEase = Ease.OutCubic;

    [Tooltip("닫힐 때 가속 곡선.")]
    [SerializeField] private Ease closeEase = Ease.InCubic;

    [Tooltip("조각마다 순서대로 주는 시작 지연 (초). 0이면 동시에 움직입니다.")]
    [SerializeField] private float partStagger;

    /// <summary>True while the door is open or opening.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// 참이면 클릭해도 움직이지 않는다. 오븐이 조리 중에 잠근다.
    ///
    /// CanClick을 false로 만들지 않는 이유: 그러면 리졸버가 좌클릭을 '내려놓기'로 떨어뜨려,
    /// 문을 열려던 플레이어가 들고 있던 재료를 바닥에 놓아버린다.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>Fires on every toggle with the new state.</summary>
    public event Action<bool> OpenStateChanged;

    private bool _captured;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        CaptureClosedPose();
        _captured = true;
    }

    private void OnDestroy()
    {
        // Tweens outlive their target unless killed, and a half-finished tween writing to
        // a destroyed transform throws on scene unload.
        KillTweens();
    }

    private void CaptureClosedPose()
    {
        if (parts == null)
        {
            return;
        }

        foreach (DoorPart part in parts)
        {
            if (part == null || part.target == null)
            {
                continue;
            }

            part.ClosedRotation = part.target.localRotation;
            part.ClosedPosition = part.target.localPosition;
        }
    }

    // ---------------------------------------------------------------- IClickTarget

    public string ClickPrompt => Locked ? lockedPrompt : (IsOpen ? closePrompt : openPrompt);

    public bool CanClick(PlayerHands hands) => parts != null && parts.Length > 0;

    public void OnClick(PlayerHands hands)
    {
        if (Locked)
        {
            return;
        }

        Toggle();
    }

    // ---------------------------------------------------------------- opening

    /// <summary>Flips the door. Interrupting a running tween is fine — it retargets.</summary>
    public void Toggle() => SetOpen(!IsOpen);

    /// <summary>Drives the door to a state. Does nothing if already heading there.</summary>
    public void SetOpen(bool open)
    {
        IsOpen = open;
        Play(open, instant: false);
        OpenStateChanged?.Invoke(open);
    }

    private void Play(bool open, bool instant)
    {
        if (parts == null)
        {
            return;
        }

        Ease ease = open ? openEase : closeEase;

        for (int i = 0; i < parts.Length; i++)
        {
            DoorPart part = parts[i];
            if (part == null || part.target == null)
            {
                continue;
            }

            // Retarget rather than queue: clicking mid-swing should reverse smoothly.
            part.target.DOKill();

            float delay = instant ? 0f : partStagger * i;
            float time = instant ? 0f : duration;

            if (part.motion == DoorMotion.Rotate)
            {
                // Quaternion slerp, so opening and closing trace the same arc regardless
                // of how the euler angles happen to wrap. Offsets beyond 180 degrees would
                // take the short way round instead — split those into two parts.
                Quaternion to = open
                    ? part.ClosedRotation * Quaternion.Euler(part.openOffset)
                    : part.ClosedRotation;

                if (instant)
                {
                    part.target.localRotation = to;
                    continue;
                }

                part.target.DOLocalRotateQuaternion(to, time)
                    .SetEase(ease)
                    .SetDelay(delay)
                    .SetLink(part.target.gameObject);
            }
            else
            {
                Vector3 to = open ? part.ClosedPosition + part.openOffset : part.ClosedPosition;

                if (instant)
                {
                    part.target.localPosition = to;
                    continue;
                }

                part.target.DOLocalMove(to, time)
                    .SetEase(ease)
                    .SetDelay(delay)
                    .SetLink(part.target.gameObject);
            }
        }
    }

    private void KillTweens()
    {
        if (parts == null)
        {
            return;
        }

        foreach (DoorPart part in parts)
        {
            if (part != null && part.target != null)
            {
                part.target.DOKill();
            }
        }
    }

    // ---------------------------------------------------------------- editor helpers

    /// <summary>
    /// Snaps to the open pose so the offsets can be judged without entering play mode.
    /// Always run the closed preview afterwards — whatever pose is saved becomes the
    /// closed one, since the closed pose is read off the transform at startup.
    /// </summary>
    [ContextMenu("미리보기: 열린 상태")]
    private void PreviewOpen()
    {
        CaptureClosedPose();
        _captured = true;
        Play(true, instant: true);
    }

    /// <summary>Snaps back to the pose captured by the open preview.</summary>
    [ContextMenu("미리보기: 닫힌 상태")]
    private void PreviewClosed()
    {
        if (!_captured)
        {
            Debug.LogWarning("먼저 '미리보기: 열린 상태'를 실행하세요. 닫힌 자세를 아직 기억하지 못합니다.", this);
            return;
        }

        Play(false, instant: true);
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.05f, duration);
        partStagger = Mathf.Max(0f, partStagger);
    }
}
