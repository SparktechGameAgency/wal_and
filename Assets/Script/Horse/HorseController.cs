using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


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

        // If a soldier is already mounted (e.g. SetupWalk called AFTER PerformMount
        // during a walk-zone re-setup), keep the soldier deactivated and refresh the
        // rider visual so the seat Images stay enabled and animate correctly.
        // This covers the case where HorseWalkZone calls PerformMount → SetupWalk.
        if (_mountedSoldier != null)
        {
            _mountedSoldier.gameObject.SetActive(false);   // ensure soldier stays hidden
            _riderAnimator = null;                         // FIX B — keep null while inactive
            var equipment = _mountedSoldier.GetComponent<CharacterEquipment>();
            riderVisual?.ShowRider(equipment);             // re-populate seat Images
        }

        // Drive the rider visual to Run (works whether ShowRider just ran above
        // or was called earlier inside PerformMount — force:true bypasses the guard).
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);   // null-safe; no-op while soldier is inactive

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
    ///
    /// ── TRANSFER RULE ────────────────────────────────────────────────────────
    /// The soldier is kept SetActive(false) for the entire transfer window.
    /// Do NOT call SetActive(true) here — that is what causes the "detached
    /// soldier" bug where the soldier prefab pops into view as a scene-root
    /// sibling while the new horse is being set up.
    ///
    /// ClearHorseSeatForTransfer() clears the soldier's internal seat reference
    /// so it does not point at the now-released seat, but it must NOT re-parent
    /// or re-enable the soldier. PerformMount on the new horse handles both:
    ///   • forcibly re-parents the soldier under the new SoldierSeat
    ///   • keeps it SetActive(false)
    ///   • calls ShowRider to make the seat Images visible instead
    /// </summary>
    public SoldierDragDrop ExtractRiderForTransfer()
    {
        if (seat == null || !seat.IsOccupied) return null;

        SoldierDragDrop soldier = seat.MountedSoldier;

        // Hide the 4 seat Images on this horse — the new horse's ShowRider will
        // re-populate them after PerformMount.
        riderVisual?.HideRider();

        // Keep soldier INACTIVE during the transfer.
        // The old code called SetActive(true) here so PerformMount could "see" it,
        // but PerformMount works fine on an inactive GameObject (it just calls
        // methods and reparents the transform — neither requires the object to be
        // active). Leaving it inactive prevents the soldier from flashing on screen
        // as a detached scene-root prefab between the two PerformMount calls.
        // soldier.gameObject.SetActive(true);  ← intentionally removed

        seat.ReleaseSoldier();

        // ClearHorseSeatForTransfer clears the soldier's internal _currentHorseSeat
        // reference so it no longer points at the seat we just released.
        // IMPORTANT: the implementation of ClearHorseSeatForTransfer must NOT
        // reparent the soldier back to its spawn-area parent or call SetActive.
        // Only the seat reference should be cleared here.
        soldier?.ClearHorseSeatForTransfer();

        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider '{soldier?.name}' extracted for transfer (stays inactive).");
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
        var equipment = soldier.GetComponent<CharacterEquipment>();

        // ── Step 1: Temporarily activate so seat + components can initialise ──
        // seat.MountSoldier calls soldier.MountOnHorse which may require the
        // soldier to be active (some Unity API calls are no-ops on inactive objects).
        // We immediately deactivate again in Step 3 — this window is sub-frame.
        bool wasActive = soldier.gameObject.activeSelf;
        if (!wasActive) soldier.gameObject.SetActive(true);

        // ── Step 2: Register soldier with seat ───────────────────────────────
        // Sets seat.IsOccupied, seat.MountedSoldier, and calls soldier.MountOnHorse.
        seat.MountSoldier(soldier);

        // ── Step 3: Force correct parent — HorsePrefab → SoldierSeat → Soldier
        // Regardless of where the soldier was in the hierarchy (scene root after a
        // walk-zone transfer, original spawn area after ClearHorseSeatForTransfer,
        // etc.), this reparents it explicitly under seat.transform so the final
        // hierarchy is always:
        //   HorsePrefab
        //     └── SoldierSeat   (seat.transform)
        //           └── SoldierPrefab   ← soldier always lands here
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

        // ── Step 4: Deactivate soldier prefab, show rider Images ─────────────
        // SetActive(false) completely hides the soldier GameObject so only the
        // HorseRiderVisual seat Images (Face/Armor/Helmet/Weapon) are visible.
        // Must run AFTER seat.MountSoldier() (which reparents + positions the
        // soldier) so the soldier is in the correct hierarchy before being hidden.
        //
        // FIX B: _riderAnimator is nulled because the soldier's SpriteLayerAnimator
        // is on a now-inactive GameObject. Calling SetState on it would silently
        // do nothing and fight HorseRiderVisual. HorseRiderVisual drives all four
        // Images autonomously while the soldier is inactive.
        _mountedSoldier.gameObject.SetActive(false);

        _riderAnimator = null;   // FIX B — soldier is inactive; null to avoid no-op calls

        riderVisual?.ShowRider(equipment);

        // Sync rider animation to the horse's current state.
        // ShowRider always initialises seat Images to HorseIdle frame 0.
        // In a HorseWalkZone drop, SetupWalk has already set _state = Run before
        // PerformMount is called, so this corrects the rider to HorseRun immediately,
        // keeping it in sync with the horse body animation from the first frame.
        // In a normal HorseSlot drop, _state is still Idle — harmless no-op.
        riderVisual?.SetRiderState(MapToRiderState(_state));

        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. Rider state → {_state}.");
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