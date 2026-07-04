using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BotCastleGenerator
///
/// Builds the bot castle as a real staircase/half-triangle shape — the same
/// shape a player's own castle naturally forms in CastleGrid, where a block
/// can only be added on top of an existing block or next to one already on
/// the ground row. The castle is sized off the PLAYER's actual dimensions
/// (rows x cols), each axis independently nudged by -1, 0, or +1, then grown
/// into a staircase using CastleGrid's own adjacency rule — so it is never a
/// solid rectangle and never the same rigid diagonal every time.
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
    [Tooltip("Your CastleBlock prefab — the same visual used in the Village scene.")]
    [SerializeField] private GameObject castleBlockPrefab;

    [Tooltip("Size of one block in pixels. Match your Village scene block size.")]
    [SerializeField] private float blockSize = 120f;

    [Tooltip("Gap between blocks in pixels.")]
    [SerializeField] private float blockSpacing = 0f;

    [Tooltip("Flip blocks horizontally so the gate faces the opposite side. " +
             "ON for the bot castle (gate faces left, toward the player). " +
             "OFF for the player castle (default orientation already faces right).")]
    [SerializeField] private bool flipHorizontally = true;

    [Tooltip("How far each axis (rows, cols) is allowed to drift from the " +
             "player's own castle dimensions. 1 = -1/0/+1 per axis.")]
    [SerializeField] private int dimensionVariance = 1;

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
        BuildBlocks(shape);
    }

    /// <summary>Player side: exact dimensions, still a staircase shape (no randomization).</summary>
    public void GenerateExact(int rows, int cols)
    {
        List<Vector2Int> shape = GenerateStaircaseShape(Mathf.Max(1, rows), Mathf.Max(1, cols));
        BuildBlocks(shape);
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
    /// Instantiates one block per grid position, centering the whole shape
    /// the same way CastleGrid/PlayerCastleBuilder do. flipHorizontally
    /// mirrors both the column axis (so the shape climbs toward the left)
    /// and the block sprite itself (so the gate art faces the player).
    /// </summary>
    private void BuildBlocks(List<Vector2Int> positions)
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

        int maxRow = 0, maxCol = 0;
        foreach (var pos in positions)
        {
            if (pos.x > maxRow) maxRow = pos.x;
            if (pos.y > maxCol) maxCol = pos.y;
        }

        float gridW = (maxCol + 1) * step - blockSpacing;
        float gridH = (maxRow + 1) * step - blockSpacing;
        float originX = -gridW * 0.5f + blockSize * 0.5f;
        float originY = -gridH * 0.5f + blockSize * 0.5f;

        foreach (var pos in positions)
        {
            int row = pos.x;
            int col = pos.y;

            GameObject block = Instantiate(castleBlockPrefab, transform);
            block.name = $"BotBlock_{row}_{col}";

            RectTransform rt = block.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(blockSize, blockSize);

            // Mirror the column axis when flipped so the staircase climbs
            // toward the left (toward the player) instead of the right.
            float x = flipHorizontally ? -(originX + col * step) : (originX + col * step);
            float y = originY + row * step;
            rt.anchoredPosition = new Vector2(x, y);

            // Disable all interactive scripts — battle castle is pure visuals.
            foreach (var mb in block.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is CastleBlock || mb is CastleBlockHUD)
                    mb.enabled = false;
            }

            if (flipHorizontally)
                rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        Debug.Log($"[BotCastleGenerator] '{name}': generated {positions.Count} blocks " +
                  $"as a staircase inside a {maxRow + 1}x{maxCol + 1} box (flip={flipHorizontally}).");
    }
}