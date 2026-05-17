////////////////////using UnityEngine;
////////////////////using UnityEngine.UI;
////////////////////using UnityEngine.EventSystems;

/////////////////////// <summary>
/////////////////////// Attach to each cannon / soldier icon in the Castle Shop panel.
/////////////////////// Dragging this creates a ghost that follows the cursor.
/////////////////////// CastleUnitDropZone checks CastleUnitDraggable.CurrentlyDragging on drop.
///////////////////////
/////////////////////// ── Inspector ──────────────────────────────────────────────────
///////////////////////   unitType   → Cannon or Soldier
///////////////////////   unitSprite → the sprite shown on the drag ghost (and placed in the slot)
/////////////////////// </summary>
////////////////////[RequireComponent(typeof(CanvasGroup))]
////////////////////[RequireComponent(typeof(Image))]
////////////////////public class CastleUnitDraggable : MonoBehaviour,
////////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////////////{
////////////////////    // ── Static drag state (shared across all draggables) ──────────
////////////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////    [Header("Unit")]
////////////////////    public CastleUnitType unitType;
////////////////////    public Sprite unitSprite;

////////////////////    // ── Private ───────────────────────────────────────────────────
////////////////////    private CanvasGroup _canvasGroup;
////////////////////    private static GameObject _ghost;
////////////////////    private static Canvas _rootCanvas;

////////////////////    private void Awake()
////////////////////    {
////////////////////        _canvasGroup = GetComponent<CanvasGroup>();

////////////////////        // If no sprite set in inspector, fall back to this Image's sprite
////////////////////        if (unitSprite == null)
////////////////////            unitSprite = GetComponent<Image>().sprite;
////////////////////    }

////////////////////    // ── Drag ──────────────────────────────────────────────────────

////////////////////    public void OnBeginDrag(PointerEventData eventData)
////////////////////    {
////////////////////        CurrentlyDragging = this;
////////////////////        _rootCanvas = FindRootCanvas();

////////////////////        // Fade the source icon slightly
////////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;

////////////////////        // Create ghost image that follows the cursor
////////////////////        if (_rootCanvas != null)
////////////////////        {
////////////////////            _ghost = new GameObject("CastleUnitDragGhost",
////////////////////                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
////////////////////            _ghost.transform.SetParent(_rootCanvas.transform, false);
////////////////////            _ghost.transform.SetAsLastSibling();        // render on top

////////////////////            // Ghost is visible but doesn't block raycasts (so drop zones receive events)
////////////////////            var cg = _ghost.GetComponent<CanvasGroup>();
////////////////////            cg.alpha = 0.80f;
////////////////////            cg.blocksRaycasts = false;

////////////////////            var img = _ghost.GetComponent<Image>();
////////////////////            if (unitSprite != null) { img.sprite = unitSprite; img.preserveAspect = true; }

////////////////////            _ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
////////////////////            MoveGhostTo(eventData);
////////////////////        }
////////////////////    }

////////////////////    public void OnDrag(PointerEventData eventData)
////////////////////    {
////////////////////        MoveGhostTo(eventData);
////////////////////    }

////////////////////    public void OnEndDrag(PointerEventData eventData)
////////////////////    {
////////////////////        // Restore source icon
////////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

////////////////////        DestroyGhost();
////////////////////        CurrentlyDragging = null;
////////////////////    }

////////////////////    // ── Ghost helpers ─────────────────────────────────────────────

////////////////////    private static void MoveGhostTo(PointerEventData eventData)
////////////////////    {
////////////////////        if (_ghost == null || _rootCanvas == null) return;

////////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////////            _rootCanvas.GetComponent<RectTransform>(),
////////////////////            eventData.position,
////////////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////////                ? null : _rootCanvas.worldCamera,
////////////////////            out Vector2 local);

////////////////////        _ghost.GetComponent<RectTransform>().anchoredPosition = local;
////////////////////    }

////////////////////    public static void DestroyGhost()
////////////////////    {
////////////////////        if (_ghost != null) { Object.Destroy(_ghost); _ghost = null; }
////////////////////    }

////////////////////    private Canvas FindRootCanvas()
////////////////////    {
////////////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
////////////////////    }
////////////////////}

//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using UnityEngine.EventSystems;

///////////////////// <summary>
///////////////////// Attach to each cannon / soldier icon in the Castle Shop panel.
///////////////////// Dragging this creates a ghost that follows the cursor; on a successful
///////////////////// drop the unit PREFAB is instantiated inside the CastleUnitDropZone.
/////////////////////
///////////////////// ── Inspector ──────────────────────────────────────────────────
/////////////////////   unitType    → Cannon or Soldier
/////////////////////   unitPrefab  → the prefab that will be instantiated in the drop zone
/////////////////////   ghostSprite → (optional) sprite shown on the drag ghost.
/////////////////////                 If left empty, the sprite is read from unitPrefab's
/////////////////////                 root Image component automatically.
///////////////////// </summary>
//////////////////[RequireComponent(typeof(CanvasGroup))]
//////////////////[RequireComponent(typeof(Image))]
//////////////////public class CastleUnitDraggable : MonoBehaviour,
//////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////////{
//////////////////    // ── Static drag state (shared across all draggables) ──────────
//////////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////    [Header("Unit")]
//////////////////    public CastleUnitType unitType;

//////////////////    [Tooltip("The prefab that will be spawned inside the drop zone on a successful drop.")]
//////////////////    public GameObject unitPrefab;

//////////////////    [Tooltip("Sprite displayed on the semi-transparent drag ghost. " +
//////////////////             "Leave empty to auto-read from unitPrefab's root Image.")]
//////////////////    public Sprite ghostSprite;

//////////////////    // ── Private ───────────────────────────────────────────────────
//////////////////    private CanvasGroup _canvasGroup;
//////////////////    private static GameObject _ghost;
//////////////////    private static Canvas _rootCanvas;

//////////////////    private void Awake()
//////////////////    {
//////////////////        _canvasGroup = GetComponent<CanvasGroup>();
//////////////////    }

//////////////////    // ── Drag handlers ─────────────────────────────────────────────

//////////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////////    {
//////////////////        if (unitPrefab == null)
//////////////////        {
//////////////////            Debug.LogWarning("[CastleUnitDraggable] unitPrefab is not assigned!");
//////////////////            return;
//////////////////        }

//////////////////        CurrentlyDragging = this;
//////////////////        _rootCanvas = FindRootCanvas();

//////////////////        // Fade the source icon slightly while dragging
//////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;

//////////////////        // ── Create semi-transparent ghost that follows the cursor ──
//////////////////        if (_rootCanvas != null)
//////////////////        {
//////////////////            _ghost = new GameObject("CastleUnitDragGhost",
//////////////////                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
//////////////////            _ghost.transform.SetParent(_rootCanvas.transform, false);
//////////////////            _ghost.transform.SetAsLastSibling();    // render above everything

//////////////////            // Ghost is visible but does NOT block raycasts so drop zones
//////////////////            // still receive pointer events
//////////////////            var cg = _ghost.GetComponent<CanvasGroup>();
//////////////////            cg.alpha = 0.80f;
//////////////////            cg.blocksRaycasts = false;

//////////////////            // Resolve ghost sprite: explicit override → prefab's root Image → null
//////////////////            Sprite sprite = ghostSprite;
//////////////////            if (sprite == null)
//////////////////            {
//////////////////                Image prefabImg = unitPrefab.GetComponent<Image>();
//////////////////                if (prefabImg != null) sprite = prefabImg.sprite;
//////////////////            }

//////////////////            var img = _ghost.GetComponent<Image>();
//////////////////            if (sprite != null) { img.sprite = sprite; img.preserveAspect = true; }

//////////////////            _ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
//////////////////            MoveGhostTo(eventData);
//////////////////        }
//////////////////    }

//////////////////    public void OnDrag(PointerEventData eventData)
//////////////////    {
//////////////////        MoveGhostTo(eventData);
//////////////////    }

//////////////////    public void OnEndDrag(PointerEventData eventData)
//////////////////    {
//////////////////        // Restore source icon opacity
//////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

//////////////////        DestroyGhost();
//////////////////        CurrentlyDragging = null;
//////////////////    }

//////////////////    // ── Ghost helpers ─────────────────────────────────────────────

//////////////////    private static void MoveGhostTo(PointerEventData eventData)
//////////////////    {
//////////////////        if (_ghost == null || _rootCanvas == null) return;

//////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////////            _rootCanvas.GetComponent<RectTransform>(),
//////////////////            eventData.position,
//////////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////////                ? null : _rootCanvas.worldCamera,
//////////////////            out Vector2 local);

//////////////////        _ghost.GetComponent<RectTransform>().anchoredPosition = local;
//////////////////    }

//////////////////    public static void DestroyGhost()
//////////////////    {
//////////////////        if (_ghost != null) { Object.Destroy(_ghost); _ghost = null; }
//////////////////    }

//////////////////    // ── Utility ───────────────────────────────────────────────────

//////////////////    private Canvas FindRootCanvas()
//////////////////    {
//////////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
//////////////////    }
//////////////////}


////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using UnityEngine.EventSystems;

/////////////////// <summary>
/////////////////// Attach this to your Cannon Spawner and Soldier Spawner objects.
///////////////////
/////////////////// When the player begins a drag, a live instance of <see cref="unitPrefab"/>
/////////////////// is created and follows the cursor. On a successful drop the
/////////////////// <see cref="CastleUnitDropZone"/> reparents that instance into the slot.
/////////////////// If the drag ends without a valid drop the instance is destroyed.
///////////////////
/////////////////// ── Inspector ──────────────────────────────────────────────────
///////////////////   unitType   → Cannon or Soldier
///////////////////   unitPrefab → the prefab to spawn and drag
/////////////////// </summary>
////////////////[RequireComponent(typeof(CanvasGroup))]
////////////////public class CastleUnitDraggable : MonoBehaviour,
////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////////{
////////////////    // ── Shared drag state ─────────────────────────────────────────
////////////////    /// <summary>The live prefab instance currently being dragged.</summary>
////////////////    public static GameObject CurrentDragInstance { get; private set; }

////////////////    /// <summary>The draggable (spawner) that started the current drag.</summary>
////////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////    [Header("Unit")]
////////////////    public CastleUnitType unitType;

////////////////    [Tooltip("Prefab instantiated when drag starts. " +
////////////////             "Placed in the drop zone on success, destroyed on failure.")]
////////////////    public GameObject unitPrefab;

////////////////    // ── Private ───────────────────────────────────────────────────
////////////////    private CanvasGroup _canvasGroup;
////////////////    private Canvas _rootCanvas;

////////////////    // Set to true by CastleUnitDropZone when the drop succeeds,
////////////////    // so OnEndDrag knows NOT to destroy the instance.
////////////////    private static bool _droppedSuccessfully;

////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        _canvasGroup = GetComponent<CanvasGroup>();
////////////////    }

////////////////    // ── Drag handlers ─────────────────────────────────────────────

////////////////    public void OnBeginDrag(PointerEventData eventData)
////////////////    {
////////////////        if (unitPrefab == null)
////////////////        {
////////////////            Debug.LogWarning($"[CastleUnitDraggable] unitPrefab not assigned on '{name}'!");
////////////////            return;
////////////////        }

////////////////        _rootCanvas = FindRootCanvas();
////////////////        CurrentlyDragging = this;
////////////////        _droppedSuccessfully = false;

////////////////        // Spawn the live unit instance on the root canvas so it floats
////////////////        // above everything else while being dragged.
////////////////        CurrentDragInstance = Instantiate(unitPrefab, _rootCanvas.transform);
////////////////        CurrentDragInstance.transform.SetAsLastSibling();

////////////////        // Disable raycasts on the instance so pointer events pass
////////////////        // through to the drop zones underneath.
////////////////        CanvasGroup cg = CurrentDragInstance.GetComponent<CanvasGroup>();
////////////////        if (cg == null) cg = CurrentDragInstance.AddComponent<CanvasGroup>();
////////////////        cg.blocksRaycasts = false;
////////////////        cg.alpha = 0.85f;

////////////////        // Position and size the floating instance
////////////////        RectTransform rt = CurrentDragInstance.GetComponent<RectTransform>();
////////////////        if (rt != null)
////////////////        {
////////////////            rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////////            rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////////            rt.pivot = new Vector2(0.5f, 0.5f);
////////////////            rt.sizeDelta = new Vector2(64f, 64f);
////////////////        }

////////////////        MoveInstanceTo(eventData);

////////////////        // Dim the spawner icon during the drag
////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
////////////////    }

////////////////    public void OnDrag(PointerEventData eventData)
////////////////    {
////////////////        MoveInstanceTo(eventData);
////////////////    }

////////////////    public void OnEndDrag(PointerEventData eventData)
////////////////    {
////////////////        // Restore spawner icon
////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

////////////////        // Drop zone did NOT accept the unit — destroy the floating instance
////////////////        if (!_droppedSuccessfully && CurrentDragInstance != null)
////////////////            Destroy(CurrentDragInstance);

////////////////        CurrentDragInstance = null;
////////////////        CurrentlyDragging = null;
////////////////        _droppedSuccessfully = false;
////////////////    }

////////////////    // ── Called by CastleUnitDropZone on a successful drop ─────────

////////////////    /// <summary>
////////////////    /// Signals that the drop zone accepted and reparented the instance,
////////////////    /// so OnEndDrag will not destroy it.
////////////////    /// </summary>
////////////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////////////////    // ── Helpers ───────────────────────────────────────────────────

////////////////    private void MoveInstanceTo(PointerEventData eventData)
////////////////    {
////////////////        if (CurrentDragInstance == null || _rootCanvas == null) return;

////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////            _rootCanvas.GetComponent<RectTransform>(),
////////////////            eventData.position,
////////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////                ? null : _rootCanvas.worldCamera,
////////////////            out Vector2 local);

////////////////        CurrentDragInstance.GetComponent<RectTransform>().anchoredPosition = local;
////////////////    }

////////////////    private Canvas FindRootCanvas()
////////////////    {
////////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
////////////////    }
////////////////}

//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using UnityEngine.EventSystems;

///////////////// <summary>
///////////////// Attach to each spawner object (Cannon Spawner x3, Soldier Spawner x1, etc.).
/////////////////
///////////////// On drag start a live instance of <see cref="unitPrefab"/> is created and
///////////////// follows the cursor. On a successful drop <see cref="CastleUnitDropZone"/>
///////////////// reparents that instance into the slot. On a failed drop it is destroyed.
/////////////////
///////////////// ── Inspector ────────────────────────────────────────────────────────────
/////////////////   unitType   → Cannon or Soldier  (controls which drop zone accepts it)
/////////////////   variantId  → 0 / 1 / 2 … distinguishes cannon variants from each other
/////////////////                (e.g. 0 = Light Cannon, 1 = Medium Cannon, 2 = Heavy Cannon)
/////////////////                Ignored for units that have only one variant (e.g. Soldier = 0)
/////////////////   unitPrefab → the actual prefab instantiated during drag and placed on drop
///////////////// </summary>
//////////////[RequireComponent(typeof(CanvasGroup))]
//////////////public class CastleUnitDraggable : MonoBehaviour,
//////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////{
//////////////    // ── Shared static drag state ──────────────────────────────────
//////////////    /// <summary>The live prefab instance currently being dragged.</summary>
//////////////    public static GameObject CurrentDragInstance { get; private set; }

//////////////    /// <summary>The spawner that initiated the current drag.</summary>
//////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////    [Header("Unit Identity")]
//////////////    public CastleUnitType unitType;

//////////////    [Tooltip("Distinguishes variants of the same unit type.\n" +
//////////////             "Example — Cannon spawners:\n" +
//////////////             "  Spawner A → variantId 0  (Light Cannon)\n" +
//////////////             "  Spawner B → variantId 1  (Medium Cannon)\n" +
//////////////             "  Spawner C → variantId 2  (Heavy Cannon)\n" +
//////////////             "Leave at 0 for units with only one variant (e.g. Soldier).")]
//////////////    public int variantId = 0;

//////////////    [Tooltip("Prefab instantiated on drag start. Placed in the drop zone " +
//////////////             "on success, destroyed on failure.")]
//////////////    public GameObject unitPrefab;

//////////////    // ── Private ───────────────────────────────────────────────────
//////////////    private CanvasGroup _canvasGroup;
//////////////    private Canvas _rootCanvas;

//////////////    // CastleUnitDropZone sets this to true when it accepts the drop,
//////////////    // so OnEndDrag knows not to destroy the instance.
//////////////    private static bool _droppedSuccessfully;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        _canvasGroup = GetComponent<CanvasGroup>();
//////////////    }

//////////////    // ── Drag handlers ─────────────────────────────────────────────

//////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////    {
//////////////        if (unitPrefab == null)
//////////////        {
//////////////            Debug.LogWarning($"[CastleUnitDraggable] unitPrefab not assigned on '{name}'!");
//////////////            return;
//////////////        }

//////////////        _rootCanvas = FindRootCanvas();
//////////////        CurrentlyDragging = this;
//////////////        _droppedSuccessfully = false;

//////////////        // Spawn the live unit on the root canvas so it floats above everything.
//////////////        CurrentDragInstance = Instantiate(unitPrefab, _rootCanvas.transform);
//////////////        CurrentDragInstance.transform.SetAsLastSibling();

//////////////        // Disable raycasts on the floating instance so pointer events reach
//////////////        // the drop zones underneath it.
//////////////        CanvasGroup cg = CurrentDragInstance.GetComponent<CanvasGroup>();
//////////////        if (cg == null) cg = CurrentDragInstance.AddComponent<CanvasGroup>();
//////////////        cg.blocksRaycasts = false;
//////////////        cg.alpha = 0.85f;

//////////////        // Initial size of the floating instance while dragging
//////////////        RectTransform rt = CurrentDragInstance.GetComponent<RectTransform>();
//////////////        if (rt != null)
//////////////        {
//////////////            rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////            rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////            rt.pivot = new Vector2(0.5f, 0.5f);
//////////////            rt.sizeDelta = new Vector2(64f, 64f);
//////////////        }

//////////////        MoveInstanceTo(eventData);

//////////////        // Dim the spawner icon during the drag
//////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
//////////////    }

//////////////    public void OnDrag(PointerEventData eventData)
//////////////    {
//////////////        MoveInstanceTo(eventData);
//////////////    }

//////////////    public void OnEndDrag(PointerEventData eventData)
//////////////    {
//////////////        // Restore spawner icon
//////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

//////////////        // No valid drop → destroy the floating instance
//////////////        if (!_droppedSuccessfully && CurrentDragInstance != null)
//////////////            Destroy(CurrentDragInstance);

//////////////        CurrentDragInstance = null;
//////////////        CurrentlyDragging = null;
//////////////        _droppedSuccessfully = false;
//////////////    }

//////////////    // ── Called by CastleUnitDropZone on a successful drop ─────────

//////////////    /// <summary>
//////////////    /// Signals that the drop zone accepted and reparented the instance,
//////////////    /// so OnEndDrag will NOT destroy it.
//////////////    /// </summary>
//////////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////////////    // ── Helpers ───────────────────────────────────────────────────

//////////////    private void MoveInstanceTo(PointerEventData eventData)
//////////////    {
//////////////        if (CurrentDragInstance == null || _rootCanvas == null) return;

//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            _rootCanvas.GetComponent<RectTransform>(),
//////////////            eventData.position,
//////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////                ? null : _rootCanvas.worldCamera,
//////////////            out Vector2 local);

//////////////        CurrentDragInstance.GetComponent<RectTransform>().anchoredPosition = local;
//////////////    }

//////////////    private Canvas FindRootCanvas()
//////////////    {
//////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using UnityEngine.EventSystems;

/////////////// <summary>
/////////////// Attach to your Cannon and Soldier prefabs.
/////////////// The unit itself follows the cursor while dragging.
/////////////// - Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////////////// - Invalid drop → unit snaps back to where it came from.
///////////////
/////////////// Inspector:
///////////////   unitType  → Cannon or Soldier
///////////////   variantId → 0 / 1 / 2 … (set this on each cannon variant prefab)
/////////////// </summary>
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class CastleUnitDraggable : MonoBehaviour,
////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////{
////////////    // ── Shared drag state ─────────────────────────────────────────
////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    public CastleUnitType unitType;

////////////    [Tooltip("Distinguishes cannon variants (0 = Light, 1 = Medium, 2 = Heavy). " +
////////////             "Leave 0 for Soldier.")]
////////////    public int variantId = 0;

////////////    // ── Private ───────────────────────────────────────────────────
////////////    private CanvasGroup _canvasGroup;
////////////    private Canvas _rootCanvas;
////////////    private Transform _originalParent;
////////////    private Vector2 _originalPosition;
////////////    private static bool _droppedSuccessfully;

////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        _canvasGroup = GetComponent<CanvasGroup>();
////////////    }

////////////    // ── Drag ──────────────────────────────────────────────────────

////////////    public void OnBeginDrag(PointerEventData eventData)
////////////    {
////////////        CurrentlyDragging = this;
////////////        _droppedSuccessfully = false;

////////////        // Remember where to return if the drop fails
////////////        _originalParent = transform.parent;
////////////        _originalPosition = GetComponent<RectTransform>().anchoredPosition;

////////////        // Move to root canvas so it renders above all other UI
////////////        _rootCanvas = FindRootCanvas();
////////////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////////////        transform.SetAsLastSibling();

////////////        // Let pointer events pass through to the drop zones below
////////////        _canvasGroup.blocksRaycasts = false;
////////////        _canvasGroup.alpha = 0.85f;
////////////    }

////////////    public void OnDrag(PointerEventData eventData)
////////////    {
////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            _rootCanvas.GetComponent<RectTransform>(),
////////////            eventData.position,
////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////                ? null : _rootCanvas.worldCamera,
////////////            out Vector2 local);

////////////        GetComponent<RectTransform>().anchoredPosition = local;
////////////    }

////////////    public void OnEndDrag(PointerEventData eventData)
////////////    {
////////////        _canvasGroup.blocksRaycasts = true;
////////////        _canvasGroup.alpha = 1f;

////////////        if (!_droppedSuccessfully)
////////////        {
////////////            // Return to original parent and position
////////////            transform.SetParent(_originalParent, worldPositionStays: false);
////////////            GetComponent<RectTransform>().anchoredPosition = _originalPosition;
////////////        }

////////////        CurrentlyDragging = null;
////////////        _droppedSuccessfully = false;
////////////    }

////////////    // ── Called by CastleUnitDropZone on success ───────────────────

////////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////////////    // ── Helpers ───────────────────────────────────────────────────

////////////    private Canvas FindRootCanvas()
////////////    {
////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////////        return (all == null || all.Length == 0)
////////////            ? FindObjectOfType<Canvas>()
////////////            : all[all.Length - 1];
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using UnityEngine.EventSystems;

///////////// <summary>
///////////// Attach to your Cannon and Soldier prefabs.
///////////// The unit itself follows the cursor while dragging.
/////////////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////////////   Invalid drop → unit snaps back to its original position in the village panel.
/////////////
///////////// Inspector:
/////////////   unitType  → Cannon or Soldier
/////////////   variantId → 0 / 1 / 2 for each cannon variant (leave 0 for Soldier)
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class CastleUnitDraggable : MonoBehaviour,
//////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////{
//////////    // ── Shared drag state ─────────────────────────────────────────
//////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    public CastleUnitType unitType;

//////////    [Tooltip("0 = Light Cannon, 1 = Medium Cannon, 2 = Heavy Cannon. Leave 0 for Soldier.")]
//////////    public int variantId = 0;

//////////    // ── Private ───────────────────────────────────────────────────
//////////    private CanvasGroup _canvasGroup;
//////////    private Canvas _rootCanvas;
//////////    private Transform _originalParent;
//////////    private Vector2 _originalPosition;
//////////    private static bool _droppedSuccessfully;

//////////    private void Awake()
//////////    {
//////////        _canvasGroup = GetComponent<CanvasGroup>();
//////////    }

//////////    // ── Drag ──────────────────────────────────────────────────────

//////////    public void OnBeginDrag(PointerEventData eventData)
//////////    {
//////////        CurrentlyDragging = this;
//////////        _droppedSuccessfully = false;

//////////        // Remember where to return on a failed drop
//////////        _originalParent = transform.parent;
//////////        _originalPosition = GetComponent<RectTransform>().anchoredPosition;

//////////        // Lift to root canvas so it renders above all UI panels
//////////        _rootCanvas = FindRootCanvas();
//////////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////////        transform.SetAsLastSibling();

//////////        // Pass pointer events through to drop zones beneath
//////////        _canvasGroup.blocksRaycasts = false;
//////////        _canvasGroup.alpha = 0.85f;
//////////    }

//////////    public void OnDrag(PointerEventData eventData)
//////////    {
//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            _rootCanvas.GetComponent<RectTransform>(),
//////////            eventData.position,
//////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////                ? null : _rootCanvas.worldCamera,
//////////            out Vector2 local);

//////////        GetComponent<RectTransform>().anchoredPosition = local;
//////////    }

//////////    public void OnEndDrag(PointerEventData eventData)
//////////    {
//////////        _canvasGroup.blocksRaycasts = true;
//////////        _canvasGroup.alpha = 1f;

//////////        if (!_droppedSuccessfully)
//////////        {
//////////            // Snap back to the village panel
//////////            transform.SetParent(_originalParent, worldPositionStays: false);
//////////            GetComponent<RectTransform>().anchoredPosition = _originalPosition;
//////////        }

//////////        CurrentlyDragging = null;
//////////        _droppedSuccessfully = false;
//////////    }

//////////    // ── Called by CastleUnitDropZone on a successful drop ─────────
//////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////////    // ── Helpers ───────────────────────────────────────────────────
//////////    private Canvas FindRootCanvas()
//////////    {
//////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////        return (all == null || all.Length == 0)
//////////            ? FindObjectOfType<Canvas>()
//////////            : all[all.Length - 1];
//////////    }
//////////}


////////using UnityEngine;
////////using UnityEngine.UI;
////////using UnityEngine.EventSystems;

/////////// <summary>
/////////// Attach to your Cannon and Soldier draggable objects in the village / unit panel.
/////////// The unit itself follows the cursor while dragging.
///////////
///////////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////////   Invalid drop → unit snaps back to its original position.
///////////
/////////// ── Inspector ────────────────────────────────────────────────────────────
///////////   unitType          → Cannon or Soldier
///////////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///////////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone (simple icon prefabs)
///////////                       FALSE : centers the unit at its natural size (customized / animated prefabs)
///////////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class CastleUnitDraggable : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    // ── Shared drag state ─────────────────────────────────────────
////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////    // ── Inspector ─────────────────────────────────────────────────
////////    [Header("Unit Identity")]
////////    public CastleUnitType unitType;

////////    [Tooltip("0 = Light Cannon / default Soldier. " +
////////             "Increment for each cannon variant (1 = Medium, 2 = Heavy).")]
////////    public int variantId = 0;

////////    [Header("Slot Behaviour")]
////////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle (good for simple icon prefabs).\n" +
////////             "FALSE → unit is centered at its natural size inside the zone " +
////////             "(use this for customized / animated soldier prefabs to prevent broken layouts).")]
////////    public bool stretchToFillSlot = false;   // OFF by default — safest for customized prefabs.

////////    [Tooltip("Pixel size of the unit while being dragged.")]
////////    public Vector2 dragGhostSize = new Vector2(64f, 64f);

////////    // ── Private ───────────────────────────────────────────────────
////////    private CanvasGroup _canvasGroup;
////////    private Canvas _rootCanvas;
////////    private Transform _originalParent;
////////    private Vector2 _originalAnchoredPos;
////////    private Vector2 _originalSizeDelta;
////////    private static bool _droppedSuccessfully;

////////    private void Awake()
////////    {
////////        _canvasGroup = GetComponent<CanvasGroup>();
////////    }

////////    // ── Drag ──────────────────────────────────────────────────────

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        CurrentlyDragging = this;
////////        _droppedSuccessfully = false;

////////        // Remember original location for snap-back on a failed drop
////////        _originalParent = transform.parent;
////////        RectTransform selfRt = GetComponent<RectTransform>();
////////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
////////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

////////        // Lift to root canvas so the unit renders above all UI panels
////////        _rootCanvas = FindRootCanvas();
////////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////////        transform.SetAsLastSibling();

////////        // Resize to dragGhostSize while dragging so all unit types look consistent
////////        if (selfRt != null)
////////        {
////////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
////////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
////////            selfRt.pivot = new Vector2(0.5f, 0.5f);
////////            selfRt.sizeDelta = dragGhostSize;
////////        }

////////        // Pass pointer events through to the drop zones beneath
////////        if (_canvasGroup != null)
////////        {
////////            _canvasGroup.blocksRaycasts = false;
////////            _canvasGroup.alpha = 0.85f;
////////        }

////////        MoveToPointer(eventData);
////////    }

////////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        if (_canvasGroup != null)
////////        {
////////            _canvasGroup.blocksRaycasts = true;
////////            _canvasGroup.alpha = 1f;
////////        }

////////        if (!_droppedSuccessfully)
////////        {
////////            // Snap back — restore parent, position, and original size
////////            transform.SetParent(_originalParent, worldPositionStays: false);
////////            RectTransform rt = GetComponent<RectTransform>();
////////            if (rt != null)
////////            {
////////                rt.anchoredPosition = _originalAnchoredPos;
////////                rt.sizeDelta = _originalSizeDelta;
////////            }
////////        }

////////        CurrentlyDragging = null;
////////        _droppedSuccessfully = false;
////////    }

////////    // ── Called by CastleUnitDropZone on a successful drop ─────────
////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////////    // ── Helpers ───────────────────────────────────────────────────

////////    private void MoveToPointer(PointerEventData eventData)
////////    {
////////        if (_rootCanvas == null) return;

////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            _rootCanvas.GetComponent<RectTransform>(),
////////            eventData.position,
////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////                ? null : _rootCanvas.worldCamera,
////////            out Vector2 local);

////////        RectTransform rt = GetComponent<RectTransform>();
////////        if (rt != null) rt.anchoredPosition = local;
////////    }

////////    private Canvas FindRootCanvas()
////////    {
////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////        return (all == null || all.Length == 0)
////////            ? FindObjectOfType<Canvas>()
////////            : all[all.Length - 1];
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// Attach to your Cannon draggable objects in the village / unit panel.
///////// The unit itself follows the cursor while dragging.
/////////
/////////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////////   Invalid drop → unit snaps back to its original position.
/////////
///////// When the cannon is already placed in a CastleUnitDropZone and the player
///////// drags it again:
/////////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed.
/////////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there.
/////////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored.
/////////
///////// ── Inspector ────────────────────────────────────────────────────────────
/////////   unitType          → Cannon or Soldier
/////////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone (simple icon prefabs)
/////////                       FALSE : centers the unit at its natural size (customised / animated prefabs)
/////////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class CastleUnitDraggable : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Shared drag state ─────────────────────────────────────────
//////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Header("Unit Identity")]
//////    public CastleUnitType unitType;

//////    [Tooltip("0 = Light Cannon / default Soldier. " +
//////             "Increment for each cannon variant (1 = Medium, 2 = Heavy).")]
//////    public int variantId = 0;

//////    [Header("Slot Behaviour")]
//////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle (good for simple icon prefabs).\n" +
//////             "FALSE → unit is centered at its natural size inside the zone " +
//////             "(use this for customised / animated soldier prefabs to prevent broken layouts).")]
//////    public bool stretchToFillSlot = false;

//////    [Tooltip("Pixel size of the unit while being dragged.")]
//////    public Vector2 dragGhostSize = new Vector2(64f, 64f);

//////    // ── Private ───────────────────────────────────────────────────
//////    private CanvasGroup _canvasGroup;
//////    private Canvas _rootCanvas;

//////    // Saved so we can snap back or re-notify the original zone on failed drops
//////    private Transform _originalParent;
//////    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a zone
//////    private Vector2 _originalAnchoredPos;
//////    private Vector2 _originalSizeDelta;

//////    private static bool _droppedSuccessfully;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////    }

//////    // ── Drag ──────────────────────────────────────────────────────

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        CurrentlyDragging = this;
//////        _droppedSuccessfully = false;

//////        // Remember where to return on a failed drop
//////        _originalParent = transform.parent;
//////        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

//////        RectTransform selfRt = GetComponent<RectTransform>();
//////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//////        // If this cannon was sitting inside a drop zone, free that zone now
//////        // so its soldier hides and it can accept a new cannon.
//////        _originalZone?.DetachUnit();

//////        // Lift to root canvas so the unit renders above all UI panels
//////        _rootCanvas = FindRootCanvas();
//////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////        transform.SetAsLastSibling();

//////        // Resize to dragGhostSize while dragging
//////        if (selfRt != null)
//////        {
//////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//////            selfRt.pivot = new Vector2(0.5f, 0.5f);
//////            selfRt.sizeDelta = dragGhostSize;
//////        }

//////        // Pass pointer events through to drop zones beneath
//////        if (_canvasGroup != null)
//////        {
//////            _canvasGroup.blocksRaycasts = false;
//////            _canvasGroup.alpha = 0.85f;
//////        }

//////        MoveToPointer(eventData);
//////    }

//////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        if (_canvasGroup != null)
//////        {
//////            _canvasGroup.blocksRaycasts = true;
//////            _canvasGroup.alpha = 1f;
//////        }

//////        if (!_droppedSuccessfully)
//////        {
//////            // Snap back to the original parent (zone or panel slot)
//////            transform.SetParent(_originalParent, worldPositionStays: false);

//////            RectTransform rt = GetComponent<RectTransform>();
//////            if (rt != null)
//////            {
//////                rt.anchoredPosition = _originalAnchoredPos;
//////                rt.sizeDelta = _originalSizeDelta;
//////            }

//////            // If the cannon came from a zone, restore that zone's state
//////            // (re-shows the soldier, marks HasUnit = true again)
//////            _originalZone?.ReattachUnit(this);
//////        }

//////        _originalZone = null;
//////        _originalParent = null;
//////        CurrentlyDragging = null;
//////        _droppedSuccessfully = false;
//////    }

//////    // ── Called by CastleUnitDropZone on a successful drop ─────────
//////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////    // ── Helpers ───────────────────────────────────────────────────

//////    private void MoveToPointer(PointerEventData eventData)
//////    {
//////        if (_rootCanvas == null) return;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            _rootCanvas.GetComponent<RectTransform>(),
//////            eventData.position,
//////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////                ? null : _rootCanvas.worldCamera,
//////            out Vector2 local);

//////        RectTransform rt = GetComponent<RectTransform>();
//////        if (rt != null) rt.anchoredPosition = local;
//////    }

//////    private Canvas FindRootCanvas()
//////    {
//////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////        return (all == null || all.Length == 0)
//////            ? FindObjectOfType<Canvas>()
//////            : all[all.Length - 1];
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// Attach to your Cannon draggable objects in the village / unit panel.
/////// The unit itself follows the cursor while dragging.
///////
///////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////   Invalid drop → unit snaps back to its original position.
///////
/////// When the cannon is already placed in a CastleUnitDropZone and the player
/////// drags it again:
///////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
///////                                                        linked expansion slot shown.
///////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
///////                                     new expansion slot hidden.
///////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
///////                                     old expansion slot hidden again.
///////
/////// ── Inspector ────────────────────────────────────────────────────────────
///////   unitType          → Cannon or Soldier
///////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
///////                       FALSE : centers the unit at its natural size
///////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class CastleUnitDraggable : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Shared drag state ─────────────────────────────────────────
////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────
////    [Header("Unit Identity")]
////    public CastleUnitType unitType;

////    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
////    public int variantId = 0;

////    [Header("Slot Behaviour")]
////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
////             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
////    public bool stretchToFillSlot = false;

////    [Tooltip("Pixel size of the unit while being dragged.")]
////    public Vector2 dragGhostSize = new Vector2(64f, 64f);

////    // ── Private ───────────────────────────────────────────────────
////    private CanvasGroup _canvasGroup;
////    private Canvas _rootCanvas;
////    private Transform _originalParent;
////    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
////    private Vector2 _originalAnchoredPos;
////    private Vector2 _originalSizeDelta;
////    private static bool _droppedSuccessfully;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////    }

////    // ── Drag ──────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        CurrentlyDragging = this;
////        _droppedSuccessfully = false;

////        // Remember where to return on a failed drop
////        _originalParent = transform.parent;
////        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

////        RectTransform selfRt = GetComponent<RectTransform>();
////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

////        // If the cannon was sitting in a drop zone, free it now:
////        //   • hides the soldier
////        //   • sets HasUnit = false so the zone can accept a new cannon
////        //   • shows the linked expansion slot (if any) so it can be dropped on again
////        _originalZone?.DetachUnit();

////        // Lift to root canvas so the unit renders above all UI panels
////        _rootCanvas = FindRootCanvas();
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////        transform.SetAsLastSibling();

////        // Resize to dragGhostSize while dragging
////        if (selfRt != null)
////        {
////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
////            selfRt.pivot = new Vector2(0.5f, 0.5f);
////            selfRt.sizeDelta = dragGhostSize;
////        }

////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = false;
////            _canvasGroup.alpha = 0.85f;
////        }

////        MoveToPointer(eventData);
////    }

////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = true;
////            _canvasGroup.alpha = 1f;
////        }

////        if (!_droppedSuccessfully)
////        {
////            // Snap back to original parent (zone or panel slot)
////            transform.SetParent(_originalParent, worldPositionStays: false);

////            RectTransform rt = GetComponent<RectTransform>();
////            if (rt != null)
////            {
////                rt.anchoredPosition = _originalAnchoredPos;
////                rt.sizeDelta = _originalSizeDelta;
////            }

////            // Restore the original zone:
////            //   • sets HasUnit = true, shows soldier again
////            //   • hides the linked expansion slot again (cannon is back)
////            _originalZone?.ReattachUnit(this);
////        }

////        _originalZone = null;
////        _originalParent = null;
////        CurrentlyDragging = null;
////        _droppedSuccessfully = false;
////    }

////    // ── Called by CastleUnitDropZone on a successful drop ─────────
////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////    // ── Helpers ───────────────────────────────────────────────────

////    private void MoveToPointer(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _rootCanvas.GetComponent<RectTransform>(),
////            eventData.position,
////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////                ? null : _rootCanvas.worldCamera,
////            out Vector2 local);

////        RectTransform rt = GetComponent<RectTransform>();
////        if (rt != null) rt.anchoredPosition = local;
////    }

////    private Canvas FindRootCanvas()
////    {
////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////        return (all == null || all.Length == 0)
////            ? FindObjectOfType<Canvas>()
////            : all[all.Length - 1];
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Attach to your Cannon draggable objects in the village / unit panel.
///// The unit itself follows the cursor while dragging.
/////
/////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////   Invalid drop → unit snaps back to its original position.
/////
///// When the cannon is already placed in a CastleUnitDropZone and the player
///// drags it again:
/////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
/////                                                        linked expansion slot shown.
/////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
/////                                     new expansion slot hidden.
/////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
/////                                     old expansion slot hidden again.
/////
///// ── Inspector ────────────────────────────────────────────────────────────
/////   unitType          → Cannon or Soldier
/////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
/////                       FALSE : centers the unit at its natural size
/////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class CastleUnitDraggable : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Shared drag state ─────────────────────────────────────────
//    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────
//    [Header("Unit Identity")]
//    public CastleUnitType unitType;

//    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
//    public int variantId = 0;

//    [Header("Slot Behaviour")]
//    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
//             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
//    public bool stretchToFillSlot = false;

//    [Tooltip("Pixel size of the unit while being dragged.")]
//    public Vector2 dragGhostSize = new Vector2(64f, 64f);

//    // ── Private ───────────────────────────────────────────────────
//    private CanvasGroup _canvasGroup;
//    private Canvas _rootCanvas;
//    private Transform _originalParent;
//    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
//    private Vector2 _originalAnchoredPos;
//    private Vector2 _originalSizeDelta;
//    private static bool _droppedSuccessfully;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//    }

//    // ── Drag ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        CurrentlyDragging = this;
//        _droppedSuccessfully = false;

//        // Remember where to return on a failed drop
//        _originalParent = transform.parent;
//        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

//        RectTransform selfRt = GetComponent<RectTransform>();
//        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//        // If the cannon was sitting in a drop zone, free it now:
//        //   • hides the soldier
//        //   • sets HasUnit = false so the zone can accept a new cannon
//        //   • shows the linked expansion slot (if any) so it can be dropped on again
//        _originalZone?.DetachUnit();

//        // Lift to root canvas so the unit renders above all UI panels
//        _rootCanvas = FindRootCanvas();
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        // Resize to dragGhostSize while dragging
//        if (selfRt != null)
//        {
//            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//            selfRt.pivot = new Vector2(0.5f, 0.5f);
//            selfRt.sizeDelta = dragGhostSize;
//        }

//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.alpha = 0.85f;
//        }

//        MoveToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.alpha = 1f;
//        }

//        if (!_droppedSuccessfully)
//        {
//            // Snap back to original parent (zone or panel slot)
//            transform.SetParent(_originalParent, worldPositionStays: false);

//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null)
//            {
//                rt.anchoredPosition = _originalAnchoredPos;
//                rt.sizeDelta = _originalSizeDelta;
//            }

//            // Restore the original zone:
//            //   • sets HasUnit = true, shows soldier again
//            //   • hides the linked expansion slot again (cannon is back)
//            _originalZone?.ReattachUnit(this);
//        }

//        _originalZone = null;
//        _originalParent = null;
//        CurrentlyDragging = null;
//        _droppedSuccessfully = false;
//    }

//    // ── Called by CastleUnitDropZone on a successful drop ─────────
//    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//    // ── Helpers ───────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _rootCanvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                ? null : _rootCanvas.worldCamera,
//            out Vector2 local);

//        RectTransform rt = GetComponent<RectTransform>();
//        if (rt != null) rt.anchoredPosition = local;
//    }

//    private Canvas FindRootCanvas()
//    {
//        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//        return (all == null || all.Length == 0)
//            ? FindObjectOfType<Canvas>()
//            : all[all.Length - 1];
//    }
//}

//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using UnityEngine.EventSystems;

///////////////////// <summary>
///////////////////// Attach to each cannon / soldier icon in the Castle Shop panel.
///////////////////// Dragging this creates a ghost that follows the cursor.
///////////////////// CastleUnitDropZone checks CastleUnitDraggable.CurrentlyDragging on drop.
/////////////////////
///////////////////// ── Inspector ──────────────────────────────────────────────────
/////////////////////   unitType   → Cannon or Soldier
/////////////////////   unitSprite → the sprite shown on the drag ghost (and placed in the slot)
///////////////////// </summary>
//////////////////[RequireComponent(typeof(CanvasGroup))]
//////////////////[RequireComponent(typeof(Image))]
//////////////////public class CastleUnitDraggable : MonoBehaviour,
//////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////////{
//////////////////    // ── Static drag state (shared across all draggables) ──────────
//////////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////    [Header("Unit")]
//////////////////    public CastleUnitType unitType;
//////////////////    public Sprite unitSprite;

//////////////////    // ── Private ───────────────────────────────────────────────────
//////////////////    private CanvasGroup _canvasGroup;
//////////////////    private static GameObject _ghost;
//////////////////    private static Canvas _rootCanvas;

//////////////////    private void Awake()
//////////////////    {
//////////////////        _canvasGroup = GetComponent<CanvasGroup>();

//////////////////        // If no sprite set in inspector, fall back to this Image's sprite
//////////////////        if (unitSprite == null)
//////////////////            unitSprite = GetComponent<Image>().sprite;
//////////////////    }

//////////////////    // ── Drag ──────────────────────────────────────────────────────

//////////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////////    {
//////////////////        CurrentlyDragging = this;
//////////////////        _rootCanvas = FindRootCanvas();

//////////////////        // Fade the source icon slightly
//////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;

//////////////////        // Create ghost image that follows the cursor
//////////////////        if (_rootCanvas != null)
//////////////////        {
//////////////////            _ghost = new GameObject("CastleUnitDragGhost",
//////////////////                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
//////////////////            _ghost.transform.SetParent(_rootCanvas.transform, false);
//////////////////            _ghost.transform.SetAsLastSibling();        // render on top

//////////////////            // Ghost is visible but doesn't block raycasts (so drop zones receive events)
//////////////////            var cg = _ghost.GetComponent<CanvasGroup>();
//////////////////            cg.alpha = 0.80f;
//////////////////            cg.blocksRaycasts = false;

//////////////////            var img = _ghost.GetComponent<Image>();
//////////////////            if (unitSprite != null) { img.sprite = unitSprite; img.preserveAspect = true; }

//////////////////            _ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
//////////////////            MoveGhostTo(eventData);
//////////////////        }
//////////////////    }

//////////////////    public void OnDrag(PointerEventData eventData)
//////////////////    {
//////////////////        MoveGhostTo(eventData);
//////////////////    }

//////////////////    public void OnEndDrag(PointerEventData eventData)
//////////////////    {
//////////////////        // Restore source icon
//////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

//////////////////        DestroyGhost();
//////////////////        CurrentlyDragging = null;
//////////////////    }

//////////////////    // ── Ghost helpers ─────────────────────────────────────────────

//////////////////    private static void MoveGhostTo(PointerEventData eventData)
//////////////////    {
//////////////////        if (_ghost == null || _rootCanvas == null) return;

//////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////////            _rootCanvas.GetComponent<RectTransform>(),
//////////////////            eventData.position,
//////////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////////                ? null : _rootCanvas.worldCamera,
//////////////////            out Vector2 local);

//////////////////        _ghost.GetComponent<RectTransform>().anchoredPosition = local;
//////////////////    }

//////////////////    public static void DestroyGhost()
//////////////////    {
//////////////////        if (_ghost != null) { Object.Destroy(_ghost); _ghost = null; }
//////////////////    }

//////////////////    private Canvas FindRootCanvas()
//////////////////    {
//////////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
//////////////////    }
//////////////////}

////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using UnityEngine.EventSystems;

/////////////////// <summary>
/////////////////// Attach to each cannon / soldier icon in the Castle Shop panel.
/////////////////// Dragging this creates a ghost that follows the cursor; on a successful
/////////////////// drop the unit PREFAB is instantiated inside the CastleUnitDropZone.
///////////////////
/////////////////// ── Inspector ──────────────────────────────────────────────────
///////////////////   unitType    → Cannon or Soldier
///////////////////   unitPrefab  → the prefab that will be instantiated in the drop zone
///////////////////   ghostSprite → (optional) sprite shown on the drag ghost.
///////////////////                 If left empty, the sprite is read from unitPrefab's
///////////////////                 root Image component automatically.
/////////////////// </summary>
////////////////[RequireComponent(typeof(CanvasGroup))]
////////////////[RequireComponent(typeof(Image))]
////////////////public class CastleUnitDraggable : MonoBehaviour,
////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////////{
////////////////    // ── Static drag state (shared across all draggables) ──────────
////////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////    [Header("Unit")]
////////////////    public CastleUnitType unitType;

////////////////    [Tooltip("The prefab that will be spawned inside the drop zone on a successful drop.")]
////////////////    public GameObject unitPrefab;

////////////////    [Tooltip("Sprite displayed on the semi-transparent drag ghost. " +
////////////////             "Leave empty to auto-read from unitPrefab's root Image.")]
////////////////    public Sprite ghostSprite;

////////////////    // ── Private ───────────────────────────────────────────────────
////////////////    private CanvasGroup _canvasGroup;
////////////////    private static GameObject _ghost;
////////////////    private static Canvas _rootCanvas;

////////////////    private void Awake()
////////////////    {
////////////////        _canvasGroup = GetComponent<CanvasGroup>();
////////////////    }

////////////////    // ── Drag handlers ─────────────────────────────────────────────

////////////////    public void OnBeginDrag(PointerEventData eventData)
////////////////    {
////////////////        if (unitPrefab == null)
////////////////        {
////////////////            Debug.LogWarning("[CastleUnitDraggable] unitPrefab is not assigned!");
////////////////            return;
////////////////        }

////////////////        CurrentlyDragging = this;
////////////////        _rootCanvas = FindRootCanvas();

////////////////        // Fade the source icon slightly while dragging
////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;

////////////////        // ── Create semi-transparent ghost that follows the cursor ──
////////////////        if (_rootCanvas != null)
////////////////        {
////////////////            _ghost = new GameObject("CastleUnitDragGhost",
////////////////                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
////////////////            _ghost.transform.SetParent(_rootCanvas.transform, false);
////////////////            _ghost.transform.SetAsLastSibling();    // render above everything

////////////////            // Ghost is visible but does NOT block raycasts so drop zones
////////////////            // still receive pointer events
////////////////            var cg = _ghost.GetComponent<CanvasGroup>();
////////////////            cg.alpha = 0.80f;
////////////////            cg.blocksRaycasts = false;

////////////////            // Resolve ghost sprite: explicit override → prefab's root Image → null
////////////////            Sprite sprite = ghostSprite;
////////////////            if (sprite == null)
////////////////            {
////////////////                Image prefabImg = unitPrefab.GetComponent<Image>();
////////////////                if (prefabImg != null) sprite = prefabImg.sprite;
////////////////            }

////////////////            var img = _ghost.GetComponent<Image>();
////////////////            if (sprite != null) { img.sprite = sprite; img.preserveAspect = true; }

////////////////            _ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
////////////////            MoveGhostTo(eventData);
////////////////        }
////////////////    }

////////////////    public void OnDrag(PointerEventData eventData)
////////////////    {
////////////////        MoveGhostTo(eventData);
////////////////    }

////////////////    public void OnEndDrag(PointerEventData eventData)
////////////////    {
////////////////        // Restore source icon opacity
////////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

////////////////        DestroyGhost();
////////////////        CurrentlyDragging = null;
////////////////    }

////////////////    // ── Ghost helpers ─────────────────────────────────────────────

////////////////    private static void MoveGhostTo(PointerEventData eventData)
////////////////    {
////////////////        if (_ghost == null || _rootCanvas == null) return;

////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////            _rootCanvas.GetComponent<RectTransform>(),
////////////////            eventData.position,
////////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////                ? null : _rootCanvas.worldCamera,
////////////////            out Vector2 local);

////////////////        _ghost.GetComponent<RectTransform>().anchoredPosition = local;
////////////////    }

////////////////    public static void DestroyGhost()
////////////////    {
////////////////        if (_ghost != null) { Object.Destroy(_ghost); _ghost = null; }
////////////////    }

////////////////    // ── Utility ───────────────────────────────────────────────────

////////////////    private Canvas FindRootCanvas()
////////////////    {
////////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
////////////////    }
////////////////}


//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using UnityEngine.EventSystems;

///////////////// <summary>
///////////////// Attach this to your Cannon Spawner and Soldier Spawner objects.
/////////////////
///////////////// When the player begins a drag, a live instance of <see cref="unitPrefab"/>
///////////////// is created and follows the cursor. On a successful drop the
///////////////// <see cref="CastleUnitDropZone"/> reparents that instance into the slot.
///////////////// If the drag ends without a valid drop the instance is destroyed.
/////////////////
///////////////// ── Inspector ──────────────────────────────────────────────────
/////////////////   unitType   → Cannon or Soldier
/////////////////   unitPrefab → the prefab to spawn and drag
///////////////// </summary>
//////////////[RequireComponent(typeof(CanvasGroup))]
//////////////public class CastleUnitDraggable : MonoBehaviour,
//////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////{
//////////////    // ── Shared drag state ─────────────────────────────────────────
//////////////    /// <summary>The live prefab instance currently being dragged.</summary>
//////////////    public static GameObject CurrentDragInstance { get; private set; }

//////////////    /// <summary>The draggable (spawner) that started the current drag.</summary>
//////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////    [Header("Unit")]
//////////////    public CastleUnitType unitType;

//////////////    [Tooltip("Prefab instantiated when drag starts. " +
//////////////             "Placed in the drop zone on success, destroyed on failure.")]
//////////////    public GameObject unitPrefab;

//////////////    // ── Private ───────────────────────────────────────────────────
//////////////    private CanvasGroup _canvasGroup;
//////////////    private Canvas _rootCanvas;

//////////////    // Set to true by CastleUnitDropZone when the drop succeeds,
//////////////    // so OnEndDrag knows NOT to destroy the instance.
//////////////    private static bool _droppedSuccessfully;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        _canvasGroup = GetComponent<CanvasGroup>();
//////////////    }

//////////////    // ── Drag handlers ─────────────────────────────────────────────

//////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////    {
//////////////        if (unitPrefab == null)
//////////////        {
//////////////            Debug.LogWarning($"[CastleUnitDraggable] unitPrefab not assigned on '{name}'!");
//////////////            return;
//////////////        }

//////////////        _rootCanvas = FindRootCanvas();
//////////////        CurrentlyDragging = this;
//////////////        _droppedSuccessfully = false;

//////////////        // Spawn the live unit instance on the root canvas so it floats
//////////////        // above everything else while being dragged.
//////////////        CurrentDragInstance = Instantiate(unitPrefab, _rootCanvas.transform);
//////////////        CurrentDragInstance.transform.SetAsLastSibling();

//////////////        // Disable raycasts on the instance so pointer events pass
//////////////        // through to the drop zones underneath.
//////////////        CanvasGroup cg = CurrentDragInstance.GetComponent<CanvasGroup>();
//////////////        if (cg == null) cg = CurrentDragInstance.AddComponent<CanvasGroup>();
//////////////        cg.blocksRaycasts = false;
//////////////        cg.alpha = 0.85f;

//////////////        // Position and size the floating instance
//////////////        RectTransform rt = CurrentDragInstance.GetComponent<RectTransform>();
//////////////        if (rt != null)
//////////////        {
//////////////            rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////            rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////            rt.pivot = new Vector2(0.5f, 0.5f);
//////////////            rt.sizeDelta = new Vector2(64f, 64f);
//////////////        }

//////////////        MoveInstanceTo(eventData);

//////////////        // Dim the spawner icon during the drag
//////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
//////////////    }

//////////////    public void OnDrag(PointerEventData eventData)
//////////////    {
//////////////        MoveInstanceTo(eventData);
//////////////    }

//////////////    public void OnEndDrag(PointerEventData eventData)
//////////////    {
//////////////        // Restore spawner icon
//////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

//////////////        // Drop zone did NOT accept the unit — destroy the floating instance
//////////////        if (!_droppedSuccessfully && CurrentDragInstance != null)
//////////////            Destroy(CurrentDragInstance);

//////////////        CurrentDragInstance = null;
//////////////        CurrentlyDragging = null;
//////////////        _droppedSuccessfully = false;
//////////////    }

//////////////    // ── Called by CastleUnitDropZone on a successful drop ─────────

//////////////    /// <summary>
//////////////    /// Signals that the drop zone accepted and reparented the instance,
//////////////    /// so OnEndDrag will not destroy it.
//////////////    /// </summary>
//////////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////////////    // ── Helpers ───────────────────────────────────────────────────

//////////////    private void MoveInstanceTo(PointerEventData eventData)
//////////////    {
//////////////        if (CurrentDragInstance == null || _rootCanvas == null) return;

//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            _rootCanvas.GetComponent<RectTransform>(),
//////////////            eventData.position,
//////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////                ? null : _rootCanvas.worldCamera,
//////////////            out Vector2 local);

//////////////        CurrentDragInstance.GetComponent<RectTransform>().anchoredPosition = local;
//////////////    }

//////////////    private Canvas FindRootCanvas()
//////////////    {
//////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using UnityEngine.EventSystems;

/////////////// <summary>
/////////////// Attach to each spawner object (Cannon Spawner x3, Soldier Spawner x1, etc.).
///////////////
/////////////// On drag start a live instance of <see cref="unitPrefab"/> is created and
/////////////// follows the cursor. On a successful drop <see cref="CastleUnitDropZone"/>
/////////////// reparents that instance into the slot. On a failed drop it is destroyed.
///////////////
/////////////// ── Inspector ────────────────────────────────────────────────────────────
///////////////   unitType   → Cannon or Soldier  (controls which drop zone accepts it)
///////////////   variantId  → 0 / 1 / 2 … distinguishes cannon variants from each other
///////////////                (e.g. 0 = Light Cannon, 1 = Medium Cannon, 2 = Heavy Cannon)
///////////////                Ignored for units that have only one variant (e.g. Soldier = 0)
///////////////   unitPrefab → the actual prefab instantiated during drag and placed on drop
/////////////// </summary>
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class CastleUnitDraggable : MonoBehaviour,
////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////{
////////////    // ── Shared static drag state ──────────────────────────────────
////////////    /// <summary>The live prefab instance currently being dragged.</summary>
////////////    public static GameObject CurrentDragInstance { get; private set; }

////////////    /// <summary>The spawner that initiated the current drag.</summary>
////////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    [Header("Unit Identity")]
////////////    public CastleUnitType unitType;

////////////    [Tooltip("Distinguishes variants of the same unit type.\n" +
////////////             "Example — Cannon spawners:\n" +
////////////             "  Spawner A → variantId 0  (Light Cannon)\n" +
////////////             "  Spawner B → variantId 1  (Medium Cannon)\n" +
////////////             "  Spawner C → variantId 2  (Heavy Cannon)\n" +
////////////             "Leave at 0 for units with only one variant (e.g. Soldier).")]
////////////    public int variantId = 0;

////////////    [Tooltip("Prefab instantiated on drag start. Placed in the drop zone " +
////////////             "on success, destroyed on failure.")]
////////////    public GameObject unitPrefab;

////////////    // ── Private ───────────────────────────────────────────────────
////////////    private CanvasGroup _canvasGroup;
////////////    private Canvas _rootCanvas;

////////////    // CastleUnitDropZone sets this to true when it accepts the drop,
////////////    // so OnEndDrag knows not to destroy the instance.
////////////    private static bool _droppedSuccessfully;

////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        _canvasGroup = GetComponent<CanvasGroup>();
////////////    }

////////////    // ── Drag handlers ─────────────────────────────────────────────

////////////    public void OnBeginDrag(PointerEventData eventData)
////////////    {
////////////        if (unitPrefab == null)
////////////        {
////////////            Debug.LogWarning($"[CastleUnitDraggable] unitPrefab not assigned on '{name}'!");
////////////            return;
////////////        }

////////////        _rootCanvas = FindRootCanvas();
////////////        CurrentlyDragging = this;
////////////        _droppedSuccessfully = false;

////////////        // Spawn the live unit on the root canvas so it floats above everything.
////////////        CurrentDragInstance = Instantiate(unitPrefab, _rootCanvas.transform);
////////////        CurrentDragInstance.transform.SetAsLastSibling();

////////////        // Disable raycasts on the floating instance so pointer events reach
////////////        // the drop zones underneath it.
////////////        CanvasGroup cg = CurrentDragInstance.GetComponent<CanvasGroup>();
////////////        if (cg == null) cg = CurrentDragInstance.AddComponent<CanvasGroup>();
////////////        cg.blocksRaycasts = false;
////////////        cg.alpha = 0.85f;

////////////        // Initial size of the floating instance while dragging
////////////        RectTransform rt = CurrentDragInstance.GetComponent<RectTransform>();
////////////        if (rt != null)
////////////        {
////////////            rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////            rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////            rt.pivot = new Vector2(0.5f, 0.5f);
////////////            rt.sizeDelta = new Vector2(64f, 64f);
////////////        }

////////////        MoveInstanceTo(eventData);

////////////        // Dim the spawner icon during the drag
////////////        if (_canvasGroup != null) _canvasGroup.alpha = 0.45f;
////////////    }

////////////    public void OnDrag(PointerEventData eventData)
////////////    {
////////////        MoveInstanceTo(eventData);
////////////    }

////////////    public void OnEndDrag(PointerEventData eventData)
////////////    {
////////////        // Restore spawner icon
////////////        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

////////////        // No valid drop → destroy the floating instance
////////////        if (!_droppedSuccessfully && CurrentDragInstance != null)
////////////            Destroy(CurrentDragInstance);

////////////        CurrentDragInstance = null;
////////////        CurrentlyDragging = null;
////////////        _droppedSuccessfully = false;
////////////    }

////////////    // ── Called by CastleUnitDropZone on a successful drop ─────────

////////////    /// <summary>
////////////    /// Signals that the drop zone accepted and reparented the instance,
////////////    /// so OnEndDrag will NOT destroy it.
////////////    /// </summary>
////////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////////////    // ── Helpers ───────────────────────────────────────────────────

////////////    private void MoveInstanceTo(PointerEventData eventData)
////////////    {
////////////        if (CurrentDragInstance == null || _rootCanvas == null) return;

////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            _rootCanvas.GetComponent<RectTransform>(),
////////////            eventData.position,
////////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////                ? null : _rootCanvas.worldCamera,
////////////            out Vector2 local);

////////////        CurrentDragInstance.GetComponent<RectTransform>().anchoredPosition = local;
////////////    }

////////////    private Canvas FindRootCanvas()
////////////    {
////////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////////        return (all == null || all.Length == 0) ? null : all[all.Length - 1];
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using UnityEngine.EventSystems;

///////////// <summary>
///////////// Attach to your Cannon and Soldier prefabs.
///////////// The unit itself follows the cursor while dragging.
///////////// - Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////////// - Invalid drop → unit snaps back to where it came from.
/////////////
///////////// Inspector:
/////////////   unitType  → Cannon or Soldier
/////////////   variantId → 0 / 1 / 2 … (set this on each cannon variant prefab)
///////////// </summary>
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class CastleUnitDraggable : MonoBehaviour,
//////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////{
//////////    // ── Shared drag state ─────────────────────────────────────────
//////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    public CastleUnitType unitType;

//////////    [Tooltip("Distinguishes cannon variants (0 = Light, 1 = Medium, 2 = Heavy). " +
//////////             "Leave 0 for Soldier.")]
//////////    public int variantId = 0;

//////////    // ── Private ───────────────────────────────────────────────────
//////////    private CanvasGroup _canvasGroup;
//////////    private Canvas _rootCanvas;
//////////    private Transform _originalParent;
//////////    private Vector2 _originalPosition;
//////////    private static bool _droppedSuccessfully;

//////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        _canvasGroup = GetComponent<CanvasGroup>();
//////////    }

//////////    // ── Drag ──────────────────────────────────────────────────────

//////////    public void OnBeginDrag(PointerEventData eventData)
//////////    {
//////////        CurrentlyDragging = this;
//////////        _droppedSuccessfully = false;

//////////        // Remember where to return if the drop fails
//////////        _originalParent = transform.parent;
//////////        _originalPosition = GetComponent<RectTransform>().anchoredPosition;

//////////        // Move to root canvas so it renders above all other UI
//////////        _rootCanvas = FindRootCanvas();
//////////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////////        transform.SetAsLastSibling();

//////////        // Let pointer events pass through to the drop zones below
//////////        _canvasGroup.blocksRaycasts = false;
//////////        _canvasGroup.alpha = 0.85f;
//////////    }

//////////    public void OnDrag(PointerEventData eventData)
//////////    {
//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            _rootCanvas.GetComponent<RectTransform>(),
//////////            eventData.position,
//////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////                ? null : _rootCanvas.worldCamera,
//////////            out Vector2 local);

//////////        GetComponent<RectTransform>().anchoredPosition = local;
//////////    }

//////////    public void OnEndDrag(PointerEventData eventData)
//////////    {
//////////        _canvasGroup.blocksRaycasts = true;
//////////        _canvasGroup.alpha = 1f;

//////////        if (!_droppedSuccessfully)
//////////        {
//////////            // Return to original parent and position
//////////            transform.SetParent(_originalParent, worldPositionStays: false);
//////////            GetComponent<RectTransform>().anchoredPosition = _originalPosition;
//////////        }

//////////        CurrentlyDragging = null;
//////////        _droppedSuccessfully = false;
//////////    }

//////////    // ── Called by CastleUnitDropZone on success ───────────────────

//////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////////    // ── Helpers ───────────────────────────────────────────────────

//////////    private Canvas FindRootCanvas()
//////////    {
//////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////////        return (all == null || all.Length == 0)
//////////            ? FindObjectOfType<Canvas>()
//////////            : all[all.Length - 1];
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;
////////using UnityEngine.EventSystems;

/////////// <summary>
/////////// Attach to your Cannon and Soldier prefabs.
/////////// The unit itself follows the cursor while dragging.
///////////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////////   Invalid drop → unit snaps back to its original position in the village panel.
///////////
/////////// Inspector:
///////////   unitType  → Cannon or Soldier
///////////   variantId → 0 / 1 / 2 for each cannon variant (leave 0 for Soldier)
/////////// </summary>
////////[RequireComponent(typeof(CanvasGroup))]
////////public class CastleUnitDraggable : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    // ── Shared drag state ─────────────────────────────────────────
////////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////////    // ── Inspector ─────────────────────────────────────────────────
////////    public CastleUnitType unitType;

////////    [Tooltip("0 = Light Cannon, 1 = Medium Cannon, 2 = Heavy Cannon. Leave 0 for Soldier.")]
////////    public int variantId = 0;

////////    // ── Private ───────────────────────────────────────────────────
////////    private CanvasGroup _canvasGroup;
////////    private Canvas _rootCanvas;
////////    private Transform _originalParent;
////////    private Vector2 _originalPosition;
////////    private static bool _droppedSuccessfully;

////////    private void Awake()
////////    {
////////        _canvasGroup = GetComponent<CanvasGroup>();
////////    }

////////    // ── Drag ──────────────────────────────────────────────────────

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        CurrentlyDragging = this;
////////        _droppedSuccessfully = false;

////////        // Remember where to return on a failed drop
////////        _originalParent = transform.parent;
////////        _originalPosition = GetComponent<RectTransform>().anchoredPosition;

////////        // Lift to root canvas so it renders above all UI panels
////////        _rootCanvas = FindRootCanvas();
////////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////////        transform.SetAsLastSibling();

////////        // Pass pointer events through to drop zones beneath
////////        _canvasGroup.blocksRaycasts = false;
////////        _canvasGroup.alpha = 0.85f;
////////    }

////////    public void OnDrag(PointerEventData eventData)
////////    {
////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            _rootCanvas.GetComponent<RectTransform>(),
////////            eventData.position,
////////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////                ? null : _rootCanvas.worldCamera,
////////            out Vector2 local);

////////        GetComponent<RectTransform>().anchoredPosition = local;
////////    }

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        _canvasGroup.blocksRaycasts = true;
////////        _canvasGroup.alpha = 1f;

////////        if (!_droppedSuccessfully)
////////        {
////////            // Snap back to the village panel
////////            transform.SetParent(_originalParent, worldPositionStays: false);
////////            GetComponent<RectTransform>().anchoredPosition = _originalPosition;
////////        }

////////        CurrentlyDragging = null;
////////        _droppedSuccessfully = false;
////////    }

////////    // ── Called by CastleUnitDropZone on a successful drop ─────────
////////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////////    // ── Helpers ───────────────────────────────────────────────────
////////    private Canvas FindRootCanvas()
////////    {
////////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////////        return (all == null || all.Length == 0)
////////            ? FindObjectOfType<Canvas>()
////////            : all[all.Length - 1];
////////    }
////////}


//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// Attach to your Cannon and Soldier draggable objects in the village / unit panel.
///////// The unit itself follows the cursor while dragging.
/////////
/////////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////////   Invalid drop → unit snaps back to its original position.
/////////
///////// ── Inspector ────────────────────────────────────────────────────────────
/////////   unitType          → Cannon or Soldier
/////////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone (simple icon prefabs)
/////////                       FALSE : centers the unit at its natural size (customized / animated prefabs)
/////////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///////// </summary>
//////[RequireComponent(typeof(CanvasGroup))]
//////public class CastleUnitDraggable : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Shared drag state ─────────────────────────────────────────
//////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Header("Unit Identity")]
//////    public CastleUnitType unitType;

//////    [Tooltip("0 = Light Cannon / default Soldier. " +
//////             "Increment for each cannon variant (1 = Medium, 2 = Heavy).")]
//////    public int variantId = 0;

//////    [Header("Slot Behaviour")]
//////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle (good for simple icon prefabs).\n" +
//////             "FALSE → unit is centered at its natural size inside the zone " +
//////             "(use this for customized / animated soldier prefabs to prevent broken layouts).")]
//////    public bool stretchToFillSlot = false;   // OFF by default — safest for customized prefabs.

//////    [Tooltip("Pixel size of the unit while being dragged.")]
//////    public Vector2 dragGhostSize = new Vector2(64f, 64f);

//////    // ── Private ───────────────────────────────────────────────────
//////    private CanvasGroup _canvasGroup;
//////    private Canvas _rootCanvas;
//////    private Transform _originalParent;
//////    private Vector2 _originalAnchoredPos;
//////    private Vector2 _originalSizeDelta;
//////    private static bool _droppedSuccessfully;

//////    private void Awake()
//////    {
//////        _canvasGroup = GetComponent<CanvasGroup>();
//////    }

//////    // ── Drag ──────────────────────────────────────────────────────

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        CurrentlyDragging = this;
//////        _droppedSuccessfully = false;

//////        // Remember original location for snap-back on a failed drop
//////        _originalParent = transform.parent;
//////        RectTransform selfRt = GetComponent<RectTransform>();
//////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//////        // Lift to root canvas so the unit renders above all UI panels
//////        _rootCanvas = FindRootCanvas();
//////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//////        transform.SetAsLastSibling();

//////        // Resize to dragGhostSize while dragging so all unit types look consistent
//////        if (selfRt != null)
//////        {
//////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//////            selfRt.pivot = new Vector2(0.5f, 0.5f);
//////            selfRt.sizeDelta = dragGhostSize;
//////        }

//////        // Pass pointer events through to the drop zones beneath
//////        if (_canvasGroup != null)
//////        {
//////            _canvasGroup.blocksRaycasts = false;
//////            _canvasGroup.alpha = 0.85f;
//////        }

//////        MoveToPointer(eventData);
//////    }

//////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        if (_canvasGroup != null)
//////        {
//////            _canvasGroup.blocksRaycasts = true;
//////            _canvasGroup.alpha = 1f;
//////        }

//////        if (!_droppedSuccessfully)
//////        {
//////            // Snap back — restore parent, position, and original size
//////            transform.SetParent(_originalParent, worldPositionStays: false);
//////            RectTransform rt = GetComponent<RectTransform>();
//////            if (rt != null)
//////            {
//////                rt.anchoredPosition = _originalAnchoredPos;
//////                rt.sizeDelta = _originalSizeDelta;
//////            }
//////        }

//////        CurrentlyDragging = null;
//////        _droppedSuccessfully = false;
//////    }

//////    // ── Called by CastleUnitDropZone on a successful drop ─────────
//////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//////    // ── Helpers ───────────────────────────────────────────────────

//////    private void MoveToPointer(PointerEventData eventData)
//////    {
//////        if (_rootCanvas == null) return;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            _rootCanvas.GetComponent<RectTransform>(),
//////            eventData.position,
//////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////                ? null : _rootCanvas.worldCamera,
//////            out Vector2 local);

//////        RectTransform rt = GetComponent<RectTransform>();
//////        if (rt != null) rt.anchoredPosition = local;
//////    }

//////    private Canvas FindRootCanvas()
//////    {
//////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//////        return (all == null || all.Length == 0)
//////            ? FindObjectOfType<Canvas>()
//////            : all[all.Length - 1];
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// Attach to your Cannon draggable objects in the village / unit panel.
/////// The unit itself follows the cursor while dragging.
///////
///////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///////   Invalid drop → unit snaps back to its original position.
///////
/////// When the cannon is already placed in a CastleUnitDropZone and the player
/////// drags it again:
///////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed.
///////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there.
///////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored.
///////
/////// ── Inspector ────────────────────────────────────────────────────────────
///////   unitType          → Cannon or Soldier
///////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone (simple icon prefabs)
///////                       FALSE : centers the unit at its natural size (customised / animated prefabs)
///////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/////// </summary>
////[RequireComponent(typeof(CanvasGroup))]
////public class CastleUnitDraggable : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Shared drag state ─────────────────────────────────────────
////    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────
////    [Header("Unit Identity")]
////    public CastleUnitType unitType;

////    [Tooltip("0 = Light Cannon / default Soldier. " +
////             "Increment for each cannon variant (1 = Medium, 2 = Heavy).")]
////    public int variantId = 0;

////    [Header("Slot Behaviour")]
////    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle (good for simple icon prefabs).\n" +
////             "FALSE → unit is centered at its natural size inside the zone " +
////             "(use this for customised / animated soldier prefabs to prevent broken layouts).")]
////    public bool stretchToFillSlot = false;

////    [Tooltip("Pixel size of the unit while being dragged.")]
////    public Vector2 dragGhostSize = new Vector2(64f, 64f);

////    // ── Private ───────────────────────────────────────────────────
////    private CanvasGroup _canvasGroup;
////    private Canvas _rootCanvas;

////    // Saved so we can snap back or re-notify the original zone on failed drops
////    private Transform _originalParent;
////    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a zone
////    private Vector2 _originalAnchoredPos;
////    private Vector2 _originalSizeDelta;

////    private static bool _droppedSuccessfully;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _canvasGroup = GetComponent<CanvasGroup>();
////    }

////    // ── Drag ──────────────────────────────────────────────────────

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        CurrentlyDragging = this;
////        _droppedSuccessfully = false;

////        // Remember where to return on a failed drop
////        _originalParent = transform.parent;
////        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

////        RectTransform selfRt = GetComponent<RectTransform>();
////        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
////        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

////        // If this cannon was sitting inside a drop zone, free that zone now
////        // so its soldier hides and it can accept a new cannon.
////        _originalZone?.DetachUnit();

////        // Lift to root canvas so the unit renders above all UI panels
////        _rootCanvas = FindRootCanvas();
////        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
////        transform.SetAsLastSibling();

////        // Resize to dragGhostSize while dragging
////        if (selfRt != null)
////        {
////            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
////            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
////            selfRt.pivot = new Vector2(0.5f, 0.5f);
////            selfRt.sizeDelta = dragGhostSize;
////        }

////        // Pass pointer events through to drop zones beneath
////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = false;
////            _canvasGroup.alpha = 0.85f;
////        }

////        MoveToPointer(eventData);
////    }

////    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        if (_canvasGroup != null)
////        {
////            _canvasGroup.blocksRaycasts = true;
////            _canvasGroup.alpha = 1f;
////        }

////        if (!_droppedSuccessfully)
////        {
////            // Snap back to the original parent (zone or panel slot)
////            transform.SetParent(_originalParent, worldPositionStays: false);

////            RectTransform rt = GetComponent<RectTransform>();
////            if (rt != null)
////            {
////                rt.anchoredPosition = _originalAnchoredPos;
////                rt.sizeDelta = _originalSizeDelta;
////            }

////            // If the cannon came from a zone, restore that zone's state
////            // (re-shows the soldier, marks HasUnit = true again)
////            _originalZone?.ReattachUnit(this);
////        }

////        _originalZone = null;
////        _originalParent = null;
////        CurrentlyDragging = null;
////        _droppedSuccessfully = false;
////    }

////    // ── Called by CastleUnitDropZone on a successful drop ─────────
////    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

////    // ── Helpers ───────────────────────────────────────────────────

////    private void MoveToPointer(PointerEventData eventData)
////    {
////        if (_rootCanvas == null) return;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _rootCanvas.GetComponent<RectTransform>(),
////            eventData.position,
////            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////                ? null : _rootCanvas.worldCamera,
////            out Vector2 local);

////        RectTransform rt = GetComponent<RectTransform>();
////        if (rt != null) rt.anchoredPosition = local;
////    }

////    private Canvas FindRootCanvas()
////    {
////        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
////        return (all == null || all.Length == 0)
////            ? FindObjectOfType<Canvas>()
////            : all[all.Length - 1];
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Attach to your Cannon draggable objects in the village / unit panel.
///// The unit itself follows the cursor while dragging.
/////
/////   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
/////   Invalid drop → unit snaps back to its original position.
/////
///// When the cannon is already placed in a CastleUnitDropZone and the player
///// drags it again:
/////   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
/////                                                        linked expansion slot shown.
/////   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
/////                                     new expansion slot hidden.
/////   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
/////                                     old expansion slot hidden again.
/////
///// ── Inspector ────────────────────────────────────────────────────────────
/////   unitType          → Cannon or Soldier
/////   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
/////   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
/////                       FALSE : centers the unit at its natural size
/////   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class CastleUnitDraggable : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Shared drag state ─────────────────────────────────────────
//    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────
//    [Header("Unit Identity")]
//    public CastleUnitType unitType;

//    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
//    public int variantId = 0;

//    [Header("Slot Behaviour")]
//    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
//             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
//    public bool stretchToFillSlot = false;

//    [Tooltip("Pixel size of the unit while being dragged.")]
//    public Vector2 dragGhostSize = new Vector2(64f, 64f);

//    // ── Private ───────────────────────────────────────────────────
//    private CanvasGroup _canvasGroup;
//    private Canvas _rootCanvas;
//    private Transform _originalParent;
//    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
//    private Vector2 _originalAnchoredPos;
//    private Vector2 _originalSizeDelta;
//    private static bool _droppedSuccessfully;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//    }

//    // ── Drag ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        CurrentlyDragging = this;
//        _droppedSuccessfully = false;

//        // Remember where to return on a failed drop
//        _originalParent = transform.parent;
//        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

//        RectTransform selfRt = GetComponent<RectTransform>();
//        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
//        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

//        // If the cannon was sitting in a drop zone, free it now:
//        //   • hides the soldier
//        //   • sets HasUnit = false so the zone can accept a new cannon
//        //   • shows the linked expansion slot (if any) so it can be dropped on again
//        _originalZone?.DetachUnit();

//        // Lift to root canvas so the unit renders above all UI panels
//        _rootCanvas = FindRootCanvas();
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        // Resize to dragGhostSize while dragging
//        if (selfRt != null)
//        {
//            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
//            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
//            selfRt.pivot = new Vector2(0.5f, 0.5f);
//            selfRt.sizeDelta = dragGhostSize;
//        }

//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.alpha = 0.85f;
//        }

//        MoveToPointer(eventData);
//    }

//    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_canvasGroup != null)
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.alpha = 1f;
//        }

//        if (!_droppedSuccessfully)
//        {
//            // Snap back to original parent (zone or panel slot)
//            transform.SetParent(_originalParent, worldPositionStays: false);

//            RectTransform rt = GetComponent<RectTransform>();
//            if (rt != null)
//            {
//                rt.anchoredPosition = _originalAnchoredPos;
//                rt.sizeDelta = _originalSizeDelta;
//            }

//            // Restore the original zone:
//            //   • sets HasUnit = true, shows soldier again
//            //   • hides the linked expansion slot again (cannon is back)
//            _originalZone?.ReattachUnit(this);
//        }

//        _originalZone = null;
//        _originalParent = null;
//        CurrentlyDragging = null;
//        _droppedSuccessfully = false;
//    }

//    // ── Called by CastleUnitDropZone on a successful drop ─────────
//    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

//    // ── Helpers ───────────────────────────────────────────────────

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _rootCanvas.GetComponent<RectTransform>(),
//            eventData.position,
//            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                ? null : _rootCanvas.worldCamera,
//            out Vector2 local);

//        RectTransform rt = GetComponent<RectTransform>();
//        if (rt != null) rt.anchoredPosition = local;
//    }

//    private Canvas FindRootCanvas()
//    {
//        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
//        return (all == null || all.Length == 0)
//            ? FindObjectOfType<Canvas>()
//            : all[all.Length - 1];
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to your Cannon draggable objects in the village / unit panel.
/// The unit itself follows the cursor while dragging.
///
///   Valid drop   → CastleUnitDropZone reparents it onto the castle block.
///   Invalid drop → unit snaps back to its original position.
///
/// When the cannon is already placed in a CastleUnitDropZone and the player
/// drags it again:
///   • OnBeginDrag calls DetachUnit() on the old zone  → soldier hidden, zone freed,
///                                                        linked expansion slot shown.
///   • Successful drop on a new zone → PlaceUnit() there → soldier shown there,
///                                     new expansion slot hidden.
///   • Failed drop (snap-back)       → ReattachUnit() on old zone → soldier restored,
///                                     old expansion slot hidden again.
///
/// ── Inspector ────────────────────────────────────────────────────────────
///   unitType          → Cannon or Soldier
///   variantId         → 0/1/2 for cannon variants; leave 0 for Soldier
///   stretchToFillSlot → TRUE  : stretches the unit to fill the drop zone
///                       FALSE : centers the unit at its natural size
///   dragGhostSize     → pixel size of the unit while being dragged (default 64×64)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CastleUnitDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Shared drag state ─────────────────────────────────────────
    public static CastleUnitDraggable CurrentlyDragging { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Unit Identity")]
    public CastleUnitType unitType;

    [Tooltip("0 = Light Cannon / default. Increment for each cannon variant.")]
    public int variantId = 0;

    [Header("Slot Behaviour")]
    [Tooltip("TRUE  → unit stretches to fill the drop-zone rectangle.\n" +
             "FALSE → unit is centered at its natural size (safer for animated prefabs).")]
    public bool stretchToFillSlot = false;

    [Tooltip("Pixel size of the unit while being dragged.")]
    public Vector2 dragGhostSize = new Vector2(64f, 64f);

    // ── Private ───────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _originalParent;
    private CastleUnitDropZone _originalZone;       // non-null only if dragged from a placed zone
    private Vector2 _originalAnchoredPos;
    private Vector2 _originalSizeDelta;
    private static bool _droppedSuccessfully;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // FIX — The cannon GameObject must be a raycast target so Unity's
        // EventSystem can deliver IBeginDragHandler / IDragHandler / IEndDragHandler
        // events to it directly.
        //
        // Root cause: the root Image on the cannon prefab often has
        //   Source Image = None  →  it is invisible, so developers sometimes
        //   accidentally uncheck "Raycast Target", or the field is left at its
        //   default (true) but then Unity silently strips the hit-area because
        //   the Image has no sprite and alpha = 0.  Either way the pointer event
        //   falls through to CannonZone's background Image, which has no drag
        //   handlers, so dragging a placed cannon does nothing.
        //
        // Solution: get (or add) an Image on this GameObject and force
        //   raycastTarget = true.  If no Image exists we add an invisible one
        //   that acts purely as an EventSystem hit-area.
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);   // fully transparent — hit-area only
        }
        img.raycastTarget = true;
    }

    // ── Drag ──────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        CurrentlyDragging = this;
        _droppedSuccessfully = false;

        // Remember where to return on a failed drop
        _originalParent = transform.parent;
        _originalZone = _originalParent?.GetComponent<CastleUnitDropZone>();

        RectTransform selfRt = GetComponent<RectTransform>();
        _originalAnchoredPos = selfRt != null ? selfRt.anchoredPosition : Vector2.zero;
        _originalSizeDelta = selfRt != null ? selfRt.sizeDelta : dragGhostSize;

        // If the cannon was sitting in a drop zone, free it now:
        //   • hides the soldier
        //   • sets HasUnit = false so the zone can accept a new cannon
        //   • shows the linked expansion slot (if any) so it can be dropped on again
        _originalZone?.DetachUnit();

        // Lift to root canvas so the unit renders above all UI panels
        _rootCanvas = FindRootCanvas();
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        // Resize to dragGhostSize while dragging
        if (selfRt != null)
        {
            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
            selfRt.pivot = new Vector2(0.5f, 0.5f);
            selfRt.sizeDelta = dragGhostSize;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;
        }

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }

        if (!_droppedSuccessfully)
        {
            // Snap back to original parent (zone or panel slot)
            transform.SetParent(_originalParent, worldPositionStays: false);

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = _originalAnchoredPos;
                rt.sizeDelta = _originalSizeDelta;
            }

            // Restore the original zone:
            //   • sets HasUnit = true, shows soldier again
            //   • hides the linked expansion slot again (cannon is back)
            _originalZone?.ReattachUnit(this);
        }

        _originalZone = null;
        _originalParent = null;
        CurrentlyDragging = null;
        _droppedSuccessfully = false;
    }

    // ── Called by CastleUnitDropZone on a successful drop ─────────
    public static void NotifyDropSucceeded() => _droppedSuccessfully = true;

    // ── Helpers ───────────────────────────────────────────────────

    private void MoveToPointer(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _rootCanvas.worldCamera,
            out Vector2 local);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = local;
    }

    private Canvas FindRootCanvas()
    {
        Canvas[] all = GetComponentsInParent<Canvas>(includeInactive: false);
        return (all == null || all.Length == 0)
            ? FindObjectOfType<Canvas>()
            : all[all.Length - 1];
    }
}