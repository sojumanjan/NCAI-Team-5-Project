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
        if (characterImage != null && evolutionStages != null && evolutionStages.Length > 0)
        {
            characterImage.sprite = evolutionStages[0];
        }
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
}
