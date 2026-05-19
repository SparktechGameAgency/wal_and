using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Attach this to any enemy GameObject (world-space or UI).
///
/// ── Static registry ────────────────────────────────────────────────────────
/// All live EnemyUnit instances register themselves in <see cref="All"/>.
/// CannonAutoShooter queries this list every frame to find targets — no scene
/// searches needed at runtime.
///
/// ── Movement ───────────────────────────────────────────────────────────────
/// Set <see cref="moveDirection"/> and <see cref="moveSpeed"/> in the Inspector
/// (or call them from a spawner) to make the enemy walk across the screen.
/// Movement is applied in world space with Transform.Translate, so it works for
/// both world-space enemies and Screen-Space Overlay UI enemies.
///
/// ── Health ─────────────────────────────────────────────────────────────────
/// Call <see cref="TakeDamage(float)"/> from ProjectileBlast.
/// At 0 HP the enemy fires <see cref="OnDied"/> and destroys itself.
///
/// ── Inspector wiring ───────────────────────────────────────────────────────
///   maxHealth      → starting hit-points (default 100)
///   moveSpeed      → world units per second (default 80)
///   moveDirection  → normalised direction vector (default left: -1, 0, 0)
///   healthBarFill  → optional UI Image (filled type) that tracks current HP
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    // ── Static registry ───────────────────────────────────────────
    /// <summary>All currently living EnemyUnit instances.</summary>
    public static readonly List<EnemyUnit> All = new List<EnemyUnit>();

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Stats")]
    public float maxHealth = 100f;

    [Header("Movement")]
    [Tooltip("World units per second.")]
    public float moveSpeed = 80f;

    [Tooltip("Direction the enemy walks. Normalised automatically on Awake.")]
    public Vector3 moveDirection = Vector3.left;

    [Header("Visuals (optional)")]
    [Tooltip("UI Image with fill type set to Filled — tracks HP.")]
    public Image healthBarFill;

    // ── Events ────────────────────────────────────────────────────
    /// <summary>Fired just before the enemy destroys itself.</summary>
    public event System.Action<EnemyUnit> OnDied;

    // ── State ─────────────────────────────────────────────────────
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        CurrentHealth = maxHealth;
        moveDirection = moveDirection.normalized;

        // Normalise in case the designer set a non-unit vector
        if (moveDirection == Vector3.zero)
            moveDirection = Vector3.left;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        if (IsDead) return;

        // Walk in world space — works for both world objects and UI canvas elements
        transform.Translate(moveDirection * (moveSpeed * Time.deltaTime), Space.World);
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Deal <paramref name="amount"/> damage to this enemy.
    /// Called by ProjectileBlast after the projectile arrives.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);

        RefreshHealthBar();

        Debug.Log($"[EnemyUnit] '{name}' took {amount} damage. HP = {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
            Die();
    }

    // ── Private ───────────────────────────────────────────────────

    private void RefreshHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = CurrentHealth / maxHealth;
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Debug.Log($"[EnemyUnit] '{name}' died.");
        OnDied?.Invoke(this);

        // TODO: play death VFX / award currency here before Destroy
        Destroy(gameObject);
    }
}