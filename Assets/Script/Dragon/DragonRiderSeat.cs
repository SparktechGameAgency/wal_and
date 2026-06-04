//using UnityEngine;
//using UnityEngine.UI;


//[RequireComponent(typeof(RectTransform))]
//public class DragonRiderSeat : MonoBehaviour
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Seat Offset")]
//    [Tooltip("Local position inside RiderSeat where the soldier is placed. " +
//             "Increase Y to move the soldier higher on the dragon sprite.")]
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
//    /// Called by DragonController.PerformMount() when the soldier is dropped here.
//    /// Reparents the soldier to this seat at seatOffset.
//    ///
//    /// Does NOT touch the soldier's CanvasGroup.blocksRaycasts — that is owned by
//    /// SoldierDragDrop.SetLocked(). After mount the soldier is always unlocked
//    /// (blocksRaycasts=true) so the player can drag them off without clicking Attach.
//    ///
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

//        // MountOnDragon handles: helmet auto-equip, patrol stop, reparenting,
//        // HideOwnVisuals (alpha=0, blocksRaycasts=true, interactable=true).
//        // blocksRaycasts=true is the correct unlocked default — the player can
//        // drag the soldier off the dragon without needing to click Attach first.
//        soldier.MountOnDragon(this, seatOffset);

//        Debug.Log($"[DragonRiderSeat] '{soldier.name}' mounted on '{transform.parent?.name}'.");
//    }

//    /// <summary>
//    /// Called by SoldierDragDrop.OnBeginDrag() at the START of a dismount drag,
//    /// and by SoldierDragDrop.DismountFromDragon() for programmatic dismounts.
//    ///
//    /// Clears the seat so a new soldier can mount. Does NOT restore blocksRaycasts —
//    /// SoldierDragDrop.ShowOwnVisuals() / DismountFromDragon() handle that.
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

//        Vector3 worldPos = rt.TransformPoint(new Vector3(seatOffset.x, seatOffset.y, 0f));

//        Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
//        Gizmos.DrawWireSphere(worldPos, 8f);

//        UnityEditor.Handles.Label(worldPos + Vector3.up * 12f, "Rider Seat");
//    }
//#endif
//}

using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class DragonRiderSeat : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Seat Offset")]
    [Tooltip("Local position inside the DragonRide root where the soldier is placed.")]
    [SerializeField] private Vector2 seatOffset = new Vector2(0f, 40f);

    [Header("Rider GameObject (built-in visual — will be hidden on mount)")]
    [Tooltip("Assign the 'Rider' child GameObject here. It will be deactivated " +
             "when a soldier mounts so only the soldier prefab is visible.")]
    [SerializeField] private GameObject riderVisualObject;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SoldierDragDrop _mountedSoldier;

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

        // Auto-find the Rider child if not assigned in Inspector.
        if (riderVisualObject == null)
        {
            Transform found = transform.Find("Rider");
            if (found != null)
            {
                riderVisualObject = found.gameObject;
                Debug.Log("[DragonRiderSeat] Auto-found 'Rider' child GameObject.", this);
            }
        }
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
    /// Called by DragonController.PerformMount() when the soldier is dropped here.
    ///
    /// The soldier prefab is reparented INSIDE the DragonRide(Clone) root
    /// (transform.parent of this seat), NOT directly under this seat object.
    /// seatOffset positions it correctly on top of the dragon.
    ///
    /// The built-in 'Rider' GameObject is deactivated so only the soldier
    /// prefab is visible as the rider.
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

        // Deactivate the built-in Rider visual so only the soldier prefab shows.
        if (riderVisualObject != null)
            riderVisualObject.SetActive(false);
        else
            Debug.LogWarning("[DragonRiderSeat] riderVisualObject is not assigned. " +
                             "Assign the 'Rider' child in the Inspector.", this);

        // MountOnDragon handles: helmet auto-equip, patrol stop,
        // reparenting to the DragonRide(Clone) root, seatOffset placement,
        // and HideOwnVisuals (alpha=0, blocksRaycasts=true, interactable=true).
        soldier.MountOnDragon(this, seatOffset);

        Debug.Log($"[DragonRiderSeat] '{soldier.name}' mounted on '{transform.parent?.name}'.");
    }

    /// <summary>
    /// Called by SoldierDragDrop.OnBeginDrag() at the START of a dismount drag,
    /// and by SoldierDragDrop.DismountFromDragon() for programmatic dismounts.
    ///
    /// Re-activates the built-in Rider visual and clears the seat.
    /// </summary>
    public void ReleaseSoldier()
    {
        if (!IsOccupied) return;

        Debug.Log($"[DragonRiderSeat] '{_mountedSoldier?.name}' leaving seat.");
        _mountedSoldier = null;

        // Restore the built-in Rider visual now that no soldier prefab is riding.
        if (riderVisualObject != null)
            riderVisualObject.SetActive(true);
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