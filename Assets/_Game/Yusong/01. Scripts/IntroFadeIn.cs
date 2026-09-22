using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public class IntroFadeIn : MonoBehaviour
{
    // 튜토리얼 스포트라이트(암전)가 이 값을 보고 자기 활성화를 늦춰서,
    // 인트로 페이드가 끝난 뒤에야 화면을 다시 어둡게 만들도록 한다.
    public static bool IsPlaying { get; private set; }

    [SerializeField] private float fadeDuration = 1f;

    private Image image;

    private void Awake()
    {
        IsPlaying = true;
        image = GetComponent<Image>();

        Color c = image.color;
        c.a = 1f;
        image.color = c;
        image.raycastTarget = true;
    }

    private void Start()
    {
        StartCoroutine(FadeOut());
    }

    // 씬 로드 직후 첫 프레임에 렉(셰이더 컴파일, 캔버스 초기화 등)이 튀면 그 한 프레임의
    // deltaTime이 fadeDuration을 통째로 삼켜버려 페이드가 순간적으로 끝나버릴 수 있다.
    // 프레임당 반영량에 상한을 둬서 렉이 있어도 항상 여러 프레임에 걸쳐 자연스럽게 재생되게 한다.
    private const float MaxStepPerFrame = 1f / 30f;

    private IEnumerator FadeOut()
    {
        // 타임스케일이 0이 되는 다른 연출(튜토리얼 등)과 겹쳐도 항상 재생되도록 unscaled 사용.
        float t = 0f;
        Color c = image.color;

        while (t < fadeDuration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, MaxStepPerFrame);
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            image.color = c;
            yield return null;
        }

        c.a = 0f;
        image.color = c;
        image.raycastTarget = false;
        IsPlaying = false;
        gameObject.SetActive(false);
    }
}
}
