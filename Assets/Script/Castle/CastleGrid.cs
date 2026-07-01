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
    /// Returns the total number of castle blocks currently placed.
    /// Used by BattleStarter to tell the bot how large to make its castle.
    /// </summary>
    public int GetPlacedBlockCount()
    {
        int count = 0;
        for (int r = 0; r < totalRows; r++)
            for (int c = 0; c < totalCols; c++)
                if (_grid[r, c] != null && _grid[r, c].HasBlock)
                    count++;
        return count;
    }

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
        if (!hasUnits)
        {
            foreach (var archer in belowCell.GetComponentsInChildren<ArcherZoneCastle>(true))
            {
                if (archer.IsOccupied) { hasUnits = true; break; }
            }
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