using UnityEngine;

/// <summary>
/// CANNON PANEL — CannonData (ScriptableObject)
///
/// One asset per cannon TYPE (3 total — e.g. Iron Field, War Cart, Swivel).
/// Create via: Right-click Project → Create → AreaForge → Cannon Data
///
/// Stats match the screenshot HUD:
///   Details section → Name, Cost, Range
///   HUD bars        → Health, Ability, Damage
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Cannon Data", fileName = "NewCannon")]
public class CannonData : ScriptableObject
{
    [Header("Identity")]
    public string cannonName = "Iron Field";
    public int cost = 100;

    [Header("Prefab — drag the matching cannon prefab here")]
    [Tooltip("Instantiated inside CannonSlot when equipped")]
    public GameObject prefab;

    [Header("Preview Sprite")]
    [Tooltip("Shown on the card and the large preview in the details panel")]
    public Sprite previewSprite;

    [Header("Idle Animation")]
    public Sprite[] idleSprites;
    public float idleFPS = 6f;

    [Header("Base Stats")]
    public float health = 80f;
    public float ability = 50f;
    public float damage = 20f;
    public float range = 40f;   // shown as "Range: 40m" in details

    [Header("Upgrade Gains (applied per upgrade, max 3 upgrades)")]
    public float upgradeHealthGain = 10f;
    public float upgradeAbilityGain = 7f;
    public float upgradeDamageGain = 5f;
    public float upgradeRangeGain = 8f;

    [Header("Upgrade Timing")]
    [Tooltip("Real-time seconds the upgrade takes to finish")]
    public float upgradeDuration = 15f;
}