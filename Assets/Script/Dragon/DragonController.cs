//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

//////[RequireComponent(typeof(RectTransform))]
//////[RequireComponent(typeof(CanvasGroup))]
//////public class DragonController : MonoBehaviour,
//////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////{
//////    // ── Inspector ──────────────────────────────────────────────────────────────

//////    [Header("Dragon Data")]
//////    [SerializeField] private DragonData dragonData;

//////    [Header("Canvas — auto-found if blank")]
//////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////    [SerializeField] private Canvas rootCanvas;

//////    [Header("Sprite Orientation")]
//////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//////             "The controller flips the scale to match patrol direction.")]
//////    [SerializeField] private bool spriteDefaultFacesLeft = true;

//////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

//////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////    [HideInInspector] public DragonEggSlot homeSlot;

//////    // ── Private components ────────────────────────────────────────────────────

//////    private RectTransform _rt;
//////    private Animator _anim;
//////    private CanvasGroup _cg;

//////    // Found in children — all live permanently in the hierarchy.
//////    private DragonRiderVisual _riderVisual;
//////    private DragonRiderSeat _riderSeat;
//////    private DragonWingAnimator _wingAnimator;
//////    private DragonBodyAnimator _bodyAnimator;

//////    // ── Drag state ────────────────────────────────────────────────────────────

//////    private Transform _savedParent;
//////    private Vector2 _savedAnchoredPos;
//////    private int _savedSiblingIndex;
//////    private Vector2 _dragOffset;

//////    // ── Patrol state ──────────────────────────────────────────────────────────

//////    private FlyZone _currentZone;
//////    private float _patrolDir = 1f;   // +1 = right, -1 = left

//////    // ── Dragon state ──────────────────────────────────────────────────────────

//////    public enum DragonState { Idle, Dragging, Flying }
//////    public DragonState State { get; private set; } = DragonState.Idle;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        _rt = GetComponent<RectTransform>();
//////        _anim = GetComponent<Animator>();
//////        _cg = GetComponent<CanvasGroup>();

//////        if (rootCanvas == null)
//////            rootCanvas = GetComponentInParent<Canvas>();

//////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
//////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
//////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
//////        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

//////        if (_riderVisual == null)
//////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
//////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
//////        if (_riderSeat == null)
//////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
//////                             "Add DragonRiderSeat to the RiderSeat child.", this);
//////        if (_wingAnimator == null)
//////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
//////                             "Add DragonWingAnimator to the DragonWing child.", this);
//////        if (_bodyAnimator == null)
//////            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
//////                             "Add DragonBodyAnimator to the DragonBody child.", this);
//////    }

//////    private void Start()
//////    {
//////        EnterIdle();
//////    }

//////    private void Update()
//////    {
//////        if (State == DragonState.Flying)
//////            DoPatrol();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — BEGIN
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnBeginDrag(PointerEventData eventData)
//////    {
//////        // ── Rider lock check ──────────────────────────────────────────────────
//////        // If a rider is mounted but NOT yet locked (Attached), block the dragon
//////        // drag. The player must click Attach first before moving the dragon.
//////        //
//////        // If the rider IS locked: their CanvasGroup has blocksRaycasts=false so
//////        // clicks pass through to the dragon — this handler fires — allow drag.
//////        //
//////        // If there is no rider → allow drag normally.
//////        if (_riderSeat != null && _riderSeat.IsOccupied)
//////        {
//////            var rider = _riderSeat.MountedSoldier;
//////            if (rider == null || !rider.IsLocked)
//////            {
//////                Debug.Log("[DragonController] Drag blocked — rider is not Attached. " +
//////                          "Click Attach to lock the rider before moving the dragon.");
//////                return;
//////            }
//////            // Rider IS locked — drag the whole dragon+rider unit.
//////        }

//////        _savedParent = _rt.parent;
//////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////        // Switch to Dragging BEFORE SetParent so Update()/DoPatrol() stops
//////        // moving the dragon the moment we lift it. This prevents the saved
//////        // position from drifting between pointer-down and OnBeginDrag.
//////        State = DragonState.Dragging;

//////        // Capture anchored position BEFORE reparenting — this is the true
//////        // resting position relative to the current parent, used for snap-back.
//////        _savedAnchoredPos = _rt.anchoredPosition;

//////        // Lift to canvas root with worldPositionStays so the dragon doesn't
//////        // jump visually during reparenting.
//////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////        _rt.SetAsLastSibling();

//////        // FIX: Use delta-based dragging (same as SoldierDragDrop) instead of
//////        // computing an absolute offset. Delta accumulation is immune to pivot/
//////        // anchor mismatches between the FlyZone, DragonArea, and Canvas root,
//////        // so the dragon stays exactly under the finger from the very first frame.
//////        // _dragOffset is no longer used.

//////        _cg.alpha = 0.75f;
//////        _cg.blocksRaycasts = false;

//////        Debug.Log("[DragonController] OnBeginDrag");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — MOVE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        // Delta-based: add the pointer's screen-space delta, adjusted for the
//////        // canvas scale. This never needs an offset calculation and therefore
//////        // never jumps — regardless of where in the hierarchy the dragon started.
//////        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — END
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnEndDrag(PointerEventData eventData)
//////    {
//////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
//////        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
//////        _cg.alpha = 1f;

//////        var results = new List<RaycastResult>();
//////        EventSystem.current.RaycastAll(eventData, results);

//////        _cg.blocksRaycasts = true;

//////        FlyZone hitFlyZone = null;
//////        DragonEggSlot hitAreaSlot = null;

//////        foreach (var r in results)
//////        {
//////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////        }

//////        if (hitFlyZone != null)
//////        {
//////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////            _rt.anchoredPosition = Vector2.zero;
//////            _currentZone = hitFlyZone;
//////            EnterFlying();
//////        }
//////        else if (hitAreaSlot != null)
//////        {
//////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//////            _rt.anchoredPosition = Vector2.zero;
//////            _currentZone = null;
//////            EnterIdle();
//////        }
//////        else
//////        {
//////            SnapBack();
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — IDLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterIdle()
//////    {
//////        State = DragonState.Idle;

//////        Vector3 s = transform.localScale;
//////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////        transform.localScale = s;

//////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
//////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
//////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////        Debug.Log("[DragonController] -> Idle");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — FLYING + PATROL
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterFlying()
//////    {
//////        State = DragonState.Flying;
//////        _patrolDir = -1f;

//////        Vector3 s = transform.localScale;
//////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////        transform.localScale = s;

//////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
//////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
//////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
//////    }

//////    private void DoPatrol()
//////    {
//////        if (_currentZone == null) return;

//////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////        float halfWidth = _currentZone.PatrolHalfWidth;
//////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

//////        if (newX >= halfWidth)
//////        {
//////            newX = halfWidth;
//////            _patrolDir = -1f;
//////            FlipHorizontal();
//////        }
//////        else if (newX <= -halfWidth)
//////        {
//////            newX = -halfWidth;
//////            _patrolDir = 1f;
//////            FlipHorizontal();
//////        }

//////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // MOUNT — called by SoldierDragDrop.OnEndDrag
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
//////    ///
//////    /// What happens:
//////    ///   1. seat.MountSoldier(soldier) is called.
//////    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
//////    ///            and reparents them under the seat.
//////    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
//////    ///      CharacterEquipment and displays the matching armor / helmet sprites
//////    ///      on the dragon's built-in rider layers.
//////    ///
//////    /// No prefabs are spawned or destroyed.
//////    /// </summary>
//////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
//////    {
//////        if (seat == null)
//////        {
//////            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
//////                             "Make sure the prefab has a DragonRiderSeat child.", this);
//////            return;
//////        }

//////        if (seat.IsOccupied)
//////        {
//////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
//////            return;
//////        }

//////        // Step 1 — seat the soldier (reparents and sets animator state).
//////        seat.MountSoldier(soldier);

//////        // Step 2 — show the dragon's rider visual with the soldier's equipment.
//////        if (_riderVisual != null)
//////        {
//////            var equipment = soldier.GetComponent<CharacterEquipment>();
//////            _riderVisual.ShowForSoldier(equipment);
//////        }
//////        else
//////        {
//////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
//////                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
//////        }

//////        // Step 3 — deactivate the soldier GameObject so its own sprites are
//////        // completely hidden. SetActive(false) is used instead of alpha=0 because
//////        // the animator's SetState() calls re-enable Image components and would
//////        // override an alpha-based hide.
//////        soldier.gameObject.SetActive(false);

//////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DISMOUNT — called by SoldierDragDrop
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
//////    /// away from the seat (to the canvas root or back to their ground home).
//////    ///
//////    /// Hides the rider visual. The dragon continues its current state (Idle or
//////    /// Flying) without any prefab swap.
//////    /// </summary>
//////    public void PerformDismount()
//////    {
//////        _riderVisual?.Hide();
//////        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELPERS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void FlipHorizontal()
//////    {
//////        Vector3 s = transform.localScale;
//////        s.x = -s.x;
//////        transform.localScale = s;
//////    }

//////    private void ReturnToHome()
//////    {
//////        if (_savedParent == null) return;
//////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////        _rt.anchoredPosition = _savedAnchoredPos;
//////    }

//////    private void SnapBack()
//////    {
//////        ReturnToHome();

//////        if (_currentZone != null)
//////        {
//////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////            EnterFlying();  // ← FIXED: was `State = DragonState.Flying` (skipped wing animator)
//////            Debug.Log("[DragonController] SnapBack -> resume Flying");
//////        }
//////        else
//////        {
//////            EnterIdle();
//////            Debug.Log("[DragonController] SnapBack -> resume Idle");
//////        }
//////    }

//////    private void TriggerAnim(string trigger)
//////    {
//////        if (_anim == null)
//////        {
//////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
//////            return;
//////        }
//////        if (dragonData == null)
//////        {
//////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
//////                             "Drag it into the DragonController Inspector field.", this);
//////            return;
//////        }
//////        if (string.IsNullOrEmpty(trigger))
//////        {
//////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
//////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
//////            return;
//////        }

//////        _anim.SetTrigger(trigger);
//////        Debug.Log($"[DragonController] SetTrigger({trigger})");
//////    }
//////}


////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

////[RequireComponent(typeof(RectTransform))]
////[RequireComponent(typeof(CanvasGroup))]
////public class DragonController : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Dragon Data")]
////    [SerializeField] private DragonData dragonData;

////    [Header("Canvas — auto-found if blank")]
////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////    [SerializeField] private Canvas rootCanvas;

////    [Header("Sprite Orientation")]
////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
////             "The controller flips the scale to match patrol direction.")]
////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////    [Header("Combat — Fire Breath")]
////    [Tooltip("ParticleSystem on a child GameObject (positioned at the dragon's mouth). " +
////             "Played while the dragon is breathing fire, stopped otherwise.")]
////    [SerializeField] private DragonFireBreath fireBreath;

////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////    [HideInInspector] public DragonEggSlot homeSlot;

////    // ── Private components ────────────────────────────────────────────────────

////    private RectTransform _rt;
////    private Animator _anim;
////    private CanvasGroup _cg;

////    // Found in children — all live permanently in the hierarchy.
////    private DragonRiderVisual _riderVisual;
////    private DragonRiderSeat _riderSeat;
////    private DragonWingAnimator _wingAnimator;
////    private DragonBodyAnimator _bodyAnimator;

////    // ── Drag state ────────────────────────────────────────────────────────────

////    private Transform _savedParent;
////    private Vector2 _savedAnchoredPos;
////    private int _savedSiblingIndex;

////    // ── Patrol state ──────────────────────────────────────────────────────────

////    private FlyZone _currentZone;
////    private float _patrolDir = 1f;   // +1 = right, -1 = left
////    private float _patrolY;          // anchoredPosition.y to glide back to after a chase

////    // ── Combat state ───────────────────────────────────────────────────────────

////    private EnemyUnit _target;
////    private bool _isAttacking;
////    private float _damageTimer;

////    // ── Dragon state ──────────────────────────────────────────────────────────

////    public enum DragonState { Idle, Dragging, Flying }
////    public DragonState State { get; private set; } = DragonState.Idle;

////    // ══════════════════════════════════════════════════════════════════════════
////    // LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        _rt = GetComponent<RectTransform>();
////        _anim = GetComponent<Animator>();
////        _cg = GetComponent<CanvasGroup>();

////        if (rootCanvas == null)
////            rootCanvas = GetComponentInParent<Canvas>();

////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
////        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

////        if (_riderVisual == null)
////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
////        if (_riderSeat == null)
////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
////                             "Add DragonRiderSeat to the RiderSeat child.", this);
////        if (_wingAnimator == null)
////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
////                             "Add DragonWingAnimator to the DragonWing child.", this);
////        if (_bodyAnimator == null)
////            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
////                             "Add DragonBodyAnimator to the DragonBody child.", this);
////    }

////    private void Start()
////    {
////        EnterIdle();
////    }

////    private void Update()
////    {
////        if (State == DragonState.Flying)
////            DoCombatAI();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — BEGIN
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        // No lock check needed — the Detach button is the only way to dismount.
////        // The dragon can always be dragged freely (rider travels with it).

////        // Cancel any in-progress fire breath so the particle doesn't keep
////        // playing (or damage ticking) while the dragon is being carried.
////        StopAttacking();
////        _target = null;

////        _savedParent = _rt.parent;
////        _savedSiblingIndex = _rt.GetSiblingIndex();

////        // Set Dragging BEFORE SetParent so DoPatrol() stops immediately.
////        State = DragonState.Dragging;

////        // Capture position BEFORE reparenting — used for snap-back.
////        _savedAnchoredPos = _rt.anchoredPosition;

////        // ── Play idle animation while the dragon is carried ───────────────────
////        // Wings fold and body settles the moment the player picks it up,
////        // whether it was patrolling or sitting idle. This gives clear feedback
////        // that the dragon is "in hand" and ready to be placed.
////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
////        TriggerAnim(dragonData?.dragonIdleTrigger);

////        // Lift to canvas root so it renders over every other UI element.
////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////        _rt.SetAsLastSibling();

////        _cg.alpha = 0.75f;
////        _cg.blocksRaycasts = false;

////        Debug.Log("[DragonController] OnBeginDrag");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        // Delta-based movement — immune to pivot/anchor mismatches regardless
////        // of where in the hierarchy the dragon started (FlyZone, DragonArea…).
////        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — END
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        // Restore opacity but keep blocksRaycasts=false until AFTER the raycast
////        // so the dragon's CanvasGroup does not shadow the target beneath it.
////        _cg.alpha = 1f;

////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        _cg.blocksRaycasts = true;

////        FlyZone hitFlyZone = null;
////        DragonEggSlot hitAreaSlot = null;

////        foreach (var r in results)
////        {
////            // Skip any UI element that belongs to the slot the dragon came FROM.
////            // Without this, an idle dragon dragged toward the FlyZone would still
////            // have its own DragonArea underneath the pointer (the DragonArea panel
////            // is large), causing hitAreaSlot to win and snapping the dragon back.
////            if (_savedParent != null && r.gameObject.transform.IsChildOf(_savedParent))
////                continue;

////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////            if (hitFlyZone != null && hitAreaSlot != null) break;
////        }

////        // FlyZone is checked first when coming from idle (no _currentZone).
////        // DragonEggSlot wins when coming from a FlyZone (returning home).
////        // The _savedParent skip above already strips the home slot from results,
////        // so a flying dragon dropped back on its own DragonArea still works.
////        if (hitFlyZone != null && _currentZone == null)
////        {
////            // Idle dragon dropped onto FlyZone → start flying.
////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            _currentZone = hitFlyZone;
////            EnterFlying();
////        }
////        else if (hitAreaSlot != null)
////        {
////            // Flying dragon dropped onto DragonArea → return to idle.
////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            _currentZone = null;
////            EnterIdle();
////        }
////        else if (hitFlyZone != null)
////        {
////            // Flying dragon dropped onto a (different) FlyZone → keep flying.
////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            _currentZone = hitFlyZone;
////            EnterFlying();
////        }
////        else
////        {
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — IDLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterIdle()
////    {
////        State = DragonState.Idle;

////        Vector3 s = transform.localScale;
////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////        transform.localScale = s;

////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
////        TriggerAnim(dragonData?.dragonIdleTrigger);
////        Debug.Log("[DragonController] -> Idle");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — FLYING + PATROL
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterFlying()
////    {
////        State = DragonState.Flying;
////        _patrolDir = -1f;
////        _patrolY = _rt.anchoredPosition.y; // remember the patrol-line height before any chasing

////        Vector3 s = transform.localScale;
////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////        transform.localScale = s;

////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
////        TriggerAnim(dragonData?.dragonFlyTrigger);
////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
////    }

////    private void DoPatrol()
////    {
////        if (_currentZone == null) return;

////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////        float halfWidth = _currentZone.PatrolHalfWidth;
////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

////        if (newX >= halfWidth)
////        {
////            newX = halfWidth;
////            _patrolDir = -1f;
////        }
////        else if (newX <= -halfWidth)
////        {
////            newX = -halfWidth;
////            _patrolDir = 1f;
////        }

////        // Use FaceDirection (absolute) not FlipHorizontal (toggle) so facing
////        // is always driven by _patrolDir and never drifts out of sync after
////        // a chase/attack that used FaceDirection for its own facing.
////        FaceDirection(_patrolDir);

////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////    }

////    /// <summary>True if a chase has carried the dragon outside its FlyZone's patrol box
////    /// (in X) or off the patrol-line height (in Y).</summary>
////    private bool IsOutsidePatrolZone()
////    {
////        if (_currentZone == null) return false;

////        float halfWidth = _currentZone.PatrolHalfWidth;
////        Vector2 ap = _rt.anchoredPosition;
////        return Mathf.Abs(ap.x) > halfWidth || !Mathf.Approximately(ap.y, _patrolY);
////    }

////    /// <summary>
////    /// Smoothly flies back toward the patrol box/line after a chase, instead of
////    /// snapping instantly — fixes the "dragon jumps backward" pop that happened
////    /// when DoPatrol's clamp caught a position that was already outside bounds.
////    /// </summary>
////    private void ReturnToPatrolZone()
////    {
////        float halfWidth = _currentZone.PatrolHalfWidth;
////        float speed = dragonData != null ? dragonData.chaseSpeed : 160f;

////        Vector2 ap = _rt.anchoredPosition;
////        float targetX = Mathf.Clamp(ap.x, -halfWidth, halfWidth);
////        Vector2 target = new Vector2(targetX, _patrolY);

////        Vector2 newPos = Vector2.MoveTowards(ap, target, speed * Time.deltaTime);
////        _rt.anchoredPosition = newPos;
////        FaceDirection(target.x - ap.x);

////        // Once we've arrived, point the patrol direction back toward the centre
////        // so DoPatrol doesn't immediately bounce off the wall we just reached.
////        if ((newPos - target).sqrMagnitude < 0.01f)
////            _patrolDir = newPos.x >= 0f ? -1f : 1f;
////    }


////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — COMBAT  (detect → chase → breathe fire)
////    // ══════════════════════════════════════════════════════════════════════════
////    //
////    // Called every frame instead of DoPatrol() while Flying. Falls back to the
////    // normal left↔right patrol whenever there is no live enemy in range.

////    private void DoCombatAI()
////    {
////        // Drop a target that died or was destroyed while we weren't looking.
////        // NOTE: the Unity-null check (`_target != null`) must run first — it
////        // short-circuits before touching .IsDead, which would throw on an
////        // already-destroyed enemy GameObject.
////        if (_target != null && _target.IsDead)
////            _target = null;

////        if (_target == null)
////            _target = FindNearestEnemyInRange();

////        if (_target == null)
////        {
////            if (_isAttacking) StopAttacking();

////            if (IsOutsidePatrolZone())
////                ReturnToPatrolZone();
////            else
////                DoPatrol();

////            return;
////        }

////        float attackRange = dragonData != null ? dragonData.attackRange : 150f;
////        float dist = Vector3.Distance(transform.position, _target.transform.position);

////        if (dist <= attackRange)
////        {
////            if (!_isAttacking) StartAttacking();
////            FaceDirection(_target.transform.position.x - transform.position.x);
////            Breathe();
////        }
////        else
////        {
////            if (_isAttacking) StopAttacking();
////            ChaseTarget(_target);
////        }
////    }

////    /// <summary>Nearest live EnemyUnit within DragonData.detectionRadius, or null.</summary>
////    private EnemyUnit FindNearestEnemyInRange()
////    {
////        float radius = dragonData != null ? dragonData.detectionRadius : 500f;
////        Vector3 myPos = transform.position;

////        EnemyUnit nearest = null;
////        float bestDist = float.MaxValue;

////        foreach (EnemyUnit enemy in EnemyUnit.All)
////        {
////            if (enemy == null || enemy.IsDead) continue;

////            float dist = Vector3.Distance(myPos, enemy.transform.position);
////            if (dist <= radius && dist < bestDist)
////            {
////                bestDist = dist;
////                nearest = enemy;
////            }
////        }
////        return nearest;
////    }

////    /// <summary>
////    /// Moves the dragon toward the target using anchoredPosition so it travels
////    /// in canvas units at patrol speed — same coordinate space as DoPatrol,
////    /// preventing the instant-teleport that occurred when transform.position
////    /// (world units) was moved at a speed value meant for canvas units.
////    /// </summary>
////    private void ChaseTarget(EnemyUnit target)
////    {
////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;

////        // Convert target's world position to canvas anchoredPosition so we
////        // can measure and move in the same unit space as the patrol system.
////        Vector2 targetAP = WorldToCanvasAnchoredPos(target.transform.position);
////        Vector2 myAP = _rt.anchoredPosition;

////        Vector2 newAP = Vector2.MoveTowards(myAP, targetAP, speed * Time.deltaTime);
////        _rt.anchoredPosition = newAP;

////        FaceDirection(targetAP.x - myAP.x);
////    }

////    /// <summary>Starts the fire-breath particle + animation. Idempotent.</summary>
////    private void StartAttacking()
////    {
////        _isAttacking = true;
////        _damageTimer = 0f; // first damage tick lands immediately

////        if (fireBreath != null)
////            fireBreath.Play();
////        else
////            Debug.LogWarning("[DragonController] fireBreath not assigned — " +
////                              "no fire VFX will play while attacking.", this);

////        // Body and wings keep playing their normal Fly animation — the
////        // FireBreathFX particle is what visually sells the attack.
////        TriggerAnim(dragonData?.dragonAttackTrigger);
////        Debug.Log($"[DragonController] Breathing fire on '{_target?.name}'.");
////    }

////    /// <summary>Stops the fire-breath particle. Safe to call even if already stopped.</summary>
////    private void StopAttacking()
////    {
////        if (!_isAttacking && (fireBreath == null || !fireBreath.IsPlaying))
////            return;

////        _isAttacking = false;
////        if (fireBreath != null)
////            fireBreath.Stop();
////    }

////    /// <summary>Ticks fire damage into the current target while in attack range.</summary>
////    private void Breathe()
////    {
////        if (_target == null || _target.IsDead)
////        {
////            StopAttacking();
////            _target = null;
////            return;
////        }

////        float tickInterval = dragonData != null ? dragonData.damageTickInterval : 0.5f;
////        float damage = dragonData != null ? dragonData.fireDamage : 15f;

////        _damageTimer -= Time.deltaTime;
////        if (_damageTimer > 0f) return;

////        _target.TakeDamage(damage);
////        _damageTimer = tickInterval;

////        if (_target.IsDead)
////        {
////            Debug.Log($"[DragonController] Killed '{_target.name}' with fire breath.");
////            StopAttacking();
////            _target = null;
////        }
////    }

////    /// <summary>
////    /// Sets absolute facing from a movement-direction sign (negative = left,
////    /// positive = right). Unlike FlipHorizontal() (which toggles), this is
////    /// idempotent — safe to call every frame while chasing/attacking.
////    /// </summary>
////    private void FaceDirection(float dirX)
////    {
////        if (Mathf.Approximately(dirX, 0f)) return;

////        Vector3 s = transform.localScale;
////        float absX = Mathf.Abs(s.x);
////        bool movingLeft = dirX < 0f;
////        s.x = (movingLeft == spriteDefaultFacesLeft) ? absX : -absX;
////        transform.localScale = s;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // MOUNT — called by SoldierDragDrop.OnEndDrag
////    // ══════════════════════════════════════════════════════════════════════════

////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning("[DragonController] PerformMount: seat is null.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
////            return;
////        }

////        seat.MountSoldier(soldier);

////        if (_riderVisual != null)
////        {
////            var equipment = soldier.GetComponent<CharacterEquipment>();
////            _riderVisual.ShowForSoldier(equipment);
////        }
////        else
////        {
////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider invisible.", this);
////        }

////        soldier.gameObject.SetActive(false);

////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DISMOUNT — called by SoldierDragDrop
////    // ══════════════════════════════════════════════════════════════════════════

////    public void PerformDismount()
////    {
////        _riderVisual?.Hide();
////        Debug.Log($"[DragonController] Rider dismounted from '{name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void FlipHorizontal()
////    {
////        Vector3 s = transform.localScale;
////        s.x = -s.x;
////        transform.localScale = s;
////    }

////    private void ReturnToHome()
////    {
////        if (_savedParent == null) return;
////        _rt.SetParent(_savedParent, worldPositionStays: false);
////        _rt.SetSiblingIndex(_savedSiblingIndex);
////        _rt.anchoredPosition = _savedAnchoredPos;
////    }

////    private void SnapBack()
////    {
////        if (_currentZone != null)
////        {
////            // Flying dragon: reparent straight back to its zone centred.
////            // Do NOT use ReturnToHome() — _savedAnchoredPos was a mid-patrol
////            // snapshot relative to the FlyZone parent, which means nothing
////            // after the canvas-root reparent during drag.
////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            EnterFlying();
////            Debug.Log("[DragonController] SnapBack -> resume Flying");
////        }
////        else
////        {
////            // Idle dragon: restore exactly where it was before the drag.
////            ReturnToHome();
////            EnterIdle();
////            Debug.Log("[DragonController] SnapBack -> resume Idle");
////        }
////    }

////    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
////    {
////        if (rootCanvas == null) return Vector2.zero;
////        RectTransform canvasRt = rootCanvas.GetComponent<RectTransform>();
////        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            canvasRt, screenPos, null, out Vector2 local);
////        return local;
////    }

////    private void TriggerAnim(string trigger)
////    {
////        if (_anim == null)
////        {
////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
////            return;
////        }
////        if (dragonData == null)
////        {
////            Debug.LogWarning("[DragonController] DragonData is not assigned.", this);
////            return;
////        }
////        if (string.IsNullOrEmpty(trigger))
////        {
////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData.", this);
////            return;
////        }

////        _anim.SetTrigger(trigger);
////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////    }
////}


////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

////[RequireComponent(typeof(RectTransform))]
////[RequireComponent(typeof(CanvasGroup))]
////public class DragonController : MonoBehaviour,
////    IBeginDragHandler, IDragHandler, IEndDragHandler
////{
////    // ── Inspector ──────────────────────────────────────────────────────────────

////    [Header("Dragon Data")]
////    [SerializeField] private DragonData dragonData;

////    [Header("Canvas — auto-found if blank")]
////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////    [SerializeField] private Canvas rootCanvas;

////    [Header("Sprite Orientation")]
////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
////             "The controller flips the scale to match patrol direction.")]
////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////    [HideInInspector] public DragonEggSlot homeSlot;

////    // ── Private components ────────────────────────────────────────────────────

////    private RectTransform _rt;
////    private Animator _anim;
////    private CanvasGroup _cg;

////    // Found in children — all live permanently in the hierarchy.
////    private DragonRiderVisual _riderVisual;
////    private DragonRiderSeat _riderSeat;
////    private DragonWingAnimator _wingAnimator;
////    private DragonBodyAnimator _bodyAnimator;

////    // ── Drag state ────────────────────────────────────────────────────────────

////    private Transform _savedParent;
////    private Vector2 _savedAnchoredPos;
////    private int _savedSiblingIndex;
////    private Vector2 _dragOffset;

////    // ── Patrol state ──────────────────────────────────────────────────────────

////    private FlyZone _currentZone;
////    private float _patrolDir = 1f;   // +1 = right, -1 = left

////    // ── Dragon state ──────────────────────────────────────────────────────────

////    public enum DragonState { Idle, Dragging, Flying }
////    public DragonState State { get; private set; } = DragonState.Idle;

////    // ══════════════════════════════════════════════════════════════════════════
////    // LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        _rt = GetComponent<RectTransform>();
////        _anim = GetComponent<Animator>();
////        _cg = GetComponent<CanvasGroup>();

////        if (rootCanvas == null)
////            rootCanvas = GetComponentInParent<Canvas>();

////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
////        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

////        if (_riderVisual == null)
////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
////        if (_riderSeat == null)
////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
////                             "Add DragonRiderSeat to the RiderSeat child.", this);
////        if (_wingAnimator == null)
////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
////                             "Add DragonWingAnimator to the DragonWing child.", this);
////        if (_bodyAnimator == null)
////            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
////                             "Add DragonBodyAnimator to the DragonBody child.", this);
////    }

////    private void Start()
////    {
////        EnterIdle();
////    }

////    private void Update()
////    {
////        if (State == DragonState.Flying)
////            DoPatrol();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — BEGIN
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnBeginDrag(PointerEventData eventData)
////    {
////        // ── Rider lock check ──────────────────────────────────────────────────
////        // If a rider is mounted but NOT yet locked (Attached), block the dragon
////        // drag. The player must click Attach first before moving the dragon.
////        //
////        // If the rider IS locked: their CanvasGroup has blocksRaycasts=false so
////        // clicks pass through to the dragon — this handler fires — allow drag.
////        //
////        // If there is no rider → allow drag normally.
////        if (_riderSeat != null && _riderSeat.IsOccupied)
////        {
////            var rider = _riderSeat.MountedSoldier;
////            if (rider == null || !rider.IsLocked)
////            {
////                Debug.Log("[DragonController] Drag blocked — rider is not Attached. " +
////                          "Click Attach to lock the rider before moving the dragon.");
////                return;
////            }
////            // Rider IS locked — drag the whole dragon+rider unit.
////        }

////        _savedParent = _rt.parent;
////        _savedSiblingIndex = _rt.GetSiblingIndex();

////        // Switch to Dragging BEFORE SetParent so Update()/DoPatrol() stops
////        // moving the dragon the moment we lift it. This prevents the saved
////        // position from drifting between pointer-down and OnBeginDrag.
////        State = DragonState.Dragging;

////        // Capture anchored position BEFORE reparenting — this is the true
////        // resting position relative to the current parent, used for snap-back.
////        _savedAnchoredPos = _rt.anchoredPosition;

////        // Lift to canvas root with worldPositionStays so the dragon doesn't
////        // jump visually during reparenting.
////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////        _rt.SetAsLastSibling();

////        // FIX: Use delta-based dragging (same as SoldierDragDrop) instead of
////        // computing an absolute offset. Delta accumulation is immune to pivot/
////        // anchor mismatches between the FlyZone, DragonArea, and Canvas root,
////        // so the dragon stays exactly under the finger from the very first frame.
////        // _dragOffset is no longer used.

////        _cg.alpha = 0.75f;
////        _cg.blocksRaycasts = false;

////        Debug.Log("[DragonController] OnBeginDrag");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        // Delta-based: add the pointer's screen-space delta, adjusted for the
////        // canvas scale. This never needs an offset calculation and therefore
////        // never jumps — regardless of where in the hierarchy the dragon started.
////        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — END
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnEndDrag(PointerEventData eventData)
////    {
////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
////        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
////        _cg.alpha = 1f;

////        var results = new List<RaycastResult>();
////        EventSystem.current.RaycastAll(eventData, results);

////        _cg.blocksRaycasts = true;

////        FlyZone hitFlyZone = null;
////        DragonEggSlot hitAreaSlot = null;

////        foreach (var r in results)
////        {
////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////            if (hitFlyZone != null && hitAreaSlot != null) break;
////        }

////        if (hitFlyZone != null)
////        {
////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            _currentZone = hitFlyZone;
////            EnterFlying();
////        }
////        else if (hitAreaSlot != null)
////        {
////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////            _rt.anchoredPosition = Vector2.zero;
////            _currentZone = null;
////            EnterIdle();
////        }
////        else
////        {
////            SnapBack();
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — IDLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterIdle()
////    {
////        State = DragonState.Idle;

////        Vector3 s = transform.localScale;
////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////        transform.localScale = s;

////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
////        TriggerAnim(dragonData?.dragonIdleTrigger);
////        Debug.Log("[DragonController] -> Idle");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — FLYING + PATROL
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterFlying()
////    {
////        State = DragonState.Flying;
////        _patrolDir = -1f;

////        Vector3 s = transform.localScale;
////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////        transform.localScale = s;

////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
////        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
////        TriggerAnim(dragonData?.dragonFlyTrigger);
////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
////    }

////    private void DoPatrol()
////    {
////        if (_currentZone == null) return;

////        float speed = dragonData != null ? dragonData.chaseSpeed : 160f;
////        float halfWidth = _currentZone.PatrolHalfWidth;
////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

////        if (newX >= halfWidth)
////        {
////            newX = halfWidth;
////            _patrolDir = -1f;
////            FlipHorizontal();
////        }
////        else if (newX <= -halfWidth)
////        {
////            newX = -halfWidth;
////            _patrolDir = 1f;
////            FlipHorizontal();
////        }

////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // MOUNT — called by SoldierDragDrop.OnEndDrag
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
////    ///
////    /// What happens:
////    ///   1. seat.MountSoldier(soldier) is called.
////    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
////    ///            and reparents them under the seat.
////    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
////    ///      CharacterEquipment and displays the matching armor / helmet sprites
////    ///      on the dragon's built-in rider layers.
////    ///
////    /// No prefabs are spawned or destroyed.
////    /// </summary>
////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
////    {
////        if (seat == null)
////        {
////            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
////                             "Make sure the prefab has a DragonRiderSeat child.", this);
////            return;
////        }

////        if (seat.IsOccupied)
////        {
////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
////            return;
////        }

////        // Step 1 — seat the soldier (reparents and sets animator state).
////        seat.MountSoldier(soldier);

////        // Step 2 — show the dragon's rider visual with the soldier's equipment.
////        if (_riderVisual != null)
////        {
////            var equipment = soldier.GetComponent<CharacterEquipment>();
////            _riderVisual.ShowForSoldier(equipment);
////        }
////        else
////        {
////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
////                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
////        }

////        // Step 3 — deactivate the soldier GameObject so its own sprites are
////        // completely hidden. SetActive(false) is used instead of alpha=0 because
////        // the animator's SetState() calls re-enable Image components and would
////        // override an alpha-based hide.
////        soldier.gameObject.SetActive(false);

////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DISMOUNT — called by SoldierDragDrop
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
////    /// away from the seat (to the canvas root or back to their ground home).
////    ///
////    /// Hides the rider visual. The dragon continues its current state (Idle or
////    /// Flying) without any prefab swap.
////    /// </summary>
////    public void PerformDismount()
////    {
////        _riderVisual?.Hide();
////        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void FlipHorizontal()
////    {
////        Vector3 s = transform.localScale;
////        s.x = -s.x;
////        transform.localScale = s;
////    }

////    private void ReturnToHome()
////    {
////        if (_savedParent == null) return;
////        _rt.SetParent(_savedParent, worldPositionStays: false);
////        _rt.SetSiblingIndex(_savedSiblingIndex);
////        _rt.anchoredPosition = _savedAnchoredPos;
////    }

////    private void SnapBack()
////    {
////        ReturnToHome();

////        if (_currentZone != null)
////        {
////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////            EnterFlying();  // ← FIXED: was `State = DragonState.Flying` (skipped wing animator)
////            Debug.Log("[DragonController] SnapBack -> resume Flying");
////        }
////        else
////        {
////            EnterIdle();
////            Debug.Log("[DragonController] SnapBack -> resume Idle");
////        }
////    }

////    private void TriggerAnim(string trigger)
////    {
////        if (_anim == null)
////        {
////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
////            return;
////        }
////        if (dragonData == null)
////        {
////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
////                             "Drag it into the DragonController Inspector field.", this);
////            return;
////        }
////        if (string.IsNullOrEmpty(trigger))
////        {
////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
////            return;
////        }

////        _anim.SetTrigger(trigger);
////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////    }
////}


//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//[RequireComponent(typeof(RectTransform))]
//[RequireComponent(typeof(CanvasGroup))]
//public class DragonController : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Dragon Data")]
//    [SerializeField] private DragonData dragonData;

//    [Header("Canvas — auto-found if blank")]
//    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//    [SerializeField] private Canvas rootCanvas;

//    [Header("Sprite Orientation")]
//    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//             "The controller flips the scale to match patrol direction.")]
//    [SerializeField] private bool spriteDefaultFacesLeft = true;

//    [Header("Combat — Fire Breath")]
//    [Tooltip("ParticleSystem on a child GameObject (positioned at the dragon's mouth). " +
//             "Played while the dragon is breathing fire, stopped otherwise.")]
//    [SerializeField] private DragonFireBreath fireBreath;

//    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

//    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//    [HideInInspector] public DragonEggSlot homeSlot;

//    // ── Private components ────────────────────────────────────────────────────

//    private RectTransform _rt;
//    private Animator _anim;
//    private CanvasGroup _cg;

//    // Found in children — all live permanently in the hierarchy.
//    private DragonRiderVisual _riderVisual;
//    private DragonRiderSeat _riderSeat;
//    private DragonWingAnimator _wingAnimator;
//    private DragonBodyAnimator _bodyAnimator;

//    // ── Drag state ────────────────────────────────────────────────────────────

//    private Transform _savedParent;
//    private Vector2 _savedAnchoredPos;
//    private int _savedSiblingIndex;

//    // ── Patrol state ──────────────────────────────────────────────────────────

//    private FlyZone _currentZone;
//    private float _patrolDir = 1f;   // +1 = right, -1 = left
//    private float _patrolY;          // anchoredPosition.y to glide back to after a chase

//    // ── Combat state ───────────────────────────────────────────────────────────

//    private EnemyUnit _target;
//    private bool _isAttacking;
//    private float _damageTimer;

//    // ── Dragon state ──────────────────────────────────────────────────────────

//    public enum DragonState { Idle, Dragging, Flying }
//    public DragonState State { get; private set; } = DragonState.Idle;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _rt = GetComponent<RectTransform>();
//        _anim = GetComponent<Animator>();
//        _cg = GetComponent<CanvasGroup>();

//        if (rootCanvas == null)
//            rootCanvas = GetComponentInParent<Canvas>();

//        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
//        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
//        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
//        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

//        if (_riderVisual == null)
//            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
//                             "Add DragonRiderVisual to a child of RiderSeat.", this);
//        if (_riderSeat == null)
//            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
//                             "Add DragonRiderSeat to the RiderSeat child.", this);
//        if (_wingAnimator == null)
//            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
//                             "Add DragonWingAnimator to the DragonWing child.", this);
//        if (_bodyAnimator == null)
//            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
//                             "Add DragonBodyAnimator to the DragonBody child.", this);
//    }

//    private void Start()
//    {
//        EnterIdle();
//    }

//    private void Update()
//    {
//        if (State == DragonState.Flying)
//            DoCombatAI();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        // No lock check needed — the Detach button is the only way to dismount.
//        // The dragon can always be dragged freely (rider travels with it).

//        // Cancel any in-progress fire breath so the particle doesn't keep
//        // playing (or damage ticking) while the dragon is being carried.
//        StopAttacking();
//        _target = null;

//        _savedParent = _rt.parent;
//        _savedSiblingIndex = _rt.GetSiblingIndex();

//        // Set Dragging BEFORE SetParent so DoPatrol() stops immediately.
//        State = DragonState.Dragging;

//        // Capture position BEFORE reparenting — used for snap-back.
//        _savedAnchoredPos = _rt.anchoredPosition;

//        // ── Play idle animation while the dragon is carried ───────────────────
//        // Wings fold and body settles the moment the player picks it up,
//        // whether it was patrolling or sitting idle. This gives clear feedback
//        // that the dragon is "in hand" and ready to be placed.
//        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
//        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
//        TriggerAnim(dragonData?.dragonIdleTrigger);

//        // Lift to canvas root so it renders over every other UI element.
//        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//        _rt.SetAsLastSibling();

//        _cg.alpha = 0.75f;
//        _cg.blocksRaycasts = false;

//        Debug.Log("[DragonController] OnBeginDrag");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        // Delta-based movement — immune to pivot/anchor mismatches regardless
//        // of where in the hierarchy the dragon started (FlyZone, DragonArea…).
//        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // Restore opacity but keep blocksRaycasts=false until AFTER the raycast
//        // so the dragon's CanvasGroup does not shadow the target beneath it.
//        _cg.alpha = 1f;

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        _cg.blocksRaycasts = true;

//        FlyZone hitFlyZone = null;
//        DragonEggSlot hitAreaSlot = null;

//        foreach (var r in results)
//        {
//            // Skip any UI element that belongs to the slot the dragon came FROM.
//            // Without this, an idle dragon dragged toward the FlyZone would still
//            // have its own DragonArea underneath the pointer (the DragonArea panel
//            // is large), causing hitAreaSlot to win and snapping the dragon back.
//            if (_savedParent != null && r.gameObject.transform.IsChildOf(_savedParent))
//                continue;

//            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//            if (hitFlyZone != null && hitAreaSlot != null) break;
//        }

//        // FlyZone is checked first when coming from idle (no _currentZone).
//        // DragonEggSlot wins when coming from a FlyZone (returning home).
//        // The _savedParent skip above already strips the home slot from results,
//        // so a flying dragon dropped back on its own DragonArea still works.
//        if (hitFlyZone != null && _currentZone == null)
//        {
//            // Idle dragon dropped onto FlyZone → start flying.
//            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            _currentZone = hitFlyZone;
//            EnterFlying();
//        }
//        else if (hitAreaSlot != null)
//        {
//            // Flying dragon dropped onto DragonArea → return to idle.
//            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            _currentZone = null;
//            EnterIdle();
//        }
//        else if (hitFlyZone != null)
//        {
//            // Flying dragon dropped onto a (different) FlyZone → keep flying.
//            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            _currentZone = hitFlyZone;
//            EnterFlying();
//        }
//        else
//        {
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — IDLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterIdle()
//    {
//        State = DragonState.Idle;

//        Vector3 s = transform.localScale;
//        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//        transform.localScale = s;

//        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
//        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
//        TriggerAnim(dragonData?.dragonIdleTrigger);
//        Debug.Log("[DragonController] -> Idle");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — FLYING + PATROL
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterFlying()
//    {
//        State = DragonState.Flying;
//        _patrolDir = -1f;
//        _patrolY = _rt.anchoredPosition.y; // remember the patrol-line height before any chasing

//        Vector3 s = transform.localScale;
//        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//        transform.localScale = s;

//        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
//        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
//        TriggerAnim(dragonData?.dragonFlyTrigger);
//        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
//    }

//    private void DoPatrol()
//    {
//        if (_currentZone == null) return;

//        float speed = dragonData != null ? dragonData.chaseSpeed : 160f;
//        float halfWidth = _currentZone.PatrolHalfWidth;
//        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

//        if (newX >= halfWidth)
//        {
//            newX = halfWidth;
//            _patrolDir = -1f;
//        }
//        else if (newX <= -halfWidth)
//        {
//            newX = -halfWidth;
//            _patrolDir = 1f;
//        }

//        // Use FaceDirection (absolute) not FlipHorizontal (toggle) so facing
//        // is always driven by _patrolDir and never drifts out of sync after
//        // a chase/attack that used FaceDirection for its own facing.
//        FaceDirection(_patrolDir);

//        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//    }

//    /// <summary>True if a chase has carried the dragon outside its FlyZone's patrol box
//    /// (in X) or off the patrol-line height (in Y).</summary>
//    private bool IsOutsidePatrolZone()
//    {
//        if (_currentZone == null) return false;

//        float halfWidth = _currentZone.PatrolHalfWidth;
//        Vector2 ap = _rt.anchoredPosition;
//        return Mathf.Abs(ap.x) > halfWidth || !Mathf.Approximately(ap.y, _patrolY);
//    }

//    /// <summary>
//    /// Smoothly flies back toward the patrol box/line after a chase, instead of
//    /// snapping instantly — fixes the "dragon jumps backward" pop that happened
//    /// when DoPatrol's clamp caught a position that was already outside bounds.
//    /// </summary>
//    private void ReturnToPatrolZone()
//    {
//        float halfWidth = _currentZone.PatrolHalfWidth;
//        float speed = dragonData != null ? dragonData.chaseSpeed : 160f;

//        Vector2 ap = _rt.anchoredPosition;
//        float targetX = Mathf.Clamp(ap.x, -halfWidth, halfWidth);
//        Vector2 target = new Vector2(targetX, _patrolY);

//        Vector2 newPos = Vector2.MoveTowards(ap, target, speed * Time.deltaTime);
//        _rt.anchoredPosition = newPos;
//        FaceDirection(target.x - ap.x);

//        // Once we've arrived, point the patrol direction back toward the centre
//        // so DoPatrol doesn't immediately bounce off the wall we just reached.
//        if ((newPos - target).sqrMagnitude < 0.01f)
//            _patrolDir = newPos.x >= 0f ? -1f : 1f;
//    }


//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — COMBAT  (detect → chase → breathe fire)
//    // ══════════════════════════════════════════════════════════════════════════
//    //
//    // Called every frame instead of DoPatrol() while Flying. Falls back to the
//    // normal left↔right patrol whenever there is no live enemy in range.

//    private void DoCombatAI()
//    {
//        // Drop a target that died or was destroyed while we weren't looking.
//        // NOTE: the Unity-null check (`_target != null`) must run first — it
//        // short-circuits before touching .IsDead, which would throw on an
//        // already-destroyed enemy GameObject.
//        if (_target != null && _target.IsDead)
//            _target = null;

//        if (_target == null)
//            _target = FindNearestEnemyInRange();

//        if (_target == null)
//        {
//            if (_isAttacking) StopAttacking();

//            if (IsOutsidePatrolZone())
//                ReturnToPatrolZone();
//            else
//                DoPatrol();

//            return;
//        }

//        float attackRange = dragonData != null ? dragonData.attackRange : 150f;
//        float dist = Vector3.Distance(transform.position, _target.transform.position);

//        if (dist <= attackRange)
//        {
//            if (!_isAttacking) StartAttacking();
//            FaceDirection(_target.transform.position.x - transform.position.x);
//            Breathe();
//        }
//        else
//        {
//            if (_isAttacking) StopAttacking();
//            ChaseTarget(_target);
//        }
//    }

//    /// <summary>Nearest live EnemyUnit within DragonData.detectionRadius, or null.</summary>
//    private EnemyUnit FindNearestEnemyInRange()
//    {
//        float radius = dragonData != null ? dragonData.detectionRadius : 500f;
//        Vector3 myPos = transform.position;

//        EnemyUnit nearest = null;
//        float bestDist = float.MaxValue;

//        foreach (EnemyUnit enemy in EnemyUnit.All)
//        {
//            if (enemy == null || enemy.IsDead) continue;

//            float dist = Vector3.Distance(myPos, enemy.transform.position);
//            if (dist <= radius && dist < bestDist)
//            {
//                bestDist = dist;
//                nearest = enemy;
//            }
//        }
//        return nearest;
//    }

//    /// <summary>
//    /// Moves the dragon toward the target using anchoredPosition so it travels
//    /// in canvas units at patrol speed — same coordinate space as DoPatrol,
//    /// preventing the instant-teleport that occurred when transform.position
//    /// (world units) was moved at a speed value meant for canvas units.
//    /// </summary>
//    private void ChaseTarget(EnemyUnit target)
//    {
//        float speed = dragonData != null ? dragonData.chaseSpeed : 160f;

//        // Convert target's world position to canvas anchoredPosition so we
//        // can measure and move in the same unit space as the patrol system.
//        Vector2 targetAP = WorldToCanvasAnchoredPos(target.transform.position);
//        Vector2 myAP = _rt.anchoredPosition;

//        Vector2 newAP = Vector2.MoveTowards(myAP, targetAP, speed * Time.deltaTime);
//        _rt.anchoredPosition = newAP;

//        FaceDirection(targetAP.x - myAP.x);
//    }

//    /// <summary>Starts the fire-breath particle + animation. Idempotent.</summary>
//    private void StartAttacking()
//    {
//        _isAttacking = true;
//        _damageTimer = 0f; // first damage tick lands immediately

//        if (fireBreath != null)
//            fireBreath.Play();
//        else
//            Debug.LogWarning("[DragonController] fireBreath not assigned — " +
//                              "no fire VFX will play while attacking.", this);

//        // Body and wings keep playing their normal Fly animation — the
//        // FireBreathFX particle is what visually sells the attack.
//        TriggerAnim(dragonData?.dragonAttackTrigger);
//        Debug.Log($"[DragonController] Breathing fire on '{_target?.name}'.");
//    }

//    /// <summary>Stops the fire-breath particle. Safe to call even if already stopped.</summary>
//    private void StopAttacking()
//    {
//        if (!_isAttacking && (fireBreath == null || !fireBreath.IsPlaying))
//            return;

//        _isAttacking = false;
//        if (fireBreath != null)
//            fireBreath.Stop();
//    }

//    /// <summary>Ticks fire damage into the current target while in attack range.</summary>
//    private void Breathe()
//    {
//        if (_target == null || _target.IsDead)
//        {
//            StopAttacking();
//            _target = null;
//            return;
//        }

//        float tickInterval = dragonData != null ? dragonData.damageTickInterval : 0.5f;
//        float damage = dragonData != null ? dragonData.fireDamage : 15f;

//        _damageTimer -= Time.deltaTime;
//        if (_damageTimer > 0f) return;

//        _target.TakeDamage(damage);
//        _damageTimer = tickInterval;

//        if (_target.IsDead)
//        {
//            Debug.Log($"[DragonController] Killed '{_target.name}' with fire breath.");
//            StopAttacking();
//            _target = null;
//        }
//    }

//    /// <summary>
//    /// Sets absolute facing from a movement-direction sign (negative = left,
//    /// positive = right). Unlike FlipHorizontal() (which toggles), this is
//    /// idempotent — safe to call every frame while chasing/attacking.
//    /// </summary>
//    private void FaceDirection(float dirX)
//    {
//        if (Mathf.Approximately(dirX, 0f)) return;

//        Vector3 s = transform.localScale;
//        float absX = Mathf.Abs(s.x);
//        bool movingLeft = dirX < 0f;
//        s.x = (movingLeft == spriteDefaultFacesLeft) ? absX : -absX;
//        transform.localScale = s;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // MOUNT — called by SoldierDragDrop.OnEndDrag
//    // ══════════════════════════════════════════════════════════════════════════

//    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
//    {
//        if (seat == null)
//        {
//            Debug.LogWarning("[DragonController] PerformMount: seat is null.", this);
//            return;
//        }

//        if (seat.IsOccupied)
//        {
//            Debug.Log("[DragonController] PerformMount: seat already occupied.");
//            return;
//        }

//        seat.MountSoldier(soldier);

//        if (_riderVisual != null)
//        {
//            var equipment = soldier.GetComponent<CharacterEquipment>();
//            _riderVisual.ShowForSoldier(equipment);
//        }
//        else
//        {
//            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider invisible.", this);
//        }

//        soldier.gameObject.SetActive(false);

//        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DISMOUNT — called by SoldierDragDrop
//    // ══════════════════════════════════════════════════════════════════════════

//    public void PerformDismount()
//    {
//        _riderVisual?.Hide();
//        Debug.Log($"[DragonController] Rider dismounted from '{name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void FlipHorizontal()
//    {
//        Vector3 s = transform.localScale;
//        s.x = -s.x;
//        transform.localScale = s;
//    }

//    private void ReturnToHome()
//    {
//        if (_savedParent == null) return;
//        _rt.SetParent(_savedParent, worldPositionStays: false);
//        _rt.SetSiblingIndex(_savedSiblingIndex);
//        _rt.anchoredPosition = _savedAnchoredPos;
//    }

//    private void SnapBack()
//    {
//        if (_currentZone != null)
//        {
//            // Flying dragon: reparent straight back to its zone centred.
//            // Do NOT use ReturnToHome() — _savedAnchoredPos was a mid-patrol
//            // snapshot relative to the FlyZone parent, which means nothing
//            // after the canvas-root reparent during drag.
//            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            EnterFlying();
//            Debug.Log("[DragonController] SnapBack -> resume Flying");
//        }
//        else
//        {
//            // Idle dragon: restore exactly where it was before the drag.
//            ReturnToHome();
//            EnterIdle();
//            Debug.Log("[DragonController] SnapBack -> resume Idle");
//        }
//    }

//    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
//    {
//        if (rootCanvas == null) return Vector2.zero;
//        RectTransform canvasRt = rootCanvas.GetComponent<RectTransform>();
//        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            canvasRt, screenPos, null, out Vector2 local);
//        return local;
//    }

//    private void TriggerAnim(string trigger)
//    {
//        if (_anim == null)
//        {
//            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
//            return;
//        }
//        if (dragonData == null)
//        {
//            Debug.LogWarning("[DragonController] DragonData is not assigned.", this);
//            return;
//        }
//        if (string.IsNullOrEmpty(trigger))
//        {
//            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData.", this);
//            return;
//        }

//        _anim.SetTrigger(trigger);
//        Debug.Log($"[DragonController] SetTrigger({trigger})");
//    }
//}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//[RequireComponent(typeof(RectTransform))]
//[RequireComponent(typeof(CanvasGroup))]
//public class DragonController : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("Dragon Data")]
//    [SerializeField] private DragonData dragonData;

//    [Header("Canvas — auto-found if blank")]
//    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//    [SerializeField] private Canvas rootCanvas;

//    [Header("Sprite Orientation")]
//    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//             "The controller flips the scale to match patrol direction.")]
//    [SerializeField] private bool spriteDefaultFacesLeft = true;

//    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

//    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//    [HideInInspector] public DragonEggSlot homeSlot;

//    // ── Private components ────────────────────────────────────────────────────

//    private RectTransform _rt;
//    private Animator _anim;
//    private CanvasGroup _cg;

//    // Found in children — all live permanently in the hierarchy.
//    private DragonRiderVisual _riderVisual;
//    private DragonRiderSeat _riderSeat;
//    private DragonWingAnimator _wingAnimator;
//    private DragonBodyAnimator _bodyAnimator;

//    // ── Drag state ────────────────────────────────────────────────────────────

//    private Transform _savedParent;
//    private Vector2 _savedAnchoredPos;
//    private int _savedSiblingIndex;
//    private Vector2 _dragOffset;

//    // ── Patrol state ──────────────────────────────────────────────────────────

//    private FlyZone _currentZone;
//    private float _patrolDir = 1f;   // +1 = right, -1 = left

//    // ── Dragon state ──────────────────────────────────────────────────────────

//    public enum DragonState { Idle, Dragging, Flying }
//    public DragonState State { get; private set; } = DragonState.Idle;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _rt = GetComponent<RectTransform>();
//        _anim = GetComponent<Animator>();
//        _cg = GetComponent<CanvasGroup>();

//        if (rootCanvas == null)
//            rootCanvas = GetComponentInParent<Canvas>();

//        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
//        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
//        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
//        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

//        if (_riderVisual == null)
//            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
//                             "Add DragonRiderVisual to a child of RiderSeat.", this);
//        if (_riderSeat == null)
//            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
//                             "Add DragonRiderSeat to the RiderSeat child.", this);
//        if (_wingAnimator == null)
//            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
//                             "Add DragonWingAnimator to the DragonWing child.", this);
//        if (_bodyAnimator == null)
//            Debug.LogWarning("[DragonController] No DragonBodyAnimator found in children. " +
//                             "Add DragonBodyAnimator to the DragonBody child.", this);
//    }

//    private void Start()
//    {
//        EnterIdle();
//    }

//    private void Update()
//    {
//        if (State == DragonState.Flying)
//            DoPatrol();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        // ── Rider lock check ──────────────────────────────────────────────────
//        // If a rider is mounted but NOT yet locked (Attached), block the dragon
//        // drag. The player must click Attach first before moving the dragon.
//        //
//        // If the rider IS locked: their CanvasGroup has blocksRaycasts=false so
//        // clicks pass through to the dragon — this handler fires — allow drag.
//        //
//        // If there is no rider → allow drag normally.
//        if (_riderSeat != null && _riderSeat.IsOccupied)
//        {
//            var rider = _riderSeat.MountedSoldier;
//            if (rider == null || !rider.IsLocked)
//            {
//                Debug.Log("[DragonController] Drag blocked — rider is not Attached. " +
//                          "Click Attach to lock the rider before moving the dragon.");
//                return;
//            }
//            // Rider IS locked — drag the whole dragon+rider unit.
//        }

//        _savedParent = _rt.parent;
//        _savedSiblingIndex = _rt.GetSiblingIndex();

//        // Switch to Dragging BEFORE SetParent so Update()/DoPatrol() stops
//        // moving the dragon the moment we lift it. This prevents the saved
//        // position from drifting between pointer-down and OnBeginDrag.
//        State = DragonState.Dragging;

//        // Capture anchored position BEFORE reparenting — this is the true
//        // resting position relative to the current parent, used for snap-back.
//        _savedAnchoredPos = _rt.anchoredPosition;

//        // Lift to canvas root with worldPositionStays so the dragon doesn't
//        // jump visually during reparenting.
//        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//        _rt.SetAsLastSibling();

//        // FIX: Use delta-based dragging (same as SoldierDragDrop) instead of
//        // computing an absolute offset. Delta accumulation is immune to pivot/
//        // anchor mismatches between the FlyZone, DragonArea, and Canvas root,
//        // so the dragon stays exactly under the finger from the very first frame.
//        // _dragOffset is no longer used.

//        _cg.alpha = 0.75f;
//        _cg.blocksRaycasts = false;

//        Debug.Log("[DragonController] OnBeginDrag");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        // Delta-based: add the pointer's screen-space delta, adjusted for the
//        // canvas scale. This never needs an offset calculation and therefore
//        // never jumps — regardless of where in the hierarchy the dragon started.
//        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
//        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
//        _cg.alpha = 1f;

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        _cg.blocksRaycasts = true;

//        FlyZone hitFlyZone = null;
//        DragonEggSlot hitAreaSlot = null;

//        foreach (var r in results)
//        {
//            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//            if (hitFlyZone != null && hitAreaSlot != null) break;
//        }

//        if (hitFlyZone != null)
//        {
//            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            _currentZone = hitFlyZone;
//            EnterFlying();
//        }
//        else if (hitAreaSlot != null)
//        {
//            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;
//            _currentZone = null;
//            EnterIdle();
//        }
//        else
//        {
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — IDLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterIdle()
//    {
//        State = DragonState.Idle;

//        Vector3 s = transform.localScale;
//        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//        transform.localScale = s;

//        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
//        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
//        TriggerAnim(dragonData?.dragonIdleTrigger);
//        Debug.Log("[DragonController] -> Idle");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — FLYING + PATROL
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterFlying()
//    {
//        State = DragonState.Flying;
//        _patrolDir = -1f;

//        Vector3 s = transform.localScale;
//        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//        transform.localScale = s;

//        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
//        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
//        TriggerAnim(dragonData?.dragonFlyTrigger);
//        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
//    }

//    private void DoPatrol()
//    {
//        if (_currentZone == null) return;

//        float speed = dragonData != null ? dragonData.chaseSpeed : 280f;
//        float halfWidth = _currentZone.PatrolHalfWidth;
//        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

//        if (newX >= halfWidth)
//        {
//            newX = halfWidth;
//            _patrolDir = -1f;
//            FlipHorizontal();
//        }
//        else if (newX <= -halfWidth)
//        {
//            newX = -halfWidth;
//            _patrolDir = 1f;
//            FlipHorizontal();
//        }

//        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // MOUNT — called by SoldierDragDrop.OnEndDrag
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
//    ///
//    /// What happens:
//    ///   1. seat.MountSoldier(soldier) is called.
//    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
//    ///            and reparents them under the seat.
//    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
//    ///      CharacterEquipment and displays the matching armor / helmet sprites
//    ///      on the dragon's built-in rider layers.
//    ///
//    /// No prefabs are spawned or destroyed.
//    /// </summary>
//    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
//    {
//        if (seat == null)
//        {
//            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
//                             "Make sure the prefab has a DragonRiderSeat child.", this);
//            return;
//        }

//        if (seat.IsOccupied)
//        {
//            Debug.Log("[DragonController] PerformMount: seat already occupied.");
//            return;
//        }

//        // Step 1 — seat the soldier (reparents and sets animator state).
//        seat.MountSoldier(soldier);

//        // Step 2 — show the dragon's rider visual with the soldier's equipment.
//        if (_riderVisual != null)
//        {
//            var equipment = soldier.GetComponent<CharacterEquipment>();
//            _riderVisual.ShowForSoldier(equipment);
//        }
//        else
//        {
//            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
//                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
//        }

//        // Step 3 — deactivate the soldier GameObject so its own sprites are
//        // completely hidden. SetActive(false) is used instead of alpha=0 because
//        // the animator's SetState() calls re-enable Image components and would
//        // override an alpha-based hide.
//        soldier.gameObject.SetActive(false);

//        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DISMOUNT — called by SoldierDragDrop
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
//    /// away from the seat (to the canvas root or back to their ground home).
//    ///
//    /// Hides the rider visual. The dragon continues its current state (Idle or
//    /// Flying) without any prefab swap.
//    /// </summary>
//    public void PerformDismount()
//    {
//        _riderVisual?.Hide();
//        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void FlipHorizontal()
//    {
//        Vector3 s = transform.localScale;
//        s.x = -s.x;
//        transform.localScale = s;
//    }

//    private void ReturnToHome()
//    {
//        if (_savedParent == null) return;
//        _rt.SetParent(_savedParent, worldPositionStays: false);
//        _rt.SetSiblingIndex(_savedSiblingIndex);
//        _rt.anchoredPosition = _savedAnchoredPos;
//    }

//    private void SnapBack()
//    {
//        ReturnToHome();

//        if (_currentZone != null)
//        {
//            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//            EnterFlying();  // ← FIXED: was `State = DragonState.Flying` (skipped wing animator)
//            Debug.Log("[DragonController] SnapBack -> resume Flying");
//        }
//        else
//        {
//            EnterIdle();
//            Debug.Log("[DragonController] SnapBack -> resume Idle");
//        }
//    }

//    private void TriggerAnim(string trigger)
//    {
//        if (_anim == null)
//        {
//            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
//            return;
//        }
//        if (dragonData == null)
//        {
//            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
//                             "Drag it into the DragonController Inspector field.", this);
//            return;
//        }
//        if (string.IsNullOrEmpty(trigger))
//        {
//            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
//                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
//            return;
//        }

//        _anim.SetTrigger(trigger);
//        Debug.Log($"[DragonController] SetTrigger({trigger})");
//    }
//}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Combat — Fire Breath")]
    [Tooltip("ParticleSystem on a child GameObject (positioned at the dragon's mouth). " +
             "Played while the dragon is breathing fire, stopped otherwise.")]
    [SerializeField] private DragonFireBreath fireBreath;

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

    // ── Patrol state ──────────────────────────────────────────────────────────

    private FlyZone _currentZone;
    private float _patrolDir = 1f;   // +1 = right, -1 = left
    private float _patrolY;          // anchoredPosition.y to glide back to after a chase

    // ── Combat state ───────────────────────────────────────────────────────────

    private EnemyUnit _target;
    private bool _isAttacking;
    private float _damageTimer;

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
            DoCombatAI();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — BEGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        // No lock check needed — the Detach button is the only way to dismount.
        // The dragon can always be dragged freely (rider travels with it).

        // Cancel any in-progress fire breath so the particle doesn't keep
        // playing (or damage ticking) while the dragon is being carried.
        StopAttacking();
        _target = null;

        _savedParent = _rt.parent;
        _savedSiblingIndex = _rt.GetSiblingIndex();

        // Set Dragging BEFORE SetParent so DoPatrol() stops immediately.
        State = DragonState.Dragging;

        // Capture position BEFORE reparenting — used for snap-back.
        _savedAnchoredPos = _rt.anchoredPosition;

        // ── Play idle animation while the dragon is carried ───────────────────
        // Wings fold and body settles the moment the player picks it up,
        // whether it was patrolling or sitting idle. This gives clear feedback
        // that the dragon is "in hand" and ready to be placed.
        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle);
        TriggerAnim(dragonData?.dragonIdleTrigger);

        // Lift to canvas root so it renders over every other UI element.
        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        _cg.alpha = 0.75f;
        _cg.blocksRaycasts = false;

        Debug.Log("[DragonController] OnBeginDrag");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — MOVE
    // ══════════════════════════════════════════════════════════════════════════

    public void OnDrag(PointerEventData eventData)
    {
        // Delta-based movement — immune to pivot/anchor mismatches regardless
        // of where in the hierarchy the dragon started (FlyZone, DragonArea…).
        _rt.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — END
    // ══════════════════════════════════════════════════════════════════════════

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore opacity but keep blocksRaycasts=false until AFTER the raycast
        // so the dragon's CanvasGroup does not shadow the target beneath it.
        _cg.alpha = 1f;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        _cg.blocksRaycasts = true;

        FlyZone hitFlyZone = null;
        DragonEggSlot hitAreaSlot = null;

        foreach (var r in results)
        {
            // Skip any UI element that belongs to the slot the dragon came FROM.
            // Without this, an idle dragon dragged toward the FlyZone would still
            // have its own DragonArea underneath the pointer (the DragonArea panel
            // is large), causing hitAreaSlot to win and snapping the dragon back.
            if (_savedParent != null && r.gameObject.transform.IsChildOf(_savedParent))
                continue;

            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
            if (hitFlyZone != null && hitAreaSlot != null) break;
        }

        // FlyZone is checked first when coming from idle (no _currentZone).
        // DragonEggSlot wins when coming from a FlyZone (returning home).
        // The _savedParent skip above already strips the home slot from results,
        // so a flying dragon dropped back on its own DragonArea still works.
        if (hitFlyZone != null && _currentZone == null)
        {
            // Idle dragon dropped onto FlyZone → start flying.
            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = hitFlyZone;
            EnterFlying();
        }
        else if (hitAreaSlot != null)
        {
            // Flying dragon dropped onto DragonArea → return to idle.
            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = null;
            EnterIdle();
        }
        else if (hitFlyZone != null)
        {
            // Flying dragon dropped onto a (different) FlyZone → keep flying.
            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = hitFlyZone;
            EnterFlying();
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
        _patrolY = _rt.anchoredPosition.y; // remember the patrol-line height before any chasing

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

        float speed = dragonData != null ? dragonData.chaseSpeed : 280f;
        float halfWidth = _currentZone.PatrolHalfWidth;
        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

        if (newX >= halfWidth)
        {
            newX = halfWidth;
            _patrolDir = -1f;
        }
        else if (newX <= -halfWidth)
        {
            newX = -halfWidth;
            _patrolDir = 1f;
        }

        // Use FaceDirection (absolute) not FlipHorizontal (toggle) so facing
        // is always driven by _patrolDir and never drifts out of sync after
        // a chase/attack that used FaceDirection for its own facing.
        FaceDirection(_patrolDir);

        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
    }

    /// <summary>True if a chase has carried the dragon outside its FlyZone's patrol box
    /// (in X) or off the patrol-line height (in Y).</summary>
    private bool IsOutsidePatrolZone()
    {
        if (_currentZone == null) return false;

        float halfWidth = _currentZone.PatrolHalfWidth;
        Vector2 ap = _rt.anchoredPosition;
        return Mathf.Abs(ap.x) > halfWidth || !Mathf.Approximately(ap.y, _patrolY);
    }

    /// <summary>
    /// Smoothly flies back toward the patrol box/line after a chase, instead of
    /// snapping instantly — fixes the "dragon jumps backward" pop that happened
    /// when DoPatrol's clamp caught a position that was already outside bounds.
    /// </summary>
    private void ReturnToPatrolZone()
    {
        float halfWidth = _currentZone.PatrolHalfWidth;
        float speed = dragonData != null ? dragonData.chaseSpeed : 280f;

        Vector2 ap = _rt.anchoredPosition;
        float targetX = Mathf.Clamp(ap.x, -halfWidth, halfWidth);
        Vector2 target = new Vector2(targetX, _patrolY);

        Vector2 newPos = Vector2.MoveTowards(ap, target, speed * Time.deltaTime);
        _rt.anchoredPosition = newPos;
        FaceDirection(target.x - ap.x);

        // Once we've arrived, point the patrol direction back toward the centre
        // so DoPatrol doesn't immediately bounce off the wall we just reached.
        if ((newPos - target).sqrMagnitude < 0.01f)
            _patrolDir = newPos.x >= 0f ? -1f : 1f;
    }


    // ══════════════════════════════════════════════════════════════════════════
    // STATE — COMBAT  (detect → chase → breathe fire)
    // ══════════════════════════════════════════════════════════════════════════
    //
    // Called every frame instead of DoPatrol() while Flying. Falls back to the
    // normal left↔right patrol whenever there is no live enemy in range.

    private void DoCombatAI()
    {
        // Drop a target that died or was destroyed while we weren't looking.
        // NOTE: the Unity-null check (`_target != null`) must run first — it
        // short-circuits before touching .IsDead, which would throw on an
        // already-destroyed enemy GameObject.
        if (_target != null && _target.IsDead)
            _target = null;

        if (_target == null)
            _target = FindNearestEnemyInRange();

        if (_target == null)
        {
            if (_isAttacking) StopAttacking();

            if (IsOutsidePatrolZone())
                ReturnToPatrolZone();
            else
                DoPatrol();

            return;
        }

        float attackRange = dragonData != null ? dragonData.attackRange : 150f;
        float dist = Vector3.Distance(transform.position, _target.transform.position);

        if (dist <= attackRange)
        {
            if (!_isAttacking) StartAttacking();
            // Update aim target every frame so fire tracks the enemy as it moves
            fireBreath?.SetTarget(_target.transform);
            FaceDirection(_target.transform.position.x - transform.position.x);
            Breathe();
        }
        else
        {
            if (_isAttacking) StopAttacking();
            ChaseTarget(_target);
        }
    }

    /// <summary>Nearest live EnemyUnit within DragonData.detectionRadius, or null.</summary>
    private EnemyUnit FindNearestEnemyInRange()
    {
        float radius = dragonData != null ? dragonData.detectionRadius : 500f;
        Vector3 myPos = transform.position;

        EnemyUnit nearest = null;
        float bestDist = float.MaxValue;

        foreach (EnemyUnit enemy in EnemyUnit.All)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(myPos, enemy.transform.position);
            if (dist <= radius && dist < bestDist)
            {
                bestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Moves the dragon toward the target using anchoredPosition so it travels
    /// in canvas units at patrol speed — same coordinate space as DoPatrol,
    /// preventing the instant-teleport that occurred when transform.position
    /// (world units) was moved at a speed value meant for canvas units.
    /// </summary>
    private void ChaseTarget(EnemyUnit target)
    {
        float speed = dragonData != null ? dragonData.chaseSpeed : 280f;

        // Convert target's world position to canvas anchoredPosition so we
        // can measure and move in the same unit space as the patrol system.
        Vector2 targetAP = WorldToCanvasAnchoredPos(target.transform.position);
        Vector2 myAP = _rt.anchoredPosition;

        Vector2 newAP = Vector2.MoveTowards(myAP, targetAP, speed * Time.deltaTime);
        _rt.anchoredPosition = newAP;

        FaceDirection(targetAP.x - myAP.x);
    }

    /// <summary>Starts the fire-breath particle + animation. Idempotent.</summary>
    private void StartAttacking()
    {
        _isAttacking = true;
        _damageTimer = 0f; // first damage tick lands immediately

        if (fireBreath != null)
        {
            fireBreath.SetTarget(_target?.transform);
            fireBreath.Play();
        }
        else
            Debug.LogWarning("[DragonController] fireBreath not assigned — " +
                              "no fire VFX will play while attacking.", this);

        // Body and wings keep playing their normal Fly animation — the
        // FireBreathFX particle is what visually sells the attack.
        TriggerAnim(dragonData?.dragonAttackTrigger);
        Debug.Log($"[DragonController] Breathing fire on '{_target?.name}'.");
    }

    /// <summary>Stops the fire-breath particle. Safe to call even if already stopped.</summary>
    private void StopAttacking()
    {
        if (!_isAttacking && (fireBreath == null || !fireBreath.IsPlaying))
            return;

        _isAttacking = false;
        if (fireBreath != null)
        {
            fireBreath.Stop();
            fireBreath.ClearTarget();
        }
    }

    /// <summary>Ticks fire damage into the current target while in attack range.</summary>
    private void Breathe()
    {
        if (_target == null || _target.IsDead)
        {
            StopAttacking();
            _target = null;
            return;
        }

        float tickInterval = dragonData != null ? dragonData.damageTickInterval : 0.5f;
        float damage = dragonData != null ? dragonData.fireDamage : 15f;

        _damageTimer -= Time.deltaTime;
        if (_damageTimer > 0f) return;

        _target.TakeDamage(damage);
        _damageTimer = tickInterval;

        if (_target.IsDead)
        {
            Debug.Log($"[DragonController] Killed '{_target.name}' with fire breath.");
            StopAttacking();
            _target = null;
        }
    }

    /// <summary>
    /// Sets absolute facing from a movement-direction sign (negative = left,
    /// positive = right). Unlike FlipHorizontal() (which toggles), this is
    /// idempotent — safe to call every frame while chasing/attacking.
    /// </summary>
    private void FaceDirection(float dirX)
    {
        if (Mathf.Approximately(dirX, 0f)) return;

        Vector3 s = transform.localScale;
        float absX = Mathf.Abs(s.x);
        bool movingLeft = dirX < 0f;
        s.x = (movingLeft == spriteDefaultFacesLeft) ? absX : -absX;
        transform.localScale = s;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MOUNT — called by SoldierDragDrop.OnEndDrag
    // ══════════════════════════════════════════════════════════════════════════

    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
    {
        if (seat == null)
        {
            Debug.LogWarning("[DragonController] PerformMount: seat is null.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log("[DragonController] PerformMount: seat already occupied.");
            return;
        }

        seat.MountSoldier(soldier);

        if (_riderVisual != null)
        {
            var equipment = soldier.GetComponent<CharacterEquipment>();
            _riderVisual.ShowForSoldier(equipment);
        }
        else
        {
            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider invisible.", this);
        }

        soldier.gameObject.SetActive(false);

        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DISMOUNT — called by SoldierDragDrop
    // ══════════════════════════════════════════════════════════════════════════

    public void PerformDismount()
    {
        _riderVisual?.Hide();
        Debug.Log($"[DragonController] Rider dismounted from '{name}'.");
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
        if (_currentZone != null)
        {
            // Flying dragon: reparent straight back to its zone centred.
            // Do NOT use ReturnToHome() — _savedAnchoredPos was a mid-patrol
            // snapshot relative to the FlyZone parent, which means nothing
            // after the canvas-root reparent during drag.
            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            EnterFlying();
            Debug.Log("[DragonController] SnapBack -> resume Flying");
        }
        else
        {
            // Idle dragon: restore exactly where it was before the drag.
            ReturnToHome();
            EnterIdle();
            Debug.Log("[DragonController] SnapBack -> resume Idle");
        }
    }

    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
    {
        if (rootCanvas == null) return Vector2.zero;
        RectTransform canvasRt = rootCanvas.GetComponent<RectTransform>();
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos, null, out Vector2 local);
        return local;
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
            Debug.LogWarning("[DragonController] DragonData is not assigned.", this);
            return;
        }
        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData.", this);
            return;
        }

        _anim.SetTrigger(trigger);
        Debug.Log($"[DragonController] SetTrigger({trigger})");
    }
}