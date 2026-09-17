using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 미니게임에서 메인 화면으로 나가는 문. 결과 화면의 "돌아가기" 버튼에 <see cref="Exit"/>를
/// 걸면 되고, 작업 중 씬을 오갈 때 쓰라고 단축키도 붙어 있다.
///
/// 단축키는 <see cref="shortcutInEditorOnly"/>가 켜져 있으면 빌드에서 스스로 꺼진다.
/// </summary>
public class MiniGameExit : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("프로젝트에 하나뿐인 GameFlow 에셋.")]
    [SerializeField] private GameFlow flow;

    [Header("단축키")]
    [Tooltip("누르면 메인 화면으로 나갑니다. 작업 중 오갈 때 쓰세요.")]
    [SerializeField] private Key shortcutKey = Key.Escape;

    [Tooltip("켜면 에디터에서만 단축키가 동작합니다.")]
    [SerializeField] private bool shortcutInEditorOnly = true;

    [Tooltip("끄면 단축키 없이 Exit() 호출로만 나갑니다.")]
    [SerializeField] private bool useShortcut = true;

    private void Awake()
    {
        if (flow == null)
        {
            Debug.LogError($"{nameof(MiniGameExit)} on '{name}': GameFlow를 연결하세요.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!useShortcut)
        {
            return;
        }

        if (shortcutInEditorOnly && !Application.isEditor)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[shortcutKey].wasPressedThisFrame)
        {
            Exit();
        }
    }

    /// <summary>메인 화면으로. Button OnClick에 그대로 연결할 수 있다.</summary>
    public void Exit()
    {
        if (flow != null)
        {
            flow.ReturnToMain();
        }
    }
}
