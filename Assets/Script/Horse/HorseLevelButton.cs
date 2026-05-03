//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class HorseLevelButton : MonoBehaviour
//{
//    [Header("Wire from Hierarchy")]
//    [SerializeField] private TextMeshProUGUI levelText;
//    [SerializeField] private Image horseThumb;
//    [Tooltip("Button on the horse Image child")]
//    [SerializeField] private Button cardButton;
//    [SerializeField] private GameObject lockObject;
//    [SerializeField] private GameObject selectedObject;

//    private HorseData _data;
//    private HorsePanelManager _manager;
//    private bool _locked = false;
//    private bool _bought = false;
//    private bool _sellMode = false;
//    private int _sellIndex = -1;

//    public HorseData Data => _data;
//    public int SellIndex => _sellIndex;

//    // ─── Setup: BUY mode ─────────────────────────────────────────────────────

//    public void Setup(HorseData data, HorsePanelManager manager, bool locked)
//    {
//        _data = data;
//        _manager = manager;
//        _locked = locked;
//        _bought = false;
//        _sellMode = false;
//        _sellIndex = -1;

//        if (levelText != null) { levelText.text = $"Level {data.level}"; levelText.fontSize = 26; }
//        if (horseThumb != null && data.idleSprites?.Length > 0)
//            horseThumb.sprite = data.idleSprites[0];

//        WireButton();
//        RefreshVisuals();
//        SetSelected(false);
//    }

//    // ─── Setup: SELL / UPDATE mode ───────────────────────────────────────────

//    /// <summary>
//    /// sellIndex is the position in the owned list — prevents two cards with the
//    /// same HorseData from both highlighting when one is selected.
//    /// Also used by Update mode cards (manager routes to SelectHorseForSell).
//    /// </summary>
//    public void SetupForSell(HorseData data, HorsePanelManager manager, int sellIndex)
//    {
//        _data = data;
//        _manager = manager;
//        _locked = false;
//        _bought = false;
//        _sellMode = true;
//        _sellIndex = sellIndex;

//        if (levelText != null) { levelText.text = data.horseName; levelText.fontSize = 26; }
//        if (horseThumb != null && data.idleSprites?.Length > 0)
//            horseThumb.sprite = data.idleSprites[0];

//        WireButton();
//        RefreshVisuals();
//        SetSelected(false);
//    }

//    // ─── Lock ────────────────────────────────────────────────────────────────

//    public void SetLocked(bool locked) { _locked = locked; RefreshVisuals(); }

//    private void RefreshVisuals()
//    {
//        // Lock overlay — only in buy mode
//        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);

//        // 30 % alpha when locked in buy mode
//        if (horseThumb != null)
//        {
//            Color c = horseThumb.color;
//            c.a = (!_sellMode && _locked) ? 0.3f : 1f;
//            horseThumb.color = c;
//        }

//        if (cardButton != null) cardButton.interactable = true;
//    }

//    // ─── Selection ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Buy mode: highlights only when actually selected (current tap).
//    ///
//    /// BUG 3 FIX: removed "|| _bought" — the old code permanently locked the
//    /// selectedObject ON once a horse was bought, so clicking any other card
//    /// would highlight it but the bought card would stay highlighted too.
//    /// Now only one card is highlighted at a time.
//    /// </summary>
//    public void SetSelected(bool selected)
//    {
//        if (selectedObject != null)
//            selectedObject.SetActive(_sellMode ? false : selected);
//    }

//    /// <summary>Sell / Update mode: matched by unique sell index only.</summary>
//    public void SetSelectedBySellIndex(bool selected)
//    {
//        if (selectedObject != null) selectedObject.SetActive(selected);
//    }

//    /// <summary>
//    /// Marks the card as bought (e.g. to dim the buy button, show a badge, etc.).
//    /// Does NOT affect the selection highlight — that is controlled by SetSelected only.
//    /// </summary>
//    public void SetBought(bool bought)
//    {
//        _bought = bought;
//        // No longer sets selectedObject — see BUG 3 FIX note above.
//        // If you need a permanent "owned" visual, add a separate boughtObject field.
//    }

//    // ─── Click ────────────────────────────────────────────────────────────────

//    private void OnClick()
//    {
//        if (_sellMode) _manager?.SelectHorseForSell(_data, _sellIndex);
//        else _manager?.SelectHorse(_data);
//    }

//    private void WireButton()
//    {
//        if (cardButton == null)
//            cardButton = GetComponentInChildren<Button>(true);

//        if (cardButton != null)
//        {
//            cardButton.onClick.RemoveAllListeners();
//            cardButton.onClick.AddListener(OnClick);
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseLevelButton] No Button found on '{gameObject.name}'.");
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HorseLevelButton
///
/// Used for both Buy mode cards and Inventory mode cards.
///
/// INVENTORY BADGE  (new):
///   SetupForInventory() now accepts typeIndex and typeTotal so the card can
///   display "(1/2)", "(2/2)", etc., telling the player which copy of a
///   multi-owned horse they are looking at.
///   e.g.  "Brown Horse (1/2)"  and  "Brown Horse (2/2)"
/// </summary>
public class HorseLevelButton : MonoBehaviour
{
    [Header("Wire from Hierarchy")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image horseThumb;
    [Tooltip("Button on the horse Image child")]
    [SerializeField] private Button cardButton;
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject selectedObject;

    [Header("(Optional) Separate badge label for the N/total counter")]
    [Tooltip("If wired, the (N/total) counter is shown here instead of appended to levelText")]
    [SerializeField] private TextMeshProUGUI countBadgeText;

    private HorseData _data;
    private HorsePanelManager _manager;
    private bool _locked = false;
    private bool _bought = false;
    private bool _inventoryMode = false;
    private int _sellIndex = -1;

    public HorseData Data => _data;
    public int SellIndex => _sellIndex;

    // ─── Setup: BUY mode ─────────────────────────────────────────────────────

    public void Setup(HorseData data, HorsePanelManager manager, bool locked)
    {
        _data = data;
        _manager = manager;
        _locked = locked;
        _bought = false;
        _inventoryMode = false;
        _sellIndex = -1;

        if (levelText != null)
        {
            levelText.text = $"Level {data.level}";
            levelText.fontSize = 26;
        }

        if (countBadgeText != null)
            countBadgeText.gameObject.SetActive(false);

        if (horseThumb != null && data.idleSprites?.Length > 0)
            horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Setup: INVENTORY mode ───────────────────────────────────────────────

    /// <summary>
    /// Shows this owned horse in Inventory mode.
    ///
    /// sellIndex  – position in the owned list (unique per card, even for duplicates).
    /// typeIndex  – 1-based position among same-type horses  (e.g. 1, 2 …).
    /// typeTotal  – total copies of this horse type owned    (e.g. 2).
    ///
    /// The card will display:  "Brown Horse (1/2)"
    /// If a separate countBadgeText label is wired, the badge goes there and
    /// the levelText shows only the horse name.
    /// </summary>
    public void SetupForInventory(HorseData data, HorsePanelManager manager,
                                  int sellIndex, int typeIndex, int typeTotal)
    {
        _data = data;
        _manager = manager;
        _locked = false;
        _bought = false;
        _inventoryMode = true;
        _sellIndex = sellIndex;

        string badge = $"({typeIndex}/{typeTotal})";

        if (countBadgeText != null)
        {
            // Separate label — cleaner layout
            levelText.text = data.horseName;
            levelText.fontSize = 26;
            countBadgeText.text = badge;
            countBadgeText.gameObject.SetActive(true);
        }
        else
        {
            // Fold badge into levelText
            if (levelText != null)
            {
                levelText.text = $"{data.horseName} {badge}";
                levelText.fontSize = 22;   // slightly smaller to fit
            }
        }

        if (horseThumb != null && data.idleSprites?.Length > 0)
            horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Backward-compat alias (kept so existing callers don't break) ─────────

    /// <summary>
    /// Legacy alias — calls SetupForInventory with typeIndex = sellIndex+1
    /// and typeTotal = 1.  Prefer calling SetupForInventory directly so the
    /// correct (N/total) values are passed.
    /// </summary>
    public void SetupForSell(HorseData data, HorsePanelManager manager, int sellIndex)
        => SetupForInventory(data, manager, sellIndex, sellIndex + 1, 1);

    // ─── Lock ────────────────────────────────────────────────────────────────

    public void SetLocked(bool locked) { _locked = locked; RefreshVisuals(); }

    private void RefreshVisuals()
    {
        // Lock overlay — only in buy mode
        if (lockObject != null) lockObject.SetActive(!_inventoryMode && _locked);

        // 30 % alpha when locked in buy mode
        if (horseThumb != null)
        {
            Color c = horseThumb.color;
            c.a = (!_inventoryMode && _locked) ? 0.3f : 1f;
            horseThumb.color = c;
        }

        if (cardButton != null) cardButton.interactable = true;
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    public void SetSelected(bool selected)
    {
        if (selectedObject != null)
            selectedObject.SetActive(_inventoryMode ? false : selected);
    }

    public void SetSelectedBySellIndex(bool selected)
    {
        if (selectedObject != null) selectedObject.SetActive(selected);
    }

    public void SetBought(bool bought)
    {
        _bought = bought;
        // SetBought does NOT affect the selection highlight.
        // Add a separate boughtObject field here if you need an "owned" badge in buy mode.
    }

    // ─── Click ────────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (_inventoryMode) _manager?.SelectHorseForSell(_data, _sellIndex);
        else _manager?.SelectHorse(_data);
    }

    private void WireButton()
    {
        if (cardButton == null)
            cardButton = GetComponentInChildren<Button>(true);

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning($"[HorseLevelButton] No Button found on '{gameObject.name}'.");
        }
    }
}