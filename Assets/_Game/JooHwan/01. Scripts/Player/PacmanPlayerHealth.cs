using UnityEngine;

/// <summary>
/// 팩맨 전용 목숨 시스템. 고스트에 접촉하면 목숨을 잃고, 0이 되면 팩맨만 재시작한다.
/// (테트리스는 별도의 끼임 즉사/재시작 로직을 그대로 사용하며, 이 컴포넌트와 무관하다)
/// </summary>
public class PacmanPlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    [SerializeField] private HeartUI heartUI;
    [Tooltip("사망 시 팩맨을 재시작할 위치 (맵 중앙의 PacmanEntryPoint)")]
    [SerializeField] private Transform pacmanRestartPoint;
    [Tooltip("피격 후 연속으로 목숨을 잃지 않도록 잠시 무적이 되는 시간")]
    [SerializeField] private float invincibleDuration = 1.5f;

    private int currentLives;
    private float invincibleUntil;

    public Transform PacmanRestartPoint => pacmanRestartPoint;

    private void OnEnable()
    {
        ResetLives();
    }

    public void ResetLives()
    {
        currentLives = maxLives;
        invincibleUntil = 0f;

        if (heartUI != null)
        {
            heartUI.SetLives(currentLives);
        }
    }

    public void TakeHit()
    {
        if (Time.time < invincibleUntil)
        {
            return;
        }

        currentLives--;
        invincibleUntil = Time.time + invincibleDuration;

        if (heartUI != null)
        {
            heartUI.SetLives(currentLives);
        }

        if (currentLives <= 0)
        {
            PacmanGameManager.Instance.ShowDeathPopupForPacman();
        }
        else
        {
            PacmanGameManager.Instance.OnPlayerHit();
        }
    }
}
