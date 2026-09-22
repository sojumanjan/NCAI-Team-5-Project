using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 씬을 열자마자 깜깜한 화면을 잠시 덮었다가 걷어내는 도입부.
///
/// 허브에서 사물을 누르고 설명을 읽고 넘어왔는데 곧바로 주방 한가운데 서 있으면
/// 이야기가 툭 끊긴다. 한 박자 쉬어주면 "설명을 들었다"가 "들어왔다"가 된다.
///
/// 무엇을 보여줄지는 여기서 정하지 않는다. 이 오브젝트 밑에 자식으로 깔아둔 것이
/// 그대로 보인다 — 글이든 그림이든. 문구를 코드가 덮어쓰면 씬에서 고친 것이
/// 플레이할 때마다 되돌아가서, 손으로 다듬을 수가 없다.
///
/// 튜토리얼 패널을 따로 잡아두지 않는다. 검은 화면이 이미 전부 가리고 있어서,
/// 가려진 동안 무엇이 떠 있든 보이지 않는다. 대신 이 오브젝트는 Canvas의
/// <b>맨 마지막 자식</b>이어야 한다 — 그래야 다른 UI 위에 덮인다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SceneIntroCard : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("덮개 전체의 투명도. 비워두면 이 오브젝트에서 찾습니다.")]
    [SerializeField] private CanvasGroup group;

    [Header("시간")]
    [Tooltip("문구를 띄워두는 시간 (초).")]
    [SerializeField] private float holdSeconds = 3f;

    [Tooltip("걷히는 데 걸리는 시간 (초).")]
    [SerializeField] private float fadeSeconds = 1f;

    [Header("건너뛰기")]
    [Tooltip("아무 키나 눌러 넘깁니다. 작업 중 같은 씬을 반복해서 열 때 편합니다.")]
    [SerializeField] private bool allowSkip = true;

    [Header("잠글 것")]
    [Tooltip("도입부 동안 꺼둘 스크립트들. MiniGameSession의 Disable On End와 같은 것을 넣으세요.")]
    [SerializeField] private MonoBehaviour[] disableWhileShowing;

    // 원래 꺼져 있던 스크립트까지 켜버리면 안 된다. 내가 끈 것만 기억해뒀다 되돌린다.
    private readonly List<MonoBehaviour> _locked = new();

    private void Awake()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }

        // 첫 프레임이 그려지기 전에 덮어야 한다. Start에서 하면 씬이 한 번 번쩍인다.
        group.alpha = 1f;
        group.blocksRaycasts = true;
    }

    private void Start()
    {
        Lock();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float held = 0f;
        while (held < holdSeconds && !ReadSkip())
        {
            held += Time.unscaledDeltaTime;
            yield return null;
        }

        float duration = Mathf.Max(0.01f, fadeSeconds);
        float faded = 0f;
        while (faded < duration)
        {
            faded += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(faded / duration);
            yield return null;
        }

        Finish();
    }

    /// <summary>지금 바로 걷어낸다. 다른 연출이 끼어들 때를 위해 열어둔다.</summary>
    public void Finish()
    {
        StopAllCoroutines();

        group.alpha = 0f;
        group.blocksRaycasts = false;

        Unlock();
        gameObject.SetActive(false);
    }

    private bool ReadSkip()
    {
        if (!allowSkip)
        {
            return false;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private void Lock()
    {
        _locked.Clear();

        if (disableWhileShowing == null)
        {
            return;
        }

        foreach (MonoBehaviour script in disableWhileShowing)
        {
            if (script == null || !script.enabled)
            {
                continue;
            }

            script.enabled = false;
            _locked.Add(script);
        }
    }

    private void Unlock()
    {
        foreach (MonoBehaviour script in _locked)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        _locked.Clear();
    }

    private void OnValidate()
    {
        holdSeconds = Mathf.Max(0f, holdSeconds);
        fadeSeconds = Mathf.Max(0.01f, fadeSeconds);
    }
}
