using UnityEngine;
using TMPro;

namespace Yusong
{
public enum EnemyShape
{
    Circle,
    Triangle,
    Square
}

[CreateAssetMenu(fileName = "UITheme", menuName = "Yusong/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Font")]
    public TMP_FontAsset primaryFont;

    [Header("Shape Sprites")]
    public Sprite circleSprite;
    public Sprite triangleSprite;
    public Sprite squareSprite;

    [Header("Enemy")]
    public Color enemyHitFlashColor = Color.white;

    [Header("Player")]
    public Color playerHitColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    public Color playerHealColor = new Color(0.45f, 1f, 0.55f, 1f);

    [Header("Combo / Fever")]
    public Color comboGaugeNormalColor = new Color(0.2f, 0.9f, 1f, 1f);
    public Color comboGaugeFeverColor = new Color(1f, 0.1f, 0.6f, 1f);
    public Color feverAnnouncementColor = new Color(1f, 0.1f, 0.55f, 1f);

    [Header("Score")]
    public Color scoreGainColor = new Color(0.3f, 1f, 0.4f, 1f);
    public Color scoreLossColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Explosion / AOE")]
    public Color aoeRingColor = new Color(0.45f, 0.92f, 1f, 0.9f);
    public Color explosionRingColor = new Color(1f, 0.78f, 0.22f, 0.95f);

    [Header("Secondary Object")]
    public Color secondarySuccessColor = new Color(0.55f, 1f, 0.35f, 1f);

    [Header("HP Gauge")]
    public Color centerHpGaugeColor = new Color(0.9f, 0.25f, 0.25f, 1f);
    public Color secondaryHpGaugeColor = new Color(0.55f, 1f, 0.35f, 1f);
    public Color hpPipOffColor = new Color(1f, 1f, 1f, 0.25f);
    public Color bossHealthBarColor = new Color(0.75f, 0.2f, 0.85f, 1f);
    public Color bossIndicatorColor = new Color(1f, 0.55f, 0f, 1f);
    public Color bossAlertTextColor = new Color(0.9f, 0.1f, 0.08f, 1f);

    [Header("Game Over")]
    public Color gameOverTextColor = new Color(1f, 0.15f, 0.15f, 1f);
    public Color gameOverBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    public Color clearTextColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color victoryTextColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    public Color defeatHeadlineColor = new Color(0.33f, 0.37f, 0.43f, 1f);
}
}
