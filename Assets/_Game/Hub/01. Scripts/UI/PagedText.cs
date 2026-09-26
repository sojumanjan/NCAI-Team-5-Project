using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 글을 한 장씩 띄우고 클릭으로 넘긴다. 엔딩 크레딧과 허브 인트로 툴팁이 같이 쓴다.
///
/// 한 장: 살짝 아래에서 떠오름 → 다 떠오르면 넘기기 표시(삼각형)가 까딱임 → 클릭 → 사라짐 → 다음 장.
/// 다 떠오르기 전 클릭은 받지 않는다. 떠오르는 도중에 넘기면 글을 못 읽고 지나간다.
/// </summary>
public static class PagedText
{
    /// <summary>
    /// <paramref name="pages"/>를 차례로 보여준다. 마지막 장까지 넘기면 끝난다.
    /// <paramref name="textRest"/>는 글이 최종적으로 멈춰 설 자리.
    /// </summary>
    public static IEnumerator Play(TMP_Text text, Image indicator, string[] pages, Vector2 textRest,
                                   float fadeIn, float fadeOut, float rise)
    {
        if (text == null || pages == null)
        {
            yield break;
        }

        RectTransform rect = text.rectTransform;

        foreach (string page in pages)
        {
            text.text = page;
            rect.anchoredPosition = textRest - new Vector2(0f, rise);

            yield return DOTween.Sequence()
                                .Append(text.DOFade(1f, fadeIn).SetEase(Ease.OutQuad))
                                .Join(rect.DOAnchorPos(textRest, fadeIn).SetEase(Ease.OutCubic))
                                .SetLink(text.gameObject)
                                .WaitForCompletion();

            ShowIndicator(indicator, true);
            yield return WaitForAdvance();
            ShowIndicator(indicator, false);

            yield return text.DOFade(0f, fadeOut).SetEase(Ease.InQuad).SetLink(text.gameObject).WaitForCompletion();
        }
    }

    /// <summary>글을 비우고 투명하게, 넘기기 표시는 숨긴 채로 둔다. 첫 장을 띄우기 전에 부른다.</summary>
    public static void Prepare(TMP_Text text, Image indicator, Vector2 textRest)
    {
        if (text != null)
        {
            text.text = string.Empty;
            Color color = text.color;
            color.a = 0f;
            text.color = color;
            text.rectTransform.anchoredPosition = textRest;
        }

        ShowIndicator(indicator, false);
    }

    /// <summary>
    /// 화면 아무 데나 누르면 넘어간다. 스페이스·엔터도 받는다.
    /// 옵션 창이 열려 시간이 멈춘 동안은 받지 않는다 — 옵션 창 버튼을 누른 게 글까지 넘기면 안 된다.
    /// </summary>
    public static IEnumerator WaitForAdvance()
    {
        // 앞 장을 넘긴 그 클릭이 이번 장까지 넘기지 않도록 한 프레임 쉰다.
        yield return null;

        while (true)
        {
            if (Time.timeScale > 0f && AdvancePressed())
            {
                yield break;
            }

            yield return null;
        }
    }

    public static void ShowIndicator(Image indicator, bool on)
    {
        if (indicator == null)
        {
            return;
        }

        RectTransform rect = indicator.rectTransform;
        rect.DOKill();
        indicator.DOKill();

        if (!on)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
        Color color = indicator.color;
        color.a = 0f;
        indicator.color = color;
        indicator.DOFade(1f, 0.25f).SetLink(indicator.gameObject);

        // 위아래로 까딱여야 "눌러도 된다"로 읽힌다. 가만히 있으면 장식처럼 보인다.
        Vector2 rest = rect.anchoredPosition;
        rect.DOAnchorPosY(rest.y - 10f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetLink(indicator.gameObject)
            .OnKill(() => { if (rect != null) rect.anchoredPosition = rest; });
    }

    private static bool AdvancePressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
    }
}
