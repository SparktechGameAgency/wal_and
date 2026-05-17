
//////using UnityEngine;
//////using UnityEngine.UI;

//////[RequireComponent(typeof(Image))]
//////public class GridCell : MonoBehaviour
//////{
//////    public int Row { get; private set; }
//////    public int Col { get; private set; }
//////    public bool HasBlock => _block != null;

//////    private CastleBlock _block;
//////    private GameObject _expansionSlotInstance;
//////    private CastleGrid _grid;

//////    public void Init(int row, int col, CastleGrid grid)
//////    {
//////        Row = row;
//////        Col = col;
//////        _grid = grid;

//////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
//////    }

//////    public void PlaceBlock(CastleBlock block)
//////    {
//////        _block = block;
//////        HideExpansionSlot();
//////    }

//////    public void ClearBlock()
//////    {
//////        _block = null;
//////        _grid.RefreshExpansionSlots();
//////    }

//////    public void ShowExpansionSlot(GameObject slotPrefab)
//////    {
//////        if (_expansionSlotInstance != null) return;

//////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////        rt.anchorMin = Vector2.zero;
//////        rt.anchorMax = Vector2.one;
//////        rt.offsetMin = Vector2.zero;
//////        rt.offsetMax = Vector2.zero;
//////        rt.anchoredPosition = Vector2.zero;

//////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//////        if (slot != null) slot.Init(Row, Col, _grid);
//////    }

//////    public void HideExpansionSlot()
//////    {
//////        if (_expansionSlotInstance != null)
//////        {
//////            Destroy(_expansionSlotInstance);
//////            _expansionSlotInstance = null;
//////        }
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

////[RequireComponent(typeof(Image))]
////public class GridCell : MonoBehaviour
////{
////    public int Row { get; private set; }
////    public int Col { get; private set; }
////    public bool HasBlock => _block != null;

////    private CastleBlock _block;
////    private GameObject _expansionSlotInstance;
////    private GameObject _unitSlotInstance;       // cannon + soldier drop overlay
////    private CastleGrid _grid;

////    public void Init(int row, int col, CastleGrid grid)
////    {
////        Row = row;
////        Col = col;
////        _grid = grid;

////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
////    }

////    // ── Block placement ───────────────────────────────────────────

////    public void PlaceBlock(CastleBlock block)
////    {
////        _block = block;
////        HideExpansionSlot();
////        // Unit slot refresh is handled by CastleGrid.RefreshUnitSlots()
////    }

////    public void ClearBlock()
////    {
////        _block = null;
////        HideUnitSlot();
////        _grid.RefreshExpansionSlots();
////        _grid.RefreshUnitSlots();
////    }

////    // ── Expansion slot ────────────────────────────────────────────

////    public void ShowExpansionSlot(GameObject slotPrefab)
////    {
////        if (_expansionSlotInstance != null) return;

////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////        rt.anchorMin = Vector2.zero;
////        rt.anchorMax = Vector2.one;
////        rt.offsetMin = Vector2.zero;
////        rt.offsetMax = Vector2.zero;
////        rt.anchoredPosition = Vector2.zero;

////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////        if (slot != null) slot.Init(Row, Col, _grid);
////    }

////    public void HideExpansionSlot()
////    {
////        if (_expansionSlotInstance != null)
////        {
////            Destroy(_expansionSlotInstance);
////            _expansionSlotInstance = null;
////        }
////    }

////    // ── Unit slot (cannon + soldier drop zones) ───────────────────

////    /// <summary>
////    /// Instantiates the unit slot prefab over this cell.
////    /// Called by CastleGrid when this block becomes exposed (nothing above it).
////    /// </summary>
////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////    {
////        if (_unitSlotInstance != null) return;   // already shown
////        if (!HasBlock) return;                   // only blocks get a unit slot

////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////        // Stretch to fill the cell, then push to the top half so it sits
////        // visually "on top" of the block sprite
////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////        rt.anchorMin = new Vector2(0f, 0.5f);   // top half of the cell
////        rt.anchorMax = new Vector2(1f, 1f);
////        rt.offsetMin = Vector2.zero;
////        rt.offsetMax = Vector2.zero;
////        rt.anchoredPosition = Vector2.zero;
////        rt.SetAsLastSibling();                         // render above the block sprite
////    }

////    /// <summary>
////    /// Destroys the unit slot overlay (called when a block above is placed,
////    /// or when the block itself is removed).
////    /// </summary>
////    public void HideUnitSlot()
////    {
////        if (_unitSlotInstance != null)
////        {
////            // Remove any placed units cleanly before destroying
////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////            slot?.RemoveAll();

////            Destroy(_unitSlotInstance);
////            _unitSlotInstance = null;
////        }
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

//[RequireComponent(typeof(Image))]
//public class GridCell : MonoBehaviour
//{
//    public int Row { get; private set; }
//    public int Col { get; private set; }
//    public bool HasBlock => _block != null;

//    private CastleBlock _block;
//    private GameObject _expansionSlotInstance;
//    private GameObject _unitSlotInstance;
//    private CastleGrid _grid;

//    public void Init(int row, int col, CastleGrid grid)
//    {
//        Row = row;
//        Col = col;
//        _grid = grid;
//        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//    }

//    // ── Block placement ───────────────────────────────────────────

//    public void PlaceBlock(CastleBlock block)
//    {
//        _block = block;
//        HideExpansionSlot();
//    }

//    public void ClearBlock()
//    {
//        _block = null;
//        HideUnitSlot();
//        _grid.RefreshExpansionSlots();
//        _grid.RefreshUnitSlots();
//    }

//    // ── Expansion slot ────────────────────────────────────────────

//    public void ShowExpansionSlot(GameObject slotPrefab)
//    {
//        if (_expansionSlotInstance != null) return;

//        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//        rt.anchorMin = Vector2.zero;
//        rt.anchorMax = Vector2.one;
//        rt.offsetMin = Vector2.zero;
//        rt.offsetMax = Vector2.zero;
//        rt.anchoredPosition = Vector2.zero;

//        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//        if (slot != null) slot.Init(Row, Col, _grid);
//    }

//    public void HideExpansionSlot()
//    {
//        if (_expansionSlotInstance != null)
//        {
//            Destroy(_expansionSlotInstance);
//            _expansionSlotInstance = null;
//        }
//    }

//    // ── Unit slot ─────────────────────────────────────────────────

//    /// <summary>
//    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
//    /// The prefab fills the entire cell; its internal CannonZone / SoldierZone
//    /// children control the exact placement of each unit type above the block.
//    /// </summary>
//    public void ShowUnitSlot(GameObject unitSlotPrefab)
//    {
//        if (_unitSlotInstance != null) return;
//        if (!HasBlock) return;

//        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//        // Fill the entire cell — the prefab's own layout handles
//        // where cannon and soldier zones appear visually
//        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//        rt.anchorMin = Vector2.zero;
//        rt.anchorMax = Vector2.one;
//        rt.offsetMin = Vector2.zero;
//        rt.offsetMax = Vector2.zero;
//        rt.anchoredPosition = Vector2.zero;
//        rt.SetAsLastSibling();  // render above the block sprite
//    }

//    public void HideUnitSlot()
//    {
//        if (_unitSlotInstance != null)
//        {
//            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//            slot?.RemoveAll();

//            Destroy(_unitSlotInstance);
//            _unitSlotInstance = null;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCell : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool HasBlock => _block != null;

    private CastleBlock _block;
    private GameObject _expansionSlotInstance;
    private GameObject _unitSlotInstance;
    private CastleGrid _grid;

    public void Init(int row, int col, CastleGrid grid)
    {
        Row = row; Col = col; _grid = grid;
        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
    }

    // ── Block ─────────────────────────────────────────────────────

    public void PlaceBlock(CastleBlock block)
    {
        _block = block;
        HideExpansionSlot();
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

    public void ShowUnitSlot(GameObject unitSlotPrefab)
    {
        if (_unitSlotInstance != null) return;
        if (!HasBlock) return;

        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.SetAsLastSibling();
    }

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
}