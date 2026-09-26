using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터(씨앗) 오브젝트가 직접 들고 있는 진화 상태.
/// 몇 단계인지, 다음에 어떤 스프라이트로 바뀌어야 하는지를 캐릭터 스스로 알고 있어야
/// 세이브/로드나 다른 UI(상태창 등)에서 이 캐릭터만 보고 진행도를 참조할 수 있다.
/// 실제 줌/화이트아웃/흔들림 등 연출 재생은 EvolutionController(EvolutionRoot)가 담당하고,
/// 이 컴포넌트는 "지금 어떤 스프라이트인지"와 "다음 단계로 넘어가기"만 책임진다.
/// </summary>
public class CharacterEvolutionState : MonoBehaviour
{
    [Tooltip("characterImage와 whiteFlashOverlay를 함께 감싸는 부모. 진화 연출의 줌/흔들림/스케일 팝을 " +
        "이 RectTransform에 적용해야 캐릭터와 흰색 오버레이가 항상 같이 움직이고 커진다 " +
        "(characterImage 자신만 스케일하면 형제인 whiteFlashOverlay는 그대로 남는다).")]
    [SerializeField] private RectTransform selfRoot;
    [SerializeField] private Image characterImage;
    [Tooltip("캐릭터 Image와 같은 크기/위치에 겹쳐두는 흰색 Image. sprite는 비워둔다(null) — " +
        "Image.color는 스프라이트 텍스처에 곱셈되므로, sprite가 있으면 흰색이 그 색으로 물든다.")]
    [SerializeField] private Image whiteFlashOverlay;

    [Header("진화 단계 스프라이트 (0번이 현재 시작 단계)")]
    [SerializeField] private Sprite[] evolutionStages;
    [Tooltip("evolutionStages와 같은 순서/개수. 각 단계 스프라이트의 실루엣을 흰색으로 채운 버전(알파는 원본과 동일).")]
    [SerializeField] private Sprite[] evolutionStagesWhite;

    [Tooltip("evolutionStages와 같은 순서/개수. 각 단계의 신난 표정. 진화 직후 잠깐 보여준다. 비어 있는 단계는 표정을 바꾸지 않는다.")]
    [SerializeField] private Sprite[] happyStages;

    private int currentStageIndex;

    public int CurrentStageIndex => currentStageIndex;
    public bool CanEvolve => evolutionStages != null && currentStageIndex < evolutionStages.Length - 1;
    public Image CharacterImage => characterImage;
    public Image WhiteFlashOverlay => whiteFlashOverlay;
    public RectTransform RectTransform => selfRoot;

    private void Awake()
    {
        currentStageIndex = RestoredStage();

        if (characterImage != null && evolutionStages != null && evolutionStages.Length > 0)
        {
            characterImage.sprite = evolutionStages[currentStageIndex];
        }
    }

    /// <summary>
    /// 허브는 미니게임에서 돌아올 때마다 새로 로드되어 이 컴포넌트도 새로 생긴다. 단계를 여기에만 들고 있으면
    /// 매번 0단계로 돌아가, 몇 개를 깼든 클리어 연출 한 번(=1단계)만큼만 자란다.
    /// 그래서 씬을 넘어 남는 GameFlow의 깬 게임 수로 단계를 되살린다. 방금 처음 깨고 온 게임은 클리어 연출이
    /// 눈앞에서 한 단계 키우므로 그만큼 빼고 시작한다.
    /// </summary>
    private int RestoredStage()
    {
        GameFlow flow = GameFlow.Instance;
        if (flow == null || evolutionStages == null || evolutionStages.Length == 0)
        {
            return 0;
        }

        int grown = flow.ClearedCount - (flow.HasPendingNewClear ? 1 : 0);
        return Mathf.Clamp(grown, 0, evolutionStages.Length - 1);
    }

    /// <summary>다음 단계 스프라이트로 넘어간다. CanEvolve가 false면 아무 동작도 하지 않는다.</summary>
    public void AdvanceStage()
    {
        if (!CanEvolve)
        {
            return;
        }

        currentStageIndex++;
        characterImage.sprite = evolutionStages[currentStageIndex];
    }

    /// <summary>지금 단계의 신난 표정으로 바꾼다. 단계는 그대로이고 겉모습만 바뀐다.</summary>
    public void ShowHappy()
    {
        if (characterImage == null || happyStages == null || currentStageIndex >= happyStages.Length)
        {
            return;
        }

        Sprite happy = happyStages[currentStageIndex];
        if (happy != null)
        {
            characterImage.sprite = happy;
        }
    }

    /// <summary>
    /// 단계와 상관없는 특별한 표정(엔딩의 잠든 얼굴 등)을 잠깐 입힌다. 단계는 그대로라
    /// <see cref="ShowNormal"/>로 언제든 원래 얼굴로 돌아온다.
    /// </summary>
    public void ShowSprite(Sprite sprite)
    {
        if (characterImage != null && sprite != null)
        {
            characterImage.sprite = sprite;
        }
    }

    /// <summary>지금 단계의 기본 표정으로 되돌린다.</summary>
    public void ShowNormal()
    {
        if (characterImage != null && evolutionStages != null && currentStageIndex < evolutionStages.Length)
        {
            characterImage.sprite = evolutionStages[currentStageIndex];
        }
    }

    /// <summary>첫 단계로 되돌린다. 디버그 초기화용 — 진화 연출을 처음부터 다시 보려면 필요하다.</summary>
    public void ResetStage()
    {
        currentStageIndex = 0;

        if (characterImage != null && evolutionStages != null && evolutionStages.Length > 0)
        {
            characterImage.sprite = evolutionStages[0];
        }
    }

    /// <summary>해당 단계의 흰색 실루엣 스프라이트. 흰색 버전이 아직 없는 단계는 null(EvolutionController가 사각형으로 대체).</summary>
    public Sprite GetWhiteSpriteForStage(int stageIndex)
    {
        if (evolutionStagesWhite == null || stageIndex < 0 || stageIndex >= evolutionStagesWhite.Length)
        {
            return null;
        }

        return evolutionStagesWhite[stageIndex];
    }

    public Sprite CurrentWhiteSprite => GetWhiteSpriteForStage(currentStageIndex);

    /// <summary>
    /// 흰색 덮개에 지금 단계의 흰 실루엣을 입히고, 그 그림의 캔버스 크기에 맞춰 덮개 사각형을 키운다.
    ///
    /// 광원이 들어간 흰 실루엣은 번지는 빛이 잘리지 않게 원본(512)보다 큰 캔버스로 뽑힌다. 그걸 원본과
    /// 같은 사각형에 그리면 캔버스째 눌려 들어가 실루엣이 작아지고 찌그러진다. 캔버스가 큰 만큼 사각형도
    /// 키우면, 캔버스 중앙에 원본 영역을 맞춰 뽑은 그림은 원본과 정확히 겹친다.
    /// </summary>
    public void PrepareWhiteOverlay()
    {
        if (whiteFlashOverlay == null || characterImage == null)
        {
            return;
        }

        Sprite white = CurrentWhiteSprite;
        whiteFlashOverlay.sprite = white;

        RectTransform image = characterImage.rectTransform;
        RectTransform overlay = whiteFlashOverlay.rectTransform;

        overlay.anchorMin = image.anchorMin;
        overlay.anchorMax = image.anchorMax;
        overlay.pivot = image.pivot;
        overlay.anchoredPosition = image.anchoredPosition;
        overlay.localScale = image.localScale;

        Sprite baseSprite = evolutionStages != null && currentStageIndex < evolutionStages.Length ? evolutionStages[currentStageIndex] : null;
        Vector2 ratio = Vector2.one;
        if (white != null && baseSprite != null && baseSprite.rect.width > 0f && baseSprite.rect.height > 0f)
        {
            ratio = new Vector2(white.rect.width / baseSprite.rect.width, white.rect.height / baseSprite.rect.height);
        }

        // 앵커가 한 점이든 늘어나 있든 같은 식으로 된다: 늘어난 만큼(실제 크기 × (배율-1))만 더한다.
        overlay.sizeDelta = image.sizeDelta + Vector2.Scale(image.rect.size, ratio - Vector2.one);
    }
}
