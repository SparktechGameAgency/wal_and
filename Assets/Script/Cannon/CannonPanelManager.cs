//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// CANNON PANEL — CannonPanelManager
/////////
///////// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
/////////
/////////  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
/////////     Inventory cards spawn into a plain Transform (inventoryGridContent).
/////////     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
/////////
/////////  2. RANGE BAR replaces Ability bar.
/////////     Inspector fields: rangeBar / rangeValueText.
/////////     Max value: maxRange.
/////////
/////////  3. TIMER hidden by default.
/////////     timerText.gameObject is deactivated when not upgrading.
/////////     Shown (and counts down) only while an upgrade is running.
/////////
/////////  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
/////////     Both the value text and a separate delta label can show this.
/////////
/////////  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
/////////     buyTabButton and inventoryTabButton are never hidden by this manager.
/////////
///////// ════ HIERARCHY ══════════════════════════════════════════════════════════════
/////////
/////////  CannonPanel
/////////  ├── BackButton
/////////  ├── BuyTabButton          ← always visible; switches to Buy mode
/////////  ├── InventoryTabButton    ← always visible; switches to Inventory mode
/////////  ├── CardGrid              ← 3 pre-placed buy CannonCards
/////////  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
/////////  ├── Details Panel
/////////  │   ├── PreviewImage
/////////  │   ├── LevelText         "LEVEL 1"
/////////  │   ├── TimerText         hidden unless upgrading → shows "01:30"
/////////  │   ├── NameText
/////////  │   ├── CostText          "Cost: 100"
/////////  │   ├── RangeStatText     "Range: 40m"
/////////  │   ├── HealthBar (Filled Image) + HealthValueText
/////////  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
/////////  │   ├── DamageBar (Filled Image) + DamageValueText
/////////  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
/////////  │   │   └── UpgradeProgressBar (Filled Image)
/////////  │   ├── BuyButton + BuyButtonText
/////////  │   ├── EquipButton
/////////  │   ├── UnequipButton
/////////  │   └── UpgradeButton + UpgradeButtonText
/////////  └── CoinText
/////////
///////// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
/////////  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
/////////  Castle Slots          all CannonSlot prefab instances in the scene
/////////  Starting Gold         default 840
///////// </summary>
//////public class CannonPanelManager : MonoBehaviour
//////{
//////    public static CannonPanelManager Instance { get; private set; }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // INSPECTOR FIELDS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
//////    [SerializeField] private CannonData[] cannonTypes;

//////    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
//////    [SerializeField] private CannonSlot[] castleSlots;

//////    // ── Tab & Back Buttons ────────────────────────────────────────────────────
//////    [Header("Tab Buttons  (always visible in both modes)")]
//////    [SerializeField] private Button buyTabButton;
//////    [SerializeField] private Button inventoryTabButton;
//////    [SerializeField] private Button backButton;

//////    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
//////    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
//////    [SerializeField] private GameObject cardGridRoot;
//////    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

//////    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
//////    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
//////    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
//////    [SerializeField] private Transform inventoryGridContent;
//////    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
//////    [SerializeField] private CannonCard cannonCardPrefab;

//////    // ── Details Panel ─────────────────────────────────────────────────────────
//////    [Header("Details Panel")]
//////    [SerializeField] private Image previewImage;
//////    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
//////    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
//////    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
//////    [SerializeField] private TextMeshProUGUI nameText;
//////    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
//////    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

//////    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
//////    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
//////    [SerializeField] private Image healthBar;
//////    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
//////    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
//////    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
//////    [SerializeField] private Image damageBar;
//////    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

//////    [Header("Max values used to compute bar fill  (tune per game balance)")]
//////    [SerializeField] private float maxHealth = 200f;
//////    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
//////    [SerializeField] private float maxDamage = 100f;

//////    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
//////    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
//////    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
//////    [SerializeField] private GameObject upgradeProgressBG;
//////    [SerializeField] private Image upgradeProgressBar;

//////    // ── Action Buttons ────────────────────────────────────────────────────────
//////    [Header("Action Buttons")]
//////    [SerializeField] private Button buyButton;
//////    [SerializeField] private TextMeshProUGUI buyButtonText;
//////    [SerializeField] private Button equipButton;
//////    [SerializeField] private Button unequipButton;
//////    [SerializeField] private Button upgradeButton;
//////    [SerializeField] private TextMeshProUGUI upgradeButtonText;
//////    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
//////             + "Visible only in Inventory mode — switches back to Buy mode.")]
//////    [SerializeField] private Button inventoryBuyButton;

//////    // ── Coin & Warning ────────────────────────────────────────────────────────
//////    [Header("Coin & Warning")]
//////    [SerializeField] private TextMeshProUGUI coinText;
//////    [SerializeField] private TextMeshProUGUI warningText;

//////    [Header("Starting Gold")]
//////    [SerializeField] private int startingGold = 840;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // PRIVATE STATE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private int _gold;

//////    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//////    private int _nextId = 0;
//////    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

//////    private enum Mode { Buy, Inventory }
//////    private Mode _mode = Mode.Buy;

//////    private CannonSlot _callingSlot;

//////    // Buy mode
//////    private CannonCard _selectedBuyCard;
//////    private CannonData _selectedBuyData;

//////    // Inventory mode — dynamically spawned cards
//////    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
//////    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
//////    private CannonCard _selectedInventoryCard;

//////    private CannonInventoryEntry SelectedEntry
//////    {
//////        get
//////        {
//////            if (_selectedInventoryCard == null) return null;
//////            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
//////            return e;
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UNITY LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;

//////        _gold = startingGold;

//////        WireButtons();
//////        SetupBuyCards();
//////        RefreshCoinText();
//////        HideTimer();
//////        HideProgressBar();

//////        ShowBuyMode();   // start in Buy mode
//////    }

//////    private void Update()
//////    {
//////        if (_mode == Mode.Inventory)
//////            TickUpgrades();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // INIT
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void WireButtons()
//////    {
//////        // Tab buttons — always visible, switch mode when clicked
//////        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
//////        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
//////        backButton?.onClick.AddListener(OnBackClicked);

//////        // Action buttons
//////        buyButton?.onClick.AddListener(OnBuyClicked);
//////        equipButton?.onClick.AddListener(OnEquipClicked);
//////        unequipButton?.onClick.AddListener(OnUnequipClicked);
//////        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
//////        // Inventory-mode Buy button — takes player back to Buy mode
//////        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
//////    }

//////    private void SetupBuyCards()
//////    {
//////        if (cards == null || cannonTypes == null) return;
//////        for (int i = 0; i < cards.Length; i++)
//////        {
//////            if (cards[i] == null) continue;
//////            if (i < cannonTypes.Length && cannonTypes[i] != null)
//////                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
//////            else
//////                cards[i].gameObject.SetActive(false);
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // PUBLIC ENTRY POINTS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
//////    public void OnPanelOpened(CannonSlot callingSlot)
//////    {
//////        _callingSlot = callingSlot;
//////        gameObject.SetActive(true);
//////        RefreshCoinText();
//////        SwitchToBuyMode();
//////    }

//////    //    public void OnPanelOpened()
//////    //    {
//////    //        RefreshCoinText();
//////    //        SwitchToBuyMode();
//////    //    }

//////    /// <summary>
//////    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
//////    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
//////    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
//////    /// </summary>
//////    public void OpenAtInventory(CannonSlot callingSlot)
//////    {
//////        _callingSlot = callingSlot;
//////        gameObject.SetActive(true);
//////        RefreshCoinText();

//////        if (_inventory.Count == 0)
//////        {
//////            SwitchToBuyMode();
//////            return;
//////        }

//////        _mode = Mode.Inventory;
//////        ShowInventoryMode();

//////        // Pre-select the card that belongs to this slot's cannon
//////        if (callingSlot?.Entry != null)
//////        {
//////            int targetId = callingSlot.Entry.inventoryId;
//////            foreach (var c in _spawnedCards)
//////                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
//////                { SelectInventoryCard(c, e); break; }
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // MODE SWITCHING
//////    // ══════════════════════════════════════════════════════════════════════════

//////    // Called by BuyTabButton — always visible, works from any mode
//////    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

//////    private void SwitchToInventoryMode()
//////    {
//////        // FIX: warn and block if the player hasn't bought anything yet
//////        if (_inventory.Count == 0)
//////        {
//////            ShowWarning("Buy a cannon first!");
//////            return;
//////        }
//////        _mode = Mode.Inventory;
//////        ShowInventoryMode();
//////    }

//////    // ── Buy Mode ──────────────────────────────────────────────────────────────

//////    private void ShowBuyMode()
//////    {
//////        // Show card grid; hide inventory container
//////        if (cardGridRoot != null) cardGridRoot.SetActive(true);
//////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

//////        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
//////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
//////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

//////        // Inventory-mode Buy button is only relevant when in Inventory
//////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

//////        // Buy button visible; inventory action buttons hidden
//////        SetActionButtons(buyVisible: true, inventoryVisible: false);

//////        HideTimer();
//////        HideProgressBar();
//////        ClearWarning();

//////        // Refresh buy card locks (may have changed since last visit)
//////        if (cards != null && cannonTypes != null)
//////        {
//////            for (int i = 0; i < cards.Length; i++)
//////            {
//////                if (cards[i] == null) continue;
//////                if (i < cannonTypes.Length && cannonTypes[i] != null)
//////                {
//////                    cards[i].gameObject.SetActive(true);
//////                    cards[i].SetupBuyCard(cannonTypes[i],
//////                        locked: !_everBought.Contains(cannonTypes[i]));
//////                }
//////                else
//////                {
//////                    cards[i].gameObject.SetActive(false);
//////                }
//////            }
//////        }

//////        // Restore previous selection or auto-select first card
//////        if (_selectedBuyCard != null && cards != null
//////            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
//////        {
//////            _selectedBuyCard.SetSelected(true);
//////            ShowBuyDetails(_selectedBuyData);
//////            RefreshBuyButton();
//////        }
//////        else if (cards != null && cards.Length > 0 && cards[0] != null)
//////        {
//////            SelectBuyCard(cards[0], 0);
//////        }
//////        else
//////        {
//////            ClearDetails();
//////        }
//////    }

//////    // ── Inventory Mode ─────────────────────────────────────────────────────────

//////    private void ShowInventoryMode()
//////    {
//////        // Hide card grid; show inventory container
//////        if (cardGridRoot != null) cardGridRoot.SetActive(false);
//////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

//////        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
//////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
//////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

//////        // Show the in-inventory Buy button so the player can jump to the Buy panel
//////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

//////        // Inventory action buttons visible; buy button hidden
//////        SetActionButtons(buyVisible: false, inventoryVisible: true);

//////        ClearWarning();
//////        HideTimer();
//////        HideProgressBar();

//////        // Destroy previously spawned inventory cards
//////        foreach (CannonCard c in _spawnedCards)
//////            if (c != null) Destroy(c.gameObject);
//////        _spawnedCards.Clear();
//////        _cardEntryMap.Clear();
//////        _selectedInventoryCard = null;

//////        if (cannonCardPrefab == null || inventoryGridContent == null)
//////        {
//////            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
//////            ClearDetails();
//////            SetInventoryButtonsEmpty();
//////            return;
//////        }

//////        // Spawn one card per owned cannon entry
//////        // Card label always shows the plain cannon name — no "(1/3)" clutter.
//////        // The copy number is shown only in the Details panel via GetDetailDisplayName().
//////        foreach (var entry in _inventory)
//////        {
//////            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
//////            string displayName = entry.data.cannonName;   // plain name on the card
//////            card.SetupInventoryCard(entry, displayName);
//////            _spawnedCards.Add(card);
//////            _cardEntryMap[card] = entry;
//////        }

//////        // Auto-select: restore previous selection, else pick first card
//////        if (_spawnedCards.Count > 0)
//////        {
//////            CannonCard toSelect = null;
//////            int prevId = SelectedEntry?.inventoryId ?? -1;

//////            if (prevId >= 0)
//////            {
//////                foreach (CannonCard c in _spawnedCards)
//////                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
//////                    { toSelect = c; break; }
//////            }

//////            if (toSelect == null) toSelect = _spawnedCards[0];
//////            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
//////        }
//////        else
//////        {
//////            ClearDetails();
//////            SetInventoryButtonsEmpty();
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnCardSelected(CannonCard card)
//////    {
//////        if (_mode == Mode.Buy)
//////        {
//////            if (cards == null) return;
//////            int idx = System.Array.IndexOf(cards, card);
//////            if (idx >= 0) SelectBuyCard(card, idx);
//////        }
//////        else
//////        {
//////            if (_cardEntryMap.TryGetValue(card, out var entry))
//////                SelectInventoryCard(card, entry);
//////        }
//////        ClearWarning();
//////    }

//////    private void SelectBuyCard(CannonCard card, int idx)
//////    {
//////        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
//////        _selectedBuyCard = card;
//////        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
//////        card.SetSelected(true);

//////        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
//////        else ClearDetails();
//////    }

//////    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
//////    {
//////        foreach (var c in _spawnedCards) c?.SetSelected(false);
//////        _selectedInventoryCard = card;
//////        card?.SetSelected(true);

//////        ShowInventoryDetails(entry);
//////        RefreshInventoryButtons(entry);
//////        RefreshTimerAndProgress(entry);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // BUY
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void OnBuyClicked()
//////    {
//////        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
//////        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

//////        _gold -= _selectedBuyData.cost;
//////        RefreshCoinText();

//////        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
//////        _inventory.Add(entry);

//////        // Unlock the buy card on first purchase of this type
//////        if (!_everBought.Contains(_selectedBuyData))
//////        {
//////            _everBought.Add(_selectedBuyData);
//////            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
//////                if (cannonTypes[i] == _selectedBuyData)
//////                    cards[i]?.SetLocked(false);
//////        }

//////        RefreshBuyButton();
//////        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
//////        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
//////    }

//////    private void RefreshBuyButton()
//////    {
//////        if (buyButton == null) return;
//////        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
//////        buyButton.interactable = canAfford;
//////        if (buyButtonText != null)
//////            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // EQUIP / UNEQUIP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void OnEquipClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

//////        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
//////            ? _callingSlot
//////            : FindFreeSlot();

//////        if (target == null) { ShowWarning("No free cannon slot!"); return; }

//////        target.Equip(entry);
//////        RefreshInventoryButtons(entry);
//////        ShowWarning($"Equipped {entry.data.cannonName}!");
//////    }

//////    private void OnUnequipClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
//////        entry.equippedSlot?.Unequip();
//////    }

//////    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
//////    public void RefreshAfterUnequip()
//////    {
//////        if (_mode == Mode.Inventory) ShowInventoryMode();
//////    }

//////    private CannonSlot FindFreeSlot()
//////    {
//////        if (castleSlots == null) return null;
//////        foreach (var s in castleSlots)
//////            if (s != null && !s.IsOccupied) return s;
//////        return null;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UPGRADE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void OnUpgradeClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
//////        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

//////        entry.isUpgrading = true;
//////        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//////        // Activate the badge on the card right away so the player sees the upgrade is running
//////        _selectedInventoryCard?.RefreshBadge(entry);

//////        // Immediately update UI to show timer, progress bar, and "40+7" stat format
//////        ShowInventoryDetails(entry);
//////        RefreshInventoryButtons(entry);
//////        RefreshTimerAndProgress(entry);
//////        ShowWarning("Upgrading…");
//////    }

//////    private void TickUpgrades()
//////    {
//////        bool anyCompleted = false;

//////        foreach (var entry in _inventory)
//////        {
//////            if (!entry.isUpgrading) continue;

//////            float remaining = entry.UpgradeTimeRemaining;

//////            // Update timer + progress bar for the currently selected entry
//////            if (entry == SelectedEntry)
//////            {
//////                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//////                float progress = 1f - Mathf.Clamp01(remaining / total);

//////                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//////                if (timerText != null) timerText.text = FormatTimer(remaining);
//////            }

//////            if (remaining <= 0f)
//////            {
//////                entry.upgradeCount++;
//////                entry.isUpgrading = false;
//////                anyCompleted = true;

//////                // Refresh the badge on the card in-place
//////                foreach (var c in _spawnedCards)
//////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
//////                        c.RefreshBadge(entry);

//////                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
//////                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
//////            }
//////        }

//////        if (anyCompleted)
//////        {
//////            // Rebuild card list so everything reflects the new level
//////            CannonInventoryEntry prevSel = SelectedEntry;
//////            ShowInventoryMode();

//////            if (prevSel != null)
//////            {
//////                foreach (var c in _spawnedCards)
//////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
//////                    { SelectInventoryCard(c, e); break; }
//////            }
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DETAILS PANEL — BUY MODE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void ShowBuyDetails(CannonData data)
//////    {
//////        if (data == null) { ClearDetails(); return; }

//////        ApplyPreview(data.previewSprite
//////            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

//////        if (levelText != null) levelText.text = "LEVEL 1";
//////        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
//////        ;
//////        if (costText != null) costText.text = $"Cost: {data.cost}";
//////        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

//////        // Plain values — no delta, no timer
//////        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
//////        HideTimer();
//////        HideProgressBar();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DETAILS PANEL — INVENTORY MODE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void ShowInventoryDetails(CannonInventoryEntry entry)
//////    {
//////        if (entry == null) { ClearDetails(); return; }

//////        ApplyPreview(entry.data.previewSprite
//////            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

//////        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
//////        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
//////        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
//////        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

//////        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
//////        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
//////                   upgrading: showDelta, entry: entry);
//////    }

//////    // ── Details helpers ────────────────────────────────────────────────────────

//////    private void ClearDetails()
//////    {
//////        ApplyPreview(null);
//////        if (levelText != null) levelText.text = "LEVEL 1";
//////        if (nameText != null) nameText.text = "—";
//////        if (costText != null) costText.text = "";
//////        if (rangeStatText != null) rangeStatText.text = "";
//////        SetHUDBars(0f, 0f, 0f, upgrading: false);
//////        HideTimer();
//////    }

//////    private void ApplyPreview(Sprite s)
//////    {
//////        if (previewImage == null) return;
//////        previewImage.enabled = s != null;
//////        if (s != null) previewImage.sprite = s;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HUD BARS — Health / Range / Damage
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Sets the three stat bars.
//////    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
//////    /// (current value + the gain from the next upgrade level).
//////    /// </summary>
//////    private void SetHUDBars(float h, float r, float d,
//////                            bool upgrading,
//////                            CannonInventoryEntry entry = null)
//////    {
//////        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
//////        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
//////        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

//////        if (upgrading && entry != null && !entry.IsMaxLevel)
//////        {
//////            // Peek at next-level stats without permanently mutating the entry
//////            entry.upgradeCount++;
//////            float nh = entry.CurrentHealth;
//////            float nr = entry.CurrentRange;
//////            float nd = entry.CurrentDamage;
//////            entry.upgradeCount--;

//////            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
//////            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
//////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
//////            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
//////        }
//////        else
//////        {
//////            if (healthValueText != null) healthValueText.text = $"{h:F0}";
//////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
//////            if (damageValueText != null) damageValueText.text = $"{d:F0}";
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // TIMER & PROGRESS BAR
//////    // Timer is hidden by default; activated only during an upgrade.
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
//////    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
//////    {
//////        if (entry != null && entry.isUpgrading)
//////        {
//////            ShowTimer(entry.UpgradeTimeRemaining);
//////            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//////            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//////            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
//////            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//////        }
//////        else
//////        {
//////            HideTimer();
//////            HideProgressBar();
//////        }
//////    }

//////    // Timer — show only while upgrading
//////    private void ShowTimer(float seconds)
//////    {
//////        if (timerText == null) return;
//////        timerText.gameObject.SetActive(true);
//////        timerText.text = FormatTimer(seconds);
//////    }

//////    private void HideTimer()
//////    {
//////        if (timerText != null) timerText.gameObject.SetActive(false);
//////    }

//////    // Progress bar — show only while upgrading
//////    private void ShowProgressBar()
//////    {
//////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//////    }

//////    private void HideProgressBar()
//////    {
//////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // INVENTORY BUTTON STATES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void RefreshInventoryButtons(CannonInventoryEntry entry)
//////    {
//////        if (entry == null) { SetInventoryButtonsEmpty(); return; }

//////        bool equipped = entry.isEquipped;
//////        bool maxLevel = entry.IsMaxLevel;
//////        bool upgrading = entry.isUpgrading;

//////        // Equip / Unequip are mutually exclusive
//////        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
//////        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

//////        if (upgradeButton != null)
//////        {
//////            upgradeButton.interactable = !maxLevel && !upgrading;
//////            if (upgradeButtonText != null)
//////            {
//////                if (maxLevel) upgradeButtonText.text = "MAX";
//////                else if (upgrading) upgradeButtonText.text = "";
//////                else upgradeButtonText.text =
//////                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//////            }
//////        }
//////    }

//////    private void SetInventoryButtonsEmpty()
//////    {
//////        if (equipButton != null) equipButton.interactable = false;
//////        if (unequipButton != null) unequipButton.interactable = false;
//////        if (upgradeButton != null)
//////        {
//////            upgradeButton.interactable = false;
//////            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
//////        }
//////    }

//////    /// <summary>
//////    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
//////    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
//////    /// they are always visible so the player can switch modes freely.
//////    /// </summary>
//////    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
//////    {
//////        buyButton?.gameObject.SetActive(buyVisible);
//////        equipButton?.gameObject.SetActive(inventoryVisible);
//////        unequipButton?.gameObject.SetActive(inventoryVisible);
//////        upgradeButton?.gameObject.SetActive(inventoryVisible);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // BACK / CLOSE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void OnBackClicked()
//////    {
//////        ClearWarning();

//////        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
//////        if (_mode == Mode.Inventory)
//////        {
//////            SwitchToBuyMode();
//////            return;
//////        }

//////        // In Buy mode — close the panel as usual
//////        if (GameManager.Instance != null)
//////            GameManager.Instance.CloseCurrentPanel();
//////        else
//////            gameObject.SetActive(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // COIN
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void RefreshCoinText()
//////    {
//////        if (coinText != null) coinText.text = _gold.ToString();
//////    }

//////    public void AddGold(int amount)
//////    {
//////        _gold += amount;
//////        RefreshCoinText();
//////        if (_mode == Mode.Buy) RefreshBuyButton();
//////    }

//////    public int GetGold() => _gold;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // WARNING / FEEDBACK
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void ShowWarning(string msg)
//////    {
//////        if (warningText == null) return;
//////        warningText.text = msg;
//////        CancelInvoke(nameof(ClearWarning));
//////        Invoke(nameof(ClearWarning), 2.5f);
//////    }

//////    private void ClearWarning()
//////    {
//////        if (warningText != null) warningText.text = "";
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UTILITIES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
//////        => totalCopies > 1
//////            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
//////            : entry.data.cannonName;

//////    private string GetDetailDisplayName(CannonInventoryEntry entry)
//////    {
//////        int total = 0, myIdx = 0;
//////        foreach (var e in _inventory)
//////        {
//////            if (e.data == entry.data) total++;
//////            if (e == entry && myIdx == 0) myIdx = total;
//////        }
//////        return total > 1
//////            ? $"{entry.data.cannonName} ({myIdx}/{total})"
//////            : entry.data.cannonName;
//////    }

//////    private static string FormatTimer(float seconds)
//////    {
//////        float s = Mathf.Max(0f, seconds);
//////        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//////    }

//////    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

//////    public int CountOwned(CannonData data)
//////    {
//////        int n = 0;
//////        foreach (var e in _inventory) if (e.data == data) n++;
//////        return n;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // EDITOR VALIDATION
//////    // ══════════════════════════════════════════════════════════════════════════

//////#if UNITY_EDITOR
//////    private void OnValidate()
//////    {
//////        if (cannonTypes == null || cannonTypes.Length == 0)
//////            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
//////        if (cards == null || cards.Length == 0)
//////            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
//////        if (cannonCardPrefab == null)
//////            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
//////        if (inventoryGridContent == null)
//////            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
//////        if (buyButton == null)
//////            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
//////    }
//////#endif
//////}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonPanelManager
///////
/////// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
///////
///////  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
///////     Inventory cards spawn into a plain Transform (inventoryGridContent).
///////     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
///////
///////  2. RANGE BAR replaces Ability bar.
///////     Inspector fields: rangeBar / rangeValueText.
///////     Max value: maxRange.
///////
///////  3. TIMER hidden by default.
///////     timerText.gameObject is deactivated when not upgrading.
///////     Shown (and counts down) only while an upgrade is running.
///////
///////  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
///////     Both the value text and a separate delta label can show this.
///////
///////  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
///////     buyTabButton and inventoryTabButton are never hidden by this manager.
///////
/////// ════ HIERARCHY ══════════════════════════════════════════════════════════════
///////
///////  CannonPanel
///////  ├── BackButton
///////  ├── BuyTabButton          ← always visible; switches to Buy mode
///////  ├── InventoryTabButton    ← always visible; switches to Inventory mode
///////  ├── CardGrid              ← 3 pre-placed buy CannonCards
///////  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
///////  ├── Details Panel
///////  │   ├── PreviewImage
///////  │   ├── LevelText         "LEVEL 1"
///////  │   ├── TimerText         hidden unless upgrading → shows "01:30"
///////  │   ├── NameText
///////  │   ├── CostText          "Cost: 100"
///////  │   ├── RangeStatText     "Range: 40m"
///////  │   ├── HealthBar (Filled Image) + HealthValueText
///////  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
///////  │   ├── DamageBar (Filled Image) + DamageValueText
///////  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
///////  │   │   └── UpgradeProgressBar (Filled Image)
///////  │   ├── BuyButton + BuyButtonText
///////  │   ├── EquipButton
///////  │   ├── UnequipButton
///////  │   └── UpgradeButton + UpgradeButtonText
///////  └── CoinText
///////
/////// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
///////  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
///////  Castle Slots          all CannonSlot prefab instances in the scene
///////  Starting Gold         default 840
/////// </summary>
////public class CannonPanelManager : MonoBehaviour
////{
////    public static CannonPanelManager Instance { get; private set; }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INSPECTOR FIELDS
////    // ══════════════════════════════════════════════════════════════════════════

////    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
////    [SerializeField] private CannonData[] cannonTypes;

////    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
////    [SerializeField] private CannonSlot[] castleSlots;

////    // ── Tab & Back Buttons ────────────────────────────────────────────────────
////    [Header("Tab Buttons  (always visible in both modes)")]
////    [SerializeField] private Button buyTabButton;
////    [SerializeField] private Button inventoryTabButton;
////    [SerializeField] private Button backButton;

////    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
////    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
////    [SerializeField] private GameObject cardGridRoot;
////    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

////    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
////    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
////    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
////    [SerializeField] private Transform inventoryGridContent;
////    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
////    [SerializeField] private CannonCard cannonCardPrefab;

////    // ── Details Panel ─────────────────────────────────────────────────────────
////    [Header("Details Panel")]
////    [SerializeField] private Image previewImage;
////    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
////    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
////    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
////    [SerializeField] private TextMeshProUGUI nameText;
////    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
////    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

////    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
////    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
////    [SerializeField] private Image healthBar;
////    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
////    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
////    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
////    [SerializeField] private Image damageBar;
////    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

////    [Header("Max values used to compute bar fill  (tune per game balance)")]
////    [SerializeField] private float maxHealth = 200f;
////    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
////    [SerializeField] private float maxDamage = 100f;

////    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
////    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
////    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
////    [SerializeField] private GameObject upgradeProgressBG;
////    [SerializeField] private Image upgradeProgressBar;

////    // ── Action Buttons ────────────────────────────────────────────────────────
////    [Header("Action Buttons")]
////    [SerializeField] private Button buyButton;
////    [SerializeField] private TextMeshProUGUI buyButtonText;
////    [SerializeField] private Button equipButton;
////    [SerializeField] private Button unequipButton;
////    [SerializeField] private Button upgradeButton;
////    [SerializeField] private TextMeshProUGUI upgradeButtonText;
////    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
////             + "Visible only in Inventory mode — switches back to Buy mode.")]
////    [SerializeField] private Button inventoryBuyButton;

////    // ── Coin & Warning ────────────────────────────────────────────────────────
////    [Header("Coin & Warning")]
////    [SerializeField] private TextMeshProUGUI coinText;
////    [SerializeField] private TextMeshProUGUI warningText;

////    [Header("Starting Gold")]
////    [SerializeField] private int startingGold = 840;

////    // ══════════════════════════════════════════════════════════════════════════
////    // PRIVATE STATE
////    // ══════════════════════════════════════════════════════════════════════════

////    private int _gold;

////    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
////    private int _nextId = 0;
////    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

////    private enum Mode { Buy, Inventory }
////    private Mode _mode = Mode.Buy;

////    private CannonSlot _callingSlot;

////    // Buy mode
////    private CannonCard _selectedBuyCard;
////    private CannonData _selectedBuyData;

////    // Inventory mode — dynamically spawned cards
////    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
////    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
////    private CannonCard _selectedInventoryCard;

////    private CannonInventoryEntry SelectedEntry
////    {
////        get
////        {
////            if (_selectedInventoryCard == null) return null;
////            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
////            return e;
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        _gold = startingGold;

////        WireButtons();
////        SetupBuyCards();
////        RefreshCoinText();
////        HideTimer();
////        HideProgressBar();

////        ShowBuyMode();   // start in Buy mode
////    }

////    private void Update()
////    {
////        if (_mode == Mode.Inventory)
////            TickUpgrades();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INIT
////    // ══════════════════════════════════════════════════════════════════════════

////    private void WireButtons()
////    {
////        // Tab buttons — always visible, switch mode when clicked
////        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
////        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
////        backButton?.onClick.AddListener(OnBackClicked);

////        // Action buttons
////        buyButton?.onClick.AddListener(OnBuyClicked);
////        equipButton?.onClick.AddListener(OnEquipClicked);
////        unequipButton?.onClick.AddListener(OnUnequipClicked);
////        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
////        // Inventory-mode Buy button — takes player back to Buy mode
////        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
////    }

////    private void SetupBuyCards()
////    {
////        if (cards == null || cannonTypes == null) return;
////        for (int i = 0; i < cards.Length; i++)
////        {
////            if (cards[i] == null) continue;
////            if (i < cannonTypes.Length && cannonTypes[i] != null)
////                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
////            else
////                cards[i].gameObject.SetActive(false);
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PUBLIC ENTRY POINTS
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
////    public void OnPanelOpened(CannonSlot callingSlot)
////    {
////        _callingSlot = callingSlot;
////        gameObject.SetActive(true);
////        RefreshCoinText();
////        SwitchToBuyMode();
////    }

////    //    public void OnPanelOpened()
////    //    {
////    //        RefreshCoinText();
////    //        SwitchToBuyMode();
////    //    }

////    /// <summary>
////    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
////    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
////    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
////    /// </summary>
////    public void OpenAtInventory(CannonSlot callingSlot)
////    {
////        _callingSlot = callingSlot;
////        gameObject.SetActive(true);
////        RefreshCoinText();

////        if (_inventory.Count == 0)
////        {
////            SwitchToBuyMode();
////            return;
////        }

////        _mode = Mode.Inventory;
////        ShowInventoryMode();

////        // Pre-select the card that belongs to this slot's cannon
////        if (callingSlot?.Entry != null)
////        {
////            int targetId = callingSlot.Entry.inventoryId;
////            foreach (var c in _spawnedCards)
////                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
////                { SelectInventoryCard(c, e); break; }
////        }
////    }

////    /// <summary>
////    /// Called when the player clicks a CannonSlotCastle (castle-grid cannon zone).
////    /// Opens the panel in Buy mode so the player can purchase/assign a cannon.
////    /// _callingSlot is set to null because CannonSlotCastle is not a CannonSlot.
////    /// </summary>
////    public void OpenFromCastleSlot(CannonSlotCastle castleSlot)
////    {
////        _callingSlot = null;
////        gameObject.SetActive(true);
////        RefreshCoinText();
////        SwitchToBuyMode();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // MODE SWITCHING
////    // ══════════════════════════════════════════════════════════════════════════

////    // Called by BuyTabButton — always visible, works from any mode
////    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

////    private void SwitchToInventoryMode()
////    {
////        // FIX: warn and block if the player hasn't bought anything yet
////        if (_inventory.Count == 0)
////        {
////            ShowWarning("Buy a cannon first!");
////            return;
////        }
////        _mode = Mode.Inventory;
////        ShowInventoryMode();
////    }

////    // ── Buy Mode ──────────────────────────────────────────────────────────────

////    private void ShowBuyMode()
////    {
////        // Show card grid; hide inventory container
////        if (cardGridRoot != null) cardGridRoot.SetActive(true);
////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

////        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

////        // Inventory-mode Buy button is only relevant when in Inventory
////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

////        // Buy button visible; inventory action buttons hidden
////        SetActionButtons(buyVisible: true, inventoryVisible: false);

////        HideTimer();
////        HideProgressBar();
////        ClearWarning();

////        // Refresh buy card locks (may have changed since last visit)
////        if (cards != null && cannonTypes != null)
////        {
////            for (int i = 0; i < cards.Length; i++)
////            {
////                if (cards[i] == null) continue;
////                if (i < cannonTypes.Length && cannonTypes[i] != null)
////                {
////                    cards[i].gameObject.SetActive(true);
////                    cards[i].SetupBuyCard(cannonTypes[i],
////                        locked: !_everBought.Contains(cannonTypes[i]));
////                }
////                else
////                {
////                    cards[i].gameObject.SetActive(false);
////                }
////            }
////        }

////        // Restore previous selection or auto-select first card
////        if (_selectedBuyCard != null && cards != null
////            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
////        {
////            _selectedBuyCard.SetSelected(true);
////            ShowBuyDetails(_selectedBuyData);
////            RefreshBuyButton();
////        }
////        else if (cards != null && cards.Length > 0 && cards[0] != null)
////        {
////            SelectBuyCard(cards[0], 0);
////        }
////        else
////        {
////            ClearDetails();
////        }
////    }

////    // ── Inventory Mode ─────────────────────────────────────────────────────────

////    private void ShowInventoryMode()
////    {
////        // Hide card grid; show inventory container
////        if (cardGridRoot != null) cardGridRoot.SetActive(false);
////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

////        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

////        // Show the in-inventory Buy button so the player can jump to the Buy panel
////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

////        // Inventory action buttons visible; buy button hidden
////        SetActionButtons(buyVisible: false, inventoryVisible: true);

////        ClearWarning();
////        HideTimer();
////        HideProgressBar();

////        // Destroy previously spawned inventory cards
////        foreach (CannonCard c in _spawnedCards)
////            if (c != null) Destroy(c.gameObject);
////        _spawnedCards.Clear();
////        _cardEntryMap.Clear();
////        _selectedInventoryCard = null;

////        if (cannonCardPrefab == null || inventoryGridContent == null)
////        {
////            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////            return;
////        }

////        // Spawn one card per owned cannon entry
////        // Card label always shows the plain cannon name — no "(1/3)" clutter.
////        // The copy number is shown only in the Details panel via GetDetailDisplayName().
////        foreach (var entry in _inventory)
////        {
////            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
////            string displayName = entry.data.cannonName;   // plain name on the card
////            card.SetupInventoryCard(entry, displayName);
////            _spawnedCards.Add(card);
////            _cardEntryMap[card] = entry;
////        }

////        // Auto-select: restore previous selection, else pick first card
////        if (_spawnedCards.Count > 0)
////        {
////            CannonCard toSelect = null;
////            int prevId = SelectedEntry?.inventoryId ?? -1;

////            if (prevId >= 0)
////            {
////                foreach (CannonCard c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
////                    { toSelect = c; break; }
////            }

////            if (toSelect == null) toSelect = _spawnedCards[0];
////            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
////        }
////        else
////        {
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnCardSelected(CannonCard card)
////    {
////        if (_mode == Mode.Buy)
////        {
////            if (cards == null) return;
////            int idx = System.Array.IndexOf(cards, card);
////            if (idx >= 0) SelectBuyCard(card, idx);
////        }
////        else
////        {
////            if (_cardEntryMap.TryGetValue(card, out var entry))
////                SelectInventoryCard(card, entry);
////        }
////        ClearWarning();
////    }

////    private void SelectBuyCard(CannonCard card, int idx)
////    {
////        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
////        _selectedBuyCard = card;
////        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
////        card.SetSelected(true);

////        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
////        else ClearDetails();
////    }

////    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
////    {
////        foreach (var c in _spawnedCards) c?.SetSelected(false);
////        _selectedInventoryCard = card;
////        card?.SetSelected(true);

////        ShowInventoryDetails(entry);
////        RefreshInventoryButtons(entry);
////        RefreshTimerAndProgress(entry);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BUY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBuyClicked()
////    {
////        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
////        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

////        _gold -= _selectedBuyData.cost;
////        RefreshCoinText();

////        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
////        _inventory.Add(entry);

////        // Unlock the buy card on first purchase of this type
////        if (!_everBought.Contains(_selectedBuyData))
////        {
////            _everBought.Add(_selectedBuyData);
////            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
////                if (cannonTypes[i] == _selectedBuyData)
////                    cards[i]?.SetLocked(false);
////        }

////        RefreshBuyButton();
////        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
////        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
////    }

////    private void RefreshBuyButton()
////    {
////        if (buyButton == null) return;
////        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
////        buyButton.interactable = canAfford;
////        if (buyButtonText != null)
////            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // EQUIP / UNEQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnEquipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

////        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
////            ? _callingSlot
////            : FindFreeSlot();

////        if (target == null) { ShowWarning("No free cannon slot!"); return; }

////        target.Equip(entry);
////        RefreshInventoryButtons(entry);
////        ShowWarning($"Equipped {entry.data.cannonName}!");
////    }

////    private void OnUnequipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
////        entry.equippedSlot?.Unequip();
////    }

////    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
////    public void RefreshAfterUnequip()
////    {
////        if (_mode == Mode.Inventory) ShowInventoryMode();
////    }

////    private CannonSlot FindFreeSlot()
////    {
////        if (castleSlots == null) return null;
////        foreach (var s in castleSlots)
////            if (s != null && !s.IsOccupied) return s;
////        return null;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UPGRADE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnUpgradeClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
////        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

////        entry.isUpgrading = true;
////        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

////        // Activate the badge on the card right away so the player sees the upgrade is running
////        _selectedInventoryCard?.RefreshBadge(entry);

////        // Immediately update UI to show timer, progress bar, and "40+7" stat format
////        ShowInventoryDetails(entry);
////        RefreshInventoryButtons(entry);
////        RefreshTimerAndProgress(entry);
////        ShowWarning("Upgrading…");
////    }

////    private void TickUpgrades()
////    {
////        bool anyCompleted = false;

////        foreach (var entry in _inventory)
////        {
////            if (!entry.isUpgrading) continue;

////            float remaining = entry.UpgradeTimeRemaining;

////            // Update timer + progress bar for the currently selected entry
////            if (entry == SelectedEntry)
////            {
////                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
////                float progress = 1f - Mathf.Clamp01(remaining / total);

////                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
////                if (timerText != null) timerText.text = FormatTimer(remaining);
////            }

////            if (remaining <= 0f)
////            {
////                entry.upgradeCount++;
////                entry.isUpgrading = false;
////                anyCompleted = true;

////                // Refresh the badge on the card in-place
////                foreach (var c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
////                        c.RefreshBadge(entry);

////                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
////                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
////            }
////        }

////        if (anyCompleted)
////        {
////            // Rebuild card list so everything reflects the new level
////            CannonInventoryEntry prevSel = SelectedEntry;
////            ShowInventoryMode();

////            if (prevSel != null)
////            {
////                foreach (var c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
////                    { SelectInventoryCard(c, e); break; }
////            }
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DETAILS PANEL — BUY MODE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowBuyDetails(CannonData data)
////    {
////        if (data == null) { ClearDetails(); return; }

////        ApplyPreview(data.previewSprite
////            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
////        ;
////        if (costText != null) costText.text = $"Cost: {data.cost}";
////        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

////        // Plain values — no delta, no timer
////        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
////        HideTimer();
////        HideProgressBar();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DETAILS PANEL — INVENTORY MODE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowInventoryDetails(CannonInventoryEntry entry)
////    {
////        if (entry == null) { ClearDetails(); return; }

////        ApplyPreview(entry.data.previewSprite
////            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
////        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
////        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
////        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

////        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
////        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
////                   upgrading: showDelta, entry: entry);
////    }

////    // ── Details helpers ────────────────────────────────────────────────────────

////    private void ClearDetails()
////    {
////        ApplyPreview(null);
////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (nameText != null) nameText.text = "—";
////        if (costText != null) costText.text = "";
////        if (rangeStatText != null) rangeStatText.text = "";
////        SetHUDBars(0f, 0f, 0f, upgrading: false);
////        HideTimer();
////    }

////    private void ApplyPreview(Sprite s)
////    {
////        if (previewImage == null) return;
////        previewImage.enabled = s != null;
////        if (s != null) previewImage.sprite = s;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HUD BARS — Health / Range / Damage
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Sets the three stat bars.
////    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
////    /// (current value + the gain from the next upgrade level).
////    /// </summary>
////    private void SetHUDBars(float h, float r, float d,
////                            bool upgrading,
////                            CannonInventoryEntry entry = null)
////    {
////        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
////        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
////        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

////        if (upgrading && entry != null && !entry.IsMaxLevel)
////        {
////            // Peek at next-level stats without permanently mutating the entry
////            entry.upgradeCount++;
////            float nh = entry.CurrentHealth;
////            float nr = entry.CurrentRange;
////            float nd = entry.CurrentDamage;
////            entry.upgradeCount--;

////            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
////            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
////            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
////        }
////        else
////        {
////            if (healthValueText != null) healthValueText.text = $"{h:F0}";
////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
////            if (damageValueText != null) damageValueText.text = $"{d:F0}";
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // TIMER & PROGRESS BAR
////    // Timer is hidden by default; activated only during an upgrade.
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
////    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
////    {
////        if (entry != null && entry.isUpgrading)
////        {
////            ShowTimer(entry.UpgradeTimeRemaining);
////            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
////            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
////            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
////            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
////        }
////        else
////        {
////            HideTimer();
////            HideProgressBar();
////        }
////    }

////    // Timer — show only while upgrading
////    private void ShowTimer(float seconds)
////    {
////        if (timerText == null) return;
////        timerText.gameObject.SetActive(true);
////        timerText.text = FormatTimer(seconds);
////    }

////    private void HideTimer()
////    {
////        if (timerText != null) timerText.gameObject.SetActive(false);
////    }

////    // Progress bar — show only while upgrading
////    private void ShowProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
////    }

////    private void HideProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INVENTORY BUTTON STATES
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RefreshInventoryButtons(CannonInventoryEntry entry)
////    {
////        if (entry == null) { SetInventoryButtonsEmpty(); return; }

////        bool equipped = entry.isEquipped;
////        bool maxLevel = entry.IsMaxLevel;
////        bool upgrading = entry.isUpgrading;

////        // Equip / Unequip are mutually exclusive
////        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
////        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

////        if (upgradeButton != null)
////        {
////            upgradeButton.interactable = !maxLevel && !upgrading;
////            if (upgradeButtonText != null)
////            {
////                if (maxLevel) upgradeButtonText.text = "MAX";
////                else if (upgrading) upgradeButtonText.text = "";
////                else upgradeButtonText.text =
////                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
////            }
////        }
////    }

////    private void SetInventoryButtonsEmpty()
////    {
////        if (equipButton != null) equipButton.interactable = false;
////        if (unequipButton != null) unequipButton.interactable = false;
////        if (upgradeButton != null)
////        {
////            upgradeButton.interactable = false;
////            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
////        }
////    }

////    /// <summary>
////    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
////    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
////    /// they are always visible so the player can switch modes freely.
////    /// </summary>
////    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
////    {
////        buyButton?.gameObject.SetActive(buyVisible);
////        equipButton?.gameObject.SetActive(inventoryVisible);
////        unequipButton?.gameObject.SetActive(inventoryVisible);
////        upgradeButton?.gameObject.SetActive(inventoryVisible);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BACK / CLOSE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBackClicked()
////    {
////        ClearWarning();

////        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
////        if (_mode == Mode.Inventory)
////        {
////            SwitchToBuyMode();
////            return;
////        }

////        // In Buy mode — close the panel as usual
////        if (GameManager.Instance != null)
////            GameManager.Instance.CloseCurrentPanel();
////        else
////            gameObject.SetActive(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // COIN
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RefreshCoinText()
////    {
////        if (coinText != null) coinText.text = _gold.ToString();
////    }

////    public void AddGold(int amount)
////    {
////        _gold += amount;
////        RefreshCoinText();
////        if (_mode == Mode.Buy) RefreshBuyButton();
////    }

////    public int GetGold() => _gold;

////    // ══════════════════════════════════════════════════════════════════════════
////    // WARNING / FEEDBACK
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowWarning(string msg)
////    {
////        if (warningText == null) return;
////        warningText.text = msg;
////        CancelInvoke(nameof(ClearWarning));
////        Invoke(nameof(ClearWarning), 2.5f);
////    }

////    private void ClearWarning()
////    {
////        if (warningText != null) warningText.text = "";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UTILITIES
////    // ══════════════════════════════════════════════════════════════════════════

////    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
////        => totalCopies > 1
////            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
////            : entry.data.cannonName;

////    private string GetDetailDisplayName(CannonInventoryEntry entry)
////    {
////        int total = 0, myIdx = 0;
////        foreach (var e in _inventory)
////        {
////            if (e.data == entry.data) total++;
////            if (e == entry && myIdx == 0) myIdx = total;
////        }
////        return total > 1
////            ? $"{entry.data.cannonName} ({myIdx}/{total})"
////            : entry.data.cannonName;
////    }

////    private static string FormatTimer(float seconds)
////    {
////        float s = Mathf.Max(0f, seconds);
////        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
////    }

////    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

////    public int CountOwned(CannonData data)
////    {
////        int n = 0;
////        foreach (var e in _inventory) if (e.data == data) n++;
////        return n;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // EDITOR VALIDATION
////    // ══════════════════════════════════════════════════════════════════════════

////#if UNITY_EDITOR
////    private void OnValidate()
////    {
////        if (cannonTypes == null || cannonTypes.Length == 0)
////            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
////        if (cards == null || cards.Length == 0)
////            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
////        if (cannonCardPrefab == null)
////            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
////        if (inventoryGridContent == null)
////            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
////        if (buyButton == null)
////            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
////    }
////#endif
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonPanelManager
/////
///// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
/////
/////  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
/////     Inventory cards spawn into a plain Transform (inventoryGridContent).
/////     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
/////
/////  2. RANGE BAR replaces Ability bar.
/////     Inspector fields: rangeBar / rangeValueText.
/////     Max value: maxRange.
/////
/////  3. TIMER hidden by default.
/////     timerText.gameObject is deactivated when not upgrading.
/////     Shown (and counts down) only while an upgrade is running.
/////
/////  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
/////     Both the value text and a separate delta label can show this.
/////
/////  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
/////     buyTabButton and inventoryTabButton are never hidden by this manager.
/////
///// ════ HIERARCHY ══════════════════════════════════════════════════════════════
/////
/////  CannonPanel
/////  ├── BackButton
/////  ├── BuyTabButton          ← always visible; switches to Buy mode
/////  ├── InventoryTabButton    ← always visible; switches to Inventory mode
/////  ├── CardGrid              ← 3 pre-placed buy CannonCards
/////  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
/////  ├── Details Panel
/////  │   ├── PreviewImage
/////  │   ├── LevelText         "LEVEL 1"
/////  │   ├── TimerText         hidden unless upgrading → shows "01:30"
/////  │   ├── NameText
/////  │   ├── CostText          "Cost: 100"
/////  │   ├── RangeStatText     "Range: 40m"
/////  │   ├── HealthBar (Filled Image) + HealthValueText
/////  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
/////  │   ├── DamageBar (Filled Image) + DamageValueText
/////  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
/////  │   │   └── UpgradeProgressBar (Filled Image)
/////  │   ├── BuyButton + BuyButtonText
/////  │   ├── EquipButton
/////  │   ├── UnequipButton
/////  │   └── UpgradeButton + UpgradeButtonText
/////  └── CoinText
/////
///// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
/////  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
/////  Castle Slots          all CannonSlot prefab instances in the scene
/////  Starting Gold         default 840
///// </summary>
//public class CannonPanelManager : MonoBehaviour
//{
//    public static CannonPanelManager Instance { get; private set; }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INSPECTOR FIELDS
//    // ══════════════════════════════════════════════════════════════════════════

//    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
//    [SerializeField] private CannonData[] cannonTypes;

//    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
//    [SerializeField] private CannonSlot[] castleSlots;

//    // ── Tab & Back Buttons ────────────────────────────────────────────────────
//    [Header("Tab Buttons  (always visible in both modes)")]
//    [SerializeField] private Button buyTabButton;
//    [SerializeField] private Button inventoryTabButton;
//    [SerializeField] private Button backButton;

//    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
//    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
//    [SerializeField] private GameObject cardGridRoot;
//    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

//    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
//    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
//    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
//    [SerializeField] private Transform inventoryGridContent;
//    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
//    [SerializeField] private CannonCard cannonCardPrefab;

//    // ── Details Panel ─────────────────────────────────────────────────────────
//    [Header("Details Panel")]
//    [SerializeField] private Image previewImage;
//    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
//    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
//    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
//    [SerializeField] private TextMeshProUGUI nameText;
//    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
//    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

//    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
//    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
//    [SerializeField] private Image healthBar;
//    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
//    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
//    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
//    [SerializeField] private Image damageBar;
//    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

//    [Header("Max values used to compute bar fill  (tune per game balance)")]
//    [SerializeField] private float maxHealth = 200f;
//    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
//    [SerializeField] private float maxDamage = 100f;

//    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
//    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
//    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
//    [SerializeField] private GameObject upgradeProgressBG;
//    [SerializeField] private Image upgradeProgressBar;

//    // ── Action Buttons ────────────────────────────────────────────────────────
//    [Header("Action Buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button equipButton;
//    [SerializeField] private Button unequipButton;
//    [SerializeField] private Button upgradeButton;
//    [SerializeField] private TextMeshProUGUI upgradeButtonText;
//    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
//             + "Visible only in Inventory mode — switches back to Buy mode.")]
//    [SerializeField] private Button inventoryBuyButton;

//    // ── Coin & Warning ────────────────────────────────────────────────────────
//    [Header("Coin & Warning")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private TextMeshProUGUI warningText;

//    [Header("Starting Gold")]
//    [SerializeField] private int startingGold = 840;

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE STATE
//    // ══════════════════════════════════════════════════════════════════════════

//    private int _gold;

//    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//    private int _nextId = 0;
//    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

//    private enum Mode { Buy, Inventory }
//    private Mode _mode = Mode.Buy;

//    private CannonSlot _callingSlot;
//    private CannonSlotCastle _callingCastleSlot;

//    // Buy mode
//    private CannonCard _selectedBuyCard;
//    private CannonData _selectedBuyData;

//    // Inventory mode — dynamically spawned cards
//    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
//    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
//    private CannonCard _selectedInventoryCard;

//    private CannonInventoryEntry SelectedEntry
//    {
//        get
//        {
//            if (_selectedInventoryCard == null) return null;
//            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
//            return e;
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        _gold = startingGold;

//        WireButtons();
//        SetupBuyCards();
//        RefreshCoinText();
//        HideTimer();
//        HideProgressBar();

//        ShowBuyMode();   // start in Buy mode
//    }

//    private void Update()
//    {
//        if (_mode == Mode.Inventory)
//            TickUpgrades();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INIT
//    // ══════════════════════════════════════════════════════════════════════════

//    private void WireButtons()
//    {
//        // Tab buttons — always visible, switch mode when clicked
//        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
//        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
//        backButton?.onClick.AddListener(OnBackClicked);

//        // Action buttons
//        buyButton?.onClick.AddListener(OnBuyClicked);
//        equipButton?.onClick.AddListener(OnEquipClicked);
//        unequipButton?.onClick.AddListener(OnUnequipClicked);
//        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
//        // Inventory-mode Buy button — takes player back to Buy mode
//        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
//    }

//    private void SetupBuyCards()
//    {
//        if (cards == null || cannonTypes == null) return;
//        for (int i = 0; i < cards.Length; i++)
//        {
//            if (cards[i] == null) continue;
//            if (i < cannonTypes.Length && cannonTypes[i] != null)
//                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
//            else
//                cards[i].gameObject.SetActive(false);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC ENTRY POINTS
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
//    public void OnPanelOpened(CannonSlot callingSlot)
//    {
//        _callingSlot = callingSlot;
//        _callingCastleSlot = null;
//        gameObject.SetActive(true);
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    /// <summary>
//    /// Called when the player clicks a CannonZone on the castle grid.
//    /// Stores the castle slot so OnEquipClicked can equip directly into it.
//    /// </summary>
//    public void OpenFromCastleSlot(CannonSlotCastle castleSlot)
//    {
//        _callingSlot = null;
//        _callingCastleSlot = castleSlot;
//        gameObject.SetActive(true);
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    //    public void OnPanelOpened()
//    //    {
//    //        RefreshCoinText();
//    //        SwitchToBuyMode();
//    //    }

//    /// <summary>
//    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
//    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
//    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
//    /// </summary>
//    public void OpenAtInventory(CannonSlot callingSlot)
//    {
//        _callingSlot = callingSlot;
//        gameObject.SetActive(true);
//        RefreshCoinText();

//        if (_inventory.Count == 0)
//        {
//            SwitchToBuyMode();
//            return;
//        }

//        _mode = Mode.Inventory;
//        ShowInventoryMode();

//        // Pre-select the card that belongs to this slot's cannon
//        if (callingSlot?.Entry != null)
//        {
//            int targetId = callingSlot.Entry.inventoryId;
//            foreach (var c in _spawnedCards)
//                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
//                { SelectInventoryCard(c, e); break; }
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // MODE SWITCHING
//    // ══════════════════════════════════════════════════════════════════════════

//    // Called by BuyTabButton — always visible, works from any mode
//    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

//    private void SwitchToInventoryMode()
//    {
//        // FIX: warn and block if the player hasn't bought anything yet
//        if (_inventory.Count == 0)
//        {
//            ShowWarning("Buy a cannon first!");
//            return;
//        }
//        _mode = Mode.Inventory;
//        ShowInventoryMode();
//    }

//    // ── Buy Mode ──────────────────────────────────────────────────────────────

//    private void ShowBuyMode()
//    {
//        // Show card grid; hide inventory container
//        if (cardGridRoot != null) cardGridRoot.SetActive(true);
//        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

//        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
//        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
//        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

//        // Inventory-mode Buy button is only relevant when in Inventory
//        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

//        // Buy button visible; inventory action buttons hidden
//        SetActionButtons(buyVisible: true, inventoryVisible: false);

//        HideTimer();
//        HideProgressBar();
//        ClearWarning();

//        // Refresh buy card locks (may have changed since last visit)
//        if (cards != null && cannonTypes != null)
//        {
//            for (int i = 0; i < cards.Length; i++)
//            {
//                if (cards[i] == null) continue;
//                if (i < cannonTypes.Length && cannonTypes[i] != null)
//                {
//                    cards[i].gameObject.SetActive(true);
//                    cards[i].SetupBuyCard(cannonTypes[i],
//                        locked: !_everBought.Contains(cannonTypes[i]));
//                }
//                else
//                {
//                    cards[i].gameObject.SetActive(false);
//                }
//            }
//        }

//        // Restore previous selection or auto-select first card
//        if (_selectedBuyCard != null && cards != null
//            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
//        {
//            _selectedBuyCard.SetSelected(true);
//            ShowBuyDetails(_selectedBuyData);
//            RefreshBuyButton();
//        }
//        else if (cards != null && cards.Length > 0 && cards[0] != null)
//        {
//            SelectBuyCard(cards[0], 0);
//        }
//        else
//        {
//            ClearDetails();
//        }
//    }

//    // ── Inventory Mode ─────────────────────────────────────────────────────────

//    private void ShowInventoryMode()
//    {
//        // Hide card grid; show inventory container
//        if (cardGridRoot != null) cardGridRoot.SetActive(false);
//        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

//        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
//        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
//        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

//        // Show the in-inventory Buy button so the player can jump to the Buy panel
//        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

//        // Inventory action buttons visible; buy button hidden
//        SetActionButtons(buyVisible: false, inventoryVisible: true);

//        ClearWarning();
//        HideTimer();
//        HideProgressBar();

//        // Destroy previously spawned inventory cards
//        foreach (CannonCard c in _spawnedCards)
//            if (c != null) Destroy(c.gameObject);
//        _spawnedCards.Clear();
//        _cardEntryMap.Clear();
//        _selectedInventoryCard = null;

//        if (cannonCardPrefab == null || inventoryGridContent == null)
//        {
//            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//            return;
//        }

//        // Spawn one card per owned cannon entry
//        // Card label always shows the plain cannon name — no "(1/3)" clutter.
//        // The copy number is shown only in the Details panel via GetDetailDisplayName().
//        foreach (var entry in _inventory)
//        {
//            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
//            string displayName = entry.data.cannonName;   // plain name on the card
//            card.SetupInventoryCard(entry, displayName);
//            _spawnedCards.Add(card);
//            _cardEntryMap[card] = entry;
//        }

//        // Auto-select: restore previous selection, else pick first card
//        if (_spawnedCards.Count > 0)
//        {
//            CannonCard toSelect = null;
//            int prevId = SelectedEntry?.inventoryId ?? -1;

//            if (prevId >= 0)
//            {
//                foreach (CannonCard c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
//                    { toSelect = c; break; }
//            }

//            if (toSelect == null) toSelect = _spawnedCards[0];
//            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
//        }
//        else
//        {
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnCardSelected(CannonCard card)
//    {
//        if (_mode == Mode.Buy)
//        {
//            if (cards == null) return;
//            int idx = System.Array.IndexOf(cards, card);
//            if (idx >= 0) SelectBuyCard(card, idx);
//        }
//        else
//        {
//            if (_cardEntryMap.TryGetValue(card, out var entry))
//                SelectInventoryCard(card, entry);
//        }
//        ClearWarning();
//    }

//    private void SelectBuyCard(CannonCard card, int idx)
//    {
//        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
//        _selectedBuyCard = card;
//        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
//        card.SetSelected(true);

//        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
//        else ClearDetails();
//    }

//    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
//    {
//        foreach (var c in _spawnedCards) c?.SetSelected(false);
//        _selectedInventoryCard = card;
//        card?.SetSelected(true);

//        ShowInventoryDetails(entry);
//        RefreshInventoryButtons(entry);
//        RefreshTimerAndProgress(entry);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BUY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBuyClicked()
//    {
//        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
//        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

//        _gold -= _selectedBuyData.cost;
//        RefreshCoinText();

//        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
//        _inventory.Add(entry);

//        // Unlock the buy card on first purchase of this type
//        if (!_everBought.Contains(_selectedBuyData))
//        {
//            _everBought.Add(_selectedBuyData);
//            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
//                if (cannonTypes[i] == _selectedBuyData)
//                    cards[i]?.SetLocked(false);
//        }

//        RefreshBuyButton();
//        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
//        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
//    }

//    private void RefreshBuyButton()
//    {
//        if (buyButton == null) return;
//        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
//        buyButton.interactable = canAfford;
//        if (buyButtonText != null)
//            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // EQUIP / UNEQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnEquipClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

//        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
//            ? _callingSlot
//            : FindFreeSlot();

//        if (target == null) { ShowWarning("No free cannon slot!"); return; }

//        target.Equip(entry);
//        RefreshInventoryButtons(entry);
//        ShowWarning($"Equipped {entry.data.cannonName}!");
//    }

//    private void OnUnequipClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
//        entry.equippedSlot?.Unequip();
//    }

//    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
//    public void RefreshAfterUnequip()
//    {
//        if (_mode == Mode.Inventory) ShowInventoryMode();
//    }

//    private CannonSlot FindFreeSlot()
//    {
//        // First try the Inspector-assigned array (scene slots).
//        if (castleSlots != null)
//            foreach (var s in castleSlots)
//                if (s != null && !s.IsOccupied) return s;

//        // Fallback: CannonSlot objects are spawned as prefabs at runtime and
//        // cannot be pre-assigned in the Inspector. Find them dynamically.
//        foreach (var s in FindObjectsOfType<CannonSlot>())
//            if (!s.IsOccupied) return s;

//        return null;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UPGRADE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnUpgradeClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
//        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

//        entry.isUpgrading = true;
//        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//        // Activate the badge on the card right away so the player sees the upgrade is running
//        _selectedInventoryCard?.RefreshBadge(entry);

//        // Immediately update UI to show timer, progress bar, and "40+7" stat format
//        ShowInventoryDetails(entry);
//        RefreshInventoryButtons(entry);
//        RefreshTimerAndProgress(entry);
//        ShowWarning("Upgrading…");
//    }

//    private void TickUpgrades()
//    {
//        bool anyCompleted = false;

//        foreach (var entry in _inventory)
//        {
//            if (!entry.isUpgrading) continue;

//            float remaining = entry.UpgradeTimeRemaining;

//            // Update timer + progress bar for the currently selected entry
//            if (entry == SelectedEntry)
//            {
//                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//                float progress = 1f - Mathf.Clamp01(remaining / total);

//                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//                if (timerText != null) timerText.text = FormatTimer(remaining);
//            }

//            if (remaining <= 0f)
//            {
//                entry.upgradeCount++;
//                entry.isUpgrading = false;
//                anyCompleted = true;

//                // Refresh the badge on the card in-place
//                foreach (var c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
//                        c.RefreshBadge(entry);

//                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
//                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
//            }
//        }

//        if (anyCompleted)
//        {
//            // Rebuild card list so everything reflects the new level
//            CannonInventoryEntry prevSel = SelectedEntry;
//            ShowInventoryMode();

//            if (prevSel != null)
//            {
//                foreach (var c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
//                    { SelectInventoryCard(c, e); break; }
//            }
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DETAILS PANEL — BUY MODE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowBuyDetails(CannonData data)
//    {
//        if (data == null) { ClearDetails(); return; }

//        ApplyPreview(data.previewSprite
//            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
//        ;
//        if (costText != null) costText.text = $"Cost: {data.cost}";
//        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

//        // Plain values — no delta, no timer
//        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
//        HideTimer();
//        HideProgressBar();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DETAILS PANEL — INVENTORY MODE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowInventoryDetails(CannonInventoryEntry entry)
//    {
//        if (entry == null) { ClearDetails(); return; }

//        ApplyPreview(entry.data.previewSprite
//            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

//        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
//        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
//        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
//        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

//        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
//        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
//                   upgrading: showDelta, entry: entry);
//    }

//    // ── Details helpers ────────────────────────────────────────────────────────

//    private void ClearDetails()
//    {
//        ApplyPreview(null);
//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (nameText != null) nameText.text = "—";
//        if (costText != null) costText.text = "";
//        if (rangeStatText != null) rangeStatText.text = "";
//        SetHUDBars(0f, 0f, 0f, upgrading: false);
//        HideTimer();
//    }

//    private void ApplyPreview(Sprite s)
//    {
//        if (previewImage == null) return;
//        previewImage.enabled = s != null;
//        if (s != null) previewImage.sprite = s;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HUD BARS — Health / Range / Damage
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Sets the three stat bars.
//    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
//    /// (current value + the gain from the next upgrade level).
//    /// </summary>
//    private void SetHUDBars(float h, float r, float d,
//                            bool upgrading,
//                            CannonInventoryEntry entry = null)
//    {
//        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
//        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
//        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

//        if (upgrading && entry != null && !entry.IsMaxLevel)
//        {
//            // Peek at next-level stats without permanently mutating the entry
//            entry.upgradeCount++;
//            float nh = entry.CurrentHealth;
//            float nr = entry.CurrentRange;
//            float nd = entry.CurrentDamage;
//            entry.upgradeCount--;

//            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
//            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
//            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
//            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
//        }
//        else
//        {
//            if (healthValueText != null) healthValueText.text = $"{h:F0}";
//            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
//            if (damageValueText != null) damageValueText.text = $"{d:F0}";
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // TIMER & PROGRESS BAR
//    // Timer is hidden by default; activated only during an upgrade.
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
//    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
//    {
//        if (entry != null && entry.isUpgrading)
//        {
//            ShowTimer(entry.UpgradeTimeRemaining);
//            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
//            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//        }
//        else
//        {
//            HideTimer();
//            HideProgressBar();
//        }
//    }

//    // Timer — show only while upgrading
//    private void ShowTimer(float seconds)
//    {
//        if (timerText == null) return;
//        timerText.gameObject.SetActive(true);
//        timerText.text = FormatTimer(seconds);
//    }

//    private void HideTimer()
//    {
//        if (timerText != null) timerText.gameObject.SetActive(false);
//    }

//    // Progress bar — show only while upgrading
//    private void ShowProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//    }

//    private void HideProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INVENTORY BUTTON STATES
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RefreshInventoryButtons(CannonInventoryEntry entry)
//    {
//        if (entry == null) { SetInventoryButtonsEmpty(); return; }

//        bool equipped = entry.isEquipped;
//        bool maxLevel = entry.IsMaxLevel;
//        bool upgrading = entry.isUpgrading;

//        // Equip / Unequip are mutually exclusive
//        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
//        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

//        if (upgradeButton != null)
//        {
//            upgradeButton.interactable = !maxLevel && !upgrading;
//            if (upgradeButtonText != null)
//            {
//                if (maxLevel) upgradeButtonText.text = "MAX";
//                else if (upgrading) upgradeButtonText.text = "";
//                else upgradeButtonText.text =
//                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//            }
//        }
//    }

//    private void SetInventoryButtonsEmpty()
//    {
//        if (equipButton != null) equipButton.interactable = false;
//        if (unequipButton != null) unequipButton.interactable = false;
//        if (upgradeButton != null)
//        {
//            upgradeButton.interactable = false;
//            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
//        }
//    }

//    /// <summary>
//    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
//    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
//    /// they are always visible so the player can switch modes freely.
//    /// </summary>
//    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
//    {
//        buyButton?.gameObject.SetActive(buyVisible);
//        equipButton?.gameObject.SetActive(inventoryVisible);
//        unequipButton?.gameObject.SetActive(inventoryVisible);
//        upgradeButton?.gameObject.SetActive(inventoryVisible);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BACK / CLOSE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBackClicked()
//    {
//        ClearWarning();

//        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
//        if (_mode == Mode.Inventory)
//        {
//            SwitchToBuyMode();
//            return;
//        }

//        // In Buy mode — close the panel as usual
//        if (GameManager.Instance != null)
//            GameManager.Instance.CloseCurrentPanel();
//        else
//            gameObject.SetActive(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // COIN
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RefreshCoinText()
//    {
//        if (coinText != null) coinText.text = _gold.ToString();
//    }

//    public void AddGold(int amount)
//    {
//        _gold += amount;
//        RefreshCoinText();
//        if (_mode == Mode.Buy) RefreshBuyButton();
//    }

//    public int GetGold() => _gold;

//    // ══════════════════════════════════════════════════════════════════════════
//    // WARNING / FEEDBACK
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowWarning(string msg)
//    {
//        if (warningText == null) return;
//        warningText.text = msg;
//        CancelInvoke(nameof(ClearWarning));
//        Invoke(nameof(ClearWarning), 2.5f);
//    }

//    private void ClearWarning()
//    {
//        if (warningText != null) warningText.text = "";
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UTILITIES
//    // ══════════════════════════════════════════════════════════════════════════

//    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
//        => totalCopies > 1
//            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
//            : entry.data.cannonName;

//    private string GetDetailDisplayName(CannonInventoryEntry entry)
//    {
//        int total = 0, myIdx = 0;
//        foreach (var e in _inventory)
//        {
//            if (e.data == entry.data) total++;
//            if (e == entry && myIdx == 0) myIdx = total;
//        }
//        return total > 1
//            ? $"{entry.data.cannonName} ({myIdx}/{total})"
//            : entry.data.cannonName;
//    }

//    private static string FormatTimer(float seconds)
//    {
//        float s = Mathf.Max(0f, seconds);
//        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//    }

//    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

//    public int CountOwned(CannonData data)
//    {
//        int n = 0;
//        foreach (var e in _inventory) if (e.data == data) n++;
//        return n;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // EDITOR VALIDATION
//    // ══════════════════════════════════════════════════════════════════════════

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (cannonTypes == null || cannonTypes.Length == 0)
//            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
//        if (cards == null || cards.Length == 0)
//            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
//        if (cannonCardPrefab == null)
//            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
//        if (inventoryGridContent == null)
//            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
//        if (buyButton == null)
//            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
//    }
//#endif
//}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonPanelManager
///////
/////// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
///////
///////  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
///////     Inventory cards spawn into a plain Transform (inventoryGridContent).
///////     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
///////
///////  2. RANGE BAR replaces Ability bar.
///////     Inspector fields: rangeBar / rangeValueText.
///////     Max value: maxRange.
///////
///////  3. TIMER hidden by default.
///////     timerText.gameObject is deactivated when not upgrading.
///////     Shown (and counts down) only while an upgrade is running.
///////
///////  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
///////     Both the value text and a separate delta label can show this.
///////
///////  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
///////     buyTabButton and inventoryTabButton are never hidden by this manager.
///////
/////// ════ HIERARCHY ══════════════════════════════════════════════════════════════
///////
///////  CannonPanel
///////  ├── BackButton
///////  ├── BuyTabButton          ← always visible; switches to Buy mode
///////  ├── InventoryTabButton    ← always visible; switches to Inventory mode
///////  ├── CardGrid              ← 3 pre-placed buy CannonCards
///////  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
///////  ├── Details Panel
///////  │   ├── PreviewImage
///////  │   ├── LevelText         "LEVEL 1"
///////  │   ├── TimerText         hidden unless upgrading → shows "01:30"
///////  │   ├── NameText
///////  │   ├── CostText          "Cost: 100"
///////  │   ├── RangeStatText     "Range: 40m"
///////  │   ├── HealthBar (Filled Image) + HealthValueText
///////  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
///////  │   ├── DamageBar (Filled Image) + DamageValueText
///////  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
///////  │   │   └── UpgradeProgressBar (Filled Image)
///////  │   ├── BuyButton + BuyButtonText
///////  │   ├── EquipButton
///////  │   ├── UnequipButton
///////  │   └── UpgradeButton + UpgradeButtonText
///////  └── CoinText
///////
/////// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
///////  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
///////  Castle Slots          all CannonSlot prefab instances in the scene
///////  Starting Gold         default 840
/////// </summary>
////public class CannonPanelManager : MonoBehaviour
////{
////    public static CannonPanelManager Instance { get; private set; }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INSPECTOR FIELDS
////    // ══════════════════════════════════════════════════════════════════════════

////    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
////    [SerializeField] private CannonData[] cannonTypes;

////    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
////    [SerializeField] private CannonSlot[] castleSlots;

////    // ── Tab & Back Buttons ────────────────────────────────────────────────────
////    [Header("Tab Buttons  (always visible in both modes)")]
////    [SerializeField] private Button buyTabButton;
////    [SerializeField] private Button inventoryTabButton;
////    [SerializeField] private Button backButton;

////    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
////    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
////    [SerializeField] private GameObject cardGridRoot;
////    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

////    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
////    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
////    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
////    [SerializeField] private Transform inventoryGridContent;
////    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
////    [SerializeField] private CannonCard cannonCardPrefab;

////    // ── Details Panel ─────────────────────────────────────────────────────────
////    [Header("Details Panel")]
////    [SerializeField] private Image previewImage;
////    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
////    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
////    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
////    [SerializeField] private TextMeshProUGUI nameText;
////    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
////    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

////    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
////    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
////    [SerializeField] private Image healthBar;
////    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
////    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
////    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
////    [SerializeField] private Image damageBar;
////    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

////    [Header("Max values used to compute bar fill  (tune per game balance)")]
////    [SerializeField] private float maxHealth = 200f;
////    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
////    [SerializeField] private float maxDamage = 100f;

////    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
////    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
////    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
////    [SerializeField] private GameObject upgradeProgressBG;
////    [SerializeField] private Image upgradeProgressBar;

////    // ── Action Buttons ────────────────────────────────────────────────────────
////    [Header("Action Buttons")]
////    [SerializeField] private Button buyButton;
////    [SerializeField] private TextMeshProUGUI buyButtonText;
////    [SerializeField] private Button equipButton;
////    [SerializeField] private Button unequipButton;
////    [SerializeField] private Button upgradeButton;
////    [SerializeField] private TextMeshProUGUI upgradeButtonText;
////    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
////             + "Visible only in Inventory mode — switches back to Buy mode.")]
////    [SerializeField] private Button inventoryBuyButton;

////    // ── Coin & Warning ────────────────────────────────────────────────────────
////    [Header("Coin & Warning")]
////    [SerializeField] private TextMeshProUGUI coinText;
////    [SerializeField] private TextMeshProUGUI warningText;

////    [Header("Starting Gold")]
////    [SerializeField] private int startingGold = 840;

////    // ══════════════════════════════════════════════════════════════════════════
////    // PRIVATE STATE
////    // ══════════════════════════════════════════════════════════════════════════

////    private int _gold;

////    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
////    private int _nextId = 0;
////    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

////    private enum Mode { Buy, Inventory }
////    private Mode _mode = Mode.Buy;

////    private CannonSlot _callingSlot;

////    // Buy mode
////    private CannonCard _selectedBuyCard;
////    private CannonData _selectedBuyData;

////    // Inventory mode — dynamically spawned cards
////    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
////    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
////    private CannonCard _selectedInventoryCard;

////    private CannonInventoryEntry SelectedEntry
////    {
////        get
////        {
////            if (_selectedInventoryCard == null) return null;
////            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
////            return e;
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        _gold = startingGold;

////        WireButtons();
////        SetupBuyCards();
////        RefreshCoinText();
////        HideTimer();
////        HideProgressBar();

////        ShowBuyMode();   // start in Buy mode
////    }

////    private void Update()
////    {
////        if (_mode == Mode.Inventory)
////            TickUpgrades();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INIT
////    // ══════════════════════════════════════════════════════════════════════════

////    private void WireButtons()
////    {
////        // Tab buttons — always visible, switch mode when clicked
////        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
////        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
////        backButton?.onClick.AddListener(OnBackClicked);

////        // Action buttons
////        buyButton?.onClick.AddListener(OnBuyClicked);
////        equipButton?.onClick.AddListener(OnEquipClicked);
////        unequipButton?.onClick.AddListener(OnUnequipClicked);
////        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
////        // Inventory-mode Buy button — takes player back to Buy mode
////        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
////    }

////    private void SetupBuyCards()
////    {
////        if (cards == null || cannonTypes == null) return;
////        for (int i = 0; i < cards.Length; i++)
////        {
////            if (cards[i] == null) continue;
////            if (i < cannonTypes.Length && cannonTypes[i] != null)
////                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
////            else
////                cards[i].gameObject.SetActive(false);
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PUBLIC ENTRY POINTS
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
////    public void OnPanelOpened(CannonSlot callingSlot)
////    {
////        _callingSlot = callingSlot;
////        gameObject.SetActive(true);
////        RefreshCoinText();
////        SwitchToBuyMode();
////    }

////    //    public void OnPanelOpened()
////    //    {
////    //        RefreshCoinText();
////    //        SwitchToBuyMode();
////    //    }

////    /// <summary>
////    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
////    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
////    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
////    /// </summary>
////    public void OpenAtInventory(CannonSlot callingSlot)
////    {
////        _callingSlot = callingSlot;
////        gameObject.SetActive(true);
////        RefreshCoinText();

////        if (_inventory.Count == 0)
////        {
////            SwitchToBuyMode();
////            return;
////        }

////        _mode = Mode.Inventory;
////        ShowInventoryMode();

////        // Pre-select the card that belongs to this slot's cannon
////        if (callingSlot?.Entry != null)
////        {
////            int targetId = callingSlot.Entry.inventoryId;
////            foreach (var c in _spawnedCards)
////                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
////                { SelectInventoryCard(c, e); break; }
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // MODE SWITCHING
////    // ══════════════════════════════════════════════════════════════════════════

////    // Called by BuyTabButton — always visible, works from any mode
////    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

////    private void SwitchToInventoryMode()
////    {
////        // FIX: warn and block if the player hasn't bought anything yet
////        if (_inventory.Count == 0)
////        {
////            ShowWarning("Buy a cannon first!");
////            return;
////        }
////        _mode = Mode.Inventory;
////        ShowInventoryMode();
////    }

////    // ── Buy Mode ──────────────────────────────────────────────────────────────

////    private void ShowBuyMode()
////    {
////        // Show card grid; hide inventory container
////        if (cardGridRoot != null) cardGridRoot.SetActive(true);
////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

////        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

////        // Inventory-mode Buy button is only relevant when in Inventory
////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

////        // Buy button visible; inventory action buttons hidden
////        SetActionButtons(buyVisible: true, inventoryVisible: false);

////        HideTimer();
////        HideProgressBar();
////        ClearWarning();

////        // Refresh buy card locks (may have changed since last visit)
////        if (cards != null && cannonTypes != null)
////        {
////            for (int i = 0; i < cards.Length; i++)
////            {
////                if (cards[i] == null) continue;
////                if (i < cannonTypes.Length && cannonTypes[i] != null)
////                {
////                    cards[i].gameObject.SetActive(true);
////                    cards[i].SetupBuyCard(cannonTypes[i],
////                        locked: !_everBought.Contains(cannonTypes[i]));
////                }
////                else
////                {
////                    cards[i].gameObject.SetActive(false);
////                }
////            }
////        }

////        // Restore previous selection or auto-select first card
////        if (_selectedBuyCard != null && cards != null
////            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
////        {
////            _selectedBuyCard.SetSelected(true);
////            ShowBuyDetails(_selectedBuyData);
////            RefreshBuyButton();
////        }
////        else if (cards != null && cards.Length > 0 && cards[0] != null)
////        {
////            SelectBuyCard(cards[0], 0);
////        }
////        else
////        {
////            ClearDetails();
////        }
////    }

////    // ── Inventory Mode ─────────────────────────────────────────────────────────

////    private void ShowInventoryMode()
////    {
////        // Hide card grid; show inventory container
////        if (cardGridRoot != null) cardGridRoot.SetActive(false);
////        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

////        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
////        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
////        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

////        // Show the in-inventory Buy button so the player can jump to the Buy panel
////        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

////        // Inventory action buttons visible; buy button hidden
////        SetActionButtons(buyVisible: false, inventoryVisible: true);

////        ClearWarning();
////        HideTimer();
////        HideProgressBar();

////        // Destroy previously spawned inventory cards
////        foreach (CannonCard c in _spawnedCards)
////            if (c != null) Destroy(c.gameObject);
////        _spawnedCards.Clear();
////        _cardEntryMap.Clear();
////        _selectedInventoryCard = null;

////        if (cannonCardPrefab == null || inventoryGridContent == null)
////        {
////            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////            return;
////        }

////        // Spawn one card per owned cannon entry
////        // Card label always shows the plain cannon name — no "(1/3)" clutter.
////        // The copy number is shown only in the Details panel via GetDetailDisplayName().
////        foreach (var entry in _inventory)
////        {
////            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
////            string displayName = entry.data.cannonName;   // plain name on the card
////            card.SetupInventoryCard(entry, displayName);
////            _spawnedCards.Add(card);
////            _cardEntryMap[card] = entry;
////        }

////        // Auto-select: restore previous selection, else pick first card
////        if (_spawnedCards.Count > 0)
////        {
////            CannonCard toSelect = null;
////            int prevId = SelectedEntry?.inventoryId ?? -1;

////            if (prevId >= 0)
////            {
////                foreach (CannonCard c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
////                    { toSelect = c; break; }
////            }

////            if (toSelect == null) toSelect = _spawnedCards[0];
////            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
////        }
////        else
////        {
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnCardSelected(CannonCard card)
////    {
////        if (_mode == Mode.Buy)
////        {
////            if (cards == null) return;
////            int idx = System.Array.IndexOf(cards, card);
////            if (idx >= 0) SelectBuyCard(card, idx);
////        }
////        else
////        {
////            if (_cardEntryMap.TryGetValue(card, out var entry))
////                SelectInventoryCard(card, entry);
////        }
////        ClearWarning();
////    }

////    private void SelectBuyCard(CannonCard card, int idx)
////    {
////        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
////        _selectedBuyCard = card;
////        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
////        card.SetSelected(true);

////        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
////        else ClearDetails();
////    }

////    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
////    {
////        foreach (var c in _spawnedCards) c?.SetSelected(false);
////        _selectedInventoryCard = card;
////        card?.SetSelected(true);

////        ShowInventoryDetails(entry);
////        RefreshInventoryButtons(entry);
////        RefreshTimerAndProgress(entry);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BUY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBuyClicked()
////    {
////        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
////        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

////        _gold -= _selectedBuyData.cost;
////        RefreshCoinText();

////        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
////        _inventory.Add(entry);

////        // Unlock the buy card on first purchase of this type
////        if (!_everBought.Contains(_selectedBuyData))
////        {
////            _everBought.Add(_selectedBuyData);
////            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
////                if (cannonTypes[i] == _selectedBuyData)
////                    cards[i]?.SetLocked(false);
////        }

////        RefreshBuyButton();
////        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
////        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
////    }

////    private void RefreshBuyButton()
////    {
////        if (buyButton == null) return;
////        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
////        buyButton.interactable = canAfford;
////        if (buyButtonText != null)
////            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // EQUIP / UNEQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnEquipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

////        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
////            ? _callingSlot
////            : FindFreeSlot();

////        if (target == null) { ShowWarning("No free cannon slot!"); return; }

////        target.Equip(entry);
////        RefreshInventoryButtons(entry);
////        ShowWarning($"Equipped {entry.data.cannonName}!");
////    }

////    private void OnUnequipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
////        entry.equippedSlot?.Unequip();
////    }

////    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
////    public void RefreshAfterUnequip()
////    {
////        if (_mode == Mode.Inventory) ShowInventoryMode();
////    }

////    private CannonSlot FindFreeSlot()
////    {
////        if (castleSlots == null) return null;
////        foreach (var s in castleSlots)
////            if (s != null && !s.IsOccupied) return s;
////        return null;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UPGRADE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnUpgradeClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
////        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

////        entry.isUpgrading = true;
////        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

////        // Activate the badge on the card right away so the player sees the upgrade is running
////        _selectedInventoryCard?.RefreshBadge(entry);

////        // Immediately update UI to show timer, progress bar, and "40+7" stat format
////        ShowInventoryDetails(entry);
////        RefreshInventoryButtons(entry);
////        RefreshTimerAndProgress(entry);
////        ShowWarning("Upgrading…");
////    }

////    private void TickUpgrades()
////    {
////        bool anyCompleted = false;

////        foreach (var entry in _inventory)
////        {
////            if (!entry.isUpgrading) continue;

////            float remaining = entry.UpgradeTimeRemaining;

////            // Update timer + progress bar for the currently selected entry
////            if (entry == SelectedEntry)
////            {
////                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
////                float progress = 1f - Mathf.Clamp01(remaining / total);

////                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
////                if (timerText != null) timerText.text = FormatTimer(remaining);
////            }

////            if (remaining <= 0f)
////            {
////                entry.upgradeCount++;
////                entry.isUpgrading = false;
////                anyCompleted = true;

////                // Refresh the badge on the card in-place
////                foreach (var c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
////                        c.RefreshBadge(entry);

////                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
////                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
////            }
////        }

////        if (anyCompleted)
////        {
////            // Rebuild card list so everything reflects the new level
////            CannonInventoryEntry prevSel = SelectedEntry;
////            ShowInventoryMode();

////            if (prevSel != null)
////            {
////                foreach (var c in _spawnedCards)
////                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
////                    { SelectInventoryCard(c, e); break; }
////            }
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DETAILS PANEL — BUY MODE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowBuyDetails(CannonData data)
////    {
////        if (data == null) { ClearDetails(); return; }

////        ApplyPreview(data.previewSprite
////            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
////        ;
////        if (costText != null) costText.text = $"Cost: {data.cost}";
////        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

////        // Plain values — no delta, no timer
////        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
////        HideTimer();
////        HideProgressBar();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DETAILS PANEL — INVENTORY MODE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowInventoryDetails(CannonInventoryEntry entry)
////    {
////        if (entry == null) { ClearDetails(); return; }

////        ApplyPreview(entry.data.previewSprite
////            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
////        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
////        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
////        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

////        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
////        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
////                   upgrading: showDelta, entry: entry);
////    }

////    // ── Details helpers ────────────────────────────────────────────────────────

////    private void ClearDetails()
////    {
////        ApplyPreview(null);
////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (nameText != null) nameText.text = "—";
////        if (costText != null) costText.text = "";
////        if (rangeStatText != null) rangeStatText.text = "";
////        SetHUDBars(0f, 0f, 0f, upgrading: false);
////        HideTimer();
////    }

////    private void ApplyPreview(Sprite s)
////    {
////        if (previewImage == null) return;
////        previewImage.enabled = s != null;
////        if (s != null) previewImage.sprite = s;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HUD BARS — Health / Range / Damage
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Sets the three stat bars.
////    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
////    /// (current value + the gain from the next upgrade level).
////    /// </summary>
////    private void SetHUDBars(float h, float r, float d,
////                            bool upgrading,
////                            CannonInventoryEntry entry = null)
////    {
////        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
////        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
////        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

////        if (upgrading && entry != null && !entry.IsMaxLevel)
////        {
////            // Peek at next-level stats without permanently mutating the entry
////            entry.upgradeCount++;
////            float nh = entry.CurrentHealth;
////            float nr = entry.CurrentRange;
////            float nd = entry.CurrentDamage;
////            entry.upgradeCount--;

////            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
////            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
////            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
////        }
////        else
////        {
////            if (healthValueText != null) healthValueText.text = $"{h:F0}";
////            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
////            if (damageValueText != null) damageValueText.text = $"{d:F0}";
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // TIMER & PROGRESS BAR
////    // Timer is hidden by default; activated only during an upgrade.
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
////    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
////    {
////        if (entry != null && entry.isUpgrading)
////        {
////            ShowTimer(entry.UpgradeTimeRemaining);
////            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
////            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
////            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
////            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
////        }
////        else
////        {
////            HideTimer();
////            HideProgressBar();
////        }
////    }

////    // Timer — show only while upgrading
////    private void ShowTimer(float seconds)
////    {
////        if (timerText == null) return;
////        timerText.gameObject.SetActive(true);
////        timerText.text = FormatTimer(seconds);
////    }

////    private void HideTimer()
////    {
////        if (timerText != null) timerText.gameObject.SetActive(false);
////    }

////    // Progress bar — show only while upgrading
////    private void ShowProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
////    }

////    private void HideProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INVENTORY BUTTON STATES
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RefreshInventoryButtons(CannonInventoryEntry entry)
////    {
////        if (entry == null) { SetInventoryButtonsEmpty(); return; }

////        bool equipped = entry.isEquipped;
////        bool maxLevel = entry.IsMaxLevel;
////        bool upgrading = entry.isUpgrading;

////        // Equip / Unequip are mutually exclusive
////        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
////        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

////        if (upgradeButton != null)
////        {
////            upgradeButton.interactable = !maxLevel && !upgrading;
////            if (upgradeButtonText != null)
////            {
////                if (maxLevel) upgradeButtonText.text = "MAX";
////                else if (upgrading) upgradeButtonText.text = "";
////                else upgradeButtonText.text =
////                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
////            }
////        }
////    }

////    private void SetInventoryButtonsEmpty()
////    {
////        if (equipButton != null) equipButton.interactable = false;
////        if (unequipButton != null) unequipButton.interactable = false;
////        if (upgradeButton != null)
////        {
////            upgradeButton.interactable = false;
////            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
////        }
////    }

////    /// <summary>
////    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
////    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
////    /// they are always visible so the player can switch modes freely.
////    /// </summary>
////    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
////    {
////        buyButton?.gameObject.SetActive(buyVisible);
////        equipButton?.gameObject.SetActive(inventoryVisible);
////        unequipButton?.gameObject.SetActive(inventoryVisible);
////        upgradeButton?.gameObject.SetActive(inventoryVisible);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BACK / CLOSE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBackClicked()
////    {
////        ClearWarning();

////        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
////        if (_mode == Mode.Inventory)
////        {
////            SwitchToBuyMode();
////            return;
////        }

////        // In Buy mode — close the panel as usual
////        if (GameManager.Instance != null)
////            GameManager.Instance.CloseCurrentPanel();
////        else
////            gameObject.SetActive(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // COIN
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RefreshCoinText()
////    {
////        if (coinText != null) coinText.text = _gold.ToString();
////    }

////    public void AddGold(int amount)
////    {
////        _gold += amount;
////        RefreshCoinText();
////        if (_mode == Mode.Buy) RefreshBuyButton();
////    }

////    public int GetGold() => _gold;

////    // ══════════════════════════════════════════════════════════════════════════
////    // WARNING / FEEDBACK
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowWarning(string msg)
////    {
////        if (warningText == null) return;
////        warningText.text = msg;
////        CancelInvoke(nameof(ClearWarning));
////        Invoke(nameof(ClearWarning), 2.5f);
////    }

////    private void ClearWarning()
////    {
////        if (warningText != null) warningText.text = "";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // UTILITIES
////    // ══════════════════════════════════════════════════════════════════════════

////    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
////        => totalCopies > 1
////            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
////            : entry.data.cannonName;

////    private string GetDetailDisplayName(CannonInventoryEntry entry)
////    {
////        int total = 0, myIdx = 0;
////        foreach (var e in _inventory)
////        {
////            if (e.data == entry.data) total++;
////            if (e == entry && myIdx == 0) myIdx = total;
////        }
////        return total > 1
////            ? $"{entry.data.cannonName} ({myIdx}/{total})"
////            : entry.data.cannonName;
////    }

////    private static string FormatTimer(float seconds)
////    {
////        float s = Mathf.Max(0f, seconds);
////        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
////    }

////    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

////    public int CountOwned(CannonData data)
////    {
////        int n = 0;
////        foreach (var e in _inventory) if (e.data == data) n++;
////        return n;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // EDITOR VALIDATION
////    // ══════════════════════════════════════════════════════════════════════════

////#if UNITY_EDITOR
////    private void OnValidate()
////    {
////        if (cannonTypes == null || cannonTypes.Length == 0)
////            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
////        if (cards == null || cards.Length == 0)
////            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
////        if (cannonCardPrefab == null)
////            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
////        if (inventoryGridContent == null)
////            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
////        if (buyButton == null)
////            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
////    }
////#endif
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonPanelManager
/////
///// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
/////
/////  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
/////     Inventory cards spawn into a plain Transform (inventoryGridContent).
/////     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
/////
/////  2. RANGE BAR replaces Ability bar.
/////     Inspector fields: rangeBar / rangeValueText.
/////     Max value: maxRange.
/////
/////  3. TIMER hidden by default.
/////     timerText.gameObject is deactivated when not upgrading.
/////     Shown (and counts down) only while an upgrade is running.
/////
/////  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
/////     Both the value text and a separate delta label can show this.
/////
/////  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
/////     buyTabButton and inventoryTabButton are never hidden by this manager.
/////
///// ════ HIERARCHY ══════════════════════════════════════════════════════════════
/////
/////  CannonPanel
/////  ├── BackButton
/////  ├── BuyTabButton          ← always visible; switches to Buy mode
/////  ├── InventoryTabButton    ← always visible; switches to Inventory mode
/////  ├── CardGrid              ← 3 pre-placed buy CannonCards
/////  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
/////  ├── Details Panel
/////  │   ├── PreviewImage
/////  │   ├── LevelText         "LEVEL 1"
/////  │   ├── TimerText         hidden unless upgrading → shows "01:30"
/////  │   ├── NameText
/////  │   ├── CostText          "Cost: 100"
/////  │   ├── RangeStatText     "Range: 40m"
/////  │   ├── HealthBar (Filled Image) + HealthValueText
/////  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
/////  │   ├── DamageBar (Filled Image) + DamageValueText
/////  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
/////  │   │   └── UpgradeProgressBar (Filled Image)
/////  │   ├── BuyButton + BuyButtonText
/////  │   ├── EquipButton
/////  │   ├── UnequipButton
/////  │   └── UpgradeButton + UpgradeButtonText
/////  └── CoinText
/////
///// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
/////  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
/////  Castle Slots          all CannonSlot prefab instances in the scene
/////  Starting Gold         default 840
///// </summary>
//public class CannonPanelManager : MonoBehaviour
//{
//    public static CannonPanelManager Instance { get; private set; }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INSPECTOR FIELDS
//    // ══════════════════════════════════════════════════════════════════════════

//    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
//    [SerializeField] private CannonData[] cannonTypes;

//    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
//    [SerializeField] private CannonSlot[] castleSlots;

//    // ── Tab & Back Buttons ────────────────────────────────────────────────────
//    [Header("Tab Buttons  (always visible in both modes)")]
//    [SerializeField] private Button buyTabButton;
//    [SerializeField] private Button inventoryTabButton;
//    [SerializeField] private Button backButton;

//    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
//    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
//    [SerializeField] private GameObject cardGridRoot;
//    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

//    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
//    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
//    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
//    [SerializeField] private Transform inventoryGridContent;
//    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
//    [SerializeField] private CannonCard cannonCardPrefab;

//    // ── Details Panel ─────────────────────────────────────────────────────────
//    [Header("Details Panel")]
//    [SerializeField] private Image previewImage;
//    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
//    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
//    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
//    [SerializeField] private TextMeshProUGUI nameText;
//    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
//    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

//    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
//    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
//    [SerializeField] private Image healthBar;
//    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
//    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
//    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
//    [SerializeField] private Image damageBar;
//    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

//    [Header("Max values used to compute bar fill  (tune per game balance)")]
//    [SerializeField] private float maxHealth = 200f;
//    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
//    [SerializeField] private float maxDamage = 100f;

//    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
//    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
//    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
//    [SerializeField] private GameObject upgradeProgressBG;
//    [SerializeField] private Image upgradeProgressBar;

//    // ── Action Buttons ────────────────────────────────────────────────────────
//    [Header("Action Buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button equipButton;
//    [SerializeField] private Button unequipButton;
//    [SerializeField] private Button upgradeButton;
//    [SerializeField] private TextMeshProUGUI upgradeButtonText;
//    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
//             + "Visible only in Inventory mode — switches back to Buy mode.")]
//    [SerializeField] private Button inventoryBuyButton;

//    // ── Coin & Warning ────────────────────────────────────────────────────────
//    [Header("Coin & Warning")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private TextMeshProUGUI warningText;

//    [Header("Starting Gold")]
//    [SerializeField] private int startingGold = 840;

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE STATE
//    // ══════════════════════════════════════════════════════════════════════════

//    private int _gold;

//    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//    private int _nextId = 0;
//    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

//    private enum Mode { Buy, Inventory }
//    private Mode _mode = Mode.Buy;

//    private CannonSlot _callingSlot;

//    // Buy mode
//    private CannonCard _selectedBuyCard;
//    private CannonData _selectedBuyData;

//    // Inventory mode — dynamically spawned cards
//    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
//    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
//    private CannonCard _selectedInventoryCard;

//    private CannonInventoryEntry SelectedEntry
//    {
//        get
//        {
//            if (_selectedInventoryCard == null) return null;
//            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
//            return e;
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        _gold = startingGold;

//        WireButtons();
//        SetupBuyCards();
//        RefreshCoinText();
//        HideTimer();
//        HideProgressBar();

//        ShowBuyMode();   // start in Buy mode
//    }

//    private void Update()
//    {
//        if (_mode == Mode.Inventory)
//            TickUpgrades();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INIT
//    // ══════════════════════════════════════════════════════════════════════════

//    private void WireButtons()
//    {
//        // Tab buttons — always visible, switch mode when clicked
//        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
//        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
//        backButton?.onClick.AddListener(OnBackClicked);

//        // Action buttons
//        buyButton?.onClick.AddListener(OnBuyClicked);
//        equipButton?.onClick.AddListener(OnEquipClicked);
//        unequipButton?.onClick.AddListener(OnUnequipClicked);
//        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
//        // Inventory-mode Buy button — takes player back to Buy mode
//        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
//    }

//    private void SetupBuyCards()
//    {
//        if (cards == null || cannonTypes == null) return;
//        for (int i = 0; i < cards.Length; i++)
//        {
//            if (cards[i] == null) continue;
//            if (i < cannonTypes.Length && cannonTypes[i] != null)
//                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
//            else
//                cards[i].gameObject.SetActive(false);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC ENTRY POINTS
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
//    public void OnPanelOpened(CannonSlot callingSlot)
//    {
//        _callingSlot = callingSlot;
//        gameObject.SetActive(true);
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    //    public void OnPanelOpened()
//    //    {
//    //        RefreshCoinText();
//    //        SwitchToBuyMode();
//    //    }

//    /// <summary>
//    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
//    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
//    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
//    /// </summary>
//    public void OpenAtInventory(CannonSlot callingSlot)
//    {
//        _callingSlot = callingSlot;
//        gameObject.SetActive(true);
//        RefreshCoinText();

//        if (_inventory.Count == 0)
//        {
//            SwitchToBuyMode();
//            return;
//        }

//        _mode = Mode.Inventory;
//        ShowInventoryMode();

//        // Pre-select the card that belongs to this slot's cannon
//        if (callingSlot?.Entry != null)
//        {
//            int targetId = callingSlot.Entry.inventoryId;
//            foreach (var c in _spawnedCards)
//                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
//                { SelectInventoryCard(c, e); break; }
//        }
//    }

//    /// <summary>
//    /// Called when the player clicks a CannonSlotCastle (castle-grid cannon zone).
//    /// Opens the panel in Buy mode so the player can purchase/assign a cannon.
//    /// _callingSlot is set to null because CannonSlotCastle is not a CannonSlot.
//    /// </summary>
//    public void OpenFromCastleSlot(CannonSlotCastle castleSlot)
//    {
//        _callingSlot = null;
//        gameObject.SetActive(true);
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // MODE SWITCHING
//    // ══════════════════════════════════════════════════════════════════════════

//    // Called by BuyTabButton — always visible, works from any mode
//    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

//    private void SwitchToInventoryMode()
//    {
//        // FIX: warn and block if the player hasn't bought anything yet
//        if (_inventory.Count == 0)
//        {
//            ShowWarning("Buy a cannon first!");
//            return;
//        }
//        _mode = Mode.Inventory;
//        ShowInventoryMode();
//    }

//    // ── Buy Mode ──────────────────────────────────────────────────────────────

//    private void ShowBuyMode()
//    {
//        // Show card grid; hide inventory container
//        if (cardGridRoot != null) cardGridRoot.SetActive(true);
//        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

//        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
//        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
//        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

//        // Inventory-mode Buy button is only relevant when in Inventory
//        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

//        // Buy button visible; inventory action buttons hidden
//        SetActionButtons(buyVisible: true, inventoryVisible: false);

//        HideTimer();
//        HideProgressBar();
//        ClearWarning();

//        // Refresh buy card locks (may have changed since last visit)
//        if (cards != null && cannonTypes != null)
//        {
//            for (int i = 0; i < cards.Length; i++)
//            {
//                if (cards[i] == null) continue;
//                if (i < cannonTypes.Length && cannonTypes[i] != null)
//                {
//                    cards[i].gameObject.SetActive(true);
//                    cards[i].SetupBuyCard(cannonTypes[i],
//                        locked: !_everBought.Contains(cannonTypes[i]));
//                }
//                else
//                {
//                    cards[i].gameObject.SetActive(false);
//                }
//            }
//        }

//        // Restore previous selection or auto-select first card
//        if (_selectedBuyCard != null && cards != null
//            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
//        {
//            _selectedBuyCard.SetSelected(true);
//            ShowBuyDetails(_selectedBuyData);
//            RefreshBuyButton();
//        }
//        else if (cards != null && cards.Length > 0 && cards[0] != null)
//        {
//            SelectBuyCard(cards[0], 0);
//        }
//        else
//        {
//            ClearDetails();
//        }
//    }

//    // ── Inventory Mode ─────────────────────────────────────────────────────────

//    private void ShowInventoryMode()
//    {
//        // Hide card grid; show inventory container
//        if (cardGridRoot != null) cardGridRoot.SetActive(false);
//        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

//        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
//        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
//        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

//        // Show the in-inventory Buy button so the player can jump to the Buy panel
//        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

//        // Inventory action buttons visible; buy button hidden
//        SetActionButtons(buyVisible: false, inventoryVisible: true);

//        ClearWarning();
//        HideTimer();
//        HideProgressBar();

//        // Destroy previously spawned inventory cards
//        foreach (CannonCard c in _spawnedCards)
//            if (c != null) Destroy(c.gameObject);
//        _spawnedCards.Clear();
//        _cardEntryMap.Clear();
//        _selectedInventoryCard = null;

//        if (cannonCardPrefab == null || inventoryGridContent == null)
//        {
//            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//            return;
//        }

//        // Spawn one card per owned cannon entry
//        // Card label always shows the plain cannon name — no "(1/3)" clutter.
//        // The copy number is shown only in the Details panel via GetDetailDisplayName().
//        foreach (var entry in _inventory)
//        {
//            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
//            string displayName = entry.data.cannonName;   // plain name on the card
//            card.SetupInventoryCard(entry, displayName);
//            _spawnedCards.Add(card);
//            _cardEntryMap[card] = entry;
//        }

//        // Auto-select: restore previous selection, else pick first card
//        if (_spawnedCards.Count > 0)
//        {
//            CannonCard toSelect = null;
//            int prevId = SelectedEntry?.inventoryId ?? -1;

//            if (prevId >= 0)
//            {
//                foreach (CannonCard c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
//                    { toSelect = c; break; }
//            }

//            if (toSelect == null) toSelect = _spawnedCards[0];
//            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
//        }
//        else
//        {
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnCardSelected(CannonCard card)
//    {
//        if (_mode == Mode.Buy)
//        {
//            if (cards == null) return;
//            int idx = System.Array.IndexOf(cards, card);
//            if (idx >= 0) SelectBuyCard(card, idx);
//        }
//        else
//        {
//            if (_cardEntryMap.TryGetValue(card, out var entry))
//                SelectInventoryCard(card, entry);
//        }
//        ClearWarning();
//    }

//    private void SelectBuyCard(CannonCard card, int idx)
//    {
//        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
//        _selectedBuyCard = card;
//        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
//        card.SetSelected(true);

//        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
//        else ClearDetails();
//    }

//    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
//    {
//        foreach (var c in _spawnedCards) c?.SetSelected(false);
//        _selectedInventoryCard = card;
//        card?.SetSelected(true);

//        ShowInventoryDetails(entry);
//        RefreshInventoryButtons(entry);
//        RefreshTimerAndProgress(entry);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BUY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBuyClicked()
//    {
//        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
//        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

//        _gold -= _selectedBuyData.cost;
//        RefreshCoinText();

//        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
//        _inventory.Add(entry);

//        // Unlock the buy card on first purchase of this type
//        if (!_everBought.Contains(_selectedBuyData))
//        {
//            _everBought.Add(_selectedBuyData);
//            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
//                if (cannonTypes[i] == _selectedBuyData)
//                    cards[i]?.SetLocked(false);
//        }

//        RefreshBuyButton();
//        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
//        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
//    }

//    private void RefreshBuyButton()
//    {
//        if (buyButton == null) return;
//        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
//        buyButton.interactable = canAfford;
//        if (buyButtonText != null)
//            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // EQUIP / UNEQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnEquipClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

//        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
//            ? _callingSlot
//            : FindFreeSlot();

//        if (target == null) { ShowWarning("No free cannon slot!"); return; }

//        target.Equip(entry);
//        RefreshInventoryButtons(entry);
//        ShowWarning($"Equipped {entry.data.cannonName}!");
//    }

//    private void OnUnequipClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
//        entry.equippedSlot?.Unequip();
//    }

//    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
//    public void RefreshAfterUnequip()
//    {
//        if (_mode == Mode.Inventory) ShowInventoryMode();
//    }

//    private CannonSlot FindFreeSlot()
//    {
//        if (castleSlots == null) return null;
//        foreach (var s in castleSlots)
//            if (s != null && !s.IsOccupied) return s;
//        return null;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UPGRADE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnUpgradeClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
//        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

//        entry.isUpgrading = true;
//        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//        // Activate the badge on the card right away so the player sees the upgrade is running
//        _selectedInventoryCard?.RefreshBadge(entry);

//        // Immediately update UI to show timer, progress bar, and "40+7" stat format
//        ShowInventoryDetails(entry);
//        RefreshInventoryButtons(entry);
//        RefreshTimerAndProgress(entry);
//        ShowWarning("Upgrading…");
//    }

//    private void TickUpgrades()
//    {
//        bool anyCompleted = false;

//        foreach (var entry in _inventory)
//        {
//            if (!entry.isUpgrading) continue;

//            float remaining = entry.UpgradeTimeRemaining;

//            // Update timer + progress bar for the currently selected entry
//            if (entry == SelectedEntry)
//            {
//                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//                float progress = 1f - Mathf.Clamp01(remaining / total);

//                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//                if (timerText != null) timerText.text = FormatTimer(remaining);
//            }

//            if (remaining <= 0f)
//            {
//                entry.upgradeCount++;
//                entry.isUpgrading = false;
//                anyCompleted = true;

//                // Refresh the badge on the card in-place
//                foreach (var c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
//                        c.RefreshBadge(entry);

//                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
//                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
//            }
//        }

//        if (anyCompleted)
//        {
//            // Rebuild card list so everything reflects the new level
//            CannonInventoryEntry prevSel = SelectedEntry;
//            ShowInventoryMode();

//            if (prevSel != null)
//            {
//                foreach (var c in _spawnedCards)
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
//                    { SelectInventoryCard(c, e); break; }
//            }
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DETAILS PANEL — BUY MODE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowBuyDetails(CannonData data)
//    {
//        if (data == null) { ClearDetails(); return; }

//        ApplyPreview(data.previewSprite
//            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
//        ;
//        if (costText != null) costText.text = $"Cost: {data.cost}";
//        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

//        // Plain values — no delta, no timer
//        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
//        HideTimer();
//        HideProgressBar();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DETAILS PANEL — INVENTORY MODE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowInventoryDetails(CannonInventoryEntry entry)
//    {
//        if (entry == null) { ClearDetails(); return; }

//        ApplyPreview(entry.data.previewSprite
//            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

//        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
//        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
//        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
//        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

//        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
//        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
//                   upgrading: showDelta, entry: entry);
//    }

//    // ── Details helpers ────────────────────────────────────────────────────────

//    private void ClearDetails()
//    {
//        ApplyPreview(null);
//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (nameText != null) nameText.text = "—";
//        if (costText != null) costText.text = "";
//        if (rangeStatText != null) rangeStatText.text = "";
//        SetHUDBars(0f, 0f, 0f, upgrading: false);
//        HideTimer();
//    }

//    private void ApplyPreview(Sprite s)
//    {
//        if (previewImage == null) return;
//        previewImage.enabled = s != null;
//        if (s != null) previewImage.sprite = s;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HUD BARS — Health / Range / Damage
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Sets the three stat bars.
//    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
//    /// (current value + the gain from the next upgrade level).
//    /// </summary>
//    private void SetHUDBars(float h, float r, float d,
//                            bool upgrading,
//                            CannonInventoryEntry entry = null)
//    {
//        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
//        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
//        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

//        if (upgrading && entry != null && !entry.IsMaxLevel)
//        {
//            // Peek at next-level stats without permanently mutating the entry
//            entry.upgradeCount++;
//            float nh = entry.CurrentHealth;
//            float nr = entry.CurrentRange;
//            float nd = entry.CurrentDamage;
//            entry.upgradeCount--;

//            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
//            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
//            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
//            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
//        }
//        else
//        {
//            if (healthValueText != null) healthValueText.text = $"{h:F0}";
//            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
//            if (damageValueText != null) damageValueText.text = $"{d:F0}";
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // TIMER & PROGRESS BAR
//    // Timer is hidden by default; activated only during an upgrade.
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
//    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
//    {
//        if (entry != null && entry.isUpgrading)
//        {
//            ShowTimer(entry.UpgradeTimeRemaining);
//            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
//            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//        }
//        else
//        {
//            HideTimer();
//            HideProgressBar();
//        }
//    }

//    // Timer — show only while upgrading
//    private void ShowTimer(float seconds)
//    {
//        if (timerText == null) return;
//        timerText.gameObject.SetActive(true);
//        timerText.text = FormatTimer(seconds);
//    }

//    private void HideTimer()
//    {
//        if (timerText != null) timerText.gameObject.SetActive(false);
//    }

//    // Progress bar — show only while upgrading
//    private void ShowProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//    }

//    private void HideProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INVENTORY BUTTON STATES
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RefreshInventoryButtons(CannonInventoryEntry entry)
//    {
//        if (entry == null) { SetInventoryButtonsEmpty(); return; }

//        bool equipped = entry.isEquipped;
//        bool maxLevel = entry.IsMaxLevel;
//        bool upgrading = entry.isUpgrading;

//        // Equip / Unequip are mutually exclusive
//        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
//        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

//        if (upgradeButton != null)
//        {
//            upgradeButton.interactable = !maxLevel && !upgrading;
//            if (upgradeButtonText != null)
//            {
//                if (maxLevel) upgradeButtonText.text = "MAX";
//                else if (upgrading) upgradeButtonText.text = "";
//                else upgradeButtonText.text =
//                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//            }
//        }
//    }

//    private void SetInventoryButtonsEmpty()
//    {
//        if (equipButton != null) equipButton.interactable = false;
//        if (unequipButton != null) unequipButton.interactable = false;
//        if (upgradeButton != null)
//        {
//            upgradeButton.interactable = false;
//            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
//        }
//    }

//    /// <summary>
//    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
//    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
//    /// they are always visible so the player can switch modes freely.
//    /// </summary>
//    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
//    {
//        buyButton?.gameObject.SetActive(buyVisible);
//        equipButton?.gameObject.SetActive(inventoryVisible);
//        unequipButton?.gameObject.SetActive(inventoryVisible);
//        upgradeButton?.gameObject.SetActive(inventoryVisible);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BACK / CLOSE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBackClicked()
//    {
//        ClearWarning();

//        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
//        if (_mode == Mode.Inventory)
//        {
//            SwitchToBuyMode();
//            return;
//        }

//        // In Buy mode — close the panel as usual
//        if (GameManager.Instance != null)
//            GameManager.Instance.CloseCurrentPanel();
//        else
//            gameObject.SetActive(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // COIN
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RefreshCoinText()
//    {
//        if (coinText != null) coinText.text = _gold.ToString();
//    }

//    public void AddGold(int amount)
//    {
//        _gold += amount;
//        RefreshCoinText();
//        if (_mode == Mode.Buy) RefreshBuyButton();
//    }

//    public int GetGold() => _gold;

//    // ══════════════════════════════════════════════════════════════════════════
//    // WARNING / FEEDBACK
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowWarning(string msg)
//    {
//        if (warningText == null) return;
//        warningText.text = msg;
//        CancelInvoke(nameof(ClearWarning));
//        Invoke(nameof(ClearWarning), 2.5f);
//    }

//    private void ClearWarning()
//    {
//        if (warningText != null) warningText.text = "";
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UTILITIES
//    // ══════════════════════════════════════════════════════════════════════════

//    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
//        => totalCopies > 1
//            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
//            : entry.data.cannonName;

//    private string GetDetailDisplayName(CannonInventoryEntry entry)
//    {
//        int total = 0, myIdx = 0;
//        foreach (var e in _inventory)
//        {
//            if (e.data == entry.data) total++;
//            if (e == entry && myIdx == 0) myIdx = total;
//        }
//        return total > 1
//            ? $"{entry.data.cannonName} ({myIdx}/{total})"
//            : entry.data.cannonName;
//    }

//    private static string FormatTimer(float seconds)
//    {
//        float s = Mathf.Max(0f, seconds);
//        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//    }

//    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

//    public int CountOwned(CannonData data)
//    {
//        int n = 0;
//        foreach (var e in _inventory) if (e.data == data) n++;
//        return n;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // EDITOR VALIDATION
//    // ══════════════════════════════════════════════════════════════════════════

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (cannonTypes == null || cannonTypes.Length == 0)
//            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
//        if (cards == null || cards.Length == 0)
//            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
//        if (cannonCardPrefab == null)
//            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
//        if (inventoryGridContent == null)
//            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
//        if (buyButton == null)
//            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
//    }
//#endif
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonPanelManager
///
/// ════ CHANGES IN THIS VERSION ════════════════════════════════════════════════
///
///  1. NO SCROLL RECT — inventoryScrollRoot removed entirely.
///     Inventory cards spawn into a plain Transform (inventoryGridContent).
///     Assign any container (e.g. a GridLayoutGroup) — no ScrollRect needed.
///
///  2. RANGE BAR replaces Ability bar.
///     Inspector fields: rangeBar / rangeValueText.
///     Max value: maxRange.
///
///  3. TIMER hidden by default.
///     timerText.gameObject is deactivated when not upgrading.
///     Shown (and counts down) only while an upgrade is running.
///
///  4. STAT DELTA FORMAT — "40+7" (current + gain) while upgrading.
///     Both the value text and a separate delta label can show this.
///
///  5. BUY TAB always visible — clicking it from Inventory mode returns to Buy.
///     buyTabButton and inventoryTabButton are never hidden by this manager.
///
/// ════ HIERARCHY ══════════════════════════════════════════════════════════════
///
///  CannonPanel
///  ├── BackButton
///  ├── BuyTabButton          ← always visible; switches to Buy mode
///  ├── InventoryTabButton    ← always visible; switches to Inventory mode
///  ├── CardGrid              ← 3 pre-placed buy CannonCards
///  ├── InventoryGrid         ← plain container (NO ScrollRect). Any LayoutGroup.
///  ├── Details Panel
///  │   ├── PreviewImage
///  │   ├── LevelText         "LEVEL 1"
///  │   ├── TimerText         hidden unless upgrading → shows "01:30"
///  │   ├── NameText
///  │   ├── CostText          "Cost: 100"
///  │   ├── RangeStatText     "Range: 40m"
///  │   ├── HealthBar (Filled Image) + HealthValueText
///  │   ├── RangeBar  (Filled Image) + RangeValueText   ← replaces AbilityBar
///  │   ├── DamageBar (Filled Image) + DamageValueText
///  │   ├── UpgradeProgressBG (parent GO, hidden when not upgrading)
///  │   │   └── UpgradeProgressBar (Filled Image)
///  │   ├── BuyButton + BuyButtonText
///  │   ├── EquipButton
///  │   ├── UnequipButton
///  │   └── UpgradeButton + UpgradeButtonText
///  └── CoinText
///
/// ════ INSPECTOR WIRING ═══════════════════════════════════════════════════════
///  Cannon Types          3 CannonData ScriptableObjects (same order as buy cards)
///  Castle Slots          all CannonSlot prefab instances in the scene
///  Starting Gold         default 840
/// </summary>
public class CannonPanelManager : MonoBehaviour
{
    public static CannonPanelManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ══════════════════════════════════════════════════════════════════════════

    [Header("Cannon Types — 3 CannonData assets (same order as buy cards)")]
    [SerializeField] private CannonData[] cannonTypes;

    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
    [SerializeField] private CannonSlot[] castleSlots;

    // ── Tab & Back Buttons ────────────────────────────────────────────────────
    [Header("Tab Buttons  (always visible in both modes)")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button backButton;

    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
    [Header("Buy Mode — Card Grid  (3 pre-placed CannonCard objects)")]
    [SerializeField] private GameObject cardGridRoot;
    [SerializeField] private CannonCard[] cards;   // exactly 3 pre-placed buy cards

    // ── Inventory Mode — plain grid container, NO ScrollRect ──────────────────
    [Header("Inventory Mode — Plain Grid Container  (NO ScrollRect)")]
    [Tooltip("Assign any Transform or LayoutGroup container. Cards are Instantiated here.")]
    [SerializeField] private Transform inventoryGridContent;
    [Tooltip("CannonCard prefab (must have children: CannonImage, CardName, Selected, Locked, UpgradeBadge)")]
    [SerializeField] private CannonCard cannonCardPrefab;

    // ── Details Panel ─────────────────────────────────────────────────────────
    [Header("Details Panel")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI levelText;        // "LEVEL 1"
    [Tooltip("Hidden when not upgrading; shown + counting down during an upgrade.")]
    [SerializeField] private TextMeshProUGUI timerText;        // "01:30"
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;         // "Cost: 100"
    [SerializeField] private TextMeshProUGUI rangeStatText;    // "Range: 40m"

    // ── HUD Bars — Health / RANGE / Damage  (Image Type = Filled) ─────────────
    [Header("HUD Bars  (Filled Image, Horizontal, Fill Origin = Left)")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthValueText;  // "80" or "80+10"
    [SerializeField] private Image rangeBar;         // ← replaces old AbilityBar
    [SerializeField] private TextMeshProUGUI rangeValueText;   // "40" or "40+8"
    [SerializeField] private Image damageBar;
    [SerializeField] private TextMeshProUGUI damageValueText;  // "20" or "20+5"

    [Header("Max values used to compute bar fill  (tune per game balance)")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float maxRange = 120f;   // ← replaces old maxAbility
    [SerializeField] private float maxDamage = 100f;

    // ── Upgrade Progress Bar  (hidden when not upgrading) ─────────────────────
    [Header("Upgrade Progress Bar  (hidden when not upgrading)")]
    [Tooltip("Parent GameObject that wraps the progress bar; deactivated when not upgrading.")]
    [SerializeField] private GameObject upgradeProgressBG;
    [SerializeField] private Image upgradeProgressBar;

    // ── Action Buttons ────────────────────────────────────────────────────────
    [Header("Action Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [Tooltip("A 'Buy' button placed inside the Inventory grid. "
             + "Visible only in Inventory mode — switches back to Buy mode.")]
    [SerializeField] private Button inventoryBuyButton;

    // ── Coin & Warning ────────────────────────────────────────────────────────
    [Header("Coin & Warning")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Starting Gold")]
    [SerializeField] private int startingGold = 840;

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE STATE
    // ══════════════════════════════════════════════════════════════════════════

    private int _gold;

    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
    private int _nextId = 0;
    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

    private enum Mode { Buy, Inventory }
    private Mode _mode = Mode.Buy;

    private CannonSlot _callingSlot;
    private CannonSlotCastle _callingCastleSlot;
    private CastleUnitDropZone _callingDropZone;   // ← set when opened from a CannonZone click

    /// <summary>
    /// Set by CastleUnitDropZone.OnPointerClick BEFORE GameManager activates this panel.
    /// Consumed in OnPanelOpened so the drop zone target is never lost.
    /// </summary>
    public static CastleUnitDropZone PendingDropZone;

    // Buy mode
    private CannonCard _selectedBuyCard;
    private CannonData _selectedBuyData;

    // Inventory mode — dynamically spawned cards
    private readonly List<CannonCard> _spawnedCards = new List<CannonCard>();
    private readonly Dictionary<CannonCard, CannonInventoryEntry> _cardEntryMap = new Dictionary<CannonCard, CannonInventoryEntry>();
    private CannonCard _selectedInventoryCard;

    private CannonInventoryEntry SelectedEntry
    {
        get
        {
            if (_selectedInventoryCard == null) return null;
            _cardEntryMap.TryGetValue(_selectedInventoryCard, out var e);
            return e;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _gold = startingGold;

        WireButtons();
        SetupBuyCards();
        RefreshCoinText();
        HideTimer();
        HideProgressBar();

        ShowBuyMode();   // start in Buy mode
    }

    private void Update()
    {
        if (_mode == Mode.Inventory)
            TickUpgrades();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // INIT
    // ══════════════════════════════════════════════════════════════════════════

    private void WireButtons()
    {
        // Tab buttons — always visible, switch mode when clicked
        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
        backButton?.onClick.AddListener(OnBackClicked);

        // Action buttons
        buyButton?.onClick.AddListener(OnBuyClicked);
        equipButton?.onClick.AddListener(OnEquipClicked);
        unequipButton?.onClick.AddListener(OnUnequipClicked);
        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
        // Inventory-mode Buy button — takes player back to Buy mode
        inventoryBuyButton?.onClick.AddListener(SwitchToBuyMode);
    }

    private void SetupBuyCards()
    {
        if (cards == null || cannonTypes == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < cannonTypes.Length && cannonTypes[i] != null)
                cards[i].SetupBuyCard(cannonTypes[i], locked: true);
            else
                cards[i].gameObject.SetActive(false);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC ENTRY POINTS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Called by a CannonSlot's Add button to open the panel in Buy mode.</summary>
    public void OnPanelOpened(CannonSlot callingSlot)
    {
        _callingSlot = callingSlot;
        _callingCastleSlot = null;

        // If a CannonZone was clicked just before GameManager activated this panel,
        // PendingDropZone will be set — pick it up now and clear the static.
        if (PendingDropZone != null)
        {
            _callingDropZone = PendingDropZone;
            PendingDropZone = null;
        }
        else if (callingSlot != null)
        {
            // Opened from a CannonSlot click — clear any previous drop zone target.
            _callingDropZone = null;
        }
        // If callingSlot is null and PendingDropZone was null, preserve _callingDropZone
        // in case OpenFromDropZone already set it this frame.

        gameObject.SetActive(true);
        RefreshCoinText();
        SwitchToBuyMode();
    }

    /// <summary>
    /// Called when the player clicks a CannonZone on the castle grid.
    /// Stores the castle slot so OnEquipClicked can equip directly into it.
    /// </summary>
    public void OpenFromCastleSlot(CannonSlotCastle castleSlot)
    {
        _callingSlot = null;
        _callingCastleSlot = castleSlot;
        _callingDropZone = null;
        gameObject.SetActive(true);
        RefreshCoinText();
        SwitchToBuyMode();
    }

    /// <summary>
    /// Called when the player clicks a CannonZone (CastleUnitDropZone) on the castle grid.
    /// Stores the drop zone so OnEquipClicked can place the cannon prefab directly into it.
    /// </summary>
    public void OpenFromDropZone(CastleUnitDropZone dropZone)
    {
        _callingSlot = null;
        _callingCastleSlot = null;
        _callingDropZone = dropZone;
        gameObject.SetActive(true);
        RefreshCoinText();
        SwitchToBuyMode();
    }

    //    public void OnPanelOpened()
    //    {
    //        RefreshCoinText();
    //        SwitchToBuyMode();
    //    }

    /// <summary>
    /// Called when the player clicks an equipped cannon prefab inside a CannonSlot.
    /// Opens the panel directly in Inventory mode and pre-selects that cannon's card.
    /// If the inventory is empty (shouldn't happen from a slot click) falls back to Buy.
    /// </summary>
    public void OpenAtInventory(CannonSlot callingSlot)
    {
        _callingSlot = callingSlot;
        gameObject.SetActive(true);
        RefreshCoinText();

        if (_inventory.Count == 0)
        {
            SwitchToBuyMode();
            return;
        }

        _mode = Mode.Inventory;
        ShowInventoryMode();

        // Pre-select the card that belongs to this slot's cannon
        if (callingSlot?.Entry != null)
        {
            int targetId = callingSlot.Entry.inventoryId;
            foreach (var c in _spawnedCards)
                if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == targetId)
                { SelectInventoryCard(c, e); break; }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MODE SWITCHING
    // ══════════════════════════════════════════════════════════════════════════

    // Called by BuyTabButton — always visible, works from any mode
    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }

    private void SwitchToInventoryMode()
    {
        // FIX: warn and block if the player hasn't bought anything yet
        if (_inventory.Count == 0)
        {
            ShowWarning("Buy a cannon first!");
            return;
        }
        _mode = Mode.Inventory;
        ShowInventoryMode();
    }

    // ── Buy Mode ──────────────────────────────────────────────────────────────

    private void ShowBuyMode()
    {
        // Show card grid; hide inventory container
        if (cardGridRoot != null) cardGridRoot.SetActive(true);
        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

        // Tab state: in Buy mode the Buy tab is current — hide it, show Inventory tab
        if (buyTabButton != null) buyTabButton.gameObject.SetActive(false);
        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(true);

        // Inventory-mode Buy button is only relevant when in Inventory
        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(false);

        // Buy button visible; inventory action buttons hidden
        SetActionButtons(buyVisible: true, inventoryVisible: false);

        HideTimer();
        HideProgressBar();
        ClearWarning();

        // Refresh buy card locks (may have changed since last visit)
        if (cards != null && cannonTypes != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                if (i < cannonTypes.Length && cannonTypes[i] != null)
                {
                    cards[i].gameObject.SetActive(true);
                    cards[i].SetupBuyCard(cannonTypes[i],
                        locked: !_everBought.Contains(cannonTypes[i]));
                }
                else
                {
                    cards[i].gameObject.SetActive(false);
                }
            }
        }

        // Restore previous selection or auto-select first card
        if (_selectedBuyCard != null && cards != null
            && System.Array.IndexOf(cards, _selectedBuyCard) >= 0)
        {
            _selectedBuyCard.SetSelected(true);
            ShowBuyDetails(_selectedBuyData);
            RefreshBuyButton();
        }
        else if (cards != null && cards.Length > 0 && cards[0] != null)
        {
            SelectBuyCard(cards[0], 0);
        }
        else
        {
            ClearDetails();
        }
    }

    // ── Inventory Mode ─────────────────────────────────────────────────────────

    private void ShowInventoryMode()
    {
        // Hide card grid; show inventory container
        if (cardGridRoot != null) cardGridRoot.SetActive(false);
        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(true);

        // Tab state: in Inventory mode the Inventory tab is current — hide it, show Buy tab
        if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
        if (buyTabButton != null) buyTabButton.gameObject.SetActive(true);

        // Show the in-inventory Buy button so the player can jump to the Buy panel
        if (inventoryBuyButton != null) inventoryBuyButton.gameObject.SetActive(true);

        // Inventory action buttons visible; buy button hidden
        SetActionButtons(buyVisible: false, inventoryVisible: true);

        ClearWarning();
        HideTimer();
        HideProgressBar();

        // Destroy previously spawned inventory cards
        foreach (CannonCard c in _spawnedCards)
            if (c != null) Destroy(c.gameObject);
        _spawnedCards.Clear();
        _cardEntryMap.Clear();
        _selectedInventoryCard = null;

        if (cannonCardPrefab == null || inventoryGridContent == null)
        {
            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryGridContent not assigned!");
            ClearDetails();
            SetInventoryButtonsEmpty();
            return;
        }

        // Spawn one card per owned cannon entry
        // Card label always shows the plain cannon name — no "(1/3)" clutter.
        // The copy number is shown only in the Details panel via GetDetailDisplayName().
        foreach (var entry in _inventory)
        {
            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
            string displayName = entry.data.cannonName;   // plain name on the card
            card.SetupInventoryCard(entry, displayName);
            _spawnedCards.Add(card);
            _cardEntryMap[card] = entry;
        }

        // Auto-select: restore previous selection, else pick first card
        if (_spawnedCards.Count > 0)
        {
            CannonCard toSelect = null;
            int prevId = SelectedEntry?.inventoryId ?? -1;

            if (prevId >= 0)
            {
                foreach (CannonCard c in _spawnedCards)
                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevId)
                    { toSelect = c; break; }
            }

            if (toSelect == null) toSelect = _spawnedCards[0];
            SelectInventoryCard(toSelect, _cardEntryMap[toSelect]);
        }
        else
        {
            ClearDetails();
            SetInventoryButtonsEmpty();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CARD SELECTION  (called by CannonCard.OnClick → Instance.OnCardSelected)
    // ══════════════════════════════════════════════════════════════════════════

    public void OnCardSelected(CannonCard card)
    {
        if (_mode == Mode.Buy)
        {
            if (cards == null) return;
            int idx = System.Array.IndexOf(cards, card);
            if (idx >= 0) SelectBuyCard(card, idx);
        }
        else
        {
            if (_cardEntryMap.TryGetValue(card, out var entry))
                SelectInventoryCard(card, entry);
        }
        ClearWarning();
    }

    private void SelectBuyCard(CannonCard card, int idx)
    {
        if (cards != null) foreach (var c in cards) c?.SetSelected(false);
        _selectedBuyCard = card;
        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
        card.SetSelected(true);

        if (_selectedBuyData != null) { ShowBuyDetails(_selectedBuyData); RefreshBuyButton(); }
        else ClearDetails();
    }

    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
    {
        foreach (var c in _spawnedCards) c?.SetSelected(false);
        _selectedInventoryCard = card;
        card?.SetSelected(true);

        ShowInventoryDetails(entry);
        RefreshInventoryButtons(entry);
        RefreshTimerAndProgress(entry);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUY
    // ══════════════════════════════════════════════════════════════════════════

    private void OnBuyClicked()
    {
        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

        _gold -= _selectedBuyData.cost;
        RefreshCoinText();

        var entry = new CannonInventoryEntry { data = _selectedBuyData, inventoryId = _nextId++ };
        _inventory.Add(entry);

        // Unlock the buy card on first purchase of this type
        if (!_everBought.Contains(_selectedBuyData))
        {
            _everBought.Add(_selectedBuyData);
            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
                if (cannonTypes[i] == _selectedBuyData)
                    cards[i]?.SetLocked(false);
        }

        RefreshBuyButton();

        // Only auto-equip if the panel was opened by clicking a specific CannonZone
        // and that zone is still empty. Never grab a random free zone automatically.
        if (_callingDropZone != null && !_callingDropZone.HasUnit)
        {
            _callingDropZone.PlaceCannonFromPanel(entry.data?.prefab, entry);
            _callingDropZone = null;  // consumed
            Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
            // Return to castle panel so player can click the next slot.
            if (GameManager.Instance != null)
                GameManager.Instance.OpenCastlePanel();
            else
                gameObject.SetActive(false);
        }
        else
        {
            ShowWarning($"Bought {_selectedBuyData.cannonName}! Tap a slot then Equip.");
            Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
        }
    }

    private void RefreshBuyButton()
    {
        if (buyButton == null) return;
        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
        buyButton.interactable = canAfford;
        if (buyButtonText != null)
            buyButtonText.text = _selectedBuyData != null ? $"{_selectedBuyData.cost}" : "Buy";
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EQUIP / UNEQUIP
    // ══════════════════════════════════════════════════════════════════════════

    private void OnEquipClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

        // ── Path A: opened by clicking a specific CannonZone on the castle grid ──
        if (_callingDropZone != null)
        {
            if (_callingDropZone.HasUnit)
            {
                ShowWarning("This slot already has a cannon!");
                return;
            }
            _callingDropZone.PlaceCannonFromPanel(entry.data?.prefab, entry);
            _callingDropZone = null;  // consumed
            RefreshInventoryButtons(entry);
            // Return to castle panel so player can click the next slot.
            if (GameManager.Instance != null)
                GameManager.Instance.OpenCastlePanel();
            else
                gameObject.SetActive(false);
            return;
        }

        // ── Path B: legacy CannonSlot flow (panel opened from a CannonSlot, not a grid zone) ──
        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
            ? _callingSlot
            : FindFreeSlot();

        if (target == null) { ShowWarning("No free cannon slot!"); return; }

        target.Equip(entry);
        RefreshInventoryButtons(entry);
        ShowWarning($"Equipped {entry.data.cannonName}!");
    }

    private void OnUnequipClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }
        entry.equippedSlot?.Unequip();
    }

    /// <summary>Called by CannonSlot.Unequip() to rebuild the inventory card list.</summary>
    public void RefreshAfterUnequip()
    {
        if (_mode == Mode.Inventory) ShowInventoryMode();
    }

    private CannonSlot FindFreeSlot()
    {
        // First try the Inspector-assigned array (scene slots).
        if (castleSlots != null)
            foreach (var s in castleSlots)
                if (s != null && !s.IsOccupied) return s;

        // Fallback: CannonSlot objects are spawned as prefabs at runtime and
        // cannot be pre-assigned in the Inspector. Find them dynamically.
        foreach (var s in FindObjectsOfType<CannonSlot>())
            if (!s.IsOccupied) return s;

        return null;
    }

    /// <summary>
    /// Finds any free CastleUnitDropZone (CannonZone) in the scene that
    /// accepts Cannon units and is not already occupied.
    /// Used when the panel is opened without a specific zone pre-selected.
    /// </summary>
    private CastleUnitDropZone FindFreeDropZone()
    {
        foreach (var zone in FindObjectsOfType<CastleUnitDropZone>())
            if (!zone.HasUnit && zone.acceptedType == CastleUnitType.Cannon)
                return zone;
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UPGRADE
    // ══════════════════════════════════════════════════════════════════════════

    private void OnUpgradeClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (entry.IsMaxLevel) { ShowWarning("Already at max level!"); return; }
        if (entry.isUpgrading) { ShowWarning("Already upgrading!"); return; }

        entry.isUpgrading = true;
        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

        // Activate the badge on the card right away so the player sees the upgrade is running
        _selectedInventoryCard?.RefreshBadge(entry);

        // Immediately update UI to show timer, progress bar, and "40+7" stat format
        ShowInventoryDetails(entry);
        RefreshInventoryButtons(entry);
        RefreshTimerAndProgress(entry);
        ShowWarning("Upgrading…");
    }

    private void TickUpgrades()
    {
        bool anyCompleted = false;

        foreach (var entry in _inventory)
        {
            if (!entry.isUpgrading) continue;

            float remaining = entry.UpgradeTimeRemaining;

            // Update timer + progress bar for the currently selected entry
            if (entry == SelectedEntry)
            {
                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
                float progress = 1f - Mathf.Clamp01(remaining / total);

                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
                if (timerText != null) timerText.text = FormatTimer(remaining);
            }

            if (remaining <= 0f)
            {
                entry.upgradeCount++;
                entry.isUpgrading = false;
                anyCompleted = true;

                // Refresh the badge on the card in-place
                foreach (var c in _spawnedCards)
                    if (_cardEntryMap.TryGetValue(c, out var e) && e == entry)
                        c.RefreshBadge(entry);

                Debug.Log($"[CannonPanel] Upgrade complete: '{entry.data.cannonName}' " +
                          $"id={entry.inventoryId} → Level {entry.DisplayLevel}");
            }
        }

        if (anyCompleted)
        {
            // Rebuild card list so everything reflects the new level
            CannonInventoryEntry prevSel = SelectedEntry;
            ShowInventoryMode();

            if (prevSel != null)
            {
                foreach (var c in _spawnedCards)
                    if (_cardEntryMap.TryGetValue(c, out var e) && e == prevSel)
                    { SelectInventoryCard(c, e); break; }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DETAILS PANEL — BUY MODE
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowBuyDetails(CannonData data)
    {
        if (data == null) { ClearDetails(); return; }

        ApplyPreview(data.previewSprite
            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

        if (levelText != null) levelText.text = "LEVEL 1";
        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
        ;
        if (costText != null) costText.text = $"Cost: {data.cost}";
        if (rangeStatText != null) rangeStatText.text = $"Range: {data.range:F0}m";

        // Plain values — no delta, no timer
        SetHUDBars(data.health, data.range, data.damage, upgrading: false);
        HideTimer();
        HideProgressBar();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DETAILS PANEL — INVENTORY MODE
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowInventoryDetails(CannonInventoryEntry entry)
    {
        if (entry == null) { ClearDetails(); return; }

        ApplyPreview(entry.data.previewSprite
            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
        if (nameText != null) nameText.text = $"Name: {GetDetailDisplayName(entry)}";
        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
        if (rangeStatText != null) rangeStatText.text = $"Range: {entry.CurrentRange:F0}m";

        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
        SetHUDBars(entry.CurrentHealth, entry.CurrentRange, entry.CurrentDamage,
                   upgrading: showDelta, entry: entry);
    }

    // ── Details helpers ────────────────────────────────────────────────────────

    private void ClearDetails()
    {
        ApplyPreview(null);
        if (levelText != null) levelText.text = "LEVEL 1";
        if (nameText != null) nameText.text = "—";
        if (costText != null) costText.text = "";
        if (rangeStatText != null) rangeStatText.text = "";
        SetHUDBars(0f, 0f, 0f, upgrading: false);
        HideTimer();
    }

    private void ApplyPreview(Sprite s)
    {
        if (previewImage == null) return;
        previewImage.enabled = s != null;
        if (s != null) previewImage.sprite = s;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HUD BARS — Health / Range / Damage
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the three stat bars.
    /// When <paramref name="upgrading"/> is true the value texts show "40+7"
    /// (current value + the gain from the next upgrade level).
    /// </summary>
    private void SetHUDBars(float h, float r, float d,
                            bool upgrading,
                            CannonInventoryEntry entry = null)
    {
        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / Mathf.Max(1f, maxHealth));
        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(r / Mathf.Max(1f, maxRange));
        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / Mathf.Max(1f, maxDamage));

        if (upgrading && entry != null && !entry.IsMaxLevel)
        {
            // Peek at next-level stats without permanently mutating the entry
            entry.upgradeCount++;
            float nh = entry.CurrentHealth;
            float nr = entry.CurrentRange;
            float nd = entry.CurrentDamage;
            entry.upgradeCount--;

            // FIX: delta portion rendered in green — "40<color=#00E676>+8</color>"
            if (healthValueText != null) healthValueText.text = $"{h:F0}<color=#00E676>+{(nh - h):F0}</color>";
            if (rangeValueText != null) rangeValueText.text = $"{r:F0}<color=#00E676>+{(nr - r):F0}</color>";
            if (damageValueText != null) damageValueText.text = $"{d:F0}<color=#00E676>+{(nd - d):F0}</color>";
        }
        else
        {
            if (healthValueText != null) healthValueText.text = $"{h:F0}";
            if (rangeValueText != null) rangeValueText.text = $"{r:F0}";
            if (damageValueText != null) damageValueText.text = $"{d:F0}";
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TIMER & PROGRESS BAR
    // Timer is hidden by default; activated only during an upgrade.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Syncs timer text and progress bar for the given entry.</summary>
    private void RefreshTimerAndProgress(CannonInventoryEntry entry)
    {
        if (entry != null && entry.isUpgrading)
        {
            ShowTimer(entry.UpgradeTimeRemaining);
            if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
        }
        else
        {
            HideTimer();
            HideProgressBar();
        }
    }

    // Timer — show only while upgrading
    private void ShowTimer(float seconds)
    {
        if (timerText == null) return;
        timerText.gameObject.SetActive(true);
        timerText.text = FormatTimer(seconds);
    }

    private void HideTimer()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    // Progress bar — show only while upgrading
    private void ShowProgressBar()
    {
        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
    }

    private void HideProgressBar()
    {
        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // INVENTORY BUTTON STATES
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshInventoryButtons(CannonInventoryEntry entry)
    {
        if (entry == null) { SetInventoryButtonsEmpty(); return; }

        bool equipped = entry.isEquipped;
        bool maxLevel = entry.IsMaxLevel;
        bool upgrading = entry.isUpgrading;

        // Equip / Unequip are mutually exclusive
        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = !maxLevel && !upgrading;
            if (upgradeButtonText != null)
            {
                if (maxLevel) upgradeButtonText.text = "MAX";
                else if (upgrading) upgradeButtonText.text = "";
                else upgradeButtonText.text =
                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
            }
        }
    }

    private void SetInventoryButtonsEmpty()
    {
        if (equipButton != null) equipButton.interactable = false;
        if (unequipButton != null) unequipButton.interactable = false;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
            if (upgradeButtonText != null) upgradeButtonText.text = "Upgrade";
        }
    }

    /// <summary>
    /// Toggles between Buy-mode buttons (buy action) and Inventory-mode buttons.
    /// Tab buttons (buyTabButton / inventoryTabButton) are NEVER touched here —
    /// they are always visible so the player can switch modes freely.
    /// </summary>
    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
    {
        buyButton?.gameObject.SetActive(buyVisible);
        equipButton?.gameObject.SetActive(inventoryVisible);
        unequipButton?.gameObject.SetActive(inventoryVisible);
        upgradeButton?.gameObject.SetActive(inventoryVisible);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BACK / CLOSE
    // ══════════════════════════════════════════════════════════════════════════

    private void OnBackClicked()
    {
        ClearWarning();

        // FIX: while in Inventory, Back navigates to Buy mode instead of closing
        if (_mode == Mode.Inventory)
        {
            SwitchToBuyMode();
            return;
        }

        // In Buy mode — close the panel as usual
        if (GameManager.Instance != null)
            GameManager.Instance.CloseCurrentPanel();
        else
            gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // COIN
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshCoinText()
    {
        if (coinText != null) coinText.text = _gold.ToString();
    }

    public void AddGold(int amount)
    {
        _gold += amount;
        RefreshCoinText();
        if (_mode == Mode.Buy) RefreshBuyButton();
    }

    public int GetGold() => _gold;

    // ══════════════════════════════════════════════════════════════════════════
    // WARNING / FEEDBACK
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        CancelInvoke(nameof(ClearWarning));
        Invoke(nameof(ClearWarning), 2.5f);
    }

    private void ClearWarning()
    {
        if (warningText != null) warningText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UTILITIES
    // ══════════════════════════════════════════════════════════════════════════

    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
        => totalCopies > 1
            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
            : entry.data.cannonName;

    private string GetDetailDisplayName(CannonInventoryEntry entry)
    {
        int total = 0, myIdx = 0;
        foreach (var e in _inventory)
        {
            if (e.data == entry.data) total++;
            if (e == entry && myIdx == 0) myIdx = total;
        }
        return total > 1
            ? $"{entry.data.cannonName} ({myIdx}/{total})"
            : entry.data.cannonName;
    }

    private static string FormatTimer(float seconds)
    {
        float s = Mathf.Max(0f, seconds);
        return $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
    }

    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

    public int CountOwned(CannonData data)
    {
        int n = 0;
        foreach (var e in _inventory) if (e.data == data) n++;
        return n;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EDITOR VALIDATION
    // ══════════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cannonTypes == null || cannonTypes.Length == 0)
            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty — assign 3 CannonData assets.", this);
        if (cards == null || cards.Length == 0)
            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects.", this);
        if (cannonCardPrefab == null)
            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
        if (inventoryGridContent == null)
            Debug.LogWarning("[CannonPanelManager] inventoryGridContent not assigned — no container for inventory cards.", this);
        if (buyButton == null)
            Debug.LogWarning("[CannonPanelManager] buyButton not assigned.", this);
    }
#endif
}