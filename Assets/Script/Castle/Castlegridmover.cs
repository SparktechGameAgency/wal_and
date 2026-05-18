
using UnityEngine;

/// <summary>
/// Moves the CastleGridPanel between Village and Castle containers.
/// UIManager calls OpenCastlePanel() / OpenVillagePanel() directly —
/// no button wiring here (UIManager already owns the buttons).
///
/// ── Inspector wiring ──────────────────────────────────────────────
///   castleGridPanel   → your CastleGridPanel RectTransform
///   villageGridParent → the container inside VillagePanel  (e.g. VillageGridSlot)
///   castleGridParent  → the container inside CastlePanel   (e.g. CastleGridSlot)
///   castlePanelRoot   → the root GameObject of CastlePanel (shown/hidden on switch)
/// </summary>
public class CastleGridMover : MonoBehaviour
{
    public static CastleGridMover Instance { get; private set; }

    [Header("The grid that moves")]
    public RectTransform castleGridPanel;

    [Header("Parent containers inside each panel")]
    public RectTransform villageGridParent;   // e.g. VillageGridSlot
    public RectTransform castleGridParent;    // e.g. CastleGridSlot

    [Header("Castle Panel root GameObject")]
    public GameObject castlePanelRoot;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!Validate()) return;

        // Start in Village view — hide Castle Panel, grid stays in village slot
        castlePanelRoot.SetActive(false);
        SendGridTo(villageGridParent, showSlots: false);
    }

    // ── Public API (called by UIManager) ─────────────────────────

    /// <summary>Called by UIManager when the Castle button is clicked.</summary>
    public void OpenCastlePanel()
    {
        if (!Validate()) return;

        // 1. Show Castle Panel root FIRST so castleGridParent is active
        castlePanelRoot.SetActive(true);

        // 2. Move grid into Castle Panel
        SendGridTo(castleGridParent, showSlots: true);

        Debug.Log("[CastleGridMover] Castle Panel opened.");
    }

    /// <summary>Called by UIManager when the Back / close button is clicked.</summary>
    public void OpenVillagePanel()
    {
        if (!Validate()) return;

        // 1. Move grid back to Village Panel
        SendGridTo(villageGridParent, showSlots: false);

        // 2. Hide Castle Panel root
        castlePanelRoot.SetActive(false);

        Debug.Log("[CastleGridMover] Village Panel restored.");
    }

    // ── Core move ─────────────────────────────────────────────────

    private void SendGridTo(RectTransform newParent, bool showSlots)
    {
        if (castleGridPanel == null || newParent == null) return;

        // Reparent
        castleGridPanel.SetParent(newParent, worldPositionStays: false);

        // Stretch to fill parent
        castleGridPanel.anchorMin = Vector2.zero;
        castleGridPanel.anchorMax = Vector2.one;
        castleGridPanel.offsetMin = Vector2.zero;
        castleGridPanel.offsetMax = Vector2.zero;
        castleGridPanel.anchoredPosition = Vector2.zero;
        castleGridPanel.localScale = Vector3.one;

        castleGridPanel.gameObject.SetActive(true);

        // Toggle expansion slots
        if (CastleGrid.Instance != null)
            CastleGrid.Instance.SetExpansionSlotsVisible(showSlots);

        Debug.Log($"[CastleGridMover] Grid → '{newParent.name}' | slots={showSlots}");
    }

    // ── Validation ────────────────────────────────────────────────

    private bool Validate()
    {
        bool ok = true;
        if (castleGridPanel == null) { Debug.LogError("[CastleGridMover] castleGridPanel not assigned!"); ok = false; }
        if (villageGridParent == null) { Debug.LogError("[CastleGridMover] villageGridParent not assigned!"); ok = false; }
        if (castleGridParent == null) { Debug.LogError("[CastleGridMover] castleGridParent not assigned!"); ok = false; }
        if (castlePanelRoot == null) { Debug.LogError("[CastleGridMover] castlePanelRoot not assigned!"); ok = false; }
        return ok;
    }
}