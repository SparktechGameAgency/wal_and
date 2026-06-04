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

    /// <summary>
    /// Called by CannonZone or ArcherZone whenever their occupancy changes.
    /// If one zone is occupied, the other zone is hidden; if both are empty,
    /// each zone follows normal tab-visibility rules.
    /// </summary>
    public void NotifyOccupancyChanged()
    {
        if (HasCannon)
        {
            // Cannon is placed — hide the archer zone entirely
            if (_archerZone != null)
                _archerZone.gameObject.SetActive(false);
        }
        else if (HasArcher)
        {
            // Archer is placed — hide the cannon zone entirely
            if (_cannonZone != null)
                _cannonZone.gameObject.SetActive(false);
        }
        else
        {
            // Both zones are empty — restore visibility based on current tab.
            // Each zone's SetXxxTabActive call already handles show/hide,
            // so we re-apply whichever tab is currently active by asking the
            // zones to refresh without changing any tab state. The simplest
            // safe approach is to re-activate each zone's GameObject so the
            // tab controller can manage it normally on the next tab switch.
            if (_cannonZone != null)
                _cannonZone.gameObject.SetActive(true);
            if (_archerZone != null)
                _archerZone.gameObject.SetActive(true);
        }
    }

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