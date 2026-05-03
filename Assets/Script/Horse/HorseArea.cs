////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.UI;

/////////// <summary>
/////////// HorseArea
///////////   - Owns the inventory of bought horses (up to any number)
///////////   - Manages 2 active HorseSlots (equipped horses)
///////////   - Buy / Sell buttons open the panel
///////////
/////////// KEY FIX — equip-once-per-purchase:
///////////   A separate _equippedSet tracks which HorseData instances are currently
///////////   sitting in a slot.  EquipHorse() refuses to equip the same instance
///////////   again until it is first unequipped.  This means buying ONE horse and
///////////   clicking Equip repeatedly will only place it once.
/////////// </summary>
////////public class HorseArea : MonoBehaviour
////////{
////////    public static HorseArea Instance { get; private set; }

////////    [Header("Active slots (equipped horses)")]
////////    [SerializeField] private HorseSlot[] slots;

////////    [Header("Area buttons")]
////////    [SerializeField] private Button buyButton;
////////    [SerializeField] private Button sellButton;

////////    // All horses the player has bought (may or may not be in a slot)
////////    private List<HorseData> _ownedHorses = new List<HorseData>();

////////    // Subset of owned that are currently in a slot
////////    private HashSet<HorseData> _equippedSet = new HashSet<HorseData>();

////////    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

////////    private void Awake()
////////    {
////////        Instance = this;
////////        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
////////        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
////////        RefreshButtons();
////////    }

////////    // ─── Area button handlers ─────────────────────────────────────────────────

////////    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();

////////    private void OnSellClicked()
////////    {
////////        if (_ownedHorses.Count == 0) return;
////////        HorsePanelManager.Instance?.OpenSellMode();
////////    }

////////    // ─── Buy / Sell API (called by HorsePanelManager) ─────────────────────────

////////    /// <summary>Adds horse to owned list. Does NOT place it in a slot.</summary>
////////    public bool BuyHorse(HorseData data)
////////    {
////////        if (_ownedHorses.Contains(data))
////////        {
////////            Debug.LogWarning("[HorseArea] Already own this horse.");
////////            return false;
////////        }
////////        _ownedHorses.Add(data);
////////        RefreshButtons();
////////        return true;
////////    }

////////    /// <summary>Removes from owned list AND un-equips from any slot.</summary>
////////    public void SellHorse(HorseData data)
////////    {
////////        // Un-equip from slot if currently equipped
////////        foreach (var slot in slots)
////////        {
////////            if (slot.IsOccupied && slot.CurrentData == data)
////////            {
////////                slot.UnequipHorse();
////////                _equippedSet.Remove(data);
////////            }
////////        }

////////        _ownedHorses.Remove(data);
////////        HorsePanelManager.Instance?.AddGold(Mathf.RoundToInt(data.cost * 0.5f));
////////        RefreshButtons();
////////    }

////////    /// <summary>
////////    /// Equips an owned horse into the first free slot (or a specific target slot).
////////    ///
////////    /// IMPORTANT: if this HorseData instance is already in a slot the call is
////////    /// silently rejected.  The player must sell/unequip before equipping again.
////////    /// This prevents the same horse appearing in two slots simultaneously.
////////    /// </summary>
////////    public bool EquipHorse(HorseData data, HorseSlot targetSlot = null)
////////    {
////////        if (!_ownedHorses.Contains(data))
////////        {
////////            Debug.LogWarning("[HorseArea] Cannot equip a horse you don't own.");
////////            return false;
////////        }

////////        // Already in a slot — don't double-equip
////////        if (_equippedSet.Contains(data))
////////        {
////////            Debug.LogWarning("[HorseArea] Horse is already equipped in a slot.");
////////            return false;
////////        }

////////        HorseSlot slot = (targetSlot != null && !targetSlot.IsOccupied)
////////            ? targetSlot
////////            : GetFreeSlot();

////////        if (slot == null)
////////        {
////////            Debug.LogWarning("[HorseArea] No free slot to equip into.");
////////            return false;
////////        }

////////        slot.Equip(data);
////////        _equippedSet.Add(data);
////////        RefreshButtons();
////////        return true;
////////    }

////////    /// <summary>Un-equips horse from its slot (horse stays owned, can be re-equipped).</summary>
////////    public void UnequipHorse(HorseSlot slot)
////////    {
////////        if (!slot.IsOccupied) return;
////////        _equippedSet.Remove(slot.CurrentData);
////////        slot.UnequipHorse();
////////        RefreshButtons();
////////    }

////////    // ─── Queries ──────────────────────────────────────────────────────────────

////////    public bool HasFreeSlot() => GetFreeSlot() != null;
////////    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

////////    /// <summary>Returns true if this data instance is already sitting in a slot.</summary>
////////    public bool IsEquipped(HorseData data) => _equippedSet.Contains(data);

////////    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

////////    /// <summary>Returns the HorseSlot currently holding the given data, or null.</summary>
////////    public HorseSlot FindSlotForData(HorseData data)
////////    {
////////        if (data == null) return null;
////////        foreach (var slot in slots)
////////            if (slot != null && slot.IsOccupied && slot.CurrentData == data) return slot;
////////        return null;
////////    }

////////    private HorseSlot GetFreeSlot()
////////    {
////////        foreach (var s in slots)
////////            if (s != null && !s.IsOccupied) return s;
////////        return null;
////////    }

////////    private void RefreshButtons()
////////    {
////////        if (buyButton != null) buyButton.interactable = true;
////////        if (sellButton != null) sellButton.interactable = _ownedHorses.Count > 0;
////////    }
////////}

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// HorseArea
/////////   - Owns the inventory of bought horses (unlimited, including duplicates)
/////////   - Manages active HorseSlots (equipped horses)
/////////   - Buy / Sell buttons open the panel
/////////
///////// CHANGES FROM PREVIOUS VERSION:
/////////
/////////  1. MULTI-BUY SUPPORT
/////////     BuyHorse() no longer blocks purchasing a horse you already own.
/////////     The owned list is a List<HorseData> that allows duplicates, so the
/////////     player can buy as many copies of the same type as they like.
/////////
/////////  2. SLOT-BASED EQUIP TRACKING
/////////     _equippedSet (HashSet<HorseData>) has been removed.  It used the
/////////     HorseData reference as the key, which broke when multiple copies of
/////////     the same HorseData existed — the second copy would always be treated
/////////     as "already equipped".
/////////     IsEquipped() and EquipHorse() now check the actual slot contents
/////////     directly, so duplicate-type horses each track their own slot state.
/////////
/////////  3. NO GOLD IN SellHorse()
/////////     SellHorse() no longer calls AddGold().  Gold is now calculated and
/////////     added by HorsePanelManager.OnSellClicked(), which reads the upgrade-
/////////     based refund percentage from HorseSlot.SellRefundPercent.
/////////     This prevents gold being awarded twice.
/////////
/////////  4. UnequipHorse(HorseSlot) KEPT
/////////     The public UnequipHorse(HorseSlot) overload is still here so
/////////     HorsePanelManager.OnUnequipClicked() can call it cleanly.
///////// </summary>
//////public class HorseArea : MonoBehaviour
//////{
//////    public static HorseArea Instance { get; private set; }

//////    [Header("Active slots (equipped horses)")]
//////    [SerializeField] private HorseSlot[] slots;

//////    [Header("Area buttons")]
//////    [SerializeField] private Button buyButton;
//////    [SerializeField] private Button sellButton;

//////    // ── Inventory ─────────────────────────────────────────────────────────────
//////    // Allows duplicate HorseData entries — each entry is one purchased horse.
//////    private List<HorseData> _ownedHorses = new List<HorseData>();

//////    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

//////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        Instance = this;
//////        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
//////        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
//////        RefreshButtons();
//////    }

//////    // ─── Area button handlers ─────────────────────────────────────────────────

//////    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();

//////    private void OnSellClicked()
//////    {
//////        if (_ownedHorses.Count == 0) return;
//////        HorsePanelManager.Instance?.OpenSellMode();
//////    }

//////    // ─── Buy API ──────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Adds one copy of this horse to the owned list.
//////    /// Duplicates are explicitly allowed — the player can own many of the
//////    /// same type and each is an independent entry in the inventory.
//////    /// </summary>
//////    public bool BuyHorse(HorseData data)
//////    {
//////        if (data == null) return false;
//////        _ownedHorses.Add(data);          // no Contains() check — duplicates OK
//////        RefreshButtons();
//////        Debug.Log($"[HorseArea] Bought '{data.horseName}'. " +
//////                  $"Total owned: {CountOwned(data)}x.");
//////        return true;
//////    }

//////    // ─── Sell API ─────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Removes one copy of this horse from the owned list and un-equips it
//////    /// from any slot it occupies.
//////    ///
//////    /// NOTE: Gold is NOT added here. HorsePanelManager.OnSellClicked()
//////    /// calculates the upgrade-based refund and calls AddGold() itself.
//////    /// </summary>
//////    public void SellHorse(HorseData data)
//////    {
//////        // Un-equip from slot if currently sitting in one
//////        foreach (var slot in slots)
//////        {
//////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//////            {
//////                slot.UnequipHorse();
//////                break;   // only one slot can hold this exact copy
//////            }
//////        }

//////        // Remove exactly ONE entry from the list (not all duplicates)
//////        int idx = _ownedHorses.IndexOf(data);
//////        if (idx >= 0) _ownedHorses.RemoveAt(idx);

//////        RefreshButtons();
//////        Debug.Log($"[HorseArea] Sold '{data.horseName}'. " +
//////                  $"Remaining owned: {CountOwned(data)}x.");
//////    }

//////    // ─── Equip API ────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Equips an owned horse into the first free slot (or a specific target slot).
//////    ///
//////    /// Because the same HorseData asset can appear multiple times in the owned
//////    /// list, we no longer use a HashSet to guard against double-equipping.
//////    /// Instead we check slot contents directly: if ANY slot already holds this
//////    /// exact data reference, we reject the call only when the caller passes the
//////    /// SAME reference that is already slotted.
//////    ///
//////    /// In practice: buying two Level-1 horses gives you two IDENTICAL references
//////    /// (same ScriptableObject).  To allow both to be equipped simultaneously you
//////    /// should create separate ScriptableObject assets per horse, OR ensure the
//////    /// game only equips the same type into two slots intentionally.
//////    /// If you want to block same-type double-equip, uncomment the guard below.
//////    /// </summary>
//////    public bool EquipHorse(HorseData data, HorseSlot targetSlot = null)
//////    {
//////        if (!_ownedHorses.Contains(data))
//////        {
//////            Debug.LogWarning("[HorseArea] Cannot equip a horse you don't own.");
//////            return false;
//////        }

//////        // ── Optional: block equipping the same ScriptableObject reference twice ──
//////        // Uncomment if you want to prevent two slots holding identical HorseData.
//////        // if (IsEquipped(data))
//////        // {
//////        //     Debug.LogWarning("[HorseArea] This horse type is already equipped.");
//////        //     return false;
//////        // }

//////        HorseSlot slot = (targetSlot != null && !targetSlot.IsOccupied)
//////            ? targetSlot
//////            : GetFreeSlot();

//////        if (slot == null)
//////        {
//////            Debug.LogWarning("[HorseArea] No free slot to equip into.");
//////            return false;
//////        }

//////        slot.Equip(data);
//////        RefreshButtons();
//////        return true;
//////    }

//////    /// <summary>
//////    /// Un-equips the horse from the given slot.
//////    /// The horse stays in the owned list — it can be re-equipped later.
//////    /// </summary>
//////    public void UnequipHorse(HorseSlot slot)
//////    {
//////        if (slot == null || !slot.IsOccupied) return;
//////        slot.UnequipHorse();   // removes from slot, horse stays in _ownedHorses
//////        RefreshButtons();
//////    }

//////    // ─── Queries ──────────────────────────────────────────────────────────────

//////    public bool HasFreeSlot() => GetFreeSlot() != null;
//////    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

//////    /// <summary>
//////    /// Returns true if this data instance is currently sitting in ANY slot.
//////    /// Checks slot contents directly instead of a HashSet.
//////    /// </summary>
//////    public bool IsEquipped(HorseData data)
//////    {
//////        if (data == null) return false;
//////        foreach (var slot in slots)
//////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//////                return true;
//////        return false;
//////    }

//////    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

//////    /// <summary>Returns the HorseSlot currently holding the given data, or null.</summary>
//////    public HorseSlot FindSlotForData(HorseData data)
//////    {
//////        if (data == null) return null;
//////        foreach (var slot in slots)
//////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//////                return slot;
//////        return null;
//////    }

//////    /// <summary>How many copies of this horse type the player currently owns.</summary>
//////    public int CountOwned(HorseData data)
//////    {
//////        int count = 0;
//////        foreach (var h in _ownedHorses)
//////            if (h == data) count++;
//////        return count;
//////    }

//////    // ─── Private helpers ──────────────────────────────────────────────────────

//////    private HorseSlot GetFreeSlot()
//////    {
//////        foreach (var s in slots)
//////            if (s != null && !s.IsOccupied) return s;
//////        return null;
//////    }

//////    private void RefreshButtons()
//////    {
//////        if (buyButton != null) buyButton.interactable = true;
//////        if (sellButton != null) sellButton.interactable = _ownedHorses.Count > 0;
//////    }
//////}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// HorseArea
///////   - Owns the inventory of bought horses (unlimited, including duplicates)
///////   - Manages active HorseSlots (equipped horses)
///////
/////// AREA BUTTONS:
///////   • buyButton       → opens Buy panel  (purchase horses only)
///////   • inventoryButton → opens Inventory panel (equip / update / sell)
///////
/////// The old "Sell" button has been replaced with "Inventory".
/////// Inventory mode is the central hub for managing owned horses.
/////// </summary>
////public class HorseArea : MonoBehaviour
////{
////    public static HorseArea Instance { get; private set; }

////    [Header("Active slots (equipped horses)")]
////    [SerializeField] private HorseSlot[] slots;

////    [Header("Area buttons")]
////    [SerializeField] private Button buyButton;
////    [Tooltip("Opens the Inventory panel where horses can be equipped, upgraded, and sold")]
////    [SerializeField] private Button inventoryButton;

////    // ── Inventory ─────────────────────────────────────────────────────────────
////    // Allows duplicate HorseData entries — each entry is one purchased horse.
////    private List<HorseData> _ownedHorses = new List<HorseData>();

////    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        Instance = this;
////        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
////        if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryClicked);
////        RefreshButtons();
////    }

////    // ─── Area button handlers ─────────────────────────────────────────────────

////    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();

////    private void OnInventoryClicked()
////    {
////        if (_ownedHorses.Count == 0)
////        {
////            // Nothing owned yet — open buy mode as a hint
////            HorsePanelManager.Instance?.OpenBuyMode();
////            return;
////        }
////        HorsePanelManager.Instance?.OpenInventoryMode();
////    }

////    // ─── Buy API ──────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Adds one copy of this horse to the owned list.
////    /// Duplicates are explicitly allowed — the player can own many of the
////    /// same type, each as an independent inventory entry.
////    /// </summary>
////    public bool BuyHorse(HorseData data)
////    {
////        if (data == null) return false;
////        _ownedHorses.Add(data);
////        RefreshButtons();
////        Debug.Log($"[HorseArea] Bought '{data.horseName}'. " +
////                  $"Total owned: {CountOwned(data)}x.");
////        return true;
////    }

////    // ─── Sell API ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Removes one copy of this horse from the owned list and un-equips it
////    /// from any slot it occupies.
////    ///
////    /// NOTE: Gold refund is NOT calculated here.
////    /// HorsePanelManager.OnSellClicked() reads HorseSlot.SellRefundPercent
////    /// and calls AddGold() itself to avoid double-awarding.
////    /// </summary>
////    public void SellHorse(HorseData data)
////    {
////        // Un-equip from slot if currently sitting in one
////        foreach (var slot in slots)
////        {
////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
////            {
////                slot.UnequipHorse();
////                break;   // only one slot can hold this exact copy
////            }
////        }

////        // Remove exactly ONE entry from the list (not all duplicates)
////        int idx = _ownedHorses.IndexOf(data);
////        if (idx >= 0) _ownedHorses.RemoveAt(idx);

////        RefreshButtons();
////        Debug.Log($"[HorseArea] Sold '{data.horseName}'. " +
////                  $"Remaining owned: {CountOwned(data)}x.");
////    }

////    // ─── Equip API ────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Equips an owned horse into the first free slot (or a specific target slot).
////    /// The same ScriptableObject asset can appear multiple times in the owned list;
////    /// slot contents are checked directly instead of using a HashSet.
////    /// </summary>
////    public bool EquipHorse(HorseData data, HorseSlot targetSlot = null)
////    {
////        if (!_ownedHorses.Contains(data))
////        {
////            Debug.LogWarning("[HorseArea] Cannot equip a horse you don't own.");
////            return false;
////        }

////        HorseSlot slot = (targetSlot != null && !targetSlot.IsOccupied)
////            ? targetSlot
////            : GetFreeSlot();

////        if (slot == null)
////        {
////            Debug.LogWarning("[HorseArea] No free slot to equip into.");
////            return false;
////        }

////        slot.Equip(data);
////        RefreshButtons();
////        return true;
////    }

////    /// <summary>
////    /// Un-equips the horse from the given slot.
////    /// The horse stays in the owned list — it can be re-equipped later.
////    /// </summary>
////    public void UnequipHorse(HorseSlot slot)
////    {
////        if (slot == null || !slot.IsOccupied) return;
////        slot.UnequipHorse();
////        RefreshButtons();
////    }

////    // ─── Queries ──────────────────────────────────────────────────────────────

////    public bool HasFreeSlot() => GetFreeSlot() != null;
////    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

////    /// <summary>Returns true if this data instance is sitting in ANY slot.</summary>
////    public bool IsEquipped(HorseData data)
////    {
////        if (data == null) return false;
////        foreach (var slot in slots)
////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
////                return true;
////        return false;
////    }

////    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

////    /// <summary>Returns the HorseSlot currently holding the given data, or null.</summary>
////    public HorseSlot FindSlotForData(HorseData data)
////    {
////        if (data == null) return null;
////        foreach (var slot in slots)
////            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
////                return slot;
////        return null;
////    }

////    /// <summary>How many copies of this horse type the player currently owns.</summary>
////    public int CountOwned(HorseData data)
////    {
////        int count = 0;
////        foreach (var h in _ownedHorses)
////            if (h == data) count++;
////        return count;
////    }

////    // ─── Private helpers ──────────────────────────────────────────────────────

////    private HorseSlot GetFreeSlot()
////    {
////        foreach (var s in slots)
////            if (s != null && !s.IsOccupied) return s;
////        return null;
////    }

////    private void RefreshButtons()
////    {
////        if (buyButton != null) buyButton.interactable = true;
////        if (inventoryButton != null) inventoryButton.interactable = _ownedHorses.Count > 0;
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// HorseArea
/////   - Owns the inventory of bought horses (unlimited, including duplicates)
/////   - Manages active HorseSlots (equipped horses)
/////
///// BUG FIX — individual equip tracking for duplicate horses:
/////   The old IsEquipped(HorseData) used ScriptableObject reference equality,
/////   so ALL copies of "Brown Horse" would show as equipped the moment ONE was.
/////
/////   Now every purchased horse gets a unique int ID (_nextHorseId counter).
/////   Slots store that ID (via HorseSlot.InventoryIndex).
/////   IsEquippedByIndex(id) / FindSlotForIndex(id) check slots by ID, not by
/////   HorseData reference, so each copy is tracked independently.
/////
/////   Public API changes (callers in HorsePanelManager updated accordingly):
/////     BuyHorse()     → now returns the assigned unique ID
/////     EquipHorse()   → new inventoryIndex parameter (the ID from BuyHorse)
/////     SellHorse()    → new inventoryIndex parameter
/////     IsEquipped()   → deprecated alias; prefer IsEquippedByIndex()
/////     FindSlotForIndex(int) → new, replaces FindSlotForData for inventory use
///// </summary>
//public class HorseArea : MonoBehaviour
//{
//    public static HorseArea Instance { get; private set; }

//    [Header("Active slots (equipped horses)")]
//    [SerializeField] private HorseSlot[] slots;

//    [Header("Area buttons")]
//    [SerializeField] private Button buyButton;
//    [Tooltip("Opens the Inventory panel where horses can be equipped, upgraded, and sold")]
//    [SerializeField] private Button inventoryButton;

//    // ── Inventory ─────────────────────────────────────────────────────────────
//    // Each purchased horse is stored as an (HorseData, uniqueId) pair so that
//    // duplicate types can be told apart.
//    private List<HorseData> _ownedHorses = new List<HorseData>();
//    private List<int> _ownedIds = new List<int>();   // parallel, same length
//    private int _nextHorseId = 0;

//    public IReadOnlyList<HorseData> OwnedHorses => _ownedHorses;

//    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        Instance = this;
//        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
//        if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryClicked);
//        RefreshButtons();
//    }

//    // ─── Area button handlers ─────────────────────────────────────────────────

//    private void OnBuyClicked() => HorsePanelManager.Instance?.OpenBuyMode();

//    private void OnInventoryClicked()
//    {
//        if (_ownedHorses.Count == 0)
//        {
//            HorsePanelManager.Instance?.OpenBuyMode();
//            return;
//        }
//        HorsePanelManager.Instance?.OpenInventoryMode();
//    }

//    // ─── Buy API ──────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Adds one copy of this horse to the owned list.
//    /// Returns the unique inventory ID assigned to this copy, or -1 on failure.
//    /// Pass this ID to EquipHorse / SellHorse so the right copy is targeted.
//    /// </summary>
//    public int BuyHorse(HorseData data)
//    {
//        if (data == null) return -1;

//        int id = _nextHorseId++;
//        _ownedHorses.Add(data);
//        _ownedIds.Add(id);

//        RefreshButtons();
//        Debug.Log($"[HorseArea] Bought '{data.horseName}' (id={id}). " +
//                  $"Total owned: {CountOwned(data)}x.");
//        return id;
//    }

//    // ─── Sell API ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Removes the copy identified by inventoryIndex (unique ID) from the
//    /// owned list and un-equips it from any slot.
//    ///
//    /// NOTE: Gold refund is NOT calculated here.
//    /// </summary>
//    public void SellHorse(HorseData data, int inventoryIndex)
//    {
//        // Un-equip from slot if currently sitting in one
//        foreach (var slot in slots)
//        {
//            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
//            {
//                slot.UnequipHorse();
//                break;
//            }
//        }

//        // Remove by unique ID (not by HorseData reference, to avoid removing the wrong copy)
//        int listPos = _ownedIds.IndexOf(inventoryIndex);
//        if (listPos >= 0)
//        {
//            _ownedHorses.RemoveAt(listPos);
//            _ownedIds.RemoveAt(listPos);
//        }
//        else
//        {
//            // Fallback: remove first matching HorseData (legacy safety net)
//            int fallback = _ownedHorses.IndexOf(data);
//            if (fallback >= 0) { _ownedHorses.RemoveAt(fallback); _ownedIds.RemoveAt(fallback); }
//        }

//        RefreshButtons();
//        Debug.Log($"[HorseArea] Sold '{data.horseName}' (id={inventoryIndex}). " +
//                  $"Remaining owned: {CountOwned(data)}x.");
//    }

//    // ─── Equip API ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Equips the copy identified by inventoryIndex into the first free slot
//    /// (or a specific target slot).
//    ///
//    /// inventoryIndex is the unique ID returned by BuyHorse(), stored per-card
//    /// in HorseLevelButton and tracked as HorsePanelManager._selectedInventoryId.
//    /// </summary>
//    public bool EquipHorse(HorseData data, int inventoryIndex, HorseSlot targetSlot = null)
//    {
//        if (!_ownedIds.Contains(inventoryIndex))
//        {
//            Debug.LogWarning($"[HorseArea] Cannot equip: id={inventoryIndex} not owned.");
//            return false;
//        }

//        // Already in a slot?
//        if (IsEquippedByIndex(inventoryIndex))
//        {
//            Debug.LogWarning($"[HorseArea] id={inventoryIndex} is already in a slot.");
//            return false;
//        }

//        HorseSlot slot = (targetSlot != null && !targetSlot.IsOccupied)
//            ? targetSlot
//            : GetFreeSlot();

//        if (slot == null)
//        {
//            Debug.LogWarning("[HorseArea] No free slot to equip into.");
//            return false;
//        }

//        slot.Equip(data, inventoryIndex);   // pass the unique ID so the slot knows which copy it holds
//        RefreshButtons();
//        return true;
//    }

//    /// <summary>
//    /// Un-equips the horse from the given slot.
//    /// The horse stays in the owned list and can be re-equipped later.
//    /// </summary>
//    public void UnequipHorse(HorseSlot slot)
//    {
//        if (slot == null || !slot.IsOccupied) return;
//        slot.UnequipHorse();
//        RefreshButtons();
//    }

//    // ─── Queries ──────────────────────────────────────────────────────────────

//    public bool HasFreeSlot() => GetFreeSlot() != null;
//    public bool HasOwnedHorse() => _ownedHorses.Count > 0;

//    /// <summary>
//    /// Returns true if the copy with this unique ID is currently in a slot.
//    /// Use this instead of IsEquipped(HorseData) when you have the ID.
//    /// </summary>
//    public bool IsEquippedByIndex(int inventoryIndex)
//    {
//        if (inventoryIndex < 0) return false;
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
//                return true;
//        return false;
//    }

//    /// <summary>
//    /// Legacy reference-equality check — returns true if ANY copy of this
//    /// HorseData type is equipped. Kept for buy-mode HUD only.
//    /// Prefer IsEquippedByIndex() for inventory-mode logic.
//    /// </summary>
//    public bool IsEquipped(HorseData data)
//    {
//        if (data == null) return false;
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//                return true;
//        return false;
//    }

//    public HorseData[] GetOwnedHorses() => _ownedHorses.ToArray();

//    /// <summary>
//    /// Returns the unique ID for the horse at list position i.
//    /// Used by PopulateOwnedCards to give each card its stable ID.
//    /// </summary>
//    public int GetInventoryId(int listPosition)
//    {
//        if (listPosition < 0 || listPosition >= _ownedIds.Count) return -1;
//        return _ownedIds[listPosition];
//    }

//    /// <summary>Returns the slot holding the copy with this unique ID, or null.</summary>
//    public HorseSlot FindSlotForIndex(int inventoryIndex)
//    {
//        if (inventoryIndex < 0) return null;
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied && slot.InventoryIndex == inventoryIndex)
//                return slot;
//        return null;
//    }

//    /// <summary>Returns the slot holding any copy of this HorseData type, or null.</summary>
//    public HorseSlot FindSlotForData(HorseData data)
//    {
//        if (data == null) return null;
//        foreach (var slot in slots)
//            if (slot != null && slot.IsOccupied && slot.CurrentData == data)
//                return slot;
//        return null;
//    }

//    /// <summary>How many copies of this horse type the player currently owns.</summary>
//    public int CountOwned(HorseData data)
//    {
//        int count = 0;
//        foreach (var h in _ownedHorses)
//            if (h == data) count++;
//        return count;
//    }

//    // ─── Private helpers ──────────────────────────────────────────────────────

//    private HorseSlot GetFreeSlot()
//    {
//        foreach (var s in slots)
//            if (s != null && !s.IsOccupied) return s;
//        return null;
//    }

//    private void RefreshButtons()
//    {
//        if (buyButton != null) buyButton.interactable = true;
//        if (inventoryButton != null) inventoryButton.interactable = _ownedHorses.Count > 0;
//    }
//}

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
}