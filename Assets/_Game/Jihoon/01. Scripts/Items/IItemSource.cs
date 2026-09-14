using UnityEngine;

/// <summary>
/// Something that can hand an item to the player's empty hands (left click).
///
/// The name is deliberately about *providing*, not about being picked up: most
/// implementers are never carried themselves. A <see cref="WorldItem"/> lying on a table
/// happens to hand over itself, an <see cref="IngredientDispenser"/> spawns a fresh copy,
/// and a <see cref="StationBase"/> hands over the dish it just finished. The player's
/// hands cannot tell the three apart.
///
/// Paired with <see cref="IItemReceiver"/>, which is the same thing in reverse.
/// </summary>
public interface IItemSource
{
    /// <summary>What would end up in the hands. Used for prompts before taking anything.</summary>
    ItemData ProvidedItem { get; }

    /// <summary>False when there is nothing to hand over right now.</summary>
    bool CanProvide(PlayerHands hands);

    /// <summary>
    /// Hand the item over. Return the GameObject that should end up in the hands —
    /// this one, or a newly spawned instance. Return null to refuse.
    /// </summary>
    GameObject Provide(PlayerHands hands);
}
