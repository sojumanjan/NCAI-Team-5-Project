using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 허브에 처음 들어왔을 때 씨앗이 잠에서 깨는 짧은 연출. 씬에 하나만 둔다.
///
/// 흐름: 잠 (숨 쉬듯 두 번 부풂) → 뽀잉 깨어남 → 두리번 (좌우 네 번) → 버엉 놀람(느낌표) → 비장한 얼굴
///       → 비장한 얼굴로 돌아다니며 상황 설명 툴팁 (한 장씩 클릭으로 넘김) → 다 읽으면 원래 얼굴
///
/// 이야기의 첫 장면이다. "오래 잠들어 있던 씨앗이 위험을 느끼고 깨어나, 엉망이 된 방을 보고 결심한다."
/// 글보다 씨앗의 얼굴 변화로 먼저 전하고, 툴팁은 그걸 말로 한 번 짚어준다.
///
/// 크기 변화는 전부 발밑을 붙잡고 한다. 가운데 기준으로 부풀면 씨앗이 공중에 뜬 것처럼 보인다.
/// </summary>
public class SeedIntro : MonoBehaviour
{
    [Header("재생")]
    [Tooltip("에디터 테스트용 스위치입니다. 끄면 에디터에서는 인트로 없이 바로 돌아다닙니다. " +
             "빌드에서는 이 값과 상관없이 항상 처음 허브에 들어올 때 한 번 틉니다.")]
    [SerializeField] private bool playIntro = true;

    [Header("참조")]
    [Tooltip("씨앗. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private CharacterEvolutionState character;

    [Tooltip("씨앗 돌아다니기. 인트로 동안 멈춰 둡니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private SeedWanderer wanderer;

    [Tooltip("이 페이드가 다 걷힌 뒤에 시작합니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private SceneFadeIn fade;

    [Header("얼굴")]
    [SerializeField] private Sprite sleepySprite;

    [Tooltip("깨어난 얼굴. 비워두면 지금 단계의 기본 얼굴.")]
    [SerializeField] private Sprite awakeSprite;

    [SerializeField] private Sprite surprisedSprite;
    [SerializeField] private Sprite angrySprite;

    [Header("느낌표")]
    [Tooltip("놀랄 때 씨앗 옆에 튀어나올 자리. 안에 Image 스프라이트를 넣으면 나타납니다. 비어 있으면 건너뜁니다.")]
    [SerializeField] private RectTransform exclamation;

    [Header("상황 설명 툴팁")]
    [Tooltip("툴팁 전체. 인트로 끝에 떠오릅니다. 평소엔 꺼둡니다.")]
    [SerializeField] private CanvasGroup storyRoot;

    [SerializeField] private TMP_Text storyLabel;

    [Tooltip("다 떠오르면 나타나는 '넘기기' 표시. 스프라이트가 비어 있으면 삼각형을 그려 넣습니다.")]
    [SerializeField] private Image storyIndicator;

    [Tooltip("한 칸이 한 장입니다. 클릭할 때마다 다음 장으로 넘어갑니다. 줄바꿈과 TMP 서식을 쓸 수 있습니다.")]
    [TextArea(3, 8)]
    [SerializeField] private string[] storyPages =
    {
        "오랜 잠에서 깨어난 씨앗.",
        "방이 엉망이 되어 있다.",
    };

    [Header("잠")]
    [Tooltip("자는 시간 (초). 이 동안 숨 쉬듯 부풀었다 가라앉습니다.")]
    [SerializeField] private float sleepDuration = 5f;

    [Tooltip("자는 동안 숨 쉬는 횟수.")]
    [SerializeField] private int breathCount = 2;

    [Tooltip("숨 들이쉴 때의 배율.")]
    [SerializeField] private float breathScale = 1.1f;

    [Tooltip("숨 들이쉬기 시작할 때마다 한 번씩 나는 코 고는 소리.")]
    [SerializeField] private SoundData sleepSound;

    [Header("깨어남 (뽀잉)")]
    [Tooltip("뽀잉 깨어나는 순간 나는 소리.")]
    [SerializeField] private SoundData wakeSound;

    [SerializeField] private Vector2 wakeStretch = new Vector2(0.9f, 1.15f);
    [SerializeField] private Vector2 wakeSquash = new Vector2(1.12f, 0.88f);
    [SerializeField] private float wakeHop = 18f;

    [Header("두리번")]
    [Tooltip("깨어나서 처음 고개를 돌릴 때까지 (초).")]
    [SerializeField] private float firstLookDelay = 0.7f;

    [Tooltip("그 뒤로 고개를 돌리는 간격 (초).")]
    [SerializeField] private float lookInterval = 0.3f;

    [Tooltip("좌우로 고개를 돌리는 횟수.")]
    [SerializeField] private int lookCount = 4;

    [Header("놀람")]
    [Tooltip("마지막으로 고개를 돌린 뒤 놀랄 때까지 (초).")]
    [SerializeField] private float surpriseDelay = 1f;

    [Tooltip("버엉 커질 때의 배율.")]
    [SerializeField] private float surprisePop = 1.3f;

    [Tooltip("놀란 얼굴을 유지하는 시간 (초).")]
    [SerializeField] private float surpriseHold = 2.5f;

    [Tooltip("놀라며 느낌표가 튀어나오는 순간 나는 소리.")]
    [SerializeField] private SoundData surpriseSound;

    [Header("비장함")]
    [Tooltip("비장한 얼굴을 유지한 뒤 툴팁이 뜰 때까지 (초).")]
    [SerializeField] private float angryHold = 2f;

    [Tooltip("비장한 얼굴로 바뀌는 순간 나는 소리.")]
    [SerializeField] private SoundData angrySound;

    [Header("툴팁")]
    [Tooltip("툴팁 창이 떠오르고 사라지는 시간 (초).")]
    [SerializeField] private float storyFadeIn = 0.5f;
    [SerializeField] private float storyFadeOut = 0.35f;
    [SerializeField] private float storyRise = 20f;

    [Tooltip("글 한 장이 떠오르는 시간 (초).")]
    [SerializeField] private float pageFadeIn = 0.6f;

    [Tooltip("글 한 장이 사라지는 시간 (초).")]
    [SerializeField] private float pageFadeOut = 0.3f;

    [Tooltip("글이 떠오르며 아래에서 올라오는 거리 (캔버스 픽셀).")]
    [SerializeField] private float pageRise = 16f;

    [Header("카메라")]
    [Tooltip("씨앗과 함께 확대할 방(보통 MainRoot). 비워두면 확대 없이 처음부터 방 전체가 보입니다.")]
    [SerializeField] private RectTransform cameraTarget;

    [Tooltip("시작할 때 씨앗 쪽으로 확대된 배율.")]
    [SerializeField] private float cameraZoom = 2.2f;

    [Tooltip("두리번거리기 시작(첫 고개 돌림)부터 아래 '남길 확대 배율'까지 물러나는 시간 (초).")]
    [SerializeField] private float cameraZoomOutDuration = 1f;

    [Tooltip("두리번 뒤에 남겨 둘 확대 배율. 1보다 조금 커야 놀랄 때 흔들려도 방 바깥이 보이지 않습니다.")]
    [SerializeField] private float cameraHoldZoom = 1.12f;

    [Tooltip("놀라는 순간부터 방이 꽉 차는 1배까지 마저 물러나는 시간 (초). 흔들리면서 함께 물러납니다.")]
    [SerializeField] private float cameraFinalZoomOutDuration = 0.8f;

    [SerializeField] private Ease cameraZoomOutEase = Ease.InOutCubic;

    [Tooltip("놀라는 순간 카메라가 흔들리는 시간 (초). 0이면 흔들리지 않습니다.")]
    [SerializeField] private float surpriseShakeDuration = 0.35f;

    [Tooltip("흔들리는 세기 (캔버스 픽셀).")]
    [SerializeField] private float surpriseShakeStrength = 12f;

    [Tooltip("흔들리는 잦기. 클수록 잘게 떱니다.")]
    [SerializeField] private int surpriseShakeVibrato = 20;

    private RectTransform _seed;
    private Vector3 _baseScale;
    private Vector2 _basePosition;
    private float _visualHeight;
    private float _facing = 1f;
    private GameObject _blocker;
    private Sprite _triangleSprite;
    private Vector2 _storyRest;
    private Vector2 _labelRest;
    private bool _playing;

    // 캔버스 하나라 진짜 카메라가 없다. 방과 씨앗을 같은 배율·같은 이동으로 움직여 카메라처럼 보이게 한다.
    // 씨앗은 인트로가 크기·자리를 직접 움직이므로, 그 결과(아래 _anim*) 위에 카메라를 한 번 더 씌운다.
    private float _cameraK;
    private Vector2 _cameraFocus;
    private Vector2 _mapBasePosition;
    private Vector3 _mapBaseScale;
    private Tween _cameraTween;
    private Tween _shakeTween;
    private Vector2 _shakeOffset;
    private float _animSx = 1f;
    private float _animSy = 1f;
    private float _animLift;

    // 미니게임에서 돌아오면 허브 씬이 새로 로드되어 인스펙터 값이 되살아난다. 씬을 넘어 기억하려고 static으로 둔다.
    private static bool _played;

    /// <summary>인트로가 도는 중인지. 클리어 복귀 연출과 표정 타이머가 비켜설 때 본다.</summary>
    public bool IsPlaying => _playing;

    // 도메인 리로드를 꺼 두면 static이 플레이 사이에 남는다. 플레이마다 처음엔 다시 보이도록 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => _played = false;

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

        if (fade == null)
        {
            fade = FindAnyObjectByType<SceneFadeIn>();
        }

        if (storyRoot != null)
        {
            _storyRest = ((RectTransform)storyRoot.transform).anchoredPosition;
            storyRoot.gameObject.SetActive(false);
        }

        if (storyLabel != null)
        {
            _labelRest = storyLabel.rectTransform.anchoredPosition;
        }

        if (storyIndicator != null && storyIndicator.sprite == null)
        {
            _triangleSprite = UIProceduralSprite.TriangleDown();
            storyIndicator.sprite = _triangleSprite;
        }

        if (exclamation != null)
        {
            exclamation.gameObject.SetActive(false);
        }

        // 다른 연출이 Start에서 인트로 여부를 물어볼 수 있어, 재생할 거라면 Awake에서 미리 켜 둔다.
#if UNITY_EDITOR
        bool wantIntro = playIntro;
#else
        // 테스트하느라 스위치를 꺼 둔 채 빌드해도 실제 플레이어는 반드시 첫 장면을 봐야 한다.
        bool wantIntro = true;
#endif
        _playing = wantIntro && !_played && character != null;

        if (_playing)
        {
            _played = true;
        }

        if (_playing && wanderer != null)
        {
            wanderer.enabled = false;
        }
    }

    private void Start()
    {
        if (!_playing)
        {
            return;
        }

        // 첫 화면부터 자고 있어야 한다. Awake에서 바꾸면 씨앗 쪽 Awake가 첫 단계 얼굴로 덮어쓸 수 있어서,
        // 모든 Awake가 끝나고 첫 화면이 그려지기 전인 Start에서 바꾼다.
        character.ShowSprite(sleepySprite);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _seed = character.RectTransform;
        _baseScale = _seed.localScale;
        _basePosition = _seed.anchoredPosition;
        _facing = Mathf.Sign(_baseScale.x);
        _baseScale.x = Mathf.Abs(_baseScale.x);
        _visualHeight = MeasureHeight();

        // 페이드가 걷히기 전, 첫 화면부터 씨앗 쪽으로 당겨져 있어야 한다. Start에서 첫 yield 전까지는 그리기 전이다.
        BeginCameraZoomedIn();

        _blocker = ScreenInputBlocker.Create(transform, "IntroBlocker");
        _blocker.SetActive(true);

        if (fade != null)
        {
            // 편집하다 페이드 캔버스를 꺼둔 채 플레이하면 페이드가 아예 안 돈다. 그때 기다리면 영원히 멈춘다.
            yield return new WaitUntil(() => fade == null || fade.IsDone || !fade.isActiveAndEnabled);
        }

        yield return Sleep();
        yield return Wake();
        yield return LookAround();
        yield return Surprise();
        yield return Resolve();

        // 툴팁을 읽는 동안에도 방은 살아 있어야 한다. 씨앗은 비장한 얼굴 그대로 먼저 돌아다니기 시작한다.
        ReleaseSeed();
        yield return ShowStory();

        Finish();
    }

    // ---------------------------------------------------------------- 1. 잠

    private IEnumerator Sleep()
    {
        float half = sleepDuration / Mathf.Max(1, breathCount) * 0.5f;
        Sequence breathing = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < breathCount; i++)
        {
            // 코 고는 소리는 부풀기 시작하는 순간에. 소리와 몸이 같이 부풀어야 숨소리로 들린다.
            breathing.AppendCallback(() =>
            {
                if (sleepSound != null)
                {
                    AudioManager.Play(sleepSound);
                }
            });

            // 들숨과 날숨이 느리게 이어져야 잠든 것처럼 보인다. 딱딱 끊기면 맥박처럼 보인다.
            breathing.Append(DOVirtual.Float(1f, breathScale, half, s => Apply(s, s, 0f)).SetEase(Ease.InOutSine));
            breathing.Append(DOVirtual.Float(breathScale, 1f, half, s => Apply(s, s, 0f)).SetEase(Ease.InOutSine));
        }

        yield return breathing.WaitForCompletion();
    }

    // ---------------------------------------------------------------- 2. 깨어남 · 두리번

    private IEnumerator Wake()
    {
        if (awakeSprite != null)
        {
            character.ShowSprite(awakeSprite);
        }
        else
        {
            character.ShowNormal();
        }

        if (wakeSound != null)
        {
            AudioManager.Play(wakeSound);
        }

        yield return Boing(wakeStretch, wakeSquash, wakeHop).WaitForCompletion();
    }

    private IEnumerator LookAround()
    {
        // 두리번을 0번으로 두면 물러날 계기가 없어 놀라는 내내 확대된 채 남는다.
        if (lookCount <= 0)
        {
            StartCameraZoomOut(HoldK, cameraZoomOutDuration);
        }

        for (int i = 0; i < lookCount; i++)
        {
            yield return new WaitForSeconds(i == 0 ? firstLookDelay : lookInterval);

            // 고개만 휙 돌린다. 트윈 없이 한 프레임에 뒤집어야 "두리번"이다.
            _facing = -_facing;
            Apply(1f, 1f, 0f);

            // 첫 고개 돌림과 함께 카메라가 물러난다. 다 풀지 않고 살짝 남겨 둔다 — 곧 놀라며 흔들릴 여유다.
            if (i == 0)
            {
                StartCameraZoomOut(HoldK, cameraZoomOutDuration);
            }
        }
    }

    // ---------------------------------------------------------------- 3. 놀람

    private IEnumerator Surprise()
    {
        yield return new WaitForSeconds(surpriseDelay);

        character.ShowSprite(surprisedSprite);
        ShowExclamation();
        ShakeCamera();

        // 느낌표와 같은 프레임에. 느낌표 그림이 아직 없어도 놀라는 순간은 소리로 짚어준다.
        if (surpriseSound != null)
        {
            AudioManager.Play(surpriseSound);
        }

        yield return DOTween.Sequence()
                            .Append(DOVirtual.Float(1f, surprisePop, 0.12f, s => Apply(s, s, 0f)).SetEase(Ease.OutQuad))
                            .Append(DOVirtual.Float(surprisePop, 1f, 0.35f, s => Apply(s, s, 0f)).SetEase(Ease.OutBack))
                            .SetLink(gameObject)
                            .WaitForCompletion();

        yield return new WaitForSeconds(Mathf.Max(0f, surpriseHold - 0.47f));
    }

    private void ShowExclamation()
    {
        if (exclamation == null || !HasVisual(exclamation))
        {
            return;
        }

        exclamation.gameObject.SetActive(true);
        exclamation.localScale = Vector3.zero;
        exclamation.localRotation = Quaternion.Euler(0f, 0f, -20f);

        DOTween.Sequence()
               .Append(exclamation.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
               .Join(exclamation.DOLocalRotate(Vector3.zero, 0.25f).SetEase(Ease.OutBack))
               .SetLink(exclamation.gameObject);
    }

    private void HideExclamation()
    {
        if (exclamation == null || !exclamation.gameObject.activeSelf)
        {
            return;
        }

        exclamation.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetLink(exclamation.gameObject)
                   .OnComplete(() => exclamation.gameObject.SetActive(false));
    }

    /// <summary>느낌표 자리에 실제 그림이 들어 있는지. 스프라이트 없는 Image는 흰 네모로 보여서 띄우면 안 된다.</summary>
    private static bool HasVisual(RectTransform slot)
    {
        foreach (Graphic graphic in slot.GetComponentsInChildren<Graphic>(true))
        {
            if (!(graphic is Image image) || image.sprite != null)
            {
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------- 4. 비장함

    /// <summary>
    /// 움찔 웅크렸다가 결연하게 일어선다. 얼굴은 가장 웅크린 순간에 바꾼다 — 그래야 "마음먹고 일어선다"로 읽힌다.
    /// </summary>
    private IEnumerator Resolve()
    {
        HideExclamation();

        yield return DOTween.Sequence()
                            .Append(DOVirtual.Float(0f, 1f, 0.15f, t => Apply(Mathf.Lerp(1f, 1.06f, t), Mathf.Lerp(1f, 0.93f, t), 0f)).SetEase(Ease.OutQuad))
                            .AppendCallback(() =>
                            {
                                character.ShowSprite(angrySprite);

                                // 얼굴이 바뀌는 바로 그 프레임에. 소리가 얼굴보다 먼저면 무엇에 놀랐는지 헷갈린다.
                                if (angrySound != null)
                                {
                                    AudioManager.Play(angrySound);
                                }
                            })
                            .Append(DOVirtual.Float(0f, 1f, 0.18f, t => Apply(Mathf.Lerp(1.06f, 0.97f, t), Mathf.Lerp(0.93f, 1.06f, t), 0f)).SetEase(Ease.OutQuad))
                            .Append(DOVirtual.Float(0f, 1f, 0.35f, t => Apply(Mathf.LerpUnclamped(0.97f, 1f, t), Mathf.LerpUnclamped(1.06f, 1f, t), 0f)).SetEase(Ease.OutBack))
                            .SetLink(gameObject)
                            .WaitForCompletion();

        yield return new WaitForSeconds(angryHold);
    }

    // ---------------------------------------------------------------- 5. 툴팁

    /// <summary>
    /// 툴팁 창이 떠오르고, 글이 한 장씩 넘어간다. 다 넘기면 창이 가라앉는다.
    /// 읽는 동안 씨앗은 비장한 얼굴로 돌아다니고, 방 클릭은 막힌다 — 넘기려는 클릭이 사물까지 눌러 씬을 떠나면 안 된다.
    /// </summary>
    private IEnumerator ShowStory()
    {
        if (storyRoot == null)
        {
            yield break;
        }

        var rect = (RectTransform)storyRoot.transform;
        PagedText.Prepare(storyLabel, storyIndicator, _labelRest);

        rect.anchoredPosition = _storyRest - new Vector2(0f, storyRise);
        storyRoot.alpha = 0f;
        storyRoot.gameObject.SetActive(true);

        yield return DOTween.Sequence()
                            .Append(storyRoot.DOFade(1f, storyFadeIn).SetEase(Ease.OutQuad))
                            .Join(rect.DOAnchorPos(_storyRest, storyFadeIn).SetEase(Ease.OutCubic))
                            .SetLink(storyRoot.gameObject)
                            .WaitForCompletion();

        yield return PagedText.Play(storyLabel, storyIndicator, storyPages, _labelRest, pageFadeIn, pageFadeOut, pageRise);

        yield return storyRoot.DOFade(0f, storyFadeOut).SetEase(Ease.InQuad).SetLink(storyRoot.gameObject).WaitForCompletion();
        storyRoot.gameObject.SetActive(false);
        rect.anchoredPosition = _storyRest;
    }

    // ---------------------------------------------------------------- 마무리

    /// <summary>
    /// 인트로가 만진 크기와 자리를 정확히 되돌리고 돌아다니기를 켠다. 얼굴은 건드리지 않는다 —
    /// 툴팁을 다 읽을 때까지 비장한 얼굴을 유지해야 해서다. 돌아다니기는 얼굴을 바꾸지 않고,
    /// 가끔 짓는 >< 표정은 인트로가 끝날 때까지 비켜서 있으므로 그 사이 얼굴이 덮이지 않는다.
    /// </summary>
    private void ReleaseSeed()
    {
        // 돌아다니기 전에 카메라를 확실히 제자리로. 물러나는 도중이면 방이 확대된 채 굳는다.
        EndCamera();

        _seed.localScale = new Vector3(_baseScale.x * _facing, _baseScale.y, _baseScale.z);
        _seed.anchoredPosition = _basePosition;

        if (wanderer != null)
        {
            wanderer.enabled = true;
        }
    }

    private void Finish()
    {
        // 이야기를 다 읽은 뒤에야 원래 얼굴로 돌아온다. 결심한 얼굴로 상황을 듣고, 다 듣고 나서야 풀린다.
        character.ShowNormal();

        // 막은 끝까지 유지한다. 글을 넘기려는 클릭이 사물까지 눌러 씬을 떠나면 안 된다.
        if (_blocker != null)
        {
            Destroy(_blocker);
        }

        _playing = false;
    }

    // ---------------------------------------------------------------- 공통

    /// <summary>뽀잉: 곧장 늘어나며 튀어 오르고 → 내려앉으며 눌렸다가 → 출렁이며 자리 잡는다.</summary>
    private Sequence Boing(Vector2 stretch, Vector2 squash, float hop)
    {
        return DOTween.Sequence()
                      .Append(DOVirtual.Float(0f, 1f, 0.1f, t => Apply(Mathf.Lerp(1f, stretch.x, t), Mathf.Lerp(1f, stretch.y, t), hop * t)).SetEase(Ease.OutCubic))
                      .Append(DOVirtual.Float(0f, 1f, 0.08f, t => Apply(Mathf.Lerp(stretch.x, squash.x, t), Mathf.Lerp(stretch.y, squash.y, t), hop * (1f - t))).SetEase(Ease.InQuad))
                      .Append(DOVirtual.Float(0f, 1f, 0.35f, t => Apply(Mathf.LerpUnclamped(squash.x, 1f, t), Mathf.LerpUnclamped(squash.y, 1f, t), 0f)).SetEase(Ease.OutBack))
                      .SetLink(gameObject);
    }

    /// <summary>
    /// 가로·세로 배율과 들어 올린 높이를 적용한다. 세로로 줄어든 만큼 내려서 발밑을 제자리에 둔다.
    /// 바라보는 방향(_facing)은 가로 부호로 따로 들고 있어서, 크기 트윈이 방향을 덮어쓰지 않는다.
    /// </summary>
    private void Apply(float sx, float sy, float lift)
    {
        _animSx = sx;
        _animSy = sy;
        _animLift = lift;
        ApplySeed();
    }

    /// <summary>씨앗의 인트로 움직임(_anim*)을 계산하고, 그 위에 지금 카메라(확대·이동)를 씌운다.</summary>
    private void ApplySeed()
    {
        float zoom = Mathf.Lerp(1f, cameraZoom, _cameraK);
        Vector2 offset = CameraOffset();

        Vector3 localScale = new Vector3(_baseScale.x * _animSx * _facing, _baseScale.y * _animSy, _baseScale.z);
        float keepBottom = -(1f - _animSy) * _visualHeight * 0.5f;
        Vector2 localPosition = _basePosition + new Vector2(0f, keepBottom + Mathf.Max(0f, _animLift));

        _seed.localScale = new Vector3(localScale.x * zoom, localScale.y * zoom, localScale.z);
        _seed.anchoredPosition = localPosition * zoom + offset;
    }

    // ---------------------------------------------------------------- 카메라

    /// <summary>
    /// 캔버스 위의 점 X가 화면에서 X × 배율 + 오프셋에 오게 한다. 완전히 당겨졌을 때(k=1) 씨앗 한가운데가 화면 중앙에 온다.
    /// 배율과 오프셋을 같은 k로 함께 움직여야 씨앗이 물러나는 내내 제자리에 붙어 있는 것처럼 보인다.
    /// </summary>
    private Vector2 CameraOffset()
    {
        Vector2 offset = -_cameraFocus * cameraZoom * _cameraK + _shakeOffset;
        if (cameraTarget == null)
        {
            return offset;
        }

        // 방이 화면을 항상 꽉 채우는 범위로 묶는다. 흔들림·확대값을 어떻게 바꿔도 방 바깥(흰 바탕)이 보이지 않는다.
        // 1배에 가까울수록 움직일 여유가 없어서 흔들림도 그만큼 약해진다.
        float zoom = Mathf.Lerp(1f, cameraZoom, _cameraK);
        Vector2 screenHalf = ((RectTransform)_seed.parent).rect.size * 0.5f;
        Vector2 mapHalf = Vector2.Scale(cameraTarget.rect.size * 0.5f, (Vector2)_mapBaseScale) * zoom;
        Vector2 limit = Vector2.Max(Vector2.zero, mapHalf - screenHalf);

        Vector2 center = _mapBasePosition * zoom + offset;
        center.x = Mathf.Clamp(center.x, -limit.x, limit.x);
        center.y = Mathf.Clamp(center.y, -limit.y, limit.y);
        return center - _mapBasePosition * zoom;
    }

    /// <summary>두리번 뒤에 남겨 둘 확대 배율을 카메라 진행도(0=방 전체, 1=처음 확대)로 바꾼 값.</summary>
    private float HoldK => Mathf.Clamp01((cameraHoldZoom - 1f) / Mathf.Max(0.0001f, cameraZoom - 1f));

    /// <summary>놀라는 순간 화면이 흠칫한다. 방과 씨앗을 같은 만큼 흔들어야 씨앗이 방 안에서 미끄러지지 않는다.</summary>
    private void ShakeCamera()
    {
        if (cameraTarget == null)
        {
            return;
        }

        // 흔들리면서 동시에 마저 물러난다. 흔들림이 끝나길 기다렸다 물러나면 두 동작이 끊겨 보인다.
        // 1배에 가까워질수록 흔들 여유가 줄어(CameraOffset의 제한) 흔들림도 줌과 함께 잦아든다.
        StartCameraZoomOut(0f, cameraFinalZoomOutDuration);

        if (surpriseShakeDuration <= 0f || surpriseShakeStrength <= 0f)
        {
            return;
        }

        _shakeTween?.Kill();
        _shakeTween = DOTween.Shake(() => (Vector3)_shakeOffset,
                                    v =>
                                    {
                                        _shakeOffset = v;
                                        ApplyCamera();
                                    },
                                    surpriseShakeDuration, surpriseShakeStrength, surpriseShakeVibrato, 90f, true, true)
                             .SetLink(gameObject)
                             .OnComplete(() =>
                             {
                                 _shakeOffset = Vector2.zero;
                                 ApplyCamera();
                             });
    }

    private void BeginCameraZoomedIn()
    {
        if (cameraTarget == null)
        {
            return;
        }

        _mapBasePosition = cameraTarget.anchoredPosition;
        _mapBaseScale = cameraTarget.localScale;

        // 씨앗 그림의 한가운데(발밑 기준점이 아니라). 씨앗은 캔버스 바로 아래에 있어 부모 좌표가 곧 캔버스 좌표다.
        RectTransform focus = character.CharacterImage != null ? character.CharacterImage.rectTransform : _seed;
        _cameraFocus = _seed.parent.InverseTransformPoint(focus.TransformPoint(focus.rect.center));

        _cameraK = 1f;
        ApplyCamera();
    }

    private void StartCameraZoomOut(float targetK, float duration)
    {
        if (cameraTarget == null || _cameraK <= targetK)
        {
            return;
        }

        _cameraTween?.Kill();
        _cameraTween = DOVirtual.Float(_cameraK, targetK, Mathf.Max(0.01f, duration), k =>
                                {
                                    _cameraK = k;
                                    ApplyCamera();
                                })
                                .SetEase(cameraZoomOutEase)
                                .SetLink(gameObject);
    }

    private void EndCamera()
    {
        _cameraTween?.Kill();
        _cameraTween = null;
        _shakeTween?.Kill();
        _shakeTween = null;
        _shakeOffset = Vector2.zero;

        if (cameraTarget == null)
        {
            return;
        }

        _cameraK = 0f;
        cameraTarget.anchoredPosition = _mapBasePosition;
        cameraTarget.localScale = _mapBaseScale;
    }

    private void ApplyCamera()
    {
        float zoom = Mathf.Lerp(1f, cameraZoom, _cameraK);
        cameraTarget.localScale = _mapBaseScale * zoom;
        cameraTarget.anchoredPosition = _mapBasePosition * zoom + CameraOffset();

        // 뽀잉이 끝난 뒤엔 씨앗 트윈이 멈춰 있어도 카메라는 계속 움직인다. 씨앗도 매번 같이 옮겨야 따라온다.
        ApplySeed();
    }

    /// <summary>씨앗 그림이 화면에서 차지하는 높이(부모 단위). 기준점 사각형이 아니라 실제 그림 크기로 재야 발밑이 맞는다.</summary>
    private float MeasureHeight()
    {
        Image image = character.CharacterImage;
        if (image == null)
        {
            return _seed.rect.height * _baseScale.y;
        }

        RectTransform rect = image.rectTransform;
        return rect.rect.height * Mathf.Abs(rect.localScale.y) * _baseScale.y;
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_triangleSprite);
    }

    [ContextMenu("인트로 다시 보기")]
    private void Replay()
    {
        if (!Application.isPlaying || _playing || character == null)
        {
            return;
        }

        if (storyRoot != null)
        {
            storyRoot.gameObject.SetActive(false);
        }

        _playing = true;
        character.ShowSprite(sleepySprite);

        if (wanderer != null)
        {
            wanderer.enabled = false;
        }

        // 페이드는 이미 끝났으니 기다리지 않는다.
        fade = null;
        StartCoroutine(Run());
    }

    private void OnValidate()
    {
        sleepDuration = Mathf.Max(0.1f, sleepDuration);
        breathCount = Mathf.Max(1, breathCount);
        breathScale = Mathf.Max(1f, breathScale);
        firstLookDelay = Mathf.Max(0f, firstLookDelay);
        lookInterval = Mathf.Max(0f, lookInterval);
        lookCount = Mathf.Max(0, lookCount);
        surpriseDelay = Mathf.Max(0f, surpriseDelay);
        surprisePop = Mathf.Max(1f, surprisePop);
        surpriseHold = Mathf.Max(0f, surpriseHold);
        angryHold = Mathf.Max(0f, angryHold);
        storyFadeIn = Mathf.Max(0.01f, storyFadeIn);
        storyFadeOut = Mathf.Max(0.01f, storyFadeOut);
        pageFadeIn = Mathf.Max(0.01f, pageFadeIn);
        pageFadeOut = Mathf.Max(0.01f, pageFadeOut);
        cameraZoom = Mathf.Max(1f, cameraZoom);
        cameraHoldZoom = Mathf.Clamp(cameraHoldZoom, 1f, cameraZoom);
        cameraZoomOutDuration = Mathf.Max(0.01f, cameraZoomOutDuration);
        cameraFinalZoomOutDuration = Mathf.Max(0.01f, cameraFinalZoomOutDuration);
        surpriseShakeDuration = Mathf.Max(0f, surpriseShakeDuration);
        surpriseShakeStrength = Mathf.Max(0f, surpriseShakeStrength);
        surpriseShakeVibrato = Mathf.Max(1, surpriseShakeVibrato);
    }
}
