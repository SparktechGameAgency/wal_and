//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// HorseDragHandler
/////
///// Attach to every draggable horse icon in the HorseArea
///// (Horse1, Horse2, Horse3, Horse4).
/////
///// ── Setup in Inspector ────────────────────────────────────────────────────
/////   • Assign the matching HorseData ScriptableObject to "Horse Data".
/////   • The GameObject must have an Image component (the horse icon sprite).
/////
///// ── Drag behaviour ────────────────────────────────────────────────────────
/////   1. OnBeginDrag  → creates a semi-transparent ghost that follows the finger/mouse.
/////                     The original icon fades to 50 % alpha.
/////   2. OnDrag       → moves the ghost.
/////   3. OnEndDrag    → destroys the ghost; restores the icon.
/////                     Unity's EventSystem calls OnDrop on the object under the
/////                     pointer — HorseWalkZone and HorseSlot both implement it.
///// </summary>
//[RequireComponent(typeof(Image))]
//public class HorseDragHandler : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    [Header("Horse to drag")]
//    [Tooltip("ScriptableObject for this horse icon")]
//    public HorseData horseData;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private Image _image;
//    private RectTransform _rectTransform;
//    private CanvasGroup _canvasGroup;
//    private Canvas _canvas;

//    private GameObject _ghost;
//    private RectTransform _ghostRect;

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

//        // Fade original so the player sees it "lifted"
//        _canvasGroup.alpha = 0.45f;
//        _canvasGroup.blocksRaycasts = false; // let raycasts pass through to drop targets

//        // Build ghost ─────────────────────────────────────────────────────────
//        _ghost = new GameObject("HorseDragGhost");
//        _ghost.transform.SetParent(_canvas.transform, false);
//        _ghost.transform.SetAsLastSibling(); // always on top

//        Image ghostImg = _ghost.AddComponent<Image>();
//        ghostImg.sprite = _image.sprite;
//        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
//        ghostImg.raycastTarget = false;    // ghost must NOT block raycasts

//        _ghostRect = _ghost.GetComponent<RectTransform>();
//        _ghostRect.sizeDelta = _rectTransform.rect.size;
//        _ghostRect.localScale = Vector3.one;

//        MoveGhostToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_ghostRect == null) return;
//        MoveGhostToPointer(eventData);
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // Restore original icon
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;

//        // Destroy ghost
//        if (_ghost != null)
//        {
//            Destroy(_ghost);
//            _ghost = null;
//        }
//    }

//    // ── Helpers ───────────────────────────────────────────────────────────────

//    private void MoveGhostToPointer(PointerEventData eventData)
//    {
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
//            out Vector2 localPoint);

//        _ghostRect.localPosition = localPoint;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// HorseDragHandler
///
/// Attach to every draggable horse (slot icons AND walk-zone horses).
///
/// ── Two modes ────────────────────────────────────────────────────────────
///   destroyOnSuccessfulDrop = false  →  Slot horse icon.
///       The icon fades to 50 % during drag and is restored when released.
///       The icon itself never moves or disappears.
///
///   destroyOnSuccessfulDrop = true   →  Walk-zone horse.
///       Set automatically by HorseWalkZone when it spawns the horse.
///       On a successful drop (slot accepts it) the walk-zone horse
///       GameObject destroys itself so the zone becomes empty.
///
/// ── Drop flow ────────────────────────────────────────────────────────────
///   IDropHandler on the target calls RegisterSuccessfulDrop() BEFORE
///   Unity fires OnEndDrag on this component.
/// </summary>
[RequireComponent(typeof(Image))]
public class HorseDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Horse to drag")]
    [Tooltip("ScriptableObject for this horse")]
    public HorseData horseData;

    [Header("Behaviour")]
    [Tooltip("If TRUE this GameObject destroys itself when successfully dropped on a valid target.\n" +
             "Set automatically by HorseWalkZone for walk-zone horses.")]
    public bool destroyOnSuccessfulDrop = false;

    // ── Private state ─────────────────────────────────────────────────────────

    private Image _image;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;

    private GameObject _ghost;
    private RectTransform _ghostRect;
    private bool _droppedSuccessfully = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    // ── Drag handlers ─────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (horseData == null)
        {
            Debug.LogWarning($"[HorseDragHandler] '{name}' has no HorseData assigned!");
            return;
        }

        _droppedSuccessfully = false;

        // Fade the original so the player sees it "lifted"
        _canvasGroup.alpha = 0.45f;
        _canvasGroup.blocksRaycasts = false; // pass raycasts through to drop targets

        // Build ghost ─────────────────────────────────────────────────────────
        _ghost = new GameObject("HorseDragGhost");
        _ghost.transform.SetParent(_canvas.transform, false);
        _ghost.transform.SetAsLastSibling(); // always on top

        Image ghostImg = _ghost.AddComponent<Image>();
        ghostImg.sprite = _image.sprite;     // snapshot of the current frame
        ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
        ghostImg.raycastTarget = false;      // ghost must NOT block raycasts

        _ghostRect = _ghost.GetComponent<RectTransform>();
        _ghostRect.sizeDelta = _rectTransform.rect.size;
        _ghostRect.localScale = Vector3.one;

        MoveGhostToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRect == null) return;
        MoveGhostToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Always destroy the ghost
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

        // Walk-zone horse that was accepted by a slot → remove it from the zone
        if (_droppedSuccessfully && destroyOnSuccessfulDrop)
        {
            Destroy(gameObject);
            return;
        }

        // Slot icon or unaccepted drop → restore the original
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Drop targets call this to signal that the drop was accepted.
    /// Must be called before OnEndDrag fires (Unity guarantees this order).
    /// </summary>
    public void RegisterSuccessfulDrop() => _droppedSuccessfully = true;

    // ── Private helpers ───────────────────────────────────────────────────────

    private void MoveGhostToPointer(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            eventData.position,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPoint);

        _ghostRect.localPosition = localPoint;
    }
}