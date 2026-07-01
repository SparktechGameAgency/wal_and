using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BotArmyGenerator
///
/// Spawns a randomized enemy army on the RIGHT side of the Battle scene.
/// The bot army is scaled to be roughly even with the player's army size
/// (±2 units) so fights feel fair but unpredictable.
///
/// Assign to an empty RectTransform called "BotArmyRoot".
/// </summary>
public class BotArmyGenerator : MonoBehaviour
{
    [Header("Bot Unit Prefabs")]
    [Tooltip("One prefab per unit type — must each have a BattleUnit component.")]
    [SerializeField] private GameObject botSoldierPrefab;
    [SerializeField] private GameObject botMountedPrefab;
    [SerializeField] private GameObject botArcherPrefab;
    [SerializeField] private GameObject botDragonPrefab;
    [SerializeField] private GameObject botCannonPrefab;   // optional

    [Header("Spawn Layout")]
    [SerializeField] private float startX = 0f;    // local x offset from BotArmyRoot
    [SerializeField] private float unitSpacingX = 80f;
    [SerializeField] private float unitSpacingY = 60f;
    [SerializeField] private int unitsPerRow = 3;

    [Header("Bot Stat Ranges")]
    [SerializeField] private Vector2 hpRange = new Vector2(70f, 130f);
    [SerializeField] private Vector2 dmgRange = new Vector2(8f, 18f);
    [SerializeField] private Vector2 speedRange = new Vector2(60f, 110f);

    public List<BattleUnit> SpawnedUnits { get; private set; } = new List<BattleUnit>();

    public void Generate(int playerUnitCount)
    {
        // Bot army is playerCount ± 2 (min 1).
        int botCount = Mathf.Max(1, playerUnitCount + Random.Range(-2, 3));

        // Build a pool of available prefabs (skip null ones).
        var pool = new List<GameObject>();
        if (botSoldierPrefab != null) pool.Add(botSoldierPrefab);
        if (botMountedPrefab != null) pool.Add(botMountedPrefab);
        if (botArcherPrefab != null) pool.Add(botArcherPrefab);
        if (botDragonPrefab != null) pool.Add(botDragonPrefab);

        if (pool.Count == 0)
        {
            Debug.LogError("[BotArmyGenerator] No bot unit prefabs assigned!");
            return;
        }

        for (int i = 0; i < botCount; i++)
        {
            GameObject prefab = pool[Random.Range(0, pool.Count)];
            GameObject go = Instantiate(prefab, transform);

            // Grid layout — stack left-to-right then up.
            int col = i % unitsPerRow;
            int row = i / unitsPerRow;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                startX + col * unitSpacingX,
                row * unitSpacingY);

            // Assign random stats.
            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu != null)
            {
                var data = new BattleUnitData(
                    BattleUnitType.Soldier,           // type doesn't change stats here
                    Random.Range(hpRange.x, hpRange.y),
                    Random.Range(dmgRange.x, dmgRange.y),
                    Random.Range(speedRange.x, speedRange.y));
                bu.Init(data, playerUnit: false);
                SpawnedUnits.Add(bu);
            }
        }

        Debug.Log($"[BotArmyGenerator] Spawned {botCount} bot units " +
                  $"(player had {playerUnitCount}).");
    }
}