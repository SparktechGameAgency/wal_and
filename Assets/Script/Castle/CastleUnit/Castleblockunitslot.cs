//using UnityEngine;

///// <summary>
///// Overlay placed on top of an exposed castle block (one with no block above it).
///// Contains ONE drop zone child — CannonZone — for dragging a cannon onto the block.
/////
///// The soldier is NOT a separate draggable unit. A child Image named "Soldier"
///// lives inside CannonZone and is shown automatically when the cannon is placed
///// (handled entirely inside CastleUnitDropZone.PlaceUnit / RemoveUnit).
/////
///// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
/////   CastleBlockUnitSlot   ← this script
/////   └── CannonZone        CastleUnitDropZone (acceptedType = Cannon)
/////       ├── EmptyVisual   GameObject (e.g. "+" icon shown on valid hover)
/////       ├── Highlight     GameObject (glow frame on hover)
/////       └── Soldier       Image — hidden by default, revealed when cannon placed
/////
///// ── Village / Castle mode ────────────────────────────────────────────────
///// Call SetVillageMode(true)  via GridCell when the grid is in the Village Panel:
/////   → The cannon zone background goes alpha = 0 (invisible).
/////   → Raycasts stay ON — the player can still drop a cannon from the shop.
///// Call SetVillageMode(false) when the grid is in the Castle Panel:
/////   → The cannon zone background is restored to its normal color.
///// </summary>
//public class CastleBlockUnitSlot : MonoBehaviour
//{
//    // ── Auto-wired ────────────────────────────────────────────────
//    private CastleUnitDropZone _cannonZone;

//    private void Awake()
//    {
//        var cannonT = transform.Find("CannonZone");

//        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();

//        // Enforce the correct acceptedType in code so Inspector misconfiguration
//        // can never cause drops to silently fail.
//        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;

//        if (_cannonZone == null)
//            Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
//    }

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>True when a cannon (and its accompanying soldier image) is present.</summary>
//    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;

//    /// <summary>Removes the cannon and hides the soldier image.</summary>
//    public void RemoveCannon() => _cannonZone?.RemoveUnit();

//    /// <summary>Alias kept for call-sites that use RemoveAll().</summary>
//    public void RemoveAll() => RemoveCannon();

//    /// <summary>
//    /// Sets village or castle display mode on the cannon zone.
//    ///
//    /// Village mode (isVillage = true):
//    ///   Zone background becomes fully transparent (alpha = 0).
//    ///   Raycasts remain enabled — drag-and-drop still works.
//    ///   On hover, the zone briefly shows the hover color as visual feedback.
//    ///
//    /// Castle mode (isVillage = false):
//    ///   Zone background is restored to its normal (slightly tinted) color.
//    /// </summary>
//    public void SetVillageMode(bool isVillage) => _cannonZone?.SetVillageMode(isVillage);
//}

using UnityEngine;

/// <summary>
/// Overlay placed on top of an exposed castle block (one with no block above it).
/// Contains ONE drop zone child — CannonZone — for dragging a cannon onto the block.
///
/// ── Required child hierarchy (auto-wired by name in Awake) ─────────────────
///   CastleBlockUnitSlot   ← this script
///   └── CannonZone        CastleUnitDropZone (acceptedType = Cannon)
///       ├── EmptySlotZone GameObject — shown ONLY in Cannon tab when zone is empty
///       ├── EmptyVisual   GameObject (hover hint)
///       ├── Highlight     GameObject (hover glow)
///       └── Soldier       Image — shown when cannon is placed
///
/// ── Visibility rules ─────────────────────────────────────────────────────
///   Cannon tab selected  → SetCannonTabActive(true)
///     Empty zone  → EmptySlotZone ON,  zone interactive.
///     Filled zone → EmptySlotZone OFF, zone non-interactive (cannon visible).
///
///   Any other tab/panel → SetCannonTabActive(false)
///     ALL zones → EmptySlotZone OFF, zone non-interactive.
///     Placed cannon children remain visible (they are children of the zone GO).
/// </summary>
public class CastleBlockUnitSlot : MonoBehaviour
{
    // ── Auto-wired ────────────────────────────────────────────────
    private CastleUnitDropZone _cannonZone;

    private void Awake()
    {
        var cannonT = transform.Find("CannonZone");

        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();

        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;

        if (_cannonZone == null)
            Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!");
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>True when a cannon is present in this slot.</summary>
    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;

    /// <summary>Removes the cannon and hides the soldier image.</summary>
    public void RemoveCannon() => _cannonZone?.RemoveUnit();

    /// <summary>Alias kept for call-sites that use RemoveAll().</summary>
    public void RemoveAll() => RemoveCannon();

    /// <summary>
    /// Legacy village/castle mode — kept for CastleGrid compatibility.
    /// Forwards to SetCannonTabActive(false) when in village mode.
    /// </summary>
    public void SetVillageMode(bool isVillage)
    {
        // Village = not in Cannon tab → hide all zone overlays.
        if (isVillage) SetCannonTabActive(false);
        // Castle mode is handled by CastleTabController via SetCannonTabActive.
    }

    /// <summary>
    /// Called by CastleTabController when the Cannon tab is activated (true)
    /// or deactivated (false, i.e. Expand / Archer / Village / any other panel).
    ///
    /// true  → empty zones show EmptySlotZone and accept drops.
    ///         filled zones keep their cannon visible but stay non-interactive.
    /// false → all zone overlay visuals are hidden; placed cannons stay visible.
    /// </summary>
    public void SetCannonTabActive(bool active)
    {
        _cannonZone?.SetCannonTabActive(active);
    }
}