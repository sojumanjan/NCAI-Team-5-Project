using System;

/// <summary>Which button an action is bound to. Display text is decided by the UI.</summary>
public enum InputVerb
{
    /// <summary>Carrying things around: take, put in, plain click.</summary>
    LeftClick,

    /// <summary>Operating a station.</summary>
    Interact,
}

/// <summary>
/// One line of the on-screen prompt: a button, what it does, and whether it is available.
///
/// A struct rather than a class because these are rebuilt every frame and thrown away; no
/// reason to allocate. Several can be shown at once — holding beans at the coffee machine
/// offers both "원두 넣기" and "작동시키기".
/// </summary>
public readonly struct ActionPrompt : IEquatable<ActionPrompt>
{
    /// <summary>Which button performs this.</summary>
    public readonly InputVerb Verb;

    /// <summary>What it does, in the player's language. "원두 넣기".</summary>
    public readonly string Label;

    /// <summary>False when the action is visible but not currently possible — draw it dimmed.</summary>
    public readonly bool Enabled;

    /// <summary>Seconds the button must be held. 0 means a single press.</summary>
    public readonly float HoldSeconds;

    public ActionPrompt(InputVerb verb, string label, bool enabled, float holdSeconds = 0f)
    {
        Verb = verb;
        Label = label;
        Enabled = enabled;
        HoldSeconds = holdSeconds;
    }

    /// <summary>True when this action needs a press-and-hold and a progress bar.</summary>
    public bool IsHold => HoldSeconds > 0f;

    // Equality lets the source skip raising an event when nothing actually changed,
    // which keeps the UI from rebuilding itself sixty times a second.
    public bool Equals(ActionPrompt other)
    {
        return Verb == other.Verb
               && Enabled == other.Enabled
               && string.Equals(Label, other.Label, StringComparison.Ordinal)
               && HoldSeconds.Equals(other.HoldSeconds);
    }

    public override bool Equals(object obj) => obj is ActionPrompt other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Verb;
            hash = (hash * 397) ^ (Label != null ? Label.GetHashCode() : 0);
            hash = (hash * 397) ^ Enabled.GetHashCode();
            hash = (hash * 397) ^ HoldSeconds.GetHashCode();
            return hash;
        }
    }
}
