//////using UnityEngine;
//////using UnityEngine.UI;


//////public class HorseSlot : MonoBehaviour
//////{
//////    [Header("Children")]
//////    [SerializeField] private RectTransform spawnPoint;
//////    [SerializeField] private GameObject emptyGroup;

//////    // ─── Horse reference ──────────────────────────────────────────────────────

//////    private HorseController _horse;
//////    public bool IsOccupied => _horse != null;
//////    public HorseData CurrentData => _horse?.Data;

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

//////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
//////    public float UpgradeTimeRemaining =>
//////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//////    private void Awake() => RefreshUI();

//////    private void Update()
//////    {
//////        // Timer runs regardless of whether the panel is open
//////        if (_isUpgrading && Time.time >= _upgradeEndTime)
//////            CompleteUpgrade();
//////    }

//////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//////    public void Equip(HorseData data)
//////    {
//////        if (IsOccupied) UnequipHorse();
//////        if (data.prefab == null)
//////        {
//////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
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

//////        // Initialise live stats from the ScriptableObject BASE values.
//////        // These are our own copies — we never write back to the asset.
//////        _upgradeCount = 0;
//////        _currentHealth = data.health;
//////        _currentAbility = data.ability;
//////        _currentDamage = data.damage;
//////        _isUpgrading = false;

//////        // Tap the horse to open Update mode for THIS slot
//////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//////        btn.transition = Selectable.Transition.None;
//////        btn.onClick.RemoveAllListeners();
//////        btn.onClick.AddListener(OnHorseTapped);

//////        RefreshUI();
//////    }

//////    public void UnequipHorse()
//////    {
//////        if (!IsOccupied) return;
//////        _isUpgrading = false;
//////        Destroy(_horse.gameObject);
//////        _horse = null;
//////        RefreshUI();
//////    }

//////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

//////    // ─── Upgrade API (called by HorsePanelManager) ───────────────────────────

//////    /// <summary>
//////    /// Starts the upgrade countdown for this specific slot.
//////    /// Returns false if the slot is empty, already upgrading, or at max level.
//////    /// </summary>
//////    public bool StartUpgrade()
//////    {
//////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//////            return false;

//////        _isUpgrading = true;
//////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

//////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
//////                  $"({_upgradeCount + 1}/{MAX_UPGRADES}). " +
//////                  $"Duration: {CurrentData.upgradeDuration}s.");
//////        return true;
//////    }

//////    /// <summary>
//////    /// Applies the upgrade gains to THIS slot's live stats.
//////    /// Called automatically by Update() when the timer expires.
//////    /// </summary>
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
//////                  $"'{d.horseName}'. " +
//////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}");

//////        // If the panel is currently showing THIS slot, refresh its HUD live
//////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//////    }

//////    /// <summary>Cancels an in-progress upgrade without applying any gains.</summary>
//////    public void CancelUpgrade()
//////    {
//////        _isUpgrading = false;
//////    }

//////    // ─── UI ───────────────────────────────────────────────────────────────────

//////    private void RefreshUI()
//////    {
//////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// HorseSlot — one of the active slots in the horse area.
///////
/////// CHANGES IN THIS VERSION:
///////   • UnequipHorse() now returns the HorseData that was removed so the
///////     caller (HorseArea / HorsePanelManager) can handle ownership correctly.
///////   • SellRefundPercent exposes the correct refund rate based on upgrade
///////     count: 0 upgrades = 50 %, 1 = 60 %, 2 = 70 %, 3 = 80 %.
///////   • Upgrade timer, live stats, and all existing behaviour is unchanged.
/////// </summary>
////public class HorseSlot : MonoBehaviour
////{
////    [Header("Children")]
////    [SerializeField] private RectTransform spawnPoint;
////    [SerializeField] private GameObject emptyGroup;

////    // ─── Horse reference ──────────────────────────────────────────────────────

////    private HorseController _horse;
////    public bool IsOccupied => _horse != null;
////    public HorseData CurrentData => _horse?.Data;

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

////    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
////    public float UpgradeTimeRemaining =>
////        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

////    /// <summary>
////    /// Sell refund percentage based on upgrade count.
////    ///   0 upgrades → 50 %
////    ///   1 upgrade  → 60 %
////    ///   2 upgrades → 70 %
////    ///   3 upgrades → 80 %
////    /// </summary>
////    public float SellRefundPercent
////    {
////        get
////        {
////            switch (_upgradeCount)
////            {
////                case 1: return 0.60f;
////                case 2: return 0.70f;
////                case 3: return 0.80f;
////                default: return 0.50f;   // 0 upgrades (base)
////            }
////        }
////    }

////    // ─── Unity lifecycle ──────────────────────────────────────────────────────

////    private void Awake() => RefreshUI();

////    private void Update()
////    {
////        // Timer runs regardless of whether the panel is open
////        if (_isUpgrading && Time.time >= _upgradeEndTime)
////            CompleteUpgrade();
////    }

////    // ─── Equip / Unequip ─────────────────────────────────────────────────────

////    public void Equip(HorseData data)
////    {
////        if (IsOccupied) UnequipHorse();
////        if (data.prefab == null)
////        {
////            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
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

////        // Initialise live stats from the ScriptableObject BASE values.
////        // These are our own copies — we never write back to the asset.
////        _upgradeCount = 0;
////        _currentHealth = data.health;
////        _currentAbility = data.ability;
////        _currentDamage = data.damage;
////        _isUpgrading = false;

////        // Tap the horse to open Update mode for THIS slot
////        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
////        btn.transition = Selectable.Transition.None;
////        btn.onClick.RemoveAllListeners();
////        btn.onClick.AddListener(OnHorseTapped);

////        RefreshUI();
////    }

////    /// <summary>
////    /// Removes the horse from this slot WITHOUT selling it.
////    /// Returns the HorseData that was unequipped (null if slot was empty).
////    /// The horse stays in the owner's inventory — it is NOT removed from HorseArea.
////    /// </summary>
////    public HorseData UnequipHorse()
////    {
////        if (!IsOccupied) return null;

////        // Cancel any in-progress upgrade (gains are lost on unequip)
////        _isUpgrading = false;

////        HorseData unequipped = _horse.Data;
////        Destroy(_horse.gameObject);
////        _horse = null;

////        RefreshUI();
////        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
////        return unequipped;
////    }

////    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

////    // ─── Upgrade API (called by HorsePanelManager) ───────────────────────────

////    /// <summary>
////    /// Starts the upgrade countdown for this specific slot.
////    /// Returns false if the slot is empty, already upgrading, or at max level.
////    /// </summary>
////    public bool StartUpgrade()
////    {
////        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
////            return false;

////        _isUpgrading = true;
////        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;

////        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}' " +
////                  $"({_upgradeCount + 1}/{MAX_UPGRADES}). " +
////                  $"Duration: {CurrentData.upgradeDuration}s.");
////        return true;
////    }

////    /// <summary>
////    /// Applies the upgrade gains to THIS slot's live stats.
////    /// Called automatically by Update() when the timer expires.
////    /// </summary>
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
////                  $"'{d.horseName}'. " +
////                  $"HP:{_currentHealth:F0}  AB:{_currentAbility:F0}  DM:{_currentDamage:F0}  " +
////                  $"SellRefund:{SellRefundPercent * 100:F0}%");

////        // If the panel is currently showing THIS slot, refresh its HUD live
////        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
////    }

////    /// <summary>Cancels an in-progress upgrade without applying any gains.</summary>
////    public void CancelUpgrade()
////    {
////        _isUpgrading = false;
////    }

////    // ─── UI ───────────────────────────────────────────────────────────────────

////    private void RefreshUI()
////    {
////        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// HorseSlot — one of the active slots in the horse area.
/////
///// FIX: Added _inventoryIndex so each slot knows WHICH copy (by list index)
/////      of a horse it holds. This lets HorseArea distinguish between
/////      two copies of the same HorseData type (e.g. Brown #0 vs Brown #1).
/////
/////      Equip(data, inventoryIndex) — new overload stores the index.
/////      InventoryIndex property exposes it to HorseArea / HorsePanelManager.
///// </summary>
//public class HorseSlot : MonoBehaviour
//{
//    [Header("Children")]
//    [SerializeField] private RectTransform spawnPoint;
//    [SerializeField] private GameObject emptyGroup;

//    // ─── Horse reference ──────────────────────────────────────────────────────

//    private HorseController _horse;
//    public bool IsOccupied => _horse != null;
//    public HorseData CurrentData => _horse?.Data;

//    // ─── BUG FIX: per-copy identity ──────────────────────────────────────────
//    // Stores the index in HorseArea._ownedHorses this slot is holding.
//    // -1 means "unknown / not set" (legacy path).
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

//    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
//    public float UpgradeTimeRemaining =>
//        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//    /// <summary>
//    /// Sell refund percentage based on upgrade count.
//    ///   0 upgrades → 50 %
//    ///   1 upgrade  → 60 %
//    ///   2 upgrades → 70 %
//    ///   3 upgrades → 80 %
//    /// </summary>
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

//    // ─── Equip / Unequip ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Equips a horse into this slot.
//    /// inventoryIndex = position of this copy in HorseArea._ownedHorses.
//    /// Pass -1 only from legacy code that doesn't track indices.
//    /// </summary>
//    public void Equip(HorseData data, int inventoryIndex = -1)
//    {
//        if (IsOccupied) UnequipHorse();
//        if (data.prefab == null)
//        {
//            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
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

//        // Store which copy this is
//        _inventoryIndex = inventoryIndex;

//        // Initialise live stats from the ScriptableObject BASE values.
//        _upgradeCount = 0;
//        _currentHealth = data.health;
//        _currentAbility = data.ability;
//        _currentDamage = data.damage;
//        _isUpgrading = false;

//        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
//        btn.transition = Selectable.Transition.None;
//        btn.onClick.RemoveAllListeners();
//        btn.onClick.AddListener(OnHorseTapped);

//        RefreshUI();
//    }

//    /// <summary>
//    /// Removes the horse from this slot WITHOUT selling it.
//    /// Returns the HorseData that was unequipped (null if slot was empty).
//    /// </summary>
//    public HorseData UnequipHorse()
//    {
//        if (!IsOccupied) return null;

//        _isUpgrading = false;

//        HorseData unequipped = _horse.Data;
//        Destroy(_horse.gameObject);
//        _horse = null;
//        _inventoryIndex = -1;   // reset identity

//        RefreshUI();
//        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
//        return unequipped;
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
//                  $"SellRefund:{SellRefundPercent * 100:F0}%");

//        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
//    }

//    public void CancelUpgrade() => _isUpgrading = false;

//    // ─── UI ───────────────────────────────────────────────────────────────────

//    private void RefreshUI()
//    {
//        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorseSlot — one of the active slots in the horse area.
///
/// FIX: Added _inventoryIndex so each slot knows WHICH copy (by list index)
///      of a horse it holds. This lets HorseArea distinguish between
///      two copies of the same HorseData type (e.g. Brown #0 vs Brown #1).
///
///      Equip(data, inventoryIndex) — new overload stores the index.
///      InventoryIndex property exposes it to HorseArea / HorsePanelManager.
/// </summary>
public class HorseSlot : MonoBehaviour
{
    [Header("Children")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private GameObject emptyGroup;

    // ─── Horse reference ──────────────────────────────────────────────────────

    private HorseController _horse;
    public bool IsOccupied => _horse != null;
    public HorseData CurrentData => _horse?.Data;

    // ─── BUG FIX: per-copy identity ──────────────────────────────────────────
    // Stores the index in HorseArea._ownedHorses this slot is holding.
    // -1 means "unknown / not set" (legacy path).
    private int _inventoryIndex = -1;
    public int InventoryIndex => _inventoryIndex;

    // ─── Live upgrade state (per-slot, NOT on HorseData) ─────────────────────

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

    /// <summary>Seconds remaining in the active upgrade countdown (0 if none).</summary>
    public float UpgradeTimeRemaining =>
        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

    /// <summary>
    /// Sell refund percentage based on upgrade count.
    ///   0 upgrades → 50 %
    ///   1 upgrade  → 60 %
    ///   2 upgrades → 70 %
    ///   3 upgrades → 80 %
    /// </summary>
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

    // ─── Equip / Unequip ─────────────────────────────────────────────────────

    /// <summary>
    /// Equips a horse into this slot.
    /// inventoryIndex = position of this copy in HorseArea._ownedHorses.
    /// Pass -1 only from legacy code that doesn't track indices.
    /// </summary>
    public void Equip(HorseData data, int inventoryIndex = -1)
    {
        if (IsOccupied) UnequipHorse();
        if (data.prefab == null)
        {
            Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!");
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

        // Store which copy this is
        _inventoryIndex = inventoryIndex;

        // Initialise live stats from the ScriptableObject BASE values.
        _upgradeCount = 0;
        _currentHealth = data.health;
        _currentAbility = data.ability;
        _currentDamage = data.damage;
        _isUpgrading = false;

        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnHorseTapped);

        RefreshUI();
    }

    /// <summary>
    /// Removes the horse from this slot WITHOUT selling it.
    /// Returns the HorseData that was unequipped (null if slot was empty).
    /// </summary>
    public HorseData UnequipHorse()
    {
        if (!IsOccupied) return null;

        _isUpgrading = false;

        HorseData unequipped = _horse.Data;
        Destroy(_horse.gameObject);
        _horse = null;
        _inventoryIndex = -1;   // reset identity

        RefreshUI();
        Debug.Log($"[HorseSlot] Unequipped '{unequipped.horseName}'.");
        return unequipped;
    }

    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

    // ─── Upgrade API ─────────────────────────────────────────────────────────

    public bool StartUpgrade()
    {
        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
            return false;

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
                  $"SellRefund:{SellRefundPercent * 100:F0}%");

        HorsePanelManager.Instance?.OnSlotUpgradeComplete(this);
    }

    public void CancelUpgrade() => _isUpgrading = false;

    // ─── Upgrade state save / restore ─────────────────────────────────────────

    /// <summary>Snapshot of this slot's upgrade progress — used by HorseArea to
    /// persist progress across unequip/re-equip cycles.</summary>
    public struct UpgradeState
    {
        public int upgradeCount;
        public float health;
        public float ability;
        public float damage;
    }

    /// <summary>Returns the current upgrade progress of this slot.</summary>
    public UpgradeState GetUpgradeState() => new UpgradeState
    {
        upgradeCount = _upgradeCount,
        health = _currentHealth,
        ability = _currentAbility,
        damage = _currentDamage
    };

    /// <summary>
    /// Restores previously saved upgrade progress after re-equipping.
    /// Any in-progress upgrade timer is cancelled — only completed upgrades persist.
    /// </summary>
    public void RestoreUpgradeState(UpgradeState state)
    {
        _upgradeCount = state.upgradeCount;
        _currentHealth = state.health;
        _currentAbility = state.ability;
        _currentDamage = state.damage;
        _isUpgrading = false;   // mid-upgrade progress is intentionally not saved
        Debug.Log($"[HorseSlot] Restored upgrade state: count={_upgradeCount} " +
                  $"HP:{_currentHealth:F0} AB:{_currentAbility:F0} DM:{_currentDamage:F0}");
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
    }
}