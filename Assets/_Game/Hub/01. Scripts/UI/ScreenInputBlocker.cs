using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 덮어 방의 클릭·호버를 막는 투명 막을 자기 캔버스째 만든다. 허브 연출(인트로, 클리어 복귀)이 쓴다.
///
/// 씬에 미리 두지 않고 코드로 만드는 이유: 편집하느라 다른 캔버스를 꺼둬도 막이 같이 꺼지지 않게.
/// </summary>
public static class ScreenInputBlocker
{
    /// <summary>
    /// 방 UI(0)와 진화 이펙트(10)보다 위, 옵션 창(21)보다 아래. 옵션 창까지 막으면 연출 중 ESC 메뉴 버튼을 못 누른다.
    /// </summary>
    public const int SORTING_ORDER = 20;

    /// <summary>꺼진 채로 만든다. SetActive로 켜고 끈다.</summary>
    public static GameObject Create(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)cover.transform;
        rect.SetParent(go.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 알파 0이어도 레이캐스트는 받는다. 보이지 않게 클릭만 삼킨다.
        var image = cover.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        go.SetActive(false);
        return go;
    }
}
