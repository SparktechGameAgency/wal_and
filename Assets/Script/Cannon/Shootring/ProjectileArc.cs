using System.Collections;
using UnityEngine;

/// <summary>
/// Moves a UI projectile along a parabolic arc from start to end.
///
/// Provides three Launch() overloads so both old and new callers compile:
///
///   1. Launch(start, end, arc, duration, enemy, damage)
///      → Used by CannonAutoShooter. Flies to enemy and deals damage on arrival.
///
///   2. Launch(start, end, arc, duration)
///      → Used by the original CannonControllerShot (manual fire button).
///        No damage applied — kept for backward compatibility.
///
///   3. Launch(start, end)
///      → Minimal overload. Uses the arcHeight / flightDuration fields on this
///        component. Handy for quick tests from other scripts.
///
/// ── arcHeight is now a real serialized field ───────────────────────────────
/// Previously arcHeight only existed as a Launch() parameter, so editing it on
/// the ProjectileArc component had no effect. Now the field is serialized and
/// Launch() writes the incoming value back into it — the Inspector always shows
/// the value actually used.
/// </summary>
public class ProjectileArc : MonoBehaviour
{
    // Arc height is a const — NOT a serialized field.
    // A public/[SerializeField] float keeps the OLD baked value from the prefab
    // asset and ignores code defaults, which is why the arc stayed huge even after
    // changing the value in code. This const is the single source of truth.
    // To change the arc, edit ARC_HEIGHT here and save — no Inspector tweak needed.
    public const float ARC_HEIGHT = 0.5f;

    [Tooltip("Seconds the projectile takes to reach the target.")]
    public float flightDuration = 1.2f;

    // ── Internal state set by whichever Launch() overload is called ──
    private RectTransform _rt;
    // Was "EnemyUnit _targetEnemy" — now IDamageable so this same component
    // works whether it's fired in the Village (target = EnemyUnit) or
    // carried into the Battle scene (target = BattleUnit). See IDamageable.cs.
    private IDamageable _targetEnemy;
    private float _damage;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    // ── Overload 1: CannonAutoShooter (enemy + damage) ────────────

    /// <summary>
    /// Full launch used by <see cref="CannonAutoShooter"/>.
    /// Flies to the enemy and triggers <see cref="ProjectileBlast"/> on arrival,
    /// which plays the explosion and calls <see cref="EnemyUnit.TakeDamage"/>.
    /// </summary>
    public void Launch(Vector3 start, Vector3 end,
                       float arc, float duration,
                       IDamageable targetEnemy, float damage)
    {
        // arc parameter intentionally ignored — ARC_HEIGHT const is used instead.
        flightDuration = duration;
        _targetEnemy = targetEnemy;
        _damage = damage;
        StartCoroutine(MoveInArc(start, end));
    }

    // ── Overload 2: original CannonControllerShot (no enemy/damage) ──

    /// <summary>
    /// Legacy overload — keeps the original <see cref="CannonControllerShot"/>
    /// compiling without changes. No damage is applied on arrival.
    /// </summary>
    public void Launch(Vector3 start, Vector3 end, float arc, float duration)
    {
        // arc parameter intentionally ignored — ARC_HEIGHT const is used instead.
        flightDuration = duration;
        _targetEnemy = null;
        _damage = 0f;
        StartCoroutine(MoveInArc(start, end));
    }

    // ── Overload 3: minimal (uses component field values) ─────────

    /// <summary>
    /// Minimal overload — uses the <see cref="arcHeight"/> and
    /// <see cref="flightDuration"/> values already set on this component.
    /// </summary>
    public void Launch(Vector3 start, Vector3 end)
    {
        _targetEnemy = null;
        _damage = 0f;
        StartCoroutine(MoveInArc(start, end));
    }

    // ── Arc coroutine ─────────────────────────────────────────────

    private IEnumerator MoveInArc(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // Straight-line lerp + parabolic vertical offset.
            // Peak at t=0.5: 4 * 0.5 * 0.5 = 1.0 → exactly ARC_HEIGHT units above midpoint.
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += ARC_HEIGHT * 4f * t * (1f - t);

            if (_rt != null) _rt.position = pos;

            yield return null;
        }

        // Snap exactly to destination
        if (_rt != null) _rt.position = end;

        // Hand off to ProjectileBlast — plays explosion, deals damage, destroys GO
        ProjectileBlast blast = GetComponent<ProjectileBlast>();
        if (blast != null)
            blast.Explode(_targetEnemy, _damage);
        else
            Fallback();
    }

    // ── Fallback if no ProjectileBlast component ──────────────────

    private void Fallback()
    {
        if (_targetEnemy != null && !_targetEnemy.IsDead)
            _targetEnemy.TakeDamage(_damage);
        Destroy(gameObject);
    }
}