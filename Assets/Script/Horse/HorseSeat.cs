//using UnityEngine;
//using UnityEngine.EventSystems;

///// <summary>
///// AREA FORGE — HorseSeat
/////
///// Attach to a CHILD RectTransform of the Horse prefab.
///// This is the anchor point where the soldier sits.
/////
///// ════════════════════════════════════════════════════════════════════
/////  HORSE PREFAB HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   Horse  (RectTransform + Image + HorseController + CanvasGroup)
/////     └── SoldierSeat  (RectTransform + HorseSeat)   ← THIS component
/////
/////  Position the SoldierSeat RectTransform so its centre sits at the
/////  horse's saddle. The soldier's RectTransform pivot is centred on
/////  that point, so seatOffset lets you nudge the soldier up/forward
/////  relative to the saddle if needed.
/////
///// ════════════════════════════════════════════════════════════════════
/////  WHAT THIS DOES
///// ════════════════════════════════════════════════════════════════════
/////
/////  HorseSeat owns the slot:
/////    • MountSoldier(soldier)  — accepts a soldier, calls soldier.MountOnHorse()
/////    • ReleaseSoldier()       — clears the slot without moving the soldier
/////    • IsOccupied             — true while a soldier is sitting here
/////
/////  HorseController calls these from OnDrop / DismountButton.
///// </summary>
//public class HorseSeat : MonoBehaviour
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Seat Offset")]
//    [Tooltip("Pixel offset from the seat anchor to the soldier's anchoredPosition.\n" +
//             "Use this to nudge the soldier sprite up (Y) or forward (X) on the saddle.\n" +
//             "Zero works fine if your sprites are aligned already.")]
//    public Vector2 seatOffset = Vector2.zero;

//    // ── State ─────────────────────────────────────────────────────────────────

//    /// <summary>True while a soldier is sitting here.</summary>
//    public bool IsOccupied => _soldier != null;

//    /// <summary>The soldier currently in this seat, or null.</summary>
//    public SoldierDragDrop MountedSoldier => _soldier;

//    private SoldierDragDrop _soldier;

//    // ── Public API ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Accepts <paramref name="soldier"/> into the seat and calls
//    /// <see cref="SoldierDragDrop.MountOnHorse"/> so the soldier reparents,
//    /// hides its own visuals, and starts playing the horse-mounted animation.
//    /// </summary>
//    public void MountSoldier(SoldierDragDrop soldier)
//    {
//        if (soldier == null) return;

//        if (_soldier != null)
//        {
//            Debug.LogWarning($"[HorseSeat] '{name}' is already occupied by " +
//                             $"'{_soldier.name}'. Ignoring new mount request.", this);
//            return;
//        }

//        _soldier = soldier;
//        _soldier.MountOnHorse(this, seatOffset);

//        Debug.Log($"[HorseSeat] '{soldier.name}' mounted on horse seat '{name}'.");
//    }

//    /// <summary>
//    /// Clears the seat reference WITHOUT moving or showing the soldier.
//    /// The caller (HorseController or SoldierDragDrop) is responsible for
//    /// reparenting and showing the soldier after this call.
//    /// </summary>
//    public void ReleaseSoldier()
//    {
//        if (_soldier == null) return;
//        Debug.Log($"[HorseSeat] '{_soldier.name}' released from seat '{name}'.");
//        _soldier = null;
//    }
//}



using UnityEngine;

/// <summary>
/// HorseSeat
///
/// Attach to the SoldierSeat child of the horse prefab.
/// Handles mounting / dismounting a SoldierDragDrop onto this seat.
///
/// ════════════════════════════════════════════════════════════════════
///  POSITION FIX
/// ════════════════════════════════════════════════════════════════════
///
///  Root cause of "soldier jumps to top / walk zone":
///    SetParent(transform, worldPositionStays: true)  ← default / wrong
///    Unity tries to preserve the soldier's WORLD position, but inside a
///    Canvas hierarchy this converts screen-space coords into the seat's
///    local space, producing a huge, wrong anchoredPosition.
///
///  Fix: SetParent(transform, worldPositionStays: false) then manually
///  set anchoredPosition to seatOffset so the soldier snaps exactly
///  onto the seat regardless of where it was dragged from.
///
/// ════════════════════════════════════════════════════════════════════
///  PREFAB SETUP
/// ════════════════════════════════════════════════════════════════════
///
///   HorsePrefab
///     └── SoldierSeat   ← HorseSeat lives here
///           ├── Face    (Image)
///           ├── Helmet  (Image)
///           ├── Weapon  (Image)
///           └── Armor   (Image)
///
///  • Position SoldierSeat in the Inspector so it sits where the rider
///    should appear (e.g. slightly above the horse's back).
///  • seatOffset below is the final anchoredPosition of the soldier
///    relative to SoldierSeat — leave at zero if SoldierSeat is already
///    at the right spot; nudge Y upward if the soldier floats.
/// </summary>
public class HorseSeat : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Seat Offset")]
    [Tooltip("Fine-tune where the soldier sits relative to SoldierSeat (UI units).\n" +
             "Positive Y moves the soldier up, negative Y moves it down.\n" +
             "Leave at zero if SoldierSeat is already positioned correctly in the prefab.")]
    [SerializeField] private Vector2 seatOffset = Vector2.zero;

    // ── State ─────────────────────────────────────────────────────────────────

    private SoldierDragDrop _mountedSoldier;

    /// <summary>True while a soldier is seated.</summary>
    public bool IsOccupied => _mountedSoldier != null;

    /// <summary>The currently mounted soldier, or null.</summary>
    public SoldierDragDrop MountedSoldier => _mountedSoldier;

    // ── Mount ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reparents <paramref name="soldier"/> onto this seat and snaps it into place.
    ///
    /// KEY FIX — worldPositionStays: false
    ///   Passing false discards the soldier's old Canvas-space position and lets
    ///   us set anchoredPosition manually. Without this the soldier lands at a
    ///   position calculated from its drag-release coordinates, which maps to
    ///   somewhere completely wrong inside the seat's local space.
    /// </summary>
    public void MountSoldier(SoldierDragDrop soldier)
    {
        if (soldier == null) return;
        if (_mountedSoldier != null)
        {
            Debug.LogWarning($"[HorseSeat] '{name}' already occupied.", this);
            return;
        }

        _mountedSoldier = soldier;

        // MountOnHorse handles reparenting itself — do NOT call SetParent here.
        // Calling it here AND inside MountOnHorse caused a double-reparent that
        // confused RecordHome() and left the seat offset wrong on dismount.
        soldier.MountOnHorse(this, seatOffset);

        Debug.Log($"[HorseSeat] '{soldier.name}' mounted on '{name}'.");
    }

    // ── Dismount ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears the seat reference. Call after the soldier has been reparented
    /// back to its original parent by SoldierDragDrop.DismountFromHorse().
    /// </summary>
    public void ReleaseSoldier()
    {
        if (_mountedSoldier != null)
            Debug.Log($"[HorseSeat] '{_mountedSoldier.name}' dismounted from '{name}'.");

        _mountedSoldier = null;
    }
}