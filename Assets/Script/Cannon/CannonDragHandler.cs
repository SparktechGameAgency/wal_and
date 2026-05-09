using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CANNON PANEL — CannonDragHandler
///
/// Added at runtime to each inventory card by CannonCard.SetupInventoryCard().
/// Handles dragging a cannon out of the inventory panel and dropping it onto
/// a CannonSlot on the castle.
///
/// Behaviour:
///   BeginDrag  — creates a ghost Image that follows the pointer
///   Drag       — moves ghost to pointer position
///   EndDrag    — destroys ghost; if a CannonSlot's OnDrop fired, the
///                cannon is already placed. If it wasn't dropped on a valid
///                slot nothing happens (card stays in inventory list).
///
/// Requirements:
///   • The Canvas must have a GraphicRaycaster component.
///   • CannonSlot implements IDropHandler — Unity's EventSystem handles routing.
///   • Call Init(entry) after adding this component.
/// </summary>
public class CannonDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ─── Runtime State ────────────────────────────────────────────────────────

    private CannonInventoryEntry _entry;
    private Canvas _rootCanvas;
    private GameObject _ghost;
    private RectTransform _ghostRect;
    private CanvasGroup _cardCanvasGroup;

    [Tooltip("Size of the ghost image while dragging")]
    [SerializeField] private Vector2 ghostSize = new Vector2(80f, 80f);

    public CannonInventoryEntry Entry => _entry;

    // ─── Init (called by CannonCard) ──────────────────────────────────────────

    /// <summary>
    /// Must be called after the component is added.
    /// Finds the root canvas so the ghost renders on top of everything.
    /// </summary>
    public void Init(CannonInventoryEntry entry)
    {
        _entry = entry;

        // Walk up to the root canvas so the ghost is drawn above all panels
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null)
        {
            Canvas[] all = c.GetComponentsInParent<Canvas>();
            foreach (Canvas candidate in all)
                if (candidate.isRootCanvas) { c = candidate; break; }
        }
        _rootCanvas = c;
    }

    // ─── IBeginDragHandler ────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Don't allow dragging a cannon that's already on the castle
        if (_entry == null || _entry.isPlacedOnCastle) { eventData.pointerDrag = null; return; }
        if (_rootCanvas == null) return;

        // ── Ghost image ──────────────────────────────────────────────────────
        _ghost = new GameObject("CannonDragGhost");
        _ghost.transform.SetParent(_rootCanvas.transform, false);
        _ghost.transform.SetAsLastSibling();   // always on top

        Image ghostImg = _ghost.AddComponent<Image>();
        ghostImg.raycastTarget = false;         // must not block drops on CannonSlot

        // Prefer the dedicated preview sprite; fall back to first idle frame
        Sprite s = _entry.data.previewSprite;
        if (s == null && _entry.data.idleSprites != null && _entry.data.idleSprites.Length > 0)
            s = _entry.data.idleSprites[0];
        if (s != null) ghostImg.sprite = s;

        _ghostRect = _ghost.GetComponent<RectTransform>();
        _ghostRect.sizeDelta = ghostSize;
        _ghostRect.pivot = new Vector2(0.5f, 0.5f);
        MoveGhostToPointer(eventData);

        // ── Make the original card semi-transparent while dragging ───────────
        _cardCanvasGroup = GetComponent<CanvasGroup>();
        if (_cardCanvasGroup == null)
            _cardCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        _cardCanvasGroup.alpha = 0.45f;
        _cardCanvasGroup.blocksRaycasts = false;   // let drops pass through to CannonSlot
    }

    // ─── IDragHandler ─────────────────────────────────────────────────────────

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghost == null) return;
        MoveGhostToPointer(eventData);
    }

    // ─── IEndDragHandler ──────────────────────────────────────────────────────

    public void OnEndDrag(PointerEventData eventData)
    {
        // Destroy ghost
        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }

        // Restore card appearance
        if (_cardCanvasGroup != null)
        {
            _cardCanvasGroup.alpha = 1f;
            _cardCanvasGroup.blocksRaycasts = true;
        }

        // If the drop succeeded, CannonSlot.OnDrop() already called PlaceCannon().
        // If the drop failed (missed a slot) nothing further is needed —
        // the card just stays in the inventory list.
        if (_entry != null && _entry.isPlacedOnCastle)
        {
            // Cannon was successfully placed — refresh inventory panel
            CannonPanelManager.Instance?.OnCannonPlacedOnCastle(_entry);
        }
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private void MoveGhostToPointer(PointerEventData eventData)
    {
        if (_ghostRect == null || _rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
            out Vector2 localPos);

        _ghostRect.localPosition = localPos;
    }
}