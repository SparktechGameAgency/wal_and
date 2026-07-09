using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCell : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool HasBlock => _block != null;

    /// <summary>The CastleBlock placed on this cell, or null if empty.</summary>
    public CastleBlock Block => _block;

    private CastleBlock _block;
    private GameObject _expansionSlotInstance;
    private GameObject _unitSlotInstance;
    private GameObject _unitSlotPrefab;   // cached so migration can self-initialise
    private CastleGrid _grid;

    public void Init(int row, int col, CastleGrid grid)
    {
        Row = row; Col = col; _grid = grid;
        Image bg = GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0f);
        bg.raycastTarget = false;   // invisible layout cell — must NOT block CannonZone clicks
    }

    // ── Block ─────────────────────────────────────────────────────

    public void PlaceBlock(CastleBlock block)
    {
        _block = block;
        HideExpansionSlot();
        // Migration and slot refresh are handled by CastleGrid.PlaceBlockAt
        // via MigrateUnitsFromBelow() → RefreshUnitSlots(). Do NOT call them here.
    }

    public void ClearBlock()
    {
        _block = null;
        HideUnitSlot();
        _grid.RefreshExpansionSlots();
        _grid.RefreshUnitSlots();
    }

    // ── Expansion slot ────────────────────────────────────────────

    public void ShowExpansionSlot(GameObject slotPrefab)
    {
        if (_expansionSlotInstance != null) return;

        _expansionSlotInstance = Instantiate(slotPrefab, transform);

        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
        if (slot != null) slot.Init(Row, Col, _grid);
    }

    public void HideExpansionSlot()
    {
        if (_expansionSlotInstance != null)
        {
            Destroy(_expansionSlotInstance);
            _expansionSlotInstance = null;
        }
    }

    // ── Unit slot ─────────────────────────────────────────────────

    /// <summary>
    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
    /// Safe to call repeatedly — will no-op if already shown.
    /// </summary>
    public void ShowUnitSlot(GameObject unitSlotPrefab)
    {
        _unitSlotPrefab = unitSlotPrefab;   // cache for use during migration
        if (_unitSlotInstance != null) return;
        if (!HasBlock) return;

        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // CastleBlock and CastleBlockUnitSlot are siblings under this GridCell.
        // Sibling order = draw order on a Screen Space Overlay canvas, so the
        // LAST sibling always renders on top. Previously this was SetAsLastSibling(),
        // which made the cannon/archer always render in FRONT of the castle wall art.
        // Pushing the slot to be the FIRST sibling makes the wall art (the CastleBlock,
        // which stays after it in the hierarchy) render on top instead, so the
        // cannon/archer visually sit inside/behind the block.
        rt.SetAsFirstSibling();
    }

    /// <summary>
    /// Destroys the unit slot overlay and removes any placed units.
    /// Only call this when the block is genuinely removed or covered by another block.
    /// Do NOT call this on panel switches — use SetUnitSlotVillageMode() instead.
    /// </summary>
    public void HideUnitSlot()
    {
        if (_unitSlotInstance != null)
        {
            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
            slot?.RemoveAll();
            Destroy(_unitSlotInstance);
            _unitSlotInstance = null;
        }
    }

    /// <summary>
    /// Switches the cannon zone between village and castle display modes.
    ///
    /// Village mode (isVillage = true):
    ///   • Zone background alpha = 0 — invisible, no clutter.
    ///   • CanvasGroup.blocksRaycasts = TRUE — drops accepted, placed cannon
    ///     can be picked up and moved between blocks.
    ///
    /// Castle mode (isVillage = false):
    ///   • Zone background alpha = normalColor — visible.
    ///   • CanvasGroup.blocksRaycasts = FALSE — ALL pointer/drag events are
    ///     blocked. Cannons cannot be dragged FROM the shop here, and a placed
    ///     cannon cannot be re-dragged. Block-adding via expansion slots is
    ///     unaffected (those are separate GameObjects outside this slot).
    /// </summary>
    public void SetUnitSlotVillageMode(bool isVillage)
    {
        if (_unitSlotInstance == null) return;

        // CanvasGroup on the unit-slot must ALWAYS have blocksRaycasts = TRUE.
        //
        // Previously, Castle panel mode set blocksRaycasts = false to prevent
        // cannon drag-drop, but that also blocked Button (CannonZone) click events.
        //
        // Drag-drop is now disabled at the CastleUnitDraggable level
        // (dragEnabled = false in Castle mode) so blocksRaycasts stays true
        // and the CannonZone Button remains clickable in both modes.
        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;   // ALWAYS true — CannonZone Button must receive clicks

        // Toggle drag-drop on any CastleUnitDraggable children.
        // Village  → drag enabled.   Castle → drag disabled.
        foreach (var draggable in _unitSlotInstance.GetComponentsInChildren<CastleUnitDraggable>(true))
            draggable.dragEnabled = isVillage;

        // Apply zone-level alpha: transparent in village, visible in castle.
        CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
        slot?.SetVillageMode(isVillage);
    }

    /// <summary>
    /// Returns the CastleBlockUnitSlot component on this cell's unit slot instance.
    /// Used by CastleGrid to migrate units before a block covers this cell.
    /// </summary>
    public CastleBlockUnitSlot GetUnitSlot() =>
        _unitSlotInstance != null ? _unitSlotInstance.GetComponent<CastleBlockUnitSlot>() : null;

    /// <summary>
    /// Reparents every unit sitting inside each CastleUnitDropZone on this cell
    /// into the matching drop zone on <paramref name="target"/>.
    /// Searches the entire cell hierarchy — works even if _unitSlotInstance was
    /// lost on a panel reload. The unit GameObjects are never destroyed.
    /// Call this before HideUnitSlotEmpty() so the cannons are safely moved first.
    /// </summary>
    public void TransferUnitSlotTo(GridCell target)
    {
        if (target == null) return;

        // ── Migrate cannons ───────────────────────────────────────
        foreach (var srcZone in GetComponentsInChildren<CastleUnitDropZone>(true))
        {
            if (!srcZone.HasUnit) continue;

            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
            if (destZone == null || destZone.HasUnit) continue;

            srcZone.MigrateUnitTo(destZone);
        }

        // ── Migrate archers ───────────────────────────────────────
        foreach (var srcArcher in GetComponentsInChildren<ArcherZoneCastle>(true))
        {
            if (!srcArcher.IsOccupied) continue;

            ArcherZoneCastle destArcher = target.FindArcherZone();
            if (destArcher == null || destArcher.IsOccupied) continue;

            srcArcher.MigrateArcherTo(destArcher);
        }
    }

    /// <summary>
    /// Destroys the unit slot container WITHOUT calling RemoveAll().
    /// Only use after TransferUnitSlotTo() has already moved all units out.
    /// </summary>
    public void HideUnitSlotEmpty()
    {
        if (_unitSlotInstance != null)
        {
            Destroy(_unitSlotInstance);
            _unitSlotInstance = null;
        }
    }

    /// <summary>
    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
    /// unit on the block below without placing a new block.
    /// Returns null if no unit slot exists or no matching zone found.
    /// </summary>
    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
    {
        if (_unitSlotInstance == null) return null;

        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
        {
            if (zone.acceptedType == unitType)
                return zone;
        }
        return null;
    }

    /// <summary>
    /// Returns the ArcherZoneCastle on this cell's unit slot.
    /// Used by TransferUnitSlotTo to migrate a stationed archer on expansion.
    /// </summary>
    public ArcherZoneCastle FindArcherZone()
    {
        if (_unitSlotInstance == null) return null;
        return _unitSlotInstance.GetComponentInChildren<ArcherZoneCastle>(true);
    }
}