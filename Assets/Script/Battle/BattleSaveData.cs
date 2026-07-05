using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BattleSaveData
///
/// Static snapshot of the player's army AND castle taken the moment they
/// press Start Battle in the Village panel. Survives the scene load because
/// it's a static class — no MonoBehaviour, no DontDestroyOnLoad needed.
///
/// The Battle scene reads this once in BattleManager.Start() to rebuild the
/// player's real castle shape (via PlayerCastleBuilder) and to spawn the
/// player's army — including seating cannons/archers back on the exact
/// block they were stationed on in the Village.
/// </summary>
public static class BattleSaveData
{
    // ── Castle ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Number of castle blocks the player has placed. Still used to size the
    /// bot's random castle height (Random.Range(1, blockCount+1)).
    /// </summary>
    public static int PlayerBlockCount;

    /// <summary>
    /// Exact (row, col) of every block the player placed — the real
    /// half-triangle/staircase shape. PlayerCastleBuilder rebuilds this
    /// exact silhouette in the Battle scene instead of a generic stack.
    /// </summary>
    public static List<Vector2Int> PlayerBlockPositions = new List<Vector2Int>();

    // ── Army units ────────────────────────────────────────────────────────────
    /// <summary>Each entry = one soldier, horse-mounted soldier, cannon, or archer.</summary>
    public static List<BattleUnitData> PlayerUnits = new List<BattleUnitData>();

    /// <summary>Number of dragons the player has stationed.</summary>
    public static int DragonCount;

    /// <summary>
    /// The live SoldierSpawnArea RectTransform, detached and DontDestroyOnLoad'd
    /// by BattleStarter.CarrySoldiersIntoBattle() right before the scene load.
    /// CastleGridMover (the MonoBehaviour that normally owns this reference)
    /// does NOT survive the scene change itself, so its static Instance goes
    /// null in the Battle scene — this static field is how BattleManager finds
    /// the carried GameObject on the other side instead.
    /// </summary>
    public static RectTransform CarriedSoldierSpawnArea;

    /// <summary>
    /// Every live FlyZone RectTransform that had a mounted dragon in it,
    /// detached and DontDestroyOnLoad'd by BattleStarter.CarryDragonsIntoBattle()
    /// right before the scene load — same pattern as CarriedSoldierSpawnArea.
    /// BattleManager.ReceivePlayerDragons() reparents each one (dragon still
    /// live inside it) into PlayerArmyRoot instead of spawning a fresh copy.
    /// </summary>
    public static List<RectTransform> CarriedDragonFlyZones = new List<RectTransform>();

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static void Clear()
    {
        PlayerBlockCount = 0;
        PlayerBlockPositions.Clear();
        PlayerUnits.Clear();
        DragonCount = 0;
        CarriedSoldierSpawnArea = null;
        CarriedDragonFlyZones.Clear();
    }
}

/// <summary>
/// Lightweight description of one battle unit.
/// </summary>
[System.Serializable]
public class BattleUnitData
{
    public BattleUnitType unitType;
    public float health;
    public float damage;
    public float moveSpeed;

    // ── Rider visuals — only used for Horse / Dragon units ─────────────────────
    public EquipmentItem riderFace;
    public EquipmentItem riderArmor;
    public EquipmentItem riderHelmet;
    public EquipmentItem riderWeapon;

    // ── Horse type — only used for Horse units ──────────────────────────────────
    public HorseData horseType;

    // ── Cannon type — only used for Cannon units ────────────────────────────────
    public CannonData cannonType;

    // ── Castle position — only used for Cannon / Archer units ───────────────────
    // Set when this unit was stationed on a specific block in the Village
    // CastleGrid. BattleManager uses this to seat the unit directly onto the
    // matching block in PlayerCastleBuilder instead of the flat army row, so
    // the cannon/archer visually shifts WITH the castle into battle.
    public bool hasGridPosition;
    public Vector2Int gridPosition;

    public BattleUnitData(BattleUnitType type, float hp, float dmg, float speed)
    {
        unitType = type;
        health = hp;
        damage = dmg;
        moveSpeed = speed;
    }
}

public enum BattleUnitType
{
    Soldier,
    Horse,     // horse with a soldier already mounted (rider visuals via Face/Helmet/Armor/Weapon)
    Archer,
    Dragon,    // dragon with a soldier already mounted (same rider visual pattern)
    Cannon,
}