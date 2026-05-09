using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonCard
///
/// Represents one card in the cannon panel grid.
/// Used in BOTH modes:
///   Buy Mode       — 3 pre-placed cards, one per cannon type. All start locked.
///   Inventory Mode — dynamically spawned, one per owned cannon copy.
///
/// Clicking the card calls CannonPanelManager.Instance.OnCardSelected(this).
///
/// Hierarchy per card:
///   CannonCard (this script + Button)
///   ├── CannonImage          Image — shows previewSprite
///   ├── NameText             TMP   — cannon name below the image
///   ├── LockOverlay          Image — semi-transparent lock, active when locked
///   ├── SelectedHighlight    Image/Outline — active when this card is selected
///   └── UpgradeBadge         TMP   — "(2/3)" shown in Inventory mode only
/// </summary>
public class CannonCard : MonoBehaviour
{
    // ── Inspector refs ─────────────────────────────────────────────────────────
    [SerializeField] private Image cannonImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private TextMeshProUGUI upgradeBadge;   // "(1/3)", "MAX"
    [SerializeField] private Button cardButton;

    // ── Runtime data ───────────────────────────────────────────────────────────
    private CannonData _data;
    private int _inventoryId = -1;
    private bool _isBuyMode = true;
    private bool _locked = true;

    public CannonData Data => _data;
    public int InventoryId => _inventoryId;
    public bool IsBuyMode => _isBuyMode;

    // ── Unity ──────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        cardButton?.onClick.AddListener(OnClick);
        SetSelected(false);
    }

    // ── Setup ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call for the 3 fixed buy-mode cards.
    /// locked = true means the player hasn't purchased this type yet.
    /// </summary>
    public void SetupBuyCard(CannonData data, bool locked)
    {
        _data = data;
        _inventoryId = -1;
        _isBuyMode = true;
        _locked = locked;

        ApplySprite(data);
        if (nameText != null) nameText.text = data.cannonName;
        if (lockOverlay != null) lockOverlay.SetActive(locked);
        if (upgradeBadge != null) upgradeBadge.gameObject.SetActive(false);
    }

    /// <summary>
    /// Call for dynamically spawned inventory cards.
    /// </summary>
    public void SetupInventoryCard(CannonInventoryEntry entry)
    {
        _data = entry.data;
        _inventoryId = entry.inventoryId;
        _isBuyMode = false;
        _locked = false;

        ApplySprite(entry.data);
        if (nameText != null) nameText.text = entry.data.cannonName;
        if (lockOverlay != null) lockOverlay.SetActive(false);

        RefreshBadge(entry);
    }

    // ── Runtime refresh ────────────────────────────────────────────────────────

    /// <summary>Called by CannonPanelManager after upgrade starts/completes.</summary>
    public void RefreshBadge(CannonInventoryEntry entry)
    {
        if (upgradeBadge == null) return;
        upgradeBadge.gameObject.SetActive(true);
        upgradeBadge.text = entry.IsMaxLevel
            ? "MAX"
            : $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
    }

    /// <summary>Removes the lock overlay after the player purchases this type.</summary>
    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (lockOverlay != null) lockOverlay.SetActive(locked);
    }

    /// <summary>Shows or hides the selected outline/glow.</summary>
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void ApplySprite(CannonData data)
    {
        if (cannonImage == null) return;
        Sprite s = data.previewSprite;
        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
            s = data.idleSprites[0];
        if (s != null) { cannonImage.sprite = s; cannonImage.enabled = true; }
    }

    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
}