// 붙은 오브젝트를 켜질 때의 크기를 기준으로 아주 작게 부풀었다 가라앉기를 반복하는 컴포넌트 (엔딩 여우의 잠든 숨)
using UnityEngine;

/// <summary>
/// 1 → Amount → 1을 코사인 곡선으로 되풀이한다. 1에서 시작하므로 켜지는 순간 크기가 튀지 않는다.
///
/// 축은 이 오브젝트의 피벗이다. 바닥에 놓인 그림은 발밑에 빈 부모를 두고 거기에 붙인다 —
/// 가운데 기준으로 부풀면 공중에 뜬 것처럼 보인다.
///
/// 게임 시간으로 잰다. 옵션 창을 열어 시간이 멈추면 다른 허브 연출처럼 같이 멈춰 있어야 한다.
/// </summary>
public class UIBreathe : MonoBehaviour
{
    [Tooltip("숨을 들이쉬었을 때의 배율.")]
    [SerializeField] private float amount = 1.05f;

    [Tooltip("한 번 들이쉬고 내쉬는 시간 (초). 잠든 숨은 느릴수록 편안해 보입니다.")]
    [SerializeField] private float period = 3.5f;

    private Vector3 _baseScale;
    private float _time;

    private void OnEnable()
    {
        _baseScale = transform.localScale;
        _time = 0f;
    }

    private void Update()
    {
        _time += Time.deltaTime;
        float breath = 0.5f - 0.5f * Mathf.Cos(_time / period * Mathf.PI * 2f);
        transform.localScale = _baseScale * Mathf.Lerp(1f, amount, breath);
    }

    // 엔딩을 다시 틀 때 부푼 채로 시작하지 않게 되돌려 둔다.
    private void OnDisable()
    {
        transform.localScale = _baseScale;
    }

    private void OnValidate()
    {
        amount = Mathf.Max(0.01f, amount);
        period = Mathf.Max(0.1f, period);
    }
}
