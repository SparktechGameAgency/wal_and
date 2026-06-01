////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// Drop zone for a Cannon unit on an exposed castle block.
///////
/////// When a cannon is placed via an ExpansionSlot, that slot registers itself
/////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
/////// hides / shows that slot as the cannon moves:
///////
///////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
///////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
///////   DetachUnit   → cannon dragged away; soldier stays visible; linked slot shown.
///////   ReattachUnit → failed drag, cannon snaps back; linked slot hidden.
///////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
///////   MigrateUnitTo → cannon reparented to another zone; state transferred cleanly.
///////
/////// Child hierarchy (auto-wired by name in Awake):
///////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////   ├── EmptyVisual     hint shown only during a valid drag hover
///////   ├── Highlight       glow frame shown during any drag hover
///////   └── Soldier         Image — hidden by default, shown when cannon is placed,
///////                               stays visible while cannon is being dragged
/////// </summary>
////[RequireComponent(typeof(Image))]
////public class CastleUnitDropZone : MonoBehaviour,
////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────
////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////    public CastleUnitType acceptedType;

////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////    // ── Auto-wired ────────────────────────────────────────────────
////    private Image _bg;
////    private GameObject _emptyVisual;
////    private GameObject _highlight;
////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

////    // ── State ─────────────────────────────────────────────────────
////    public bool HasUnit { get; private set; }
////    public int PlacedVariantId { get; private set; } = -1;

////    /// <summary>
////    /// The ExpansionSlot that placed the cannon here.
////    /// Kept even while the cannon is being dragged so snap-back can hide
////    /// the slot again. Cleared only on RemoveUnit or when a new slot links.
////    /// </summary>
////    public ExpansionSlot LinkedExpansionSlot { get; set; }

////    private GameObject _placedInstance;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _bg = GetComponent<Image>();
////        _bg.color = normalColor;
////        _bg.raycastTarget = true;

////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////        _highlight = transform.Find("Highlight")?.gameObject;
////        _soldierImage = transform.Find("Soldier")?.gameObject;

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _soldierImage?.SetActive(false);

////        // Soldier is a pure visual — must NOT block raycasts on the cannon above it
////        if (_soldierImage != null)
////        {
////            var img = _soldierImage.GetComponent<Image>();
////            if (img != null) img.raycastTarget = false;
////        }
////    }

////    // ── Standard drag-drop path ───────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var unit = CastleUnitDraggable.CurrentlyDragging;

////        if (unit == null) { Debug.Log("[DropZone] OnDrop — nothing dragged."); return; }
////        if (unit.unitType != acceptedType) { Debug.Log($"[DropZone] Type mismatch."); return; }
////        if (HasUnit) { Debug.Log($"[DropZone] Already occupied."); return; }

////        PlaceUnit(unit);
////        CastleUnitDraggable.NotifyDropSucceeded();
////    }

////    // ── Public API ────────────────────────────────────────────────

////    /// <summary>
////    /// Seats the cannon, shows the soldier.
////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
////    /// </summary>
////    public void PlaceUnit(CastleUnitDraggable unit)
////    {
////        if (unit == null || HasUnit) return;

////        unit.transform.SetParent(transform, worldPositionStays: false);

////        RectTransform rt = unit.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            if (unit.stretchToFillSlot)
////            {
////                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////                rt.anchoredPosition = Vector2.zero;
////            }
////            else
////            {
////                rt.anchorMin = new Vector2(0f, 0.5f);
////                rt.anchorMax = new Vector2(0f, 0.5f);
////                rt.pivot = new Vector2(0f, 0.5f);
////                rt.sizeDelta = unit.placedSize;
////                rt.anchoredPosition = Vector2.zero;
////            }
////            rt.SetAsLastSibling();
////        }

////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////        // Ensure cannon can be dragged again after placement
////        Image unitImg = unit.GetComponent<Image>();
////        if (unitImg != null) unitImg.raycastTarget = true;

////        _placedInstance = unit.gameObject;
////        HasUnit = true;
////        PlacedVariantId = unit.variantId;

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;
////        _soldierImage?.SetActive(true);   // ← soldier appears with cannon
////    }

////    /// <summary>
////    /// Called by CastleUnitDraggable.OnBeginDrag — cannon is being lifted.
////    /// Zone is freed so it can accept a drop. Soldier STAYS VISIBLE because
////    /// the soldier belongs to the block, not the cannon.
////    /// LinkedExpansionSlot is shown but NOT cleared (snap-back needs it).
////    /// </summary>
////    public void DetachUnit()
////    {
////        _placedInstance = null;
////        HasUnit = false;
////        PlacedVariantId = -1;

////        // ── Soldier intentionally NOT hidden ─────────────────────
////        // The soldier image represents a unit on the block itself.
////        // It stays visible while the cannon is in the air and disappears
////        // only when the cannon is permanently removed (RemoveUnit).

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;

////        // Show the expansion slot — player can drop here again.
////        // Keep the reference so ReattachUnit can hide it on snap-back.
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(true);
////            Debug.Log("[DropZone] Detached — expansion slot restored (ref kept for snap-back).");
////        }
////    }

////    /// <summary>
////    /// Called by CastleUnitDraggable.OnEndDrag on a failed drop — cannon snaps back.
////    /// Soldier was already visible (DetachUnit didn't hide it), so nothing to restore.
////    /// Hides the expansion slot again because the cannon is back.
////    /// </summary>
////    public void ReattachUnit(CastleUnitDraggable unit)
////    {
////        if (unit == null) return;

////        _placedInstance = unit.gameObject;
////        HasUnit = true;
////        PlacedVariantId = unit.variantId;

////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;
////        // Soldier already visible from PlaceUnit — nothing to change.

////        // Cannon is back → hide the expansion slot again.
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(false);
////            Debug.Log("[DropZone] Reattached — expansion slot hidden again.");
////        }
////    }

////    /// <summary>
////    /// Cannon permanently destroyed (block removed, etc.).
////    /// Hides the soldier and restores the expansion slot.
////    /// </summary>
////    public void RemoveUnit()
////    {
////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

////        HasUnit = false;
////        PlacedVariantId = -1;

////        _soldierImage?.SetActive(false);   // ← soldier hidden only on permanent removal
////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;

////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(true);
////            LinkedExpansionSlot = null;
////            Debug.Log("[DropZone] Removed — expansion slot restored.");
////        }
////    }

////    /// <summary>
////    /// Reparents the cannon sitting in THIS zone into <paramref name="destination"/>
////    /// without destroying it. Used by GridCell.TransferUnitSlotTo() when a new block
////    /// covers this cell and the cannon must move up to the newly exposed cell above.
////    ///
////    /// Safety checks:
////    ///   • Does nothing if this zone is empty.
////    ///   • Does nothing if destination is null or already occupied.
////    /// The cannon GameObject is never destroyed — only reparented.
////    /// </summary>
////    public void MigrateUnitTo(CastleUnitDropZone destination)
////    {
////        if (destination == null || !HasUnit || destination.HasUnit) return;
////        if (_placedInstance == null) return;

////        // ── Move the cannon GO into the destination zone ──────────
////        _placedInstance.transform.SetParent(destination.transform, worldPositionStays: false);

////        // Re-centre inside the new zone
////        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            rt.anchorMin = new Vector2(0f, 0.5f);
////            rt.anchorMax = new Vector2(0f, 0.5f);
////            rt.pivot = new Vector2(0f, 0.5f);
////            var draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
////            rt.sizeDelta = draggable != null ? draggable.placedSize : destination.centeredUnitSize;
////            rt.anchoredPosition = Vector2.zero;
////            rt.SetAsLastSibling();
////        }

////        // ── Hand ownership to the destination ────────────────────
////        destination._placedInstance = _placedInstance;
////        destination.HasUnit = true;
////        destination.PlacedVariantId = PlacedVariantId;

////        // Destination visuals: soldier on, hover hints off
////        destination._emptyVisual?.SetActive(false);
////        destination._highlight?.SetActive(false);
////        destination._bg.color = destination.normalColor;
////        destination._soldierImage?.SetActive(true);

////        // ── Clear this zone (no Destroy, no RemoveUnit) ───────────
////        _placedInstance = null;
////        HasUnit = false;
////        PlacedVariantId = -1;

////        _soldierImage?.SetActive(false);
////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;

////        // Restore linked expansion slot on the source (now exposed again).
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(true);
////            LinkedExpansionSlot = null;
////        }

////        Debug.Log($"[DropZone] MigrateUnitTo — cannon moved from '{transform.parent?.name}' " +
////                  $"to '{destination.transform.parent?.name}'.");
////    }

////    /// <summary>
////    /// Switches the zone background between Village and Castle display modes.
////    /// Called by CastleBlockUnitSlot.SetVillageMode() → GridCell.SetUnitSlotVillageMode().
////    ///
////    /// Village mode (isVillage = true):
////    ///   Zone background alpha → 0 (invisible). Raycasts are controlled at the
////    ///   GridCell level via CanvasGroup, not here — so drag-drop still works.
////    ///   On hover, the zone briefly shows hoverValidColor as visual feedback.
////    ///
////    /// Castle mode (isVillage = false):
////    ///   Zone background is restored to normalColor (slightly tinted / visible).
////    /// </summary>
////    public void SetVillageMode(bool isVillage)
////    {
////        if (_bg == null) return;

////        if (isVillage)
////        {
////            // Transparent background; hover colors still apply during drags.
////            Color c = normalColor;
////            c.a = 0f;
////            _bg.color = c;
////        }
////        else
////        {
////            // Restore the standard tinted background for Castle Panel.
////            _bg.color = normalColor;
////        }
////    }

////    // ── Hover ─────────────────────────────────────────────────────

////    public void OnPointerEnter(PointerEventData eventData)
////    {
////        var unit = CastleUnitDraggable.CurrentlyDragging;
////        if (unit == null) return;

////        bool valid = unit.unitType == acceptedType && !HasUnit;
////        _highlight?.SetActive(true);
////        _emptyVisual?.SetActive(valid);
////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////    }

////    public void OnPointerExit(PointerEventData eventData)
////    {
////        _highlight?.SetActive(false);
////        _emptyVisual?.SetActive(false);
////        _bg.color = normalColor;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Drop zone for a Cannon unit on an exposed castle block.
/////
///// Soldier visibility rules:
/////   PlaceUnit    → cannon arrives    → soldier SHOWN,  expansion slot hidden.
/////   DetachUnit   → cannon dragged    → soldier HIDDEN, expansion slot shown.
/////   ReattachUnit → cannon snaps back → soldier SHOWN,  expansion slot hidden.
/////   RemoveUnit   → cannon destroyed  → soldier HIDDEN, expansion slot shown.
/////   MigrateUnitTo → cannon reparented → source soldier HIDDEN, dest soldier SHOWN.
/////
///// Child hierarchy (auto-wired by name in Awake):
/////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////   ├── EmptyVisual     hint shown only during a valid drag hover
/////   ├── Highlight       glow frame shown during any drag hover
/////   └── Soldier         Image — hidden by default, shown when cannon is placed
///// </summary>
//[RequireComponent(typeof(Image))]
//public class CastleUnitDropZone : MonoBehaviour,
//    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────
//    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//    public CastleUnitType acceptedType;

//    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//    // ── Auto-wired ────────────────────────────────────────────────
//    private Image _bg;
//    private GameObject _emptyVisual;
//    private GameObject _highlight;
//    private GameObject _soldierImage;

//    // ── State ─────────────────────────────────────────────────────
//    public bool HasUnit { get; private set; }
//    public int PlacedVariantId { get; private set; } = -1;

//    /// <summary>
//    /// The ExpansionSlot above this block that should be shown/hidden
//    /// as the cannon moves. Auto-linked on drop if not already set.
//    /// </summary>
//    public ExpansionSlot LinkedExpansionSlot { get; set; }

//    private GameObject _placedInstance;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _bg = GetComponent<Image>();
//        _bg.color = normalColor;
//        _bg.raycastTarget = true;

//        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//        _highlight = transform.Find("Highlight")?.gameObject;
//        _soldierImage = transform.Find("Soldier")?.gameObject;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _soldierImage?.SetActive(false);

//        // Soldier must NOT block raycasts on the cannon above it
//        if (_soldierImage != null)
//        {
//            var img = _soldierImage.GetComponent<Image>();
//            if (img != null) img.raycastTarget = false;
//        }
//    }

//    private void Start()
//    {
//        // Force-hide soldier at runtime in case the prefab has it active by default.
//        // PlaceUnit() will show it again when a cannon is placed.
//        if (!HasUnit)
//            _soldierImage?.SetActive(false);
//    }

//    // ── Standard drag-drop path ───────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;

//        if (unit == null) { Debug.Log("[DropZone] OnDrop — nothing dragged."); return; }
//        if (unit.unitType != acceptedType) { Debug.Log("[DropZone] Type mismatch."); return; }
//        if (HasUnit) { Debug.Log("[DropZone] Already occupied."); return; }

//        // Auto-link the expansion slot above this block if not linked yet.
//        // This handles the case where the cannon was dropped directly onto the
//        // zone (bypassing ExpansionSlot.OnDrop) so LinkedExpansionSlot is null.
//        AutoLinkExpansionSlot();

//        PlaceUnit(unit);
//        CastleUnitDraggable.NotifyDropSucceeded();
//    }

//    // ── Auto-link expansion slot ──────────────────────────────────

//    /// <summary>
//    /// Searches the CastleGrid for the ExpansionSlot that sits directly above
//    /// this block cell and links it so DetachUnit/RemoveUnit can show it later.
//    /// Only runs if LinkedExpansionSlot is not already set.
//    /// </summary>
//    private void AutoLinkExpansionSlot()
//    {
//        if (LinkedExpansionSlot != null) return;

//        CastleGrid grid = CastleGrid.Instance;
//        if (grid == null) return;

//        // Find which GridCell this drop zone belongs to by walking up
//        GridCell myCell = GetComponentInParent<GridCell>();
//        if (myCell == null) return;

//        int row = myCell.Row;
//        int col = myCell.Col;

//        // The expansion slot that places onto this block sits one row above
//        GridCell cellAbove = grid.GetCell(row + 1, col);
//        if (cellAbove == null) return;

//        // Find the ExpansionSlot on that cell
//        ExpansionSlot slot = cellAbove.GetComponentInChildren<ExpansionSlot>(includeInactive: true);
//        if (slot == null) return;

//        LinkedExpansionSlot = slot;
//        Debug.Log($"[DropZone] Auto-linked ExpansionSlot at ({row + 1},{col}).");
//    }

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>
//    /// Seats the cannon and shows the soldier.
//    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//    /// </summary>
//    public void PlaceUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null || HasUnit) return;

//        unit.transform.SetParent(transform, worldPositionStays: false);

//        RectTransform rt = unit.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            if (unit.stretchToFillSlot)
//            {
//                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            else
//            {
//                rt.anchorMin = new Vector2(0f, 0.5f);
//                rt.anchorMax = new Vector2(0f, 0.5f);
//                rt.pivot = new Vector2(0f, 0.5f);
//                rt.sizeDelta = unit.placedSize;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            rt.SetAsLastSibling();
//        }

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        Image unitImg = unit.GetComponent<Image>();
//        if (unitImg != null) unitImg.raycastTarget = true;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;
//        _soldierImage?.SetActive(true);    // ← soldier appears with cannon

//        Debug.Log($"[DropZone] PlaceUnit — soldier shown. LinkedSlot={(LinkedExpansionSlot != null ? LinkedExpansionSlot.gameObject.name : "none")}");
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnBeginDrag — cannon is being lifted.
//    /// Soldier hides. Expansion slot shows so player can drop here again.
//    /// </summary>
//    public void DetachUnit()
//    {
//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);   // ← soldier hides when cannon lifts
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            Debug.Log("[DropZone] DetachUnit — soldier hidden, expansion slot shown.");
//        }
//        else
//        {
//            // No expansion slot linked — try to auto-find and show it
//            Debug.LogWarning("[DropZone] DetachUnit — LinkedExpansionSlot is null! " +
//                             "Trying to auto-find...");
//            AutoLinkExpansionSlot();
//            if (LinkedExpansionSlot != null)
//                LinkedExpansionSlot.gameObject.SetActive(true);
//        }
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnEndDrag on a failed drop — cannon snaps back.
//    /// Soldier shows again. Expansion slot hides.
//    /// </summary>
//    public void ReattachUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null) return;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;
//        _soldierImage?.SetActive(true);    // ← soldier shows when cannon snaps back

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(false);
//            Debug.Log("[DropZone] ReattachUnit — soldier shown, expansion slot hidden.");
//        }
//    }

//    /// <summary>
//    /// Cannon permanently removed (block removed, etc.).
//    /// Hides the soldier and restores the expansion slot.
//    /// </summary>
//    public void RemoveUnit()
//    {
//        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);   // ← soldier hides
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            LinkedExpansionSlot = null;
//            Debug.Log("[DropZone] RemoveUnit — soldier hidden, expansion slot shown.");
//        }
//        else
//        {
//            Debug.LogWarning("[DropZone] RemoveUnit — LinkedExpansionSlot is null. " +
//                             "Expansion slot cannot be shown automatically.");
//        }
//    }

//    /// <summary>
//    /// Reparents the cannon from this zone into destination.
//    /// Source soldier hides, destination soldier shows.
//    /// </summary>
//    public void MigrateUnitTo(CastleUnitDropZone destination)
//    {
//        if (destination == null || !HasUnit || destination.HasUnit) return;
//        if (_placedInstance == null) return;

//        // Move cannon into destination
//        _placedInstance.transform.SetParent(destination.transform, worldPositionStays: false);

//        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            var draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
//            rt.anchorMin = new Vector2(0f, 0.5f);
//            rt.anchorMax = new Vector2(0f, 0.5f);
//            rt.pivot = new Vector2(0f, 0.5f);
//            rt.sizeDelta = draggable != null ? draggable.placedSize : destination.centeredUnitSize;
//            rt.anchoredPosition = Vector2.zero;
//            rt.SetAsLastSibling();
//        }

//        // Hand ownership to destination
//        destination._placedInstance = _placedInstance;
//        destination.HasUnit = true;
//        destination.PlacedVariantId = PlacedVariantId;

//        destination._emptyVisual?.SetActive(false);
//        destination._highlight?.SetActive(false);
//        destination._bg.color = destination.normalColor;
//        destination._soldierImage?.SetActive(true);    // ← dest soldier shown

//        // Clear this zone
//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);               // ← source soldier hides
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        // Restore expansion slot on source
//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            LinkedExpansionSlot = null;
//        }

//        Debug.Log($"[DropZone] MigrateUnitTo — cannon moved from '{transform.parent?.name}' " +
//                  $"to '{destination.transform.parent?.name}'.");
//    }

//    /// <summary>
//    /// Switches the zone background between Village and Castle display modes.
//    /// </summary>
//    public void SetVillageMode(bool isVillage)
//    {
//        if (_bg == null) return;
//        Color c = isVillage ? new Color(normalColor.r, normalColor.g, normalColor.b, 0f)
//                            : normalColor;
//        _bg.color = c;
//    }

//    // ── Hover ─────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;
//        if (unit == null) return;

//        bool valid = unit.unitType == acceptedType && !HasUnit;
//        _highlight?.SetActive(true);
//        _emptyVisual?.SetActive(valid);
//        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);
//        _emptyVisual?.SetActive(false);
//        _bg.color = normalColor;
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Drop zone for a Cannon unit on an exposed castle block.
/////
///// When a cannon is placed via an ExpansionSlot, that slot registers itself
///// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
///// hides / shows that slot as the cannon moves:
/////
/////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
/////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
/////   DetachUnit   → cannon dragged away; soldier stays visible; linked slot shown.
/////   ReattachUnit → failed drag, cannon snaps back; linked slot hidden.
/////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
/////   MigrateUnitTo → cannon reparented to another zone; state transferred cleanly.
/////
///// Child hierarchy (auto-wired by name in Awake):
/////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////   ├── EmptyVisual     hint shown only during a valid drag hover
/////   ├── Highlight       glow frame shown during any drag hover
/////   └── Soldier         Image — hidden by default, shown when cannon is placed,
/////                               stays visible while cannon is being dragged
///// </summary>
//[RequireComponent(typeof(Image))]
//public class CastleUnitDropZone : MonoBehaviour,
//    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────
//    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//    public CastleUnitType acceptedType;

//    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//    // ── Auto-wired ────────────────────────────────────────────────
//    private Image _bg;
//    private GameObject _emptyVisual;
//    private GameObject _highlight;
//    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

//    // ── State ─────────────────────────────────────────────────────
//    public bool HasUnit { get; private set; }
//    public int PlacedVariantId { get; private set; } = -1;

//    /// <summary>
//    /// The ExpansionSlot that placed the cannon here.
//    /// Kept even while the cannon is being dragged so snap-back can hide
//    /// the slot again. Cleared only on RemoveUnit or when a new slot links.
//    /// </summary>
//    public ExpansionSlot LinkedExpansionSlot { get; set; }

//    private GameObject _placedInstance;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _bg = GetComponent<Image>();
//        _bg.color = normalColor;
//        _bg.raycastTarget = true;

//        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//        _highlight = transform.Find("Highlight")?.gameObject;
//        _soldierImage = transform.Find("Soldier")?.gameObject;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _soldierImage?.SetActive(false);

//        // Soldier is a pure visual — must NOT block raycasts on the cannon above it
//        if (_soldierImage != null)
//        {
//            var img = _soldierImage.GetComponent<Image>();
//            if (img != null) img.raycastTarget = false;
//        }
//    }

//    // ── Standard drag-drop path ───────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;

//        if (unit == null) { Debug.Log("[DropZone] OnDrop — nothing dragged."); return; }
//        if (unit.unitType != acceptedType) { Debug.Log($"[DropZone] Type mismatch."); return; }
//        if (HasUnit) { Debug.Log($"[DropZone] Already occupied."); return; }

//        PlaceUnit(unit);
//        CastleUnitDraggable.NotifyDropSucceeded();
//    }

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>
//    /// Seats the cannon, shows the soldier.
//    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//    /// </summary>
//    public void PlaceUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null || HasUnit) return;

//        unit.transform.SetParent(transform, worldPositionStays: false);

//        RectTransform rt = unit.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            if (unit.stretchToFillSlot)
//            {
//                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            else
//            {
//                rt.anchorMin = new Vector2(0f, 0.5f);
//                rt.anchorMax = new Vector2(0f, 0.5f);
//                rt.pivot = new Vector2(0f, 0.5f);
//                rt.sizeDelta = unit.placedSize;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            rt.SetAsLastSibling();
//        }

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        // Ensure cannon can be dragged again after placement
//        Image unitImg = unit.GetComponent<Image>();
//        if (unitImg != null) unitImg.raycastTarget = true;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;
//        _soldierImage?.SetActive(true);   // ← soldier appears with cannon
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnBeginDrag — cannon is being lifted.
//    /// Zone is freed so it can accept a drop. Soldier STAYS VISIBLE because
//    /// the soldier belongs to the block, not the cannon.
//    /// LinkedExpansionSlot is shown but NOT cleared (snap-back needs it).
//    /// </summary>
//    public void DetachUnit()
//    {
//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;

//        // ── Soldier intentionally NOT hidden ─────────────────────
//        // The soldier image represents a unit on the block itself.
//        // It stays visible while the cannon is in the air and disappears
//        // only when the cannon is permanently removed (RemoveUnit).

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        // Show the expansion slot — player can drop here again.
//        // Keep the reference so ReattachUnit can hide it on snap-back.
//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            Debug.Log("[DropZone] Detached — expansion slot restored (ref kept for snap-back).");
//        }
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnEndDrag on a failed drop — cannon snaps back.
//    /// Soldier was already visible (DetachUnit didn't hide it), so nothing to restore.
//    /// Hides the expansion slot again because the cannon is back.
//    /// </summary>
//    public void ReattachUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null) return;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;
//        // Soldier already visible from PlaceUnit — nothing to change.

//        // Cannon is back → hide the expansion slot again.
//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(false);
//            Debug.Log("[DropZone] Reattached — expansion slot hidden again.");
//        }
//    }

//    /// <summary>
//    /// Cannon permanently destroyed (block removed, etc.).
//    /// Hides the soldier and restores the expansion slot.
//    /// </summary>
//    public void RemoveUnit()
//    {
//        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);   // ← soldier hidden only on permanent removal
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            LinkedExpansionSlot = null;
//            Debug.Log("[DropZone] Removed — expansion slot restored.");
//        }
//    }

//    /// <summary>
//    /// Reparents the cannon sitting in THIS zone into <paramref name="destination"/>
//    /// without destroying it. Used by GridCell.TransferUnitSlotTo() when a new block
//    /// covers this cell and the cannon must move up to the newly exposed cell above.
//    ///
//    /// Safety checks:
//    ///   • Does nothing if this zone is empty.
//    ///   • Does nothing if destination is null or already occupied.
//    /// The cannon GameObject is never destroyed — only reparented.
//    /// </summary>
//    public void MigrateUnitTo(CastleUnitDropZone destination)
//    {
//        if (destination == null || !HasUnit || destination.HasUnit) return;
//        if (_placedInstance == null) return;

//        // ── Move the cannon GO into the destination zone ──────────
//        _placedInstance.transform.SetParent(destination.transform, worldPositionStays: false);

//        // Re-centre inside the new zone
//        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchorMin = new Vector2(0f, 0.5f);
//            rt.anchorMax = new Vector2(0f, 0.5f);
//            rt.pivot = new Vector2(0f, 0.5f);
//            var draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
//            rt.sizeDelta = draggable != null ? draggable.placedSize : destination.centeredUnitSize;
//            rt.anchoredPosition = Vector2.zero;
//            rt.SetAsLastSibling();
//        }

//        // ── Hand ownership to the destination ────────────────────
//        destination._placedInstance = _placedInstance;
//        destination.HasUnit = true;
//        destination.PlacedVariantId = PlacedVariantId;

//        // Destination visuals: soldier on, hover hints off
//        destination._emptyVisual?.SetActive(false);
//        destination._highlight?.SetActive(false);
//        destination._bg.color = destination.normalColor;
//        destination._soldierImage?.SetActive(true);

//        // ── Clear this zone (no Destroy, no RemoveUnit) ───────────
//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = normalColor;

//        // Restore linked expansion slot on the source (now exposed again).
//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            LinkedExpansionSlot = null;
//        }

//        Debug.Log($"[DropZone] MigrateUnitTo — cannon moved from '{transform.parent?.name}' " +
//                  $"to '{destination.transform.parent?.name}'.");
//    }

//    /// <summary>
//    /// Switches the zone background between Village and Castle display modes.
//    /// Called by CastleBlockUnitSlot.SetVillageMode() → GridCell.SetUnitSlotVillageMode().
//    ///
//    /// Village mode (isVillage = true):
//    ///   Zone background alpha → 0 (invisible). Raycasts are controlled at the
//    ///   GridCell level via CanvasGroup, not here — so drag-drop still works.
//    ///   On hover, the zone briefly shows hoverValidColor as visual feedback.
//    ///
//    /// Castle mode (isVillage = false):
//    ///   Zone background is restored to normalColor (slightly tinted / visible).
//    /// </summary>
//    public void SetVillageMode(bool isVillage)
//    {
//        if (_bg == null) return;

//        if (isVillage)
//        {
//            // Transparent background; hover colors still apply during drags.
//            Color c = normalColor;
//            c.a = 0f;
//            _bg.color = c;
//        }
//        else
//        {
//            // Restore the standard tinted background for Castle Panel.
//            _bg.color = normalColor;
//        }
//    }

//    // ── Hover ─────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;
//        if (unit == null) return;

//        bool valid = unit.unitType == acceptedType && !HasUnit;
//        _highlight?.SetActive(true);
//        _emptyVisual?.SetActive(valid);
//        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);
//        _emptyVisual?.SetActive(false);
//        _bg.color = normalColor;
//    }
//}

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

    // ── State ─────────────────────────────────────────────────────
    public bool HasUnit { get; private set; }
    public int PlacedVariantId { get; private set; } = -1;

    /// <summary>
    /// The ExpansionSlot above this block that should be shown/hidden
    /// as the cannon moves. Auto-linked on drop if not already set.
    /// </summary>
    public ExpansionSlot LinkedExpansionSlot { get; set; }

    private GameObject _placedInstance;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _bg = GetComponent<Image>();
        _bg.color = normalColor;
        _bg.raycastTarget = true;
        Debug.Log("[CannonZone] Awake on " + gameObject.name + " parent=" + (transform.parent ? transform.parent.name : "none"));

        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
        _highlight = transform.Find("Highlight")?.gameObject;
        _soldierImage = transform.Find("Soldier")?.gameObject;

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _soldierImage?.SetActive(false);

        // Soldier must NOT block raycasts on the cannon above it
        if (_soldierImage != null)
        {
            var img = _soldierImage.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
    }

    private void Start()
    {
        // Force-hide soldier at runtime in case the prefab has it active by default.
        // PlaceUnit() will show it again when a cannon is placed.
        if (!HasUnit)
            _soldierImage?.SetActive(false);
    }

    // ── Standard drag-drop path ───────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;

        if (unit == null) { Debug.Log("[DropZone] OnDrop — nothing dragged."); return; }
        if (unit.unitType != acceptedType) { Debug.Log("[DropZone] Type mismatch."); return; }
        if (HasUnit) { Debug.Log("[DropZone] Already occupied."); return; }

        // Auto-link the expansion slot above this block if not linked yet.
        // This handles the case where the cannon was dropped directly onto the
        // zone (bypassing ExpansionSlot.OnDrop) so LinkedExpansionSlot is null.
        AutoLinkExpansionSlot();

        PlaceUnit(unit);
        CastleUnitDraggable.NotifyDropSucceeded();
    }

    // ── Auto-link expansion slot ──────────────────────────────────

    /// <summary>
    /// Searches the CastleGrid for the ExpansionSlot that sits directly above
    /// this block cell and links it so DetachUnit/RemoveUnit can show it later.
    /// Only runs if LinkedExpansionSlot is not already set.
    /// </summary>
    private void AutoLinkExpansionSlot()
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

        Debug.Log($"[DropZone] PlaceUnit — soldier shown. LinkedSlot={(LinkedExpansionSlot != null ? LinkedExpansionSlot.gameObject.name : "none")}");
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

        _soldierImage?.SetActive(false);   // ← soldier hides when cannon lifts
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;

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
    }

    /// <summary>
    /// Called by CastleUnitDraggable.OnEndDrag on a failed drop — cannon snaps back.
    /// Soldier shows again. Expansion slot hides.
    /// </summary>
    public void ReattachUnit(CastleUnitDraggable unit)
    {
        if (unit == null) return;

        _placedInstance = unit.gameObject;
        HasUnit = true;
        PlacedVariantId = unit.variantId;

        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _soldierImage?.SetActive(true);    // ← soldier shows when cannon snaps back

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(false);
            Debug.Log("[DropZone] ReattachUnit — soldier shown, expansion slot hidden.");
        }
    }

    /// <summary>
    /// Cannon permanently removed (block removed, etc.).
    /// Hides the soldier and restores the expansion slot.
    /// </summary>
    public void RemoveUnit()
    {
        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

        HasUnit = false;
        PlacedVariantId = -1;

        _soldierImage?.SetActive(false);   // ← soldier hides
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;

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

        // Clear this zone
        _placedInstance = null;
        HasUnit = false;
        PlacedVariantId = -1;

        _soldierImage?.SetActive(false);               // ← source soldier hides
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;

        // Restore expansion slot on source
        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            LinkedExpansionSlot = null;
        }

        Debug.Log($"[DropZone] MigrateUnitTo — cannon moved from '{transform.parent?.name}' " +
                  $"to '{destination.transform.parent?.name}'.");
    }

    /// <summary>
    /// Switches the zone background between Village and Castle display modes.
    /// </summary>
    public void SetVillageMode(bool isVillage)
    {
        if (_bg == null) return;
        Color c = isVillage ? new Color(normalColor.r, normalColor.g, normalColor.b, 0f)
                            : normalColor;
        _bg.color = c;
    }

    // ── Hover ─────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;
        if (unit == null) return;

        bool valid = unit.unitType == acceptedType && !HasUnit;
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
        Debug.Log("[CannonZone] Opening cannon panel. GameManager=" + (GameManager.Instance != null ? "OK" : "NULL"));
        GameManager.Instance?.OpenCannonPanel();
    }

    // IPointerDownHandler fallback - fires on any press
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[CannonZone] OnPointerDown fired on " + gameObject.name);
    }
}