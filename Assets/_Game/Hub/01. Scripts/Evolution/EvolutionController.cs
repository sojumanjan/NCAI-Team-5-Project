using DG.Tweening;
using UnityEngine;

/// <summary>
/// 미니게임 클리어마다 재생되는 캐릭터 진화 연출 재생기.
/// 캐릭터 자체(스프라이트, 진화 단계)는 CharacterEvolutionState가 들고 있고,
/// 이 컨트롤러는 PlayEvolution에 넘겨받은 캐릭터를 대상으로 줌/화이트아웃/흔들림/스케일 팝/별 흩뿌리기만 재생한다.
///
/// 캐릭터(CharacterRoot)와 맵 배경(zoomTarget, 예: MainRoot)은 서로 다른 사람이 독립적으로 관리하는
/// 별개의 좌표계이므로(부모-자식 관계로 얽어두지 않는다). 카메라가 캐릭터를 따라가며 줌인하는 것과
/// 같은 느낌을 내기 위해, 맵/캐릭터의 확대와 "캐릭터가 있던 지점이 화면 중앙에 오도록 이동"을
/// 하나의 진행률(0~1)로 매 프레임 동시에 반영한다. 확대와 이동을 서로 다른 트윈으로 따로 돌리면
/// 속도가 미묘하게 어긋나 아직 덜 커진 가장자리에 빈 공간이 보일 수 있어서, 반드시 같은 t값으로
/// 스케일과 위치를 함께 보간한다.
///
/// 별 흩뿌리기는 화면 중앙 고정 위치(StarBurstRoot의 원래 자리)에서 재생한다.
/// </summary>
public class EvolutionController : MonoBehaviour
    
{
    public static EvolutionController Instance;
    [Header("참조")]
    [Tooltip("줌인 시 확대할 맵/배경 UI. 화면 전체를 덮는 stretch RectTransform(예: MainRoot)을 연결한다. " +
        "캐릭터와는 별개의 오브젝트이므로, 연출마다 캐릭터의 현재 화면 위치를 계산해 그 지점이 " +
        "화면 중앙에 오도록 맵을 확대하면서 동시에 이동시킨다.")]
    [SerializeField] private RectTransform zoomTarget;
    [Tooltip("별 흩뿌리기. 원래 배치된 자리(화면 중앙)에서 그대로 재생한다.")]
    [SerializeField] private UIStarBurst starBurst;
    [Tooltip("켜면 별이 새 모습으로 바뀌는 순간(플래시·소리와 같은 프레임)에 터진다. " +
        "끄면 예전처럼 스케일 팝이 끝난 뒤에 터진다.")]
    [SerializeField] private bool starBurstOnEvolve = true;
    [Tooltip("빛 모으기·플래시·충격파 고리. 비워두면 이펙트 없이 진행한다.")]
    [SerializeField] private EvolutionEffects effects;
    [Tooltip("흔들림이 끝나고 새 모습으로 바뀌는 순간 나는 소리.")]
    [SerializeField] private SoundData evolutionSound;

    [Header("화면 줌")]
    [SerializeField] private float zoomedScale = 1.15f;
    [Tooltip("맵/캐릭터를 확대하면서 동시에 캐릭터 위치를 화면 중앙으로 이동시키는 총 시간. " +
        "확대와 이동을 같은 진행률로 동시에 진행하므로 빈 공간이 보이지 않는다.")]
    [SerializeField] private float zoomInDuration = 0.6f;
    [Tooltip("연출이 끝나고 원래 크기/위치로 되돌아갈 때 걸리는 시간(줌인의 역순, 동시 진행).")]
    [SerializeField] private float zoomOutDuration = 0.5f;
    [Tooltip("별 흩뿌리기가 끝난 뒤, 원래 크기/위치로 되돌아가기 시작하기까지의 대기 시간.")]
    [SerializeField] private float zoomOutDelayAfterBurst = 0.5f;

    [Header("화이트아웃")]
    [SerializeField] private float fadeToWhiteDuration = 1f;
    [SerializeField] private Color evolutionFlashColor = Color.white;

    [Header("흔들림 유지")]
    [SerializeField] private float shakeHoldDuration = 2f;
    [SerializeField] private float shakeStrength = 15f;
    [SerializeField] private int shakeVibrato = 20;

    [Header("스케일 팝")]
    [SerializeField] private float popScaleMultiplier = 1.4f;
    [SerializeField] private float popDuration = 0.3f;

    [Header("표정")]
    [Tooltip("줌아웃이 끝난 뒤 신난 표정을 더 유지하는 시간(초). 끝나자마자 바꾸면 웃다가 뚝 멈춘 것처럼 보인다.")]
    [SerializeField] private float happyHoldAfter = 0.5f;

    private Vector3 defaultZoomScale;
    private Vector2 defaultZoomAnchoredPosition;
    private bool isPlaying;

    // 캐릭터를 따로 움직이는 스크립트(SeedWanderer 등)가 연출과 싸우지 않도록 비켜설 때 본다.
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (zoomTarget != null)
        {
            defaultZoomScale = zoomTarget.localScale;
            defaultZoomAnchoredPosition = zoomTarget.anchoredPosition;
        }
    }

    /// <summary>
    /// 미니게임 클리어 시 외부(GameFlow 등)에서, 진화시킬 캐릭터를 넘겨 호출한다.
    /// 캐릭터가 이미 마지막 단계라면(CanEvolve == false) 아무 동작도 하지 않는다.
    /// 이미 연출이 재생 중일 때 다시 호출되면(예: 버튼 연타) 무시한다.
    /// </summary>
    public void PlayEvolution(CharacterEvolutionState character)
    {
        if (character == null || !character.CanEvolve || isPlaying)
        {
            return;
        }

        isPlaying = true;

        // 돌아다니다 >< 표정을 짓던 중일 수 있다. 흰 덮개는 기본 얼굴 실루엣이라 얼굴이 다르면 삐져나온다.
        character.ShowNormal();

        RectTransform characterRect = character.RectTransform;
        Vector2 originalCharacterPosition = characterRect.anchoredPosition;
        Vector3 originalCharacterScale = characterRect.localScale;

        // 캐릭터와 zoomTarget은 서로 다른 부모를 가진 별개 좌표계이므로,
        // 캐릭터의 화면(스크린) 좌표를 기준으로 맵의 로컬 좌표를 계산해 맞춘다.
        Vector2 characterScreenPoint = RectTransformUtility.WorldToScreenPoint(null, characterRect.position);
        Vector2 zoomTargetLocalPointAtScale1 = Vector2.zero;
        if (zoomTarget != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zoomTarget, characterScreenPoint, null, out zoomTargetLocalPointAtScale1);
        }

        var whiteFlashOverlay = character.WhiteFlashOverlay;
        if (whiteFlashOverlay != null)
        {
            // 광원이 들어간 실루엣은 캔버스가 원본보다 커서, 덮개 사각형도 그만큼 맞춰 키운다.
            character.PrepareWhiteOverlay();
            SetOverlayAlpha(whiteFlashOverlay, 0f);
        }

        Sequence sequence = DOTween.Sequence();

        // 1단계: 맵과 캐릭터를 "동시에, 같은 진행률로" 확대 + 캐릭터 위치를 화면 중앙으로 이동.
        // 확대와 이동이 각각 독립된 트윈으로 따로 진행되면 속도가 미묘하게 어긋나 빈 공간이
        // 보일 수 있으므로, 하나의 t(0~1) 트윈에서 매 프레임 둘 다 함께 갱신한다.
        sequence.Append(CreateZoomInTween(characterRect, originalCharacterPosition, originalCharacterScale, zoomTargetLocalPointAtScale1));

        // 빛이 모여드는 건 하얘지는 시간과 떨리는 시간 전체에 걸친다. 마지막 알갱이가 닿는 순간이 곧 진화 순간이다.
        RectTransform effectTarget = character.CharacterImage != null ? character.CharacterImage.rectTransform : characterRect;
        sequence.AppendCallback(() =>
        {
            if (effects != null)
            {
                effects.PlayGather(effectTarget, fadeToWhiteDuration + shakeHoldDuration);
            }
        });

        // 흰색 오버레이를 캐릭터 위로 서서히 덮는다 (Image.color 곱셈 틴트로는
        // 유색 스프라이트가 흰색으로 안 바뀌므로, 별도 오버레이의 알파를 올리는 방식으로 구현).
        if (whiteFlashOverlay != null)
        {
            sequence.Append(whiteFlashOverlay.DOFade(1f, fadeToWhiteDuration));
        }
        else
        {
            sequence.Append(character.CharacterImage.DOColor(evolutionFlashColor, fadeToWhiteDuration));
        }

        // 흰색 유지 + 바들바들 떨림 (캐릭터 자신을 흔들면 오버레이도 자식이라 함께 움직인다).
        sequence.AppendCallback(() =>
        {
            characterRect.DOShakeAnchorPos(
                shakeHoldDuration, shakeStrength, shakeVibrato, 90f, false, true);
        });
        sequence.AppendInterval(shakeHoldDuration);

        // 흔들림이 끝난 뒤 캐릭터를 화면 중앙(흔들림 시작 전 위치)으로 되돌리고,
        // 스프라이트를 교체하며 흰색 오버레이를 걷어낸다.
        sequence.AppendCallback(() =>
        {
            characterRect.anchoredPosition = Vector2.zero;

            if (effects != null)
            {
                effects.PlayBurst(effectTarget);
            }

            // 별은 플래시·고리·소리와 같은 프레임에 터뜨린다. 스케일 팝이 끝난 뒤에 터뜨리면 혼자 한 박자 늦는다.
            if (starBurstOnEvolve && starBurst != null)
            {
                starBurst.Play();
            }

            // 소리는 모습이 바뀌는 바로 그 프레임에. 흔들림 도중에 나면 무엇이 일어났는지 귀가 먼저 알아버린다.
            if (evolutionSound != null)
            {
                AudioManager.Play(evolutionSound);
            }

            character.AdvanceStage();

            // 흰빛이 걷히는 순간 이미 웃고 있어야 "자라서 기쁘다"로 읽힌다.
            character.ShowHappy();

            if (whiteFlashOverlay != null)
            {
                SetOverlayAlpha(whiteFlashOverlay, 0f);
            }
        });

        // 캐릭터가 줌인된 크기(originalScale * zoomedScale)에서 다시 스케일 팝을 하도록 기준을 잡는다.
        Vector3 zoomedCharacterScale = originalCharacterScale * zoomedScale;
        Tween popTween = characterRect
            .DOScale(zoomedCharacterScale * popScaleMultiplier, popDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo);
        sequence.Append(popTween);

        float burstDuration = starBurst != null ? starBurst.Duration : 0f;

        if (starBurstOnEvolve)
        {
            // 별은 팝과 함께 이미 퍼지고 있으니, 팝이 끝난 뒤엔 남은 만큼만 기다린다.
            sequence.AppendInterval(Mathf.Max(0f, burstDuration - popDuration));
        }
        else
        {
            sequence.AppendCallback(() =>
            {
                if (starBurst != null)
                {
                    starBurst.Play();
                }
            });
            sequence.AppendInterval(burstDuration);
        }

        // 별 흩뿌리기가 다 끝난 뒤에도 잠깐 여운을 두고 나서 원래 크기/위치로 돌아간다.
        sequence.AppendInterval(zoomOutDelayAfterBurst);

        // 2단계: 줌인의 역순으로, 맵/캐릭터를 원래 크기/위치로 동시에(같은 진행률로) 되돌린다.
        sequence.Append(CreateZoomOutTween(characterRect, originalCharacterPosition, originalCharacterScale, zoomTargetLocalPointAtScale1));

        // 표정 복귀까지를 연출로 친다. 그래야 씨앗이 웃는 얼굴로 다시 걸어 다니기 시작하지 않는다.
        sequence.AppendInterval(happyHoldAfter);
        sequence.AppendCallback(character.ShowNormal);

        sequence.OnComplete(() => isPlaying = false);
    }

    /// <summary>
    /// 진행률 t(0→1)에 따라 zoomTarget과 characterRect의 스케일/위치를 동시에 보간하는 트윈을 만든다.
    /// t=0: 원래 크기, 캐릭터 원래 위치. t=1: zoomedScale배 확대, 캐릭터가 화면 중앙에 위치.
    /// 스케일과 위치가 항상 같은 t로 함께 움직이므로 확대/이동 속도가 어긋나 생기는 빈 공간이 없다.
    /// </summary>
    private Tween CreateZoomInTween(RectTransform characterRect, Vector2 originalCharacterPosition,
        Vector3 originalCharacterScale, Vector2 zoomTargetLocalPointAtScale1)
    {
        return DOTween.To(() => 0f, t =>
        {
            ApplyZoomProgress(t, characterRect, originalCharacterPosition, originalCharacterScale, zoomTargetLocalPointAtScale1);
        }, 1f, zoomInDuration);
    }

    private Tween CreateZoomOutTween(RectTransform characterRect, Vector2 originalCharacterPosition,
        Vector3 originalCharacterScale, Vector2 zoomTargetLocalPointAtScale1)
    {
        return DOTween.To(() => 1f, t =>
        {
            ApplyZoomProgress(t, characterRect, originalCharacterPosition, originalCharacterScale, zoomTargetLocalPointAtScale1);
        }, 0f, zoomOutDuration);
    }

    private void ApplyZoomProgress(float t, RectTransform characterRect, Vector2 originalCharacterPosition,
        Vector3 originalCharacterScale, Vector2 zoomTargetLocalPointAtScale1)
    {
        float currentScaleMultiplier = Mathf.Lerp(1f, zoomedScale, t);

        characterRect.localScale = originalCharacterScale * currentScaleMultiplier;
        characterRect.anchoredPosition = Vector2.Lerp(originalCharacterPosition, Vector2.zero, t);

        if (zoomTarget == null)
        {
            return;
        }

        zoomTarget.localScale = defaultZoomScale * currentScaleMultiplier;

        // "캐릭터 지점이 배경 위에서 화면상 어디에 있는지"를 항상 캐릭터 자신의 이동과 똑같은 방식
        // (원래 위치 -> 화면 중앙(0)을 t로 선형 보간)으로 목표를 잡고, 그 목표를 만족하는
        // anchoredPosition을 역산한다. 캐릭터 지점의 화면상 위치는 (anchoredPosition + P * scale)
        // 이므로, 목표값에서 P * scale을 뺀 값이 필요한 anchoredPosition이다.
        // 캐릭터 쪽과 완전히 같은 Lerp(원래위치, 0, t) 형태를 쓰기 때문에, 두 좌표계의 해상도/스케일이
        // 달라도(Canvas Scaler 등) 항상 정확히 맞아떨어진다.
        Vector2 characterScreenOffsetAtScale1 = zoomTargetLocalPointAtScale1 + defaultZoomAnchoredPosition;
        Vector2 desiredCharacterScreenOffset = Vector2.Lerp(characterScreenOffsetAtScale1, Vector2.zero, t);
        Vector2 scaledLocalPoint = zoomTargetLocalPointAtScale1 * currentScaleMultiplier;
        zoomTarget.anchoredPosition = desiredCharacterScreenOffset - scaledLocalPoint;
    }

    private static void SetOverlayAlpha(UnityEngine.UI.Image overlay, float alpha)
    {
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
