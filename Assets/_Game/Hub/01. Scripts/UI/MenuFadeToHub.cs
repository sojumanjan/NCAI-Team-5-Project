// 일시정지 메뉴의 "메인 허브로" 버튼을 누르면 화면을 검게 덮은 뒤 허브로 보내는 컴포넌트 (공용 Menu Canvas용)
using System.Collections;
using Taegeon;
using UnityEngine;

/// <summary>
/// 허브에서 미니게임으로 들어갈 때는 화면이 덮이며 넘어가는데, 일시정지에서 나올 때는 뚝 끊겨 넘어갔다. 같은 느낌으로 맞춘다.
///
/// PauseSceneActions.ReturnToMain은 바로 씬을 바꿔서 쓰지 않는다. 그 스크립트는 고치지 않고 버튼 연결만 이쪽으로 돌린다.
/// 덮개는 프리팹 안에 꺼 둔 채 들고 있다가 켠다. 자체 캔버스 정렬 순서를 높여 두어 미니게임의 어떤 UI보다도 위에 그려진다.
/// </summary>
[DisallowMultipleComponent]
public class MenuFadeToHub : MonoBehaviour
{
    [Tooltip("화면을 덮을 검은 덮개. 꺼 둔 채로 둡니다.")]
    [SerializeField] private CanvasGroup fadeCover;

    [Tooltip("화면이 다 덮이기까지 걸리는 시간 (초). 배경음도 같은 시간 동안 사그라듭니다.")]
    [SerializeField] private float fadeSeconds = 1.2f;

    [Tooltip("덮는 동안 ESC를 막을 메뉴. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private MenuEscapeToggle menu;

    private bool _leaving;

    private void Awake()
    {
        if (menu == null) menu = GetComponent<MenuEscapeToggle>();
        if (fadeCover != null) fadeCover.gameObject.SetActive(false);
    }

    /// <summary>메인 허브로 가기 버튼의 OnClick에 연결한다.</summary>
    public void ReturnToMain()
    {
        if (_leaving) return;
        _leaving = true;
        StartCoroutine(FadeThenLeave());
    }

    private IEnumerator FadeThenLeave()
    {
        // 덮는 도중 ESC로 일시정지가 풀리면 검어지는 동안 게임이 다시 움직인다.
        if (menu != null) menu.enabled = false;

        if (fadeCover != null)
        {
            fadeCover.alpha = 0f;
            fadeCover.blocksRaycasts = true;
            fadeCover.gameObject.SetActive(true);
        }

        AudioManager.StopBGM(fadeSeconds);

        float total = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;
        while (elapsed < total)
        {
            // 일시정지로 시간이 0이라 실제 시간으로 잰다.
            elapsed += Time.unscaledDeltaTime;
            if (fadeCover != null) fadeCover.alpha = Mathf.Clamp01(elapsed / total);
            yield return null;
        }

        if (fadeCover != null) fadeCover.alpha = 1f;

        // PauseSceneActions가 넘어가기 전에 하던 정리를 그대로 한다. 안 하면 허브가 멈추거나 커서가 잠긴 채 뜬다.
        if (menu != null)
        {
            menu.enabled = true;
            menu.Resume();
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameFlow flow = GameFlow.Instance;
        if (flow != null) flow.ReturnToMain();
    }

    private void OnValidate()
    {
        fadeSeconds = Mathf.Max(0f, fadeSeconds);
    }
}
