//////////////////////////////using System.Collections.Generic;
//////////////////////////////using UnityEngine;
//////////////////////////////using UnityEngine.UI;
//////////////////////////////using TMPro;

///////////////////////////////// <summary>
///////////////////////////////// AREA FORGE - InventoryPanel
/////////////////////////////////
///////////////////////////////// The main inventory window. Shows one tab per equipment slot.
///////////////////////////////// Clicking a tab filters the item grid to show only items for that slot.
///////////////////////////////// Clicking an item equips it on the soldier instantly.
/////////////////////////////////
///////////////////////////////// ── UI Hierarchy to build ────────────────────────────────────────────────────
/////////////////////////////////
/////////////////////////////////   InventoryPanel  (Panel)           ← InventoryPanel.cs
/////////////////////////////////     ├── TabBar    (HorizontalLayoutGroup)
/////////////////////////////////     │     ├── Tab_BodyType  (Button + TabButton.cs)
/////////////////////////////////     │     ├── Tab_Face      (Button + TabButton.cs)
/////////////////////////////////     │     ├── Tab_Hair      (Button + TabButton.cs)
/////////////////////////////////     │     ├── Tab_Helmet    (Button + TabButton.cs)
/////////////////////////////////     │     ├── Tab_Armor     (Button + TabButton.cs)
/////////////////////////////////     │     └── Tab_Weapon    (Button + TabButton.cs)
/////////////////////////////////     ├── ItemGrid  (ScrollRect → Viewport → Content)
/////////////////////////////////     │     └── Content  (GridLayoutGroup)  ← SlotButton prefabs spawn here
/////////////////////////////////     └── StatsPreview (optional)
/////////////////////////////////           ├── HPText   (TextMeshProUGUI)
/////////////////////////////////           ├── APText   (TextMeshProUGUI)
/////////////////////////////////           └── ADText   (TextMeshProUGUI)
/////////////////////////////////
///////////////////////////////// ── Inspector fields ─────────────────────────────────────────────────────────
/////////////////////////////////   • soldierEquipment  → drag the SolderPrefab (or its CharacterEquipment)
/////////////////////////////////   • allItems[]        → drag ALL your EquipmentItem ScriptableObject assets
/////////////////////////////////   • slotButtonPrefab  → drag the SlotButton prefab
/////////////////////////////////   • gridContent       → drag the Content object inside the ScrollRect
/////////////////////////////////   • tab buttons       → drag each Tab_XXX button
///////////////////////////////// </summary>
//////////////////////////////public class InventoryPanel : MonoBehaviour
//////////////////////////////{
//////////////////////////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////////////////////////    [Header("Soldier Reference")]
//////////////////////////////    [Tooltip("The soldier whose equipment this panel controls")]
//////////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////////////////    [Header("All Equipment Items")]
//////////////////////////////    [Tooltip("Drag every EquipmentItem ScriptableObject asset here")]
//////////////////////////////    [SerializeField] private EquipmentItem[] allItems;

//////////////////////////////    [Header("Grid")]
//////////////////////////////    [Tooltip("The SlotButton prefab (has InventorySlotButton + Button + Image)")]
//////////////////////////////    [SerializeField] private GameObject slotButtonPrefab;
//////////////////////////////    [Tooltip("GridLayoutGroup Content object inside the ScrollRect")]
//////////////////////////////    [SerializeField] private Transform gridContent;

//////////////////////////////    [Header("Tab Buttons (one per slot — order must match EquipmentSlot enum)")]
//////////////////////////////    [SerializeField] private Button tabBodyType;
//////////////////////////////    [SerializeField] private Button tabFace;
//////////////////////////////    [SerializeField] private Button tabHair;
//////////////////////////////    [SerializeField] private Button tabHelmet;
//////////////////////////////    [SerializeField] private Button tabArmor;
//////////////////////////////    [SerializeField] private Button tabWeapon;

//////////////////////////////    [Header("Tab Active Colour")]
//////////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////////////////    [Header("Stats Preview (optional — leave null to skip)")]
//////////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////////////////////    private readonly List<InventorySlotButton> _spawnedButtons = new();

//////////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////////////////    private void Awake()
//////////////////////////////    {
//////////////////////////////        // Wire tab buttons
//////////////////////////////        tabBodyType?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////////////////        tabFace?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));
//////////////////////////////    }

//////////////////////////////    private void OnEnable()
//////////////////////////////    {
//////////////////////////////        // Refresh stats display whenever the panel is opened
//////////////////////////////        if (soldierEquipment != null)
//////////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;

//////////////////////////////        ShowSlot(_activeSlot);
//////////////////////////////        RefreshStats();
//////////////////////////////    }

//////////////////////////////    private void OnDisable()
//////////////////////////////    {
//////////////////////////////        if (soldierEquipment != null)
//////////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////////////////////    }

//////////////////////////////    // ─── Tab Logic ────────────────────────────────────────────────────────────

//////////////////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////////////////    {
//////////////////////////////        _activeSlot = slot;
//////////////////////////////        UpdateTabColours();
//////////////////////////////        PopulateGrid(slot);
//////////////////////////////    }

//////////////////////////////    private void UpdateTabColours()
//////////////////////////////    {
//////////////////////////////        SetTabColour(tabBodyType, EquipmentSlot.BodyType);
//////////////////////////////        SetTabColour(tabFace, EquipmentSlot.Face);
//////////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
//////////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
//////////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
//////////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
//////////////////////////////    }

//////////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
//////////////////////////////    {
//////////////////////////////        if (btn == null) return;
//////////////////////////////        var img = btn.GetComponent<Image>();
//////////////////////////////        if (img != null)
//////////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////////////////    }

//////////////////////////////    // ─── Grid Population ──────────────────────────────────────────────────────

//////////////////////////////    private void PopulateGrid(EquipmentSlot slot)
//////////////////////////////    {
//////////////////////////////        // Clear existing buttons
//////////////////////////////        foreach (var btn in _spawnedButtons)
//////////////////////////////            if (btn != null) Destroy(btn.gameObject);
//////////////////////////////        _spawnedButtons.Clear();

//////////////////////////////        // Spawn one button per item that matches this slot
//////////////////////////////        foreach (var item in allItems)
//////////////////////////////        {
//////////////////////////////            if (item == null || item.slot != slot) continue;

//////////////////////////////            var go = Instantiate(slotButtonPrefab, gridContent);
//////////////////////////////            var btn = go.GetComponent<InventorySlotButton>();
//////////////////////////////            if (btn != null)
//////////////////////////////            {
//////////////////////////////                btn.Setup(item, soldierEquipment, this);
//////////////////////////////                _spawnedButtons.Add(btn);
//////////////////////////////            }
//////////////////////////////        }
//////////////////////////////    }

//////////////////////////////    // ─── Button Refresh ───────────────────────────────────────────────────────

//////////////////////////////    /// <summary>Called by InventorySlotButton after any equip/unequip.</summary>
//////////////////////////////    public void RefreshAllButtons()
//////////////////////////////    {
//////////////////////////////        foreach (var btn in _spawnedButtons)
//////////////////////////////            btn?.RefreshSelection();

//////////////////////////////        RefreshStats();
//////////////////////////////    }

//////////////////////////////    // ─── Stats Preview ────────────────────────────────────────────────────────

//////////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
//////////////////////////////        => RefreshStats();

//////////////////////////////    private void RefreshStats()
//////////////////////////////    {
//////////////////////////////        if (soldierEquipment == null) return;
//////////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
//////////////////////////////        if (stats == null) return;

//////////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
//////////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
//////////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
//////////////////////////////    }

//////////////////////////////    // ─── Open / Close (call from a button in the HUD) ────────────────────────

//////////////////////////////    public void Open() => gameObject.SetActive(true);
//////////////////////////////    public void Close() => gameObject.SetActive(false);
//////////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////////////////}

////////////////////////////using UnityEngine;
////////////////////////////using UnityEngine.UI;
////////////////////////////using TMPro;

/////////////////////////////// <summary>
/////////////////////////////// AREA FORGE - InventoryPanel  (Pre-Placed Groups version)
///////////////////////////////
/////////////////////////////// Your item buttons already exist in the hierarchy as pre-placed GameObjects,
/////////////////////////////// organised into one group per slot:
///////////////////////////////
///////////////////////////////   Content
///////////////////////////////     ├── GROUP_Player   ← all BodyType buttons live here
///////////////////////////////     ├── GROUP_Head     ← all Face/Head buttons live here
///////////////////////////////     ├── GROUP_Hair
///////////////////////////////     ├── GROUP_Helmet
///////////////////////////////     ├── GROUP_Armor
///////////////////////////////     └── GROUP_Weapon
///////////////////////////////
/////////////////////////////// This script shows the active group and hides all others when a tab is clicked.
/////////////////////////////// It also injects the soldier reference into every InventorySlotButton child
/////////////////////////////// so they can equip items at runtime.
///////////////////////////////
/////////////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
///////////////////////////////   1. Drag each group GameObject into the matching Group field below.
///////////////////////////////   2. Drag each tab Button into the matching Tab field.
///////////////////////////////   3. Leave soldierEquipment EMPTY — found automatically at runtime.
///////////////////////////////   4. On each pre-placed button GameObject, add InventorySlotButton and
///////////////////////////////      drag the correct EquipmentItem asset into its "Item" field.
/////////////////////////////// </summary>
////////////////////////////public class InventoryPanel : MonoBehaviour
////////////////////////////{
////////////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////////////////////////////    [Header("Soldier (leave empty — found at runtime)")]
////////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////////////////////////    [Header("Item Groups — drag each slot's parent GameObject here")]
////////////////////////////    [SerializeField] private GameObject groupPlayer;   // BodyType slot
////////////////////////////    [SerializeField] private GameObject groupHead;     // Face slot
////////////////////////////    [SerializeField] private GameObject groupHair;
////////////////////////////    [SerializeField] private GameObject groupHelmet;
////////////////////////////    [SerializeField] private GameObject groupArmor;
////////////////////////////    [SerializeField] private GameObject groupWeapon;

////////////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////////////////////////////    [Header("Tab Buttons")]
////////////////////////////    [SerializeField] private Button tabPlayer;
////////////////////////////    [SerializeField] private Button tabHead;
////////////////////////////    [SerializeField] private Button tabHair;
////////////////////////////    [SerializeField] private Button tabHelmet;
////////////////////////////    [SerializeField] private Button tabArmor;
////////////////////////////    [SerializeField] private Button tabWeapon;

////////////////////////////    [Header("Tab Colours")]
////////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////////////    // ─── Inspector — Front-View Preview (optional) ────────────────────────────

////////////////////////////    [Header("Front-View Preview (optional)")]
////////////////////////////    [SerializeField] private CharacterPreview characterPreview;

////////////////////////////    // ─── Inspector — Stats Display (optional) ─────────────────────────────────

////////////////////////////    [Header("Stats Display (optional)")]
////////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

////////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////////////    private void Awake()
////////////////////////////    {
////////////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////////////////    }

////////////////////////////    private void OnDestroy()
////////////////////////////    {
////////////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////////////////        UnsubscribeEquipment();
////////////////////////////    }

////////////////////////////    private void OnEnable()
////////////////////////////    {
////////////////////////////        if (soldierEquipment == null)
////////////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////////////////        SubscribeEquipment();
////////////////////////////        characterPreview?.SetEquipmentSource(soldierEquipment);
////////////////////////////        InitAllButtons();
////////////////////////////        ShowSlot(_activeSlot);
////////////////////////////        RefreshStats();
////////////////////////////    }

////////////////////////////    private void OnDisable()
////////////////////////////    {
////////////////////////////        UnsubscribeEquipment();
////////////////////////////    }

////////////////////////////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

////////////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////////////////    {
////////////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////////////////        if (eq == null) return;

////////////////////////////        UnsubscribeEquipment();
////////////////////////////        soldierEquipment = eq;

////////////////////////////        if (!gameObject.activeInHierarchy) return;

////////////////////////////        SubscribeEquipment();
////////////////////////////        characterPreview?.SetEquipmentSource(soldierEquipment);
////////////////////////////        InitAllButtons();
////////////////////////////        ShowSlot(_activeSlot);
////////////////////////////        RefreshStats();
////////////////////////////    }

////////////////////////////    // ─── Button Initialisation ────────────────────────────────────────────────

////////////////////////////    /// <summary>
////////////////////////////    /// Walks every group and calls Init() on every InventorySlotButton child
////////////////////////////    /// so each button knows which soldier to equip onto.
////////////////////////////    /// </summary>
////////////////////////////    private void InitAllButtons()
////////////////////////////    {
////////////////////////////        InitGroup(groupPlayer);
////////////////////////////        InitGroup(groupHead);
////////////////////////////        InitGroup(groupHair);
////////////////////////////        InitGroup(groupHelmet);
////////////////////////////        InitGroup(groupArmor);
////////////////////////////        InitGroup(groupWeapon);
////////////////////////////    }

////////////////////////////    private void InitGroup(GameObject group)
////////////////////////////    {
////////////////////////////        if (group == null || soldierEquipment == null) return;
////////////////////////////        foreach (var btn in group.GetComponentsInChildren<InventorySlotButton>(true))
////////////////////////////            btn.Init(soldierEquipment, this);
////////////////////////////    }

////////////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////////////    {
////////////////////////////        _activeSlot = slot;
////////////////////////////        UpdateTabColours();
////////////////////////////        ShowActiveGroup();
////////////////////////////    }

////////////////////////////    private void ShowActiveGroup()
////////////////////////////    {
////////////////////////////        SetGroupActive(groupPlayer, _activeSlot == EquipmentSlot.BodyType);
////////////////////////////        SetGroupActive(groupHead, _activeSlot == EquipmentSlot.Face);
////////////////////////////        SetGroupActive(groupHair, _activeSlot == EquipmentSlot.Hair);
////////////////////////////        SetGroupActive(groupHelmet, _activeSlot == EquipmentSlot.Helmet);
////////////////////////////        SetGroupActive(groupArmor, _activeSlot == EquipmentSlot.Armor);
////////////////////////////        SetGroupActive(groupWeapon, _activeSlot == EquipmentSlot.Weapon);
////////////////////////////    }

////////////////////////////    private static void SetGroupActive(GameObject group, bool active)
////////////////////////////    {
////////////////////////////        if (group != null) group.SetActive(active);
////////////////////////////    }

////////////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////////////////    private void UpdateTabColours()
////////////////////////////    {
////////////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
////////////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
////////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
////////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
////////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
////////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
////////////////////////////    }

////////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
////////////////////////////    {
////////////////////////////        if (btn == null) return;
////////////////////////////        var img = btn.GetComponent<Image>();
////////////////////////////        if (img != null)
////////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////////////    }

////////////////////////////    // ─── Public Refresh (called by InventorySlotButton after equip) ───────────

////////////////////////////    public void RefreshAllButtons()
////////////////////////////    {
////////////////////////////        var activeGroup = GetActiveGroup();
////////////////////////////        if (activeGroup == null) return;

////////////////////////////        foreach (var btn in activeGroup.GetComponentsInChildren<InventorySlotButton>(true))
////////////////////////////            btn.RefreshSelection();

////////////////////////////        RefreshStats();
////////////////////////////    }

////////////////////////////    private GameObject GetActiveGroup() => _activeSlot switch
////////////////////////////    {
////////////////////////////        EquipmentSlot.BodyType => groupPlayer,
////////////////////////////        EquipmentSlot.Face => groupHead,
////////////////////////////        EquipmentSlot.Hair => groupHair,
////////////////////////////        EquipmentSlot.Helmet => groupHelmet,
////////////////////////////        EquipmentSlot.Armor => groupArmor,
////////////////////////////        EquipmentSlot.Weapon => groupWeapon,
////////////////////////////        _ => null
////////////////////////////    };

////////////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

////////////////////////////    private void SubscribeEquipment()
////////////////////////////    {
////////////////////////////        if (soldierEquipment != null)
////////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;
////////////////////////////    }

////////////////////////////    private void UnsubscribeEquipment()
////////////////////////////    {
////////////////////////////        if (soldierEquipment != null)
////////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////////////////////    }

////////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item) => RefreshStats();

////////////////////////////    private void RefreshStats()
////////////////////////////    {
////////////////////////////        if (soldierEquipment == null) return;
////////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
////////////////////////////        if (stats == null) return;

////////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
////////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
////////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
////////////////////////////    }

////////////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

////////////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////////////}

//////////////////////////using UnityEngine;
//////////////////////////using UnityEngine.UI;
//////////////////////////using TMPro;

///////////////////////////// <summary>
///////////////////////////// AREA FORGE - InventoryPanel  (Pre-Placed Groups version)
/////////////////////////////
///////////////////////////// Your item buttons already exist in the hierarchy as pre-placed GameObjects,
///////////////////////////// organised into one group per slot:
/////////////////////////////
/////////////////////////////   Content
/////////////////////////////     ├── GROUP_Player   ← all BodyType buttons live here
/////////////////////////////     ├── GROUP_Head     ← all Face/Head buttons live here
/////////////////////////////     ├── GROUP_Hair
/////////////////////////////     ├── GROUP_Helmet
/////////////////////////////     ├── GROUP_Armor
/////////////////////////////     └── GROUP_Weapon
/////////////////////////////
///////////////////////////// This script shows the active group and hides all others when a tab is clicked.
///////////////////////////// It also injects the soldier reference into every InventorySlotButton child
///////////////////////////// so they can equip items at runtime.
/////////////////////////////
///////////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////////////////////////   1. Drag each group GameObject into the matching Group field below.
/////////////////////////////   2. Drag each tab Button into the matching Tab field.
/////////////////////////////   3. Leave soldierEquipment EMPTY — found automatically at runtime.
/////////////////////////////   4. On each pre-placed button GameObject, add InventorySlotButton and
/////////////////////////////      drag the correct EquipmentItem asset into its "Item" field.
///////////////////////////// </summary>
//////////////////////////public class InventoryPanel : MonoBehaviour
//////////////////////////{
//////////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//////////////////////////    [Header("Soldier (leave empty — found at runtime)")]
//////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////////////////////////    [Header("Item Groups — drag each slot's parent GameObject here")]
//////////////////////////    [SerializeField] private GameObject groupPlayer;   // BodyType slot
//////////////////////////    [SerializeField] private GameObject groupHead;     // Face slot
//////////////////////////    [SerializeField] private GameObject groupHair;
//////////////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////////////    [SerializeField] private GameObject groupArmor;
//////////////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//////////////////////////    [Header("Tab Buttons")]
//////////////////////////    [SerializeField] private Button tabPlayer;
//////////////////////////    [SerializeField] private Button tabHead;
//////////////////////////    [SerializeField] private Button tabHair;
//////////////////////////    [SerializeField] private Button tabHelmet;
//////////////////////////    [SerializeField] private Button tabArmor;
//////////////////////////    [SerializeField] private Button tabWeapon;

//////////////////////////    [Header("Tab Colours")]
//////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////////////    // ─── Inspector — Stats Display (optional) ─────────────────────────────────

//////////////////////////    [Header("Stats Display (optional)")]
//////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

//////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////////////    private void Awake()
//////////////////////////    {
//////////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////////////    }

//////////////////////////    private void OnDestroy()
//////////////////////////    {
//////////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////////////        UnsubscribeEquipment();
//////////////////////////    }

//////////////////////////    private void OnEnable()
//////////////////////////    {
//////////////////////////        if (soldierEquipment == null)
//////////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////////////        SubscribeEquipment();
//////////////////////////        InitAllButtons();
//////////////////////////        ShowSlot(_activeSlot);
//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    private void OnDisable()
//////////////////////////    {
//////////////////////////        UnsubscribeEquipment();
//////////////////////////    }

//////////////////////////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

//////////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////////////    {
//////////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////////////        if (eq == null) return;

//////////////////////////        UnsubscribeEquipment();
//////////////////////////        soldierEquipment = eq;

//////////////////////////        if (!gameObject.activeInHierarchy) return;

//////////////////////////        SubscribeEquipment();
//////////////////////////        InitAllButtons();
//////////////////////////        ShowSlot(_activeSlot);
//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    // ─── Button Initialisation ────────────────────────────────────────────────

//////////////////////////    /// <summary>
//////////////////////////    /// Walks every group and calls Init() on every InventorySlotButton child
//////////////////////////    /// so each button knows which soldier to equip onto.
//////////////////////////    /// </summary>
//////////////////////////    private void InitAllButtons()
//////////////////////////    {
//////////////////////////        InitGroup(groupPlayer);
//////////////////////////        InitGroup(groupHead);
//////////////////////////        InitGroup(groupHair);
//////////////////////////        InitGroup(groupHelmet);
//////////////////////////        InitGroup(groupArmor);
//////////////////////////        InitGroup(groupWeapon);
//////////////////////////    }

//////////////////////////    private void InitGroup(GameObject group)
//////////////////////////    {
//////////////////////////        if (group == null || soldierEquipment == null) return;
//////////////////////////        foreach (var btn in group.GetComponentsInChildren<InventorySlotButton>(true))
//////////////////////////            btn.Init(soldierEquipment, this);
//////////////////////////    }

//////////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////////////    {
//////////////////////////        _activeSlot = slot;
//////////////////////////        UpdateTabColours();
//////////////////////////        ShowActiveGroup();
//////////////////////////    }

//////////////////////////    private void ShowActiveGroup()
//////////////////////////    {
//////////////////////////        SetGroupActive(groupPlayer, _activeSlot == EquipmentSlot.BodyType);
//////////////////////////        SetGroupActive(groupHead, _activeSlot == EquipmentSlot.Face);
//////////////////////////        SetGroupActive(groupHair, _activeSlot == EquipmentSlot.Hair);
//////////////////////////        SetGroupActive(groupHelmet, _activeSlot == EquipmentSlot.Helmet);
//////////////////////////        SetGroupActive(groupArmor, _activeSlot == EquipmentSlot.Armor);
//////////////////////////        SetGroupActive(groupWeapon, _activeSlot == EquipmentSlot.Weapon);
//////////////////////////    }

//////////////////////////    private static void SetGroupActive(GameObject group, bool active)
//////////////////////////    {
//////////////////////////        if (group != null) group.SetActive(active);
//////////////////////////    }

//////////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////////////    private void UpdateTabColours()
//////////////////////////    {
//////////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
//////////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
//////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
//////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
//////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
//////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
//////////////////////////    }

//////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
//////////////////////////    {
//////////////////////////        if (btn == null) return;
//////////////////////////        var img = btn.GetComponent<Image>();
//////////////////////////        if (img != null)
//////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////////////    }

//////////////////////////    // ─── Public Refresh (called by InventorySlotButton after equip) ───────────

//////////////////////////    public void RefreshAllButtons()
//////////////////////////    {
//////////////////////////        var activeGroup = GetActiveGroup();
//////////////////////////        if (activeGroup == null) return;

//////////////////////////        foreach (var btn in activeGroup.GetComponentsInChildren<InventorySlotButton>(true))
//////////////////////////            btn.RefreshSelection();

//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    private GameObject GetActiveGroup() => _activeSlot switch
//////////////////////////    {
//////////////////////////        EquipmentSlot.BodyType => groupPlayer,
//////////////////////////        EquipmentSlot.Face => groupHead,
//////////////////////////        EquipmentSlot.Hair => groupHair,
//////////////////////////        EquipmentSlot.Helmet => groupHelmet,
//////////////////////////        EquipmentSlot.Armor => groupArmor,
//////////////////////////        EquipmentSlot.Weapon => groupWeapon,
//////////////////////////        _ => null
//////////////////////////    };

//////////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

//////////////////////////    private void SubscribeEquipment()
//////////////////////////    {
//////////////////////////        if (soldierEquipment != null)
//////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;
//////////////////////////    }

//////////////////////////    private void UnsubscribeEquipment()
//////////////////////////    {
//////////////////////////        if (soldierEquipment != null)
//////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////////////////    }

//////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item) => RefreshStats();

//////////////////////////    private void RefreshStats()
//////////////////////////    {
//////////////////////////        if (soldierEquipment == null) return;
//////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
//////////////////////////        if (stats == null) return;

//////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
//////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
//////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
//////////////////////////    }

//////////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

//////////////////////////    public void Open() => gameObject.SetActive(true);
//////////////////////////    public void Close() => gameObject.SetActive(false);
//////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////////////}


////////////////////////using System.Collections.Generic;
////////////////////////using UnityEngine;
////////////////////////using UnityEngine.UI;
////////////////////////using TMPro;

/////////////////////////// <summary>
/////////////////////////// AREA FORGE - InventoryPanel
///////////////////////////
/////////////////////////// Manages the inventory UI and drives the player visual swapping via
/////////////////////////// GameObject.SetActive().
///////////////////////////
/////////////////////////// ── How it works ─────────────────────────────────────────────────────────────
///////////////////////////   Each InventorySlotButton has a "playerVisualObject" field pointing to a
///////////////////////////   child GO on the Player (e.g. Armor1, Armor2, Hair1, Hair2 …).
///////////////////////////
///////////////////////////   SelectButton(btn)  → btn.Select()   activates  btn.playerVisualObject
///////////////////////////                        all other buttons in the same group → Deselect()
///////////////////////////                        their playerVisualObjects are deactivated
///////////////////////////
///////////////////////////   DeselectButton(btn)→ btn.Deselect() deactivates btn.playerVisualObject
///////////////////////////                        (nothing equipped in that slot)
///////////////////////////
/////////////////////////// ── Default first-item rule ───────────────────────────────────────────────────
///////////////////////////   When the panel opens, if no button in a group is selected, the FIRST
///////////////////////////   button is auto-selected so the player always looks correct.
///////////////////////////
/////////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
///////////////////////////   Groups  → drag the parent GO of each slot's buttons (GROUP_ARMOR etc.)
///////////////////////////   Tabs    → drag each tab Button
///////////////////////////   Soldier → leave empty (found at runtime via FindObjectOfType)
///////////////////////////
///////////////////////////   On each InventorySlotButton:
///////////////////////////     playerVisualObject → drag Player/Armor/Armor1  (or Armor2, Hair1 …)
/////////////////////////// </summary>
////////////////////////public class InventoryPanel : MonoBehaviour
////////////////////////{
////////////////////////    // ─── Inspector — Soldier (optional, for stat bonuses) ─────────────────────

////////////////////////    [Header("Soldier — leave empty, found automatically at runtime")]
////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////////////////////    [Header("Item Groups — drag each slot's button-parent GO here")]
////////////////////////    [SerializeField] private GameObject groupPlayer;    // BodyType slot
////////////////////////    [SerializeField] private GameObject groupHead;      // Face slot
////////////////////////    [SerializeField] private GameObject groupHair;
////////////////////////    [SerializeField] private GameObject groupHelmet;
////////////////////////    [SerializeField] private GameObject groupArmor;
////////////////////////    [SerializeField] private GameObject groupWeapon;

////////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////////////////////////    [Header("Tab Buttons")]
////////////////////////    [SerializeField] private Button tabPlayer;
////////////////////////    [SerializeField] private Button tabHead;
////////////////////////    [SerializeField] private Button tabHair;
////////////////////////    [SerializeField] private Button tabHelmet;
////////////////////////    [SerializeField] private Button tabArmor;
////////////////////////    [SerializeField] private Button tabWeapon;

////////////////////////    [Header("Tab Colours")]
////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////////    // ─── Inspector — Stats (optional) ─────────────────────────────────────────

////////////////////////    [Header("Stats Display (optional)")]
////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

////////////////////////    // Cached lists of buttons per group (built once in Init)
////////////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////////    private void Awake()
////////////////////////    {
////////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////////////    }

////////////////////////    private void OnDestroy()
////////////////////////    {
////////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////////////    }

////////////////////////    private void OnEnable()
////////////////////////    {
////////////////////////        if (soldierEquipment == null)
////////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////////////        BuildGroupCache();
////////////////////////        InitAllButtons();
////////////////////////        AutoSelectFirstItems();
////////////////////////        ShowSlot(_activeSlot);
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

////////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////////////    {
////////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////////////        if (eq == null) return;
////////////////////////        soldierEquipment = eq;
////////////////////////        if (!gameObject.activeInHierarchy) return;
////////////////////////        InitAllButtons();
////////////////////////        AutoSelectFirstItems();
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////////////////    private void BuildGroupCache()
////////////////////////    {
////////////////////////        _groups.Clear();
////////////////////////        AddToCache(EquipmentSlot.BodyType, groupPlayer);
////////////////////////        AddToCache(EquipmentSlot.Face, groupHead);
////////////////////////        AddToCache(EquipmentSlot.Hair, groupHair);
////////////////////////        AddToCache(EquipmentSlot.Helmet, groupHelmet);
////////////////////////        AddToCache(EquipmentSlot.Armor, groupArmor);
////////////////////////        AddToCache(EquipmentSlot.Weapon, groupWeapon);
////////////////////////    }

////////////////////////    private void AddToCache(EquipmentSlot slot, GameObject group)
////////////////////////    {
////////////////////////        var list = new List<InventorySlotButton>();
////////////////////////        if (group != null)
////////////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////////////////        _groups[slot] = list;
////////////////////////    }

////////////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

////////////////////////    private void InitAllButtons()
////////////////////////    {
////////////////////////        foreach (var kvp in _groups)
////////////////////////            foreach (var btn in kvp.Value)
////////////////////////                btn.Init(this, soldierEquipment);
////////////////////////    }

////////////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

////////////////////////    /// <summary>
////////////////////////    /// For each slot, if nothing is selected yet, auto-select the first button.
////////////////////////    /// This activates the first playerVisualObject in each group by default.
////////////////////////    /// </summary>
////////////////////////    private void AutoSelectFirstItems()
////////////////////////    {
////////////////////////        foreach (var kvp in _groups)
////////////////////////        {
////////////////////////            var list = kvp.Value;
////////////////////////            if (list.Count == 0) continue;

////////////////////////            // Already something selected? Leave it.
////////////////////////            bool anySelected = false;
////////////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

////////////////////////            if (!anySelected)
////////////////////////                SelectExclusive(list[0], list);
////////////////////////        }
////////////////////////    }

////////////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////////////////    /// <summary>
////////////////////////    /// Selects btn, deselects all others in the same slot group.
////////////////////////    /// This activates btn.playerVisualObject and deactivates the rest.
////////////////////////    /// </summary>
////////////////////////    public void SelectButton(InventorySlotButton btn)
////////////////////////    {
////////////////////////        var list = GetGroupForButton(btn);
////////////////////////        if (list == null) return;
////////////////////////        SelectExclusive(btn, list);
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    /// <summary>
////////////////////////    /// Deselects btn only — no other button is auto-selected.
////////////////////////    /// The player will have nothing equipped in that slot.
////////////////////////    /// </summary>
////////////////////////    public void DeselectButton(InventorySlotButton btn)
////////////////////////    {
////////////////////////        btn.Deselect();
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    // ─── Core Exclusive-Activate Logic ───────────────────────────────────────

////////////////////////    /// <summary>
////////////////////////    /// Activates target's playerVisualObject, deactivates all others in the list.
////////////////////////    /// </summary>
////////////////////////    private void SelectExclusive(InventorySlotButton target, List<InventorySlotButton> group)
////////////////////////    {
////////////////////////        foreach (var btn in group)
////////////////////////        {
////////////////////////            if (btn == target)
////////////////////////                btn.Select();     // SetActive(true) on its playerVisualObject
////////////////////////            else
////////////////////////                btn.Deselect();   // SetActive(false) on its playerVisualObject
////////////////////////        }
////////////////////////    }

////////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////////    {
////////////////////////        _activeSlot = slot;
////////////////////////        UpdateTabColours();

////////////////////////        // Show only the active group's button panel
////////////////////////        SetGroupUIActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////////////////        SetGroupUIActive(groupHead, slot == EquipmentSlot.Face);
////////////////////////        SetGroupUIActive(groupHair, slot == EquipmentSlot.Hair);
////////////////////////        SetGroupUIActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////////////////        SetGroupUIActive(groupArmor, slot == EquipmentSlot.Armor);
////////////////////////        SetGroupUIActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////////////////    }

////////////////////////    private static void SetGroupUIActive(GameObject group, bool active)
////////////////////////    {
////////////////////////        if (group != null) group.SetActive(active);
////////////////////////    }

////////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////////////    private void UpdateTabColours()
////////////////////////    {
////////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
////////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
////////////////////////    }

////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
////////////////////////    {
////////////////////////        if (btn == null) return;
////////////////////////        var img = btn.GetComponent<Image>();
////////////////////////        if (img != null)
////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////////    }

////////////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////////////////    private List<InventorySlotButton> GetGroupForButton(InventorySlotButton target)
////////////////////////    {
////////////////////////        foreach (var kvp in _groups)
////////////////////////            foreach (var btn in kvp.Value)
////////////////////////                if (btn == target) return kvp.Value;
////////////////////////        return null;
////////////////////////    }

////////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

////////////////////////    private void RefreshStats()
////////////////////////    {
////////////////////////        if (soldierEquipment == null) return;
////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
////////////////////////        if (stats == null) return;
////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
////////////////////////    }

////////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

////////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////////}

//////////////////////using System.Collections.Generic;
//////////////////////using UnityEngine;
//////////////////////using UnityEngine.UI;
//////////////////////using TMPro;

///////////////////////// <summary>
///////////////////////// AREA FORGE - InventoryPanel
/////////////////////////
///////////////////////// ── Key fix ──────────────────────────────────────────────────────────────────
/////////////////////////   All slot groups are FORCED ACTIVE before Init so that
/////////////////////////   InventorySlotButton.Init() can run on every button (including ones in
/////////////////////////   hidden groups). After Init, ShowSlot() hides the non-active groups again.
/////////////////////////
///////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////////////////////   groupArmor  → drag the parent GO of all Armor buttons   (e.g. Content/ARMOR)
/////////////////////////   groupHair   → drag the parent GO of all Hair buttons    (e.g. Content/HAIR)
/////////////////////////   … and so on for Head, Helmet, Player, Weapon
/////////////////////////   Tab buttons → drag each tab Button
///////////////////////// </summary>
//////////////////////public class InventoryPanel : MonoBehaviour
//////////////////////{
//////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//////////////////////    [Header("Soldier — leave empty, found automatically")]
//////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////////////////////    [Header("Item Groups — one parent GO per slot")]
//////////////////////    [SerializeField] private GameObject groupPlayer;
//////////////////////    [SerializeField] private GameObject groupHead;
//////////////////////    [SerializeField] private GameObject groupHair;
//////////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////////    [SerializeField] private GameObject groupArmor;
//////////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////////    // ─── Inspector — Tabs ─────────────────────────────────────────────────────

//////////////////////    [Header("Tab Buttons")]
//////////////////////    [SerializeField] private Button tabPlayer;
//////////////////////    [SerializeField] private Button tabHead;
//////////////////////    [SerializeField] private Button tabHair;
//////////////////////    [SerializeField] private Button tabHelmet;
//////////////////////    [SerializeField] private Button tabArmor;
//////////////////////    [SerializeField] private Button tabWeapon;

//////////////////////    [Header("Tab Colours")]
//////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////////    // ─── Inspector — Stats ────────────────────────────────────────────────────

//////////////////////    [Header("Stats Display (optional)")]
//////////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////////    private void Awake()
//////////////////////    {
//////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////////    }

//////////////////////    private void OnDestroy()
//////////////////////    {
//////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////////    }

//////////////////////    private void OnEnable()
//////////////////////    {
//////////////////////        if (soldierEquipment == null)
//////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////////        // ── CRITICAL: activate ALL groups first so Init() reaches every button ──
//////////////////////        // (buttons inside inactive GOs never have Awake/Start called)
//////////////////////        ForceAllGroupsActive(true);

//////////////////////        BuildGroupCache();
//////////////////////        InitAllButtons();
//////////////////////        AutoSelectFirstItems();

//////////////////////        // Now hide the non-active groups
//////////////////////        ShowSlot(_activeSlot);
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

//////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////////    {
//////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////////        if (eq == null) return;
//////////////////////        soldierEquipment = eq;
//////////////////////        if (!gameObject.activeInHierarchy) return;

//////////////////////        ForceAllGroupsActive(true);
//////////////////////        InitAllButtons();
//////////////////////        AutoSelectFirstItems();
//////////////////////        ShowSlot(_activeSlot);
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////////////    private void BuildGroupCache()
//////////////////////    {
//////////////////////        _groups.Clear();
//////////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////////////////        Cache(EquipmentSlot.Face, groupHead);
//////////////////////        Cache(EquipmentSlot.Hair, groupHair);
//////////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////////////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////////////////    }

//////////////////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////////////////    {
//////////////////////        var list = new List<InventorySlotButton>();
//////////////////////        if (group != null)
//////////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////////////        _groups[slot] = list;
//////////////////////    }

//////////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

//////////////////////    private void InitAllButtons()
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////            foreach (var btn in kvp.Value)
//////////////////////                btn.Init(this, soldierEquipment);
//////////////////////    }

//////////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

//////////////////////    private void AutoSelectFirstItems()
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////        {
//////////////////////            var list = kvp.Value;
//////////////////////            if (list.Count == 0) continue;

//////////////////////            bool anySelected = false;
//////////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

//////////////////////            if (!anySelected)
//////////////////////                Activate(list[0], list);
//////////////////////        }
//////////////////////    }

//////////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////////////////    public void SelectButton(InventorySlotButton btn)
//////////////////////    {
//////////////////////        var list = FindGroup(btn);
//////////////////////        if (list == null) return;
//////////////////////        Activate(btn, list);
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    public void DeselectButton(InventorySlotButton btn)
//////////////////////    {
//////////////////////        btn.Deselect();
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

//////////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
//////////////////////    {
//////////////////////        foreach (var btn in group)
//////////////////////        {
//////////////////////            if (btn == target) btn.Select();
//////////////////////            else btn.Deselect();
//////////////////////        }
//////////////////////    }

//////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////////    {
//////////////////////        _activeSlot = slot;
//////////////////////        UpdateTabColours();

//////////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////////////    }

//////////////////////    private void ForceAllGroupsActive(bool active)
//////////////////////    {
//////////////////////        SetActive(groupPlayer, active);
//////////////////////        SetActive(groupHead, active);
//////////////////////        SetActive(groupHair, active);
//////////////////////        SetActive(groupHelmet, active);
//////////////////////        SetActive(groupArmor, active);
//////////////////////        SetActive(groupWeapon, active);
//////////////////////    }

//////////////////////    private static void SetActive(GameObject go, bool active)
//////////////////////    {
//////////////////////        if (go != null) go.SetActive(active);
//////////////////////    }

//////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////////    private void UpdateTabColours()
//////////////////////    {
//////////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////////////////        Tint(tabHead, EquipmentSlot.Face);
//////////////////////        Tint(tabHair, EquipmentSlot.Hair);
//////////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////////////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////////////////    }

//////////////////////    private void Tint(Button btn, EquipmentSlot slot)
//////////////////////    {
//////////////////////        if (btn == null) return;
//////////////////////        var img = btn.GetComponent<Image>();
//////////////////////        if (img != null)
//////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////////    }

//////////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////            foreach (var btn in kvp.Value)
//////////////////////                if (btn == target) return kvp.Value;
//////////////////////        return null;
//////////////////////    }

//////////////////////    private void RefreshStats()
//////////////////////    {
//////////////////////        if (soldierEquipment == null) return;
//////////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////////////////        if (s == null) return;
//////////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////////////////    }

//////////////////////    public void Open() => gameObject.SetActive(true);
//////////////////////    public void Close() => gameObject.SetActive(false);
//////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////////}

////////////////////using System.Collections.Generic;
////////////////////using UnityEngine;
////////////////////using UnityEngine.UI;
////////////////////using TMPro;

/////////////////////// <summary>
/////////////////////// AREA FORGE - InventoryPanel
///////////////////////
/////////////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////////////////   Drag the currently-equipped Hair player GO into "hairVisualRoot".
///////////////////////   When a helmet is equipped   → hairVisualRoot.SetActive(false)
///////////////////////   When a helmet is unequipped → hairVisualRoot.SetActive(true)
///////////////////////
///////////////////////   hairVisualRoot should be the PARENT of all hair GOs under the Player,
///////////////////////   e.g. Player/Hair  (so all hair variants hide together).
///////////////////////   If each hair has its own GO, drag whichever is currently selected —
///////////////////////   the panel updates it live whenever hair selection changes.
///////////////////////
/////////////////////// ── Default items (Body, Face) ───────────────────────────────────────────────
///////////////////////   Tick "Is Default" on the Body and Face InventorySlotButtons.
///////////////////////   They auto-select on open and clicking them does nothing.
///////////////////////
/////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
///////////////////////   groupArmor   → parent GO of all Armor buttons   (e.g. Content/ARMOR)
///////////////////////   groupHair    → parent GO of all Hair  buttons   (e.g. Content/HAIR)
///////////////////////   … same for Head, Helmet, Player, Weapon
///////////////////////   hairVisualRoot → Player/Hair  (the hair layer on the player)
/////////////////////// </summary>
////////////////////public class InventoryPanel : MonoBehaviour
////////////////////{
////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////////////////////    [Header("Soldier — leave empty, found automatically")]
////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////////////////    [Header("Item Groups — one parent GO per slot")]
////////////////////    [SerializeField] private GameObject groupPlayer;
////////////////////    [SerializeField] private GameObject groupHead;
////////////////////    [SerializeField] private GameObject groupHair;
////////////////////    [SerializeField] private GameObject groupHelmet;
////////////////////    [SerializeField] private GameObject groupArmor;
////////////////////    [SerializeField] private GameObject groupWeapon;

////////////////////    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

////////////////////    [Header("Helmet hides Hair")]
////////////////////    [Tooltip("Drag the Hair parent GO on the PLAYER (e.g. Player/Hair).\n" +
////////////////////             "This whole GO is hidden when any helmet is equipped.")]
////////////////////    [SerializeField] private GameObject hairVisualRoot;

////////////////////    // ─── Inspector — Tabs ─────────────────────────────────────────────────────

////////////////////    [Header("Tab Buttons")]
////////////////////    [SerializeField] private Button tabPlayer;
////////////////////    [SerializeField] private Button tabHead;
////////////////////    [SerializeField] private Button tabHair;
////////////////////    [SerializeField] private Button tabHelmet;
////////////////////    [SerializeField] private Button tabArmor;
////////////////////    [SerializeField] private Button tabWeapon;

////////////////////    [Header("Tab Colours")]
////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////    // ─── Inspector — Stats ────────────────────────────────────────────────────

////////////////////    [Header("Stats Display (optional)")]
////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////////////    // Track whether any helmet is currently selected
////////////////////    private bool _helmetEquipped = false;

////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////    private void Awake()
////////////////////    {
////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////////    }

////////////////////    private void OnDestroy()
////////////////////    {
////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////////    }

////////////////////    private void OnEnable()
////////////////////    {
////////////////////        if (soldierEquipment == null)
////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////////        // Activate ALL groups first so Init() reaches every button
////////////////////        // (buttons inside inactive GOs never have Awake called)
////////////////////        ForceAllGroupsActive(true);

////////////////////        BuildGroupCache();
////////////////////        InitAllButtons();
////////////////////        AutoSelectFirstItems();

////////////////////        ShowSlot(_activeSlot);
////////////////////        ApplyHairHelmetRule();
////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////////    {
////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////////        if (eq == null) return;
////////////////////        soldierEquipment = eq;
////////////////////        if (!gameObject.activeInHierarchy) return;

////////////////////        ForceAllGroupsActive(true);
////////////////////        InitAllButtons();
////////////////////        AutoSelectFirstItems();
////////////////////        ShowSlot(_activeSlot);
////////////////////        ApplyHairHelmetRule();
////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////////////    private void BuildGroupCache()
////////////////////    {
////////////////////        _groups.Clear();
////////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////////////        Cache(EquipmentSlot.Face, groupHead);
////////////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////////////    }

////////////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////////////    {
////////////////////        var list = new List<InventorySlotButton>();
////////////////////        if (group != null)
////////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////////////        _groups[slot] = list;
////////////////////    }

////////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

////////////////////    private void InitAllButtons()
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////            foreach (var btn in kvp.Value)
////////////////////                btn.Init(this, soldierEquipment);
////////////////////    }

////////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

////////////////////    private void AutoSelectFirstItems()
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////        {
////////////////////            var list = kvp.Value;
////////////////////            if (list.Count == 0) continue;

////////////////////            bool anySelected = false;
////////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

////////////////////            if (!anySelected)
////////////////////                Activate(list[0], list);
////////////////////        }
////////////////////    }

////////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////////////    public void SelectButton(InventorySlotButton btn)
////////////////////    {
////////////////////        var list = FindGroup(btn);
////////////////////        if (list == null) return;

////////////////////        Activate(btn, list);

////////////////////        // Check if this is a helmet slot selection
////////////////////        if (btn.Item != null && btn.Item.slot == EquipmentSlot.Helmet)
////////////////////        {
////////////////////            _helmetEquipped = true;
////////////////////            ApplyHairHelmetRule();
////////////////////        }

////////////////////        RefreshStats();
////////////////////    }

////////////////////    public void DeselectButton(InventorySlotButton btn)
////////////////////    {
////////////////////        btn.Deselect();

////////////////////        // Check if a helmet was just removed
////////////////////        if (btn.Item != null && btn.Item.slot == EquipmentSlot.Helmet)
////////////////////        {
////////////////////            _helmetEquipped = false;
////////////////////            ApplyHairHelmetRule();
////////////////////        }

////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////////////////    /// <summary>
////////////////////    /// Hides the hair visual root when any helmet is equipped.
////////////////////    /// Shows it again when the helmet is removed.
////////////////////    /// </summary>
////////////////////    private void ApplyHairHelmetRule()
////////////////////    {
////////////////////        if (hairVisualRoot == null) return;

////////////////////        // Check the helmet group for any selected button
////////////////////        bool helmetOn = false;
////////////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////////////////            foreach (var btn in helmetGroup)
////////////////////                if (btn.IsSelected) { helmetOn = true; break; }

////////////////////        _helmetEquipped = helmetOn;

////////////////////        // Show or hide the currently selected hair visual
////////////////////        // We hide/show the entire hairVisualRoot (parent of all hair GOs on the player)
////////////////////        hairVisualRoot.SetActive(!helmetOn);
////////////////////    }

////////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

////////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
////////////////////    {
////////////////////        foreach (var btn in group)
////////////////////        {
////////////////////            if (btn == target) btn.Select();
////////////////////            else btn.Deselect();
////////////////////        }
////////////////////    }

////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////    {
////////////////////        _activeSlot = slot;
////////////////////        UpdateTabColours();

////////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////////////    }

////////////////////    private void ForceAllGroupsActive(bool active)
////////////////////    {
////////////////////        SetActive(groupPlayer, active);
////////////////////        SetActive(groupHead, active);
////////////////////        SetActive(groupHair, active);
////////////////////        SetActive(groupHelmet, active);
////////////////////        SetActive(groupArmor, active);
////////////////////        SetActive(groupWeapon, active);
////////////////////    }

////////////////////    private static void SetActive(GameObject go, bool active)
////////////////////    {
////////////////////        if (go != null) go.SetActive(active);
////////////////////    }

////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////////    private void UpdateTabColours()
////////////////////    {
////////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////////////        Tint(tabHead, EquipmentSlot.Face);
////////////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////////////    }

////////////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////////////    {
////////////////////        if (btn == null) return;
////////////////////        var img = btn.GetComponent<Image>();
////////////////////        if (img != null)
////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////    }

////////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////            foreach (var btn in kvp.Value)
////////////////////                if (btn == target) return kvp.Value;
////////////////////        return null;
////////////////////    }

////////////////////    private void RefreshStats()
////////////////////    {
////////////////////        if (soldierEquipment == null) return;
////////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////////////        if (s == null) return;
////////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////////////    }

////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////}

//////////////////using System.Collections.Generic;
//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using TMPro;

///////////////////// <summary>
///////////////////// AREA FORGE - InventoryPanel
/////////////////////
///////////////////// ── Selection rule ───────────────────────────────────────────────────────────
/////////////////////   Exactly ONE item is always selected per slot group.
/////////////////////   Clicking another item switches to it. Clicking the current one does nothing.
/////////////////////   The first item in each group is selected by default on open.
/////////////////////
///////////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////////////   Drag Player/Hair into hairVisualRoot.
/////////////////////   Helmet equipped   → hairVisualRoot hides
/////////////////////   Helmet unequipped → hairVisualRoot shows
///////////////////// </summary>
//////////////////public class InventoryPanel : MonoBehaviour
//////////////////{
//////////////////    [Header("Soldier — leave empty, found automatically")]
//////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////    [Header("Item Groups — one parent GO per slot")]
//////////////////    [SerializeField] private GameObject groupPlayer;
//////////////////    [SerializeField] private GameObject groupHead;
//////////////////    [SerializeField] private GameObject groupHair;
//////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////    [SerializeField] private GameObject groupArmor;
//////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////    [Header("Helmet hides Hair")]
//////////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////////////////    [SerializeField] private GameObject hairVisualRoot;

//////////////////    [Header("Tab Buttons")]
//////////////////    [SerializeField] private Button tabPlayer;
//////////////////    [SerializeField] private Button tabHead;
//////////////////    [SerializeField] private Button tabHair;
//////////////////    [SerializeField] private Button tabHelmet;
//////////////////    [SerializeField] private Button tabArmor;
//////////////////    [SerializeField] private Button tabWeapon;

//////////////////    [Header("Tab Colours")]
//////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////    [Header("Stats Display (optional)")]
//////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////    private void Awake()
//////////////////    {
//////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////    }

//////////////////    private void OnDestroy()
//////////////////    {
//////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////    }

//////////////////    private void OnEnable()
//////////////////    {
//////////////////        if (soldierEquipment == null)
//////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////        ForceAllGroupsActive(true);   // must be active before Init() runs
//////////////////        BuildGroupCache();
//////////////////        InitAllButtons();
//////////////////        AutoSelectFirstItems();
//////////////////        ShowSlot(_activeSlot);
//////////////////        ApplyHairHelmetRule();
//////////////////        RefreshStats();
//////////////////    }

//////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////    {
//////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////        if (eq == null) return;
//////////////////        soldierEquipment = eq;
//////////////////        if (!gameObject.activeInHierarchy) return;
//////////////////        ForceAllGroupsActive(true);
//////////////////        InitAllButtons();
//////////////////        AutoSelectFirstItems();
//////////////////        ShowSlot(_activeSlot);
//////////////////        ApplyHairHelmetRule();
//////////////////        RefreshStats();
//////////////////    }

//////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////////    private void BuildGroupCache()
//////////////////    {
//////////////////        _groups.Clear();
//////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////////////        Cache(EquipmentSlot.Face, groupHead);
//////////////////        Cache(EquipmentSlot.Hair, groupHair);
//////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////////////    }

//////////////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////////////    {
//////////////////        var list = new List<InventorySlotButton>();
//////////////////        if (group != null)
//////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////////        _groups[slot] = list;
//////////////////    }

//////////////////    private void InitAllButtons()
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////            foreach (var btn in kvp.Value)
//////////////////                btn.Init(this, soldierEquipment);
//////////////////    }

//////////////////    // ─── Default: auto-select first item per group ────────────────────────────

//////////////////    private void AutoSelectFirstItems()
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////        {
//////////////////            var list = kvp.Value;
//////////////////            if (list.Count == 0) continue;

//////////////////            // Check if anything is already selected
//////////////////            bool anySelected = false;
//////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

//////////////////            if (!anySelected)
//////////////////                Activate(list[0], list);
//////////////////        }
//////////////////    }

//////////////////    // ─── Public: called by InventorySlotButton.OnClick ───────────────────────

//////////////////    /// <summary>
//////////////////    /// Selects btn exclusively in its group.
//////////////////    /// All other buttons in the same slot are deselected.
//////////////////    /// </summary>
//////////////////    public void SelectButton(InventorySlotButton btn)
//////////////////    {
//////////////////        var list = FindGroup(btn);
//////////////////        if (list == null) return;

//////////////////        Activate(btn, list);
//////////////////        ApplyHairHelmetRule();
//////////////////        RefreshStats();
//////////////////    }

//////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

//////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
//////////////////    {
//////////////////        foreach (var btn in group)
//////////////////        {
//////////////////            if (btn == target) btn.Select();
//////////////////            else btn.Deselect();
//////////////////        }
//////////////////    }

//////////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////////////    private void ApplyHairHelmetRule()
//////////////////    {
//////////////////        if (hairVisualRoot == null) return;

//////////////////        bool helmetOn = false;
//////////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////////////            foreach (var btn in helmetGroup)
//////////////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////////////        hairVisualRoot.SetActive(!helmetOn);
//////////////////    }

//////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////    {
//////////////////        _activeSlot = slot;
//////////////////        UpdateTabColours();
//////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////////    }

//////////////////    private void ForceAllGroupsActive(bool active)
//////////////////    {
//////////////////        SetActive(groupPlayer, active);
//////////////////        SetActive(groupHead, active);
//////////////////        SetActive(groupHair, active);
//////////////////        SetActive(groupHelmet, active);
//////////////////        SetActive(groupArmor, active);
//////////////////        SetActive(groupWeapon, active);
//////////////////    }

//////////////////    private static void SetActive(GameObject go, bool active)
//////////////////    {
//////////////////        if (go != null) go.SetActive(active);
//////////////////    }

//////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////    private void UpdateTabColours()
//////////////////    {
//////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////////////        Tint(tabHead, EquipmentSlot.Face);
//////////////////        Tint(tabHair, EquipmentSlot.Hair);
//////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////////////    }

//////////////////    private void Tint(Button btn, EquipmentSlot slot)
//////////////////    {
//////////////////        if (btn == null) return;
//////////////////        var img = btn.GetComponent<Image>();
//////////////////        if (img != null)
//////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////    }

//////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////            foreach (var btn in kvp.Value)
//////////////////                if (btn == target) return kvp.Value;
//////////////////        return null;
//////////////////    }

//////////////////    private void RefreshStats()
//////////////////    {
//////////////////        if (soldierEquipment == null) return;
//////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////////////        if (s == null) return;
//////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////////////    }

//////////////////    public void Open() => gameObject.SetActive(true);
//////////////////    public void Close() => gameObject.SetActive(false);
//////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////}

////////////////using System.Collections.Generic;
////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using TMPro;

/////////////////// <summary>
/////////////////// AREA FORGE - InventoryPanel
///////////////////
/////////////////// ── Selection rules ──────────────────────────────────────────────────────────
///////////////////   • Body (Skinny)              → always selected, never deselectable (isDefault)
///////////////////   • Armor / Helmet / Weapon / Hair → click to select, click again to deselect
///////////////////   • No other auto-selection on open
///////////////////
/////////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////////////   Drag Player/Hair into hairVisualRoot.
///////////////////   Helmet selected   → hairVisualRoot hides
///////////////////   Helmet deselected → hairVisualRoot shows
/////////////////// </summary>
////////////////public class InventoryPanel : MonoBehaviour
////////////////{
////////////////    [Header("Soldier — leave empty, found automatically")]
////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////    [Header("Item Groups — one parent GO per slot")]
////////////////    [SerializeField] private GameObject groupPlayer;
////////////////    [SerializeField] private GameObject groupHead;
////////////////    [SerializeField] private GameObject groupHair;
////////////////    [SerializeField] private GameObject groupHelmet;
////////////////    [SerializeField] private GameObject groupArmor;
////////////////    [SerializeField] private GameObject groupWeapon;

////////////////    [Header("Helmet hides Hair")]
////////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////////////    [SerializeField] private GameObject hairVisualRoot;

////////////////    [Header("Tab Buttons")]
////////////////    [SerializeField] private Button tabPlayer;
////////////////    [SerializeField] private Button tabHead;
////////////////    [SerializeField] private Button tabHair;
////////////////    [SerializeField] private Button tabHelmet;
////////////////    [SerializeField] private Button tabArmor;
////////////////    [SerializeField] private Button tabWeapon;

////////////////    [Header("Tab Colours")]
////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////    [Header("Stats Display (optional)")]
////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////    }

////////////////    private void OnDestroy()
////////////////    {
////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////    }

////////////////    private void OnEnable()
////////////////    {
////////////////        if (soldierEquipment == null)
////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////        ForceAllGroupsActive(true);
////////////////        BuildGroupCache();
////////////////        InitAllButtons();
////////////////        SelectDefaultItems();   // only selects buttons marked isDefault (Skinny Body)
////////////////        ShowSlot(_activeSlot);
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////    {
////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////        if (eq == null) return;
////////////////        soldierEquipment = eq;
////////////////        if (!gameObject.activeInHierarchy) return;
////////////////        ForceAllGroupsActive(true);
////////////////        InitAllButtons();
////////////////        SelectDefaultItems();
////////////////        ShowSlot(_activeSlot);
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////////    private void BuildGroupCache()
////////////////    {
////////////////        _groups.Clear();
////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////////        Cache(EquipmentSlot.Face, groupHead);
////////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////////    }

////////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////////    {
////////////////        var list = new List<InventorySlotButton>();
////////////////        if (group != null)
////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////////        _groups[slot] = list;
////////////////    }

////////////////    private void InitAllButtons()
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////            foreach (var btn in kvp.Value)
////////////////                btn.Init(this, soldierEquipment);
////////////////    }

////////////////    // ─── Default Selection ────────────────────────────────────────────────────

////////////////    /// <summary>
////////////////    /// Only selects buttons that have "isDefault" ticked (Skinny Body).
////////////////    /// Everything else starts deselected / deactivated.
////////////////    /// </summary>
////////////////    private void SelectDefaultItems()
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////        {
////////////////            foreach (var btn in kvp.Value)
////////////////            {
////////////////                if (btn.IsDefault)
////////////////                    btn.Select();     // Skinny Body — force selected
////////////////                else
////////////////                    btn.Deselect();   // everything else — deactivated
////////////////            }
////////////////        }
////////////////    }

////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////////    /// <summary>
////////////////    /// Selects btn and deselects all other NON-DEFAULT buttons in the same group.
////////////////    /// </summary>
////////////////    public void SelectButton(InventorySlotButton btn)
////////////////    {
////////////////        var list = FindGroup(btn);
////////////////        if (list == null) return;

////////////////        foreach (var b in list)
////////////////        {
////////////////            if (b == btn) b.Select();
////////////////            else if (!b.IsDefault) b.Deselect();   // never deselect a default button
////////////////        }

////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    /// <summary>
////////////////    /// Deselects btn only (called when user clicks an already-selected item).
////////////////    /// </summary>
////////////////    public void DeselectButton(InventorySlotButton btn)
////////////////    {
////////////////        if (btn.IsDefault) return;   // safety
////////////////        btn.Deselect();
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////////////    private void ApplyHairHelmetRule()
////////////////    {
////////////////        if (hairVisualRoot == null) return;

////////////////        bool helmetOn = false;
////////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////////////            foreach (var btn in helmetGroup)
////////////////                if (btn.IsSelected) { helmetOn = true; break; }

////////////////        hairVisualRoot.SetActive(!helmetOn);
////////////////    }

////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////    {
////////////////        _activeSlot = slot;
////////////////        UpdateTabColours();
////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////////    }

////////////////    private void ForceAllGroupsActive(bool active)
////////////////    {
////////////////        SetActive(groupPlayer, active);
////////////////        SetActive(groupHead, active);
////////////////        SetActive(groupHair, active);
////////////////        SetActive(groupHelmet, active);
////////////////        SetActive(groupArmor, active);
////////////////        SetActive(groupWeapon, active);
////////////////    }

////////////////    private static void SetActive(GameObject go, bool active)
////////////////    {
////////////////        if (go != null) go.SetActive(active);
////////////////    }

////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////    private void UpdateTabColours()
////////////////    {
////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////////        Tint(tabHead, EquipmentSlot.Face);
////////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////////    }

////////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////////    {
////////////////        if (btn == null) return;
////////////////        var img = btn.GetComponent<Image>();
////////////////        if (img != null)
////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////    }

////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////            foreach (var btn in kvp.Value)
////////////////                if (btn == target) return kvp.Value;
////////////////        return null;
////////////////    }

////////////////    private void RefreshStats()
////////////////    {
////////////////        if (soldierEquipment == null) return;
////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////////        if (s == null) return;
////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////////    }

////////////////    public void Open() => gameObject.SetActive(true);
////////////////    public void Close() => gameObject.SetActive(false);
////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////}

//////////////using System.Collections.Generic;
//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using TMPro;

///////////////// <summary>
///////////////// AREA FORGE - InventoryPanel
/////////////////
///////////////// ── Selection rules ──────────────────────────────────────────────────────────
/////////////////   • Body (Skinny)   → isDefault = true  → always selected, cannot deselect
/////////////////   • Face (first)    → auto-selected on open, CAN be deselected by clicking again
/////////////////   • Hair (first)    → auto-selected on open, CAN be deselected by clicking again
/////////////////   • Armor / Helmet / Weapon → no default, click to select / click again to deselect
/////////////////
///////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////////   Drag Player/Hair into hairVisualRoot.
/////////////////   Helmet selected   → hairVisualRoot hides
/////////////////   Helmet deselected → hairVisualRoot shows
///////////////// </summary>
//////////////public class InventoryPanel : MonoBehaviour
//////////////{
//////////////    [Header("Soldier — leave empty, found automatically")]
//////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////    [Header("Item Groups — one parent GO per slot")]
//////////////    [SerializeField] private GameObject groupPlayer;
//////////////    [SerializeField] private GameObject groupHead;
//////////////    [SerializeField] private GameObject groupHair;
//////////////    [SerializeField] private GameObject groupHelmet;
//////////////    [SerializeField] private GameObject groupArmor;
//////////////    [SerializeField] private GameObject groupWeapon;

//////////////    [Header("Helmet hides Hair")]
//////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////////////    [SerializeField] private GameObject hairVisualRoot;

//////////////    [Header("Tab Buttons")]
//////////////    [SerializeField] private Button tabPlayer;
//////////////    [SerializeField] private Button tabHead;
//////////////    [SerializeField] private Button tabHair;
//////////////    [SerializeField] private Button tabHelmet;
//////////////    [SerializeField] private Button tabArmor;
//////////////    [SerializeField] private Button tabWeapon;

//////////////    [Header("Tab Colours")]
//////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////    [Header("Stats Display (optional)")]
//////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////    }

//////////////    private void OnDestroy()
//////////////    {
//////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////    }

//////////////    private void OnEnable()
//////////////    {
//////////////        if (soldierEquipment == null)
//////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////        ForceAllGroupsActive(true);
//////////////        BuildGroupCache();
//////////////        InitAllButtons();
//////////////        ApplyDefaultSelections();
//////////////        ShowSlot(_activeSlot);
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////    {
//////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////        if (eq == null) return;
//////////////        soldierEquipment = eq;
//////////////        if (!gameObject.activeInHierarchy) return;
//////////////        ForceAllGroupsActive(true);
//////////////        InitAllButtons();
//////////////        ApplyDefaultSelections();
//////////////        ShowSlot(_activeSlot);
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////    private void BuildGroupCache()
//////////////    {
//////////////        _groups.Clear();
//////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////////        Cache(EquipmentSlot.Face, groupHead);
//////////////        Cache(EquipmentSlot.Hair, groupHair);
//////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////////    }

//////////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////////    {
//////////////        var list = new List<InventorySlotButton>();
//////////////        if (group != null)
//////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////        _groups[slot] = list;
//////////////    }

//////////////    private void InitAllButtons()
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////            foreach (var btn in kvp.Value)
//////////////                btn.Init(this, soldierEquipment);
//////////////    }

//////////////    // ─── Default Selections ───────────────────────────────────────────────────

//////////////    private void ApplyDefaultSelections()
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////        {
//////////////            var slot = kvp.Key;
//////////////            var list = kvp.Value;
//////////////            if (list.Count == 0) continue;

//////////////            if (slot == EquipmentSlot.BodyType)
//////////////            {
//////////////                // Select only the isDefault button (Skinny Body), deselect rest
//////////////                foreach (var btn in list)
//////////////                {
//////////////                    if (btn.IsDefault) btn.Select();
//////////////                    else btn.Deselect();
//////////////                }
//////////////            }
//////////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//////////////            {
//////////////                // Auto-select the first button — but it CAN be deselected later
//////////////                for (int i = 0; i < list.Count; i++)
//////////////                {
//////////////                    if (i == 0) list[i].Select();
//////////////                    else list[i].Deselect();
//////////////                }
//////////////            }
//////////////            else
//////////////            {
//////////////                // Armor, Helmet, Weapon — nothing selected by default
//////////////                foreach (var btn in list)
//////////////                    btn.Deselect();
//////////////            }
//////////////        }
//////////////    }

//////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////////    public void SelectButton(InventorySlotButton btn)
//////////////    {
//////////////        var list = FindGroup(btn);
//////////////        if (list == null) return;

//////////////        foreach (var b in list)
//////////////        {
//////////////            if (b == btn) b.Select();
//////////////            else if (!b.IsDefault) b.Deselect();
//////////////        }

//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    public void DeselectButton(InventorySlotButton btn)
//////////////    {
//////////////        if (btn.IsDefault) return;   // Skinny Body can never be deselected
//////////////        btn.Deselect();
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////////    private void ApplyHairHelmetRule()
//////////////    {
//////////////        if (hairVisualRoot == null) return;

//////////////        bool helmetOn = false;
//////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////////            foreach (var btn in helmetGroup)
//////////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////////        hairVisualRoot.SetActive(!helmetOn);
//////////////    }

//////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////    private void ShowSlot(EquipmentSlot slot)
//////////////    {
//////////////        _activeSlot = slot;
//////////////        UpdateTabColours();
//////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////    }

//////////////    private void ForceAllGroupsActive(bool active)
//////////////    {
//////////////        SetActive(groupPlayer, active);
//////////////        SetActive(groupHead, active);
//////////////        SetActive(groupHair, active);
//////////////        SetActive(groupHelmet, active);
//////////////        SetActive(groupArmor, active);
//////////////        SetActive(groupWeapon, active);
//////////////    }

//////////////    private static void SetActive(GameObject go, bool active)
//////////////    {
//////////////        if (go != null) go.SetActive(active);
//////////////    }

//////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////    private void UpdateTabColours()
//////////////    {
//////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////////        Tint(tabHead, EquipmentSlot.Face);
//////////////        Tint(tabHair, EquipmentSlot.Hair);
//////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////////    }

//////////////    private void Tint(Button btn, EquipmentSlot slot)
//////////////    {
//////////////        if (btn == null) return;
//////////////        var img = btn.GetComponent<Image>();
//////////////        if (img != null)
//////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////    }

//////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////            foreach (var btn in kvp.Value)
//////////////                if (btn == target) return kvp.Value;
//////////////        return null;
//////////////    }

//////////////    private void RefreshStats()
//////////////    {
//////////////        if (soldierEquipment == null) return;
//////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////////        if (s == null) return;
//////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////////    }

//////////////    public void Open() => gameObject.SetActive(true);
//////////////    public void Close() => gameObject.SetActive(false);
//////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////}

////////////using System.Collections.Generic;
////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using TMPro;

/////////////// <summary>
/////////////// AREA FORGE - InventoryPanel
///////////////
/////////////// ── Selection rules ──────────────────────────────────────────────────────────
///////////////   • Body (Skinny)              → isDefault = true → always selected, locked
///////////////   • Face (first) / Hair (first)→ auto-selected on open, can be deselected
///////////////   • Armor / Helmet / Weapon    → nothing selected by default, click to toggle
///////////////
/////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////////   Drag Player/Hair into hairVisualRoot.
///////////////   Helmet selected   → hairVisualRoot hides
///////////////   Helmet deselected → hairVisualRoot shows
/////////////// </summary>
////////////public class InventoryPanel : MonoBehaviour
////////////{
////////////    [Header("Soldier — leave empty, found automatically")]
////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////    [Header("Item Groups — one parent GO per slot")]
////////////    [SerializeField] private GameObject groupPlayer;
////////////    [SerializeField] private GameObject groupHead;
////////////    [SerializeField] private GameObject groupHair;
////////////    [SerializeField] private GameObject groupHelmet;
////////////    [SerializeField] private GameObject groupArmor;
////////////    [SerializeField] private GameObject groupWeapon;

////////////    [Header("Helmet hides Hair")]
////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////////    [SerializeField] private GameObject hairVisualRoot;

////////////    [Header("Tab Buttons")]
////////////    [SerializeField] private Button tabPlayer;
////////////    [SerializeField] private Button tabHead;
////////////    [SerializeField] private Button tabHair;
////////////    [SerializeField] private Button tabHelmet;
////////////    [SerializeField] private Button tabArmor;
////////////    [SerializeField] private Button tabWeapon;

////////////    [Header("Tab Colours")]
////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////    [Header("Stats Display (optional)")]
////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////    [SerializeField] private TextMeshProUGUI apText;
////////////    [SerializeField] private TextMeshProUGUI adText;

////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////    }

////////////    private void OnDestroy()
////////////    {
////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////    }

////////////    private void OnEnable()
////////////    {
////////////        if (soldierEquipment == null)
////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////        ForceAllGroupsActive(true);
////////////        BuildGroupCache();
////////////        InitAllButtons();
////////////        ApplyDefaultSelections();
////////////        ShowSlot(_activeSlot);
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////    {
////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////        if (eq == null) return;
////////////        soldierEquipment = eq;
////////////        if (!gameObject.activeInHierarchy) return;
////////////        ForceAllGroupsActive(true);
////////////        InitAllButtons();
////////////        ApplyDefaultSelections();
////////////        ShowSlot(_activeSlot);
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////    private void BuildGroupCache()
////////////    {
////////////        _groups.Clear();
////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////        Cache(EquipmentSlot.Face, groupHead);
////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////    }

////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////    {
////////////        var list = new List<InventorySlotButton>();
////////////        if (group != null)
////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////        _groups[slot] = list;
////////////    }

////////////    private void InitAllButtons()
////////////    {
////////////        foreach (var kvp in _groups)
////////////            foreach (var btn in kvp.Value)
////////////                btn.Init(this, soldierEquipment);
////////////    }

////////////    // ─── Default Selections ───────────────────────────────────────────────────

////////////    private void ApplyDefaultSelections()
////////////    {
////////////        foreach (var kvp in _groups)
////////////        {
////////////            var slot = kvp.Key;
////////////            var list = kvp.Value;
////////////            if (list.Count == 0) continue;

////////////            if (slot == EquipmentSlot.BodyType)
////////////            {
////////////                // Only the isDefault button (Skinny Body) stays selected
////////////                foreach (var btn in list)
////////////                {
////////////                    if (btn.IsDefault) btn.Select();
////////////                    else btn.Deselect();
////////////                }
////////////            }
////////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////////////            {
////////////                // First button selected by default — but CAN be deselected later
////////////                for (int i = 0; i < list.Count; i++)
////////////                {
////////////                    if (i == 0) list[i].Select();
////////////                    else list[i].Deselect();
////////////                }
////////////            }
////////////            else
////////////            {
////////////                // Armor, Helmet, Weapon — nothing selected by default
////////////                foreach (var btn in list)
////////////                    btn.Deselect();
////////////            }
////////////        }
////////////    }

////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////    /// <summary>
////////////    /// Selects btn and deselects ALL other buttons in the same group.
////////////    /// The isDefault (Skinny Body) is protected inside Deselect() itself
////////////    /// so it is safe to call Deselect() on every other button here.
////////////    /// </summary>
////////////    public void SelectButton(InventorySlotButton btn)
////////////    {
////////////        var list = FindGroup(btn);
////////////        if (list == null) return;

////////////        foreach (var b in list)
////////////        {
////////////            if (b == btn) b.Select();
////////////            else b.Deselect();   // Deselect() ignores isDefault buttons internally
////////////        }

////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    /// <summary>
////////////    /// Deselects btn only — user clicked an already-selected item.
////////////    /// </summary>
////////////    public void DeselectButton(InventorySlotButton btn)
////////////    {
////////////        if (btn.IsDefault) return;
////////////        btn.Deselect();
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////////    private void ApplyHairHelmetRule()
////////////    {
////////////        if (hairVisualRoot == null) return;

////////////        bool helmetOn = false;
////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////////            foreach (var btn in helmetGroup)
////////////                if (btn.IsSelected) { helmetOn = true; break; }

////////////        hairVisualRoot.SetActive(!helmetOn);
////////////    }

////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////    private void ShowSlot(EquipmentSlot slot)
////////////    {
////////////        _activeSlot = slot;
////////////        UpdateTabColours();
////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////    }

////////////    private void ForceAllGroupsActive(bool active)
////////////    {
////////////        SetActive(groupPlayer, active);
////////////        SetActive(groupHead, active);
////////////        SetActive(groupHair, active);
////////////        SetActive(groupHelmet, active);
////////////        SetActive(groupArmor, active);
////////////        SetActive(groupWeapon, active);
////////////    }

////////////    private static void SetActive(GameObject go, bool active)
////////////    {
////////////        if (go != null) go.SetActive(active);
////////////    }

////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////    private void UpdateTabColours()
////////////    {
////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////        Tint(tabHead, EquipmentSlot.Face);
////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////    }

////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////    {
////////////        if (btn == null) return;
////////////        var img = btn.GetComponent<Image>();
////////////        if (img != null)
////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////    }

////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////    {
////////////        foreach (var kvp in _groups)
////////////            foreach (var btn in kvp.Value)
////////////                if (btn == target) return kvp.Value;
////////////        return null;
////////////    }

////////////    private void RefreshStats()
////////////    {
////////////        if (soldierEquipment == null) return;
////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////        if (s == null) return;
////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////    }

////////////    public void Open() => gameObject.SetActive(true);
////////////    public void Close() => gameObject.SetActive(false);
////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////}

//////////using System.Collections.Generic;
//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using TMPro;

///////////// <summary>
///////////// AREA FORGE - InventoryPanel
/////////////
///////////// ── Selection rules ──────────────────────────────────────────────────────────
/////////////   • Body (Skinny)              → isDefault = true → always selected, locked
/////////////   • Face (first) / Hair (first)→ auto-selected on open, can be deselected
/////////////   • Armor / Helmet / Weapon    → nothing selected by default, click to toggle
/////////////
///////////// ── Stats display ────────────────────────────────────────────────────────────
/////////////   Drag the three bar fill Images (healthBarFill, abilityBarFill, damageBarFill)
/////////////   and the three text labels into the Inspector.
/////////////   These update live whenever equipment changes.
/////////////
///////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////   Drag Player/Hair into hairVisualRoot — hidden when any helmet is selected.
///////////// </summary>
//////////public class InventoryPanel : MonoBehaviour
//////////{
//////////    [Header("Soldier — leave empty, found automatically")]
//////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////    [Header("Item Groups — one parent GO per slot")]
//////////    [SerializeField] private GameObject groupPlayer;
//////////    [SerializeField] private GameObject groupHead;
//////////    [SerializeField] private GameObject groupHair;
//////////    [SerializeField] private GameObject groupHelmet;
//////////    [SerializeField] private GameObject groupArmor;
//////////    [SerializeField] private GameObject groupWeapon;

//////////    [Header("Helmet hides Hair")]
//////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////////    [SerializeField] private GameObject hairVisualRoot;

//////////    [Header("Tab Buttons")]
//////////    [SerializeField] private Button tabPlayer;
//////////    [SerializeField] private Button tabHead;
//////////    [SerializeField] private Button tabHair;
//////////    [SerializeField] private Button tabHelmet;
//////////    [SerializeField] private Button tabArmor;
//////////    [SerializeField] private Button tabWeapon;

//////////    [Header("Tab Colours")]
//////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////    [Header("Stats — Bar Fills (Image Type: Filled → Horizontal → Left)")]
//////////    [SerializeField] private Image healthBarFill;
//////////    [SerializeField] private Image abilityBarFill;
//////////    [SerializeField] private Image damageBarFill;

//////////    [Header("Stats — Text Labels (TextMeshPro)")]
//////////    [SerializeField] private TextMeshProUGUI healthText;
//////////    [SerializeField] private TextMeshProUGUI abilityText;
//////////    [SerializeField] private TextMeshProUGUI damageText;

//////////    [Header("Bar Max Reference Values")]
//////////    [Tooltip("Ability value that = 100% full bar")]
//////////    [SerializeField] private float maxAbilityDisplay = 100f;
//////////    [Tooltip("Damage value that = 100% full bar")]
//////////    [SerializeField] private float maxDamageDisplay = 100f;

//////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////        UnsubscribeStats();
//////////    }

//////////    private void OnEnable()
//////////    {
//////////        if (soldierEquipment == null)
//////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////        SubscribeStats();
//////////        ForceAllGroupsActive(true);
//////////        BuildGroupCache();
//////////        InitAllButtons();
//////////        ApplyDefaultSelections();
//////////        ShowSlot(_activeSlot);
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    private void OnDisable()
//////////    {
//////////        UnsubscribeStats();
//////////    }

//////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////    {
//////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////        if (eq == null) return;
//////////        UnsubscribeStats();
//////////        soldierEquipment = eq;
//////////        if (!gameObject.activeInHierarchy) return;
//////////        SubscribeStats();
//////////        ForceAllGroupsActive(true);
//////////        InitAllButtons();
//////////        ApplyDefaultSelections();
//////////        ShowSlot(_activeSlot);
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    // ─── Stats subscription ───────────────────────────────────────────────────

//////////    private void SubscribeStats()
//////////    {
//////////        var stats = GetStats();
//////////        if (stats != null) stats.OnStatsChanged += OnStatsChanged;
//////////    }

//////////    private void UnsubscribeStats()
//////////    {
//////////        var stats = GetStats();
//////////        if (stats != null) stats.OnStatsChanged -= OnStatsChanged;
//////////    }

//////////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

//////////    private SoldierStats GetStats() =>
//////////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

//////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////    private void BuildGroupCache()
//////////    {
//////////        _groups.Clear();
//////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////        Cache(EquipmentSlot.Face, groupHead);
//////////        Cache(EquipmentSlot.Hair, groupHair);
//////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////    }

//////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////    {
//////////        var list = new List<InventorySlotButton>();
//////////        if (group != null)
//////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////        _groups[slot] = list;
//////////    }

//////////    private void InitAllButtons()
//////////    {
//////////        foreach (var kvp in _groups)
//////////            foreach (var btn in kvp.Value)
//////////                btn.Init(this, soldierEquipment);
//////////    }

//////////    // ─── Default Selections ───────────────────────────────────────────────────

//////////    private void ApplyDefaultSelections()
//////////    {
//////////        foreach (var kvp in _groups)
//////////        {
//////////            var slot = kvp.Key;
//////////            var list = kvp.Value;
//////////            if (list.Count == 0) continue;

//////////            if (slot == EquipmentSlot.BodyType)
//////////            {
//////////                foreach (var btn in list)
//////////                {
//////////                    if (btn.IsDefault) btn.Select();
//////////                    else btn.Deselect();
//////////                }
//////////            }
//////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//////////            {
//////////                for (int i = 0; i < list.Count; i++)
//////////                {
//////////                    if (i == 0) list[i].Select();
//////////                    else list[i].Deselect();
//////////                }
//////////            }
//////////            else
//////////            {
//////////                foreach (var btn in list) btn.Deselect();
//////////            }
//////////        }
//////////    }

//////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////    public void SelectButton(InventorySlotButton btn)
//////////    {
//////////        var list = FindGroup(btn);
//////////        if (list == null) return;

//////////        // Select the tapped button, deselect all others
//////////        foreach (var b in list)
//////////        {
//////////            if (b == btn) b.Select();
//////////            else b.Deselect();
//////////        }

//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    public void DeselectButton(InventorySlotButton btn)
//////////    {
//////////        if (btn.IsDefault) return;
//////////        btn.Deselect();
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////    private void ApplyHairHelmetRule()
//////////    {
//////////        if (hairVisualRoot == null) return;

//////////        bool helmetOn = false;
//////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////            foreach (var btn in helmetGroup)
//////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////        hairVisualRoot.SetActive(!helmetOn);
//////////    }

//////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////    private void ShowSlot(EquipmentSlot slot)
//////////    {
//////////        _activeSlot = slot;
//////////        UpdateTabColours();
//////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////    }

//////////    private void ForceAllGroupsActive(bool active)
//////////    {
//////////        SetActive(groupPlayer, active);
//////////        SetActive(groupHead, active);
//////////        SetActive(groupHair, active);
//////////        SetActive(groupHelmet, active);
//////////        SetActive(groupArmor, active);
//////////        SetActive(groupWeapon, active);
//////////    }

//////////    private static void SetActive(GameObject go, bool active)
//////////    {
//////////        if (go != null) go.SetActive(active);
//////////    }

//////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////    private void UpdateTabColours()
//////////    {
//////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////        Tint(tabHead, EquipmentSlot.Face);
//////////        Tint(tabHair, EquipmentSlot.Hair);
//////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////    }

//////////    private void Tint(Button btn, EquipmentSlot slot)
//////////    {
//////////        if (btn == null) return;
//////////        var img = btn.GetComponent<Image>();
//////////        if (img != null)
//////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////    }

//////////    // ─── Stats Refresh ────────────────────────────────────────────────────────

//////////    private void RefreshStats()
//////////    {
//////////        var stats = GetStats();
//////////        if (stats == null) return;

//////////        // Health
//////////        float hp = stats.HealthPercent;
//////////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
//////////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

//////////        // Ability
//////////        float ap = Mathf.Clamp01(stats.AbilityPower / maxAbilityDisplay);
//////////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//////////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

//////////        // Damage
//////////        float ad = Mathf.Clamp01(stats.AttackDamage / maxDamageDisplay);
//////////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
//////////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//////////    }

//////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////    {
//////////        foreach (var kvp in _groups)
//////////            foreach (var btn in kvp.Value)
//////////                if (btn == target) return kvp.Value;
//////////        return null;
//////////    }

//////////    public void Open() => gameObject.SetActive(true);
//////////    public void Close() => gameObject.SetActive(false);
//////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////}

////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.UI;
////////using TMPro;

/////////// <summary>
/////////// AREA FORGE - InventoryPanel (Army / Customize Panel)
///////////
/////////// ── What it does ─────────────────────────────────────────────────────────────
///////////   Shows one tab per equipment slot (Armor, Helmet, Weapon, Hair, etc.).
///////////   Clicking a tab shows that slot's item buttons.
///////////   Clicking an item button equips it → stats bars update instantly.
///////////
/////////// ── Stat bar sync (how it works) ─────────────────────────────────────────────
///////////   Each EquipmentItem ScriptableObject has healthBonus / abilityBonus / damageBonus.
///////////   When you click an item:
///////////     1. InventorySlotButton.Select() → CharacterEquipment.Equip(item)
///////////     2. CharacterEquipment calls SoldierStats.ApplyEquipmentBonus(+bonuses)
///////////        and removes the old item's bonuses first (-oldBonuses)
///////////     3. SoldierStats fires OnStatsChanged
///////////     4. InventoryPanel.OnStatsChanged() → RefreshStats() → bars + text update
///////////
///////////   So ALL you need to do in Unity is:
///////////     a) Set healthBonus / abilityBonus / damageBonus on each EquipmentItem asset
///////////     b) Wire the three bar Images and text labels in the Inspector (see below)
///////////
/////////// ── Inspector wiring ─────────────────────────────────────────────────────────
///////////   soldierEquipment → leave EMPTY — found automatically at runtime
///////////
///////////   Item Groups → drag each slot's button-parent GO:
///////////     groupPlayer  = BodyType buttons parent
///////////     groupHead    = Face buttons parent
///////////     groupHair    = Hair buttons parent
///////////     groupHelmet  = Helmet buttons parent
///////////     groupArmor   = Armor buttons parent
///////////     groupWeapon  = Weapon buttons parent
///////////
///////////   Tab Buttons → drag each tab Button (one per slot)
///////////
///////////   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
///////////
///////////   ── Stats Panel (wire these to see live stat bars) ────────────────────────
///////////   healthBarFill  → Image (Filled, Horizontal, Left) for health
///////////   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
///////////   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
///////////   healthText     → TextMeshProUGUI (shows MaxHealth number)
///////////   abilityText    → TextMeshProUGUI (shows AbilityPower number)
///////////   damageText     → TextMeshProUGUI (shows AttackDamage number)
///////////   maxAbilityDisplay → value that = 100% full ability bar (e.g. 100)
///////////   maxDamageDisplay  → value that = 100% full damage  bar (e.g. 100)
///////////
/////////// ── EquipmentItem stat fields to fill in the Project window ──────────────────
///////////   For each armor/helmet/weapon asset, set:
///////////     Health Bonus   → e.g. 20 for heavy armor
///////////     Ability Bonus  → e.g. 15 for a magic staff
///////////     Damage Bonus   → e.g. 10 for a sword
///////////   Cosmetic-only items (hair, face, body) can stay at 0.
/////////// </summary>
////////public class InventoryPanel : MonoBehaviour
////////{
////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////////    [Header("Soldier — leave empty, found automatically")]
////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////    [Header("Item Groups — one parent GO per slot")]
////////    [SerializeField] private GameObject groupPlayer;   // BodyType
////////    [SerializeField] private GameObject groupHead;     // Face
////////    [SerializeField] private GameObject groupHair;
////////    [SerializeField] private GameObject groupHelmet;
////////    [SerializeField] private GameObject groupArmor;
////////    [SerializeField] private GameObject groupWeapon;

////////    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

////////    [Header("Helmet hides Hair")]
////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////    [SerializeField] private GameObject hairVisualRoot;

////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////////    [Header("Tab Buttons")]
////////    [SerializeField] private Button tabPlayer;
////////    [SerializeField] private Button tabHead;
////////    [SerializeField] private Button tabHair;
////////    [SerializeField] private Button tabHelmet;
////////    [SerializeField] private Button tabArmor;
////////    [SerializeField] private Button tabWeapon;

////////    [Header("Tab Colours")]
////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////    // ─── Inspector — Stat Bars (the Army-panel HUD) ───────────────────────────

////////    [Header("Stats Bars — wire these to see live stat changes on equip")]
////////    [Tooltip("Image (Filled, Horizontal, Left) — health bar in the Army panel")]
////////    [SerializeField] private Image healthBarFill;
////////    [Tooltip("Image (Filled, Horizontal, Left) — ability power bar")]
////////    [SerializeField] private Image abilityBarFill;
////////    [Tooltip("Image (Filled, Horizontal, Left) — attack damage bar")]
////////    [SerializeField] private Image damageBarFill;

////////    [Header("Stats Labels (TextMeshPro — optional)")]
////////    [SerializeField] private TextMeshProUGUI healthText;
////////    [SerializeField] private TextMeshProUGUI abilityText;
////////    [SerializeField] private TextMeshProUGUI damageText;

////////    [Header("Bar Max Reference Values")]
////////    [Tooltip("Ability value that fills the bar to 100%. E.g. 100.")]
////////    [SerializeField] private float maxAbilityDisplay = 100f;
////////    [Tooltip("Damage value that fills the bar to 100%. E.g. 100.")]
////////    [SerializeField] private float maxDamageDisplay = 100f;

////////    // ─── Private ──────────────────────────────────────────────────────────────

////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        // Wire tab clicks
////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////    }

////////    private void OnDestroy()
////////    {
////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////        UnsubscribeStats();
////////    }

////////    private void OnEnable()
////////    {
////////        // Try to find the soldier if not already set
////////        if (soldierEquipment == null)
////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////        SubscribeStats();

////////        // All groups must be active before Init() so Awake runs on every button
////////        ForceAllGroupsActive(true);
////////        BuildGroupCache();
////////        InitAllButtons();
////////        ApplyDefaultSelections();

////////        // Restore correct group visibility and tab colour
////////        ShowSlot(_activeSlot);
////////        ApplyHairHelmetRule();

////////        // Immediately show the current stats (base + any already-equipped bonuses)
////////        RefreshStats();
////////    }

////////    private void OnDisable()
////////    {
////////        UnsubscribeStats();
////////    }

////////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

////////    private void OnSoldierSpawned(GameObject soldierGO)
////////    {
////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////        if (eq == null) return;

////////        UnsubscribeStats();
////////        soldierEquipment = eq;

////////        if (!gameObject.activeInHierarchy) return;

////////        SubscribeStats();
////////        ForceAllGroupsActive(true);
////////        InitAllButtons();
////////        ApplyDefaultSelections();
////////        ShowSlot(_activeSlot);
////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    // ─── Stats Subscription ───────────────────────────────────────────────────

////////    /// <summary>
////////    /// Subscribes to OnStatsChanged so the bars update the moment an item
////////    /// is equipped or unequipped (triggered from CharacterEquipment.Equip/Unequip
////////    /// → SoldierStats.ApplyEquipmentBonus → OnStatsChanged).
////////    /// </summary>
////////    private void SubscribeStats()
////////    {
////////        var s = GetStats();
////////        if (s != null) s.OnStatsChanged += OnStatsChanged;
////////    }

////////    private void UnsubscribeStats()
////////    {
////////        var s = GetStats();
////////        if (s != null) s.OnStatsChanged -= OnStatsChanged;
////////    }

////////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

////////    private SoldierStats GetStats() =>
////////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////    private void BuildGroupCache()
////////    {
////////        _groups.Clear();
////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////        Cache(EquipmentSlot.Face, groupHead);
////////        Cache(EquipmentSlot.Hair, groupHair);
////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////        Cache(EquipmentSlot.Armor, groupArmor);
////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////    }

////////    private void Cache(EquipmentSlot slot, GameObject group)
////////    {
////////        var list = new List<InventorySlotButton>();
////////        if (group != null)
////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////        _groups[slot] = list;
////////    }

////////    // ─── Button Init ──────────────────────────────────────────────────────────

////////    private void InitAllButtons()
////////    {
////////        foreach (var kvp in _groups)
////////            foreach (var btn in kvp.Value)
////////                btn.Init(this, soldierEquipment);
////////    }

////////    // ─── Default Selections ───────────────────────────────────────────────────

////////    private void ApplyDefaultSelections()
////////    {
////////        foreach (var kvp in _groups)
////////        {
////////            var slot = kvp.Key;
////////            var list = kvp.Value;
////////            if (list.Count == 0) continue;

////////            if (slot == EquipmentSlot.BodyType)
////////            {
////////                // Skinny Body is isDefault — always selected, cannot be deselected
////////                foreach (var btn in list)
////////                {
////////                    if (btn.IsDefault) btn.Select();
////////                    else btn.Deselect();
////////                }
////////            }
////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////////            {
////////                // First button selected by default — can be deselected later
////////                for (int i = 0; i < list.Count; i++)
////////                {
////////                    if (i == 0) list[i].Select();
////////                    else list[i].Deselect();
////////                }
////////            }
////////            else
////////            {
////////                // Armor, Helmet, Weapon — nothing selected by default
////////                foreach (var btn in list) btn.Deselect();
////////            }
////////        }
////////    }

////////    // ─── Public API (called by InventorySlotButton) ───────────────────────────

////////    /// <summary>
////////    /// Selects btn and deselects all others in the same slot group.
////////    /// The stats bars update automatically via the OnStatsChanged event.
////////    /// </summary>
////////    public void SelectButton(InventorySlotButton btn)
////////    {
////////        var list = FindGroup(btn);
////////        if (list == null) return;

////////        foreach (var b in list)
////////        {
////////            if (b == btn) b.Select();
////////            else b.Deselect();
////////        }

////////        ApplyHairHelmetRule();
////////        // RefreshStats is called via OnStatsChanged event — no need to call it here
////////        // but we call it defensively in case the item has no stat bonuses (no event fires)
////////        RefreshStats();
////////    }

////////    /// <summary>
////////    /// Deselects btn only — user clicked an already-selected, non-default item.
////////    /// </summary>
////////    public void DeselectButton(InventorySlotButton btn)
////////    {
////////        if (btn.IsDefault) return;
////////        btn.Deselect();
////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////    private void ApplyHairHelmetRule()
////////    {
////////        if (hairVisualRoot == null) return;

////////        bool helmetOn = false;
////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////            foreach (var btn in helmetGroup)
////////                if (btn.IsSelected) { helmetOn = true; break; }

////////        hairVisualRoot.SetActive(!helmetOn);
////////    }

////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////    private void ShowSlot(EquipmentSlot slot)
////////    {
////////        _activeSlot = slot;
////////        UpdateTabColours();
////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////    }

////////    private void ForceAllGroupsActive(bool active)
////////    {
////////        SetActive(groupPlayer, active);
////////        SetActive(groupHead, active);
////////        SetActive(groupHair, active);
////////        SetActive(groupHelmet, active);
////////        SetActive(groupArmor, active);
////////        SetActive(groupWeapon, active);
////////    }

////////    private static void SetActive(GameObject go, bool active)
////////    {
////////        if (go != null) go.SetActive(active);
////////    }

////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////    private void UpdateTabColours()
////////    {
////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////        Tint(tabHead, EquipmentSlot.Face);
////////        Tint(tabHair, EquipmentSlot.Hair);
////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////        Tint(tabArmor, EquipmentSlot.Armor);
////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////    }

////////    private void Tint(Button btn, EquipmentSlot slot)
////////    {
////////        if (btn == null) return;
////////        var img = btn.GetComponent<Image>();
////////        if (img != null)
////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////    }

////////    // ─── Stats Refresh ────────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Updates all three stat bars and labels from the current SoldierStats values
////////    /// (base stats + equipment bonuses).  Called on every equip/unequip and on open.
////////    /// </summary>
////////    private void RefreshStats()
////////    {
////////        var stats = GetStats();
////////        if (stats == null) return;

////////        // ── Health ────────────────────────────────────────────────────────────
////////        float hp = stats.HealthPercent;   // 0–1
////////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
////////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

////////        // ── Ability Power ─────────────────────────────────────────────────────
////////        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
////////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
////////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

////////        // ── Attack Damage ─────────────────────────────────────────────────────
////////        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
////////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
////////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
////////    }

////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////    {
////////        foreach (var kvp in _groups)
////////            foreach (var btn in kvp.Value)
////////                if (btn == target) return kvp.Value;
////////        return null;
////////    }

////////    // ─── Open / Close ─────────────────────────────────────────────────────────

////////    public void Open() => gameObject.SetActive(true);
////////    public void Close() => gameObject.SetActive(false);
////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////}

////////////////////////////using System.Collections.Generic;
////////////////////////////using UnityEngine;
////////////////////////////using UnityEngine.UI;
////////////////////////////using TMPro;

/////////////////////////////// <summary>
/////////////////////////////// AREA FORGE - InventoryPanel
///////////////////////////////
/////////////////////////////// The main inventory window. Shows one tab per equipment slot.
/////////////////////////////// Clicking a tab filters the item grid to show only items for that slot.
/////////////////////////////// Clicking an item equips it on the soldier instantly.
///////////////////////////////
/////////////////////////////// ── UI Hierarchy to build ────────────────────────────────────────────────────
///////////////////////////////
///////////////////////////////   InventoryPanel  (Panel)           ← InventoryPanel.cs
///////////////////////////////     ├── TabBar    (HorizontalLayoutGroup)
///////////////////////////////     │     ├── Tab_BodyType  (Button + TabButton.cs)
///////////////////////////////     │     ├── Tab_Face      (Button + TabButton.cs)
///////////////////////////////     │     ├── Tab_Hair      (Button + TabButton.cs)
///////////////////////////////     │     ├── Tab_Helmet    (Button + TabButton.cs)
///////////////////////////////     │     ├── Tab_Armor     (Button + TabButton.cs)
///////////////////////////////     │     └── Tab_Weapon    (Button + TabButton.cs)
///////////////////////////////     ├── ItemGrid  (ScrollRect → Viewport → Content)
///////////////////////////////     │     └── Content  (GridLayoutGroup)  ← SlotButton prefabs spawn here
///////////////////////////////     └── StatsPreview (optional)
///////////////////////////////           ├── HPText   (TextMeshProUGUI)
///////////////////////////////           ├── APText   (TextMeshProUGUI)
///////////////////////////////           └── ADText   (TextMeshProUGUI)
///////////////////////////////
/////////////////////////////// ── Inspector fields ─────────────────────────────────────────────────────────
///////////////////////////////   • soldierEquipment  → drag the SolderPrefab (or its CharacterEquipment)
///////////////////////////////   • allItems[]        → drag ALL your EquipmentItem ScriptableObject assets
///////////////////////////////   • slotButtonPrefab  → drag the SlotButton prefab
///////////////////////////////   • gridContent       → drag the Content object inside the ScrollRect
///////////////////////////////   • tab buttons       → drag each Tab_XXX button
/////////////////////////////// </summary>
////////////////////////////public class InventoryPanel : MonoBehaviour
////////////////////////////{
////////////////////////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////////////////////////    [Header("Soldier Reference")]
////////////////////////////    [Tooltip("The soldier whose equipment this panel controls")]
////////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////////////    [Header("All Equipment Items")]
////////////////////////////    [Tooltip("Drag every EquipmentItem ScriptableObject asset here")]
////////////////////////////    [SerializeField] private EquipmentItem[] allItems;

////////////////////////////    [Header("Grid")]
////////////////////////////    [Tooltip("The SlotButton prefab (has InventorySlotButton + Button + Image)")]
////////////////////////////    [SerializeField] private GameObject slotButtonPrefab;
////////////////////////////    [Tooltip("GridLayoutGroup Content object inside the ScrollRect")]
////////////////////////////    [SerializeField] private Transform gridContent;

////////////////////////////    [Header("Tab Buttons (one per slot — order must match EquipmentSlot enum)")]
////////////////////////////    [SerializeField] private Button tabBodyType;
////////////////////////////    [SerializeField] private Button tabFace;
////////////////////////////    [SerializeField] private Button tabHair;
////////////////////////////    [SerializeField] private Button tabHelmet;
////////////////////////////    [SerializeField] private Button tabArmor;
////////////////////////////    [SerializeField] private Button tabWeapon;

////////////////////////////    [Header("Tab Active Colour")]
////////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////////////    [Header("Stats Preview (optional — leave null to skip)")]
////////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////////////////////    private readonly List<InventorySlotButton> _spawnedButtons = new();

////////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////////////    private void Awake()
////////////////////////////    {
////////////////////////////        // Wire tab buttons
////////////////////////////        tabBodyType?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////////////        tabFace?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));
////////////////////////////    }

////////////////////////////    private void OnEnable()
////////////////////////////    {
////////////////////////////        // Refresh stats display whenever the panel is opened
////////////////////////////        if (soldierEquipment != null)
////////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;

////////////////////////////        ShowSlot(_activeSlot);
////////////////////////////        RefreshStats();
////////////////////////////    }

////////////////////////////    private void OnDisable()
////////////////////////////    {
////////////////////////////        if (soldierEquipment != null)
////////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////////////////////    }

////////////////////////////    // ─── Tab Logic ────────────────────────────────────────────────────────────

////////////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////////////    {
////////////////////////////        _activeSlot = slot;
////////////////////////////        UpdateTabColours();
////////////////////////////        PopulateGrid(slot);
////////////////////////////    }

////////////////////////////    private void UpdateTabColours()
////////////////////////////    {
////////////////////////////        SetTabColour(tabBodyType, EquipmentSlot.BodyType);
////////////////////////////        SetTabColour(tabFace, EquipmentSlot.Face);
////////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
////////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
////////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
////////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
////////////////////////////    }

////////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
////////////////////////////    {
////////////////////////////        if (btn == null) return;
////////////////////////////        var img = btn.GetComponent<Image>();
////////////////////////////        if (img != null)
////////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////////////    }

////////////////////////////    // ─── Grid Population ──────────────────────────────────────────────────────

////////////////////////////    private void PopulateGrid(EquipmentSlot slot)
////////////////////////////    {
////////////////////////////        // Clear existing buttons
////////////////////////////        foreach (var btn in _spawnedButtons)
////////////////////////////            if (btn != null) Destroy(btn.gameObject);
////////////////////////////        _spawnedButtons.Clear();

////////////////////////////        // Spawn one button per item that matches this slot
////////////////////////////        foreach (var item in allItems)
////////////////////////////        {
////////////////////////////            if (item == null || item.slot != slot) continue;

////////////////////////////            var go = Instantiate(slotButtonPrefab, gridContent);
////////////////////////////            var btn = go.GetComponent<InventorySlotButton>();
////////////////////////////            if (btn != null)
////////////////////////////            {
////////////////////////////                btn.Setup(item, soldierEquipment, this);
////////////////////////////                _spawnedButtons.Add(btn);
////////////////////////////            }
////////////////////////////        }
////////////////////////////    }

////////////////////////////    // ─── Button Refresh ───────────────────────────────────────────────────────

////////////////////////////    /// <summary>Called by InventorySlotButton after any equip/unequip.</summary>
////////////////////////////    public void RefreshAllButtons()
////////////////////////////    {
////////////////////////////        foreach (var btn in _spawnedButtons)
////////////////////////////            btn?.RefreshSelection();

////////////////////////////        RefreshStats();
////////////////////////////    }

////////////////////////////    // ─── Stats Preview ────────────────────────────────────────────────────────

////////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
////////////////////////////        => RefreshStats();

////////////////////////////    private void RefreshStats()
////////////////////////////    {
////////////////////////////        if (soldierEquipment == null) return;
////////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
////////////////////////////        if (stats == null) return;

////////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
////////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
////////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
////////////////////////////    }

////////////////////////////    // ─── Open / Close (call from a button in the HUD) ────────────────────────

////////////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////////////}

//////////////////////////using UnityEngine;
//////////////////////////using UnityEngine.UI;
//////////////////////////using TMPro;

///////////////////////////// <summary>
///////////////////////////// AREA FORGE - InventoryPanel  (Pre-Placed Groups version)
/////////////////////////////
///////////////////////////// Your item buttons already exist in the hierarchy as pre-placed GameObjects,
///////////////////////////// organised into one group per slot:
/////////////////////////////
/////////////////////////////   Content
/////////////////////////////     ├── GROUP_Player   ← all BodyType buttons live here
/////////////////////////////     ├── GROUP_Head     ← all Face/Head buttons live here
/////////////////////////////     ├── GROUP_Hair
/////////////////////////////     ├── GROUP_Helmet
/////////////////////////////     ├── GROUP_Armor
/////////////////////////////     └── GROUP_Weapon
/////////////////////////////
///////////////////////////// This script shows the active group and hides all others when a tab is clicked.
///////////////////////////// It also injects the soldier reference into every InventorySlotButton child
///////////////////////////// so they can equip items at runtime.
/////////////////////////////
///////////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////////////////////////   1. Drag each group GameObject into the matching Group field below.
/////////////////////////////   2. Drag each tab Button into the matching Tab field.
/////////////////////////////   3. Leave soldierEquipment EMPTY — found automatically at runtime.
/////////////////////////////   4. On each pre-placed button GameObject, add InventorySlotButton and
/////////////////////////////      drag the correct EquipmentItem asset into its "Item" field.
///////////////////////////// </summary>
//////////////////////////public class InventoryPanel : MonoBehaviour
//////////////////////////{
//////////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//////////////////////////    [Header("Soldier (leave empty — found at runtime)")]
//////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////////////////////////    [Header("Item Groups — drag each slot's parent GameObject here")]
//////////////////////////    [SerializeField] private GameObject groupPlayer;   // BodyType slot
//////////////////////////    [SerializeField] private GameObject groupHead;     // Face slot
//////////////////////////    [SerializeField] private GameObject groupHair;
//////////////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////////////    [SerializeField] private GameObject groupArmor;
//////////////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//////////////////////////    [Header("Tab Buttons")]
//////////////////////////    [SerializeField] private Button tabPlayer;
//////////////////////////    [SerializeField] private Button tabHead;
//////////////////////////    [SerializeField] private Button tabHair;
//////////////////////////    [SerializeField] private Button tabHelmet;
//////////////////////////    [SerializeField] private Button tabArmor;
//////////////////////////    [SerializeField] private Button tabWeapon;

//////////////////////////    [Header("Tab Colours")]
//////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////////////    // ─── Inspector — Front-View Preview (optional) ────────────────────────────

//////////////////////////    [Header("Front-View Preview (optional)")]
//////////////////////////    [SerializeField] private CharacterPreview characterPreview;

//////////////////////////    // ─── Inspector — Stats Display (optional) ─────────────────────────────────

//////////////////////////    [Header("Stats Display (optional)")]
//////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

//////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////////////    private void Awake()
//////////////////////////    {
//////////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////////////    }

//////////////////////////    private void OnDestroy()
//////////////////////////    {
//////////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////////////        UnsubscribeEquipment();
//////////////////////////    }

//////////////////////////    private void OnEnable()
//////////////////////////    {
//////////////////////////        if (soldierEquipment == null)
//////////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////////////        SubscribeEquipment();
//////////////////////////        characterPreview?.SetEquipmentSource(soldierEquipment);
//////////////////////////        InitAllButtons();
//////////////////////////        ShowSlot(_activeSlot);
//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    private void OnDisable()
//////////////////////////    {
//////////////////////////        UnsubscribeEquipment();
//////////////////////////    }

//////////////////////////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

//////////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////////////    {
//////////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////////////        if (eq == null) return;

//////////////////////////        UnsubscribeEquipment();
//////////////////////////        soldierEquipment = eq;

//////////////////////////        if (!gameObject.activeInHierarchy) return;

//////////////////////////        SubscribeEquipment();
//////////////////////////        characterPreview?.SetEquipmentSource(soldierEquipment);
//////////////////////////        InitAllButtons();
//////////////////////////        ShowSlot(_activeSlot);
//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    // ─── Button Initialisation ────────────────────────────────────────────────

//////////////////////////    /// <summary>
//////////////////////////    /// Walks every group and calls Init() on every InventorySlotButton child
//////////////////////////    /// so each button knows which soldier to equip onto.
//////////////////////////    /// </summary>
//////////////////////////    private void InitAllButtons()
//////////////////////////    {
//////////////////////////        InitGroup(groupPlayer);
//////////////////////////        InitGroup(groupHead);
//////////////////////////        InitGroup(groupHair);
//////////////////////////        InitGroup(groupHelmet);
//////////////////////////        InitGroup(groupArmor);
//////////////////////////        InitGroup(groupWeapon);
//////////////////////////    }

//////////////////////////    private void InitGroup(GameObject group)
//////////////////////////    {
//////////////////////////        if (group == null || soldierEquipment == null) return;
//////////////////////////        foreach (var btn in group.GetComponentsInChildren<InventorySlotButton>(true))
//////////////////////////            btn.Init(soldierEquipment, this);
//////////////////////////    }

//////////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////////////    {
//////////////////////////        _activeSlot = slot;
//////////////////////////        UpdateTabColours();
//////////////////////////        ShowActiveGroup();
//////////////////////////    }

//////////////////////////    private void ShowActiveGroup()
//////////////////////////    {
//////////////////////////        SetGroupActive(groupPlayer, _activeSlot == EquipmentSlot.BodyType);
//////////////////////////        SetGroupActive(groupHead, _activeSlot == EquipmentSlot.Face);
//////////////////////////        SetGroupActive(groupHair, _activeSlot == EquipmentSlot.Hair);
//////////////////////////        SetGroupActive(groupHelmet, _activeSlot == EquipmentSlot.Helmet);
//////////////////////////        SetGroupActive(groupArmor, _activeSlot == EquipmentSlot.Armor);
//////////////////////////        SetGroupActive(groupWeapon, _activeSlot == EquipmentSlot.Weapon);
//////////////////////////    }

//////////////////////////    private static void SetGroupActive(GameObject group, bool active)
//////////////////////////    {
//////////////////////////        if (group != null) group.SetActive(active);
//////////////////////////    }

//////////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////////////    private void UpdateTabColours()
//////////////////////////    {
//////////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
//////////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
//////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
//////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
//////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
//////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
//////////////////////////    }

//////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
//////////////////////////    {
//////////////////////////        if (btn == null) return;
//////////////////////////        var img = btn.GetComponent<Image>();
//////////////////////////        if (img != null)
//////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////////////    }

//////////////////////////    // ─── Public Refresh (called by InventorySlotButton after equip) ───────────

//////////////////////////    public void RefreshAllButtons()
//////////////////////////    {
//////////////////////////        var activeGroup = GetActiveGroup();
//////////////////////////        if (activeGroup == null) return;

//////////////////////////        foreach (var btn in activeGroup.GetComponentsInChildren<InventorySlotButton>(true))
//////////////////////////            btn.RefreshSelection();

//////////////////////////        RefreshStats();
//////////////////////////    }

//////////////////////////    private GameObject GetActiveGroup() => _activeSlot switch
//////////////////////////    {
//////////////////////////        EquipmentSlot.BodyType => groupPlayer,
//////////////////////////        EquipmentSlot.Face => groupHead,
//////////////////////////        EquipmentSlot.Hair => groupHair,
//////////////////////////        EquipmentSlot.Helmet => groupHelmet,
//////////////////////////        EquipmentSlot.Armor => groupArmor,
//////////////////////////        EquipmentSlot.Weapon => groupWeapon,
//////////////////////////        _ => null
//////////////////////////    };

//////////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

//////////////////////////    private void SubscribeEquipment()
//////////////////////////    {
//////////////////////////        if (soldierEquipment != null)
//////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;
//////////////////////////    }

//////////////////////////    private void UnsubscribeEquipment()
//////////////////////////    {
//////////////////////////        if (soldierEquipment != null)
//////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////////////////    }

//////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item) => RefreshStats();

//////////////////////////    private void RefreshStats()
//////////////////////////    {
//////////////////////////        if (soldierEquipment == null) return;
//////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
//////////////////////////        if (stats == null) return;

//////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
//////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
//////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
//////////////////////////    }

//////////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

//////////////////////////    public void Open() => gameObject.SetActive(true);
//////////////////////////    public void Close() => gameObject.SetActive(false);
//////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////////////}

////////////////////////using UnityEngine;
////////////////////////using UnityEngine.UI;
////////////////////////using TMPro;

/////////////////////////// <summary>
/////////////////////////// AREA FORGE - InventoryPanel  (Pre-Placed Groups version)
///////////////////////////
/////////////////////////// Your item buttons already exist in the hierarchy as pre-placed GameObjects,
/////////////////////////// organised into one group per slot:
///////////////////////////
///////////////////////////   Content
///////////////////////////     ├── GROUP_Player   ← all BodyType buttons live here
///////////////////////////     ├── GROUP_Head     ← all Face/Head buttons live here
///////////////////////////     ├── GROUP_Hair
///////////////////////////     ├── GROUP_Helmet
///////////////////////////     ├── GROUP_Armor
///////////////////////////     └── GROUP_Weapon
///////////////////////////
/////////////////////////// This script shows the active group and hides all others when a tab is clicked.
/////////////////////////// It also injects the soldier reference into every InventorySlotButton child
/////////////////////////// so they can equip items at runtime.
///////////////////////////
/////////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
///////////////////////////   1. Drag each group GameObject into the matching Group field below.
///////////////////////////   2. Drag each tab Button into the matching Tab field.
///////////////////////////   3. Leave soldierEquipment EMPTY — found automatically at runtime.
///////////////////////////   4. On each pre-placed button GameObject, add InventorySlotButton and
///////////////////////////      drag the correct EquipmentItem asset into its "Item" field.
/////////////////////////// </summary>
////////////////////////public class InventoryPanel : MonoBehaviour
////////////////////////{
////////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////////////////////////    [Header("Soldier (leave empty — found at runtime)")]
////////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////////////////////    [Header("Item Groups — drag each slot's parent GameObject here")]
////////////////////////    [SerializeField] private GameObject groupPlayer;   // BodyType slot
////////////////////////    [SerializeField] private GameObject groupHead;     // Face slot
////////////////////////    [SerializeField] private GameObject groupHair;
////////////////////////    [SerializeField] private GameObject groupHelmet;
////////////////////////    [SerializeField] private GameObject groupArmor;
////////////////////////    [SerializeField] private GameObject groupWeapon;

////////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////////////////////////    [Header("Tab Buttons")]
////////////////////////    [SerializeField] private Button tabPlayer;
////////////////////////    [SerializeField] private Button tabHead;
////////////////////////    [SerializeField] private Button tabHair;
////////////////////////    [SerializeField] private Button tabHelmet;
////////////////////////    [SerializeField] private Button tabArmor;
////////////////////////    [SerializeField] private Button tabWeapon;

////////////////////////    [Header("Tab Colours")]
////////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////////    // ─── Inspector — Stats Display (optional) ─────────────────────────────────

////////////////////////    [Header("Stats Display (optional)")]
////////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

////////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////////    private void Awake()
////////////////////////    {
////////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////////////    }

////////////////////////    private void OnDestroy()
////////////////////////    {
////////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////////////        UnsubscribeEquipment();
////////////////////////    }

////////////////////////    private void OnEnable()
////////////////////////    {
////////////////////////        if (soldierEquipment == null)
////////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////////////        SubscribeEquipment();
////////////////////////        InitAllButtons();
////////////////////////        ShowSlot(_activeSlot);
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    private void OnDisable()
////////////////////////    {
////////////////////////        UnsubscribeEquipment();
////////////////////////    }

////////////////////////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

////////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////////////    {
////////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////////////        if (eq == null) return;

////////////////////////        UnsubscribeEquipment();
////////////////////////        soldierEquipment = eq;

////////////////////////        if (!gameObject.activeInHierarchy) return;

////////////////////////        SubscribeEquipment();
////////////////////////        InitAllButtons();
////////////////////////        ShowSlot(_activeSlot);
////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    // ─── Button Initialisation ────────────────────────────────────────────────

////////////////////////    /// <summary>
////////////////////////    /// Walks every group and calls Init() on every InventorySlotButton child
////////////////////////    /// so each button knows which soldier to equip onto.
////////////////////////    /// </summary>
////////////////////////    private void InitAllButtons()
////////////////////////    {
////////////////////////        InitGroup(groupPlayer);
////////////////////////        InitGroup(groupHead);
////////////////////////        InitGroup(groupHair);
////////////////////////        InitGroup(groupHelmet);
////////////////////////        InitGroup(groupArmor);
////////////////////////        InitGroup(groupWeapon);
////////////////////////    }

////////////////////////    private void InitGroup(GameObject group)
////////////////////////    {
////////////////////////        if (group == null || soldierEquipment == null) return;
////////////////////////        foreach (var btn in group.GetComponentsInChildren<InventorySlotButton>(true))
////////////////////////            btn.Init(soldierEquipment, this);
////////////////////////    }

////////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////////    {
////////////////////////        _activeSlot = slot;
////////////////////////        UpdateTabColours();
////////////////////////        ShowActiveGroup();
////////////////////////    }

////////////////////////    private void ShowActiveGroup()
////////////////////////    {
////////////////////////        SetGroupActive(groupPlayer, _activeSlot == EquipmentSlot.BodyType);
////////////////////////        SetGroupActive(groupHead, _activeSlot == EquipmentSlot.Face);
////////////////////////        SetGroupActive(groupHair, _activeSlot == EquipmentSlot.Hair);
////////////////////////        SetGroupActive(groupHelmet, _activeSlot == EquipmentSlot.Helmet);
////////////////////////        SetGroupActive(groupArmor, _activeSlot == EquipmentSlot.Armor);
////////////////////////        SetGroupActive(groupWeapon, _activeSlot == EquipmentSlot.Weapon);
////////////////////////    }

////////////////////////    private static void SetGroupActive(GameObject group, bool active)
////////////////////////    {
////////////////////////        if (group != null) group.SetActive(active);
////////////////////////    }

////////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////////////    private void UpdateTabColours()
////////////////////////    {
////////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
////////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
////////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
////////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
////////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
////////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
////////////////////////    }

////////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
////////////////////////    {
////////////////////////        if (btn == null) return;
////////////////////////        var img = btn.GetComponent<Image>();
////////////////////////        if (img != null)
////////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////////    }

////////////////////////    // ─── Public Refresh (called by InventorySlotButton after equip) ───────────

////////////////////////    public void RefreshAllButtons()
////////////////////////    {
////////////////////////        var activeGroup = GetActiveGroup();
////////////////////////        if (activeGroup == null) return;

////////////////////////        foreach (var btn in activeGroup.GetComponentsInChildren<InventorySlotButton>(true))
////////////////////////            btn.RefreshSelection();

////////////////////////        RefreshStats();
////////////////////////    }

////////////////////////    private GameObject GetActiveGroup() => _activeSlot switch
////////////////////////    {
////////////////////////        EquipmentSlot.BodyType => groupPlayer,
////////////////////////        EquipmentSlot.Face => groupHead,
////////////////////////        EquipmentSlot.Hair => groupHair,
////////////////////////        EquipmentSlot.Helmet => groupHelmet,
////////////////////////        EquipmentSlot.Armor => groupArmor,
////////////////////////        EquipmentSlot.Weapon => groupWeapon,
////////////////////////        _ => null
////////////////////////    };

////////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

////////////////////////    private void SubscribeEquipment()
////////////////////////    {
////////////////////////        if (soldierEquipment != null)
////////////////////////            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;
////////////////////////    }

////////////////////////    private void UnsubscribeEquipment()
////////////////////////    {
////////////////////////        if (soldierEquipment != null)
////////////////////////            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////////////////    }

////////////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item) => RefreshStats();

////////////////////////    private void RefreshStats()
////////////////////////    {
////////////////////////        if (soldierEquipment == null) return;
////////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
////////////////////////        if (stats == null) return;

////////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
////////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
////////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
////////////////////////    }

////////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

////////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////////}


//////////////////////using System.Collections.Generic;
//////////////////////using UnityEngine;
//////////////////////using UnityEngine.UI;
//////////////////////using TMPro;

///////////////////////// <summary>
///////////////////////// AREA FORGE - InventoryPanel
/////////////////////////
///////////////////////// Manages the inventory UI and drives the player visual swapping via
///////////////////////// GameObject.SetActive().
/////////////////////////
///////////////////////// ── How it works ─────────────────────────────────────────────────────────────
/////////////////////////   Each InventorySlotButton has a "playerVisualObject" field pointing to a
/////////////////////////   child GO on the Player (e.g. Armor1, Armor2, Hair1, Hair2 …).
/////////////////////////
/////////////////////////   SelectButton(btn)  → btn.Select()   activates  btn.playerVisualObject
/////////////////////////                        all other buttons in the same group → Deselect()
/////////////////////////                        their playerVisualObjects are deactivated
/////////////////////////
/////////////////////////   DeselectButton(btn)→ btn.Deselect() deactivates btn.playerVisualObject
/////////////////////////                        (nothing equipped in that slot)
/////////////////////////
///////////////////////// ── Default first-item rule ───────────────────────────────────────────────────
/////////////////////////   When the panel opens, if no button in a group is selected, the FIRST
/////////////////////////   button is auto-selected so the player always looks correct.
/////////////////////////
///////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////////////////////   Groups  → drag the parent GO of each slot's buttons (GROUP_ARMOR etc.)
/////////////////////////   Tabs    → drag each tab Button
/////////////////////////   Soldier → leave empty (found at runtime via FindObjectOfType)
/////////////////////////
/////////////////////////   On each InventorySlotButton:
/////////////////////////     playerVisualObject → drag Player/Armor/Armor1  (or Armor2, Hair1 …)
///////////////////////// </summary>
//////////////////////public class InventoryPanel : MonoBehaviour
//////////////////////{
//////////////////////    // ─── Inspector — Soldier (optional, for stat bonuses) ─────────────────────

//////////////////////    [Header("Soldier — leave empty, found automatically at runtime")]
//////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////////////////////    [Header("Item Groups — drag each slot's button-parent GO here")]
//////////////////////    [SerializeField] private GameObject groupPlayer;    // BodyType slot
//////////////////////    [SerializeField] private GameObject groupHead;      // Face slot
//////////////////////    [SerializeField] private GameObject groupHair;
//////////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////////    [SerializeField] private GameObject groupArmor;
//////////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//////////////////////    [Header("Tab Buttons")]
//////////////////////    [SerializeField] private Button tabPlayer;
//////////////////////    [SerializeField] private Button tabHead;
//////////////////////    [SerializeField] private Button tabHair;
//////////////////////    [SerializeField] private Button tabHelmet;
//////////////////////    [SerializeField] private Button tabArmor;
//////////////////////    [SerializeField] private Button tabWeapon;

//////////////////////    [Header("Tab Colours")]
//////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////////    // ─── Inspector — Stats (optional) ─────────────────────────────────────────

//////////////////////    [Header("Stats Display (optional)")]
//////////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;

//////////////////////    // Cached lists of buttons per group (built once in Init)
//////////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////////    private void Awake()
//////////////////////    {
//////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////////    }

//////////////////////    private void OnDestroy()
//////////////////////    {
//////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////////    }

//////////////////////    private void OnEnable()
//////////////////////    {
//////////////////////        if (soldierEquipment == null)
//////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////////        BuildGroupCache();
//////////////////////        InitAllButtons();
//////////////////////        AutoSelectFirstItems();
//////////////////////        ShowSlot(_activeSlot);
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

//////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////////    {
//////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////////        if (eq == null) return;
//////////////////////        soldierEquipment = eq;
//////////////////////        if (!gameObject.activeInHierarchy) return;
//////////////////////        InitAllButtons();
//////////////////////        AutoSelectFirstItems();
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////////////    private void BuildGroupCache()
//////////////////////    {
//////////////////////        _groups.Clear();
//////////////////////        AddToCache(EquipmentSlot.BodyType, groupPlayer);
//////////////////////        AddToCache(EquipmentSlot.Face, groupHead);
//////////////////////        AddToCache(EquipmentSlot.Hair, groupHair);
//////////////////////        AddToCache(EquipmentSlot.Helmet, groupHelmet);
//////////////////////        AddToCache(EquipmentSlot.Armor, groupArmor);
//////////////////////        AddToCache(EquipmentSlot.Weapon, groupWeapon);
//////////////////////    }

//////////////////////    private void AddToCache(EquipmentSlot slot, GameObject group)
//////////////////////    {
//////////////////////        var list = new List<InventorySlotButton>();
//////////////////////        if (group != null)
//////////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////////////        _groups[slot] = list;
//////////////////////    }

//////////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

//////////////////////    private void InitAllButtons()
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////            foreach (var btn in kvp.Value)
//////////////////////                btn.Init(this, soldierEquipment);
//////////////////////    }

//////////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

//////////////////////    /// <summary>
//////////////////////    /// For each slot, if nothing is selected yet, auto-select the first button.
//////////////////////    /// This activates the first playerVisualObject in each group by default.
//////////////////////    /// </summary>
//////////////////////    private void AutoSelectFirstItems()
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////        {
//////////////////////            var list = kvp.Value;
//////////////////////            if (list.Count == 0) continue;

//////////////////////            // Already something selected? Leave it.
//////////////////////            bool anySelected = false;
//////////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

//////////////////////            if (!anySelected)
//////////////////////                SelectExclusive(list[0], list);
//////////////////////        }
//////////////////////    }

//////////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////////////////    /// <summary>
//////////////////////    /// Selects btn, deselects all others in the same slot group.
//////////////////////    /// This activates btn.playerVisualObject and deactivates the rest.
//////////////////////    /// </summary>
//////////////////////    public void SelectButton(InventorySlotButton btn)
//////////////////////    {
//////////////////////        var list = GetGroupForButton(btn);
//////////////////////        if (list == null) return;
//////////////////////        SelectExclusive(btn, list);
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    /// <summary>
//////////////////////    /// Deselects btn only — no other button is auto-selected.
//////////////////////    /// The player will have nothing equipped in that slot.
//////////////////////    /// </summary>
//////////////////////    public void DeselectButton(InventorySlotButton btn)
//////////////////////    {
//////////////////////        btn.Deselect();
//////////////////////        RefreshStats();
//////////////////////    }

//////////////////////    // ─── Core Exclusive-Activate Logic ───────────────────────────────────────

//////////////////////    /// <summary>
//////////////////////    /// Activates target's playerVisualObject, deactivates all others in the list.
//////////////////////    /// </summary>
//////////////////////    private void SelectExclusive(InventorySlotButton target, List<InventorySlotButton> group)
//////////////////////    {
//////////////////////        foreach (var btn in group)
//////////////////////        {
//////////////////////            if (btn == target)
//////////////////////                btn.Select();     // SetActive(true) on its playerVisualObject
//////////////////////            else
//////////////////////                btn.Deselect();   // SetActive(false) on its playerVisualObject
//////////////////////        }
//////////////////////    }

//////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////////    {
//////////////////////        _activeSlot = slot;
//////////////////////        UpdateTabColours();

//////////////////////        // Show only the active group's button panel
//////////////////////        SetGroupUIActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////////////        SetGroupUIActive(groupHead, slot == EquipmentSlot.Face);
//////////////////////        SetGroupUIActive(groupHair, slot == EquipmentSlot.Hair);
//////////////////////        SetGroupUIActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////////////        SetGroupUIActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////////////        SetGroupUIActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////////////    }

//////////////////////    private static void SetGroupUIActive(GameObject group, bool active)
//////////////////////    {
//////////////////////        if (group != null) group.SetActive(active);
//////////////////////    }

//////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////////    private void UpdateTabColours()
//////////////////////    {
//////////////////////        SetTabColour(tabPlayer, EquipmentSlot.BodyType);
//////////////////////        SetTabColour(tabHead, EquipmentSlot.Face);
//////////////////////        SetTabColour(tabHair, EquipmentSlot.Hair);
//////////////////////        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
//////////////////////        SetTabColour(tabArmor, EquipmentSlot.Armor);
//////////////////////        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
//////////////////////    }

//////////////////////    private void SetTabColour(Button btn, EquipmentSlot slot)
//////////////////////    {
//////////////////////        if (btn == null) return;
//////////////////////        var img = btn.GetComponent<Image>();
//////////////////////        if (img != null)
//////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////////    }

//////////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////////////    private List<InventorySlotButton> GetGroupForButton(InventorySlotButton target)
//////////////////////    {
//////////////////////        foreach (var kvp in _groups)
//////////////////////            foreach (var btn in kvp.Value)
//////////////////////                if (btn == target) return kvp.Value;
//////////////////////        return null;
//////////////////////    }

//////////////////////    // ─── Stats ────────────────────────────────────────────────────────────────

//////////////////////    private void RefreshStats()
//////////////////////    {
//////////////////////        if (soldierEquipment == null) return;
//////////////////////        var stats = soldierEquipment.GetComponent<SoldierStats>();
//////////////////////        if (stats == null) return;
//////////////////////        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
//////////////////////        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
//////////////////////        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
//////////////////////    }

//////////////////////    // ─── Open / Close ─────────────────────────────────────────────────────────

//////////////////////    public void Open() => gameObject.SetActive(true);
//////////////////////    public void Close() => gameObject.SetActive(false);
//////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////////}

////////////////////using System.Collections.Generic;
////////////////////using UnityEngine;
////////////////////using UnityEngine.UI;
////////////////////using TMPro;

/////////////////////// <summary>
/////////////////////// AREA FORGE - InventoryPanel
///////////////////////
/////////////////////// ── Key fix ──────────────────────────────────────────────────────────────────
///////////////////////   All slot groups are FORCED ACTIVE before Init so that
///////////////////////   InventorySlotButton.Init() can run on every button (including ones in
///////////////////////   hidden groups). After Init, ShowSlot() hides the non-active groups again.
///////////////////////
/////////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
///////////////////////   groupArmor  → drag the parent GO of all Armor buttons   (e.g. Content/ARMOR)
///////////////////////   groupHair   → drag the parent GO of all Hair buttons    (e.g. Content/HAIR)
///////////////////////   … and so on for Head, Helmet, Player, Weapon
///////////////////////   Tab buttons → drag each tab Button
/////////////////////// </summary>
////////////////////public class InventoryPanel : MonoBehaviour
////////////////////{
////////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////////////////////    [Header("Soldier — leave empty, found automatically")]
////////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////////////////////    [Header("Item Groups — one parent GO per slot")]
////////////////////    [SerializeField] private GameObject groupPlayer;
////////////////////    [SerializeField] private GameObject groupHead;
////////////////////    [SerializeField] private GameObject groupHair;
////////////////////    [SerializeField] private GameObject groupHelmet;
////////////////////    [SerializeField] private GameObject groupArmor;
////////////////////    [SerializeField] private GameObject groupWeapon;

////////////////////    // ─── Inspector — Tabs ─────────────────────────────────────────────────────

////////////////////    [Header("Tab Buttons")]
////////////////////    [SerializeField] private Button tabPlayer;
////////////////////    [SerializeField] private Button tabHead;
////////////////////    [SerializeField] private Button tabHair;
////////////////////    [SerializeField] private Button tabHelmet;
////////////////////    [SerializeField] private Button tabArmor;
////////////////////    [SerializeField] private Button tabWeapon;

////////////////////    [Header("Tab Colours")]
////////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////////    // ─── Inspector — Stats ────────────────────────────────────────────────────

////////////////////    [Header("Stats Display (optional)")]
////////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////    private void Awake()
////////////////////    {
////////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////////    }

////////////////////    private void OnDestroy()
////////////////////    {
////////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////////    }

////////////////////    private void OnEnable()
////////////////////    {
////////////////////        if (soldierEquipment == null)
////////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////////        // ── CRITICAL: activate ALL groups first so Init() reaches every button ──
////////////////////        // (buttons inside inactive GOs never have Awake/Start called)
////////////////////        ForceAllGroupsActive(true);

////////////////////        BuildGroupCache();
////////////////////        InitAllButtons();
////////////////////        AutoSelectFirstItems();

////////////////////        // Now hide the non-active groups
////////////////////        ShowSlot(_activeSlot);
////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

////////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////////    {
////////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////////        if (eq == null) return;
////////////////////        soldierEquipment = eq;
////////////////////        if (!gameObject.activeInHierarchy) return;

////////////////////        ForceAllGroupsActive(true);
////////////////////        InitAllButtons();
////////////////////        AutoSelectFirstItems();
////////////////////        ShowSlot(_activeSlot);
////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////////////    private void BuildGroupCache()
////////////////////    {
////////////////////        _groups.Clear();
////////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////////////        Cache(EquipmentSlot.Face, groupHead);
////////////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////////////    }

////////////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////////////    {
////////////////////        var list = new List<InventorySlotButton>();
////////////////////        if (group != null)
////////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////////////        _groups[slot] = list;
////////////////////    }

////////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

////////////////////    private void InitAllButtons()
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////            foreach (var btn in kvp.Value)
////////////////////                btn.Init(this, soldierEquipment);
////////////////////    }

////////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

////////////////////    private void AutoSelectFirstItems()
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////        {
////////////////////            var list = kvp.Value;
////////////////////            if (list.Count == 0) continue;

////////////////////            bool anySelected = false;
////////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

////////////////////            if (!anySelected)
////////////////////                Activate(list[0], list);
////////////////////        }
////////////////////    }

////////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////////////    public void SelectButton(InventorySlotButton btn)
////////////////////    {
////////////////////        var list = FindGroup(btn);
////////////////////        if (list == null) return;
////////////////////        Activate(btn, list);
////////////////////        RefreshStats();
////////////////////    }

////////////////////    public void DeselectButton(InventorySlotButton btn)
////////////////////    {
////////////////////        btn.Deselect();
////////////////////        RefreshStats();
////////////////////    }

////////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

////////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
////////////////////    {
////////////////////        foreach (var btn in group)
////////////////////        {
////////////////////            if (btn == target) btn.Select();
////////////////////            else btn.Deselect();
////////////////////        }
////////////////////    }

////////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////////    {
////////////////////        _activeSlot = slot;
////////////////////        UpdateTabColours();

////////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////////////    }

////////////////////    private void ForceAllGroupsActive(bool active)
////////////////////    {
////////////////////        SetActive(groupPlayer, active);
////////////////////        SetActive(groupHead, active);
////////////////////        SetActive(groupHair, active);
////////////////////        SetActive(groupHelmet, active);
////////////////////        SetActive(groupArmor, active);
////////////////////        SetActive(groupWeapon, active);
////////////////////    }

////////////////////    private static void SetActive(GameObject go, bool active)
////////////////////    {
////////////////////        if (go != null) go.SetActive(active);
////////////////////    }

////////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////////    private void UpdateTabColours()
////////////////////    {
////////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////////////        Tint(tabHead, EquipmentSlot.Face);
////////////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////////////    }

////////////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////////////    {
////////////////////        if (btn == null) return;
////////////////////        var img = btn.GetComponent<Image>();
////////////////////        if (img != null)
////////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////////    }

////////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////////////    {
////////////////////        foreach (var kvp in _groups)
////////////////////            foreach (var btn in kvp.Value)
////////////////////                if (btn == target) return kvp.Value;
////////////////////        return null;
////////////////////    }

////////////////////    private void RefreshStats()
////////////////////    {
////////////////////        if (soldierEquipment == null) return;
////////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////////////        if (s == null) return;
////////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////////////    }

////////////////////    public void Open() => gameObject.SetActive(true);
////////////////////    public void Close() => gameObject.SetActive(false);
////////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////////}

//////////////////using System.Collections.Generic;
//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using TMPro;

///////////////////// <summary>
///////////////////// AREA FORGE - InventoryPanel
/////////////////////
///////////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////////////   Drag the currently-equipped Hair player GO into "hairVisualRoot".
/////////////////////   When a helmet is equipped   → hairVisualRoot.SetActive(false)
/////////////////////   When a helmet is unequipped → hairVisualRoot.SetActive(true)
/////////////////////
/////////////////////   hairVisualRoot should be the PARENT of all hair GOs under the Player,
/////////////////////   e.g. Player/Hair  (so all hair variants hide together).
/////////////////////   If each hair has its own GO, drag whichever is currently selected —
/////////////////////   the panel updates it live whenever hair selection changes.
/////////////////////
///////////////////// ── Default items (Body, Face) ───────────────────────────────────────────────
/////////////////////   Tick "Is Default" on the Body and Face InventorySlotButtons.
/////////////////////   They auto-select on open and clicking them does nothing.
/////////////////////
///////////////////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////////////////   groupArmor   → parent GO of all Armor buttons   (e.g. Content/ARMOR)
/////////////////////   groupHair    → parent GO of all Hair  buttons   (e.g. Content/HAIR)
/////////////////////   … same for Head, Helmet, Player, Weapon
/////////////////////   hairVisualRoot → Player/Hair  (the hair layer on the player)
///////////////////// </summary>
//////////////////public class InventoryPanel : MonoBehaviour
//////////////////{
//////////////////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//////////////////    [Header("Soldier — leave empty, found automatically")]
//////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////////////////    [Header("Item Groups — one parent GO per slot")]
//////////////////    [SerializeField] private GameObject groupPlayer;
//////////////////    [SerializeField] private GameObject groupHead;
//////////////////    [SerializeField] private GameObject groupHair;
//////////////////    [SerializeField] private GameObject groupHelmet;
//////////////////    [SerializeField] private GameObject groupArmor;
//////////////////    [SerializeField] private GameObject groupWeapon;

//////////////////    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

//////////////////    [Header("Helmet hides Hair")]
//////////////////    [Tooltip("Drag the Hair parent GO on the PLAYER (e.g. Player/Hair).\n" +
//////////////////             "This whole GO is hidden when any helmet is equipped.")]
//////////////////    [SerializeField] private GameObject hairVisualRoot;

//////////////////    // ─── Inspector — Tabs ─────────────────────────────────────────────────────

//////////////////    [Header("Tab Buttons")]
//////////////////    [SerializeField] private Button tabPlayer;
//////////////////    [SerializeField] private Button tabHead;
//////////////////    [SerializeField] private Button tabHair;
//////////////////    [SerializeField] private Button tabHelmet;
//////////////////    [SerializeField] private Button tabArmor;
//////////////////    [SerializeField] private Button tabWeapon;

//////////////////    [Header("Tab Colours")]
//////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////////    // ─── Inspector — Stats ────────────────────────────────────────────────────

//////////////////    [Header("Stats Display (optional)")]
//////////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////////    // Track whether any helmet is currently selected
//////////////////    private bool _helmetEquipped = false;

//////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////    private void Awake()
//////////////////    {
//////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////////    }

//////////////////    private void OnDestroy()
//////////////////    {
//////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////////    }

//////////////////    private void OnEnable()
//////////////////    {
//////////////////        if (soldierEquipment == null)
//////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////////        // Activate ALL groups first so Init() reaches every button
//////////////////        // (buttons inside inactive GOs never have Awake called)
//////////////////        ForceAllGroupsActive(true);

//////////////////        BuildGroupCache();
//////////////////        InitAllButtons();
//////////////////        AutoSelectFirstItems();

//////////////////        ShowSlot(_activeSlot);
//////////////////        ApplyHairHelmetRule();
//////////////////        RefreshStats();
//////////////////    }

//////////////////    // ─── Soldier Spawn ────────────────────────────────────────────────────────

//////////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////////    {
//////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////////        if (eq == null) return;
//////////////////        soldierEquipment = eq;
//////////////////        if (!gameObject.activeInHierarchy) return;

//////////////////        ForceAllGroupsActive(true);
//////////////////        InitAllButtons();
//////////////////        AutoSelectFirstItems();
//////////////////        ShowSlot(_activeSlot);
//////////////////        ApplyHairHelmetRule();
//////////////////        RefreshStats();
//////////////////    }

//////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////////    private void BuildGroupCache()
//////////////////    {
//////////////////        _groups.Clear();
//////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////////////        Cache(EquipmentSlot.Face, groupHead);
//////////////////        Cache(EquipmentSlot.Hair, groupHair);
//////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////////////    }

//////////////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////////////    {
//////////////////        var list = new List<InventorySlotButton>();
//////////////////        if (group != null)
//////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////////        _groups[slot] = list;
//////////////////    }

//////////////////    // ─── Button Init ──────────────────────────────────────────────────────────

//////////////////    private void InitAllButtons()
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////            foreach (var btn in kvp.Value)
//////////////////                btn.Init(this, soldierEquipment);
//////////////////    }

//////////////////    // ─── Default First-Item ───────────────────────────────────────────────────

//////////////////    private void AutoSelectFirstItems()
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////        {
//////////////////            var list = kvp.Value;
//////////////////            if (list.Count == 0) continue;

//////////////////            bool anySelected = false;
//////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

//////////////////            if (!anySelected)
//////////////////                Activate(list[0], list);
//////////////////        }
//////////////////    }

//////////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////////////    public void SelectButton(InventorySlotButton btn)
//////////////////    {
//////////////////        var list = FindGroup(btn);
//////////////////        if (list == null) return;

//////////////////        Activate(btn, list);

//////////////////        // Check if this is a helmet slot selection
//////////////////        if (btn.Item != null && btn.Item.slot == EquipmentSlot.Helmet)
//////////////////        {
//////////////////            _helmetEquipped = true;
//////////////////            ApplyHairHelmetRule();
//////////////////        }

//////////////////        RefreshStats();
//////////////////    }

//////////////////    public void DeselectButton(InventorySlotButton btn)
//////////////////    {
//////////////////        btn.Deselect();

//////////////////        // Check if a helmet was just removed
//////////////////        if (btn.Item != null && btn.Item.slot == EquipmentSlot.Helmet)
//////////////////        {
//////////////////            _helmetEquipped = false;
//////////////////            ApplyHairHelmetRule();
//////////////////        }

//////////////////        RefreshStats();
//////////////////    }

//////////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////////////    /// <summary>
//////////////////    /// Hides the hair visual root when any helmet is equipped.
//////////////////    /// Shows it again when the helmet is removed.
//////////////////    /// </summary>
//////////////////    private void ApplyHairHelmetRule()
//////////////////    {
//////////////////        if (hairVisualRoot == null) return;

//////////////////        // Check the helmet group for any selected button
//////////////////        bool helmetOn = false;
//////////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////////////            foreach (var btn in helmetGroup)
//////////////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////////////        _helmetEquipped = helmetOn;

//////////////////        // Show or hide the currently selected hair visual
//////////////////        // We hide/show the entire hairVisualRoot (parent of all hair GOs on the player)
//////////////////        hairVisualRoot.SetActive(!helmetOn);
//////////////////    }

//////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

//////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
//////////////////    {
//////////////////        foreach (var btn in group)
//////////////////        {
//////////////////            if (btn == target) btn.Select();
//////////////////            else btn.Deselect();
//////////////////        }
//////////////////    }

//////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////////    private void ShowSlot(EquipmentSlot slot)
//////////////////    {
//////////////////        _activeSlot = slot;
//////////////////        UpdateTabColours();

//////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////////    }

//////////////////    private void ForceAllGroupsActive(bool active)
//////////////////    {
//////////////////        SetActive(groupPlayer, active);
//////////////////        SetActive(groupHead, active);
//////////////////        SetActive(groupHair, active);
//////////////////        SetActive(groupHelmet, active);
//////////////////        SetActive(groupArmor, active);
//////////////////        SetActive(groupWeapon, active);
//////////////////    }

//////////////////    private static void SetActive(GameObject go, bool active)
//////////////////    {
//////////////////        if (go != null) go.SetActive(active);
//////////////////    }

//////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////////    private void UpdateTabColours()
//////////////////    {
//////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////////////        Tint(tabHead, EquipmentSlot.Face);
//////////////////        Tint(tabHair, EquipmentSlot.Hair);
//////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////////////    }

//////////////////    private void Tint(Button btn, EquipmentSlot slot)
//////////////////    {
//////////////////        if (btn == null) return;
//////////////////        var img = btn.GetComponent<Image>();
//////////////////        if (img != null)
//////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////////    }

//////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////////////    {
//////////////////        foreach (var kvp in _groups)
//////////////////            foreach (var btn in kvp.Value)
//////////////////                if (btn == target) return kvp.Value;
//////////////////        return null;
//////////////////    }

//////////////////    private void RefreshStats()
//////////////////    {
//////////////////        if (soldierEquipment == null) return;
//////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////////////        if (s == null) return;
//////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////////////    }

//////////////////    public void Open() => gameObject.SetActive(true);
//////////////////    public void Close() => gameObject.SetActive(false);
//////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////////}

////////////////using System.Collections.Generic;
////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using TMPro;

/////////////////// <summary>
/////////////////// AREA FORGE - InventoryPanel
///////////////////
/////////////////// ── Selection rule ───────────────────────────────────────────────────────────
///////////////////   Exactly ONE item is always selected per slot group.
///////////////////   Clicking another item switches to it. Clicking the current one does nothing.
///////////////////   The first item in each group is selected by default on open.
///////////////////
/////////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////////////   Drag Player/Hair into hairVisualRoot.
///////////////////   Helmet equipped   → hairVisualRoot hides
///////////////////   Helmet unequipped → hairVisualRoot shows
/////////////////// </summary>
////////////////public class InventoryPanel : MonoBehaviour
////////////////{
////////////////    [Header("Soldier — leave empty, found automatically")]
////////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////////    [Header("Item Groups — one parent GO per slot")]
////////////////    [SerializeField] private GameObject groupPlayer;
////////////////    [SerializeField] private GameObject groupHead;
////////////////    [SerializeField] private GameObject groupHair;
////////////////    [SerializeField] private GameObject groupHelmet;
////////////////    [SerializeField] private GameObject groupArmor;
////////////////    [SerializeField] private GameObject groupWeapon;

////////////////    [Header("Helmet hides Hair")]
////////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////////////    [SerializeField] private GameObject hairVisualRoot;

////////////////    [Header("Tab Buttons")]
////////////////    [SerializeField] private Button tabPlayer;
////////////////    [SerializeField] private Button tabHead;
////////////////    [SerializeField] private Button tabHair;
////////////////    [SerializeField] private Button tabHelmet;
////////////////    [SerializeField] private Button tabArmor;
////////////////    [SerializeField] private Button tabWeapon;

////////////////    [Header("Tab Colours")]
////////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////////    [Header("Stats Display (optional)")]
////////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////////    [SerializeField] private TextMeshProUGUI apText;
////////////////    [SerializeField] private TextMeshProUGUI adText;

////////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////////    }

////////////////    private void OnDestroy()
////////////////    {
////////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////////    }

////////////////    private void OnEnable()
////////////////    {
////////////////        if (soldierEquipment == null)
////////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////////        ForceAllGroupsActive(true);   // must be active before Init() runs
////////////////        BuildGroupCache();
////////////////        InitAllButtons();
////////////////        AutoSelectFirstItems();
////////////////        ShowSlot(_activeSlot);
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////////    {
////////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////////        if (eq == null) return;
////////////////        soldierEquipment = eq;
////////////////        if (!gameObject.activeInHierarchy) return;
////////////////        ForceAllGroupsActive(true);
////////////////        InitAllButtons();
////////////////        AutoSelectFirstItems();
////////////////        ShowSlot(_activeSlot);
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////////    private void BuildGroupCache()
////////////////    {
////////////////        _groups.Clear();
////////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////////        Cache(EquipmentSlot.Face, groupHead);
////////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////////    }

////////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////////    {
////////////////        var list = new List<InventorySlotButton>();
////////////////        if (group != null)
////////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////////        _groups[slot] = list;
////////////////    }

////////////////    private void InitAllButtons()
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////            foreach (var btn in kvp.Value)
////////////////                btn.Init(this, soldierEquipment);
////////////////    }

////////////////    // ─── Default: auto-select first item per group ────────────────────────────

////////////////    private void AutoSelectFirstItems()
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////        {
////////////////            var list = kvp.Value;
////////////////            if (list.Count == 0) continue;

////////////////            // Check if anything is already selected
////////////////            bool anySelected = false;
////////////////            foreach (var b in list) if (b.IsSelected) { anySelected = true; break; }

////////////////            if (!anySelected)
////////////////                Activate(list[0], list);
////////////////        }
////////////////    }

////////////////    // ─── Public: called by InventorySlotButton.OnClick ───────────────────────

////////////////    /// <summary>
////////////////    /// Selects btn exclusively in its group.
////////////////    /// All other buttons in the same slot are deselected.
////////////////    /// </summary>
////////////////    public void SelectButton(InventorySlotButton btn)
////////////////    {
////////////////        var list = FindGroup(btn);
////////////////        if (list == null) return;

////////////////        Activate(btn, list);
////////////////        ApplyHairHelmetRule();
////////////////        RefreshStats();
////////////////    }

////////////////    // ─── Exclusive Activate ───────────────────────────────────────────────────

////////////////    private void Activate(InventorySlotButton target, List<InventorySlotButton> group)
////////////////    {
////////////////        foreach (var btn in group)
////////////////        {
////////////////            if (btn == target) btn.Select();
////////////////            else btn.Deselect();
////////////////        }
////////////////    }

////////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////////////    private void ApplyHairHelmetRule()
////////////////    {
////////////////        if (hairVisualRoot == null) return;

////////////////        bool helmetOn = false;
////////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////////////            foreach (var btn in helmetGroup)
////////////////                if (btn.IsSelected) { helmetOn = true; break; }

////////////////        hairVisualRoot.SetActive(!helmetOn);
////////////////    }

////////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////////    private void ShowSlot(EquipmentSlot slot)
////////////////    {
////////////////        _activeSlot = slot;
////////////////        UpdateTabColours();
////////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////////    }

////////////////    private void ForceAllGroupsActive(bool active)
////////////////    {
////////////////        SetActive(groupPlayer, active);
////////////////        SetActive(groupHead, active);
////////////////        SetActive(groupHair, active);
////////////////        SetActive(groupHelmet, active);
////////////////        SetActive(groupArmor, active);
////////////////        SetActive(groupWeapon, active);
////////////////    }

////////////////    private static void SetActive(GameObject go, bool active)
////////////////    {
////////////////        if (go != null) go.SetActive(active);
////////////////    }

////////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////////    private void UpdateTabColours()
////////////////    {
////////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////////        Tint(tabHead, EquipmentSlot.Face);
////////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////////    }

////////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////////    {
////////////////        if (btn == null) return;
////////////////        var img = btn.GetComponent<Image>();
////////////////        if (img != null)
////////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////////    }

////////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////////    {
////////////////        foreach (var kvp in _groups)
////////////////            foreach (var btn in kvp.Value)
////////////////                if (btn == target) return kvp.Value;
////////////////        return null;
////////////////    }

////////////////    private void RefreshStats()
////////////////    {
////////////////        if (soldierEquipment == null) return;
////////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////////        if (s == null) return;
////////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////////    }

////////////////    public void Open() => gameObject.SetActive(true);
////////////////    public void Close() => gameObject.SetActive(false);
////////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////////}

//////////////using System.Collections.Generic;
//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using TMPro;

///////////////// <summary>
///////////////// AREA FORGE - InventoryPanel
/////////////////
///////////////// ── Selection rules ──────────────────────────────────────────────────────────
/////////////////   • Body (Skinny)              → always selected, never deselectable (isDefault)
/////////////////   • Armor / Helmet / Weapon / Hair → click to select, click again to deselect
/////////////////   • No other auto-selection on open
/////////////////
///////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////////   Drag Player/Hair into hairVisualRoot.
/////////////////   Helmet selected   → hairVisualRoot hides
/////////////////   Helmet deselected → hairVisualRoot shows
///////////////// </summary>
//////////////public class InventoryPanel : MonoBehaviour
//////////////{
//////////////    [Header("Soldier — leave empty, found automatically")]
//////////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////////    [Header("Item Groups — one parent GO per slot")]
//////////////    [SerializeField] private GameObject groupPlayer;
//////////////    [SerializeField] private GameObject groupHead;
//////////////    [SerializeField] private GameObject groupHair;
//////////////    [SerializeField] private GameObject groupHelmet;
//////////////    [SerializeField] private GameObject groupArmor;
//////////////    [SerializeField] private GameObject groupWeapon;

//////////////    [Header("Helmet hides Hair")]
//////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////////////    [SerializeField] private GameObject hairVisualRoot;

//////////////    [Header("Tab Buttons")]
//////////////    [SerializeField] private Button tabPlayer;
//////////////    [SerializeField] private Button tabHead;
//////////////    [SerializeField] private Button tabHair;
//////////////    [SerializeField] private Button tabHelmet;
//////////////    [SerializeField] private Button tabArmor;
//////////////    [SerializeField] private Button tabWeapon;

//////////////    [Header("Tab Colours")]
//////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////////    [Header("Stats Display (optional)")]
//////////////    [SerializeField] private TextMeshProUGUI hpText;
//////////////    [SerializeField] private TextMeshProUGUI apText;
//////////////    [SerializeField] private TextMeshProUGUI adText;

//////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////////    }

//////////////    private void OnDestroy()
//////////////    {
//////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////////    }

//////////////    private void OnEnable()
//////////////    {
//////////////        if (soldierEquipment == null)
//////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////////        ForceAllGroupsActive(true);
//////////////        BuildGroupCache();
//////////////        InitAllButtons();
//////////////        SelectDefaultItems();   // only selects buttons marked isDefault (Skinny Body)
//////////////        ShowSlot(_activeSlot);
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////////    {
//////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////////        if (eq == null) return;
//////////////        soldierEquipment = eq;
//////////////        if (!gameObject.activeInHierarchy) return;
//////////////        ForceAllGroupsActive(true);
//////////////        InitAllButtons();
//////////////        SelectDefaultItems();
//////////////        ShowSlot(_activeSlot);
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////////    private void BuildGroupCache()
//////////////    {
//////////////        _groups.Clear();
//////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////////        Cache(EquipmentSlot.Face, groupHead);
//////////////        Cache(EquipmentSlot.Hair, groupHair);
//////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////////    }

//////////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////////    {
//////////////        var list = new List<InventorySlotButton>();
//////////////        if (group != null)
//////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////////        _groups[slot] = list;
//////////////    }

//////////////    private void InitAllButtons()
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////            foreach (var btn in kvp.Value)
//////////////                btn.Init(this, soldierEquipment);
//////////////    }

//////////////    // ─── Default Selection ────────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Only selects buttons that have "isDefault" ticked (Skinny Body).
//////////////    /// Everything else starts deselected / deactivated.
//////////////    /// </summary>
//////////////    private void SelectDefaultItems()
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////        {
//////////////            foreach (var btn in kvp.Value)
//////////////            {
//////////////                if (btn.IsDefault)
//////////////                    btn.Select();     // Skinny Body — force selected
//////////////                else
//////////////                    btn.Deselect();   // everything else — deactivated
//////////////            }
//////////////        }
//////////////    }

//////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////////    /// <summary>
//////////////    /// Selects btn and deselects all other NON-DEFAULT buttons in the same group.
//////////////    /// </summary>
//////////////    public void SelectButton(InventorySlotButton btn)
//////////////    {
//////////////        var list = FindGroup(btn);
//////////////        if (list == null) return;

//////////////        foreach (var b in list)
//////////////        {
//////////////            if (b == btn) b.Select();
//////////////            else if (!b.IsDefault) b.Deselect();   // never deselect a default button
//////////////        }

//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    /// <summary>
//////////////    /// Deselects btn only (called when user clicks an already-selected item).
//////////////    /// </summary>
//////////////    public void DeselectButton(InventorySlotButton btn)
//////////////    {
//////////////        if (btn.IsDefault) return;   // safety
//////////////        btn.Deselect();
//////////////        ApplyHairHelmetRule();
//////////////        RefreshStats();
//////////////    }

//////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////////    private void ApplyHairHelmetRule()
//////////////    {
//////////////        if (hairVisualRoot == null) return;

//////////////        bool helmetOn = false;
//////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////////            foreach (var btn in helmetGroup)
//////////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////////        hairVisualRoot.SetActive(!helmetOn);
//////////////    }

//////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////////    private void ShowSlot(EquipmentSlot slot)
//////////////    {
//////////////        _activeSlot = slot;
//////////////        UpdateTabColours();
//////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////////    }

//////////////    private void ForceAllGroupsActive(bool active)
//////////////    {
//////////////        SetActive(groupPlayer, active);
//////////////        SetActive(groupHead, active);
//////////////        SetActive(groupHair, active);
//////////////        SetActive(groupHelmet, active);
//////////////        SetActive(groupArmor, active);
//////////////        SetActive(groupWeapon, active);
//////////////    }

//////////////    private static void SetActive(GameObject go, bool active)
//////////////    {
//////////////        if (go != null) go.SetActive(active);
//////////////    }

//////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////////    private void UpdateTabColours()
//////////////    {
//////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////////        Tint(tabHead, EquipmentSlot.Face);
//////////////        Tint(tabHair, EquipmentSlot.Hair);
//////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////////    }

//////////////    private void Tint(Button btn, EquipmentSlot slot)
//////////////    {
//////////////        if (btn == null) return;
//////////////        var img = btn.GetComponent<Image>();
//////////////        if (img != null)
//////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////////    }

//////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////////    {
//////////////        foreach (var kvp in _groups)
//////////////            foreach (var btn in kvp.Value)
//////////////                if (btn == target) return kvp.Value;
//////////////        return null;
//////////////    }

//////////////    private void RefreshStats()
//////////////    {
//////////////        if (soldierEquipment == null) return;
//////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////////        if (s == null) return;
//////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////////    }

//////////////    public void Open() => gameObject.SetActive(true);
//////////////    public void Close() => gameObject.SetActive(false);
//////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////////}

////////////using System.Collections.Generic;
////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using TMPro;

/////////////// <summary>
/////////////// AREA FORGE - InventoryPanel
///////////////
/////////////// ── Selection rules ──────────────────────────────────────────────────────────
///////////////   • Body (Skinny)   → isDefault = true  → always selected, cannot deselect
///////////////   • Face (first)    → auto-selected on open, CAN be deselected by clicking again
///////////////   • Hair (first)    → auto-selected on open, CAN be deselected by clicking again
///////////////   • Armor / Helmet / Weapon → no default, click to select / click again to deselect
///////////////
/////////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////////   Drag Player/Hair into hairVisualRoot.
///////////////   Helmet selected   → hairVisualRoot hides
///////////////   Helmet deselected → hairVisualRoot shows
/////////////// </summary>
////////////public class InventoryPanel : MonoBehaviour
////////////{
////////////    [Header("Soldier — leave empty, found automatically")]
////////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////////    [Header("Item Groups — one parent GO per slot")]
////////////    [SerializeField] private GameObject groupPlayer;
////////////    [SerializeField] private GameObject groupHead;
////////////    [SerializeField] private GameObject groupHair;
////////////    [SerializeField] private GameObject groupHelmet;
////////////    [SerializeField] private GameObject groupArmor;
////////////    [SerializeField] private GameObject groupWeapon;

////////////    [Header("Helmet hides Hair")]
////////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////////    [SerializeField] private GameObject hairVisualRoot;

////////////    [Header("Tab Buttons")]
////////////    [SerializeField] private Button tabPlayer;
////////////    [SerializeField] private Button tabHead;
////////////    [SerializeField] private Button tabHair;
////////////    [SerializeField] private Button tabHelmet;
////////////    [SerializeField] private Button tabArmor;
////////////    [SerializeField] private Button tabWeapon;

////////////    [Header("Tab Colours")]
////////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////////    [Header("Stats Display (optional)")]
////////////    [SerializeField] private TextMeshProUGUI hpText;
////////////    [SerializeField] private TextMeshProUGUI apText;
////////////    [SerializeField] private TextMeshProUGUI adText;

////////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////////    }

////////////    private void OnDestroy()
////////////    {
////////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////////    }

////////////    private void OnEnable()
////////////    {
////////////        if (soldierEquipment == null)
////////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////////        ForceAllGroupsActive(true);
////////////        BuildGroupCache();
////////////        InitAllButtons();
////////////        ApplyDefaultSelections();
////////////        ShowSlot(_activeSlot);
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    private void OnSoldierSpawned(GameObject soldierGO)
////////////    {
////////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////////        if (eq == null) return;
////////////        soldierEquipment = eq;
////////////        if (!gameObject.activeInHierarchy) return;
////////////        ForceAllGroupsActive(true);
////////////        InitAllButtons();
////////////        ApplyDefaultSelections();
////////////        ShowSlot(_activeSlot);
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////////    private void BuildGroupCache()
////////////    {
////////////        _groups.Clear();
////////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////////        Cache(EquipmentSlot.Face, groupHead);
////////////        Cache(EquipmentSlot.Hair, groupHair);
////////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////////        Cache(EquipmentSlot.Armor, groupArmor);
////////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////////    }

////////////    private void Cache(EquipmentSlot slot, GameObject group)
////////////    {
////////////        var list = new List<InventorySlotButton>();
////////////        if (group != null)
////////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////////        _groups[slot] = list;
////////////    }

////////////    private void InitAllButtons()
////////////    {
////////////        foreach (var kvp in _groups)
////////////            foreach (var btn in kvp.Value)
////////////                btn.Init(this, soldierEquipment);
////////////    }

////////////    // ─── Default Selections ───────────────────────────────────────────────────

////////////    private void ApplyDefaultSelections()
////////////    {
////////////        foreach (var kvp in _groups)
////////////        {
////////////            var slot = kvp.Key;
////////////            var list = kvp.Value;
////////////            if (list.Count == 0) continue;

////////////            if (slot == EquipmentSlot.BodyType)
////////////            {
////////////                // Select only the isDefault button (Skinny Body), deselect rest
////////////                foreach (var btn in list)
////////////                {
////////////                    if (btn.IsDefault) btn.Select();
////////////                    else btn.Deselect();
////////////                }
////////////            }
////////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////////////            {
////////////                // Auto-select the first button — but it CAN be deselected later
////////////                for (int i = 0; i < list.Count; i++)
////////////                {
////////////                    if (i == 0) list[i].Select();
////////////                    else list[i].Deselect();
////////////                }
////////////            }
////////////            else
////////////            {
////////////                // Armor, Helmet, Weapon — nothing selected by default
////////////                foreach (var btn in list)
////////////                    btn.Deselect();
////////////            }
////////////        }
////////////    }

////////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////////    public void SelectButton(InventorySlotButton btn)
////////////    {
////////////        var list = FindGroup(btn);
////////////        if (list == null) return;

////////////        foreach (var b in list)
////////////        {
////////////            if (b == btn) b.Select();
////////////            else if (!b.IsDefault) b.Deselect();
////////////        }

////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    public void DeselectButton(InventorySlotButton btn)
////////////    {
////////////        if (btn.IsDefault) return;   // Skinny Body can never be deselected
////////////        btn.Deselect();
////////////        ApplyHairHelmetRule();
////////////        RefreshStats();
////////////    }

////////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////////    private void ApplyHairHelmetRule()
////////////    {
////////////        if (hairVisualRoot == null) return;

////////////        bool helmetOn = false;
////////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////////            foreach (var btn in helmetGroup)
////////////                if (btn.IsSelected) { helmetOn = true; break; }

////////////        hairVisualRoot.SetActive(!helmetOn);
////////////    }

////////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////////    private void ShowSlot(EquipmentSlot slot)
////////////    {
////////////        _activeSlot = slot;
////////////        UpdateTabColours();
////////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////////    }

////////////    private void ForceAllGroupsActive(bool active)
////////////    {
////////////        SetActive(groupPlayer, active);
////////////        SetActive(groupHead, active);
////////////        SetActive(groupHair, active);
////////////        SetActive(groupHelmet, active);
////////////        SetActive(groupArmor, active);
////////////        SetActive(groupWeapon, active);
////////////    }

////////////    private static void SetActive(GameObject go, bool active)
////////////    {
////////////        if (go != null) go.SetActive(active);
////////////    }

////////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////////    private void UpdateTabColours()
////////////    {
////////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////////        Tint(tabHead, EquipmentSlot.Face);
////////////        Tint(tabHair, EquipmentSlot.Hair);
////////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////////        Tint(tabArmor, EquipmentSlot.Armor);
////////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////////    }

////////////    private void Tint(Button btn, EquipmentSlot slot)
////////////    {
////////////        if (btn == null) return;
////////////        var img = btn.GetComponent<Image>();
////////////        if (img != null)
////////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////////    }

////////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////////    {
////////////        foreach (var kvp in _groups)
////////////            foreach (var btn in kvp.Value)
////////////                if (btn == target) return kvp.Value;
////////////        return null;
////////////    }

////////////    private void RefreshStats()
////////////    {
////////////        if (soldierEquipment == null) return;
////////////        var s = soldierEquipment.GetComponent<SoldierStats>();
////////////        if (s == null) return;
////////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
////////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
////////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
////////////    }

////////////    public void Open() => gameObject.SetActive(true);
////////////    public void Close() => gameObject.SetActive(false);
////////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////////}

//////////using System.Collections.Generic;
//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using TMPro;

///////////// <summary>
///////////// AREA FORGE - InventoryPanel
/////////////
///////////// ── Selection rules ──────────────────────────────────────────────────────────
/////////////   • Body (Skinny)              → isDefault = true → always selected, locked
/////////////   • Face (first) / Hair (first)→ auto-selected on open, can be deselected
/////////////   • Armor / Helmet / Weapon    → nothing selected by default, click to toggle
/////////////
///////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
/////////////   Drag Player/Hair into hairVisualRoot.
/////////////   Helmet selected   → hairVisualRoot hides
/////////////   Helmet deselected → hairVisualRoot shows
///////////// </summary>
//////////public class InventoryPanel : MonoBehaviour
//////////{
//////////    [Header("Soldier — leave empty, found automatically")]
//////////    [SerializeField] private CharacterEquipment soldierEquipment;

//////////    [Header("Item Groups — one parent GO per slot")]
//////////    [SerializeField] private GameObject groupPlayer;
//////////    [SerializeField] private GameObject groupHead;
//////////    [SerializeField] private GameObject groupHair;
//////////    [SerializeField] private GameObject groupHelmet;
//////////    [SerializeField] private GameObject groupArmor;
//////////    [SerializeField] private GameObject groupWeapon;

//////////    [Header("Helmet hides Hair")]
//////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////////    [SerializeField] private GameObject hairVisualRoot;

//////////    [Header("Tab Buttons")]
//////////    [SerializeField] private Button tabPlayer;
//////////    [SerializeField] private Button tabHead;
//////////    [SerializeField] private Button tabHair;
//////////    [SerializeField] private Button tabHelmet;
//////////    [SerializeField] private Button tabArmor;
//////////    [SerializeField] private Button tabWeapon;

//////////    [Header("Tab Colours")]
//////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////////    [Header("Stats Display (optional)")]
//////////    [SerializeField] private TextMeshProUGUI hpText;
//////////    [SerializeField] private TextMeshProUGUI apText;
//////////    [SerializeField] private TextMeshProUGUI adText;

//////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////////    }

//////////    private void OnEnable()
//////////    {
//////////        if (soldierEquipment == null)
//////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////////        ForceAllGroupsActive(true);
//////////        BuildGroupCache();
//////////        InitAllButtons();
//////////        ApplyDefaultSelections();
//////////        ShowSlot(_activeSlot);
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    private void OnSoldierSpawned(GameObject soldierGO)
//////////    {
//////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////////        if (eq == null) return;
//////////        soldierEquipment = eq;
//////////        if (!gameObject.activeInHierarchy) return;
//////////        ForceAllGroupsActive(true);
//////////        InitAllButtons();
//////////        ApplyDefaultSelections();
//////////        ShowSlot(_activeSlot);
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////////    private void BuildGroupCache()
//////////    {
//////////        _groups.Clear();
//////////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////////        Cache(EquipmentSlot.Face, groupHead);
//////////        Cache(EquipmentSlot.Hair, groupHair);
//////////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////////        Cache(EquipmentSlot.Armor, groupArmor);
//////////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////////    }

//////////    private void Cache(EquipmentSlot slot, GameObject group)
//////////    {
//////////        var list = new List<InventorySlotButton>();
//////////        if (group != null)
//////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////////        _groups[slot] = list;
//////////    }

//////////    private void InitAllButtons()
//////////    {
//////////        foreach (var kvp in _groups)
//////////            foreach (var btn in kvp.Value)
//////////                btn.Init(this, soldierEquipment);
//////////    }

//////////    // ─── Default Selections ───────────────────────────────────────────────────

//////////    private void ApplyDefaultSelections()
//////////    {
//////////        foreach (var kvp in _groups)
//////////        {
//////////            var slot = kvp.Key;
//////////            var list = kvp.Value;
//////////            if (list.Count == 0) continue;

//////////            if (slot == EquipmentSlot.BodyType)
//////////            {
//////////                // Only the isDefault button (Skinny Body) stays selected
//////////                foreach (var btn in list)
//////////                {
//////////                    if (btn.IsDefault) btn.Select();
//////////                    else btn.Deselect();
//////////                }
//////////            }
//////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//////////            {
//////////                // First button selected by default — but CAN be deselected later
//////////                for (int i = 0; i < list.Count; i++)
//////////                {
//////////                    if (i == 0) list[i].Select();
//////////                    else list[i].Deselect();
//////////                }
//////////            }
//////////            else
//////////            {
//////////                // Armor, Helmet, Weapon — nothing selected by default
//////////                foreach (var btn in list)
//////////                    btn.Deselect();
//////////            }
//////////        }
//////////    }

//////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

//////////    /// <summary>
//////////    /// Selects btn and deselects ALL other buttons in the same group.
//////////    /// The isDefault (Skinny Body) is protected inside Deselect() itself
//////////    /// so it is safe to call Deselect() on every other button here.
//////////    /// </summary>
//////////    public void SelectButton(InventorySlotButton btn)
//////////    {
//////////        var list = FindGroup(btn);
//////////        if (list == null) return;

//////////        foreach (var b in list)
//////////        {
//////////            if (b == btn) b.Select();
//////////            else b.Deselect();   // Deselect() ignores isDefault buttons internally
//////////        }

//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    /// <summary>
//////////    /// Deselects btn only — user clicked an already-selected item.
//////////    /// </summary>
//////////    public void DeselectButton(InventorySlotButton btn)
//////////    {
//////////        if (btn.IsDefault) return;
//////////        btn.Deselect();
//////////        ApplyHairHelmetRule();
//////////        RefreshStats();
//////////    }

//////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////////    private void ApplyHairHelmetRule()
//////////    {
//////////        if (hairVisualRoot == null) return;

//////////        bool helmetOn = false;
//////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////////            foreach (var btn in helmetGroup)
//////////                if (btn.IsSelected) { helmetOn = true; break; }

//////////        hairVisualRoot.SetActive(!helmetOn);
//////////    }

//////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////////    private void ShowSlot(EquipmentSlot slot)
//////////    {
//////////        _activeSlot = slot;
//////////        UpdateTabColours();
//////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////////    }

//////////    private void ForceAllGroupsActive(bool active)
//////////    {
//////////        SetActive(groupPlayer, active);
//////////        SetActive(groupHead, active);
//////////        SetActive(groupHair, active);
//////////        SetActive(groupHelmet, active);
//////////        SetActive(groupArmor, active);
//////////        SetActive(groupWeapon, active);
//////////    }

//////////    private static void SetActive(GameObject go, bool active)
//////////    {
//////////        if (go != null) go.SetActive(active);
//////////    }

//////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////////    private void UpdateTabColours()
//////////    {
//////////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////////        Tint(tabHead, EquipmentSlot.Face);
//////////        Tint(tabHair, EquipmentSlot.Hair);
//////////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////////        Tint(tabArmor, EquipmentSlot.Armor);
//////////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////////    }

//////////    private void Tint(Button btn, EquipmentSlot slot)
//////////    {
//////////        if (btn == null) return;
//////////        var img = btn.GetComponent<Image>();
//////////        if (img != null)
//////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////////    }

//////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////////    {
//////////        foreach (var kvp in _groups)
//////////            foreach (var btn in kvp.Value)
//////////                if (btn == target) return kvp.Value;
//////////        return null;
//////////    }

//////////    private void RefreshStats()
//////////    {
//////////        if (soldierEquipment == null) return;
//////////        var s = soldierEquipment.GetComponent<SoldierStats>();
//////////        if (s == null) return;
//////////        if (hpText != null) hpText.text = $"HP  {s.MaxHealth:F0}";
//////////        if (apText != null) apText.text = $"AP  {s.AbilityPower:F0}";
//////////        if (adText != null) adText.text = $"AD  {s.AttackDamage:F0}";
//////////    }

//////////    public void Open() => gameObject.SetActive(true);
//////////    public void Close() => gameObject.SetActive(false);
//////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//////////}

////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.UI;
////////using TMPro;

/////////// <summary>
/////////// AREA FORGE - InventoryPanel
///////////
/////////// ── Selection rules ──────────────────────────────────────────────────────────
///////////   • Body (Skinny)              → isDefault = true → always selected, locked
///////////   • Face (first) / Hair (first)→ auto-selected on open, can be deselected
///////////   • Armor / Helmet / Weapon    → nothing selected by default, click to toggle
///////////
/////////// ── Stats display ────────────────────────────────────────────────────────────
///////////   Drag the three bar fill Images (healthBarFill, abilityBarFill, damageBarFill)
///////////   and the three text labels into the Inspector.
///////////   These update live whenever equipment changes.
///////////
/////////// ── Helmet / Hair rule ───────────────────────────────────────────────────────
///////////   Drag Player/Hair into hairVisualRoot — hidden when any helmet is selected.
/////////// </summary>
////////public class InventoryPanel : MonoBehaviour
////////{
////////    [Header("Soldier — leave empty, found automatically")]
////////    [SerializeField] private CharacterEquipment soldierEquipment;

////////    [Header("Item Groups — one parent GO per slot")]
////////    [SerializeField] private GameObject groupPlayer;
////////    [SerializeField] private GameObject groupHead;
////////    [SerializeField] private GameObject groupHair;
////////    [SerializeField] private GameObject groupHelmet;
////////    [SerializeField] private GameObject groupArmor;
////////    [SerializeField] private GameObject groupWeapon;

////////    [Header("Helmet hides Hair")]
////////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////////    [SerializeField] private GameObject hairVisualRoot;

////////    [Header("Tab Buttons")]
////////    [SerializeField] private Button tabPlayer;
////////    [SerializeField] private Button tabHead;
////////    [SerializeField] private Button tabHair;
////////    [SerializeField] private Button tabHelmet;
////////    [SerializeField] private Button tabArmor;
////////    [SerializeField] private Button tabWeapon;

////////    [Header("Tab Colours")]
////////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////////    [Header("Stats — Bar Fills (Image Type: Filled → Horizontal → Left)")]
////////    [SerializeField] private Image healthBarFill;
////////    [SerializeField] private Image abilityBarFill;
////////    [SerializeField] private Image damageBarFill;

////////    [Header("Stats — Text Labels (TextMeshPro)")]
////////    [SerializeField] private TextMeshProUGUI healthText;
////////    [SerializeField] private TextMeshProUGUI abilityText;
////////    [SerializeField] private TextMeshProUGUI damageText;

////////    [Header("Bar Max Reference Values")]
////////    [Tooltip("Ability value that = 100% full bar")]
////////    [SerializeField] private float maxAbilityDisplay = 100f;
////////    [Tooltip("Damage value that = 100% full bar")]
////////    [SerializeField] private float maxDamageDisplay = 100f;

////////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////////    }

////////    private void OnDestroy()
////////    {
////////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////////        UnsubscribeStats();
////////    }

////////    private void OnEnable()
////////    {
////////        if (soldierEquipment == null)
////////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////////        SubscribeStats();
////////        ForceAllGroupsActive(true);
////////        BuildGroupCache();
////////        InitAllButtons();
////////        ApplyDefaultSelections();
////////        ShowSlot(_activeSlot);
////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    private void OnDisable()
////////    {
////////        UnsubscribeStats();
////////    }

////////    private void OnSoldierSpawned(GameObject soldierGO)
////////    {
////////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////////        if (eq == null) return;
////////        UnsubscribeStats();
////////        soldierEquipment = eq;
////////        if (!gameObject.activeInHierarchy) return;
////////        SubscribeStats();
////////        ForceAllGroupsActive(true);
////////        InitAllButtons();
////////        ApplyDefaultSelections();
////////        ShowSlot(_activeSlot);
////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    // ─── Stats subscription ───────────────────────────────────────────────────

////////    private void SubscribeStats()
////////    {
////////        var stats = GetStats();
////////        if (stats != null) stats.OnStatsChanged += OnStatsChanged;
////////    }

////////    private void UnsubscribeStats()
////////    {
////////        var stats = GetStats();
////////        if (stats != null) stats.OnStatsChanged -= OnStatsChanged;
////////    }

////////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

////////    private SoldierStats GetStats() =>
////////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

////////    // ─── Group Cache ──────────────────────────────────────────────────────────

////////    private void BuildGroupCache()
////////    {
////////        _groups.Clear();
////////        Cache(EquipmentSlot.BodyType, groupPlayer);
////////        Cache(EquipmentSlot.Face, groupHead);
////////        Cache(EquipmentSlot.Hair, groupHair);
////////        Cache(EquipmentSlot.Helmet, groupHelmet);
////////        Cache(EquipmentSlot.Armor, groupArmor);
////////        Cache(EquipmentSlot.Weapon, groupWeapon);
////////    }

////////    private void Cache(EquipmentSlot slot, GameObject group)
////////    {
////////        var list = new List<InventorySlotButton>();
////////        if (group != null)
////////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////////        _groups[slot] = list;
////////    }

////////    private void InitAllButtons()
////////    {
////////        foreach (var kvp in _groups)
////////            foreach (var btn in kvp.Value)
////////                btn.Init(this, soldierEquipment);
////////    }

////////    // ─── Default Selections ───────────────────────────────────────────────────

////////    private void ApplyDefaultSelections()
////////    {
////////        foreach (var kvp in _groups)
////////        {
////////            var slot = kvp.Key;
////////            var list = kvp.Value;
////////            if (list.Count == 0) continue;

////////            if (slot == EquipmentSlot.BodyType)
////////            {
////////                foreach (var btn in list)
////////                {
////////                    if (btn.IsDefault) btn.Select();
////////                    else btn.Deselect();
////////                }
////////            }
////////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////////            {
////////                for (int i = 0; i < list.Count; i++)
////////                {
////////                    if (i == 0) list[i].Select();
////////                    else list[i].Deselect();
////////                }
////////            }
////////            else
////////            {
////////                foreach (var btn in list) btn.Deselect();
////////            }
////////        }
////////    }

////////    // ─── Public: called by InventorySlotButton ────────────────────────────────

////////    public void SelectButton(InventorySlotButton btn)
////////    {
////////        var list = FindGroup(btn);
////////        if (list == null) return;

////////        // Select the tapped button, deselect all others
////////        foreach (var b in list)
////////        {
////////            if (b == btn) b.Select();
////////            else b.Deselect();
////////        }

////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    public void DeselectButton(InventorySlotButton btn)
////////    {
////////        if (btn.IsDefault) return;
////////        btn.Deselect();
////////        ApplyHairHelmetRule();
////////        RefreshStats();
////////    }

////////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////////    private void ApplyHairHelmetRule()
////////    {
////////        if (hairVisualRoot == null) return;

////////        bool helmetOn = false;
////////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////////            foreach (var btn in helmetGroup)
////////                if (btn.IsSelected) { helmetOn = true; break; }

////////        hairVisualRoot.SetActive(!helmetOn);
////////    }

////////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////////    private void ShowSlot(EquipmentSlot slot)
////////    {
////////        _activeSlot = slot;
////////        UpdateTabColours();
////////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////////        SetActive(groupHead, slot == EquipmentSlot.Face);
////////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////////    }

////////    private void ForceAllGroupsActive(bool active)
////////    {
////////        SetActive(groupPlayer, active);
////////        SetActive(groupHead, active);
////////        SetActive(groupHair, active);
////////        SetActive(groupHelmet, active);
////////        SetActive(groupArmor, active);
////////        SetActive(groupWeapon, active);
////////    }

////////    private static void SetActive(GameObject go, bool active)
////////    {
////////        if (go != null) go.SetActive(active);
////////    }

////////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////////    private void UpdateTabColours()
////////    {
////////        Tint(tabPlayer, EquipmentSlot.BodyType);
////////        Tint(tabHead, EquipmentSlot.Face);
////////        Tint(tabHair, EquipmentSlot.Hair);
////////        Tint(tabHelmet, EquipmentSlot.Helmet);
////////        Tint(tabArmor, EquipmentSlot.Armor);
////////        Tint(tabWeapon, EquipmentSlot.Weapon);
////////    }

////////    private void Tint(Button btn, EquipmentSlot slot)
////////    {
////////        if (btn == null) return;
////////        var img = btn.GetComponent<Image>();
////////        if (img != null)
////////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////////    }

////////    // ─── Stats Refresh ────────────────────────────────────────────────────────

////////    private void RefreshStats()
////////    {
////////        var stats = GetStats();
////////        if (stats == null) return;

////////        // Health
////////        float hp = stats.HealthPercent;
////////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
////////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

////////        // Ability
////////        float ap = Mathf.Clamp01(stats.AbilityPower / maxAbilityDisplay);
////////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
////////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

////////        // Damage
////////        float ad = Mathf.Clamp01(stats.AttackDamage / maxDamageDisplay);
////////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
////////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
////////    }

////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////////    {
////////        foreach (var kvp in _groups)
////////            foreach (var btn in kvp.Value)
////////                if (btn == target) return kvp.Value;
////////        return null;
////////    }

////////    public void Open() => gameObject.SetActive(true);
////////    public void Close() => gameObject.SetActive(false);
////////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////////}

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// AREA FORGE - InventoryPanel (Army / Customize Panel)
/////////
///////// ── What it does ─────────────────────────────────────────────────────────────
/////////   Shows one tab per equipment slot (Armor, Helmet, Weapon, Hair, etc.).
/////////   Clicking a tab shows that slot's item buttons.
/////////   Clicking an item button equips it → stats bars update instantly.
/////////
///////// ── Stat bar sync (how it works) ─────────────────────────────────────────────
/////////   Each EquipmentItem ScriptableObject has healthBonus / abilityBonus / damageBonus.
/////////   When you click an item:
/////////     1. InventorySlotButton.Select() → CharacterEquipment.Equip(item)
/////////     2. CharacterEquipment calls SoldierStats.ApplyEquipmentBonus(+bonuses)
/////////        and removes the old item's bonuses first (-oldBonuses)
/////////     3. SoldierStats fires OnStatsChanged
/////////     4. InventoryPanel.OnStatsChanged() → RefreshStats() → bars + text update
/////////
/////////   So ALL you need to do in Unity is:
/////////     a) Set healthBonus / abilityBonus / damageBonus on each EquipmentItem asset
/////////     b) Wire the three bar Images and text labels in the Inspector (see below)
/////////
///////// ── Inspector wiring ─────────────────────────────────────────────────────────
/////////   soldierEquipment → leave EMPTY — found automatically at runtime
/////////
/////////   Item Groups → drag each slot's button-parent GO:
/////////     groupPlayer  = BodyType buttons parent
/////////     groupHead    = Face buttons parent
/////////     groupHair    = Hair buttons parent
/////////     groupHelmet  = Helmet buttons parent
/////////     groupArmor   = Armor buttons parent
/////////     groupWeapon  = Weapon buttons parent
/////////
/////////   Tab Buttons → drag each tab Button (one per slot)
/////////
/////////   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
/////////
/////////   ── Stats Panel (wire these to see live stat bars) ────────────────────────
/////////   healthBarFill  → Image (Filled, Horizontal, Left) for health
/////////   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
/////////   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
/////////   healthText     → TextMeshProUGUI (shows MaxHealth number)
/////////   abilityText    → TextMeshProUGUI (shows AbilityPower number)
/////////   damageText     → TextMeshProUGUI (shows AttackDamage number)
/////////   maxAbilityDisplay → value that = 100% full ability bar (e.g. 100)
/////////   maxDamageDisplay  → value that = 100% full damage  bar (e.g. 100)
/////////
///////// ── EquipmentItem stat fields to fill in the Project window ──────────────────
/////////   For each armor/helmet/weapon asset, set:
/////////     Health Bonus   → e.g. 20 for heavy armor
/////////     Ability Bonus  → e.g. 15 for a magic staff
/////////     Damage Bonus   → e.g. 10 for a sword
/////////   Cosmetic-only items (hair, face, body) can stay at 0.
///////// </summary>
//////public class InventoryPanel : MonoBehaviour
//////{
//////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//////    [Header("Soldier — leave empty, found automatically")]
//////    [SerializeField] private CharacterEquipment soldierEquipment;

//////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//////    [Header("Item Groups — one parent GO per slot")]
//////    [SerializeField] private GameObject groupPlayer;   // BodyType
//////    [SerializeField] private GameObject groupHead;     // Face
//////    [SerializeField] private GameObject groupHair;
//////    [SerializeField] private GameObject groupHelmet;
//////    [SerializeField] private GameObject groupArmor;
//////    [SerializeField] private GameObject groupWeapon;

//////    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

//////    [Header("Helmet hides Hair")]
//////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//////    [SerializeField] private GameObject hairVisualRoot;

//////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//////    [Header("Tab Buttons")]
//////    [SerializeField] private Button tabPlayer;
//////    [SerializeField] private Button tabHead;
//////    [SerializeField] private Button tabHair;
//////    [SerializeField] private Button tabHelmet;
//////    [SerializeField] private Button tabArmor;
//////    [SerializeField] private Button tabWeapon;

//////    [Header("Tab Colours")]
//////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//////    // ─── Inspector — Stat Bars (the Army-panel HUD) ───────────────────────────

//////    [Header("Stats Bars — wire these to see live stat changes on equip")]
//////    [Tooltip("Image (Filled, Horizontal, Left) — health bar in the Army panel")]
//////    [SerializeField] private Image healthBarFill;
//////    [Tooltip("Image (Filled, Horizontal, Left) — ability power bar")]
//////    [SerializeField] private Image abilityBarFill;
//////    [Tooltip("Image (Filled, Horizontal, Left) — attack damage bar")]
//////    [SerializeField] private Image damageBarFill;

//////    [Header("Stats Labels (TextMeshPro — optional)")]
//////    [SerializeField] private TextMeshProUGUI healthText;
//////    [SerializeField] private TextMeshProUGUI abilityText;
//////    [SerializeField] private TextMeshProUGUI damageText;

//////    [Header("Bar Max Reference Values")]
//////    [Tooltip("MaxHealth value that fills the health bar to 100%. E.g. if max possible HP is 200, set 200.")]
//////    [SerializeField] private float maxHealthDisplay = 200f;
//////    [Tooltip("Ability value that fills the bar to 100%. E.g. 100.")]
//////    [SerializeField] private float maxAbilityDisplay = 100f;
//////    [Tooltip("Damage value that fills the bar to 100%. E.g. 100.")]
//////    [SerializeField] private float maxDamageDisplay = 100f;

//////    // ─── Private ──────────────────────────────────────────────────────────────

//////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        // Wire tab clicks
//////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//////    }

//////    private void OnDestroy()
//////    {
//////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//////        UnsubscribeStats();
//////    }

//////    private void OnEnable()
//////    {
//////        // Try to find the soldier if not already set
//////        if (soldierEquipment == null)
//////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//////        SubscribeStats();

//////        // All groups must be active before Init() so Awake runs on every button
//////        ForceAllGroupsActive(true);
//////        BuildGroupCache();
//////        InitAllButtons();
//////        ApplyDefaultSelections();

//////        // Restore correct group visibility and tab colour
//////        ShowSlot(_activeSlot);
//////        ApplyHairHelmetRule();

//////        // Immediately show the current stats (base + any already-equipped bonuses)
//////        RefreshStats();
//////    }

//////    private void OnDisable()
//////    {
//////        UnsubscribeStats();
//////    }

//////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

//////    private void OnSoldierSpawned(GameObject soldierGO)
//////    {
//////        var eq = soldierGO.GetComponent<CharacterEquipment>();
//////        if (eq == null) return;

//////        UnsubscribeStats();
//////        soldierEquipment = eq;

//////        if (!gameObject.activeInHierarchy) return;

//////        SubscribeStats();
//////        ForceAllGroupsActive(true);
//////        InitAllButtons();
//////        ApplyDefaultSelections();
//////        ShowSlot(_activeSlot);
//////        ApplyHairHelmetRule();
//////        RefreshStats();
//////    }

//////    // ─── Stats Subscription ───────────────────────────────────────────────────

//////    /// <summary>
//////    /// Subscribes to OnStatsChanged so the bars update the moment an item
//////    /// is equipped or unequipped (triggered from CharacterEquipment.Equip/Unequip
//////    /// → SoldierStats.ApplyEquipmentBonus → OnStatsChanged).
//////    /// </summary>
//////    private void SubscribeStats()
//////    {
//////        var s = GetStats();
//////        if (s != null) s.OnStatsChanged += OnStatsChanged;
//////    }

//////    private void UnsubscribeStats()
//////    {
//////        var s = GetStats();
//////        if (s != null) s.OnStatsChanged -= OnStatsChanged;
//////    }

//////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

//////    private SoldierStats GetStats() =>
//////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

//////    // ─── Group Cache ──────────────────────────────────────────────────────────

//////    private void BuildGroupCache()
//////    {
//////        _groups.Clear();
//////        Cache(EquipmentSlot.BodyType, groupPlayer);
//////        Cache(EquipmentSlot.Face, groupHead);
//////        Cache(EquipmentSlot.Hair, groupHair);
//////        Cache(EquipmentSlot.Helmet, groupHelmet);
//////        Cache(EquipmentSlot.Armor, groupArmor);
//////        Cache(EquipmentSlot.Weapon, groupWeapon);
//////    }

//////    private void Cache(EquipmentSlot slot, GameObject group)
//////    {
//////        var list = new List<InventorySlotButton>();
//////        if (group != null)
//////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//////        _groups[slot] = list;
//////    }

//////    // ─── Button Init ──────────────────────────────────────────────────────────

//////    private void InitAllButtons()
//////    {
//////        foreach (var kvp in _groups)
//////            foreach (var btn in kvp.Value)
//////                btn.Init(this, soldierEquipment);
//////    }

//////    // ─── Default Selections ───────────────────────────────────────────────────

//////    private void ApplyDefaultSelections()
//////    {
//////        foreach (var kvp in _groups)
//////        {
//////            var slot = kvp.Key;
//////            var list = kvp.Value;
//////            if (list.Count == 0) continue;

//////            if (slot == EquipmentSlot.BodyType)
//////            {
//////                // Skinny Body is isDefault — always selected, cannot be deselected
//////                foreach (var btn in list)
//////                {
//////                    if (btn.IsDefault) btn.Select();
//////                    else btn.Deselect();
//////                }
//////            }
//////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//////            {
//////                // First button selected by default — can be deselected later
//////                for (int i = 0; i < list.Count; i++)
//////                {
//////                    if (i == 0) list[i].Select();
//////                    else list[i].Deselect();
//////                }
//////            }
//////            else
//////            {
//////                // Armor, Helmet, Weapon — nothing selected by default
//////                foreach (var btn in list) btn.Deselect();
//////            }
//////        }
//////    }

//////    // ─── Public API (called by InventorySlotButton) ───────────────────────────

//////    /// <summary>
//////    /// Selects btn and deselects all others in the same slot group.
//////    /// The stats bars update automatically via the OnStatsChanged event.
//////    /// </summary>
//////    public void SelectButton(InventorySlotButton btn)
//////    {
//////        var list = FindGroup(btn);
//////        if (list == null) return;

//////        foreach (var b in list)
//////        {
//////            if (b == btn) b.Select();
//////            else b.Deselect();
//////        }

//////        ApplyHairHelmetRule();
//////        // RefreshStats is called via OnStatsChanged event — no need to call it here
//////        // but we call it defensively in case the item has no stat bonuses (no event fires)
//////        RefreshStats();
//////    }

//////    /// <summary>
//////    /// Deselects btn only — user clicked an already-selected, non-default item.
//////    /// </summary>
//////    public void DeselectButton(InventorySlotButton btn)
//////    {
//////        if (btn.IsDefault) return;
//////        btn.Deselect();
//////        ApplyHairHelmetRule();
//////        RefreshStats();
//////    }

//////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//////    private void ApplyHairHelmetRule()
//////    {
//////        if (hairVisualRoot == null) return;

//////        bool helmetOn = false;
//////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//////            foreach (var btn in helmetGroup)
//////                if (btn.IsSelected) { helmetOn = true; break; }

//////        hairVisualRoot.SetActive(!helmetOn);
//////    }

//////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//////    private void ShowSlot(EquipmentSlot slot)
//////    {
//////        _activeSlot = slot;
//////        UpdateTabColours();
//////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//////        SetActive(groupHead, slot == EquipmentSlot.Face);
//////        SetActive(groupHair, slot == EquipmentSlot.Hair);
//////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//////    }

//////    private void ForceAllGroupsActive(bool active)
//////    {
//////        SetActive(groupPlayer, active);
//////        SetActive(groupHead, active);
//////        SetActive(groupHair, active);
//////        SetActive(groupHelmet, active);
//////        SetActive(groupArmor, active);
//////        SetActive(groupWeapon, active);
//////    }

//////    private static void SetActive(GameObject go, bool active)
//////    {
//////        if (go != null) go.SetActive(active);
//////    }

//////    // ─── Tab Colours ──────────────────────────────────────────────────────────

//////    private void UpdateTabColours()
//////    {
//////        Tint(tabPlayer, EquipmentSlot.BodyType);
//////        Tint(tabHead, EquipmentSlot.Face);
//////        Tint(tabHair, EquipmentSlot.Hair);
//////        Tint(tabHelmet, EquipmentSlot.Helmet);
//////        Tint(tabArmor, EquipmentSlot.Armor);
//////        Tint(tabWeapon, EquipmentSlot.Weapon);
//////    }

//////    private void Tint(Button btn, EquipmentSlot slot)
//////    {
//////        if (btn == null) return;
//////        var img = btn.GetComponent<Image>();
//////        if (img != null)
//////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//////    }

//////    // ─── Stats Refresh ────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Updates all three stat bars and labels from the current SoldierStats values
//////    /// (base stats + equipment bonuses).  Called on every equip/unequip and on open.
//////    /// </summary>
//////    private void RefreshStats()
//////    {
//////        var stats = GetStats();
//////        if (stats == null) return;

//////        // ── Health ────────────────────────────────────────────────────────────
//////        // KEY FIX: use MaxHealth/maxHealthDisplay so bar grows when armor is equipped.
//////        // Old code used HealthPercent (CurrentHP/MaxHP) = always 1.0 at full health.
//////        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
//////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
//////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

//////        // ── Ability Power ─────────────────────────────────────────────────────
//////        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
//////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

//////        // ── Attack Damage ─────────────────────────────────────────────────────
//////        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
//////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
//////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//////    }

//////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//////    {
//////        foreach (var kvp in _groups)
//////            foreach (var btn in kvp.Value)
//////                if (btn == target) return kvp.Value;
//////        return null;
//////    }

//////    // ─── Open / Close ─────────────────────────────────────────────────────────

//////    public void Open() => gameObject.SetActive(true);
//////    public void Close() => gameObject.SetActive(false);
//////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);

//////    /// <summary>
//////    /// Copies all currently-selected items from the Army panel to a spawned soldier.
//////    /// Called by GameManager right after Instantiate().
//////    /// </summary>
//////    public void ApplySelectionToSoldier(CharacterEquipment target)
//////    {
//////        if (target == null)
//////        {
//////            Debug.LogError("[InventoryPanel] ApplySelectionToSoldier: target is null!");
//////            return;
//////        }

//////        int applied = 0;
//////        foreach (var kvp in _groups)
//////        {
//////            foreach (var btn in kvp.Value)
//////            {
//////                if (btn.IsSelected && btn.Item != null)
//////                {
//////                    target.Equip(btn.Item);
//////                    applied++;
//////                    Debug.Log($"[InventoryPanel] Applied {btn.Item.itemName} to spawned soldier.");
//////                }
//////            }
//////        }

//////        if (applied == 0)
//////            Debug.LogWarning("[InventoryPanel] No items were selected — soldier spawned with no equipment.");
//////    }
//////}


////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// AREA FORGE - InventoryPanel (Army / Customize Panel)
///////
/////// ── Two-step selection flow ───────────────────────────────────────────────────
///////   1. Player clicks an item button
///////      → InventorySlotButton.OnClick() → SetPendingButton(btn)
///////      → The button shows a WHITE border ("pending")
///////      → The preview character is NOT updated yet
///////
///////   2. Player clicks the SELECT button (wire it to ConfirmPendingForActiveSlot)
///////      → The pending item is equipped on the preview character
///////      → CharacterEquipment.Equip() → CharacterVisuals + SpriteLayerAnimator update
///////      → Stats bars refresh
///////      → Button shows GOLD/rarity border ("confirmed")
///////
///////   3. Player clicks BUY button (wire it to GameManager.SpawnBasicSoldier)
///////      → Soldier with all confirmed items is spawned in the Village panel
///////
/////// ── Inspector wiring ─────────────────────────────────────────────────────────
///////   soldierEquipment → leave EMPTY — found automatically at runtime
///////
///////   Item Groups → drag each slot's button-parent GO:
///////     groupPlayer  = BodyType buttons parent  (Content/BODY)
///////     groupHead    = Face buttons parent       (Content/FACE)
///////     groupHair    = Hair buttons parent       (Content/HAIR)
///////     groupHelmet  = Helmet buttons parent     (Content/HELMET)
///////     groupArmor   = Armor buttons parent      (Content/ARMOR)
///////     groupWeapon  = Weapon buttons parent     (Content/WEAPON)
///////
///////   Tab Buttons → drag each tab Button
///////   hairVisualRoot → drag Player/Hair (hidden when a helmet is confirmed)
///////
///////   SELECT Button → wire its OnClick() to ConfirmPendingForActiveSlot()
///////   BUY Button    → wire its OnClick() to GameManager.SpawnBasicSoldier()
///////
///////   ── Stats bars (wire to see live changes) ────────────────────────────────
///////   healthBarFill  → Image (Filled, Horizontal, Left)
///////   abilityBarFill → Image (Filled, Horizontal, Left)
///////   damageBarFill  → Image (Filled, Horizontal, Left)
///////   healthText / abilityText / damageText → TextMeshProUGUI labels
///////   maxHealthDisplay / maxAbilityDisplay / maxDamageDisplay → bar ceiling values
/////// </summary>
////public class InventoryPanel : MonoBehaviour
////{
////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////    [Header("Soldier — leave empty, found automatically")]
////    [SerializeField] private CharacterEquipment soldierEquipment;

////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////    [Header("Item Groups — one parent GO per slot")]
////    [SerializeField] private GameObject groupPlayer;   // BodyType
////    [SerializeField] private GameObject groupHead;     // Face
////    [SerializeField] private GameObject groupHair;
////    [SerializeField] private GameObject groupHelmet;
////    [SerializeField] private GameObject groupArmor;
////    [SerializeField] private GameObject groupWeapon;

////    // ─── Inspector — Helmet / Hair rule ──────────────────────────────────────

////    [Header("Helmet hides Hair")]
////    [Tooltip("Drag Player/Hair here — hidden when a helmet is CONFIRMED (equipped).")]
////    [SerializeField] private GameObject hairVisualRoot;

////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button tabPlayer;
////    [SerializeField] private Button tabHead;
////    [SerializeField] private Button tabHair;
////    [SerializeField] private Button tabHelmet;
////    [SerializeField] private Button tabArmor;
////    [SerializeField] private Button tabWeapon;

////    [Header("Tab Colours")]
////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////    // ─── Inspector — Stats ────────────────────────────────────────────────────

////    [Header("Stats Bars (Image Type: Filled → Horizontal → Left)")]
////    [SerializeField] private Image healthBarFill;
////    [SerializeField] private Image abilityBarFill;
////    [SerializeField] private Image damageBarFill;

////    [Header("Stats Labels (TextMeshPro — optional)")]
////    [SerializeField] private TextMeshProUGUI healthText;
////    [SerializeField] private TextMeshProUGUI abilityText;
////    [SerializeField] private TextMeshProUGUI damageText;

////    [Header("Bar Max Reference Values")]
////    [Tooltip("MaxHealth that fills the bar 100%")]
////    [SerializeField] private float maxHealthDisplay = 200f;
////    [Tooltip("AbilityPower that fills the bar 100%")]
////    [SerializeField] private float maxAbilityDisplay = 100f;
////    [Tooltip("AttackDamage that fills the bar 100%")]
////    [SerializeField] private float maxDamageDisplay = 100f;

////    // ─── Private ──────────────────────────────────────────────────────────────

////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();
////    private readonly Dictionary<EquipmentSlot, InventorySlotButton> _pending = new();

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////    }

////    private void OnDestroy()
////    {
////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////        UnsubscribeStats();
////    }

////    private void OnEnable()
////    {
////        if (soldierEquipment == null)
////            soldierEquipment = FindObjectOfType<CharacterEquipment>();

////        SubscribeStats();
////        ForceAllGroupsActive(true);
////        BuildGroupCache();
////        InitAllButtons();
////        ApplyDefaultSelections();
////        ShowSlot(_activeSlot);
////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    private void OnDisable()
////    {
////        UnsubscribeStats();
////    }

////    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

////    private void OnSoldierSpawned(GameObject soldierGO)
////    {
////        var eq = soldierGO.GetComponent<CharacterEquipment>();
////        if (eq == null) return;

////        UnsubscribeStats();
////        soldierEquipment = eq;

////        if (!gameObject.activeInHierarchy) return;

////        SubscribeStats();
////        ForceAllGroupsActive(true);
////        InitAllButtons();
////        ApplyDefaultSelections();
////        ShowSlot(_activeSlot);
////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    // ─── Stats Subscription ───────────────────────────────────────────────────

////    private void SubscribeStats()
////    {
////        var s = GetStats();
////        if (s != null) s.OnStatsChanged += OnStatsChanged;
////    }

////    private void UnsubscribeStats()
////    {
////        var s = GetStats();
////        if (s != null) s.OnStatsChanged -= OnStatsChanged;
////    }

////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

////    private SoldierStats GetStats() =>
////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

////    // ─── Group Cache ──────────────────────────────────────────────────────────

////    private void BuildGroupCache()
////    {
////        _groups.Clear();
////        Cache(EquipmentSlot.BodyType, groupPlayer);
////        Cache(EquipmentSlot.Face, groupHead);
////        Cache(EquipmentSlot.Hair, groupHair);
////        Cache(EquipmentSlot.Helmet, groupHelmet);
////        Cache(EquipmentSlot.Armor, groupArmor);
////        Cache(EquipmentSlot.Weapon, groupWeapon);
////    }

////    private void Cache(EquipmentSlot slot, GameObject group)
////    {
////        var list = new List<InventorySlotButton>();
////        if (group != null)
////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////        _groups[slot] = list;
////    }

////    private void InitAllButtons()
////    {
////        foreach (var kvp in _groups)
////            foreach (var btn in kvp.Value)
////                btn.Init(this, soldierEquipment);
////    }

////    // ─── Default Selections (applied on open — no confirm step needed) ────────

////    private void ApplyDefaultSelections()
////    {
////        _pending.Clear();

////        foreach (var kvp in _groups)
////        {
////            var slot = kvp.Key;
////            var list = kvp.Value;
////            if (list.Count == 0) continue;

////            if (slot == EquipmentSlot.BodyType)
////            {
////                foreach (var btn in list)
////                {
////                    if (btn.IsDefault) btn.Select();
////                    else btn.Deselect();
////                }
////            }
////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////            {
////                // Auto-select first item immediately (no confirm step for defaults)
////                for (int i = 0; i < list.Count; i++)
////                {
////                    if (i == 0) list[i].Select();
////                    else list[i].Deselect();
////                }
////            }
////            else
////            {
////                // Armor, Helmet, Weapon — nothing selected by default
////                foreach (var btn in list) btn.Deselect();
////            }
////        }
////    }

////    // ─── PENDING — called by InventorySlotButton.OnClick() ───────────────────

////    /// <summary>
////    /// Marks btn as PENDING for its slot group.
////    /// WHITE border is shown; the preview character is NOT updated yet.
////    /// Any previously-pending button in the same group loses its pending state.
////    /// </summary>
////    public void SetPendingButton(InventorySlotButton btn)
////    {
////        var slot = GetSlotForButton(btn);
////        if (slot == null) return;

////        // Clear old pending for this slot
////        if (_pending.TryGetValue(slot.Value, out var old) && old != null && old != btn)
////            old.ClearPending();

////        _pending[slot.Value] = btn;
////        btn.SetPending();

////        Debug.Log($"[InventoryPanel] Pending: {btn.Item?.itemName} ({slot})");
////    }

////    // ─── CONFIRM — called by the SELECT button ────────────────────────────────

////    /// <summary>
////    /// Confirms the pending item for the CURRENTLY ACTIVE tab slot.
////    /// Wire the SELECT button's OnClick() event to this method.
////    ///
////    /// Flow:
////    ///   1. Gets the pending button for the active slot.
////    ///   2. Deselects the previously-confirmed button in that slot group.
////    ///   3. Calls ConfirmSelect() → equips the item → preview updates + animation.
////    ///   4. Refreshes stats bars.
////    /// </summary>
////    public void ConfirmPendingForActiveSlot()
////    {
////        if (!_pending.TryGetValue(_activeSlot, out var pendingBtn) || pendingBtn == null)
////        {
////            Debug.Log("[InventoryPanel] SELECT pressed but no pending item for this slot.");
////            return;
////        }

////        if (!_groups.TryGetValue(_activeSlot, out var group)) return;

////        // Deselect the old confirmed button in this group (except the pending one)
////        foreach (var btn in group)
////        {
////            if (btn != pendingBtn && btn.IsSelected)
////                btn.Deselect();
////        }

////        // Confirm the pending button → equips on preview
////        pendingBtn.ConfirmSelect();
////        _pending.Remove(_activeSlot);

////        ApplyHairHelmetRule();
////        RefreshStats();

////        Debug.Log($"[InventoryPanel] Confirmed: {pendingBtn.Item?.itemName} on {_activeSlot}");
////    }

////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////    private void ShowSlot(EquipmentSlot slot)
////    {
////        _activeSlot = slot;
////        UpdateTabColours();
////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////        SetActive(groupHead, slot == EquipmentSlot.Face);
////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////    }

////    private void ForceAllGroupsActive(bool active)
////    {
////        SetActive(groupPlayer, active);
////        SetActive(groupHead, active);
////        SetActive(groupHair, active);
////        SetActive(groupHelmet, active);
////        SetActive(groupArmor, active);
////        SetActive(groupWeapon, active);
////    }

////    private static void SetActive(GameObject go, bool active)
////    {
////        if (go != null) go.SetActive(active);
////    }

////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////    private void UpdateTabColours()
////    {
////        Tint(tabPlayer, EquipmentSlot.BodyType);
////        Tint(tabHead, EquipmentSlot.Face);
////        Tint(tabHair, EquipmentSlot.Hair);
////        Tint(tabHelmet, EquipmentSlot.Helmet);
////        Tint(tabArmor, EquipmentSlot.Armor);
////        Tint(tabWeapon, EquipmentSlot.Weapon);
////    }

////    private void Tint(Button btn, EquipmentSlot slot)
////    {
////        if (btn == null) return;
////        var img = btn.GetComponent<Image>();
////        if (img != null)
////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////    }

////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////    private void ApplyHairHelmetRule()
////    {
////        if (hairVisualRoot == null) return;

////        bool helmetOn = false;
////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////            foreach (var btn in helmetGroup)
////                if (btn.IsSelected) { helmetOn = true; break; }

////        hairVisualRoot.SetActive(!helmetOn);
////    }

////    // ─── Stats Refresh ────────────────────────────────────────────────────────

////    private void RefreshStats()
////    {
////        var stats = GetStats();
////        if (stats == null) return;

////        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
////        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
////        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
////        if (damageBarFill != null) damageBarFill.fillAmount = ad;

////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
////    }

////    // ─── Helpers ──────────────────────────────────────────────────────────────

////    private EquipmentSlot? GetSlotForButton(InventorySlotButton target)
////    {
////        foreach (var kvp in _groups)
////            foreach (var btn in kvp.Value)
////                if (btn == target) return kvp.Key;
////        return null;
////    }

////    // ─── Open / Close ─────────────────────────────────────────────────────────

////    public void Open() => gameObject.SetActive(true);
////    public void Close() => gameObject.SetActive(false);
////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);

////    // ─── BUY: copy confirmed selection to a spawned soldier ───────────────────

////    /// <summary>
////    /// Called by GameManager.SpawnBasicSoldier() right after Instantiate().
////    /// Copies all CONFIRMED (IsSelected) items to the spawned village soldier.
////    /// </summary>
////    public void ApplySelectionToSoldier(CharacterEquipment target)
////    {
////        if (target == null)
////        {
////            Debug.LogError("[InventoryPanel] ApplySelectionToSoldier: target is null!");
////            return;
////        }

////        int applied = 0;
////        foreach (var kvp in _groups)
////        {
////            foreach (var btn in kvp.Value)
////            {
////                if (btn.IsSelected && btn.Item != null)
////                {
////                    target.Equip(btn.Item);
////                    applied++;
////                    Debug.Log($"[InventoryPanel] Applied {btn.Item.itemName} to spawned soldier.");
////                }
////            }
////        }

////        if (applied == 0)
////            Debug.LogWarning("[InventoryPanel] No confirmed items — soldier spawned bare.");
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - InventoryPanel
/////
///// ── ROOT CAUSE FIX ────────────────────────────────────────────────────────────
/////   BUG: OnSoldierSpawned() called ApplyDefaultSelections() AFTER switching
/////   soldierEquipment to the new soldier. This wiped every user selection
/////   (golden armor, custom helmet, sword) before they were ever applied.
/////
/////   FIX: OnSoldierSpawned() now:
/////     1. Saves which buttons are selected BEFORE switching soldier
/////     2. Switches soldierEquipment to the new soldier
/////     3. Calls ApplyDefaultSelections() for visual button reset
/////     4. Re-equips the saved items on the NEW soldier  ← THE FIX
/////
///// ── How equipping works ───────────────────────────────────────────────────────
/////   InventorySlotButton.Select()
/////     → CharacterEquipment.Equip(item)           (applies stat bonuses)
/////     → CharacterVisuals.SetSprite(slot, frame0) (swaps the Image sprite)
/////     → SpriteLayerAnimator advances frames       (runs idle/walk animation)
/////   SoldierStats fires OnStatsChanged
/////     → InventoryPanel.RefreshStats() → bars + text update live
/////
///// ── Inspector wiring ─────────────────────────────────────────────────────────
/////   soldierEquipment → leave EMPTY — found automatically at runtime
/////
/////   Item Groups → drag each slot's button-parent GO:
/////     groupPlayer  = BodyType buttons parent
/////     groupHead    = Face buttons parent
/////     groupHair    = Hair buttons parent
/////     groupHelmet  = Helmet buttons parent
/////     groupArmor   = Armor buttons parent
/////     groupWeapon  = Weapon buttons parent
/////
/////   Tab Buttons → drag each tab Button (one per slot)
/////   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
/////
/////   ── Stats Panel ──────────────────────────────────────────────────────────
/////   healthBarFill  → Image (Filled, Horizontal, Left) for health
/////   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
/////   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
/////   healthText     → TextMeshProUGUI (shows MaxHealth number)
/////   abilityText    → TextMeshProUGUI (shows AbilityPower number)
/////   damageText     → TextMeshProUGUI (shows AttackDamage number)
///// </summary>
//public class InventoryPanel : MonoBehaviour
//{
//    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//    [Header("Soldier — leave empty, found automatically")]
//    [SerializeField] private CharacterEquipment soldierEquipment;

//    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//    [Header("Item Groups — one parent GO per slot")]
//    [SerializeField] private GameObject groupPlayer;   // BodyType
//    [SerializeField] private GameObject groupHead;     // Face
//    [SerializeField] private GameObject groupHair;
//    [SerializeField] private GameObject groupHelmet;
//    [SerializeField] private GameObject groupArmor;
//    [SerializeField] private GameObject groupWeapon;

//    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

//    [Header("Helmet hides Hair")]
//    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//    [SerializeField] private GameObject hairVisualRoot;

//    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//    [Header("Tab Buttons")]
//    [SerializeField] private Button tabPlayer;
//    [SerializeField] private Button tabHead;
//    [SerializeField] private Button tabHair;
//    [SerializeField] private Button tabHelmet;
//    [SerializeField] private Button tabArmor;
//    [SerializeField] private Button tabWeapon;

//    [Header("Tab Colours")]
//    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//    // ─── Inspector — Stat Bars ────────────────────────────────────────────────

//    [Header("Stats Bars — wire to see live stat changes on equip")]
//    [SerializeField] private Image healthBarFill;
//    [SerializeField] private Image abilityBarFill;
//    [SerializeField] private Image damageBarFill;

//    [Header("Stats Labels (TextMeshPro — optional)")]
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private TextMeshProUGUI abilityText;
//    [SerializeField] private TextMeshProUGUI damageText;

//    [Header("Bar Max Reference Values")]
//    [SerializeField] private float maxHealthDisplay = 200f;
//    [SerializeField] private float maxAbilityDisplay = 100f;
//    [SerializeField] private float maxDamageDisplay = 100f;

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//    }

//    private void OnDestroy()
//    {
//        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//        UnsubscribeStats();
//    }

//    private void OnEnable()
//    {
//        if (soldierEquipment == null)
//            soldierEquipment = FindObjectOfType<CharacterEquipment>();

//        SubscribeStats();
//        ForceAllGroupsActive(true);
//        BuildGroupCache();
//        InitAllButtons();
//        ApplyDefaultSelections();
//        ShowSlot(_activeSlot);
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    private void OnDisable()
//    {
//        UnsubscribeStats();
//    }

//    // ─── Soldier Spawn Callback  ──────────────────────────────────────────────
//    //
//    // THIS IS THE KEY FIX.
//    //
//    // Old behaviour (broken):
//    //   soldierEquipment = newSoldier
//    //   ApplyDefaultSelections()    ← wiped golden armor, custom helmet, sword
//    //   // selected items NEVER applied to soldier
//    //
//    // New behaviour (fixed):
//    //   savedItems = GetSelectedItems()   ← capture user's choices FIRST
//    //   soldierEquipment = newSoldier
//    //   ApplyDefaultSelections()           ← resets button visuals (OK)
//    //   ApplyItemsToSoldier(savedItems)    ← re-equips choices on new soldier ✓

//    private void OnSoldierSpawned(GameObject soldierGO)
//    {
//        var newEquip = soldierGO.GetComponent<CharacterEquipment>();
//        if (newEquip == null)
//        {
//            Debug.LogWarning("[InventoryPanel] Spawned soldier has no CharacterEquipment — " +
//                             "add CharacterEquipment to the SolderPrefab.");
//            return;
//        }

//        // ── 1. Save current user selections BEFORE switching ──────────────────
//        var savedItems = GetSelectedItems();
//        Debug.Log($"[InventoryPanel] BUY pressed — saving {savedItems.Count} selected items.");

//        // ── 2. Switch to the new soldier ──────────────────────────────────────
//        UnsubscribeStats();
//        soldierEquipment = newEquip;

//        if (!gameObject.activeInHierarchy) return;

//        SubscribeStats();
//        ForceAllGroupsActive(true);
//        InitAllButtons();

//        // ── 3. Reset button visuals to defaults (nothing selected in armor/helmet/weapon) ──
//        ApplyDefaultSelections();

//        // ── 4. Re-equip saved items on the NEW soldier ← THE FIX ─────────────
//        if (savedItems.Count > 0)
//        {
//            ApplyItemsToSoldier(soldierEquipment, savedItems);
//            Debug.Log($"[InventoryPanel] Applied {savedItems.Count} items to spawned soldier.");
//        }
//        else
//        {
//            Debug.Log("[InventoryPanel] No items selected — soldier uses DefaultLoadout.");
//        }

//        ShowSlot(_activeSlot);
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    // ─── Public API ───────────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns one selected EquipmentItem per slot (skips slots with nothing chosen).
//    /// Called by OnSoldierSpawned before the soldier reference switches.
//    /// </summary>
//    public Dictionary<EquipmentSlot, EquipmentItem> GetSelectedItems()
//    {
//        var result = new Dictionary<EquipmentSlot, EquipmentItem>();

//        foreach (var kvp in _groups)
//        {
//            foreach (var btn in kvp.Value)
//            {
//                if (btn.IsSelected && btn.Item != null)
//                {
//                    result[kvp.Key] = btn.Item;
//                    break;   // one per slot
//                }
//            }
//        }

//        return result;
//    }

//    /// <summary>
//    /// Equips a dictionary of items (slot → item) onto a CharacterEquipment target.
//    /// Also updates the matching buttons to IsSelected = true so the panel
//    /// reflects the soldier's loadout correctly.
//    /// </summary>
//    private void ApplyItemsToSoldier(CharacterEquipment target,
//                                     Dictionary<EquipmentSlot, EquipmentItem> items)
//    {
//        if (target == null) return;

//        foreach (var kvp in items)
//        {
//            EquipmentSlot slot = kvp.Key;
//            EquipmentItem item = kvp.Value;

//            // Apply to soldier's CharacterEquipment
//            // This updates visuals via CharacterVisuals.SetSprite() AND stats
//            target.Equip(item);

//            // Sync button highlight so panel matches the soldier
//            if (_groups.TryGetValue(slot, out var list))
//            {
//                foreach (var btn in list)
//                {
//                    if (btn.Item == item)
//                    {
//                        btn.Select();   // highlight this button
//                    }
//                    else if (btn.IsSelected && !btn.IsDefault)
//                    {
//                        // Silently deselect others in this slot without unequipping
//                        // (the item was already replaced above)
//                        btn.ForceDeselect();
//                    }
//                }
//            }

//            Debug.Log($"[InventoryPanel] Equipped '{item.itemName}' ({slot}) on " +
//                      $"'{target.gameObject.name}'.");
//        }
//    }

//    /// <summary>
//    /// Called by InventorySlotButton — selects btn, deselects others in same slot.
//    /// </summary>
//    public void SelectButton(InventorySlotButton btn)
//    {
//        var list = FindGroup(btn);
//        if (list == null) return;

//        foreach (var b in list)
//        {
//            if (b == btn) b.Select();
//            else b.Deselect();
//        }

//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    /// <summary>
//    /// Called by InventorySlotButton — deselects btn only.
//    /// </summary>
//    public void DeselectButton(InventorySlotButton btn)
//    {
//        if (btn.IsDefault) return;
//        btn.Deselect();
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//    private void ApplyHairHelmetRule()
//    {
//        if (hairVisualRoot == null) return;

//        bool helmetOn = false;
//        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//            foreach (var btn in helmetGroup)
//                if (btn.IsSelected) { helmetOn = true; break; }

//        hairVisualRoot.SetActive(!helmetOn);
//    }

//    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//    private void ShowSlot(EquipmentSlot slot)
//    {
//        _activeSlot = slot;
//        UpdateTabColours();
//        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//        SetActive(groupHead, slot == EquipmentSlot.Face);
//        SetActive(groupHair, slot == EquipmentSlot.Hair);
//        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//    }

//    private void ForceAllGroupsActive(bool active)
//    {
//        SetActive(groupPlayer, active);
//        SetActive(groupHead, active);
//        SetActive(groupHair, active);
//        SetActive(groupHelmet, active);
//        SetActive(groupArmor, active);
//        SetActive(groupWeapon, active);
//    }

//    private static void SetActive(GameObject go, bool active)
//    {
//        if (go != null) go.SetActive(active);
//    }

//    // ─── Tab Colours ──────────────────────────────────────────────────────────

//    private void UpdateTabColours()
//    {
//        Tint(tabPlayer, EquipmentSlot.BodyType);
//        Tint(tabHead, EquipmentSlot.Face);
//        Tint(tabHair, EquipmentSlot.Hair);
//        Tint(tabHelmet, EquipmentSlot.Helmet);
//        Tint(tabArmor, EquipmentSlot.Armor);
//        Tint(tabWeapon, EquipmentSlot.Weapon);
//    }

//    private void Tint(Button btn, EquipmentSlot slot)
//    {
//        if (btn == null) return;
//        var img = btn.GetComponent<Image>();
//        if (img != null)
//            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//    }

//    // ─── Stats Refresh ────────────────────────────────────────────────────────

//    private void RefreshStats()
//    {
//        var stats = GetStats();
//        if (stats == null) return;

//        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
//        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
//        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

//        if (healthBarFill != null) healthBarFill.fillAmount = hp;
//        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//        if (damageBarFill != null) damageBarFill.fillAmount = ad;

//        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
//        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
//        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//    }

//    // ─── Stats Subscription ───────────────────────────────────────────────────

//    private void SubscribeStats()
//    {
//        var s = GetStats();
//        if (s != null) s.OnStatsChanged += OnStatsChanged;
//    }

//    private void UnsubscribeStats()
//    {
//        var s = GetStats();
//        if (s != null) s.OnStatsChanged -= OnStatsChanged;
//    }

//    private void OnStatsChanged(SoldierStats _) => RefreshStats();

//    private SoldierStats GetStats() =>
//        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

//    // ─── Group Cache ──────────────────────────────────────────────────────────

//    private void BuildGroupCache()
//    {
//        _groups.Clear();
//        Cache(EquipmentSlot.BodyType, groupPlayer);
//        Cache(EquipmentSlot.Face, groupHead);
//        Cache(EquipmentSlot.Hair, groupHair);
//        Cache(EquipmentSlot.Helmet, groupHelmet);
//        Cache(EquipmentSlot.Armor, groupArmor);
//        Cache(EquipmentSlot.Weapon, groupWeapon);
//    }

//    private void Cache(EquipmentSlot slot, GameObject group)
//    {
//        var list = new List<InventorySlotButton>();
//        if (group != null)
//            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//        _groups[slot] = list;
//    }

//    // ─── Button Init ──────────────────────────────────────────────────────────

//    private void InitAllButtons()
//    {
//        foreach (var kvp in _groups)
//            foreach (var btn in kvp.Value)
//                btn.Init(this, soldierEquipment);
//    }

//    // ─── Default Selections ───────────────────────────────────────────────────

//    private void ApplyDefaultSelections()
//    {
//        foreach (var kvp in _groups)
//        {
//            var slot = kvp.Key;
//            var list = kvp.Value;
//            if (list.Count == 0) continue;

//            if (slot == EquipmentSlot.BodyType)
//            {
//                foreach (var btn in list)
//                {
//                    if (btn.IsDefault) btn.Select();
//                    else btn.Deselect();
//                }
//            }
//            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//            {
//                for (int i = 0; i < list.Count; i++)
//                {
//                    if (i == 0) list[i].Select();
//                    else list[i].Deselect();
//                }
//            }
//            else
//            {
//                // Armor, Helmet, Weapon — nothing selected by default
//                foreach (var btn in list) btn.Deselect();
//            }
//        }
//    }

//    // ─── Helpers ──────────────────────────────────────────────────────────────

//    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//    {
//        foreach (var kvp in _groups)
//            foreach (var btn in kvp.Value)
//                if (btn == target) return kvp.Value;
//        return null;
//    }

//    // ─── Open / Close ─────────────────────────────────────────────────────────

//    public void Open() => gameObject.SetActive(true);
//    public void Close() => gameObject.SetActive(false);
//    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventoryPanel
///
/// ── ROOT CAUSE FIX ────────────────────────────────────────────────────────────
///   BUG: OnSoldierSpawned() called ApplyDefaultSelections() AFTER switching
///   soldierEquipment to the new soldier. This wiped every user selection
///   (golden armor, custom helmet, sword) before they were ever applied.
///
///   FIX: OnSoldierSpawned() now:
///     1. Saves which buttons are selected BEFORE switching soldier
///     2. Switches soldierEquipment to the new soldier
///     3. Calls ApplyDefaultSelections() for visual button reset
///     4. Re-equips the saved items on the NEW soldier  ← THE FIX
///
/// ── How equipping works ───────────────────────────────────────────────────────
///   InventorySlotButton.Select()
///     → CharacterEquipment.Equip(item)           (applies stat bonuses)
///     → CharacterVisuals.SetSprite(slot, frame0) (swaps the Image sprite)
///     → SpriteLayerAnimator advances frames       (runs idle/walk animation)
///   SoldierStats fires OnStatsChanged
///     → InventoryPanel.RefreshStats() → bars + text update live
///
/// ── Inspector wiring ─────────────────────────────────────────────────────────
///   soldierEquipment → leave EMPTY — found automatically at runtime
///
///   Item Groups → drag each slot's button-parent GO:
///     groupPlayer  = BodyType buttons parent
///     groupHead    = Face buttons parent
///     groupHair    = Hair buttons parent
///     groupHelmet  = Helmet buttons parent
///     groupArmor   = Armor buttons parent
///     groupWeapon  = Weapon buttons parent
///
///   Tab Buttons → drag each tab Button (one per slot)
///   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
///
///   ── Stats Panel ──────────────────────────────────────────────────────────
///   healthBarFill  → Image (Filled, Horizontal, Left) for health
///   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
///   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
///   healthText     → TextMeshProUGUI (shows MaxHealth number)
///   abilityText    → TextMeshProUGUI (shows AbilityPower number)
///   damageText     → TextMeshProUGUI (shows AttackDamage number)
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    // ─── Inspector — Soldier ──────────────────────────────────────────────────

    [Header("Soldier — leave empty, found automatically")]
    [SerializeField] private CharacterEquipment soldierEquipment;

    // ─── Inspector — Item Groups ──────────────────────────────────────────────

    [Header("Item Groups — one parent GO per slot")]
    [SerializeField] private GameObject groupPlayer;   // BodyType
    [SerializeField] private GameObject groupHead;     // Face
    [SerializeField] private GameObject groupHair;
    [SerializeField] private GameObject groupHelmet;
    [SerializeField] private GameObject groupArmor;
    [SerializeField] private GameObject groupWeapon;

    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

    [Header("Helmet hides Hair")]
    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
    [SerializeField] private GameObject hairVisualRoot;

    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

    [Header("Tab Buttons")]
    [SerializeField] private Button tabPlayer;
    [SerializeField] private Button tabHead;
    [SerializeField] private Button tabHair;
    [SerializeField] private Button tabHelmet;
    [SerializeField] private Button tabArmor;
    [SerializeField] private Button tabWeapon;

    [Header("Tab Colours")]
    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

    // ─── Inspector — Stat Bars ────────────────────────────────────────────────

    [Header("Stats Bars — wire to see live stat changes on equip")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image abilityBarFill;
    [SerializeField] private Image damageBarFill;

    [Header("Stats Labels (TextMeshPro — optional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Bar Max Reference Values")]
    [SerializeField] private float maxHealthDisplay = 200f;
    [SerializeField] private float maxAbilityDisplay = 100f;
    [SerializeField] private float maxDamageDisplay = 100f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

        GameManager.OnSoldierSpawned += OnSoldierSpawned;
    }

    private void OnDestroy()
    {
        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
        UnsubscribeStats();
    }

    private void OnEnable()
    {
        if (soldierEquipment == null)
            soldierEquipment = FindFirstObjectByType<CharacterEquipment>();

        SubscribeStats();
        ForceAllGroupsActive(true);
        BuildGroupCache();
        InitAllButtons();
        ApplyDefaultSelections();
        ShowSlot(_activeSlot);
        ApplyHairHelmetRule();
        RefreshStats();
    }

    private void OnDisable()
    {
        UnsubscribeStats();
    }

    // ─── Soldier Spawn Callback  ──────────────────────────────────────────────
    //
    // THIS IS THE KEY FIX.
    //
    // Old behaviour (broken):
    //   soldierEquipment = newSoldier
    //   ApplyDefaultSelections()    ← wiped golden armor, custom helmet, sword
    //   // selected items NEVER applied to soldier
    //
    // New behaviour (fixed):
    //   savedItems = GetSelectedItems()   ← capture user's choices FIRST
    //   soldierEquipment = newSoldier
    //   ApplyDefaultSelections()           ← resets button visuals (OK)
    //   ApplyItemsToSoldier(savedItems)    ← re-equips choices on new soldier ✓

    private void OnSoldierSpawned(GameObject soldierGO)
    {
        var newEquip = soldierGO.GetComponent<CharacterEquipment>();
        if (newEquip == null)
        {
            Debug.LogWarning("[InventoryPanel] Spawned soldier has no CharacterEquipment — " +
                             "add CharacterEquipment to the SolderPrefab.");
            return;
        }

        // ── 1. Save current user selections BEFORE switching ──────────────────
        var savedItems = GetSelectedItems();
        Debug.Log($"[InventoryPanel] BUY pressed — saving {savedItems.Count} selected items.");

        // ── 2. Switch to the new soldier ──────────────────────────────────────
        UnsubscribeStats();
        soldierEquipment = newEquip;

        if (!gameObject.activeInHierarchy) return;

        SubscribeStats();
        ForceAllGroupsActive(true);
        InitAllButtons();

        // ── 3. Reset button visuals to defaults (nothing selected in armor/helmet/weapon) ──
        ApplyDefaultSelections();

        // ── 4. Re-equip saved items on the NEW soldier ← THE FIX ─────────────
        if (savedItems.Count > 0)
        {
            ApplyItemsToSoldier(soldierEquipment, savedItems);
            Debug.Log($"[InventoryPanel] Applied {savedItems.Count} items to spawned soldier.");
        }
        else
        {
            Debug.Log("[InventoryPanel] No items selected — soldier uses DefaultLoadout.");
        }

        ShowSlot(_activeSlot);
        ApplyHairHelmetRule();
        RefreshStats();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns one selected EquipmentItem per slot (skips slots with nothing chosen).
    /// Called by OnSoldierSpawned before the soldier reference switches.
    /// </summary>
    public Dictionary<EquipmentSlot, EquipmentItem> GetSelectedItems()
    {
        var result = new Dictionary<EquipmentSlot, EquipmentItem>();

        foreach (var kvp in _groups)
        {
            foreach (var btn in kvp.Value)
            {
                if (btn.IsSelected && btn.Item != null)
                {
                    result[kvp.Key] = btn.Item;
                    break;   // one per slot
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Equips a dictionary of items (slot → item) onto a CharacterEquipment target.
    /// Also updates the matching buttons to IsSelected = true so the panel
    /// reflects the soldier's loadout correctly.
    /// </summary>
    private void ApplyItemsToSoldier(CharacterEquipment target,
                                     Dictionary<EquipmentSlot, EquipmentItem> items)
    {
        if (target == null) return;

        foreach (var kvp in items)
        {
            EquipmentSlot slot = kvp.Key;
            EquipmentItem item = kvp.Value;

            // Apply to soldier's CharacterEquipment
            // This updates visuals via CharacterVisuals.SetSprite() AND stats
            target.Equip(item);

            // Sync button highlight so panel matches the soldier
            if (_groups.TryGetValue(slot, out var list))
            {
                foreach (var btn in list)
                {
                    if (btn.Item == item)
                    {
                        btn.Select();   // highlight this button
                    }
                    else if (btn.IsSelected && !btn.IsDefault)
                    {
                        // Silently deselect others in this slot without unequipping
                        // (the item was already replaced above)
                        btn.ForceDeselect();
                    }
                }
            }

            Debug.Log($"[InventoryPanel] Equipped '{item.itemName}' ({slot}) on " +
                      $"'{target.gameObject.name}'.");
        }
    }

    /// <summary>
    /// Called by InventorySlotButton — selects btn, deselects others in same slot.
    /// </summary>
    public void SelectButton(InventorySlotButton btn)
    {
        var list = FindGroup(btn);
        if (list == null) return;

        foreach (var b in list)
        {
            if (b == btn) b.Select();
            else b.Deselect();
        }

        ApplyHairHelmetRule();
        RefreshStats();
    }

    /// <summary>
    /// Called by InventorySlotButton — deselects btn only.
    /// </summary>
    public void DeselectButton(InventorySlotButton btn)
    {
        if (btn.IsDefault) return;
        btn.Deselect();
        ApplyHairHelmetRule();
        RefreshStats();
    }

    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

    private void ApplyHairHelmetRule()
    {
        if (hairVisualRoot == null) return;

        bool helmetOn = false;
        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
            foreach (var btn in helmetGroup)
                if (btn.IsSelected) { helmetOn = true; break; }

        hairVisualRoot.SetActive(!helmetOn);
    }

    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

    private void ShowSlot(EquipmentSlot slot)
    {
        _activeSlot = slot;
        UpdateTabColours();
        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
        SetActive(groupHead, slot == EquipmentSlot.Face);
        SetActive(groupHair, slot == EquipmentSlot.Hair);
        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
        SetActive(groupArmor, slot == EquipmentSlot.Armor);
        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
    }

    private void ForceAllGroupsActive(bool active)
    {
        SetActive(groupPlayer, active);
        SetActive(groupHead, active);
        SetActive(groupHair, active);
        SetActive(groupHelmet, active);
        SetActive(groupArmor, active);
        SetActive(groupWeapon, active);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ─── Tab Colours ──────────────────────────────────────────────────────────

    private void UpdateTabColours()
    {
        Tint(tabPlayer, EquipmentSlot.BodyType);
        Tint(tabHead, EquipmentSlot.Face);
        Tint(tabHair, EquipmentSlot.Hair);
        Tint(tabHelmet, EquipmentSlot.Helmet);
        Tint(tabArmor, EquipmentSlot.Armor);
        Tint(tabWeapon, EquipmentSlot.Weapon);
    }

    private void Tint(Button btn, EquipmentSlot slot)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
    }

    // ─── Stats Refresh ────────────────────────────────────────────────────────

    private void RefreshStats()
    {
        var stats = GetStats();
        if (stats == null) return;

        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

        if (healthBarFill != null) healthBarFill.fillAmount = hp;
        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
        if (damageBarFill != null) damageBarFill.fillAmount = ad;

        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
    }

    // ─── Stats Subscription ───────────────────────────────────────────────────

    private void SubscribeStats()
    {
        var s = GetStats();
        if (s != null) s.OnStatsChanged += OnStatsChanged;
    }

    private void UnsubscribeStats()
    {
        var s = GetStats();
        if (s != null) s.OnStatsChanged -= OnStatsChanged;
    }

    private void OnStatsChanged(SoldierStats _) => RefreshStats();

    private SoldierStats GetStats() =>
        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

    // ─── Group Cache ──────────────────────────────────────────────────────────

    private void BuildGroupCache()
    {
        _groups.Clear();
        Cache(EquipmentSlot.BodyType, groupPlayer);
        Cache(EquipmentSlot.Face, groupHead);
        Cache(EquipmentSlot.Hair, groupHair);
        Cache(EquipmentSlot.Helmet, groupHelmet);
        Cache(EquipmentSlot.Armor, groupArmor);
        Cache(EquipmentSlot.Weapon, groupWeapon);
    }

    private void Cache(EquipmentSlot slot, GameObject group)
    {
        var list = new List<InventorySlotButton>();
        if (group != null)
            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
        _groups[slot] = list;
    }

    // ─── Button Init ──────────────────────────────────────────────────────────

    private void InitAllButtons()
    {
        foreach (var kvp in _groups)
            foreach (var btn in kvp.Value)
                btn.Init(this, soldierEquipment);
    }

    // ─── Default Selections ───────────────────────────────────────────────────

    private void ApplyDefaultSelections()
    {
        foreach (var kvp in _groups)
        {
            var slot = kvp.Key;
            var list = kvp.Value;
            if (list.Count == 0) continue;

            if (slot == EquipmentSlot.BodyType)
            {
                foreach (var btn in list)
                {
                    if (btn.IsDefault) btn.Select();
                    else btn.Deselect();
                }
            }
            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == 0) list[i].Select();
                    else list[i].Deselect();
                }
            }
            else
            {
                // Armor, Helmet, Weapon — nothing selected by default
                foreach (var btn in list) btn.Deselect();
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
    {
        foreach (var kvp in _groups)
            foreach (var btn in kvp.Value)
                if (btn == target) return kvp.Value;
        return null;
    }

    // ─── Open / Close ─────────────────────────────────────────────────────────

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
}