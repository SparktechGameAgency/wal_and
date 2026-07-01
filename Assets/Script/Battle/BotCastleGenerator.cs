using UnityEngine;

/// <summary>
/// BotCastleGenerator
///
/// Instantiates a stack of castle block Images as a tapered staircase
/// silhouette (not a straight tower). Reused for BOTH sides:
///   • Bot side  → Generate(playerBlockCount) — random height, capped at
///     the player's block count, gate flipped to face left (toward player).
///   • Player side → GenerateExact(blockCount) — exact height (no random),
///     gate NOT flipped (default prefab orientation already faces right,
///     toward the battlefield / bot side).
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
    [SerializeField] private float blockSpacing = 4f;

    [Tooltip("Flip blocks horizontally so the gate faces the opposite side. " +
             "ON for the bot castle (gate faces left, toward the player). " +
             "OFF for the player castle (default orientation already faces right).")]
    [SerializeField] private bool flipHorizontally = true;

    [Tooltip("Horizontal shift (pixels) applied per level, tapering the stack " +
             "into a staircase/triangle silhouette instead of a straight tower. " +
             "Use a POSITIVE value on the player side (tapers right, toward the " +
             "battlefield) and a NEGATIVE value on the bot side (tapers left).")]
    [SerializeField] private float taperPerLevel = 0f;

    // How many blocks were generated (BattleManager reads this).
    public int GeneratedBlockCount { get; private set; }

    /// <summary>Bot side: random height between 1 and playerBlockCount (inclusive).</summary>
    public void Generate(int playerBlockCount)
    {
        int botBlocks = Random.Range(1, playerBlockCount + 1);
        BuildBlocks(botBlocks);
    }

    /// <summary>Player side: exact height, no randomization.</summary>
    public void GenerateExact(int blockCount)
    {
        BuildBlocks(Mathf.Max(0, blockCount));
    }

    private void BuildBlocks(int blockCount)
    {
        GeneratedBlockCount = blockCount;

        float step = blockSize + blockSpacing;

        for (int i = 0; i < blockCount; i++)
        {
            GameObject block = Instantiate(castleBlockPrefab, transform);
            RectTransform rt = block.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(i * taperPerLevel, i * step);
            rt.sizeDelta = new Vector2(blockSize, blockSize);

            // Disable all interactive scripts — battle castle is pure visuals.
            foreach (var mb in block.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is CastleBlock || mb is CastleBlockHUD)
                    mb.enabled = false;
            }

            if (flipHorizontally)
                rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        Debug.Log($"[BotCastleGenerator] '{name}': generated {blockCount} blocks " +
                  $"(taper={taperPerLevel}, flip={flipHorizontally}).");
    }
}