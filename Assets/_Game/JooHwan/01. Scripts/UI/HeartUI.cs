using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽 위에 하트 3개를 표시한다. 남은 목숨만큼 빨간 하트, 잃은 만큼 검은 하트로 바뀐다.
/// 하트 스프라이트가 따로 없어 지금은 단색 원형 스프라이트(빨강/검정)로 대체한다.
/// </summary>
public class HeartUI : MonoBehaviour
{
    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    public void SetLives(int currentLives)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            bool isFull = i < currentLives;
            hearts[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
        }
    }
}
