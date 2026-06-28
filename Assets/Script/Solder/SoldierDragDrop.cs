////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Dragon Mount Settings")]
////    [Tooltip("Maps each armor to its default helmet.\n" +
////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
////    [SerializeField] private ArmorHelmetTable helmetTable;

////    // ── Component References ──────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;    // optional — patrol + flip
////    private CharacterEquipment _equipment;    // tracks equipped items
////    private SpriteLayerAnimator _animator;    // drives per-layer animation
////    private CharacterVisuals _visuals;        // direct layer image access

////    // Snapshot of which equipment slots were occupied BEFORE dragon mount
////    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
////    // slot not in this set is unequipped so the soldier returns with exactly
////    // the outfit it had before mounting.
////    private HashSet<EquipmentSlot> _preMountEquippedSlots;

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
////    private Transform _dragOriginalParent; // parent before drag reparent
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;

////    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
////    // Start() and never updated by patrol. SnapBack() uses this so the soldier
////    // always returns to the same spot regardless of where it was patrolling
////    // when the player grabbed it.
////    private Vector2 _spawnAnchoredPosition;
////    private bool _isDragging;

////    // ── Dragon Rider State ────────────────────────────────────────────────────

////    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
////    private DragonRiderSeat _currentSeat;

////    /// <summary>
////    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
////    /// return the soldier to its patrol area, not back to the seat.
////    /// </summary>
////    private Transform _mountHomeParent;
////    private Vector2 _mountHomePos;

////    // ── Lock State ────────────────────────────────────────────────────────────

////    private bool _isLocked = false;

////    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
////    public bool IsRiding => _currentSeat != null;

////    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
////    public bool IsLocked => _isLocked;

////    // ── Cannon Slot State ─────────────────────────────────────────────────────

////    /// <summary>
////    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
////    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
////    /// </summary>
////    public static SoldierDragDrop CurrentlyDragging { get; private set; }

////    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
////    private CannonSlot _currentCannonSlot;

////    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
////    public bool IsAtCannon => _currentCannonSlot != null;

////    // ── Horse Rider State ─────────────────────────────────────────────────────

////    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
////    private HorseSeat _currentHorseSeat;

////    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
////    private Transform _mountHorseHomeParent;

////    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
////    private Vector2 _mountHorseHomePos;

////    /// <summary>True while this soldier is seated on a horse.</summary>
////    public bool IsOnHorse => _currentHorseSeat != null;

////    // ── Archer Zone State ─────────────────────────────────────────────────────

////    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
////    private ArcherZoneCastle _currentArcherZone;

////    /// <summary>True while this soldier is assigned to an archer zone.</summary>
////    public bool IsArcher => _currentArcherZone != null;

////    // ══════════════════════════════════════════════════════════════════════════
////    // LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////        _rect = GetComponent<RectTransform>();
////        _controller = GetComponent<SoldierController>();   // optional
////        _equipment = GetComponent<CharacterEquipment>();
////        _animator = GetComponent<SpriteLayerAnimator>();
////        _visuals = GetComponent<CharacterVisuals>();

////        if (_equipment == null)
////            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
////                             $"'{name}'. Helmet auto-equip will be skipped.", this);
////        if (_animator == null)
////            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
////                             $"'{name}'. Riding animation will not play.", this);
////        if (helmetTable == null)
////            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
////                             $"'{name}'. Soldiers will mount without a helmet.", this);
////    }

////    private void Start()
////    {
////        RecordHome();
////        // Capture spawn position once; SnapBack() uses this fixed value.
////        _spawnAnchoredPosition = _rect.anchoredPosition;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — BEGIN
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // Locked to the dragon — drag disabled until Attach is toggled off.
////        if (_isLocked) return;

////        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
////        CurrentlyDragging = this;

////        // ── Release cannon slot if stationed there ────────────────────────────
////        if (_currentCannonSlot != null)
////        {
////            _currentCannonSlot.ReleaseSoldier(notify: false);
////            _currentCannonSlot = null;
////        }

////        // ── Capture mounted dragon before clearing the seat ───────────────────
////        bool wasMounted = _currentSeat != null;
////        DragonController mountedDragonDC = null;

////        if (wasMounted)
////        {
////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // ── Release horse seat if stationed there ─────────────────────────────
////        bool wasOnHorse = _currentHorseSeat != null;
////        HorseController mountedHorseHC = null;

////        if (wasOnHorse)
////        {
////            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
////        _rootCanvas = GetComponentInParent<Canvas>();

////        if (!wasMounted && !wasOnHorse)
////            _spawnAnchoredPosition = _rect.anchoredPosition;
////        RecordHome();

////        if (wasMounted && _mountHomeParent != null)
////        {
////            _homeParent = _mountHomeParent;
////            _homeAnchoredPosition = _mountHomePos;
////            _mountHomeParent = null;
////        }

////        if (wasOnHorse && _mountHorseHomeParent != null)
////        {
////            _homeParent = _mountHorseHomeParent;
////            _homeAnchoredPosition = _mountHorseHomePos;
////            _mountHorseHomeParent = null;
////        }

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // Restore own visuals before reparenting (in case coming from dragon or horse).
////        if (wasMounted || wasOnHorse)
////        {
////            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
////            ShowOwnVisuals();
////        }

////        // Store sibling order so SnapBack can restore it.
////        _dragOriginalParent = transform.parent;
////        _preDragSiblingIndex = transform.GetSiblingIndex();

////        // Lift to top of siblings so it renders above panel contents.
////        // No reparent -- soldier stays inside VillageSoldierSlot.
////        transform.SetAsLastSibling();

////        _canvasGroup.alpha = 0.75f;
////        _canvasGroup.blocksRaycasts = false;

////        // Notify dragon to hide its rider visual.
////        if (wasMounted)
////            mountedDragonDC?.PerformDismount();

////        // Notify horse to hide rider layers and reset to Idle.
////        if (wasOnHorse)
////            mountedHorseHC?.PerformDismount();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;
////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — END
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        _isDragging = false;

////        // ── HORSE GUARD ───────────────────────────────────────────────────────
////        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
////        // already mounted this soldier. If we let OnEndDrag continue it would:
////        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
////        //   • Find no free horse (seat is now occupied) → targetHorse = null
////        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
////        //     with standing idle animation (the "ghost copy" bug).
////        // Early-exit here prevents all of that.
////        if (_currentHorseSeat != null)
////        {
////            CurrentlyDragging = null;
////            return;
////        }
////        // ─────────────────────────────────────────────────────────────────────

////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
////        // CanvasGroup does not shadow targets sitting underneath.

////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        DragonRiderSeat targetSeat = null;
////        DragonController targetDC = null;
////        CannonSlot targetCannon = null;
////        HorseController targetHorse = null;

////        foreach (var r in results)
////        {
////            // ── Check for cannon slot ─────────────────────────────────────────
////            if (targetCannon == null)
////                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

////            // ── Check for dragon ──────────────────────────────────────────────
////            if (targetDC == null)
////            {
////                var dragon = r.gameObject.GetComponentInParent<DragonController>();
////                if (dragon != null)
////                {
////                    targetDC = dragon;
////                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
////                }
////            }

////            // ── Check for horse ───────────────────────────────────────────────
////            if (targetHorse == null)
////            {
////                var hc = r.gameObject.GetComponentInParent<HorseController>();
////                if (hc != null && !hc.IsOccupied)
////                    targetHorse = hc;
////            }

////            if (targetCannon != null && targetDC != null && targetHorse != null) break;
////        }

////        _canvasGroup.blocksRaycasts = true;

////        // ── Clear the static drag reference ───────────────────────────────────
////        CurrentlyDragging = null;

////        // ── Cannon slot drop ──────────────────────────────────────────────────
////        if (targetCannon != null)
////        {
////            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
////            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
////            ShowOwnVisuals();
////            _controller?.SetPatrolling(false);
////            return;
////        }

////        // ── Horse drop ────────────────────────────────────────────────────────
////        // Alpha is intentionally NOT restored to 1 before this call.
////        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
////        // If we set alpha=1 first, the soldier flashes visible for one frame
////        // and if the IDropHandler path already ran, we get the duplicate visual.
////        if (targetHorse != null)
////        {
////            _mountHorseHomeParent = _homeParent;
////            _mountHorseHomePos = _homeAnchoredPosition;
////            targetHorse.PerformMount(this);
////            return;
////        }

////        // ── Dragon drop ───────────────────────────────────────────────────────
////        // Alpha is NOT restored before mount — DragonController.PerformMount
////        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
////        // guaranteed to be hidden after all animator calls complete.
////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && targetSeat != null && seatFree)
////        {
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;
////            targetDC.PerformMount(this, targetSeat);
////            return;
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

////            if (currentRider != null && currentRider._isLocked)
////            {
////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
////                _canvasGroup.alpha = 1f;
////                SnapBack();
////            }
////            else if (currentRider != null)
////            {
////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
////                _mountHomeParent = _homeParent;
////                _mountHomePos = _homeAnchoredPosition;
////                currentRider.DismountFromDragon();
////                targetDC.PerformMount(this, targetSeat);
////                return;
////            }
////            else
////            {
////                _canvasGroup.alpha = 1f;
////                SnapBack();
////            }
////        }
////        else
////        {
////            // No valid drop target — restore alpha and snap back.
////            _canvasGroup.alpha = 1f;
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DROP OUTCOMES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////    public void SnapBack()
////    {
////        if (_homeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
////            ShowOwnVisuals();
////            _controller?.SetPatrolling(true);
////            return;
////        }

////        // worldPositionStays: true preserves the world position on reparent,
////        // then we apply the fixed spawn anchor to land at the correct slot position.
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _spawnAnchoredPosition;
////        // Restore the original sibling order so rendering order is unchanged.
////        if (_dragOriginalParent == _homeParent)
////            transform.SetSiblingIndex(_preDragSiblingIndex);
////        ShowOwnVisuals();
////        _controller?.SetPatrolling(true);
////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by a drop target after accepting the soldier.
////    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;
////        _controller?.SetPatrolling(false);
////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////    }

////    /// <summary>
////    /// Re-parents the soldier to spawnParent and resumes patrol.
////    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
////    /// </summary>
////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
////    {
////        if (spawnParent == null)
////        {
////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
////            return;
////        }

////        transform.SetParent(spawnParent, worldPositionStays: true);

////        if (spawnPosition.HasValue)
////            _rect.anchoredPosition = spawnPosition.Value;

////        _canvasGroup.blocksRaycasts = true;
////        _isDragging = false;

////        ShowOwnVisuals();
////        RecordHome();
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // CANNON SLOT MOUNT / RELEASE
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
////    /// cannon's SoldierSpawnpoint and records the new home.
////    ///
////    /// Safe to call whether the soldier was at a dragon, another cannon slot,
////    /// or the ground spawn area.
////    /// </summary>
////    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
////    {
////        if (slot == null || spawnpoint == null) return;

////        // Release any existing dragon seat first
////        if (_currentSeat != null)
////        {
////            var dc = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            dc?.PerformDismount();
////        }

////        // Release any existing horse seat first
////        if (_currentHorseSeat != null)
////        {
////            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////            hc?.PerformDismount();
////        }

////        // Release previous cannon slot without notifying (we're already moving)
////        if (_currentCannonSlot != null && _currentCannonSlot != slot)
////            _currentCannonSlot.ReleaseSoldier(notify: false);

////        _currentCannonSlot = slot;

////        // Reparent to the cannon's SoldierSpawnpoint
////        transform.SetParent(spawnpoint, worldPositionStays: false);
////        _rect.anchoredPosition = Vector2.zero;
////        _rect.localScale = Vector3.one;

////        // Record this position as home so SnapBack() returns here
////        RecordHome();
////        _spawnAnchoredPosition = _rect.anchoredPosition;

////        // Restore visuals and stop patrol
////        ShowOwnVisuals();
////        _animator?.SetState(AnimationState.Idle);
////        _controller?.ExitRidingState();

////        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
////    }

////    /// <summary>
////    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
////    /// Snaps the soldier back to their original home position.
////    /// </summary>
////    public void RemoveFromCannonSlot()
////    {
////        if (_currentCannonSlot == null) return;
////        _currentCannonSlot = null;
////        SnapBack();
////        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
////    /// </summary>
////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
////    {
////        _currentSeat = seat;

////        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
////        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
////        SaveVisualLayerSnapshot();

////        EnsureHelmetEquipped();

////        _controller?.EnterRidingState();

////        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
////        // the Rider GameObject, inserted directly after it in the hierarchy.
////        Transform dragonRoot = seat.transform.parent != null
////            ? seat.transform.parent
////            : seat.transform;

////        transform.SetParent(dragonRoot, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;

////        // Place the soldier right after the Rider child in sibling order.
////        // Find the Rider by name; fall back to SetAsLastSibling if not found.
////        int riderIndex = -1;
////        for (int i = 0; i < dragonRoot.childCount; i++)
////        {
////            if (dragonRoot.GetChild(i).name == "Rider")
////            {
////                riderIndex = i;
////                break;
////            }
////        }

////        if (riderIndex >= 0)
////            transform.SetSiblingIndex(riderIndex + 1);
////        else
////            transform.SetAsLastSibling();

////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the soldier visible on top of the dragon.)
////        _animator?.SetState(AnimationState.RiderIdle);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
////                  $"at sibling index {transform.GetSiblingIndex()}.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
////    /// </summary>
////    public void DismountFromDragon()
////    {
////        DragonController riderDragonDC = null;
////        if (_currentSeat != null)
////        {
////            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////        }

////        if (_mountHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
////                             "recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            RestoreVisualLayerSnapshot();
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        gameObject.SetActive(true);
////        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
////        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
////        // already calls StartWalking() which sets AnimationState.Walk on the
////        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
////        // to standing still instead of resuming patrol walking.
////        RestoreVisualLayerSnapshot();
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHomeParent = null;

////        riderDragonDC?.PerformDismount();

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // ATTACH LOCK
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Locks or unlocks the soldier to the current dragon seat.
////    /// Called by DragonAttachButton.
////    /// </summary>
////    public void SetLocked(bool locked)
////    {
////        if (_currentSeat == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
////            return;
////        }

////        _isLocked = locked;

////        if (locked)
////        {
////            _canvasGroup.blocksRaycasts = false;
////            _canvasGroup.interactable = false;
////        }
////        else
////        {
////            _canvasGroup.blocksRaycasts = true;
////            _canvasGroup.interactable = true;
////        }

////        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
////                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // VISUAL SHOW / HIDE
////    // ══════════════════════════════════════════════════════════════════════════

////    // ══════════════════════════════════════════════════════════════════════════
////    // LAYER SNAPSHOT (dragon mount / dismount)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Records the enabled/disabled state of every visual layer at the moment
////    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
////    /// so the soldier returns with exactly the outfit it had before mounting.
////    /// </summary>
////    private void SaveVisualLayerSnapshot()
////    {
////        if (_equipment == null) return;

////        // Record exactly which slots have an item equipped right now.
////        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
////        // helmet/armor will be absent from the set and removed on dismount.
////        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
////        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
////        {
////            if (_equipment.GetEquipped(slot) != null)
////                _preMountEquippedSlots.Add(slot);
////        }

////        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
////                  + string.Join(", ", _preMountEquippedSlots));
////    }

////    /// <summary>
////    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
////    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
////    /// </summary>
////    private void RestoreVisualLayerSnapshot()
////    {
////        if (_equipment == null || _preMountEquippedSlots == null) return;

////        // Unequip any slot that was NOT equipped before mounting.
////        // This removes items auto-added by EnsureHelmetEquipped() so the
////        // soldier returns with exactly the outfit it had before the drag.
////        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
////        {
////            if (!_preMountEquippedSlots.Contains(slot)
////                && _equipment.GetEquipped(slot) != null)
////            {
////                _equipment.Unequip(slot);
////                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
////            }
////        }

////        _preMountEquippedSlots = null;
////        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
////    }

////    public void HideOwnVisuals()
////    {
////        _canvasGroup.alpha = 0f;
////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.interactable = true;
////    }

////    private void ShowOwnVisuals()
////    {
////        _canvasGroup.alpha = 1f;
////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.interactable = true;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELMET AUTO-EQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnsureHelmetEquipped()
////    {
////        if (_equipment == null) return;
////        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

////        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
////        EquipmentItem helmet = helmetTable != null
////            ? helmetTable.GetDefaultHelmet(armor)
////            : null;

////        if (helmet != null)
////        {
////            _equipment.Equip(helmet);
////            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
////                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
////        }
////        else
////        {
////            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
////                             $"armor '{armor?.itemName ?? "none"}'. " +
////                             "Set fallbackHelmet in ArmorHelmetTable.", this);
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPER
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RecordHome()
////    {
////        _homeParent = transform.parent;
////        _homeAnchoredPosition = _rect.anchoredPosition;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
////    ///
////    /// The soldier is reparented under the seat so it moves with the horse.
////    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
////    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
////    ///
////    /// FIX: EnterRidingState() is intentionally NOT called here.
////    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
////    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
////    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
////    /// If the soldier's CharacterVisuals image fields are wired to the same Image
////    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
////    /// those images get disabled right before HorseController.PerformMount calls
////    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
////    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
////    /// so no animator state change is needed.
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();

////        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
////        _controller?.SetPatrolling(false);

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        // Guard: only proceed if this soldier was actually on a horse.
////        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
////        // was called before this (e.g. during a slot→walkzone transfer).
////        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
////        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
////        if (!wasMounted) return;

////        // Clear the seat reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.SetPatrolling(true);
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _animator?.SetState(AnimationState.Idle);
////        _controller?.SetPatrolling(true);
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHorseHomeParent = null;

////        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and restores its own visuals.
////    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
////    /// </summary>
////    public void DismountFromHorse()
////    {
////        if (_currentHorseSeat != null)
////        {
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////        }

////        if (_mountHorseHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
////                             "— snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.SetPatrolling(true);
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _animator?.SetState(AnimationState.Idle);
////        _controller?.SetPatrolling(true);
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHorseHomeParent = null;

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
////    }

////    public void BecomeArcher(ArcherZoneCastle zone)
////    {
////        if (zone == null) return;

////        _currentArcherZone = zone;
////        _isLocked = true;

////        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
////        gameObject.SetActive(false);

////        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
////    }

////    /// <summary>
////    /// Called by ArcherZoneCastle.RemoveArcher().
////    /// Re-enables this soldier and snaps them back to their walk zone.
////    /// </summary>
////    public void ReturnFromArcher()
////    {
////        _currentArcherZone = null;
////        _isLocked = false;

////        // Re-show the soldier.
////        gameObject.SetActive(true);

////        // Snap back to the home position recorded before the last drag.
////        if (_homeParent != null)
////        {
////            transform.SetParent(_homeParent, worldPositionStays: false);
////            RectTransform rt = GetComponent<RectTransform>();
////            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
////        }

////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
////    }

////    public void ClearHorseSeatForTransfer()
////    {
////        // Null out the seat so IsOnHorse returns false.
////        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
////        // Do NOT call ExitRidingState — the soldier stays in riding state
////        // so EnterRidingState / SetActive(false) in the next PerformMount
////        // runs cleanly without a redundant coroutine restart.
////        _currentHorseSeat = null;
////        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
////    }
////}


//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
//    [SerializeField] private ArmorHelmetTable helmetTable;

//    // ── Component References ──────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;    // optional — patrol + flip
//    private CharacterEquipment _equipment;    // tracks equipped items
//    private SpriteLayerAnimator _animator;    // drives per-layer animation
//    private CharacterVisuals _visuals;        // direct layer image access

//    // Snapshot of which equipment slots were occupied BEFORE dragon mount
//    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
//    // slot not in this set is unequipped so the soldier returns with exactly
//    // the outfit it had before mounting.
//    private HashSet<EquipmentSlot> _preMountEquippedSlots;

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
//    private Transform _dragOriginalParent; // parent before drag reparent
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;

//    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
//    // Start() and never updated by patrol. SnapBack() uses this so the soldier
//    // always returns to the same spot regardless of where it was patrolling
//    // when the player grabbed it.
//    private Vector2 _spawnAnchoredPosition;
//    private bool _isDragging;

//    // ── Dragon Rider State ────────────────────────────────────────────────────

//    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
//    private DragonRiderSeat _currentSeat;

//    /// <summary>
//    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
//    /// return the soldier to its patrol area, not back to the seat.
//    /// </summary>
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ── Lock State ────────────────────────────────────────────────────────────

//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

//    // ── Cannon Slot State ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//    /// </summary>
//    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//    private CannonSlot _currentCannonSlot;

//    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//    public bool IsAtCannon => _currentCannonSlot != null;

//    // ── Horse Rider State ─────────────────────────────────────────────────────

//    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
//    private HorseSeat _currentHorseSeat;

//    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Transform _mountHorseHomeParent;

//    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Vector2 _mountHorseHomePos;

//    /// <summary>True while this soldier is seated on a horse.</summary>
//    public bool IsOnHorse => _currentHorseSeat != null;

//    // ── Archer Zone State ─────────────────────────────────────────────────────

//    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
//    private ArcherZoneCastle _currentArcherZone;

//    /// <summary>True while this soldier is assigned to an archer zone.</summary>
//    public bool IsArcher => _currentArcherZone != null;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();   // optional
//        _equipment = GetComponent<CharacterEquipment>();
//        _animator = GetComponent<SpriteLayerAnimator>();
//        _visuals = GetComponent<CharacterVisuals>();

//        if (_equipment == null)
//            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//        if (_animator == null)
//            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//                             $"'{name}'. Riding animation will not play.", this);
//        if (helmetTable == null)
//            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//                             $"'{name}'. Soldiers will mount without a helmet.", this);
//    }

//    private void Start()
//    {
//        RecordHome();
//        // Capture spawn position once; SnapBack() uses this fixed value.
//        _spawnAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Locked to the dragon — drag disabled until Attach is toggled off.
//        if (_isLocked) return;

//        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//        CurrentlyDragging = this;

//        // ── Release cannon slot if stationed there ────────────────────────────
//        if (_currentCannonSlot != null)
//        {
//            _currentCannonSlot.ReleaseSoldier(notify: false);
//            _currentCannonSlot = null;
//        }

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // ── Release horse seat if stationed there ─────────────────────────────
//        bool wasOnHorse = _currentHorseSeat != null;
//        HorseController mountedHorseHC = null;

//        if (wasOnHorse)
//        {
//            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
//        _rootCanvas = GetComponentInParent<Canvas>();

//        if (!wasMounted && !wasOnHorse)
//            _spawnAnchoredPosition = _rect.anchoredPosition;
//        RecordHome();

//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;
//        }

//        if (wasOnHorse && _mountHorseHomeParent != null)
//        {
//            _homeParent = _mountHorseHomeParent;
//            _homeAnchoredPosition = _mountHorseHomePos;
//            _mountHorseHomeParent = null;
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Restore own visuals before reparenting (in case coming from dragon or horse).
//        if (wasMounted || wasOnHorse)
//        {
//            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
//            ShowOwnVisuals();
//        }

//        // Store sibling order so SnapBack can restore it.
//        _dragOriginalParent = transform.parent;
//        _preDragSiblingIndex = transform.GetSiblingIndex();

//        // Lift to top of siblings so it renders above panel contents.
//        // No reparent -- soldier stays inside VillageSoldierSlot.
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // Notify dragon to hide its rider visual.
//        if (wasMounted)
//            mountedDragonDC?.PerformDismount();

//        // Notify horse to hide rider layers and reset to Idle.
//        if (wasOnHorse)
//            mountedHorseHC?.PerformDismount();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _isDragging = false;

//        // ── HORSE GUARD ───────────────────────────────────────────────────────
//        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
//        // already mounted this soldier. If we let OnEndDrag continue it would:
//        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
//        //   • Find no free horse (seat is now occupied) → targetHorse = null
//        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
//        //     with standing idle animation (the "ghost copy" bug).
//        // Early-exit here prevents all of that.
//        if (_currentHorseSeat != null)
//        {
//            CurrentlyDragging = null;
//            return;
//        }
//        // ─────────────────────────────────────────────────────────────────────

//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//        // CanvasGroup does not shadow targets sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;
//        CannonSlot targetCannon = null;
//        HorseController targetHorse = null;

//        foreach (var r in results)
//        {
//            // ── Check for cannon slot ─────────────────────────────────────────
//            if (targetCannon == null)
//                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//            // ── Check for dragon ──────────────────────────────────────────────
//            if (targetDC == null)
//            {
//                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//                if (dragon != null)
//                {
//                    targetDC = dragon;
//                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//                }
//            }

//            // ── Check for horse ───────────────────────────────────────────────
//            if (targetHorse == null)
//            {
//                var hc = r.gameObject.GetComponentInParent<HorseController>();
//                if (hc != null && !hc.IsOccupied)
//                    targetHorse = hc;
//            }

//            if (targetCannon != null && targetDC != null && targetHorse != null) break;
//        }

//        _canvasGroup.blocksRaycasts = true;

//        // ── Clear the static drag reference ───────────────────────────────────
//        CurrentlyDragging = null;

//        // ── Cannon slot drop ──────────────────────────────────────────────────
//        if (targetCannon != null)
//        {
//            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(false);
//            return;
//        }

//        // ── Horse drop ────────────────────────────────────────────────────────
//        // Alpha is intentionally NOT restored to 1 before this call.
//        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
//        // If we set alpha=1 first, the soldier flashes visible for one frame
//        // and if the IDropHandler path already ran, we get the duplicate visual.
//        if (targetHorse != null)
//        {
//            _mountHorseHomeParent = _homeParent;
//            _mountHorseHomePos = _homeAnchoredPosition;
//            targetHorse.PerformMount(this);
//            return;
//        }

//        // ── Dragon drop ───────────────────────────────────────────────────────
//        // Alpha is NOT restored before mount — DragonController.PerformMount
//        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
//        // guaranteed to be hidden after all animator calls complete.
//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetDC.PerformMount(this, targetSeat);
//            return;
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//                _mountHomeParent = _homeParent;
//                _mountHomePos = _homeAnchoredPosition;
//                currentRider.DismountFromDragon();
//                targetDC.PerformMount(this, targetSeat);
//                return;
//            }
//            else
//            {
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//        }
//        else
//        {
//            // No valid drop target — restore alpha and snap back.
//            _canvasGroup.alpha = 1f;
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        if (_homeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(true);
//            return;
//        }

//        // worldPositionStays: true preserves the world position on reparent,
//        // then we apply the fixed spawn anchor to land at the correct slot position.
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _spawnAnchoredPosition;
//        // Restore the original sibling order so rendering order is unchanged.
//        if (_dragOriginalParent == _homeParent)
//            transform.SetSiblingIndex(_preDragSiblingIndex);
//        ShowOwnVisuals();
//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by a drop target after accepting the soldier.
//    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Re-parents the soldier to spawnParent and resumes patrol.
//    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//    /// </summary>
//    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//    {
//        if (spawnParent == null)
//        {
//            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//            return;
//        }

//        transform.SetParent(spawnParent, worldPositionStays: true);

//        if (spawnPosition.HasValue)
//            _rect.anchoredPosition = spawnPosition.Value;

//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        ShowOwnVisuals();
//        RecordHome();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CANNON SLOT MOUNT / RELEASE
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//    /// cannon's SoldierSpawnpoint and records the new home.
//    ///
//    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//    /// or the ground spawn area.
//    /// </summary>
//    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//    {
//        if (slot == null || spawnpoint == null) return;

//        // Release any existing dragon seat first
//        if (_currentSeat != null)
//        {
//            var dc = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            dc?.PerformDismount();
//        }

//        // Release any existing horse seat first
//        if (_currentHorseSeat != null)
//        {
//            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            hc?.PerformDismount();
//        }

//        // Release previous cannon slot without notifying (we're already moving)
//        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//            _currentCannonSlot.ReleaseSoldier(notify: false);

//        _currentCannonSlot = slot;

//        // Reparent to the cannon's SoldierSpawnpoint
//        transform.SetParent(spawnpoint, worldPositionStays: false);
//        _rect.anchoredPosition = Vector2.zero;
//        _rect.localScale = Vector3.one;

//        // Record this position as home so SnapBack() returns here
//        RecordHome();
//        _spawnAnchoredPosition = _rect.anchoredPosition;

//        // Restore visuals and stop patrol
//        ShowOwnVisuals();
//        _animator?.SetState(AnimationState.Idle);
//        _controller?.ExitRidingState();

//        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//    }

//    /// <summary>
//    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//    /// Snaps the soldier back to their original home position.
//    /// </summary>
//    public void RemoveFromCannonSlot()
//    {
//        if (_currentCannonSlot == null) return;
//        _currentCannonSlot = null;
//        SnapBack();
//        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
//        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
//        SaveVisualLayerSnapshot();

//        EnsureHelmetEquipped();

//        _controller?.EnterRidingState();

//        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
//        // the Rider GameObject, inserted directly after it in the hierarchy.
//        Transform dragonRoot = seat.transform.parent != null
//            ? seat.transform.parent
//            : seat.transform;

//        transform.SetParent(dragonRoot, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;

//        // Place the soldier right after the Rider child in sibling order.
//        // Find the Rider by name; fall back to SetAsLastSibling if not found.
//        int riderIndex = -1;
//        for (int i = 0; i < dragonRoot.childCount; i++)
//        {
//            if (dragonRoot.GetChild(i).name == "Rider")
//            {
//                riderIndex = i;
//                break;
//            }
//        }

//        if (riderIndex >= 0)
//            transform.SetSiblingIndex(riderIndex + 1);
//        else
//            transform.SetAsLastSibling();

//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the soldier visible on top of the dragon.)
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
//                  $"at sibling index {transform.GetSiblingIndex()}.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        DragonController riderDragonDC = null;
//        if (_currentSeat != null)
//        {
//            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        if (_mountHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//                             "recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            RestoreVisualLayerSnapshot();
//            ShowOwnVisuals();
//            SnapBack();
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        gameObject.SetActive(true);
//        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
//        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
//        // already calls StartWalking() which sets AnimationState.Walk on the
//        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
//        // to standing still instead of resuming patrol walking.
//        RestoreVisualLayerSnapshot();
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHomeParent = null;

//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton.
//    /// </summary>
//    public void SetLocked(bool locked)
//    {
//        if (_currentSeat == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//            return;
//        }

//        _isLocked = locked;

//        if (locked)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.interactable = false;
//        }
//        else
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.interactable = true;
//        }

//        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    // ══════════════════════════════════════════════════════════════════════════
//    // LAYER SNAPSHOT (dragon mount / dismount)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Records the enabled/disabled state of every visual layer at the moment
//    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
//    /// so the soldier returns with exactly the outfit it had before mounting.
//    /// </summary>
//    private void SaveVisualLayerSnapshot()
//    {
//        if (_equipment == null) return;

//        // Record exactly which slots have an item equipped right now.
//        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
//        // helmet/armor will be absent from the set and removed on dismount.
//        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (_equipment.GetEquipped(slot) != null)
//                _preMountEquippedSlots.Add(slot);
//        }

//        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
//                  + string.Join(", ", _preMountEquippedSlots));
//    }

//    /// <summary>
//    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
//    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
//    /// </summary>
//    private void RestoreVisualLayerSnapshot()
//    {
//        if (_equipment == null || _preMountEquippedSlots == null) return;

//        // Unequip any slot that was NOT equipped before mounting.
//        // This removes items auto-added by EnsureHelmetEquipped() so the
//        // soldier returns with exactly the outfit it had before the drag.
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (!_preMountEquippedSlots.Contains(slot)
//                && _equipment.GetEquipped(slot) != null)
//            {
//                _equipment.Unequip(slot);
//                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
//            }
//        }

//        _preMountEquippedSlots = null;
//        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
//    }

//    public void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnsureHelmetEquipped()
//    {
//        if (_equipment == null) return;
//        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//        EquipmentItem helmet = helmetTable != null
//            ? helmetTable.GetDefaultHelmet(armor)
//            : null;

//        if (helmet != null)
//        {
//            _equipment.Equip(helmet);
//            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//        }
//        else
//        {
//            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//                             $"armor '{armor?.itemName ?? "none"}'. " +
//                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPER
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
//    ///
//    /// The soldier is reparented under the seat so it moves with the horse.
//    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
//    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
//    ///
//    /// FIX: EnterRidingState() is intentionally NOT called here.
//    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
//    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
//    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
//    /// If the soldier's CharacterVisuals image fields are wired to the same Image
//    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
//    /// those images get disabled right before HorseController.PerformMount calls
//    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
//    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
//    /// so no animator state change is needed.
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        // Only record home on the FIRST mount.
//        // On a transfer (walk-zone horse → slot horse), _mountHorseHomeParent is
//        // already set to the soldier's original walk-zone parent. Overwriting it
//        // here would capture the old horse's seat transform, which gets destroyed,
//        // so DismountFromHorse() would try to reparent into a null/dead object.
//        if (_mountHorseHomeParent == null)
//        {
//            _mountHorseHomeParent = _homeParent;
//            _mountHorseHomePos = _homeAnchoredPosition;
//        }
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();

//        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
//        _controller?.SetPatrolling(false);

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        // Guard: only proceed if this soldier was actually on a horse.
//        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
//        // was called before this (e.g. during a slot→walkzone transfer).
//        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
//        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
//        if (!wasMounted) return;

//        // Clear the seat reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and restores its own visuals.
//    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
//    /// </summary>
//    public void DismountFromHorse()
//    {
//        if (_currentHorseSeat != null)
//        {
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//        }

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
//                             "— snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
//    }

//    public void BecomeArcher(ArcherZoneCastle zone)
//    {
//        if (zone == null) return;

//        _currentArcherZone = zone;
//        _isLocked = true;

//        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
//        gameObject.SetActive(false);

//        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
//    }

//    /// <summary>
//    /// Called by ArcherZoneCastle.RemoveArcher().
//    /// Re-enables this soldier and snaps them back to their walk zone.
//    /// </summary>
//    public void ReturnFromArcher()
//    {
//        _currentArcherZone = null;
//        _isLocked = false;

//        // Re-show the soldier.
//        gameObject.SetActive(true);

//        // Snap back to the home position recorded before the last drag.
//        if (_homeParent != null)
//        {
//            transform.SetParent(_homeParent, worldPositionStays: false);
//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
//        }

//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
//    }

//    public void ClearHorseSeatForTransfer()
//    {
//        // Null out the seat so IsOnHorse returns false.
//        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
//        // Do NOT call ExitRidingState — the soldier stays in riding state
//        // so EnterRidingState / SetActive(false) in the next PerformMount
//        // runs cleanly without a redundant coroutine restart.
//        _currentHorseSeat = null;
//        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
//    }
//}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Dragon Mount Settings")]
////    [Tooltip("Maps each armor to its default helmet.\n" +
////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
////    [SerializeField] private ArmorHelmetTable helmetTable;

////    // ── Component References ──────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;    // optional — patrol + flip
////    private CharacterEquipment _equipment;    // tracks equipped items
////    private SpriteLayerAnimator _animator;    // drives per-layer animation
////    private CharacterVisuals _visuals;        // direct layer image access

////    // Snapshot of which equipment slots were occupied BEFORE dragon mount
////    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
////    // slot not in this set is unequipped so the soldier returns with exactly
////    // the outfit it had before mounting.
////    private HashSet<EquipmentSlot> _preMountEquippedSlots;

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
////    private Transform _dragOriginalParent; // parent before drag reparent
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;

////    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
////    // Start() and never updated by patrol. SnapBack() uses this so the soldier
////    // always returns to the same spot regardless of where it was patrolling
////    // when the player grabbed it.
////    private Vector2 _spawnAnchoredPosition;
////    private bool _isDragging;

////    // ── Dragon Rider State ────────────────────────────────────────────────────

////    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
////    private DragonRiderSeat _currentSeat;

////    /// <summary>
////    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
////    /// return the soldier to its patrol area, not back to the seat.
////    /// </summary>
////    private Transform _mountHomeParent;
////    private Vector2 _mountHomePos;

////    // ── Lock State ────────────────────────────────────────────────────────────

////    private bool _isLocked = false;

////    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
////    public bool IsRiding => _currentSeat != null;

////    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
////    public bool IsLocked => _isLocked;

////    // ── Cannon Slot State ─────────────────────────────────────────────────────

////    /// <summary>
////    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
////    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
////    /// </summary>
////    public static SoldierDragDrop CurrentlyDragging { get; private set; }

////    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
////    private CannonSlot _currentCannonSlot;

////    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
////    public bool IsAtCannon => _currentCannonSlot != null;

////    // ── Horse Rider State ─────────────────────────────────────────────────────

////    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
////    private HorseSeat _currentHorseSeat;

////    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
////    private Transform _mountHorseHomeParent;

////    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
////    private Vector2 _mountHorseHomePos;

////    /// <summary>True while this soldier is seated on a horse.</summary>
////    public bool IsOnHorse => _currentHorseSeat != null;

////    // ── Archer Zone State ─────────────────────────────────────────────────────

////    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
////    private ArcherZoneCastle _currentArcherZone;

////    /// <summary>True while this soldier is assigned to an archer zone.</summary>
////    public bool IsArcher => _currentArcherZone != null;

////    // ══════════════════════════════════════════════════════════════════════════
////    // LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////        _rect = GetComponent<RectTransform>();
////        _controller = GetComponent<SoldierController>();   // optional
////        _equipment = GetComponent<CharacterEquipment>();
////        _animator = GetComponent<SpriteLayerAnimator>();
////        _visuals = GetComponent<CharacterVisuals>();

////        if (_equipment == null)
////            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
////                             $"'{name}'. Helmet auto-equip will be skipped.", this);
////        if (_animator == null)
////            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
////                             $"'{name}'. Riding animation will not play.", this);
////        if (helmetTable == null)
////            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
////                             $"'{name}'. Soldiers will mount without a helmet.", this);
////    }

////    private void Start()
////    {
////        RecordHome();
////        // Capture spawn position once; SnapBack() uses this fixed value.
////        _spawnAnchoredPosition = _rect.anchoredPosition;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — BEGIN
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // Locked to the dragon — drag disabled until Attach is toggled off.
////        if (_isLocked) return;

////        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
////        CurrentlyDragging = this;

////        // ── Release cannon slot if stationed there ────────────────────────────
////        if (_currentCannonSlot != null)
////        {
////            _currentCannonSlot.ReleaseSoldier(notify: false);
////            _currentCannonSlot = null;
////        }

////        // ── Capture mounted dragon before clearing the seat ───────────────────
////        bool wasMounted = _currentSeat != null;
////        DragonController mountedDragonDC = null;

////        if (wasMounted)
////        {
////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // ── Release horse seat if stationed there ─────────────────────────────
////        bool wasOnHorse = _currentHorseSeat != null;
////        HorseController mountedHorseHC = null;

////        if (wasOnHorse)
////        {
////            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
////        _rootCanvas = GetComponentInParent<Canvas>();

////        if (!wasMounted && !wasOnHorse)
////            _spawnAnchoredPosition = _rect.anchoredPosition;
////        RecordHome();

////        if (wasMounted && _mountHomeParent != null)
////        {
////            _homeParent = _mountHomeParent;
////            _homeAnchoredPosition = _mountHomePos;
////            _mountHomeParent = null;
////        }

////        if (wasOnHorse && _mountHorseHomeParent != null)
////        {
////            _homeParent = _mountHorseHomeParent;
////            _homeAnchoredPosition = _mountHorseHomePos;
////            _mountHorseHomeParent = null;
////        }

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // Restore own visuals before reparenting (in case coming from dragon or horse).
////        if (wasMounted || wasOnHorse)
////        {
////            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
////            ShowOwnVisuals();
////        }

////        // Store sibling order so SnapBack can restore it.
////        _dragOriginalParent = transform.parent;
////        _preDragSiblingIndex = transform.GetSiblingIndex();

////        // Lift to top of siblings so it renders above panel contents.
////        // No reparent -- soldier stays inside VillageSoldierSlot.
////        transform.SetAsLastSibling();

////        _canvasGroup.alpha = 0.75f;
////        _canvasGroup.blocksRaycasts = false;

////        // Notify dragon to hide its rider visual.
////        if (wasMounted)
////            mountedDragonDC?.PerformDismount();

////        // Notify horse to hide rider layers and reset to Idle.
////        if (wasOnHorse)
////            mountedHorseHC?.PerformDismount();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;
////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — END
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        _isDragging = false;

////        // ── HORSE GUARD ───────────────────────────────────────────────────────
////        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
////        // already mounted this soldier. If we let OnEndDrag continue it would:
////        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
////        //   • Find no free horse (seat is now occupied) → targetHorse = null
////        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
////        //     with standing idle animation (the "ghost copy" bug).
////        // Early-exit here prevents all of that.
////        if (_currentHorseSeat != null)
////        {
////            CurrentlyDragging = null;
////            return;
////        }
////        // ─────────────────────────────────────────────────────────────────────

////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
////        // CanvasGroup does not shadow targets sitting underneath.

////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        DragonRiderSeat targetSeat = null;
////        DragonController targetDC = null;
////        CannonSlot targetCannon = null;
////        HorseController targetHorse = null;

////        foreach (var r in results)
////        {
////            // ── Check for cannon slot ─────────────────────────────────────────
////            if (targetCannon == null)
////                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

////            // ── Check for dragon ──────────────────────────────────────────────
////            if (targetDC == null)
////            {
////                var dragon = r.gameObject.GetComponentInParent<DragonController>();
////                if (dragon != null)
////                {
////                    targetDC = dragon;
////                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
////                }
////            }

////            // ── Check for horse ───────────────────────────────────────────────
////            if (targetHorse == null)
////            {
////                var hc = r.gameObject.GetComponentInParent<HorseController>();
////                if (hc != null && !hc.IsOccupied)
////                    targetHorse = hc;
////            }

////            if (targetCannon != null && targetDC != null && targetHorse != null) break;
////        }

////        _canvasGroup.blocksRaycasts = true;

////        // ── Clear the static drag reference ───────────────────────────────────
////        CurrentlyDragging = null;

////        // ── Cannon slot drop ──────────────────────────────────────────────────
////        if (targetCannon != null)
////        {
////            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
////            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
////            ShowOwnVisuals();
////            _controller?.SetPatrolling(false);
////            return;
////        }

////        // ── Horse drop ────────────────────────────────────────────────────────
////        // Alpha is intentionally NOT restored to 1 before this call.
////        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
////        // If we set alpha=1 first, the soldier flashes visible for one frame
////        // and if the IDropHandler path already ran, we get the duplicate visual.
////        if (targetHorse != null)
////        {
////            _mountHorseHomeParent = _homeParent;
////            _mountHorseHomePos = _homeAnchoredPosition;
////            targetHorse.PerformMount(this);
////            return;
////        }

////        // ── Dragon drop ───────────────────────────────────────────────────────
////        // Alpha is NOT restored before mount — DragonController.PerformMount
////        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
////        // guaranteed to be hidden after all animator calls complete.
////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && targetSeat != null && seatFree)
////        {
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;
////            targetDC.PerformMount(this, targetSeat);
////            return;
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

////            if (currentRider != null && currentRider._isLocked)
////            {
////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
////                _canvasGroup.alpha = 1f;
////                SnapBack();
////            }
////            else if (currentRider != null)
////            {
////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
////                _mountHomeParent = _homeParent;
////                _mountHomePos = _homeAnchoredPosition;
////                currentRider.DismountFromDragon();
////                targetDC.PerformMount(this, targetSeat);
////                return;
////            }
////            else
////            {
////                _canvasGroup.alpha = 1f;
////                SnapBack();
////            }
////        }
////        else
////        {
////            // No valid drop target — restore alpha and snap back.
////            _canvasGroup.alpha = 1f;
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DROP OUTCOMES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////    public void SnapBack()
////    {
////        if (_homeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
////            ShowOwnVisuals();
////            _controller?.SetPatrolling(true);
////            return;
////        }

////        // worldPositionStays: true preserves the world position on reparent,
////        // then we apply the fixed spawn anchor to land at the correct slot position.
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _spawnAnchoredPosition;
////        // Restore the original sibling order so rendering order is unchanged.
////        if (_dragOriginalParent == _homeParent)
////            transform.SetSiblingIndex(_preDragSiblingIndex);
////        ShowOwnVisuals();
////        _controller?.SetPatrolling(true);
////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by a drop target after accepting the soldier.
////    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;
////        _controller?.SetPatrolling(false);
////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////    }

////    /// <summary>
////    /// Re-parents the soldier to spawnParent and resumes patrol.
////    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
////    /// </summary>
////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
////    {
////        if (spawnParent == null)
////        {
////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
////            return;
////        }

////        transform.SetParent(spawnParent, worldPositionStays: true);

////        if (spawnPosition.HasValue)
////            _rect.anchoredPosition = spawnPosition.Value;

////        _canvasGroup.blocksRaycasts = true;
////        _isDragging = false;

////        ShowOwnVisuals();
////        RecordHome();
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // CANNON SLOT MOUNT / RELEASE
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
////    /// cannon's SoldierSpawnpoint and records the new home.
////    ///
////    /// Safe to call whether the soldier was at a dragon, another cannon slot,
////    /// or the ground spawn area.
////    /// </summary>
////    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
////    {
////        if (slot == null || spawnpoint == null) return;

////        // Release any existing dragon seat first
////        if (_currentSeat != null)
////        {
////            var dc = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            dc?.PerformDismount();
////        }

////        // Release any existing horse seat first
////        if (_currentHorseSeat != null)
////        {
////            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////            hc?.PerformDismount();
////        }

////        // Release previous cannon slot without notifying (we're already moving)
////        if (_currentCannonSlot != null && _currentCannonSlot != slot)
////            _currentCannonSlot.ReleaseSoldier(notify: false);

////        _currentCannonSlot = slot;

////        // Reparent to the cannon's SoldierSpawnpoint
////        transform.SetParent(spawnpoint, worldPositionStays: false);
////        _rect.anchoredPosition = Vector2.zero;
////        _rect.localScale = Vector3.one;

////        // Record this position as home so SnapBack() returns here
////        RecordHome();
////        _spawnAnchoredPosition = _rect.anchoredPosition;

////        // Restore visuals and stop patrol
////        ShowOwnVisuals();
////        _animator?.SetState(AnimationState.Idle);
////        _controller?.ExitRidingState();

////        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
////    }

////    /// <summary>
////    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
////    /// Snaps the soldier back to their original home position.
////    /// </summary>
////    public void RemoveFromCannonSlot()
////    {
////        if (_currentCannonSlot == null) return;
////        _currentCannonSlot = null;
////        SnapBack();
////        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
////    /// </summary>
////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
////    {
////        _currentSeat = seat;

////        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
////        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
////        SaveVisualLayerSnapshot();

////        EnsureHelmetEquipped();

////        _controller?.EnterRidingState();

////        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
////        // the Rider GameObject, inserted directly after it in the hierarchy.
////        Transform dragonRoot = seat.transform.parent != null
////            ? seat.transform.parent
////            : seat.transform;

////        transform.SetParent(dragonRoot, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;

////        // Place the soldier right after the Rider child in sibling order.
////        // Find the Rider by name; fall back to SetAsLastSibling if not found.
////        int riderIndex = -1;
////        for (int i = 0; i < dragonRoot.childCount; i++)
////        {
////            if (dragonRoot.GetChild(i).name == "Rider")
////            {
////                riderIndex = i;
////                break;
////            }
////        }

////        if (riderIndex >= 0)
////            transform.SetSiblingIndex(riderIndex + 1);
////        else
////            transform.SetAsLastSibling();

////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the soldier visible on top of the dragon.)
////        _animator?.SetState(AnimationState.RiderIdle);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
////                  $"at sibling index {transform.GetSiblingIndex()}.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
////    /// </summary>
////    public void DismountFromDragon()
////    {
////        DragonController riderDragonDC = null;
////        if (_currentSeat != null)
////        {
////            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////        }

////        if (_mountHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
////                             "recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            RestoreVisualLayerSnapshot();
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        gameObject.SetActive(true);
////        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
////        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
////        // already calls StartWalking() which sets AnimationState.Walk on the
////        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
////        // to standing still instead of resuming patrol walking.
////        RestoreVisualLayerSnapshot();
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHomeParent = null;

////        riderDragonDC?.PerformDismount();

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // ATTACH LOCK
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Locks or unlocks the soldier to the current dragon seat.
////    /// Called by DragonAttachButton.
////    /// </summary>
////    public void SetLocked(bool locked)
////    {
////        if (_currentSeat == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
////            return;
////        }

////        _isLocked = locked;

////        if (locked)
////        {
////            _canvasGroup.blocksRaycasts = false;
////            _canvasGroup.interactable = false;
////        }
////        else
////        {
////            _canvasGroup.blocksRaycasts = true;
////            _canvasGroup.interactable = true;
////        }

////        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
////                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // VISUAL SHOW / HIDE
////    // ══════════════════════════════════════════════════════════════════════════

////    // ══════════════════════════════════════════════════════════════════════════
////    // LAYER SNAPSHOT (dragon mount / dismount)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Records the enabled/disabled state of every visual layer at the moment
////    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
////    /// so the soldier returns with exactly the outfit it had before mounting.
////    /// </summary>
////    private void SaveVisualLayerSnapshot()
////    {
////        if (_equipment == null) return;

////        // Record exactly which slots have an item equipped right now.
////        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
////        // helmet/armor will be absent from the set and removed on dismount.
////        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
////        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
////        {
////            if (_equipment.GetEquipped(slot) != null)
////                _preMountEquippedSlots.Add(slot);
////        }

////        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
////                  + string.Join(", ", _preMountEquippedSlots));
////    }

////    /// <summary>
////    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
////    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
////    /// </summary>
////    private void RestoreVisualLayerSnapshot()
////    {
////        if (_equipment == null || _preMountEquippedSlots == null) return;

////        // Unequip any slot that was NOT equipped before mounting.
////        // This removes items auto-added by EnsureHelmetEquipped() so the
////        // soldier returns with exactly the outfit it had before the drag.
////        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
////        {
////            if (!_preMountEquippedSlots.Contains(slot)
////                && _equipment.GetEquipped(slot) != null)
////            {
////                _equipment.Unequip(slot);
////                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
////            }
////        }

////        _preMountEquippedSlots = null;
////        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
////    }

////    public void HideOwnVisuals()
////    {
////        _canvasGroup.alpha = 0f;
////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.interactable = true;
////    }

////    private void ShowOwnVisuals()
////    {
////        _canvasGroup.alpha = 1f;
////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.interactable = true;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELMET AUTO-EQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnsureHelmetEquipped()
////    {
////        if (_equipment == null) return;
////        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

////        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
////        EquipmentItem helmet = helmetTable != null
////            ? helmetTable.GetDefaultHelmet(armor)
////            : null;

////        if (helmet != null)
////        {
////            _equipment.Equip(helmet);
////            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
////                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
////        }
////        else
////        {
////            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
////                             $"armor '{armor?.itemName ?? "none"}'. " +
////                             "Set fallbackHelmet in ArmorHelmetTable.", this);
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPER
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RecordHome()
////    {
////        _homeParent = transform.parent;
////        _homeAnchoredPosition = _rect.anchoredPosition;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
////    ///
////    /// The soldier is reparented under the seat so it moves with the horse.
////    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
////    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
////    ///
////    /// FIX: EnterRidingState() is intentionally NOT called here.
////    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
////    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
////    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
////    /// If the soldier's CharacterVisuals image fields are wired to the same Image
////    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
////    /// those images get disabled right before HorseController.PerformMount calls
////    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
////    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
////    /// so no animator state change is needed.
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();

////        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
////        _controller?.SetPatrolling(false);

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        // Guard: only proceed if this soldier was actually on a horse.
////        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
////        // was called before this (e.g. during a slot→walkzone transfer).
////        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
////        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
////        if (!wasMounted) return;

////        // Clear the seat reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.SetPatrolling(true);
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _animator?.SetState(AnimationState.Idle);
////        _controller?.SetPatrolling(true);
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHorseHomeParent = null;

////        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
////    }

////    /// <summary>
////    /// Returns the soldier to the ground and restores its own visuals.
////    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
////    /// </summary>
////    public void DismountFromHorse()
////    {
////        if (_currentHorseSeat != null)
////        {
////            _currentHorseSeat.ReleaseSoldier();
////            _currentHorseSeat = null;
////        }

////        if (_mountHorseHomeParent == null)
////        {
////            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
////                             "— snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.SetPatrolling(true);
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _animator?.SetState(AnimationState.Idle);
////        _controller?.SetPatrolling(true);
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHorseHomeParent = null;

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
////    }

////    public void BecomeArcher(ArcherZoneCastle zone)
////    {
////        if (zone == null) return;

////        _currentArcherZone = zone;
////        _isLocked = true;

////        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
////        gameObject.SetActive(false);

////        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
////    }

////    /// <summary>
////    /// Called by ArcherZoneCastle.RemoveArcher().
////    /// Re-enables this soldier and snaps them back to their walk zone.
////    /// </summary>
////    public void ReturnFromArcher()
////    {
////        _currentArcherZone = null;
////        _isLocked = false;

////        // Re-show the soldier.
////        gameObject.SetActive(true);

////        // Snap back to the home position recorded before the last drag.
////        if (_homeParent != null)
////        {
////            transform.SetParent(_homeParent, worldPositionStays: false);
////            RectTransform rt = GetComponent<RectTransform>();
////            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
////        }

////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
////    }

////    public void ClearHorseSeatForTransfer()
////    {
////        // Null out the seat so IsOnHorse returns false.
////        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
////        // Do NOT call ExitRidingState — the soldier stays in riding state
////        // so EnterRidingState / SetActive(false) in the next PerformMount
////        // runs cleanly without a redundant coroutine restart.
////        _currentHorseSeat = null;
////        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
////    }
////}


//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
//    [SerializeField] private ArmorHelmetTable helmetTable;

//    // ── Component References ──────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;    // optional — patrol + flip
//    private CharacterEquipment _equipment;    // tracks equipped items
//    private SpriteLayerAnimator _animator;    // drives per-layer animation
//    private CharacterVisuals _visuals;        // direct layer image access

//    // Snapshot of which equipment slots were occupied BEFORE dragon mount
//    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
//    // slot not in this set is unequipped so the soldier returns with exactly
//    // the outfit it had before mounting.
//    private HashSet<EquipmentSlot> _preMountEquippedSlots;

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
//    private Transform _dragOriginalParent; // parent before drag reparent
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;

//    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
//    // Start() and never updated by patrol. SnapBack() uses this so the soldier
//    // always returns to the same spot regardless of where it was patrolling
//    // when the player grabbed it.
//    private Vector2 _spawnAnchoredPosition;
//    private bool _isDragging;

//    // ── Dragon Rider State ────────────────────────────────────────────────────

//    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
//    private DragonRiderSeat _currentSeat;

//    /// <summary>
//    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
//    /// return the soldier to its patrol area, not back to the seat.
//    /// </summary>
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ── Lock State ────────────────────────────────────────────────────────────

//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

//    // ── Cannon Slot State ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//    /// </summary>
//    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//    private CannonSlot _currentCannonSlot;

//    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//    public bool IsAtCannon => _currentCannonSlot != null;

//    // ── Horse Rider State ─────────────────────────────────────────────────────

//    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
//    private HorseSeat _currentHorseSeat;

//    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Transform _mountHorseHomeParent;

//    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Vector2 _mountHorseHomePos;

//    /// <summary>True while this soldier is seated on a horse.</summary>
//    public bool IsOnHorse => _currentHorseSeat != null;

//    // ── Archer Zone State ─────────────────────────────────────────────────────

//    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
//    private ArcherZoneCastle _currentArcherZone;

//    /// <summary>True while this soldier is assigned to an archer zone.</summary>
//    public bool IsArcher => _currentArcherZone != null;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();   // optional
//        _equipment = GetComponent<CharacterEquipment>();
//        _animator = GetComponent<SpriteLayerAnimator>();
//        _visuals = GetComponent<CharacterVisuals>();

//        if (_equipment == null)
//            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//        if (_animator == null)
//            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//                             $"'{name}'. Riding animation will not play.", this);
//        if (helmetTable == null)
//            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//                             $"'{name}'. Soldiers will mount without a helmet.", this);
//    }

//    private void Start()
//    {
//        RecordHome();
//        // Capture spawn position once; SnapBack() uses this fixed value.
//        _spawnAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Locked to the dragon — drag disabled until Attach is toggled off.
//        if (_isLocked) return;

//        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//        CurrentlyDragging = this;

//        // ── Release cannon slot if stationed there ────────────────────────────
//        if (_currentCannonSlot != null)
//        {
//            _currentCannonSlot.ReleaseSoldier(notify: false);
//            _currentCannonSlot = null;
//        }

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // ── Release horse seat if stationed there ─────────────────────────────
//        bool wasOnHorse = _currentHorseSeat != null;
//        HorseController mountedHorseHC = null;

//        if (wasOnHorse)
//        {
//            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
//        _rootCanvas = GetComponentInParent<Canvas>();

//        if (!wasMounted && !wasOnHorse)
//            _spawnAnchoredPosition = _rect.anchoredPosition;
//        RecordHome();

//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;
//        }

//        if (wasOnHorse && _mountHorseHomeParent != null)
//        {
//            _homeParent = _mountHorseHomeParent;
//            _homeAnchoredPosition = _mountHorseHomePos;
//            _mountHorseHomeParent = null;
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Restore own visuals before reparenting (in case coming from dragon or horse).
//        if (wasMounted || wasOnHorse)
//        {
//            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
//            ShowOwnVisuals();
//        }

//        // Store sibling order so SnapBack can restore it.
//        _dragOriginalParent = transform.parent;
//        _preDragSiblingIndex = transform.GetSiblingIndex();

//        // Lift to top of siblings so it renders above panel contents.
//        // No reparent -- soldier stays inside VillageSoldierSlot.
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // Notify dragon to hide its rider visual.
//        if (wasMounted)
//            mountedDragonDC?.PerformDismount();

//        // Notify horse to hide rider layers and reset to Idle.
//        if (wasOnHorse)
//            mountedHorseHC?.PerformDismount();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _isDragging = false;

//        // ── HORSE GUARD ───────────────────────────────────────────────────────
//        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
//        // already mounted this soldier. If we let OnEndDrag continue it would:
//        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
//        //   • Find no free horse (seat is now occupied) → targetHorse = null
//        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
//        //     with standing idle animation (the "ghost copy" bug).
//        // Early-exit here prevents all of that.
//        if (_currentHorseSeat != null)
//        {
//            CurrentlyDragging = null;
//            return;
//        }
//        // ─────────────────────────────────────────────────────────────────────

//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//        // CanvasGroup does not shadow targets sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;
//        CannonSlot targetCannon = null;
//        HorseController targetHorse = null;

//        foreach (var r in results)
//        {
//            // ── Check for cannon slot ─────────────────────────────────────────
//            if (targetCannon == null)
//                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//            // ── Check for dragon ──────────────────────────────────────────────
//            if (targetDC == null)
//            {
//                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//                if (dragon != null)
//                {
//                    targetDC = dragon;
//                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//                }
//            }

//            // ── Check for horse ───────────────────────────────────────────────
//            if (targetHorse == null)
//            {
//                var hc = r.gameObject.GetComponentInParent<HorseController>();
//                if (hc != null && !hc.IsOccupied)
//                    targetHorse = hc;
//            }

//            if (targetCannon != null && targetDC != null && targetHorse != null) break;
//        }

//        _canvasGroup.blocksRaycasts = true;

//        // ── Clear the static drag reference ───────────────────────────────────
//        CurrentlyDragging = null;

//        // ── Cannon slot drop ──────────────────────────────────────────────────
//        if (targetCannon != null)
//        {
//            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(false);
//            return;
//        }

//        // ── Horse drop ────────────────────────────────────────────────────────
//        // Alpha is intentionally NOT restored to 1 before this call.
//        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
//        // If we set alpha=1 first, the soldier flashes visible for one frame
//        // and if the IDropHandler path already ran, we get the duplicate visual.
//        if (targetHorse != null)
//        {
//            _mountHorseHomeParent = _homeParent;
//            _mountHorseHomePos = _homeAnchoredPosition;
//            targetHorse.PerformMount(this);
//            return;
//        }

//        // ── Dragon drop ───────────────────────────────────────────────────────
//        // Alpha is NOT restored before mount — DragonController.PerformMount
//        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
//        // guaranteed to be hidden after all animator calls complete.
//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetDC.PerformMount(this, targetSeat);
//            return;
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//                _mountHomeParent = _homeParent;
//                _mountHomePos = _homeAnchoredPosition;
//                currentRider.DismountFromDragon();
//                targetDC.PerformMount(this, targetSeat);
//                return;
//            }
//            else
//            {
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//        }
//        else
//        {
//            // No valid drop target — restore alpha and snap back.
//            _canvasGroup.alpha = 1f;
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        if (_homeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(true);
//            return;
//        }

//        // worldPositionStays: true preserves the world position on reparent,
//        // then we apply the fixed spawn anchor to land at the correct slot position.
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _spawnAnchoredPosition;
//        // Restore the original sibling order so rendering order is unchanged.
//        if (_dragOriginalParent == _homeParent)
//            transform.SetSiblingIndex(_preDragSiblingIndex);
//        ShowOwnVisuals();
//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by a drop target after accepting the soldier.
//    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Re-parents the soldier to spawnParent and resumes patrol.
//    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//    /// </summary>
//    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//    {
//        if (spawnParent == null)
//        {
//            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//            return;
//        }

//        transform.SetParent(spawnParent, worldPositionStays: true);

//        if (spawnPosition.HasValue)
//            _rect.anchoredPosition = spawnPosition.Value;

//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        ShowOwnVisuals();
//        RecordHome();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CANNON SLOT MOUNT / RELEASE
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//    /// cannon's SoldierSpawnpoint and records the new home.
//    ///
//    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//    /// or the ground spawn area.
//    /// </summary>
//    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//    {
//        if (slot == null || spawnpoint == null) return;

//        // Release any existing dragon seat first
//        if (_currentSeat != null)
//        {
//            var dc = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            dc?.PerformDismount();
//        }

//        // Release any existing horse seat first
//        if (_currentHorseSeat != null)
//        {
//            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            hc?.PerformDismount();
//        }

//        // Release previous cannon slot without notifying (we're already moving)
//        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//            _currentCannonSlot.ReleaseSoldier(notify: false);

//        _currentCannonSlot = slot;

//        // Reparent to the cannon's SoldierSpawnpoint
//        transform.SetParent(spawnpoint, worldPositionStays: false);
//        _rect.anchoredPosition = Vector2.zero;
//        _rect.localScale = Vector3.one;

//        // Record this position as home so SnapBack() returns here
//        RecordHome();
//        _spawnAnchoredPosition = _rect.anchoredPosition;

//        // Restore visuals and stop patrol
//        ShowOwnVisuals();
//        _animator?.SetState(AnimationState.Idle);
//        _controller?.ExitRidingState();

//        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//    }

//    /// <summary>
//    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//    /// Snaps the soldier back to their original home position.
//    /// </summary>
//    public void RemoveFromCannonSlot()
//    {
//        if (_currentCannonSlot == null) return;
//        _currentCannonSlot = null;
//        SnapBack();
//        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
//        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
//        SaveVisualLayerSnapshot();

//        EnsureHelmetEquipped();

//        _controller?.EnterRidingState();

//        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
//        // the Rider GameObject, inserted directly after it in the hierarchy.
//        Transform dragonRoot = seat.transform.parent != null
//            ? seat.transform.parent
//            : seat.transform;

//        transform.SetParent(dragonRoot, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;

//        // Place the soldier right after the Rider child in sibling order.
//        // Find the Rider by name; fall back to SetAsLastSibling if not found.
//        int riderIndex = -1;
//        for (int i = 0; i < dragonRoot.childCount; i++)
//        {
//            if (dragonRoot.GetChild(i).name == "Rider")
//            {
//                riderIndex = i;
//                break;
//            }
//        }

//        if (riderIndex >= 0)
//            transform.SetSiblingIndex(riderIndex + 1);
//        else
//            transform.SetAsLastSibling();

//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the soldier visible on top of the dragon.)
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
//                  $"at sibling index {transform.GetSiblingIndex()}.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        DragonController riderDragonDC = null;
//        if (_currentSeat != null)
//        {
//            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        if (_mountHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//                             "recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            RestoreVisualLayerSnapshot();
//            ShowOwnVisuals();
//            SnapBack();
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        gameObject.SetActive(true);
//        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
//        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
//        // already calls StartWalking() which sets AnimationState.Walk on the
//        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
//        // to standing still instead of resuming patrol walking.
//        RestoreVisualLayerSnapshot();
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHomeParent = null;

//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton.
//    /// </summary>
//    public void SetLocked(bool locked)
//    {
//        if (_currentSeat == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//            return;
//        }

//        _isLocked = locked;

//        if (locked)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.interactable = false;
//        }
//        else
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.interactable = true;
//        }

//        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    // ══════════════════════════════════════════════════════════════════════════
//    // LAYER SNAPSHOT (dragon mount / dismount)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Records the enabled/disabled state of every visual layer at the moment
//    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
//    /// so the soldier returns with exactly the outfit it had before mounting.
//    /// </summary>
//    private void SaveVisualLayerSnapshot()
//    {
//        if (_equipment == null) return;

//        // Record exactly which slots have an item equipped right now.
//        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
//        // helmet/armor will be absent from the set and removed on dismount.
//        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (_equipment.GetEquipped(slot) != null)
//                _preMountEquippedSlots.Add(slot);
//        }

//        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
//                  + string.Join(", ", _preMountEquippedSlots));
//    }

//    /// <summary>
//    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
//    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
//    /// </summary>
//    private void RestoreVisualLayerSnapshot()
//    {
//        if (_equipment == null || _preMountEquippedSlots == null) return;

//        // Unequip any slot that was NOT equipped before mounting.
//        // This removes items auto-added by EnsureHelmetEquipped() so the
//        // soldier returns with exactly the outfit it had before the drag.
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (!_preMountEquippedSlots.Contains(slot)
//                && _equipment.GetEquipped(slot) != null)
//            {
//                _equipment.Unequip(slot);
//                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
//            }
//        }

//        _preMountEquippedSlots = null;
//        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
//    }

//    public void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnsureHelmetEquipped()
//    {
//        if (_equipment == null) return;
//        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//        EquipmentItem helmet = helmetTable != null
//            ? helmetTable.GetDefaultHelmet(armor)
//            : null;

//        if (helmet != null)
//        {
//            _equipment.Equip(helmet);
//            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//        }
//        else
//        {
//            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//                             $"armor '{armor?.itemName ?? "none"}'. " +
//                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPER
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
//    ///
//    /// The soldier is reparented under the seat so it moves with the horse.
//    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
//    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
//    ///
//    /// FIX: EnterRidingState() is intentionally NOT called here.
//    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
//    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
//    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
//    /// If the soldier's CharacterVisuals image fields are wired to the same Image
//    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
//    /// those images get disabled right before HorseController.PerformMount calls
//    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
//    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
//    /// so no animator state change is needed.
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        // Only record home on the FIRST mount.
//        // On a transfer (walk-zone horse → slot horse), _mountHorseHomeParent is
//        // already set to the soldier's original walk-zone parent. Overwriting it
//        // here would capture the old horse's seat transform, which gets destroyed,
//        // so DismountFromHorse() would try to reparent into a null/dead object.
//        if (_mountHorseHomeParent == null)
//        {
//            _mountHorseHomeParent = _homeParent;
//            _mountHorseHomePos = _homeAnchoredPosition;
//        }
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();

//        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
//        _controller?.SetPatrolling(false);

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        // Guard: only proceed if this soldier was actually on a horse.
//        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
//        // was called before this (e.g. during a slot→walkzone transfer).
//        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
//        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
//        if (!wasMounted) return;

//        // Clear the seat reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and restores its own visuals.
//    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
//    /// </summary>
//    public void DismountFromHorse()
//    {
//        if (_currentHorseSeat != null)
//        {
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//        }

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
//                             "— snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
//    }

//    public void BecomeArcher(ArcherZoneCastle zone)
//    {
//        if (zone == null) return;

//        _currentArcherZone = zone;
//        _isLocked = true;

//        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
//        gameObject.SetActive(false);

//        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
//    }

//    /// <summary>
//    /// Called by ArcherZoneCastle.RemoveArcher().
//    /// Re-enables this soldier and snaps them back to their walk zone.
//    /// </summary>
//    public void ReturnFromArcher()
//    {
//        _currentArcherZone = null;
//        _isLocked = false;

//        // Re-show the soldier.
//        gameObject.SetActive(true);

//        // Snap back to the home position recorded before the last drag.
//        if (_homeParent != null)
//        {
//            transform.SetParent(_homeParent, worldPositionStays: false);
//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
//        }

//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
//    }

//    public void ClearHorseSeatForTransfer()
//    {
//        // Null out the seat so IsOnHorse returns false.
//        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
//        // Do NOT call ExitRidingState — the soldier stays in riding state
//        // so EnterRidingState / SetActive(false) in the next PerformMount
//        // runs cleanly without a redundant coroutine restart.
//        _currentHorseSeat = null;
//        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
//    }
//}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
//    [SerializeField] private ArmorHelmetTable helmetTable;

//    // ── Component References ──────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;    // optional — patrol + flip
//    private CharacterEquipment _equipment;    // tracks equipped items
//    private SpriteLayerAnimator _animator;    // drives per-layer animation
//    private CharacterVisuals _visuals;        // direct layer image access

//    // Snapshot of which equipment slots were occupied BEFORE dragon mount
//    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
//    // slot not in this set is unequipped so the soldier returns with exactly
//    // the outfit it had before mounting.
//    private HashSet<EquipmentSlot> _preMountEquippedSlots;

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
//    private Transform _dragOriginalParent; // parent before drag reparent
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;

//    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
//    // Start() and never updated by patrol. SnapBack() uses this so the soldier
//    // always returns to the same spot regardless of where it was patrolling
//    // when the player grabbed it.
//    private Vector2 _spawnAnchoredPosition;
//    private bool _isDragging;

//    // ── Dragon Rider State ────────────────────────────────────────────────────

//    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
//    private DragonRiderSeat _currentSeat;

//    /// <summary>
//    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
//    /// return the soldier to its patrol area, not back to the seat.
//    /// </summary>
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ── Lock State ────────────────────────────────────────────────────────────

//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

//    // ── Cannon Slot State ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//    /// </summary>
//    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//    private CannonSlot _currentCannonSlot;

//    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//    public bool IsAtCannon => _currentCannonSlot != null;

//    // ── Horse Rider State ─────────────────────────────────────────────────────

//    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
//    private HorseSeat _currentHorseSeat;

//    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Transform _mountHorseHomeParent;

//    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
//    private Vector2 _mountHorseHomePos;

//    /// <summary>True while this soldier is seated on a horse.</summary>
//    public bool IsOnHorse => _currentHorseSeat != null;

//    // ── Archer Zone State ─────────────────────────────────────────────────────

//    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
//    private ArcherZoneCastle _currentArcherZone;

//    /// <summary>True while this soldier is assigned to an archer zone.</summary>
//    public bool IsArcher => _currentArcherZone != null;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();   // optional
//        _equipment = GetComponent<CharacterEquipment>();
//        _animator = GetComponent<SpriteLayerAnimator>();
//        _visuals = GetComponent<CharacterVisuals>();

//        if (_equipment == null)
//            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//        if (_animator == null)
//            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//                             $"'{name}'. Riding animation will not play.", this);
//        if (helmetTable == null)
//            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//                             $"'{name}'. Soldiers will mount without a helmet.", this);
//    }

//    private void Start()
//    {
//        RecordHome();
//        // Capture spawn position once; SnapBack() uses this fixed value.
//        _spawnAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Locked to the dragon — drag disabled until Attach is toggled off.
//        if (_isLocked) return;

//        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//        CurrentlyDragging = this;

//        // ── Release cannon slot if stationed there ────────────────────────────
//        if (_currentCannonSlot != null)
//        {
//            _currentCannonSlot.ReleaseSoldier(notify: false);
//            _currentCannonSlot = null;
//        }

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // ── Release horse seat if stationed there ─────────────────────────────
//        bool wasOnHorse = _currentHorseSeat != null;
//        HorseController mountedHorseHC = null;

//        if (wasOnHorse)
//        {
//            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
//        _rootCanvas = GetComponentInParent<Canvas>();

//        if (!wasMounted && !wasOnHorse)
//            _spawnAnchoredPosition = _rect.anchoredPosition;
//        RecordHome();

//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;
//        }

//        if (wasOnHorse && _mountHorseHomeParent != null)
//        {
//            _homeParent = _mountHorseHomeParent;
//            _homeAnchoredPosition = _mountHorseHomePos;
//            _mountHorseHomeParent = null;
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Restore own visuals before reparenting (in case coming from dragon or horse).
//        if (wasMounted || wasOnHorse)
//        {
//            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
//            ShowOwnVisuals();
//        }

//        // Store sibling order so SnapBack can restore it.
//        _dragOriginalParent = transform.parent;
//        _preDragSiblingIndex = transform.GetSiblingIndex();

//        // Lift to top of siblings so it renders above panel contents.
//        // No reparent -- soldier stays inside VillageSoldierSlot.
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // Notify dragon to hide its rider visual.
//        if (wasMounted)
//            mountedDragonDC?.PerformDismount();

//        // Notify horse to hide rider layers and reset to Idle.
//        if (wasOnHorse)
//            mountedHorseHC?.PerformDismount();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _isDragging = false;

//        // ── HORSE GUARD ───────────────────────────────────────────────────────
//        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
//        // already mounted this soldier. If we let OnEndDrag continue it would:
//        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
//        //   • Find no free horse (seat is now occupied) → targetHorse = null
//        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
//        //     with standing idle animation (the "ghost copy" bug).
//        // Early-exit here prevents all of that.
//        if (_currentHorseSeat != null)
//        {
//            CurrentlyDragging = null;
//            return;
//        }
//        // ─────────────────────────────────────────────────────────────────────

//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//        // CanvasGroup does not shadow targets sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;
//        CannonSlot targetCannon = null;
//        HorseController targetHorse = null;

//        foreach (var r in results)
//        {
//            // ── Check for cannon slot ─────────────────────────────────────────
//            if (targetCannon == null)
//                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//            // ── Check for dragon ──────────────────────────────────────────────
//            if (targetDC == null)
//            {
//                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//                if (dragon != null)
//                {
//                    targetDC = dragon;
//                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//                }
//            }

//            // ── Check for horse ───────────────────────────────────────────────
//            if (targetHorse == null)
//            {
//                var hc = r.gameObject.GetComponentInParent<HorseController>();
//                if (hc != null && !hc.IsOccupied)
//                    targetHorse = hc;
//            }

//            if (targetCannon != null && targetDC != null && targetHorse != null) break;
//        }

//        _canvasGroup.blocksRaycasts = true;

//        // ── Clear the static drag reference ───────────────────────────────────
//        CurrentlyDragging = null;

//        // ── Cannon slot drop ──────────────────────────────────────────────────
//        if (targetCannon != null)
//        {
//            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(false);
//            return;
//        }

//        // ── Horse drop ────────────────────────────────────────────────────────
//        // Alpha is intentionally NOT restored to 1 before this call.
//        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
//        // If we set alpha=1 first, the soldier flashes visible for one frame
//        // and if the IDropHandler path already ran, we get the duplicate visual.
//        if (targetHorse != null)
//        {
//            _mountHorseHomeParent = _homeParent;
//            _mountHorseHomePos = _homeAnchoredPosition;
//            targetHorse.PerformMount(this);
//            return;
//        }

//        // ── Dragon drop ───────────────────────────────────────────────────────
//        // Alpha is NOT restored before mount — DragonController.PerformMount
//        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
//        // guaranteed to be hidden after all animator calls complete.
//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetDC.PerformMount(this, targetSeat);
//            return;
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//                _mountHomeParent = _homeParent;
//                _mountHomePos = _homeAnchoredPosition;
//                currentRider.DismountFromDragon();
//                targetDC.PerformMount(this, targetSeat);
//                return;
//            }
//            else
//            {
//                _canvasGroup.alpha = 1f;
//                SnapBack();
//            }
//        }
//        else
//        {
//            // No valid drop target — restore alpha and snap back.
//            _canvasGroup.alpha = 1f;
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        if (_homeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(true);
//            return;
//        }

//        // worldPositionStays: true preserves the world position on reparent,
//        // then we apply the fixed spawn anchor to land at the correct slot position.
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _spawnAnchoredPosition;
//        // Restore the original sibling order so rendering order is unchanged.
//        if (_dragOriginalParent == _homeParent)
//            transform.SetSiblingIndex(_preDragSiblingIndex);
//        ShowOwnVisuals();
//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by a drop target after accepting the soldier.
//    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Re-parents the soldier to spawnParent and resumes patrol.
//    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//    /// </summary>
//    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//    {
//        if (spawnParent == null)
//        {
//            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//            return;
//        }

//        transform.SetParent(spawnParent, worldPositionStays: true);

//        if (spawnPosition.HasValue)
//            _rect.anchoredPosition = spawnPosition.Value;

//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        ShowOwnVisuals();
//        RecordHome();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CANNON SLOT MOUNT / RELEASE
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//    /// cannon's SoldierSpawnpoint and records the new home.
//    ///
//    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//    /// or the ground spawn area.
//    /// </summary>
//    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//    {
//        if (slot == null || spawnpoint == null) return;

//        // Release any existing dragon seat first
//        if (_currentSeat != null)
//        {
//            var dc = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            dc?.PerformDismount();
//        }

//        // Release any existing horse seat first
//        if (_currentHorseSeat != null)
//        {
//            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//            hc?.PerformDismount();
//        }

//        // Release previous cannon slot without notifying (we're already moving)
//        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//            _currentCannonSlot.ReleaseSoldier(notify: false);

//        _currentCannonSlot = slot;

//        // Reparent to the cannon's SoldierSpawnpoint
//        transform.SetParent(spawnpoint, worldPositionStays: false);
//        _rect.anchoredPosition = Vector2.zero;
//        _rect.localScale = Vector3.one;

//        // Record this position as home so SnapBack() returns here
//        RecordHome();
//        _spawnAnchoredPosition = _rect.anchoredPosition;

//        // Restore visuals and stop patrol
//        ShowOwnVisuals();
//        _animator?.SetState(AnimationState.Idle);
//        _controller?.ExitRidingState();

//        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//    }

//    /// <summary>
//    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//    /// Snaps the soldier back to their original home position.
//    /// </summary>
//    public void RemoveFromCannonSlot()
//    {
//        if (_currentCannonSlot == null) return;
//        _currentCannonSlot = null;
//        SnapBack();
//        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
//        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
//        SaveVisualLayerSnapshot();

//        EnsureHelmetEquipped();

//        _controller?.EnterRidingState();

//        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
//        // the Rider GameObject, inserted directly after it in the hierarchy.
//        Transform dragonRoot = seat.transform.parent != null
//            ? seat.transform.parent
//            : seat.transform;

//        transform.SetParent(dragonRoot, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;

//        // Place the soldier right after the Rider child in sibling order.
//        // Find the Rider by name; fall back to SetAsLastSibling if not found.
//        int riderIndex = -1;
//        for (int i = 0; i < dragonRoot.childCount; i++)
//        {
//            if (dragonRoot.GetChild(i).name == "Rider")
//            {
//                riderIndex = i;
//                break;
//            }
//        }

//        if (riderIndex >= 0)
//            transform.SetSiblingIndex(riderIndex + 1);
//        else
//            transform.SetAsLastSibling();

//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the soldier visible on top of the dragon.)
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
//                  $"at sibling index {transform.GetSiblingIndex()}.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        DragonController riderDragonDC = null;
//        if (_currentSeat != null)
//        {
//            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        if (_mountHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//                             "recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            RestoreVisualLayerSnapshot();
//            ShowOwnVisuals();
//            SnapBack();
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        gameObject.SetActive(true);
//        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
//        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
//        // already calls StartWalking() which sets AnimationState.Walk on the
//        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
//        // to standing still instead of resuming patrol walking.
//        RestoreVisualLayerSnapshot();
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHomeParent = null;

//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton.
//    /// </summary>
//    public void SetLocked(bool locked)
//    {
//        if (_currentSeat == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//            return;
//        }

//        _isLocked = locked;

//        if (locked)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.interactable = false;
//        }
//        else
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.interactable = true;
//        }

//        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    // ══════════════════════════════════════════════════════════════════════════
//    // LAYER SNAPSHOT (dragon mount / dismount)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Records the enabled/disabled state of every visual layer at the moment
//    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
//    /// so the soldier returns with exactly the outfit it had before mounting.
//    /// </summary>
//    private void SaveVisualLayerSnapshot()
//    {
//        if (_equipment == null) return;

//        // Record exactly which slots have an item equipped right now.
//        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
//        // helmet/armor will be absent from the set and removed on dismount.
//        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (_equipment.GetEquipped(slot) != null)
//                _preMountEquippedSlots.Add(slot);
//        }

//        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
//                  + string.Join(", ", _preMountEquippedSlots));
//    }

//    /// <summary>
//    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
//    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
//    /// </summary>
//    private void RestoreVisualLayerSnapshot()
//    {
//        if (_equipment == null || _preMountEquippedSlots == null) return;

//        // Unequip any slot that was NOT equipped before mounting.
//        // This removes items auto-added by EnsureHelmetEquipped() so the
//        // soldier returns with exactly the outfit it had before the drag.
//        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
//        {
//            if (!_preMountEquippedSlots.Contains(slot)
//                && _equipment.GetEquipped(slot) != null)
//            {
//                _equipment.Unequip(slot);
//                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
//            }
//        }

//        _preMountEquippedSlots = null;
//        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
//    }

//    public void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnsureHelmetEquipped()
//    {
//        if (_equipment == null) return;
//        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//        EquipmentItem helmet = helmetTable != null
//            ? helmetTable.GetDefaultHelmet(armor)
//            : null;

//        if (helmet != null)
//        {
//            _equipment.Equip(helmet);
//            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//        }
//        else
//        {
//            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//                             $"armor '{armor?.itemName ?? "none"}'. " +
//                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPER
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
//    ///
//    /// The soldier is reparented under the seat so it moves with the horse.
//    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
//    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
//    ///
//    /// FIX: EnterRidingState() is intentionally NOT called here.
//    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
//    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
//    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
//    /// If the soldier's CharacterVisuals image fields are wired to the same Image
//    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
//    /// those images get disabled right before HorseController.PerformMount calls
//    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
//    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
//    /// so no animator state change is needed.
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        _mountHorseHomeParent = _homeParent;
//        _mountHorseHomePos = _homeAnchoredPosition;
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();

//        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
//        _controller?.SetPatrolling(false);

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        // Guard: only proceed if this soldier was actually on a horse.
//        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
//        // was called before this (e.g. during a slot→walkzone transfer).
//        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
//        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
//        if (!wasMounted) return;

//        // Clear the seat reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground and restores its own visuals.
//    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
//    /// </summary>
//    public void DismountFromHorse()
//    {
//        if (_currentHorseSeat != null)
//        {
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//        }

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
//                             "— snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.SetPatrolling(true);
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _animator?.SetState(AnimationState.Idle);
//        _controller?.SetPatrolling(true);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
//    }

//    public void BecomeArcher(ArcherZoneCastle zone)
//    {
//        if (zone == null) return;

//        _currentArcherZone = zone;
//        _isLocked = true;

//        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
//        gameObject.SetActive(false);

//        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
//    }

//    /// <summary>
//    /// Called by ArcherZoneCastle.RemoveArcher().
//    /// Re-enables this soldier and snaps them back to their walk zone.
//    /// </summary>
//    public void ReturnFromArcher()
//    {
//        _currentArcherZone = null;
//        _isLocked = false;

//        // Re-show the soldier.
//        gameObject.SetActive(true);

//        // Snap back to the home position recorded before the last drag.
//        if (_homeParent != null)
//        {
//            transform.SetParent(_homeParent, worldPositionStays: false);
//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
//        }

//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
//    }

//    public void ClearHorseSeatForTransfer()
//    {
//        // Null out the seat so IsOnHorse returns false.
//        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
//        // Do NOT call ExitRidingState — the soldier stays in riding state
//        // so EnterRidingState / SetActive(false) in the next PerformMount
//        // runs cleanly without a redundant coroutine restart.
//        _currentHorseSeat = null;
//        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
//    }
//}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dragon Mount Settings")]
    [Tooltip("Maps each armor to its default helmet.\n" +
             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
    [SerializeField] private ArmorHelmetTable helmetTable;

    // ── Component References ──────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;    // optional — patrol + flip
    private CharacterEquipment _equipment;    // tracks equipped items
    private SpriteLayerAnimator _animator;    // drives per-layer animation
    private CharacterVisuals _visuals;        // direct layer image access

    // Snapshot of which equipment slots were occupied BEFORE dragon mount
    // (before EnsureHelmetEquipped may auto-add items). On dismount, any
    // slot not in this set is unequipped so the soldier returns with exactly
    // the outfit it had before mounting.
    private HashSet<EquipmentSlot> _preMountEquippedSlots;

    // ── Drag State ────────────────────────────────────────────────────────────

    private Canvas _rootCanvas;
    private int _preDragSiblingIndex;  // sibling order before drag, restored on SnapBack
    private Transform _dragOriginalParent; // parent before drag reparent
    private Transform _homeParent;
    private Vector2 _homeAnchoredPosition;

    // Fixed spawn position inside the VillageSoldierSlot — recorded once in
    // Start() and never updated by patrol. SnapBack() uses this so the soldier
    // always returns to the same spot regardless of where it was patrolling
    // when the player grabbed it.
    private Vector2 _spawnAnchoredPosition;
    private bool _isDragging;

    // ── Dragon Rider State ────────────────────────────────────────────────────

    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
    private DragonRiderSeat _currentSeat;

    /// <summary>
    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
    /// return the soldier to its patrol area, not back to the seat.
    /// </summary>
    private Transform _mountHomeParent;
    private Vector2 _mountHomePos;

    // ── Lock State ────────────────────────────────────────────────────────────

    private bool _isLocked = false;

    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
    public bool IsRiding => _currentSeat != null;

    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
    public bool IsLocked => _isLocked;

    // ── Cannon Slot State ─────────────────────────────────────────────────────

    /// <summary>
    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
    /// </summary>
    public static SoldierDragDrop CurrentlyDragging { get; private set; }

    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
    private CannonSlot _currentCannonSlot;

    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
    public bool IsAtCannon => _currentCannonSlot != null;

    // ── Horse Rider State ─────────────────────────────────────────────────────

    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
    private HorseSeat _currentHorseSeat;

    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
    private Transform _mountHorseHomeParent;

    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
    private Vector2 _mountHorseHomePos;

    /// <summary>True while this soldier is seated on a horse.</summary>
    public bool IsOnHorse => _currentHorseSeat != null;

    // ── Archer Zone State ─────────────────────────────────────────────────────

    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
    private ArcherZoneCastle _currentArcherZone;

    /// <summary>True while this soldier is assigned to an archer zone.</summary>
    public bool IsArcher => _currentArcherZone != null;

    // ── Archer Slot State (castle panel archer slots) ──────────────────────────

    /// <summary>The ArcherSlot this soldier is currently stationed on. Null = not stationed.</summary>
    private ArcherSlot _currentArcherSlot;

    /// <summary>True while this soldier is stationed on a castle archer slot.</summary>
    public bool IsOnArcherSlot => _currentArcherSlot != null;

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _controller = GetComponent<SoldierController>();   // optional
        _equipment = GetComponent<CharacterEquipment>();
        _animator = GetComponent<SpriteLayerAnimator>();
        _visuals = GetComponent<CharacterVisuals>();

        if (_equipment == null)
            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
                             $"'{name}'. Helmet auto-equip will be skipped.", this);
        if (_animator == null)
            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
                             $"'{name}'. Riding animation will not play.", this);
        if (helmetTable == null)
            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
                             $"'{name}'. Soldiers will mount without a helmet.", this);
    }

    private void Start()
    {
        RecordHome();
        // Capture spawn position once; SnapBack() uses this fixed value.
        _spawnAnchoredPosition = _rect.anchoredPosition;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — BEGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        // Locked to the dragon — drag disabled until Attach is toggled off.
        if (_isLocked) return;

        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
        CurrentlyDragging = this;

        // ── Release cannon slot if stationed there ────────────────────────────
        if (_currentCannonSlot != null)
        {
            _currentCannonSlot.ReleaseSoldier(notify: false);
            _currentCannonSlot = null;
        }

        // ── Capture mounted dragon before clearing the seat ───────────────────
        bool wasMounted = _currentSeat != null;
        DragonController mountedDragonDC = null;

        if (wasMounted)
        {
            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
            _animator?.SetState(AnimationState.Idle);
        }

        // ── Release horse seat if stationed there ─────────────────────────────
        bool wasOnHorse = _currentHorseSeat != null;
        HorseController mountedHorseHC = null;

        if (wasOnHorse)
        {
            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
            _animator?.SetState(AnimationState.Idle);
        }

        // Find the canvas this soldier lives in (for scaleFactor in OnDrag).
        _rootCanvas = GetComponentInParent<Canvas>();

        if (!wasMounted && !wasOnHorse)
            _spawnAnchoredPosition = _rect.anchoredPosition;
        RecordHome();

        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;
        }

        if (wasOnHorse && _mountHorseHomeParent != null)
        {
            _homeParent = _mountHorseHomeParent;
            _homeAnchoredPosition = _mountHorseHomePos;
            _mountHorseHomeParent = null;
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // Restore own visuals before reparenting (in case coming from dragon or horse).
        if (wasMounted || wasOnHorse)
        {
            gameObject.SetActive(true);   // re-enable if disabled by dragon or horse mount
            ShowOwnVisuals();
        }

        // Store sibling order so SnapBack can restore it.
        _dragOriginalParent = transform.parent;
        _preDragSiblingIndex = transform.GetSiblingIndex();

        // Lift to top of siblings so it renders above panel contents.
        // No reparent -- soldier stays inside VillageSoldierSlot.
        transform.SetAsLastSibling();

        _canvasGroup.alpha = 0.75f;
        _canvasGroup.blocksRaycasts = false;

        // Notify dragon to hide its rider visual.
        if (wasMounted)
            mountedDragonDC?.PerformDismount();

        // Notify horse to hide rider layers and reset to Idle.
        if (wasOnHorse)
            mountedHorseHC?.PerformDismount();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — MOVE
    // ══════════════════════════════════════════════════════════════════════════

    public void OnDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;
        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — END
    // ══════════════════════════════════════════════════════════════════════════

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        // ── HORSE GUARD ───────────────────────────────────────────────────────
        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
        // already mounted this soldier. If we let OnEndDrag continue it would:
        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
        //   • Find no free horse (seat is now occupied) → targetHorse = null
        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
        //     with standing idle animation (the "ghost copy" bug).
        // Early-exit here prevents all of that.
        if (_currentHorseSeat != null)
        {
            CurrentlyDragging = null;
            return;
        }
        // ─────────────────────────────────────────────────────────────────────

        // ── ARCHER SLOT GUARD ─────────────────────────────────────────────────
        // ArcherSlot.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
        // already called StationOnArcherSlot() which sets _currentArcherSlot
        // and does SetActive(false). If we let OnEndDrag continue it would
        // find no valid target and call SnapBack(), re-enabling the soldier
        // as a ghost. Early-exit here prevents that.
        if (_currentArcherSlot != null)
        {
            CurrentlyDragging = null;
            return;
        }
        // ─────────────────────────────────────────────────────────────────────

        // ── ARCHER ZONE GUARD ─────────────────────────────────────────────────
        // ArcherZoneCastle.OnDrop fires BEFORE OnEndDrag and has already called
        // BecomeArcher() which sets _currentArcherZone and does SetActive(false).
        // Without this guard, OnEndDrag falls through to SnapBack() which calls
        // ShowOwnVisuals() and re-enables the soldier as a ghost.
        if (_currentArcherZone != null)
        {
            CurrentlyDragging = null;
            return;
        }
        // ─────────────────────────────────────────────────────────────────────

        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
        // CanvasGroup does not shadow targets sitting underneath.

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragonRiderSeat targetSeat = null;
        DragonController targetDC = null;
        CannonSlot targetCannon = null;
        HorseController targetHorse = null;

        foreach (var r in results)
        {
            // ── Check for cannon slot ─────────────────────────────────────────
            if (targetCannon == null)
                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

            // ── Check for dragon ──────────────────────────────────────────────
            if (targetDC == null)
            {
                var dragon = r.gameObject.GetComponentInParent<DragonController>();
                if (dragon != null)
                {
                    targetDC = dragon;
                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
                }
            }

            // ── Check for horse ───────────────────────────────────────────────
            if (targetHorse == null)
            {
                var hc = r.gameObject.GetComponentInParent<HorseController>();
                if (hc != null && !hc.IsOccupied)
                    targetHorse = hc;
            }

            if (targetCannon != null && targetDC != null && targetHorse != null) break;
        }

        _canvasGroup.blocksRaycasts = true;

        // ── Clear the static drag reference ───────────────────────────────────
        CurrentlyDragging = null;

        // ── Cannon slot drop ──────────────────────────────────────────────────
        if (targetCannon != null)
        {
            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
            ShowOwnVisuals();
            _controller?.SetPatrolling(false);
            return;
        }

        // ── Horse drop ────────────────────────────────────────────────────────
        // Alpha is intentionally NOT restored to 1 before this call.
        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
        // If we set alpha=1 first, the soldier flashes visible for one frame
        // and if the IDropHandler path already ran, we get the duplicate visual.
        if (targetHorse != null)
        {
            _mountHorseHomeParent = _homeParent;
            _mountHorseHomePos = _homeAnchoredPosition;
            targetHorse.PerformMount(this);
            return;
        }

        // ── Dragon drop ───────────────────────────────────────────────────────
        // Alpha is NOT restored before mount — DragonController.PerformMount
        // calls HideOwnVisuals() AFTER ShowForSoldier() so the soldier is
        // guaranteed to be hidden after all animator calls complete.
        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

        if (targetDC != null && targetSeat != null && seatFree)
        {
            _mountHomeParent = _homeParent;
            _mountHomePos = _homeAnchoredPosition;
            targetDC.PerformMount(this, targetSeat);
            return;
        }
        else if (targetSeat != null && targetSeat.IsOccupied)
        {
            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

            if (currentRider != null && currentRider._isLocked)
            {
                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
                _canvasGroup.alpha = 1f;
                SnapBack();
            }
            else if (currentRider != null)
            {
                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
                _mountHomeParent = _homeParent;
                _mountHomePos = _homeAnchoredPosition;
                currentRider.DismountFromDragon();
                targetDC.PerformMount(this, targetSeat);
                return;
            }
            else
            {
                _canvasGroup.alpha = 1f;
                SnapBack();
            }
        }
        else
        {
            // No valid drop target — restore alpha and snap back.
            _canvasGroup.alpha = 1f;
            SnapBack();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DROP OUTCOMES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
    public void SnapBack()
    {
        if (_homeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] SnapBack: _homeParent is null — cannot snap.");
            ShowOwnVisuals();
            _controller?.SetPatrolling(true);
            return;
        }

        // worldPositionStays: true preserves the world position on reparent,
        // then we apply the fixed spawn anchor to land at the correct slot position.
        transform.SetParent(_homeParent, worldPositionStays: true);
        _rect.anchoredPosition = _spawnAnchoredPosition;
        // Restore the original sibling order so rendering order is unchanged.
        if (_dragOriginalParent == _homeParent)
            transform.SetSiblingIndex(_preDragSiblingIndex);
        ShowOwnVisuals();
        _controller?.SetPatrolling(true);
        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by a drop target after accepting the soldier.
    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
    /// </summary>
    public void OnSuccessfulDrop()
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _controller?.SetPatrolling(false);
        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
    }

    /// <summary>
    /// Re-parents the soldier to spawnParent and resumes patrol.
    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
    /// </summary>
    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
    {
        if (spawnParent == null)
        {
            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
            return;
        }

        transform.SetParent(spawnParent, worldPositionStays: true);

        if (spawnPosition.HasValue)
            _rect.anchoredPosition = spawnPosition.Value;

        _canvasGroup.blocksRaycasts = true;
        _isDragging = false;

        ShowOwnVisuals();
        RecordHome();
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CANNON SLOT MOUNT / RELEASE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
    /// cannon's SoldierSpawnpoint and records the new home.
    ///
    /// Safe to call whether the soldier was at a dragon, another cannon slot,
    /// or the ground spawn area.
    /// </summary>
    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
    {
        if (slot == null || spawnpoint == null) return;

        // Release any existing dragon seat first
        if (_currentSeat != null)
        {
            var dc = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
            dc?.PerformDismount();
        }

        // Release any existing horse seat first
        if (_currentHorseSeat != null)
        {
            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
            hc?.PerformDismount();
        }

        // Release previous cannon slot without notifying (we're already moving)
        if (_currentCannonSlot != null && _currentCannonSlot != slot)
            _currentCannonSlot.ReleaseSoldier(notify: false);

        _currentCannonSlot = slot;

        // Reparent to the cannon's SoldierSpawnpoint
        transform.SetParent(spawnpoint, worldPositionStays: false);
        _rect.anchoredPosition = Vector2.zero;
        _rect.localScale = Vector3.one;

        // Record this position as home so SnapBack() returns here
        RecordHome();
        _spawnAnchoredPosition = _rect.anchoredPosition;

        // Restore visuals and stop patrol
        ShowOwnVisuals();
        _animator?.SetState(AnimationState.Idle);
        _controller?.ExitRidingState();

        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
    }

    /// <summary>
    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
    /// Snaps the soldier back to their original home position.
    /// </summary>
    public void RemoveFromCannonSlot()
    {
        if (_currentCannonSlot == null) return;
        _currentCannonSlot = null;
        SnapBack();
        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
    /// </summary>
    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
    {
        _currentSeat = seat;

        // Snapshot which layers are currently visible BEFORE EnsureHelmetEquipped()
        // may add a helmet. This preserves the soldier's pre-mount outfit exactly.
        SaveVisualLayerSnapshot();

        EnsureHelmetEquipped();

        _controller?.EnterRidingState();

        // Parent the soldier INSIDE the DragonRide(Clone) root as a sibling of
        // the Rider GameObject, inserted directly after it in the hierarchy.
        Transform dragonRoot = seat.transform.parent != null
            ? seat.transform.parent
            : seat.transform;

        transform.SetParent(dragonRoot, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        _rect.localScale = Vector3.one;

        // Place the soldier right after the Rider child in sibling order.
        // Find the Rider by name; fall back to SetAsLastSibling if not found.
        int riderIndex = -1;
        for (int i = 0; i < dragonRoot.childCount; i++)
        {
            if (dragonRoot.GetChild(i).name == "Rider")
            {
                riderIndex = i;
                break;
            }
        }

        if (riderIndex >= 0)
            transform.SetSiblingIndex(riderIndex + 1);
        else
            transform.SetAsLastSibling();

        RecordHome();

        // DO NOT call HideOwnVisuals() here.
        // DragonController.PerformMount() calls it explicitly AFTER ShowForSoldier()
        // so the hide runs last and is never overridden by the animator.
        // (Calling it here caused alpha to be reset to 1 by SetState on the
        // next frame, leaving the soldier visible on top of the dragon.)
        _animator?.SetState(AnimationState.RiderIdle);

        Debug.Log($"[SoldierDragDrop] '{name}' mounted inside '{dragonRoot.name}' " +
                  $"at sibling index {transform.GetSiblingIndex()}.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
    /// </summary>
    public void DismountFromDragon()
    {
        DragonController riderDragonDC = null;
        if (_currentSeat != null)
        {
            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
        }

        if (_mountHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
                             "recorded — snapping to current home.");
            gameObject.SetActive(true);
            RestoreVisualLayerSnapshot();
            ShowOwnVisuals();
            SnapBack();
            riderDragonDC?.PerformDismount();
            return;
        }

        transform.SetParent(_mountHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHomePos;

        gameObject.SetActive(true);
        _controller?.ExitRidingState();   // sets Walk animation via StartWalking()
        // NOTE: do NOT call _animator.SetState(Idle) here — ExitRidingState()
        // already calls StartWalking() which sets AnimationState.Walk on the
        // SpriteLayerAnimator.  Calling Idle after would snap the soldier back
        // to standing still instead of resuming patrol walking.
        RestoreVisualLayerSnapshot();
        ShowOwnVisuals();

        RecordHome();
        _mountHomeParent = null;

        riderDragonDC?.PerformDismount();

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ATTACH LOCK
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Locks or unlocks the soldier to the current dragon seat.
    /// Called by DragonAttachButton.
    /// </summary>
    public void SetLocked(bool locked)
    {
        if (_currentSeat == null)
        {
            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
            return;
        }

        _isLocked = locked;

        if (locked)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        else
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // VISUAL SHOW / HIDE
    // ══════════════════════════════════════════════════════════════════════════

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER SNAPSHOT (dragon mount / dismount)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records the enabled/disabled state of every visual layer at the moment
    /// the soldier mounts the dragon. Restored by RestoreVisualLayerSnapshot()
    /// so the soldier returns with exactly the outfit it had before mounting.
    /// </summary>
    private void SaveVisualLayerSnapshot()
    {
        if (_equipment == null) return;

        // Record exactly which slots have an item equipped right now.
        // EnsureHelmetEquipped() runs AFTER this, so any auto-added
        // helmet/armor will be absent from the set and removed on dismount.
        _preMountEquippedSlots = new HashSet<EquipmentSlot>();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (_equipment.GetEquipped(slot) != null)
                _preMountEquippedSlots.Add(slot);
        }

        Debug.Log($"[SoldierDragDrop] Pre-mount equipment snapshot: "
                  + string.Join(", ", _preMountEquippedSlots));
    }

    /// <summary>
    /// Re-applies the layer enabled states that were captured by SaveVisualLayerSnapshot().
    /// Called on dismount so the soldier's outfit is exactly as it was before mounting.
    /// </summary>
    private void RestoreVisualLayerSnapshot()
    {
        if (_equipment == null || _preMountEquippedSlots == null) return;

        // Unequip any slot that was NOT equipped before mounting.
        // This removes items auto-added by EnsureHelmetEquipped() so the
        // soldier returns with exactly the outfit it had before the drag.
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (!_preMountEquippedSlots.Contains(slot)
                && _equipment.GetEquipped(slot) != null)
            {
                _equipment.Unequip(slot);
                Debug.Log($"[SoldierDragDrop] Unequipped '{slot}' — not part of pre-mount outfit.");
            }
        }

        _preMountEquippedSlots = null;
        Debug.Log("[SoldierDragDrop] Pre-mount outfit restored.");
    }

    public void HideOwnVisuals()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    private void ShowOwnVisuals()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELMET AUTO-EQUIP
    // ══════════════════════════════════════════════════════════════════════════

    private void EnsureHelmetEquipped()
    {
        if (_equipment == null) return;
        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
        EquipmentItem helmet = helmetTable != null
            ? helmetTable.GetDefaultHelmet(armor)
            : null;

        if (helmet != null)
        {
            _equipment.Equip(helmet);
            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
        }
        else
        {
            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
                             $"armor '{armor?.itemName ?? "none"}'. " +
                             "Set fallbackHelmet in ArmorHelmetTable.", this);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPER
    // ══════════════════════════════════════════════════════════════════════════

    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HORSE MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
    ///
    /// The soldier is reparented under the seat so it moves with the horse.
    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
    ///
    /// FIX: EnterRidingState() is intentionally NOT called here.
    /// EnterRidingState calls _spriteAnim.SetState(RiderIdle), which calls
    /// _visuals.SetSprite(slot, null) for any slot that has no riderIdle sprites.
    /// CharacterVisuals.SetSprite(null) sets img.enabled = false.
    /// If the soldier's CharacterVisuals image fields are wired to the same Image
    /// components that HorseRiderVisual uses on SoldierSeat (Face/Helmet/Weapon/Armor),
    /// those images get disabled right before HorseController.PerformMount calls
    /// ShowRider() — and then WalkCycleRoutine.SetIdle() disables them again every
    /// idle cycle. The soldier is SetActive(false) immediately after mounting anyway,
    /// so no animator state change is needed.
    /// </summary>
    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
    {
        // Only record home on the FIRST mount.
        // On a transfer (walk-zone horse → slot horse), _mountHorseHomeParent is
        // already set to the soldier's original walk-zone parent. Overwriting it
        // here would capture the old horse's seat transform, which gets destroyed,
        // so DismountFromHorse() would try to reparent into a null/dead object.
        if (_mountHorseHomeParent == null)
        {
            _mountHorseHomeParent = _homeParent;
            _mountHorseHomePos = _homeAnchoredPosition;
        }
        _currentHorseSeat = seat;

        EnsureHelmetEquipped();

        // Stop patrol only — do NOT call EnterRidingState() (see summary above).
        _controller?.SetPatrolling(false);

        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
        transform.SetParent(seat.transform, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        _rect.localScale = Vector3.one;
        RecordHome();

        // DO NOT call HideOwnVisuals() here.
        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
        // so the hide runs last and is never overridden by the animator.

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Emergency return path called by HorseController.OnDestroy() (or its
    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
    /// </summary>
    public void ReturnHomeFromDestroyedHorse()
    {
        // Guard: only proceed if this soldier was actually on a horse.
        // _currentHorseSeat may already be null if ClearHorseSeatForTransfer()
        // was called before this (e.g. during a slot→walkzone transfer).
        // Use _mountHorseHomeParent as the secondary signal that a mount happened.
        bool wasMounted = _currentHorseSeat != null || _mountHorseHomeParent != null;
        if (!wasMounted) return;

        // Clear the seat reference without calling back into the dying seat.
        _currentHorseSeat = null;

        if (_mountHorseHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
                             "no mount home recorded — snapping to current home.");
            gameObject.SetActive(true);
            _animator?.SetState(AnimationState.Idle);
            _controller?.SetPatrolling(true);
            ShowOwnVisuals();
            SnapBack();
            return;
        }

        // Re-enable before reparenting so the soldier is visible on the ground.
        gameObject.SetActive(true);
        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHorseHomePos;

        _animator?.SetState(AnimationState.Idle);
        _controller?.SetPatrolling(true);
        ShowOwnVisuals();

        RecordHome();
        _mountHorseHomeParent = null;

        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
    }

    /// <summary>
    /// Returns the soldier to the ground and restores its own visuals.
    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
    /// </summary>
    public void DismountFromHorse()
    {
        if (_currentHorseSeat != null)
        {
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
        }

        if (_mountHorseHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
                             "— snapping to current home.");
            gameObject.SetActive(true);
            _animator?.SetState(AnimationState.Idle);
            _controller?.SetPatrolling(true);
            ShowOwnVisuals();
            SnapBack();
            return;
        }

        // Re-enable before reparenting so the soldier is visible on the ground.
        gameObject.SetActive(true);
        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHorseHomePos;

        _animator?.SetState(AnimationState.Idle);
        _controller?.SetPatrolling(true);
        ShowOwnVisuals();

        RecordHome();
        _mountHorseHomeParent = null;

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
    }

    public void BecomeArcher(ArcherZoneCastle zone)
    {
        if (zone == null) return;

        _currentArcherZone = zone;
        _isLocked = true;

        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
        gameObject.SetActive(false);

        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
    }

    /// <summary>
    /// Called by ArcherZoneCastle.RemoveArcher().
    /// Re-enables this soldier and snaps them back to their walk zone.
    /// </summary>
    public void ReturnFromArcher()
    {
        // Already returned — nothing to do.
        if (_currentArcherZone == null && !_isLocked)
            return;

        _currentArcherZone = null;
        _isLocked = false;

        // Clear any stale drag state left over from when the soldier was hidden.
        _isDragging = false;
        CurrentlyDragging = null;

        // Re-show the soldier and restore full alpha + raycasts so it can be dragged.
        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        // Snap back to the home position recorded before the last drag.
        if (_homeParent != null)
        {
            transform.SetParent(_homeParent, worldPositionStays: false);
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
        }
        else
        {
            Debug.LogWarning($"[SoldierDragDrop] ReturnFromArcher: '{name}' has no " +
                              "_homeParent recorded — leaving soldier at its current parent.");
        }

        transform.SetAsLastSibling();
        ShowOwnVisuals();

        // SetActive(false) in BecomeArcher kills coroutines — RestartPatrol()
        // does the full relaunch (same fix as RecallFromArcherSlot / horse walk-zone).
        _controller?.RestartPatrol();

        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone — ready to drag.");
    }

    /// <summary>
    /// Called by ArcherSlot when a soldier is dropped onto a castle archer slot.
    /// Locks the soldier and hides it — the ArcherUnit prefab is the visual now.
    /// </summary>
    public void StationOnArcherSlot(ArcherSlot slot)
    {
        // Safety net: _homeParent/_homeAnchoredPosition are normally recorded by
        // RecordHome() inside OnBeginDrag(), which always runs before a soldier
        // can reach an ArcherSlot (ArcherSlot.OnDrop requires CurrentlyDragging).
        // If that's ever bypassed, RecordHome() here guarantees RecallFromArcherSlot()
        // always has a valid place to return the soldier to instead of leaving it
        // wherever it happens to be parented (the "ghost" bug).
        if (_homeParent == null)
            RecordHome();

        _currentArcherSlot = slot;
        _isLocked = true;
        _canvasGroup.blocksRaycasts = true; // keep true — soldier is hidden anyway

        // gameObject.SetActive(false) below kills every coroutine running on this
        // GameObject's components — including SoldierController.RestCycle().
        // RecallFromArcherSlot() restarts patrol via RestartPatrol() to recover
        // from this, so it's safe to disable here.
        gameObject.SetActive(false);
        Debug.Log($"[SoldierDragDrop] '{name}' stationed on archer slot '{slot?.name}'.");
    }

    /// <summary>
    /// Called by ArcherSlot.RemoveArcher() when the remove button is clicked.
    /// Restores the soldier to its pre-drop state so it can be dragged again —
    /// mirrors HorseController.PerformDismount()/EjectRiderBeforeDestroy() for
    /// the horse detach button.
    /// </summary>
    public void RecallFromArcherSlot()
    {
        // Already recalled (or was never stationed) — nothing to do.
        if (_currentArcherSlot == null && !_isLocked)
            return;

        _currentArcherSlot = null;
        _isLocked = false;

        // Clear any leftover drag-in-progress state so OnBeginDrag isn't
        // blocked by a stale _isDragging flag from before the soldier was hidden.
        _isDragging = false;
        CurrentlyDragging = null;

        // Re-show soldier and restore full alpha + raycasts so it can be dragged.
        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        RectTransform rt = GetComponent<RectTransform>();

        // Snap back to the home position recorded before the last drag.
        // Falls back to the soldier's current parent/position (rather than
        // silently doing nothing) if _homeParent was somehow never recorded,
        // so the soldier is never left undraggable inside the archer slot.
        if (_homeParent != null)
        {
            transform.SetParent(_homeParent, worldPositionStays: false);
            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
        }
        else
        {
            Debug.LogWarning($"[SoldierDragDrop] RecallFromArcherSlot: '{name}' has no " +
                              "_homeParent recorded — leaving soldier at its current parent.");
        }

        // Make sure this soldier renders above its siblings again.
        transform.SetAsLastSibling();

        ShowOwnVisuals();

        // SetPatrolling(true) alone does NOT relaunch the RestCycle() coroutine
        // that StationOnArcherSlot's SetActive(false) killed — RestartPatrol()
        // does the full relaunch (same fix pattern as the horse walk-zone fix).
        _controller?.RestartPatrol();

        Debug.Log($"[SoldierDragDrop] '{name}' recalled from archer slot — ready to drag.");
    }

    public void ClearHorseSeatForTransfer()
    {
        // Null out the seat so IsOnHorse returns false.
        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
        // Do NOT call ExitRidingState — the soldier stays in riding state
        // so EnterRidingState / SetActive(false) in the next PerformMount
        // runs cleanly without a redundant coroutine restart.
        _currentHorseSeat = null;
        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
    }
}