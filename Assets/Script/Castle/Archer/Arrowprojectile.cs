//////using System.Collections;
//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// ArrowProjectile
/////////
///////// Moves a UI arrow along a shallow parabolic arc from spawn point to target.
///////// Mirrors the structure of ProjectileArc (used by the cannon) but is tuned
///////// for arrows — lower arc, faster flight, no blast explosion.
/////////
///////// ── Launch overloads ──────────────────────────────────────────────────────
/////////
/////////   1. Launch(start, end, arc, duration, enemy, damage)
/////////      → Used by ArcherUnit. Flies to the enemy and calls TakeDamage on arrival.
/////////
/////////   2. Launch(start, end)
/////////      → Minimal overload using fields set in the Inspector / on the prefab.
/////////
///////// ── Arc behaviour ─────────────────────────────────────────────────────────
/////////   The arc is a quadratic Bézier curve:
/////////
/////////     P(t) = (1-t)² · start  +  2(1-t)t · mid  +  t² · end
/////////
/////////   where mid is the midpoint lifted by arcHeight.
/////////
///////// ── Inspector wiring ──────────────────────────────────────────────────────
/////////   arcHeight      → Peak height above the straight line in pixels (default 10).
/////////   flightDuration → Seconds to reach the target (default 0.6).
/////////
///////// ── Dependencies ──────────────────────────────────────────────────────────
/////////   EnemyUnit  (Script/Cannon/Shootring/EnemyUnit.cs)
///////// </summary>
//////public class ArrowProjectile : MonoBehaviour
//////{
//////    // ── Inspector / serialised fields ─────────────────────────────

//////    [Tooltip("Peak height above the straight line (pixels). Low values = flat trajectory.")]
//////    public float arcHeight = 10f;

//////    [Tooltip("Seconds for the arrow to reach its target.")]
//////    public float flightDuration = 0.6f;

//////    // ── Internal state ────────────────────────────────────────────

//////    private RectTransform _rt;
//////    private EnemyUnit _targetEnemy;
//////    private float _damage;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _rt = GetComponent<RectTransform>();
//////    }

//////    // ── Launch overloads ──────────────────────────────────────────

//////    /// <summary>
//////    /// Full launch used by <see cref="ArcherUnit"/>.
//////    /// Flies toward the enemy and calls <see cref="EnemyUnit.TakeDamage"/> on arrival.
//////    /// </summary>
//////    public void Launch(Vector3 start, Vector3 end,
//////                       float arc, float duration,
//////                       EnemyUnit targetEnemy, float damage)
//////    {
//////        arcHeight = arc;
//////        flightDuration = duration;
//////        _targetEnemy = targetEnemy;
//////        _damage = damage;

//////        StartCoroutine(FlyRoutine(start, end));
//////    }

//////    /// <summary>
//////    /// Minimal overload — uses <see cref="arcHeight"/> and
//////    /// <see cref="flightDuration"/> set on the prefab / Inspector.
//////    /// No damage applied (use for VFX / testing).
//////    /// </summary>
//////    public void Launch(Vector3 start, Vector3 end)
//////    {
//////        _targetEnemy = null;
//////        _damage = 0f;
//////        StartCoroutine(FlyRoutine(start, end));
//////    }

//////    // ── Flight coroutine ──────────────────────────────────────────

//////    private IEnumerator FlyRoutine(Vector3 start, Vector3 end)
//////    {
//////        // Bézier control point: midpoint lifted by arcHeight
//////        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;

//////        float elapsed = 0f;
//////        Vector3 prevPos = start;

//////        while (elapsed < flightDuration)
//////        {
//////            elapsed += Time.deltaTime;
//////            float t = Mathf.Clamp01(elapsed / flightDuration);

//////            // Quadratic Bézier
//////            float u = 1f - t;
//////            Vector3 pos = (u * u) * start
//////                        + (2f * u * t) * mid
//////                        + (t * t) * end;

//////            // Rotate the arrow sprite to face the direction of travel
//////            Vector3 dir = (pos - prevPos);
//////            if (dir.sqrMagnitude > 0.0001f)
//////            {
//////                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//////                transform.rotation = Quaternion.Euler(0f, 0f, angle);
//////            }

//////            if (_rt != null)
//////                _rt.position = pos;
//////            else
//////                transform.position = pos;

//////            prevPos = pos;
//////            yield return null;
//////        }

//////        // ── Arrival ───────────────────────────────────────────────

//////        // Snap to exact target position
//////        if (_rt != null)
//////            _rt.position = end;
//////        else
//////            transform.position = end;

//////        // Deal damage
//////        if (_targetEnemy != null && !_targetEnemy.IsDead)
//////            _targetEnemy.TakeDamage(_damage);

//////        // Destroy the arrow
//////        Destroy(gameObject);
//////    }

//////    // ── Tracking: update end-point each frame so arrow leads the enemy ───────

//////    /// <summary>
//////    /// Optional — call this each frame from ArcherUnit to make the arrow
//////    /// track a moving target. Not mandatory; the simple arc also works.
//////    /// The coroutine re-reads <see cref="_targetEnemy"/> only at arrival,
//////    /// so the damage still lands on the right unit even without tracking.
//////    /// </summary>
//////    public void UpdateTargetPosition(Vector3 newEnd)
//////    {
//////        // The FlyRoutine already captures 'end' by value so tracking is not
//////        // built-in. For homing arrows, stop the current coroutine and restart
//////        // with the updated target position. For simplicity this is omitted here
//////        // — arrows fly to the position where the enemy was when the arrow spawned.
//////        // Override this method in a subclass if you want homing behaviour.
//////    }
//////}

////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// ArrowProjectile
///////
/////// Moves a UI arrow along a shallow parabolic arc from spawn point to target.
/////// Mirrors the structure of ProjectileArc (used by the cannon) but is tuned
/////// for arrows — lower arc, faster flight, no blast explosion.
///////
/////// ── Launch overloads ──────────────────────────────────────────────────────
///////
///////   1. Launch(start, end, arc, duration, enemy, damage)
///////      → Used by ArcherUnit. Flies to the enemy and calls TakeDamage on arrival.
///////
///////   2. Launch(start, end)
///////      → Minimal overload using fields set in the Inspector / on the prefab.
///////
/////// ── Arc behaviour ─────────────────────────────────────────────────────────
///////   The arc is a quadratic Bézier curve:
///////
///////     P(t) = (1-t)² · start  +  2(1-t)t · mid  +  t² · end
///////
///////   where mid is the midpoint lifted by arcHeight.
///////
/////// ── Inspector wiring ──────────────────────────────────────────────────────
///////   arcHeight      → Peak height above the straight line in pixels (default 10).
///////   flightDuration → Seconds to reach the target (default 0.6).
///////
/////// ── Dependencies ──────────────────────────────────────────────────────────
///////   EnemyUnit  (Script/Cannon/Shootring/EnemyUnit.cs)
/////// </summary>
////public class ArrowProjectile : MonoBehaviour
////{
////    // ── Inspector / serialised fields ─────────────────────────────

////    [Tooltip("Peak height above the straight line (pixels). Low values = flat trajectory.")]
////    public float arcHeight = 10f;

////    [Tooltip("Seconds for the arrow to reach its target.")]
////    public float flightDuration = 0.6f;

////    // ── Internal state ────────────────────────────────────────────

////    private RectTransform _rt;
////    private EnemyUnit _targetEnemy;
////    private float _damage;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _rt = GetComponent<RectTransform>();
////    }

////    // ── Launch overloads ──────────────────────────────────────────

////    /// <summary>
////    /// Full launch used by <see cref="ArcherUnit"/>.
////    /// Flies toward the enemy and calls <see cref="EnemyUnit.TakeDamage"/> on arrival.
////    /// </summary>
////    public void Launch(Vector3 start, Vector3 end,
////                       float arc, float duration,
////                       EnemyUnit targetEnemy, float damage)
////    {
////        arcHeight = arc;
////        flightDuration = duration;
////        _targetEnemy = targetEnemy;
////        _damage = damage;

////        StartCoroutine(FlyRoutine(start, end));
////    }

////    /// <summary>
////    /// Minimal overload — uses <see cref="arcHeight"/> and
////    /// <see cref="flightDuration"/> set on the prefab / Inspector.
////    /// No damage applied (use for VFX / testing).
////    /// </summary>
////    public void Launch(Vector3 start, Vector3 end)
////    {
////        _targetEnemy = null;
////        _damage = 0f;
////        StartCoroutine(FlyRoutine(start, end));
////    }

////    // ── Flight coroutine ──────────────────────────────────────────

////    private IEnumerator FlyRoutine(Vector3 start, Vector3 end)
////    {
////        // Bézier control point: midpoint lifted by arcHeight
////        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;

////        float elapsed = 0f;
////        Vector3 prevPos = start;

////        while (elapsed < flightDuration)
////        {
////            elapsed += Time.deltaTime;
////            float t = Mathf.Clamp01(elapsed / flightDuration);

////            // Quadratic Bézier
////            float u = 1f - t;
////            Vector3 pos = (u * u) * start
////                        + (2f * u * t) * mid
////                        + (t * t) * end;

////            // Rotate the arrow sprite to face the direction of travel.
////            // Atan2 assumes the sprite's forward is the +X axis (facing right).
////            // Our arrow sprite faces +Y (upward), so we subtract 90 degrees
////            // to align the sprite tip with the direction of travel.
////            Vector3 dir = (pos - prevPos);
////            if (dir.sqrMagnitude > 0.0001f)
////            {
////                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
////                transform.rotation = Quaternion.Euler(0f, 0f, angle);
////            }

////            if (_rt != null)
////                _rt.position = pos;
////            else
////                transform.position = pos;

////            prevPos = pos;
////            yield return null;
////        }

////        // ── Arrival ───────────────────────────────────────────────

////        // Snap to exact target position
////        if (_rt != null)
////            _rt.position = end;
////        else
////            transform.position = end;

////        // Deal damage
////        if (_targetEnemy != null && !_targetEnemy.IsDead)
////            _targetEnemy.TakeDamage(_damage);

////        // Destroy the arrow
////        Destroy(gameObject);
////    }

////    // ── Tracking: update end-point each frame so arrow leads the enemy ───────

////    /// <summary>
////    /// Optional — call this each frame from ArcherUnit to make the arrow
////    /// track a moving target. Not mandatory; the simple arc also works.
////    /// The coroutine re-reads <see cref="_targetEnemy"/> only at arrival,
////    /// so the damage still lands on the right unit even without tracking.
////    /// </summary>
////    public void UpdateTargetPosition(Vector3 newEnd)
////    {
////        // The FlyRoutine already captures 'end' by value so tracking is not
////        // built-in. For homing arrows, stop the current coroutine and restart
////        // with the updated target position. For simplicity this is omitted here
////        // — arrows fly to the position where the enemy was when the arrow spawned.
////        // Override this method in a subclass if you want homing behaviour.
////    }
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// ArrowProjectile
/////
///// Moves a UI arrow along a shallow parabolic arc from spawn point to target.
///// Mirrors the structure of ProjectileArc (used by the cannon) but is tuned
///// for arrows — lower arc, faster flight, no blast explosion.
/////
///// ── Launch overloads ──────────────────────────────────────────────────────
/////
/////   1. Launch(start, end, arc, duration, enemy, damage)
/////      → Used by ArcherUnit. Flies to the enemy and calls TakeDamage on arrival.
/////
/////   2. Launch(start, end)
/////      → Minimal overload using fields set in the Inspector / on the prefab.
/////
///// ── Arc behaviour ─────────────────────────────────────────────────────────
/////   The arc is a quadratic Bézier curve:
/////
/////     P(t) = (1-t)² · start  +  2(1-t)t · mid  +  t² · end
/////
/////   where mid is the midpoint lifted by arcHeight.
/////
///// ── Inspector wiring ──────────────────────────────────────────────────────
/////   arcHeight      → Peak height above the straight line in pixels (default 10).
/////   flightDuration → Seconds to reach the target (default 0.6).
/////
///// ── Dependencies ──────────────────────────────────────────────────────────
/////   EnemyUnit  (Script/Cannon/Shootring/EnemyUnit.cs)
///// </summary>
//public class ArrowProjectile : MonoBehaviour
//{
//    // ── Inspector / serialised fields ─────────────────────────────

//    [Tooltip("Peak height above the straight line (pixels). Low values = flat trajectory.")]
//    public float arcHeight = 10f;

//    [Tooltip("Seconds for the arrow to reach its target.")]
//    public float flightDuration = 0.6f;

//    // ── Internal state ────────────────────────────────────────────

//    private RectTransform _rt;
//    private EnemyUnit _targetEnemy;
//    private float _damage;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _rt = GetComponent<RectTransform>();
//    }

//    // ── Launch overloads ──────────────────────────────────────────

//    /// <summary>
//    /// Full launch used by <see cref="ArcherUnit"/>.
//    /// Flies toward the enemy and calls <see cref="EnemyUnit.TakeDamage"/> on arrival.
//    /// </summary>
//    public void Launch(Vector3 start, Vector3 end,
//                       float arc, float duration,
//                       EnemyUnit targetEnemy, float damage)
//    {
//        arcHeight = arc;
//        flightDuration = duration;
//        _targetEnemy = targetEnemy;
//        _damage = damage;

//        // Centre the pivot and anchors so the arrow rotates around its middle.
//        // This must happen before the first rotation is applied.
//        if (_rt != null)
//        {
//            _rt.anchorMin = new Vector2(0.5f, 0.5f);
//            _rt.anchorMax = new Vector2(0.5f, 0.5f);
//            _rt.pivot = new Vector2(0.5f, 0.5f);
//        }

//        StartCoroutine(FlyRoutine(start, end));
//    }

//    /// <summary>
//    /// Minimal overload — uses <see cref="arcHeight"/> and
//    /// <see cref="flightDuration"/> set on the prefab / Inspector.
//    /// No damage applied (use for VFX / testing).
//    /// </summary>
//    public void Launch(Vector3 start, Vector3 end)
//    {
//        _targetEnemy = null;
//        _damage = 0f;
//        StartCoroutine(FlyRoutine(start, end));
//    }

//    // ── Flight coroutine ──────────────────────────────────────────

//    private IEnumerator FlyRoutine(Vector3 start, Vector3 end)
//    {
//        // Bézier control point: midpoint lifted by arcHeight
//        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;

//        float elapsed = 0f;
//        Vector3 prevPos = start;

//        while (elapsed < flightDuration)
//        {
//            elapsed += Time.deltaTime;
//            float t = Mathf.Clamp01(elapsed / flightDuration);

//            // Quadratic Bézier
//            float u = 1f - t;
//            Vector3 pos = (u * u) * start
//                        + (2f * u * t) * mid
//                        + (t * t) * end;

//            // Rotate the arrow sprite to face the direction of travel.
//            // Atan2 assumes the sprite's forward is the +X axis (facing right).
//            // Our arrow sprite faces +Y (upward), so we subtract 90 degrees
//            // to align the sprite tip with the direction of travel.
//            Vector3 dir = (pos - prevPos);
//            if (dir.sqrMagnitude > 0.0001f)
//            {
//                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
//                transform.rotation = Quaternion.Euler(0f, 0f, angle);
//            }

//            if (_rt != null)
//                _rt.position = pos;
//            else
//                transform.position = pos;

//            prevPos = pos;
//            yield return null;
//        }

//        // ── Arrival ───────────────────────────────────────────────

//        // Snap to exact target position
//        if (_rt != null)
//            _rt.position = end;
//        else
//            transform.position = end;

//        // Deal damage
//        if (_targetEnemy != null && !_targetEnemy.IsDead)
//            _targetEnemy.TakeDamage(_damage);

//        // Destroy the arrow
//        Destroy(gameObject);
//    }

//    // ── Tracking: update end-point each frame so arrow leads the enemy ───────

//    /// <summary>
//    /// Optional — call this each frame from ArcherUnit to make the arrow
//    /// track a moving target. Not mandatory; the simple arc also works.
//    /// The coroutine re-reads <see cref="_targetEnemy"/> only at arrival,
//    /// so the damage still lands on the right unit even without tracking.
//    /// </summary>
//    public void UpdateTargetPosition(Vector3 newEnd)
//    {
//        // The FlyRoutine already captures 'end' by value so tracking is not
//        // built-in. For homing arrows, stop the current coroutine and restart
//        // with the updated target position. For simplicity this is omitted here
//        // — arrows fly to the position where the enemy was when the arrow spawned.
//        // Override this method in a subclass if you want homing behaviour.
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArrowProjectile — attach to the ArrowPrefab.
///
/// Flies along a quadratic Bezier arc from spawn point to target.
/// Rotates to face the direction of travel each frame.
///
/// SPRITE ORIENTATION:
///   spriteRotationOffset = 0   if your sprite faces RIGHT  (+X)
///   spriteRotationOffset = -90 if your sprite faces UP     (+Y)  <-- default
///   spriteRotationOffset = 90  if your sprite faces DOWN   (-Y)
///   spriteRotationOffset = 180 if your sprite faces LEFT   (-X)
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    [Tooltip("Peak height above the straight line (pixels).")]
    public float arcHeight = 10f;

    [Tooltip("Seconds for the arrow to reach its target.")]
    public float flightDuration = 0.6f;

    [Tooltip("Rotation offset to align the sprite tip with travel direction.\n" +
             "-90 if sprite faces UP (default). 0 if sprite faces RIGHT.")]
    public float spriteRotationOffset = -90f;

    // ── Internal ──────────────────────────────────────────────────

    private RectTransform _rt;
    private EnemyUnit _targetEnemy;
    private float _damage;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    // ── Launch ────────────────────────────────────────────────────

    public void Launch(Vector3 start, Vector3 end,
                       float arc, float duration,
                       EnemyUnit targetEnemy, float damage)
    {
        arcHeight = arc;
        flightDuration = duration;
        _targetEnemy = targetEnemy;
        _damage = damage;

        // Fix anchors/pivot so sizeDelta works regardless of prefab settings,
        // and rotation spins around the arrow's centre.
        if (_rt != null)
        {
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.localScale = Vector3.one;
        }

        StartCoroutine(FlyRoutine(start, end));
    }

    public void Launch(Vector3 start, Vector3 end)
    {
        _targetEnemy = null;
        _damage = 0f;

        if (_rt != null)
        {
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.localScale = Vector3.one;
        }

        StartCoroutine(FlyRoutine(start, end));
    }

    // ── Flight ────────────────────────────────────────────────────

    private IEnumerator FlyRoutine(Vector3 start, Vector3 end)
    {
        // Bezier control point: midpoint raised by arcHeight
        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;
        float elapsed = 0f;
        Vector3 prevPos = start;

        // Set initial position before first yield so arrow doesn't flash at origin
        if (_rt != null) _rt.position = start;
        else transform.position = start;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            float u = 1f - t;

            Vector3 pos = (u * u) * start
                        + (2f * u * t) * mid
                        + (t * t) * end;

            // Rotate to face direction of travel
            Vector3 dir = pos - prevPos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (_rt != null) _rt.position = pos;
            else transform.position = pos;

            prevPos = pos;
            yield return null;
        }

        // Snap to target
        if (_rt != null) _rt.position = end;
        else transform.position = end;

        // Deal damage
        if (_targetEnemy != null && !_targetEnemy.IsDead)
            _targetEnemy.TakeDamage(_damage);

        Destroy(gameObject);
    }
}