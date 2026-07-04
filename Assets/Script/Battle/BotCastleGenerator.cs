using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// BotCastleGenerator
///
/// Builds the bot castle using the SAME grid-layout structure as the
/// Village's CastleGrid: for every generated position, an invisible
/// GridCell container is placed on the grid, and the actual castle block
/// is instantiated as its child, centered at (0,0) — exactly like
/// CastleGrid.BuildGrid() + PlaceBlockAt() does. This keeps the bot castle
/// structurally identical to the player's castle instead of dropping raw
/// block sprites straight onto computed coordinates.
///
/// The shape itself is still a random staircase/half-triangle grown with
/// CastleGrid's own adjacency rule (a cell can only get a block if the cell
/// below it or to its left already has one), sized off the PLAYER's actual
/// dimensions (rows x cols), each axis independently nudged by -1, 0, or +1.
///
/// Reused for BOTH sides:
///   • Bot side    → Generate(playerRows, playerCols) — randomized +/-1 per
///     axis, gate flipped to face left (toward the player).
///   • Player side → GenerateExact(rows, cols) — exact dimensions, no
///     randomization, gate NOT flipped.
///
/// Assign one instance to "PlayerCastleRoot" (flipHorizontally = false) and
/// another to "BotCastleRoot" (flipHorizontally = true, the original setup).
/// </summary>
public class BotCastleGenerator : MonoBehaviour
{
    [Tooltip("Same invisible layout-cell prefab used by CastleGrid (has GridCell, " +
             "Image, Box Collider 2D, Button). Leave empty to fall back to placing " +
             "block sprites directly with no cell wrapper.")]
    [SerializeField] private GameObject gridCellPrefab;

    [Tooltip("Your CastleBlock prefab — the same visual used in the Village scene.")]
    [SerializeField] private GameObject castleBlockPrefab;

    [Tooltip("Size of one cell/block in pixels. Match your Village scene's CastleGrid.cellSize.")]
    [SerializeField] private float blockSize = 120f;

    [Tooltip("Gap between cells in pixels. Match your Village scene's CastleGrid.cellSpacing.")]
    [SerializeField] private float blockSpacing = 0f;

    [Tooltip("Flip the castle horizontally so the gate faces the opposite side. " +
             "ON for the bot castle (gate faces left, toward the player). " +
             "OFF for the player castle (default orientation already faces right).")]
    [SerializeField] private bool flipHorizontally = true;

    [Tooltip("How far each axis (rows, cols) is allowed to drift from the " +
             "player's own castle dimensions. 1 = -1/0/+1 per axis.")]
    [SerializeField] private int dimensionVariance = 1;

    [Header("Fixed Grid Capacity (must match Village's CastleGrid)")]
    [Tooltip("Same value as CastleGrid.totalRows in the Village scene. Used ONLY " +
             "to compute the origin so the coordinate system never changes size " +
             "between battles — NOT used to clamp/limit the generated shape.")]
    [SerializeField] private int fixedTotalRows = 6;

    [Tooltip("Same value as CastleGrid.totalCols in the Village scene. Used ONLY " +
             "to compute the origin so the coordinate system never changes size " +
             "between battles — NOT used to clamp/limit the generated shape.")]
    [SerializeField] private int fixedTotalCols = 8;

    [Range(0.2f, 1f)]
    [Tooltip("How much of the bounding rows x cols box gets filled. 0.5 gives " +
             "a classic half-triangle staircase; higher values look chunkier, " +
             "1.0 would fill the whole rectangle.")]
    [SerializeField] private float fillRatio = 0.5f;

    // How many blocks were generated (BattleManager reads this if needed).
    public int GeneratedBlockCount { get; private set; }

    // The bounding rows/cols the staircase was grown inside of.
    public int GeneratedRows { get; private set; }
    public int GeneratedCols { get; private set; }

    /// <summary>
    /// Bot side: takes the player's real castle dimensions, nudges each axis
    /// independently by -dimensionVariance..+dimensionVariance (clamped to at
    /// least 1), then grows a random staircase/half-triangle shape inside
    /// that bounding box using CastleGrid's own adjacency rule.
    /// </summary>
    public void Generate(int playerRows, int playerCols)
    {
        int botRows = Mathf.Max(1, playerRows + Random.Range(-dimensionVariance, dimensionVariance + 1));
        int botCols = Mathf.Max(1, playerCols + Random.Range(-dimensionVariance, dimensionVariance + 1));

        List<Vector2Int> shape = GenerateStaircaseShape(botRows, botCols);
        BuildGridCells(shape);
    }

    /// <summary>Player side: exact dimensions, still a staircase shape (no randomization).</summary>
    public void GenerateExact(int rows, int cols)
    {
        List<Vector2Int> shape = GenerateStaircaseShape(Mathf.Max(1, rows), Mathf.Max(1, cols));
        BuildGridCells(shape);
    }

    /// <summary>
    /// Grows a random staircase inside a rows x cols bounding box using the
    /// exact same adjacency rule as CastleGrid.RefreshExpansionSlots:
    /// (0,0) is free; a cell in row 0 needs its left neighbor filled; a cell
    /// in col 0 needs the cell below it filled; any other cell needs BOTH
    /// its left neighbor and the cell below it filled. Stops once fillRatio
    /// of the box is filled, which is what naturally produces the classic
    /// half-triangle silhouette instead of a solid rectangle.
    /// x = row, y = col — same convention as CastleGrid.
    /// </summary>
    private List<Vector2Int> GenerateStaircaseShape(int rows, int cols)
    {
        GeneratedRows = rows;
        GeneratedCols = cols;

        int targetCount = Mathf.Clamp(Mathf.RoundToInt(rows * cols * fillRatio), 1, rows * cols);

        var placed = new HashSet<Vector2Int>();
        var result = new List<Vector2Int>();

        Vector2Int origin = new Vector2Int(0, 0);
        placed.Add(origin);
        result.Add(origin);

        while (result.Count < targetCount)
        {
            var frontier = new List<Vector2Int>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var pos = new Vector2Int(r, c);
                    if (placed.Contains(pos)) continue;

                    bool valid;
                    if (r == 0 && c == 0) valid = false; // origin already placed
                    else if (r == 0) valid = placed.Contains(new Vector2Int(0, c - 1));
                    else if (c == 0) valid = placed.Contains(new Vector2Int(r - 1, c));
                    else valid = placed.Contains(new Vector2Int(r - 1, c)) &&
                                 placed.Contains(new Vector2Int(r, c - 1));

                    if (valid) frontier.Add(pos);
                }
            }

            if (frontier.Count == 0) break; // box fully filled or boxed in — stop

            Vector2Int pick = frontier[Random.Range(0, frontier.Count)];
            placed.Add(pick);
            result.Add(pick);
        }

        return result;
    }

    /// <summary>
    /// Builds the castle using the same two-tier structure as
    /// CastleGrid.BuildGrid() + PlaceBlockAt(): one invisible GridCell
    /// container per grid position, with the actual block instantiated as
    /// its child at anchoredPosition zero. flipHorizontally mirrors the
    /// column axis (so the shape climbs toward the left) by flipping the
    /// CELL's position and scale — the block inside just rides along at
    /// (0,0), exactly like the real castle.
    /// </summary>
    private void BuildGridCells(List<Vector2Int> positions)
    {
        GeneratedBlockCount = positions.Count;

        if (castleBlockPrefab == null)
        {
            Debug.LogWarning("[BotCastleGenerator] castleBlockPrefab is not assigned!");
            return;
        }

        if (positions.Count == 0)
        {
            Debug.LogWarning("[BotCastleGenerator] No blocks to build.");
            return;
        }

        float step = blockSize + blockSpacing;

        // Anchor the origin off a FIXED grid capacity (fixedTotalRows x
        // fixedTotalCols — same numbers as CastleGrid.totalRows/totalCols
        // in the Village) using the exact same centering formula
        // CastleGrid.BuildGrid() uses, instead of centering around THIS
        // shape's own bounding box (GeneratedRows x GeneratedCols).
        //
        // GeneratedRows/GeneratedCols change every battle because of the
        // +/-1 dimensionVariance, so if the origin were computed from them,
        // the whole coordinate system would grow/shrink by half a cell each
        // time and row 0 would land at a different height every battle —
        // that's what made the castle look like it was floating/flying.
        // Anchoring off the fixed capacity instead keeps the coordinate
        // system a constant size, so BotCastleRoot's Transform position
        // (tuned once in the Inspector) lines up with the ground on every
        // single battle, no matter how big the generated shape ends up.
        float gridW = fixedTotalCols * step - blockSpacing;
        float gridH = fixedTotalRows * step - blockSpacing;
        float originX = -gridW * 0.5f + blockSize * 0.5f;
        float originY = -gridH * 0.5f + blockSize * 0.5f;

        if (gridCellPrefab == null)
        {
            Debug.LogWarning("[BotCastleGenerator] gridCellPrefab is not assigned — " +
                              "falling back to flat block placement with NO cell wrapper. " +
                              "Assign your Village's cell prefab to use the real grid-cell layout.");
        }

        foreach (var pos in positions)
        {
            int row = pos.x;
            int col = pos.y;

            // Mirror the column axis when flipped so the staircase climbs
            // toward the left (toward the player) instead of the right.
            float x = flipHorizontally ? -(originX + col * step) : (originX + col * step);
            float y = originY + row * step;

            // ── Cell container (matches CastleGrid.BuildGrid()) ────────
            Transform blockParent;
            GameObject cellObj = null;

            if (gridCellPrefab != null)
            {
                cellObj = Instantiate(gridCellPrefab, transform);
                cellObj.name = $"Cell_{row}_{col}";

                RectTransform crt = cellObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(blockSize, blockSize);
                crt.anchoredPosition = new Vector2(x, y);

                if (flipHorizontally)
                    crt.localScale = new Vector3(-1f, 1f, 1f);

                // Purely visual on the battle side — no clicking/expanding.
                Button cellButton = cellObj.GetComponent<Button>();
                if (cellButton != null) cellButton.enabled = false;

                Collider2D cellCollider = cellObj.GetComponent<Collider2D>();
                if (cellCollider != null) cellCollider.enabled = false;

                GridCell gridCellScript = cellObj.GetComponent<GridCell>();
                if (gridCellScript != null) gridCellScript.enabled = false;

                blockParent = cellObj.transform;
            }
            else
            {
                // No cell prefab assigned — fall back to placing the block directly.
                blockParent = transform;
            }

            // ── Block (matches CastleGrid.PlaceBlockAt()) ──────────────
            GameObject block = Instantiate(castleBlockPrefab, blockParent);
            block.name = $"BotBlock_{row}_{col}";

            RectTransform brt = block.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(blockSize, blockSize);

            if (cellObj != null)
            {
                // Sits centered inside its cell — the cell already carries
                // the grid position (and the flip), just like the real castle.
                brt.anchoredPosition = Vector2.zero;
            }
            else
            {
                brt.anchoredPosition = new Vector2(x, y);
                if (flipHorizontally)
                    brt.localScale = new Vector3(-1f, 1f, 1f);
            }

            // Disable all interactive scripts — battle castle is pure visuals.
            foreach (var mb in block.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is CastleBlock || mb is CastleBlockHUD)
                    mb.enabled = false;
            }
        }

        Debug.Log($"[BotCastleGenerator] '{name}': generated {positions.Count} blocks " +
                  $"as a {GeneratedRows}x{GeneratedCols}-box staircase " +
                  $"(cells={(gridCellPrefab != null)}, flip={flipHorizontally}).");
    }
}