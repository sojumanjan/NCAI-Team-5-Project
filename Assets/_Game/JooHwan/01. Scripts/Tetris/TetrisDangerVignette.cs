using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 테트리스 끼임 위험도(0~1)를 URP Vignette로 시각화한다.
/// 0~1은 "블록이 착지까지 5m 남은 시점"부터 "침투율 80%(사망 임계값)"까지 이어지는 연속된 위험도이며,
/// FallingBlock이 매 프레임 계산해 SetDangerRatio로 전달한다.
/// 최대 강도에서도 화면을 완전히 가리지 않도록 실제 intensity에는 상한(maxIntensity)을 곱해 적용한다.
/// </summary>
[RequireComponent(typeof(Volume))]
public class TetrisDangerVignette : MonoBehaviour
{
    public static TetrisDangerVignette Instance { get; private set; }

    [Tooltip("dangerRatio=1(사망 임계값)일 때의 실제 Vignette intensity. 사망 플래시 등 다른 피드백이 가려지지 않도록 1보다 낮게 잡는다.")]
    [SerializeField] private float maxIntensity = 0.5f;

    private Vignette vignette;

    private void Awake()
    {
        Instance = this;

        var volume = GetComponent<Volume>();

        // sharedProfile(원본 에셋)의 Vignette를 직접 수정하면, Play 모드를 끌 때 그 값이
        // 에셋 파일 자체에 남아 다음 Play 시작 시 이전 상태(예: 사망 임박 intensity)를 그대로 이어받는다.
        // 반드시 원본을 복제한 별도 인스턴스를 만들어 volume.profile에 할당하고, 그 복제본만 수정한다.
        if (volume.sharedProfile != null)
        {
            volume.profile = Instantiate(volume.sharedProfile);
        }

        volume.profile.TryGet(out vignette);

        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 Play 모드를 끄는 순간, 그 직전 프레임의 intensity가 Game 뷰에 정지 화면처럼
    /// 남아 보이는 것을 막기 위해 종료가 시작되는 즉시 0으로 되돌린다.
    /// (빌드에는 Play 모드 개념이 없어 이 문제 자체가 발생하지 않는다)
    /// </summary>
    private void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode && vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }
#endif

    public void SetDangerRatio(float dangerRatio)
    {
        if (vignette == null)
        {
            return;
        }

        vignette.intensity.value = Mathf.Clamp01(dangerRatio) * maxIntensity;
    }
}
