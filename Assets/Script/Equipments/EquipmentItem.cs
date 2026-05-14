//////using UnityEngine;
//////using System;

///////// <summary>
///////// AREA FORGE - EquipmentItem (ScriptableObject)
/////////
///////// HOW TO CREATE ONE IN UNITY:
/////////   Right-click in Project window → Create → AreaForge → Equipment Item
/////////
///////// One asset = one piece of equipment.
///////// Examples:
/////////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites
/////////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites
/////////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites
/////////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
/////////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites
/////////
///////// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
///////// Drag them from the Project window into the sprite array fields below.
///////// </summary>
//////[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
//////public class EquipmentItem : ScriptableObject
//////{
//////    // ─── Identity ─────────────────────────────────────────────────────────────

//////    [Header("─── 1. IDENTITY ───────────────────────────────")]
//////    [Tooltip("Display name shown in the inventory UI")]
//////    public string itemName = "New Item";

//////    [Tooltip("Which layer this item goes on")]
//////    public EquipmentSlot slot;

//////    [Tooltip("Small square icon shown in the inventory grid buttons")]
//////    public Sprite inventoryIcon;

//////    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
//////    public Color rarityColour = Color.white;

//////    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

//////    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
//////    public float healthBonus = 0f;
//////    public float abilityBonus = 0f;
//////    public float damageBonus = 0f;

//////    // ─── Animation Sprites (UI Image version — uses Sprite not SpriteRenderer) ──

//////    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
//////    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
//////    public Sprite[] walkSprites;

//////    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
//////    public Sprite[] idleSprites;

//////    [Tooltip("Drag sprites here for the Death animation (optional)")]
//////    public Sprite[] deathSprites;

//////    // ─── Body Type Variants ───────────────────────────────────────────────────

//////    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
//////    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
//////             "add one entry here per body type.\n" +
//////             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.")]
//////    public BodyTypeVariant[] bodyTypeVariants;

//////    // ─── Helper ───────────────────────────────────────────────────────────────

//////    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
//////    {
//////        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
//////        {
//////            foreach (var v in bodyTypeVariants)
//////            {
//////                if (v.bodyType != bodyType) continue;
//////                return state switch
//////                {
//////                    AnimationState.Walk => v.walkSprites,
//////                    AnimationState.Idle => v.idleSprites,
//////                    AnimationState.Death => v.deathSprites,
//////                    _ => v.idleSprites
//////                };
//////            }
//////        }

//////        return state switch
//////        {
//////            AnimationState.Walk => walkSprites,
//////            AnimationState.Idle => idleSprites,
//////            AnimationState.Death => deathSprites,
//////            _ => idleSprites
//////        };
//////    }
//////}

//////// ─── Supporting types ─────────────────────────────────────────────────────────

//////[Serializable]
//////public class BodyTypeVariant
//////{
//////    [Tooltip("Which body shape these sprites are for")]
//////    public BodyType bodyType;
//////    public Sprite[] walkSprites;
//////    public Sprite[] idleSprites;
//////    public Sprite[] deathSprites;
//////}

//////public enum AnimationState { Idle, Walk, Death }

////using UnityEngine;
////using System;

/////// <summary>
/////// AREA FORGE - EquipmentItem (ScriptableObject)
///////
/////// HOW TO CREATE ONE IN UNITY:
///////   Right-click in Project window → Create → AreaForge → Equipment Item
///////
/////// One asset = one piece of equipment.
/////// Examples:
///////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites + ridingSprites
///////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites + ridingSprites
///////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites + ridingSprites
///////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
///////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites + ridingSprites
///////
/////// RIDING SPRITES
/////// ─────────────────────────────────────────────────────────────────────────────
///////   ridingSprites  = the sitting-on-dragon frames for this item.
///////
///////   For the soldier you have one "sitting" pose animation (the brief statement
///////   said "there is a soldier sitting animation").  Drag those frames here for
///////   every EquipmentItem you want animated while mounted.
///////
///////   Items with an EMPTY ridingSprites array fall back to idleSprites[0], so
///////   the soldier still looks correct even on items you haven't rigged for riding
///////   yet — he'll just hold the first idle frame.
///////
/////// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
/////// Drag them from the Project window into the sprite array fields below.
/////// </summary>
////[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
////public class EquipmentItem : ScriptableObject
////{
////    // ─── Identity ─────────────────────────────────────────────────────────────

////    [Header("─── 1. IDENTITY ───────────────────────────────")]
////    [Tooltip("Display name shown in the inventory UI")]
////    public string itemName = "New Item";

////    [Tooltip("Which layer this item goes on")]
////    public EquipmentSlot slot;

////    [Tooltip("Small square icon shown in the inventory grid buttons")]
////    public Sprite inventoryIcon;

////    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
////    public Color rarityColour = Color.white;

////    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

////    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
////    public float healthBonus = 0f;
////    public float abilityBonus = 0f;
////    public float damageBonus = 0f;

////    // ─── Animation Sprites ────────────────────────────────────────────────────

////    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
////    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
////    public Sprite[] walkSprites;

////    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
////    public Sprite[] idleSprites;

////    [Tooltip("Drag sprites here for the Death animation (optional)")]
////    public Sprite[] deathSprites;

////    [Tooltip("Drag sprites here for the Riding (sitting on dragon) animation.\n\n" +
////             "Leave empty to fall back to idleSprites[0] — useful for items you\n" +
////             "haven't rigged for riding yet, e.g. the weapon layer.")]
////    public Sprite[] ridingSprites;

////    // ─── Body Type Variants ───────────────────────────────────────────────────

////    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
////    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
////             "add one entry here per body type.\n" +
////             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.\n\n" +
////             "Each variant now also has a ridingSprites array for the mounted pose.")]
////    public BodyTypeVariant[] bodyTypeVariants;

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Returns the sprite array for the given animation state and body type.
////    ///
////    /// Riding fallback chain:
////    ///   1. Variant ridingSprites (if variant exists and has riding frames)
////    ///   2. Top-level ridingSprites (if non-empty)
////    ///   3. idleSprites (graceful fallback — soldier holds idle frame while mounted)
////    ///
////    /// This means you can rig armors for riding one at a time and every armor
////    /// looks correct from day one, even before you've added riding sprites.
////    /// </summary>
////    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
////    {
////        // ── Body-type variant path ────────────────────────────────────────────
////        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
////        {
////            foreach (var v in bodyTypeVariants)
////            {
////                if (v.bodyType != bodyType) continue;

////                if (state == AnimationState.Riding)
////                {
////                    // Prefer variant riding sprites, then fall back gracefully
////                    if (v.ridingSprites != null && v.ridingSprites.Length > 0)
////                        return v.ridingSprites;
////                    if (ridingSprites != null && ridingSprites.Length > 0)
////                        return ridingSprites;
////                    return v.idleSprites;  // ultimate fallback: idle frame while mounted
////                }

////                return state switch
////                {
////                    AnimationState.Walk => v.walkSprites,
////                    AnimationState.Idle => v.idleSprites,
////                    AnimationState.Death => v.deathSprites,
////                    _ => v.idleSprites
////                };
////            }
////        }

////        // ── Standard (no variant) path ────────────────────────────────────────
////        if (state == AnimationState.Riding)
////        {
////            if (ridingSprites != null && ridingSprites.Length > 0)
////                return ridingSprites;
////            return idleSprites;  // fallback: idle frame while mounted
////        }

////        return state switch
////        {
////            AnimationState.Walk => walkSprites,
////            AnimationState.Idle => idleSprites,
////            AnimationState.Death => deathSprites,
////            _ => idleSprites
////        };
////    }
////}

////// ─── Supporting types ─────────────────────────────────────────────────────────

////[Serializable]
////public class BodyTypeVariant
////{
////    [Tooltip("Which body shape these sprites are for")]
////    public BodyType bodyType;
////    public Sprite[] walkSprites;
////    public Sprite[] idleSprites;
////    public Sprite[] deathSprites;

////    [Tooltip("Sitting-on-dragon sprites for this body shape.\n" +
////             "Leave empty to use the parent item's ridingSprites, " +
////             "or idle as a last resort.")]
////    public Sprite[] ridingSprites;
////}

/////// <summary>
/////// Animation states that SpriteLayerAnimator can be put into.
///////
/////// Riding  = soldier is mounted on a dragon (sitting animation).
///////           SoldierController.EnterRidingState() triggers this.
///////           SoldierController.ExitRidingState()  returns to Walk/Idle.
/////// </summary>
////public enum AnimationState { Idle, Walk, Death, Riding }

////using UnityEngine;
////using System;

/////// <summary>
/////// AREA FORGE - EquipmentItem (ScriptableObject)
///////
/////// HOW TO CREATE ONE IN UNITY:
///////   Right-click in Project window → Create → AreaForge → Equipment Item
///////
/////// One asset = one piece of equipment.
/////// Examples:
///////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites
///////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites
///////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites
///////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
///////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites
///////
/////// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
/////// Drag them from the Project window into the sprite array fields below.
/////// </summary>
////[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
////public class EquipmentItem : ScriptableObject
////{
////    // ─── Identity ─────────────────────────────────────────────────────────────

////    [Header("─── 1. IDENTITY ───────────────────────────────")]
////    [Tooltip("Display name shown in the inventory UI")]
////    public string itemName = "New Item";

////    [Tooltip("Which layer this item goes on")]
////    public EquipmentSlot slot;

////    [Tooltip("Small square icon shown in the inventory grid buttons")]
////    public Sprite inventoryIcon;

////    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
////    public Color rarityColour = Color.white;

////    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

////    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
////    public float healthBonus = 0f;
////    public float abilityBonus = 0f;
////    public float damageBonus = 0f;

////    // ─── Animation Sprites (UI Image version — uses Sprite not SpriteRenderer) ──

////    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
////    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
////    public Sprite[] walkSprites;

////    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
////    public Sprite[] idleSprites;

////    [Tooltip("Drag sprites here for the Death animation (optional)")]
////    public Sprite[] deathSprites;

////    // ─── Body Type Variants ───────────────────────────────────────────────────

////    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
////    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
////             "add one entry here per body type.\n" +
////             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.")]
////    public BodyTypeVariant[] bodyTypeVariants;

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
////    {
////        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
////        {
////            foreach (var v in bodyTypeVariants)
////            {
////                if (v.bodyType != bodyType) continue;
////                return state switch
////                {
////                    AnimationState.Walk => v.walkSprites,
////                    AnimationState.Idle => v.idleSprites,
////                    AnimationState.Death => v.deathSprites,
////                    _ => v.idleSprites
////                };
////            }
////        }

////        return state switch
////        {
////            AnimationState.Walk => walkSprites,
////            AnimationState.Idle => idleSprites,
////            AnimationState.Death => deathSprites,
////            _ => idleSprites
////        };
////    }
////}

////// ─── Supporting types ─────────────────────────────────────────────────────────

////[Serializable]
////public class BodyTypeVariant
////{
////    [Tooltip("Which body shape these sprites are for")]
////    public BodyType bodyType;
////    public Sprite[] walkSprites;
////    public Sprite[] idleSprites;
////    public Sprite[] deathSprites;
////}

////public enum AnimationState { Idle, Walk, Death }

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
//                    AnimationState.Walk  => v.walkSprites,
//                    AnimationState.Idle  => v.idleSprites,
//                    AnimationState.Death => v.deathSprites,
//                    _                    => v.idleSprites
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
//            AnimationState.Walk  => walkSprites,
//            AnimationState.Idle  => idleSprites,
//            AnimationState.Death => deathSprites,
//            _                    => idleSprites
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

////using UnityEngine;
////using System;

/////// <summary>
/////// AREA FORGE - EquipmentItem (ScriptableObject)
///////
/////// HOW TO CREATE ONE IN UNITY:
///////   Right-click in Project window → Create → AreaForge → Equipment Item
///////
/////// One asset = one piece of equipment.
/////// Examples:
///////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites
///////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites
///////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites
///////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
///////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites
///////
/////// YOUR SPRITES ARE IN: Assets/Sprites/Player  (and sub-folders)
/////// Drag them from the Project window into the sprite array fields below.
/////// </summary>
////[CreateAssetMenu(menuName = "AreaForge/Equipment Item", fileName = "NewEquipmentItem")]
////public class EquipmentItem : ScriptableObject
////{
////    // ─── Identity ─────────────────────────────────────────────────────────────

////    [Header("─── 1. IDENTITY ───────────────────────────────")]
////    [Tooltip("Display name shown in the inventory UI")]
////    public string itemName = "New Item";

////    [Tooltip("Which layer this item goes on")]
////    public EquipmentSlot slot;

////    [Tooltip("Small square icon shown in the inventory grid buttons")]
////    public Sprite inventoryIcon;

////    [Tooltip("Gold = Legendary, Blue = Rare, White = Common")]
////    public Color rarityColour = Color.white;

////    // ─── Stat Bonuses ─────────────────────────────────────────────────────────

////    [Header("─── 2. STAT BONUSES (0 for cosmetic-only) ─────")]
////    public float healthBonus = 0f;
////    public float abilityBonus = 0f;
////    public float damageBonus = 0f;

////    // ─── Animation Sprites (UI Image version — uses Sprite not SpriteRenderer) ──

////    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
////    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
////    public Sprite[] walkSprites;

////    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
////    public Sprite[] idleSprites;

////    [Tooltip("Drag sprites here for the Death animation (optional)")]
////    public Sprite[] deathSprites;

////    // ─── Body Type Variants ───────────────────────────────────────────────────

////    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
////    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
////             "add one entry here per body type.\n" +
////             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.")]
////    public BodyTypeVariant[] bodyTypeVariants;

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
////    {
////        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
////        {
////            foreach (var v in bodyTypeVariants)
////            {
////                if (v.bodyType != bodyType) continue;
////                return state switch
////                {
////                    AnimationState.Walk => v.walkSprites,
////                    AnimationState.Idle => v.idleSprites,
////                    AnimationState.Death => v.deathSprites,
////                    _ => v.idleSprites
////                };
////            }
////        }

////        return state switch
////        {
////            AnimationState.Walk => walkSprites,
////            AnimationState.Idle => idleSprites,
////            AnimationState.Death => deathSprites,
////            _ => idleSprites
////        };
////    }
////}

////// ─── Supporting types ─────────────────────────────────────────────────────────

////[Serializable]
////public class BodyTypeVariant
////{
////    [Tooltip("Which body shape these sprites are for")]
////    public BodyType bodyType;
////    public Sprite[] walkSprites;
////    public Sprite[] idleSprites;
////    public Sprite[] deathSprites;
////}

////public enum AnimationState { Idle, Walk, Death }

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
/////   ridingSprites  = the sitting-on-dragon frames for this item.
/////
/////   For the soldier you have one "sitting" pose animation (the brief statement
/////   said "there is a soldier sitting animation").  Drag those frames here for
/////   every EquipmentItem you want animated while mounted.
/////
/////   Items with an EMPTY ridingSprites array fall back to idleSprites[0], so
/////   the soldier still looks correct even on items you haven't rigged for riding
/////   yet — he'll just hold the first idle frame.
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

//    [Tooltip("Drag sprites here for the Riding (sitting on dragon) animation.\n\n" +
//             "Leave empty to fall back to idleSprites[0] — useful for items you\n" +
//             "haven't rigged for riding yet, e.g. the weapon layer.")]
//    public Sprite[] ridingSprites;

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
//    /// Riding fallback chain:
//    ///   1. Variant ridingSprites (if variant exists and has riding frames)
//    ///   2. Top-level ridingSprites (if non-empty)
//    ///   3. idleSprites (graceful fallback — soldier holds idle frame while mounted)
//    ///
//    /// This means you can rig armors for riding one at a time and every armor
//    /// looks correct from day one, even before you've added riding sprites.
//    /// </summary>
//    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
//    {
//        // ── Body-type variant path ────────────────────────────────────────────
//        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
//        {
//            foreach (var v in bodyTypeVariants)
//            {
//                if (v.bodyType != bodyType) continue;

//                if (state == AnimationState.Riding)
//                {
//                    // Prefer variant riding sprites, then fall back gracefully
//                    if (v.ridingSprites != null && v.ridingSprites.Length > 0)
//                        return v.ridingSprites;
//                    if (ridingSprites != null && ridingSprites.Length > 0)
//                        return ridingSprites;
//                    return v.idleSprites;  // ultimate fallback: idle frame while mounted
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
//        if (state == AnimationState.Riding)
//        {
//            if (ridingSprites != null && ridingSprites.Length > 0)
//                return ridingSprites;
//            return idleSprites;  // fallback: idle frame while mounted
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

//    [Tooltip("Sitting-on-dragon sprites for this body shape.\n" +
//             "Leave empty to use the parent item's ridingSprites, " +
//             "or idle as a last resort.")]
//    public Sprite[] ridingSprites;
//}

///// <summary>
///// Animation states that SpriteLayerAnimator can be put into.
/////
///// Riding  = soldier is mounted on a dragon (sitting animation).
/////           SoldierController.EnterRidingState() triggers this.
/////           SoldierController.ExitRidingState()  returns to Walk/Idle.
///// </summary>
//public enum AnimationState { Idle, Walk, Death, Riding }

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
/////   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites
/////   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites
/////   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites
/////   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
/////   "Normal Body"     slot = BodyType fill walkSprites + idleSprites
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

//    // ─── Animation Sprites (UI Image version — uses Sprite not SpriteRenderer) ──

//    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
//    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
//    public Sprite[] walkSprites;

//    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
//    public Sprite[] idleSprites;

//    [Tooltip("Drag sprites here for the Death animation (optional)")]
//    public Sprite[] deathSprites;

//    // ─── Body Type Variants ───────────────────────────────────────────────────

//    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
//    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
//             "add one entry here per body type.\n" +
//             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.")]
//    public BodyTypeVariant[] bodyTypeVariants;

//    // ─── Helper ───────────────────────────────────────────────────────────────

//    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
//    {
//        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
//        {
//            foreach (var v in bodyTypeVariants)
//            {
//                if (v.bodyType != bodyType) continue;
//                return state switch
//                {
//                    AnimationState.Walk => v.walkSprites,
//                    AnimationState.Idle => v.idleSprites,
//                    AnimationState.Death => v.deathSprites,
//                    _ => v.idleSprites
//                };
//            }
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
//}

//public enum AnimationState { Idle, Walk, Death }

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
///   "Golden Armor"    slot = Armor,   fill walkSprites + idleSprites + ridingSprites
///   "Silver Helmet"   slot = Helmet,  fill walkSprites + idleSprites + ridingSprites
///   "Iron Sword"      slot = Weapon,  fill walkSprites + idleSprites + ridingSprites
///   "Short Hair"      slot = Hair,    fill bodyTypeVariants (different per body)
///   "Normal Body"     slot = BodyType fill walkSprites + idleSprites + ridingSprites
///
/// RIDING SPRITES
/// ─────────────────────────────────────────────────────────────────────────────
///   riderIdleSprites = frames played while the soldier is seated on an IDLE dragon.
///   riderFlySprites  = frames played while the soldier is seated on a FLYING dragon.
///
///   DragonController calls DragonRiderVisual.SetRiderState(RiderIdle / RiderFly)
///   automatically whenever the dragon enters or leaves the Flying state.
///
///   Items with empty rider arrays fall back gracefully:
///     riderFlySprites empty  →  tries riderIdleSprites, then idleSprites[0]
///     riderIdleSprites empty →  uses idleSprites[0]
///   So the soldier always looks correct even on items you haven't rigged yet.
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

    // ─── Animation Sprites ────────────────────────────────────────────────────

    [Header("─── 3. ANIMATION SPRITES ───────────────────────")]
    [Tooltip("Drag sprites here IN ORDER for the Walk animation (frame 0, 1, 2...)")]
    public Sprite[] walkSprites;

    [Tooltip("Drag sprites here IN ORDER for the Idle/Rest animation")]
    public Sprite[] idleSprites;

    [Tooltip("Drag sprites here for the Death animation (optional)")]
    public Sprite[] deathSprites;

    // ─── Rider Sprites (Dragon Mount) ────────────────────────────────────────

    [Header("─── 3b. RIDER SPRITES (Dragon Mount) ──────────")]
    [Tooltip("Drag sprites here for the Rider IDLE animation " +
             "(soldier sitting still while the dragon is idle).\n\n" +
             "Leave empty to fall back to idleSprites — the soldier will hold\n" +
             "the first idle frame while the dragon is resting.")]
    public Sprite[] riderIdleSprites;

    [Tooltip("Drag sprites here for the Rider FLY animation " +
             "(soldier leaning forward while the dragon is flying).\n\n" +
             "Leave empty to fall back to riderIdleSprites, then idleSprites.")]
    public Sprite[] riderFlySprites;

    // ─── Body Type Variants ───────────────────────────────────────────────────

    [Header("─── 4. BODY-TYPE VARIANTS (Hair/Armor/Helmet only) ─")]
    [Tooltip("If this item looks DIFFERENT on Skinny vs Muscular vs Chubby bodies,\n" +
             "add one entry here per body type.\n" +
             "If it looks the same on all bodies (e.g. a sword), leave this EMPTY.\n\n" +
             "Each variant now also has a ridingSprites array for the mounted pose.")]
    public BodyTypeVariant[] bodyTypeVariants;

    // ─── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the sprite array for the given animation state and body type.
    ///
    /// RiderIdle fallback chain:
    ///   1. Variant riderIdleSprites (if variant exists and has frames)
    ///   2. Top-level riderIdleSprites (if non-empty)
    ///   3. idleSprites (graceful fallback)
    ///
    /// RiderFly fallback chain:
    ///   1. Variant riderFlySprites (if variant exists and has frames)
    ///   2. Top-level riderFlySprites (if non-empty)
    ///   3. RiderIdle result from above (fly falls back to idle-ride pose)
    ///
    /// This means you can rig each item for riding one animation at a time and
    /// every item looks correct from day one.
    /// </summary>
    public Sprite[] GetSprites(AnimationState state, BodyType bodyType)
    {
        // ── Body-type variant path ────────────────────────────────────────────
        if (bodyTypeVariants != null && bodyTypeVariants.Length > 0)
        {
            foreach (var v in bodyTypeVariants)
            {
                if (v.bodyType != bodyType) continue;

                if (state == AnimationState.RiderIdle)
                {
                    if (v.riderIdleSprites != null && v.riderIdleSprites.Length > 0)
                        return v.riderIdleSprites;
                    if (riderIdleSprites != null && riderIdleSprites.Length > 0)
                        return riderIdleSprites;
                    return v.idleSprites;                    // ultimate fallback
                }

                if (state == AnimationState.RiderFly)
                {
                    if (v.riderFlySprites != null && v.riderFlySprites.Length > 0)
                        return v.riderFlySprites;
                    if (riderFlySprites != null && riderFlySprites.Length > 0)
                        return riderFlySprites;
                    // Fall back to rider-idle for this variant
                    if (v.riderIdleSprites != null && v.riderIdleSprites.Length > 0)
                        return v.riderIdleSprites;
                    if (riderIdleSprites != null && riderIdleSprites.Length > 0)
                        return riderIdleSprites;
                    return v.idleSprites;                    // ultimate fallback
                }

                return state switch
                {
                    AnimationState.Walk => v.walkSprites,
                    AnimationState.Idle => v.idleSprites,
                    AnimationState.Death => v.deathSprites,
                    _ => v.idleSprites
                };
            }
        }

        // ── Standard (no variant) path ────────────────────────────────────────
        if (state == AnimationState.RiderIdle)
        {
            if (riderIdleSprites != null && riderIdleSprites.Length > 0)
                return riderIdleSprites;
            return idleSprites;                              // fallback
        }

        if (state == AnimationState.RiderFly)
        {
            if (riderFlySprites != null && riderFlySprites.Length > 0)
                return riderFlySprites;
            if (riderIdleSprites != null && riderIdleSprites.Length > 0)
                return riderIdleSprites;                     // fly → idle-ride fallback
            return idleSprites;                              // ultimate fallback
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

    [Tooltip("Sitting-still sprites for this body shape (dragon is idle).\n" +
             "Leave empty to use the parent item's riderIdleSprites, " +
             "or idle as a last resort.")]
    public Sprite[] riderIdleSprites;

    [Tooltip("Flying-pose sprites for this body shape (dragon is flying).\n" +
             "Leave empty to fall back to riderFlySprites on the parent item, " +
             "then riderIdleSprites, then idle.")]
    public Sprite[] riderFlySprites;
}

/// <summary>
/// Animation states that SpriteLayerAnimator can be put into.
///
/// RiderIdle = soldier is mounted and the dragon is idle (sitting still on dragon).
///             DragonController.EnterIdle()   triggers this via DragonRiderVisual.SetRiderState().
///
/// RiderFly  = soldier is mounted and the dragon is flying (leaning-forward fly pose).
///             DragonController.EnterFlying() triggers this via DragonRiderVisual.SetRiderState().
/// </summary>
public enum AnimationState { Idle, Walk, Death, RiderIdle, RiderFly }