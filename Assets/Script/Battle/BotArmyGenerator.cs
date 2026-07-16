using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BotArmyGenerator
///
/// Spawns a randomized enemy army on the RIGHT side of the Battle scene.
/// The bot army size is scaled off the player's army size by a
/// difficulty-tuned random offset (see BotDifficulty / CurrentBotCountOffset)
/// so fights feel fair but unpredictable, and scale up/down with the chosen
/// difficulty level.
///
/// Assign to an empty RectTransform called "BotArmyRoot".
/// </summary>
public class BotArmyGenerator : MonoBehaviour
{
    public enum BotDifficulty
    {
        ExtraEasy,
        Easy,
        Medium,
        Hard
    }

    [Header("Difficulty")]
    [Tooltip("Set in the Inspector (or from code via the Difficulty property " +
             "before calling Generate) to control how large the bot army is.")]
    [SerializeField] private BotDifficulty difficulty = BotDifficulty.Medium;

    [Tooltip("Bot army size = player army size + a random value from this " +
             "range (inclusive), clamped to a minimum of 1. Use negative " +
             "values to keep the bot army smaller than the player's.")]
    [SerializeField] private Vector2Int easyBotCountOffset = new Vector2Int(-4, -2);

    [Tooltip("Same as above, used when difficulty is set to Medium.")]
    [SerializeField] private Vector2Int mediumBotCountOffset = new Vector2Int(-2, 2);

    [Tooltip("Same as above, used when difficulty is set to Hard.")]
    [SerializeField] private Vector2Int hardBotCountOffset = new Vector2Int(2, 5);

    // Lets a difficulty-select menu set this at runtime before Generate() is
    // called, without needing a reference to the SerializeField directly.
    public BotDifficulty Difficulty
    {
        get => difficulty;
        set => difficulty = value;
    }

    private Vector2Int CurrentBotCountOffset
    {
        get
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy: return easyBotCountOffset;
                case BotDifficulty.Hard: return hardBotCountOffset;
                // ExtraEasy never reads this — Generate() hard-codes botCount = 1
                // for it before this property would otherwise be consulted.
                default: return mediumBotCountOffset;
            }
        }
    }

    [Header("Spawn Layout")]
    [SerializeField] private float startX = 0f;    // local x offset from BotArmyRoot
    [SerializeField] private float unitSpacingX = 80f;
    [SerializeField] private float unitSpacingY = 60f;
    [SerializeField] private int unitsPerRow = 3;
    [Tooltip("Fixed ground-line Y (local to BotArmyRoot) for freshly-spawned bot " +
             "Soldiers — BotArmyRoot and PlayerArmyRoot are different RectTransforms, " +
             "so the same anchoredPosition.y does NOT land at the same on-screen height " +
             "in both. Player soldiers get their ground line for free from the live " +
             "carried-over GameObject (see BattleManager.ReceivePlayerSoldiers, -35), but " +
             "a bot soldier is instantiated fresh here with no such reference, and used " +
             "to float above the ground line at anchoredPosition.y = 0 without this. " +
             "Tune this in the Inspector against BotArmyRoot if -35 isn't pixel-perfect.")]
    [SerializeField] private float soldierGroundOffsetY = -35f;

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
    /// Horse/Dragon units get a randomized rider look from riderLoadouts, and
    /// Soldier units get a randomized armor/helmet/weapon outfit from the
    /// same pool.
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

        // Bot army is playerCount + a difficulty-tuned random offset (min 1).
        // Easy skews negative (fewer bots), Medium is roughly even (±2, same
        // as before difficulty was added), Hard skews positive (more bots).
        //
        // ExtraEasy is a special case, not just "Easy with a bigger negative
        // offset" — it must ALWAYS spawn exactly one bot unit regardless of
        // how large the player's army is (a 12-soldier player army still
        // only faces one lone cannon/horse/etc.), so it skips the
        // offset-based scaling entirely instead of going through
        // CurrentBotCountOffset like every other difficulty.
        int botCount;
        if (difficulty == BotDifficulty.ExtraEasy)
        {
            botCount = 1;
        }
        else
        {
            Vector2Int offset = CurrentBotCountOffset;
            botCount = Mathf.Max(1, playerUnitCount + Random.Range(offset.x, offset.y + 1));
        }

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

        // ExtraEasy always faces exactly one bot unit (botCount forced to 1
        // above), and that lone unit should only ever be a Soldier, Archer,
        // or Cannon — a single Horse or Dragon is a much bigger, more mobile
        // threat than the "barely a fight" ExtraEasy tier is meant to offer.
        // Restrict the pool it randomly picks from instead of reusing the
        // full pool every other difficulty draws from.
        List<PoolEntry> activePool = pool;
        if (difficulty == BotDifficulty.ExtraEasy)
        {
            activePool = pool.FindAll(p => p.type == BattleUnitType.Soldier ||
                                            p.type == BattleUnitType.Archer ||
                                            p.type == BattleUnitType.Cannon);

            // Fail-safe: if none of those 3 types have a prefab assigned
            // (unlikely, but possible on a half-configured BattleUnitPrefabs),
            // fall back to the full pool instead of spawning nothing.
            if (activePool.Count == 0)
                activePool = pool;
        }

        int flatIndex = 0;

        for (int i = 0; i < botCount; i++)
        {
            PoolEntry entry = activePool[Random.Range(0, activePool.Count)];

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
                //
                // worldPositionStays: false — the 2-arg Instantiate(prefab,
                // parent) overload defaults to worldPositionStays: TRUE,
                // which tries to preserve the prefab's original world
                // position while reparenting it under BotArmyRoot (deep in a
                // scaled Canvas hierarchy) and back-solves into garbage
                // localPosition values — same failure mode already documented
                // in BattleManager.ReceivePlayerDragons ("Y landing around
                // -2,480,058"). Passing false makes the new instance start at
                // the prefab's own local origin under the new parent instead,
                // which the anchoredPosition3D line below then overwrites
                // with a known-good value anyway.
                go = Instantiate(entry.prefab, transform, false);

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
                // Soldiers, like horses, always sit on a fixed ground line
                // instead of stacking row-by-row — matches the player side's
                // carried-over soldiers, which are snapped to a fixed
                // anchoredPosition.y (-8) in BattleManager.ReceivePlayerSoldiers
                // regardless of where they were patrolling in the Village.
                // -16 is the bot-side equivalent ground line for a freshly
                // spawned (not carried-over) soldier.
                float y;
                if (entry.type == BattleUnitType.Horse)
                    y = 0f;
                else if (entry.type == BattleUnitType.Soldier)
                    y = soldierGroundOffsetY;
                else
                    y = row * unitSpacingY;

                // anchoredPosition (Vector2) only ever wrote X/Y — it never
                // touches localPosition.z, so any leftover garbage Z from a
                // bad reparent used to survive untouched. Setting
                // anchoredPosition3D explicitly zeroes Z too. localScale is
                // reset for the same "don't trust whatever Instantiate left
                // behind" reason the dragon carry-over path already resets it.
                rt.anchoredPosition3D = new Vector3(
                    startX + col * unitSpacingX, y, 0f);
                rt.localScale = Vector3.one;

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

            // Only cannons/archers actually seated on a castle block have a
            // floor a climbing soldier needs to reach — everyone else
            // (flat-row fallback, or non-seatable types) keeps CastleRow at
            // its default (-1), which BattleUnit.Update reads as "not on a
            // castle block, don't bother climbing for this one."
            if (seatSlot != null)
                bu.SetCastleRow(seatSlot.Value.row);

            SpawnedUnits.Add(bu);
        }

        Debug.Log($"[BotArmyGenerator] Spawned {SpawnedUnits.Count} bot units " +
                  $"(player had {playerUnitCount}, difficulty: {difficulty}).");
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

        // Bot Soldiers get a randomized armor/helmet/weapon outfit too, for
        // the same "don't all look identical" reason — without this they
        // fall back to the soldier prefab's own CharacterEquipment
        // defaultLoadout and every bot soldier spawns wearing the exact
        // same default outfit. No riderFace here — a soldier's face isn't
        // meant to be swapped like a mounted rider's is; only its worn
        // equipment. BattleUnit.ApplyRiderVisuals() equips these straight
        // onto the soldier's OWN CharacterEquipment (not a throwaway one)
        // since a Soldier prefab already carries that component itself.
        if (entry.type == BattleUnitType.Soldier && riderLoadouts != null)
        {
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