////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// ArcherZoneCastle
///////
/////// Attach to the "ArcherZone" child of CastleBlockUnitSlot — mirrors CannonSlotCastle.
///////
/////// CLICK  → opens the Army/Soldier panel (GameManager.OpenArmyPanel).
/////// DROP   → soldier dragged onto this zone becomes an ArcherUnit prefab.
///////
/////// Required child hierarchy:
///////   ArcherZone              ← this script + Image + Button + CanvasGroup
///////   ├── EmptySlotZone       shown while empty and Archer tab is active
///////   ├── Highlight           glow shown during valid drag hover
///////   ├── Spawnpoint          where the ArcherUnit prefab spawns
///////   └── RemoveButton (opt)  Button to remove the stationed archer
/////// </summary>
////[RequireComponent(typeof(Button))]
////[RequireComponent(typeof(Image))]
////public class ArcherZoneCastle : MonoBehaviour,
////    IDropHandler,
////    IPointerEnterHandler,
////    IPointerExitHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────

////    [Header("Prefab")]
////    [Tooltip("Prefab with an ArcherUnit component. Spawned when a soldier is dropped here.")]
////    [SerializeField] private GameObject archerPrefab;

////    [Header("Colors")]
////    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////    [SerializeField] private Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////    [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);
////    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.10f);

////    // ── Auto-wired children ───────────────────────────────────────

////    private Button _button;
////    private Button _removeButton;
////    private Image _bg;
////    private GameObject _emptySlotZone;
////    private GameObject _highlight;
////    private Transform _spawnpoint;

////    // ── State ─────────────────────────────────────────────────────

////    public bool IsOccupied { get; private set; }

////    private GameObject _archerInstance;
////    private SoldierDragDrop _stationedSoldier;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _button = GetComponent<Button>();
////        _button.onClick.AddListener(OnArcherZoneClicked);

////        _bg = GetComponent<Image>();
////        _bg.color = normalColor;
////        _bg.raycastTarget = true;

////        _emptySlotZone = transform.Find("EmptySlotZone")?.gameObject;
////        _highlight = transform.Find("Highlight")?.gameObject;
////        _spawnpoint = transform.Find("Spawnpoint") != null
////                         ? transform.Find("Spawnpoint")
////                         : transform;

////        var removeBtnT = transform.Find("RemoveButton");
////        if (removeBtnT != null)
////        {
////            _removeButton = removeBtnT.GetComponent<Button>();
////            _removeButton?.onClick.AddListener(RemoveArcher);
////        }

////        CanvasGroup cg = GetComponent<CanvasGroup>();
////        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

////        EnsureRaycastPassthrough();
////        RefreshVisuals();

////        // Hidden by default — shown only when Archer tab is active.
////        gameObject.SetActive(false);
////    }

////    // ── Click handler ─────────────────────────────────────────────

////    private void OnArcherZoneClicked()
////    {
////        Debug.Log("[ArcherZoneCastle] Clicked — opening Army panel.");
////        GameManager.Instance?.OpenArmyPanel();
////    }

////    // ── IDropHandler ──────────────────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        _highlight?.SetActive(false);

////        if (IsOccupied)
////        {
////            Debug.Log("[ArcherZoneCastle] Already occupied.");
////            ResetColor();
////            return;
////        }

////        SoldierDragDrop soldier = SoldierDragDrop.CurrentlyDragging;
////        if (soldier == null)
////        {
////            Debug.Log("[ArcherZoneCastle] OnDrop — no soldier being dragged.");
////            ResetColor();
////            return;
////        }

////        PlaceArcher(soldier);
////    }

////    // ── IPointerEnterHandler / IPointerExitHandler ────────────────

////    public void OnPointerEnter(PointerEventData eventData)
////    {
////        if (IsOccupied) return;
////        _highlight?.SetActive(true);
////        bool validDrag = SoldierDragDrop.CurrentlyDragging != null;
////        _bg.color = validDrag ? hoverValidColor : hoverInvalidColor;
////    }

////    public void OnPointerExit(PointerEventData eventData)
////    {
////        _highlight?.SetActive(false);
////        ResetColor();
////    }

////    // ── Public API ────────────────────────────────────────────────

////    public void PlaceArcher(SoldierDragDrop soldier)
////    {
////        if (IsOccupied) return;

////        if (archerPrefab == null)
////        {
////            Debug.LogError("[ArcherZoneCastle] archerPrefab is not assigned!", this);
////            return;
////        }

////        _archerInstance = Instantiate(archerPrefab, _spawnpoint.position,
////                                      Quaternion.identity, _spawnpoint);

////        RectTransform rt = _archerInstance.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            rt.anchoredPosition = Vector2.zero;
////            rt.localScale = Vector3.one;
////        }

////        ArcherUnit archerUnit = _archerInstance.GetComponent<ArcherUnit>();
////        if (archerUnit != null) archerUnit.Init(null);

////        soldier.BecomeArcher(this);

////        _stationedSoldier = soldier;
////        IsOccupied = true;

////        RefreshVisuals();
////        Debug.Log($"[ArcherZoneCastle] Archer placed at {gameObject.name}.");
////    }

////    public void RemoveArcher()
////    {
////        if (!IsOccupied) return;

////        if (_archerInstance != null) { Destroy(_archerInstance); _archerInstance = null; }

////        _stationedSoldier?.ReturnFromArcher();
////        _stationedSoldier = null;

////        IsOccupied = false;
////        RefreshVisuals();
////        Debug.Log($"[ArcherZoneCastle] Archer removed from {gameObject.name}.");
////    }

////    // ── Tab visibility ────────────────────────────────────────────

////    public void SetArcherTabActive(bool active)
////    {
////        gameObject.SetActive(active);
////        if (active) RefreshVisuals();
////    }

////    public static void SetArcherZonesVisible(bool visible)
////    {
////        foreach (var zone in FindObjectsByType<ArcherZoneCastle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
////            zone.SetArcherTabActive(visible);
////    }

////    // ── Helpers ───────────────────────────────────────────────────

////    private void RefreshVisuals()
////    {
////        _emptySlotZone?.SetActive(!IsOccupied);
////        _highlight?.SetActive(false);
////        if (_removeButton != null) _removeButton.gameObject.SetActive(IsOccupied);
////        ResetColor();
////    }

////    private void ResetColor()
////    {
////        if (_bg != null) _bg.color = IsOccupied ? occupiedColor : normalColor;
////    }

////    private void EnsureRaycastPassthrough()
////    {
////        Transform t = transform.parent;
////        while (t != null)
////        {
////            Image img = t.GetComponent<Image>();
////            if (img != null && img.color.a == 0f)
////                img.raycastTarget = false;
////            t = t.parent;
////        }
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// ArcherZoneCastle
/////
///// Attach to the "ArcherZone" child of CastleBlockUnitSlot — mirrors CannonSlotCastle.
/////
///// CLICK  → opens the Army/Soldier panel (GameManager.OpenArmyPanel).
///// DROP   → soldier dragged onto this zone becomes an ArcherUnit prefab.
/////
///// Required child hierarchy:
/////   ArcherZone              ← this script + Image + Button + CanvasGroup
/////   ├── EmptySlotZone       shown while empty and Archer tab is active
/////   ├── Highlight           glow shown during valid drag hover
/////   ├── Spawnpoint          where the ArcherUnit prefab spawns
/////   └── RemoveButton (opt)  Button to remove the stationed archer
///// </summary>
//[RequireComponent(typeof(Button))]
//[RequireComponent(typeof(Image))]
//public class ArcherZoneCastle : MonoBehaviour,
//    IDropHandler,
//    IPointerEnterHandler,
//    IPointerExitHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────

//    [Header("Prefab")]
//    [Tooltip("Prefab with an ArcherUnit component. Spawned when a soldier is dropped here.")]
//    [SerializeField] private GameObject archerPrefab;

//    [Header("Colors")]
//    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//    [SerializeField] private Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//    [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);
//    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.10f);

//    // ── Auto-wired children ───────────────────────────────────────

//    private Button _button;
//    private Button _removeButton;
//    private Image _bg;
//    private GameObject _emptySlotZone;
//    private GameObject _highlight;
//    private Transform _spawnpoint;
//    private CastleBlockUnitSlot _parentSlot;

//    // ── State ─────────────────────────────────────────────────────

//    public bool IsOccupied { get; private set; }

//    private GameObject _archerInstance;
//    private SoldierDragDrop _stationedSoldier;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnArcherZoneClicked);

//        _bg = GetComponent<Image>();
//        _bg.color = normalColor;
//        _bg.raycastTarget = true;

//        _parentSlot = GetComponentInParent<CastleBlockUnitSlot>();

//        _emptySlotZone = transform.Find("EmptySlotZone")?.gameObject;
//        _highlight = transform.Find("Highlight")?.gameObject;
//        _spawnpoint = transform.Find("Spawnpoint") != null
//                         ? transform.Find("Spawnpoint")
//                         : transform;

//        var removeBtnT = transform.Find("RemoveButton");
//        if (removeBtnT != null)
//        {
//            _removeButton = removeBtnT.GetComponent<Button>();
//            _removeButton?.onClick.AddListener(RemoveArcher);
//        }

//        CanvasGroup cg = GetComponent<CanvasGroup>();
//        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

//        EnsureRaycastPassthrough();
//        RefreshVisuals();

//        // Hidden by default — shown only when Archer tab is active.
//        gameObject.SetActive(false);
//    }

//    // ── Click handler ─────────────────────────────────────────────

//    private void OnArcherZoneClicked()
//    {
//        Debug.Log("[ArcherZoneCastle] Clicked — opening Army panel.");
//        GameManager.Instance?.OpenArmyPanel();
//    }

//    // ── IDropHandler ──────────────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);

//        if (IsOccupied)
//        {
//            Debug.Log("[ArcherZoneCastle] Already occupied.");
//            ResetColor();
//            return;
//        }

//        if (_parentSlot != null && _parentSlot.IsBlockedByCannon)
//        {
//            Debug.Log("[ArcherZoneCastle] Blocked — a cannon is already placed on this block.");
//            ResetColor();
//            return;
//        }

//        SoldierDragDrop soldier = SoldierDragDrop.CurrentlyDragging;
//        if (soldier == null)
//        {
//            Debug.Log("[ArcherZoneCastle] OnDrop — no soldier being dragged.");
//            ResetColor();
//            return;
//        }

//        PlaceArcher(soldier);
//    }

//    // ── IPointerEnterHandler / IPointerExitHandler ────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (IsOccupied) return;
//        _highlight?.SetActive(true);
//        bool blockedByCannon = _parentSlot != null && _parentSlot.IsBlockedByCannon;
//        bool validDrag = SoldierDragDrop.CurrentlyDragging != null && !blockedByCannon;
//        _bg.color = validDrag ? hoverValidColor : hoverInvalidColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);
//        ResetColor();
//    }

//    // ── Public API ────────────────────────────────────────────────

//    public void PlaceArcher(SoldierDragDrop soldier)
//    {
//        if (IsOccupied) return;

//        if (_parentSlot != null && _parentSlot.IsBlockedByCannon)
//        {
//            Debug.LogWarning("[ArcherZoneCastle] Cannot place archer — cannon already occupies this block.");
//            return;
//        }

//        if (archerPrefab == null)
//        {
//            Debug.LogError("[ArcherZoneCastle] archerPrefab is not assigned!", this);
//            return;
//        }

//        _archerInstance = Instantiate(archerPrefab, _spawnpoint.position,
//                                      Quaternion.identity, _spawnpoint);

//        RectTransform rt = _archerInstance.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale = Vector3.one;
//        }

//        ArcherUnit archerUnit = _archerInstance.GetComponent<ArcherUnit>();
//        if (archerUnit != null) archerUnit.Init(null);

//        soldier.BecomeArcher(this);

//        _stationedSoldier = soldier;
//        IsOccupied = true;

//        RefreshVisuals();
//        Debug.Log($"[ArcherZoneCastle] Archer placed at {gameObject.name}.");
//    }

//    public void RemoveArcher()
//    {
//        if (!IsOccupied) return;

//        if (_archerInstance != null) { Destroy(_archerInstance); _archerInstance = null; }

//        _stationedSoldier?.ReturnFromArcher();
//        _stationedSoldier = null;

//        IsOccupied = false;
//        RefreshVisuals();
//        Debug.Log($"[ArcherZoneCastle] Archer removed from {gameObject.name}.");
//    }

//    // ── Tab visibility ────────────────────────────────────────────

//    public void SetArcherTabActive(bool active)
//    {
//        gameObject.SetActive(active);
//        if (active) RefreshVisuals();
//    }

//    public static void SetArcherZonesVisible(bool visible)
//    {
//        foreach (var zone in FindObjectsByType<ArcherZoneCastle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
//            zone.SetArcherTabActive(visible);
//    }

//    // ── Helpers ───────────────────────────────────────────────────

//    private void RefreshVisuals()
//    {
//        _emptySlotZone?.SetActive(!IsOccupied);
//        _highlight?.SetActive(false);
//        if (_removeButton != null) _removeButton.gameObject.SetActive(IsOccupied);
//        ResetColor();
//    }

//    private void ResetColor()
//    {
//        if (_bg != null) _bg.color = IsOccupied ? occupiedColor : normalColor;
//    }

//    private void EnsureRaycastPassthrough()
//    {
//        Transform t = transform.parent;
//        while (t != null)
//        {
//            Image img = t.GetComponent<Image>();
//            if (img != null && img.color.a == 0f)
//                img.raycastTarget = false;
//            t = t.parent;
//        }
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// ArcherZoneCastle
/////
///// Attach to the "ArcherZone" child of CastleBlockUnitSlot — mirrors CannonSlotCastle.
/////
///// CLICK  → opens the Army/Soldier panel (GameManager.OpenArmyPanel).
///// DROP   → soldier dragged onto this zone becomes an ArcherUnit prefab.
/////
///// Required child hierarchy:
/////   ArcherZone              ← this script + Image + Button + CanvasGroup
/////   ├── EmptySlotZone       shown while empty and Archer tab is active
/////   ├── Highlight           glow shown during valid drag hover
/////   ├── Spawnpoint          where the ArcherUnit prefab spawns
/////   └── RemoveButton (opt)  Button to remove the stationed archer
///// </summary>
//[RequireComponent(typeof(Button))]
//[RequireComponent(typeof(Image))]
//public class ArcherZoneCastle : MonoBehaviour,
//    IDropHandler,
//    IPointerEnterHandler,
//    IPointerExitHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────

//    [Header("Prefab")]
//    [Tooltip("Prefab with an ArcherUnit component. Spawned when a soldier is dropped here.")]
//    [SerializeField] private GameObject archerPrefab;

//    [Header("Colors")]
//    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//    [SerializeField] private Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//    [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);
//    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.10f);

//    // ── Auto-wired children ───────────────────────────────────────

//    private Button _button;
//    private Button _removeButton;
//    private Image _bg;
//    private GameObject _emptySlotZone;
//    private GameObject _highlight;
//    private Transform _spawnpoint;

//    // ── State ─────────────────────────────────────────────────────

//    public bool IsOccupied { get; private set; }

//    private GameObject _archerInstance;
//    private SoldierDragDrop _stationedSoldier;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnArcherZoneClicked);

//        _bg = GetComponent<Image>();
//        _bg.color = normalColor;
//        _bg.raycastTarget = true;

//        _emptySlotZone = transform.Find("EmptySlotZone")?.gameObject;
//        _highlight = transform.Find("Highlight")?.gameObject;
//        _spawnpoint = transform.Find("Spawnpoint") != null
//                         ? transform.Find("Spawnpoint")
//                         : transform;

//        var removeBtnT = transform.Find("RemoveButton");
//        if (removeBtnT != null)
//        {
//            _removeButton = removeBtnT.GetComponent<Button>();
//            _removeButton?.onClick.AddListener(RemoveArcher);
//        }

//        CanvasGroup cg = GetComponent<CanvasGroup>();
//        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

//        EnsureRaycastPassthrough();
//        RefreshVisuals();

//        // Hidden by default — shown only when Archer tab is active.
//        gameObject.SetActive(false);
//    }

//    // ── Click handler ─────────────────────────────────────────────

//    private void OnArcherZoneClicked()
//    {
//        Debug.Log("[ArcherZoneCastle] Clicked — opening Army panel.");
//        GameManager.Instance?.OpenArmyPanel();
//    }

//    // ── IDropHandler ──────────────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);

//        if (IsOccupied)
//        {
//            Debug.Log("[ArcherZoneCastle] Already occupied.");
//            ResetColor();
//            return;
//        }

//        SoldierDragDrop soldier = SoldierDragDrop.CurrentlyDragging;
//        if (soldier == null)
//        {
//            Debug.Log("[ArcherZoneCastle] OnDrop — no soldier being dragged.");
//            ResetColor();
//            return;
//        }

//        PlaceArcher(soldier);
//    }

//    // ── IPointerEnterHandler / IPointerExitHandler ────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (IsOccupied) return;
//        _highlight?.SetActive(true);
//        bool validDrag = SoldierDragDrop.CurrentlyDragging != null;
//        _bg.color = validDrag ? hoverValidColor : hoverInvalidColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);
//        ResetColor();
//    }

//    // ── Public API ────────────────────────────────────────────────

//    public void PlaceArcher(SoldierDragDrop soldier)
//    {
//        if (IsOccupied) return;

//        if (archerPrefab == null)
//        {
//            Debug.LogError("[ArcherZoneCastle] archerPrefab is not assigned!", this);
//            return;
//        }

//        _archerInstance = Instantiate(archerPrefab, _spawnpoint.position,
//                                      Quaternion.identity, _spawnpoint);

//        RectTransform rt = _archerInstance.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale = Vector3.one;
//        }

//        ArcherUnit archerUnit = _archerInstance.GetComponent<ArcherUnit>();
//        if (archerUnit != null) archerUnit.Init(null);

//        soldier.BecomeArcher(this);

//        _stationedSoldier = soldier;
//        IsOccupied = true;

//        RefreshVisuals();
//        Debug.Log($"[ArcherZoneCastle] Archer placed at {gameObject.name}.");
//    }

//    public void RemoveArcher()
//    {
//        if (!IsOccupied) return;

//        if (_archerInstance != null) { Destroy(_archerInstance); _archerInstance = null; }

//        _stationedSoldier?.ReturnFromArcher();
//        _stationedSoldier = null;

//        IsOccupied = false;
//        RefreshVisuals();
//        Debug.Log($"[ArcherZoneCastle] Archer removed from {gameObject.name}.");
//    }

//    // ── Tab visibility ────────────────────────────────────────────

//    public void SetArcherTabActive(bool active)
//    {
//        gameObject.SetActive(active);
//        if (active) RefreshVisuals();
//    }

//    public static void SetArcherZonesVisible(bool visible)
//    {
//        foreach (var zone in FindObjectsByType<ArcherZoneCastle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
//            zone.SetArcherTabActive(visible);
//    }

//    // ── Helpers ───────────────────────────────────────────────────

//    private void RefreshVisuals()
//    {
//        _emptySlotZone?.SetActive(!IsOccupied);
//        _highlight?.SetActive(false);
//        if (_removeButton != null) _removeButton.gameObject.SetActive(IsOccupied);
//        ResetColor();
//    }

//    private void ResetColor()
//    {
//        if (_bg != null) _bg.color = IsOccupied ? occupiedColor : normalColor;
//    }

//    private void EnsureRaycastPassthrough()
//    {
//        Transform t = transform.parent;
//        while (t != null)
//        {
//            Image img = t.GetComponent<Image>();
//            if (img != null && img.color.a == 0f)
//                img.raycastTarget = false;
//            t = t.parent;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ArcherZoneCastle
///
/// Attach to the "ArcherZone" child of CastleBlockUnitSlot — mirrors CannonSlotCastle.
///
/// CLICK  → opens the Army/Soldier panel (GameManager.OpenArmyPanel).
/// DROP   → soldier dragged onto this zone becomes an ArcherUnit prefab.
///
/// Required child hierarchy:
///   ArcherZone              ← this script + Image + Button + CanvasGroup
///   ├── EmptySlotZone       shown while empty and Archer tab is active
///   ├── Highlight           glow shown during valid drag hover
///   ├── Spawnpoint          where the ArcherUnit prefab spawns
///   └── RemoveButton (opt)  Button to remove the stationed archer
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class ArcherZoneCastle : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // ── Inspector ─────────────────────────────────────────────────

    [Header("Prefab")]
    [Tooltip("Prefab with an ArcherUnit component. Spawned when a soldier is dropped here.")]
    [SerializeField] private GameObject archerPrefab;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
    [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);
    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.10f);

    // ── Auto-wired children ───────────────────────────────────────

    private Button _button;
    private Button _removeButton;
    private Image _bg;
    private GameObject _emptySlotZone;
    private GameObject _highlight;
    private Transform _spawnpoint;
    private CastleBlockUnitSlot _parentSlot;

    // ── State ─────────────────────────────────────────────────────

    public bool IsOccupied { get; private set; }

    /// <summary>
    /// Set to true when a zone is clicked to open the Army panel.
    /// UIManager reads this to know it should return to Castle+Archer after buying.
    /// </summary>
    public static bool PendingArcherBuy { get; private set; }

    private GameObject _archerInstance;
    private SoldierDragDrop _stationedSoldier;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnArcherZoneClicked);

        _bg = GetComponent<Image>();
        _bg.color = normalColor;
        _bg.raycastTarget = true;

        _parentSlot = GetComponentInParent<CastleBlockUnitSlot>();

        _emptySlotZone = transform.Find("EmptySlotZone")?.gameObject;
        _highlight = transform.Find("Highlight")?.gameObject;
        _spawnpoint = transform.Find("Spawnpoint") != null
                         ? transform.Find("Spawnpoint")
                         : transform;

        var removeBtnT = transform.Find("RemoveButton");
        if (removeBtnT != null)
        {
            _removeButton = removeBtnT.GetComponent<Button>();
            _removeButton?.onClick.AddListener(RemoveArcher);
        }

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

        EnsureRaycastPassthrough();
        RefreshVisuals();

        // Hidden by default — shown only when Archer tab is active.
        gameObject.SetActive(false);
    }

    // ── Click handler ─────────────────────────────────────────────

    private void OnArcherZoneClicked()
    {
        Debug.Log("[ArcherZoneCastle] Clicked — opening Army panel.");
        PendingArcherBuy = true;          // tell UIManager to return to Castle+Archer
        GameManager.Instance?.OpenArmyPanel();
    }

    /// <summary>Called by UIManager after it consumes the pending buy context.</summary>
    public static void ClearPendingArcherBuy() => PendingArcherBuy = false;

    // ── IDropHandler ──────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        _highlight?.SetActive(false);

        if (IsOccupied)
        {
            Debug.Log("[ArcherZoneCastle] Already occupied.");
            ResetColor();
            return;
        }

        if (_parentSlot != null && _parentSlot.IsBlockedByCannon)
        {
            Debug.Log("[ArcherZoneCastle] Blocked — a cannon is already placed on this block.");
            ResetColor();
            return;
        }

        SoldierDragDrop soldier = SoldierDragDrop.CurrentlyDragging;
        if (soldier == null)
        {
            Debug.Log("[ArcherZoneCastle] OnDrop — no soldier being dragged.");
            ResetColor();
            return;
        }

        PlaceArcher(soldier);
    }

    // ── IPointerEnterHandler / IPointerExitHandler ────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsOccupied) return;
        _highlight?.SetActive(true);
        bool blockedByCannon = _parentSlot != null && _parentSlot.IsBlockedByCannon;
        bool validDrag = SoldierDragDrop.CurrentlyDragging != null && !blockedByCannon;
        _bg.color = validDrag ? hoverValidColor : hoverInvalidColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlight?.SetActive(false);
        ResetColor();
    }

    // ── Public API ────────────────────────────────────────────────

    public void PlaceArcher(SoldierDragDrop soldier)
    {
        if (IsOccupied) return;

        if (_parentSlot != null && _parentSlot.IsBlockedByCannon)
        {
            Debug.LogWarning("[ArcherZoneCastle] Cannot place archer — cannon already occupies this block.");
            return;
        }

        if (archerPrefab == null)
        {
            Debug.LogError("[ArcherZoneCastle] archerPrefab is not assigned!", this);
            return;
        }

        _archerInstance = Instantiate(archerPrefab, _spawnpoint.position,
                                      Quaternion.identity, _spawnpoint);

        RectTransform rt = _archerInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        ArcherUnit archerUnit = _archerInstance.GetComponent<ArcherUnit>();
        if (archerUnit != null) archerUnit.Init(null);

        soldier.BecomeArcher(this);

        _stationedSoldier = soldier;
        IsOccupied = true;

        RefreshVisuals();
        Debug.Log($"[ArcherZoneCastle] Archer placed at {gameObject.name}.");

        // Hide the cannon zone now that this block is occupied by an archer.
        _parentSlot?.NotifyOccupancyChanged();
    }

    public void RemoveArcher()
    {
        if (!IsOccupied) return;

        if (_archerInstance != null) { Destroy(_archerInstance); _archerInstance = null; }

        _stationedSoldier?.ReturnFromArcher();
        _stationedSoldier = null;

        IsOccupied = false;
        RefreshVisuals();
        Debug.Log($"[ArcherZoneCastle] Archer removed from {gameObject.name}.");

        // Reveal the cannon zone now that this block is vacant.
        _parentSlot?.NotifyOccupancyChanged();
    }

    /// <summary>
    /// Migrates the stationed archer (instance + soldier reference) from this zone
    /// into <paramref name="destination"/>. Mirrors CastleUnitDropZone.MigrateUnitTo.
    /// Call this before the source block/cell is destroyed (e.g. on expansion).
    /// </summary>
    public void MigrateArcherTo(ArcherZoneCastle destination)
    {
        if (destination == null || !IsOccupied || destination.IsOccupied) return;

        // ── Move the archer instance to the destination spawnpoint ──
        if (_archerInstance != null)
        {
            _archerInstance.transform.SetParent(destination._spawnpoint, worldPositionStays: false);
            RectTransform rt = _archerInstance.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
            destination._archerInstance = _archerInstance;
            _archerInstance = null;
        }

        // ── Transfer soldier reference ──
        destination._stationedSoldier = _stationedSoldier;
        if (_stationedSoldier != null)
            _stationedSoldier.BecomeArcher(destination);   // update back-reference on soldier
        _stationedSoldier = null;

        // ── Update state ──
        destination.IsOccupied = true;
        IsOccupied = false;

        destination.RefreshVisuals();
        RefreshVisuals();

        // Notify both parent slots so mutual-hide stays consistent.
        _parentSlot?.NotifyOccupancyChanged();
        destination._parentSlot?.NotifyOccupancyChanged();

        Debug.Log($"[ArcherZoneCastle] MigrateArcherTo — archer moved from '{gameObject.name}' to '{destination.gameObject.name}'.");
    }

    // ── Tab visibility ────────────────────────────────────────────

    public void SetArcherTabActive(bool active)
    {
        // If trying to show this zone but the sibling cannon slot is occupied,
        // keep it hidden — a cannon already owns this block.
        if (active && _parentSlot != null && _parentSlot.HasCannon)
        {
            gameObject.SetActive(false);
            return;
        }

        if (IsOccupied)
        {
            // Archer is placed — keep the GameObject active so the spawned
            // archer unit stays visible (including in village mode). Only hide
            // the interactive overlays (EmptySlotZone, highlight) that belong
            // to the empty state.
            gameObject.SetActive(true);
            _emptySlotZone?.SetActive(false);
            _highlight?.SetActive(false);
            // Show RemoveButton only when the archer tab is open.
            _removeButton?.gameObject.SetActive(active);
            // Enable/disable interaction based on whether tab is open.
            if (_button != null) _button.interactable = active;
            if (_bg != null) _bg.raycastTarget = active;
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) { cg.interactable = active; cg.blocksRaycasts = active; }
        }
        else
        {
            // Zone is empty — safe to fully deactivate when tab is off.
            gameObject.SetActive(active);
            if (active)
            {
                if (_button != null) _button.interactable = true;
                if (_bg != null) _bg.raycastTarget = true;
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }
                RefreshVisuals();
            }
        }
    }

    public static void SetArcherZonesVisible(bool visible)
    {
        foreach (var zone in FindObjectsByType<ArcherZoneCastle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            zone.SetArcherTabActive(visible);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void RefreshVisuals()
    {
        _emptySlotZone?.SetActive(!IsOccupied);
        _highlight?.SetActive(false);
        if (_removeButton != null) _removeButton.gameObject.SetActive(IsOccupied);
        ResetColor();
    }

    private void ResetColor()
    {
        if (_bg != null) _bg.color = IsOccupied ? occupiedColor : normalColor;
    }

    private void EnsureRaycastPassthrough()
    {
        Transform t = transform.parent;
        while (t != null)
        {
            Image img = t.GetComponent<Image>();
            if (img != null && img.color.a == 0f)
                img.raycastTarget = false;
            t = t.parent;
        }
    }
}