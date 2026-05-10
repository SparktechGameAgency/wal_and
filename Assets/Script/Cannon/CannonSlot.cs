//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonSlot
/////
///// Attach to the CannonSlot GameObject on the Village panel.
///// Matches the hierarchy in the screenshot:
/////
/////   CannonSlot  (this script)
/////   ├── Spawnpoint         — cannon prefab is parented here when equipped
/////   ├── EmptySlotVisual    — shown when no cannon is equipped
/////   │   └── Text (TMP)     — e.g. "+" or "Empty"
/////   ├── SlotHighLight      — optional hover glow
/////   ├── RemoveButton       — unequips the current cannon
/////   └── AddButton          — opens the Cannon Panel
/////       └── Text (TMP)
/////
///// FLOW:
/////   1. Player clicks AddButton  → CannonPanelManager.OpenPanel(this)
/////   2. Player buys a cannon in the panel (added to inventory)
/////   3. Player clicks Equip in the inventory tab
/////      → CannonPanelManager calls slot.Equip(entry)
/////   4. Player clicks RemoveButton → slot.Unequip()
///// </summary>
//public class CannonSlot : MonoBehaviour
//{
//    // ── Inspector refs ─────────────────────────────────────────────────────────
//    [Header("Children (match screenshot hierarchy)")]
//    [SerializeField] private Transform spawnpoint;
//    [SerializeField] private GameObject emptySlotVisual;
//    [SerializeField] private GameObject slotHighLight;
//    [SerializeField] private Button removeButton;
//    [SerializeField] private Button addButton;

//    // ── Runtime state ──────────────────────────────────────────────────────────
//    private CannonInventoryEntry _entry;
//    private CannonController _controller;

//    public bool IsOccupied => _entry != null;
//    public CannonInventoryEntry Entry => _entry;

//    // ── Unity ──────────────────────────────────────────────────────────────────
//    private void Awake()
//    {
//        addButton?.onClick.AddListener(OnAddClicked);
//        removeButton?.onClick.AddListener(OnRemoveClicked);
//        RefreshVisuals();
//    }

//    // ── Public API ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by CannonPanelManager when the player clicks Equip in the inventory tab.
//    /// Spawns the cannon prefab at the Spawnpoint.
//    /// </summary>
//    public void Equip(CannonInventoryEntry entry)
//    {
//        if (entry == null) return;

//        // If something is already here, unequip it first
//        if (IsOccupied) Unequip();

//        _entry = entry;
//        _entry.isEquipped = true;
//        _entry.equippedSlot = this;

//        // Spawn visual prefab
//        if (entry.data.prefab != null && spawnpoint != null)
//        {
//            GameObject go = Instantiate(entry.data.prefab, spawnpoint);
//            RectTransform rt = go.GetComponent<RectTransform>();
//            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }

//            _controller = go.GetComponent<CannonController>();
//            _controller?.Setup(entry.data);
//        }

//        RefreshVisuals();
//        Debug.Log($"[CannonSlot] Equipped '{entry.data.cannonName}' (id={entry.inventoryId})");
//    }

//    /// <summary>
//    /// Called by RemoveButton or by CannonPanelManager (Unequip button in inventory).
//    /// Returns the cannon to the inventory — upgrade progress is fully preserved.
//    /// </summary>
//    public void Unequip()
//    {
//        if (!IsOccupied) return;

//        _entry.isEquipped = false;
//        _entry.equippedSlot = null;

//        if (_controller != null)
//        {
//            Destroy(_controller.gameObject);
//            _controller = null;
//        }

//        Debug.Log($"[CannonSlot] Unequipped '{_entry.data.cannonName}' (id={_entry.inventoryId})");
//        _entry = null;

//        RefreshVisuals();

//        // Refresh the panel so this cannon reappears in the inventory list
//        CannonPanelManager.Instance?.RefreshAfterUnequip();
//    }

//    // ── Button handlers ────────────────────────────────────────────────────────

//    private void OnAddClicked()
//    {
//        // Open the cannon panel, telling it this slot is the target
//        CannonPanelManager.Instance?.OpenPanel(this);
//    }

//    private void OnRemoveClicked() => Unequip();

//    // ── Visuals ────────────────────────────────────────────────────────────────

//    private void RefreshVisuals()
//    {
//        bool occupied = IsOccupied;

//        if (emptySlotVisual != null) emptySlotVisual.SetActive(!occupied);
//        if (slotHighLight != null) slotHighLight.SetActive(false);

//        // AddButton always visible so player can swap cannon; RemoveButton only when occupied
//        if (removeButton != null) removeButton.gameObject.SetActive(occupied);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CANNON PANEL — CannonSlot  (Prefab-ready)
///
/// ════ WHAT'S NEW IN THIS VERSION ═════════════════════════════════════════════
///
///  1. PREFAB-READY — all child references are auto-wired by name in Awake().
///     No Inspector dragging required. Drop this prefab anywhere in the scene
///     and it configures itself.
///
///  2. DRAG AND DROP — drag an equipped cannon from one slot to another.
///     • Dragging starts a semi-transparent ghost image that follows the cursor.
///     • Dropping on an occupied slot  → the two cannons swap.
///     • Dropping on an empty slot     → cannon moves there.
///     • Dropping anywhere else        → cannon stays in the source slot.
///     • The destination slot highlights on hover while a drag is in progress.
///
/// ════ REQUIRED CHILD HIERARCHY (exact GameObject names) ══════════════════════
///
///   CannonSlot  ← root: this script + CanvasGroup (added automatically)
///   ├── Spawnpoint         cannon prefab is Instantiated here
///   ├── EmptySlotVisual    shown when slot is empty
///   ├── SlotHighLight      shown while a dragged cannon hovers over this slot
///   ├── RemoveButton       Button — unequips the current cannon
///   └── AddButton          Button — opens the Cannon Panel
///
/// ════ NOTES FOR THE DESIGNER ══════════════════════════════════════════════════
///  • The slot root needs a Graphic (e.g. a transparent Image) so the Unity
///    EventSystem can detect pointer events (drag, drop, hover).
///  • The slot root needs a CanvasGroup (auto-added by RequireComponent) which
///    is used for the drag-source fade effect.
///  • Ensure a GraphicRaycaster is on the parent Canvas.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CannonSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    // ════════════════════════════════════════
    // AUTO-WIRED CHILDREN  (by name in Awake)
    // ════════════════════════════════════════

    private Transform _spawnpoint;
    private GameObject _emptySlotVisual;
    private GameObject _slotHighLight;
    private Button _removeButton;
    private Button _addButton;
    private CanvasGroup _canvasGroup;

    // ════════════════════════════════════════
    // RUNTIME STATE
    // ════════════════════════════════════════

    private CannonInventoryEntry _entry;
    private CannonController _controller;

    public bool IsOccupied => _entry != null;
    public CannonInventoryEntry Entry => _entry;

    // ════════════════════════════════════════
    // DRAG STATE  (static — shared across all slots)
    // ════════════════════════════════════════

    private static CannonSlot s_dragSource;
    private static CannonInventoryEntry s_dragEntry;
    private static GameObject s_ghost;
    private static Canvas s_rootCanvas;

    // ════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════

    private void Awake()
    {
        // ── Wire children by name ─────────────────────────────────────────────
        _canvasGroup = GetComponent<CanvasGroup>();

        Transform t = transform;

        var spawnT = t.Find("Spawnpoint");
        if (spawnT != null) _spawnpoint = spawnT;

        var emptyT = t.Find("EmptySlotVisual");
        if (emptyT != null) _emptySlotVisual = emptyT.gameObject;

        var hlT = t.Find("SlotHighLight");
        if (hlT != null) _slotHighLight = hlT.gameObject;

        var removeT = t.Find("RemoveButton");
        if (removeT != null) _removeButton = removeT.GetComponent<Button>();

        var addT = t.Find("AddButton");
        if (addT != null) _addButton = addT.GetComponent<Button>();

        // ── Wire button listeners ─────────────────────────────────────────────
        _addButton?.onClick.AddListener(OnAddClicked);
        _removeButton?.onClick.AddListener(OnRemoveClicked);

        RefreshVisuals();
    }

    // ════════════════════════════════════════
    // PUBLIC API
    // ════════════════════════════════════════

    /// <summary>
    /// Equips a cannon entry onto this slot.
    /// Called by CannonPanelManager (Equip button) or drag-and-drop (SwapWith).
    /// </summary>
    public void Equip(CannonInventoryEntry entry)
    {
        if (entry == null) return;
        if (IsOccupied) Unequip();    // clear existing cannon first

        _entry = entry;
        _entry.isEquipped = true;
        _entry.equippedSlot = this;

        // Instantiate the cannon's visual prefab inside the Spawnpoint
        if (entry.data?.prefab != null && _spawnpoint != null)
        {
            GameObject go = Instantiate(entry.data.prefab, _spawnpoint);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
            _controller = go.GetComponent<CannonController>();
            _controller?.Setup(entry.data);
        }

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Equipped '{entry.data.cannonName}' (id={entry.inventoryId}) → {name}");
    }

    /// <summary>
    /// Unequips the current cannon, returning it to the inventory.
    /// Called by the Remove button or CannonPanelManager.
    /// </summary>
    public void Unequip()
    {
        if (!IsOccupied) return;

        _entry.isEquipped = false;
        _entry.equippedSlot = null;

        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }

        Debug.Log($"[CannonSlot] Unequipped '{_entry.data.cannonName}' (id={_entry.inventoryId}) ← {name}");
        _entry = null;

        RefreshVisuals();
        CannonPanelManager.Instance?.RefreshAfterUnequip();
    }

    // ════════════════════════════════════════
    // DRAG — SOURCE  (this slot is being dragged FROM)
    // ════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only occupied slots can be dragged
        if (!IsOccupied) return;

        s_dragSource = this;
        s_dragEntry = _entry;
        s_rootCanvas = FindRootCanvas();

        // Create ghost image on top of all UI
        if (s_rootCanvas != null)
        {
            s_ghost = new GameObject("DragGhost",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            s_ghost.transform.SetParent(s_rootCanvas.transform, false);
            s_ghost.transform.SetAsLastSibling();   // render on top

            // Ghost is visible but doesn't block raycasts (so drop events reach slots)
            var cg = s_ghost.GetComponent<CanvasGroup>();
            cg.alpha = 0.75f;
            cg.blocksRaycasts = false;

            // Use the cannon's preview sprite on the ghost
            var img = s_ghost.GetComponent<Image>();
            Sprite preview = s_dragEntry.data.previewSprite
                ?? (s_dragEntry.data.idleSprites?.Length > 0 ? s_dragEntry.data.idleSprites[0] : null);
            if (preview != null) { img.sprite = preview; img.preserveAspect = true; }

            var rt = s_ghost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 80f);

            MoveGhostToPointer(eventData);
        }

        // Fade source slot slightly while dragging
        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (s_dragSource != this) return;
        MoveGhostToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (s_dragSource != this) return;

        // If ghost still alive the drag wasn't caught by any valid drop target —
        // restore the source slot (cannon stays where it was; nothing changes).
        if (s_ghost != null)
        {
            DestroyGhost();
            // No state change needed — entry is still assigned to this slot
        }

        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        s_dragSource = null;
        s_dragEntry = null;
    }

    // ════════════════════════════════════════
    // DROP — TARGET  (something is being dropped ONTO this slot)
    // ════════════════════════════════════════

    public void OnDrop(PointerEventData eventData)
    {
        // No drag in progress, or dropped on the source slot itself
        if (s_dragSource == null || s_dragEntry == null) return;
        if (s_dragSource == this) return;

        CannonSlot source = s_dragSource;
        CannonInventoryEntry draggedEntry = s_dragEntry;

        // Clear drag state and ghost before performing the swap
        DestroyGhost();
        if (source._canvasGroup != null) source._canvasGroup.alpha = 1f;
        s_dragSource = null;
        s_dragEntry = null;

        PerformSwap(source, draggedEntry);
    }

    // ════════════════════════════════════════
    // HOVER HIGHLIGHT  (shows while a drag hovers over this slot)
    // ════════════════════════════════════════

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only light up when a drag from another slot is in progress
        if (s_dragSource != null && s_dragSource != this && _slotHighLight != null)
            _slotHighLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_slotHighLight != null) _slotHighLight.SetActive(false);
    }

    // ════════════════════════════════════════
    // SWAP LOGIC
    // ════════════════════════════════════════

    /// <summary>
    /// Swaps the dragged cannon (from <paramref name="source"/>) into this slot.
    /// If this slot already has a cannon, it moves to <paramref name="source"/>.
    /// </summary>
    private void PerformSwap(CannonSlot source, CannonInventoryEntry draggedEntry)
    {
        // Capture the current occupant of this (target) slot before clearing it
        CannonInventoryEntry targetEntry = IsOccupied ? _entry : null;

        // Silent-unequip both slots (no RefreshAfterUnequip spam; we do one pass at the end)
        source.UnequipSilent();
        if (IsOccupied) UnequipSilent();

        // Re-equip: dragged cannon → this slot; this slot's old cannon → source slot
        Equip(draggedEntry);
        if (targetEntry != null) source.Equip(targetEntry);

        // One UI refresh pass
        CannonPanelManager.Instance?.RefreshAfterUnequip();

        Debug.Log($"[CannonSlot] Swap: '{draggedEntry.data.cannonName}' → {name}"
            + (targetEntry != null ? $" | '{targetEntry.data.cannonName}' → {source.name}" : ""));
    }

    /// <summary>
    /// Removes the cannon entry without triggering a UI refresh.
    /// Used internally so we can batch two unequips before a RefreshAfterUnequip.
    /// </summary>
    private void UnequipSilent()
    {
        if (!IsOccupied) return;

        _entry.isEquipped = false;
        _entry.equippedSlot = null;

        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }

        _entry = null;
        RefreshVisuals();
    }

    // ════════════════════════════════════════
    // BUTTON HANDLERS
    // ════════════════════════════════════════

    //private void OnAddClicked() => CannonPanelManager.Instance?.OnPanelOpene(this);

    private void OnAddClicked() => CannonPanelManager.Instance?.OnPanelOpened(this);
    private void OnRemoveClicked() => Unequip();

    // ════════════════════════════════════════
    // VISUALS
    // ════════════════════════════════════════

    private void RefreshVisuals()
    {
        bool occupied = IsOccupied;
        if (_emptySlotVisual != null) _emptySlotVisual.SetActive(!occupied);
        if (_slotHighLight != null) _slotHighLight.SetActive(false);
        if (_removeButton != null) _removeButton.gameObject.SetActive(occupied);
        // AddButton is always visible so the player can swap/open the panel at any time
    }

    // ════════════════════════════════════════
    // GHOST HELPERS  (static — operate on the shared ghost)
    // ════════════════════════════════════════

    private static void MoveGhostToPointer(PointerEventData eventData)
    {
        if (s_ghost == null || s_rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            s_rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            s_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : s_rootCanvas.worldCamera,
            out Vector2 localPos);

        s_ghost.GetComponent<RectTransform>().anchoredPosition = localPos;
    }

    private static void DestroyGhost()
    {
        if (s_ghost != null) { Destroy(s_ghost); s_ghost = null; }
    }

    // ════════════════════════════════════════
    // UTILITY
    // ════════════════════════════════════════

    /// <summary>Walks up the hierarchy to find the root-most Canvas.</summary>
    private Canvas FindRootCanvas()
    {
        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
        if (all == null || all.Length == 0) return null;
        return all[all.Length - 1];   // last in array = highest ancestor
    }
}