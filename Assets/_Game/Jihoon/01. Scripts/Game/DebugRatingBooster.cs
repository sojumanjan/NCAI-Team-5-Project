using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 평점을 한 번에 올려보는 치트 키. 클리어 판정과 결과 화면, 허브 연동까지 확인하려면
/// 5분 동안 주문을 열다섯 번 성공시켜야 하는데, 그걸 매번 할 수는 없다.
///
/// <see cref="DebugTimeSkipper"/>와 나란히 두고, 제출 빌드에서는 컴포넌트만 지우면
/// 치트 코드가 한 줄도 남지 않는다.
/// </summary>
public class DebugRatingBooster : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("평점을 올릴 대상. 비워두면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private RatingService rating;

    [Header("치트")]
    [Tooltip("누르면 평점이 오르는 키.")]
    [SerializeField] private Key boostKey = Key.F2;

    [Tooltip("한 번 누를 때 오르는 평점.")]
    [SerializeField] private float boostAmount = 1f;

    [Tooltip("켜두면 에디터 밖에서는 스스로 꺼집니다. 제출 빌드에 치트가 남지 않게.")]
    [SerializeField] private bool editorOnly = true;

    private void Awake()
    {
        if (rating == null)
        {
            rating = GetComponent<RatingService>();
        }

        if (rating == null)
        {
            Debug.LogError($"{nameof(DebugRatingBooster)}: {nameof(RatingService)}를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        if (editorOnly && !Application.isEditor)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[boostKey].wasPressedThisFrame)
        {
            rating.AddRating(boostAmount);
            Debug.Log($"[Debug] 평점 +{boostAmount} → {rating.Rating:0.0}");
        }
    }
}
