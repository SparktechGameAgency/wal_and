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
///
/// ── Village / Castle mode ────────────────────────────────────────────────
/// Call SetVillageMode(true)  via GridCell when the grid is in the Village Panel:
///   → The cannon zone background goes alpha = 0 (invisible).
///   → Raycasts stay ON — the player can still drop a cannon from the shop.
/// Call SetVillageMode(false) when the grid is in the Castle Panel:
///   → The cannon zone background is restored to its normal color.
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

    /// <summary>
    /// Sets village or castle display mode on the cannon zone.
    ///
    /// Village mode (isVillage = true):
    ///   Zone background becomes fully transparent (alpha = 0).
    ///   Raycasts remain enabled — drag-and-drop still works.
    ///   On hover, the zone briefly shows the hover color as visual feedback.
    ///
    /// Castle mode (isVillage = false):
    ///   Zone background is restored to its normal (slightly tinted) color.
    /// </summary>
    public void SetVillageMode(bool isVillage) => _cannonZone?.SetVillageMode(isVillage);
}