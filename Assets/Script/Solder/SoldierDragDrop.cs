using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - SoldierDragDrop
///
/// ── BUG FIX: Can't re-drag after retrieve ──────────────────────────────────────
///   OLD: _rootCanvas cached once in Awake.
///        After retrieve re-parents the soldier to a different panel, the cached
///        canvas could be stale or the soldier could end up in a different canvas
///        subtree. Also, patrol was never resumed after a retrieve (only SnapBack
///        called SetPatrolling(true), but retrieve bypassed SnapBack).
///
///   FIX 1: Re-walk the canvas hierarchy at the START of every drag (OnBeginDrag)
///          so _rootCanvas is always fresh.
///   FIX 2: Add public Retrieve() — call this from whatever button/system moves
///          the soldier back to the spawn area. It re-parents, resets position,
///          resumes patrol, and refreshes the drag-origin so the next drag works.
///   FIX 3: Added null-check guard on _rootCanvas to log a clear error instead
///          of a silent NullReference.
///
/// ── Setup ──────────────────────────────────────────────────────────────────────
///   1. Attach to SolderPrefab root (same GO as CanvasGroup, SoldierStats).
///   2. The parent Canvas must have a GraphicRaycaster.
///   3. An EventSystem must exist in the scene.
///   4. Call soldier.GetComponent<SoldierDragDrop>().Retrieve(spawnParent)
///      from your WizardBox / retrieve button instead of directly re-parenting.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ─── State ────────────────────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;

    private Canvas _rootCanvas;           // refreshed on every OnBeginDrag
    private Transform _homeParent;           // where the soldier lives at rest
    private Vector2 _homeAnchoredPosition; // local position at rest
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
        // Record home on first frame (soldier just spawned at correct position)
        RecordHome();
    }

    // ─── Drag Handlers ────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        // ── FIX 1: Re-find root canvas every drag start ────────────────────────
        // Caching in Awake breaks after retrieve re-parents the soldier.
        _rootCanvas = GetComponentInParent<Canvas>();
        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.transform.parent
                              ?.GetComponentInParent<Canvas>();

        if (_rootCanvas == null)
        {
            Debug.LogError("[SoldierDragDrop] No root Canvas found in parent hierarchy. " +
                           "Make sure the soldier is inside a Canvas.");
            return;
        }

        // Snapshot home so SnapBack works
        RecordHome();

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // Move to root canvas so the soldier draws on top of everything
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // Disable raycasts so pointer hits the drop target beneath
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;
        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;

        // If nobody called OnSuccessfulDrop (drop target didn't accept it)
        // and we're still parented to the root canvas → snap back home
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

        _controller?.SetPatrolling(true);
        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by the drop target (e.g. WizardBox) after accepting the soldier.
    /// Patrol stays paused — the drop target now owns this soldier.
    /// </summary>
    public void OnSuccessfulDrop()
    {
        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
        // Drop target calls transform.SetParent(itself) — we just stop patrol.
        _controller?.SetPatrolling(false);
    }

    /// <summary>
    /// ── FIX 2: Retrieve ────────────────────────────────────────────────────────
    /// Call this from your WizardBox "Retrieve" button / retrieve logic INSTEAD
    /// of directly calling transform.SetParent().
    ///
    ///   spawnParent   — the Transform to parent the soldier under (spawn area)
    ///   spawnPosition — anchoredPosition in that parent (pass Vector2.zero to
    ///                   keep whatever position the soldier was retrieved to)
    ///
    /// What it does:
    ///   • Re-parents soldier to spawnParent
    ///   • Optionally moves to spawnPosition
    ///   • Resumes patrol                          ← was missing before
    ///   • Updates home so the NEXT drag snaps back here correctly  ← was missing
    ///   • Re-enables raycasts in case something left them off
    /// </summary>
    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
    {
        if (spawnParent == null)
        {
            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
            return;
        }

        // Re-parent
        transform.SetParent(spawnParent, true);

        // Reposition if requested
        if (spawnPosition.HasValue)
            _rect.anchoredPosition = spawnPosition.Value;

        // Always re-enable raycasts
        _canvasGroup.blocksRaycasts = true;
        _isDragging = false;

        // Record new home so the next drag snaps back here
        RecordHome();

        // Resume patrol
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshots the current parent and anchoredPosition as "home".
    /// Called in Start (initial spawn) and in Retrieve (after moving back).
    /// </summary>
    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }
}