//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using TMPro;

///////////////////// <summary>
///////////////////// AREA FORGE - InventorySlotButton
/////////////////////
///////////////////// One button in the inventory grid.
///////////////////// Displays the item's icon, name, and a "selected" highlight border.
///////////////////// Attach to the SlotButton prefab inside the inventory scroll grid.
/////////////////////
///////////////////// SlotButton prefab structure:
/////////////////////   SlotButton               ← Button, InventorySlotButton, Image (background)
/////////////////////     ├── Icon               ← Image (item icon sprite)
/////////////////////     ├── NameText           ← TextMeshProUGUI (item name)
/////////////////////     └── SelectedBorder     ← Image (yellow/gold border, hidden by default)
///////////////////// </summary>
//////////////////public class InventorySlotButton : MonoBehaviour
//////////////////{
//////////////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////////////    [SerializeField] private Image iconImage;
//////////////////    [SerializeField] private TextMeshProUGUI nameText;
//////////////////    [SerializeField] private Image selectedBorder;
//////////////////    [SerializeField] private Image background;

//////////////////    // ─── State ───────────────────────────────────────────────────────────────

//////////////////    private EquipmentItem _item;
//////////////////    private CharacterEquipment _equipment;
//////////////////    private InventoryPanel _panel;

//////////////////    // ─── Setup ───────────────────────────────────────────────────────────────

//////////////////    /// <summary>Called by InventoryPanel when populating the grid.</summary>
//////////////////    public void Setup(EquipmentItem item, CharacterEquipment equipment, InventoryPanel panel)
//////////////////    {
//////////////////        _item = item;
//////////////////        _equipment = equipment;
//////////////////        _panel = panel;

//////////////////        // Visual fill
//////////////////        if (iconImage != null) iconImage.sprite = item.inventoryIcon;
//////////////////        if (nameText != null) nameText.text = item.itemName;
//////////////////        if (background != null) background.color = new Color(0.15f, 0.15f, 0.15f);

//////////////////        // Rarity border tint
//////////////////        if (selectedBorder != null)
//////////////////            selectedBorder.color = item.rarityColour;

//////////////////        // Wire click
//////////////////        var btn = GetComponent<Button>();
//////////////////        if (btn != null) btn.onClick.AddListener(OnClick);

//////////////////        // Reflect current selection state
//////////////////        RefreshSelection();

//////////////////        // Subscribe to equipment changes so this button updates live
//////////////////        _equipment.OnEquipmentChanged += OnEquipmentChanged;
//////////////////    }

//////////////////    private void OnDestroy()
//////////////////    {
//////////////////        if (_equipment != null)
//////////////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////////    }

//////////////////    // ─── Click ───────────────────────────────────────────────────────────────

//////////////////    private void OnClick()
//////////////////    {
//////////////////        if (_equipment.IsEquipped(_item))
//////////////////        {
//////////////////            // Clicking the already-equipped item unequips it
//////////////////            _equipment.Unequip(_item.slot);
//////////////////        }
//////////////////        else
//////////////////        {
//////////////////            _equipment.Equip(_item);
//////////////////        }

//////////////////        _panel.RefreshAllButtons();
//////////////////    }

//////////////////    // ─── Selection Visual ────────────────────────────────────────────────────

//////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
//////////////////    {
//////////////////        if (slot == _item.slot)
//////////////////            RefreshSelection();
//////////////////    }

//////////////////    public void RefreshSelection()
//////////////////    {
//////////////////        bool selected = _equipment.IsEquipped(_item);
//////////////////        if (selectedBorder != null) selectedBorder.enabled = selected;
//////////////////        if (background != null)
//////////////////            background.color = selected
//////////////////                ? new Color(0.25f, 0.20f, 0.05f)   // warm gold tint when selected
//////////////////                : new Color(0.12f, 0.12f, 0.12f);
//////////////////    }
//////////////////}

////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using TMPro;

/////////////////// <summary>
/////////////////// AREA FORGE - InventorySlotButton  (Pre-Placed version)
///////////////////
/////////////////// Attach this to each pre-placed item button in your hierarchy.
/////////////////// Drag the correct EquipmentItem ScriptableObject into the "Item" field.
/////////////////// The soldier reference and panel reference are injected at runtime by
/////////////////// InventoryPanel.InitGroup() — you do NOT need to set those in the Inspector.
///////////////////
/////////////////// SlotButton hierarchy example:
///////////////////   Armor1                    ← Button + InventorySlotButton
///////////////////     ├── Selected            ← Image (gold highlight border, hidden by default)
///////////////////     └── Armor               ← Image (item icon / preview sprite)
/////////////////// </summary>
////////////////public class InventorySlotButton : MonoBehaviour
////////////////{
////////////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////////////    [Header("Item — drag the EquipmentItem ScriptableObject here")]
////////////////    [SerializeField] private EquipmentItem item;

////////////////    [Header("Visuals — drag child Images here")]
////////////////    [SerializeField] private Image iconImage;
////////////////    [SerializeField] private Image selectedBorder;   // "Selected" child
////////////////    [SerializeField] private Image background;

////////////////    [SerializeField] private TextMeshProUGUI nameText;  // optional

////////////////    // ─── Runtime (injected by InventoryPanel) ─────────────────────────────────

////////////////    private CharacterEquipment _equipment;
////////////////    private InventoryPanel _panel;
////////////////    private bool _initialised;

////////////////    // ─── Init (called by InventoryPanel.InitGroup) ────────────────────────────

////////////////    /// <summary>
////////////////    /// Called every time the panel opens (or a new soldier spawns).
////////////////    /// Safe to call multiple times — unsubscribes from the old soldier first.
////////////////    /// </summary>
////////////////    public void Init(CharacterEquipment equipment, InventoryPanel panel)
////////////////    {
////////////////        // Unsubscribe from old soldier
////////////////        if (_equipment != null)
////////////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;

////////////////        _equipment = equipment;
////////////////        _panel = panel;
////////////////        _initialised = true;

////////////////        // Subscribe to new soldier
////////////////        if (_equipment != null)
////////////////            _equipment.OnEquipmentChanged += OnEquipmentChanged;

////////////////        // Wire the click only once
////////////////        if (!_initialised)
////////////////        {
////////////////            var btn = GetComponent<Button>();
////////////////            if (btn != null) btn.onClick.AddListener(OnClick);
////////////////        }

////////////////        // Apply icon sprite if assigned
////////////////        if (iconImage != null && item != null)
////////////////        {
////////////////            iconImage.sprite = item.inventoryIcon != null ? item.inventoryIcon
////////////////                                                           : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
////////////////            iconImage.enabled = iconImage.sprite != null;
////////////////        }

////////////////        // Rarity tint on the selected border
////////////////        if (selectedBorder != null && item != null)
////////////////            selectedBorder.color = item.rarityColour;

////////////////        if (nameText != null && item != null)
////////////////            nameText.text = item.itemName;

////////////////        RefreshSelection();
////////////////    }

////////////////    private void Awake()
////////////////    {
////////////////        // Wire click once on Awake so it works even if Init hasn't been called yet
////////////////        var btn = GetComponent<Button>();
////////////////        if (btn != null) btn.onClick.AddListener(OnClick);
////////////////    }

////////////////    private void OnDestroy()
////////////////    {
////////////////        if (_equipment != null)
////////////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////////    }

////////////////    // ─── Click ───────────────────────────────────────────────────────────────

////////////////    private void OnClick()
////////////////    {
////////////////        if (_equipment == null || item == null) return;

////////////////        if (_equipment.IsEquipped(item))
////////////////            _equipment.Unequip(item.slot);
////////////////        else
////////////////            _equipment.Equip(item);

////////////////        _panel?.RefreshAllButtons();
////////////////    }

////////////////    // ─── Selection Visual ────────────────────────────────────────────────────

////////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changedItem)
////////////////    {
////////////////        if (item != null && slot == item.slot)
////////////////            RefreshSelection();
////////////////    }

////////////////    /// <summary>Updates the selected border and background to reflect equip state.</summary>
////////////////    public void RefreshSelection()
////////////////    {
////////////////        if (_equipment == null || item == null) return;

////////////////        bool selected = _equipment.IsEquipped(item);

////////////////        if (selectedBorder != null) selectedBorder.enabled = selected;

////////////////        if (background != null)
////////////////            background.color = selected
////////////////                ? new Color(0.25f, 0.20f, 0.05f)   // warm gold when selected
////////////////                : new Color(0.12f, 0.12f, 0.12f);
////////////////    }
////////////////}


//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using TMPro;

///////////////// <summary>
///////////////// AREA FORGE - InventorySlotButton
/////////////////
///////////////// Attach to each item button in the inventory grid.
/////////////////
///////////////// ── Inspector wiring (per button) ───────────────────────────────────────────
/////////////////   playerVisualObject → drag the matching child GO from the Player hierarchy
/////////////////                        e.g. Silver Armor button  → drag Player/Armor/Armor1
/////////////////                             Gold   Armor button  → drag Player/Armor/Armor2
/////////////////                             Short  Hair  button  → drag Player/Hair/Hair1
/////////////////                             etc.
/////////////////   iconImage          → drag the Icon Image child of this button
/////////////////   selectedBorder     → drag the Selected highlight Image child
/////////////////
///////////////// ── Behaviour ────────────────────────────────────────────────────────────────
/////////////////   • Tap item      → activates Armor2 (for example), deactivates Armor1/3/4
/////////////////   • Tap again     → deselects, deactivates Armor2 (nothing worn)
/////////////////   • Default       → InventoryPanel auto-selects the first button per group
///////////////// </summary>
//////////////public class InventorySlotButton : MonoBehaviour
//////////////{
//////////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////////    [Header("Player Visual")]
//////////////    [Tooltip("Drag the child GO from Player hierarchy that this item represents.\n" +
//////////////             "e.g. Silver Armor → Player/Armor/Armor1\n" +
//////////////             "     Gold Armor   → Player/Armor/Armor2")]
//////////////    [SerializeField] private GameObject playerVisualObject;

//////////////    [Header("Inventory UI")]
//////////////    [SerializeField] private Image iconImage;
//////////////    [SerializeField] private Image selectedBorder;
//////////////    [SerializeField] private Image background;
//////////////    [SerializeField] private TextMeshProUGUI nameText;

//////////////    [Header("Stats (optional)")]
//////////////    [Tooltip("Drag an EquipmentItem ScriptableObject here only if you want stat bonuses.\n" +
//////////////             "Leave empty for purely visual items.")]
//////////////    [SerializeField] private EquipmentItem item;

//////////////    // ─── Runtime ──────────────────────────────────────────────────────────────

//////////////    private InventoryPanel _panel;
//////////////    private CharacterEquipment _equipment;

//////////////    public bool IsSelected { get; private set; }
//////////////    public GameObject PlayerVisualObject => playerVisualObject;
//////////////    public EquipmentItem Item => item;

//////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        var btn = GetComponent<Button>();
//////////////        if (btn != null) btn.onClick.AddListener(OnClick);
//////////////    }

//////////////    private void OnDestroy()
//////////////    {
//////////////        if (_equipment != null)
//////////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////    }

//////////////    // ─── Init (called by InventoryPanel on open) ──────────────────────────────

//////////////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
//////////////    {
//////////////        _panel = panel;

//////////////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////////        _equipment = equipment;
//////////////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

//////////////        // Populate icon
//////////////        if (iconImage != null && item != null)
//////////////        {
//////////////            var spr = item.inventoryIcon != null ? item.inventoryIcon
//////////////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
//////////////            iconImage.sprite = spr;
//////////////            iconImage.enabled = spr != null;
//////////////        }

//////////////        if (nameText != null && item != null) nameText.text = item.itemName;
//////////////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

//////////////        RefreshVisual();
//////////////    }

//////////////    // ─── Click ───────────────────────────────────────────────────────────────

//////////////    private void OnClick()
//////////////    {
//////////////        if (_panel == null) return;

//////////////        if (IsSelected)
//////////////            _panel.DeselectButton(this);
//////////////        else
//////////////            _panel.SelectButton(this);
//////////////    }

//////////////    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

//////////////    /// <summary>Activates the player GO and marks this button selected.</summary>
//////////////    public void Select()
//////////////    {
//////////////        IsSelected = true;

//////////////        if (playerVisualObject != null)
//////////////            playerVisualObject.SetActive(true);

//////////////        if (_equipment != null && item != null)
//////////////            _equipment.Equip(item);

//////////////        RefreshVisual();
//////////////    }

//////////////    /// <summary>Deactivates the player GO and marks this button deselected.</summary>
//////////////    public void Deselect()
//////////////    {
//////////////        IsSelected = false;

//////////////        if (playerVisualObject != null)
//////////////            playerVisualObject.SetActive(false);

//////////////        if (_equipment != null && item != null)
//////////////            _equipment.Unequip(item.slot);

//////////////        RefreshVisual();
//////////////    }

//////////////    // ─── Visual ───────────────────────────────────────────────────────────────

//////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
//////////////    {
//////////////        if (item != null && slot == item.slot)
//////////////        {
//////////////            IsSelected = changed == item;
//////////////            RefreshVisual();
//////////////        }
//////////////    }

//////////////    public void RefreshVisual()
//////////////    {
//////////////        if (selectedBorder != null)
//////////////            selectedBorder.enabled = IsSelected;

//////////////        //if (background != null)
//////////////        //    background.color = IsSelected
//////////////        //        ? new Color(0.25f, 0.20f, 0.05f)
//////////////        //        : new Color(0.12f, 0.12f, 0.12f);
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using TMPro;

/////////////// <summary>
/////////////// AREA FORGE - InventorySlotButton
///////////////
/////////////// Attach to each item button in the inventory grid.
///////////////
/////////////// ── Inspector wiring (per button) ───────────────────────────────────────────
///////////////   playerVisualObject → drag the matching child GO from the Player hierarchy
///////////////                        e.g. Silver Armor button  → Player/Armor/Armor1
///////////////                             Gold   Armor button  → Player/Armor/Armor2
///////////////   selectedBorder     → drag the "Selected" highlight Image child (shown when active)
///////////////   iconImage          → drag the Icon Image child (optional)
///////////////
/////////////// NOTE: The click listener is wired in Init(), NOT in Awake(), because groups
///////////////       are inactive at start — Awake() never runs on inactive GameObjects.
/////////////// </summary>
////////////public class InventorySlotButton : MonoBehaviour
////////////{
////////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////////    [Header("Player Visual — drag the Player child GO this button represents")]
////////////    [SerializeField] private GameObject playerVisualObject;

////////////    [Header("Inventory UI Visuals")]
////////////    [SerializeField] private Image iconImage;
////////////    [SerializeField] private Image selectedBorder;   // shown when selected
////////////    [SerializeField] private TextMeshProUGUI nameText;        // optional

////////////    [Header("Stats (optional — leave empty for purely visual items)")]
////////////    [SerializeField] private EquipmentItem item;

////////////    // ─── Runtime ──────────────────────────────────────────────────────────────

////////////    private InventoryPanel _panel;
////////////    private CharacterEquipment _equipment;
////////////    private bool _clickWired;

////////////    public bool IsSelected { get; private set; }
////////////    public GameObject PlayerVisualObject => playerVisualObject;
////////////    public EquipmentItem Item => item;

////////////    private void OnDestroy()
////////////    {
////////////        if (_equipment != null)
////////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////    }

////////////    // ─── Init (called by InventoryPanel — groups may be inactive, so NOT Awake) ──

////////////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
////////////    {
////////////        _panel = panel;

////////////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////////////        _equipment = equipment;
////////////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

////////////        // Wire click here (once) because Awake may never run if GO starts inactive
////////////        if (!_clickWired)
////////////        {
////////////            var btn = GetComponent<Button>();
////////////            if (btn != null)
////////////            {
////////////                btn.onClick.RemoveListener(OnClick);   // safety — avoid double-add
////////////                btn.onClick.AddListener(OnClick);
////////////                _clickWired = true;
////////////            }
////////////        }

////////////        // Populate icon
////////////        if (iconImage != null && item != null)
////////////        {
////////////            var spr = item.inventoryIcon != null ? item.inventoryIcon
////////////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
////////////            iconImage.sprite = spr;
////////////            iconImage.enabled = spr != null;
////////////        }

////////////        if (nameText != null && item != null) nameText.text = item.itemName;
////////////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

////////////        RefreshVisual();
////////////    }

////////////    // ─── Click ───────────────────────────────────────────────────────────────

////////////    private void OnClick()
////////////    {
////////////        if (_panel == null)
////////////        {
////////////            Debug.LogWarning($"[InventorySlotButton] {name}: _panel is null — was Init() called?");
////////////            return;
////////////        }

////////////        if (IsSelected)
////////////            _panel.DeselectButton(this);
////////////        else
////////////            _panel.SelectButton(this);
////////////    }

////////////    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

////////////    public void Select()
////////////    {
////////////        IsSelected = true;
////////////        if (playerVisualObject != null) playerVisualObject.SetActive(true);
////////////        if (_equipment != null && item != null) _equipment.Equip(item);
////////////        RefreshVisual();
////////////    }

////////////    public void Deselect()
////////////    {
////////////        IsSelected = false;
////////////        if (playerVisualObject != null) playerVisualObject.SetActive(false);
////////////        if (_equipment != null && item != null) _equipment.Unequip(item.slot);
////////////        RefreshVisual();
////////////    }

////////////    // ─── Visual ───────────────────────────────────────────────────────────────

////////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
////////////    {
////////////        if (item != null && slot == item.slot)
////////////        {
////////////            IsSelected = changed == item;
////////////            RefreshVisual();
////////////        }
////////////    }

////////////    public void RefreshVisual()
////////////    {
////////////        // Only the selected border toggles — background color is NEVER changed
////////////        if (selectedBorder != null)
////////////            selectedBorder.enabled = IsSelected;
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using TMPro;

///////////// <summary>
///////////// AREA FORGE - InventorySlotButton
/////////////
///////////// ── Inspector wiring (per button) ───────────────────────────────────────────
/////////////   playerVisualObject → drag the matching child GO from the Player hierarchy
/////////////                        e.g. Silver Armor button → Player/Armor/Armor1
/////////////                             Gold   Armor button → Player/Armor/Armor2
/////////////   selectedBorder     → drag the "Selected" highlight Image child
/////////////   iconImage          → drag the icon Image child
/////////////   isDefault          → tick ON for Body and Face buttons (cannot be deselected)
///////////// </summary>
//////////public class InventorySlotButton : MonoBehaviour
//////////{
//////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////    [Header("Player Visual — drag the Player child GO this button represents")]
//////////    [SerializeField] private GameObject playerVisualObject;

//////////    [Header("Inventory UI Visuals")]
//////////    [SerializeField] private Image iconImage;
//////////    [SerializeField] private Image selectedBorder;
//////////    [SerializeField] private TextMeshProUGUI nameText;

//////////    [Header("Settings")]
//////////    [Tooltip("Tick ON for Body and Face — these are always selected and cannot be deselected.")]
//////////    [SerializeField] private bool isDefault = false;

//////////    [Header("Stats (optional)")]
//////////    [SerializeField] private EquipmentItem item;

//////////    // ─── Runtime ──────────────────────────────────────────────────────────────

//////////    private InventoryPanel _panel;
//////////    private CharacterEquipment _equipment;
//////////    private bool _clickWired;

//////////    public bool IsSelected { get; private set; }
//////////    public bool IsDefault => isDefault;
//////////    public GameObject PlayerVisualObject => playerVisualObject;
//////////    public EquipmentItem Item => item;

//////////    private void OnDestroy()
//////////    {
//////////        if (_equipment != null)
//////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////    }

//////////    // ─── Init ────────────────────────────────────────────────────────────────

//////////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
//////////    {
//////////        _panel = panel;

//////////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////////        _equipment = equipment;
//////////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

//////////        // Wire click here — Awake never runs if GO starts inside an inactive parent
//////////        if (!_clickWired)
//////////        {
//////////            var btn = GetComponent<Button>();
//////////            if (btn != null)
//////////            {
//////////                btn.onClick.RemoveListener(OnClick);
//////////                btn.onClick.AddListener(OnClick);
//////////                _clickWired = true;
//////////            }
//////////        }

//////////        // Populate icon
//////////        if (iconImage != null && item != null)
//////////        {
//////////            var spr = item.inventoryIcon != null ? item.inventoryIcon
//////////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
//////////            iconImage.sprite = spr;
//////////            iconImage.enabled = spr != null;
//////////        }

//////////        if (nameText != null && item != null) nameText.text = item.itemName;
//////////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

//////////        RefreshVisual();
//////////    }

//////////    // ─── Click ───────────────────────────────────────────────────────────────

//////////    private void OnClick()
//////////    {
//////////        if (_panel == null) return;

//////////        if (IsSelected)
//////////        {
//////////            // Default items (Body, Face) cannot be deselected — clicking does nothing
//////////            if (isDefault) return;

//////////            _panel.DeselectButton(this);
//////////        }
//////////        else
//////////        {
//////////            _panel.SelectButton(this);
//////////        }
//////////    }

//////////    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

//////////    public void Select()
//////////    {
//////////        IsSelected = true;
//////////        if (playerVisualObject != null) playerVisualObject.SetActive(true);
//////////        if (_equipment != null && item != null) _equipment.Equip(item);
//////////        RefreshVisual();
//////////    }

//////////    public void Deselect()
//////////    {
//////////        // Safety: never deselect a default item
//////////        if (isDefault) return;

//////////        IsSelected = false;
//////////        if (playerVisualObject != null) playerVisualObject.SetActive(false);
//////////        if (_equipment != null && item != null) _equipment.Unequip(item.slot);
//////////        RefreshVisual();
//////////    }

//////////    // ─── Visual ───────────────────────────────────────────────────────────────

//////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
//////////    {
//////////        if (item != null && slot == item.slot)
//////////        {
//////////            IsSelected = changed == item;
//////////            RefreshVisual();
//////////        }
//////////    }

//////////    public void RefreshVisual()
//////////    {
//////////        if (selectedBorder != null)
//////////            selectedBorder.enabled = IsSelected;
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;
////////using TMPro;

/////////// <summary>
/////////// AREA FORGE - InventorySlotButton
///////////
/////////// Clicking always SELECTS this item.
/////////// Clicking an already-selected item does nothing.
/////////// There is no deselect — something is always selected per slot.
/////////// </summary>
////////public class InventorySlotButton : MonoBehaviour
////////{
////////    [Header("Player Visual — drag the Player child GO this button represents")]
////////    [SerializeField] private GameObject playerVisualObject;

////////    [Header("Inventory UI Visuals")]
////////    [SerializeField] private Image            iconImage;
////////    [SerializeField] private Image            selectedBorder;
////////    [SerializeField] private TextMeshProUGUI  nameText;

////////    [Header("Stats (optional)")]
////////    [SerializeField] private EquipmentItem item;

////////    // ─── Runtime ──────────────────────────────────────────────────────────────

////////    private InventoryPanel     _panel;
////////    private CharacterEquipment _equipment;
////////    private bool               _clickWired;

////////    public bool          IsSelected         { get; private set; }
////////    public GameObject    PlayerVisualObject => playerVisualObject;
////////    public EquipmentItem Item               => item;

////////    private void OnDestroy()
////////    {
////////        if (_equipment != null)
////////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////////    }

////////    // ─── Init ────────────────────────────────────────────────────────────────

////////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
////////    {
////////        _panel = panel;

////////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////////        _equipment = equipment;
////////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

////////        if (!_clickWired)
////////        {
////////            var btn = GetComponent<Button>();
////////            if (btn != null)
////////            {
////////                btn.onClick.RemoveListener(OnClick);
////////                btn.onClick.AddListener(OnClick);
////////                _clickWired = true;
////////            }
////////        }

////////        if (iconImage != null && item != null)
////////        {
////////            var spr = item.inventoryIcon != null ? item.inventoryIcon
////////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
////////            iconImage.sprite  = spr;
////////            iconImage.enabled = spr != null;
////////        }

////////        if (nameText       != null && item != null) nameText.text        = item.itemName;
////////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

////////        RefreshVisual();
////////    }

////////    // ─── Click — always selects, never deselects ──────────────────────────────

////////    private void OnClick()
////////    {
////////        if (_panel == null) return;
////////        // Already selected → do nothing
////////        if (IsSelected) return;
////////        // Otherwise tell the panel to select this button
////////        _panel.SelectButton(this);
////////    }

////////    // ─── Select / Deselect (driven by InventoryPanel only) ────────────────────

////////    public void Select()
////////    {
////////        IsSelected = true;
////////        if (playerVisualObject != null) playerVisualObject.SetActive(true);
////////        if (_equipment != null && item != null) _equipment.Equip(item);
////////        RefreshVisual();
////////    }

////////    public void Deselect()
////////    {
////////        IsSelected = false;
////////        if (playerVisualObject != null) playerVisualObject.SetActive(false);
////////        if (_equipment != null && item != null) _equipment.Unequip(item.slot);
////////        RefreshVisual();
////////    }

////////    // ─── Visual ───────────────────────────────────────────────────────────────

////////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
////////    {
////////        if (item != null && slot == item.slot)
////////        {
////////            IsSelected = changed == item;
////////            RefreshVisual();
////////        }
////////    }

////////    public void RefreshVisual()
////////    {
////////        if (selectedBorder != null)
////////            selectedBorder.enabled = IsSelected;
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// AREA FORGE - InventorySlotButton
/////////
///////// ── Behaviour ────────────────────────────────────────────────────────────────
/////////   • Armor / Helmet / Weapon / Hair → click to select, click again to deselect
/////////   • Skinny Body                    → tick "Is Default" → always stays selected,
/////////                                      clicking it does nothing
///////// </summary>
//////public class InventorySlotButton : MonoBehaviour
//////{
//////    [Header("Player Visual — drag the Player child GO this button represents")]
//////    [SerializeField] private GameObject playerVisualObject;

//////    [Header("Inventory UI Visuals")]
//////    [SerializeField] private Image iconImage;
//////    [SerializeField] private Image selectedBorder;
//////    [SerializeField] private TextMeshProUGUI nameText;

//////    [Header("Settings")]
//////    [Tooltip("Tick ON for the Skinny Body button ONLY.\n" +
//////             "It will always be selected and cannot be deselected.")]
//////    [SerializeField] private bool isDefault = false;

//////    [Header("Stats (optional)")]
//////    [SerializeField] private EquipmentItem item;

//////    // ─── Runtime ──────────────────────────────────────────────────────────────

//////    private InventoryPanel _panel;
//////    private CharacterEquipment _equipment;
//////    private bool _clickWired;

//////    public bool IsSelected { get; private set; }
//////    public bool IsDefault => isDefault;
//////    public GameObject PlayerVisualObject => playerVisualObject;
//////    public EquipmentItem Item => item;

//////    private void OnDestroy()
//////    {
//////        if (_equipment != null)
//////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////    }

//////    // ─── Init ────────────────────────────────────────────────────────────────

//////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
//////    {
//////        _panel = panel;

//////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////        _equipment = equipment;
//////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

//////        if (!_clickWired)
//////        {
//////            var btn = GetComponent<Button>();
//////            if (btn != null)
//////            {
//////                btn.onClick.RemoveListener(OnClick);
//////                btn.onClick.AddListener(OnClick);
//////                _clickWired = true;
//////            }
//////        }

//////        if (iconImage != null && item != null)
//////        {
//////            var spr = item.inventoryIcon != null ? item.inventoryIcon
//////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
//////            iconImage.sprite = spr;
//////            iconImage.enabled = spr != null;
//////        }

//////        if (nameText != null && item != null) nameText.text = item.itemName;
//////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

//////        RefreshVisual();
//////    }

//////    // ─── Click ───────────────────────────────────────────────────────────────

//////    private void OnClick()
//////    {
//////        if (_panel == null) return;

//////        // Default item (Skinny Body) — always stays selected, ignore clicks
//////        if (isDefault) return;

//////        if (IsSelected)
//////            _panel.DeselectButton(this);   // click again → deselect
//////        else
//////            _panel.SelectButton(this);     // click → select
//////    }

//////    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

//////    public void Select()
//////    {
//////        IsSelected = true;
//////        if (playerVisualObject != null) playerVisualObject.SetActive(true);
//////        if (_equipment != null && item != null) _equipment.Equip(item);
//////        RefreshVisual();
//////    }

//////    public void Deselect()
//////    {
//////        if (isDefault) return;   // safety — default items cannot be deselected
//////        IsSelected = false;
//////        if (playerVisualObject != null) playerVisualObject.SetActive(false);
//////        if (_equipment != null && item != null) _equipment.Unequip(item.slot);
//////        RefreshVisual();
//////    }

//////    // ─── Visual ───────────────────────────────────────────────────────────────

//////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
//////    {
//////        if (item != null && slot == item.slot)
//////        {
//////            IsSelected = changed == item;
//////            RefreshVisual();
//////        }
//////    }

//////    public void RefreshVisual()
//////    {
//////        if (selectedBorder != null)
//////            selectedBorder.enabled = IsSelected;
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// AREA FORGE - InventorySlotButton
///////
/////// ── Bug fix ──────────────────────────────────────────────────────────────────
///////   Previously Deselect() always called _equipment.Unequip(), even after
///////   Select() on the new button had already replaced the item in that slot.
///////   This caused the new item's bonus to get removed immediately after being added.
///////
///////   Fix: Deselect() checks IsEquipped(item) first — if something else has
///////   already taken the slot, it skips the Unequip call.
/////// </summary>
////public class InventorySlotButton : MonoBehaviour
////{
////    [Header("Player Visual — drag the Player child GO this button represents")]
////    [SerializeField] private GameObject playerVisualObject;

////    [Header("Inventory UI Visuals")]
////    [SerializeField] private Image iconImage;
////    [SerializeField] private Image selectedBorder;
////    [SerializeField] private TextMeshProUGUI nameText;

////    [Header("Settings")]
////    [Tooltip("Tick ON for the Skinny Body button ONLY — always selected, cannot be deselected.")]
////    [SerializeField] private bool isDefault = false;

////    [Header("Stats (optional — fill for stat bonuses on equip)")]
////    [SerializeField] private EquipmentItem item;

////    // ─── Runtime ──────────────────────────────────────────────────────────────

////    private InventoryPanel _panel;
////    private CharacterEquipment _equipment;
////    private bool _clickWired;

////    public bool IsSelected { get; private set; }
////    public bool IsDefault => isDefault;
////    public GameObject PlayerVisualObject => playerVisualObject;
////    public EquipmentItem Item => item;

////    private void OnDestroy()
////    {
////        if (_equipment != null)
////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////    }

////    // ─── Init ────────────────────────────────────────────────────────────────

////    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
////    {
////        _panel = panel;

////        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////        _equipment = equipment;
////        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

////        if (!_clickWired)
////        {
////            var btn = GetComponent<Button>();
////            if (btn != null)
////            {
////                btn.onClick.RemoveListener(OnClick);
////                btn.onClick.AddListener(OnClick);
////                _clickWired = true;
////            }
////        }

////        if (iconImage != null && item != null)
////        {
////            var spr = item.inventoryIcon != null ? item.inventoryIcon
////                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
////            iconImage.sprite = spr;
////            iconImage.enabled = spr != null;
////        }

////        if (nameText != null && item != null) nameText.text = item.itemName;
////        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

////        RefreshVisual();
////    }

////    // ─── Click ───────────────────────────────────────────────────────────────

////    private void OnClick()
////    {
////        if (_panel == null) return;
////        if (isDefault) return;                      // Skinny Body — always locked

////        if (IsSelected) _panel.DeselectButton(this);
////        else _panel.SelectButton(this);
////    }

////    // ─── Select / Deselect ────────────────────────────────────────────────────

////    public void Select()
////    {
////        IsSelected = true;
////        if (playerVisualObject != null) playerVisualObject.SetActive(true);

////        // Equip → CharacterEquipment removes the old slot item first (reverses its
////        // bonus), then adds this item's bonus. Safe to always call.
////        if (_equipment != null && item != null)
////            _equipment.Equip(item);

////        RefreshVisual();
////    }

////    public void Deselect()
////    {
////        if (isDefault) return;

////        IsSelected = false;
////        if (playerVisualObject != null) playerVisualObject.SetActive(false);

////        // ── KEY FIX ────────────────────────────────────────────────────────────
////        // Only unequip if THIS item is still the one occupying the slot.
////        // If Select() on another button already replaced it, IsEquipped returns
////        // false and we skip — prevents removing the new item's bonus by mistake.
////        if (_equipment != null && item != null && _equipment.IsEquipped(item))
////            _equipment.Unequip(item.slot);

////        RefreshVisual();
////    }

////    // ─── Visual ───────────────────────────────────────────────────────────────

////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
////    {
////        if (item != null && slot == item.slot)
////        {
////            IsSelected = changed == item;
////            RefreshVisual();
////        }
////    }

////    public void RefreshVisual()
////    {
////        if (selectedBorder != null)
////            selectedBorder.enabled = IsSelected;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - InventorySlotButton
/////
///// ── Two-step selection flow ───────────────────────────────────────────────────
/////   1. Click item   → SetPending()  — shows a WHITE border highlight, does NOT equip
/////   2. SELECT button → ConfirmSelect() — actually equips the item on the preview
/////                      and updates the animation
/////
///// ── Visual states ────────────────────────────────────────────────────────────
/////   No state   → selectedBorder hidden
/////   Pending    → selectedBorder shown, WHITE colour
/////   Confirmed  → selectedBorder shown, item's rarity colour (gold / blue / white)
/////
///// ── Inspector wiring (per button) ───────────────────────────────────────────
/////   playerVisualObject → drag the matching child GO on the Player preview
/////                        e.g. Armor button #1 → Player/Armor/Armor1
/////   iconImage          → drag the Icon child Image
/////   selectedBorder     → drag the highlight border Image (reused for both states)
/////   nameText           → drag the name TextMeshPro label (optional)
/////   isDefault          → tick ON for the Skinny Body button ONLY (always selected)
/////   item               → drag the EquipmentItem ScriptableObject
///// </summary>
//public class InventorySlotButton : MonoBehaviour
//{
//    // ─── Inspector ────────────────────────────────────────────────────────────

//    [Header("Player Visual — drag the Player child GO this button represents")]
//    [SerializeField] private GameObject playerVisualObject;

//    [Header("Inventory UI Visuals")]
//    [SerializeField] private Image iconImage;
//    [SerializeField] private Image selectedBorder;   // reused: white = pending, rarity = confirmed
//    [SerializeField] private TextMeshProUGUI nameText;

//    [Header("Settings")]
//    [Tooltip("Tick ON for the Skinny Body button ONLY — always selected, cannot be deselected.")]
//    [SerializeField] private bool isDefault = false;

//    [Header("Stats (optional — set bonuses on the EquipmentItem asset)")]
//    [SerializeField] private EquipmentItem item;

//    // ─── Runtime ──────────────────────────────────────────────────────────────

//    private InventoryPanel _panel;
//    private CharacterEquipment _equipment;
//    private bool _clickWired;

//    /// <summary>True after SELECT is pressed — item is equipped on the preview.</summary>
//    public bool IsSelected { get; private set; }

//    /// <summary>True after the button is clicked but before SELECT is pressed.</summary>
//    public bool IsPending { get; private set; }

//    public bool IsDefault => isDefault;
//    public EquipmentItem Item => item;

//    // Colour constants
//    private static readonly Color ColourPending = new Color(1f, 1f, 1f, 0.85f); // white
//    private static readonly Color ColourConfirmed = new Color(0.9f, 0.7f, 0.1f, 1f);   // gold fallback

//    private void OnDestroy()
//    {
//        if (_equipment != null)
//            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//    }

//    // ─── Init (called by InventoryPanel each time the panel opens) ────────────

//    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
//    {
//        _panel = panel;

//        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//        _equipment = equipment;
//        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

//        if (!_clickWired)
//        {
//            var btn = GetComponent<Button>();
//            if (btn != null)
//            {
//                btn.onClick.RemoveListener(OnClick);
//                btn.onClick.AddListener(OnClick);
//                _clickWired = true;
//            }
//        }

//        if (iconImage != null && item != null)
//        {
//            var spr = item.inventoryIcon != null
//                      ? item.inventoryIcon
//                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
//            iconImage.sprite = spr;
//            iconImage.enabled = spr != null;
//        }

//        if (nameText != null && item != null)
//            nameText.text = item.itemName;

//        RefreshVisual();
//    }

//    // ─── Click — marks as PENDING only, does NOT equip ───────────────────────

//    private void OnClick()
//    {
//        if (_panel == null) return;
//        if (isDefault) return;   // Skinny Body — always locked, nothing to do

//        // Tell the panel: "this item is now pending for its slot"
//        _panel.SetPendingButton(this);
//    }

//    // ─── PENDING (set by InventoryPanel on click) ─────────────────────────────

//    /// <summary>
//    /// Marks this button as pending (clicked but not yet confirmed).
//    /// Shows a WHITE border. The preview is NOT updated yet.
//    /// </summary>
//    public void SetPending()
//    {
//        IsPending = true;
//        RefreshVisual();
//    }

//    /// <summary>Clears the pending state without equipping.</summary>
//    public void ClearPending()
//    {
//        IsPending = false;
//        RefreshVisual();
//    }

//    // ─── CONFIRM (called when SELECT button is pressed) ───────────────────────

//    /// <summary>
//    /// Equips the item on the preview character.
//    /// Called by InventoryPanel.ConfirmPendingForActiveSlot() when the SELECT button is pressed.
//    /// </summary>
//    public void ConfirmSelect()
//    {
//        IsPending = false;
//        IsSelected = true;

//        if (playerVisualObject != null)
//            playerVisualObject.SetActive(true);

//        if (_equipment != null && item != null)
//            _equipment.Equip(item);   // → CharacterVisuals + SpriteLayerAnimator update

//        RefreshVisual();
//    }

//    // ─── SELECT / DESELECT (used by panel for defaults + auto-selection) ─────

//    /// <summary>
//    /// Immediately selects and equips — used for default/auto-selected items
//    /// that don't need the two-step confirm flow.
//    /// </summary>
//    public void Select()
//    {
//        IsPending = false;
//        IsSelected = true;

//        if (playerVisualObject != null)
//            playerVisualObject.SetActive(true);

//        if (_equipment != null && item != null)
//            _equipment.Equip(item);

//        RefreshVisual();
//    }

//    /// <summary>Unequips and deselects this button.</summary>
//    public void Deselect()
//    {
//        if (isDefault) return;

//        IsPending = false;
//        IsSelected = false;

//        if (playerVisualObject != null)
//            playerVisualObject.SetActive(false);

//        // Only unequip if this item is still the one in the slot
//        if (_equipment != null && item != null && _equipment.IsEquipped(item))
//            _equipment.Unequip(item.slot);

//        RefreshVisual();
//    }

//    // ─── Visual ───────────────────────────────────────────────────────────────

//    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
//    {
//        if (item == null || slot != item.slot) return;
//        IsSelected = (changed == item);
//        if (IsSelected) IsPending = false;
//        RefreshVisual();
//    }

//    public void RefreshVisual()
//    {
//        if (selectedBorder == null) return;

//        if (IsSelected)
//        {
//            selectedBorder.enabled = true;
//            // Use the item's rarity colour for confirmed state, fallback to gold
//            selectedBorder.color = (item != null) ? item.rarityColour : ColourConfirmed;
//        }
//        else if (IsPending)
//        {
//            selectedBorder.enabled = true;
//            selectedBorder.color = ColourPending;   // white = "pending" feedback
//        }
//        else
//        {
//            selectedBorder.enabled = false;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventorySlotButton
///
/// One pre-placed item button in the customize panel.
/// Attach to each slot button GO (e.g. Armor1, Armor2, GoldenHelmet…).
///
/// Hierarchy expected:
///   Armor1  (Button + InventorySlotButton)
///     ├── Selected   (Image — gold highlight border, hidden by default)
///     └── [any Image child]   (item preview sprite — optional)
///
/// ── Inspector fields ──────────────────────────────────────────────────────────
///   playerVisualObject → drag the child GO inside the PLAYER preview whose
///                         Image shows this item on the character
///   item               → drag the matching EquipmentItem ScriptableObject
///   selectedBorder     → drag the "Selected" Image child
///   isDefault          → tick ONLY on the one button that must always stay
///                         selected (e.g. default body type)
///
/// ── Key methods ───────────────────────────────────────────────────────────────
///   Select()       — highlights button, shows playerVisualObject, equips item
///   Deselect()     — only unequips if this item still owns the slot (bug fix)
///   ForceDeselect() — silently clears highlight without calling Unequip
///                     (used by InventoryPanel.ApplyItemsToSoldier to sync
///                      button visuals without double-removing bonuses)
/// </summary>
public class InventorySlotButton : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Player Visual — child GO whose Image represents this item on the character")]
    [SerializeField] private GameObject playerVisualObject;

    [Header("UI Visuals")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedBorder;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Settings")]
    [Tooltip("Tick ON for the one button that must always stay selected (e.g. default body).")]
    [SerializeField] private bool isDefault = false;

    [Header("Equipment Item")]
    [Tooltip("Drag the EquipmentItem ScriptableObject for this button. " +
             "Make sure idleSprites[] and walkSprites[] are filled so the " +
             "animation plays on the preview player and spawned soldier.")]
    [SerializeField] private EquipmentItem item;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private InventoryPanel _panel;
    private CharacterEquipment _equipment;
    private bool _clickWired;

    // ─── Public accessors ─────────────────────────────────────────────────────

    public bool IsSelected { get; private set; }
    public bool IsDefault => isDefault;
    public EquipmentItem Item => item;
    public GameObject PlayerVisualObject => playerVisualObject;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    // ─── Init (called by InventoryPanel.InitAllButtons) ──────────────────────

    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
    {
        _panel = panel;

        // Swap soldier reference
        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
        _equipment = equipment;
        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

        // Wire click once only
        if (!_clickWired)
        {
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);
                btn.onClick.AddListener(OnClick);
                _clickWired = true;
            }
        }

        // Set icon from inventoryIcon, fall back to idleSprites[0]
        if (iconImage != null && item != null)
        {
            Sprite spr = item.inventoryIcon
                      ?? (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
            iconImage.sprite = spr;
            iconImage.enabled = spr != null;
        }

        if (nameText != null && item != null) nameText.text = item.itemName;
        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

        RefreshVisual();
    }

    // ─── Click ───────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (_panel == null) return;
        if (isDefault) return;   // default item is always locked

        if (IsSelected) _panel.DeselectButton(this);
        else _panel.SelectButton(this);
    }

    // ─── Select / Deselect ────────────────────────────────────────────────────

    /// <summary>
    /// Highlights this button, shows its playerVisualObject, and equips the item
    /// on the preview Player (updating sprite + stat bonuses).
    /// </summary>
    public void Select()
    {
        IsSelected = true;

        if (playerVisualObject != null)
            playerVisualObject.SetActive(true);

        // CharacterEquipment.Equip():
        //   • removes old item in this slot (reverses its bonuses)
        //   • applies this item's idleSprites[0] to the preview Player Image
        //   • applies this item's stat bonuses
        if (_equipment != null && item != null)
            _equipment.Equip(item);

        RefreshVisual();
    }

    /// <summary>
    /// Hides playerVisualObject and removes stat bonuses — but ONLY if this item
    /// still owns the slot. Prevents accidentally removing the NEW item's bonuses
    /// when the panel calls Deselect on the OLD button after Select() on the new one.
    /// </summary>
    public void Deselect()
    {
        if (isDefault) return;

        IsSelected = false;

        if (playerVisualObject != null)
            playerVisualObject.SetActive(false);

        // KEY FIX: only unequip if this item is still the equipped one.
        // If Select() on another button has already replaced it, skip.
        if (_equipment != null && item != null && _equipment.IsEquipped(item))
            _equipment.Unequip(item.slot);

        RefreshVisual();
    }

    /// <summary>
    /// Silently clears the IsSelected highlight WITHOUT calling Unequip.
    /// Used by InventoryPanel.ApplyItemsToSoldier() to sync button visuals
    /// after it has already equipped the correct item — avoids double-removal
    /// of stat bonuses.
    /// </summary>
    public void ForceDeselect()
    {
        if (isDefault) return;
        IsSelected = false;
        if (playerVisualObject != null) playerVisualObject.SetActive(false);
        RefreshVisual();
    }

    // ─── Equipment change callback ────────────────────────────────────────────

    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
    {
        if (item != null && slot == item.slot)
        {
            IsSelected = changed == item;
            RefreshVisual();
        }
    }

    // ─── Visual ───────────────────────────────────────────────────────────────

    public void RefreshVisual()
    {
        if (selectedBorder != null)
            selectedBorder.enabled = IsSelected;

        // FIX: sync the player's armor visual layer every time selection changes
        if (playerVisualObject != null)
            playerVisualObject.SetActive(IsSelected);
    }
}