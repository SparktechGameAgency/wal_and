//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonCard
/////
///// Represents one card in either:
/////   • Buy Mode     — 3 pre-placed cards in the BuyView (one per cannon type)
/////   • Inventory Mode — dynamically spawned, one per owned cannon copy
/////
///// Clicking the card calls CannonPanelManager.OnCardSelected(this).
/////
///// In Inventory Mode the card also receives a CannonDragHandler component
///// so the player can drag-drop the cannon onto a castle CannonSlot.
///// </summary>
//public class CannonCard : MonoBehaviour
//{
//    // ─── Inspector References ─────────────────────────────────────────────────

//    [Header("UI References")]
//    [SerializeField] private Image cannonPreviewImage;
//    [SerializeField] private TextMeshProUGUI nameText;
//    [SerializeField] private TextMeshProUGUI costText;
//    [Tooltip("Shows '(2/3)' upgrade badge — only visible in Inventory mode")]
//    [SerializeField] private TextMeshProUGUI upgradeCountText;
//    [Tooltip("Green glow / outline shown on the selected card")]
//    [SerializeField] private GameObject selectedHighlight;
//    [Tooltip("Shown while an upgrade is running on this cannon")]
//    [SerializeField] private GameObject upgradingBadge;
//    [SerializeField] private Button cardButton;

//    // ─── Runtime Data ─────────────────────────────────────────────────────────

//    private CannonData _data;
//    private int _inventoryId = -1;   // -1 means Buy-mode card
//    private bool _isBuyMode = true;

//    public CannonData Data => _data;
//    public int InventoryId => _inventoryId;
//    public bool IsBuyMode => _isBuyMode;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (cardButton == null) cardButton = GetComponent<Button>();
//        cardButton?.onClick.AddListener(OnClick);
//        SetSelected(false);
//    }

//    // ─── Setup ────────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Call for the 3 fixed cards in BuyView.
//    /// inventoryId is -1 (no owned copy yet).
//    /// </summary>
//    public void SetupBuyCard(CannonData data)
//    {
//        _data = data;
//        _inventoryId = -1;
//        _isBuyMode = true;

//        SetPreviewSprite(data);
//        if (nameText != null) nameText.text = data.cannonName;
//        if (costText != null)
//        {
//            costText.gameObject.SetActive(true);
//            costText.text = $"{data.cost}";
//        }
//        if (upgradeCountText != null) upgradeCountText.gameObject.SetActive(false);
//        if (upgradingBadge != null) upgradingBadge.SetActive(false);
//    }

//    /// <summary>
//    /// Call for dynamically spawned cards in InventoryView.
//    /// </summary>
//    public void SetupInventoryCard(CannonInventoryEntry entry)
//    {
//        _data = entry.data;
//        _inventoryId = entry.inventoryId;
//        _isBuyMode = false;

//        SetPreviewSprite(entry.data);
//        if (nameText != null) nameText.text = entry.data.cannonName;
//        if (costText != null) costText.gameObject.SetActive(false);

//        RefreshUpgradeBadge(entry);

//        // Add or refresh the drag handler so this card can be dropped onto a slot
//        CannonDragHandler drag = GetComponent<CannonDragHandler>();
//        if (drag == null) drag = gameObject.AddComponent<CannonDragHandler>();
//        drag.Init(entry);
//    }

//    // ─── Runtime Refresh ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by CannonPanelManager after an upgrade completes or starts,
//    /// to keep the badge on this card up to date.
//    /// </summary>
//    public void RefreshUpgradeBadge(CannonInventoryEntry entry)
//    {
//        if (upgradeCountText != null)
//        {
//            upgradeCountText.gameObject.SetActive(true);
//            upgradeCountText.text = entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES
//                ? "MAX"
//                : $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
//        }
//        if (upgradingBadge != null)
//            upgradingBadge.SetActive(entry.isUpgrading);
//    }

//    /// <summary>Highlight or un-highlight this card as the selected one.</summary>
//    public void SetSelected(bool selected)
//    {
//        if (selectedHighlight != null)
//            selectedHighlight.SetActive(selected);
//    }

//    // ─── Private Helpers ──────────────────────────────────────────────────────

//    private void SetPreviewSprite(CannonData data)
//    {
//        if (cannonPreviewImage == null) return;

//        // Use the dedicated previewSprite if set, otherwise fall back to first idle frame
//        Sprite s = data.previewSprite;
//        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//            s = data.idleSprites[0];

//        if (s != null)
//        {
//            cannonPreviewImage.sprite = s;
//            cannonPreviewImage.enabled = true;
//        }
//    }

//    private void OnClick()
//    {
//        CannonPanelManager.Instance?.OnCardSelected(this);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonCard
///
/// Represents one card in either:
///   • Buy Mode       — 3 pre-placed cards in the BuyView (one per cannon type)
///   • Inventory Mode — dynamically spawned, one per owned cannon copy
///                      (shown for BOTH placed and unplaced cannons)
///
/// Clicking the card calls CannonPanelManager.OnCardSelected(this).
///
/// In Inventory Mode the card also receives a CannonDragHandler component
/// so the player can drag-drop the cannon onto a castle CannonSlot.
///
/// EQUIP / UNEQUIP BUTTONS (Inventory Mode only)
///   • equipButton   — visible when the cannon is NOT on the castle.
///                     Calls CannonPanelManager.EquipCannon(inventoryId).
///   • unequipButton — visible when the cannon IS on the castle.
///                     Calls CannonPanelManager.UnequipCannon(inventoryId).
///
/// Inspector setup: add child Button objects to your InventoryCard prefab and
/// drag them into the equipButton / unequipButton fields.
/// </summary>
public class CannonCard : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Image cannonPreviewImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [Tooltip("Shows '(2/3)' upgrade badge — only visible in Inventory mode")]
    [SerializeField] private TextMeshProUGUI upgradeCountText;
    [Tooltip("Green glow / outline shown on the selected card")]
    [SerializeField] private GameObject selectedHighlight;
    [Tooltip("Shown while an upgrade is running on this cannon")]
    [SerializeField] private GameObject upgradingBadge;
    [SerializeField] private Button cardButton;

    [Header("Inventory Mode — Equip / Unequip")]
    [Tooltip("Button shown when the cannon is NOT placed on the castle")]
    [SerializeField] private Button equipButton;
    [Tooltip("Button shown when the cannon IS placed on the castle")]
    [SerializeField] private Button unequipButton;
    [Tooltip("Optional: visual tint / overlay shown when the cannon is on the castle")]
    [SerializeField] private GameObject placedOverlay;

    // ─── Runtime Data ─────────────────────────────────────────────────────────

    private CannonData _data;
    private int _inventoryId = -1;   // -1 means Buy-mode card
    private bool _isBuyMode = true;

    public CannonData Data => _data;
    public int InventoryId => _inventoryId;
    public bool IsBuyMode => _isBuyMode;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        cardButton?.onClick.AddListener(OnClick);
        SetSelected(false);
    }

    // ─── Setup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call for the 3 fixed cards in BuyView.
    /// inventoryId is -1 (no owned copy yet).
    /// </summary>
    public void SetupBuyCard(CannonData data)
    {
        _data = data;
        _inventoryId = -1;
        _isBuyMode = true;

        SetPreviewSprite(data);
        if (nameText != null) nameText.text = data.cannonName;
        if (costText != null)
        {
            costText.gameObject.SetActive(true);
            costText.text = $"{data.cost}";
        }
        if (upgradeCountText != null) upgradeCountText.gameObject.SetActive(false);
        if (upgradingBadge != null) upgradingBadge.SetActive(false);

        // Equip/Unequip buttons are irrelevant in Buy mode
        if (equipButton != null) equipButton.gameObject.SetActive(false);
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
        if (placedOverlay != null) placedOverlay.SetActive(false);
    }

    /// <summary>
    /// Call for dynamically spawned cards in InventoryView.
    /// Works for both placed and unplaced cannons — the equip/unequip
    /// buttons reflect the current placement state.
    /// </summary>
    public void SetupInventoryCard(CannonInventoryEntry entry)
    {
        _data = entry.data;
        _inventoryId = entry.inventoryId;
        _isBuyMode = false;

        SetPreviewSprite(entry.data);
        if (nameText != null) nameText.text = entry.data.cannonName;
        if (costText != null) costText.gameObject.SetActive(false);

        RefreshUpgradeBadge(entry);
        RefreshEquipState(entry.isPlacedOnCastle);

        // Add or refresh the drag handler so unplaced cards can be dropped onto a slot.
        // CannonDragHandler.OnBeginDrag already blocks dragging placed cannons,
        // so it is safe to add it unconditionally.
        CannonDragHandler drag = GetComponent<CannonDragHandler>();
        if (drag == null) drag = gameObject.AddComponent<CannonDragHandler>();
        drag.Init(entry);
    }

    // ─── Runtime Refresh ──────────────────────────────────────────────────────

    /// <summary>
    /// Called by CannonPanelManager after an upgrade completes or starts,
    /// to keep the badge on this card up to date.
    /// </summary>
    public void RefreshUpgradeBadge(CannonInventoryEntry entry)
    {
        if (upgradeCountText != null)
        {
            upgradeCountText.gameObject.SetActive(true);
            upgradeCountText.text = entry.upgradeCount >= CannonInventoryEntry.MAX_UPGRADES
                ? "MAX"
                : $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
        }
        if (upgradingBadge != null)
            upgradingBadge.SetActive(entry.isUpgrading);
    }

    /// <summary>
    /// Swaps the visible action button and optional overlay based on whether
    /// this cannon is currently placed on the castle.
    /// Called from SetupInventoryCard() and whenever placement state changes.
    /// </summary>
    public void RefreshEquipState(bool isPlaced)
    {
        // ── Equip button ──────────────────────────────────────────────────────
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isPlaced);
            equipButton.onClick.RemoveAllListeners();
            if (!isPlaced)
            {
                int id = _inventoryId;   // capture for lambda
                equipButton.onClick.AddListener(() => CannonPanelManager.Instance?.EquipCannon(id));
            }
        }

        // ── Unequip button ────────────────────────────────────────────────────
        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isPlaced);
            unequipButton.onClick.RemoveAllListeners();
            if (isPlaced)
            {
                int id = _inventoryId;   // capture for lambda
                unequipButton.onClick.AddListener(() => CannonPanelManager.Instance?.UnequipCannon(id));
            }
        }

        // ── Optional placed-on-castle visual tint ─────────────────────────────
        if (placedOverlay != null)
            placedOverlay.SetActive(isPlaced);
    }

    /// <summary>Highlight or un-highlight this card as the selected one.</summary>
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private void SetPreviewSprite(CannonData data)
    {
        if (cannonPreviewImage == null) return;

        // Use the dedicated previewSprite if set, otherwise fall back to first idle frame
        Sprite s = data.previewSprite;
        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
            s = data.idleSprites[0];

        if (s != null)
        {
            cannonPreviewImage.sprite = s;
            cannonPreviewImage.enabled = true;
        }
    }

    private void OnClick()
    {
        CannonPanelManager.Instance?.OnCardSelected(this);
    }
}