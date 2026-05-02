using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HorsePanelManager — Buy / Sell / Update panel controller.
///
/// ── FIXES IN THIS VERSION ───────────────────────────────────────────────────
///
///  1. UPGRADE ISOLATION
///     Stats are stored PER SLOT (in HorseSlot), not on the HorseData asset.
///     Upgrading horse A never touches horse B.
///
///  2. LIVE UPGRADE TIMER IN HUD
///     While a slot is upgrading the panel shows:
///       • A countdown label ("Upgrading: 7.3 s")
///       • Hint bars showing the FUTURE stat values at ~50 % alpha
///       • Stat numbers showing "40  +7" in green
///     The timer is ticked every frame in Update() via TickUpgradeTimer().
///     Works even when you switch between horses — each horse's slot carries
///     its own independent timer.
///
///  3. EQUIP GUARD
///     OnEquipClicked() now checks HorseArea.IsEquipped() first.
///     A horse that is already in a slot shows "Already equipped!" instead of
///     silently dropping into a second slot.
///
///  4. SWITCHING BETWEEN HORSES WHILE ONE IS UPGRADING
///     Switching selection changes what the HUD displays, but both upgrades
///     continue running independently in HorseSlot.Update().  When a slot
///     finishes it calls OnSlotUpgradeComplete(); the panel refreshes only if
///     that slot is currently selected.
///
/// ── INSPECTOR SETUP FOR HINT BARS ───────────────────────────────────────────
///   Duplicate each stat-bar Image, give it a green tint + alpha ~100,
///   name it e.g. "HealthBarHint", and drag it into the matching Hint field.
/// </summary>
public class HorsePanelManager : MonoBehaviour
{
    public static HorsePanelManager Instance { get; private set; }

    public enum PanelMode { Buy, Sell, Update }

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Horse levels (Brown=0, Black=1, White=2 …)")]
    [SerializeField] private HorseData[] horseLevels;

    [Header("Level Buttons (same order as horseLevels)")]
    [SerializeField] private HorseLevelButton[] levelButtons;

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
    [SerializeField] private TextMeshProUGUI updateButtonText;   // shows "(1/3)" or "Max"
    [SerializeField] private Button equipButton;

    [Header("Upgrade Timer Label")]
    [Tooltip("Shows 'Upgrading: 7.3s' while a slot is upgrading. Drag a TMP label here.")]
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
    private HorseSlot _updateTargetSlot;   // slot that opened Update mode
    private int _selectedSellIndex = -1;
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
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Wire(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    private void Update()
    {
        TickPreview();
        TickUpgradeTimer();
    }

    // ─── Open: BUY ───────────────────────────────────────────────────────────

    public void OpenBuyMode()
    {
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

        ShowOnly(sell: false, update: false, equip: false);
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

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
        HideHintBars();
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);

        HorseData[] owned = HorseArea.Instance?.GetOwnedHorses();
        int count = owned != null ? owned.Length : 0;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            if (i < count)
            {
                levelButtons[i].gameObject.SetActive(true);
                levelButtons[i].SetupForSell(owned[i], this, i);
            }
            else
            {
                levelButtons[i].gameObject.SetActive(false);
            }
        }

        _selected = null;
        if (previewImage != null) previewImage.enabled = false;
        if (owned != null && owned.Length > 0) SelectHorseForSell(owned[0], 0);
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Open: UPDATE (tap horse in slot) ────────────────────────────────────

    /// <summary>
    /// Opens the panel focused on one specific slot.
    /// Shows the live (per-slot) stats and the upgrade countdown if one is running.
    /// </summary>
    public void OpenUpdateMode(HorseSlot slot)
    {
        if (slot == null || !slot.IsOccupied) return;

        _mode = PanelMode.Update;
        _updateTargetSlot = slot;
        _selectedButton = null;

        // Hide all level cards — Update mode is for one specific horse
        foreach (var btn in levelButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        // Show Update button only (Equip is irrelevant here — horse is already equipped)
        ShowOnly(sell: false, update: true, equip: false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        // Populate preview
        HorseData data = slot.CurrentData;
        _selected = data;
        _previewFrame = 0;
        _previewTimer = 0f;

        if (previewImage != null && data.idleSprites?.Length > 0)
        {
            previewImage.sprite = data.idleSprites[0];
            previewImage.enabled = true;
            previewImage.preserveAspect = true;
        }

        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (costText != null) costText.text = $"{data.cost}";

        HideWarning();
        RefreshUpdateModeHUD(slot);  // draws live stats + timer state
        GameManager.Instance?.OpenHorsePanel();
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    /// <summary>Called by HorseLevelButton — tracks the exact button tapped to avoid
    /// multi-highlight when multiple owned horses share the same HorseData.</summary>
    public void SelectHorse(HorseData data)
    {
        if (data == null) return;
        _selected = data;
        _previewFrame = 0;
        _previewTimer = 0f;

        if (previewImage != null && data.idleSprites?.Length > 0)
        {
            previewImage.sprite = data.idleSprites[0];
            previewImage.enabled = true;
            previewImage.preserveAspect = true;
        }

        if (horseLevelText != null) horseLevelText.text = $"Level {data.level}";
        if (previewNameText != null) previewNameText.text = $"Name: {data.horseName}";
        if (previewAgeText != null) previewAgeText.text = $"Age: {data.age} Years";
        if (buyButtonText != null) buyButtonText.text = $"{data.cost}";
        if (costText != null) costText.text = $"{data.cost}";
        HideWarning();

        // Highlight only the tapped button (or fall back to data match on auto-select)
        foreach (var btn in levelButtons)
        {
            if (btn == null) continue;
            bool sel = _selectedButton != null
                ? (btn == _selectedButton)
                : (btn.Data == data);
            btn.SetSelected(sel);
        }

        if (_mode == PanelMode.Buy)
            ApplyBuyModeSelection(data);
        // Update mode selection is handled entirely in OpenUpdateMode
    }

    public void SelectHorseForSell(HorseData data, int sellIndex)
    {
        if (data == null) return;
        _selected = data;
        _selectedSellIndex = sellIndex;
        _previewFrame = 0;
        _previewTimer = 0f;

        if (previewImage != null && data.idleSprites?.Length > 0)
        {
            previewImage.sprite = data.idleSprites[0];
            previewImage.enabled = true;
            previewImage.preserveAspect = true;
        }

        // Sell HUD shows base stats (no per-slot upgrades needed here)
        RefreshHUDFromValues(data.health, data.ability, data.damage,
                             upgrading: false, hpGain: 0, abGain: 0, dmGain: 0);

        if (horseLevelText != null) horseLevelText.text = data.horseName;
        if (costText != null) costText.text = $"{data.cost}";
        HideWarning();

        // Highlight only the card at this exact sell index
        foreach (var btn in levelButtons)
            btn?.SetSelectedBySellIndex(btn.SellIndex == sellIndex);
    }

    // Called by HorseLevelButton when it is tapped
    public void SelectHorseButton(HorseLevelButton button)
    {
        _selectedButton = button;
        SelectHorse(button.Data);
    }

    // ─── BUY mode: button visibility per selected horse ───────────────────────

    private void ApplyBuyModeSelection(HorseData data)
    {
        bool alreadyOwned = HorseArea.Instance != null &&
                            System.Array.Exists(HorseArea.Instance.GetOwnedHorses(), d => d == data);
        bool alreadyEquipped = alreadyOwned && HorseArea.Instance.IsEquipped(data);
        bool unlocked = IsUnlocked(data);

        // Buy only when unlocked and not yet purchased
        if (buyButton != null) buyButton.gameObject.SetActive(unlocked && !alreadyOwned);
        // Update / Equip only when owned
        if (updateButton != null) updateButton.gameObject.SetActive(alreadyOwned);
        if (equipButton != null)
            equipButton.gameObject.SetActive(alreadyOwned && !alreadyEquipped && HorseArea.Instance.HasFreeSlot());
        if (sellButton != null) sellButton.gameObject.SetActive(false);

        // HUD: show per-slot live stats if the horse is equipped, otherwise base stats
        if (alreadyOwned)
        {
            // Find the slot holding this horse (if any) to read live stats
            HorseSlot liveSlot = FindSlotForData(data);
            if (liveSlot != null)
                RefreshHUDFromSlot(liveSlot);
            else
                RefreshHUDFromValues(data.health, data.ability, data.damage,
                                     upgrading: false, hpGain: 0, abGain: 0, dmGain: 0);

            // Also refresh the update button label
            if (liveSlot != null) RefreshUpdateButton(liveSlot);
        }
        else
        {
            RefreshHUDFromValues(data.health, data.ability, data.damage,
                                 upgrading: false, hpGain: 0, abGain: 0, dmGain: 0);
            HideHintBars();
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        }
    }

    // ─── Update mode HUD ─────────────────────────────────────────────────────

    /// <summary>
    /// Draws the full update-mode HUD for the given slot:
    ///   • Live stats from the slot (not the asset)
    ///   • Hint bars when an upgrade is running
    ///   • Green "+gain" numbers when upgrading
    ///   • Timer text ("Upgrading: 7.3s")
    ///   • Update button label and interactability
    /// </summary>
    private void RefreshUpdateModeHUD(HorseSlot slot)
    {
        if (slot == null || !slot.IsOccupied) return;

        RefreshHUDFromSlot(slot);
        RefreshUpdateButton(slot);

        // Timer
        if (upgradeTimerText != null)
        {
            bool upgrading = slot.IsUpgrading;
            upgradeTimerText.gameObject.SetActive(upgrading);
            if (upgrading)
                upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
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
            updateButtonText.text = maxed
                ? "Max"
                : $"({slot.UpgradeCount}/{HorseSlot.MAX_UPGRADES})";
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

        // Numbers: plain or with green "+gain"
        if (healthText != null)
            healthText.text = upgrading ? $"{hp:F0} <color=#4CFF72>+{hpGain:F0}</color>" : $"{hp:F0}";
        if (abilityText != null)
            abilityText.text = upgrading ? $"{ab:F0} <color=#4CFF72>+{abGain:F0}</color>" : $"{ab:F0}";
        if (damageText != null)
            damageText.text = upgrading ? $"{dm:F0} <color=#4CFF72>+{dmGain:F0}</color>" : $"{dm:F0}";

        // Hint bars — future fill values shown while upgrading
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

    /// <summary>
    /// Refreshes the upgrade countdown label every frame while the panel is open.
    /// Works in both Update mode (slot is _updateTargetSlot) and Buy mode
    /// (slot found via FindSlotForData).
    /// </summary>
    private void TickUpgradeTimer()
    {
        HorseSlot slot = ResolveCurrentSlot();
        if (slot == null || !slot.IsOccupied) return;

        if (_mode == PanelMode.Update)
        {
            // Live-update the full HUD every frame while upgrading
            if (slot.IsUpgrading)
            {
                RefreshUpdateModeHUD(slot);
            }
        }
        else if (_mode == PanelMode.Buy && slot.IsUpgrading)
        {
            // In Buy mode, just update the timer text and stat colour
            if (upgradeTimerText != null)
            {
                upgradeTimerText.gameObject.SetActive(true);
                upgradeTimerText.text = $"{slot.UpgradeTimeRemaining:F1}s";
            }
            RefreshHUDFromSlot(slot);
        }
        else
        {
            // Not upgrading — make sure timer label is off
            if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
        }
    }

    // ─── Called back by HorseSlot when an upgrade finishes ───────────────────

    /// <summary>
    /// HorseSlot.CompleteUpgrade() calls this so the panel can update instantly
    /// instead of waiting for the next TickUpgradeTimer() call.
    /// Only refreshes if the finished slot is currently shown in the panel.
    /// </summary>
    public void OnSlotUpgradeComplete(HorseSlot slot)
    {
        if (slot != ResolveCurrentSlot()) return;   // different horse on screen — ignore

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

        HorseArea.Instance.BuyHorse(_selected);
        MarkBoughtAndUnlockNext(_selected);

        ShowWarning($"'{_selected.horseName}' bought! Tap Equip to place in a slot.");

        // Refresh buttons — Buy should now hide, Equip should appear
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

        // Guard: already in a slot
        if (HorseArea.Instance.IsEquipped(_selected))
        {
            ShowWarning($"'{_selected.horseName}' is already equipped!");
            return;
        }

        if (!HorseArea.Instance.HasFreeSlot())
        {
            ShowWarning("No free slot! Unequip a horse first.");
            return;
        }

        bool ok = HorseArea.Instance.EquipHorse(_selected, _updateTargetSlot);
        if (ok)
        {
            ShowWarning($"'{_selected.horseName}' equipped!");
            // Refresh so the Equip button disappears now that the horse is slotted
            _selectedButton = null;
            SelectHorse(_selected);
        }
        else
        {
            ShowWarning("Could not equip — no free slot.");
        }
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

        // Resolve which slot to upgrade
        HorseSlot slot = ResolveCurrentSlot();
        if (slot == null) { ShowWarning("Horse is not in a slot yet — equip first!"); return; }

        int upgradeCost = Mathf.RoundToInt(_selected.cost * 0.5f);
        if (_gold < upgradeCost) { ShowWarning($"Need {upgradeCost}g to upgrade!"); return; }

        // StartUpgrade returns false if already upgrading or at max level
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
        RefreshUpdateModeHUD(slot);   // show hint bars + timer immediately
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
                float pulse = 1f + 0.1f * Mathf.Sin(elapsed * 2f * Mathf.PI * 0.5f);
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

    private void DelayedClose() => GameManager.Instance?.CloseHorsePanel();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Returns the slot currently relevant to the selected horse.</summary>
    private HorseSlot ResolveCurrentSlot()
    {
        if (_mode == PanelMode.Update && _updateTargetSlot != null)
            return _updateTargetSlot;

        if (_selected != null)
            return FindSlotForData(_selected);

        return null;
    }

    /// <summary>Searches all HorseArea slots for one holding the given data.</summary>
    private HorseSlot FindSlotForData(HorseData data)
    {
        if (data == null || HorseArea.Instance == null) return null;
        // Iterate the slots array via reflection-free public API — we call into
        // HorseArea to get each slot's occupant via the public OwnedHorses list,
        // but for the slot object itself we need the area's slot array.
        // Simplest: expose a FindSlot method on HorseArea (added below).
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

    /// <summary>Show only the specified buttons; hide all others.</summary>
    private void ShowOnly(bool sell, bool update, bool equip)
    {
        if (buyButton != null) buyButton.gameObject.SetActive(true);
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

    // ─── Public API ───────────────────────────────────────────────────────────

    public void RefreshGoldText() { if (coinText != null) coinText.text = $"{_gold}"; }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        warningText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideWarning));
        Invoke(nameof(HideWarning), 2.5f);
    }
    private void HideWarning() { if (warningText != null) warningText.gameObject.SetActive(false); }

    public void AddGold(int amount) { _gold += amount; RefreshGoldText(); }
    public int Gold => _gold;

    public void OnPanelClosed()
    {
        HideWarning();
        CancelInvoke(nameof(DelayedClose));
        // Note: DO NOT StopAllCoroutines here — there are no panel coroutines in this version.
        // Upgrade timers live in HorseSlot.Update() and must keep running after the panel closes.
    }
}