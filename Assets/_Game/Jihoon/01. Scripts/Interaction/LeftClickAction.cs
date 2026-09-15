using UnityEngine;

/// <summary>What a left click would actually do right now.</summary>
public enum LeftClickKind
{
    /// <summary>Nothing. No prompt, no ghost.</summary>
    None,

    /// <summary>Take an item into empty hands.</summary>
    Take,

    /// <summary>Put the held item into something that accepts it.</summary>
    Put,

    /// <summary>Press something. Hands are untouched.</summary>
    Click,

    /// <summary>Set the held item down on the world.</summary>
    Drop,
}

/// <summary>
/// The resolved meaning of a left click, with whatever target is needed to carry it out.
/// Produced by <see cref="InteractionResolver"/>.
/// </summary>
public readonly struct LeftClickAction
{
    public readonly LeftClickKind Kind;
    public readonly IItemSource Source;
    public readonly IItemReceiver Receiver;
    public readonly IClickTarget Click;

    private LeftClickAction(LeftClickKind kind, IItemSource source, IItemReceiver receiver, IClickTarget click)
    {
        Kind = kind;
        Source = source;
        Receiver = receiver;
        Click = click;
    }

    public static readonly LeftClickAction None = new(LeftClickKind.None, null, null, null);

    public static LeftClickAction Take(IItemSource source) => new(LeftClickKind.Take, source, null, null);

    public static LeftClickAction Put(IItemReceiver receiver) => new(LeftClickKind.Put, null, receiver, null);

    public static LeftClickAction Press(IClickTarget click) => new(LeftClickKind.Click, null, null, click);

    public static readonly LeftClickAction Drop = new(LeftClickKind.Drop, null, null, null);
}

/// <summary>
/// The single place that decides what a left click means.
///
/// This used to live in two files — <see cref="PlayerHands"/> did it to act, and the
/// prompt UI did it again to describe — and the two had already drifted apart: aiming at
/// an idle POS while carrying something showed "주문 수락" but actually dropped the item on
/// the floor. Now everything asks this one method, so the prompt, the placement ghost and
/// the click itself cannot disagree.
/// </summary>
public static class InteractionResolver
{
    /// <summary>
    /// Works out what left click does, in the same priority order for everyone:
    /// full hands put down or press; empty hands take or press.
    /// </summary>
    public static LeftClickAction Resolve(PlayerInteractor aim, PlayerHands hands)
    {
        if (aim == null || hands == null)
        {
            return LeftClickAction.None;
        }

        if (hands.IsHolding)
        {
            if (!aim.HasHit)
            {
                // Nothing under the crosshair: it lands in front of the player.
                return LeftClickAction.Drop;
            }

            IItemReceiver receiver = aim.GetAimed<IItemReceiver>();
            if (receiver != null && receiver.CanReceive(hands.HeldItem, hands))
            {
                return LeftClickAction.Put(receiver);
            }

            IClickTarget heldClick = aim.GetAimed<IClickTarget>();
            if (heldClick != null && heldClick.CanClick(hands))
            {
                return LeftClickAction.Press(heldClick);
            }

            return LeftClickAction.Drop;
        }

        if (!aim.HasHit)
        {
            return LeftClickAction.None;
        }

        IItemSource source = aim.GetAimed<IItemSource>();
        if (source != null && source.CanProvide(hands))
        {
            return LeftClickAction.Take(source);
        }

        IClickTarget click = aim.GetAimed<IClickTarget>();
        if (click != null && click.CanClick(hands))
        {
            return LeftClickAction.Press(click);
        }

        return LeftClickAction.None;
    }
}
