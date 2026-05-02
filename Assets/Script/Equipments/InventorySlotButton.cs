using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventorySlotButton
///
/// One button in the inventory grid.
/// Displays the item's icon, name, and a "selected" highlight border.
/// Attach to the SlotButton prefab inside the inventory scroll grid.
///
/// SlotButton prefab structure:
///   SlotButton               ← Button, InventorySlotButton, Image (background)
///     ├── Icon               ← Image (item icon sprite)
///     ├── NameText           ← TextMeshProUGUI (item name)
///     └── SelectedBorder     ← Image (yellow/gold border, hidden by default)
/// </summary>
public class InventorySlotButton : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image selectedBorder;
    [SerializeField] private Image background;

    // ─── State ───────────────────────────────────────────────────────────────

    private EquipmentItem _item;
    private CharacterEquipment _equipment;
    private InventoryPanel _panel;

    // ─── Setup ───────────────────────────────────────────────────────────────

    /// <summary>Called by InventoryPanel when populating the grid.</summary>
    public void Setup(EquipmentItem item, CharacterEquipment equipment, InventoryPanel panel)
    {
        _item = item;
        _equipment = equipment;
        _panel = panel;

        // Visual fill
        if (iconImage != null) iconImage.sprite = item.inventoryIcon;
        if (nameText != null) nameText.text = item.itemName;
        if (background != null) background.color = new Color(0.15f, 0.15f, 0.15f);

        // Rarity border tint
        if (selectedBorder != null)
            selectedBorder.color = item.rarityColour;

        // Wire click
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnClick);

        // Reflect current selection state
        RefreshSelection();

        // Subscribe to equipment changes so this button updates live
        _equipment.OnEquipmentChanged += OnEquipmentChanged;
    }

    private void OnDestroy()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    // ─── Click ───────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (_equipment.IsEquipped(_item))
        {
            // Clicking the already-equipped item unequips it
            _equipment.Unequip(_item.slot);
        }
        else
        {
            _equipment.Equip(_item);
        }

        _panel.RefreshAllButtons();
    }

    // ─── Selection Visual ────────────────────────────────────────────────────

    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
    {
        if (slot == _item.slot)
            RefreshSelection();
    }

    public void RefreshSelection()
    {
        bool selected = _equipment.IsEquipped(_item);
        if (selectedBorder != null) selectedBorder.enabled = selected;
        if (background != null)
            background.color = selected
                ? new Color(0.25f, 0.20f, 0.05f)   // warm gold tint when selected
                : new Color(0.12f, 0.12f, 0.12f);
    }
}