////using UnityEngine;
////using UnityEngine.UI;
////using System.Collections.Generic;

/////// <summary>
/////// CastleTabController
///////
/////// Sits on the Castle Panel root (alongside CastlePanel).
/////// Manages the three sub-mode buttons that live inside the Castle Panel:
///////
///////   [Expand]  → shows ExpansionSlots so the player can add new castle blocks.
///////   [Cannon]  → hides expansion slots; reveals CannonSlots so the player can
///////               open the Cannon Panel and equip cannons.
///////   [Archer]  → hides expansion slots; reveals ArcherSlots so the player can
///////               drag soldiers onto archer positions.
///////
/////// ── Inspector wiring ───────────────────────────────────────────────────────
///////   expandButton   → Button labelled "Expand"
///////   cannonButton   → Button labelled "Cannon"
///////   archerButton   → Button labelled "Archer"
///////
///////   cannonSlotsRoot → Parent GameObject that holds all CannonSlot GameObjects
///////                     (show/hide the whole group instead of individual slots).
///////   archerSlotsRoot → Parent GameObject that holds all ArcherSlot GameObjects.
///////
///////   Selected / normal colors let you visually highlight the active tab.
///////
/////// ── How it connects to CastleGrid ──────────────────────────────────────────
///////   SetExpansionSlotsVisible(true/false) on CastleGrid is called so expansion
///////   overlays appear only in Expand mode.
/////// </summary>
////public class CastleTabController : MonoBehaviour
////{
////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button expandButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button archerButton;

////    [Header("Slot Containers")]
////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
////    [SerializeField] private GameObject cannonSlotsRoot;

////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
////    [SerializeField] private GameObject archerSlotsRoot;

////    [Header("Tab Highlight Colors (optional)")]
////    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
////    [SerializeField] private Color deselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

////    // ── State ─────────────────────────────────────────────────────

////    public enum CastleTab { None, Expand, Cannon, Archer }
////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        Wire(expandButton, OnExpandClicked);
////        Wire(cannonButton, OnCannonClicked);
////        Wire(archerButton, OnArcherClicked);
////    }

////    private void OnEnable()
////    {
////        // Default to Expand when the Castle Panel first opens.
////        ActivateTab(CastleTab.Expand);
////    }

////    // ── Button handlers ───────────────────────────────────────────

////    private void OnExpandClicked() => ActivateTab(CastleTab.Expand);
////    private void OnCannonClicked() => ActivateTab(CastleTab.Cannon);
////    private void OnArcherClicked() => ActivateTab(CastleTab.Archer);

////    // ── Core logic ────────────────────────────────────────────────

////    /// <summary>
////    /// Switch to the requested tab and update all slot visibility accordingly.
////    /// Safe to call with the currently-active tab (no-op visually).
////    /// </summary>
////    public void ActivateTab(CastleTab tab)
////    {
////        ActiveTab = tab;

////        // ── Expansion slots (castle-block overlays) ────────────────
////        bool showExpand = (tab == CastleTab.Expand);
////        if (CastleGrid.Instance != null)
////            CastleGrid.Instance.SetExpansionSlotsVisible(showExpand);

////        // ── Cannon slot container ──────────────────────────────────
////        if (cannonSlotsRoot != null)
////            cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);

////        // ── Archer slot container ──────────────────────────────────
////        if (archerSlotsRoot != null)
////            archerSlotsRoot.SetActive(tab == CastleTab.Archer);

////        // ── Button tint feedback ───────────────────────────────────
////        TintButton(expandButton, tab == CastleTab.Expand);
////        TintButton(cannonButton, tab == CastleTab.Cannon);
////        TintButton(archerButton, tab == CastleTab.Archer);

////        Debug.Log($"[CastleTabController] Active tab → {tab}");
////    }

////    // ── Helpers ───────────────────────────────────────────────────

////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
////    {
////        if (btn != null)
////            btn.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
////    }

////    private void TintButton(Button btn, bool selected)
////    {
////        if (btn == null) return;
////        var img = btn.GetComponent<Image>();
////        if (img != null)
////            img.color = selected ? selectedColor : deselectedColor;
////    }
////}

////using UnityEngine;
////using UnityEngine.UI;
////using System.Collections.Generic;

/////// <summary>
/////// CastleTabController
///////
/////// Sits on the Castle Panel root (alongside CastlePanel).
/////// Manages the three sub-mode buttons that live inside the Castle Panel:
///////
///////   [Expand]  → shows ExpansionSlots so the player can add new castle blocks.
///////   [Cannon]  → hides expansion slots; reveals CannonSlots so the player can
///////               open the Cannon Panel and equip cannons.
///////   [Archer]  → hides expansion slots; reveals ArcherSlots so the player can
///////               drag soldiers onto archer positions.
///////
/////// ── Inspector wiring ───────────────────────────────────────────────────────
///////   expandButton   → Button labelled "Expand"
///////   cannonButton   → Button labelled "Cannon"
///////   archerButton   → Button labelled "Archer"
///////
///////   cannonSlotsRoot → Parent GameObject that holds all CannonSlot GameObjects
///////                     (show/hide the whole group instead of individual slots).
///////   archerSlotsRoot → Parent GameObject that holds all ArcherSlot GameObjects.
///////
///////   Selected / normal colors let you visually highlight the active tab.
///////
/////// ── How it connects to CastleGrid ──────────────────────────────────────────
///////   SetExpansionSlotsVisible(true/false) on CastleGrid is called so expansion
///////   overlays appear only in Expand mode.
/////// </summary>
////public class CastleTabController : MonoBehaviour
////{
////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button expandButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button archerButton;

////    [Header("Slot Containers")]
////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
////    [SerializeField] private GameObject cannonSlotsRoot;

////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
////    [SerializeField] private GameObject archerSlotsRoot;

////    [Header("Tab Highlight Colors (optional)")]
////    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
////    [SerializeField] private Color deselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

////    // ── State ─────────────────────────────────────────────────────

////    public enum CastleTab { None, Expand, Cannon, Archer }
////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        Wire(expandButton, OnExpandClicked);
////        Wire(cannonButton, OnCannonClicked);
////        Wire(archerButton, OnArcherClicked);
////    }

////    private void OnEnable()
////    {
////        // Default to Expand when the Castle Panel first opens.
////        ActivateTab(CastleTab.Expand);
////    }

////    // ── Button handlers ───────────────────────────────────────────

////    private void OnExpandClicked() => ActivateTab(CastleTab.Expand);
////    private void OnCannonClicked() => ActivateTab(CastleTab.Cannon);
////    private void OnArcherClicked() => ActivateTab(CastleTab.Archer);

////    // ── Core logic ────────────────────────────────────────────────

////    /// <summary>
////    /// Switch to the requested tab and update all slot visibility accordingly.
////    /// Safe to call with the currently-active tab (no-op visually).
////    /// </summary>
////    public void ActivateTab(CastleTab tab)
////    {
////        ActiveTab = tab;

////        // ── Expansion slots (castle-block overlays) ────────────────
////        bool showExpand = (tab == CastleTab.Expand);
////        if (CastleGrid.Instance != null)
////            CastleGrid.Instance.SetExpansionSlotsVisible(showExpand);

////        // ── Cannon slot container ──────────────────────────────────
////        if (cannonSlotsRoot != null)
////            cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);

////        // ── Archer slot container ──────────────────────────────────
////        if (archerSlotsRoot != null)
////            archerSlotsRoot.SetActive(tab == CastleTab.Archer);

////        // ── Button tint feedback ───────────────────────────────────
////        TintButton(expandButton, tab == CastleTab.Expand);
////        TintButton(cannonButton, tab == CastleTab.Cannon);
////        TintButton(archerButton, tab == CastleTab.Archer);

////        Debug.Log($"[CastleTabController] Active tab → {tab}");
////    }

////    // ── Helpers ───────────────────────────────────────────────────

////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
////    {
////        if (btn != null)
////            btn.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
////    }

////    private void TintButton(Button btn, bool selected)
////    {
////        if (btn == null) return;
////        var img = btn.GetComponent<Image>();
////        if (img != null)
////            img.color = selected ? selectedColor : deselectedColor;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// CastleTabController
///// Manages the three sub-mode buttons inside the Castle Panel:
/////   [Expand] [Cannon] [Archer]
///// </summary>
//public class CastleTabController : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────
//    public static CastleTabController Instance { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────

//    [Header("Tab Buttons")]
//    [SerializeField] private Button expandButton;
//    [SerializeField] private Button cannonButton;
//    [SerializeField] private Button archerButton;

//    [Header("Selected Indicator GameObjects")]
//    [Tooltip("Child GameObject inside the Expand button that marks it as selected (e.g. an underline or highlight sprite).")]
//    [SerializeField] private GameObject expandSelectedObject;

//    [Tooltip("Child GameObject inside the Cannon button that marks it as selected.")]
//    [SerializeField] private GameObject cannonSelectedObject;

//    [Tooltip("Child GameObject inside the Archer button that marks it as selected.")]
//    [SerializeField] private GameObject archerSelectedObject;

//    [Header("Slot Containers")]
//    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
//    [SerializeField] private GameObject cannonSlotsRoot;

//    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
//    [SerializeField] private GameObject archerSlotsRoot;

//    // ── State ─────────────────────────────────────────────────────

//    public enum CastleTab { None, Expand, Cannon, Archer }
//    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        Instance = this;
//        Wire(expandButton, OnExpandClicked);
//        Wire(cannonButton, OnCannonClicked);
//        Wire(archerButton, OnArcherClicked);
//    }

//    private void OnEnable()
//    {
//        CastleTab tabToRestore = (ActiveTab == CastleTab.None) ? CastleTab.Expand : ActiveTab;
//        ApplyTabVisuals(tabToRestore);
//        ActiveTab = tabToRestore;
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this) Instance = null;
//    }

//    // ── Button handlers ───────────────────────────────────────────

//    private void OnExpandClicked()
//    {
//        ActivateTab(CastleTab.Expand);
//        Deselect();
//    }

//    private void OnCannonClicked()
//    {
//        ActivateTab(CastleTab.Cannon);
//        Deselect();
//    }

//    private void OnArcherClicked()
//    {
//        ActivateTab(CastleTab.Archer);
//        Deselect();
//    }

//    // ── Core logic ────────────────────────────────────────────────

//    public void ActivateTab(CastleTab tab)
//    {
//        ActiveTab = tab;
//        ApplyTabVisuals(tab);
//        Debug.Log($"[CastleTabController] Active tab → {tab}");
//    }

//    // ── Internal ──────────────────────────────────────────────────

//    private void ApplyTabVisuals(CastleTab tab)
//    {
//        // ── Slot containers ────────────────────────────────────────
//        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);
//        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

//        // ── Selected indicator GameObjects ─────────────────────────
//        // Each button has a dedicated child GameObject (e.g. an underline or
//        // highlight sprite) that is simply activated for the active tab and
//        // deactivated for the rest — no color manipulation involved.
//        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
//        SetSelected(cannonSelectedObject, tab == CastleTab.Cannon);
//        SetSelected(archerSelectedObject, tab == CastleTab.Archer);
//    }

//    /// <summary>
//    /// Activates or deactivates the indicator GameObject for a single button.
//    /// </summary>
//    private static void SetSelected(GameObject indicator, bool selected)
//    {
//        if (indicator != null)
//            indicator.SetActive(selected);
//    }

//    /// <summary>
//    /// Clears Unity's EventSystem selection so the button doesn't stay
//    /// highlighted in its Selected color state after being clicked.
//    /// </summary>
//    private static void Deselect()
//    {
//        if (EventSystem.current != null)
//            EventSystem.current.SetSelectedGameObject(null);
//    }

//    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
//    {
//        if (btn != null)
//            btn.onClick.AddListener(action);
//        else
//            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;

///// <summary>
///// CastleTabController
/////
///// Sits on the Castle Panel root (alongside CastlePanel).
///// Manages the three sub-mode buttons that live inside the Castle Panel:
/////
/////   [Expand]  → shows ExpansionSlots so the player can add new castle blocks.
/////   [Cannon]  → hides expansion slots; reveals CannonSlots so the player can
/////               open the Cannon Panel and equip cannons.
/////   [Archer]  → hides expansion slots; reveals ArcherSlots so the player can
/////               drag soldiers onto archer positions.
/////
///// ── Inspector wiring ───────────────────────────────────────────────────────
/////   expandButton   → Button labelled "Expand"
/////   cannonButton   → Button labelled "Cannon"
/////   archerButton   → Button labelled "Archer"
/////
/////   cannonSlotsRoot → Parent GameObject that holds all CannonSlot GameObjects
/////                     (show/hide the whole group instead of individual slots).
/////   archerSlotsRoot → Parent GameObject that holds all ArcherSlot GameObjects.
/////
/////   Selected / normal colors let you visually highlight the active tab.
/////
///// ── How it connects to CastleGrid ──────────────────────────────────────────
/////   SetExpansionSlotsVisible(true/false) on CastleGrid is called so expansion
/////   overlays appear only in Expand mode.
///// </summary>
//public class CastleTabController : MonoBehaviour
//{
//    // ── Inspector ─────────────────────────────────────────────────

//    [Header("Tab Buttons")]
//    [SerializeField] private Button expandButton;
//    [SerializeField] private Button cannonButton;
//    [SerializeField] private Button archerButton;

//    [Header("Slot Containers")]
//    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
//    [SerializeField] private GameObject cannonSlotsRoot;

//    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
//    [SerializeField] private GameObject archerSlotsRoot;

//    [Header("Tab Highlight Colors (optional)")]
//    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
//    [SerializeField] private Color deselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

//    // ── State ─────────────────────────────────────────────────────

//    public enum CastleTab { None, Expand, Cannon, Archer }
//    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        Wire(expandButton, OnExpandClicked);
//        Wire(cannonButton, OnCannonClicked);
//        Wire(archerButton, OnArcherClicked);
//    }

//    private void OnEnable()
//    {
//        // Default to Expand when the Castle Panel first opens.
//        ActivateTab(CastleTab.Expand);
//    }

//    // ── Button handlers ───────────────────────────────────────────

//    private void OnExpandClicked() => ActivateTab(CastleTab.Expand);
//    private void OnCannonClicked() => ActivateTab(CastleTab.Cannon);
//    private void OnArcherClicked() => ActivateTab(CastleTab.Archer);

//    // ── Core logic ────────────────────────────────────────────────

//    /// <summary>
//    /// Switch to the requested tab and update all slot visibility accordingly.
//    /// Safe to call with the currently-active tab (no-op visually).
//    /// </summary>
//    public void ActivateTab(CastleTab tab)
//    {
//        ActiveTab = tab;

//        // ── Expansion slots (castle-block overlays) ────────────────
//        bool showExpand = (tab == CastleTab.Expand);
//        if (CastleGrid.Instance != null)
//            CastleGrid.Instance.SetExpansionSlotsVisible(showExpand);

//        // ── Cannon slot container ──────────────────────────────────
//        if (cannonSlotsRoot != null)
//            cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);

//        // ── Archer slot container ──────────────────────────────────
//        if (archerSlotsRoot != null)
//            archerSlotsRoot.SetActive(tab == CastleTab.Archer);

//        // ── Button tint feedback ───────────────────────────────────
//        TintButton(expandButton, tab == CastleTab.Expand);
//        TintButton(cannonButton, tab == CastleTab.Cannon);
//        TintButton(archerButton, tab == CastleTab.Archer);

//        Debug.Log($"[CastleTabController] Active tab → {tab}");
//    }

//    // ── Helpers ───────────────────────────────────────────────────

//    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
//    {
//        if (btn != null)
//            btn.onClick.AddListener(action);
//        else
//            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
//    }

//    private void TintButton(Button btn, bool selected)
//    {
//        if (btn == null) return;
//        var img = btn.GetComponent<Image>();
//        if (img != null)
//            img.color = selected ? selectedColor : deselectedColor;
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;

///// <summary>
///// CastleTabController
/////
///// Sits on the Castle Panel root (alongside CastlePanel).
///// Manages the three sub-mode buttons that live inside the Castle Panel:
/////
/////   [Expand]  → shows ExpansionSlots so the player can add new castle blocks.
/////   [Cannon]  → hides expansion slots; reveals CannonSlots so the player can
/////               open the Cannon Panel and equip cannons.
/////   [Archer]  → hides expansion slots; reveals ArcherSlots so the player can
/////               drag soldiers onto archer positions.
/////
///// ── Inspector wiring ───────────────────────────────────────────────────────
/////   expandButton   → Button labelled "Expand"
/////   cannonButton   → Button labelled "Cannon"
/////   archerButton   → Button labelled "Archer"
/////
/////   cannonSlotsRoot → Parent GameObject that holds all CannonSlot GameObjects
/////                     (show/hide the whole group instead of individual slots).
/////   archerSlotsRoot → Parent GameObject that holds all ArcherSlot GameObjects.
/////
/////   Selected / normal colors let you visually highlight the active tab.
/////
///// ── How it connects to CastleGrid ──────────────────────────────────────────
/////   SetExpansionSlotsVisible(true/false) on CastleGrid is called so expansion
/////   overlays appear only in Expand mode.
///// </summary>
//public class CastleTabController : MonoBehaviour
//{
//    // ── Inspector ─────────────────────────────────────────────────

//    [Header("Tab Buttons")]
//    [SerializeField] private Button expandButton;
//    [SerializeField] private Button cannonButton;
//    [SerializeField] private Button archerButton;

//    [Header("Slot Containers")]
//    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
//    [SerializeField] private GameObject cannonSlotsRoot;

//    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
//    [SerializeField] private GameObject archerSlotsRoot;

//    [Header("Tab Highlight Colors (optional)")]
//    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
//    [SerializeField] private Color deselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

//    // ── State ─────────────────────────────────────────────────────

//    public enum CastleTab { None, Expand, Cannon, Archer }
//    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        Wire(expandButton, OnExpandClicked);
//        Wire(cannonButton, OnCannonClicked);
//        Wire(archerButton, OnArcherClicked);
//    }

//    private void OnEnable()
//    {
//        // Default to Expand when the Castle Panel first opens.
//        ActivateTab(CastleTab.Expand);
//    }

//    // ── Button handlers ───────────────────────────────────────────

//    private void OnExpandClicked() => ActivateTab(CastleTab.Expand);
//    private void OnCannonClicked() => ActivateTab(CastleTab.Cannon);
//    private void OnArcherClicked() => ActivateTab(CastleTab.Archer);

//    // ── Core logic ────────────────────────────────────────────────

//    /// <summary>
//    /// Switch to the requested tab and update all slot visibility accordingly.
//    /// Safe to call with the currently-active tab (no-op visually).
//    /// </summary>
//    public void ActivateTab(CastleTab tab)
//    {
//        ActiveTab = tab;

//        // ── Expansion slots (castle-block overlays) ────────────────
//        bool showExpand = (tab == CastleTab.Expand);
//        if (CastleGrid.Instance != null)
//            CastleGrid.Instance.SetExpansionSlotsVisible(showExpand);

//        // ── Cannon slot container ──────────────────────────────────
//        if (cannonSlotsRoot != null)
//            cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);

//        // ── Archer slot container ──────────────────────────────────
//        if (archerSlotsRoot != null)
//            archerSlotsRoot.SetActive(tab == CastleTab.Archer);

//        // ── Button tint feedback ───────────────────────────────────
//        TintButton(expandButton, tab == CastleTab.Expand);
//        TintButton(cannonButton, tab == CastleTab.Cannon);
//        TintButton(archerButton, tab == CastleTab.Archer);

//        Debug.Log($"[CastleTabController] Active tab → {tab}");
//    }

//    // ── Helpers ───────────────────────────────────────────────────

//    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
//    {
//        if (btn != null)
//            btn.onClick.AddListener(action);
//        else
//            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
//    }

//    private void TintButton(Button btn, bool selected)
//    {
//        if (btn == null) return;
//        var img = btn.GetComponent<Image>();
//        if (img != null)
//            img.color = selected ? selectedColor : deselectedColor;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CastleTabController
/// Manages the three sub-mode buttons inside the Castle Panel:
///   [Expand] [Cannon] [Archer]
/// </summary>
public class CastleTabController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static CastleTabController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────

    [Header("Tab Buttons")]
    [SerializeField] private Button expandButton;
    [SerializeField] private Button cannonButton;
    [SerializeField] private Button archerButton;

    [Header("Selected Indicator GameObjects")]
    [Tooltip("Child GameObject inside the Expand button that marks it as selected (e.g. an underline or highlight sprite).")]
    [SerializeField] private GameObject expandSelectedObject;

    [Tooltip("Child GameObject inside the Cannon button that marks it as selected.")]
    [SerializeField] private GameObject cannonSelectedObject;

    [Tooltip("Child GameObject inside the Archer button that marks it as selected.")]
    [SerializeField] private GameObject archerSelectedObject;

    [Header("Slot Containers")]
    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
    [SerializeField] private GameObject cannonSlotsRoot;

    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
    [SerializeField] private GameObject archerSlotsRoot;

    // ── State ─────────────────────────────────────────────────────

    public enum CastleTab { None, Expand, Cannon, Archer }
    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        Wire(expandButton, OnExpandClicked);
        Wire(cannonButton, OnCannonClicked);
        Wire(archerButton, OnArcherClicked);
    }

    private void OnEnable()
    {
        CastleTab tabToRestore = (ActiveTab == CastleTab.None) ? CastleTab.Expand : ActiveTab;
        ApplyTabVisuals(tabToRestore);
        ActiveTab = tabToRestore;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Button handlers ───────────────────────────────────────────

    private void OnExpandClicked()
    {
        ActivateTab(CastleTab.Expand);
        Deselect();
    }

    private void OnCannonClicked()
    {
        ActivateTab(CastleTab.Cannon);
        Deselect();
    }

    private void OnArcherClicked()
    {
        ActivateTab(CastleTab.Archer);
        Deselect();
    }

    // ── Core logic ────────────────────────────────────────────────

    public void ActivateTab(CastleTab tab)
    {
        ActiveTab = tab;
        ApplyTabVisuals(tab);
        Debug.Log($"[CastleTabController] Active tab → {tab}");
    }

    // ── Internal ──────────────────────────────────────────────────

    private void ApplyTabVisuals(CastleTab tab)
    {
        // ── Expansion slots (castle-block overlays on the grid) ────
        // Only visible in Expand mode; hidden for Cannon and Archer tabs.
        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

        // ── Slot containers ────────────────────────────────────────
        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(tab == CastleTab.Cannon);
        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

        // ── Selected indicator GameObjects ─────────────────────────
        // Each button has a dedicated child GameObject (e.g. an underline or
        // highlight sprite) that is simply activated for the active tab and
        // deactivated for the rest — no color manipulation involved.
        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
        SetSelected(cannonSelectedObject, tab == CastleTab.Cannon);
        SetSelected(archerSelectedObject, tab == CastleTab.Archer);
    }

    /// <summary>
    /// Activates or deactivates the indicator GameObject for a single button.
    /// </summary>
    private static void SetSelected(GameObject indicator, bool selected)
    {
        if (indicator != null)
            indicator.SetActive(selected);
    }

    /// <summary>
    /// Clears Unity's EventSystem selection so the button doesn't stay
    /// highlighted in its Selected color state after being clicked.
    /// </summary>
    private static void Deselect()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
            btn.onClick.AddListener(action);
        else
            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
    }
}