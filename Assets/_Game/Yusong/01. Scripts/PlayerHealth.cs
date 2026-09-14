using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [SerializeField] private int maxHealth = 5;
    [SerializeField] private Color hitColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private int flashBlinks = 2;

    private Image image;
    private Color originalColor;
    private int currentHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Instance = this;
        image = GetComponent<Image>();
        originalColor = image.color;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());
    }

    private IEnumerator FlashHit()
    {
        for (int i = 0; i < flashBlinks; i++)
        {
            image.color = hitColor;
            yield return new WaitForSeconds(flashInterval);
            image.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
    }
}
