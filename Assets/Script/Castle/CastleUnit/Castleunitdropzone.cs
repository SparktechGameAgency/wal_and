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

        // Cannon zones start non-interactive and invisible — only the cannon
        // section reveals them. We do NOT deactivate the whole GameObject because
        // the cannon prefab placed inside must always remain visible.
        if (acceptedType == CastleUnitType.Cannon && !HasUnit)
            SetInteractable(false);
    }

    /// <summary>
    /// Show or hide every CastleUnitDropZone that accepts Cannons.
    /// Call this when entering/leaving the Cannon panel section.
    /// Does NOT touch the whole GameObject — only the zone overlay visibility
    /// and raycast target, so placed cannon prefabs stay visible at all times.
    /// Zones that are already filled (HasUnit == true) stay non-interactive always.
    /// </summary>
    public static void SetCannonZonesVisible(bool visible)
    {
        foreach (var zone in FindObjectsOfType<CastleUnitDropZone>(includeInactive: false))
        {
            if (zone.acceptedType != CastleUnitType.Cannon) continue;
            // A filled zone is never interactive — cannon is already there.
            if (zone.HasUnit) { zone.SetInteractable(false); continue; }
            zone.SetInteractable(visible);
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

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = normalColor;
        _soldierImage?.SetActive(true);   // soldier shows with cannon

        // Hide the expansion slot so the player can't drop another cannon here.
        if (LinkedExpansionSlot != null)
            LinkedExpansionSlot.gameObject.SetActive(false);

        // Disable interaction on this zone — it's full, nothing to tap anymore.
        // Do NOT deactivate the whole GameObject; the placed cannon must stay visible.
        SetInteractable(false);

        Debug.Log($"[DropZone] PlaceCannonFromPanel — placed '{cannonPrefab.name}' in {gameObject.name}");
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
}