////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// HorsePanelManager — manages both Buy and Sell panel modes.
///////
/////// ── BUY mode ─────────────────────────────────────────────────────────────
///////  • previewCostText  ACTIVE  — "Gold: X"
///////  • sellPriceText    HIDDEN
///////  • Upgrade Button   VISIBLE — shown when the selected horse is already
///////                               owned (in a slot); hidden for unowned horses.
///////    - Upgrade timer counts down while upgrading
///////    - Hint bars show future stat fill at 50 % alpha
///////    - Stat numbers show "40 <color=green>+7</color>" while upgrading
///////
/////// ── SELL mode ────────────────────────────────────────────────────────────
///////  • previewCostText  HIDDEN
///////  • sellPriceText    ACTIVE  — "Sell: Xg"  (50 % of cost)
///////  • Upgrade Button   HIDDEN
///////
/////// ── Inspector setup for hint bars ────────────────────────────────────────
///////   Duplicate each real stat bar Image, give it a green tint + alpha ~100,
///////   name it e.g. HealthBarHint, and drag it into the matching Hint field.
/////// </summary>
////public class HorsePanelManager : MonoBehaviour
////{
////    public static HorsePanelManager Instance { get; private set; }

////    public enum PanelMode { Buy, Sell, Update }

////    // Data
////    [Header("All purchasable horse levels")]
////    [SerializeField] private HorseData[] horseLevels;

////    [Header("Level Buttons (same order as horseLevels)")]
////    [SerializeField] private HorseLevelButton[] levelButtons;

////    [Tooltip("Parent that holds ALL level-card children (scripted + locked placeholders).")]
////    [SerializeField] private Transform levelCardsParent;

////    // Preview
////    [Header("Preview Image")]
////    [SerializeField] private Image previewImage;

////    [Header("Preview Info")]
////    [Tooltip("Shown in BUY mode only — 'Gold: X'")]
////    [SerializeField] private TextMeshProUGUI previewCostText;
////    [SerializeField] private TextMeshProUGUI previewNameText;
////    [SerializeField] private TextMeshProUGUI previewAgeText;

////    // HUD bars + numbers
////    [Header("HUD Stat Bars (Image Type = Filled, Horizontal)")]
////    [SerializeField] private Image healthBar;
////    [SerializeField] private TextMeshProUGUI healthText;
////    [SerializeField] private Image abilityBar;
////    [SerializeField] private TextMeshProUGUI abilityText;
////    [SerializeField] private Image damageBar;
////    [SerializeField] private TextMeshProUGUI damageText;

////    private const float MAX_STAT = 100f;

////    // Hint bars — BUY mode, during upgrade
////    [Header("HUD Hint Bars (BUY mode during upgrade — set Image alpha ~100)")]
////    [SerializeField] private Image healthBarHint;
////    [SerializeField] private Image abilityBarHint;
////    [SerializeField] private Image damageBarHint;

////    // Action buttons
////    [Header("Panel Buttons")]
////    [SerializeField] private Button buyButton;
////    [SerializeField] private TextMeshProUGUI buyButtonText;
////    [SerializeField] private Button sellButton;

////    // Sell mode
////    [Header("Sell Mode")]
////    [Tooltip("Shown in SELL mode only — 'Sell: Xg'. Replaces the cost text.")]
////    [SerializeField] private TextMeshProUGUI sellPriceText;

////    // Buy mode — upgrade
////    [Header("Buy Mode — Upgrade (shown when selected horse is already owned)")]
////    [SerializeField] private Button upgradeButton;
////    [Tooltip("Label: 'Upgrade (1/3)' or 'Max Level'")]
////    [SerializeField] private TextMeshProUGUI upgradeButtonText;
////    [Tooltip("Countdown: 'Upgrading: 7.3s'")]
////    [SerializeField] private TextMeshProUGUI upgradeTimerText;

////    // Misc
////    [Header("Labels")]
////    [SerializeField] private TextMeshProUGUI horseLevelText;

////    [Header("Coin Text")]
////    [SerializeField] private TextMeshProUGUI coinText;
////    [SerializeField] private int startingGold = 100;

////    [Header("Warning / Status")]
////    [SerializeField] private TextMeshProUGUI warningText;

////    // Private state
////    private HorseData _selected;
////    private float _previewTimer;
////    private int _previewFrame;
////    private int _gold;
////    private bool[] _unlocked;
////    private PanelMode _mode;
////    private bool _wasUpgrading = false;

////    // Tracks which card button was tapped — fixes duplicate-data highlight bug
////    private HorseLevelButton _selectedButton = null;

////    // Tracks which slot opened Update mode
////    private HorseSlot _updateSlot = null;

////    // Lifecycle

////    private void Awake()
////    {
////        Instance = this;
////        _gold = startingGold;

////        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
////        if (_unlocked.Length > 0) _unlocked[0] = true;

////        if (buyButton != null) { buyButton.onClick.RemoveAllListeners(); buyButton.onClick.AddListener(OnBuyClicked); }
////        if (sellButton != null) { sellButton.onClick.RemoveAllListeners(); sellButton.onClick.AddListener(OnSellClicked); }
////        if (upgradeButton != null) { upgradeButton.onClick.RemoveAllListeners(); upgradeButton.onClick.AddListener(OnUpgradeClicked); }

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
////        if (sellPriceText != null) sellPriceText.gameObject.SetActive(false);
////        if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
////        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
////        gameObject.SetActive(false);
////    }

////    private void Update()
////    {
////        TickPreview();
////        TickUpgradeTimer();
////    }

////    // Open: BUY mode

////    public void OpenBuyMode()
////    {
////        _mode = PanelMode.Buy;
////        _wasUpgrading = false;
////        _selectedButton = null;
////        _updateSlot = null;

////        SetAllLevelCardsActive(true);

////        for (int i = 0; i < levelButtons.Length; i++)
////        {
////            if (levelButtons[i] == null) continue;
////            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
////            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
////            levelButtons[i].gameObject.SetActive(true);
////            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
////        }

////        SetSellButtonVisible(false);
////        if (sellPriceText != null) sellPriceText.gameObject.SetActive(false);

////        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);

////        GameManager.Instance?.OpenHorsePanel();
////    }

////    // Open: SELL mode

////    public void OpenSellMode()
////    {
////        _mode = PanelMode.Sell;
////        _wasUpgrading = false;
////        _selectedButton = null;
////        _updateSlot = null;

////        SetBuyButtonVisible(false);
////        SetSellButtonVisible(true);

////        // Upgrade is buy-mode only — always hidden here
////        SetUpgradeUIVisible(false);
////        HideHintBars();

////        SetAllLevelCardsActive(false);

////        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
////        int ownedCount = owned != null ? owned.Length : 0;

////        for (int i = 0; i < ownedCount && i < levelButtons.Length; i++)
////        {
////            if (levelButtons[i] == null) continue;
////            levelButtons[i].gameObject.SetActive(true);
////            levelButtons[i].SetupForSell(owned[i], this);
////        }

////        _selected = null;
////        if (previewImage != null) previewImage.enabled = false;

////        if (owned != null && owned.Length > 0) SelectHorse(owned[0]);

////        GameManager.Instance?.OpenHorsePanel();
////    }

////    public void OnPanelClosed()
////    {
////        HideWarning();
////        CancelInvoke(nameof(DelayedClose));
////    }

////    // ─── Open: UPDATE mode (opened by tapping a horse in the zone) ───────────

////    /// <summary>
////    /// Opens the panel focused on upgrading a specific already-owned horse.
////    /// Only the Upgrade button is shown — no Buy, no Sell, no level cards.
////    /// </summary>
////    public void OpenUpdateMode(HorseSlot slot)
////    {
////        if (slot == null || !slot.IsOccupied) return;

////        _mode = PanelMode.Update;
////        _wasUpgrading = false;
////        _selectedButton = null;
////        _updateSlot = slot;
////        _selected = slot.CurrentData;

////        // Hide the level card grid entirely
////        SetAllLevelCardsActive(false);

////        // Hide buy and sell buttons — update mode shows upgrade only
////        SetBuyButtonVisible(false);
////        SetSellButtonVisible(false);

////        // Sell price hidden
////        if (sellPriceText != null) sellPriceText.gameObject.SetActive(false);

////        // Preview
////        HorseData data = slot.CurrentData;
////        _previewFrame = 0;
////        _previewTimer = 0f;

////        if (previewImage != null && data.idleSprites?.Length > 0)
////        {
////            previewImage.sprite = data.idleSprites[0];
////            previewImage.enabled = true;
////            previewImage.preserveAspect = true;
////        }

////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
////        if (previewCostText != null) previewCostText.gameObject.SetActive(false);

////        HideWarning();

////        // Refresh upgrade UI immediately
////        RefreshUpdateModeHUD(slot);

////        GameManager.Instance?.OpenHorsePanel();
////    }

////    // ─── HUD — Update mode ────────────────────────────────────────────────────

////    private void RefreshUpdateModeHUD(HorseSlot slot)
////    {
////        if (slot == null) return;

////        bool upgrading = slot.IsUpgrading;
////        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !upgrading;
////        bool maxed = slot.UpgradeCount >= HorseSlot.MAX_UPGRADES;
////        int count = slot.UpgradeCount;

////        float hp = slot.CurrentHealth;
////        float ab = slot.CurrentAbility;
////        float dm = slot.CurrentDamage;

////        HorseData d = slot.CurrentData;
////        float hpGain = d.upgradeHealthGain;
////        float abGain = d.upgradeAbilityGain;
////        float dmGain = d.upgradeDamageGain;

////        // Main bars
////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

////        // Stat numbers — show "+gain" in green while upgrading
////        if (healthText != null)
////            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
////        if (abilityText != null)
////            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
////        if (damageText != null)
////            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

////        // Hint bars — shown while upgrading
////        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);

////        // Upgrade button
////        SetUpgradeUIVisible(true);
////        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
////        if (upgradeButtonText != null)
////            upgradeButtonText.text = maxed ? "Max" : $"({count}/{HorseSlot.MAX_UPGRADES})";

////        // Timer text
////        if (upgradeTimerText != null)
////        {
////            upgradeTimerText.gameObject.SetActive(upgrading);
////            if (upgrading)
////                upgradeTimerText.text = FormatUpgradeTimer(slot.UpgradeTimeRemaining);
////        }
////    }

////    // Selection

////    /// <summary>
////    /// Called by HorseLevelButton.OnClick — tracks which specific card button was tapped.
////    /// This fixes the duplicate-selection bug when two slots hold the same HorseData.
////    /// </summary>
////    public void SelectHorseButton(HorseLevelButton button)
////    {
////        _selectedButton = button;
////        SelectHorse(button.Data);
////    }

////    public void SelectHorse(HorseData data)
////    {
////        if (data == null) return;
////        _selected = data;
////        _previewFrame = 0;
////        _previewTimer = 0f;

////        if (previewImage != null && data.idleSprites?.Length > 0)
////        {
////            previewImage.sprite = data.idleSprites[0];
////            previewImage.enabled = true;
////            previewImage.preserveAspect = true;
////        }

////        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
////        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
////        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";

////        HideWarning();

////        foreach (var btn in levelButtons)
////        {
////            // Use button reference when available (sell mode multi-same-horse fix).
////            // Fall back to data comparison (buy mode, auto-select on open).
////            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn?.Data == data);
////            btn?.SetSelected(sel);
////        }

////        if (_mode == PanelMode.Buy)
////            ApplyBuyModeSelection(data);
////        else
////            ApplySellModeSelection(data);
////    }

////    // BUY: cost text ON, sell price OFF, upgrade button if owned

////    private void ApplyBuyModeSelection(HorseData data)
////    {
////        if (previewCostText != null)
////        {
////            previewCostText.text = $"Gold: {data.cost}";
////            previewCostText.gameObject.SetActive(true);
////        }
////        if (sellPriceText != null) sellPriceText.gameObject.SetActive(false);

////        bool unlocked = IsUnlocked(data);
////        SetBuyButtonVisible(unlocked);
////        SetSellButtonVisible(false);
////        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";

////        HorseSlot slot = HorseArea.Instance?.GetSlotForData(data);
////        if (slot != null)
////        {
////            // Owned horse — live stats + upgrade UI
////            RefreshBuyModeOwnedHUD(data, slot);
////        }
////        else
////        {
////            // Not yet bought — base stats, no upgrade UI
////            RefreshBaseStatsHUD(data);
////            SetUpgradeUIVisible(false);
////            HideHintBars();
////        }
////    }

////    // SELL: cost text OFF, sell price ON, no upgrade

////    private void ApplySellModeSelection(HorseData data)
////    {
////        if (previewCostText != null) previewCostText.gameObject.SetActive(false);

////        if (sellPriceText != null)
////        {
////            sellPriceText.text = $"Sell: {Mathf.RoundToInt(data.cost * 0.5f)}g";
////            sellPriceText.gameObject.SetActive(true);
////        }

////        SetUpgradeUIVisible(false);
////        HideHintBars();

////        HorseSlot slot = HorseArea.Instance?.GetSlotForData(data);
////        float hp = slot != null ? slot.CurrentHealth : data.health;
////        float ab = slot != null ? slot.CurrentAbility : data.ability;
////        float dm = slot != null ? slot.CurrentDamage : data.damage;

////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

////        if (healthText != null) healthText.text = $"{hp:F0}";
////        if (abilityText != null) abilityText.text = $"{ab:F0}";
////        if (damageText != null) damageText.text = $"{dm:F0}";
////    }

////    private bool IsUnlocked(HorseData data)
////    {
////        for (int i = 0; i < horseLevels.Length; i++)
////            if (horseLevels[i] == data)
////                return i < _unlocked.Length && _unlocked[i];
////        return false;
////    }

////    // HUD — base stats (unowned horse)

////    private void RefreshBaseStatsHUD(HorseData data)
////    {
////        float hp = Mathf.Clamp(data.health, 0f, MAX_STAT);
////        float ab = Mathf.Clamp(data.ability, 0f, MAX_STAT);
////        float dm = Mathf.Clamp(data.damage, 0f, MAX_STAT);

////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

////        if (healthText != null) healthText.text = $"{hp:F0}";
////        if (abilityText != null) abilityText.text = $"{ab:F0}";
////        if (damageText != null) damageText.text = $"{dm:F0}";
////    }

////    // HUD — owned horse in buy mode (live stats + upgrade UI)

////    private void RefreshBuyModeOwnedHUD(HorseData data, HorseSlot slot)
////    {
////        bool upgrading = slot.IsUpgrading;
////        bool canUpgrade = slot.UpgradeCount < HorseSlot.MAX_UPGRADES && !upgrading;
////        int upgradeCount = slot.UpgradeCount;
////        bool maxed = upgradeCount >= HorseSlot.MAX_UPGRADES;

////        float hp = slot.CurrentHealth;
////        float ab = slot.CurrentAbility;
////        float dm = slot.CurrentDamage;

////        float hpGain = data.upgradeHealthGain;
////        float abGain = data.upgradeAbilityGain;
////        float dmGain = data.upgradeDamageGain;

////        // Main bars
////        if (healthBar != null) healthBar.fillAmount = hp / MAX_STAT;
////        if (abilityBar != null) abilityBar.fillAmount = ab / MAX_STAT;
////        if (damageBar != null) damageBar.fillAmount = dm / MAX_STAT;

////        // Delta text while upgrading
////        if (healthText != null)
////            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
////        if (abilityText != null)
////            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
////        if (damageText != null)
////            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

////        // Hint bars
////        SetHintBar(healthBarHint, upgrading, Mathf.Min(hp + hpGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(abilityBarHint, upgrading, Mathf.Min(ab + abGain, MAX_STAT) / MAX_STAT);
////        SetHintBar(damageBarHint, upgrading, Mathf.Min(dm + dmGain, MAX_STAT) / MAX_STAT);

////        // Upgrade button
////        SetUpgradeUIVisible(true);
////        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
////        if (upgradeButtonText != null)
////            upgradeButtonText.text = maxed ? "Max" : $"({upgradeCount}/{HorseSlot.MAX_UPGRADES})";

////        // Timer
////        if (upgradeTimerText != null)
////        {
////            upgradeTimerText.gameObject.SetActive(upgrading);
////            if (upgrading)
////                upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
////        }
////    }

////    // Upgrade timer tick (BUY mode only)

////    private void TickUpgradeTimer()
////    {
////        // Run in both Buy and Update modes
////        if (_mode != PanelMode.Buy && _mode != PanelMode.Update) return;
////        if (_selected == null) return;

////        HorseSlot slot = _mode == PanelMode.Update && _updateSlot != null
////            ? _updateSlot
////            : HorseArea.Instance?.GetSlotForData(_selected);

////        if (slot == null) return;

////        bool upgrading = slot.IsUpgrading;

////        if (upgrading)
////        {
////            _wasUpgrading = true;

////            if (upgradeTimerText != null)
////            {
////                upgradeTimerText.gameObject.SetActive(true);
////                upgradeTimerText.text = FormatUpgradeTimer(slot.UpgradeTimeRemaining);

////                // Pulse the timer text: sine wave drives alpha between 60 % and 100 %
////                float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
////                Color c = upgradeTimerText.color;
////                upgradeTimerText.color = new Color(c.r, c.g, c.b, pulse);
////            }

////            // Keep "+gain" green text and hint bars live each frame
////            if (_mode == PanelMode.Update)
////                RefreshUpdateModeHUD(slot);
////            else
////                RefreshBuyModeOwnedHUD(_selected, slot);
////        }
////        else if (_wasUpgrading)
////        {
////            _wasUpgrading = false;

////            // Restore timer text alpha
////            if (upgradeTimerText != null)
////            {
////                Color c = upgradeTimerText.color;
////                upgradeTimerText.color = new Color(c.r, c.g, c.b, 1f);
////                upgradeTimerText.gameObject.SetActive(false);
////            }

////            if (_mode == PanelMode.Update)
////                RefreshUpdateModeHUD(slot);
////            else
////                RefreshBuyModeOwnedHUD(_selected, slot);

////            ShowWarning("Upgrade complete!");
////        }
////    }

////    // Returns "Xs" or "Xm Ys" depending on duration
////    private static string FormatUpgradeTimer(float seconds)
////    {
////        if (seconds >= 60f)
////        {
////            int m = (int)(seconds / 60f);
////            int s = (int)(seconds % 60f);
////            return $"{m}m {s:00}s";
////        }
////        return $"{seconds:F1}s";
////    }

////    // Preview animation

////    private void TickPreview()
////    {
////        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
////        _previewTimer += Time.deltaTime;
////        if (_previewTimer < 1f / _selected.idleFPS) return;
////        _previewTimer = 0f;
////        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
////        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
////    }

////    // Buy button

////    private void OnBuyClicked()
////    {
////        if (_selected == null) return;
////        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }
////        if (!HorseArea.Instance.HasFreeSlot()) { ShowWarning("Horse area is full!"); return; }

////        _gold -= _selected.cost;
////        RefreshGoldText();

////        HorseArea.Instance.SpawnHorse(_selected);
////        MarkBoughtAndUnlockNext(_selected);

////        ShowWarning($"'{_selected.horseName}' Brought!");
////        Invoke(nameof(DelayedClose), 1.5f);
////    }

////    private void MarkBoughtAndUnlockNext(HorseData bought)
////    {
////        for (int i = 0; i < horseLevels.Length; i++)
////        {
////            if (horseLevels[i] != bought) continue;
////            if (i < levelButtons.Length) levelButtons[i].SetBought(true);
////            int next = i + 1;
////            if (next < _unlocked.Length)
////            {
////                _unlocked[next] = true;
////                if (next < levelButtons.Length) levelButtons[next].SetLocked(false);
////            }
////            break;
////        }
////    }

////    // Sell button

////    private void OnSellClicked()
////    {
////        if (_selected == null) { ShowWarning("Select a horse to sell!"); return; }

////        _wasUpgrading = false;

////        int refund = Mathf.RoundToInt(_selected.cost * 0.5f);
////        HorseArea.Instance?.SellHorse(_selected);

////        if (sellPriceText != null) sellPriceText.gameObject.SetActive(false);

////        ShowWarning($"Sold for {refund}g.");
////        _selected = null;
////        if (previewImage != null) previewImage.enabled = false;

////        Invoke(nameof(DelayedClose), 1.5f);
////    }

////    // Upgrade button (BUY mode)

////    private void OnUpgradeClicked()
////    {
////        if (_selected == null) return;

////        // Resolve which slot to upgrade
////        HorseSlot slot = _mode == PanelMode.Update && _updateSlot != null
////            ? _updateSlot
////            : HorseArea.Instance?.GetSlotForData(_selected);

////        if (slot == null) return;

////        if (!slot.StartUpgrade())
////        {
////            ShowWarning(slot.UpgradeCount >= HorseSlot.MAX_UPGRADES
////                ? "Already at max level!"
////                : "Upgrade already in progress!");
////            return;
////        }

////        _wasUpgrading = true;

////        if (_mode == PanelMode.Update)
////            RefreshUpdateModeHUD(slot);
////        else
////            RefreshBuyModeOwnedHUD(_selected, slot);

////        ShowWarning("Upgrade started!");
////    }

////    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();

////    // Helpers

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

////    private void SetAllLevelCardsActive(bool active)
////    {
////        if (levelCardsParent != null)
////            for (int i = 0; i < levelCardsParent.childCount; i++)
////                levelCardsParent.GetChild(i).gameObject.SetActive(active);
////    }

////    private void SetBuyButtonVisible(bool v) { if (buyButton != null) buyButton.gameObject.SetActive(v); }
////    private void SetSellButtonVisible(bool v) { if (sellButton != null) sellButton.gameObject.SetActive(v); }

////    private void SetUpgradeUIVisible(bool v)
////    {
////        if (upgradeButton != null) upgradeButton.gameObject.SetActive(v);
////        if (upgradeTimerText != null && !v) upgradeTimerText.gameObject.SetActive(false);
////    }

////    public void RefreshGoldText() { if (coinText != null) coinText.text = $"{_gold}"; }
////    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
////    public int Gold => _gold;

////    private void ShowWarning(string msg)
////    {
////        if (warningText == null) return;
////        warningText.text = msg;
////        warningText.gameObject.SetActive(true);
////        CancelInvoke(nameof(HideWarning));
////        Invoke(nameof(HideWarning), 2.5f);
////    }

////    private void HideWarning() { if (warningText != null) warningText.gameObject.SetActive(false); }
////}



//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class HorsePanelManager : MonoBehaviour
//{
//    public static HorsePanelManager Instance { get; private set; }

//    public enum PanelMode { Buy, Sell, Update }

//    [Header("Horse levels (Brown=0, Black=1, White=2)")]
//    [SerializeField] private HorseData[] horseLevels;

//    [Header("Level Buttons (same order as horseLevels)")]
//    [SerializeField] private HorseLevelButton[] levelButtons;

//    [Header("Preview Image")]
//    [SerializeField] private Image previewImage;

//    [Header("HUD bars (Filled, Horizontal, Fill Origin Left)")]
//    [SerializeField] private Image           healthBar;
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private Image           abilityBar;
//    [SerializeField] private TextMeshProUGUI abilityText;
//    [SerializeField] private Image           damageBar;
//    [SerializeField] private TextMeshProUGUI damageText;

//    [Header("Panel buttons")]
//    [SerializeField] private Button          buyButton;
//    [SerializeField] private TextMeshProUGUI buyButtonText;
//    [SerializeField] private Button          sellButton;
//    [SerializeField] private Button          updateButton;
//    [SerializeField] private Button          equipButton;

//    [Header("Labels")]
//    [SerializeField] private TextMeshProUGUI horseLevelText;
//    [SerializeField] private TextMeshProUGUI costText;

//    [Header("Coin text")]
//    [SerializeField] private TextMeshProUGUI coinText;
//    [SerializeField] private int             startingGold = 100;

//    [Header("Warning")]
//    [SerializeField] private TextMeshProUGUI warningText;

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private HorseData  _selected;
//    private float      _previewTimer;
//    private int        _previewFrame;
//    private int        _gold;
//    private bool[]     _unlocked;
//    private PanelMode  _mode;
//    private HorseSlot  _updateTargetSlot;
//    private int        _selectedSellIndex = -1;

//    private const float MAX_STAT = 100f;

//    // ─── Lifecycle ────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        Instance = this;
//        _gold = startingGold;

//        _unlocked = new bool[horseLevels != null ? horseLevels.Length : 0];
//        if (_unlocked.Length > 0) _unlocked[0] = true;

//        Wire(buyButton,    OnBuyClicked);
//        Wire(sellButton,   OnSellClicked);
//        Wire(updateButton, OnUpdateClicked);
//        Wire(equipButton,  OnEquipClicked);

//        for (int i = 0; i < levelButtons.Length; i++)
//        {
//            if (levelButtons[i] == null) continue;
//            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//        }

//        RefreshGoldText();
//        HideWarning();
//        gameObject.SetActive(false);
//    }

//    private void Wire(Button b, UnityEngine.Events.UnityAction a)
//    {
//        if (b == null) return;
//        b.onClick.RemoveAllListeners();
//        b.onClick.AddListener(a);
//    }

//    private void Update() => TickPreview();

//    // ─── Open: BUY ───────────────────────────────────────────────────────────

//    public void OpenBuyMode()
//    {
//        _mode = PanelMode.Buy;
//        _updateTargetSlot = null;

//        for (int i = 0; i < levelButtons.Length; i++)
//        {
//            if (levelButtons[i] == null) continue;
//            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
//            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
//            levelButtons[i].gameObject.SetActive(true);
//            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
//        }

//        ShowOnly(sell: false, update: false, equip: false);
//        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // ─── Open: SELL ──────────────────────────────────────────────────────────

//    public void OpenSellMode()
//    {
//        _mode = PanelMode.Sell;
//        _updateTargetSlot = null;
//        _selectedSellIndex = -1;

//        ShowOnly(sell: true, update: false, equip: false);

//        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
//        int count = owned != null ? owned.Length : 0;

//        for (int i = 0; i < levelButtons.Length; i++)
//        {
//            if (levelButtons[i] == null) continue;
//            if (i < count) { levelButtons[i].gameObject.SetActive(true);  levelButtons[i].SetupForSell(owned[i], this, i); }
//            else             levelButtons[i].gameObject.SetActive(false);
//        }

//        _selected = null;
//        if (previewImage != null) previewImage.enabled = false;
//        if (owned != null && owned.Length > 0) SelectHorseForSell(owned[0], 0);
//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

//    public void OpenUpdateMode(HorseSlot slot)
//    {
//        _mode = PanelMode.Update;
//        _updateTargetSlot = slot;

//        // Hide all level cards — update mode is for one specific horse
//        foreach (var btn in levelButtons) if (btn != null) btn.gameObject.SetActive(false);

//        // Show Update + Equip buttons
//        ShowOnly(sell: false, update: true, equip: true);
//        if (buyButton != null) buyButton.gameObject.SetActive(false);

//        if (slot.CurrentData != null) SelectHorse(slot.CurrentData);
//        GameManager.Instance?.OpenHorsePanel();
//    }

//    // ─── Selection ────────────────────────────────────────────────────────────

//    public void SelectHorse(HorseData data)
//    {
//        if (data == null) return;
//        _selected = data; _previewFrame = 0; _previewTimer = 0f;

//        if (previewImage != null && data.idleSprites?.Length > 0)
//        { previewImage.sprite = data.idleSprites[0]; previewImage.enabled = true; previewImage.preserveAspect = true; }

//        RefreshHUD(data);
//        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
//        if (buyButtonText  != null) buyButtonText.text  = $"{data.cost}";
//        if (costText       != null) costText.text       = $"{data.cost}";
//        HideWarning();

//        foreach (var btn in levelButtons) btn?.SetSelected(btn.Data == data);

//        if (_mode == PanelMode.Buy)
//        {
//            bool alreadyOwned = HorseArea.Instance != null &&
//                                System.Array.Exists(HorseArea.Instance.GetOwnedHorses(), d => d == data);
//            bool unlocked = IsUnlocked(data);

//            // Show Buy only if unlocked AND not already owned
//            if (buyButton != null) buyButton.gameObject.SetActive(unlocked && !alreadyOwned);
//            // Show Update & Equip if already owned
//            if (updateButton != null) updateButton.gameObject.SetActive(alreadyOwned);
//            if (equipButton  != null) equipButton .gameObject.SetActive(alreadyOwned && HorseArea.Instance.HasFreeSlot());
//            if (sellButton   != null) sellButton  .gameObject.SetActive(false);
//        }
//    }

//    public void SelectHorseForSell(HorseData data, int sellIndex)
//    {
//        if (data == null) return;
//        _selected = data; _selectedSellIndex = sellIndex; _previewFrame = 0; _previewTimer = 0f;

//        if (previewImage != null && data.idleSprites?.Length > 0)
//        { previewImage.sprite = data.idleSprites[0]; previewImage.enabled = true; previewImage.preserveAspect = true; }

//        RefreshHUD(data);
//        if (horseLevelText != null) horseLevelText.text = data.horseName;
//        if (costText       != null) costText.text       = $"{data.cost}";
//        HideWarning();

//        // Only the card at this exact sell index highlights — not both copies of the same horse
//        foreach (var btn in levelButtons)
//            btn?.SetSelectedBySellIndex(btn.SellIndex == sellIndex);
//    }

//    // ─── Preview animation ────────────────────────────────────────────────────

//    private void TickPreview()
//    {
//        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
//        _previewTimer += Time.deltaTime;
//        if (_previewTimer < 1f / _selected.idleFPS) return;
//        _previewTimer = 0f;
//        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
//        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
//    }

//    // ─── Button actions ───────────────────────────────────────────────────────

//    private void OnBuyClicked()
//    {
//        if (_selected == null) return;
//        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

//        _gold -= _selected.cost;
//        RefreshGoldText();

//        HorseArea.Instance.BuyHorse(_selected);
//        MarkBoughtAndUnlockNext(_selected);

//        ShowWarning($"'{_selected.horseName}' bought! Tap Equip to place in slot.");

//        // Refresh button visibility — buy button should now hide
//        SelectHorse(_selected);
//    }

//    private void MarkBoughtAndUnlockNext(HorseData bought)
//    {
//        for (int i = 0; i < horseLevels.Length; i++)
//        {
//            if (horseLevels[i] != bought) continue;
//            if (i < levelButtons.Length) levelButtons[i].SetBought(true);
//            int next = i + 1;
//            if (next < _unlocked.Length) { _unlocked[next] = true; if (next < levelButtons.Length) levelButtons[next].SetLocked(false); }
//            break;
//        }
//    }

//    private void OnEquipClicked()
//    {
//        if (_selected == null) return;
//        if (!HorseArea.Instance.HasFreeSlot()) { ShowWarning("No free slot! Unequip a horse first."); return; }

//        bool ok = HorseArea.Instance.EquipHorse(_selected, _updateTargetSlot);
//        if (ok)
//        {
//            ShowWarning($"'{_selected.horseName}' equipped!");
//            Invoke(nameof(DelayedClose), 1.2f);
//        }
//        else ShowWarning("Could not equip — no free slot.");
//    }

//    private void OnSellClicked()
//    {
//        if (_selected == null) { ShowWarning("Select a horse to sell!"); return; }
//        int refund = Mathf.RoundToInt(_selected.cost * 0.5f);
//        HorseArea.Instance?.SellHorse(_selected);
//        ShowWarning($"Sold for {refund}g.");
//        _selected = null;
//        if (previewImage != null) previewImage.enabled = false;
//        Invoke(nameof(DelayedClose), 1.2f);
//    }

//    private void OnUpdateClicked()
//    {
//        if (_selected == null) return;
//        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
//        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

//        _gold -= upgradeCost;
//        RefreshGoldText();
//        if (updateButton != null) updateButton.interactable = false;
//        ShowWarning($"Upgrading '{_selected.horseName}'...");
//        StartCoroutine(UpdateAnimation(_selected));
//    }

//    private IEnumerator UpdateAnimation(HorseData data)
//    {
//        float elapsed = 0f;
//        float duration = data.upgradeDuration;
//        Vector3 origScale = previewImage != null ? previewImage.transform.localScale : Vector3.one;

//        while (elapsed < duration)
//        {
//            elapsed += Time.deltaTime;
//            if (previewImage != null)
//            {
//                float pulse = 1f + 0.1f * Mathf.Sin(elapsed * 4f * Mathf.PI * 2f);
//                previewImage.transform.localScale = origScale * pulse;
//                float t = (Mathf.Sin(elapsed * 4f * Mathf.PI * 2f) + 1f) * 0.5f;
//                previewImage.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.2f), t * 0.4f);
//            }
//            yield return null;
//        }

//        if (previewImage != null) { previewImage.transform.localScale = origScale; previewImage.color = Color.white; }

//        data.health  = Mathf.Min(data.health  + data.upgradeHealthGain,  MAX_STAT);
//        data.ability = Mathf.Min(data.ability + data.upgradeAbilityGain, MAX_STAT);
//        data.damage  = Mathf.Min(data.damage  + data.upgradeDamageGain,  MAX_STAT);
//        RefreshHUD(data);

//        if (updateButton != null) updateButton.interactable = true;
//        ShowWarning("Upgrade complete!");
//    }

//    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();

//    // ─── HUD ─────────────────────────────────────────────────────────────────

//    private void RefreshHUD(HorseData data)
//    {
//        float h = Mathf.Clamp(data.health, 0, MAX_STAT), a = Mathf.Clamp(data.ability, 0, MAX_STAT), d = Mathf.Clamp(data.damage, 0, MAX_STAT);
//        if (healthBar  != null) healthBar .fillAmount = h / MAX_STAT;
//        if (abilityBar != null) abilityBar.fillAmount = a / MAX_STAT;
//        if (damageBar  != null) damageBar .fillAmount = d / MAX_STAT;
//        if (healthText  != null) healthText .text = $"{h:F0}";
//        if (abilityText != null) abilityText.text = $"{a:F0}";
//        if (damageText  != null) damageText .text = $"{d:F0}";
//    }

//    // ─── Helpers ─────────────────────────────────────────────────────────────

//    /// <summary>Show only the specified buttons, hide all others.</summary>
//    private void ShowOnly(bool sell, bool update, bool equip)
//    {
//        if (buyButton    != null) buyButton   .gameObject.SetActive(true);   // buy starts visible, SelectHorse may hide it
//        if (sellButton   != null) sellButton  .gameObject.SetActive(sell);
//        if (updateButton != null) updateButton.gameObject.SetActive(update);
//        if (equipButton  != null) equipButton .gameObject.SetActive(equip);
//    }

//    private bool IsUnlocked(HorseData data)
//    {
//        for (int i = 0; i < horseLevels.Length; i++)
//            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
//        return false;
//    }

//    public void RefreshGoldText() { if (coinText != null) coinText.text = $"{_gold}"; }

//    private void ShowWarning(string msg)
//    {
//        if (warningText == null) return;
//        warningText.text = msg; warningText.gameObject.SetActive(true);
//        CancelInvoke(nameof(HideWarning)); Invoke(nameof(HideWarning), 2.5f);
//    }
//    private void HideWarning() { if (warningText != null) warningText.gameObject.SetActive(false); }

//    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
//    public int Gold => _gold;
//    public void OnPanelClosed() { HideWarning(); CancelInvoke(nameof(DelayedClose)); StopAllCoroutines(); }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HorsePanelManager : MonoBehaviour
{
    public static HorsePanelManager Instance { get; private set; }

    public enum PanelMode { Buy, Sell, Update }

    [Header("Horse levels (Brown=0, Black=1, White=2)")]
    [SerializeField] private HorseData[] horseLevels;

    [Header("Level Buttons (same order as horseLevels)")]
    [SerializeField] private HorseLevelButton[] levelButtons;

    [Header("Preview Image")]
    [SerializeField] private Image previewImage;

    // ── Restored missing inspector fields ──────────────────────────────────
    [Header("Preview Info")]
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewAgeText;
    // ───────────────────────────────────────────────────────────────────────

    [Header("HUD bars (Filled, Horizontal, Fill Origin Left)")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image abilityBar;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private Image damageBar;
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Panel buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button updateButton;
    [SerializeField] private Button equipButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI horseLevelText;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("Coin text")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private int startingGold = 100;

    [Header("Warning")]
    [SerializeField] private TextMeshProUGUI warningText;

    // ─── Private ──────────────────────────────────────────────────────────────

    private HorseData _selected;
    private float _previewTimer;
    private int _previewFrame;
    private int _gold;
    private bool[] _unlocked;
    private PanelMode _mode;
    private HorseSlot _updateTargetSlot;
    private int _selectedSellIndex = -1;

    // FIX: tracks the exact button that was tapped — prevents all bought cards
    //      from appearing selected when multiple horses have been purchased.
    private HorseLevelButton _selectedButton = null;

    private const float MAX_STAT = 100f;

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

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
        }

        RefreshGoldText();
        HideWarning();
        gameObject.SetActive(false);
    }

    private void Wire(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    private void Update() => TickPreview();

    // ─── Open: BUY ───────────────────────────────────────────────────────────

    public void OpenBuyMode()
    {
        _mode = PanelMode.Buy;
        _updateTargetSlot = null;
        _selectedButton = null;   // clear so first auto-select uses data comparison

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            bool hasData = i < horseLevels.Length && horseLevels[i] != null;
            if (!hasData) { levelButtons[i].gameObject.SetActive(false); continue; }
            levelButtons[i].gameObject.SetActive(true);
            levelButtons[i].Setup(horseLevels[i], this, i >= _unlocked.Length || !_unlocked[i]);
        }

        ShowOnly(sell: false, update: false, equip: false);
        if (horseLevels?.Length > 0) SelectHorse(horseLevels[0]);
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Open: SELL ──────────────────────────────────────────────────────────

    public void OpenSellMode()
    {
        _mode = PanelMode.Sell;
        _updateTargetSlot = null;
        _selectedSellIndex = -1;
        _selectedButton = null;

        ShowOnly(sell: true, update: false, equip: false);

        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
        int count = owned != null ? owned.Length : 0;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            if (i < count) { levelButtons[i].gameObject.SetActive(true); levelButtons[i].SetupForSell(owned[i], this, i); }
            else levelButtons[i].gameObject.SetActive(false);
        }

        _selected = null;
        if (previewImage != null) previewImage.enabled = false;
        if (owned != null && owned.Length > 0) SelectHorseForSell(owned[0], 0);
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

    public void OpenUpdateMode(HorseSlot slot)
    {
        _mode = PanelMode.Update;
        _updateTargetSlot = slot;
        _selectedButton = null;

        // Hide all level cards — update mode is for one specific horse
        foreach (var btn in levelButtons) if (btn != null) btn.gameObject.SetActive(false);

        // Show Update + Equip buttons
        ShowOnly(sell: false, update: true, equip: true);
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        if (slot.CurrentData != null) SelectHorse(slot.CurrentData);
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by HorseLevelButton.OnClick — tracks the exact button that was tapped.
    /// This is the fix for the "all bought cards appear selected" bug: selection is
    /// compared by button reference rather than data, so only the tapped card highlights.
    /// </summary>
    public void SelectHorseButton(HorseLevelButton button)
    {
        _selectedButton = button;
        SelectHorse(button.Data);
    }

    public void SelectHorse(HorseData data)
    {
        if (data == null) return;
        _selected = data; _previewFrame = 0; _previewTimer = 0f;

        if (previewImage != null && data.idleSprites?.Length > 0)
        { previewImage.sprite = data.idleSprites[0]; previewImage.enabled = true; previewImage.preserveAspect = true; }

        RefreshHUD(data);
        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
        if (costText != null) costText.text = $"{data.cost}";
        HideWarning();

        // FIX: when a specific button was tapped (_selectedButton is set), compare by
        // button reference — this prevents all bought cards from lighting up together.
        // Fall back to data comparison only for automatic selection on panel open.
        foreach (var btn in levelButtons)
        {
            if (btn == null) continue;
            bool sel = _selectedButton != null ? (btn == _selectedButton) : (btn.Data == data);
            btn.SetSelected(sel);
        }

        if (_mode == PanelMode.Buy)
        {
            bool alreadyOwned = HorseArea.Instance != null &&
                                System.Array.Exists(HorseArea.Instance.GetOwnedHorses(), d => d == data);
            bool unlocked = IsUnlocked(data);

            // Show Buy only if unlocked AND not already owned
            if (buyButton != null) buyButton.gameObject.SetActive(unlocked && !alreadyOwned);
            // Show Update & Equip if already owned
            if (updateButton != null) updateButton.gameObject.SetActive(alreadyOwned);
            if (equipButton != null) equipButton.gameObject.SetActive(alreadyOwned && HorseArea.Instance.HasFreeSlot());
            if (sellButton != null) sellButton.gameObject.SetActive(false);
        }
    }

    public void SelectHorseForSell(HorseData data, int sellIndex)
    {
        if (data == null) return;
        _selected = data; _selectedSellIndex = sellIndex; _previewFrame = 0; _previewTimer = 0f;

        if (previewImage != null && data.idleSprites?.Length > 0)
        { previewImage.sprite = data.idleSprites[0]; previewImage.enabled = true; previewImage.preserveAspect = true; }

        RefreshHUD(data);
        if (horseLevelText != null) horseLevelText.text = data.horseName;
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"{data.cost}";
        HideWarning();

        // Only the card at this exact sell index highlights — not both copies of the same horse
        foreach (var btn in levelButtons)
            btn?.SetSelectedBySellIndex(btn.SellIndex == sellIndex);
    }

    // ─── Preview animation ────────────────────────────────────────────────────

    private void TickPreview()
    {
        if (_selected?.idleSprites == null || _selected.idleSprites.Length <= 1) return;
        _previewTimer += Time.deltaTime;
        if (_previewTimer < 1f / _selected.idleFPS) return;
        _previewTimer = 0f;
        _previewFrame = (_previewFrame + 1) % _selected.idleSprites.Length;
        if (previewImage != null) previewImage.sprite = _selected.idleSprites[_previewFrame];
    }

    // ─── Button actions ───────────────────────────────────────────────────────

    private void OnBuyClicked()
    {
        if (_selected == null) return;
        if (_gold < _selected.cost) { ShowWarning("Not enough gold!"); return; }

        _gold -= _selected.cost;
        RefreshGoldText();

        HorseArea.Instance.BuyHorse(_selected);
        MarkBoughtAndUnlockNext(_selected);

        ShowWarning($"'{_selected.horseName}' bought! Tap Equip to place in slot.");

        // Refresh button visibility — buy button should now hide.
        // Clear _selectedButton so SelectHorse uses data comparison for this auto-refresh.
        _selectedButton = null;
        SelectHorse(_selected);
    }

    private void MarkBoughtAndUnlockNext(HorseData bought)
    {
        for (int i = 0; i < horseLevels.Length; i++)
        {
            if (horseLevels[i] != bought) continue;
            if (i < levelButtons.Length) levelButtons[i].SetBought(true);
            int next = i + 1;
            if (next < _unlocked.Length) { _unlocked[next] = true; if (next < levelButtons.Length) levelButtons[next].SetLocked(false); }
            break;
        }
    }

    private void OnEquipClicked()
    {
        if (_selected == null) return;
        if (!HorseArea.Instance.HasFreeSlot()) { ShowWarning("No free slot! Unequip a horse first."); return; }

        bool ok = HorseArea.Instance.EquipHorse(_selected, _updateTargetSlot);
        if (ok)
        {
            ShowWarning($"'{_selected.horseName}' equipped!");
            Invoke(nameof(DelayedClose), 1.2f);
        }
        else ShowWarning("Could not equip — no free slot.");
    }

    private void OnSellClicked()
    {
        if (_selected == null) { ShowWarning("Select a horse to sell!"); return; }
        int refund = Mathf.RoundToInt(_selected.cost * 0.5f);
        HorseArea.Instance?.SellHorse(_selected);
        ShowWarning($"Sold for {refund}g.");
        _selected = null;
        if (previewImage != null) previewImage.enabled = false;
        Invoke(nameof(DelayedClose), 1.2f);
    }

    private void OnUpdateClicked()
    {
        if (_selected == null) return;
        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

        _gold -= upgradeCost;
        RefreshGoldText();
        if (updateButton != null) updateButton.interactable = false;
        ShowWarning($"Upgrading '{_selected.horseName}'...");
        StartCoroutine(UpdateAnimation(_selected));
    }

    private IEnumerator UpdateAnimation(HorseData data)
    {
        float elapsed = 0f;
        float duration = data.upgradeDuration;
        Vector3 origScale = previewImage != null ? previewImage.transform.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (previewImage != null)
            {
                float pulse = 1f + 0.1f * Mathf.Sin(elapsed * 4f * Mathf.PI * 2f);
                previewImage.transform.localScale = origScale * pulse;
                float t = (Mathf.Sin(elapsed * 4f * Mathf.PI * 2f) + 1f) * 0.5f;
                previewImage.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.2f), t * 0.4f);
            }
            yield return null;
        }

        if (previewImage != null) { previewImage.transform.localScale = origScale; previewImage.color = Color.white; }

        data.health = Mathf.Min(data.health + data.upgradeHealthGain, MAX_STAT);
        data.ability = Mathf.Min(data.ability + data.upgradeAbilityGain, MAX_STAT);
        data.damage = Mathf.Min(data.damage + data.upgradeDamageGain, MAX_STAT);
        RefreshHUD(data);

        if (updateButton != null) updateButton.interactable = true;
        ShowWarning("Upgrade complete!");
    }

    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();

    // ─── HUD ─────────────────────────────────────────────────────────────────

    private void RefreshHUD(HorseData data)
    {
        float h = Mathf.Clamp(data.health, 0, MAX_STAT), a = Mathf.Clamp(data.ability, 0, MAX_STAT), d = Mathf.Clamp(data.damage, 0, MAX_STAT);
        if (healthBar != null) healthBar.fillAmount = h / MAX_STAT;
        if (abilityBar != null) abilityBar.fillAmount = a / MAX_STAT;
        if (damageBar != null) damageBar.fillAmount = d / MAX_STAT;
        if (healthText != null) healthText.text = $"{h:F0}";
        if (abilityText != null) abilityText.text = $"{a:F0}";
        if (damageText != null) damageText.text = $"{d:F0}";
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Show only the specified buttons, hide all others.</summary>
    private void ShowOnly(bool sell, bool update, bool equip)
    {
        if (buyButton != null) buyButton.gameObject.SetActive(true);   // buy starts visible, SelectHorse may hide it
        if (sellButton != null) sellButton.gameObject.SetActive(sell);
        if (updateButton != null) updateButton.gameObject.SetActive(update);
        if (equipButton != null) equipButton.gameObject.SetActive(equip);
    }

    private bool IsUnlocked(HorseData data)
    {
        for (int i = 0; i < horseLevels.Length; i++)
            if (horseLevels[i] == data) return i < _unlocked.Length && _unlocked[i];
        return false;
    }

    public void RefreshGoldText() { if (coinText != null) coinText.text = $"{_gold}"; }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg; warningText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideWarning)); Invoke(nameof(HideWarning), 2.5f);
    }
    private void HideWarning() { if (warningText != null) warningText.gameObject.SetActive(false); }

    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
    public int Gold => _gold;
    public void OnPanelClosed() { HideWarning(); CancelInvoke(nameof(DelayedClose)); StopAllCoroutines(); }
}