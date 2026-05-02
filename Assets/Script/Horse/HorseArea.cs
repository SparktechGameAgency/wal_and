//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// HorseArea — one Buy button and one Sell button for the whole area.
/////
/////   HorseArea
/////     ├── SlotsArea
/////     │     ├── HorseSlot_1   ← HorseSlot.cs
/////     │     └── HorseSlot_2   ← HorseSlot.cs
/////     ├── BuyButton            ← opens horse panel in BUY mode
/////     └── SellButton           ← opens horse panel in SELL / UPGRADE mode
///// </summary>
//public class HorseArea : MonoBehaviour
//{
//    public static HorseArea Instance { get; private set; }

//    [Header("Slots")]
//    [SerializeField] private HorseSlot[] slots;

//    [Header("Area buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private Button sellButton;

//    private void Awake()
//    {
//        Instance = this;

//        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
//        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);

//        RefreshButtons();
//    }

//    // ─── Button handlers ──────────────────────────────────────────────────────

//    private void OnBuyClicked()
//    {
//        if (!HasFreeSlot()) { Debug.Log("[HorseArea] No free slot."); return; }
//        HorsePanelManager.Instance?.OpenBuyMode();
//    }

//    private void OnSellClicked()
//    {
//        if (!HasOccupiedSlot()) { Debug.Log("[HorseArea] No horse to sell."); return; }
//        HorsePanelManager.Instance?.OpenSellMode();
//    }

//    // ─── Spawn API (called by HorsePanelManager) ──────────────────────────────

//    /// <summary>Spawns in the first free slot.</summary>
//    public bool SpawnHorse(HorseData data)
//    {
//        int i = GetFreeIndex();
//        if (i == -1) { Debug.LogWarning("[HorseArea] All slots full."); return false; }
//        slots[i].Spawn(data);
//        RefreshButtons();
//        return true;
//    }

//    /// <summary>Sells the slot that holds the given HorseData.</summary>
//    public void SellHorse(HorseData data)
//    {
//        foreach (var slot in slots)
//        {
//            if (slot.IsOccupied && slot.CurrentData == data)
//            {
//                slot.SellHorse();
//                RefreshButtons();
//                return;
//            }
//        }
//    }

//    // ─── Slot queries ─────────────────────────────────────────────────────────

//    public bool HasFreeSlot() => GetFreeIndex() != -1;
//    public bool HasOccupiedSlot() => System.Array.Exists(slots, s => s != null && s.IsOccupied);

//    /// <summary>Returns all HorseData currently occupying slots (for sell panel).</summary>
//    public HorseData[] GetOwnedHorses()
//    {
//        var list = new System.Collections.Generic.List<HorseData>();
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied) list.Add(slot.CurrentData);
//        return list.ToArray();
//    }

//    /// <summary>
//    /// Returns the HorseSlot that currently holds <paramref name="data"/>,
//    /// or null if not found. Used by HorsePanelManager to read live upgrade state.
//    /// </summary>
//    public HorseSlot GetSlotForData(HorseData data)
//    {
//        if (data == null) return null;
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//                return slot;
//        return null;
//    }

//    // ─── UI ───────────────────────────────────────────────────────────────────

//    public void RefreshButtons()
//    {
//        if (buyButton != null) buyButton.interactable = HasFreeSlot();
//        if (sellButton != null) sellButton.interactable = HasOccupiedSlot();
//    }

//    private int GetFreeIndex()
//    {
//        for (int i = 0; i < slots.Length; i++)
//            if (slots[i] != null && !slots[i].IsOccupied) return i;
//        return -1;
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorseArea
///   - Tracks owned horses (bought, not yet in a slot) — up to 3
///   - Manages 2 active HorseSlots (equipped horses)
///   - Buy / Sell buttons open the panel
/// </summary>
public class HorseArea : MonoBehaviour
{
    public static HorseArea Instance { get; private set; }

    [Header("Active slots (equipped horses)")]
    [SerializeField] private HorseSlot[] slots;

    [Header("Area buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;

    // Owned horses = bought but not necessarily equipped
    private List<HorseData> _ownedHorses = new List<HorseData>();
    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

    private void Awake()
    {
        Instance = this;
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
        RefreshButtons();
    }

    // ─── Area button handlers ─────────────────────────────────────────────────

    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();
    private void OnSellClicked()
    {
        if (_ownedHorses.Count == 0) return;
        HorsePanelManager.Instance?.OpenSellMode();
    }

    // ─── Buy / Sell API (called by HorsePanelManager) ─────────────────────────

    /// <summary>Adds horse to owned list. Does NOT place it in a slot.</summary>
    public bool BuyHorse(HorseData data)
    {
        if (_ownedHorses.Contains(data)) { Debug.LogWarning("[HorseArea] Already own this horse."); return false; }
        _ownedHorses.Add(data);
        RefreshButtons();
        return true;
    }

    /// <summary>Removes from owned list AND un-equips from any slot.</summary>
    public void SellHorse(HorseData data)
    {
        // Un-equip from slot if equipped
        foreach (var slot in slots)
            if (slot.IsOccupied && slot.CurrentData == data)
                slot.UnequipHorse();

        _ownedHorses.Remove(data);
        HorsePanelManager.Instance?.AddGold(Mathf.RoundToInt(data.cost * 0.5f));
        RefreshButtons();
    }

    /// <summary>Equips an owned horse into the first free slot (or a specific slot).</summary>
    public bool EquipHorse(HorseData data, HorseSlot targetSlot = null)
    {
        if (!_ownedHorses.Contains(data)) return false;

        HorseSlot slot = targetSlot != null && !targetSlot.IsOccupied
            ? targetSlot
            : GetFreeSlot();

        if (slot == null) { Debug.LogWarning("[HorseArea] No free slot to equip into."); return false; }

        slot.Equip(data);
        RefreshButtons();
        return true;
    }

    /// <summary>Un-equips horse from its slot (horse stays owned).</summary>
    public void UnequipHorse(HorseSlot slot)
    {
        if (!slot.IsOccupied) return;
        slot.UnequipHorse();
        RefreshButtons();
    }

    public bool HasFreeSlot() => GetFreeSlot() != null;
    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

    private HorseSlot GetFreeSlot()
    {
        foreach (var s in slots) if (s != null && !s.IsOccupied) return s;
        return null;
    }

    private void RefreshButtons()
    {
        if (buyButton != null) buyButton.interactable = true;  // always available to browse
        if (sellButton != null) sellButton.interactable = _ownedHorses.Count > 0;
    }
}