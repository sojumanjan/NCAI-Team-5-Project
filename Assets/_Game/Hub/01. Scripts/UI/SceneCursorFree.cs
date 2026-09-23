using UnityEngine;

/// <summary>
/// 마우스로만 조작하는 씬(허브 방)에서 커서를 늘 보이게, 잠기지 않게 둔다. 씬 아무 오브젝트에나 하나만 붙인다.
///
/// 커서 잠금은 씬을 넘어가도 살아남는 전역 상태다. 1인칭 미니게임이 커서를 잠근 채 허브로 넘어오면
/// 마우스가 안 보인다. 들어올 때 한 번만 풀어서는 부족한데, 옵션 창(MenuEscapeToggle)이 닫힐 때
/// "열기 전 커서 상태"를 되살리기 때문이다 — 미니게임에서 열었던 상태가 이어지면 허브에서 닫는 순간
/// 다시 잠긴다. 그래서 이 씬에 있는 동안은 매 프레임 확인해 풀어 둔다. 허브에 커서를 잠글 일은 없다.
/// </summary>
public class SceneCursorFree : MonoBehaviour
{
    [Tooltip("이 씬에 있는 동안 계속 커서를 풀어 둡니다. 끄면 들어올 때 한 번만 풉니다.")]
    [SerializeField] private bool keepFree = true;

    [Tooltip("들어올 때 멈춰 있던 시간을 되돌립니다. 결과 화면이나 옵션 창이 시간을 0으로 둔 채 넘어오는 경우 대비.")]
    [SerializeField] private bool resumeTimeOnEnter = true;

    private void OnEnable()
    {
        Free();

        if (resumeTimeOnEnter)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>로딩 도중 다른 스크립트가 커서를 다시 잡는 경우가 있어 첫 프레임 뒤에도 한 번 더 푼다.</summary>
    private void Start() => Free();

    // LateUpdate인 이유: 같은 프레임에 다른 스크립트가 잠가도 화면이 그려지기 전에 되돌린다.
    private void LateUpdate()
    {
        if (keepFree && (Cursor.lockState != CursorLockMode.None || !Cursor.visible))
        {
            Free();
        }
    }

    private static void Free()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
