using UnityEngine;

/// <summary>
/// Something the player can take into their hands with left click.
///
/// Two very different things implement this and the player cannot tell them apart:
/// a <see cref="WorldItem"/> lying on a table hands over itself, while an
/// <see cref="IngredientDispenser"/> spawns a fresh copy every time. Stations will
/// later implement it too, to hand over whatever they just finished making.
/// </summary>
public interface IPickable
{
    /// <summary>What would end up in the player's hands. Used for prompts before picking.</summary>
    ItemData Item { get; }

    /// <summary>False if there is nothing to take right now (empty station, used up).</summary>
    bool CanPick(PlayerHands hands);

    /// <summary>
    /// Hand the object over. Return the GameObject that should go into the hands —
    /// either this one, or a newly spawned instance. Return null to refuse.
    /// </summary>
    GameObject Pick(PlayerHands hands);
}
