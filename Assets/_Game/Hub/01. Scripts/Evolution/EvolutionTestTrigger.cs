using UnityEngine;

/// <summary>
/// Evol 테스트 씬 전용 버튼 브릿지. UnityEvent(Button.onClick)는 인자 있는 메서드를
/// 직접 연결할 수 없으므로, EvolutionController.PlayEvolution(character)를 대신 호출해준다.
/// 실제 게임에서는 미니게임 클리어 이벤트가 이 역할을 대신하므로 이 스크립트는 필요 없다.
/// </summary>
public class EvolutionTestTrigger : MonoBehaviour
{
    [SerializeField] private EvolutionController evolutionController;
    [SerializeField] private CharacterEvolutionState character;

    public void TriggerEvolution()
    {
        evolutionController.PlayEvolution(character);
    }
}
