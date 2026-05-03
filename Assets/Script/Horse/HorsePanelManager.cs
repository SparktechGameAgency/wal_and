//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// HorsePanelManager
/////
///// CHANGES IN THIS VERSION:
/////
/////  1. LEVEL BAR always shows "Level N" where N = data.level + slot.UpgradeCount.
/////     — Buy mode   : shows base level from HorseData (e.g. "Level 1").
/////     — Inventory / Update mode: resolves the slot and adds upgrade count so
/////       an upgraded horse correctly shows "Level 2", "Level 3", etc.
/////     — The horse NAME is no longer shown in the level bar; it now lives only
/////       on the card label and in the previewNameText field.
/////
/////  2. UPGRADE PROGRESS BAR (upgradeProgressBar Image):
/////     — Must be set up as: Image Type = Filled, Fill Method = Horizontal,
/////       Fill Origin = Left.
/////     — Hidden when no upgrade is in progress.
/////     — Fills left→right in real-time as the upgrade timer counts down.
/////     — Reaches full (1.0) exactly when the upgrade completes, then hides.
///// </summary>
//public class HorsePanelManager : MonoBehaviour
//{
//    public static HorsePanelManager Instance { get; private set; }

//    public enum PanelMode { Buy, Inventory, Update }

//    // ─── Inspector fields ─────────────────────────────────────────────────────

//    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
//    [SerializeField] private HorseData[] horseLevels;

//    [Header("Buy-mode Level Buttons (one per horse TYPE, same order as horseLevels)")]
//    [SerializeField] private HorseLevelButton[] levelButtons;

//    [Header("Inventory Cards (dynamic — set prefab + container)")]
//    [Tooltip("Prefab used to spawn one card per owned horse in Inventory mode")]
//    [SerializeField] private HorseLevelButton inventoryCardPrefab;
//    [Tooltip("Parent transform the spawned inventory cards are placed under")]
//    [SerializeField] private Transform inventoryCardContainer;

//    [Header("Preview Image")]
//    [SerializeField] private Image previewImage;

//    [Header("Preview Info")]
//    [SerializeField] private TextMeshProUGUI previewNameText;
//    [SerializeField] private TextMeshProUGUI previewAgeText;

//    [Header("HUD bars (Image Type = Filled, Horizontal, Fill Origin Left)")]
//    [SerializeField] private Image healthBar;
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private Image abilityBar;
//    [SerializeField] private TextMeshProUGUI abilityText;
//    [SerializeField] private Image damageBar;
//    [SerializeField] private TextMeshProUGUI damageText;

//    [Header("HUD Hint Bars (duplicate stat bars, green tint, alpha ~100)")]
//    [SerializeField] private Image healthBarHint;
//    [SerializeField] private Image abilityBarHint;
//    [SerializeField] private Image damageBarHint;

//    [Header("Panel Buttons")]
//    [SerializeField] private Button buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button sellButton;
//    [SerializeField] private Button updateButton;
//    [SerializeField] private TextMeshProUGUI updateButtonText;   // "(1/3)" or "Max"
//    [SerializeField] private Button equipButton;
//    [Tooltip("Remove horse from slot but keep in inventory")]
//    [SerializeField] private Button unequipButton;

//    [Header("Upgrade Timer")]
//    [Tooltip("Shows '7.3s' while a slot is upgrading")]
//    [SerializeField] private TextMeshProUGUI upgradeTimerText;

//    [Header("Upgrade Progress Bar")]
//    [Tooltip("The background/container of the progress bar. Hidden when no upgrade is running.")]
//    [SerializeField] private GameObject upgradeProgressBarBackground;
//    [Tooltip("Image (Type=Filled, Horizontal, Fill Origin=Left) that fills left→right " +
//             "during an upgrade. Child of upgradeProgressBarBackground.")]
//    [SerializeField] private Image upgradeProgressBar;

//    [Header("Labels")]
//    [Tooltip("Upper-centre bar — shows 'Level N' (base level + upgrade count)")]
//    [SerializeField] private TextMeshProUGUI horseLevelText;
//    [SerializeField] private TextMeshProUGUI costText;

//    [Header("Coin Text")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private int startingGold = 100;

//    [Header("Warning / Status")]
//    [SerializeField] private TextMeshProUGUI warningText;

//    // ─── Private state ────────────────────────────────────────────────────────

//    private HorseData _selected;
//    private float _previewTimer;
//    private int _previewFrame;
//    private int _gold;
//    private bool[] _unlocked;
//    private PanelMode _mode;
//    private HorseSlot _updateTargetSlot;

//    private int _selectedInventoryId = -1;
//    private HorseLevelButton _selectedButton = null;

//    private List<HorseLevelButton> _inventoryCards = new List<HorseLevelButton>();

//    private const float MAX_STAT = 100f;

//    // ─── Visual FX ────────────────────────────────────────────────────────────

//    private Coroutine _pulseCoroutine;
//    private Coroutine _glowCoroutine;
//    private Vector3 _previewOriginalScale = Vector3.one;

//    // ─── Lifecycle ────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        Instance = this;
//        _gold = startingGold;

//        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
//        if (_unlocked.Length > 0) _unlocked[0] = true;

//        Wire(buyButton, OnBuyClicked);
//        Wire(sellButton, OnSellClicked);
//        Wire(updateButton, OnUpdateClicked);
//        Wire(equipButton, OnEquipClicked);
//        Wire(unequipButton, OnUnequipClicked);

//        if (previewImage != null)
//            _previewOriginalScale = previewImage.transform.localScale;

//        // Set up buy-mode cards (cards now show horse NAME via HorseLevelButton.Setup)
//        for (int i = 0; i < levelButtons.Length; i++)
//        {
//            if (levelButtons[i] == null) continue;
//            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//        }

//        RefreshGoldText();
//        HideWarning();
//        HideHintBars();
//        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        HideProgressBar();
//        gameObject.SetActive(false);
//    }

//    private void Wire(Button b, UnityEngine.Events.UnityAction a)
//    { if (b == null) return; b.onClick.RemoveAllListeners(); b.onClick.AddListener(a); }

//    private void Update()
//    {
//        TickPreview();
//        TickUpgradeTimer();
//    }

//    // ─── Open: BUY ───────────────────────────────────────────────────────────

//    public void OpenBuyMode()
//    {
//        StopAllPanelFX();
//        DestroyInventoryCards();

//        _mode = PanelMode.Buy;
//        _updateTargetSlot = null;
//        _selectedButton = null;

//        for (int i = 0; i < levelButtons.Length; i++)
//        {
//            if (levelButtons[i] == null) continue;
//            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//            levelButtons[i].gameObject.SetActive(true);
//            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//        }

//        ShowButtons(buy: true, sell: false, update: false, equip: false, unequip: false);
//        HideHintBars();
//        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        HideProgressBar();

//        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // ─── Open: INVENTORY ─────────────────────────────────────────────────────

//    public void OpenInventoryMode()
//    {
//        StopAllPanelFX();

//        _mode = PanelMode.Inventory;
//        _updateTargetSlot = null;
//        _selectedInventoryId = -1;
//        _selectedButton = null;

//        ShowButtons(buy: false, sell: false, update: false, equip: false, unequip: false);
//        HideHintBars();
//        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        HideProgressBar();

//        foreach (var btn in levelButtons)
//            if (btn != null) btn.gameObject.SetActive(false);

//        PopulateOwnedCards();

//        _selected = null;
//        if (previewImage != null) previewImage.enabled = false;

//        if (_inventoryCards.Count > 0)
//        {
//            var first = _inventoryCards[0];
//            SelectHorseForSell(first.Data, first.SellIndex);
//        }

//        GameManager.Instance?.OpenHorsePanel();
//    }

//    public void OpenSellMode() => OpenInventoryMode();

//    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

//    public void OpenUpdateMode(HorseSlot slot)
//    {
//        if (slot == null || !slot.IsOccupied) return;

//        StopAllPanelFX();
//        DestroyInventoryCards();

//        _mode = PanelMode.Update;
//        _updateTargetSlot = slot;
//        _selectedButton = null;

//        ShowButtons(buy: false, sell: false, update: true, equip: false, unequip: true);
//        HideHintBars();
//        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        HideProgressBar();

//        foreach (var btn in levelButtons)
//            if (btn != null) btn.gameObject.SetActive(false);

//        PopulateOwnedCards();

//        HorseData data = slot.CurrentData;
//        _selected = data;
//        _selectedInventoryId = slot.InventoryIndex;

//        _previewFrame = 0; _previewTimer = 0f;
//        SetPreviewForData(data);

//        // Level bar: base level + how many upgrades this slot has already
//        SetLevelText(data, slot);

//        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//        if (costText != null) costText.text = $"Gold: {data.cost}";
//        HideWarning();

//        foreach (var card in _inventoryCards)
//            card?.SetSelectedBySellIndex(card.SellIndex == _selectedInventoryId);

//        RefreshUpdateModeHUD(slot);

//        if (slot.IsUpgrading)
//            _pulseCoroutine = StartCoroutine(PulseCoroutine());

//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // ─── Selection ────────────────────────────────────────────────────────────

//    /// <summary>Called by HorseLevelButton when tapped in Buy mode.</summary>
//    public void SelectHorse(HorseData data)
//    {
//        if (data == null) return;
//        if (data != _selected) StopAllPanelFX();

//        _selected = data; _previewFrame = 0; _previewTimer = 0f;

//        SetPreviewForData(data);

//        // In buy mode there is no slot yet — show the base level from HorseData
//        SetLevelText(data, slot: null);

//        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
//        if (costText != null) costText.text = $"Gold: {data.cost}";
//        HideWarning();

//        foreach (var btn in levelButtons)
//        {
//            if (btn == null) continue;
//            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn.Data == data);
//            btn.SetSelected(sel);
//        }

//        if (_mode == PanelMode.Buy)
//            ApplyBuyModeButtons(data);
//    }

//    /// <summary>
//    /// Called by HorseLevelButton when tapped in Inventory or Update mode.
//    /// inventoryId is the unique ID (not a list position).
//    /// </summary>
//    public void SelectHorseForSell(HorseData data, int inventoryId)
//    {
//        if (data == null) return;

//        StopAllPanelFX();

//        _selected = data;
//        _selectedInventoryId = inventoryId;
//        _previewFrame = 0;
//        _previewTimer = 0f;

//        if (_mode == PanelMode.Update)
//            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

//        SetPreviewForData(data);

//        // Resolve the slot so we can add upgrade count to the displayed level
//        HorseSlot resolvedSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
//        SetLevelText(data, resolvedSlot);

//        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//        if (costText != null) costText.text = $"Gold: {data.cost}";
//        HideWarning();

//        foreach (var card in _inventoryCards)
//            card?.SetSelectedBySellIndex(card.SellIndex == inventoryId);

//        if (_mode == PanelMode.Update)
//        {
//            if (unequipButton != null)
//                unequipButton.gameObject.SetActive(_updateTargetSlot != null);

//            if (_updateTargetSlot != null)
//            {
//                RefreshUpdateModeHUD(_updateTargetSlot);
//                if (_updateTargetSlot.IsUpgrading)
//                    _pulseCoroutine = StartCoroutine(PulseCoroutine());
//            }
//            else
//            {
//                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//                HideHintBars();
//                HideProgressBar();
//                if (updateButton != null) updateButton.interactable = false;
//                if (updateButtonText != null) updateButtonText.text = "Equip first";
//                if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//            }
//        }
//        else  // Inventory mode
//        {
//            ApplyInventoryModeButtons(data, inventoryId);

//            HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
//            if (slot != null)
//                RefreshHUDFromSlot(slot);
//            else
//            {
//                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//                HideProgressBar();
//            }
//        }
//    }

//    // ─── BUY mode buttons ────────────────────────────────────────────────────

//    private void ApplyBuyModeButtons(HorseData data)
//    {
//        bool unlocked = IsUnlocked(data);

//        if (buyButton != null) buyButton.gameObject.SetActive(unlocked);
//        if (buyButtonText != null && unlocked) buyButtonText.text = $"{data.cost}";

//        if (equipButton != null) equipButton.gameObject.SetActive(false);
//        if (updateButton != null) updateButton.gameObject.SetActive(false);
//        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
//        if (sellButton != null) sellButton.gameObject.SetActive(false);

//        HorseSlot liveSlot = HorseArea.Instance != null ? FindSlotForData(data) : null;
//        if (liveSlot != null)
//        {
//            RefreshHUDFromSlot(liveSlot);
//            if (liveSlot.IsUpgrading && _pulseCoroutine == null)
//                _pulseCoroutine = StartCoroutine(PulseCoroutine());
//            else if (!liveSlot.IsUpgrading && _pulseCoroutine != null)
//                StopAllPanelFX();
//        }
//        else
//        {
//            RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//            HideHintBars();
//            HideProgressBar();
//            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        }
//    }

//    // ─── Inventory mode buttons ───────────────────────────────────────────────

//    private void ApplyInventoryModeButtons(HorseData data, int inventoryId)
//    {
//        if (HorseArea.Instance == null) return;

//        bool isEquipped = HorseArea.Instance.IsEquippedByIndex(inventoryId);
//        bool hasFreeSlot = HorseArea.Instance.HasFreeSlot();

//        if (sellButton != null) sellButton.gameObject.SetActive(true);

//        if (isEquipped)
//        {
//            HorseSlot slot = HorseArea.Instance.FindSlotForIndex(inventoryId);

//            if (equipButton != null) equipButton.gameObject.SetActive(false);
//            if (unequipButton != null) unequipButton.gameObject.SetActive(true);
//            if (updateButton != null) updateButton.gameObject.SetActive(true);
//            if (slot != null) RefreshUpdateButton(slot);

//            if (slot != null)
//            {
//                float refundPct = slot.SellRefundPercent;
//                int refund = Mathf.RoundToInt(data.cost * refundPct);
//                var sellText = sellButton != null
//                    ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
//                if (sellText != null) sellText.text = $"Sell"; //(+{refund}g)
//            }
//        }
//        else
//        {
//            if (equipButton != null) equipButton.gameObject.SetActive(hasFreeSlot);
//            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
//            if (updateButton != null) updateButton.gameObject.SetActive(false);

//            var sellText = sellButton != null
//                ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
//            if (sellText != null) sellText.text = "Sell";
//        }
//    }

//    // ─── Populate inventory cards ─────────────────────────────────────────────

//    private void PopulateOwnedCards()
//    {
//        DestroyInventoryCards();

//        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//        int count = owned != null ? owned.Length : 0;
//        if (count == 0) return;

//        Transform container = inventoryCardContainer;
//        if (container == null && levelButtons.Length > 0 && levelButtons[0] != null)
//            container = levelButtons[0].transform.parent;
//        if (container == null) container = transform;

//        HorseLevelButton prefab = inventoryCardPrefab;
//        if (prefab == null && levelButtons.Length > 0 && levelButtons[0] != null)
//            prefab = levelButtons[0];

//        if (prefab == null)
//        {
//            Debug.LogError("[HorsePanelManager] No inventoryCardPrefab set!");
//            return;
//        }

//        for (int i = 0; i < count; i++)
//        {
//            HorseData data = owned[i];
//            int inventoryId = HorseArea.Instance.GetInventoryId(i);

//            int typeTotal = HorseArea.Instance.CountOwned(data);
//            int typeIndex = 0;
//            for (int j = 0; j <= i; j++)
//                if (owned[j] == data) typeIndex++;

//            HorseLevelButton card = Instantiate(prefab, container);
//            card.gameObject.SetActive(true);
//            card.SetupForInventory(data, this, inventoryId, typeIndex, typeTotal);
//            _inventoryCards.Add(card);
//        }
//    }

//    private void DestroyInventoryCards()
//    {
//        foreach (var card in _inventoryCards)
//            if (card != null) Destroy(card.gameObject);
//        _inventoryCards.Clear();
//    }

//    // ─── Update mode HUD ─────────────────────────────────────────────────────

//    private void RefreshUpdateModeHUD(HorseSlot slot)
//    {
//        if (slot == null || !slot.IsOccupied) return;

//        RefreshHUDFromSlot(slot);
//        RefreshUpdateButton(slot);

//        bool upgrading = slot.IsUpgrading;

//        // Timer label
//        if (upgradeTimerText != null)
//        {
//            upgradeTimerText.gameObject.SetActive(upgrading);
//            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//        }

//        // Progress bar — fills left→right as time elapses
//        RefreshProgressBar(slot);

//        // Level bar always reflects current level (base + completed upgrades)
//        // Note: during an upgrade, UpgradeCount has NOT incremented yet, so
//        // the level shown stays at the current level until the upgrade finishes.
//        SetLevelText(slot.CurrentData, slot);
//    }

//    private void RefreshHUDFromSlot(HorseSlot slot)
//    {
//        bool upgrading = slot.IsUpgrading;
//        HorseData d = slot.CurrentData;
//        RefreshHUDFromValues(
//            slot.CurrentHealth, slot.CurrentAbility, slot.CurrentDamage,
//            upgrading,
//            d.upgradeHealthGain, d.upgradeAbilityGain, d.upgradeDamageGain);
//    }

//    private void RefreshUpdateButton(HorseSlot slot)
//    {
//        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !slot.IsUpgrading;
//        bool maxed = slot.UpgradeCount >= HorseSlot.MAX_UPGRADES;
//        if (updateButton != null) updateButton.interactable = canUpgrade;
//        if (updateButtonText != null)
//            updateButtonText.text = maxed ? "Max" : $"({slot.UpgradeCount}/{HorseSlot.MAX_UPGRADES})";
//    }

//    // ─── Shared HUD renderer ──────────────────────────────────────────────────

//    private void RefreshHUDFromValues(float hp, float ab, float dm,
//                                      bool upgrading,
//                                      float hpGain, float abGain, float dmGain)
//    {
//        hp = Mathf.Clamp(hp, 0, MAX_STAT);
//        ab = Mathf.Clamp(ab, 0, MAX_STAT);
//        dm = Mathf.Clamp(dm, 0, MAX_STAT);

//        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
//        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
//        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

//        if (healthText != null)
//            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
//        if (abilityText != null)
//            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
//        if (damageText != null)
//            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

//        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
//        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
//        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);
//    }

//    // ─── Per-frame tickers ────────────────────────────────────────────────────

//    private void TickPreview()
//    {
//        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
//        _previewTimer += Time.deltaTime;
//        if (_previewTimer < 1f / _selected.idleFPS) return;
//        _previewTimer = 0f;
//        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
//        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
//    }

//    private void TickUpgradeTimer()
//    {
//        HorseSlot slot = ResolveCurrentSlot();
//        if (slot == null || !slot.IsOccupied) return;

//        if (slot.IsUpgrading)
//        {
//            if (_mode == PanelMode.Update || _mode == PanelMode.Inventory)
//            {
//                RefreshUpdateModeHUD(slot);   // also ticks the progress bar
//            }
//            else if (_mode == PanelMode.Buy)
//            {
//                if (upgradeTimerText != null)
//                {
//                    upgradeTimerText.gameObject.SetActive(true);
//                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//                }
//                RefreshProgressBar(slot);
//                RefreshHUDFromSlot(slot);
//            }
//        }
//        else
//        {
//            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//            HideProgressBar();
//        }
//    }

//    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

//    public void OnSlotUpgradeComplete(HorseSlot slot)
//    {
//        if (slot != ResolveCurrentSlot()) return;

//        StopAllPanelFX();
//        _glowCoroutine = StartCoroutine(GlowCoroutine());
//        RefreshUpdateModeHUD(slot);
//        HideProgressBar();

//        // Level bar now shows the new, higher level
//        SetLevelText(slot.CurrentData, slot);

//        ShowWarning($"'{slot.CurrentData.horseName}' upgrade complete!");
//    }

//    // ─── Button actions ───────────────────────────────────────────────────────

//    private void OnBuyClicked()
//    {
//        if (_selected == null) return;
//        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

//        _gold -= _selected.cost;
//        RefreshGoldText();

//        int assignedId = HorseArea.Instance.BuyHorse(_selected);
//        MarkUnlockNext(_selected);

//        int total = HorseArea.Instance.CountOwned(_selected);
//        ShowWarning($"'{_selected.horseName}' bought (id={assignedId})! You own {total}x. " +
//                    $"Go to Inventory to equip.");

//        _selectedButton = null;
//        SelectHorse(_selected);
//    }

//    private void MarkUnlockNext(HorseData bought)
//    {
//        for (int i = 0; i < horseLevels.Length; i++)
//        {
//            if (horseLevels[i] != bought) continue;
//            int next = i + 1;
//            if (next < _unlocked.Length)
//            {
//                _unlocked[next] = true;
//                if (next < levelButtons.Length) levelButtons[next].SetLocked(false);
//            }
//            break;
//        }
//    }

//    private void OnEquipClicked()
//    {
//        if (_selected == null) return;
//        if (_selectedInventoryId < 0) { ShowWarning("Select a horse first."); return; }

//        if (!HorseArea.Instance.HasFreeSlot())
//        { ShowWarning("No free slot! Unequip a horse first."); return; }

//        bool ok = HorseArea.Instance.EquipHorse(_selected, _selectedInventoryId, _updateTargetSlot);
//        if (ok)
//        {
//            ShowWarning($"'{_selected.horseName}' equipped!");
//            PopulateOwnedCards();
//            SelectHorseForSell(_selected, _selectedInventoryId);
//        }
//        else ShowWarning("Could not equip — no free slot.");
//    }

//    private void OnUnequipClicked()
//    {
//        if (_updateTargetSlot == null || !_updateTargetSlot.IsOccupied)
//        { ShowWarning("No horse to unequip."); return; }

//        HorseData data = _updateTargetSlot.CurrentData;
//        int unequippedId = _updateTargetSlot.InventoryIndex;

//        HorseArea.Instance?.UnequipHorse(_updateTargetSlot);
//        _updateTargetSlot = null;

//        ShowWarning($"'{data.horseName}' unequipped. Progress saved.");

//        if (_mode == PanelMode.Inventory)
//        {
//            PopulateOwnedCards();
//            SelectHorseForSell(data, unequippedId);
//        }
//        else
//        {
//            Invoke(nameof(DelayedClose), 1.0f);
//        }
//    }

//    private void OnSellClicked()
//    {
//        if (_selected == null || _selectedInventoryId < 0)
//        { ShowWarning("Select a horse to sell!"); return; }

//        HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);
//        float refundPct = slot != null ? slot.SellRefundPercent : 0.50f;
//        int refund = Mathf.RoundToInt(_selected.cost * refundPct);

//        HorseArea.Instance?.SellHorse(_selected, _selectedInventoryId);
//        _gold += refund;
//        RefreshGoldText();

//        ShowWarning($"Sold '{_selected.horseName}' for {refund}g ({refundPct * 100:F0}% refund).");
//        _selected = null;
//        _selectedInventoryId = -1;
//        if (previewImage != null) previewImage.enabled = false;
//        Invoke(nameof(DelayedClose), 1.2f);
//    }

//    private void OnUpdateClicked()
//    {
//        if (_selected == null) return;

//        HorseSlot slot = ResolveCurrentSlot();
//        if (slot == null) { ShowWarning("Equip this horse to a slot first!"); return; }

//        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
//        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

//        if (!slot.StartUpgrade())
//        {
//            ShowWarning(slot.UpgradeCount >= HorseSlot.MAX_UPGRADES
//                ? "Already at max level!"
//                : "Upgrade already in progress!");
//            return;
//        }

//        _gold -= upgradeCost;
//        RefreshGoldText();
//        ShowWarning($"Upgrading '{_selected.horseName}'…");
//        RefreshUpdateModeHUD(slot);
//        StopAllPanelFX();
//        _pulseCoroutine = StartCoroutine(PulseCoroutine());
//    }

//    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();
//    private void DelayedCloseToBuy() => GameManager.Instance?.CloseHorsePanel();

//    // ─── Level text helper ────────────────────────────────────────────────────

//    /// <summary>
//    /// Sets the upper-centre level bar to "Level N" where N = data.level + slot.UpgradeCount.
//    /// Pass slot=null in Buy mode (no upgrades applied yet).
//    /// </summary>
//    private void SetLevelText(HorseData data, HorseSlot slot)
//    {
//        if (horseLevelText == null) return;
//        int level = data.level + (slot != null ? slot.UpgradeCount : 0);
//        horseLevelText.text = $"Level {level}";
//    }

//    // ─── Progress bar helpers ──────────────────────────────────────────────────

//    /// <summary>
//    /// Updates the upgrade progress bar fill based on how much of the upgrade
//    /// duration has elapsed.  0 = just started, 1 = finished.
//    /// </summary>
//    private void RefreshProgressBar(HorseSlot slot)
//    {
//        if (slot == null || !slot.IsUpgrading) { HideProgressBar(); return; }

//        float total = slot.CurrentData.upgradeDuration;
//        if (total <= 0f) { HideProgressBar(); return; }

//        float elapsed = total - slot.UpgradeTimeRemaining;

//        // Show background + fill bar together
//        if (upgradeProgressBarBackground != null) upgradeProgressBarBackground.SetActive(true);
//        if (upgradeProgressBar != null)
//        {
//            upgradeProgressBar.gameObject.SetActive(true);
//            upgradeProgressBar.fillAmount = Mathf.Clamp01(elapsed / total);
//        }
//    }

//    private void HideProgressBar()
//    {
//        if (upgradeProgressBarBackground != null) upgradeProgressBarBackground.SetActive(false);
//        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
//    }

//    // ─── Other helpers ────────────────────────────────────────────────────────

//    private void SetPreviewForData(HorseData data)
//    {
//        if (previewImage == null || data.idleSprites == null || data.idleSprites.Length == 0) return;
//        previewImage.sprite = data.idleSprites[0];
//        previewImage.enabled = true;
//        previewImage.preserveAspect = true;
//    }

//    private HorseSlot ResolveCurrentSlot()
//    {
//        if (_mode == PanelMode.Update && _updateTargetSlot != null)
//            return _updateTargetSlot;
//        if (_selectedInventoryId >= 0)
//            return HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);
//        return null;
//    }

//    private HorseSlot FindSlotForData(HorseData data)
//    {
//        if (data == null || HorseArea.Instance == null) return null;
//        return HorseArea.Instance.FindSlotForData(data);
//    }

//    private void SetHintBar(Image bar, bool show, float fillAmount)
//    {
//        if (bar == null) return;
//        bar.gameObject.SetActive(show);
//        if (show) bar.fillAmount = fillAmount;
//    }

//    private void HideHintBars()
//    {
//        if (healthBarHint != null) healthBarHint.gameObject.SetActive(false);
//        if (abilityBarHint != null) abilityBarHint.gameObject.SetActive(false);
//        if (damageBarHint != null) damageBarHint.gameObject.SetActive(false);
//    }

//    private void ShowButtons(bool buy, bool sell, bool update, bool equip, bool unequip)
//    {
//        if (buyButton != null) buyButton.gameObject.SetActive(buy);
//        if (sellButton != null) sellButton.gameObject.SetActive(sell);
//        if (updateButton != null) updateButton.gameObject.SetActive(update);
//        if (equipButton != null) equipButton.gameObject.SetActive(equip);
//        if (unequipButton != null) unequipButton.gameObject.SetActive(unequip);
//    }

//    private bool IsUnlocked(HorseData data)
//    {
//        for (int i = 0; i < horseLevels.Length; i++)
//            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
//        return false;
//    }

//    public void RefreshGoldText()
//    { if (coinText != null) coinText.text = $"{_gold}"; }

//    private void ShowWarning(string msg)
//    {
//        if (warningText == null) return;
//        warningText.text = msg;
//        warningText.gameObject.SetActive(true);
//        CancelInvoke(nameof(HideWarning));
//        Invoke(nameof(HideWarning), 2.5f);
//    }
//    private void HideWarning()
//    { if (warningText != null) warningText.gameObject.SetActive(false); }

//    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
//    public int Gold => _gold;

//    public void OnPanelClosed()
//    {
//        StopAllPanelFX();
//        DestroyInventoryCards();
//        HideWarning();
//        HideProgressBar();
//        CancelInvoke(nameof(DelayedClose));
//        CancelInvoke(nameof(DelayedCloseToBuy));
//    }

//    // ─── Visual FX ────────────────────────────────────────────────────────────

//    private void StopAllPanelFX()
//    {
//        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
//        if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
//        if (previewImage != null)
//        {
//            previewImage.transform.localScale = _previewOriginalScale;
//            previewImage.color = Color.white;
//        }
//    }

//    private IEnumerator PulseCoroutine()
//    {
//        while (true)
//        {
//            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 1.5f);
//            if (previewImage != null)
//                previewImage.transform.localScale = _previewOriginalScale * pulse;
//            yield return null;
//        }
//    }

//    private IEnumerator GlowCoroutine()
//    {
//        if (previewImage == null) yield break;
//        previewImage.transform.localScale = _previewOriginalScale;

//        Color gold = new Color(1f, 0.82f, 0.1f);
//        float half = 0.45f;

//        for (float t = 0f; t < half; t += Time.deltaTime)
//        {
//            previewImage.color = Color.Lerp(Color.white, gold, t / half);
//            yield return null;
//        }
//        previewImage.color = gold;

//        for (float t = 0f; t < half; t += Time.deltaTime)
//        {
//            previewImage.color = Color.Lerp(gold, Color.white, t / half);
//            yield return null;
//        }

//        previewImage.color = Color.white;
//        _glowCoroutine = null;
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HorsePanelManager
///
/// CHANGES IN THIS VERSION:
///
///  1. LEVEL BAR always shows "Level N" where N = data.level + slot.UpgradeCount.
///     — Buy mode   : shows base level from HorseData (e.g. "Level 1").
///     — Inventory / Update mode: resolves the slot and adds upgrade count so
///       an upgraded horse correctly shows "Level 2", "Level 3", etc.
///     — The horse NAME is no longer shown in the level bar; it now lives only
///       on the card label and in the previewNameText field.
///
///  2. UPGRADE PROGRESS BAR (upgradeProgressBar Image):
///     — Must be set up as: Image Type = Filled, Fill Method = Horizontal,
///       Fill Origin = Left.
///     — Hidden when no upgrade is in progress.
///     — Fills left→right in real-time as the upgrade timer counts down.
///     — Reaches full (1.0) exactly when the upgrade completes, then hides.
/// </summary>
public class HorsePanelManager : MonoBehaviour
{
    public static HorsePanelManager Instance { get; private set; }

    public enum PanelMode { Buy, Inventory, Update }

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
    [SerializeField] private HorseData[] horseLevels;

    [Header("Buy-mode Level Buttons (one per horse TYPE, same order as horseLevels)")]
    [SerializeField] private HorseLevelButton[] levelButtons;

    [Header("Inventory Cards (dynamic — set prefab + container)")]
    [Tooltip("Prefab used to spawn one card per owned horse in Inventory mode")]
    [SerializeField] private HorseLevelButton inventoryCardPrefab;
    [Tooltip("Parent transform the spawned inventory cards are placed under")]
    [SerializeField] private Transform inventoryCardContainer;

    [Header("Preview Image")]
    [SerializeField] private Image previewImage;

    [Header("Preview Info")]
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewAgeText;

    [Header("HUD bars (Image Type = Filled, Horizontal, Fill Origin Left)")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image abilityBar;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private Image damageBar;
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("HUD Hint Bars (duplicate stat bars, green tint, alpha ~100)")]
    [SerializeField] private Image healthBarHint;
    [SerializeField] private Image abilityBarHint;
    [SerializeField] private Image damageBarHint;

    [Header("Panel Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button updateButton;
    [SerializeField] private TextMeshProUGUI updateButtonText;   // "(1/3)" or "Max"
    [SerializeField] private Button equipButton;
    [Tooltip("Remove horse from slot but keep in inventory")]
    [SerializeField] private Button unequipButton;

    [Header("Upgrade Timer")]
    [Tooltip("Shows '7.3s' while a slot is upgrading")]
    [SerializeField] private TextMeshProUGUI upgradeTimerText;

    [Header("Upgrade Progress Bar")]
    [Tooltip("The background/container of the progress bar. Hidden when no upgrade is running.")]
    [SerializeField] private GameObject upgradeProgressBarBackground;
    [Tooltip("Image (Type=Filled, Horizontal, Fill Origin=Left) that fills left→right " +
             "during an upgrade. Child of upgradeProgressBarBackground.")]
    [SerializeField] private Image upgradeProgressBar;

    [Header("Labels")]
    [Tooltip("Upper-centre bar — shows 'Level N' (base level + upgrade count)")]
    [SerializeField] private TextMeshProUGUI horseLevelText;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("Coin Text")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private int startingGold = 100;

    [Header("Warning / Status")]
    [SerializeField] private TextMeshProUGUI warningText;

    // ─── Private state ────────────────────────────────────────────────────────

    private HorseData _selected;
    private float _previewTimer;
    private int _previewFrame;
    private int _gold;
    private bool[] _unlocked;
    private PanelMode _mode;
    private HorseSlot _updateTargetSlot;

    private int _selectedInventoryId = -1;
    private HorseLevelButton _selectedButton = null;

    private List<HorseLevelButton> _inventoryCards = new List<HorseLevelButton>();

    private const float MAX_STAT = 100f;

    // ─── Visual FX ────────────────────────────────────────────────────────────

    private Coroutine _pulseCoroutine;
    private Coroutine _glowCoroutine;
    private Vector3 _previewOriginalScale = Vector3.one;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        _gold = startingGold;

        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
        if (_unlocked.Length > 0) _unlocked[0] = true;

        Wire(buyButton, OnBuyClicked);
        Wire(sellButton, OnSellClicked);
        Wire(updateButton, OnUpdateClicked);
        Wire(equipButton, OnEquipClicked);
        Wire(unequipButton, OnUnequipClicked);

        if (previewImage != null)
            _previewOriginalScale = previewImage.transform.localScale;

        // Set up buy-mode cards (cards now show horse NAME via HorseLevelButton.Setup)
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
        }

        RefreshGoldText();
        HideWarning();
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        HideProgressBar();
        gameObject.SetActive(false);
    }

    private void Wire(Button b, UnityEngine.Events.UnityAction a)
    { if (b == null) return; b.onClick.RemoveAllListeners(); b.onClick.AddListener(a); }

    private void Update()
    {
        TickPreview();
        TickUpgradeTimer();
    }

    // ─── Open: BUY ───────────────────────────────────────────────────────────

    public void OpenBuyMode()
    {
        StopAllPanelFX();
        DestroyInventoryCards();

        _mode = PanelMode.Buy;
        _updateTargetSlot = null;
        _selectedButton = null;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
            levelButtons[i].gameObject.SetActive(true);
            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
        }

        ShowButtons(buy: true, sell: false, update: false, equip: false, unequip: false);
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        HideProgressBar();

        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Open: INVENTORY ─────────────────────────────────────────────────────

    public void OpenInventoryMode()
    {
        StopAllPanelFX();

        _mode = PanelMode.Inventory;
        _updateTargetSlot = null;
        _selectedInventoryId = -1;
        _selectedButton = null;

        ShowButtons(buy: false, sell: false, update: false, equip: false, unequip: false);
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        HideProgressBar();

        foreach (var btn in levelButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        PopulateOwnedCards();

        _selected = null;
        if (previewImage != null) previewImage.enabled = false;

        if (_inventoryCards.Count > 0)
        {
            var first = _inventoryCards[0];
            SelectHorseForSell(first.Data, first.SellIndex);
        }

        GameManager.Instance?.OpenHorsePanel();
    }

    public void OpenSellMode() => OpenInventoryMode();

    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

    public void OpenUpdateMode(HorseSlot slot)
    {
        if (slot == null || !slot.IsOccupied) return;

        StopAllPanelFX();
        DestroyInventoryCards();

        _mode = PanelMode.Update;
        _updateTargetSlot = slot;
        _selectedButton = null;

        ShowButtons(buy: false, sell: false, update: true, equip: false, unequip: true);
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        HideProgressBar();

        foreach (var btn in levelButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        PopulateOwnedCards();

        HorseData data = slot.CurrentData;
        _selected = data;
        _selectedInventoryId = slot.InventoryIndex;

        _previewFrame = 0; _previewTimer = 0f;
        SetPreviewForData(data);

        // Level bar: base level + how many upgrades this slot has already
        SetLevelText(data, slot);

        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"Gold: {data.cost}";
        HideWarning();

        foreach (var card in _inventoryCards)
            card?.SetSelectedBySellIndex(card.SellIndex == _selectedInventoryId);

        RefreshUpdateModeHUD(slot);

        if (slot.IsUpgrading)
            _pulseCoroutine = StartCoroutine(PulseCoroutine());

        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    /// <summary>Called by HorseLevelButton when tapped in Buy mode.</summary>
    public void SelectHorse(HorseData data)
    {
        if (data == null) return;
        if (data != _selected) StopAllPanelFX();

        _selected = data; _previewFrame = 0; _previewTimer = 0f;

        SetPreviewForData(data);

        // In buy mode there is no slot yet — show the base level from HorseData
        SetLevelText(data, slot: null);

        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
        if (costText != null) costText.text = $"Gold: {data.cost}";
        HideWarning();

        foreach (var btn in levelButtons)
        {
            if (btn == null) continue;
            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn.Data == data);
            btn.SetSelected(sel);
        }

        if (_mode == PanelMode.Buy)
            ApplyBuyModeButtons(data);
    }

    /// <summary>
    /// Called by HorseLevelButton when tapped in Inventory or Update mode.
    /// inventoryId is the unique ID (not a list position).
    /// </summary>
    public void SelectHorseForSell(HorseData data, int inventoryId)
    {
        if (data == null) return;

        StopAllPanelFX();

        _selected = data;
        _selectedInventoryId = inventoryId;
        _previewFrame = 0;
        _previewTimer = 0f;

        if (_mode == PanelMode.Update)
            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

        SetPreviewForData(data);

        // Resolve the slot so we can add upgrade count to the displayed level
        HorseSlot resolvedSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
        SetLevelText(data, resolvedSlot);

        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"Gold: {data.cost}";
        HideWarning();

        foreach (var card in _inventoryCards)
            card?.SetSelectedBySellIndex(card.SellIndex == inventoryId);

        if (_mode == PanelMode.Update)
        {
            if (unequipButton != null)
                unequipButton.gameObject.SetActive(_updateTargetSlot != null);

            if (_updateTargetSlot != null)
            {
                RefreshUpdateModeHUD(_updateTargetSlot);
                if (_updateTargetSlot.IsUpgrading)
                    _pulseCoroutine = StartCoroutine(PulseCoroutine());
            }
            else
            {
                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
                HideHintBars();
                HideProgressBar();
                if (updateButton != null) updateButton.interactable = false;
                if (updateButtonText != null) updateButtonText.text = "Equip first";
                if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
            }
        }
        else  // Inventory mode
        {
            ApplyInventoryModeButtons(data, inventoryId);

            HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
            if (slot != null)
                RefreshHUDFromSlot(slot);
            else
            {
                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
                HideProgressBar();
            }
        }
    }

    // ─── BUY mode buttons ────────────────────────────────────────────────────

    private void ApplyBuyModeButtons(HorseData data)
    {
        bool unlocked = IsUnlocked(data);

        if (buyButton != null) buyButton.gameObject.SetActive(unlocked);
        if (buyButtonText != null && unlocked) buyButtonText.text = $"{data.cost}";

        if (equipButton != null) equipButton.gameObject.SetActive(false);
        if (updateButton != null) updateButton.gameObject.SetActive(false);
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
        if (sellButton != null) sellButton.gameObject.SetActive(false);

        HorseSlot liveSlot = HorseArea.Instance != null ? FindSlotForData(data) : null;
        if (liveSlot != null)
        {
            RefreshHUDFromSlot(liveSlot);
            if (liveSlot.IsUpgrading && _pulseCoroutine == null)
                _pulseCoroutine = StartCoroutine(PulseCoroutine());
            else if (!liveSlot.IsUpgrading && _pulseCoroutine != null)
                StopAllPanelFX();
        }
        else
        {
            RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
            HideHintBars();
            HideProgressBar();
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        }
    }

    // ─── Inventory mode buttons ───────────────────────────────────────────────

    private void ApplyInventoryModeButtons(HorseData data, int inventoryId)
    {
        if (HorseArea.Instance == null) return;

        bool isEquipped = HorseArea.Instance.IsEquippedByIndex(inventoryId);
        bool hasFreeSlot = HorseArea.Instance.HasFreeSlot();

        if (sellButton != null) sellButton.gameObject.SetActive(true);

        if (isEquipped)
        {
            HorseSlot slot = HorseArea.Instance.FindSlotForIndex(inventoryId);

            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unequipButton != null) unequipButton.gameObject.SetActive(true);
            if (updateButton != null) updateButton.gameObject.SetActive(true);
            if (slot != null) RefreshUpdateButton(slot);

            if (slot != null)
            {
                float refundPct = slot.SellRefundPercent;
                int refund = Mathf.RoundToInt(data.cost * refundPct);
                var sellText = sellButton != null
                    ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
                if (sellText != null) sellText.text = $"Sell";//(+{refund}g)
            }
        }
        else
        {
            if (equipButton != null) equipButton.gameObject.SetActive(hasFreeSlot);
            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            if (updateButton != null) updateButton.gameObject.SetActive(false);

            var sellText = sellButton != null
                ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (sellText != null) sellText.text = "Sell";
        }
    }

    // ─── Populate inventory cards ─────────────────────────────────────────────

    private void PopulateOwnedCards()
    {
        DestroyInventoryCards();

        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
        int count = owned != null ? owned.Length : 0;
        if (count == 0) return;

        Transform container = inventoryCardContainer;
        if (container == null && levelButtons.Length > 0 && levelButtons[0] != null)
            container = levelButtons[0].transform.parent;
        if (container == null) container = transform;

        HorseLevelButton prefab = inventoryCardPrefab;
        if (prefab == null && levelButtons.Length > 0 && levelButtons[0] != null)
            prefab = levelButtons[0];

        if (prefab == null)
        {
            Debug.LogError("[HorsePanelManager] No inventoryCardPrefab set!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            HorseData data = owned[i];
            int inventoryId = HorseArea.Instance.GetInventoryId(i);

            int typeTotal = HorseArea.Instance.CountOwned(data);
            int typeIndex = 0;
            for (int j = 0; j <= i; j++)
                if (owned[j] == data) typeIndex++;

            HorseLevelButton card = Instantiate(prefab, container);
            card.gameObject.SetActive(true);
            card.SetupForInventory(data, this, inventoryId, typeIndex, typeTotal);
            _inventoryCards.Add(card);
        }
    }

    private void DestroyInventoryCards()
    {
        foreach (var card in _inventoryCards)
            if (card != null) Destroy(card.gameObject);
        _inventoryCards.Clear();
    }

    // ─── Update mode HUD ─────────────────────────────────────────────────────

    private void RefreshUpdateModeHUD(HorseSlot slot)
    {
        if (slot == null || !slot.IsOccupied) return;

        RefreshHUDFromSlot(slot);
        RefreshUpdateButton(slot);

        bool upgrading = slot.IsUpgrading;

        // Timer label
        if (upgradeTimerText != null)
        {
            upgradeTimerText.gameObject.SetActive(upgrading);
            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
        }

        // Progress bar — fills left→right as time elapses
        RefreshProgressBar(slot);

        // Level bar always reflects current level (base + completed upgrades)
        // Note: during an upgrade, UpgradeCount has NOT incremented yet, so
        // the level shown stays at the current level until the upgrade finishes.
        SetLevelText(slot.CurrentData, slot);
    }

    private void RefreshHUDFromSlot(HorseSlot slot)
    {
        bool upgrading = slot.IsUpgrading;
        HorseData d = slot.CurrentData;
        RefreshHUDFromValues(
            slot.CurrentHealth, slot.CurrentAbility, slot.CurrentDamage,
            upgrading,
            d.upgradeHealthGain, d.upgradeAbilityGain, d.upgradeDamageGain);
    }

    private void RefreshUpdateButton(HorseSlot slot)
    {
        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !slot.IsUpgrading;
        bool maxed = slot.UpgradeCount >= HorseSlot.MAX_UPGRADES;
        if (updateButton != null) updateButton.interactable = canUpgrade;
        if (updateButtonText != null)
            updateButtonText.text = maxed ? "Max" : $"({slot.UpgradeCount}/{HorseSlot.MAX_UPGRADES})";
    }

    // ─── Shared HUD renderer ──────────────────────────────────────────────────

    private void RefreshHUDFromValues(float hp, float ab, float dm,
                                      bool upgrading,
                                      float hpGain, float abGain, float dmGain)
    {
        hp = Mathf.Clamp(hp, 0, MAX_STAT);
        ab = Mathf.Clamp(ab, 0, MAX_STAT);
        dm = Mathf.Clamp(dm, 0, MAX_STAT);

        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

        if (healthText != null)
            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
        if (abilityText != null)
            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
        if (damageText != null)
            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);
    }

    // ─── Per-frame tickers ────────────────────────────────────────────────────

    private void TickPreview()
    {
        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
        _previewTimer += Time.deltaTime;
        if (_previewTimer < 1f / _selected.idleFPS) return;
        _previewTimer = 0f;
        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
    }

    private void TickUpgradeTimer()
    {
        HorseSlot slot = ResolveCurrentSlot();
        if (slot == null || !slot.IsOccupied) return;

        if (slot.IsUpgrading)
        {
            if (_mode == PanelMode.Update || _mode == PanelMode.Inventory)
            {
                RefreshUpdateModeHUD(slot);   // also ticks the progress bar
            }
            else if (_mode == PanelMode.Buy)
            {
                if (upgradeTimerText != null)
                {
                    upgradeTimerText.gameObject.SetActive(true);
                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
                }
                RefreshProgressBar(slot);
                RefreshHUDFromSlot(slot);
            }
        }
        else
        {
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
            HideProgressBar();
        }
    }

    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

    public void OnSlotUpgradeComplete(HorseSlot slot)
    {
        if (slot != ResolveCurrentSlot()) return;

        StopAllPanelFX();
        _glowCoroutine = StartCoroutine(GlowCoroutine());
        RefreshUpdateModeHUD(slot);
        HideProgressBar();

        // Level bar now shows the new, higher level
        SetLevelText(slot.CurrentData, slot);

        ShowWarning($"'{slot.CurrentData.horseName}' upgrade complete!");
    }

    // ─── Button actions ───────────────────────────────────────────────────────

    private void OnBuyClicked()
    {
        if (_selected == null) return;
        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

        _gold -= _selected.cost;
        RefreshGoldText();

        int assignedId = HorseArea.Instance.BuyHorse(_selected);
        MarkUnlockNext(_selected);

        int total = HorseArea.Instance.CountOwned(_selected);
        ShowWarning($"'{_selected.horseName}' bought (id={assignedId})! You own {total}x. " +
                    $"Go to Inventory to equip.");

        _selectedButton = null;
        SelectHorse(_selected);
    }

    private void MarkUnlockNext(HorseData bought)
    {
        for (int i = 0; i < horseLevels.Length; i++)
        {
            if (horseLevels[i] != bought) continue;
            int next = i + 1;
            if (next < _unlocked.Length)
            {
                _unlocked[next] = true;
                if (next < levelButtons.Length) levelButtons[next].SetLocked(false);
            }
            break;
        }
    }

    private void OnEquipClicked()
    {
        if (_selected == null) return;
        if (_selectedInventoryId < 0) { ShowWarning("Select a horse first."); return; }

        if (!HorseArea.Instance.HasFreeSlot())
        { ShowWarning("No free slot! Unequip a horse first."); return; }

        bool ok = HorseArea.Instance.EquipHorse(_selected, _selectedInventoryId, _updateTargetSlot);
        if (ok)
        {
            ShowWarning($"'{_selected.horseName}' equipped!");
            PopulateOwnedCards();
            SelectHorseForSell(_selected, _selectedInventoryId);
        }
        else ShowWarning("Could not equip — no free slot.");
    }

    private void OnUnequipClicked()
    {
        // In Inventory mode _updateTargetSlot is null — resolve from the selected ID instead
        HorseSlot slot = _updateTargetSlot;
        if (slot == null && _selectedInventoryId >= 0)
            slot = HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);

        if (slot == null || !slot.IsOccupied)
        { ShowWarning("No horse to unequip."); return; }

        HorseData data = slot.CurrentData;
        int unequippedId = slot.InventoryIndex;

        // Save upgrade progress through HorseArea before clearing the slot
        HorseArea.Instance?.UnequipHorse(slot);
        _updateTargetSlot = null;

        ShowWarning($"'{data.horseName}' unequipped. Progress saved.");

        if (_mode == PanelMode.Inventory)
        {
            PopulateOwnedCards();
            SelectHorseForSell(data, unequippedId);
        }
        else
        {
            Invoke(nameof(DelayedClose), 1.0f);
        }
    }

    private void OnSellClicked()
    {
        if (_selected == null || _selectedInventoryId < 0)
        { ShowWarning("Select a horse to sell!"); return; }

        HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);
        float refundPct = slot != null ? slot.SellRefundPercent : 0.50f;
        int refund = Mathf.RoundToInt(_selected.cost * refundPct);

        HorseArea.Instance?.SellHorse(_selected, _selectedInventoryId);
        _gold += refund;
        RefreshGoldText();

        ShowWarning($"Sold '{_selected.horseName}' for {refund}g ({refundPct * 100:F0}% refund).");
        _selected = null;
        _selectedInventoryId = -1;
        if (previewImage != null) previewImage.enabled = false;
        Invoke(nameof(DelayedClose), 1.2f);
    }

    private void OnUpdateClicked()
    {
        if (_selected == null) return;

        HorseSlot slot = ResolveCurrentSlot();
        if (slot == null) { ShowWarning("Equip this horse to a slot first!"); return; }

        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

        if (!slot.StartUpgrade())
        {
            ShowWarning(slot.UpgradeCount >= HorseSlot.MAX_UPGRADES
                ? "Already at max level!"
                : "Upgrade already in progress!");
            return;
        }

        _gold -= upgradeCost;
        RefreshGoldText();
        ShowWarning($"Upgrading '{_selected.horseName}'…");
        RefreshUpdateModeHUD(slot);
        StopAllPanelFX();
        _pulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();
    private void DelayedCloseToBuy() => GameManager.Instance?.CloseHorsePanel();

    // ─── Level text helper ────────────────────────────────────────────────────

    /// <summary>
    /// Sets the upper-centre level bar to "Level N" where N = data.level + slot.UpgradeCount.
    /// Pass slot=null in Buy mode (no upgrades applied yet).
    /// </summary>
    /// <summary>
    /// Always shows "Level 1" for a fresh horse, "Level 2" after first upgrade, etc.
    /// Uses 1 as the base so every horse type starts at Level 1 regardless of
    /// the data.level value set in the ScriptableObject.
    /// </summary>
    private void SetLevelText(HorseData data, HorseSlot slot)
    {
        if (horseLevelText == null) return;
        int level = 1 + (slot != null ? slot.UpgradeCount : 0);
        horseLevelText.text = $"Level {level}";
    }

    // ─── Progress bar helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Updates the upgrade progress bar fill based on how much of the upgrade
    /// duration has elapsed.  0 = just started, 1 = finished.
    /// </summary>
    private void RefreshProgressBar(HorseSlot slot)
    {
        if (slot == null || !slot.IsUpgrading) { HideProgressBar(); return; }

        float total = slot.CurrentData.upgradeDuration;
        if (total <= 0f) { HideProgressBar(); return; }

        float elapsed = total - slot.UpgradeTimeRemaining;

        // Show background + fill bar together
        if (upgradeProgressBarBackground != null) upgradeProgressBarBackground.SetActive(true);
        if (upgradeProgressBar != null)
        {
            upgradeProgressBar.gameObject.SetActive(true);
            upgradeProgressBar.fillAmount = Mathf.Clamp01(elapsed / total);
        }
    }

    private void HideProgressBar()
    {
        if (upgradeProgressBarBackground != null) upgradeProgressBarBackground.SetActive(false);
        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
    }

    // ─── Other helpers ────────────────────────────────────────────────────────

    private void SetPreviewForData(HorseData data)
    {
        if (previewImage == null || data.idleSprites == null || data.idleSprites.Length == 0) return;
        previewImage.sprite = data.idleSprites[0];
        previewImage.enabled = true;
        previewImage.preserveAspect = true;
    }

    private HorseSlot ResolveCurrentSlot()
    {
        if (_mode == PanelMode.Update && _updateTargetSlot != null)
            return _updateTargetSlot;
        if (_selectedInventoryId >= 0)
            return HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);
        return null;
    }

    private HorseSlot FindSlotForData(HorseData data)
    {
        if (data == null || HorseArea.Instance == null) return null;
        return HorseArea.Instance.FindSlotForData(data);
    }

    private void SetHintBar(Image bar, bool show, float fillAmount)
    {
        if (bar == null) return;
        bar.gameObject.SetActive(show);
        if (show) bar.fillAmount = fillAmount;
    }

    private void HideHintBars()
    {
        if (healthBarHint != null) healthBarHint.gameObject.SetActive(false);
        if (abilityBarHint != null) abilityBarHint.gameObject.SetActive(false);
        if (damageBarHint != null) damageBarHint.gameObject.SetActive(false);
    }

    private void ShowButtons(bool buy, bool sell, bool update, bool equip, bool unequip)
    {
        if (buyButton != null) buyButton.gameObject.SetActive(buy);
        if (sellButton != null) sellButton.gameObject.SetActive(sell);
        if (updateButton != null) updateButton.gameObject.SetActive(update);
        if (equipButton != null) equipButton.gameObject.SetActive(equip);
        if (unequipButton != null) unequipButton.gameObject.SetActive(unequip);
    }

    private bool IsUnlocked(HorseData data)
    {
        for (int i = 0; i < horseLevels.Length; i++)
            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
        return false;
    }

    public void RefreshGoldText()
    { if (coinText != null) coinText.text = $"{_gold}"; }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        warningText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideWarning));
        Invoke(nameof(HideWarning), 2.5f);
    }
    private void HideWarning()
    { if (warningText != null) warningText.gameObject.SetActive(false); }

    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
    public int Gold => _gold;

    public void OnPanelClosed()
    {
        StopAllPanelFX();
        DestroyInventoryCards();
        HideWarning();
        HideProgressBar();
        CancelInvoke(nameof(DelayedClose));
        CancelInvoke(nameof(DelayedCloseToBuy));
    }

    // ─── Visual FX ────────────────────────────────────────────────────────────

    private void StopAllPanelFX()
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
        if (previewImage != null)
        {
            previewImage.transform.localScale = _previewOriginalScale;
            previewImage.color = Color.white;
        }
    }

    private IEnumerator PulseCoroutine()
    {
        while (true)
        {
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 1.5f);
            if (previewImage != null)
                previewImage.transform.localScale = _previewOriginalScale * pulse;
            yield return null;
        }
    }

    private IEnumerator GlowCoroutine()
    {
        if (previewImage == null) yield break;
        previewImage.transform.localScale = _previewOriginalScale;

        Color gold = new Color(1f, 0.82f, 0.1f);
        float half = 0.45f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            previewImage.color = Color.Lerp(Color.white, gold, t / half);
            yield return null;
        }
        previewImage.color = gold;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            previewImage.color = Color.Lerp(gold, Color.white, t / half);
            yield return null;
        }

        previewImage.color = Color.white;
        _glowCoroutine = null;
    }
}