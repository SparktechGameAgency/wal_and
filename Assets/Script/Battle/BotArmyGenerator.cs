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
    [Header("Spawn Layout")]
    [SerializeField] private float startX = 0f;    // local x offset from BotArmyRoot
    [SerializeField] private float unitSpacingX = 80f;
    [SerializeField] private float unitSpacingY = 60f;
    [SerializeField] private int unitsPerRow = 3;

    [Header("Bot Stat Ranges (Soldier / Horse / Archer / Dragon)")]
    [SerializeField] private Vector2 hpRange = new Vector2(70f, 130f);
    [SerializeField] private Vector2 dmgRange = new Vector2(8f, 18f);
    [SerializeField] private Vector2 speedRange = new Vector2(60f, 110f);

    public List<BattleUnit> SpawnedUnits { get; private set; } = new List<BattleUnit>();

    // One pool entry = one spawnable unit type for the bot side.
    private struct PoolEntry
    {
        public GameObject prefab;
        public BattleUnitType type;
        public HorseData horseData;     // only set when type == Horse
        public CannonData cannonData;   // only set when type == Cannon
    }

    /// <summary>
    /// Spawns the bot army using the SAME prefabs the player uses
    /// (passed in from BattleManager) — no separate bot-only prefabs needed.
    /// BattleUnit.Init(playerUnit: false) is what makes them face/move left
    /// and target the player side; the prefab itself is identical either way.
    /// Horse/Dragon units get a randomized rider look from riderLoadouts.
    /// </summary>
    public void Generate(int playerUnitCount, BattleUnitPrefabs prefabs, RiderLoadoutPool riderLoadouts)
    {
        if (prefabs == null)
        {
            Debug.LogError("[BotArmyGenerator] No BattleUnitPrefabs passed in from BattleManager!");
            return;
        }

        // Bot army is playerCount ± 2 (min 1).
        int botCount = Mathf.Max(1, playerUnitCount + Random.Range(-2, 3));

        // Build a pool of available unit types (skip anything unassigned).
        var pool = new List<PoolEntry>();
        if (prefabs.soldierPrefab != null)
            pool.Add(new PoolEntry { prefab = prefabs.soldierPrefab, type = BattleUnitType.Soldier });
        if (prefabs.horseTypes != null)
        {
            foreach (var horse in prefabs.horseTypes)
            {
                if (horse != null && horse.prefab != null)
                    pool.Add(new PoolEntry { prefab = horse.prefab, type = BattleUnitType.Horse, horseData = horse });
            }
        }
        if (prefabs.archerPrefab != null)
            pool.Add(new PoolEntry { prefab = prefabs.archerPrefab, type = BattleUnitType.Archer });
        if (prefabs.dragonPrefab != null)
            pool.Add(new PoolEntry { prefab = prefabs.dragonPrefab, type = BattleUnitType.Dragon });
        if (prefabs.cannonTypes != null)
        {
            foreach (var cannon in prefabs.cannonTypes)
            {
                if (cannon != null && cannon.prefab != null)
                    pool.Add(new PoolEntry { prefab = cannon.prefab, type = BattleUnitType.Cannon, cannonData = cannon });
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogError("[BotArmyGenerator] No prefabs assigned on BattleUnitPrefabs!");
            return;
        }

        for (int i = 0; i < botCount; i++)
        {
            PoolEntry entry = pool[Random.Range(0, pool.Count)];
            GameObject go = Instantiate(entry.prefab, transform);

            // Grid layout — stack left-to-right then up.
            int col = i % unitsPerRow;
            int row = i / unitsPerRow;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                startX + col * unitSpacingX,
                row * unitSpacingY);

            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu == null)
            {
                // A unit prefab missing its BattleUnit component used to make
                // this silently `continue`, which could (and did) drop the
                // ENTIRE bot army down to 0 units if every randomly-picked
                // prefab in the pool happened to be missing it — leaving the
                // player side with no target to fight, looking permanently
                // "stuck" in idle/walk. Auto-add it instead of skipping, same
                // fallback BattleManager.ReceivePlayerSoldiers() already uses
                // for carried-over soldiers.
                bu = go.AddComponent<BattleUnit>();
                Debug.LogWarning($"[BotArmyGenerator] '{entry.prefab.name}' had no BattleUnit " +
                                  "component — added one at runtime. Add it to the prefab directly " +
                                  "to avoid this warning.");
            }

            BattleUnitData data = BuildData(entry, riderLoadouts);
            bu.Init(data, playerUnit: false);
            SpawnedUnits.Add(bu);
        }

        Debug.Log($"[BotArmyGenerator] Spawned {SpawnedUnits.Count} bot units " +
                  $"(player had {playerUnitCount}).");
    }

    private BattleUnitData BuildData(PoolEntry entry, RiderLoadoutPool riderLoadouts)
    {
        if (entry.type == BattleUnitType.Cannon)
        {
            // Cannons use their own CannonData base stats — no randomization,
            // same as picking a fixed type. Bots don't have upgrades.
            var cannonUnitData = new BattleUnitData(
                BattleUnitType.Cannon,
                entry.cannonData.health,
                entry.cannonData.damage,
                0f);
            cannonUnitData.cannonType = entry.cannonData;
            return cannonUnitData;
        }

        if (entry.type == BattleUnitType.Horse)
        {
            // Horses use their own HorseData base stats (Brown/Black/White each
            // have different health/damage/ability) instead of the shared
            // random range, same as picking a fixed cannon type.
            var horseUnitData = new BattleUnitData(
                BattleUnitType.Horse,
                entry.horseData.health,
                entry.horseData.damage,
                entry.horseData.ability);
            horseUnitData.horseType = entry.horseData;

            if (riderLoadouts != null)
            {
                horseUnitData.riderFace = RandomItem(riderLoadouts.faces);
                horseUnitData.riderArmor = RandomItem(riderLoadouts.armors);
                horseUnitData.riderHelmet = RandomItem(riderLoadouts.helmets);
                horseUnitData.riderWeapon = RandomItem(riderLoadouts.weapons);
            }

            return horseUnitData;
        }

        var data = new BattleUnitData(
            entry.type,
            Random.Range(hpRange.x, hpRange.y),
            Random.Range(dmgRange.x, dmgRange.y),
            Random.Range(speedRange.x, speedRange.y));

        // Dragon gets a randomized rider look so bots aren't bare
        // and don't all look identical.
        if (entry.type == BattleUnitType.Dragon && riderLoadouts != null)
        {
            data.riderFace = RandomItem(riderLoadouts.faces);
            data.riderArmor = RandomItem(riderLoadouts.armors);
            data.riderHelmet = RandomItem(riderLoadouts.helmets);
            data.riderWeapon = RandomItem(riderLoadouts.weapons);
        }

        return data;
    }

    private static EquipmentItem RandomItem(EquipmentItem[] items)
    {
        if (items == null || items.Length == 0) return null;
        return items[Random.Range(0, items.Length)];
    }
}