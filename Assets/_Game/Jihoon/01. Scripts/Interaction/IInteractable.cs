/// <summary>
/// 플레이어가 조준해서 사용할 수 있는 모든 것. 조준 쪽은 그 물체가 무엇인지 끝까지 모르고,
/// 포커스와 입력만 알려준다. 실제 동작은 물체가 <see cref="Interact"/>에서 한다.
///
/// 대부분은 이걸 직접 구현하지 말고 <see cref="InteractableBase"/>를 상속하면 된다.
/// Interact 말고는 전부 쓸 만한 기본값이 들어 있다.
/// </summary>
public interface IInteractable
{
    /// <summary>화면 프롬프트에 띄울 문구. 예: "문 열기".</summary>
    string Prompt { get; }

    /// <summary>키를 눌러야 하는 시간(초). 0이면 누르는 즉시 실행.</summary>
    float HoldDuration { get; }

    /// <summary>false면 프롬프트가 흐려지고 Interact가 막힌다 (잠김, 쿨다운 등).</summary>
    bool CanInteract(PlayerInteractor interactor);

    /// <summary>실행. 홀드가 있으면 다 채운 뒤 한 번 불린다.</summary>
    void Interact(PlayerInteractor interactor);

    /// <summary>플레이어가 이 물체를 조준하기 시작했다.</summary>
    void OnFocusEnter(PlayerInteractor interactor);

    /// <summary>시선이 벗어났거나 물체를 더 쓸 수 없게 됐다.</summary>
    void OnFocusExit(PlayerInteractor interactor);

    /// <summary>홀드 진행도 0~1. HoldDuration이 0보다 클 때만 불린다.</summary>
    void OnHoldProgress(PlayerInteractor interactor, float normalized);

    /// <summary>홀드를 다 채우기 전에 손을 뗐거나 중단됐다.</summary>
    void OnHoldCanceled(PlayerInteractor interactor);
}
