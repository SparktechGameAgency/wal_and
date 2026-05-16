//using UnityEngine;
//using UnityEngine.EventSystems;
//using System.Collections.Generic;

//public class CastleGrid : MonoBehaviour
//{
//    public static CastleGrid Instance { get; private set; }

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

//    [Header("Starting Block Positions")]
//    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

//    // ── Private ───────────────────────────────────────────────────
//    private GridCell[,] _grid;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//    }

//    private void Start()
//    {
//        BuildGrid();
//        PlaceDefaultBlocks();
//        RefreshExpansionSlots();
//    }

//    // ── Build full grid ───────────────────────────────────────────
//    private void BuildGrid()
//    {
//        _grid = new GridCell[totalRows, totalCols];

//        float step = cellSize + cellSpacing;

//        float totalWidth = totalCols * step - cellSpacing;
//        float totalHeight = totalRows * step - cellSpacing;
//        float startX = -totalWidth * 0.5f + cellSize * 0.5f;
//        float startY = -totalHeight * 0.5f + cellSize * 0.5f;

//        for (int r = 0; r < totalRows; r++)
//        {
//            for (int c = 0; c < totalCols; c++)
//            {
//                GameObject cellObj = Instantiate(gridCellPrefab, transform);
//                cellObj.name = $"Cell_{r}_{c}";

//                RectTransform rt = cellObj.GetComponent<RectTransform>();
//                rt.anchoredPosition = new Vector2(startX + c * step, startY + r * step);
//                rt.sizeDelta = new Vector2(cellSize, cellSize);

//                GridCell cell = cellObj.GetComponent<GridCell>();
//                cell.Init(r, c, this);
//                _grid[r, c] = cell;
//            }
//        }

//        Debug.Log($"[CastleGrid] Grid built: {totalRows} x {totalCols}");
//    }

//    // ── Place starting blocks ─────────────────────────────────────
//    private void PlaceDefaultBlocks()
//    {
//        foreach (var pos in defaultBlockPositions)
//            PlaceBlockAt(pos.x, pos.y, castleBlockPrefab);
//    }

//    // ── Place a block at row/col ──────────────────────────────────
//    public void PlaceBlockAt(int row, int col, GameObject prefab)
//    {
//        if (!InBounds(row, col)) return;

//        GridCell cell = _grid[row, col];
//        if (cell.HasBlock) return;

//        GameObject blockObj = Instantiate(prefab, cell.transform);

//        RectTransform brt = blockObj.GetComponent<RectTransform>();
//        brt.anchoredPosition = Vector2.zero;
//        brt.sizeDelta = new Vector2(cellSize, cellSize);
//        brt.anchorMin = new Vector2(0.5f, 0.5f);
//        brt.anchorMax = new Vector2(0.5f, 0.5f);
//        brt.pivot = new Vector2(0.5f, 0.5f);

//        CastleBlock block = blockObj.GetComponent<CastleBlock>();
//        cell.PlaceBlock(block);

//        RefreshExpansionSlots();
//    }

//    // ── Show expansion slots next to every block ──────────────────
//    public void RefreshExpansionSlots()
//    {
//        Vector2Int[] dirs = {
//            Vector2Int.up,
//            Vector2Int.left,
//            Vector2Int.right,
//            Vector2Int.down
//        };

//        for (int r = 0; r < totalRows; r++)
//        {
//            for (int c = 0; c < totalCols; c++)
//            {
//                GridCell cell = _grid[r, c];

//                if (cell.HasBlock)
//                {
//                    cell.HideExpansionSlot();
//                    continue;
//                }

//                bool adjacentToBlock = false;
//                foreach (var d in dirs)
//                {
//                    int nr = r + d.y;
//                    int nc = c + d.x;
//                    if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
//                    {
//                        adjacentToBlock = true;
//                        break;
//                    }
//                }

//                if (adjacentToBlock)
//                    cell.ShowExpansionSlot(expansionSlotPrefab);
//                else
//                    cell.HideExpansionSlot();
//            }
//        }
//    }

//    // ── Helpers ───────────────────────────────────────────────────
//    public bool InBounds(int r, int c) =>
//        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

//    public GridCell GetCell(int r, int c) =>
//        InBounds(r, c) ? _grid[r, c] : null;
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

    [Header("Starting Block Positions")]
    public List<Vector2Int> defaultBlockPositions = new List<Vector2Int>();

    // ── Private ───────────────────────────────────────────────────
    private GridCell[,] _grid;

    // ── Lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildGrid();
        PlaceDefaultBlocks();
        RefreshExpansionSlots();
    }

    // ── Build Grid ────────────────────────────────────────────────
    private void BuildGrid()
    {
        // Clear old cells
        if (_grid != null)
        {
            for (int r = 0; r < _grid.GetLength(0); r++)
                for (int c = 0; c < _grid.GetLength(1); c++)
                    if (_grid[r, c] != null)
                        Destroy(_grid[r, c].gameObject);
        }

        _grid = new GridCell[totalRows, totalCols];

        float step = cellSize + cellSpacing;

        // Centre the entire grid on this panel's pivot
        float gridW = totalCols * step - cellSpacing;
        float gridH = totalRows * step - cellSpacing;

        // Bottom-left corner position (relative to panel centre)
        float originX = -gridW * 0.5f + cellSize * 0.5f;
        float originY = -gridH * 0.5f + cellSize * 0.5f;

        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                // ── Instantiate as UI child ──────────────────────
                GameObject cellObj = Instantiate(gridCellPrefab, transform);
                cellObj.name = $"Cell_{r}_{c}";

                // ── Fix RectTransform ────────────────────────────
                RectTransform rt = cellObj.GetComponent<RectTransform>();

                // Anchor to centre of parent so anchoredPosition is
                // always relative to the panel centre — predictable!
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(
                    originX + c * step,
                    originY + r * step
                );

                // ── Init cell ────────────────────────────────────
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
    //public void PlaceBlockAt(int row, int col, GameObject prefab)
    //{
    //    if (!InBounds(row, col)) return;

    //    GridCell cell = _grid[row, col];
    //    if (cell.HasBlock) return;
    //    if (prefab == null) return;

    //    // Spawn block as child of the cell so it sits exactly on it
    //    GameObject blockObj = Instantiate(prefab, cell.transform);

    //    RectTransform brt = blockObj.GetComponent<RectTransform>();
    //    brt.anchorMin = new Vector2(0.5f, 0.5f);
    //    brt.anchorMax = new Vector2(0.5f, 0.5f);
    //    brt.pivot = new Vector2(0.5f, 0.5f);
    //    brt.anchoredPosition = Vector2.zero;          // centre of cell
    //    brt.sizeDelta = new Vector2(cellSize, cellSize);

    //    CastleBlock block = blockObj.GetComponent<CastleBlock>();
    //    cell.PlaceBlock(block);

    //    RefreshExpansionSlots();
    //}

    public void PlaceBlockAt(int row, int col, GameObject prefab)
    {
        if (!InBounds(row, col))
        {
            Debug.LogWarning($"[CastleGrid] Out of bounds: ({row},{col})");
            return;
        }
        if (prefab == null)
        {
            Debug.LogWarning("[CastleGrid] castleBlockPrefab is null! Assign it in Inspector.");
            return;
        }

        GridCell cell = _grid[row, col];
        if (cell.HasBlock)
        {
            Debug.LogWarning($"[CastleGrid] Cell ({row},{col}) already has a block.");
            return;
        }

        GameObject blockObj = Instantiate(prefab, cell.transform);

        RectTransform brt = blockObj.GetComponent<RectTransform>();
        if (brt == null)
        {
            Debug.LogWarning("[CastleGrid] CastleBlock prefab has no RectTransform!");
            return;
        }

        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(cellSize, cellSize);

        CastleBlock block = blockObj.GetComponent<CastleBlock>();
        cell.PlaceBlock(block);

        Debug.Log($"[CastleGrid] Block placed at ({row},{col})");
        RefreshExpansionSlots();
    }

    // ── Expansion Slots ───────────────────────────────────────────
    //public void RefreshExpansionSlots()
    //{
    //    // 4 cardinal neighbours
    //    Vector2Int[] dirs =
    //    {
    //        Vector2Int.up,
    //        Vector2Int.down,
    //        Vector2Int.left,
    //        Vector2Int.right
    //    };

    //    for (int r = 0; r < totalRows; r++)
    //    {
    //        for (int c = 0; c < totalCols; c++)
    //        {
    //            GridCell cell = _grid[r, c];

    //            // Cells with a block never show a slot
    //            if (cell.HasBlock)
    //            {
    //                cell.HideExpansionSlot();
    //                continue;
    //            }

    //            // Show slot only if at least one neighbour has a block
    //            bool nextToBlock = false;
    //            foreach (var d in dirs)
    //            {
    //                int nr = r + d.y, nc = c + d.x;
    //                if (InBounds(nr, nc) && _grid[nr, nc].HasBlock)
    //                {
    //                    nextToBlock = true;
    //                    break;
    //                }
    //            }

    //            if (nextToBlock) cell.ShowExpansionSlot(expansionSlotPrefab);
    //            else cell.HideExpansionSlot();
    //        }
    //    }
    //}

    public void RefreshExpansionSlots()
    {
        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                GridCell cell = _grid[r, c];

                if (cell.HasBlock)
                {
                    cell.HideExpansionSlot();
                    continue;
                }

                bool shouldShow = false;

                if (r == 0 && c == 0)
                {
                    // Anchor corner — never an expansion slot
                    shouldShow = false;
                }
                else if (r == 0)
                {
                    // Bottom row — only expand RIGHT
                    // Left neighbour must have a block
                    shouldShow = InBounds(r, c - 1) && _grid[r, c - 1].HasBlock;
                }
                else if (c == 0)
                {
                    // Left column — only expand UP
                    // Cell below must have a block
                    shouldShow = _grid[r - 1, c].HasBlock;
                }
                else
                {
                    // All other cells — BOTH below AND left must have blocks
                    // This enforces the staircase / half-triangle shape
                    bool belowHasBlock = _grid[r - 1, c].HasBlock;
                    bool leftHasBlock = _grid[r, c - 1].HasBlock;
                    shouldShow = belowHasBlock && leftHasBlock;
                }

                if (shouldShow) cell.ShowExpansionSlot(expansionSlotPrefab);
                else cell.HideExpansionSlot();
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────
    public bool InBounds(int r, int c) =>
        r >= 0 && r < totalRows && c >= 0 && c < totalCols;

    public GridCell GetCell(int r, int c) =>
        InBounds(r, c) ? _grid[r, c] : null;
}