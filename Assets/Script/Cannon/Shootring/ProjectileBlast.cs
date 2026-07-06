////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;

////public class ProjectileBlast : MonoBehaviour
////{
////    [Header("Blast Animation")]
////    public SpriteAnimator blastAnimator;

////    public void Explode()
////    {
////        StartCoroutine(BlastSequence());
////    }

////    IEnumerator BlastSequence()
////    {
////        // Hide projectile image
////        Image projImage = GetComponent<Image>();
////        if (projImage != null)
////            projImage.enabled = false;

////        if (blastAnimator != null)
////        {
////            blastAnimator.gameObject.SetActive(true);
////            blastAnimator.Play();

////            yield return new WaitForSeconds(blastAnimator.GetDuration());
////        }
////        else
////        {
////            yield return null;
////        }

////        Destroy(gameObject);
////    }
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// Plays the explosion sprite animation when the projectile arrives at its target.
///// After the animation finishes, damage is applied to the locked <see cref="EnemyUnit"/>.
/////
///// Changes from the original version
///// ───────────────────────────────────────────────────────────────────────────
///// • <see cref="Explode(EnemyUnit, float)"/> replaces the zero-argument version.
/////   The enemy reference and damage amount are received from <see cref="ProjectileArc"/>.
///// • A zero-argument <see cref="Explode()"/> overload is kept for backward compatibility.
/////
///// ── Prefab hierarchy ───────────────────────────────────────────────────────
/////   ProjectilePrefab              ← ProjectileArc + ProjectileBlast + Image (projectile sprite)
/////   └── BlastEffect               ← SpriteAnimator (frames = explosion sprites)
/////                                    starts inactive; activated by Explode()
///// </summary>
//public class ProjectileBlast : MonoBehaviour
//{
//    [Header("Blast Animation")]
//    [Tooltip("SpriteAnimator on the blast effect child GameObject.")]
//    public SpriteAnimator blastAnimator;

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>
//    /// Called by <see cref="ProjectileArc"/> when the projectile reaches its destination.
//    /// Plays the blast animation, then deals <paramref name="damage"/> to the enemy.
//    /// </summary>
//    public void Explode(EnemyUnit target, float damage)
//    {
//        StartCoroutine(BlastSequence(target, damage));
//    }

//    /// <summary>
//    /// Backward-compatible overload — no damage applied.
//    /// Can be called manually (e.g. for test fire buttons).
//    /// </summary>
//    public void Explode()
//    {
//        StartCoroutine(BlastSequence(null, 0f));
//    }

//    // ── Coroutine ─────────────────────────────────────────────────

//    private IEnumerator BlastSequence(EnemyUnit target, float damage)
//    {
//        // ── 1. Hide the flying projectile sprite ─────────────────
//        Image projImage = GetComponent<Image>();
//        if (projImage != null) projImage.enabled = false;

//        // ── 2. Play explosion animation ──────────────────────────
//        if (blastAnimator != null)
//        {
//            blastAnimator.gameObject.SetActive(true);
//            blastAnimator.Play();

//            // Wait for the animation to fully finish before applying damage
//            // so the visual and the damage feel simultaneous.
//            yield return new WaitForSeconds(blastAnimator.GetDuration());
//        }
//        else
//        {
//            // No animator assigned — apply damage without a visual delay
//            Debug.LogWarning("[ProjectileBlast] blastAnimator not assigned. " +
//                             "Dealing damage immediately.");
//            yield return null;
//        }

//        // ── 3. Deal damage ───────────────────────────────────────
//        if (target != null && !target.IsDead && damage > 0f)
//        {
//            Debug.Log($"[ProjectileBlast] Dealing {damage} damage to '{target.name}'.");
//            target.TakeDamage(damage);
//        }

//        // ── 4. Clean up ──────────────────────────────────────────
//        Destroy(gameObject);
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the explosion sprite animation when the projectile arrives at its target.
/// After the animation finishes, damage is applied to the locked <see cref="EnemyUnit"/>.
///
/// ── FIX: blast child is forced inactive on Awake ──────────────────────────
/// Previously, if the BlastEffect child was accidentally left active in the prefab,
/// SpriteAnimator.Awake() would run immediately and overwrite the Image sprite
/// with frames[0] of the blast animation — hiding the cannonball.
/// Awake() now unconditionally deactivates the blast child so the cannonball
/// Image is always visible during flight.
///
/// ── Prefab hierarchy ───────────────────────────────────────────────────────
///   ProjectilePrefab              ← ProjectileArc + ProjectileBlast + Image (cannonball sprite)
///   └── BlastEffect               ← SpriteAnimator (frames = explosion sprites)
///                                    starts inactive; activated by Explode()
/// </summary>
public class ProjectileBlast : MonoBehaviour
{
    [Header("Blast Animation")]
    [Tooltip("SpriteAnimator on the blast effect child GameObject.")]
    public SpriteAnimator blastAnimator;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        // Always hide the blast child at spawn so the cannonball Image shows during flight.
        // This is safe even if the prefab has BlastEffect accidentally left active.
        if (blastAnimator != null)
            blastAnimator.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="ProjectileArc"/> when the projectile reaches its destination.
    /// Plays the blast animation, then deals <paramref name="damage"/> to the target.
    ///
    /// Was typed to EnemyUnit — now IDamageable so the exact same cannon
    /// projectile works whether it's fired at a Village EnemyUnit or (once
    /// this cannon is carried into the Battle scene) a BattleUnit. See
    /// IDamageable.cs / CannonAutoShooter for the full picture.
    /// </summary>
    public void Explode(IDamageable target, float damage)
    {
        StartCoroutine(BlastSequence(target, damage));
    }

    /// <summary>
    /// Backward-compatible overload — no damage applied.
    /// Can be called manually (e.g. for test fire buttons).
    /// </summary>
    public void Explode()
    {
        StartCoroutine(BlastSequence(null, 0f));
    }

    // ── Coroutine ─────────────────────────────────────────────────

    private IEnumerator BlastSequence(IDamageable target, float damage)
    {
        // ── 1. Hide the flying projectile sprite ─────────────────
        Image projImage = GetComponent<Image>();
        if (projImage != null) projImage.enabled = false;

        // ── 2. Play explosion animation ──────────────────────────
        if (blastAnimator != null)
        {
            blastAnimator.gameObject.SetActive(true);
            blastAnimator.Play();

            // Wait for the animation to fully finish before applying damage
            // so the visual and the damage feel simultaneous.
            yield return new WaitForSeconds(blastAnimator.GetDuration());
        }
        else
        {
            // No animator assigned — apply damage without a visual delay
            Debug.LogWarning("[ProjectileBlast] blastAnimator not assigned. " +
                             "Dealing damage immediately.");
            yield return null;
        }

        // ── 3. Deal damage ───────────────────────────────────────
        if (target != null && !target.IsDead && damage > 0f)
        {
            Debug.Log($"[ProjectileBlast] Dealing {damage} damage to '{target.DamageableTransform.name}'.");
            target.TakeDamage(damage);
        }

        // ── 4. Clean up ──────────────────────────────────────────
        Destroy(gameObject);
    }
}