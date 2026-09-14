using UnityEngine;
using UnityEngine.UI;

public class SparkParticle : MonoBehaviour
{
    private RectTransform rt;
    private Image image;
    private Vector2 velocity;
    private float lifetime;
    private float age;
    private Color startColor;

    public void Init(Vector2 direction, float speed, float life)
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        startColor = image.color;
        velocity = direction * speed;
        lifetime = life;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        age += Time.deltaTime;
        rt.anchoredPosition += velocity * Time.deltaTime;

        float t = age / lifetime;
        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        image.color = c;

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
