//////using UnityEngine;

///////// <summary>
///////// Overlay placed on top of an exposed castle block (one with no block above it).
///////// Contains two CastleUnitDropZone children — one for Cannon, one for Soldier.
/////////
///////// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
/////////   CastleBlockUnitSlot   ← this script
/////////   ├── CannonZone        CastleUnitDropZone (acceptedType = Cannon)
/////////   │   ├── UnitIcon      Image
/////////   │   ├── EmptyVisual   GameObject (e.g. "+" icon)
/////////   │   └── Highlight     GameObject (glow frame)
/////////   └── SoldierZone       CastleUnitDropZone (acceptedType = Soldier)
/////////       ├── UnitIcon      Image
/////////       ├── EmptyVisual   GameObject
/////////       └── Highlight     GameObject
/////////
///////// ── How it is shown / hidden ───────────────────────────────────────────────
///////// CastleGrid calls GridCell.ShowUnitSlot / HideUnitSlot which instantiates
///////// or destroys this prefab as a child of the cell.
///////// </summary>
//////public class CastleBlockUnitSlot : MonoBehaviour
//////{
//////    // ── Auto-wired ────────────────────────────────────────────────
//////    private CastleUnitDropZone _cannonZone;
//////    private CastleUnitDropZone _soldierZone;

//////    private void Awake()
//////    {
//////        // Wire children by name
//////        var cannonT = transform.Find("CannonZone");
//////        var soldierT = transform.Find("SoldierZone");

//////        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();
//////        if (soldierT != null) _soldierZone = soldierT.GetComponent<CastleUnitDropZone>();

//////        if (_cannonZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
//////        if (_soldierZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'SoldierZone' child not found!");
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;
//////    public bool HasSoldier => _soldierZone != null && _soldierZone.HasUnit;

//////    public void RemoveCannon() => _cannonZone?.RemoveUnit();
//////    public void RemoveSoldier() => _soldierZone?.RemoveUnit();
//////    public void RemoveAll() { RemoveCannon(); RemoveSoldier(); }
//////}

////using UnityEngine;

/////// <summary>
/////// Overlay placed on top of an exposed castle block (one with no block above it).
/////// Contains two CastleUnitDropZone children — one for Cannon, one for Soldier.
///////
/////// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
///////   CastleBlockUnitSlot   ← this script
///////   ├── CannonZone        CastleUnitDropZone
///////   │   ├── EmptyVisual   GameObject (e.g. "+" icon)
///////   │   └── Highlight     GameObject (glow frame)
///////   └── SoldierZone       CastleUnitDropZone
///////       ├── EmptyVisual   GameObject
///////       └── Highlight     GameObject
/////// </summary>
////public class CastleBlockUnitSlot : MonoBehaviour
////{
////    // ── Auto-wired ────────────────────────────────────────────────
////    private CastleUnitDropZone _cannonZone;
////    private CastleUnitDropZone _soldierZone;

////    private void Awake()
////    {
////        var cannonT = transform.Find("CannonZone");
////        var soldierT = transform.Find("SoldierZone");

////        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();
////        if (soldierT != null) _soldierZone = soldierT.GetComponent<CastleUnitDropZone>();

////        // ── FIX: Enforce correct acceptedType in code so Inspector
////        //         misconfiguration can never cause drops to silently fail.
////        //         Cannon = 0 (default), Soldier = 1 — without this line
////        //         SoldierZone stays at 0 (Cannon) and all soldier drops
////        //         are rejected by the type-check inside OnDrop. ──────────
////        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;
////        if (_soldierZone != null) _soldierZone.acceptedType = CastleUnitType.Soldier;

////        if (_cannonZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
////        if (_soldierZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'SoldierZone' child not found!");
////    }

////    // ── Public API ────────────────────────────────────────────────

////    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;
////    public bool HasSoldier => _soldierZone != null && _soldierZone.HasUnit;

////    public void RemoveCannon() => _cannonZone?.RemoveUnit();
////    public void RemoveSoldier() => _soldierZone?.RemoveUnit();
////    public void RemoveAll() { RemoveCannon(); RemoveSoldier(); }
////}


//using UnityEngine;

///// <summary>
///// Overlay placed on top of an exposed castle block (one with no block above it).
///// Contains two CastleUnitDropZone children — one for Cannon, one for Soldier.
/////
///// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
/////   CastleBlockUnitSlot   ← this script
/////   ├── CannonZone        CastleUnitDropZone
/////   │   ├── EmptyVisual   GameObject (e.g. "+" icon)
/////   │   └── Highlight     GameObject (glow frame)
/////   └── SoldierZone       CastleUnitDropZone
/////       ├── EmptyVisual   GameObject
/////       └── Highlight     GameObject
///// </summary>
//public class CastleBlockUnitSlot : MonoBehaviour
//{
//    // ── Auto-wired ────────────────────────────────────────────────
//    private CastleUnitDropZone _cannonZone;
//    private CastleUnitDropZone _soldierZone;

//    private void Awake()
//    {
//        var cannonT = transform.Find("CannonZone");
//        var soldierT = transform.Find("SoldierZone");

//        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();
//        if (soldierT != null) _soldierZone = soldierT.GetComponent<CastleUnitDropZone>();

//        // ── FIX: Enforce correct acceptedType in code. ─────────────────────
//        // Without this, both zones stay at the enum default value of 0 (Cannon)
//        // because Unity initialises enum fields to 0 unless you set them in
//        // the Inspector — and it's easy to forget to set SoldierZone.
//        // Doing it here guarantees it is always correct regardless of prefab state.
//        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;
//        if (_soldierZone != null) _soldierZone.acceptedType = CastleUnitType.Soldier;

//        if (_cannonZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
//        if (_soldierZone == null) Debug.LogWarning("[CastleBlockUnitSlot] 'SoldierZone' child not found!");
//    }

//    // ── Public API ────────────────────────────────────────────────

//    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;
//    public bool HasSoldier => _soldierZone != null && _soldierZone.HasUnit;

//    public void RemoveCannon() => _cannonZone?.RemoveUnit();
//    public void RemoveSoldier() => _soldierZone?.RemoveUnit();
//    public void RemoveAll() { RemoveCannon(); RemoveSoldier(); }
//}

using UnityEngine;

/// <summary>
/// Overlay placed on top of an exposed castle block (one with no block above it).
/// Contains ONE drop zone child — CannonZone — for dragging a cannon onto the block.
///
/// The soldier is NOT a separate draggable unit. A child Image named "Soldier"
/// lives inside CannonZone and is shown automatically when the cannon is placed
/// (handled entirely inside CastleUnitDropZone.PlaceUnit / RemoveUnit).
///
/// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
///   CastleBlockUnitSlot   ← this script
///   └── CannonZone        CastleUnitDropZone (acceptedType = Cannon)
///       ├── EmptyVisual   GameObject (e.g. "+" icon shown on valid hover)
///       ├── Highlight     GameObject (glow frame on hover)
///       └── Soldier       Image — hidden by default, revealed when cannon placed
/// </summary>
public class CastleBlockUnitSlot : MonoBehaviour
{
    // ── Auto-wired ────────────────────────────────────────────────
    private CastleUnitDropZone _cannonZone;

    private void Awake()
    {
        var cannonT = transform.Find("CannonZone");

        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();

        // Enforce the correct acceptedType in code so Inspector misconfiguration
        // can never cause drops to silently fail.
        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;

        if (_cannonZone == null)
            Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>True when a cannon (and its accompanying soldier image) is present.</summary>
    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;

    /// <summary>Removes the cannon and hides the soldier image.</summary>
    public void RemoveCannon() => _cannonZone?.RemoveUnit();

    /// <summary>Alias kept for call-sites that use RemoveAll().</summary>
    public void RemoveAll() => RemoveCannon();
}