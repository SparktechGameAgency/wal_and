using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - SoldierDragDrop
/// Attach this to the SolderPrefab root.
/// Lets the player drag the soldier and drop it onto the WizardBox.
///
/// Setup:
///   1. Add this script to SolderPrefab (same GameObject as Image, Animator, SoldierStats).
///   2. Make sure a CanvasGroup component is also on SolderPrefab
///      (this script adds one automatically if missing).
///   3. The parent Canvas must have a GraphicRaycaster component.
///   4. An EventSystem must exist in the scene (Unity adds one automatically
///      when you create a UI element).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ─── Private State ────────────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private Transform _originalParent;
    private Vector3 _originalPosition;
    private Canvas _rootCanvas;

    private SoldierController _controller;   // to pause patrol while dragging

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _controller = GetComponent<SoldierController>();

        // Walk up until we find the root Canvas (needed for screen→canvas coords)
        _rootCanvas = GetComponentInParent<Canvas>();
        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
    }

    // ─── Drag Handlers ────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Remember where to snap back if the drop is invalid
        _originalParent = transform.parent;
        _originalPosition = transform.localPosition;

        // Pause patrol so the soldier stops walking while held
        _controller?.SetPatrolling(false);

        // Reparent to the root canvas so the soldier renders on top of everything
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // Disable raycasts on self so the pointer can hit the WizardBox beneath
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the soldier with the pointer
        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Re-enable raycasts so the soldier is interactive again
        _canvasGroup.blocksRaycasts = true;

        // If WizardBox.OnDrop() already handled the drop it will have
        // reparented this object — only snap back if we are still orphaned
        // (i.e. the drop landed on empty space).
        if (transform.parent == _rootCanvas.transform)
            SnapBack();
    }

    // ─── Snap Back ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the soldier to its original position in the spawn area
    /// and resumes patrol.
    /// </summary>
    public void SnapBack()
    {
        transform.SetParent(_originalParent, true);
        transform.localPosition = _originalPosition;
        _controller?.SetPatrolling(true);

        Debug.Log("[SoldierDragDrop] Dropped on empty space — snapping back.");
    }

    /// <summary>Called by WizardBox after a successful drop.</summary>
    public void OnSuccessfulDrop()
    {
        // Patrol is already paused from OnBeginDrag.
        // WizardBox takes ownership of this GameObject from here.
        Debug.Log("[SoldierDragDrop] Soldier accepted by WizardBox.");
    }
}