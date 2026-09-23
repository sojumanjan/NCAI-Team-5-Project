using UnityEngine;

/// <summary>
/// 방을 돌아다니는 씨앗이 가끔 >< 표정(신난 얼굴)을 지었다가 돌아온다. CharacterRoot에 붙인다.
/// 계속 같은 얼굴로 걷기만 하면 금방 배경처럼 보여서, 가끔 표정이 바뀌어 살아 있는 느낌을 준다.
///
/// 진화나 엔딩이 도는 동안은 손을 뗀다. 그쪽이 표정을 직접 쥐고 있어서, 여기서 타이머가 끝났다고
/// 기본 얼굴로 되돌리면 진화 직후의 웃는 얼굴을 덮어버린다. 진화 시작 때 기본 얼굴로 돌려놓는 건
/// EvolutionController가 한다 — 그래야 이 컴포넌트가 없어도 진화는 늘 기본 얼굴에서 시작한다.
/// </summary>
public class SeedExpression : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("표정을 바꿀 씨앗. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private CharacterEvolutionState character;

    [Tooltip("진화 중엔 손을 뗍니다. 비워두면 EvolutionController.Instance를 씁니다.")]
    [SerializeField] private EvolutionController evolution;

    [Tooltip("엔딩 중엔 손을 뗍니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private HubEnding ending;

    [Header("타이밍")]
    [Tooltip("다음 표정 변화까지 기다리는 시간 범위 (초). 매번 이 안에서 무작위로 고릅니다.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(17f, 23f);

    [Tooltip(">< 표정을 유지하는 시간 범위 (초).")]
    [SerializeField] private Vector2 durationRange = new Vector2(3f, 4f);

    private bool _showing;
    private float _nextAt;
    private float _showUntil;

    private void Awake()
    {
        if (character == null)
        {
            character = FindAnyObjectByType<CharacterEvolutionState>();
        }

        if (ending == null)
        {
            ending = FindAnyObjectByType<HubEnding>();
        }
    }

    private void OnEnable() => ScheduleNext();

    private void Update()
    {
        if (character == null)
        {
            return;
        }

        if (IsBusy())
        {
            // 표정은 그쪽이 쥐고 있으니 건드리지 않고, 끝난 뒤부터 다시 센다.
            _showing = false;
            ScheduleNext();
            return;
        }

        // 게임 시간으로 센다. 옵션 창으로 멈춘 동안 표정만 혼자 바뀌면 이상하다.
        float now = Time.time;

        if (_showing)
        {
            if (now >= _showUntil)
            {
                character.ShowNormal();
                _showing = false;
                ScheduleNext();
            }

            return;
        }

        if (now >= _nextAt)
        {
            character.ShowHappy();
            _showing = true;
            _showUntil = now + Random.Range(durationRange.x, durationRange.y);
        }
    }

    private bool IsBusy()
    {
        EvolutionController controller = evolution != null ? evolution : EvolutionController.Instance;
        if (controller != null && controller.IsPlaying)
        {
            return true;
        }

        return ending != null && ending.IsPlaying;
    }

    private void ScheduleNext()
    {
        _nextAt = Time.time + Random.Range(intervalRange.x, intervalRange.y);
    }

    private void OnValidate()
    {
        intervalRange.x = Mathf.Max(0.1f, intervalRange.x);
        intervalRange.y = Mathf.Max(intervalRange.x, intervalRange.y);
        durationRange.x = Mathf.Max(0.1f, durationRange.x);
        durationRange.y = Mathf.Max(durationRange.x, durationRange.y);
    }
}
