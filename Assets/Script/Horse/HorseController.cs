////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// Attach to the HorsePrefab (requires an Image component).
///////
/////// Animation states:
///////   Idle  — loops idleSprites continuously.
///////   Walk  — plays walkSprites for N full cycles, then automatically
///////            switches back to Idle. If walkCyclesBeforeIdle == 0 it
///////            loops walk forever.
/////// </summary>
////[RequireComponent(typeof(Image))]
////public class HorseController : MonoBehaviour
////{
////    public enum AnimState { Idle, Walk }

////    private Image _image;
////    private HorseData _data;
////    private AnimState _state = AnimState.Idle;
////    private float _timer = 0f;
////    private int _frame = 0;
////    private bool _playing = false;
////    private int _walkCyclesCompleted = 0;

////    // ── Unity lifecycle ───────────────────────────────────────────────────────

////    private void Awake() => _image = GetComponent<Image>();

////    private void Update()
////    {
////        if (!_playing || _data == null) return;

////        Sprite[] sprites = CurrentSprites();
////        float fps = CurrentFPS();

////        if (sprites == null || sprites.Length <= 1) return;

////        _timer += Time.deltaTime;
////        if (_timer < 1f / fps) return;
////        _timer = 0f;

////        _frame++;

////        if (_frame >= sprites.Length)
////        {
////            _frame = 0;

////            if (_state == AnimState.Walk)
////            {
////                _walkCyclesCompleted++;

////                if (_data.walkCyclesBeforeIdle > 0 &&
////                    _walkCyclesCompleted >= _data.walkCyclesBeforeIdle)
////                {
////                    SwitchToIdle();
////                    return;
////                }
////            }
////        }

////        _image.sprite = sprites[_frame];
////    }

////    // ── Public API ────────────────────────────────────────────────────────────

////    /// <summary>Call after spawning for a slot horse. Starts idle animation.</summary>
////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = AnimState.Idle;
////        ResetFrameState();
////        ShowFirstFrame();
////    }

////    /// <summary>
////    /// Call when the horse is dropped onto the HorseWalkZone.
////    /// Plays walk animation, then automatically switches to idle.
////    /// </summary>
////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = AnimState.Walk;
////        _walkCyclesCompleted = 0;
////        ResetFrameState();

////        // Fall back to idle sprites if no walk sprites are assigned
////        Sprite[] sprites = (data.walkSprites != null && data.walkSprites.Length > 0)
////            ? data.walkSprites
////            : data.idleSprites;

////        if (sprites != null && sprites.Length > 0)
////        {
////            _image.sprite = sprites[0];
////            _image.enabled = true;
////        }

////        _playing = true;
////    }

////    public void StopAnimation() => _playing = false;
////    public void PlayAnimation() => _playing = true;
////    public HorseData Data => _data;
////    public AnimState CurrentState => _state;

////    // ── Private helpers ───────────────────────────────────────────────────────

////    private Sprite[] CurrentSprites()
////    {
////        if (_state == AnimState.Walk &&
////            _data.walkSprites != null &&
////            _data.walkSprites.Length > 0)
////            return _data.walkSprites;

////        return _data.idleSprites;
////    }

////    private float CurrentFPS() =>
////        (_state == AnimState.Walk &&
////         _data.walkSprites != null &&
////         _data.walkSprites.Length > 0)
////            ? _data.walkFPS
////            : _data.idleFPS;

////    private void SwitchToIdle()
////    {
////        _state = AnimState.Idle;
////        ResetFrameState();

////        Sprite[] idle = _data.idleSprites;
////        if (idle != null && idle.Length > 0)
////            _image.sprite = idle[0];
////    }

////    private void ResetFrameState()
////    {
////        _frame = 0;
////        _timer = 0f;
////        _playing = true;
////    }

////    private void ShowFirstFrame()
////    {
////        Sprite[] sprites = CurrentSprites();
////        if (sprites != null && sprites.Length > 0)
////        {
////            _image.sprite = sprites[0];
////            _image.enabled = true;
////        }
////    }
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — HorseController  (complete rewrite)
/////
///// Attach to the HorsePrefab root alongside:
/////   RectTransform, Image, CanvasGroup
/////
///// ════════════════════════════════════════════════════════════════════
/////  HORSE PREFAB HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////           ├── Face    (Image)
/////           ├── Armor   (Image)
/////           ├── Helmet  (Image)
/////           └── Weapon  (Image)
/////
///// ════════════════════════════════════════════════════════════════════
/////  ANIMATION
///// ════════════════════════════════════════════════════════════════════
/////
/////  HorseController drives the horse Image(s) from HorseAnimationSO.
/////  When the state changes it also notifies:
/////    • HorseRiderVisual  → switches the 4 rider layer Images
/////    • SpriteLayerAnimator on the soldier → keeps the soldier's own
/////      animator in sync (for any system that still reads it)
/////
///// ════════════════════════════════════════════════════════════════════
/////  MOUNT / DISMOUNT
///// ════════════════════════════════════════════════════════════════════
/////
/////  Drop a soldier on the horse → OnDrop → PerformMount
/////  Call PerformDismount()      → from a UI button or external system
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP CHECKLIST
///// ════════════════════════════════════════════════════════════════════
/////
/////  □ HorseController + Image + CanvasGroup  on prefab root
/////  □ HorseAnimationSO asset assigned to horseAnimSO
/////  □ horseImage wired (or auto-found via GetComponent)
/////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////  □ Canvas root: GraphicRaycaster enabled
/////  □ Scene: EventSystem present
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class HorseController : MonoBehaviour, IDropHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Animation Data")]
//    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//             "Create via: right-click Project → Create → AreaForge → Horse Animation.")]
//    [SerializeField] private HorseAnimationSO horseAnimSO;

//    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//             "Leave null if your horse is a single-layer sprite.")]
//    [SerializeField] private HorseAnimationSO saddleAnimSO;

//    [Header("Image Layers")]
//    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//    [SerializeField] private Image horseImage;

//    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//    [SerializeField] private Image saddleImage;

//    [Header("Seat & Rider")]
//    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//             "Auto-found in children if left empty.")]
//    [SerializeField] private HorseSeat seat;

//    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//             "Auto-found in children if left empty.")]
//    [SerializeField] private HorseRiderVisual riderVisual;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private HorseState _state = HorseState.Idle;

//    // Per-layer animation timers
//    private float _horseTimer;
//    private float _saddleTimer;
//    private int _horseFrame;
//    private int _saddleFrame;

//    // Rider references (captured at mount time, cleared at dismount)
//    private SoldierDragDrop _mountedSoldier;
//    private SpriteLayerAnimator _riderAnimator;

//    // ── Lifecycle ─────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (horseImage == null)
//            horseImage = GetComponent<Image>();

//        if (seat == null)
//            seat = GetComponentInChildren<HorseSeat>();

//        if (riderVisual == null)
//            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//        if (horseImage == null)
//            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//        if (seat == null)
//            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//        if (riderVisual == null)
//            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//                             "Rider body-part layers will not animate.", this);
//    }

//    private void Start()
//    {
//        // Show frame 0 immediately so the horse doesn't appear blank
//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        // Make sure rider layers start hidden
//        riderVisual?.HideRider();
//    }

//    private void Update()
//    {
//        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

//        if (saddleImage != null && saddleAnimSO != null)
//            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
//    }

//    // ── Animation Engine ──────────────────────────────────────────────────────

//    /// <summary>Advances one Image layer's timer by dt and updates the sprite.</summary>
//    private void TickLayer(HorseAnimationSO so, Image img,
//                           ref int frame, ref float timer)
//    {
//        if (so == null || img == null) return;

//        HorseClip clip = so.GetClip(_state);
//        if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//        timer += Time.deltaTime;
//        float frameDuration = 1f / clip.fps;
//        if (timer < frameDuration) return;

//        timer -= frameDuration;   // carry-over keeps timing accurate

//        if (clip.loop)
//        {
//            frame = (frame + 1) % clip.frames.Length;
//        }
//        else
//        {
//            // Dead — play once, freeze on last frame
//            if (frame < clip.frames.Length - 1)
//                frame++;
//        }

//        img.sprite = clip.frames[frame];
//    }

//    /// <summary>Resets a layer to frame 0 of the current state immediately.</summary>
//    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
//    {
//        if (so == null || img == null) return;
//        HorseClip clip = so.GetClip(_state);
//        if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//        frame = 0;
//        img.sprite = clip.frames[0];
//    }

//    // ── Public API — State ────────────────────────────────────────────────────

//    /// <summary>Current animation state.</summary>
//    public HorseState CurrentState => _state;

//    /// <summary>True while a soldier is seated on this horse.</summary>
//    public bool IsOccupied => seat != null && seat.IsOccupied;

//    /// <summary>
//    /// Switches the horse (and mounted rider) to a new state.
//    /// Both the horse Images and all four rider body-part Images are updated.
//    /// </summary>
//    public void SetState(HorseState newState)
//    {
//        if (_state == newState) return;
//        _state = newState;

//        // Reset frame counters so the new clip starts from frame 0
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        // Map horse state → rider AnimationState and notify both systems
//        AnimationState riderState = MapToRiderState(newState);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] '{name}' → {newState}");
//    }

//    // Convenience shorthands — hook these to UI buttons or external controllers
//    public void SetIdle() => SetState(HorseState.Idle);
//    public void SetRun() => SetState(HorseState.Run);
//    public void SetFight() => SetState(HorseState.Fight);
//    public void SetDead() => SetState(HorseState.Dead);

//    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//    public HorseData Data => _data;
//    private HorseData _data;

//    /// <summary>
//    /// Called by HorseSlot to initialise a slotted horse.
//    /// Stores the HorseData reference and starts Idle animation.
//    /// </summary>
//    public void Setup(HorseData data)
//    {
//        _data = data;
//        SetState(HorseState.Idle);
//    }

//    /// <summary>
//    /// Called by HorseWalkZone to start the horse walking.
//    /// Stores the HorseData reference and switches to Run state.
//    /// After the zone finishes, call SetIdle() to return to Idle.
//    /// </summary>
//    public void SetupWalk(HorseData data)
//    {
//        _data = data;
//        SetState(HorseState.Run);
//    }

//    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//    /// <summary>
//    /// Accepts a soldier into the seat.
//    /// Called by OnDrop or externally (e.g. a formation spawner).
//    /// </summary>
//    public void PerformMount(SoldierDragDrop soldier)
//    {
//        if (seat == null)
//        {
//            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//            return;
//        }

//        if (seat.IsOccupied)
//        {
//            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//            return;
//        }

//        if (soldier == null) return;

//        // Cache references before MountOnHorse() reparents the soldier
//        _mountedSoldier = soldier;
//        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//        var equipment = soldier.GetComponent<CharacterEquipment>();

//        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//        seat.MountSoldier(soldier);

//        // Show the 4 rider Images using the soldier's equipped items
//        riderVisual?.ShowRider(equipment);

//        // Start in Idle state (state also notifies riderVisual & animator)
//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and resets the horse to Idle.
//    /// Wire this to a UI "Dismount" button or call it from an external system.
//    /// </summary>
//    public void PerformDismount()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        // Hide rider Images before the soldier is reparented away
//        riderVisual?.HideRider();

//        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//        seat.MountedSoldier.DismountFromHorse();
//        seat.ReleaseSoldier();

//        _mountedSoldier = null;
//        _riderAnimator = null;

//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//    }

//    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//    /// <summary>
//    /// Fired by Unity's EventSystem when a dragged object is released over
//    /// any Raycast-Target Image on this GameObject.
//    /// Accepts soldiers only; ignores anything else.
//    /// </summary>
//    public void OnDrop(PointerEventData eventData)
//    {
//        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//        if (soldier == null) return;

//        if (seat == null)
//        {
//            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//            return;
//        }

//        if (seat.IsOccupied)
//        {
//            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//            return;
//        }

//        PerformMount(soldier);
//    }

//    // ── Internal ──────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//    /// Safe to call when no rider is present (null-checked).
//    /// </summary>
//    private void NotifySoldierAnimator(AnimationState riderState)
//    {
//        _riderAnimator?.SetState(riderState);
//    }

//    /// <summary>Maps HorseState → the matching AnimationState for the soldier.</summary>
//    private static AnimationState MapToRiderState(HorseState state) => state switch
//    {
//        HorseState.Idle => AnimationState.HorseIdle,
//        HorseState.Run => AnimationState.HorseRun,
//        HorseState.Fight => AnimationState.HorseFight,
//        HorseState.Dead => AnimationState.HorseDead,
//        _ => AnimationState.HorseIdle,
//    };
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — HorseController  (fixed)
///
/// Attach to the HorsePrefab root alongside:
///   RectTransform, Image, CanvasGroup
///
/// ════════════════════════════════════════════════════════════════════
///  HORSE PREFAB HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Horse  ← HorseController + Image (horse body) + CanvasGroup
///     ├── SaddleLayer   (optional Image — saddle / bridle)
///     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///           ├── Face    (Image)
///           ├── Armor   (Image)
///           ├── Helmet  (Image)
///           └── Weapon  (Image)
///
/// ════════════════════════════════════════════════════════════════════
///  ANIMATION — TWO PATHS (auto-selected)
/// ════════════════════════════════════════════════════════════════════
///
///  PATH A  horseAnimSO assigned in Inspector
///    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///    → Full control per clip: custom fps, loop flag, frame array.
///
///  PATH B  horseAnimSO left null  (backward-compatible)
///    → Falls back to HorseData sprite arrays directly:
///        Idle  → HorseData.idleSprites  / idleFPS
///        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///    → This keeps every existing HorseData asset working without
///      requiring a HorseAnimationSO to be created first.
///
/// ════════════════════════════════════════════════════════════════════
///  BUG FIXES vs previous rewrite
/// ════════════════════════════════════════════════════════════════════
///
///  FIX 1 — Idle never played
///    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///    null, so HorseData.idleSprites were never shown.  Both methods now
///    fall back to HorseData when the SO is absent.
///
///  FIX 2 — Horse swap did nothing
///    Setup(HorseData) called SetState(HorseState.Idle) which contained
///    "if (_state == newState) return" — so swapping to a new horse while
///    already Idle skipped every frame update.
///    Setup() / SetupWalk() now force-reset the animation directly,
///    bypassing the equality guard entirely.
///
///  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///    HorseRiderVisual.ShowRider() / SetRiderState() must use
///    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///    sprite arrays are selected.  HorseController now always maps
///    HorseState → AnimationState correctly before notifying the rider.
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP CHECKLIST
/// ════════════════════════════════════════════════════════════════════
///
///  □ HorseController + Image + CanvasGroup  on prefab root
///  □ horseImage wired (or auto-found via GetComponent)
///  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///                   use HorseData sprite arrays (backward-compatible)
///  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///  □ Canvas root: GraphicRaycaster enabled
///  □ Scene: EventSystem present
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HorseController : MonoBehaviour, IDropHandler
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Animation Data")]
    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
             "(backward-compatible mode — no SO required).")]
    [SerializeField] private HorseAnimationSO horseAnimSO;

    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
             "Leave null if your horse is a single-layer sprite.")]
    [SerializeField] private HorseAnimationSO saddleAnimSO;

    [Header("Image Layers")]
    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
    [SerializeField] private Image horseImage;

    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
    [SerializeField] private Image saddleImage;

    [Header("Seat & Rider")]
    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
             "Auto-found in children if left empty.")]
    [SerializeField] private HorseSeat seat;

    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
             "Auto-found in children if left empty.")]
    [SerializeField] private HorseRiderVisual riderVisual;

    // ── Private state ─────────────────────────────────────────────────────────

    private HorseState _state = HorseState.Idle;

    // Per-layer animation timers
    private float _horseTimer;
    private float _saddleTimer;
    private int _horseFrame;
    private int _saddleFrame;

    // Rider references (captured at mount time, cleared at dismount)
    private SoldierDragDrop _mountedSoldier;
    private SpriteLayerAnimator _riderAnimator;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (horseImage == null)
            horseImage = GetComponent<Image>();

        if (seat == null)
            seat = GetComponentInChildren<HorseSeat>();

        if (riderVisual == null)
            riderVisual = GetComponentInChildren<HorseRiderVisual>();

        if (horseImage == null)
            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

        if (seat == null)
            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

        if (riderVisual == null)
            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
                             "Rider body-part layers will not animate.", this);
    }

    private void Start()
    {
        // Show frame 0 immediately so the horse doesn't appear blank.
        // Works whether horseAnimSO is assigned or not.
        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        // Make sure rider layers start hidden
        riderVisual?.HideRider();
    }

    private void Update()
    {
        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

        if (saddleImage != null && saddleAnimSO != null)
            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
    }

    // ── Animation Engine ──────────────────────────────────────────────────────

    /// <summary>
    /// Advances one Image layer's timer and updates the sprite.
    ///
    /// Priority:
    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
    ///   2. HorseData sprite array (if _data != null)
    ///   3. Early-return silently  (nothing to show yet)
    /// </summary>
    private void TickLayer(HorseAnimationSO so, Image img,
                           ref int frame, ref float timer)
    {
        if (img == null) return;

        // ── PATH A: SO-driven ────────────────────────────────────────────────
        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            timer += Time.deltaTime;
            if (timer < 1f / clip.fps) return;
            timer -= 1f / clip.fps;

            if (clip.loop)
                frame = (frame + 1) % clip.frames.Length;
            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
                frame++;

            img.sprite = clip.frames[frame];
            return;
        }

        // ── PATH B: HorseData fallback ───────────────────────────────────────
        // Only the main horseImage layer uses HorseData; the saddle layer has
        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
        if (_data == null || img != horseImage) return;

        Sprite[] sprites = GetDataSprites(_state);
        if (sprites == null || sprites.Length == 0) return;
        float fps = GetDataFPS(_state);

        timer += Time.deltaTime;
        if (timer < 1f / fps) return;
        timer -= 1f / fps;

        // Dead state: play once and freeze
        if (_state == HorseState.Dead)
        {
            if (frame < sprites.Length - 1) frame++;
        }
        else
        {
            frame = (frame + 1) % sprites.Length;
        }

        img.sprite = sprites[frame];
    }

    /// <summary>
    /// Resets a layer to frame 0 of the current state and shows it immediately.
    ///
    /// Same two-path priority as TickLayer.
    /// </summary>
    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
    {
        if (img == null) return;

        frame = 0;

        // ── PATH A: SO-driven ────────────────────────────────────────────────
        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            img.sprite = clip.frames[0];
            return;
        }

        // ── PATH B: HorseData fallback ───────────────────────────────────────
        if (_data == null || img != horseImage) return;

        Sprite[] sprites = GetDataSprites(_state);
        if (sprites != null && sprites.Length > 0)
            img.sprite = sprites[0];
    }

    // ── HorseData sprite / fps helpers ───────────────────────────────────────

    /// <summary>
    /// Maps a HorseState to the best available HorseData sprite array.
    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
    /// </summary>
    private Sprite[] GetDataSprites(HorseState state)
    {
        if (_data == null) return null;

        switch (state)
        {
            case HorseState.Run:
                // walkSprites → idleSprites
                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
                    ? _data.walkSprites
                    : _data.idleSprites;

            case HorseState.Fight:
                // No dedicated fight clip in HorseData — use idle
                return _data.idleSprites;

            case HorseState.Dead:
                // No dedicated dead clip in HorseData — freeze on idle frame 0
                return _data.idleSprites;

            default: // Idle
                return _data.idleSprites;
        }
    }

    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
    private float GetDataFPS(HorseState state)
    {
        if (_data == null) return 6f;

        return (state == HorseState.Run
                && _data.walkSprites != null
                && _data.walkSprites.Length > 0)
            ? _data.walkFPS
            : _data.idleFPS;
    }

    // ── Public API — State ────────────────────────────────────────────────────

    /// <summary>Current animation state.</summary>
    public HorseState CurrentState => _state;

    /// <summary>True while a soldier is seated on this horse.</summary>
    public bool IsOccupied => seat != null && seat.IsOccupied;

    /// <summary>
    /// Switches the horse (and mounted rider) to a new state.
    /// Both the horse Images and all four rider body-part Images are updated.
    /// Calling with the same state as the current one still resets to frame 0.
    /// </summary>
    public void SetState(HorseState newState)
    {
        _state = newState;

        // Reset frame counters so the new clip starts from frame 0
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        // Map horse state → rider AnimationState and notify both systems
        AnimationState riderState = MapToRiderState(newState);
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] '{name}' → {newState}");
    }

    // Convenience shorthands — hook these to UI buttons or external controllers
    public void SetIdle() => SetState(HorseState.Idle);
    public void SetRun() => SetState(HorseState.Run);
    public void SetFight() => SetState(HorseState.Fight);
    public void SetDead() => SetState(HorseState.Dead);

    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
    public HorseData Data => _data;
    private HorseData _data;

    /// <summary>
    /// Called by HorseSlot to initialise a slotted horse.
    /// Stores the HorseData reference and starts the Idle animation.
    ///
    /// FIX: Force-resets animation state directly instead of routing through
    /// SetState(), so swapping to a new HorseData while already in Idle
    /// correctly updates the displayed sprites instead of being a no-op.
    /// </summary>
    public void Setup(HorseData data)
    {
        _data = data;

        // Force full animation reset — bypasses the old equality guard so that
        // swapping horses (same state, new sprite array) always takes effect.
        _state = HorseState.Idle;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
        AnimationState riderState = MapToRiderState(HorseState.Idle);
        riderVisual?.SetRiderState(riderState);

        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
    }

    /// <summary>
    /// Called by HorseWalkZone to start the horse walking.
    /// Stores the HorseData reference and switches to Run state.
    /// After the zone finishes, call SetIdle() to return to Idle.
    ///
    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
    /// the zone assigns a new horse while the controller is already in Run.
    /// </summary>
    public void SetupWalk(HorseData data)
    {
        _data = data;

        _state = HorseState.Run;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        AnimationState riderState = MapToRiderState(HorseState.Run);
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
    }

    // ── Public API — Mount / Dismount ─────────────────────────────────────────

    /// <summary>
    /// Accepts a soldier into the seat.
    /// Called by OnDrop or externally (e.g. a formation spawner).
    ///
    /// The soldier's CharacterEquipment is read to populate the four rider
    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
    /// </summary>
    public void PerformMount(SoldierDragDrop soldier)
    {
        if (seat == null)
        {
            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
            return;
        }

        if (soldier == null) return;

        // Cache references before MountOnHorse() reparents the soldier
        _mountedSoldier = soldier;
        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
        var equipment = soldier.GetComponent<CharacterEquipment>();

        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
        seat.MountSoldier(soldier);

        // Show the 4 rider Images using the soldier's equipped items.
        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
        // for each of: Face, Armor, Helmet, Weapon.
        riderVisual?.ShowRider(equipment);

        // Start in Idle state (SetState also notifies riderVisual & animator)
        SetState(HorseState.Idle);

        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
    }

    /// <summary>
    /// Returns the soldier to the ground and resets the horse to Idle.
    /// Wire this to a UI "Dismount" button or call it from an external system.
    /// </summary>
    public void PerformDismount()
    {
        if (seat == null || !seat.IsOccupied) return;

        // Hide rider Images before the soldier is reparented away
        riderVisual?.HideRider();

        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
        seat.MountedSoldier.DismountFromHorse();
        seat.ReleaseSoldier();

        _mountedSoldier = null;
        _riderAnimator = null;

        SetState(HorseState.Idle);

        Debug.Log($"[HorseController] '{name}': rider dismounted.");
    }

    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

    /// <summary>
    /// Fired by Unity's EventSystem when a dragged object is released over
    /// any Raycast-Target Image on this GameObject.
    /// Accepts soldiers only; ignores anything else.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
        if (soldier == null) return;

        if (seat == null)
        {
            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
            return;
        }

        PerformMount(soldier);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
    /// Safe to call when no rider is present (null-checked).
    /// </summary>
    private void NotifySoldierAnimator(AnimationState riderState)
    {
        _riderAnimator?.SetState(riderState);
    }

    /// <summary>
    /// Maps HorseState → the matching AnimationState for the soldier.
    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
    /// both receive this mapped value so rider equipment sprites are selected
    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
    /// </summary>
    private static AnimationState MapToRiderState(HorseState state) => state switch
    {
        HorseState.Idle => AnimationState.HorseIdle,
        HorseState.Run => AnimationState.HorseRun,
        HorseState.Fight => AnimationState.HorseFight,
        HorseState.Dead => AnimationState.HorseDead,
        _ => AnimationState.HorseIdle,
    };
}