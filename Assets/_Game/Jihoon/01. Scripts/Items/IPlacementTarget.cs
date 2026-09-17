using UnityEngine;

/// <summary>
/// 아이템이 어디에 놓일지 미리 알려줄 수 있는 <see cref="IItemReceiver"/>. 배치 고스트가
/// 사라지지 않고 그 자리에 딱 붙을 수 있게 해준다.
///
/// 선택 구현이다. 구현하지 않은 리시버는 그냥 미리보기가 안 뜬다. 구현할 거라면 실제로
/// 쓸 위치를 그대로 반환해야 한다 — 아니면 고스트가 거짓말을 한다. 미리보기와 실제 배치가
/// 같은 메서드를 부르게 만드는 게 가장 확실하다.
/// </summary>
public interface IPlacementTarget
{
    /// <summary>
    /// <paramref name="item"/>이 놓일 위치. 지금 받지 않을 상황이면 false.
    /// </summary>
    bool TryGetPlacement(ItemData item, out Vector3 position, out Quaternion rotation);
}
