using UnityEngine;

/// <summary>
/// CANNON PANEL — CannonData (ScriptableObject)
///
/// One asset per cannon TYPE (3 total: Iron, Bronze, Golden).
/// Create via: Right-click Project → Create → AreaForge → Cannon Data
///
/// All three start at "level 1". There is no progressive lock —
/// any cannon can be bought as long as the player has enough coins.
/// The same type can be bought multiple times.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Cannon Data", fileName = "NewCannon")]
public class CannonData : ScriptableObject
{
    [Header("Identity")]
    public string cannonName = "Iron Cannon";
    public int cost = 80;

    [Header("Prefab — drag the matching cannon prefab here")]
    [Tooltip("Instantiated inside a CannonSlot when drag-dropped onto the castle")]
    public GameObject prefab;

    [Header("Preview Sprite (used in cards and drag ghost)")]
    [Tooltip("Drag the first idle frame here for a quick preview reference")]
    public Sprite previewSprite;

    [Header("Idle Animation")]
    public Sprite[] idleSprites;
    public float idleFPS = 6f;

    [Header("Base Stats")]
    public float health = 60f;
    public float damage = 25f;
    public float range = 40f;

    [Header("Upgrade Gains (applied each upgrade, max 3 upgrades)")]
    [Tooltip("Stat added to health per upgrade")]
    public float upgradeHealthGain = 8f;
    [Tooltip("Stat added to damage per upgrade")]
    public float upgradeDamageGain = 5f;
    [Tooltip("Stat added to range per upgrade")]
    public float upgradeRangeGain = 8f;

    [Header("Upgrade Timing")]
    [Tooltip("Real-time seconds the upgrade takes to complete")]
    public float upgradeDuration = 10f;
}