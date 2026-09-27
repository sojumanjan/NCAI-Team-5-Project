using UnityEngine;

/// <summary>
/// 팩맨 진행 중, 화면 가장자리에 화살표로 가장 가까운 파워펠릿의 방향을 가리킨다.
/// 펠릿이 남아있지 않거나 팩맨 상태가 아니면 화살표를 숨긴다.
/// </summary>
public class PelletDirectionIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform arrowRect;
    [Tooltip("방향 기준이 되는 플레이어 캐릭터. 카메라(시야)가 아니라 이 오브젝트가 바라보는 수평 방향을 기준으로 삼는다.")]
    [SerializeField] private Transform player;
    [Tooltip("화살표가 화면 테두리에서 안쪽으로 떨어지는 여백 (Canvas 기준 단위)")]
    [SerializeField] private float screenEdgePadding = 60f;

    private void Update()
    {
        bool isPacman = MiniGameFlowManager.Instance != null
            && MiniGameFlowManager.Instance.CurrentState == MiniGameState.Pacman;

        Vector3? nearest = isPacman && PelletSpawner.Instance != null && player != null
            ? PelletSpawner.Instance.GetNearestPelletPosition(player.position)
            : null;

        SetVisible(nearest.HasValue);

        if (nearest.HasValue)
        {
            PointTo(nearest.Value);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (arrowRect.gameObject.activeSelf != isVisible)
        {
            arrowRect.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// 카메라(시야/에임) 방향이 아니라 플레이어 캐릭터의 수평 정면을 기준으로 삼는다.
    /// PlayerController.HandleLook()이 마우스 좌우 입력을 카메라가 아니라 Player 루트 자체에
    /// 적용하고(상하 피치만 별도 자식 카메라 피벗에 적용) 카메라 흔들림도 그 자식에서만 일어나므로,
    /// player.forward는 시야 흔들림/피치와 무관한 순수한 "몸이 향한 수평 방향"이다.
    /// </summary>
    private void PointTo(Vector3 worldTarget)
    {
        Vector3 toTarget = worldTarget - player.position;
        toTarget.y = 0f;

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 플레이어 정면 = 화면 위쪽(12시)이 되도록, 정면 기준 좌우 각도를 화면 각도로 변환한다.
        float signedAngle = Vector3.SignedAngle(forward, toTarget, Vector3.up);
        float angle = (90f - signedAngle) * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float halfWidth = canvasRect.rect.width * 0.5f - screenEdgePadding;
        float halfHeight = canvasRect.rect.height * 0.5f - screenEdgePadding;

        float scaleX = Mathf.Abs(cos) > 0.0001f ? halfWidth / Mathf.Abs(cos) : float.MaxValue;
        float scaleY = Mathf.Abs(sin) > 0.0001f ? halfHeight / Mathf.Abs(sin) : float.MaxValue;
        float scale = Mathf.Min(scaleX, scaleY);

        arrowRect.anchoredPosition = new Vector2(cos, sin) * scale;
        arrowRect.localEulerAngles = new Vector3(0f, 0f, angle * Mathf.Rad2Deg);
    }
}
