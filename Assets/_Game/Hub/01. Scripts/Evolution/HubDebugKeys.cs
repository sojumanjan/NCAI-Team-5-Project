using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 허브 연출 디버그 키. 미니게임을 매번 깨고 오지 않고도 변신·진화 연출을 반복해서 보기 위한 것.
///
/// F3      : 전체 초기화. 모든 클리어 기록을 지우고 씨앗도 첫 단계로 되돌린다.
/// F4 ~ F7 : targets 순서대로 그 미니게임을 깨고 돌아온 것처럼 보고하고, 돌아왔을 때와 같은 길로 연출을 튼다.
/// F8      : 4단계 진화를 바로 본다. 앞의 두 게임을 깬 상태(씨앗 3단계)로 만든 뒤, 세 번째 게임의
///           클리어 연출을 이어서 틀어 씨앗이 4단계로 진화한다. 끝나면 엔딩 직전 상태라
///           이어서 F7을 누르면 마지막 클리어 → 최종 진화 → 엔딩까지 본다.
/// F9      : 엔딩만 바로 튼다. 크레딧 글을 고치며 확인할 때.
///
/// 에디터에서만 동작한다. 빌드(개발 빌드 포함)에서 키를 눌러 진행도가 바뀌면 안 된다.
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

    [Tooltip("F9로 틀 엔딩. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private HubEnding ending;

    [Tooltip("클리어로 보고할 점수 (0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float reportScore01 = 1f;

#if UNITY_EDITOR
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

        if (ending == null)
        {
            ending = FindAnyObjectByType<HubEnding>();
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
        else if (keyboard.f8Key.wasPressedThisFrame)
        {
            EvolveToStageFour();
        }
        else if (keyboard.f9Key.wasPressedThisFrame)
        {
            PlayEndingNow();
        }
    }

    private bool Busy => (reveal != null && reveal.IsPlaying) || (ending != null && ending.IsPlaying);

    private void EvolveToStageFour()
    {
        // 세 번째 칸의 클리어 연출로 4단계 진화를 틀어야 하니 최소 두 칸은 있어야 한다.
        if (Busy || targets == null || targets.Length < 2)
        {
            return;
        }

        GameFlow flow = GameFlow.Instance;
        if (flow == null)
        {
            return;
        }

        ResetAll();

        // 마지막 두 칸을 남기고 깬다. 남은 둘 중 앞쪽이 지금 진화할 판, 뒤쪽이 엔딩으로 가는 판이다.
        int evolveIndex = targets.Length - 2;
        int grown = 0;
        for (int i = 0; i < evolveIndex; i++)
        {
            if (targets[i] == null || targets[i].MiniGame == null)
            {
                continue;
            }

            flow.Report(targets[i].MiniGame, new MiniGameResult(true, reportScore01));
            grown++;
        }

        // 보고만 하면 "방금 깨고 돌아온 결과"로 남아, 다음 F 키가 엉뚱한 게임의 연출을 튼다.
        flow.TryConsumeLastResult(out _, out _);

        if (character != null)
        {
            for (int i = 0; i < grown; i++)
            {
                character.AdvanceStage();
            }
        }

        Debug.Log($"[HubDebug] F8: {grown}개 클리어 상태에서 '{targets[evolveIndex]?.name}' 클리어 연출 → 씨앗 {grown + 2}단계 진화. " +
                  $"끝나면 F{targets.Length + 3}로 마지막 클리어 → 엔딩", this);

        // 연출은 실제로 돌아왔을 때와 같은 길로 튼다. 진화 흰 덮개·표정까지 실제와 똑같이 확인하려고.
        SimulateClearReturn(evolveIndex);
    }

    private void PlayEndingNow()
    {
        if (Busy || ending == null)
        {
            return;
        }

        StartCoroutine(ending.Play());
        Debug.Log("[HubDebug] F9: 엔딩 바로 재생", this);
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

        // 엔딩을 한 번 봤으면 방이 깨끗해져 있다. 처음부터 다시 보려면 엉망인 방으로 돌려놔야 한다.
        if (ending != null)
        {
            ending.SetRoomClean(false);
        }

        Debug.Log("[HubDebug] F3: 전체 클리어 초기화 + 씨앗 첫 단계 + 엉망인 방", this);
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
