//////////////using UnityEngine;
//////////////using UnityEngine.UI;


//////////////public class HorseSlot : MonoBehaviour
//////////////{
//////////////    [Header("Children")]
//////////////    [SerializeField] private RectTransform spawnPoint;
//////////////    [SerializeField] private GameObject emptyGroup;

//////////////    // ─── Horse reference ──────────────────────────────────────────────────────

//////////////    private HorseController _horse;
//////////////    public bool IsOccupied => _horse != null;
//////////////    public HorseData CurrentData => _horse?.Data;

//////////////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

//////////////    private int _upgradeCount = 0;
//////////////    private float _currentHealth;
//////////////    private float _currentAbility;
//////////////    private float _currentDamage;
//////////////    private bool _isUpgrading = false;
//////////////    private float _upgradeEndTime;

//////////////    public const int MAX_UPGRADES = 3;

//////////////    public int UpgradeCount => _upgradeCount;
//////////////    public bool IsUpgrading => _isUpgrading;
//////////////    public float CurrentHealth => _currentHealth;
//////////////    public float CurrentAbility => _currentAbility;
//////////////    public float CurrentDamage => _currentDamage;

//////////////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
//////////////    public float UpgradeTimeRemaining =>
//////////////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//////////////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//////////////    private void Awake() => RefreshUI();

//////////////    private void Update()
//////////////    {
//////////////        // Timer runs regardless of whether the panel is open
//////////////        if (_isUpgrading && Time.time >= _upgradeEndTime)
//////////////            CompleteUpgrade();
//////////////    }

//////////////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//////////////    public void Equip(HorseData data)
//////////////    {
//////////////        if (IsOccupied) UnequipHorse();
//////////////        if (data.prefab == null)
//////////////        {
//////////////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
//////////////            return;
//////////////        }

//////////////        GameObject go = Instantiate(data.prefab, spawnPoint);
//////////////        RectTransform rt = go.GetComponent<RectTransform>();
//////////////        if (rt != null)
//////////////        {
//////////////            rt.anchoredPosition = Vector2.zero;
//////////////            rt.localScale = Vector3.one;
//////////////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
//////////////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
//////////////        }

//////////////        _horse = go.GetComponent<HorseController>();
//////////////        _horse?.Setup(data);

//////////////        // Initialise live stats from the ScriptableObject BASE values.
//////////////        // These are our own copies — we never write back to the asset.
//////////////        _upgradeCount = 0;
//////////////        _currentHealth = data.health;
//////////////        _currentAbility = data.ability;
//////////////        _currentDamage = data.damage;
//////////////        _isUpgrading = false;

//////////////        // Tap the horse to open Update mode for THIS slot
//////////////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//////////////        btn.transition = Selectable.Transition.None;
//////////////        btn.onClick.RemoveAllListeners();
//////////////        btn.onClick.AddListener(OnHorseTapped);

//////////////        RefreshUI();
//////////////    }

//////////////    public void UnequipHorse()
//////////////    {
//////////////        if (!IsOccupied) return;
//////////////        _isUpgrading = false;
//////////////        Destroy(_horse.gameObject);
//////////////        _horse = null;
//////////////        RefreshUI();
//////////////    }

//////////////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

//////////////    // ─── Upgrade API (called by HorsePanelManager) ───────────────────────────

//////////////    /// <summary>
//////////////    /// Starts the upgrade countdown for this specific slot.
//////////////    /// Returns false if the slot is empty, already upgrading, or at max level.
//////////////    /// </summary>
//////////////    public bool StartUpgrade()
//////////////    {
//////////////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//////////////            return false;

//////////////        _isUpgrading = true;
//////////////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

//////////////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
//////////////                  $"({_upgradeCount + 1}/{MAX_UPGRADES}). " +
//////////////                  $"Duration: {CurrentData.upgradeDuration}s.");
//////////////        return true;
//////////////    }

//////////////    /// <summary>
//////////////    /// Applies the upgrade gains to THIS slot's live stats.
//////////////    /// Called automatically by Update() when the timer expires.
//////////////    /// </summary>
//////////////    public void CompleteUpgrade()
//////////////    {
//////////////        if (!_isUpgrading) return;

//////////////        _isUpgrading = false;
//////////////        _upgradeCount++;

//////////////        HorseData d = CurrentData;
//////////////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
//////////////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
//////////////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

//////////////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
//////////////                  $"'{d.horseName}'. " +
//////////////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}");

//////////////        // If the panel is currently showing THIS slot, refresh its HUD live
//////////////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//////////////    }

//////////////    /// <summary>Cancels an in-progress upgrade without applying any gains.</summary>
//////////////    public void CancelUpgrade()
//////////////    {
//////////////        _isUpgrading = false;
//////////////    }

//////////////    // ─── UI ───────────────────────────────────────────────────────────────────

//////////////    private void RefreshUI()
//////////////    {
//////////////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// HorseSlot — one of the active slots in the horse area.
///////////////
/////////////// CHANGES IN THIS VERSION:
///////////////   • UnequipHorse() now returns the HorseData that was removed so the
///////////////     caller (HorseArea / HorsePanelManager) can handle ownership correctly.
///////////////   • SellRefundPercent exposes the correct refund rate based on upgrade
///////////////     count: 0 upgrades = 50 %, 1 = 60 %, 2 = 70 %, 3 = 80 %.
///////////////   • Upgrade timer, live stats, and all existing behaviour is unchanged.
/////////////// </summary>
////////////public class HorseSlot : MonoBehaviour
////////////{
////////////    [Header("Children")]
////////////    [SerializeField] private RectTransform spawnPoint;
////////////    [SerializeField] private GameObject emptyGroup;

////////////    // ─── Horse reference ──────────────────────────────────────────────────────

////////////    private HorseController _horse;
////////////    public bool IsOccupied => _horse != null;
////////////    public HorseData CurrentData => _horse?.Data;

////////////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

////////////    private int _upgradeCount = 0;
////////////    private float _currentHealth;
////////////    private float _currentAbility;
////////////    private float _currentDamage;
////////////    private bool _isUpgrading = false;
////////////    private float _upgradeEndTime;

////////////    public const int MAX_UPGRADES = 3;

////////////    public int UpgradeCount => _upgradeCount;
////////////    public bool IsUpgrading => _isUpgrading;
////////////    public float CurrentHealth => _currentHealth;
////////////    public float CurrentAbility => _currentAbility;
////////////    public float CurrentDamage => _currentDamage;

////////////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
////////////    public float UpgradeTimeRemaining =>
////////////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

////////////    /// <summary>
////////////    /// Sell refund percentage based on upgrade count.
////////////    ///   0 upgrades → 50 %
////////////    ///   1 upgrade  → 60 %
////////////    ///   2 upgrades → 70 %
////////////    ///   3 upgrades → 80 %
////////////    /// </summary>
////////////    public float SellRefundPercent
////////////    {
////////////        get
////////////        {
////////////            switch (_upgradeCount)
////////////            {
////////////                case 1: return 0.60f;
////////////                case 2: return 0.70f;
////////////                case 3: return 0.80f;
////////////                default: return 0.50f;   // 0 upgrades (base)
////////////            }
////////////        }
////////////    }

////////////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

////////////    private void Awake() => RefreshUI();

////////////    private void Update()
////////////    {
////////////        // Timer runs regardless of whether the panel is open
////////////        if (_isUpgrading && Time.time >= _upgradeEndTime)
////////////            CompleteUpgrade();
////////////    }

////////////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

////////////    public void Equip(HorseData data)
////////////    {
////////////        if (IsOccupied) UnequipHorse();
////////////        if (data.prefab == null)
////////////        {
////////////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
////////////            return;
////////////        }

////////////        GameObject go = Instantiate(data.prefab, spawnPoint);
////////////        RectTransform rt = go.GetComponent<RectTransform>();
////////////        if (rt != null)
////////////        {
////////////            rt.anchoredPosition = Vector2.zero;
////////////            rt.localScale = Vector3.one;
////////////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
////////////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
////////////        }

////////////        _horse = go.GetComponent<HorseController>();
////////////        _horse?.Setup(data);

////////////        // Initialise live stats from the ScriptableObject BASE values.
////////////        // These are our own copies — we never write back to the asset.
////////////        _upgradeCount = 0;
////////////        _currentHealth = data.health;
////////////        _currentAbility = data.ability;
////////////        _currentDamage = data.damage;
////////////        _isUpgrading = false;

////////////        // Tap the horse to open Update mode for THIS slot
////////////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
////////////        btn.transition = Selectable.Transition.None;
////////////        btn.onClick.RemoveAllListeners();
////////////        btn.onClick.AddListener(OnHorseTapped);

////////////        RefreshUI();
////////////    }

////////////    /// <summary>
////////////    /// Removes the horse from this slot WITHOUT selling it.
////////////    /// Returns the HorseData that was unequipped (null if slot was empty).
////////////    /// The horse stays in the owner's inventory — it is NOT removed from HorseArea.
////////////    /// </summary>
////////////    public HorseData UnequipHorse()
////////////    {
////////////        if (!IsOccupied) return null;

////////////        // Cancel any in-progress upgrade (gains are lost on unequip)
////////////        _isUpgrading = false;

////////////        HorseData unequipped = _horse.Data;
////////////        Destroy(_horse.gameObject);
////////////        _horse = null;

////////////        RefreshUI();
////////////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
////////////        return unequipped;
////////////    }

////////////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

////////////    // ─── Upgrade API (called by HorsePanelManager) ───────────────────────────

////////////    /// <summary>
////////////    /// Starts the upgrade countdown for this specific slot.
////////////    /// Returns false if the slot is empty, already upgrading, or at max level.
////////////    /// </summary>
////////////    public bool StartUpgrade()
////////////    {
////////////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
////////////            return false;

////////////        _isUpgrading = true;
////////////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

////////////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
////////////                  $"({_upgradeCount + 1}/{MAX_UPGRADES}). " +
////////////                  $"Duration: {CurrentData.upgradeDuration}s.");
////////////        return true;
////////////    }

////////////    /// <summary>
////////////    /// Applies the upgrade gains to THIS slot's live stats.
////////////    /// Called automatically by Update() when the timer expires.
////////////    /// </summary>
////////////    public void CompleteUpgrade()
////////////    {
////////////        if (!_isUpgrading) return;

////////////        _isUpgrading = false;
////////////        _upgradeCount++;

////////////        HorseData d = CurrentData;
////////////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
////////////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
////////////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

////////////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
////////////                  $"'{d.horseName}'. " +
////////////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
////////////                  $"SellRefund:{SellRefundPercent * 100:F0}%");

////////////        // If the panel is currently showing THIS slot, refresh its HUD live
////////////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
////////////    }

////////////    /// <summary>Cancels an in-progress upgrade without applying any gains.</summary>
////////////    public void CancelUpgrade()
////////////    {
////////////        _isUpgrading = false;
////////////    }

////////////    // ─── UI ───────────────────────────────────────────────────────────────────

////////////    private void RefreshUI()
////////////    {
////////////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// HorseSlot — one of the active slots in the horse area.
/////////////
///////////// FIX: Added _inventoryIndex so each slot knows WHICH copy (by list index)
/////////////      of a horse it holds. This lets HorseArea distinguish between
/////////////      two copies of the same HorseData type (e.g. Brown #0 vs Brown #1).
/////////////
/////////////      Equip(data, inventoryIndex) — new overload stores the index.
/////////////      InventoryIndex property exposes it to HorseArea / HorsePanelManager.
///////////// </summary>
//////////public class HorseSlot : MonoBehaviour
//////////{
//////////    [Header("Children")]
//////////    [SerializeField] private RectTransform spawnPoint;
//////////    [SerializeField] private GameObject emptyGroup;

//////////    // ─── Horse reference ──────────────────────────────────────────────────────

//////////    private HorseController _horse;
//////////    public bool IsOccupied => _horse != null;
//////////    public HorseData CurrentData => _horse?.Data;

//////////    // ─── BUG FIX: per-copy identity ──────────────────────────────────────────
//////////    // Stores the index in HorseArea._ownedHorses this slot is holding.
//////////    // -1 means "unknown / not set" (legacy path).
//////////    private int _inventoryIndex = -1;
//////////    public int InventoryIndex => _inventoryIndex;

//////////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

//////////    private int _upgradeCount = 0;
//////////    private float _currentHealth;
//////////    private float _currentAbility;
//////////    private float _currentDamage;
//////////    private bool _isUpgrading = false;
//////////    private float _upgradeEndTime;

//////////    public const int MAX_UPGRADES = 3;

//////////    public int UpgradeCount => _upgradeCount;
//////////    public bool IsUpgrading => _isUpgrading;
//////////    public float CurrentHealth => _currentHealth;
//////////    public float CurrentAbility => _currentAbility;
//////////    public float CurrentDamage => _currentDamage;

//////////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
//////////    public float UpgradeTimeRemaining =>
//////////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//////////    /// <summary>
//////////    /// Sell refund percentage based on upgrade count.
//////////    ///   0 upgrades → 50 %
//////////    ///   1 upgrade  → 60 %
//////////    ///   2 upgrades → 70 %
//////////    ///   3 upgrades → 80 %
//////////    /// </summary>
//////////    public float SellRefundPercent
//////////    {
//////////        get
//////////        {
//////////            switch (_upgradeCount)
//////////            {
//////////                case 1: return 0.60f;
//////////                case 2: return 0.70f;
//////////                case 3: return 0.80f;
//////////                default: return 0.50f;
//////////            }
//////////        }
//////////    }

//////////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//////////    private void Awake() => RefreshUI();

//////////    private void Update()
//////////    {
//////////        if (_isUpgrading && Time.time >= _upgradeEndTime)
//////////            CompleteUpgrade();
//////////    }

//////////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Equips a horse into this slot.
//////////    /// inventoryIndex = position of this copy in HorseArea._ownedHorses.
//////////    /// Pass -1 only from legacy code that doesn't track indices.
//////////    /// </summary>
//////////    public void Equip(HorseData data, int inventoryIndex = -1)
//////////    {
//////////        if (IsOccupied) UnequipHorse();
//////////        if (data.prefab == null)
//////////        {
//////////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
//////////            return;
//////////        }

//////////        GameObject go = Instantiate(data.prefab, spawnPoint);
//////////        RectTransform rt = go.GetComponent<RectTransform>();
//////////        if (rt != null)
//////////        {
//////////            rt.anchoredPosition = Vector2.zero;
//////////            rt.localScale = Vector3.one;
//////////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
//////////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
//////////        }

//////////        _horse = go.GetComponent<HorseController>();
//////////        _horse?.Setup(data);

//////////        // Store which copy this is
//////////        _inventoryIndex = inventoryIndex;

//////////        // Initialise live stats from the ScriptableObject BASE values.
//////////        _upgradeCount = 0;
//////////        _currentHealth = data.health;
//////////        _currentAbility = data.ability;
//////////        _currentDamage = data.damage;
//////////        _isUpgrading = false;

//////////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//////////        btn.transition = Selectable.Transition.None;
//////////        btn.onClick.RemoveAllListeners();
//////////        btn.onClick.AddListener(OnHorseTapped);

//////////        RefreshUI();
//////////    }

//////////    /// <summary>
//////////    /// Removes the horse from this slot WITHOUT selling it.
//////////    /// Returns the HorseData that was unequipped (null if slot was empty).
//////////    /// </summary>
//////////    public HorseData UnequipHorse()
//////////    {
//////////        if (!IsOccupied) return null;

//////////        _isUpgrading = false;

//////////        HorseData unequipped = _horse.Data;
//////////        Destroy(_horse.gameObject);
//////////        _horse = null;
//////////        _inventoryIndex = -1;   // reset identity

//////////        RefreshUI();
//////////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
//////////        return unequipped;
//////////    }

//////////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

//////////    // ─── Upgrade API ─────────────────────────────────────────────────────────

//////////    public bool StartUpgrade()
//////////    {
//////////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//////////            return false;

//////////        _isUpgrading = true;
//////////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

//////////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
//////////                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
//////////                  $"Duration: {CurrentData.upgradeDuration}s.");
//////////        return true;
//////////    }

//////////    public void CompleteUpgrade()
//////////    {
//////////        if (!_isUpgrading) return;

//////////        _isUpgrading = false;
//////////        _upgradeCount++;

//////////        HorseData d = CurrentData;
//////////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
//////////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
//////////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

//////////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
//////////                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
//////////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
//////////                  $"SellRefund:{SellRefundPercent * 100:F0}%");

//////////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//////////    }

//////////    public void CancelUpgrade() => _isUpgrading = false;

//////////    // ─── UI ───────────────────────────────────────────────────────────────────

//////////    private void RefreshUI()
//////////    {
//////////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

/////////// <summary>
/////////// HorseSlot — one of the active slots in the horse area.
///////////
/////////// FIX: Added _inventoryIndex so each slot knows WHICH copy (by list index)
///////////      of a horse it holds. This lets HorseArea distinguish between
///////////      two copies of the same HorseData type (e.g. Brown #0 vs Brown #1).
///////////
///////////      Equip(data, inventoryIndex) — new overload stores the index.
///////////      InventoryIndex property exposes it to HorseArea / HorsePanelManager.
/////////// </summary>
////////public class HorseSlot : MonoBehaviour
////////{
////////    [Header("Children")]
////////    [SerializeField] private RectTransform spawnPoint;
////////    [SerializeField] private GameObject emptyGroup;

////////    // ─── Horse reference ──────────────────────────────────────────────────────

////////    private HorseController _horse;
////////    public bool IsOccupied => _horse != null;
////////    public HorseData CurrentData => _horse?.Data;

////////    // ─── BUG FIX: per-copy identity ──────────────────────────────────────────
////////    // Stores the index in HorseArea._ownedHorses this slot is holding.
////////    // -1 means "unknown / not set" (legacy path).
////////    private int _inventoryIndex = -1;
////////    public int InventoryIndex => _inventoryIndex;

////////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

////////    private int _upgradeCount = 0;
////////    private float _currentHealth;
////////    private float _currentAbility;
////////    private float _currentDamage;
////////    private bool _isUpgrading = false;
////////    private float _upgradeEndTime;

////////    public const int MAX_UPGRADES = 3;

////////    public int UpgradeCount => _upgradeCount;
////////    public bool IsUpgrading => _isUpgrading;
////////    public float CurrentHealth => _currentHealth;
////////    public float CurrentAbility => _currentAbility;
////////    public float CurrentDamage => _currentDamage;

////////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
////////    public float UpgradeTimeRemaining =>
////////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

////////    /// <summary>
////////    /// Sell refund percentage based on upgrade count.
////////    ///   0 upgrades → 50 %
////////    ///   1 upgrade  → 60 %
////////    ///   2 upgrades → 70 %
////////    ///   3 upgrades → 80 %
////////    /// </summary>
////////    public float SellRefundPercent
////////    {
////////        get
////////        {
////////            switch (_upgradeCount)
////////            {
////////                case 1: return 0.60f;
////////                case 2: return 0.70f;
////////                case 3: return 0.80f;
////////                default: return 0.50f;
////////            }
////////        }
////////    }

////////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

////////    private void Awake() => RefreshUI();

////////    private void Update()
////////    {
////////        if (_isUpgrading && Time.time >= _upgradeEndTime)
////////            CompleteUpgrade();
////////    }

////////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Equips a horse into this slot.
////////    /// inventoryIndex = position of this copy in HorseArea._ownedHorses.
////////    /// Pass -1 only from legacy code that doesn't track indices.
////////    /// </summary>
////////    public void Equip(HorseData data, int inventoryIndex = -1)
////////    {
////////        if (IsOccupied) UnequipHorse();
////////        if (data.prefab == null)
////////        {
////////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
////////            return;
////////        }

////////        GameObject go = Instantiate(data.prefab, spawnPoint);
////////        RectTransform rt = go.GetComponent<RectTransform>();
////////        if (rt != null)
////////        {
////////            rt.anchoredPosition = Vector2.zero;
////////            rt.localScale = Vector3.one;
////////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
////////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
////////        }

////////        _horse = go.GetComponent<HorseController>();
////////        _horse?.Setup(data);

////////        // Store which copy this is
////////        _inventoryIndex = inventoryIndex;

////////        // Initialise live stats from the ScriptableObject BASE values.
////////        _upgradeCount = 0;
////////        _currentHealth = data.health;
////////        _currentAbility = data.ability;
////////        _currentDamage = data.damage;
////////        _isUpgrading = false;

////////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
////////        btn.transition = Selectable.Transition.None;
////////        btn.onClick.RemoveAllListeners();
////////        btn.onClick.AddListener(OnHorseTapped);

////////        RefreshUI();
////////    }

////////    /// <summary>
////////    /// Removes the horse from this slot WITHOUT selling it.
////////    /// Returns the HorseData that was unequipped (null if slot was empty).
////////    /// </summary>
////////    public HorseData UnequipHorse()
////////    {
////////        if (!IsOccupied) return null;

////////        _isUpgrading = false;

////////        HorseData unequipped = _horse.Data;
////////        Destroy(_horse.gameObject);
////////        _horse = null;
////////        _inventoryIndex = -1;   // reset identity

////////        RefreshUI();
////////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
////////        return unequipped;
////////    }

////////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

////////    // ─── Upgrade API ─────────────────────────────────────────────────────────

////////    public bool StartUpgrade()
////////    {
////////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
////////            return false;

////////        _isUpgrading = true;
////////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

////////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
////////                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
////////                  $"Duration: {CurrentData.upgradeDuration}s.");
////////        return true;
////////    }

////////    public void CompleteUpgrade()
////////    {
////////        if (!_isUpgrading) return;

////////        _isUpgrading = false;
////////        _upgradeCount++;

////////        HorseData d = CurrentData;
////////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
////////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
////////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

////////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
////////                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
////////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
////////                  $"SellRefund:{SellRefundPercent * 100:F0}%");

////////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
////////    }

////////    public void CancelUpgrade() => _isUpgrading = false;

////////    // ─── Upgrade state save / restore ─────────────────────────────────────────

////////    /// <summary>Snapshot of this slot's upgrade progress — used by HorseArea to
////////    /// persist progress across unequip/re-equip cycles.</summary>
////////    public struct UpgradeState
////////    {
////////        public int upgradeCount;
////////        public float health;
////////        public float ability;
////////        public float damage;
////////    }

////////    /// <summary>Returns the current upgrade progress of this slot.</summary>
////////    public UpgradeState GetUpgradeState() => new UpgradeState
////////    {
////////        upgradeCount = _upgradeCount,
////////        health = _currentHealth,
////////        ability = _currentAbility,
////////        damage = _currentDamage
////////    };

////////    /// <summary>
////////    /// Restores previously saved upgrade progress after re-equipping.
////////    /// Any in-progress upgrade timer is cancelled — only completed upgrades persist.
////////    /// </summary>
////////    public void RestoreUpgradeState(UpgradeState state)
////////    {
////////        _upgradeCount = state.upgradeCount;
////////        _currentHealth = state.health;
////////        _currentAbility = state.ability;
////////        _currentDamage = state.damage;
////////        _isUpgrading = false;   // mid-upgrade progress is intentionally not saved
////////        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
////////                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
////////    }

////////    // ─── UI ───────────────────────────────────────────────────────────────────

////////    private void RefreshUI()
////////    {
////////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// HorseSlot — one of the active slots in the horse area.
/////////
///////// ── Drag-and-drop ────────────────────────────────────────────────────────
/////////   Implements IDropHandler so horse icons (from the Horses panel) and
/////////   walk-zone horses (dragged back) can be dropped onto this slot.
/////////
/////////   The horse PREFAB must already have HorseDragHandler on it.
/////////   Equip() sets horseData and destroyOnSuccessfulDrop at runtime.
/////////
///////// ── Inspector setup ──────────────────────────────────────────────────────
/////////   • This GameObject needs an Image with Raycast Target = ON so the
/////////     EventSystem can detect drops on it.
/////////   • Optionally assign slotHighlight for a green hover tint.
///////// </summary>
//////public class HorseSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////{
//////    [Header("Children")]
//////    [SerializeField] private RectTransform spawnPoint;
//////    [SerializeField] private GameObject emptyGroup;

//////    [Header("Drop highlight (optional)")]
//////    [Tooltip("Image tinted green when a valid horse is dragged over this slot")]
//////    [SerializeField] private Image slotHighlight;

//////    // ─── Horse reference ──────────────────────────────────────────────────────

//////    private HorseController _horse;
//////    public bool IsOccupied => _horse != null;
//////    public HorseData CurrentData => _horse?.Data;

//////    // ─── Per-copy identity ────────────────────────────────────────────────────

//////    private int _inventoryIndex = -1;
//////    public int InventoryIndex => _inventoryIndex;

//////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

//////    private int _upgradeCount = 0;
//////    private float _currentHealth;
//////    private float _currentAbility;
//////    private float _currentDamage;
//////    private bool _isUpgrading = false;
//////    private float _upgradeEndTime;

//////    public const int MAX_UPGRADES = 3;

//////    public int UpgradeCount => _upgradeCount;
//////    public bool IsUpgrading => _isUpgrading;
//////    public float CurrentHealth => _currentHealth;
//////    public float CurrentAbility => _currentAbility;
//////    public float CurrentDamage => _currentDamage;

//////    public float UpgradeTimeRemaining =>
//////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//////    public float SellRefundPercent
//////    {
//////        get
//////        {
//////            switch (_upgradeCount)
//////            {
//////                case 1: return 0.60f;
//////                case 2: return 0.70f;
//////                case 3: return 0.80f;
//////                default: return 0.50f;
//////            }
//////        }
//////    }

//////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//////    private void Awake() => RefreshUI();

//////    private void Update()
//////    {
//////        if (_isUpgrading && Time.time >= _upgradeEndTime)
//////            CompleteUpgrade();
//////    }

//////    // ─── IDropHandler ─────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Receives a horse dropped from:
//////    ///   a) A panel icon  (destroyOnSuccessfulDrop = false — icon stays)
//////    ///   b) A walk-zone horse dragged back  (destroyOnSuccessfulDrop = true)
//////    /// </summary>
//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//////        if (drag == null || drag.horseData == null) return;

//////        // Don't allow a panel icon to re-equip the same data already in this slot
//////        if (IsOccupied && CurrentData == drag.horseData && !drag.destroyOnSuccessfulDrop)
//////            return;

//////        // If it came from the walk zone, clear the zone's reference
//////        if (drag.destroyOnSuccessfulDrop)
//////        {
//////            WalkZoneOwner owner = eventData.pointerDrag.GetComponent<WalkZoneOwner>();
//////            owner?.Zone?.NotifyHorseLeft();
//////        }

//////        // Signal success BEFORE OnEndDrag fires
//////        drag.RegisterSuccessfulDrop();

//////        Equip(drag.horseData, _inventoryIndex);
//////        SetHighlight(false);
//////    }

//////    public void OnPointerEnter(PointerEventData eventData)
//////    {
//////        if (eventData.pointerDrag == null) return;
//////        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
//////        if (!IsOccupied) SetHighlight(true);
//////    }

//////    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

//////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Equips a horse into this slot.
//////    /// inventoryIndex = position in HorseArea._ownedHorses (-1 if unknown).
//////    /// </summary>
//////    public void Equip(HorseData data, int inventoryIndex = -1)
//////    {
//////        if (IsOccupied) UnequipHorse();

//////        if (data.prefab == null)
//////        {
//////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab assigned!");
//////            return;
//////        }

//////        GameObject go = Instantiate(data.prefab, spawnPoint);

//////        RectTransform rt = go.GetComponent<RectTransform>();
//////        if (rt != null)
//////        {
//////            rt.anchoredPosition = Vector2.zero;
//////            rt.localScale = Vector3.one;
//////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
//////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
//////        }

//////        _horse = go.GetComponent<HorseController>();
//////        _horse?.Setup(data);

//////        _inventoryIndex = inventoryIndex;

//////        _upgradeCount = 0;
//////        _currentHealth = data.health;
//////        _currentAbility = data.ability;
//////        _currentDamage = data.damage;
//////        _isUpgrading = false;

//////        // Tap the horse to open the upgrade panel
//////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//////        btn.transition = Selectable.Transition.None;
//////        btn.onClick.RemoveAllListeners();
//////        btn.onClick.AddListener(OnHorseTapped);

//////        // Configure the HorseDragHandler that lives on the prefab.
//////        // destroyOnSuccessfulDrop = true so the horse removes itself when
//////        // successfully dragged to the HorseWalkZone.
//////        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
//////        if (drag != null)
//////        {
//////            drag.horseData = data;
//////            drag.destroyOnSuccessfulDrop = true;
//////        }
//////        else
//////        {
//////            Debug.LogWarning($"[HorseSlot] Prefab '{data.horseName}' has no HorseDragHandler. " +
//////                             "Add it to the prefab so the horse can be dragged to the walk zone.");
//////        }

//////        RefreshUI();
//////    }

//////    /// <summary>Removes the horse from this slot. Returns the removed HorseData.</summary>
//////    public HorseData UnequipHorse()
//////    {
//////        if (!IsOccupied) return null;

//////        _isUpgrading = false;
//////        HorseData unequipped = _horse.Data;
//////        Destroy(_horse.gameObject);
//////        _horse = null;
//////        _inventoryIndex = -1;

//////        RefreshUI();
//////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
//////        return unequipped;
//////    }

//////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

//////    // ─── Upgrade API ─────────────────────────────────────────────────────────

//////    public bool StartUpgrade()
//////    {
//////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//////            return false;

//////        _isUpgrading = true;
//////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

//////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
//////                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
//////                  $"Duration: {CurrentData.upgradeDuration}s.");
//////        return true;
//////    }

//////    public void CompleteUpgrade()
//////    {
//////        if (!_isUpgrading) return;

//////        _isUpgrading = false;
//////        _upgradeCount++;

//////        HorseData d = CurrentData;
//////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
//////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
//////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

//////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
//////                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
//////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
//////                  $"Refund:{SellRefundPercent * 100:F0}%");

//////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//////    }

//////    public void CancelUpgrade() => _isUpgrading = false;

//////    // ─── Upgrade state save / restore ────────────────────────────────────────

//////    public struct UpgradeState
//////    {
//////        public int upgradeCount;
//////        public float health;
//////        public float ability;
//////        public float damage;
//////    }

//////    public UpgradeState GetUpgradeState() => new UpgradeState
//////    {
//////        upgradeCount = _upgradeCount,
//////        health = _currentHealth,
//////        ability = _currentAbility,
//////        damage = _currentDamage
//////    };

//////    public void RestoreUpgradeState(UpgradeState state)
//////    {
//////        _upgradeCount = state.upgradeCount;
//////        _currentHealth = state.health;
//////        _currentAbility = state.ability;
//////        _currentDamage = state.damage;
//////        _isUpgrading = false;

//////        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
//////                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
//////    }

//////    // ─── UI ───────────────────────────────────────────────────────────────────

//////    private void SetHighlight(bool on)
//////    {
//////        if (slotHighlight == null) return;
//////        slotHighlight.color = on
//////            ? new Color(0.4f, 1f, 0.4f, 0.35f)
//////            : new Color(1f, 1f, 1f, 0f);
//////    }

//////    private void RefreshUI()
//////    {
//////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//////    }
//////}


////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// HorseSlot — one of the active slots in the horse area.
///////
/////// ── Drag-and-drop ────────────────────────────────────────────────────────
///////   Implements IDropHandler so horse icons (from the Horses panel) and
///////   walk-zone horses (dragged back) can be dropped onto this slot.
///////
///////   The horse PREFAB must already have HorseDragHandler on it.
///////   Equip() sets horseData and destroyOnSuccessfulDrop at runtime.
///////
/////// ── Inspector setup ──────────────────────────────────────────────────────
///////   • This GameObject needs an Image with Raycast Target = ON so the
///////     EventSystem can detect drops on it.
///////   • Optionally assign slotHighlight for a green hover tint.
/////// </summary>
////public class HorseSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
////{
////    [Header("Children")]
////    [SerializeField] private RectTransform spawnPoint;
////    [SerializeField] private GameObject emptyGroup;

////    [Header("Drop highlight (optional)")]
////    [Tooltip("Image tinted green when a valid horse is dragged over this slot")]
////    [SerializeField] private Image slotHighlight;

////    // ─── Horse reference ──────────────────────────────────────────────────────

////    private HorseController _horse;
////    public bool IsOccupied => _horse != null;
////    public HorseData CurrentData => _horse?.Data;

////    // ─── Per-copy identity ────────────────────────────────────────────────────

////    private int _inventoryIndex = -1;
////    public int InventoryIndex => _inventoryIndex;

////    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

////    private int _upgradeCount = 0;
////    private float _currentHealth;
////    private float _currentAbility;
////    private float _currentDamage;
////    private bool _isUpgrading = false;
////    private float _upgradeEndTime;

////    public const int MAX_UPGRADES = 3;

////    public int UpgradeCount => _upgradeCount;
////    public bool IsUpgrading => _isUpgrading;
////    public float CurrentHealth => _currentHealth;
////    public float CurrentAbility => _currentAbility;
////    public float CurrentDamage => _currentDamage;

////    public float UpgradeTimeRemaining =>
////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

////    public float SellRefundPercent
////    {
////        get
////        {
////            switch (_upgradeCount)
////            {
////                case 1: return 0.60f;
////                case 2: return 0.70f;
////                case 3: return 0.80f;
////                default: return 0.50f;
////            }
////        }
////    }

////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

////    private void Awake() => RefreshUI();

////    private void Update()
////    {
////        if (_isUpgrading && Time.time >= _upgradeEndTime)
////            CompleteUpgrade();
////    }

////    // ─── IDropHandler ─────────────────────────────────────────────────────────

////    /// <summary>
////    /// Receives a horse dropped from:
////    ///   a) A panel icon  (destroyOnSuccessfulDrop = false — icon stays)
////    ///   b) A walk-zone horse dragged back  (destroyOnSuccessfulDrop = true)
////    /// </summary>
////    public void OnDrop(PointerEventData eventData)
////    {
////        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
////        if (drag == null || drag.horseData == null) return;

////        // Don't allow a panel icon to re-equip the same data already in this slot
////        if (IsOccupied && CurrentData == drag.horseData && !drag.destroyOnSuccessfulDrop)
////            return;

////        // If it came from the walk zone, clear the zone's reference
////        if (drag.destroyOnSuccessfulDrop)
////        {
////            WalkZoneOwner owner = eventData.pointerDrag.GetComponent<WalkZoneOwner>();
////            owner?.Zone?.NotifyHorseLeft();
////        }

////        // Signal success BEFORE OnEndDrag fires
////        drag.RegisterSuccessfulDrop();

////        Equip(drag.horseData, _inventoryIndex);
////        SetHighlight(false);
////    }

////    public void OnPointerEnter(PointerEventData eventData)
////    {
////        if (eventData.pointerDrag == null) return;
////        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
////        SetHighlight(true);   // highlight whether the slot is empty OR occupied
////    }

////    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

////    /// <summary>
////    /// Equips a horse into this slot.
////    /// inventoryIndex = position in HorseArea._ownedHorses (-1 if unknown).
////    /// </summary>
////    public void Equip(HorseData data, int inventoryIndex = -1)
////    {
////        if (IsOccupied) UnequipHorse();

////        if (data.prefab == null)
////        {
////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab assigned!");
////            return;
////        }

////        GameObject go = Instantiate(data.prefab, spawnPoint);

////        RectTransform rt = go.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            rt.anchoredPosition = Vector2.zero;
////            rt.localScale = Vector3.one;
////            RectTransform pr = data.prefab.GetComponent<RectTransform>();
////            if (pr != null) rt.sizeDelta = pr.sizeDelta;
////        }

////        _horse = go.GetComponent<HorseController>();
////        _horse?.Setup(data);

////        _inventoryIndex = inventoryIndex;

////        _upgradeCount = 0;
////        _currentHealth = data.health;
////        _currentAbility = data.ability;
////        _currentDamage = data.damage;
////        _isUpgrading = false;

////        // Tap the horse to open the upgrade panel
////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
////        btn.transition = Selectable.Transition.None;
////        btn.onClick.RemoveAllListeners();
////        btn.onClick.AddListener(OnHorseTapped);

////        // Configure the HorseDragHandler that lives on the prefab.
////        // destroyOnSuccessfulDrop = true so the horse removes itself when
////        // successfully dragged to the HorseWalkZone.
////        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
////        if (drag != null)
////        {
////            drag.horseData = data;
////            drag.destroyOnSuccessfulDrop = true;
////            // When the horse is dragged away and accepted, refresh THIS slot's
////            // UI so the empty-group is shown immediately.
////            drag.onRemovedFromSlot = RefreshUI;
////        }
////        else
////        {
////            Debug.LogWarning($"[HorseSlot] Prefab '{data.horseName}' has no HorseDragHandler. " +
////                             "Add it to the prefab so the horse can be dragged to the walk zone.");
////        }

////        RefreshUI();
////    }

////    /// <summary>Removes the horse from this slot. Returns the removed HorseData.</summary>
////    public HorseData UnequipHorse()
////    {
////        if (!IsOccupied) return null;

////        _isUpgrading = false;
////        HorseData unequipped = _horse.Data;
////        Destroy(_horse.gameObject);
////        _horse = null;
////        _inventoryIndex = -1;

////        RefreshUI();
////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
////        return unequipped;
////    }

////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

////    // ─── Upgrade API ─────────────────────────────────────────────────────────

////    public bool StartUpgrade()
////    {
////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
////            return false;

////        _isUpgrading = true;
////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
////                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
////                  $"Duration: {CurrentData.upgradeDuration}s.");
////        return true;
////    }

////    public void CompleteUpgrade()
////    {
////        if (!_isUpgrading) return;

////        _isUpgrading = false;
////        _upgradeCount++;

////        HorseData d = CurrentData;
////        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
////        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
////        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

////        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
////                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
////                  $"Refund:{SellRefundPercent * 100:F0}%");

////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
////    }

////    public void CancelUpgrade() => _isUpgrading = false;

////    // ─── Upgrade state save / restore ────────────────────────────────────────

////    public struct UpgradeState
////    {
////        public int upgradeCount;
////        public float health;
////        public float ability;
////        public float damage;
////    }

////    public UpgradeState GetUpgradeState() => new UpgradeState
////    {
////        upgradeCount = _upgradeCount,
////        health = _currentHealth,
////        ability = _currentAbility,
////        damage = _currentDamage
////    };

////    public void RestoreUpgradeState(UpgradeState state)
////    {
////        _upgradeCount = state.upgradeCount;
////        _currentHealth = state.health;
////        _currentAbility = state.ability;
////        _currentDamage = state.damage;
////        _isUpgrading = false;

////        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
////                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
////    }

////    // ─── UI ───────────────────────────────────────────────────────────────────

////    private void SetHighlight(bool on)
////    {
////        if (slotHighlight == null) return;
////        slotHighlight.color = on
////            ? new Color(0.4f, 1f, 0.4f, 0.35f)
////            : new Color(1f, 1f, 1f, 0f);
////    }

////    private void RefreshUI()
////    {
////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// HorseSlot — one of the active slots in the horse area.
/////
///// ── Drag-and-drop changes in this version ────────────────────────────────
/////   • Equip() now sets drag.ownerSlot, drag.inventoryIndex and
/////     drag.onRemovedFromSlot so the source slot's UI refreshes correctly
/////     when a horse is dragged away.
/////
/////   • OnDrop() passes drag.inventoryIndex (the DRAGGED horse's index)
/////     instead of this slot's own _inventoryIndex, so the horse always
/////     lands in the correct slot in HorseArea's list.
/////
/////   • SWAP: if both slots are occupied and the horse comes from another
/////     slot, the two horses swap positions instead of the target horse
/////     being silently destroyed.
/////
/////   • OnPointerEnter() highlights this slot whenever a horse is dragged
/////     over it, whether it is occupied or empty.
/////
/////   • ClearHorseRef() lets the swap code null out the source slot's
/////     horse reference before re-equipping it, preventing the dragged
/////     GameObject from being destroyed mid-drag by UnequipHorse().
///// </summary>
//public class HorseSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    [Header("Children")]
//    [SerializeField] private RectTransform spawnPoint;
//    [SerializeField] private GameObject emptyGroup;

//    [Header("Drop highlight (optional)")]
//    [Tooltip("Image tinted green when a valid horse is dragged over this slot")]
//    [SerializeField] private Image slotHighlight;

//    // ─── Horse reference ──────────────────────────────────────────────────────

//    private HorseController _horse;
//    public bool IsOccupied => _horse != null;
//    public HorseData CurrentData => _horse?.Data;

//    // ─── Per-copy identity ────────────────────────────────────────────────────

//    private int _inventoryIndex = -1;
//    public int InventoryIndex => _inventoryIndex;

//    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

//    private int _upgradeCount = 0;
//    private float _currentHealth;
//    private float _currentAbility;
//    private float _currentDamage;
//    private bool _isUpgrading = false;
//    private float _upgradeEndTime;

//    public const int MAX_UPGRADES = 3;

//    public int UpgradeCount => _upgradeCount;
//    public bool IsUpgrading => _isUpgrading;
//    public float CurrentHealth => _currentHealth;
//    public float CurrentAbility => _currentAbility;
//    public float CurrentDamage => _currentDamage;

//    public float UpgradeTimeRemaining =>
//        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//    public float SellRefundPercent
//    {
//        get
//        {
//            switch (_upgradeCount)
//            {
//                case 1: return 0.60f;
//                case 2: return 0.70f;
//                case 3: return 0.80f;
//                default: return 0.50f;
//            }
//        }
//    }

//    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//    private void Awake() => RefreshUI();

//    private void Update()
//    {
//        if (_isUpgrading && Time.time >= _upgradeEndTime)
//            CompleteUpgrade();
//    }

//    // ─── IDropHandler ─────────────────────────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//        if (drag == null || drag.horseData == null) return;

//        // Walk-zone horses carry a WalkZoneOwner — clear the zone's reference.
//        WalkZoneOwner zoneOwner = eventData.pointerDrag.GetComponent<WalkZoneOwner>();
//        zoneOwner?.Zone?.NotifyHorseLeft();

//        bool comesFromSlot = drag.ownerSlot != null && zoneOwner == null;

//        // ── Prevent no-op: dropped back onto the same slot ────────────────────
//        if (comesFromSlot && drag.ownerSlot == this) return;

//        // ── SWAP: dragged from another slot onto an occupied slot ─────────────
//        if (comesFromSlot && IsOccupied && drag.ownerSlot != this)
//        {
//            HorseSlot source = drag.ownerSlot;
//            HorseData thisData = CurrentData;
//            int thisIndex = _inventoryIndex;
//            int sourceIndex = drag.inventoryIndex;

//            // Null out the source slot's horse ref BEFORE calling source.Equip().
//            // Without this, UnequipHorse() inside Equip() would Destroy the
//            // GameObject currently being dragged — it is still alive on the
//            // canvas root and will be destroyed correctly in OnEndDrag instead.
//            source.ClearHorseRef();

//            // Put this slot's horse data into the source slot
//            source.Equip(thisData, thisIndex);

//            // Put the dragged horse data into this slot
//            // (UnequipHorse inside Equip destroys this slot's old visual — fine,
//            //  source.Equip has already spawned a fresh one there.)
//            drag.RegisterSuccessfulDrop();
//            Equip(drag.horseData, sourceIndex);
//            SetHighlight(false);
//            return;
//        }

//        // ── Normal drop (empty slot, or walk-zone horse to any slot) ──────────
//        drag.RegisterSuccessfulDrop();

//        // Use the dragged horse's own inventory index, not this slot's old one.
//        int idx = drag.inventoryIndex >= 0 ? drag.inventoryIndex : _inventoryIndex;
//        Equip(drag.horseData, idx);
//        SetHighlight(false);
//    }

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (eventData.pointerDrag == null) return;
//        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
//        SetHighlight(true);   // highlight whether occupied or empty
//    }

//    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

//    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Equips a horse into this slot.
//    /// inventoryIndex = position in HorseArea._ownedHorses (-1 if unknown).
//    /// </summary>
//    public void Equip(HorseData data, int inventoryIndex = -1)
//    {
//        if (IsOccupied) UnequipHorse();

//        if (data.prefab == null)
//        {
//            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab assigned!");
//            return;
//        }

//        GameObject go = Instantiate(data.prefab, spawnPoint);

//        RectTransform rt = go.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale = Vector3.one;
//            RectTransform pr = data.prefab.GetComponent<RectTransform>();
//            if (pr != null) rt.sizeDelta = pr.sizeDelta;
//        }

//        _horse = go.GetComponent<HorseController>();
//        _horse?.Setup(data);

//        _inventoryIndex = inventoryIndex;

//        _upgradeCount = 0;
//        _currentHealth = data.health;
//        _currentAbility = data.ability;
//        _currentDamage = data.damage;
//        _isUpgrading = false;

//        // Tap the horse to open the upgrade panel
//        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//        btn.transition = Selectable.Transition.None;
//        btn.onClick.RemoveAllListeners();
//        btn.onClick.AddListener(OnHorseTapped);

//        // Wire up the HorseDragHandler so this horse can be dragged to the
//        // walk zone or swapped with another slot.
//        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
//        if (drag != null)
//        {
//            drag.horseData = data;
//            drag.destroyOnSuccessfulDrop = true;
//            drag.ownerSlot = this;          // ← back-reference to THIS slot
//            drag.inventoryIndex = inventoryIndex; // ← so drop targets know the index
//            drag.onRemovedFromSlot = RefreshUI;     // ← refreshes emptyGroup when taken
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseSlot] Prefab '{data.horseName}' has no HorseDragHandler. " +
//                             "Add it to the prefab so the horse can be dragged.");
//        }

//        RefreshUI();
//    }

//    /// <summary>Removes the horse from this slot. Returns the removed HorseData.</summary>
//    public HorseData UnequipHorse()
//    {
//        if (!IsOccupied) return null;

//        _isUpgrading = false;
//        HorseData unequipped = _horse.Data;
//        Destroy(_horse.gameObject);
//        _horse = null;
//        _inventoryIndex = -1;

//        RefreshUI();
//        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
//        return unequipped;
//    }

//    /// <summary>
//    /// Clears the horse reference WITHOUT destroying the GameObject.
//    /// Used during slot-to-slot swaps so the dragged object (currently on the
//    /// canvas root) is not destroyed by the next call to Equip / UnequipHorse.
//    /// </summary>
//    public void ClearHorseRef()
//    {
//        _horse = null;
//        _inventoryIndex = -1;
//        RefreshUI();
//    }

//    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

//    // ─── Upgrade API ─────────────────────────────────────────────────────────

//    public bool StartUpgrade()
//    {
//        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//            return false;

//        _isUpgrading = true;
//        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

//        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
//                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
//                  $"Duration: {CurrentData.upgradeDuration}s.");
//        return true;
//    }

//    public void CompleteUpgrade()
//    {
//        if (!_isUpgrading) return;

//        _isUpgrading = false;
//        _upgradeCount++;

//        HorseData d = CurrentData;
//        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
//        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
//        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

//        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
//                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
//                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
//                  $"Refund:{SellRefundPercent * 100:F0}%");

//        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//    }

//    public void CancelUpgrade() => _isUpgrading = false;

//    // ─── Upgrade state save / restore ────────────────────────────────────────

//    public struct UpgradeState
//    {
//        public int upgradeCount;
//        public float health, ability, damage;
//    }

//    public UpgradeState GetUpgradeState() => new UpgradeState
//    {
//        upgradeCount = _upgradeCount,
//        health = _currentHealth,
//        ability = _currentAbility,
//        damage = _currentDamage
//    };

//    public void RestoreUpgradeState(UpgradeState state)
//    {
//        _upgradeCount = state.upgradeCount;
//        _currentHealth = state.health;
//        _currentAbility = state.ability;
//        _currentDamage = state.damage;
//        _isUpgrading = false;

//        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
//                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
//    }

//    // ─── UI ───────────────────────────────────────────────────────────────────

//    private void SetHighlight(bool on)
//    {
//        if (slotHighlight == null) return;
//        slotHighlight.color = on
//            ? new Color(0.4f, 1f, 0.4f, 0.35f)
//            : new Color(1f, 1f, 1f, 0f);
//    }

//    private void RefreshUI()
//    {
//        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// HorseSlot — one of the active slots in the horse area.
///
/// ── Drag-and-drop rules ───────────────────────────────────────────────────
///   Any horse dropped onto an OCCUPIED slot SWAPS instead of destroys:
///
///   • Slot horse → empty slot       : normal equip
///   • Slot horse → occupied slot    : the two horses swap slots
///   • Walk-zone horse → empty slot  : horse moves from zone to slot
///   • Walk-zone horse → occupied slot: horse in this slot goes to the walk
///                                      zone, walk-zone horse comes here
///
///   Dropping onto the SAME slot is ignored.
///
/// ── Inspector setup ──────────────────────────────────────────────────────
///   • This GameObject needs an Image (alpha 0, Raycast Target ON) so the
///     EventSystem can detect drops.
/// </summary>
public class HorseSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Children")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private GameObject emptyGroup;

    [Header("Drop highlight (optional)")]
    [Tooltip("Image tinted green when a valid horse is dragged over this slot")]
    [SerializeField] private Image slotHighlight;

    // ─── Horse reference ──────────────────────────────────────────────────────

    private HorseController _horse;
    public bool IsOccupied => _horse != null;
    public HorseData CurrentData => _horse?.Data;

    // ─── Per-copy identity ────────────────────────────────────────────────────

    private int _inventoryIndex = -1;
    public int InventoryIndex => _inventoryIndex;

    // ─── Live upgrade state ───────────────────────────────────────────────────

    private int _upgradeCount = 0;
    private float _currentHealth;
    private float _currentAbility;
    private float _currentDamage;
    private bool _isUpgrading = false;
    private float _upgradeEndTime;

    public const int MAX_UPGRADES = 3;

    public int UpgradeCount => _upgradeCount;
    public bool IsUpgrading => _isUpgrading;
    public float CurrentHealth => _currentHealth;
    public float CurrentAbility => _currentAbility;
    public float CurrentDamage => _currentDamage;

    public float UpgradeTimeRemaining =>
        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

    public float SellRefundPercent
    {
        get
        {
            switch (_upgradeCount)
            {
                case 1: return 0.60f;
                case 2: return 0.70f;
                case 3: return 0.80f;
                default: return 0.50f;
            }
        }
    }

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake() => RefreshUI();

    private void Update()
    {
        if (_isUpgrading && Time.time >= _upgradeEndTime)
            CompleteUpgrade();
    }

    // ─── IDropHandler ─────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
        if (drag == null || drag.horseData == null) return;

        WalkZoneOwner zoneOwner = eventData.pointerDrag.GetComponent<WalkZoneOwner>();
        bool comesFromSlot = drag.ownerSlot != null && zoneOwner == null;
        bool comesFromZone = zoneOwner != null;

        // ── Dropped back onto the same slot — do nothing ──────────────────────
        if (comesFromSlot && drag.ownerSlot == this) return;

        // ── SLOT → OCCUPIED SLOT: swap the two horses ─────────────────────────
        if (comesFromSlot && IsOccupied)
        {
            HorseSlot source = drag.ownerSlot;
            HorseData sourceData = drag.horseData;
            int sourceIdx = drag.inventoryIndex;
            HorseData thisData = CurrentData;
            int thisIdx = _inventoryIndex;

            // Clear source slot ref WITHOUT destroying the dragged GO
            // (it's on the canvas root and will be destroyed by OnEndDrag)
            source.ClearHorseRef();

            // Put this slot's horse into the source slot
            source.Equip(thisData, thisIdx);

            // Put the dragged horse into this slot
            drag.RegisterSuccessfulDrop();
            Equip(sourceData, sourceIdx);

            SetHighlight(false);
            return;
        }

        // ── WALK-ZONE HORSE → OCCUPIED SLOT: swap with the walk zone ──────────
        if (comesFromZone && IsOccupied)
        {
            HorseData displaced = CurrentData;
            int displacedIdx = _inventoryIndex;

            // Extract any soldier from the dragged horse BEFORE Equip() destroys
            // the old prefab instance. The soldier is re-mounted onto the new
            // slot horse instance that Equip() creates below.
            HorseController dragHC = eventData.pointerDrag.GetComponent<HorseController>();
            SoldierDragDrop transferSoldier = dragHC != null
                ? dragHC.ExtractRiderForTransfer()
                : null;

            // Notify zone that THIS SPECIFIC horse is leaving — other horses
            // still walking in the zone are left untouched.
            zoneOwner.Zone.NotifyHorseLeft(zoneOwner);

            // Accept the walk-zone horse into this slot (creates new prefab instance)
            drag.RegisterSuccessfulDrop();
            Equip(drag.horseData, drag.inventoryIndex >= 0 ? drag.inventoryIndex : _inventoryIndex);

            // Re-mount the soldier onto the freshly spawned slot horse
            if (transferSoldier != null && _horse != null)
                _horse.PerformMount(transferSoldier);

            // Send the displaced slot horse to the walk zone
            zoneOwner.Zone.SpawnWalkingHorse(displaced, displacedIdx);

            SetHighlight(false);
            return;
        }

        // ── WALK-ZONE HORSE → EMPTY SLOT: simple move ─────────────────────────
        if (comesFromZone && !IsOccupied)
        {
            // Extract any soldier from the dragged horse BEFORE Equip() destroys
            // the old prefab instance.
            HorseController dragHC = eventData.pointerDrag.GetComponent<HorseController>();
            SoldierDragDrop transferSoldier = dragHC != null
                ? dragHC.ExtractRiderForTransfer()
                : null;

            zoneOwner.Zone.NotifyHorseLeft(zoneOwner);
            drag.RegisterSuccessfulDrop();
            int idx = drag.inventoryIndex >= 0 ? drag.inventoryIndex : _inventoryIndex;
            Equip(drag.horseData, idx);

            // Re-mount the soldier onto the freshly spawned slot horse
            if (transferSoldier != null && _horse != null)
                _horse.PerformMount(transferSoldier);

            SetHighlight(false);
            return;
        }

        //// ── SLOT HORSE → EMPTY SLOT: simple move ──────────────────────────────
        //if (comesFromSlot && !IsOccupied)
        //{
        //    drag.RegisterSuccessfulDrop();
        //    int idx = drag.inventoryIndex >= 0 ? drag.inventoryIndex : _inventoryIndex;
        //    Equip(drag.horseData, idx);
        //    SetHighlight(false);
        //    return;
        //}

        if (comesFromSlot && !IsOccupied)
        {
            drag.ownerSlot?.ClearHorseRef(); // ← ADD: clear source slot NOW, before OnEndDrag fires
            drag.RegisterSuccessfulDrop();
            int idx = drag.inventoryIndex >= 0 ? drag.inventoryIndex : _inventoryIndex;
            Equip(drag.horseData, idx);
            SetHighlight(false);
            return;
        }

        // ── PANEL ICON (no ownerSlot, no zone) → any slot ────────────────────
        // e.g. Horse1/2/3/4 icons in the HorseArea panel
        drag.RegisterSuccessfulDrop();
        Equip(drag.horseData, _inventoryIndex);
        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

    // ─── Equip / Unequip ─────────────────────────────────────────────────────

    public void Equip(HorseData data, int inventoryIndex = -1)
    {
        if (IsOccupied) UnequipHorse();

        if (data.prefab == null)
        {
            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab assigned!");
            return;
        }

        GameObject go = Instantiate(data.prefab, spawnPoint);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            RectTransform pr = data.prefab.GetComponent<RectTransform>();
            if (pr != null) rt.sizeDelta = pr.sizeDelta;
        }

        _horse = go.GetComponent<HorseController>();
        _horse?.Setup(data);

        _inventoryIndex = inventoryIndex;
        _upgradeCount = 0;
        _currentHealth = data.health;
        _currentAbility = data.ability;
        _currentDamage = data.damage;
        _isUpgrading = false;

        // Tap the horse to open the upgrade panel
        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnHorseTapped);

        // Configure the HorseDragHandler that lives on the prefab
        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
        if (drag != null)
        {
            drag.horseData = data;
            drag.destroyOnSuccessfulDrop = true;
            drag.ownerSlot = this;
            drag.inventoryIndex = inventoryIndex;
            drag.onRemovedFromSlot = RefreshUI;
        }
        else
        {
            Debug.LogWarning($"[HorseSlot] Prefab '{data.horseName}' has no HorseDragHandler — " +
                             "add it to the prefab so the horse can be dragged.");
        }

        RefreshUI();
    }

    /// <summary>Removes the horse, destroys its GameObject, returns the HorseData.</summary>
    public HorseData UnequipHorse()
    {
        if (!IsOccupied) return null;

        _isUpgrading = false;
        HorseData unequipped = _horse.Data;

        // ── Eject any mounted soldier BEFORE destroying the horse ─────────────
        // A soldier parented inside SoldierSeat is a child of _horse.gameObject.
        // Calling Destroy(_horse.gameObject) without ejecting first would also
        // destroy the soldier, removing it from the game permanently.
        // EjectRiderBeforeDestroy() reparents the soldier back to its pre-mount
        // home (WalkZone parent) and re-enables it so it survives the destroy.
        // This mirrors the same fix already present in HorseDragHandler.OnEndDrag.
        _horse.EjectRiderBeforeDestroy();
        // ─────────────────────────────────────────────────────────────────────

        Destroy(_horse.gameObject);
        _horse = null;
        _inventoryIndex = -1;

        RefreshUI();
        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
        return unequipped;
    }

    /// <summary>
    /// Clears the horse reference WITHOUT destroying the GameObject.
    /// Used during slot-to-slot swaps so the dragged horse (on the canvas root)
    /// is not destroyed by the next Equip call.
    /// </summary>
    public void ClearHorseRef()
    {
        _horse = null;
        _inventoryIndex = -1;
        RefreshUI();
    }

    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

    // ─── Upgrade API ─────────────────────────────────────────────────────────

    public bool StartUpgrade()
    {
        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading) return false;
        _isUpgrading = true;
        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;
        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
                  $"(copy #{_inventoryIndex}, {_upgradeCount + 1}/{MAX_UPGRADES}). " +
                  $"Duration: {CurrentData.upgradeDuration}s.");
        return true;
    }

    public void CompleteUpgrade()
    {
        if (!_isUpgrading) return;
        _isUpgrading = false;
        _upgradeCount++;

        HorseData d = CurrentData;
        _currentHealth = Mathf.Min(_currentHealth + d.upgradeHealthGain, 100f);
        _currentAbility = Mathf.Min(_currentAbility + d.upgradeAbilityGain, 100f);
        _currentDamage = Mathf.Min(_currentDamage + d.upgradeDamageGain, 100f);

        Debug.Log($"[HorseSlot] Upgrade {_upgradeCount}/{MAX_UPGRADES} complete on " +
                  $"'{d.horseName}' (copy #{_inventoryIndex}). " +
                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
                  $"Refund:{SellRefundPercent * 100:F0}%");

        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
    }

    public void CancelUpgrade() => _isUpgrading = false;

    // ─── Upgrade state save / restore ────────────────────────────────────────

    public struct UpgradeState
    {
        public int upgradeCount;
        public float health, ability, damage;
    }

    public UpgradeState GetUpgradeState() => new UpgradeState
    {
        upgradeCount = _upgradeCount,
        health = _currentHealth,
        ability = _currentAbility,
        damage = _currentDamage
    };

    public void RestoreUpgradeState(UpgradeState state)
    {
        _upgradeCount = state.upgradeCount;
        _currentHealth = state.health;
        _currentAbility = state.ability;
        _currentDamage = state.damage;
        _isUpgrading = false;
        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    private void SetHighlight(bool on)
    {
        if (slotHighlight == null) return;
        slotHighlight.color = on
            ? new Color(0.4f, 1f, 0.4f, 0.35f)
            : new Color(1f, 1f, 1f, 0f);
    }

    private void RefreshUI()
    {
        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
    }
}