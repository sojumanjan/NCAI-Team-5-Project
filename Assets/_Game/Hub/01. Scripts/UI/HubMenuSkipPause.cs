// 메인 허브에서만 일시정지 창(계속하기/옵션/메인으로)을 건너뛰고 설정 창을 바로 열고 닫게 하는 컴포넌트
using Taegeon;
using UnityEngine;

/// <summary>
/// 공용 Menu Canvas는 설정 창을 닫거나(X) ESC를 누르면 항상 일시정지 창으로 돌아간다. 미니게임에선 맞지만
/// 허브엔 이어서 할 판도, 돌아갈 메인도 없어 거쳐 갈 이유가 없다.
///
/// 공용 프리팹과 MenuEscapeToggle은 다른 미니게임도 쓰므로 고치지 않는다. 허브 씬의 Menu Canvas에만 이걸 붙여,
/// 일시정지 창이 켜진 프레임에 곧바로 뒤집는다. LateUpdate라 버튼·ESC 처리가 다 끝난 뒤, 화면을 그리기 전이다 —
/// 일시정지 창은 한 번도 그려지지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class HubMenuSkipPause : MonoBehaviour
{
    [Tooltip("같은 오브젝트의 MenuEscapeToggle. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private MenuEscapeToggle menu;

    [Tooltip("설정 창 (MenuRoot).")]
    [SerializeField] private GameObject menuRoot;

    [Tooltip("건너뛸 일시정지 창 (Pause Root).")]
    [SerializeField] private GameObject pauseRoot;

    private bool _wasMenuOpen;

    private void Awake()
    {
        if (menu == null)
        {
            menu = GetComponent<MenuEscapeToggle>();
        }
    }

    private void LateUpdate()
    {
        if (menu == null || menuRoot == null || pauseRoot == null)
        {
            return;
        }

        if (pauseRoot.activeSelf)
        {
            // 설정 창에서 X나 ESC로 넘어왔으면 그대로 닫고, 아무것도 안 열린 채 ESC로 열렸으면 설정 창으로 바꾼다.
            if (_wasMenuOpen)
            {
                menu.Resume();
            }
            else
            {
                menu.Open();
            }
        }

        _wasMenuOpen = menuRoot.activeSelf;
    }
}
