using UnityEngine;

/// <summary>
/// 팩맨 전용 크로스헤어(에임 포인터) 표시를 전담한다.
/// 테트리스에는 조준 요소가 없으므로, 팩맨 진입/이탈 시 MiniGameFlowManager가 호출한다.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [SerializeField] private GameObject crosshair;

    public void SetAllowed(bool isAllowed)
    {
        if (crosshair != null)
        {
            crosshair.SetActive(isAllowed);
        }
    }
}
