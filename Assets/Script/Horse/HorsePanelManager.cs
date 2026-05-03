//////using System.Collections;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;


//////public class HorsePanelManager : MonoBehaviour
//////{
//////    public static HorsePanelManager Instance { get; private set; }

//////    public enum PanelMode { Buy, Inventory, Update }

//////    // ─── Inspector fields ─────────────────────────────────────────────────────

//////    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
//////    [SerializeField] private HorseData[] horseLevels;

//////    [Header("Level Buttons (same order as horseLevels)")]
//////    [SerializeField] private HorseLevelButton[] levelButtons;

//////    [Header("Preview Image")]
//////    [SerializeField] private Image previewImage;

//////    [Header("Preview Info")]
//////    [SerializeField] private TextMeshProUGUI previewNameText;
//////    [SerializeField] private TextMeshProUGUI previewAgeText;

//////    [Header("HUD bars (Image Type = Filled, Horizontal, Fill Origin Left)")]
//////    [SerializeField] private Image healthBar;
//////    [SerializeField] private TextMeshProUGUI healthText;
//////    [SerializeField] private Image abilityBar;
//////    [SerializeField] private TextMeshProUGUI abilityText;
//////    [SerializeField] private Image damageBar;
//////    [SerializeField] private TextMeshProUGUI damageText;

//////    [Header("HUD Hint Bars (duplicate stat bars, green tint, alpha ~100)")]
//////    [SerializeField] private Image healthBarHint;
//////    [SerializeField] private Image abilityBarHint;
//////    [SerializeField] private Image damageBarHint;

//////    [Header("Panel Buttons")]
//////    [SerializeField] private Button buyButton;
//////    [SerializeField] private TextMeshProUGUI buyButtonText;
//////    [SerializeField] private Button sellButton;
//////    [SerializeField] private Button updateButton;
//////    [SerializeField] private TextMeshProUGUI updateButtonText;   // "(1/3)" or "Max"
//////    [SerializeField] private Button equipButton;
//////    [Tooltip("Remove horse from slot but keep in inventory")]
//////    [SerializeField] private Button unequipButton;

//////    [Header("Upgrade Timer Label")]
//////    [Tooltip("Shows '7.3s' while a slot is upgrading")]
//////    [SerializeField] private TextMeshProUGUI upgradeTimerText;

//////    [Header("Labels")]
//////    [SerializeField] private TextMeshProUGUI horseLevelText;
//////    [SerializeField] private TextMeshProUGUI costText;

//////    [Header("Coin Text")]
//////    [SerializeField] private TextMeshProUGUI coinText;
//////    [SerializeField] private int startingGold = 100;

//////    [Header("Warning / Status")]
//////    [SerializeField] private TextMeshProUGUI warningText;

//////    // ─── Private state ────────────────────────────────────────────────────────

//////    private HorseData _selected;
//////    private float _previewTimer;
//////    private int _previewFrame;
//////    private int _gold;
//////    private bool[] _unlocked;
//////    private PanelMode _mode;
//////    private HorseSlot _updateTargetSlot;
//////    private int _selectedSellIndex = -1;
//////    private HorseLevelButton _selectedButton = null;

//////    private const float MAX_STAT = 100f;

//////    // ─── Visual FX ────────────────────────────────────────────────────────────

//////    private Coroutine _pulseCoroutine;
//////    private Coroutine _glowCoroutine;
//////    private Vector3 _previewOriginalScale = Vector3.one;

//////    // ─── Lifecycle ────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        Instance = this;
//////        _gold = startingGold;

//////        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
//////        if (_unlocked.Length > 0) _unlocked[0] = true;

//////        Wire(buyButton, OnBuyClicked);
//////        Wire(sellButton, OnSellClicked);
//////        Wire(updateButton, OnUpdateClicked);
//////        Wire(equipButton, OnEquipClicked);
//////        Wire(unequipButton, OnUnequipClicked);

//////        if (previewImage != null)
//////            _previewOriginalScale = previewImage.transform.localScale;

//////        // Initial buy-mode cards
//////        for (int i = 0; i < levelButtons.Length; i++)
//////        {
//////            if (levelButtons[i] == null) continue;
//////            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//////            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//////            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//////        }

//////        RefreshGoldText();
//////        HideWarning();
//////        HideHintBars();
//////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//////        gameObject.SetActive(false);
//////    }

//////    private void Wire(Button b, UnityEngine.Events.UnityAction a)
//////    { if (b == null) return; b.onClick.RemoveAllListeners(); b.onClick.AddListener(a); }

//////    private void Update()
//////    {
//////        TickPreview();
//////        TickUpgradeTimer();
//////    }

//////    // ─── Open: BUY ───────────────────────────────────────────────────────────
//////    // Buy mode shows horse-type cards.  ONLY the buy button is visible here.

//////    public void OpenBuyMode()
//////    {
//////        StopAllPanelFX();

//////        _mode = PanelMode.Buy;
//////        _updateTargetSlot = null;
//////        _selectedButton = null;

//////        for (int i = 0; i < levelButtons.Length; i++)
//////        {
//////            if (levelButtons[i] == null) continue;
//////            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//////            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//////            levelButtons[i].gameObject.SetActive(true);
//////            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//////        }

//////        // Buy mode — only the Buy button is shown.
//////        // Equip / Sell / Update / Unequip are all Inventory-mode actions.
//////        ShowButtons(buy: true, sell: false, update: false, equip: false, unequip: false);
//////        HideHintBars();
//////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

//////        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
//////        GameManager.Instance?.OpenHorsePanel();
//////    }

//////    // ─── Open: INVENTORY ─────────────────────────────────────────────────────
//////    // Inventory mode shows all owned horses with (N/total) badges.
//////    // Equip / Update / Sell are accessible here.

//////    public void OpenInventoryMode()
//////    {
//////        StopAllPanelFX();

//////        _mode = PanelMode.Inventory;
//////        _updateTargetSlot = null;
//////        _selectedSellIndex = -1;
//////        _selectedButton = null;

//////        // No Buy button here; sell/equip/update are shown based on selection
//////        ShowButtons(buy: false, sell: false, update: false, equip: false, unequip: false);
//////        HideHintBars();
//////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

//////        PopulateOwnedCards();

//////        _selected = null;
//////        if (previewImage != null) previewImage.enabled = false;

//////        // Auto-select the first owned horse
//////        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//////        if (owned != null && owned.Length > 0) SelectHorseForSell(owned[0], 0);

//////        GameManager.Instance?.OpenHorsePanel();
//////    }

//////    // Kept for backward compatibility — OpenInventoryMode is the canonical name.
//////    public void OpenSellMode() => OpenInventoryMode();

//////    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────
//////    // Opens when the player taps an equipped horse in the scene.

//////    public void OpenUpdateMode(HorseSlot slot)
//////    {
//////        if (slot == null || !slot.IsOccupied) return;

//////        StopAllPanelFX();

//////        _mode = PanelMode.Update;
//////        _updateTargetSlot = slot;
//////        _selectedButton = null;

//////        ShowButtons(buy: false, sell: false, update: true, equip: false, unequip: true);
//////        HideHintBars();
//////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

//////        PopulateOwnedCards();

//////        HorseData data = slot.CurrentData;
//////        _selected = data;

//////        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//////        int preselect = -1;
//////        if (owned != null)
//////            for (int i = 0; i < owned.Length; i++)
//////                if (owned[i] == data) { preselect = i; break; }

//////        _previewFrame = 0; _previewTimer = 0f;
//////        SetPreviewForData(data);
//////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
//////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//////        if (costText != null) costText.text = $"Gold: {data.cost}";
//////        HideWarning();

//////        foreach (var btn in levelButtons)
//////            btn?.SetSelectedBySellIndex(btn.SellIndex == preselect);

//////        RefreshUpdateModeHUD(slot);

//////        if (slot.IsUpgrading)
//////            _pulseCoroutine = StartCoroutine(PulseCoroutine());

//////        GameManager.Instance?.OpenHorsePanel();
//////    }

//////    // ─── Selection ────────────────────────────────────────────────────────────

//////    /// <summary>Called by HorseLevelButton when tapped in Buy mode.</summary>
//////    public void SelectHorse(HorseData data)
//////    {
//////        if (data == null) return;

//////        if (data != _selected) StopAllPanelFX();

//////        _selected = data; _previewFrame = 0; _previewTimer = 0f;

//////        SetPreviewForData(data);
//////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
//////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//////        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
//////        if (costText != null) costText.text = $"Gold: {data.cost}";
//////        HideWarning();

//////        foreach (var btn in levelButtons)
//////        {
//////            if (btn == null) continue;
//////            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn.Data == data);
//////            btn.SetSelected(sel);
//////        }

//////        if (_mode == PanelMode.Buy)
//////            ApplyBuyModeButtons(data);
//////    }

//////    /// <summary>Called by HorseLevelButton when tapped in Inventory or Update mode.</summary>
//////    public void SelectHorseForSell(HorseData data, int sellIndex)
//////    {
//////        if (data == null) return;

//////        StopAllPanelFX();

//////        _selected = data;
//////        _selectedSellIndex = sellIndex;
//////        _previewFrame = 0;
//////        _previewTimer = 0f;

//////        // In Update mode, re-point the target slot to this horse's slot
//////        if (_mode == PanelMode.Update)
//////            _updateTargetSlot = HorseArea.Instance?.FindSlotForData(data);

//////        SetPreviewForData(data);
//////        if (horseLevelText != null) horseLevelText.text = data.horseName;
//////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//////        if (costText != null) costText.text = $"Gold:  {data.cost}";
//////        HideWarning();

//////        foreach (var btn in levelButtons)
//////            btn?.SetSelectedBySellIndex(btn.SellIndex == sellIndex);

//////        if (_mode == PanelMode.Update)
//////        {
//////            if (unequipButton != null)
//////                unequipButton.gameObject.SetActive(_updateTargetSlot != null);

//////            if (_updateTargetSlot != null)
//////            {
//////                RefreshUpdateModeHUD(_updateTargetSlot);
//////                if (_updateTargetSlot.IsUpgrading)
//////                    _pulseCoroutine = StartCoroutine(PulseCoroutine());
//////            }
//////            else
//////            {
//////                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//////                HideHintBars();
//////                if (updateButton != null) updateButton.interactable = false;
//////                if (updateButtonText != null) updateButtonText.text = "Equip first";
//////                if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//////            }
//////        }
//////        else  // Inventory mode
//////        {
//////            ApplyInventoryModeButtons(data);

//////            HorseSlot slot = FindSlotForData(data);
//////            if (slot != null)
//////                RefreshHUDFromSlot(slot);
//////            else
//////                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//////        }
//////    }

//////    // ─── BUY mode: only the Buy button ───────────────────────────────────────

//////    private void ApplyBuyModeButtons(HorseData data)
//////    {
//////        bool unlocked = IsUnlocked(data);

//////        // Buy is the ONLY action available in buy mode
//////        if (buyButton != null) buyButton.gameObject.SetActive(unlocked);
//////        if (buyButtonText != null && unlocked) buyButtonText.text = $"{data.cost}";

//////        // All other buttons are hidden in buy mode
//////        if (equipButton != null) equipButton.gameObject.SetActive(false);
//////        if (updateButton != null) updateButton.gameObject.SetActive(false);
//////        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
//////        if (sellButton != null) sellButton.gameObject.SetActive(false);

//////        // HUD: show live slot stats or base stats
//////        HorseSlot liveSlot = HorseArea.Instance != null ? FindSlotForData(data) : null;
//////        if (liveSlot != null)
//////        {
//////            RefreshHUDFromSlot(liveSlot);
//////            if (liveSlot.IsUpgrading && _pulseCoroutine == null)
//////                _pulseCoroutine = StartCoroutine(PulseCoroutine());
//////            else if (!liveSlot.IsUpgrading && _pulseCoroutine != null)
//////                StopAllPanelFX();
//////        }
//////        else
//////        {
//////            RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
//////            HideHintBars();
//////            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//////        }
//////    }

//////    // ─── Inventory mode: equip / update / sell based on slot state ────────────

//////    private void ApplyInventoryModeButtons(HorseData data)
//////    {
//////        if (HorseArea.Instance == null) return;

//////        bool isEquipped = HorseArea.Instance.IsEquipped(data);
//////        bool hasFreeSlot = HorseArea.Instance.HasFreeSlot();

//////        // Sell is always visible once a horse is selected
//////        if (sellButton != null) sellButton.gameObject.SetActive(true);

//////        if (isEquipped)
//////        {
//////            // Horse is in a slot → show Update + Unequip
//////            HorseSlot slot = FindSlotForData(data);

//////            if (equipButton != null) equipButton.gameObject.SetActive(false);
//////            if (unequipButton != null) unequipButton.gameObject.SetActive(true);

//////            if (updateButton != null) updateButton.gameObject.SetActive(true);
//////            if (slot != null) RefreshUpdateButton(slot);

//////            // Show sell refund hint on button
//////            if (slot != null)
//////            {
//////                float refundPct = slot.SellRefundPercent;
//////                int refund = Mathf.RoundToInt(data.cost * refundPct);
//////                var sellText = sellButton != null
//////                    ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
//////                if (sellText != null) sellText.text = $"Sell "; //(+{refund}g)
//////            }
//////        }
//////        else
//////        {
//////            // Horse is not in a slot → show Equip (if slots available)
//////            if (equipButton != null)
//////                equipButton.gameObject.SetActive(hasFreeSlot);
//////            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
//////            if (updateButton != null) updateButton.gameObject.SetActive(false);

//////            // Base 50 % refund when not in a slot
//////            int refund = Mathf.RoundToInt(data.cost * 0.50f);
//////            var sellText = sellButton != null
//////                ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
//////            if (sellText != null) sellText.text = $"Sell ";  //(+{refund}g)
//////        }
//////    }

//////    // ─── Update mode HUD ─────────────────────────────────────────────────────

//////    private void RefreshUpdateModeHUD(HorseSlot slot)
//////    {
//////        if (slot == null || !slot.IsOccupied) return;

//////        RefreshHUDFromSlot(slot);
//////        RefreshUpdateButton(slot);

//////        if (upgradeTimerText != null)
//////        {
//////            bool upgrading = slot.IsUpgrading;
//////            upgradeTimerText.gameObject.SetActive(upgrading);
//////            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//////        }
//////    }

//////    private void RefreshHUDFromSlot(HorseSlot slot)
//////    {
//////        bool upgrading = slot.IsUpgrading;
//////        HorseData d = slot.CurrentData;
//////        RefreshHUDFromValues(
//////            slot.CurrentHealth, slot.CurrentAbility, slot.CurrentDamage,
//////            upgrading,
//////            d.upgradeHealthGain, d.upgradeAbilityGain, d.upgradeDamageGain);
//////    }

//////    private void RefreshUpdateButton(HorseSlot slot)
//////    {
//////        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !slot.IsUpgrading;
//////        bool maxed = slot.UpgradeCount >= HorseSlot.MAX_UPGRADES;

//////        if (updateButton != null) updateButton.interactable = canUpgrade;
//////        if (updateButtonText != null)
//////            updateButtonText.text = maxed ? "Max" : $"({slot.UpgradeCount}/{HorseSlot.MAX_UPGRADES})";
//////    }

//////    // ─── Shared HUD renderer ──────────────────────────────────────────────────

//////    private void RefreshHUDFromValues(float hp, float ab, float dm,
//////                                      bool upgrading,
//////                                      float hpGain, float abGain, float dmGain)
//////    {
//////        hp = Mathf.Clamp(hp, 0, MAX_STAT);
//////        ab = Mathf.Clamp(ab, 0, MAX_STAT);
//////        dm = Mathf.Clamp(dm, 0, MAX_STAT);

//////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
//////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
//////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

//////        if (healthText != null)
//////            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
//////        if (abilityText != null)
//////            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
//////        if (damageText != null)
//////            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

//////        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
//////        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
//////        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);
//////    }

//////    // ─── Per-frame tickers ────────────────────────────────────────────────────

//////    private void TickPreview()
//////    {
//////        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
//////        _previewTimer += Time.deltaTime;
//////        if (_previewTimer < 1f / _selected.idleFPS) return;
//////        _previewTimer = 0f;
//////        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
//////        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
//////    }

//////    private void TickUpgradeTimer()
//////    {
//////        HorseSlot slot = ResolveCurrentSlot();
//////        if (slot == null || !slot.IsOccupied) return;

//////        if (slot.IsUpgrading)
//////        {
//////            if (_mode == PanelMode.Update || _mode == PanelMode.Inventory)
//////                RefreshUpdateModeHUD(slot);
//////            else if (_mode == PanelMode.Buy)
//////            {
//////                if (upgradeTimerText != null)
//////                {
//////                    upgradeTimerText.gameObject.SetActive(true);
//////                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//////                }
//////                RefreshHUDFromSlot(slot);
//////            }
//////        }
//////        else
//////        {
//////            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//////        }
//////    }

//////    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

//////    public void OnSlotUpgradeComplete(HorseSlot slot)
//////    {
//////        if (slot != ResolveCurrentSlot()) return;

//////        StopAllPanelFX();
//////        _glowCoroutine = StartCoroutine(GlowCoroutine());

//////        RefreshUpdateModeHUD(slot);
//////        ShowWarning($"'{slot.CurrentData.horseName}' upgrade complete!");
//////    }

//////    // ─── Button actions ───────────────────────────────────────────────────────

//////    private void OnBuyClicked()
//////    {
//////        if (_selected == null) return;
//////        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

//////        _gold -= _selected.cost;
//////        RefreshGoldText();

//////        HorseArea.Instance.BuyHorse(_selected);
//////        MarkUnlockNext(_selected);

//////        int total = HorseArea.Instance.CountOwned(_selected);
//////        ShowWarning($"'{_selected.horseName}' bought! You own {total}x. " +
//////                    $"Go to Inventory to equip.");

//////        _selectedButton = null;
//////        SelectHorse(_selected);
//////    }

//////    private void MarkUnlockNext(HorseData bought)
//////    {
//////        for (int i = 0; i < horseLevels.Length; i++)
//////        {
//////            if (horseLevels[i] != bought) continue;
//////            int next = i + 1;
//////            if (next < _unlocked.Length)
//////            {
//////                _unlocked[next] = true;
//////                if (next < levelButtons.Length) levelButtons[next].SetLocked(false);
//////            }
//////            break;
//////        }
//////    }

//////    private void OnEquipClicked()
//////    {
//////        if (_selected == null) return;

//////        if (!HorseArea.Instance.HasFreeSlot())
//////        { ShowWarning("No free slot! Unequip a horse first."); return; }

//////        bool ok = HorseArea.Instance.EquipHorse(_selected, _updateTargetSlot);
//////        if (ok)
//////        {
//////            ShowWarning($"'{_selected.horseName}' equipped!");
//////            // Refresh inventory cards so the badge and buttons update
//////            PopulateOwnedCards();
//////            SelectHorseForSell(_selected, _selectedSellIndex);
//////        }
//////        else ShowWarning("Could not equip — no free slot.");
//////    }

//////    private void OnUnequipClicked()
//////    {
//////        if (_updateTargetSlot == null || !_updateTargetSlot.IsOccupied)
//////        { ShowWarning("No horse to unequip."); return; }

//////        HorseData data = _updateTargetSlot.CurrentData;
//////        _updateTargetSlot.UnequipHorse();

//////        ShowWarning($"'{data.horseName}' unequipped.");
//////        _updateTargetSlot = null;

//////        Invoke(nameof(DelayedClose), 1.0f);
//////    }

//////    private void OnSellClicked()
//////    {
//////        if (_selected == null) { ShowWarning("Select a horse to sell!"); return; }

//////        HorseSlot slot = FindSlotForData(_selected);
//////        float refundPct = slot != null ? slot.SellRefundPercent : 0.50f;
//////        int refund = Mathf.RoundToInt(_selected.cost * refundPct);

//////        HorseArea.Instance?.SellHorse(_selected);
//////        _gold += refund;
//////        RefreshGoldText();

//////        ShowWarning($"Sold '{_selected.horseName}' for {refund}g ({refundPct * 100:F0}% refund).");
//////        _selected = null;
//////        if (previewImage != null) previewImage.enabled = false;
//////        Invoke(nameof(DelayedClose), 1.2f);
//////    }

//////    private void OnUpdateClicked()
//////    {
//////        if (_selected == null) return;

//////        HorseSlot slot = ResolveCurrentSlot();
//////        if (slot == null) { ShowWarning("Equip this horse to a slot first!"); return; }

//////        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
//////        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

//////        if (!slot.StartUpgrade())
//////        {
//////            ShowWarning(slot.UpgradeCount >= HorseSlot.MAX_UPGRADES
//////                ? "Already at max level!"
//////                : "Upgrade already in progress!");
//////            return;
//////        }

//////        _gold -= upgradeCost;
//////        RefreshGoldText();
//////        ShowWarning($"Upgrading '{_selected.horseName}'…");
//////        RefreshUpdateModeHUD(slot);

//////        StopAllPanelFX();
//////        _pulseCoroutine = StartCoroutine(PulseCoroutine());
//////    }

//////    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();
//////    private void DelayedCloseToBuy() => GameManager.Instance?.CloseHorsePanel();

//////    // ─── Helpers ─────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Populates the card grid with owned horses only (no lock overlays).
//////    /// Each card gets a (typeIndex/typeTotal) badge computed from the owned list.
//////    ///
//////    /// Example — if you own  [Brown, Brown, Black]:
//////    ///   card 0 → "Brown Horse (1/2)"
//////    ///   card 1 → "Brown Horse (2/2)"
//////    ///   card 2 → "Black Horse (1/1)"
//////    /// </summary>
//////    private void PopulateOwnedCards()
//////    {
//////        foreach (var btn in levelButtons)
//////            if (btn != null) btn.gameObject.SetActive(false);

//////        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//////        int count = owned != null ? owned.Length : 0;

//////        for (int i = 0; i < levelButtons.Length && i < count; i++)
//////        {
//////            if (levelButtons[i] == null) continue;

//////            HorseData data = owned[i];
//////            int typeTotal = HorseArea.Instance.CountOwned(data);

//////            // Count how many of the same type appear at or before index i
//////            int typeIndex = 0;
//////            for (int j = 0; j <= i; j++)
//////                if (owned[j] == data) typeIndex++;

//////            levelButtons[i].gameObject.SetActive(true);
//////            levelButtons[i].SetupForInventory(data, this, i, typeIndex, typeTotal);
//////        }
//////    }

//////    private void SetPreviewForData(HorseData data)
//////    {
//////        if (previewImage == null || data.idleSprites == null || data.idleSprites.Length == 0) return;
//////        previewImage.sprite = data.idleSprites[0];
//////        previewImage.enabled = true;
//////        previewImage.preserveAspect = true;
//////    }

//////    private HorseSlot ResolveCurrentSlot()
//////    {
//////        if (_mode == PanelMode.Update && _updateTargetSlot != null)
//////            return _updateTargetSlot;

//////        if (_selected != null)
//////            return FindSlotForData(_selected);

//////        return null;
//////    }

//////    private HorseSlot FindSlotForData(HorseData data)
//////    {
//////        if (data == null || HorseArea.Instance == null) return null;
//////        return HorseArea.Instance.FindSlotForData(data);
//////    }

//////    private void SetHintBar(Image bar, bool show, float fillAmount)
//////    {
//////        if (bar == null) return;
//////        bar.gameObject.SetActive(show);
//////        if (show) bar.fillAmount = fillAmount;
//////    }

//////    private void HideHintBars()
//////    {
//////        if (healthBarHint != null) healthBarHint.gameObject.SetActive(false);
//////        if (abilityBarHint != null) abilityBarHint.gameObject.SetActive(false);
//////        if (damageBarHint != null) damageBarHint.gameObject.SetActive(false);
//////    }

//////    /// <summary>Show exactly the buttons specified; hide all others.</summary>
//////    private void ShowButtons(bool buy, bool sell, bool update, bool equip, bool unequip)
//////    {
//////        if (buyButton != null) buyButton.gameObject.SetActive(buy);
//////        if (sellButton != null) sellButton.gameObject.SetActive(sell);
//////        if (updateButton != null) updateButton.gameObject.SetActive(update);
//////        if (equipButton != null) equipButton.gameObject.SetActive(equip);
//////        if (unequipButton != null) unequipButton.gameObject.SetActive(unequip);
//////    }

//////    // Legacy overload used by older call-sites that pass only 4 args
//////    private void ShowOnly(bool sell, bool update, bool equip, bool unequip)
//////        => ShowButtons(buy: true, sell: sell, update: update, equip: equip, unequip: unequip);

//////    private bool IsUnlocked(HorseData data)
//////    {
//////        for (int i = 0; i < horseLevels.Length; i++)
//////            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
//////        return false;
//////    }

//////    public void RefreshGoldText()
//////    { if (coinText != null) coinText.text = $"{_gold}"; }

//////    private void ShowWarning(string msg)
//////    {
//////        if (warningText == null) return;
//////        warningText.text = msg;
//////        warningText.gameObject.SetActive(true);
//////        CancelInvoke(nameof(HideWarning));
//////        Invoke(nameof(HideWarning), 2.5f);
//////    }
//////    private void HideWarning()
//////    { if (warningText != null) warningText.gameObject.SetActive(false); }

//////    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
//////    public int Gold => _gold;

//////    public void OnPanelClosed()
//////    {
//////        StopAllPanelFX();
//////        HideWarning();
//////        CancelInvoke(nameof(DelayedClose));
//////        CancelInvoke(nameof(DelayedCloseToBuy));
//////    }

//////    // ─── Visual FX ────────────────────────────────────────────────────────────

//////    private void StopAllPanelFX()
//////    {
//////        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
//////        if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }

//////        if (previewImage != null)
//////        {
//////            previewImage.transform.localScale = _previewOriginalScale;
//////            previewImage.color = Color.white;
//////        }
//////    }

//////    private IEnumerator PulseCoroutine()
//////    {
//////        while (true)
//////        {
//////            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 1.5f);
//////            if (previewImage != null)
//////                previewImage.transform.localScale = _previewOriginalScale * pulse;
//////            yield return null;
//////        }
//////    }

//////    private IEnumerator GlowCoroutine()
//////    {
//////        if (previewImage == null) yield break;

//////        previewImage.transform.localScale = _previewOriginalScale;

//////        Color gold = new Color(1f, 0.82f, 0.1f);
//////        float half = 0.45f;

//////        for (float t = 0f; t < half; t += Time.deltaTime)
//////        {
//////            previewImage.color = Color.Lerp(Color.white, gold, t / half);
//////            yield return null;
//////        }
//////        previewImage.color = gold;

//////        for (float t = 0f; t < half; t += Time.deltaTime)
//////        {
//////            previewImage.color = Color.Lerp(gold, Color.white, t / half);
//////            yield return null;
//////        }

//////        previewImage.color = Color.white;
//////        _glowCoroutine = null;
//////    }
//////}

////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

////public class HorsePanelManager : MonoBehaviour
////{
////    public static HorsePanelManager Instance { get; private set; }

////    public enum PanelMode { Buy, Inventory, Update }

////    // ─── Inspector fields ─────────────────────────────────────────────────────

////    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
////    [SerializeField] private HorseData[] horseLevels;

////    [Header("Buy-mode Level Buttons (one per horse TYPE, same order as horseLevels)")]
////    [SerializeField] private HorseLevelButton[] levelButtons;

////    // ── BUG FIX: Inventory mode uses dynamically spawned cards ────────────────
////    // Wire inventoryCardPrefab to the same prefab used for levelButtons.
////    // Wire inventoryCardContainer to the parent transform of the card grid.
////    // This removes the hard cap of levelButtons.Length on owned horses shown.
////    [Header("Inventory Cards (dynamic — set prefab + container)")]
////    [Tooltip("Prefab used to spawn one card per owned horse in Inventory mode")]
////    [SerializeField] private HorseLevelButton inventoryCardPrefab;
////    [Tooltip("Parent transform the spawned inventory cards are placed under")]
////    [SerializeField] private Transform inventoryCardContainer;

////    [Header("Preview Image")]
////    [SerializeField] private Image previewImage;

////    [Header("Preview Info")]
////    [SerializeField] private TextMeshProUGUI previewNameText;
////    [SerializeField] private TextMeshProUGUI previewAgeText;

////    [Header("HUD bars (Image Type = Filled, Horizontal, Fill Origin Left)")]
////    [SerializeField] private Image healthBar;
////    [SerializeField] private TextMeshProUGUI healthText;
////    [SerializeField] private Image abilityBar;
////    [SerializeField] private TextMeshProUGUI abilityText;
////    [SerializeField] private Image damageBar;
////    [SerializeField] private TextMeshProUGUI damageText;

////    [Header("HUD Hint Bars (duplicate stat bars, green tint, alpha ~100)")]
////    [SerializeField] private Image healthBarHint;
////    [SerializeField] private Image abilityBarHint;
////    [SerializeField] private Image damageBarHint;

////    [Header("Panel Buttons")]
////    [SerializeField] private Button buyButton;
////    [SerializeField] private TextMeshProUGUI buyButtonText;
////    [SerializeField] private Button sellButton;
////    [SerializeField] private Button updateButton;
////    [SerializeField] private TextMeshProUGUI updateButtonText;
////    [SerializeField] private Button equipButton;
////    [Tooltip("Remove horse from slot but keep in inventory")]
////    [SerializeField] private Button unequipButton;

////    [Header("Upgrade Timer Label")]
////    [SerializeField] private TextMeshProUGUI upgradeTimerText;

////    [Header("Labels")]
////    [SerializeField] private TextMeshProUGUI horseLevelText;
////    [SerializeField] private TextMeshProUGUI costText;

////    [Header("Coin Text")]
////    [SerializeField] private TextMeshProUGUI coinText;
////    [SerializeField] private int startingGold = 100;

////    [Header("Warning / Status")]
////    [SerializeField] private TextMeshProUGUI warningText;

////    // ─── Private state ────────────────────────────────────────────────────────

////    private HorseData _selected;
////    private float _previewTimer;
////    private int _previewFrame;
////    private int _gold;
////    private bool[] _unlocked;
////    private PanelMode _mode;
////    private HorseSlot _updateTargetSlot;

////    // BUG FIX: _selectedInventoryId replaces _selectedSellIndex.
////    // It is the unique ID (from HorseArea._ownedIds) of the selected horse copy,
////    // NOT a list index. This survives list mutations and uniquely identifies each copy.
////    private int _selectedInventoryId = -1;

////    private HorseLevelButton _selectedButton = null;

////    // Dynamically spawned inventory cards (destroyed and recreated each time)
////    private List<HorseLevelButton> _inventoryCards = new List<HorseLevelButton>();

////    private const float MAX_STAT = 100f;

////    // ─── Visual FX ────────────────────────────────────────────────────────────

////    private Coroutine _pulseCoroutine;
////    private Coroutine _glowCoroutine;
////    private Vector3 _previewOriginalScale = Vector3.one;

////    // ─── Lifecycle ────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        Instance = this;
////        _gold = startingGold;

////        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
////        if (_unlocked.Length > 0) _unlocked[0] = true;

////        Wire(buyButton, OnBuyClicked);
////        Wire(sellButton, OnSellClicked);
////        Wire(updateButton, OnUpdateClicked);
////        Wire(equipButton, OnEquipClicked);
////        Wire(unequipButton, OnUnequipClicked);

////        if (previewImage != null)
////            _previewOriginalScale = previewImage.transform.localScale;

////        // Set up buy-mode cards
////        for (int i = 0; i < levelButtons.Length; i++)
////        {
////            if (levelButtons[i] == null) continue;
////            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
////            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
////            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
////        }

////        RefreshGoldText();
////        HideWarning();
////        HideHintBars();
////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
////        gameObject.SetActive(false);
////    }

////    private void Wire(Button b, UnityEngine.Events.UnityAction a)
////    { if (b == null) return; b.onClick.RemoveAllListeners(); b.onClick.AddListener(a); }

////    private void Update()
////    {
////        TickPreview();
////        TickUpgradeTimer();
////    }

////    // ─── Open: BUY ───────────────────────────────────────────────────────────

////    public void OpenBuyMode()
////    {
////        StopAllPanelFX();
////        DestroyInventoryCards();

////        _mode = PanelMode.Buy;
////        _updateTargetSlot = null;
////        _selectedButton = null;

////        for (int i = 0; i < levelButtons.Length; i++)
////        {
////            if (levelButtons[i] == null) continue;
////            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
////            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
////            levelButtons[i].gameObject.SetActive(true);
////            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
////        }

////        ShowButtons(buy: true, sell: false, update: false, equip: false, unequip: false);
////        HideHintBars();
////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

////        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
////        GameManager.Instance?.OpenHorsePanel();
////    }

////    // ─── Open: INVENTORY ─────────────────────────────────────────────────────

////    public void OpenInventoryMode()
////    {
////        StopAllPanelFX();

////        _mode = PanelMode.Inventory;
////        _updateTargetSlot = null;
////        _selectedInventoryId = -1;
////        _selectedButton = null;

////        ShowButtons(buy: false, sell: false, update: false, equip: false, unequip: false);
////        HideHintBars();
////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

////        // Hide buy-mode static buttons; inventory uses dynamic cards
////        foreach (var btn in levelButtons)
////            if (btn != null) btn.gameObject.SetActive(false);

////        PopulateOwnedCards();

////        _selected = null;
////        if (previewImage != null) previewImage.enabled = false;

////        // Auto-select the first owned horse
////        if (_inventoryCards.Count > 0)
////        {
////            var first = _inventoryCards[0];
////            SelectHorseForSell(first.Data, first.SellIndex);
////        }

////        GameManager.Instance?.OpenHorsePanel();
////    }

////    // Backward-compat alias
////    public void OpenSellMode() => OpenInventoryMode();

////    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

////    public void OpenUpdateMode(HorseSlot slot)
////    {
////        if (slot == null || !slot.IsOccupied) return;

////        StopAllPanelFX();
////        DestroyInventoryCards();

////        _mode = PanelMode.Update;
////        _updateTargetSlot = slot;
////        _selectedButton = null;

////        ShowButtons(buy: false, sell: false, update: true, equip: false, unequip: true);
////        HideHintBars();
////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

////        // Hide buy-mode static buttons
////        foreach (var btn in levelButtons)
////            if (btn != null) btn.gameObject.SetActive(false);

////        PopulateOwnedCards();

////        HorseData data = slot.CurrentData;
////        _selected = data;
////        _selectedInventoryId = slot.InventoryIndex;

////        _previewFrame = 0; _previewTimer = 0f;
////        SetPreviewForData(data);
////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
////        if (costText != null) costText.text = $"Gold: {data.cost}";
////        HideWarning();

////        // Highlight the matching inventory card
////        foreach (var card in _inventoryCards)
////            card?.SetSelectedBySellIndex(card.SellIndex == _selectedInventoryId);

////        RefreshUpdateModeHUD(slot);

////        if (slot.IsUpgrading)
////            _pulseCoroutine = StartCoroutine(PulseCoroutine());

////        GameManager.Instance?.OpenHorsePanel();
////    }

////    // ─── Selection ────────────────────────────────────────────────────────────

////    /// <summary>Called by HorseLevelButton when tapped in Buy mode.</summary>
////    public void SelectHorse(HorseData data)
////    {
////        if (data == null) return;
////        if (data != _selected) StopAllPanelFX();

////        _selected = data; _previewFrame = 0; _previewTimer = 0f;

////        SetPreviewForData(data);
////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
////        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
////        if (costText != null) costText.text = $"Gold: {data.cost}";
////        HideWarning();

////        foreach (var btn in levelButtons)
////        {
////            if (btn == null) continue;
////            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn.Data == data);
////            btn.SetSelected(sel);
////        }

////        if (_mode == PanelMode.Buy)
////            ApplyBuyModeButtons(data);
////    }

////    /// <summary>
////    /// Called by HorseLevelButton when tapped in Inventory or Update mode.
////    /// sellIndex here is the unique inventory ID (not a list position).
////    /// </summary>
////    public void SelectHorseForSell(HorseData data, int inventoryId)
////    {
////        if (data == null) return;

////        StopAllPanelFX();

////        _selected = data;
////        _selectedInventoryId = inventoryId;
////        _previewFrame = 0;
////        _previewTimer = 0f;

////        // In Update mode, re-point the target slot to this copy's slot
////        if (_mode == PanelMode.Update)
////            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

////        SetPreviewForData(data);
////        if (horseLevelText != null) horseLevelText.text = data.horseName;
////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
////        if (costText != null) costText.text = $"Gold:  {data.cost}";
////        HideWarning();

////        // Highlight only the card whose SellIndex matches this unique ID
////        foreach (var card in _inventoryCards)
////            card?.SetSelectedBySellIndex(card.SellIndex == inventoryId);

////        if (_mode == PanelMode.Update)
////        {
////            if (unequipButton != null)
////                unequipButton.gameObject.SetActive(_updateTargetSlot != null);

////            if (_updateTargetSlot != null)
////            {
////                RefreshUpdateModeHUD(_updateTargetSlot);
////                if (_updateTargetSlot.IsUpgrading)
////                    _pulseCoroutine = StartCoroutine(PulseCoroutine());
////            }
////            else
////            {
////                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
////                HideHintBars();
////                if (updateButton != null) updateButton.interactable = false;
////                if (updateButtonText != null) updateButtonText.text = "Equip first";
////                if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
////            }
////        }
////        else  // Inventory mode
////        {
////            ApplyInventoryModeButtons(data, inventoryId);

////            HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
////            if (slot != null)
////                RefreshHUDFromSlot(slot);
////            else
////                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
////        }
////    }

////    // ─── BUY mode buttons ────────────────────────────────────────────────────

////    private void ApplyBuyModeButtons(HorseData data)
////    {
////        bool unlocked = IsUnlocked(data);

////        if (buyButton != null) buyButton.gameObject.SetActive(unlocked);
////        if (buyButtonText != null && unlocked) buyButtonText.text = $"{data.cost}";

////        if (equipButton != null) equipButton.gameObject.SetActive(false);
////        if (updateButton != null) updateButton.gameObject.SetActive(false);
////        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
////        if (sellButton != null) sellButton.gameObject.SetActive(false);

////        HorseSlot liveSlot = HorseArea.Instance != null ? FindSlotForData(data) : null;
////        if (liveSlot != null)
////        {
////            RefreshHUDFromSlot(liveSlot);
////            if (liveSlot.IsUpgrading && _pulseCoroutine == null)
////                _pulseCoroutine = StartCoroutine(PulseCoroutine());
////            else if (!liveSlot.IsUpgrading && _pulseCoroutine != null)
////                StopAllPanelFX();
////        }
////        else
////        {
////            RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
////            HideHintBars();
////            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
////        }
////    }

////    // ─── Inventory mode buttons ───────────────────────────────────────────────

////    /// <summary>
////    /// BUG FIX: now checks IsEquippedByIndex(inventoryId) so that equipping
////    /// Brown #0 does NOT make Brown #1 and Brown #2 appear equipped too.
////    /// </summary>
////    private void ApplyInventoryModeButtons(HorseData data, int inventoryId)
////    {
////        if (HorseArea.Instance == null) return;

////        // Use ID-based check — reference-equality check was the source of bug 2
////        bool isEquipped = HorseArea.Instance.IsEquippedByIndex(inventoryId);
////        bool hasFreeSlot = HorseArea.Instance.HasFreeSlot();

////        if (sellButton != null) sellButton.gameObject.SetActive(true);

////        if (isEquipped)
////        {
////            HorseSlot slot = HorseArea.Instance.FindSlotForIndex(inventoryId);

////            if (equipButton != null) equipButton.gameObject.SetActive(false);
////            if (unequipButton != null) unequipButton.gameObject.SetActive(true);
////            if (updateButton != null) updateButton.gameObject.SetActive(true);
////            if (slot != null) RefreshUpdateButton(slot);

////            if (slot != null)
////            {
////                float refundPct = slot.SellRefundPercent;
////                int refund = Mathf.RoundToInt(data.cost * refundPct);
////                var sellText = sellButton != null
////                    ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
////                if (sellText != null) sellText.text = $"Sell (+{refund}g)";
////            }
////        }
////        else
////        {
////            if (equipButton != null) equipButton.gameObject.SetActive(hasFreeSlot);
////            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
////            if (updateButton != null) updateButton.gameObject.SetActive(false);

////            var sellText = sellButton != null
////                ? sellButton.GetComponentInChildren<TextMeshProUGUI>() : null;
////            if (sellText != null) sellText.text = "Sell";
////        }
////    }

////    // ─── Populate inventory cards (BUG FIX: dynamic, not capped by array size) ─

////    /// <summary>
////    /// Destroys previously spawned inventory cards and spawns one card per
////    /// owned horse.  No longer capped by levelButtons.Length.
////    ///
////    /// Each card's SellIndex is set to the horse's unique inventory ID
////    /// (from HorseArea.GetInventoryId), NOT its list position, so that
////    /// IsEquippedByIndex and FindSlotForIndex look up the right slot.
////    ///
////    /// Example — own [Brown(id=0), Brown(id=1), Black(id=2), White(id=3)]:
////    ///   card 0 → "Brown Horse (1/2)"  SellIndex=0
////    ///   card 1 → "Brown Horse (2/2)"  SellIndex=1
////    ///   card 2 → "Black Horse (1/1)"  SellIndex=2
////    ///   card 3 → "White Horse (1/1)"  SellIndex=3
////    /// </summary>
////    private void PopulateOwnedCards()
////    {
////        DestroyInventoryCards();

////        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
////        int count = owned != null ? owned.Length : 0;
////        if (count == 0) return;

////        // Resolve the container and prefab to use
////        Transform container = inventoryCardContainer;
////        if (container == null && levelButtons.Length > 0 && levelButtons[0] != null)
////            container = levelButtons[0].transform.parent;
////        if (container == null) container = transform;

////        HorseLevelButton prefab = inventoryCardPrefab;
////        // Fallback: clone the first static level button as the prefab
////        if (prefab == null && levelButtons.Length > 0 && levelButtons[0] != null)
////            prefab = levelButtons[0];

////        if (prefab == null)
////        {
////            Debug.LogError("[HorsePanelManager] No inventoryCardPrefab set and no levelButtons to fall back to!");
////            return;
////        }

////        for (int i = 0; i < count; i++)
////        {
////            HorseData data = owned[i];
////            int inventoryId = HorseArea.Instance.GetInventoryId(i);

////            int typeTotal = HorseArea.Instance.CountOwned(data);
////            int typeIndex = 0;
////            for (int j = 0; j <= i; j++)
////                if (owned[j] == data) typeIndex++;

////            // Instantiate a fresh card
////            HorseLevelButton card = Instantiate(prefab, container);
////            card.gameObject.SetActive(true);

////            // SellIndex stores the unique inventory ID (not list position)
////            card.SetupForInventory(data, this, inventoryId, typeIndex, typeTotal);
////            _inventoryCards.Add(card);
////        }
////    }

////    /// <summary>Destroys all dynamically spawned inventory cards.</summary>
////    private void DestroyInventoryCards()
////    {
////        foreach (var card in _inventoryCards)
////            if (card != null) Destroy(card.gameObject);
////        _inventoryCards.Clear();
////    }

////    // ─── Update mode HUD ─────────────────────────────────────────────────────

////    private void RefreshUpdateModeHUD(HorseSlot slot)
////    {
////        if (slot == null || !slot.IsOccupied) return;
////        RefreshHUDFromSlot(slot);
////        RefreshUpdateButton(slot);

////        if (upgradeTimerText != null)
////        {
////            bool upgrading = slot.IsUpgrading;
////            upgradeTimerText.gameObject.SetActive(upgrading);
////            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
////        }
////    }

////    private void RefreshHUDFromSlot(HorseSlot slot)
////    {
////        bool upgrading = slot.IsUpgrading;
////        HorseData d = slot.CurrentData;
////        RefreshHUDFromValues(
////            slot.CurrentHealth, slot.CurrentAbility, slot.CurrentDamage,
////            upgrading,
////            d.upgradeHealthGain, d.upgradeAbilityGain, d.upgradeDamageGain);
////    }

////    private void RefreshUpdateButton(HorseSlot slot)
////    {
////        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !slot.IsUpgrading;
////        bool maxed = slot.UpgradeCount >= HorseSlot.MAX_UPGRADES;
////        if (updateButton != null) updateButton.interactable = canUpgrade;
////        if (updateButtonText != null)
////            updateButtonText.text = maxed ? "Max" : $"({slot.UpgradeCount}/{HorseSlot.MAX_UPGRADES})";
////    }

////    // ─── Shared HUD renderer ──────────────────────────────────────────────────

////    private void RefreshHUDFromValues(float hp, float ab, float dm,
////                                      bool upgrading,
////                                      float hpGain, float abGain, float dmGain)
////    {
////        hp = Mathf.Clamp(hp, 0, MAX_STAT);
////        ab = Mathf.Clamp(ab, 0, MAX_STAT);
////        dm = Mathf.Clamp(dm, 0, MAX_STAT);

////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

////        if (healthText != null)
////            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
////        if (abilityText != null)
////            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
////        if (damageText != null)
////            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

////        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);
////    }

////    // ─── Per-frame tickers ────────────────────────────────────────────────────

////    private void TickPreview()
////    {
////        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
////        _previewTimer += Time.deltaTime;
////        if (_previewTimer < 1f / _selected.idleFPS) return;
////        _previewTimer = 0f;
////        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
////        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
////    }

////    private void TickUpgradeTimer()
////    {
////        HorseSlot slot = ResolveCurrentSlot();
////        if (slot == null || !slot.IsOccupied) return;

////        if (slot.IsUpgrading)
////        {
////            if (_mode == PanelMode.Update || _mode == PanelMode.Inventory)
////                RefreshUpdateModeHUD(slot);
////            else if (_mode == PanelMode.Buy)
////            {
////                if (upgradeTimerText != null)
////                {
////                    upgradeTimerText.gameObject.SetActive(true);
////                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
////                }
////                RefreshHUDFromSlot(slot);
////            }
////        }
////        else
////        {
////            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
////        }
////    }

////    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

////    public void OnSlotUpgradeComplete(HorseSlot slot)
////    {
////        if (slot != ResolveCurrentSlot()) return;
////        StopAllPanelFX();
////        _glowCoroutine = StartCoroutine(GlowCoroutine());
////        RefreshUpdateModeHUD(slot);
////        ShowWarning($"'{slot.CurrentData.horseName}' upgrade complete!");
////    }

////    // ─── Button actions ───────────────────────────────────────────────────────

////    private void OnBuyClicked()
////    {
////        if (_selected == null) return;
////        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

////        _gold -= _selected.cost;
////        RefreshGoldText();

////        int assignedId = HorseArea.Instance.BuyHorse(_selected);
////        MarkUnlockNext(_selected);

////        int total = HorseArea.Instance.CountOwned(_selected);
////        ShowWarning($"'{_selected.horseName}' bought (id={assignedId})! You own {total}x. " +
////                    $"Go to Inventory to equip.");

////        _selectedButton = null;
////        SelectHorse(_selected);
////    }

////    private void MarkUnlockNext(HorseData bought)
////    {
////        for (int i = 0; i < horseLevels.Length; i++)
////        {
////            if (horseLevels[i] != bought) continue;
////            int next = i + 1;
////            if (next < _unlocked.Length)
////            {
////                _unlocked[next] = true;
////                if (next < levelButtons.Length) levelButtons[next].SetLocked(false);
////            }
////            break;
////        }
////    }

////    private void OnEquipClicked()
////    {
////        if (_selected == null) return;
////        if (_selectedInventoryId < 0) { ShowWarning("Select a horse first."); return; }

////        if (!HorseArea.Instance.HasFreeSlot())
////        { ShowWarning("No free slot! Unequip a horse first."); return; }

////        // Pass the unique ID so the slot knows which copy it holds
////        bool ok = HorseArea.Instance.EquipHorse(_selected, _selectedInventoryId, _updateTargetSlot);
////        if (ok)
////        {
////            ShowWarning($"'{_selected.horseName}' equipped!");
////            PopulateOwnedCards();
////            SelectHorseForSell(_selected, _selectedInventoryId);
////        }
////        else ShowWarning("Could not equip — no free slot.");
////    }

////    private void OnUnequipClicked()
////    {
////        if (_updateTargetSlot == null || !_updateTargetSlot.IsOccupied)
////        { ShowWarning("No horse to unequip."); return; }

////        HorseData data = _updateTargetSlot.CurrentData;
////        _updateTargetSlot.UnequipHorse();

////        ShowWarning($"'{data.horseName}' unequipped.");
////        _updateTargetSlot = null;

////        Invoke(nameof(DelayedClose), 1.0f);
////    }

////    private void OnSellClicked()
////    {
////        if (_selected == null || _selectedInventoryId < 0)
////        { ShowWarning("Select a horse to sell!"); return; }

////        HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);
////        float refundPct = slot != null ? slot.SellRefundPercent : 0.50f;
////        int refund = Mathf.RoundToInt(_selected.cost * refundPct);

////        HorseArea.Instance?.SellHorse(_selected, _selectedInventoryId);
////        _gold += refund;
////        RefreshGoldText();

////        ShowWarning($"Sold '{_selected.horseName}' for {refund}g ({refundPct * 100:F0}% refund).");
////        _selected = null;
////        _selectedInventoryId = -1;
////        if (previewImage != null) previewImage.enabled = false;
////        Invoke(nameof(DelayedClose), 1.2f);
////    }

////    private void OnUpdateClicked()
////    {
////        if (_selected == null) return;

////        HorseSlot slot = ResolveCurrentSlot();
////        if (slot == null) { ShowWarning("Equip this horse to a slot first!"); return; }

////        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
////        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

////        if (!slot.StartUpgrade())
////        {
////            ShowWarning(slot.UpgradeCount >= HorseSlot.MAX_UPGRADES
////                ? "Already at max level!"
////                : "Upgrade already in progress!");
////            return;
////        }

////        _gold -= upgradeCost;
////        RefreshGoldText();
////        ShowWarning($"Upgrading '{_selected.horseName}'…");
////        RefreshUpdateModeHUD(slot);
////        StopAllPanelFX();
////        _pulseCoroutine = StartCoroutine(PulseCoroutine());
////    }

////    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();
////    private void DelayedCloseToBuy() => GameManager.Instance?.CloseHorsePanel();

////    // ─── Helpers ─────────────────────────────────────────────────────────────

////    private void SetPreviewForData(HorseData data)
////    {
////        if (previewImage == null || data.idleSprites == null || data.idleSprites.Length == 0) return;
////        previewImage.sprite = data.idleSprites[0];
////        previewImage.enabled = true;
////        previewImage.preserveAspect = true;
////    }

////    private HorseSlot ResolveCurrentSlot()
////    {
////        if (_mode == PanelMode.Update && _updateTargetSlot != null)
////            return _updateTargetSlot;

////        // Use ID-based lookup so we find the right copy
////        if (_selectedInventoryId >= 0)
////            return HorseArea.Instance?.FindSlotForIndex(_selectedInventoryId);

////        return null;
////    }

////    private HorseSlot FindSlotForData(HorseData data)
////    {
////        if (data == null || HorseArea.Instance == null) return null;
////        return HorseArea.Instance.FindSlotForData(data);
////    }

////    private void SetHintBar(Image bar, bool show, float fillAmount)
////    {
////        if (bar == null) return;
////        bar.gameObject.SetActive(show);
////        if (show) bar.fillAmount = fillAmount;
////    }

////    private void HideHintBars()
////    {
////        if (healthBarHint != null) healthBarHint.gameObject.SetActive(false);
////        if (abilityBarHint != null) abilityBarHint.gameObject.SetActive(false);
////        if (damageBarHint != null) damageBarHint.gameObject.SetActive(false);
////    }

////    private void ShowButtons(bool buy, bool sell, bool update, bool equip, bool unequip)
////    {
////        if (buyButton != null) buyButton.gameObject.SetActive(buy);
////        if (sellButton != null) sellButton.gameObject.SetActive(sell);
////        if (updateButton != null) updateButton.gameObject.SetActive(update);
////        if (equipButton != null) equipButton.gameObject.SetActive(equip);
////        if (unequipButton != null) unequipButton.gameObject.SetActive(unequip);
////    }

////    private bool IsUnlocked(HorseData data)
////    {
////        for (int i = 0; i < horseLevels.Length; i++)
////            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
////        return false;
////    }

////    public void RefreshGoldText()
////    { if (coinText != null) coinText.text = $"{_gold}"; }

////    private void ShowWarning(string msg)
////    {
////        if (warningText == null) return;
////        warningText.text = msg;
////        warningText.gameObject.SetActive(true);
////        CancelInvoke(nameof(HideWarning));
////        Invoke(nameof(HideWarning), 2.5f);
////    }
////    private void HideWarning()
////    { if (warningText != null) warningText.gameObject.SetActive(false); }

////    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
////    public int Gold => _gold;

////    public void OnPanelClosed()
////    {
////        StopAllPanelFX();
////        DestroyInventoryCards();
////        HideWarning();
////        CancelInvoke(nameof(DelayedClose));
////        CancelInvoke(nameof(DelayedCloseToBuy));
////    }

////    // ─── Visual FX ────────────────────────────────────────────────────────────

////    private void StopAllPanelFX()
////    {
////        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
////        if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
////        if (previewImage != null)
////        {
////            previewImage.transform.localScale = _previewOriginalScale;
////            previewImage.color = Color.white;
////        }
////    }

////    private IEnumerator PulseCoroutine()
////    {
////        while (true)
////        {
////            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 1.5f);
////            if (previewImage != null)
////                previewImage.transform.localScale = _previewOriginalScale * pulse;
////            yield return null;
////        }
////    }

////    private IEnumerator GlowCoroutine()
////    {
////        if (previewImage == null) yield break;
////        previewImage.transform.localScale = _previewOriginalScale;

////        Color gold = new Color(1f, 0.82f, 0.1f);
////        float half = 0.45f;

////        for (float t = 0f; t < half; t += Time.deltaTime)
////        {
////            previewImage.color = Color.Lerp(Color.white, gold, t / half);
////            yield return null;
////        }
////        previewImage.color = gold;

////        for (float t = 0f; t < half; t += Time.deltaTime)
////        {
////            previewImage.color = Color.Lerp(gold, Color.white, t / half);
////            yield return null;
////        }

////        previewImage.color = Color.white;
////        _glowCoroutine = null;
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class HorsePanelManager : MonoBehaviour
//{
//    public static HorsePanelManager Instance { get; private set; }

//    public enum PanelMode { Buy, Inventory, Update }

//    // ─── Inspector fields ─────────────────────────────────────────────────────

//    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
//    [SerializeField] private HorseData[] horseLevels;

//    [Header("Buy-mode Level Buttons (one per horse TYPE, same order as horseLevels)")]
//    [SerializeField] private HorseLevelButton[] levelButtons;

//    // ── BUG FIX: Inventory mode uses dynamically spawned cards ────────────────
//    // Wire inventoryCardPrefab to the same prefab used for levelButtons.
//    // Wire inventoryCardContainer to the parent transform of the card grid.
//    // This removes the hard cap of levelButtons.Length on owned horses shown.
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
//    [SerializeField] private TextMeshProUGUI updateButtonText;
//    [SerializeField] private Button equipButton;
//    [Tooltip("Remove horse from slot but keep in inventory")]
//    [SerializeField] private Button unequipButton;

//    [Header("Upgrade Timer Label")]
//    [SerializeField] private TextMeshProUGUI upgradeTimerText;

//    [Header("Labels")]
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

//    // BUG FIX: _selectedInventoryId replaces _selectedSellIndex.
//    // It is the unique ID (from HorseArea._ownedIds) of the selected horse copy,
//    // NOT a list index. This survives list mutations and uniquely identifies each copy.
//    private int _selectedInventoryId = -1;

//    private HorseLevelButton _selectedButton = null;

//    // Dynamically spawned inventory cards (destroyed and recreated each time)
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

//        // Set up buy-mode cards
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

//        // Hide buy-mode static buttons; inventory uses dynamic cards
//        foreach (var btn in levelButtons)
//            if (btn != null) btn.gameObject.SetActive(false);

//        PopulateOwnedCards();

//        _selected = null;
//        if (previewImage != null) previewImage.enabled = false;

//        // Auto-select the first owned horse
//        if (_inventoryCards.Count > 0)
//        {
//            var first = _inventoryCards[0];
//            SelectHorseForSell(first.Data, first.SellIndex);
//        }

//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // Backward-compat alias
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

//        // Hide buy-mode static buttons
//        foreach (var btn in levelButtons)
//            if (btn != null) btn.gameObject.SetActive(false);

//        PopulateOwnedCards();

//        HorseData data = slot.CurrentData;
//        _selected = data;
//        _selectedInventoryId = slot.InventoryIndex;

//        _previewFrame = 0; _previewTimer = 0f;
//        SetPreviewForData(data);
//        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
//        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//        if (costText != null) costText.text = $"Gold: {data.cost}";
//        HideWarning();

//        // Highlight the matching inventory card
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
//        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
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
//    /// sellIndex here is the unique inventory ID (not a list position).
//    /// </summary>
//    public void SelectHorseForSell(HorseData data, int inventoryId)
//    {
//        if (data == null) return;

//        StopAllPanelFX();

//        _selected = data;
//        _selectedInventoryId = inventoryId;
//        _previewFrame = 0;
//        _previewTimer = 0f;

//        // In Update mode, re-point the target slot to this copy's slot
//        if (_mode == PanelMode.Update)
//            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

//        SetPreviewForData(data);
//        if (horseLevelText != null) horseLevelText.text = data.horseName;
//        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
//        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
//        if (costText != null) costText.text = $"Gold:  {data.cost}";
//        HideWarning();

//        // Highlight only the card whose SellIndex matches this unique ID
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
//                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
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
//            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        }
//    }

//    // ─── Inventory mode buttons ───────────────────────────────────────────────

//    /// <summary>
//    /// BUG FIX: now checks IsEquippedByIndex(inventoryId) so that equipping
//    /// Brown #0 does NOT make Brown #1 and Brown #2 appear equipped too.
//    /// </summary>
//    private void ApplyInventoryModeButtons(HorseData data, int inventoryId)
//    {
//        if (HorseArea.Instance == null) return;

//        // Use ID-based check — reference-equality check was the source of bug 2
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
//                if (sellText != null) sellText.text = $"Sell (+{refund}g)";
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

//    // ─── Populate inventory cards (BUG FIX: dynamic, not capped by array size) ─

//    /// <summary>
//    /// Destroys previously spawned inventory cards and spawns one card per
//    /// owned horse.  No longer capped by levelButtons.Length.
//    ///
//    /// Each card's SellIndex is set to the horse's unique inventory ID
//    /// (from HorseArea.GetInventoryId), NOT its list position, so that
//    /// IsEquippedByIndex and FindSlotForIndex look up the right slot.
//    ///
//    /// Example — own [Brown(id=0), Brown(id=1), Black(id=2), White(id=3)]:
//    ///   card 0 → "Brown Horse (1/2)"  SellIndex=0
//    ///   card 1 → "Brown Horse (2/2)"  SellIndex=1
//    ///   card 2 → "Black Horse (1/1)"  SellIndex=2
//    ///   card 3 → "White Horse (1/1)"  SellIndex=3
//    /// </summary>
//    private void PopulateOwnedCards()
//    {
//        DestroyInventoryCards();

//        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//        int count = owned != null ? owned.Length : 0;
//        if (count == 0) return;

//        // Resolve the container and prefab to use
//        Transform container = inventoryCardContainer;
//        if (container == null && levelButtons.Length > 0 && levelButtons[0] != null)
//            container = levelButtons[0].transform.parent;
//        if (container == null) container = transform;

//        HorseLevelButton prefab = inventoryCardPrefab;
//        // Fallback: clone the first static level button as the prefab
//        if (prefab == null && levelButtons.Length > 0 && levelButtons[0] != null)
//            prefab = levelButtons[0];

//        if (prefab == null)
//        {
//            Debug.LogError("[HorsePanelManager] No inventoryCardPrefab set and no levelButtons to fall back to!");
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

//            // Instantiate a fresh card
//            HorseLevelButton card = Instantiate(prefab, container);
//            card.gameObject.SetActive(true);

//            // SellIndex stores the unique inventory ID (not list position)
//            card.SetupForInventory(data, this, inventoryId, typeIndex, typeTotal);
//            _inventoryCards.Add(card);
//        }
//    }

//    /// <summary>Destroys all dynamically spawned inventory cards.</summary>
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

//        if (upgradeTimerText != null)
//        {
//            bool upgrading = slot.IsUpgrading;
//            upgradeTimerText.gameObject.SetActive(upgrading);
//            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//        }
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
//                RefreshUpdateModeHUD(slot);
//            else if (_mode == PanelMode.Buy)
//            {
//                if (upgradeTimerText != null)
//                {
//                    upgradeTimerText.gameObject.SetActive(true);
//                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
//                }
//                RefreshHUDFromSlot(slot);
//            }
//        }
//        else
//        {
//            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
//        }
//    }

//    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

//    public void OnSlotUpgradeComplete(HorseSlot slot)
//    {
//        if (slot != ResolveCurrentSlot()) return;
//        StopAllPanelFX();
//        _glowCoroutine = StartCoroutine(GlowCoroutine());
//        RefreshUpdateModeHUD(slot);
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

//        // Pass the unique ID so the slot knows which copy it holds
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

//        // Go through HorseArea so upgrade progress is saved before the slot is cleared
//        HorseArea.Instance?.UnequipHorse(_updateTargetSlot);

//        ShowWarning($"'{data.horseName}' unequipped. Progress saved.");
//        _updateTargetSlot = null;

//        Invoke(nameof(DelayedClose), 1.0f);
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

//    // ─── Helpers ─────────────────────────────────────────────────────────────

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

//        // Use ID-based lookup so we find the right copy
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

public class HorsePanelManager : MonoBehaviour
{
    public static HorsePanelManager Instance { get; private set; }

    public enum PanelMode { Buy, Inventory, Update }

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
    [SerializeField] private HorseData[] horseLevels;

    [Header("Buy-mode Level Buttons (one per horse TYPE, same order as horseLevels)")]
    [SerializeField] private HorseLevelButton[] levelButtons;

    // ── BUG FIX: Inventory mode uses dynamically spawned cards ────────────────
    // Wire inventoryCardPrefab to the same prefab used for levelButtons.
    // Wire inventoryCardContainer to the parent transform of the card grid.
    // This removes the hard cap of levelButtons.Length on owned horses shown.
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
    [SerializeField] private TextMeshProUGUI updateButtonText;
    [SerializeField] private Button equipButton;
    [Tooltip("Remove horse from slot but keep in inventory")]
    [SerializeField] private Button unequipButton;

    [Header("Upgrade Timer Label")]
    [SerializeField] private TextMeshProUGUI upgradeTimerText;

    [Header("Labels")]
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

    // BUG FIX: _selectedInventoryId replaces _selectedSellIndex.
    // It is the unique ID (from HorseArea._ownedIds) of the selected horse copy,
    // NOT a list index. This survives list mutations and uniquely identifies each copy.
    private int _selectedInventoryId = -1;

    private HorseLevelButton _selectedButton = null;

    // Dynamically spawned inventory cards (destroyed and recreated each time)
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

        // Set up buy-mode cards
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

        // Hide buy-mode static buttons; inventory uses dynamic cards
        foreach (var btn in levelButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        PopulateOwnedCards();

        _selected = null;
        if (previewImage != null) previewImage.enabled = false;

        // Auto-select the first owned horse
        if (_inventoryCards.Count > 0)
        {
            var first = _inventoryCards[0];
            SelectHorseForSell(first.Data, first.SellIndex);
        }

        GameManager.Instance?.OpenHorsePanel();
    }

    // Backward-compat alias
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

        // Hide buy-mode static buttons
        foreach (var btn in levelButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        PopulateOwnedCards();

        HorseData data = slot.CurrentData;
        _selected = data;
        _selectedInventoryId = slot.InventoryIndex;

        _previewFrame = 0; _previewTimer = 0f;
        SetPreviewForData(data);
        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"Gold: {data.cost}";
        HideWarning();

        // Highlight the matching inventory card
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
        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
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
    /// sellIndex here is the unique inventory ID (not a list position).
    /// </summary>
    public void SelectHorseForSell(HorseData data, int inventoryId)
    {
        if (data == null) return;

        StopAllPanelFX();

        _selected = data;
        _selectedInventoryId = inventoryId;
        _previewFrame = 0;
        _previewTimer = 0f;

        // In Update mode, re-point the target slot to this copy's slot
        if (_mode == PanelMode.Update)
            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

        SetPreviewForData(data);
        if (horseLevelText != null) horseLevelText.text = data.horseName;
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"Gold:  {data.cost}";
        HideWarning();

        // Highlight only the card whose SellIndex matches this unique ID
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
                if (updateButton != null) updateButton.interactable = false;
                if (updateButtonText != null) updateButtonText.text = "Equip first";
                if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
            }
        }
        else  // Inventory mode
        {
            // Set the target slot so OnUnequipClicked works from inventory too
            _updateTargetSlot = HorseArea.Instance?.FindSlotForIndex(inventoryId);

            ApplyInventoryModeButtons(data, inventoryId);

            HorseSlot slot = HorseArea.Instance?.FindSlotForIndex(inventoryId);
            if (slot != null)
                RefreshHUDFromSlot(slot);
            else
                RefreshHUDFromValues(data.health, data.ability, data.damage, false, 0, 0, 0);
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
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        }
    }

    // ─── Inventory mode buttons ───────────────────────────────────────────────

    /// <summary>
    /// BUG FIX: now checks IsEquippedByIndex(inventoryId) so that equipping
    /// Brown #0 does NOT make Brown #1 and Brown #2 appear equipped too.
    /// </summary>
    private void ApplyInventoryModeButtons(HorseData data, int inventoryId)
    {
        if (HorseArea.Instance == null) return;

        // Use ID-based check — reference-equality check was the source of bug 2
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
                if (sellText != null) sellText.text = $"Sell (+{refund}g)";
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

    // ─── Populate inventory cards (BUG FIX: dynamic, not capped by array size) ─

    /// <summary>
    /// Destroys previously spawned inventory cards and spawns one card per
    /// owned horse.  No longer capped by levelButtons.Length.
    ///
    /// Each card's SellIndex is set to the horse's unique inventory ID
    /// (from HorseArea.GetInventoryId), NOT its list position, so that
    /// IsEquippedByIndex and FindSlotForIndex look up the right slot.
    ///
    /// Example — own [Brown(id=0), Brown(id=1), Black(id=2), White(id=3)]:
    ///   card 0 → "Brown Horse (1/2)"  SellIndex=0
    ///   card 1 → "Brown Horse (2/2)"  SellIndex=1
    ///   card 2 → "Black Horse (1/1)"  SellIndex=2
    ///   card 3 → "White Horse (1/1)"  SellIndex=3
    /// </summary>
    private void PopulateOwnedCards()
    {
        DestroyInventoryCards();

        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
        int count = owned != null ? owned.Length : 0;
        if (count == 0) return;

        // Resolve the container and prefab to use
        Transform container = inventoryCardContainer;
        if (container == null && levelButtons.Length > 0 && levelButtons[0] != null)
            container = levelButtons[0].transform.parent;
        if (container == null) container = transform;

        HorseLevelButton prefab = inventoryCardPrefab;
        // Fallback: clone the first static level button as the prefab
        if (prefab == null && levelButtons.Length > 0 && levelButtons[0] != null)
            prefab = levelButtons[0];

        if (prefab == null)
        {
            Debug.LogError("[HorsePanelManager] No inventoryCardPrefab set and no levelButtons to fall back to!");
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

            // Instantiate a fresh card
            HorseLevelButton card = Instantiate(prefab, container);
            card.gameObject.SetActive(true);

            // SellIndex stores the unique inventory ID (not list position)
            card.SetupForInventory(data, this, inventoryId, typeIndex, typeTotal);
            _inventoryCards.Add(card);
        }
    }

    /// <summary>Destroys all dynamically spawned inventory cards.</summary>
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

        if (upgradeTimerText != null)
        {
            bool upgrading = slot.IsUpgrading;
            upgradeTimerText.gameObject.SetActive(upgrading);
            if (upgrading) upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
        }
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
                RefreshUpdateModeHUD(slot);
            else if (_mode == PanelMode.Buy)
            {
                if (upgradeTimerText != null)
                {
                    upgradeTimerText.gameObject.SetActive(true);
                    upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
                }
                RefreshHUDFromSlot(slot);
            }
        }
        else
        {
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        }
    }

    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

    public void OnSlotUpgradeComplete(HorseSlot slot)
    {
        if (slot != ResolveCurrentSlot()) return;
        StopAllPanelFX();
        _glowCoroutine = StartCoroutine(GlowCoroutine());
        RefreshUpdateModeHUD(slot);
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

        // Pass the unique ID so the slot knows which copy it holds
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
        if (_updateTargetSlot == null || !_updateTargetSlot.IsOccupied)
        { ShowWarning("No horse to unequip."); return; }

        HorseData data = _updateTargetSlot.CurrentData;
        int unequippedId = _updateTargetSlot.InventoryIndex;

        // Go through HorseArea so upgrade progress is saved before the slot is cleared
        HorseArea.Instance?.UnequipHorse(_updateTargetSlot);
        _updateTargetSlot = null;

        ShowWarning($"'{data.horseName}' unequipped. Progress saved.");

        // In inventory mode stay in the panel and refresh so the Equip button appears.
        // In update mode (tapped from the scene slot) close the panel as before.
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

    // ─── Helpers ─────────────────────────────────────────────────────────────

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

        // Use ID-based lookup so we find the right copy
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