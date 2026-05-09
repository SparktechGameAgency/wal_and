using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonSlot
///
/// Attach to the CannonSlot GameObject on the Village panel.
/// Matches the hierarchy in the screenshot:
///
///   CannonSlot  (this script)
///   ├── Spawnpoint         — cannon prefab is parented here when equipped
///   ├── EmptySlotVisual    — shown when no cannon is equipped
///   │   └── Text (TMP)     — e.g. "+" or "Empty"
///   ├── SlotHighLight      — optional hover glow
///   ├── RemoveButton       — unequips the current cannon
///   └── AddButton          — opens the Cannon Panel
///       └── Text (TMP)
///
/// FLOW:
///   1. Player clicks AddButton  → CannonPanelManager.OpenPanel(this)
///   2. Player buys a cannon in the panel (added to inventory)
///   3. Player clicks Equip in the inventory tab
///      → CannonPanelManager calls slot.Equip(entry)
///   4. Player clicks RemoveButton → slot.Unequip()
/// </summary>
public class CannonSlot : MonoBehaviour
{
    // ── Inspector refs ─────────────────────────────────────────────────────────
    [Header("Children (match screenshot hierarchy)")]
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private GameObject emptySlotVisual;
    [SerializeField] private GameObject slotHighLight;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button addButton;

    // ── Runtime state ──────────────────────────────────────────────────────────
    private CannonInventoryEntry _entry;
    private CannonController _controller;

    public bool IsOccupied => _entry != null;
    public CannonInventoryEntry Entry => _entry;

    // ── Unity ──────────────────────────────────────────────────────────────────
    private void Awake()
    {
        addButton?.onClick.AddListener(OnAddClicked);
        removeButton?.onClick.AddListener(OnRemoveClicked);
        RefreshVisuals();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by CannonPanelManager when the player clicks Equip in the inventory tab.
    /// Spawns the cannon prefab at the Spawnpoint.
    /// </summary>
    public void Equip(CannonInventoryEntry entry)
    {
        if (entry == null) return;

        // If something is already here, unequip it first
        if (IsOccupied) Unequip();

        _entry = entry;
        _entry.isEquipped = true;
        _entry.equippedSlot = this;

        // Spawn visual prefab
        if (entry.data.prefab != null && spawnpoint != null)
        {
            GameObject go = Instantiate(entry.data.prefab, spawnpoint);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }

            _controller = go.GetComponent<CannonController>();
            _controller?.Setup(entry.data);
        }

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Equipped '{entry.data.cannonName}' (id={entry.inventoryId})");
    }

    /// <summary>
    /// Called by RemoveButton or by CannonPanelManager (Unequip button in inventory).
    /// Returns the cannon to the inventory — upgrade progress is fully preserved.
    /// </summary>
    public void Unequip()
    {
        if (!IsOccupied) return;

        _entry.isEquipped = false;
        _entry.equippedSlot = null;

        if (_controller != null)
        {
            Destroy(_controller.gameObject);
            _controller = null;
        }

        Debug.Log($"[CannonSlot] Unequipped '{_entry.data.cannonName}' (id={_entry.inventoryId})");
        _entry = null;

        RefreshVisuals();

        // Refresh the panel so this cannon reappears in the inventory list
        CannonPanelManager.Instance?.RefreshAfterUnequip();
    }

    // ── Button handlers ────────────────────────────────────────────────────────

    private void OnAddClicked()
    {
        // Open the cannon panel, telling it this slot is the target
        CannonPanelManager.Instance?.OpenPanel(this);
    }

    private void OnRemoveClicked() => Unequip();

    // ── Visuals ────────────────────────────────────────────────────────────────

    private void RefreshVisuals()
    {
        bool occupied = IsOccupied;

        if (emptySlotVisual != null) emptySlotVisual.SetActive(!occupied);
        if (slotHighLight != null) slotHighLight.SetActive(false);

        // AddButton always visible so player can swap cannon; RemoveButton only when occupied
        if (removeButton != null) removeButton.gameObject.SetActive(occupied);
    }
}