using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 상자를 열었을 때 결과를 보여주는 패널(DeathPopup과 동일한 구조).
/// 자동으로 사라지지 않고, 패널의 "메인씬으로 돌아가기" 버튼을 눌러야 닫힌다.
/// 신규 획득/이미 클리어 여부는 RewardBoxTrigger가 판단해 어느 메서드를 부를지만 정한다.
/// </summary>
public class RewardPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private string itemGainedMessage = "아이템 획득!";
    [SerializeField] private string alreadyClearedMessage = "클리어!";

    public void ShowItemGained()
    {
        Show(itemGainedMessage);
    }

    public void ShowAlreadyCleared()
    {
        Show(alreadyClearedMessage);
    }

    private void Show(string message)
    {
        titleText.text = message;
        root.SetActive(true);
    }
}
