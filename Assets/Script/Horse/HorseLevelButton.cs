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

//    public HorseData Data => _data;

//    // ─── Setup: BUY mode ─────────────────────────────────────────────────────

//    public void Setup(HorseData data, HorsePanelManager manager, bool locked)
//    {
//        _data = data;
//        _manager = manager;
//        _locked = locked;
//        _bought = false;
//        _sellMode = false;

//        if (levelText != null)
//        {
//            levelText.text = $"Level {data.level}";
//            levelText.fontSize = 26;
//        }
//        if (horseThumb != null && data.idleSprites?.Length > 0)
//            horseThumb.sprite = data.idleSprites[0];

//        WireButton();
//        RefreshVisuals();
//        SetSelected(false);
//    }

//    // ─── Setup: SELL mode ────────────────────────────────────────────────────

//    public void SetupForSell(HorseData data, HorsePanelManager manager)
//    {
//        _data = data;
//        _manager = manager;
//        _locked = false;
//        _bought = false;   // false so SetSelected works purely on selection
//        _sellMode = true;

//        if (levelText != null)
//        {
//            levelText.text = data.horseName;
//            levelText.fontSize = 26;
//        }
//        if (horseThumb != null && data.idleSprites?.Length > 0)
//            horseThumb.sprite = data.idleSprites[0];

//        WireButton();
//        RefreshVisuals();
//        SetSelected(false);
//    }

//    // ─── Lock ────────────────────────────────────────────────────────────────

//    public void SetLocked(bool locked)
//    {
//        _locked = locked;
//        RefreshVisuals();
//    }

//    private void RefreshVisuals()
//    {
//        // Lock overlay — only in buy mode
//        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);

//        // 30% alpha when locked in buy mode
//        if (horseThumb != null)
//        {
//            Color c = horseThumb.color;
//            c.a = (!_sellMode && _locked) ? 1f : 1f;
//            horseThumb.color = c;
//        }

//        // All cards always clickable — locked cards just hide the buy button
//        if (cardButton != null) cardButton.interactable = true;
//    }

//    // ─── Selection ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Buy mode:  selected highlights OR permanent bought highlight.
//    /// Sell mode: only active selection highlight — no permanent state.
//    /// </summary>
//    public void SetSelected(bool selected)
//    {
//        if (selectedObject != null)
//            selectedObject.SetActive(_sellMode ? selected : (selected || _bought));
//    }

//    public void SetBought(bool bought)
//    {
//        _bought = bought;
//        if (!_sellMode && selectedObject != null && bought)
//            selectedObject.SetActive(true);
//    }

//    // ─── Click ────────────────────────────────────────────────────────────────

//    private void OnClick() => _manager?.SelectHorse(_data);

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
//            Debug.LogWarning($"[HorseLevelButton] No Button on '{gameObject.name}'. " +
//                             "Drag the horse Image Button into the Card Button field.");
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private int _sellIndex = -1;   // unique per card in sell list

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
        if (horseThumb != null && data.idleSprites?.Length > 0) horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Setup: SELL mode ────────────────────────────────────────────────────

    /// <summary>sellIndex is the position in the owned list — prevents same-data cards both highlighting.</summary>
    public void SetupForSell(HorseData data, HorsePanelManager manager, int sellIndex)
    {
        _data = data;
        _manager = manager;
        _locked = false;
        _bought = false;
        _sellMode = true;
        _sellIndex = sellIndex;

        if (levelText != null) { levelText.text = data.horseName; levelText.fontSize = 26; }
        if (horseThumb != null && data.idleSprites?.Length > 0) horseThumb.sprite = data.idleSprites[0];

        WireButton();
        RefreshVisuals();
        SetSelected(false);
    }

    // ─── Lock ────────────────────────────────────────────────────────────────

    public void SetLocked(bool locked) { _locked = locked; RefreshVisuals(); }

    private void RefreshVisuals()
    {
        if (lockObject != null) lockObject.SetActive(!_sellMode && _locked);
        if (horseThumb != null)
        {
            Color c = horseThumb.color;
            c.a = (!_sellMode && _locked) ? 0.3f : 1f;
            horseThumb.color = c;
        }
        if (cardButton != null) cardButton.interactable = true;
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    /// <summary>Buy/Update mode: selected or permanently bought.</summary>
    public void SetSelected(bool selected)
    {
        if (selectedObject != null)
            selectedObject.SetActive(_sellMode ? false : (selected || _bought));
    }

    /// <summary>Sell mode: matched by index only — prevents two same-horse cards both lighting up.</summary>
    public void SetSelectedBySellIndex(bool selected)
    {
        if (selectedObject != null) selectedObject.SetActive(selected);
    }

    public void SetBought(bool bought)
    {
        _bought = bought;
        if (!_sellMode && selectedObject != null && bought) selectedObject.SetActive(true);
    }

    // ─── Click ────────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (_sellMode) _manager?.SelectHorseForSell(_data, _sellIndex);
        else _manager?.SelectHorse(_data);
    }

    private void WireButton()
    {
        if (cardButton == null) cardButton = GetComponentInChildren<Button>(true);
        if (cardButton != null) { cardButton.onClick.RemoveAllListeners(); cardButton.onClick.AddListener(OnClick); }
        else Debug.LogWarning($"[HorseLevelButton] No Button on '{gameObject.name}'.");
    }
}