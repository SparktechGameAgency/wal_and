//using UnityEngine;
//using System.Collections.Generic;

///// <summary>
///// AREA FORGE — SpriteLayerAnimator
/////
///// Attach to the root Soldier / Player GameObject alongside
///// CharacterEquipment and CharacterVisuals.
/////
///// ════════════════════════════════════════════════════════════════════
/////  WHAT THIS DOES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Drives frame-by-frame sprite animation for ALL equipment layers
/////  (body, face, hair, armor, helmet, weapon) simultaneously.
/////
/////  Each Update tick it advances a per-slot frame counter and calls
/////  CharacterVisuals.SetSprite() with the correct frame. Sprite arrays
/////  come from EquipmentItem.GetSprites(currentState, bodyType), so
/////  adding a new animation state or body type requires no changes here.
/////
/////  When SoldierDragDrop.MountOnDragon() fires, it calls
/////  SetState(AnimationState.RiderIdle). Every slot immediately jumps to
/////  frame 0 of its rider-idle sprites (or falls back to idle sprites for
/////  items that have none). DragonController then promotes to RiderFly
/////  while airborne. When the soldier dismounts, SoldierDragDrop
/////  calls SetState(AnimationState.Idle).
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR FIELDS
///// ════════════════════════════════════════════════════════════════════
/////
/////  idleFps     Frames per second while Idle    (try 6)
/////  walkFps     Frames per second while Walking (try 10)
/////  ridingFps   Frames per second while Riding (RiderIdle or RiderFly) (try 8)
/////  deathFps    Frames per second while dying   (try 6)
/////
/////  Each state can feel different:
/////    Riding can be slow (just a gentle sway), while Walk is snappy.
/////
///// ════════════════════════════════════════════════════════════════════
/////  HOW FRAME TIMING WORKS
///// ════════════════════════════════════════════════════════════════════
/////
/////  Each slot has its own timer (_slotTimer) and frame index (_slotFrame).
/////  Slots tick independently so a 4-frame idle body and an 8-frame idle
/////  armor can coexist at their natural cycle lengths without either one
/////  waiting for the other.
/////
/////  Single-frame (static) layers cost almost nothing: the timer advances
/////  but the modulo keeps _slotFrame at 0 forever, so no SetSprite is
/////  called more than once per state change.
/////
/////  Slots with a null or empty sprite array are silently skipped.
///// </summary>
//public class SpriteLayerAnimator : MonoBehaviour
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Playback Speed per Animation State (frames per second)")]
//    [Tooltip("FPS while Idle. Try 5–7 for a relaxed breathing look.")]
//    [Min(1f)][SerializeField] private float idleFps = 6f;

//    [Tooltip("FPS while Walking. Try 10–12 for a brisk walk.")]
//    [Min(1f)][SerializeField] private float walkFps = 10f;

//    [Tooltip("FPS while Riding (RiderIdle / RiderFly — sitting or leaning on dragon). Try 6–9 for a gentle sway.")]
//    [Min(1f)][SerializeField] private float ridingFps = 8f;

//    [Tooltip("FPS for the Death animation. Try 5–8 depending on how snappy the fall looks.")]
//    [Min(1f)][SerializeField] private float deathFps = 6f;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private CharacterEquipment _equipment;
//    private CharacterVisuals _visuals;

//    // Per-slot animation state (frame index + accumulated time).
//    // Indexed by EquipmentSlot for O(1) access without a Dictionary allocation.
//    private readonly Dictionary<EquipmentSlot, int> _slotFrame = new();
//    private readonly Dictionary<EquipmentSlot, float> _slotTimer = new();

//    // All slots we animate — order doesn't matter for logic but matches the
//    // visual stack from bottom to top for readability.
//    private static readonly EquipmentSlot[] AllSlots =
//    {
//        EquipmentSlot.BodyType,
//        EquipmentSlot.Face,
//        EquipmentSlot.Hair,
//        EquipmentSlot.Armor,
//        EquipmentSlot.Helmet,
//        EquipmentSlot.Weapon,
//    };

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _equipment = GetComponent<CharacterEquipment>();
//        _visuals = GetComponent<CharacterVisuals>();

//        if (_equipment == null)
//            Debug.LogWarning("[SpriteLayerAnimator] CharacterEquipment not found on " +
//                             $"'{name}'. Attach it to the same root GameObject.", this);

//        if (_visuals == null)
//            Debug.LogWarning("[SpriteLayerAnimator] CharacterVisuals not found on " +
//                             $"'{name}'. Attach it to the same root GameObject.", this);

//        // Initialise per-slot counters.
//        foreach (var slot in AllSlots)
//        {
//            _slotFrame[slot] = 0;
//            _slotTimer[slot] = 0f;
//        }
//    }

//    private void Update()
//    {
//        if (_equipment == null || _visuals == null) return;

//        float fps = FpsForState(_visuals.CurrentState);
//        float dt = Time.deltaTime;

//        foreach (var slot in AllSlots)
//        {
//            // ── Get the sprite array for this slot in the current state ───────
//            var item = _equipment.GetEquipped(slot);
//            if (item == null) continue;

//            Sprite[] sprites = item.GetSprites(_visuals.CurrentState,
//                                               _equipment.CurrentBodyType);

//            if (sprites == null || sprites.Length == 0) continue;

//            // Static layers (one frame) — SetSprite was already called by
//            // CharacterEquipment.Equip() or SetState(); skip the timer.
//            if (sprites.Length == 1) continue;

//            // ── Advance the slot timer ────────────────────────────────────────
//            _slotTimer[slot] += dt;
//            float frameDuration = 1f / fps;
//            if (_slotTimer[slot] < frameDuration) continue;

//            _slotTimer[slot] -= frameDuration;   // carry over leftover time
//            _slotFrame[slot] = (_slotFrame[slot] + 1) % sprites.Length;

//            // ── Push the new frame to the UI Image ────────────────────────────
//            _visuals.SetSprite(slot, sprites[_slotFrame[slot]]);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC API
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Switches the soldier's animation state.
//    ///
//    /// Called by:
//    ///   SoldierDragDrop.MountOnDragon() → SetState(RiderIdle)
//    ///   SoldierDragDrop.ExitRiding()    → SetState(Idle)
//    ///   SoldierController (if present)  → SetState(Walk / Death)
//    ///
//    /// Every slot's frame counter resets to 0 and the corresponding
//    /// sprite array's first frame is pushed to the UI Image immediately,
//    /// so there is never a one-Update delay before the new animation starts.
//    /// </summary>
//    public void SetState(AnimationState newState)
//    {
//        if (_visuals == null) return;

//        _visuals.CurrentState = newState;

//        // ── Reset all slot counters and show frame 0 of the new state ─────────
//        foreach (var slot in AllSlots)
//        {
//            _slotFrame[slot] = 0;
//            _slotTimer[slot] = 0f;

//            var item = _equipment?.GetEquipped(slot);
//            if (item == null) continue;

//            Sprite[] sprites = item.GetSprites(newState, _equipment.CurrentBodyType);

//            // SetSprite(null) hides the Image layer — correct for "no item / no sprite".
//            _visuals.SetSprite(slot, sprites != null && sprites.Length > 0
//                ? sprites[0]
//                : null);
//        }

//        Debug.Log($"[SpriteLayerAnimator] '{name}' → {newState}");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INTERNAL
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Maps an AnimationState to its configured FPS value.
//    private float FpsForState(AnimationState state) => state switch
//    {
//        AnimationState.Walk => walkFps,
//        AnimationState.RiderIdle => ridingFps,
//        AnimationState.RiderFly => ridingFps,
//        AnimationState.Death => deathFps,
//        _ => idleFps,
//    };
//}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AREA FORGE — SpriteLayerAnimator
///
/// Attach to the root Soldier / Player GameObject alongside
/// CharacterEquipment and CharacterVisuals.
///
/// ════════════════════════════════════════════════════════════════════
///  WHAT THIS DOES
/// ════════════════════════════════════════════════════════════════════
///
///  Drives frame-by-frame sprite animation for ALL equipment layers
///  (body, face, hair, armor, helmet, weapon) simultaneously.
///
///  Every Update tick it advances a per-slot frame counter and calls
///  CharacterVisuals.SetSprite() with the correct frame.  Sprite arrays
///  come from EquipmentItem.GetSprites(currentState, bodyType), so
///  adding new states or body types requires no changes here.
///
/// ════════════════════════════════════════════════════════════════════
///  MOUNT STATES
/// ════════════════════════════════════════════════════════════════════
///
///  Dragon mounting:
///    SoldierDragDrop.MountOnDragon() → SetState(RiderIdle)
///    DragonController promotes to    → SetState(RiderFly) while airborne
///    SoldierDragDrop.DismountFromDragon() → SetState(Idle)
///
///  Horse mounting (NEW):
///    SoldierDragDrop.MountOnHorse()  → SetState(HorseIdle)
///    HorseController.SetRun()        → SetState(HorseRun)
///    HorseController.SetFight()      → SetState(HorseFight)
///    HorseController.SetDead()       → SetState(HorseDead)
///    SoldierDragDrop.DismountFromHorse() → SetState(Idle)
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR FIELDS
/// ════════════════════════════════════════════════════════════════════
///
///  idleFps      FPS for Idle          (try 6)
///  walkFps      FPS for Walk          (try 10)
///  ridingFps    FPS for RiderIdle/Fly (try 8)
///  horseFps     FPS for all horse states (try 8–12 depending on state)
///  deathFps     FPS for Death/HorseDead (try 6)
/// </summary>
public class SpriteLayerAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Playback Speed per Animation State (frames per second)")]
    [Tooltip("FPS while Idle.")]
    [Min(1f)][SerializeField] private float idleFps = 6f;

    [Tooltip("FPS while Walking / on-foot running.")]
    [Min(1f)][SerializeField] private float walkFps = 10f;

    [Tooltip("FPS while Dragon-Riding (RiderIdle or RiderFly).")]
    [Min(1f)][SerializeField] private float ridingFps = 8f;

    [Tooltip("FPS while Horse-Riding (HorseIdle, HorseRun, HorseFight).\n" +
             "HorseRun looks good at 12; HorseIdle at 6–8.")]
    [Min(1f)][SerializeField] private float horseFps = 10f;

    [Tooltip("FPS for Death and HorseDead.")]
    [Min(1f)][SerializeField] private float deathFps = 6f;

    // ── Private ───────────────────────────────────────────────────────────────

    private CharacterEquipment _equipment;
    private CharacterVisuals _visuals;

    // Per-slot frame state
    private readonly Dictionary<EquipmentSlot, int> _slotFrame = new();
    private readonly Dictionary<EquipmentSlot, float> _slotTimer = new();

    // All animated slots — ordered back-to-front for readability
    private static readonly EquipmentSlot[] AllSlots =
    {
        EquipmentSlot.BodyType,
        EquipmentSlot.Face,
        EquipmentSlot.Hair,
        EquipmentSlot.Armor,
        EquipmentSlot.Helmet,
        EquipmentSlot.Weapon,
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _equipment = GetComponent<CharacterEquipment>();
        _visuals = GetComponent<CharacterVisuals>();

        if (_equipment == null)
            Debug.LogWarning("[SpriteLayerAnimator] CharacterEquipment not found on " +
                             $"'{name}'.", this);
        if (_visuals == null)
            Debug.LogWarning("[SpriteLayerAnimator] CharacterVisuals not found on " +
                             $"'{name}'.", this);

        foreach (var slot in AllSlots)
        {
            _slotFrame[slot] = 0;
            _slotTimer[slot] = 0f;
        }
    }

    private void Update()
    {
        if (_equipment == null || _visuals == null) return;

        float fps = FpsForState(_visuals.CurrentState);
        float dt = Time.deltaTime;

        foreach (var slot in AllSlots)
        {
            var item = _equipment.GetEquipped(slot);
            if (item == null) continue;

            Sprite[] sprites = item.GetSprites(_visuals.CurrentState,
                                               _equipment.CurrentBodyType);
            if (sprites == null || sprites.Length == 0) continue;
            if (sprites.Length == 1) continue;   // static frame — already set

            _slotTimer[slot] += dt;
            float frameDuration = 1f / fps;
            if (_slotTimer[slot] < frameDuration) continue;

            _slotTimer[slot] -= frameDuration;
            _slotFrame[slot] = (_slotFrame[slot] + 1) % sprites.Length;

            _visuals.SetSprite(slot, sprites[_slotFrame[slot]]);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Switches to a new animation state and immediately shows frame 0 on all layers.
    ///
    /// Called by:
    ///   SoldierDragDrop.MountOnHorse()      → SetState(HorseIdle)
    ///   HorseController.SetState()          → SetState(HorseRun / HorseFight / HorseDead)
    ///   SoldierDragDrop.DismountFromHorse() → SetState(Idle)
    ///   SoldierDragDrop.MountOnDragon()     → SetState(RiderIdle)
    ///   SoldierController                   → SetState(Walk / Death)
    /// </summary>
    public void SetState(AnimationState newState)
    {
        if (_visuals == null) return;

        _visuals.CurrentState = newState;

        foreach (var slot in AllSlots)
        {
            _slotFrame[slot] = 0;
            _slotTimer[slot] = 0f;

            var item = _equipment?.GetEquipped(slot);
            if (item == null) continue;

            Sprite[] sprites = item.GetSprites(newState, _equipment.CurrentBodyType);

            _visuals.SetSprite(slot, sprites != null && sprites.Length > 0
                ? sprites[0]
                : null);
        }

        Debug.Log($"[SpriteLayerAnimator] '{name}' → {newState}");
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps each AnimationState to its configured FPS.
    ///
    /// Both HorseRun and HorseFight use horseFps.
    /// HorseDead and Death use deathFps (slow, dramatic).
    /// HorseIdle uses idleFps (gentle breathing sway).
    /// </summary>
    private float FpsForState(AnimationState state) => state switch
    {
        AnimationState.Walk => walkFps,
        AnimationState.RiderIdle => ridingFps,
        AnimationState.RiderFly => ridingFps,
        AnimationState.HorseIdle => idleFps,    // gentle seated sway
        AnimationState.HorseRun => horseFps,   // fast gallop
        AnimationState.HorseFight => horseFps,   // active combat
        AnimationState.HorseDead => deathFps,   // slow death
        AnimationState.Death => deathFps,
        _ => idleFps,
    };
}