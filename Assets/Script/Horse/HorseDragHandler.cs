////////using UnityEngine;
////////using UnityEngine.UI;
////////using UnityEngine.EventSystems;

/////////// <summary>
/////////// HorseDragHandler
///////////
/////////// Attach to every draggable horse icon in the HorseArea
/////////// (Horse1, Horse2, Horse3, Horse4).
///////////
/////////// ── Setup in Inspector ────────────────────────────────────────────────────
///////////   • Assign the matching HorseData ScriptableObject to "Horse Data".
///////////   • The GameObject must have an Image component (the horse icon sprite).
///////////
/////////// ── Drag behaviour ────────────────────────────────────────────────────────
///////////   1. OnBeginDrag  → creates a semi-transparent ghost that follows the finger/mouse.
///////////                     The original icon fades to 50 % alpha.
///////////   2. OnDrag       → moves the ghost.
///////////   3. OnEndDrag    → destroys the ghost; restores the icon.
///////////                     Unity's EventSystem calls OnDrop on the object under the
///////////                     pointer — HorseWalkZone and HorseSlot both implement it.
/////////// </summary>
////////[RequireComponent(typeof(Image))]
////////public class HorseDragHandler : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    [Header("Horse to drag")]
////////    [Tooltip("ScriptableObject for this horse icon")]
////////    public HorseData horseData;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private Image _image;
////////    private RectTransform _rectTransform;
////////    private CanvasGroup _canvasGroup;
////////    private Canvas _canvas;

////////    private GameObject _ghost;
////////    private RectTransform _ghostRect;

////////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        _image = GetComponent<Image>();
////////        _rectTransform = GetComponent<RectTransform>();
////////        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
////////        _canvas = GetComponentInParent<Canvas>();
////////    }

////////    // ── Drag handlers ─────────────────────────────────────────────────────────

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        if (horseData == null)
////////        {
////////            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
////////            return;
////////        }

////////        // Fade original so the player sees it "lifted"
////////        _canvasGroup.alpha = 0.45f;
////////        _canvasGroup.blocksRaycasts = false; // let raycasts pass through to drop targets

////////        // Build ghost ─────────────────────────────────────────────────────────
////////        _ghost = new GameObject("HorseDragGhost");
////////        _ghost.transform.SetParent(_canvas.transform, false);
////////        _ghost.transform.SetAsLastSibling(); // always on top

////////        Image ghostImg = _ghost.AddComponent<Image>();
////////        ghostImg.sprite = _image.sprite;
////////        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
////////        ghostImg.raycastTarget = false;    // ghost must NOT block raycasts

////////        _ghostRect = _ghost.GetComponent<RectTransform>();
////////        _ghostRect.sizeDelta = _rectTransform.rect.size;
////////        _ghostRect.localScale = Vector3.one;

////////        MoveGhostToPointer(eventData);
////////    }

////////    public void OnDrag(PointerEventData eventData)
////////    {
////////        if (_ghostRect == null) return;
////////        MoveGhostToPointer(eventData);
////////    }

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        // Restore original icon
////////        _canvasGroup.alpha = 1f;
////////        _canvasGroup.blocksRaycasts = true;

////////        // Destroy ghost
////////        if (_ghost != null)
////////        {
////////            Destroy(_ghost);
////////            _ghost = null;
////////        }
////////    }

////////    // ── Helpers ───────────────────────────────────────────────────────────────

////////    private void MoveGhostToPointer(PointerEventData eventData)
////////    {
////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            _canvas.GetComponent<RectTransform>(),
////////            eventData.position,
////////            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
////////            out Vector2 localPoint);

////////        _ghostRect.localPosition = localPoint;
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// HorseDragHandler
/////////
///////// Attach to every draggable horse (slot icons AND walk-zone horses).
/////////
///////// ── Two modes ────────────────────────────────────────────────────────────
/////////   destroyOnSuccessfulDrop = false  →  Slot horse icon.
/////////       The icon fades to 50 % during drag and is restored when released.
/////////       The icon itself never moves or disappears.
/////////
/////////   destroyOnSuccessfulDrop = true   →  Walk-zone horse.
/////////       Set automatically by HorseWalkZone when it spawns the horse.
/////////       On a successful drop (slot accepts it) the walk-zone horse
/////////       GameObject destroys itself so the zone becomes empty.
/////////
///////// ── Drop flow ────────────────────────────────────────────────────────────
/////////   IDropHandler on the target calls RegisterSuccessfulDrop() BEFORE
/////////   Unity fires OnEndDrag on this component.
///////// </summary>
//////[RequireComponent(typeof(Image))]
//////public class HorseDragHandler : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    [Header("Horse to drag")]
//////    [Tooltip("ScriptableObject for this horse")]
//////    public HorseData horseData;

//////    [Header("Behaviour")]
//////    [Tooltip("If TRUE this GameObject destroys itself when successfully dropped on a valid target.\n" +
//////             "Set automatically by HorseWalkZone for walk-zone horses.")]
//////    public bool destroyOnSuccessfulDrop = false;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private Image _image;
//////    private RectTransform _rectTransform;
//////    private CanvasGroup _canvasGroup;
//////    private Canvas _canvas;

//////    private GameObject _ghost;
//////    private RectTransform _ghostRect;
//////    private bool _droppedSuccessfully = false;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _image = GetComponent<Image>();
//////        _rectTransform = GetComponent<RectTransform>();
//////        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
//////        _canvas = GetComponentInParent<Canvas>();
//////    }

//////    // ── Drag handlers ─────────────────────────────────────────────────────────

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        if (horseData == null)
//////        {
//////            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
//////            return;
//////        }

//////        _droppedSuccessfully = false;

//////        // Fade the original so the player sees it "lifted"
//////        _canvasGroup.alpha = 0.45f;
//////        _canvasGroup.blocksRaycasts = false; // pass raycasts through to drop targets

//////        // Build ghost ─────────────────────────────────────────────────────────
//////        _ghost = new GameObject("HorseDragGhost");
//////        _ghost.transform.SetParent(_canvas.transform, false);
//////        _ghost.transform.SetAsLastSibling(); // always on top

//////        Image ghostImg = _ghost.AddComponent<Image>();
//////        ghostImg.sprite = _image.sprite;     // snapshot of the current frame
//////        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
//////        ghostImg.raycastTarget = false;      // ghost must NOT block raycasts

//////        _ghostRect = _ghost.GetComponent<RectTransform>();
//////        _ghostRect.sizeDelta = _rectTransform.rect.size;
//////        _ghostRect.localScale = Vector3.one;

//////        MoveGhostToPointer(eventData);
//////    }

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        if (_ghostRect == null) return;
//////        MoveGhostToPointer(eventData);
//////    }

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        // Always destroy the ghost
//////        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

//////        // Walk-zone horse that was accepted by a slot → remove it from the zone
//////        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
//////        {
//////            Destroy(gameObject);
//////            return;
//////        }

//////        // Slot icon or unaccepted drop → restore the original
//////        _canvasGroup.alpha = 1f;
//////        _canvasGroup.blocksRaycasts = true;
//////    }

//////    // ── Public API ────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Drop targets call this to signal that the drop was accepted.
//////    /// Must be called before OnEndDrag fires (Unity guarantees this order).
//////    /// </summary>
//////    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

//////    // ── Private helpers ───────────────────────────────────────────────────────

//////    private void MoveGhostToPointer(PointerEventData eventData)
//////    {
//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            _canvas.GetComponent<RectTransform>(),
//////            eventData.position,
//////            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
//////            out Vector2 localPoint);

//////        _ghostRect.localPosition = localPoint;
//////    }
//////}


////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// HorseDragHandler
///////
/////// Attach to the HorsePrefab.
///////
/////// ── Drag behaviour ───────────────────────────────────────────────────────
///////   OnBeginDrag  → lifts the actual RectTransform to the canvas root so it
///////                  renders above every other UI element.
///////   OnDrag       → moves the RectTransform to follow the pointer.
///////   OnEndDrag    → if the drop was accepted, destroys this GameObject
///////                  (slot becomes empty, walk zone spawns its own copy).
///////                  If the drop was rejected, snaps back to the original
///////                  parent and position.
///////
/////// ── Two roles ────────────────────────────────────────────────────────────
///////   destroyOnSuccessfulDrop = true  (set by HorseSlot.Equip at runtime)
///////       → This is a slot horse. Destroy self when accepted by a walk zone.
///////         Call onRemovedFromSlot so the slot knows it is now empty.
///////
///////   destroyOnSuccessfulDrop = true  (set by HorseWalkZone at runtime)
///////       + WalkZoneOwner component present
///////       → This is a walk-zone horse. Destroy self when accepted by a slot.
///////
/////// ── Note on HorseSlot wiring ─────────────────────────────────────────────
///////   In HorseSlot.Equip(), after setting drag.destroyOnSuccessfulDrop = true,
///////   also set:
///////       drag.onRemovedFromSlot = RefreshUI;          // shows emptyGroup
///////   This is the only change needed in HorseSlot.
/////// </summary>
////[RequireComponent(typeof(Image))]
////public class HorseDragHandler : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    [Header("Horse data")]
////    [Tooltip("ScriptableObject for this horse. Set at runtime by HorseSlot / HorseWalkZone.")]
////    public HorseData horseData;

////    [Header("Behaviour")]
////    [Tooltip("Destroy this GameObject when a valid drop target accepts it.")]
////    public bool destroyOnSuccessfulDrop = false;

////    /// <summary>
////    /// Optional callback invoked just before this GameObject is destroyed on a
////    /// successful drop. HorseSlot assigns this to its own RefreshUI method so
////    /// the empty-slot UI is shown immediately.
////    /// </summary>
////    public System.Action onRemovedFromSlot;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private Image _image;
////    private RectTransform _rectTransform;
////    private CanvasGroup _canvasGroup;
////    private Canvas _canvas;

////    private Transform _originalParent;
////    private int _originalSiblingIndex;
////    private Vector2 _originalAnchoredPosition;
////    private bool _droppedSuccessfully = false;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _image = GetComponent<Image>();
////        _rectTransform = GetComponent<RectTransform>();
////        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
////        _canvas = GetComponentInParent<Canvas>();
////    }

////    // ── Drag handlers ─────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (horseData == null)
////        {
////            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
////            return;
////        }

////        _droppedSuccessfully = false;

////        // Remember where we came from so we can snap back on a failed drop
////        _originalParent = _rectTransform.parent;
////        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
////        _originalAnchoredPosition = _rectTransform.anchoredPosition;

////        // Lift to canvas root — renders above every other UI panel
////        _rectTransform.SetParent(_canvas.transform, true);
////        _rectTransform.SetAsLastSibling();

////        // Must be false so raycasts reach the drop targets beneath this object
////        _canvasGroup.blocksRaycasts = false;

////        MoveToPointer(eventData);
////    }

////    public void OnDrag(PointerEventData eventData)
////    {
////        MoveToPointer(eventData);
////    }

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        _canvasGroup.blocksRaycasts = true;

////        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
////        {
////            // Notify the owning slot so its empty-group UI refreshes
////            onRemovedFromSlot?.Invoke();
////            Destroy(gameObject);
////            return;
////        }

////        // Failed drop — return the horse to its original position
////        _rectTransform.SetParent(_originalParent, true);
////        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
////        _rectTransform.anchoredPosition = _originalAnchoredPosition;
////    }

////    // ── Public API ────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Called by a drop target (HorseWalkZone or HorseSlot) to signal that the
////    /// drop was accepted. Must be called before Unity fires OnEndDrag.
////    /// </summary>
////    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

////    // ── Private helpers ───────────────────────────────────────────────────────

////    private void MoveToPointer(PointerEventData eventData)
////    {
////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _canvas.GetComponent<RectTransform>(),
////            eventData.position,
////            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
////            out Vector2 localPoint);

////        _rectTransform.localPosition = localPoint;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// HorseDragHandler
/////
///// Attach to the HorsePrefab.
/////
///// ── Drag behaviour ───────────────────────────────────────────────────────
/////   OnBeginDrag  → lifts the actual RectTransform to the canvas root.
/////                  Records where on the horse the player grabbed so the
/////                  horse follows the pointer WITHOUT snapping to its centre.
/////   OnDrag       → moves the RectTransform keeping the grab-point offset.
/////   OnEndDrag    → successful: destroys self (drop target spawns new instance).
/////                  Failed: snaps back to original parent and position.
/////
///// ── Fields set by HorseSlot.Equip at runtime ─────────────────────────────
/////   horseData               ScriptableObject for this horse
/////   destroyOnSuccessfulDrop true for slot horses and walk-zone horses
/////   ownerSlot               HorseSlot this horse lives in (null = walk zone)
/////   inventoryIndex          position in HorseArea._ownedHorses
/////   onRemovedFromSlot       callback → refreshes the source slot empty-group
///// </summary>
//[RequireComponent(typeof(Image))]
//public class HorseDragHandler : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    [Header("Horse data")]
//    public HorseData horseData;

//    [Header("Behaviour")]
//    public bool destroyOnSuccessfulDrop = false;

//    // Set at runtime by HorseSlot.Equip ──────────────────────────────────────
//    public HorseSlot ownerSlot;
//    public int inventoryIndex = -1;
//    public System.Action onRemovedFromSlot;

//    // Private state ───────────────────────────────────────────────────────────
//    private Image _image;
//    private RectTransform _rectTransform;
//    private CanvasGroup _canvasGroup;
//    private Canvas _canvas;

//    private Transform _originalParent;
//    private int _originalSiblingIndex;
//    private Vector2 _originalAnchoredPosition;
//    private Vector2 _dragOffset;
//    private bool _droppedSuccessfully = false;

//    // Lifecycle ───────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _image = GetComponent<Image>();
//        _rectTransform = GetComponent<RectTransform>();
//        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
//        _canvas = GetComponentInParent<Canvas>();
//    }

//    // Drag handlers ───────────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (horseData == null)
//        {
//            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
//            return;
//        }

//        _droppedSuccessfully = false;

//        // Remember origin for snap-back on failed drop
//        _originalParent = _rectTransform.parent;
//        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
//        _originalAnchoredPosition = _rectTransform.anchoredPosition;

//        // Lift to canvas root (worldPositionStays=true keeps horse visually in place)
//        _rectTransform.SetParent(_canvas.transform, true);
//        _rectTransform.SetAsLastSibling();

//        // Compute the canvas-space offset from horse centre to the grab point.
//        // This is added back in MoveToPointer each frame so the horse doesn't
//        // snap its centre to the pointer — it follows from exactly where grabbed.
//        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null : _canvas.worldCamera;
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(), eventData.position, uiCam,
//            out Vector2 pointerInCanvas);
//        _dragOffset = (Vector2)_rectTransform.localPosition - pointerInCanvas;

//        // Must be false so raycasts reach the drop targets beneath this object
//        _canvasGroup.blocksRaycasts = false;
//    }

//    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _canvasGroup.blocksRaycasts = true;

//        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
//        {
//            onRemovedFromSlot?.Invoke();   // refreshes source slot's empty-group
//            Destroy(gameObject);
//            return;
//        }

//        // Failed / cancelled drop — return to original position
//        _rectTransform.SetParent(_originalParent, true);
//        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
//        _rectTransform.anchoredPosition = _originalAnchoredPosition;
//    }

//    // Public API ──────────────────────────────────────────────────────────────

//    /// <summary>Called by a drop target BEFORE OnEndDrag to signal the drop was accepted.</summary>
//    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

//    // Private helpers ─────────────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null : _canvas.worldCamera;
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(), eventData.position, uiCam,
//            out Vector2 localPoint);
//        _rectTransform.localPosition = localPoint + _dragOffset;
//    }
//}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// HorseDragHandler
/////////
///////// Attach to every draggable horse icon in the HorseArea
///////// (Horse1, Horse2, Horse3, Horse4).
/////////
///////// ── Setup in Inspector ────────────────────────────────────────────────────
/////////   • Assign the matching HorseData ScriptableObject to "Horse Data".
/////////   • The GameObject must have an Image component (the horse icon sprite).
/////////
///////// ── Drag behaviour ────────────────────────────────────────────────────────
/////////   1. OnBeginDrag  → creates a semi-transparent ghost that follows the finger/mouse.
/////////                     The original icon fades to 50 % alpha.
/////////   2. OnDrag       → moves the ghost.
/////////   3. OnEndDrag    → destroys the ghost; restores the icon.
/////////                     Unity's EventSystem calls OnDrop on the object under the
/////////                     pointer — HorseWalkZone and HorseSlot both implement it.
///////// </summary>
//////[RequireComponent(typeof(Image))]
//////public class HorseDragHandler : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    [Header("Horse to drag")]
//////    [Tooltip("ScriptableObject for this horse icon")]
//////    public HorseData horseData;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private Image _image;
//////    private RectTransform _rectTransform;
//////    private CanvasGroup _canvasGroup;
//////    private Canvas _canvas;

//////    private GameObject _ghost;
//////    private RectTransform _ghostRect;

//////    // ── Lifecycle ─────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _image = GetComponent<Image>();
//////        _rectTransform = GetComponent<RectTransform>();
//////        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
//////        _canvas = GetComponentInParent<Canvas>();
//////    }

//////    // ── Drag handlers ─────────────────────────────────────────────────────────

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        if (horseData == null)
//////        {
//////            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
//////            return;
//////        }

//////        // Fade original so the player sees it "lifted"
//////        _canvasGroup.alpha = 0.45f;
//////        _canvasGroup.blocksRaycasts = false; // let raycasts pass through to drop targets

//////        // Build ghost ─────────────────────────────────────────────────────────
//////        _ghost = new GameObject("HorseDragGhost");
//////        _ghost.transform.SetParent(_canvas.transform, false);
//////        _ghost.transform.SetAsLastSibling(); // always on top

//////        Image ghostImg = _ghost.AddComponent<Image>();
//////        ghostImg.sprite = _image.sprite;
//////        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
//////        ghostImg.raycastTarget = false;    // ghost must NOT block raycasts

//////        _ghostRect = _ghost.GetComponent<RectTransform>();
//////        _ghostRect.sizeDelta = _rectTransform.rect.size;
//////        _ghostRect.localScale = Vector3.one;

//////        MoveGhostToPointer(eventData);
//////    }

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        if (_ghostRect == null) return;
//////        MoveGhostToPointer(eventData);
//////    }

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        // Restore original icon
//////        _canvasGroup.alpha = 1f;
//////        _canvasGroup.blocksRaycasts = true;

//////        // Destroy ghost
//////        if (_ghost != null)
//////        {
//////            Destroy(_ghost);
//////            _ghost = null;
//////        }
//////    }

//////    // ── Helpers ───────────────────────────────────────────────────────────────

//////    private void MoveGhostToPointer(PointerEventData eventData)
//////    {
//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            _canvas.GetComponent<RectTransform>(),
//////            eventData.position,
//////            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
//////            out Vector2 localPoint);

//////        _ghostRect.localPosition = localPoint;
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// HorseDragHandler
///////
/////// Attach to every draggable horse (slot icons AND walk-zone horses).
///////
/////// ── Two modes ────────────────────────────────────────────────────────────
///////   destroyOnSuccessfulDrop = false  →  Slot horse icon.
///////       The icon fades to 50 % during drag and is restored when released.
///////       The icon itself never moves or disappears.
///////
///////   destroyOnSuccessfulDrop = true   →  Walk-zone horse.
///////       Set automatically by HorseWalkZone when it spawns the horse.
///////       On a successful drop (slot accepts it) the walk-zone horse
///////       GameObject destroys itself so the zone becomes empty.
///////
/////// ── Drop flow ────────────────────────────────────────────────────────────
///////   IDropHandler on the target calls RegisterSuccessfulDrop() BEFORE
///////   Unity fires OnEndDrag on this component.
/////// </summary>
////[RequireComponent(typeof(Image))]
////public class HorseDragHandler : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    [Header("Horse to drag")]
////    [Tooltip("ScriptableObject for this horse")]
////    public HorseData horseData;

////    [Header("Behaviour")]
////    [Tooltip("If TRUE this GameObject destroys itself when successfully dropped on a valid target.\n" +
////             "Set automatically by HorseWalkZone for walk-zone horses.")]
////    public bool destroyOnSuccessfulDrop = false;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private Image _image;
////    private RectTransform _rectTransform;
////    private CanvasGroup _canvasGroup;
////    private Canvas _canvas;

////    private GameObject _ghost;
////    private RectTransform _ghostRect;
////    private bool _droppedSuccessfully = false;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _image = GetComponent<Image>();
////        _rectTransform = GetComponent<RectTransform>();
////        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
////        _canvas = GetComponentInParent<Canvas>();
////    }

////    // ── Drag handlers ─────────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        if (horseData == null)
////        {
////            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
////            return;
////        }

////        _droppedSuccessfully = false;

////        // Fade the original so the player sees it "lifted"
////        _canvasGroup.alpha = 0.45f;
////        _canvasGroup.blocksRaycasts = false; // pass raycasts through to drop targets

////        // Build ghost ─────────────────────────────────────────────────────────
////        _ghost = new GameObject("HorseDragGhost");
////        _ghost.transform.SetParent(_canvas.transform, false);
////        _ghost.transform.SetAsLastSibling(); // always on top

////        Image ghostImg = _ghost.AddComponent<Image>();
////        ghostImg.sprite = _image.sprite;     // snapshot of the current frame
////        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
////        ghostImg.raycastTarget = false;      // ghost must NOT block raycasts

////        _ghostRect = _ghost.GetComponent<RectTransform>();
////        _ghostRect.sizeDelta = _rectTransform.rect.size;
////        _ghostRect.localScale = Vector3.one;

////        MoveGhostToPointer(eventData);
////    }

////    public void OnDrag(PointerEventData eventData)
////    {
////        if (_ghostRect == null) return;
////        MoveGhostToPointer(eventData);
////    }

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        // Always destroy the ghost
////        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

////        // Walk-zone horse that was accepted by a slot → remove it from the zone
////        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
////        {
////            Destroy(gameObject);
////            return;
////        }

////        // Slot icon or unaccepted drop → restore the original
////        _canvasGroup.alpha = 1f;
////        _canvasGroup.blocksRaycasts = true;
////    }

////    // ── Public API ────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Drop targets call this to signal that the drop was accepted.
////    /// Must be called before OnEndDrag fires (Unity guarantees this order).
////    /// </summary>
////    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

////    // ── Private helpers ───────────────────────────────────────────────────────

////    private void MoveGhostToPointer(PointerEventData eventData)
////    {
////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _canvas.GetComponent<RectTransform>(),
////            eventData.position,
////            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
////            out Vector2 localPoint);

////        _ghostRect.localPosition = localPoint;
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// HorseDragHandler
/////
///// Attach to the HorsePrefab.
/////
///// ── Drag behaviour ───────────────────────────────────────────────────────
/////   OnBeginDrag  → lifts the actual RectTransform to the canvas root so it
/////                  renders above every other UI element.
/////   OnDrag       → moves the RectTransform to follow the pointer.
/////   OnEndDrag    → if the drop was accepted, destroys this GameObject
/////                  (slot becomes empty, walk zone spawns its own copy).
/////                  If the drop was rejected, snaps back to the original
/////                  parent and position.
/////
///// ── Two roles ────────────────────────────────────────────────────────────
/////   destroyOnSuccessfulDrop = true  (set by HorseSlot.Equip at runtime)
/////       → This is a slot horse. Destroy self when accepted by a walk zone.
/////         Call onRemovedFromSlot so the slot knows it is now empty.
/////
/////   destroyOnSuccessfulDrop = true  (set by HorseWalkZone at runtime)
/////       + WalkZoneOwner component present
/////       → This is a walk-zone horse. Destroy self when accepted by a slot.
/////
///// ── Note on HorseSlot wiring ─────────────────────────────────────────────
/////   In HorseSlot.Equip(), after setting drag.destroyOnSuccessfulDrop = true,
/////   also set:
/////       drag.onRemovedFromSlot = RefreshUI;          // shows emptyGroup
/////   This is the only change needed in HorseSlot.
///// </summary>
//[RequireComponent(typeof(Image))]
//public class HorseDragHandler : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    [Header("Horse data")]
//    [Tooltip("ScriptableObject for this horse. Set at runtime by HorseSlot / HorseWalkZone.")]
//    public HorseData horseData;

//    [Header("Behaviour")]
//    [Tooltip("Destroy this GameObject when a valid drop target accepts it.")]
//    public bool destroyOnSuccessfulDrop = false;

//    /// <summary>
//    /// Optional callback invoked just before this GameObject is destroyed on a
//    /// successful drop. HorseSlot assigns this to its own RefreshUI method so
//    /// the empty-slot UI is shown immediately.
//    /// </summary>
//    public System.Action onRemovedFromSlot;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private Image _image;
//    private RectTransform _rectTransform;
//    private CanvasGroup _canvasGroup;
//    private Canvas _canvas;

//    private Transform _originalParent;
//    private int _originalSiblingIndex;
//    private Vector2 _originalAnchoredPosition;
//    private bool _droppedSuccessfully = false;

//    // ── Lifecycle ─────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _image = GetComponent<Image>();
//        _rectTransform = GetComponent<RectTransform>();
//        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
//        _canvas = GetComponentInParent<Canvas>();
//    }

//    // ── Drag handlers ─────────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (horseData == null)
//        {
//            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
//            return;
//        }

//        _droppedSuccessfully = false;

//        // Remember where we came from so we can snap back on a failed drop
//        _originalParent = _rectTransform.parent;
//        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
//        _originalAnchoredPosition = _rectTransform.anchoredPosition;

//        // Lift to canvas root — renders above every other UI panel
//        _rectTransform.SetParent(_canvas.transform, true);
//        _rectTransform.SetAsLastSibling();

//        // Must be false so raycasts reach the drop targets beneath this object
//        _canvasGroup.blocksRaycasts = false;

//        MoveToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        MoveToPointer(eventData);
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _canvasGroup.blocksRaycasts = true;

//        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
//        {
//            // Notify the owning slot so its empty-group UI refreshes
//            onRemovedFromSlot?.Invoke();
//            Destroy(gameObject);
//            return;
//        }

//        // Failed drop — return the horse to its original position
//        _rectTransform.SetParent(_originalParent, true);
//        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
//        _rectTransform.anchoredPosition = _originalAnchoredPosition;
//    }

//    // ── Public API ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by a drop target (HorseWalkZone or HorseSlot) to signal that the
//    /// drop was accepted. Must be called before Unity fires OnEndDrag.
//    /// </summary>
//    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

//    // ── Private helpers ───────────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
//            out Vector2 localPoint);

//        _rectTransform.localPosition = localPoint;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// HorseDragHandler
///
/// Attach to the HorsePrefab.
///
/// ── Drag behaviour ───────────────────────────────────────────────────────
///   OnBeginDrag  → lifts the actual RectTransform to the canvas root.
///                  Records where on the horse the player grabbed so the
///                  horse follows the pointer WITHOUT snapping to its centre.
///   OnDrag       → moves the RectTransform keeping the grab-point offset.
///   OnEndDrag    → successful: destroys self (drop target spawns new instance).
///                  Failed: snaps back to original parent and position.
///
/// ── Fields set by HorseSlot.Equip at runtime ─────────────────────────────
///   horseData               ScriptableObject for this horse
///   destroyOnSuccessfulDrop true for slot horses and walk-zone horses
///   ownerSlot               HorseSlot this horse lives in (null = walk zone)
///   inventoryIndex          position in HorseArea._ownedHorses
///   onRemovedFromSlot       callback → refreshes the source slot empty-group
/// </summary>
[RequireComponent(typeof(Image))]
public class HorseDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Horse data")]
    public HorseData horseData;

    [Header("Behaviour")]
    public bool destroyOnSuccessfulDrop = false;

    // Set at runtime by HorseSlot.Equip ──────────────────────────────────────
    public HorseSlot ownerSlot;
    public int inventoryIndex = -1;
    public System.Action onRemovedFromSlot;

    // Private state ───────────────────────────────────────────────────────────
    private Image _image;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;

    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _originalAnchoredPosition;
    private Vector2 _dragOffset;
    private bool _droppedSuccessfully = false;

    // Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    // Drag handlers ───────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (horseData == null)
        {
            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
            return;
        }

        _droppedSuccessfully = false;

        // Remember origin for snap-back on failed drop
        _originalParent = _rectTransform.parent;
        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        // Lift to canvas root (worldPositionStays=true keeps horse visually in place)
        _rectTransform.SetParent(_canvas.transform, true);
        _rectTransform.SetAsLastSibling();

        // Compute the canvas-space offset from horse centre to the grab point.
        // This is added back in MoveToPointer each frame so the horse doesn't
        // snap its centre to the pointer — it follows from exactly where grabbed.
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), eventData.position, uiCam,
            out Vector2 pointerInCanvas);
        _dragOffset = (Vector2)_rectTransform.localPosition - pointerInCanvas;

        // Must be false so raycasts reach the drop targets beneath this object
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
        {
            onRemovedFromSlot?.Invoke();   // refreshes source slot's empty-group

            // ── Eject any mounted soldier BEFORE Destroy ──────────────────────
            // The soldier is a child of SoldierSeat on this horse. Destroy(gameObject)
            // would also destroy the soldier since it is parented here.
            // EjectRiderBeforeDestroy() reparents the soldier back to its original
            // home so it survives the horse being removed.
            var hc = GetComponent<HorseController>();
            if (hc != null)
                hc.EjectRiderBeforeDestroy();
            // ─────────────────────────────────────────────────────────────────

            Destroy(gameObject);
            return;
        }

        // Failed / cancelled drop — return to original position
        _rectTransform.SetParent(_originalParent, true);
        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
    }

    // Public API ──────────────────────────────────────────────────────────────

    /// <summary>Called by a drop target BEFORE OnEndDrag to signal the drop was accepted.</summary>
    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

    // Private helpers ─────────────────────────────────────────────────────────

    private void MoveToPointer(PointerEventData eventData)
    {
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), eventData.position, uiCam,
            out Vector2 localPoint);
        _rectTransform.localPosition = localPoint + _dragOffset;
    }
}