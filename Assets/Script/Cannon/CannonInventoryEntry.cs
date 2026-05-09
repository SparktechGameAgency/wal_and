using UnityEngine;

/// <summary>
/// CANNON PANEL — CannonInventoryEntry
///
/// Plain C# class. One instance created every time the player buys a cannon.
/// Buying the same type twice creates two independent entries with different IDs.
///
/// All mutable state (upgrade count, timer, equipped slot) lives here.
/// CannonData is never modified — only read for base values.
///
/// Owned and managed by CannonPanelManager._inventory list.
/// </summary>
public class CannonInventoryEntry
{
    public const int MAX_UPGRADES = 3;

    // ── Identity ───────────────────────────────────────────────────────────────
    public CannonData data;
    public int inventoryId;      // unique ID assigned at purchase

    // ── Upgrade State ──────────────────────────────────────────────────────────
    public int upgradeCount = 0;   // 0–3
    public bool isUpgrading = false;
    public float upgradeEndTime = 0f;  // Time.time when upgrade completes

    public float UpgradeTimeRemaining =>
        isUpgrading ? Mathf.Max(0f, upgradeEndTime - Time.time) : 0f;

    // ── Slot State ─────────────────────────────────────────────────────────────
    public bool isEquipped = false;
    public CannonSlot equippedSlot = null;

    // ── Computed Live Stats ────────────────────────────────────────────────────
    public float CurrentHealth => data.health + upgradeCount * data.upgradeHealthGain;
    public float CurrentAbility => data.ability + upgradeCount * data.upgradeAbilityGain;
    public float CurrentDamage => data.damage + upgradeCount * data.upgradeDamageGain;
    public float CurrentRange => data.range + upgradeCount * data.upgradeRangeGain;

    // ── Helpers ────────────────────────────────────────────────────────────────
    public bool IsMaxLevel => upgradeCount >= MAX_UPGRADES;
    public int DisplayLevel => upgradeCount + 1;  // shown as "LEVEL 1", "LEVEL 2" …
}