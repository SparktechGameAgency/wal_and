using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// AREA FORGE — HorseSeat
///
/// Attach to a CHILD RectTransform of the Horse prefab.
/// This is the anchor point where the soldier sits.
///
/// ════════════════════════════════════════════════════════════════════
///  HORSE PREFAB HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Horse  (RectTransform + Image + HorseController + CanvasGroup)
///     └── SoldierSeat  (RectTransform + HorseSeat)   ← THIS component
///
///  Position the SoldierSeat RectTransform so its centre sits at the
///  horse's saddle. The soldier's RectTransform pivot is centred on
///  that point, so seatOffset lets you nudge the soldier up/forward
///  relative to the saddle if needed.
///
/// ════════════════════════════════════════════════════════════════════
///  WHAT THIS DOES
/// ════════════════════════════════════════════════════════════════════
///
///  HorseSeat owns the slot:
///    • MountSoldier(soldier)  — accepts a soldier, calls soldier.MountOnHorse()
///    • ReleaseSoldier()       — clears the slot without moving the soldier
///    • IsOccupied             — true while a soldier is sitting here
///
///  HorseController calls these from OnDrop / DismountButton.
/// </summary>
public class HorseSeat : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Seat Offset")]
    [Tooltip("Pixel offset from the seat anchor to the soldier's anchoredPosition.\n" +
             "Use this to nudge the soldier sprite up (Y) or forward (X) on the saddle.\n" +
             "Zero works fine if your sprites are aligned already.")]
    public Vector2 seatOffset = Vector2.zero;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>True while a soldier is sitting here.</summary>
    public bool IsOccupied => _soldier != null;

    /// <summary>The soldier currently in this seat, or null.</summary>
    public SoldierDragDrop MountedSoldier => _soldier;

    private SoldierDragDrop _soldier;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Accepts <paramref name="soldier"/> into the seat and calls
    /// <see cref="SoldierDragDrop.MountOnHorse"/> so the soldier reparents,
    /// hides its own visuals, and starts playing the horse-mounted animation.
    /// </summary>
    public void MountSoldier(SoldierDragDrop soldier)
    {
        if (soldier == null) return;

        if (_soldier != null)
        {
            Debug.LogWarning($"[HorseSeat] '{name}' is already occupied by " +
                             $"'{_soldier.name}'. Ignoring new mount request.", this);
            return;
        }

        _soldier = soldier;
        _soldier.MountOnHorse(this, seatOffset);

        Debug.Log($"[HorseSeat] '{soldier.name}' mounted on horse seat '{name}'.");
    }

    /// <summary>
    /// Clears the seat reference WITHOUT moving or showing the soldier.
    /// The caller (HorseController or SoldierDragDrop) is responsible for
    /// reparenting and showing the soldier after this call.
    /// </summary>
    public void ReleaseSoldier()
    {
        if (_soldier == null) return;
        Debug.Log($"[HorseSeat] '{_soldier.name}' released from seat '{name}'.");
        _soldier = null;
    }
}

