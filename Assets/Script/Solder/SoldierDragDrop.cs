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

    // ─── Helper ───────────────────────────────────────────────────────────────

    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }
}