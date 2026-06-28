//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// CastleTabController
///////// Manages the three sub-mode buttons inside the Castle Panel:
/////////   [Expand] [Cannon] [Archer]
///////// </summary>
//////public class CastleTabController : MonoBehaviour
//////{
//////    // ── Singleton ─────────────────────────────────────────────────
//////    public static CastleTabController Instance { get; private set; }

//////    // ── Inspector ─────────────────────────────────────────────────

//////    [Header("Tab Buttons")]
//////    [SerializeField] private Button expandButton;
//////    [SerializeField] private Button cannonButton;
//////    [SerializeField] private Button archerButton;

//////    [Header("Selected Indicator GameObjects")]
//////    [Tooltip("Child GameObject inside the Expand button that marks it as selected (e.g. an underline or highlight sprite).")]
//////    [SerializeField] private GameObject expandSelectedObject;

//////    [Tooltip("Child GameObject inside the Cannon button that marks it as selected.")]
//////    [SerializeField] private GameObject cannonSelectedObject;

//////    [Tooltip("Child GameObject inside the Archer button that marks it as selected.")]
//////    [SerializeField] private GameObject archerSelectedObject;

//////    [Header("Slot Containers")]
//////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
//////    [SerializeField] private GameObject cannonSlotsRoot;

//////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
//////    [SerializeField] private GameObject archerSlotsRoot;

//////    // ── State ─────────────────────────────────────────────────────

//////    public enum CastleTab { None, Expand, Cannon, Archer }
//////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        Instance = this;
//////        Wire(expandButton, OnExpandClicked);
//////        Wire(cannonButton, OnCannonClicked);
//////        Wire(archerButton, OnArcherClicked);
//////    }

//////    private void OnEnable()
//////    {
//////        CastleTab tabToRestore = (ActiveTab == CastleTab.None) ? CastleTab.Expand : ActiveTab;
//////        ApplyTabVisuals(tabToRestore);
//////        ActiveTab = tabToRestore;
//////    }

//////    private void OnDestroy()
//////    {
//////        if (Instance == this) Instance = null;
//////    }

//////    // ── Button handlers ───────────────────────────────────────────

//////    private void OnExpandClicked()
//////    {
//////        ActivateTab(CastleTab.Expand);
//////        Deselect();
//////    }

//////    private void OnCannonClicked()
//////    {
//////        ActivateTab(CastleTab.Cannon);
//////        Deselect();
//////    }

//////    private void OnArcherClicked()
//////    {
//////        ActivateTab(CastleTab.Archer);
//////        Deselect();
//////    }

//////    // ── Core logic ────────────────────────────────────────────────

//////    public void ActivateTab(CastleTab tab)
//////    {
//////        ActiveTab = tab;
//////        ApplyTabVisuals(tab);
//////        Debug.Log($"[CastleTabController] Active tab → {tab}");
//////    }

//////    // ── Internal ──────────────────────────────────────────────────

//////    private void ApplyTabVisuals(CastleTab tab)
//////    {
//////        bool isCannonTab = tab == CastleTab.Cannon;

//////        // ── Expansion slots (castle-block overlays on the grid) ────
//////        // Only visible in Expand mode; hidden for Cannon and Archer tabs.
//////        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

//////        // ── Cannon drop zones ──────────────────────────────────────
//////        // Activate zone overlays (EmptySlotZone + interactability) ONLY in
//////        // Cannon tab. In every other tab/panel they must be fully hidden.
//////        foreach (var slot in FindObjectsOfType<CastleBlockUnitSlot>(includeInactive: false))
//////            slot.SetCannonTabActive(isCannonTab);

//////        // Also cover the static path used by GameManager / CannonPanelManager.
//////        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);

//////        // ── Slot containers ────────────────────────────────────────
//////        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
//////        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

//////        // ── Selected indicator GameObjects ─────────────────────────
//////        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
//////        SetSelected(cannonSelectedObject, isCannonTab);
//////        SetSelected(archerSelectedObject, tab == CastleTab.Archer);
//////    }

//////    /// <summary>
//////    /// Activates or deactivates the indicator GameObject for a single button.
//////    /// </summary>
//////    private static void SetSelected(GameObject indicator, bool selected)
//////    {
//////        if (indicator != null)
//////            indicator.SetActive(selected);
//////    }

//////    /// <summary>
//////    /// Clears Unity's EventSystem selection so the button doesn't stay
//////    /// highlighted in its Selected color state after being clicked.
//////    /// </summary>
//////    private static void Deselect()
//////    {
//////        if (EventSystem.current != null)
//////            EventSystem.current.SetSelectedGameObject(null);
//////    }

//////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
//////    {
//////        if (btn != null)
//////            btn.onClick.AddListener(action);
//////        else
//////            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// CastleTabController
/////// Manages the three sub-mode buttons inside the Castle Panel:
///////   [Expand]  [Cannon]  [Archer]
/////// </summary>
////public class CastleTabController : MonoBehaviour
////{
////    // ── Singleton ─────────────────────────────────────────────────
////    public static CastleTabController Instance { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button expandButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button archerButton;

////    [Header("Selected Indicator GameObjects")]
////    [SerializeField] private GameObject expandSelectedObject;
////    [SerializeField] private GameObject cannonSelectedObject;
////    [SerializeField] private GameObject archerSelectedObject;

////    [Header("Slot Containers")]
////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
////    [SerializeField] private GameObject cannonSlotsRoot;

////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
////    [SerializeField] private GameObject archerSlotsRoot;

////    // ── State ─────────────────────────────────────────────────────

////    public enum CastleTab { None, Expand, Cannon, Archer }
////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        Instance = this;
////        Wire(expandButton, OnExpandClicked);
////        Wire(cannonButton, OnCannonClicked);
////        Wire(archerButton, OnArcherClicked);
////    }

////    private void OnEnable()
////    {
////        CastleTab tab = ActiveTab == CastleTab.None ? CastleTab.Expand : ActiveTab;
////        ApplyTabVisuals(tab);
////        ActiveTab = tab;
////    }

////    private void OnDestroy()
////    {
////        if (Instance == this) Instance = null;
////    }

////    // ── Button handlers ───────────────────────────────────────────

////    private void OnExpandClicked() { ActivateTab(CastleTab.Expand); Deselect(); }
////    private void OnCannonClicked() { ActivateTab(CastleTab.Cannon); Deselect(); }
////    private void OnArcherClicked() { ActivateTab(CastleTab.Archer); Deselect(); }

////    // ── Core logic ────────────────────────────────────────────────

////    public void ActivateTab(CastleTab tab)
////    {
////        ActiveTab = tab;
////        ApplyTabVisuals(tab);
////        Debug.Log($"[CastleTabController] Active tab → {tab}");
////    }

////    // ── Internal ──────────────────────────────────────────────────

////    private void ApplyTabVisuals(CastleTab tab)
////    {
////        bool isCannonTab = tab == CastleTab.Cannon;
////        bool isArcherTab = tab == CastleTab.Archer;

////        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

////        foreach (var slot in FindObjectsByType<CastleBlockUnitSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
////        {
////            slot.SetCannonTabActive(isCannonTab);
////            slot.SetArcherTabActive(isArcherTab);
////        }

////        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);
////        ArcherZoneCastle.SetArcherZonesVisible(isArcherTab);

////        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
////        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(isArcherTab);

////        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
////        SetSelected(cannonSelectedObject, isCannonTab);
////        SetSelected(archerSelectedObject, isArcherTab);
////    }

////    private static void SetSelected(GameObject indicator, bool selected)
////    {
////        if (indicator != null) indicator.SetActive(selected);
////    }

////    private static void Deselect()
////    {
////        if (EventSystem.current != null)
////            EventSystem.current.SetSelectedGameObject(null);
////    }

////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
////    {
////        if (btn != null)
////            btn.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[CastleTabController] A tab button reference is null.");
////    }
////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// CastleTabController
/////// Manages the three sub-mode buttons inside the Castle Panel:
///////   [Expand] [Cannon] [Archer]
/////// </summary>
////public class CastleTabController : MonoBehaviour
////{
////    // ── Singleton ─────────────────────────────────────────────────
////    public static CastleTabController Instance { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button expandButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button archerButton;

////    [Header("Selected Indicator GameObjects")]
////    [Tooltip("Child GameObject inside the Expand button that marks it as selected (e.g. an underline or highlight sprite).")]
////    [SerializeField] private GameObject expandSelectedObject;

////    [Tooltip("Child GameObject inside the Cannon button that marks it as selected.")]
////    [SerializeField] private GameObject cannonSelectedObject;

////    [Tooltip("Child GameObject inside the Archer button that marks it as selected.")]
////    [SerializeField] private GameObject archerSelectedObject;

////    [Header("Slot Containers")]
////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
////    [SerializeField] private GameObject cannonSlotsRoot;

////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
////    [SerializeField] private GameObject archerSlotsRoot;

////    // ── State ─────────────────────────────────────────────────────

////    public enum CastleTab { None, Expand, Cannon, Archer }
////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        Instance = this;
////        Wire(expandButton, OnExpandClicked);
////        Wire(cannonButton, OnCannonClicked);
////        Wire(archerButton, OnArcherClicked);
////    }

////    private void OnEnable()
////    {
////        CastleTab tabToRestore = (ActiveTab == CastleTab.None) ? CastleTab.Expand : ActiveTab;
////        ApplyTabVisuals(tabToRestore);
////        ActiveTab = tabToRestore;
////    }

////    private void OnDestroy()
////    {
////        if (Instance == this) Instance = null;
////    }

////    // ── Button handlers ───────────────────────────────────────────

////    private void OnExpandClicked()
////    {
////        ActivateTab(CastleTab.Expand);
////        Deselect();
////    }

////    private void OnCannonClicked()
////    {
////        ActivateTab(CastleTab.Cannon);
////        Deselect();
////    }

////    private void OnArcherClicked()
////    {
////        ActivateTab(CastleTab.Archer);
////        Deselect();
////    }

////    // ── Core logic ────────────────────────────────────────────────

////    public void ActivateTab(CastleTab tab)
////    {
////        ActiveTab = tab;
////        ApplyTabVisuals(tab);
////        Debug.Log($"[CastleTabController] Active tab → {tab}");
////    }

////    // ── Internal ──────────────────────────────────────────────────

////    private void ApplyTabVisuals(CastleTab tab)
////    {
////        bool isCannonTab = tab == CastleTab.Cannon;

////        // ── Expansion slots (castle-block overlays on the grid) ────
////        // Only visible in Expand mode; hidden for Cannon and Archer tabs.
////        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

////        // ── Cannon drop zones ──────────────────────────────────────
////        // Activate zone overlays (EmptySlotZone + interactability) ONLY in
////        // Cannon tab. In every other tab/panel they must be fully hidden.
////        foreach (var slot in FindObjectsOfType<CastleBlockUnitSlot>(includeInactive: false))
////            slot.SetCannonTabActive(isCannonTab);

////        // Also cover the static path used by GameManager / CannonPanelManager.
////        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);

////        // ── Slot containers ────────────────────────────────────────
////        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
////        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

////        // ── Selected indicator GameObjects ─────────────────────────
////        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
////        SetSelected(cannonSelectedObject, isCannonTab);
////        SetSelected(archerSelectedObject, tab == CastleTab.Archer);
////    }

////    /// <summary>
////    /// Activates or deactivates the indicator GameObject for a single button.
////    /// </summary>
////    private static void SetSelected(GameObject indicator, bool selected)
////    {
////        if (indicator != null)
////            indicator.SetActive(selected);
////    }

////    /// <summary>
////    /// Clears Unity's EventSystem selection so the button doesn't stay
////    /// highlighted in its Selected color state after being clicked.
////    /// </summary>
////    private static void Deselect()
////    {
////        if (EventSystem.current != null)
////            EventSystem.current.SetSelectedGameObject(null);
////    }

////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
////    {
////        if (btn != null)
////            btn.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// CastleTabController
///// Manages the three sub-mode buttons inside the Castle Panel:
/////   [Expand]  [Cannon]  [Archer]
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
//    [SerializeField] private GameObject expandSelectedObject;
//    [SerializeField] private GameObject cannonSelectedObject;
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
//        // Always start with all zones hidden; ApplyTabVisuals will re-enable
//        // only the ones that belong to the active tab.
//        CastleUnitDropZone.SetCannonZonesVisible(false);
//        ArcherZoneCastle.SetArcherZonesVisible(false);

//        CastleTab tab = ActiveTab == CastleTab.None ? CastleTab.Expand : ActiveTab;
//        ApplyTabVisuals(tab);
//        ActiveTab = tab;
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this) Instance = null;
//    }

//    // ── Button handlers ───────────────────────────────────────────

//    private void OnExpandClicked() { ActivateTab(CastleTab.Expand); Deselect(); }
//    private void OnCannonClicked() { ActivateTab(CastleTab.Cannon); Deselect(); }
//    private void OnArcherClicked() { ActivateTab(CastleTab.Archer); Deselect(); }

//    // ── Core logic ────────────────────────────────────────────────

//    public void ActivateTab(CastleTab tab)
//    {
//        ActiveTab = tab;
//        ApplyTabVisuals(tab);
//        Debug.Log($"[CastleTabController] Active tab → {tab}");
//    }

//    /// <summary>
//    /// Resets the active tab back to None so the next OnEnable
//    /// always opens on the Expand tab. Call this before showing the Castle panel.
//    /// </summary>
//    public void ResetTab()
//    {
//        ActiveTab = CastleTab.None;
//    }

//    // ── Internal ──────────────────────────────────────────────────

//    private void ApplyTabVisuals(CastleTab tab)
//    {
//        bool isCannonTab = tab == CastleTab.Cannon;
//        bool isArcherTab = tab == CastleTab.Archer;

//        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

//        foreach (var slot in FindObjectsByType<CastleBlockUnitSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
//        {
//            slot.SetCannonTabActive(isCannonTab);
//            slot.SetArcherTabActive(isArcherTab);
//        }

//        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);
//        ArcherZoneCastle.SetArcherZonesVisible(isArcherTab);

//        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
//        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(isArcherTab);

//        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
//        SetSelected(cannonSelectedObject, isCannonTab);
//        SetSelected(archerSelectedObject, isArcherTab);
//    }

//    private static void SetSelected(GameObject indicator, bool selected)
//    {
//        if (indicator != null) indicator.SetActive(selected);
//    }

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
//            Debug.LogWarning("[CastleTabController] A tab button reference is null.");
//    }
//}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// CastleTabController
/////// Manages the three sub-mode buttons inside the Castle Panel:
///////   [Expand] [Cannon] [Archer]
/////// </summary>
////public class CastleTabController : MonoBehaviour
////{
////    // ── Singleton ─────────────────────────────────────────────────
////    public static CastleTabController Instance { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button expandButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button archerButton;

////    [Header("Selected Indicator GameObjects")]
////    [Tooltip("Child GameObject inside the Expand button that marks it as selected (e.g. an underline or highlight sprite).")]
////    [SerializeField] private GameObject expandSelectedObject;

////    [Tooltip("Child GameObject inside the Cannon button that marks it as selected.")]
////    [SerializeField] private GameObject cannonSelectedObject;

////    [Tooltip("Child GameObject inside the Archer button that marks it as selected.")]
////    [SerializeField] private GameObject archerSelectedObject;

////    [Header("Slot Containers")]
////    [Tooltip("Parent that contains all CannonSlot GameObjects. Shown only in Cannon mode.")]
////    [SerializeField] private GameObject cannonSlotsRoot;

////    [Tooltip("Parent that contains all ArcherSlot GameObjects. Shown only in Archer mode.")]
////    [SerializeField] private GameObject archerSlotsRoot;

////    // ── State ─────────────────────────────────────────────────────

////    public enum CastleTab { None, Expand, Cannon, Archer }
////    public CastleTab ActiveTab { get; private set; } = CastleTab.None;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        Instance = this;
////        Wire(expandButton, OnExpandClicked);
////        Wire(cannonButton, OnCannonClicked);
////        Wire(archerButton, OnArcherClicked);
////    }

////    private void OnEnable()
////    {
////        CastleTab tabToRestore = (ActiveTab == CastleTab.None) ? CastleTab.Expand : ActiveTab;
////        ApplyTabVisuals(tabToRestore);
////        ActiveTab = tabToRestore;
////    }

////    private void OnDestroy()
////    {
////        if (Instance == this) Instance = null;
////    }

////    // ── Button handlers ───────────────────────────────────────────

////    private void OnExpandClicked()
////    {
////        ActivateTab(CastleTab.Expand);
////        Deselect();
////    }

////    private void OnCannonClicked()
////    {
////        ActivateTab(CastleTab.Cannon);
////        Deselect();
////    }

////    private void OnArcherClicked()
////    {
////        ActivateTab(CastleTab.Archer);
////        Deselect();
////    }

////    // ── Core logic ────────────────────────────────────────────────

////    public void ActivateTab(CastleTab tab)
////    {
////        ActiveTab = tab;
////        ApplyTabVisuals(tab);
////        Debug.Log($"[CastleTabController] Active tab → {tab}");
////    }

////    // ── Internal ──────────────────────────────────────────────────

////    private void ApplyTabVisuals(CastleTab tab)
////    {
////        bool isCannonTab = tab == CastleTab.Cannon;

////        // ── Expansion slots (castle-block overlays on the grid) ────
////        // Only visible in Expand mode; hidden for Cannon and Archer tabs.
////        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

////        // ── Cannon drop zones ──────────────────────────────────────
////        // Activate zone overlays (EmptySlotZone + interactability) ONLY in
////        // Cannon tab. In every other tab/panel they must be fully hidden.
////        foreach (var slot in FindObjectsOfType<CastleBlockUnitSlot>(includeInactive: false))
////            slot.SetCannonTabActive(isCannonTab);

////        // Also cover the static path used by GameManager / CannonPanelManager.
////        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);

////        // ── Slot containers ────────────────────────────────────────
////        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
////        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

////        // ── Selected indicator GameObjects ─────────────────────────
////        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
////        SetSelected(cannonSelectedObject, isCannonTab);
////        SetSelected(archerSelectedObject, tab == CastleTab.Archer);
////    }

////    /// <summary>
////    /// Activates or deactivates the indicator GameObject for a single button.
////    /// </summary>
////    private static void SetSelected(GameObject indicator, bool selected)
////    {
////        if (indicator != null)
////            indicator.SetActive(selected);
////    }

////    /// <summary>
////    /// Clears Unity's EventSystem selection so the button doesn't stay
////    /// highlighted in its Selected color state after being clicked.
////    /// </summary>
////    private static void Deselect()
////    {
////        if (EventSystem.current != null)
////            EventSystem.current.SetSelectedGameObject(null);
////    }

////    private void Wire(Button btn, UnityEngine.Events.UnityAction action)
////    {
////        if (btn != null)
////            btn.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[CastleTabController] A tab button reference is null. Check Inspector.");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// CastleTabController
///// Manages the three sub-mode buttons inside the Castle Panel:
/////   [Expand]  [Cannon]  [Archer]
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
//    [SerializeField] private GameObject expandSelectedObject;
//    [SerializeField] private GameObject cannonSelectedObject;
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
//        CastleTab tab = ActiveTab == CastleTab.None ? CastleTab.Expand : ActiveTab;
//        ApplyTabVisuals(tab);
//        ActiveTab = tab;
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this) Instance = null;
//    }

//    // ── Button handlers ───────────────────────────────────────────

//    private void OnExpandClicked() { ActivateTab(CastleTab.Expand); Deselect(); }
//    private void OnCannonClicked() { ActivateTab(CastleTab.Cannon); Deselect(); }
//    private void OnArcherClicked() { ActivateTab(CastleTab.Archer); Deselect(); }

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
//        bool isCannonTab = tab == CastleTab.Cannon;
//        bool isArcherTab = tab == CastleTab.Archer;

//        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

//        foreach (var slot in FindObjectsByType<CastleBlockUnitSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
//        {
//            slot.SetCannonTabActive(isCannonTab);
//            slot.SetArcherTabActive(isArcherTab);
//        }

//        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);
//        ArcherZoneCastle.SetArcherZonesVisible(isArcherTab);

//        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
//        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(isArcherTab);

//        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
//        SetSelected(cannonSelectedObject, isCannonTab);
//        SetSelected(archerSelectedObject, isArcherTab);
//    }

//    private static void SetSelected(GameObject indicator, bool selected)
//    {
//        if (indicator != null) indicator.SetActive(selected);
//    }

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
//            Debug.LogWarning("[CastleTabController] A tab button reference is null.");
//    }
//}

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
//        bool isCannonTab = tab == CastleTab.Cannon;

//        // ── Expansion slots (castle-block overlays on the grid) ────
//        // Only visible in Expand mode; hidden for Cannon and Archer tabs.
//        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

//        // ── Cannon drop zones ──────────────────────────────────────
//        // Activate zone overlays (EmptySlotZone + interactability) ONLY in
//        // Cannon tab. In every other tab/panel they must be fully hidden.
//        foreach (var slot in FindObjectsOfType<CastleBlockUnitSlot>(includeInactive: false))
//            slot.SetCannonTabActive(isCannonTab);

//        // Also cover the static path used by GameManager / CannonPanelManager.
//        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);

//        // ── Slot containers ────────────────────────────────────────
//        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
//        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(tab == CastleTab.Archer);

//        // ── Selected indicator GameObjects ─────────────────────────
//        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
//        SetSelected(cannonSelectedObject, isCannonTab);
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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CastleTabController
/// Manages the three sub-mode buttons inside the Castle Panel:
///   [Expand]  [Cannon]  [Archer]
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
    [SerializeField] private GameObject expandSelectedObject;
    [SerializeField] private GameObject cannonSelectedObject;
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
        // Always start with all zones hidden; ApplyTabVisuals will re-enable
        // only the ones that belong to the active tab.
        CastleUnitDropZone.SetCannonZonesVisible(false);
        ArcherZoneCastle.SetArcherZonesVisible(false);

        CastleTab tab = ActiveTab == CastleTab.None ? CastleTab.Expand : ActiveTab;
        ApplyTabVisuals(tab);
        ActiveTab = tab;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Button handlers ───────────────────────────────────────────

    private void OnExpandClicked() { ActivateTab(CastleTab.Expand); Deselect(); }
    private void OnCannonClicked() { ActivateTab(CastleTab.Cannon); Deselect(); }
    private void OnArcherClicked() { ActivateTab(CastleTab.Archer); Deselect(); }

    // ── Core logic ────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the active tab changes. ArcherSlot subscribes to this
    /// so it can show/hide its remove button when the player switches tabs
    /// without the slot itself being disabled/enabled.
    /// </summary>
    public static event System.Action<CastleTab> OnTabChanged;

    public void ActivateTab(CastleTab tab)
    {
        ActiveTab = tab;
        ApplyTabVisuals(tab);
        OnTabChanged?.Invoke(tab);
        Debug.Log($"[CastleTabController] Active tab → {tab}");
    }

    /// <summary>
    /// Resets the active tab back to None so the next OnEnable
    /// always opens on the Expand tab. Call this before showing the Castle panel.
    /// </summary>
    public void ResetTab()
    {
        ActiveTab = CastleTab.None;
    }

    // ── Internal ──────────────────────────────────────────────────

    private void ApplyTabVisuals(CastleTab tab)
    {
        bool isCannonTab = tab == CastleTab.Cannon;
        bool isArcherTab = tab == CastleTab.Archer;

        CastleGrid.Instance?.SetExpansionSlotsVisible(tab == CastleTab.Expand);

        foreach (var slot in FindObjectsByType<CastleBlockUnitSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            slot.SetCannonTabActive(isCannonTab);
            slot.SetArcherTabActive(isArcherTab);
        }

        CastleUnitDropZone.SetCannonZonesVisible(isCannonTab);
        ArcherZoneCastle.SetArcherZonesVisible(isArcherTab);

        if (cannonSlotsRoot != null) cannonSlotsRoot.SetActive(isCannonTab);
        if (archerSlotsRoot != null) archerSlotsRoot.SetActive(isArcherTab);

        SetSelected(expandSelectedObject, tab == CastleTab.Expand);
        SetSelected(cannonSelectedObject, isCannonTab);
        SetSelected(archerSelectedObject, isArcherTab);
    }

    private static void SetSelected(GameObject indicator, bool selected)
    {
        if (indicator != null) indicator.SetActive(selected);
    }

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
            Debug.LogWarning("[CastleTabController] A tab button reference is null.");
    }
}