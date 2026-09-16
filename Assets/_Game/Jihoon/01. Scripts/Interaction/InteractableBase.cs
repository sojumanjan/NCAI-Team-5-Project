using UnityEngine;

/// <summary>
/// 상호작용 물체용 편의 베이스. <see cref="Interact"/> 하나만 override하면 끝이고,
/// 나머지는 전부 동작하는 기본값이 있다. 이미 다른 클래스를 상속 중이라면
/// <see cref="IInteractable"/>을 직접 구현하면 된다.
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

    /// <summary>런타임에 상호작용을 켜고 끈다. 퀘스트 단계가 넘어갔을 때 같은 경우.</summary>
    public void SetInteractable(bool value) => interactable = value;
}
