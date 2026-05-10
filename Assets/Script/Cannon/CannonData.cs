//using UnityEngine;

///// <summary>
///// CANNON PANEL — CannonData (ScriptableObject)
/////
///// One asset per cannon TYPE (3 total — e.g. Iron Field, War Cart, Swivel).
///// Create via: Right-click Project → Create → AreaForge → Cannon Data
/////
///// Stats match the screenshot HUD:
/////   Details section → Name, Cost, Range
/////   HUD bars        → Health, Ability, Damage
///// </summary>
//[CreateAssetMenu(menuName = "AreaForge/Cannon Data", fileName = "NewCannon")]
//public class CannonData : ScriptableObject
//{
//    [Header("Identity")]
//    public string cannonName = "Iron Field";
//    public int cost = 100;

//    [Header("Prefab — drag the matching cannon prefab here")]
//    [Tooltip("Instantiated inside CannonSlot when equipped")]
//    public GameObject prefab;

//    [Header("Preview Sprite")]
//    [Tooltip("Shown on the card and the large preview in the details panel")]
//    public Sprite previewSprite;

//    [Header("Idle Animation")]
//    public Sprite[] idleSprites;
//    public float idleFPS = 6f;

//    [Header("Base Stats")]
//    public float health = 80f;
//    public float ability = 50f;
//    public float damage = 20f;
//    public float range = 40f;   // shown as "Range: 40m" in details

//    [Header("Upgrade Gains (applied per upgrade, max 3 upgrades)")]
//    public float upgradeHealthGain = 10f;
//    public float upgradeAbilityGain = 7f;
//    public float upgradeDamageGain = 5f;
//    public float upgradeRangeGain = 8f;

//    [Header("Upgrade Timing")]
//    [Tooltip("Real-time seconds the upgrade takes to finish")]
//    public float upgradeDuration = 15f;
//}

using UnityEngine;

/// <summary>
/// CANNON PANEL — CannonData (ScriptableObject)
///
/// One asset per cannon TYPE (3 total).
/// Create via: Right-click Project → Create → AreaForge → Cannon Data
///
/// ════ BARS DISPLAYED IN THE DETAILS PANEL ════
///   Health  ← healthBar
///   Range   ← rangeBar   (replaces old Ability bar — second bar)
///   Damage  ← damageBar
///
/// ════ UPGRADE GAINS (applied each upgrade, 3 upgrades max) ════
///   Health += upgradeHealthGain
///   Range  += upgradeRangeGain   (shown in rangeBar)
///   Damage += upgradeDamageGain
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Cannon Data", fileName = "NewCannon")]
public class CannonData : ScriptableObject
{
    [Header("Identity")]
    public string cannonName = "Iron Field";
    public int cost = 100;

    [Header("Prefab  — instantiated inside CannonSlot when equipped")]
    public GameObject prefab;

    [Header("Preview Sprite  — shown on cards and the large details preview")]
    public Sprite previewSprite;

    [Header("Idle Animation")]
    public Sprite[] idleSprites;
    public float idleFPS = 6f;

    [Header("Base Stats")]
    [Tooltip("Shown on the Health bar.")]
    public float health = 80f;

    [Tooltip("Shown on the Range bar (second bar, replaces old Ability bar).")]
    public float range = 40f;

    [Tooltip("Shown on the Damage bar.")]
    public float damage = 20f;

    [Header("Upgrade Gains  (added per upgrade level, 3 levels max)")]
    public float upgradeHealthGain = 10f;
    public float upgradeRangeGain = 8f;   // ← second bar gain
    public float upgradeDamageGain = 5f;

    [Header("Upgrade Timing")]
    [Tooltip("Real-time seconds the upgrade takes to complete.")]
    public float upgradeDuration = 15f;
}