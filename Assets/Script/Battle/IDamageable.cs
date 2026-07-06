using UnityEngine;

/// <summary>
/// IDamageable
///
/// Shared contract for anything a projectile (cannonball / arrow / dragon
/// fire-breath) can be launched at and deal damage to.
///
/// WHY THIS EXISTS
/// ────────────────────────────────────────────────────────────────────────
/// ProjectileArc / ProjectileBlast / ArrowProjectile were originally typed
/// directly to EnemyUnit — fine in the Village, where EnemyUnit is the only
/// kind of target that exists. But the SAME cannon/archer GameObjects get
/// carried over into the Battle scene (via BattleStarter/BattleManager) and
/// re-used there, where the opposing units are BattleUnit instances instead
/// — there are no EnemyUnit objects in the Battle scene at all.
///
/// Both EnemyUnit and BattleUnit already expose the same shape (IsDead +
/// TakeDamage), so implementing this interface on both lets every
/// projectile script work unmodified in either scene — CannonAutoShooter /
/// ArcherUnit just decide WHICH list to search (EnemyUnit.All in the
/// Village, BattleUnit.CurrentTarget in the Battle scene) and hand the
/// result to the exact same Launch()/Explode() calls either way.
/// </summary>
public interface IDamageable
{
    /// <summary>True once this target has died / been destroyed logically.</summary>
    bool IsDead { get; }

    /// <summary>
    /// The Transform to aim at / measure distance to. Every MonoBehaviour
    /// already has .transform, so implementers just return `transform`.
    /// </summary>
    Transform DamageableTransform { get; }

    /// <summary>Applies damage. Implementations should no-op if already dead.</summary>
    void TakeDamage(float amount);
}