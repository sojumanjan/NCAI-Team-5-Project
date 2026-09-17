using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발 중 하루의 후반부를 확인하려고 두는 치트 키. 5분을 매번 다 기다리지 않게 해준다.
///
/// DayClock 안에 넣지 않고 따로 뗀 이유는, 제출용 빌드에서 이 컴포넌트만 지우면 디버그
/// 코드가 한 줄도 남지 않기 때문이다.
/// </summary>
public class DebugTimeSkipper : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("시간을 당길 시계. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private DayClock dayClock;

    [Header("치트")]
    [Tooltip("누르면 시간이 건너뛰는 키.")]
    [SerializeField] private Key skipKey = Key.F1;

    [Tooltip("한 번 누를 때 앞으로 당길 게임 내 시간 (시).")]
    [SerializeField] private float skipHours = 1f;

    [Tooltip("켜두면 에디터 밖에서는 스스로 꺼집니다. 제출 빌드에 치트가 남지 않게.")]
    [SerializeField] private bool editorOnly = true;

    private void Awake()
    {
        if (dayClock == null)
        {
            dayClock = GetComponent<DayClock>();
        }

        if (dayClock == null)
        {
            Debug.LogError($"{nameof(DebugTimeSkipper)}: {nameof(DayClock)}을 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        if (editorOnly && !Application.isEditor)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[skipKey].wasPressedThisFrame)
        {
            dayClock.SkipHours(skipHours);
            Debug.Log($"[Debug] {skipHours}시간 건너뜀 → {dayClock.TimeText}");
        }
    }
}
