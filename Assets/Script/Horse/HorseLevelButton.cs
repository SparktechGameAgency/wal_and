//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

//////public class HorseLevelButton : MonoBehaviour
//////{
//////    [Header("Wire from Hierarchy")]
//////    [SerializeField] private TextMeshProUGUI levelText;
//////    [SerializeField] private Image horseThumb;
//////    [Tooltip("Button on the horse Image child")]
//////    [SerializeField] private Button cardButton;
//////    [SerializeField] private GameObject lockObject;
//////    [SerializeField] private GameObject selectedObject;

//////    private HorseData _data;
//////    private HorsePanelManager _manager;
//////    private bool _locked = false;
//////    private bool _bought = false;
//////    private bool _sellMode = false;

//////    public HorseData Data => _data;

//////    // ─── Setup: BUY mode ─────────────────────────────────────────────────────

//////    public void Setup(HorseData data, HorsePanelManager manager, bool locked)
//////    {
//////        _data = data;
//////        _manager = manager;
//////        _locked = locked;
//////        _bought = false;
//////        _sellMode = false;

//////        if (levelText != null)
//////        {
//////            levelText.text = $"Level {data.level}";
//////            levelText.fontSize = 26;
//////        }
//////        if (horseThumb != null && data.idleSprites?.Length > 0)
//////            horseThumb.sprite = data.idleSprites[0];

//////        WireButton();
//////        RefreshVisuals();
//////        SetSelected(false);
//////    }

//////    // ─── Setup: SELL mode ────────────────────────────────────────────────────

//////    public void SetupForSell(HorseData data, HorsePanelManager manager)
//////    {
//////        _data = data;
//////        _manager = manager;
//////        _locked = false;
//////        _bought = false;   // false so SetSelected works purely on selection
//////        _sellMode = true;

//////        if (levelText != null)
//////        {
//////            levelText.text = data.horseName;
//////            levelText.fontSize = 26;
//////        }
//////        if (horseThumb != null && data.idleSprites?.Length > 0)
//////            horseThumb.sprite = data.idleSprites[0];

//////        WireButton();
//////        RefreshVisuals();
//////        SetSelected(false);
//////    }

//////    // ─── Lock ────────────────────────────────────────────────────────────────

//////    public void SetLocked(bool locked)
//////    {
//////        _locked = locked;
//////        RefreshVisuals();
//////    }

//////    private void RefreshVisuals()
//////    {
//////        // Lock overlay — only in buy mode
//////        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);

//////        // 30% alpha when locked in buy mode
//////        if (horseThumb != null)
//////        {
//////            Color c = horseThumb.color;
//////            c.a = (!_sellMode && _locked) ? 1f : 1f;
//////            horseThumb.color = c;
//////        }

//////        // All cards always clickable — locked cards just hide the buy button
//////        if (cardButton != null) cardButton.interactable = true;
//////    }

//////    // ─── Selection ────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Buy mode:  selected highlights OR permanent bought highlight.
//////    /// Sell mode: only active selection highlight — no permanent state.
//////    /// </summary>
//////    public void SetSelected(bool selected)
//////    {
//////        if (selectedObject != null)
//////            selectedObject.SetActive(_sellMode ? selected : (selected || _bought));
//////    }

//////    public void SetBought(bool bought)
//////    {
//////        _bought = bought;
//////        if (!_sellMode && selectedObject != null && bought)
//////            selectedObject.SetActive(true);
//////    }

//////    // ─── Click ────────────────────────────────────────────────────────────────

//////    private void OnClick() => _manager?.SelectHorse(_data);

//////    private void WireButton()
//////    {
//////        if (cardButton == null)
//////            cardButton = GetComponentInChildren<Button>(true);

//////        if (cardButton != null)
//////        {
//////            cardButton.onClick.RemoveAllListeners();
//////            cardButton.onClick.AddListener(OnClick);
//////        }
//////        else
//////        {
//////            Debug.LogWarning($"[HorseLevelButton] No Button on '{gameObject.name}'. " +
//////                             "Drag the horse Image Button into the Card Button field.");
//////        }
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

////public class HorseLevelButton : MonoBehaviour
////{
////    [Header("Wire from Hierarchy")]
////    [SerializeField] private TextMeshProUGUI levelText;
////    [SerializeField] private Image horseThumb;
////    [Tooltip("Button on the horse Image child")]
////    [SerializeField] private Button cardButton;
////    [SerializeField] private GameObject lockObject;
////    [SerializeField] private GameObject selectedObject;

////    private HorseData _data;
////    private HorsePanelManager _manager;
////    private bool _locked = false;
////    private bool _bought = false;
////    private bool _sellMode = false;
////    private int _sellIndex = -1;   // unique per card in sell list

////    public HorseData Data => _data;
////    public int SellIndex => _sellIndex;

////    // ─── Setup: BUY mode ─────────────────────────────────────────────────────

////    public void Setup(HorseData data, HorsePanelManager manager, bool locked)
////    {
////        _data = data;
////        _manager = manager;
////        _locked = locked;
////        _bought = false;
////        _sellMode = false;
////        _sellIndex = -1;

////        if (levelText != null) { levelText.text = $"Level {data.level}"; levelText.fontSize = 26; }
////        if (horseThumb != null && data.idleSprites?.Length > 0) horseThumb.sprite = data.idleSprites[0];

////        WireButton();
////        RefreshVisuals();
////        SetSelected(false);
////    }

////    // ─── Setup: SELL mode ────────────────────────────────────────────────────

////    /// <summary>sellIndex is the position in the owned list — prevents same-data cards both highlighting.</summary>
////    public void SetupForSell(HorseData data, HorsePanelManager manager, int sellIndex)
////    {
////        _data = data;
////        _manager = manager;
////        _locked = false;
////        _bought = false;
////        _sellMode = true;
////        _sellIndex = sellIndex;

////        if (levelText != null) { levelText.text = data.horseName; levelText.fontSize = 26; }
////        if (horseThumb != null && data.idleSprites?.Length > 0) horseThumb.sprite = data.idleSprites[0];

////        WireButton();
////        RefreshVisuals();
////        SetSelected(false);
////    }

////    // ─── Lock ────────────────────────────────────────────────────────────────

////    public void SetLocked(bool locked) { _locked = locked; RefreshVisuals(); }

////    private void RefreshVisuals()
////    {
////        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);
////        if (horseThumb != null)
////        {
////            Color c = horseThumb.color;
////            c.a = (!_sellMode && _locked) ? 0.3f : 1f;
////            horseThumb.color = c;
////        }
////        if (cardButton != null) cardButton.interactable = true;
////    }

////    // ─── Selection ────────────────────────────────────────────────────────────

////    /// <summary>Buy/Update mode: selected or permanently bought.</summary>
////    public void SetSelected(bool selected)
////    {
////        if (selectedObject != null)
////            selectedObject.SetActive(_sellMode ? false : (selected || _bought));
////    }

////    /// <summary>Sell mode: matched by index only — prevents two same-horse cards both lighting up.</summary>
////    public void SetSelectedBySellIndex(bool selected)
////    {
////        if (selectedObject != null) selectedObject.SetActive(selected);
////    }

////    public void SetBought(bool bought)
////    {
////        _bought = bought;
////        if (!_sellMode && selectedObject != null && bought) selectedObject.SetActive(true);
////    }

////    // ─── Click ────────────────────────────────────────────────────────────────

////    private void OnClick()
////    {
////        if (_sellMode) _manager?.SelectHorseForSell(_data, _sellIndex);
////        else _manager?.SelectHorse(_data);
////    }

////    private void WireButton()
////    {
////        if (cardButton == null) cardButton = GetComponentInChildren<Button>(true);
////        if (cardButton != null) { cardButton.onClick.RemoveAllListeners(); cardButton.onClick.AddListener(OnClick); }
////        else Debug.LogWarning($"[HorseLevelButton] No Button on '{gameObject.name}'.");
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// HorseLevelButton — one card in the horse selection grid.
/////
///// Buy mode:  clicking calls manager.SelectHorse(data)
///// Sell mode: clicking calls manager.SelectHorseForSell(data, sellIndex)
/////            sellIndex prevents two cards with the same HorseData from
/////            both highlighting when one is selected.
///// </summary>
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

//    // ─── Setup: SELL mode ────────────────────────────────────────────────────

//    /// <summary>sellIndex is the position in the owned list — prevents same-data cards
//    /// both highlighting when one is selected.</summary>
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

//    /// <summary>Buy mode: selected OR permanently bought highlight.</summary>
//    public void SetSelected(bool selected)
//    {
//        if (selectedObject != null)
//            selectedObject.SetActive(_sellMode ? false : (selected || _bought));
//    }

//    /// <summary>Sell mode: matched by unique sell index only.</summary>
//    public void SetSelectedBySellIndex(bool selected)
//    {
//        if (selectedObject != null) selectedObject.SetActive(selected);
//    }

//    public void SetBought(bool bought)
//    {
//        _bought = bought;
//        if (!_sellMode && selectedObject != null && bought) selectedObject.SetActive(true);
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
/// HorseLevelButton — one card in the horse selection grid.
///
/// Buy mode:    clicking calls manager.SelectHorse(data)
/// Sell mode:   clicking calls manager.SelectHorseForSell(data, sellIndex)
/// Update mode: cards are set up with SetupForSell, so clicking also calls
///              manager.SelectHorseForSell(data, sellIndex) — the manager
///              switches the upgrade target to that horse's slot.
///
/// BUG 3 FIX — bought horse stays highlighted after selecting another card:
///   SetSelected used to do selectedObject.SetActive(selected || _bought).
///   Once _bought was true the highlight could never be cleared by selecting
///   a different card.  Now SetSelected reflects ONLY the current tap.
///   SetBought no longer touches selectedObject at all.
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

    private HorseData _data;
    private HorsePanelManager _manager;
    private bool _locked = false;
    private bool _bought = false;
    private bool _sellMode = false;
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
        _sellMode = false;
        _sellIndex = -1;

        if (levelText != null) { levelText.text = $"Level {data.level}"; levelText.fontSize = 26; }
        if (horseThumb != null && data.idleSprites?.Length > 0)
            horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Setup: SELL / UPDATE mode ───────────────────────────────────────────

    /// <summary>
    /// sellIndex is the position in the owned list — prevents two cards with the
    /// same HorseData from both highlighting when one is selected.
    /// Also used by Update mode cards (manager routes to SelectHorseForSell).
    /// </summary>
    public void SetupForSell(HorseData data, HorsePanelManager manager, int sellIndex)
    {
        _data = data;
        _manager = manager;
        _locked = false;
        _bought = false;
        _sellMode = true;
        _sellIndex = sellIndex;

        if (levelText != null) { levelText.text = data.horseName; levelText.fontSize = 26; }
        if (horseThumb != null && data.idleSprites?.Length > 0)
            horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Lock ────────────────────────────────────────────────────────────────

    public void SetLocked(bool locked) { _locked = locked; RefreshVisuals(); }

    private void RefreshVisuals()
    {
        // Lock overlay — only in buy mode
        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);

        // 30 % alpha when locked in buy mode
        if (horseThumb != null)
        {
            Color c = horseThumb.color;
            c.a = (!_sellMode && _locked) ? 0.3f : 1f;
            horseThumb.color = c;
        }

        if (cardButton != null) cardButton.interactable = true;
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Buy mode: highlights only when actually selected (current tap).
    ///
    /// BUG 3 FIX: removed "|| _bought" — the old code permanently locked the
    /// selectedObject ON once a horse was bought, so clicking any other card
    /// would highlight it but the bought card would stay highlighted too.
    /// Now only one card is highlighted at a time.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedObject != null)
            selectedObject.SetActive(_sellMode ? false : selected);
    }

    /// <summary>Sell / Update mode: matched by unique sell index only.</summary>
    public void SetSelectedBySellIndex(bool selected)
    {
        if (selectedObject != null) selectedObject.SetActive(selected);
    }

    /// <summary>
    /// Marks the card as bought (e.g. to dim the buy button, show a badge, etc.).
    /// Does NOT affect the selection highlight — that is controlled by SetSelected only.
    /// </summary>
    public void SetBought(bool bought)
    {
        _bought = bought;
        // No longer sets selectedObject — see BUG 3 FIX note above.
        // If you need a permanent "owned" visual, add a separate boughtObject field.
    }

    // ─── Click ────────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (_sellMode) _manager?.SelectHorseForSell(_data, _sellIndex);
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