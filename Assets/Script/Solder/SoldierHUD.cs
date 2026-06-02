using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - SoldierHUD  (FIXED v3)
///
/// ROOT CAUSE OF HEALTH BAR NOT UPDATING:
///   Old code used stats.HealthPercent = CurrentHealth / MaxHealth.
///   When you equip armor, BOTH CurrentHealth and MaxHealth increase by the same
///   amount — so the ratio stays 1.0 (100%) forever. The bar was always full.
///
///   FIX: Health bar now uses  stats.MaxHealth / maxHealthDisplay
///   (same pattern as ability and damage bars).
///   Set maxHealthDisplay = the maximum possible MaxHealth in your game (e.g. 200).
///   Equipping armor increases MaxHealth → bar visibly grows. ✓
///
/// Inspector wiring:
///   Health Bar Fill     → HealthhudGreen  (Image Type: Filled / Horizontal / Left)
///   Ability Bar Fill    → AbilityBrown    (Image Type: Filled / Horizontal / Left)
///   Damage Bar Fill     → DamagehudRed    (Image Type: Filled / Horizontal / Left)
///   Health Text         → HealthNumber    (TextMeshPro)
///   Ability Text        → AbilityNumber   (TextMeshPro)
///   Damage Text         → DamageNumber    (TextMeshPro)
///   Max Health Display  → 200
///   Max Ability Display → 100
///   Max Damage Display  → 100
/// </summary>
public class SoldierHUD : MonoBehaviour
{
    // ─── Stat Bars ────────────────────────────────────────────────────────────

    [Header("Stat Bars — Image Type → Filled → Horizontal → Left")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image abilityBarFill;
    [SerializeField] private Image damageBarFill;

    // ─── Stat Labels ──────────────────────────────────────────────────────────

    [Header("Stat Labels (TextMeshPro — optional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;

    // ─── Bar Max Reference Values ─────────────────────────────────────────────

    [Header("Bar Max Reference Values")]
    [Tooltip("MaxHealth value that fills the health bar to 100%. E.g. if max possible HP is 200, set 200.")]
    [SerializeField] private float maxHealthDisplay = 100f;
    [Tooltip("AbilityPower value that fills the ability bar to 100%.")]
    [SerializeField] private float maxAbilityDisplay = 100f;
    [Tooltip("AttackDamage value that fills the damage bar to 100%.")]
    [SerializeField] private float maxDamageDisplay = 100f;

    // ─── Colours ──────────────────────────────────────────────────────────────

    [Header("Health Bar Colours")]
    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        GameManager.OnSoldierSpawned += OnSoldierSpawned;
    }

    private void Start()
    {
        TryFindAndLink();
    }

    private void OnDestroy()
    {
        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
        Unlink();
    }

    // ─── Spawn Callback ───────────────────────────────────────────────────────

    private void OnSoldierSpawned(GameObject soldierGO)
    {
        var newStats = soldierGO.GetComponent<SoldierStats>();
        if (newStats == null) return;
        Unlink();
        _stats = newStats;
        Link();
        RefreshHUD(_stats);
        Debug.Log($"[SoldierHUD] Re-linked to spawned soldier '{soldierGO.name}'.");
    }

    // ─── Link / Unlink ────────────────────────────────────────────────────────

    private void TryFindAndLink()
    {
        if (_stats != null) return;

        _stats = GetComponentInParent<SoldierStats>();

        if (_stats == null)
        {
            // AFTER (fix):
            _stats = FindFirstObjectByType<SoldierStats>();
            if (_stats != null)
                Debug.LogWarning("[SoldierHUD] Found SoldierStats via FindObjectOfType — fallback mode.");
        }

        if (_stats == null)
        {
            Debug.LogError("[SoldierHUD] No SoldierStats in scene! " +
                           "Add SoldierStats component to your Player GameObject.");
            return;
        }

        Link();
        RefreshHUD(_stats);
        Debug.Log($"[SoldierHUD] Linked to '{_stats.gameObject.name}'. " +
                  $"HP:{_stats.MaxHealth} AP:{_stats.AbilityPower} AD:{_stats.AttackDamage}");
    }

    private void Link()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged += RefreshHUD;
        _stats.OnSoldierDied += HandleDeath;
    }

    private void Unlink()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged -= RefreshHUD;
        _stats.OnSoldierDied -= HandleDeath;
        _stats = null;
    }

    // ─── Anti-Mirror (LateUpdate) ─────────────────────────────────────────────

    private void LateUpdate()
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // ─── HUD Refresh ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called automatically via OnStatsChanged every time an item is
    /// equipped / unequipped or the soldier takes damage / heals.
    ///
    /// KEY FIX: All three bars now use (value / maxDisplay) so each bar
    /// visibly grows when equipment bonuses increase a stat.
    /// Previously health used HealthPercent (CurrentHP/MaxHP) which is
    /// always 1.0 when the soldier is at full health — bar never moved.
    /// </summary>
    private void RefreshHUD(SoldierStats stats)
    {
        // ── Health ─────────────────────────────────────────────────────────────
        //  Fill = MaxHealth / maxHealthDisplay
        //  e.g. base 100 HP, maxHealthDisplay=200 → bar at 50%
        //       equip +10 armor → MaxHealth=110 → bar grows to 55% ✓
        float hpFill = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = hpFill;
            //healthBarFill.color = hpFill > 0.5f ? colorHealthy : colorDanger;
        }
        if (healthText != null)
            healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

        // ── Ability Power ──────────────────────────────────────────────────────
        float apFill = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
        if (abilityBarFill != null)
            abilityBarFill.fillAmount = apFill;
        if (abilityText != null)
            abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

        // ── Attack Damage ──────────────────────────────────────────────────────
        float adFill = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
        if (damageBarFill != null)
            damageBarFill.fillAmount = adFill;
        if (damageText != null)
            damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();

        // Debug log — remove once confirmed working
        Debug.Log($"[SoldierHUD] RefreshHUD → " +
                  $"HP:{stats.MaxHealth} fill:{hpFill:F2} | " +
                  $"AP:{stats.AbilityPower} fill:{apFill:F2} | " +
                  $"AD:{stats.AttackDamage} fill:{adFill:F2}");
    }

    private void HandleDeath(SoldierStats stats)
    {
        gameObject.SetActive(false);
    }

    // ─── Editor Test ──────────────────────────────────────────────────────────

    [ContextMenu("Force Refresh HUD")]
    private void EditorForceRefresh()
    {
        // AFTER:
        if (_stats == null) _stats = FindFirstObjectByType<SoldierStats>();
        if (_stats != null) RefreshHUD(_stats);
        else Debug.LogWarning("[SoldierHUD] No SoldierStats found to refresh.");
    }
}