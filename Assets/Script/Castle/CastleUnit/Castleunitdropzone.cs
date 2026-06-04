using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Drop zone for a Cannon unit on an exposed castle block.
///
/// Soldier visibility rules:
///   PlaceUnit    → cannon arrives    → soldier SHOWN,  expansion slot hidden.
///   DetachUnit   → cannon dragged    → soldier HIDDEN, expansion slot shown.
///   ReattachUnit → cannon snaps back → soldier SHOWN,  expansion slot hidden.
///   RemoveUnit   → cannon destroyed  → soldier HIDDEN, expansion slot shown.
///   MigrateUnitTo → cannon reparented → source soldier HIDDEN, dest soldier SHOWN.
///
/// Child hierarchy (auto-wired by name in Awake):
///   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///   ├── EmptyVisual     hint shown only during a valid drag hover
///   ├── Highlight       glow frame shown during any drag hover
///   └── Soldier         Image — hidden by default, shown when cannon is placed
/// </summary>
[RequireComponent(typeof(Image))]
public class CastleUnitDropZone : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
{
    // ── Inspector ─────────────────────────────────────────────────
    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
    public CastleUnitType acceptedType;

    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

    // ── Auto-wired ────────────────────────────────────────────────
    private Image _bg;
    private GameObject _emptyVisual;
    private GameObject _highlight;
    private GameObject _soldierImage;
    private GameObject _emptySlotZone;   // shown only in Cannon scene when zone is empty
    private Button _removeButton;

    // ── State ─────────────────────────────────────────────────────
    public bool HasUnit { get; private set; }
    public int PlacedVariantId { get; private set; } = -1;

    /// <summary>
    /// The ExpansionSlot above this block that should be shown/hidden
    /// as the cannon moves. Auto-linked on drop if not already set.
    /// </summary>
    public ExpansionSlot LinkedExpansionSlot { get; set; }

    private GameObject _placedInstance;
    private CannonInventoryEntry _equippedEntry;  // set by PlaceCannonFromPanel
    private CastleBlockUnitSlot _parentSlot;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _bg = GetComponent<Image>();
        _bg.color = normalColor;
        _bg.raycastTarget = true;
        Debug.Log("[CannonZone] Awake on " + gameObject.name + " parent=" + (transform.parent ? transform.parent.name : "none"));

        _parentSlot = GetComponentInParent<CastleBlockUnitSlot>();

        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
        _highlight = transform.Find("Highlight")?.gameObject;
        _soldierImage = transform.Find("Soldier")?.gameObject;

        _emptySlotZone = transform.Find("EmptySlotVisual")?.gameObject;

        var removeBtnT = transform.Find("RemoveButton");
        if (removeBtnT != null)
        {
            _removeButton = removeBtnT.GetComponent<Button>();
            if (_removeButton != null)
                _removeButton.onClick.AddListener(OnRemoveClicked);
            else
                Debug.LogWarning("[DropZone] RemoveButton found but has no Button component.", this);
        }
        else
        {
            Debug.LogWarning("[DropZone] No child named 'RemoveButton' found on " + gameObject.name + ". Add a Button child with that exact name.", this);
        }

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _soldierImage?.SetActive(false);
        _emptySlotZone?.SetActive(false);   // hidden until Cannon scene is entered
        _removeButton?.gameObject.SetActive(false);

        // Soldier must NOT block raycasts on the cannon above it
        if (_soldierImage != null)
        {
            var img = _soldierImage.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        Debug.Log($"[DropZone] {gameObject.name} received OnGameStateChanged → {state}");
        RefreshRemoveButton();
    }

    private void Start()
    {
        // Force-hide soldier at runtime in case the prefab has it active by default.
        // PlaceUnit() will show it again when a cannon is placed.
        if (!HasUnit)
            _soldierImage?.SetActive(false);

        // Cannon zones start non-interactive and invisible — only the cannon
        // section reveals them. We do NOT deactivate the whole GameObject because
        // the cannon prefab placed inside must always remain visible.
        if (acceptedType == CastleUnitType.Cannon && !HasUnit)
        {
            SetInteractable(false);
            _emptySlotZone?.SetActive(false);  // always hidden until Cannon tab is opened
        }
    }

    /// <summary>
    /// Show or hide every CastleUnitDropZone that accepts Cannons.
    /// Call this when entering/leaving the Cannon panel section.
    /// Does NOT touch the whole GameObject — only the zone overlay visibility
    /// and raycast target, so placed cannon prefabs stay visible at all times.
    /// Zones that are already filled (HasUnit == true) stay non-interactive always.
    /// </summary>
    /// <summary>
    /// Show or hide every CastleUnitDropZone that accepts Cannons.
    /// Call this when entering/leaving the Cannon tab.
    /// When visible=false the entire zone GameObject is deactivated so nothing
    /// inside it (EmptySlotZone, labels, highlights) can show through.
    /// When visible=true, only empty zones are activated (occupied zones stay
    /// deactivated — the cannon prefab inside them is a sibling, not a child,
    /// so deactivating the zone doesn't hide the cannon).
    /// </summary>
    public static void SetCannonZonesVisible(bool visible)
    {
        foreach (var zone in FindObjectsOfType<CastleUnitDropZone>(includeInactive: true))
        {
            if (zone.acceptedType != CastleUnitType.Cannon) continue;

            if (!visible)
            {
                // Hide the zone overlay entirely — cannon prefab placed INSIDE
                // this zone IS a child, so we must NOT deactivate the whole GO.
                // Instead hide just the overlay background + EmptySlotZone.
                zone._emptySlotZone?.SetActive(false);
                zone.SetInteractable(false);
            }
            else if (!zone.HasUnit)
            {
                // Cannon tab opened and zone is empty — show and enable it.
                zone._emptySlotZone?.SetActive(true);
                zone.SetInteractable(true);
            }
            // HasUnit zones are never interactive and EmptySlotZone stays hidden.
        }
    }

    /// <summary>Public wrapper so external classes (e.g. CastleUnitDraggable) can set interactability.</summary>
    public void SetInteractablePublic(bool on) => SetInteractable(on);

    /// <summary>
    /// Enables or disables the zone overlay: background alpha and raycast target.
    /// The zone GameObject itself stays active so placed cannon children are visible.
    /// </summary>
    private void SetInteractable(bool on)
    {
        if (_bg == null) return;
        _bg.raycastTarget = on;
        // Show the zone background colour when interactive, fully transparent when not.
        Color c = _bg.color;
        c.a = on ? normalColor.a : 0f;
        _bg.color = c;
    }

    // ── Standard drag-drop path ───────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;

        if (unit == null) { Debug.Log("[DropZone] OnDrop — nothing dragged."); return; }
        if (unit.unitType != acceptedType) { Debug.Log("[DropZone] Type mismatch."); return; }

        // ── SWAP: this zone already has a cannon — swap the two ──────────────
        if (HasUnit)
        {
            // Read the source zone from the static set at BeginDrag — do NOT read
            // unit.transform.parent because by now it is the root canvas, not the zone.
            CastleUnitDropZone sourceZone = CastleUnitDraggable.OriginalZone;

            if (sourceZone == null || sourceZone == this)
            {
                Debug.Log("[DropZone] Swap — no valid source zone, ignoring.");
                return;
            }

            // Grab the cannon sitting in THIS zone
            CastleUnitDraggable residentUnit = _placedInstance?.GetComponent<CastleUnitDraggable>();
            if (residentUnit == null)
            {
                Debug.Log("[DropZone] Swap — resident has no CastleUnitDraggable, cannot swap.");
                return;
            }

            // Detach resident from this zone (clears HasUnit, shows expansion slot)
            DetachUnit();

            // Place resident into source zone
            sourceZone.AutoLinkExpansionSlot();
            sourceZone.PlaceUnit(residentUnit);
            sourceZone.SetInteractable(false);

            // Place dragged unit here
            AutoLinkExpansionSlot();
            PlaceUnit(unit);
            SetInteractable(false);

            CastleUnitDraggable.NotifyDropSucceeded();
            Debug.Log($"[DropZone] Swapped cannons between {sourceZone.name} and {name}.");
            return;
        }

        // ── NORMAL DROP: zone is empty ────────────────────────────────────────
        if (_parentSlot != null && _parentSlot.IsBlockedByArcher)
        {
            Debug.Log("[DropZone] Blocked — an archer is already stationed on this block.");
            return;
        }
        AutoLinkExpansionSlot();
        PlaceUnit(unit);
        SetInteractable(false);
        CastleUnitDraggable.NotifyDropSucceeded();
    }

    // ── Auto-link expansion slot ──────────────────────────────────

    /// <summary>
    /// Searches the CastleGrid for the ExpansionSlot that sits directly above
    /// this block cell and links it so DetachUnit/RemoveUnit can show it later.
    /// Only runs if LinkedExpansionSlot is not already set.
    /// </summary>
    public void AutoLinkExpansionSlot()
    {
        if (LinkedExpansionSlot != null) return;

        CastleGrid grid = CastleGrid.Instance;
        if (grid == null) return;

        // Find which GridCell this drop zone belongs to by walking up
        GridCell myCell = GetComponentInParent<GridCell>();
        if (myCell == null) return;

        int row = myCell.Row;
        int col = myCell.Col;

        // The expansion slot that places onto this block sits one row above
        GridCell cellAbove = grid.GetCell(row + 1, col);
        if (cellAbove == null) return;

        // Find the ExpansionSlot on that cell
        ExpansionSlot slot = cellAbove.GetComponentInChildren<ExpansionSlot>(includeInactive: true);
        if (slot == null) return;

        LinkedExpansionSlot = slot;
        Debug.Log($"[DropZone] Auto-linked ExpansionSlot at ({row + 1},{col}).");
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Seats the cannon and shows the soldier.
    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
    /// </summary>
    public void PlaceUnit(CastleUnitDraggable unit)
    {
        if (unit == null || HasUnit) return;

        unit.transform.SetParent(transform, worldPositionStays: false);

        RectTransform rt = unit.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (unit.stretchToFillSlot)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = unit.placedSize;
                rt.anchoredPosition = Vector2.zero;
            }
            rt.SetAsLastSibling();
        }

        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

        Image unitImg = unit.GetComponent<Image>();
        if (unitImg != null) unitImg.raycastTarget = true;

        _placedInstance = unit.gameObject;
        HasUnit = true;
        PlacedVariantId = unit.variantId;

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _soldierImage?.SetActive(true);    // ← soldier appears with cannon
        RefreshEmptySlotZone();
        RefreshRemoveButton();
        _removeButton?.transform.SetAsLastSibling();

        Debug.Log($"[DropZone] PlaceUnit — soldier shown. LinkedSlot={(LinkedExpansionSlot != null ? LinkedExpansionSlot.gameObject.name : "none")}");

        // Hide the other zone now that this one is occupied.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Called by CastleUnitDraggable.OnBeginDrag — cannon is being lifted.
    /// Soldier hides. Expansion slot shows so player can drop here again.
    /// </summary>
    public void DetachUnit()
    {
        _placedInstance = null;
        HasUnit = false;
        PlacedVariantId = -1;
        _equippedEntry = null;
        RefreshRemoveButton();

        _soldierImage?.SetActive(false);   // ← soldier hides when cannon lifts
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        RefreshEmptySlotZone();

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            Debug.Log("[DropZone] DetachUnit — soldier hidden, expansion slot shown.");
        }
        else
        {
            // No expansion slot linked — try to auto-find and show it
            Debug.LogWarning("[DropZone] DetachUnit — LinkedExpansionSlot is null! " +
                             "Trying to auto-find...");
            AutoLinkExpansionSlot();
            if (LinkedExpansionSlot != null)
                LinkedExpansionSlot.gameObject.SetActive(true);
        }

        // Zone is now empty while cannon is in the air — reveal the other zone.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Called by CastleUnitDraggable.OnEndDrag on a failed drop — cannon snaps back.
    /// Soldier shows again. Expansion slot hides.
    /// </summary>
    public void ReattachUnit(CastleUnitDraggable unit)
    {
        if (unit == null) return;

        unit.transform.SetParent(transform, worldPositionStays: false);

        // Re-apply the same sizing logic as PlaceUnit so the cannon always
        // snaps back at the correct size regardless of what happened during drag.
        RectTransform rt = unit.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (unit.stretchToFillSlot)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = unit.placedSize;
                rt.anchoredPosition = Vector2.zero;
            }
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        _placedInstance = unit.gameObject;
        HasUnit = true;
        PlacedVariantId = unit.variantId;

        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _soldierImage?.SetActive(true);
        RefreshEmptySlotZone();
        RefreshRemoveButton();
        _removeButton?.transform.SetAsLastSibling();

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(false);
            Debug.Log("[DropZone] ReattachUnit — soldier shown, expansion slot hidden.");
        }

        // Hide the other zone now that this one is re-occupied.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Cannon permanently removed (block removed, etc.).
    /// Hides the soldier and restores the expansion slot.
    /// </summary>
    public void RemoveUnit()
    {
        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

        if (_equippedEntry != null)
        {
            _equippedEntry.isEquipped = false;
            _equippedEntry.equippedSlot = null;
            _equippedEntry = null;
        }

        HasUnit = false;
        PlacedVariantId = -1;

        _soldierImage?.SetActive(false);   // ← soldier hides
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        RefreshEmptySlotZone();

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            LinkedExpansionSlot = null;
            Debug.Log("[DropZone] RemoveUnit — soldier hidden, expansion slot shown.");
        }
        else
        {
            Debug.LogWarning("[DropZone] RemoveUnit — LinkedExpansionSlot is null. " +
                             "Expansion slot cannot be shown automatically.");
        }
        RefreshRemoveButton();

        // Reveal the other zone now that this one is vacant.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Called by CannonPanelManager when the player equips a cannon from the panel.
    /// Instantiates the cannon prefab directly into this drop zone — no drag needed.
    /// </summary>
    public void PlaceCannonFromPanel(GameObject cannonPrefab, CannonInventoryEntry entry)
    {
        if (HasUnit)
        {
            Debug.Log("[DropZone] PlaceCannonFromPanel — slot already occupied.");
            return;
        }
        if (_parentSlot != null && _parentSlot.IsBlockedByArcher)
        {
            Debug.Log("[DropZone] PlaceCannonFromPanel — blocked, an archer is stationed on this block.");
            return;
        }
        if (cannonPrefab == null)
        {
            Debug.LogWarning("[DropZone] PlaceCannonFromPanel — cannonPrefab is null!");
            return;
        }

        // Instantiate the cannon prefab as a child of this zone.
        GameObject go = Instantiate(cannonPrefab, transform);

        // Fit the cannon using the same sizing logic as PlaceUnit (drag-drop path)
        // so size is always consistent regardless of how the cannon was placed.
        RectTransform rt = go.GetComponent<RectTransform>();
        CastleUnitDraggable draggable = go.GetComponent<CastleUnitDraggable>();
        if (rt != null)
        {
            bool stretch = draggable != null ? draggable.stretchToFillSlot : true;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                Vector2 sz = draggable != null ? draggable.placedSize : centeredUnitSize;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = sz;
                rt.anchoredPosition = Vector2.zero;
            }
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        // Set up the CannonController if the prefab has one.
        var controller = go.GetComponent<CannonController>();
        controller?.Setup(entry?.data);

        _placedInstance = go;
        HasUnit = true;
        PlacedVariantId = entry != null ? entry.inventoryId : -1;

        // Mark inventory entry as equipped.
        if (entry != null)
        {
            entry.isEquipped = true;
        }
        _equippedEntry = entry;

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _soldierImage?.SetActive(true);   // soldier shows with cannon
        RefreshEmptySlotZone();

        // Hide the expansion slot so the player can't drop another cannon here.
        if (LinkedExpansionSlot != null)
            LinkedExpansionSlot.gameObject.SetActive(false);

        // Disable interaction on this zone — it's full, nothing to tap anymore.
        // Do NOT deactivate the whole GameObject; the placed cannon must stay visible.
        SetInteractable(false);
        RefreshRemoveButton();
        _removeButton?.transform.SetAsLastSibling();

        Debug.Log($"[DropZone] PlaceCannonFromPanel — placed '{cannonPrefab.name}' in {gameObject.name}");

        // Hide the other zone now that this one is occupied.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Reparents the cannon from this zone into destination.
    /// Source soldier hides, destination soldier shows.
    /// </summary>
    public void MigrateUnitTo(CastleUnitDropZone destination)
    {
        if (destination == null || !HasUnit || destination.HasUnit) return;
        if (_placedInstance == null) return;

        // Move cannon into destination
        _placedInstance.transform.SetParent(destination.transform, worldPositionStays: false);

        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            var draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = draggable != null ? draggable.placedSize : destination.centeredUnitSize;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
        }

        // Hand ownership to destination
        destination._placedInstance = _placedInstance;
        destination.HasUnit = true;
        destination.PlacedVariantId = PlacedVariantId;

        destination._emptyVisual?.SetActive(false);
        destination._highlight?.SetActive(false);
        destination._bg.color = destination.normalColor;
        destination._soldierImage?.SetActive(true);    // ← dest soldier shown
        destination._equippedEntry = _equippedEntry;   // transfer entry ownership
        destination.RefreshEmptySlotZone();
        destination.RefreshRemoveButton();
        if (destination._removeButton != null) destination._removeButton.transform.SetAsLastSibling();

        // Clear this zone
        _placedInstance = null;
        HasUnit = false;
        PlacedVariantId = -1;

        _soldierImage?.SetActive(false);               // ← source soldier hides
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _equippedEntry = null;
        RefreshEmptySlotZone();
        RefreshRemoveButton();

        // Restore expansion slot on source
        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            LinkedExpansionSlot = null;
        }

        Debug.Log($"[DropZone] MigrateUnitTo — cannon moved from '{transform.parent?.name}' " +
                  $"to '{destination.transform.parent?.name}'.");

        // Update mutual-hide for both slots.
        _parentSlot?.NotifyOccupancyChanged();
        destination._parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Switches the zone background between Village and Castle display modes.
    /// </summary>
    /// <summary>
    /// Switches the zone between Cannon-tab-active and all-other-panels state.
    ///
    /// active = true  (Cannon tab open):
    ///   Empty zone  → EmptySlotZone shown, zone is interactable.
    ///   Filled zone → EmptySlotZone hidden, zone non-interactable (cannon visible).
    ///
    /// active = false (Village / Expand / Archer / any other panel):
    ///   ALL overlay visuals hidden. Zone non-interactable.
    ///   Placed cannon children remain visible.
    /// </summary>
    public void SetCannonTabActive(bool active)
    {
        // If trying to activate but the sibling archer zone is occupied,
        // keep this zone hidden — an archer already owns this block.
        if (active && _parentSlot != null && _parentSlot.HasArcher)
        {
            _emptySlotZone?.SetActive(false);
            SetInteractable(false);
            RefreshRemoveButton();
            return;
        }

        if (HasUnit)
        {
            _emptySlotZone?.SetActive(false);
            SetInteractable(false);
        }
        else
        {
            _emptySlotZone?.SetActive(active);
            SetInteractable(active);
        }
        RefreshRemoveButton();
    }

    public void SetVillageMode(bool isVillage)
    {
        // Kept for legacy call-sites. Village = not Cannon tab → hide everything.
        SetCannonTabActive(false);
    }

    // ── Hover ─────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;
        if (unit == null) return;

        bool blockedByArcher = _parentSlot != null && _parentSlot.IsBlockedByArcher;
        bool valid = unit.unitType == acceptedType && !HasUnit && !blockedByArcher;
        _highlight?.SetActive(true);
        _emptyVisual?.SetActive(valid);
        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlight?.SetActive(false);
        _emptyVisual?.SetActive(false);
        _bg.color = normalColor;
    }

    // IPointerClickHandler - fires on tap/click without drag
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[CannonZone] OnPointerClick fired! dragging=" + eventData.dragging + " currentlyDragging=" + (CastleUnitDraggable.CurrentlyDragging != null));
        if (eventData.dragging) return;
        if (CastleUnitDraggable.CurrentlyDragging != null) return;

        // Only open the cannon panel for empty (not yet filled) zones.
        if (HasUnit) return;

        Debug.Log("[CannonZone] Opening cannon panel for this drop zone.");

        // Always route through GameManager so the game state and panel visibility
        // are correctly updated — even if CannonPanelManager.Instance already exists
        // (e.g. the panel was previously opened for another zone and then closed).
        CannonPanelManager.PendingDropZone = this;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenCannonPanel();
        }
        else if (CannonPanelManager.Instance != null)
        {
            // Fallback: no GameManager — open directly.
            CannonPanelManager.PendingDropZone = null;
            CannonPanelManager.Instance.OpenFromDropZone(this);
        }
    }

    // IPointerDownHandler fallback - fires on any press
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[CannonZone] OnPointerDown fired on " + gameObject.name);
    }
    // ── Remove button ─────────────────────────────────────────────

    /// <summary>
    /// Called when the player clicks the RemoveButton child on this cannon zone.
    /// Removes the cannon, unequips the entry, and re-opens the zone so a new
    /// cannon can be bought and dropped here.
    /// </summary>
    private void OnRemoveClicked() => RemoveCannonFromZone();

    /// <summary>
    /// Public version so external code (e.g. CastleTabController) can also trigger
    /// a user-initiated removal of the cannon in this zone.
    /// </summary>
    public void RemoveCannonFromZone()
    {
        if (!HasUnit) return;

        // Destroy the placed prefab
        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

        // Resolve entry: prefer stored reference, fall back to variantId lookup
        // (drag-drop path stores variantId but not the full entry object)
        if (_equippedEntry == null && PlacedVariantId >= 0 && CannonPanelManager.Instance != null)
        {
            foreach (var e in CannonPanelManager.Instance.GetInventory())
                if (e.inventoryId == PlacedVariantId) { _equippedEntry = e; break; }
        }

        // Unequip the inventory entry so the cannon reappears in the buy panel
        if (_equippedEntry != null)
        {
            _equippedEntry.isEquipped = false;
            _equippedEntry.equippedSlot = null;
            CannonPanelManager.Instance?.RefreshAfterUnequip();
            _equippedEntry = null;
        }

        HasUnit = false;
        PlacedVariantId = -1;

        _soldierImage?.SetActive(false);
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        RefreshEmptySlotZone();

        // Re-show the expansion slot so the player can place another cannon
        if (LinkedExpansionSlot != null)
            LinkedExpansionSlot.gameObject.SetActive(true);

        // Re-enable the zone only if the Cannon tab is currently active
        bool inCannonTab = CastleTabController.Instance != null &&
                           CastleTabController.Instance.ActiveTab == CastleTabController.CastleTab.Cannon;
        SetInteractable(inCannonTab);
        RefreshRemoveButton();

        Debug.Log($"[DropZone] RemoveCannonFromZone — zone '{gameObject.name}' is now empty.");

        // Reveal the other zone now that cannon was removed.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>Shows the RemoveButton only when a cannon is placed in this zone.</summary>
    private void RefreshRemoveButton()
    {
        if (_removeButton == null) return;
        bool inCannonScene = GameManager.Instance != null &&
                             GameManager.Instance.CurrentState == GameManager.GameState.Cannon;
        Debug.Log($"[RemoveButton] {gameObject.name} | HasUnit={HasUnit} | GameState={GameManager.Instance?.CurrentState} | inCannonScene={inCannonScene} | setting active={HasUnit && inCannonScene}");
        _removeButton.gameObject.SetActive(HasUnit && inCannonScene);
    }

    /// <summary>
    /// Shows EmptySlotZone only when the zone is empty AND we are in the Cannon scene.
    /// Hidden when occupied (cannon is present) or when in Village/Castle/other scenes.
    /// </summary>
    private void RefreshEmptySlotZone()
    {
        if (_emptySlotZone == null) return;
        bool inCannonTab = CastleTabController.Instance != null &&
                           CastleTabController.Instance.ActiveTab == CastleTabController.CastleTab.Cannon;
        _emptySlotZone.SetActive(!HasUnit && inCannonTab);
    }

}