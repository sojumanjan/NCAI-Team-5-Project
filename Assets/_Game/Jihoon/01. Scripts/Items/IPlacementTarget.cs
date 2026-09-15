using UnityEngine;

/// <summary>
/// An <see cref="IItemReceiver"/> that can say in advance where the item will end up, so
/// the placement ghost can snap to that exact spot instead of disappearing.
///
/// Optional: receivers that do not implement it simply show no preview. Implementers must
/// return the same pose they will actually use, or the ghost becomes a lie — the easiest
/// way is to have both the preview and the real placement call one shared method.
/// </summary>
public interface IPlacementTarget
{
    /// <summary>
    /// Where <paramref name="item"/> would be placed. Returns false when this target would
    /// not take it right now.
    /// </summary>
    bool TryGetPlacement(ItemData item, out Vector3 position, out Quaternion rotation);
}
