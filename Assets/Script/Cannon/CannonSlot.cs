using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CANNON PANEL — CannonSlot
///
/// Attach to each slot GameObject on the castle.
/// Receives cannons via drag-and-drop (IDropHandler).
///
/// Upgrade state is NOT stored here — it lives in CannonInventoryEntry.
/// Removing a cannon from the slot sends it back to the inventory
/// with all upgrade progress intact.
///
/// Hierarchy suggestion per slot:
///   CannonSlot (this script + Image for background)
///   ├── SpawnPoint        ← cannon prefab is parented here
///   ├── EmptySlotVisual   ← shown when nothing is placed
///   ├── SlotHighlight     ← shown while dragging over this slot
///   └── RemoveButton      ← shown while occupied; sends cannon back to inventory
/// </summary>
public class CannonSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Children")]
    [SerializeField] private RectTransform spawnPoint;
    [Tooltip("Shown when the slot is empty")]
    [SerializeField] private GameObject emptySlotVisual;
    [Tooltip("Glows while a cannon is being dragged over this slot")]
    [SerializeField] private Image slotHighlight;
    [Tooltip("Button that removes the cannon back to the inventory")]
    [SerializeField] private Button removeButton;

    // ─── Runtime State ────────────────────────────────────────────────────────

    private CannonInventoryEntry _entry;
    private CannonController _controller;

    // ─── Public Properties ────────────────────────────────────────────────────

    public bool IsOccupied => _entry != null;
    public int OccupiedId => _entry?.inventoryId ?? -1;
    public CannonInventoryEntry Entry => _entry;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (slotHighlight != null) slotHighlight.gameObject.SetActive(false);
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(false);
            removeButton.onClick.AddListener(OnRemoveClicked);
        }
        RefreshVisuals();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Place a cannon into this slot.
    /// Called by OnDrop (drag-and-drop) and can also be called directly.
    /// If the slot is already occupied the existing cannon is removed first.
    /// </summary>
    public void PlaceCannon(CannonInventoryEntry entry)
    {
        if (entry == null) return;
        if (IsOccupied) RemoveCannon();

        _entry = entry;
        _entry.isPlacedOnCastle = true;
        _entry.occupiedSlot = this;

        // Spawn the visual prefab
        if (entry.data.prefab != null)
        {
            Transform parent = spawnPoint != null ? spawnPoint : transform;
            GameObject go = Instantiate(entry.data.prefab, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                // Match the prefab's own RectTransform size
                RectTransform prefabRt = entry.data.prefab.GetComponent<RectTransform>();
                if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
            }
            _controller = go.GetComponent<CannonController>();
            _controller?.Setup(entry.data);
        }

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Placed '{entry.data.cannonName}' (id={entry.inventoryId})");
    }

    /// <summary>
    /// Remove the cannon from this slot and send it back to inventory.
    /// Upgrade progress is preserved inside the CannonInventoryEntry.
    /// </summary>
    public void RemoveCannon()
    {
        if (!IsOccupied) return;

        _entry.isPlacedOnCastle = false;
        _entry.occupiedSlot = null;

        if (_controller != null)
        {
            Destroy(_controller.gameObject);
            _controller = null;
        }

        Debug.Log($"[CannonSlot] Removed '{_entry.data.cannonName}' (id={_entry.inventoryId})");
        _entry = null;

        RefreshVisuals();

        // Refresh inventory cards so the returned cannon reappears
        CannonPanelManager.Instance?.OnSlotRemoved();
    }

    // ─── IDropHandler ─────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        // Only accept drops when the slot is free
        if (IsOccupied) return;

        CannonDragHandler drag = eventData.pointerDrag?.GetComponent<CannonDragHandler>();
        if (drag == null || drag.Entry == null) return;

        // Don't accept a cannon that is already placed on another slot
        if (drag.Entry.isPlacedOnCastle) return;

        PlaceCannon(drag.Entry);
    }

    // ─── IPointerEnterHandler / IPointerExitHandler ───────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight only when slot is free (acts as a drop target visual cue)
        if (!IsOccupied && slotHighlight != null)
            slotHighlight.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotHighlight != null)
            slotHighlight.gameObject.SetActive(false);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private void OnRemoveClicked() => RemoveCannon();

    private void RefreshVisuals()
    {
        bool occupied = IsOccupied;
        if (emptySlotVisual != null) emptySlotVisual.SetActive(!occupied);
        if (removeButton != null) removeButton.gameObject.SetActive(occupied);
        if (slotHighlight != null) slotHighlight.gameObject.SetActive(false);
    }
}