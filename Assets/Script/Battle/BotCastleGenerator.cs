using UnityEngine;

/// <summary>
/// BotCastleGenerator
///
/// Instantiates a random stack of castle block Images on the RIGHT side
/// of the Battle scene canvas. The bot castle is never taller than the
/// player's castle (read from BattleSaveData.PlayerBlockCount).
///
/// Assign this to an empty RectTransform called "BotCastleRoot" on the
/// right side of the Battle scene canvas.
/// </summary>
public class BotCastleGenerator : MonoBehaviour
{
    [Tooltip("Your CastleBlock prefab — the same visual used in the Village scene.")]
    [SerializeField] private GameObject castleBlockPrefab;

    [Tooltip("Size of one block in pixels. Match your Village scene block size.")]
    [SerializeField] private float blockSize = 120f;

    [Tooltip("Gap between blocks in pixels.")]
    [SerializeField] private float blockSpacing = 4f;

    // How many blocks were generated (BattleManager reads this).
    public int GeneratedBlockCount { get; private set; }

    public void Generate(int playerBlockCount)
    {
        // Bot castle is between 1 and playerBlockCount blocks tall.
        int botBlocks = Random.Range(1, playerBlockCount + 1);
        GeneratedBlockCount = botBlocks;

        float step = blockSize + blockSpacing;

        for (int i = 0; i < botBlocks; i++)
        {
            GameObject block = Instantiate(castleBlockPrefab, transform);
            RectTransform rt = block.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, i * step);
            rt.sizeDelta = new Vector2(blockSize, blockSize);

            // Disable all interactive scripts — bot castle is pure visuals.
            foreach (var mb in block.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is CastleBlock || mb is CastleBlockHUD)
                    mb.enabled = false;
            }

            // Flip the block horizontally so the gate faces left (toward the player).
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        Debug.Log($"[BotCastleGenerator] Generated {botBlocks} bot blocks " +
                  $"(player had {playerBlockCount}).");
    }
}