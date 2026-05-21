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

//    // ── Public API — Eject before destroy ────────────────────────────────────

//    /// <summary>
//    /// Called by HorseDragHandler.OnEndDrag immediately before Destroy(gameObject).
//    ///
//    /// Problem: the soldier is reparented under SoldierSeat (a child of this horse)
//    /// when mounted. If the horse is destroyed without ejecting the soldier first,
//    /// Unity destroys the soldier along with it.
//    ///
//    /// This method safely returns the soldier to its pre-mount home and hides the
//    /// rider visual so nothing is left dangling after the horse is removed.
//    /// </summary>
//    public void EjectRiderBeforeDestroy()
//    {
//        if (seat == null || !seat.IsOccupied) return;

//        SoldierDragDrop soldier = seat.MountedSoldier;

//        // Hide the rider visual — these Images belong to this horse and are
//        // about to be destroyed, but clearing them prevents a one-frame flash.
//        riderVisual?.HideRider();

//        // Re-enable the soldier prefab (it was disabled on mount) so it is
//        // visible and interactive when it arrives back at its spawn area.
//        if (soldier != null)
//            soldier.gameObject.SetActive(true);

//        // Return the soldier to its ground home. Routed through SoldierDragDrop
//        // so it correctly clears _currentHorseSeat and restores patrol state.
//        soldier?.ReturnHomeFromDestroyedHorse();

//        // Clear cached references — this horse is being destroyed after this call.
//        seat.ReleaseSoldier();
//        _mountedSoldier = null;
//        _riderAnimator = null;
//        _soldierCanvasGroup = null;

//        Debug.Log($"[HorseController] '{name}': rider ejected before horse destroy.");
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

//        // ── Step 2: Disable the soldier prefab ───────────────────────────────
//        // SetActive(false) completely hides the soldier GameObject so only the
//        // HorseRiderVisual seat Images (Face/Helmet/Weapon/Armor) are visible.
//        // Must run AFTER seat.MountSoldier() (which reparents + positions the
//        // soldier) but BEFORE ShowRider() so there is never a frame where both
//        // the soldier and the rider-visual are visible simultaneously.
//        _mountedSoldier.gameObject.SetActive(false);

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

//        // Re-enable the soldier prefab before DismountFromHorse() reparents it,
//        // otherwise the soldier returns home as an invisible disabled GameObject.
//        if (_mountedSoldier != null)
//            _mountedSoldier.gameObject.SetActive(true);

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

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — HorseController
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
///  MOUNT FLOW
/// ════════════════════════════════════════════════════════════════════
///
///  1. PerformMount(soldier)
///       → HorseSeat.MountSoldier(soldier)
///           → SetParent(SoldierSeat, worldPositionStays:false)
///           → anchoredPosition = seatOffset
///           → soldier.MountOnHorse(seat)
///       → soldier.SetActive(false)              [hide soldier's own visuals]
///       → _riderAnimator = null                 [FIX B — soldier is inactive]
///       → riderVisual.ShowRider(equipment)      [show Face/Helmet/Weapon/Armor]
///       — WalkCycleRoutine drives SetIdle/SetRun from here —
///
/// ════════════════════════════════════════════════════════════════════
///  SYNC BUG FIXES (three bugs, all fixed here + HorseRiderVisual.cs)
/// ════════════════════════════════════════════════════════════════════
///
///  BUG A — SetState(Idle) after ShowRider was a no-op
///    ShowRider internally calls SetRiderStateInternal(HorseIdle, force:true),
///    setting _state = HorseIdle on the visual.  The old PerformMount then called
///    SetState(HorseState.Idle) → riderVisual.SetRiderState(HorseIdle, force:false),
///    which hit the "if (_state == newState) return" guard and silently did nothing.
///    Later WalkCycleRoutine called SetRun → SetRiderState(HorseRun) — that worked —
///    but then SetIdle → SetRiderState(HorseIdle) hit the same guard again because
///    _state was still HorseIdle from ShowRider, freezing the Images permanently.
///
///    FIX A: HorseRiderVisual.SetRiderState() now always passes force:true so every
///    state change from WalkCycleRoutine fully resets frame counters and sprite arrays.
///    (See HorseRiderVisual.cs — one-line change in SetRiderState().)
///
///  BUG B — _riderAnimator pointed at a disabled GameObject
///    PerformMount cached _riderAnimator = soldier.GetComponent<SpriteLayerAnimator>()
///    then called soldier.SetActive(false).  Every subsequent NotifySoldierAnimator()
///    call (from SetState, SetupWalk, WalkCycleRoutine) fired on a disabled component —
///    Unity does not throw, it silently does nothing.  State was never actually applied.
///
///    FIX B: After SetActive(false), _riderAnimator is set to null.  HorseRiderVisual
///    drives all four Images autonomously; the soldier's own SpriteLayerAnimator is
///    irrelevant while the soldier is hidden.  On dismount, _riderAnimator is restored
///    before re-enabling the soldier so future needs still work.
///
///  BUG C — PerformMount called SetState AFTER ShowRider, creating a double-init
///    ShowRider set all Images to HorseIdle frame 0.  SetState then tried to do the
///    same again (no-op due to Bug A guard) AND called NotifySoldierAnimator (no-op
///    due to Bug B).  The redundant call is removed; ShowRider is the sole initialiser
///    on mount.  WalkCycleRoutine's SetIdle/SetRun calls are the only subsequent state
///    drivers, and they now work correctly thanks to Fixes A and B.
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
    private SpriteLayerAnimator _riderAnimator;   // null while soldier is SetActive(false)
    private CanvasGroup _soldierCanvasGroup;
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

        // Notify HorseRiderVisual — this is what drives the 4 equipment Images.
        // FIX A (in HorseRiderVisual): SetRiderState now always forces the state,
        // so this call correctly resets frame counters every time WalkCycleRoutine
        // switches between Idle and Run.
        AnimationState riderState = MapToRiderState(newState);
        riderVisual?.SetRiderState(riderState);

        // FIX B: _riderAnimator is null while the soldier is SetActive(false),
        // so this is a harmless no-op during that time. It only fires when the
        // soldier is active (e.g. after dismount, or if using the "own visuals" path).
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

        // No rider yet — SetRiderState is safe to call (HorseRiderVisual is hidden).
        riderVisual?.SetRiderState(MapToRiderState(HorseState.Idle));

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
        NotifySoldierAnimator(riderState);   // null-safe; no-op if soldier is inactive

        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
    }

    // ── Public API — Eject before destroy ────────────────────────────────────

    /// <summary>
    /// Called by HorseDragHandler.OnEndDrag immediately before Destroy(gameObject).
    /// Safely returns the mounted soldier to its pre-mount home so it is not
    /// destroyed along with the horse.
    /// </summary>
    public void EjectRiderBeforeDestroy()
    {
        if (seat == null || !seat.IsOccupied) return;

        SoldierDragDrop soldier = seat.MountedSoldier;

        // Hide rider Images before the horse is destroyed.
        riderVisual?.HideRider();

        // Re-enable the soldier so it is visible and interactive when it
        // arrives back at its spawn area.
        if (soldier != null)
            soldier.gameObject.SetActive(true);

        soldier?.ReturnHomeFromDestroyedHorse();

        seat.ReleaseSoldier();
        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider ejected before horse destroy.");
    }

    /// <summary>
    /// Extracts the mounted soldier so it can be re-mounted on a new horse instance
    /// (used by HorseWalkZone when the horse prefab is swapped).
    /// Returns the soldier, or null if the seat was empty.
    /// </summary>
    public SoldierDragDrop ExtractRiderForTransfer()
    {
        if (seat == null || !seat.IsOccupied) return null;

        SoldierDragDrop soldier = seat.MountedSoldier;

        riderVisual?.HideRider();

        // Re-enable so PerformMount on the new horse can immediately use it.
        if (soldier != null)
            soldier.gameObject.SetActive(true);

        seat.ReleaseSoldier();
        soldier?.ClearHorseSeatForTransfer();

        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider '{soldier?.name}' extracted for transfer.");
        return soldier;
    }

    public void CounterFlipSeat()
    {
        if (seat == null) return;
        Vector3 s = seat.transform.localScale;
        s.x = -s.x;
        seat.transform.localScale = s;
        Debug.Log($"[HorseController] '{name}': SoldierSeat counter-flipped (x={s.x:F2}).");
    }

    // ── Public API — Mount / Dismount ─────────────────────────────────────────

    /// <summary>
    /// Accepts a soldier into the seat.
    ///
    /// Works identically whether the horse is in a HorseSlot or a HorseWalkZone:
    ///
    ///  Step 1  seat.MountSoldier(soldier)
    ///          → SetParent(SoldierSeat, worldPositionStays:false)
    ///          → anchoredPosition = seatOffset
    ///          → soldier.MountOnHorse(seat)
    ///
    ///  Step 2  soldier.SetActive(false)
    ///          Hides the soldier's own GameObject completely.
    ///          This prevents the soldier from appearing at an unexpected world
    ///          position (SoldierSeat's offset) and prevents it from being
    ///          independently dragged while mounted.
    ///
    ///  Step 3  riderVisual.ShowRider(equipment)
    ///          Populates the Face/Armor/Helmet/Weapon Images on SoldierSeat
    ///          from the soldier's CharacterEquipment and sets them to HorseIdle
    ///          frame 0. Because these Images are children of the horse prefab
    ///          they render at the horse's own position — correct in both the
    ///          slot and the walk zone.
    ///          WalkCycleRoutine drives all subsequent state changes via SetIdle/SetRun.
    ///
    ///  _riderAnimator is nulled out because calling SetState on a component
    ///  attached to a disabled GameObject silently does nothing (Bug B fix).
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
        _soldierCanvasGroup = soldier.GetComponent<CanvasGroup>();
        var equipment = soldier.GetComponent<CharacterEquipment>();

        // ── Step 1: Call seat.MountSoldier so seat state is correct ──────────
        // (sets seat.IsOccupied, seat.MountedSoldier, and calls soldier.MountOnHorse)
        // MountOnHorse can reparent the soldier to the wrong location and/or call
        // SetActive(false). We fix both immediately below.
        seat.MountSoldier(soldier);

        // ── Step 2: Force correct parent — HorsePrefab → SoldierSeat → Soldier
        // MountOnHorse may have placed the soldier in the wrong part of the hierarchy.
        // This re-parents it explicitly to seat.transform so the final hierarchy is:
        //   HorsePrefab
        //     └── SoldierSeat   (seat.transform)
        //           └── SoldierPrefab   ← soldier lands here
        var soldierRT = soldier.GetComponent<RectTransform>();
        if (soldierRT != null)
        {
            soldierRT.SetParent(seat.transform, false);
            soldierRT.anchoredPosition = Vector2.zero;
            soldierRT.localScale = Vector3.one;
        }
        else
        {
            soldier.transform.SetParent(seat.transform, false);
            soldier.transform.localPosition = Vector3.zero;
            soldier.transform.localScale = Vector3.one;
        }

        // ── Step 3: Hide soldier's own visuals, show rider Images ────────────
        // The soldier's own CanvasGroup is hidden (alpha = 0) so its sprites do
        // not render on top of the horse. HorseRiderVisual drives the 4 seat
        // Images (Face/Armor/Helmet/Weapon) from the soldier's CharacterEquipment
        // instead — they are children of the horse prefab so they always sit at
        // the correct position whether the horse is in a slot or the walk zone.
        //
        // Order matters:
        //   ShowRider first  — populates and enables the 4 seat Images.
        //   Hide after       — alpha=0 runs last so it is never overridden by
        //                      ShowRider's internal SetRiderStateInternal call.
        //
        // FIX B: _riderAnimator is nulled because the soldier's SpriteLayerAnimator
        // is on a CanvasGroup-hidden object. SetState on a hidden (but still active)
        // component would silently fight HorseRiderVisual. HorseRiderVisual drives
        // all animation autonomously; the soldier's own animator is irrelevant here.
        riderVisual?.ShowRider(equipment);

        if (_soldierCanvasGroup != null)
        {
            _soldierCanvasGroup.alpha = 0f;
            _soldierCanvasGroup.blocksRaycasts = true;
            _soldierCanvasGroup.interactable = true;
        }

        _riderAnimator = null;   // FIX B — do not call SetState on a hidden soldier

        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted.");
    }

    /// <summary>
    /// Returns the soldier to the ground and resets the horse to Idle.
    /// </summary>
    public void PerformDismount()
    {
        if (seat == null || !seat.IsOccupied) return;

        // Hide the 4 seat Images — soldier's own visuals take over from here.
        riderVisual?.HideRider();

        // Re-enable soldier before DismountFromHorse() reparents it so it
        // returns home as a visible, interactive GameObject.
        if (_mountedSoldier != null)
        {
            _riderAnimator = _mountedSoldier.GetComponent<SpriteLayerAnimator>();
            _mountedSoldier.gameObject.SetActive(true);
        }

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

    /// <summary>
    /// Null-safe; silently does nothing while soldier is SetActive(false) (FIX B).
    /// </summary>
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