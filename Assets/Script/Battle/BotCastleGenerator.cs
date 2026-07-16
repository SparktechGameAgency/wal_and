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

    [Tooltip("Same prefab as CastleGrid.castleBlockUnitSlotPrefab — the one with the " +
             "CastleBlockUnitSlot component and its CannonZone/ArcherZone children. " +
             "Placed on every exposed bot block (no block above it in the same " +
             "column), exactly like CastleGrid.RefreshUnitSlots() does for the " +
             "player, so the bot's cannon/archer zones are the SAME components " +
             "(not raw prefabs dropped on top) as the player's castle.")]
    [SerializeField] private GameObject castleBlockUnitSlotPrefab;

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
    /// One entry per EXPOSED generated block (no block stacked above it in
    /// the same column) — the real CastleUnitDropZone/ArcherZoneCastle
    /// components from an instantiated castleBlockUnitSlotPrefab, exactly
    /// like the player's CastleBlockUnitSlot zones, instead of a bare
    /// RectTransform units get dropped onto. BotArmyGenerator consumes and
    /// empties this list as it seats units — one cannon OR archer per block,
    /// via CastleUnitDropZone.PlaceCannonForBattle / ArcherZoneCastle.PlaceArcherForBattle
    /// (mirrors the player's mutual-exclusion rule automatically, since both
    /// zones share the same CastleBlockUnitSlot parent).
    /// </summary>
    public struct BotUnitSlot
    {
        public CastleUnitDropZone cannonZone;
        public ArcherZoneCastle archerZone;

        /// <summary>The grid row (floor) this slot's block sits on — set by BuildGridCells.
        /// BotArmyGenerator copies this onto the seated unit's BattleUnit.CastleRow so a
        /// climbing soldier knows which floor to stop on.</summary>
        public int row;
    }

    public List<BotUnitSlot> GeneratedUnitSlots { get; private set; } = new List<BotUnitSlot>();

    [Header("Castle Door (soldiers climb through these to reach archers/cannons)")]
    [Tooltip("CastleDoor now lives as a CHILD of castleBlockPrefab itself (added directly " +
             "on that prefab in the Editor, start it SetActive(false) there) — every block " +
             "instantiated below already carries one, so there's nothing to assign here. " +
             "Only the block that sits at COLUMN 0 of each floor row ('the last castle grid' " +
             "of that floor, e.g. grid_0_0, grid_1_0, grid_2_0 ...) has its door child " +
             "re-enabled and registered; every other block's copy is explicitly disabled. " +
             "Column 0 of a row is guaranteed to exist for any row that has blocks at all — " +
             "every other column in a row chains back to it (see GenerateStaircaseShape's " +
             "adjacency rule) — which is what makes it a reliable, always-present door spot, " +
             "unlike the row's shifting frontier edge. If castleBlockPrefab has no CastleDoor " +
             "child at all, doors are skipped entirely for every row — soldiers fall back to " +
             "the old straight-line walk (see BattleUnit.Update).")]

    /// <summary>One CastleDoor per generated floor row. BattleManager.GetCastleDoorForClimb
    /// looks these up by row for a climbing soldier.</summary>
    public List<CastleDoor> GeneratedDoors { get; private set; } = new List<CastleDoor>();

    /// <summary>The door belonging to <paramref name="row"/>, or null if that row doesn't
    /// exist / castleBlockPrefab has no CastleDoor child.</summary>
    public CastleDoor GetDoor(int row) => GeneratedDoors.Find(d => d != null && d.Row == row);

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
        GeneratedUnitSlots.Clear();
        GeneratedDoors.Clear();

        // "Exposed" = same rule CastleGrid.RefreshUnitSlots uses: no block
        // stacked directly above this one in the same column. Used below to
        // decide which blocks get a CastleBlockUnitSlot (cannon/archer zones).
        var placedSet = new HashSet<Vector2Int>(positions);

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

            // ── Unit slot (matches CastleGrid.RefreshUnitSlots()) ──────
            // Only exposed blocks (nothing stacked above in this column) get
            // a cannon/archer zone — same rule the player's castle follows.
            // Requires the two-tier cell/block hierarchy (cellObj != null),
            // since the slot is placed as a SIBLING of the block under the
            // shared GridCell, exactly like the Village's real castle.
            bool isExposed = !placedSet.Contains(new Vector2Int(row + 1, col));

            if (isExposed && cellObj != null && castleBlockUnitSlotPrefab != null)
            {
                GameObject slotObj = Instantiate(castleBlockUnitSlotPrefab, cellObj.transform);
                slotObj.name = $"BotUnitSlot_{row}_{col}";

                RectTransform srt = slotObj.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero;
                srt.anchorMax = Vector2.one;
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
                srt.anchoredPosition = Vector2.zero;

                // CastleBlock and CastleBlockUnitSlot are siblings under the
                // GridCell — pushing the slot to the FIRST sibling makes the
                // block (which stays after it) render on top, same as
                // GridCell.ShowUnitSlot() does for the player's castle.
                srt.SetAsFirstSibling();

                // Bot castle is pure visuals — block ALL pointer events on
                // this slot (cannon/archer drag, click-to-open-panel, etc.)
                // with a CanvasGroup instead of disabling individual scripts,
                // since Unity's EventSystem still fires IPointerXHandler
                // methods on disabled MonoBehaviours.
                CanvasGroup slotCg = slotObj.GetComponent<CanvasGroup>();
                if (slotCg == null) slotCg = slotObj.AddComponent<CanvasGroup>();
                slotCg.interactable = false;
                slotCg.blocksRaycasts = false;

                var cannonZone = slotObj.GetComponentInChildren<CastleUnitDropZone>(true);
                var archerZone = slotObj.GetComponentInChildren<ArcherZoneCastle>(true);

                GeneratedUnitSlots.Add(new BotUnitSlot
                {
                    cannonZone = cannonZone,
                    archerZone = archerZone,
                    row = row
                });
            }

            // ── Castle door (matches the "last castle grid" of this row) ──
            // The door is a CHILD OF THE BLOCK ITSELF now (wired directly on
            // castleBlockPrefab in the Editor), not a separate prefab
            // instantiated on top — so it comes along for free with `block`
            // above. Only the block sitting at COLUMN 0 of each row (e.g.
            // grid_0_0, grid_1_0, grid_2_0 ...) keeps its door active and
            // registered; every other block's copy is disabled so it's
            // neither visible nor picked up by GetDoor(). Column 0 is used
            // (rather than the row's highest/frontier column) because it's
            // GUARANTEED to exist for any row that has blocks at all — every
            // other column in a row requires (row, col-1) to already be
            // placed, so column 0 is always the first block built in its
            // row (see GenerateStaircaseShape) — a stable, predictable door
            // spot instead of one that shifts with the random shape.
            CastleDoor blockDoor = block.GetComponentInChildren<CastleDoor>(true);
            if (blockDoor != null)
            {
                bool isLastBlockOfRow = col == 0;

                if (isLastBlockOfRow)
                {
                    blockDoor.gameObject.SetActive(true);
                    blockDoor.Init(row);
                    GeneratedDoors.Add(blockDoor);
                }
                else
                {
                    blockDoor.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log($"[BotCastleGenerator] '{name}': generated {positions.Count} blocks " +
                  $"as a {GeneratedRows}x{GeneratedCols}-box staircase " +
                  $"(cells={(gridCellPrefab != null)}, flip={flipHorizontally}).");
    }
}