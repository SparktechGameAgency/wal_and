////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonCard
///////
/////// Represents one card in the cannon panel grid.
/////// Used in BOTH modes:
///////   Buy Mode       — 3 pre-placed cards, one per cannon type. All start locked.
///////   Inventory Mode — dynamically spawned, one per owned cannon copy.
///////
/////// Clicking the card calls CannonPanelManager.Instance.OnCardSelected(this).
///////
/////// Hierarchy per card:
///////   CannonCard (this script + Button)
///////   ├── CannonImage          Image — shows previewSprite
///////   ├── NameText             TMP   — cannon name below the image
///////   ├── LockOverlay          Image — semi-transparent lock, active when locked
///////   ├── SelectedHighlight    Image/Outline — active when this card is selected
///////   └── UpgradeBadge         TMP   — "(2/3)" shown in Inventory mode only
/////// </summary>
////public class CannonCard : MonoBehaviour
////{
////    // ── Inspector refs ─────────────────────────────────────────────────────────
////    [SerializeField] private Image cannonImage;
////    [SerializeField] private TextMeshProUGUI nameText;
////    [SerializeField] private GameObject lockOverlay;
////    [SerializeField] private GameObject selectedHighlight;
////    [SerializeField] private TextMeshProUGUI upgradeBadge;   // "(1/3)", "MAX"
////    [SerializeField] private Button cardButton;

////    // ── Runtime data ───────────────────────────────────────────────────────────
////    private CannonData _data;
////    private int _inventoryId = -1;
////    private bool _isBuyMode = true;
////    private bool _locked = true;

////    public CannonData Data => _data;
////    public int InventoryId => _inventoryId;
////    public bool IsBuyMode => _isBuyMode;

////    // ── Unity ──────────────────────────────────────────────────────────────────
////    private void Awake()
////    {
////        if (cardButton == null) cardButton = GetComponent<Button>();
////        cardButton?.onClick.AddListener(OnClick);
////        SetSelected(false);
////    }

////    // ── Setup ──────────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Call for the 3 fixed buy-mode cards.
////    /// locked = true means the player hasn't purchased this type yet.
////    /// </summary>
////    public void SetupBuyCard(CannonData data, bool locked)
////    {
////        _data = data;
////        _inventoryId = -1;
////        _isBuyMode = true;
////        _locked = locked;

////        ApplySprite(data);
////        if (nameText != null) nameText.text = data.cannonName;
////        if (lockOverlay != null) lockOverlay.SetActive(locked);
////        if (upgradeBadge != null) upgradeBadge.gameObject.SetActive(false);
////    }

////    /// <summary>
////    /// Call for dynamically spawned inventory cards.
////    /// </summary>
////    public void SetupInventoryCard(CannonInventoryEntry entry)
////    {
////        _data = entry.data;
////        _inventoryId = entry.inventoryId;
////        _isBuyMode = false;
////        _locked = false;

////        ApplySprite(entry.data);
////        if (nameText != null) nameText.text = entry.data.cannonName;
////        if (lockOverlay != null) lockOverlay.SetActive(false);

////        RefreshBadge(entry);
////    }

////    // ── Runtime refresh ────────────────────────────────────────────────────────

////    /// <summary>Called by CannonPanelManager after upgrade starts/completes.</summary>
////    public void RefreshBadge(CannonInventoryEntry entry)
////    {
////        if (upgradeBadge == null) return;
////        upgradeBadge.gameObject.SetActive(true);
////        upgradeBadge.text = entry.IsMaxLevel
////            ? "MAX"
////            : $"({entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES})";
////    }

////    /// <summary>Removes the lock overlay after the player purchases this type.</summary>
////    public void SetLocked(bool locked)
////    {
////        _locked = locked;
////        if (lockOverlay != null) lockOverlay.SetActive(locked);
////    }

////    /// <summary>Shows or hides the selected outline/glow.</summary>
////    public void SetSelected(bool selected)
////    {
////        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
////    }

////    // ── Private ────────────────────────────────────────────────────────────────

////    private void ApplySprite(CannonData data)
////    {
////        if (cannonImage == null) return;
////        Sprite s = data.previewSprite;
////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
////            s = data.idleSprites[0];
////        if (s != null) { cannonImage.sprite = s; cannonImage.enabled = true; }
////    }

////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
////}

//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonCard
/////
///// Attach this to the root of your CannonCard prefab.
///// The script auto-wires all child refs by name in Awake() so you never need
///// to drag anything in the Inspector — just keep these exact child names:
/////
/////   CannonCard (root — Button + this script)
/////   ├── CannonImage      (Image component)
/////   ├── CardName         (TextMeshProUGUI)
/////   ├── Selected         (any GameObject — shown when this card is selected)
/////   ├── Locked           (any GameObject — shown when type not purchased)
/////   └── UpgradeBadge     (GameObject with a TextMeshProUGUI — "1/3" / "MAX")
/////
///// The card reports clicks to CannonPanelManager.Instance.OnCardSelected(this).
///// </summary>
//[RequireComponent(typeof(Button))]
//public class CannonCard : MonoBehaviour
//{
//    // ── Auto-wired at runtime (no Inspector drag needed) ───────────────────────
//    private Image _cannonImage;
//    private TextMeshProUGUI _nameText;
//    private GameObject _selectedHighlight;
//    private GameObject _lockOverlay;
//    private GameObject _badgeRoot;          // UpgradeBadge GameObject
//    private TextMeshProUGUI _badgeText;          // TMP inside UpgradeBadge
//    private Button _button;

//    // ── Runtime data ───────────────────────────────────────────────────────────
//    private CannonData _data;
//    private int _inventoryId = -1;
//    private bool _isBuyMode = true;

//    public CannonData Data => _data;
//    public int InventoryId => _inventoryId;
//    public bool IsBuyMode => _isBuyMode;

//    // ── Unity ──────────────────────────────────────────────────────────────────
//    private void Awake()
//    {
//        // ── wire children by name ──────────────────────────────────────────────
//        Transform t = transform;

//        var imgT = t.Find("CannonImage");
//        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

//        var nameT = t.Find("CardName");
//        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

//        var selT = t.Find("Selected");
//        if (selT != null) _selectedHighlight = selT.gameObject;

//        var lockT = t.Find("Locked");
//        if (lockT != null) _lockOverlay = lockT.gameObject;

//        var badgeT = t.Find("UpgradeBadge");
//        if (badgeT != null)
//        {
//            _badgeRoot = badgeT.gameObject;
//            // TMP may sit directly on the badge, or on a child Text object
//            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
//                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
//        }

//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnClick);

//        SetSelected(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // SETUP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Used by the 3 fixed buy-mode cards.
//    /// <paramref name="locked"/> = true until the player buys this type at least once.
//    /// </summary>
//    public void SetupBuyCard(CannonData data, bool locked)
//    {
//        _data = data;
//        _inventoryId = -1;
//        _isBuyMode = true;

//        ApplySprite(data);
//        SetCardName(data.cannonName);
//        SetLocked(locked);
//        ShowBadge(false, string.Empty);
//        SetSelected(false);
//    }

//    /// <summary>
//    /// Used for dynamically spawned inventory cards.
//    /// <paramref name="displayName"/> is already formatted, e.g. "Iron Cannon (2/3)".
//    /// </summary>
//    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
//    {
//        _data = entry.data;
//        _inventoryId = entry.inventoryId;
//        _isBuyMode = false;

//        ApplySprite(entry.data);
//        SetCardName(displayName);
//        SetLocked(false);          // inventory cards are always owned — never locked
//        RefreshBadge(entry);
//        SetSelected(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // RUNTIME REFRESH
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Updates the upgrade badge text. Called after an upgrade starts or completes.</summary>
//    public void RefreshBadge(CannonInventoryEntry entry)
//    {
//        if (entry == null) { ShowBadge(false, string.Empty); return; }

//        if (_isBuyMode)
//        {
//            ShowBadge(false, string.Empty);
//            return;
//        }

//        string text = entry.IsMaxLevel
//            ? "MAX"
//            : $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}";

//        ShowBadge(true, text);
//    }

//    /// <summary>Shows or hides the lock overlay.</summary>
//    public void SetLocked(bool locked)
//    {
//        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
//    }

//    /// <summary>Shows or hides the selected highlight.</summary>
//    public void SetSelected(bool selected)
//    {
//        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ApplySprite(CannonData data)
//    {
//        if (_cannonImage == null || data == null) return;

//        Sprite s = data.previewSprite;
//        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//            s = data.idleSprites[0];

//        if (s != null)
//        {
//            _cannonImage.sprite = s;
//            _cannonImage.enabled = true;
//        }
//    }

//    private void SetCardName(string text)
//    {
//        if (_nameText != null) _nameText.text = text;
//    }

//    private void ShowBadge(bool visible, string text)
//    {
//        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
//        if (_badgeText != null) _badgeText.text = text;
//    }

//    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonCard
///
/// Attach this to the root of your CannonCard prefab.
/// All child references are auto-wired by name in Awake() — no Inspector
/// drag-and-drop required. Keep these exact child GameObject names:
///
///   CannonCard  (root — Button + this script)
///   ├── CannonImage      (Image component)
///   ├── CardName         (TextMeshProUGUI)
///   ├── Selected         (any GameObject — active when this card is selected)
///   ├── Locked           (any GameObject — active when type hasn't been bought yet)
///   └── UpgradeBadge     (GameObject with TextMeshProUGUI — shows "2/3" or "MAX")
///
/// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
/// </summary>
[RequireComponent(typeof(Button))]
public class CannonCard : MonoBehaviour
{
    // ── Auto-wired at runtime ──────────────────────────────────────────────────
    public Image _cannonImage;
    public TextMeshProUGUI _nameText;
    public GameObject _selectedHighlight;
    public GameObject _lockOverlay;
    public GameObject _badgeRoot;
    public TextMeshProUGUI _badgeText;
    public Button _button;

    // ── Runtime data ───────────────────────────────────────────────────────────
    private CannonData _data;
    private int _inventoryId = -1;
    private bool _isBuyMode = true;

    public CannonData Data => _data;
    public int InventoryId => _inventoryId;
    public bool IsBuyMode => _isBuyMode;

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        Transform t = transform;

        var imgT = t.Find("CannonImage");
        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

        var nameT = t.Find("CardName");
        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

        var selT = t.Find("Selected");
        if (selT != null) _selectedHighlight = selT.gameObject;

        var lockT = t.Find("Locked");
        if (lockT != null) _lockOverlay = lockT.gameObject;

        var badgeT = t.Find("UpgradeBadge");
        if (badgeT != null)
        {
            _badgeRoot = badgeT.gameObject;
            // TMP may sit directly on the badge object or on a child Text object
            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
        }

        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Used for the 3 fixed Buy-mode cards.
    /// <paramref name="locked"/> = true until the player buys this type at least once.
    /// </summary>
    public void SetupBuyCard(CannonData data, bool locked)
    {
        _data = data;
        _inventoryId = -1;
        _isBuyMode = true;

        ApplySprite(data);
        SetCardName(data.cannonName);
        SetLocked(locked);
        ShowBadge(false, string.Empty);
        SetSelected(false);
    }

    /// <summary>
    /// Used for dynamically spawned Inventory-mode cards.
    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
    /// </summary>
    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
    {
        _data = entry.data;
        _inventoryId = entry.inventoryId;
        _isBuyMode = false;

        ApplySprite(entry.data);
        SetCardName(displayName);
        SetLocked(false);          // inventory cards are always owned — never locked
        RefreshBadge(entry);
        SetSelected(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RUNTIME REFRESH
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Updates the upgrade badge. Called after an upgrade starts or completes.</summary>
    public void RefreshBadge(CannonInventoryEntry entry)
    {
        if (entry == null || _isBuyMode) { ShowBadge(false, string.Empty); return; }

        string text = entry.IsMaxLevel
            ? "MAX"
            : $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}";

        ShowBadge(true, text);
    }

    /// <summary>Shows or hides the lock overlay.</summary>
    public void SetLocked(bool locked)
    {
        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
    }

    /// <summary>Shows or hides the selection highlight.</summary>
    public void SetSelected(bool selected)
    {
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplySprite(CannonData data)
    {
        if (_cannonImage == null || data == null) return;

        Sprite s = data.previewSprite;
        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
            s = data.idleSprites[0];

        if (s != null)
        {
            _cannonImage.sprite = s;
            _cannonImage.enabled = true;
        }
    }

    private void SetCardName(string text)
    {
        if (_nameText != null) _nameText.text = text;
    }

    private void ShowBadge(bool visible, string text)
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
        if (_badgeText != null) _badgeText.text = text;
    }

    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
}