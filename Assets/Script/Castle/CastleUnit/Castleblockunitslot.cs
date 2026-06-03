using UnityEngine;

/// <summary>
/// CastleBlockUnitSlot
///
/// Manages both the CannonZone and ArcherZone children on every castle block.
///
/// Child hierarchy:
///   CastleBlockUnitSlot
///   ├── CannonZone    CastleUnitDropZone  — shown in Cannon tab
///   └── ArcherZone    ArcherZoneCastle    — shown in Archer tab
/// </summary>
public class CastleBlockUnitSlot : MonoBehaviour
{
    private CastleUnitDropZone _cannonZone;
    private ArcherZoneCastle _archerZone;

    private void Awake()
    {
        // ── Cannon Zone ──────────────────────────────────────────
        var cannonT = transform.Find("CannonZone");
        if (cannonT != null) _cannonZone = cannonT.GetComponent<CastleUnitDropZone>();
        if (_cannonZone != null) _cannonZone.acceptedType = CastleUnitType.Cannon;
        else Debug.LogWarning("[CastleBlockUnitSlot] 'CannonZone' child not found!", this);

        // ── Archer Zone ──────────────────────────────────────────
        var archerT = transform.Find("ArcherZone");
        if (archerT != null) _archerZone = archerT.GetComponent<ArcherZoneCastle>();
        else Debug.LogWarning("[CastleBlockUnitSlot] 'ArcherZone' child not found! " +
                              "Add a child named 'ArcherZone' with ArcherZoneCastle.", this);
    }

    // ── Public API ────────────────────────────────────────────────

    public bool HasCannon => _cannonZone != null && _cannonZone.HasUnit;
    public bool HasArcher => _archerZone != null && _archerZone.IsOccupied;

    /// <summary>True when an archer is stationed here — blocks cannon placement.</summary>
    public bool IsBlockedByCannon => HasCannon;

    /// <summary>True when a cannon is placed here — blocks archer placement.</summary>
    public bool IsBlockedByArcher => HasArcher;

    public void RemoveCannon() => _cannonZone?.RemoveUnit();
    public void RemoveArcher() => _archerZone?.RemoveArcher();
    public void RemoveAll() { RemoveCannon(); RemoveArcher(); }

    /// <summary>Legacy compatibility — hides both zones in village mode.</summary>
    public void SetVillageMode(bool isVillage)
    {
        if (isVillage)
        {
            SetCannonTabActive(false);
            SetArcherTabActive(false);
        }
    }

    /// <summary>
    /// Called by CastleTabController when the Cannon tab activates/deactivates.
    /// </summary>
    public void SetCannonTabActive(bool active)
    {
        _cannonZone?.SetCannonTabActive(active);

        // Hide archer zone whenever cannon tab turns on.
        if (active) _archerZone?.SetArcherTabActive(false);
    }

    /// <summary>
    /// Called by CastleTabController when the Archer tab activates/deactivates.
    /// </summary>
    public void SetArcherTabActive(bool active)
    {
        _archerZone?.SetArcherTabActive(active);

        // Hide cannon zone whenever archer tab turns on.
        if (active) _cannonZone?.SetCannonTabActive(false);
    }
}