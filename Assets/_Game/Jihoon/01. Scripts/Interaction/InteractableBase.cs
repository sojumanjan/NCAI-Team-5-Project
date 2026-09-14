using UnityEngine;

/// <summary>
/// Convenience base for interactable objects. Override <see cref="Interact"/> and you are
/// done; everything else has a working default. Objects that already inherit something
/// else can implement <see cref="IInteractable"/> directly instead.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("상호작용")]
    [Tooltip("화면에 표시할 문구.")]
    [SerializeField] protected string prompt = "상호작용";

    [Tooltip("길게 누르는 시간 (초). 0이면 누르는 즉시 실행됩니다.")]
    [SerializeField] protected float holdDuration;

    [Tooltip("끄면 조준은 되지만 실행되지 않습니다.")]
    [SerializeField] protected bool interactable = true;

    public virtual string Prompt => prompt;

    public virtual float HoldDuration => Mathf.Max(0f, holdDuration);

    public virtual bool CanInteract(PlayerInteractor interactor) => interactable && isActiveAndEnabled;

    public abstract void Interact(PlayerInteractor interactor);

    public virtual void OnFocusEnter(PlayerInteractor interactor) { }

    public virtual void OnFocusExit(PlayerInteractor interactor) { }

    public virtual void OnHoldProgress(PlayerInteractor interactor, float normalized) { }

    public virtual void OnHoldCanceled(PlayerInteractor interactor) { }

    /// <summary>Turns interaction on or off at runtime, e.g. after a quest step.</summary>
    public void SetInteractable(bool value) => interactable = value;
}
