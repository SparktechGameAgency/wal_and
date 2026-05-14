
//////////////using System.Collections.Generic;
//////////////using UnityEngine;
//////////////using UnityEngine.EventSystems;
//////////////using UnityEngine.UI;

///////////////// <summary>
///////////////// AREA FORGE — DragonController
/////////////////
///////////////// Attach to both the plain dragon prefab and the rider dragon prefab.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  STATES
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Idle      Dragon sits inside DragonArea.
/////////////////  Dragging  Dragon follows the pointer at canvas-root level.
/////////////////  Flying    Dragon patrols left↔right inside a FlyZone.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  TWO-PREFAB RIDER SWAP
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Two separate dragon prefabs exist in the project:
/////////////////
/////////////////    PlainDragon   — no rider, draggable by the player.
/////////////////    RiderDragon   — rider visuals baked in, soldier parented to its
/////////////////                    DragonRiderSeat at runtime.
/////////////////
/////////////////  When a soldier is dropped on the plain dragon:
/////////////////    1. PerformMount() spawns the rider variant at the same position.
/////////////////    2. All patrol state (zone, direction, flip, homeSlot) is copied.
/////////////////    3. The soldier is mounted on the rider's DragonRiderSeat.
/////////////////    4. The plain dragon is deactivated (not destroyed — reused on dismount).
/////////////////
/////////////////  When the soldier leaves the rider dragon:
/////////////////    1. PerformDismount() spawns the plain variant back.
/////////////////    2. State is copied again.
/////////////////    3. The rider dragon is destroyed.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  INSPECTOR SETUP — PLAIN DRAGON PREFAB
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  dragonData           Your DragonData ScriptableObject.
/////////////////  riderVariantPrefab   Drag the RiderDragon prefab here.
/////////////////  plainVariantPrefab   Leave BLANK on the plain dragon.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  INSPECTOR SETUP — RIDER DRAGON PREFAB
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  dragonData           Same DragonData ScriptableObject.
/////////////////  riderVariantPrefab   Leave BLANK on the rider dragon.
/////////////////  plainVariantPrefab   Drag the PlainDragon prefab here.
/////////////////
/////////////////  The rider dragon MUST have a DragonRiderSeat child so the soldier
/////////////////  can be reparented under it.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  SETUP — OTHER
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  1. Both prefabs need a CanvasGroup (auto-required below).
/////////////////  2. FlyZone.cs must be on your FlyZone GameObject with a Graphic
/////////////////     component so the EventSystem can raycast it.
/////////////////  3. In DragonEggSlot.EnterHatched(), after spawning the plain dragon:
/////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////////        if (dc != null) dc.homeSlot = this;
///////////////// </summary>
//////////////[RequireComponent(typeof(RectTransform))]
//////////////[RequireComponent(typeof(CanvasGroup))]
//////////////public class DragonController : MonoBehaviour,
//////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////{
//////////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////////    [Header("Dragon Data")]
//////////////    [SerializeField] private DragonData dragonData;

//////////////    [Header("Canvas — auto-found if blank")]
//////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////////    [SerializeField] private Canvas rootCanvas;

//////////////    [Header("Sprite Orientation")]
//////////////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1.")]
//////////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

//////////////    // ── Rider Variant Prefab Swap ──────────────────────────────────────────────

//////////////    [Header("Rider Variant Prefab Swap")]
//////////////    [Tooltip("PLAIN DRAGON: drag the RiderDragon prefab here.\n\n" +
//////////////             "When a soldier mounts, this prefab is spawned in place of the plain " +
//////////////             "dragon. Leave blank to use the classic in-place mount (soldier sits " +
//////////////             "on this dragon's own RiderSeat instead).")]
//////////////    [SerializeField] private GameObject riderVariantPrefab;

//////////////    [Tooltip("RIDER DRAGON: drag the PlainDragon prefab here.\n\n" +
//////////////             "When the soldier dismounts, this prefab is restored. " +
//////////////             "Leave blank on the plain dragon.")]
//////////////    [SerializeField] private GameObject plainVariantPrefab;

//////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

//////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////////    // ── Private ───────────────────────────────────────────────────────────────

//////////////    private DragonWingAnimator _wingAnimator;
//////////////    private RectTransform _rt;
//////////////    private Animator _anim;
//////////////    private CanvasGroup _cg;

//////////////    // Saved before every drag so we can snap back on an invalid drop.
//////////////    private Transform _savedParent;
//////////////    private Vector2 _savedAnchoredPos;
//////////////    private int _savedSiblingIndex;

//////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea).
//////////////    private FlyZone _currentZone;

//////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre.
//////////////    private Vector2 _dragOffset;

//////////////    // Patrol direction: +1 = right, -1 = left.
//////////////    private float _patrolDir = 1f;

//////////////    // True after TransferStateFrom() has already called EnterIdle/Flying,
//////////////    // so Start() does not override it with a second EnterIdle().
//////////////    private bool _stateTransferred;

//////////////    // ── State ─────────────────────────────────────────────────────────────────

//////////////    public enum DragonState { Idle, Dragging, Flying }
//////////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // LIFECYCLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void Awake()
//////////////    {
//////////////        _rt = GetComponent<RectTransform>();
//////////////        _anim = GetComponent<Animator>();
//////////////        _cg = GetComponent<CanvasGroup>();

//////////////        if (rootCanvas == null)
//////////////            rootCanvas = GetComponentInParent<Canvas>();

//////////////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);

//////////////        if (_wingAnimator == null)
//////////////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
//////////////                             "Add DragonWingAnimator to the DragonWing child.", this);
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        // Skip if TransferStateFrom() already put us in the correct state.
//////////////        if (!_stateTransferred)
//////////////            EnterIdle();
//////////////    }

//////////////    private void Update()
//////////////    {
//////////////        if (State == DragonState.Flying)
//////////////            DoPatrol();
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — BEGIN
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////    {
//////////////        // Block dragging the dragon while a soldier is riding it.
//////////////        var seat = GetComponentInChildren<DragonRiderSeat>();
//////////////        if (seat != null && seat.IsOccupied)
//////////////        {
//////////////            Debug.Log("[DragonController] Drag blocked — soldier is riding this dragon.");
//////////////            return;
//////////////        }

//////////////        // Snapshot position so we can snap back on an invalid drop.
//////////////        _savedParent = _rt.parent;
//////////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////////        // Move to canvas root so the dragon draws on top of all panels.
//////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////////        _rt.SetAsLastSibling();

//////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
//////////////        // in canvas space — prevents the dragon jumping on the first drag frame.
//////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null : rootCanvas.worldCamera;
//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCamBegin,
//////////////            out Vector2 pointerCanvasPos);
//////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////////////        // Semi-transparent while dragging; disable raycasts so zones are hit.
//////////////        _cg.alpha = 0.75f;
//////////////        _cg.blocksRaycasts = false;

//////////////        State = DragonState.Dragging;
//////////////        Debug.Log("[DragonController] OnBeginDrag");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — MOVE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnDrag(PointerEventData eventData)
//////////////    {
//////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null : rootCanvas.worldCamera;

//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCam,
//////////////            out Vector2 localPos);

//////////////        _rt.anchoredPosition = localPos + _dragOffset;
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — END  (zone detection + state transition)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnEndDrag(PointerEventData eventData)
//////////////    {
//////////////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast —
//////////////        // otherwise the dragon's own CanvasGroup would shadow the zone below it.
//////////////        _cg.alpha = 1f;

//////////////        var results = new List<RaycastResult>();
//////////////        EventSystem.current.RaycastAll(eventData, results);

//////////////        _cg.blocksRaycasts = true;

//////////////        FlyZone hitFlyZone = null;
//////////////        DragonEggSlot hitAreaSlot = null;

//////////////        foreach (var r in results)
//////////////        {
//////////////            // GetComponentInParent so hitting any child of the zone/area still counts.
//////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////////        }

//////////////        if (hitFlyZone != null)
//////////////        {
//////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////////            _rt.anchoredPosition = Vector2.zero;
//////////////            _currentZone = hitFlyZone;
//////////////            EnterFlying();
//////////////        }
//////////////        else if (hitAreaSlot != null)
//////////////        {
//////////////            // Reparent directly to the slot, not to _savedParent (which would be the
//////////////            // FlyZone when dragging from patrol, causing a wrong re-parent).
//////////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//////////////            _rt.anchoredPosition = Vector2.zero;
//////////////            _currentZone = null;
//////////////            EnterIdle();
//////////////        }
//////////////        else
//////////////        {
//////////////            SnapBack();
//////////////        }
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — IDLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterIdle()
//////////////    {
//////////////        State = DragonState.Idle;

//////////////        Vector3 s = transform.localScale;
//////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////////        transform.localScale = s;

//////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
//////////////        Debug.Log("[DragonController] → Idle");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — FLYING + PATROL
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterFlying()
//////////////    {
//////////////        State = DragonState.Flying;
//////////////        _patrolDir = -1f;

//////////////        Vector3 s = transform.localScale;
//////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////////        transform.localScale = s;

//////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
//////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//////////////    }

//////////////    private void DoPatrol()
//////////////    {
//////////////        if (_currentZone == null) return;

//////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////////        float currentX = _rt.anchoredPosition.x;
//////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//////////////        if (newX >= halfWidth)
//////////////        {
//////////////            newX = halfWidth;
//////////////            _patrolDir = -1f;
//////////////            FlipHorizontal();
//////////////        }
//////////////        else if (newX <= -halfWidth)
//////////////        {
//////////////            newX = -halfWidth;
//////////////            _patrolDir = 1f;
//////////////            FlipHorizontal();
//////////////        }

//////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // PREFAB SWAP — MOUNT  (called by SoldierDragDrop.OnEndDrag)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// <summary>
//////////////    /// Called by SoldierDragDrop when a soldier is dropped on this (plain) dragon.
//////////////    ///
//////////////    /// ── If riderVariantPrefab IS assigned (normal path) ───────────────────────
//////////////    ///   1. Instantiates the rider variant as a sibling at the same
//////////////    ///      parent, anchoredPosition, localScale, and sibling index.
//////////////    ///   2. Calls TransferStateFrom(this) on the new dragon so it
//////////////    ///      immediately continues the same patrol without a reset.
//////////////    ///   3. Mounts the soldier on the rider variant's DragonRiderSeat.
//////////////    ///   4. Deactivates this plain dragon (preserved for reuse on dismount).
//////////////    ///
//////////////    /// ── If riderVariantPrefab is NULL (fallback) ──────────────────────────────
//////////////    ///   Falls back to the original in-place behaviour: soldier is mounted on
//////////////    ///   this dragon's own DragonRiderSeat (classic system).
//////////////    ///
//////////////    /// CALL ORDER: SoldierDragDrop must save _mountHomeParent and _mountHomePos
//////////////    /// BEFORE calling this method.
//////////////    /// </summary>
//////////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat fallbackSeat)
//////////////    {
//////////////        if (riderVariantPrefab == null)
//////////////        {
//////////////            // No swap configured — classic in-place mount.
//////////////            fallbackSeat.MountSoldier(soldier);
//////////////            return;
//////////////        }

//////////////        // ── Spawn rider variant ───────────────────────────────────────────────
//////////////        var riderGO = Instantiate(riderVariantPrefab, transform.parent);
//////////////        var riderRT = riderGO.GetComponent<RectTransform>();
//////////////        riderRT.anchoredPosition = _rt.anchoredPosition;
//////////////        riderGO.transform.localScale = transform.localScale;
//////////////        riderGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

//////////////        // ── Transfer patrol state ─────────────────────────────────────────────
//////////////        var riderDC = riderGO.GetComponent<DragonController>();
//////////////        if (riderDC != null)
//////////////            riderDC.TransferStateFrom(this);

//////////////        // ── Mount soldier on the rider variant's seat ─────────────────────────
//////////////        var riderSeat = riderGO.GetComponentInChildren<DragonRiderSeat>();
//////////////        if (riderSeat != null)
//////////////        {
//////////////            riderSeat.MountSoldier(soldier);
//////////////        }
//////////////        else
//////////////        {
//////////////            Debug.LogError("[DragonController] Rider variant prefab has no DragonRiderSeat " +
//////////////                           "child! Add a DragonRiderSeat child to the rider dragon prefab.", riderGO);
//////////////        }

//////////////        // ── Hide plain dragon (keep alive for potential pool reuse) ───────────
//////////////        gameObject.SetActive(false);
//////////////        Debug.Log($"[DragonController] '{name}' swapped → rider variant for '{soldier.name}'.");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // PREFAB SWAP — DISMOUNT  (called by SoldierDragDrop after soldier leaves)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// <summary>
//////////////    /// Called by SoldierDragDrop when the soldier leaves this (rider) dragon.
//////////////    ///
//////////////    /// IMPORTANT — call this ONLY after the soldier has been reparented away
//////////////    /// from the seat (e.g. to the root canvas or to their ground home). If
//////////////    /// called while the soldier is still a child of this dragon, the soldier
//////////////    /// will be destroyed along with this GameObject.
//////////////    ///
//////////////    /// ── If plainVariantPrefab IS assigned (normal path) ───────────────────────
//////////////    ///   1. Instantiates the plain dragon at the same parent, position, scale.
//////////////    ///   2. Calls TransferStateFrom(this) so patrol resumes seamlessly.
//////////////    ///   3. Destroys this rider dragon.
//////////////    ///
//////////////    /// ── If plainVariantPrefab is NULL ─────────────────────────────────────────
//////////////    ///   Logs a warning and does nothing — set it in the rider dragon's Inspector.
//////////////    /// </summary>
//////////////    public void PerformDismount()
//////////////    {
//////////////        if (plainVariantPrefab == null)
//////////////        {
//////////////            Debug.LogWarning("[DragonController] PerformDismount: plainVariantPrefab is not " +
//////////////                             "set on this rider variant. Assign it in the Inspector.", this);
//////////////            return;
//////////////        }

//////////////        // ── Spawn plain dragon ────────────────────────────────────────────────
//////////////        var plainGO = Instantiate(plainVariantPrefab, transform.parent);
//////////////        var plainRT = plainGO.GetComponent<RectTransform>();
//////////////        plainRT.anchoredPosition = _rt.anchoredPosition;
//////////////        plainGO.transform.localScale = transform.localScale;
//////////////        plainGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

//////////////        // ── Transfer patrol state ─────────────────────────────────────────────
//////////////        var plainDC = plainGO.GetComponent<DragonController>();
//////////////        if (plainDC != null)
//////////////            plainDC.TransferStateFrom(this);

//////////////        // ── Remove rider dragon ───────────────────────────────────────────────
//////////////        Debug.Log($"[DragonController] '{name}' swapped → plain variant.");
//////////////        Destroy(gameObject);
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE TRANSFER  (shared by PerformMount and PerformDismount)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// <summary>
//////////////    /// Copies all patrol state from <paramref name="source"/> and immediately
//////////////    /// enters the matching animation state (Idle or Flying).
//////////////    ///
//////////////    /// Sets _stateTransferred = true so Start() does not override the state
//////////////    /// with its own EnterIdle() call on the next frame.
//////////////    ///
//////////////    /// Called on the newly spawned dragon immediately after Instantiate(),
//////////////    /// before Start() has fired, so the one-frame lag is avoided entirely.
//////////////    /// </summary>
//////////////    public void TransferStateFrom(DragonController source)
//////////////    {
//////////////        _stateTransferred = true;

//////////////        homeSlot = source.homeSlot;
//////////////        _currentZone = source._currentZone;
//////////////        _patrolDir = source._patrolDir;

//////////////        // Sync position and scale — the caller sets these too, but doing it
//////////////        // here as well guards against any future call-order changes.
//////////////        _rt.anchoredPosition = source._rt.anchoredPosition;
//////////////        transform.localScale = source.transform.localScale;

//////////////        if (source.State == DragonState.Flying)
//////////////        {
//////////////            State = DragonState.Flying;
//////////////            _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
//////////////            TriggerAnim(dragonData?.dragonFlyTrigger);
//////////////        }
//////////////        else
//////////////        {
//////////////            EnterIdle();
//////////////        }

//////////////        Debug.Log($"[DragonController] '{name}' received state from '{source.name}' " +
//////////////                  $"(State={source.State}, Zone={source._currentZone?.name ?? "none"}, " +
//////////////                  $"Dir={source._patrolDir}).");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // HELPERS
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// <summary>Flip the sprite by negating localScale.x.</summary>
//////////////    private void FlipHorizontal()
//////////////    {
//////////////        Vector3 s = transform.localScale;
//////////////        s.x = -s.x;
//////////////        transform.localScale = s;
//////////////    }

//////////////    /// <summary>Restore the RectTransform to its pre-drag parent, position, and depth.</summary>
//////////////    private void ReturnToHome()
//////////////    {
//////////////        if (_savedParent == null) return;
//////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////////    }

//////////////    /// <summary>Invalid drop: put the dragon back where it was and resume its old state.</summary>
//////////////    private void SnapBack()
//////////////    {
//////////////        ReturnToHome();

//////////////        if (_currentZone != null)
//////////////        {
//////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////////            State = DragonState.Flying;
//////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
//////////////        }
//////////////        else
//////////////        {
//////////////            EnterIdle();
//////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
//////////////        }
//////////////    }

//////////////    /// <summary>Fire an Animator trigger by name with warnings for common misconfigurations.</summary>
//////////////    private void TriggerAnim(string trigger)
//////////////    {
//////////////        if (_anim == null)
//////////////        {
//////////////            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
//////////////            return;
//////////////        }
//////////////        if (dragonData == null)
//////////////        {
//////////////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
//////////////                             "Drag your DragonData ScriptableObject into the Inspector.", this);
//////////////            return;
//////////////        }
//////////////        if (string.IsNullOrEmpty(trigger))
//////////////        {
//////////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
//////////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
//////////////            return;
//////////////        }
//////////////        _anim.SetTrigger(trigger);
//////////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
//////////////    }
//////////////}


//////////////////using System.Collections.Generic;
//////////////////using UnityEngine;
//////////////////using UnityEngine.EventSystems;
//////////////////using UnityEngine.UI;

///////////////////// <summary>
///////////////////// DRAGON CONTROLLER
/////////////////////
///////////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
///////////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////////////////
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////  STATES
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////
/////////////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////////////////            It can be picked up and dragged.
/////////////////////
/////////////////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////////////////            semi-transparent, raycasts pass through it.
/////////////////////
/////////////////////  Flying    Dragon was dropped on a FlyZone.
/////////////////////            It plays the fly animation and patrols left↔right
/////////////////////            inside the zone, flipping its sprite at each edge.
/////////////////////
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////  DROP RULES
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////
/////////////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
/////////////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
/////////////////////  Drop anywhere else   → SnapBack     (return to previous state)
/////////////////////
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////  SETUP
///////////////////// ════════════════════════════════════════════════════════════════════
/////////////////////
/////////////////////  1. Add this script to your dragon prefab.
/////////////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
/////////////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
/////////////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
/////////////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
/////////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////////////        if (dc != null) dc.homeSlot = this;
///////////////////// </summary>
//////////////////[RequireComponent(typeof(RectTransform))]
//////////////////[RequireComponent(typeof(CanvasGroup))]
//////////////////public class DragonController : MonoBehaviour,
//////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////////{
//////////////////    // ── Inspector ──────────────────────────────────────────────────────────────
//////////////////    [Header("Dragon Data")]
//////////////////    [SerializeField] private DragonData dragonData;

//////////////////    [Header("Canvas — auto-found if blank")]
//////////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////////////    [SerializeField] private Canvas rootCanvas;

//////////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
//////////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////////////    // ── Private ───────────────────────────────────────────────────────────────
//////////////////    private RectTransform _rt;
//////////////////    private Animator _anim;
//////////////////    private CanvasGroup _cg;

//////////////////    // Saved before every drag so we can snap back on an invalid drop
//////////////////    private Transform _savedParent;
//////////////////    private Vector2 _savedAnchoredPos;
//////////////////    private int _savedSiblingIndex;

//////////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
//////////////////    private FlyZone _currentZone;

//////////////////    // Patrol bookkeeping
//////////////////    private float _patrolDir = 1f;   // +1 right, -1 left

//////////////////    // ── State ─────────────────────────────────────────────────────────────────
//////////////////    public enum DragonState { Idle, Dragging, Flying }
//////////////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // LIFECYCLE
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    private void Awake()
//////////////////    {
//////////////////        _rt = GetComponent<RectTransform>();
//////////////////        _anim = GetComponent<Animator>();
//////////////////        _cg = GetComponent<CanvasGroup>();

//////////////////        if (rootCanvas == null)
//////////////////            rootCanvas = GetComponentInParent<Canvas>();
//////////////////    }

//////////////////    private void Start()
//////////////////    {
//////////////////        EnterIdle();
//////////////////    }

//////////////////    private void Update()
//////////////////    {
//////////////////        if (State == DragonState.Flying)
//////////////////            DoPatrol();
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // DRAG — BEGIN
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////////    {
//////////////////        // Snapshot current position so we can snap back if the drop is invalid
//////////////////        _savedParent = _rt.parent;
//////////////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////////////        // Move to canvas root so the dragon draws on top of all panels
//////////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////////////        _rt.SetAsLastSibling();

//////////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
//////////////////        _cg.alpha = 0.75f;
//////////////////        _cg.blocksRaycasts = false;

//////////////////        State = DragonState.Dragging;

//////////////////        Debug.Log("[DragonController] OnBeginDrag");
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // DRAG — MOVE
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    public void OnDrag(PointerEventData eventData)
//////////////////    {
//////////////////        // Convert screen-space pointer to canvas-local position
//////////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////////            ? null
//////////////////            : rootCanvas.worldCamera;

//////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////////            rootCanvas.transform as RectTransform,
//////////////////            eventData.position,
//////////////////            uiCam,
//////////////////            out Vector2 localPos);

//////////////////        _rt.anchoredPosition = localPos;
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // DRAG — END  (zone detection + state transition)
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    public void OnEndDrag(PointerEventData eventData)
//////////////////    {
//////////////////        // Restore full opacity and raycast blocking
//////////////////        _cg.alpha = 1f;
//////////////////        _cg.blocksRaycasts = true;

//////////////////        // ── Raycast everything under the pointer ──────────────────────────────
//////////////////        var results = new List<RaycastResult>();
//////////////////        EventSystem.current.RaycastAll(eventData, results);

//////////////////        FlyZone hitFlyZone = null;
//////////////////        DragonEggSlot hitAreaSlot = null;

//////////////////        foreach (var r in results)
//////////////////        {
//////////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
//////////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
//////////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////////////        }

//////////////////        // ── Decide destination ────────────────────────────────────────────────
//////////////////        if (hitFlyZone != null)
//////////////////        {
//////////////////            // Dropped onto a Fly Zone → start flying + patrol
//////////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
//////////////////            _currentZone = hitFlyZone;
//////////////////            EnterFlying();
//////////////////        }
//////////////////        else if (hitAreaSlot != null)
//////////////////        {
//////////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
//////////////////            ReturnToHome();
//////////////////            _currentZone = null;
//////////////////            EnterIdle();
//////////////////        }
//////////////////        else
//////////////////        {
//////////////////            // Invalid drop → snap back to wherever it was before the drag
//////////////////            SnapBack();
//////////////////        }
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // STATE — IDLE
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    private void EnterIdle()
//////////////////    {
//////////////////        State = DragonState.Idle;
//////////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////////////        Debug.Log("[DragonController] → Idle");
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // STATE — FLYING + PATROL
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    private void EnterFlying()
//////////////////    {
//////////////////        State = DragonState.Flying;
//////////////////        _patrolDir = 1f;   // always start moving right

//////////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//////////////////    }

//////////////////    private void DoPatrol()
//////////////////    {
//////////////////        if (_currentZone == null) return;

//////////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////////////        float currentX = _rt.anchoredPosition.x;
//////////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//////////////////        // Bounce at patrol edges
//////////////////        if (newX >= halfWidth)
//////////////////        {
//////////////////            newX = halfWidth;
//////////////////            _patrolDir = -1f;
//////////////////            FlipHorizontal();
//////////////////        }
//////////////////        else if (newX <= -halfWidth)
//////////////////        {
//////////////////            newX = -halfWidth;
//////////////////            _patrolDir = 1f;
//////////////////            FlipHorizontal();
//////////////////        }

//////////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////////////    }

//////////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////////    // HELPERS
//////////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
//////////////////    private void FlipHorizontal()
//////////////////    {
//////////////////        Vector3 s = transform.localScale;
//////////////////        s.x = -s.x;
//////////////////        transform.localScale = s;
//////////////////    }

//////////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
//////////////////    private void ReturnToHome()
//////////////////    {
//////////////////        if (_savedParent == null) return;
//////////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////////////    }

//////////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
//////////////////    private void SnapBack()
//////////////////    {
//////////////////        ReturnToHome();

//////////////////        // Resume previous state without re-triggering animations
//////////////////        if (_currentZone != null)
//////////////////        {
//////////////////            // Was flying before the drag — re-parent to the zone and resume
//////////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
//////////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
//////////////////        }
//////////////////        else
//////////////////        {
//////////////////            EnterIdle();
//////////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
//////////////////        }
//////////////////    }

//////////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
//////////////////    private void TriggerAnim(string trigger)
//////////////////    {
//////////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
//////////////////        _anim.SetTrigger(trigger);
//////////////////    }
//////////////////}


////////////////using System.Collections.Generic;
////////////////using UnityEngine;
////////////////using UnityEngine.EventSystems;
////////////////using UnityEngine.UI;

/////////////////// <summary>
/////////////////// DRAGON CONTROLLER
///////////////////
/////////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
/////////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  STATES
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////////////            It can be picked up and dragged.
///////////////////
///////////////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////////////            semi-transparent, raycasts pass through it.
///////////////////
///////////////////  Flying    Dragon was dropped on a FlyZone.
///////////////////            It plays the fly animation and patrols left↔right
///////////////////            inside the zone, flipping its sprite at each edge.
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  DROP RULES
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
///////////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
///////////////////  Drop anywhere else   → SnapBack     (return to previous state)
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  SETUP
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  1. Add this script to your dragon prefab.
///////////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
///////////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
///////////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
///////////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
///////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
///////////////////        if (dc != null) dc.homeSlot = this;
/////////////////// </summary>
////////////////[RequireComponent(typeof(RectTransform))]
////////////////[RequireComponent(typeof(CanvasGroup))]
////////////////public class DragonController : MonoBehaviour,
////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////////{
////////////////    // ── Inspector ──────────────────────────────────────────────────────────────
////////////////    [Header("Dragon Data")]
////////////////    [SerializeField] private DragonData dragonData;

////////////////    [Header("Canvas — auto-found if blank")]
////////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////////////    [SerializeField] private Canvas rootCanvas;

////////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
////////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////////////    [HideInInspector] public DragonEggSlot homeSlot;

////////////////    // ── Private ───────────────────────────────────────────────────────────────
////////////////    private RectTransform _rt;
////////////////    private Animator _anim;
////////////////    private CanvasGroup _cg;

////////////////    // Saved before every drag so we can snap back on an invalid drop
////////////////    private Transform _savedParent;
////////////////    private Vector2 _savedAnchoredPos;
////////////////    private int _savedSiblingIndex;

////////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
////////////////    private FlyZone _currentZone;

////////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
////////////////    private Vector2 _dragOffset;

////////////////    // Patrol bookkeeping
////////////////    private float _patrolDir = 1f;   // +1 right, -1 left

////////////////    // ── State ─────────────────────────────────────────────────────────────────
////////////////    public enum DragonState { Idle, Dragging, Flying }
////////////////    public DragonState State { get; private set; } = DragonState.Idle;

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // LIFECYCLE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void Awake()
////////////////    {
////////////////        _rt = GetComponent<RectTransform>();
////////////////        _anim = GetComponent<Animator>();
////////////////        _cg = GetComponent<CanvasGroup>();

////////////////        if (rootCanvas == null)
////////////////            rootCanvas = GetComponentInParent<Canvas>();
////////////////    }

////////////////    private void Start()
////////////////    {
////////////////        EnterIdle();
////////////////    }

////////////////    private void Update()
////////////////    {
////////////////        if (State == DragonState.Flying)
////////////////            DoPatrol();
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — BEGIN
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnBeginDrag(PointerEventData eventData)
////////////////    {
////////////////        // Snapshot current position so we can snap back if the drop is invalid
////////////////        _savedParent = _rt.parent;
////////////////        _savedAnchoredPos = _rt.anchoredPosition;
////////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////////////        // Move to canvas root so the dragon draws on top of all panels
////////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////////////        _rt.SetAsLastSibling();

////////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
////////////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
////////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////            ? null : rootCanvas.worldCamera;
////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////            rootCanvas.transform as RectTransform,
////////////////            eventData.position,
////////////////            uiCamBegin,
////////////////            out Vector2 pointerCanvasPos);
////////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
////////////////        _cg.alpha = 0.75f;
////////////////        _cg.blocksRaycasts = false;

////////////////        State = DragonState.Dragging;

////////////////        Debug.Log("[DragonController] OnBeginDrag");
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — MOVE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnDrag(PointerEventData eventData)
////////////////    {
////////////////        // Convert screen-space pointer to canvas-local position
////////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////            ? null
////////////////            : rootCanvas.worldCamera;

////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////            rootCanvas.transform as RectTransform,
////////////////            eventData.position,
////////////////            uiCam,
////////////////            out Vector2 localPos);

////////////////        _rt.anchoredPosition = localPos + _dragOffset;
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — END  (zone detection + state transition)
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnEndDrag(PointerEventData eventData)
////////////////    {
////////////////        // Restore full opacity and raycast blocking
////////////////        _cg.alpha = 1f;
////////////////        _cg.blocksRaycasts = true;

////////////////        // ── Raycast everything under the pointer ──────────────────────────────
////////////////        var results = new List<RaycastResult>();
////////////////        EventSystem.current.RaycastAll(eventData, results);

////////////////        FlyZone hitFlyZone = null;
////////////////        DragonEggSlot hitAreaSlot = null;

////////////////        foreach (var r in results)
////////////////        {
////////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
////////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
////////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////////////        }

////////////////        // ── Decide destination ────────────────────────────────────────────────
////////////////        if (hitFlyZone != null)
////////////////        {
////////////////            // Dropped onto a Fly Zone → start flying + patrol
////////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
////////////////            _currentZone = hitFlyZone;
////////////////            EnterFlying();
////////////////        }
////////////////        else if (hitAreaSlot != null)
////////////////        {
////////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
////////////////            ReturnToHome();
////////////////            _currentZone = null;
////////////////            EnterIdle();
////////////////        }
////////////////        else
////////////////        {
////////////////            // Invalid drop → snap back to wherever it was before the drag
////////////////            SnapBack();
////////////////        }
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // STATE — IDLE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void EnterIdle()
////////////////    {
////////////////        State = DragonState.Idle;
////////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////////////        Debug.Log("[DragonController] → Idle");
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // STATE — FLYING + PATROL
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void EnterFlying()
////////////////    {
////////////////        State = DragonState.Flying;
////////////////        _patrolDir = 1f;   // always start moving right

////////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
////////////////    }

////////////////    private void DoPatrol()
////////////////    {
////////////////        if (_currentZone == null) return;

////////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////////////        float currentX = _rt.anchoredPosition.x;
////////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

////////////////        // Bounce at patrol edges
////////////////        if (newX >= halfWidth)
////////////////        {
////////////////            newX = halfWidth;
////////////////            _patrolDir = -1f;
////////////////            FlipHorizontal();
////////////////        }
////////////////        else if (newX <= -halfWidth)
////////////////        {
////////////////            newX = -halfWidth;
////////////////            _patrolDir = 1f;
////////////////            FlipHorizontal();
////////////////        }

////////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // HELPERS
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
////////////////    private void FlipHorizontal()
////////////////    {
////////////////        Vector3 s = transform.localScale;
////////////////        s.x = -s.x;
////////////////        transform.localScale = s;
////////////////    }

////////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
////////////////    private void ReturnToHome()
////////////////    {
////////////////        if (_savedParent == null) return;
////////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////////////        _rt.anchoredPosition = _savedAnchoredPos;
////////////////    }

////////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
////////////////    private void SnapBack()
////////////////    {
////////////////        ReturnToHome();

////////////////        // Resume previous state without re-triggering animations
////////////////        if (_currentZone != null)
////////////////        {
////////////////            // Was flying before the drag — re-parent to the zone and resume
////////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
////////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
////////////////        }
////////////////        else
////////////////        {
////////////////            EnterIdle();
////////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
////////////////        }
////////////////    }

////////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
////////////////    private void TriggerAnim(string trigger)
////////////////    {
////////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
////////////////        _anim.SetTrigger(trigger);
////////////////    }
////////////////}


//////////////using System.Collections.Generic;
//////////////using UnityEngine;
//////////////using UnityEngine.EventSystems;
//////////////using UnityEngine.UI;

///////////////// <summary>
///////////////// DRAGON CONTROLLER
/////////////////
///////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
///////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  STATES
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////////////            It can be picked up and dragged.
/////////////////
/////////////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////////////            semi-transparent, raycasts pass through it.
/////////////////
/////////////////  Flying    Dragon was dropped on a FlyZone.
/////////////////            It plays the fly animation and patrols left↔right
/////////////////            inside the zone, flipping its sprite at each edge.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  DROP RULES
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
/////////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
/////////////////  Drop anywhere else   → SnapBack     (return to previous state)
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  SETUP
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  1. Add this script to your dragon prefab.
/////////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
/////////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
/////////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
/////////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
/////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////////        if (dc != null) dc.homeSlot = this;
///////////////// </summary>
//////////////[RequireComponent(typeof(RectTransform))]
//////////////[RequireComponent(typeof(CanvasGroup))]
//////////////public class DragonController : MonoBehaviour,
//////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////{
//////////////    // ── Inspector ──────────────────────────────────────────────────────────────
//////////////    [Header("Dragon Data")]
//////////////    [SerializeField] private DragonData dragonData;

//////////////    [Header("Canvas — auto-found if blank")]
//////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////////    [SerializeField] private Canvas rootCanvas;

//////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
//////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////////    // ── Private ───────────────────────────────────────────────────────────────
//////////////    private RectTransform _rt;
//////////////    private Animator _anim;
//////////////    private CanvasGroup _cg;

//////////////    // Saved before every drag so we can snap back on an invalid drop
//////////////    private Transform _savedParent;
//////////////    private Vector2 _savedAnchoredPos;
//////////////    private int _savedSiblingIndex;

//////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
//////////////    private FlyZone _currentZone;

//////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
//////////////    private Vector2 _dragOffset;

//////////////    // Patrol bookkeeping
//////////////    private float _patrolDir = 1f;   // +1 right, -1 left

//////////////    // ── State ─────────────────────────────────────────────────────────────────
//////////////    public enum DragonState { Idle, Dragging, Flying }
//////////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // LIFECYCLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void Awake()
//////////////    {
//////////////        _rt = GetComponent<RectTransform>();
//////////////        _anim = GetComponent<Animator>();
//////////////        _cg = GetComponent<CanvasGroup>();

//////////////        if (rootCanvas == null)
//////////////            rootCanvas = GetComponentInParent<Canvas>();
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        EnterIdle();
//////////////    }

//////////////    private void Update()
//////////////    {
//////////////        if (State == DragonState.Flying)
//////////////            DoPatrol();
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — BEGIN
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////    {
//////////////        // Snapshot current position so we can snap back if the drop is invalid
//////////////        _savedParent = _rt.parent;
//////////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////////        // Move to canvas root so the dragon draws on top of all panels
//////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////////        _rt.SetAsLastSibling();

//////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
//////////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
//////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null : rootCanvas.worldCamera;
//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCamBegin,
//////////////            out Vector2 pointerCanvasPos);
//////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
//////////////        _cg.alpha = 0.75f;
//////////////        _cg.blocksRaycasts = false;

//////////////        State = DragonState.Dragging;

//////////////        Debug.Log("[DragonController] OnBeginDrag");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — MOVE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnDrag(PointerEventData eventData)
//////////////    {
//////////////        // Convert screen-space pointer to canvas-local position
//////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null
//////////////            : rootCanvas.worldCamera;

//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCam,
//////////////            out Vector2 localPos);

//////////////        _rt.anchoredPosition = localPos + _dragOffset;
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — END  (zone detection + state transition)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnEndDrag(PointerEventData eventData)
//////////////    {
//////////////        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
//////////////        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
//////////////        // the FlyZone underneath is never detected.
//////////////        _cg.alpha = 1f;

//////////////        // ── Raycast everything under the pointer ──────────────────────────────
//////////////        var results = new List<RaycastResult>();
//////////////        EventSystem.current.RaycastAll(eventData, results);

//////////////        // Now safe to restore — raycast is already done
//////////////        _cg.blocksRaycasts = true;

//////////////        FlyZone hitFlyZone = null;
//////////////        DragonEggSlot hitAreaSlot = null;

//////////////        foreach (var r in results)
//////////////        {
//////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
//////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
//////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////////        }

//////////////        // ── Decide destination ────────────────────────────────────────────────
//////////////        if (hitFlyZone != null)
//////////////        {
//////////////            // Dropped onto a Fly Zone → start flying + patrol
//////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
//////////////            _currentZone = hitFlyZone;
//////////////            EnterFlying();
//////////////        }
//////////////        else if (hitAreaSlot != null)
//////////////        {
//////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
//////////////            ReturnToHome();
//////////////            _currentZone = null;
//////////////            EnterIdle();
//////////////        }
//////////////        else
//////////////        {
//////////////            // Invalid drop → snap back to wherever it was before the drag
//////////////            SnapBack();
//////////////        }
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — IDLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterIdle()
//////////////    {
//////////////        State = DragonState.Idle;
//////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////////        Debug.Log("[DragonController] → Idle");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — FLYING + PATROL
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterFlying()
//////////////    {
//////////////        State = DragonState.Flying;
//////////////        _patrolDir = 1f;   // start moving right

//////////////        // Ensure localScale.x is positive so the sprite faces right,
//////////////        // matching _patrolDir. Without this, a leftward flip from a
//////////////        // previous patrol session carries over and the dragon faces the
//////////////        // wrong way on entry.
//////////////        Vector3 s = transform.localScale;
//////////////        s.x = Mathf.Abs(s.x);
//////////////        transform.localScale = s;

//////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//////////////    }

//////////////    private void DoPatrol()
//////////////    {
//////////////        if (_currentZone == null) return;

//////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////////        float currentX = _rt.anchoredPosition.x;
//////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//////////////        // Bounce at patrol edges
//////////////        if (newX >= halfWidth)
//////////////        {
//////////////            newX = halfWidth;
//////////////            _patrolDir = -1f;
//////////////            FlipHorizontal();
//////////////        }
//////////////        else if (newX <= -halfWidth)
//////////////        {
//////////////            newX = -halfWidth;
//////////////            _patrolDir = 1f;
//////////////            FlipHorizontal();
//////////////        }

//////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // HELPERS
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
//////////////    private void FlipHorizontal()
//////////////    {
//////////////        Vector3 s = transform.localScale;
//////////////        //s.x = -s.x;
//////////////        transform.localScale = s;
//////////////    }

//////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
//////////////    private void ReturnToHome()
//////////////    {
//////////////        if (_savedParent == null) return;
//////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////////    }

//////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
//////////////    private void SnapBack()
//////////////    {
//////////////        ReturnToHome();

//////////////        // Resume previous state without re-triggering animations
//////////////        if (_currentZone != null)
//////////////        {
//////////////            // Was flying before the drag — re-parent to the zone and resume
//////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
//////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
//////////////        }
//////////////        else
//////////////        {
//////////////            EnterIdle();
//////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
//////////////        }
//////////////    }

//////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
//////////////    private void TriggerAnim(string trigger)
//////////////    {
//////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
//////////////        _anim.SetTrigger(trigger);
//////////////    }
//////////////}

////////////using System.Collections.Generic;
////////////using UnityEngine;
////////////using UnityEngine.EventSystems;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// DRAGON CONTROLLER
///////////////
/////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
/////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  STATES
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////////            It can be picked up and dragged.
///////////////
///////////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////////            semi-transparent, raycasts pass through it.
///////////////
///////////////  Flying    Dragon was dropped on a FlyZone.
///////////////            It plays the fly animation and patrols left↔right
///////////////            inside the zone, flipping its sprite at each edge.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  DROP RULES
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
///////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
///////////////  Drop anywhere else   → SnapBack     (return to previous state)
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  SETUP
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  1. Add this script to your dragon prefab.
///////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
///////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
///////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
///////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
///////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
///////////////        if (dc != null) dc.homeSlot = this;
/////////////// </summary>
////////////[RequireComponent(typeof(RectTransform))]
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class DragonController : MonoBehaviour,
////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////{
////////////    // ── Inspector ──────────────────────────────────────────────────────────────
////////////    [Header("Dragon Data")]
////////////    [SerializeField] private DragonData dragonData;

////////////    [Header("Canvas — auto-found if blank")]
////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////////    [SerializeField] private Canvas rootCanvas;

////////////    [Header("Sprite Orientation")]
////////////    [Tooltip("Tick this if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
////////////             "The controller flips the scale to match the patrol direction.")]
////////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////////////    [Header("Prefab Swap — Dragon Mount")]
////////////    //    [Tooltip("The RIDER variant of this dragon (has DragonLayeredVisual, RiderSeat, DragonWing).
////////////    //" +
////////////    //             "Assign on the PLAIN dragon prefab.
////////////    //" +
////////////    //             "When a soldier is dropped onto the plain dragon, it is destroyed and this prefab
////////////    //" +
////////////    //             "is spawned in its place with the soldier already seated.")]
////////////    //    [SerializeField] private GameObject riderDragonPrefab;

////////////    //    [Tooltip("The PLAIN variant of this dragon (just Image + DragonController + Animator).
////////////    //" +
////////////    //             "Assign on the RIDER dragon prefab.
////////////    //" +
////////////    //             "When the soldier dismounts, the rider dragon is destroyed and this prefab
////////////    //" +
////////////    //             "is spawned in its place so the dragon continues patrolling alone.")]
////////////    [Tooltip("The RIDER variant of this dragon (has DragonLayeredVisual, RiderSeat, DragonWing).\n" +
////////////         "Assign on the PLAIN dragon prefab.\n" +
////////////         "When a soldier is dropped onto the plain dragon, it is destroyed and this prefab\n" +
////////////         "is spawned in its place with the soldier already seated.")]
////////////    [SerializeField] private GameObject riderDragonPrefab;

////////////    [Tooltip("The PLAIN variant of this dragon (just Image + DragonController + Animator).\n" +
////////////             "Assign on the RIDER dragon prefab.\n" +
////////////             "When the soldier dismounts, the rider dragon is destroyed and this prefab\n" +
////////////             "is spawned in its place so the dragon continues patrolling alone.")]

////////////    [SerializeField] private GameObject plainDragonPrefab;

////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////////    [HideInInspector] public DragonEggSlot homeSlot;

////////////    // ── Private ───────────────────────────────────────────────────────────────
////////////    private RectTransform _rt;
////////////    private Animator _anim;
////////////    private CanvasGroup _cg;

////////////    // Saved before every drag so we can snap back on an invalid drop
////////////    private Transform _savedParent;
////////////    private Vector2 _savedAnchoredPos;
////////////    private int _savedSiblingIndex;

////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
////////////    private FlyZone _currentZone;

////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
////////////    private Vector2 _dragOffset;

////////////    // Patrol bookkeeping
////////////    private float _patrolDir = 1f;   // +1 right, -1 left

////////////    // Prefab-swap guard — set by ForceEnterFlying/Idle so that Start() does
////////////    // not call EnterIdle() and override the state that PerformMount applied.
////////////    private bool _stateInitialised;

////////////    // ── State ─────────────────────────────────────────────────────────────────
////////////    public enum DragonState { Idle, Dragging, Flying }
////////////    public DragonState State { get; private set; } = DragonState.Idle;

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // LIFECYCLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void Awake()
////////////    {
////////////        _rt = GetComponent<RectTransform>();
////////////        _anim = GetComponent<Animator>();
////////////        _cg = GetComponent<CanvasGroup>();

////////////        if (rootCanvas == null)
////////////            rootCanvas = GetComponentInParent<Canvas>();
////////////    }

////////////    private void Start()
////////////    {
////////////        // Skip if ForceEnterFlying/Idle already set state during a prefab swap.
////////////        // Without this guard, Start() would call EnterIdle() one frame after
////////////        // PerformMount() called ForceEnterFlying(), cancelling the flying state.
////////////        if (!_stateInitialised)
////////////            EnterIdle();
////////////    }

////////////    private void Update()
////////////    {
////////////        if (State == DragonState.Flying)
////////////            DoPatrol();
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — BEGIN
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnBeginDrag(PointerEventData eventData)
////////////    {
////////////        // Snapshot current position so we can snap back if the drop is invalid
////////////        _savedParent = _rt.parent;
////////////        _savedAnchoredPos = _rt.anchoredPosition;
////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////////        // Move to canvas root so the dragon draws on top of all panels
////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////////        _rt.SetAsLastSibling();

////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
////////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null : rootCanvas.worldCamera;
////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCamBegin,
////////////            out Vector2 pointerCanvasPos);
////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
////////////        _cg.alpha = 0.75f;
////////////        _cg.blocksRaycasts = false;

////////////        State = DragonState.Dragging;

////////////        Debug.Log("[DragonController] OnBeginDrag");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — MOVE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnDrag(PointerEventData eventData)
////////////    {
////////////        // Convert screen-space pointer to canvas-local position
////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null
////////////            : rootCanvas.worldCamera;

////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCam,
////////////            out Vector2 localPos);

////////////        _rt.anchoredPosition = localPos + _dragOffset;
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — END  (zone detection + state transition)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnEndDrag(PointerEventData eventData)
////////////    {
////////////        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
////////////        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
////////////        // the FlyZone underneath is never detected.
////////////        _cg.alpha = 1f;

////////////        // ── Raycast everything under the pointer ──────────────────────────────
////////////        var results = new List<RaycastResult>();
////////////        EventSystem.current.RaycastAll(eventData, results);

////////////        // Now safe to restore — raycast is already done
////////////        _cg.blocksRaycasts = true;

////////////        FlyZone hitFlyZone = null;
////////////        DragonEggSlot hitAreaSlot = null;

////////////        foreach (var r in results)
////////////        {
////////////            // GetComponentInParent so hitting any child of the zone/area still counts
////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////////        }

////////////        // ── Decide destination ────────────────────────────────────────────────
////////////        if (hitFlyZone != null)
////////////        {
////////////            // Dropped onto a Fly Zone → start flying + patrol
////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
////////////            _currentZone = hitFlyZone;
////////////            EnterFlying();
////////////        }
////////////        else if (hitAreaSlot != null)
////////////        {
////////////            // Dropped onto DragonArea → reparent directly to the slot, not to
////////////            // _savedParent (which would be the FlyZone when dragging from patrol).
////////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////////////            _rt.anchoredPosition = Vector2.zero;
////////////            _currentZone = null;
////////////            EnterIdle();
////////////        }
////////////        else
////////////        {
////////////            // Invalid drop → snap back to wherever it was before the drag
////////////            SnapBack();
////////////        }
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — IDLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterIdle()
////////////    {
////////////        State = DragonState.Idle;

////////////        // Reset to natural facing direction so patrol flips don't carry over to idle.
////////////        //   spriteDefaultFacesLeft = true  → restore positive scale (faces left)
////////////        //   spriteDefaultFacesLeft = false → restore negative scale (faces right)
////////////        Vector3 s = transform.localScale;
////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////////        transform.localScale = s;

////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////////        Debug.Log("[DragonController] → Idle");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — FLYING + PATROL
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterFlying()
////////////    {
////////////        State = DragonState.Flying;
////////////        _patrolDir = -1f;  // start moving left

////////////        // Set localScale.x so the sprite FACES LEFT on entry (matching patrolDir -1).
////////////        //   spriteDefaultFacesLeft = true  → positive scale = faces left  → Abs (natural)
////////////        //   spriteDefaultFacesLeft = false → positive scale = faces right → negate to face left
////////////        Vector3 s = transform.localScale;
////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////////        transform.localScale = s;

////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
////////////    }

////////////    private void DoPatrol()
////////////    {
////////////        if (_currentZone == null) return;

////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////////        float currentX = _rt.anchoredPosition.x;
////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

////////////        // Bounce at patrol edges
////////////        if (newX >= halfWidth)
////////////        {
////////////            newX = halfWidth;
////////////            _patrolDir = -1f;
////////////            FlipHorizontal();
////////////        }
////////////        else if (newX <= -halfWidth)
////////////        {
////////////            newX = -halfWidth;
////////////            _patrolDir = 1f;
////////////            FlipHorizontal();
////////////        }

////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // HELPERS
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
////////////    private void FlipHorizontal()
////////////    {
////////////        Vector3 s = transform.localScale;
////////////        s.x = -s.x;
////////////        transform.localScale = s;
////////////    }

////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
////////////    private void ReturnToHome()
////////////    {
////////////        if (_savedParent == null) return;
////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////////        _rt.anchoredPosition = _savedAnchoredPos;
////////////    }

////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
////////////    private void SnapBack()
////////////    {
////////////        ReturnToHome();

////////////        // Resume previous state without re-triggering animations
////////////        if (_currentZone != null)
////////////        {
////////////            // Was flying before the drag — re-parent to the zone and resume
////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
////////////        }
////////////        else
////////////        {
////////////            EnterIdle();
////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
////////////        }
////////////    }

////////////    /// Fire an Animator trigger by name with warnings for common misconfigurations.
////////////    private void TriggerAnim(string trigger)
////////////    {
////////////        if (_anim == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
////////////            return;
////////////        }
////////////        if (dragonData == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] DragonData is not assigned on the dragon prefab! " +
////////////                             "Drag your DragonData ScriptableObject into the DragonController Inspector field.", this);
////////////            return;
////////////        }
////////////        if (string.IsNullOrEmpty(trigger))
////////////        {
////////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
////////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
////////////            return;
////////////        }
////////////        _anim.SetTrigger(trigger);
////////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // PREFAB SWAP — MOUNT
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Called by SoldierDragDrop.OnEndDrag when a soldier is dropped on this dragon.
////////////    ///
////////////    /// Two paths:
////////////    ///   existingSeat == null  →  PLAIN dragon.  Swap this GO for riderDragonPrefab,
////////////    ///                            transfer state, then mount the soldier on the new seat.
////////////    ///   existingSeat != null  →  Already a RIDER dragon.  Mount the soldier in place.
////////////    ///
////////////    /// The soldier is NEVER a child of this GO when Destroy(gameObject) runs —
////////////    /// SoldierDragDrop.OnBeginDrag reparents it to the canvas root first.
////////////    /// </summary>
////////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat existingSeat)
////////////    {
////////////        // ── Already a rider dragon — mount directly ───────────────────────────
////////////        if (existingSeat != null)
////////////        {
////////////            existingSeat.MountSoldier(soldier);
////////////            return;
////////////        }

////////////        // ── Plain dragon — swap to rider variant ──────────────────────────────
////////////        if (riderDragonPrefab == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] riderDragonPrefab is not assigned. " +
////////////                             "Drag the rider-dragon prefab into the Inspector.", this);
////////////            return;
////////////        }

////////////        // Spawn the rider dragon at the same parent so it inherits the same
////////////        // coordinate space (FlyZone, DragonArea, or root canvas).
////////////        var riderGO = Instantiate(riderDragonPrefab, transform.parent);
////////////        var riderRT = riderGO.GetComponent<RectTransform>();
////////////        riderRT.anchoredPosition = _rt.anchoredPosition;
////////////        riderRT.sizeDelta = _rt.sizeDelta;
////////////        riderRT.localScale = _rt.localScale;
////////////        riderRT.SetSiblingIndex(_rt.GetSiblingIndex());

////////////        // Transfer dragon state so the rider dragon continues from exactly
////////////        // where the plain dragon left off (flying vs idle, zone, homeSlot).
////////////        var riderDC = riderGO.GetComponent<DragonController>();
////////////        if (riderDC != null)
////////////        {
////////////            riderDC.homeSlot = homeSlot;

////////////            if (State == DragonState.Flying && _currentZone != null)
////////////                riderDC.ForceEnterFlying(_currentZone);
////////////            else
////////////                riderDC.ForceEnterIdle();
////////////        }

////////////        // Seat the soldier — DragonRiderSeat.MountSoldier → soldier.MountOnDragon
////////////        var riderSeat = riderGO.GetComponentInChildren<DragonRiderSeat>();
////////////        if (riderSeat != null)
////////////            riderSeat.MountSoldier(soldier);
////////////        else
////////////            Debug.LogWarning("[DragonController] riderDragonPrefab has no DragonRiderSeat child. " +
////////////                             "Add a RiderSeat child with DragonRiderSeat.cs.", this);

////////////        // Destroy the plain dragon — the soldier is already at canvas-root level
////////////        // (reparented by SoldierDragDrop.OnBeginDrag) so it is safe to destroy.
////////////        Destroy(gameObject);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // PREFAB SWAP — DISMOUNT
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Called by SoldierDragDrop.OnBeginDrag after the soldier is safely
////////////    /// reparented to the canvas root.
////////////    ///
////////////    /// Spawns the plain dragon in place of this rider dragon and destroys self.
////////////    /// The soldier is already gone from the hierarchy — safe to Destroy here.
////////////    /// </summary>
////////////    public void PerformDismount()
////////////    {
////////////        if (plainDragonPrefab == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] plainDragonPrefab is not assigned on " +
////////////                             "the rider dragon. Drag the plain-dragon prefab into the Inspector.", this);
////////////            return;
////////////        }

////////////        var plainGO = Instantiate(plainDragonPrefab, transform.parent);
////////////        var plainRT = plainGO.GetComponent<RectTransform>();
////////////        plainRT.anchoredPosition = _rt.anchoredPosition;
////////////        plainRT.sizeDelta = _rt.sizeDelta;
////////////        plainRT.localScale = _rt.localScale;
////////////        plainRT.SetSiblingIndex(_rt.GetSiblingIndex());

////////////        var plainDC = plainGO.GetComponent<DragonController>();
////////////        if (plainDC != null)
////////////        {
////////////            plainDC.homeSlot = homeSlot;

////////////            if (State == DragonState.Flying && _currentZone != null)
////////////                plainDC.ForceEnterFlying(_currentZone);
////////////            else
////////////                plainDC.ForceEnterIdle();
////////////        }

////////////        Destroy(gameObject);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE RESTORE — called on a freshly-spawned dragon after a prefab swap
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Restores flying+patrol state on a newly spawned dragon.
////////////    /// Sets _stateInitialised so Start() does not override it.
////////////    /// </summary>
////////////    public void ForceEnterFlying(FlyZone zone)
////////////    {
////////////        _stateInitialised = true;
////////////        _currentZone = zone;
////////////        EnterFlying();
////////////    }

////////////    /// <summary>
////////////    /// Restores idle state on a newly spawned dragon.
////////////    /// Sets _stateInitialised so Start() does not override it.
////////////    /// </summary>
////////////    public void ForceEnterIdle()
////////////    {
////////////        _stateInitialised = true;
////////////        _currentZone = null;
////////////        EnterIdle();
////////////    }
////////////}

//////////using System.Collections.Generic;
//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// DRAGON CONTROLLER — Single-Prefab Rider System
/////////////
///////////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
///////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SINGLE PREFAB — NO PREFAB SWAP
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  One dragon prefab covers both the plain and rider states:
/////////////
/////////////   Plain state  The dragon patrols / sits idle normally.
/////////////                The DragonRiderVisual child is invisible.
/////////////
/////////////   Rider state  A soldier has been dropped on the dragon.
/////////////                The soldier's own visuals are hidden (alpha 0).
/////////////                The dragon's DragonRiderVisual child is shown with
/////////////                that soldier's armor / helmet / weapon sprites.
/////////////
/////////////  No GameObject is ever destroyed or spawned on mount / dismount.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  STATES
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////////            It can be picked up and dragged (unless a soldier is riding).
/////////////
/////////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////////            semi-transparent, raycasts pass through it.
/////////////
/////////////  Flying    Dragon was dropped on a FlyZone.
/////////////            It patrols left right, flipping sprite at each edge.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  DROP RULES (dragon drag)
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
/////////////  Drop on DragonArea → EnterIdle     (reparented to slot)
/////////////  Drop anywhere else → SnapBack      (return to previous state)
/////////////
/////////////  Dragon dragging is BLOCKED while a soldier is riding it.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  PREFAB HIERARCHY
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
/////////////   DragonBody [0]             Image: dragon body sprite
/////////////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
/////////////     DragonRiderVisual        DragonRiderVisual (hidden by default)
/////////////       BodyLayer              Image
/////////////       FaceLayer              Image
/////////////       HairLayer              Image
/////////////       HelmetLayer            Image
/////////////       WeaponLayer            Image
/////////////   DragonWing [2]             Image: front wing (renders on top of rider)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
/////////////  2. Assign DragonData in the Inspector.
/////////////  3. Add DragonRiderSeat to the RiderSeat child.
/////////////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
/////////////  5. In DragonEggSlot.EnterHatched(), after spawning:
/////////////         var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////         if (dc != null) dc.homeSlot = this;
///////////// </summary>
//////////[RequireComponent(typeof(RectTransform))]
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class DragonController : MonoBehaviour,
//////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────

//////////    [Header("Dragon Data")]
//////////    [SerializeField] private DragonData dragonData;

//////////    [Header("Canvas — auto-found if blank")]
//////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////    [SerializeField] private Canvas rootCanvas;

//////////    [Header("Sprite Orientation")]
//////////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//////////             "The controller flips the scale to match patrol direction.")]
//////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

//////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

//////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////    // ── Private components ────────────────────────────────────────────────────

//////////    private RectTransform _rt;
//////////    private Animator _anim;
//////////    private CanvasGroup _cg;

//////////    // Found in children — both live permanently in the hierarchy.
//////////    private DragonRiderVisual _riderVisual;
//////////    private DragonRiderSeat _riderSeat;

//////////    // ── Drag state ────────────────────────────────────────────────────────────

//////////    private Transform _savedParent;
//////////    private Vector2 _savedAnchoredPos;
//////////    private int _savedSiblingIndex;
//////////    private Vector2 _dragOffset;

//////////    // ── Patrol state ──────────────────────────────────────────────────────────

//////////    private FlyZone _currentZone;
//////////    private float _patrolDir = 1f;   // +1 = right, -1 = left

//////////    // ── Dragon state ──────────────────────────────────────────────────────────

//////////    public enum DragonState { Idle, Dragging, Flying }
//////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // LIFECYCLE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void Awake()
//////////    {
//////////        _rt = GetComponent<RectTransform>();
//////////        _anim = GetComponent<Animator>();
//////////        _cg = GetComponent<CanvasGroup>();

//////////        if (rootCanvas == null)
//////////            rootCanvas = GetComponentInParent<Canvas>();

//////////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
//////////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);

//////////        if (_riderVisual == null)
//////////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
//////////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
//////////        if (_riderSeat == null)
//////////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
//////////                             "Add DragonRiderSeat to the RiderSeat child.", this);
//////////    }

//////////    private void Start()
//////////    {
//////////        EnterIdle();
//////////    }

//////////    private void Update()
//////////    {
//////////        if (State == DragonState.Flying)
//////////            DoPatrol();
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — BEGIN
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnBeginDrag(PointerEventData eventData)
//////////    {
//////////        // Block dragging the dragon while a soldier is riding it.
//////////        if (_riderSeat != null && _riderSeat.IsOccupied)
//////////        {
//////////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
//////////            return;
//////////        }

//////////        _savedParent = _rt.parent;
//////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////        _rt.SetAsLastSibling();

//////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////            ? null : rootCanvas.worldCamera;
//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            rootCanvas.transform as RectTransform,
//////////            eventData.position, uiCam,
//////////            out Vector2 pointerCanvasPos);
//////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////////        _cg.alpha = 0.75f;
//////////        _cg.blocksRaycasts = false;

//////////        State = DragonState.Dragging;
//////////        Debug.Log("[DragonController] OnBeginDrag");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — MOVE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnDrag(PointerEventData eventData)
//////////    {
//////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////            ? null : rootCanvas.worldCamera;

//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            rootCanvas.transform as RectTransform,
//////////            eventData.position, uiCam,
//////////            out Vector2 localPos);

//////////        _rt.anchoredPosition = localPos + _dragOffset;
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — END
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnEndDrag(PointerEventData eventData)
//////////    {
//////////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
//////////        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
//////////        _cg.alpha = 1f;

//////////        var results = new List<RaycastResult>();
//////////        EventSystem.current.RaycastAll(eventData, results);

//////////        _cg.blocksRaycasts = true;

//////////        FlyZone hitFlyZone = null;
//////////        DragonEggSlot hitAreaSlot = null;

//////////        foreach (var r in results)
//////////        {
//////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////        }

//////////        if (hitFlyZone != null)
//////////        {
//////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////            _rt.anchoredPosition = Vector2.zero;
//////////            _currentZone = hitFlyZone;
//////////            EnterFlying();
//////////        }
//////////        else if (hitAreaSlot != null)
//////////        {
//////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//////////            _rt.anchoredPosition = Vector2.zero;
//////////            _currentZone = null;
//////////            EnterIdle();
//////////        }
//////////        else
//////////        {
//////////            SnapBack();
//////////        }
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // STATE — IDLE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void EnterIdle()
//////////    {
//////////        State = DragonState.Idle;

//////////        Vector3 s = transform.localScale;
//////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////        transform.localScale = s;

//////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////        Debug.Log("[DragonController] -> Idle");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // STATE — FLYING + PATROL
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void EnterFlying()
//////////    {
//////////        State = DragonState.Flying;
//////////        _patrolDir = -1f;

//////////        Vector3 s = transform.localScale;
//////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////        transform.localScale = s;

//////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
//////////    }

//////////    private void DoPatrol()
//////////    {
//////////        if (_currentZone == null) return;

//////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

//////////        if (newX >= halfWidth)
//////////        {
//////////            newX = halfWidth;
//////////            _patrolDir = -1f;
//////////            FlipHorizontal();
//////////        }
//////////        else if (newX <= -halfWidth)
//////////        {
//////////            newX = -halfWidth;
//////////            _patrolDir = 1f;
//////////            FlipHorizontal();
//////////        }

//////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // MOUNT — called by SoldierDragDrop.OnEndDrag
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// <summary>
//////////    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
//////////    ///
//////////    /// What happens:
//////////    ///   1. seat.MountSoldier(soldier) is called.
//////////    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
//////////    ///            and reparents them under the seat.
//////////    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
//////////    ///      CharacterEquipment and displays the matching armor / helmet sprites
//////////    ///      on the dragon's built-in rider layers.
//////////    ///
//////////    /// No prefabs are spawned or destroyed.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
//////////    {
//////////        if (seat == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
//////////                             "Make sure the prefab has a DragonRiderSeat child.", this);
//////////            return;
//////////        }

//////////        if (seat.IsOccupied)
//////////        {
//////////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
//////////            return;
//////////        }

//////////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
//////////        seat.MountSoldier(soldier);

//////////        // Step 2 — show the dragon's rider visual with the soldier's equipment.
//////////        if (_riderVisual != null)
//////////        {
//////////            var equipment = soldier.GetComponent<CharacterEquipment>();
//////////            _riderVisual.ShowForSoldier(equipment);
//////////        }
//////////        else
//////////        {
//////////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
//////////                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
//////////        }

//////////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DISMOUNT — called by SoldierDragDrop
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// <summary>
//////////    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
//////////    /// away from the seat (to the canvas root or back to their ground home).
//////////    ///
//////////    /// Hides the rider visual. The dragon continues its current state (Idle or
//////////    /// Flying) without any prefab swap.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        _riderVisual?.Hide();
//////////        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // HELPERS
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void FlipHorizontal()
//////////    {
//////////        Vector3 s = transform.localScale;
//////////        s.x = -s.x;
//////////        transform.localScale = s;
//////////    }

//////////    private void ReturnToHome()
//////////    {
//////////        if (_savedParent == null) return;
//////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////    }

//////////    private void SnapBack()
//////////    {
//////////        ReturnToHome();

//////////        if (_currentZone != null)
//////////        {
//////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////            State = DragonState.Flying;
//////////            Debug.Log("[DragonController] SnapBack -> resume Flying");
//////////        }
//////////        else
//////////        {
//////////            EnterIdle();
//////////            Debug.Log("[DragonController] SnapBack -> resume Idle");
//////////        }
//////////    }

//////////    private void TriggerAnim(string trigger)
//////////    {
//////////        if (_anim == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
//////////            return;
//////////        }
//////////        if (dragonData == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
//////////                             "Drag it into the DragonController Inspector field.", this);
//////////            return;
//////////        }
//////////        if (string.IsNullOrEmpty(trigger))
//////////        {
//////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
//////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
//////////            return;
//////////        }

//////////        _anim.SetTrigger(trigger);
//////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
//////////    }
//////////}

////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// DRAGON CONTROLLER — Single-Prefab Rider System
///////////
/////////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
/////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SINGLE PREFAB — NO PREFAB SWAP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  One dragon prefab covers both the plain and rider states:
///////////
///////////   Plain state  The dragon patrols / sits idle normally.
///////////                The DragonRiderVisual child is invisible.
///////////
///////////   Rider state  A soldier has been dropped on the dragon.
///////////                The soldier's own visuals are hidden (alpha 0).
///////////                The dragon's DragonRiderVisual child is shown with
///////////                that soldier's armor / helmet / weapon sprites.
///////////
///////////  No GameObject is ever destroyed or spawned on mount / dismount.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  STATES
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////            It can be picked up and dragged (unless a soldier is riding).
///////////
///////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////            semi-transparent, raycasts pass through it.
///////////
///////////  Flying    Dragon was dropped on a FlyZone.
///////////            It patrols left right, flipping sprite at each edge.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  DROP RULES (dragon drag)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
///////////  Drop on DragonArea → EnterIdle     (reparented to slot)
///////////  Drop anywhere else → SnapBack      (return to previous state)
///////////
///////////  Dragon dragging is BLOCKED while a soldier is riding it.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
///////////   DragonBody [0]             Image: dragon body sprite
///////////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
///////////     DragonRiderVisual        DragonRiderVisual (hidden by default)
///////////       BodyLayer              Image
///////////       FaceLayer              Image
///////////       HairLayer              Image
///////////       HelmetLayer            Image
///////////       WeaponLayer            Image
///////////   DragonWing [2]             Image: front wing (renders on top of rider)
///////////                              + DragonWingAnimator
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
///////////  2. Assign DragonData in the Inspector.
///////////  3. Add DragonRiderSeat to the RiderSeat child.
///////////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
///////////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
///////////  6. In DragonEggSlot.EnterHatched(), after spawning:
///////////         var dc = _spawnedDragon.GetComponent<DragonController>();
///////////         if (dc != null) dc.homeSlot = this;
/////////// </summary>
////////[RequireComponent(typeof(RectTransform))]
////////[RequireComponent(typeof(CanvasGroup))]
////////public class DragonController : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Dragon Data")]
////////    [SerializeField] private DragonData dragonData;

////////    [Header("Canvas — auto-found if blank")]
////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////    [SerializeField] private Canvas rootCanvas;

////////    [Header("Sprite Orientation")]
////////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
////////             "The controller flips the scale to match patrol direction.")]
////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////    [HideInInspector] public DragonEggSlot homeSlot;

////////    // ── Private components ────────────────────────────────────────────────────

////////    private RectTransform _rt;
////////    private Animator _anim;
////////    private CanvasGroup _cg;

////////    // Found in children — all live permanently in the hierarchy.
////////    private DragonRiderVisual _riderVisual;
////////    private DragonRiderSeat _riderSeat;
////////    private DragonWingAnimator _wingAnimator;   // ← ADDED

////////    // ── Drag state ────────────────────────────────────────────────────────────

////////    private Transform _savedParent;
////////    private Vector2 _savedAnchoredPos;
////////    private int _savedSiblingIndex;
////////    private Vector2 _dragOffset;

////////    // ── Patrol state ──────────────────────────────────────────────────────────

////////    private FlyZone _currentZone;
////////    private float _patrolDir = 1f;   // +1 = right, -1 = left

////////    // ── Dragon state ──────────────────────────────────────────────────────────

////////    public enum DragonState { Idle, Dragging, Flying }
////////    public DragonState State { get; private set; } = DragonState.Idle;

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // LIFECYCLE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void Awake()
////////    {
////////        _rt = GetComponent<RectTransform>();
////////        _anim = GetComponent<Animator>();
////////        _cg = GetComponent<CanvasGroup>();

////////        if (rootCanvas == null)
////////            rootCanvas = GetComponentInParent<Canvas>();

////////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
////////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
////////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);  // ← ADDED

////////        if (_riderVisual == null)
////////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
////////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
////////        if (_riderSeat == null)
////////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
////////                             "Add DragonRiderSeat to the RiderSeat child.", this);
////////        if (_wingAnimator == null)
////////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
////////                             "Add DragonWingAnimator to the DragonWing child.", this);
////////    }

////////    private void Start()
////////    {
////////        EnterIdle();
////////    }

////////    private void Update()
////////    {
////////        if (State == DragonState.Flying)
////////            DoPatrol();
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — BEGIN
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        // Block dragging the dragon while a soldier is riding it.
////////        if (_riderSeat != null && _riderSeat.IsOccupied)
////////        {
////////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
////////            return;
////////        }

////////        _savedParent = _rt.parent;
////////        _savedAnchoredPos = _rt.anchoredPosition;
////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////        _rt.SetAsLastSibling();

////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////            ? null : rootCanvas.worldCamera;
////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            rootCanvas.transform as RectTransform,
////////            eventData.position, uiCam,
////////            out Vector2 pointerCanvasPos);
////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////        _cg.alpha = 0.75f;
////////        _cg.blocksRaycasts = false;

////////        State = DragonState.Dragging;
////////        Debug.Log("[DragonController] OnBeginDrag");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — MOVE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnDrag(PointerEventData eventData)
////////    {
////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////            ? null : rootCanvas.worldCamera;

////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            rootCanvas.transform as RectTransform,
////////            eventData.position, uiCam,
////////            out Vector2 localPos);

////////        _rt.anchoredPosition = localPos + _dragOffset;
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — END
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
////////        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
////////        _cg.alpha = 1f;

////////        var results = new List<RaycastResult>();
////////        EventSystem.current.RaycastAll(eventData, results);

////////        _cg.blocksRaycasts = true;

////////        FlyZone hitFlyZone = null;
////////        DragonEggSlot hitAreaSlot = null;

////////        foreach (var r in results)
////////        {
////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////        }

////////        if (hitFlyZone != null)
////////        {
////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////            _rt.anchoredPosition = Vector2.zero;
////////            _currentZone = hitFlyZone;
////////            EnterFlying();
////////        }
////////        else if (hitAreaSlot != null)
////////        {
////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////////            _rt.anchoredPosition = Vector2.zero;
////////            _currentZone = null;
////////            EnterIdle();
////////        }
////////        else
////////        {
////////            SnapBack();
////////        }
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // STATE — IDLE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void EnterIdle()
////////    {
////////        State = DragonState.Idle;

////////        Vector3 s = transform.localScale;
////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////        transform.localScale = s;

////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);  // ← ADDED
////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////        Debug.Log("[DragonController] -> Idle");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // STATE — FLYING + PATROL
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void EnterFlying()
////////    {
////////        State = DragonState.Flying;
////////        _patrolDir = -1f;

////////        Vector3 s = transform.localScale;
////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////        transform.localScale = s;

////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);  // ← ADDED
////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
////////    }

////////    private void DoPatrol()
////////    {
////////        if (_currentZone == null) return;

////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

////////        if (newX >= halfWidth)
////////        {
////////            newX = halfWidth;
////////            _patrolDir = -1f;
////////            FlipHorizontal();
////////        }
////////        else if (newX <= -halfWidth)
////////        {
////////            newX = -halfWidth;
////////            _patrolDir = 1f;
////////            FlipHorizontal();
////////        }

////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // MOUNT — called by SoldierDragDrop.OnEndDrag
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
////////    ///
////////    /// What happens:
////////    ///   1. seat.MountSoldier(soldier) is called.
////////    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
////////    ///            and reparents them under the seat.
////////    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
////////    ///      CharacterEquipment and displays the matching armor / helmet sprites
////////    ///      on the dragon's built-in rider layers.
////////    ///
////////    /// No prefabs are spawned or destroyed.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
////////                             "Make sure the prefab has a DragonRiderSeat child.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
////////            return;
////////        }

////////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
////////        seat.MountSoldier(soldier);

////////        // Step 2 — show the dragon's rider visual with the soldier's equipment.
////////        if (_riderVisual != null)
////////        {
////////            var equipment = soldier.GetComponent<CharacterEquipment>();
////////            _riderVisual.ShowForSoldier(equipment);
////////        }
////////        else
////////        {
////////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
////////                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
////////        }

////////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DISMOUNT — called by SoldierDragDrop
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
////////    /// away from the seat (to the canvas root or back to their ground home).
////////    ///
////////    /// Hides the rider visual. The dragon continues its current state (Idle or
////////    /// Flying) without any prefab swap.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        _riderVisual?.Hide();
////////        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // HELPERS
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void FlipHorizontal()
////////    {
////////        Vector3 s = transform.localScale;
////////        s.x = -s.x;
////////        transform.localScale = s;
////////    }

////////    private void ReturnToHome()
////////    {
////////        if (_savedParent == null) return;
////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////        _rt.anchoredPosition = _savedAnchoredPos;
////////    }

////////    private void SnapBack()
////////    {
////////        ReturnToHome();

////////        if (_currentZone != null)
////////        {
////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////            EnterFlying();  // ← FIXED: was `State = DragonState.Flying` (skipped wing animator)
////////            Debug.Log("[DragonController] SnapBack -> resume Flying");
////////        }
////////        else
////////        {
////////            EnterIdle();
////////            Debug.Log("[DragonController] SnapBack -> resume Idle");
////////        }
////////    }

////////    private void TriggerAnim(string trigger)
////////    {
////////        if (_anim == null)
////////        {
////////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
////////            return;
////////        }
////////        if (dragonData == null)
////////        {
////////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
////////                             "Drag it into the DragonController Inspector field.", this);
////////            return;
////////        }
////////        if (string.IsNullOrEmpty(trigger))
////////        {
////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
////////            return;
////////        }

////////        _anim.SetTrigger(trigger);
////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////////    }
////////}

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// DRAGON CONTROLLER — Single-Prefab Rider System
/////////
///////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
///////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SINGLE PREFAB — NO PREFAB SWAP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  One dragon prefab covers both the plain and rider states:
/////////
/////////   Plain state  The dragon patrols / sits idle normally.
/////////                The DragonRiderVisual child is invisible.
/////////
/////////   Rider state  A soldier has been dropped on the dragon.
/////////                The soldier's own visuals are hidden (alpha 0).
/////////                The dragon's DragonRiderVisual child is shown with
/////////                that soldier's armor / helmet / weapon sprites.
/////////
/////////  No GameObject is ever destroyed or spawned on mount / dismount.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  STATES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////            It can be picked up and dragged (unless a soldier is riding).
/////////
/////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////            semi-transparent, raycasts pass through it.
/////////
/////////  Flying    Dragon was dropped on a FlyZone.
/////////            It patrols left right, flipping sprite at each edge.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  DROP RULES (dragon drag)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
/////////  Drop on DragonArea → EnterIdle     (reparented to slot)
/////////  Drop anywhere else → SnapBack      (return to previous state)
/////////
/////////  Dragon dragging is BLOCKED while a soldier is riding it.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
/////////   DragonBody [0]             Image: dragon body sprite
/////////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
/////////     DragonRiderVisual        DragonRiderVisual (hidden by default)
/////////       BodyLayer              Image
/////////       FaceLayer              Image
/////////       HairLayer              Image
/////////       HelmetLayer            Image
/////////       WeaponLayer            Image
/////////   DragonWing [2]             Image: front wing (renders on top of rider)
/////////                              + DragonWingAnimator
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
/////////  2. Assign DragonData in the Inspector.
/////////  3. Add DragonRiderSeat to the RiderSeat child.
/////////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
/////////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
/////////  6. In DragonEggSlot.EnterHatched(), after spawning:
/////////         var dc = _spawnedDragon.GetComponent<DragonController>();
/////////         if (dc != null) dc.homeSlot = this;
///////// </summary>
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
//////        // Block dragging the dragon while a soldier is riding it.
//////        if (_riderSeat != null && _riderSeat.IsOccupied)
//////        {
//////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
//////            return;
//////        }

//////        _savedParent = _rt.parent;
//////        _savedAnchoredPos = _rt.anchoredPosition;
//////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////        _rt.SetAsLastSibling();

//////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////            ? null : rootCanvas.worldCamera;
//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            rootCanvas.transform as RectTransform,
//////            eventData.position, uiCam,
//////            out Vector2 pointerCanvasPos);
//////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////        _cg.alpha = 0.75f;
//////        _cg.blocksRaycasts = false;

//////        State = DragonState.Dragging;
//////        Debug.Log("[DragonController] OnBeginDrag");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — MOVE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////            ? null : rootCanvas.worldCamera;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            rootCanvas.transform as RectTransform,
//////            eventData.position, uiCam,
//////            out Vector2 localPos);

//////        _rt.anchoredPosition = localPos + _dragOffset;
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

//////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
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

/////// <summary>
/////// DRAGON CONTROLLER — Single-Prefab Rider System
///////
/////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
/////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SINGLE PREFAB — NO PREFAB SWAP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  One dragon prefab covers both the plain and rider states:
///////
///////   Plain state  The dragon patrols / sits idle normally.
///////                The DragonRiderVisual child is invisible.
///////
///////   Rider state  A soldier has been dropped on the dragon.
///////                The soldier's own visuals are hidden (alpha 0).
///////                The dragon's DragonRiderVisual child is shown with
///////                that soldier's armor / helmet / weapon sprites.
///////
///////  No GameObject is ever destroyed or spawned on mount / dismount.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  STATES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////            It can be picked up and dragged (unless a soldier is riding).
///////
///////  Dragging  Dragon follows the pointer at canvas-root level,
///////            semi-transparent, raycasts pass through it.
///////
///////  Flying    Dragon was dropped on a FlyZone.
///////            It patrols left right, flipping sprite at each edge.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  DROP RULES (dragon drag)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
///////  Drop on DragonArea → EnterIdle     (reparented to slot)
///////  Drop anywhere else → SnapBack      (return to previous state)
///////
///////  Dragon dragging is BLOCKED while a soldier is riding it.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
///////   DragonBody [0]             Image: dragon body sprite
///////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
///////     DragonRiderVisual        DragonRiderVisual (hidden by default)
///////       BodyLayer              Image
///////       FaceLayer              Image
///////       HairLayer              Image
///////       HelmetLayer            Image
///////       WeaponLayer            Image
///////   DragonWing [2]             Image: front wing (renders on top of rider)
///////                              + DragonWingAnimator
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
///////  2. Assign DragonData in the Inspector.
///////  3. Add DragonRiderSeat to the RiderSeat child.
///////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
///////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
///////  6. In DragonEggSlot.EnterHatched(), after spawning:
///////         var dc = _spawnedDragon.GetComponent<DragonController>();
///////         if (dc != null) dc.homeSlot = this;
/////// </summary>
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
////        // Dragon can be dragged even when a soldier is riding.
////        // The soldier is a child of RiderSeat so it follows the dragon automatically.
////        // Clicks pass through to the dragon because DragonRiderSeat.MountSoldier
////        // sets the soldier's CanvasGroup.blocksRaycasts = false on mount.
////        _savedParent = _rt.parent;
////        _savedAnchoredPos = _rt.anchoredPosition;
////        _savedSiblingIndex = _rt.GetSiblingIndex();

////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////        _rt.SetAsLastSibling();

////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////            ? null : rootCanvas.worldCamera;
////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            rootCanvas.transform as RectTransform,
////            eventData.position, uiCam,
////            out Vector2 pointerCanvasPos);
////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////        _cg.alpha = 0.75f;
////        _cg.blocksRaycasts = false;

////        State = DragonState.Dragging;
////        Debug.Log("[DragonController] OnBeginDrag");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////            ? null : rootCanvas.worldCamera;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            rootCanvas.transform as RectTransform,
////            eventData.position, uiCam,
////            out Vector2 localPos);

////        _rt.anchoredPosition = localPos + _dragOffset;
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

////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
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

////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
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

////////////using System.Collections.Generic;
////////////using UnityEngine;
////////////using UnityEngine.EventSystems;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// AREA FORGE — DragonController
///////////////
/////////////// Attach to both the plain dragon prefab and the rider dragon prefab.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  STATES
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Idle      Dragon sits inside DragonArea.
///////////////  Dragging  Dragon follows the pointer at canvas-root level.
///////////////  Flying    Dragon patrols left↔right inside a FlyZone.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  TWO-PREFAB RIDER SWAP
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Two separate dragon prefabs exist in the project:
///////////////
///////////////    PlainDragon   — no rider, draggable by the player.
///////////////    RiderDragon   — rider visuals baked in, soldier parented to its
///////////////                    DragonRiderSeat at runtime.
///////////////
///////////////  When a soldier is dropped on the plain dragon:
///////////////    1. PerformMount() spawns the rider variant at the same position.
///////////////    2. All patrol state (zone, direction, flip, homeSlot) is copied.
///////////////    3. The soldier is mounted on the rider's DragonRiderSeat.
///////////////    4. The plain dragon is deactivated (not destroyed — reused on dismount).
///////////////
///////////////  When the soldier leaves the rider dragon:
///////////////    1. PerformDismount() spawns the plain variant back.
///////////////    2. State is copied again.
///////////////    3. The rider dragon is destroyed.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  INSPECTOR SETUP — PLAIN DRAGON PREFAB
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  dragonData           Your DragonData ScriptableObject.
///////////////  riderVariantPrefab   Drag the RiderDragon prefab here.
///////////////  plainVariantPrefab   Leave BLANK on the plain dragon.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  INSPECTOR SETUP — RIDER DRAGON PREFAB
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  dragonData           Same DragonData ScriptableObject.
///////////////  riderVariantPrefab   Leave BLANK on the rider dragon.
///////////////  plainVariantPrefab   Drag the PlainDragon prefab here.
///////////////
///////////////  The rider dragon MUST have a DragonRiderSeat child so the soldier
///////////////  can be reparented under it.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  SETUP — OTHER
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  1. Both prefabs need a CanvasGroup (auto-required below).
///////////////  2. FlyZone.cs must be on your FlyZone GameObject with a Graphic
///////////////     component so the EventSystem can raycast it.
///////////////  3. In DragonEggSlot.EnterHatched(), after spawning the plain dragon:
///////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
///////////////        if (dc != null) dc.homeSlot = this;
/////////////// </summary>
////////////[RequireComponent(typeof(RectTransform))]
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class DragonController : MonoBehaviour,
////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////{
////////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////////    [Header("Dragon Data")]
////////////    [SerializeField] private DragonData dragonData;

////////////    [Header("Canvas — auto-found if blank")]
////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////////    [SerializeField] private Canvas rootCanvas;

////////////    [Header("Sprite Orientation")]
////////////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1.")]
////////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////////////    // ── Rider Variant Prefab Swap ──────────────────────────────────────────────

////////////    [Header("Rider Variant Prefab Swap")]
////////////    [Tooltip("PLAIN DRAGON: drag the RiderDragon prefab here.\n\n" +
////////////             "When a soldier mounts, this prefab is spawned in place of the plain " +
////////////             "dragon. Leave blank to use the classic in-place mount (soldier sits " +
////////////             "on this dragon's own RiderSeat instead).")]
////////////    [SerializeField] private GameObject riderVariantPrefab;

////////////    [Tooltip("RIDER DRAGON: drag the PlainDragon prefab here.\n\n" +
////////////             "When the soldier dismounts, this prefab is restored. " +
////////////             "Leave blank on the plain dragon.")]
////////////    [SerializeField] private GameObject plainVariantPrefab;

////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////////    [HideInInspector] public DragonEggSlot homeSlot;

////////////    // ── Private ───────────────────────────────────────────────────────────────

////////////    private DragonWingAnimator _wingAnimator;
////////////    private RectTransform _rt;
////////////    private Animator _anim;
////////////    private CanvasGroup _cg;

////////////    // Saved before every drag so we can snap back on an invalid drop.
////////////    private Transform _savedParent;
////////////    private Vector2 _savedAnchoredPos;
////////////    private int _savedSiblingIndex;

////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea).
////////////    private FlyZone _currentZone;

////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre.
////////////    private Vector2 _dragOffset;

////////////    // Patrol direction: +1 = right, -1 = left.
////////////    private float _patrolDir = 1f;

////////////    // True after TransferStateFrom() has already called EnterIdle/Flying,
////////////    // so Start() does not override it with a second EnterIdle().
////////////    private bool _stateTransferred;

////////////    // ── State ─────────────────────────────────────────────────────────────────

////////////    public enum DragonState { Idle, Dragging, Flying }
////////////    public DragonState State { get; private set; } = DragonState.Idle;

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // LIFECYCLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void Awake()
////////////    {
////////////        _rt = GetComponent<RectTransform>();
////////////        _anim = GetComponent<Animator>();
////////////        _cg = GetComponent<CanvasGroup>();

////////////        if (rootCanvas == null)
////////////            rootCanvas = GetComponentInParent<Canvas>();

////////////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);

////////////        if (_wingAnimator == null)
////////////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
////////////                             "Add DragonWingAnimator to the DragonWing child.", this);
////////////    }

////////////    private void Start()
////////////    {
////////////        // Skip if TransferStateFrom() already put us in the correct state.
////////////        if (!_stateTransferred)
////////////            EnterIdle();
////////////    }

////////////    private void Update()
////////////    {
////////////        if (State == DragonState.Flying)
////////////            DoPatrol();
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — BEGIN
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnBeginDrag(PointerEventData eventData)
////////////    {
////////////        // Block dragging the dragon while a soldier is riding it.
////////////        var seat = GetComponentInChildren<DragonRiderSeat>();
////////////        if (seat != null && seat.IsOccupied)
////////////        {
////////////            Debug.Log("[DragonController] Drag blocked — soldier is riding this dragon.");
////////////            return;
////////////        }

////////////        // Snapshot position so we can snap back on an invalid drop.
////////////        _savedParent = _rt.parent;
////////////        _savedAnchoredPos = _rt.anchoredPosition;
////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////////        // Move to canvas root so the dragon draws on top of all panels.
////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////////        _rt.SetAsLastSibling();

////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
////////////        // in canvas space — prevents the dragon jumping on the first drag frame.
////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null : rootCanvas.worldCamera;
////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCamBegin,
////////////            out Vector2 pointerCanvasPos);
////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////////        // Semi-transparent while dragging; disable raycasts so zones are hit.
////////////        _cg.alpha = 0.75f;
////////////        _cg.blocksRaycasts = false;

////////////        State = DragonState.Dragging;
////////////        Debug.Log("[DragonController] OnBeginDrag");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — MOVE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnDrag(PointerEventData eventData)
////////////    {
////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null : rootCanvas.worldCamera;

////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCam,
////////////            out Vector2 localPos);

////////////        _rt.anchoredPosition = localPos + _dragOffset;
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — END  (zone detection + state transition)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnEndDrag(PointerEventData eventData)
////////////    {
////////////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast —
////////////        // otherwise the dragon's own CanvasGroup would shadow the zone below it.
////////////        _cg.alpha = 1f;

////////////        var results = new List<RaycastResult>();
////////////        EventSystem.current.RaycastAll(eventData, results);

////////////        _cg.blocksRaycasts = true;

////////////        FlyZone hitFlyZone = null;
////////////        DragonEggSlot hitAreaSlot = null;

////////////        foreach (var r in results)
////////////        {
////////////            // GetComponentInParent so hitting any child of the zone/area still counts.
////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////////        }

////////////        if (hitFlyZone != null)
////////////        {
////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////////            _rt.anchoredPosition = Vector2.zero;
////////////            _currentZone = hitFlyZone;
////////////            EnterFlying();
////////////        }
////////////        else if (hitAreaSlot != null)
////////////        {
////////////            // Reparent directly to the slot, not to _savedParent (which would be the
////////////            // FlyZone when dragging from patrol, causing a wrong re-parent).
////////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////////////            _rt.anchoredPosition = Vector2.zero;
////////////            _currentZone = null;
////////////            EnterIdle();
////////////        }
////////////        else
////////////        {
////////////            SnapBack();
////////////        }
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — IDLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterIdle()
////////////    {
////////////        State = DragonState.Idle;

////////////        Vector3 s = transform.localScale;
////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////////        transform.localScale = s;

////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
////////////        Debug.Log("[DragonController] → Idle");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — FLYING + PATROL
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterFlying()
////////////    {
////////////        State = DragonState.Flying;
////////////        _patrolDir = -1f;

////////////        Vector3 s = transform.localScale;
////////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////////        transform.localScale = s;

////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
////////////    }

////////////    private void DoPatrol()
////////////    {
////////////        if (_currentZone == null) return;

////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////////        float currentX = _rt.anchoredPosition.x;
////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

////////////        if (newX >= halfWidth)
////////////        {
////////////            newX = halfWidth;
////////////            _patrolDir = -1f;
////////////            FlipHorizontal();
////////////        }
////////////        else if (newX <= -halfWidth)
////////////        {
////////////            newX = -halfWidth;
////////////            _patrolDir = 1f;
////////////            FlipHorizontal();
////////////        }

////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // PREFAB SWAP — MOUNT  (called by SoldierDragDrop.OnEndDrag)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Called by SoldierDragDrop when a soldier is dropped on this (plain) dragon.
////////////    ///
////////////    /// ── If riderVariantPrefab IS assigned (normal path) ───────────────────────
////////////    ///   1. Instantiates the rider variant as a sibling at the same
////////////    ///      parent, anchoredPosition, localScale, and sibling index.
////////////    ///   2. Calls TransferStateFrom(this) on the new dragon so it
////////////    ///      immediately continues the same patrol without a reset.
////////////    ///   3. Mounts the soldier on the rider variant's DragonRiderSeat.
////////////    ///   4. Deactivates this plain dragon (preserved for reuse on dismount).
////////////    ///
////////////    /// ── If riderVariantPrefab is NULL (fallback) ──────────────────────────────
////////////    ///   Falls back to the original in-place behaviour: soldier is mounted on
////////////    ///   this dragon's own DragonRiderSeat (classic system).
////////////    ///
////////////    /// CALL ORDER: SoldierDragDrop must save _mountHomeParent and _mountHomePos
////////////    /// BEFORE calling this method.
////////////    /// </summary>
////////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat fallbackSeat)
////////////    {
////////////        if (riderVariantPrefab == null)
////////////        {
////////////            // No swap configured — classic in-place mount.
////////////            fallbackSeat.MountSoldier(soldier);
////////////            return;
////////////        }

////////////        // ── Spawn rider variant ───────────────────────────────────────────────
////////////        var riderGO = Instantiate(riderVariantPrefab, transform.parent);
////////////        var riderRT = riderGO.GetComponent<RectTransform>();
////////////        riderRT.anchoredPosition = _rt.anchoredPosition;
////////////        riderGO.transform.localScale = transform.localScale;
////////////        riderGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

////////////        // ── Transfer patrol state ─────────────────────────────────────────────
////////////        var riderDC = riderGO.GetComponent<DragonController>();
////////////        if (riderDC != null)
////////////            riderDC.TransferStateFrom(this);

////////////        // ── Mount soldier on the rider variant's seat ─────────────────────────
////////////        var riderSeat = riderGO.GetComponentInChildren<DragonRiderSeat>();
////////////        if (riderSeat != null)
////////////        {
////////////            riderSeat.MountSoldier(soldier);
////////////        }
////////////        else
////////////        {
////////////            Debug.LogError("[DragonController] Rider variant prefab has no DragonRiderSeat " +
////////////                           "child! Add a DragonRiderSeat child to the rider dragon prefab.", riderGO);
////////////        }

////////////        // ── Hide plain dragon (keep alive for potential pool reuse) ───────────
////////////        gameObject.SetActive(false);
////////////        Debug.Log($"[DragonController] '{name}' swapped → rider variant for '{soldier.name}'.");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // PREFAB SWAP — DISMOUNT  (called by SoldierDragDrop after soldier leaves)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Called by SoldierDragDrop when the soldier leaves this (rider) dragon.
////////////    ///
////////////    /// IMPORTANT — call this ONLY after the soldier has been reparented away
////////////    /// from the seat (e.g. to the root canvas or to their ground home). If
////////////    /// called while the soldier is still a child of this dragon, the soldier
////////////    /// will be destroyed along with this GameObject.
////////////    ///
////////////    /// ── If plainVariantPrefab IS assigned (normal path) ───────────────────────
////////////    ///   1. Instantiates the plain dragon at the same parent, position, scale.
////////////    ///   2. Calls TransferStateFrom(this) so patrol resumes seamlessly.
////////////    ///   3. Destroys this rider dragon.
////////////    ///
////////////    /// ── If plainVariantPrefab is NULL ─────────────────────────────────────────
////////////    ///   Logs a warning and does nothing — set it in the rider dragon's Inspector.
////////////    /// </summary>
////////////    public void PerformDismount()
////////////    {
////////////        if (plainVariantPrefab == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] PerformDismount: plainVariantPrefab is not " +
////////////                             "set on this rider variant. Assign it in the Inspector.", this);
////////////            return;
////////////        }

////////////        // ── Spawn plain dragon ────────────────────────────────────────────────
////////////        var plainGO = Instantiate(plainVariantPrefab, transform.parent);
////////////        var plainRT = plainGO.GetComponent<RectTransform>();
////////////        plainRT.anchoredPosition = _rt.anchoredPosition;
////////////        plainGO.transform.localScale = transform.localScale;
////////////        plainGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

////////////        // ── Transfer patrol state ─────────────────────────────────────────────
////////////        var plainDC = plainGO.GetComponent<DragonController>();
////////////        if (plainDC != null)
////////////            plainDC.TransferStateFrom(this);

////////////        // ── Remove rider dragon ───────────────────────────────────────────────
////////////        Debug.Log($"[DragonController] '{name}' swapped → plain variant.");
////////////        Destroy(gameObject);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE TRANSFER  (shared by PerformMount and PerformDismount)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>
////////////    /// Copies all patrol state from <paramref name="source"/> and immediately
////////////    /// enters the matching animation state (Idle or Flying).
////////////    ///
////////////    /// Sets _stateTransferred = true so Start() does not override the state
////////////    /// with its own EnterIdle() call on the next frame.
////////////    ///
////////////    /// Called on the newly spawned dragon immediately after Instantiate(),
////////////    /// before Start() has fired, so the one-frame lag is avoided entirely.
////////////    /// </summary>
////////////    public void TransferStateFrom(DragonController source)
////////////    {
////////////        _stateTransferred = true;

////////////        homeSlot = source.homeSlot;
////////////        _currentZone = source._currentZone;
////////////        _patrolDir = source._patrolDir;

////////////        // Sync position and scale — the caller sets these too, but doing it
////////////        // here as well guards against any future call-order changes.
////////////        _rt.anchoredPosition = source._rt.anchoredPosition;
////////////        transform.localScale = source.transform.localScale;

////////////        if (source.State == DragonState.Flying)
////////////        {
////////////            State = DragonState.Flying;
////////////            _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
////////////            TriggerAnim(dragonData?.dragonFlyTrigger);
////////////        }
////////////        else
////////////        {
////////////            EnterIdle();
////////////        }

////////////        Debug.Log($"[DragonController] '{name}' received state from '{source.name}' " +
////////////                  $"(State={source.State}, Zone={source._currentZone?.name ?? "none"}, " +
////////////                  $"Dir={source._patrolDir}).");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // HELPERS
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// <summary>Flip the sprite by negating localScale.x.</summary>
////////////    private void FlipHorizontal()
////////////    {
////////////        Vector3 s = transform.localScale;
////////////        s.x = -s.x;
////////////        transform.localScale = s;
////////////    }

////////////    /// <summary>Restore the RectTransform to its pre-drag parent, position, and depth.</summary>
////////////    private void ReturnToHome()
////////////    {
////////////        if (_savedParent == null) return;
////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////////        _rt.anchoredPosition = _savedAnchoredPos;
////////////    }

////////////    /// <summary>Invalid drop: put the dragon back where it was and resume its old state.</summary>
////////////    private void SnapBack()
////////////    {
////////////        ReturnToHome();

////////////        if (_currentZone != null)
////////////        {
////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////////            State = DragonState.Flying;
////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
////////////        }
////////////        else
////////////        {
////////////            EnterIdle();
////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
////////////        }
////////////    }

////////////    /// <summary>Fire an Animator trigger by name with warnings for common misconfigurations.</summary>
////////////    private void TriggerAnim(string trigger)
////////////    {
////////////        if (_anim == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
////////////            return;
////////////        }
////////////        if (dragonData == null)
////////////        {
////////////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
////////////                             "Drag your DragonData ScriptableObject into the Inspector.", this);
////////////            return;
////////////        }
////////////        if (string.IsNullOrEmpty(trigger))
////////////        {
////////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
////////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
////////////            return;
////////////        }
////////////        _anim.SetTrigger(trigger);
////////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////////////    }
////////////}


////////////////using System.Collections.Generic;
////////////////using UnityEngine;
////////////////using UnityEngine.EventSystems;
////////////////using UnityEngine.UI;

/////////////////// <summary>
/////////////////// DRAGON CONTROLLER
///////////////////
/////////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
/////////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  STATES
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////////////            It can be picked up and dragged.
///////////////////
///////////////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////////////            semi-transparent, raycasts pass through it.
///////////////////
///////////////////  Flying    Dragon was dropped on a FlyZone.
///////////////////            It plays the fly animation and patrols left↔right
///////////////////            inside the zone, flipping its sprite at each edge.
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  DROP RULES
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
///////////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
///////////////////  Drop anywhere else   → SnapBack     (return to previous state)
///////////////////
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////  SETUP
/////////////////// ════════════════════════════════════════════════════════════════════
///////////////////
///////////////////  1. Add this script to your dragon prefab.
///////////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
///////////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
///////////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
///////////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
///////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
///////////////////        if (dc != null) dc.homeSlot = this;
/////////////////// </summary>
////////////////[RequireComponent(typeof(RectTransform))]
////////////////[RequireComponent(typeof(CanvasGroup))]
////////////////public class DragonController : MonoBehaviour,
////////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////////{
////////////////    // ── Inspector ──────────────────────────────────────────────────────────────
////////////////    [Header("Dragon Data")]
////////////////    [SerializeField] private DragonData dragonData;

////////////////    [Header("Canvas — auto-found if blank")]
////////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////////////    [SerializeField] private Canvas rootCanvas;

////////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
////////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////////////    [HideInInspector] public DragonEggSlot homeSlot;

////////////////    // ── Private ───────────────────────────────────────────────────────────────
////////////////    private RectTransform _rt;
////////////////    private Animator _anim;
////////////////    private CanvasGroup _cg;

////////////////    // Saved before every drag so we can snap back on an invalid drop
////////////////    private Transform _savedParent;
////////////////    private Vector2 _savedAnchoredPos;
////////////////    private int _savedSiblingIndex;

////////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
////////////////    private FlyZone _currentZone;

////////////////    // Patrol bookkeeping
////////////////    private float _patrolDir = 1f;   // +1 right, -1 left

////////////////    // ── State ─────────────────────────────────────────────────────────────────
////////////////    public enum DragonState { Idle, Dragging, Flying }
////////////////    public DragonState State { get; private set; } = DragonState.Idle;

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // LIFECYCLE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void Awake()
////////////////    {
////////////////        _rt = GetComponent<RectTransform>();
////////////////        _anim = GetComponent<Animator>();
////////////////        _cg = GetComponent<CanvasGroup>();

////////////////        if (rootCanvas == null)
////////////////            rootCanvas = GetComponentInParent<Canvas>();
////////////////    }

////////////////    private void Start()
////////////////    {
////////////////        EnterIdle();
////////////////    }

////////////////    private void Update()
////////////////    {
////////////////        if (State == DragonState.Flying)
////////////////            DoPatrol();
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — BEGIN
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnBeginDrag(PointerEventData eventData)
////////////////    {
////////////////        // Snapshot current position so we can snap back if the drop is invalid
////////////////        _savedParent = _rt.parent;
////////////////        _savedAnchoredPos = _rt.anchoredPosition;
////////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////////////        // Move to canvas root so the dragon draws on top of all panels
////////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////////////        _rt.SetAsLastSibling();

////////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
////////////////        _cg.alpha = 0.75f;
////////////////        _cg.blocksRaycasts = false;

////////////////        State = DragonState.Dragging;

////////////////        Debug.Log("[DragonController] OnBeginDrag");
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — MOVE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnDrag(PointerEventData eventData)
////////////////    {
////////////////        // Convert screen-space pointer to canvas-local position
////////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////////            ? null
////////////////            : rootCanvas.worldCamera;

////////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////////            rootCanvas.transform as RectTransform,
////////////////            eventData.position,
////////////////            uiCam,
////////////////            out Vector2 localPos);

////////////////        _rt.anchoredPosition = localPos;
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // DRAG — END  (zone detection + state transition)
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    public void OnEndDrag(PointerEventData eventData)
////////////////    {
////////////////        // Restore full opacity and raycast blocking
////////////////        _cg.alpha = 1f;
////////////////        _cg.blocksRaycasts = true;

////////////////        // ── Raycast everything under the pointer ──────────────────────────────
////////////////        var results = new List<RaycastResult>();
////////////////        EventSystem.current.RaycastAll(eventData, results);

////////////////        FlyZone hitFlyZone = null;
////////////////        DragonEggSlot hitAreaSlot = null;

////////////////        foreach (var r in results)
////////////////        {
////////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
////////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
////////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////////////        }

////////////////        // ── Decide destination ────────────────────────────────────────────────
////////////////        if (hitFlyZone != null)
////////////////        {
////////////////            // Dropped onto a Fly Zone → start flying + patrol
////////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
////////////////            _currentZone = hitFlyZone;
////////////////            EnterFlying();
////////////////        }
////////////////        else if (hitAreaSlot != null)
////////////////        {
////////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
////////////////            ReturnToHome();
////////////////            _currentZone = null;
////////////////            EnterIdle();
////////////////        }
////////////////        else
////////////////        {
////////////////            // Invalid drop → snap back to wherever it was before the drag
////////////////            SnapBack();
////////////////        }
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // STATE — IDLE
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void EnterIdle()
////////////////    {
////////////////        State = DragonState.Idle;
////////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////////////        Debug.Log("[DragonController] → Idle");
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // STATE — FLYING + PATROL
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    private void EnterFlying()
////////////////    {
////////////////        State = DragonState.Flying;
////////////////        _patrolDir = 1f;   // always start moving right

////////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
////////////////    }

////////////////    private void DoPatrol()
////////////////    {
////////////////        if (_currentZone == null) return;

////////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////////////        float currentX = _rt.anchoredPosition.x;
////////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

////////////////        // Bounce at patrol edges
////////////////        if (newX >= halfWidth)
////////////////        {
////////////////            newX = halfWidth;
////////////////            _patrolDir = -1f;
////////////////            FlipHorizontal();
////////////////        }
////////////////        else if (newX <= -halfWidth)
////////////////        {
////////////////            newX = -halfWidth;
////////////////            _patrolDir = 1f;
////////////////            FlipHorizontal();
////////////////        }

////////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////////////    }

////////////////    // ══════════════════════════════════════════════════════════════════════════
////////////////    // HELPERS
////////////////    // ══════════════════════════════════════════════════════════════════════════

////////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
////////////////    private void FlipHorizontal()
////////////////    {
////////////////        Vector3 s = transform.localScale;
////////////////        s.x = -s.x;
////////////////        transform.localScale = s;
////////////////    }

////////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
////////////////    private void ReturnToHome()
////////////////    {
////////////////        if (_savedParent == null) return;
////////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////////////        _rt.anchoredPosition = _savedAnchoredPos;
////////////////    }

////////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
////////////////    private void SnapBack()
////////////////    {
////////////////        ReturnToHome();

////////////////        // Resume previous state without re-triggering animations
////////////////        if (_currentZone != null)
////////////////        {
////////////////            // Was flying before the drag — re-parent to the zone and resume
////////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
////////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
////////////////        }
////////////////        else
////////////////        {
////////////////            EnterIdle();
////////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
////////////////        }
////////////////    }

////////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
////////////////    private void TriggerAnim(string trigger)
////////////////    {
////////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
////////////////        _anim.SetTrigger(trigger);
////////////////    }
////////////////}


//////////////using System.Collections.Generic;
//////////////using UnityEngine;
//////////////using UnityEngine.EventSystems;
//////////////using UnityEngine.UI;

///////////////// <summary>
///////////////// DRAGON CONTROLLER
/////////////////
///////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
///////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  STATES
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////////////            It can be picked up and dragged.
/////////////////
/////////////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////////////            semi-transparent, raycasts pass through it.
/////////////////
/////////////////  Flying    Dragon was dropped on a FlyZone.
/////////////////            It plays the fly animation and patrols left↔right
/////////////////            inside the zone, flipping its sprite at each edge.
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  DROP RULES
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
/////////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
/////////////////  Drop anywhere else   → SnapBack     (return to previous state)
/////////////////
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////  SETUP
///////////////// ════════════════════════════════════════════════════════════════════
/////////////////
/////////////////  1. Add this script to your dragon prefab.
/////////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
/////////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
/////////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
/////////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
/////////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////////        if (dc != null) dc.homeSlot = this;
///////////////// </summary>
//////////////[RequireComponent(typeof(RectTransform))]
//////////////[RequireComponent(typeof(CanvasGroup))]
//////////////public class DragonController : MonoBehaviour,
//////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////////{
//////////////    // ── Inspector ──────────────────────────────────────────────────────────────
//////////////    [Header("Dragon Data")]
//////////////    [SerializeField] private DragonData dragonData;

//////////////    [Header("Canvas — auto-found if blank")]
//////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////////    [SerializeField] private Canvas rootCanvas;

//////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
//////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////////    // ── Private ───────────────────────────────────────────────────────────────
//////////////    private RectTransform _rt;
//////////////    private Animator _anim;
//////////////    private CanvasGroup _cg;

//////////////    // Saved before every drag so we can snap back on an invalid drop
//////////////    private Transform _savedParent;
//////////////    private Vector2 _savedAnchoredPos;
//////////////    private int _savedSiblingIndex;

//////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
//////////////    private FlyZone _currentZone;

//////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
//////////////    private Vector2 _dragOffset;

//////////////    // Patrol bookkeeping
//////////////    private float _patrolDir = 1f;   // +1 right, -1 left

//////////////    // ── State ─────────────────────────────────────────────────────────────────
//////////////    public enum DragonState { Idle, Dragging, Flying }
//////////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // LIFECYCLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void Awake()
//////////////    {
//////////////        _rt = GetComponent<RectTransform>();
//////////////        _anim = GetComponent<Animator>();
//////////////        _cg = GetComponent<CanvasGroup>();

//////////////        if (rootCanvas == null)
//////////////            rootCanvas = GetComponentInParent<Canvas>();
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        EnterIdle();
//////////////    }

//////////////    private void Update()
//////////////    {
//////////////        if (State == DragonState.Flying)
//////////////            DoPatrol();
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — BEGIN
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnBeginDrag(PointerEventData eventData)
//////////////    {
//////////////        // Snapshot current position so we can snap back if the drop is invalid
//////////////        _savedParent = _rt.parent;
//////////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////////        // Move to canvas root so the dragon draws on top of all panels
//////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////////        _rt.SetAsLastSibling();

//////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
//////////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
//////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null : rootCanvas.worldCamera;
//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCamBegin,
//////////////            out Vector2 pointerCanvasPos);
//////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
//////////////        _cg.alpha = 0.75f;
//////////////        _cg.blocksRaycasts = false;

//////////////        State = DragonState.Dragging;

//////////////        Debug.Log("[DragonController] OnBeginDrag");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — MOVE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnDrag(PointerEventData eventData)
//////////////    {
//////////////        // Convert screen-space pointer to canvas-local position
//////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////////            ? null
//////////////            : rootCanvas.worldCamera;

//////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////////            rootCanvas.transform as RectTransform,
//////////////            eventData.position,
//////////////            uiCam,
//////////////            out Vector2 localPos);

//////////////        _rt.anchoredPosition = localPos + _dragOffset;
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // DRAG — END  (zone detection + state transition)
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    public void OnEndDrag(PointerEventData eventData)
//////////////    {
//////////////        // Restore full opacity and raycast blocking
//////////////        _cg.alpha = 1f;
//////////////        _cg.blocksRaycasts = true;

//////////////        // ── Raycast everything under the pointer ──────────────────────────────
//////////////        var results = new List<RaycastResult>();
//////////////        EventSystem.current.RaycastAll(eventData, results);

//////////////        FlyZone hitFlyZone = null;
//////////////        DragonEggSlot hitAreaSlot = null;

//////////////        foreach (var r in results)
//////////////        {
//////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
//////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
//////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////////        }

//////////////        // ── Decide destination ────────────────────────────────────────────────
//////////////        if (hitFlyZone != null)
//////////////        {
//////////////            // Dropped onto a Fly Zone → start flying + patrol
//////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
//////////////            _currentZone = hitFlyZone;
//////////////            EnterFlying();
//////////////        }
//////////////        else if (hitAreaSlot != null)
//////////////        {
//////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
//////////////            ReturnToHome();
//////////////            _currentZone = null;
//////////////            EnterIdle();
//////////////        }
//////////////        else
//////////////        {
//////////////            // Invalid drop → snap back to wherever it was before the drag
//////////////            SnapBack();
//////////////        }
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — IDLE
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterIdle()
//////////////    {
//////////////        State = DragonState.Idle;
//////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////////        Debug.Log("[DragonController] → Idle");
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // STATE — FLYING + PATROL
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    private void EnterFlying()
//////////////    {
//////////////        State = DragonState.Flying;
//////////////        _patrolDir = 1f;   // always start moving right

//////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//////////////    }

//////////////    private void DoPatrol()
//////////////    {
//////////////        if (_currentZone == null) return;

//////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////////        float currentX = _rt.anchoredPosition.x;
//////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//////////////        // Bounce at patrol edges
//////////////        if (newX >= halfWidth)
//////////////        {
//////////////            newX = halfWidth;
//////////////            _patrolDir = -1f;
//////////////            FlipHorizontal();
//////////////        }
//////////////        else if (newX <= -halfWidth)
//////////////        {
//////////////            newX = -halfWidth;
//////////////            _patrolDir = 1f;
//////////////            FlipHorizontal();
//////////////        }

//////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////////    }

//////////////    // ══════════════════════════════════════════════════════════════════════════
//////////////    // HELPERS
//////////////    // ══════════════════════════════════════════════════════════════════════════

//////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
//////////////    private void FlipHorizontal()
//////////////    {
//////////////        Vector3 s = transform.localScale;
//////////////        s.x = -s.x;
//////////////        transform.localScale = s;
//////////////    }

//////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
//////////////    private void ReturnToHome()
//////////////    {
//////////////        if (_savedParent == null) return;
//////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////////    }

//////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
//////////////    private void SnapBack()
//////////////    {
//////////////        ReturnToHome();

//////////////        // Resume previous state without re-triggering animations
//////////////        if (_currentZone != null)
//////////////        {
//////////////            // Was flying before the drag — re-parent to the zone and resume
//////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
//////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
//////////////        }
//////////////        else
//////////////        {
//////////////            EnterIdle();
//////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
//////////////        }
//////////////    }

//////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
//////////////    private void TriggerAnim(string trigger)
//////////////    {
//////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
//////////////        _anim.SetTrigger(trigger);
//////////////    }
//////////////}


////////////using System.Collections.Generic;
////////////using UnityEngine;
////////////using UnityEngine.EventSystems;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// DRAGON CONTROLLER
///////////////
/////////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
/////////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  STATES
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////////            It can be picked up and dragged.
///////////////
///////////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////////            semi-transparent, raycasts pass through it.
///////////////
///////////////  Flying    Dragon was dropped on a FlyZone.
///////////////            It plays the fly animation and patrols left↔right
///////////////            inside the zone, flipping its sprite at each edge.
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  DROP RULES
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
///////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
///////////////  Drop anywhere else   → SnapBack     (return to previous state)
///////////////
/////////////// ════════════════════════════════════════════════════════════════════
///////////////  SETUP
/////////////// ════════════════════════════════════════════════════════════════════
///////////////
///////////////  1. Add this script to your dragon prefab.
///////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
///////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
///////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
///////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
///////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
///////////////        if (dc != null) dc.homeSlot = this;
/////////////// </summary>
////////////[RequireComponent(typeof(RectTransform))]
////////////[RequireComponent(typeof(CanvasGroup))]
////////////public class DragonController : MonoBehaviour,
////////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////////{
////////////    // ── Inspector ──────────────────────────────────────────────────────────────
////////////    [Header("Dragon Data")]
////////////    [SerializeField] private DragonData dragonData;

////////////    [Header("Canvas — auto-found if blank")]
////////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////////    [SerializeField] private Canvas rootCanvas;

////////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
////////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////////    [HideInInspector] public DragonEggSlot homeSlot;

////////////    // ── Private ───────────────────────────────────────────────────────────────
////////////    private RectTransform _rt;
////////////    private Animator _anim;
////////////    private CanvasGroup _cg;

////////////    // Saved before every drag so we can snap back on an invalid drop
////////////    private Transform _savedParent;
////////////    private Vector2 _savedAnchoredPos;
////////////    private int _savedSiblingIndex;

////////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
////////////    private FlyZone _currentZone;

////////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
////////////    private Vector2 _dragOffset;

////////////    // Patrol bookkeeping
////////////    private float _patrolDir = 1f;   // +1 right, -1 left

////////////    // ── State ─────────────────────────────────────────────────────────────────
////////////    public enum DragonState { Idle, Dragging, Flying }
////////////    public DragonState State { get; private set; } = DragonState.Idle;

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // LIFECYCLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void Awake()
////////////    {
////////////        _rt = GetComponent<RectTransform>();
////////////        _anim = GetComponent<Animator>();
////////////        _cg = GetComponent<CanvasGroup>();

////////////        if (rootCanvas == null)
////////////            rootCanvas = GetComponentInParent<Canvas>();
////////////    }

////////////    private void Start()
////////////    {
////////////        EnterIdle();
////////////    }

////////////    private void Update()
////////////    {
////////////        if (State == DragonState.Flying)
////////////            DoPatrol();
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — BEGIN
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnBeginDrag(PointerEventData eventData)
////////////    {
////////////        // Snapshot current position so we can snap back if the drop is invalid
////////////        _savedParent = _rt.parent;
////////////        _savedAnchoredPos = _rt.anchoredPosition;
////////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////////        // Move to canvas root so the dragon draws on top of all panels
////////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////////        _rt.SetAsLastSibling();

////////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
////////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
////////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null : rootCanvas.worldCamera;
////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCamBegin,
////////////            out Vector2 pointerCanvasPos);
////////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////////        // Semi-transparent while dragging; disable raycasts so zones are hit
////////////        _cg.alpha = 0.75f;
////////////        _cg.blocksRaycasts = false;

////////////        State = DragonState.Dragging;

////////////        Debug.Log("[DragonController] OnBeginDrag");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — MOVE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnDrag(PointerEventData eventData)
////////////    {
////////////        // Convert screen-space pointer to canvas-local position
////////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////////            ? null
////////////            : rootCanvas.worldCamera;

////////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////////            rootCanvas.transform as RectTransform,
////////////            eventData.position,
////////////            uiCam,
////////////            out Vector2 localPos);

////////////        _rt.anchoredPosition = localPos + _dragOffset;
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // DRAG — END  (zone detection + state transition)
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    public void OnEndDrag(PointerEventData eventData)
////////////    {
////////////        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
////////////        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
////////////        // the FlyZone underneath is never detected.
////////////        _cg.alpha = 1f;

////////////        // ── Raycast everything under the pointer ──────────────────────────────
////////////        var results = new List<RaycastResult>();
////////////        EventSystem.current.RaycastAll(eventData, results);

////////////        // Now safe to restore — raycast is already done
////////////        _cg.blocksRaycasts = true;

////////////        FlyZone hitFlyZone = null;
////////////        DragonEggSlot hitAreaSlot = null;

////////////        foreach (var r in results)
////////////        {
////////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
////////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
////////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////////        }

////////////        // ── Decide destination ────────────────────────────────────────────────
////////////        if (hitFlyZone != null)
////////////        {
////////////            // Dropped onto a Fly Zone → start flying + patrol
////////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
////////////            _currentZone = hitFlyZone;
////////////            EnterFlying();
////////////        }
////////////        else if (hitAreaSlot != null)
////////////        {
////////////            // Dropped onto any DragonArea (preferably its home) → back to idle
////////////            ReturnToHome();
////////////            _currentZone = null;
////////////            EnterIdle();
////////////        }
////////////        else
////////////        {
////////////            // Invalid drop → snap back to wherever it was before the drag
////////////            SnapBack();
////////////        }
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — IDLE
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterIdle()
////////////    {
////////////        State = DragonState.Idle;
////////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////////        Debug.Log("[DragonController] → Idle");
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // STATE — FLYING + PATROL
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    private void EnterFlying()
////////////    {
////////////        State = DragonState.Flying;
////////////        _patrolDir = 1f;   // start moving right

////////////        // Ensure localScale.x is positive so the sprite faces right,
////////////        // matching _patrolDir. Without this, a leftward flip from a
////////////        // previous patrol session carries over and the dragon faces the
////////////        // wrong way on entry.
////////////        Vector3 s = transform.localScale;
////////////        s.x = Mathf.Abs(s.x);
////////////        transform.localScale = s;

////////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
////////////    }

////////////    private void DoPatrol()
////////////    {
////////////        if (_currentZone == null) return;

////////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////////        float currentX = _rt.anchoredPosition.x;
////////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

////////////        // Bounce at patrol edges
////////////        if (newX >= halfWidth)
////////////        {
////////////            newX = halfWidth;
////////////            _patrolDir = -1f;
////////////            FlipHorizontal();
////////////        }
////////////        else if (newX <= -halfWidth)
////////////        {
////////////            newX = -halfWidth;
////////////            _patrolDir = 1f;
////////////            FlipHorizontal();
////////////        }

////////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////////    }

////////////    // ══════════════════════════════════════════════════════════════════════════
////////////    // HELPERS
////////////    // ══════════════════════════════════════════════════════════════════════════

////////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
////////////    private void FlipHorizontal()
////////////    {
////////////        Vector3 s = transform.localScale;
////////////        //s.x = -s.x;
////////////        transform.localScale = s;
////////////    }

////////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
////////////    private void ReturnToHome()
////////////    {
////////////        if (_savedParent == null) return;
////////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////////        _rt.anchoredPosition = _savedAnchoredPos;
////////////    }

////////////    /// Invalid drop: put the dragon back where it was and resume its old state.
////////////    private void SnapBack()
////////////    {
////////////        ReturnToHome();

////////////        // Resume previous state without re-triggering animations
////////////        if (_currentZone != null)
////////////        {
////////////            // Was flying before the drag — re-parent to the zone and resume
////////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
////////////            Debug.Log("[DragonController] SnapBack → resume Flying");
////////////        }
////////////        else
////////////        {
////////////            EnterIdle();
////////////            Debug.Log("[DragonController] SnapBack → resume Idle");
////////////        }
////////////    }

////////////    /// Fire an Animator trigger by name (safe: skips if null or empty).
////////////    private void TriggerAnim(string trigger)
////////////    {
////////////        if (_anim == null || string.IsNullOrEmpty(trigger)) return;
////////////        _anim.SetTrigger(trigger);
////////////    }
////////////}

//////////using System.Collections.Generic;
//////////using UnityEngine;
//////////using UnityEngine.EventSystems;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// DRAGON CONTROLLER
/////////////
///////////// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
///////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  STATES
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////////            It can be picked up and dragged.
/////////////
/////////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////////            semi-transparent, raycasts pass through it.
/////////////
/////////////  Flying    Dragon was dropped on a FlyZone.
/////////////            It plays the fly animation and patrols left↔right
/////////////            inside the zone, flipping its sprite at each edge.
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  DROP RULES
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
/////////////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
/////////////  Drop anywhere else   → SnapBack     (return to previous state)
/////////////
///////////// ════════════════════════════════════════════════════════════════════
/////////////  SETUP
///////////// ════════════════════════════════════════════════════════════════════
/////////////
/////////////  1. Add this script to your dragon prefab.
/////////////  2. Give the prefab a CanvasGroup component (auto-created if missing).
/////////////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
/////////////     (e.g. a transparent Image) so the EventSystem can raycast it.
/////////////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
/////////////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////////////        if (dc != null) dc.homeSlot = this;
///////////// </summary>
//////////[RequireComponent(typeof(RectTransform))]
//////////[RequireComponent(typeof(CanvasGroup))]
//////////public class DragonController : MonoBehaviour,
//////////    IBeginDragHandler, IDragHandler, IEndDragHandler
//////////{
//////////    // ── Inspector ──────────────────────────────────────────────────────────────
//////////    [Header("Dragon Data")]
//////////    [SerializeField] private DragonData dragonData;

//////////    [Header("Canvas — auto-found if blank")]
//////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
//////////    [SerializeField] private Canvas rootCanvas;

//////////    [Header("Sprite Orientation")]
//////////    [Tooltip("Tick this if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//////////             "The controller flips the scale to match the patrol direction.")]
//////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

//////////    [Header("Prefab Swap — Dragon Mount")]
//////////    //    [Tooltip("The RIDER variant of this dragon (has DragonLayeredVisual, RiderSeat, DragonWing).
//////////    //" +
//////////    //             "Assign on the PLAIN dragon prefab.
//////////    //" +
//////////    //             "When a soldier is dropped onto the plain dragon, it is destroyed and this prefab
//////////    //" +
//////////    //             "is spawned in its place with the soldier already seated.")]
//////////    //    [SerializeField] private GameObject riderDragonPrefab;

//////////    //    [Tooltip("The PLAIN variant of this dragon (just Image + DragonController + Animator).
//////////    //" +
//////////    //             "Assign on the RIDER dragon prefab.
//////////    //" +
//////////    //             "When the soldier dismounts, the rider dragon is destroyed and this prefab
//////////    //" +
//////////    //             "is spawned in its place so the dragon continues patrolling alone.")]
//////////    [Tooltip("The RIDER variant of this dragon (has DragonLayeredVisual, RiderSeat, DragonWing).\n" +
//////////         "Assign on the PLAIN dragon prefab.\n" +
//////////         "When a soldier is dropped onto the plain dragon, it is destroyed and this prefab\n" +
//////////         "is spawned in its place with the soldier already seated.")]
//////////    [SerializeField] private GameObject riderDragonPrefab;

//////////    [Tooltip("The PLAIN variant of this dragon (just Image + DragonController + Animator).\n" +
//////////             "Assign on the RIDER dragon prefab.\n" +
//////////             "When the soldier dismounts, the rider dragon is destroyed and this prefab\n" +
//////////             "is spawned in its place so the dragon continues patrolling alone.")]

//////////    [SerializeField] private GameObject plainDragonPrefab;

//////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
//////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//////////    [HideInInspector] public DragonEggSlot homeSlot;

//////////    // ── Private ───────────────────────────────────────────────────────────────
//////////    private RectTransform _rt;
//////////    private Animator _anim;
//////////    private CanvasGroup _cg;

//////////    // Saved before every drag so we can snap back on an invalid drop
//////////    private Transform _savedParent;
//////////    private Vector2 _savedAnchoredPos;
//////////    private int _savedSiblingIndex;

//////////    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
//////////    private FlyZone _currentZone;

//////////    // Drag offset — keeps the dragon under the grab point, not the pointer centre
//////////    private Vector2 _dragOffset;

//////////    // Patrol bookkeeping
//////////    private float _patrolDir = 1f;   // +1 right, -1 left

//////////    // Prefab-swap guard — set by ForceEnterFlying/Idle so that Start() does
//////////    // not call EnterIdle() and override the state that PerformMount applied.
//////////    private bool _stateInitialised;

//////////    // ── State ─────────────────────────────────────────────────────────────────
//////////    public enum DragonState { Idle, Dragging, Flying }
//////////    public DragonState State { get; private set; } = DragonState.Idle;

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // LIFECYCLE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void Awake()
//////////    {
//////////        _rt = GetComponent<RectTransform>();
//////////        _anim = GetComponent<Animator>();
//////////        _cg = GetComponent<CanvasGroup>();

//////////        if (rootCanvas == null)
//////////            rootCanvas = GetComponentInParent<Canvas>();
//////////    }

//////////    private void Start()
//////////    {
//////////        // Skip if ForceEnterFlying/Idle already set state during a prefab swap.
//////////        // Without this guard, Start() would call EnterIdle() one frame after
//////////        // PerformMount() called ForceEnterFlying(), cancelling the flying state.
//////////        if (!_stateInitialised)
//////////            EnterIdle();
//////////    }

//////////    private void Update()
//////////    {
//////////        if (State == DragonState.Flying)
//////////            DoPatrol();
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — BEGIN
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnBeginDrag(PointerEventData eventData)
//////////    {
//////////        // Snapshot current position so we can snap back if the drop is invalid
//////////        _savedParent = _rt.parent;
//////////        _savedAnchoredPos = _rt.anchoredPosition;
//////////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////////        // Move to canvas root so the dragon draws on top of all panels
//////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////////        _rt.SetAsLastSibling();

//////////        // Calculate grab offset AFTER reparenting so anchoredPosition is already
//////////        // in canvas space. Prevents the dragon jumping on the first drag frame.
//////////        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////            ? null : rootCanvas.worldCamera;
//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            rootCanvas.transform as RectTransform,
//////////            eventData.position,
//////////            uiCamBegin,
//////////            out Vector2 pointerCanvasPos);
//////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////////        // Semi-transparent while dragging; disable raycasts so zones are hit
//////////        _cg.alpha = 0.75f;
//////////        _cg.blocksRaycasts = false;

//////////        State = DragonState.Dragging;

//////////        Debug.Log("[DragonController] OnBeginDrag");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — MOVE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnDrag(PointerEventData eventData)
//////////    {
//////////        // Convert screen-space pointer to canvas-local position
//////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////////            ? null
//////////            : rootCanvas.worldCamera;

//////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////////            rootCanvas.transform as RectTransform,
//////////            eventData.position,
//////////            uiCam,
//////////            out Vector2 localPos);

//////////        _rt.anchoredPosition = localPos + _dragOffset;
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // DRAG — END  (zone detection + state transition)
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    public void OnEndDrag(PointerEventData eventData)
//////////    {
//////////        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
//////////        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
//////////        // the FlyZone underneath is never detected.
//////////        _cg.alpha = 1f;

//////////        // ── Raycast everything under the pointer ──────────────────────────────
//////////        var results = new List<RaycastResult>();
//////////        EventSystem.current.RaycastAll(eventData, results);

//////////        // Now safe to restore — raycast is already done
//////////        _cg.blocksRaycasts = true;

//////////        FlyZone hitFlyZone = null;
//////////        DragonEggSlot hitAreaSlot = null;

//////////        foreach (var r in results)
//////////        {
//////////            // GetComponentInParent so hitting any child of the zone/area still counts
//////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
//////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
//////////            if (hitFlyZone != null && hitAreaSlot != null) break;
//////////        }

//////////        // ── Decide destination ────────────────────────────────────────────────
//////////        if (hitFlyZone != null)
//////////        {
//////////            // Dropped onto a Fly Zone → start flying + patrol
//////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//////////            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
//////////            _currentZone = hitFlyZone;
//////////            EnterFlying();
//////////        }
//////////        else if (hitAreaSlot != null)
//////////        {
//////////            // Dropped onto DragonArea → reparent directly to the slot, not to
//////////            // _savedParent (which would be the FlyZone when dragging from patrol).
//////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
//////////            _rt.anchoredPosition = Vector2.zero;
//////////            _currentZone = null;
//////////            EnterIdle();
//////////        }
//////////        else
//////////        {
//////////            // Invalid drop → snap back to wherever it was before the drag
//////////            SnapBack();
//////////        }
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // STATE — IDLE
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void EnterIdle()
//////////    {
//////////        State = DragonState.Idle;

//////////        // Reset to natural facing direction so patrol flips don't carry over to idle.
//////////        //   spriteDefaultFacesLeft = true  → restore positive scale (faces left)
//////////        //   spriteDefaultFacesLeft = false → restore negative scale (faces right)
//////////        Vector3 s = transform.localScale;
//////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////        transform.localScale = s;

//////////        TriggerAnim(dragonData?.dragonIdleTrigger);
//////////        Debug.Log("[DragonController] → Idle");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // STATE — FLYING + PATROL
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    private void EnterFlying()
//////////    {
//////////        State = DragonState.Flying;
//////////        _patrolDir = -1f;  // start moving left

//////////        // Set localScale.x so the sprite FACES LEFT on entry (matching patrolDir -1).
//////////        //   spriteDefaultFacesLeft = true  → positive scale = faces left  → Abs (natural)
//////////        //   spriteDefaultFacesLeft = false → positive scale = faces right → negate to face left
//////////        Vector3 s = transform.localScale;
//////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//////////        transform.localScale = s;

//////////        TriggerAnim(dragonData?.dragonFlyTrigger);
//////////        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//////////    }

//////////    private void DoPatrol()
//////////    {
//////////        if (_currentZone == null) return;

//////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//////////        float halfWidth = _currentZone.PatrolHalfWidth;
//////////        float currentX = _rt.anchoredPosition.x;
//////////        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//////////        // Bounce at patrol edges
//////////        if (newX >= halfWidth)
//////////        {
//////////            newX = halfWidth;
//////////            _patrolDir = -1f;
//////////            FlipHorizontal();
//////////        }
//////////        else if (newX <= -halfWidth)
//////////        {
//////////            newX = -halfWidth;
//////////            _patrolDir = 1f;
//////////            FlipHorizontal();
//////////        }

//////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // HELPERS
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
//////////    private void FlipHorizontal()
//////////    {
//////////        Vector3 s = transform.localScale;
//////////        s.x = -s.x;
//////////        transform.localScale = s;
//////////    }

//////////    /// Restore the RectTransform to its pre-drag parent, position and depth.
//////////    private void ReturnToHome()
//////////    {
//////////        if (_savedParent == null) return;
//////////        _rt.SetParent(_savedParent, worldPositionStays: false);
//////////        _rt.SetSiblingIndex(_savedSiblingIndex);
//////////        _rt.anchoredPosition = _savedAnchoredPos;
//////////    }

//////////    /// Invalid drop: put the dragon back where it was and resume its old state.
//////////    private void SnapBack()
//////////    {
//////////        ReturnToHome();

//////////        // Resume previous state without re-triggering animations
//////////        if (_currentZone != null)
//////////        {
//////////            // Was flying before the drag — re-parent to the zone and resume
//////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//////////            State = DragonState.Flying;   // keep flying, patrol continues in Update
//////////            Debug.Log("[DragonController] SnapBack → resume Flying");
//////////        }
//////////        else
//////////        {
//////////            EnterIdle();
//////////            Debug.Log("[DragonController] SnapBack → resume Idle");
//////////        }
//////////    }

//////////    /// Fire an Animator trigger by name with warnings for common misconfigurations.
//////////    private void TriggerAnim(string trigger)
//////////    {
//////////        if (_anim == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
//////////            return;
//////////        }
//////////        if (dragonData == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] DragonData is not assigned on the dragon prefab! " +
//////////                             "Drag your DragonData ScriptableObject into the DragonController Inspector field.", this);
//////////            return;
//////////        }
//////////        if (string.IsNullOrEmpty(trigger))
//////////        {
//////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
//////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
//////////            return;
//////////        }
//////////        _anim.SetTrigger(trigger);
//////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // PREFAB SWAP — MOUNT
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// <summary>
//////////    /// Called by SoldierDragDrop.OnEndDrag when a soldier is dropped on this dragon.
//////////    ///
//////////    /// Two paths:
//////////    ///   existingSeat == null  →  PLAIN dragon.  Swap this GO for riderDragonPrefab,
//////////    ///                            transfer state, then mount the soldier on the new seat.
//////////    ///   existingSeat != null  →  Already a RIDER dragon.  Mount the soldier in place.
//////////    ///
//////////    /// The soldier is NEVER a child of this GO when Destroy(gameObject) runs —
//////////    /// SoldierDragDrop.OnBeginDrag reparents it to the canvas root first.
//////////    /// </summary>
//////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat existingSeat)
//////////    {
//////////        // ── Already a rider dragon — mount directly ───────────────────────────
//////////        if (existingSeat != null)
//////////        {
//////////            existingSeat.MountSoldier(soldier);
//////////            return;
//////////        }

//////////        // ── Plain dragon — swap to rider variant ──────────────────────────────
//////////        if (riderDragonPrefab == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] riderDragonPrefab is not assigned. " +
//////////                             "Drag the rider-dragon prefab into the Inspector.", this);
//////////            return;
//////////        }

//////////        // Spawn the rider dragon at the same parent so it inherits the same
//////////        // coordinate space (FlyZone, DragonArea, or root canvas).
//////////        var riderGO = Instantiate(riderDragonPrefab, transform.parent);
//////////        var riderRT = riderGO.GetComponent<RectTransform>();
//////////        riderRT.anchoredPosition = _rt.anchoredPosition;
//////////        riderRT.sizeDelta = _rt.sizeDelta;
//////////        riderRT.localScale = _rt.localScale;
//////////        riderRT.SetSiblingIndex(_rt.GetSiblingIndex());

//////////        // Transfer dragon state so the rider dragon continues from exactly
//////////        // where the plain dragon left off (flying vs idle, zone, homeSlot).
//////////        var riderDC = riderGO.GetComponent<DragonController>();
//////////        if (riderDC != null)
//////////        {
//////////            riderDC.homeSlot = homeSlot;

//////////            if (State == DragonState.Flying && _currentZone != null)
//////////                riderDC.ForceEnterFlying(_currentZone);
//////////            else
//////////                riderDC.ForceEnterIdle();
//////////        }

//////////        // Seat the soldier — DragonRiderSeat.MountSoldier → soldier.MountOnDragon
//////////        var riderSeat = riderGO.GetComponentInChildren<DragonRiderSeat>();
//////////        if (riderSeat != null)
//////////            riderSeat.MountSoldier(soldier);
//////////        else
//////////            Debug.LogWarning("[DragonController] riderDragonPrefab has no DragonRiderSeat child. " +
//////////                             "Add a RiderSeat child with DragonRiderSeat.cs.", this);

//////////        // Destroy the plain dragon — the soldier is already at canvas-root level
//////////        // (reparented by SoldierDragDrop.OnBeginDrag) so it is safe to destroy.
//////////        Destroy(gameObject);
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // PREFAB SWAP — DISMOUNT
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// <summary>
//////////    /// Called by SoldierDragDrop.OnBeginDrag after the soldier is safely
//////////    /// reparented to the canvas root.
//////////    ///
//////////    /// Spawns the plain dragon in place of this rider dragon and destroys self.
//////////    /// The soldier is already gone from the hierarchy — safe to Destroy here.
//////////    /// </summary>
//////////    public void PerformDismount()
//////////    {
//////////        if (plainDragonPrefab == null)
//////////        {
//////////            Debug.LogWarning("[DragonController] plainDragonPrefab is not assigned on " +
//////////                             "the rider dragon. Drag the plain-dragon prefab into the Inspector.", this);
//////////            return;
//////////        }

//////////        var plainGO = Instantiate(plainDragonPrefab, transform.parent);
//////////        var plainRT = plainGO.GetComponent<RectTransform>();
//////////        plainRT.anchoredPosition = _rt.anchoredPosition;
//////////        plainRT.sizeDelta = _rt.sizeDelta;
//////////        plainRT.localScale = _rt.localScale;
//////////        plainRT.SetSiblingIndex(_rt.GetSiblingIndex());

//////////        var plainDC = plainGO.GetComponent<DragonController>();
//////////        if (plainDC != null)
//////////        {
//////////            plainDC.homeSlot = homeSlot;

//////////            if (State == DragonState.Flying && _currentZone != null)
//////////                plainDC.ForceEnterFlying(_currentZone);
//////////            else
//////////                plainDC.ForceEnterIdle();
//////////        }

//////////        Destroy(gameObject);
//////////    }

//////////    // ══════════════════════════════════════════════════════════════════════════
//////////    // STATE RESTORE — called on a freshly-spawned dragon after a prefab swap
//////////    // ══════════════════════════════════════════════════════════════════════════

//////////    /// <summary>
//////////    /// Restores flying+patrol state on a newly spawned dragon.
//////////    /// Sets _stateInitialised so Start() does not override it.
//////////    /// </summary>
//////////    public void ForceEnterFlying(FlyZone zone)
//////////    {
//////////        _stateInitialised = true;
//////////        _currentZone = zone;
//////////        EnterFlying();
//////////    }

//////////    /// <summary>
//////////    /// Restores idle state on a newly spawned dragon.
//////////    /// Sets _stateInitialised so Start() does not override it.
//////////    /// </summary>
//////////    public void ForceEnterIdle()
//////////    {
//////////        _stateInitialised = true;
//////////        _currentZone = null;
//////////        EnterIdle();
//////////    }
//////////}

////////using System.Collections.Generic;
////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// DRAGON CONTROLLER — Single-Prefab Rider System
///////////
/////////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
/////////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SINGLE PREFAB — NO PREFAB SWAP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  One dragon prefab covers both the plain and rider states:
///////////
///////////   Plain state  The dragon patrols / sits idle normally.
///////////                The DragonRiderVisual child is invisible.
///////////
///////////   Rider state  A soldier has been dropped on the dragon.
///////////                The soldier's own visuals are hidden (alpha 0).
///////////                The dragon's DragonRiderVisual child is shown with
///////////                that soldier's armor / helmet / weapon sprites.
///////////
///////////  No GameObject is ever destroyed or spawned on mount / dismount.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  STATES
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////////            It can be picked up and dragged (unless a soldier is riding).
///////////
///////////  Dragging  Dragon follows the pointer at canvas-root level,
///////////            semi-transparent, raycasts pass through it.
///////////
///////////  Flying    Dragon was dropped on a FlyZone.
///////////            It patrols left right, flipping sprite at each edge.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  DROP RULES (dragon drag)
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
///////////  Drop on DragonArea → EnterIdle     (reparented to slot)
///////////  Drop anywhere else → SnapBack      (return to previous state)
///////////
///////////  Dragon dragging is BLOCKED while a soldier is riding it.
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  PREFAB HIERARCHY
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
///////////   DragonBody [0]             Image: dragon body sprite
///////////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
///////////     DragonRiderVisual        DragonRiderVisual (hidden by default)
///////////       BodyLayer              Image
///////////       FaceLayer              Image
///////////       HairLayer              Image
///////////       HelmetLayer            Image
///////////       WeaponLayer            Image
///////////   DragonWing [2]             Image: front wing (renders on top of rider)
///////////
/////////// ════════════════════════════════════════════════════════════════════
///////////  SETUP
/////////// ════════════════════════════════════════════════════════════════════
///////////
///////////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
///////////  2. Assign DragonData in the Inspector.
///////////  3. Add DragonRiderSeat to the RiderSeat child.
///////////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
///////////  5. In DragonEggSlot.EnterHatched(), after spawning:
///////////         var dc = _spawnedDragon.GetComponent<DragonController>();
///////////         if (dc != null) dc.homeSlot = this;
/////////// </summary>
////////[RequireComponent(typeof(RectTransform))]
////////[RequireComponent(typeof(CanvasGroup))]
////////public class DragonController : MonoBehaviour,
////////    IBeginDragHandler, IDragHandler, IEndDragHandler
////////{
////////    // ── Inspector ──────────────────────────────────────────────────────────────

////////    [Header("Dragon Data")]
////////    [SerializeField] private DragonData dragonData;

////////    [Header("Canvas — auto-found if blank")]
////////    [Tooltip("Root Canvas that hosts the UI. Found automatically via GetComponentInParent.")]
////////    [SerializeField] private Canvas rootCanvas;

////////    [Header("Sprite Orientation")]
////////    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
////////             "The controller flips the scale to match patrol direction.")]
////////    [SerializeField] private bool spriteDefaultFacesLeft = true;

////////    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

////////    /// <summary>The DragonArea slot this dragon hatched from.</summary>
////////    [HideInInspector] public DragonEggSlot homeSlot;

////////    // ── Private components ────────────────────────────────────────────────────

////////    private RectTransform _rt;
////////    private Animator _anim;
////////    private CanvasGroup _cg;

////////    // Found in children — both live permanently in the hierarchy.
////////    private DragonRiderVisual _riderVisual;
////////    private DragonRiderSeat _riderSeat;

////////    // ── Drag state ────────────────────────────────────────────────────────────

////////    private Transform _savedParent;
////////    private Vector2 _savedAnchoredPos;
////////    private int _savedSiblingIndex;
////////    private Vector2 _dragOffset;

////////    // ── Patrol state ──────────────────────────────────────────────────────────

////////    private FlyZone _currentZone;
////////    private float _patrolDir = 1f;   // +1 = right, -1 = left

////////    // ── Dragon state ──────────────────────────────────────────────────────────

////////    public enum DragonState { Idle, Dragging, Flying }
////////    public DragonState State { get; private set; } = DragonState.Idle;

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // LIFECYCLE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void Awake()
////////    {
////////        _rt = GetComponent<RectTransform>();
////////        _anim = GetComponent<Animator>();
////////        _cg = GetComponent<CanvasGroup>();

////////        if (rootCanvas == null)
////////            rootCanvas = GetComponentInParent<Canvas>();

////////        _riderVisual = GetComponentInChildren<DragonRiderVisual>(includeInactive: true);
////////        _riderSeat = GetComponentInChildren<DragonRiderSeat>(includeInactive: true);

////////        if (_riderVisual == null)
////////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
////////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
////////        if (_riderSeat == null)
////////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
////////                             "Add DragonRiderSeat to the RiderSeat child.", this);
////////    }

////////    private void Start()
////////    {
////////        EnterIdle();
////////    }

////////    private void Update()
////////    {
////////        if (State == DragonState.Flying)
////////            DoPatrol();
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — BEGIN
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnBeginDrag(PointerEventData eventData)
////////    {
////////        // Block dragging the dragon while a soldier is riding it.
////////        if (_riderSeat != null && _riderSeat.IsOccupied)
////////        {
////////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
////////            return;
////////        }

////////        _savedParent = _rt.parent;
////////        _savedAnchoredPos = _rt.anchoredPosition;
////////        _savedSiblingIndex = _rt.GetSiblingIndex();

////////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////////        _rt.SetAsLastSibling();

////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////            ? null : rootCanvas.worldCamera;
////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            rootCanvas.transform as RectTransform,
////////            eventData.position, uiCam,
////////            out Vector2 pointerCanvasPos);
////////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////////        _cg.alpha = 0.75f;
////////        _cg.blocksRaycasts = false;

////////        State = DragonState.Dragging;
////////        Debug.Log("[DragonController] OnBeginDrag");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — MOVE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnDrag(PointerEventData eventData)
////////    {
////////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////            ? null : rootCanvas.worldCamera;

////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            rootCanvas.transform as RectTransform,
////////            eventData.position, uiCam,
////////            out Vector2 localPos);

////////        _rt.anchoredPosition = localPos + _dragOffset;
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DRAG — END
////////    // ══════════════════════════════════════════════════════════════════════════

////////    public void OnEndDrag(PointerEventData eventData)
////////    {
////////        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast
////////        // so the dragon's own CanvasGroup does not shadow the zone beneath it.
////////        _cg.alpha = 1f;

////////        var results = new List<RaycastResult>();
////////        EventSystem.current.RaycastAll(eventData, results);

////////        _cg.blocksRaycasts = true;

////////        FlyZone hitFlyZone = null;
////////        DragonEggSlot hitAreaSlot = null;

////////        foreach (var r in results)
////////        {
////////            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
////////            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
////////            if (hitFlyZone != null && hitAreaSlot != null) break;
////////        }

////////        if (hitFlyZone != null)
////////        {
////////            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
////////            _rt.anchoredPosition = Vector2.zero;
////////            _currentZone = hitFlyZone;
////////            EnterFlying();
////////        }
////////        else if (hitAreaSlot != null)
////////        {
////////            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
////////            _rt.anchoredPosition = Vector2.zero;
////////            _currentZone = null;
////////            EnterIdle();
////////        }
////////        else
////////        {
////////            SnapBack();
////////        }
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // STATE — IDLE
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void EnterIdle()
////////    {
////////        State = DragonState.Idle;

////////        Vector3 s = transform.localScale;
////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////        transform.localScale = s;

////////        TriggerAnim(dragonData?.dragonIdleTrigger);
////////        Debug.Log("[DragonController] -> Idle");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // STATE — FLYING + PATROL
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void EnterFlying()
////////    {
////////        State = DragonState.Flying;
////////        _patrolDir = -1f;

////////        Vector3 s = transform.localScale;
////////        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
////////        transform.localScale = s;

////////        TriggerAnim(dragonData?.dragonFlyTrigger);
////////        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
////////    }

////////    private void DoPatrol()
////////    {
////////        if (_currentZone == null) return;

////////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
////////        float halfWidth = _currentZone.PatrolHalfWidth;
////////        float newX = _rt.anchoredPosition.x + _patrolDir * speed * Time.deltaTime;

////////        if (newX >= halfWidth)
////////        {
////////            newX = halfWidth;
////////            _patrolDir = -1f;
////////            FlipHorizontal();
////////        }
////////        else if (newX <= -halfWidth)
////////        {
////////            newX = -halfWidth;
////////            _patrolDir = 1f;
////////            FlipHorizontal();
////////        }

////////        _rt.anchoredPosition = new Vector2(newX, _rt.anchoredPosition.y);
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // MOUNT — called by SoldierDragDrop.OnEndDrag
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Called by SoldierDragDrop when the soldier is dropped on this dragon.
////////    ///
////////    /// What happens:
////////    ///   1. seat.MountSoldier(soldier) is called.
////////    ///         -> soldier.MountOnDragon() hides the soldier's own visuals
////////    ///            and reparents them under the seat.
////////    ///   2. DragonRiderVisual.ShowForSoldier() reads the soldier's
////////    ///      CharacterEquipment and displays the matching armor / helmet sprites
////////    ///      on the dragon's built-in rider layers.
////////    ///
////////    /// No prefabs are spawned or destroyed.
////////    /// </summary>
////////    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat seat)
////////    {
////////        if (seat == null)
////////        {
////////            Debug.LogWarning("[DragonController] PerformMount: seat is null. " +
////////                             "Make sure the prefab has a DragonRiderSeat child.", this);
////////            return;
////////        }

////////        if (seat.IsOccupied)
////////        {
////////            Debug.Log("[DragonController] PerformMount: seat already occupied.");
////////            return;
////////        }

////////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
////////        seat.MountSoldier(soldier);

////////        // Step 2 — show the dragon's rider visual with the soldier's equipment.
////////        if (_riderVisual != null)
////////        {
////////            var equipment = soldier.GetComponent<CharacterEquipment>();
////////            _riderVisual.ShowForSoldier(equipment);
////////        }
////////        else
////////        {
////////            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
////////                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
////////        }

////////        Debug.Log($"[DragonController] '{soldier.name}' mounted on '{name}'.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // DISMOUNT — called by SoldierDragDrop
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Called by SoldierDragDrop AFTER the soldier has been safely reparented
////////    /// away from the seat (to the canvas root or back to their ground home).
////////    ///
////////    /// Hides the rider visual. The dragon continues its current state (Idle or
////////    /// Flying) without any prefab swap.
////////    /// </summary>
////////    public void PerformDismount()
////////    {
////////        _riderVisual?.Hide();
////////        Debug.Log($"[DragonController] Rider dismounted from '{name}' — visual hidden.");
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // HELPERS
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void FlipHorizontal()
////////    {
////////        Vector3 s = transform.localScale;
////////        s.x = -s.x;
////////        transform.localScale = s;
////////    }

////////    private void ReturnToHome()
////////    {
////////        if (_savedParent == null) return;
////////        _rt.SetParent(_savedParent, worldPositionStays: false);
////////        _rt.SetSiblingIndex(_savedSiblingIndex);
////////        _rt.anchoredPosition = _savedAnchoredPos;
////////    }

////////    private void SnapBack()
////////    {
////////        ReturnToHome();

////////        if (_currentZone != null)
////////        {
////////            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
////////            State = DragonState.Flying;
////////            Debug.Log("[DragonController] SnapBack -> resume Flying");
////////        }
////////        else
////////        {
////////            EnterIdle();
////////            Debug.Log("[DragonController] SnapBack -> resume Idle");
////////        }
////////    }

////////    private void TriggerAnim(string trigger)
////////    {
////////        if (_anim == null)
////////        {
////////            Debug.LogWarning("[DragonController] No Animator on the dragon prefab!", this);
////////            return;
////////        }
////////        if (dragonData == null)
////////        {
////////            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
////////                             "Drag it into the DragonController Inspector field.", this);
////////            return;
////////        }
////////        if (string.IsNullOrEmpty(trigger))
////////        {
////////            Debug.LogWarning("[DragonController] Trigger name is empty in DragonData. " +
////////                             "Fill in dragonFlyTrigger / dragonIdleTrigger.", this);
////////            return;
////////        }

////////        _anim.SetTrigger(trigger);
////////        Debug.Log($"[DragonController] SetTrigger({trigger})");
////////    }
////////}

//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// DRAGON CONTROLLER — Single-Prefab Rider System
/////////
///////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
///////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SINGLE PREFAB — NO PREFAB SWAP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  One dragon prefab covers both the plain and rider states:
/////////
/////////   Plain state  The dragon patrols / sits idle normally.
/////////                The DragonRiderVisual child is invisible.
/////////
/////////   Rider state  A soldier has been dropped on the dragon.
/////////                The soldier's own visuals are hidden (alpha 0).
/////////                The dragon's DragonRiderVisual child is shown with
/////////                that soldier's armor / helmet / weapon sprites.
/////////
/////////  No GameObject is ever destroyed or spawned on mount / dismount.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  STATES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////////            It can be picked up and dragged (unless a soldier is riding).
/////////
/////////  Dragging  Dragon follows the pointer at canvas-root level,
/////////            semi-transparent, raycasts pass through it.
/////////
/////////  Flying    Dragon was dropped on a FlyZone.
/////////            It patrols left right, flipping sprite at each edge.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  DROP RULES (dragon drag)
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
/////////  Drop on DragonArea → EnterIdle     (reparented to slot)
/////////  Drop anywhere else → SnapBack      (return to previous state)
/////////
/////////  Dragon dragging is BLOCKED while a soldier is riding it.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  PREFAB HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
/////////   DragonBody [0]             Image: dragon body sprite
/////////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
/////////     DragonRiderVisual        DragonRiderVisual (hidden by default)
/////////       BodyLayer              Image
/////////       FaceLayer              Image
/////////       HairLayer              Image
/////////       HelmetLayer            Image
/////////       WeaponLayer            Image
/////////   DragonWing [2]             Image: front wing (renders on top of rider)
/////////                              + DragonWingAnimator
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  SETUP
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
/////////  2. Assign DragonData in the Inspector.
/////////  3. Add DragonRiderSeat to the RiderSeat child.
/////////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
/////////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
/////////  6. In DragonEggSlot.EnterHatched(), after spawning:
/////////         var dc = _spawnedDragon.GetComponent<DragonController>();
/////////         if (dc != null) dc.homeSlot = this;
///////// </summary>
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
//////    private DragonWingAnimator _wingAnimator;   // ← ADDED

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
//////        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);  // ← ADDED

//////        if (_riderVisual == null)
//////            Debug.LogWarning("[DragonController] No DragonRiderVisual found in children. " +
//////                             "Add DragonRiderVisual to a child of RiderSeat.", this);
//////        if (_riderSeat == null)
//////            Debug.LogWarning("[DragonController] No DragonRiderSeat found in children. " +
//////                             "Add DragonRiderSeat to the RiderSeat child.", this);
//////        if (_wingAnimator == null)
//////            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
//////                             "Add DragonWingAnimator to the DragonWing child.", this);
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
//////        // Block dragging the dragon while a soldier is riding it.
//////        if (_riderSeat != null && _riderSeat.IsOccupied)
//////        {
//////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
//////            return;
//////        }

//////        _savedParent = _rt.parent;
//////        _savedAnchoredPos = _rt.anchoredPosition;
//////        _savedSiblingIndex = _rt.GetSiblingIndex();

//////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//////        _rt.SetAsLastSibling();

//////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////            ? null : rootCanvas.worldCamera;
//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            rootCanvas.transform as RectTransform,
//////            eventData.position, uiCam,
//////            out Vector2 pointerCanvasPos);
//////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//////        _cg.alpha = 0.75f;
//////        _cg.blocksRaycasts = false;

//////        State = DragonState.Dragging;
//////        Debug.Log("[DragonController] OnBeginDrag");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // DRAG — MOVE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    public void OnDrag(PointerEventData eventData)
//////    {
//////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////            ? null : rootCanvas.worldCamera;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            rootCanvas.transform as RectTransform,
//////            eventData.position, uiCam,
//////            out Vector2 localPos);

//////        _rt.anchoredPosition = localPos + _dragOffset;
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

//////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);  // ← ADDED
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

//////        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);  // ← ADDED
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

//////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
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

/////// <summary>
/////// DRAGON CONTROLLER — Single-Prefab Rider System
///////
/////// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
/////// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SINGLE PREFAB — NO PREFAB SWAP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  One dragon prefab covers both the plain and rider states:
///////
///////   Plain state  The dragon patrols / sits idle normally.
///////                The DragonRiderVisual child is invisible.
///////
///////   Rider state  A soldier has been dropped on the dragon.
///////                The soldier's own visuals are hidden (alpha 0).
///////                The dragon's DragonRiderVisual child is shown with
///////                that soldier's armor / helmet / weapon sprites.
///////
///////  No GameObject is ever destroyed or spawned on mount / dismount.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  STATES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Idle      Dragon sits inside DragonArea playing its idle animation.
///////            It can be picked up and dragged (unless a soldier is riding).
///////
///////  Dragging  Dragon follows the pointer at canvas-root level,
///////            semi-transparent, raycasts pass through it.
///////
///////  Flying    Dragon was dropped on a FlyZone.
///////            It patrols left right, flipping sprite at each edge.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  DROP RULES (dragon drag)
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
///////  Drop on DragonArea → EnterIdle     (reparented to slot)
///////  Drop anywhere else → SnapBack      (return to previous state)
///////
///////  Dragon dragging is BLOCKED while a soldier is riding it.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  PREFAB HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
///////   DragonBody [0]             Image: dragon body sprite
///////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
///////     DragonRiderVisual        DragonRiderVisual (hidden by default)
///////       BodyLayer              Image
///////       FaceLayer              Image
///////       HairLayer              Image
///////       HelmetLayer            Image
///////       WeaponLayer            Image
///////   DragonWing [2]             Image: front wing (renders on top of rider)
///////                              + DragonWingAnimator
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
///////  2. Assign DragonData in the Inspector.
///////  3. Add DragonRiderSeat to the RiderSeat child.
///////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
///////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
///////  6. In DragonEggSlot.EnterHatched(), after spawning:
///////         var dc = _spawnedDragon.GetComponent<DragonController>();
///////         if (dc != null) dc.homeSlot = this;
/////// </summary>
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
////        // Block dragging the dragon while a soldier is riding it.
////        if (_riderSeat != null && _riderSeat.IsOccupied)
////        {
////            Debug.Log("[DragonController] Drag blocked — a soldier is riding this dragon.");
////            return;
////        }

////        _savedParent = _rt.parent;
////        _savedAnchoredPos = _rt.anchoredPosition;
////        _savedSiblingIndex = _rt.GetSiblingIndex();

////        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
////        _rt.SetAsLastSibling();

////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////            ? null : rootCanvas.worldCamera;
////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            rootCanvas.transform as RectTransform,
////            eventData.position, uiCam,
////            out Vector2 pointerCanvasPos);
////        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

////        _cg.alpha = 0.75f;
////        _cg.blocksRaycasts = false;

////        State = DragonState.Dragging;
////        Debug.Log("[DragonController] OnBeginDrag");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // DRAG — MOVE
////    // ══════════════════════════════════════════════════════════════════════════

////    public void OnDrag(PointerEventData eventData)
////    {
////        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////            ? null : rootCanvas.worldCamera;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            rootCanvas.transform as RectTransform,
////            eventData.position, uiCam,
////            out Vector2 localPos);

////        _rt.anchoredPosition = localPos + _dragOffset;
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

////        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
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

////        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
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

///// <summary>
///// DRAGON CONTROLLER — Single-Prefab Rider System
/////
///// Attach to the dragon prefab (needs RectTransform + Animator + CanvasGroup).
///// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////
///// ════════════════════════════════════════════════════════════════════
/////  SINGLE PREFAB — NO PREFAB SWAP
///// ════════════════════════════════════════════════════════════════════
/////
/////  One dragon prefab covers both the plain and rider states:
/////
/////   Plain state  The dragon patrols / sits idle normally.
/////                The DragonRiderVisual child is invisible.
/////
/////   Rider state  A soldier has been dropped on the dragon.
/////                The soldier's own visuals are hidden (alpha 0).
/////                The dragon's DragonRiderVisual child is shown with
/////                that soldier's armor / helmet / weapon sprites.
/////
/////  No GameObject is ever destroyed or spawned on mount / dismount.
/////
///// ════════════════════════════════════════════════════════════════════
/////  STATES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////            It can be picked up and dragged (unless a soldier is riding).
/////
/////  Dragging  Dragon follows the pointer at canvas-root level,
/////            semi-transparent, raycasts pass through it.
/////
/////  Flying    Dragon was dropped on a FlyZone.
/////            It patrols left right, flipping sprite at each edge.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DROP RULES (dragon drag)
///// ════════════════════════════════════════════════════════════════════
/////
/////  Drop on FlyZone    → EnterFlying   (reparented to FlyZone)
/////  Drop on DragonArea → EnterIdle     (reparented to slot)
/////  Drop anywhere else → SnapBack      (return to previous state)
/////
/////  Dragon dragging is BLOCKED while a soldier is riding it.
/////
///// ════════════════════════════════════════════════════════════════════
/////  PREFAB HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   Dragon (root)              Dragon Controller + CanvasGroup + DragonLayeredVisual
/////   DragonBody [0]             Image: dragon body sprite
/////   RiderSeat  [1]             DragonRiderSeat (transparent raycast target)
/////     DragonRiderVisual        DragonRiderVisual (hidden by default)
/////       BodyLayer              Image
/////       FaceLayer              Image
/////       HairLayer              Image
/////       HelmetLayer            Image
/////       WeaponLayer            Image
/////   DragonWing [2]             Image: front wing (renders on top of rider)
/////                              + DragonWingAnimator
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Add DragonController + CanvasGroup + DragonLayeredVisual to the root.
/////  2. Assign DragonData in the Inspector.
/////  3. Add DragonRiderSeat to the RiderSeat child.
/////  4. Add DragonRiderVisual to a child of RiderSeat; assign its Image layers.
/////  5. Add DragonWingAnimator to the DragonWing child; assign idle/fly sprites.
/////  6. In DragonEggSlot.EnterHatched(), after spawning:
/////         var dc = _spawnedDragon.GetComponent<DragonController>();
/////         if (dc != null) dc.homeSlot = this;
///// </summary>
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
//        // Dragon can be dragged even when a soldier is riding.
//        // The soldier is a child of RiderSeat so it follows the dragon automatically.
//        // Clicks pass through to the dragon because DragonRiderSeat.MountSoldier
//        // sets the soldier's CanvasGroup.blocksRaycasts = false on mount.
//        _savedParent = _rt.parent;
//        _savedAnchoredPos = _rt.anchoredPosition;
//        _savedSiblingIndex = _rt.GetSiblingIndex();

//        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//        _rt.SetAsLastSibling();

//        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null : rootCanvas.worldCamera;
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            rootCanvas.transform as RectTransform,
//            eventData.position, uiCam,
//            out Vector2 pointerCanvasPos);
//        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//        _cg.alpha = 0.75f;
//        _cg.blocksRaycasts = false;

//        State = DragonState.Dragging;
//        Debug.Log("[DragonController] OnBeginDrag");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null : rootCanvas.worldCamera;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            rootCanvas.transform as RectTransform,
//            eventData.position, uiCam,
//            out Vector2 localPos);

//        _rt.anchoredPosition = localPos + _dragOffset;
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

//        // Rider mirrors the dragon: switch to sit-still animation.
//        _riderVisual?.SetRiderState(AnimationState.RiderIdle);

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

//        // Rider mirrors the dragon: switch to leaning-forward fly animation.
//        _riderVisual?.SetRiderState(AnimationState.RiderFly);

//        Debug.Log($"[DragonController] -> Flying  zone={_currentZone?.name}");
//    }

//    private void DoPatrol()
//    {
//        if (_currentZone == null) return;

//        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
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

//        // Step 1 — seat the soldier (calls MountOnDragon which hides soldier visuals).
//        seat.MountSoldier(soldier);

//        // Step 2 — show the dragon's rider visual with the soldier's equipment.
//        if (_riderVisual != null)
//        {
//            var equipment = soldier.GetComponent<CharacterEquipment>();
//            _riderVisual.ShowForSoldier(equipment);

//            // Sync rider animation to the dragon's current state immediately
//            // so a soldier mounted on a flying dragon sees the fly pose right away.
//            _riderVisual.SetRiderState(State == DragonState.Flying
//                ? AnimationState.RiderFly
//                : AnimationState.RiderIdle);
//        }
//        else
//        {
//            Debug.LogWarning("[DragonController] No DragonRiderVisual — rider will be " +
//                             "invisible. Add DragonRiderVisual to a child of RiderSeat.", this);
//        }

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