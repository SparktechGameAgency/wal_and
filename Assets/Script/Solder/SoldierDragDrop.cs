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
/////               top of all panels. If the soldier was riding a dragon,
/////               the seat is released and the rider dragon swaps back
/////               to the plain dragon AFTER the soldier is safely at
/////               canvas-root (so the soldier is not destroyed with the
/////               rider dragon GameObject).
/////
/////  OnDrag       Moves the soldier under the pointer.
/////
/////  OnEndDrag    Raycasts under the pointer:
/////                 → DragonController found → PerformMount()
/////                    (which either swaps to rider prefab or falls back
/////                     to classic in-place mount depending on setup)
/////                 → Seat occupied          → SnapBack
/////                 → Empty space            → SnapBack
/////
///// ════════════════════════════════════════════════════════════════════
/////  MOUNT / DISMOUNT
///// ════════════════════════════════════════════════════════════════════
/////
/////  OnEndDrag routes through DragonController.PerformMount() instead of
/////  calling DragonRiderSeat.MountSoldier() directly. This lets the dragon
/////  controller decide whether to swap prefabs or mount in place.
/////
/////  When the soldier is dragged off the rider dragon (OnBeginDrag with
/////  wasMounted=true), DragonController.PerformDismount() is called AFTER
/////  the soldier is already at canvas-root — never while still a child.
/////
/////  DismountFromDragon() (programmatic dismount, e.g. Retrieve button)
/////  follows the same safe order: reparent soldier → then PerformDismount.
/////
///// ════════════════════════════════════════════════════════════════════
/////  HELMET AUTO-EQUIP
///// ════════════════════════════════════════════════════════════════════
/////
/////  If the soldier has no Helmet equipped when they mount, the system
/////  looks up the correct default in ArmorHelmetTable (matched to their
/////  Armor) and calls CharacterEquipment.Equip() automatically.
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
/////  5. Spawn panel must be a RectTransform + Image (Raycast Target ON).
/////     No Layout Group — it overrides anchoredPosition every frame.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
//    [SerializeField] private ArmorHelmetTable helmetTable;

//    // ── Component References ──────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;     // optional — patrol + flip
//    private CharacterEquipment _equipment;      // tracks equipped items
//    private SpriteLayerAnimator _animator;       // drives per-layer animation

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

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();    // optional
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

//        // ── If riding, release the seat and capture the rider dragon DC ───────
//        //
//        // We capture the DragonController BEFORE clearing _currentSeat so we
//        // can call PerformDismount() later (after the soldier is safely at
//        // canvas-root — never while still a child of the rider dragon).
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
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

//        // RecordHome() would save the rider seat as home while mounted.
//        // Override with the original ground home that was stored at mount time.
//        RecordHome();
//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;   // consumed — prevent stale reuse
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
//        // The soldier must not be a child of the rider dragon when it is
//        // destroyed by PerformDismount, or the soldier would be destroyed too.
//        transform.SetParent(_rootCanvas.transform, true);
//        transform.SetAsLastSibling();
//        _canvasGroup.blocksRaycasts = false;

//        // ── Swap rider dragon → plain dragon now that the soldier is safe ──────
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

//        // ── Raycast all UI elements under the pointer ─────────────────────────
//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;

//        foreach (var r in results)
//        {
//            // Walk UP to the DragonController from any hit child
//            // (body, wing, seat root — all share the dragon root as an ancestor).
//            var dragon = r.gameObject.GetComponentInParent<DragonController>();
//            if (dragon == null) continue;

//            // Walk DOWN to the seat from the dragon root.
//            var seat = dragon.GetComponentInChildren<DragonRiderSeat>();
//            if (seat == null) continue;

//            targetDC = dragon;
//            targetSeat = seat;
//            break;
//        }

//        // Restore raycast blocking — detection is done.
//        _canvasGroup.blocksRaycasts = true;

//        if (targetSeat != null && !targetSeat.IsOccupied)
//        {
//            // ── Valid drop on an unoccupied dragon ────────────────────────────
//            //
//            // Save the ground home BEFORE mounting so DismountFromDragon()
//            // can return here later.
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;

//            // Route through the DragonController so it can decide whether to
//            // swap prefabs (plain → rider variant) or mount in place.
//            targetDC.PerformMount(this, targetSeat);
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            // Dragon already has a rider — snap back silently.
//            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
//            SnapBack();
//        }
//        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
//        {
//            // Dropped on empty space — snap back to patrol area.
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        transform.SetParent(_homeParent, true);
//        _rect.anchoredPosition = _homeAnchoredPosition;
//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by a drop target (WizardBox) after accepting the soldier.
//    /// Resets flags here because SetActive(false) prevents OnEndDrag from firing.
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

//        transform.SetParent(spawnParent, true);

//        if (spawnPosition.HasValue)
//            _rect.anchoredPosition = spawnPosition.Value;

//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        RecordHome();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() — either from the plain dragon's
//    /// own seat (classic fallback) or from the newly spawned rider dragon's seat
//    /// (prefab-swap path via PerformMount).
//    ///
//    /// Order:
//    ///   1. Auto-equip helmet if missing.
//    ///   2. Stop patrol and freeze facing direction.
//    ///   3. Reparent soldier under the seat at seatOffset.
//    ///   4. Switch ALL sprite layers to the Riding animation.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // 1. Auto-equip helmet BEFORE reparenting so CharacterEquipment.Equip()
//        //    fires while the soldier is still at canvas-root level.
//        EnsureHelmetEquipped();

//        // 2. Stop patrol and freeze facing direction.
//        _controller?.EnterRidingState();

//        // 3. Reparent under the seat at the configured offset.
//        transform.SetParent(seat.transform, false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        // 4. All layers (face, armor, helmet…) switch to their riding sprites.
//        _animator?.SetState(AnimationState.Riding);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and triggers the
//    /// rider-dragon → plain-dragon prefab swap.
//    ///
//    /// Call this from a "Retrieve" button or any dismount game event.
//    ///
//    /// SAFE ORDER enforced internally:
//    ///   1. Capture rider dragon DC before clearing _currentSeat.
//    ///   2. Release seat.
//    ///   3. Reparent soldier to ground home.
//    ///   4. THEN call PerformDismount() so the soldier is no longer a
//    ///      child of the rider dragon when it is destroyed.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        // Capture the rider dragon DC before we null _currentSeat.
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
//            SnapBack();
//            // Even when snapping back, swap the rider dragon out.
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        // ── Reparent soldier to ground BEFORE destroying the rider dragon ──────
//        transform.SetParent(_mountHomeParent, false);
//        _rect.anchoredPosition = _mountHomePos;

//        // Restore patrol and facing direction.
//        _controller?.ExitRidingState();

//        // Return all sprite layers to idle animation.
//        _animator?.SetState(AnimationState.Idle);

//        RecordHome();
//        _mountHomeParent = null;   // consumed — prevent stale reuse

//        // ── NOW safe to swap the rider dragon back to the plain dragon ─────────
//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// If the soldier has no Helmet equipped, looks up the default helmet
//    /// for their Armor in ArmorHelmetTable and equips it automatically.
//    ///
//    /// Lookup order (ArmorHelmetTable.GetDefaultHelmet):
//    ///   1. Entry matching the soldier's equipped armor → paired defaultHelmet.
//    ///   2. fallbackHelmet — used when no armor or no matching entry.
//    ///   3. null           — logs a warning; soldier mounts without helmet.
//    /// </summary>
//    private void EnsureHelmetEquipped()
//    {
//        if (_equipment == null) return;

//        // Already has a helmet — nothing to do.
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
///               top of all panels. If the soldier was riding a dragon,
///               the seat is released and the rider dragon swaps back
///               to the plain dragon AFTER the soldier is safely at
///               canvas-root (so the soldier is not destroyed with the
///               rider dragon GameObject).
///
///  OnDrag       Moves the soldier under the pointer.
///
///  OnEndDrag    Raycasts under the pointer:
///                 → DragonController found → PerformMount()
///                    (which either swaps to rider prefab or falls back
///                     to classic in-place mount depending on setup)
///                 → Seat occupied          → SnapBack
///                 → Empty space            → SnapBack
///
/// ════════════════════════════════════════════════════════════════════
///  MOUNT / DISMOUNT
/// ════════════════════════════════════════════════════════════════════
///
///  OnEndDrag routes through DragonController.PerformMount() instead of
///  calling DragonRiderSeat.MountSoldier() directly. This lets the dragon
///  controller decide whether to swap prefabs or mount in place.
///
///  When the soldier is dragged off the rider dragon (OnBeginDrag with
///  wasMounted=true), DragonController.PerformDismount() is called AFTER
///  the soldier is already at canvas-root — never while still a child.
///
///  DismountFromDragon() (programmatic dismount, e.g. Retrieve button)
///  follows the same safe order: reparent soldier → then PerformDismount.
///
/// ════════════════════════════════════════════════════════════════════
///  HELMET AUTO-EQUIP
/// ════════════════════════════════════════════════════════════════════
///
///  If the soldier has no Helmet equipped when they mount, the system
///  looks up the correct default in ArmorHelmetTable (matched to their
///  Armor) and calls CharacterEquipment.Equip() automatically.
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
///  5. Spawn panel must be a RectTransform + Image (Raycast Target ON).
///     No Layout Group — it overrides anchoredPosition every frame.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dragon Mount Settings")]
    [Tooltip("Maps each armor to its default helmet.\n" +
             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
    [SerializeField] private ArmorHelmetTable helmetTable;

    // ── Component References ──────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;     // optional — patrol + flip
    private CharacterEquipment _equipment;      // tracks equipped items
    private SpriteLayerAnimator _animator;       // drives per-layer animation

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

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _controller = GetComponent<SoldierController>();    // optional
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

        // ── If riding, release the seat and capture the rider dragon DC ───────
        //
        // We capture the DragonController BEFORE clearing _currentSeat so we
        // can call PerformDismount() later (after the soldier is safely at
        // canvas-root — never while still a child of the rider dragon).
        bool wasMounted = _currentSeat != null;
        DragonController mountedDragonDC = null;

        if (wasMounted)
        {
            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
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

        // RecordHome() would save the rider seat as home while mounted.
        // Override with the original ground home that was stored at mount time.
        RecordHome();
        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;   // consumed — prevent stale reuse
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // ── Reparent to root canvas BEFORE calling PerformDismount() ──────────
        // The soldier must not be a child of the rider dragon when it is
        // destroyed by PerformDismount, or the soldier would be destroyed too.
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();
        _canvasGroup.blocksRaycasts = false;

        // ── Swap rider dragon → plain dragon now that the soldier is safe ──────
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

        // ── Raycast all UI elements under the pointer ─────────────────────────
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragonRiderSeat targetSeat = null;
        DragonController targetDC = null;

        foreach (var r in results)
        {
            // Walk UP to the DragonController from any hit child.
            var dragon = r.gameObject.GetComponentInParent<DragonController>();
            if (dragon == null) continue;

            // Walk DOWN for a seat — may be null on the PLAIN dragon variant
            // (which has no DragonRiderSeat).  We accept null here and let
            // PerformMount decide what to do (prefab-swap vs mount-in-place).
            targetDC = dragon;
            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
            break;
        }

        // Restore raycast blocking — detection is done.
        _canvasGroup.blocksRaycasts = true;

        // A valid mount target is:
        //   • Any DragonController found (targetDC != null)   AND
        //   • Either no seat (plain dragon) OR an unoccupied seat
        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

        if (targetDC != null && seatFree)
        {
            // ── Valid drop ────────────────────────────────────────────────────
            //
            // Save the ground home BEFORE mounting so DismountFromDragon()
            // can return here later.
            _mountHomeParent = _homeParent;
            _mountHomePos = _homeAnchoredPosition;

            // PerformMount handles two cases internally:
            //   targetSeat == null  → plain dragon  → swap to rider-dragon prefab
            //   targetSeat != null  → rider dragon   → mount in place
            targetDC.PerformMount(this, targetSeat);
        }
        else if (targetSeat != null && targetSeat.IsOccupied)
        {
            // Dragon already has a rider — snap back silently.
            Debug.Log("[SoldierDragDrop] Dragon seat is occupied — snapping back.");
            SnapBack();
        }
        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
        {
            // Dropped on empty space — snap back to patrol area.
            SnapBack();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DROP OUTCOMES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
    public void SnapBack()
    {
        transform.SetParent(_homeParent, true);
        _rect.anchoredPosition = _homeAnchoredPosition;
        _controller?.SetPatrolling(true);
        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by a drop target (WizardBox) after accepting the soldier.
    /// Resets flags here because SetActive(false) prevents OnEndDrag from firing.
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

        transform.SetParent(spawnParent, true);

        if (spawnPosition.HasValue)
            _rect.anchoredPosition = spawnPosition.Value;

        _canvasGroup.blocksRaycasts = true;
        _isDragging = false;

        RecordHome();
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by DragonRiderSeat.MountSoldier() — either from the plain dragon's
    /// own seat (classic fallback) or from the newly spawned rider dragon's seat
    /// (prefab-swap path via PerformMount).
    ///
    /// Order:
    ///   1. Auto-equip helmet if missing.
    ///   2. Stop patrol and freeze facing direction.
    ///   3. Reparent soldier under the seat at seatOffset.
    ///   4. Switch ALL sprite layers to the Riding animation.
    /// </summary>
    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
    {
        _currentSeat = seat;

        // 1. Auto-equip helmet BEFORE reparenting so CharacterEquipment.Equip()
        //    fires while the soldier is still at canvas-root level.
        EnsureHelmetEquipped();

        // 2. Stop patrol and freeze facing direction.
        _controller?.EnterRidingState();

        // 3. Reparent under the seat at the configured offset.
        transform.SetParent(seat.transform, false);
        _rect.anchoredPosition = seatOffset;
        RecordHome();

        // 4. All layers (face, armor, helmet…) switch to their riding sprites.
        _animator?.SetState(AnimationState.Riding);

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON DISMOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the soldier to the ground patrol area and triggers the
    /// rider-dragon → plain-dragon prefab swap.
    ///
    /// Call this from a "Retrieve" button or any dismount game event.
    ///
    /// SAFE ORDER enforced internally:
    ///   1. Capture rider dragon DC before clearing _currentSeat.
    ///   2. Release seat.
    ///   3. Reparent soldier to ground home.
    ///   4. THEN call PerformDismount() so the soldier is no longer a
    ///      child of the rider dragon when it is destroyed.
    /// </summary>
    public void DismountFromDragon()
    {
        // Capture the rider dragon DC before we null _currentSeat.
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
            SnapBack();
            // Even when snapping back, swap the rider dragon out.
            riderDragonDC?.PerformDismount();
            return;
        }

        // ── Reparent soldier to ground BEFORE destroying the rider dragon ──────
        transform.SetParent(_mountHomeParent, false);
        _rect.anchoredPosition = _mountHomePos;

        // Restore patrol and facing direction.
        _controller?.ExitRidingState();

        // Return all sprite layers to idle animation.
        _animator?.SetState(AnimationState.Idle);

        RecordHome();
        _mountHomeParent = null;   // consumed — prevent stale reuse

        // ── NOW safe to swap the rider dragon back to the plain dragon ─────────
        riderDragonDC?.PerformDismount();

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELMET AUTO-EQUIP
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// If the soldier has no Helmet equipped, looks up the default helmet
    /// for their Armor in ArmorHelmetTable and equips it automatically.
    ///
    /// Lookup order (ArmorHelmetTable.GetDefaultHelmet):
    ///   1. Entry matching the soldier's equipped armor → paired defaultHelmet.
    ///   2. fallbackHelmet — used when no armor or no matching entry.
    ///   3. null           — logs a warning; soldier mounts without helmet.
    /// </summary>
    private void EnsureHelmetEquipped()
    {
        if (_equipment == null) return;

        // Already has a helmet — nothing to do.
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