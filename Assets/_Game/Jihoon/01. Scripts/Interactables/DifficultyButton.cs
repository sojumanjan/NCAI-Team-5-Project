// 벽의 난이도 버튼. 좌클릭으로 난이도를 고르고, 골라진 버튼만 눌려 들어간 모습이 된다
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 버튼 3개 중 하나만 눌려 있다. 다른 버튼을 누르면 원래 눌려 있던 버튼이 튀어나온다 —
/// 선택 상태는 <see cref="CookingDifficulty"/> 한 곳이 쥐고, 버튼들은 그 알림만 보고 모습을 맞춘다.
///
/// 이 컴포넌트는 버튼 루트에 붙인다. 판·누름쇠의 콜라이더를 조준해도 리졸버가 부모에서 찾아온다.
/// </summary>
public class DifficultyButton : MonoBehaviour, IClickTarget
{
    [Header("참조")]
    [SerializeField] private CookingDifficulty difficulty;

    [Tooltip("이 버튼이 고르는 난이도.")]
    [SerializeField] private CookingDifficultyLevel level = CookingDifficultyLevel.Normal;

    [Tooltip("눌려 들어가고 튀어나올 누름쇠. 로컬 Z로 움직입니다.")]
    [SerializeField] private Transform pressButton;

    [Header("누름쇠 위치 (로컬 Z)")]
    [Tooltip("골라져 있을 때. 눌려 들어간 자리.")]
    [SerializeField] private float activeZ = 0.06f;

    [Tooltip("골라져 있지 않을 때. 더 튀어나온 자리.")]
    [SerializeField] private float inactiveZ = 0.1f;

    [Tooltip("눌리고 튀어나오는 시간 (초). 짧을수록 딸깍 한다.")]
    [SerializeField] private float pressDuration = 0.08f;

    [Header("소리")]
    [Tooltip("누를 때마다. 이미 골라져 있어도 납니다.")]
    [SerializeField] private SoundData pressSound;

    [Header("문구")]
    [Tooltip("버튼 이름. 문구의 {0} 자리에 들어갑니다.")]
    [SerializeField] private string label = "보통";

    [Tooltip("아직 골라지지 않았을 때.")]
    [SerializeField] private string selectPrompt = "난이도 {0} 선택하기";

    [Tooltip("이미 골라져 있을 때.")]
    [SerializeField] private string selectedPrompt = "난이도 {0} (선택됨)";

    private Tween _tween;

    private void Awake()
    {
        if (difficulty == null)
        {
            difficulty = FindAnyObjectByType<CookingDifficulty>();
        }
    }

    private void OnEnable()
    {
        if (difficulty != null)
        {
            difficulty.Changed += HandleChanged;

            // 첫 화면부터 맞는 모습이어야 해서 트윈 없이 맞춘다. 아직 아무것도 안 골랐으면 셋 다 튀어나와 있다.
            SetPressed(difficulty.IsSelected(level), false);
        }
    }

    private void OnDisable()
    {
        if (difficulty != null)
        {
            difficulty.Changed -= HandleChanged;
        }
    }

    private void OnDestroy()
    {
        _tween?.Kill();
    }

    // ---------------------------------------------------------------- IClickTarget

    public string ClickPrompt
    {
        get
        {
            if (difficulty == null)
            {
                return string.Empty;
            }

            return string.Format(difficulty.IsSelected(level) ? selectedPrompt : selectPrompt, label);
        }
    }

    // 영업이 시작되면 잠긴다. 거짓이면 리졸버가 문구까지 감춰서, 영업 중엔 버튼이 조용해진다.
    public bool CanClick(PlayerHands hands) => difficulty != null && difficulty.CanChange;

    public void OnClick(PlayerHands hands)
    {
        if (!CanClick(hands))
        {
            return;
        }

        if (pressSound != null)
        {
            AudioManager.PlayAt(pressSound, transform.position);
        }

        difficulty.Select(level);
    }

    // ---------------------------------------------------------------- 모습

    private void HandleChanged(CookingDifficultyLevel current) => SetPressed(difficulty.IsSelected(level), true);

    private void SetPressed(bool pressed, bool animate)
    {
        if (pressButton == null)
        {
            return;
        }

        float z = pressed ? activeZ : inactiveZ;
        _tween?.Kill();

        if (!animate || pressDuration <= 0f)
        {
            Vector3 position = pressButton.localPosition;
            position.z = z;
            pressButton.localPosition = position;
            return;
        }

        _tween = pressButton.DOLocalMoveZ(z, pressDuration).SetEase(Ease.OutQuad).SetLink(pressButton.gameObject);
    }

    private void OnValidate()
    {
        pressDuration = Mathf.Max(0f, pressDuration);
    }
}
