//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// CANNON PANEL — CannonSlot  (Prefab-ready)
/////
///// ════ WHAT'S NEW IN THIS VERSION ═════════════════════════════════════════════
/////
/////  1. PREFAB-READY — all child references are auto-wired by name in Awake().
/////     No Inspector dragging required. Drop this prefab anywhere in the scene
/////     and it configures itself.
/////
/////  2. DRAG AND DROP — drag an equipped cannon from one slot to another.
/////     • Dragging starts a semi-transparent ghost image that follows the cursor.
/////     • Dropping on an occupied slot  → the two cannons swap.
/////     • Dropping on an empty slot     → cannon moves there.
/////     • Dropping anywhere else        → cannon stays in the source slot.
/////     • The destination slot highlights on hover while a drag is in progress.
/////
///// ════ REQUIRED CHILD HIERARCHY (exact GameObject names) ══════════════════════
/////
/////   CannonSlot  ← root: this script + CanvasGroup (added automatically)
/////   ├── Spawnpoint         cannon prefab is Instantiated here
/////   ├── EmptySlotVisual    shown when slot is empty
/////   ├── SlotHighLight      shown while a dragged cannon hovers over this slot
/////   ├── RemoveButton       Button — unequips the current cannon
/////   └── AddButton          Button — opens the Cannon Panel
/////
///// ════ NOTES FOR THE DESIGNER ══════════════════════════════════════════════════
/////  • The slot root needs a Graphic (e.g. a transparent Image) so the Unity
/////    EventSystem can detect pointer events (drag, drop, hover).
/////  • The slot root needs a CanvasGroup (auto-added by RequireComponent) which
/////    is used for the drag-source fade effect.
/////  • Ensure a GraphicRaycaster is on the parent Canvas.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class CannonSlot : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler,
//    IDropHandler,
//    IPointerEnterHandler, IPointerExitHandler
//{
//    // ════════════════════════════════════════
//    // AUTO-WIRED CHILDREN  (by name in Awake)
//    // ════════════════════════════════════════

//    private Transform _spawnpoint;
//    private GameObject _emptySlotVisual;
//    private GameObject _slotHighLight;
//    private Button _removeButton;
//    private Button _addButton;
//    private CanvasGroup _canvasGroup;

//    // ════════════════════════════════════════
//    // RUNTIME STATE
//    // ════════════════════════════════════════

//    private CannonInventoryEntry _entry;
//    private CannonController _controller;
//    private Button _cannonClickBtn;   // click-catcher added to the equipped prefab GO

//    public bool IsOccupied => _entry != null;
//    public CannonInventoryEntry Entry => _entry;

//    // ════════════════════════════════════════
//    // DRAG STATE  (static — shared across all slots)
//    // ════════════════════════════════════════

//    private static CannonSlot s_dragSource;
//    private static CannonInventoryEntry s_dragEntry;
//    private static GameObject s_ghost;
//    private static Canvas s_rootCanvas;

//    // ════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ════════════════════════════════════════

//    private void Awake()
//    {
//        // ── Wire children by name ─────────────────────────────────────────────
//        _canvasGroup = GetComponent<CanvasGroup>();

//        Transform t = transform;

//        var spawnT = t.Find("Spawnpoint");
//        if (spawnT != null) _spawnpoint = spawnT;

//        var emptyT = t.Find("EmptySlotVisual");
//        if (emptyT != null) _emptySlotVisual = emptyT.gameObject;

//        var hlT = t.Find("SlotHighLight");
//        if (hlT != null) _slotHighLight = hlT.gameObject;

//        var removeT = t.Find("RemoveButton");
//        if (removeT != null) _removeButton = removeT.GetComponent<Button>();

//        var addT = t.Find("AddButton");
//        if (addT != null) _addButton = addT.GetComponent<Button>();

//        // ── Wire button listeners ─────────────────────────────────────────────
//        _addButton?.onClick.AddListener(OnAddClicked);
//        _removeButton?.onClick.AddListener(OnRemoveClicked);

//        RefreshVisuals();
//    }

//    // ════════════════════════════════════════
//    // PUBLIC API
//    // ════════════════════════════════════════

//    /// <summary>
//    /// Equips a cannon entry onto this slot.
//    /// Called by CannonPanelManager (Equip button) or drag-and-drop (SwapWith).
//    /// </summary>
//    public void Equip(CannonInventoryEntry entry)
//    {
//        if (entry == null) return;
//        if (IsOccupied) Unequip();    // clear existing cannon first

//        _entry = entry;
//        _entry.isEquipped = true;
//        _entry.equippedSlot = this;

//        // Instantiate the cannon's visual prefab inside the Spawnpoint
//        if (entry.data?.prefab != null && _spawnpoint != null)
//        {
//            GameObject go = Instantiate(entry.data.prefab, _spawnpoint);
//            RectTransform rt = go.GetComponent<RectTransform>();
//            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
//            _controller = go.GetComponent<CannonController>();
//            _controller?.Setup(entry.data);

//            // Wire a click on the cannon prefab → open the Inventory panel for this slot.
//            // Re-use an existing Button on the prefab root, or add one if absent.
//            _cannonClickBtn = go.GetComponent<Button>() ?? go.AddComponent<Button>();

//            // A Button requires a targetGraphic to receive pointer events.
//            // Use the root Image if present, otherwise add a fully-transparent overlay.
//            if (_cannonClickBtn.targetGraphic == null)
//            {
//                Image img = go.GetComponent<Image>();
//                if (img == null)
//                {
//                    img = go.AddComponent<Image>();
//                    img.color = Color.clear;   // invisible but catches raycasts
//                }
//                _cannonClickBtn.targetGraphic = img;
//            }

//            _cannonClickBtn.onClick.AddListener(OnCannonClicked);
//        }

//        RefreshVisuals();
//        Debug.Log($"[CannonSlot] Equipped '{entry.data.cannonName}' (id={entry.inventoryId}) → {name}");
//    }

//    /// <summary>
//    /// Unequips the current cannon, returning it to the inventory.
//    /// Called by the Remove button or CannonPanelManager.
//    /// </summary>
//    public void Unequip()
//    {
//        if (!IsOccupied) return;

//        _entry.isEquipped = false;
//        _entry.equippedSlot = null;

//        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }
//        _cannonClickBtn = null;   // GO destroyed above; clear reference

//        Debug.Log($"[CannonSlot] Unequipped '{_entry.data.cannonName}' (id={_entry.inventoryId}) ← {name}");
//        _entry = null;

//        RefreshVisuals();
//        CannonPanelManager.Instance?.RefreshAfterUnequip();
//    }

//    // ════════════════════════════════════════
//    // DRAG — SOURCE  (this slot is being dragged FROM)
//    // ════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        // Only occupied slots can be dragged
//        if (!IsOccupied) return;

//        s_dragSource = this;
//        s_dragEntry = _entry;
//        s_rootCanvas = FindRootCanvas();

//        // Create ghost image on top of all UI
//        if (s_rootCanvas != null)
//        {
//            s_ghost = new GameObject("DragGhost",
//                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
//            s_ghost.transform.SetParent(s_rootCanvas.transform, false);
//            s_ghost.transform.SetAsLastSibling();   // render on top

//            // Ghost is visible but doesn't block raycasts (so drop events reach slots)
//            var cg = s_ghost.GetComponent<CanvasGroup>();
//            cg.alpha = 0.75f;
//            cg.blocksRaycasts = false;

//            // Use the cannon's preview sprite on the ghost
//            var img = s_ghost.GetComponent<Image>();
//            Sprite preview = s_dragEntry.data.previewSprite
//                ?? (s_dragEntry.data.idleSprites?.Length > 0 ? s_dragEntry.data.idleSprites[0] : null);
//            if (preview != null) { img.sprite = preview; img.preserveAspect = true; }

//            var rt = s_ghost.GetComponent<RectTransform>();
//            rt.sizeDelta = new Vector2(80f, 80f);

//            MoveGhostToPointer(eventData);
//        }

//        // Fade source slot slightly while dragging
//        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (s_dragSource != this) return;
//        MoveGhostToPointer(eventData);
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (s_dragSource != this) return;

//        // If ghost still alive the drag wasn't caught by any valid drop target —
//        // restore the source slot (cannon stays where it was; nothing changes).
//        if (s_ghost != null)
//        {
//            DestroyGhost();
//            // No state change needed — entry is still assigned to this slot
//        }

//        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

//        s_dragSource = null;
//        s_dragEntry = null;
//    }

//    // ════════════════════════════════════════
//    // DROP — TARGET  (something is being dropped ONTO this slot)
//    // ════════════════════════════════════════

//    public void OnDrop(PointerEventData eventData)
//    {
//        // No drag in progress, or dropped on the source slot itself
//        if (s_dragSource == null || s_dragEntry == null) return;
//        if (s_dragSource == this) return;

//        CannonSlot source = s_dragSource;
//        CannonInventoryEntry draggedEntry = s_dragEntry;

//        // Clear drag state and ghost before performing the swap
//        DestroyGhost();
//        if (source._canvasGroup != null) source._canvasGroup.alpha = 1f;
//        s_dragSource = null;
//        s_dragEntry = null;

//        PerformSwap(source, draggedEntry);
//    }

//    // ════════════════════════════════════════
//    // HOVER HIGHLIGHT  (shows while a drag hovers over this slot)
//    // ════════════════════════════════════════

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        // Only light up when a drag from another slot is in progress
//        if (s_dragSource != null && s_dragSource != this && _slotHighLight != null)
//            _slotHighLight.SetActive(true);
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        if (_slotHighLight != null) _slotHighLight.SetActive(false);
//    }

//    // ════════════════════════════════════════
//    // SWAP LOGIC
//    // ════════════════════════════════════════

//    /// <summary>
//    /// Swaps the dragged cannon (from <paramref name="source"/>) into this slot.
//    /// If this slot already has a cannon, it moves to <paramref name="source"/>.
//    /// </summary>
//    private void PerformSwap(CannonSlot source, CannonInventoryEntry draggedEntry)
//    {
//        // Capture the current occupant of this (target) slot before clearing it
//        CannonInventoryEntry targetEntry = IsOccupied ? _entry : null;

//        // Silent-unequip both slots (no RefreshAfterUnequip spam; we do one pass at the end)
//        source.UnequipSilent();
//        if (IsOccupied) UnequipSilent();

//        // Re-equip: dragged cannon → this slot; this slot's old cannon → source slot
//        Equip(draggedEntry);
//        if (targetEntry != null) source.Equip(targetEntry);

//        // One UI refresh pass
//        CannonPanelManager.Instance?.RefreshAfterUnequip();

//        Debug.Log($"[CannonSlot] Swap: '{draggedEntry.data.cannonName}' → {name}"
//            + (targetEntry != null ? $" | '{targetEntry.data.cannonName}' → {source.name}" : ""));
//    }

//    /// <summary>
//    /// Removes the cannon entry without triggering a UI refresh.
//    /// Used internally so we can batch two unequips before a RefreshAfterUnequip.
//    /// </summary>
//    private void UnequipSilent()
//    {
//        if (!IsOccupied) return;

//        _entry.isEquipped = false;
//        _entry.equippedSlot = null;

//        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }
//        _cannonClickBtn = null;   // GO destroyed above; clear reference

//        _entry = null;
//        RefreshVisuals();
//    }

//    // ════════════════════════════════════════
//    // BUTTON HANDLERS
//    // ════════════════════════════════════════

//    //private void OnAddClicked() => CannonPanelManager.Instance?.OnPanelOpene(this);

//    /// <summary>Called when the player clicks the cannon prefab inside this slot.
//    /// Opens the Cannon Panel directly in Inventory mode, pre-selecting this cannon.</summary>
//    private void OnCannonClicked() => CannonPanelManager.Instance?.OpenAtInventory(this);

//    private void OnAddClicked() => CannonPanelManager.Instance?.OnPanelOpened(this);
//    private void OnRemoveClicked() => Unequip();

//    // ════════════════════════════════════════
//    // VISUALS
//    // ════════════════════════════════════════

//    private void RefreshVisuals()
//    {
//        bool occupied = IsOccupied;
//        if (_emptySlotVisual != null) _emptySlotVisual.SetActive(!occupied);
//        if (_slotHighLight != null) _slotHighLight.SetActive(false);
//        if (_removeButton != null) _removeButton.gameObject.SetActive(occupied);
//        // AddButton is always visible so the player can swap/open the panel at any time
//    }

//    // ════════════════════════════════════════
//    // GHOST HELPERS  (static — operate on the shared ghost)
//    // ════════════════════════════════════════

//    private static void MoveGhostToPointer(PointerEventData eventData)
//    {
//        if (s_ghost == null || s_rootCanvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            s_rootCanvas.GetComponent<RectTransform>(),
//            eventData.position,
//            s_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                ? null
//                : s_rootCanvas.worldCamera,
//            out Vector2 localPos);

//        s_ghost.GetComponent<RectTransform>().anchoredPosition = localPos;
//    }

//    private static void DestroyGhost()
//    {
//        if (s_ghost != null) { Destroy(s_ghost); s_ghost = null; }
//    }

//    // ════════════════════════════════════════
//    // UTILITY
//    // ════════════════════════════════════════

//    /// <summary>Walks up the hierarchy to find the root-most Canvas.</summary>
//    private Canvas FindRootCanvas()
//    {
//        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//        if (all == null || all.Length == 0) return null;
//        return all[all.Length - 1];   // last in array = highest ancestor
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CANNON PANEL — CannonSlot  (Prefab-ready)
///
/// ════ CHILD HIERARCHY (exact GameObject names) ═══════════════════════
///
///   CannonSlot
///   ├── Spawnpoint           ← cannon prefab spawns here (front)
///   ├── SoldierSpawnpoint    ← soldier stands here (behind the cannon)
///   ├── EmptySlotVisual      ← shown when no cannon equipped
///   ├── SoldierEmptyVisual   ← shown when no soldier assigned (optional)
///   ├── SlotHighLight        ← lights up during any drag hover
///   ├── RemoveButton         ← unequips the cannon
///   └── AddButton            ← opens the Cannon Panel
///
/// ════ DRAG AND DROP ═══════════════════════════════════════════════════
///
///   CANNON — drag an equipped cannon between CannonSlots to swap.
///   SOLDIER — drag a SoldierDragDrop soldier onto this slot to station
///             them behind the cannon. Drop on empty space to remove.
///
/// ════ NOTES ═══════════════════════════════════════════════════════════
///   • Root needs a Graphic (transparent Image) for pointer events.
///   • CanvasGroup is auto-added (RequireComponent).
///   • Parent Canvas needs a GraphicRaycaster.
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
    private Transform _soldierSpawnpoint;
    private GameObject _emptySlotVisual;
    private GameObject _soldierEmptyVisual;
    private GameObject _slotHighLight;
    private Button _removeButton;
    private Button _addButton;
    private CanvasGroup _canvasGroup;

    // ════════════════════════════════════════
    // CANNON STATE
    // ════════════════════════════════════════

    private CannonInventoryEntry _entry;
    private CannonController _controller;
    private Button _cannonClickBtn;

    public bool IsOccupied => _entry != null;
    public CannonInventoryEntry Entry => _entry;

    // ════════════════════════════════════════
    // SOLDIER STATE
    // ════════════════════════════════════════

    private SoldierDragDrop _assignedSoldier;

    public bool HasSoldier => _assignedSoldier != null;
    public SoldierDragDrop AssignedSoldier => _assignedSoldier;

    // ════════════════════════════════════════
    // CANNON DRAG STATE  (static — shared across all slots)
    // ════════════════════════════════════════

    private static CannonSlot s_dragSource;
    private static CannonInventoryEntry s_dragEntry;
    private static GameObject s_ghost;
    private static Canvas s_rootCanvas;

    // ════════════════════════════════════════
    // AWAKE
    // ════════════════════════════════════════

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        Transform t = transform;

        var spawnT = t.Find("Spawnpoint");
        if (spawnT != null) _spawnpoint = spawnT;

        var soldierT = t.Find("SoldierSpawnpoint");
        if (soldierT != null) _soldierSpawnpoint = soldierT;

        var emptyT = t.Find("EmptySlotVisual");
        if (emptyT != null) _emptySlotVisual = emptyT.gameObject;

        var sEmptyT = t.Find("SoldierEmptyVisual");
        if (sEmptyT != null) _soldierEmptyVisual = sEmptyT.gameObject;

        var hlT = t.Find("SlotHighLight");
        if (hlT != null) _slotHighLight = hlT.gameObject;

        var removeT = t.Find("RemoveButton");
        if (removeT != null) _removeButton = removeT.GetComponent<Button>();

        var addT = t.Find("AddButton");
        if (addT != null) _addButton = addT.GetComponent<Button>();

        _addButton?.onClick.AddListener(OnAddClicked);
        _removeButton?.onClick.AddListener(OnRemoveClicked);

        RefreshVisuals();
    }

    // ════════════════════════════════════════
    // CANNON PUBLIC API
    // ════════════════════════════════════════

    /// <summary>Equips a cannon onto this slot.</summary>
    public void Equip(CannonInventoryEntry entry)
    {
        if (entry == null) return;
        if (IsOccupied) Unequip();

        _entry = entry;
        _entry.isEquipped = true;
        _entry.equippedSlot = this;

        if (entry.data?.prefab != null && _spawnpoint != null)
        {
            GameObject go = Instantiate(entry.data.prefab, _spawnpoint);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }

            _controller = go.GetComponent<CannonController>();
            _controller?.Setup(entry.data);

            _cannonClickBtn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            if (_cannonClickBtn.targetGraphic == null)
            {
                Image img = go.GetComponent<Image>();
                if (img == null) { img = go.AddComponent<Image>(); img.color = Color.clear; }
                _cannonClickBtn.targetGraphic = img;
            }
            _cannonClickBtn.onClick.AddListener(OnCannonClicked);
        }

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Equipped '{entry.data.cannonName}' → {name}");
    }

    /// <summary>Unequips the cannon, returning it to inventory.</summary>
    public void Unequip()
    {
        if (!IsOccupied) return;

        _entry.isEquipped = false;
        _entry.equippedSlot = null;

        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }
        _cannonClickBtn = null;

        Debug.Log($"[CannonSlot] Unequipped '{_entry.data.cannonName}' ← {name}");
        _entry = null;

        RefreshVisuals();
        CannonPanelManager.Instance?.RefreshAfterUnequip();
    }

    // ════════════════════════════════════════
    // SOLDIER PUBLIC API
    // ════════════════════════════════════════

    /// <summary>
    /// Stations a soldier behind the cannon.
    /// Called by OnDrop when SoldierDragDrop.CurrentlyDragging is set.
    /// </summary>
    public void AssignSoldier(SoldierDragDrop soldier)
    {
        if (soldier == null) return;

        if (_soldierSpawnpoint == null)
        {
            Debug.LogWarning("[CannonSlot] 'SoldierSpawnpoint' child not found! " +
                             "Add a child GameObject with that exact name.", this);
            return;
        }

        // Release previous soldier without triggering their SnapBack
        if (HasSoldier) ReleaseSoldier(notify: false);

        _assignedSoldier = soldier;

        // Tell the soldier to move to this cannon's spawnpoint
        soldier.PlaceAtCannonSlot(this, _soldierSpawnpoint);

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Soldier '{soldier.name}' stationed at {name}.");
    }

    /// <summary>
    /// Removes the soldier from this slot.
    /// notify = true  → soldier.RemoveFromCannonSlot() is called (soldier snaps home).
    /// notify = false → soldier is already handling relocation (avoids loop).
    /// </summary>
    public void ReleaseSoldier(bool notify = true)
    {
        if (!HasSoldier) return;

        SoldierDragDrop soldier = _assignedSoldier;
        _assignedSoldier = null;

        if (notify) soldier.RemoveFromCannonSlot();

        RefreshVisuals();
        Debug.Log($"[CannonSlot] Soldier '{soldier.name}' released from {name}.");
    }

    // ════════════════════════════════════════
    // CANNON DRAG — SOURCE
    // ════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsOccupied) return;

        s_dragSource = this;
        s_dragEntry = _entry;
        s_rootCanvas = FindRootCanvas();

        if (s_rootCanvas != null)
        {
            s_ghost = new GameObject("DragGhost",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            s_ghost.transform.SetParent(s_rootCanvas.transform, false);
            s_ghost.transform.SetAsLastSibling();

            var cg = s_ghost.GetComponent<CanvasGroup>();
            cg.alpha = 0.75f; cg.blocksRaycasts = false;

            var img = s_ghost.GetComponent<Image>();
            Sprite preview = s_dragEntry.data.previewSprite
                ?? (s_dragEntry.data.idleSprites?.Length > 0
                    ? s_dragEntry.data.idleSprites[0] : null);
            if (preview != null) { img.sprite = preview; img.preserveAspect = true; }

            s_ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 80f);
            MoveGhostToPointer(eventData);
        }

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

        if (s_ghost != null) DestroyGhost();
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        s_dragSource = null;
        s_dragEntry = null;
    }

    // ════════════════════════════════════════
    // DROP — TARGET
    // Handles BOTH cannon swaps AND soldier drops
    // ════════════════════════════════════════

    public void OnDrop(PointerEventData eventData)
    {
        // ── Soldier drop ──────────────────────────────────────────────────────
        if (SoldierDragDrop.CurrentlyDragging != null)
        {
            AssignSoldier(SoldierDragDrop.CurrentlyDragging);
            return;
        }

        // ── Cannon swap ───────────────────────────────────────────────────────
        if (s_dragSource == null || s_dragEntry == null) return;
        if (s_dragSource == this) return;

        CannonSlot source = s_dragSource;
        CannonInventoryEntry draggedEntry = s_dragEntry;

        DestroyGhost();
        if (source._canvasGroup != null) source._canvasGroup.alpha = 1f;
        s_dragSource = null;
        s_dragEntry = null;

        PerformSwap(source, draggedEntry);
    }

    // ════════════════════════════════════════
    // HOVER HIGHLIGHT
    // ════════════════════════════════════════

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool cannonDrag = s_dragSource != null && s_dragSource != this;
        bool soldierDrag = SoldierDragDrop.CurrentlyDragging != null;

        if ((cannonDrag || soldierDrag) && _slotHighLight != null)
            _slotHighLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_slotHighLight != null) _slotHighLight.SetActive(false);
    }

    // ════════════════════════════════════════
    // CANNON SWAP LOGIC
    // ════════════════════════════════════════

    private void PerformSwap(CannonSlot source, CannonInventoryEntry draggedEntry)
    {
        CannonInventoryEntry targetEntry = IsOccupied ? _entry : null;

        source.UnequipSilent();
        if (IsOccupied) UnequipSilent();

        Equip(draggedEntry);
        if (targetEntry != null) source.Equip(targetEntry);

        CannonPanelManager.Instance?.RefreshAfterUnequip();

        Debug.Log($"[CannonSlot] Swap: '{draggedEntry.data.cannonName}' → {name}"
            + (targetEntry != null
                ? $" | '{targetEntry.data.cannonName}' → {source.name}" : ""));
    }

    private void UnequipSilent()
    {
        if (!IsOccupied) return;

        _entry.isEquipped = false;
        _entry.equippedSlot = null;

        if (_controller != null) { Destroy(_controller.gameObject); _controller = null; }
        _cannonClickBtn = null;

        _entry = null;
        RefreshVisuals();
    }

    // ════════════════════════════════════════
    // BUTTON HANDLERS
    // ════════════════════════════════════════

    private void OnCannonClicked() => CannonPanelManager.Instance?.OpenAtInventory(this);
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
        if (_soldierEmptyVisual != null) _soldierEmptyVisual.SetActive(!HasSoldier);
    }

    // ════════════════════════════════════════
    // GHOST HELPERS
    // ════════════════════════════════════════

    private static void MoveGhostToPointer(PointerEventData eventData)
    {
        if (s_ghost == null || s_rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            s_rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            s_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : s_rootCanvas.worldCamera,
            out Vector2 local);
        s_ghost.GetComponent<RectTransform>().anchoredPosition = local;
    }

    private static void DestroyGhost()
    {
        if (s_ghost != null) { Destroy(s_ghost); s_ghost = null; }
    }

    private Canvas FindRootCanvas()
    {
        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
    }
}