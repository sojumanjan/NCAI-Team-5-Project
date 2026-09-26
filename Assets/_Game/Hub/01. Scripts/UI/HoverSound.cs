// 버튼에 마우스가 올라오는 순간 효과음을 한 번 재생하는 컴포넌트 (허브 오브젝트 호버)
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 호버 외곽선(UniformImageHoverOutline)과 같은 조건으로 소리를 낸다 — 버튼이 눌릴 수 있을 때만.
/// 외곽선은 안 뜨는데 소리만 나면 눌러도 되는 것처럼 오해하게 된다.
/// </summary>
[RequireComponent(typeof(Button))]
public class HoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("마우스가 올라오는 순간 한 번 재생할 소리.")]
    [SerializeField] private SoundData sound;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (sound != null && _button.IsInteractable())
        {
            AudioManager.Play(sound);
        }
    }
}
