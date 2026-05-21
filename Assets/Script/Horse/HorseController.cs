////////////using UnityEngine;
////////////using UnityEngine.EventSystems;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// AREA FORGE — HorseController  (fixed)
///////////////
/////////////// Attach to the HorsePrefab root alongside:
///////////////   RectTransform, Image, CanvasGroup
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  HORSE PREFAB HIERARCHY
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////////           ├── Face    (Image)
///////////////           ├── Armor   (Image)
///////////////           ├── Helmet  (Image)
///////////////           └── Weapon  (Image)
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  ANIMATION — TWO PATHS (auto-selected)
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  PATH A  horseAnimSO assigned in Inspector
///////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////////    → Full control per clip: custom fps, loop flag, frame array.
///////////////
///////////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////////    → Falls back to HorseData sprite arrays directly:
///////////////        Idle  → HorseData.idleSprites  / idleFPS
///////////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///////////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///////////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///////////////    → This keeps every existing HorseData asset working without
///////////////      requiring a HorseAnimationSO to be created first.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  BUG FIXES vs previous rewrite
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  FIX 1 — Idle never played
///////////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///////////////    null, so HorseData.idleSprites were never shown.  Both methods now
///////////////    fall back to HorseData when the SO is absent.
///////////////
///////////////  FIX 2 — Horse swap did nothing
///////////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
///////////////    "if (_state == newState) return" — so swapping to a new horse while
///////////////    already Idle skipped every frame update.
///////////////    Setup() / SetupWalk() now force-reset the animation directly,
///////////////    bypassing the equality guard entirely.
///////////////
///////////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///////////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///////////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
///////////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///////////////    sprite arrays are selected.  HorseController now always maps
///////////////    HorseState → AnimationState correctly before notifying the rider.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  SETUP CHECKLIST
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////////  □ horseImage wired (or auto-found via GetComponent)
///////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////////                   use HorseData sprite arrays (backward-compatible)
///////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////////  □ Canvas root: GraphicRaycaster enabled
///////////////  □ Scene: EventSystem present
/////////////// </summary>
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class HorseController : MonoBehaviour, IDropHandler
////////////{
////////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////////    [Header("Animation Data")]
////////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////////             "(backward-compatible mode — no SO required).")]
////////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////////             "Leave null if your horse is a single-layer sprite.")]
////////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////////    [Header("Image Layers")]
////////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////////    [SerializeField] private Image horseImage;

////////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////////    [SerializeField] private Image saddleImage;

////////////    [Header("Seat & Rider")]
////////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////////             "Auto-found in children if left empty.")]
////////////    [SerializeField] private HorseSeat seat;

////////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////////             "Auto-found in children if left empty.")]
////////////    [SerializeField] private HorseRiderVisual riderVisual;

////////////    // ── Private state ─────────────────────────────────────────────────────────

////////////    private HorseState _state = HorseState.Idle;

////////////    // Per-layer animation timers
////////////    private float _horseTimer;
////////////    private float _saddleTimer;
////////////    private int _horseFrame;
////////////    private int _saddleFrame;

////////////    // Rider references (captured at mount time, cleared at dismount)
////////////    private SoldierDragDrop _mountedSoldier;
////////////    private SpriteLayerAnimator _riderAnimator;

////////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        if (horseImage == null)
////////////            horseImage = GetComponent<Image>();

////////////        if (seat == null)
////////////            seat = GetComponentInChildren<HorseSeat>();

////////////        if (riderVisual == null)
////////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////////        if (horseImage == null)
////////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////////        if (seat == null)
////////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////////        if (riderVisual == null)
////////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////////                             "Rider body-part layers will not animate.", this);
////////////    }

////////////    private void Start()
////////////    {
////////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////////        // Works whether horseAnimSO is assigned or not.
////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Make sure rider layers start hidden
////////////        riderVisual?.HideRider();
////////////    }

////////////    private void Update()
////////////    {
////////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
////////////    }

////////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Advances one Image layer's timer and updates the sprite.
////////////    ///
////////////    /// Priority:
////////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
////////////    ///   2. HorseData sprite array (if _data != null)
////////////    ///   3. Early-return silently  (nothing to show yet)
////////////    /// </summary>
////////////    private void TickLayer(HorseAnimationSO so, Image img,
////////////                           ref int frame, ref float timer)
////////////    {
////////////        if (img == null) return;

////////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////////        if (so != null)
////////////        {
////////////            HorseClip clip = so.GetClip(_state);
////////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////////            timer += Time.deltaTime;
////////////            if (timer < 1f / clip.fps) return;
////////////            timer -= 1f / clip.fps;

////////////            if (clip.loop)
////////////                frame = (frame + 1) % clip.frames.Length;
////////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////////                frame++;

////////////            img.sprite = clip.frames[frame];
////////////            return;
////////////        }

////////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////////        // Only the main horseImage layer uses HorseData; the saddle layer has
////////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
////////////        if (_data == null || img != horseImage) return;

////////////        Sprite[] sprites = GetDataSprites(_state);
////////////        if (sprites == null || sprites.Length == 0) return;
////////////        float fps = GetDataFPS(_state);

////////////        timer += Time.deltaTime;
////////////        if (timer < 1f / fps) return;
////////////        timer -= 1f / fps;

////////////        // Dead state: play once and freeze
////////////        if (_state == HorseState.Dead)
////////////        {
////////////            if (frame < sprites.Length - 1) frame++;
////////////        }
////////////        else
////////////        {
////////////            frame = (frame + 1) % sprites.Length;
////////////        }

////////////        img.sprite = sprites[frame];
////////////    }

////////////    /// <summary>
////////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////////    ///
////////////    /// Same two-path priority as TickLayer.
////////////    /// </summary>
////////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
////////////    {
////////////        if (img == null) return;

////////////        frame = 0;

////////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////////        if (so != null)
////////////        {
////////////            HorseClip clip = so.GetClip(_state);
////////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////////            img.sprite = clip.frames[0];
////////////            return;
////////////        }

////////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////////        if (_data == null || img != horseImage) return;

////////////        Sprite[] sprites = GetDataSprites(_state);
////////////        if (sprites != null && sprites.Length > 0)
////////////            img.sprite = sprites[0];
////////////    }

////////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

////////////    /// <summary>
////////////    /// Maps a HorseState to the best available HorseData sprite array.
////////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
////////////    /// </summary>
////////////    private Sprite[] GetDataSprites(HorseState state)
////////////    {
////////////        if (_data == null) return null;

////////////        switch (state)
////////////        {
////////////            case HorseState.Run:
////////////                // walkSprites → idleSprites
////////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
////////////                    ? _data.walkSprites
////////////                    : _data.idleSprites;

////////////            case HorseState.Fight:
////////////                // No dedicated fight clip in HorseData — use idle
////////////                return _data.idleSprites;

////////////            case HorseState.Dead:
////////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
////////////                return _data.idleSprites;

////////////            default: // Idle
////////////                return _data.idleSprites;
////////////        }
////////////    }

////////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
////////////    private float GetDataFPS(HorseState state)
////////////    {
////////////        if (_data == null) return 6f;

////////////        return (state == HorseState.Run
////////////                && _data.walkSprites != null
////////////                && _data.walkSprites.Length > 0)
////////////            ? _data.walkFPS
////////////            : _data.idleFPS;
////////////    }

////////////    // ── Public API — State ────────────────────────────────────────────────────

////////////    /// <summary>Current animation state.</summary>
////////////    public HorseState CurrentState => _state;

////////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////////    /// <summary>
////////////    /// Switches the horse (and mounted rider) to a new state.
////////////    /// Both the horse Images and all four rider body-part Images are updated.
////////////    /// Calling with the same state as the current one still resets to frame 0.
////////////    /// </summary>
////////////    public void SetState(HorseState newState)
////////////    {
////////////        _state = newState;

////////////        // Reset frame counters so the new clip starts from frame 0
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Map horse state → rider AnimationState and notify both systems
////////////        AnimationState riderState = MapToRiderState(newState);
////////////        riderVisual?.SetRiderState(riderState);
////////////        NotifySoldierAnimator(riderState);

////////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////////    }

////////////    // Convenience shorthands — hook these to UI buttons or external controllers
////////////    public void SetIdle() => SetState(HorseState.Idle);
////////////    public void SetRun() => SetState(HorseState.Run);
////////////    public void SetFight() => SetState(HorseState.Fight);
////////////    public void SetDead() => SetState(HorseState.Dead);

////////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////////    public HorseData Data => _data;
////////////    private HorseData _data;

////////////    /// <summary>
////////////    /// Called by HorseSlot to initialise a slotted horse.
////////////    /// Stores the HorseData reference and starts the Idle animation.
////////////    ///
////////////    /// FIX: Force-resets animation state directly instead of routing through
////////////    /// SetState(), so swapping to a new HorseData while already in Idle
////////////    /// correctly updates the displayed sprites instead of being a no-op.
////////////    /// </summary>
////////////    public void Setup(HorseData data)
////////////    {
////////////        _data = data;

////////////        // Force full animation reset — bypasses the old equality guard so that
////////////        // swapping horses (same state, new sprite array) always takes effect.
////////////        _state = HorseState.Idle;
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
////////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////////        riderVisual?.SetRiderState(riderState);

////////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////////    }

////////////    /// <summary>
////////////    /// Called by HorseWalkZone to start the horse walking.
////////////    /// Stores the HorseData reference and switches to Run state.
////////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////////    ///
////////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
////////////    /// the zone assigns a new horse while the controller is already in Run.
////////////    /// </summary>
////////////    public void SetupWalk(HorseData data)
////////////    {
////////////        _data = data;

////////////        _state = HorseState.Run;
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////////        riderVisual?.SetRiderState(riderState);
////////////        NotifySoldierAnimator(riderState);

////////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////////    }

////////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////////    /// <summary>
////////////    /// Accepts a soldier into the seat.
////////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////////    ///
////////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
////////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
////////////    /// </summary>
////////////    public void PerformMount(SoldierDragDrop soldier)
////////////    {
////////////        if (seat == null)
////////////        {
////////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////////            return;
////////////        }

////////////        if (seat.IsOccupied)
////////////        {
////////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////////            return;
////////////        }

////////////        if (soldier == null) return;

////////////        // Cache references before MountOnHorse() reparents the soldier
////////////        _mountedSoldier = soldier;
////////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////////        seat.MountSoldier(soldier);

////////////        // Show the 4 rider Images using the soldier's equipped items.
////////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
////////////        // for each of: Face, Armor, Helmet, Weapon.
////////////        riderVisual?.ShowRider(equipment);

////////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////////        SetState(HorseState.Idle);

////////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////////    }

////////////    /// <summary>
////////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////////    /// Wire this to a UI "Dismount" button or call it from an external system.
////////////    /// </summary>
////////////    public void PerformDismount()
////////////    {
////////////        if (seat == null || !seat.IsOccupied) return;

////////////        // Hide rider Images before the soldier is reparented away
////////////        riderVisual?.HideRider();

////////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////////        seat.MountedSoldier.DismountFromHorse();
////////////        seat.ReleaseSoldier();

////////////        _mountedSoldier = null;
////////////        _riderAnimator = null;

////////////        SetState(HorseState.Idle);

////////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////////    }

////////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////////    /// <summary>
////////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////////    /// any Raycast-Target Image on this GameObject.
////////////    /// Accepts soldiers only; ignores anything else.
////////////    /// </summary>
////////////    public void OnDrop(PointerEventData eventData)
////////////    {
////////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////////        if (soldier == null) return;

////////////        if (seat == null)
////////////        {
////////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////////            return;
////////////        }

////////////        if (seat.IsOccupied)
////////////        {
////////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////////            return;
////////////        }

////////////        PerformMount(soldier);
////////////    }

////////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////////    /// Safe to call when no rider is present (null-checked).
////////////    /// </summary>
////////////    private void NotifySoldierAnimator(AnimationState riderState)
////////////    {
////////////        _riderAnimator?.SetState(riderState);
////////////    }

////////////    /// <summary>
////////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////////    /// both receive this mapped value so rider equipment sprites are selected
////////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
////////////    /// </summary>
////////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////////    {
////////////        HorseState.Idle => AnimationState.HorseIdle,
////////////        HorseState.Run => AnimationState.HorseRun,
////////////        HorseState.Fight => AnimationState.HorseFight,
////////////        HorseState.Dead => AnimationState.HorseDead,
////////////        _ => AnimationState.HorseIdle,
////////////    };
////////////}

//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE — HorseController
/////////////
///////////// Attach to the HorsePrefab root alongside:
/////////////   RectTransform, Image, CanvasGroup
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  HORSE PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////////           ├── Face    (Image)
/////////////           ├── Armor   (Image)
/////////////           ├── Helmet  (Image)
/////////////           └── Weapon  (Image)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  ANIMATION — TWO PATHS (auto-selected)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  PATH A  horseAnimSO assigned in Inspector
/////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////////    → Full control per clip: custom fps, loop flag, frame array.
/////////////
/////////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////////    → Reads all four animation sets directly from HorseData:
/////////////        Idle   → idleSprites   / idleFPS     — loops forever
/////////////        Run    → runSprites    / runFPS       — auto-returns to Idle
/////////////                                                after runCyclesBeforeIdle loops
/////////////                                                (0 = loop forever)
/////////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
/////////////                                                after attackCyclesBeforeIdle loops
/////////////                                                (0 = loop forever)
/////////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
/////////////    → Falls back to idleSprites for any clip whose array is empty.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP CHECKLIST
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////////  □ horseImage wired (or auto-found via GetComponent)
/////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////////                   use HorseData sprite arrays (backward-compatible)
/////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////////  □ Canvas root: GraphicRaycaster enabled
/////////////  □ Scene: EventSystem present
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class HorseController : MonoBehaviour, IDropHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Animation Data")]
//////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////////             "(backward-compatible mode — all four clips supported).")]
//////////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////////             "Leave null if your horse is a single-layer sprite.")]
//////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////////    [Header("Image Layers")]
//////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////////    [SerializeField] private Image horseImage;

//////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////////    [SerializeField] private Image saddleImage;

//////////    [Header("Seat & Rider")]
//////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseSeat seat;

//////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseRiderVisual riderVisual;

//////////    // ── Private state ─────────────────────────────────────────────────────────

//////////    private HorseState _state = HorseState.Idle;

//////////    // Per-layer animation timers (used by both Path A and Path B)
//////////    private float _horseTimer;
//////////    private float _saddleTimer;
//////////    private int _horseFrame;
//////////    private int _saddleFrame;

//////////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
//////////    private int _dataCyclesCompleted;

//////////    // Rider references (captured at mount time, cleared at dismount)
//////////    private SoldierDragDrop _mountedSoldier;
//////////    private SpriteLayerAnimator _riderAnimator;

//////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (horseImage == null)
//////////            horseImage = GetComponent<Image>();

//////////        if (seat == null)
//////////            seat = GetComponentInChildren<HorseSeat>();

//////////        if (riderVisual == null)
//////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////////        if (horseImage == null)
//////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////////        if (seat == null)
//////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////////        if (riderVisual == null)
//////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////////                             "Rider body-part layers will not animate.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        // Show frame 0 immediately so the horse doesn't appear blank.
//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Rider layers start hidden until a soldier mounts
//////////        riderVisual?.HideRider();
//////////    }

//////////    private void Update()
//////////    {
//////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////////    }

//////////    // ── Animation Engine ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Advances one Image layer by dt and updates the sprite.
//////////    ///
//////////    /// Priority:
//////////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
//////////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
//////////    /// </summary>
//////////    private void TickLayer(HorseAnimationSO so, Image img,
//////////                           ref int frame, ref float timer,
//////////                           bool isMainLayer)
//////////    {
//////////        if (img == null) return;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////////            timer += Time.deltaTime;
//////////            if (timer < 1f / clip.fps) return;
//////////            timer -= 1f / clip.fps;

//////////            if (clip.loop)
//////////            {
//////////                frame = (frame + 1) % clip.frames.Length;
//////////            }
//////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////////            {
//////////                frame++;
//////////            }

//////////            img.sprite = clip.frames[frame];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
//////////        if (!isMainLayer || _data == null) return;

//////////        Sprite[] sprites = _data.GetSprites(_state);
//////////        if (sprites == null || sprites.Length == 0) return;

//////////        float fps = _data.GetFPS(_state);

//////////        timer += Time.deltaTime;
//////////        if (timer < 1f / fps) return;
//////////        timer -= 1f / fps;

//////////        switch (_state)
//////////        {
//////////            case HorseState.Dead:
//////////                // Play once — freeze on the last frame
//////////                if (frame < sprites.Length - 1)
//////////                    frame++;
//////////                break;

//////////            case HorseState.Run:
//////////            case HorseState.Fight:
//////////                // Advance frame; count completed cycles for auto-return to Idle
//////////                frame++;
//////////                if (frame >= sprites.Length)
//////////                {
//////////                    frame = 0;
//////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////////                    if (maxCycles > 0)
//////////                    {
//////////                        _dataCyclesCompleted++;
//////////                        if (_dataCyclesCompleted >= maxCycles)
//////////                            SetState(HorseState.Idle);   // auto-return
//////////                    }
//////////                }
//////////                break;

//////////            default: // Idle — loop forever
//////////                frame = (frame + 1) % sprites.Length;
//////////                break;
//////////        }

//////////        if (_state != HorseState.Idle || frame < sprites.Length)
//////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////////    }

//////////    /// <summary>
//////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////////    /// Same two-path priority as TickLayer.
//////////    /// </summary>
//////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////////                            bool isMainLayer = true)
//////////    {
//////////        if (img == null) return;

//////////        frame = 0;

//////////        // PATH A
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////////            img.sprite = clip.frames[0];
//////////            return;
//////////        }

//////////        // PATH B
//////////        if (!isMainLayer || _data == null) return;

//////////        Sprite[] sprites = _data.GetSprites(_state);
//////////        if (sprites != null && sprites.Length > 0)
//////////            img.sprite = sprites[0];
//////////    }

//////////    // ── Public API — State ────────────────────────────────────────────────────

//////////    /// <summary>Current animation state.</summary>
//////////    public HorseState CurrentState => _state;

//////////    /// <summary>True while a soldier is seated on this horse.</summary>
//////////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////////    /// <summary>
//////////    /// Switches the horse (and mounted rider) to a new animation state.
//////////    /// Resets to frame 0 every time — even when switching to the same state —
//////////    /// so swapping horse data always refreshes the displayed sprite.
//////////    /// </summary>
//////////    public void SetState(HorseState newState)
//////////    {
//////////        _state = newState;

//////////        // Reset counters so the new clip starts fresh
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        // Map horse state → rider AnimationState and notify both systems
//////////        AnimationState riderState = MapToRiderState(newState);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////////    }

//////////    // Convenience shorthands — wire to UI buttons or call from game systems
//////////    public void SetIdle() => SetState(HorseState.Idle);
//////////    public void SetRun() => SetState(HorseState.Run);
//////////    public void SetFight() => SetState(HorseState.Fight);
//////////    public void SetDead() => SetState(HorseState.Dead);

//////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////////    public HorseData Data => _data;
//////////    private HorseData _data;

//////////    /// <summary>
//////////    /// Called by HorseSlot to initialise a slotted horse.
//////////    /// Stores the HorseData reference and starts the Idle animation.
//////////    ///
//////////    /// Force-resets animation state directly so swapping to a new HorseData
//////////    /// while already in Idle correctly updates the displayed sprites.
//////////    /// </summary>
//////////    public void Setup(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Idle;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////////        riderVisual?.SetRiderState(riderState);

//////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////////    }

//////////    /// <summary>
//////////    /// Called by HorseWalkZone to start the horse running.
//////////    /// Stores the HorseData reference and switches to Run state.
//////////    /// After the zone finishes, call SetIdle() to return to Idle.
//////////    /// </summary>
//////////    public void SetupWalk(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Run;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////////    }

//////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////////    /// <summary>
//////////    /// Accepts a soldier into the seat.
//////////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////////    ///
//////////    /// The soldier's CharacterEquipment is read to populate the four rider
//////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////////            return;
//////////        }

//////////        if (soldier == null) return;

//////////        // Cache references before MountOnHorse() reparents the soldier
//////////        _mountedSoldier = soldier;
//////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////////        seat.MountSoldier(soldier);

//////////        // Show the 4 rider Images using the soldier's equipped items
//////////        riderVisual?.ShowRider(equipment);

//////////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////////    }

//////////    /// <summary>
//////////    /// Returns the soldier to the ground and resets the horse to Idle.
//////////    /// Wire this to a UI "Dismount" button or call from an external system.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (seat == null || !seat.IsOccupied) return;

//////////        // Hide rider Images before the soldier is reparented away
//////////        riderVisual?.HideRider();

//////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////////        seat.MountedSoldier.DismountFromHorse();
//////////        seat.ReleaseSoldier();

//////////        _mountedSoldier = null;
//////////        _riderAnimator = null;

//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////////    }

//////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////////    /// <summary>
//////////    /// Fired by Unity's EventSystem when a dragged object is released over
//////////    /// any Raycast-Target Image on this GameObject.
//////////    /// Accepts soldiers only; ignores anything else.
//////////    /// </summary>
//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////////        if (soldier == null) return;

//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////////            return;
//////////        }

//////////        PerformMount(soldier);
//////////    }

//////////    // ── Internal helpers ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////////    /// Safe to call when no rider is present (null-checked).
//////////    /// </summary>
//////////    private void NotifySoldierAnimator(AnimationState riderState)
//////////    {
//////////        _riderAnimator?.SetState(riderState);
//////////    }

//////////    /// <summary>
//////////    /// Maps HorseState → the matching AnimationState for the soldier.
//////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////////    /// both receive this mapped value so rider equipment sprites are selected
//////////    /// from the correct EquipmentItem horse arrays.
//////////    /// </summary>
//////////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////////    {
//////////        HorseState.Idle => AnimationState.HorseIdle,
//////////        HorseState.Run => AnimationState.HorseRun,
//////////        HorseState.Fight => AnimationState.HorseFight,
//////////        HorseState.Dead => AnimationState.HorseDead,
//////////        _ => AnimationState.HorseIdle,
//////////    };
//////////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Falls back to HorseData sprite arrays directly.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  RIDER RENDERING — SOLDIER'S OWN SPRITES
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
///////////  soldier's own SpriteLayerAnimator handles the mounted pose via
///////////  the HorseIdle / HorseRun AnimationStates.
///////////
///////////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
///////////  activated during mount — calling ShowRider while the soldier's
///////////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
///////////  soldier to appear on the horse.
///////////
///////////  If you want to use HorseRiderVisual instead, change MountOnHorse
///////////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
///////////  riderVisual?.ShowRider(equipment) line in PerformMount below.
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////////             "Auto-found in children if left empty.\n" +
////////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;
////////    private int _dataCyclesCompleted;

////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;
////////    private HorseData _data;

////////    // ── Public queries ────────────────────────────────────────────────────────

////////    public HorseData Data => _data;
////////    public HorseState CurrentState => _state;
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
////////    }

////////    private void Start()
////////    {
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer,
////////                           bool isMainLayer)
////////    {
////////        if (img == null) return;

////////        // PATH A: SO-driven
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////                frame = (frame + 1) % clip.frames.Length;
////////            else if (frame < clip.frames.Length - 1)
////////                frame++;

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // PATH B: HorseData fallback (main layer only)
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;

////////        float fps = _data.GetFPS(_state);
////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        switch (_state)
////////        {
////////            case HorseState.Dead:
////////                if (frame < sprites.Length - 1) frame++;
////////                break;

////////            case HorseState.Run:
////////            case HorseState.Fight:
////////                frame++;
////////                if (frame >= sprites.Length)
////////                {
////////                    frame = 0;
////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////////                    if (maxCycles > 0)
////////                    {
////////                        _dataCyclesCompleted++;
////////                        if (_dataCyclesCompleted >= maxCycles)
////////                            SetState(HorseState.Idle);
////////                    }
////////                }
////////                break;

////////            default:
////////                frame = (frame + 1) % sprites.Length;
////////                break;
////////        }

////////        if (frame < sprites.Length)
////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////////    }

////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////////                            bool isMainLayer = true)
////////    {
////////        if (img == null) return;
////////        frame = 0;

////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        if (!isMainLayer || _data == null) return;
////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    public void Setup(HorseData data)
////////    {
////////        _data = data;
////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;
////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop (IDropHandler) or externally.
////////    ///
////////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
////////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
////////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
////////    ///
////////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
////////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
////////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
////////    /// visuals appear — the "duplicate soldier" bug.
////////    ///
////////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
////////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

////////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
////////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
////////        seat.MountSoldier(soldier);

////////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
////////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
////////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
////////        // duplicate — two overlapping soldier visuals on the horse.
////////        // SetState below will still call riderVisual.SetRiderState() which is
////////        // harmless because HideRider was already called in Start().
////////        // ────────────────────────────────────────────────────────────────────────

////////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // HideRider is safe to call even though ShowRider was never called
////////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
////////        riderVisual?.HideRider();

////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController  (mount / equipment fix)
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  MOUNT FLOW (fixed)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. PerformMount(soldier)
/////////       → HorseSeat.MountSoldier(soldier)          [position fix here]
/////////           → SetParent(SoldierSeat, false)
/////////           → anchoredPosition = seatOffset
/////////           → soldier.MountOnHorse(seat)
/////////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
/////////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
/////////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
/////////       → SetState(HorseState.Idle)
/////////
/////////  RENDERING PATH DECISION
/////////  ───────────────────────
/////////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
/////////  rider's equipment, and hide the soldier's own CanvasGroup so only
/////////  one visual is visible. This avoids the "duplicate soldier" bug.
/////////
/////////  If you prefer the soldier's own SpriteLayerAnimator to drive
/////////  everything (and skip the 4 seat Images), reverse the two lines
/////////  flagged RENDERING_CHOICE below.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  BUG FIXES vs previous version
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  FIX 1 — Soldier jumps to wrong position on drop
/////////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
/////////    soldier's anchoredPosition is set explicitly instead of being
/////////    derived from its drag-release screen coordinate.
/////////
/////////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
/////////    PerformMount now calls riderVisual.ShowRider(equipment) after
/////////    hiding the soldier's own CanvasGroup. The 4 seat Images are
/////////    populated from the soldier's CharacterEquipment and animated by
/////////    NotifySoldierAnimator(HorseIdle).
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//////             "Auto-found in children if left empty.\n" +
//////             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;
//////    private int _dataCyclesCompleted;

//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;
//////    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
//////    private HorseData _data;

//////    // ── Public queries ────────────────────────────────────────────────────────

//////    public HorseData Data => _data;
//////    public HorseState CurrentState => _state;
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
//////    }

//////    private void Start()
//////    {
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // PATH A: SO-driven
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // PATH B: HorseData fallback (main layer only)
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);
//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                if (frame < sprites.Length - 1) frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);
//////                    }
//////                }
//////                break;

//////            default:
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;
//////        frame = 0;

//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        if (!isMainLayer || _data == null) return;
//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    public void Setup(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    ///
//////    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
//////    ///
//////    ///  Step 1  HorseSeat.MountSoldier(soldier)
//////    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
//////    ///          → anchoredPosition = seatOffset
//////    ///          → soldier.MountOnHorse(seat)
//////    ///
//////    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
//////    ///          Prevents the "duplicate soldier" — the soldier's body is now
//////    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
//////    ///          will show.                                            [FIX 2]
//////    ///
//////    ///  Step 3  riderVisual.ShowRider(equipment)
//////    ///          Populates Face / Helmet / Weapon / Armor Images from the
//////    ///          soldier's CharacterEquipment.                         [FIX 2]
//////    ///
//////    ///  Step 4  NotifySoldierAnimator(HorseIdle)
//////    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
//////    ///          equipment sprites animate in the mounted pose.
//////    ///
//////    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
//////    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
//////    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
//////    ///  path instead:
//////    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
//////    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
//////    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache before reparenting
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
//////        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
//////        // snaps to seatOffset instead of jumping to its drag-release position.
//////        seat.MountSoldier(soldier);

//////        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
//////        // Comment this line out if you want the soldier's own SpriteLayerAnimator
//////        // to drive everything instead of the 4 seat Images.
//////        if (_soldierCanvasGroup != null)
//////            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

//////        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
//////        // Comment this line out if using the "soldier's own visuals" path.
//////        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

//////        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        NotifySoldierAnimator(riderState);
//////        riderVisual?.SetRiderState(riderState);

//////        // Horse itself switches to Idle (also re-notifies rider — harmless)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
//////                  $"Equipment shown via HorseRiderVisual.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide the 4 seat Images
//////        riderVisual?.HideRider();

//////        // Restore the soldier's own CanvasGroup so it is visible on the ground
//////        if (_soldierCanvasGroup != null)
//////            _soldierCanvasGroup.alpha = 1f;

//////        // Reparent the soldier back to its original parent + restore ground state
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;
//////        _soldierCanvasGroup = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler ──────────────────────────────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////////////using UnityEngine;
////////////using UnityEngine.EventSystems;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// AREA FORGE — HorseController  (fixed)
///////////////
/////////////// Attach to the HorsePrefab root alongside:
///////////////   RectTransform, Image, CanvasGroup
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  HORSE PREFAB HIERARCHY
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////////           ├── Face    (Image)
///////////////           ├── Armor   (Image)
///////////////           ├── Helmet  (Image)
///////////////           └── Weapon  (Image)
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  ANIMATION — TWO PATHS (auto-selected)
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  PATH A  horseAnimSO assigned in Inspector
///////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////////    → Full control per clip: custom fps, loop flag, frame array.
///////////////
///////////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////////    → Falls back to HorseData sprite arrays directly:
///////////////        Idle  → HorseData.idleSprites  / idleFPS
///////////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///////////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///////////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///////////////    → This keeps every existing HorseData asset working without
///////////////      requiring a HorseAnimationSO to be created first.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  BUG FIXES vs previous rewrite
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  FIX 1 — Idle never played
///////////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///////////////    null, so HorseData.idleSprites were never shown.  Both methods now
///////////////    fall back to HorseData when the SO is absent.
///////////////
///////////////  FIX 2 — Horse swap did nothing
///////////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
///////////////    "if (_state == newState) return" — so swapping to a new horse while
///////////////    already Idle skipped every frame update.
///////////////    Setup() / SetupWalk() now force-reset the animation directly,
///////////////    bypassing the equality guard entirely.
///////////////
///////////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///////////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///////////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
///////////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///////////////    sprite arrays are selected.  HorseController now always maps
///////////////    HorseState → AnimationState correctly before notifying the rider.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  SETUP CHECKLIST
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////////  □ horseImage wired (or auto-found via GetComponent)
///////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////////                   use HorseData sprite arrays (backward-compatible)
///////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////////  □ Canvas root: GraphicRaycaster enabled
///////////////  □ Scene: EventSystem present
/////////////// </summary>
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class HorseController : MonoBehaviour, IDropHandler
////////////{
////////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////////    [Header("Animation Data")]
////////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////////             "(backward-compatible mode — no SO required).")]
////////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////////             "Leave null if your horse is a single-layer sprite.")]
////////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////////    [Header("Image Layers")]
////////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////////    [SerializeField] private Image horseImage;

////////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////////    [SerializeField] private Image saddleImage;

////////////    [Header("Seat & Rider")]
////////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////////             "Auto-found in children if left empty.")]
////////////    [SerializeField] private HorseSeat seat;

////////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////////             "Auto-found in children if left empty.")]
////////////    [SerializeField] private HorseRiderVisual riderVisual;

////////////    // ── Private state ─────────────────────────────────────────────────────────

////////////    private HorseState _state = HorseState.Idle;

////////////    // Per-layer animation timers
////////////    private float _horseTimer;
////////////    private float _saddleTimer;
////////////    private int _horseFrame;
////////////    private int _saddleFrame;

////////////    // Rider references (captured at mount time, cleared at dismount)
////////////    private SoldierDragDrop _mountedSoldier;
////////////    private SpriteLayerAnimator _riderAnimator;

////////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        if (horseImage == null)
////////////            horseImage = GetComponent<Image>();

////////////        if (seat == null)
////////////            seat = GetComponentInChildren<HorseSeat>();

////////////        if (riderVisual == null)
////////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////////        if (horseImage == null)
////////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////////        if (seat == null)
////////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////////        if (riderVisual == null)
////////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////////                             "Rider body-part layers will not animate.", this);
////////////    }

////////////    private void Start()
////////////    {
////////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////////        // Works whether horseAnimSO is assigned or not.
////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Make sure rider layers start hidden
////////////        riderVisual?.HideRider();
////////////    }

////////////    private void Update()
////////////    {
////////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
////////////    }

////////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Advances one Image layer's timer and updates the sprite.
////////////    ///
////////////    /// Priority:
////////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
////////////    ///   2. HorseData sprite array (if _data != null)
////////////    ///   3. Early-return silently  (nothing to show yet)
////////////    /// </summary>
////////////    private void TickLayer(HorseAnimationSO so, Image img,
////////////                           ref int frame, ref float timer)
////////////    {
////////////        if (img == null) return;

////////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////////        if (so != null)
////////////        {
////////////            HorseClip clip = so.GetClip(_state);
////////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////////            timer += Time.deltaTime;
////////////            if (timer < 1f / clip.fps) return;
////////////            timer -= 1f / clip.fps;

////////////            if (clip.loop)
////////////                frame = (frame + 1) % clip.frames.Length;
////////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////////                frame++;

////////////            img.sprite = clip.frames[frame];
////////////            return;
////////////        }

////////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////////        // Only the main horseImage layer uses HorseData; the saddle layer has
////////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
////////////        if (_data == null || img != horseImage) return;

////////////        Sprite[] sprites = GetDataSprites(_state);
////////////        if (sprites == null || sprites.Length == 0) return;
////////////        float fps = GetDataFPS(_state);

////////////        timer += Time.deltaTime;
////////////        if (timer < 1f / fps) return;
////////////        timer -= 1f / fps;

////////////        // Dead state: play once and freeze
////////////        if (_state == HorseState.Dead)
////////////        {
////////////            if (frame < sprites.Length - 1) frame++;
////////////        }
////////////        else
////////////        {
////////////            frame = (frame + 1) % sprites.Length;
////////////        }

////////////        img.sprite = sprites[frame];
////////////    }

////////////    /// <summary>
////////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////////    ///
////////////    /// Same two-path priority as TickLayer.
////////////    /// </summary>
////////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
////////////    {
////////////        if (img == null) return;

////////////        frame = 0;

////////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////////        if (so != null)
////////////        {
////////////            HorseClip clip = so.GetClip(_state);
////////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////////            img.sprite = clip.frames[0];
////////////            return;
////////////        }

////////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////////        if (_data == null || img != horseImage) return;

////////////        Sprite[] sprites = GetDataSprites(_state);
////////////        if (sprites != null && sprites.Length > 0)
////////////            img.sprite = sprites[0];
////////////    }

////////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

////////////    /// <summary>
////////////    /// Maps a HorseState to the best available HorseData sprite array.
////////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
////////////    /// </summary>
////////////    private Sprite[] GetDataSprites(HorseState state)
////////////    {
////////////        if (_data == null) return null;

////////////        switch (state)
////////////        {
////////////            case HorseState.Run:
////////////                // walkSprites → idleSprites
////////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
////////////                    ? _data.walkSprites
////////////                    : _data.idleSprites;

////////////            case HorseState.Fight:
////////////                // No dedicated fight clip in HorseData — use idle
////////////                return _data.idleSprites;

////////////            case HorseState.Dead:
////////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
////////////                return _data.idleSprites;

////////////            default: // Idle
////////////                return _data.idleSprites;
////////////        }
////////////    }

////////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
////////////    private float GetDataFPS(HorseState state)
////////////    {
////////////        if (_data == null) return 6f;

////////////        return (state == HorseState.Run
////////////                && _data.walkSprites != null
////////////                && _data.walkSprites.Length > 0)
////////////            ? _data.walkFPS
////////////            : _data.idleFPS;
////////////    }

////////////    // ── Public API — State ────────────────────────────────────────────────────

////////////    /// <summary>Current animation state.</summary>
////////////    public HorseState CurrentState => _state;

////////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////////    /// <summary>
////////////    /// Switches the horse (and mounted rider) to a new state.
////////////    /// Both the horse Images and all four rider body-part Images are updated.
////////////    /// Calling with the same state as the current one still resets to frame 0.
////////////    /// </summary>
////////////    public void SetState(HorseState newState)
////////////    {
////////////        _state = newState;

////////////        // Reset frame counters so the new clip starts from frame 0
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Map horse state → rider AnimationState and notify both systems
////////////        AnimationState riderState = MapToRiderState(newState);
////////////        riderVisual?.SetRiderState(riderState);
////////////        NotifySoldierAnimator(riderState);

////////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////////    }

////////////    // Convenience shorthands — hook these to UI buttons or external controllers
////////////    public void SetIdle() => SetState(HorseState.Idle);
////////////    public void SetRun() => SetState(HorseState.Run);
////////////    public void SetFight() => SetState(HorseState.Fight);
////////////    public void SetDead() => SetState(HorseState.Dead);

////////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////////    public HorseData Data => _data;
////////////    private HorseData _data;

////////////    /// <summary>
////////////    /// Called by HorseSlot to initialise a slotted horse.
////////////    /// Stores the HorseData reference and starts the Idle animation.
////////////    ///
////////////    /// FIX: Force-resets animation state directly instead of routing through
////////////    /// SetState(), so swapping to a new HorseData while already in Idle
////////////    /// correctly updates the displayed sprites instead of being a no-op.
////////////    /// </summary>
////////////    public void Setup(HorseData data)
////////////    {
////////////        _data = data;

////////////        // Force full animation reset — bypasses the old equality guard so that
////////////        // swapping horses (same state, new sprite array) always takes effect.
////////////        _state = HorseState.Idle;
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
////////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////////        riderVisual?.SetRiderState(riderState);

////////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////////    }

////////////    /// <summary>
////////////    /// Called by HorseWalkZone to start the horse walking.
////////////    /// Stores the HorseData reference and switches to Run state.
////////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////////    ///
////////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
////////////    /// the zone assigns a new horse while the controller is already in Run.
////////////    /// </summary>
////////////    public void SetupWalk(HorseData data)
////////////    {
////////////        _data = data;

////////////        _state = HorseState.Run;
////////////        _horseFrame = _saddleFrame = 0;
////////////        _horseTimer = _saddleTimer = 0f;

////////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////////        if (saddleImage != null && saddleAnimSO != null)
////////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////////        riderVisual?.SetRiderState(riderState);
////////////        NotifySoldierAnimator(riderState);

////////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////////    }

////////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////////    /// <summary>
////////////    /// Accepts a soldier into the seat.
////////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////////    ///
////////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
////////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
////////////    /// </summary>
////////////    public void PerformMount(SoldierDragDrop soldier)
////////////    {
////////////        if (seat == null)
////////////        {
////////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////////            return;
////////////        }

////////////        if (seat.IsOccupied)
////////////        {
////////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////////            return;
////////////        }

////////////        if (soldier == null) return;

////////////        // Cache references before MountOnHorse() reparents the soldier
////////////        _mountedSoldier = soldier;
////////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////////        seat.MountSoldier(soldier);

////////////        // Show the 4 rider Images using the soldier's equipped items.
////////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
////////////        // for each of: Face, Armor, Helmet, Weapon.
////////////        riderVisual?.ShowRider(equipment);

////////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////////        SetState(HorseState.Idle);

////////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////////    }

////////////    /// <summary>
////////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////////    /// Wire this to a UI "Dismount" button or call it from an external system.
////////////    /// </summary>
////////////    public void PerformDismount()
////////////    {
////////////        if (seat == null || !seat.IsOccupied) return;

////////////        // Hide rider Images before the soldier is reparented away
////////////        riderVisual?.HideRider();

////////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////////        seat.MountedSoldier.DismountFromHorse();
////////////        seat.ReleaseSoldier();

////////////        _mountedSoldier = null;
////////////        _riderAnimator = null;

////////////        SetState(HorseState.Idle);

////////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////////    }

////////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////////    /// <summary>
////////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////////    /// any Raycast-Target Image on this GameObject.
////////////    /// Accepts soldiers only; ignores anything else.
////////////    /// </summary>
////////////    public void OnDrop(PointerEventData eventData)
////////////    {
////////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////////        if (soldier == null) return;

////////////        if (seat == null)
////////////        {
////////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////////            return;
////////////        }

////////////        if (seat.IsOccupied)
////////////        {
////////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////////            return;
////////////        }

////////////        PerformMount(soldier);
////////////    }

////////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////////    /// Safe to call when no rider is present (null-checked).
////////////    /// </summary>
////////////    private void NotifySoldierAnimator(AnimationState riderState)
////////////    {
////////////        _riderAnimator?.SetState(riderState);
////////////    }

////////////    /// <summary>
////////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////////    /// both receive this mapped value so rider equipment sprites are selected
////////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
////////////    /// </summary>
////////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////////    {
////////////        HorseState.Idle => AnimationState.HorseIdle,
////////////        HorseState.Run => AnimationState.HorseRun,
////////////        HorseState.Fight => AnimationState.HorseFight,
////////////        HorseState.Dead => AnimationState.HorseDead,
////////////        _ => AnimationState.HorseIdle,
////////////    };
////////////}

//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE — HorseController
/////////////
///////////// Attach to the HorsePrefab root alongside:
/////////////   RectTransform, Image, CanvasGroup
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  HORSE PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////////           ├── Face    (Image)
/////////////           ├── Armor   (Image)
/////////////           ├── Helmet  (Image)
/////////////           └── Weapon  (Image)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  ANIMATION — TWO PATHS (auto-selected)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  PATH A  horseAnimSO assigned in Inspector
/////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////////    → Full control per clip: custom fps, loop flag, frame array.
/////////////
/////////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////////    → Reads all four animation sets directly from HorseData:
/////////////        Idle   → idleSprites   / idleFPS     — loops forever
/////////////        Run    → runSprites    / runFPS       — auto-returns to Idle
/////////////                                                after runCyclesBeforeIdle loops
/////////////                                                (0 = loop forever)
/////////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
/////////////                                                after attackCyclesBeforeIdle loops
/////////////                                                (0 = loop forever)
/////////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
/////////////    → Falls back to idleSprites for any clip whose array is empty.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP CHECKLIST
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////////  □ horseImage wired (or auto-found via GetComponent)
/////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////////                   use HorseData sprite arrays (backward-compatible)
/////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////////  □ Canvas root: GraphicRaycaster enabled
/////////////  □ Scene: EventSystem present
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class HorseController : MonoBehaviour, IDropHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Animation Data")]
//////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////////             "(backward-compatible mode — all four clips supported).")]
//////////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////////             "Leave null if your horse is a single-layer sprite.")]
//////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////////    [Header("Image Layers")]
//////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////////    [SerializeField] private Image horseImage;

//////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////////    [SerializeField] private Image saddleImage;

//////////    [Header("Seat & Rider")]
//////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseSeat seat;

//////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseRiderVisual riderVisual;

//////////    // ── Private state ─────────────────────────────────────────────────────────

//////////    private HorseState _state = HorseState.Idle;

//////////    // Per-layer animation timers (used by both Path A and Path B)
//////////    private float _horseTimer;
//////////    private float _saddleTimer;
//////////    private int _horseFrame;
//////////    private int _saddleFrame;

//////////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
//////////    private int _dataCyclesCompleted;

//////////    // Rider references (captured at mount time, cleared at dismount)
//////////    private SoldierDragDrop _mountedSoldier;
//////////    private SpriteLayerAnimator _riderAnimator;

//////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (horseImage == null)
//////////            horseImage = GetComponent<Image>();

//////////        if (seat == null)
//////////            seat = GetComponentInChildren<HorseSeat>();

//////////        if (riderVisual == null)
//////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////////        if (horseImage == null)
//////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////////        if (seat == null)
//////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////////        if (riderVisual == null)
//////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////////                             "Rider body-part layers will not animate.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        // Show frame 0 immediately so the horse doesn't appear blank.
//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Rider layers start hidden until a soldier mounts
//////////        riderVisual?.HideRider();
//////////    }

//////////    private void Update()
//////////    {
//////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////////    }

//////////    // ── Animation Engine ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Advances one Image layer by dt and updates the sprite.
//////////    ///
//////////    /// Priority:
//////////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
//////////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
//////////    /// </summary>
//////////    private void TickLayer(HorseAnimationSO so, Image img,
//////////                           ref int frame, ref float timer,
//////////                           bool isMainLayer)
//////////    {
//////////        if (img == null) return;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////////            timer += Time.deltaTime;
//////////            if (timer < 1f / clip.fps) return;
//////////            timer -= 1f / clip.fps;

//////////            if (clip.loop)
//////////            {
//////////                frame = (frame + 1) % clip.frames.Length;
//////////            }
//////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////////            {
//////////                frame++;
//////////            }

//////////            img.sprite = clip.frames[frame];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
//////////        if (!isMainLayer || _data == null) return;

//////////        Sprite[] sprites = _data.GetSprites(_state);
//////////        if (sprites == null || sprites.Length == 0) return;

//////////        float fps = _data.GetFPS(_state);

//////////        timer += Time.deltaTime;
//////////        if (timer < 1f / fps) return;
//////////        timer -= 1f / fps;

//////////        switch (_state)
//////////        {
//////////            case HorseState.Dead:
//////////                // Play once — freeze on the last frame
//////////                if (frame < sprites.Length - 1)
//////////                    frame++;
//////////                break;

//////////            case HorseState.Run:
//////////            case HorseState.Fight:
//////////                // Advance frame; count completed cycles for auto-return to Idle
//////////                frame++;
//////////                if (frame >= sprites.Length)
//////////                {
//////////                    frame = 0;
//////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////////                    if (maxCycles > 0)
//////////                    {
//////////                        _dataCyclesCompleted++;
//////////                        if (_dataCyclesCompleted >= maxCycles)
//////////                            SetState(HorseState.Idle);   // auto-return
//////////                    }
//////////                }
//////////                break;

//////////            default: // Idle — loop forever
//////////                frame = (frame + 1) % sprites.Length;
//////////                break;
//////////        }

//////////        if (_state != HorseState.Idle || frame < sprites.Length)
//////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////////    }

//////////    /// <summary>
//////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////////    /// Same two-path priority as TickLayer.
//////////    /// </summary>
//////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////////                            bool isMainLayer = true)
//////////    {
//////////        if (img == null) return;

//////////        frame = 0;

//////////        // PATH A
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////////            img.sprite = clip.frames[0];
//////////            return;
//////////        }

//////////        // PATH B
//////////        if (!isMainLayer || _data == null) return;

//////////        Sprite[] sprites = _data.GetSprites(_state);
//////////        if (sprites != null && sprites.Length > 0)
//////////            img.sprite = sprites[0];
//////////    }

//////////    // ── Public API — State ────────────────────────────────────────────────────

//////////    /// <summary>Current animation state.</summary>
//////////    public HorseState CurrentState => _state;

//////////    /// <summary>True while a soldier is seated on this horse.</summary>
//////////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////////    /// <summary>
//////////    /// Switches the horse (and mounted rider) to a new animation state.
//////////    /// Resets to frame 0 every time — even when switching to the same state —
//////////    /// so swapping horse data always refreshes the displayed sprite.
//////////    /// </summary>
//////////    public void SetState(HorseState newState)
//////////    {
//////////        _state = newState;

//////////        // Reset counters so the new clip starts fresh
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        // Map horse state → rider AnimationState and notify both systems
//////////        AnimationState riderState = MapToRiderState(newState);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////////    }

//////////    // Convenience shorthands — wire to UI buttons or call from game systems
//////////    public void SetIdle() => SetState(HorseState.Idle);
//////////    public void SetRun() => SetState(HorseState.Run);
//////////    public void SetFight() => SetState(HorseState.Fight);
//////////    public void SetDead() => SetState(HorseState.Dead);

//////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////////    public HorseData Data => _data;
//////////    private HorseData _data;

//////////    /// <summary>
//////////    /// Called by HorseSlot to initialise a slotted horse.
//////////    /// Stores the HorseData reference and starts the Idle animation.
//////////    ///
//////////    /// Force-resets animation state directly so swapping to a new HorseData
//////////    /// while already in Idle correctly updates the displayed sprites.
//////////    /// </summary>
//////////    public void Setup(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Idle;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////////        riderVisual?.SetRiderState(riderState);

//////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////////    }

//////////    /// <summary>
//////////    /// Called by HorseWalkZone to start the horse running.
//////////    /// Stores the HorseData reference and switches to Run state.
//////////    /// After the zone finishes, call SetIdle() to return to Idle.
//////////    /// </summary>
//////////    public void SetupWalk(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Run;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;
//////////        _dataCyclesCompleted = 0;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////////    }

//////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////////    /// <summary>
//////////    /// Accepts a soldier into the seat.
//////////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////////    ///
//////////    /// The soldier's CharacterEquipment is read to populate the four rider
//////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////////            return;
//////////        }

//////////        if (soldier == null) return;

//////////        // Cache references before MountOnHorse() reparents the soldier
//////////        _mountedSoldier = soldier;
//////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////////        seat.MountSoldier(soldier);

//////////        // Show the 4 rider Images using the soldier's equipped items
//////////        riderVisual?.ShowRider(equipment);

//////////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////////    }

//////////    /// <summary>
//////////    /// Returns the soldier to the ground and resets the horse to Idle.
//////////    /// Wire this to a UI "Dismount" button or call from an external system.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (seat == null || !seat.IsOccupied) return;

//////////        // Hide rider Images before the soldier is reparented away
//////////        riderVisual?.HideRider();

//////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////////        seat.MountedSoldier.DismountFromHorse();
//////////        seat.ReleaseSoldier();

//////////        _mountedSoldier = null;
//////////        _riderAnimator = null;

//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////////    }

//////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////////    /// <summary>
//////////    /// Fired by Unity's EventSystem when a dragged object is released over
//////////    /// any Raycast-Target Image on this GameObject.
//////////    /// Accepts soldiers only; ignores anything else.
//////////    /// </summary>
//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////////        if (soldier == null) return;

//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////////            return;
//////////        }

//////////        PerformMount(soldier);
//////////    }

//////////    // ── Internal helpers ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////////    /// Safe to call when no rider is present (null-checked).
//////////    /// </summary>
//////////    private void NotifySoldierAnimator(AnimationState riderState)
//////////    {
//////////        _riderAnimator?.SetState(riderState);
//////////    }

//////////    /// <summary>
//////////    /// Maps HorseState → the matching AnimationState for the soldier.
//////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////////    /// both receive this mapped value so rider equipment sprites are selected
//////////    /// from the correct EquipmentItem horse arrays.
//////////    /// </summary>
//////////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////////    {
//////////        HorseState.Idle => AnimationState.HorseIdle,
//////////        HorseState.Run => AnimationState.HorseRun,
//////////        HorseState.Fight => AnimationState.HorseFight,
//////////        HorseState.Dead => AnimationState.HorseDead,
//////////        _ => AnimationState.HorseIdle,
//////////    };
//////////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Falls back to HorseData sprite arrays directly.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  RIDER RENDERING — SOLDIER'S OWN SPRITES
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
///////////  soldier's own SpriteLayerAnimator handles the mounted pose via
///////////  the HorseIdle / HorseRun AnimationStates.
///////////
///////////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
///////////  activated during mount — calling ShowRider while the soldier's
///////////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
///////////  soldier to appear on the horse.
///////////
///////////  If you want to use HorseRiderVisual instead, change MountOnHorse
///////////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
///////////  riderVisual?.ShowRider(equipment) line in PerformMount below.
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////////             "Auto-found in children if left empty.\n" +
////////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;
////////    private int _dataCyclesCompleted;

////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;
////////    private HorseData _data;

////////    // ── Public queries ────────────────────────────────────────────────────────

////////    public HorseData Data => _data;
////////    public HorseState CurrentState => _state;
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
////////    }

////////    private void Start()
////////    {
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer,
////////                           bool isMainLayer)
////////    {
////////        if (img == null) return;

////////        // PATH A: SO-driven
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////                frame = (frame + 1) % clip.frames.Length;
////////            else if (frame < clip.frames.Length - 1)
////////                frame++;

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // PATH B: HorseData fallback (main layer only)
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;

////////        float fps = _data.GetFPS(_state);
////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        switch (_state)
////////        {
////////            case HorseState.Dead:
////////                if (frame < sprites.Length - 1) frame++;
////////                break;

////////            case HorseState.Run:
////////            case HorseState.Fight:
////////                frame++;
////////                if (frame >= sprites.Length)
////////                {
////////                    frame = 0;
////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////////                    if (maxCycles > 0)
////////                    {
////////                        _dataCyclesCompleted++;
////////                        if (_dataCyclesCompleted >= maxCycles)
////////                            SetState(HorseState.Idle);
////////                    }
////////                }
////////                break;

////////            default:
////////                frame = (frame + 1) % sprites.Length;
////////                break;
////////        }

////////        if (frame < sprites.Length)
////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////////    }

////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////////                            bool isMainLayer = true)
////////    {
////////        if (img == null) return;
////////        frame = 0;

////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        if (!isMainLayer || _data == null) return;
////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    public void Setup(HorseData data)
////////    {
////////        _data = data;
////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;
////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop (IDropHandler) or externally.
////////    ///
////////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
////////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
////////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
////////    ///
////////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
////////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
////////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
////////    /// visuals appear — the "duplicate soldier" bug.
////////    ///
////////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
////////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

////////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
////////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
////////        seat.MountSoldier(soldier);

////////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
////////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
////////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
////////        // duplicate — two overlapping soldier visuals on the horse.
////////        // SetState below will still call riderVisual.SetRiderState() which is
////////        // harmless because HideRider was already called in Start().
////////        // ────────────────────────────────────────────────────────────────────────

////////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // HideRider is safe to call even though ShowRider was never called
////////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
////////        riderVisual?.HideRider();

////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController  (mount / equipment fix)
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  MOUNT FLOW (fixed)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. PerformMount(soldier)
/////////       → HorseSeat.MountSoldier(soldier)          [position fix here]
/////////           → SetParent(SoldierSeat, false)
/////////           → anchoredPosition = seatOffset
/////////           → soldier.MountOnHorse(seat)
/////////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
/////////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
/////////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
/////////       → SetState(HorseState.Idle)
/////////
/////////  RENDERING PATH DECISION
/////////  ───────────────────────
/////////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
/////////  rider's equipment, and hide the soldier's own CanvasGroup so only
/////////  one visual is visible. This avoids the "duplicate soldier" bug.
/////////
/////////  If you prefer the soldier's own SpriteLayerAnimator to drive
/////////  everything (and skip the 4 seat Images), reverse the two lines
/////////  flagged RENDERING_CHOICE below.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  BUG FIXES vs previous version
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  FIX 1 — Soldier jumps to wrong position on drop
/////////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
/////////    soldier's anchoredPosition is set explicitly instead of being
/////////    derived from its drag-release screen coordinate.
/////////
/////////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
/////////    PerformMount now calls riderVisual.ShowRider(equipment) after
/////////    hiding the soldier's own CanvasGroup. The 4 seat Images are
/////////    populated from the soldier's CharacterEquipment and animated by
/////////    NotifySoldierAnimator(HorseIdle).
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//////             "Auto-found in children if left empty.\n" +
//////             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;
//////    private int _dataCyclesCompleted;

//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;
//////    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
//////    private HorseData _data;

//////    // ── Public queries ────────────────────────────────────────────────────────

//////    public HorseData Data => _data;
//////    public HorseState CurrentState => _state;
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
//////    }

//////    private void Start()
//////    {
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // PATH A: SO-driven
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // PATH B: HorseData fallback (main layer only)
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);
//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                if (frame < sprites.Length - 1) frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);
//////                    }
//////                }
//////                break;

//////            default:
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;
//////        frame = 0;

//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        if (!isMainLayer || _data == null) return;
//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    public void Setup(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    ///
//////    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
//////    ///
//////    ///  Step 1  HorseSeat.MountSoldier(soldier)
//////    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
//////    ///          → anchoredPosition = seatOffset
//////    ///          → soldier.MountOnHorse(seat)
//////    ///
//////    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
//////    ///          Prevents the "duplicate soldier" — the soldier's body is now
//////    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
//////    ///          will show.                                            [FIX 2]
//////    ///
//////    ///  Step 3  riderVisual.ShowRider(equipment)
//////    ///          Populates Face / Helmet / Weapon / Armor Images from the
//////    ///          soldier's CharacterEquipment.                         [FIX 2]
//////    ///
//////    ///  Step 4  NotifySoldierAnimator(HorseIdle)
//////    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
//////    ///          equipment sprites animate in the mounted pose.
//////    ///
//////    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
//////    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
//////    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
//////    ///  path instead:
//////    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
//////    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
//////    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache before reparenting
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
//////        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
//////        // snaps to seatOffset instead of jumping to its drag-release position.
//////        seat.MountSoldier(soldier);

//////        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
//////        // Comment this line out if you want the soldier's own SpriteLayerAnimator
//////        // to drive everything instead of the 4 seat Images.
//////        if (_soldierCanvasGroup != null)
//////            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

//////        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
//////        // Comment this line out if using the "soldier's own visuals" path.
//////        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

//////        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        NotifySoldierAnimator(riderState);
//////        riderVisual?.SetRiderState(riderState);

//////        // Horse itself switches to Idle (also re-notifies rider — harmless)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
//////                  $"Equipment shown via HorseRiderVisual.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide the 4 seat Images
//////        riderVisual?.HideRider();

//////        // Restore the soldier's own CanvasGroup so it is visible on the ground
//////        if (_soldierCanvasGroup != null)
//////            _soldierCanvasGroup.alpha = 1f;

//////        // Reparent the soldier back to its original parent + restore ground state
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;
//////        _soldierCanvasGroup = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler ──────────────────────────────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE — HorseController  (fixed)
/////////////
///////////// Attach to the HorsePrefab root alongside:
/////////////   RectTransform, Image, CanvasGroup
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  HORSE PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////////           ├── Face    (Image)
/////////////           ├── Armor   (Image)
/////////////           ├── Helmet  (Image)
/////////////           └── Weapon  (Image)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  ANIMATION — TWO PATHS (auto-selected)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  PATH A  horseAnimSO assigned in Inspector
/////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////////    → Full control per clip: custom fps, loop flag, frame array.
/////////////
/////////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////////    → Falls back to HorseData sprite arrays directly:
/////////////        Idle  → HorseData.idleSprites  / idleFPS
/////////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
/////////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
/////////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
/////////////    → This keeps every existing HorseData asset working without
/////////////      requiring a HorseAnimationSO to be created first.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  BUG FIXES vs previous rewrite
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  FIX 1 — Idle never played
/////////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
/////////////    null, so HorseData.idleSprites were never shown.  Both methods now
/////////////    fall back to HorseData when the SO is absent.
/////////////
/////////////  FIX 2 — Horse swap did nothing
/////////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
/////////////    "if (_state == newState) return" — so swapping to a new horse while
/////////////    already Idle skipped every frame update.
/////////////    Setup() / SetupWalk() now force-reset the animation directly,
/////////////    bypassing the equality guard entirely.
/////////////
/////////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
/////////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
/////////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
/////////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
/////////////    sprite arrays are selected.  HorseController now always maps
/////////////    HorseState → AnimationState correctly before notifying the rider.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP CHECKLIST
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////////  □ horseImage wired (or auto-found via GetComponent)
/////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////////                   use HorseData sprite arrays (backward-compatible)
/////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////////  □ Canvas root: GraphicRaycaster enabled
/////////////  □ Scene: EventSystem present
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class HorseController : MonoBehaviour, IDropHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Animation Data")]
//////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////////             "(backward-compatible mode — no SO required).")]
//////////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////////             "Leave null if your horse is a single-layer sprite.")]
//////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////////    [Header("Image Layers")]
//////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////////    [SerializeField] private Image horseImage;

//////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////////    [SerializeField] private Image saddleImage;

//////////    [Header("Seat & Rider")]
//////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseSeat seat;

//////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseRiderVisual riderVisual;

//////////    // ── Private state ─────────────────────────────────────────────────────────

//////////    private HorseState _state = HorseState.Idle;

//////////    // Per-layer animation timers
//////////    private float _horseTimer;
//////////    private float _saddleTimer;
//////////    private int _horseFrame;
//////////    private int _saddleFrame;

//////////    // Rider references (captured at mount time, cleared at dismount)
//////////    private SoldierDragDrop _mountedSoldier;
//////////    private SpriteLayerAnimator _riderAnimator;

//////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (horseImage == null)
//////////            horseImage = GetComponent<Image>();

//////////        if (seat == null)
//////////            seat = GetComponentInChildren<HorseSeat>();

//////////        if (riderVisual == null)
//////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////////        if (horseImage == null)
//////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////////        if (seat == null)
//////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////////        if (riderVisual == null)
//////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////////                             "Rider body-part layers will not animate.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        // Show frame 0 immediately so the horse doesn't appear blank.
//////////        // Works whether horseAnimSO is assigned or not.
//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Make sure rider layers start hidden
//////////        riderVisual?.HideRider();
//////////    }

//////////    private void Update()
//////////    {
//////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
//////////    }

//////////    // ── Animation Engine ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Advances one Image layer's timer and updates the sprite.
//////////    ///
//////////    /// Priority:
//////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
//////////    ///   2. HorseData sprite array (if _data != null)
//////////    ///   3. Early-return silently  (nothing to show yet)
//////////    /// </summary>
//////////    private void TickLayer(HorseAnimationSO so, Image img,
//////////                           ref int frame, ref float timer)
//////////    {
//////////        if (img == null) return;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////////            timer += Time.deltaTime;
//////////            if (timer < 1f / clip.fps) return;
//////////            timer -= 1f / clip.fps;

//////////            if (clip.loop)
//////////                frame = (frame + 1) % clip.frames.Length;
//////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////////                frame++;

//////////            img.sprite = clip.frames[frame];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        // Only the main horseImage layer uses HorseData; the saddle layer has
//////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites == null || sprites.Length == 0) return;
//////////        float fps = GetDataFPS(_state);

//////////        timer += Time.deltaTime;
//////////        if (timer < 1f / fps) return;
//////////        timer -= 1f / fps;

//////////        // Dead state: play once and freeze
//////////        if (_state == HorseState.Dead)
//////////        {
//////////            if (frame < sprites.Length - 1) frame++;
//////////        }
//////////        else
//////////        {
//////////            frame = (frame + 1) % sprites.Length;
//////////        }

//////////        img.sprite = sprites[frame];
//////////    }

//////////    /// <summary>
//////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////////    ///
//////////    /// Same two-path priority as TickLayer.
//////////    /// </summary>
//////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
//////////    {
//////////        if (img == null) return;

//////////        frame = 0;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////////            img.sprite = clip.frames[0];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites != null && sprites.Length > 0)
//////////            img.sprite = sprites[0];
//////////    }

//////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

//////////    /// <summary>
//////////    /// Maps a HorseState to the best available HorseData sprite array.
//////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
//////////    /// </summary>
//////////    private Sprite[] GetDataSprites(HorseState state)
//////////    {
//////////        if (_data == null) return null;

//////////        switch (state)
//////////        {
//////////            case HorseState.Run:
//////////                // walkSprites → idleSprites
//////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
//////////                    ? _data.walkSprites
//////////                    : _data.idleSprites;

//////////            case HorseState.Fight:
//////////                // No dedicated fight clip in HorseData — use idle
//////////                return _data.idleSprites;

//////////            case HorseState.Dead:
//////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
//////////                return _data.idleSprites;

//////////            default: // Idle
//////////                return _data.idleSprites;
//////////        }
//////////    }

//////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
//////////    private float GetDataFPS(HorseState state)
//////////    {
//////////        if (_data == null) return 6f;

//////////        return (state == HorseState.Run
//////////                && _data.walkSprites != null
//////////                && _data.walkSprites.Length > 0)
//////////            ? _data.walkFPS
//////////            : _data.idleFPS;
//////////    }

//////////    // ── Public API — State ────────────────────────────────────────────────────

//////////    /// <summary>Current animation state.</summary>
//////////    public HorseState CurrentState => _state;

//////////    /// <summary>True while a soldier is seated on this horse.</summary>
//////////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////////    /// <summary>
//////////    /// Switches the horse (and mounted rider) to a new state.
//////////    /// Both the horse Images and all four rider body-part Images are updated.
//////////    /// Calling with the same state as the current one still resets to frame 0.
//////////    /// </summary>
//////////    public void SetState(HorseState newState)
//////////    {
//////////        _state = newState;

//////////        // Reset frame counters so the new clip starts from frame 0
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Map horse state → rider AnimationState and notify both systems
//////////        AnimationState riderState = MapToRiderState(newState);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////////    }

//////////    // Convenience shorthands — hook these to UI buttons or external controllers
//////////    public void SetIdle() => SetState(HorseState.Idle);
//////////    public void SetRun() => SetState(HorseState.Run);
//////////    public void SetFight() => SetState(HorseState.Fight);
//////////    public void SetDead() => SetState(HorseState.Dead);

//////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////////    public HorseData Data => _data;
//////////    private HorseData _data;

//////////    /// <summary>
//////////    /// Called by HorseSlot to initialise a slotted horse.
//////////    /// Stores the HorseData reference and starts the Idle animation.
//////////    ///
//////////    /// FIX: Force-resets animation state directly instead of routing through
//////////    /// SetState(), so swapping to a new HorseData while already in Idle
//////////    /// correctly updates the displayed sprites instead of being a no-op.
//////////    /// </summary>
//////////    public void Setup(HorseData data)
//////////    {
//////////        _data = data;

//////////        // Force full animation reset — bypasses the old equality guard so that
//////////        // swapping horses (same state, new sprite array) always takes effect.
//////////        _state = HorseState.Idle;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
//////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////////        riderVisual?.SetRiderState(riderState);

//////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////////    }

//////////    /// <summary>
//////////    /// Called by HorseWalkZone to start the horse walking.
//////////    /// Stores the HorseData reference and switches to Run state.
//////////    /// After the zone finishes, call SetIdle() to return to Idle.
//////////    ///
//////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
//////////    /// the zone assigns a new horse while the controller is already in Run.
//////////    /// </summary>
//////////    public void SetupWalk(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Run;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////////    }

//////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////////    /// <summary>
//////////    /// Accepts a soldier into the seat.
//////////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////////    ///
//////////    /// The soldier's CharacterEquipment is read to populate the four rider
//////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
//////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////////            return;
//////////        }

//////////        if (soldier == null) return;

//////////        // Cache references before MountOnHorse() reparents the soldier
//////////        _mountedSoldier = soldier;
//////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////////        seat.MountSoldier(soldier);

//////////        // Show the 4 rider Images using the soldier's equipped items.
//////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
//////////        // for each of: Face, Armor, Helmet, Weapon.
//////////        riderVisual?.ShowRider(equipment);

//////////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////////    }

//////////    /// <summary>
//////////    /// Returns the soldier to the ground and resets the horse to Idle.
//////////    /// Wire this to a UI "Dismount" button or call it from an external system.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (seat == null || !seat.IsOccupied) return;

//////////        // Hide rider Images before the soldier is reparented away
//////////        riderVisual?.HideRider();

//////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////////        seat.MountedSoldier.DismountFromHorse();
//////////        seat.ReleaseSoldier();

//////////        _mountedSoldier = null;
//////////        _riderAnimator = null;

//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////////    }

//////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////////    /// <summary>
//////////    /// Fired by Unity's EventSystem when a dragged object is released over
//////////    /// any Raycast-Target Image on this GameObject.
//////////    /// Accepts soldiers only; ignores anything else.
//////////    /// </summary>
//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////////        if (soldier == null) return;

//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////////            return;
//////////        }

//////////        PerformMount(soldier);
//////////    }

//////////    // ── Internal helpers ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////////    /// Safe to call when no rider is present (null-checked).
//////////    /// </summary>
//////////    private void NotifySoldierAnimator(AnimationState riderState)
//////////    {
//////////        _riderAnimator?.SetState(riderState);
//////////    }

//////////    /// <summary>
//////////    /// Maps HorseState → the matching AnimationState for the soldier.
//////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////////    /// both receive this mapped value so rider equipment sprites are selected
//////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
//////////    /// </summary>
//////////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////////    {
//////////        HorseState.Idle => AnimationState.HorseIdle,
//////////        HorseState.Run => AnimationState.HorseRun,
//////////        HorseState.Fight => AnimationState.HorseFight,
//////////        HorseState.Dead => AnimationState.HorseDead,
//////////        _ => AnimationState.HorseIdle,
//////////    };
//////////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Reads all four animation sets directly from HorseData:
///////////        Idle   → idleSprites   / idleFPS     — loops forever
///////////        Run    → runSprites    / runFPS       — auto-returns to Idle
///////////                                                after runCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
///////////                                                after attackCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
///////////    → Falls back to idleSprites for any clip whose array is empty.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — all four clips supported).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers (used by both Path A and Path B)
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
////////    private int _dataCyclesCompleted;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Rider layers start hidden until a soldier mounts
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer by dt and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
////////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer,
////////                           bool isMainLayer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////            {
////////                frame = (frame + 1) % clip.frames.Length;
////////            }
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////            {
////////                frame++;
////////            }

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;

////////        float fps = _data.GetFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        switch (_state)
////////        {
////////            case HorseState.Dead:
////////                // Play once — freeze on the last frame
////////                if (frame < sprites.Length - 1)
////////                    frame++;
////////                break;

////////            case HorseState.Run:
////////            case HorseState.Fight:
////////                // Advance frame; count completed cycles for auto-return to Idle
////////                frame++;
////////                if (frame >= sprites.Length)
////////                {
////////                    frame = 0;
////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////////                    if (maxCycles > 0)
////////                    {
////////                        _dataCyclesCompleted++;
////////                        if (_dataCyclesCompleted >= maxCycles)
////////                            SetState(HorseState.Idle);   // auto-return
////////                    }
////////                }
////////                break;

////////            default: // Idle — loop forever
////////                frame = (frame + 1) % sprites.Length;
////////                break;
////////        }

////////        if (_state != HorseState.Idle || frame < sprites.Length)
////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////////                            bool isMainLayer = true)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // PATH A
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // PATH B
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new animation state.
////////    /// Resets to frame 0 every time — even when switching to the same state —
////////    /// so swapping horse data always refreshes the displayed sprite.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset counters so the new clip starts fresh
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — wire to UI buttons or call from game systems
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// Force-resets animation state directly so swapping to a new HorseData
////////    /// while already in Idle correctly updates the displayed sprites.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse running.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays.
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Falls back to HorseData sprite arrays directly.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  RIDER RENDERING — SOLDIER'S OWN SPRITES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
/////////  soldier's own SpriteLayerAnimator handles the mounted pose via
/////////  the HorseIdle / HorseRun AnimationStates.
/////////
/////////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
/////////  activated during mount — calling ShowRider while the soldier's
/////////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
/////////  soldier to appear on the horse.
/////////
/////////  If you want to use HorseRiderVisual instead, change MountOnHorse
/////////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
/////////  riderVisual?.ShowRider(equipment) line in PerformMount below.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//////             "Auto-found in children if left empty.\n" +
//////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;
//////    private int _dataCyclesCompleted;

//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;
//////    private HorseData _data;

//////    // ── Public queries ────────────────────────────────────────────────────────

//////    public HorseData Data => _data;
//////    public HorseState CurrentState => _state;
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
//////    }

//////    private void Start()
//////    {
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // PATH A: SO-driven
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // PATH B: HorseData fallback (main layer only)
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);
//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                if (frame < sprites.Length - 1) frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);
//////                    }
//////                }
//////                break;

//////            default:
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;
//////        frame = 0;

//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        if (!isMainLayer || _data == null) return;
//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    public void Setup(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop (IDropHandler) or externally.
//////    ///
//////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
//////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
//////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
//////    ///
//////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
//////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
//////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
//////    /// visuals appear — the "duplicate soldier" bug.
//////    ///
//////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
//////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

//////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
//////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
//////        seat.MountSoldier(soldier);

//////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
//////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
//////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
//////        // duplicate — two overlapping soldier visuals on the horse.
//////        // SetState below will still call riderVisual.SetRiderState() which is
//////        // harmless because HideRider was already called in Start().
//////        // ────────────────────────────────────────────────────────────────────────

//////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // HideRider is safe to call even though ShowRider was never called
//////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
//////        riderVisual?.HideRider();

//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController  (mount / equipment fix)
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  MOUNT FLOW (fixed)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. PerformMount(soldier)
///////       → HorseSeat.MountSoldier(soldier)          [position fix here]
///////           → SetParent(SoldierSeat, false)
///////           → anchoredPosition = seatOffset
///////           → soldier.MountOnHorse(seat)
///////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
///////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
///////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
///////       → SetState(HorseState.Idle)
///////
///////  RENDERING PATH DECISION
///////  ───────────────────────
///////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
///////  rider's equipment, and hide the soldier's own CanvasGroup so only
///////  one visual is visible. This avoids the "duplicate soldier" bug.
///////
///////  If you prefer the soldier's own SpriteLayerAnimator to drive
///////  everything (and skip the 4 seat Images), reverse the two lines
///////  flagged RENDERING_CHOICE below.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  BUG FIXES vs previous version
/////// ════════════════════════════════════════════════════════════════════
///////
///////  FIX 1 — Soldier jumps to wrong position on drop
///////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
///////    soldier's anchoredPosition is set explicitly instead of being
///////    derived from its drag-release screen coordinate.
///////
///////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
///////    PerformMount now calls riderVisual.ShowRider(equipment) after
///////    hiding the soldier's own CanvasGroup. The 4 seat Images are
///////    populated from the soldier's CharacterEquipment and animated by
///////    NotifySoldierAnimator(HorseIdle).
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////        if (riderVisual == null)
////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    ///
////    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
////    ///
////    ///  Step 1  HorseSeat.MountSoldier(soldier)
////    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
////    ///          → anchoredPosition = seatOffset
////    ///          → soldier.MountOnHorse(seat)
////    ///
////    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
////    ///          Prevents the "duplicate soldier" — the soldier's body is now
////    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
////    ///          will show.                                            [FIX 2]
////    ///
////    ///  Step 3  riderVisual.ShowRider(equipment)
////    ///          Populates Face / Helmet / Weapon / Armor Images from the
////    ///          soldier's CharacterEquipment.                         [FIX 2]
////    ///
////    ///  Step 4  NotifySoldierAnimator(HorseIdle)
////    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
////    ///          equipment sprites animate in the mounted pose.
////    ///
////    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
////    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
////    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
////    ///  path instead:
////    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
////    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
////    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache before reparenting
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
////        var equipment = soldier.GetComponent<CharacterEquipment>();

////        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
////        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
////        // snaps to seatOffset instead of jumping to its drag-release position.
////        seat.MountSoldier(soldier);

////        // ── Steps 2 & 3: visual ownership ────────────────────────────────────
////        // seat.MountSoldier() → soldier.MountOnHorse() already called
////        // HideOwnVisuals() — soldier's CanvasGroup alpha is now 0.
////        // ShowRider populates Face/Helmet/Weapon/Armor Images and internally
////        // calls SetRiderStateInternal(HorseIdle) — so we do NOT call
////        // SetRiderState again here (that would be a duplicate, causing the
////        // double-visual bug). SetState below handles the final notification.
////        riderVisual?.ShowRider(equipment);

////        // ── Step 4: single authoritative state transition ─────────────────────
////        // SetState notifies both riderVisual.SetRiderState and NotifySoldierAnimator
////        // exactly once. Do not call either directly before this line.
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
////                  $"Equipment shown via HorseRiderVisual.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // Hide the 4 seat Images
////        riderVisual?.HideRider();

////        // Restore the soldier's own CanvasGroup so it is visible on the ground
////        if (_soldierCanvasGroup != null)
////            _soldierCanvasGroup.alpha = 1f;

////        // Reparent the soldier back to its original parent + restore ground state
////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;
////        _soldierCanvasGroup = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler ──────────────────────────────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE — HorseController  (fixed)
/////////////
///////////// Attach to the HorsePrefab root alongside:
/////////////   RectTransform, Image, CanvasGroup
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  HORSE PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////////           ├── Face    (Image)
/////////////           ├── Armor   (Image)
/////////////           ├── Helmet  (Image)
/////////////           └── Weapon  (Image)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  ANIMATION — TWO PATHS (auto-selected)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  PATH A  horseAnimSO assigned in Inspector
/////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////////    → Full control per clip: custom fps, loop flag, frame array.
/////////////
/////////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////////    → Falls back to HorseData sprite arrays directly:
/////////////        Idle  → HorseData.idleSprites  / idleFPS
/////////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
/////////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
/////////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
/////////////    → This keeps every existing HorseData asset working without
/////////////      requiring a HorseAnimationSO to be created first.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  BUG FIXES vs previous rewrite
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  FIX 1 — Idle never played
/////////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
/////////////    null, so HorseData.idleSprites were never shown.  Both methods now
/////////////    fall back to HorseData when the SO is absent.
/////////////
/////////////  FIX 2 — Horse swap did nothing
/////////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
/////////////    "if (_state == newState) return" — so swapping to a new horse while
/////////////    already Idle skipped every frame update.
/////////////    Setup() / SetupWalk() now force-reset the animation directly,
/////////////    bypassing the equality guard entirely.
/////////////
/////////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
/////////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
/////////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
/////////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
/////////////    sprite arrays are selected.  HorseController now always maps
/////////////    HorseState → AnimationState correctly before notifying the rider.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP CHECKLIST
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////////  □ horseImage wired (or auto-found via GetComponent)
/////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////////                   use HorseData sprite arrays (backward-compatible)
/////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////////  □ Canvas root: GraphicRaycaster enabled
/////////////  □ Scene: EventSystem present
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class HorseController : MonoBehaviour, IDropHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Animation Data")]
//////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////////             "(backward-compatible mode — no SO required).")]
//////////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////////             "Leave null if your horse is a single-layer sprite.")]
//////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////////    [Header("Image Layers")]
//////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////////    [SerializeField] private Image horseImage;

//////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////////    [SerializeField] private Image saddleImage;

//////////    [Header("Seat & Rider")]
//////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseSeat seat;

//////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseRiderVisual riderVisual;

//////////    // ── Private state ─────────────────────────────────────────────────────────

//////////    private HorseState _state = HorseState.Idle;

//////////    // Per-layer animation timers
//////////    private float _horseTimer;
//////////    private float _saddleTimer;
//////////    private int _horseFrame;
//////////    private int _saddleFrame;

//////////    // Rider references (captured at mount time, cleared at dismount)
//////////    private SoldierDragDrop _mountedSoldier;
//////////    private SpriteLayerAnimator _riderAnimator;

//////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (horseImage == null)
//////////            horseImage = GetComponent<Image>();

//////////        if (seat == null)
//////////            seat = GetComponentInChildren<HorseSeat>();

//////////        if (riderVisual == null)
//////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////////        if (horseImage == null)
//////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////////        if (seat == null)
//////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////////        if (riderVisual == null)
//////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////////                             "Rider body-part layers will not animate.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        // Show frame 0 immediately so the horse doesn't appear blank.
//////////        // Works whether horseAnimSO is assigned or not.
//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Make sure rider layers start hidden
//////////        riderVisual?.HideRider();
//////////    }

//////////    private void Update()
//////////    {
//////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
//////////    }

//////////    // ── Animation Engine ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Advances one Image layer's timer and updates the sprite.
//////////    ///
//////////    /// Priority:
//////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
//////////    ///   2. HorseData sprite array (if _data != null)
//////////    ///   3. Early-return silently  (nothing to show yet)
//////////    /// </summary>
//////////    private void TickLayer(HorseAnimationSO so, Image img,
//////////                           ref int frame, ref float timer)
//////////    {
//////////        if (img == null) return;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////////            timer += Time.deltaTime;
//////////            if (timer < 1f / clip.fps) return;
//////////            timer -= 1f / clip.fps;

//////////            if (clip.loop)
//////////                frame = (frame + 1) % clip.frames.Length;
//////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////////                frame++;

//////////            img.sprite = clip.frames[frame];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        // Only the main horseImage layer uses HorseData; the saddle layer has
//////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites == null || sprites.Length == 0) return;
//////////        float fps = GetDataFPS(_state);

//////////        timer += Time.deltaTime;
//////////        if (timer < 1f / fps) return;
//////////        timer -= 1f / fps;

//////////        // Dead state: play once and freeze
//////////        if (_state == HorseState.Dead)
//////////        {
//////////            if (frame < sprites.Length - 1) frame++;
//////////        }
//////////        else
//////////        {
//////////            frame = (frame + 1) % sprites.Length;
//////////        }

//////////        img.sprite = sprites[frame];
//////////    }

//////////    /// <summary>
//////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////////    ///
//////////    /// Same two-path priority as TickLayer.
//////////    /// </summary>
//////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
//////////    {
//////////        if (img == null) return;

//////////        frame = 0;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////////            img.sprite = clip.frames[0];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites != null && sprites.Length > 0)
//////////            img.sprite = sprites[0];
//////////    }

//////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

//////////    /// <summary>
//////////    /// Maps a HorseState to the best available HorseData sprite array.
//////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
//////////    /// </summary>
//////////    private Sprite[] GetDataSprites(HorseState state)
//////////    {
//////////        if (_data == null) return null;

//////////        switch (state)
//////////        {
//////////            case HorseState.Run:
//////////                // walkSprites → idleSprites
//////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
//////////                    ? _data.walkSprites
//////////                    : _data.idleSprites;

//////////            case HorseState.Fight:
//////////                // No dedicated fight clip in HorseData — use idle
//////////                return _data.idleSprites;

//////////            case HorseState.Dead:
//////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
//////////                return _data.idleSprites;

//////////            default: // Idle
//////////                return _data.idleSprites;
//////////        }
//////////    }

//////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
//////////    private float GetDataFPS(HorseState state)
//////////    {
//////////        if (_data == null) return 6f;

//////////        return (state == HorseState.Run
//////////                && _data.walkSprites != null
//////////                && _data.walkSprites.Length > 0)
//////////            ? _data.walkFPS
//////////            : _data.idleFPS;
//////////    }

//////////    // ── Public API — State ────────────────────────────────────────────────────

//////////    /// <summary>Current animation state.</summary>
//////////    public HorseState CurrentState => _state;

//////////    /// <summary>True while a soldier is seated on this horse.</summary>
//////////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////////    /// <summary>
//////////    /// Switches the horse (and mounted rider) to a new state.
//////////    /// Both the horse Images and all four rider body-part Images are updated.
//////////    /// Calling with the same state as the current one still resets to frame 0.
//////////    /// </summary>
//////////    public void SetState(HorseState newState)
//////////    {
//////////        _state = newState;

//////////        // Reset frame counters so the new clip starts from frame 0
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Map horse state → rider AnimationState and notify both systems
//////////        AnimationState riderState = MapToRiderState(newState);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////////    }

//////////    // Convenience shorthands — hook these to UI buttons or external controllers
//////////    public void SetIdle() => SetState(HorseState.Idle);
//////////    public void SetRun() => SetState(HorseState.Run);
//////////    public void SetFight() => SetState(HorseState.Fight);
//////////    public void SetDead() => SetState(HorseState.Dead);

//////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////////    public HorseData Data => _data;
//////////    private HorseData _data;

//////////    /// <summary>
//////////    /// Called by HorseSlot to initialise a slotted horse.
//////////    /// Stores the HorseData reference and starts the Idle animation.
//////////    ///
//////////    /// FIX: Force-resets animation state directly instead of routing through
//////////    /// SetState(), so swapping to a new HorseData while already in Idle
//////////    /// correctly updates the displayed sprites instead of being a no-op.
//////////    /// </summary>
//////////    public void Setup(HorseData data)
//////////    {
//////////        _data = data;

//////////        // Force full animation reset — bypasses the old equality guard so that
//////////        // swapping horses (same state, new sprite array) always takes effect.
//////////        _state = HorseState.Idle;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
//////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////////        riderVisual?.SetRiderState(riderState);

//////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////////    }

//////////    /// <summary>
//////////    /// Called by HorseWalkZone to start the horse walking.
//////////    /// Stores the HorseData reference and switches to Run state.
//////////    /// After the zone finishes, call SetIdle() to return to Idle.
//////////    ///
//////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
//////////    /// the zone assigns a new horse while the controller is already in Run.
//////////    /// </summary>
//////////    public void SetupWalk(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Run;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////////    }

//////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////////    /// <summary>
//////////    /// Accepts a soldier into the seat.
//////////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////////    ///
//////////    /// The soldier's CharacterEquipment is read to populate the four rider
//////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
//////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////////            return;
//////////        }

//////////        if (soldier == null) return;

//////////        // Cache references before MountOnHorse() reparents the soldier
//////////        _mountedSoldier = soldier;
//////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////////        seat.MountSoldier(soldier);

//////////        // Show the 4 rider Images using the soldier's equipped items.
//////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
//////////        // for each of: Face, Armor, Helmet, Weapon.
//////////        riderVisual?.ShowRider(equipment);

//////////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////////    }

//////////    /// <summary>
//////////    /// Returns the soldier to the ground and resets the horse to Idle.
//////////    /// Wire this to a UI "Dismount" button or call it from an external system.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (seat == null || !seat.IsOccupied) return;

//////////        // Hide rider Images before the soldier is reparented away
//////////        riderVisual?.HideRider();

//////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////////        seat.MountedSoldier.DismountFromHorse();
//////////        seat.ReleaseSoldier();

//////////        _mountedSoldier = null;
//////////        _riderAnimator = null;

//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////////    }

//////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////////    /// <summary>
//////////    /// Fired by Unity's EventSystem when a dragged object is released over
//////////    /// any Raycast-Target Image on this GameObject.
//////////    /// Accepts soldiers only; ignores anything else.
//////////    /// </summary>
//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////////        if (soldier == null) return;

//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////////            return;
//////////        }

//////////        PerformMount(soldier);
//////////    }

//////////    // ── Internal helpers ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////////    /// Safe to call when no rider is present (null-checked).
//////////    /// </summary>
//////////    private void NotifySoldierAnimator(AnimationState riderState)
//////////    {
//////////        _riderAnimator?.SetState(riderState);
//////////    }

//////////    /// <summary>
//////////    /// Maps HorseState → the matching AnimationState for the soldier.
//////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////////    /// both receive this mapped value so rider equipment sprites are selected
//////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
//////////    /// </summary>
//////////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////////    {
//////////        HorseState.Idle => AnimationState.HorseIdle,
//////////        HorseState.Run => AnimationState.HorseRun,
//////////        HorseState.Fight => AnimationState.HorseFight,
//////////        HorseState.Dead => AnimationState.HorseDead,
//////////        _ => AnimationState.HorseIdle,
//////////    };
//////////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Reads all four animation sets directly from HorseData:
///////////        Idle   → idleSprites   / idleFPS     — loops forever
///////////        Run    → runSprites    / runFPS       — auto-returns to Idle
///////////                                                after runCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
///////////                                                after attackCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
///////////    → Falls back to idleSprites for any clip whose array is empty.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — all four clips supported).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers (used by both Path A and Path B)
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
////////    private int _dataCyclesCompleted;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Rider layers start hidden until a soldier mounts
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer by dt and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
////////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer,
////////                           bool isMainLayer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////            {
////////                frame = (frame + 1) % clip.frames.Length;
////////            }
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////            {
////////                frame++;
////////            }

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;

////////        float fps = _data.GetFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        switch (_state)
////////        {
////////            case HorseState.Dead:
////////                // Play once — freeze on the last frame
////////                if (frame < sprites.Length - 1)
////////                    frame++;
////////                break;

////////            case HorseState.Run:
////////            case HorseState.Fight:
////////                // Advance frame; count completed cycles for auto-return to Idle
////////                frame++;
////////                if (frame >= sprites.Length)
////////                {
////////                    frame = 0;
////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////////                    if (maxCycles > 0)
////////                    {
////////                        _dataCyclesCompleted++;
////////                        if (_dataCyclesCompleted >= maxCycles)
////////                            SetState(HorseState.Idle);   // auto-return
////////                    }
////////                }
////////                break;

////////            default: // Idle — loop forever
////////                frame = (frame + 1) % sprites.Length;
////////                break;
////////        }

////////        if (_state != HorseState.Idle || frame < sprites.Length)
////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////////                            bool isMainLayer = true)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // PATH A
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // PATH B
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new animation state.
////////    /// Resets to frame 0 every time — even when switching to the same state —
////////    /// so swapping horse data always refreshes the displayed sprite.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset counters so the new clip starts fresh
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — wire to UI buttons or call from game systems
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// Force-resets animation state directly so swapping to a new HorseData
////////    /// while already in Idle correctly updates the displayed sprites.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse running.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays.
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Falls back to HorseData sprite arrays directly.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  RIDER RENDERING — SOLDIER'S OWN SPRITES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
/////////  soldier's own SpriteLayerAnimator handles the mounted pose via
/////////  the HorseIdle / HorseRun AnimationStates.
/////////
/////////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
/////////  activated during mount — calling ShowRider while the soldier's
/////////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
/////////  soldier to appear on the horse.
/////////
/////////  If you want to use HorseRiderVisual instead, change MountOnHorse
/////////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
/////////  riderVisual?.ShowRider(equipment) line in PerformMount below.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//////             "Auto-found in children if left empty.\n" +
//////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;
//////    private int _dataCyclesCompleted;

//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;
//////    private HorseData _data;

//////    // ── Public queries ────────────────────────────────────────────────────────

//////    public HorseData Data => _data;
//////    public HorseState CurrentState => _state;
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
//////    }

//////    private void Start()
//////    {
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // PATH A: SO-driven
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // PATH B: HorseData fallback (main layer only)
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);
//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                if (frame < sprites.Length - 1) frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);
//////                    }
//////                }
//////                break;

//////            default:
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;
//////        frame = 0;

//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        if (!isMainLayer || _data == null) return;
//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    public void Setup(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop (IDropHandler) or externally.
//////    ///
//////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
//////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
//////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
//////    ///
//////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
//////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
//////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
//////    /// visuals appear — the "duplicate soldier" bug.
//////    ///
//////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
//////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

//////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
//////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
//////        seat.MountSoldier(soldier);

//////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
//////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
//////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
//////        // duplicate — two overlapping soldier visuals on the horse.
//////        // SetState below will still call riderVisual.SetRiderState() which is
//////        // harmless because HideRider was already called in Start().
//////        // ────────────────────────────────────────────────────────────────────────

//////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // HideRider is safe to call even though ShowRider was never called
//////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
//////        riderVisual?.HideRider();

//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController  (mount / equipment fix)
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  MOUNT FLOW (fixed)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. PerformMount(soldier)
///////       → HorseSeat.MountSoldier(soldier)          [position fix here]
///////           → SetParent(SoldierSeat, false)
///////           → anchoredPosition = seatOffset
///////           → soldier.MountOnHorse(seat)
///////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
///////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
///////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
///////       → SetState(HorseState.Idle)
///////
///////  RENDERING PATH DECISION
///////  ───────────────────────
///////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
///////  rider's equipment, and hide the soldier's own CanvasGroup so only
///////  one visual is visible. This avoids the "duplicate soldier" bug.
///////
///////  If you prefer the soldier's own SpriteLayerAnimator to drive
///////  everything (and skip the 4 seat Images), reverse the two lines
///////  flagged RENDERING_CHOICE below.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  BUG FIXES vs previous version
/////// ════════════════════════════════════════════════════════════════════
///////
///////  FIX 1 — Soldier jumps to wrong position on drop
///////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
///////    soldier's anchoredPosition is set explicitly instead of being
///////    derived from its drag-release screen coordinate.
///////
///////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
///////    PerformMount now calls riderVisual.ShowRider(equipment) after
///////    hiding the soldier's own CanvasGroup. The 4 seat Images are
///////    populated from the soldier's CharacterEquipment and animated by
///////    NotifySoldierAnimator(HorseIdle).
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////        if (riderVisual == null)
////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    ///
////    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
////    ///
////    ///  Step 1  HorseSeat.MountSoldier(soldier)
////    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
////    ///          → anchoredPosition = seatOffset
////    ///          → soldier.MountOnHorse(seat)
////    ///
////    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
////    ///          Prevents the "duplicate soldier" — the soldier's body is now
////    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
////    ///          will show.                                            [FIX 2]
////    ///
////    ///  Step 3  riderVisual.ShowRider(equipment)
////    ///          Populates Face / Helmet / Weapon / Armor Images from the
////    ///          soldier's CharacterEquipment.                         [FIX 2]
////    ///
////    ///  Step 4  NotifySoldierAnimator(HorseIdle)
////    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
////    ///          equipment sprites animate in the mounted pose.
////    ///
////    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
////    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
////    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
////    ///  path instead:
////    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
////    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
////    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache before reparenting
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
////        var equipment = soldier.GetComponent<CharacterEquipment>();

////        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
////        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
////        // snaps to seatOffset instead of jumping to its drag-release position.
////        seat.MountSoldier(soldier);

////        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
////        // Comment this line out if you want the soldier's own SpriteLayerAnimator
////        // to drive everything instead of the 4 seat Images.
////        if (_soldierCanvasGroup != null)
////            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

////        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
////        // Comment this line out if using the "soldier's own visuals" path.
////        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

////        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        NotifySoldierAnimator(riderState);
////        riderVisual?.SetRiderState(riderState);

////        // Horse itself switches to Idle (also re-notifies rider — harmless)
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
////                  $"Equipment shown via HorseRiderVisual.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // Hide the 4 seat Images
////        riderVisual?.HideRider();

////        // Restore the soldier's own CanvasGroup so it is visible on the ground
////        if (_soldierCanvasGroup != null)
////            _soldierCanvasGroup.alpha = 1f;

////        // Reparent the soldier back to its original parent + restore ground state
////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;
////        _soldierCanvasGroup = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler ──────────────────────────────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE — HorseController  (fixed)
/////////////
///////////// Attach to the HorsePrefab root alongside:
/////////////   RectTransform, Image, CanvasGroup
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  HORSE PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////////           ├── Face    (Image)
/////////////           ├── Armor   (Image)
/////////////           ├── Helmet  (Image)
/////////////           └── Weapon  (Image)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  ANIMATION — TWO PATHS (auto-selected)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  PATH A  horseAnimSO assigned in Inspector
/////////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////////    → Full control per clip: custom fps, loop flag, frame array.
/////////////
/////////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////////    → Falls back to HorseData sprite arrays directly:
/////////////        Idle  → HorseData.idleSprites  / idleFPS
/////////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
/////////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
/////////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
/////////////    → This keeps every existing HorseData asset working without
/////////////      requiring a HorseAnimationSO to be created first.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  BUG FIXES vs previous rewrite
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  FIX 1 — Idle never played
/////////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
/////////////    null, so HorseData.idleSprites were never shown.  Both methods now
/////////////    fall back to HorseData when the SO is absent.
/////////////
/////////////  FIX 2 — Horse swap did nothing
/////////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
/////////////    "if (_state == newState) return" — so swapping to a new horse while
/////////////    already Idle skipped every frame update.
/////////////    Setup() / SetupWalk() now force-reset the animation directly,
/////////////    bypassing the equality guard entirely.
/////////////
/////////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
/////////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
/////////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
/////////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
/////////////    sprite arrays are selected.  HorseController now always maps
/////////////    HorseState → AnimationState correctly before notifying the rider.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP CHECKLIST
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////////  □ horseImage wired (or auto-found via GetComponent)
/////////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////////                   use HorseData sprite arrays (backward-compatible)
/////////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////////  □ Canvas root: GraphicRaycaster enabled
/////////////  □ Scene: EventSystem present
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class HorseController : MonoBehaviour, IDropHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Animation Data")]
//////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////////             "(backward-compatible mode — no SO required).")]
//////////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////////             "Leave null if your horse is a single-layer sprite.")]
//////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////////    [Header("Image Layers")]
//////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////////    [SerializeField] private Image horseImage;

//////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////////    [SerializeField] private Image saddleImage;

//////////    [Header("Seat & Rider")]
//////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseSeat seat;

//////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////////             "Auto-found in children if left empty.")]
//////////    [SerializeField] private HorseRiderVisual riderVisual;

//////////    // ── Private state ─────────────────────────────────────────────────────────

//////////    private HorseState _state = HorseState.Idle;

//////////    // Per-layer animation timers
//////////    private float _horseTimer;
//////////    private float _saddleTimer;
//////////    private int _horseFrame;
//////////    private int _saddleFrame;

//////////    // Rider references (captured at mount time, cleared at dismount)
//////////    private SoldierDragDrop _mountedSoldier;
//////////    private SpriteLayerAnimator _riderAnimator;

//////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (horseImage == null)
//////////            horseImage = GetComponent<Image>();

//////////        if (seat == null)
//////////            seat = GetComponentInChildren<HorseSeat>();

//////////        if (riderVisual == null)
//////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////////        if (horseImage == null)
//////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////////        if (seat == null)
//////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////////        if (riderVisual == null)
//////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////////                             "Rider body-part layers will not animate.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        // Show frame 0 immediately so the horse doesn't appear blank.
//////////        // Works whether horseAnimSO is assigned or not.
//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Make sure rider layers start hidden
//////////        riderVisual?.HideRider();
//////////    }

//////////    private void Update()
//////////    {
//////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
//////////    }

//////////    // ── Animation Engine ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Advances one Image layer's timer and updates the sprite.
//////////    ///
//////////    /// Priority:
//////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
//////////    ///   2. HorseData sprite array (if _data != null)
//////////    ///   3. Early-return silently  (nothing to show yet)
//////////    /// </summary>
//////////    private void TickLayer(HorseAnimationSO so, Image img,
//////////                           ref int frame, ref float timer)
//////////    {
//////////        if (img == null) return;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////////            timer += Time.deltaTime;
//////////            if (timer < 1f / clip.fps) return;
//////////            timer -= 1f / clip.fps;

//////////            if (clip.loop)
//////////                frame = (frame + 1) % clip.frames.Length;
//////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////////                frame++;

//////////            img.sprite = clip.frames[frame];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        // Only the main horseImage layer uses HorseData; the saddle layer has
//////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites == null || sprites.Length == 0) return;
//////////        float fps = GetDataFPS(_state);

//////////        timer += Time.deltaTime;
//////////        if (timer < 1f / fps) return;
//////////        timer -= 1f / fps;

//////////        // Dead state: play once and freeze
//////////        if (_state == HorseState.Dead)
//////////        {
//////////            if (frame < sprites.Length - 1) frame++;
//////////        }
//////////        else
//////////        {
//////////            frame = (frame + 1) % sprites.Length;
//////////        }

//////////        img.sprite = sprites[frame];
//////////    }

//////////    /// <summary>
//////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////////    ///
//////////    /// Same two-path priority as TickLayer.
//////////    /// </summary>
//////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
//////////    {
//////////        if (img == null) return;

//////////        frame = 0;

//////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////////        if (so != null)
//////////        {
//////////            HorseClip clip = so.GetClip(_state);
//////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////////            img.sprite = clip.frames[0];
//////////            return;
//////////        }

//////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////////        if (_data == null || img != horseImage) return;

//////////        Sprite[] sprites = GetDataSprites(_state);
//////////        if (sprites != null && sprites.Length > 0)
//////////            img.sprite = sprites[0];
//////////    }

//////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

//////////    /// <summary>
//////////    /// Maps a HorseState to the best available HorseData sprite array.
//////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
//////////    /// </summary>
//////////    private Sprite[] GetDataSprites(HorseState state)
//////////    {
//////////        if (_data == null) return null;

//////////        switch (state)
//////////        {
//////////            case HorseState.Run:
//////////                // walkSprites → idleSprites
//////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
//////////                    ? _data.walkSprites
//////////                    : _data.idleSprites;

//////////            case HorseState.Fight:
//////////                // No dedicated fight clip in HorseData — use idle
//////////                return _data.idleSprites;

//////////            case HorseState.Dead:
//////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
//////////                return _data.idleSprites;

//////////            default: // Idle
//////////                return _data.idleSprites;
//////////        }
//////////    }

//////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
//////////    private float GetDataFPS(HorseState state)
//////////    {
//////////        if (_data == null) return 6f;

//////////        return (state == HorseState.Run
//////////                && _data.walkSprites != null
//////////                && _data.walkSprites.Length > 0)
//////////            ? _data.walkFPS
//////////            : _data.idleFPS;
//////////    }

//////////    // ── Public API — State ────────────────────────────────────────────────────

//////////    /// <summary>Current animation state.</summary>
//////////    public HorseState CurrentState => _state;

//////////    /// <summary>True while a soldier is seated on this horse.</summary>
//////////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////////    /// <summary>
//////////    /// Switches the horse (and mounted rider) to a new state.
//////////    /// Both the horse Images and all four rider body-part Images are updated.
//////////    /// Calling with the same state as the current one still resets to frame 0.
//////////    /// </summary>
//////////    public void SetState(HorseState newState)
//////////    {
//////////        _state = newState;

//////////        // Reset frame counters so the new clip starts from frame 0
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Map horse state → rider AnimationState and notify both systems
//////////        AnimationState riderState = MapToRiderState(newState);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////////    }

//////////    // Convenience shorthands — hook these to UI buttons or external controllers
//////////    public void SetIdle() => SetState(HorseState.Idle);
//////////    public void SetRun() => SetState(HorseState.Run);
//////////    public void SetFight() => SetState(HorseState.Fight);
//////////    public void SetDead() => SetState(HorseState.Dead);

//////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////////    public HorseData Data => _data;
//////////    private HorseData _data;

//////////    /// <summary>
//////////    /// Called by HorseSlot to initialise a slotted horse.
//////////    /// Stores the HorseData reference and starts the Idle animation.
//////////    ///
//////////    /// FIX: Force-resets animation state directly instead of routing through
//////////    /// SetState(), so swapping to a new HorseData while already in Idle
//////////    /// correctly updates the displayed sprites instead of being a no-op.
//////////    /// </summary>
//////////    public void Setup(HorseData data)
//////////    {
//////////        _data = data;

//////////        // Force full animation reset — bypasses the old equality guard so that
//////////        // swapping horses (same state, new sprite array) always takes effect.
//////////        _state = HorseState.Idle;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
//////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////////        riderVisual?.SetRiderState(riderState);

//////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////////    }

//////////    /// <summary>
//////////    /// Called by HorseWalkZone to start the horse walking.
//////////    /// Stores the HorseData reference and switches to Run state.
//////////    /// After the zone finishes, call SetIdle() to return to Idle.
//////////    ///
//////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
//////////    /// the zone assigns a new horse while the controller is already in Run.
//////////    /// </summary>
//////////    public void SetupWalk(HorseData data)
//////////    {
//////////        _data = data;

//////////        _state = HorseState.Run;
//////////        _horseFrame = _saddleFrame = 0;
//////////        _horseTimer = _saddleTimer = 0f;

//////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////////        if (saddleImage != null && saddleAnimSO != null)
//////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////////        riderVisual?.SetRiderState(riderState);
//////////        NotifySoldierAnimator(riderState);

//////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////////    }

//////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////////    /// <summary>
//////////    /// Accepts a soldier into the seat.
//////////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////////    ///
//////////    /// The soldier's CharacterEquipment is read to populate the four rider
//////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
//////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////////            return;
//////////        }

//////////        if (soldier == null) return;

//////////        // Cache references before MountOnHorse() reparents the soldier
//////////        _mountedSoldier = soldier;
//////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////////        seat.MountSoldier(soldier);

//////////        // Show the 4 rider Images using the soldier's equipped items.
//////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
//////////        // for each of: Face, Armor, Helmet, Weapon.
//////////        riderVisual?.ShowRider(equipment);

//////////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////////    }

//////////    /// <summary>
//////////    /// Returns the soldier to the ground and resets the horse to Idle.
//////////    /// Wire this to a UI "Dismount" button or call it from an external system.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (seat == null || !seat.IsOccupied) return;

//////////        // Hide rider Images before the soldier is reparented away
//////////        riderVisual?.HideRider();

//////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////////        seat.MountedSoldier.DismountFromHorse();
//////////        seat.ReleaseSoldier();

//////////        _mountedSoldier = null;
//////////        _riderAnimator = null;

//////////        SetState(HorseState.Idle);

//////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////////    }

//////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////////    /// <summary>
//////////    /// Fired by Unity's EventSystem when a dragged object is released over
//////////    /// any Raycast-Target Image on this GameObject.
//////////    /// Accepts soldiers only; ignores anything else.
//////////    /// </summary>
//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////////        if (soldier == null) return;

//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////////            return;
//////////        }

//////////        PerformMount(soldier);
//////////    }

//////////    // ── Internal helpers ──────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////////    /// Safe to call when no rider is present (null-checked).
//////////    /// </summary>
//////////    private void NotifySoldierAnimator(AnimationState riderState)
//////////    {
//////////        _riderAnimator?.SetState(riderState);
//////////    }

//////////    /// <summary>
//////////    /// Maps HorseState → the matching AnimationState for the soldier.
//////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////////    /// both receive this mapped value so rider equipment sprites are selected
//////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
//////////    /// </summary>
//////////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////////    {
//////////        HorseState.Idle => AnimationState.HorseIdle,
//////////        HorseState.Run => AnimationState.HorseRun,
//////////        HorseState.Fight => AnimationState.HorseFight,
//////////        HorseState.Dead => AnimationState.HorseDead,
//////////        _ => AnimationState.HorseIdle,
//////////    };
//////////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Reads all four animation sets directly from HorseData:
///////////        Idle   → idleSprites   / idleFPS     — loops forever
///////////        Run    → runSprites    / runFPS       — auto-returns to Idle
///////////                                                after runCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
///////////                                                after attackCyclesBeforeIdle loops
///////////                                                (0 = loop forever)
///////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
///////////    → Falls back to idleSprites for any clip whose array is empty.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — all four clips supported).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers (used by both Path A and Path B)
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
////////    private int _dataCyclesCompleted;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Rider layers start hidden until a soldier mounts
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer by dt and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
////////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer,
////////                           bool isMainLayer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////            {
////////                frame = (frame + 1) % clip.frames.Length;
////////            }
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////            {
////////                frame++;
////////            }

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;

////////        float fps = _data.GetFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        switch (_state)
////////        {
////////            case HorseState.Dead:
////////                // Play once — freeze on the last frame
////////                if (frame < sprites.Length - 1)
////////                    frame++;
////////                break;

////////            case HorseState.Run:
////////            case HorseState.Fight:
////////                // Advance frame; count completed cycles for auto-return to Idle
////////                frame++;
////////                if (frame >= sprites.Length)
////////                {
////////                    frame = 0;
////////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////////                    if (maxCycles > 0)
////////                    {
////////                        _dataCyclesCompleted++;
////////                        if (_dataCyclesCompleted >= maxCycles)
////////                            SetState(HorseState.Idle);   // auto-return
////////                    }
////////                }
////////                break;

////////            default: // Idle — loop forever
////////                frame = (frame + 1) % sprites.Length;
////////                break;
////////        }

////////        if (_state != HorseState.Idle || frame < sprites.Length)
////////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////////                            bool isMainLayer = true)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // PATH A
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // PATH B
////////        if (!isMainLayer || _data == null) return;

////////        Sprite[] sprites = _data.GetSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new animation state.
////////    /// Resets to frame 0 every time — even when switching to the same state —
////////    /// so swapping horse data always refreshes the displayed sprite.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset counters so the new clip starts fresh
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — wire to UI buttons or call from game systems
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// Force-resets animation state directly so swapping to a new HorseData
////////    /// while already in Idle correctly updates the displayed sprites.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse running.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;
////////        _dataCyclesCompleted = 0;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays.
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Falls back to HorseData sprite arrays directly.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  RIDER RENDERING — SOLDIER'S OWN SPRITES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
/////////  soldier's own SpriteLayerAnimator handles the mounted pose via
/////////  the HorseIdle / HorseRun AnimationStates.
/////////
/////////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
/////////  activated during mount — calling ShowRider while the soldier's
/////////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
/////////  soldier to appear on the horse.
/////////
/////////  If you want to use HorseRiderVisual instead, change MountOnHorse
/////////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
/////////  riderVisual?.ShowRider(equipment) line in PerformMount below.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//////             "Auto-found in children if left empty.\n" +
//////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;
//////    private int _dataCyclesCompleted;

//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;
//////    private HorseData _data;

//////    // ── Public queries ────────────────────────────────────────────────────────

//////    public HorseData Data => _data;
//////    public HorseState CurrentState => _state;
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
//////    }

//////    private void Start()
//////    {
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // PATH A: SO-driven
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // PATH B: HorseData fallback (main layer only)
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);
//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                if (frame < sprites.Length - 1) frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);
//////                    }
//////                }
//////                break;

//////            default:
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;
//////        frame = 0;

//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        if (!isMainLayer || _data == null) return;
//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    public void Setup(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;
//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop (IDropHandler) or externally.
//////    ///
//////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
//////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
//////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
//////    ///
//////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
//////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
//////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
//////    /// visuals appear — the "duplicate soldier" bug.
//////    ///
//////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
//////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

//////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
//////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
//////        seat.MountSoldier(soldier);

//////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
//////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
//////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
//////        // duplicate — two overlapping soldier visuals on the horse.
//////        // SetState below will still call riderVisual.SetRiderState() which is
//////        // harmless because HideRider was already called in Start().
//////        // ────────────────────────────────────────────────────────────────────────

//////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // HideRider is safe to call even though ShowRider was never called
//////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
//////        riderVisual?.HideRider();

//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController  (mount / equipment fix)
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  MOUNT FLOW (fixed)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. PerformMount(soldier)
///////       → HorseSeat.MountSoldier(soldier)          [position fix here]
///////           → SetParent(SoldierSeat, false)
///////           → anchoredPosition = seatOffset
///////           → soldier.MountOnHorse(seat)
///////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
///////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
///////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
///////       → SetState(HorseState.Idle)
///////
///////  RENDERING PATH DECISION
///////  ───────────────────────
///////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
///////  rider's equipment, and hide the soldier's own CanvasGroup so only
///////  one visual is visible. This avoids the "duplicate soldier" bug.
///////
///////  If you prefer the soldier's own SpriteLayerAnimator to drive
///////  everything (and skip the 4 seat Images), reverse the two lines
///////  flagged RENDERING_CHOICE below.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  BUG FIXES vs previous version
/////// ════════════════════════════════════════════════════════════════════
///////
///////  FIX 1 — Soldier jumps to wrong position on drop
///////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
///////    soldier's anchoredPosition is set explicitly instead of being
///////    derived from its drag-release screen coordinate.
///////
///////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
///////    PerformMount now calls riderVisual.ShowRider(equipment) after
///////    hiding the soldier's own CanvasGroup. The 4 seat Images are
///////    populated from the soldier's CharacterEquipment and animated by
///////    NotifySoldierAnimator(HorseIdle).
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////        if (riderVisual == null)
////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    ///
////    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
////    ///
////    ///  Step 1  HorseSeat.MountSoldier(soldier)
////    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
////    ///          → anchoredPosition = seatOffset
////    ///          → soldier.MountOnHorse(seat)
////    ///
////    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
////    ///          Prevents the "duplicate soldier" — the soldier's body is now
////    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
////    ///          will show.                                            [FIX 2]
////    ///
////    ///  Step 3  riderVisual.ShowRider(equipment)
////    ///          Populates Face / Helmet / Weapon / Armor Images from the
////    ///          soldier's CharacterEquipment.                         [FIX 2]
////    ///
////    ///  Step 4  NotifySoldierAnimator(HorseIdle)
////    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
////    ///          equipment sprites animate in the mounted pose.
////    ///
////    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
////    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
////    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
////    ///  path instead:
////    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
////    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
////    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache before reparenting
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
////        var equipment = soldier.GetComponent<CharacterEquipment>();

////        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
////        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
////        // snaps to seatOffset instead of jumping to its drag-release position.
////        seat.MountSoldier(soldier);

////        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
////        // Comment this line out if you want the soldier's own SpriteLayerAnimator
////        // to drive everything instead of the 4 seat Images.
////        if (_soldierCanvasGroup != null)
////            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

////        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
////        // Comment this line out if using the "soldier's own visuals" path.
////        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

////        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        NotifySoldierAnimator(riderState);
////        riderVisual?.SetRiderState(riderState);

////        // Horse itself switches to Idle (also re-notifies rider — harmless)
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
////                  $"Equipment shown via HorseRiderVisual.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // Hide the 4 seat Images
////        riderVisual?.HideRider();

////        // Restore the soldier's own CanvasGroup so it is visible on the ground
////        if (_soldierCanvasGroup != null)
////            _soldierCanvasGroup.alpha = 1f;

////        // Reparent the soldier back to its original parent + restore ground state
////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;
////        _soldierCanvasGroup = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler ──────────────────────────────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController  (fixed)
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Falls back to HorseData sprite arrays directly:
///////////        Idle  → HorseData.idleSprites  / idleFPS
///////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///////////    → This keeps every existing HorseData asset working without
///////////      requiring a HorseAnimationSO to be created first.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  BUG FIXES vs previous rewrite
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  FIX 1 — Idle never played
///////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///////////    null, so HorseData.idleSprites were never shown.  Both methods now
///////////    fall back to HorseData when the SO is absent.
///////////
///////////  FIX 2 — Horse swap did nothing
///////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
///////////    "if (_state == newState) return" — so swapping to a new horse while
///////////    already Idle skipped every frame update.
///////////    Setup() / SetupWalk() now force-reset the animation directly,
///////////    bypassing the equality guard entirely.
///////////
///////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
///////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///////////    sprite arrays are selected.  HorseController now always maps
///////////    HorseState → AnimationState correctly before notifying the rider.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — no SO required).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        // Works whether horseAnimSO is assigned or not.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Make sure rider layers start hidden
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer's timer and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
////////    ///   2. HorseData sprite array (if _data != null)
////////    ///   3. Early-return silently  (nothing to show yet)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////                frame = (frame + 1) % clip.frames.Length;
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////                frame++;

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        // Only the main horseImage layer uses HorseData; the saddle layer has
////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;
////////        float fps = GetDataFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        // Dead state: play once and freeze
////////        if (_state == HorseState.Dead)
////////        {
////////            if (frame < sprites.Length - 1) frame++;
////////        }
////////        else
////////        {
////////            frame = (frame + 1) % sprites.Length;
////////        }

////////        img.sprite = sprites[frame];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    ///
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

////////    /// <summary>
////////    /// Maps a HorseState to the best available HorseData sprite array.
////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
////////    /// </summary>
////////    private Sprite[] GetDataSprites(HorseState state)
////////    {
////////        if (_data == null) return null;

////////        switch (state)
////////        {
////////            case HorseState.Run:
////////                // walkSprites → idleSprites
////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
////////                    ? _data.walkSprites
////////                    : _data.idleSprites;

////////            case HorseState.Fight:
////////                // No dedicated fight clip in HorseData — use idle
////////                return _data.idleSprites;

////////            case HorseState.Dead:
////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
////////                return _data.idleSprites;

////////            default: // Idle
////////                return _data.idleSprites;
////////        }
////////    }

////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
////////    private float GetDataFPS(HorseState state)
////////    {
////////        if (_data == null) return 6f;

////////        return (state == HorseState.Run
////////                && _data.walkSprites != null
////////                && _data.walkSprites.Length > 0)
////////            ? _data.walkFPS
////////            : _data.idleFPS;
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new state.
////////    /// Both the horse Images and all four rider body-part Images are updated.
////////    /// Calling with the same state as the current one still resets to frame 0.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset frame counters so the new clip starts from frame 0
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — hook these to UI buttons or external controllers
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// FIX: Force-resets animation state directly instead of routing through
////////    /// SetState(), so swapping to a new HorseData while already in Idle
////////    /// correctly updates the displayed sprites instead of being a no-op.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        // Force full animation reset — bypasses the old equality guard so that
////////        // swapping horses (same state, new sprite array) always takes effect.
////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse walking.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    ///
////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
////////    /// the zone assigns a new horse while the controller is already in Run.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items.
////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
////////        // for each of: Face, Armor, Helmet, Weapon.
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call it from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////    → Full control per clip: custom fps, loop flag, frame array.
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Reads all four animation sets directly from HorseData:
/////////        Idle   → idleSprites   / idleFPS     — loops forever
/////////        Run    → runSprites    / runFPS       — auto-returns to Idle
/////////                                                after runCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
/////////                                                after attackCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
/////////    → Falls back to idleSprites for any clip whose array is empty.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP CHECKLIST
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////  □ horseImage wired (or auto-found via GetComponent)
/////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////                   use HorseData sprite arrays (backward-compatible)
/////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////  □ Canvas root: GraphicRaycaster enabled
/////////  □ Scene: EventSystem present
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////             "(backward-compatible mode — all four clips supported).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////             "Leave null if your horse is a single-layer sprite.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    // Per-layer animation timers (used by both Path A and Path B)
//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;

//////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
//////    private int _dataCyclesCompleted;

//////    // Rider references (captured at mount time, cleared at dismount)
//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Rider body-part layers will not animate.", this);
//////    }

//////    private void Start()
//////    {
//////        // Show frame 0 immediately so the horse doesn't appear blank.
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Rider layers start hidden until a soldier mounts
//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Advances one Image layer by dt and updates the sprite.
//////    ///
//////    /// Priority:
//////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
//////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
//////    /// </summary>
//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////            {
//////                frame = (frame + 1) % clip.frames.Length;
//////            }
//////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////            {
//////                frame++;
//////            }

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);

//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                // Play once — freeze on the last frame
//////                if (frame < sprites.Length - 1)
//////                    frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                // Advance frame; count completed cycles for auto-return to Idle
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);   // auto-return
//////                    }
//////                }
//////                break;

//////            default: // Idle — loop forever
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (_state != HorseState.Idle || frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    /// <summary>
//////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////    /// Same two-path priority as TickLayer.
//////    /// </summary>
//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;

//////        frame = 0;

//////        // PATH A
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        // PATH B
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    /// <summary>Current animation state.</summary>
//////    public HorseState CurrentState => _state;

//////    /// <summary>True while a soldier is seated on this horse.</summary>
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    /// <summary>
//////    /// Switches the horse (and mounted rider) to a new animation state.
//////    /// Resets to frame 0 every time — even when switching to the same state —
//////    /// so swapping horse data always refreshes the displayed sprite.
//////    /// </summary>
//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        // Reset counters so the new clip starts fresh
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        // Map horse state → rider AnimationState and notify both systems
//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    // Convenience shorthands — wire to UI buttons or call from game systems
//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////    public HorseData Data => _data;
//////    private HorseData _data;

//////    /// <summary>
//////    /// Called by HorseSlot to initialise a slotted horse.
//////    /// Stores the HorseData reference and starts the Idle animation.
//////    ///
//////    /// Force-resets animation state directly so swapping to a new HorseData
//////    /// while already in Idle correctly updates the displayed sprites.
//////    /// </summary>
//////    public void Setup(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    /// <summary>
//////    /// Called by HorseWalkZone to start the horse running.
//////    /// Stores the HorseData reference and switches to Run state.
//////    /// After the zone finishes, call SetIdle() to return to Idle.
//////    /// </summary>
//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////    ///
//////    /// The soldier's CharacterEquipment is read to populate the four rider
//////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////        seat.MountSoldier(soldier);

//////        // Show the 4 rider Images using the soldier's equipped items
//////        riderVisual?.ShowRider(equipment);

//////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide rider Images before the soldier is reparented away
//////        riderVisual?.HideRider();

//////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    /// <summary>
//////    /// Fired by Unity's EventSystem when a dragged object is released over
//////    /// any Raycast-Target Image on this GameObject.
//////    /// Accepts soldiers only; ignores anything else.
//////    /// </summary>
//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////    /// Safe to call when no rider is present (null-checked).
//////    /// </summary>
//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    /// <summary>
//////    /// Maps HorseState → the matching AnimationState for the soldier.
//////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////    /// both receive this mapped value so rider equipment sprites are selected
//////    /// from the correct EquipmentItem horse arrays.
//////    /// </summary>
//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  ANIMATION — TWO PATHS (auto-selected)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  PATH A  horseAnimSO assigned in Inspector
///////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////
///////  PATH B  horseAnimSO left null  (backward-compatible)
///////    → Falls back to HorseData sprite arrays directly.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  RIDER RENDERING — SOLDIER'S OWN SPRITES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
///////  soldier's own SpriteLayerAnimator handles the mounted pose via
///////  the HorseIdle / HorseRun AnimationStates.
///////
///////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
///////  activated during mount — calling ShowRider while the soldier's
///////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
///////  soldier to appear on the horse.
///////
///////  If you want to use HorseRiderVisual instead, change MountOnHorse
///////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
///////  riderVisual?.ShowRider(equipment) line in PerformMount below.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    /// Called by OnDrop (IDropHandler) or externally.
////    ///
////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
////    ///
////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
////    /// visuals appear — the "duplicate soldier" bug.
////    ///
////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache references before MountOnHorse() reparents the soldier
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
////        seat.MountSoldier(soldier);

////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
////        // duplicate — two overlapping soldier visuals on the horse.
////        // SetState below will still call riderVisual.SetRiderState() which is
////        // harmless because HideRider was already called in Start().
////        // ────────────────────────────────────────────────────────────────────────

////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// Wire this to a UI "Dismount" button or call from an external system.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // HideRider is safe to call even though ShowRider was never called
////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
////        riderVisual?.HideRider();

////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — HorseController  (mount / equipment fix)
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
/////  MOUNT FLOW (fixed)
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. PerformMount(soldier)
/////       → HorseSeat.MountSoldier(soldier)          [position fix here]
/////           → SetParent(SoldierSeat, false)
/////           → anchoredPosition = seatOffset
/////           → soldier.MountOnHorse(seat)
/////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
/////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
/////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
/////       → SetState(HorseState.Idle)
/////
/////  RENDERING PATH DECISION
/////  ───────────────────────
/////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
/////  rider's equipment, and hide the soldier's own CanvasGroup so only
/////  one visual is visible. This avoids the "duplicate soldier" bug.
/////
/////  If you prefer the soldier's own SpriteLayerAnimator to drive
/////  everything (and skip the 4 seat Images), reverse the two lines
/////  flagged RENDERING_CHOICE below.
/////
///// ════════════════════════════════════════════════════════════════════
/////  BUG FIXES vs previous version
///// ════════════════════════════════════════════════════════════════════
/////
/////  FIX 1 — Soldier jumps to wrong position on drop
/////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
/////    soldier's anchoredPosition is set explicitly instead of being
/////    derived from its drag-release screen coordinate.
/////
/////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
/////    PerformMount now calls riderVisual.ShowRider(equipment) after
/////    hiding the soldier's own CanvasGroup. The 4 seat Images are
/////    populated from the soldier's CharacterEquipment and animated by
/////    NotifySoldierAnimator(HorseIdle).
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class HorseController : MonoBehaviour, IDropHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Animation Data")]
//    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//    [SerializeField] private HorseAnimationSO horseAnimSO;

//    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//    [SerializeField] private HorseAnimationSO saddleAnimSO;

//    [Header("Image Layers")]
//    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//    [SerializeField] private Image horseImage;

//    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//    [SerializeField] private Image saddleImage;

//    [Header("Seat & Rider")]
//    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//    [SerializeField] private HorseSeat seat;

//    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//             "Auto-found in children if left empty.\n" +
//             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
//    [SerializeField] private HorseRiderVisual riderVisual;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseState _state = HorseState.Idle;

//    private float _horseTimer;
//    private float _saddleTimer;
//    private int _horseFrame;
//    private int _saddleFrame;
//    private int _dataCyclesCompleted;

//    private SoldierDragDrop _mountedSoldier;
//    private SpriteLayerAnimator _riderAnimator;
//    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
//    private HorseData _data;

//    // ── Public queries ────────────────────────────────────────────────────────

//    public HorseData Data => _data;
//    public HorseState CurrentState => _state;
//    public bool IsOccupied => seat != null && seat.IsOccupied;

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
//                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
//    }

//    private void Start()
//    {
//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        riderVisual?.HideRider();
//    }

//    private void Update()
//    {
//        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//        if (saddleImage != null && saddleAnimSO != null)
//            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//    }

//    // ── Animation Engine ──────────────────────────────────────────────────────

//    private void TickLayer(HorseAnimationSO so, Image img,
//                           ref int frame, ref float timer,
//                           bool isMainLayer)
//    {
//        if (img == null) return;

//        // PATH A: SO-driven
//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//            timer += Time.deltaTime;
//            if (timer < 1f / clip.fps) return;
//            timer -= 1f / clip.fps;

//            if (clip.loop)
//                frame = (frame + 1) % clip.frames.Length;
//            else if (frame < clip.frames.Length - 1)
//                frame++;

//            img.sprite = clip.frames[frame];
//            return;
//        }

//        // PATH B: HorseData fallback (main layer only)
//        if (!isMainLayer || _data == null) return;

//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites == null || sprites.Length == 0) return;

//        float fps = _data.GetFPS(_state);
//        timer += Time.deltaTime;
//        if (timer < 1f / fps) return;
//        timer -= 1f / fps;

//        switch (_state)
//        {
//            case HorseState.Dead:
//                if (frame < sprites.Length - 1) frame++;
//                break;

//            case HorseState.Run:
//            case HorseState.Fight:
//                frame++;
//                if (frame >= sprites.Length)
//                {
//                    frame = 0;
//                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//                    if (maxCycles > 0)
//                    {
//                        _dataCyclesCompleted++;
//                        if (_dataCyclesCompleted >= maxCycles)
//                            SetState(HorseState.Idle);
//                    }
//                }
//                break;

//            default:
//                frame = (frame + 1) % sprites.Length;
//                break;
//        }

//        if (frame < sprites.Length)
//            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//    }

//    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//                            bool isMainLayer = true)
//    {
//        if (img == null) return;
//        frame = 0;

//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//            img.sprite = clip.frames[0];
//            return;
//        }

//        if (!isMainLayer || _data == null) return;
//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites != null && sprites.Length > 0)
//            img.sprite = sprites[0];
//    }

//    // ── Public API — State ────────────────────────────────────────────────────

//    public void SetState(HorseState newState)
//    {
//        _state = newState;

//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(newState);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] '{name}' → {newState}");
//    }

//    public void SetIdle() => SetState(HorseState.Idle);
//    public void SetRun() => SetState(HorseState.Run);
//    public void SetFight() => SetState(HorseState.Fight);
//    public void SetDead() => SetState(HorseState.Dead);

//    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//    public void Setup(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Idle;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        riderVisual?.SetRiderState(riderState);

//        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//    }

//    public void SetupWalk(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Run;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Run);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//    }

//    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//    /// <summary>
//    /// Accepts a soldier into the seat.
//    ///
//    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
//    ///
//    ///  Step 1  HorseSeat.MountSoldier(soldier)
//    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
//    ///          → anchoredPosition = seatOffset
//    ///          → soldier.MountOnHorse(seat)
//    ///
//    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
//    ///          Prevents the "duplicate soldier" — the soldier's body is now
//    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
//    ///          will show.                                            [FIX 2]
//    ///
//    ///  Step 3  riderVisual.ShowRider(equipment)
//    ///          Populates Face / Helmet / Weapon / Armor Images from the
//    ///          soldier's CharacterEquipment.                         [FIX 2]
//    ///
//    ///  Step 4  NotifySoldierAnimator(HorseIdle)
//    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
//    ///          equipment sprites animate in the mounted pose.
//    ///
//    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
//    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
//    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
//    ///  path instead:
//    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
//    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
//    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
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

//        // Cache before reparenting
//        _mountedSoldier = soldier;
//        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
//        var equipment = soldier.GetComponent<CharacterEquipment>();

//        // ── Step 1: Reparent + position ───────────────────────────────────────
//        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
//        // snaps to seatOffset instead of jumping to its drag-release position.
//        seat.MountSoldier(soldier);

//        // ── Step 2: Hide the soldier's own CanvasGroup ────────────────────────
//        // MountOnHorse() deliberately does NOT call HideOwnVisuals() — it
//        // defers to us so the hide runs AFTER ShowRider() (below), ensuring
//        // the rider-visual Images are populated before the soldier goes dark.
//        // Without this line the soldier's sprites stay at alpha=1 while
//        // HorseRiderVisual also renders → two soldiers visible simultaneously.
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 0f;

//        // ── Step 3: Populate the 4 seat Images (Face/Helmet/Weapon/Armor) ─────
//        // ShowRider internally calls SetRiderStateInternal(HorseIdle) — do NOT
//        // call SetRiderState again here or it will trigger a duplicate pass.
//        // SetState below is the single authoritative state notification.
//        riderVisual?.ShowRider(equipment);

//        // ── Step 4: single authoritative state transition ─────────────────────
//        // SetState notifies both riderVisual.SetRiderState and NotifySoldierAnimator
//        // exactly once. Do not call either directly before this line.
//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
//                  $"Equipment shown via HorseRiderVisual.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and resets the horse to Idle.
//    /// </summary>
//    public void PerformDismount()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        // Hide the 4 seat Images
//        riderVisual?.HideRider();

//        // Restore the soldier's own CanvasGroup so it is visible on the ground
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 1f;

//        // Reparent the soldier back to its original parent + restore ground state
//        seat.MountedSoldier.DismountFromHorse();
//        seat.ReleaseSoldier();

//        _mountedSoldier = null;
//        _riderAnimator = null;
//        _soldierCanvasGroup = null;

//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//    }

//    // ── IDropHandler ──────────────────────────────────────────────────────────

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

//    // ── Internal helpers ──────────────────────────────────────────────────────

//    private void NotifySoldierAnimator(AnimationState riderState)
//    {
//        _riderAnimator?.SetState(riderState);
//    }

//    private static AnimationState MapToRiderState(HorseState state) => state switch
//    {
//        HorseState.Idle => AnimationState.HorseIdle,
//        HorseState.Run => AnimationState.HorseRun,
//        HorseState.Fight => AnimationState.HorseFight,
//        HorseState.Dead => AnimationState.HorseDead,
//        _ => AnimationState.HorseIdle,
//    };
//}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController  (fixed)
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Falls back to HorseData sprite arrays directly:
///////////        Idle  → HorseData.idleSprites  / idleFPS
///////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///////////    → This keeps every existing HorseData asset working without
///////////      requiring a HorseAnimationSO to be created first.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  BUG FIXES vs previous rewrite
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  FIX 1 — Idle never played
///////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///////////    null, so HorseData.idleSprites were never shown.  Both methods now
///////////    fall back to HorseData when the SO is absent.
///////////
///////////  FIX 2 — Horse swap did nothing
///////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
///////////    "if (_state == newState) return" — so swapping to a new horse while
///////////    already Idle skipped every frame update.
///////////    Setup() / SetupWalk() now force-reset the animation directly,
///////////    bypassing the equality guard entirely.
///////////
///////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
///////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///////////    sprite arrays are selected.  HorseController now always maps
///////////    HorseState → AnimationState correctly before notifying the rider.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — no SO required).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        // Works whether horseAnimSO is assigned or not.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Make sure rider layers start hidden
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer's timer and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
////////    ///   2. HorseData sprite array (if _data != null)
////////    ///   3. Early-return silently  (nothing to show yet)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////                frame = (frame + 1) % clip.frames.Length;
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////                frame++;

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        // Only the main horseImage layer uses HorseData; the saddle layer has
////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;
////////        float fps = GetDataFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        // Dead state: play once and freeze
////////        if (_state == HorseState.Dead)
////////        {
////////            if (frame < sprites.Length - 1) frame++;
////////        }
////////        else
////////        {
////////            frame = (frame + 1) % sprites.Length;
////////        }

////////        img.sprite = sprites[frame];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    ///
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

////////    /// <summary>
////////    /// Maps a HorseState to the best available HorseData sprite array.
////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
////////    /// </summary>
////////    private Sprite[] GetDataSprites(HorseState state)
////////    {
////////        if (_data == null) return null;

////////        switch (state)
////////        {
////////            case HorseState.Run:
////////                // walkSprites → idleSprites
////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
////////                    ? _data.walkSprites
////////                    : _data.idleSprites;

////////            case HorseState.Fight:
////////                // No dedicated fight clip in HorseData — use idle
////////                return _data.idleSprites;

////////            case HorseState.Dead:
////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
////////                return _data.idleSprites;

////////            default: // Idle
////////                return _data.idleSprites;
////////        }
////////    }

////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
////////    private float GetDataFPS(HorseState state)
////////    {
////////        if (_data == null) return 6f;

////////        return (state == HorseState.Run
////////                && _data.walkSprites != null
////////                && _data.walkSprites.Length > 0)
////////            ? _data.walkFPS
////////            : _data.idleFPS;
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new state.
////////    /// Both the horse Images and all four rider body-part Images are updated.
////////    /// Calling with the same state as the current one still resets to frame 0.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset frame counters so the new clip starts from frame 0
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — hook these to UI buttons or external controllers
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// FIX: Force-resets animation state directly instead of routing through
////////    /// SetState(), so swapping to a new HorseData while already in Idle
////////    /// correctly updates the displayed sprites instead of being a no-op.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        // Force full animation reset — bypasses the old equality guard so that
////////        // swapping horses (same state, new sprite array) always takes effect.
////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse walking.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    ///
////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
////////    /// the zone assigns a new horse while the controller is already in Run.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items.
////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
////////        // for each of: Face, Armor, Helmet, Weapon.
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call it from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////    → Full control per clip: custom fps, loop flag, frame array.
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Reads all four animation sets directly from HorseData:
/////////        Idle   → idleSprites   / idleFPS     — loops forever
/////////        Run    → runSprites    / runFPS       — auto-returns to Idle
/////////                                                after runCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
/////////                                                after attackCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
/////////    → Falls back to idleSprites for any clip whose array is empty.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP CHECKLIST
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////  □ horseImage wired (or auto-found via GetComponent)
/////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////                   use HorseData sprite arrays (backward-compatible)
/////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////  □ Canvas root: GraphicRaycaster enabled
/////////  □ Scene: EventSystem present
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////             "(backward-compatible mode — all four clips supported).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////             "Leave null if your horse is a single-layer sprite.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    // Per-layer animation timers (used by both Path A and Path B)
//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;

//////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
//////    private int _dataCyclesCompleted;

//////    // Rider references (captured at mount time, cleared at dismount)
//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Rider body-part layers will not animate.", this);
//////    }

//////    private void Start()
//////    {
//////        // Show frame 0 immediately so the horse doesn't appear blank.
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Rider layers start hidden until a soldier mounts
//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Advances one Image layer by dt and updates the sprite.
//////    ///
//////    /// Priority:
//////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
//////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
//////    /// </summary>
//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////            {
//////                frame = (frame + 1) % clip.frames.Length;
//////            }
//////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////            {
//////                frame++;
//////            }

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);

//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                // Play once — freeze on the last frame
//////                if (frame < sprites.Length - 1)
//////                    frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                // Advance frame; count completed cycles for auto-return to Idle
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);   // auto-return
//////                    }
//////                }
//////                break;

//////            default: // Idle — loop forever
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (_state != HorseState.Idle || frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    /// <summary>
//////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////    /// Same two-path priority as TickLayer.
//////    /// </summary>
//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;

//////        frame = 0;

//////        // PATH A
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        // PATH B
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    /// <summary>Current animation state.</summary>
//////    public HorseState CurrentState => _state;

//////    /// <summary>True while a soldier is seated on this horse.</summary>
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    /// <summary>
//////    /// Switches the horse (and mounted rider) to a new animation state.
//////    /// Resets to frame 0 every time — even when switching to the same state —
//////    /// so swapping horse data always refreshes the displayed sprite.
//////    /// </summary>
//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        // Reset counters so the new clip starts fresh
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        // Map horse state → rider AnimationState and notify both systems
//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    // Convenience shorthands — wire to UI buttons or call from game systems
//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////    public HorseData Data => _data;
//////    private HorseData _data;

//////    /// <summary>
//////    /// Called by HorseSlot to initialise a slotted horse.
//////    /// Stores the HorseData reference and starts the Idle animation.
//////    ///
//////    /// Force-resets animation state directly so swapping to a new HorseData
//////    /// while already in Idle correctly updates the displayed sprites.
//////    /// </summary>
//////    public void Setup(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    /// <summary>
//////    /// Called by HorseWalkZone to start the horse running.
//////    /// Stores the HorseData reference and switches to Run state.
//////    /// After the zone finishes, call SetIdle() to return to Idle.
//////    /// </summary>
//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////    ///
//////    /// The soldier's CharacterEquipment is read to populate the four rider
//////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////        seat.MountSoldier(soldier);

//////        // Show the 4 rider Images using the soldier's equipped items
//////        riderVisual?.ShowRider(equipment);

//////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide rider Images before the soldier is reparented away
//////        riderVisual?.HideRider();

//////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    /// <summary>
//////    /// Fired by Unity's EventSystem when a dragged object is released over
//////    /// any Raycast-Target Image on this GameObject.
//////    /// Accepts soldiers only; ignores anything else.
//////    /// </summary>
//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////    /// Safe to call when no rider is present (null-checked).
//////    /// </summary>
//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    /// <summary>
//////    /// Maps HorseState → the matching AnimationState for the soldier.
//////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////    /// both receive this mapped value so rider equipment sprites are selected
//////    /// from the correct EquipmentItem horse arrays.
//////    /// </summary>
//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  ANIMATION — TWO PATHS (auto-selected)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  PATH A  horseAnimSO assigned in Inspector
///////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////
///////  PATH B  horseAnimSO left null  (backward-compatible)
///////    → Falls back to HorseData sprite arrays directly.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  RIDER RENDERING — SOLDIER'S OWN SPRITES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
///////  soldier's own SpriteLayerAnimator handles the mounted pose via
///////  the HorseIdle / HorseRun AnimationStates.
///////
///////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
///////  activated during mount — calling ShowRider while the soldier's
///////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
///////  soldier to appear on the horse.
///////
///////  If you want to use HorseRiderVisual instead, change MountOnHorse
///////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
///////  riderVisual?.ShowRider(equipment) line in PerformMount below.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    /// Called by OnDrop (IDropHandler) or externally.
////    ///
////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
////    ///
////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
////    /// visuals appear — the "duplicate soldier" bug.
////    ///
////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache references before MountOnHorse() reparents the soldier
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
////        seat.MountSoldier(soldier);

////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
////        // duplicate — two overlapping soldier visuals on the horse.
////        // SetState below will still call riderVisual.SetRiderState() which is
////        // harmless because HideRider was already called in Start().
////        // ────────────────────────────────────────────────────────────────────────

////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// Wire this to a UI "Dismount" button or call from an external system.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // HideRider is safe to call even though ShowRider was never called
////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
////        riderVisual?.HideRider();

////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — HorseController  (mount / equipment fix)
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
/////  MOUNT FLOW (fixed)
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. PerformMount(soldier)
/////       → HorseSeat.MountSoldier(soldier)          [position fix here]
/////           → SetParent(SoldierSeat, false)
/////           → anchoredPosition = seatOffset
/////           → soldier.MountOnHorse(seat)
/////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
/////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
/////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
/////       → SetState(HorseState.Idle)
/////
/////  RENDERING PATH DECISION
/////  ───────────────────────
/////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
/////  rider's equipment, and hide the soldier's own CanvasGroup so only
/////  one visual is visible. This avoids the "duplicate soldier" bug.
/////
/////  If you prefer the soldier's own SpriteLayerAnimator to drive
/////  everything (and skip the 4 seat Images), reverse the two lines
/////  flagged RENDERING_CHOICE below.
/////
///// ════════════════════════════════════════════════════════════════════
/////  BUG FIXES vs previous version
///// ════════════════════════════════════════════════════════════════════
/////
/////  FIX 1 — Soldier jumps to wrong position on drop
/////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
/////    soldier's anchoredPosition is set explicitly instead of being
/////    derived from its drag-release screen coordinate.
/////
/////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
/////    PerformMount now calls riderVisual.ShowRider(equipment) after
/////    hiding the soldier's own CanvasGroup. The 4 seat Images are
/////    populated from the soldier's CharacterEquipment and animated by
/////    NotifySoldierAnimator(HorseIdle).
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class HorseController : MonoBehaviour, IDropHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Animation Data")]
//    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//    [SerializeField] private HorseAnimationSO horseAnimSO;

//    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//    [SerializeField] private HorseAnimationSO saddleAnimSO;

//    [Header("Image Layers")]
//    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//    [SerializeField] private Image horseImage;

//    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//    [SerializeField] private Image saddleImage;

//    [Header("Seat & Rider")]
//    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//    [SerializeField] private HorseSeat seat;

//    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//             "Auto-found in children if left empty.\n" +
//             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
//    [SerializeField] private HorseRiderVisual riderVisual;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseState _state = HorseState.Idle;

//    private float _horseTimer;
//    private float _saddleTimer;
//    private int _horseFrame;
//    private int _saddleFrame;
//    private int _dataCyclesCompleted;

//    private SoldierDragDrop _mountedSoldier;
//    private SpriteLayerAnimator _riderAnimator;
//    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
//    private HorseData _data;

//    // ── Public queries ────────────────────────────────────────────────────────

//    public HorseData Data => _data;
//    public HorseState CurrentState => _state;
//    public bool IsOccupied => seat != null && seat.IsOccupied;

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
//                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
//    }

//    private void Start()
//    {
//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        riderVisual?.HideRider();
//    }

//    private void Update()
//    {
//        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//        if (saddleImage != null && saddleAnimSO != null)
//            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//    }

//    // ── Animation Engine ──────────────────────────────────────────────────────

//    private void TickLayer(HorseAnimationSO so, Image img,
//                           ref int frame, ref float timer,
//                           bool isMainLayer)
//    {
//        if (img == null) return;

//        // PATH A: SO-driven
//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//            timer += Time.deltaTime;
//            if (timer < 1f / clip.fps) return;
//            timer -= 1f / clip.fps;

//            if (clip.loop)
//                frame = (frame + 1) % clip.frames.Length;
//            else if (frame < clip.frames.Length - 1)
//                frame++;

//            img.sprite = clip.frames[frame];
//            return;
//        }

//        // PATH B: HorseData fallback (main layer only)
//        if (!isMainLayer || _data == null) return;

//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites == null || sprites.Length == 0) return;

//        float fps = _data.GetFPS(_state);
//        timer += Time.deltaTime;
//        if (timer < 1f / fps) return;
//        timer -= 1f / fps;

//        switch (_state)
//        {
//            case HorseState.Dead:
//                if (frame < sprites.Length - 1) frame++;
//                break;

//            case HorseState.Run:
//            case HorseState.Fight:
//                frame++;
//                if (frame >= sprites.Length)
//                {
//                    frame = 0;
//                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//                    if (maxCycles > 0)
//                    {
//                        _dataCyclesCompleted++;
//                        if (_dataCyclesCompleted >= maxCycles)
//                            SetState(HorseState.Idle);
//                    }
//                }
//                break;

//            default:
//                frame = (frame + 1) % sprites.Length;
//                break;
//        }

//        if (frame < sprites.Length)
//            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//    }

//    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//                            bool isMainLayer = true)
//    {
//        if (img == null) return;
//        frame = 0;

//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//            img.sprite = clip.frames[0];
//            return;
//        }

//        if (!isMainLayer || _data == null) return;
//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites != null && sprites.Length > 0)
//            img.sprite = sprites[0];
//    }

//    // ── Public API — State ────────────────────────────────────────────────────

//    public void SetState(HorseState newState)
//    {
//        _state = newState;

//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(newState);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] '{name}' → {newState}");
//    }

//    public void SetIdle() => SetState(HorseState.Idle);
//    public void SetRun() => SetState(HorseState.Run);
//    public void SetFight() => SetState(HorseState.Fight);
//    public void SetDead() => SetState(HorseState.Dead);

//    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//    public void Setup(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Idle;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        riderVisual?.SetRiderState(riderState);

//        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//    }

//    public void SetupWalk(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Run;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Run);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//    }

//    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//    /// <summary>
//    /// Accepts a soldier into the seat.
//    ///
//    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
//    ///
//    ///  Step 1  HorseSeat.MountSoldier(soldier)
//    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
//    ///          → anchoredPosition = seatOffset
//    ///          → soldier.MountOnHorse(seat)
//    ///
//    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
//    ///          Prevents the "duplicate soldier" — the soldier's body is now
//    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
//    ///          will show.                                            [FIX 2]
//    ///
//    ///  Step 3  riderVisual.ShowRider(equipment)
//    ///          Populates Face / Helmet / Weapon / Armor Images from the
//    ///          soldier's CharacterEquipment.                         [FIX 2]
//    ///
//    ///  Step 4  NotifySoldierAnimator(HorseIdle)
//    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
//    ///          equipment sprites animate in the mounted pose.
//    ///
//    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
//    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
//    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
//    ///  path instead:
//    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
//    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
//    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
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

//        // Cache before reparenting
//        _mountedSoldier = soldier;
//        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
//        var equipment = soldier.GetComponent<CharacterEquipment>();

//        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
//        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
//        // snaps to seatOffset instead of jumping to its drag-release position.
//        seat.MountSoldier(soldier);

//        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
//        // Comment this line out if you want the soldier's own SpriteLayerAnimator
//        // to drive everything instead of the 4 seat Images.
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

//        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
//        // Comment this line out if using the "soldier's own visuals" path.
//        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

//        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        NotifySoldierAnimator(riderState);
//        riderVisual?.SetRiderState(riderState);

//        // Horse itself switches to Idle (also re-notifies rider — harmless)
//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
//                  $"Equipment shown via HorseRiderVisual.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and resets the horse to Idle.
//    /// </summary>
//    public void PerformDismount()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        // Hide the 4 seat Images
//        riderVisual?.HideRider();

//        // Restore the soldier's own CanvasGroup so it is visible on the ground
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 1f;

//        // Reparent the soldier back to its original parent + restore ground state
//        seat.MountedSoldier.DismountFromHorse();
//        seat.ReleaseSoldier();

//        _mountedSoldier = null;
//        _riderAnimator = null;
//        _soldierCanvasGroup = null;

//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//    }

//    // ── IDropHandler ──────────────────────────────────────────────────────────

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

//    // ── Internal helpers ──────────────────────────────────────────────────────

//    private void NotifySoldierAnimator(AnimationState riderState)
//    {
//        _riderAnimator?.SetState(riderState);
//    }

//    private static AnimationState MapToRiderState(HorseState state) => state switch
//    {
//        HorseState.Idle => AnimationState.HorseIdle,
//        HorseState.Run => AnimationState.HorseRun,
//        HorseState.Fight => AnimationState.HorseFight,
//        HorseState.Dead => AnimationState.HorseDead,
//        _ => AnimationState.HorseIdle,
//    };
//}

////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — HorseController  (fixed)
///////////
/////////// Attach to the HorsePrefab root alongside:
///////////   RectTransform, Image, CanvasGroup
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HORSE PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////////           ├── Face    (Image)
///////////           ├── Armor   (Image)
///////////           ├── Helmet  (Image)
///////////           └── Weapon  (Image)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  ANIMATION — TWO PATHS (auto-selected)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  PATH A  horseAnimSO assigned in Inspector
///////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////////    → Full control per clip: custom fps, loop flag, frame array.
///////////
///////////  PATH B  horseAnimSO left null  (backward-compatible)
///////////    → Falls back to HorseData sprite arrays directly:
///////////        Idle  → HorseData.idleSprites  / idleFPS
///////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
///////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
///////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
///////////    → This keeps every existing HorseData asset working without
///////////      requiring a HorseAnimationSO to be created first.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  BUG FIXES vs previous rewrite
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  FIX 1 — Idle never played
///////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
///////////    null, so HorseData.idleSprites were never shown.  Both methods now
///////////    fall back to HorseData when the SO is absent.
///////////
///////////  FIX 2 — Horse swap did nothing
///////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
///////////    "if (_state == newState) return" — so swapping to a new horse while
///////////    already Idle skipped every frame update.
///////////    Setup() / SetupWalk() now force-reset the animation directly,
///////////    bypassing the equality guard entirely.
///////////
///////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
///////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
///////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
///////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
///////////    sprite arrays are selected.  HorseController now always maps
///////////    HorseState → AnimationState correctly before notifying the rider.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP CHECKLIST
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  □ HorseController + Image + CanvasGroup  on prefab root
///////////  □ horseImage wired (or auto-found via GetComponent)
///////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////////                   use HorseData sprite arrays (backward-compatible)
///////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////////  □ Canvas root: GraphicRaycaster enabled
///////////  □ Scene: EventSystem present
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class HorseController : MonoBehaviour, IDropHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Animation Data")]
////////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////////             "(backward-compatible mode — no SO required).")]
////////    [SerializeField] private HorseAnimationSO horseAnimSO;

////////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////////             "Leave null if your horse is a single-layer sprite.")]
////////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////////    [Header("Image Layers")]
////////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////////    [SerializeField] private Image horseImage;

////////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////////    [SerializeField] private Image saddleImage;

////////    [Header("Seat & Rider")]
////////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseSeat seat;

////////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////////             "Auto-found in children if left empty.")]
////////    [SerializeField] private HorseRiderVisual riderVisual;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseState _state = HorseState.Idle;

////////    // Per-layer animation timers
////////    private float _horseTimer;
////////    private float _saddleTimer;
////////    private int _horseFrame;
////////    private int _saddleFrame;

////////    // Rider references (captured at mount time, cleared at dismount)
////////    private SoldierDragDrop _mountedSoldier;
////////    private SpriteLayerAnimator _riderAnimator;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (horseImage == null)
////////            horseImage = GetComponent<Image>();

////////        if (seat == null)
////////            seat = GetComponentInChildren<HorseSeat>();

////////        if (riderVisual == null)
////////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////////        if (horseImage == null)
////////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////////        if (seat == null)
////////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////////        if (riderVisual == null)
////////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////////                             "Rider body-part layers will not animate.", this);
////////    }

////////    private void Start()
////////    {
////////        // Show frame 0 immediately so the horse doesn't appear blank.
////////        // Works whether horseAnimSO is assigned or not.
////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Make sure rider layers start hidden
////////        riderVisual?.HideRider();
////////    }

////////    private void Update()
////////    {
////////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

////////        if (saddleImage != null && saddleAnimSO != null)
////////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
////////    }

////////    // ── Animation Engine ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Advances one Image layer's timer and updates the sprite.
////////    ///
////////    /// Priority:
////////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
////////    ///   2. HorseData sprite array (if _data != null)
////////    ///   3. Early-return silently  (nothing to show yet)
////////    /// </summary>
////////    private void TickLayer(HorseAnimationSO so, Image img,
////////                           ref int frame, ref float timer)
////////    {
////////        if (img == null) return;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////////            timer += Time.deltaTime;
////////            if (timer < 1f / clip.fps) return;
////////            timer -= 1f / clip.fps;

////////            if (clip.loop)
////////                frame = (frame + 1) % clip.frames.Length;
////////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////////                frame++;

////////            img.sprite = clip.frames[frame];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        // Only the main horseImage layer uses HorseData; the saddle layer has
////////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites == null || sprites.Length == 0) return;
////////        float fps = GetDataFPS(_state);

////////        timer += Time.deltaTime;
////////        if (timer < 1f / fps) return;
////////        timer -= 1f / fps;

////////        // Dead state: play once and freeze
////////        if (_state == HorseState.Dead)
////////        {
////////            if (frame < sprites.Length - 1) frame++;
////////        }
////////        else
////////        {
////////            frame = (frame + 1) % sprites.Length;
////////        }

////////        img.sprite = sprites[frame];
////////    }

////////    /// <summary>
////////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////////    ///
////////    /// Same two-path priority as TickLayer.
////////    /// </summary>
////////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
////////    {
////////        if (img == null) return;

////////        frame = 0;

////////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////////        if (so != null)
////////        {
////////            HorseClip clip = so.GetClip(_state);
////////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////////            img.sprite = clip.frames[0];
////////            return;
////////        }

////////        // ── PATH B: HorseData fallback ───────────────────────────────────────
////////        if (_data == null || img != horseImage) return;

////////        Sprite[] sprites = GetDataSprites(_state);
////////        if (sprites != null && sprites.Length > 0)
////////            img.sprite = sprites[0];
////////    }

////////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

////////    /// <summary>
////////    /// Maps a HorseState to the best available HorseData sprite array.
////////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
////////    /// </summary>
////////    private Sprite[] GetDataSprites(HorseState state)
////////    {
////////        if (_data == null) return null;

////////        switch (state)
////////        {
////////            case HorseState.Run:
////////                // walkSprites → idleSprites
////////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
////////                    ? _data.walkSprites
////////                    : _data.idleSprites;

////////            case HorseState.Fight:
////////                // No dedicated fight clip in HorseData — use idle
////////                return _data.idleSprites;

////////            case HorseState.Dead:
////////                // No dedicated dead clip in HorseData — freeze on idle frame 0
////////                return _data.idleSprites;

////////            default: // Idle
////////                return _data.idleSprites;
////////        }
////////    }

////////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
////////    private float GetDataFPS(HorseState state)
////////    {
////////        if (_data == null) return 6f;

////////        return (state == HorseState.Run
////////                && _data.walkSprites != null
////////                && _data.walkSprites.Length > 0)
////////            ? _data.walkFPS
////////            : _data.idleFPS;
////////    }

////////    // ── Public API — State ────────────────────────────────────────────────────

////////    /// <summary>Current animation state.</summary>
////////    public HorseState CurrentState => _state;

////////    /// <summary>True while a soldier is seated on this horse.</summary>
////////    public bool IsOccupied => seat != null && seat.IsOccupied;

////////    /// <summary>
////////    /// Switches the horse (and mounted rider) to a new state.
////////    /// Both the horse Images and all four rider body-part Images are updated.
////////    /// Calling with the same state as the current one still resets to frame 0.
////////    /// </summary>
////////    public void SetState(HorseState newState)
////////    {
////////        _state = newState;

////////        // Reset frame counters so the new clip starts from frame 0
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Map horse state → rider AnimationState and notify both systems
////////        AnimationState riderState = MapToRiderState(newState);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] '{name}' → {newState}");
////////    }

////////    // Convenience shorthands — hook these to UI buttons or external controllers
////////    public void SetIdle() => SetState(HorseState.Idle);
////////    public void SetRun() => SetState(HorseState.Run);
////////    public void SetFight() => SetState(HorseState.Fight);
////////    public void SetDead() => SetState(HorseState.Dead);

////////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////////    public HorseData Data => _data;
////////    private HorseData _data;

////////    /// <summary>
////////    /// Called by HorseSlot to initialise a slotted horse.
////////    /// Stores the HorseData reference and starts the Idle animation.
////////    ///
////////    /// FIX: Force-resets animation state directly instead of routing through
////////    /// SetState(), so swapping to a new HorseData while already in Idle
////////    /// correctly updates the displayed sprites instead of being a no-op.
////////    /// </summary>
////////    public void Setup(HorseData data)
////////    {
////////        _data = data;

////////        // Force full animation reset — bypasses the old equality guard so that
////////        // swapping horses (same state, new sprite array) always takes effect.
////////        _state = HorseState.Idle;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
////////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////////        riderVisual?.SetRiderState(riderState);

////////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////////    }

////////    /// <summary>
////////    /// Called by HorseWalkZone to start the horse walking.
////////    /// Stores the HorseData reference and switches to Run state.
////////    /// After the zone finishes, call SetIdle() to return to Idle.
////////    ///
////////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
////////    /// the zone assigns a new horse while the controller is already in Run.
////////    /// </summary>
////////    public void SetupWalk(HorseData data)
////////    {
////////        _data = data;

////////        _state = HorseState.Run;
////////        _horseFrame = _saddleFrame = 0;
////////        _horseTimer = _saddleTimer = 0f;

////////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////////        if (saddleImage != null && saddleAnimSO != null)
////////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////////        AnimationState riderState = MapToRiderState(HorseState.Run);
////////        riderVisual?.SetRiderState(riderState);
////////        NotifySoldierAnimator(riderState);

////////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////////    }

////////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////////    /// <summary>
////////    /// Accepts a soldier into the seat.
////////    /// Called by OnDrop or externally (e.g. a formation spawner).
////////    ///
////////    /// The soldier's CharacterEquipment is read to populate the four rider
////////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
////////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////////            return;
////////        }

////////        if (soldier == null) return;

////////        // Cache references before MountOnHorse() reparents the soldier
////////        _mountedSoldier = soldier;
////////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////////        var equipment = soldier.GetComponent<CharacterEquipment>();

////////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////////        seat.MountSoldier(soldier);

////////        // Show the 4 rider Images using the soldier's equipped items.
////////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
////////        // for each of: Face, Armor, Helmet, Weapon.
////////        riderVisual?.ShowRider(equipment);

////////        // Start in Idle state (SetState also notifies riderVisual & animator)
////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////////    }

////////    /// <summary>
////////    /// Returns the soldier to the ground and resets the horse to Idle.
////////    /// Wire this to a UI "Dismount" button or call it from an external system.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        if (seat == null || !seat.IsOccupied) return;

////////        // Hide rider Images before the soldier is reparented away
////////        riderVisual?.HideRider();

////////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////////        seat.MountedSoldier.DismountFromHorse();
////////        seat.ReleaseSoldier();

////////        _mountedSoldier = null;
////////        _riderAnimator = null;

////////        SetState(HorseState.Idle);

////////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////////    }

////////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////////    /// <summary>
////////    /// Fired by Unity's EventSystem when a dragged object is released over
////////    /// any Raycast-Target Image on this GameObject.
////////    /// Accepts soldiers only; ignores anything else.
////////    /// </summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////////        if (soldier == null) return;

////////        if (seat == null)
////////        {
////////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////////            return;
////////        }

////////        PerformMount(soldier);
////////    }

////////    // ── Internal helpers ──────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////////    /// Safe to call when no rider is present (null-checked).
////////    /// </summary>
////////    private void NotifySoldierAnimator(AnimationState riderState)
////////    {
////////        _riderAnimator?.SetState(riderState);
////////    }

////////    /// <summary>
////////    /// Maps HorseState → the matching AnimationState for the soldier.
////////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////////    /// both receive this mapped value so rider equipment sprites are selected
////////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
////////    /// </summary>
////////    private static AnimationState MapToRiderState(HorseState state) => state switch
////////    {
////////        HorseState.Idle => AnimationState.HorseIdle,
////////        HorseState.Run => AnimationState.HorseRun,
////////        HorseState.Fight => AnimationState.HorseFight,
////////        HorseState.Dead => AnimationState.HorseDead,
////////        _ => AnimationState.HorseIdle,
////////    };
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////    → Full control per clip: custom fps, loop flag, frame array.
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Reads all four animation sets directly from HorseData:
/////////        Idle   → idleSprites   / idleFPS     — loops forever
/////////        Run    → runSprites    / runFPS       — auto-returns to Idle
/////////                                                after runCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Attack → attackSprites / attackFPS   — auto-returns to Idle
/////////                                                after attackCyclesBeforeIdle loops
/////////                                                (0 = loop forever)
/////////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
/////////    → Falls back to idleSprites for any clip whose array is empty.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP CHECKLIST
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////  □ horseImage wired (or auto-found via GetComponent)
/////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////                   use HorseData sprite arrays (backward-compatible)
/////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////  □ Canvas root: GraphicRaycaster enabled
/////////  □ Scene: EventSystem present
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////             "(backward-compatible mode — all four clips supported).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////             "Leave null if your horse is a single-layer sprite.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    // Per-layer animation timers (used by both Path A and Path B)
//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;

//////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
//////    private int _dataCyclesCompleted;

//////    // Rider references (captured at mount time, cleared at dismount)
//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Rider body-part layers will not animate.", this);
//////    }

//////    private void Start()
//////    {
//////        // Show frame 0 immediately so the horse doesn't appear blank.
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Rider layers start hidden until a soldier mounts
//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Advances one Image layer by dt and updates the sprite.
//////    ///
//////    /// Priority:
//////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
//////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
//////    /// </summary>
//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer,
//////                           bool isMainLayer)
//////    {
//////        if (img == null) return;

//////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////            {
//////                frame = (frame + 1) % clip.frames.Length;
//////            }
//////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////            {
//////                frame++;
//////            }

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;

//////        float fps = _data.GetFPS(_state);

//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        switch (_state)
//////        {
//////            case HorseState.Dead:
//////                // Play once — freeze on the last frame
//////                if (frame < sprites.Length - 1)
//////                    frame++;
//////                break;

//////            case HorseState.Run:
//////            case HorseState.Fight:
//////                // Advance frame; count completed cycles for auto-return to Idle
//////                frame++;
//////                if (frame >= sprites.Length)
//////                {
//////                    frame = 0;
//////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//////                    if (maxCycles > 0)
//////                    {
//////                        _dataCyclesCompleted++;
//////                        if (_dataCyclesCompleted >= maxCycles)
//////                            SetState(HorseState.Idle);   // auto-return
//////                    }
//////                }
//////                break;

//////            default: // Idle — loop forever
//////                frame = (frame + 1) % sprites.Length;
//////                break;
//////        }

//////        if (_state != HorseState.Idle || frame < sprites.Length)
//////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//////    }

//////    /// <summary>
//////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////    /// Same two-path priority as TickLayer.
//////    /// </summary>
//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//////                            bool isMainLayer = true)
//////    {
//////        if (img == null) return;

//////        frame = 0;

//////        // PATH A
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        // PATH B
//////        if (!isMainLayer || _data == null) return;

//////        Sprite[] sprites = _data.GetSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    /// <summary>Current animation state.</summary>
//////    public HorseState CurrentState => _state;

//////    /// <summary>True while a soldier is seated on this horse.</summary>
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    /// <summary>
//////    /// Switches the horse (and mounted rider) to a new animation state.
//////    /// Resets to frame 0 every time — even when switching to the same state —
//////    /// so swapping horse data always refreshes the displayed sprite.
//////    /// </summary>
//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        // Reset counters so the new clip starts fresh
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        // Map horse state → rider AnimationState and notify both systems
//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    // Convenience shorthands — wire to UI buttons or call from game systems
//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////    public HorseData Data => _data;
//////    private HorseData _data;

//////    /// <summary>
//////    /// Called by HorseSlot to initialise a slotted horse.
//////    /// Stores the HorseData reference and starts the Idle animation.
//////    ///
//////    /// Force-resets animation state directly so swapping to a new HorseData
//////    /// while already in Idle correctly updates the displayed sprites.
//////    /// </summary>
//////    public void Setup(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    /// <summary>
//////    /// Called by HorseWalkZone to start the horse running.
//////    /// Stores the HorseData reference and switches to Run state.
//////    /// After the zone finishes, call SetIdle() to return to Idle.
//////    /// </summary>
//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;
//////        _dataCyclesCompleted = 0;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////    ///
//////    /// The soldier's CharacterEquipment is read to populate the four rider
//////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////        seat.MountSoldier(soldier);

//////        // Show the 4 rider Images using the soldier's equipped items
//////        riderVisual?.ShowRider(equipment);

//////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide rider Images before the soldier is reparented away
//////        riderVisual?.HideRider();

//////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    /// <summary>
//////    /// Fired by Unity's EventSystem when a dragged object is released over
//////    /// any Raycast-Target Image on this GameObject.
//////    /// Accepts soldiers only; ignores anything else.
//////    /// </summary>
//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////    /// Safe to call when no rider is present (null-checked).
//////    /// </summary>
//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    /// <summary>
//////    /// Maps HorseState → the matching AnimationState for the soldier.
//////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////    /// both receive this mapped value so rider equipment sprites are selected
//////    /// from the correct EquipmentItem horse arrays.
//////    /// </summary>
//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  ANIMATION — TWO PATHS (auto-selected)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  PATH A  horseAnimSO assigned in Inspector
///////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////
///////  PATH B  horseAnimSO left null  (backward-compatible)
///////    → Falls back to HorseData sprite arrays directly.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  RIDER RENDERING — SOLDIER'S OWN SPRITES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
///////  soldier's own SpriteLayerAnimator handles the mounted pose via
///////  the HorseIdle / HorseRun AnimationStates.
///////
///////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
///////  activated during mount — calling ShowRider while the soldier's
///////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
///////  soldier to appear on the horse.
///////
///////  If you want to use HorseRiderVisual instead, change MountOnHorse
///////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
///////  riderVisual?.ShowRider(equipment) line in PerformMount below.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
////             "Auto-found in children if left empty.\n" +
////             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;
////    private int _dataCyclesCompleted;

////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;
////    private HorseData _data;

////    // ── Public queries ────────────────────────────────────────────────────────

////    public HorseData Data => _data;
////    public HorseState CurrentState => _state;
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);
////    }

////    private void Start()
////    {
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // PATH A: SO-driven
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////                frame = (frame + 1) % clip.frames.Length;
////            else if (frame < clip.frames.Length - 1)
////                frame++;

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // PATH B: HorseData fallback (main layer only)
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);
////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                if (frame < sprites.Length - 1) frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);
////                    }
////                }
////                break;

////            default:
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;
////        frame = 0;

////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        if (!isMainLayer || _data == null) return;
////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    public void Setup(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    public void SetupWalk(HorseData data)
////    {
////        _data = data;
////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    /// Called by OnDrop (IDropHandler) or externally.
////    ///
////    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
////    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
////    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
////    ///
////    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
////    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
////    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
////    /// visuals appear — the "duplicate soldier" bug.
////    ///
////    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
////    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache references before MountOnHorse() reparents the soldier
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();

////        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
////        // Soldier's own SpriteLayerAnimator drives the mounted pose.
////        seat.MountSoldier(soldier);

////        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
////        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
////        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
////        // duplicate — two overlapping soldier visuals on the horse.
////        // SetState below will still call riderVisual.SetRiderState() which is
////        // harmless because HideRider was already called in Start().
////        // ────────────────────────────────────────────────────────────────────────

////        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// Wire this to a UI "Dismount" button or call from an external system.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // HideRider is safe to call even though ShowRider was never called
////        // (all 4 Images are already hidden from Start() / the previous HideRider call).
////        riderVisual?.HideRider();

////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — HorseController  (mount / equipment fix)
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
/////  MOUNT FLOW (fixed)
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. PerformMount(soldier)
/////       → HorseSeat.MountSoldier(soldier)          [position fix here]
/////           → SetParent(SoldierSeat, false)
/////           → anchoredPosition = seatOffset
/////           → soldier.MountOnHorse(seat)
/////       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
/////       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
/////       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
/////       → SetState(HorseState.Idle)
/////
/////  RENDERING PATH DECISION
/////  ───────────────────────
/////  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
/////  rider's equipment, and hide the soldier's own CanvasGroup so only
/////  one visual is visible. This avoids the "duplicate soldier" bug.
/////
/////  If you prefer the soldier's own SpriteLayerAnimator to drive
/////  everything (and skip the 4 seat Images), reverse the two lines
/////  flagged RENDERING_CHOICE below.
/////
///// ════════════════════════════════════════════════════════════════════
/////  BUG FIXES vs previous version
///// ════════════════════════════════════════════════════════════════════
/////
/////  FIX 1 — Soldier jumps to wrong position on drop
/////    HorseSeat.MountSoldier now uses worldPositionStays:false so the
/////    soldier's anchoredPosition is set explicitly instead of being
/////    derived from its drag-release screen coordinate.
/////
/////  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
/////    PerformMount now calls riderVisual.ShowRider(equipment) after
/////    hiding the soldier's own CanvasGroup. The 4 seat Images are
/////    populated from the soldier's CharacterEquipment and animated by
/////    NotifySoldierAnimator(HorseIdle).
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class HorseController : MonoBehaviour, IDropHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Animation Data")]
//    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//    [SerializeField] private HorseAnimationSO horseAnimSO;

//    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//    [SerializeField] private HorseAnimationSO saddleAnimSO;

//    [Header("Image Layers")]
//    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//    [SerializeField] private Image horseImage;

//    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//    [SerializeField] private Image saddleImage;

//    [Header("Seat & Rider")]
//    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//    [SerializeField] private HorseSeat seat;

//    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//             "Auto-found in children if left empty.\n" +
//             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
//    [SerializeField] private HorseRiderVisual riderVisual;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseState _state = HorseState.Idle;

//    private float _horseTimer;
//    private float _saddleTimer;
//    private int _horseFrame;
//    private int _saddleFrame;
//    private int _dataCyclesCompleted;

//    private SoldierDragDrop _mountedSoldier;
//    private SpriteLayerAnimator _riderAnimator;
//    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
//    private HorseData _data;

//    // ── Public queries ────────────────────────────────────────────────────────

//    public HorseData Data => _data;
//    public HorseState CurrentState => _state;
//    public bool IsOccupied => seat != null && seat.IsOccupied;

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
//                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
//    }

//    private void Start()
//    {
//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        riderVisual?.HideRider();
//    }

//    private void Update()
//    {
//        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//        if (saddleImage != null && saddleAnimSO != null)
//            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//    }

//    // ── Animation Engine ──────────────────────────────────────────────────────

//    private void TickLayer(HorseAnimationSO so, Image img,
//                           ref int frame, ref float timer,
//                           bool isMainLayer)
//    {
//        if (img == null) return;

//        // PATH A: SO-driven
//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//            timer += Time.deltaTime;
//            if (timer < 1f / clip.fps) return;
//            timer -= 1f / clip.fps;

//            if (clip.loop)
//                frame = (frame + 1) % clip.frames.Length;
//            else if (frame < clip.frames.Length - 1)
//                frame++;

//            img.sprite = clip.frames[frame];
//            return;
//        }

//        // PATH B: HorseData fallback (main layer only)
//        if (!isMainLayer || _data == null) return;

//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites == null || sprites.Length == 0) return;

//        float fps = _data.GetFPS(_state);
//        timer += Time.deltaTime;
//        if (timer < 1f / fps) return;
//        timer -= 1f / fps;

//        switch (_state)
//        {
//            case HorseState.Dead:
//                if (frame < sprites.Length - 1) frame++;
//                break;

//            case HorseState.Run:
//            case HorseState.Fight:
//                frame++;
//                if (frame >= sprites.Length)
//                {
//                    frame = 0;
//                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//                    if (maxCycles > 0)
//                    {
//                        _dataCyclesCompleted++;
//                        if (_dataCyclesCompleted >= maxCycles)
//                            SetState(HorseState.Idle);
//                    }
//                }
//                break;

//            default:
//                frame = (frame + 1) % sprites.Length;
//                break;
//        }

//        if (frame < sprites.Length)
//            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//    }

//    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//                            bool isMainLayer = true)
//    {
//        if (img == null) return;
//        frame = 0;

//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//            img.sprite = clip.frames[0];
//            return;
//        }

//        if (!isMainLayer || _data == null) return;
//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites != null && sprites.Length > 0)
//            img.sprite = sprites[0];
//    }

//    // ── Public API — State ────────────────────────────────────────────────────

//    public void SetState(HorseState newState)
//    {
//        _state = newState;

//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(newState);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] '{name}' → {newState}");
//    }

//    public void SetIdle() => SetState(HorseState.Idle);
//    public void SetRun() => SetState(HorseState.Run);
//    public void SetFight() => SetState(HorseState.Fight);
//    public void SetDead() => SetState(HorseState.Dead);

//    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//    public void Setup(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Idle;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        riderVisual?.SetRiderState(riderState);

//        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//    }

//    public void SetupWalk(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Run;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Run);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//    }

//    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//    /// <summary>
//    /// Accepts a soldier into the seat.
//    ///
//    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
//    ///
//    ///  Step 1  HorseSeat.MountSoldier(soldier)
//    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
//    ///          → anchoredPosition = seatOffset
//    ///          → soldier.MountOnHorse(seat)
//    ///
//    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
//    ///          Prevents the "duplicate soldier" — the soldier's body is now
//    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
//    ///          will show.                                            [FIX 2]
//    ///
//    ///  Step 3  riderVisual.ShowRider(equipment)
//    ///          Populates Face / Helmet / Weapon / Armor Images from the
//    ///          soldier's CharacterEquipment.                         [FIX 2]
//    ///
//    ///  Step 4  NotifySoldierAnimator(HorseIdle)
//    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
//    ///          equipment sprites animate in the mounted pose.
//    ///
//    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
//    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
//    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
//    ///  path instead:
//    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
//    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
//    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
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

//        // Cache before reparenting
//        _mountedSoldier = soldier;
//        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
//        var equipment = soldier.GetComponent<CharacterEquipment>();

//        // ── Step 1: Reparent + position (FIX 1) ──────────────────────────────
//        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
//        // snaps to seatOffset instead of jumping to its drag-release position.
//        seat.MountSoldier(soldier);

//        // ── Step 2: Hide soldier's own CanvasGroup (RENDERING_CHOICE A) ──────
//        // Comment this line out if you want the soldier's own SpriteLayerAnimator
//        // to drive everything instead of the 4 seat Images.
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 0f;                    // RENDERING_CHOICE A

//        // ── Step 3: Show Face / Helmet / Weapon / Armor (FIX 2) ──────────────
//        // Comment this line out if using the "soldier's own visuals" path.
//        riderVisual?.ShowRider(equipment);                     // RENDERING_CHOICE B

//        // ── Step 4: Animate equipment in HorseIdle pose ───────────────────────
//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        NotifySoldierAnimator(riderState);
//        riderVisual?.SetRiderState(riderState);

//        // Horse itself switches to Idle (also re-notifies rider — harmless)
//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
//                  $"Equipment shown via HorseRiderVisual.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and resets the horse to Idle.
//    /// </summary>
//    public void PerformDismount()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        // Hide the 4 seat Images
//        riderVisual?.HideRider();

//        // Restore the soldier's own CanvasGroup so it is visible on the ground
//        if (_soldierCanvasGroup != null)
//            _soldierCanvasGroup.alpha = 1f;

//        // Reparent the soldier back to its original parent + restore ground state
//        seat.MountedSoldier.DismountFromHorse();
//        seat.ReleaseSoldier();

//        _mountedSoldier = null;
//        _riderAnimator = null;
//        _soldierCanvasGroup = null;

//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//    }

//    // ── IDropHandler ──────────────────────────────────────────────────────────

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

//    // ── Internal helpers ──────────────────────────────────────────────────────

//    private void NotifySoldierAnimator(AnimationState riderState)
//    {
//        _riderAnimator?.SetState(riderState);
//    }

//    private static AnimationState MapToRiderState(HorseState state) => state switch
//    {
//        HorseState.Idle => AnimationState.HorseIdle,
//        HorseState.Run => AnimationState.HorseRun,
//        HorseState.Fight => AnimationState.HorseFight,
//        HorseState.Dead => AnimationState.HorseDead,
//        _ => AnimationState.HorseIdle,
//    };
//}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — HorseController  (fixed)
/////////
///////// Attach to the HorsePrefab root alongside:
/////////   RectTransform, Image, CanvasGroup
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HORSE PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Horse  ← HorseController + Image (horse body) + CanvasGroup
/////////     ├── SaddleLayer   (optional Image — saddle / bridle)
/////////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
/////////           ├── Face    (Image)
/////////           ├── Armor   (Image)
/////////           ├── Helmet  (Image)
/////////           └── Weapon  (Image)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  ANIMATION — TWO PATHS (auto-selected)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  PATH A  horseAnimSO assigned in Inspector
/////////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////////    → Full control per clip: custom fps, loop flag, frame array.
/////////
/////////  PATH B  horseAnimSO left null  (backward-compatible)
/////////    → Falls back to HorseData sprite arrays directly:
/////////        Idle  → HorseData.idleSprites  / idleFPS
/////////        Run   → HorseData.walkSprites  / walkFPS  (falls back to idle)
/////////        Fight → HorseData.idleSprites  / idleFPS  (no dedicated fight clip)
/////////        Dead  → HorseData.idleSprites  / idleFPS  frozen on frame 0
/////////    → This keeps every existing HorseData asset working without
/////////      requiring a HorseAnimationSO to be created first.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  BUG FIXES vs previous rewrite
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  FIX 1 — Idle never played
/////////    TickLayer / ApplyFrame returned immediately when horseAnimSO was
/////////    null, so HorseData.idleSprites were never shown.  Both methods now
/////////    fall back to HorseData when the SO is absent.
/////////
/////////  FIX 2 — Horse swap did nothing
/////////    Setup(HorseData) called SetState(HorseState.Idle) which contained
/////////    "if (_state == newState) return" — so swapping to a new horse while
/////////    already Idle skipped every frame update.
/////////    Setup() / SetupWalk() now force-reset the animation directly,
/////////    bypassing the equality guard entirely.
/////////
/////////  FIX 3 — Rider slots (Face / Armor / Helmet / Weapon)
/////////    EquipmentItem.GetSprites(HorseIdle, bodyType) is the correct call.
/////////    HorseRiderVisual.ShowRider() / SetRiderState() must use
/////////    AnimationState.HorseIdle (not AnimationState.Idle) so the right
/////////    sprite arrays are selected.  HorseController now always maps
/////////    HorseState → AnimationState correctly before notifying the rider.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP CHECKLIST
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  □ HorseController + Image + CanvasGroup  on prefab root
/////////  □ horseImage wired (or auto-found via GetComponent)
/////////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
/////////                   use HorseData sprite arrays (backward-compatible)
/////////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
/////////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
/////////  □ Canvas root: GraphicRaycaster enabled
/////////  □ Scene: EventSystem present
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class HorseController : MonoBehaviour, IDropHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Animation Data")]
//////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
//////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
//////             "(backward-compatible mode — no SO required).")]
//////    [SerializeField] private HorseAnimationSO horseAnimSO;

//////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
//////             "Leave null if your horse is a single-layer sprite.")]
//////    [SerializeField] private HorseAnimationSO saddleAnimSO;

//////    [Header("Image Layers")]
//////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//////    [SerializeField] private Image horseImage;

//////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//////    [SerializeField] private Image saddleImage;

//////    [Header("Seat & Rider")]
//////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseSeat seat;

//////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
//////             "Auto-found in children if left empty.")]
//////    [SerializeField] private HorseRiderVisual riderVisual;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseState _state = HorseState.Idle;

//////    // Per-layer animation timers
//////    private float _horseTimer;
//////    private float _saddleTimer;
//////    private int _horseFrame;
//////    private int _saddleFrame;

//////    // Rider references (captured at mount time, cleared at dismount)
//////    private SoldierDragDrop _mountedSoldier;
//////    private SpriteLayerAnimator _riderAnimator;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (horseImage == null)
//////            horseImage = GetComponent<Image>();

//////        if (seat == null)
//////            seat = GetComponentInChildren<HorseSeat>();

//////        if (riderVisual == null)
//////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

//////        if (horseImage == null)
//////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

//////        if (seat == null)
//////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

//////        if (riderVisual == null)
//////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
//////                             "Rider body-part layers will not animate.", this);
//////    }

//////    private void Start()
//////    {
//////        // Show frame 0 immediately so the horse doesn't appear blank.
//////        // Works whether horseAnimSO is assigned or not.
//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Make sure rider layers start hidden
//////        riderVisual?.HideRider();
//////    }

//////    private void Update()
//////    {
//////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer);

//////        if (saddleImage != null && saddleAnimSO != null)
//////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer);
//////    }

//////    // ── Animation Engine ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Advances one Image layer's timer and updates the sprite.
//////    ///
//////    /// Priority:
//////    ///   1. HorseAnimationSO clip  (if so != null and clip has frames)
//////    ///   2. HorseData sprite array (if _data != null)
//////    ///   3. Early-return silently  (nothing to show yet)
//////    /// </summary>
//////    private void TickLayer(HorseAnimationSO so, Image img,
//////                           ref int frame, ref float timer)
//////    {
//////        if (img == null) return;

//////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//////            timer += Time.deltaTime;
//////            if (timer < 1f / clip.fps) return;
//////            timer -= 1f / clip.fps;

//////            if (clip.loop)
//////                frame = (frame + 1) % clip.frames.Length;
//////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
//////                frame++;

//////            img.sprite = clip.frames[frame];
//////            return;
//////        }

//////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////        // Only the main horseImage layer uses HorseData; the saddle layer has
//////        // no HorseData equivalent so it is skipped when saddleAnimSO is null.
//////        if (_data == null || img != horseImage) return;

//////        Sprite[] sprites = GetDataSprites(_state);
//////        if (sprites == null || sprites.Length == 0) return;
//////        float fps = GetDataFPS(_state);

//////        timer += Time.deltaTime;
//////        if (timer < 1f / fps) return;
//////        timer -= 1f / fps;

//////        // Dead state: play once and freeze
//////        if (_state == HorseState.Dead)
//////        {
//////            if (frame < sprites.Length - 1) frame++;
//////        }
//////        else
//////        {
//////            frame = (frame + 1) % sprites.Length;
//////        }

//////        img.sprite = sprites[frame];
//////    }

//////    /// <summary>
//////    /// Resets a layer to frame 0 of the current state and shows it immediately.
//////    ///
//////    /// Same two-path priority as TickLayer.
//////    /// </summary>
//////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so)
//////    {
//////        if (img == null) return;

//////        frame = 0;

//////        // ── PATH A: SO-driven ────────────────────────────────────────────────
//////        if (so != null)
//////        {
//////            HorseClip clip = so.GetClip(_state);
//////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//////            img.sprite = clip.frames[0];
//////            return;
//////        }

//////        // ── PATH B: HorseData fallback ───────────────────────────────────────
//////        if (_data == null || img != horseImage) return;

//////        Sprite[] sprites = GetDataSprites(_state);
//////        if (sprites != null && sprites.Length > 0)
//////            img.sprite = sprites[0];
//////    }

//////    // ── HorseData sprite / fps helpers ───────────────────────────────────────

//////    /// <summary>
//////    /// Maps a HorseState to the best available HorseData sprite array.
//////    /// Fallback order matches EquipmentItem horse fallback chains for consistency.
//////    /// </summary>
//////    private Sprite[] GetDataSprites(HorseState state)
//////    {
//////        if (_data == null) return null;

//////        switch (state)
//////        {
//////            case HorseState.Run:
//////                // walkSprites → idleSprites
//////                return (_data.walkSprites != null && _data.walkSprites.Length > 0)
//////                    ? _data.walkSprites
//////                    : _data.idleSprites;

//////            case HorseState.Fight:
//////                // No dedicated fight clip in HorseData — use idle
//////                return _data.idleSprites;

//////            case HorseState.Dead:
//////                // No dedicated dead clip in HorseData — freeze on idle frame 0
//////                return _data.idleSprites;

//////            default: // Idle
//////                return _data.idleSprites;
//////        }
//////    }

//////    /// <summary>Returns the playback FPS for the current HorseState from HorseData.</summary>
//////    private float GetDataFPS(HorseState state)
//////    {
//////        if (_data == null) return 6f;

//////        return (state == HorseState.Run
//////                && _data.walkSprites != null
//////                && _data.walkSprites.Length > 0)
//////            ? _data.walkFPS
//////            : _data.idleFPS;
//////    }

//////    // ── Public API — State ────────────────────────────────────────────────────

//////    /// <summary>Current animation state.</summary>
//////    public HorseState CurrentState => _state;

//////    /// <summary>True while a soldier is seated on this horse.</summary>
//////    public bool IsOccupied => seat != null && seat.IsOccupied;

//////    /// <summary>
//////    /// Switches the horse (and mounted rider) to a new state.
//////    /// Both the horse Images and all four rider body-part Images are updated.
//////    /// Calling with the same state as the current one still resets to frame 0.
//////    /// </summary>
//////    public void SetState(HorseState newState)
//////    {
//////        _state = newState;

//////        // Reset frame counters so the new clip starts from frame 0
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Map horse state → rider AnimationState and notify both systems
//////        AnimationState riderState = MapToRiderState(newState);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] '{name}' → {newState}");
//////    }

//////    // Convenience shorthands — hook these to UI buttons or external controllers
//////    public void SetIdle() => SetState(HorseState.Idle);
//////    public void SetRun() => SetState(HorseState.Run);
//////    public void SetFight() => SetState(HorseState.Fight);
//////    public void SetDead() => SetState(HorseState.Dead);

//////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
//////    public HorseData Data => _data;
//////    private HorseData _data;

//////    /// <summary>
//////    /// Called by HorseSlot to initialise a slotted horse.
//////    /// Stores the HorseData reference and starts the Idle animation.
//////    ///
//////    /// FIX: Force-resets animation state directly instead of routing through
//////    /// SetState(), so swapping to a new HorseData while already in Idle
//////    /// correctly updates the displayed sprites instead of being a no-op.
//////    /// </summary>
//////    public void Setup(HorseData data)
//////    {
//////        _data = data;

//////        // Force full animation reset — bypasses the old equality guard so that
//////        // swapping horses (same state, new sprite array) always takes effect.
//////        _state = HorseState.Idle;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        // Keep rider visual in sync (no mounted soldier yet — HideRider is safe)
//////        AnimationState riderState = MapToRiderState(HorseState.Idle);
//////        riderVisual?.SetRiderState(riderState);

//////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//////    }

//////    /// <summary>
//////    /// Called by HorseWalkZone to start the horse walking.
//////    /// Stores the HorseData reference and switches to Run state.
//////    /// After the zone finishes, call SetIdle() to return to Idle.
//////    ///
//////    /// FIX: Same force-reset pattern as Setup() — avoids no-op when
//////    /// the zone assigns a new horse while the controller is already in Run.
//////    /// </summary>
//////    public void SetupWalk(HorseData data)
//////    {
//////        _data = data;

//////        _state = HorseState.Run;
//////        _horseFrame = _saddleFrame = 0;
//////        _horseTimer = _saddleTimer = 0f;

//////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//////        if (saddleImage != null && saddleAnimSO != null)
//////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//////        AnimationState riderState = MapToRiderState(HorseState.Run);
//////        riderVisual?.SetRiderState(riderState);
//////        NotifySoldierAnimator(riderState);

//////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//////    }

//////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//////    /// <summary>
//////    /// Accepts a soldier into the seat.
//////    /// Called by OnDrop or externally (e.g. a formation spawner).
//////    ///
//////    /// The soldier's CharacterEquipment is read to populate the four rider
//////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
//////    /// EquipmentItem.GetSprites(AnimationState.HorseIdle, bodyType) is used
//////    /// for each slot — fill horseIdleSprites on each EquipmentItem asset.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
//////            return;
//////        }

//////        if (soldier == null) return;

//////        // Cache references before MountOnHorse() reparents the soldier
//////        _mountedSoldier = soldier;
//////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
//////        var equipment = soldier.GetComponent<CharacterEquipment>();

//////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
//////        seat.MountSoldier(soldier);

//////        // Show the 4 rider Images using the soldier's equipped items.
//////        // HorseRiderVisual.ShowRider calls EquipmentItem.GetSprites(HorseIdle, bodyType)
//////        // for each of: Face, Armor, Helmet, Weapon.
//////        riderVisual?.ShowRider(equipment);

//////        // Start in Idle state (SetState also notifies riderVisual & animator)
//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and resets the horse to Idle.
//////    /// Wire this to a UI "Dismount" button or call it from an external system.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        if (seat == null || !seat.IsOccupied) return;

//////        // Hide rider Images before the soldier is reparented away
//////        riderVisual?.HideRider();

//////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
//////        seat.MountedSoldier.DismountFromHorse();
//////        seat.ReleaseSoldier();

//////        _mountedSoldier = null;
//////        _riderAnimator = null;

//////        SetState(HorseState.Idle);

//////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//////    }

//////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

//////    /// <summary>
//////    /// Fired by Unity's EventSystem when a dragged object is released over
//////    /// any Raycast-Target Image on this GameObject.
//////    /// Accepts soldiers only; ignores anything else.
//////    /// </summary>
//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
//////        if (soldier == null) return;

//////        if (seat == null)
//////        {
//////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
//////            return;
//////        }

//////        PerformMount(soldier);
//////    }

//////    // ── Internal helpers ──────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
//////    /// Safe to call when no rider is present (null-checked).
//////    /// </summary>
//////    private void NotifySoldierAnimator(AnimationState riderState)
//////    {
//////        _riderAnimator?.SetState(riderState);
//////    }

//////    /// <summary>
//////    /// Maps HorseState → the matching AnimationState for the soldier.
//////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
//////    /// both receive this mapped value so rider equipment sprites are selected
//////    /// from the correct EquipmentItem horse arrays (horseIdleSprites, etc.).
//////    /// </summary>
//////    private static AnimationState MapToRiderState(HorseState state) => state switch
//////    {
//////        HorseState.Idle => AnimationState.HorseIdle,
//////        HorseState.Run => AnimationState.HorseRun,
//////        HorseState.Fight => AnimationState.HorseFight,
//////        HorseState.Dead => AnimationState.HorseDead,
//////        _ => AnimationState.HorseIdle,
//////    };
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — HorseController
///////
/////// Attach to the HorsePrefab root alongside:
///////   RectTransform, Image, CanvasGroup
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HORSE PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Horse  ← HorseController + Image (horse body) + CanvasGroup
///////     ├── SaddleLayer   (optional Image — saddle / bridle)
///////     └── SoldierSeat   ← HorseSeat + HorseRiderVisual
///////           ├── Face    (Image)
///////           ├── Armor   (Image)
///////           ├── Helmet  (Image)
///////           └── Weapon  (Image)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  ANIMATION — TWO PATHS (auto-selected)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  PATH A  horseAnimSO assigned in Inspector
///////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
///////    → Full control per clip: custom fps, loop flag, frame array.
///////
///////  PATH B  horseAnimSO left null  (backward-compatible)
///////    → Reads all four animation sets directly from HorseData:
///////        Idle   → idleSprites   / idleFPS     — loops forever
///////        Run    → runSprites    / runFPS       — auto-returns to Idle
///////                                                after runCyclesBeforeIdle loops
///////                                                (0 = loop forever)
///////        Attack → attackSprites / attackFPS   — auto-returns to Idle
///////                                                after attackCyclesBeforeIdle loops
///////                                                (0 = loop forever)
///////        Dead   → deadSprites   / deadFPS     — plays once, freezes on last frame
///////    → Falls back to idleSprites for any clip whose array is empty.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SETUP CHECKLIST
/////// ════════════════════════════════════════════════════════════════════
///////
///////  □ HorseController + Image + CanvasGroup  on prefab root
///////  □ horseImage wired (or auto-found via GetComponent)
///////  □ horseAnimSO  — assign for full SO-driven animation; leave null to
///////                   use HorseData sprite arrays (backward-compatible)
///////  □ SoldierSeat child: HorseSeat + HorseRiderVisual
///////      Children of SoldierSeat: Face / Armor / Helmet / Weapon (Image)
///////  □ Canvas root: GraphicRaycaster enabled
///////  □ Scene: EventSystem present
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class HorseController : MonoBehaviour, IDropHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Animation Data")]
////    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
////             "Create via: right-click Project → Create → AreaForge → Horse Animation.\n\n" +
////             "Leave NULL to drive animation directly from HorseData sprite arrays\n" +
////             "(backward-compatible mode — all four clips supported).")]
////    [SerializeField] private HorseAnimationSO horseAnimSO;

////    [Tooltip("Optional second SO for the saddle/bridle Image layer.\n" +
////             "Leave null if your horse is a single-layer sprite.")]
////    [SerializeField] private HorseAnimationSO saddleAnimSO;

////    [Header("Image Layers")]
////    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
////    [SerializeField] private Image horseImage;

////    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
////    [SerializeField] private Image saddleImage;

////    [Header("Seat & Rider")]
////    [Tooltip("HorseSeat child component (the anchor where the soldier sits).\n" +
////             "Auto-found in children if left empty.")]
////    [SerializeField] private HorseSeat seat;

////    [Tooltip("HorseRiderVisual on the SoldierSeat child — drives the 4 body-part Images.\n" +
////             "Auto-found in children if left empty.")]
////    [SerializeField] private HorseRiderVisual riderVisual;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseState _state = HorseState.Idle;

////    // Per-layer animation timers (used by both Path A and Path B)
////    private float _horseTimer;
////    private float _saddleTimer;
////    private int _horseFrame;
////    private int _saddleFrame;

////    // Path B: cycle counter for auto-return to Idle (Run / Attack)
////    private int _dataCyclesCompleted;

////    // Rider references (captured at mount time, cleared at dismount)
////    private SoldierDragDrop _mountedSoldier;
////    private SpriteLayerAnimator _riderAnimator;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (horseImage == null)
////            horseImage = GetComponent<Image>();

////        if (seat == null)
////            seat = GetComponentInChildren<HorseSeat>();

////        if (riderVisual == null)
////            riderVisual = GetComponentInChildren<HorseRiderVisual>();

////        if (horseImage == null)
////            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

////        if (seat == null)
////            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

////        if (riderVisual == null)
////            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
////                             "Rider body-part layers will not animate.", this);
////    }

////    private void Start()
////    {
////        // Show frame 0 immediately so the horse doesn't appear blank.
////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);

////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

////        // Rider layers start hidden until a soldier mounts
////        riderVisual?.HideRider();
////    }

////    private void Update()
////    {
////        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

////        if (saddleImage != null && saddleAnimSO != null)
////            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
////    }

////    // ── Animation Engine ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Advances one Image layer by dt and updates the sprite.
////    ///
////    /// Priority:
////    ///   PATH A — HorseAnimationSO  (when <paramref name="so"/> is non-null)
////    ///   PATH B — HorseData arrays  (when so is null, main layer only)
////    /// </summary>
////    private void TickLayer(HorseAnimationSO so, Image img,
////                           ref int frame, ref float timer,
////                           bool isMainLayer)
////    {
////        if (img == null) return;

////        // ── PATH A: SO-driven ────────────────────────────────────────────────
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

////            timer += Time.deltaTime;
////            if (timer < 1f / clip.fps) return;
////            timer -= 1f / clip.fps;

////            if (clip.loop)
////            {
////                frame = (frame + 1) % clip.frames.Length;
////            }
////            else if (frame < clip.frames.Length - 1)   // Dead — freeze on last frame
////            {
////                frame++;
////            }

////            img.sprite = clip.frames[frame];
////            return;
////        }

////        // ── PATH B: HorseData fallback (main horseImage layer only) ──────────
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites == null || sprites.Length == 0) return;

////        float fps = _data.GetFPS(_state);

////        timer += Time.deltaTime;
////        if (timer < 1f / fps) return;
////        timer -= 1f / fps;

////        switch (_state)
////        {
////            case HorseState.Dead:
////                // Play once — freeze on the last frame
////                if (frame < sprites.Length - 1)
////                    frame++;
////                break;

////            case HorseState.Run:
////            case HorseState.Fight:
////                // Advance frame; count completed cycles for auto-return to Idle
////                frame++;
////                if (frame >= sprites.Length)
////                {
////                    frame = 0;
////                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
////                    if (maxCycles > 0)
////                    {
////                        _dataCyclesCompleted++;
////                        if (_dataCyclesCompleted >= maxCycles)
////                            SetState(HorseState.Idle);   // auto-return
////                    }
////                }
////                break;

////            default: // Idle — loop forever
////                frame = (frame + 1) % sprites.Length;
////                break;
////        }

////        if (_state != HorseState.Idle || frame < sprites.Length)
////            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
////    }

////    /// <summary>
////    /// Resets a layer to frame 0 of the current state and shows it immediately.
////    /// Same two-path priority as TickLayer.
////    /// </summary>
////    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
////                            bool isMainLayer = true)
////    {
////        if (img == null) return;

////        frame = 0;

////        // PATH A
////        if (so != null)
////        {
////            HorseClip clip = so.GetClip(_state);
////            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
////            img.sprite = clip.frames[0];
////            return;
////        }

////        // PATH B
////        if (!isMainLayer || _data == null) return;

////        Sprite[] sprites = _data.GetSprites(_state);
////        if (sprites != null && sprites.Length > 0)
////            img.sprite = sprites[0];
////    }

////    // ── Public API — State ────────────────────────────────────────────────────

////    /// <summary>Current animation state.</summary>
////    public HorseState CurrentState => _state;

////    /// <summary>True while a soldier is seated on this horse.</summary>
////    public bool IsOccupied => seat != null && seat.IsOccupied;

////    /// <summary>
////    /// Switches the horse (and mounted rider) to a new animation state.
////    /// Resets to frame 0 every time — even when switching to the same state —
////    /// so swapping horse data always refreshes the displayed sprite.
////    /// </summary>
////    public void SetState(HorseState newState)
////    {
////        _state = newState;

////        // Reset counters so the new clip starts fresh
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        // Map horse state → rider AnimationState and notify both systems
////        AnimationState riderState = MapToRiderState(newState);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] '{name}' → {newState}");
////    }

////    // Convenience shorthands — wire to UI buttons or call from game systems
////    public void SetIdle() => SetState(HorseState.Idle);
////    public void SetRun() => SetState(HorseState.Run);
////    public void SetFight() => SetState(HorseState.Fight);
////    public void SetDead() => SetState(HorseState.Dead);

////    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

////    /// <summary>The HorseData assigned via Setup() or SetupWalk(). Null until one is called.</summary>
////    public HorseData Data => _data;
////    private HorseData _data;

////    /// <summary>
////    /// Called by HorseSlot to initialise a slotted horse.
////    /// Stores the HorseData reference and starts the Idle animation.
////    ///
////    /// Force-resets animation state directly so swapping to a new HorseData
////    /// while already in Idle correctly updates the displayed sprites.
////    /// </summary>
////    public void Setup(HorseData data)
////    {
////        _data = data;

////        _state = HorseState.Idle;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Idle);
////        riderVisual?.SetRiderState(riderState);

////        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
////    }

////    /// <summary>
////    /// Called by HorseWalkZone to start the horse running.
////    /// Stores the HorseData reference and switches to Run state.
////    /// After the zone finishes, call SetIdle() to return to Idle.
////    /// </summary>
////    public void SetupWalk(HorseData data)
////    {
////        _data = data;

////        _state = HorseState.Run;
////        _horseFrame = _saddleFrame = 0;
////        _horseTimer = _saddleTimer = 0f;
////        _dataCyclesCompleted = 0;

////        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
////        if (saddleImage != null && saddleAnimSO != null)
////            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

////        AnimationState riderState = MapToRiderState(HorseState.Run);
////        riderVisual?.SetRiderState(riderState);
////        NotifySoldierAnimator(riderState);

////        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
////    }

////    // ── Public API — Mount / Dismount ─────────────────────────────────────────

////    /// <summary>
////    /// Accepts a soldier into the seat.
////    /// Called by OnDrop or externally (e.g. a formation spawner).
////    ///
////    /// The soldier's CharacterEquipment is read to populate the four rider
////    /// Images (Face / Armor / Helmet / Weapon) via HorseRiderVisual.
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
////            return;
////        }

////        if (soldier == null) return;

////        // Cache references before MountOnHorse() reparents the soldier
////        _mountedSoldier = soldier;
////        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
////        var equipment = soldier.GetComponent<CharacterEquipment>();

////        // HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse
////        seat.MountSoldier(soldier);

////        // Show the 4 rider Images using the soldier's equipped items
////        riderVisual?.ShowRider(equipment);

////        // Start in Idle state (SetState also notifies riderVisual & animator)
////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and resets the horse to Idle.
////    /// Wire this to a UI "Dismount" button or call from an external system.
////    /// </summary>
////    public void PerformDismount()
////    {
////        if (seat == null || !seat.IsOccupied) return;

////        // Hide rider Images before the soldier is reparented away
////        riderVisual?.HideRider();

////        // SoldierDragDrop.DismountFromHorse() reparents + restores soldier visuals
////        seat.MountedSoldier.DismountFromHorse();
////        seat.ReleaseSoldier();

////        _mountedSoldier = null;
////        _riderAnimator = null;

////        SetState(HorseState.Idle);

////        Debug.Log($"[HorseController] '{name}': rider dismounted.");
////    }

////    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

////    /// <summary>
////    /// Fired by Unity's EventSystem when a dragged object is released over
////    /// any Raycast-Target Image on this GameObject.
////    /// Accepts soldiers only; ignores anything else.
////    /// </summary>
////    public void OnDrop(PointerEventData eventData)
////    {
////        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
////        if (soldier == null) return;

////        if (seat == null)
////        {
////            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
////            return;
////        }

////        PerformMount(soldier);
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Tells the mounted soldier's SpriteLayerAnimator to switch state.
////    /// Safe to call when no rider is present (null-checked).
////    /// </summary>
////    private void NotifySoldierAnimator(AnimationState riderState)
////    {
////        _riderAnimator?.SetState(riderState);
////    }

////    /// <summary>
////    /// Maps HorseState → the matching AnimationState for the soldier.
////    /// HorseRiderVisual.SetRiderState() and SpriteLayerAnimator.SetState()
////    /// both receive this mapped value so rider equipment sprites are selected
////    /// from the correct EquipmentItem horse arrays.
////    /// </summary>
////    private static AnimationState MapToRiderState(HorseState state) => state switch
////    {
////        HorseState.Idle => AnimationState.HorseIdle,
////        HorseState.Run => AnimationState.HorseRun,
////        HorseState.Fight => AnimationState.HorseFight,
////        HorseState.Dead => AnimationState.HorseDead,
////        _ => AnimationState.HorseIdle,
////    };
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — HorseController
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
/////  ANIMATION — TWO PATHS (auto-selected)
///// ════════════════════════════════════════════════════════════════════
/////
/////  PATH A  horseAnimSO assigned in Inspector
/////    → Uses HorseAnimationSO clips (Idle / Run / Fight / Dead).
/////
/////  PATH B  horseAnimSO left null  (backward-compatible)
/////    → Falls back to HorseData sprite arrays directly.
/////
///// ════════════════════════════════════════════════════════════════════
/////  RIDER RENDERING — SOLDIER'S OWN SPRITES
///// ════════════════════════════════════════════════════════════════════
/////
/////  SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the
/////  soldier's own SpriteLayerAnimator handles the mounted pose via
/////  the HorseIdle / HorseRun AnimationStates.
/////
/////  HorseRiderVisual (the 4 body-part Images on the seat) is NOT
/////  activated during mount — calling ShowRider while the soldier's
/////  own CanvasGroup is also visible (alpha = 1) caused a duplicate
/////  soldier to appear on the horse.
/////
/////  If you want to use HorseRiderVisual instead, change MountOnHorse
/////  in SoldierDragDrop to call HideOwnVisuals(), then re-enable the
/////  riderVisual?.ShowRider(equipment) line in PerformMount below.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class HorseController : MonoBehaviour, IDropHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Animation Data")]
//    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
//             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
//    [SerializeField] private HorseAnimationSO horseAnimSO;

//    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
//    [SerializeField] private HorseAnimationSO saddleAnimSO;

//    [Header("Image Layers")]
//    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
//    [SerializeField] private Image horseImage;

//    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
//    [SerializeField] private Image saddleImage;

//    [Header("Seat & Rider")]
//    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
//    [SerializeField] private HorseSeat seat;

//    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
//             "Auto-found in children if left empty.\n" +
//             "NOTE: Only used when soldier's own visuals are HIDDEN (HideOwnVisuals path).")]
//    [SerializeField] private HorseRiderVisual riderVisual;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseState _state = HorseState.Idle;

//    private float _horseTimer;
//    private float _saddleTimer;
//    private int _horseFrame;
//    private int _saddleFrame;
//    private int _dataCyclesCompleted;

//    private SoldierDragDrop _mountedSoldier;
//    private SpriteLayerAnimator _riderAnimator;
//    private HorseData _data;

//    // ── Public queries ────────────────────────────────────────────────────────

//    public HorseData Data => _data;
//    public HorseState CurrentState => _state;
//    public bool IsOccupied => seat != null && seat.IsOccupied;

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
//    }

//    private void Start()
//    {
//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

//        riderVisual?.HideRider();
//    }

//    private void Update()
//    {
//        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

//        if (saddleImage != null && saddleAnimSO != null)
//            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
//    }

//    // ── Animation Engine ──────────────────────────────────────────────────────

//    private void TickLayer(HorseAnimationSO so, Image img,
//                           ref int frame, ref float timer,
//                           bool isMainLayer)
//    {
//        if (img == null) return;

//        // PATH A: SO-driven
//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

//            timer += Time.deltaTime;
//            if (timer < 1f / clip.fps) return;
//            timer -= 1f / clip.fps;

//            if (clip.loop)
//                frame = (frame + 1) % clip.frames.Length;
//            else if (frame < clip.frames.Length - 1)
//                frame++;

//            img.sprite = clip.frames[frame];
//            return;
//        }

//        // PATH B: HorseData fallback (main layer only)
//        if (!isMainLayer || _data == null) return;

//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites == null || sprites.Length == 0) return;

//        float fps = _data.GetFPS(_state);
//        timer += Time.deltaTime;
//        if (timer < 1f / fps) return;
//        timer -= 1f / fps;

//        switch (_state)
//        {
//            case HorseState.Dead:
//                if (frame < sprites.Length - 1) frame++;
//                break;

//            case HorseState.Run:
//            case HorseState.Fight:
//                frame++;
//                if (frame >= sprites.Length)
//                {
//                    frame = 0;
//                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
//                    if (maxCycles > 0)
//                    {
//                        _dataCyclesCompleted++;
//                        if (_dataCyclesCompleted >= maxCycles)
//                            SetState(HorseState.Idle);
//                    }
//                }
//                break;

//            default:
//                frame = (frame + 1) % sprites.Length;
//                break;
//        }

//        if (frame < sprites.Length)
//            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
//    }

//    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
//                            bool isMainLayer = true)
//    {
//        if (img == null) return;
//        frame = 0;

//        if (so != null)
//        {
//            HorseClip clip = so.GetClip(_state);
//            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
//            img.sprite = clip.frames[0];
//            return;
//        }

//        if (!isMainLayer || _data == null) return;
//        Sprite[] sprites = _data.GetSprites(_state);
//        if (sprites != null && sprites.Length > 0)
//            img.sprite = sprites[0];
//    }

//    // ── Public API — State ────────────────────────────────────────────────────

//    public void SetState(HorseState newState)
//    {
//        _state = newState;

//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(newState);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] '{name}' → {newState}");
//    }

//    public void SetIdle() => SetState(HorseState.Idle);
//    public void SetRun() => SetState(HorseState.Run);
//    public void SetFight() => SetState(HorseState.Fight);
//    public void SetDead() => SetState(HorseState.Dead);

//    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

//    public void Setup(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Idle;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Idle);
//        riderVisual?.SetRiderState(riderState);

//        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
//    }

//    public void SetupWalk(HorseData data)
//    {
//        _data = data;
//        _state = HorseState.Run;
//        _horseFrame = _saddleFrame = 0;
//        _horseTimer = _saddleTimer = 0f;
//        _dataCyclesCompleted = 0;

//        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
//        if (saddleImage != null && saddleAnimSO != null)
//            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

//        AnimationState riderState = MapToRiderState(HorseState.Run);
//        riderVisual?.SetRiderState(riderState);
//        NotifySoldierAnimator(riderState);

//        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
//    }

//    // ── Public API — Mount / Dismount ─────────────────────────────────────────

//    /// <summary>
//    /// Accepts a soldier into the seat.
//    /// Called by OnDrop (IDropHandler) or externally.
//    ///
//    /// ── DUPLICATE FIX ─────────────────────────────────────────────────────────
//    /// SoldierDragDrop.MountOnHorse() calls ShowOwnVisuals() so the soldier's
//    /// own SpriteLayerAnimator (HorseIdle / HorseRun states) drives the visual.
//    ///
//    /// We deliberately do NOT call riderVisual?.ShowRider(equipment) here.
//    /// If both rendering paths are active simultaneously (soldier's CanvasGroup
//    /// alpha=1 AND HorseRiderVisual's 4 Images enabled), two overlapping soldier
//    /// visuals appear — the "duplicate soldier" bug.
//    ///
//    /// To switch to HorseRiderVisual rendering instead, change MountOnHorse in
//    /// SoldierDragDrop to call HideOwnVisuals(), then restore ShowRider below.
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

//        // MountSoldier → SoldierDragDrop.MountOnHorse (ShowOwnVisuals path).
//        // Soldier's own SpriteLayerAnimator drives the mounted pose.
//        seat.MountSoldier(soldier);

//        // ── DO NOT call riderVisual?.ShowRider(equipment) here ──────────────────
//        // Reason: MountOnHorse keeps the soldier's CanvasGroup at alpha = 1
//        // (ShowOwnVisuals). Enabling HorseRiderVisual at the same time causes a
//        // duplicate — two overlapping soldier visuals on the horse.
//        // SetState below will still call riderVisual.SetRiderState() which is
//        // harmless because HideRider was already called in Start().
//        // ────────────────────────────────────────────────────────────────────────

//        // SetState notifies the rider's SpriteLayerAnimator AND riderVisual
//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and resets the horse to Idle.
//    /// Wire this to a UI "Dismount" button or call from an external system.
//    /// </summary>
//    public void PerformDismount()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        // HideRider is safe to call even though ShowRider was never called
//        // (all 4 Images are already hidden from Start() / the previous HideRider call).
//        riderVisual?.HideRider();

//        seat.MountedSoldier.DismountFromHorse();
//        seat.ReleaseSoldier();

//        _mountedSoldier = null;
//        _riderAnimator = null;

//        SetState(HorseState.Idle);

//        Debug.Log($"[HorseController] '{name}': rider dismounted.");
//    }

//    // ── IDropHandler — drag soldier onto horse ────────────────────────────────

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

//    // ── Internal helpers ──────────────────────────────────────────────────────

//    private void NotifySoldierAnimator(AnimationState riderState)
//    {
//        _riderAnimator?.SetState(riderState);
//    }

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
/// AREA FORGE — HorseController  (mount / equipment fix)
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
///  MOUNT FLOW (fixed)
/// ════════════════════════════════════════════════════════════════════
///
///  1. PerformMount(soldier)
///       → HorseSeat.MountSoldier(soldier)          [position fix here]
///           → SetParent(SoldierSeat, false)
///           → anchoredPosition = seatOffset
///           → soldier.MountOnHorse(seat)
///       → soldier.HideOwnCanvasGroup()             [prevent duplicate]
///       → riderVisual.ShowRider(equipment)         [show Face/Helmet/Weapon/Armor]
///       → NotifySoldierAnimator(HorseIdle)         [drive equipment sprites]
///       → SetState(HorseState.Idle)
///
///  RENDERING PATH DECISION
///  ───────────────────────
///  We use HorseRiderVisual (the 4 Images on SoldierSeat) to draw the
///  rider's equipment, and hide the soldier's own CanvasGroup so only
///  one visual is visible. This avoids the "duplicate soldier" bug.
///
///  If you prefer the soldier's own SpriteLayerAnimator to drive
///  everything (and skip the 4 seat Images), reverse the two lines
///  flagged RENDERING_CHOICE below.
///
/// ════════════════════════════════════════════════════════════════════
///  BUG FIXES vs previous version
/// ════════════════════════════════════════════════════════════════════
///
///  FIX 1 — Soldier jumps to wrong position on drop
///    HorseSeat.MountSoldier now uses worldPositionStays:false so the
///    soldier's anchoredPosition is set explicitly instead of being
///    derived from its drag-release screen coordinate.
///
///  FIX 2 — Face / Helmet / Weapon / Armor not visible after mount
///    PerformMount now calls riderVisual.ShowRider(equipment) after
///    hiding the soldier's own CanvasGroup. The 4 seat Images are
///    populated from the soldier's CharacterEquipment and animated by
///    NotifySoldierAnimator(HorseIdle).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HorseController : MonoBehaviour, IDropHandler
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Animation Data")]
    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
    [SerializeField] private HorseAnimationSO horseAnimSO;

    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
    [SerializeField] private HorseAnimationSO saddleAnimSO;

    [Header("Image Layers")]
    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
    [SerializeField] private Image horseImage;

    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
    [SerializeField] private Image saddleImage;

    [Header("Seat & Rider")]
    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
    [SerializeField] private HorseSeat seat;

    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
             "Auto-found in children if left empty.\n" +
             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
    [SerializeField] private HorseRiderVisual riderVisual;

    // ── Private state ─────────────────────────────────────────────────────────

    private HorseState _state = HorseState.Idle;

    private float _horseTimer;
    private float _saddleTimer;
    private int _horseFrame;
    private int _saddleFrame;
    private int _dataCyclesCompleted;

    private SoldierDragDrop _mountedSoldier;
    private SpriteLayerAnimator _riderAnimator;
    private CanvasGroup _soldierCanvasGroup;   // ← NEW: for hide/show
    private HorseData _data;

    // ── Public queries ────────────────────────────────────────────────────────

    public HorseData Data => _data;
    public HorseState CurrentState => _state;
    public bool IsOccupied => seat != null && seat.IsOccupied;

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
                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
    }

    private void Start()
    {
        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        riderVisual?.HideRider();
    }

    private void Update()
    {
        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

        if (saddleImage != null && saddleAnimSO != null)
            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
    }

    // ── Animation Engine ──────────────────────────────────────────────────────

    private void TickLayer(HorseAnimationSO so, Image img,
                           ref int frame, ref float timer,
                           bool isMainLayer)
    {
        if (img == null) return;

        // PATH A: SO-driven
        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            timer += Time.deltaTime;
            if (timer < 1f / clip.fps) return;
            timer -= 1f / clip.fps;

            if (clip.loop)
                frame = (frame + 1) % clip.frames.Length;
            else if (frame < clip.frames.Length - 1)
                frame++;

            img.sprite = clip.frames[frame];
            return;
        }

        // PATH B: HorseData fallback (main layer only)
        if (!isMainLayer || _data == null) return;

        Sprite[] sprites = _data.GetSprites(_state);
        if (sprites == null || sprites.Length == 0) return;

        float fps = _data.GetFPS(_state);
        timer += Time.deltaTime;
        if (timer < 1f / fps) return;
        timer -= 1f / fps;

        switch (_state)
        {
            case HorseState.Dead:
                if (frame < sprites.Length - 1) frame++;
                break;

            case HorseState.Run:
            case HorseState.Fight:
                frame++;
                if (frame >= sprites.Length)
                {
                    frame = 0;
                    int maxCycles = _data.GetCyclesBeforeIdle(_state);
                    if (maxCycles > 0)
                    {
                        _dataCyclesCompleted++;
                        if (_dataCyclesCompleted >= maxCycles)
                            SetState(HorseState.Idle);
                    }
                }
                break;

            default:
                frame = (frame + 1) % sprites.Length;
                break;
        }

        if (frame < sprites.Length)
            img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
    }

    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
                            bool isMainLayer = true)
    {
        if (img == null) return;
        frame = 0;

        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            img.sprite = clip.frames[0];
            return;
        }

        if (!isMainLayer || _data == null) return;
        Sprite[] sprites = _data.GetSprites(_state);
        if (sprites != null && sprites.Length > 0)
            img.sprite = sprites[0];
    }

    // ── Public API — State ────────────────────────────────────────────────────

    public void SetState(HorseState newState)
    {
        _state = newState;

        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        AnimationState riderState = MapToRiderState(newState);
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] '{name}' → {newState}");
    }

    public void SetIdle() => SetState(HorseState.Idle);
    public void SetRun() => SetState(HorseState.Run);
    public void SetFight() => SetState(HorseState.Fight);
    public void SetDead() => SetState(HorseState.Dead);

    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

    public void Setup(HorseData data)
    {
        _data = data;
        _state = HorseState.Idle;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        AnimationState riderState = MapToRiderState(HorseState.Idle);
        riderVisual?.SetRiderState(riderState);

        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
    }

    public void SetupWalk(HorseData data)
    {
        _data = data;
        _state = HorseState.Run;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        AnimationState riderState = MapToRiderState(HorseState.Run);
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
    }

    // ── Public API — Eject before destroy ────────────────────────────────────

    /// <summary>
    /// Called by HorseDragHandler.OnEndDrag immediately before Destroy(gameObject).
    ///
    /// Problem: the soldier is reparented under SoldierSeat (a child of this horse)
    /// when mounted. If the horse is destroyed without ejecting the soldier first,
    /// Unity destroys the soldier along with it.
    ///
    /// This method safely returns the soldier to its pre-mount home and hides the
    /// rider visual so nothing is left dangling after the horse is removed.
    /// </summary>
    public void EjectRiderBeforeDestroy()
    {
        if (seat == null || !seat.IsOccupied) return;

        SoldierDragDrop soldier = seat.MountedSoldier;

        // Hide the rider visual — these Images belong to this horse and are
        // about to be destroyed, but clearing them prevents a one-frame flash.
        riderVisual?.HideRider();

        // Re-enable the soldier prefab (it was disabled on mount) so it is
        // visible and interactive when it arrives back at its spawn area.
        if (soldier != null)
            soldier.gameObject.SetActive(true);

        // Return the soldier to its ground home. Routed through SoldierDragDrop
        // so it correctly clears _currentHorseSeat and restores patrol state.
        soldier?.ReturnHomeFromDestroyedHorse();

        // Clear cached references — this horse is being destroyed after this call.
        seat.ReleaseSoldier();
        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider ejected before horse destroy.");
    }

    // ── Public API — Mount / Dismount ─────────────────────────────────────────

    /// <summary>
    /// Accepts a soldier into the seat.
    ///
    /// ── MOUNT FLOW (fixed) ────────────────────────────────────────────────────
    ///
    ///  Step 1  HorseSeat.MountSoldier(soldier)
    ///          → SetParent(SoldierSeat, worldPositionStays:false)   [FIX 1]
    ///          → anchoredPosition = seatOffset
    ///          → soldier.MountOnHorse(seat)
    ///
    ///  Step 2  Hide the soldier's own CanvasGroup (alpha = 0)
    ///          Prevents the "duplicate soldier" — the soldier's body is now
    ///          invisible; only the 4 seat Images (Face/Helmet/Weapon/Armor)
    ///          will show.                                            [FIX 2]
    ///
    ///  Step 3  riderVisual.ShowRider(equipment)
    ///          Populates Face / Helmet / Weapon / Armor Images from the
    ///          soldier's CharacterEquipment.                         [FIX 2]
    ///
    ///  Step 4  NotifySoldierAnimator(HorseIdle)
    ///          Tells the SpriteLayerAnimator to switch to HorseIdle so the
    ///          equipment sprites animate in the mounted pose.
    ///
    ///  ── RENDERING CHOICE NOTE ──────────────────────────────────────────────
    ///  This method uses HorseRiderVisual (4 seat Images) and hides the
    ///  soldier's own CanvasGroup. To switch to the "soldier's own visuals"
    ///  path instead:
    ///    • Comment out the HideOwnCanvasGroup line   (RENDERING_CHOICE A)
    ///    • Comment out the ShowRider line            (RENDERING_CHOICE B)
    ///    • Make sure soldier.MountOnHorse calls ShowOwnVisuals (alpha = 1)
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

        // Cache before reparenting
        _mountedSoldier = soldier;
        _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>();
        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
        var equipment = soldier.GetComponent<CharacterEquipment>();

        // ── Step 1: Reparent + position ───────────────────────────────────────
        // HorseSeat.MountSoldier uses worldPositionStays:false so the soldier
        // snaps to seatOffset instead of jumping to its drag-release position.
        seat.MountSoldier(soldier);

        // ── Step 2: Disable the soldier prefab ───────────────────────────────
        // SetActive(false) completely hides the soldier GameObject so only the
        // HorseRiderVisual seat Images (Face/Helmet/Weapon/Armor) are visible.
        // Must run AFTER seat.MountSoldier() (which reparents + positions the
        // soldier) but BEFORE ShowRider() so there is never a frame where both
        // the soldier and the rider-visual are visible simultaneously.
        _mountedSoldier.gameObject.SetActive(false);

        // ── Step 3: Populate the 4 seat Images (Face/Helmet/Weapon/Armor) ─────
        // ShowRider internally calls SetRiderStateInternal(HorseIdle) — do NOT
        // call SetRiderState again here or it will trigger a duplicate pass.
        // SetState below is the single authoritative state notification.
        riderVisual?.ShowRider(equipment);

        // ── Step 4: single authoritative state transition ─────────────────────
        // SetState notifies both riderVisual.SetRiderState and NotifySoldierAnimator
        // exactly once. Do not call either directly before this line.
        SetState(HorseState.Idle);

        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. " +
                  $"Equipment shown via HorseRiderVisual.");
    }

    /// <summary>
    /// Returns the soldier to the ground and resets the horse to Idle.
    /// </summary>
    public void PerformDismount()
    {
        if (seat == null || !seat.IsOccupied) return;

        // Hide the 4 seat Images
        riderVisual?.HideRider();

        // Re-enable the soldier prefab before DismountFromHorse() reparents it,
        // otherwise the soldier returns home as an invisible disabled GameObject.
        if (_mountedSoldier != null)
            _mountedSoldier.gameObject.SetActive(true);

        // Reparent the soldier back to its original parent + restore ground state
        seat.MountedSoldier.DismountFromHorse();
        seat.ReleaseSoldier();

        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        SetState(HorseState.Idle);

        Debug.Log($"[HorseController] '{name}': rider dismounted.");
    }

    // ── IDropHandler ──────────────────────────────────────────────────────────

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

    private void NotifySoldierAnimator(AnimationState riderState)
    {
        _riderAnimator?.SetState(riderState);
    }

    private static AnimationState MapToRiderState(HorseState state) => state switch
    {
        HorseState.Idle => AnimationState.HorseIdle,
        HorseState.Run => AnimationState.HorseRun,
        HorseState.Fight => AnimationState.HorseFight,
        HorseState.Dead => AnimationState.HorseDead,
        _ => AnimationState.HorseIdle,
    };
}