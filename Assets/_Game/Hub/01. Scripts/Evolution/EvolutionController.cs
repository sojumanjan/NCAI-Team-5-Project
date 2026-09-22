using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니게임 클리어마다 재생되는 캐릭터 진화 연출.
/// 화면(줌 대상 UI) 확대 → 캐릭터 화이트아웃 → 흔들림 유지 → 스케일 팝+별 흩뿌리기와 함께
/// 다음 단계 스프라이트로 교체 → 화면 축소 순으로 진행한다.
/// 스프라이트는 evolutionStages 배열만 교체하면 실제 아트로 바로 대체할 수 있다.
/// Screen Space - Overlay Canvas는 카메라로 렌더링되지 않으므로, 줌인은 카메라 FOV가 아니라
/// 화면 전체를 감싸는 zoomTarget(RectTransform)의 스케일을 키우는 방식으로 구현한다.
/// </summary>
public class EvolutionController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("줌인 시 확대할 대상. 배경+캐릭터를 모두 포함하는 최상위 RectTransform(예: MainRoot)을 연결한다.")]
    [SerializeField] private RectTransform zoomTarget;
    [Tooltip("CharacterImage와 whiteFlashOverlay를 함께 감싸는 부모. 흔들림/스케일 팝을 이 RectTransform에" +
        " 적용해 캐릭터와 흰색 오버레이가 항상 같이 움직이게 한다(따로 흔들면 서로 어긋나 보인다).")]
    [SerializeField] private RectTransform characterRoot;
    [SerializeField] private Image characterImage;
    [Tooltip("캐릭터 Image와 같은 크기/위치에 겹쳐두는 흰색 Image. 알파를 올려 흰색이 덮이는 것처럼 보이게 한다. " +
        "sprite는 반드시 비워둔다(null) — Image.color는 스프라이트 텍스처에 곱셈(multiply)되므로, " +
        "캐릭터 스프라이트(유색)를 그대로 물려두면 흰색을 곱해도 원래 색이 그대로 나온다. " +
        "sprite가 없으면 Image가 내장 흰색 텍스처를 사용해 진짜 흰색으로 보인다.")]
    [SerializeField] private Image whiteFlashOverlay;
    [SerializeField] private UIStarBurst starBurst;

    [Header("진화 단계 스프라이트 (0번이 현재 시작 단계)")]
    [SerializeField] private Sprite[] evolutionStages;
    [Tooltip("evolutionStages와 같은 순서/개수. 각 단계 스프라이트의 실루엣을 흰색으로 채운 버전(알파는 원본과 동일). " +
        "비워두면 whiteFlashOverlay가 계속 사각형 모양으로 남는다.")]
    [SerializeField] private Sprite[] evolutionStagesWhite;

    [Header("화면 줌")]
    [SerializeField] private float zoomedScale = 1.15f;
    [SerializeField] private float zoomDuration = 0.5f;

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

    private int currentStageIndex;
    private Vector3 defaultZoomScale;
    private Vector2 originalCharacterRootPosition;
    private Vector3 originalCharacterRootScale;

    public int CurrentStageIndex => currentStageIndex;

    private void Awake()
    {
        if (zoomTarget != null)
        {
            defaultZoomScale = zoomTarget.localScale;
        }

        if (characterRoot != null)
        {
            originalCharacterRootPosition = characterRoot.anchoredPosition;
            originalCharacterRootScale = characterRoot.localScale;
        }

        if (characterImage != null && evolutionStages != null && evolutionStages.Length > 0)
        {
            characterImage.sprite = evolutionStages[0];
        }

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.sprite = GetWhiteSpriteForStage(0);
            SetOverlayAlpha(0f);
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color color = evolutionFlashColor;
        color.a = alpha;
        whiteFlashOverlay.color = color;
    }

    /// <summary>
    /// 미니게임 클리어 시 외부(GameFlow 등)에서 호출한다.
    /// 마지막 단계에 이미 도달했다면 아무 동작도 하지 않는다.
    /// </summary>
    public void PlayEvolution()
    {
        if (evolutionStages == null || currentStageIndex >= evolutionStages.Length - 1)
        {
            return;
        }

        Sequence sequence = DOTween.Sequence();

        // 화면 확대(줌인)는 다른 연출과 동시에 시작한다.
        if (zoomTarget != null)
        {
            sequence.Join(zoomTarget.DOScale(defaultZoomScale * zoomedScale, zoomDuration));
        }

        // 1단계: 흰색 오버레이를 캐릭터 위로 서서히 덮는다 (Image.color 곱셈 틴트로는
        // 유색 스프라이트가 흰색으로 안 바뀌므로, 별도 오버레이의 알파를 올리는 방식으로 구현).
        if (whiteFlashOverlay != null)
        {
            sequence.Append(whiteFlashOverlay.DOFade(1f, fadeToWhiteDuration));
        }
        else
        {
            sequence.Append(characterImage.DOColor(evolutionFlashColor, fadeToWhiteDuration));
        }

        // 2단계: 흰색 유지 + 바들바들 떨림 (캐릭터와 흰색 오버레이를 함께 감싸는 characterRoot를 흔들어야
        // 둘이 같이 움직여서 어긋나 보이지 않는다).
        sequence.AppendCallback(() =>
        {
            characterRoot.DOShakeAnchorPos(
                shakeHoldDuration, shakeStrength, shakeVibrato, 90f, false, true);
        });
        sequence.AppendInterval(shakeHoldDuration);

        // 3단계: 흔들림이 끝난 뒤 위치를 원래대로 되돌리고, 스프라이트를 교체하며 흰색 오버레이를 걷어낸다.
        sequence.AppendCallback(() =>
        {
            characterRoot.anchoredPosition = originalCharacterRootPosition;
            AdvanceStage();

            if (whiteFlashOverlay != null)
            {
                SetOverlayAlpha(0f);
            }
        });

        Tween popTween = characterRoot
            .DOScale(originalCharacterRootScale * popScaleMultiplier, popDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo);
        sequence.Append(popTween);

        sequence.AppendCallback(() =>
        {
            if (starBurst != null)
            {
                starBurst.Play();
            }
        });

        float burstDuration = starBurst != null ? starBurst.Duration : 0f;
        sequence.AppendInterval(burstDuration);

        // 4단계: 별 흩뿌리기가 끝나면 화면을 원래 크기로 되돌린다.
        sequence.AppendCallback(() =>
        {
            if (zoomTarget != null)
            {
                zoomTarget.DOScale(defaultZoomScale, zoomDuration);
            }
        });
    }

    private void AdvanceStage()
    {
        currentStageIndex++;
        characterImage.sprite = evolutionStages[currentStageIndex];

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.sprite = GetWhiteSpriteForStage(currentStageIndex);
        }
    }

    /// <summary>
    /// 해당 단계의 흰색 실루엣 스프라이트를 반환한다. evolutionStagesWhite가 비어있거나
    /// 개수가 안 맞으면(아직 흰색 버전을 안 만든 단계) null을 반환해 사각형 오버레이로 대체한다.
    /// </summary>
    private Sprite GetWhiteSpriteForStage(int stageIndex)
    {
        if (evolutionStagesWhite == null || stageIndex >= evolutionStagesWhite.Length)
        {
            return null;
        }

        return evolutionStagesWhite[stageIndex];
    }
}
