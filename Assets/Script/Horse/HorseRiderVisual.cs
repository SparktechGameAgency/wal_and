using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — HorseRiderVisual
///
/// Attach to the SoldierSeat child of the Horse prefab (same GameObject as HorseSeat).
///
/// ════════════════════════════════════════════════════════════════════
///  HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Horse  (HorseController + Image + CanvasGroup)
///     └── SoldierSeat  (HorseSeat + HorseRiderVisual)    ← this component
///           ├── Face    (Image)   ← faceImage
///           ├── Armor   (Image)   ← armorImage
///           ├── Helmet  (Image)   ← helmetImage
///           └── Weapon  (Image)   ← weaponImage
///
/// ════════════════════════════════════════════════════════════════════
///  HOW IT WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  When a soldier mounts (HorseController.PerformMount):
///    1. ShowRider(equipment) is called.
///    2. The 4 Images are enabled and their sprites come from
///       EquipmentItem.GetSprites(HorseIdle, bodyType) for each slot.
///    3. Update() ticks each layer independently at the configured FPS.
///
///  When the horse state changes (HorseController.SetState):
///    SetRiderState(newAnimationState) is called, resetting all frame
///    counters and immediately showing frame 0 of the new state on
///    every layer.
///
///  When the soldier dismounts (HorseController.PerformDismount):
///    HideRider() disables all 4 Images.
///
/// ════════════════════════════════════════════════════════════════════
///  SPRITE DATA (EquipmentItem)
/// ════════════════════════════════════════════════════════════════════
///
///  Fill in the horse sprite arrays on each EquipmentItem ScriptableObject:
///    horseIdleSprites   → soldier sitting still on the horse
///    horseRunSprites    → soldier leaning forward at the gallop
///    horseFightSprites  → soldier raising lance / sword on horseback
///    horseDeadSprites   → soldier pose when the horse is killed
///
///  Fallback chain (all handled automatically by EquipmentItem.GetSprites):
///    Run  → HorseIdle → Idle
///    Fight→ HorseIdle → Idle
///    Dead → Idle
///  So you can ship idle-only sprites and everything still looks correct.
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP CHECKLIST
/// ════════════════════════════════════════════════════════════════════
///
///  □ Attach HorseRiderVisual to SoldierSeat (alongside HorseSeat)
///  □ Create 4 child GameObjects: Face / Armor / Helmet / Weapon
///      Each needs: RectTransform + Image (Raycast Target = OFF)
///  □ Wire faceImage / armorImage / helmetImage / weaponImage
///      OR leave them null — Awake() auto-finds children by name
///  □ Tune horseFps values in the Inspector
///  □ Fill horse sprite arrays on each EquipmentItem SO
/// </summary>
public class HorseRiderVisual : MonoBehaviour
{
    // ── Inspector — Layer Images ───────────────────────────────────────────────

    [Header("Rider Body-Part Images  (auto-found by child name if left null)")]
    [Tooltip("Image on the 'Face' child.   Raycast Target = OFF.")]
    [SerializeField] private Image faceImage;

    [Tooltip("Image on the 'Armor' child.  Raycast Target = OFF.")]
    [SerializeField] private Image armorImage;

    [Tooltip("Image on the 'Helmet' child. Raycast Target = OFF.")]
    [SerializeField] private Image helmetImage;

    [Tooltip("Image on the 'Weapon' child. Raycast Target = OFF.")]
    [SerializeField] private Image weaponImage;

    // ── Inspector — Playback Speed ─────────────────────────────────────────────

    [Header("Playback Speed (frames per second)")]
    [Tooltip("FPS while the horse stands still. Try 6–8.")]
    [Min(1f)][SerializeField] private float horseIdleFps = 6f;

    [Tooltip("FPS while the horse gallops. Try 10–12.")]
    [Min(1f)][SerializeField] private float horseRunFps = 12f;

    [Tooltip("FPS during mounted combat. Try 10.")]
    [Min(1f)][SerializeField] private float horseFightFps = 10f;

    [Tooltip("FPS for the death animation (slow, dramatic). Try 6–7.")]
    [Min(1f)][SerializeField] private float horseDeadFps = 6f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private CharacterEquipment _equipment;
    private AnimationState _state = AnimationState.HorseIdle;
    private bool _active = false;

    // Per-layer timers and frame counters
    private float _faceTimer, _armorTimer, _helmetTimer, _weaponTimer;
    private int _faceFrame, _armorFrame, _helmetFrame, _weaponFrame;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find child Images by name so the Inspector wiring is optional
        if (faceImage == null) faceImage = FindChildImage("Face");
        if (armorImage == null) armorImage = FindChildImage("Armor");
        if (helmetImage == null) helmetImage = FindChildImage("Helmet");
        if (weaponImage == null) weaponImage = FindChildImage("Weapon");

        HideRider();   // start hidden until a soldier mounts
    }

    private void Update()
    {
        if (!_active || _equipment == null) return;

        float fps = FpsForState(_state);
        float dt = Time.deltaTime;
        BodyType bodyType = _equipment.CurrentBodyType;

        TickLayer(ref _faceFrame, ref _faceTimer, faceImage,
                  _equipment.GetEquipped(EquipmentSlot.Face), bodyType, fps, dt);

        TickLayer(ref _armorFrame, ref _armorTimer, armorImage,
                  _equipment.GetEquipped(EquipmentSlot.Armor), bodyType, fps, dt);

        TickLayer(ref _helmetFrame, ref _helmetTimer, helmetImage,
                  _equipment.GetEquipped(EquipmentSlot.Helmet), bodyType, fps, dt);

        TickLayer(ref _weaponFrame, ref _weaponTimer, weaponImage,
                  _equipment.GetEquipped(EquipmentSlot.Weapon), bodyType, fps, dt);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows all four rider Images using the mounted soldier's equipped items.
    /// Call this from HorseController.PerformMount() right after seat.MountSoldier().
    /// </summary>
    public void ShowRider(CharacterEquipment equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[HorseRiderVisual] ShowRider called with null equipment. " +
                             "Ensure the soldier has a CharacterEquipment component.", this);
            return;
        }

        _equipment = equipment;
        _active = true;

        EnableLayer(faceImage, true);
        EnableLayer(armorImage, true);
        EnableLayer(helmetImage, true);
        EnableLayer(weaponImage, true);

        // Start in HorseIdle and display frame 0 immediately
        SetRiderStateInternal(AnimationState.HorseIdle, force: true);
    }

    /// <summary>
    /// Hides all four rider Images.
    /// Call this from HorseController.PerformDismount() before releasing the seat.
    /// </summary>
    public void HideRider()
    {
        _active = false;
        _equipment = null;

        EnableLayer(faceImage, false);
        EnableLayer(armorImage, false);
        EnableLayer(helmetImage, false);
        EnableLayer(weaponImage, false);
    }

    /// <summary>
    /// Switches the rider animation state and resets all layer frame counters.
    /// Called by HorseController.SetState() whenever the horse changes state.
    /// </summary>
    public void SetRiderState(AnimationState newState)
    {
        if (!_active || _equipment == null) return;
        SetRiderStateInternal(newState, force: false);
    }

    // ── Internal animation engine ─────────────────────────────────────────────

    private void SetRiderStateInternal(AnimationState newState, bool force)
    {
        if (!force && _state == newState) return;

        _state = newState;

        // Reset all counters
        _faceTimer = _armorTimer = _helmetTimer = _weaponTimer = 0f;
        _faceFrame = _armorFrame = _helmetFrame = _weaponFrame = 0;

        BodyType bodyType = _equipment.CurrentBodyType;

        // Show frame 0 of the new state on every layer immediately
        ShowFirstFrame(faceImage, _equipment.GetEquipped(EquipmentSlot.Face), bodyType);
        ShowFirstFrame(armorImage, _equipment.GetEquipped(EquipmentSlot.Armor), bodyType);
        ShowFirstFrame(helmetImage, _equipment.GetEquipped(EquipmentSlot.Helmet), bodyType);
        ShowFirstFrame(weaponImage, _equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);
    }

    /// <summary>
    /// Advances one body-part layer's timer and pushes the next sprite.
    /// Dead state plays once and freezes on the last frame.
    /// </summary>
    private void TickLayer(ref int frame, ref float timer, Image img,
                           EquipmentItem item, BodyType bodyType,
                           float fps, float dt)
    {
        if (img == null || item == null) return;

        Sprite[] sprites = item.GetSprites(_state, bodyType);
        if (sprites == null || sprites.Length <= 1) return;  // static or missing

        timer += dt;
        float frameDuration = 1f / fps;
        if (timer < frameDuration) return;

        timer -= frameDuration;   // carry-over keeps speed accurate

        if (_state == AnimationState.HorseDead)
        {
            // One-shot: advance until the last frame, then freeze
            if (frame < sprites.Length - 1)
                frame++;
        }
        else
        {
            frame = (frame + 1) % sprites.Length;
        }

        img.sprite = sprites[frame];
    }

    /// <summary>Sets an Image to frame 0 of the current state immediately.</summary>
    private void ShowFirstFrame(Image img, EquipmentItem item, BodyType bodyType)
    {
        if (img == null || item == null) return;

        Sprite[] sprites = item.GetSprites(_state, bodyType);

        if (sprites != null && sprites.Length > 0)
        {
            img.sprite = sprites[0];
            img.enabled = true;
        }
        else
        {
            img.enabled = false;   // no sprite for this slot in this state
        }
    }

    private static void EnableLayer(Image img, bool enabled)
    {
        if (img != null) img.enabled = enabled;
    }

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[HorseRiderVisual] '{name}': child '{childName}' not found. " +
                             "Create a child GameObject with that name and an Image component.", this);
            return null;
        }

        Image img = child.GetComponent<Image>();
        if (img == null)
            Debug.LogWarning($"[HorseRiderVisual] '{name}': child '{childName}' " +
                             "has no Image component.", this);
        return img;
    }

    private float FpsForState(AnimationState state) => state switch
    {
        AnimationState.HorseRun => horseRunFps,
        AnimationState.HorseFight => horseFightFps,
        AnimationState.HorseDead => horseDeadFps,
        _ => horseIdleFps,   // HorseIdle + anything else
    };
}