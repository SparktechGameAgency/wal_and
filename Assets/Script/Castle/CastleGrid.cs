////////////////using UnityEngine;
////////////////using UnityEngine.EventSystems;
////////////////using System.Collections.Generic;

////////////////public class CastleGrid : MonoBehaviour
////////////////{
////////////////    public static CastleGrid Instance { get; private set; }

////////////////    [Header("Grid Settings")]
////////////////    public int totalRows = 6;
////////////////    public int totalCols = 8;

////////////////    [Header("Cell Size (pixels)")]
////////////////    public float cellSize = 120f;
////////////////    public float cellSpacing = 4f;

////////////////    [Header("Prefabs")]
////////////////    public GameObject gridCellPrefab;
////////////////    public GameObject castleBlockPrefab;
////////////////    public GameObject expansionSlotPrefab;

////////////////    [Header("Starting Block Positions")]
////////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////////////    // ── Private ───────────────────────────────────────────────────
////////////////    private GridCell[,] _grid;

////////////////    private void Awake()
////////////////    {
////////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////////        Instance = this;
////////////////    }

////////////////    private void Start()
////////////////    {
////////////////        BuildGrid();
////////////////        PlaceDefaultBlocks();
////////////////        RefreshExpansionSlots();
////////////////    }

////////////////    // ── Build full grid ───────────────────────────────────────────
////////////////    private void BuildGrid()
////////////////    {
////////////////        _grid = new GridCell[totalRows, totalCols];

////////////////        float step = cellSize + cellSpacing;

////////////////        float totalWidth = totalCols * step - cellSpacing;
////////////////        float totalHeight = totalRows * step - cellSpacing;
////////////////        float startX = -totalWidth * 0.5f + cellSize * 0.5f;
////////////////        float startY = -totalHeight * 0.5f + cellSize * 0.5f;

////////////////        for (int r = 0; r < totalRows; r++)
////////////////        {
////////////////            for (int c = 0; c < totalCols; c++)
////////////////            {
////////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////////////                cellObj.name = $"Cell_{r}_{c}";

////////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////////////                rt.anchoredPosition = new Vector2(startX + c * step, startY + r * step);
////////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);

////////////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////////////                cell.Init(r, c, this);
////////////////                _grid[r, c] = cell;
////////////////            }
////////////////        }

////////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////////////    }

////////////////    // ── Place starting blocks ─────────────────────────────────────
////////////////    private void PlaceDefaultBlocks()
////////////////    {
////////////////        foreach (var pos in defaultBlockPositions)
////////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////////////    }

////////////////    // ── Place a block at row/col ──────────────────────────────────
////////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////////////    {
////////////////        if (!InBounds(row, col)) return;

////////////////        GridCell cell = _grid[row, col];
////////////////        if (cell.HasBlock) return;

////////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////////////        brt.anchoredPosition = Vector2.zero;
////////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);
////////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////////////        brt.pivot = new Vector2(0.5f, 0.5f);

////////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////////////        cell.PlaceBlock(block);

////////////////        RefreshExpansionSlots();
////////////////    }

////////////////    // ── Show expansion slots next to every block ──────────────────
////////////////    public void RefreshExpansionSlots()
////////////////    {
////////////////        Vector2Int[] dirs = {
////////////////            Vector2Int.up,
////////////////            Vector2Int.left,
////////////////            Vector2Int.right,
////////////////            Vector2Int.down
////////////////        };

////////////////        for (int r = 0; r < totalRows; r++)
////////////////        {
////////////////            for (int c = 0; c < totalCols; c++)
////////////////            {
////////////////                GridCell cell = _grid[r, c];

////////////////                if (cell.HasBlock)
////////////////                {
////////////////                    cell.HideExpansionSlot();
////////////////                    continue;
////////////////                }

////////////////                bool adjacentToBlock = false;
////////////////                foreach (var d in dirs)
////////////////                {
////////////////                    int nr = r + d.y;
////////////////                    int nc = c + d.x;
////////////////                    if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
////////////////                    {
////////////////                        adjacentToBlock = true;
////////////////                        break;
////////////////                    }
////////////////                }

////////////////                if (adjacentToBlock)
////////////////                    cell.ShowExpansionSlot(expansionSlotPrefab);
////////////////                else
////////////////                    cell.HideExpansionSlot();
////////////////            }
////////////////        }
////////////////    }

////////////////    // ── Helpers ───────────────────────────────────────────────────
////////////////    public bool InBounds(int r, int c) =>
////////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////////////    public GridCell GetCell(int r, int c) =>
////////////////        InBounds(r, c) ? _grid[r, c] : null;
////////////////}

//////////////using UnityEngine;
//////////////using System.Collections.Generic;

//////////////public class CastleGrid : MonoBehaviour
//////////////{
//////////////    // ── Singleton ─────────────────────────────────────────────────
//////////////    public static CastleGrid Instance { get; private set; }

//////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////    [Header("Grid Settings")]
//////////////    public int totalRows = 6;
//////////////    public int totalCols = 8;

//////////////    [Header("Cell Size (pixels)")]
//////////////    public float cellSize = 120f;
//////////////    public float cellSpacing = 4f;

//////////////    [Header("Prefabs")]
//////////////    public GameObject gridCellPrefab;
//////////////    public GameObject castleBlockPrefab;
//////////////    public GameObject expansionSlotPrefab;

//////////////    [Header("Starting Block Positions")]
//////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////////////    // ── Private ───────────────────────────────────────────────────
//////////////    private GridCell[,] _grid;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────
//////////////    private void Awake()
//////////////    {
//////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////////        Instance = this;
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        BuildGrid();
//////////////        PlaceDefaultBlocks();
//////////////        RefreshExpansionSlots();
//////////////    }

//////////////    // ── Build Grid ────────────────────────────────────────────────
//////////////    private void BuildGrid()
//////////////    {
//////////////        // Clear old cells
//////////////        if (_grid != null)
//////////////        {
//////////////            for (int r = 0; r < _grid.GetLength(0); r++)
//////////////                for (int c = 0; c < _grid.GetLength(1); c++)
//////////////                    if (_grid[r, c] != null)
//////////////                        Destroy(_grid[r, c].gameObject);
//////////////        }

//////////////        _grid = new GridCell[totalRows, totalCols];

//////////////        float step = cellSize + cellSpacing;

//////////////        // Centre the entire grid on this panel's pivot
//////////////        float gridW = totalCols * step - cellSpacing;
//////////////        float gridH = totalRows * step - cellSpacing;

//////////////        // Bottom-left corner position (relative to panel centre)
//////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////////////        for (int r = 0; r < totalRows; r++)
//////////////        {
//////////////            for (int c = 0; c < totalCols; c++)
//////////////            {
//////////////                // ── Instantiate as UI child ──────────────────────
//////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////////////                cellObj.name = $"Cell_{r}_{c}";

//////////////                // ── Fix RectTransform ────────────────────────────
//////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();

//////////////                // Anchor to centre of parent so anchoredPosition is
//////////////                // always relative to the panel centre — predictable!
//////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////////////                rt.anchoredPosition = new Vector2(
//////////////                    originX + c * step,
//////////////                    originY + r * step
//////////////                );

//////////////                // ── Init cell ────────────────────────────────────
//////////////                GridCell cell = cellObj.GetComponent<GridCell>();
//////////////                cell.Init(r, c, this);
//////////////                _grid[r, c] = cell;
//////////////            }
//////////////        }

//////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////////////    }

//////////////    // ── Default Blocks ────────────────────────────────────────────
//////////////    private void PlaceDefaultBlocks()
//////////////    {
//////////////        foreach (var pos in defaultBlockPositions)
//////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////////////    }

//////////////    // ── Place Block ───────────────────────────────────────────────
//////////////    //public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////////    //{
//////////////    //    if (!InBounds(row, col)) return;

//////////////    //    GridCell cell = _grid[row, col];
//////////////    //    if (cell.HasBlock) return;
//////////////    //    if (prefab == null) return;

//////////////    //    // Spawn block as child of the cell so it sits exactly on it
//////////////    //    GameObject blockObj = Instantiate(prefab, cell.transform);

//////////////    //    RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////////    //    brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////    //    brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////    //    brt.pivot = new Vector2(0.5f, 0.5f);
//////////////    //    brt.anchoredPosition = Vector2.zero;          // centre of cell
//////////////    //    brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////////    //    CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////////    //    cell.PlaceBlock(block);

//////////////    //    RefreshExpansionSlots();
//////////////    //}

//////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////////    {
//////////////        if (!InBounds(row, col))
//////////////        {
//////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////////////            return;
//////////////        }
//////////////        if (prefab == null)
//////////////        {
//////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
//////////////            return;
//////////////        }

//////////////        GridCell cell = _grid[row, col];
//////////////        if (cell.HasBlock)
//////////////        {
//////////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
//////////////            return;
//////////////        }

//////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////////        if (brt == null)
//////////////        {
//////////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
//////////////            return;
//////////////        }

//////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////        brt.pivot = new Vector2(0.5f, 0.5f);
//////////////        brt.anchoredPosition = Vector2.zero;
//////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////////        cell.PlaceBlock(block);

//////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////////////        RefreshExpansionSlots();
//////////////    }

//////////////    // ── Expansion Slots ───────────────────────────────────────────
//////////////    //public void RefreshExpansionSlots()
//////////////    //{
//////////////    //    // 4 cardinal neighbours
//////////////    //    Vector2Int[] dirs =
//////////////    //    {
//////////////    //        Vector2Int.up,
//////////////    //        Vector2Int.down,
//////////////    //        Vector2Int.left,
//////////////    //        Vector2Int.right
//////////////    //    };

//////////////    //    for (int r = 0; r < totalRows; r++)
//////////////    //    {
//////////////    //        for (int c = 0; c < totalCols; c++)
//////////////    //        {
//////////////    //            GridCell cell = _grid[r, c];

//////////////    //            // Cells with a block never show a slot
//////////////    //            if (cell.HasBlock)
//////////////    //            {
//////////////    //                cell.HideExpansionSlot();
//////////////    //                continue;
//////////////    //            }

//////////////    //            // Show slot only if at least one neighbour has a block
//////////////    //            bool nextToBlock = false;
//////////////    //            foreach (var d in dirs)
//////////////    //            {
//////////////    //                int nr = r + d.y, nc = c + d.x;
//////////////    //                if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
//////////////    //                {
//////////////    //                    nextToBlock = true;
//////////////    //                    break;
//////////////    //                }
//////////////    //            }

//////////////    //            if (nextToBlock) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////////    //            else cell.HideExpansionSlot();
//////////////    //        }
//////////////    //    }
//////////////    //}

//////////////    public void RefreshExpansionSlots()
//////////////    {
//////////////        for (int r = 0; r < totalRows; r++)
//////////////        {
//////////////            for (int c = 0; c < totalCols; c++)
//////////////            {
//////////////                GridCell cell = _grid[r, c];

//////////////                if (cell.HasBlock)
//////////////                {
//////////////                    cell.HideExpansionSlot();
//////////////                    continue;
//////////////                }

//////////////                bool shouldShow = false;

//////////////                if (r == 0 && c == 0)
//////////////                {
//////////////                    // Anchor corner — never an expansion slot
//////////////                    shouldShow = false;
//////////////                }
//////////////                else if (r == 0)
//////////////                {
//////////////                    // Bottom row — only expand RIGHT
//////////////                    // Left neighbour must have a block
//////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////////////                }
//////////////                else if (c == 0)
//////////////                {
//////////////                    // Left column — only expand UP
//////////////                    // Cell below must have a block
//////////////                    shouldShow = _grid[r - 1, c].HasBlock;
//////////////                }
//////////////                else
//////////////                {
//////////////                    // All other cells — BOTH below AND left must have blocks
//////////////                    // This enforces the staircase / half-triangle shape
//////////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
//////////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
//////////////                    shouldShow = belowHasBlock && leftHasBlock;
//////////////                }

//////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////////                else cell.HideExpansionSlot();
//////////////            }
//////////////        }
//////////////    }

//////////////    // ── Helpers ───────────────────────────────────────────────────
//////////////    public bool InBounds(int r, int c) =>
//////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////////////    public GridCell GetCell(int r, int c) =>
//////////////        InBounds(r, c) ? _grid[r, c] : null;
//////////////}

////////////using UnityEngine;
////////////using System.Collections.Generic;

////////////public class CastleGrid : MonoBehaviour
////////////{
////////////    // ── Singleton ─────────────────────────────────────────────────
////////////    public static CastleGrid Instance { get; private set; }

////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    [Header("Grid Settings")]
////////////    public int totalRows = 6;
////////////    public int totalCols = 8;

////////////    [Header("Cell Size (pixels)")]
////////////    public float cellSize = 120f;
////////////    public float cellSpacing = 4f;

////////////    [Header("Prefabs")]
////////////    public GameObject gridCellPrefab;
////////////    public GameObject castleBlockPrefab;
////////////    public GameObject expansionSlotPrefab;

////////////    [Header("Starting Block Positions")]
////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////////    // ── Private ───────────────────────────────────────────────────
////////////    private GridCell[,] _grid;

////////////    /// <summary>
////////////    /// Whether expansion slots are currently allowed to render.
////////////    /// True  → Castle Panel is active  (slots shown next to blocks).
////////////    /// False → Village Panel is active (slots hidden everywhere).
////////////    /// </summary>
////////////    private bool _expansionSlotsVisible = true;

////////////    // ── Lifecycle ─────────────────────────────────────────────────
////////////    private void Awake()
////////////    {
////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////        Instance = this;
////////////    }

////////////    private void Start()
////////////    {
////////////        BuildGrid();
////////////        PlaceDefaultBlocks();
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Public API ────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Call this whenever the active panel changes.
////////////    ///   true  → Castle Panel opened  (expansion slots visible)
////////////    ///   false → Village Panel opened (expansion slots hidden)
////////////    /// </summary>
////////////    public void SetExpansionSlotsVisible(bool visible)
////////////    {
////////////        if (_expansionSlotsVisible == visible) return; // nothing changed
////////////        _expansionSlotsVisible = visible;
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Build Grid ────────────────────────────────────────────────
////////////    private void BuildGrid()
////////////    {
////////////        // Clear any old cells
////////////        if (_grid != null)
////////////        {
////////////            for (int r = 0; r < _grid.GetLength(0); r++)
////////////                for (int c = 0; c < _grid.GetLength(1); c++)
////////////                    if (_grid[r, c] != null)
////////////                        Destroy(_grid[r, c].gameObject);
////////////        }

////////////        _grid = new GridCell[totalRows, totalCols];

////////////        float step = cellSize + cellSpacing;
////////////        float gridW = totalCols * step - cellSpacing;
////////////        float gridH = totalRows * step - cellSpacing;

////////////        // Bottom-left corner of the grid (relative to panel centre)
////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////////////        for (int r = 0; r < totalRows; r++)
////////////        {
////////////            for (int c = 0; c < totalCols; c++)
////////////            {
////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////////                cellObj.name = $"Cell_{r}_{c}";

////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////////////                rt.anchoredPosition = new Vector2(
////////////                    originX + c * step,
////////////                    originY + r * step
////////////                );

////////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////////                cell.Init(r, c, this);
////////////                _grid[r, c] = cell;
////////////            }
////////////        }

////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////////    }

////////////    // ── Default Blocks ────────────────────────────────────────────
////////////    private void PlaceDefaultBlocks()
////////////    {
////////////        foreach (var pos in defaultBlockPositions)
////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////////    }

////////////    // ── Place Block ───────────────────────────────────────────────
////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////////    {
////////////        if (!InBounds(row, col))
////////////        {
////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////////////            return;
////////////        }
////////////        if (prefab == null)
////////////        {
////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
////////////            return;
////////////        }

////////////        GridCell cell = _grid[row, col];
////////////        if (cell.HasBlock)
////////////        {
////////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
////////////            return;
////////////        }

////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////////        if (brt == null)
////////////        {
////////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
////////////            return;
////////////        }

////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////////        brt.pivot = new Vector2(0.5f, 0.5f);
////////////        brt.anchoredPosition = Vector2.zero;
////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////////        cell.PlaceBlock(block);

////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Expansion Slots ───────────────────────────────────────────

////////////    /// <summary>
////////////    /// Recalculates which cells should show an expansion slot.
////////////    /// When _expansionSlotsVisible is false (Village Panel), every
////////////    /// slot is hidden regardless of adjacency.
////////////    /// </summary>
////////////    public void RefreshExpansionSlots()
////////////    {
////////////        for (int r = 0; r < totalRows; r++)
////////////        {
////////////            for (int c = 0; c < totalCols; c++)
////////////            {
////////////                GridCell cell = _grid[r, c];

////////////                // Cells that already hold a block never show a slot
////////////                if (cell.HasBlock)
////////////                {
////////////                    cell.HideExpansionSlot();
////////////                    continue;
////////////                }

////////////                // ── Village Panel: hide all expansion slots ────────
////////////                if (!_expansionSlotsVisible)
////////////                {
////////////                    cell.HideExpansionSlot();
////////////                    continue;
////////////                }

////////////                // ── Castle Panel: staircase / half-triangle rule ───
////////////                bool shouldShow = false;

////////////                if (r == 0 && c == 0)
////////////                {
////////////                    // Anchor corner — never an expansion slot
////////////                    shouldShow = false;
////////////                }
////////////                else if (r == 0)
////////////                {
////////////                    // Bottom row — only expand right; left neighbour must have a block
////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////////////                }
////////////                else if (c == 0)
////////////                {
////////////                    // Left column — only expand up; cell below must have a block
////////////                    shouldShow = _grid[r - 1, c].HasBlock;
////////////                }
////////////                else
////////////                {
////////////                    // All other cells — both below AND left must have blocks
////////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
////////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
////////////                    shouldShow = belowHasBlock && leftHasBlock;
////////////                }

////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////////////                else cell.HideExpansionSlot();
////////////            }
////////////        }
////////////    }

////////////    // ── Helpers ───────────────────────────────────────────────────
////////////    public bool InBounds(int r, int c) =>
////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////////    public GridCell GetCell(int r, int c) =>
////////////        InBounds(r, c) ? _grid[r, c] : null;
////////////}

//////////using UnityEngine;
//////////using System.Collections.Generic;

//////////public class CastleGrid : MonoBehaviour
//////////{
//////////    // ── Singleton ─────────────────────────────────────────────────
//////////    public static CastleGrid Instance { get; private set; }

//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    [Header("Grid Settings")]
//////////    public int totalRows = 6;
//////////    public int totalCols = 8;

//////////    [Header("Cell Size (pixels)")]
//////////    public float cellSize = 120f;
//////////    public float cellSpacing = 4f;

//////////    [Header("Prefabs")]
//////////    public GameObject gridCellPrefab;
//////////    public GameObject castleBlockPrefab;
//////////    public GameObject expansionSlotPrefab;

//////////    [Header("Starting Block Positions")]
//////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////////    // ── Private ───────────────────────────────────────────────────
//////////    private GridCell[,] _grid;

//////////    /// <summary>
//////////    /// Whether expansion slots are currently allowed to render.
//////////    /// True  → Castle Panel is active  (slots shown next to blocks).
//////////    /// False → Village Panel is active (slots hidden everywhere).
//////////    /// </summary>
//////////    private bool _expansionSlotsVisible = true;

//////////    // ── Lifecycle ─────────────────────────────────────────────────
//////////    private void Awake()
//////////    {
//////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////        Instance = this;
//////////    }

//////////    private void Start()
//////////    {
//////////        BuildGrid();
//////////        PlaceDefaultBlocks();
//////////        RefreshExpansionSlots();
//////////    }

//////////    // ── Public API ────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Call this whenever the active panel changes.
//////////    ///   true  → Castle Panel opened  (expansion slots visible)
//////////    ///   false → Village Panel opened (expansion slots hidden)
//////////    /// </summary>
//////////    public void SetExpansionSlotsVisible(bool visible)
//////////    {
//////////        if (_expansionSlotsVisible == visible) return; // nothing changed
//////////        _expansionSlotsVisible = visible;
//////////        RefreshExpansionSlots();
//////////    }

//////////    // ── Build Grid ────────────────────────────────────────────────
//////////    private void BuildGrid()
//////////    {
//////////        // Clear any old cells
//////////        if (_grid != null)
//////////        {
//////////            for (int r = 0; r < _grid.GetLength(0); r++)
//////////                for (int c = 0; c < _grid.GetLength(1); c++)
//////////                    if (_grid[r, c] != null)
//////////                        Destroy(_grid[r, c].gameObject);
//////////        }

//////////        _grid = new GridCell[totalRows, totalCols];

//////////        float step = cellSize + cellSpacing;
//////////        float gridW = totalCols * step - cellSpacing;
//////////        float gridH = totalRows * step - cellSpacing;

//////////        // Bottom-left corner of the grid (relative to panel centre)
//////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////////        for (int r = 0; r < totalRows; r++)
//////////        {
//////////            for (int c = 0; c < totalCols; c++)
//////////            {
//////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////////                cellObj.name = $"Cell_{r}_{c}";

//////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
//////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////////                rt.anchoredPosition = new Vector2(
//////////                    originX + c * step,
//////////                    originY + r * step
//////////                );

//////////                GridCell cell = cellObj.GetComponent<GridCell>();
//////////                cell.Init(r, c, this);
//////////                _grid[r, c] = cell;
//////////            }
//////////        }

//////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////////    }

//////////    // ── Default Blocks ────────────────────────────────────────────
//////////    private void PlaceDefaultBlocks()
//////////    {
//////////        foreach (var pos in defaultBlockPositions)
//////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////////    }

//////////    // ── Place Block ───────────────────────────────────────────────
//////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////    {
//////////        if (!InBounds(row, col))
//////////        {
//////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////////            return;
//////////        }
//////////        if (prefab == null)
//////////        {
//////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
//////////            return;
//////////        }

//////////        GridCell cell = _grid[row, col];
//////////        if (cell.HasBlock)
//////////        {
//////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
//////////            return;
//////////        }

//////////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////        if (brt == null)
//////////        {
//////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
//////////            return;
//////////        }

//////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////        brt.pivot = new Vector2(0.5f, 0.5f);
//////////        brt.anchoredPosition = Vector2.zero;
//////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////        cell.PlaceBlock(block);

//////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////////        RefreshExpansionSlots();
//////////    }

//////////    // ── Expansion Slots ───────────────────────────────────────────

//////////    /// <summary>
//////////    /// Recalculates which cells should show an expansion slot.
//////////    /// When _expansionSlotsVisible is false (Village Panel), every
//////////    /// slot is hidden regardless of adjacency.
//////////    /// </summary>
//////////    public void RefreshExpansionSlots()
//////////    {
//////////        for (int r = 0; r < totalRows; r++)
//////////        {
//////////            for (int c = 0; c < totalCols; c++)
//////////            {
//////////                GridCell cell = _grid[r, c];

//////////                // Cells that already hold a block never show a slot
//////////                if (cell.HasBlock)
//////////                {
//////////                    cell.HideExpansionSlot();
//////////                    continue;
//////////                }

//////////                // ── Village Panel: hide all expansion slots ────────
//////////                if (!_expansionSlotsVisible)
//////////                {
//////////                    cell.HideExpansionSlot();
//////////                    continue;
//////////                }

//////////                // ── Castle Panel: staircase / half-triangle rule ───
//////////                bool shouldShow = false;

//////////                if (r == 0 && c == 0)
//////////                {
//////////                    // Anchor corner — never an expansion slot
//////////                    shouldShow = false;
//////////                }
//////////                else if (r == 0)
//////////                {
//////////                    // Bottom row — only expand right; left neighbour must have a block
//////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////////                }
//////////                else if (c == 0)
//////////                {
//////////                    // Left column — only expand up; cell below must have a block
//////////                    shouldShow = _grid[r - 1, c].HasBlock;
//////////                }
//////////                else
//////////                {
//////////                    // All other cells — both below AND left must have blocks
//////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
//////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
//////////                    shouldShow = belowHasBlock && leftHasBlock;
//////////                }

//////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////                else cell.HideExpansionSlot();
//////////            }
//////////        }
//////////    }

//////////    // ── Helpers ───────────────────────────────────────────────────
//////////    public bool InBounds(int r, int c) =>
//////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////////    public GridCell GetCell(int r, int c) =>
//////////        InBounds(r, c) ? _grid[r, c] : null;
//////////}

////////using UnityEngine;
////////using System.Collections.Generic;

////////public class CastleGrid : MonoBehaviour
////////{
////////    // ── Singleton ─────────────────────────────────────────────────
////////    public static CastleGrid Instance { get; private set; }

////////    // ── Inspector ─────────────────────────────────────────────────
////////    [Header("Grid Settings")]
////////    public int totalRows = 6;
////////    public int totalCols = 8;

////////    [Header("Cell Size (pixels)")]
////////    public float cellSize = 120f;
////////    public float cellSpacing = 4f;

////////    [Header("Prefabs")]
////////    public GameObject gridCellPrefab;
////////    public GameObject castleBlockPrefab;
////////    public GameObject expansionSlotPrefab;

////////    [Header("Starting Block Positions")]
////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////    // ── Private ───────────────────────────────────────────────────
////////    private GridCell[,] _grid;

////////    /// <summary>
////////    /// Expansion slots are hidden by default (Village Panel is shown first).
////////    /// UIManager calls SetExpansionSlotsVisible(true) when Castle Panel opens.
////////    /// </summary>
////////    private bool _expansionSlotsVisible = false;

////////    // ── Lifecycle ─────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;

////////        // Build grid in Awake so _grid is ready before any other script's Start()
////////        BuildGrid();
////////        PlaceDefaultBlocks();
////////    }

////////    private void Start()
////////    {
////////        // Slots start hidden (village view)
////////        RefreshExpansionSlots();
////////    }

////////    // ── Public API ────────────────────────────────────────────────

////////    /// <summary>
////////    /// Call when switching panels:
////////    ///   true  → Castle Panel opened  (show expansion slots)
////////    ///   false → Village Panel shown  (hide expansion slots)
////////    /// </summary>
////////    public void SetExpansionSlotsVisible(bool visible)
////////    {
////////        if (_expansionSlotsVisible == visible) return;
////////        _expansionSlotsVisible = visible;
////////        RefreshExpansionSlots();
////////    }

////////    // ── Build Grid ────────────────────────────────────────────────
////////    private void BuildGrid()
////////    {
////////        _grid = new GridCell[totalRows, totalCols];

////////        float step = cellSize + cellSpacing;
////////        float gridW = totalCols * step - cellSpacing;
////////        float gridH = totalRows * step - cellSpacing;
////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////////        for (int r = 0; r < totalRows; r++)
////////        {
////////            for (int c = 0; c < totalCols; c++)
////////            {
////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////                cellObj.name = $"Cell_{r}_{c}";

////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////                cell.Init(r, c, this);
////////                _grid[r, c] = cell;
////////            }
////////        }

////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////    }

////////    // ── Default Blocks ────────────────────────────────────────────
////////    private void PlaceDefaultBlocks()
////////    {
////////        foreach (var pos in defaultBlockPositions)
////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////    }

////////    // ── Place Block ───────────────────────────────────────────────
////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////    {
////////        if (!InBounds(row, col))
////////        {
////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////////            return;
////////        }
////////        if (prefab == null)
////////        {
////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
////////            return;
////////        }

////////        GridCell cell = _grid[row, col];
////////        if (cell.HasBlock) return;

////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////        if (brt == null) return;

////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////        brt.pivot = new Vector2(0.5f, 0.5f);
////////        brt.anchoredPosition = Vector2.zero;
////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////        cell.PlaceBlock(block);

////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
////////        RefreshExpansionSlots();
////////    }

////////    // ── Expansion Slots ───────────────────────────────────────────
////////    public void RefreshExpansionSlots()
////////    {
////////        // Guard: grid not built yet
////////        if (_grid == null) return;

////////        for (int r = 0; r < totalRows; r++)
////////        {
////////            for (int c = 0; c < totalCols; c++)
////////            {
////////                GridCell cell = _grid[r, c];

////////                if (cell.HasBlock)
////////                {
////////                    cell.HideExpansionSlot();
////////                    continue;
////////                }

////////                // Always hide when in Village Panel
////////                if (!_expansionSlotsVisible)
////////                {
////////                    cell.HideExpansionSlot();
////////                    continue;
////////                }

////////                // Castle Panel — staircase rule
////////                bool shouldShow;

////////                if (r == 0 && c == 0)
////////                    shouldShow = false;
////////                else if (r == 0)
////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////////                else if (c == 0)
////////                    shouldShow = _grid[r - 1, c].HasBlock;
////////                else
////////                    shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////////                else cell.HideExpansionSlot();
////////            }
////////        }
////////    }

////////    // ── Helpers ───────────────────────────────────────────────────
////////    public bool InBounds(int r, int c) =>
////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////    public GridCell GetCell(int r, int c) =>
////////        InBounds(r, c) ? _grid[r, c] : null;
////////}

//////using UnityEngine;
//////using System.Collections.Generic;

//////public class CastleGrid : MonoBehaviour
//////{
//////    // ── Singleton ─────────────────────────────────────────────────
//////    public static CastleGrid Instance { get; private set; }

//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Header("Grid Settings")]
//////    public int totalRows = 6;
//////    public int totalCols = 8;

//////    [Header("Cell Size (pixels)")]
//////    public float cellSize = 120f;
//////    public float cellSpacing = 4f;

//////    [Header("Prefabs")]
//////    public GameObject gridCellPrefab;
//////    public GameObject castleBlockPrefab;
//////    public GameObject expansionSlotPrefab;

//////    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone + SoldierZone children). " +
//////             "Placed on top of every exposed block (no block above it).")]
//////    public GameObject castleBlockUnitSlotPrefab;

//////    [Header("Starting Block Positions")]
//////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////    // ── Private ───────────────────────────────────────────────────
//////    private GridCell[,] _grid;

//////    /// <summary>
//////    /// false = Village Panel visible → all slots hidden.
//////    /// true  = Castle Panel visible  → slots shown as appropriate.
//////    /// </summary>
//////    private bool _expansionSlotsVisible = false;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;

//////        BuildGrid();
//////        PlaceDefaultBlocks();
//////    }

//////    private void Start()
//////    {
//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    /// <summary>
//////    /// true  → Castle Panel open  (show expansion + unit slots)
//////    /// false → Village Panel open (hide all slots)
//////    /// </summary>
//////    public void SetExpansionSlotsVisible(bool visible)
//////    {
//////        if (_expansionSlotsVisible == visible) return;
//////        _expansionSlotsVisible = visible;
//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Build Grid ────────────────────────────────────────────────
//////    private void BuildGrid()
//////    {
//////        _grid = new GridCell[totalRows, totalCols];

//////        float step = cellSize + cellSpacing;
//////        float gridW = totalCols * step - cellSpacing;
//////        float gridH = totalRows * step - cellSpacing;
//////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////                cellObj.name = $"Cell_{r}_{c}";

//////                RectTransform rt = cellObj.GetComponent<RectTransform>();
//////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////                rt.pivot = new Vector2(0.5f, 0.5f);
//////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

//////                GridCell cell = cellObj.GetComponent<GridCell>();
//////                cell.Init(r, c, this);
//////                _grid[r, c] = cell;
//////            }
//////        }

//////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////    }

//////    // ── Default Blocks ────────────────────────────────────────────
//////    private void PlaceDefaultBlocks()
//////    {
//////        foreach (var pos in defaultBlockPositions)
//////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////    }

//////    // ── Place Block ───────────────────────────────────────────────
//////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////    {
//////        if (!InBounds(row, col))
//////        {
//////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////            return;
//////        }
//////        if (prefab == null)
//////        {
//////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
//////            return;
//////        }

//////        GridCell cell = _grid[row, col];
//////        if (cell.HasBlock) return;

//////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////        if (brt == null) return;

//////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////        brt.pivot = new Vector2(0.5f, 0.5f);
//////        brt.anchoredPosition = Vector2.zero;
//////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////        cell.PlaceBlock(block);

//////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Expansion Slots ───────────────────────────────────────────
//////    public void RefreshExpansionSlots()
//////    {
//////        if (_grid == null) return;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GridCell cell = _grid[r, c];

//////                if (cell.HasBlock || !_expansionSlotsVisible)
//////                {
//////                    cell.HideExpansionSlot();
//////                    continue;
//////                }

//////                // Staircase rule
//////                bool shouldShow;
//////                if (r == 0 && c == 0) shouldShow = false;
//////                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
//////                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

//////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////                else cell.HideExpansionSlot();
//////            }
//////        }
//////    }

//////    // ── Unit Slots ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Shows a cannon+soldier drop overlay on every block that has nothing above it
//////    /// (the "exposed" top-edge blocks of the staircase).
//////    /// Completely hidden when the Village Panel is active.
//////    /// </summary>
//////    public void RefreshUnitSlots()
//////    {
//////        if (_grid == null) return;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GridCell cell = _grid[r, c];

//////                // Only blocks visible in the Castle Panel get unit slots
//////                if (!cell.HasBlock || !_expansionSlotsVisible)
//////                {
//////                    cell.HideUnitSlot();
//////                    continue;
//////                }

//////                // "Exposed" = the cell directly above this one has no block
//////                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

//////                if (isExposed && castleBlockUnitSlotPrefab != null)
//////                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);
//////                else
//////                    cell.HideUnitSlot();
//////            }
//////        }
//////    }

//////    // ── Helpers ───────────────────────────────────────────────────
//////    public bool InBounds(int r, int c) =>
//////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////    public GridCell GetCell(int r, int c) =>
//////        InBounds(r, c) ? _grid[r, c] : null;
//////}


////////////////////using UnityEngine;
////////////////////using UnityEngine.EventSystems;
////////////////////using System.Collections.Generic;

////////////////////public class CastleGrid : MonoBehaviour
////////////////////{
////////////////////    public static CastleGrid Instance { get; private set; }

////////////////////    [Header("Grid Settings")]
////////////////////    public int totalRows = 6;
////////////////////    public int totalCols = 8;

////////////////////    [Header("Cell Size (pixels)")]
////////////////////    public float cellSize = 120f;
////////////////////    public float cellSpacing = 4f;

////////////////////    [Header("Prefabs")]
////////////////////    public GameObject gridCellPrefab;
////////////////////    public GameObject castleBlockPrefab;
////////////////////    public GameObject expansionSlotPrefab;

////////////////////    [Header("Starting Block Positions")]
////////////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////////////////    // ── Private ───────────────────────────────────────────────────
////////////////////    private GridCell[,] _grid;

////////////////////    private void Awake()
////////////////////    {
////////////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////////////        Instance = this;
////////////////////    }

////////////////////    private void Start()
////////////////////    {
////////////////////        BuildGrid();
////////////////////        PlaceDefaultBlocks();
////////////////////        RefreshExpansionSlots();
////////////////////    }

////////////////////    // ── Build full grid ───────────────────────────────────────────
////////////////////    private void BuildGrid()
////////////////////    {
////////////////////        _grid = new GridCell[totalRows, totalCols];

////////////////////        float step = cellSize + cellSpacing;

////////////////////        float totalWidth = totalCols * step - cellSpacing;
////////////////////        float totalHeight = totalRows * step - cellSpacing;
////////////////////        float startX = -totalWidth * 0.5f + cellSize * 0.5f;
////////////////////        float startY = -totalHeight * 0.5f + cellSize * 0.5f;

////////////////////        for (int r = 0; r < totalRows; r++)
////////////////////        {
////////////////////            for (int c = 0; c < totalCols; c++)
////////////////////            {
////////////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////////////////                cellObj.name = $"Cell_{r}_{c}";

////////////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////////////////                rt.anchoredPosition = new Vector2(startX + c * step, startY + r * step);
////////////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);

////////////////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////////////////                cell.Init(r, c, this);
////////////////////                _grid[r, c] = cell;
////////////////////            }
////////////////////        }

////////////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////////////////    }

////////////////////    // ── Place starting blocks ─────────────────────────────────────
////////////////////    private void PlaceDefaultBlocks()
////////////////////    {
////////////////////        foreach (var pos in defaultBlockPositions)
////////////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////////////////    }

////////////////////    // ── Place a block at row/col ──────────────────────────────────
////////////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////////////////    {
////////////////////        if (!InBounds(row, col)) return;

////////////////////        GridCell cell = _grid[row, col];
////////////////////        if (cell.HasBlock) return;

////////////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////////////////        brt.anchoredPosition = Vector2.zero;
////////////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);
////////////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////////////////        brt.pivot = new Vector2(0.5f, 0.5f);

////////////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////////////////        cell.PlaceBlock(block);

////////////////////        RefreshExpansionSlots();
////////////////////    }

////////////////////    // ── Show expansion slots next to every block ──────────────────
////////////////////    public void RefreshExpansionSlots()
////////////////////    {
////////////////////        Vector2Int[] dirs = {
////////////////////            Vector2Int.up,
////////////////////            Vector2Int.left,
////////////////////            Vector2Int.right,
////////////////////            Vector2Int.down
////////////////////        };

////////////////////        for (int r = 0; r < totalRows; r++)
////////////////////        {
////////////////////            for (int c = 0; c < totalCols; c++)
////////////////////            {
////////////////////                GridCell cell = _grid[r, c];

////////////////////                if (cell.HasBlock)
////////////////////                {
////////////////////                    cell.HideExpansionSlot();
////////////////////                    continue;
////////////////////                }

////////////////////                bool adjacentToBlock = false;
////////////////////                foreach (var d in dirs)
////////////////////                {
////////////////////                    int nr = r + d.y;
////////////////////                    int nc = c + d.x;
////////////////////                    if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
////////////////////                    {
////////////////////                        adjacentToBlock = true;
////////////////////                        break;
////////////////////                    }
////////////////////                }

////////////////////                if (adjacentToBlock)
////////////////////                    cell.ShowExpansionSlot(expansionSlotPrefab);
////////////////////                else
////////////////////                    cell.HideExpansionSlot();
////////////////////            }
////////////////////        }
////////////////////    }

////////////////////    // ── Helpers ───────────────────────────────────────────────────
////////////////////    public bool InBounds(int r, int c) =>
////////////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////////////////    public GridCell GetCell(int r, int c) =>
////////////////////        InBounds(r, c) ? _grid[r, c] : null;
////////////////////}

//////////////////using UnityEngine;
//////////////////using System.Collections.Generic;

//////////////////public class CastleGrid : MonoBehaviour
//////////////////{
//////////////////    // ── Singleton ─────────────────────────────────────────────────
//////////////////    public static CastleGrid Instance { get; private set; }

//////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////    [Header("Grid Settings")]
//////////////////    public int totalRows = 6;
//////////////////    public int totalCols = 8;

//////////////////    [Header("Cell Size (pixels)")]
//////////////////    public float cellSize = 120f;
//////////////////    public float cellSpacing = 4f;

//////////////////    [Header("Prefabs")]
//////////////////    public GameObject gridCellPrefab;
//////////////////    public GameObject castleBlockPrefab;
//////////////////    public GameObject expansionSlotPrefab;

//////////////////    [Header("Starting Block Positions")]
//////////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////////////////    // ── Private ───────────────────────────────────────────────────
//////////////////    private GridCell[,] _grid;

//////////////////    // ── Lifecycle ─────────────────────────────────────────────────
//////////////////    private void Awake()
//////////////////    {
//////////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////////////        Instance = this;
//////////////////    }

//////////////////    private void Start()
//////////////////    {
//////////////////        BuildGrid();
//////////////////        PlaceDefaultBlocks();
//////////////////        RefreshExpansionSlots();
//////////////////    }

//////////////////    // ── Build Grid ────────────────────────────────────────────────
//////////////////    private void BuildGrid()
//////////////////    {
//////////////////        // Clear old cells
//////////////////        if (_grid != null)
//////////////////        {
//////////////////            for (int r = 0; r < _grid.GetLength(0); r++)
//////////////////                for (int c = 0; c < _grid.GetLength(1); c++)
//////////////////                    if (_grid[r, c] != null)
//////////////////                        Destroy(_grid[r, c].gameObject);
//////////////////        }

//////////////////        _grid = new GridCell[totalRows, totalCols];

//////////////////        float step = cellSize + cellSpacing;

//////////////////        // Centre the entire grid on this panel's pivot
//////////////////        float gridW = totalCols * step - cellSpacing;
//////////////////        float gridH = totalRows * step - cellSpacing;

//////////////////        // Bottom-left corner position (relative to panel centre)
//////////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////////////////        for (int r = 0; r < totalRows; r++)
//////////////////        {
//////////////////            for (int c = 0; c < totalCols; c++)
//////////////////            {
//////////////////                // ── Instantiate as UI child ──────────────────────
//////////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////////////////                cellObj.name = $"Cell_{r}_{c}";

//////////////////                // ── Fix RectTransform ────────────────────────────
//////////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();

//////////////////                // Anchor to centre of parent so anchoredPosition is
//////////////////                // always relative to the panel centre — predictable!
//////////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////////////////                rt.anchoredPosition = new Vector2(
//////////////////                    originX + c * step,
//////////////////                    originY + r * step
//////////////////                );

//////////////////                // ── Init cell ────────────────────────────────────
//////////////////                GridCell cell = cellObj.GetComponent<GridCell>();
//////////////////                cell.Init(r, c, this);
//////////////////                _grid[r, c] = cell;
//////////////////            }
//////////////////        }

//////////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////////////////    }

//////////////////    // ── Default Blocks ────────────────────────────────────────────
//////////////////    private void PlaceDefaultBlocks()
//////////////////    {
//////////////////        foreach (var pos in defaultBlockPositions)
//////////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////////////////    }

//////////////////    // ── Place Block ───────────────────────────────────────────────
//////////////////    //public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////////////    //{
//////////////////    //    if (!InBounds(row, col)) return;

//////////////////    //    GridCell cell = _grid[row, col];
//////////////////    //    if (cell.HasBlock) return;
//////////////////    //    if (prefab == null) return;

//////////////////    //    // Spawn block as child of the cell so it sits exactly on it
//////////////////    //    GameObject blockObj = Instantiate(prefab, cell.transform);

//////////////////    //    RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////////////    //    brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////////    //    brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////////    //    brt.pivot = new Vector2(0.5f, 0.5f);
//////////////////    //    brt.anchoredPosition = Vector2.zero;          // centre of cell
//////////////////    //    brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////////////    //    CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////////////    //    cell.PlaceBlock(block);

//////////////////    //    RefreshExpansionSlots();
//////////////////    //}

//////////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////////////    {
//////////////////        if (!InBounds(row, col))
//////////////////        {
//////////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////////////////            return;
//////////////////        }
//////////////////        if (prefab == null)
//////////////////        {
//////////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
//////////////////            return;
//////////////////        }

//////////////////        GridCell cell = _grid[row, col];
//////////////////        if (cell.HasBlock)
//////////////////        {
//////////////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
//////////////////            return;
//////////////////        }

//////////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////////////        if (brt == null)
//////////////////        {
//////////////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
//////////////////            return;
//////////////////        }

//////////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////////        brt.pivot = new Vector2(0.5f, 0.5f);
//////////////////        brt.anchoredPosition = Vector2.zero;
//////////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////////////        cell.PlaceBlock(block);

//////////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////////////////        RefreshExpansionSlots();
//////////////////    }

//////////////////    // ── Expansion Slots ───────────────────────────────────────────
//////////////////    //public void RefreshExpansionSlots()
//////////////////    //{
//////////////////    //    // 4 cardinal neighbours
//////////////////    //    Vector2Int[] dirs =
//////////////////    //    {
//////////////////    //        Vector2Int.up,
//////////////////    //        Vector2Int.down,
//////////////////    //        Vector2Int.left,
//////////////////    //        Vector2Int.right
//////////////////    //    };

//////////////////    //    for (int r = 0; r < totalRows; r++)
//////////////////    //    {
//////////////////    //        for (int c = 0; c < totalCols; c++)
//////////////////    //        {
//////////////////    //            GridCell cell = _grid[r, c];

//////////////////    //            // Cells with a block never show a slot
//////////////////    //            if (cell.HasBlock)
//////////////////    //            {
//////////////////    //                cell.HideExpansionSlot();
//////////////////    //                continue;
//////////////////    //            }

//////////////////    //            // Show slot only if at least one neighbour has a block
//////////////////    //            bool nextToBlock = false;
//////////////////    //            foreach (var d in dirs)
//////////////////    //            {
//////////////////    //                int nr = r + d.y, nc = c + d.x;
//////////////////    //                if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
//////////////////    //                {
//////////////////    //                    nextToBlock = true;
//////////////////    //                    break;
//////////////////    //                }
//////////////////    //            }

//////////////////    //            if (nextToBlock) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////////////    //            else cell.HideExpansionSlot();
//////////////////    //        }
//////////////////    //    }
//////////////////    //}

//////////////////    public void RefreshExpansionSlots()
//////////////////    {
//////////////////        for (int r = 0; r < totalRows; r++)
//////////////////        {
//////////////////            for (int c = 0; c < totalCols; c++)
//////////////////            {
//////////////////                GridCell cell = _grid[r, c];

//////////////////                if (cell.HasBlock)
//////////////////                {
//////////////////                    cell.HideExpansionSlot();
//////////////////                    continue;
//////////////////                }

//////////////////                bool shouldShow = false;

//////////////////                if (r == 0 && c == 0)
//////////////////                {
//////////////////                    // Anchor corner — never an expansion slot
//////////////////                    shouldShow = false;
//////////////////                }
//////////////////                else if (r == 0)
//////////////////                {
//////////////////                    // Bottom row — only expand RIGHT
//////////////////                    // Left neighbour must have a block
//////////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////////////////                }
//////////////////                else if (c == 0)
//////////////////                {
//////////////////                    // Left column — only expand UP
//////////////////                    // Cell below must have a block
//////////////////                    shouldShow = _grid[r - 1, c].HasBlock;
//////////////////                }
//////////////////                else
//////////////////                {
//////////////////                    // All other cells — BOTH below AND left must have blocks
//////////////////                    // This enforces the staircase / half-triangle shape
//////////////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
//////////////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
//////////////////                    shouldShow = belowHasBlock && leftHasBlock;
//////////////////                }

//////////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////////////                else cell.HideExpansionSlot();
//////////////////            }
//////////////////        }
//////////////////    }

//////////////////    // ── Helpers ───────────────────────────────────────────────────
//////////////////    public bool InBounds(int r, int c) =>
//////////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////////////////    public GridCell GetCell(int r, int c) =>
//////////////////        InBounds(r, c) ? _grid[r, c] : null;
//////////////////}

////////////////using UnityEngine;
////////////////using System.Collections.Generic;

////////////////public class CastleGrid : MonoBehaviour
////////////////{
////////////////    // ── Singleton ─────────────────────────────────────────────────
////////////////    public static CastleGrid Instance { get; private set; }

////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////    [Header("Grid Settings")]
////////////////    public int totalRows = 6;
////////////////    public int totalCols = 8;

////////////////    [Header("Cell Size (pixels)")]
////////////////    public float cellSize = 120f;
////////////////    public float cellSpacing = 4f;

////////////////    [Header("Prefabs")]
////////////////    public GameObject gridCellPrefab;
////////////////    public GameObject castleBlockPrefab;
////////////////    public GameObject expansionSlotPrefab;

////////////////    [Header("Starting Block Positions")]
////////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////////////    // ── Private ───────────────────────────────────────────────────
////////////////    private GridCell[,] _grid;

////////////////    /// <summary>
////////////////    /// Whether expansion slots are currently allowed to render.
////////////////    /// True  → Castle Panel is active  (slots shown next to blocks).
////////////////    /// False → Village Panel is active (slots hidden everywhere).
////////////////    /// </summary>
////////////////    private bool _expansionSlotsVisible = true;

////////////////    // ── Lifecycle ─────────────────────────────────────────────────
////////////////    private void Awake()
////////////////    {
////////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////////        Instance = this;
////////////////    }

////////////////    private void Start()
////////////////    {
////////////////        BuildGrid();
////////////////        PlaceDefaultBlocks();
////////////////        RefreshExpansionSlots();
////////////////    }

////////////////    // ── Public API ────────────────────────────────────────────────

////////////////    /// <summary>
////////////////    /// Call this whenever the active panel changes.
////////////////    ///   true  → Castle Panel opened  (expansion slots visible)
////////////////    ///   false → Village Panel opened (expansion slots hidden)
////////////////    /// </summary>
////////////////    public void SetExpansionSlotsVisible(bool visible)
////////////////    {
////////////////        if (_expansionSlotsVisible == visible) return; // nothing changed
////////////////        _expansionSlotsVisible = visible;
////////////////        RefreshExpansionSlots();
////////////////    }

////////////////    // ── Build Grid ────────────────────────────────────────────────
////////////////    private void BuildGrid()
////////////////    {
////////////////        // Clear any old cells
////////////////        if (_grid != null)
////////////////        {
////////////////            for (int r = 0; r < _grid.GetLength(0); r++)
////////////////                for (int c = 0; c < _grid.GetLength(1); c++)
////////////////                    if (_grid[r, c] != null)
////////////////                        Destroy(_grid[r, c].gameObject);
////////////////        }

////////////////        _grid = new GridCell[totalRows, totalCols];

////////////////        float step = cellSize + cellSpacing;
////////////////        float gridW = totalCols * step - cellSpacing;
////////////////        float gridH = totalRows * step - cellSpacing;

////////////////        // Bottom-left corner of the grid (relative to panel centre)
////////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////////////////        for (int r = 0; r < totalRows; r++)
////////////////        {
////////////////            for (int c = 0; c < totalCols; c++)
////////////////            {
////////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////////////                cellObj.name = $"Cell_{r}_{c}";

////////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////////////////                rt.anchoredPosition = new Vector2(
////////////////                    originX + c * step,
////////////////                    originY + r * step
////////////////                );

////////////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////////////                cell.Init(r, c, this);
////////////////                _grid[r, c] = cell;
////////////////            }
////////////////        }

////////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////////////    }

////////////////    // ── Default Blocks ────────────────────────────────────────────
////////////////    private void PlaceDefaultBlocks()
////////////////    {
////////////////        foreach (var pos in defaultBlockPositions)
////////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////////////    }

////////////////    // ── Place Block ───────────────────────────────────────────────
////////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////////////    {
////////////////        if (!InBounds(row, col))
////////////////        {
////////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////////////////            return;
////////////////        }
////////////////        if (prefab == null)
////////////////        {
////////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
////////////////            return;
////////////////        }

////////////////        GridCell cell = _grid[row, col];
////////////////        if (cell.HasBlock)
////////////////        {
////////////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
////////////////            return;
////////////////        }

////////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////////////        if (brt == null)
////////////////        {
////////////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
////////////////            return;
////////////////        }

////////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////////////        brt.pivot = new Vector2(0.5f, 0.5f);
////////////////        brt.anchoredPosition = Vector2.zero;
////////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////////////        cell.PlaceBlock(block);

////////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
////////////////        RefreshExpansionSlots();
////////////////    }

////////////////    // ── Expansion Slots ───────────────────────────────────────────

////////////////    /// <summary>
////////////////    /// Recalculates which cells should show an expansion slot.
////////////////    /// When _expansionSlotsVisible is false (Village Panel), every
////////////////    /// slot is hidden regardless of adjacency.
////////////////    /// </summary>
////////////////    public void RefreshExpansionSlots()
////////////////    {
////////////////        for (int r = 0; r < totalRows; r++)
////////////////        {
////////////////            for (int c = 0; c < totalCols; c++)
////////////////            {
////////////////                GridCell cell = _grid[r, c];

////////////////                // Cells that already hold a block never show a slot
////////////////                if (cell.HasBlock)
////////////////                {
////////////////                    cell.HideExpansionSlot();
////////////////                    continue;
////////////////                }

////////////////                // ── Village Panel: hide all expansion slots ────────
////////////////                if (!_expansionSlotsVisible)
////////////////                {
////////////////                    cell.HideExpansionSlot();
////////////////                    continue;
////////////////                }

////////////////                // ── Castle Panel: staircase / half-triangle rule ───
////////////////                bool shouldShow = false;

////////////////                if (r == 0 && c == 0)
////////////////                {
////////////////                    // Anchor corner — never an expansion slot
////////////////                    shouldShow = false;
////////////////                }
////////////////                else if (r == 0)
////////////////                {
////////////////                    // Bottom row — only expand right; left neighbour must have a block
////////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////////////////                }
////////////////                else if (c == 0)
////////////////                {
////////////////                    // Left column — only expand up; cell below must have a block
////////////////                    shouldShow = _grid[r - 1, c].HasBlock;
////////////////                }
////////////////                else
////////////////                {
////////////////                    // All other cells — both below AND left must have blocks
////////////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
////////////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
////////////////                    shouldShow = belowHasBlock && leftHasBlock;
////////////////                }

////////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////////////////                else cell.HideExpansionSlot();
////////////////            }
////////////////        }
////////////////    }

////////////////    // ── Helpers ───────────────────────────────────────────────────
////////////////    public bool InBounds(int r, int c) =>
////////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////////////    public GridCell GetCell(int r, int c) =>
////////////////        InBounds(r, c) ? _grid[r, c] : null;
////////////////}

//////////////using UnityEngine;
//////////////using System.Collections.Generic;

//////////////public class CastleGrid : MonoBehaviour
//////////////{
//////////////    // ── Singleton ─────────────────────────────────────────────────
//////////////    public static CastleGrid Instance { get; private set; }

//////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////    [Header("Grid Settings")]
//////////////    public int totalRows = 6;
//////////////    public int totalCols = 8;

//////////////    [Header("Cell Size (pixels)")]
//////////////    public float cellSize = 120f;
//////////////    public float cellSpacing = 4f;

//////////////    [Header("Prefabs")]
//////////////    public GameObject gridCellPrefab;
//////////////    public GameObject castleBlockPrefab;
//////////////    public GameObject expansionSlotPrefab;

//////////////    [Header("Starting Block Positions")]
//////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////////////    // ── Private ───────────────────────────────────────────────────
//////////////    private GridCell[,] _grid;

//////////////    /// <summary>
//////////////    /// Whether expansion slots are currently allowed to render.
//////////////    /// True  → Castle Panel is active  (slots shown next to blocks).
//////////////    /// False → Village Panel is active (slots hidden everywhere).
//////////////    /// </summary>
//////////////    private bool _expansionSlotsVisible = true;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────
//////////////    private void Awake()
//////////////    {
//////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////////        Instance = this;
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        BuildGrid();
//////////////        PlaceDefaultBlocks();
//////////////        RefreshExpansionSlots();
//////////////    }

//////////////    // ── Public API ────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Call this whenever the active panel changes.
//////////////    ///   true  → Castle Panel opened  (expansion slots visible)
//////////////    ///   false → Village Panel opened (expansion slots hidden)
//////////////    /// </summary>
//////////////    public void SetExpansionSlotsVisible(bool visible)
//////////////    {
//////////////        if (_expansionSlotsVisible == visible) return; // nothing changed
//////////////        _expansionSlotsVisible = visible;
//////////////        RefreshExpansionSlots();
//////////////    }

//////////////    // ── Build Grid ────────────────────────────────────────────────
//////////////    private void BuildGrid()
//////////////    {
//////////////        // Clear any old cells
//////////////        if (_grid != null)
//////////////        {
//////////////            for (int r = 0; r < _grid.GetLength(0); r++)
//////////////                for (int c = 0; c < _grid.GetLength(1); c++)
//////////////                    if (_grid[r, c] != null)
//////////////                        Destroy(_grid[r, c].gameObject);
//////////////        }

//////////////        _grid = new GridCell[totalRows, totalCols];

//////////////        float step = cellSize + cellSpacing;
//////////////        float gridW = totalCols * step - cellSpacing;
//////////////        float gridH = totalRows * step - cellSpacing;

//////////////        // Bottom-left corner of the grid (relative to panel centre)
//////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////////////        for (int r = 0; r < totalRows; r++)
//////////////        {
//////////////            for (int c = 0; c < totalCols; c++)
//////////////            {
//////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////////////                cellObj.name = $"Cell_{r}_{c}";

//////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
//////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////////////                rt.anchoredPosition = new Vector2(
//////////////                    originX + c * step,
//////////////                    originY + r * step
//////////////                );

//////////////                GridCell cell = cellObj.GetComponent<GridCell>();
//////////////                cell.Init(r, c, this);
//////////////                _grid[r, c] = cell;
//////////////            }
//////////////        }

//////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////////////    }

//////////////    // ── Default Blocks ────────────────────────────────────────────
//////////////    private void PlaceDefaultBlocks()
//////////////    {
//////////////        foreach (var pos in defaultBlockPositions)
//////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////////////    }

//////////////    // ── Place Block ───────────────────────────────────────────────
//////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////////    {
//////////////        if (!InBounds(row, col))
//////////////        {
//////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////////////            return;
//////////////        }
//////////////        if (prefab == null)
//////////////        {
//////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
//////////////            return;
//////////////        }

//////////////        GridCell cell = _grid[row, col];
//////////////        if (cell.HasBlock)
//////////////        {
//////////////            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
//////////////            return;
//////////////        }

//////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////////        if (brt == null)
//////////////        {
//////////////            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
//////////////            return;
//////////////        }

//////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////        brt.pivot = new Vector2(0.5f, 0.5f);
//////////////        brt.anchoredPosition = Vector2.zero;
//////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////////        cell.PlaceBlock(block);

//////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////////////        RefreshExpansionSlots();
//////////////    }

//////////////    // ── Expansion Slots ───────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Recalculates which cells should show an expansion slot.
//////////////    /// When _expansionSlotsVisible is false (Village Panel), every
//////////////    /// slot is hidden regardless of adjacency.
//////////////    /// </summary>
//////////////    public void RefreshExpansionSlots()
//////////////    {
//////////////        for (int r = 0; r < totalRows; r++)
//////////////        {
//////////////            for (int c = 0; c < totalCols; c++)
//////////////            {
//////////////                GridCell cell = _grid[r, c];

//////////////                // Cells that already hold a block never show a slot
//////////////                if (cell.HasBlock)
//////////////                {
//////////////                    cell.HideExpansionSlot();
//////////////                    continue;
//////////////                }

//////////////                // ── Village Panel: hide all expansion slots ────────
//////////////                if (!_expansionSlotsVisible)
//////////////                {
//////////////                    cell.HideExpansionSlot();
//////////////                    continue;
//////////////                }

//////////////                // ── Castle Panel: staircase / half-triangle rule ───
//////////////                bool shouldShow = false;

//////////////                if (r == 0 && c == 0)
//////////////                {
//////////////                    // Anchor corner — never an expansion slot
//////////////                    shouldShow = false;
//////////////                }
//////////////                else if (r == 0)
//////////////                {
//////////////                    // Bottom row — only expand right; left neighbour must have a block
//////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////////////                }
//////////////                else if (c == 0)
//////////////                {
//////////////                    // Left column — only expand up; cell below must have a block
//////////////                    shouldShow = _grid[r - 1, c].HasBlock;
//////////////                }
//////////////                else
//////////////                {
//////////////                    // All other cells — both below AND left must have blocks
//////////////                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
//////////////                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
//////////////                    shouldShow = belowHasBlock && leftHasBlock;
//////////////                }

//////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////////                else cell.HideExpansionSlot();
//////////////            }
//////////////        }
//////////////    }

//////////////    // ── Helpers ───────────────────────────────────────────────────
//////////////    public bool InBounds(int r, int c) =>
//////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////////////    public GridCell GetCell(int r, int c) =>
//////////////        InBounds(r, c) ? _grid[r, c] : null;
//////////////}

////////////using UnityEngine;
////////////using System.Collections.Generic;

////////////public class CastleGrid : MonoBehaviour
////////////{
////////////    // ── Singleton ─────────────────────────────────────────────────
////////////    public static CastleGrid Instance { get; private set; }

////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    [Header("Grid Settings")]
////////////    public int totalRows = 6;
////////////    public int totalCols = 8;

////////////    [Header("Cell Size (pixels)")]
////////////    public float cellSize = 120f;
////////////    public float cellSpacing = 4f;

////////////    [Header("Prefabs")]
////////////    public GameObject gridCellPrefab;
////////////    public GameObject castleBlockPrefab;
////////////    public GameObject expansionSlotPrefab;

////////////    [Header("Starting Block Positions")]
////////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////////    // ── Private ───────────────────────────────────────────────────
////////////    private GridCell[,] _grid;

////////////    /// <summary>
////////////    /// Expansion slots are hidden by default (Village Panel is shown first).
////////////    /// UIManager calls SetExpansionSlotsVisible(true) when Castle Panel opens.
////////////    /// </summary>
////////////    private bool _expansionSlotsVisible = false;

////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////        Instance = this;

////////////        // Build grid in Awake so _grid is ready before any other script's Start()
////////////        BuildGrid();
////////////        PlaceDefaultBlocks();
////////////    }

////////////    private void Start()
////////////    {
////////////        // Slots start hidden (village view)
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Public API ────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Call when switching panels:
////////////    ///   true  → Castle Panel opened  (show expansion slots)
////////////    ///   false → Village Panel shown  (hide expansion slots)
////////////    /// </summary>
////////////    public void SetExpansionSlotsVisible(bool visible)
////////////    {
////////////        if (_expansionSlotsVisible == visible) return;
////////////        _expansionSlotsVisible = visible;
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Build Grid ────────────────────────────────────────────────
////////////    private void BuildGrid()
////////////    {
////////////        _grid = new GridCell[totalRows, totalCols];

////////////        float step = cellSize + cellSpacing;
////////////        float gridW = totalCols * step - cellSpacing;
////////////        float gridH = totalRows * step - cellSpacing;
////////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////////////        for (int r = 0; r < totalRows; r++)
////////////        {
////////////            for (int c = 0; c < totalCols; c++)
////////////            {
////////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////////                cellObj.name = $"Cell_{r}_{c}";

////////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////////////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

////////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////////                cell.Init(r, c, this);
////////////                _grid[r, c] = cell;
////////////            }
////////////        }

////////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////////    }

////////////    // ── Default Blocks ────────────────────────────────────────────
////////////    private void PlaceDefaultBlocks()
////////////    {
////////////        foreach (var pos in defaultBlockPositions)
////////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////////    }

////////////    // ── Place Block ───────────────────────────────────────────────
////////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////////    {
////////////        if (!InBounds(row, col))
////////////        {
////////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////////////            return;
////////////        }
////////////        if (prefab == null)
////////////        {
////////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
////////////            return;
////////////        }

////////////        GridCell cell = _grid[row, col];
////////////        if (cell.HasBlock) return;

////////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////////        if (brt == null) return;

////////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////////        brt.pivot = new Vector2(0.5f, 0.5f);
////////////        brt.anchoredPosition = Vector2.zero;
////////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////////        cell.PlaceBlock(block);

////////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
////////////        RefreshExpansionSlots();
////////////    }

////////////    // ── Expansion Slots ───────────────────────────────────────────
////////////    public void RefreshExpansionSlots()
////////////    {
////////////        // Guard: grid not built yet
////////////        if (_grid == null) return;

////////////        for (int r = 0; r < totalRows; r++)
////////////        {
////////////            for (int c = 0; c < totalCols; c++)
////////////            {
////////////                GridCell cell = _grid[r, c];

////////////                if (cell.HasBlock)
////////////                {
////////////                    cell.HideExpansionSlot();
////////////                    continue;
////////////                }

////////////                // Always hide when in Village Panel
////////////                if (!_expansionSlotsVisible)
////////////                {
////////////                    cell.HideExpansionSlot();
////////////                    continue;
////////////                }

////////////                // Castle Panel — staircase rule
////////////                bool shouldShow;

////////////                if (r == 0 && c == 0)
////////////                    shouldShow = false;
////////////                else if (r == 0)
////////////                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////////////                else if (c == 0)
////////////                    shouldShow = _grid[r - 1, c].HasBlock;
////////////                else
////////////                    shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

////////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////////////                else cell.HideExpansionSlot();
////////////            }
////////////        }
////////////    }

////////////    // ── Helpers ───────────────────────────────────────────────────
////////////    public bool InBounds(int r, int c) =>
////////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////////    public GridCell GetCell(int r, int c) =>
////////////        InBounds(r, c) ? _grid[r, c] : null;
////////////}

//////////using UnityEngine;
//////////using System.Collections.Generic;

//////////public class CastleGrid : MonoBehaviour
//////////{
//////////    // ── Singleton ─────────────────────────────────────────────────
//////////    public static CastleGrid Instance { get; private set; }

//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    [Header("Grid Settings")]
//////////    public int totalRows = 6;
//////////    public int totalCols = 8;

//////////    [Header("Cell Size (pixels)")]
//////////    public float cellSize = 120f;
//////////    public float cellSpacing = 4f;

//////////    [Header("Prefabs")]
//////////    public GameObject gridCellPrefab;
//////////    public GameObject castleBlockPrefab;
//////////    public GameObject expansionSlotPrefab;

//////////    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone + SoldierZone children). " +
//////////             "Placed on top of every exposed block (no block above it).")]
//////////    public GameObject castleBlockUnitSlotPrefab;

//////////    [Header("Starting Block Positions")]
//////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////////    // ── Private ───────────────────────────────────────────────────
//////////    private GridCell[,] _grid;

//////////    /// <summary>
//////////    /// false = Village Panel visible → all slots hidden.
//////////    /// true  = Castle Panel visible  → slots shown as appropriate.
//////////    /// </summary>
//////////    private bool _expansionSlotsVisible = false;

//////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////        Instance = this;

//////////        BuildGrid();
//////////        PlaceDefaultBlocks();
//////////    }

//////////    private void Start()
//////////    {
//////////        RefreshExpansionSlots();
//////////        RefreshUnitSlots();
//////////    }

//////////    // ── Public API ────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// true  → Castle Panel open  (show expansion + unit slots)
//////////    /// false → Village Panel open (hide all slots)
//////////    /// </summary>
//////////    public void SetExpansionSlotsVisible(bool visible)
//////////    {
//////////        if (_expansionSlotsVisible == visible) return;
//////////        _expansionSlotsVisible = visible;
//////////        RefreshExpansionSlots();
//////////        RefreshUnitSlots();
//////////    }

//////////    // ── Build Grid ────────────────────────────────────────────────
//////////    private void BuildGrid()
//////////    {
//////////        _grid = new GridCell[totalRows, totalCols];

//////////        float step = cellSize + cellSpacing;
//////////        float gridW = totalCols * step - cellSpacing;
//////////        float gridH = totalRows * step - cellSpacing;
//////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////////        for (int r = 0; r < totalRows; r++)
//////////        {
//////////            for (int c = 0; c < totalCols; c++)
//////////            {
//////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////////                cellObj.name = $"Cell_{r}_{c}";

//////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
//////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

//////////                GridCell cell = cellObj.GetComponent<GridCell>();
//////////                cell.Init(r, c, this);
//////////                _grid[r, c] = cell;
//////////            }
//////////        }

//////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////////    }

//////////    // ── Default Blocks ────────────────────────────────────────────
//////////    private void PlaceDefaultBlocks()
//////////    {
//////////        foreach (var pos in defaultBlockPositions)
//////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////////    }

//////////    // ── Place Block ───────────────────────────────────────────────
//////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////////    {
//////////        if (!InBounds(row, col))
//////////        {
//////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////////            return;
//////////        }
//////////        if (prefab == null)
//////////        {
//////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
//////////            return;
//////////        }

//////////        GridCell cell = _grid[row, col];
//////////        if (cell.HasBlock) return;

//////////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////////        if (brt == null) return;

//////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////////        brt.pivot = new Vector2(0.5f, 0.5f);
//////////        brt.anchoredPosition = Vector2.zero;
//////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////////        cell.PlaceBlock(block);

//////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
//////////        RefreshExpansionSlots();
//////////        RefreshUnitSlots();
//////////    }

//////////    // ── Expansion Slots ───────────────────────────────────────────
//////////    public void RefreshExpansionSlots()
//////////    {
//////////        if (_grid == null) return;

//////////        for (int r = 0; r < totalRows; r++)
//////////        {
//////////            for (int c = 0; c < totalCols; c++)
//////////            {
//////////                GridCell cell = _grid[r, c];

//////////                if (cell.HasBlock || !_expansionSlotsVisible)
//////////                {
//////////                    cell.HideExpansionSlot();
//////////                    continue;
//////////                }

//////////                // Staircase rule
//////////                bool shouldShow;
//////////                if (r == 0 && c == 0) shouldShow = false;
//////////                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////////                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
//////////                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

//////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////////                else cell.HideExpansionSlot();
//////////            }
//////////        }
//////////    }

//////////    // ── Unit Slots ────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Shows a cannon+soldier drop overlay on every block that has nothing above it
//////////    /// (the "exposed" top-edge blocks of the staircase).
//////////    /// Completely hidden when the Village Panel is active.
//////////    /// </summary>
//////////    public void RefreshUnitSlots()
//////////    {
//////////        if (_grid == null) return;

//////////        for (int r = 0; r < totalRows; r++)
//////////        {
//////////            for (int c = 0; c < totalCols; c++)
//////////            {
//////////                GridCell cell = _grid[r, c];

//////////                // Only blocks visible in the Castle Panel get unit slots
//////////                if (!cell.HasBlock || !_expansionSlotsVisible)
//////////                {
//////////                    cell.HideUnitSlot();
//////////                    continue;
//////////                }

//////////                // "Exposed" = the cell directly above this one has no block
//////////                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

//////////                if (isExposed && castleBlockUnitSlotPrefab != null)
//////////                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);
//////////                else
//////////                    cell.HideUnitSlot();
//////////            }
//////////        }
//////////    }

//////////    // ── Helpers ───────────────────────────────────────────────────
//////////    public bool InBounds(int r, int c) =>
//////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////////    public GridCell GetCell(int r, int c) =>
//////////        InBounds(r, c) ? _grid[r, c] : null;
//////////}


////////using UnityEngine;
////////using System.Collections.Generic;

////////public class CastleGrid : MonoBehaviour
////////{
////////    // ── Singleton ─────────────────────────────────────────────────
////////    public static CastleGrid Instance { get; private set; }

////////    // ── Inspector ─────────────────────────────────────────────────
////////    [Header("Grid Settings")]
////////    public int totalRows = 6;
////////    public int totalCols = 8;

////////    [Header("Cell Size (pixels)")]
////////    public float cellSize = 120f;
////////    public float cellSpacing = 4f;

////////    [Header("Prefabs")]
////////    public GameObject gridCellPrefab;
////////    public GameObject castleBlockPrefab;
////////    public GameObject expansionSlotPrefab;

////////    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone + SoldierZone children). " +
////////             "Placed on top of every exposed block (no block above it).")]
////////    public GameObject castleBlockUnitSlotPrefab;

////////    [Header("Starting Block Positions")]
////////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////////    // ── Private ───────────────────────────────────────────────────
////////    private GridCell[,] _grid;

////////    /// <summary>
////////    /// false = Village Panel visible → expansion slots hidden, unit slots non-interactable.
////////    /// true  = Castle Panel visible  → expansion slots shown, unit slots interactable.
////////    /// Unit slot VISUALS (including placed cannons/soldiers) are always kept alive.
////////    /// </summary>
////////    private bool _expansionSlotsVisible = false;

////////    // ── Lifecycle ─────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;

////////        BuildGrid();
////////        PlaceDefaultBlocks();
////////    }

////////    private void Start()
////////    {
////////        RefreshExpansionSlots();
////////        RefreshUnitSlots();
////////    }

////////    // ── Public API ────────────────────────────────────────────────

////////    /// <summary>
////////    /// Call this when switching panels.
////////    /// true  → Castle Panel open  (expansion slots shown, unit slots interactable)
////////    /// false → Village Panel open (expansion slots hidden, unit slots visible but non-interactable)
////////    /// </summary>
////////    public void SetExpansionSlotsVisible(bool visible)
////////    {
////////        if (_expansionSlotsVisible == visible) return;
////////        _expansionSlotsVisible = visible;
////////        RefreshExpansionSlots();
////////        RefreshUnitSlots();
////////    }

////////    // ── Build Grid ────────────────────────────────────────────────
////////    private void BuildGrid()
////////    {
////////        _grid = new GridCell[totalRows, totalCols];

////////        float step = cellSize + cellSpacing;
////////        float gridW = totalCols * step - cellSpacing;
////////        float gridH = totalRows * step - cellSpacing;
////////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////////        for (int r = 0; r < totalRows; r++)
////////        {
////////            for (int c = 0; c < totalCols; c++)
////////            {
////////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////////                cellObj.name = $"Cell_{r}_{c}";

////////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

////////                GridCell cell = cellObj.GetComponent<GridCell>();
////////                cell.Init(r, c, this);
////////                _grid[r, c] = cell;
////////            }
////////        }

////////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////////    }

////////    // ── Default Blocks ────────────────────────────────────────────
////////    private void PlaceDefaultBlocks()
////////    {
////////        foreach (var pos in defaultBlockPositions)
////////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////////    }

////////    // ── Place Block ───────────────────────────────────────────────
////////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////////    {
////////        if (!InBounds(row, col))
////////        {
////////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////////            return;
////////        }
////////        if (prefab == null)
////////        {
////////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
////////            return;
////////        }

////////        GridCell cell = _grid[row, col];
////////        if (cell.HasBlock) return;

////////        GameObject blockObj = Instantiate(prefab, cell.transform);

////////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////////        if (brt == null) return;

////////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////////        brt.pivot = new Vector2(0.5f, 0.5f);
////////        brt.anchoredPosition = Vector2.zero;
////////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////////        cell.PlaceBlock(block);

////////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
////////        RefreshExpansionSlots();
////////        RefreshUnitSlots();
////////    }

////////    // ── Expansion Slots ───────────────────────────────────────────
////////    public void RefreshExpansionSlots()
////////    {
////////        if (_grid == null) return;

////////        for (int r = 0; r < totalRows; r++)
////////        {
////////            for (int c = 0; c < totalCols; c++)
////////            {
////////                GridCell cell = _grid[r, c];

////////                if (cell.HasBlock || !_expansionSlotsVisible)
////////                {
////////                    cell.HideExpansionSlot();
////////                    continue;
////////                }

////////                // Staircase rule
////////                bool shouldShow;
////////                if (r == 0 && c == 0) shouldShow = false;
////////                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////////                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
////////                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

////////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////////                else cell.HideExpansionSlot();
////////            }
////////        }
////////    }

////////    // ── Unit Slots ────────────────────────────────────────────────

////////    /// <summary>
////////    /// Shows a cannon+soldier drop overlay on every exposed block.
////////    /// The overlay is ALWAYS kept alive so placed units survive panel switches.
////////    /// Only interactability is toggled: dragging works in Castle Panel only.
////////    /// HideUnitSlot() is only called when a block is genuinely removed or covered.
////////    /// </summary>
////////    public void RefreshUnitSlots()
////////    {
////////        if (_grid == null) return;

////////        for (int r = 0; r < totalRows; r++)
////////        {
////////            for (int c = 0; c < totalCols; c++)
////////            {
////////                GridCell cell = _grid[r, c];

////////                // No block here → no unit slot needed at all
////////                if (!cell.HasBlock)
////////                {
////////                    cell.HideUnitSlot();
////////                    continue;
////////                }

////////                // "Exposed" = the cell directly above this one has no block
////////                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

////////                if (isExposed && castleBlockUnitSlotPrefab != null)
////////                {
////////                    // Always show the slot so the cannon visual persists
////////                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);

////////                    // Only allow drag-drop in Castle Panel
////////                    cell.SetUnitSlotInteractable(_expansionSlotsVisible);
////////                }
////////                else
////////                {
////////                    // Block is covered — destroy the slot (safe, no cannon can sit here)
////////                    cell.HideUnitSlot();
////////                }
////////            }
////////        }
////////    }

////////    // ── Helpers ───────────────────────────────────────────────────
////////    public bool InBounds(int r, int c) =>
////////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////////    public GridCell GetCell(int r, int c) =>
////////        InBounds(r, c) ? _grid[r, c] : null;
////////}

//////using UnityEngine;
//////using System.Collections.Generic;

//////public class CastleGrid : MonoBehaviour
//////{
//////    // ── Singleton ─────────────────────────────────────────────────
//////    public static CastleGrid Instance { get; private set; }

//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Header("Grid Settings")]
//////    public int totalRows = 6;
//////    public int totalCols = 8;

//////    [Header("Cell Size (pixels)")]
//////    public float cellSize = 120f;
//////    public float cellSpacing = 4f;

//////    [Header("Prefabs")]
//////    public GameObject gridCellPrefab;
//////    public GameObject castleBlockPrefab;
//////    public GameObject expansionSlotPrefab;

//////    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone + SoldierZone children). " +
//////             "Placed on top of every exposed block (no block above it).")]
//////    public GameObject castleBlockUnitSlotPrefab;

//////    [Header("Starting Block Positions")]
//////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//////    // ── Private ───────────────────────────────────────────────────
//////    private GridCell[,] _grid;

//////    /// <summary>
//////    /// false = Village Panel visible → expansion slots hidden, unit slots non-interactable.
//////    /// true  = Castle Panel visible  → expansion slots shown, unit slots interactable.
//////    /// Unit slot VISUALS (including placed cannons/soldiers) are always kept alive.
//////    /// </summary>
//////    private bool _expansionSlotsVisible = false;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;

//////        BuildGrid();
//////        PlaceDefaultBlocks();
//////    }

//////    private void Start()
//////    {
//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Call this when switching panels.
//////    /// true  → Castle Panel open  (expansion slots shown, unit slots interactable)
//////    /// false → Village Panel open (expansion slots hidden, unit slots visible but non-interactable)
//////    /// </summary>
//////    public void SetExpansionSlotsVisible(bool visible)
//////    {
//////        if (_expansionSlotsVisible == visible) return;
//////        _expansionSlotsVisible = visible;
//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Build Grid ────────────────────────────────────────────────
//////    private void BuildGrid()
//////    {
//////        _grid = new GridCell[totalRows, totalCols];

//////        float step = cellSize + cellSpacing;
//////        float gridW = totalCols * step - cellSpacing;
//////        float gridH = totalRows * step - cellSpacing;
//////        float originX = -gridW * 0.5f + cellSize * 0.5f;
//////        float originY = -gridH * 0.5f + cellSize * 0.5f;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//////                cellObj.name = $"Cell_{r}_{c}";

//////                RectTransform rt = cellObj.GetComponent<RectTransform>();
//////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////                rt.pivot = new Vector2(0.5f, 0.5f);
//////                rt.sizeDelta = new Vector2(cellSize, cellSize);
//////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

//////                GridCell cell = cellObj.GetComponent<GridCell>();
//////                cell.Init(r, c, this);
//////                _grid[r, c] = cell;
//////            }
//////        }

//////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//////    }

//////    // ── Default Blocks ────────────────────────────────────────────
//////    private void PlaceDefaultBlocks()
//////    {
//////        foreach (var pos in defaultBlockPositions)
//////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//////    }

//////    // ── Place Block ───────────────────────────────────────────────
//////    public void PlaceBlockAt(int row, int col, GameObject prefab)
//////    {
//////        if (!InBounds(row, col))
//////        {
//////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//////            return;
//////        }
//////        if (prefab == null)
//////        {
//////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
//////            return;
//////        }

//////        GridCell cell = _grid[row, col];
//////        if (cell.HasBlock) return;

//////        GameObject blockObj = Instantiate(prefab, cell.transform);

//////        RectTransform brt = blockObj.GetComponent<RectTransform>();
//////        if (brt == null) return;

//////        brt.anchorMin = new Vector2(0.5f, 0.5f);
//////        brt.anchorMax = new Vector2(0.5f, 0.5f);
//////        brt.pivot = new Vector2(0.5f, 0.5f);
//////        brt.anchoredPosition = Vector2.zero;
//////        brt.sizeDelta = new Vector2(cellSize, cellSize);

//////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//////        cell.PlaceBlock(block);

//////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");

//////        // ── Migrate units upward before RefreshUnitSlots destroys the source slot ──
//////        // The cell directly below (row-1, col) was the previous exposed top of this
//////        // column. It's now covered, so its unit slot will be destroyed in
//////        // RefreshUnitSlots(). Manually create the new slot first and move units into it.
//////        MigrateUnitsFromBelow(row, col);

//////        RefreshExpansionSlots();
//////        RefreshUnitSlots();
//////    }

//////    // ── Expansion Slots ───────────────────────────────────────────
//////    public void RefreshExpansionSlots()
//////    {
//////        if (_grid == null) return;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GridCell cell = _grid[r, c];

//////                if (cell.HasBlock || !_expansionSlotsVisible)
//////                {
//////                    cell.HideExpansionSlot();
//////                    continue;
//////                }

//////                // Staircase rule
//////                bool shouldShow;
//////                if (r == 0 && c == 0) shouldShow = false;
//////                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//////                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
//////                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

//////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//////                else cell.HideExpansionSlot();
//////            }
//////        }
//////    }

//////    // ── Unit Slots ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Shows a cannon+soldier drop overlay on every exposed block.
//////    /// The overlay is ALWAYS kept alive so placed units survive panel switches.
//////    /// Only interactability is toggled: dragging works in Castle Panel only.
//////    /// HideUnitSlot() is only called when a block is genuinely removed or covered.
//////    /// </summary>
//////    public void RefreshUnitSlots()
//////    {
//////        if (_grid == null) return;

//////        for (int r = 0; r < totalRows; r++)
//////        {
//////            for (int c = 0; c < totalCols; c++)
//////            {
//////                GridCell cell = _grid[r, c];

//////                // No block here → no unit slot needed at all
//////                if (!cell.HasBlock)
//////                {
//////                    cell.HideUnitSlot();
//////                    continue;
//////                }

//////                // "Exposed" = the cell directly above this one has no block
//////                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

//////                if (isExposed && castleBlockUnitSlotPrefab != null)
//////                {
//////                    // Always show the slot so the cannon visual persists
//////                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);

//////                    // Only allow drag-drop in Castle Panel
//////                    cell.SetUnitSlotInteractable(_expansionSlotsVisible);
//////                }
//////                else
//////                {
//////                    // Block is covered — destroy the slot (safe, no cannon can sit here)
//////                    cell.HideUnitSlot();
//////                }
//////            }
//////        }
//////    }

//////    // ── Helpers ───────────────────────────────────────────────────
//////    public bool InBounds(int r, int c) =>
//////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//////    public GridCell GetCell(int r, int c) =>
//////        InBounds(r, c) ? _grid[r, c] : null;

//////    /// <summary>
//////    /// Called immediately after a block is placed at (row, col).
//////    /// If the cell directly below (row-1, col) has a unit slot with placed units,
//////    /// creates the new cell's unit slot now, reparents all units into it, then
//////    /// destroys the old slot container — before RefreshUnitSlots() runs.
//////    /// The cannon GameObject is never destroyed; it is reparented.
//////    /// </summary>
//////    private void MigrateUnitsFromBelow(int row, int col)
//////    {
//////        if (!InBounds(row - 1, col)) return;

//////        GridCell belowCell = _grid[row - 1, col];
//////        if (belowCell.GetUnitSlot() == null) return;  // nothing placed below

//////        GridCell newCell = _grid[row, col];

//////        // 1. Create the destination slot on the new block right now so it
//////        //    exists as a reparent target before RefreshUnitSlots() runs
//////        if (castleBlockUnitSlotPrefab != null)
//////        {
//////            newCell.ShowUnitSlot(castleBlockUnitSlotPrefab);
//////            newCell.SetUnitSlotInteractable(_expansionSlotsVisible);
//////        }

//////        // 2. Reparent every cannon / soldier from the source drop zones
//////        //    into the matching drop zones on the new slot — nothing is destroyed
//////        belowCell.TransferUnitSlotTo(newCell);

//////        // 3. Destroy the now-empty source slot container (no RemoveAll call)
//////        belowCell.HideUnitSlotEmpty();
//////    }

//////}


////using UnityEngine;
////using System.Collections.Generic;

////public class CastleGrid : MonoBehaviour
////{
////    // ── Singleton ─────────────────────────────────────────────────
////    public static CastleGrid Instance { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────
////    [Header("Grid Settings")]
////    public int totalRows = 6;
////    public int totalCols = 8;

////    [Header("Cell Size (pixels)")]
////    public float cellSize = 120f;
////    public float cellSpacing = 4f;

////    [Header("Prefabs")]
////    public GameObject gridCellPrefab;
////    public GameObject castleBlockPrefab;
////    public GameObject expansionSlotPrefab;

////    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone + SoldierZone children). " +
////             "Placed on top of every exposed block (no block above it).")]
////    public GameObject castleBlockUnitSlotPrefab;

////    [Header("Starting Block Positions")]
////    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

////    // ── Private ───────────────────────────────────────────────────
////    private GridCell[,] _grid;

////    /// <summary>
////    /// false = Village Panel visible → expansion slots hidden, unit slots non-interactable.
////    /// true  = Castle Panel visible  → expansion slots shown, unit slots interactable.
////    /// Unit slot VISUALS (including placed cannons/soldiers) are always kept alive.
////    /// </summary>
////    private bool _expansionSlotsVisible = false;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        BuildGrid();
////        PlaceDefaultBlocks();
////    }

////    private void Start()
////    {
////        RefreshExpansionSlots();
////        RefreshUnitSlots();
////    }

////    // ── Public API ────────────────────────────────────────────────

////    /// <summary>
////    /// Call this when switching panels.
////    /// true  → Castle Panel open  (expansion slots shown, unit slots interactable)
////    /// false → Village Panel open (expansion slots hidden, unit slots visible but non-interactable)
////    /// </summary>
////    public void SetExpansionSlotsVisible(bool visible)
////    {
////        if (_expansionSlotsVisible == visible) return;
////        _expansionSlotsVisible = visible;
////        RefreshExpansionSlots();
////        RefreshUnitSlots();
////    }

////    // ── Build Grid ────────────────────────────────────────────────
////    private void BuildGrid()
////    {
////        _grid = new GridCell[totalRows, totalCols];

////        float step = cellSize + cellSpacing;
////        float gridW = totalCols * step - cellSpacing;
////        float gridH = totalRows * step - cellSpacing;
////        float originX = -gridW * 0.5f + cellSize * 0.5f;
////        float originY = -gridH * 0.5f + cellSize * 0.5f;

////        for (int r = 0; r < totalRows; r++)
////        {
////            for (int c = 0; c < totalCols; c++)
////            {
////                GameObject cellObj = Instantiate(gridCellPrefab, transform);
////                cellObj.name = $"Cell_{r}_{c}";

////                RectTransform rt = cellObj.GetComponent<RectTransform>();
////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////                rt.pivot = new Vector2(0.5f, 0.5f);
////                rt.sizeDelta = new Vector2(cellSize, cellSize);
////                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

////                GridCell cell = cellObj.GetComponent<GridCell>();
////                cell.Init(r, c, this);
////                _grid[r, c] = cell;
////            }
////        }

////        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
////    }

////    // ── Default Blocks ────────────────────────────────────────────
////    private void PlaceDefaultBlocks()
////    {
////        foreach (var pos in defaultBlockPositions)
////            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
////    }

////    // ── Place Block ───────────────────────────────────────────────
////    public void PlaceBlockAt(int row, int col, GameObject prefab)
////    {
////        if (!InBounds(row, col))
////        {
////            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
////            return;
////        }
////        if (prefab == null)
////        {
////            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
////            return;
////        }

////        GridCell cell = _grid[row, col];
////        if (cell.HasBlock) return;

////        GameObject blockObj = Instantiate(prefab, cell.transform);

////        RectTransform brt = blockObj.GetComponent<RectTransform>();
////        if (brt == null) return;

////        brt.anchorMin = new Vector2(0.5f, 0.5f);
////        brt.anchorMax = new Vector2(0.5f, 0.5f);
////        brt.pivot = new Vector2(0.5f, 0.5f);
////        brt.anchoredPosition = Vector2.zero;
////        brt.sizeDelta = new Vector2(cellSize, cellSize);

////        CastleBlock block = blockObj.GetComponent<CastleBlock>();
////        cell.PlaceBlock(block);

////        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");

////        // ── Migrate units upward before RefreshUnitSlots destroys the source slot ──
////        // The cell directly below (row-1, col) was the previous exposed top of this
////        // column. It's now covered, so its unit slot will be destroyed in
////        // RefreshUnitSlots(). Manually create the new slot first and move units into it.
////        MigrateUnitsFromBelow(row, col);

////        RefreshExpansionSlots();
////        RefreshUnitSlots();
////    }

////    // ── Expansion Slots ───────────────────────────────────────────
////    public void RefreshExpansionSlots()
////    {
////        if (_grid == null) return;

////        for (int r = 0; r < totalRows; r++)
////        {
////            for (int c = 0; c < totalCols; c++)
////            {
////                GridCell cell = _grid[r, c];

////                if (cell.HasBlock || !_expansionSlotsVisible)
////                {
////                    cell.HideExpansionSlot();
////                    continue;
////                }

////                // Staircase rule
////                bool shouldShow;
////                if (r == 0 && c == 0) shouldShow = false;
////                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
////                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
////                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

////                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
////                else cell.HideExpansionSlot();
////            }
////        }
////    }

////    // ── Unit Slots ────────────────────────────────────────────────

////    /// <summary>
////    /// Shows a cannon+soldier drop overlay on every exposed block.
////    /// The overlay is ALWAYS kept alive so placed units survive panel switches.
////    /// Only interactability is toggled: dragging works in Castle Panel only.
////    /// HideUnitSlot() is only called when a block is genuinely removed or covered.
////    /// </summary>
////    public void RefreshUnitSlots()
////    {
////        if (_grid == null) return;

////        for (int r = 0; r < totalRows; r++)
////        {
////            for (int c = 0; c < totalCols; c++)
////            {
////                GridCell cell = _grid[r, c];

////                // No block here → no unit slot needed at all
////                if (!cell.HasBlock)
////                {
////                    cell.HideUnitSlot();
////                    continue;
////                }

////                // "Exposed" = the cell directly above this one has no block
////                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

////                if (isExposed && castleBlockUnitSlotPrefab != null)
////                {
////                    // Always show the slot so the cannon visual persists
////                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);

////                    // Only allow drag-drop in Castle Panel
////                    cell.SetUnitSlotInteractable(_expansionSlotsVisible);
////                }
////                else
////                {
////                    // Block is covered — destroy the slot (safe, no cannon can sit here)
////                    cell.HideUnitSlot();
////                }
////            }
////        }
////    }

////    // ── Helpers ───────────────────────────────────────────────────
////    public bool InBounds(int r, int c) =>
////        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

////    public GridCell GetCell(int r, int c) =>
////        InBounds(r, c) ? _grid[r, c] : null;

////    /// <summary>
////    /// Called immediately after a block is placed at (row, col).
////    /// Always checks whether the cell directly below (row-1, col) has any
////    /// placed units — even if its _unitSlotInstance was lost on a panel reload —
////    /// by searching the full cell hierarchy for CastleUnitDropZone children.
////    /// If units exist, the new block's slot is created first and the units are
////    /// reparented into it before RefreshUnitSlots() destroys the old slot.
////    /// The cannon GameObject is never destroyed; it is only reparented.
////    /// </summary>
////    private void MigrateUnitsFromBelow(int row, int col)
////    {
////        if (!InBounds(row - 1, col)) return;

////        GridCell belowCell = _grid[row - 1, col];
////        if (!belowCell.HasBlock) return;

////        // Check for placed units by searching the full hierarchy,
////        // not via GetUnitSlot() — that reference can be null after a panel reload
////        // Use HasUnit — NOT childCount. EmptyVisual / Highlight / Soldier are
////        // always children of the zone even when empty, so childCount > 0 is
////        // always true and would trigger migration on every block placement.
////        bool hasUnits = false;
////        foreach (var zone in belowCell.GetComponentsInChildren<CastleUnitDropZone>(true))
////        {
////            if (zone.HasUnit) { hasUnits = true; break; }
////        }
////        if (!hasUnits) return;

////        GridCell newCell = _grid[row, col];

////        // 1. Create the destination slot on the new block right now so it
////        //    exists as a reparent target before RefreshUnitSlots() runs
////        if (castleBlockUnitSlotPrefab != null)
////        {
////            newCell.ShowUnitSlot(castleBlockUnitSlotPrefab);
////            newCell.SetUnitSlotInteractable(_expansionSlotsVisible);
////        }

////        // 2. Reparent every cannon / soldier into the new slot — nothing destroyed
////        belowCell.TransferUnitSlotTo(newCell);

////        // 3. Destroy the now-empty source slot container (no RemoveAll call)
////        belowCell.HideUnitSlotEmpty();
////    }

////}

//using UnityEngine;
//using System.Collections.Generic;

//public class CastleGrid : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────
//    public static CastleGrid Instance { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────
//    [Header("Grid Settings")]
//    public int totalRows = 6;
//    public int totalCols = 8;

//    [Header("Cell Size (pixels)")]
//    public float cellSize = 120f;
//    public float cellSpacing = 4f;

//    [Header("Prefabs")]
//    public GameObject gridCellPrefab;
//    public GameObject castleBlockPrefab;
//    public GameObject expansionSlotPrefab;

//    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone child). " +
//             "Placed on top of every exposed block (no block above it).")]
//    public GameObject castleBlockUnitSlotPrefab;

//    [Header("Starting Block Positions")]
//    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//    // ── Private ───────────────────────────────────────────────────
//    private GridCell[,] _grid;

//    /// <summary>
//    /// false = Village Panel → expansion slots hidden, cannon zones transparent (alpha 0).
//    ///                         Drag-and-drop of cannons is still ENABLED.
//    /// true  = Castle Panel  → expansion slots shown, cannon zones visible, full interaction.
//    /// </summary>
//    private bool _expansionSlotsVisible = false;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        BuildGrid();
//        PlaceDefaultBlocks();
//    }

//    private void Start()
//    {
//        RefreshExpansionSlots();
//        RefreshUnitSlots();
//    }

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>
//    /// Call this when switching panels.
//    ///
//    /// true  (Castle Panel open):
//    ///   • Expansion slots shown  → player can BUY and place new blocks.
//    ///   • Cannon zones visible   → drop-zone background shown normally.
//    ///   • Drag-drop: enabled.
//    ///
//    /// false (Village Panel open):
//    ///   • Expansion slots hidden → block adding is NOT possible here.
//    ///   • Cannon zones alpha = 0 → zone UI invisible but raycasts ON.
//    ///   • Drag-drop: still ENABLED — cannons from the shop can be dropped
//    ///     onto blocks even while the village panel is showing.
//    /// </summary>
//    public void SetExpansionSlotsVisible(bool visible)
//    {
//        if (_expansionSlotsVisible == visible) return;
//        _expansionSlotsVisible = visible;
//        RefreshExpansionSlots();
//        RefreshUnitSlots();
//    }

//    // ── Build Grid ────────────────────────────────────────────────

//    private void BuildGrid()
//    {
//        _grid = new GridCell[totalRows, totalCols];

//        float step = cellSize + cellSpacing;
//        float gridW = totalCols * step - cellSpacing;
//        float gridH = totalRows * step - cellSpacing;
//        float originX = -gridW * 0.5f + cellSize * 0.5f;
//        float originY = -gridH * 0.5f + cellSize * 0.5f;

//        for (int r = 0; r < totalRows; r++)
//        {
//            for (int c = 0; c < totalCols; c++)
//            {
//                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//                cellObj.name = $"Cell_{r}_{c}";

//                RectTransform rt = cellObj.GetComponent<RectTransform>();
//                rt.anchorMin = new Vector2(0.5f, 0.5f);
//                rt.anchorMax = new Vector2(0.5f, 0.5f);
//                rt.pivot = new Vector2(0.5f, 0.5f);
//                rt.sizeDelta = new Vector2(cellSize, cellSize);
//                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

//                GridCell cell = cellObj.GetComponent<GridCell>();
//                cell.Init(r, c, this);
//                _grid[r, c] = cell;
//            }
//        }

//        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//    }

//    // ── Default Blocks ────────────────────────────────────────────

//    private void PlaceDefaultBlocks()
//    {
//        foreach (var pos in defaultBlockPositions)
//            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//    }

//    // ── Place Block ───────────────────────────────────────────────

//    public void PlaceBlockAt(int row, int col, GameObject prefab)
//    {
//        if (!InBounds(row, col))
//        {
//            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
//            return;
//        }
//        if (prefab == null)
//        {
//            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
//            return;
//        }

//        GridCell cell = _grid[row, col];
//        if (cell.HasBlock) return;

//        GameObject blockObj = Instantiate(prefab, cell.transform);

//        RectTransform brt = blockObj.GetComponent<RectTransform>();
//        if (brt == null) return;

//        brt.anchorMin = new Vector2(0.5f, 0.5f);
//        brt.anchorMax = new Vector2(0.5f, 0.5f);
//        brt.pivot = new Vector2(0.5f, 0.5f);
//        brt.anchoredPosition = Vector2.zero;
//        brt.sizeDelta = new Vector2(cellSize, cellSize);

//        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//        cell.PlaceBlock(block);

//        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");

//        // Migrate units upward before RefreshUnitSlots destroys the source slot.
//        MigrateUnitsFromBelow(row, col);

//        RefreshExpansionSlots();
//        RefreshUnitSlots();
//    }

//    // ── Expansion Slots ───────────────────────────────────────────

//    /// <summary>
//    /// Expansion slots (used to BUY and place new blocks) are only shown in
//    /// the Castle Panel. They are always hidden in the Village Panel.
//    /// </summary>
//    public void RefreshExpansionSlots()
//    {
//        if (_grid == null) return;

//        for (int r = 0; r < totalRows; r++)
//        {
//            for (int c = 0; c < totalCols; c++)
//            {
//                GridCell cell = _grid[r, c];

//                // Hidden if: cell already has a block, OR we are in Village Panel.
//                if (cell.HasBlock || !_expansionSlotsVisible)
//                {
//                    cell.HideExpansionSlot();
//                    continue;
//                }

//                // Staircase adjacency rule
//                bool shouldShow;
//                if (r == 0 && c == 0) shouldShow = false;
//                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
//                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
//                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

//                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
//                else cell.HideExpansionSlot();
//            }
//        }
//    }

//    // ── Unit Slots ────────────────────────────────────────────────

//    /// <summary>
//    /// Shows the cannon drop-zone overlay on every exposed block and applies the
//    /// correct village / castle display mode.
//    ///
//    /// The overlay is ALWAYS kept alive across panel switches so placed cannons
//    /// survive. Only the visual mode changes:
//    ///
//    ///   Castle Panel → cannon zone visible (normalColor background).
//    ///   Village Panel → cannon zone alpha = 0 (invisible background).
//    ///                   Raycasts remain ON — drag-drop still works.
//    ///
//    /// HideUnitSlot() is only called when a block is genuinely removed or covered.
//    /// </summary>
//    public void RefreshUnitSlots()
//    {
//        if (_grid == null) return;

//        for (int r = 0; r < totalRows; r++)
//        {
//            for (int c = 0; c < totalCols; c++)
//            {
//                GridCell cell = _grid[r, c];

//                // No block here → no unit slot
//                if (!cell.HasBlock)
//                {
//                    cell.HideUnitSlot();
//                    continue;
//                }

//                // "Exposed" = the cell directly above this one has no block
//                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

//                if (isExposed && castleBlockUnitSlotPrefab != null)
//                {
//                    // Always show the slot so the cannon visual persists.
//                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);

//                    // Village Panel  → isVillage = true  → alpha 0, raycasts ON.
//                    // Castle Panel   → isVillage = false → normal color, raycasts ON.
//                    bool isVillage = !_expansionSlotsVisible;
//                    cell.SetUnitSlotVillageMode(isVillage);
//                }
//                else
//                {
//                    // Block is covered — destroy the slot (no cannon can sit here).
//                    cell.HideUnitSlot();
//                }
//            }
//        }
//    }

//    // ── Helpers ───────────────────────────────────────────────────

//    public bool InBounds(int r, int c) =>
//        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//    public GridCell GetCell(int r, int c) =>
//        InBounds(r, c) ? _grid[r, c] : null;

//    /// <summary>
//    /// Called immediately after a block is placed at (row, col).
//    /// If the cell directly below (row-1, col) has placed units, they are
//    /// migrated up into the new block's slot before RefreshUnitSlots() removes
//    /// the old slot. The cannon GameObjects are never destroyed; only reparented.
//    /// </summary>
//    private void MigrateUnitsFromBelow(int row, int col)
//    {
//        if (!InBounds(row - 1, col)) return;

//        GridCell belowCell = _grid[row - 1, col];
//        if (!belowCell.HasBlock) return;

//        bool hasUnits = false;
//        foreach (var zone in belowCell.GetComponentsInChildren<CastleUnitDropZone>(true))
//        {
//            if (zone.HasUnit) { hasUnits = true; break; }
//        }
//        if (!hasUnits) return;

//        GridCell newCell = _grid[row, col];

//        // 1. Create the destination slot on the new block first.
//        if (castleBlockUnitSlotPrefab != null)
//        {
//            newCell.ShowUnitSlot(castleBlockUnitSlotPrefab);
//            newCell.SetUnitSlotVillageMode(!_expansionSlotsVisible);
//        }

//        // 2. Reparent every cannon / soldier into the new slot.
//        belowCell.TransferUnitSlotTo(newCell);

//        // 3. Destroy the now-empty source slot (no RemoveAll call).
//        belowCell.HideUnitSlotEmpty();
//    }
//}

using UnityEngine;
using System.Collections.Generic;

public class CastleGrid : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static CastleGrid Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Grid Settings")]
    public int totalRows = 6;
    public int totalCols = 8;

    [Header("Cell Size (pixels)")]
    public float cellSize = 120f;
    public float cellSpacing = 4f;

    [Header("Prefabs")]
    public GameObject gridCellPrefab;
    public GameObject castleBlockPrefab;
    public GameObject expansionSlotPrefab;

    [Tooltip("Prefab with CastleBlockUnitSlot (CannonZone child). " +
             "Placed on top of every exposed block (no block above it).")]
    public GameObject castleBlockUnitSlotPrefab;

    [Header("Starting Block Positions")]
    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

    // ── Private ───────────────────────────────────────────────────
    private GridCell[,] _grid;

    /// <summary>
    /// false = Village Panel → expansion slots hidden, cannon zones transparent (alpha 0).
    ///                         Drag-and-drop of cannons is still ENABLED.
    /// true  = Castle Panel  → expansion slots shown, cannon zones visible, full interaction.
    /// </summary>
    private bool _expansionSlotsVisible = false;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildGrid();
        PlaceDefaultBlocks();
    }

    private void Start()
    {
        RefreshExpansionSlots();
        RefreshUnitSlots();
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Call this when switching panels.
    ///
    /// true  (Castle Panel open):
    ///   • Expansion slots shown  → player can BUY and place new blocks.
    ///   • Cannon zones visible, raycasts OFF → cannon drag-drop DISABLED.
    ///
    /// false (Village Panel open):
    ///   • Expansion slots hidden → block adding is NOT possible here.
    ///   • Cannon zones alpha = 0, raycasts ON → cannon drag-drop ENABLED.
    ///     Cannons from the shop can be dragged and dropped onto blocks.
    /// </summary>
    public void SetExpansionSlotsVisible(bool visible)
    {
        if (_expansionSlotsVisible == visible) return;
        _expansionSlotsVisible = visible;
        RefreshExpansionSlots();
        RefreshUnitSlots();
    }

    // ── Build Grid ────────────────────────────────────────────────

    private void BuildGrid()
    {
        _grid = new GridCell[totalRows, totalCols];

        float step = cellSize + cellSpacing;
        float gridW = totalCols * step - cellSpacing;
        float gridH = totalRows * step - cellSpacing;
        float originX = -gridW * 0.5f + cellSize * 0.5f;
        float originY = -gridH * 0.5f + cellSize * 0.5f;

        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                GameObject cellObj = Instantiate(gridCellPrefab, transform);
                cellObj.name = $"Cell_{r}_{c}";

                RectTransform rt = cellObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(originX + c * step, originY + r * step);

                GridCell cell = cellObj.GetComponent<GridCell>();
                cell.Init(r, c, this);
                _grid[r, c] = cell;
            }
        }

        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
    }

    // ── Default Blocks ────────────────────────────────────────────

    private void PlaceDefaultBlocks()
    {
        foreach (var pos in defaultBlockPositions)
            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
    }

    // ── Place Block ───────────────────────────────────────────────

    public void PlaceBlockAt(int row, int col, GameObject prefab)
    {
        if (!InBounds(row, col))
        {
            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
            return;
        }
        if (prefab == null)
        {
            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null!");
            return;
        }

        GridCell cell = _grid[row, col];
        if (cell.HasBlock) return;

        GameObject blockObj = Instantiate(prefab, cell.transform);

        RectTransform brt = blockObj.GetComponent<RectTransform>();
        if (brt == null) return;

        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(cellSize, cellSize);

        CastleBlock block = blockObj.GetComponent<CastleBlock>();
        cell.PlaceBlock(block);

        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");

        // Migrate units upward before RefreshUnitSlots destroys the source slot.
        MigrateUnitsFromBelow(row, col);

        RefreshExpansionSlots();
        RefreshUnitSlots();
    }

    // ── Expansion Slots ───────────────────────────────────────────

    /// <summary>
    /// Expansion slots (used to BUY and place new blocks) are only shown in
    /// the Castle Panel. They are always hidden in the Village Panel.
    /// </summary>
    public void RefreshExpansionSlots()
    {
        if (_grid == null) return;

        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                GridCell cell = _grid[r, c];

                // Hidden if: cell already has a block, OR we are in Village Panel.
                if (cell.HasBlock || !_expansionSlotsVisible)
                {
                    cell.HideExpansionSlot();
                    continue;
                }

                // Staircase adjacency rule
                bool shouldShow;
                if (r == 0 && c == 0) shouldShow = false;
                else if (r == 0) shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
                else if (c == 0) shouldShow = _grid[r - 1, c].HasBlock;
                else shouldShow = _grid[r - 1, c].HasBlock && _grid[r, c - 1].HasBlock;

                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
                else cell.HideExpansionSlot();
            }
        }
    }

    // ── Unit Slots ────────────────────────────────────────────────

    /// <summary>
    /// Shows the cannon drop-zone overlay on every exposed block and applies the
    /// correct village / castle display mode.
    ///
    /// The overlay is ALWAYS kept alive across panel switches so placed cannons
    /// survive. Only the visual mode changes:
    ///
    ///   Village Panel → zone alpha = 0, CanvasGroup.blocksRaycasts = TRUE.
    ///                   Cannon drag-drop IS enabled here.
    ///   Castle Panel  → zone alpha = normalColor, CanvasGroup.blocksRaycasts = FALSE.
    ///                   Cannon drag-drop is DISABLED; expansion slots handle block-adding.
    ///
    /// HideUnitSlot() is only called when a block is genuinely removed or covered.
    /// </summary>
    public void RefreshUnitSlots()
    {
        if (_grid == null) return;

        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                GridCell cell = _grid[r, c];

                // No block here → no unit slot
                if (!cell.HasBlock)
                {
                    cell.HideUnitSlot();
                    continue;
                }

                // "Exposed" = the cell directly above this one has no block
                bool isExposed = !InBounds(r + 1, c) || !_grid[r + 1, c].HasBlock;

                if (isExposed && castleBlockUnitSlotPrefab != null)
                {
                    // Always show the slot so the cannon visual persists.
                    cell.ShowUnitSlot(castleBlockUnitSlotPrefab);

                    // Village Panel  → isVillage = true  → alpha 0, raycasts ON.
                    // Castle Panel   → isVillage = false → normal color, raycasts ON.
                    bool isVillage = !_expansionSlotsVisible;
                    cell.SetUnitSlotVillageMode(isVillage);
                }
                else
                {
                    // Block is covered — destroy the slot (no cannon can sit here).
                    cell.HideUnitSlot();
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    public bool InBounds(int r, int c) =>
        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

    public GridCell GetCell(int r, int c) =>
        InBounds(r, c) ? _grid[r, c] : null;

    /// <summary>
    /// Called immediately after a block is placed at (row, col).
    /// If the cell directly below (row-1, col) has placed units, they are
    /// migrated up into the new block's slot before RefreshUnitSlots() removes
    /// the old slot. The cannon GameObjects are never destroyed; only reparented.
    /// </summary>
    private void MigrateUnitsFromBelow(int row, int col)
    {
        if (!InBounds(row - 1, col)) return;

        GridCell belowCell = _grid[row - 1, col];
        if (!belowCell.HasBlock) return;

        bool hasUnits = false;
        foreach (var zone in belowCell.GetComponentsInChildren<CastleUnitDropZone>(true))
        {
            if (zone.HasUnit) { hasUnits = true; break; }
        }
        if (!hasUnits) return;

        GridCell newCell = _grid[row, col];

        // 1. Create the destination slot on the new block first.
        if (castleBlockUnitSlotPrefab != null)
        {
            newCell.ShowUnitSlot(castleBlockUnitSlotPrefab);
            newCell.SetUnitSlotVillageMode(!_expansionSlotsVisible);
        }

        // 2. Reparent every cannon / soldier into the new slot.
        belowCell.TransferUnitSlotTo(newCell);

        // 3. Destroy the now-empty source slot (no RemoveAll call).
        belowCell.HideUnitSlotEmpty();
    }
}