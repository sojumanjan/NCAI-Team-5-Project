using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 화면을 검게 덮고 다음 씬으로 넘긴다. 타이틀의 시작하기 버튼이 쓴다.
///
/// 덮개를 자기 Canvas에 따로 두는 이유는 옵션 창(Menu Canvas, sortingOrder 21)까지
/// 가려야 하기 때문이다. 같은 캔버스에 두면 옵션 창이 암전 위에 떠 있는 그림이 된다.
///
/// 덮이는 동안 클릭을 막는다. 1.5초는 버튼을 두 번 누르기 충분한 시간이고, 그러면
/// 씬 로드가 두 번 걸린다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [Header("시간")]
    [Tooltip("검게 덮이는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 1.5f;

    [Header("참조")]
    [Tooltip("투명도를 만질 곳. 비워두면 이 오브젝트에서 찾습니다.")]
    [SerializeField] private CanvasGroup group;

    private bool _running;

    private void Awake()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    /// <summary>덮은 뒤 허브로 넘어간다. 시작하기 버튼에 그대로 연결할 수 있다.</summary>
    public void FadeToHub()
    {
        Fade(() =>
        {
            GameFlow flow = GameFlow.Instance;
            if (flow != null)
            {
                flow.ReturnToMain();
            }
        });
    }

    /// <summary>덮은 뒤 <paramref name="then"/>을 부른다.</summary>
    public void Fade(Action then)
    {
        if (_running)
        {
            return;
        }

        _running = true;
        StartCoroutine(Run(then));
    }

    private IEnumerator Run(Action then)
    {
        // 덮이기 시작하는 순간부터 막는다. 다 검어진 뒤에 막으면 그 사이에 또 눌린다.
        group.blocksRaycasts = true;

        float elapsed = 0f;
        float total = Mathf.Max(0.01f, duration);

        while (elapsed < total)
        {
            // 옵션 창이 시간을 0으로 눌러둔 채 넘어올 수 있다. 연출은 그것과 무관해야 한다.
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / total);
            yield return null;
        }

        group.alpha = 1f;
        then?.Invoke();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
    }
}
