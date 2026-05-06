//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE - SoldierDragDrop
/////////
///////// ── BUG FIX: Can't re-drag after retrieve ──────────────────────────────────────
/////////   OLD: _rootCanvas cached once in Awake.
/////////        After retrieve re-parents the soldier to a different panel, the cached
/////////        canvas could be stale or the soldier could end up in a different canvas
/////////        subtree. Also, patrol was never resumed after a retrieve (only SnapBack
/////////        called SetPatrolling(true), but retrieve bypassed SnapBack).
/////////
/////////   FIX 1: Re-walk the canvas hierarchy at the START of every drag (OnBeginDrag)
/////////          so _rootCanvas is always fresh.
/////////   FIX 2: Add public Retrieve() — call this from whatever button/system moves
/////////          the soldier back to the spawn area. It re-parents, resets position,
/////////          resumes patrol, and refreshes the drag-origin so the next drag works.
/////////   FIX 3: Added null-check guard on _rootCanvas to log a clear error instead
/////////          of a silent NullReference.
/////////
///////// ── Setup ──────────────────────────────────────────────────────────────────────
/////////   1. Attach to SolderPrefab root (same GO as CanvasGroup, SoldierStats).
/////////   2. The parent Canvas must have a GraphicRaycaster.
/////////   3. An EventSystem must exist in the scene.
/////////   4. Call soldier.GetComponent<SoldierDragDrop>().Retrieve(spawnParent)
/////////      from your WizardBox / retrieve button instead of directly re-parenting.
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class SoldierDragDrop : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ─── State ────────────────────────────────────────────────────────────────

//////    private CanvasGroup _canvasGroup;
//////    private RectTransform _rect;
//////    private SoldierController _controller;

//////    private Canvas _rootCanvas;           // refreshed on every OnBeginDrag
//////    private Transform _homeParent;           // where the soldier lives at rest
//////    private Vector2 _homeAnchoredPosition; // local position at rest
//////    private bool _isDragging;

//////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////        _rect = GetComponent<RectTransform>();
//////        _controller = GetComponent<SoldierController>();
//////    }

//////    private void Start()
//////    {
//////        // Record home on first frame (soldier just spawned at correct position)
//////        RecordHome();
//////    }

//////    // ─── Drag Handlers ────────────────────────────────────────────────────────

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        if (_isDragging) return;

//////        // ── FIX 1: Re-find root canvas every drag start ────────────────────────
//////        // Caching in Awake breaks after retrieve re-parents the soldier.
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//////            _rootCanvas = _rootCanvas.transform.parent
//////                              ?.GetComponentInParent<Canvas>();

//////        if (_rootCanvas == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] No root Canvas found in parent hierarchy. " +
//////                           "Make sure the soldier is inside a Canvas.");
//////            return;
//////        }

//////        // Snapshot home so SnapBack works
//////        RecordHome();

//////        _isDragging = true;
//////        _controller?.SetPatrolling(false);

//////        // Move to root canvas so the soldier draws on top of everything
//////        transform.SetParent(_rootCanvas.transform, true);
//////        transform.SetAsLastSibling();

//////        // Disable raycasts so pointer hits the drop target beneath
//////        _canvasGroup.blocksRaycasts = false;
//////    }

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        if (_rootCanvas == null) return;
//////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//////    }

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        _isDragging = false;
//////        _canvasGroup.blocksRaycasts = true;

//////        // If nobody called OnSuccessfulDrop (drop target didn't accept it)
//////        // and we're still parented to the root canvas → snap back home
//////        if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
//////            SnapBack();
//////    }

//////    // ─── Drop Outcomes ────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Returns the soldier to its home position and resumes patrol.
//////    /// Called automatically when the drag ends over empty space.
//////    /// </summary>
//////    public void SnapBack()
//////    {
//////        transform.SetParent(_homeParent, true);
//////        _rect.anchoredPosition = _homeAnchoredPosition;

//////        _controller?.SetPatrolling(true);
//////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//////    }

//////    /// <summary>
//////    /// Called by the drop target (e.g. WizardBox) after accepting the soldier.
//////    /// Patrol stays paused — the drop target now owns this soldier.
//////    /// </summary>
//////    public void OnSuccessfulDrop()
//////    {
//////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//////        // Drop target calls transform.SetParent(itself) — we just stop patrol.
//////        _controller?.SetPatrolling(false);
//////    }

//////    /// <summary>
//////    /// ── FIX 2: Retrieve ────────────────────────────────────────────────────────
//////    /// Call this from your WizardBox "Retrieve" button / retrieve logic INSTEAD
//////    /// of directly calling transform.SetParent().
//////    ///
//////    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
//////    ///   spawnPosition — anchoredPosition in that parent (pass Vector2.zero to
//////    ///                   keep whatever position the soldier was retrieved to)
//////    ///
//////    /// What it does:
//////    ///   • Re-parents soldier to spawnParent
//////    ///   • Optionally moves to spawnPosition
//////    ///   • Resumes patrol                          ← was missing before
//////    ///   • Updates home so the NEXT drag snaps back here correctly  ← was missing
//////    ///   • Re-enables raycasts in case something left them off
//////    /// </summary>
//////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//////    {
//////        if (spawnParent == null)
//////        {
//////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//////            return;
//////        }

//////        // Re-parent
//////        transform.SetParent(spawnParent, true);

//////        // Reposition if requested
//////        if (spawnPosition.HasValue)
//////            _rect.anchoredPosition = spawnPosition.Value;

//////        // Always re-enable raycasts
//////        _canvasGroup.blocksRaycasts = true;
//////        _isDragging = false;

//////        // Record new home so the next drag snaps back here
//////        RecordHome();

//////        // Resume patrol
//////        _controller?.SetPatrolling(true);

//////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//////    }

//////    // ─── Helper ───────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Snapshots the current parent and anchoredPosition as "home".
//////    /// Called in Start (initial spawn) and in Retrieve (after moving back).
//////    /// </summary>
//////    private void RecordHome()
//////    {
//////        _homeParent = transform.parent;
//////        _homeAnchoredPosition = _rect.anchoredPosition;
//////    }
//////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - SoldierDragDrop
///////
/////// ── BUG FIX: Can't re-drag after retrieve ──────────────────────────────────────
///////   ROOT CAUSE (was missing from previous fix):
///////     Unity's EventSystem fires events in this order on a successful drop:
///////       1. IDropHandler.OnDrop  → WizardBox.AcceptSoldier() → SetActive(false)
///////       2. IEndDragHandler.OnEndDrag → skipped because the GO is now DISABLED
///////
///////     Because OnEndDrag never runs, two flags stay stuck forever:
///////       • _isDragging        = true  → every future OnBeginDrag returns immediately
///////       • blocksRaycasts     = false → soldier doesn't receive pointer events at all
///////
///////   FIX: Reset both flags inside OnSuccessfulDrop(), which is called *before*
///////        the drop target calls SetActive(false). This guarantees the soldier is
///////        always in a clean, draggable state after being retrieved.
///////
/////// ── Other fixes (carried over) ────────────────────────────────────────────────
///////   FIX 1: Re-walk canvas hierarchy on every OnBeginDrag (not cached in Awake).
///////   FIX 2: Public Retrieve() re-parents, resets position, resumes patrol, and
///////           refreshes home so the next drag snaps back correctly.
///////   FIX 3: Null-check guard on _rootCanvas logs a clear error.
///////
/////// ── Setup ──────────────────────────────────────────────────────────────────────
///////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
///////   2. The root Canvas must have a GraphicRaycaster.
///////   3. An EventSystem must exist in the scene.
///////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
///////      Do NOT put a HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup
///////      on the spawn panel — layout groups override anchoredPosition every frame
///////      and will prevent the soldier from moving.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ─── State ────────────────────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;

////    private Canvas _rootCanvas;            // refreshed on every OnBeginDrag
////    private Transform _homeParent;            // where the soldier lives at rest
////    private Vector2 _homeAnchoredPosition;  // local position at rest
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
////        // Record home on first frame (soldier just spawned at correct position)
////        RecordHome();
////    }

////    // ─── Drag Handlers ────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // Re-find root canvas every drag — caching in Awake breaks after retrieve
////        // re-parents the soldier to a different panel.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent
////                              ?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found in parent hierarchy. " +
////                           "Make sure the soldier is inside a Canvas.");
////            return;
////        }

////        // Snapshot home so SnapBack works
////        RecordHome();

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // Move to root canvas so the soldier draws on top of everything
////        transform.SetParent(_rootCanvas.transform, true);
////        transform.SetAsLastSibling();

////        // Disable raycasts so the pointer hits the drop target beneath
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
////        // inside OnDrop (Unity skips events on disabled GameObjects). That is why
////        // OnSuccessfulDrop() also resets these flags.
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;

////        // If nobody accepted the drop and we are still under the root canvas → snap back
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

////        _controller?.SetPatrolling(true);
////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by the drop target (e.g. WizardBox) after accepting the soldier.
////    ///
////    /// ── KEY FIX ───────────────────────────────────────────────────────────────
////    /// Resets _isDragging and blocksRaycasts HERE, not just in OnEndDrag.
////    ///
////    /// Why: AcceptSoldier() calls SetActive(false) immediately after this method
////    /// returns. Unity's EventSystem will not deliver OnEndDrag to a disabled
////    /// GameObject, so those flags would stay stuck and block every future drag.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        // ── THE FIX: reset drag state before the drop target disables us ───────
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;
////        // ────────────────────────────────────────────────────────────────────────

////        // Patrol stays paused — the drop target now owns this soldier.
////        _controller?.SetPatrolling(false);

////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////    }

////    /// <summary>
////    /// Call this from your WizardBox "Retrieve" button / retrieve logic INSTEAD
////    /// of directly calling transform.SetParent().
////    ///
////    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
////    ///   spawnPosition — anchoredPosition in that parent (pass Vector2.zero to
////    ///                   keep whatever position the soldier was retrieved to)
////    ///
////    /// What it does:
////    ///   • Re-parents soldier to spawnParent
////    ///   • Optionally moves to spawnPosition
////    ///   • Resumes patrol
////    ///   • Updates home so the NEXT drag snaps back here correctly
////    ///   • Re-enables raycasts in case something left them off
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

////        // Guarantee clean state regardless of how the soldier got here
////        _canvasGroup.blocksRaycasts = true;
////        _isDragging = false;

////        // Record new home so the next drag snaps back here
////        RecordHome();

////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Snapshots the current parent and anchoredPosition as "home".
////    /// Called in Start (initial spawn), OnBeginDrag (before each drag),
////    /// and Retrieve (after moving back).
////    /// </summary>
////    private void RecordHome()
////    {
////        _homeParent = transform.parent;
////        _homeAnchoredPosition = _rect.anchoredPosition;
////    }
////}


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

//        // Restore correct flip — SetParent(worldPositionStays:true) corrupts
//        // localScale.x when parent Canvas Scaler factors differ.
//        _controller?.RefreshFlip();

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

//        // Restore flip after re-parent (SetParent corrupts localScale.x).
//        _controller?.RefreshFlip();

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
//    ///   • Calls RefreshFlip() to fix localScale.x corruption from SetParent
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

//        // ── FIX: restore flip after re-parent ─────────────────────────────────
//        // SetParent(worldPositionStays:true) changes localScale when the old and
//        // new parents have different Canvas Scaler factors, corrupting the flip.
//        _controller?.RefreshFlip();

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

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - SoldierDragDrop
///////
/////// ── BUG FIX: Can't re-drag after retrieve ──────────────────────────────────────
///////   OLD: _rootCanvas cached once in Awake.
///////        After retrieve re-parents the soldier to a different panel, the cached
///////        canvas could be stale or the soldier could end up in a different canvas
///////        subtree. Also, patrol was never resumed after a retrieve (only SnapBack
///////        called SetPatrolling(true), but retrieve bypassed SnapBack).
///////
///////   FIX 1: Re-walk the canvas hierarchy at the START of every drag (OnBeginDrag)
///////          so _rootCanvas is always fresh.
///////   FIX 2: Add public Retrieve() — call this from whatever button/system moves
///////          the soldier back to the spawn area. It re-parents, resets position,
///////          resumes patrol, and refreshes the drag-origin so the next drag works.
///////   FIX 3: Added null-check guard on _rootCanvas to log a clear error instead
///////          of a silent NullReference.
///////
/////// ── Setup ──────────────────────────────────────────────────────────────────────
///////   1. Attach to SolderPrefab root (same GO as CanvasGroup, SoldierStats).
///////   2. The parent Canvas must have a GraphicRaycaster.
///////   3. An EventSystem must exist in the scene.
///////   4. Call soldier.GetComponent<SoldierDragDrop>().Retrieve(spawnParent)
///////      from your WizardBox / retrieve button instead of directly re-parenting.
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class SoldierDragDrop : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ─── State ────────────────────────────────────────────────────────────────

////    private CanvasGroup _canvasGroup;
////    private RectTransform _rect;
////    private SoldierController _controller;

////    private Canvas _rootCanvas;           // refreshed on every OnBeginDrag
////    private Transform _homeParent;           // where the soldier lives at rest
////    private Vector2 _homeAnchoredPosition; // local position at rest
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
////        // Record home on first frame (soldier just spawned at correct position)
////        RecordHome();
////    }

////    // ─── Drag Handlers ────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (_isDragging) return;

////        // ── FIX 1: Re-find root canvas every drag start ────────────────────────
////        // Caching in Awake breaks after retrieve re-parents the soldier.
////        _rootCanvas = GetComponentInParent<Canvas>();
////        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
////            _rootCanvas = _rootCanvas.transform.parent
////                              ?.GetComponentInParent<Canvas>();

////        if (_rootCanvas == null)
////        {
////            Debug.LogError("[SoldierDragDrop] No root Canvas found in parent hierarchy. " +
////                           "Make sure the soldier is inside a Canvas.");
////            return;
////        }

////        // Snapshot home so SnapBack works
////        RecordHome();

////        _isDragging = true;
////        _controller?.SetPatrolling(false);

////        // Move to root canvas so the soldier draws on top of everything
////        transform.SetParent(_rootCanvas.transform, true);
////        transform.SetAsLastSibling();

////        // Disable raycasts so pointer hits the drop target beneath
////        _canvasGroup.blocksRaycasts = false;
////    }

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;
////        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
////    }

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        _isDragging = false;
////        _canvasGroup.blocksRaycasts = true;

////        // If nobody called OnSuccessfulDrop (drop target didn't accept it)
////        // and we're still parented to the root canvas → snap back home
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

////        _controller?.SetPatrolling(true);
////        Debug.Log("[SoldierDragDrop] Snapped back to home.");
////    }

////    /// <summary>
////    /// Called by the drop target (e.g. WizardBox) after accepting the soldier.
////    /// Patrol stays paused — the drop target now owns this soldier.
////    /// </summary>
////    public void OnSuccessfulDrop()
////    {
////        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
////        // Drop target calls transform.SetParent(itself) — we just stop patrol.
////        _controller?.SetPatrolling(false);
////    }

////    /// <summary>
////    /// ── FIX 2: Retrieve ────────────────────────────────────────────────────────
////    /// Call this from your WizardBox "Retrieve" button / retrieve logic INSTEAD
////    /// of directly calling transform.SetParent().
////    ///
////    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
////    ///   spawnPosition — anchoredPosition in that parent (pass Vector2.zero to
////    ///                   keep whatever position the soldier was retrieved to)
////    ///
////    /// What it does:
////    ///   • Re-parents soldier to spawnParent
////    ///   • Optionally moves to spawnPosition
////    ///   • Resumes patrol                          ← was missing before
////    ///   • Updates home so the NEXT drag snaps back here correctly  ← was missing
////    ///   • Re-enables raycasts in case something left them off
////    /// </summary>
////    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
////    {
////        if (spawnParent == null)
////        {
////            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
////            return;
////        }

////        // Re-parent
////        transform.SetParent(spawnParent, true);

////        // Reposition if requested
////        if (spawnPosition.HasValue)
////            _rect.anchoredPosition = spawnPosition.Value;

////        // Always re-enable raycasts
////        _canvasGroup.blocksRaycasts = true;
////        _isDragging = false;

////        // Record new home so the next drag snaps back here
////        RecordHome();

////        // Resume patrol
////        _controller?.SetPatrolling(true);

////        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
////    }

////    // ─── Helper ───────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Snapshots the current parent and anchoredPosition as "home".
////    /// Called in Start (initial spawn) and in Retrieve (after moving back).
////    /// </summary>
////    private void RecordHome()
////    {
////        _homeParent = transform.parent;
////        _homeAnchoredPosition = _rect.anchoredPosition;
////    }
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - SoldierDragDrop
/////
///// ── BUG FIX: Can't re-drag after retrieve ──────────────────────────────────────
/////   ROOT CAUSE (was missing from previous fix):
/////     Unity's EventSystem fires events in this order on a successful drop:
/////       1. IDropHandler.OnDrop  → WizardBox.AcceptSoldier() → SetActive(false)
/////       2. IEndDragHandler.OnEndDrag → skipped because the GO is now DISABLED
/////
/////     Because OnEndDrag never runs, two flags stay stuck forever:
/////       • _isDragging        = true  → every future OnBeginDrag returns immediately
/////       • blocksRaycasts     = false → soldier doesn't receive pointer events at all
/////
/////   FIX: Reset both flags inside OnSuccessfulDrop(), which is called *before*
/////        the drop target calls SetActive(false). This guarantees the soldier is
/////        always in a clean, draggable state after being retrieved.
/////
///// ── Other fixes (carried over) ────────────────────────────────────────────────
/////   FIX 1: Re-walk canvas hierarchy on every OnBeginDrag (not cached in Awake).
/////   FIX 2: Public Retrieve() re-parents, resets position, resumes patrol, and
/////           refreshes home so the next drag snaps back correctly.
/////   FIX 3: Null-check guard on _rootCanvas logs a clear error.
/////
///// ── Setup ──────────────────────────────────────────────────────────────────────
/////   1. Attach to SoldierPrefab root (same GO as CanvasGroup, SoldierStats).
/////   2. The root Canvas must have a GraphicRaycaster.
/////   3. An EventSystem must exist in the scene.
/////   4. The SPAWN PANEL must be a plain RectTransform + Image (Raycast Target ON).
/////      Do NOT put a HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup
/////      on the spawn panel — layout groups override anchoredPosition every frame
/////      and will prevent the soldier from moving.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ─── State ────────────────────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;

//    private Canvas _rootCanvas;            // refreshed on every OnBeginDrag
//    private Transform _homeParent;            // where the soldier lives at rest
//    private Vector2 _homeAnchoredPosition;  // local position at rest
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
//        // Record home on first frame (soldier just spawned at correct position)
//        RecordHome();
//    }

//    // ─── Drag Handlers ────────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Re-find root canvas every drag — caching in Awake breaks after retrieve
//        // re-parents the soldier to a different panel.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent
//                              ?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found in parent hierarchy. " +
//                           "Make sure the soldier is inside a Canvas.");
//            return;
//        }

//        // Snapshot home so SnapBack works
//        RecordHome();

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Move to root canvas so the soldier draws on top of everything
//        transform.SetParent(_rootCanvas.transform, true);
//        transform.SetAsLastSibling();

//        // Disable raycasts so the pointer hits the drop target beneath
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
//        // inside OnDrop (Unity skips events on disabled GameObjects). That is why
//        // OnSuccessfulDrop() also resets these flags.
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;

//        // If nobody accepted the drop and we are still under the root canvas → snap back
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

//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by the drop target (e.g. WizardBox) after accepting the soldier.
//    ///
//    /// ── KEY FIX ───────────────────────────────────────────────────────────────
//    /// Resets _isDragging and blocksRaycasts HERE, not just in OnEndDrag.
//    ///
//    /// Why: AcceptSoldier() calls SetActive(false) immediately after this method
//    /// returns. Unity's EventSystem will not deliver OnEndDrag to a disabled
//    /// GameObject, so those flags would stay stuck and block every future drag.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        // ── THE FIX: reset drag state before the drop target disables us ───────
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        // ────────────────────────────────────────────────────────────────────────

//        // Patrol stays paused — the drop target now owns this soldier.
//        _controller?.SetPatrolling(false);

//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Call this from your WizardBox "Retrieve" button / retrieve logic INSTEAD
//    /// of directly calling transform.SetParent().
//    ///
//    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
//    ///   spawnPosition — anchoredPosition in that parent (pass Vector2.zero to
//    ///                   keep whatever position the soldier was retrieved to)
//    ///
//    /// What it does:
//    ///   • Re-parents soldier to spawnParent
//    ///   • Optionally moves to spawnPosition
//    ///   • Resumes patrol
//    ///   • Updates home so the NEXT drag snaps back here correctly
//    ///   • Re-enables raycasts in case something left them off
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

//        // Guarantee clean state regardless of how the soldier got here
//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        // Record new home so the next drag snaps back here
//        RecordHome();

//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ─── Helper ───────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Snapshots the current parent and anchoredPosition as "home".
//    /// Called in Start (initial spawn), OnBeginDrag (before each drag),
//    /// and Retrieve (after moving back).
//    /// </summary>
//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }
//}


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

        RecordHome();

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
        _canvasGroup.blocksRaycasts = true;

        if (_rootCanvas != null && transform.parent == _rootCanvas.transform)
            SnapBack();
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

        // SetPatrolling(true) recalculates patrol bounds from current position
        // before resuming, so the patrol range is centred on the soldier.
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
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }
}