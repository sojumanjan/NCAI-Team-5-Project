/// <summary>
/// Anything the player can aim at and use. The interactor never knows what the object
/// actually is — it only reports focus and input, and the object does the work in
/// <see cref="Interact"/>.
///
/// Most objects should inherit <see cref="InteractableBase"/> instead of implementing
/// this directly; it supplies sane defaults for everything but Interact.
/// </summary>
public interface IInteractable
{
    /// <summary>Text for the on-screen prompt, e.g. "문 열기".</summary>
    string Prompt { get; }

    /// <summary>Seconds the key must be held. 0 means it fires on press.</summary>
    float HoldDuration { get; }

    /// <summary>False greys the prompt out and blocks Interact (locked, on cooldown...).</summary>
    bool CanInteract(PlayerInteractor interactor);

    /// <summary>Do the thing. Called once, after any hold completes.</summary>
    void Interact(PlayerInteractor interactor);

    /// <summary>The player started aiming at this object.</summary>
    void OnFocusEnter(PlayerInteractor interactor);

    /// <summary>The player looked away, or the object became unavailable.</summary>
    void OnFocusExit(PlayerInteractor interactor);

    /// <summary>Hold progress, 0 to 1. Only called while HoldDuration is above zero.</summary>
    void OnHoldProgress(PlayerInteractor interactor, float normalized);

    /// <summary>The hold was released or interrupted before completing.</summary>
    void OnHoldCanceled(PlayerInteractor interactor);
}
