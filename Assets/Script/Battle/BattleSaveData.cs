using System.Collections.Generic;

/// <summary>
/// BattleSaveData
///
/// Static snapshot of the player's army taken the moment they press Start Battle
/// in the Village panel. Survives the scene load because it's a static class —
/// no MonoBehaviour, no DontDestroyOnLoad needed.
///
/// The Battle scene reads this once in BattleManager.Start() to spawn the
/// player's army and build the player's castle wall on the left side.
/// </summary>
public static class BattleSaveData
{
    // ── Castle ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Number of castle blocks the player has placed (read from CastleGrid).
    /// The bot castle will be Random.Range(1, blockCount+1) blocks tall.
    /// </summary>
    public static int PlayerBlockCount;

    // ── Army units ────────────────────────────────────────────────────────────
    /// <summary>Each entry = one soldier or horse-mounted soldier.</summary>
    public static List<BattleUnitData> PlayerUnits = new List<BattleUnitData>();

    /// <summary>Number of dragons the player has stationed.</summary>
    public static int DragonCount;

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static void Clear()
    {
        PlayerBlockCount = 0;
        PlayerUnits.Clear();
        DragonCount = 0;
    }
}

/// <summary>
/// Lightweight description of one battle unit.
/// Add more fields (weapon type, sprite names, etc.) as needed.
/// </summary>
[System.Serializable]
public class BattleUnitData
{
    public BattleUnitType unitType;
    public float health;
    public float damage;
    public float moveSpeed;

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
    MountedSoldier,   // horse + soldier
    Archer,
    Dragon,
    Cannon,           // castle-mounted, fires at bot units automatically
}