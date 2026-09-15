/// <summary>
/// A plain left click, where nothing enters or leaves the player's hands.
///
/// The third and last left-click verb. <see cref="IItemSource"/> hands something over,
/// <see cref="IItemReceiver"/> takes something away, and this one just registers the
/// click — accepting an order at a POS terminal, flipping a switch, ringing a bell.
///
/// <see cref="PlayerHands"/> tries it only after the item verbs decline, so an object can
/// safely be both (a station that hands over a dish, and reacts to a click when empty).
/// </summary>
public interface IClickTarget
{
    /// <summary>
    /// What the prompt should say, e.g. "주문 수락". The other two left-click verbs can
    /// build their own wording from the item involved, but a plain click could mean
    /// anything, so it has to say so itself.
    /// </summary>
    string ClickPrompt { get; }

    /// <summary>False when clicking would do nothing right now.</summary>
    bool CanClick(PlayerHands hands);

    /// <summary>The player clicked. Hands are untouched.</summary>
    void OnClick(PlayerHands hands);
}
