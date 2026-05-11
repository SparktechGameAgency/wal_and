using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FLY ZONE
///
/// Attach to a GameObject that represents an airspace the dragon can be
/// dragged into.  The GameObject must have a Graphic component (e.g. an
/// Image set to alpha 0) so the EventSystem can raycast it.
///
/// ════════════════════════════════════════════════════════════════════
///  HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   FlyZone                 ← FlyZone.cs + Image (alpha 0) here
///   └── (dragon is reparented here at runtime)
///
/// ════════════════════════════════════════════════════════════════════
///  HOW IT WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  DragonController.OnEndDrag() raycasts under the pointer.
///  If it finds a GameObject carrying this component it calls EnterFlying.
///  The dragon is then reparented here and patrols left↔right by
///  ±PatrolHalfWidth units around this RectTransform's centre (0,0).
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR FIELDS
/// ════════════════════════════════════════════════════════════════════
///
///  patrolHalfWidth   Half the patrol width in UI units.
///                    e.g. 250 → dragon moves from x=-250 to x=+250.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FlyZone : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [Tooltip("Half the patrol width in canvas units. " +
             "Dragon moves from –PatrolHalfWidth to +PatrolHalfWidth along X.")]
    [Min(10f)]
    [SerializeField] private float patrolHalfWidth = 250f;

    [Header("Debug Gizmo")]
    [Tooltip("Draw patrol bounds in the Scene view so you can see the range.")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.4f);

    // ── Public read-only ───────────────────────────────────────────────────────
    /// <summary>How far (in canvas units) the dragon patrols either side of centre.</summary>
    public float PatrolHalfWidth => patrolHalfWidth;

    // ══════════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        EnsureRaycastTarget();
    }

    /// Make sure there is a Graphic so the EventSystem can raycast this zone.
    /// If no Image exists, one is added automatically with alpha = 0.
    private void EnsureRaycastTarget()
    {
        if (GetComponent<Graphic>() != null) return;   // already has one

        var img = gameObject.AddComponent<Image>();
        img.color = Color.clear;          // invisible but still raycastable
        img.raycastTarget = true;

        Debug.Log("[FlyZone] Added transparent Image for raycasting.", this);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EDITOR GIZMO
    // ══════════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        // Draw the patrol range as a coloured rectangle in the Scene view
        var rt = transform as RectTransform;
        if (rt == null) return;

        Vector3 center = rt.position;
        float height = rt.rect.height * rt.lossyScale.y;
        float width = patrolHalfWidth * 2f * rt.lossyScale.x;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, new Vector3(width, height, 1f));

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, new Vector3(width, height, 1f));
    }
#endif
}