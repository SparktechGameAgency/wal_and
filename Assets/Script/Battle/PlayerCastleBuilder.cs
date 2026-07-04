using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PlayerCastleBuilder
///
/// Rebuilds the player's ACTUAL castle in the Battle scene — the exact
/// half-triangle/staircase shape built in the Village's CastleGrid — instead
/// of the old fake single-column stack (BotCastleGenerator.GenerateExact).
///
/// Reads BattleSaveData.PlayerBlockPositions (the (row, col) of every block
/// the player placed) and instantiates one castleBlockPrefab per position,
/// using the same (col, row) → pixel math CastleGrid uses, just anchored to
/// this root's bottom-left corner instead of a full grid of empty cells.
///
/// Exposes GetBlockTransform(row, col) so BattleManager can seat cannon and
/// archer units directly on the matching block — the whole castle, cannons
/// and archers included, "shifts" from the Village into the Battle scene.
///
/// Assign to "PlayerCastleRoot" in the Battle scene Canvas, replacing the
/// old BotCastleGenerator (flipHorizontally OFF) reference.
/// </summary>
public class PlayerCastleBuilder : MonoBehaviour
{
    [Tooltip("Same CastleBlock prefab used in the Village scene.")]
    [SerializeField] private GameObject castleBlockPrefab;

    [Tooltip("Must match CastleGrid.cellSize in the Village scene.")]
    [SerializeField] private float cellSize = 120f;

    [Tooltip("Must match CastleGrid.cellSpacing in the Village scene.")]
    [SerializeField] private float cellSpacing = 4f;

    private readonly Dictionary<Vector2Int, RectTransform> _blocks =
        new Dictionary<Vector2Int, RectTransform>();

    /// <summary>Number of blocks actually built (used to size the bot's castle).</summary>
    public int BuiltBlockCount => _blocks.Count;

    /// <summary>
    /// Instantiates one block per saved grid position, preserving the exact
    /// staircase shape the player built in the Village panel. Safe to call
    /// again (e.g. on retry) — clears any previous blocks first.
    /// </summary>
    public void Build(List<Vector2Int> blockPositions)
    {
        Clear();

        if (castleBlockPrefab == null)
        {
            Debug.LogWarning("[PlayerCastleBuilder] castleBlockPrefab is not assigned!");
            return;
        }

        if (blockPositions == null || blockPositions.Count == 0)
        {
            Debug.LogWarning("[PlayerCastleBuilder] No saved block positions — nothing to build.");
            return;
        }

        float step = cellSize + cellSpacing;

        // Center the rebuilt castle the same way CastleGrid centers its grid,
        // instead of planting it at PlayerCastleRoot's bottom-left corner.
        // Uses the actual span of the saved positions so it centers correctly
        // no matter how large the player's castle grew.
        int maxRow = 0, maxCol = 0;
        foreach (var pos in blockPositions)
        {
            if (pos.x > maxRow) maxRow = pos.x;
            if (pos.y > maxCol) maxCol = pos.y;
        }
        float gridW = (maxCol + 1) * step - cellSpacing;
        float gridH = (maxRow + 1) * step - cellSpacing;
        float originX = -gridW * 0.5f + cellSize * 0.5f;
        float originY = -gridH * 0.5f + cellSize * 0.5f;

        foreach (var pos in blockPositions)
        {
            // pos.x = row, pos.y = col — same convention as CastleGrid.
            int row = pos.x;
            int col = pos.y;

            GameObject block = Instantiate(castleBlockPrefab, transform);
            block.name = $"PlayerBlock_{row}_{col}";

            RectTransform rt = block.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(cellSize, cellSize);
            rt.anchoredPosition = new Vector2(originX + col * step, originY + row * step);

            // Battle castle blocks are pure visuals — disable Village
            // interaction scripts exactly like BotCastleGenerator did.
            foreach (var mb in block.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is CastleBlock || mb is CastleBlockHUD)
                    mb.enabled = false;
            }

            _blocks[pos] = rt;
        }

        Debug.Log($"[PlayerCastleBuilder] Built {_blocks.Count} blocks in the exact Village shape.");
    }

    /// <summary>The block RectTransform at (row, col), or null if nothing was placed there.</summary>
    public RectTransform GetBlockTransform(int row, int col)
    {
        _blocks.TryGetValue(new Vector2Int(row, col), out RectTransform rt);
        return rt;
    }

    private void Clear()
    {
        foreach (var kv in _blocks)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _blocks.Clear();
    }
}