//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;
//////[RequireComponent(typeof(CanvasGroup))]
//////public class SoldierDragDrop : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────────────────

//////    [Header("Dragon Mount Settings")]
//////    [Tooltip("Maps each armor to its default helmet.\n" +
//////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
//////    [SerializeField] private ArmorHelmetTable helmetTable;

//////    // ── Component References ──────────────────────────────────────────────────

//////    private CanvasGroup _canvasGroup;
//////    private RectTransform _rect;
//////    private SoldierController _controller;    // optional — patrol + flip
//////    private CharacterEquipment _equipment;    // tracks equipped items
//////    private SpriteLayerAnimator _animator;    // drives per-layer animation

//////    // ── Drag State ────────────────────────────────────────────────────────────

//////    private Canvas _rootCanvas;
//////    private Transform _homeParent;
//////    private Vector2 _homeAnchoredPosition;
//////    private bool _isDragging;

//////    // ── Dragon Rider State ────────────────────────────────────────────────────

//////    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
//////    private DragonRiderSeat _currentSeat;

//////    /// <summary>
//////    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
//////    /// return the soldier to its patrol area, not back to the seat.
//////    /// </summary>
//////    private Transform _mountHomeParent;
//////    private Vector2 _mountHomePos;

//////    // ── Lock State ────────────────────────────────────────────────────────────

//////    private bool _isLocked = false;

//////    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//////    public bool IsRiding => _currentSeat != null;

//////    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//////    public bool IsLocked => _isLocked;

//////    // ── Cannon Slot State ─────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//////    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//////    /// </summary>
//////    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//////    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//////    private CannonSlot _currentCannonSlot;

//////    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//////    public bool IsAtCannon => _currentCannonSlot != null;

//////    // ── Horse Rider State ─────────────────────────────────────────────────────

//////    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
//////    private HorseSeat _currentHorseSeat;

//////    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
//////    private Transform _mountHorseHomeParent;

//////    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
//////    private Vector2 _mountHorseHomePos;

//////    /// <summary>True while this soldier is seated on a horse.</summary>
//////    public bool IsOnHorse => _currentHorseSeat != null;

//////    // ── Archer Zone State ─────────────────────────────────────────────────────

//////    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
//////    private ArcherZoneCastle _currentArcherZone;

//////    /// <summary>True while this soldier is assigned to an archer zone.</summary>
//////    public bool IsArcher => _currentArcherZone != null;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////        _rect = GetComponent<RectTransform>();
//////        _controller = GetComponent<SoldierController>();   // optional
//////        _equipment = GetComponent<CharacterEquipment>();
//////        _animator = GetComponent<SpriteLayerAnimator>();

//////        if (_equipment == null)
//////            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//////                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//////        if (_animator == null)
//////            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//////                             $"'{name}'. Riding animation will not play.", this);
//////        if (helmetTable == null)
//////            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//////                             $"'{name}'. Soldiers will mount without a helmet.", this);
//////    }

//////    private void Start()
//////    {
//////        RecordHome();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — BEGIN
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        if (_isDragging) return;

//////        // Locked to the dragon — drag disabled until Attach is toggled off.
//////        if (_isLocked) return;

//////        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//////        CurrentlyDragging = this;

//////        // ── Release cannon slot if stationed there ────────────────────────────
//////        if (_currentCannonSlot != null)
//////        {
//////            _currentCannonSlot.ReleaseSoldier(notify: false);
//////            _currentCannonSlot = null;
//////        }

//////        // ── Capture mounted dragon before clearing the seat ───────────────────
//////        bool wasMounted = _currentSeat != null;
//////        DragonController mountedDragonDC = null;

//////        if (wasMounted)
//////        {
//////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // ── Release horse seat if stationed there ─────────────────────────────
//////        bool wasOnHorse = _currentHorseSeat != null;
//////        HorseController mountedHorseHC = null;

//////        if (wasOnHorse)
//////        {
//////            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // Re-find root canvas every drag.
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//////        if (_rootCanvas == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//////                           "Make sure the soldier is inside a Canvas.");
//////            CurrentlyDragging = null;
//////            return;
//////        }

//////        // Record home, then override with pre-mount ground position if dismounting.
//////        RecordHome();

//////        if (wasMounted && _mountHomeParent != null)
//////        {
//////            _homeParent = _mountHomeParent;
//////            _homeAnchoredPosition = _mountHomePos;
//////            _mountHomeParent = null;
//////        }

//////        if (wasOnHorse && _mountHorseHomeParent != null)
//////        {
//////            _homeParent = _mountHorseHomeParent;
//////            _homeAnchoredPosition = _mountHorseHomePos;
//////            _mountHorseHomeParent = null;
//////        }

//////        _isDragging = true;
//////        _controller?.SetPatrolling(false);

//////        // Restore own visuals before reparenting (in case coming from dragon or horse).
//////        if (wasMounted || wasOnHorse)
//////        {
//////            gameObject.SetActive(true);   // re-enable if disabled by horse mount
//////            ShowOwnVisuals();
//////        }

//////        // Reparent to root canvas so the soldier draws above all panels.
//////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////        transform.SetAsLastSibling();

//////        _canvasGroup.alpha = 0.75f;
//////        _canvasGroup.blocksRaycasts = false;

//////        // Notify dragon to hide its rider visual.
//////        if (wasMounted)
//////            mountedDragonDC?.PerformDismount();

//////        // Notify horse to hide rider layers and reset to Idle.
//////        if (wasOnHorse)
//////            mountedHorseHC?.PerformDismount();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — MOVE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        if (_rootCanvas == null) return;
//////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — END
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        _isDragging = false;

//////        // ── HORSE GUARD ───────────────────────────────────────────────────────
//////        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
//////        // already mounted this soldier. If we let OnEndDrag continue it would:
//////        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
//////        //   • Find no free horse (seat is now occupied) → targetHorse = null
//////        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
//////        //     with standing idle animation (the "ghost copy" bug).
//////        // Early-exit here prevents all of that.
//////        if (_currentHorseSeat != null)
//////        {
//////            CurrentlyDragging = null;
//////            return;
//////        }
//////        // ─────────────────────────────────────────────────────────────────────

//////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//////        // CanvasGroup does not shadow targets sitting underneath.

//////        var results = new List<RaycastResult>();
//////        EventSystem.current.RaycastAll(eventData, results);

//////        DragonRiderSeat targetSeat = null;
//////        DragonController targetDC = null;
//////        CannonSlot targetCannon = null;
//////        HorseController targetHorse = null;

//////        foreach (var r in results)
//////        {
//////            // ── Check for cannon slot ─────────────────────────────────────────
//////            if (targetCannon == null)
//////                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//////            // ── Check for dragon ──────────────────────────────────────────────
//////            if (targetDC == null)
//////            {
//////                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//////                if (dragon != null)
//////                {
//////                    targetDC = dragon;
//////                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//////                }
//////            }

//////            // ── Check for horse ───────────────────────────────────────────────
//////            if (targetHorse == null)
//////            {
//////                var hc = r.gameObject.GetComponentInParent<HorseController>();
//////                if (hc != null && !hc.IsOccupied)
//////                    targetHorse = hc;
//////            }

//////            if (targetCannon != null && targetDC != null && targetHorse != null) break;
//////        }

//////        _canvasGroup.blocksRaycasts = true;

//////        // ── Clear the static drag reference ───────────────────────────────────
//////        CurrentlyDragging = null;

//////        // ── Cannon slot drop ──────────────────────────────────────────────────
//////        if (targetCannon != null)
//////        {
//////            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//////            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//////            ShowOwnVisuals();
//////            _controller?.SetPatrolling(false);
//////            return;
//////        }

//////        // ── Horse drop ────────────────────────────────────────────────────────
//////        // Alpha is intentionally NOT restored to 1 before this call.
//////        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
//////        // If we set alpha=1 first, the soldier flashes visible for one frame
//////        // and if the IDropHandler path already ran, we get the duplicate visual.
//////        if (targetHorse != null)
//////        {
//////            _mountHorseHomeParent = _homeParent;
//////            _mountHorseHomePos = _homeAnchoredPosition;
//////            targetHorse.PerformMount(this);
//////            return;
//////        }

//////        // Not a horse/cannon drop — safe to restore alpha now.
//////        _canvasGroup.alpha = 1f;

//////        // ── Dragon drop ───────────────────────────────────────────────────────
//////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//////        if (targetDC != null && targetSeat != null && seatFree)
//////        {
//////            _mountHomeParent = _homeParent;
//////            _mountHomePos = _homeAnchoredPosition;
//////            targetDC.PerformMount(this, targetSeat);
//////        }
//////        else if (targetSeat != null && targetSeat.IsOccupied)
//////        {
//////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//////            if (currentRider != null && currentRider._isLocked)
//////            {
//////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//////                SnapBack();
//////            }
//////            else if (currentRider != null)
//////            {
//////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//////                _mountHomeParent = _homeParent;
//////                _mountHomePos = _homeAnchoredPosition;
//////                currentRider.DismountFromDragon();
//////                targetDC.PerformMount(this, targetSeat);
//////            }
//////            else
//////            {
//////                SnapBack();
//////            }
//////        }
//////        else
//////        {
//////            SnapBack();
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DROP OUTCOMES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//////    public void SnapBack()
//////    {
//////        transform.SetParent(_homeParent, worldPositionStays: true);
//////        _rect.anchoredPosition = _homeAnchoredPosition;
//////        ShowOwnVisuals();
//////        _controller?.SetPatrolling(true);
//////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//////    }

//////    /// <summary>
//////    /// Called by a drop target after accepting the soldier.
//////    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//////    /// </summary>
//////    public void OnSuccessfulDrop()
//////    {
//////        _isDragging = false;
//////        _canvasGroup.blocksRaycasts = true;
//////        _controller?.SetPatrolling(false);
//////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//////    }

//////    /// <summary>
//////    /// Re-parents the soldier to spawnParent and resumes patrol.
//////    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//////    /// </summary>
//////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//////    {
//////        if (spawnParent == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//////            return;
//////        }

//////        transform.SetParent(spawnParent, worldPositionStays: true);

//////        if (spawnPosition.HasValue)
//////            _rect.anchoredPosition = spawnPosition.Value;

//////        _canvasGroup.blocksRaycasts = true;
//////        _isDragging = false;

//////        ShowOwnVisuals();
//////        RecordHome();
//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // CANNON SLOT MOUNT / RELEASE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//////    /// cannon's SoldierSpawnpoint and records the new home.
//////    ///
//////    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//////    /// or the ground spawn area.
//////    /// </summary>
//////    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//////    {
//////        if (slot == null || spawnpoint == null) return;

//////        // Release any existing dragon seat first
//////        if (_currentSeat != null)
//////        {
//////            var dc = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            dc?.PerformDismount();
//////        }

//////        // Release any existing horse seat first
//////        if (_currentHorseSeat != null)
//////        {
//////            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////            hc?.PerformDismount();
//////        }

//////        // Release previous cannon slot without notifying (we're already moving)
//////        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//////            _currentCannonSlot.ReleaseSoldier(notify: false);

//////        _currentCannonSlot = slot;

//////        // Reparent to the cannon's SoldierSpawnpoint
//////        transform.SetParent(spawnpoint, worldPositionStays: false);
//////        _rect.anchoredPosition = Vector2.zero;
//////        _rect.localScale = Vector3.one;

//////        // Record this position as home so SnapBack() returns here
//////        RecordHome();

//////        // Restore visuals and stop patrol
//////        ShowOwnVisuals();
//////        _animator?.SetState(AnimationState.Idle);
//////        _controller?.ExitRidingState();

//////        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//////    /// Snaps the soldier back to their original home position.
//////    /// </summary>
//////    public void RemoveFromCannonSlot()
//////    {
//////        if (_currentCannonSlot == null) return;
//////        _currentCannonSlot = null;
//////        SnapBack();
//////        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//////    /// </summary>
//////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//////    {
//////        _currentSeat = seat;

//////        EnsureHelmetEquipped();

//////        _controller?.EnterRidingState();

//////        transform.SetParent(seat.transform, worldPositionStays: false);
//////        _rect.anchoredPosition = seatOffset;
//////        RecordHome();

//////        HideOwnVisuals();
//////        _animator?.SetState(AnimationState.RiderIdle);

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
//////    /// </summary>
//////    public void DismountFromDragon()
//////    {
//////        DragonController riderDragonDC = null;
//////        if (_currentSeat != null)
//////        {
//////            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////        }

//////        if (_mountHomeParent == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//////                             "recorded — snapping to current home.");
//////            ShowOwnVisuals();
//////            SnapBack();
//////            riderDragonDC?.PerformDismount();
//////            return;
//////        }

//////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHomeParent = null;

//////        riderDragonDC?.PerformDismount();

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // ATTACH LOCK
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Locks or unlocks the soldier to the current dragon seat.
//////    /// Called by DragonAttachButton.
//////    /// </summary>
//////    public void SetLocked(bool locked)
//////    {
//////        if (_currentSeat == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//////            return;
//////        }

//////        _isLocked = locked;

//////        if (locked)
//////        {
//////            _canvasGroup.blocksRaycasts = false;
//////            _canvasGroup.interactable = false;
//////        }
//////        else
//////        {
//////            _canvasGroup.blocksRaycasts = true;
//////            _canvasGroup.interactable = true;
//////        }

//////        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//////                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // VISUAL SHOW / HIDE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void HideOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 0f;
//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.interactable = true;
//////    }

//////    private void ShowOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 1f;
//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.interactable = true;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELMET AUTO-EQUIP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnsureHelmetEquipped()
//////    {
//////        if (_equipment == null) return;
//////        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//////        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//////        EquipmentItem helmet = helmetTable != null
//////            ? helmetTable.GetDefaultHelmet(armor)
//////            : null;

//////        if (helmet != null)
//////        {
//////            _equipment.Equip(helmet);
//////            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//////                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//////        }
//////        else
//////        {
//////            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//////                             $"armor '{armor?.itemName ?? "none"}'. " +
//////                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELPER
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void RecordHome()
//////    {
//////        _homeParent = transform.parent;
//////        _homeAnchoredPosition = _rect.anchoredPosition;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HORSE MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
//////    ///
//////    /// The soldier is reparented under the seat so it moves with the horse.
//////    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
//////    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
//////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
//////    /// active causes two visual layers to fight each other (the "soldier above
//////    /// horse" and "duplicate visual" bugs).
//////    /// </summary>
//////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//////    {
//////        _mountHorseHomeParent = _homeParent;
//////        _mountHorseHomePos = _homeAnchoredPosition;
//////        _currentHorseSeat = seat;

//////        EnsureHelmetEquipped();
//////        _controller?.EnterRidingState();

//////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//////        transform.SetParent(seat.transform, worldPositionStays: false);
//////        _rect.anchoredPosition = seatOffset;
//////        _rect.localScale = Vector3.one;
//////        RecordHome();

//////        // DO NOT call HideOwnVisuals() here.
//////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//////        // so the hide runs last and is never overridden by the animator.
//////        // (Calling it here caused alpha to be reset to 1 by SetState on the
//////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Emergency return path called by HorseController.OnDestroy() (or its
//////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//////    ///
//////    /// Unlike DismountFromHorse(), this method does NOT call
//////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
//////    /// and calling back into it would touch destroyed objects. It simply:
//////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
//////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
//////    ///      the home was somehow lost), so the soldier is never left orphaned
//////    ///      as a child of a destroyed horse GameObject.
//////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
//////    ///
//////    /// Call order in HorseController (walk-zone drop or any destruction path):
//////    ///   soldier.ReturnHomeFromDestroyedHorse();
//////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
//////    /// </summary>
//////    public void ReturnHomeFromDestroyedHorse()
//////    {
//////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

//////        // Clear the reference without calling back into the dying seat.
//////        _currentHorseSeat = null;

//////        if (_mountHorseHomeParent == null)
//////        {
//////            // No pre-mount home recorded — snap to wherever home currently points.
//////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//////                             "no mount home recorded — snapping to current home.");
//////            gameObject.SetActive(true);
//////            _animator?.SetState(AnimationState.Idle);
//////            _controller?.ExitRidingState();
//////            ShowOwnVisuals();
//////            SnapBack();
//////            return;
//////        }

//////        // Re-enable before reparenting so the soldier is visible on the ground.
//////        gameObject.SetActive(true);
//////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHorseHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHorseHomeParent = null;

//////        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and restores its own visuals.
//////    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
//////    /// </summary>
//////    public void DismountFromHorse()
//////    {
//////        if (_currentHorseSeat != null)
//////        {
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////        }

//////        if (_mountHorseHomeParent == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
//////                             "— snapping to current home.");
//////            gameObject.SetActive(true);
//////            _animator?.SetState(AnimationState.Idle);
//////            _controller?.ExitRidingState();
//////            ShowOwnVisuals();
//////            SnapBack();
//////            return;
//////        }

//////        // Re-enable before reparenting so the soldier is visible on the ground.
//////        gameObject.SetActive(true);
//////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHorseHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHorseHomeParent = null;

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
//////    }
//////    public void BecomeArcher(ArcherZoneCastle zone)
//////    {
//////        if (zone == null) return;

//////        _currentArcherZone = zone;
//////        _isLocked = true;

//////        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
//////        gameObject.SetActive(false);

//////        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by ArcherZoneCastle.RemoveArcher().
//////    /// Re-enables this soldier and snaps them back to their walk zone.
//////    /// </summary>
//////    public void ReturnFromArcher()
//////    {
//////        _currentArcherZone = null;
//////        _isLocked = false;

//////        // Re-show the soldier.
//////        gameObject.SetActive(true);

//////        // Snap back to the home position recorded before the last drag.
//////        if (_homeParent != null)
//////        {
//////            transform.SetParent(_homeParent, worldPositionStays: false);
//////            RectTransform rt = GetComponent<RectTransform>();
//////            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
//////        }

//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
//////    }

//////    public void ClearHorseSeatForTransfer()
//////    {
//////        // Null out the seat so IsOnHorse returns false.
//////        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
//////        // Do NOT call ExitRidingState — the soldier stays in riding state
//////        // so EnterRidingState / SetActive(false) in the next PerformMount
//////        // runs cleanly without a redundant coroutine restart.
//////        _currentHorseSeat = null;
//////        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
//////    }
//////}

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

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
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

////        // Re-find root canvas every drag.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            CurrentlyDragging = null;
////            return;
////        }

////        // Record home, then override with pre-mount ground position if dismounting.
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

////        // Reparent to root canvas so the soldier draws above all panels.
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _homeAnchoredPosition;
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
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        gameObject.SetActive(true);
////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
////    /// active causes two visual layers to fight each other (the "soldier above
////    /// horse" and "duplicate visual" bugs).
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();
////        _controller?.EnterRidingState();

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    ///
////    /// Unlike DismountFromHorse(), this method does NOT call
////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
////    /// and calling back into it would touch destroyed objects. It simply:
////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
////    ///      the home was somehow lost), so the soldier is never left orphaned
////    ///      as a child of a destroyed horse GameObject.
////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
////    ///
////    /// Call order in HorseController (walk-zone drop or any destruction path):
////    ///   soldier.ReturnHomeFromDestroyedHorse();
////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

////        // Clear the reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            // No pre-mount home recorded — snap to wherever home currently points.
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
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

////        // Re-find root canvas every drag.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            CurrentlyDragging = null;
////            return;
////        }

////        // Record home, then override with pre-mount ground position if dismounting.
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
////            gameObject.SetActive(true);   // re-enable if disabled by horse mount
////            ShowOwnVisuals();
////        }

////        // Reparent to root canvas so the soldier draws above all panels.
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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

////        // Not a horse/cannon drop — safe to restore alpha now.
////        _canvasGroup.alpha = 1f;

////        // ── Dragon drop ───────────────────────────────────────────────────────
////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && targetSeat != null && seatFree)
////        {
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;
////            targetDC.PerformMount(this, targetSeat);
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

////            if (currentRider != null && currentRider._isLocked)
////            {
////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
////                SnapBack();
////            }
////            else if (currentRider != null)
////            {
////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
////                _mountHomeParent = _homeParent;
////                _mountHomePos = _homeAnchoredPosition;
////                currentRider.DismountFromDragon();
////                targetDC.PerformMount(this, targetSeat);
////            }
////            else
////            {
////                SnapBack();
////            }
////        }
////        else
////        {
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DROP OUTCOMES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////    public void SnapBack()
////    {
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _homeAnchoredPosition;
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

////        EnsureHelmetEquipped();

////        _controller?.EnterRidingState();

////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        RecordHome();

////        HideOwnVisuals();
////        _animator?.SetState(AnimationState.RiderIdle);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
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
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

////    private void HideOwnVisuals()
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
////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
////    /// active causes two visual layers to fight each other (the "soldier above
////    /// horse" and "duplicate visual" bugs).
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();
////        _controller?.EnterRidingState();

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    ///
////    /// Unlike DismountFromHorse(), this method does NOT call
////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
////    /// and calling back into it would touch destroyed objects. It simply:
////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
////    ///      the home was somehow lost), so the soldier is never left orphaned
////    ///      as a child of a destroyed horse GameObject.
////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
////    ///
////    /// Call order in HorseController (walk-zone drop or any destruction path):
////    ///   soldier.ReturnHomeFromDestroyedHorse();
////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

////        // Clear the reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            // No pre-mount home recorded — snap to wherever home currently points.
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
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

//        // Re-find root canvas every drag.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            CurrentlyDragging = null;
//            return;
//        }

//        // Record home, then override with pre-mount ground position if dismounting.
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

//        // Reparent to root canvas so the soldier draws above all panels.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _homeAnchoredPosition;
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
//    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
//    /// active causes two visual layers to fight each other (the "soldier above
//    /// horse" and "duplicate visual" bugs).
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        _mountHorseHomeParent = _homeParent;
//        _mountHorseHomePos = _homeAnchoredPosition;
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();
//        _controller?.EnterRidingState();

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the "ghost soldier" visible on top of the horse.)

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    ///
//    /// Unlike DismountFromHorse(), this method does NOT call
//    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
//    /// and calling back into it would touch destroyed objects. It simply:
//    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
//    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
//    ///      the home was somehow lost), so the soldier is never left orphaned
//    ///      as a child of a destroyed horse GameObject.
//    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
//    ///
//    /// Call order in HorseController (walk-zone drop or any destruction path):
//    ///   soldier.ReturnHomeFromDestroyedHorse();
//    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

//        // Clear the reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            // No pre-mount home recorded — snap to wherever home currently points.
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;
//////[RequireComponent(typeof(CanvasGroup))]
//////public class SoldierDragDrop : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────────────────

//////    [Header("Dragon Mount Settings")]
//////    [Tooltip("Maps each armor to its default helmet.\n" +
//////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
//////    [SerializeField] private ArmorHelmetTable helmetTable;

//////    // ── Component References ──────────────────────────────────────────────────

//////    private CanvasGroup _canvasGroup;
//////    private RectTransform _rect;
//////    private SoldierController _controller;    // optional — patrol + flip
//////    private CharacterEquipment _equipment;    // tracks equipped items
//////    private SpriteLayerAnimator _animator;    // drives per-layer animation

//////    // ── Drag State ────────────────────────────────────────────────────────────

//////    private Canvas _rootCanvas;
//////    private Transform _homeParent;
//////    private Vector2 _homeAnchoredPosition;
//////    private bool _isDragging;

//////    // ── Dragon Rider State ────────────────────────────────────────────────────

//////    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
//////    private DragonRiderSeat _currentSeat;

//////    /// <summary>
//////    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
//////    /// return the soldier to its patrol area, not back to the seat.
//////    /// </summary>
//////    private Transform _mountHomeParent;
//////    private Vector2 _mountHomePos;

//////    // ── Lock State ────────────────────────────────────────────────────────────

//////    private bool _isLocked = false;

//////    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//////    public bool IsRiding => _currentSeat != null;

//////    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//////    public bool IsLocked => _isLocked;

//////    // ── Cannon Slot State ─────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//////    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//////    /// </summary>
//////    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//////    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//////    private CannonSlot _currentCannonSlot;

//////    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//////    public bool IsAtCannon => _currentCannonSlot != null;

//////    // ── Horse Rider State ─────────────────────────────────────────────────────

//////    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
//////    private HorseSeat _currentHorseSeat;

//////    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
//////    private Transform _mountHorseHomeParent;

//////    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
//////    private Vector2 _mountHorseHomePos;

//////    /// <summary>True while this soldier is seated on a horse.</summary>
//////    public bool IsOnHorse => _currentHorseSeat != null;

//////    // ── Archer Zone State ─────────────────────────────────────────────────────

//////    /// <summary>The ArcherZoneCastle this soldier is currently assigned to. Null = not an archer.</summary>
//////    private ArcherZoneCastle _currentArcherZone;

//////    /// <summary>True while this soldier is assigned to an archer zone.</summary>
//////    public bool IsArcher => _currentArcherZone != null;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////        _rect = GetComponent<RectTransform>();
//////        _controller = GetComponent<SoldierController>();   // optional
//////        _equipment = GetComponent<CharacterEquipment>();
//////        _animator = GetComponent<SpriteLayerAnimator>();

//////        if (_equipment == null)
//////            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//////                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//////        if (_animator == null)
//////            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//////                             $"'{name}'. Riding animation will not play.", this);
//////        if (helmetTable == null)
//////            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//////                             $"'{name}'. Soldiers will mount without a helmet.", this);
//////    }

//////    private void Start()
//////    {
//////        RecordHome();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — BEGIN
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        if (_isDragging) return;

//////        // Locked to the dragon — drag disabled until Attach is toggled off.
//////        if (_isLocked) return;

//////        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//////        CurrentlyDragging = this;

//////        // ── Release cannon slot if stationed there ────────────────────────────
//////        if (_currentCannonSlot != null)
//////        {
//////            _currentCannonSlot.ReleaseSoldier(notify: false);
//////            _currentCannonSlot = null;
//////        }

//////        // ── Capture mounted dragon before clearing the seat ───────────────────
//////        bool wasMounted = _currentSeat != null;
//////        DragonController mountedDragonDC = null;

//////        if (wasMounted)
//////        {
//////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // ── Release horse seat if stationed there ─────────────────────────────
//////        bool wasOnHorse = _currentHorseSeat != null;
//////        HorseController mountedHorseHC = null;

//////        if (wasOnHorse)
//////        {
//////            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // Re-find root canvas every drag.
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//////        if (_rootCanvas == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//////                           "Make sure the soldier is inside a Canvas.");
//////            CurrentlyDragging = null;
//////            return;
//////        }

//////        // Record home, then override with pre-mount ground position if dismounting.
//////        RecordHome();

//////        if (wasMounted && _mountHomeParent != null)
//////        {
//////            _homeParent = _mountHomeParent;
//////            _homeAnchoredPosition = _mountHomePos;
//////            _mountHomeParent = null;
//////        }

//////        if (wasOnHorse && _mountHorseHomeParent != null)
//////        {
//////            _homeParent = _mountHorseHomeParent;
//////            _homeAnchoredPosition = _mountHorseHomePos;
//////            _mountHorseHomeParent = null;
//////        }

//////        _isDragging = true;
//////        _controller?.SetPatrolling(false);

//////        // Restore own visuals before reparenting (in case coming from dragon or horse).
//////        if (wasMounted || wasOnHorse)
//////        {
//////            gameObject.SetActive(true);   // re-enable if disabled by horse mount
//////            ShowOwnVisuals();
//////        }

//////        // Reparent to root canvas so the soldier draws above all panels.
//////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////        transform.SetAsLastSibling();

//////        _canvasGroup.alpha = 0.75f;
//////        _canvasGroup.blocksRaycasts = false;

//////        // Notify dragon to hide its rider visual.
//////        if (wasMounted)
//////            mountedDragonDC?.PerformDismount();

//////        // Notify horse to hide rider layers and reset to Idle.
//////        if (wasOnHorse)
//////            mountedHorseHC?.PerformDismount();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — MOVE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        if (_rootCanvas == null) return;
//////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — END
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        _isDragging = false;

//////        // ── HORSE GUARD ───────────────────────────────────────────────────────
//////        // HorseController.OnDrop (IDropHandler) fires BEFORE OnEndDrag and has
//////        // already mounted this soldier. If we let OnEndDrag continue it would:
//////        //   • Set _canvasGroup.alpha = 1 (undoing HideOwnVisuals)
//////        //   • Find no free horse (seat is now occupied) → targetHorse = null
//////        //   • Fall through to SnapBack() → ShowOwnVisuals() → soldier reappears
//////        //     with standing idle animation (the "ghost copy" bug).
//////        // Early-exit here prevents all of that.
//////        if (_currentHorseSeat != null)
//////        {
//////            CurrentlyDragging = null;
//////            return;
//////        }
//////        // ─────────────────────────────────────────────────────────────────────

//////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//////        // CanvasGroup does not shadow targets sitting underneath.

//////        var results = new List<RaycastResult>();
//////        EventSystem.current.RaycastAll(eventData, results);

//////        DragonRiderSeat targetSeat = null;
//////        DragonController targetDC = null;
//////        CannonSlot targetCannon = null;
//////        HorseController targetHorse = null;

//////        foreach (var r in results)
//////        {
//////            // ── Check for cannon slot ─────────────────────────────────────────
//////            if (targetCannon == null)
//////                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//////            // ── Check for dragon ──────────────────────────────────────────────
//////            if (targetDC == null)
//////            {
//////                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//////                if (dragon != null)
//////                {
//////                    targetDC = dragon;
//////                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//////                }
//////            }

//////            // ── Check for horse ───────────────────────────────────────────────
//////            if (targetHorse == null)
//////            {
//////                var hc = r.gameObject.GetComponentInParent<HorseController>();
//////                if (hc != null && !hc.IsOccupied)
//////                    targetHorse = hc;
//////            }

//////            if (targetCannon != null && targetDC != null && targetHorse != null) break;
//////        }

//////        _canvasGroup.blocksRaycasts = true;

//////        // ── Clear the static drag reference ───────────────────────────────────
//////        CurrentlyDragging = null;

//////        // ── Cannon slot drop ──────────────────────────────────────────────────
//////        if (targetCannon != null)
//////        {
//////            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//////            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//////            ShowOwnVisuals();
//////            _controller?.SetPatrolling(false);
//////            return;
//////        }

//////        // ── Horse drop ────────────────────────────────────────────────────────
//////        // Alpha is intentionally NOT restored to 1 before this call.
//////        // PerformMount → MountOnHorse → HideOwnVisuals sets alpha=0.
//////        // If we set alpha=1 first, the soldier flashes visible for one frame
//////        // and if the IDropHandler path already ran, we get the duplicate visual.
//////        if (targetHorse != null)
//////        {
//////            _mountHorseHomeParent = _homeParent;
//////            _mountHorseHomePos = _homeAnchoredPosition;
//////            targetHorse.PerformMount(this);
//////            return;
//////        }

//////        // Not a horse/cannon drop — safe to restore alpha now.
//////        _canvasGroup.alpha = 1f;

//////        // ── Dragon drop ───────────────────────────────────────────────────────
//////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//////        if (targetDC != null && targetSeat != null && seatFree)
//////        {
//////            _mountHomeParent = _homeParent;
//////            _mountHomePos = _homeAnchoredPosition;
//////            targetDC.PerformMount(this, targetSeat);
//////        }
//////        else if (targetSeat != null && targetSeat.IsOccupied)
//////        {
//////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//////            if (currentRider != null && currentRider._isLocked)
//////            {
//////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//////                SnapBack();
//////            }
//////            else if (currentRider != null)
//////            {
//////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//////                _mountHomeParent = _homeParent;
//////                _mountHomePos = _homeAnchoredPosition;
//////                currentRider.DismountFromDragon();
//////                targetDC.PerformMount(this, targetSeat);
//////            }
//////            else
//////            {
//////                SnapBack();
//////            }
//////        }
//////        else
//////        {
//////            SnapBack();
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DROP OUTCOMES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//////    public void SnapBack()
//////    {
//////        transform.SetParent(_homeParent, worldPositionStays: true);
//////        _rect.anchoredPosition = _homeAnchoredPosition;
//////        ShowOwnVisuals();
//////        _controller?.SetPatrolling(true);
//////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//////    }

//////    /// <summary>
//////    /// Called by a drop target after accepting the soldier.
//////    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//////    /// </summary>
//////    public void OnSuccessfulDrop()
//////    {
//////        _isDragging = false;
//////        _canvasGroup.blocksRaycasts = true;
//////        _controller?.SetPatrolling(false);
//////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//////    }

//////    /// <summary>
//////    /// Re-parents the soldier to spawnParent and resumes patrol.
//////    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//////    /// </summary>
//////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//////    {
//////        if (spawnParent == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//////            return;
//////        }

//////        transform.SetParent(spawnParent, worldPositionStays: true);

//////        if (spawnPosition.HasValue)
//////            _rect.anchoredPosition = spawnPosition.Value;

//////        _canvasGroup.blocksRaycasts = true;
//////        _isDragging = false;

//////        ShowOwnVisuals();
//////        RecordHome();
//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // CANNON SLOT MOUNT / RELEASE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//////    /// cannon's SoldierSpawnpoint and records the new home.
//////    ///
//////    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//////    /// or the ground spawn area.
//////    /// </summary>
//////    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//////    {
//////        if (slot == null || spawnpoint == null) return;

//////        // Release any existing dragon seat first
//////        if (_currentSeat != null)
//////        {
//////            var dc = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            dc?.PerformDismount();
//////        }

//////        // Release any existing horse seat first
//////        if (_currentHorseSeat != null)
//////        {
//////            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////            hc?.PerformDismount();
//////        }

//////        // Release previous cannon slot without notifying (we're already moving)
//////        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//////            _currentCannonSlot.ReleaseSoldier(notify: false);

//////        _currentCannonSlot = slot;

//////        // Reparent to the cannon's SoldierSpawnpoint
//////        transform.SetParent(spawnpoint, worldPositionStays: false);
//////        _rect.anchoredPosition = Vector2.zero;
//////        _rect.localScale = Vector3.one;

//////        // Record this position as home so SnapBack() returns here
//////        RecordHome();

//////        // Restore visuals and stop patrol
//////        ShowOwnVisuals();
//////        _animator?.SetState(AnimationState.Idle);
//////        _controller?.ExitRidingState();

//////        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//////    /// Snaps the soldier back to their original home position.
//////    /// </summary>
//////    public void RemoveFromCannonSlot()
//////    {
//////        if (_currentCannonSlot == null) return;
//////        _currentCannonSlot = null;
//////        SnapBack();
//////        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//////    /// </summary>
//////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//////    {
//////        _currentSeat = seat;

//////        EnsureHelmetEquipped();

//////        _controller?.EnterRidingState();

//////        transform.SetParent(seat.transform, worldPositionStays: false);
//////        _rect.anchoredPosition = seatOffset;
//////        RecordHome();

//////        HideOwnVisuals();
//////        _animator?.SetState(AnimationState.RiderIdle);

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
//////    /// </summary>
//////    public void DismountFromDragon()
//////    {
//////        DragonController riderDragonDC = null;
//////        if (_currentSeat != null)
//////        {
//////            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////        }

//////        if (_mountHomeParent == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//////                             "recorded — snapping to current home.");
//////            ShowOwnVisuals();
//////            SnapBack();
//////            riderDragonDC?.PerformDismount();
//////            return;
//////        }

//////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHomeParent = null;

//////        riderDragonDC?.PerformDismount();

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // ATTACH LOCK
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Locks or unlocks the soldier to the current dragon seat.
//////    /// Called by DragonAttachButton.
//////    /// </summary>
//////    public void SetLocked(bool locked)
//////    {
//////        if (_currentSeat == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//////            return;
//////        }

//////        _isLocked = locked;

//////        if (locked)
//////        {
//////            _canvasGroup.blocksRaycasts = false;
//////            _canvasGroup.interactable = false;
//////        }
//////        else
//////        {
//////            _canvasGroup.blocksRaycasts = true;
//////            _canvasGroup.interactable = true;
//////        }

//////        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//////                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // VISUAL SHOW / HIDE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void HideOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 0f;
//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.interactable = true;
//////    }

//////    private void ShowOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 1f;
//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.interactable = true;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELMET AUTO-EQUIP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnsureHelmetEquipped()
//////    {
//////        if (_equipment == null) return;
//////        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//////        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//////        EquipmentItem helmet = helmetTable != null
//////            ? helmetTable.GetDefaultHelmet(armor)
//////            : null;

//////        if (helmet != null)
//////        {
//////            _equipment.Equip(helmet);
//////            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//////                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//////        }
//////        else
//////        {
//////            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//////                             $"armor '{armor?.itemName ?? "none"}'. " +
//////                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELPER
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void RecordHome()
//////    {
//////        _homeParent = transform.parent;
//////        _homeAnchoredPosition = _rect.anchoredPosition;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HORSE MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
//////    ///
//////    /// The soldier is reparented under the seat so it moves with the horse.
//////    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
//////    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
//////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
//////    /// active causes two visual layers to fight each other (the "soldier above
//////    /// horse" and "duplicate visual" bugs).
//////    /// </summary>
//////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//////    {
//////        _mountHorseHomeParent = _homeParent;
//////        _mountHorseHomePos = _homeAnchoredPosition;
//////        _currentHorseSeat = seat;

//////        EnsureHelmetEquipped();
//////        _controller?.EnterRidingState();

//////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//////        transform.SetParent(seat.transform, worldPositionStays: false);
//////        _rect.anchoredPosition = seatOffset;
//////        _rect.localScale = Vector3.one;
//////        RecordHome();

//////        // DO NOT call HideOwnVisuals() here.
//////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//////        // so the hide runs last and is never overridden by the animator.
//////        // (Calling it here caused alpha to be reset to 1 by SetState on the
//////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Emergency return path called by HorseController.OnDestroy() (or its
//////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//////    ///
//////    /// Unlike DismountFromHorse(), this method does NOT call
//////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
//////    /// and calling back into it would touch destroyed objects. It simply:
//////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
//////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
//////    ///      the home was somehow lost), so the soldier is never left orphaned
//////    ///      as a child of a destroyed horse GameObject.
//////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
//////    ///
//////    /// Call order in HorseController (walk-zone drop or any destruction path):
//////    ///   soldier.ReturnHomeFromDestroyedHorse();
//////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
//////    /// </summary>
//////    public void ReturnHomeFromDestroyedHorse()
//////    {
//////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

//////        // Clear the reference without calling back into the dying seat.
//////        _currentHorseSeat = null;

//////        if (_mountHorseHomeParent == null)
//////        {
//////            // No pre-mount home recorded — snap to wherever home currently points.
//////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//////                             "no mount home recorded — snapping to current home.");
//////            gameObject.SetActive(true);
//////            _animator?.SetState(AnimationState.Idle);
//////            _controller?.ExitRidingState();
//////            ShowOwnVisuals();
//////            SnapBack();
//////            return;
//////        }

//////        // Re-enable before reparenting so the soldier is visible on the ground.
//////        gameObject.SetActive(true);
//////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHorseHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHorseHomeParent = null;

//////        Debug.Log($"[SoldierDragDrop] '{name}' returned home after horse was destroyed.");
//////    }

//////    /// <summary>
//////    /// Returns the soldier to the ground and restores its own visuals.
//////    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
//////    /// </summary>
//////    public void DismountFromHorse()
//////    {
//////        if (_currentHorseSeat != null)
//////        {
//////            _currentHorseSeat.ReleaseSoldier();
//////            _currentHorseSeat = null;
//////        }

//////        if (_mountHorseHomeParent == null)
//////        {
//////            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
//////                             "— snapping to current home.");
//////            gameObject.SetActive(true);
//////            _animator?.SetState(AnimationState.Idle);
//////            _controller?.ExitRidingState();
//////            ShowOwnVisuals();
//////            SnapBack();
//////            return;
//////        }

//////        // Re-enable before reparenting so the soldier is visible on the ground.
//////        gameObject.SetActive(true);
//////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHorseHomePos;

//////        _controller?.ExitRidingState();
//////        _animator?.SetState(AnimationState.Idle);
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHorseHomeParent = null;

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
//////    }
//////    public void BecomeArcher(ArcherZoneCastle zone)
//////    {
//////        if (zone == null) return;

//////        _currentArcherZone = zone;
//////        _isLocked = true;

//////        // Hide the soldier — the spawned ArcherUnit prefab is the visual now.
//////        gameObject.SetActive(false);

//////        Debug.Log($"[SoldierDragDrop] '{name}' became an archer at '{zone.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by ArcherZoneCastle.RemoveArcher().
//////    /// Re-enables this soldier and snaps them back to their walk zone.
//////    /// </summary>
//////    public void ReturnFromArcher()
//////    {
//////        _currentArcherZone = null;
//////        _isLocked = false;

//////        // Re-show the soldier.
//////        gameObject.SetActive(true);

//////        // Snap back to the home position recorded before the last drag.
//////        if (_homeParent != null)
//////        {
//////            transform.SetParent(_homeParent, worldPositionStays: false);
//////            RectTransform rt = GetComponent<RectTransform>();
//////            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
//////        }

//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
//////    }

//////    public void ClearHorseSeatForTransfer()
//////    {
//////        // Null out the seat so IsOnHorse returns false.
//////        // Do NOT clear _mountHorseHomeParent / _mountHorseHomePos.
//////        // Do NOT call ExitRidingState — the soldier stays in riding state
//////        // so EnterRidingState / SetActive(false) in the next PerformMount
//////        // runs cleanly without a redundant coroutine restart.
//////        _currentHorseSeat = null;
//////        Debug.Log($"[SoldierDragDrop] '{name}' seat cleared for transfer (home preserved).");
//////    }
//////}

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

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
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

////        // Re-find root canvas every drag.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            CurrentlyDragging = null;
////            return;
////        }

////        // Record home, then override with pre-mount ground position if dismounting.
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

////        // Reparent to root canvas so the soldier draws above all panels.
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _homeAnchoredPosition;
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
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        gameObject.SetActive(true);
////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
////    /// active causes two visual layers to fight each other (the "soldier above
////    /// horse" and "duplicate visual" bugs).
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();
////        _controller?.EnterRidingState();

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    ///
////    /// Unlike DismountFromHorse(), this method does NOT call
////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
////    /// and calling back into it would touch destroyed objects. It simply:
////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
////    ///      the home was somehow lost), so the soldier is never left orphaned
////    ///      as a child of a destroyed horse GameObject.
////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
////    ///
////    /// Call order in HorseController (walk-zone drop or any destruction path):
////    ///   soldier.ReturnHomeFromDestroyedHorse();
////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

////        // Clear the reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            // No pre-mount home recorded — snap to wherever home currently points.
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
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

////        // Re-find root canvas every drag.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            CurrentlyDragging = null;
////            return;
////        }

////        // Record home, then override with pre-mount ground position if dismounting.
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
////            gameObject.SetActive(true);   // re-enable if disabled by horse mount
////            ShowOwnVisuals();
////        }

////        // Reparent to root canvas so the soldier draws above all panels.
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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

////        // Not a horse/cannon drop — safe to restore alpha now.
////        _canvasGroup.alpha = 1f;

////        // ── Dragon drop ───────────────────────────────────────────────────────
////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && targetSeat != null && seatFree)
////        {
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;
////            targetDC.PerformMount(this, targetSeat);
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

////            if (currentRider != null && currentRider._isLocked)
////            {
////                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
////                SnapBack();
////            }
////            else if (currentRider != null)
////            {
////                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
////                _mountHomeParent = _homeParent;
////                _mountHomePos = _homeAnchoredPosition;
////                currentRider.DismountFromDragon();
////                targetDC.PerformMount(this, targetSeat);
////            }
////            else
////            {
////                SnapBack();
////            }
////        }
////        else
////        {
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DROP OUTCOMES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////    public void SnapBack()
////    {
////        transform.SetParent(_homeParent, worldPositionStays: true);
////        _rect.anchoredPosition = _homeAnchoredPosition;
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

////        EnsureHelmetEquipped();

////        _controller?.EnterRidingState();

////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        RecordHome();

////        HideOwnVisuals();
////        _animator?.SetState(AnimationState.RiderIdle);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
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
////            ShowOwnVisuals();
////            SnapBack();
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

////    private void HideOwnVisuals()
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
////    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
////    /// active causes two visual layers to fight each other (the "soldier above
////    /// horse" and "duplicate visual" bugs).
////    /// </summary>
////    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
////    {
////        _mountHorseHomeParent = _homeParent;
////        _mountHorseHomePos = _homeAnchoredPosition;
////        _currentHorseSeat = seat;

////        EnsureHelmetEquipped();
////        _controller?.EnterRidingState();

////        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        _rect.localScale = Vector3.one;
////        RecordHome();

////        // DO NOT call HideOwnVisuals() here.
////        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
////        // so the hide runs last and is never overridden by the animator.
////        // (Calling it here caused alpha to be reset to 1 by SetState on the
////        // next frame, leaving the "ghost soldier" visible on top of the horse.)

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Emergency return path called by HorseController.OnDestroy() (or its
////    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
////    ///
////    /// Unlike DismountFromHorse(), this method does NOT call
////    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
////    /// and calling back into it would touch destroyed objects. It simply:
////    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
////    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
////    ///      the home was somehow lost), so the soldier is never left orphaned
////    ///      as a child of a destroyed horse GameObject.
////    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
////    ///
////    /// Call order in HorseController (walk-zone drop or any destruction path):
////    ///   soldier.ReturnHomeFromDestroyedHorse();
////    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
////    /// </summary>
////    public void ReturnHomeFromDestroyedHorse()
////    {
////        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

////        // Clear the reference without calling back into the dying seat.
////        _currentHorseSeat = null;

////        if (_mountHorseHomeParent == null)
////        {
////            // No pre-mount home recorded — snap to wherever home currently points.
////            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
////                             "no mount home recorded — snapping to current home.");
////            gameObject.SetActive(true);
////            _animator?.SetState(AnimationState.Idle);
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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
////            _controller?.ExitRidingState();
////            ShowOwnVisuals();
////            SnapBack();
////            return;
////        }

////        // Re-enable before reparenting so the soldier is visible on the ground.
////        gameObject.SetActive(true);
////        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHorseHomePos;

////        _controller?.ExitRidingState();
////        _animator?.SetState(AnimationState.Idle);
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

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
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

//        // Re-find root canvas every drag.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            CurrentlyDragging = null;
//            return;
//        }

//        // Record home, then override with pre-mount ground position if dismounting.
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

//        // Reparent to root canvas so the soldier draws above all panels.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _homeAnchoredPosition;
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
//    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
//    /// active causes two visual layers to fight each other (the "soldier above
//    /// horse" and "duplicate visual" bugs).
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        _mountHorseHomeParent = _homeParent;
//        _mountHorseHomePos = _homeAnchoredPosition;
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();
//        _controller?.EnterRidingState();

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the "ghost soldier" visible on top of the horse.)

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    ///
//    /// Unlike DismountFromHorse(), this method does NOT call
//    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
//    /// and calling back into it would touch destroyed objects. It simply:
//    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
//    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
//    ///      the home was somehow lost), so the soldier is never left orphaned
//    ///      as a child of a destroyed horse GameObject.
//    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
//    ///
//    /// Call order in HorseController (walk-zone drop or any destruction path):
//    ///   soldier.ReturnHomeFromDestroyedHorse();
//    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

//        // Clear the reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            // No pre-mount home recorded — snap to wherever home currently points.
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
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

//        // Re-find root canvas every drag.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            CurrentlyDragging = null;
//            return;
//        }

//        // Record home, then override with pre-mount ground position if dismounting.
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
//            gameObject.SetActive(true);   // re-enable if disabled by horse mount
//            ShowOwnVisuals();
//        }

//        // Reparent to root canvas so the soldier draws above all panels.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
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

//        // Not a horse/cannon drop — safe to restore alpha now.
//        _canvasGroup.alpha = 1f;

//        // ── Dragon drop ───────────────────────────────────────────────────────
//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetDC.PerformMount(this, targetSeat);
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//                _mountHomeParent = _homeParent;
//                _mountHomePos = _homeAnchoredPosition;
//                currentRider.DismountFromDragon();
//                targetDC.PerformMount(this, targetSeat);
//            }
//            else
//            {
//                SnapBack();
//            }
//        }
//        else
//        {
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _homeAnchoredPosition;
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

//        EnsureHelmetEquipped();

//        _controller?.EnterRidingState();

//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        HideOwnVisuals();
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
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
//            ShowOwnVisuals();
//            SnapBack();
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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

//    private void HideOwnVisuals()
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
//    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
//    /// active causes two visual layers to fight each other (the "soldier above
//    /// horse" and "duplicate visual" bugs).
//    /// </summary>
//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        _mountHorseHomeParent = _homeParent;
//        _mountHorseHomePos = _homeAnchoredPosition;
//        _currentHorseSeat = seat;

//        EnsureHelmetEquipped();
//        _controller?.EnterRidingState();

//        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        _rect.localScale = Vector3.one;
//        RecordHome();

//        // DO NOT call HideOwnVisuals() here.
//        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
//        // so the hide runs last and is never overridden by the animator.
//        // (Calling it here caused alpha to be reset to 1 by SetState on the
//        // next frame, leaving the "ghost soldier" visible on top of the horse.)

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Emergency return path called by HorseController.OnDestroy() (or its
//    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
//    ///
//    /// Unlike DismountFromHorse(), this method does NOT call
//    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
//    /// and calling back into it would touch destroyed objects. It simply:
//    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
//    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
//    ///      the home was somehow lost), so the soldier is never left orphaned
//    ///      as a child of a destroyed horse GameObject.
//    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
//    ///
//    /// Call order in HorseController (walk-zone drop or any destruction path):
//    ///   soldier.ReturnHomeFromDestroyedHorse();
//    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
//    /// </summary>
//    public void ReturnHomeFromDestroyedHorse()
//    {
//        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

//        // Clear the reference without calling back into the dying seat.
//        _currentHorseSeat = null;

//        if (_mountHorseHomeParent == null)
//        {
//            // No pre-mount home recorded — snap to wherever home currently points.
//            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
//                             "no mount home recorded — snapping to current home.");
//            gameObject.SetActive(true);
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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
//            _controller?.ExitRidingState();
//            ShowOwnVisuals();
//            SnapBack();
//            return;
//        }

//        // Re-enable before reparenting so the soldier is visible on the ground.
//        gameObject.SetActive(true);
//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
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
    /// Keeping the soldier's own sprites visible while HorseRiderVisual is also
    /// active causes two visual layers to fight each other (the "soldier above
    /// horse" and "duplicate visual" bugs).
    /// </summary>
    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
    {
        _mountHorseHomeParent = _homeParent;
        _mountHorseHomePos = _homeAnchoredPosition;
        _currentHorseSeat = seat;

        EnsureHelmetEquipped();
        _controller?.EnterRidingState();

        // Reparent (single canonical reparent — HorseSeat.MountSoldier no longer does it)
        transform.SetParent(seat.transform, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        _rect.localScale = Vector3.one;
        RecordHome();

        // DO NOT call HideOwnVisuals() here.
        // HorseController.PerformMount() calls it explicitly AFTER ShowRider()
        // so the hide runs last and is never overridden by the animator.
        // (Calling it here caused alpha to be reset to 1 by SetState on the
        // next frame, leaving the "ghost soldier" visible on top of the horse.)

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Emergency return path called by HorseController.OnDestroy() (or its
    /// walk-zone destruction logic) BEFORE the horse GameObject is destroyed.
    ///
    /// Unlike DismountFromHorse(), this method does NOT call
    /// _currentHorseSeat.ReleaseSoldier() — the seat is already being torn down
    /// and calling back into it would touch destroyed objects. It simply:
    ///   1. Clears the stale seat reference so IsOnHorse becomes false.
    ///   2. Reparents the soldier to its pre-mount home (or snaps back if
    ///      the home was somehow lost), so the soldier is never left orphaned
    ///      as a child of a destroyed horse GameObject.
    ///   3. Restores patrol and visuals exactly as DismountFromHorse() does.
    ///
    /// Call order in HorseController (walk-zone drop or any destruction path):
    ///   soldier.ReturnHomeFromDestroyedHorse();
    ///   Destroy(gameObject);   // ← horse destroyed AFTER soldier is safely home
    /// </summary>
    public void ReturnHomeFromDestroyedHorse()
    {
        if (_currentHorseSeat == null) return;   // not on this horse — nothing to do

        // Clear the reference without calling back into the dying seat.
        _currentHorseSeat = null;

        if (_mountHorseHomeParent == null)
        {
            // No pre-mount home recorded — snap to wherever home currently points.
            Debug.LogWarning("[SoldierDragDrop] ReturnHomeFromDestroyedHorse: " +
                             "no mount home recorded — snapping to current home.");
            gameObject.SetActive(true);
            _animator?.SetState(AnimationState.Idle);
            _controller?.ExitRidingState();
            ShowOwnVisuals();
            SnapBack();
            return;
        }

        // Re-enable before reparenting so the soldier is visible on the ground.
        gameObject.SetActive(true);
        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHorseHomePos;

        _controller?.ExitRidingState();
        _animator?.SetState(AnimationState.Idle);
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
            _controller?.ExitRidingState();
            ShowOwnVisuals();
            SnapBack();
            return;
        }

        // Re-enable before reparenting so the soldier is visible on the ground.
        gameObject.SetActive(true);
        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHorseHomePos;

        _controller?.ExitRidingState();
        _animator?.SetState(AnimationState.Idle);
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
        _currentArcherZone = null;
        _isLocked = false;

        // Re-show the soldier.
        gameObject.SetActive(true);

        // Snap back to the home position recorded before the last drag.
        if (_homeParent != null)
        {
            transform.SetParent(_homeParent, worldPositionStays: false);
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = _homeAnchoredPosition;
        }

        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] '{name}' returned from archer zone.");
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