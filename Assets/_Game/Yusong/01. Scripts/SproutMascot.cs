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
    private Coroutine surprisedRoutine;

    private void OnEnable()
    {
        ComboManager.FeverStarted += HandleFeverStarted;
        ComboManager.FeverEnded += HandleFeverEnded;
        PlayerHealth.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        ComboManager.FeverStarted -= HandleFeverStarted;
        ComboManager.FeverEnded -= HandleFeverEnded;
        PlayerHealth.Damaged -= HandleDamaged;
    }

    private void Start()
    {
        isFever = false;
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

        if (surprisedRoutine == null) SetSprite(basicSprite);
    }

    private void HandleDamaged()
    {
        if (surprisedRoutine != null) StopCoroutine(surprisedRoutine);
        surprisedRoutine = StartCoroutine(ShowSurprisedThenRevert());
    }

    private IEnumerator ShowSurprisedThenRevert()
    {
        SetSprite(surprisedSprite);
        yield return new WaitForSeconds(surprisedDuration);
        SetSprite(isFever ? happySprite : basicSprite);
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
