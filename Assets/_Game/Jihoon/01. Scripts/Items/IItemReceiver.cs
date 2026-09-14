/// <summary>
/// Something that can take the item out of the player's full hands (left click).
/// The mirror of <see cref="IItemSource"/>.
///
/// Without this, left click with full hands can only ever mean "drop on the floor", so
/// putting beans into the coffee machine would be impossible. Stations implement it to
/// accept ingredients; a serving counter will implement it to accept a finished dish.
/// </summary>
public interface IItemReceiver
{
    /// <summary>
    /// Would this item be accepted right now? Called before taking it, so the player can
    /// be shown a refusal instead of silently dropping the item.
    /// </summary>
    bool CanReceive(ItemData item, PlayerHands hands);

    /// <summary>
    /// Take the item. The receiver now owns the object and is responsible for destroying
    /// or storing it — the hands have already let go by this point.
    /// </summary>
    void Receive(WorldItem item, PlayerHands hands);
}
