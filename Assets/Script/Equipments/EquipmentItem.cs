using UnityEngine;
using System;

/// <summary>
/// AREA FORGE - EquipmentItem (ScriptableObject)
///
/// HOW TO CREATE ONE IN UNITY:
///   Right-click in Project window → Create → AreaForge → Equipment Item
///
/// One asset = one piece of equipment.
/// Examples:
///   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites
///   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites
///   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites
///   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
///   "Normal Body"     slot = BodyType fill walkSprites + idleSprites
///
/// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
/// Drag them from the Project window into the sprite array fields below.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
public class EquipmentItem : ScriptableObject
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    [Header("─── 1. IDENTITY ───────────────────────────────")]
    [Tooltip("Display name shown in the inventory UI")]
    public string itemName = "New Item";

    [Tooltip("Which layer this item goes on")]
    public EquipmentSlot slot;

    [Tooltip("Small square icon shown in the inventory grid buttons")]
    public Sprite inventoryIcon;

    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
    public Color rarityColour = Color.white;

    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
    public float healthBonus = 0f;
    public float abilityBonus = 0f;
    public float damageBonus = 0f;

    // ─── Animation Sprites (UI Image version — uses Sprite not SpriteRenderer) ──

    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
    public Sprite[] walkSprites;

    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
    public Sprite[] idleSprites;

    [Tooltip("Drag sprites here for the Death animation (optional)")]
    public Sprite[] deathSprites;

    // ─── Body Type Variants ───────────────────────────────────────────────────

    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
             "add one entry here per body type.\n" +
             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.")]
    public BodyTypeVariant[] bodyTypeVariants;

    // ─── Helper ───────────────────────────────────────────────────────────────

    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
    {
        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
        {
            foreach (var v in bodyTypeVariants)
            {
                if (v.bodyType != bodyType) continue;
                return state switch
                {
                    AnimationState.Walk => v.walkSprites,
                    AnimationState.Idle => v.idleSprites,
                    AnimationState.Death => v.deathSprites,
                    _ => v.idleSprites
                };
            }
        }

        return state switch
        {
            AnimationState.Walk => walkSprites,
            AnimationState.Idle => idleSprites,
            AnimationState.Death => deathSprites,
            _ => idleSprites
        };
    }
}

// ─── Supporting types ─────────────────────────────────────────────────────────

[Serializable]
public class BodyTypeVariant
{
    [Tooltip("Which body shape these sprites are for")]
    public BodyType bodyType;
    public Sprite[] walkSprites;
    public Sprite[] idleSprites;
    public Sprite[] deathSprites;
}

public enum AnimationState { Idle, Walk, Death }