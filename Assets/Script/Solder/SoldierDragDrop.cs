////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - SoldierDragDrop
///////
/////// ── BUG FIX: Soldier never flips after drag/retrieve ──────────────────────────
///////   Every SetParent(x, worldPositionStays: true) call makes Unity recompute
///////   localScale to preserve world scale across parents with different Canvas
///////   Scaler factors. This corrupts the sign of localScale.x that SoldierController
///////   uses to track facing direction. Fix: call _controller.RefreshFlip() after
///////   every re-parent to restore the correct sign from the authoritative _direction.
///////
/////// ── BUG FIX: Patrol area shifts after retrieve ────────────────────────────────
///////   Handled inside SoldierController.SetPatrolling(true) — it now always
///////   recalculates patrol bounds from the soldier's current position before
///////   resuming, so the patrol range is always centred on where the soldier stands.
///////
/////// ── BUG FIX: Can't re-drag after retrieve ─────────────────────────────────────
///////   Unity skips OnEndDrag on a disabled GameObject. AcceptSoldier calls
///////   SetActive(false), so _isDragging and blocksRaycasts stay stuck.
///////   Fix: reset both flags in OnSuccessfulDrop() (called before SetActive(false)).
///////
/////// ── Setup ──────────────────────────────────────────────────────────────────────
///////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
///////   2. The root Canvas must have a GraphicRaycaster.
///////   3. An EventSystem must exist in the scene.
///////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
///////      Do NOT add any Layout Group — it overrides anchoredPosition every frame
///////      and stops the soldier from moving (animations play, position is frozen).
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ─── State ────────────────────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
////    private bool _isDragging;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////        _rect = GetComponent<RectTransform>();
////        _controller = GetComponent<SoldierController>();
////    }

////    private void Start()
////    {
////        RecordHome();
////    }

////    // ─── Drag Handlers ────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // Re-find root canvas every drag — cached value breaks after retrieve
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

////        RecordHome();

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // ── Re-parent to root canvas ──────────────────────────────────────────
////        transform.SetParent(_rootCanvas.transform, true);
////        transform.SetAsLastSibling();

////        _canvasGroup.blocksRaycasts = false;
////    }

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;
////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////    }

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        // NOTE: This may NOT fire if the drop target called SetActive(false) on us
////        // inside OnDrop. OnSuccessfulDrop() also resets these flags for that case.
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;

////        if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
////            SnapBack();
////    }

////    // ─── Drop Outcomes ────────────────────────────────────────────────────────

////    /// <summary>
////    /// Returns the soldier to its home position and resumes patrol.
////    /// Called automatically when the drag ends over empty space.
////    /// </summary>
////    public void SnapBack()
////    {
////        transform.SetParent(_homeParent, true);
////        _rect.anchoredPosition = _homeAnchoredPosition;

////        // RefreshFlip re-applies the correct localScale.x sign after SetParent,
////        // which can corrupt localScale when parent Canvas Scalers differ.
////        //_controller?.RefreshFlip();
////        _controller?.SetPatrolling(true);

////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by the drop target (WizardBox) after accepting the soldier.
////    ///
////    /// Resets _isDragging and blocksRaycasts HERE (not just in OnEndDrag) because
////    /// the drop target calls SetActive(false) immediately after, which prevents
////    /// Unity from delivering OnEndDrag to the disabled GameObject.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;
////        _controller?.SetPatrolling(false);
////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////    }

////    /// <summary>
////    /// Call this from WizardBox "Retrieve" instead of directly calling SetParent.
////    ///
////    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
////    ///   spawnPosition — optional anchoredPosition override inside that parent
////    ///
////    /// What it does:
////    ///   • Re-parents soldier to spawnParent
////    ///   • Optionally moves to spawnPosition
////    ///   • Y-rotation flip is unaffected by SetParent — no refresh needed
////    ///   • Calls SetPatrolling(true) which recalculates patrol bounds from
////    ///     the soldier's new position before resuming
////    ///   • Resets drag state flags
////    ///   • Records new home for next drag's snap-back
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

////        // Record new home so the next drag's SnapBack comes back here.
////        RecordHome();

////        // SetPatrolling(true) internally recalculates patrol bounds from the
////        // soldier's current anchoredPosition, fixing the "patrol area drifts"
////        // bug after the soldier lands at a new position.
////        //_controller?.RefreshFlip();
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    private void RecordHome()
////    {
////        _homeParent = transform.parent;
////        _homeAnchoredPosition = _rect.anchoredPosition;
////    }
////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - SoldierDragDrop
///////
/////// ── BUG FIX: Soldier never flips after drag/retrieve ──────────────────────────
///////   Every SetParent(x, worldPositionStays: true) call makes Unity recompute
///////   localScale to preserve world scale across parents with different Canvas
///////   Scaler factors. This corrupts the sign of localScale.x that SoldierController
///////   uses to track facing direction. Fix: call _controller.RefreshFlip() after
///////   every re-parent to restore the correct sign from the authoritative _direction.
///////
/////// ── BUG FIX: Patrol area shifts after retrieve ────────────────────────────────
///////   Handled inside SoldierController.SetPatrolling(true) — it now always
///////   recalculates patrol bounds from the soldier's current position before
///////   resuming, so the patrol range is always centred on where the soldier stands.
///////
/////// ── BUG FIX: Can't re-drag after retrieve ─────────────────────────────────────
///////   Unity skips OnEndDrag on a disabled GameObject. AcceptSoldier calls
///////   SetActive(false), so _isDragging and blocksRaycasts stay stuck.
///////   Fix: reset both flags in OnSuccessfulDrop() (called before SetActive(false)).
///////
/////// ── Setup ──────────────────────────────────────────────────────────────────────
///////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
///////   2. The root Canvas must have a GraphicRaycaster.
///////   3. An EventSystem must exist in the scene.
///////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
///////      Do NOT add any Layout Group — it overrides anchoredPosition every frame
///////      and stops the soldier from moving (animations play, position is frozen).
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ─── State ────────────────────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;

////    private Canvas _rootCanvas;
////    private Transform _homeParent;
////    private Vector2 _homeAnchoredPosition;
////    private bool _isDragging;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////        _rect = GetComponent<RectTransform>();
////        _controller = GetComponent<SoldierController>();
////    }

////    private void Start()
////    {
////        RecordHome();
////    }

////    // ─── Drag Handlers ────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // Re-find root canvas every drag — cached value breaks after retrieve
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

////        RecordHome();

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // ── Re-parent to root canvas ──────────────────────────────────────────
////        transform.SetParent(_rootCanvas.transform, true);
////        transform.SetAsLastSibling();

////        _canvasGroup.blocksRaycasts = false;
////    }

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;
////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////    }

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        // NOTE: This may NOT fire if the drop target called SetActive(false) on us
////        // inside OnDrop. OnSuccessfulDrop() also resets these flags for that case.
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;

////        if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
////            SnapBack();
////    }

////    // ─── Drop Outcomes ────────────────────────────────────────────────────────

////    /// <summary>
////    /// Returns the soldier to its home position and resumes patrol.
////    /// Called automatically when the drag ends over empty space.
////    /// </summary>
////    public void SnapBack()
////    {
////        transform.SetParent(_homeParent, true);
////        _rect.anchoredPosition = _homeAnchoredPosition;

////        // SetPatrolling(true) recalculates patrol bounds from current position
////        // before resuming, so the patrol range is centred on the soldier.
////        _controller?.SetPatrolling(true);

////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by the drop target (WizardBox) after accepting the soldier.
////    ///
////    /// Resets _isDragging and blocksRaycasts HERE (not just in OnEndDrag) because
////    /// the drop target calls SetActive(false) immediately after, which prevents
////    /// Unity from delivering OnEndDrag to the disabled GameObject.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;
////        _controller?.SetPatrolling(false);
////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////    }

////    /// <summary>
////    /// Call this from WizardBox "Retrieve" instead of directly calling SetParent.
////    ///
////    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
////    ///   spawnPosition — optional anchoredPosition override inside that parent
////    ///
////    /// What it does:
////    ///   • Re-parents soldier to spawnParent
////    ///   • Optionally moves to spawnPosition
////    ///   • Y-rotation flip is unaffected by SetParent — no refresh needed
////    ///   • Calls SetPatrolling(true) which recalculates patrol bounds from
////    ///     the soldier's new position before resuming
////    ///   • Resets drag state flags
////    ///   • Records new home for next drag's snap-back
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

////        // Record new home so the next drag's SnapBack comes back here.
////        RecordHome();

////        // SetPatrolling(true) internally recalculates patrol bounds from the
////        // soldier's current anchoredPosition, fixing the "patrol area drifts"
////        // bug after the soldier lands at a new position.
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ─── Helper ───────────────────────────────────────────────────────────────

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
///// AREA FORGE - SoldierDragDrop
/////
///// ── BUG FIX: Soldier never flips after drag/retrieve ──────────────────────────
/////   Every SetParent(x, worldPositionStays: true) call makes Unity recompute
/////   localScale to preserve world scale across parents with different Canvas
/////   Scaler factors. This corrupts the sign of localScale.x that SoldierController
/////   uses to track facing direction. Fix: call _controller.RefreshFlip() after
/////   every re-parent to restore the correct sign from the authoritative _direction.
/////
///// ── BUG FIX: Patrol area shifts after retrieve ────────────────────────────────
/////   Handled inside SoldierController.SetPatrolling(true) — it now always
/////   recalculates patrol bounds from the soldier's current position before
/////   resuming, so the patrol range is always centred on where the soldier stands.
/////
///// ── BUG FIX: Can't re-drag after retrieve ─────────────────────────────────────
/////   Unity skips OnEndDrag on a disabled GameObject. AcceptSoldier calls
/////   SetActive(false), so _isDragging and blocksRaycasts stay stuck.
/////   Fix: reset both flags in OnSuccessfulDrop() (called before SetActive(false)).
/////
///// ── Setup ──────────────────────────────────────────────────────────────────────
/////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
/////   2. The root Canvas must have a GraphicRaycaster.
/////   3. An EventSystem must exist in the scene.
/////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
/////      Do NOT add any Layout Group — it overrides anchoredPosition every frame
/////      and stops the soldier from moving (animations play, position is frozen).
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ─── State ────────────────────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
//    private bool _isDragging;

//    // ─── Dragon Rider State ───────────────────────────────────────────────────

//    // The seat this soldier is currently riding on (null if on the ground).
//    private DragonRiderSeat _currentSeat;

//    // The home parent/position recorded BEFORE mounting — used by DismountFromDragon
//    // to send the soldier back to the patrol area, not to the seat.
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();
//    }

//    private void Start()
//    {
//        RecordHome();
//    }

//    // ─── Drag Handlers ────────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // ── If riding a dragon, release the seat before lifting off ──────────
//        // Track whether we came from a mount so we can fix the snap-back home below.
//        bool wasMounted = _currentSeat != null;
//        if (wasMounted)
//        {
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        // Re-find root canvas every drag — cached value breaks after retrieve
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

//        // RecordHome() saves transform.parent as home. When dismounting, the
//        // current parent is still the dragon seat — so home would wrongly become
//        // the seat, and SnapBack() would re-attach the soldier to the dragon.
//        // Fix: after RecordHome(), override with the original ground home that
//        // was stored in _mountHomeParent at the time of mounting.
//        RecordHome();
//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;   // consumed — prevent stale reuse
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // ── Re-parent to root canvas ──────────────────────────────────────────
//        transform.SetParent(_rootCanvas.transform, true);
//        transform.SetAsLastSibling();

//        _canvasGroup.blocksRaycasts = false;
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // NOTE: This may NOT fire if the drop target called SetActive(false) on us
//        // inside OnDrop. OnSuccessfulDrop() also resets these flags for that case.
//        _isDragging = false;
//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's own
//        // CanvasGroup doesn't shadow the seat/zone underneath.

//        // ── Raycast to find a DragonRiderSeat under the pointer ──────────────
//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        foreach (var r in results)
//        {
//            targetSeat = r.gameObject.GetComponentInParent<DragonRiderSeat>();
//            if (targetSeat != null) break;
//        }

//        // Restore raycast blocking now that detection is done
//        _canvasGroup.blocksRaycasts = true;

//        if (targetSeat != null && !targetSeat.IsOccupied)
//        {
//            // ── Dropped on an empty dragon seat → mount ───────────────────────
//            // Save ground home BEFORE mounting so DismountFromDragon can return here.
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetSeat.MountSoldier(this);
//        }
//        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
//        {
//            // ── Dropped on empty space → snap back ───────────────────────────
//            SnapBack();
//        }
//    }

//    // ─── Drop Outcomes ────────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns the soldier to its home position and resumes patrol.
//    /// Called automatically when the drag ends over empty space.
//    /// </summary>
//    public void SnapBack()
//    {
//        transform.SetParent(_homeParent, true);
//        _rect.anchoredPosition = _homeAnchoredPosition;

//        // RefreshFlip re-applies the correct localScale.x sign after SetParent,
//        // which can corrupt localScale when parent Canvas Scalers differ.
//        //_controller?.RefreshFlip();
//        _controller?.SetPatrolling(true);

//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by the drop target (WizardBox) after accepting the soldier.
//    ///
//    /// Resets _isDragging and blocksRaycasts HERE (not just in OnEndDrag) because
//    /// the drop target calls SetActive(false) immediately after, which prevents
//    /// Unity from delivering OnEndDrag to the disabled GameObject.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Call this from WizardBox "Retrieve" instead of directly calling SetParent.
//    ///
//    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
//    ///   spawnPosition — optional anchoredPosition override inside that parent
//    ///
//    /// What it does:
//    ///   • Re-parents soldier to spawnParent
//    ///   • Optionally moves to spawnPosition
//    ///   • Y-rotation flip is unaffected by SetParent — no refresh needed
//    ///   • Calls SetPatrolling(true) which recalculates patrol bounds from
//    ///     the soldier's new position before resuming
//    ///   • Resets drag state flags
//    ///   • Records new home for next drag's snap-back
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

//        // Record new home so the next drag's SnapBack comes back here.
//        RecordHome();

//        // SetPatrolling(true) internally recalculates patrol bounds from the
//        // soldier's current anchoredPosition, fixing the "patrol area drifts"
//        // bug after the soldier lands at a new position.
//        //_controller?.RefreshFlip();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ─── Dragon Mount / Dismount ─────────────────────────────────────────────────

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier.
//    /// Reparents the soldier to the seat, positions it at seatOffset,
//    /// stops patrol, and resets the visual flip so dragon's flip takes over.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // Stop patrolling — soldier sits still while riding
//        _controller?.SetPatrolling(false);

//        // Reset the soldier's own visual flip to neutral.
//        // The dragon's localScale flip is inherited through the hierarchy,
//        // so the soldier automatically faces the correct direction.
//        _controller?.ResetFlipForMount();

//        // Reparent to the seat (keeps world scale stable with worldPositionStays:false)
//        transform.SetParent(seat.transform, false);
//        _rect.anchoredPosition = seatOffset;

//        // Record home as the seat position so re-drags snap back here if needed
//        RecordHome();

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on dragon seat '{seat.name}'.");
//    }

//    /// <summary>
//    /// Returns the soldier to the ground patrol area it came from.
//    /// Call this from a "Retrieve" button or any game event that dismounts the rider.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        if (_currentSeat != null)
//        {
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        if (_mountHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home recorded — snapping to current home.");
//            SnapBack();
//            return;
//        }

//        transform.SetParent(_mountHomeParent, false);
//        _rect.anchoredPosition = _mountHomePos;

//        // Restore facing direction for ground patrol
//        _controller?.RefreshFlip();
//        _controller?.SetPatrolling(true);

//        // Update home so the next drag snaps back to the patrol area
//        RecordHome();

//        // Clear mount home so we don't accidentally reuse a stale value
//        _mountHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ─── Helper ───────────────────────────────────────────────────────────────

//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }
//}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - SoldierDragDrop
/////
///// ── BUG FIX: Soldier never flips after drag/retrieve ──────────────────────────
/////   Every SetParent(x, worldPositionStays: true) call makes Unity recompute
/////   localScale to preserve world scale across parents with different Canvas
/////   Scaler factors. This corrupts the sign of localScale.x that SoldierController
/////   uses to track facing direction. Fix: call _controller.RefreshFlip() after
/////   every re-parent to restore the correct sign from the authoritative _direction.
/////
///// ── BUG FIX: Patrol area shifts after retrieve ────────────────────────────────
/////   Handled inside SoldierController.SetPatrolling(true) — it now always
/////   recalculates patrol bounds from the soldier's current position before
/////   resuming, so the patrol range is always centred on where the soldier stands.
/////
///// ── BUG FIX: Can't re-drag after retrieve ─────────────────────────────────────
/////   Unity skips OnEndDrag on a disabled GameObject. AcceptSoldier calls
/////   SetActive(false), so _isDragging and blocksRaycasts stay stuck.
/////   Fix: reset both flags in OnSuccessfulDrop() (called before SetActive(false)).
/////
///// ── Setup ──────────────────────────────────────────────────────────────────────
/////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
/////   2. The root Canvas must have a GraphicRaycaster.
/////   3. An EventSystem must exist in the scene.
/////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
/////      Do NOT add any Layout Group — it overrides anchoredPosition every frame
/////      and stops the soldier from moving (animations play, position is frozen).
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ─── State ────────────────────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
//    private bool _isDragging;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();
//    }

//    private void Start()
//    {
//        RecordHome();
//    }

//    // ─── Drag Handlers ────────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Re-find root canvas every drag — cached value breaks after retrieve
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

//        RecordHome();

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // ── Re-parent to root canvas ──────────────────────────────────────────
//        transform.SetParent(_rootCanvas.transform, true);
//        transform.SetAsLastSibling();

//        _canvasGroup.blocksRaycasts = false;
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // NOTE: This may NOT fire if the drop target called SetActive(false) on us
//        // inside OnDrop. OnSuccessfulDrop() also resets these flags for that case.
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;

//        if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
//            SnapBack();
//    }

//    // ─── Drop Outcomes ────────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns the soldier to its home position and resumes patrol.
//    /// Called automatically when the drag ends over empty space.
//    /// </summary>
//    public void SnapBack()
//    {
//        transform.SetParent(_homeParent, true);
//        _rect.anchoredPosition = _homeAnchoredPosition;

//        // SetPatrolling(true) recalculates patrol bounds from current position
//        // before resuming, so the patrol range is centred on the soldier.
//        _controller?.SetPatrolling(true);

//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by the drop target (WizardBox) after accepting the soldier.
//    ///
//    /// Resets _isDragging and blocksRaycasts HERE (not just in OnEndDrag) because
//    /// the drop target calls SetActive(false) immediately after, which prevents
//    /// Unity from delivering OnEndDrag to the disabled GameObject.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Call this from WizardBox "Retrieve" instead of directly calling SetParent.
//    ///
//    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
//    ///   spawnPosition — optional anchoredPosition override inside that parent
//    ///
//    /// What it does:
//    ///   • Re-parents soldier to spawnParent
//    ///   • Optionally moves to spawnPosition
//    ///   • Y-rotation flip is unaffected by SetParent — no refresh needed
//    ///   • Calls SetPatrolling(true) which recalculates patrol bounds from
//    ///     the soldier's new position before resuming
//    ///   • Resets drag state flags
//    ///   • Records new home for next drag's snap-back
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

//        // Record new home so the next drag's SnapBack comes back here.
//        RecordHome();

//        // SetPatrolling(true) internally recalculates patrol bounds from the
//        // soldier's current anchoredPosition, fixing the "patrol area drifts"
//        // bug after the soldier lands at a new position.
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ─── Helper ───────────────────────────────────────────────────────────────

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
/// AREA FORGE - SoldierDragDrop
///
/// ── BUG FIX: Soldier never flips after drag/retrieve ──────────────────────────
///   Every SetParent(x, worldPositionStays: true) call makes Unity recompute
///   localScale to preserve world scale across parents with different Canvas
///   Scaler factors. This corrupts the sign of localScale.x that SoldierController
///   uses to track facing direction. Fix: call _controller.RefreshFlip() after
///   every re-parent to restore the correct sign from the authoritative _direction.
///
/// ── BUG FIX: Patrol area shifts after retrieve ────────────────────────────────
///   Handled inside SoldierController.SetPatrolling(true) — it now always
///   recalculates patrol bounds from the soldier's current position before
///   resuming, so the patrol range is always centred on where the soldier stands.
///
/// ── BUG FIX: Can't re-drag after retrieve ─────────────────────────────────────
///   Unity skips OnEndDrag on a disabled GameObject. AcceptSoldier calls
///   SetActive(false), so _isDragging and blocksRaycasts stay stuck.
///   Fix: reset both flags in OnSuccessfulDrop() (called before SetActive(false)).
///
/// ── Setup ──────────────────────────────────────────────────────────────────────
///   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
///   2. The root Canvas must have a GraphicRaycaster.
///   3. An EventSystem must exist in the scene.
///   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
///      Do NOT add any Layout Group — it overrides anchoredPosition every frame
///      and stops the soldier from moving (animations play, position is frozen).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ─── State ────────────────────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;

    private Canvas _rootCanvas;
    private Transform _homeParent;
    private Vector2 _homeAnchoredPosition;
    private bool _isDragging;

    // ─── Dragon Rider State ───────────────────────────────────────────────────

    // The seat this soldier is currently riding on (null if on the ground).
    private DragonRiderSeat _currentSeat;

    // The home parent/position recorded BEFORE mounting — used by DismountFromDragon
    // to send the soldier back to the patrol area, not to the seat.
    private Transform _mountHomeParent;
    private Vector2 _mountHomePos;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _controller = GetComponent<SoldierController>();
    }

    private void Start()
    {
        RecordHome();
    }

    // ─── Drag Handlers ────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        // ── If riding a dragon, release the seat before lifting off ──────────
        // Track whether we came from a mount so we can fix the snap-back home below.
        bool wasMounted = _currentSeat != null;
        if (wasMounted)
        {
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
        }

        // Re-find root canvas every drag — cached value breaks after retrieve
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

        // RecordHome() saves transform.parent as home. When dismounting, the
        // current parent is still the dragon seat — so home would wrongly become
        // the seat, and SnapBack() would re-attach the soldier to the dragon.
        // Fix: after RecordHome(), override with the original ground home that
        // was stored in _mountHomeParent at the time of mounting.
        RecordHome();
        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;   // consumed — prevent stale reuse
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // ── Re-parent to root canvas ──────────────────────────────────────────
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;
        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // NOTE: This may NOT fire if the drop target called SetActive(false) on us
        // inside OnDrop. OnSuccessfulDrop() also resets these flags for that case.
        _isDragging = false;
        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's own
        // CanvasGroup doesn't shadow the seat/zone underneath.

        // ── Raycast to find a DragonRiderSeat under the pointer ──────────────
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragonRiderSeat targetSeat = null;
        foreach (var r in results)
        {
            targetSeat = r.gameObject.GetComponentInParent<DragonRiderSeat>();
            if (targetSeat != null) break;
        }

        // Restore raycast blocking now that detection is done
        _canvasGroup.blocksRaycasts = true;

        if (targetSeat != null && !targetSeat.IsOccupied)
        {
            // ── Dropped on an empty dragon seat → mount ───────────────────────
            // Save ground home BEFORE mounting so DismountFromDragon can return here.
            _mountHomeParent = _homeParent;
            _mountHomePos = _homeAnchoredPosition;
            targetSeat.MountSoldier(this);
        }
        else if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
        {
            // ── Dropped on empty space → snap back ───────────────────────────
            SnapBack();
        }
    }

    // ─── Drop Outcomes ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the soldier to its home position and resumes patrol.
    /// Called automatically when the drag ends over empty space.
    /// </summary>
    public void SnapBack()
    {
        transform.SetParent(_homeParent, true);
        _rect.anchoredPosition = _homeAnchoredPosition;

        // RefreshFlip re-applies the correct localScale.x sign after SetParent,
        // which can corrupt localScale when parent Canvas Scalers differ.
        //_controller?.RefreshFlip();
        _controller?.SetPatrolling(true);

        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by the drop target (WizardBox) after accepting the soldier.
    ///
    /// Resets _isDragging and blocksRaycasts HERE (not just in OnEndDrag) because
    /// the drop target calls SetActive(false) immediately after, which prevents
    /// Unity from delivering OnEndDrag to the disabled GameObject.
    /// </summary>
    public void OnSuccessfulDrop()
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _controller?.SetPatrolling(false);
        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
    }

    /// <summary>
    /// Call this from WizardBox "Retrieve" instead of directly calling SetParent.
    ///
    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
    ///   spawnPosition — optional anchoredPosition override inside that parent
    ///
    /// What it does:
    ///   • Re-parents soldier to spawnParent
    ///   • Optionally moves to spawnPosition
    ///   • Y-rotation flip is unaffected by SetParent — no refresh needed
    ///   • Calls SetPatrolling(true) which recalculates patrol bounds from
    ///     the soldier's new position before resuming
    ///   • Resets drag state flags
    ///   • Records new home for next drag's snap-back
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

        // Record new home so the next drag's SnapBack comes back here.
        RecordHome();

        // SetPatrolling(true) internally recalculates patrol bounds from the
        // soldier's current anchoredPosition, fixing the "patrol area drifts"
        // bug after the soldier lands at a new position.
        //_controller?.RefreshFlip();
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ─── Dragon Mount / Dismount ─────────────────────────────────────────────────

    /// <summary>
    /// Called by DragonRiderSeat.MountSoldier.
    /// Reparents the soldier to the seat, positions it at seatOffset,
    /// stops patrol, and resets the visual flip so dragon's flip takes over.
    /// </summary>
    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
    {
        _currentSeat = seat;

        // ── CHANGE: single call replaces SetPatrolling(false) + ResetFlipForMount()
        //    and also switches to the Riding animation state automatically.
        _controller?.EnterRidingState();

        // Reparent to the seat (keeps world scale stable with worldPositionStays:false)
        transform.SetParent(seat.transform, false);
        _rect.anchoredPosition = seatOffset;

        // Record home as the seat position so re-drags snap back here if needed
        RecordHome();

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on dragon seat '{seat.name}'.");
    }

    /// <summary>
    /// Returns the soldier to the ground patrol area it came from.
    /// Call this from a "Retrieve" button or any game event that dismounts the rider.
    /// </summary>
    public void DismountFromDragon()
    {
        if (_currentSeat != null)
        {
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
        }

        if (_mountHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home recorded — snapping to current home.");
            SnapBack();
            return;
        }

        transform.SetParent(_mountHomeParent, false);
        _rect.anchoredPosition = _mountHomePos;

        // ── CHANGE: single call replaces RefreshFlip() + SetPatrolling(true)
        //    and also exits the Riding animation state, resuming Walk + rest cycle.
        _controller?.ExitRidingState();

        // Update home so the next drag snaps back to the patrol area
        RecordHome();

        // Clear mount home so we don't accidentally reuse a stale value
        _mountHomeParent = null;

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }


}