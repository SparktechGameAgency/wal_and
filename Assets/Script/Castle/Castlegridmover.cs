////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// Moves the CastleGridPanel between the Village Panel and the Castle Panel.
///////
/////// Setup in Inspector:
///////   castleGridPanel   → the RectTransform of your CastleGridPanel GameObject
///////   villageSlot       → empty RectTransform inside Village Panel (acts as placeholder anchor)
///////   castleSlot        → empty RectTransform inside Castle Panel  (acts as placeholder anchor)
///////   castleButton      → the Button that opens the Castle Panel
///////   villageButton     → the Button (or back button) that returns to Village Panel
///////   castlePanelRoot   → the root GameObject of the Castle Panel (to show/hide it)
/////// </summary>
////public class CastleGridMover : MonoBehaviour
////{
////    [Header("Grid")]
////    public RectTransform castleGridPanel;

////    [Header("Slot Anchors")]
////    [Tooltip("Empty child of Village Panel — grid lives here by default.")]
////    public RectTransform villageSlot;
////    [Tooltip("Empty child of Castle Panel — grid moves here when castle opens.")]
////    public RectTransform castleSlot;

////    [Header("Buttons")]
////    public Button castleButton;
////    public Button villageButton;   // back / village tab button

////    [Header("Castle Panel Root")]
////    [Tooltip("The root GameObject of the Castle Panel (shop list, coin display, etc.).")]
////    public GameObject castlePanelRoot;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Start()
////    {
////        // Start in Village view
////        MoveGridTo(villageSlot, showExpansionSlots: false);

////        if (castlePanelRoot != null)
////            castlePanelRoot.SetActive(false);

////        // Wire buttons
////        if (castleButton != null) castleButton.onClick.AddListener(OpenCastlePanel);
////        if (villageButton != null) villageButton.onClick.AddListener(OpenVillagePanel);
////    }

////    private void OnDestroy()
////    {
////        if (castleButton != null) castleButton.onClick.RemoveListener(OpenCastlePanel);
////        if (villageButton != null) villageButton.onClick.RemoveListener(OpenVillagePanel);
////    }

////    // ── Public API ────────────────────────────────────────────────

////    /// <summary>Called when the Castle button is clicked.</summary>
////    public void OpenCastlePanel()
////    {
////        if (castlePanelRoot != null)
////            castlePanelRoot.SetActive(true);

////        MoveGridTo(castleSlot, showExpansionSlots: true);
////    }

////    /// <summary>Called when the Village / back button is clicked.</summary>
////    public void OpenVillagePanel()
////    {
////        if (castlePanelRoot != null)
////            castlePanelRoot.SetActive(false);

////        MoveGridTo(villageSlot, showExpansionSlots: false);
////    }

////    // ── Private ───────────────────────────────────────────────────

////    private void MoveGridTo(RectTransform slot, bool showExpansionSlots)
////    {
////        if (castleGridPanel == null || slot == null) return;

////        // Reparent — grid becomes a child of the target slot
////        castleGridPanel.SetParent(slot, worldPositionStays: false);

////        // Stretch to fill the slot perfectly
////        castleGridPanel.anchorMin = Vector2.zero;
////        castleGridPanel.anchorMax = Vector2.one;
////        castleGridPanel.offsetMin = Vector2.zero;
////        castleGridPanel.offsetMax = Vector2.zero;
////        castleGridPanel.anchoredPosition = Vector2.zero;

////        // Show or hide expansion slots depending on which panel we moved to
////        if (CastleGrid.Instance != null)
////            CastleGrid.Instance.SetExpansionSlotsVisible(showExpansionSlots);

////        Debug.Log($"[CastleGridMover] Grid moved to {slot.name} | expansionSlots={showExpansionSlots}");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// Moves the CastleGridPanel between the Village Panel and Castle Panel
///// when the Castle button is clicked.
/////
///// ── Inspector wiring ──────────────────────────────────────────────────
/////
/////   castleGridPanel    →  the CastleGridPanel RectTransform
/////   villageGridParent  →  the container inside Village Panel that holds the grid
/////   castleGridParent   →  the container inside Castle Panel that will hold the grid
/////   castlePanelRoot    →  the root GameObject of the Castle Panel UI
/////   castleButton       →  the button that opens the Castle Panel
/////   villageButton      →  the back / Village tab button
/////
///// ── Scene hierarchy example ───────────────────────────────────────────
/////
/////   Canvas
/////   ├── VillagePanel
/////   │   ├── VillageGridContainer   ← assign to villageGridParent
/////   │   │   └── CastleGridPanel    ← assign to castleGridPanel  (starts here)
/////   │   └── ... other village UI
/////   └── CastlePanel                ← assign to castlePanelRoot  (starts hidden)
/////       ├── CastleGridContainer    ← assign to castleGridParent
/////       └── ... shop list, coins etc.
/////
///// </summary>
//public class CastleGridMover : MonoBehaviour
//{
//    [Header("The grid that moves")]
//    public RectTransform castleGridPanel;

//    [Header("Parent containers  (NOT slot anchors — the actual containers)")]
//    public RectTransform villageGridParent;   // inside Village Panel
//    public RectTransform castleGridParent;    // inside Castle Panel

//    [Header("Castle Panel root (will be shown / hidden)")]
//    public GameObject castlePanelRoot;

//    [Header("Buttons")]
//    public Button castleButton;
//    public Button villageButton;   // back / village tab

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Start()
//    {
//        // Validate required references before doing anything
//        if (!Validate()) return;

//        // Always start in Village view
//        castlePanelRoot.SetActive(false);
//        SendGridTo(villageGridParent, showSlots: false);

//        castleButton.onClick.AddListener(OpenCastlePanel);
//        if (villageButton != null)
//            villageButton.onClick.AddListener(OpenVillagePanel);
//    }

//    private void OnDestroy()
//    {
//        if (castleButton != null) castleButton.onClick.RemoveListener(OpenCastlePanel);
//        if (villageButton != null) villageButton.onClick.RemoveListener(OpenVillagePanel);
//    }

//    // ── Button callbacks ──────────────────────────────────────────

//    public void OpenCastlePanel()
//    {
//        Debug.Log("[CastleGridMover] OpenCastlePanel called.");

//        // 1. Show Castle Panel FIRST so castleGridParent is active
//        castlePanelRoot.SetActive(true);

//        // 2. Move grid into Castle Panel
//        SendGridTo(castleGridParent, showSlots: true);
//    }

//    public void OpenVillagePanel()
//    {
//        Debug.Log("[CastleGridMover] OpenVillagePanel called.");

//        // 1. Move grid back to Village Panel
//        SendGridTo(villageGridParent, showSlots: false);

//        // 2. Hide Castle Panel
//        castlePanelRoot.SetActive(false);
//    }

//    // ── Core move ─────────────────────────────────────────────────

//    private void SendGridTo(RectTransform newParent, bool showSlots)
//    {
//        if (castleGridPanel == null || newParent == null)
//        {
//            Debug.LogError("[CastleGridMover] castleGridPanel or target parent is null!");
//            return;
//        }

//        // Reparent without preserving world position
//        castleGridPanel.SetParent(newParent, worldPositionStays: false);

//        // Stretch to fill the new parent completely
//        castleGridPanel.anchorMin = Vector2.zero;
//        castleGridPanel.anchorMax = Vector2.one;
//        castleGridPanel.offsetMin = Vector2.zero;
//        castleGridPanel.offsetMax = Vector2.zero;
//        castleGridPanel.anchoredPosition = Vector2.zero;
//        castleGridPanel.localScale = Vector3.one;

//        // Make sure the grid itself is active
//        castleGridPanel.gameObject.SetActive(true);

//        // Toggle expansion slots
//        if (CastleGrid.Instance != null)
//            CastleGrid.Instance.SetExpansionSlotsVisible(showSlots);
//        else
//            Debug.LogWarning("[CastleGridMover] CastleGrid.Instance is null — expansion slots not toggled.");

//        Debug.Log($"[CastleGridMover] Grid moved to '{newParent.name}' | expansionSlots={showSlots}");
//    }

//    // ── Validation ────────────────────────────────────────────────

//    private bool Validate()
//    {
//        bool ok = true;

//        if (castleGridPanel == null) { Debug.LogError("[CastleGridMover] castleGridPanel is not assigned!"); ok = false; }
//        if (villageGridParent == null) { Debug.LogError("[CastleGridMover] villageGridParent is not assigned!"); ok = false; }
//        if (castleGridParent == null) { Debug.LogError("[CastleGridMover] castleGridParent is not assigned!"); ok = false; }
//        if (castlePanelRoot == null) { Debug.LogError("[CastleGridMover] castlePanelRoot is not assigned!"); ok = false; }
//        if (castleButton == null) { Debug.LogError("[CastleGridMover] castleButton is not assigned!"); ok = false; }

//        return ok;
//    }
//}

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