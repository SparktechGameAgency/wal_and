using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorseArea
///   - Owns the inventory of bought horses (unlimited, including duplicates)
///   - Manages active HorseSlots (equipped horses)
///
/// BUG FIX — individual equip tracking for duplicate horses:
///   The old IsEquipped(HorseData) used ScriptableObject reference equality,
///   so ALL copies of "Brown Horse" would show as equipped the moment ONE was.
///
///   Now every purchased horse gets a unique int ID (_nextHorseId counter).
///   Slots store that ID (via HorseSlot.InventoryIndex).
///   IsEquippedByIndex(id) / FindSlotForIndex(id) check slots by ID, not by
///   HorseData reference, so each copy is tracked independently.
///
///   Public API changes (callers in HorsePanelManager updated accordingly):
///     BuyHorse()     → now returns the assigned unique ID
///     EquipHorse()   → new inventoryIndex parameter (the ID from BuyHorse)
///     SellHorse()    → new inventoryIndex parameter
///     IsEquipped()   → deprecated alias; prefer IsEquippedByIndex()
///     FindSlotForIndex(int) → new, replaces FindSlotForData for inventory use
/// </summary>
public class HorseArea : MonoBehaviour
{
    public static HorseArea Instance { get; private set; }

    [Header("Active slots (equipped horses)")]
    [SerializeField] private HorseSlot[] slots;

    /// <summary>Read-only view of all equipped slots. Used by HorseDetachButton.</summary>
    public HorseSlot[] Slots => slots;

    [Header("Area buttons")]
    [SerializeField] private Button buyButton;
    [Tooltip("Opens the Inventory panel where horses can be equipped, upgraded, and sold")]
    [SerializeField] private Button inventoryButton;

    // ── Inventory ─────────────────────────────────────────────────────────────
    // Each purchased horse is stored as an (HorseData, uniqueId) pair so that
    // duplicate types can be told apart.
    private List<HorseData> _ownedHorses = new List<HorseData>();
    private List<int> _ownedIds = new List<int>();   // parallel, same length
    private int _nextHorseId = 0;

    // Upgrade progress is saved here when a horse is unequipped so it is
    // restored if the horse is re-equipped later.
    private Dictionary<int, HorseSlot.UpgradeState> _savedStates
        = new Dictionary<int, HorseSlot.UpgradeState>();

    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryClicked);
        RefreshButtons();
    }

    // ─── Area button handlers ─────────────────────────────────────────────────

    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();

    private void OnInventoryClicked()
    {
        if (_ownedHorses.Count == 0)
        {
            HorsePanelManager.Instance?.OpenBuyMode();
            return;
        }
        HorsePanelManager.Instance?.OpenInventoryMode();
    }

    // ─── Buy API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds one copy of this horse to the owned list.
    /// Returns the unique inventory ID assigned to this copy, or -1 on failure.
    /// Pass this ID to EquipHorse / SellHorse so the right copy is targeted.
    /// </summary>
    public int BuyHorse(HorseData data)
    {
        if (data == null) return -1;

        int id = _nextHorseId++;
        _ownedHorses.Add(data);
        _ownedIds.Add(id);

        RefreshButtons();
        Debug.Log($"[HorseArea] Bought '{data.horseName}' (id={id}). " +
                  $"Total owned: {CountOwned(data)}x.");
        return id;
    }

    // ─── Sell API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes the copy identified by inventoryIndex (unique ID) from the
    /// owned list and un-equips it from any slot.
    ///
    /// NOTE: Gold refund is NOT calculated here.
    /// </summary>
    public void SellHorse(HorseData data, int inventoryIndex)
    {
        // Un-equip from slot if currently sitting in one
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
            {
                slot.UnequipHorse();
                break;
            }
        }

        // Remove by unique ID (not by HorseData reference, to avoid removing the wrong copy)
        int listPos = _ownedIds.IndexOf(inventoryIndex);
        if (listPos >= 0)
        {
            _ownedHorses.RemoveAt(listPos);
            _ownedIds.RemoveAt(listPos);
        }
        else
        {
            // Fallback: remove first matching HorseData (legacy safety net)
            int fallback = _ownedHorses.IndexOf(data);
            if (fallback >= 0) { _ownedHorses.RemoveAt(fallback); _ownedIds.RemoveAt(fallback); }
        }

        // Horse is sold — discard any saved upgrade state for it
        _savedStates.Remove(inventoryIndex);

        RefreshButtons();
        Debug.Log($"[HorseArea] Sold '{data.horseName}' (id={inventoryIndex}). " +
                  $"Remaining owned: {CountOwned(data)}x.");
    }

    // ─── Equip API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Equips the copy identified by inventoryIndex into the first free slot
    /// (or a specific target slot).
    ///
    /// inventoryIndex is the unique ID returned by BuyHorse(), stored per-card
    /// in HorseLevelButton and tracked as HorsePanelManager._selectedInventoryId.
    /// </summary>
    public bool EquipHorse(HorseData data, int inventoryIndex, HorseSlot targetSlot = null)
    {
        if (!_ownedIds.Contains(inventoryIndex))
        {
            Debug.LogWarning($"[HorseArea] Cannot equip: id={inventoryIndex} not owned.");
            return false;
        }

        // Already in a slot?
        if (IsEquippedByIndex(inventoryIndex))
        {
            Debug.LogWarning($"[HorseArea] id={inventoryIndex} is already in a slot.");
            return false;
        }

        HorseSlot slot = (targetSlot != null && !targetSlot.IsOccupied)
            ? targetSlot
            : GetFreeSlot();

        if (slot == null)
        {
            Debug.LogWarning("[HorseArea] No free slot to equip into.");
            return false;
        }

        slot.Equip(data, inventoryIndex);   // pass the unique ID so the slot knows which copy it holds

        // Restore upgrade progress if this horse had been equipped before
        if (_savedStates.TryGetValue(inventoryIndex, out HorseSlot.UpgradeState saved))
        {
            slot.RestoreUpgradeState(saved);
            Debug.Log($"[HorseArea] Restored upgrade state for id={inventoryIndex}.");
        }

        RefreshButtons();
        return true;
    }

    /// <summary>
    /// Un-equips the horse from the given slot.
    /// Upgrade progress is saved so it is restored on re-equip.
    /// The horse stays in the owned list and can be re-equipped later.
    /// </summary>
    public void UnequipHorse(HorseSlot slot)
    {
        if (slot == null || !slot.IsOccupied) return;

        // Save upgrade progress before clearing the slot
        int id = slot.InventoryIndex;
        if (id >= 0)
        {
            _savedStates[id] = slot.GetUpgradeState();
            Debug.Log($"[HorseArea] Saved upgrade state for id={id} " +
                      $"(count={_savedStates[id].upgradeCount}).");
        }

        slot.UnequipHorse();
        RefreshButtons();
    }

    // ─── Queries ──────────────────────────────────────────────────────────────

    public bool HasFreeSlot() => GetFreeSlot() != null;
    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

    /// <summary>
    /// Returns true if the copy with this unique ID is currently in a slot.
    /// Use this instead of IsEquipped(HorseData) when you have the ID.
    /// </summary>
    public bool IsEquippedByIndex(int inventoryIndex)
    {
        if (inventoryIndex < 0) return false;
        foreach (var slot in slots)
            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
                return true;
        return false;
    }

    /// <summary>
    /// Legacy reference-equality check — returns true if ANY copy of this
    /// HorseData type is equipped. Kept for buy-mode HUD only.
    /// Prefer IsEquippedByIndex() for inventory-mode logic.
    /// </summary>
    public bool IsEquipped(HorseData data)
    {
        if (data == null) return false;
        foreach (var slot in slots)
            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
                return true;
        return false;
    }

    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

    /// <summary>
    /// Returns the unique ID for the horse at list position i.
    /// Used by PopulateOwnedCards to give each card its stable ID.
    /// </summary>
    public int GetInventoryId(int listPosition)
    {
        if (listPosition < 0 || listPosition >= _ownedIds.Count) return -1;
        return _ownedIds[listPosition];
    }

    /// <summary>Returns the slot holding the copy with this unique ID, or null.</summary>
    public HorseSlot FindSlotForIndex(int inventoryIndex)
    {
        if (inventoryIndex < 0) return null;
        foreach (var slot in slots)
            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
                return slot;
        return null;
    }

    /// <summary>Returns the slot holding any copy of this HorseData type, or null.</summary>
    public HorseSlot FindSlotForData(HorseData data)
    {
        if (data == null) return null;
        foreach (var slot in slots)
            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
                return slot;
        return null;
    }

    /// <summary>How many copies of this horse type the player currently owns.</summary>
    public int CountOwned(HorseData data)
    {
        int count = 0;
        foreach (var h in _ownedHorses)
            if (h == data) count++;
        return count;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private HorseSlot GetFreeSlot()
    {
        foreach (var s in slots)
            if (s != null && !s.IsOccupied) return s;
        return null;
    }

    private void RefreshButtons()
    {
        if (buyButton != null) buyButton.interactable = true;
        if (inventoryButton != null) inventoryButton.interactable = _ownedHorses.Count > 0;
    }
    public HorseSlot GetFirstFreeSlot()
    {
        foreach (HorseSlot slot in slots)   // rename 'slots' to match your field name
            if (!slot.IsOccupied) return slot;
        return null;
    }

    public void OnHorseEquippedToSlot(int inventoryIndex, HorseSlot targetSlot)
    {
        // If your HorseArea tracks equipped state per-entry, update it here.
        // Example if you have a list of entries with isEquipped / equippedSlot:
        //
        //   var entry = _ownedHorses[inventoryIndex];
        //   entry.isEquipped  = true;
        //   entry.equippedSlot = targetSlot;
        //
        // If HorseArea infers equipped state from slot.InventoryIndex (like FindSlotForIndex),
        // no extra tracking is needed here — the slot already has the correct InventoryIndex
        // set by HorseWalkZone.RecallToSlot → HorseSlot.Equip. Leave this body empty in that case.

        Debug.Log($"[HorseArea] Horse (idx={inventoryIndex}) marked as equipped in '{targetSlot.name}'.");
    }

}