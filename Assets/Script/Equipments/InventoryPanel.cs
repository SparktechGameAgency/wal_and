using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventoryPanel
///
/// The main inventory window. Shows one tab per equipment slot.
/// Clicking a tab filters the item grid to show only items for that slot.
/// Clicking an item equips it on the soldier instantly.
///
/// ── UI Hierarchy to build ────────────────────────────────────────────────────
///
///   InventoryPanel  (Panel)           ← InventoryPanel.cs
///     ├── TabBar    (HorizontalLayoutGroup)
///     │     ├── Tab_BodyType  (Button + TabButton.cs)
///     │     ├── Tab_Face      (Button + TabButton.cs)
///     │     ├── Tab_Hair      (Button + TabButton.cs)
///     │     ├── Tab_Helmet    (Button + TabButton.cs)
///     │     ├── Tab_Armor     (Button + TabButton.cs)
///     │     └── Tab_Weapon    (Button + TabButton.cs)
///     ├── ItemGrid  (ScrollRect → Viewport → Content)
///     │     └── Content  (GridLayoutGroup)  ← SlotButton prefabs spawn here
///     └── StatsPreview (optional)
///           ├── HPText   (TextMeshProUGUI)
///           ├── APText   (TextMeshProUGUI)
///           └── ADText   (TextMeshProUGUI)
///
/// ── Inspector fields ─────────────────────────────────────────────────────────
///   • soldierEquipment  → drag the SolderPrefab (or its CharacterEquipment)
///   • allItems[]        → drag ALL your EquipmentItem ScriptableObject assets
///   • slotButtonPrefab  → drag the SlotButton prefab
///   • gridContent       → drag the Content object inside the ScrollRect
///   • tab buttons       → drag each Tab_XXX button
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Soldier Reference")]
    [Tooltip("The soldier whose equipment this panel controls")]
    [SerializeField] private CharacterEquipment soldierEquipment;

    [Header("All Equipment Items")]
    [Tooltip("Drag every EquipmentItem ScriptableObject asset here")]
    [SerializeField] private EquipmentItem[] allItems;

    [Header("Grid")]
    [Tooltip("The SlotButton prefab (has InventorySlotButton + Button + Image)")]
    [SerializeField] private GameObject slotButtonPrefab;
    [Tooltip("GridLayoutGroup Content object inside the ScrollRect")]
    [SerializeField] private Transform gridContent;

    [Header("Tab Buttons (one per slot — order must match EquipmentSlot enum)")]
    [SerializeField] private Button tabBodyType;
    [SerializeField] private Button tabFace;
    [SerializeField] private Button tabHair;
    [SerializeField] private Button tabHelmet;
    [SerializeField] private Button tabArmor;
    [SerializeField] private Button tabWeapon;

    [Header("Tab Active Colour")]
    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

    [Header("Stats Preview (optional — leave null to skip)")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private TextMeshProUGUI adText;

    // ─── Private ──────────────────────────────────────────────────────────────

    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
    private readonly List<InventorySlotButton> _spawnedButtons = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Wire tab buttons
        tabBodyType?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
        tabFace?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));
    }

    private void OnEnable()
    {
        // Refresh stats display whenever the panel is opened
        if (soldierEquipment != null)
            soldierEquipment.OnEquipmentChanged += OnEquipmentChanged;

        ShowSlot(_activeSlot);
        RefreshStats();
    }

    private void OnDisable()
    {
        if (soldierEquipment != null)
            soldierEquipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    // ─── Tab Logic ────────────────────────────────────────────────────────────

    private void ShowSlot(EquipmentSlot slot)
    {
        _activeSlot = slot;
        UpdateTabColours();
        PopulateGrid(slot);
    }

    private void UpdateTabColours()
    {
        SetTabColour(tabBodyType, EquipmentSlot.BodyType);
        SetTabColour(tabFace, EquipmentSlot.Face);
        SetTabColour(tabHair, EquipmentSlot.Hair);
        SetTabColour(tabHelmet, EquipmentSlot.Helmet);
        SetTabColour(tabArmor, EquipmentSlot.Armor);
        SetTabColour(tabWeapon, EquipmentSlot.Weapon);
    }

    private void SetTabColour(Button btn, EquipmentSlot slot)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
    }

    // ─── Grid Population ──────────────────────────────────────────────────────

    private void PopulateGrid(EquipmentSlot slot)
    {
        // Clear existing buttons
        foreach (var btn in _spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        _spawnedButtons.Clear();

        // Spawn one button per item that matches this slot
        foreach (var item in allItems)
        {
            if (item == null || item.slot != slot) continue;

            var go = Instantiate(slotButtonPrefab, gridContent);
            var btn = go.GetComponent<InventorySlotButton>();
            if (btn != null)
            {
                btn.Setup(item, soldierEquipment, this);
                _spawnedButtons.Add(btn);
            }
        }
    }

    // ─── Button Refresh ───────────────────────────────────────────────────────

    /// <summary>Called by InventorySlotButton after any equip/unequip.</summary>
    public void RefreshAllButtons()
    {
        foreach (var btn in _spawnedButtons)
            btn?.RefreshSelection();

        RefreshStats();
    }

    // ─── Stats Preview ────────────────────────────────────────────────────────

    private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
        => RefreshStats();

    private void RefreshStats()
    {
        if (soldierEquipment == null) return;
        var stats = soldierEquipment.GetComponent<SoldierStats>();
        if (stats == null) return;

        if (hpText != null) hpText.text = $"HP  {stats.MaxHealth:F0}";
        if (apText != null) apText.text = $"AP  {stats.AbilityPower:F0}";
        if (adText != null) adText.text = $"AD  {stats.AttackDamage:F0}";
    }

    // ─── Open / Close (call from a button in the HUD) ────────────────────────

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
}