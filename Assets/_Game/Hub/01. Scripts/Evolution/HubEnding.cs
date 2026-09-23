using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씨앗이 다 자란 뒤의 엔딩. 씬을 옮기지 않고 허브 위에 엔딩 캔버스를 덮었다가 걷는다.
/// 씬을 옮기지 않으니 클리어 기록과 씨앗 모습이 그대로 남고, 돌아와서 미니게임을 다시 할 수 있다.
///
/// 흐름
///  1. 여운    : 씨앗이 멈춰 눈을 감고, 꽃잎이 흩날리고, 방 음악이 잦아든다.
///  2. 전환    : 씨앗 쪽으로 다가가기 시작하고, 0.3초 뒤부터 흰빛이 따라 차오른다. 흰빛과 함께 엔딩 음악이 시작된다.
///  3. 크레딧  : 흰빛이 걷히면 엔딩 그림. 글이 한 장씩 떠오르고, 다 떠오르면 삼각형이 나와 클릭을 기다린다.
///  4. 복귀    : 검게 덮었다가 방으로. 씨앗은 다 자란 모습 그대로 다시 돌아다닌다.
///
/// 흰빛으로 들어가 검은빛으로 나오는 이유: 흰빛은 지금까지 "자라는 순간"에만 썼다. 엔딩은 그 끝이라
/// 흰빛으로 잇고, 돌아올 때는 꿈에서 깨어 일상으로 돌아오는 느낌으로 검게 닫는다.
/// </summary>
public class HubEnding : MonoBehaviour
{
    [Header("엔딩 화면")]
    [Tooltip("엔딩 그림과 글이 들어 있는 캔버스. 평소엔 꺼둡니다.")]
    [SerializeField] private GameObject endingCanvas;

    [Tooltip("엔딩 그림. 크레딧 내내 아주 천천히 다가옵니다.")]
    [SerializeField] private RectTransform endingImage;

    [Tooltip("한 장씩 떠오를 글.")]
    [SerializeField] private TMP_Text creditText;

    [Tooltip("다 떠오르면 나타나는 '넘기기' 표시. 스프라이트가 비어 있으면 삼각형을 그려 넣습니다.")]
    [SerializeField] private Image nextIndicator;

    [Tooltip("화면 전환용 덮개(흰색·검은색). 엔딩 캔버스보다 위에 있어야 합니다.")]
    [SerializeField] private Image transitionCover;

    [Tooltip("꽃잎. 여운부터 크레딧 끝까지 흩날립니다.")]
    [SerializeField] private PetalRain petals;

    [Header("크레딧 내용")]
    [Tooltip("한 칸이 한 장입니다. 줄바꿈과 TMP 서식(<size=60>, <b> 등)을 쓸 수 있습니다.")]
    [TextArea(3, 10)]
    [SerializeField] private string[] pages =
    {
        "씨앗이 꽃을 피웠습니다.",
        "플레이해주셔서 감사합니다.",
    };

    [Header("방 (여운·전환에서 다가갈 대상)")]
    [Tooltip("방 배경. 전환 때 씨앗 쪽으로 확대됩니다.")]
    [SerializeField] private RectTransform roomRoot;

    [Tooltip("씨앗. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private CharacterEvolutionState character;

    [Tooltip("씨앗 돌아다니기. 여운부터 복귀까지 멈춰 둡니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private SeedWanderer wanderer;

    [Header("음악")]
    [Tooltip("엔딩 음악. 흰빛이 차오르기 시작하는 순간 시작됩니다.")]
    [SerializeField] private SoundData endingBgm;

    [Tooltip("방으로 돌아올 때 다시 틀 방 음악.")]
    [SerializeField] private SoundData hubBgm;

    [Header("여운")]
    [Tooltip("다 자란 뒤 엔딩으로 넘어가기 전까지 머무는 시간 (초).")]
    [SerializeField] private float lingerDuration = 2f;

    [Tooltip("여운 동안 입힐 얼굴. 눈을 감은 얼굴이면 '긴 여정 끝에 잠든다'로 읽힌다. 비워두면 신난 얼굴.")]
    [SerializeField] private Sprite lingerSprite;

    [Header("전환")]
    [Tooltip("다가가기 시작한 뒤 흰빛이 차오르기 시작할 때까지 (초). 다가가는 건 흰빛이 다 찰 때까지 계속됩니다.")]
    [SerializeField] private float whiteStartDelay = 0.3f;

    [Tooltip("흰빛이 차오르는 시간 (초).")]
    [SerializeField] private float whiteInDuration = 1.5f;

    [Tooltip("흰빛이 걷히며 엔딩 그림이 드러나는 시간 (초).")]
    [SerializeField] private float whiteOutDuration = 1.5f;

    [Tooltip("흰빛이 다 찰 때까지 씨앗 쪽으로 다가가는 배율.")]
    [SerializeField] private float zoomScale = 1.9f;

    [SerializeField] private Color enterColor = Color.white;

    [Header("마지막 진화에서 곧장 이어받을 때")]
    [Tooltip("진화 연출이 확대된 채로 끝난 뒤, 눈을 감고 머무는 시간 (초).")]
    [SerializeField] private float heldLingerDuration = 1.5f;

    [Tooltip("이미 확대된 화면에서 흰빛이 다 찰 때까지 더 다가가는 배율. 이미 크게 당겨져 있어 평소보다 작게 둔다.")]
    [SerializeField] private float heldZoomScale = 1.5f;

    [Header("크레딧")]
    [Tooltip("글이 떠오르는 시간 (초).")]
    [SerializeField] private float textFadeIn = 0.9f;

    [Tooltip("글이 사라지는 시간 (초).")]
    [SerializeField] private float textFadeOut = 0.5f;

    [Tooltip("떠오르며 아래에서 올라오는 거리 (캔버스 픽셀).")]
    [SerializeField] private float textRise = 24f;

    [Tooltip("엔딩 그림이 크레딧 내내 다가오는 배율.")]
    [SerializeField] private float imageDrift = 1.06f;

    [Tooltip("엔딩 그림이 그만큼 다가오는 데 걸리는 시간 (초).")]
    [SerializeField] private float imageDriftDuration = 40f;

    [Header("복귀")]
    [SerializeField] private float blackInDuration = 1f;
    [SerializeField] private float blackOutDuration = 1f;
    [SerializeField] private Color exitColor = Color.black;

    private Sprite _triangleSprite;
    private Vector2 _textRestPosition;
    private bool _playing;

    /// <summary>엔딩이 도는 중인지.</summary>
    public bool IsPlaying => _playing;

    private void Awake()
    {
        if (character == null)
        {
            character = FindAnyObjectByType<CharacterEvolutionState>();
        }

        if (wanderer == null)
        {
            wanderer = FindAnyObjectByType<SeedWanderer>();
        }

        if (nextIndicator != null && nextIndicator.sprite == null)
        {
            _triangleSprite = UIProceduralSprite.TriangleDown();
            nextIndicator.sprite = _triangleSprite;
        }

        if (creditText != null)
        {
            _textRestPosition = creditText.rectTransform.anchoredPosition;
        }

        if (endingCanvas != null)
        {
            endingCanvas.SetActive(false);
        }

        if (transitionCover != null)
        {
            transitionCover.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_triangleSprite);
    }

    /// <summary>
    /// 엔딩을 볼 차례인지. 씨앗 그림 수가 아니라 깬 게임 수로 판단한다 — 아트 쪽에서 단계를
    /// 더 그려 넣어도 엔딩 조건이 어긋나지 않게.
    /// </summary>
    public static bool AllCleared()
    {
        GameFlow flow = GameFlow.Instance;
        return flow != null && flow.MiniGames.Count > 0 && flow.ClearedCount >= flow.MiniGames.Count;
    }

    /// <summary>
    /// 엔딩을 처음부터 끝까지 돌린다. 방으로 돌아와 화면이 걷힐 때 끝난다.
    ///
    /// 마지막 진화가 줌아웃 없이 확대된 채로 끝났으면 그 자리에서 곧장 이어받는다: 눈을 감고 잠깐 머문 뒤,
    /// 기다림 없이 더 다가가며 하얘진다. 한 번 물러났다 다시 다가가면 "다 자랐다 → 잠든다"의 흐름이 끊긴다.
    /// </summary>
    public IEnumerator Play()
    {
        EvolutionController evolution = EvolutionController.Instance;

        if (_playing || endingCanvas == null || transitionCover == null)
        {
            // 엔딩을 못 틀 때 확대된 채로 남겨두면 방이 계속 당겨진 채다.
            if (!_playing && evolution != null)
            {
                evolution.RestoreHeldZoom();
            }

            yield break;
        }

        _playing = true;

        bool fromHeldZoom = evolution != null && evolution.HasHeldZoom;

        yield return Linger(fromHeldZoom ? heldLingerDuration : lingerDuration);
        yield return EnterEnding(fromHeldZoom);
        yield return ShowCredits();
        yield return ReturnToRoom();

        _playing = false;
    }

    // ---------------------------------------------------------------- 1. 여운

    private IEnumerator Linger(float duration)
    {
        if (wanderer != null)
        {
            wanderer.enabled = false;
        }

        // 다 자란 씨앗이 조용히 눈을 감는다. 긴 여정이 끝났다는 걸 말 대신 얼굴로 전한다.
        if (character != null)
        {
            if (lingerSprite != null)
            {
                character.ShowSprite(lingerSprite);
            }
            else
            {
                character.ShowHappy();
            }
        }

        if (petals != null)
        {
            petals.StartRain();
        }

        // 여운이 끝날 때 방 음악이 다 사그라져 있어야, 엔딩 음악이 조용한 데서 시작한다.
        AudioManager.StopBGM(duration);

        yield return new WaitForSeconds(duration);
    }

    // ---------------------------------------------------------------- 2. 전환

    private IEnumerator EnterEnding(bool fromHeldZoom)
    {
        // 잠든 씨앗에게 다가가기 시작하고, 조금 늦게 흰빛이 따라 차오른다. 다가가는 건 흰빛이 다 찰 때까지
        // 멈추지 않는다 — 도중에 멈추면 흰빛 속에서 화면이 얼어붙은 것처럼 보인다.
        // 흰빛을 살짝 늦추는 건 "다가간다"를 먼저 눈에 담게 하려는 것. 동시에 시작하면 다가가는 게 흰빛에 묻힌다.
        // 이미 확대된 채로 이어받았으면 다가가는 건 충분히 봤으니 흰빛을 기다리지 않는다.
        ZoomState zoom = BeginZoom();
        ShowCover(enterColor, 0f);

        float whiteDelay = fromHeldZoom ? 0f : whiteStartDelay;
        float scale = fromHeldZoom ? heldZoomScale : zoomScale;
        float zoomDuration = whiteDelay + whiteInDuration;

        yield return DOTween.Sequence()
                            .Insert(0f, DOVirtual.Float(0f, 1f, zoomDuration, t => zoom.Apply(Mathf.Lerp(1f, scale, t))).SetEase(Ease.InQuad))

                            // 엔딩 음악은 흰빛이 차오르기 시작하는 순간에. 화면과 소리가 같은 박자에 넘어가야 한다.
                            .InsertCallback(whiteDelay, () => AudioManager.PlayBGM(endingBgm))
                            .Insert(whiteDelay, transitionCover.DOFade(1f, whiteInDuration).SetEase(Ease.InQuad))
                            .SetLink(gameObject)
                            .WaitForCompletion();

        // 하얀 화면 뒤에서 방을 원래대로 돌려놓는다. 돌아왔을 때 확대된 채면 어색하다.
        // 순서가 중요하다: 엔딩이 더 당긴 것을 먼저 풀고, 그다음 진화가 당겨둔 것을 푼다.
        zoom.Restore();

        if (fromHeldZoom && EvolutionController.Instance != null)
        {
            EvolutionController.Instance.RestoreHeldZoom();
        }

        endingCanvas.SetActive(true);
        PrepareText();

        if (endingImage != null)
        {
            endingImage.localScale = Vector3.one;
            endingImage.DOScale(imageDrift, imageDriftDuration).SetEase(Ease.Linear).SetLink(endingImage.gameObject);
        }

        yield return transitionCover.DOFade(0f, whiteOutDuration).SetEase(Ease.OutQuad).SetLink(gameObject).WaitForCompletion();
        transitionCover.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- 3. 크레딧

    private IEnumerator ShowCredits()
    {
        yield return PagedText.Play(creditText, nextIndicator, pages, _textRestPosition, textFadeIn, textFadeOut, textRise);
    }

    private void PrepareText() => PagedText.Prepare(creditText, nextIndicator, _textRestPosition);

    // ---------------------------------------------------------------- 4. 복귀

    private IEnumerator ReturnToRoom()
    {
        ShowCover(exitColor, 0f);
        AudioManager.StopBGM(blackInDuration);

        yield return transitionCover.DOFade(1f, blackInDuration).SetEase(Ease.InQuad).SetLink(gameObject).WaitForCompletion();

        if (endingImage != null)
        {
            endingImage.DOKill();
            endingImage.localScale = Vector3.one;
        }

        endingCanvas.SetActive(false);

        if (petals != null)
        {
            petals.Clear();
        }

        if (character != null)
        {
            character.ShowNormal();
        }

        if (wanderer != null)
        {
            wanderer.enabled = true;
        }

        AudioManager.PlayBGM(hubBgm);

        yield return transitionCover.DOFade(0f, blackOutDuration).SetEase(Ease.OutQuad).SetLink(gameObject).WaitForCompletion();
        transitionCover.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- 공통

    private void ShowCover(Color color, float alpha)
    {
        color.a = alpha;
        transitionCover.color = color;
        transitionCover.gameObject.SetActive(true);
    }

    /// <summary>
    /// 방 배경과 씨앗을 씨앗을 중심으로 함께 키운다. 둘은 같은 부모 아래 형제라서, 같은 점을 기준으로
    /// 같은 배율을 주면 카메라가 다가가는 것처럼 보인다.
    /// </summary>
    private ZoomState BeginZoom()
    {
        var state = new ZoomState();

        RectTransform seed = character != null ? character.RectTransform : null;
        if (seed == null)
        {
            return state;
        }

        RectTransform imageRect = character.CharacterImage != null ? character.CharacterImage.rectTransform : seed;
        var parent = seed.parent as RectTransform;
        if (parent == null)
        {
            return state;
        }

        state.Focus = parent.InverseTransformPoint(imageRect.position);
        state.Add(seed);

        if (roomRoot != null && roomRoot.parent == parent)
        {
            state.Add(roomRoot);
        }

        return state;
    }

    private class ZoomState
    {
        public Vector2 Focus;

        private readonly System.Collections.Generic.List<RectTransform> _targets = new();
        private readonly System.Collections.Generic.List<Vector3> _basePositions = new();
        private readonly System.Collections.Generic.List<Vector3> _baseScales = new();

        public void Add(RectTransform target)
        {
            _targets.Add(target);
            _basePositions.Add(target.localPosition);
            _baseScales.Add(target.localScale);
        }

        public void Apply(float scale)
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Vector2 from = _basePositions[i];
                Vector2 moved = Focus + (from - Focus) * scale;
                _targets[i].localPosition = new Vector3(moved.x, moved.y, _basePositions[i].z);
                _targets[i].localScale = _baseScales[i] * scale;
            }
        }

        public void Restore()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                _targets[i].localPosition = _basePositions[i];
                _targets[i].localScale = _baseScales[i];
            }
        }
    }

    private void OnValidate()
    {
        lingerDuration = Mathf.Max(0f, lingerDuration);
        whiteStartDelay = Mathf.Max(0f, whiteStartDelay);
        whiteInDuration = Mathf.Max(0.01f, whiteInDuration);
        whiteOutDuration = Mathf.Max(0.01f, whiteOutDuration);
        zoomScale = Mathf.Max(1f, zoomScale);
        heldLingerDuration = Mathf.Max(0f, heldLingerDuration);
        heldZoomScale = Mathf.Max(1f, heldZoomScale);
        textFadeIn = Mathf.Max(0.01f, textFadeIn);
        textFadeOut = Mathf.Max(0.01f, textFadeOut);
        imageDrift = Mathf.Max(1f, imageDrift);
        imageDriftDuration = Mathf.Max(0.01f, imageDriftDuration);
        blackInDuration = Mathf.Max(0.01f, blackInDuration);
        blackOutDuration = Mathf.Max(0.01f, blackOutDuration);
    }
}
