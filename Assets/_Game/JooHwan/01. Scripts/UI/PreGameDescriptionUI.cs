using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카운트다운 시작 전에 게임 설명을 보여주고, 확인 버튼을 눌러야 다음 단계(카운트다운)로 넘어가게 한다.
/// 테트리스용/팩맨용을 각각 별도 인스턴스로 씬에 배치해서 쓴다.
/// </summary>
public class PreGameDescriptionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button confirmButton;
    [Tooltip("설명 UI가 떠 있는 동안 플레이어 조작(및 CameraSwitch/Throw 등 좌클릭 바인딩 액션)을 잠그기 위해 필요하다. " +
        "잠그지 않으면 확인 버튼을 누르는 좌클릭이 동시에 CameraSwitch 액션으로도 들어가 관전 카메라로 전환되어버린다.")]
    [SerializeField] private PlayerController playerController;

    private System.Action onConfirm;

    /// <summary>설명 UI가 떠 있는 동안(확인 대기 중)인지. MenuEscapeBridge가 ESC 메뉴를 닫을 때
    /// 조작 잠금을 임의로 풀어버리지 않도록 이 상태를 참고한다.</summary>
    public bool IsVisible => root.activeSelf;

    private void Awake()
    {
        // root(자기 자신 또는 부모)가 비활성 상태로 시작하면 Awake는 실제로 SetActive(true)가
        // 호출되는 순간에야 실행된다. 여기서 root.SetActive(false)를 또 호출하면 Show()가 막 켠
        // 것을 이 Awake가 뒤늦게 다시 꺼버리는 셈이 되므로, 최초 비활성화는 씬 데이터 자체에 맡기고
        // 여기서는 버튼 리스너만 연결한다.
        confirmButton.onClick.AddListener(HandleConfirm);
    }

    public void Show(System.Action onConfirmCallback)
    {
        onConfirm = onConfirmCallback;
        root.SetActive(true);

        // PlayerController.SetControlsLocked(true)가 커서를 보이게 하는 것은 물론,
        // CameraRig.ToggleCamera 등 "조작 잠금 중엔 좌클릭을 무시"하는 기존 가드도 함께 작동시켜,
        // 확인 버튼 클릭(좌클릭)이 CameraSwitch/Throw 같은 다른 좌클릭 액션까지 함께 발동시키는 것을 막는다.
        if (playerController != null)
        {
            playerController.SetControlsLocked(true);
        }
    }

    private void HandleConfirm()
    {
        root.SetActive(false);

        // 카운트다운 진행 중에도 플레이어는 바로 움직일 수 있어야 하므로(다른 재시작 경로와 동일하게),
        // 설명 UI를 닫는 시점에 조작 잠금을 풀어 1인칭 시점 조작이 가능하게 한다.
        if (playerController != null)
        {
            playerController.SetControlsLocked(false);
        }

        var callback = onConfirm;
        onConfirm = null;
        callback?.Invoke();
    }
}
