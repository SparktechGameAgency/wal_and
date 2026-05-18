//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// Drop zone for a Cannon unit on an exposed castle block.
/////////
///////// When a cannon is placed via an ExpansionSlot, that slot registers itself
///////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
///////// hides / shows that slot as the cannon moves:
/////////
/////////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
/////////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
/////////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
/////////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
/////////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
/////////
///////// Child hierarchy (auto-wired by name in Awake):
/////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////   ├── Highlight       glow frame shown during any drag hover
/////////   └── Soldier         Image — hidden by default, shown when cannon is placed
///////// </summary>
//////[RequireComponent(typeof(Image))]
//////public class CastleUnitDropZone : MonoBehaviour,
//////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////    public CastleUnitType acceptedType;

//////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////    // ── Auto-wired ────────────────────────────────────────────────
//////    private Image _bg;
//////    private GameObject _emptyVisual;
//////    private GameObject _highlight;
//////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

//////    // ── State ─────────────────────────────────────────────────────
//////    public bool HasUnit { get; private set; }
//////    public int PlacedVariantId { get; private set; } = -1;

//////    /// <summary>
//////    /// The ExpansionSlot that was used to place the cannon here.
//////    /// Set by ExpansionSlot.OnDrop, or pre-linked by ExpansionSlot.Init.
//////    /// The zone uses it to show/hide that slot when the cannon arrives,
//////    /// leaves, or is destroyed.
//////    /// Null when no expansion slot is associated with this zone.
//////    /// </summary>
//////    public ExpansionSlot LinkedExpansionSlot { get; set; }

//////    private GameObject _placedInstance;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _bg = GetComponent<Image>();
//////        _bg.color = normalColor;
//////        _bg.raycastTarget = true;

//////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////        _highlight = transform.Find("Highlight")?.gameObject;
//////        _soldierImage = transform.Find("Soldier")?.gameObject;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _soldierImage?.SetActive(false);

//////        // FIX 1 — The Soldier child is a pure visual overlay.
//////        // It must NOT be a raycast target, otherwise it blocks pointer events
//////        // from reaching the cannon that sits on top of it, making the
//////        // placed cannon impossible to drag.
//////        if (_soldierImage != null)
//////        {
//////            Image soldierImg = _soldierImage.GetComponent<Image>();
//////            if (soldierImg != null)
//////                soldierImg.raycastTarget = false;
//////        }
//////    }

//////    // ── Standard drag-drop path (direct drop onto this zone) ──────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////        if (unit == null)
//////        {
//////            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
//////            return;
//////        }
//////        if (unit.unitType != acceptedType)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
//////                      $"zone accepts {acceptedType}.");
//////            return;
//////        }
//////        if (HasUnit)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
//////            return;
//////        }

//////        PlaceUnit(unit);
//////        CastleUnitDraggable.NotifyDropSucceeded();
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
//////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//////    /// </summary>
//////    public void PlaceUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null || HasUnit) return;

//////        unit.transform.SetParent(transform, worldPositionStays: false);

//////        RectTransform rt = unit.GetComponent<RectTransform>();
//////        if (rt != null)
//////        {
//////            if (unit.stretchToFillSlot)
//////            {
//////                rt.anchorMin = Vector2.zero;
//////                rt.anchorMax = Vector2.one;
//////                rt.offsetMin = Vector2.zero;
//////                rt.offsetMax = Vector2.zero;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            else
//////            {
//////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////                rt.pivot = new Vector2(0.5f, 0.5f);
//////                rt.sizeDelta = centeredUnitSize;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            rt.SetAsLastSibling();
//////        }

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        // FIX — Re-assert raycastTarget = true on the cannon's own Image after
//////        // reparenting.  This is the authoritative moment the cannon becomes
//////        // interactive again; ensuring it here means the fix holds even if
//////        // Awake's guarantee is somehow lost (e.g. prefab variant overrides).
//////        Image unitImg = unit.GetComponent<Image>();
//////        if (unitImg != null) unitImg.raycastTarget = true;

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////                  $"placed on '{transform.parent?.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
//////    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
//////    /// Does NOT destroy the cannon — it is still alive being dragged.
//////    /// </summary>
//////    public void DetachUnit()
//////    {
//////        _placedInstance = null;
//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Reveal the expansion slot above so the cannon (or another) can be
//////        // dropped there again.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            Debug.Log($"[CastleUnitDropZone] Detached — expansion slot restored.");

//////            // FIX 3 — Clear the reference after restoring so it is not stale
//////            // if a different slot links to this zone in the future.
//////            LinkedExpansionSlot = null;
//////        }
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
//////    /// snaps back here. Restores zone state exactly as before the drag started.
//////    /// </summary>
//////    public void ReattachUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null) return;

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        // Hide the expansion slot again — cannon is back on this block.
//////        // Re-link it so the next DetachUnit can restore it correctly.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(false);
//////            Debug.Log($"[CastleUnitDropZone] Reattached — expansion slot hidden again.");
//////        }
//////    }

//////    /// <summary>
//////    /// Called by ExpansionSlot.OnDrop to restore the link after a successful
//////    /// snap-back so the slot can be revealed again on the next detach.
//////    /// </summary>
//////    public void RestoreLinkedExpansionSlot(ExpansionSlot slot)
//////    {
//////        LinkedExpansionSlot = slot;
//////    }

//////    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
//////    public void RemoveUnit()
//////    {
//////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Restore the expansion slot — the block is now cannon-free.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            LinkedExpansionSlot = null;
//////            Debug.Log($"[CastleUnitDropZone] Removed — expansion slot restored.");
//////        }
//////    }

//////    // ── Hover ─────────────────────────────────────────────────────

//////    public void OnPointerEnter(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////        if (unit == null) return;

//////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////        _highlight?.SetActive(true);
//////        _emptyVisual?.SetActive(valid);
//////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////    }

//////    public void OnPointerExit(PointerEventData eventData)
//////    {
//////        _highlight?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _bg.color = normalColor;
//////    }
//////}


////////////////////////////////using UnityEngine;
////////////////////////////////using UnityEngine.UI;
////////////////////////////////using UnityEngine.EventSystems;

/////////////////////////////////// <summary>
/////////////////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////////////////
/////////////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
///////////////////////////////////   CastleUnitDropZone  ← this script + Image (transparent, raycast target)
///////////////////////////////////   ├── UnitIcon        Image — shown when a unit is placed (displays the sprite)
///////////////////////////////////   ├── EmptyVisual     GameObject — shown when the zone is empty
///////////////////////////////////   └── Highlight       GameObject — shown while the correct unit type is dragged over
/////////////////////////////////// </summary>
////////////////////////////////[RequireComponent(typeof(Image))]
////////////////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////////////////    IDropHandler,
////////////////////////////////    IPointerEnterHandler,
////////////////////////////////    IPointerExitHandler
////////////////////////////////{
////////////////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////////////////    [Header("Zone Settings")]
////////////////////////////////    public CastleUnitType acceptedType;

////////////////////////////////    [Header("Colors")]
////////////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////////////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////////////////    // ── Auto-wired children ───────────────────────────────────────
////////////////////////////////    private Image _bg;
////////////////////////////////    private Image _unitIcon;
////////////////////////////////    private GameObject _emptyVisual;
////////////////////////////////    private GameObject _highlight;

////////////////////////////////    // ── State ─────────────────────────────────────────────────────
////////////////////////////////    public bool HasUnit { get; private set; }
////////////////////////////////    private Sprite _placedSprite;

////////////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////////////////    private void Awake()
////////////////////////////////    {
////////////////////////////////        _bg = GetComponent<Image>();
////////////////////////////////        _bg.color = normalColor;

////////////////////////////////        // Auto-wire children by name
////////////////////////////////        var iconT = transform.Find("UnitIcon");
////////////////////////////////        var emptyT = transform.Find("EmptyVisual");
////////////////////////////////        var hlT = transform.Find("Highlight");

////////////////////////////////        if (iconT != null) _unitIcon = iconT.GetComponent<Image>();
////////////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
////////////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

////////////////////////////////        RefreshVisuals();
////////////////////////////////    }

////////////////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////////////////    {
////////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////////        if (dragged == null) return;
////////////////////////////////        if (dragged.unitType != acceptedType) return;   // wrong unit type
////////////////////////////////        if (HasUnit) return;                            // slot already full

////////////////////////////////        PlaceUnit(dragged.unitSprite);

////////////////////////////////        // The drag source icon stays in the panel (unlimited supply).
////////////////////////////////        // Destroy the ghost manually here since OnEndDrag fires after OnDrop.
////////////////////////////////        CastleUnitDraggable.DestroyGhost();

////////////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} placed on {transform.parent.name}.");
////////////////////////////////    }

////////////////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////////////////    {
////////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////////        if (dragged == null) return;

////////////////////////////////        if (_highlight != null) _highlight.SetActive(true);
////////////////////////////////        _bg.color = (dragged.unitType == acceptedType && !HasUnit)
////////////////////////////////            ? hoverValidColor
////////////////////////////////            : hoverInvalidColor;
////////////////////////////////    }

////////////////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////////////////    {
////////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////////        _bg.color = normalColor;
////////////////////////////////    }

////////////////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////////////////    public void PlaceUnit(Sprite sprite)
////////////////////////////////    {
////////////////////////////////        HasUnit = true;
////////////////////////////////        _placedSprite = sprite;
////////////////////////////////        RefreshVisuals();
////////////////////////////////    }

////////////////////////////////    public void RemoveUnit()
////////////////////////////////    {
////////////////////////////////        HasUnit = false;
////////////////////////////////        _placedSprite = null;
////////////////////////////////        RefreshVisuals();
////////////////////////////////    }

////////////////////////////////    // ── Visuals ───────────────────────────────────────────────────

////////////////////////////////    private void RefreshVisuals()
////////////////////////////////    {
////////////////////////////////        if (_unitIcon != null)
////////////////////////////////        {
////////////////////////////////            _unitIcon.gameObject.SetActive(HasUnit);
////////////////////////////////            if (HasUnit && _placedSprite != null) _unitIcon.sprite = _placedSprite;
////////////////////////////////        }

////////////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
////////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////////        _bg.color = normalColor;
////////////////////////////////    }
////////////////////////////////}

//////////////////////////////using UnityEngine;
//////////////////////////////using UnityEngine.UI;
//////////////////////////////using UnityEngine.EventSystems;

///////////////////////////////// <summary>
///////////////////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////////////
///////////////////////////////// On a valid drop the unit PREFAB is instantiated as a child of this zone
///////////////////////////////// and scaled to fill it. Removing clears and destroys the instance.
/////////////////////////////////
///////////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
/////////////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
/////////////////////////////////   ├── EmptyVisual      GameObject — shown when the zone is empty (e.g. "+" icon)
/////////////////////////////////   └── Highlight        GameObject — glow frame while a valid drag hovers
/////////////////////////////////
///////////////////////////////// Note: there is no longer a static UnitIcon Image child — the spawned
///////////////////////////////// prefab itself provides the visual.
///////////////////////////////// </summary>
//////////////////////////////[RequireComponent(typeof(Image))]
//////////////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////////////    IDropHandler,
//////////////////////////////    IPointerEnterHandler,
//////////////////////////////    IPointerExitHandler
//////////////////////////////{
//////////////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////////////    [Header("Zone Settings")]
//////////////////////////////    public CastleUnitType acceptedType;

//////////////////////////////    [Header("Colors")]
//////////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//////////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////////////    // ── Auto-wired children ───────────────────────────────────────
//////////////////////////////    private Image _bg;
//////////////////////////////    private GameObject _emptyVisual;   // shown when the zone is empty
//////////////////////////////    private GameObject _highlight;     // hover glow

//////////////////////////////    // ── Runtime state ─────────────────────────────────────────────
//////////////////////////////    public bool HasUnit { get; private set; }
//////////////////////////////    private GameObject _placedInstance;  // the instantiated unit prefab

//////////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////////////    private void Awake()
//////////////////////////////    {
//////////////////////////////        _bg = GetComponent<Image>();
//////////////////////////////        _bg.color = normalColor;

//////////////////////////////        // Wire optional UI children by name
//////////////////////////////        var emptyT = transform.Find("EmptyVisual");
//////////////////////////////        var hlT = transform.Find("Highlight");

//////////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
//////////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////////        if (dragged == null) return;
//////////////////////////////        if (dragged.unitType != acceptedType) return; // wrong unit type
//////////////////////////////        if (HasUnit) return; // zone already occupied
//////////////////////////////        if (dragged.unitPrefab == null) return;

//////////////////////////////        PlaceUnit(dragged.unitPrefab);

//////////////////////////////        // Destroy the ghost immediately (OnEndDrag fires after OnDrop, but
//////////////////////////////        // DestroyGhost is safe to call multiple times)
//////////////////////////////        CastleUnitDraggable.DestroyGhost();

//////////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} prefab placed on {transform.parent.name}.");
//////////////////////////////    }

//////////////////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////////        if (dragged == null) return;

//////////////////////////////        if (_highlight != null) _highlight.SetActive(true);

//////////////////////////////        _bg.color = (dragged.unitType == acceptedType && !HasUnit)
//////////////////////////////            ? hoverValidColor
//////////////////////////////            : hoverInvalidColor;
//////////////////////////////    }

//////////////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////////        _bg.color = normalColor;
//////////////////////////////    }

//////////////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////////////    /// <summary>
//////////////////////////////    /// Instantiates the unit prefab as a child of this zone, stretching it
//////////////////////////////    /// to fill the zone's RectTransform.
//////////////////////////////    /// </summary>
//////////////////////////////    public void PlaceUnit(GameObject prefab)
//////////////////////////////    {
//////////////////////////////        if (HasUnit) RemoveUnit(); // replace if somehow called twice

//////////////////////////////        _placedInstance = Instantiate(prefab, transform);

//////////////////////////////        // Stretch to fill the drop zone
//////////////////////////////        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
//////////////////////////////        if (rt != null)
//////////////////////////////        {
//////////////////////////////            rt.anchorMin = Vector2.zero;
//////////////////////////////            rt.anchorMax = Vector2.one;
//////////////////////////////            rt.offsetMin = Vector2.zero;
//////////////////////////////            rt.offsetMax = Vector2.zero;
//////////////////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////////////////            rt.SetAsLastSibling();
//////////////////////////////        }

//////////////////////////////        HasUnit = true;
//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    /// <summary>
//////////////////////////////    /// Destroys the placed unit prefab instance and resets the zone.
//////////////////////////////    /// </summary>
//////////////////////////////    public void RemoveUnit()
//////////////////////////////    {
//////////////////////////////        if (_placedInstance != null)
//////////////////////////////        {
//////////////////////////////            Destroy(_placedInstance);
//////////////////////////////            _placedInstance = null;
//////////////////////////////        }

//////////////////////////////        HasUnit = false;
//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    // ── Visuals ───────────────────────────────────────────────────

//////////////////////////////    private void RefreshVisuals()
//////////////////////////////    {
//////////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
//////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////////        _bg.color = normalColor;
//////////////////////////////    }
//////////////////////////////}

////////////////////////////using UnityEngine;
////////////////////////////using UnityEngine.UI;
////////////////////////////using UnityEngine.EventSystems;

/////////////////////////////// <summary>
/////////////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////////////
/////////////////////////////// On a valid drop, the live prefab instance being dragged (held by
/////////////////////////////// <see cref="CastleUnitDraggable.CurrentDragInstance"/>) is reparented
/////////////////////////////// here and stretched to fill the zone. No new prefab is instantiated.
///////////////////////////////
/////////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
///////////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
///////////////////////////////   ├── EmptyVisual      GameObject — shown while the zone is empty
///////////////////////////////   └── Highlight        GameObject — glow shown while a valid drag hovers
/////////////////////////////// </summary>
////////////////////////////[RequireComponent(typeof(Image))]
////////////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////////////    IDropHandler,
////////////////////////////    IPointerEnterHandler,
////////////////////////////    IPointerExitHandler
////////////////////////////{
////////////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////////////    [Header("Zone Settings")]
////////////////////////////    public CastleUnitType acceptedType;

////////////////////////////    [Header("Colors")]
////////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////////////    // ── Auto-wired children ───────────────────────────────────────
////////////////////////////    private Image _bg;
////////////////////////////    private GameObject _emptyVisual;  // "+" or empty-state indicator
////////////////////////////    private GameObject _highlight;    // hover glow frame

////////////////////////////    // ── Runtime state ─────────────────────────────────────────────
////////////////////////////    public bool HasUnit { get; private set; }
////////////////////////////    private GameObject _placedInstance;  // the reparented unit object

////////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////////////    private void Awake()
////////////////////////////    {
////////////////////////////        _bg = GetComponent<Image>();
////////////////////////////        _bg.color = normalColor;

////////////////////////////        var emptyT = transform.Find("EmptyVisual");
////////////////////////////        var hlT = transform.Find("Highlight");

////////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
////////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

////////////////////////////        RefreshVisuals();
////////////////////////////    }

////////////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////        if (source == null) return;
////////////////////////////        if (source.unitType != acceptedType) return; // wrong type
////////////////////////////        if (HasUnit) return; // zone full

////////////////////////////        GameObject instance = CastleUnitDraggable.CurrentDragInstance;
////////////////////////////        if (instance == null) return;

////////////////////////////        // ── Reparent the live instance into this zone ──────────────
////////////////////////////        instance.transform.SetParent(transform, worldPositionStays: false);

////////////////////////////        // Re-enable raycasts now that it is placed (so it can receive
////////////////////////////        // future interactions if needed)
////////////////////////////        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
////////////////////////////        if (cg != null)
////////////////////////////        {
////////////////////////////            cg.blocksRaycasts = true;
////////////////////////////            cg.alpha = 1f;
////////////////////////////        }

////////////////////////////        // Stretch to fill the drop zone
////////////////////////////        RectTransform rt = instance.GetComponent<RectTransform>();
////////////////////////////        if (rt != null)
////////////////////////////        {
////////////////////////////            rt.anchorMin = Vector2.zero;
////////////////////////////            rt.anchorMax = Vector2.one;
////////////////////////////            rt.offsetMin = Vector2.zero;
////////////////////////////            rt.offsetMax = Vector2.zero;
////////////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////////////            rt.SetAsLastSibling();
////////////////////////////        }

////////////////////////////        _placedInstance = instance;
////////////////////////////        HasUnit = true;

////////////////////////////        // Tell the draggable not to destroy the instance in OnEndDrag
////////////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

////////////////////////////        RefreshVisuals();

////////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} placed on '{transform.parent.name}'.");
////////////////////////////    }

////////////////////////////    // ── Hover feedback ────────────────────────────────────────────

////////////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////        if (source == null) return;

////////////////////////////        if (_highlight != null) _highlight.SetActive(true);

////////////////////////////        _bg.color = (source.unitType == acceptedType && !HasUnit)
////////////////////////////            ? hoverValidColor
////////////////////////////            : hoverInvalidColor;
////////////////////////////    }

////////////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////        _bg.color = normalColor;
////////////////////////////    }

////////////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////////////    /// <summary>Destroys the placed unit and resets the zone.</summary>
////////////////////////////    public void RemoveUnit()
////////////////////////////    {
////////////////////////////        if (_placedInstance != null)
////////////////////////////        {
////////////////////////////            Destroy(_placedInstance);
////////////////////////////            _placedInstance = null;
////////////////////////////        }

////////////////////////////        HasUnit = false;
////////////////////////////        RefreshVisuals();
////////////////////////////    }

////////////////////////////    // ── Visuals ───────────────────────────────────────────────────

////////////////////////////    private void RefreshVisuals()
////////////////////////////    {
////////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////        _bg.color = normalColor;
////////////////////////////    }
////////////////////////////}

//////////////////////////using UnityEngine;
//////////////////////////using UnityEngine.UI;
//////////////////////////using UnityEngine.EventSystems;

///////////////////////////// <summary>
///////////////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////////
///////////////////////////// On a valid drop the live prefab instance from the spawner is reparented
///////////////////////////// here and stretched to fill the zone. The placed unit's variant id is
///////////////////////////// stored in <see cref="PlacedVariantId"/> for gameplay queries.
/////////////////////////////
///////////////////////////// ── Child hierarchy (auto-wired by name in Awake) ───────────────
/////////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
/////////////////////////////   ├── EmptyVisual      GameObject — visible when the zone is empty
/////////////////////////////   └── Highlight        GameObject — glow shown during a valid hover
///////////////////////////// </summary>
//////////////////////////[RequireComponent(typeof(Image))]
//////////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////////    IDropHandler,
//////////////////////////    IPointerEnterHandler,
//////////////////////////    IPointerExitHandler
//////////////////////////{
//////////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////////    [Header("Zone Settings")]
//////////////////////////    public CastleUnitType acceptedType;

//////////////////////////    [Header("Colors")]
//////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////////    // ── Auto-wired children ───────────────────────────────────────
//////////////////////////    private Image _bg;
//////////////////////////    private GameObject _emptyVisual;
//////////////////////////    private GameObject _highlight;

//////////////////////////    // ── Runtime state ─────────────────────────────────────────────
//////////////////////////    /// <summary>True when a unit prefab is currently placed in this zone.</summary>
//////////////////////////    public bool HasUnit { get; private set; }

//////////////////////////    /// <summary>
//////////////////////////    /// The variant id of the placed unit (matches <see cref="CastleUnitDraggable.variantId"/>).
//////////////////////////    /// -1 when the zone is empty.
//////////////////////////    /// Example: Cannon zone holds variantId 2 → Heavy Cannon is placed here.
//////////////////////////    /// </summary>
//////////////////////////    public int PlacedVariantId { get; private set; } = -1;

//////////////////////////    private GameObject _placedInstance;

//////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////////    private void Awake()
//////////////////////////    {
//////////////////////////        _bg = GetComponent<Image>();
//////////////////////////        _bg.color = normalColor;

//////////////////////////        var emptyT = transform.Find("EmptyVisual");
//////////////////////////        var hlT = transform.Find("Highlight");

//////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
//////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

//////////////////////////        RefreshVisuals();
//////////////////////////    }

//////////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////        if (source == null) return;
//////////////////////////        if (source.unitType != acceptedType) return; // wrong unit type
//////////////////////////        if (HasUnit) return; // zone already occupied

//////////////////////////        GameObject instance = CastleUnitDraggable.CurrentDragInstance;
//////////////////////////        if (instance == null) return;

//////////////////////////        // ── Reparent the live instance into this zone ──────────────
//////////////////////////        instance.transform.SetParent(transform, worldPositionStays: false);

//////////////////////////        // Re-enable raycasts now that the unit is placed
//////////////////////////        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
//////////////////////////        if (cg != null)
//////////////////////////        {
//////////////////////////            cg.blocksRaycasts = true;
//////////////////////////            cg.alpha = 1f;
//////////////////////////        }

//////////////////////////        // Stretch to fill the drop zone
//////////////////////////        RectTransform rt = instance.GetComponent<RectTransform>();
//////////////////////////        if (rt != null)
//////////////////////////        {
//////////////////////////            rt.anchorMin = Vector2.zero;
//////////////////////////            rt.anchorMax = Vector2.one;
//////////////////////////            rt.offsetMin = Vector2.zero;
//////////////////////////            rt.offsetMax = Vector2.zero;
//////////////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////////////            rt.SetAsLastSibling();
//////////////////////////        }

//////////////////////////        _placedInstance = instance;
//////////////////////////        HasUnit = true;
//////////////////////////        PlacedVariantId = source.variantId;

//////////////////////////        // Tell the spawner not to destroy the instance in OnEndDrag
//////////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

//////////////////////////        RefreshVisuals();

//////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variantId={PlacedVariantId}) " +
//////////////////////////                  $"placed on '{transform.parent.name}'.");
//////////////////////////    }

//////////////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////        if (source == null) return;

//////////////////////////        if (_highlight != null) _highlight.SetActive(true);

//////////////////////////        _bg.color = (source.unitType == acceptedType && !HasUnit)
//////////////////////////            ? hoverValidColor
//////////////////////////            : hoverInvalidColor;
//////////////////////////    }

//////////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////        _bg.color = normalColor;
//////////////////////////    }

//////////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////////    /// <summary>Destroys the placed unit instance and resets the zone.</summary>
//////////////////////////    public void RemoveUnit()
//////////////////////////    {
//////////////////////////        if (_placedInstance != null)
//////////////////////////        {
//////////////////////////            Destroy(_placedInstance);
//////////////////////////            _placedInstance = null;
//////////////////////////        }

//////////////////////////        HasUnit = false;
//////////////////////////        PlacedVariantId = -1;
//////////////////////////        RefreshVisuals();
//////////////////////////    }

//////////////////////////    // ── Visuals ───────────────────────────────────────────────────

//////////////////////////    private void RefreshVisuals()
//////////////////////////    {
//////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
//////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////        _bg.color = normalColor;
//////////////////////////    }
//////////////////////////}

////////////////////////using UnityEngine;
////////////////////////using UnityEngine.UI;
////////////////////////using UnityEngine.EventSystems;

/////////////////////////// <summary>
/////////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////////
/////////////////////////// On a valid drop the dragged unit is reparented here and stretched to fill
/////////////////////////// the zone. PlacedVariantId records which cannon variant (0/1/2) was placed.
///////////////////////////
/////////////////////////// Child hierarchy (auto-wired by name):
///////////////////////////   CastleUnitDropZone  ← this script + Image (transparent, raycast target)
///////////////////////////   ├── EmptyVisual     shown while the zone is empty
///////////////////////////   └── Highlight       glow shown during a valid hover
/////////////////////////// </summary>
////////////////////////[RequireComponent(typeof(Image))]
////////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////////////////{
////////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////////    public CastleUnitType acceptedType;

////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////////////////    private Image _bg;
////////////////////////    private GameObject _emptyVisual;
////////////////////////    private GameObject _highlight;

////////////////////////    // ── State ─────────────────────────────────────────────────────
////////////////////////    public bool HasUnit { get; private set; }
////////////////////////    public int PlacedVariantId { get; private set; } = -1;

////////////////////////    private GameObject _placedInstance;

////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////////    private void Awake()
////////////////////////    {
////////////////////////        _bg = GetComponent<Image>();
////////////////////////        _bg.color = normalColor;

////////////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////////////////        RefreshVisuals();
////////////////////////    }

////////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////////    {
////////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////////        if (unit == null) return;
////////////////////////        if (unit.unitType != acceptedType) return;
////////////////////////        if (HasUnit) return;

////////////////////////        // Reparent the unit into this zone and stretch it to fill
////////////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////////////////        if (rt != null)
////////////////////////        {
////////////////////////            rt.anchorMin = Vector2.zero;
////////////////////////            rt.anchorMax = Vector2.one;
////////////////////////            rt.offsetMin = Vector2.zero;
////////////////////////            rt.offsetMax = Vector2.zero;
////////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////////        }

////////////////////////        _placedInstance = unit.gameObject;
////////////////////////        HasUnit = true;
////////////////////////        PlacedVariantId = unit.variantId;

////////////////////////        CastleUnitDraggable.NotifyDropSucceeded();
////////////////////////        RefreshVisuals();

////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////////////////                  $"placed on '{transform.parent.name}'.");
////////////////////////    }

////////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////////    {
////////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////////        if (unit == null) return;

////////////////////////        _highlight?.SetActive(true);
////////////////////////        _bg.color = (unit.unitType == acceptedType && !HasUnit)
////////////////////////            ? hoverValidColor : hoverInvalidColor;
////////////////////////    }

////////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////////    {
////////////////////////        _highlight?.SetActive(false);
////////////////////////        _bg.color = normalColor;
////////////////////////    }

////////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////////    public void RemoveUnit()
////////////////////////    {
////////////////////////        if (_placedInstance != null)
////////////////////////        {
////////////////////////            Destroy(_placedInstance);
////////////////////////            _placedInstance = null;
////////////////////////        }

////////////////////////        HasUnit = false;
////////////////////////        PlacedVariantId = -1;
////////////////////////        RefreshVisuals();
////////////////////////    }

////////////////////////    // ── Visuals ───────────────────────────────────────────────────

////////////////////////    private void RefreshVisuals()
////////////////////////    {
////////////////////////        _emptyVisual?.SetActive(!HasUnit);
////////////////////////        _highlight?.SetActive(false);
////////////////////////        _bg.color = normalColor;
////////////////////////    }
////////////////////////}

//////////////////////using UnityEngine;
//////////////////////using UnityEngine.UI;
//////////////////////using UnityEngine.EventSystems;

///////////////////////// <summary>
///////////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////
///////////////////////// Visual behaviour:
/////////////////////////   • Empty, nothing being dragged  → zone is completely invisible
/////////////////////////   • Compatible unit dragged over  → green highlight + EmptyVisual hint
/////////////////////////   • Incompatible / zone full      → red highlight
/////////////////////////   • Unit placed                   → placed prefab visible, zone invisible
/////////////////////////
///////////////////////// Child hierarchy (auto-wired by name):
/////////////////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////////////////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////////////////////   └── Highlight       glow frame shown during any drag hover
///////////////////////// </summary>
//////////////////////[RequireComponent(typeof(Image))]
//////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////////////{
//////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////    public CastleUnitType acceptedType;

//////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0f);    // fully invisible
//////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////////////    private Image _bg;
//////////////////////    private GameObject _emptyVisual;   // hint shown only on valid hover
//////////////////////    private GameObject _highlight;     // glow frame

//////////////////////    // ── State ─────────────────────────────────────────────────────
//////////////////////    public bool HasUnit { get; private set; }
//////////////////////    public int PlacedVariantId { get; private set; } = -1;

//////////////////////    private GameObject _placedInstance;

//////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////    private void Awake()
//////////////////////    {
//////////////////////        _bg = GetComponent<Image>();
//////////////////////        _bg.color = normalColor;

//////////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////////////        // Both hidden until a drag interaction begins
//////////////////////        _emptyVisual?.SetActive(false);
//////////////////////        _highlight?.SetActive(false);
//////////////////////    }

//////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////    {
//////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////////        if (unit == null) return;
//////////////////////        if (unit.unitType != acceptedType) return;
//////////////////////        if (HasUnit) return;

//////////////////////        // Reparent the dragged unit into this zone
//////////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////////////        if (rt != null)
//////////////////////        {
//////////////////////            rt.anchorMin = Vector2.zero;
//////////////////////            rt.anchorMax = Vector2.one;
//////////////////////            rt.offsetMin = Vector2.zero;
//////////////////////            rt.offsetMax = Vector2.zero;
//////////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////////            rt.SetAsLastSibling();
//////////////////////        }

//////////////////////        // Restore full opacity and raycasts now it is settled
//////////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////////////////        _placedInstance = unit.gameObject;
//////////////////////        HasUnit = true;
//////////////////////        PlacedVariantId = unit.variantId;

//////////////////////        // Hide all zone chrome — the unit prefab is the only visible thing
//////////////////////        _emptyVisual?.SetActive(false);
//////////////////////        _highlight?.SetActive(false);
//////////////////////        _bg.color = normalColor;

//////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

//////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////////////                  $"placed on '{transform.parent.name}'.");
//////////////////////    }

//////////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////    {
//////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////////        if (unit == null) return;

//////////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;

//////////////////////        _highlight?.SetActive(true);
//////////////////////        _emptyVisual?.SetActive(valid);          // hint only when valid
//////////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////////////////    }

//////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////    {
//////////////////////        _highlight?.SetActive(false);
//////////////////////        _emptyVisual?.SetActive(false);
//////////////////////        _bg.color = normalColor;
//////////////////////    }

//////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////    /// <summary>Destroys the placed unit and resets this zone to empty.</summary>
//////////////////////    public void RemoveUnit()
//////////////////////    {
//////////////////////        if (_placedInstance != null)
//////////////////////        {
//////////////////////            Destroy(_placedInstance);
//////////////////////            _placedInstance = null;
//////////////////////        }

//////////////////////        HasUnit = false;
//////////////////////        PlacedVariantId = -1;

//////////////////////        _emptyVisual?.SetActive(false);
//////////////////////        _highlight?.SetActive(false);
//////////////////////        _bg.color = normalColor;
//////////////////////    }
//////////////////////}

////////////////////using UnityEngine;
////////////////////using UnityEngine.UI;
////////////////////using UnityEngine.EventSystems;

/////////////////////// <summary>
/////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////
/////////////////////// FIX APPLIED: normalColor alpha was 0f, which causes Unity to skip
/////////////////////// raycasts on transparent Images — making OnDrop / OnPointerEnter
/////////////////////// never fire. Changed to 0.01f (imperceptibly visible, fully raycasted).
///////////////////////
/////////////////////// Child hierarchy (auto-wired by name):
///////////////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////////////////////   ├── EmptyVisual     hint shown only during a valid drag hover
///////////////////////   └── Highlight       glow frame shown during any drag hover
/////////////////////// </summary>
////////////////////[RequireComponent(typeof(Image))]
////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////////////{
////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////    public CastleUnitType acceptedType;

////////////////////    // FIX: alpha was 0f — Unity skips raycasts on fully transparent Images.
////////////////////    // 0.01f is invisible to the eye but Unity WILL raycast it.
////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////////////    private Image _bg;
////////////////////    private GameObject _emptyVisual;
////////////////////    private GameObject _highlight;

////////////////////    // ── State ─────────────────────────────────────────────────────
////////////////////    public bool HasUnit { get; private set; }
////////////////////    public int PlacedVariantId { get; private set; } = -1;

////////////////////    private GameObject _placedInstance;

////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////    private void Awake()
////////////////////    {
////////////////////        _bg = GetComponent<Image>();
////////////////////        _bg.color = normalColor;

////////////////////        // Guarantee raycasts are on — this must be true for OnDrop to fire.
////////////////////        _bg.raycastTarget = true;

////////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////    }

////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////    {
////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////        if (unit == null)
////////////////////        {
////////////////////            Debug.LogWarning("[CastleUnitDropZone] OnDrop fired but CurrentlyDragging is null.");
////////////////////            return;
////////////////////        }
////////////////////        if (unit.unitType != acceptedType)
////////////////////        {
////////////////////            Debug.Log($"[CastleUnitDropZone] Wrong type: got {unit.unitType}, need {acceptedType}.");
////////////////////            return;
////////////////////        }
////////////////////        if (HasUnit)
////////////////////        {
////////////////////            Debug.Log($"[CastleUnitDropZone] Zone already occupied.");
////////////////////            return;
////////////////////        }

////////////////////        // Reparent the dragged unit into this zone
////////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////////////        if (rt != null)
////////////////////        {
////////////////////            rt.anchorMin = Vector2.zero;
////////////////////            rt.anchorMax = Vector2.one;
////////////////////            rt.offsetMin = Vector2.zero;
////////////////////            rt.offsetMax = Vector2.zero;
////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////            rt.SetAsLastSibling();
////////////////////        }

////////////////////        // Re-enable raycasts and full opacity now the unit is settled
////////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////////////        _placedInstance = unit.gameObject;
////////////////////        HasUnit = true;
////////////////////        PlacedVariantId = unit.variantId;

////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////        _bg.color = normalColor;

////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////////////                  $"placed on '{transform.parent?.name}'.");
////////////////////    }

////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////    {
////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////        if (unit == null) return;

////////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;

////////////////////        _highlight?.SetActive(true);
////////////////////        _emptyVisual?.SetActive(valid);
////////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////////////    }

////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////    {
////////////////////        _highlight?.SetActive(false);
////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _bg.color = normalColor;
////////////////////    }

////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////    /// <summary>Destroys the placed unit and resets this zone to empty.</summary>
////////////////////    public void RemoveUnit()
////////////////////    {
////////////////////        if (_placedInstance != null)
////////////////////        {
////////////////////            Destroy(_placedInstance);
////////////////////            _placedInstance = null;
////////////////////        }

////////////////////        HasUnit = false;
////////////////////        PlacedVariantId = -1;

////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////        _bg.color = normalColor;
////////////////////    }
////////////////////}

//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using UnityEngine.EventSystems;

///////////////////// <summary>
///////////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
/////////////////////
///////////////////// acceptedType must be public so GridCell.FindDropZoneForType() can read it.
///////////////////// PlaceUnit(CastleUnitDraggable) is public so ExpansionSlot can call it
///////////////////// directly when routing a unit drop to the block below the expansion slot.
///////////////////// </summary>
//////////////////[RequireComponent(typeof(Image))]
//////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////////{
//////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////    public CastleUnitType acceptedType; // public — read by FindDropZoneForType

//////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f); // near-invisible but raycasts
//////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////////    private Image _bg;
//////////////////    private GameObject _emptyVisual;
//////////////////    private GameObject _highlight;

//////////////////    // ── State ─────────────────────────────────────────────────────
//////////////////    public bool HasUnit { get; private set; }
//////////////////    public int PlacedVariantId { get; private set; } = -1;
//////////////////    private GameObject _placedInstance;

//////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////    private void Awake()
//////////////////    {
//////////////////        _bg = GetComponent<Image>();
//////////////////        _bg.color = normalColor;
//////////////////        _bg.raycastTarget = true;

//////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////    }

//////////////////    // ── Standard drag-drop path ───────────────────────────────────

//////////////////    public void OnDrop(PointerEventData eventData)
//////////////////    {
//////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////        if (unit == null || unit.unitType != acceptedType || HasUnit) return;

//////////////////        PlaceUnit(unit);
//////////////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////////////    }

//////////////////    // ── Public: also called by ExpansionSlot ──────────────────────

//////////////////    /// <summary>
//////////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
//////////////////    /// Called from OnDrop (normal path) and from ExpansionSlot.OnDrop
//////////////////    /// (unit dropped on expansion slot → seated on block below).
//////////////////    /// </summary>
//////////////////    public void PlaceUnit(CastleUnitDraggable unit)
//////////////////    {
//////////////////        if (unit == null || HasUnit) return;

//////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////////        if (rt != null)
//////////////////        {
//////////////////            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//////////////////            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
//////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////            rt.SetAsLastSibling();
//////////////////        }

//////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////////////        _placedInstance = unit.gameObject;
//////////////////        HasUnit = true;
//////////////////        PlacedVariantId = unit.variantId;

//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////        _bg.color = normalColor;

//////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////////                  $"placed on '{transform.parent?.name}'.");
//////////////////    }

//////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////    {
//////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////        if (unit == null) return;

//////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////////////////        _highlight?.SetActive(true);
//////////////////        _emptyVisual?.SetActive(valid);
//////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////////////    }

//////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////    {
//////////////////        _highlight?.SetActive(false);
//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _bg.color = normalColor;
//////////////////    }

//////////////////    // ── Remove ────────────────────────────────────────────────────

//////////////////    public void RemoveUnit()
//////////////////    {
//////////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
//////////////////        HasUnit = false; PlacedVariantId = -1;
//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////        _bg.color = normalColor;
//////////////////    }
//////////////////}

////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using UnityEngine.EventSystems;

/////////////////// <summary>
/////////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
///////////////////
/////////////////// acceptedType is set by CastleBlockUnitSlot.Awake() in code — do NOT rely
/////////////////// on the Inspector value; it will be overwritten at runtime.
///////////////////
/////////////////// PlaceUnit(CastleUnitDraggable) is public so ExpansionSlot can call it
/////////////////// directly when routing a unit drop to the block below the expansion slot.
/////////////////// </summary>
////////////////[RequireComponent(typeof(Image))]
////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////////{
////////////////    // ── Inspector / set-by-code ───────────────────────────────────
////////////////    // CastleBlockUnitSlot.Awake() overwrites this — Inspector value
////////////////    // is kept only as a fallback for standalone testing.
////////////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). " +
////////////////             "Manual Inspector value is overwritten at runtime.")]
////////////////    public CastleUnitType acceptedType;

////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////////    private Image _bg;
////////////////    private GameObject _emptyVisual;
////////////////    private GameObject _highlight;

////////////////    // ── State ─────────────────────────────────────────────────────
////////////////    public bool HasUnit { get; private set; }
////////////////    public int PlacedVariantId { get; private set; } = -1;
////////////////    private GameObject _placedInstance;

////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        _bg = GetComponent<Image>();
////////////////        _bg.color = normalColor;
////////////////        _bg.raycastTarget = true;   // MUST be true or OnDrop never fires

////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////    }

////////////////    // ── Standard drag-drop path ───────────────────────────────────

////////////////    public void OnDrop(PointerEventData eventData)
////////////////    {
////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;

////////////////        // ── Verbose rejection logging so you can see exactly why a
////////////////        //    drop failed in the Console (remove after debugging). ──
////////////////        if (unit == null)
////////////////        {
////////////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but CurrentlyDragging is null.");
////////////////            return;
////////////////        }
////////////////        if (unit.unitType != acceptedType)
////////////////        {
////////////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
////////////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
////////////////            return;
////////////////        }
////////////////        if (HasUnit)
////////////////        {
////////////////            Debug.Log($"[CastleUnitDropZone] Zone already occupied ({acceptedType}). Drop rejected.");
////////////////            return;
////////////////        }

////////////////        PlaceUnit(unit);
////////////////        CastleUnitDraggable.NotifyDropSucceeded();
////////////////    }

////////////////    // ── Public: also called by ExpansionSlot ──────────────────────

////////////////    /// <summary>
////////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
////////////////    /// Called from OnDrop (normal path) and ExpansionSlot.OnDrop
////////////////    /// (unit dropped on expansion slot → seated on block below).
////////////////    /// </summary>
////////////////    public void PlaceUnit(CastleUnitDraggable unit)
////////////////    {
////////////////        if (unit == null || HasUnit) return;

////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////////        if (rt != null)
////////////////        {
////////////////            rt.anchorMin = Vector2.zero;
////////////////            rt.anchorMax = Vector2.one;
////////////////            rt.offsetMin = Vector2.zero;
////////////////            rt.offsetMax = Vector2.zero;
////////////////            rt.anchoredPosition = Vector2.zero;
////////////////            rt.SetAsLastSibling();
////////////////        }

////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////////        _placedInstance = unit.gameObject;
////////////////        HasUnit = true;
////////////////        PlacedVariantId = unit.variantId;

////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////        _bg.color = normalColor;

////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////////                  $"placed on '{transform.parent?.name}'.");
////////////////    }

////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////    {
////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////        if (unit == null) return;

////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////////////        _highlight?.SetActive(true);
////////////////        _emptyVisual?.SetActive(valid);
////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////////    }

////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////    {
////////////////        _highlight?.SetActive(false);
////////////////        _emptyVisual?.SetActive(false);
////////////////        _bg.color = normalColor;
////////////////    }

////////////////    // ── Remove ────────────────────────────────────────────────────

////////////////    public void RemoveUnit()
////////////////    {
////////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
////////////////        HasUnit = false;
////////////////        PlacedVariantId = -1;
////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////        _bg.color = normalColor;
////////////////    }
////////////////}


//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using UnityEngine.EventSystems;

///////////////// <summary>
///////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
/////////////////
///////////////// acceptedType is enforced by CastleBlockUnitSlot.Awake() in code —
///////////////// the Inspector value is overwritten at runtime.
/////////////////
///////////////// PlaceUnit respects CastleUnitDraggable.stretchToFillSlot:
/////////////////   TRUE  → stretch-anchors the unit to fill this zone rectangle.
/////////////////   FALSE → centers the unit at a fixed size (safe for customized / animated soldier prefabs).
///////////////// </summary>
//////////////[RequireComponent(typeof(Image))]
//////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////{
//////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////////////    public CastleUnitType acceptedType;

//////////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false " +
//////////////             "(i.e. customized / animated soldier prefabs).")]
//////////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////    private Image _bg;
//////////////    private GameObject _emptyVisual;
//////////////    private GameObject _highlight;

//////////////    // ── State ─────────────────────────────────────────────────────
//////////////    public bool HasUnit { get; private set; }
//////////////    public int PlacedVariantId { get; private set; } = -1;
//////////////    private GameObject _placedInstance;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        _bg = GetComponent<Image>();
//////////////        _bg.color = normalColor;
//////////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

//////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////    }

//////////////    // ── Standard drag-drop path ───────────────────────────────────

//////////////    public void OnDrop(PointerEventData eventData)
//////////////    {
//////////////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////////////        if (unit == null)
//////////////        {
//////////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
//////////////            return;
//////////////        }
//////////////        if (unit.unitType != acceptedType)
//////////////        {
//////////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
//////////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
//////////////            return;
//////////////        }
//////////////        if (HasUnit)
//////////////        {
//////////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
//////////////            return;
//////////////        }

//////////////        PlaceUnit(unit);
//////////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////////    }

//////////////    // ── Public: also called by ExpansionSlot ──────────────────────

//////////////    /// <summary>
//////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
//////////////    ///
//////////////    /// stretchToFillSlot = true  → anchors 0→1 to fill the zone (simple icons).
//////////////    /// stretchToFillSlot = false → centered at <see cref="centeredUnitSize"/>
//////////////    ///                             (customized / animated soldier prefabs — prevents
//////////////    ///                             broken child layouts that cause the unit to look
//////////////    ///                             invisible or distorted after placement).
//////////////    /// </summary>
//////////////    public void PlaceUnit(CastleUnitDraggable unit)
//////////////    {
//////////////        if (unit == null || HasUnit) return;

//////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////        if (rt != null)
//////////////        {
//////////////            if (unit.stretchToFillSlot)
//////////////            {
//////////////                // Stretch to fill the entire zone rectangle
//////////////                rt.anchorMin = Vector2.zero;
//////////////                rt.anchorMax = Vector2.one;
//////////////                rt.offsetMin = Vector2.zero;
//////////////                rt.offsetMax = Vector2.zero;
//////////////                rt.anchoredPosition = Vector2.zero;
//////////////            }
//////////////            else
//////////////            {
//////////////                // ── FIX for customized soldier prefabs ────────────────
//////////////                // Center at a fixed pixel size instead of stretching.
//////////////                // Stretching breaks child Animators, multi-Image hierarchies,
//////////////                // and fixed-pixel children — making the unit invisible/distorted.
//////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////////                rt.sizeDelta = centeredUnitSize;
//////////////                rt.anchoredPosition = Vector2.zero;
//////////////            }

//////////////            rt.SetAsLastSibling();
//////////////        }

//////////////        // Re-enable raycasts and full opacity now the unit is settled
//////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////////        _placedInstance = unit.gameObject;
//////////////        HasUnit = true;
//////////////        PlacedVariantId = unit.variantId;

//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////        _bg.color = normalColor;

//////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////                  $"placed on '{transform.parent?.name}' [stretch={unit.stretchToFillSlot}].");
//////////////    }

//////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////    {
//////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////        if (unit == null) return;

//////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////////////        _highlight?.SetActive(true);
//////////////        _emptyVisual?.SetActive(valid);
//////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////////    }

//////////////    public void OnPointerExit(PointerEventData eventData)
//////////////    {
//////////////        _highlight?.SetActive(false);
//////////////        _emptyVisual?.SetActive(false);
//////////////        _bg.color = normalColor;
//////////////    }

//////////////    // ── Remove ────────────────────────────────────────────────────

//////////////    public void RemoveUnit()
//////////////    {
//////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
//////////////        HasUnit = false;
//////////////        PlacedVariantId = -1;
//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////        _bg.color = normalColor;
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using UnityEngine.EventSystems;

/////////////// <summary>
/////////////// Drop zone for a Cannon unit on an exposed castle block.
///////////////
/////////////// When a cannon is successfully placed, a child Image named "Soldier"
/////////////// (already present in the prefab hierarchy) is automatically made visible
/////////////// alongside it — no separate soldier drag-and-drop required.
///////////////
/////////////// Child hierarchy (auto-wired by name in Awake):
///////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////////////   ├── EmptyVisual     hint shown only during a valid drag hover
///////////////   ├── Highlight       glow frame shown during any drag hover
///////////////   └── Soldier         Image — hidden by default, shown when cannon is placed
/////////////// </summary>
////////////[RequireComponent(typeof(Image))]
////////////public class CastleUnitDropZone : MonoBehaviour,
////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////{
////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////////////    public CastleUnitType acceptedType;

////////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
////////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////    private Image _bg;
////////////    private GameObject _emptyVisual;
////////////    private GameObject _highlight;
////////////    private GameObject _soldierImage;   // child named "Soldier" — shown with the cannon

////////////    // ── State ─────────────────────────────────────────────────────
////////////    public bool HasUnit { get; private set; }
////////////    public int PlacedVariantId { get; private set; } = -1;
////////////    private GameObject _placedInstance;

////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        _bg = GetComponent<Image>();
////////////        _bg.color = normalColor;
////////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////        _highlight = transform.Find("Highlight")?.gameObject;
////////////        _soldierImage = transform.Find("Soldier")?.gameObject;

////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////        _soldierImage?.SetActive(false);   // hidden until a cannon is placed
////////////    }

////////////    // ── Standard drag-drop path ───────────────────────────────────

////////////    public void OnDrop(PointerEventData eventData)
////////////    {
////////////        var unit = CastleUnitDraggable.CurrentlyDragging;

////////////        if (unit == null)
////////////        {
////////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
////////////            return;
////////////        }
////////////        if (unit.unitType != acceptedType)
////////////        {
////////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
////////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
////////////            return;
////////////        }
////////////        if (HasUnit)
////////////        {
////////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
////////////            return;
////////////        }

////////////        PlaceUnit(unit);
////////////        CastleUnitDraggable.NotifyDropSucceeded();
////////////    }

////////////    // ── Public: also called by ExpansionSlot ──────────────────────

////////////    /// <summary>
////////////    /// Reparents <paramref name="unit"/> into this zone, marks it occupied,
////////////    /// and reveals the Soldier image that lives alongside the cannon.
////////////    /// </summary>
////////////    public void PlaceUnit(CastleUnitDraggable unit)
////////////    {
////////////        if (unit == null || HasUnit) return;

////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////        if (rt != null)
////////////        {
////////////            if (unit.stretchToFillSlot)
////////////            {
////////////                rt.anchorMin = Vector2.zero;
////////////                rt.anchorMax = Vector2.one;
////////////                rt.offsetMin = Vector2.zero;
////////////                rt.offsetMax = Vector2.zero;
////////////                rt.anchoredPosition = Vector2.zero;
////////////            }
////////////            else
////////////            {
////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////////                rt.sizeDelta = centeredUnitSize;
////////////                rt.anchoredPosition = Vector2.zero;
////////////            }

////////////            rt.SetAsLastSibling();
////////////        }

////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////        _placedInstance = unit.gameObject;
////////////        HasUnit = true;
////////////        PlacedVariantId = unit.variantId;

////////////        // Hide zone chrome
////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////        _bg.color = normalColor;

////////////        // ── Show the soldier image that lives beside the cannon ────
////////////        _soldierImage?.SetActive(true);

////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////                  $"placed on '{transform.parent?.name}' [stretch={unit.stretchToFillSlot}].");
////////////    }

////////////    // ── Hover ─────────────────────────────────────────────────────

////////////    public void OnPointerEnter(PointerEventData eventData)
////////////    {
////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////        if (unit == null) return;

////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////////        _highlight?.SetActive(true);
////////////        _emptyVisual?.SetActive(valid);
////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////    }

////////////    public void OnPointerExit(PointerEventData eventData)
////////////    {
////////////        _highlight?.SetActive(false);
////////////        _emptyVisual?.SetActive(false);
////////////        _bg.color = normalColor;
////////////    }

////////////    // ── Remove ────────────────────────────────────────────────────

////////////    /// <summary>Destroys the placed cannon and hides the soldier image.</summary>
////////////    public void RemoveUnit()
////////////    {
////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

////////////        HasUnit = false;
////////////        PlacedVariantId = -1;

////////////        _soldierImage?.SetActive(false);   // hide soldier when cannon is removed
////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////        _bg.color = normalColor;
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using UnityEngine.EventSystems;

///////////// <summary>
///////////// Drop zone for a Cannon unit on an exposed castle block.
/////////////
///////////// When a cannon is successfully placed, a child Image named "Soldier"
///////////// (already present in the prefab hierarchy) is automatically made visible
///////////// alongside it — no separate soldier drag-and-drop required.
/////////////
///////////// Full lifecycle:
/////////////   PlaceUnit    → cannon placed here from drag; soldier shown.
/////////////   DetachUnit   → cannon is being dragged away; soldier hidden, zone reset to empty.
/////////////                  Does NOT destroy the cannon — it is still alive being dragged.
/////////////   ReattachUnit → drag was cancelled / dropped on invalid target; cannon snapped
/////////////                  back to this zone; zone state restored as if it was never moved.
/////////////   RemoveUnit   → cannon destroyed (block removed etc.); soldier hidden.
/////////////
///////////// Child hierarchy (auto-wired by name in Awake):
/////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////////   ├── Highlight       glow frame shown during any drag hover
/////////////   └── Soldier         Image — hidden by default, shown when cannon is placed
///////////// </summary>
//////////[RequireComponent(typeof(Image))]
//////////public class CastleUnitDropZone : MonoBehaviour,
//////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////{
//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////////    public CastleUnitType acceptedType;

//////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////    // ── Auto-wired ────────────────────────────────────────────────
//////////    private Image _bg;
//////////    private GameObject _emptyVisual;
//////////    private GameObject _highlight;
//////////    private GameObject _soldierImage;   // child named "Soldier" — shown alongside the cannon

//////////    // ── State ─────────────────────────────────────────────────────
//////////    public bool HasUnit { get; private set; }
//////////    public int PlacedVariantId { get; private set; } = -1;
//////////    private GameObject _placedInstance;

//////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        _bg = GetComponent<Image>();
//////////        _bg.color = normalColor;
//////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

//////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////        _highlight = transform.Find("Highlight")?.gameObject;
//////////        _soldierImage = transform.Find("Soldier")?.gameObject;

//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _soldierImage?.SetActive(false);
//////////    }

//////////    // ── Standard drag-drop path ───────────────────────────────────

//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////////        if (unit == null)
//////////        {
//////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
//////////            return;
//////////        }
//////////        if (unit.unitType != acceptedType)
//////////        {
//////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
//////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
//////////            return;
//////////        }
//////////        if (HasUnit)
//////////        {
//////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
//////////            return;
//////////        }

//////////        PlaceUnit(unit);
//////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////    }

//////////    // ── Public: also called by ExpansionSlot ──────────────────────

//////////    /// <summary>
//////////    /// Reparents <paramref name="unit"/> into this zone, marks it occupied,
//////////    /// and reveals the Soldier image that lives alongside the cannon.
//////////    /// </summary>
//////////    public void PlaceUnit(CastleUnitDraggable unit)
//////////    {
//////////        if (unit == null || HasUnit) return;

//////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////        if (rt != null)
//////////        {
//////////            if (unit.stretchToFillSlot)
//////////            {
//////////                rt.anchorMin = Vector2.zero;
//////////                rt.anchorMax = Vector2.one;
//////////                rt.offsetMin = Vector2.zero;
//////////                rt.offsetMax = Vector2.zero;
//////////                rt.anchoredPosition = Vector2.zero;
//////////            }
//////////            else
//////////            {
//////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////                rt.sizeDelta = centeredUnitSize;
//////////                rt.anchoredPosition = Vector2.zero;
//////////            }
//////////            rt.SetAsLastSibling();
//////////        }

//////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////        _placedInstance = unit.gameObject;
//////////        HasUnit = true;
//////////        PlacedVariantId = unit.variantId;

//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;
//////////        _soldierImage?.SetActive(true);

//////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////                  $"placed on '{transform.parent?.name}'.");
//////////    }

//////////    /// <summary>
//////////    /// Called by <see cref="CastleUnitDraggable.OnBeginDrag"/> when the already-placed
//////////    /// cannon starts being dragged away from this zone.
//////////    /// Clears zone state and hides the soldier WITHOUT destroying the cannon
//////////    /// (it is still alive — currently being dragged).
//////////    /// </summary>
//////////    public void DetachUnit()
//////////    {
//////////        _placedInstance = null;
//////////        HasUnit = false;
//////////        PlacedVariantId = -1;

//////////        _soldierImage?.SetActive(false);
//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;

//////////        Debug.Log($"[CastleUnitDropZone] Cannon detached from '{transform.parent?.name}' — soldier hidden.");
//////////    }

//////////    /// <summary>
//////////    /// Called by <see cref="CastleUnitDraggable.OnEndDrag"/> when the drag was cancelled
//////////    /// (dropped on an invalid target) and the cannon snaps back to this zone.
//////////    /// Restores zone state as if the cannon was never moved.
//////////    /// </summary>
//////////    public void ReattachUnit(CastleUnitDraggable unit)
//////////    {
//////////        if (unit == null) return;

//////////        _placedInstance = unit.gameObject;
//////////        HasUnit = true;
//////////        PlacedVariantId = unit.variantId;

//////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;
//////////        _soldierImage?.SetActive(true);

//////////        Debug.Log($"[CastleUnitDropZone] Cannon snapped back to '{transform.parent?.name}' — soldier restored.");
//////////    }

//////////    // ── Hover ─────────────────────────────────────────────────────

//////////    public void OnPointerEnter(PointerEventData eventData)
//////////    {
//////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////        if (unit == null) return;

//////////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////////        _highlight?.SetActive(true);
//////////        _emptyVisual?.SetActive(valid);
//////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////    }

//////////    public void OnPointerExit(PointerEventData eventData)
//////////    {
//////////        _highlight?.SetActive(false);
//////////        _emptyVisual?.SetActive(false);
//////////        _bg.color = normalColor;
//////////    }

//////////    // ── Remove (cannon destroyed externally, e.g. block removed) ──

//////////    /// <summary>Destroys the placed cannon and hides the soldier image.</summary>
//////////    public void RemoveUnit()
//////////    {
//////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
//////////        HasUnit = false;
//////////        PlacedVariantId = -1;

//////////        _soldierImage?.SetActive(false);
//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;
////////using UnityEngine.EventSystems;

/////////// <summary>
/////////// Drop zone for a Cannon unit on an exposed castle block.
///////////
/////////// When a cannon is placed via an ExpansionSlot, that slot registers itself
/////////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
/////////// hides / shows that slot as the cannon moves:
///////////
///////////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
///////////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
///////////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
///////////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
///////////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
///////////
/////////// Child hierarchy (auto-wired by name in Awake):
///////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////////   ├── EmptyVisual     hint shown only during a valid drag hover
///////////   ├── Highlight       glow frame shown during any drag hover
///////////   └── Soldier         Image — hidden by default, shown when cannon is placed
/////////// </summary>
////////[RequireComponent(typeof(Image))]
////////public class CastleUnitDropZone : MonoBehaviour,
////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////{
////////    // ── Inspector ─────────────────────────────────────────────────
////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////////    public CastleUnitType acceptedType;

////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////    // ── Auto-wired ────────────────────────────────────────────────
////////    private Image _bg;
////////    private GameObject _emptyVisual;
////////    private GameObject _highlight;
////////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

////////    // ── State ─────────────────────────────────────────────────────
////////    public bool HasUnit { get; private set; }
////////    public int PlacedVariantId { get; private set; } = -1;

////////    /// <summary>
////////    /// The ExpansionSlot that was used to place the cannon here.
////////    /// Set by ExpansionSlot.OnDrop. The zone uses it to show/hide that
////////    /// slot when the cannon arrives, leaves, or is destroyed.
////////    /// Null when the cannon was dropped directly onto this zone.
////////    /// </summary>
////////    public ExpansionSlot LinkedExpansionSlot { get; set; }

////////    private GameObject _placedInstance;

////////    // ── Lifecycle ─────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        _bg = GetComponent<Image>();
////////        _bg.color = normalColor;
////////        _bg.raycastTarget = true;

////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////        _highlight = transform.Find("Highlight")?.gameObject;
////////        _soldierImage = transform.Find("Soldier")?.gameObject;

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _soldierImage?.SetActive(false);
////////    }

////////    // ── Standard drag-drop path (direct drop onto this zone) ──────

////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var unit = CastleUnitDraggable.CurrentlyDragging;

////////        if (unit == null)
////////        {
////////            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
////////            return;
////////        }
////////        if (unit.unitType != acceptedType)
////////        {
////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
////////                      $"zone accepts {acceptedType}.");
////////            return;
////////        }
////////        if (HasUnit)
////////        {
////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
////////            return;
////////        }

////////        PlaceUnit(unit);
////////        CastleUnitDraggable.NotifyDropSucceeded();
////////    }

////////    // ── Public API ────────────────────────────────────────────────

////////    /// <summary>
////////    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
////////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
////////    /// </summary>
////////    public void PlaceUnit(CastleUnitDraggable unit)
////////    {
////////        if (unit == null || HasUnit) return;

////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////        if (rt != null)
////////        {
////////            if (unit.stretchToFillSlot)
////////            {
////////                rt.anchorMin = Vector2.zero;
////////                rt.anchorMax = Vector2.one;
////////                rt.offsetMin = Vector2.zero;
////////                rt.offsetMax = Vector2.zero;
////////                rt.anchoredPosition = Vector2.zero;
////////            }
////////            else
////////            {
////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////                rt.sizeDelta = centeredUnitSize;
////////                rt.anchoredPosition = Vector2.zero;
////////            }
////////            rt.SetAsLastSibling();
////////        }

////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////        _placedInstance = unit.gameObject;
////////        HasUnit = true;
////////        PlacedVariantId = unit.variantId;

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;
////////        _soldierImage?.SetActive(true);

////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////                  $"placed on '{transform.parent?.name}'.");
////////    }

////////    /// <summary>
////////    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
////////    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
////////    /// Does NOT destroy the cannon — it is still alive being dragged.
////////    /// </summary>
////////    public void DetachUnit()
////////    {
////////        _placedInstance = null;
////////        HasUnit = false;
////////        PlacedVariantId = -1;

////////        _soldierImage?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;

////////        // Reveal the expansion slot above so the cannon (or another) can be
////////        // dropped there again
////////        if (LinkedExpansionSlot != null)
////////        {
////////            LinkedExpansionSlot.gameObject.SetActive(true);
////////            Debug.Log($"[CastleUnitDropZone] Detached — expansion slot restored.");
////////        }
////////    }

////////    /// <summary>
////////    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
////////    /// snaps back here. Restores zone state exactly as before the drag started.
////////    /// </summary>
////////    public void ReattachUnit(CastleUnitDraggable unit)
////////    {
////////        if (unit == null) return;

////////        _placedInstance = unit.gameObject;
////////        HasUnit = true;
////////        PlacedVariantId = unit.variantId;

////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;
////////        _soldierImage?.SetActive(true);

////////        // Hide the expansion slot again — cannon is back on this block
////////        if (LinkedExpansionSlot != null)
////////        {
////////            LinkedExpansionSlot.gameObject.SetActive(false);
////////            Debug.Log($"[CastleUnitDropZone] Reattached — expansion slot hidden again.");
////////        }
////////    }

////////    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
////////    public void RemoveUnit()
////////    {
////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

////////        HasUnit = false;
////////        PlacedVariantId = -1;

////////        _soldierImage?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;

////////        // Restore the expansion slot — the block is now cannon-free
////////        if (LinkedExpansionSlot != null)
////////        {
////////            LinkedExpansionSlot.gameObject.SetActive(true);
////////            LinkedExpansionSlot = null;
////////            Debug.Log($"[CastleUnitDropZone] Removed — expansion slot restored.");
////////        }
////////    }

////////    // ── Hover ─────────────────────────────────────────────────────

////////    public void OnPointerEnter(PointerEventData eventData)
////////    {
////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////        if (unit == null) return;

////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////        _highlight?.SetActive(true);
////////        _emptyVisual?.SetActive(valid);
////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////    }

////////    public void OnPointerExit(PointerEventData eventData)
////////    {
////////        _highlight?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _bg.color = normalColor;
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// Drop zone for a Cannon unit on an exposed castle block.
/////////
///////// When a cannon is placed via an ExpansionSlot, that slot registers itself
///////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
///////// hides / shows that slot as the cannon moves:
/////////
/////////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
/////////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
/////////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
/////////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
/////////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
/////////
///////// Child hierarchy (auto-wired by name in Awake):
/////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////   ├── Highlight       glow frame shown during any drag hover
/////////   └── Soldier         Image — hidden by default, shown when cannon is placed
///////// </summary>
//////[RequireComponent(typeof(Image))]
//////public class CastleUnitDropZone : MonoBehaviour,
//////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////    public CastleUnitType acceptedType;

//////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////    // ── Auto-wired ────────────────────────────────────────────────
//////    private Image _bg;
//////    private GameObject _emptyVisual;
//////    private GameObject _highlight;
//////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

//////    // ── State ─────────────────────────────────────────────────────
//////    public bool HasUnit { get; private set; }
//////    public int PlacedVariantId { get; private set; } = -1;

//////    /// <summary>
//////    /// The ExpansionSlot that was used to place the cannon here.
//////    /// Set by ExpansionSlot.OnDrop, or pre-linked by ExpansionSlot.Init.
//////    /// The zone uses it to show/hide that slot when the cannon arrives,
//////    /// leaves, or is destroyed.
//////    /// Null when no expansion slot is associated with this zone.
//////    /// </summary>
//////    public ExpansionSlot LinkedExpansionSlot { get; set; }

//////    private GameObject _placedInstance;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _bg = GetComponent<Image>();
//////        _bg.color = normalColor;
//////        _bg.raycastTarget = true;

//////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////        _highlight = transform.Find("Highlight")?.gameObject;
//////        _soldierImage = transform.Find("Soldier")?.gameObject;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _soldierImage?.SetActive(false);

//////        // FIX 1 — The Soldier child is a pure visual overlay.
//////        // It must NOT be a raycast target, otherwise it blocks pointer events
//////        // from reaching the cannon that sits on top of it, making the
//////        // placed cannon impossible to drag.
//////        if (_soldierImage != null)
//////        {
//////            Image soldierImg = _soldierImage.GetComponent<Image>();
//////            if (soldierImg != null)
//////                soldierImg.raycastTarget = false;
//////        }
//////    }

//////    // ── Standard drag-drop path (direct drop onto this zone) ──────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////        if (unit == null)
//////        {
//////            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
//////            return;
//////        }
//////        if (unit.unitType != acceptedType)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
//////                      $"zone accepts {acceptedType}.");
//////            return;
//////        }
//////        if (HasUnit)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
//////            return;
//////        }

//////        PlaceUnit(unit);
//////        CastleUnitDraggable.NotifyDropSucceeded();
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
//////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//////    /// </summary>
//////    public void PlaceUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null || HasUnit) return;

//////        unit.transform.SetParent(transform, worldPositionStays: false);

//////        RectTransform rt = unit.GetComponent<RectTransform>();
//////        if (rt != null)
//////        {
//////            if (unit.stretchToFillSlot)
//////            {
//////                rt.anchorMin = Vector2.zero;
//////                rt.anchorMax = Vector2.one;
//////                rt.offsetMin = Vector2.zero;
//////                rt.offsetMax = Vector2.zero;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            else
//////            {
//////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////                rt.pivot = new Vector2(0.5f, 0.5f);
//////                rt.sizeDelta = centeredUnitSize;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            rt.SetAsLastSibling();
//////        }

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////                  $"placed on '{transform.parent?.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
//////    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
//////    /// Does NOT destroy the cannon — it is still alive being dragged.
//////    /// </summary>
//////    public void DetachUnit()
//////    {
//////        _placedInstance = null;
//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Reveal the expansion slot above so the cannon (or another) can be
//////        // dropped there again.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            Debug.Log($"[CastleUnitDropZone] Detached — expansion slot restored.");

//////            // FIX 3 — Clear the reference after restoring so it is not stale
//////            // if a different slot links to this zone in the future.
//////            LinkedExpansionSlot = null;
//////        }
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
//////    /// snaps back here. Restores zone state exactly as before the drag started.
//////    /// </summary>
//////    public void ReattachUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null) return;

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        // Hide the expansion slot again — cannon is back on this block.
//////        // Re-link it so the next DetachUnit can restore it correctly.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(false);
//////            Debug.Log($"[CastleUnitDropZone] Reattached — expansion slot hidden again.");
//////        }
//////    }

//////    /// <summary>
//////    /// Called by ExpansionSlot.OnDrop to restore the link after a successful
//////    /// snap-back so the slot can be revealed again on the next detach.
//////    /// </summary>
//////    public void RestoreLinkedExpansionSlot(ExpansionSlot slot)
//////    {
//////        LinkedExpansionSlot = slot;
//////    }

//////    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
//////    public void RemoveUnit()
//////    {
//////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Restore the expansion slot — the block is now cannon-free.
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            LinkedExpansionSlot = null;
//////            Debug.Log($"[CastleUnitDropZone] Removed — expansion slot restored.");
//////        }
//////    }

//////    // ── Hover ─────────────────────────────────────────────────────

//////    public void OnPointerEnter(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////        if (unit == null) return;

//////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////        _highlight?.SetActive(true);
//////        _emptyVisual?.SetActive(valid);
//////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////    }

//////    public void OnPointerExit(PointerEventData eventData)
//////    {
//////        _highlight?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _bg.color = normalColor;
//////    }
//////}


//////////////////////////////using UnityEngine;
//////////////////////////////using UnityEngine.UI;
//////////////////////////////using UnityEngine.EventSystems;

///////////////////////////////// <summary>
///////////////////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////////////
///////////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
/////////////////////////////////   CastleUnitDropZone  ← this script + Image (transparent, raycast target)
/////////////////////////////////   ├── UnitIcon        Image — shown when a unit is placed (displays the sprite)
/////////////////////////////////   ├── EmptyVisual     GameObject — shown when the zone is empty
/////////////////////////////////   └── Highlight       GameObject — shown while the correct unit type is dragged over
///////////////////////////////// </summary>
//////////////////////////////[RequireComponent(typeof(Image))]
//////////////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////////////    IDropHandler,
//////////////////////////////    IPointerEnterHandler,
//////////////////////////////    IPointerExitHandler
//////////////////////////////{
//////////////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////////////    [Header("Zone Settings")]
//////////////////////////////    public CastleUnitType acceptedType;

//////////////////////////////    [Header("Colors")]
//////////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//////////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////////////    // ── Auto-wired children ───────────────────────────────────────
//////////////////////////////    private Image _bg;
//////////////////////////////    private Image _unitIcon;
//////////////////////////////    private GameObject _emptyVisual;
//////////////////////////////    private GameObject _highlight;

//////////////////////////////    // ── State ─────────────────────────────────────────────────────
//////////////////////////////    public bool HasUnit { get; private set; }
//////////////////////////////    private Sprite _placedSprite;

//////////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////////////    private void Awake()
//////////////////////////////    {
//////////////////////////////        _bg = GetComponent<Image>();
//////////////////////////////        _bg.color = normalColor;

//////////////////////////////        // Auto-wire children by name
//////////////////////////////        var iconT = transform.Find("UnitIcon");
//////////////////////////////        var emptyT = transform.Find("EmptyVisual");
//////////////////////////////        var hlT = transform.Find("Highlight");

//////////////////////////////        if (iconT != null) _unitIcon = iconT.GetComponent<Image>();
//////////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
//////////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////////        if (dragged == null) return;
//////////////////////////////        if (dragged.unitType != acceptedType) return;   // wrong unit type
//////////////////////////////        if (HasUnit) return;                            // slot already full

//////////////////////////////        PlaceUnit(dragged.unitSprite);

//////////////////////////////        // The drag source icon stays in the panel (unlimited supply).
//////////////////////////////        // Destroy the ghost manually here since OnEndDrag fires after OnDrop.
//////////////////////////////        CastleUnitDraggable.DestroyGhost();

//////////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} placed on {transform.parent.name}.");
//////////////////////////////    }

//////////////////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////////        if (dragged == null) return;

//////////////////////////////        if (_highlight != null) _highlight.SetActive(true);
//////////////////////////////        _bg.color = (dragged.unitType == acceptedType && !HasUnit)
//////////////////////////////            ? hoverValidColor
//////////////////////////////            : hoverInvalidColor;
//////////////////////////////    }

//////////////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////////////    {
//////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////////        _bg.color = normalColor;
//////////////////////////////    }

//////////////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////////////    public void PlaceUnit(Sprite sprite)
//////////////////////////////    {
//////////////////////////////        HasUnit = true;
//////////////////////////////        _placedSprite = sprite;
//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    public void RemoveUnit()
//////////////////////////////    {
//////////////////////////////        HasUnit = false;
//////////////////////////////        _placedSprite = null;
//////////////////////////////        RefreshVisuals();
//////////////////////////////    }

//////////////////////////////    // ── Visuals ───────────────────────────────────────────────────

//////////////////////////////    private void RefreshVisuals()
//////////////////////////////    {
//////////////////////////////        if (_unitIcon != null)
//////////////////////////////        {
//////////////////////////////            _unitIcon.gameObject.SetActive(HasUnit);
//////////////////////////////            if (HasUnit && _placedSprite != null) _unitIcon.sprite = _placedSprite;
//////////////////////////////        }

//////////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
//////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////////        _bg.color = normalColor;
//////////////////////////////    }
//////////////////////////////}

////////////////////////////using UnityEngine;
////////////////////////////using UnityEngine.UI;
////////////////////////////using UnityEngine.EventSystems;

/////////////////////////////// <summary>
/////////////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////////////
/////////////////////////////// On a valid drop the unit PREFAB is instantiated as a child of this zone
/////////////////////////////// and scaled to fill it. Removing clears and destroys the instance.
///////////////////////////////
/////////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
///////////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
///////////////////////////////   ├── EmptyVisual      GameObject — shown when the zone is empty (e.g. "+" icon)
///////////////////////////////   └── Highlight        GameObject — glow frame while a valid drag hovers
///////////////////////////////
/////////////////////////////// Note: there is no longer a static UnitIcon Image child — the spawned
/////////////////////////////// prefab itself provides the visual.
/////////////////////////////// </summary>
////////////////////////////[RequireComponent(typeof(Image))]
////////////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////////////    IDropHandler,
////////////////////////////    IPointerEnterHandler,
////////////////////////////    IPointerExitHandler
////////////////////////////{
////////////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////////////    [Header("Zone Settings")]
////////////////////////////    public CastleUnitType acceptedType;

////////////////////////////    [Header("Colors")]
////////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////////////    // ── Auto-wired children ───────────────────────────────────────
////////////////////////////    private Image _bg;
////////////////////////////    private GameObject _emptyVisual;   // shown when the zone is empty
////////////////////////////    private GameObject _highlight;     // hover glow

////////////////////////////    // ── Runtime state ─────────────────────────────────────────────
////////////////////////////    public bool HasUnit { get; private set; }
////////////////////////////    private GameObject _placedInstance;  // the instantiated unit prefab

////////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////////////    private void Awake()
////////////////////////////    {
////////////////////////////        _bg = GetComponent<Image>();
////////////////////////////        _bg.color = normalColor;

////////////////////////////        // Wire optional UI children by name
////////////////////////////        var emptyT = transform.Find("EmptyVisual");
////////////////////////////        var hlT = transform.Find("Highlight");

////////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
////////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

////////////////////////////        RefreshVisuals();
////////////////////////////    }

////////////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////        if (dragged == null) return;
////////////////////////////        if (dragged.unitType != acceptedType) return; // wrong unit type
////////////////////////////        if (HasUnit) return; // zone already occupied
////////////////////////////        if (dragged.unitPrefab == null) return;

////////////////////////////        PlaceUnit(dragged.unitPrefab);

////////////////////////////        // Destroy the ghost immediately (OnEndDrag fires after OnDrop, but
////////////////////////////        // DestroyGhost is safe to call multiple times)
////////////////////////////        CastleUnitDraggable.DestroyGhost();

////////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} prefab placed on {transform.parent.name}.");
////////////////////////////    }

////////////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        var dragged = CastleUnitDraggable.CurrentlyDragging;
////////////////////////////        if (dragged == null) return;

////////////////////////////        if (_highlight != null) _highlight.SetActive(true);

////////////////////////////        _bg.color = (dragged.unitType == acceptedType && !HasUnit)
////////////////////////////            ? hoverValidColor
////////////////////////////            : hoverInvalidColor;
////////////////////////////    }

////////////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////////////    {
////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////        _bg.color = normalColor;
////////////////////////////    }

////////////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////////////    /// <summary>
////////////////////////////    /// Instantiates the unit prefab as a child of this zone, stretching it
////////////////////////////    /// to fill the zone's RectTransform.
////////////////////////////    /// </summary>
////////////////////////////    public void PlaceUnit(GameObject prefab)
////////////////////////////    {
////////////////////////////        if (HasUnit) RemoveUnit(); // replace if somehow called twice

////////////////////////////        _placedInstance = Instantiate(prefab, transform);

////////////////////////////        // Stretch to fill the drop zone
////////////////////////////        RectTransform rt = _placedInstance.GetComponent<RectTransform>();
////////////////////////////        if (rt != null)
////////////////////////////        {
////////////////////////////            rt.anchorMin = Vector2.zero;
////////////////////////////            rt.anchorMax = Vector2.one;
////////////////////////////            rt.offsetMin = Vector2.zero;
////////////////////////////            rt.offsetMax = Vector2.zero;
////////////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////////////            rt.SetAsLastSibling();
////////////////////////////        }

////////////////////////////        HasUnit = true;
////////////////////////////        RefreshVisuals();
////////////////////////////    }

////////////////////////////    /// <summary>
////////////////////////////    /// Destroys the placed unit prefab instance and resets the zone.
////////////////////////////    /// </summary>
////////////////////////////    public void RemoveUnit()
////////////////////////////    {
////////////////////////////        if (_placedInstance != null)
////////////////////////////        {
////////////////////////////            Destroy(_placedInstance);
////////////////////////////            _placedInstance = null;
////////////////////////////        }

////////////////////////////        HasUnit = false;
////////////////////////////        RefreshVisuals();
////////////////////////////    }

////////////////////////////    // ── Visuals ───────────────────────────────────────────────────

////////////////////////////    private void RefreshVisuals()
////////////////////////////    {
////////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
////////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////////        _bg.color = normalColor;
////////////////////////////    }
////////////////////////////}

//////////////////////////using UnityEngine;
//////////////////////////using UnityEngine.UI;
//////////////////////////using UnityEngine.EventSystems;

///////////////////////////// <summary>
///////////////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////////
///////////////////////////// On a valid drop, the live prefab instance being dragged (held by
///////////////////////////// <see cref="CastleUnitDraggable.CurrentDragInstance"/>) is reparented
///////////////////////////// here and stretched to fill the zone. No new prefab is instantiated.
/////////////////////////////
///////////////////////////// ── Required child hierarchy (auto-wired by name in Awake) ─────
/////////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
/////////////////////////////   ├── EmptyVisual      GameObject — shown while the zone is empty
/////////////////////////////   └── Highlight        GameObject — glow shown while a valid drag hovers
///////////////////////////// </summary>
//////////////////////////[RequireComponent(typeof(Image))]
//////////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////////    IDropHandler,
//////////////////////////    IPointerEnterHandler,
//////////////////////////    IPointerExitHandler
//////////////////////////{
//////////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////////    [Header("Zone Settings")]
//////////////////////////    public CastleUnitType acceptedType;

//////////////////////////    [Header("Colors")]
//////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////////    // ── Auto-wired children ───────────────────────────────────────
//////////////////////////    private Image _bg;
//////////////////////////    private GameObject _emptyVisual;  // "+" or empty-state indicator
//////////////////////////    private GameObject _highlight;    // hover glow frame

//////////////////////////    // ── Runtime state ─────────────────────────────────────────────
//////////////////////////    public bool HasUnit { get; private set; }
//////////////////////////    private GameObject _placedInstance;  // the reparented unit object

//////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////////    private void Awake()
//////////////////////////    {
//////////////////////////        _bg = GetComponent<Image>();
//////////////////////////        _bg.color = normalColor;

//////////////////////////        var emptyT = transform.Find("EmptyVisual");
//////////////////////////        var hlT = transform.Find("Highlight");

//////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
//////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

//////////////////////////        RefreshVisuals();
//////////////////////////    }

//////////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////        if (source == null) return;
//////////////////////////        if (source.unitType != acceptedType) return; // wrong type
//////////////////////////        if (HasUnit) return; // zone full

//////////////////////////        GameObject instance = CastleUnitDraggable.CurrentDragInstance;
//////////////////////////        if (instance == null) return;

//////////////////////////        // ── Reparent the live instance into this zone ──────────────
//////////////////////////        instance.transform.SetParent(transform, worldPositionStays: false);

//////////////////////////        // Re-enable raycasts now that it is placed (so it can receive
//////////////////////////        // future interactions if needed)
//////////////////////////        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
//////////////////////////        if (cg != null)
//////////////////////////        {
//////////////////////////            cg.blocksRaycasts = true;
//////////////////////////            cg.alpha = 1f;
//////////////////////////        }

//////////////////////////        // Stretch to fill the drop zone
//////////////////////////        RectTransform rt = instance.GetComponent<RectTransform>();
//////////////////////////        if (rt != null)
//////////////////////////        {
//////////////////////////            rt.anchorMin = Vector2.zero;
//////////////////////////            rt.anchorMax = Vector2.one;
//////////////////////////            rt.offsetMin = Vector2.zero;
//////////////////////////            rt.offsetMax = Vector2.zero;
//////////////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////////////            rt.SetAsLastSibling();
//////////////////////////        }

//////////////////////////        _placedInstance = instance;
//////////////////////////        HasUnit = true;

//////////////////////////        // Tell the draggable not to destroy the instance in OnEndDrag
//////////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

//////////////////////////        RefreshVisuals();

//////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} placed on '{transform.parent.name}'.");
//////////////////////////    }

//////////////////////////    // ── Hover feedback ────────────────────────────────────────────

//////////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
//////////////////////////        if (source == null) return;

//////////////////////////        if (_highlight != null) _highlight.SetActive(true);

//////////////////////////        _bg.color = (source.unitType == acceptedType && !HasUnit)
//////////////////////////            ? hoverValidColor
//////////////////////////            : hoverInvalidColor;
//////////////////////////    }

//////////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////////    {
//////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////        _bg.color = normalColor;
//////////////////////////    }

//////////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////////    /// <summary>Destroys the placed unit and resets the zone.</summary>
//////////////////////////    public void RemoveUnit()
//////////////////////////    {
//////////////////////////        if (_placedInstance != null)
//////////////////////////        {
//////////////////////////            Destroy(_placedInstance);
//////////////////////////            _placedInstance = null;
//////////////////////////        }

//////////////////////////        HasUnit = false;
//////////////////////////        RefreshVisuals();
//////////////////////////    }

//////////////////////////    // ── Visuals ───────────────────────────────────────────────────

//////////////////////////    private void RefreshVisuals()
//////////////////////////    {
//////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
//////////////////////////        if (_highlight != null) _highlight.SetActive(false);
//////////////////////////        _bg.color = normalColor;
//////////////////////////    }
//////////////////////////}

////////////////////////using UnityEngine;
////////////////////////using UnityEngine.UI;
////////////////////////using UnityEngine.EventSystems;

/////////////////////////// <summary>
/////////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////////
/////////////////////////// On a valid drop the live prefab instance from the spawner is reparented
/////////////////////////// here and stretched to fill the zone. The placed unit's variant id is
/////////////////////////// stored in <see cref="PlacedVariantId"/> for gameplay queries.
///////////////////////////
/////////////////////////// ── Child hierarchy (auto-wired by name in Awake) ───────────────
///////////////////////////   CastleUnitDropZone   ← this script + Image (transparent, raycast target)
///////////////////////////   ├── EmptyVisual      GameObject — visible when the zone is empty
///////////////////////////   └── Highlight        GameObject — glow shown during a valid hover
/////////////////////////// </summary>
////////////////////////[RequireComponent(typeof(Image))]
////////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////////    IDropHandler,
////////////////////////    IPointerEnterHandler,
////////////////////////    IPointerExitHandler
////////////////////////{
////////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////////    [Header("Zone Settings")]
////////////////////////    public CastleUnitType acceptedType;

////////////////////////    [Header("Colors")]
////////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
////////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////////    // ── Auto-wired children ───────────────────────────────────────
////////////////////////    private Image _bg;
////////////////////////    private GameObject _emptyVisual;
////////////////////////    private GameObject _highlight;

////////////////////////    // ── Runtime state ─────────────────────────────────────────────
////////////////////////    /// <summary>True when a unit prefab is currently placed in this zone.</summary>
////////////////////////    public bool HasUnit { get; private set; }

////////////////////////    /// <summary>
////////////////////////    /// The variant id of the placed unit (matches <see cref="CastleUnitDraggable.variantId"/>).
////////////////////////    /// -1 when the zone is empty.
////////////////////////    /// Example: Cannon zone holds variantId 2 → Heavy Cannon is placed here.
////////////////////////    /// </summary>
////////////////////////    public int PlacedVariantId { get; private set; } = -1;

////////////////////////    private GameObject _placedInstance;

////////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////////    private void Awake()
////////////////////////    {
////////////////////////        _bg = GetComponent<Image>();
////////////////////////        _bg.color = normalColor;

////////////////////////        var emptyT = transform.Find("EmptyVisual");
////////////////////////        var hlT = transform.Find("Highlight");

////////////////////////        if (emptyT != null) _emptyVisual = emptyT.gameObject;
////////////////////////        if (hlT != null) _highlight = hlT.gameObject;

////////////////////////        RefreshVisuals();
////////////////////////    }

////////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////////    {
////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
////////////////////////        if (source == null) return;
////////////////////////        if (source.unitType != acceptedType) return; // wrong unit type
////////////////////////        if (HasUnit) return; // zone already occupied

////////////////////////        GameObject instance = CastleUnitDraggable.CurrentDragInstance;
////////////////////////        if (instance == null) return;

////////////////////////        // ── Reparent the live instance into this zone ──────────────
////////////////////////        instance.transform.SetParent(transform, worldPositionStays: false);

////////////////////////        // Re-enable raycasts now that the unit is placed
////////////////////////        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
////////////////////////        if (cg != null)
////////////////////////        {
////////////////////////            cg.blocksRaycasts = true;
////////////////////////            cg.alpha = 1f;
////////////////////////        }

////////////////////////        // Stretch to fill the drop zone
////////////////////////        RectTransform rt = instance.GetComponent<RectTransform>();
////////////////////////        if (rt != null)
////////////////////////        {
////////////////////////            rt.anchorMin = Vector2.zero;
////////////////////////            rt.anchorMax = Vector2.one;
////////////////////////            rt.offsetMin = Vector2.zero;
////////////////////////            rt.offsetMax = Vector2.zero;
////////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////////            rt.SetAsLastSibling();
////////////////////////        }

////////////////////////        _placedInstance = instance;
////////////////////////        HasUnit = true;
////////////////////////        PlacedVariantId = source.variantId;

////////////////////////        // Tell the spawner not to destroy the instance in OnEndDrag
////////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

////////////////////////        RefreshVisuals();

////////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variantId={PlacedVariantId}) " +
////////////////////////                  $"placed on '{transform.parent.name}'.");
////////////////////////    }

////////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////////    {
////////////////////////        var source = CastleUnitDraggable.CurrentlyDragging;
////////////////////////        if (source == null) return;

////////////////////////        if (_highlight != null) _highlight.SetActive(true);

////////////////////////        _bg.color = (source.unitType == acceptedType && !HasUnit)
////////////////////////            ? hoverValidColor
////////////////////////            : hoverInvalidColor;
////////////////////////    }

////////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////////    {
////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////        _bg.color = normalColor;
////////////////////////    }

////////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////////    /// <summary>Destroys the placed unit instance and resets the zone.</summary>
////////////////////////    public void RemoveUnit()
////////////////////////    {
////////////////////////        if (_placedInstance != null)
////////////////////////        {
////////////////////////            Destroy(_placedInstance);
////////////////////////            _placedInstance = null;
////////////////////////        }

////////////////////////        HasUnit = false;
////////////////////////        PlacedVariantId = -1;
////////////////////////        RefreshVisuals();
////////////////////////    }

////////////////////////    // ── Visuals ───────────────────────────────────────────────────

////////////////////////    private void RefreshVisuals()
////////////////////////    {
////////////////////////        if (_emptyVisual != null) _emptyVisual.SetActive(!HasUnit);
////////////////////////        if (_highlight != null) _highlight.SetActive(false);
////////////////////////        _bg.color = normalColor;
////////////////////////    }
////////////////////////}

//////////////////////using UnityEngine;
//////////////////////using UnityEngine.UI;
//////////////////////using UnityEngine.EventSystems;

///////////////////////// <summary>
///////////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////////
///////////////////////// On a valid drop the dragged unit is reparented here and stretched to fill
///////////////////////// the zone. PlacedVariantId records which cannon variant (0/1/2) was placed.
/////////////////////////
///////////////////////// Child hierarchy (auto-wired by name):
/////////////////////////   CastleUnitDropZone  ← this script + Image (transparent, raycast target)
/////////////////////////   ├── EmptyVisual     shown while the zone is empty
/////////////////////////   └── Highlight       glow shown during a valid hover
///////////////////////// </summary>
//////////////////////[RequireComponent(typeof(Image))]
//////////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////////////{
//////////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////////    public CastleUnitType acceptedType;

//////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
//////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////////////    private Image _bg;
//////////////////////    private GameObject _emptyVisual;
//////////////////////    private GameObject _highlight;

//////////////////////    // ── State ─────────────────────────────────────────────────────
//////////////////////    public bool HasUnit { get; private set; }
//////////////////////    public int PlacedVariantId { get; private set; } = -1;

//////////////////////    private GameObject _placedInstance;

//////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////////    private void Awake()
//////////////////////    {
//////////////////////        _bg = GetComponent<Image>();
//////////////////////        _bg.color = normalColor;

//////////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////////////        RefreshVisuals();
//////////////////////    }

//////////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////////    public void OnDrop(PointerEventData eventData)
//////////////////////    {
//////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////////        if (unit == null) return;
//////////////////////        if (unit.unitType != acceptedType) return;
//////////////////////        if (HasUnit) return;

//////////////////////        // Reparent the unit into this zone and stretch it to fill
//////////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////////////        if (rt != null)
//////////////////////        {
//////////////////////            rt.anchorMin = Vector2.zero;
//////////////////////            rt.anchorMax = Vector2.one;
//////////////////////            rt.offsetMin = Vector2.zero;
//////////////////////            rt.offsetMax = Vector2.zero;
//////////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////////        }

//////////////////////        _placedInstance = unit.gameObject;
//////////////////////        HasUnit = true;
//////////////////////        PlacedVariantId = unit.variantId;

//////////////////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////////////////        RefreshVisuals();

//////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////////////                  $"placed on '{transform.parent.name}'.");
//////////////////////    }

//////////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////////    {
//////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////////        if (unit == null) return;

//////////////////////        _highlight?.SetActive(true);
//////////////////////        _bg.color = (unit.unitType == acceptedType && !HasUnit)
//////////////////////            ? hoverValidColor : hoverInvalidColor;
//////////////////////    }

//////////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////////    {
//////////////////////        _highlight?.SetActive(false);
//////////////////////        _bg.color = normalColor;
//////////////////////    }

//////////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////////    public void RemoveUnit()
//////////////////////    {
//////////////////////        if (_placedInstance != null)
//////////////////////        {
//////////////////////            Destroy(_placedInstance);
//////////////////////            _placedInstance = null;
//////////////////////        }

//////////////////////        HasUnit = false;
//////////////////////        PlacedVariantId = -1;
//////////////////////        RefreshVisuals();
//////////////////////    }

//////////////////////    // ── Visuals ───────────────────────────────────────────────────

//////////////////////    private void RefreshVisuals()
//////////////////////    {
//////////////////////        _emptyVisual?.SetActive(!HasUnit);
//////////////////////        _highlight?.SetActive(false);
//////////////////////        _bg.color = normalColor;
//////////////////////    }
//////////////////////}

////////////////////using UnityEngine;
////////////////////using UnityEngine.UI;
////////////////////using UnityEngine.EventSystems;

/////////////////////// <summary>
/////////////////////// One half of the unit slot overlay on an exposed castle block.
/////////////////////// Place two of these as children of CastleBlockUnitSlot:
///////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
///////////////////////
/////////////////////// Visual behaviour:
///////////////////////   • Empty, nothing being dragged  → zone is completely invisible
///////////////////////   • Compatible unit dragged over  → green highlight + EmptyVisual hint
///////////////////////   • Incompatible / zone full      → red highlight
///////////////////////   • Unit placed                   → placed prefab visible, zone invisible
///////////////////////
/////////////////////// Child hierarchy (auto-wired by name):
///////////////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////////////////////   ├── EmptyVisual     hint shown only during a valid drag hover
///////////////////////   └── Highlight       glow frame shown during any drag hover
/////////////////////// </summary>
////////////////////[RequireComponent(typeof(Image))]
////////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////////////{
////////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////////    public CastleUnitType acceptedType;

////////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0f);    // fully invisible
////////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////////////    private Image _bg;
////////////////////    private GameObject _emptyVisual;   // hint shown only on valid hover
////////////////////    private GameObject _highlight;     // glow frame

////////////////////    // ── State ─────────────────────────────────────────────────────
////////////////////    public bool HasUnit { get; private set; }
////////////////////    public int PlacedVariantId { get; private set; } = -1;

////////////////////    private GameObject _placedInstance;

////////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////////    private void Awake()
////////////////////    {
////////////////////        _bg = GetComponent<Image>();
////////////////////        _bg.color = normalColor;

////////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////////////        // Both hidden until a drag interaction begins
////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////    }

////////////////////    // ── Drop ──────────────────────────────────────────────────────

////////////////////    public void OnDrop(PointerEventData eventData)
////////////////////    {
////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////        if (unit == null) return;
////////////////////        if (unit.unitType != acceptedType) return;
////////////////////        if (HasUnit) return;

////////////////////        // Reparent the dragged unit into this zone
////////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////////////        if (rt != null)
////////////////////        {
////////////////////            rt.anchorMin = Vector2.zero;
////////////////////            rt.anchorMax = Vector2.one;
////////////////////            rt.offsetMin = Vector2.zero;
////////////////////            rt.offsetMax = Vector2.zero;
////////////////////            rt.anchoredPosition = Vector2.zero;
////////////////////            rt.SetAsLastSibling();
////////////////////        }

////////////////////        // Restore full opacity and raycasts now it is settled
////////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////////////        _placedInstance = unit.gameObject;
////////////////////        HasUnit = true;
////////////////////        PlacedVariantId = unit.variantId;

////////////////////        // Hide all zone chrome — the unit prefab is the only visible thing
////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////        _bg.color = normalColor;

////////////////////        CastleUnitDraggable.NotifyDropSucceeded();

////////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////////////                  $"placed on '{transform.parent.name}'.");
////////////////////    }

////////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////////    {
////////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////////        if (unit == null) return;

////////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;

////////////////////        _highlight?.SetActive(true);
////////////////////        _emptyVisual?.SetActive(valid);          // hint only when valid
////////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////////////    }

////////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////////    {
////////////////////        _highlight?.SetActive(false);
////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _bg.color = normalColor;
////////////////////    }

////////////////////    // ── Public API ────────────────────────────────────────────────

////////////////////    /// <summary>Destroys the placed unit and resets this zone to empty.</summary>
////////////////////    public void RemoveUnit()
////////////////////    {
////////////////////        if (_placedInstance != null)
////////////////////        {
////////////////////            Destroy(_placedInstance);
////////////////////            _placedInstance = null;
////////////////////        }

////////////////////        HasUnit = false;
////////////////////        PlacedVariantId = -1;

////////////////////        _emptyVisual?.SetActive(false);
////////////////////        _highlight?.SetActive(false);
////////////////////        _bg.color = normalColor;
////////////////////    }
////////////////////}

//////////////////using UnityEngine;
//////////////////using UnityEngine.UI;
//////////////////using UnityEngine.EventSystems;

///////////////////// <summary>
///////////////////// One half of the unit slot overlay on an exposed castle block.
///////////////////// Place two of these as children of CastleBlockUnitSlot:
/////////////////////   one with acceptedType = Cannon, one with acceptedType = Soldier.
/////////////////////
///////////////////// FIX APPLIED: normalColor alpha was 0f, which causes Unity to skip
///////////////////// raycasts on transparent Images — making OnDrop / OnPointerEnter
///////////////////// never fire. Changed to 0.01f (imperceptibly visible, fully raycasted).
/////////////////////
///////////////////// Child hierarchy (auto-wired by name):
/////////////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////////////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////////////////   └── Highlight       glow frame shown during any drag hover
///////////////////// </summary>
//////////////////[RequireComponent(typeof(Image))]
//////////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////////{
//////////////////    // ── Inspector ─────────────────────────────────────────────────
//////////////////    public CastleUnitType acceptedType;

//////////////////    // FIX: alpha was 0f — Unity skips raycasts on fully transparent Images.
//////////////////    // 0.01f is invisible to the eye but Unity WILL raycast it.
//////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////////    private Image _bg;
//////////////////    private GameObject _emptyVisual;
//////////////////    private GameObject _highlight;

//////////////////    // ── State ─────────────────────────────────────────────────────
//////////////////    public bool HasUnit { get; private set; }
//////////////////    public int PlacedVariantId { get; private set; } = -1;

//////////////////    private GameObject _placedInstance;

//////////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////////    private void Awake()
//////////////////    {
//////////////////        _bg = GetComponent<Image>();
//////////////////        _bg.color = normalColor;

//////////////////        // Guarantee raycasts are on — this must be true for OnDrop to fire.
//////////////////        _bg.raycastTarget = true;

//////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////    }

//////////////////    // ── Drop ──────────────────────────────────────────────────────

//////////////////    public void OnDrop(PointerEventData eventData)
//////////////////    {
//////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////        if (unit == null)
//////////////////        {
//////////////////            Debug.LogWarning("[CastleUnitDropZone] OnDrop fired but CurrentlyDragging is null.");
//////////////////            return;
//////////////////        }
//////////////////        if (unit.unitType != acceptedType)
//////////////////        {
//////////////////            Debug.Log($"[CastleUnitDropZone] Wrong type: got {unit.unitType}, need {acceptedType}.");
//////////////////            return;
//////////////////        }
//////////////////        if (HasUnit)
//////////////////        {
//////////////////            Debug.Log($"[CastleUnitDropZone] Zone already occupied.");
//////////////////            return;
//////////////////        }

//////////////////        // Reparent the dragged unit into this zone
//////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////////        if (rt != null)
//////////////////        {
//////////////////            rt.anchorMin = Vector2.zero;
//////////////////            rt.anchorMax = Vector2.one;
//////////////////            rt.offsetMin = Vector2.zero;
//////////////////            rt.offsetMax = Vector2.zero;
//////////////////            rt.anchoredPosition = Vector2.zero;
//////////////////            rt.SetAsLastSibling();
//////////////////        }

//////////////////        // Re-enable raycasts and full opacity now the unit is settled
//////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////////////        _placedInstance = unit.gameObject;
//////////////////        HasUnit = true;
//////////////////        PlacedVariantId = unit.variantId;

//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////        _bg.color = normalColor;

//////////////////        CastleUnitDraggable.NotifyDropSucceeded();

//////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////////                  $"placed on '{transform.parent?.name}'.");
//////////////////    }

//////////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////////    {
//////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////////        if (unit == null) return;

//////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;

//////////////////        _highlight?.SetActive(true);
//////////////////        _emptyVisual?.SetActive(valid);
//////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////////////    }

//////////////////    public void OnPointerExit(PointerEventData eventData)
//////////////////    {
//////////////////        _highlight?.SetActive(false);
//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _bg.color = normalColor;
//////////////////    }

//////////////////    // ── Public API ────────────────────────────────────────────────

//////////////////    /// <summary>Destroys the placed unit and resets this zone to empty.</summary>
//////////////////    public void RemoveUnit()
//////////////////    {
//////////////////        if (_placedInstance != null)
//////////////////        {
//////////////////            Destroy(_placedInstance);
//////////////////            _placedInstance = null;
//////////////////        }

//////////////////        HasUnit = false;
//////////////////        PlacedVariantId = -1;

//////////////////        _emptyVisual?.SetActive(false);
//////////////////        _highlight?.SetActive(false);
//////////////////        _bg.color = normalColor;
//////////////////    }
//////////////////}

////////////////using UnityEngine;
////////////////using UnityEngine.UI;
////////////////using UnityEngine.EventSystems;

/////////////////// <summary>
/////////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
///////////////////
/////////////////// acceptedType must be public so GridCell.FindDropZoneForType() can read it.
/////////////////// PlaceUnit(CastleUnitDraggable) is public so ExpansionSlot can call it
/////////////////// directly when routing a unit drop to the block below the expansion slot.
/////////////////// </summary>
////////////////[RequireComponent(typeof(Image))]
////////////////public class CastleUnitDropZone : MonoBehaviour,
////////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////////{
////////////////    // ── Inspector ─────────────────────────────────────────────────
////////////////    public CastleUnitType acceptedType; // public — read by FindDropZoneForType

////////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f); // near-invisible but raycasts
////////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////////    private Image _bg;
////////////////    private GameObject _emptyVisual;
////////////////    private GameObject _highlight;

////////////////    // ── State ─────────────────────────────────────────────────────
////////////////    public bool HasUnit { get; private set; }
////////////////    public int PlacedVariantId { get; private set; } = -1;
////////////////    private GameObject _placedInstance;

////////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        _bg = GetComponent<Image>();
////////////////        _bg.color = normalColor;
////////////////        _bg.raycastTarget = true;

////////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////    }

////////////////    // ── Standard drag-drop path ───────────────────────────────────

////////////////    public void OnDrop(PointerEventData eventData)
////////////////    {
////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////        if (unit == null || unit.unitType != acceptedType || HasUnit) return;

////////////////        PlaceUnit(unit);
////////////////        CastleUnitDraggable.NotifyDropSucceeded();
////////////////    }

////////////////    // ── Public: also called by ExpansionSlot ──────────────────────

////////////////    /// <summary>
////////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
////////////////    /// Called from OnDrop (normal path) and from ExpansionSlot.OnDrop
////////////////    /// (unit dropped on expansion slot → seated on block below).
////////////////    /// </summary>
////////////////    public void PlaceUnit(CastleUnitDraggable unit)
////////////////    {
////////////////        if (unit == null || HasUnit) return;

////////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////////        if (rt != null)
////////////////        {
////////////////            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
////////////////            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
////////////////            rt.anchoredPosition = Vector2.zero;
////////////////            rt.SetAsLastSibling();
////////////////        }

////////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////////        _placedInstance = unit.gameObject;
////////////////        HasUnit = true;
////////////////        PlacedVariantId = unit.variantId;

////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////        _bg.color = normalColor;

////////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////////                  $"placed on '{transform.parent?.name}'.");
////////////////    }

////////////////    // ── Hover ─────────────────────────────────────────────────────

////////////////    public void OnPointerEnter(PointerEventData eventData)
////////////////    {
////////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////////        if (unit == null) return;

////////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////////////        _highlight?.SetActive(true);
////////////////        _emptyVisual?.SetActive(valid);
////////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////////    }

////////////////    public void OnPointerExit(PointerEventData eventData)
////////////////    {
////////////////        _highlight?.SetActive(false);
////////////////        _emptyVisual?.SetActive(false);
////////////////        _bg.color = normalColor;
////////////////    }

////////////////    // ── Remove ────────────────────────────────────────────────────

////////////////    public void RemoveUnit()
////////////////    {
////////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
////////////////        HasUnit = false; PlacedVariantId = -1;
////////////////        _emptyVisual?.SetActive(false);
////////////////        _highlight?.SetActive(false);
////////////////        _bg.color = normalColor;
////////////////    }
////////////////}

//////////////using UnityEngine;
//////////////using UnityEngine.UI;
//////////////using UnityEngine.EventSystems;

///////////////// <summary>
///////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
/////////////////
///////////////// acceptedType is set by CastleBlockUnitSlot.Awake() in code — do NOT rely
///////////////// on the Inspector value; it will be overwritten at runtime.
/////////////////
///////////////// PlaceUnit(CastleUnitDraggable) is public so ExpansionSlot can call it
///////////////// directly when routing a unit drop to the block below the expansion slot.
///////////////// </summary>
//////////////[RequireComponent(typeof(Image))]
//////////////public class CastleUnitDropZone : MonoBehaviour,
//////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////////{
//////////////    // ── Inspector / set-by-code ───────────────────────────────────
//////////////    // CastleBlockUnitSlot.Awake() overwrites this — Inspector value
//////////////    // is kept only as a fallback for standalone testing.
//////////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). " +
//////////////             "Manual Inspector value is overwritten at runtime.")]
//////////////    public CastleUnitType acceptedType;

//////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////////    // ── Auto-wired ────────────────────────────────────────────────
//////////////    private Image _bg;
//////////////    private GameObject _emptyVisual;
//////////////    private GameObject _highlight;

//////////////    // ── State ─────────────────────────────────────────────────────
//////////////    public bool HasUnit { get; private set; }
//////////////    public int PlacedVariantId { get; private set; } = -1;
//////////////    private GameObject _placedInstance;

//////////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        _bg = GetComponent<Image>();
//////////////        _bg.color = normalColor;
//////////////        _bg.raycastTarget = true;   // MUST be true or OnDrop never fires

//////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////////        _highlight = transform.Find("Highlight")?.gameObject;

//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////    }

//////////////    // ── Standard drag-drop path ───────────────────────────────────

//////////////    public void OnDrop(PointerEventData eventData)
//////////////    {
//////////////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////////////        // ── Verbose rejection logging so you can see exactly why a
//////////////        //    drop failed in the Console (remove after debugging). ──
//////////////        if (unit == null)
//////////////        {
//////////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but CurrentlyDragging is null.");
//////////////            return;
//////////////        }
//////////////        if (unit.unitType != acceptedType)
//////////////        {
//////////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
//////////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
//////////////            return;
//////////////        }
//////////////        if (HasUnit)
//////////////        {
//////////////            Debug.Log($"[CastleUnitDropZone] Zone already occupied ({acceptedType}). Drop rejected.");
//////////////            return;
//////////////        }

//////////////        PlaceUnit(unit);
//////////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////////    }

//////////////    // ── Public: also called by ExpansionSlot ──────────────────────

//////////////    /// <summary>
//////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
//////////////    /// Called from OnDrop (normal path) and ExpansionSlot.OnDrop
//////////////    /// (unit dropped on expansion slot → seated on block below).
//////////////    /// </summary>
//////////////    public void PlaceUnit(CastleUnitDraggable unit)
//////////////    {
//////////////        if (unit == null || HasUnit) return;

//////////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////////        if (rt != null)
//////////////        {
//////////////            rt.anchorMin = Vector2.zero;
//////////////            rt.anchorMax = Vector2.one;
//////////////            rt.offsetMin = Vector2.zero;
//////////////            rt.offsetMax = Vector2.zero;
//////////////            rt.anchoredPosition = Vector2.zero;
//////////////            rt.SetAsLastSibling();
//////////////        }

//////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////////        _placedInstance = unit.gameObject;
//////////////        HasUnit = true;
//////////////        PlacedVariantId = unit.variantId;

//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////        _bg.color = normalColor;

//////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////////                  $"placed on '{transform.parent?.name}'.");
//////////////    }

//////////////    // ── Hover ─────────────────────────────────────────────────────

//////////////    public void OnPointerEnter(PointerEventData eventData)
//////////////    {
//////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////////        if (unit == null) return;

//////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////////////        _highlight?.SetActive(true);
//////////////        _emptyVisual?.SetActive(valid);
//////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////////    }

//////////////    public void OnPointerExit(PointerEventData eventData)
//////////////    {
//////////////        _highlight?.SetActive(false);
//////////////        _emptyVisual?.SetActive(false);
//////////////        _bg.color = normalColor;
//////////////    }

//////////////    // ── Remove ────────────────────────────────────────────────────

//////////////    public void RemoveUnit()
//////////////    {
//////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
//////////////        HasUnit = false;
//////////////        PlacedVariantId = -1;
//////////////        _emptyVisual?.SetActive(false);
//////////////        _highlight?.SetActive(false);
//////////////        _bg.color = normalColor;
//////////////    }
//////////////}


////////////using UnityEngine;
////////////using UnityEngine.UI;
////////////using UnityEngine.EventSystems;

/////////////// <summary>
/////////////// One half of the CastleBlockUnitSlot overlay on an exposed castle block.
///////////////
/////////////// acceptedType is enforced by CastleBlockUnitSlot.Awake() in code —
/////////////// the Inspector value is overwritten at runtime.
///////////////
/////////////// PlaceUnit respects CastleUnitDraggable.stretchToFillSlot:
///////////////   TRUE  → stretch-anchors the unit to fill this zone rectangle.
///////////////   FALSE → centers the unit at a fixed size (safe for customized / animated soldier prefabs).
/////////////// </summary>
////////////[RequireComponent(typeof(Image))]
////////////public class CastleUnitDropZone : MonoBehaviour,
////////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////////{
////////////    // ── Inspector ─────────────────────────────────────────────────
////////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////////////    public CastleUnitType acceptedType;

////////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false " +
////////////             "(i.e. customized / animated soldier prefabs).")]
////////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////////    // ── Auto-wired ────────────────────────────────────────────────
////////////    private Image _bg;
////////////    private GameObject _emptyVisual;
////////////    private GameObject _highlight;

////////////    // ── State ─────────────────────────────────────────────────────
////////////    public bool HasUnit { get; private set; }
////////////    public int PlacedVariantId { get; private set; } = -1;
////////////    private GameObject _placedInstance;

////////////    // ── Lifecycle ─────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        _bg = GetComponent<Image>();
////////////        _bg.color = normalColor;
////////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

////////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////////        _highlight = transform.Find("Highlight")?.gameObject;

////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////    }

////////////    // ── Standard drag-drop path ───────────────────────────────────

////////////    public void OnDrop(PointerEventData eventData)
////////////    {
////////////        var unit = CastleUnitDraggable.CurrentlyDragging;

////////////        if (unit == null)
////////////        {
////////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
////////////            return;
////////////        }
////////////        if (unit.unitType != acceptedType)
////////////        {
////////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
////////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
////////////            return;
////////////        }
////////////        if (HasUnit)
////////////        {
////////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
////////////            return;
////////////        }

////////////        PlaceUnit(unit);
////////////        CastleUnitDraggable.NotifyDropSucceeded();
////////////    }

////////////    // ── Public: also called by ExpansionSlot ──────────────────────

////////////    /// <summary>
////////////    /// Reparents <paramref name="unit"/> into this zone and marks it occupied.
////////////    ///
////////////    /// stretchToFillSlot = true  → anchors 0→1 to fill the zone (simple icons).
////////////    /// stretchToFillSlot = false → centered at <see cref="centeredUnitSize"/>
////////////    ///                             (customized / animated soldier prefabs — prevents
////////////    ///                             broken child layouts that cause the unit to look
////////////    ///                             invisible or distorted after placement).
////////////    /// </summary>
////////////    public void PlaceUnit(CastleUnitDraggable unit)
////////////    {
////////////        if (unit == null || HasUnit) return;

////////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////////        if (rt != null)
////////////        {
////////////            if (unit.stretchToFillSlot)
////////////            {
////////////                // Stretch to fill the entire zone rectangle
////////////                rt.anchorMin = Vector2.zero;
////////////                rt.anchorMax = Vector2.one;
////////////                rt.offsetMin = Vector2.zero;
////////////                rt.offsetMax = Vector2.zero;
////////////                rt.anchoredPosition = Vector2.zero;
////////////            }
////////////            else
////////////            {
////////////                // ── FIX for customized soldier prefabs ────────────────
////////////                // Center at a fixed pixel size instead of stretching.
////////////                // Stretching breaks child Animators, multi-Image hierarchies,
////////////                // and fixed-pixel children — making the unit invisible/distorted.
////////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////////                rt.sizeDelta = centeredUnitSize;
////////////                rt.anchoredPosition = Vector2.zero;
////////////            }

////////////            rt.SetAsLastSibling();
////////////        }

////////////        // Re-enable raycasts and full opacity now the unit is settled
////////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////////        _placedInstance = unit.gameObject;
////////////        HasUnit = true;
////////////        PlacedVariantId = unit.variantId;

////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////        _bg.color = normalColor;

////////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////////                  $"placed on '{transform.parent?.name}' [stretch={unit.stretchToFillSlot}].");
////////////    }

////////////    // ── Hover ─────────────────────────────────────────────────────

////////////    public void OnPointerEnter(PointerEventData eventData)
////////////    {
////////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////////        if (unit == null) return;

////////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////////        _highlight?.SetActive(true);
////////////        _emptyVisual?.SetActive(valid);
////////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////////    }

////////////    public void OnPointerExit(PointerEventData eventData)
////////////    {
////////////        _highlight?.SetActive(false);
////////////        _emptyVisual?.SetActive(false);
////////////        _bg.color = normalColor;
////////////    }

////////////    // ── Remove ────────────────────────────────────────────────────

////////////    public void RemoveUnit()
////////////    {
////////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
////////////        HasUnit = false;
////////////        PlacedVariantId = -1;
////////////        _emptyVisual?.SetActive(false);
////////////        _highlight?.SetActive(false);
////////////        _bg.color = normalColor;
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using UnityEngine.EventSystems;

///////////// <summary>
///////////// Drop zone for a Cannon unit on an exposed castle block.
/////////////
///////////// When a cannon is successfully placed, a child Image named "Soldier"
///////////// (already present in the prefab hierarchy) is automatically made visible
///////////// alongside it — no separate soldier drag-and-drop required.
/////////////
///////////// Child hierarchy (auto-wired by name in Awake):
/////////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////////   ├── Highlight       glow frame shown during any drag hover
/////////////   └── Soldier         Image — hidden by default, shown when cannon is placed
///////////// </summary>
//////////[RequireComponent(typeof(Image))]
//////////public class CastleUnitDropZone : MonoBehaviour,
//////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////////{
//////////    // ── Inspector ─────────────────────────────────────────────────
//////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////////    public CastleUnitType acceptedType;

//////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////////    // ── Auto-wired ────────────────────────────────────────────────
//////////    private Image _bg;
//////////    private GameObject _emptyVisual;
//////////    private GameObject _highlight;
//////////    private GameObject _soldierImage;   // child named "Soldier" — shown with the cannon

//////////    // ── State ─────────────────────────────────────────────────────
//////////    public bool HasUnit { get; private set; }
//////////    public int PlacedVariantId { get; private set; } = -1;
//////////    private GameObject _placedInstance;

//////////    // ── Lifecycle ─────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        _bg = GetComponent<Image>();
//////////        _bg.color = normalColor;
//////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

//////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////////        _highlight = transform.Find("Highlight")?.gameObject;
//////////        _soldierImage = transform.Find("Soldier")?.gameObject;

//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _soldierImage?.SetActive(false);   // hidden until a cannon is placed
//////////    }

//////////    // ── Standard drag-drop path ───────────────────────────────────

//////////    public void OnDrop(PointerEventData eventData)
//////////    {
//////////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////////        if (unit == null)
//////////        {
//////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
//////////            return;
//////////        }
//////////        if (unit.unitType != acceptedType)
//////////        {
//////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
//////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
//////////            return;
//////////        }
//////////        if (HasUnit)
//////////        {
//////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
//////////            return;
//////////        }

//////////        PlaceUnit(unit);
//////////        CastleUnitDraggable.NotifyDropSucceeded();
//////////    }

//////////    // ── Public: also called by ExpansionSlot ──────────────────────

//////////    /// <summary>
//////////    /// Reparents <paramref name="unit"/> into this zone, marks it occupied,
//////////    /// and reveals the Soldier image that lives alongside the cannon.
//////////    /// </summary>
//////////    public void PlaceUnit(CastleUnitDraggable unit)
//////////    {
//////////        if (unit == null || HasUnit) return;

//////////        unit.transform.SetParent(transform, worldPositionStays: false);

//////////        RectTransform rt = unit.GetComponent<RectTransform>();
//////////        if (rt != null)
//////////        {
//////////            if (unit.stretchToFillSlot)
//////////            {
//////////                rt.anchorMin = Vector2.zero;
//////////                rt.anchorMax = Vector2.one;
//////////                rt.offsetMin = Vector2.zero;
//////////                rt.offsetMax = Vector2.zero;
//////////                rt.anchoredPosition = Vector2.zero;
//////////            }
//////////            else
//////////            {
//////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////////                rt.pivot = new Vector2(0.5f, 0.5f);
//////////                rt.sizeDelta = centeredUnitSize;
//////////                rt.anchoredPosition = Vector2.zero;
//////////            }

//////////            rt.SetAsLastSibling();
//////////        }

//////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////////        _placedInstance = unit.gameObject;
//////////        HasUnit = true;
//////////        PlacedVariantId = unit.variantId;

//////////        // Hide zone chrome
//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;

//////////        // ── Show the soldier image that lives beside the cannon ────
//////////        _soldierImage?.SetActive(true);

//////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////////                  $"placed on '{transform.parent?.name}' [stretch={unit.stretchToFillSlot}].");
//////////    }

//////////    // ── Hover ─────────────────────────────────────────────────────

//////////    public void OnPointerEnter(PointerEventData eventData)
//////////    {
//////////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////////        if (unit == null) return;

//////////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////////        _highlight?.SetActive(true);
//////////        _emptyVisual?.SetActive(valid);
//////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////////    }

//////////    public void OnPointerExit(PointerEventData eventData)
//////////    {
//////////        _highlight?.SetActive(false);
//////////        _emptyVisual?.SetActive(false);
//////////        _bg.color = normalColor;
//////////    }

//////////    // ── Remove ────────────────────────────────────────────────────

//////////    /// <summary>Destroys the placed cannon and hides the soldier image.</summary>
//////////    public void RemoveUnit()
//////////    {
//////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//////////        HasUnit = false;
//////////        PlacedVariantId = -1;

//////////        _soldierImage?.SetActive(false);   // hide soldier when cannon is removed
//////////        _emptyVisual?.SetActive(false);
//////////        _highlight?.SetActive(false);
//////////        _bg.color = normalColor;
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;
////////using UnityEngine.EventSystems;

/////////// <summary>
/////////// Drop zone for a Cannon unit on an exposed castle block.
///////////
/////////// When a cannon is successfully placed, a child Image named "Soldier"
/////////// (already present in the prefab hierarchy) is automatically made visible
/////////// alongside it — no separate soldier drag-and-drop required.
///////////
/////////// Full lifecycle:
///////////   PlaceUnit    → cannon placed here from drag; soldier shown.
///////////   DetachUnit   → cannon is being dragged away; soldier hidden, zone reset to empty.
///////////                  Does NOT destroy the cannon — it is still alive being dragged.
///////////   ReattachUnit → drag was cancelled / dropped on invalid target; cannon snapped
///////////                  back to this zone; zone state restored as if it was never moved.
///////////   RemoveUnit   → cannon destroyed (block removed etc.); soldier hidden.
///////////
/////////// Child hierarchy (auto-wired by name in Awake):
///////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////////   ├── EmptyVisual     hint shown only during a valid drag hover
///////////   ├── Highlight       glow frame shown during any drag hover
///////////   └── Soldier         Image — hidden by default, shown when cannon is placed
/////////// </summary>
////////[RequireComponent(typeof(Image))]
////////public class CastleUnitDropZone : MonoBehaviour,
////////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////{
////////    // ── Inspector ─────────────────────────────────────────────────
////////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////////    public CastleUnitType acceptedType;

////////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
////////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////////    // ── Auto-wired ────────────────────────────────────────────────
////////    private Image _bg;
////////    private GameObject _emptyVisual;
////////    private GameObject _highlight;
////////    private GameObject _soldierImage;   // child named "Soldier" — shown alongside the cannon

////////    // ── State ─────────────────────────────────────────────────────
////////    public bool HasUnit { get; private set; }
////////    public int PlacedVariantId { get; private set; } = -1;
////////    private GameObject _placedInstance;

////////    // ── Lifecycle ─────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        _bg = GetComponent<Image>();
////////        _bg.color = normalColor;
////////        _bg.raycastTarget = true;   // MUST stay true or OnDrop never fires

////////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////////        _highlight = transform.Find("Highlight")?.gameObject;
////////        _soldierImage = transform.Find("Soldier")?.gameObject;

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _soldierImage?.SetActive(false);
////////    }

////////    // ── Standard drag-drop path ───────────────────────────────────

////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        var unit = CastleUnitDraggable.CurrentlyDragging;

////////        if (unit == null)
////////        {
////////            Debug.Log("[CastleUnitDropZone] OnDrop fired but nothing is being dragged.");
////////            return;
////////        }
////////        if (unit.unitType != acceptedType)
////////        {
////////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType} " +
////////                      $"but this zone accepts {acceptedType}. Drop rejected.");
////////            return;
////////        }
////////        if (HasUnit)
////////        {
////////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied. Drop rejected.");
////////            return;
////////        }

////////        PlaceUnit(unit);
////////        CastleUnitDraggable.NotifyDropSucceeded();
////////    }

////////    // ── Public: also called by ExpansionSlot ──────────────────────

////////    /// <summary>
////////    /// Reparents <paramref name="unit"/> into this zone, marks it occupied,
////////    /// and reveals the Soldier image that lives alongside the cannon.
////////    /// </summary>
////////    public void PlaceUnit(CastleUnitDraggable unit)
////////    {
////////        if (unit == null || HasUnit) return;

////////        unit.transform.SetParent(transform, worldPositionStays: false);

////////        RectTransform rt = unit.GetComponent<RectTransform>();
////////        if (rt != null)
////////        {
////////            if (unit.stretchToFillSlot)
////////            {
////////                rt.anchorMin = Vector2.zero;
////////                rt.anchorMax = Vector2.one;
////////                rt.offsetMin = Vector2.zero;
////////                rt.offsetMax = Vector2.zero;
////////                rt.anchoredPosition = Vector2.zero;
////////            }
////////            else
////////            {
////////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////////                rt.pivot = new Vector2(0.5f, 0.5f);
////////                rt.sizeDelta = centeredUnitSize;
////////                rt.anchoredPosition = Vector2.zero;
////////            }
////////            rt.SetAsLastSibling();
////////        }

////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////        _placedInstance = unit.gameObject;
////////        HasUnit = true;
////////        PlacedVariantId = unit.variantId;

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;
////////        _soldierImage?.SetActive(true);

////////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////////                  $"placed on '{transform.parent?.name}'.");
////////    }

////////    /// <summary>
////////    /// Called by <see cref="CastleUnitDraggable.OnBeginDrag"/> when the already-placed
////////    /// cannon starts being dragged away from this zone.
////////    /// Clears zone state and hides the soldier WITHOUT destroying the cannon
////////    /// (it is still alive — currently being dragged).
////////    /// </summary>
////////    public void DetachUnit()
////////    {
////////        _placedInstance = null;
////////        HasUnit = false;
////////        PlacedVariantId = -1;

////////        _soldierImage?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;

////////        Debug.Log($"[CastleUnitDropZone] Cannon detached from '{transform.parent?.name}' — soldier hidden.");
////////    }

////////    /// <summary>
////////    /// Called by <see cref="CastleUnitDraggable.OnEndDrag"/> when the drag was cancelled
////////    /// (dropped on an invalid target) and the cannon snaps back to this zone.
////////    /// Restores zone state as if the cannon was never moved.
////////    /// </summary>
////////    public void ReattachUnit(CastleUnitDraggable unit)
////////    {
////////        if (unit == null) return;

////////        _placedInstance = unit.gameObject;
////////        HasUnit = true;
////////        PlacedVariantId = unit.variantId;

////////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;
////////        _soldierImage?.SetActive(true);

////////        Debug.Log($"[CastleUnitDropZone] Cannon snapped back to '{transform.parent?.name}' — soldier restored.");
////////    }

////////    // ── Hover ─────────────────────────────────────────────────────

////////    public void OnPointerEnter(PointerEventData eventData)
////////    {
////////        var unit = CastleUnitDraggable.CurrentlyDragging;
////////        if (unit == null) return;

////////        bool valid = unit.unitType == acceptedType && !HasUnit;
////////        _highlight?.SetActive(true);
////////        _emptyVisual?.SetActive(valid);
////////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////////    }

////////    public void OnPointerExit(PointerEventData eventData)
////////    {
////////        _highlight?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _bg.color = normalColor;
////////    }

////////    // ── Remove (cannon destroyed externally, e.g. block removed) ──

////////    /// <summary>Destroys the placed cannon and hides the soldier image.</summary>
////////    public void RemoveUnit()
////////    {
////////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }
////////        HasUnit = false;
////////        PlacedVariantId = -1;

////////        _soldierImage?.SetActive(false);
////////        _emptyVisual?.SetActive(false);
////////        _highlight?.SetActive(false);
////////        _bg.color = normalColor;
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using UnityEngine.EventSystems;

///////// <summary>
///////// Drop zone for a Cannon unit on an exposed castle block.
/////////
///////// When a cannon is placed via an ExpansionSlot, that slot registers itself
///////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
///////// hides / shows that slot as the cannon moves:
/////////
/////////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
/////////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
/////////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
/////////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
/////////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
/////////
///////// Child hierarchy (auto-wired by name in Awake):
/////////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////////   ├── EmptyVisual     hint shown only during a valid drag hover
/////////   ├── Highlight       glow frame shown during any drag hover
/////////   └── Soldier         Image — hidden by default, shown when cannon is placed
///////// </summary>
//////[RequireComponent(typeof(Image))]
//////public class CastleUnitDropZone : MonoBehaviour,
//////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////{
//////    // ── Inspector ─────────────────────────────────────────────────
//////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//////    public CastleUnitType acceptedType;

//////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//////    // ── Auto-wired ────────────────────────────────────────────────
//////    private Image _bg;
//////    private GameObject _emptyVisual;
//////    private GameObject _highlight;
//////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

//////    // ── State ─────────────────────────────────────────────────────
//////    public bool HasUnit { get; private set; }
//////    public int PlacedVariantId { get; private set; } = -1;

//////    /// <summary>
//////    /// The ExpansionSlot that was used to place the cannon here.
//////    /// Set by ExpansionSlot.OnDrop. The zone uses it to show/hide that
//////    /// slot when the cannon arrives, leaves, or is destroyed.
//////    /// Null when the cannon was dropped directly onto this zone.
//////    /// </summary>
//////    public ExpansionSlot LinkedExpansionSlot { get; set; }

//////    private GameObject _placedInstance;

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _bg = GetComponent<Image>();
//////        _bg.color = normalColor;
//////        _bg.raycastTarget = true;

//////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//////        _highlight = transform.Find("Highlight")?.gameObject;
//////        _soldierImage = transform.Find("Soldier")?.gameObject;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _soldierImage?.SetActive(false);
//////    }

//////    // ── Standard drag-drop path (direct drop onto this zone) ──────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;

//////        if (unit == null)
//////        {
//////            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
//////            return;
//////        }
//////        if (unit.unitType != acceptedType)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
//////                      $"zone accepts {acceptedType}.");
//////            return;
//////        }
//////        if (HasUnit)
//////        {
//////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
//////            return;
//////        }

//////        PlaceUnit(unit);
//////        CastleUnitDraggable.NotifyDropSucceeded();
//////    }

//////    // ── Public API ────────────────────────────────────────────────

//////    /// <summary>
//////    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
//////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//////    /// </summary>
//////    public void PlaceUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null || HasUnit) return;

//////        unit.transform.SetParent(transform, worldPositionStays: false);

//////        RectTransform rt = unit.GetComponent<RectTransform>();
//////        if (rt != null)
//////        {
//////            if (unit.stretchToFillSlot)
//////            {
//////                rt.anchorMin = Vector2.zero;
//////                rt.anchorMax = Vector2.one;
//////                rt.offsetMin = Vector2.zero;
//////                rt.offsetMax = Vector2.zero;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            else
//////            {
//////                rt.anchorMin = new Vector2(0.5f, 0.5f);
//////                rt.anchorMax = new Vector2(0.5f, 0.5f);
//////                rt.pivot = new Vector2(0.5f, 0.5f);
//////                rt.sizeDelta = centeredUnitSize;
//////                rt.anchoredPosition = Vector2.zero;
//////            }
//////            rt.SetAsLastSibling();
//////        }

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//////                  $"placed on '{transform.parent?.name}'.");
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
//////    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
//////    /// Does NOT destroy the cannon — it is still alive being dragged.
//////    /// </summary>
//////    public void DetachUnit()
//////    {
//////        _placedInstance = null;
//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Reveal the expansion slot above so the cannon (or another) can be
//////        // dropped there again
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            Debug.Log($"[CastleUnitDropZone] Detached — expansion slot restored.");
//////        }
//////    }

//////    /// <summary>
//////    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
//////    /// snaps back here. Restores zone state exactly as before the drag started.
//////    /// </summary>
//////    public void ReattachUnit(CastleUnitDraggable unit)
//////    {
//////        if (unit == null) return;

//////        _placedInstance = unit.gameObject;
//////        HasUnit = true;
//////        PlacedVariantId = unit.variantId;

//////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;
//////        _soldierImage?.SetActive(true);

//////        // Hide the expansion slot again — cannon is back on this block
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(false);
//////            Debug.Log($"[CastleUnitDropZone] Reattached — expansion slot hidden again.");
//////        }
//////    }

//////    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
//////    public void RemoveUnit()
//////    {
//////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//////        HasUnit = false;
//////        PlacedVariantId = -1;

//////        _soldierImage?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _highlight?.SetActive(false);
//////        _bg.color = normalColor;

//////        // Restore the expansion slot — the block is now cannon-free
//////        if (LinkedExpansionSlot != null)
//////        {
//////            LinkedExpansionSlot.gameObject.SetActive(true);
//////            LinkedExpansionSlot = null;
//////            Debug.Log($"[CastleUnitDropZone] Removed — expansion slot restored.");
//////        }
//////    }

//////    // ── Hover ─────────────────────────────────────────────────────

//////    public void OnPointerEnter(PointerEventData eventData)
//////    {
//////        var unit = CastleUnitDraggable.CurrentlyDragging;
//////        if (unit == null) return;

//////        bool valid = unit.unitType == acceptedType && !HasUnit;
//////        _highlight?.SetActive(true);
//////        _emptyVisual?.SetActive(valid);
//////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//////    }

//////    public void OnPointerExit(PointerEventData eventData)
//////    {
//////        _highlight?.SetActive(false);
//////        _emptyVisual?.SetActive(false);
//////        _bg.color = normalColor;
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using UnityEngine.EventSystems;

/////// <summary>
/////// Drop zone for a Cannon unit on an exposed castle block.
///////
/////// When a cannon is placed via an ExpansionSlot, that slot registers itself
/////// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
/////// hides / shows that slot as the cannon moves:
///////
///////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
///////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
///////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
///////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
///////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
///////
/////// Child hierarchy (auto-wired by name in Awake):
///////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///////   ├── EmptyVisual     hint shown only during a valid drag hover
///////   ├── Highlight       glow frame shown during any drag hover
///////   └── Soldier         Image — hidden by default, shown when cannon is placed
/////// </summary>
////[RequireComponent(typeof(Image))]
////public class CastleUnitDropZone : MonoBehaviour,
////    IDropHandler, IPointerEnterHandler, IPointerExitHandler
////{
////    // ── Inspector ─────────────────────────────────────────────────
////    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
////    public CastleUnitType acceptedType;

////    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
////    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

////    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
////    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
////    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

////    // ── Auto-wired ────────────────────────────────────────────────
////    private Image _bg;
////    private GameObject _emptyVisual;
////    private GameObject _highlight;
////    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

////    // ── State ─────────────────────────────────────────────────────
////    public bool HasUnit { get; private set; }
////    public int PlacedVariantId { get; private set; } = -1;

////    /// <summary>
////    /// The ExpansionSlot that was used to place the cannon here.
////    /// Set by ExpansionSlot.OnDrop, or pre-linked by ExpansionSlot.Init.
////    /// The zone uses it to show/hide that slot when the cannon arrives,
////    /// leaves, or is destroyed.
////    /// Null when no expansion slot is associated with this zone.
////    /// </summary>
////    public ExpansionSlot LinkedExpansionSlot { get; set; }

////    private GameObject _placedInstance;

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        _bg = GetComponent<Image>();
////        _bg.color = normalColor;
////        _bg.raycastTarget = true;

////        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
////        _highlight = transform.Find("Highlight")?.gameObject;
////        _soldierImage = transform.Find("Soldier")?.gameObject;

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _soldierImage?.SetActive(false);

////        // FIX 1 — The Soldier child is a pure visual overlay.
////        // It must NOT be a raycast target, otherwise it blocks pointer events
////        // from reaching the cannon that sits on top of it, making the
////        // placed cannon impossible to drag.
////        if (_soldierImage != null)
////        {
////            Image soldierImg = _soldierImage.GetComponent<Image>();
////            if (soldierImg != null)
////                soldierImg.raycastTarget = false;
////        }
////    }

////    // ── Standard drag-drop path (direct drop onto this zone) ──────

////    public void OnDrop(PointerEventData eventData)
////    {
////        var unit = CastleUnitDraggable.CurrentlyDragging;

////        if (unit == null)
////        {
////            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
////            return;
////        }
////        if (unit.unitType != acceptedType)
////        {
////            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
////                      $"zone accepts {acceptedType}.");
////            return;
////        }
////        if (HasUnit)
////        {
////            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
////            return;
////        }

////        PlaceUnit(unit);
////        CastleUnitDraggable.NotifyDropSucceeded();
////    }

////    // ── Public API ────────────────────────────────────────────────

////    /// <summary>
////    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
////    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
////    /// </summary>
////    public void PlaceUnit(CastleUnitDraggable unit)
////    {
////        if (unit == null || HasUnit) return;

////        unit.transform.SetParent(transform, worldPositionStays: false);

////        RectTransform rt = unit.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            if (unit.stretchToFillSlot)
////            {
////                rt.anchorMin = Vector2.zero;
////                rt.anchorMax = Vector2.one;
////                rt.offsetMin = Vector2.zero;
////                rt.offsetMax = Vector2.zero;
////                rt.anchoredPosition = Vector2.zero;
////            }
////            else
////            {
////                rt.anchorMin = new Vector2(0.5f, 0.5f);
////                rt.anchorMax = new Vector2(0.5f, 0.5f);
////                rt.pivot = new Vector2(0.5f, 0.5f);
////                rt.sizeDelta = centeredUnitSize;
////                rt.anchoredPosition = Vector2.zero;
////            }
////            rt.SetAsLastSibling();
////        }

////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////        // FIX — Re-assert raycastTarget = true on the cannon's own Image after
////        // reparenting.  This is the authoritative moment the cannon becomes
////        // interactive again; ensuring it here means the fix holds even if
////        // Awake's guarantee is somehow lost (e.g. prefab variant overrides).
////        Image unitImg = unit.GetComponent<Image>();
////        if (unitImg != null) unitImg.raycastTarget = true;

////        _placedInstance = unit.gameObject;
////        HasUnit = true;
////        PlacedVariantId = unit.variantId;

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;
////        _soldierImage?.SetActive(true);

////        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
////                  $"placed on '{transform.parent?.name}'.");
////    }

////    /// <summary>
////    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
////    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
////    /// Does NOT destroy the cannon — it is still alive being dragged.
////    /// </summary>
////    public void DetachUnit()
////    {
////        _placedInstance = null;
////        HasUnit = false;
////        PlacedVariantId = -1;

////        _soldierImage?.SetActive(false);
////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;

////        // Reveal the expansion slot above so the cannon (or another) can be
////        // dropped there again.
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(true);
////            Debug.Log($"[CastleUnitDropZone] Detached — expansion slot restored.");

////            // FIX 3 — Clear the reference after restoring so it is not stale
////            // if a different slot links to this zone in the future.
////            LinkedExpansionSlot = null;
////        }
////    }

////    /// <summary>
////    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
////    /// snaps back here. Restores zone state exactly as before the drag started.
////    /// </summary>
////    public void ReattachUnit(CastleUnitDraggable unit)
////    {
////        if (unit == null) return;

////        _placedInstance = unit.gameObject;
////        HasUnit = true;
////        PlacedVariantId = unit.variantId;

////        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
////        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;
////        _soldierImage?.SetActive(true);

////        // Hide the expansion slot again — cannon is back on this block.
////        // Re-link it so the next DetachUnit can restore it correctly.
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(false);
////            Debug.Log($"[CastleUnitDropZone] Reattached — expansion slot hidden again.");
////        }
////    }

////    /// <summary>
////    /// Called by ExpansionSlot.OnDrop to restore the link after a successful
////    /// snap-back so the slot can be revealed again on the next detach.
////    /// </summary>
////    public void RestoreLinkedExpansionSlot(ExpansionSlot slot)
////    {
////        LinkedExpansionSlot = slot;
////    }

////    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
////    public void RemoveUnit()
////    {
////        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

////        HasUnit = false;
////        PlacedVariantId = -1;

////        _soldierImage?.SetActive(false);
////        _emptyVisual?.SetActive(false);
////        _highlight?.SetActive(false);
////        _bg.color = normalColor;

////        // Restore the expansion slot — the block is now cannon-free.
////        if (LinkedExpansionSlot != null)
////        {
////            LinkedExpansionSlot.gameObject.SetActive(true);
////            LinkedExpansionSlot = null;
////            Debug.Log($"[CastleUnitDropZone] Removed — expansion slot restored.");
////        }
////    }

////    /// <summary>
////    /// Moves the placed cannon into <paramref name="dest"/> using the proper API.
////    /// Called by GridCell.TransferUnitSlotTo during block expansion migration.
////    ///
////    /// Unlike DetachUnit, this does NOT restore LinkedExpansionSlot — the block
////    /// being covered is gone; its expansion slot above it is also gone.
////    /// The cannon is seated in the destination via PlaceUnit so all state
////    /// (HasUnit, PlacedVariantId, _soldierImage, CanvasGroup) is set correctly.
////    /// </summary>
////    public void MigrateUnitTo(CastleUnitDropZone dest)
////    {
////        if (!HasUnit || _placedInstance == null || dest == null) return;

////        CastleUnitDraggable draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
////        if (draggable == null) return;

////        // Clear source state — do NOT touch LinkedExpansionSlot here.
////        // The block below is being covered so its expansion slot is being
////        // destroyed by RefreshExpansionSlots anyway.
////        _placedInstance = null;
////        HasUnit = false;
////        PlacedVariantId = -1;
////        _soldierImage?.SetActive(false);

////        // Seat properly in the destination so HasUnit, soldier image,
////        // CanvasGroup, and PlacedVariantId are all correct.
////        dest.PlaceUnit(draggable);
////    }

////    // ── Hover ─────────────────────────────────────────────────────

////    public void OnPointerEnter(PointerEventData eventData)
////    {
////        var unit = CastleUnitDraggable.CurrentlyDragging;
////        if (unit == null) return;

////        bool valid = unit.unitType == acceptedType && !HasUnit;
////        _highlight?.SetActive(true);
////        _emptyVisual?.SetActive(valid);
////        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
////    }

////    public void OnPointerExit(PointerEventData eventData)
////    {
////        _highlight?.SetActive(false);
////        _emptyVisual?.SetActive(false);
////        _bg.color = normalColor;
////    }
////}
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

///// <summary>
///// Drop zone for a Cannon unit on an exposed castle block.
/////
///// When a cannon is placed via an ExpansionSlot, that slot registers itself
///// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
///// hides / shows that slot as the cannon moves:
/////
/////   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
/////                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
/////   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
/////   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
/////   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
/////
///// Child hierarchy (auto-wired by name in Awake):
/////   CastleUnitDropZone  ← this script + Image (transparent raycast target)
/////   ├── EmptyVisual     hint shown only during a valid drag hover
/////   ├── Highlight       glow frame shown during any drag hover
/////   └── Soldier         Image — hidden by default, shown when cannon is placed
/////
///// ── Village / Castle mode ────────────────────────────────────────────────
///// Call SetVillageMode(true)  when the grid moves into the Village Panel.
/////   → The zone background becomes fully transparent (alpha = 0).
/////   → Raycasts remain ON — the player can still drag a cannon from the shop
/////     and drop it onto a block while in the village view.
///// Call SetVillageMode(false) when the grid moves into the Castle Panel.
/////   → The background alpha is restored to normalColor.
///// </summary>
//[RequireComponent(typeof(Image))]
//public class CastleUnitDropZone : MonoBehaviour,
//    IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────
//    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
//    public CastleUnitType acceptedType;

//    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
//    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

//    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
//    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
//    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

//    // ── Auto-wired ────────────────────────────────────────────────
//    private Image _bg;
//    private GameObject _emptyVisual;
//    private GameObject _highlight;
//    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

//    // ── State ─────────────────────────────────────────────────────
//    public bool HasUnit { get; private set; }
//    public int PlacedVariantId { get; private set; } = -1;

//    /// <summary>
//    /// Whether we are currently in village mode (background invisible).
//    /// Stored so PlaceUnit / RemoveUnit / DetachUnit can restore the right color.
//    /// </summary>
//    private bool _isVillageMode = false;

//    /// <summary>
//    /// The ExpansionSlot that was used to place the cannon here.
//    /// Set by ExpansionSlot.OnDrop, or pre-linked by ExpansionSlot.Init.
//    /// Null when no expansion slot is associated with this zone.
//    /// </summary>
//    public ExpansionSlot LinkedExpansionSlot { get; set; }

//    private GameObject _placedInstance;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        _bg = GetComponent<Image>();
//        _bg.color = normalColor;
//        _bg.raycastTarget = true;   // always on — needed for hover AND drops in both panels

//        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
//        _highlight = transform.Find("Highlight")?.gameObject;
//        _soldierImage = transform.Find("Soldier")?.gameObject;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _soldierImage?.SetActive(false);

//        // Soldier is a pure visual overlay — must NOT be a raycast target,
//        // otherwise it blocks pointer events from reaching the placed cannon.
//        if (_soldierImage != null)
//        {
//            Image soldierImg = _soldierImage.GetComponent<Image>();
//            if (soldierImg != null) soldierImg.raycastTarget = false;
//        }
//    }

//    // ── Village / Castle mode ──────────────────────────────────────

//    /// <summary>
//    /// Village mode  (isVillage = true)  → zone background alpha = 0.
//    ///   The slot is invisible but raycasts stay ON so cannons can be
//    ///   dragged from the shop and dropped onto the block.
//    ///
//    /// Castle mode   (isVillage = false) → zone background restored to normalColor.
//    /// </summary>
//    public void SetVillageMode(bool isVillage)
//    {
//        _isVillageMode = isVillage;

//        // Only the background color changes; EmptyVisual / Highlight are
//        // hover-driven and manage themselves. The placed cannon and soldier
//        // are children with independent rendering and are unaffected.
//        _bg.color = isVillage ? new Color(0f, 0f, 0f, 0f) : normalColor;
//    }

//    // ── Standard drag-drop path ────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;

//        if (unit == null)
//        {
//            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
//            return;
//        }
//        if (unit.unitType != acceptedType)
//        {
//            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
//                      $"zone accepts {acceptedType}.");
//            return;
//        }
//        if (HasUnit)
//        {
//            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
//            return;
//        }

//        PlaceUnit(unit);
//        CastleUnitDraggable.NotifyDropSucceeded();
//    }

//    // ── Public API ────────────────────────────────────────────────

//    /// <summary>
//    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
//    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
//    /// </summary>
//    public void PlaceUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null || HasUnit) return;

//        unit.transform.SetParent(transform, worldPositionStays: false);

//        RectTransform rt = unit.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            if (unit.stretchToFillSlot)
//            {
//                rt.anchorMin = Vector2.zero;
//                rt.anchorMax = Vector2.one;
//                rt.offsetMin = Vector2.zero;
//                rt.offsetMax = Vector2.zero;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            else
//            {
//                rt.anchorMin = new Vector2(0.5f, 0.5f);
//                rt.anchorMax = new Vector2(0.5f, 0.5f);
//                rt.pivot = new Vector2(0.5f, 0.5f);
//                rt.sizeDelta = centeredUnitSize;
//                rt.anchoredPosition = Vector2.zero;
//            }
//            rt.SetAsLastSibling();
//        }

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        // Re-assert raycastTarget so the cannon can be dragged again after placement.
//        Image unitImg = unit.GetComponent<Image>();
//        if (unitImg != null) unitImg.raycastTarget = true;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);

//        // Restore the correct background color for the current panel mode.
//        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

//        _soldierImage?.SetActive(true);

//        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
//                  $"placed on '{transform.parent?.name}' (villageMode={_isVillageMode}).");
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
//    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
//    /// Does NOT destroy the cannon — it is still alive being dragged.
//    /// </summary>
//    public void DetachUnit()
//    {
//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            Debug.Log("[CastleUnitDropZone] Detached — expansion slot restored.");
//            LinkedExpansionSlot = null;
//        }
//    }

//    /// <summary>
//    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
//    /// snaps back here. Restores zone state exactly as before the drag started.
//    /// </summary>
//    public void ReattachUnit(CastleUnitDraggable unit)
//    {
//        if (unit == null) return;

//        _placedInstance = unit.gameObject;
//        HasUnit = true;
//        PlacedVariantId = unit.variantId;

//        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;
//        _soldierImage?.SetActive(true);

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(false);
//            Debug.Log("[CastleUnitDropZone] Reattached — expansion slot hidden again.");
//        }
//    }

//    /// <summary>
//    /// Called by ExpansionSlot.OnDrop to restore the link after a successful
//    /// snap-back so the slot can be revealed again on the next detach.
//    /// </summary>
//    public void RestoreLinkedExpansionSlot(ExpansionSlot slot)
//    {
//        LinkedExpansionSlot = slot;
//    }

//    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
//    public void RemoveUnit()
//    {
//        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

//        HasUnit = false;
//        PlacedVariantId = -1;

//        _soldierImage?.SetActive(false);
//        _emptyVisual?.SetActive(false);
//        _highlight?.SetActive(false);
//        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

//        if (LinkedExpansionSlot != null)
//        {
//            LinkedExpansionSlot.gameObject.SetActive(true);
//            LinkedExpansionSlot = null;
//            Debug.Log("[CastleUnitDropZone] Removed — expansion slot restored.");
//        }
//    }

//    /// <summary>
//    /// Moves the placed cannon into <paramref name="dest"/> using the proper API.
//    /// Called by GridCell.TransferUnitSlotTo during block expansion migration.
//    /// </summary>
//    public void MigrateUnitTo(CastleUnitDropZone dest)
//    {
//        if (!HasUnit || _placedInstance == null || dest == null) return;

//        CastleUnitDraggable draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
//        if (draggable == null) return;

//        _placedInstance = null;
//        HasUnit = false;
//        PlacedVariantId = -1;
//        _soldierImage?.SetActive(false);

//        dest.PlaceUnit(draggable);
//    }

//    // ── Hover ─────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        var unit = CastleUnitDraggable.CurrentlyDragging;
//        if (unit == null) return;

//        bool valid = unit.unitType == acceptedType && !HasUnit;
//        _highlight?.SetActive(true);
//        _emptyVisual?.SetActive(valid);

//        // Show hover color even in village mode so the player gets visual feedback
//        // that the drop is valid — the zone becomes briefly visible on hover only.
//        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        _highlight?.SetActive(false);
//        _emptyVisual?.SetActive(false);

//        // Return to the correct resting color for the current panel mode.
//        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Drop zone for a Cannon unit on an exposed castle block.
///
/// When a cannon is placed via an ExpansionSlot, that slot registers itself
/// via <see cref="LinkedExpansionSlot"/>. The zone then automatically
/// hides / shows that slot as the cannon moves:
///
///   PlaceUnit    → cannon arrives; soldier shown. Slot hiding is done by
///                  ExpansionSlot.OnDrop BEFORE calling PlaceUnit.
///   DetachUnit   → cannon dragged away; soldier hidden; linked slot shown.
///   ReattachUnit → failed drag, cannon snaps back; soldier shown; linked slot hidden.
///   RemoveUnit   → cannon destroyed externally; soldier hidden; linked slot shown.
///
/// Child hierarchy (auto-wired by name in Awake):
///   CastleUnitDropZone  ← this script + Image (transparent raycast target)
///   ├── EmptyVisual     hint shown only during a valid drag hover
///   ├── Highlight       glow frame shown during any drag hover
///   └── Soldier         Image — hidden by default, shown when cannon is placed
///
/// ── Village / Castle mode ────────────────────────────────────────────────
/// Call SetVillageMode(true)  when the grid moves into the Village Panel.
///   → The zone background becomes fully transparent (alpha = 0).
///   → Raycasts remain ON — the player can still drag a cannon from the shop
///     and drop it onto a block while in the village view.
/// Call SetVillageMode(false) when the grid moves into the Castle Panel.
///   → The background alpha is restored to normalColor.
/// </summary>
[RequireComponent(typeof(Image))]
public class CastleUnitDropZone : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── Inspector ─────────────────────────────────────────────────
    [Tooltip("Set automatically by CastleBlockUnitSlot.Awake(). Inspector value is overwritten.")]
    public CastleUnitType acceptedType;

    [Tooltip("Pixel size used when placing a unit that has stretchToFillSlot = false.")]
    public Vector2 centeredUnitSize = new Vector2(56f, 56f);

    public Color normalColor = new Color(1f, 1f, 1f, 0.01f);
    public Color hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.55f);
    public Color hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.35f);

    // ── Auto-wired ────────────────────────────────────────────────
    private Image _bg;
    private GameObject _emptyVisual;
    private GameObject _highlight;
    private GameObject _soldierImage;   // child "Soldier" — shown alongside the cannon

    // ── State ─────────────────────────────────────────────────────
    public bool HasUnit { get; private set; }
    public int PlacedVariantId { get; private set; } = -1;

    /// <summary>
    /// Whether we are currently in village mode (background invisible).
    /// Stored so PlaceUnit / RemoveUnit / DetachUnit can restore the right color.
    /// </summary>
    private bool _isVillageMode = false;

    /// <summary>
    /// The ExpansionSlot that was used to place the cannon here.
    /// Set by ExpansionSlot.OnDrop, or pre-linked by ExpansionSlot.Init.
    /// Null when no expansion slot is associated with this zone.
    /// </summary>
    public ExpansionSlot LinkedExpansionSlot { get; set; }

    private GameObject _placedInstance;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _bg = GetComponent<Image>();
        _bg.color = normalColor;
        _bg.raycastTarget = true;   // always on — needed for hover AND drops in both panels

        _emptyVisual = transform.Find("EmptyVisual")?.gameObject;
        _highlight = transform.Find("Highlight")?.gameObject;
        _soldierImage = transform.Find("Soldier")?.gameObject;

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _soldierImage?.SetActive(false);

        // Soldier is a pure visual overlay — must NOT be a raycast target,
        // otherwise it blocks pointer events from reaching the placed cannon.
        if (_soldierImage != null)
        {
            Image soldierImg = _soldierImage.GetComponent<Image>();
            if (soldierImg != null) soldierImg.raycastTarget = false;
        }
    }

    // ── Village / Castle mode ──────────────────────────────────────

    /// <summary>
    /// Village mode  (isVillage = true)  → zone background alpha = 0.
    ///   The slot is invisible but raycasts stay ON so cannons can be
    ///   dragged from the shop and dropped onto the block.
    ///
    /// Castle mode   (isVillage = false) → zone background restored to normalColor.
    /// </summary>
    public void SetVillageMode(bool isVillage)
    {
        _isVillageMode = isVillage;

        // Only the background color changes; EmptyVisual / Highlight are
        // hover-driven and manage themselves. The placed cannon and soldier
        // are children with independent rendering and are unaffected.
        _bg.color = isVillage ? new Color(0f, 0f, 0f, 0f) : normalColor;
    }

    // ── Standard drag-drop path ────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;

        if (unit == null)
        {
            Debug.Log("[CastleUnitDropZone] OnDrop — nothing is being dragged.");
            return;
        }
        if (unit.unitType != acceptedType)
        {
            Debug.Log($"[CastleUnitDropZone] Type mismatch — dragging {unit.unitType}, " +
                      $"zone accepts {acceptedType}.");
            return;
        }
        if (HasUnit)
        {
            Debug.Log($"[CastleUnitDropZone] Zone ({acceptedType}) already occupied.");
            return;
        }

        PlaceUnit(unit);
        CastleUnitDraggable.NotifyDropSucceeded();
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Seats <paramref name="unit"/> in this zone and shows the soldier image.
    /// Also called by ExpansionSlot after it hides itself and sets LinkedExpansionSlot.
    /// </summary>
    public void PlaceUnit(CastleUnitDraggable unit)
    {
        if (unit == null || HasUnit) return;

        unit.transform.SetParent(transform, worldPositionStays: false);

        RectTransform rt = unit.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (unit.stretchToFillSlot)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = centeredUnitSize;
                rt.anchoredPosition = Vector2.zero;
            }
            rt.SetAsLastSibling();
        }

        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

        // Re-assert raycastTarget so the cannon can be dragged again after placement.
        Image unitImg = unit.GetComponent<Image>();
        if (unitImg != null) unitImg.raycastTarget = true;

        _placedInstance = unit.gameObject;
        HasUnit = true;
        PlacedVariantId = unit.variantId;

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);

        // Restore the correct background color for the current panel mode.
        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

        _soldierImage?.SetActive(true);

        Debug.Log($"[CastleUnitDropZone] {acceptedType} (variant {PlacedVariantId}) " +
                  $"placed on '{transform.parent?.name}' (villageMode={_isVillageMode}).");
    }

    /// <summary>
    /// Called by CastleUnitDraggable.OnBeginDrag when the placed cannon is picked up.
    /// Frees this zone and shows the linked expansion slot so it can be dropped on again.
    /// Does NOT destroy the cannon — it is still alive being dragged.
    /// </summary>
    public void DetachUnit()
    {
        _placedInstance = null;
        HasUnit = false;
        PlacedVariantId = -1;

        // Soldier stays visible — it remains on the block even without the cannon.
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            Debug.Log("[CastleUnitDropZone] Detached — expansion slot restored.");
            LinkedExpansionSlot = null;
        }
    }

    /// <summary>
    /// Called by CastleUnitDraggable.OnEndDrag when the drag failed and the cannon
    /// snaps back here. Restores zone state exactly as before the drag started.
    /// </summary>
    public void ReattachUnit(CastleUnitDraggable unit)
    {
        if (unit == null) return;

        _placedInstance = unit.gameObject;
        HasUnit = true;
        PlacedVariantId = unit.variantId;

        CanvasGroup cg = unit.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.alpha = 1f; }

        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;
        _soldierImage?.SetActive(true);

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(false);
            Debug.Log("[CastleUnitDropZone] Reattached — expansion slot hidden again.");
        }
    }

    /// <summary>
    /// Called by ExpansionSlot.OnDrop to restore the link after a successful
    /// snap-back so the slot can be revealed again on the next detach.
    /// </summary>
    public void RestoreLinkedExpansionSlot(ExpansionSlot slot)
    {
        LinkedExpansionSlot = slot;
    }

    /// <summary>Destroys the placed cannon, hides the soldier, and restores the expansion slot.</summary>
    public void RemoveUnit()
    {
        if (_placedInstance != null) { Destroy(_placedInstance); _placedInstance = null; }

        HasUnit = false;
        PlacedVariantId = -1;

        // Soldier stays visible — removing the cannon does not remove the soldier.
        _emptyVisual?.SetActive(false);
        _highlight?.SetActive(false);
        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;

        if (LinkedExpansionSlot != null)
        {
            LinkedExpansionSlot.gameObject.SetActive(true);
            LinkedExpansionSlot = null;
            Debug.Log("[CastleUnitDropZone] Removed — expansion slot restored.");
        }
    }

    /// <summary>
    /// Moves the placed cannon into <paramref name="dest"/> using the proper API.
    /// Called by GridCell.TransferUnitSlotTo during block expansion migration.
    /// </summary>
    public void MigrateUnitTo(CastleUnitDropZone dest)
    {
        if (!HasUnit || _placedInstance == null || dest == null) return;

        CastleUnitDraggable draggable = _placedInstance.GetComponent<CastleUnitDraggable>();
        if (draggable == null) return;

        _placedInstance = null;
        HasUnit = false;
        PlacedVariantId = -1;
        _soldierImage?.SetActive(false);

        dest.PlaceUnit(draggable);
    }

    // ── Hover ─────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        var unit = CastleUnitDraggable.CurrentlyDragging;
        if (unit == null) return;

        bool valid = unit.unitType == acceptedType && !HasUnit;
        _highlight?.SetActive(true);
        _emptyVisual?.SetActive(valid);

        // Show hover color even in village mode so the player gets visual feedback
        // that the drop is valid — the zone becomes briefly visible on hover only.
        _bg.color = valid ? hoverValidColor : hoverInvalidColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlight?.SetActive(false);
        _emptyVisual?.SetActive(false);

        // Return to the correct resting color for the current panel mode.
        _bg.color = _isVillageMode ? new Color(0f, 0f, 0f, 0f) : normalColor;
    }
}