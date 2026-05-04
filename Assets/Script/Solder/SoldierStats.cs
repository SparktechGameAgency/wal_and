//using UnityEngine;

///// <summary>
///// AREA FORGE - SoldierStats
///// Holds and manages a soldier's core stats: Health, Ability Power, Damage.
///// This is the single source of truth for a soldier's data.
///// MULTIPLAYER NOTE: In multiplayer, sync these values with [SyncVar] (Mirror)
///// or PhotonView.RPC so all clients see the same stats.
///// </summary>
//public class SoldierStats : MonoBehaviour
//{
//    // ─── Stat Definitions ─────────────────────────────────────────────────────

//    [Header("Base Stats")]
//    [SerializeField] private float maxHealth = 100f;
//    [SerializeField] private float abilityPower = 10f;
//    [SerializeField] private float attackDamage = 15f;

//    // ─── Runtime Values ───────────────────────────────────────────────────────

//    public float MaxHealth => maxHealth;
//    public float AbilityPower => abilityPower;
//    public float AttackDamage => attackDamage;

//    public float CurrentHealth { get; private set; }

//    // ─── Events ───────────────────────────────────────────────────────────────

//    /// <summary>Fired whenever any stat changes (HUD listens to this).</summary>
//    public event System.Action<SoldierStats> OnStatsChanged;

//    /// <summary>Fired when the soldier dies.</summary>
//    public event System.Action<SoldierStats> OnSoldierDied;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        CurrentHealth = maxHealth;
//    }

//    // ─── Public Stat API ─────────────────────────────────────────────────────

//    public void TakeDamage(float amount)
//    {
//        if (amount <= 0) return;
//        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
//        OnStatsChanged?.Invoke(this);

//        if (CurrentHealth <= 0f)
//            OnSoldierDied?.Invoke(this);
//    }

//    public void Heal(float amount)
//    {
//        if (amount <= 0) return;
//        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
//        OnStatsChanged?.Invoke(this);
//    }

//    /// <summary>
//    /// Upgrade a stat when new equipment is equipped.
//    /// MULTIPLAYER NOTE: Call this server-side then sync new values.
//    /// </summary>
//    public void ApplyEquipmentBonus(float healthBonus, float abilityBonus, float damageBonus)
//    {
//        maxHealth += healthBonus;
//        abilityPower += abilityBonus;
//        attackDamage += damageBonus;
//        CurrentHealth = Mathf.Min(CurrentHealth + healthBonus, maxHealth);

//        OnStatsChanged?.Invoke(this);

//        Debug.Log($"[SoldierStats] Equipment bonus applied — HP:{maxHealth} AP:{abilityPower} AD:{attackDamage}");
//    }

//    // ─── Utility ─────────────────────────────────────────────────────────────

//    public float HealthPercent => (maxHealth > 0) ? CurrentHealth / maxHealth : 0f;

//    public override string ToString()
//        => $"HP:{CurrentHealth}/{maxHealth} | AP:{abilityPower} | AD:{attackDamage}";
//}

using UnityEngine;

/// <summary>
/// AREA FORGE - SoldierStats
///
/// Base stats are NEVER modified at runtime.
/// Equipment bonuses live in non-serialized fields — they reset to zero
/// every time you press Play, so the HUD always starts from clean values.
///
/// Displayed value = base stat + equipment bonus.
/// </summary>
public class SoldierStats : MonoBehaviour
{
    // ─── Base Stats (set in Inspector — NEVER changed at runtime) ─────────────

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float abilityPower = 10f;
    [SerializeField] private float attackDamage = 15f;

    // ─── Equipment Bonuses (runtime only — not serialized, resets on Play) ────

    private float _bonusHealth;
    private float _bonusAbility;
    private float _bonusDamage;

    // ─── Public Properties (base + bonus) ────────────────────────────────────

    public float MaxHealth => maxHealth + _bonusHealth;
    public float AbilityPower => abilityPower + _bonusAbility;
    public float AttackDamage => attackDamage + _bonusDamage;

    public float CurrentHealth { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────

    public event System.Action<SoldierStats> OnStatsChanged;
    public event System.Action<SoldierStats> OnSoldierDied;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Bonuses are 0 every Play session (non-serialized)
        _bonusHealth = 0f;
        _bonusAbility = 0f;
        _bonusDamage = 0f;
        CurrentHealth = MaxHealth;
    }

    // ─── Combat API ───────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnStatsChanged?.Invoke(this);
        if (CurrentHealth <= 0f)
            OnSoldierDied?.Invoke(this);
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnStatsChanged?.Invoke(this);
    }

    // ─── Equipment Bonus API ──────────────────────────────────────────────────

    /// <summary>
    /// Called by CharacterEquipment when an item is equipped or unequipped.
    /// Pass positive values to add a bonus, negative to remove it.
    /// Base stats in the Inspector are NEVER touched.
    /// </summary>
    public void ApplyEquipmentBonus(float healthBonus, float abilityBonus, float damageBonus)
    {
        _bonusHealth += healthBonus;
        _bonusAbility += abilityBonus;
        _bonusDamage += damageBonus;

        // Clamp CurrentHealth within the new MaxHealth
        CurrentHealth = Mathf.Clamp(CurrentHealth + healthBonus, 0f, MaxHealth);

        OnStatsChanged?.Invoke(this);

        Debug.Log($"[SoldierStats] Bonus applied — HP:{MaxHealth} AP:{AbilityPower} AD:{AttackDamage}");
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

    public override string ToString()
        => $"HP:{CurrentHealth:F0}/{MaxHealth:F0} | AP:{AbilityPower:F0} | AD:{AttackDamage:F0}";
}