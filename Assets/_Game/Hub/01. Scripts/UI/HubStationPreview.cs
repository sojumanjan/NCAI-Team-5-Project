// 스테이션 안내 창 위쪽 그림을 그 스테이션의 클리어 여부에 맞는 그림으로 바꿔 끼우는 컴포넌트
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 안내 창은 스테이션을 누를 때마다 켜지므로, 켜지는 순간 한 번만 맞추면 된다.
/// 그림은 따로 들고 있지 않고 허브 오브젝트의 ClearImage/NotClearImage에서 가져온다 — 오브젝트 그림을 바꾸면 여기도 따라 바뀐다.
/// </summary>
[RequireComponent(typeof(Image))]
public class HubStationPreview : MonoBehaviour
{
    [Tooltip("이 안내 창이 가리키는 허브 오브젝트.")]
    [SerializeField] private MiniGameEntry entry;

    private void OnEnable()
    {
        if (entry == null) return;

        GameObject view = entry.IsCleared ? entry.ClearedView : entry.NotClearedView;
        Image source = view != null ? view.GetComponent<Image>() : null;
        if (source != null && source.sprite != null)
        {
            GetComponent<Image>().sprite = source.sprite;
        }
    }
}
