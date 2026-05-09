using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonPanelManager
///
/// The single script that drives the entire Cannon Panel.
/// Attach to the root CannonPanle GameObject (matches screenshot hierarchy).
///
/// ════════════════════════════════════════════════════════════════
/// FLOW
/// ════════════════════════════════════════════════════════════════
///
///  Village Panel
///    └── CannonSlot.AddButton clicked
///          └── OpenPanel(callingSlot)  → panel opens in BUY MODE
///
///  BUY MODE
///    • Shows 3 cannon cards, all locked initially
///    • Select a card → details (Name, Cost, Range) + HUD bars update
///    • Click Buy → deducts gold, creates CannonInventoryEntry, lock removed on that card
///    • Click "Inventory" tab → switches to INVENTORY MODE
///
///  INVENTORY MODE
///    • Spawns one card per owned cannon (not equipped ones can also be shown — configurable)
///    • Select a card → details update with CURRENT upgraded stats + level badge + timer
///    • Equip   → calls callingSlot.Equip(entry); panel closes
///    • Unequip → calls entry.equippedSlot.Unequip()
///    • Upgrade → starts timed upgrade on selected entry
///    • Click "Buy" tab → switches back to BUY MODE
///
///  BackButton → closes panel, returns to Village
///
/// ════════════════════════════════════════════════════════════════
/// HIERARCHY (CannonPanle in screenshot)
/// ════════════════════════════════════════════════════════════════
///
///  CannonPanle
///  ├── bg
///  ├── BackButton
///  ├── BuyTabButton          ← "Buy" tab
///  ├── InventoryTabButton    ← "Inventory" tab (shown as "Inventory" text in screenshot)
///  ├── CardGrid              ← parent of the 3 buy-mode cards
///  │   ├── CannonCard_0  (CannonCard)
///  │   ├── CannonCard_1  (CannonCard)
///  │   └── CannonCard_2  (CannonCard)
///  ├── InventoryScrollContent  ← dynamic inventory cards spawned here
///  ├── Panel                 ← right-side details panel
///  │   ├── Level             ← "LEVEL 1" badge + upgrade timer
///  │   │   └── Text (TMP)   ← levelText
///  │   ├── TimerText (TMP)   ← "00:00"
///  │   ├── Cannon1           ← large preview Image
///  │   ├── NameText (TMP)    ← "Name: Iron Field"
///  │   ├── CostText (TMP)    ← "Cost: 100"
///  │   ├── RangeText (TMP)   ← "Range: 40m"
///  │   ├── HealthBar (Image Filled) + HealthText (TMP)
///  │   ├── AbilityBar        + AbilityText
///  │   ├── DamageBar         + DamageText
///  │   ├── BuyButton
///  │   ├── EquipButton       (inventory mode)
///  │   ├── UnequipButton     (inventory mode)
///  │   └── UpgradeButton     (inventory mode)
///  └── CoinText (TMP)        ← coin amount top-right (reads from GameManager)
///
/// </summary>
public class CannonPanelManager : MonoBehaviour
{
    public static CannonPanelManager Instance { get; private set; }

    // ════════════════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ════════════════════════════════════════════════════════════════

    [Header("Cannon Types — assign 3 CannonData assets")]
    [SerializeField] private CannonData[] cannonTypes;   // 3 entries

    // ── Tab buttons ───────────────────────────────────────────────────────────
    [Header("Tab Buttons")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button backButton;

    // ── Buy Mode ──────────────────────────────────────────────────────────────
    [Header("Buy Mode — Card Grid")]
    [Tooltip("Parent GameObject containing the 3 fixed CannonCard objects")]
    [SerializeField] private GameObject cardGridRoot;
    [Tooltip("3 pre-placed CannonCard components inside CardGrid, same order as cannonTypes")]
    [SerializeField] private CannonCard[] buyCards;       // 3 entries

    // ── Inventory Mode ────────────────────────────────────────────────────────
    [Header("Inventory Mode")]
    [Tooltip("Content Transform of the ScrollRect — dynamic cards spawn here")]
    [SerializeField] private Transform inventoryScrollContent;
    [Tooltip("CannonCard prefab spawned for each owned cannon")]
    [SerializeField] private CannonCard inventoryCardPrefab;

    // ── Details Panel (right side) ────────────────────────────────────────────
    [Header("Details Panel")]
    [SerializeField] private Image previewImage;    // Cannon1 in hierarchy
    [SerializeField] private TextMeshProUGUI levelText;       // "LEVEL 1"
    [SerializeField] private TextMeshProUGUI timerText;       // "00:00"
    [SerializeField] private TextMeshProUGUI nameText;        // "Name: Iron Field"
    [SerializeField] private TextMeshProUGUI costText;        // "Cost: 100"
    [SerializeField] private TextMeshProUGUI rangeText;       // "Range: 40m"

    // ── HUD bars (screenshot shows HEALTH / ABILITY / DAMAGE) ────────────────
    [Header("HUD Stat Bars (Image Type = Filled, Horizontal, Fill Origin = Left)")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private Image abilityBar;
    [SerializeField] private TextMeshProUGUI abilityValueText;
    [SerializeField] private Image damageBar;
    [SerializeField] private TextMeshProUGUI damageValueText;

    [Header("Max values for bar fill ratio — tune per game balance")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float maxAbility = 150f;
    [SerializeField] private float maxDamage = 100f;

    // ── Upgrade progress bar ──────────────────────────────────────────────────
    [Header("Upgrade Progress (shown while upgrading)")]
    [SerializeField] private GameObject upgradeProgressBG;
    [SerializeField] private Image upgradeProgressBar;

    // ── Action buttons ────────────────────────────────────────────────────────
    [Header("Action Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;  // "Upgrade (1/3)" / "MAX"

    // ── Coin / Warning ────────────────────────────────────────────────────────
    [Header("Coin & Warning")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI warningText;

    // ════════════════════════════════════════════════════════════════
    // PRIVATE STATE
    // ════════════════════════════════════════════════════════════════

    private readonly List<CannonInventoryEntry> _inventory = new List<CannonInventoryEntry>();
    private int _nextId = 0;

    // Track which cannon types have been purchased at least once (for lock removal)
    private readonly HashSet<CannonData> _everBought = new HashSet<CannonData>();

    private enum PanelMode { Buy, Inventory }
    private PanelMode _mode = PanelMode.Buy;

    // The slot that opened this panel (set by OpenPanel)
    private CannonSlot _callingSlot;

    // Buy mode selection
    private CannonData _selectedBuyData;
    private CannonCard _selectedBuyCard;

    // Inventory mode selection
    private int _selectedInventoryId = -1;
    private CannonInventoryEntry SelectedEntry =>
        _inventory.Find(e => e.inventoryId == _selectedInventoryId);

    // Spawned inventory cards (for badge refresh)
    private readonly List<CannonCard> _spawnedInventoryCards = new List<CannonCard>();

    // ════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Wire tab buttons
        buyTabButton?.onClick.AddListener(SwitchToBuyMode);
        inventoryTabButton?.onClick.AddListener(SwitchToInventoryMode);
        backButton?.onClick.AddListener(ClosePanel);

        // Wire action buttons
        buyButton?.onClick.AddListener(OnBuyClicked);
        equipButton?.onClick.AddListener(OnEquipClicked);
        unequipButton?.onClick.AddListener(OnUnequipClicked);
        upgradeButton?.onClick.AddListener(OnUpgradeClicked);

        // Subscribe to GameManager gold changes so coin display stays in sync
        GameManager.OnGoldChanged += OnGoldChanged;

        // Setup the 3 fixed buy cards (all locked at start)
        if (buyCards != null)
            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
                buyCards[i].SetupBuyCard(cannonTypes[i], locked: true);

        // Panel starts hidden — it is shown by OpenPanel()
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.OnGoldChanged -= OnGoldChanged;
    }

    private void Update()
    {
        TickAllUpgrades();
    }

    // ════════════════════════════════════════════════════════════════
    // PANEL OPEN / CLOSE
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by CannonSlot.AddButton.
    /// Opens the panel in Buy mode, remembers which slot triggered the open.
    /// </summary>
    public void OpenPanel(CannonSlot callingSlot)
    {
        _callingSlot = callingSlot;
        gameObject.SetActive(true);
        RefreshCoinText();
        SwitchToBuyMode();
    }

    private void ClosePanel()
    {
        ClearWarning();
        gameObject.SetActive(false);
        // Return to village via GameManager
        GameManager.Instance?.CloseCurrentPanel();
    }

    // ════════════════════════════════════════════════════════════════
    // MODE SWITCHING
    // ════════════════════════════════════════════════════════════════

    private void SwitchToBuyMode()
    {
        _mode = PanelMode.Buy;

        // Show card grid, hide inventory scroll
        if (cardGridRoot != null) cardGridRoot.SetActive(true);
        if (inventoryScrollContent != null) inventoryScrollContent.gameObject.SetActive(false);

        // Show Buy button, hide inventory action buttons
        SetButtonsForMode(buy: true);

        // Auto-select first card
        if (buyCards != null && buyCards.Length > 0 && cannonTypes.Length > 0)
            SelectBuyCard(buyCards[0], cannonTypes[0]);

        HideProgressBar();
        ClearWarning();
    }

    private void SwitchToInventoryMode()
    {
        _mode = PanelMode.Inventory;

        if (cardGridRoot != null) cardGridRoot.SetActive(false);
        if (inventoryScrollContent != null) inventoryScrollContent.gameObject.SetActive(true);

        SetButtonsForMode(buy: false);
        PopulateInventoryCards();

        if (_spawnedInventoryCards.Count > 0)
        {
            // Auto-select first card
            _selectedInventoryId = _spawnedInventoryCards[0].InventoryId;
            _spawnedInventoryCards[0].SetSelected(true);
            ShowInventoryDetails(SelectedEntry);
            RefreshInventoryButtons(SelectedEntry);
            RefreshProgressBarForSelected(SelectedEntry);
        }
        else
        {
            ClearDetails();
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (equipButton != null) equipButton.interactable = false;
            HideProgressBar();
        }

        ClearWarning();
    }

    private void SetButtonsForMode(bool buy)
    {
        if (buyButton != null) buyButton.gameObject.SetActive(buy);
        if (equipButton != null) equipButton.gameObject.SetActive(!buy);
        if (unequipButton != null) unequipButton.gameObject.SetActive(!buy);
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(!buy);
    }

    // ════════════════════════════════════════════════════════════════
    // CARD SELECTION  (called by CannonCard.OnClick)
    // ════════════════════════════════════════════════════════════════

    public void OnCardSelected(CannonCard card)
    {
        if (_mode == PanelMode.Buy)
        {
            SelectBuyCard(card, card.Data);
        }
        else
        {
            // Deselect all
            foreach (CannonCard c in _spawnedInventoryCards) c.SetSelected(false);
            card.SetSelected(true);

            _selectedInventoryId = card.InventoryId;
            CannonInventoryEntry entry = SelectedEntry;
            if (entry == null) return;

            ShowInventoryDetails(entry);
            RefreshInventoryButtons(entry);
            RefreshProgressBarForSelected(entry);
        }
        ClearWarning();
    }

    // ════════════════════════════════════════════════════════════════
    // BUY MODE
    // ════════════════════════════════════════════════════════════════

    private void SelectBuyCard(CannonCard card, CannonData data)
    {
        if (_selectedBuyCard != null) _selectedBuyCard.SetSelected(false);
        _selectedBuyData = data;
        _selectedBuyCard = card;
        card?.SetSelected(true);
        ShowBuyDetails(data);
        RefreshBuyButton();
    }

    private void OnBuyClicked()
    {
        if (_selectedBuyData == null) { ShowWarning("Select a cannon first."); return; }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[CannonPanel] GameManager not found! Cannot spend gold.");
            return;
        }

        if (!GameManager.Instance.SpendGold(_selectedBuyData.cost))
        {
            ShowWarning("Not enough coins!");
            return;
        }

        // Create new inventory entry
        var entry = new CannonInventoryEntry
        {
            data = _selectedBuyData,
            inventoryId = _nextId++
        };
        _inventory.Add(entry);

        // Remove lock from this card type if this is the first purchase
        if (!_everBought.Contains(_selectedBuyData))
        {
            _everBought.Add(_selectedBuyData);
            // Find the matching buy card and unlock it
            for (int i = 0; i < buyCards.Length && i < cannonTypes.Length; i++)
                if (cannonTypes[i] == _selectedBuyData)
                    buyCards[i].SetLocked(false);
        }

        RefreshCoinText();
        RefreshBuyButton();
        ShowWarning($"Bought {_selectedBuyData.cannonName}!");
        Debug.Log($"[CannonPanel] Bought '{_selectedBuyData.cannonName}' id={entry.inventoryId}");
    }

    private void RefreshBuyButton()
    {
        if (buyButton == null) return;
        int gold = GameManager.Instance?.Gold ?? 0;
        buyButton.interactable = _selectedBuyData != null && gold >= _selectedBuyData.cost;
        if (buyButtonText != null)
            buyButtonText.text = _selectedBuyData != null ? $"Buy ({_selectedBuyData.cost})" : "Buy";
    }

    // ════════════════════════════════════════════════════════════════
    // INVENTORY MODE
    // ════════════════════════════════════════════════════════════════

    private void PopulateInventoryCards()
    {
        _spawnedInventoryCards.Clear();

        if (inventoryScrollContent == null || inventoryCardPrefab == null) return;

        // Destroy old cards
        foreach (Transform child in inventoryScrollContent)
            Destroy(child.gameObject);

        foreach (CannonInventoryEntry entry in _inventory)
        {
            CannonCard card = Instantiate(inventoryCardPrefab, inventoryScrollContent);
            card.SetupInventoryCard(entry);

            if (entry.inventoryId == _selectedInventoryId)
                card.SetSelected(true);

            _spawnedInventoryCards.Add(card);
        }
    }

    private void OnEquipClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (_callingSlot == null) { ShowWarning("No slot to equip to!"); return; }

        if (entry.isEquipped && entry.equippedSlot == _callingSlot)
        {
            ShowWarning("Already equipped here.");
            return;
        }

        _callingSlot.Equip(entry);
        RefreshInventoryButtons(entry);
        ShowWarning($"Equipped {entry.data.cannonName}!");
    }

    private void OnUnequipClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (!entry.isEquipped) { ShowWarning("Not equipped."); return; }

        entry.equippedSlot?.Unequip();
        RefreshInventoryButtons(entry);
    }

    private void OnUpgradeClicked()
    {
        CannonInventoryEntry entry = SelectedEntry;
        if (entry == null) { ShowWarning("Select a cannon first."); return; }
        if (entry.IsMaxLevel) { ShowWarning("Already at MAX level!"); return; }
        if (entry.isUpgrading) { ShowWarning("Upgrade in progress."); return; }

        entry.isUpgrading = true;
        entry.upgradeEndTime = Time.time + entry.data.upgradeDuration;

        RefreshInventoryButtons(entry);
        ShowProgressBar();
        RefreshInventoryCardBadges();
        Debug.Log($"[CannonPanel] Upgrade started — '{entry.data.cannonName}' id={entry.inventoryId} " +
                  $"upgrade {entry.upgradeCount + 1}/{CannonInventoryEntry.MAX_UPGRADES}");
    }

    private void RefreshInventoryButtons(CannonInventoryEntry entry)
    {
        if (entry == null)
        {
            if (equipButton != null) equipButton.interactable = false;
            if (unequipButton != null) unequipButton.interactable = false;
            if (upgradeButton != null) upgradeButton.interactable = false;
            return;
        }

        // Equip — disabled if already equipped in this same slot
        if (equipButton != null)
            equipButton.interactable = !entry.isEquipped || entry.equippedSlot != _callingSlot;

        // Unequip — only enabled if equipped somewhere
        if (unequipButton != null)
            unequipButton.interactable = entry.isEquipped;

        // Upgrade button text and state
        if (upgradeButton != null)
        {
            if (entry.IsMaxLevel)
            {
                if (upgradeButtonText != null) upgradeButtonText.text = "MAX";
                upgradeButton.interactable = false;
            }
            else if (entry.isUpgrading)
            {
                if (upgradeButtonText != null) upgradeButtonText.text = "Upgrading...";
                upgradeButton.interactable = false;
            }
            else
            {
                if (upgradeButtonText != null)
                    upgradeButtonText.text =
                        $"Upgrade ({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
                upgradeButton.interactable = true;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // UPGRADE TICK  (Update)
    // ════════════════════════════════════════════════════════════════

    private void TickAllUpgrades()
    {
        foreach (CannonInventoryEntry entry in _inventory)
        {
            if (!entry.isUpgrading) continue;

            float remaining = entry.upgradeEndTime - Time.time;

            // Only update the UI for the currently selected entry
            if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
            {
                float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
                float progress = 1f - Mathf.Clamp01(remaining / total);
                if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;

                // Timer display in MM:SS format  (matches "00:00" in screenshot)
                float clamped = Mathf.Max(0f, remaining);
                int mins = (int)(clamped / 60f);
                int secs = (int)(clamped % 60f);
                if (timerText != null) timerText.text = $"{mins:00}:{secs:00}";
            }

            if (remaining <= 0f)
                CompleteUpgrade(entry);
        }
    }

    private void CompleteUpgrade(CannonInventoryEntry entry)
    {
        entry.upgradeCount++;
        entry.isUpgrading = false;

        Debug.Log($"[CannonPanel] Upgrade complete — '{entry.data.cannonName}' id={entry.inventoryId} " +
                  $"now level {entry.DisplayLevel}");

        if (_mode == PanelMode.Inventory && entry.inventoryId == _selectedInventoryId)
        {
            HideProgressBar();
            ShowInventoryDetails(entry);
            RefreshInventoryButtons(entry);
            if (timerText != null) timerText.text = "00:00";
        }

        RefreshInventoryCardBadges();
    }

    private void RefreshInventoryCardBadges()
    {
        foreach (CannonCard card in _spawnedInventoryCards)
        {
            if (card == null) continue;
            CannonInventoryEntry e = _inventory.Find(x => x.inventoryId == card.InventoryId);
            if (e != null) card.RefreshBadge(e);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // DETAILS PANEL
    // ════════════════════════════════════════════════════════════════

    private void ShowBuyDetails(CannonData data)
    {
        ApplyPreviewSprite(data.previewSprite ?? (data.idleSprites?.Length > 0 ? data.idleSprites[0] : null));

        if (levelText != null) levelText.text = "LEVEL 1";
        if (timerText != null) timerText.text = "00:00";
        if (nameText != null) nameText.text = $"Name: {data.cannonName}";
        if (costText != null) costText.text = $"Cost: {data.cost}";
        if (rangeText != null) rangeText.text = $"Range: {data.range:F0}m";

        SetHUDBars(data.health, data.ability, data.damage);
        HideProgressBar();
    }

    private void ShowInventoryDetails(CannonInventoryEntry entry)
    {
        if (entry == null) { ClearDetails(); return; }

        Sprite sp = entry.data.previewSprite
            ?? (entry.data.idleSprites?.Length > 0 ? entry.data.idleSprites[0] : null);
        ApplyPreviewSprite(sp);

        if (levelText != null) levelText.text = $"LEVEL {entry.DisplayLevel}";
        if (nameText != null) nameText.text = $"Name: {entry.data.cannonName}";
        if (costText != null) costText.text = $"Cost: {entry.data.cost}";
        if (rangeText != null) rangeText.text = $"Range: {entry.CurrentRange:F0}m";

        SetHUDBars(entry.CurrentHealth, entry.CurrentAbility, entry.CurrentDamage);

        // Timer — only show if currently upgrading
        if (timerText != null)
            timerText.text = entry.isUpgrading
                ? FormatTimer(entry.UpgradeTimeRemaining)
                : "00:00";
    }

    private void ClearDetails()
    {
        ApplyPreviewSprite(null);
        if (levelText != null) levelText.text = "LEVEL 1";
        if (timerText != null) timerText.text = "00:00";
        if (nameText != null) nameText.text = "Name: —";
        if (costText != null) costText.text = "";
        if (rangeText != null) rangeText.text = "";
        SetHUDBars(0f, 0f, 0f);
    }

    private void SetHUDBars(float health, float ability, float damage)
    {
        if (healthBar != null) healthBar.fillAmount = Mathf.Clamp01(health / maxHealth);
        if (abilityBar != null) abilityBar.fillAmount = Mathf.Clamp01(ability / maxAbility);
        if (damageBar != null) damageBar.fillAmount = Mathf.Clamp01(damage / maxDamage);

        if (healthValueText != null) healthValueText.text = $"{health:F0}";
        if (abilityValueText != null) abilityValueText.text = $"{ability:F0}";
        if (damageValueText != null) damageValueText.text = $"{damage:F0}";
    }

    private void ApplyPreviewSprite(Sprite s)
    {
        if (previewImage == null) return;
        previewImage.enabled = s != null;
        if (s != null) previewImage.sprite = s;
    }

    // ════════════════════════════════════════════════════════════════
    // PROGRESS BAR
    // ════════════════════════════════════════════════════════════════

    private void ShowProgressBar()
    {
        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(true);
        if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = 0f;
    }

    private void HideProgressBar()
    {
        if (upgradeProgressBG != null) upgradeProgressBG.SetActive(false);
        if (timerText != null && _mode == PanelMode.Buy) timerText.text = "00:00";
    }

    private void RefreshProgressBarForSelected(CannonInventoryEntry entry)
    {
        if (entry == null) { HideProgressBar(); return; }

        if (entry.isUpgrading)
        {
            ShowProgressBar();
            float total = Mathf.Max(0.001f, entry.data.upgradeDuration);
            float progress = 1f - Mathf.Clamp01(entry.UpgradeTimeRemaining / total);
            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
            if (timerText != null) timerText.text = FormatTimer(entry.UpgradeTimeRemaining);
        }
        else
        {
            HideProgressBar();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // COIN
    // ════════════════════════════════════════════════════════════════

    private void RefreshCoinText()
    {
        if (coinText == null) return;
        coinText.text = GameManager.Instance != null
            ? GameManager.Instance.Gold.ToString()
            : "0";
    }

    private void OnGoldChanged(int newAmount)
    {
        if (coinText != null) coinText.text = newAmount.ToString();
        if (_mode == PanelMode.Buy) RefreshBuyButton();
    }

    // ════════════════════════════════════════════════════════════════
    // WARNING / FEEDBACK
    // ════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════
    // CALLBACKS FROM CannonSlot
    // ════════════════════════════════════════════════════════════════

    /// <summary>Called by CannonSlot.Unequip() to refresh the inventory card list.</summary>
    public void RefreshAfterUnequip()
    {
        if (_mode == PanelMode.Inventory)
        {
            PopulateInventoryCards();
            CannonInventoryEntry entry = SelectedEntry;
            if (entry != null)
            {
                ShowInventoryDetails(entry);
                RefreshInventoryButtons(entry);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════

    private static string FormatTimer(float seconds)
    {
        float s = Mathf.Max(0f, seconds);
        int min = (int)(s / 60f);
        int sec = (int)(s % 60f);
        return $"{min:00}:{sec:00}";
    }

    /// <summary>Read-only access for external systems.</summary>
    public IReadOnlyList<CannonInventoryEntry> GetInventory() => _inventory;

    public int CountOwned(CannonData data)
    {
        int n = 0;
        foreach (var e in _inventory) if (e.data == data) n++;
        return n;
    }
}