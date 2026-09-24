// 붙은 오브젝트를 켜질 때의 각도를 기준으로 좌우로 살랑살랑 흔드는 컴포넌트 (엔딩 여우 꼬리 등)
using UnityEngine;

/// <summary>
/// 기준 각도에서 사인파로 천천히 좌우로 기운다. 켜져 있는 동안만 움직이고, 꺼지면 원래 각도로 돌아간다.
///
/// 축은 이 오브젝트의 피벗이다. 꼬리처럼 뿌리를 축으로 돌려야 하는 그림은 뿌리 자리에 빈 부모를 두고
/// 거기에 붙인다 — 그림 자체의 피벗을 옮기면 이미지 위치를 다시 맞춰야 해서 번거롭다.
///
/// 게임 시간으로 잰다. 옵션 창을 열어 시간이 멈추면 다른 허브 연출처럼 같이 멈춰 있어야 한다.
/// </summary>
public class UISway : MonoBehaviour
{
    [Tooltip("좌우로 기우는 최대 각도 (도).")]
    [SerializeField] private float amplitude = 4f;

    [Tooltip("한 번 왕복하는 시간 (초).")]
    [SerializeField] private float period = 2.4f;

    private Quaternion _baseRotation;
    private float _time;

    private void OnEnable()
    {
        _baseRotation = transform.localRotation;
        _time = 0f;
    }

    private void Update()
    {
        _time += Time.deltaTime;
        float angle = amplitude * Mathf.Sin(_time / period * Mathf.PI * 2f);
        transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
    }

    // 엔딩을 다시 틀 때 기운 채로 시작하지 않게 되돌려 둔다.
    private void OnDisable()
    {
        transform.localRotation = _baseRotation;
    }

    private void OnValidate()
    {
        period = Mathf.Max(0.1f, period);
    }
}
