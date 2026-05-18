
////////////using UnityEngine;
////////////using UnityEngine.UI;

////////////[RequireComponent(typeof(Image))]
////////////public class GridCell : MonoBehaviour
////////////{
////////////    public int Row { get; private set; }
////////////    public int Col { get; private set; }
////////////    public bool HasBlock => _block != null;

////////////    private CastleBlock _block;
////////////    private GameObject _expansionSlotInstance;
////////////    private CastleGrid _grid;

////////////    public void Init(int row, int col, CastleGrid grid)
////////////    {
////////////        Row = row;
////////////        Col = col;
////////////        _grid = grid;

////////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
////////////    }

////////////    public void PlaceBlock(CastleBlock block)
////////////    {
////////////        _block = block;
////////////        HideExpansionSlot();
////////////    }

////////////    public void ClearBlock()
////////////    {
////////////        _block = null;
////////////        _grid.RefreshExpansionSlots();
////////////    }

////////////    public void ShowExpansionSlot(GameObject slotPrefab)
////////////    {
////////////        if (_expansionSlotInstance != null) return;

////////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////////////        rt.anchorMin = Vector2.zero;
////////////        rt.anchorMax = Vector2.one;
////////////        rt.offsetMin = Vector2.zero;
////////////        rt.offsetMax = Vector2.zero;
////////////        rt.anchoredPosition = Vector2.zero;

////////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////////////        if (slot != null) slot.Init(Row, Col, _grid);
////////////    }

////////////    public void HideExpansionSlot()
////////////    {
////////////        if (_expansionSlotInstance != null)
////////////        {
////////////            Destroy(_expansionSlotInstance);
////////////            _expansionSlotInstance = null;
////////////        }
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;

//////////[RequireComponent(typeof(Image))]
//////////public class GridCell : MonoBehaviour
//////////{
//////////    public int Row { get; private set; }
//////////    public int Col { get; private set; }
//////////    public bool HasBlock => _block != null;

//////////    private CastleBlock _block;
//////////    private GameObject _expansionSlotInstance;
//////////    private GameObject _unitSlotInstance;       // cannon + soldier drop overlay
//////////    private CastleGrid _grid;

//////////    public void Init(int row, int col, CastleGrid grid)
//////////    {
//////////        Row = row;
//////////        Col = col;
//////////        _grid = grid;

//////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
//////////    }

//////////    // ── Block placement ───────────────────────────────────────────

//////////    public void PlaceBlock(CastleBlock block)
//////////    {
//////////        _block = block;
//////////        HideExpansionSlot();
//////////        // Unit slot refresh is handled by CastleGrid.RefreshUnitSlots()
//////////    }

//////////    public void ClearBlock()
//////////    {
//////////        _block = null;
//////////        HideUnitSlot();
//////////        _grid.RefreshExpansionSlots();
//////////        _grid.RefreshUnitSlots();
//////////    }

//////////    // ── Expansion slot ────────────────────────────────────────────

//////////    public void ShowExpansionSlot(GameObject slotPrefab)
//////////    {
//////////        if (_expansionSlotInstance != null) return;

//////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////////        rt.anchorMin = Vector2.zero;
//////////        rt.anchorMax = Vector2.one;
//////////        rt.offsetMin = Vector2.zero;
//////////        rt.offsetMax = Vector2.zero;
//////////        rt.anchoredPosition = Vector2.zero;

//////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//////////        if (slot != null) slot.Init(Row, Col, _grid);
//////////    }

//////////    public void HideExpansionSlot()
//////////    {
//////////        if (_expansionSlotInstance != null)
//////////        {
//////////            Destroy(_expansionSlotInstance);
//////////            _expansionSlotInstance = null;
//////////        }
//////////    }

//////////    // ── Unit slot (cannon + soldier drop zones) ───────────────────

//////////    /// <summary>
//////////    /// Instantiates the unit slot prefab over this cell.
//////////    /// Called by CastleGrid when this block becomes exposed (nothing above it).
//////////    /// </summary>
//////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
//////////    {
//////////        if (_unitSlotInstance != null) return;   // already shown
//////////        if (!HasBlock) return;                   // only blocks get a unit slot

//////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//////////        // Stretch to fill the cell, then push to the top half so it sits
//////////        // visually "on top" of the block sprite
//////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//////////        rt.anchorMin = new Vector2(0f, 0.5f);   // top half of the cell
//////////        rt.anchorMax = new Vector2(1f, 1f);
//////////        rt.offsetMin = Vector2.zero;
//////////        rt.offsetMax = Vector2.zero;
//////////        rt.anchoredPosition = Vector2.zero;
//////////        rt.SetAsLastSibling();                         // render above the block sprite
//////////    }

//////////    /// <summary>
//////////    /// Destroys the unit slot overlay (called when a block above is placed,
//////////    /// or when the block itself is removed).
//////////    /// </summary>
//////////    public void HideUnitSlot()
//////////    {
//////////        if (_unitSlotInstance != null)
//////////        {
//////////            // Remove any placed units cleanly before destroying
//////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//////////            slot?.RemoveAll();

//////////            Destroy(_unitSlotInstance);
//////////            _unitSlotInstance = null;
//////////        }
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

////////[RequireComponent(typeof(Image))]
////////public class GridCell : MonoBehaviour
////////{
////////    public int Row { get; private set; }
////////    public int Col { get; private set; }
////////    public bool HasBlock => _block != null;

////////    private CastleBlock _block;
////////    private GameObject _expansionSlotInstance;
////////    private GameObject _unitSlotInstance;
////////    private CastleGrid _grid;

////////    public void Init(int row, int col, CastleGrid grid)
////////    {
////////        Row = row;
////////        Col = col;
////////        _grid = grid;
////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
////////    }

////////    // ── Block placement ───────────────────────────────────────────

////////    public void PlaceBlock(CastleBlock block)
////////    {
////////        _block = block;
////////        HideExpansionSlot();
////////    }

////////    public void ClearBlock()
////////    {
////////        _block = null;
////////        HideUnitSlot();
////////        _grid.RefreshExpansionSlots();
////////        _grid.RefreshUnitSlots();
////////    }

////////    // ── Expansion slot ────────────────────────────────────────────

////////    public void ShowExpansionSlot(GameObject slotPrefab)
////////    {
////////        if (_expansionSlotInstance != null) return;

////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////////        rt.anchorMin = Vector2.zero;
////////        rt.anchorMax = Vector2.one;
////////        rt.offsetMin = Vector2.zero;
////////        rt.offsetMax = Vector2.zero;
////////        rt.anchoredPosition = Vector2.zero;

////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////////        if (slot != null) slot.Init(Row, Col, _grid);
////////    }

////////    public void HideExpansionSlot()
////////    {
////////        if (_expansionSlotInstance != null)
////////        {
////////            Destroy(_expansionSlotInstance);
////////            _expansionSlotInstance = null;
////////        }
////////    }

////////    // ── Unit slot ─────────────────────────────────────────────────

////////    /// <summary>
////////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
////////    /// The prefab fills the entire cell; its internal CannonZone / SoldierZone
////////    /// children control the exact placement of each unit type above the block.
////////    /// </summary>
////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////////    {
////////        if (_unitSlotInstance != null) return;
////////        if (!HasBlock) return;

////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////////        // Fill the entire cell — the prefab's own layout handles
////////        // where cannon and soldier zones appear visually
////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////////        rt.anchorMin = Vector2.zero;
////////        rt.anchorMax = Vector2.one;
////////        rt.offsetMin = Vector2.zero;
////////        rt.offsetMax = Vector2.zero;
////////        rt.anchoredPosition = Vector2.zero;
////////        rt.SetAsLastSibling();  // render above the block sprite
////////    }

////////    public void HideUnitSlot()
////////    {
////////        if (_unitSlotInstance != null)
////////        {
////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////////            slot?.RemoveAll();

////////            Destroy(_unitSlotInstance);
////////            _unitSlotInstance = null;
////////        }
////////    }
////////}

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
//////    private GameObject _unitSlotInstance;
//////    private CastleGrid _grid;

//////    public void Init(int row, int col, CastleGrid grid)
//////    {
//////        Row = row; Col = col; _grid = grid;
//////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//////    }

//////    // ── Block ─────────────────────────────────────────────────────

//////    public void PlaceBlock(CastleBlock block)
//////    {
//////        _block = block;
//////        HideExpansionSlot();
//////    }

//////    public void ClearBlock()
//////    {
//////        _block = null;
//////        HideUnitSlot();
//////        _grid.RefreshExpansionSlots();
//////        _grid.RefreshUnitSlots();
//////    }

//////    // ── Expansion slot ────────────────────────────────────────────

//////    public void ShowExpansionSlot(GameObject slotPrefab)
//////    {
//////        if (_expansionSlotInstance != null) return;

//////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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

//////    // ── Unit slot ─────────────────────────────────────────────────

//////    public void ShowUnitSlot(GameObject unitSlotPrefab)
//////    {
//////        if (_unitSlotInstance != null) return;
//////        if (!HasBlock) return;

//////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//////        rt.anchoredPosition = Vector2.zero;
//////        rt.SetAsLastSibling();
//////    }

//////    public void HideUnitSlot()
//////    {
//////        if (_unitSlotInstance != null)
//////        {
//////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//////            slot?.RemoveAll();
//////            Destroy(_unitSlotInstance);
//////            _unitSlotInstance = null;
//////        }
//////    }

//////    /// <summary>
//////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
//////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
//////    /// unit on the block below without placing a new block.
//////    /// Returns null if no unit slot exists or no matching zone found.
//////    /// </summary>
//////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
//////    {
//////        if (_unitSlotInstance == null) return null;

//////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
//////        {
//////            if (zone.acceptedType == unitType)
//////                return zone;
//////        }
//////        return null;
//////    }
//////}


//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;

//////////////////[RequireComponent(typeof(Image))]
//////////////////public class GridCell : MonoBehaviour
//////////////////{
//////////////////    public int Row { get; private set; }
//////////////////    public int Col { get; private set; }
//////////////////    public bool HasBlock => _block != null;

//////////////////    private CastleBlock _block;
//////////////////    private GameObject _expansionSlotInstance;
//////////////////    private CastleGrid _grid;

//////////////////    public void Init(int row, int col, CastleGrid grid)
//////////////////    {
//////////////////        Row = row;
//////////////////        Col = col;
//////////////////        _grid = grid;

//////////////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
//////////////////    }

//////////////////    public void PlaceBlock(CastleBlock block)
//////////////////    {
//////////////////        _block = block;
//////////////////        HideExpansionSlot();
//////////////////    }

//////////////////    public void ClearBlock()
//////////////////    {
//////////////////        _block = null;
//////////////////        _grid.RefreshExpansionSlots();
//////////////////    }

//////////////////    public void ShowExpansionSlot(GameObject slotPrefab)
//////////////////    {
//////////////////        if (_expansionSlotInstance != null) return;

//////////////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////////////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////////////////        rt.anchorMin = Vector2.zero;
//////////////////        rt.anchorMax = Vector2.one;
//////////////////        rt.offsetMin = Vector2.zero;
//////////////////        rt.offsetMax = Vector2.zero;
//////////////////        rt.anchoredPosition = Vector2.zero;

//////////////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//////////////////        if (slot != null) slot.Init(Row, Col, _grid);
//////////////////    }

//////////////////    public void HideExpansionSlot()
//////////////////    {
//////////////////        if (_expansionSlotInstance != null)
//////////////////        {
//////////////////            Destroy(_expansionSlotInstance);
//////////////////            _expansionSlotInstance = null;
//////////////////        }
//////////////////    }
//////////////////}

////////////////using UnityEngine;
////////////////using UnityEngine.UI;

////////////////[RequireComponent(typeof(Image))]
////////////////public class GridCell : MonoBehaviour
////////////////{
////////////////    public int Row { get; private set; }
////////////////    public int Col { get; private set; }
////////////////    public bool HasBlock => _block != null;

////////////////    private CastleBlock _block;
////////////////    private GameObject _expansionSlotInstance;
////////////////    private GameObject _unitSlotInstance;       // cannon + soldier drop overlay
////////////////    private CastleGrid _grid;

////////////////    public void Init(int row, int col, CastleGrid grid)
////////////////    {
////////////////        Row = row;
////////////////        Col = col;
////////////////        _grid = grid;

////////////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
////////////////    }

////////////////    // ── Block placement ───────────────────────────────────────────

////////////////    public void PlaceBlock(CastleBlock block)
////////////////    {
////////////////        _block = block;
////////////////        HideExpansionSlot();
////////////////        // Unit slot refresh is handled by CastleGrid.RefreshUnitSlots()
////////////////    }

////////////////    public void ClearBlock()
////////////////    {
////////////////        _block = null;
////////////////        HideUnitSlot();
////////////////        _grid.RefreshExpansionSlots();
////////////////        _grid.RefreshUnitSlots();
////////////////    }

////////////////    // ── Expansion slot ────────────────────────────────────────────

////////////////    public void ShowExpansionSlot(GameObject slotPrefab)
////////////////    {
////////////////        if (_expansionSlotInstance != null) return;

////////////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////////////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////////////////        rt.anchorMin = Vector2.zero;
////////////////        rt.anchorMax = Vector2.one;
////////////////        rt.offsetMin = Vector2.zero;
////////////////        rt.offsetMax = Vector2.zero;
////////////////        rt.anchoredPosition = Vector2.zero;

////////////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////////////////        if (slot != null) slot.Init(Row, Col, _grid);
////////////////    }

////////////////    public void HideExpansionSlot()
////////////////    {
////////////////        if (_expansionSlotInstance != null)
////////////////        {
////////////////            Destroy(_expansionSlotInstance);
////////////////            _expansionSlotInstance = null;
////////////////        }
////////////////    }

////////////////    // ── Unit slot (cannon + soldier drop zones) ───────────────────

////////////////    /// <summary>
////////////////    /// Instantiates the unit slot prefab over this cell.
////////////////    /// Called by CastleGrid when this block becomes exposed (nothing above it).
////////////////    /// </summary>
////////////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////////////////    {
////////////////        if (_unitSlotInstance != null) return;   // already shown
////////////////        if (!HasBlock) return;                   // only blocks get a unit slot

////////////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////////////////        // Stretch to fill the cell, then push to the top half so it sits
////////////////        // visually "on top" of the block sprite
////////////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////////////////        rt.anchorMin = new Vector2(0f, 0.5f);   // top half of the cell
////////////////        rt.anchorMax = new Vector2(1f, 1f);
////////////////        rt.offsetMin = Vector2.zero;
////////////////        rt.offsetMax = Vector2.zero;
////////////////        rt.anchoredPosition = Vector2.zero;
////////////////        rt.SetAsLastSibling();                         // render above the block sprite
////////////////    }

////////////////    /// <summary>
////////////////    /// Destroys the unit slot overlay (called when a block above is placed,
////////////////    /// or when the block itself is removed).
////////////////    /// </summary>
////////////////    public void HideUnitSlot()
////////////////    {
////////////////        if (_unitSlotInstance != null)
////////////////        {
////////////////            // Remove any placed units cleanly before destroying
////////////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////////////////            slot?.RemoveAll();

////////////////            Destroy(_unitSlotInstance);
////////////////            _unitSlotInstance = null;
////////////////        }
////////////////    }
////////////////}

//////////////using UnityEngine;
//////////////using UnityEngine.UI;

//////////////[RequireComponent(typeof(Image))]
//////////////public class GridCell : MonoBehaviour
//////////////{
//////////////    public int Row { get; private set; }
//////////////    public int Col { get; private set; }
//////////////    public bool HasBlock => _block != null;

//////////////    private CastleBlock _block;
//////////////    private GameObject _expansionSlotInstance;
//////////////    private GameObject _unitSlotInstance;
//////////////    private CastleGrid _grid;

//////////////    public void Init(int row, int col, CastleGrid grid)
//////////////    {
//////////////        Row = row;
//////////////        Col = col;
//////////////        _grid = grid;
//////////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//////////////    }

//////////////    // ── Block placement ───────────────────────────────────────────

//////////////    public void PlaceBlock(CastleBlock block)
//////////////    {
//////////////        _block = block;
//////////////        HideExpansionSlot();
//////////////    }

//////////////    public void ClearBlock()
//////////////    {
//////////////        _block = null;
//////////////        HideUnitSlot();
//////////////        _grid.RefreshExpansionSlots();
//////////////        _grid.RefreshUnitSlots();
//////////////    }

//////////////    // ── Expansion slot ────────────────────────────────────────────

//////////////    public void ShowExpansionSlot(GameObject slotPrefab)
//////////////    {
//////////////        if (_expansionSlotInstance != null) return;

//////////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////////////        rt.anchorMin = Vector2.zero;
//////////////        rt.anchorMax = Vector2.one;
//////////////        rt.offsetMin = Vector2.zero;
//////////////        rt.offsetMax = Vector2.zero;
//////////////        rt.anchoredPosition = Vector2.zero;

//////////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//////////////        if (slot != null) slot.Init(Row, Col, _grid);
//////////////    }

//////////////    public void HideExpansionSlot()
//////////////    {
//////////////        if (_expansionSlotInstance != null)
//////////////        {
//////////////            Destroy(_expansionSlotInstance);
//////////////            _expansionSlotInstance = null;
//////////////        }
//////////////    }

//////////////    // ── Unit slot ─────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
//////////////    /// The prefab fills the entire cell; its internal CannonZone / SoldierZone
//////////////    /// children control the exact placement of each unit type above the block.
//////////////    /// </summary>
//////////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
//////////////    {
//////////////        if (_unitSlotInstance != null) return;
//////////////        if (!HasBlock) return;

//////////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//////////////        // Fill the entire cell — the prefab's own layout handles
//////////////        // where cannon and soldier zones appear visually
//////////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//////////////        rt.anchorMin = Vector2.zero;
//////////////        rt.anchorMax = Vector2.one;
//////////////        rt.offsetMin = Vector2.zero;
//////////////        rt.offsetMax = Vector2.zero;
//////////////        rt.anchoredPosition = Vector2.zero;
//////////////        rt.SetAsLastSibling();  // render above the block sprite
//////////////    }

//////////////    public void HideUnitSlot()
//////////////    {
//////////////        if (_unitSlotInstance != null)
//////////////        {
//////////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//////////////            slot?.RemoveAll();

//////////////            Destroy(_unitSlotInstance);
//////////////            _unitSlotInstance = null;
//////////////        }
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;

////////////[RequireComponent(typeof(Image))]
////////////public class GridCell : MonoBehaviour
////////////{
////////////    public int Row { get; private set; }
////////////    public int Col { get; private set; }
////////////    public bool HasBlock => _block != null;

////////////    private CastleBlock _block;
////////////    private GameObject _expansionSlotInstance;
////////////    private GameObject _unitSlotInstance;
////////////    private CastleGrid _grid;

////////////    public void Init(int row, int col, CastleGrid grid)
////////////    {
////////////        Row = row; Col = col; _grid = grid;
////////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
////////////    }

////////////    // ── Block ─────────────────────────────────────────────────────

////////////    public void PlaceBlock(CastleBlock block)
////////////    {
////////////        _block = block;
////////////        HideExpansionSlot();
////////////    }

////////////    public void ClearBlock()
////////////    {
////////////        _block = null;
////////////        HideUnitSlot();
////////////        _grid.RefreshExpansionSlots();
////////////        _grid.RefreshUnitSlots();
////////////    }

////////////    // ── Expansion slot ────────────────────────────────────────────

////////////    public void ShowExpansionSlot(GameObject slotPrefab)
////////////    {
////////////        if (_expansionSlotInstance != null) return;

////////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////////////        rt.anchoredPosition = Vector2.zero;

////////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////////////        if (slot != null) slot.Init(Row, Col, _grid);
////////////    }

////////////    public void HideExpansionSlot()
////////////    {
////////////        if (_expansionSlotInstance != null)
////////////        {
////////////            Destroy(_expansionSlotInstance);
////////////            _expansionSlotInstance = null;
////////////        }
////////////    }

////////////    // ── Unit slot ─────────────────────────────────────────────────

////////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////////////    {
////////////        if (_unitSlotInstance != null) return;
////////////        if (!HasBlock) return;

////////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////////////        rt.anchoredPosition = Vector2.zero;
////////////        rt.SetAsLastSibling();
////////////    }

////////////    public void HideUnitSlot()
////////////    {
////////////        if (_unitSlotInstance != null)
////////////        {
////////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////////////            slot?.RemoveAll();
////////////            Destroy(_unitSlotInstance);
////////////            _unitSlotInstance = null;
////////////        }
////////////    }

////////////    /// <summary>
////////////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
////////////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
////////////    /// unit on the block below without placing a new block.
////////////    /// Returns null if no unit slot exists or no matching zone found.
////////////    /// </summary>
////////////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
////////////    {
////////////        if (_unitSlotInstance == null) return null;

////////////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
////////////        {
////////////            if (zone.acceptedType == unitType)
////////////                return zone;
////////////        }
////////////        return null;
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;

//////////[RequireComponent(typeof(Image))]
//////////public class GridCell : MonoBehaviour
//////////{
//////////    public int Row { get; private set; }
//////////    public int Col { get; private set; }
//////////    public bool HasBlock => _block != null;

//////////    private CastleBlock _block;
//////////    private GameObject _expansionSlotInstance;
//////////    private GameObject _unitSlotInstance;
//////////    private CastleGrid _grid;

//////////    public void Init(int row, int col, CastleGrid grid)
//////////    {
//////////        Row = row; Col = col; _grid = grid;
//////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//////////    }

//////////    // ── Block ─────────────────────────────────────────────────────

//////////    public void PlaceBlock(CastleBlock block)
//////////    {
//////////        _block = block;
//////////        HideExpansionSlot();
//////////    }

//////////    public void ClearBlock()
//////////    {
//////////        _block = null;
//////////        HideUnitSlot();
//////////        _grid.RefreshExpansionSlots();
//////////        _grid.RefreshUnitSlots();
//////////    }

//////////    // ── Expansion slot ────────────────────────────────────────────

//////////    public void ShowExpansionSlot(GameObject slotPrefab)
//////////    {
//////////        if (_expansionSlotInstance != null) return;

//////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//////////        rt.anchoredPosition = Vector2.zero;

//////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
//////////        if (slot != null) slot.Init(Row, Col, _grid);
//////////    }

//////////    public void HideExpansionSlot()
//////////    {
//////////        if (_expansionSlotInstance != null)
//////////        {
//////////            Destroy(_expansionSlotInstance);
//////////            _expansionSlotInstance = null;
//////////        }
//////////    }

//////////    // ── Unit slot ─────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
//////////    /// Safe to call repeatedly — will no-op if already shown.
//////////    /// </summary>
//////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
//////////    {
//////////        if (_unitSlotInstance != null) return;
//////////        if (!HasBlock) return;

//////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//////////        rt.anchoredPosition = Vector2.zero;
//////////        rt.SetAsLastSibling();
//////////    }

//////////    /// <summary>
//////////    /// Destroys the unit slot overlay and removes any placed units.
//////////    /// Only call this when the block is genuinely removed or covered by another block.
//////////    /// Do NOT call this on panel switches — use SetUnitSlotInteractable() instead.
//////////    /// </summary>
//////////    public void HideUnitSlot()
//////////    {
//////////        if (_unitSlotInstance != null)
//////////        {
//////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//////////            slot?.RemoveAll();
//////////            Destroy(_unitSlotInstance);
//////////            _unitSlotInstance = null;
//////////        }
//////////    }

//////////    /// <summary>
//////////    /// Toggles whether the unit slot can be interacted with (drag-drop).
//////////    /// When interactable = false the cannon/soldier visuals remain fully visible
//////////    /// but pointer events are blocked — used when the Village Panel is active.
//////////    /// A CanvasGroup is added automatically if the prefab doesn't already have one.
//////////    /// </summary>
//////////    public void SetUnitSlotInteractable(bool interactable)
//////////    {
//////////        if (_unitSlotInstance == null) return;

//////////        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
//////////        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

//////////        // Do NOT set cg.interactable = false — that triggers Unity's Disabled color
//////////        // tint on every child Selectable, making the cannon look transparent/ghosted.
//////////        // blocksRaycasts = false alone is enough to block all pointer and drag events.
//////////        cg.alpha = 1f;
//////////        cg.interactable = true;
//////////        cg.blocksRaycasts = interactable;
//////////    }

//////////    /// <summary>
//////////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
//////////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
//////////    /// unit on the block below without placing a new block.
//////////    /// Returns null if no unit slot exists or no matching zone found.
//////////    /// </summary>
//////////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
//////////    {
//////////        if (_unitSlotInstance == null) return null;

//////////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
//////////        {
//////////            if (zone.acceptedType == unitType)
//////////                return zone;
//////////        }
//////////        return null;
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

////////[RequireComponent(typeof(Image))]
////////public class GridCell : MonoBehaviour
////////{
////////    public int Row { get; private set; }
////////    public int Col { get; private set; }
////////    public bool HasBlock => _block != null;

////////    private CastleBlock _block;
////////    private GameObject _expansionSlotInstance;
////////    private GameObject _unitSlotInstance;
////////    private CastleGrid _grid;

////////    public void Init(int row, int col, CastleGrid grid)
////////    {
////////        Row = row; Col = col; _grid = grid;
////////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
////////    }

////////    // ── Block ─────────────────────────────────────────────────────

////////    public void PlaceBlock(CastleBlock block)
////////    {
////////        _block = block;
////////        HideExpansionSlot();
////////    }

////////    public void ClearBlock()
////////    {
////////        _block = null;
////////        HideUnitSlot();
////////        _grid.RefreshExpansionSlots();
////////        _grid.RefreshUnitSlots();
////////    }

////////    // ── Expansion slot ────────────────────────────────────────────

////////    public void ShowExpansionSlot(GameObject slotPrefab)
////////    {
////////        if (_expansionSlotInstance != null) return;

////////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

////////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////////        rt.anchoredPosition = Vector2.zero;

////////        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
////////        if (slot != null) slot.Init(Row, Col, _grid);
////////    }

////////    public void HideExpansionSlot()
////////    {
////////        if (_expansionSlotInstance != null)
////////        {
////////            Destroy(_expansionSlotInstance);
////////            _expansionSlotInstance = null;
////////        }
////////    }

////////    // ── Unit slot ─────────────────────────────────────────────────

////////    /// <summary>
////////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
////////    /// Safe to call repeatedly — will no-op if already shown.
////////    /// </summary>
////////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////////    {
////////        if (_unitSlotInstance != null) return;
////////        if (!HasBlock) return;

////////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////////        rt.anchoredPosition = Vector2.zero;
////////        rt.SetAsLastSibling();
////////    }

////////    /// <summary>
////////    /// Destroys the unit slot overlay and removes any placed units.
////////    /// Only call this when the block is genuinely removed or covered by another block.
////////    /// Do NOT call this on panel switches — use SetUnitSlotInteractable() instead.
////////    /// </summary>
////////    public void HideUnitSlot()
////////    {
////////        if (_unitSlotInstance != null)
////////        {
////////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////////            slot?.RemoveAll();
////////            Destroy(_unitSlotInstance);
////////            _unitSlotInstance = null;
////////        }
////////    }

////////    /// <summary>
////////    /// Toggles whether the unit slot can be interacted with (drag-drop).
////////    /// When interactable = false the cannon/soldier visuals remain fully visible
////////    /// but pointer events are blocked — used when the Village Panel is active.
////////    /// A CanvasGroup is added automatically if the prefab doesn't already have one.
////////    /// </summary>
////////    public void SetUnitSlotInteractable(bool interactable)
////////    {
////////        if (_unitSlotInstance == null) return;

////////        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
////////        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

////////        // Do NOT set cg.interactable = false — that triggers Unity's Disabled color
////////        // tint on every child Selectable, making the cannon look transparent/ghosted.
////////        // blocksRaycasts = false alone is enough to block all pointer and drag events.
////////        cg.alpha = 1f;
////////        cg.interactable = true;
////////        cg.blocksRaycasts = interactable;
////////    }

////////    /// <summary>
////////    /// Returns the CastleBlockUnitSlot component on this cell's unit slot instance.
////////    /// Used by CastleGrid to migrate units before a block covers this cell.
////////    /// </summary>
////////    public CastleBlockUnitSlot GetUnitSlot() =>
////////        _unitSlotInstance != null ? _unitSlotInstance.GetComponent<CastleBlockUnitSlot>() : null;

////////    /// <summary>
////////    /// Reparents every unit sitting inside each CastleUnitDropZone on this cell
////////    /// into the matching drop zone on <paramref name="target"/>.
////////    /// The unit GameObjects survive — nothing is destroyed.
////////    /// Call this before HideUnitSlotEmpty() so the cannons are safely moved first.
////////    /// </summary>
////////    public void TransferUnitSlotTo(GridCell target)
////////    {
////////        if (_unitSlotInstance == null || target == null) return;

////////        foreach (var srcZone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
////////        {
////////            // Find the matching zone on the destination by accepted unit type
////////            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
////////            if (destZone == null) continue;

////////            // Reparent every child (the placed cannon / soldier GameObject)
////////            // from the source zone to the destination zone
////////            for (int i = srcZone.transform.childCount - 1; i >= 0; i--)
////////            {
////////                Transform unit = srcZone.transform.GetChild(i);
////////                unit.SetParent(destZone.transform, false);
////////                unit.localPosition = Vector3.zero;
////////            }

////////            // Let the source zone know it's now empty (if it tracks by reference)
////////            srcZone.ClearOccupant();
////////        }
////////    }

////////    /// <summary>
////////    /// Destroys the unit slot container WITHOUT calling RemoveAll().
////////    /// Only use after TransferUnitSlotTo() has already moved all units out.
////////    /// </summary>
////////    public void HideUnitSlotEmpty()
////////    {
////////        if (_unitSlotInstance != null)
////////        {
////////            Destroy(_unitSlotInstance);
////////            _unitSlotInstance = null;
////////        }
////////    }

////////    /// <summary>
////////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
////////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
////////    /// unit on the block below without placing a new block.
////////    /// Returns null if no unit slot exists or no matching zone found.
////////    /// </summary>
////////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
////////    {
////////        if (_unitSlotInstance == null) return null;

////////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
////////        {
////////            if (zone.acceptedType == unitType)
////////                return zone;
////////        }
////////        return null;
////////    }
////////}



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
//////    private GameObject _unitSlotInstance;
//////    private CastleGrid _grid;

//////    public void Init(int row, int col, CastleGrid grid)
//////    {
//////        Row = row; Col = col; _grid = grid;
//////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//////    }

//////    // ── Block ─────────────────────────────────────────────────────

//////    public void PlaceBlock(CastleBlock block)
//////    {
//////        _block = block;
//////        HideExpansionSlot();
//////    }

//////    public void ClearBlock()
//////    {
//////        _block = null;
//////        HideUnitSlot();
//////        _grid.RefreshExpansionSlots();
//////        _grid.RefreshUnitSlots();
//////    }

//////    // ── Expansion slot ────────────────────────────────────────────

//////    public void ShowExpansionSlot(GameObject slotPrefab)
//////    {
//////        if (_expansionSlotInstance != null) return;

//////        _expansionSlotInstance = Instantiate(slotPrefab, transform);

//////        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
//////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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

//////    // ── Unit slot ─────────────────────────────────────────────────

//////    /// <summary>
//////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
//////    /// Safe to call repeatedly — will no-op if already shown.
//////    /// </summary>
//////    public void ShowUnitSlot(GameObject unitSlotPrefab)
//////    {
//////        if (_unitSlotInstance != null) return;
//////        if (!HasBlock) return;

//////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//////        rt.anchoredPosition = Vector2.zero;
//////        rt.SetAsLastSibling();
//////    }

//////    /// <summary>
//////    /// Destroys the unit slot overlay and removes any placed units.
//////    /// Only call this when the block is genuinely removed or covered by another block.
//////    /// Do NOT call this on panel switches — use SetUnitSlotInteractable() instead.
//////    /// </summary>
//////    public void HideUnitSlot()
//////    {
//////        if (_unitSlotInstance != null)
//////        {
//////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//////            slot?.RemoveAll();
//////            Destroy(_unitSlotInstance);
//////            _unitSlotInstance = null;
//////        }
//////    }

//////    /// <summary>
//////    /// Toggles whether the unit slot can be interacted with (drag-drop).
//////    /// When interactable = false the cannon/soldier visuals remain fully visible
//////    /// but pointer events are blocked — used when the Village Panel is active.
//////    /// A CanvasGroup is added automatically if the prefab doesn't already have one.
//////    /// </summary>
//////    public void SetUnitSlotInteractable(bool interactable)
//////    {
//////        if (_unitSlotInstance == null) return;

//////        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
//////        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

//////        // Do NOT set cg.interactable = false — that triggers Unity's Disabled color
//////        // tint on every child Selectable, making the cannon look transparent/ghosted.
//////        // blocksRaycasts = false alone is enough to block all pointer and drag events.
//////        cg.alpha = 1f;
//////        cg.interactable = true;
//////        cg.blocksRaycasts = interactable;
//////    }

//////    /// <summary>
//////    /// Returns the CastleBlockUnitSlot component on this cell's unit slot instance.
//////    /// Used by CastleGrid to migrate units before a block covers this cell.
//////    /// </summary>
//////    public CastleBlockUnitSlot GetUnitSlot() =>
//////        _unitSlotInstance != null ? _unitSlotInstance.GetComponent<CastleBlockUnitSlot>() : null;

//////    /// <summary>
//////    /// Reparents every unit sitting inside each CastleUnitDropZone on this cell
//////    /// into the matching drop zone on <paramref name="target"/>.
//////    /// Searches the entire cell hierarchy — works even if _unitSlotInstance was
//////    /// lost on a panel reload. The unit GameObjects are never destroyed.
//////    /// Call this before HideUnitSlotEmpty() so the cannons are safely moved first.
//////    /// </summary>
//////    public void TransferUnitSlotTo(GridCell target)
//////    {
//////        if (target == null) return;

//////        // Search the whole cell hierarchy — not just _unitSlotInstance —
//////        // so this is robust even when the slot reference was dropped on panel reload
//////        foreach (var srcZone in GetComponentsInChildren<CastleUnitDropZone>(true))
//////        {
//////            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
//////            if (destZone == null) continue;

//////            // Reparent every placed unit (cannon / soldier) from source → dest
//////            for (int i = srcZone.transform.childCount - 1; i >= 0; i--)
//////            {
//////                Transform unit = srcZone.transform.GetChild(i);
//////                unit.SetParent(destZone.transform, false);

//////                // Only reset the offset/position values that accumulate drift when
//////                // reparenting between cells. Do NOT touch anchorMin/anchorMax —
//////                // each unit type has its own anchor setup; overriding it causes
//////                // the soldier to snap to the cannon's position.
//////                RectTransform rt = unit.GetComponent<RectTransform>();
//////                if (rt != null)
//////                {
//////                    rt.offsetMin = Vector2.zero;
//////                    rt.offsetMax = Vector2.zero;
//////                    rt.anchoredPosition = Vector2.zero;
//////                    rt.localScale = Vector3.one;
//////                }
//////                else
//////                {
//////                    unit.localPosition = Vector3.zero;
//////                }
//////            }

//////            // Note: if CastleUnitDropZone tracks its occupant by a field reference,
//////            // add srcZone.ClearOccupant() here once that method exists.
//////        }
//////    }

//////    /// <summary>
//////    /// Destroys the unit slot container WITHOUT calling RemoveAll().
//////    /// Only use after TransferUnitSlotTo() has already moved all units out.
//////    /// </summary>
//////    public void HideUnitSlotEmpty()
//////    {
//////        if (_unitSlotInstance != null)
//////        {
//////            Destroy(_unitSlotInstance);
//////            _unitSlotInstance = null;
//////        }
//////    }

//////    /// <summary>
//////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
//////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
//////    /// unit on the block below without placing a new block.
//////    /// Returns null if no unit slot exists or no matching zone found.
//////    /// </summary>
//////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
//////    {
//////        if (_unitSlotInstance == null) return null;

//////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
//////        {
//////            if (zone.acceptedType == unitType)
//////                return zone;
//////        }
//////        return null;
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
////    private GameObject _unitSlotInstance;
////    private GameObject _unitSlotPrefab;   // cached so migration can self-initialise
////    private CastleGrid _grid;

////    public void Init(int row, int col, CastleGrid grid)
////    {
////        Row = row; Col = col; _grid = grid;
////        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
////    }

////    // ── Block ─────────────────────────────────────────────────────

////    public void PlaceBlock(CastleBlock block)
////    {
////        _block = block;
////        HideExpansionSlot();
////        // Migration and slot refresh are handled by CastleGrid.PlaceBlockAt
////        // via MigrateUnitsFromBelow() → RefreshUnitSlots(). Do NOT call them here.
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
////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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

////    // ── Unit slot ─────────────────────────────────────────────────

////    /// <summary>
////    /// Instantiates the CastleBlockUnitSlot prefab over this cell.
////    /// Safe to call repeatedly — will no-op if already shown.
////    /// </summary>
////    public void ShowUnitSlot(GameObject unitSlotPrefab)
////    {
////        _unitSlotPrefab = unitSlotPrefab;   // cache for use during migration
////        if (_unitSlotInstance != null) return;
////        if (!HasBlock) return;

////        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

////        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
////        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////        rt.anchoredPosition = Vector2.zero;
////        rt.SetAsLastSibling();
////    }

////    /// <summary>
////    /// Destroys the unit slot overlay and removes any placed units.
////    /// Only call this when the block is genuinely removed or covered by another block.
////    /// Do NOT call this on panel switches — use SetUnitSlotInteractable() instead.
////    /// </summary>
////    public void HideUnitSlot()
////    {
////        if (_unitSlotInstance != null)
////        {
////            CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
////            slot?.RemoveAll();
////            Destroy(_unitSlotInstance);
////            _unitSlotInstance = null;
////        }
////    }

////    /// <summary>
////    /// Toggles whether the unit slot can be interacted with (drag-drop).
////    /// When interactable = false the cannon/soldier visuals remain fully visible
////    /// but pointer events are blocked — used when the Village Panel is active.
////    /// A CanvasGroup is added automatically if the prefab doesn't already have one.
////    /// </summary>
////    public void SetUnitSlotInteractable(bool interactable)
////    {
////        if (_unitSlotInstance == null) return;

////        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
////        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

////        // Do NOT set cg.interactable = false — that triggers Unity's Disabled color
////        // tint on every child Selectable, making the cannon look transparent/ghosted.
////        // blocksRaycasts = false alone is enough to block all pointer and drag events.
////        cg.alpha = 1f;
////        cg.interactable = true;
////        cg.blocksRaycasts = interactable;
////    }

////    /// <summary>
////    /// Returns the CastleBlockUnitSlot component on this cell's unit slot instance.
////    /// Used by CastleGrid to migrate units before a block covers this cell.
////    /// </summary>
////    public CastleBlockUnitSlot GetUnitSlot() =>
////        _unitSlotInstance != null ? _unitSlotInstance.GetComponent<CastleBlockUnitSlot>() : null;

////    /// <summary>
////    /// Reparents every unit sitting inside each CastleUnitDropZone on this cell
////    /// into the matching drop zone on <paramref name="target"/>.
////    /// Searches the entire cell hierarchy — works even if _unitSlotInstance was
////    /// lost on a panel reload. The unit GameObjects are never destroyed.
////    /// Call this before HideUnitSlotEmpty() so the cannons are safely moved first.
////    /// </summary>
////    public void TransferUnitSlotTo(GridCell target)
////    {
////        if (target == null) return;

////        // Search the whole cell hierarchy so this works even if _unitSlotInstance
////        // was lost on a panel reload.
////        foreach (var srcZone in GetComponentsInChildren<CastleUnitDropZone>(true))
////        {
////            // Skip zones that have no cannon — HasUnit is the authoritative check.
////            // (childCount is NOT reliable: EmptyVisual / Highlight / Soldier are
////            //  always children of the zone even when it is empty.)
////            if (!srcZone.HasUnit) continue;

////            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
////            if (destZone == null || destZone.HasUnit) continue;

////            // Use the proper API so _placedInstance, HasUnit, PlacedVariantId,
////            // and the soldier image are all updated correctly in both zones.
////            srcZone.MigrateUnitTo(destZone);
////        }
////    }

////    /// <summary>
////    /// Destroys the unit slot container WITHOUT calling RemoveAll().
////    /// Only use after TransferUnitSlotTo() has already moved all units out.
////    /// </summary>
////    public void HideUnitSlotEmpty()
////    {
////        if (_unitSlotInstance != null)
////        {
////            Destroy(_unitSlotInstance);
////            _unitSlotInstance = null;
////        }
////    }

////    /// <summary>
////    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
////    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
////    /// unit on the block below without placing a new block.
////    /// Returns null if no unit slot exists or no matching zone found.
////    /// </summary>
////    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
////    {
////        if (_unitSlotInstance == null) return null;

////        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
////        {
////            if (zone.acceptedType == unitType)
////                return zone;
////        }
////        return null;
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
//    private GameObject _unitSlotPrefab;   // cached so migration can self-initialise
//    private CastleGrid _grid;

//    public void Init(int row, int col, CastleGrid grid)
//    {
//        Row = row; Col = col; _grid = grid;
//        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
//    }

//    // ── Block ─────────────────────────────────────────────────────

//    public void PlaceBlock(CastleBlock block)
//    {
//        _block = block;
//        HideExpansionSlot();
//        // Migration and slot refresh are handled by CastleGrid.PlaceBlockAt
//        // via MigrateUnitsFromBelow() → RefreshUnitSlots(). Do NOT call them here.
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
//        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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
//    /// Safe to call repeatedly — will no-op if already shown.
//    /// </summary>
//    public void ShowUnitSlot(GameObject unitSlotPrefab)
//    {
//        _unitSlotPrefab = unitSlotPrefab;   // cache for use during migration
//        if (_unitSlotInstance != null) return;
//        if (!HasBlock) return;

//        _unitSlotInstance = Instantiate(unitSlotPrefab, transform);

//        RectTransform rt = _unitSlotInstance.GetComponent<RectTransform>();
//        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//        rt.anchoredPosition = Vector2.zero;
//        rt.SetAsLastSibling();
//    }

//    /// <summary>
//    /// Destroys the unit slot overlay and removes any placed units.
//    /// Only call this when the block is genuinely removed or covered by another block.
//    /// Do NOT call this on panel switches — use SetUnitSlotVillageMode() instead.
//    /// </summary>
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

//    /// <summary>
//    /// Switches the cannon zone between village and castle display modes.
//    ///
//    /// Village mode (isVillage = true):
//    ///   The cannon zone background goes fully transparent (alpha = 0) so the
//    ///   slot UI does not clutter the village view.
//    ///   Raycasts are NOT blocked — the player can drag a cannon from the shop
//    ///   and drop it onto a block while in the village panel.
//    ///   Any cannon / soldier already placed on the block stays fully visible.
//    ///
//    /// Castle mode (isVillage = false):
//    ///   The cannon zone background is restored to its normal tinted color.
//    ///   Drag-and-drop continues to work as before.
//    ///
//    /// This replaces the old SetUnitSlotInteractable() approach, which blocked
//    /// raycasts and prevented drops in the village panel.
//    /// </summary>
//    public void SetUnitSlotVillageMode(bool isVillage)
//    {
//        if (_unitSlotInstance == null) return;

//        CastleBlockUnitSlot slot = _unitSlotInstance.GetComponent<CastleBlockUnitSlot>();
//        slot?.SetVillageMode(isVillage);
//    }

//    /// <summary>
//    /// Returns the CastleBlockUnitSlot component on this cell's unit slot instance.
//    /// Used by CastleGrid to migrate units before a block covers this cell.
//    /// </summary>
//    public CastleBlockUnitSlot GetUnitSlot() =>
//        _unitSlotInstance != null ? _unitSlotInstance.GetComponent<CastleBlockUnitSlot>() : null;

//    /// <summary>
//    /// Reparents every unit sitting inside each CastleUnitDropZone on this cell
//    /// into the matching drop zone on <paramref name="target"/>.
//    /// Searches the entire cell hierarchy — works even if _unitSlotInstance was
//    /// lost on a panel reload. The unit GameObjects are never destroyed.
//    /// Call this before HideUnitSlotEmpty() so the cannons are safely moved first.
//    /// </summary>
//    public void TransferUnitSlotTo(GridCell target)
//    {
//        if (target == null) return;

//        foreach (var srcZone in GetComponentsInChildren<CastleUnitDropZone>(true))
//        {
//            if (!srcZone.HasUnit) continue;

//            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
//            if (destZone == null || destZone.HasUnit) continue;

//            srcZone.MigrateUnitTo(destZone);
//        }
//    }

//    /// <summary>
//    /// Destroys the unit slot container WITHOUT calling RemoveAll().
//    /// Only use after TransferUnitSlotTo() has already moved all units out.
//    /// </summary>
//    public void HideUnitSlotEmpty()
//    {
//        if (_unitSlotInstance != null)
//        {
//            Destroy(_unitSlotInstance);
//            _unitSlotInstance = null;
//        }
//    }

//    /// <summary>
//    /// Returns the CastleUnitDropZone on this cell's unit slot that accepts
//    /// <paramref name="unitType"/>. Used by ExpansionSlot to seat a dragged
//    /// unit on the block below without placing a new block.
//    /// Returns null if no unit slot exists or no matching zone found.
//    /// </summary>
//    public CastleUnitDropZone FindDropZoneForType(CastleUnitType unitType)
//    {
//        if (_unitSlotInstance == null) return null;

//        foreach (var zone in _unitSlotInstance.GetComponentsInChildren<CastleUnitDropZone>(true))
//        {
//            if (zone.acceptedType == unitType)
//                return zone;
//        }
//        return null;
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
    private GameObject _unitSlotPrefab;   // cached so migration can self-initialise
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
        rt.SetAsLastSibling();
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

        // CanvasGroup gates all pointer events for the entire unit-slot subtree.
        // Village  → raycasts ON  (cannon drag-drop enabled).
        // Castle   → raycasts OFF (cannon drag-drop disabled; block-adding still works).
        CanvasGroup cg = _unitSlotInstance.GetComponent<CanvasGroup>();
        if (cg == null) cg = _unitSlotInstance.AddComponent<CanvasGroup>();

        cg.alpha = 1f;    // never dim the slot — alpha is handled per-zone below
        cg.interactable = true;  // avoid Unity's disabled tint on child Selectables
        cg.blocksRaycasts = isVillage;   // TRUE = village (interactive), FALSE = castle (locked)

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

        foreach (var srcZone in GetComponentsInChildren<CastleUnitDropZone>(true))
        {
            if (!srcZone.HasUnit) continue;

            CastleUnitDropZone destZone = target.FindDropZoneForType(srcZone.acceptedType);
            if (destZone == null || destZone.HasUnit) continue;

            srcZone.MigrateUnitTo(destZone);
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
}