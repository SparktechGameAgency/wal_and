//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// AREA FORGE - InventorySlotButton
/////////
///////// One button in the inventory grid.
///////// Displays the item's icon, name, and a "selected" highlight border.
///////// Attach to the SlotButton prefab inside the inventory scroll grid.
/////////
///////// SlotButton prefab structure:
/////////   SlotButton               ← Button, InventorySlotButton, Image (background)
/////////     ├── Icon               ← Image (item icon sprite)
/////////     ├── NameText           ← TextMeshProUGUI (item name)
/////////     └── SelectedBorder     ← Image (yellow/gold border, hidden by default)
///////// </summary>
//////public class InventorySlotButton : MonoBehaviour
//////{
//////    // ─── Inspector ────────────────────────────────────────────────────────────

//////    [SerializeField] private Image iconImage;
//////    [SerializeField] private TextMeshProUGUI nameText;
//////    [SerializeField] private Image selectedBorder;
//////    [SerializeField] private Image background;

//////    // ─── State ───────────────────────────────────────────────────────────────

//////    private EquipmentItem _item;
//////    private CharacterEquipment _equipment;
//////    private InventoryPanel _panel;

//////    // ─── Setup ───────────────────────────────────────────────────────────────

//////    /// <summary>Called by InventoryPanel when populating the grid.</summary>
//////    public void Setup(EquipmentItem item, CharacterEquipment equipment, InventoryPanel panel)
//////    {
//////        _item = item;
//////        _equipment = equipment;
//////        _panel = panel;

//////        // Visual fill
//////        if (iconImage != null) iconImage.sprite = item.inventoryIcon;
//////        if (nameText != null) nameText.text = item.itemName;
//////        if (background != null) background.color = new Color(0.15f, 0.15f, 0.15f);

//////        // Rarity border tint
//////        if (selectedBorder != null)
//////            selectedBorder.color = item.rarityColour;

//////        // Wire click
//////        var btn = GetComponent<Button>();
//////        if (btn != null) btn.onClick.AddListener(OnClick);

//////        // Reflect current selection state
//////        RefreshSelection();

//////        // Subscribe to equipment changes so this button updates live
//////        _equipment.OnEquipmentChanged += OnEquipmentChanged;
//////    }

//////    private void OnDestroy()
//////    {
//////        if (_equipment != null)
//////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//////    }

//////    // ─── Click ───────────────────────────────────────────────────────────────

//////    private void OnClick()
//////    {
//////        if (_equipment.IsEquipped(_item))
//////        {
//////            // Clicking the already-equipped item unequips it
//////            _equipment.Unequip(_item.slot);
//////        }
//////        else
//////        {
//////            _equipment.Equip(_item);
//////        }

//////        _panel.RefreshAllButtons();
//////    }

//////    // ─── Selection Visual ────────────────────────────────────────────────────

//////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
//////    {
//////        if (slot == _item.slot)
//////            RefreshSelection();
//////    }

//////    public void RefreshSelection()
//////    {
//////        bool selected = _equipment.IsEquipped(_item);
//////        if (selectedBorder != null) selectedBorder.enabled = selected;
//////        if (background != null)
//////            background.color = selected
//////                ? new Color(0.25f, 0.20f, 0.05f)   // warm gold tint when selected
//////                : new Color(0.12f, 0.12f, 0.12f);
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// AREA FORGE - InventorySlotButton  (Pre-Placed version)
///////
/////// Attach this to each pre-placed item button in your hierarchy.
/////// Drag the correct EquipmentItem ScriptableObject into the "Item" field.
/////// The soldier reference and panel reference are injected at runtime by
/////// InventoryPanel.InitGroup() — you do NOT need to set those in the Inspector.
///////
/////// SlotButton hierarchy example:
///////   Armor1                    ← Button + InventorySlotButton
///////     ├── Selected            ← Image (gold highlight border, hidden by default)
///////     └── Armor               ← Image (item icon / preview sprite)
/////// </summary>
////public class InventorySlotButton : MonoBehaviour
////{
////    // ─── Inspector ────────────────────────────────────────────────────────────

////    [Header("Item — drag the EquipmentItem ScriptableObject here")]
////    [SerializeField] private EquipmentItem item;

////    [Header("Visuals — drag child Images here")]
////    [SerializeField] private Image iconImage;
////    [SerializeField] private Image selectedBorder;   // "Selected" child
////    [SerializeField] private Image background;

////    [SerializeField] private TextMeshProUGUI nameText;  // optional

////    // ─── Runtime (injected by InventoryPanel) ─────────────────────────────────

////    private CharacterEquipment _equipment;
////    private InventoryPanel _panel;
////    private bool _initialised;

////    // ─── Init (called by InventoryPanel.InitGroup) ────────────────────────────

////    /// <summary>
////    /// Called every time the panel opens (or a new soldier spawns).
////    /// Safe to call multiple times — unsubscribes from the old soldier first.
////    /// </summary>
////    public void Init(CharacterEquipment equipment, InventoryPanel panel)
////    {
////        // Unsubscribe from old soldier
////        if (_equipment != null)
////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;

////        _equipment = equipment;
////        _panel = panel;
////        _initialised = true;

////        // Subscribe to new soldier
////        if (_equipment != null)
////            _equipment.OnEquipmentChanged += OnEquipmentChanged;

////        // Wire the click only once
////        if (!_initialised)
////        {
////            var btn = GetComponent<Button>();
////            if (btn != null) btn.onClick.AddListener(OnClick);
////        }

////        // Apply icon sprite if assigned
////        if (iconImage != null && item != null)
////        {
////            iconImage.sprite = item.inventoryIcon != null ? item.inventoryIcon
////                                                           : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
////            iconImage.enabled = iconImage.sprite != null;
////        }

////        // Rarity tint on the selected border
////        if (selectedBorder != null && item != null)
////            selectedBorder.color = item.rarityColour;

////        if (nameText != null && item != null)
////            nameText.text = item.itemName;

////        RefreshSelection();
////    }

////    private void Awake()
////    {
////        // Wire click once on Awake so it works even if Init hasn't been called yet
////        var btn = GetComponent<Button>();
////        if (btn != null) btn.onClick.AddListener(OnClick);
////    }

////    private void OnDestroy()
////    {
////        if (_equipment != null)
////            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
////    }

////    // ─── Click ───────────────────────────────────────────────────────────────

////    private void OnClick()
////    {
////        if (_equipment == null || item == null) return;

////        if (_equipment.IsEquipped(item))
////            _equipment.Unequip(item.slot);
////        else
////            _equipment.Equip(item);

////        _panel?.RefreshAllButtons();
////    }

////    // ─── Selection Visual ────────────────────────────────────────────────────

////    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changedItem)
////    {
////        if (item != null && slot == item.slot)
////            RefreshSelection();
////    }

////    /// <summary>Updates the selected border and background to reflect equip state.</summary>
////    public void RefreshSelection()
////    {
////        if (_equipment == null || item == null) return;

////        bool selected = _equipment.IsEquipped(item);

////        if (selectedBorder != null) selectedBorder.enabled = selected;

////        if (background != null)
////            background.color = selected
////                ? new Color(0.25f, 0.20f, 0.05f)   // warm gold when selected
////                : new Color(0.12f, 0.12f, 0.12f);
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - InventorySlotButton
/////
///// Attach to each item button in the inventory grid.
/////
///// ── Inspector wiring (per button) ───────────────────────────────────────────
/////   playerVisualObject → drag the matching child GO from the Player hierarchy
/////                        e.g. Silver Armor button  → drag Player/Armor/Armor1
/////                             Gold   Armor button  → drag Player/Armor/Armor2
/////                             Short  Hair  button  → drag Player/Hair/Hair1
/////                             etc.
/////   iconImage          → drag the Icon Image child of this button
/////   selectedBorder     → drag the Selected highlight Image child
/////
///// ── Behaviour ────────────────────────────────────────────────────────────────
/////   • Tap item      → activates Armor2 (for example), deactivates Armor1/3/4
/////   • Tap again     → deselects, deactivates Armor2 (nothing worn)
/////   • Default       → InventoryPanel auto-selects the first button per group
///// </summary>
//public class InventorySlotButton : MonoBehaviour
//{
//    // ─── Inspector ────────────────────────────────────────────────────────────

//    [Header("Player Visual")]
//    [Tooltip("Drag the child GO from Player hierarchy that this item represents.\n" +
//             "e.g. Silver Armor → Player/Armor/Armor1\n" +
//             "     Gold Armor   → Player/Armor/Armor2")]
//    [SerializeField] private GameObject playerVisualObject;

//    [Header("Inventory UI")]
//    [SerializeField] private Image iconImage;
//    [SerializeField] private Image selectedBorder;
//    [SerializeField] private Image background;
//    [SerializeField] private TextMeshProUGUI nameText;

//    [Header("Stats (optional)")]
//    [Tooltip("Drag an EquipmentItem ScriptableObject here only if you want stat bonuses.\n" +
//             "Leave empty for purely visual items.")]
//    [SerializeField] private EquipmentItem item;

//    // ─── Runtime ──────────────────────────────────────────────────────────────

//    private InventoryPanel _panel;
//    private CharacterEquipment _equipment;

//    public bool IsSelected { get; private set; }
//    public GameObject PlayerVisualObject => playerVisualObject;
//    public EquipmentItem Item => item;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        var btn = GetComponent<Button>();
//        if (btn != null) btn.onClick.AddListener(OnClick);
//    }

//    private void OnDestroy()
//    {
//        if (_equipment != null)
//            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//    }

//    // ─── Init (called by InventoryPanel on open) ──────────────────────────────

//    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
//    {
//        _panel = panel;

//        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
//        _equipment = equipment;
//        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

//        // Populate icon
//        if (iconImage != null && item != null)
//        {
//            var spr = item.inventoryIcon != null ? item.inventoryIcon
//                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
//            iconImage.sprite = spr;
//            iconImage.enabled = spr != null;
//        }

//        if (nameText != null && item != null) nameText.text = item.itemName;
//        if (selectedBorder != null && item != null) selectedBorder.color = item.rarityColour;

//        RefreshVisual();
//    }

//    // ─── Click ───────────────────────────────────────────────────────────────

//    private void OnClick()
//    {
//        if (_panel == null) return;

//        if (IsSelected)
//            _panel.DeselectButton(this);
//        else
//            _panel.SelectButton(this);
//    }

//    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

//    /// <summary>Activates the player GO and marks this button selected.</summary>
//    public void Select()
//    {
//        IsSelected = true;

//        if (playerVisualObject != null)
//            playerVisualObject.SetActive(true);

//        if (_equipment != null && item != null)
//            _equipment.Equip(item);

//        RefreshVisual();
//    }

//    /// <summary>Deactivates the player GO and marks this button deselected.</summary>
//    public void Deselect()
//    {
//        IsSelected = false;

//        if (playerVisualObject != null)
//            playerVisualObject.SetActive(false);

//        if (_equipment != null && item != null)
//            _equipment.Unequip(item.slot);

//        RefreshVisual();
//    }

//    // ─── Visual ───────────────────────────────────────────────────────────────

//    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
//    {
//        if (item != null && slot == item.slot)
//        {
//            IsSelected = changed == item;
//            RefreshVisual();
//        }
//    }

//    public void RefreshVisual()
//    {
//        if (selectedBorder != null)
//            selectedBorder.enabled = IsSelected;

//        //if (background != null)
//        //    background.color = IsSelected
//        //        ? new Color(0.25f, 0.20f, 0.05f)
//        //        : new Color(0.12f, 0.12f, 0.12f);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventorySlotButton
///
/// Attach to each item button in the inventory grid.
///
/// ── Inspector wiring (per button) ───────────────────────────────────────────
///   playerVisualObject → drag the matching child GO from the Player hierarchy
///                        e.g. Silver Armor button  → Player/Armor/Armor1
///                             Gold   Armor button  → Player/Armor/Armor2
///   selectedBorder     → drag the "Selected" highlight Image child (shown when active)
///   iconImage          → drag the Icon Image child (optional)
///
/// NOTE: The click listener is wired in Init(), NOT in Awake(), because groups
///       are inactive at start — Awake() never runs on inactive GameObjects.
/// </summary>
public class InventorySlotButton : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Player Visual — drag the Player child GO this button represents")]
    [SerializeField] private GameObject playerVisualObject;

    [Header("Inventory UI Visuals")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedBorder;   // shown when selected
    [SerializeField] private TextMeshProUGUI nameText;        // optional

    [Header("Stats (optional — leave empty for purely visual items)")]
    [SerializeField] private EquipmentItem item;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private InventoryPanel _panel;
    private CharacterEquipment _equipment;
    private bool _clickWired;

    public bool IsSelected { get; private set; }
    public GameObject PlayerVisualObject => playerVisualObject;
    public EquipmentItem Item => item;

    private void OnDestroy()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    // ─── Init (called by InventoryPanel — groups may be inactive, so NOT Awake) ──

    public void Init(InventoryPanel panel, CharacterEquipment equipment = null)
    {
        _panel = panel;

        if (_equipment != null) _equipment.OnEquipmentChanged -= OnEquipmentChanged;
        _equipment = equipment;
        if (_equipment != null) _equipment.OnEquipmentChanged += OnEquipmentChanged;

        // Wire click here (once) because Awake may never run if GO starts inactive
        if (!_clickWired)
        {
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);   // safety — avoid double-add
                btn.onClick.AddListener(OnClick);
                _clickWired = true;
            }
        }

        // Populate icon
        if (iconImage != null && item != null)
        {
            var spr = item.inventoryIcon != null ? item.inventoryIcon
                      : (item.idleSprites?.Length > 0 ? item.idleSprites[0] : null);
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
        if (_panel == null)
        {
            Debug.LogWarning($"[InventorySlotButton] {name}: _panel is null — was Init() called?");
            return;
        }

        if (IsSelected)
            _panel.DeselectButton(this);
        else
            _panel.SelectButton(this);
    }

    // ─── Select / Deselect (driven by InventoryPanel) ─────────────────────────

    public void Select()
    {
        IsSelected = true;
        if (playerVisualObject != null) playerVisualObject.SetActive(true);
        if (_equipment != null && item != null) _equipment.Equip(item);
        RefreshVisual();
    }

    public void Deselect()
    {
        IsSelected = false;
        if (playerVisualObject != null) playerVisualObject.SetActive(false);
        if (_equipment != null && item != null) _equipment.Unequip(item.slot);
        RefreshVisual();
    }

    // ─── Visual ───────────────────────────────────────────────────────────────

    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem changed)
    {
        if (item != null && slot == item.slot)
        {
            IsSelected = changed == item;
            RefreshVisual();
        }
    }

    public void RefreshVisual()
    {
        // Only the selected border toggles — background color is NEVER changed
        if (selectedBorder != null)
            selectedBorder.enabled = IsSelected;
    }
}