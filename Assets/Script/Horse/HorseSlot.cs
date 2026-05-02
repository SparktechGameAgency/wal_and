//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// HorseSlot — no buttons on the slot itself.
///// Buy/Sell/Upgrade is handled globally by HorseArea + HorsePanelManager.
/////
/////   HorseSlot_1
/////     ├── SpawnPoint
/////     └── EmptyGroup   ← shown when empty
///// </summary>
//public class HorseSlot : MonoBehaviour
//{
//    [Header("Children")]
//    [SerializeField] private RectTransform spawnPoint;
//    [SerializeField] private GameObject emptyGroup;   // shown when empty

//    // ─── Horse reference ──────────────────────────────────────────────────────

//    private HorseController _horse;
//    public bool IsOccupied => _horse != null;
//    public HorseData CurrentData => _horse?.Data;

//    // ─── Upgrade state ────────────────────────────────────────────────────────

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

//    /// <summary>Seconds remaining in the current upgrade (0 if not upgrading).</summary>
//    public float UpgradeTimeRemaining =>
//        _isUpgrading ? Mathf.Max(0f, _upgradeEndTime - Time.time) : 0f;

//    // ─── Unity lifecycle ──────────────────────────────────────────────────────

//    private void Update()
//    {
//        // Auto-complete when the timer expires (works even if panel is closed)
//        if (_isUpgrading && Time.time >= _upgradeEndTime)
//            CompleteUpgrade();
//    }

//    // ─── Spawn / Sell ─────────────────────────────────────────────────────────

//    public void Spawn(HorseData data)
//    {
//        if (IsOccupied) return;
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
//            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
//            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
//        }

//        _horse = go.GetComponent<HorseController>();
//        _horse?.Setup(data);

//        // Initialise live stats from the ScriptableObject base values
//        _upgradeCount = 0;
//        _currentHealth = data.health;
//        _currentAbility = data.ability;
//        _currentDamage = data.damage;
//        _isUpgrading = false;

//        RefreshUI();
//    }

//    public void SellHorse()
//    {
//        if (!IsOccupied) return;

//        // Cancel any in-progress upgrade
//        _isUpgrading = false;

//        HorseData data = _horse.Data;
//        int refund = Mathf.RoundToInt(data.cost * 0.5f);
//        Destroy(_horse.gameObject);
//        _horse = null;

//        HorsePanelManager.Instance?.AddGold(refund);
//        RefreshUI();
//        Debug.Log($"[HorseSlot] Sold '{data.horseName}'. Refund: {refund}g.");
//    }

//    // ─── Upgrade API ──────────────────────────────────────────────────────────

//    /// <summary>
//    /// Begins the upgrade countdown.
//    /// Returns false if already upgrading, fully upgraded, or slot is empty.
//    /// </summary>
//    public bool StartUpgrade()
//    {
//        if (!IsOccupied || _upgradeCount >= MAX_UPGRADES || _isUpgrading)
//            return false;

//        _isUpgrading = true;
//        _upgradeEndTime = Time.time + CurrentData.upgradeDuration;
//        Debug.Log($"[HorseSlot] Upgrade started on '{CurrentData.horseName}'. " +
//                  $"Duration: {CurrentData.upgradeDuration}s.");
//        return true;
//    }

//    /// <summary>
//    /// Finalises the upgrade: increments count and raises the live stats.
//    /// Called automatically by Update() when the timer expires.
//    /// </summary>
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
//                  $"'{d.horseName}'. HP:{_currentHealth} AB:{_currentAbility} DM:{_currentDamage}");
//    }

//    /// <summary>Cancels an in-progress upgrade without applying gains.</summary>
//    public void CancelUpgrade()
//    {
//        _isUpgrading = false;
//    }

//    // ─── UI ───────────────────────────────────────────────────────────────────

//    private void RefreshUI()
//    {
//        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorseSlot — one of the 2 active slots in the horse area.
/// Tapping an occupied slot opens Update/Equip panel for that horse.
/// </summary>
public class HorseSlot : MonoBehaviour
{
    [Header("Children")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private GameObject emptyGroup;

    private HorseController _horse;
    public bool IsOccupied => _horse != null;
    public HorseData CurrentData => _horse?.Data;

    private void Awake() => RefreshUI();

    // ─── Equip / Unequip ─────────────────────────────────────────────────────

    public void Equip(HorseData data)
    {
        if (IsOccupied) UnequipHorse();
        if (data.prefab == null) { Debug.LogError($"[HorseSlot] '{data.horseName}' has no prefab!"); return; }

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

        // Add a click listener to the spawned horse so tapping it opens Update mode
        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnHorseTapped);

        RefreshUI();
    }

    public void UnequipHorse()
    {
        if (!IsOccupied) return;
        Destroy(_horse.gameObject);
        _horse = null;
        RefreshUI();
    }

    private void OnHorseTapped() => HorsePanelManager.Instance?.OpenUpdateMode(this);

    private void RefreshUI()
    {
        if (emptyGroup != null) emptyGroup.SetActive(!IsOccupied);
    }
}