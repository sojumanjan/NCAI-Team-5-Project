using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Yusong
{
public class SproutMascot : MonoBehaviour
{
    [SerializeField] private Image[] targets;
    [SerializeField] private Sprite basicSprite;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite surprisedSprite;
    [SerializeField] private float surprisedDuration = 1.2f;

    private bool isFever;
    private bool isCleared;
    private Coroutine surprisedRoutine;

    private void OnEnable()
    {
        ComboManager.FeverStarted += HandleFeverStarted;
        ComboManager.FeverEnded += HandleFeverEnded;
        PlayerHealth.Damaged += HandleDamaged;
        CountdownTimer.GameCleared += HandleGameCleared;
    }

    private void OnDisable()
    {
        ComboManager.FeverStarted -= HandleFeverStarted;
        ComboManager.FeverEnded -= HandleFeverEnded;
        PlayerHealth.Damaged -= HandleDamaged;
        CountdownTimer.GameCleared -= HandleGameCleared;
    }

    private void Start()
    {
        isFever = false;
        isCleared = false;
        if (surprisedRoutine != null) StopCoroutine(surprisedRoutine);
        surprisedRoutine = null;
        SetSprite(basicSprite);
    }

    private void HandleFeverStarted()
    {
        isFever = true;

        // A hit landing right as fever starts shouldn't leave the mascot stuck on "surprised".
        if (surprisedRoutine != null)
        {
            StopCoroutine(surprisedRoutine);
            surprisedRoutine = null;
        }

        SetSprite(happySprite);
    }

    private void HandleFeverEnded()
    {
        isFever = false;

        if (surprisedRoutine == null && !isCleared) SetSprite(basicSprite);
    }

    // 클리어 후에는 결과 화면이 떠 있는 동안 계속 기뻐하는 얼굴로 둔다 — 피버 종료나 피격으로 표정이 돌아가지 않게 막는다.
    private void HandleGameCleared()
    {
        isCleared = true;

        if (surprisedRoutine != null)
        {
            StopCoroutine(surprisedRoutine);
            surprisedRoutine = null;
        }

        SetSprite(happySprite);
    }

    private void HandleDamaged()
    {
        if (isCleared) return;
        if (surprisedRoutine != null) StopCoroutine(surprisedRoutine);
        surprisedRoutine = StartCoroutine(ShowSurprisedThenRevert());
    }

    private IEnumerator ShowSurprisedThenRevert()
    {
        SetSprite(surprisedSprite);
        yield return new WaitForSeconds(surprisedDuration);
        SetSprite(isFever || isCleared ? happySprite : basicSprite);
        surprisedRoutine = null;
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite == null || targets == null) return;

        foreach (var image in targets)
        {
            if (image != null) image.sprite = sprite;
        }
    }
}
}
