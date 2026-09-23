using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 허브 연출 디버그 키. 미니게임을 매번 깨고 오지 않고도 변신·진화 연출을 반복해서 보기 위한 것.
///
/// F3      : 전체 초기화. 모든 클리어 기록을 지우고 씨앗도 첫 단계로 되돌린다.
/// F4 ~ F7 : targets 순서대로 그 미니게임을 깨고 돌아온 것처럼 보고하고, 돌아왔을 때와 같은 길로 연출을 튼다.
///
/// 에디터와 개발 빌드에서만 동작한다. 최종 빌드에서 키를 눌러 진행도가 바뀌면 안 된다.
/// </summary>
public class HubDebugKeys : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("F4, F5, F6, F7 순서로 다룰 오브젝트.")]
    [SerializeField] private MiniGameEntry[] targets = new MiniGameEntry[4];

    [Tooltip("연출기. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private HubClearReveal reveal;

    [Tooltip("F3에서 첫 단계로 되돌릴 씨앗. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private CharacterEvolutionState character;

    [Tooltip("클리어로 보고할 점수 (0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float reportScore01 = 1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Awake()
    {
        if (reveal == null)
        {
            reveal = FindAnyObjectByType<HubClearReveal>();
        }

        if (character == null)
        {
            character = FindAnyObjectByType<CharacterEvolutionState>();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            ResetAll();
        }
        else if (keyboard.f4Key.wasPressedThisFrame)
        {
            SimulateClearReturn(0);
        }
        else if (keyboard.f5Key.wasPressedThisFrame)
        {
            SimulateClearReturn(1);
        }
        else if (keyboard.f6Key.wasPressedThisFrame)
        {
            SimulateClearReturn(2);
        }
        else if (keyboard.f7Key.wasPressedThisFrame)
        {
            SimulateClearReturn(3);
        }
    }

    private void ResetAll()
    {
        // 연출 도중에 되돌리면 흰 덮개와 스프라이트가 엇갈린 채로 굳는다.
        if (reveal != null && reveal.IsPlaying)
        {
            return;
        }

        GameFlow flow = GameFlow.Instance;
        if (flow == null)
        {
            return;
        }

        // ProgressChanged로 모든 오브젝트가 미클리어 모습으로 돌아간다.
        flow.ResetRun();

        // 기록만 지우고 씨앗을 그대로 두면, 몇 번 돌리다 마지막 단계에 막혀 진화를 다시 볼 수 없다.
        if (character != null)
        {
            character.ResetStage();
        }

        Debug.Log("[HubDebug] F3: 전체 클리어 초기화 + 씨앗 첫 단계", this);
    }

    private void SimulateClearReturn(int index)
    {
        if (targets == null || index >= targets.Length || targets[index] == null || targets[index].MiniGame == null)
        {
            Debug.LogWarning($"[HubDebug] F{index + 4}: 대상이 비어 있습니다. Targets {index}번 칸을 채워주세요.", this);
            return;
        }

        GameFlow flow = GameFlow.Instance;
        if (flow == null || reveal == null || reveal.IsPlaying)
        {
            return;
        }

        MiniGameDefinition game = targets[index].MiniGame;
        flow.Report(game, new MiniGameResult(true, reportScore01));

        // 이미 깬 상태에서 누르면 실제 재도전과 똑같이 아무 일도 없다.
        bool played = reveal.PlayPending(false);
        Debug.Log(played
                      ? $"[HubDebug] F{index + 4}: '{game.DisplayName}' 클리어 복귀 연출 재생"
                      : $"[HubDebug] F{index + 4}: '{game.DisplayName}'는 이미 클리어 상태라 연출 없음. F3으로 초기화하세요.",
                  this);
    }
#endif
}
