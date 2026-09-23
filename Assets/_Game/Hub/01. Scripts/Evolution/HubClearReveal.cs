using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 미니게임을 처음 깨고 허브로 돌아왔을 때, 그 게임의 오브젝트가 먼지를 털어내듯 깨끗한 모습으로
/// 바뀌는 연출. 이어서 씨앗을 진화시킨다. 허브 씬에 하나만 둔다.
///
/// 흐름: 들썩 → 먼지 펑 + 그림 교체 + 뽀잉 + 반짝 (한 박자) → 먼지가 흩어짐 → 1초 뒤 씨앗 진화
///
/// 흰빛이 아니라 먼지인 이유: 흰빛은 바로 뒤 씨앗 진화가 쓴다. 같은 연출이 연달아 나오면 진화가
/// 묻힌다. 그리고 "잠든 사이 엉망이 된 집을 되찾는다"는 이야기엔 먼지가 털리는 그림이 맞다.
///
/// 변신과 진화를 한 코루틴에 묶은 이유: 둘 사이 간격과 입력 차단을 한 곳에서 쥐어야
/// "방이 깨끗해졌다 → 그래서 씨앗이 자랐다"는 인과가 끊기지 않는다.
/// </summary>
public class HubClearReveal : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워두면 Resources의 GameFlow를 씁니다.")]
    [SerializeField] private GameFlow flow;

    [Tooltip("이 페이드가 다 걷힌 뒤에 시작합니다. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private SceneFadeIn fade;

    [Header("시작")]
    [Tooltip("페이드가 걷힌 뒤 연출 시작까지 (초). 방을 한 번 둘러볼 틈.")]
    [SerializeField] private float startDelay = 0.3f;

    [Header("들썩")]
    [Tooltip("좌우로 들썩이는 최대 각도 (도).")]
    [SerializeField] private float wobbleAngle = 9f;

    [SerializeField] private float wobbleDuration = 0.7f;

    [Tooltip("들썩이는 동안 좌우로 오가는 횟수.")]
    [SerializeField] private int wobbleVibrato = 12;

    [Header("먼지 구름")]
    [Tooltip("물체를 덮는 구름 수. 적으면 틈으로 바꿔치기가 보입니다.")]
    [SerializeField] private int puffCount = 12;

    [Tooltip("구름 하나의 크기. 물체의 긴 변에 대한 비율.")]
    [SerializeField] private float puffSizeRatio = 0.5f;

    [Tooltip("구름이 흩어져 놓이는 범위. 물체 크기의 절반에 대한 비율.")]
    [SerializeField] private float puffSpread = 0.75f;

    [Tooltip("구름이 부풀어 물체를 덮는 시간 (초).")]
    [SerializeField] private float puffGrowDuration = 0.18f;

    [Tooltip("먼지가 바꿔치기보다 먼저 일어나는 시간 (초). 0이면 먼지·그림 교체·뽀잉이 동시에 터집니다.")]
    [SerializeField] private float puffLead = 0f;

    [Tooltip("그림이 바뀐 뒤 먼지가 흩어지기 시작할 때까지 (초). 0이면 튀어 오르는 것과 동시에 걷힙니다.")]
    [SerializeField] private float puffHold = 0f;

    [Tooltip("구름이 밖으로 흩어지며 사라지는 시간 (초).")]
    [SerializeField] private float puffFadeDuration = 0.6f;

    [Tooltip("흩어지며 밀려나는 거리. 구름 크기에 대한 비율.")]
    [SerializeField] private float puffDrift = 0.6f;

    [SerializeField] private Color puffColor = new Color(0.96f, 0.93f, 0.88f, 1f);

    [Header("통통")]
    [Tooltip("눌렸을 때의 가로·세로 배율.")]
    [SerializeField] private Vector2 squashScale = new Vector2(1.12f, 0.88f);

    [Tooltip("튀어오를 때의 가로·세로 배율.")]
    [SerializeField] private Vector2 stretchScale = new Vector2(0.92f, 1.1f);

    [Tooltip("튀어오르는 높이 (캔버스 픽셀).")]
    [SerializeField] private float hopHeight = 24f;

    [SerializeField] private float squashDuration = 0.08f;
    [SerializeField] private float stretchDuration = 0.1f;
    [SerializeField] private float settleDuration = 0.4f;

    [Header("반짝이")]
    [SerializeField] private int sparkleCount = 5;

    [Tooltip("반짝이 크기 범위 (캔버스 픽셀).")]
    [SerializeField] private Vector2 sparkleSize = new Vector2(30f, 52f);

    [Tooltip("반짝이가 튀어나가는 거리. 물체 긴 변에 대한 비율.")]
    [SerializeField] private Vector2 sparkleDistance = new Vector2(0.35f, 0.6f);

    [SerializeField] private float sparkleDuration = 0.55f;
    [SerializeField] private Color sparkleColor = new Color(1f, 0.95f, 0.7f, 1f);

    [Header("씨앗 진화")]
    [Tooltip("끄면 오브젝트 변신만 하고 끝납니다.")]
    [SerializeField] private bool evolveCharacter = true;

    [Tooltip("변신이 끝난 뒤 진화를 시작하기까지 (초).")]
    [SerializeField] private float evolutionDelay = 1f;

    [Tooltip("진화시킬 씨앗. 비워두면 씬에서 찾습니다.")]
    [SerializeField] private CharacterEvolutionState character;

    [Header("다음 연출")]
    [Tooltip("변신과 진화가 모두 끝났을 때.")]
    [SerializeField] private UnityEvent onFinished;

    [Header("미리보기")]
    [Tooltip("플레이 중 톱니바퀴 메뉴로 연출만 틀어볼 오브젝트.")]
    [SerializeField] private MiniGameEntry previewEntry;

    // 막 순서는 옵션 창(21)보다 아래, 방 UI보다 위. 옵션 창까지 막으면 연출 중 ESC 메뉴를 못 쓴다.
    private const int BLOCKER_SORTING_ORDER = 50;

    private readonly List<GameObject> _spawned = new();
    private GameObject _blocker;
    private Sprite _cloudSprite;
    private Sprite _sparkleSprite;
    private bool _running;

    /// <summary>연출이 도는 중인지. 씨앗이나 다른 연출이 끼어들지 않게 볼 때 쓴다.</summary>
    public bool IsPlaying => _running;

    private GameFlow Flow => flow != null ? flow : GameFlow.Instance;

    private void Awake()
    {
        if (fade == null)
        {
            fade = FindAnyObjectByType<SceneFadeIn>();
        }

        if (character == null)
        {
            character = FindAnyObjectByType<CharacterEvolutionState>();
        }

        _cloudSprite = UIProceduralSprite.Cloud();
        _sparkleSprite = UIProceduralSprite.Sparkle();
    }

    private void OnDestroy()
    {
        UIProceduralSprite.Release(_cloudSprite);
        UIProceduralSprite.Release(_sparkleSprite);
    }

    /// <summary>
    /// MiniGameEntry들은 OnEnable에서 이미 클리어 모습으로 바뀌어 있다. Start는 그 뒤, 첫 화면이
    /// 그려지기 전이라 여기서 되돌려야 바뀐 모습이 한 프레임도 비치지 않는다.
    /// </summary>
    private void Start() => PlayPending(true);

    /// <summary>
    /// GameFlow에 남아 있는 결과를 꺼내, 처음 깬 것이면 연출을 튼다. 튼 경우 true.
    /// 씬 시작과 디버그 키가 같은 길을 타야 디버그로 본 것이 실제와 같다.
    /// </summary>
    public bool PlayPending(bool waitForFade)
    {
        GameFlow current = Flow;
        if (current == null || !current.TryConsumeLastResult(out MiniGameDefinition game, out _, out bool newClear))
        {
            return false;
        }

        if (!newClear)
        {
            return false;
        }

        MiniGameEntry entry = FindEntry(game);
        if (entry == null)
        {
            Debug.LogWarning($"{nameof(HubClearReveal)}: '{game.DisplayName}'를 가리키는 MiniGameEntry가 씬에 없어 연출을 건너뜁니다.", this);
            return false;
        }

        Play(entry, waitForFade);
        return true;
    }

    /// <summary>이 오브젝트의 변신 연출을 튼다. 이미 도는 중이면 무시한다.</summary>
    public void Play(MiniGameEntry entry, bool waitForFade)
    {
        if (_running || entry == null)
        {
            return;
        }

        entry.ShowClearMark(false);
        StartCoroutine(Run(entry, waitForFade));
    }

    private IEnumerator Run(MiniGameEntry entry, bool waitForFade)
    {
        _running = true;
        SetBlocking(true);

        if (waitForFade && fade != null)
        {
            // 편집하다 페이드 캔버스를 꺼둔 채 플레이하면 페이드가 아예 안 돈다. 그때 기다리면 영원히 멈춘다.
            yield return new WaitUntil(() => fade == null || fade.IsDone || !fade.isActiveAndEnabled);
        }

        // 게임 시간으로 잰다. 연출 중 옵션 창을 열어 시간이 멈추면 연출도 같이 멈춰 있어야 한다.
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        var target = (RectTransform)entry.transform;
        Vector3 baseScale = target.localScale;
        Vector2 basePosition = target.anchoredPosition;
        Quaternion baseRotation = target.localRotation;

        Vector2 size = target.rect.size;
        float longSide = Mathf.Max(size.x, size.y);

        Sequence sequence = DOTween.Sequence().SetLink(entry.gameObject);

        // 1. 들썩: 뭔가 일어나려 한다는 예고.
        sequence.Append(target.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle), wobbleDuration, wobbleVibrato, 0.6f));

        // 2. 들썩임이 끝나는 순간 먼지·바꿔치기·뽀잉이 한꺼번에 터진다. 셋이 한 박자에 겹쳐야
        //    "펑 하고 깨끗해졌다"로 읽힌다. 먼지가 먼저 일면 한 발 늦게 바뀌는 것처럼 보인다.
        List<Image> puffs = SpawnPuffs(target, size, longSide);
        float swapAt = wobbleDuration;
        float growAt = Mathf.Max(0f, swapAt - puffLead);
        foreach (Image puff in puffs)
        {
            sequence.Insert(growAt, puff.rectTransform.DOScale(1f, puffGrowDuration).SetEase(Ease.OutBack));
            sequence.Insert(growAt, puff.DOFade(puffColor.a, puffGrowDuration * 0.4f));
        }

        // 3. 먼지 뒤에서 바꿔치기하고, 4. 그 순간 곧바로 튀어 오른다.
        //    한 박자라도 멈췄다 튀면 뒤늦게 놀란 것처럼 어색하다.
        sequence.InsertCallback(swapAt, () =>
        {
            target.localRotation = baseRotation;
            entry.ShowClearMark(true);
        });
        sequence.Insert(swapAt, CreateBounce(target, baseScale, basePosition, size));
        AddSparkles(sequence, swapAt, target, longSide);

        // 먼지는 다 부푼 뒤에 흩어진다. 부푸는 중에 흩어지기 시작하면 두 크기 변화가 서로 싸운다.
        float disperseStart = Mathf.Max(swapAt + puffHold, growAt + puffGrowDuration);
        foreach (Image puff in puffs)
        {
            RectTransform rect = puff.rectTransform;
            Vector2 outward = rect.anchoredPosition.sqrMagnitude > 1f ? rect.anchoredPosition.normalized : Random.insideUnitCircle.normalized;
            Vector2 drift = rect.anchoredPosition + outward * rect.sizeDelta.x * puffDrift;

            sequence.Insert(disperseStart, rect.DOAnchorPos(drift, puffFadeDuration).SetEase(Ease.OutQuad));
            sequence.Insert(disperseStart, rect.DOScale(1.25f, puffFadeDuration).SetEase(Ease.OutQuad));
            sequence.Insert(disperseStart, puff.DOFade(0f, puffFadeDuration).SetEase(Ease.InQuad));
        }

        yield return sequence.WaitForCompletion();

        target.localScale = baseScale;
        target.anchoredPosition = basePosition;
        target.localRotation = baseRotation;
        ClearSpawned();

        if (evolveCharacter)
        {
            yield return Evolve();
        }

        // 막은 진화까지 끝난 뒤에 푼다. 진화 도중 사물을 눌러 씬을 떠나면 씨앗이 덜 자란 채로 남는다.
        SetBlocking(false);
        _running = false;

        onFinished?.Invoke();
    }

    /// <summary>
    /// 뽀잉: 곧장 늘어나며 튀어 오르고 → 내려앉으며 눌렸다가 → 출렁이며 자리 잡는다.
    /// 눌림으로 시작하지 않는 이유는 바뀌는 순간에 바로 움직여야 해서다 — 먼저 웅크리면 그만큼 늦게 튄다.
    /// 발밑을 붙잡아 둬야 바닥에 놓인 물건처럼 보인다. 가운데 기준으로 줄이면 공중에서 쪼그라드는 것처럼 보인다.
    /// </summary>
    private Sequence CreateBounce(RectTransform target, Vector3 baseScale, Vector2 basePosition, Vector2 size)
    {
        float height = size.y * baseScale.y;

        void Apply(float sx, float sy, float lift)
        {
            target.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

            // 세로로 줄어든 만큼 내려서 아래쪽 가장자리를 제자리에 둔다.
            float keepBottom = -(1f - sy) * height * target.pivot.y;
            target.anchoredPosition = basePosition + new Vector2(0f, keepBottom + Mathf.Max(0f, lift));
        }

        Sequence bounce = DOTween.Sequence();

        // 첫 프레임부터 크게 움직여야 "뽀잉"이다. OutQuad는 출발이 덜 튀어서 OutCubic으로 튕긴다.
        bounce.Append(DOVirtual.Float(0f, 1f, stretchDuration, t =>
            Apply(Mathf.Lerp(1f, stretchScale.x, t), Mathf.Lerp(1f, stretchScale.y, t), hopHeight * t)).SetEase(Ease.OutCubic));

        // 떨어지며 가속해야 땅에 닿는 느낌이 난다.
        bounce.Append(DOVirtual.Float(0f, 1f, squashDuration, t =>
            Apply(Mathf.Lerp(stretchScale.x, squashScale.x, t), Mathf.Lerp(stretchScale.y, squashScale.y, t), hopHeight * (1f - t))).SetEase(Ease.InQuad));

        // OutBack으로 1을 살짝 넘겼다 돌아오게 해 출렁임을 낸다.
        bounce.Append(DOVirtual.Float(0f, 1f, settleDuration, t =>
            Apply(Mathf.LerpUnclamped(squashScale.x, 1f, t), Mathf.LerpUnclamped(squashScale.y, 1f, t), 0f)).SetEase(Ease.OutBack));

        return bounce;
    }

    /// <summary>
    /// 물체 위에 먼지 구름을 흩어 놓는다. 하나는 반드시 한가운데 — 가운데가 비면 거기로 바꿔치기가 보인다.
    /// </summary>
    private List<Image> SpawnPuffs(RectTransform target, Vector2 size, float longSide)
    {
        var puffs = new List<Image>(puffCount);
        Vector2 halfSpread = size * 0.5f * puffSpread;

        for (int i = 0; i < puffCount; i++)
        {
            Vector2 position = i == 0 ? Vector2.zero : Vector2.Scale(Random.insideUnitCircle, halfSpread);
            float diameter = longSide * puffSizeRatio * Random.Range(0.8f, 1.15f);

            Image puff = Spawn(target, "DustPuff", _cloudSprite, puffColor);
            RectTransform rect = puff.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            rect.localScale = Vector3.one * 0.2f;
            SetAlpha(puff, 0f);

            puffs.Add(puff);
        }

        return puffs;
    }

    /// <summary>
    /// 물체 위쪽으로 반짝이를 튀긴다. 아래로 떨어지는 반짝임은 "깨끗해졌다"보다 "부서졌다"로 읽힌다.
    ///
    /// 오브젝트는 지금(연출 시작 때) 만들어 두고 움직임만 <paramref name="at"/>초에 걸어둔다.
    /// 튀는 순간에 만들면 그 프레임에 생성 비용이 몰려 딱 그때 멈칫한다.
    /// </summary>
    private void AddSparkles(Sequence sequence, float at, RectTransform target, float longSide)
    {
        for (int i = 0; i < sparkleCount; i++)
        {
            float angle = Mathf.Lerp(20f, 160f, (i + Random.Range(0.2f, 0.8f)) / Mathf.Max(1, sparkleCount)) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = longSide * Random.Range(sparkleDistance.x, sparkleDistance.y);
            float diameter = Random.Range(sparkleSize.x, sparkleSize.y);

            Image sparkle = Spawn(target, "Sparkle", _sparkleSprite, sparkleColor);
            RectTransform rect = sparkle.rectTransform;
            rect.anchoredPosition = direction * distance * 0.3f;
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.localScale = Vector3.zero;

            sequence.Insert(at, rect.DOAnchorPos(direction * distance, sparkleDuration).SetEase(Ease.OutCubic));
            sequence.Insert(at, rect.DOLocalRotate(new Vector3(0f, 0f, 90f), sparkleDuration, RotateMode.LocalAxisAdd));
            sequence.Insert(at, rect.DOScale(1f, sparkleDuration * 0.35f).SetEase(Ease.OutBack));
            sequence.Insert(at + sparkleDuration * 0.45f, rect.DOScale(0f, sparkleDuration * 0.55f).SetEase(Ease.InQuad));
        }
    }

    private Image Spawn(RectTransform parent, string label, Sprite sprite, Color color)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        // 맨 뒤 형제라야 클리어·미클리어 그림을 바꿔 켜도 계속 그 위에 그려진다.
        rect.SetAsLastSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        _spawned.Add(go);
        return image;
    }

    private void ClearSpawned()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawned.Clear();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private IEnumerator Evolve()
    {
        EvolutionController controller = EvolutionController.Instance;
        if (controller == null || character == null)
        {
            Debug.LogWarning($"{nameof(HubClearReveal)}: EvolutionController나 씨앗(CharacterEvolutionState)이 없어 진화를 건너뜁니다.", this);
            yield break;
        }

        if (evolutionDelay > 0f)
        {
            yield return new WaitForSeconds(evolutionDelay);
        }

        // 마지막 단계면 PlayEvolution이 조용히 무시하고 IsPlaying도 안 켜진다. 그대로 흘려보내면 된다.
        controller.PlayEvolution(character);
        yield return new WaitWhile(() => controller != null && controller.IsPlaying);
    }

    private void SetBlocking(bool on)
    {
        if (_blocker == null)
        {
            if (!on)
            {
                return;
            }

            _blocker = CreateBlocker();
        }

        _blocker.SetActive(on);
    }

    /// <summary>
    /// 화면 전체를 덮는 투명 막을 자기 캔버스째 만든다. 씬에 미리 두지 않는 이유는, 편집하느라
    /// 덮개 캔버스를 꺼두면 막까지 같이 꺼져 연출 도중 클릭이 새기 때문이다.
    /// </summary>
    private GameObject CreateBlocker()
    {
        var go = new GameObject("ClearRevealBlocker", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = BLOCKER_SORTING_ORDER;

        var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)cover.transform;
        rect.SetParent(go.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 알파 0이어도 레이캐스트는 받는다. 보이지 않게 클릭만 삼킨다.
        var image = cover.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        return go;
    }

    private static MiniGameEntry FindEntry(MiniGameDefinition game)
    {
        foreach (MiniGameEntry entry in FindObjectsByType<MiniGameEntry>(FindObjectsInactive.Exclude))
        {
            if (entry.MiniGame == game)
            {
                return entry;
            }
        }

        return null;
    }

    [ContextMenu("연출 미리보기")]
    private void PreviewNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"{nameof(HubClearReveal)}: 미리보기는 플레이 중에만 됩니다.", this);
            return;
        }

        if (previewEntry == null)
        {
            Debug.LogWarning($"{nameof(HubClearReveal)}: Preview Entry에 오브젝트를 넣어주세요.", this);
            return;
        }

        Play(previewEntry, false);
    }

    private void OnValidate()
    {
        startDelay = Mathf.Max(0f, startDelay);
        wobbleDuration = Mathf.Max(0.01f, wobbleDuration);
        wobbleVibrato = Mathf.Max(1, wobbleVibrato);
        puffCount = Mathf.Max(1, puffCount);
        puffSizeRatio = Mathf.Max(0.05f, puffSizeRatio);
        puffSpread = Mathf.Max(0f, puffSpread);
        puffGrowDuration = Mathf.Max(0.01f, puffGrowDuration);
        puffLead = Mathf.Max(0f, puffLead);
        puffHold = Mathf.Max(0f, puffHold);
        puffFadeDuration = Mathf.Max(0.01f, puffFadeDuration);
        squashDuration = Mathf.Max(0.01f, squashDuration);
        stretchDuration = Mathf.Max(0.01f, stretchDuration);
        settleDuration = Mathf.Max(0.01f, settleDuration);
        sparkleCount = Mathf.Max(0, sparkleCount);
        sparkleSize.x = Mathf.Max(1f, sparkleSize.x);
        sparkleSize.y = Mathf.Max(sparkleSize.x, sparkleSize.y);
        sparkleDistance.y = Mathf.Max(sparkleDistance.x, sparkleDistance.y);
        sparkleDuration = Mathf.Max(0.01f, sparkleDuration);
        evolutionDelay = Mathf.Max(0f, evolutionDelay);
    }
}
