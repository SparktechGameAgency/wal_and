//using UnityEngine;
//using System;

///// <summary>
///// AREA FORGE - EquipmentItem (ScriptableObject)
/////
///// HOW TO CREATE ONE IN UNITY:
/////   Right-click in Project window → Create → AreaForge → Equipment Item
/////
///// One asset = one piece of equipment.
///// Examples:
/////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites + ridingSprites
/////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites + ridingSprites
/////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites + ridingSprites
/////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
/////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites + ridingSprites
/////
///// RIDING SPRITES
///// ─────────────────────────────────────────────────────────────────────────────
/////   riderIdleSprites = frames played while the soldier is seated on an IDLE dragon.
/////   riderFlySprites  = frames played while the soldier is seated on a FLYING dragon.
/////
/////   DragonController calls DragonRiderVisual.SetRiderState(RiderIdle / RiderFly)
/////   automatically whenever the dragon enters or leaves the Flying state.
/////
/////   Items with empty rider arrays fall back gracefully:
/////     riderFlySprites empty  →  tries riderIdleSprites, then idleSprites[0]
/////     riderIdleSprites empty →  uses idleSprites[0]
/////   So the soldier always looks correct even on items you haven't rigged yet.
/////
///// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
///// Drag them from the Project window into the sprite array fields below.
///// </summary>
//[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
//public class EquipmentItem : ScriptableObject
//{
//    // ─── Identity ─────────────────────────────────────────────────────────────

//    [Header("─── 1. IDENTITY ───────────────────────────────")]
//    [Tooltip("Display name shown in the inventory UI")]
//    public string itemName = "New Item";

//    [Tooltip("Which layer this item goes on")]
//    public EquipmentSlot slot;

//    [Tooltip("Small square icon shown in the inventory grid buttons")]
//    public Sprite inventoryIcon;

//    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
//    public Color rarityColour = Color.white;

//    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

//    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
//    public float healthBonus = 0f;
//    public float abilityBonus = 0f;
//    public float damageBonus = 0f;

//    // ─── Animation Sprites ────────────────────────────────────────────────────

//    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
//    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
//    public Sprite[] walkSprites;

//    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
//    public Sprite[] idleSprites;

//    [Tooltip("Drag sprites here for the Death animation (optional)")]
//    public Sprite[] deathSprites;

//    // ─── Rider Sprites (Dragon Mount) ────────────────────────────────────────

//    [Header("─── 3b. RIDER SPRITES (Dragon Mount) ──────────")]
//    [Tooltip("Drag sprites here for the Rider IDLE animation " +
//             "(soldier sitting still while the dragon is idle).\n\n" +
//             "Leave empty to fall back to idleSprites — the soldier will hold\n" +
//             "the first idle frame while the dragon is resting.")]
//    public Sprite[] riderIdleSprites;

//    [Tooltip("Drag sprites here for the Rider FLY animation " +
//             "(soldier leaning forward while the dragon is flying).\n\n" +
//             "Leave empty to fall back to riderIdleSprites, then idleSprites.")]
//    public Sprite[] riderFlySprites;

//    // ─── Body Type Variants ───────────────────────────────────────────────────

//    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
//    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
//             "add one entry here per body type.\n" +
//             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.\n\n" +
//             "Each variant now also has a ridingSprites array for the mounted pose.")]
//    public BodyTypeVariant[] bodyTypeVariants;

//    // ─── Helper ───────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns the sprite array for the given animation state and body type.
//    ///
//    /// RiderIdle fallback chain:
//    ///   1. Variant riderIdleSprites (if variant exists and has frames)
//    ///   2. Top-level riderIdleSprites (if non-empty)
//    ///   3. idleSprites (graceful fallback)
//    ///
//    /// RiderFly fallback chain:
//    ///   1. Variant riderFlySprites (if variant exists and has frames)
//    ///   2. Top-level riderFlySprites (if non-empty)
//    ///   3. RiderIdle result from above (fly falls back to idle-ride pose)
//    ///
//    /// This means you can rig each item for riding one animation at a time and
//    /// every item looks correct from day one.
//    /// </summary>
//    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
//    {
//        // ── Body-type variant path ────────────────────────────────────────────
//        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
//        {
//            foreach (var v in bodyTypeVariants)
//            {
//                if (v.bodyType != bodyType) continue;

//                if (state == AnimationState.RiderIdle)
//                {
//                    if (v.riderIdleSprites != null && v.riderIdleSprites.Length > 0)
//                        return v.riderIdleSprites;
//                    if (riderIdleSprites != null && riderIdleSprites.Length > 0)
//                        return riderIdleSprites;
//                    return v.idleSprites;                    // ultimate fallback
//                }

//                if (state == AnimationState.RiderFly)
//                {
//                    if (v.riderFlySprites != null && v.riderFlySprites.Length > 0)
//                        return v.riderFlySprites;
//                    if (riderFlySprites != null && riderFlySprites.Length > 0)
//                        return riderFlySprites;
//                    // Fall back to rider-idle for this variant
//                    if (v.riderIdleSprites != null && v.riderIdleSprites.Length > 0)
//                        return v.riderIdleSprites;
//                    if (riderIdleSprites != null && riderIdleSprites.Length > 0)
//                        return riderIdleSprites;
//                    return v.idleSprites;                    // ultimate fallback
//                }

//                return state switch
//                {
//                    AnimationState.Walk => v.walkSprites,
//                    AnimationState.Idle => v.idleSprites,
//                    AnimationState.Death => v.deathSprites,
//                    _ => v.idleSprites
//                };
//            }
//        }

//        // ── Standard (no variant) path ────────────────────────────────────────
//        if (state == AnimationState.RiderIdle)
//        {
//            if (riderIdleSprites != null && riderIdleSprites.Length > 0)
//                return riderIdleSprites;
//            return idleSprites;                              // fallback
//        }

//        if (state == AnimationState.RiderFly)
//        {
//            if (riderFlySprites != null && riderFlySprites.Length > 0)
//                return riderFlySprites;
//            if (riderIdleSprites != null && riderIdleSprites.Length > 0)
//                return riderIdleSprites;                     // fly → idle-ride fallback
//            return idleSprites;                              // ultimate fallback
//        }

//        return state switch
//        {
//            AnimationState.Walk => walkSprites,
//            AnimationState.Idle => idleSprites,
//            AnimationState.Death => deathSprites,
//            _ => idleSprites
//        };
//    }
//}

//// ─── Supporting types ─────────────────────────────────────────────────────────

//[Serializable]
//public class BodyTypeVariant
//{
//    [Tooltip("Which body shape these sprites are for")]
//    public BodyType bodyType;
//    public Sprite[] walkSprites;
//    public Sprite[] idleSprites;
//    public Sprite[] deathSprites;

//    [Tooltip("Sitting-still sprites for this body shape (dragon is idle).\n" +
//             "Leave empty to use the parent item's riderIdleSprites, " +
//             "or idle as a last resort.")]
//    public Sprite[] riderIdleSprites;

//    [Tooltip("Flying-pose sprites for this body shape (dragon is flying).\n" +
//             "Leave empty to fall back to riderFlySprites on the parent item, " +
//             "then riderIdleSprites, then idle.")]
//    public Sprite[] riderFlySprites;
//}

///// <summary>
///// Animation states that SpriteLayerAnimator can be put into.
/////
///// RiderIdle = soldier is mounted and the dragon is idle (sitting still on dragon).
/////             DragonController.EnterIdle()   triggers this via DragonRiderVisual.SetRiderState().
/////
///// RiderFly  = soldier is mounted and the dragon is flying (leaning-forward fly pose).
/////             DragonController.EnterFlying() triggers this via DragonRiderVisual.SetRiderState().
///// </summary>
//public enum AnimationState { Idle, Walk, Death, RiderIdle, RiderFly }


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
///   "Golden Armor"  slot = Armor,   fill walkSprites + idleSprites + horseIdleSprites + horseRunSprites
///   "Silver Helmet" slot = Helmet,  fill walkSprites + idleSprites + horseIdleSprites + horseRunSprites
///   "Iron Sword"    slot = Weapon,  fill walkSprites + idleSprites + horseIdleSprites + horseRunSprites
///   "Short Hair"    slot = Hair,    fill bodyTypeVariants (different per body)
///   "Normal Body"   slot = BodyType fill walkSprites + idleSprites
///
/// ════════════════════════════════════════════════════════════════════
///  HORSE SPRITES  (section 3c)
/// ════════════════════════════════════════════════════════════════════
///
///  When a soldier is mounted on a horse, HorseController calls
///  SpriteLayerAnimator.SetState(AnimationState.HorseIdle / HorseRun /
///  HorseFight / HorseDead).  SpriteLayerAnimator then calls
///  item.GetSprites(state, bodyType) for each equipped slot.
///
///  Fallback chain for each horse state:
///    HorseIdle  → horseIdleSprites  → idleSprites
///    HorseRun   → horseRunSprites   → horseIdleSprites → idleSprites
///    HorseFight → horseFightSprites → horseIdleSprites → idleSprites
///    HorseDead  → horseDeadSprites  → idleSprites
///
///  This means you can add horse sprites one state at a time.
///  A weapon with only horseIdleSprites set will still look correct
///  in all states (it'll hold the idle pose for Run and Fight).
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
public class EquipmentItem : ScriptableObject
{
    // ─── 1. IDENTITY ────────────────────────────────────────────────────────────

    [Header("─── 1. IDENTITY ───────────────────────────────")]
    [Tooltip("Display name shown in the inventory UI")]
    public string itemName = "New Item";

    [Tooltip("Which layer this item goes on")]
    public EquipmentSlot slot;

    [Tooltip("Small square icon shown in the inventory grid buttons")]
    public Sprite inventoryIcon;

    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
    public Color rarityColour = Color.white;

    // ─── 2. STAT BONUSES ────────────────────────────────────────────────────────

    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
    public float healthBonus = 0f;
    public float abilityBonus = 0f;
    public float damageBonus = 0f;

    // ─── 3a. ON-FOOT ANIMATION SPRITES ─────────────────────────────────────────

    [Header("─── 3a. ON-FOOT ANIMATION SPRITES ──────────────")]
    [Tooltip("Sprites IN ORDER for the Walk/Patrol animation (frame 0, 1, 2...)")]
    public Sprite[] walkSprites;

    [Tooltip("Sprites IN ORDER for the Idle/Rest animation")]
    public Sprite[] idleSprites;

    [Tooltip("Sprites for the Death animation (optional)")]
    public Sprite[] deathSprites;

    [Tooltip("Sprites IN ORDER for the on-foot RUN animation — played when chasing an enemy.\n" +
             "Leave empty → falls back to walkSprites.")]
    public Sprite[] runSprites;

    [Tooltip("Sprites IN ORDER for the on-foot FIGHT animation — played while attacking an enemy.\n" +
             "Leave empty → falls back to walkSprites.")]
    public Sprite[] fightSprites;

    [Tooltip("Sprites IN ORDER for the on-foot JUMP animation (optional — e.g. leap attack).\n" +
             "Leave empty → falls back to runSprites then walkSprites.")]
    public Sprite[] jumpSprites;

    // ─── 3b. DRAGON RIDER SPRITES ──────────────────────────────────────────────

    [Header("─── 3b. DRAGON RIDER SPRITES ─────────────────")]
    [Tooltip("Rider IDLE — soldier sitting still on a stationary dragon.\n" +
             "Leave empty → falls back to idleSprites.")]
    public Sprite[] riderIdleSprites;

    [Tooltip("Rider FLY — soldier leaning forward while dragon is flying.\n" +
             "Leave empty → falls back to riderIdleSprites then idleSprites.")]
    public Sprite[] riderFlySprites;

    // ─── 3c. HORSE RIDER SPRITES ───────────────────────────────────────────────

    [Header("─── 3c. HORSE RIDER SPRITES ──────────────────")]
    [Tooltip("Horse IDLE — soldier sitting still on a stationary horse.\n\n" +
             "Leave empty → falls back to idleSprites.\n" +
             "All other horse states also fall back here if their arrays are empty.")]
    public Sprite[] horseIdleSprites;

    [Tooltip("Horse RUN — soldier riding a galloping horse.\n\n" +
             "Leave empty → falls back to horseIdleSprites then idleSprites.")]
    public Sprite[] horseRunSprites;

    [Tooltip("Horse FIGHT — soldier on a horse in combat (e.g. lance raised).\n\n" +
             "Leave empty → falls back to horseIdleSprites then idleSprites.")]
    public Sprite[] horseFightSprites;

    [Tooltip("Horse DEAD — soldier pose when the horse is killed.\n\n" +
             "Leave empty → falls back to idleSprites.")]
    public Sprite[] horseDeadSprites;

    // ─── 4. BODY-TYPE VARIANTS ──────────────────────────────────────────────────

    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
    [Tooltip("Add one entry per body type IF this item looks different on\n" +
             "Skinny / Chubby / Muscular bodies.\n\n" +
             "Each variant has its own horse sprite arrays.\n" +
             "Leave empty for items that look the same on all bodies (e.g. a sword).")]
    public BodyTypeVariant[] bodyTypeVariants;

    // ─── GetSprites ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the sprite array for the given animation state and body type.
    ///
    /// On-foot combat fallback chains:
    ///   Run   → variant.runSprites   → runSprites   → walkSprites
    ///   Fight → variant.fightSprites → fightSprites → walkSprites
    ///   Jump  → variant.jumpSprites  → jumpSprites  → runSprites → walkSprites
    ///
    /// Horse fallback chains:
    ///   HorseIdle  → variant.horseIdleSprites  → horseIdleSprites  → idleSprites
    ///   HorseRun   → variant.horseRunSprites   → horseRunSprites   → (HorseIdle chain)
    ///   HorseFight → variant.horseFightSprites → horseFightSprites → (HorseIdle chain)
    ///   HorseDead  → variant.horseDeadSprites  → horseDeadSprites  → idleSprites
    ///
    /// Dragon fallback chains (unchanged):
    ///   RiderIdle  → variant.riderIdleSprites  → riderIdleSprites  → idleSprites
    ///   RiderFly   → variant.riderFlySprites   → riderFlySprites   → (RiderIdle chain)
    /// </summary>
    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
    {
        // ── Try body-type variant first ───────────────────────────────────────
        BodyTypeVariant variant = null;
        if (bodyTypeVariants != null)
        {
            foreach (var v in bodyTypeVariants)
            {
                if (v.bodyType == bodyType) { variant = v; break; }
            }
        }

        // ── Horse states ──────────────────────────────────────────────────────
        //
        // FALLBACK RULE: top-level horse arrays (e.g. horseRunSprites) always
        // take priority over a variant's idle arrays.  The old order put
        // variant?.horseIdleSprites before horseRunSprites, so an armor variant
        // with a 1-frame horseIdleSprites silently blocked the 8-frame
        // horseRunSprites from ever being used.
        //
        // Correct priority for every horse state:
        //   1. variant horse-state sprites   (most specific)
        //   2. top-level horse-state sprites (filled for this state)
        //   3. variant horseIdleSprites      (variant idle fallback)
        //   4. top-level horseIdleSprites    (top-level idle fallback)
        //   5. variant idleSprites           (on-foot idle — last resort)
        //   6. top-level idleSprites         (absolute last resort)

        if (state == AnimationState.HorseIdle)
            return FirstNonEmpty(
                variant?.horseIdleSprites,
                horseIdleSprites,
                variant?.idleSprites,
                idleSprites);

        if (state == AnimationState.HorseRun)
            return FirstNonEmpty(
                variant?.horseRunSprites,   // variant run (most specific)
                horseRunSprites,            // top-level run  ← was being skipped before
                variant?.horseIdleSprites,  // variant idle fallback
                horseIdleSprites,           // top-level idle fallback
                variant?.idleSprites,
                idleSprites);

        if (state == AnimationState.HorseFight)
            return FirstNonEmpty(
                variant?.horseFightSprites, // variant fight (most specific)
                horseFightSprites,          // top-level fight
                variant?.horseIdleSprites,  // variant idle fallback
                horseIdleSprites,           // top-level idle fallback
                variant?.idleSprites,
                idleSprites);

        if (state == AnimationState.HorseDead)
            return FirstNonEmpty(
                variant?.horseDeadSprites,  // variant dead (most specific)
                horseDeadSprites,           // top-level dead
                variant?.idleSprites,
                idleSprites);

        // ── Dragon rider states (unchanged) ───────────────────────────────────
        if (state == AnimationState.RiderIdle)
            return FirstNonEmpty(
                variant?.riderIdleSprites,
                riderIdleSprites,
                variant?.idleSprites,
                idleSprites);

        if (state == AnimationState.RiderFly)
            return FirstNonEmpty(
                variant?.riderFlySprites,
                riderFlySprites,
                variant?.riderIdleSprites,
                riderIdleSprites,
                variant?.idleSprites,
                idleSprites);

        // ── On-foot combat states ────────────────────────────────────────────
        //   Run   → variant.runSprites   → runSprites   → walkSprites
        //   Fight → variant.fightSprites → fightSprites → walkSprites
        //   Jump  → variant.jumpSprites  → jumpSprites  → runSprites → walkSprites

        if (state == AnimationState.Run)
            return FirstNonEmpty(
                variant?.runSprites,
                runSprites,
                variant?.walkSprites,
                walkSprites);

        if (state == AnimationState.Fight)
            return FirstNonEmpty(
                variant?.fightSprites,
                fightSprites,
                variant?.walkSprites,
                walkSprites);

        if (state == AnimationState.Jump)
            return FirstNonEmpty(
                variant?.jumpSprites,
                jumpSprites,
                variant?.runSprites,
                runSprites,
                variant?.walkSprites,
                walkSprites);

        // ── On-foot states ───────────────────────────────────────────────────
        if (variant != null)
        {
            return state switch
            {
                AnimationState.Walk => FirstNonEmpty(variant.walkSprites, walkSprites),
                AnimationState.Death => FirstNonEmpty(variant.deathSprites, deathSprites),
                _ => FirstNonEmpty(variant.idleSprites, idleSprites)
            };
        }

        return state switch
        {
            AnimationState.Walk => walkSprites,
            AnimationState.Death => deathSprites,
            _ => idleSprites
        };
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Returns the first Sprite[] in the list that is non-null and non-empty.</summary>
    private static Sprite[] FirstNonEmpty(params Sprite[][] candidates)
    {
        foreach (var arr in candidates)
            if (arr != null && arr.Length > 0) return arr;
        return null;
    }
}

// ─── BodyTypeVariant ─────────────────────────────────────────────────────────

[Serializable]
public class BodyTypeVariant
{
    [Tooltip("Which body shape these sprites are for")]
    public BodyType bodyType;

    // On-foot
    public Sprite[] walkSprites;
    public Sprite[] idleSprites;
    public Sprite[] deathSprites;
    [Tooltip("On-foot run for this body type. Leave empty → parent's runSprites.")]
    public Sprite[] runSprites;
    [Tooltip("On-foot fight for this body type. Leave empty → parent's fightSprites.")]
    public Sprite[] fightSprites;
    [Tooltip("On-foot jump for this body type. Leave empty → parent's jumpSprites.")]
    public Sprite[] jumpSprites;

    // Dragon rider
    [Tooltip("Sitting-still sprites (dragon idle). Leave empty → parent's riderIdleSprites.")]
    public Sprite[] riderIdleSprites;
    [Tooltip("Flying-pose sprites. Leave empty → parent's riderFlySprites fallback chain.")]
    public Sprite[] riderFlySprites;

    // Horse rider
    [Tooltip("Horse idle pose for this body type. Leave empty → parent's horseIdleSprites.")]
    public Sprite[] horseIdleSprites;
    [Tooltip("Horse run pose for this body type. Leave empty → parent's horseRunSprites.")]
    public Sprite[] horseRunSprites;
    [Tooltip("Horse fight pose for this body type. Leave empty → parent's horseFightSprites.")]
    public Sprite[] horseFightSprites;
    [Tooltip("Horse dead pose for this body type. Leave empty → parent's horseDeadSprites.")]
    public Sprite[] horseDeadSprites;
}

// ─── AnimationState ──────────────────────────────────────────────────────────

/// <summary>
/// All animation states across all mount types.
///
/// On-foot:
///   Idle, Walk, Death
///
/// Dragon-mounted:
///   RiderIdle  — sitting on a stationary dragon
///   RiderFly   — leaning forward on a flying dragon
///
/// Horse-mounted:
///   HorseIdle  — sitting on a stationary horse
///   HorseRun   — riding a galloping horse
///   HorseFight — combat pose on horseback
///   HorseDead  — pose when horse is killed
/// </summary>
public enum AnimationState
{
    // ── On-foot ──────────────────────────
    Idle,
    Walk,
    Death,
    Run,        // chasing enemy on foot
    Fight,      // attacking enemy on foot
    Jump,       // leap / jump attack on foot

    // ── Dragon-mounted ───────────────────
    RiderIdle,
    RiderFly,

    // ── Horse-mounted ────────────────────
    HorseIdle,
    HorseRun,
    HorseFight,
    HorseDead
}