using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Overlay on an empty cell adjacent to castle blocks.
///
/// TWO interactions:
///   1. Click          → spend coins, place a new castle block here.
///   2. Drag unit here → NO new block. Find the block directly BELOW (_row - 1),
///                       seat the cannon in its CannonZone, and hide THIS slot.
///                       The slot shows again when the cannon is moved or removed.
///
/// FIX 2 — On Init, if the block below already exists and its drop zone has no
/// linked expansion slot yet (e.g. the cannon was placed via direct drop rather
/// than through this slot), this slot pre-registers itself so re-dragging the
/// cannon always has a slot to reveal.
/// </summary>
public class ExpansionSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDropHandler
{
    [Header("Cost")]
    public int blockCost = 100;

    [Header("UI References")]
    public Image borderImage;
    public TextMeshProUGUI costLabel;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.70f);
    public Color hoverColor = new Color(1f, 0.9f, 0.3f, 1.00f);
    public Color hoverUnitColor = new Color(0.3f, 1f, 0.3f, 1.00f);
    public Color cantAffordColor = new Color(1f, 0.3f, 0.3f, 0.70f);

    private int _row, _col;
    private CastleGrid _grid;
    private bool _initialized = false;
    private bool _isProcessing = false;

    // ─────────────────────────────────────────────────────────────
    public void Init(int row, int col, CastleGrid grid)
    {
        _row = row; _col = col; _grid = grid;
        _initialized = true;
        UpdateDisplay();

        // FIX 2 — Pre-link this slot to the drop zone of the block directly
        // below. This covers the case where a cannon was placed on the zone
        // via direct drop (bypassing this slot), leaving LinkedExpansionSlot
        // null and preventing re-drag from ever revealing a slot.
        PreLinkToBlockBelow();
    }

    /// <summary>
    /// If the block below already exists and its cannon drop zone has no linked
    /// expansion slot, register this slot so DetachUnit() can reveal it later.
    /// Only runs when the slot isn't already hidden (i.e. it's currently active).
    /// </summary>
    private void PreLinkToBlockBelow()
    {
        if (_grid == null) return;

        int blockRow = _row - 1;
        GridCell blockCell = _grid.GetCell(blockRow, _col);
        if (blockCell == null || !blockCell.HasBlock) return;

        // Link to every drop zone on the block that has no slot yet.
        // (Typically only the Cannon zone uses expansion slots, but this
        //  handles future soldier-slot expansions too.)
        foreach (CastleUnitDropZone zone in
                 blockCell.GetComponentsInChildren<CastleUnitDropZone>(includeInactive: true))
        {
            if (zone.LinkedExpansionSlot == null)
            {
                zone.LinkedExpansionSlot = this;
                Debug.Log($"[ExpansionSlot] Pre-linked to {zone.acceptedType} zone " +
                          $"on block ({blockRow},{_col}).");
            }
        }
    }

    private void UpdateDisplay()
    {
        if (costLabel != null) costLabel.text = blockCost.ToString("N0");
        RefreshBorderColor();
    }

    private void RefreshBorderColor()
    {
        if (borderImage == null || CurrencyManager.Instance == null) return;
        borderImage.color = CurrencyManager.Instance.Coins >= blockCost
            ? normalColor : cantAffordColor;
    }

    // ── 1. Click → buy + place a NEW block ───────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        if (_isProcessing) return;
        if (!_initialized || _grid == null) return;
        if (CurrencyManager.Instance == null) return;
        if (_grid.castleBlockPrefab == null) return;

        _isProcessing = true;

        if (!CurrencyManager.Instance.SpendCoins(blockCost))
        {
            Debug.Log("[ExpansionSlot] Not enough coins.");
            StartCoroutine(FlashRed());
            _isProcessing = false;
            return;
        }

        Debug.Log($"[ExpansionSlot] Placing block at ({_row},{_col}).");
        _grid.PlaceBlockAt(_row, _col, _grid.castleBlockPrefab);
    }

    // ── 2. Drop unit → seat it on the block BELOW, hide this slot ─

    public void OnDrop(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;

        if (unit == null)
        {
            Debug.Log("[ExpansionSlot] OnDrop — nothing being dragged.");
            return;
        }

        if (_grid == null)
        {
            Debug.LogWarning("[ExpansionSlot] _grid is null — was Init() called?");
            return;
        }

        int blockRow = _row - 1;
        GridCell blockCell = _grid.GetCell(blockRow, _col);

        if (blockCell == null || !blockCell.HasBlock)
        {
            Debug.Log($"[ExpansionSlot] No block at ({blockRow},{_col}). Unit snaps back.");
            return;
        }

        CastleUnitDropZone zone = blockCell.FindDropZoneForType(unit.unitType);

        if (zone == null)
        {
            Debug.Log($"[ExpansionSlot] No {unit.unitType} drop zone on block ({blockRow},{_col}).");
            return;
        }
        if (zone.HasUnit)
        {
            Debug.Log($"[ExpansionSlot] {unit.unitType} zone on block ({blockRow},{_col}) is occupied.");
            return;
        }

        // Link and hide this slot BEFORE placing the unit so DetachUnit()
        // can show it again if the cannon is moved away later.
        zone.LinkedExpansionSlot = this;
        gameObject.SetActive(false);

        zone.PlaceUnit(unit);
        CastleUnitDraggable.NotifyDropSucceeded();

        Debug.Log($"[ExpansionSlot] {unit.unitType} seated on block ({blockRow},{_col}) — slot hidden.");
    }

    // ── Hover ─────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.08f;
        if (borderImage == null) return;
        borderImage.color = CastleUnitDraggable.CurrentlyDragging != null
            ? hoverUnitColor
            : hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
        RefreshBorderColor();
    }

    private IEnumerator FlashRed()
    {
        if (borderImage == null) yield break;
        borderImage.color = Color.red;
        yield return new WaitForSeconds(0.25f);
        RefreshBorderColor();
    }
}