using UnityEngine;

/// <summary>
/// 항상 카메라를 향하게 돌린다. 월드에 떠 있는 스프라이트에 붙인다.
///
/// 태어날 때 한 번만 각도를 맞추면 플레이어가 고개를 돌리는 순간 비스듬해진다. 1인칭이라
/// 그 일이 상시로 일어나서, 매 프레임 맞추는 수밖에 없다.
///
/// 손님 머리 위 UI들과 같은 방식으로 카메라를 '바라보지' 않고 카메라의 방향을 따라간다.
/// 가까이서 올려다볼 때 LookAt은 그림을 기울여 고장난 것처럼 보인다.
/// </summary>
public class SpriteBillboard : MonoBehaviour
{
    [Tooltip("정면을 본 상태에서 추가로 눕히는 각도. 하트마다 조금씩 다르게 주면 자연스럽습니다.")]
    [SerializeField] private float tiltDegrees;

    private Camera _camera;

    /// <summary>기울기를 코드에서 정할 때. 스폰 직후에 부른다.</summary>
    public void SetTilt(float degrees)
    {
        tiltDegrees = degrees;
    }

    // 카메라가 그 프레임의 이동을 끝낸 뒤에 맞춰야 한 프레임 늦어 흔들리지 않는다.
    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;

            if (_camera == null)
            {
                return;
            }
        }

        transform.rotation = Quaternion.LookRotation(_camera.transform.forward, Vector3.up)
                             * Quaternion.Euler(0f, 0f, tiltDegrees);
    }
}
