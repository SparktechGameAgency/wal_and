
////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE — SoldierDragDrop
///////////
/////////// Attach to the Soldier prefab root alongside:
///////////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  DRAG BEHAVIOUR
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
///////////               top of all panels. If the soldier was riding a dragon,
///////////               the seat is released and the rider dragon swaps back
///////////               to the plain dragon AFTER the soldier is safely at
///////////               canvas-root (so the soldier is not destroyed with the
///////////               rider dragon GameObject).
///////////
///////////  OnDrag       Moves the soldier under the pointer.
///////////
///////////  OnEndDrag    Raycasts under the pointer:
///////////                 → DragonController found → PerformMount()
///////////                    (which either swaps to rider prefab or falls back
///////////                     to classic in-place mount depending on setup)
///////////                 → Seat occupied          → SnapBack
///////////                 → Empty space            → SnapBack
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  MOUNT / DISMOUNT
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  OnEndDrag routes through DragonController.PerformMount() instead of
///////////  calling DragonRiderSeat.MountSoldier() directly. This lets the dragon
///////////  controller decide whether to swap prefabs or mount in place.
///////////
///////////  When the soldier is dragged off the rider dragon (OnBeginDrag with
///////////  wasMounted=true), DragonController.PerformDismount() is called AFTER
///////////  the soldier is already at canvas-root — never while still a child.
///////////
///////////  DismountFromDragon() (programmatic dismount, e.g. Retrieve button)
///////////  follows the same safe order: reparent soldier → then PerformDismount.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  HELMET AUTO-EQUIP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  If the soldier has no Helmet equipped when they mount, the system
///////////  looks up the correct default in ArmorHelmetTable (matched to their
///////////  Armor) and calls CharacterEquipment.Equip() automatically.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  1. Attach to SoldierPrefab root alongside CanvasGroup,
///////////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
///////////  3. Root Canvas must have a GraphicRaycaster.
///////////  4. An EventSystem must exist in the scene.
///////////  5. Spawn panel must be a RectTransform + Image (Raycast Target ON).
///////////     No Layout Group — it overrides anchoredPosition every frame.
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class SoldierDragDrop : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    // ── Inspector ─────────────────────────────────────────────────────────────

////////    [Header("Dragon Mount Settings")]
////////    [Tooltip("Maps each armor to its default helmet.\n" +
////////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
////////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
////////    [SerializeField] private ArmorHelmetTable helmetTable;

////////    // ── Component References ──────────────────────────────────────────────────

////////    private CanvasGroup _canvasGroup;
////////    private RectTransform _rect;
////////    private SoldierController _controller;     // optional — patrol + flip
////////    private CharacterEquipment _equipment;      // tracks equipped items
////////    private SpriteLayerAnimator _animator;       // drives per-layer animation

////////    // ── Drag State ────────────────────────────────────────────────────────────

////////    private Canvas _rootCanvas;
////////    private Transform _homeParent;
////////    private Vector2 _homeAnchoredPosition;
////////    private bool _isDragging;

////////    // ── Dragon Rider State ────────────────────────────────────────────────────

////////    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
////////    private DragonRiderSeat _currentSeat;

////////    /// <summary>
////////    /// Ground parent recorded before mounting so DismountFromDragon() can
////////    /// return the soldier to its patrol area, not back to the seat.
////////    /// </summary>
////////    private Transform _mountHomeParent;
////////    private Vector2 _mountHomePos;

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // LIFECYCLE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void Awake()
////////    {
////////        _canvasGroup = GetComponent<CanvasGroup>();
////////        _rect = GetComponent<RectTransform>();
////////        _controller = GetComponent<SoldierController>();    // optional
////////        _equipment = GetComponent<CharacterEquipment>();
////////        _animator = GetComponent<SpriteLayerAnimator>();

////////        if (_equipment == null)
////////            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
////////                             $"'{name}'. Helmet auto-equip will be skipped.", this);
////////        if (_animator == null)
////////            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
////////                             $"'{name}'. Riding animation will not play.", this);
////////        if (helmetTable == null)
////////            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
////////                             $"'{name}'. Soldiers will mount without a helmet.", this);
////////    }

////////    private void Start()
////////    {
////////        RecordHome();
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — BEGIN
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        if (_isDragging) return;

////////        // ── If riding, release the seat and capture the rider dragon DC ───────
////////        //
////////        // We capture the DragonController BEFORE clearing _currentSeat so we
////////        // can call PerformDismount() later (after the soldier is safely at
////////        // canvas-root — never while still a child of the rider dragon).
////////        bool wasMounted = _currentSeat != null;
////////        DragonController mountedDragonDC = null;

////////        if (wasMounted)
////////        {
////////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////////            _currentSeat.ReleaseSoldier();
////////            _currentSeat = null;
////////            _animator?.SetState(AnimationState.Idle);
////////        }

////////        // Re-find root canvas every drag — cached value breaks after Retrieve
////////        // re-parents the soldier to a different panel.
////////        _rootCanvas = GetComponentInParent<Canvas>();
////////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////////        if (_rootCanvas == null)
////////        {
////////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////////                           "Make sure the soldier is inside a Canvas.");
////////            return;
////////        }

////////        // RecordHome() would save the rider seat as home while mounted.
////////        // Override with the original ground home that was stored at mount time.
////////        RecordHome();
////////        if (wasMounted && _mountHomeParent != null)
////////        {
////////            _homeParent = _mountHomeParent;
////////            _homeAnchoredPosition = _mountHomePos;
////////            _mountHomeParent = null;   // consumed — prevent stale reuse
////////        }

////////        _isDragging = true;
////////        _controller?.SetPatrolling(false);

////////        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
////////        // The soldier must not be a child of the rider dragon when it is
////////        // destroyed by PerformDismount, or the soldier would be destroyed too.
////////        transform.SetParent(_rootCanvas.transform, true);
////////        transform.SetAsLastSibling();
////////        _canvasGroup.blocksRaycasts = false;

////////        // ── Swap rider dragon → plain dragon now that the soldier is safe ──────
////////        if (wasMounted)
////////            mountedDragonDC?.PerformDismount();
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — MOVE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnDrag(PointerEventData eventData)
////////    {
////////        if (_rootCanvas == null) return;
////////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — END
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        _isDragging = false;
////////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
////////        // CanvasGroup does not shadow the dragon sitting underneath.

////////        // ── Raycast all UI elements under the pointer ─────────────────────────
////////        var results = new List<RaycastResult>();
////////        EventSystem.current.RaycastAll(eventData, results);

////////        DragonRiderSeat targetSeat = null;
////////        DragonController targetDC = null;

////////        foreach (var r in results)
////////        {
////////            // Walk UP to the DragonController from any hit child.
////////            var dragon = r.gameObject.GetComponentInParent<DragonController>();
////////            if (dragon == null) continue;

////////            // Walk DOWN for a seat — may be null on the PLAIN dragon variant
////////            // (which has no DragonRiderSeat).  We accept null here and let
////////            // PerformMount decide what to do (prefab-swap vs mount-in-place).
////////            targetDC = dragon;
////////            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
////////            break;
////////        }

////////        // Restore raycast blocking — detection is done.
////////        _canvasGroup.blocksRaycasts = true;

////////        // A valid mount target is:
////////        //   • Any DragonController found (targetDC != null)   AND
////////        //   • Either no seat (plain dragon) OR an unoccupied seat
////////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////////        if (targetDC != null && seatFree)
////////        {
////////            // ── Valid drop ────────────────────────────────────────────────────
////////            //
////////            // Save the ground home BEFORE mounting so DismountFromDragon()
////////            // can return here later.
////////            _mountHomeParent = _homeParent;
////////            _mountHomePos = _homeAnchoredPosition;

////////            // PerformMount handles two cases internally:
////////            //   targetSeat == null  → plain dragon  → swap to rider-dragon prefab
////////            //   targetSeat != null  → rider dragon   → mount in place
////////            targetDC.PerformMount(this, targetSeat);
////////        }
////////        else if (targetSeat != null && targetSeat.IsOccupied)
////////        {
////////            // Dragon already has a rider — snap back silently.
////////            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
////////            SnapBack();
////////        }
////////        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
////////        {
////////            // Dropped on empty space — snap back to patrol area.
////////            SnapBack();
////////        }
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DROP OUTCOMES
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////////    public void SnapBack()
////////    {
////////        transform.SetParent(_homeParent, true);
////////        _rect.anchoredPosition = _homeAnchoredPosition;
////////        _controller?.SetPatrolling(true);
////////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////////    }

////////    /// <summary>
////////    /// Called by a drop target (WizardBox) after accepting the soldier.
////////    /// Resets flags here because SetActive(false) prevents OnEndDrag from firing.
////////    /// </summary>
////////    public void OnSuccessfulDrop()
////////    {
////////        _isDragging = false;
////////        _canvasGroup.blocksRaycasts = true;
////////        _controller?.SetPatrolling(false);
////////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////////    }

////////    /// <summary>
////////    /// Re-parents the soldier to spawnParent and resumes patrol.
////////    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
////////    /// </summary>
////////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
////////    {
////////        if (spawnParent == null)
////////        {
////////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
////////            return;
////////        }

////////        transform.SetParent(spawnParent, true);

////////        if (spawnPosition.HasValue)
////////            _rect.anchoredPosition = spawnPosition.Value;

////////        _canvasGroup.blocksRaycasts = true;
////////        _isDragging = false;

////////        RecordHome();
////////        _controller?.SetPatrolling(true);

////////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAGON MOUNT
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Called by DragonRiderSeat.MountSoldier() — either from the plain dragon's
////////    /// own seat (classic fallback) or from the newly spawned rider dragon's seat
////////    /// (prefab-swap path via PerformMount).
////////    ///
////////    /// Order:
////////    ///   1. Auto-equip helmet if missing.
////////    ///   2. Stop patrol and freeze facing direction.
////////    ///   3. Reparent soldier under the seat at seatOffset.
////////    ///   4. Switch ALL sprite layers to the Riding animation.
////////    /// </summary>
////////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
////////    {
////////        _currentSeat = seat;

////////        // 1. Auto-equip helmet BEFORE reparenting so CharacterEquipment.Equip()
////////        //    fires while the soldier is still at canvas-root level.
////////        EnsureHelmetEquipped();

////////        // 2. Stop patrol and freeze facing direction.
////////        _controller?.EnterRidingState();

////////        // 3. Reparent under the seat at the configured offset.
////////        transform.SetParent(seat.transform, false);
////////        _rect.anchoredPosition = seatOffset;
////////        RecordHome();

////////        // 4. All layers (face, armor, helmet…) switch to their riding sprites.
////////        _animator?.SetState(AnimationState.Riding);

////////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAGON DISMOUNT
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Returns the soldier to the ground patrol area and triggers the
////////    /// rider-dragon → plain-dragon prefab swap.
////////    ///
////////    /// Call this from a "Retrieve" button or any dismount game event.
////////    ///
////////    /// SAFE ORDER enforced internally:
////////    ///   1. Capture rider dragon DC before clearing _currentSeat.
////////    ///   2. Release seat.
////////    ///   3. Reparent soldier to ground home.
////////    ///   4. THEN call PerformDismount() so the soldier is no longer a
////////    ///      child of the rider dragon when it is destroyed.
////////    /// </summary>
////////    public void DismountFromDragon()
////////    {
////////        // Capture the rider dragon DC before we null _currentSeat.
////////        DragonController riderDragonDC = null;
////////        if (_currentSeat != null)
////////        {
////////            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////////            _currentSeat.ReleaseSoldier();
////////            _currentSeat = null;
////////        }

////////        if (_mountHomeParent == null)
////////        {
////////            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
////////                             "recorded — snapping to current home.");
////////            SnapBack();
////////            // Even when snapping back, swap the rider dragon out.
////////            riderDragonDC?.PerformDismount();
////////            return;
////////        }

////////        // ── Reparent soldier to ground BEFORE destroying the rider dragon ──────
////////        transform.SetParent(_mountHomeParent, false);
////////        _rect.anchoredPosition = _mountHomePos;

////////        // Restore patrol and facing direction.
////////        _controller?.ExitRidingState();

////////        // Return all sprite layers to idle animation.
////////        _animator?.SetState(AnimationState.Idle);

////////        RecordHome();
////////        _mountHomeParent = null;   // consumed — prevent stale reuse

////////        // ── NOW safe to swap the rider dragon back to the plain dragon ─────────
////////        riderDragonDC?.PerformDismount();

////////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // HELMET AUTO-EQUIP
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// If the soldier has no Helmet equipped, looks up the default helmet
////////    /// for their Armor in ArmorHelmetTable and equips it automatically.
////////    ///
////////    /// Lookup order (ArmorHelmetTable.GetDefaultHelmet):
////////    ///   1. Entry matching the soldier's equipped armor → paired defaultHelmet.
////////    ///   2. fallbackHelmet — used when no armor or no matching entry.
////////    ///   3. null           — logs a warning; soldier mounts without helmet.
////////    /// </summary>
////////    private void EnsureHelmetEquipped()
////////    {
////////        if (_equipment == null) return;

////////        // Already has a helmet — nothing to do.
////////        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

////////        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
////////        EquipmentItem helmet = helmetTable != null
////////            ? helmetTable.GetDefaultHelmet(armor)
////////            : null;

////////        if (helmet != null)
////////        {
////////            _equipment.Equip(helmet);
////////            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
////////                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
////////        }
////////        else
////////        {
////////            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
////////                             $"armor '{armor?.itemName ?? "none"}'. " +
////////                             "Set fallbackHelmet in ArmorHelmetTable.", this);
////////        }
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // HELPER
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void RecordHome()
////////    {
////////        _homeParent = transform.parent;
////////        _homeAnchoredPosition = _rect.anchoredPosition;
////////    }
////////}

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — SoldierDragDrop
/////////
///////// Attach to the Soldier prefab root alongside:
/////////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  DRAG BEHAVIOUR
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
/////////               top of all panels.
/////////               If the soldier was riding a dragon, the seat is released,
/////////               the soldier's own visuals are restored (alpha 1), and
/////////               DragonController.PerformDismount() hides the rider visual
/////////               — all AFTER the soldier is safely at canvas-root.
/////////
/////////  OnDrag       Moves the soldier under the pointer.
/////////
/////////  OnEndDrag    Raycasts under the pointer:
/////////                 → DragonController with free seat → PerformMount()
/////////                 → Occupied seat                  → SnapBack
/////////                 → Empty space                    → SnapBack
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  MOUNT FLOW
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  SoldierDragDrop.OnEndDrag
/////////    └─ DragonController.PerformMount(soldier, seat)
/////////         ├─ DragonRiderSeat.MountSoldier(soldier)
/////////         │    └─ soldier.MountOnDragon(seat, offset)
/////////         │         ├─ EnsureHelmetEquipped()
/////////         │         ├─ Reparent soldier under seat
/////////         │         ├─ HideOwnVisuals()          ← soldier turns invisible
/////////         │         └─ SpriteLayerAnimator → Riding
/////////         └─ DragonRiderVisual.ShowForSoldier()  ← dragon shows armored rider
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  DISMOUNT FLOW
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  SoldierDragDrop.OnBeginDrag  (soldier dragged off dragon)
/////////    ├─ seat.ReleaseSoldier()
/////////    ├─ ShowOwnVisuals()                         ← soldier turns visible again
/////////    ├─ Reparent soldier to canvas root
/////////    └─ DragonController.PerformDismount()       ← dragon hides rider visual
/////////
/////////  SoldierDragDrop.DismountFromDragon()          (programmatic, e.g. Retrieve button)
/////////    ├─ seat.ReleaseSoldier()
/////////    ├─ Reparent soldier to ground home
/////////    ├─ ShowOwnVisuals()
/////////    └─ DragonController.PerformDismount()
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. Attach to SoldierPrefab root alongside CanvasGroup,
/////////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
/////////  3. Root Canvas must have a GraphicRaycaster.
/////////  4. An EventSystem must exist in the scene.
/////////  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
/////////     No Layout Group — it overrides anchoredPosition every frame.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class SoldierDragDrop : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────────────────

//////    [Header("Dragon Mount Settings")]
//////    [Tooltip("Maps each armor to its default helmet.\n" +
//////             "Create via: right-click Project -> Create -> AreaForge -> Armor Helmet Table.\n" +
//////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
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

//////    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
//////    private DragonRiderSeat _currentSeat;

//////    /// <summary>
//////    /// Ground parent recorded before mounting so DismountFromDragon() can
//////    /// return the soldier to its patrol area, not back to the seat.
//////    /// </summary>
//////    private Transform _mountHomeParent;
//////    private Vector2 _mountHomePos;

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

//////        // ── Capture mounted dragon before clearing the seat ───────────────────
//////        bool wasMounted = _currentSeat != null;
//////        DragonController mountedDragonDC = null;

//////        if (wasMounted)
//////        {
//////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            // Restore soldier's own sprite layers before they become visible again.
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // Re-find root canvas every drag — cached value breaks after Retrieve
//////        // re-parents the soldier to a different panel.
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//////        if (_rootCanvas == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//////                           "Make sure the soldier is inside a Canvas.");
//////            return;
//////        }

//////        // If dismounting, override home with the ground position saved at mount time.
//////        RecordHome();
//////        if (wasMounted && _mountHomeParent != null)
//////        {
//////            _homeParent = _mountHomeParent;
//////            _homeAnchoredPosition = _mountHomePos;
//////            _mountHomeParent = null;   // consumed — prevent stale reuse
//////        }

//////        _isDragging = true;
//////        _controller?.SetPatrolling(false);

//////        // ── Make soldier's own visuals visible BEFORE reparenting ─────────────
//////        // ShowOwnVisuals() restores alpha to 1 so the soldier is fully visible
//////        // while being dragged. The drag-alpha (0.75) is applied right after.
//////        if (wasMounted)
//////            ShowOwnVisuals();

//////        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
//////        // The soldier must not be a child of the dragon when PerformDismount
//////        // hides the rider visual — ordering doesn't matter for single-prefab,
//////        // but kept for clarity and future safety.
//////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////        transform.SetAsLastSibling();

//////        _canvasGroup.alpha = 0.75f;
//////        _canvasGroup.blocksRaycasts = false;

//////        // ── Notify dragon to hide its rider visual ────────────────────────────
//////        if (wasMounted)
//////            mountedDragonDC?.PerformDismount();
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
//////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//////        // CanvasGroup does not shadow the dragon sitting underneath.

//////        var results = new List<RaycastResult>();
//////        EventSystem.current.RaycastAll(eventData, results);

//////        DragonRiderSeat targetSeat = null;
//////        DragonController targetDC = null;

//////        foreach (var r in results)
//////        {
//////            var dragon = r.gameObject.GetComponentInParent<DragonController>();
//////            if (dragon == null) continue;

//////            targetDC = dragon;
//////            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//////            break;
//////        }

//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.alpha = 1f;

//////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//////        if (targetDC != null && targetSeat != null && seatFree)
//////        {
//////            // Valid drop on an unoccupied dragon.
//////            // Save ground home BEFORE mounting so DismountFromDragon() can return here.
//////            _mountHomeParent = _homeParent;
//////            _mountHomePos = _homeAnchoredPosition;

//////            targetDC.PerformMount(this, targetSeat);
//////        }
//////        else if (targetSeat != null && targetSeat.IsOccupied)
//////        {
//////            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
//////            SnapBack();
//////        }
//////        else
//////        {
//////            // Dropped on empty space — return to patrol area.
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
//////    /// Called by a drop target (WizardBox) after accepting the soldier.
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
//////    // DRAGON MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//////    ///
//////    /// Order:
//////    ///   1. Auto-equip helmet if missing (needed so the rider visual shows
//////    ///      the correct helmet immediately after ShowForSoldier() is called).
//////    ///   2. Stop patrol and freeze facing direction.
//////    ///   3. Reparent soldier under the seat at seatOffset.
//////    ///   4. Hide the soldier's own visuals (dragon's rider visual takes over).
//////    ///   5. Switch SpriteLayerAnimator to Riding state (drives animation data
//////    ///      even while the visuals are hidden, so dismount restores correctly).
//////    /// </summary>
//////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//////    {
//////        _currentSeat = seat;

//////        // 1. Auto-equip helmet BEFORE reparenting.
//////        EnsureHelmetEquipped();

//////        // 2. Stop patrol.
//////        _controller?.EnterRidingState();

//////        // 3. Reparent under the seat.
//////        transform.SetParent(seat.transform, worldPositionStays: false);
//////        _rect.anchoredPosition = seatOffset;
//////        RecordHome();

//////        // 4. Hide soldier's own visuals — the dragon's rider visual shows instead.
//////        HideOwnVisuals();

//////        // 5. Switch animation state (so riding sprites are ready if ShowOwnVisuals
//////        //    is ever called while still parented to the seat, e.g. in a future feature).
//////        _animator?.SetState(AnimationState.Riding);

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Returns the soldier to the ground patrol area and hides the dragon's
//////    /// rider visual.
//////    ///
//////    /// Safe order:
//////    ///   1. Capture dragon DC before clearing _currentSeat.
//////    ///   2. Release seat.
//////    ///   3. Reparent soldier to ground home.
//////    ///   4. Restore soldier's visuals.
//////    ///   5. Call PerformDismount() on the dragon — hides rider visual.
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

//////        // Reparent soldier to ground home.
//////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//////        _rect.anchoredPosition = _mountHomePos;

//////        // Restore patrol and facing direction.
//////        _controller?.ExitRidingState();

//////        // Restore sprite layers to idle.
//////        _animator?.SetState(AnimationState.Idle);

//////        // Restore soldier visuals.
//////        ShowOwnVisuals();

//////        RecordHome();
//////        _mountHomeParent = null;   // consumed — prevent stale reuse

//////        // Notify dragon to hide its rider visual.
//////        riderDragonDC?.PerformDismount();

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // VISUAL SHOW / HIDE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Hides the soldier's own sprite layers by setting CanvasGroup alpha to 0.
//////    /// blocksRaycasts stays TRUE so the soldier remains draggable while mounted.
//////    /// Called by MountOnDragon() — the dragon's rider visual displays instead.
//////    /// </summary>
//////    private void HideOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 0f;
//////        _canvasGroup.blocksRaycasts = true;   // still draggable even when invisible
//////        _canvasGroup.interactable = true;
//////    }

//////    /// <summary>
//////    /// Restores the soldier's own sprite layers to fully visible.
//////    /// Called on dismount (drag-off or programmatic) before the soldier
//////    /// becomes visible in the world again.
//////    /// </summary>
//////    private void ShowOwnVisuals()
//////    {
//////        _canvasGroup.alpha = 1f;
//////        _canvasGroup.blocksRaycasts = true;
//////        _canvasGroup.interactable = true;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELMET AUTO-EQUIP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// If the soldier has no Helmet equipped, looks up the default helmet for
//////    /// their Armor in ArmorHelmetTable and equips it automatically.
//////    /// This ensures the dragon's rider visual can show the correct helmet sprite.
//////    /// </summary>
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
//////}


//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE — SoldierDragDrop
/////////
///////// Attach to the Soldier prefab root alongside:
/////////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  DRAG BEHAVIOUR
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
/////////               top of all panels. If the soldier was riding a dragon,
/////////               the seat is released and the rider dragon swaps back
/////////               to the plain dragon AFTER the soldier is safely at
/////////               canvas-root (so the soldier is not destroyed with the
/////////               rider dragon GameObject).
/////////
/////////  OnDrag       Moves the soldier under the pointer.
/////////
/////////  OnEndDrag    Raycasts under the pointer:
/////////                 → DragonController found → PerformMount()
/////////                    (which either swaps to rider prefab or falls back
/////////                     to classic in-place mount depending on setup)
/////////                 → Seat occupied          → SnapBack
/////////                 → Empty space            → SnapBack
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  MOUNT / DISMOUNT
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  OnEndDrag routes through DragonController.PerformMount() instead of
/////////  calling DragonRiderSeat.MountSoldier() directly. This lets the dragon
/////////  controller decide whether to swap prefabs or mount in place.
/////////
/////////  When the soldier is dragged off the rider dragon (OnBeginDrag with
/////////  wasMounted=true), DragonController.PerformDismount() is called AFTER
/////////  the soldier is already at canvas-root — never while still a child.
/////////
/////////  DismountFromDragon() (programmatic dismount, e.g. Retrieve button)
/////////  follows the same safe order: reparent soldier → then PerformDismount.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  HELMET AUTO-EQUIP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  If the soldier has no Helmet equipped when they mount, the system
/////////  looks up the correct default in ArmorHelmetTable (matched to their
/////////  Armor) and calls CharacterEquipment.Equip() automatically.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. Attach to SoldierPrefab root alongside CanvasGroup,
/////////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
/////////  3. Root Canvas must have a GraphicRaycaster.
/////////  4. An EventSystem must exist in the scene.
/////////  5. Spawn panel must be a RectTransform + Image (Raycast Target ON).
/////////     No Layout Group — it overrides anchoredPosition every frame.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class SoldierDragDrop : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────────────────

//////    [Header("Dragon Mount Settings")]
//////    [Tooltip("Maps each armor to its default helmet.\n" +
//////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
//////    [SerializeField] private ArmorHelmetTable helmetTable;

//////    // ── Component References ──────────────────────────────────────────────────

//////    private CanvasGroup _canvasGroup;
//////    private RectTransform _rect;
//////    private SoldierController _controller;     // optional — patrol + flip
//////    private CharacterEquipment _equipment;      // tracks equipped items
//////    private SpriteLayerAnimator _animator;       // drives per-layer animation

//////    // ── Drag State ────────────────────────────────────────────────────────────

//////    private Canvas _rootCanvas;
//////    private Transform _homeParent;
//////    private Vector2 _homeAnchoredPosition;
//////    private bool _isDragging;

//////    // ── Dragon Rider State ────────────────────────────────────────────────────

//////    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
//////    private DragonRiderSeat _currentSeat;

//////    /// <summary>
//////    /// Ground parent recorded before mounting so DismountFromDragon() can
//////    /// return the soldier to its patrol area, not back to the seat.
//////    /// </summary>
//////    private Transform _mountHomeParent;
//////    private Vector2 _mountHomePos;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////        _rect = GetComponent<RectTransform>();
//////        _controller = GetComponent<SoldierController>();    // optional
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

//////        // ── If riding, release the seat and capture the rider dragon DC ───────
//////        //
//////        // We capture the DragonController BEFORE clearing _currentSeat so we
//////        // can call PerformDismount() later (after the soldier is safely at
//////        // canvas-root — never while still a child of the rider dragon).
//////        bool wasMounted = _currentSeat != null;
//////        DragonController mountedDragonDC = null;

//////        if (wasMounted)
//////        {
//////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//////            _currentSeat.ReleaseSoldier();
//////            _currentSeat = null;
//////            _animator?.SetState(AnimationState.Idle);
//////        }

//////        // Re-find root canvas every drag — cached value breaks after Retrieve
//////        // re-parents the soldier to a different panel.
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//////        if (_rootCanvas == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//////                           "Make sure the soldier is inside a Canvas.");
//////            return;
//////        }

//////        // RecordHome() would save the rider seat as home while mounted.
//////        // Override with the original ground home that was stored at mount time.
//////        RecordHome();
//////        if (wasMounted && _mountHomeParent != null)
//////        {
//////            _homeParent = _mountHomeParent;
//////            _homeAnchoredPosition = _mountHomePos;
//////            _mountHomeParent = null;   // consumed — prevent stale reuse
//////        }

//////        _isDragging = true;
//////        _controller?.SetPatrolling(false);

//////        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
//////        // The soldier must not be a child of the rider dragon when it is
//////        // destroyed by PerformDismount, or the soldier would be destroyed too.
//////        transform.SetParent(_rootCanvas.transform, true);
//////        transform.SetAsLastSibling();
//////        _canvasGroup.blocksRaycasts = false;

//////        // ── Swap rider dragon → plain dragon now that the soldier is safe ──────
//////        if (wasMounted)
//////            mountedDragonDC?.PerformDismount();
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
//////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//////        // CanvasGroup does not shadow the dragon sitting underneath.

//////        // ── Raycast all UI elements under the pointer ─────────────────────────
//////        var results = new List<RaycastResult>();
//////        EventSystem.current.RaycastAll(eventData, results);

//////        DragonRiderSeat targetSeat = null;
//////        DragonController targetDC = null;

//////        foreach (var r in results)
//////        {
//////            // Walk UP to the DragonController from any hit child.
//////            var dragon = r.gameObject.GetComponentInParent<DragonController>();
//////            if (dragon == null) continue;

//////            // Walk DOWN for a seat — may be null on the PLAIN dragon variant
//////            // (which has no DragonRiderSeat).  We accept null here and let
//////            // PerformMount decide what to do (prefab-swap vs mount-in-place).
//////            targetDC = dragon;
//////            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//////            break;
//////        }

//////        // Restore raycast blocking — detection is done.
//////        _canvasGroup.blocksRaycasts = true;

//////        // A valid mount target is:
//////        //   • Any DragonController found (targetDC != null)   AND
//////        //   • Either no seat (plain dragon) OR an unoccupied seat
//////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//////        if (targetDC != null && seatFree)
//////        {
//////            // ── Valid drop ────────────────────────────────────────────────────
//////            //
//////            // Save the ground home BEFORE mounting so DismountFromDragon()
//////            // can return here later.
//////            _mountHomeParent = _homeParent;
//////            _mountHomePos = _homeAnchoredPosition;

//////            // PerformMount handles two cases internally:
//////            //   targetSeat == null  → plain dragon  → swap to rider-dragon prefab
//////            //   targetSeat != null  → rider dragon   → mount in place
//////            targetDC.PerformMount(this, targetSeat);
//////        }
//////        else if (targetSeat != null && targetSeat.IsOccupied)
//////        {
//////            // Dragon already has a rider — snap back silently.
//////            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
//////            SnapBack();
//////        }
//////        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
//////        {
//////            // Dropped on empty space — snap back to patrol area.
//////            SnapBack();
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DROP OUTCOMES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//////    public void SnapBack()
//////    {
//////        transform.SetParent(_homeParent, true);
//////        _rect.anchoredPosition = _homeAnchoredPosition;
//////        _controller?.SetPatrolling(true);
//////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//////    }

//////    /// <summary>
//////    /// Called by a drop target (WizardBox) after accepting the soldier.
//////    /// Resets flags here because SetActive(false) prevents OnEndDrag from firing.
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

//////        transform.SetParent(spawnParent, true);

//////        if (spawnPosition.HasValue)
//////            _rect.anchoredPosition = spawnPosition.Value;

//////        _canvasGroup.blocksRaycasts = true;
//////        _isDragging = false;

//////        RecordHome();
//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON MOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by DragonRiderSeat.MountSoldier() — either from the plain dragon's
//////    /// own seat (classic fallback) or from the newly spawned rider dragon's seat
//////    /// (prefab-swap path via PerformMount).
//////    ///
//////    /// Order:
//////    ///   1. Auto-equip helmet if missing.
//////    ///   2. Stop patrol and freeze facing direction.
//////    ///   3. Reparent soldier under the seat at seatOffset.
//////    ///   4. Switch ALL sprite layers to the Riding animation.
//////    /// </summary>
//////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//////    {
//////        _currentSeat = seat;

//////        // 1. Auto-equip helmet BEFORE reparenting so CharacterEquipment.Equip()
//////        //    fires while the soldier is still at canvas-root level.
//////        EnsureHelmetEquipped();

//////        // 2. Stop patrol and freeze facing direction.
//////        _controller?.EnterRidingState();

//////        // 3. Reparent under the seat at the configured offset.
//////        transform.SetParent(seat.transform, false);
//////        _rect.anchoredPosition = seatOffset;
//////        RecordHome();

//////        // 4. All layers (face, armor, helmet…) switch to their riding sprites.
//////        _animator?.SetState(AnimationState.Riding);

//////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAGON DISMOUNT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Returns the soldier to the ground patrol area and triggers the
//////    /// rider-dragon → plain-dragon prefab swap.
//////    ///
//////    /// Call this from a "Retrieve" button or any dismount game event.
//////    ///
//////    /// SAFE ORDER enforced internally:
//////    ///   1. Capture rider dragon DC before clearing _currentSeat.
//////    ///   2. Release seat.
//////    ///   3. Reparent soldier to ground home.
//////    ///   4. THEN call PerformDismount() so the soldier is no longer a
//////    ///      child of the rider dragon when it is destroyed.
//////    /// </summary>
//////    public void DismountFromDragon()
//////    {
//////        // Capture the rider dragon DC before we null _currentSeat.
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
//////            SnapBack();
//////            // Even when snapping back, swap the rider dragon out.
//////            riderDragonDC?.PerformDismount();
//////            return;
//////        }

//////        // ── Reparent soldier to ground BEFORE destroying the rider dragon ──────
//////        transform.SetParent(_mountHomeParent, false);
//////        _rect.anchoredPosition = _mountHomePos;

//////        // Restore patrol and facing direction.
//////        _controller?.ExitRidingState();

//////        // Return all sprite layers to idle animation.
//////        _animator?.SetState(AnimationState.Idle);

//////        RecordHome();
//////        _mountHomeParent = null;   // consumed — prevent stale reuse

//////        // ── NOW safe to swap the rider dragon back to the plain dragon ─────────
//////        riderDragonDC?.PerformDismount();

//////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELMET AUTO-EQUIP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// If the soldier has no Helmet equipped, looks up the default helmet
//////    /// for their Armor in ArmorHelmetTable and equips it automatically.
//////    ///
//////    /// Lookup order (ArmorHelmetTable.GetDefaultHelmet):
//////    ///   1. Entry matching the soldier's equipped armor → paired defaultHelmet.
//////    ///   2. fallbackHelmet — used when no armor or no matching entry.
//////    ///   3. null           — logs a warning; soldier mounts without helmet.
//////    /// </summary>
//////    private void EnsureHelmetEquipped()
//////    {
//////        if (_equipment == null) return;

//////        // Already has a helmet — nothing to do.
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
//////}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — SoldierDragDrop
///////
/////// Attach to the Soldier prefab root alongside:
///////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  DRAG BEHAVIOUR
/////// ════════════════════════════════════════════════════════════════════
///////
///////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
///////               top of all panels.
///////               If the soldier was riding a dragon, the seat is released,
///////               the soldier's own visuals are restored (alpha 1), and
///////               DragonController.PerformDismount() hides the rider visual
///////               — all AFTER the soldier is safely at canvas-root.
///////
///////  OnDrag       Moves the soldier under the pointer.
///////
///////  OnEndDrag    Raycasts under the pointer:
///////                 → DragonController with free seat → PerformMount()
///////                 → Occupied seat                  → SnapBack
///////                 → Empty space                    → SnapBack
///////
/////// ════════════════════════════════════════════════════════════════════
///////  MOUNT FLOW
/////// ════════════════════════════════════════════════════════════════════
///////
///////  SoldierDragDrop.OnEndDrag
///////    └─ DragonController.PerformMount(soldier, seat)
///////         ├─ DragonRiderSeat.MountSoldier(soldier)
///////         │    └─ soldier.MountOnDragon(seat, offset)
///////         │         ├─ EnsureHelmetEquipped()
///////         │         ├─ Reparent soldier under seat
///////         │         ├─ HideOwnVisuals()          ← soldier turns invisible
///////         │         └─ SpriteLayerAnimator → Riding
///////         └─ DragonRiderVisual.ShowForSoldier()  ← dragon shows armored rider
///////
/////// ════════════════════════════════════════════════════════════════════
///////  DISMOUNT FLOW
/////// ════════════════════════════════════════════════════════════════════
///////
///////  SoldierDragDrop.OnBeginDrag  (soldier dragged off dragon)
///////    ├─ seat.ReleaseSoldier()
///////    ├─ ShowOwnVisuals()                         ← soldier turns visible again
///////    ├─ Reparent soldier to canvas root
///////    └─ DragonController.PerformDismount()       ← dragon hides rider visual
///////
///////  SoldierDragDrop.DismountFromDragon()          (programmatic, e.g. Retrieve button)
///////    ├─ seat.ReleaseSoldier()
///////    ├─ Reparent soldier to ground home
///////    ├─ ShowOwnVisuals()
///////    └─ DragonController.PerformDismount()
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Attach to SoldierPrefab root alongside CanvasGroup,
///////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
///////  3. Root Canvas must have a GraphicRaycaster.
///////  4. An EventSystem must exist in the scene.
///////  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
///////     No Layout Group — it overrides anchoredPosition every frame.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Dragon Mount Settings")]
////    [Tooltip("Maps each armor to its default helmet.\n" +
////             "Create via: right-click Project -> Create -> AreaForge -> Armor Helmet Table.\n" +
////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
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

////    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
////    private DragonRiderSeat _currentSeat;

////    /// <summary>
////    /// Ground parent recorded before mounting so DismountFromDragon() can
////    /// return the soldier to its patrol area, not back to the seat.
////    /// </summary>
////    private Transform _mountHomeParent;
////    private Vector2 _mountHomePos;

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

////        // ── Capture mounted dragon before clearing the seat ───────────────────
////        bool wasMounted = _currentSeat != null;
////        DragonController mountedDragonDC = null;

////        if (wasMounted)
////        {
////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            // Restore soldier's own sprite layers before they become visible again.
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // Re-find root canvas every drag — cached value breaks after Retrieve
////        // re-parents the soldier to a different panel.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            return;
////        }

////        // If dismounting, override home with the ground position saved at mount time.
////        RecordHome();
////        if (wasMounted && _mountHomeParent != null)
////        {
////            _homeParent = _mountHomeParent;
////            _homeAnchoredPosition = _mountHomePos;
////            _mountHomeParent = null;   // consumed — prevent stale reuse
////        }

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // ── Make soldier's own visuals visible BEFORE reparenting ─────────────
////        // ShowOwnVisuals() restores alpha to 1 so the soldier is fully visible
////        // while being dragged. The drag-alpha (0.75) is applied right after.
////        if (wasMounted)
////            ShowOwnVisuals();

////        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
////        // The soldier must not be a child of the dragon when PerformDismount
////        // hides the rider visual — ordering doesn't matter for single-prefab,
////        // but kept for clarity and future safety.
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////        transform.SetAsLastSibling();

////        _canvasGroup.alpha = 0.75f;
////        _canvasGroup.blocksRaycasts = false;

////        // ── Notify dragon to hide its rider visual ────────────────────────────
////        if (wasMounted)
////            mountedDragonDC?.PerformDismount();
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
////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
////        // CanvasGroup does not shadow the dragon sitting underneath.

////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        DragonRiderSeat targetSeat = null;
////        DragonController targetDC = null;

////        foreach (var r in results)
////        {
////            var dragon = r.gameObject.GetComponentInParent<DragonController>();
////            if (dragon == null) continue;

////            targetDC = dragon;
////            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
////            break;
////        }

////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.alpha = 1f;

////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && targetSeat != null && seatFree)
////        {
////            // Valid drop on an unoccupied dragon.
////            // Save ground home BEFORE mounting so DismountFromDragon() can return here.
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;

////            targetDC.PerformMount(this, targetSeat);
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
////            SnapBack();
////        }
////        else
////        {
////            // Dropped on empty space — return to patrol area.
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
////    /// Called by a drop target (WizardBox) after accepting the soldier.
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
////    // DRAGON MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
////    ///
////    /// Order:
////    ///   1. Auto-equip helmet if missing (needed so the rider visual shows
////    ///      the correct helmet immediately after ShowForSoldier() is called).
////    ///   2. Stop patrol and freeze facing direction.
////    ///   3. Reparent soldier under the seat at seatOffset.
////    ///   4. Hide the soldier's own visuals (dragon's rider visual takes over).
////    ///   5. Switch SpriteLayerAnimator to Riding state (drives animation data
////    ///      even while the visuals are hidden, so dismount restores correctly).
////    /// </summary>
////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
////    {
////        _currentSeat = seat;

////        // 1. Auto-equip helmet BEFORE reparenting.
////        EnsureHelmetEquipped();

////        // 2. Stop patrol.
////        _controller?.EnterRidingState();

////        // 3. Reparent under the seat.
////        transform.SetParent(seat.transform, worldPositionStays: false);
////        _rect.anchoredPosition = seatOffset;
////        RecordHome();

////        // 4. Hide soldier's own visuals — the dragon's rider visual shows instead.
////        HideOwnVisuals();

////        // 5. Switch animation state (so riding sprites are ready if ShowOwnVisuals
////        //    is ever called while still parented to the seat, e.g. in a future feature).
////        _animator?.SetState(AnimationState.RiderIdle);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Returns the soldier to the ground patrol area and hides the dragon's
////    /// rider visual.
////    ///
////    /// Safe order:
////    ///   1. Capture dragon DC before clearing _currentSeat.
////    ///   2. Release seat.
////    ///   3. Reparent soldier to ground home.
////    ///   4. Restore soldier's visuals.
////    ///   5. Call PerformDismount() on the dragon — hides rider visual.
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

////        // Reparent soldier to ground home.
////        transform.SetParent(_mountHomeParent, worldPositionStays: false);
////        _rect.anchoredPosition = _mountHomePos;

////        // Restore patrol and facing direction.
////        _controller?.ExitRidingState();

////        // Restore sprite layers to idle.
////        _animator?.SetState(AnimationState.Idle);

////        // Restore soldier visuals.
////        ShowOwnVisuals();

////        RecordHome();
////        _mountHomeParent = null;   // consumed — prevent stale reuse

////        // Notify dragon to hide its rider visual.
////        riderDragonDC?.PerformDismount();

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // VISUAL SHOW / HIDE
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Hides the soldier's own sprite layers by setting CanvasGroup alpha to 0.
////    /// blocksRaycasts stays TRUE so the soldier remains draggable while mounted.
////    /// Called by MountOnDragon() — the dragon's rider visual displays instead.
////    /// </summary>
////    private void HideOwnVisuals()
////    {
////        _canvasGroup.alpha = 0f;
////        _canvasGroup.blocksRaycasts = true;   // still draggable even when invisible
////        _canvasGroup.interactable = true;
////    }

////    /// <summary>
////    /// Restores the soldier's own sprite layers to fully visible.
////    /// Called on dismount (drag-off or programmatic) before the soldier
////    /// becomes visible in the world again.
////    /// </summary>
////    private void ShowOwnVisuals()
////    {
////        _canvasGroup.alpha = 1f;
////        _canvasGroup.blocksRaycasts = true;
////        _canvasGroup.interactable = true;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELMET AUTO-EQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// If the soldier has no Helmet equipped, looks up the default helmet for
////    /// their Armor in ArmorHelmetTable and equips it automatically.
////    /// This ensures the dragon's rider visual can show the correct helmet sprite.
////    /// </summary>
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
////}


////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE — SoldierDragDrop
///////
/////// Attach to the Soldier prefab root alongside:
///////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  DRAG BEHAVIOUR
/////// ════════════════════════════════════════════════════════════════════
///////
///////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
///////               top of all panels. If the soldier was riding a dragon,
///////               the seat is released and the rider dragon swaps back
///////               to the plain dragon AFTER the soldier is safely at
///////               canvas-root (so the soldier is not destroyed with the
///////               rider dragon GameObject).
///////
///////  OnDrag       Moves the soldier under the pointer.
///////
///////  OnEndDrag    Raycasts under the pointer:
///////                 → DragonController found → PerformMount()
///////                    (which either swaps to rider prefab or falls back
///////                     to classic in-place mount depending on setup)
///////                 → Seat occupied          → SnapBack
///////                 → Empty space            → SnapBack
///////
/////// ════════════════════════════════════════════════════════════════════
///////  MOUNT / DISMOUNT
/////// ════════════════════════════════════════════════════════════════════
///////
///////  OnEndDrag routes through DragonController.PerformMount() instead of
///////  calling DragonRiderSeat.MountSoldier() directly. This lets the dragon
///////  controller decide whether to swap prefabs or mount in place.
///////
///////  When the soldier is dragged off the rider dragon (OnBeginDrag with
///////  wasMounted=true), DragonController.PerformDismount() is called AFTER
///////  the soldier is already at canvas-root — never while still a child.
///////
///////  DismountFromDragon() (programmatic dismount, e.g. Retrieve button)
///////  follows the same safe order: reparent soldier → then PerformDismount.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  HELMET AUTO-EQUIP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  If the soldier has no Helmet equipped when they mount, the system
///////  looks up the correct default in ArmorHelmetTable (matched to their
///////  Armor) and calls CharacterEquipment.Equip() automatically.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Attach to SoldierPrefab root alongside CanvasGroup,
///////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
///////  3. Root Canvas must have a GraphicRaycaster.
///////  4. An EventSystem must exist in the scene.
///////  5. Spawn panel must be a RectTransform + Image (Raycast Target ON).
///////     No Layout Group — it overrides anchoredPosition every frame.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Dragon Mount Settings")]
////    [Tooltip("Maps each armor to its default helmet.\n" +
////             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
////             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
////    [SerializeField] private ArmorHelmetTable helmetTable;

////    // ── Component References ──────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;     // optional — patrol + flip
////    private CharacterEquipment _equipment;      // tracks equipped items
////    private SpriteLayerAnimator _animator;       // drives per-layer animation

////    // ── Drag State ────────────────────────────────────────────────────────────

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
////    private bool _isDragging;

////    // ── Dragon Rider State ────────────────────────────────────────────────────

////    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
////    private DragonRiderSeat _currentSeat;

////    /// <summary>
////    /// Ground parent recorded before mounting so DismountFromDragon() can
////    /// return the soldier to its patrol area, not back to the seat.
////    /// </summary>
////    private Transform _mountHomeParent;
////    private Vector2 _mountHomePos;

////    // ══════════════════════════════════════════════════════════════════════════
////    // LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////        _rect = GetComponent<RectTransform>();
////        _controller = GetComponent<SoldierController>();    // optional
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

////        // ── If riding, release the seat and capture the rider dragon DC ───────
////        //
////        // We capture the DragonController BEFORE clearing _currentSeat so we
////        // can call PerformDismount() later (after the soldier is safely at
////        // canvas-root — never while still a child of the rider dragon).
////        bool wasMounted = _currentSeat != null;
////        DragonController mountedDragonDC = null;

////        if (wasMounted)
////        {
////            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
////            _currentSeat.ReleaseSoldier();
////            _currentSeat = null;
////            _animator?.SetState(AnimationState.Idle);
////        }

////        // Re-find root canvas every drag — cached value breaks after Retrieve
////        // re-parents the soldier to a different panel.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
////                           "Make sure the soldier is inside a Canvas.");
////            return;
////        }

////        // RecordHome() would save the rider seat as home while mounted.
////        // Override with the original ground home that was stored at mount time.
////        RecordHome();
////        if (wasMounted && _mountHomeParent != null)
////        {
////            _homeParent = _mountHomeParent;
////            _homeAnchoredPosition = _mountHomePos;
////            _mountHomeParent = null;   // consumed — prevent stale reuse
////        }

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
////        // The soldier must not be a child of the rider dragon when it is
////        // destroyed by PerformDismount, or the soldier would be destroyed too.
////        transform.SetParent(_rootCanvas.transform, true);
////        transform.SetAsLastSibling();
////        _canvasGroup.blocksRaycasts = false;

////        // ── Swap rider dragon → plain dragon now that the soldier is safe ──────
////        if (wasMounted)
////            mountedDragonDC?.PerformDismount();
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
////        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
////        // CanvasGroup does not shadow the dragon sitting underneath.

////        // ── Raycast all UI elements under the pointer ─────────────────────────
////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        DragonRiderSeat targetSeat = null;
////        DragonController targetDC = null;

////        foreach (var r in results)
////        {
////            // Walk UP to the DragonController from any hit child.
////            var dragon = r.gameObject.GetComponentInParent<DragonController>();
////            if (dragon == null) continue;

////            // Walk DOWN for a seat — may be null on the PLAIN dragon variant
////            // (which has no DragonRiderSeat).  We accept null here and let
////            // PerformMount decide what to do (prefab-swap vs mount-in-place).
////            targetDC = dragon;
////            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
////            break;
////        }

////        // Restore raycast blocking — detection is done.
////        _canvasGroup.blocksRaycasts = true;

////        // A valid mount target is:
////        //   • Any DragonController found (targetDC != null)   AND
////        //   • Either no seat (plain dragon) OR an unoccupied seat
////        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

////        if (targetDC != null && seatFree)
////        {
////            // ── Valid drop ────────────────────────────────────────────────────
////            //
////            // Save the ground home BEFORE mounting so DismountFromDragon()
////            // can return here later.
////            _mountHomeParent = _homeParent;
////            _mountHomePos = _homeAnchoredPosition;

////            // PerformMount handles two cases internally:
////            //   targetSeat == null  → plain dragon  → swap to rider-dragon prefab
////            //   targetSeat != null  → rider dragon   → mount in place
////            targetDC.PerformMount(this, targetSeat);
////        }
////        else if (targetSeat != null && targetSeat.IsOccupied)
////        {
////            // Dragon already has a rider — snap back silently.
////            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
////            SnapBack();
////        }
////        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
////        {
////            // Dropped on empty space — snap back to patrol area.
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DROP OUTCOMES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
////    public void SnapBack()
////    {
////        transform.SetParent(_homeParent, true);
////        _rect.anchoredPosition = _homeAnchoredPosition;
////        _controller?.SetPatrolling(true);
////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by a drop target (WizardBox) after accepting the soldier.
////    /// Resets flags here because SetActive(false) prevents OnEndDrag from firing.
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

////        transform.SetParent(spawnParent, true);

////        if (spawnPosition.HasValue)
////            _rect.anchoredPosition = spawnPosition.Value;

////        _canvasGroup.blocksRaycasts = true;
////        _isDragging = false;

////        RecordHome();
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON MOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by DragonRiderSeat.MountSoldier() — either from the plain dragon's
////    /// own seat (classic fallback) or from the newly spawned rider dragon's seat
////    /// (prefab-swap path via PerformMount).
////    ///
////    /// Order:
////    ///   1. Auto-equip helmet if missing.
////    ///   2. Stop patrol and freeze facing direction.
////    ///   3. Reparent soldier under the seat at seatOffset.
////    ///   4. Switch ALL sprite layers to the Riding animation.
////    /// </summary>
////    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
////    {
////        _currentSeat = seat;

////        // 1. Auto-equip helmet BEFORE reparenting so CharacterEquipment.Equip()
////        //    fires while the soldier is still at canvas-root level.
////        EnsureHelmetEquipped();

////        // 2. Stop patrol and freeze facing direction.
////        _controller?.EnterRidingState();

////        // 3. Reparent under the seat at the configured offset.
////        transform.SetParent(seat.transform, false);
////        _rect.anchoredPosition = seatOffset;
////        RecordHome();

////        // 4. All layers (face, armor, helmet…) switch to their riding sprites.
////        _animator?.SetState(AnimationState.Riding);

////        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAGON DISMOUNT
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Returns the soldier to the ground patrol area and triggers the
////    /// rider-dragon → plain-dragon prefab swap.
////    ///
////    /// Call this from a "Retrieve" button or any dismount game event.
////    ///
////    /// SAFE ORDER enforced internally:
////    ///   1. Capture rider dragon DC before clearing _currentSeat.
////    ///   2. Release seat.
////    ///   3. Reparent soldier to ground home.
////    ///   4. THEN call PerformDismount() so the soldier is no longer a
////    ///      child of the rider dragon when it is destroyed.
////    /// </summary>
////    public void DismountFromDragon()
////    {
////        // Capture the rider dragon DC before we null _currentSeat.
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
////            SnapBack();
////            // Even when snapping back, swap the rider dragon out.
////            riderDragonDC?.PerformDismount();
////            return;
////        }

////        // ── Reparent soldier to ground BEFORE destroying the rider dragon ──────
////        transform.SetParent(_mountHomeParent, false);
////        _rect.anchoredPosition = _mountHomePos;

////        // Restore patrol and facing direction.
////        _controller?.ExitRidingState();

////        // Return all sprite layers to idle animation.
////        _animator?.SetState(AnimationState.Idle);

////        RecordHome();
////        _mountHomeParent = null;   // consumed — prevent stale reuse

////        // ── NOW safe to swap the rider dragon back to the plain dragon ─────────
////        riderDragonDC?.PerformDismount();

////        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELMET AUTO-EQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// If the soldier has no Helmet equipped, looks up the default helmet
////    /// for their Armor in ArmorHelmetTable and equips it automatically.
////    ///
////    /// Lookup order (ArmorHelmetTable.GetDefaultHelmet):
////    ///   1. Entry matching the soldier's equipped armor → paired defaultHelmet.
////    ///   2. fallbackHelmet — used when no armor or no matching entry.
////    ///   3. null           — logs a warning; soldier mounts without helmet.
////    /// </summary>
////    private void EnsureHelmetEquipped()
////    {
////        if (_equipment == null) return;

////        // Already has a helmet — nothing to do.
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
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — SoldierDragDrop
/////
///// Attach to the Soldier prefab root alongside:
/////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DRAG BEHAVIOUR
///// ════════════════════════════════════════════════════════════════════
/////
/////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
/////               top of all panels.
/////               If the soldier was riding a dragon, the seat is released,
/////               the soldier's own visuals are restored (alpha 1), and
/////               DragonController.PerformDismount() hides the rider visual
/////               — all AFTER the soldier is safely at canvas-root.
/////
/////  OnDrag       Moves the soldier under the pointer.
/////
/////  OnEndDrag    Raycasts under the pointer:
/////                 → DragonController with free seat → PerformMount()
/////                 → Occupied seat                  → SnapBack
/////                 → Empty space                    → SnapBack
/////
///// ════════════════════════════════════════════════════════════════════
/////  MOUNT FLOW
///// ════════════════════════════════════════════════════════════════════
/////
/////  SoldierDragDrop.OnEndDrag
/////    └─ DragonController.PerformMount(soldier, seat)
/////         ├─ DragonRiderSeat.MountSoldier(soldier)
/////         │    └─ soldier.MountOnDragon(seat, offset)
/////         │         ├─ EnsureHelmetEquipped()
/////         │         ├─ Reparent soldier under seat
/////         │         ├─ HideOwnVisuals()          ← soldier turns invisible
/////         │         └─ SpriteLayerAnimator → Riding
/////         └─ DragonRiderVisual.ShowForSoldier()  ← dragon shows armored rider
/////
///// ════════════════════════════════════════════════════════════════════
/////  DISMOUNT FLOW
///// ════════════════════════════════════════════════════════════════════
/////
/////  SoldierDragDrop.OnBeginDrag  (soldier dragged off dragon)
/////    ├─ seat.ReleaseSoldier()
/////    ├─ ShowOwnVisuals()                         ← soldier turns visible again
/////    ├─ Reparent soldier to canvas root
/////    └─ DragonController.PerformDismount()       ← dragon hides rider visual
/////
/////  SoldierDragDrop.DismountFromDragon()          (programmatic, e.g. Retrieve button)
/////    ├─ seat.ReleaseSoldier()
/////    ├─ Reparent soldier to ground home
/////    ├─ ShowOwnVisuals()
/////    └─ DragonController.PerformDismount()
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Attach to SoldierPrefab root alongside CanvasGroup,
/////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
/////  3. Root Canvas must have a GraphicRaycaster.
/////  4. An EventSystem must exist in the scene.
/////  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
/////     No Layout Group — it overrides anchoredPosition every frame.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project -> Create -> AreaForge -> Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
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

//    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
//    private DragonRiderSeat _currentSeat;

//    /// <summary>
//    /// Ground parent recorded before mounting so DismountFromDragon() can
//    /// return the soldier to its patrol area, not back to the seat.
//    /// </summary>
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ── Lock State ─────────────────────────────────────────────────────

//    // When true the soldier cannot be dragged off the seat.
//    // Toggled by DragonAttachButton. Reset to false on any dismount.
//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

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
//        if (_isLocked) return;    // Locked to dragon — drag disabled until Attached is clicked again.

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            // Restore soldier's own sprite layers before they become visible again.
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Re-find root canvas every drag — cached value breaks after Retrieve
//        // re-parents the soldier to a different panel.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            return;
//        }

//        // If dismounting, override home with the ground position saved at mount time.
//        RecordHome();
//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;   // consumed — prevent stale reuse
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // ── Make soldier's own visuals visible BEFORE reparenting ─────────────
//        // ShowOwnVisuals() restores alpha to 1 so the soldier is fully visible
//        // while being dragged. The drag-alpha (0.75) is applied right after.
//        if (wasMounted)
//            ShowOwnVisuals();

//        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
//        // The soldier must not be a child of the dragon when PerformDismount
//        // hides the rider visual — ordering doesn't matter for single-prefab,
//        // but kept for clarity and future safety.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // ── Notify dragon to hide its rider visual ────────────────────────────
//        if (wasMounted)
//            mountedDragonDC?.PerformDismount();
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
//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//        // CanvasGroup does not shadow the dragon sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;

//        foreach (var r in results)
//        {
//            var dragon = r.gameObject.GetComponentInParent<DragonController>();
//            if (dragon == null) continue;

//            targetDC = dragon;
//            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//            break;
//        }

//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.alpha = 1f;

//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            // Valid drop on an unoccupied dragon.
//            // Save ground home BEFORE mounting so DismountFromDragon() can return here.
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;

//            targetDC.PerformMount(this, targetSeat);
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            // Seat is occupied — attempt a swap.
//            // The mounted soldier is a child of the seat, so GetComponentInChildren finds them.
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                // Rider is locked (Attached) — swap blocked. Snap this soldier back.
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                // Swap: return current rider home, then mount this soldier.
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
//            // Dropped on empty space — return to patrol area.
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
//    /// Called by a drop target (WizardBox) after accepting the soldier.
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
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    ///
//    /// Order:
//    ///   1. Auto-equip helmet if missing (needed so the rider visual shows
//    ///      the correct helmet immediately after ShowForSoldier() is called).
//    ///   2. Stop patrol and freeze facing direction.
//    ///   3. Reparent soldier under the seat at seatOffset.
//    ///   4. Hide the soldier's own visuals (dragon's rider visual takes over).
//    ///   5. Switch SpriteLayerAnimator to Riding state (drives animation data
//    ///      even while the visuals are hidden, so dismount restores correctly).
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // 1. Auto-equip helmet BEFORE reparenting.
//        EnsureHelmetEquipped();

//        // 2. Stop patrol.
//        _controller?.EnterRidingState();

//        // 3. Reparent under the seat.
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        // 4. Hide soldier's own visuals — the dragon's rider visual shows instead.
//        HideOwnVisuals();

//        // 5. Switch animation state (so riding sprites are ready if ShowOwnVisuals
//        //    is ever called while still parented to the seat, e.g. in a future feature).
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's
//    /// rider visual.
//    ///
//    /// Safe order:
//    ///   1. Capture dragon DC before clearing _currentSeat.
//    ///   2. Release seat.
//    ///   3. Reparent soldier to ground home.
//    ///   4. Restore soldier's visuals.
//    ///   5. Call PerformDismount() on the dragon — hides rider visual.
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

//        // Reparent soldier to ground home.
//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        // Restore patrol and facing direction.
//        _controller?.ExitRidingState();

//        // Restore sprite layers to idle.
//        _animator?.SetState(AnimationState.Idle);

//        // Clear lock so the soldier is draggable again once back on the ground.
//        _isLocked = false;
//        _canvasGroup.interactable = true;

//        // Restore soldier visuals.
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHomeParent = null;   // consumed — prevent stale reuse

//        // Notify dragon to hide its rider visual.
//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ═══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ═══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton when the player clicks Attach / Attached.
//    ///   locked = true  → drag is disabled; swap is also blocked.
//    ///   locked = false → soldier becomes draggable and swappable again.
//    /// Has no effect if the soldier is not currently mounted.
//    /// </summary>
//    public void SetLocked(bool locked)
//    {
//        if (_currentSeat == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//            return;
//        }

//        _isLocked = locked;

//        // interactable = false prevents IBeginDragHandler from firing.
//        // blocksRaycasts stays true so the soldier remains in the seat hierarchy.
//        _canvasGroup.interactable = !locked;

//        Debug.Log($"[SoldierDragDrop] '{name}' is now {(locked ? "LOCKED" : "UNLOCKED")} to dragon seat.");
//    }

//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Hides the soldier's own sprite layers by setting CanvasGroup alpha to 0.
//    /// blocksRaycasts stays TRUE so the soldier remains draggable while mounted.
//    /// Called by MountOnDragon() — the dragon's rider visual displays instead.
//    /// </summary>
//    private void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;   // still draggable even when invisible
//        _canvasGroup.interactable = true;
//    }

//    /// <summary>
//    /// Restores the soldier's own sprite layers to fully visible.
//    /// Called on dismount (drag-off or programmatic) before the soldier
//    /// becomes visible in the world again.
//    /// </summary>
//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// If the soldier has no Helmet equipped, looks up the default helmet for
//    /// their Armor in ArmorHelmetTable and equips it automatically.
//    /// This ensures the dragon's rider visual can show the correct helmet sprite.
//    /// </summary>
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
//}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — SoldierDragDrop
///
/// Attach to the Soldier prefab root alongside:
///   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///
/// ════════════════════════════════════════════════════════════════════
///  DRAG BEHAVIOUR
/// ════════════════════════════════════════════════════════════════════
///
///  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
///               top of all panels.
///               If the soldier was riding a dragon, the seat is released,
///               the soldier's own visuals are restored (alpha 1), and
///               DragonController.PerformDismount() hides the rider visual
///               — all AFTER the soldier is safely at canvas-root.
///
///  OnDrag       Moves the soldier under the pointer.
///
///  OnEndDrag    Raycasts under the pointer:
///                 → DragonController with free seat → PerformMount()
///                 → Occupied seat (unlocked rider)  → Swap riders
///                 → Occupied seat (locked rider)    → SnapBack
///                 → Empty space                    → SnapBack
///
/// ════════════════════════════════════════════════════════════════════
///  MOUNT FLOW
/// ════════════════════════════════════════════════════════════════════
///
///  SoldierDragDrop.OnEndDrag
///    └─ DragonController.PerformMount(soldier, seat)
///         ├─ DragonRiderSeat.MountSoldier(soldier)
///         │    └─ soldier.MountOnDragon(seat, offset)
///         │         ├─ EnsureHelmetEquipped()
///         │         ├─ Reparent soldier under seat
///         │         ├─ HideOwnVisuals()    ← alpha=0, blocksRaycasts=true, interactable=true
///         │         └─ SpriteLayerAnimator → RiderIdle
///         └─ DragonRiderVisual.ShowForSoldier()  ← dragon shows armored rider
///
/// ════════════════════════════════════════════════════════════════════
///  ATTACH / LOCK SYSTEM
/// ════════════════════════════════════════════════════════════════════
///
///  After mount, the soldier is UNLOCKED by default:
///    blocksRaycasts = true  → player can click the rider area and drag
///                              the soldier off the dragon normally.
///
///  DragonController.OnBeginDrag blocks dragon drag while rider is unlocked,
///  so clicking the dragon accidentally doesn't move it.
///
///  After the player clicks the Attach button, SetLocked(true) is called:
///    blocksRaycasts = false → clicks pass through the invisible soldier
///                              down to the dragon body image, letting
///                              the player drag the whole dragon+rider unit
///                              to a FlyZone.
///    interactable   = false → OnBeginDrag on the soldier is suppressed.
///
///  Clicking Attached again calls SetLocked(false) — restores draggability.
///  DismountFromDragon() always resets to unlocked.
///
/// ════════════════════════════════════════════════════════════════════
///  DISMOUNT FLOW
/// ════════════════════════════════════════════════════════════════════
///
///  SoldierDragDrop.OnBeginDrag  (soldier dragged off dragon)
///    ├─ seat.ReleaseSoldier()
///    ├─ ShowOwnVisuals()                         ← soldier turns visible again
///    ├─ Reparent soldier to canvas root
///    └─ DragonController.PerformDismount()       ← dragon hides rider visual
///
///  SoldierDragDrop.DismountFromDragon()          (programmatic, e.g. Retrieve button)
///    ├─ seat.ReleaseSoldier()
///    ├─ Reparent soldier to ground home
///    ├─ ShowOwnVisuals()
///    └─ DragonController.PerformDismount()
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP
/// ════════════════════════════════════════════════════════════════════
///
///  1. Attach to SoldierPrefab root alongside CanvasGroup,
///     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
///  3. Root Canvas must have a GraphicRaycaster.
///  4. An EventSystem must exist in the scene.
///  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
///     No Layout Group — it overrides anchoredPosition every frame.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dragon Mount Settings")]
    [Tooltip("Maps each armor to its default helmet.\n" +
             "Create via: right-click Project -> Create -> AreaForge -> Armor Helmet Table.\n" +
             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
    [SerializeField] private ArmorHelmetTable helmetTable;

    // ── Component References ──────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;    // optional — patrol + flip
    private CharacterEquipment _equipment;    // tracks equipped items
    private SpriteLayerAnimator _animator;    // drives per-layer animation

    // ── Drag State ────────────────────────────────────────────────────────────

    private Canvas _rootCanvas;
    private Transform _homeParent;
    private Vector2 _homeAnchoredPosition;
    private bool _isDragging;

    // ── Dragon Rider State ────────────────────────────────────────────────────

    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
    private DragonRiderSeat _currentSeat;

    /// <summary>
    /// Ground parent recorded before mounting so DismountFromDragon() can
    /// return the soldier to its patrol area, not back to the seat.
    /// </summary>
    private Transform _mountHomeParent;
    private Vector2 _mountHomePos;

    // ── Lock State ────────────────────────────────────────────────────────────

    // When true the soldier cannot be dragged off the seat.
    // Toggled by DragonAttachButton via SetLocked(). Reset to false on any dismount.
    private bool _isLocked = false;

    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
    public bool IsRiding => _currentSeat != null;

    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
    public bool IsLocked => _isLocked;

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
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — BEGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        // Locked to the dragon — drag is disabled until the player clicks
        // Attached (which calls SetLocked(false)). With _isLocked=true the
        // CanvasGroup has interactable=false so Unity shouldn't route drag
        // events here at all, but this is a safety guard.
        if (_isLocked) return;

        // ── Capture mounted dragon before clearing the seat ───────────────────
        bool wasMounted = _currentSeat != null;
        DragonController mountedDragonDC = null;

        if (wasMounted)
        {
            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
            // Restore sprite layers before they become visible again.
            _animator?.SetState(AnimationState.Idle);
        }

        // Re-find root canvas every drag — cached value breaks after Retrieve
        // re-parents the soldier to a different panel.
        _rootCanvas = GetComponentInParent<Canvas>();
        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

        if (_rootCanvas == null)
        {
            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
                           "Make sure the soldier is inside a Canvas.");
            return;
        }

        // If dismounting, override home with the ground position saved at mount time.
        RecordHome();
        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;   // consumed — prevent stale reuse
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // Restore soldier's own visuals BEFORE reparenting so alpha=1 when visible.
        if (wasMounted)
            ShowOwnVisuals();

        // Reparent to root canvas so the soldier draws above all panels.
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha = 0.75f;
        _canvasGroup.blocksRaycasts = false;

        // Notify dragon to hide its rider visual now that the soldier is safe
        // at canvas-root (never call this while soldier is still a child of dragon).
        if (wasMounted)
            mountedDragonDC?.PerformDismount();
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
        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
        // CanvasGroup does not shadow the dragon sitting underneath.

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragonRiderSeat targetSeat = null;
        DragonController targetDC = null;

        foreach (var r in results)
        {
            var dragon = r.gameObject.GetComponentInParent<DragonController>();
            if (dragon == null) continue;

            targetDC = dragon;
            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
            break;
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

        if (targetDC != null && targetSeat != null && seatFree)
        {
            // Valid drop on an unoccupied dragon.
            // Save ground home BEFORE mounting so DismountFromDragon() can return here.
            _mountHomeParent = _homeParent;
            _mountHomePos = _homeAnchoredPosition;

            targetDC.PerformMount(this, targetSeat);
        }
        else if (targetSeat != null && targetSeat.IsOccupied)
        {
            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

            if (currentRider != null && currentRider._isLocked)
            {
                // Rider is locked (Attached) — swap blocked. Snap this soldier back.
                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
                SnapBack();
            }
            else if (currentRider != null)
            {
                // Swap: return current rider home, then mount this soldier.
                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
                _mountHomeParent = _homeParent;
                _mountHomePos = _homeAnchoredPosition;
                currentRider.DismountFromDragon();
                targetDC.PerformMount(this, targetSeat);
            }
            else
            {
                SnapBack();
            }
        }
        else
        {
            // Dropped on empty space — return to patrol area.
            SnapBack();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DROP OUTCOMES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
    public void SnapBack()
    {
        transform.SetParent(_homeParent, worldPositionStays: true);
        _rect.anchoredPosition = _homeAnchoredPosition;
        ShowOwnVisuals();
        _controller?.SetPatrolling(true);
        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by a drop target (WizardBox) after accepting the soldier.
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
    // DRAGON MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
    ///
    /// Order:
    ///   1. Auto-equip helmet if missing.
    ///   2. Stop patrol and freeze facing direction.
    ///   3. Reparent soldier under the seat at seatOffset.
    ///   4. Hide the soldier's own visuals (dragon's rider visual takes over).
    ///      HideOwnVisuals sets blocksRaycasts=true — the UNLOCKED default,
    ///      meaning the player can drag the soldier off immediately without
    ///      needing to click Attach first.
    ///   5. Switch SpriteLayerAnimator to RiderIdle state.
    /// </summary>
    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
    {
        _currentSeat = seat;

        // 1. Auto-equip helmet BEFORE reparenting.
        EnsureHelmetEquipped();

        // 2. Stop patrol.
        _controller?.EnterRidingState();

        // 3. Reparent under the seat.
        transform.SetParent(seat.transform, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        RecordHome();

        // 4. Hide soldier's own visuals — dragon's rider visual shows instead.
        //    blocksRaycasts=true is set here → UNLOCKED default state.
        HideOwnVisuals();

        // 5. Switch animation state.
        _animator?.SetState(AnimationState.RiderIdle);

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the soldier to the ground patrol area and hides the dragon's
    /// rider visual.
    ///
    /// Safe order:
    ///   1. Capture dragon DC before clearing _currentSeat.
    ///   2. Release seat.
    ///   3. Reparent soldier to ground home.
    ///   4. Restore soldier's visuals + unlock.
    ///   5. Call PerformDismount() on the dragon.
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
            ShowOwnVisuals();
            SnapBack();
            riderDragonDC?.PerformDismount();
            return;
        }

        // Reparent soldier to ground home.
        transform.SetParent(_mountHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHomePos;

        // Restore patrol and facing direction.
        _controller?.ExitRidingState();

        // Restore sprite layers to idle.
        _animator?.SetState(AnimationState.Idle);

        // Unlock and restore full visibility + raycast blocking.
        _isLocked = false;
        ShowOwnVisuals();   // sets alpha=1, blocksRaycasts=true, interactable=true

        RecordHome();
        _mountHomeParent = null;   // consumed — prevent stale reuse

        // Notify dragon to hide its rider visual.
        riderDragonDC?.PerformDismount();

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ATTACH LOCK
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Locks or unlocks the soldier to the current dragon seat.
    /// Called by DragonAttachButton when the player clicks Attach / Attached.
    ///
    ///   locked = true
    ///     • _isLocked = true         → OnBeginDrag guard activates
    ///     • interactable   = false   → EventSystem ignores drag events on soldier
    ///     • blocksRaycasts = false   → clicks pass THROUGH to the dragon body
    ///                                  so DragonController.OnBeginDrag fires,
    ///                                  letting the player drag dragon to FlyZone
    ///
    ///   locked = false
    ///     • _isLocked = false        → OnBeginDrag guard deactivates
    ///     • interactable   = true    → drag events fire on soldier again
    ///     • blocksRaycasts = true    → clicks land on the soldier, not dragon,
    ///                                  so the player can drag the soldier off
    ///
    /// Has no effect if the soldier is not currently mounted.
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
            // Clicks pass through the invisible soldier to the dragon below.
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        else
        {
            // Clicks land on the soldier — player can drag them off the dragon.
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // VISUAL SHOW / HIDE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hides the soldier's own sprite layers (alpha 0).
    /// blocksRaycasts=true and interactable=true are preserved so the soldier
    /// is in the UNLOCKED mounted state — draggable off the dragon by default.
    /// Called by MountOnDragon(). The dragon's rider visual displays instead.
    /// </summary>
    private void HideOwnVisuals()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;   // UNLOCKED default: soldier is draggable
        _canvasGroup.interactable = true;
    }

    /// <summary>
    /// Restores the soldier's own sprite layers to fully visible.
    /// Also ensures the CanvasGroup is in the correct unlocked state.
    /// Called on dismount (drag-off or programmatic).
    /// </summary>
    private void ShowOwnVisuals()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELMET AUTO-EQUIP
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// If the soldier has no Helmet equipped, looks up the default helmet for
    /// their Armor in ArmorHelmetTable and equips it automatically.
    /// This ensures the dragon's rider visual shows the correct helmet sprite.
    /// </summary>
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
}