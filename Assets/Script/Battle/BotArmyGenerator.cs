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
    ///
    /// <paramref name="castleUnitSlots"/> — the bot castle's per-block
    /// CastleBlockUnitSlot zones (BotCastleGenerator.GeneratedUnitSlots). When
    /// a Cannon or Archer is picked from the pool, it's placed into one of
    /// these zones via CastleUnitDropZone.PlaceCannonForBattle /
    /// ArcherZoneCastle.PlaceArcherForBattle — one unit per block, same
    /// mutual-exclusion rule as the player's castle — instead of dropping
    /// into the flat army row. This is what makes the bot's cannon/archer
    /// zones the SAME components as the player's CastleBlockUnitSlot zones,
    /// not a raw prefab dropped on top. Pass null/empty to fall back to the
    /// old flat-row placement for every unit type.
    /// </summary>
    public void Generate(int playerUnitCount, BattleUnitPrefabs prefabs, RiderLoadoutPool riderLoadouts,
                          List<BotCastleGenerator.BotUnitSlot> castleUnitSlots = null)
    {
        // Local, consumable copy — one slot gets used up per cannon/archer seated.
        var availableSlots = castleUnitSlots != null
            ? new List<BotCastleGenerator.BotUnitSlot>(castleUnitSlots)
            : new List<BotCastleGenerator.BotUnitSlot>();

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

        int flatIndex = 0;

        for (int i = 0; i < botCount; i++)
        {
            PoolEntry entry = pool[Random.Range(0, pool.Count)];

            // Cannon/Archer prefer a free castle block slot so they sit ON the
            // bot's grid through the SAME CastleUnitDropZone/ArcherZoneCastle
            // components the player's castle uses, one per block.
            bool isSeatable = entry.type == BattleUnitType.Cannon || entry.type == BattleUnitType.Archer;
            BotCastleGenerator.BotUnitSlot? seatSlot = null;
            if (isSeatable && availableSlots.Count > 0)
            {
                int pick = Random.Range(0, availableSlots.Count);
                seatSlot = availableSlots[pick];
                availableSlots.RemoveAt(pick);
            }

            GameObject go = null;
            bool skipFacingFlip;

            if (seatSlot != null && entry.type == BattleUnitType.Cannon && seatSlot.Value.cannonZone != null)
            {
                go = seatSlot.Value.cannonZone.PlaceCannonForBattle(entry.prefab, entry.cannonData);
                // The zone already carries the bot castle's flipHorizontally
                // mirror (-1 scale) via its GridCell ancestor, so this unit
                // inherits the correct left-facing orientation for free —
                // flipping it again here would cancel that out.
                skipFacingFlip = true;
            }
            else if (seatSlot != null && entry.type == BattleUnitType.Archer && seatSlot.Value.archerZone != null)
            {
                go = seatSlot.Value.archerZone.PlaceArcherForBattle(entry.prefab);
                skipFacingFlip = true;
            }
            else
            {
                skipFacingFlip = false;
            }

            // Cannons AND Archers must ONLY ever end up on a castle block
            // (BotCastleRoot), never in the flat BotArmyRoot row - a cannon
            // has no walk/idle animation and doesn't make sense standing in
            // the open field, and archers are meant to sit in their castle
            // zone exactly like the player's archers do. If no free castle
            // slot was available (or the picked slot's zone was somehow
            // null), skip spawning this unit entirely instead of falling
            // through to the flat-row Instantiate() below.
            if (go == null && (entry.type == BattleUnitType.Cannon || entry.type == BattleUnitType.Archer))
            {
                continue;
            }

            if (go == null)
            {
                // No free castle slot (or zone placement failed) — fall back
                // to the flat army row, same as before.
                go = Instantiate(entry.prefab, transform);

                // Grid layout — stack left-to-right then up.
                int col = flatIndex % unitsPerRow;
                int row = flatIndex / unitsPerRow;
                flatIndex++;

                RectTransform rt = go.GetComponent<RectTransform>();

                // Horses always sit flat on the ground line — never stacked
                // into a row like foot units. Using "row * spacing" here
                // (like every other type) made a horse's Y depend on how many
                // other units happened to spawn before it in this particular
                // battle, so the SAME horse could land at y=0 in one battle
                // and y=60/120/etc. in another purely by chance of spawn
                // order — exactly why "not every horse" was sitting at y=0.
                // Forcing y=0 keeps every horse on the same ground line
                // regardless of order, same as the player-side fix.
                float y = entry.type == BattleUnitType.Horse
                    ? 0f
                    : row * unitSpacingY;
                rt.anchoredPosition = new Vector2(
                    startX + col * unitSpacingX, y);

                skipFacingFlip = false;

                // A freshly-instantiated Soldier prefab still carries its
                // own Village patrol AI (SoldierController), which would
                // otherwise start walking/resting on its own and fight
                // BattleUnit.Update() for control of this RectTransform —
                // the same conflict already fixed for carried-over player
                // soldiers in BattleManager.ReceivePlayerSoldiers(). No-ops
                // safely for every other unit type, which has no SoldierController.
                var soldierController = go.GetComponent<SoldierController>();
                if (soldierController != null)
                {
                    soldierController.StopAllCoroutines();
                    soldierController.enabled = false;
                }
            }

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
            bu.Init(data, playerUnit: false, skipFacingFlip: skipFacingFlip);
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