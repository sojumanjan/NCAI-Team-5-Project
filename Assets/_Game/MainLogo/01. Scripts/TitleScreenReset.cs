using UnityEngine;

/// <summary>
/// 타이틀 화면에 들어올 때 커서와 시간을 정상으로 되돌린다. 로고 씬의 아무 오브젝트에나
/// 하나만 붙이면 된다.
///
/// 커서 잠금과 timeScale은 씬을 넘어가도 살아남는 전역 상태다. 미니게임이 커서를
/// 잠근 채로, 혹은 일시정지 메뉴가 시간을 0으로 눌러둔 채로 타이틀에 도착하면
/// 마우스가 안 보이거나 버튼이 안 눌린다.
///
/// 앞 씬이 정리해주기를 기대하지 않는다. 타이틀은 마우스로만 조작하는 화면이라,
/// 누가 먼저 돌았든 여기서 스스로 되돌려야 한다.
/// </summary>
public class TitleScreenReset : MonoBehaviour
{
    [Header("되돌릴 것")]
    [Tooltip("커서 잠금을 풀고 보이게 합니다.")]
    [SerializeField] private bool freeCursor = true;

    [Tooltip("일시정지 메뉴에서 0으로 눌러둔 시간을 되돌립니다.")]
    [SerializeField] private bool resumeTime = true;

    private void OnEnable() => Apply();

    /// <summary>
    /// OnEnable만으로는 부족할 때가 있다. 로딩 도중 다른 스크립트가 커서를 다시 잡거나,
    /// 씬이 덧붙여 열리는 경우다. 첫 프레임이 지난 뒤 한 번 더 확인한다.
    /// </summary>
    private void Start() => Apply();

    private void Apply()
    {
        if (freeCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (resumeTime)
        {
            Time.timeScale = 1f;
        }
    }
}
