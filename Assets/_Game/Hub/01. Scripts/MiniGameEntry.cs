using UnityEngine;

/// <summary>
/// 메인 화면에서 미니게임으로 들어가는 문. 버튼의 OnClick에 <see cref="Enter"/>를 걸거나,
/// 방 안의 사물에 붙여 클릭 처리에서 불러도 된다.
/// </summary>
public class MiniGameEntry : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("프로젝트에 하나뿐인 GameFlow 에셋.")]
    [SerializeField] private GameFlow flow;

    [Tooltip("이 문이 들어갈 미니게임.")]
    [SerializeField] private MiniGameDefinition miniGame;

    /// <summary>이 문이 가리키는 미니게임. 메인 화면 UI가 이름·아이콘을 그릴 때 쓴다.</summary>
    public MiniGameDefinition MiniGame => miniGame;

    /// <summary>이미 깬 미니게임인지. 잠금 표시나 체크 표시에 쓴다.</summary>
    public bool IsCleared => flow != null && flow.IsCleared(miniGame);

    private void Awake()
    {
        if (flow == null || miniGame == null)
        {
            Debug.LogError($"{nameof(MiniGameEntry)} on '{name}': GameFlow와 Definition을 연결하세요.", this);
            enabled = false;
        }
    }

    /// <summary>들어간다. Button OnClick에 그대로 연결할 수 있다.</summary>
    public void Enter()
    {
        if (!enabled)
        {
            return;
        }

        flow.LoadMiniGame(miniGame);
    }
}
