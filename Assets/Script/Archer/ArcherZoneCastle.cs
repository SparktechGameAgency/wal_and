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
    }

    // ── Tab visibility ────────────────────────────────────────────

    public void SetArcherTabActive(bool active)
    {
        gameObject.SetActive(active);
        if (active) RefreshVisuals();
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