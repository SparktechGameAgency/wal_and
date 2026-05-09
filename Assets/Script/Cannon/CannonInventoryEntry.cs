using UnityEngine;

/// <summary>
/// CANNON PANEL — CannonInventoryEntry
///
/// Plain C# class (NOT a MonoBehaviour / ScriptableObject).
/// One instance is created every time the player buys a cannon.
/// Buying the same type twice creates two separate entries with different inventoryIds.
///
/// All mutable state (upgrade level, upgrade timer, castle placement) lives here.
/// CannonData is never modified — it is only read for base values and gains.
///
/// Owned and managed by CannonPanelManager._inventory.
/// </summary>
public class CannonInventoryEntry
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>The type of cannon (ScriptableObject — read-only).</summary>
    public CannonData data;

    /// <summary>
    /// Unique integer ID assigned at purchase time (CannonPanelManager._nextId++).
    /// Distinguishes two copies of the same CannonData type.
    /// </summary>
    public int inventoryId;

    // ─── Upgrade State ────────────────────────────────────────────────────────

    public const int MAX_UPGRADES = 3;

    /// <summary>How many upgrades have been applied (0–3).</summary>
    public int upgradeCount = 0;

    /// <summary>True while a timed upgrade is in progress.</summary>
    public bool isUpgrading = false;

    /// <summary>Time.time value at which the current upgrade completes.</summary>
    public float upgradeEndTime = 0f;

    /// <summary>Seconds remaining in the active upgrade (0 if none).</summary>
    public float UpgradeTimeRemaining =>
        isUpgrading ? Mathf.Max(0f, upgradeEndTime - Time.time) : 0f;

    // ─── Castle Placement ─────────────────────────────────────────────────────

    /// <summary>True while this cannon is sitting in a CannonSlot on the castle.</summary>
    public bool isPlacedOnCastle = false;

    /// <summary>The slot this cannon currently occupies, or null.</summary>
    public CannonSlot occupiedSlot = null;

    // ─── Computed Live Stats ──────────────────────────────────────────────────
    // These are derived — never stored. Each upgrade adds the per-upgrade gain.

    public float CurrentHealth => data.health + upgradeCount * data.upgradeHealthGain;
    public float CurrentDamage => data.damage + upgradeCount * data.upgradeDamageGain;
    public float CurrentRange => data.range + upgradeCount * data.upgradeRangeGain;
}