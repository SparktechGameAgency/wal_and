//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// CANNON PANEL — CannonPanelManager
/////////
///////// The single script that drives the entire Cannon Panel.
///////// Attach to the root CannonPanle GameObject (matches screenshot hierarchy).
/////////
///////// ════════════════════════════════════════════════════════════════
///////// FLOW
///////// ════════════════════════════════════════════════════════════════
/////////
/////////  Village Panel
/////////    └── CannonSlot.AddButton clicked
/////////          └── OpenPanel(callingSlot)  → panel opens in BUY MODE
/////////
/////////  BUY MODE
/////////    • Shows 3 cannon cards, all locked initially
/////////    • Select a card → details (Name, Cost, Range) + HUD bars update
/////////    • Click Buy → deducts gold, creates CannonInventoryEntry, lock removed on that card
/////////    • Click "Inventory" tab → switches to INVENTORY MODE
/////////
/////////  INVENTORY MODE
/////////    • Spawns one card per owned cannon (not equipped ones can also be shown — configurable)
/////////    • Select a card → details update with CURRENT upgraded stats + level badge + timer
/////////    • Equip   → calls callingSlot.Equip(entry); panel closes
/////////    • Unequip → calls entry.equippedSlot.Unequip()
/////////    • Upgrade → starts timed upgrade on selected entry
/////////    • Click "Buy" tab → switches back to BUY MODE
/////////
/////////  BackButton → closes panel, returns to Village
/////////
///////// ════════════════════════════════════════════════════════════════
///////// HIERARCHY (CannonPanle in screenshot)
///////// ════════════════════════════════════════════════════════════════
/////////
/////////  CannonPanle
/////////  ├── bg
/////////  ├── BackButton
/////////  ├── BuyTabButton          ← "Buy" tab
/////////  ├── InventoryTabButton    ← "Inventory" tab (shown as "Inventory" text in screenshot)
/////////  ├── CardGrid              ← parent of the 3 buy-mode cards
/////////  │   ├── CannonCard_0  (CannonCard)
/////////  │   ├── CannonCard_1  (CannonCard)
/////////  │   └── CannonCard_2  (CannonCard)
/////////  ├── InventoryScrollContent  ← dynamic inventory cards spawned here
/////////  ├── Panel                 ← right-side details panel
/////////  │   ├── Level             ← "LEVEL 1" badge + upgrade timer
/////////  │   │   └── Text (TMP)   ← levelText
/////////  │   ├── TimerText (TMP)   ← "00:00"
/////////  │   ├── Cannon1           ← large preview Image
/////////  │   ├── NameText (TMP)    ← "Name: Iron Field"
/////////  │   ├── CostText (TMP)    ← "Cost: 100"
/////////  │   ├── RangeText (TMP)   ← "Range: 40m"
/////////  │   ├── HealthBar (Image Filled) + HealthText (TMP)
/////////  │   ├── AbilityBar        + AbilityText
/////////  │   ├── DamageBar         + DamageText
/////////  │   ├── BuyButton
/////////  │   ├── EquipButton       (inventory mode)
/////////  │   ├── UnequipButton     (inventory mode)
/////////  │   └── UpgradeButton     (inventory mode)
/////////  └── CoinText (TMP)        ← coin amount top-right (reads from GameManager)
/////////
///////// </summary>
//////public class CannonPanelManager : MonoBehaviour
//////{
//////    public static CannonPanelManager Instance { get; private set; }

//////    // ════════════════════════════════════════════════════════════════
//////    // INSPECTOR FIELDS
//////    // ════════════════════════════════════════════════════════════════

//////    [Header("Cannon Types — assign 3 CannonData assets")]
//////    [SerializeField] private CannonData[] cannonTypes;   // 3 entries

//////    // ── Tab buttons ───────────────────────────────────────────────────────────
//////    [Header("Tab Buttons")]
//////    [SerializeField] private Button buyTabButton;
//////    [SerializeField] private Button inventoryTabButton;
//////    [SerializeField] private Button backButton;

//////    // ── Buy Mode ──────────────────────────────────────────────────────────────
//////    [Header("Buy Mode — Card Grid")]
//////    [Tooltip("Parent GameObject containing the 3 fixed CannonCard objects")]
//////    [SerializeField] private GameObject cardGridRoot;
//////    [Tooltip("3 pre-placed CannonCard components inside CardGrid, same order as cannonTypes")]
//////    [SerializeField] private CannonCard[] buyCards;       // 3 entries

//////    // ── Inventory Mode ────────────────────────────────────────────────────────
//////    [Header("Inventory Mode")]
//////    [Tooltip("Content Transform of the ScrollRect — dynamic cards spawn here")]
//////    [SerializeField] private Transform inventoryScrollContent;
//////    [Tooltip("CannonCard prefab spawned for each owned cannon")]
//////    [SerializeField] private CannonCard inventoryCardPrefab;

//////    // ── Details Panel (right side) ────────────────────────────────────────────
//////    [Header("Details Panel")]
//////    [SerializeField] private Image previewImage;    // Cannon1 in hierarchy
//////    [SerializeField] private TextMeshProUGUI levelText;       // "LEVEL 1"
//////    [SerializeField] private TextMeshProUGUI timerText;       // "00:00"
//////    [SerializeField] private TextMeshProUGUI nameText;        // "Name: Iron Field"
//////    [SerializeField] private TextMeshProUGUI costText;        // "Cost: 100"
//////    [SerializeField] private TextMeshProUGUI rangeText;       // "Range: 40m"

//////    // ── HUD bars (screenshot shows HEALTH / ABILITY / DAMAGE) ────────────────
//////    [Header("HUD Stat Bars (Image Type = Filled, Horizontal, Fill Origin = Left)")]
//////    [SerializeField] private Image healthBar;
//////    [SerializeField] private TextMeshProUGUI healthValueText;
//////    [SerializeField] private Image abilityBar;
//////    [SerializeField] private TextMeshProUGUI abilityValueText;
//////    [SerializeField] private Image damageBar;
//////    [SerializeField] private TextMeshProUGUI damageValueText;

//////    [Header("Max values for bar fill ratio — tune per game balance")]
//////    [SerializeField] private float maxHealth = 200f;
//////    [SerializeField] private float maxAbility = 150f;
//////    [SerializeField] private float maxDamage = 100f;

//////    // ── Upgrade progress bar ──────────────────────────────────────────────────
//////    [Header("Upgrade Progress (shown while upgrading)")]
//////    [SerializeField] private GameObject upgradeProgressBG;
//////    [SerializeField] private Image upgradeProgressBar;

//////    // ── Action buttons ────────────────────────────────────────────────────────
//////    [Header("Action Buttons")]
//////    [SerializeField] private Button buyButton;
//////    [SerializeField] private TextMeshProUGUI buyButtonText;
//////    [SerializeField] private Button equipButton;
//////    [SerializeField] private Button unequipButton;
//////    [SerializeField] private Button upgradeButton;
//////    [SerializeField] private TextMeshProUGUI upgradeButtonText;  // "Upgrade (1/3)" / "MAX"

//////    // ── Coin / Warning ────────────────────────────────────────────────────────
//////    [Header("Coin & Warning")]
//////    [SerializeField] private TextMeshProUGUI coinText;
//////    [SerializeField] private TextMeshProUGUI warningText;

//////    // ════════════════════════════════════════════════════════════════
//////    // PRIVATE STATE
//////    // ════════════════════════════════════════════════════════════════

//////    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//////    private int _nextId = 0;

//////    // Track which cannon types have been purchased at least once (for lock removal)
//////    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

//////    private enum PanelMode { Buy, Inventory }
//////    private PanelMode _mode = PanelMode.Buy;

//////    // The slot that opened this panel (set by OpenPanel)
//////    private CannonSlot _callingSlot;

//////    // Buy mode selection
//////    private CannonData _selectedBuyData;
//////    private CannonCard _selectedBuyCard;

//////    // Inventory mode selection
//////    private int _selectedInventoryId = -1;
//////    private CannonInventoryEntry SelectedEntry =>
//////        _inventory.Find(e => e.inventoryId == _selectedInventoryId);

//////    // Spawned inventory cards (for badge refresh)
//////    private readonly List<CannonCard> _spawnedInventoryCards = new List<CannonCard>();

//////    // ════════════════════════════════════════════════════════════════
//////    // UNITY LIFECYCLE
//////    // ════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;
//////    }

//////    private void Start()
//////    {
//////        // Wire tab buttons
//////        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
//////        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
//////        backButton?.onClick.AddListener(ClosePanel);

//////        // Wire action buttons
//////        buyButton?.onClick.AddListener(OnBuyClicked);
//////        equipButton?.onClick.AddListener(OnEquipClicked);
//////        unequipButton?.onClick.AddListener(OnUnequipClicked);
//////        upgradeButton?.onClick.AddListener(OnUpgradeClicked);

//////        // Subscribe to GameManager gold changes so coin display stays in sync
//////        GameManager.OnGoldChanged += OnGoldChanged;

//////        // Setup the 3 fixed buy cards (all locked at start)
//////        if (buyCards != null)
//////            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
//////                buyCards[i].SetupBuyCard(cannonTypes[i], locked: true);

//////        // Panel starts hidden — it is shown by OpenPanel()
//////        gameObject.SetActive(false);
//////    }

//////    private void OnDestroy()
//////    {
//////        GameManager.OnGoldChanged -= OnGoldChanged;
//////    }

//////    private void Update()
//////    {
//////        TickAllUpgrades();
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // PANEL OPEN / CLOSE
//////    // ════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by CannonSlot.AddButton.
//////    /// Opens the panel in Buy mode, remembers which slot triggered the open.
//////    /// </summary>
//////    public void OpenPanel(CannonSlot callingSlot)
//////    {
//////        _callingSlot = callingSlot;
//////        gameObject.SetActive(true);
//////        RefreshCoinText();
//////        SwitchToBuyMode();
//////    }

//////    private void ClosePanel()
//////    {
//////        ClearWarning();
//////        gameObject.SetActive(false);
//////        // Return to village via GameManager
//////        GameManager.Instance?.CloseCurrentPanel();
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // MODE SWITCHING
//////    // ════════════════════════════════════════════════════════════════

//////    private void SwitchToBuyMode()
//////    {
//////        _mode = PanelMode.Buy;

//////        // Show card grid, hide inventory scroll
//////        if (cardGridRoot != null) cardGridRoot.SetActive(true);
//////        if (inventoryScrollContent != null) inventoryScrollContent.gameObject.SetActive(false);

//////        // Show Buy button, hide inventory action buttons
//////        SetButtonsForMode(buy: true);

//////        // Auto-select first card
//////        if (buyCards != null && buyCards.Length > 0 && cannonTypes.Length > 0)
//////            SelectBuyCard(buyCards[0], cannonTypes[0]);

//////        HideProgressBar();
//////        ClearWarning();
//////    }

//////    private void SwitchToInventoryMode()
//////    {
//////        _mode = PanelMode.Inventory;

//////        if (cardGridRoot != null) cardGridRoot.SetActive(false);
//////        if (inventoryScrollContent != null) inventoryScrollContent.gameObject.SetActive(true);

//////        SetButtonsForMode(buy: false);
//////        PopulateInventoryCards();

//////        if (_spawnedInventoryCards.Count > 0)
//////        {
//////            // Auto-select first card
//////            _selectedInventoryId = _spawnedInventoryCards[0].InventoryId;
//////            _spawnedInventoryCards[0].SetSelected(true);
//////            ShowInventoryDetails(SelectedEntry);
//////            RefreshInventoryButtons(SelectedEntry);
//////            RefreshProgressBarForSelected(SelectedEntry);
//////        }
//////        else
//////        {
//////            ClearDetails();
//////            if (upgradeButton != null) upgradeButton.interactable = false;
//////            if (equipButton != null) equipButton.interactable = false;
//////            HideProgressBar();
//////        }

//////        ClearWarning();
//////    }

//////    private void SetButtonsForMode(bool buy)
//////    {
//////        if (buyButton != null) buyButton.gameObject.SetActive(buy);
//////        if (equipButton != null) equipButton.gameObject.SetActive(!buy);
//////        if (unequipButton != null) unequipButton.gameObject.SetActive(!buy);
//////        if (upgradeButton != null) upgradeButton.gameObject.SetActive(!buy);
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // CARD SELECTION  (called by CannonCard.OnClick)
//////    // ════════════════════════════════════════════════════════════════

//////    public void OnCardSelected(CannonCard card)
//////    {
//////        if (_mode == PanelMode.Buy)
//////        {
//////            SelectBuyCard(card, card.Data);
//////        }
//////        else
//////        {
//////            // Deselect all
//////            foreach (CannonCard c in _spawnedInventoryCards) c.SetSelected(false);
//////            card.SetSelected(true);

//////            _selectedInventoryId = card.InventoryId;
//////            CannonInventoryEntry entry = SelectedEntry;
//////            if (entry == null) return;

//////            ShowInventoryDetails(entry);
//////            RefreshInventoryButtons(entry);
//////            RefreshProgressBarForSelected(entry);
//////        }
//////        ClearWarning();
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // BUY MODE
//////    // ════════════════════════════════════════════════════════════════

//////    private void SelectBuyCard(CannonCard card, CannonData data)
//////    {
//////        if (_selectedBuyCard != null) _selectedBuyCard.SetSelected(false);
//////        _selectedBuyData = data;
//////        _selectedBuyCard = card;
//////        card?.SetSelected(true);
//////        ShowBuyDetails(data);
//////        RefreshBuyButton();
//////    }

//////    private void OnBuyClicked()
//////    {
//////        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }

//////        if (GameManager.Instance == null)
//////        {
//////            Debug.LogError("[CannonPanel] GameManager not found! Cannot spend gold.");
//////            return;
//////        }

//////        if (!GameManager.Instance.SpendGold(_selectedBuyData.cost))
//////        {
//////            ShowWarning("Not enough coins!");
//////            return;
//////        }

//////        // Create new inventory entry
//////        var entry = new CannonInventoryEntry
//////        {
//////            data = _selectedBuyData,
//////            inventoryId = _nextId++
//////        };
//////        _inventory.Add(entry);

//////        // Remove lock from this card type if this is the first purchase
//////        if (!_everBought.Contains(_selectedBuyData))
//////        {
//////            _everBought.Add(_selectedBuyData);
//////            // Find the matching buy card and unlock it
//////            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
//////                if (cannonTypes[i] == _selectedBuyData)
//////                    buyCards[i].SetLocked(false);
//////        }

//////        RefreshCoinText();
//////        RefreshBuyButton();
//////        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
//////        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId}");
//////    }

//////    private void RefreshBuyButton()
//////    {
//////        if (buyButton == null) return;
//////        int gold = GameManager.Instance?.Gold ?? 0;
//////        buyButton.interactable = _selectedBuyData != null && gold >= _selectedBuyData.cost;
//////        if (buyButtonText != null)
//////            buyButtonText.text = _selectedBuyData != null ? $"Buy ({_selectedBuyData.cost})" : "Buy";
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // INVENTORY MODE
//////    // ════════════════════════════════════════════════════════════════

//////    private void PopulateInventoryCards()
//////    {
//////        _spawnedInventoryCards.Clear();

//////        if (inventoryScrollContent == null || inventoryCardPrefab == null) return;

//////        // Destroy old cards
//////        foreach (Transform child in inventoryScrollContent)
//////            Destroy(child.gameObject);

//////        foreach (CannonInventoryEntry entry in _inventory)
//////        {
//////            CannonCard card = Instantiate(inventoryCardPrefab, inventoryScrollContent);
//////            card.SetupInventoryCard(entry);

//////            if (entry.inventoryId == _selectedInventoryId)
//////                card.SetSelected(true);

//////            _spawnedInventoryCards.Add(card);
//////        }
//////    }

//////    private void OnEquipClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (_callingSlot == null) { ShowWarning("No slot to equip to!"); return; }

//////        if (entry.isEquipped && entry.equippedSlot == _callingSlot)
//////        {
//////            ShowWarning("Already equipped here.");
//////            return;
//////        }

//////        _callingSlot.Equip(entry);
//////        RefreshInventoryButtons(entry);
//////        ShowWarning($"Equipped {entry.data.cannonName}!");
//////    }

//////    private void OnUnequipClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (!entry.isEquipped) { ShowWarning("Not equipped."); return; }

//////        entry.equippedSlot?.Unequip();
//////        RefreshInventoryButtons(entry);
//////    }

//////    private void OnUpgradeClicked()
//////    {
//////        CannonInventoryEntry entry = SelectedEntry;
//////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
//////        if (entry.IsMaxLevel) { ShowWarning("Already at MAX level!"); return; }
//////        if (entry.isUpgrading) { ShowWarning("Upgrade in progress."); return; }

//////        entry.isUpgrading = true;
//////        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//////        RefreshInventoryButtons(entry);
//////        ShowProgressBar();
//////        RefreshInventoryCardBadges();
//////        Debug.Log($"[CannonPanel] Upgrade started — '{entry.data.cannonName}' id={entry.inventoryId} " +
//////                  $"upgrade {entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES}");
//////    }

//////    private void RefreshInventoryButtons(CannonInventoryEntry entry)
//////    {
//////        if (entry == null)
//////        {
//////            if (equipButton != null) equipButton.interactable = false;
//////            if (unequipButton != null) unequipButton.interactable = false;
//////            if (upgradeButton != null) upgradeButton.interactable = false;
//////            return;
//////        }

//////        // Equip — disabled if already equipped in this same slot
//////        if (equipButton != null)
//////            equipButton.interactable = !entry.isEquipped || entry.equippedSlot != _callingSlot;

//////        // Unequip — only enabled if equipped somewhere
//////        if (unequipButton != null)
//////            unequipButton.interactable = entry.isEquipped;

//////        // Upgrade button text and state
//////        if (upgradeButton != null)
//////        {
//////            if (entry.IsMaxLevel)
//////            {
//////                if (upgradeButtonText != null) upgradeButtonText.text = "MAX";
//////                upgradeButton.interactable = false;
//////            }
//////            else if (entry.isUpgrading)
//////            {
//////                if (upgradeButtonText != null) upgradeButtonText.text = "Upgrading...";
//////                upgradeButton.interactable = false;
//////            }
//////            else
//////            {
//////                if (upgradeButtonText != null)
//////                    upgradeButtonText.text =
//////                        $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//////                upgradeButton.interactable = true;
//////            }
//////        }
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // UPGRADE TICK  (Update)
//////    // ════════════════════════════════════════════════════════════════

//////    private void TickAllUpgrades()
//////    {
//////        foreach (CannonInventoryEntry entry in _inventory)
//////        {
//////            if (!entry.isUpgrading) continue;

//////            float remaining = entry.upgradeEndTime - Time.time;

//////            // Only update the UI for the currently selected entry
//////            if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
//////            {
//////                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//////                float progress = 1f - Mathf.Clamp01(remaining / total);
//////                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;

//////                // Timer display in MM:SS format  (matches "00:00" in screenshot)
//////                float clamped = Mathf.Max(0f, remaining);
//////                int mins = (int)(clamped / 60f);
//////                int secs = (int)(clamped % 60f);
//////                if (timerText != null) timerText.text = $"{mins:00}:{secs:00}";
//////            }

//////            if (remaining <= 0f)
//////                CompleteUpgrade(entry);
//////        }
//////    }

//////    private void CompleteUpgrade(CannonInventoryEntry entry)
//////    {
//////        entry.upgradeCount++;
//////        entry.isUpgrading = false;

//////        Debug.Log($"[CannonPanel] Upgrade complete — '{entry.data.cannonName}' id={entry.inventoryId} " +
//////                  $"now level {entry.DisplayLevel}");

//////        if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
//////        {
//////            HideProgressBar();
//////            ShowInventoryDetails(entry);
//////            RefreshInventoryButtons(entry);
//////            if (timerText != null) timerText.text = "00:00";
//////        }

//////        RefreshInventoryCardBadges();
//////    }

//////    private void RefreshInventoryCardBadges()
//////    {
//////        foreach (CannonCard card in _spawnedInventoryCards)
//////        {
//////            if (card == null) continue;
//////            CannonInventoryEntry e = _inventory.Find(x => x.inventoryId == card.InventoryId);
//////            if (e != null) card.RefreshBadge(e);
//////        }
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // DETAILS PANEL
//////    // ════════════════════════════════════════════════════════════════

//////    private void ShowBuyDetails(CannonData data)
//////    {
//////        ApplyPreviewSprite(data.previewSprite ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

//////        if (levelText != null) levelText.text = "LEVEL 1";
//////        if (timerText != null) timerText.text = "00:00";
//////        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
//////        if (costText != null) costText.text = $"Cost: {data.cost}";
//////        if (rangeText != null) rangeText.text = $"Range: {data.range:F0}m";

//////        SetHUDBars(data.health, data.ability, data.damage);
//////        HideProgressBar();
//////    }

//////    private void ShowInventoryDetails(CannonInventoryEntry entry)
//////    {
//////        if (entry == null) { ClearDetails(); return; }

//////        Sprite sp = entry.data.previewSprite
//////            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null);
//////        ApplyPreviewSprite(sp);

//////        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
//////        if (nameText != null) nameText.text = $"Name: {entry.data.cannonName}";
//////        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
//////        if (rangeText != null) rangeText.text = $"Range: {entry.CurrentRange:F0}m";

//////        SetHUDBars(entry.CurrentHealth, entry.CurrentAbility, entry.CurrentDamage);

//////        // Timer — only show if currently upgrading
//////        if (timerText != null)
//////            timerText.text = entry.isUpgrading
//////                ? FormatTimer(entry.UpgradeTimeRemaining)
//////                : "00:00";
//////    }

//////    private void ClearDetails()
//////    {
//////        ApplyPreviewSprite(null);
//////        if (levelText != null) levelText.text = "LEVEL 1";
//////        if (timerText != null) timerText.text = "00:00";
//////        if (nameText != null) nameText.text = "Name: —";
//////        if (costText != null) costText.text = "";
//////        if (rangeText != null) rangeText.text = "";
//////        SetHUDBars(0f, 0f, 0f);
//////    }

//////    private void SetHUDBars(float health, float ability, float damage)
//////    {
//////        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(health / maxHealth);
//////        if (abilityBar != null) abilityBar.fillAmount = Mathf.Clamp01(ability / maxAbility);
//////        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(damage / maxDamage);

//////        if (healthValueText != null) healthValueText.text = $"{health:F0}";
//////        if (abilityValueText != null) abilityValueText.text = $"{ability:F0}";
//////        if (damageValueText != null) damageValueText.text = $"{damage:F0}";
//////    }

//////    private void ApplyPreviewSprite(Sprite s)
//////    {
//////        if (previewImage == null) return;
//////        previewImage.enabled = s != null;
//////        if (s != null) previewImage.sprite = s;
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // PROGRESS BAR
//////    // ════════════════════════════════════════════════════════════════

//////    private void ShowProgressBar()
//////    {
//////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//////    }

//////    private void HideProgressBar()
//////    {
//////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
//////        if (timerText != null && _mode == PanelMode.Buy) timerText.text = "00:00";
//////    }

//////    private void RefreshProgressBarForSelected(CannonInventoryEntry entry)
//////    {
//////        if (entry == null) { HideProgressBar(); return; }

//////        if (entry.isUpgrading)
//////        {
//////            ShowProgressBar();
//////            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//////            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
//////            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//////            if (timerText != null) timerText.text = FormatTimer(entry.UpgradeTimeRemaining);
//////        }
//////        else
//////        {
//////            HideProgressBar();
//////        }
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // COIN
//////    // ════════════════════════════════════════════════════════════════

//////    private void RefreshCoinText()
//////    {
//////        if (coinText == null) return;
//////        coinText.text = GameManager.Instance != null
//////            ? GameManager.Instance.Gold.ToString()
//////            : "0";
//////    }

//////    private void OnGoldChanged(int newAmount)
//////    {
//////        if (coinText != null) coinText.text = newAmount.ToString();
//////        if (_mode == PanelMode.Buy) RefreshBuyButton();
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // WARNING / FEEDBACK
//////    // ════════════════════════════════════════════════════════════════

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

//////    // ════════════════════════════════════════════════════════════════
//////    // CALLBACKS FROM CannonSlot
//////    // ════════════════════════════════════════════════════════════════

//////    /// <summary>Called by CannonSlot.Unequip() to refresh the inventory card list.</summary>
//////    public void RefreshAfterUnequip()
//////    {
//////        if (_mode == PanelMode.Inventory)
//////        {
//////            PopulateInventoryCards();
//////            CannonInventoryEntry entry = SelectedEntry;
//////            if (entry != null)
//////            {
//////                ShowInventoryDetails(entry);
//////                RefreshInventoryButtons(entry);
//////            }
//////        }
//////    }

//////    // ════════════════════════════════════════════════════════════════
//////    // HELPERS
//////    // ════════════════════════════════════════════════════════════════

//////    private static string FormatTimer(float seconds)
//////    {
//////        float s = Mathf.Max(0f, seconds);
//////        int min = (int)(s / 60f);
//////        int sec = (int)(s % 60f);
//////        return $"{min:00}:{sec:00}";
//////    }

//////    /// <summary>Read-only access for external systems.</summary>
//////    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

//////    public int CountOwned(CannonData data)
//////    {
//////        int n = 0;
//////        foreach (var e in _inventory) if (e.data == data) n++;
//////        return n;
//////    }
//////}

////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonPanelManager  (complete rewrite)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  WHAT WAS BROKEN — fixed in this version
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. COMPILE ERROR — old code called GameManager.OnGoldChanged,
///////     GameManager.Instance.Gold and GameManager.Instance.SpendGold()
///////     which do not exist → Unity never entered Play mode at all.
///////     FIX: gold is now self-contained inside this manager.
///////
///////  2. BLANK SCREEN — inventoryScrollContent was None in the Inspector
///////     so PopulateInventoryCards() returned immediately and showed nothing.
///////     FIX: NO SCROLL VIEW at all. The same 3 pre-placed buyCards are
///////     reused for Inventory mode (just repopulated with owned entries).
///////
///////  3. BUTTON WIRING TOO LATE — buttons were wired in Start().
///////     If OpenPanel() was called before Start() ran the buttons
///////     were not connected.
///////     FIX: all wiring moved to Awake().
///////
/////// ════════════════════════════════════════════════════════════════════
///////  FLOW
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Village → click CannonSlot.AddButton
///////     → CannonPanelManager.Instance.OpenPanel(slot)
///////       → opens panel in BUY MODE
///////
///////   BUY MODE
///////     3 cards show cannon types (locked until first purchase).
///////     Select a card → details panel updates.
///////     Click Buy → gold deducted, entry added to inventory, card unlocked.
///////     Click "Inventory" tab → INVENTORY MODE.
///////
///////   INVENTORY MODE
///////     Same 3 card slots, repopulated with owned cannons (first 3 shown).
///////     Select a card → details panel updates with live upgraded stats.
///////     Equip   → callingSlot.Equip(entry)  [the slot that opened the panel]
///////     Unequip → entry.equippedSlot.Unequip()
///////     Upgrade → starts timed upgrade (progress bar + timer count down)
///////     Click "Buy" tab → back to BUY MODE.
///////
///////   Back button → panel closes, returns to village.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  INSPECTOR WIRING  (matches screenshot hierarchy exactly)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Cannon Types   → 3 CannonData ScriptableObjects (same order as cards)
///////  Castle Slots   → all CannonSlot objects (so Equip can find a free one
///////                   when callingSlot is already occupied)
///////
///////  Tab Buttons    → BuyTabButton, InventoryTabButton, BackButton
///////
///////  Card Grid      → 3 CannonCard objects pre-placed in scene
///////                   (these are reused for BOTH Buy and Inventory mode —
///////                    NO scroll view is used)
///////
///////  Details Panel  → PreviewImage, LevelText, TimerText, NameText,
///////                   CostText, RangeText
///////
///////  HUD Bars       → HealthBar (filled Image), HealthValueText (TMP)
///////                   AbilityBar, AbilityValueText
///////                   DamageBar,  DamageValueText
///////
///////  Action Buttons → BuyButton (+BuyButtonText), EquipButton,
///////                   UnequipButton, UpgradeButton (+UpgradeButtonText)
///////
///////  Upgrade Bar    → UpgradeProgressBG (parent GameObject),
///////                   UpgradeProgressBar (filled Image inside it)
///////
///////  Coin & Warn    → CoinText, WarningText
///////  Starting Gold  → set in Inspector (default 840)
/////// </summary>
////public class CannonPanelManager : MonoBehaviour
////{
////    public static CannonPanelManager Instance { get; private set; }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INSPECTOR FIELDS
////    // ══════════════════════════════════════════════════════════════════════════

////    [Header("Cannon Types — 3 CannonData assets (same order as cards below)")]
////    [SerializeField] private CannonData[] cannonTypes;

////    [Header("Castle Slots — all CannonSlot objects on the village/castle")]
////    [SerializeField] private CannonSlot[] castleSlots;

////    // ── Tab Buttons ───────────────────────────────────────────────────────────
////    [Header("Tab Buttons")]
////    [SerializeField] private Button buyTabButton;
////    [SerializeField] private Button inventoryTabButton;
////    [SerializeField] private Button backButton;

////    // ── Card Grid (NO scroll — 3 pre-placed cards reused for both modes) ──────
////    [Header("Card Grid  ← 3 pre-placed CannonCard objects (no scroll view used)")]
////    [Tooltip("Exactly 3 CannonCard components pre-placed in the scene, same order as cannonTypes.")]
////    [SerializeField] private CannonCard[] cards;   // Level1, Level2, Level3

////    // ── Details Panel ─────────────────────────────────────────────────────────
////    [Header("Details Panel — right-hand info section")]
////    [SerializeField] private Image previewImage;
////    [SerializeField] private TextMeshProUGUI levelText;     // "LEVEL 1"
////    [SerializeField] private TextMeshProUGUI timerText;     // "00:00"
////    [SerializeField] private TextMeshProUGUI nameText;      // cannon name
////    [SerializeField] private TextMeshProUGUI costText;      // "Cost: 100"
////    [SerializeField] private TextMeshProUGUI rangeText;     // "Range: 40m"

////    // ── HUD Bars ──────────────────────────────────────────────────────────────
////    [Header("HUD Stat Bars  (Image Type = Filled, Horizontal, Fill Origin = Left)")]
////    [SerializeField] private Image healthBar;
////    [SerializeField] private TextMeshProUGUI healthValueText;
////    [SerializeField] private Image rangeBar;
////    [SerializeField] private TextMeshProUGUI rangeValueText;
////    [SerializeField] private Image damageBar;
////    [SerializeField] private TextMeshProUGUI damageValueText;

////    [Header("Max values for bar fill ratio — tune per game balance")]
////    [SerializeField] private float maxHealth = 200f;
////    [SerializeField] private float maxAbility = 150f;
////    [SerializeField] private float maxDamage = 100f;

////    // ── Upgrade Progress ──────────────────────────────────────────────────────
////    [Header("Upgrade Progress Bar")]
////    [Tooltip("Parent GameObject that contains the progress bar. Hidden when not upgrading.")]
////    [SerializeField] private GameObject upgradeProgressBG;
////    [Tooltip("Filled Image inside upgradeProgressBG — fill amount 0 → 1 as upgrade progresses.")]
////    [SerializeField] private Image upgradeProgressBar;

////    // ── Action Buttons ────────────────────────────────────────────────────────
////    [Header("Action Buttons")]
////    [SerializeField] private Button buyButton;
////    [SerializeField] private TextMeshProUGUI buyButtonText;
////    [SerializeField] private Button equipButton;
////    [SerializeField] private Button unequipButton;
////    [SerializeField] private Button upgradeButton;
////    [SerializeField] private TextMeshProUGUI upgradeButtonText;

////    // ── Coin & Warning ────────────────────────────────────────────────────────
////    [Header("Coin & Warning")]
////    [SerializeField] private TextMeshProUGUI coinText;
////    [SerializeField] private TextMeshProUGUI warningText;

////    [Header("Starting Gold (shown at top of panel)")]
////    [SerializeField] private int startingGold = 840;

////    // ══════════════════════════════════════════════════════════════════════════
////    // PRIVATE STATE
////    // ══════════════════════════════════════════════════════════════════════════

////    // Inventory
////    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
////    private int _nextId = 0;

////    // Gold (self-contained — no GameManager dependency)
////    private int _gold;

////    // Tracks which cannon types have been bought at least once (for lock visual)
////    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

////    // Mode
////    private enum Mode { Buy, Inventory }
////    private Mode _mode = Mode.Buy;

////    // Which slot opened the panel (set by OpenPanel)
////    private CannonSlot _callingSlot;

////    // Buy mode — which card/data is selected
////    private CannonCard _selectedBuyCard;
////    private CannonData _selectedBuyData;

////    // Inventory mode — which card slot index is selected (0..cards.Length-1)
////    // The entry at that index lives in _shownEntries[_selectedSlot]
////    private int _selectedSlot = -1;
////    private CannonInventoryEntry[] _shownEntries;   // one per card slot

////    private CannonInventoryEntry SelectedEntry =>
////        (_selectedSlot >= 0 && _shownEntries != null && _selectedSlot < _shownEntries.Length)
////            ? _shownEntries[_selectedSlot]
////            : null;

////    // ══════════════════════════════════════════════════════════════════════════
////    // AWAKE  — ALL initialisation here so buttons are ready before any
////    //          external code calls OpenPanel() or OnPanelOpened().
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        _gold = startingGold;
////        _shownEntries = new CannonInventoryEntry[cards != null ? cards.Length : 3];

////        WireButtons();
////        SetupBuyCards();
////        RefreshCoinText();

////        // Panel begins in Buy mode (visually ready immediately)
////        ShowBuyMode();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INITIALISATION HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Wire every button once in Awake. Never call AddListener more than once.
////    private void WireButtons()
////    {
////        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
////        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
////        backButton?.onClick.AddListener(OnBackClicked);

////        buyButton?.onClick.AddListener(OnBuyClicked);
////        equipButton?.onClick.AddListener(OnEquipClicked);
////        unequipButton?.onClick.AddListener(OnUnequipClicked);
////        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
////    }

////    /// Call SetupBuyCard on each of the 3 pre-placed cards.
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
////    // UPDATE — upgrade timer tick
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Update()
////    {
////        if (_mode == Mode.Inventory) TickUpgrades();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PUBLIC ENTRY POINTS
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by CannonSlot.AddButton (the "+" on the castle).
////    /// Stores which slot triggered the open, shows the panel, switches to Buy mode.
////    /// </summary>
////    public void OpenPanel(CannonSlot callingSlot)
////    {
////        _callingSlot = callingSlot;
////        gameObject.SetActive(true);
////        RefreshCoinText();
////        SwitchToBuyMode();
////    }

////    /// <summary>
////    /// Called by GameManager.OpenCannonPanel() after SetActive(true).
////    /// Resets to Buy mode. Safe because all init happened in Awake().
////    /// </summary>
////    public void OnPanelOpened()
////    {
////        RefreshCoinText();
////        SwitchToBuyMode();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // MODE SWITCHING
////    // ══════════════════════════════════════════════════════════════════════════

////    private void SwitchToBuyMode()
////    {
////        _mode = Mode.Buy;
////        ShowBuyMode();
////    }

////    private void SwitchToInventoryMode()
////    {
////        _mode = Mode.Inventory;
////        ShowInventoryMode();
////    }

////    // ── Buy mode layout ───────────────────────────────────────────────────────

////    private void ShowBuyMode()
////    {
////        SetActionButtons(buyVisible: true, inventoryVisible: false);
////        HideProgressBar();
////        ClearWarning();

////        // Repopulate the 3 cards with cannon type data
////        if (cards == null || cannonTypes == null) return;
////        for (int i = 0; i < cards.Length; i++)
////        {
////            if (cards[i] == null) continue;
////            if (i < cannonTypes.Length && cannonTypes[i] != null)
////            {
////                cards[i].gameObject.SetActive(true);
////                cards[i].SetupBuyCard(cannonTypes[i], locked: !_everBought.Contains(cannonTypes[i]));
////            }
////            else
////                cards[i].gameObject.SetActive(false);
////        }

////        // Restore previous buy selection, or auto-select first card
////        if (_selectedBuyCard != null)
////        {
////            _selectedBuyCard.SetSelected(true);
////            ShowBuyDetails(_selectedBuyData);
////            RefreshBuyButton();
////        }
////        else if (cards.Length > 0 && cards[0] != null)
////        {
////            SelectBuyCard(cards[0], 0);
////        }
////        else
////        {
////            ClearDetails();
////        }
////    }

////    // ── Inventory mode layout ──────────────────────────────────────────────────

////    private void ShowInventoryMode()
////    {
////        SetActionButtons(buyVisible: false, inventoryVisible: true);
////        ClearWarning();

////        if (_shownEntries == null || _shownEntries.Length != (cards?.Length ?? 0))
////            _shownEntries = new CannonInventoryEntry[cards != null ? cards.Length : 3];

////        // Fill card slots with owned entries (first N)
////        for (int i = 0; i < (cards?.Length ?? 0); i++)
////        {
////            if (cards[i] == null) continue;

////            if (i < _inventory.Count)
////            {
////                _shownEntries[i] = _inventory[i];
////                cards[i].gameObject.SetActive(true);
////                cards[i].SetupInventoryCard(_inventory[i]);
////            }
////            else
////            {
////                _shownEntries[i] = null;
////                cards[i].gameObject.SetActive(false);
////            }
////        }

////        // Auto-select: restore previous or pick slot 0
////        int autoSlot = -1;
////        if (_selectedSlot >= 0 && _selectedSlot < _shownEntries.Length
////            && _shownEntries[_selectedSlot] != null)
////        {
////            autoSlot = _selectedSlot;
////        }
////        else if (_shownEntries != null && _shownEntries.Length > 0 && _shownEntries[0] != null)
////        {
////            autoSlot = 0;
////        }

////        if (autoSlot >= 0)
////        {
////            SelectInventorySlot(autoSlot);
////        }
////        else
////        {
////            _selectedSlot = -1;
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////            HideProgressBar();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // CARD SELECTION
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Called by CannonCard.OnClick() via CannonPanelManager.Instance.OnCardSelected(this)
////    public void OnCardSelected(CannonCard card)
////    {
////        if (cards == null) return;

////        int idx = System.Array.IndexOf(cards, card);
////        if (idx < 0) return;   // card not in our array — ignore

////        if (_mode == Mode.Buy)
////            SelectBuyCard(card, idx);
////        else
////            SelectInventorySlot(idx);

////        ClearWarning();
////    }

////    // ── Buy card selection ─────────────────────────────────────────────────────

////    private void SelectBuyCard(CannonCard card, int idx)
////    {
////        // Deselect previous
////        foreach (CannonCard c in cards) c?.SetSelected(false);

////        _selectedBuyCard = card;
////        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
////        card.SetSelected(true);

////        if (_selectedBuyData != null)
////        {
////            ShowBuyDetails(_selectedBuyData);
////            RefreshBuyButton();
////        }
////        else
////        {
////            ClearDetails();
////        }
////    }

////    // ── Inventory card selection ───────────────────────────────────────────────

////    private void SelectInventorySlot(int slot)
////    {
////        // Deselect all
////        foreach (CannonCard c in cards) c?.SetSelected(false);

////        _selectedSlot = slot;

////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null)
////        {
////            ClearDetails();
////            SetInventoryButtonsEmpty();
////            HideProgressBar();
////            return;
////        }

////        cards[slot]?.SetSelected(true);
////        ShowInventoryDetails(entry);
////        RefreshInventoryButtons(entry);
////        RefreshProgressBar(entry);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BUY LOGIC
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBuyClicked()
////    {
////        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
////        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

////        _gold -= _selectedBuyData.cost;
////        RefreshCoinText();

////        // Create inventory entry
////        var entry = new CannonInventoryEntry
////        {
////            data = _selectedBuyData,
////            inventoryId = _nextId++
////        };
////        _inventory.Add(entry);

////        // Unlock card on first purchase of this type
////        if (!_everBought.Contains(_selectedBuyData))
////        {
////            _everBought.Add(_selectedBuyData);
////            // Find the card that shows this type and remove its lock overlay
////            for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
////                if (cannonTypes[i] == _selectedBuyData)
////                    cards[i]?.SetLocked(false);
////        }

////        RefreshBuyButton();
////        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
////        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} | gold={_gold}");
////    }

////    private void RefreshBuyButton()
////    {
////        if (buyButton == null) return;
////        bool canAfford = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
////        buyButton.interactable = canAfford;
////        if (buyButtonText != null)
////            buyButtonText.text = _selectedBuyData != null
////                ? $"{_selectedBuyData.cost}"
////                : "Buy";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // EQUIP / UNEQUIP
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnEquipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (entry.isEquipped) { ShowWarning("Already equipped!"); return; }

////        // Try the calling slot first; fall back to any free slot
////        CannonSlot target = (_callingSlot != null && !_callingSlot.IsOccupied)
////            ? _callingSlot
////            : FindFreeSlot();

////        if (target == null) { ShowWarning("No free cannon slot on the castle!"); return; }

////        target.Equip(entry);
////        RefreshInventoryButtons(entry);
////        ShowWarning($"Equipped {entry.data.cannonName}!");
////        Debug.Log($"[CannonPanel] Equipped '{entry.data.cannonName}' id={entry.inventoryId}");
////    }

////    private void OnUnequipClicked()
////    {
////        CannonInventoryEntry entry = SelectedEntry;
////        if (entry == null) { ShowWarning("Select a cannon first."); return; }
////        if (!entry.isEquipped) { ShowWarning("Not currently equipped."); return; }

////        entry.equippedSlot?.Unequip();
////        // RefreshAfterUnequip() is called by CannonSlot.Unequip()
////    }

////    /// Called by CannonSlot.Unequip() to refresh the card list.
////    public void RefreshAfterUnequip()
////    {
////        if (_mode == Mode.Inventory)
////            ShowInventoryMode();
////    }

////    private CannonSlot FindFreeSlot()
////    {
////        if (castleSlots == null) return null;
////        foreach (CannonSlot s in castleSlots)
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
////        if (entry.IsMaxLevel) { ShowWarning("Already at MAX level!"); return; }
////        if (entry.isUpgrading) { ShowWarning("Upgrade already in progress."); return; }

////        entry.isUpgrading = true;
////        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

////        RefreshInventoryButtons(entry);
////        ShowProgressBar();

////        // Refresh badge on the card
////        if (_selectedSlot >= 0 && _selectedSlot < cards.Length)
////            cards[_selectedSlot]?.RefreshBadge(entry);

////        Debug.Log($"[CannonPanel] Upgrade started — '{entry.data.cannonName}' id={entry.inventoryId} " +
////                  $"{entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES}");
////    }

////    /// Ticks all running upgrades; completes them when time expires.
////    private void TickUpgrades()
////    {
////        bool anyCompleted = false;

////        foreach (CannonInventoryEntry entry in _inventory)
////        {
////            if (!entry.isUpgrading) continue;

////            float remaining = entry.UpgradeTimeRemaining;

////            // Update UI only for the selected entry
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

////                Debug.Log($"[CannonPanel] Upgrade complete — '{entry.data.cannonName}' " +
////                          $"id={entry.inventoryId} now Level {entry.DisplayLevel}");
////            }
////        }

////        if (anyCompleted)
////        {
////            // Refresh cards + details for any completed upgrade
////            ShowInventoryMode();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // INVENTORY BUTTONS STATE
////    // ══════════════════════════════════════════════════════════════════════════

////    //private void RefreshInventoryButtons(CannonInventoryEntry entry)
////    //{
////    //    if (entry == null) { SetInventoryButtonsEmpty(); return; }

////    //    bool equipped = entry.isEquipped;
////    //    bool maxLevel = entry.IsMaxLevel;
////    //    bool upgrading = entry.isUpgrading;

////    //    if (equipButton != null) equipButton.interactable = !equipped;
////    //    if (unequipButton != null) unequipButton.interactable = equipped;

////    //    if (upgradeButton != null)
////    //    {
////    //        upgradeButton.interactable = !maxLevel && !upgrading;
////    //        if (upgradeButtonText != null)
////    //        {
////    //            if (maxLevel) upgradeButtonText.text = "MAX";
////    //            else if (upgrading) upgradeButtonText.text = "Upgrading…";
////    //            else upgradeButtonText.text =
////    //                                    $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
////    //        }
////    //    }
////    //}

////    private void RefreshInventoryButtons(CannonInventoryEntry entry)
////    {
////        if (entry == null)
////        {
////            SetInventoryButtonsEmpty();
////            return;
////        }

////        bool equipped = entry.isEquipped;
////        bool maxLevel = entry.IsMaxLevel;
////        bool upgrading = entry.isUpgrading;

////        // 🔥 FIX 1: SHOW / HIDE (NOT just interactable)
////        if (equipButton != null)
////            equipButton.gameObject.SetActive(!equipped);

////        if (unequipButton != null)
////            unequipButton.gameObject.SetActive(equipped);

////        // (optional) still control interactable if you want safety
////        if (equipButton != null)
////            equipButton.interactable = !equipped;

////        if (unequipButton != null)
////            unequipButton.interactable = equipped;

////        // ===============================
////        // Upgrade Button (same as before)
////        // ===============================
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

////    // ══════════════════════════════════════════════════════════════════════════
////    // ACTION BUTTON VISIBILITY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
////    {
////        buyButton?.gameObject.SetActive(buyVisible);
////        equipButton?.gameObject.SetActive(inventoryVisible);
////        unequipButton?.gameObject.SetActive(inventoryVisible);
////        upgradeButton?.gameObject.SetActive(inventoryVisible);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DETAILS PANEL
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowBuyDetails(CannonData data)
////    {
////        ApplyPreview(data.previewSprite
////            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (timerText != null) timerText.text = "00:00";
////        if (nameText != null) nameText.text = data.cannonName;
////        if (costText != null) costText.text = $"Cost: {data.cost}";
////        if (rangeText != null) rangeText.text = $"Range: {data.range:F0}m";

////        SetHUDBars(data.health, data.ability, data.damage);
////        HideProgressBar();
////    }

////    private void ShowInventoryDetails(CannonInventoryEntry entry)
////    {
////        if (entry == null) { ClearDetails(); return; }

////        ApplyPreview(entry.data.previewSprite
////            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null));

////        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
////        if (nameText != null) nameText.text = entry.data.cannonName;
////        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
////        if (rangeText != null) rangeText.text = $"Range: {entry.CurrentRange:F0}m";
////        if (timerText != null) timerText.text = entry.isUpgrading
////                                                      ? FormatTimer(entry.UpgradeTimeRemaining)
////                                                      : "00:00";

////        SetHUDBars(entry.CurrentHealth, entry.CurrentAbility, entry.CurrentDamage);
////    }

////    private void ClearDetails()
////    {
////        ApplyPreview(null);
////        if (levelText != null) levelText.text = "LEVEL 1";
////        if (timerText != null) timerText.text = "00:00";
////        if (nameText != null) nameText.text = "—";
////        if (costText != null) costText.text = "";
////        if (rangeText != null) rangeText.text = "";
////        SetHUDBars(0f, 0f, 0f);
////    }

////    private void ApplyPreview(Sprite s)
////    {
////        if (previewImage == null) return;
////        previewImage.enabled = s != null;
////        if (s != null) previewImage.sprite = s;
////    }

////    private void SetHUDBars(float h, float a, float d)
////    {
////        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / maxHealth);
////        if (rangeBar != null) rangeBar.fillAmount = Mathf.Clamp01(a / maxAbility);
////        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / maxDamage);
////        if (healthValueText != null) healthValueText.text = $"{h:F0}";
////        if (rangeValueText != null) rangeValueText.text = $"{a:F0}";
////        if (damageValueText != null) damageValueText.text = $"{d:F0}";
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PROGRESS BAR
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ShowProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
////    }

////    private void HideProgressBar()
////    {
////        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
////    }

////    private void RefreshProgressBar(CannonInventoryEntry entry)
////    {
////        if (entry == null || !entry.isUpgrading) { HideProgressBar(); return; }

////        ShowProgressBar();
////        float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
////        float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
////        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
////        if (timerText != null) timerText.text = FormatTimer(entry.UpgradeTimeRemaining);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BACK BUTTON
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnBackClicked()
////    {
////        ClearWarning();
////        // If GameManager is managing panels, let it handle the return to village.
////        // Otherwise just deactivate this panel.
////        if (GameManager.Instance != null)
////            GameManager.Instance.CloseCurrentPanel();
////        else
////            gameObject.SetActive(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // COIN TEXT
////    // ══════════════════════════════════════════════════════════════════════════

////    private void RefreshCoinText()
////    {
////        if (coinText != null) coinText.text = _gold.ToString();
////    }

////    /// Add gold from external systems (e.g. enemy defeat rewards).
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
////            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed CannonCard objects here.", this);
////        if (cards != null && cannonTypes != null && cards.Length != cannonTypes.Length)
////            Debug.LogWarning($"[CannonPanelManager] cards ({cards.Length}) and cannonTypes ({cannonTypes.Length}) must be the same length.", this);
////        if (buyButton == null)
////            Debug.LogWarning("[CannonPanelManager] buyButton is not assigned.", this);
////        if (equipButton == null)
////            Debug.LogWarning("[CannonPanelManager] equipButton is not assigned.", this);
////        if (unequipButton == null)
////            Debug.LogWarning("[CannonPanelManager] unequipButton is not assigned.", this);
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
///// ════════════════════════════════════════════════════════════════════
/////  WHAT THIS VERSION FIXES
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. INVENTORY — only owned cannons appear. Locked / unowned types
/////     are never shown in Inventory mode.
/////
/////  2. PREFAB SPAWNING — Inventory cards are instantiated from
/////     cannonCardPrefab into inventoryScrollContent (a ScrollRect
/////     content Transform). Buy mode still uses 3 pre-placed cards.
/////
/////  3. COPY NUMBERING — buying two Iron Cannons shows cards named
/////     "Iron Cannon (1/2)" and "Iron Cannon (2/2)".
/////
/////  4. DETAILS NAME — the right-hand panel always shows the same
/////     copy-numbered name as the card, so every card feels unique.
/////
/////  5. UPGRADE STAT DELTA — while an upgrade is in progress the
/////     stat bar values display "100 → 120" (current → next level).
/////     Delta rows are hidden when not upgrading.
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR WIRING  (all fields listed under their [Header])
///// ════════════════════════════════════════════════════════════════════
/////
/////  Cannon Types         3 CannonData assets (same order as buy cards)
/////  Castle Slots         all CannonSlot objects on the map
/////
/////  Tab / Back Buttons   BuyTabButton, InventoryTabButton, BackButton
/////
/////  Buy Card Grid        cardGridRoot (parent GO) + cards[0..2]
/////                       (3 pre-placed CannonCard objects, no scroll)
/////
/////  Inventory Scroll     cannonCardPrefab — the CannonCard prefab
/////                       inventoryScrollContent — ScrollRect content Transform
/////                       inventoryScrollRoot — the ScrollRect's root GO
/////                         (activated in Inventory mode, hidden in Buy mode)
/////
/////  Details Panel        previewImage, levelText, timerText,
/////                       nameText, costText, rangeText
/////
/////  HUD Bars             healthBar (Filled Image) + healthValueText (TMP)
/////                       abilityBar + abilityValueText
/////                       damageBar  + damageValueText
/////
/////  Upgrade Delta Texts  healthDeltaText, abilityDeltaText, damageDeltaText
/////                       (TMP labels shown as "→ 120" next to each bar;
/////                        optional — leave blank to skip delta display)
/////
/////  Max Bar Values       maxHealth, maxAbility, maxDamage
/////
/////  Upgrade Progress     upgradeProgressBG (parent GO) + upgradeProgressBar (Filled Image)
/////
/////  Action Buttons       buyButton + buyButtonText
/////                       equipButton, unequipButton
/////                       upgradeButton + upgradeButtonText
/////
/////  Coin & Warning       coinText, warningText
/////  Starting Gold        startingGold (default 840)
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

//    // ── Tab Buttons ───────────────────────────────────────────────────────────
//    [Header("Tab Buttons")]
//    [SerializeField] private Button buyTabButton;
//    [SerializeField] private Button inventoryTabButton;
//    [SerializeField] private Button backButton;

//    // ── Buy Mode — 3 pre-placed cards (no scroll) ─────────────────────────────
//    [Header("Buy Mode — Card Grid (3 pre-placed cards, no scroll)")]
//    [Tooltip("Parent GameObject that holds the 3 fixed buy-mode CannonCard objects.")]
//    [SerializeField] private GameObject cardGridRoot;
//    [Tooltip("Exactly 3 pre-placed CannonCard components, same order as cannonTypes.")]
//    [SerializeField] private CannonCard[] cards;   // 3 fixed buy cards

//    // ── Inventory Mode — prefab + scroll view ──────────────────────────────────
//    [Header("Inventory Mode — Prefab Spawning")]
//    [Tooltip("CannonCard prefab — must have children named: CannonImage, CardName, Selected, Locked, UpgradeBadge")]
//    [SerializeField] private CannonCard cannonCardPrefab;
//    [Tooltip("Content Transform of the ScrollRect — cards are instantiated here.")]
//    [SerializeField] private Transform inventoryScrollContent;
//    [Tooltip("Root GameObject of the ScrollRect — toggled on/off per mode.")]
//    [SerializeField] private GameObject inventoryScrollRoot;

//    // ── Details Panel ─────────────────────────────────────────────────────────
//    [Header("Details Panel — right-hand info section")]
//    [SerializeField] private Image previewImage;
//    [SerializeField] private TextMeshProUGUI levelText;      // "LEVEL 1"
//    [SerializeField] private TextMeshProUGUI timerText;      // "00:00"
//    [SerializeField] private TextMeshProUGUI nameText;       // cannon name (copy-numbered)
//    [SerializeField] private TextMeshProUGUI costText;       // "Cost: 100"
//    [SerializeField] private TextMeshProUGUI rangeText;      // "Range: 40m"

//    // ── HUD Bars ──────────────────────────────────────────────────────────────
//    [Header("HUD Stat Bars  (Image Type = Filled, Horizontal, Fill Origin = Left)")]
//    [SerializeField] private Image healthBar;
//    [SerializeField] private TextMeshProUGUI healthValueText;
//    [SerializeField] private Image abilityBar;
//    [SerializeField] private TextMeshProUGUI abilityValueText;
//    [SerializeField] private Image damageBar;
//    [SerializeField] private TextMeshProUGUI damageValueText;

//    [Header("Upgrade Delta Labels  (shown as '→ 120' next to each bar while upgrading; optional)")]
//    [Tooltip("Leave blank to disable delta display for this stat.")]
//    [SerializeField] private TextMeshProUGUI healthDeltaText;
//    [SerializeField] private TextMeshProUGUI abilityDeltaText;
//    [SerializeField] private TextMeshProUGUI damageDeltaText;

//    [Header("Max values for bar fill ratio — tune per game balance")]
//    [SerializeField] private float maxHealth = 200f;
//    [SerializeField] private float maxAbility = 150f;
//    [SerializeField] private float maxDamage = 100f;

//    // ── Upgrade Progress ──────────────────────────────────────────────────────
//    [Header("Upgrade Progress Bar")]
//    [Tooltip("Parent GameObject that wraps the progress bar. Hidden when not upgrading.")]
//    [SerializeField] private GameObject upgradeProgressBG;
//    [Tooltip("Filled Image inside upgradeProgressBG — fill 0 → 1 as upgrade progresses.")]
//    [SerializeField] private Image upgradeProgressBar;

//    // ── Action Buttons ────────────────────────────────────────────────────────
//    [Header("Action Buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button equipButton;
//    [SerializeField] private Button unequipButton;
//    [SerializeField] private Button upgradeButton;
//    [SerializeField] private TextMeshProUGUI upgradeButtonText;

//    // ── Coin & Warning ────────────────────────────────────────────────────────
//    [Header("Coin & Warning")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private TextMeshProUGUI warningText;

//    [Header("Starting Gold")]
//    [SerializeField] private int startingGold = 840;

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE STATE
//    // ══════════════════════════════════════════════════════════════════════════

//    // Gold (self-contained)
//    private int _gold;

//    // Owned cannons
//    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
//    private int _nextId = 0;

//    // Which cannon types have been bought at least once (buy card lock removal)
//    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

//    // Mode
//    private enum Mode { Buy, Inventory }
//    private Mode _mode = Mode.Buy;

//    // Slot that opened the panel
//    private CannonSlot _callingSlot;

//    // Buy mode selection
//    private CannonCard _selectedBuyCard;
//    private CannonData _selectedBuyData;

//    // Inventory mode — spawned cards & selection
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
//        HideDeltaTexts();

//        // Start in Buy mode
//        ShowBuyMode();

//        // Panel typically begins hidden; opened by OpenPanel()
//        // gameObject.SetActive(false); // un-comment if managed externally
//    }

//    private void Update()
//    {
//        if (_mode == Mode.Inventory)
//            TickUpgrades();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // INIT HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void WireButtons()
//    {
//        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
//        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
//        backButton?.onClick.AddListener(OnBackClicked);

//        buyButton?.onClick.AddListener(OnBuyClicked);
//        equipButton?.onClick.AddListener(OnEquipClicked);
//        unequipButton?.onClick.AddListener(OnUnequipClicked);
//        upgradeButton?.onClick.AddListener(OnUpgradeClicked);
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

//    /// <summary>Called by CannonSlot when the "+" button is tapped.</summary>
//    public void OpenPanel(CannonSlot callingSlot)
//    {
//        _callingSlot = callingSlot;
//        gameObject.SetActive(true);
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    /// <summary>Called by GameManager after SetActive(true) if it manages panels.</summary>
//    public void OnPanelOpened()
//    {
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // MODE SWITCHING
//    // ══════════════════════════════════════════════════════════════════════════

//    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }
//    private void SwitchToInventoryMode() { _mode = Mode.Inventory; ShowInventoryMode(); }

//    // ── Buy Mode ──────────────────────────────────────────────────────────────

//    private void ShowBuyMode()
//    {
//        // Show buy grid, hide inventory scroll
//        if (cardGridRoot != null) cardGridRoot.SetActive(true);
//        if (inventoryScrollRoot != null) inventoryScrollRoot.SetActive(false);

//        SetActionButtons(buyVisible: true, inventoryVisible: false);
//        HideProgressBar();
//        HideDeltaTexts();
//        ClearWarning();

//        // Re-apply buy cards (locks may have changed after purchases)
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
//                    cards[i].gameObject.SetActive(false);
//            }
//        }

//        // Restore or auto-select
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
//        // Hide buy grid, show inventory scroll
//        if (cardGridRoot != null) cardGridRoot.SetActive(false);
//        if (inventoryScrollRoot != null) inventoryScrollRoot.SetActive(true);

//        SetActionButtons(buyVisible: false, inventoryVisible: true);
//        ClearWarning();
//        HideProgressBar();

//        // Destroy old spawned cards
//        foreach (CannonCard c in _spawnedCards)
//            if (c != null) Destroy(c.gameObject);
//        _spawnedCards.Clear();
//        _cardEntryMap.Clear();
//        _selectedInventoryCard = null;

//        // Spawn one card per owned cannon — locked/unowned types are NEVER shown
//        if (cannonCardPrefab == null || inventoryScrollContent == null)
//        {
//            Debug.LogWarning("[CannonPanel] cannonCardPrefab or inventoryScrollContent not assigned!");
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//            return;
//        }

//        // Pre-compute copy numbering: "Iron Cannon (1/2)", "Iron Cannon (2/2)", etc.
//        var copyCount = new Dictionary<CannonData, int>();
//        var copyIndex = new Dictionary<CannonInventoryEntry, int>();
//        foreach (var entry in _inventory)
//        {
//            if (!copyCount.ContainsKey(entry.data)) copyCount[entry.data] = 0;
//            copyCount[entry.data]++;
//            copyIndex[entry] = copyCount[entry.data];
//        }

//        foreach (var entry in _inventory)
//        {
//            CannonCard card = Instantiate(cannonCardPrefab, inventoryScrollContent);
//            string displayName = BuildDisplayName(entry, copyIndex[entry], copyCount[entry.data]);
//            card.SetupInventoryCard(entry, displayName);

//            _spawnedCards.Add(card);
//            _cardEntryMap[card] = entry;
//        }

//        // Auto-select: try to restore previous selection, else pick first card
//        CannonCard autoSelect = null;
//        if (_selectedInventoryCard != null)
//        {
//            // Try to find a card for the same inventory entry
//            CannonInventoryEntry prev = null;
//            // _selectedInventoryCard was already cleared above, so use inventory id
//            // stored before the rebuild — not available after clear, so just pick first
//        }

//        if (autoSelect == null && _spawnedCards.Count > 0)
//            autoSelect = _spawnedCards[0];

//        if (autoSelect != null)
//            SelectInventoryCard(autoSelect, _cardEntryMap[autoSelect]);
//        else
//        {
//            ClearDetails();
//            SetInventoryButtonsEmpty();
//            HideDeltaTexts();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // CARD SELECTION  (called by CannonCard.OnClick → OnCardSelected)
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

//    // ── Buy card selection ─────────────────────────────────────────────────────

//    private void SelectBuyCard(CannonCard card, int idx)
//    {
//        if (cards != null)
//            foreach (CannonCard c in cards) c?.SetSelected(false);

//        _selectedBuyCard = card;
//        _selectedBuyData = (idx < cannonTypes.Length) ? cannonTypes[idx] : null;
//        card.SetSelected(true);

//        if (_selectedBuyData != null)
//        {
//            ShowBuyDetails(_selectedBuyData);
//            RefreshBuyButton();
//        }
//        else
//            ClearDetails();
//    }

//    // ── Inventory card selection ───────────────────────────────────────────────

//    private void SelectInventoryCard(CannonCard card, CannonInventoryEntry entry)
//    {
//        foreach (CannonCard c in _spawnedCards) c?.SetSelected(false);

//        _selectedInventoryCard = card;
//        card.SetSelected(true);

//        ShowInventoryDetails(entry);
//        RefreshInventoryButtons(entry);
//        RefreshProgressBar(entry);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BUY LOGIC
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBuyClicked()
//    {
//        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }
//        if (_gold < _selectedBuyData.cost) { ShowWarning("Not enough coins!"); return; }

//        _gold -= _selectedBuyData.cost;
//        RefreshCoinText();

//        var entry = new CannonInventoryEntry
//        {
//            data = _selectedBuyData,
//            inventoryId = _nextId++
//        };
//        _inventory.Add(entry);

//        // Unlock the buy card for this type on first purchase
//        if (!_everBought.Contains(_selectedBuyData))
//        {
//            _everBought.Add(_selectedBuyData);
//            if (cards != null && cannonTypes != null)
//                for (int i = 0; i < cards.Length && i < cannonTypes.Length; i++)
//                    if (cannonTypes[i] == _selectedBuyData)
//                        cards[i]?.SetLocked(false);
//        }

//        RefreshBuyButton();
//        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
//        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} | gold={_gold}");
//    }

//    private void RefreshBuyButton()
//    {
//        if (buyButton == null) return;
//        buyButton.interactable = _selectedBuyData != null && _gold >= _selectedBuyData.cost;
//        if (buyButtonText != null)
//            buyButtonText.text = _selectedBuyData != null
//                ? $"Buy  ({_selectedBuyData.cost})"
//                : "Buy";
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
//        // RefreshAfterUnequip() will be called back by CannonSlot.Unequip()
//    }

//    /// <summary>Called by CannonSlot.Unequip() to refresh card list.</summary>
//    public void RefreshAfterUnequip()
//    {
//        if (_mode == Mode.Inventory) ShowInventoryMode();
//    }

//    private CannonSlot FindFreeSlot()
//    {
//        if (castleSlots == null) return null;
//        foreach (CannonSlot s in castleSlots)
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
//        if (entry.IsMaxLevel) { ShowWarning("Already at MAX level!"); return; }
//        if (entry.isUpgrading) { ShowWarning("Upgrade already in progress."); return; }

//        entry.isUpgrading = true;
//        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

//        // Refresh the card badge immediately to show upgrading state
//        if (_selectedInventoryCard != null) _selectedInventoryCard.RefreshBadge(entry);

//        RefreshInventoryButtons(entry);
//        ShowProgressBar();
//        ShowInventoryDetails(entry);   // updates delta display

//        Debug.Log($"[CannonPanel] Upgrade started — '{entry.data.cannonName}' " +
//                  $"id={entry.inventoryId}  {entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES}");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // UPGRADE TICK  (called every frame while in Inventory mode)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void TickUpgrades()
//    {
//        bool anyCompleted = false;

//        foreach (var entry in _inventory)
//        {
//            if (!entry.isUpgrading) continue;

//            float remaining = entry.UpgradeTimeRemaining;

//            // Update UI only for the currently selected entry
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

//                Debug.Log($"[CannonPanel] Upgrade complete — '{entry.data.cannonName}' " +
//                          $"id={entry.inventoryId}  now Level {entry.DisplayLevel}");
//            }
//        }

//        if (anyCompleted)
//        {
//            // Rebuild the card list so badges and details reflect the new level
//            int prevSelectedId = SelectedEntry?.inventoryId ?? -1;
//            ShowInventoryMode();

//            // Re-select the same entry after rebuild
//            if (prevSelectedId >= 0)
//            {
//                foreach (CannonCard c in _spawnedCards)
//                {
//                    if (_cardEntryMap.TryGetValue(c, out var e) && e.inventoryId == prevSelectedId)
//                    {
//                        SelectInventoryCard(c, e);
//                        break;
//                    }
//                }
//            }
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DETAILS PANEL — BUY MODE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowBuyDetails(CannonData data)
//    {
//        ApplyPreview(data.previewSprite
//            ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (timerText != null) timerText.text = "00:00";
//        if (nameText != null) nameText.text = data.cannonName;
//        if (costText != null) costText.text = $"Cost: {data.cost}";
//        if (rangeText != null) rangeText.text = $"Range: {data.range:F0}m";

//        SetHUDBars(data.health, data.ability, data.damage, showDelta: false);
//        HideProgressBar();
//        HideDeltaTexts();
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
//        if (timerText != null) timerText.text = entry.isUpgrading
//            ? FormatTimer(entry.UpgradeTimeRemaining)
//            : "00:00";

//        // Show the copy-numbered name (same label the card shows)
//        if (nameText != null) nameText.text = GetDetailDisplayName(entry);

//        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
//        if (rangeText != null) rangeText.text = $"Range: {entry.CurrentRange:F0}m";

//        // Stat bars + upgrade delta
//        bool showDelta = entry.isUpgrading && !entry.IsMaxLevel;
//        SetHUDBars(entry.CurrentHealth, entry.CurrentAbility, entry.CurrentDamage,
//                   showDelta: showDelta,
//                   entry: entry);
//    }

//    // ── Details helpers ────────────────────────────────────────────────────────

//    private void ClearDetails()
//    {
//        ApplyPreview(null);
//        if (levelText != null) levelText.text = "LEVEL 1";
//        if (timerText != null) timerText.text = "00:00";
//        if (nameText != null) nameText.text = "—";
//        if (costText != null) costText.text = "";
//        if (rangeText != null) rangeText.text = "";
//        SetHUDBars(0f, 0f, 0f, showDelta: false);
//        HideDeltaTexts();
//    }

//    private void ApplyPreview(Sprite s)
//    {
//        if (previewImage == null) return;
//        previewImage.enabled = s != null;
//        if (s != null) previewImage.sprite = s;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HUD BARS  (with optional upgrade-delta overlay)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Sets the three stat bars.
//    /// When <paramref name="showDelta"/> is true and the entry is upgrading,
//    /// value texts show "100 → 120" and the delta labels show "+20".
//    ///
//    /// HOW NEXT-LEVEL STATS ARE COMPUTED:
//    ///   We temporarily increment entry.upgradeCount, read the computed properties,
//    ///   then restore the count. If your CannonInventoryEntry uses a different
//    ///   mechanism, adapt GetNextLevelStats() below.
//    /// </summary>
//    private void SetHUDBars(float h, float a, float d,
//                            bool showDelta,
//                            CannonInventoryEntry entry = null)
//    {
//        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(h / maxHealth);
//        if (abilityBar != null) abilityBar.fillAmount = Mathf.Clamp01(a / maxAbility);
//        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(d / maxDamage);

//        if (showDelta && entry != null && !entry.IsMaxLevel)
//        {
//            // Peek at next-level stats without permanently changing the entry
//            GetNextLevelStats(entry, out float nh, out float na, out float nd);

//            if (healthValueText != null) healthValueText.text = $"{h:F0} → {nh:F0}";
//            if (abilityValueText != null) abilityValueText.text = $"{a:F0} → {na:F0}";
//            if (damageValueText != null) damageValueText.text = $"{d:F0} → {nd:F0}";

//            // Delta labels (the "+20" labels beside each bar)
//            SetDeltaText(healthDeltaText, nh - h);
//            SetDeltaText(abilityDeltaText, na - a);
//            SetDeltaText(damageDeltaText, nd - d);
//        }
//        else
//        {
//            if (healthValueText != null) healthValueText.text = $"{h:F0}";
//            if (abilityValueText != null) abilityValueText.text = $"{a:F0}";
//            if (damageValueText != null) damageValueText.text = $"{d:F0}";
//            HideDeltaTexts();
//        }
//    }

//    /// <summary>
//    /// Peeks at next-level stats by temporarily incrementing upgradeCount.
//    /// CannonInventoryEntry.CurrentHealth/CurrentAbility/CurrentDamage must be
//    /// computed from upgradeCount for this to work correctly.
//    /// </summary>
//    private static void GetNextLevelStats(CannonInventoryEntry entry,
//                                          out float nextHealth,
//                                          out float nextAbility,
//                                          out float nextDamage)
//    {
//        entry.upgradeCount++;
//        nextHealth = entry.CurrentHealth;
//        nextAbility = entry.CurrentAbility;
//        nextDamage = entry.CurrentDamage;
//        entry.upgradeCount--;
//    }

//    private static void SetDeltaText(TextMeshProUGUI label, float delta)
//    {
//        if (label == null) return;
//        label.gameObject.SetActive(true);
//        label.text = delta > 0f ? $"+{delta:F0}" : $"{delta:F0}";
//    }

//    private void HideDeltaTexts()
//    {
//        if (healthDeltaText != null) healthDeltaText.gameObject.SetActive(false);
//        if (abilityDeltaText != null) abilityDeltaText.gameObject.SetActive(false);
//        if (damageDeltaText != null) damageDeltaText.gameObject.SetActive(false);
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

//        // Show Equip or Unequip based on equipped state
//        if (equipButton != null) { equipButton.gameObject.SetActive(!equipped); equipButton.interactable = !equipped; }
//        if (unequipButton != null) { unequipButton.gameObject.SetActive(equipped); unequipButton.interactable = equipped; }

//        if (upgradeButton != null)
//        {
//            upgradeButton.interactable = !maxLevel && !upgrading;

//            if (upgradeButtonText != null)
//            {
//                if (maxLevel) upgradeButtonText.text = "MAX";
//                else if (upgrading) upgradeButtonText.text = "Upgrading…";
//                else upgradeButtonText.text =
//                    $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
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

//    // ── Button visibility helper ───────────────────────────────────────────────

//    private void SetActionButtons(bool buyVisible, bool inventoryVisible)
//    {
//        buyButton?.gameObject.SetActive(buyVisible);
//        equipButton?.gameObject.SetActive(inventoryVisible);
//        unequipButton?.gameObject.SetActive(inventoryVisible);
//        upgradeButton?.gameObject.SetActive(inventoryVisible);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PROGRESS BAR
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ShowProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
//        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
//    }

//    private void HideProgressBar()
//    {
//        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
//    }

//    private void RefreshProgressBar(CannonInventoryEntry entry)
//    {
//        if (entry == null || !entry.isUpgrading) { HideProgressBar(); return; }

//        ShowProgressBar();
//        float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
//        float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
//        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
//        if (timerText != null) timerText.text = FormatTimer(entry.UpgradeTimeRemaining);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // COPY-NUMBERED DISPLAY NAMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the name shown on a spawned inventory card, e.g. "Iron Cannon (2/3)".
//    /// If the player owns only one of this type, just returns the base name.
//    /// </summary>
//    private string BuildDisplayName(CannonInventoryEntry entry, int copyIdx, int totalCopies)
//    {
//        return totalCopies > 1
//            ? $"{entry.data.cannonName} ({copyIdx}/{totalCopies})"
//            : entry.data.cannonName;
//    }

//    /// <summary>
//    /// Returns the display name for the right-hand details panel — the same
//    /// copy-numbered string the card itself shows.
//    /// </summary>
//    private string GetDetailDisplayName(CannonInventoryEntry entry)
//    {
//        int total = 0, myIdx = 0;
//        foreach (var e in _inventory)
//        {
//            if (e.data == entry.data) total++;
//            if (e == entry && myIdx == 0) myIdx = total;
//        }
//        return total > 1 ? $"{entry.data.cannonName} ({myIdx}/{total})" : entry.data.cannonName;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BACK / CLOSE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnBackClicked()
//    {
//        ClearWarning();
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
//            Debug.LogWarning("[CannonPanelManager] cards is empty — drag the 3 pre-placed buy CannonCard objects.", this);
//        if (cannonCardPrefab == null)
//            Debug.LogWarning("[CannonPanelManager] cannonCardPrefab not assigned — inventory cards won't spawn.", this);
//        if (inventoryScrollContent == null)
//            Debug.LogWarning("[CannonPanelManager] inventoryScrollContent not assigned — inventory cards won't spawn.", this);
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
        gameObject.SetActive(true);
        RefreshCoinText();
        SwitchToBuyMode();
    }

    //    public void OnPanelOpened()
//    {
//        RefreshCoinText();
//        SwitchToBuyMode();
//    }

    // ══════════════════════════════════════════════════════════════════════════
    // MODE SWITCHING
    // ══════════════════════════════════════════════════════════════════════════

    // Called by BuyTabButton — always visible, works from any mode
    private void SwitchToBuyMode() { _mode = Mode.Buy; ShowBuyMode(); }
    private void SwitchToInventoryMode() { _mode = Mode.Inventory; ShowInventoryMode(); }

    // ── Buy Mode ──────────────────────────────────────────────────────────────

    private void ShowBuyMode()
    {
        // Show card grid; hide inventory container
        if (cardGridRoot != null) cardGridRoot.SetActive(true);
        if (inventoryGridContent != null) inventoryGridContent.gameObject.SetActive(false);

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

        // Build copy-number index  →  "Iron Cannon (1/2)", "Iron Cannon (2/2)" etc.
        var copyCount = new Dictionary<CannonData, int>();
        var copyIndex = new Dictionary<CannonInventoryEntry, int>();
        foreach (var entry in _inventory)
        {
            if (!copyCount.ContainsKey(entry.data)) copyCount[entry.data] = 0;
            copyCount[entry.data]++;
            copyIndex[entry] = copyCount[entry.data];
        }

        // Spawn one card per owned cannon entry
        foreach (var entry in _inventory)
        {
            CannonCard card = Instantiate(cannonCardPrefab, inventoryGridContent);
            string displayName = BuildDisplayName(entry, copyIndex[entry], copyCount[entry.data]);
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
        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId} gold={_gold}");
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
        if (castleSlots == null) return null;
        foreach (var s in castleSlots)
            if (s != null && !s.IsOccupied) return s;
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
        if (nameText != null) nameText.text = data.cannonName;
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
        if (nameText != null) nameText.text = GetDetailDisplayName(entry);
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

            // Format: "40+8"  (shows how much this upgrade will add)
            if (healthValueText != null) healthValueText.text = $"{h:F0}+{(nh - h):F0}";
            if (rangeValueText != null) rangeValueText.text = $"{r:F0}+{(nr - r):F0}";
            if (damageValueText != null) damageValueText.text = $"{d:F0}+{(nd - d):F0}";
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
                else if (upgrading) upgradeButtonText.text = "Upgrading…";
                else upgradeButtonText.text =
                    $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
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
        // If you use a GameManager to manage open panels, call it here.
        // Otherwise just deactivate.
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