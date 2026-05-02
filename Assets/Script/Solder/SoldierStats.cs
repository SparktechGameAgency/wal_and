using UnityEngine;

/// <summary>
/// AREA FORGE - SoldierStats
/// Holds and manages a soldier's core stats: Health, Ability Power, Damage.
/// This is the single source of truth for a soldier's data.
/// MULTIPLAYER NOTE: In multiplayer, sync these values with [SyncVar] (Mirror)
/// or PhotonView.RPC so all clients see the same stats.
/// </summary>
public class SoldierStats : MonoBehaviour
{
    // ─── Stat Definitions ─────────────────────────────────────────────────────

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float abilityPower = 10f;
    [SerializeField] private float attackDamage = 15f;

    // ─── Runtime Values ───────────────────────────────────────────────────────

    public float MaxHealth => maxHealth;
    public float AbilityPower => abilityPower;
    public float AttackDamage => attackDamage;

    public float CurrentHealth { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired whenever any stat changes (HUD listens to this).</summary>
    public event System.Action<SoldierStats> OnStatsChanged;

    /// <summary>Fired when the soldier dies.</summary>
    public event System.Action<SoldierStats> OnSoldierDied;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // ─── Public Stat API ─────────────────────────────────────────────────────

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
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnStatsChanged?.Invoke(this);
    }

    /// <summary>
    /// Upgrade a stat when new equipment is equipped.
    /// MULTIPLAYER NOTE: Call this server-side then sync new values.
    /// </summary>
    public void ApplyEquipmentBonus(float healthBonus, float abilityBonus, float damageBonus)
    {
        maxHealth += healthBonus;
        abilityPower += abilityBonus;
        attackDamage += damageBonus;
        CurrentHealth = Mathf.Min(CurrentHealth + healthBonus, maxHealth);

        OnStatsChanged?.Invoke(this);

        Debug.Log($"[SoldierStats] Equipment bonus applied — HP:{maxHealth} AP:{abilityPower} AD:{attackDamage}");
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    public float HealthPercent => (maxHealth > 0) ? CurrentHealth / maxHealth : 0f;

    public override string ToString()
        => $"HP:{CurrentHealth}/{maxHealth} | AP:{abilityPower} | AD:{attackDamage}";
}