using System.Collections;
using UnityEngine;

/// <summary>
/// 씬이 열리면 검은 화면에서 서서히 걷힌다. 화면 전체를 덮는 검은 Image에 CanvasGroup과 함께 붙인다.
///
/// 앞 씬이 검게 덮으며 넘어오므로(타이틀의 ScreenFader, 미니게임 복귀) 여기서 검은 채로 받아
/// 걷어내야 화면이 끊기지 않는다. 덮개는 옵션 창보다 위인 자기 Canvas에 둬야 전부 가려진다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SceneFadeIn : MonoBehaviour
{
    [Header("시간")]
    [Tooltip("검은 화면이 다 걷히는 데 걸리는 시간 (초).")]
    [SerializeField] private float duration = 1.5f;

    [Tooltip("걷히기 시작하기 전 검은 채로 기다리는 시간 (초). 씬 로딩 직후 끊김을 가릴 때 씁니다.")]
    [SerializeField] private float delay = 0f;

    [Header("참조")]
    [Tooltip("투명도를 만질 곳. 비워두면 이 오브젝트에서 찾습니다.")]
    [SerializeField] private CanvasGroup group;

    /// <summary>다 걷혔는지. 허브 연출이 검은 화면 뒤에서 먼저 끝나버리지 않도록 이걸 기다린다.</summary>
    public bool IsDone { get; private set; }

    private void Awake()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }

        // Start까지 기다리면 첫 프레임에 방이 한 번 비친 뒤 덮인다.
        group.alpha = 1f;

        // 걷히는 동안 사물을 눌러 다른 씬으로 넘어가 버리면 연출이 중간에 잘린다.
        group.blocksRaycasts = true;
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // 미니게임이 시간을 멈춘 채 넘어와도 걷혀야 한다. 게임 시간으로 재면 영원히 검은 화면이다.
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        float elapsed = 0f;
        float total = Mathf.Max(0.01f, duration);

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / total);
            yield return null;
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
        IsDone = true;

        // 투명해도 전체 화면 Image는 매 프레임 그려진다. 다 걷혔으면 꺼둔다.
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        delay = Mathf.Max(0f, delay);
    }
}
