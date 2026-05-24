using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ArcherSlot
///
/// Place one of these on each archer position on the castle wall.
/// When the Archer tab is active and the player drags a soldier over an
/// empty slot and drops it, an ArcherUnit prefab is spawned at the slot.
///
/// ── Required child hierarchy (exact GameObject names) ─────────────────────
///
///   ArcherSlot            ← root: this script + Image (raycast target)
///   ├── EmptyVisual       shown while the slot is empty and visible
///   ├── Highlight         glow frame shown while a valid drag hovers over it
///   └── Spawnpoint        world/UI transform where the ArcherUnit prefab spawns
///
/// ── Inspector wiring ──────────────────────────────────────────────────────
///   archerPrefab    → Prefab with ArcherUnit component
///   acceptedTab     → Leave as default — slot becomes interactive only when
///                     CastleTabController is in Archer mode.
///
/// ── How it reads the dragged soldier ──────────────────────────────────────
///   SoldierDragDrop.CurrentlyDragging is the static reference that
///   SoldierDragDrop sets at the start of every drag. The slot checks it in
///   OnDrop to confirm a soldier is actually being dragged.
/// </summary>
[RequireComponent(typeof(Image))]
public class ArcherSlot : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // ── Inspector ─────────────────────────────────────────────────

    [Header("Prefab")]
    [Tooltip("Prefab that has an ArcherUnit component. Spawned when a soldier is dropped here.")]
    [SerializeField] private GameObject archerPrefab;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
    [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);
    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.10f);

    // ── Auto-wired children ───────────────────────────────────────

    private Image _bg;
    private GameObject _emptyVisual;
    private GameObject _highlight;
    private Transform _spawnpoint;

    // ── State ─────────────────────────────────────────────────────

    /// <summary>True while an ArcherUnit is stationed here.</summary>
    public bool IsOccupied { get; private set; }

    private GameObject _archerInstance;
    private SoldierDragDrop _stationedSoldier;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _bg = GetComponent<Image>();
        _bg.color = normalColor;
        _bg.raycastTarget = true;

        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
        _highlight = transform.Find("Highlight")?.gameObject;
        _spawnpoint = transform.Find("Spawnpoint");

        // Fall back to this transform if no Spawnpoint child exists
        if (_spawnpoint == null) _spawnpoint = transform;

        _emptyVisual?.SetActive(true);
        _highlight?.SetActive(false);
    }

    // ── IDropHandler ─────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        _highlight?.SetActive(false);

        // Only accept drops while in Archer tab mode
        if (!IsArcherTabActive())
        {
            Debug.Log("[ArcherSlot] Drop ignored — not in Archer tab.");
            ResetColor();
            return;
        }

        if (IsOccupied)
        {
            Debug.Log("[ArcherSlot] Already occupied.");
            ResetColor();
            return;
        }

        SoldierDragDrop soldier = SoldierDragDrop.CurrentlyDragging;
        if (soldier == null)
        {
            Debug.Log("[ArcherSlot] OnDrop — no soldier being dragged.");
            ResetColor();
            return;
        }

        PlaceArcher(soldier);
    }

    // ── IPointerEnterHandler / ExitHandler ───────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsArcherTabActive() || IsOccupied) return;

        _highlight?.SetActive(true);

        bool validDrag = SoldierDragDrop.CurrentlyDragging != null;
        _bg.color = validDrag ? hoverValidColor : hoverInvalidColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlight?.SetActive(false);
        ResetColor();
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Spawns the ArcherUnit prefab at the spawnpoint and records which soldier
    /// was stationed here. Called automatically by OnDrop.
    /// </summary>
    public void PlaceArcher(SoldierDragDrop soldier)
    {
        if (IsOccupied) return;
        if (archerPrefab == null)
        {
            Debug.LogError("[ArcherSlot] archerPrefab is not assigned!");
            return;
        }

        // Spawn the archer prefab
        _archerInstance = Instantiate(archerPrefab, _spawnpoint.position, Quaternion.identity, _spawnpoint);

        // Wire the ArcherUnit so it knows its owning slot
        ArcherUnit archerUnit = _archerInstance.GetComponent<ArcherUnit>();
        if (archerUnit != null)
            archerUnit.Init(this);

        _stationedSoldier = soldier;
        IsOccupied = true;

        _emptyVisual?.SetActive(false);
        _bg.color = occupiedColor;

        Debug.Log($"[ArcherSlot] Archer spawned at {gameObject.name}.");
    }

    /// <summary>
    /// Removes the archer from this slot (e.g. when the slot is destroyed or
    /// the player recalls the soldier).
    /// </summary>
    public void RemoveArcher()
    {
        if (!IsOccupied) return;

        if (_archerInstance != null)
            Destroy(_archerInstance);

        _archerInstance = null;
        _stationedSoldier = null;
        IsOccupied = false;

        _emptyVisual?.SetActive(true);
        ResetColor();

        Debug.Log($"[ArcherSlot] Archer removed from {gameObject.name}.");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void ResetColor()
    {
        _bg.color = IsOccupied ? occupiedColor : normalColor;
    }

    /// <summary>
    /// Returns true when the Castle panel is open and the Archer sub-tab is active.
    /// Falls back gracefully when CastleTabController is not present.
    /// </summary>
    private static bool IsArcherTabActive()
    {
        CastleTabController ctrl = FindFirstObjectByType<CastleTabController>();
        if (ctrl == null) return true; // assume active if controller not present
        return ctrl.ActiveTab == CastleTabController.CastleTab.Archer;
    }
}