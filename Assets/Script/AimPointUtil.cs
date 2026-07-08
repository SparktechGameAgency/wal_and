using UnityEngine;

/// <summary>
/// AimPointUtil
///
/// Every projectile source (CannonAutoShooter, ArcherUnit, DragonFireBreath)
/// used to aim at the target's raw `transform.position` — which for a
/// ground-standing unit is the RectTransform's PIVOT point, not its visual
/// center. Soldier/Horse/Archer/Cannon prefabs are anchored with the pivot
/// at the bottom (so they sit correctly on the ground line), so aiming at
/// `.position` put every shot at the enemy's feet instead of its torso —
/// looked like cannonballs/arrows/dragon fire all landing on the "down
/// side" of the target.
///
/// GetBodyCenter() converts the target's own rect height + pivot into a
/// local offset that lands at the vertical MIDDLE of its RectTransform,
/// then converts that local point into world space via TransformPoint —
/// which automatically follows the target's current position, scale, and
/// (if ever added) rotation, so it stays correct as the target walks.
///
/// Safe to call on any Transform: if it isn't a RectTransform (or has no
/// meaningful rect, e.g. a placeholder with zero height), it just falls
/// back to the plain .position, same as the old behaviour.
/// </summary>
public static class AimPointUtil
{
    public static Vector3 GetBodyCenter(Transform target)
    {
        if (target == null) return Vector3.zero;

        RectTransform rt = target as RectTransform;
        if (rt == null) rt = target.GetComponent<RectTransform>();
        if (rt == null || rt.rect.height <= 0f) return target.position;

        // rect.height * (0.5 - pivot.y) is 0 when the pivot IS already the
        // center (nothing changes for prefabs already set up that way), and
        // positive when the pivot is below center (the common bottom-pivot
        // ground-unit case) — pushing the aim point up into the torso.
        float centerLocalY = rt.rect.height * (0.5f - rt.pivot.y);
        return rt.TransformPoint(new Vector3(0f, centerLocalY, 0f));
    }
}