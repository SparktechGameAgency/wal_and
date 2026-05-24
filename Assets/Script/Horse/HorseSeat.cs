using UnityEngine;


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