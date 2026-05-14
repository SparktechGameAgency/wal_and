//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// DRAGON RIDER SEAT
/////
///// Attach to a child of the dragon prefab named "RiderSeat".
///// The soldier is reparented here on drop, so it automatically
///// follows and flips with the dragon during patrol.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DRAGON PREFAB HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   Dragon                        ← DragonController
/////   ├── VisualRoot                ← dragon sprites
/////   └── RiderSeat                 ← DragonRiderSeat  ◄ ADD THIS
/////       └── (soldier reparented here at runtime)
/////
///// ════════════════════════════════════════════════════════════════════
/////  HOW IT WORKS
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. SoldierDragDrop.OnEndDrag raycasts under the pointer.
/////  2. If DragonRiderSeat is found and unoccupied, it calls MountSoldier.
/////  3. The soldier is reparented to this transform at seatOffset.
/////  4. Because the soldier is now a child of the dragon, it inherits
/////     all movement and flip automatically — no extra code needed.
/////  5. Dragging the soldier off the dragon calls ReleaseSoldier,
/////     clearing the seat so a new soldier can mount.
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR
///// ════════════════════════════════════════════════════════════════════
/////
/////  seatOffset      Local anchoredPosition inside RiderSeat where the
/////                  soldier is placed. Positive Y moves the soldier up
/////                  (on top of the dragon). Tune in Play mode.
///// </summary>
//[RequireComponent(typeof(RectTransform))]
//public class DragonRiderSeat : MonoBehaviour
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────
//    [Header("Seat Offset")]
//    [Tooltip("Local position inside RiderSeat where the soldier is placed. " +
//             "Increase Y to move the soldier higher (on top of the dragon sprite).")]
//    [SerializeField] private Vector2 seatOffset = new Vector2(0f, 40f);

//    // ── Runtime ───────────────────────────────────────────────────────────────
//    private SoldierDragDrop _mountedSoldier;

//    /// <summary>True when a soldier is currently riding this dragon.</summary>
//    public bool IsOccupied => _mountedSoldier != null;

//    /// <summary>The soldier currently mounted, or null.</summary>
//    public SoldierDragDrop MountedSoldier => _mountedSoldier;

//    // ══════════════════════════════════════════════════════════════════════════
//    // SETUP
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        EnsureRaycastTarget();
//    }

//    /// Adds a transparent Image so the EventSystem can raycast this seat.
//    private void EnsureRaycastTarget()
//    {
//        if (GetComponent<Graphic>() != null) return;

//        var img = gameObject.AddComponent<Image>();
//        img.color = Color.clear;
//        img.raycastTarget = true;

//        Debug.Log("[DragonRiderSeat] Added transparent Image for raycasting.", this);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC API
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by SoldierDragDrop when the soldier is dropped on this seat.
//    /// Reparents the soldier here and positions it at seatOffset.
//    /// Does nothing if the seat is already occupied.
//    /// </summary>
//    public void MountSoldier(SoldierDragDrop soldier)
//    {
//        if (IsOccupied)
//        {
//            Debug.Log("[DragonRiderSeat] Seat already occupied — ignoring drop.");
//            return;
//        }

//        _mountedSoldier = soldier;

//        // Notify the soldier so it can handle reparenting + state changes
//        soldier.MountOnDragon(this, seatOffset);

//        Debug.Log($"[DragonRiderSeat] '{soldier.name}' mounted on '{transform.parent?.name}'.");
//    }

//    /// <summary>
//    /// Called by SoldierDragDrop at the START of a drag, before the soldier
//    /// is lifted off. Clears the seat so another soldier can mount while
//    /// this one is in the air.
//    /// </summary>
//    public void ReleaseSoldier()
//    {
//        if (!IsOccupied) return;

//        Debug.Log($"[DragonRiderSeat] '{_mountedSoldier?.name}' leaving seat.");
//        _mountedSoldier = null;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // EDITOR GIZMO — draw the seat offset in Scene view
//    // ══════════════════════════════════════════════════════════════════════════

//#if UNITY_EDITOR
//    private void OnDrawGizmosSelected()
//    {
//        var rt = transform as RectTransform;
//        if (rt == null) return;

//        // Convert local seatOffset to world position for the gizmo sphere
//        Vector3 worldPos = rt.TransformPoint(new Vector3(seatOffset.x, seatOffset.y, 0f));

//        Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
//        Gizmos.DrawWireSphere(worldPos, 8f);

//        UnityEditor.Handles.Label(worldPos + Vector3.up * 12f, "Rider Seat");
//    }
//#endif
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DRAGON RIDER SEAT
///
/// Attach to a child of the dragon prefab named "RiderSeat".
/// The soldier is reparented here on drop, so it automatically
/// follows and flips with the dragon during patrol.
///
/// ════════════════════════════════════════════════════════════════════
///  DRAGON PREFAB HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Dragon                        ← DragonController
///   ├── DragonBody [0]            ← Image: dragon body
///   ├── RiderSeat  [1]            ← DragonRiderSeat  ◄ THIS SCRIPT
///   │   └── DragonRiderVisual     ← DragonRiderVisual (hidden by default)
///   └── DragonWing [2]            ← Image: front wing
///
/// ════════════════════════════════════════════════════════════════════
///  HOW IT WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  1. SoldierDragDrop.OnEndDrag raycasts under the pointer.
///  2. If DragonRiderSeat is found and unoccupied, it calls MountSoldier.
///  3. The soldier is reparented to this transform at seatOffset.
///  4. Because the soldier is now a child of the dragon, it inherits
///     all movement and flip automatically — no extra code needed.
///  5. The soldier's CanvasGroup.blocksRaycasts is set to FALSE so that
///     clicking on the dragon area hits the dragon (not the soldier),
///     allowing the dragon to be dragged to a FlyZone with the soldier
///     riding. Without this, SoldierDragDrop intercepts every click.
///  6. Dragging the soldier off the dragon calls ReleaseSoldier, which
///     restores the soldier's blocksRaycasts and clears the seat.
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR
/// ════════════════════════════════════════════════════════════════════
///
///  seatOffset      Local anchoredPosition inside RiderSeat where the
///                  soldier is placed. Tune in Play mode.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DragonRiderSeat : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Seat Offset")]
    [Tooltip("Local position inside RiderSeat where the soldier is placed. " +
             "Increase Y to move the soldier higher on the dragon sprite.")]
    [SerializeField] private Vector2 seatOffset = new Vector2(0f, 40f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SoldierDragDrop _mountedSoldier;

    // Cached CanvasGroup from the mounted soldier so we can restore it on release.
    private CanvasGroup _soldierCG;

    /// <summary>True when a soldier is currently riding this dragon.</summary>
    public bool IsOccupied => _mountedSoldier != null;

    /// <summary>The soldier currently mounted, or null.</summary>
    public SoldierDragDrop MountedSoldier => _mountedSoldier;

    // ══════════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        EnsureRaycastTarget();
    }

    /// Adds a transparent Image so the EventSystem can raycast this seat.
    private void EnsureRaycastTarget()
    {
        if (GetComponent<Graphic>() != null) return;

        var img = gameObject.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;

        Debug.Log("[DragonRiderSeat] Added transparent Image for raycasting.", this);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by SoldierDragDrop when the soldier is dropped on this seat.
    /// Reparents the soldier here and positions it at seatOffset.
    ///
    /// Also sets the soldier's CanvasGroup.blocksRaycasts = false so that
    /// subsequent clicks reach the dragon instead of the soldier. This lets
    /// the player drag the whole dragon+rider unit to a FlyZone.
    ///
    /// Does nothing if the seat is already occupied.
    /// </summary>
    public void MountSoldier(SoldierDragDrop soldier)
    {
        if (IsOccupied)
        {
            Debug.Log("[DragonRiderSeat] Seat already occupied — ignoring drop.");
            return;
        }

        _mountedSoldier = soldier;

        // Notify the soldier so it can handle reparenting + state changes.
        soldier.MountOnDragon(this, seatOffset);

        // ── Disable soldier raycasts ──────────────────────────────────────────
        // With blocksRaycasts = true the soldier sits on top of the dragon and
        // intercepts every click, routing them to SoldierDragDrop instead of
        // DragonController. Setting it false makes clicks pass through the
        // soldier's Graphics to the dragon's body Image below, so the player
        // can drag the whole dragon+rider to a FlyZone without accidentally
        // lifting the soldier off.
        _soldierCG = soldier.GetComponent<CanvasGroup>();
        if (_soldierCG != null)
        {
            _soldierCG.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("[DragonRiderSeat] Mounted soldier has no CanvasGroup. " +
                             "Clicks may hit the soldier instead of the dragon, " +
                             "preventing the dragon from being dragged to a FlyZone.", this);
        }

        Debug.Log($"[DragonRiderSeat] '{soldier.name}' mounted on '{transform.parent?.name}'.");
    }

    /// <summary>
    /// Called by SoldierDragDrop at the START of a drag, before the soldier
    /// is lifted off. Clears the seat and restores the soldier's raycasts
    /// so SoldierDragDrop can function normally once the soldier is in the air.
    /// </summary>
    public void ReleaseSoldier()
    {
        if (!IsOccupied) return;

        // Restore the soldier's raycast blocking so it can be dropped normally.
        if (_soldierCG != null)
        {
            _soldierCG.blocksRaycasts = true;
            _soldierCG = null;
        }

        Debug.Log($"[DragonRiderSeat] '{_mountedSoldier?.name}' leaving seat.");
        _mountedSoldier = null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EDITOR GIZMO — draw the seat offset in Scene view
    // ══════════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;

        Vector3 worldPos = rt.TransformPoint(new Vector3(seatOffset.x, seatOffset.y, 0f));

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
        Gizmos.DrawWireSphere(worldPos, 8f);

        UnityEditor.Handles.Label(worldPos + Vector3.up * 12f, "Rider Seat");
    }
#endif
}