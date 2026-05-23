//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - SpriteLayerAnimator  (UI Image version)
/////
///// Advances animation frames on ALL UI Image layers simultaneously.
///// Attach to the root Player GameObject alongside CharacterVisuals + CharacterEquipment.
/////
///// Every tick it reads the current equipped item's sprite array and
///// sets the matching Image.sprite — so changing equipment mid-game
///// just works with no Animator changes needed.
///// </summary>
//public class SpriteLayerAnimator : MonoBehaviour
//{
//    [Header("Animation Speed")]
//    [Tooltip("Frames per second — match your sprite sheet's intended FPS (usually 6–12)")]
//    [SerializeField] private float fps = 8f;

//    private CharacterVisuals _visuals;
//    private CharacterEquipment _equipment;
//    private float _timer = 0f;
//    private int _frame = 0;
//    private AnimationState _state = AnimationState.Idle;

//    private void Awake()
//    {
//        _visuals = GetComponent<CharacterVisuals>();
//        _equipment = GetComponent<CharacterEquipment>();
//    }

//    private void Update()
//    {
//        _timer += Time.deltaTime;
//        if (_timer < 1f / fps) return;
//        _timer = 0f;
//        _frame++;
//        AdvanceAllLayers();
//    }

//    // ─── Called by SoldierController ─────────────────────────────────────────

//    /// <summary>
//    /// Call this from SoldierController when animation state changes:
//    ///   Walk starts  → SetState(AnimationState.Walk)
//    ///   Rest starts  → SetState(AnimationState.Idle)
//    ///   Death        → SetState(AnimationState.Death)
//    /// </summary>
//    public void SetState(AnimationState newState)
//    {
//        if (_state == newState) return;
//        _state = newState;
//        _visuals.CurrentState = newState;
//        _frame = 0;
//        _timer = 0f;
//        AdvanceAllLayers();
//    }

//    // ─── Frame stepping ───────────────────────────────────────────────────────

//    private void AdvanceAllLayers()
//    {
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            var item = _equipment.GetEquipped(slot);
//            if (item == null) continue;

//            var sprites = item.GetSprites(_state, _equipment.CurrentBodyType);
//            if (sprites == null || sprites.Length == 0) continue;

//            int idx = _frame % sprites.Length;
//            var img = _visuals.GetImage(slot);
//            if (img != null && img.enabled)
//                img.sprite = sprites[idx];
//        }
//    }
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
///  Each Update tick it advances a per-slot frame counter and calls
///  CharacterVisuals.SetSprite() with the correct frame. Sprite arrays
///  come from EquipmentItem.GetSprites(currentState, bodyType), so
///  adding a new animation state or body type requires no changes here.
///
///  When SoldierDragDrop.MountOnDragon() fires, it calls
///  SetState(AnimationState.RiderIdle). Every slot immediately jumps to
///  frame 0 of its rider-idle sprites (or falls back to idle sprites for
///  items that have none). DragonController then promotes to RiderFly
///  while airborne. When the soldier dismounts, SoldierDragDrop
///  calls SetState(AnimationState.Idle).
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR FIELDS
/// ════════════════════════════════════════════════════════════════════
///
///  idleFps     Frames per second while Idle    (try 6)
///  walkFps     Frames per second while Walking (try 10)
///  ridingFps   Frames per second while Riding (RiderIdle or RiderFly) (try 8)
///  deathFps    Frames per second while dying   (try 6)
///
///  Each state can feel different:
///    Riding can be slow (just a gentle sway), while Walk is snappy.
///
/// ════════════════════════════════════════════════════════════════════
///  HOW FRAME TIMING WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  Each slot has its own timer (_slotTimer) and frame index (_slotFrame).
///  Slots tick independently so a 4-frame idle body and an 8-frame idle
///  armor can coexist at their natural cycle lengths without either one
///  waiting for the other.
///
///  Single-frame (static) layers cost almost nothing: the timer advances
///  but the modulo keeps _slotFrame at 0 forever, so no SetSprite is
///  called more than once per state change.
///
///  Slots with a null or empty sprite array are silently skipped.
/// </summary>
public class SpriteLayerAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Playback Speed per Animation State (frames per second)")]
    [Tooltip("FPS while Idle. Try 5–7 for a relaxed breathing look.")]
    [Min(1f)][SerializeField] private float idleFps = 6f;

    [Tooltip("FPS while Walking. Try 10–12 for a brisk walk.")]
    [Min(1f)][SerializeField] private float walkFps = 10f;

    [Tooltip("FPS while Riding (RiderIdle / RiderFly — sitting or leaning on dragon). Try 6–9 for a gentle sway.")]
    [Min(1f)][SerializeField] private float ridingFps = 8f;

    [Tooltip("FPS for the Death animation. Try 5–8 depending on how snappy the fall looks.")]
    [Min(1f)][SerializeField] private float deathFps = 6f;

    // ── Private ───────────────────────────────────────────────────────────────

    private CharacterEquipment _equipment;
    private CharacterVisuals _visuals;

    // Per-slot animation state (frame index + accumulated time).
    // Indexed by EquipmentSlot for O(1) access without a Dictionary allocation.
    private readonly Dictionary<EquipmentSlot, int> _slotFrame = new();
    private readonly Dictionary<EquipmentSlot, float> _slotTimer = new();

    // All slots we animate — order doesn't matter for logic but matches the
    // visual stack from bottom to top for readability.
    private static readonly EquipmentSlot[] AllSlots =
    {
        EquipmentSlot.BodyType,
        EquipmentSlot.Face,
        EquipmentSlot.Hair,
        EquipmentSlot.Armor,
        EquipmentSlot.Helmet,
        EquipmentSlot.Weapon,
    };

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _equipment = GetComponent<CharacterEquipment>();
        _visuals = GetComponent<CharacterVisuals>();

        if (_equipment == null)
            Debug.LogWarning("[SpriteLayerAnimator] CharacterEquipment not found on " +
                             $"'{name}'. Attach it to the same root GameObject.", this);

        if (_visuals == null)
            Debug.LogWarning("[SpriteLayerAnimator] CharacterVisuals not found on " +
                             $"'{name}'. Attach it to the same root GameObject.", this);

        // Initialise per-slot counters.
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
            // ── Get the sprite array for this slot in the current state ───────
            var item = _equipment.GetEquipped(slot);
            if (item == null) continue;

            Sprite[] sprites = item.GetSprites(_visuals.CurrentState,
                                               _equipment.CurrentBodyType);

            if (sprites == null || sprites.Length == 0) continue;

            // Static layers (one frame) — SetSprite was already called by
            // CharacterEquipment.Equip() or SetState(); skip the timer.
            if (sprites.Length == 1) continue;

            // ── Advance the slot timer ────────────────────────────────────────
            _slotTimer[slot] += dt;
            float frameDuration = 1f / fps;
            if (_slotTimer[slot] < frameDuration) continue;

            _slotTimer[slot] -= frameDuration;   // carry over leftover time
            _slotFrame[slot] = (_slotFrame[slot] + 1) % sprites.Length;

            // ── Push the new frame to the UI Image ────────────────────────────
            _visuals.SetSprite(slot, sprites[_slotFrame[slot]]);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Switches the soldier's animation state.
    ///
    /// Called by:
    ///   SoldierDragDrop.MountOnDragon() → SetState(RiderIdle)
    ///   SoldierDragDrop.ExitRiding()    → SetState(Idle)
    ///   SoldierController (if present)  → SetState(Walk / Death)
    ///
    /// Every slot's frame counter resets to 0 and the corresponding
    /// sprite array's first frame is pushed to the UI Image immediately,
    /// so there is never a one-Update delay before the new animation starts.
    /// </summary>
    public void SetState(AnimationState newState)
    {
        if (_visuals == null) return;

        _visuals.CurrentState = newState;

        // ── Reset all slot counters and show frame 0 of the new state ─────────
        foreach (var slot in AllSlots)
        {
            _slotFrame[slot] = 0;
            _slotTimer[slot] = 0f;

            var item = _equipment?.GetEquipped(slot);
            if (item == null) continue;

            Sprite[] sprites = item.GetSprites(newState, _equipment.CurrentBodyType);

            // SetSprite(null) hides the Image layer — correct for "no item / no sprite".
            _visuals.SetSprite(slot, sprites != null && sprites.Length > 0
                ? sprites[0]
                : null);
        }

        Debug.Log($"[SpriteLayerAnimator] '{name}' → {newState}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // INTERNAL
    // ══════════════════════════════════════════════════════════════════════════

    /// Maps an AnimationState to its configured FPS value.
    private float FpsForState(AnimationState state) => state switch
    {
        AnimationState.Walk => walkFps,
        AnimationState.RiderIdle => ridingFps,
        AnimationState.RiderFly => ridingFps,
        AnimationState.Death => deathFps,
        _ => idleFps,
    };
}