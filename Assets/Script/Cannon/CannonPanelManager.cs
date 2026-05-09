//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonPanelManager
/////
///// Single manager that owns everything: inventory, gold, buy flow,
///// upgrade flow, and UI state. No separate "CannonArea" is needed.
/////
///// TWO MODES ───────────────────────────────────────────────────────────
/////   Buy Mode       : shows 3 fixed cannon-type cards. Player selects one
/////                    and clicks Buy to purchase a copy with coins.
/////   Inventory Mode : shows one dynamic card per owned cannon copy.
/////                    Player selects a card and clicks Upgrade (max 3 times).
/////                    Cannons can be drag-dropped onto castle CannonSlots.
/////
///// DETAILS SECTION (shared, always visible) ────────────────────────────
/////   Shows the selected cannon's: Name, Cost, Range.
/////   In Inventory mode Range shows the current upgraded value.
/////
///// UPGRADES ────────────────────────────────────────────────────────────
/////   Each owned copy has its own upgrade counter (0–3).
/////   Upgrading cannon A never affects cannon B.
/////   Upgrade timers tick in Update() even while the panel is closed.
/////
///// HIERARCHY GUIDE ─────────────────────────────────────────────────────
/////   CannonPanel
/////   ├── TabBar
/////   │   ├── TabBuyButton
/////   │   └── TabInventoryButton
/////   ├── BuyView
/////   │   ├── BuyCard_0  (CannonCard)
/////   │   ├── BuyCard_1  (CannonCard)
/////   │   └── BuyCard_2  (CannonCard)
/////   ├── InventoryView
/////   │   └── ScrollRect → InventoryCardContainer
/////   ├── DetailsSection
/////   │   ├── DetailsNameText
/////   │   ├── DetailsCostText
/////   │   └── DetailsRangeText
/////   ├── HUDSection
/////   │   ├── HealthBar (Image Filled) + HealthText
/////   │   ├── DamageBar                + DamageText
/////   │   └── RangeBar                 + RangeText
/////   ├── UpgradeProgressBarBackground
/////   │   └── UpgradeProgressBar  (Image Filled Horizontal Left)
/////   ├── UpgradeTimerText
/////   ├── BuyButton
/////   ├── UpdateButton
/////   ├── CoinText
/////   └── WarningText
///// </summary>
//public class CannonPanelManager : MonoBehaviour
//{
//    public static CannonPanelManager Instance { get; private set; }

//    // ═════════════════════════════════════════════════════════════════════════
//    // INSPECTOR FIELDS
//    // ═════════════════════════════════════════════════════════════════════════

//    [Header("Cannon Types (3 entries: Iron, Bronze, Golden)")]
//    [SerializeField] private CannonData[] cannonTypes;

//    // ─── Buy Mode ─────────────────────────────────────────────────────────────
//    [Header("Buy Mode")]
//    [SerializeField] private GameObject buyView;
//    [Tooltip("3 pre-placed CannonCard components in BuyView, same order as cannonTypes")]
//    [SerializeField] private CannonCard[] buyCards;

//    // ─── Inventory Mode ───────────────────────────────────────────────────────
//    [Header("Inventory Mode")]
//    [SerializeField] private GameObject inventoryView;
//    [Tooltip("Prefab used to spawn one card per owned cannon copy")]
//    [SerializeField] private CannonCard inventoryCardPrefab;
//    [Tooltip("Content transform of the ScrollRect")]
//    [SerializeField] private Transform inventoryCardContainer;

//    // ─── Details Section ──────────────────────────────────────────────────────
//    [Header("Details Section (shared)")]
//    [SerializeField] private TextMeshProUGUI detailsNameText;
//    [SerializeField] private TextMeshProUGUI detailsCostText;
//    [SerializeField] private TextMeshProUGUI detailsRangeText;

//    // ─── HUD Stat Bars ────────────────────────────────────────────────────────
//    [Header("HUD Stat Bars (Image Type = Filled, Horizontal, Fill Origin = Left)")]
//    [SerializeField] private Image healthBar;
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private Image damageBar;
//    [SerializeField] private TextMeshProUGUI damageText;
//    [SerializeField] private Image rangeBar;
//    [SerializeField] private TextMeshProUGUI rangeText;

//    [Header("Max stat values used for bar fill ratio")]
//    [SerializeField] private float maxHealth = 200f;
//    [SerializeField] private float maxDamage = 150f;
//    [SerializeField] private float maxRange = 200f;

//    // ─── Upgrade UI ───────────────────────────────────────────────────────────
//    [Header("Upgrade UI")]
//    [Tooltip("Parent of the progress bar — shown/hidden as upgrade starts/ends")]
//    [SerializeField] private GameObject upgradeProgressBarBG;
//    [Tooltip("Image (Filled, Horizontal, Left) — fillAmount driven in Update()")]
//    [SerializeField] private Image upgradeProgressBar;
//    [Tooltip("Shows '7.3s' countdown while upgrading")]
//    [SerializeField] private TextMeshProUGUI upgradeTimerText;

//    // ─── Buttons ──────────────────────────────────────────────────────────────
//    [Header("Buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button updateButton;
//    [SerializeField] private TextMeshProUGUI updateButtonText;   // "Upgrade (1/3)" / "MAX"

//    [Header("Tab Buttons")]
//    [SerializeField] private Button tabBuyButton;
//    [SerializeField] private Button tabInventoryButton;

//    // ─── Coin ─────────────────────────────────────────────────────────────────
//    [Header("Coin")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private int startingGold = 300;

//    // ─── Warning / Feedback ───────────────────────────────────────────────────
//    [Header("Warning / Feedback Text")]
//    [SerializeField] private TextMeshProUGUI warningText;

//    // ═════════════════════════════════════════════════════════════════════════
//    // PRIVATE STATE
//    // ═════════════════════════════════════════════════════════════════════════

//    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//    private int _nextId = 0;
//    private int _gold;

//    private enum PanelMode { Buy, Inventory }
//    private PanelMode _mode = PanelMode.Buy;

//    // ── Buy mode selection ────────────────────────────────────────────────────
//    private CannonData _selectedBuyData;
//    private CannonCard _selectedBuyCard;

//    // ── Inventory mode selection ──────────────────────────────────────────────
//    private int _selectedInventoryId = -1;

//    private CannonInventoryEntry SelectedEntry =>
//        _inventory.Find(e => e.inventoryId == _selectedInventoryId);

//    // Tracks the currently displayed inventory card for badge refreshing
//    private readonly List<CannonCard> _spawnedInventoryCards = new List<CannonCard>();

//    // ═════════════════════════════════════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ═════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        _gold = startingGold;
//    }

//    private void Start()
//    {
//        // Tab buttons
//        tabBuyButton?.onClick.AddListener(OpenBuyMode);
//        tabInventoryButton?.onClick.AddListener(OpenInventoryMode);

//        // Action buttons
//        buyButton?.onClick.AddListener(OnBuyClicked);
//        updateButton?.onClick.AddListener(OnUpdateClicked);

//        // Initialise the 3 static buy cards
//        if (buyCards != null)
//            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
//                buyCards[i].SetupBuyCard(cannonTypes[i]);

//        RefreshCoinText();
//        HideProgressBar();

//        // Open buy mode on start
//        OpenBuyMode();
//    }

//    private void Update()
//    {
//        TickAllUpgrades();
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // MODE SWITCHING
//    // ═════════════════════════════════════════════════════════════════════════

//    public void OpenBuyMode()
//    {
//        _mode = PanelMode.Buy;
//        buyView?.SetActive(true);
//        inventoryView?.SetActive(false);

//        buyButton?.gameObject.SetActive(true);
//        updateButton?.gameObject.SetActive(false);

//        HideProgressBar();

//        // Auto-select first type
//        if (cannonTypes != null && cannonTypes.Length > 0)
//            SelectBuyCard(buyCards[0], cannonTypes[0]);

//        ClearWarning();
//    }

//    public void OpenInventoryMode()
//    {
//        _mode = PanelMode.Inventory;
//        buyView?.SetActive(false);
//        inventoryView?.SetActive(true);

//        buyButton?.gameObject.SetActive(false);
//        updateButton?.gameObject.SetActive(true);

//        PopulateInventoryCards();

//        if (_inventory.Count > 0)
//        {
//            // Auto-select first owned cannon
//            _selectedInventoryId = _inventory[0].inventoryId;
//            ShowInventoryDetails(_inventory[0]);
//            RefreshUpdateButton(_inventory[0]);
//            RefreshProgressBarForSelected();
//        }
//        else
//        {
//            _selectedInventoryId = -1;
//            ClearDetails();
//            updateButton.interactable = false;
//            if (updateButtonText != null) updateButtonText.text = "No Cannons";
//            HideProgressBar();
//        }

//        ClearWarning();
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // CARD SELECTION (called by CannonCard.OnClick)
//    // ═════════════════════════════════════════════════════════════════════════

//    public void OnCardSelected(CannonCard card)
//    {
//        if (_mode == PanelMode.Buy)
//        {
//            SelectBuyCard(card, card.Data);
//        }
//        else
//        {
//            // Deselect all inventory cards
//            foreach (CannonCard c in _spawnedInventoryCards)
//                c.SetSelected(false);
//            card.SetSelected(true);

//            _selectedInventoryId = card.InventoryId;
//            CannonInventoryEntry entry = SelectedEntry;
//            if (entry == null) return;

//            ShowInventoryDetails(entry);
//            RefreshUpdateButton(entry);
//            RefreshProgressBarForSelected();
//        }
//        ClearWarning();
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // BUY FLOW
//    // ═════════════════════════════════════════════════════════════════════════

//    private void SelectBuyCard(CannonCard card, CannonData data)
//    {
//        // Clear previous selection highlight
//        if (_selectedBuyCard != null) _selectedBuyCard.SetSelected(false);

//        _selectedBuyData = data;
//        _selectedBuyCard = card;
//        card?.SetSelected(true);

//        ShowBuyDetails(data);
//        RefreshBuyButton();
//    }

//    private void OnBuyClicked()
//    {
//        if (_selectedBuyData == null)
//        {
//            ShowWarning("Select a cannon first.");
//            return;
//        }
//        if (_gold < _selectedBuyData.cost)
//        {
//            ShowWarning("Not enough coins!");
//            return;
//        }

//        // Deduct cost
//        _gold -= _selectedBuyData.cost;
//        RefreshCoinText();

//        // Create inventory entry
//        var entry = new CannonInventoryEntry
//        {
//            data = _selectedBuyData,
//            inventoryId = _nextId++,
//            upgradeCount = 0,
//            isUpgrading = false,
//            isPlacedOnCastle = false,
//            occupiedSlot = null
//        };
//        _inventory.Add(entry);

//        ShowWarning($"Purchased {_selectedBuyData.cannonName}!");
//        RefreshBuyButton();

//        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' " +
//                  $"(id={entry.inventoryId}). Total owned: {CountOwned(_selectedBuyData)}x.");
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // DETAILS SECTION
//    // ═════════════════════════════════════════════════════════════════════════

//    /// <summary>Buy mode — shows base stats from the ScriptableObject.</summary>
//    private void ShowBuyDetails(CannonData data)
//    {
//        if (detailsNameText != null) detailsNameText.text = data.cannonName;
//        if (detailsCostText != null) detailsCostText.text = $"Cost: {data.cost}";
//        if (detailsRangeText != null) detailsRangeText.text = $"Range: {data.range:F0}";

//        RefreshHUDBars(data.health, data.damage, data.range);
//    }

//    /// <summary>Inventory mode — shows current upgraded stats.</summary>
//    private void ShowInventoryDetails(CannonInventoryEntry entry)
//    {
//        if (detailsNameText != null) detailsNameText.text = entry.data.cannonName;
//        if (detailsCostText != null) detailsCostText.text = $"Cost: {entry.data.cost}";
//        if (detailsRangeText != null) detailsRangeText.text = $"Range: {entry.CurrentRange:F0}";

//        RefreshHUDBars(entry.CurrentHealth, entry.CurrentDamage, entry.CurrentRange);
//    }

//    private void ClearDetails()
//    {
//        if (detailsNameText != null) detailsNameText.text = "—";
//        if (detailsCostText != null) detailsCostText.text = "";
//        if (detailsRangeText != null) detailsRangeText.text = "";
//        RefreshHUDBars(0f, 0f, 0f);
//    }

//    private void RefreshHUDBars(float health, float damage, float range)
//    {
//        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(health / maxHealth);
//        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(damage / maxDamage);
//        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(range / maxRange);

//        if (healthText != null) healthText.text = $"{health:F0}";
//        if (damageText != null) damageText.text = $"{damage:F0}";
//        if (rangeText != null) rangeText.text = $"{range:F0}";
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // INVENTORY CARDS
//    // ═════════════════════════════════════════════════════════════════════════

//    private void PopulateInventoryCards()
//    {
//        // Destroy old cards
//        _spawnedInventoryCards.Clear();
//        if (inventoryCardContainer != null)
//            foreach (Transform child in inventoryCardContainer)
//                Destroy(child.gameObject);

//        if (inventoryCardPrefab == null) return;

//        foreach (CannonInventoryEntry entry in _inventory)
//        {
//            // Only show cannons that are NOT currently placed on the castle
//            // (placed cannons are on the castle — they don't need to show in the list)
//            // Remove the "if" below if you want placed cannons to still show
//            if (entry.isPlacedOnCastle) continue;

//            CannonCard card = Instantiate(inventoryCardPrefab, inventoryCardContainer);
//            card.SetupInventoryCard(entry);

//            // Restore selection highlight
//            if (entry.inventoryId == _selectedInventoryId)
//                card.SetSelected(true);

//            _spawnedInventoryCards.Add(card);
//        }
//    }

//    private void RefreshInventoryCardBadges()
//    {
//        foreach (CannonCard card in _spawnedInventoryCards)
//        {
//            if (card == null) continue;
//            CannonInventoryEntry entry = _inventory.Find(e => e.inventoryId == card.InventoryId);
//            if (entry != null) card.RefreshUpgradeBadge(entry);
//        }
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // UPGRADE FLOW
//    // ═════════════════════════════════════════════════════════════════════════

//    private void OnUpdateClicked()
//    {
//        CannonInventoryEntry entry = SelectedEntry;

//        if (entry == null)
//        {
//            ShowWarning("Select a cannon first.");
//            return;
//        }
//        if (entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES)
//        {
//            ShowWarning("Already at maximum level!");
//            return;
//        }
//        if (entry.isUpgrading)
//        {
//            ShowWarning("Upgrade already in progress.");
//            return;
//        }

//        // Start the upgrade timer
//        entry.isUpgrading = true;
//        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//        RefreshUpdateButton(entry);
//        ShowProgressBar();
//        RefreshInventoryCardBadges();

//        Debug.Log($"[CannonPanel] Upgrade started for '{entry.data.cannonName}' " +
//                  $"(id={entry.inventoryId}, upgrade {entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES})");
//    }

//    private void RefreshUpdateButton(CannonInventoryEntry entry)
//    {
//        if (updateButton == null) return;

//        if (entry == null)
//        {
//            updateButton.interactable = false;
//            if (updateButtonText != null) updateButtonText.text = "No Cannons";
//            return;
//        }

//        if (entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES)
//        {
//            if (updateButtonText != null) updateButtonText.text = "MAX";
//            updateButton.interactable = false;
//        }
//        else if (entry.isUpgrading)
//        {
//            if (updateButtonText != null) updateButtonText.text = "Upgrading...";
//            updateButton.interactable = false;
//        }
//        else
//        {
//            if (updateButtonText != null)
//                updateButtonText.text = $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//            updateButton.interactable = true;
//        }
//    }

//    // ─── Upgrade tick (called every frame in Update) ───────────────────────

//    private void TickAllUpgrades()
//    {
//        // All entries tick in the background even while the panel is closed
//        foreach (CannonInventoryEntry entry in _inventory)
//        {
//            if (!entry.isUpgrading) continue;

//            float remaining = entry.upgradeEndTime - Time.time;

//            // Only drive the progress bar UI for the currently selected entry
//            if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
//            {
//                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//                float progress = 1f - Mathf.Clamp01(remaining / total);

//                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//                if (upgradeTimerText != null) upgradeTimerText.text = $"{Mathf.Max(0f, remaining):F1}s";
//            }

//            if (remaining <= 0f)
//                CompleteUpgrade(entry);
//        }
//    }

//    private void CompleteUpgrade(CannonInventoryEntry entry)
//    {
//        entry.upgradeCount++;
//        entry.isUpgrading = false;

//        Debug.Log($"[CannonPanel] Upgrade complete for '{entry.data.cannonName}' " +
//                  $"(id={entry.inventoryId}). " +
//                  $"Level now {entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}");

//        // If this is the currently displayed entry, refresh the UI
//        if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
//        {
//            HideProgressBar();
//            ShowInventoryDetails(entry);
//            RefreshUpdateButton(entry);
//        }

//        RefreshInventoryCardBadges();
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // DRAG-DROP CALLBACKS (called by CannonDragHandler / CannonSlot)
//    // ═════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by CannonDragHandler.OnEndDrag() after a successful castle placement.
//    /// Removes the placed cannon's card from the inventory list.
//    /// </summary>
//    public void OnCannonPlacedOnCastle(CannonInventoryEntry entry)
//    {
//        if (_mode == PanelMode.Inventory)
//        {
//            // If the placed cannon was selected, clear the selection
//            if (_selectedInventoryId == entry.inventoryId)
//            {
//                _selectedInventoryId = -1;
//                ClearDetails();
//                HideProgressBar();
//            }
//            PopulateInventoryCards();
//            // Auto-select first remaining card
//            if (_spawnedInventoryCards.Count > 0)
//            {
//                _spawnedInventoryCards[0].SetSelected(true);
//                _selectedInventoryId = _spawnedInventoryCards[0].InventoryId;
//                CannonInventoryEntry first = SelectedEntry;
//                if (first != null) { ShowInventoryDetails(first); RefreshUpdateButton(first); }
//            }
//        }
//    }

//    /// <summary>
//    /// Called by CannonSlot.RemoveCannon() when the player removes a cannon
//    /// from the castle, sending it back to the inventory.
//    /// </summary>
//    public void OnSlotRemoved()
//    {
//        if (_mode == PanelMode.Inventory)
//            PopulateInventoryCards();
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // BUTTON HELPERS
//    // ═════════════════════════════════════════════════════════════════════════

//    private void RefreshBuyButton()
//    {
//        if (buyButton == null) return;
//        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
//        buyButton.interactable = canAfford;
//        if (buyButtonText != null)
//            buyButtonText.text = _selectedBuyData != null
//                ? $"Buy  ({_selectedBuyData.cost})"
//                : "Buy";
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // PROGRESS BAR HELPERS
//    // ═════════════════════════════════════════════════════════════════════════

//    private void ShowProgressBar()
//    {
//        upgradeProgressBarBG?.SetActive(true);
//        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//    }

//    private void HideProgressBar()
//    {
//        upgradeProgressBarBG?.SetActive(false);
//        if (upgradeTimerText != null) upgradeTimerText.text = "";
//    }

//    /// <summary>
//    /// Called when switching inventory cards — shows or hides the progress bar
//    /// depending on whether the newly selected cannon is currently upgrading.
//    /// </summary>
//    private void RefreshProgressBarForSelected()
//    {
//        CannonInventoryEntry entry = SelectedEntry;
//        if (entry == null) { HideProgressBar(); return; }

//        if (entry.isUpgrading)
//        {
//            ShowProgressBar();
//            // Snap fill to current progress immediately
//            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//            float remaining = entry.upgradeEndTime - Time.time;
//            float progress = 1f - Mathf.Clamp01(remaining / total);
//            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//            if (upgradeTimerText != null) upgradeTimerText.text = $"{Mathf.Max(0f, remaining):F1}s";
//        }
//        else
//        {
//            HideProgressBar();
//        }
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // COIN
//    // ═════════════════════════════════════════════════════════════════════════

//    private void RefreshCoinText()
//    {
//        if (coinText != null) coinText.text = _gold.ToString();
//    }

//    /// <summary>Public so other systems (rewards, quests) can add gold.</summary>
//    public void AddGold(int amount)
//    {
//        _gold += amount;
//        RefreshCoinText();
//        if (_mode == PanelMode.Buy) RefreshBuyButton();
//    }

//    public int GetGold() => _gold;

//    // ═════════════════════════════════════════════════════════════════════════
//    // WARNING / FEEDBACK
//    // ═════════════════════════════════════════════════════════════════════════

//    private void ShowWarning(string message)
//    {
//        if (warningText == null) return;
//        warningText.text = message;
//        CancelInvoke(nameof(ClearWarning));
//        Invoke(nameof(ClearWarning), 2.5f);
//    }

//    private void ClearWarning()
//    {
//        if (warningText != null) warningText.text = "";
//    }

//    // ═════════════════════════════════════════════════════════════════════════
//    // QUERIES (public utility)
//    // ═════════════════════════════════════════════════════════════════════════

//    /// <summary>How many copies of this cannon type the player currently owns.</summary>
//    public int CountOwned(CannonData data)
//    {
//        int count = 0;
//        foreach (CannonInventoryEntry e in _inventory)
//            if (e.data == data) count++;
//        return count;
//    }

//    /// <summary>Returns the full inventory list (read-only for external systems).</summary>
//    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;
//}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonPanelManager
///
/// Single manager that owns everything: inventory, gold, buy flow,
/// upgrade flow, and UI state. No separate "CannonArea" is needed.
///
/// TWO MODES ───────────────────────────────────────────────────────────
///   Buy Mode       : shows 3 fixed cannon-type cards. Player selects one
///                    and clicks Buy to purchase a copy with coins.
///   Inventory Mode : shows one dynamic card per owned cannon copy —
///                    including cannons already placed on the castle.
///                    • Unplaced cannon → Equip button + drag-drop support
///                    • Placed cannon   → Unequip button
///                    Player can also select a card and click Upgrade (max 3×).
///
/// EQUIP / UNEQUIP FLOW ────────────────────────────────────────────────
///   EquipCannon(id)   — places the cannon on the first free CannonSlot.
///                       Assign all castle slots to the castleSlots array.
///   UnequipCannon(id) — removes the cannon from its slot, returning it
///                       to the inventory with upgrade progress intact.
///
/// DETAILS SECTION (shared, always visible) ────────────────────────────
///   Shows the selected cannon's: Name, Cost, Range.
///   In Inventory mode Range shows the current upgraded value.
///
/// UPGRADES ────────────────────────────────────────────────────────────
///   Each owned copy has its own upgrade counter (0–3).
///   Upgrading cannon A never affects cannon B.
///   Upgrade timers tick in Update() even while the panel is closed.
///
/// HIERARCHY GUIDE ─────────────────────────────────────────────────────
///   CannonPanel
///   ├── TabBar
///   │   ├── TabBuyButton
///   │   └── TabInventoryButton
///   ├── BuyView
///   │   ├── BuyCard_0  (CannonCard)
///   │   ├── BuyCard_1  (CannonCard)
///   │   └── BuyCard_2  (CannonCard)
///   ├── InventoryView
///   │   └── ScrollRect → InventoryCardContainer
///   ├── DetailsSection
///   │   ├── DetailsNameText
///   │   ├── DetailsCostText
///   │   └── DetailsRangeText
///   ├── HUDSection
///   │   ├── HealthBar (Image Filled) + HealthText
///   │   ├── DamageBar                + DamageText
///   │   └── RangeBar                 + RangeText
///   ├── UpgradeProgressBarBackground
///   │   └── UpgradeProgressBar  (Image Filled Horizontal Left)
///   ├── UpgradeTimerText
///   ├── BuyButton
///   ├── UpdateButton
///   ├── CoinText
///   └── WarningText
/// </summary>
public class CannonPanelManager : MonoBehaviour
{
    public static CannonPanelManager Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Cannon Types (3 entries: Iron, Bronze, Golden)")]
    [SerializeField] private CannonData[] cannonTypes;

    // ─── Castle Slots ─────────────────────────────────────────────────────────
    [Header("Castle Slots (assign all CannonSlot objects on the castle)")]
    [Tooltip("Used by EquipCannon() to find the first available slot.")]
    [SerializeField] private CannonSlot[] castleSlots;

    // ─── Buy Mode ─────────────────────────────────────────────────────────────
    [Header("Buy Mode")]
    [SerializeField] private GameObject buyView;
    [Tooltip("3 pre-placed CannonCard components in BuyView, same order as cannonTypes")]
    [SerializeField] private CannonCard[] buyCards;

    // ─── Inventory Mode ───────────────────────────────────────────────────────
    [Header("Inventory Mode")]
    [SerializeField] private GameObject inventoryView;
    [Tooltip("Prefab used to spawn one card per owned cannon copy")]
    [SerializeField] private CannonCard inventoryCardPrefab;
    [Tooltip("Content transform of the ScrollRect")]
    [SerializeField] private Transform inventoryCardContainer;

    // ─── Details Section ──────────────────────────────────────────────────────
    [Header("Details Section (shared)")]
    [SerializeField] private TextMeshProUGUI detailsNameText;
    [SerializeField] private TextMeshProUGUI detailsCostText;
    [SerializeField] private TextMeshProUGUI detailsRangeText;

    // ─── HUD Stat Bars ────────────────────────────────────────────────────────
    [Header("HUD Stat Bars (Image Type = Filled, Horizontal, Fill Origin = Left)")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image damageBar;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Image rangeBar;
    [SerializeField] private TextMeshProUGUI rangeText;

    [Header("Max stat values used for bar fill ratio")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float maxDamage = 150f;
    [SerializeField] private float maxRange = 200f;

    // ─── Upgrade UI ───────────────────────────────────────────────────────────
    [Header("Upgrade UI")]
    [Tooltip("Parent of the progress bar — shown/hidden as upgrade starts/ends")]
    [SerializeField] private GameObject upgradeProgressBarBG;
    [Tooltip("Image (Filled, Horizontal, Left) — fillAmount driven in Update()")]
    [SerializeField] private Image upgradeProgressBar;
    [Tooltip("Shows '7.3s' countdown while upgrading")]
    [SerializeField] private TextMeshProUGUI upgradeTimerText;

    // ─── Buttons ──────────────────────────────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button updateButton;
    [SerializeField] private TextMeshProUGUI updateButtonText;   // "Upgrade (1/3)" / "MAX"

    [Header("Tab Buttons")]
    [SerializeField] private Button tabBuyButton;
    [SerializeField] private Button tabInventoryButton;

    // ─── Coin ─────────────────────────────────────────────────────────────────
    [Header("Coin")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private int startingGold = 300;

    // ─── Warning / Feedback ───────────────────────────────────────────────────
    [Header("Warning / Feedback Text")]
    [SerializeField] private TextMeshProUGUI warningText;

    // ═════════════════════════════════════════════════════════════════════════
    // PRIVATE STATE
    // ═════════════════════════════════════════════════════════════════════════

    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
    private int _nextId = 0;
    private int _gold;

    private enum PanelMode { Buy, Inventory }
    private PanelMode _mode = PanelMode.Buy;

    // ── Buy mode selection ────────────────────────────────────────────────────
    private CannonData _selectedBuyData;
    private CannonCard _selectedBuyCard;

    // ── Inventory mode selection ──────────────────────────────────────────────
    private int _selectedInventoryId = -1;

    private CannonInventoryEntry SelectedEntry =>
        _inventory.Find(e => e.inventoryId == _selectedInventoryId);

    // Tracks the currently displayed inventory cards for badge refreshing
    private readonly List<CannonCard> _spawnedInventoryCards = new List<CannonCard>();

    // ═════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _gold = startingGold;
    }

    private void Start()
    {
        // Tab buttons
        tabBuyButton?.onClick.AddListener(OpenBuyMode);
        tabInventoryButton?.onClick.AddListener(OpenInventoryMode);

        // Action buttons
        buyButton?.onClick.AddListener(OnBuyClicked);
        updateButton?.onClick.AddListener(OnUpdateClicked);

        // Initialise the 3 static buy cards
        if (buyCards != null)
            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
                buyCards[i].SetupBuyCard(cannonTypes[i]);

        RefreshCoinText();
        HideProgressBar();

        // Open buy mode on start
        OpenBuyMode();
    }

    private void Update()
    {
        TickAllUpgrades();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PUBLIC ENTRY POINT — called by GameManager.OpenCannonPanel()
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by GameManager every time the player opens the Cannon panel
    /// (e.g. via the "Add Cannon" button). Resets to Buy mode so the player
    /// always lands on the purchase screen first.
    /// </summary>
    public void OnPanelOpened()
    {
        OpenBuyMode();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MODE SWITCHING
    // ═════════════════════════════════════════════════════════════════════════

    public void OpenBuyMode()
    {
        _mode = PanelMode.Buy;
        buyView?.SetActive(true);
        inventoryView?.SetActive(false);

        buyButton?.gameObject.SetActive(true);
        updateButton?.gameObject.SetActive(false);

        HideProgressBar();

        // FIXED: guard both arrays before indexing.
        // Old code:  if (cannonTypes != null && cannonTypes.Length > 0)
        //                SelectBuyCard(buyCards[0], cannonTypes[0]);   ← crash if buyCards null/empty
        if (cannonTypes != null && cannonTypes.Length > 0 &&
            buyCards != null && buyCards.Length > 0)
        {
            SelectBuyCard(buyCards[0], cannonTypes[0]);
        }
        else if (cannonTypes == null || cannonTypes.Length == 0)
        {
            Debug.LogWarning("[CannonPanelManager] cannonTypes array is empty — " +
                             "assign at least one CannonData ScriptableObject in the Inspector.");
        }
        else if (buyCards == null || buyCards.Length == 0)
        {
            Debug.LogWarning("[CannonPanelManager] buyCards array is empty — " +
                             "drag the 3 BuyCard CannonCard components into the Inspector.");
        }

        ClearWarning();
    }

    private void OnValidate()
    {
        if (cannonTypes == null || cannonTypes.Length == 0)
            Debug.LogWarning("[CannonPanelManager] cannonTypes is empty.", this);

        if (buyCards == null || buyCards.Length == 0)
            Debug.LogWarning("[CannonPanelManager] buyCards is empty — " +
                             "drag all 3 BuyCard CannonCard components here.", this);

        if (buyView == null)
            Debug.LogWarning("[CannonPanelManager] buyView is not assigned.", this);

        if (inventoryView == null)
            Debug.LogWarning("[CannonPanelManager] inventoryView is not assigned.", this);

        if (inventoryCardPrefab == null)
            Debug.LogWarning("[CannonPanelManager] inventoryCardPrefab is not assigned.", this);

        if (inventoryCardContainer == null)
            Debug.LogWarning("[CannonPanelManager] inventoryCardContainer is not assigned.", this);

        if (buyButton == null)
            Debug.LogWarning("[CannonPanelManager] buyButton is not assigned.", this);

        if (updateButton == null)
            Debug.LogWarning("[CannonPanelManager] updateButton is not assigned.", this);
    }

    public void OpenInventoryMode()
    {
        _mode = PanelMode.Inventory;
        buyView?.SetActive(false);
        inventoryView?.SetActive(true);

        buyButton?.gameObject.SetActive(false);
        updateButton?.gameObject.SetActive(true);

        // Refresh without wiping the current selection
        RefreshInventoryUI();
        ClearWarning();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CARD SELECTION (called by CannonCard.OnClick)
    // ═════════════════════════════════════════════════════════════════════════

    public void OnCardSelected(CannonCard card)
    {
        if (_mode == PanelMode.Buy)
        {
            SelectBuyCard(card, card.Data);
        }
        else
        {
            // Deselect all inventory cards
            foreach (CannonCard c in _spawnedInventoryCards)
                c.SetSelected(false);
            card.SetSelected(true);

            _selectedInventoryId = card.InventoryId;
            CannonInventoryEntry entry = SelectedEntry;
            if (entry == null) return;

            ShowInventoryDetails(entry);
            RefreshUpdateButton(entry);
            RefreshProgressBarForSelected();
        }
        ClearWarning();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // EQUIP / UNEQUIP (called by CannonCard equip / unequip buttons)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Places the cannon with the given inventoryId onto the first free
    /// castle slot. Shows a warning if all slots are occupied.
    /// Called by the Equip button on each inventory card.
    /// </summary>
    public void EquipCannon(int inventoryId)
    {
        CannonInventoryEntry entry = _inventory.Find(e => e.inventoryId == inventoryId);
        if (entry == null || entry.isPlacedOnCastle) return;

        // Find the first free slot
        if (castleSlots != null)
        {
            foreach (CannonSlot slot in castleSlots)
            {
                if (slot == null || slot.IsOccupied) continue;

                slot.PlaceCannon(entry);

                // Keep this cannon selected so the player sees it flip to Unequip
                _selectedInventoryId = inventoryId;

                if (_mode == PanelMode.Inventory)
                    RefreshInventoryUI();

                Debug.Log($"[CannonPanel] Equipped '{entry.data.cannonName}' " +
                          $"(id={inventoryId}) via button.");
                return;
            }
        }

        ShowWarning("No free cannon slots on the castle!");
    }

    /// <summary>
    /// Removes the cannon with the given inventoryId from its castle slot,
    /// returning it to the inventory with upgrade progress intact.
    /// Called by the Unequip button on each inventory card.
    /// </summary>
    public void UnequipCannon(int inventoryId)
    {
        CannonInventoryEntry entry = _inventory.Find(e => e.inventoryId == inventoryId);
        if (entry == null || !entry.isPlacedOnCastle || entry.occupiedSlot == null) return;

        // Keep this cannon selected so the player sees it flip back to Equip
        _selectedInventoryId = inventoryId;

        // RemoveCannon() clears isPlacedOnCastle / occupiedSlot and fires
        // CannonPanelManager.Instance.OnSlotRemoved() → RefreshInventoryUI().
        entry.occupiedSlot.RemoveCannon();

        Debug.Log($"[CannonPanel] Unequipped '{entry.data.cannonName}' " +
                  $"(id={inventoryId}) via button.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUY FLOW
    // ═════════════════════════════════════════════════════════════════════════

    private void SelectBuyCard(CannonCard card, CannonData data)
    {
        // Clear previous selection highlight
        if (_selectedBuyCard != null) _selectedBuyCard.SetSelected(false);

        _selectedBuyData = data;
        _selectedBuyCard = card;
        card?.SetSelected(true);

        ShowBuyDetails(data);
        RefreshBuyButton();
    }

    private void OnBuyClicked()
    {
        if (_selectedBuyData == null)
        {
            ShowWarning("Select a cannon first.");
            return;
        }
        if (_gold < _selectedBuyData.cost)
        {
            ShowWarning("Not enough coins!");
            return;
        }

        // Deduct cost
        _gold -= _selectedBuyData.cost;
        RefreshCoinText();

        // Create inventory entry
        var entry = new CannonInventoryEntry
        {
            data = _selectedBuyData,
            inventoryId = _nextId++,
            upgradeCount = 0,
            isUpgrading = false,
            isPlacedOnCastle = false,
            occupiedSlot = null
        };
        _inventory.Add(entry);

        ShowWarning($"Purchased {_selectedBuyData.cannonName}!");
        RefreshBuyButton();

        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' " +
                  $"(id={entry.inventoryId}). Total owned: {CountOwned(_selectedBuyData)}x.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DETAILS SECTION
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Buy mode — shows base stats from the ScriptableObject.</summary>
    private void ShowBuyDetails(CannonData data)
    {
        if (detailsNameText != null) detailsNameText.text = data.cannonName;
        if (detailsCostText != null) detailsCostText.text = $"Cost: {data.cost}";
        if (detailsRangeText != null) detailsRangeText.text = $"Range: {data.range:F0}";

        RefreshHUDBars(data.health, data.damage, data.range);
    }

    /// <summary>Inventory mode — shows current upgraded stats.</summary>
    private void ShowInventoryDetails(CannonInventoryEntry entry)
    {
        if (detailsNameText != null) detailsNameText.text = entry.data.cannonName;
        if (detailsCostText != null) detailsCostText.text = $"Cost: {entry.data.cost}";
        if (detailsRangeText != null) detailsRangeText.text = $"Range: {entry.CurrentRange:F0}";

        RefreshHUDBars(entry.CurrentHealth, entry.CurrentDamage, entry.CurrentRange);
    }

    private void ClearDetails()
    {
        if (detailsNameText != null) detailsNameText.text = "—";
        if (detailsCostText != null) detailsCostText.text = "";
        if (detailsRangeText != null) detailsRangeText.text = "";
        RefreshHUDBars(0f, 0f, 0f);
    }

    private void RefreshHUDBars(float health, float damage, float range)
    {
        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(health / maxHealth);
        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(damage / maxDamage);
        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(range / maxRange);

        if (healthText != null) healthText.text = $"{health:F0}";
        if (damageText != null) damageText.text = $"{damage:F0}";
        if (rangeText != null) rangeText.text = $"{range:F0}";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // INVENTORY CARDS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Destroys all spawned inventory cards and recreates them from scratch.
    /// ALL owned cannons are shown — placed cannons display the Unequip button;
    /// unplaced cannons display the Equip button and support drag-drop.
    /// </summary>
    private void PopulateInventoryCards()
    {
        _spawnedInventoryCards.Clear();
        if (inventoryCardContainer != null)
            foreach (Transform child in inventoryCardContainer)
                Destroy(child.gameObject);

        if (inventoryCardPrefab == null) return;

        foreach (CannonInventoryEntry entry in _inventory)
        {
            CannonCard card = Instantiate(inventoryCardPrefab, inventoryCardContainer);
            card.SetupInventoryCard(entry);

            // Restore selection highlight if this was the previously selected cannon
            if (entry.inventoryId == _selectedInventoryId)
                card.SetSelected(true);

            _spawnedInventoryCards.Add(card);
        }
    }

    private void RefreshInventoryCardBadges()
    {
        foreach (CannonCard card in _spawnedInventoryCards)
        {
            if (card == null) continue;
            CannonInventoryEntry entry = _inventory.Find(e => e.inventoryId == card.InventoryId);
            if (entry != null) card.RefreshUpgradeBadge(entry);
        }
    }

    /// <summary>
    /// Central helper: repopulates cards and restores the selection/details UI.
    /// Call this whenever the inventory list or a cannon's placement state changes.
    /// </summary>
    private void RefreshInventoryUI()
    {
        PopulateInventoryCards();

        CannonInventoryEntry current = SelectedEntry;

        if (current != null)
        {
            // Selection is still valid — refresh details for the same cannon
            ShowInventoryDetails(current);
            RefreshUpdateButton(current);
            RefreshProgressBarForSelected();
        }
        else if (_spawnedInventoryCards.Count > 0)
        {
            // Previous selection gone — auto-select the first card
            _selectedInventoryId = _spawnedInventoryCards[0].InventoryId;
            _spawnedInventoryCards[0].SetSelected(true);

            CannonInventoryEntry first = SelectedEntry;
            if (first != null)
            {
                ShowInventoryDetails(first);
                RefreshUpdateButton(first);
                RefreshProgressBarForSelected();
            }
        }
        else
        {
            // No cannons owned
            _selectedInventoryId = -1;
            ClearDetails();
            HideProgressBar();
            if (updateButton != null)
            {
                updateButton.interactable = false;
                if (updateButtonText != null) updateButtonText.text = "No Cannons";
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UPGRADE FLOW
    // ═════════════════════════════════════════════════════════════════════════

    private void OnUpdateClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;

        if (entry == null)
        {
            ShowWarning("Select a cannon first.");
            return;
        }
        if (entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES)
        {
            ShowWarning("Already at maximum level!");
            return;
        }
        if (entry.isUpgrading)
        {
            ShowWarning("Upgrade already in progress.");
            return;
        }

        // Start the upgrade timer
        entry.isUpgrading = true;
        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

        RefreshUpdateButton(entry);
        ShowProgressBar();
        RefreshInventoryCardBadges();

        Debug.Log($"[CannonPanel] Upgrade started for '{entry.data.cannonName}' " +
                  $"(id={entry.inventoryId}, upgrade " +
                  $"{entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES})");
    }

    private void RefreshUpdateButton(CannonInventoryEntry entry)
    {
        if (updateButton == null) return;

        if (entry == null)
        {
            updateButton.interactable = false;
            if (updateButtonText != null) updateButtonText.text = "No Cannons";
            return;
        }

        if (entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES)
        {
            if (updateButtonText != null) updateButtonText.text = "MAX";
            updateButton.interactable = false;
        }
        else if (entry.isUpgrading)
        {
            if (updateButtonText != null) updateButtonText.text = "Upgrading...";
            updateButton.interactable = false;
        }
        else
        {
            if (updateButtonText != null)
                updateButtonText.text = $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
            updateButton.interactable = true;
        }
    }

    // ─── Upgrade tick (called every frame in Update) ───────────────────────

    private void TickAllUpgrades()
    {
        // All entries tick in the background even while the panel is closed
        foreach (CannonInventoryEntry entry in _inventory)
        {
            if (!entry.isUpgrading) continue;

            float remaining = entry.upgradeEndTime - Time.time;

            // Only drive the progress bar UI for the currently selected entry
            if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
            {
                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
                float progress = 1f - Mathf.Clamp01(remaining / total);

                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
                if (upgradeTimerText != null) upgradeTimerText.text = $"{Mathf.Max(0f, remaining):F1}s";
            }

            if (remaining <= 0f)
                CompleteUpgrade(entry);
        }
    }

    private void CompleteUpgrade(CannonInventoryEntry entry)
    {
        entry.upgradeCount++;
        entry.isUpgrading = false;

        Debug.Log($"[CannonPanel] Upgrade complete for '{entry.data.cannonName}' " +
                  $"(id={entry.inventoryId}). " +
                  $"Level now {entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}");

        // If this is the currently displayed entry, refresh the UI
        if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
        {
            HideProgressBar();
            ShowInventoryDetails(entry);
            RefreshUpdateButton(entry);
        }

        RefreshInventoryCardBadges();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DRAG-DROP CALLBACKS (called by CannonDragHandler / CannonSlot)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by CannonDragHandler.OnEndDrag() after a successful castle
    /// placement via drag-and-drop. Refreshes the inventory UI so the
    /// card flips from Equip to Unequip without disappearing from the list.
    /// </summary>
    public void OnCannonPlacedOnCastle(CannonInventoryEntry entry)
    {
        if (_mode != PanelMode.Inventory) return;

        // Keep the placed cannon selected so the player can see it flip to Unequip
        _selectedInventoryId = entry.inventoryId;
        RefreshInventoryUI();
    }

    /// <summary>
    /// Called by CannonSlot.RemoveCannon() when the player uses the slot's
    /// own remove button (the X on the castle). Refreshes the inventory list
    /// so the cannon's card flips back from Unequip to Equip.
    /// </summary>
    public void OnSlotRemoved()
    {
        if (_mode == PanelMode.Inventory)
            RefreshInventoryUI();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUTTON HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshBuyButton()
    {
        if (buyButton == null) return;
        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
        buyButton.interactable = canAfford;
        if (buyButtonText != null)
            buyButtonText.text = _selectedBuyData != null
                ? $"Buy  ({_selectedBuyData.cost})"
                : "Buy";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PROGRESS BAR HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowProgressBar()
    {
        upgradeProgressBarBG?.SetActive(true);
        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
    }

    private void HideProgressBar()
    {
        upgradeProgressBarBG?.SetActive(false);
        if (upgradeTimerText != null) upgradeTimerText.text = "";
    }

    /// <summary>
    /// Called when switching inventory cards — shows or hides the progress bar
    /// depending on whether the newly selected cannon is currently upgrading.
    /// </summary>
    private void RefreshProgressBarForSelected()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { HideProgressBar(); return; }

        if (entry.isUpgrading)
        {
            ShowProgressBar();
            // Snap fill to current progress immediately
            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
            float remaining = entry.upgradeEndTime - Time.time;
            float progress = 1f - Mathf.Clamp01(remaining / total);
            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
            if (upgradeTimerText != null) upgradeTimerText.text = $"{Mathf.Max(0f, remaining):F1}s";
        }
        else
        {
            HideProgressBar();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COIN
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshCoinText()
    {
        if (coinText != null) coinText.text = _gold.ToString();
    }

    /// <summary>Public so other systems (rewards, quests) can add gold.</summary>
    public void AddGold(int amount)
    {
        _gold += amount;
        RefreshCoinText();
        if (_mode == PanelMode.Buy) RefreshBuyButton();
    }

    public int GetGold() => _gold;

    // ═════════════════════════════════════════════════════════════════════════
    // WARNING / FEEDBACK
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.text = message;
        CancelInvoke(nameof(ClearWarning));
        Invoke(nameof(ClearWarning), 2.5f);
    }

    private void ClearWarning()
    {
        if (warningText != null) warningText.text = "";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // QUERIES (public utility)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>How many copies of this cannon type the player currently owns.</summary>
    public int CountOwned(CannonData data)
    {
        int count = 0;
        foreach (CannonInventoryEntry e in _inventory)
            if (e.data == data) count++;
        return count;
    }

    /// <summary>Returns the full inventory list (read-only for external systems).</summary>
    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;
}