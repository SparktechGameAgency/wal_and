using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// DRAGON CONTROLLER — Single-Prefab Rider System
///
/// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
/// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///
/// ════════════════════════════════════════════════════════════════════
///  SINGLE PREFAB — NO PREFAB SWAP
/// ════════════════════════════════════════════════════════════════════
///
///  One dragon prefab covers both the plain and rider states:
///
///   Plain state  The dragon patrols / sits idle normally.
///                The DragonRiderVisual child is invisible.
///
///   Rider state  A soldier has been dropped on the dragon.
///                The soldier's own visuals are hidden (alpha 0).
///                The dragon's DragonRiderVisual child is shown with
///                that soldier's armor / helmet / weapon sprites.
///
///  No GameObject is ever destroyed or spawned on mount / dismount.
///
/// ════════════════════════════════════════════════════════════════════
///  STATES
/// ════════════════════════════════════════════════════════════════════
///
///  Idle      Dragon sits inside DragonArea playing its idle animation.
///            It can be picked up and dragged (unless a soldier is riding).
///
///  Dragging  Dragon follows the pointer at canvas-root level,
///            semi-transparent, raycasts pass through it.
///
///  Flying    Dragon was dropped on a FlyZone.
///            It patrols left right, flipping sprite at each edge.
///
/// ════════════════════════════════════════════════════════════════════
///  DROP RULES (dragon drag)
/// ════════════════════════════════════════════════════════════════════
///
///  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
///  Drop on DragonArea → EnterIdle     (reparented to slot)
///  Drop anywhere else → SnapBack      (return to previous state)
///
///  Dragon dragging is BLOCKED while a soldier is riding it.
///
/// ════════════════════════════════════════════════════════════════════
///  PREFAB HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
///   DragonBody [0]             Image: dragon body sprite
///   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
///     DragonRiderVisual        DragonRiderVisual (hidden by default)
///       BodyLayer              Image
///       FaceLayer              Image
///       HairLayer              Image
///       HelmetLayer            Image
///       WeaponLayer            Image
///   DragonWing [2]             Image: front wing (renders on top of rider)
///                              + DragonWingAnimator
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP
/// ════════════════════════════════════════════════════════════════════
///
///  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
///  2. Assign DragonData in the Inspector.
///  3. Add DragonRiderSeat to the RiderSeat child.
///  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
///  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
///  6. In DragonEggSlot.EnterHatched(), after spawning:
///         var dc = _spawnedDragon.GetComponent<DragonController>();
///         if (dc != null) dc.homeSlot = this;
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DragonController : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Dragon Data")]
    [SerializeField] private DragonData dragonData;

    [Header("Canvas — auto-found if blank")]
    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Sprite Orientation")]
    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
             "The controller flips the scale to match patrol direction.")]
    [SerializeField] private bool spriteDefaultFacesLeft = true;

    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

    /// <summary>The DragonArea slot this dragon hatched from.</summary>
    [HideInInspector] public DragonEggSlot homeSlot;

    // ── Private components ────────────────────────────────────────────────────

    private RectTransform _rt;
    private Animator _anim;
    private CanvasGroup _cg;

    // Found in children — all live permanently in the hierarchy.
    private DragonRiderVisual _riderVisual;
    private DragonRiderSeat _riderSeat;
    private DragonWingAnimator _wingAnimator;
    private DragonBodyAnimator _bodyAnimator;

    // ── Drag state ────────────────────────────────────────────────────────────

    private Transform _savedParent;
    private Vector2 _savedAnchoredPos;
    private int _savedSiblingIndex;
    private Vector2 _dragOffset;

    // ── Patrol state ──────────────────────────────────────────────────────────

    private FlyZone _currentZone;
    private float _patrolDir = 1f;   // +1 = right, -1 = left

    // ── Dragon state ──────────────────────────────────────────────────────────

    public enum DragonState { Idle, Dragging, Flying }
    public DragonState State { get; private set; } = DragonState.Idle;

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _anim = GetComponent<Animator>();
        _cg = GetComponent<CanvasGroup>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

        if (_riderVisual == null)
            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
                             "Add DragonRiderVisual to a child of RiderSeat.", this);
        if (_riderSeat == null)
            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
                             "Add DragonRiderSeat to the RiderSeat child.", this);
        if (_wingAnimator == null)
            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
                             "Add DragonWingAnimator to the DragonWing child.", this);
        if (_bodyAnimator == null)
            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
                             "Add DragonBodyAnimator to the DragonBody child.", this);
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        if (State == DragonState.Flying)
            DoPatrol();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — BEGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ── Rider lock check ──────────────────────────────────────────────────
        // If a rider is mounted but NOT yet locked (Attached), block the dragon
        // drag. The player must click Attach first before moving the dragon.
        //
        // If the rider IS locked: their CanvasGroup has blocksRaycasts=false so
        // clicks pass through to the dragon — this handler fires — allow drag.
        //
        // If there is no rider → allow drag normally.
        if (_riderSeat != null && _riderSeat.IsOccupied)
        {
            var rider = _riderSeat.MountedSoldier;
            if (rider == null || !rider.IsLocked)
            {
                Debug.Log("[DragonController] Drag blocked — rider is not Attached. " +
                          "Click Attach to lock the rider before moving the dragon.");
                return;
            }
            // Rider IS locked — drag the whole dragon+rider unit.
        }

        _savedParent = _rt.parent;
        _savedAnchoredPos = _rt.anchoredPosition;
        _savedSiblingIndex = _rt.GetSiblingIndex();

        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position, uiCam,
            out Vector2 pointerCanvasPos);
        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

        _cg.alpha = 0.75f;
        _cg.blocksRaycasts = false;

        State = DragonState.Dragging;
        Debug.Log("[DragonController] OnBeginDrag");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — MOVE
    // ══════════════════════════════════════════════════════════════════════════

    public void OnDrag(PointerEventData eventData)
    {
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position, uiCam,
            out Vector2 localPos);

        _rt.anchoredPosition = localPos + _dragOffset;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — END
    // ══════════════════════════════════════════════════════════════════════════

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
        _cg.alpha = 1f;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        _cg.blocksRaycasts = true;

        FlyZone hitFlyZone = null;
        DragonEggSlot hitAreaSlot = null;

        foreach (var r in results)
        {
            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
            if (hitFlyZone != null && hitAreaSlot != null) break;
        }

        if (hitFlyZone != null)
        {
            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = hitFlyZone;
            EnterFlying();
        }
        else if (hitAreaSlot != null)
        {
            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = null;
            EnterIdle();
        }
        else
        {
            SnapBack();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — IDLE
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterIdle()
    {
        State = DragonState.Idle;

        Vector3 s = transform.localScale;
        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;

        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
        TriggerAnim(dragonData?.dragonIdleTrigger);
        Debug.Log("[DragonController] -> Idle");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — FLYING + PATROL
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterFlying()
    {
        State = DragonState.Flying;
        _patrolDir = -1f;

        Vector3 s = transform.localScale;
        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;

        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
        TriggerAnim(dragonData?.dragonFlyTrigger);
        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
    }

    private void DoPatrol()
    {
        if (_currentZone == null) return;

        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
        float halfWidth = _currentZone.PatrolHalfWidth;
        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

        if (newX >= halfWidth)
        {
            newX = halfWidth;
            _patrolDir = -1f;
            FlipHorizontal();
        }
        else if (newX <= -halfWidth)
        {
            newX = -halfWidth;
            _patrolDir = 1f;
            FlipHorizontal();
        }

        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MOUNT — called by SoldierDragDrop.OnEndDrag
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
    ///
    /// What happens:
    ///   1. seat.MountSoldier(soldier) is called.
    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
    ///            and reparents them under the seat.
    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
    ///      CharacterEquipment and displays the matching armor / helmet sprites
    ///      on the dragon's built-in rider layers.
    ///
    /// No prefabs are spawned or destroyed.
    /// </summary>
    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
    {
        if (seat == null)
        {
            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
                             "Make sure the prefab has a DragonRiderSeat child.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log("[DragonController] PerformMount: seat already occupied.");
            return;
        }

        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
        seat.MountSoldier(soldier);

        // Step 2 — show the dragon's rider visual with the soldier's equipment.
        if (_riderVisual != null)
        {
            var equipment = soldier.GetComponent<CharacterEquipment>();
            _riderVisual.ShowForSoldier(equipment);
        }
        else
        {
            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
        }

        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DISMOUNT — called by SoldierDragDrop
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
    /// away from the seat (to the canvas root or back to their ground home).
    ///
    /// Hides the rider visual. The dragon continues its current state (Idle or
    /// Flying) without any prefab swap.
    /// </summary>
    public void PerformDismount()
    {
        _riderVisual?.Hide();
        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void FlipHorizontal()
    {
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    private void ReturnToHome()
    {
        if (_savedParent == null) return;
        _rt.SetParent(_savedParent, worldPositionStays: false);
        _rt.SetSiblingIndex(_savedSiblingIndex);
        _rt.anchoredPosition = _savedAnchoredPos;
    }

    private void SnapBack()
    {
        ReturnToHome();

        if (_currentZone != null)
        {
            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
            EnterFlying();  // ← FIXED: was `State = DragonState.Flying` (skipped wing animator)
            Debug.Log("[DragonController] SnapBack -> resume Flying");
        }
        else
        {
            EnterIdle();
            Debug.Log("[DragonController] SnapBack -> resume Idle");
        }
    }

    private void TriggerAnim(string trigger)
    {
        if (_anim == null)
        {
            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
            return;
        }
        if (dragonData == null)
        {
            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
                             "Drag it into the DragonController Inspector field.", this);
            return;
        }
        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
            return;
        }

        _anim.SetTrigger(trigger);
        Debug.Log($"[DragonController] SetTrigger({trigger})");
    }
}