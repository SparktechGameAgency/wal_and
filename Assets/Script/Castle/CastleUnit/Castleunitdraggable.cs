////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// Attach to your Cannon draggable objects in the village / unit panel.
/////// The unit itself follows the cursor while dragging.
///////
///////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////   Invalid drop → unit snaps back to its original position.
///////
/////// When the cannon is already placed in a CastleUnitDropZone and the player
/////// drags it again:
///////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
///////                                                        linked expansion slot shown.
///////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
///////                                     new expansion slot hidden.
///////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
///////                                     old expansion slot hidden again.
///////
/////// ── Inspector ────────────────────────────────────────────────────────────
///////   unitType          → Cannon or Soldier
///////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
///////                       FALSE : centers the unit at its natural size
///////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class CastleUnitDraggable : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Shared drag state ─────────────────────────────────────────
////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────
////    [Header("Unit Identity")]
////    public CastleUnitType unitType;

////    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
////    public int variantId = 0;

////    [Header("Slot Behaviour")]
////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
////             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
////    public bool stretchToFillSlot = false;

////    [Tooltip("Pixel size of the unit while being dragged.")]
////    public Vector2 dragGhostSize = new Vector2(120f, 60f);

////    // ── Private ───────────────────────────────────────────────────
////    private CanvasGroup _canvasGroup;
////    private Canvas _rootCanvas;
////    private Transform _originalParent;
////    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
////    private Vector2 _originalAnchoredPos;
////    private Vector2 _originalSizeDelta;
////    private static bool _droppedSuccessfully;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();

////        // FIX — The cannon GameObject must be a raycast target so Unity's
////        // EventSystem can deliver IBeginDragHandler / IDragHandler / IEndDragHandler
////        // events to it directly.
////        //
////        // Root cause: the root Image on the cannon prefab often has
////        //   Source Image = None  →  it is invisible, so developers sometimes
////        //   accidentally uncheck "Raycast Target", or the field is left at its
////        //   default (true) but then Unity silently strips the hit-area because
////        //   the Image has no sprite and alpha = 0.  Either way the pointer event
////        //   falls through to CannonZone's background Image, which has no drag
////        //   handlers, so dragging a placed cannon does nothing.
////        //
////        // Solution: get (or add) an Image on this GameObject and force
////        //   raycastTarget = true.  If no Image exists we add an invisible one
////        //   that acts purely as an EventSystem hit-area.
////        Image img = GetComponent<Image>();
////        if (img == null)
////        {
////            img = gameObject.AddComponent<Image>();
////            img.color = new Color(0f, 0f, 0f, 0f);   // fully transparent — hit-area only
////        }
////        img.raycastTarget = true;
////    }

////    // ── Drag ──────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        CurrentlyDragging = this;
////        _droppedSuccessfully = false;

////        // Remember where to return on a failed drop
////        _originalParent = transform.parent;
////        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

////        RectTransform selfRt = GetComponent<RectTransform>();
////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

////        // If the cannon was sitting in a drop zone, free it now:
////        //   • hides the soldier
////        //   • sets HasUnit = false so the zone can accept a new cannon
////        //   • shows the linked expansion slot (if any) so it can be dropped on again
////        _originalZone?.DetachUnit();

////        // Lift to root canvas so the unit renders above all UI panels
////        _rootCanvas = FindRootCanvas();
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////        transform.SetAsLastSibling();

////        // Resize to dragGhostSize while dragging
////        if (selfRt != null)
////        {
////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
////            selfRt.pivot = new Vector2(0.5f, 0.5f);
////            selfRt.sizeDelta = dragGhostSize;
////        }

////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = false;
////            _canvasGroup.alpha = 0.85f;
////        }

////        MoveToPointer(eventData);
////    }

////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = true;
////            _canvasGroup.alpha = 1f;
////        }

////        if (!_droppedSuccessfully)
////        {
////            // Snap back to original parent (zone or panel slot)
////            transform.SetParent(_originalParent, worldPositionStays: false);

////            RectTransform rt = GetComponent<RectTransform>();
////            if (rt != null)
////            {
////                rt.anchoredPosition = _originalAnchoredPos;
////                rt.sizeDelta = _originalSizeDelta;
////            }

////            // Restore the original zone:
////            //   • sets HasUnit = true, shows soldier again
////            //   • hides the linked expansion slot again (cannon is back)
////            _originalZone?.ReattachUnit(this);
////        }

////        _originalZone = null;
////        _originalParent = null;
////        CurrentlyDragging = null;
////        _droppedSuccessfully = false;
////    }

////    // ── Called by CastleUnitDropZone on a successful drop ─────────
////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////    // ── Helpers ───────────────────────────────────────────────────

////    private void MoveToPointer(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _rootCanvas.GetComponent<RectTransform>(),
////            eventData.position,
////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////                ? null : _rootCanvas.worldCamera,
////            out Vector2 local);

////        RectTransform rt = GetComponent<RectTransform>();
////        if (rt != null) rt.anchoredPosition = local;
////    }

////    private Canvas FindRootCanvas()
////    {
////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////        return (all == null || all.Length == 0)
////            ? FindObjectOfType<Canvas>()
////            : all[all.Length - 1];
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Attach to your Cannon draggable objects in the village / unit panel.
///// The unit itself follows the cursor while dragging.
/////
/////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////   Invalid drop → unit snaps back to its original position.
/////
///// When the cannon is already placed in a CastleUnitDropZone and the player
///// drags it again:
/////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
/////                                                        linked expansion slot shown.
/////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
/////                                     new expansion slot hidden.
/////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
/////                                     old expansion slot hidden again.
/////
///// ── Inspector ────────────────────────────────────────────────────────────
/////   unitType          → Cannon or Soldier
/////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
/////                       FALSE : uses placedSize at middle-left anchor
/////   placedSize        → pixel size of this unit when placed in a slot
/////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class CastleUnitDraggable : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Shared drag state ─────────────────────────────────────────
//    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────
//    [Header("Unit Identity")]
//    public CastleUnitType unitType;

//    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
//    public int variantId = 0;

//    [Header("Slot Behaviour")]
//    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
//             "FALSE → unit is placed at middle-left anchor using placedSize.")]
//    public bool stretchToFillSlot = false;

//    [Tooltip("Size of this unit when placed in a slot. Used when stretchToFillSlot = FALSE.")]
//    public Vector2 placedSize = new Vector2(56f, 56f);

//    [Tooltip("Pixel size of the unit while being dragged.")]
//    public Vector2 dragGhostSize = new Vector2(64f, 64f);

//    // ── Private ───────────────────────────────────────────────────
//    private CanvasGroup _canvasGroup;
//    private Canvas _rootCanvas;
//    private Transform _originalParent;
//    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
//    private Vector2 _originalAnchoredPos;
//    private Vector2 _originalSizeDelta;
//    private static bool _droppedSuccessfully;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();

//        // FIX — The cannon GameObject must be a raycast target so Unity's
//        // EventSystem can deliver IBeginDragHandler / IDragHandler / IEndDragHandler
//        // events to it directly.
//        //
//        // Root cause: the root Image on the cannon prefab often has
//        //   Source Image = None  →  it is invisible, so developers sometimes
//        //   accidentally uncheck "Raycast Target", or the field is left at its
//        //   default (true) but then Unity silently strips the hit-area because
//        //   the Image has no sprite and alpha = 0.  Either way the pointer event
//        //   falls through to CannonZone's background Image, which has no drag
//        //   handlers, so dragging a placed cannon does nothing.
//        //
//        // Solution: get (or add) an Image on this GameObject and force
//        //   raycastTarget = true.  If no Image exists we add an invisible one
//        //   that acts purely as an EventSystem hit-area.
//        Image img = GetComponent<Image>();
//        if (img == null)
//        {
//            img = gameObject.AddComponent<Image>();
//            img.color = new Color(0f, 0f, 0f, 0f);   // fully transparent — hit-area only
//        }
//        img.raycastTarget = true;
//    }

//    // ── Drag ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        CurrentlyDragging = this;
//        _droppedSuccessfully = false;

//        // Remember where to return on a failed drop
//        _originalParent = transform.parent;
//        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

//        RectTransform selfRt = GetComponent<RectTransform>();
//        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//        // If the cannon was sitting in a drop zone, free it now:
//        //   • hides the soldier
//        //   • sets HasUnit = false so the zone can accept a new cannon
//        //   • shows the linked expansion slot (if any) so it can be dropped on again
//        _originalZone?.DetachUnit();

//        // Lift to root canvas so the unit renders above all UI panels
//        _rootCanvas = FindRootCanvas();
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        // Resize to dragGhostSize while dragging
//        if (selfRt != null)
//        {
//            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//            selfRt.pivot = new Vector2(0.5f, 0.5f);
//            selfRt.sizeDelta = dragGhostSize;
//        }

//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.alpha = 0.85f;
//        }

//        MoveToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.alpha = 1f;
//        }

//        if (!_droppedSuccessfully)
//        {
//            // Snap back to original parent (zone or panel slot)
//            transform.SetParent(_originalParent, worldPositionStays: false);

//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null)
//            {
//                rt.anchoredPosition = _originalAnchoredPos;
//                rt.sizeDelta = _originalSizeDelta;
//            }

//            // Restore the original zone:
//            //   • sets HasUnit = true, shows soldier again
//            //   • hides the linked expansion slot again (cannon is back)
//            _originalZone?.ReattachUnit(this);
//        }

//        _originalZone = null;
//        _originalParent = null;
//        CurrentlyDragging = null;
//        _droppedSuccessfully = false;
//    }

//    // ── Called by CastleUnitDropZone on a successful drop ─────────
//    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//    // ── Helpers ───────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _rootCanvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                ? null : _rootCanvas.worldCamera,
//            out Vector2 local);

//        RectTransform rt = GetComponent<RectTransform>();
//        if (rt != null) rt.anchoredPosition = local;
//    }

//    private Canvas FindRootCanvas()
//    {
//        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//        return (all == null || all.Length == 0)
//            ? Object.FindFirstObjectByType<Canvas>()   // fixed: was FindObjectOfType (obsolete)
//            : all[all.Length - 1];
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Attach to your Cannon draggable objects in the village / unit panel.
///// The unit itself follows the cursor while dragging.
/////
/////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////   Invalid drop → unit snaps back to its original position.
/////
///// When the cannon is already placed in a CastleUnitDropZone and the player
///// drags it again:
/////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
/////                                                        linked expansion slot shown.
/////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
/////                                     new expansion slot hidden.
/////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
/////                                     old expansion slot hidden again.
/////
///// ── Inspector ────────────────────────────────────────────────────────────
/////   unitType          → Cannon or Soldier
/////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
/////                       FALSE : centers the unit at its natural size
/////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class CastleUnitDraggable : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Shared drag state ─────────────────────────────────────────
//    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────
//    [Header("Unit Identity")]
//    public CastleUnitType unitType;

//    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
//    public int variantId = 0;

//    [Header("Slot Behaviour")]
//    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
//             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
//    public bool stretchToFillSlot = false;

//    [Tooltip("Pixel size of the unit while being dragged.")]
//    public Vector2 dragGhostSize = new Vector2(120f, 60f);

//    // ── Private ───────────────────────────────────────────────────
//    private CanvasGroup _canvasGroup;
//    private Canvas _rootCanvas;
//    private Transform _originalParent;
//    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
//    private Vector2 _originalAnchoredPos;
//    private Vector2 _originalSizeDelta;
//    private static bool _droppedSuccessfully;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();

//        // FIX — The cannon GameObject must be a raycast target so Unity's
//        // EventSystem can deliver IBeginDragHandler / IDragHandler / IEndDragHandler
//        // events to it directly.
//        //
//        // Root cause: the root Image on the cannon prefab often has
//        //   Source Image = None  →  it is invisible, so developers sometimes
//        //   accidentally uncheck "Raycast Target", or the field is left at its
//        //   default (true) but then Unity silently strips the hit-area because
//        //   the Image has no sprite and alpha = 0.  Either way the pointer event
//        //   falls through to CannonZone's background Image, which has no drag
//        //   handlers, so dragging a placed cannon does nothing.
//        //
//        // Solution: get (or add) an Image on this GameObject and force
//        //   raycastTarget = true.  If no Image exists we add an invisible one
//        //   that acts purely as an EventSystem hit-area.
//        Image img = GetComponent<Image>();
//        if (img == null)
//        {
//            img = gameObject.AddComponent<Image>();
//            img.color = new Color(0f, 0f, 0f, 0f);   // fully transparent — hit-area only
//        }
//        img.raycastTarget = true;
//    }

//    // ── Drag ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        CurrentlyDragging = this;
//        _droppedSuccessfully = false;

//        // Remember where to return on a failed drop
//        _originalParent = transform.parent;
//        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

//        RectTransform selfRt = GetComponent<RectTransform>();
//        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//        // If the cannon was sitting in a drop zone, free it now:
//        //   • hides the soldier
//        //   • sets HasUnit = false so the zone can accept a new cannon
//        //   • shows the linked expansion slot (if any) so it can be dropped on again
//        _originalZone?.DetachUnit();

//        // Lift to root canvas so the unit renders above all UI panels
//        _rootCanvas = FindRootCanvas();
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        // Resize to dragGhostSize while dragging
//        if (selfRt != null)
//        {
//            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//            selfRt.pivot = new Vector2(0.5f, 0.5f);
//            selfRt.sizeDelta = dragGhostSize;
//        }

//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.alpha = 0.85f;
//        }

//        MoveToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.alpha = 1f;
//        }

//        if (!_droppedSuccessfully)
//        {
//            // Snap back to original parent (zone or panel slot)
//            transform.SetParent(_originalParent, worldPositionStays: false);

//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null)
//            {
//                rt.anchoredPosition = _originalAnchoredPos;
//                rt.sizeDelta = _originalSizeDelta;
//            }

//            // Restore the original zone:
//            //   • sets HasUnit = true, shows soldier again
//            //   • hides the linked expansion slot again (cannon is back)
//            _originalZone?.ReattachUnit(this);
//        }

//        _originalZone = null;
//        _originalParent = null;
//        CurrentlyDragging = null;
//        _droppedSuccessfully = false;
//    }

//    // ── Called by CastleUnitDropZone on a successful drop ─────────
//    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//    // ── Helpers ───────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _rootCanvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                ? null : _rootCanvas.worldCamera,
//            out Vector2 local);

//        RectTransform rt = GetComponent<RectTransform>();
//        if (rt != null) rt.anchoredPosition = local;
//    }

//    private Canvas FindRootCanvas()
//    {
//        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//        return (all == null || all.Length == 0)
//            ? FindObjectOfType<Canvas>()
//            : all[all.Length - 1];
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to your Cannon draggable objects in the village / unit panel.
/// The unit itself follows the cursor while dragging.
///
///   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///   Invalid drop → unit snaps back to its original position.
///
/// When the cannon is already placed in a CastleUnitDropZone and the player
/// drags it again:
///   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
///                                                        linked expansion slot shown.
///   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
///                                     new expansion slot hidden.
///   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
///                                     old expansion slot hidden again.
///
/// ── Inspector ────────────────────────────────────────────────────────────
///   unitType          → Cannon or Soldier
///   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
///                       FALSE : uses placedSize at middle-left anchor
///   placedSize        → pixel size of this unit when placed in a slot
///   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CastleUnitDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Shared drag state ─────────────────────────────────────────
    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Unit Identity")]
    public CastleUnitType unitType;

    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
    public int variantId = 0;

    [Header("Slot Behaviour")]
    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
             "FALSE → unit is placed at middle-left anchor using placedSize.")]
    public bool stretchToFillSlot = false;

    [Tooltip("Size of this unit when placed in a slot. Used when stretchToFillSlot = FALSE.")]
    public Vector2 placedSize = new Vector2(56f, 56f);

    [Tooltip("Set by GridCell.SetUnitSlotVillageMode().\n" +
             "True  = Village panel → dragging enabled.\n" +
             "False = Castle panel  → dragging disabled (CannonZone Button still works).")]
    public bool dragEnabled = true;

    [Tooltip("Pixel size of the unit while being dragged.")]
    public Vector2 dragGhostSize = new Vector2(64f, 64f);

    // ── Private ───────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _originalParent;
    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
    private Vector2 _originalAnchoredPos;
    private Vector2 _originalSizeDelta;
    private static bool _droppedSuccessfully;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // FIX — The cannon GameObject must be a raycast target so Unity's
        // EventSystem can deliver IBeginDragHandler / IDragHandler / IEndDragHandler
        // events to it directly.
        //
        // Root cause: the root Image on the cannon prefab often has
        //   Source Image = None  →  it is invisible, so developers sometimes
        //   accidentally uncheck "Raycast Target", or the field is left at its
        //   default (true) but then Unity silently strips the hit-area because
        //   the Image has no sprite and alpha = 0.  Either way the pointer event
        //   falls through to CannonZone's background Image, which has no drag
        //   handlers, so dragging a placed cannon does nothing.
        //
        // Solution: get (or add) an Image on this GameObject and force
        //   raycastTarget = true.  If no Image exists we add an invisible one
        //   that acts purely as an EventSystem hit-area.
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);   // fully transparent — hit-area only
        }
        img.raycastTarget = true;
    }

    // ── Drag ──────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Drag is disabled in Castle panel mode; only Button clicks are allowed there.
        if (!dragEnabled) return;

        CurrentlyDragging = this;
        _droppedSuccessfully = false;

        // Remember where to return on a failed drop
        _originalParent = transform.parent;
        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

        RectTransform selfRt = GetComponent<RectTransform>();
        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

        // If the cannon was sitting in a drop zone, free it now:
        //   • hides the soldier
        //   • sets HasUnit = false so the zone can accept a new cannon
        //   • shows the linked expansion slot (if any) so it can be dropped on again
        _originalZone?.DetachUnit();

        // Lift to root canvas so the unit renders above all UI panels
        _rootCanvas = FindRootCanvas();
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        // Resize to dragGhostSize while dragging
        if (selfRt != null)
        {
            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
            selfRt.pivot = new Vector2(0.5f, 0.5f);
            selfRt.sizeDelta = dragGhostSize;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;
        }

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }

        if (!_droppedSuccessfully)
        {
            // Snap back to original parent (zone or panel slot)
            transform.SetParent(_originalParent, worldPositionStays: false);

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = _originalAnchoredPos;
                rt.sizeDelta = _originalSizeDelta;
            }

            // Restore the original zone:
            //   • sets HasUnit = true, shows soldier again
            //   • hides the linked expansion slot again (cannon is back)
            _originalZone?.ReattachUnit(this);
        }

        _originalZone = null;
        _originalParent = null;
        CurrentlyDragging = null;
        _droppedSuccessfully = false;
    }

    // ── Called by CastleUnitDropZone on a successful drop ─────────
    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

    // ── Helpers ───────────────────────────────────────────────────

    private void MoveToPointer(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _rootCanvas.worldCamera,
            out Vector2 local);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = local;
    }

    private Canvas FindRootCanvas()
    {
        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
        return (all == null || all.Length == 0)
            ? Object.FindFirstObjectByType<Canvas>()   // fixed: was FindObjectOfType (obsolete)
            : all[all.Length - 1];
    }
}