using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the star rating and flashes the change after each order.
/// Subscribes to <see cref="RatingService"/>; never reads it on a timer.
/// </summary>
public class RatingUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("평점 서비스.")]
    [SerializeField] private RatingService rating;

    [Header("표시")]
    [Tooltip("평점 숫자. {0} 자리에 값이 들어갑니다.")]
    [SerializeField] private TMP_Text ratingText;

    [Tooltip("표시 형식. 숫자만 넣으세요 — 별은 스프라이트로 따로 그립니다.")]
    [SerializeField] private string ratingFormat = "{0:0.0}";

    [Tooltip("평점 바. Image Type을 Filled로 두세요. (선택)")]
    [SerializeField] private Image fillBar;

    [Tooltip("몇 개 만들었는지. 형식: \"{0} / {1}\" (정답 수 / 전체 수) (선택)")]
    [SerializeField] private TMP_Text countText;

    [Tooltip("개수 표시 형식.")]
    [SerializeField] private string countFormat = "{0} / {1}";

    [Header("증감 연출")]
    [Tooltip("\"+0.2\" 같이 잠깐 뜨는 텍스트. (선택)")]
    [SerializeField] private TMP_Text deltaText;

    [Tooltip("증감 표시 형식.")]
    [SerializeField] private string deltaFormat = "{0:+0.0;-0.0}";

    [Tooltip("올랐을 때 색.")]
    [SerializeField] private Color plusColor = new Color(0.35f, 0.9f, 0.4f);

    [Tooltip("떨어졌을 때 색.")]
    [SerializeField] private Color minusColor = new Color(0.95f, 0.35f, 0.35f);

    [Tooltip("증감 표시가 떠 있는 시간 (초).")]
    [SerializeField] private float deltaSeconds = 1.2f;

    private Coroutine _deltaRoutine;

    private void Awake()
    {
        if (rating == null)
        {
            Debug.LogError($"{nameof(RatingUI)}: Rating Service is not assigned.", this);
            enabled = false;
            return;
        }

        if (deltaText != null)
        {
            deltaText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        rating.RatingChanged += HandleRatingChanged;
    }

    private void OnDisable()
    {
        rating.RatingChanged -= HandleRatingChanged;
    }

    private void HandleRatingChanged(float value, float delta)
    {
        if (ratingText != null)
        {
            ratingText.text = string.Format(ratingFormat, value);
        }

        if (fillBar != null)
        {
            fillBar.fillAmount = rating.Normalized;
        }

        if (countText != null)
        {
            countText.text = string.Format(countFormat, rating.CorrectCount, rating.ResolvedCount);
        }

        if (!Mathf.Approximately(delta, 0f))
        {
            ShowDelta(delta);
        }
    }

    private void ShowDelta(float delta)
    {
        if (deltaText == null)
        {
            return;
        }

        deltaText.text = string.Format(deltaFormat, delta);
        deltaText.color = delta >= 0f ? plusColor : minusColor;

        if (_deltaRoutine != null)
        {
            StopCoroutine(_deltaRoutine);
        }

        _deltaRoutine = StartCoroutine(HideDeltaAfterDelay());
    }

    private IEnumerator HideDeltaAfterDelay()
    {
        deltaText.gameObject.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0.1f, deltaSeconds));
        deltaText.gameObject.SetActive(false);
        _deltaRoutine = null;
    }
}
