using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — DragonController
///
/// Attach to both the plain dragon prefab and the rider dragon prefab.
///
/// ════════════════════════════════════════════════════════════════════
///  STATES
/// ════════════════════════════════════════════════════════════════════
///
///  Idle      Dragon sits inside DragonArea.
///  Dragging  Dragon follows the pointer at canvas-root level.
///  Flying    Dragon patrols left↔right inside a FlyZone.
///
/// ════════════════════════════════════════════════════════════════════
///  TWO-PREFAB RIDER SWAP
/// ════════════════════════════════════════════════════════════════════
///
///  Two separate dragon prefabs exist in the project:
///
///    PlainDragon   — no rider, draggable by the player.
///    RiderDragon   — rider visuals baked in, soldier parented to its
///                    DragonRiderSeat at runtime.
///
///  When a soldier is dropped on the plain dragon:
///    1. PerformMount() spawns the rider variant at the same position.
///    2. All patrol state (zone, direction, flip, homeSlot) is copied.
///    3. The soldier is mounted on the rider's DragonRiderSeat.
///    4. The plain dragon is deactivated (not destroyed — reused on dismount).
///
///  When the soldier leaves the rider dragon:
///    1. PerformDismount() spawns the plain variant back.
///    2. State is copied again.
///    3. The rider dragon is destroyed.
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR SETUP — PLAIN DRAGON PREFAB
/// ════════════════════════════════════════════════════════════════════
///
///  dragonData           Your DragonData ScriptableObject.
///  riderVariantPrefab   Drag the RiderDragon prefab here.
///  plainVariantPrefab   Leave BLANK on the plain dragon.
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR SETUP — RIDER DRAGON PREFAB
/// ════════════════════════════════════════════════════════════════════
///
///  dragonData           Same DragonData ScriptableObject.
///  riderVariantPrefab   Leave BLANK on the rider dragon.
///  plainVariantPrefab   Drag the PlainDragon prefab here.
///
///  The rider dragon MUST have a DragonRiderSeat child so the soldier
///  can be reparented under it.
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP — OTHER
/// ════════════════════════════════════════════════════════════════════
///
///  1. Both prefabs need a CanvasGroup (auto-required below).
///  2. FlyZone.cs must be on your FlyZone GameObject with a Graphic
///     component so the EventSystem can raycast it.
///  3. In DragonEggSlot.EnterHatched(), after spawning the plain dragon:
///        var dc = _spawnedDragon.GetComponent<DragonController>();
///        if (dc != null) dc.homeSlot = this;
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
    [Tooltip("Tick if your dragon sprite naturally faces LEFT at localScale.x = +1.")]
    [SerializeField] private bool spriteDefaultFacesLeft = true;

    // ── Rider Variant Prefab Swap ──────────────────────────────────────────────

    [Header("Rider Variant Prefab Swap")]
    [Tooltip("PLAIN DRAGON: drag the RiderDragon prefab here.\n\n" +
             "When a soldier mounts, this prefab is spawned in place of the plain " +
             "dragon. Leave blank to use the classic in-place mount (soldier sits " +
             "on this dragon's own RiderSeat instead).")]
    [SerializeField] private GameObject riderVariantPrefab;

    [Tooltip("RIDER DRAGON: drag the PlainDragon prefab here.\n\n" +
             "When the soldier dismounts, this prefab is restored. " +
             "Leave blank on the plain dragon.")]
    [SerializeField] private GameObject plainVariantPrefab;

    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────

    /// <summary>The DragonArea slot this dragon hatched from.</summary>
    [HideInInspector] public DragonEggSlot homeSlot;

    // ── Private ───────────────────────────────────────────────────────────────

    private DragonWingAnimator _wingAnimator;
    private RectTransform _rt;
    private Animator _anim;
    private CanvasGroup _cg;

    // Saved before every drag so we can snap back on an invalid drop.
    private Transform _savedParent;
    private Vector2 _savedAnchoredPos;
    private int _savedSiblingIndex;

    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea).
    private FlyZone _currentZone;

    // Drag offset — keeps the dragon under the grab point, not the pointer centre.
    private Vector2 _dragOffset;

    // Patrol direction: +1 = right, -1 = left.
    private float _patrolDir = 1f;

    // True after TransferStateFrom() has already called EnterIdle/Flying,
    // so Start() does not override it with a second EnterIdle().
    private bool _stateTransferred;

    // ── State ─────────────────────────────────────────────────────────────────

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

        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);

        if (_wingAnimator == null)
            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
                             "Add DragonWingAnimator to the DragonWing child.", this);
    }

    private void Start()
    {
        // Skip if TransferStateFrom() already put us in the correct state.
        if (!_stateTransferred)
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
        // Block dragging the dragon while a soldier is riding it.
        var seat = GetComponentInChildren<DragonRiderSeat>();
        if (seat != null && seat.IsOccupied)
        {
            Debug.Log("[DragonController] Drag blocked — soldier is riding this dragon.");
            return;
        }

        // Snapshot position so we can snap back on an invalid drop.
        _savedParent = _rt.parent;
        _savedAnchoredPos = _rt.anchoredPosition;
        _savedSiblingIndex = _rt.GetSiblingIndex();

        // Move to canvas root so the dragon draws on top of all panels.
        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        // Calculate grab offset AFTER reparenting so anchoredPosition is already
        // in canvas space — prevents the dragon jumping on the first drag frame.
        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            uiCamBegin,
            out Vector2 pointerCanvasPos);
        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

        // Semi-transparent while dragging; disable raycasts so zones are hit.
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
            eventData.position,
            uiCam,
            out Vector2 localPos);

        _rt.anchoredPosition = localPos + _dragOffset;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — END  (zone detection + state transition)
    // ══════════════════════════════════════════════════════════════════════════

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore opacity but keep blocksRaycasts FALSE until AFTER the raycast —
        // otherwise the dragon's own CanvasGroup would shadow the zone below it.
        _cg.alpha = 1f;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        _cg.blocksRaycasts = true;

        FlyZone hitFlyZone = null;
        DragonEggSlot hitAreaSlot = null;

        foreach (var r in results)
        {
            // GetComponentInParent so hitting any child of the zone/area still counts.
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
            // Reparent directly to the slot, not to _savedParent (which would be the
            // FlyZone when dragging from patrol, causing a wrong re-parent).
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

        TriggerAnim(dragonData?.dragonIdleTrigger);
        _wingAnimator?.SetState(DragonWingAnimator.WingState.Idle);
        Debug.Log("[DragonController] → Idle");
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

        TriggerAnim(dragonData?.dragonFlyTrigger);
        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
    }

    private void DoPatrol()
    {
        if (_currentZone == null) return;

        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
        float halfWidth = _currentZone.PatrolHalfWidth;
        float currentX = _rt.anchoredPosition.x;
        float newX = currentX + _patrolDir * speed * Time.deltaTime;

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
    // PREFAB SWAP — MOUNT  (called by SoldierDragDrop.OnEndDrag)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by SoldierDragDrop when a soldier is dropped on this (plain) dragon.
    ///
    /// ── If riderVariantPrefab IS assigned (normal path) ───────────────────────
    ///   1. Instantiates the rider variant as a sibling at the same
    ///      parent, anchoredPosition, localScale, and sibling index.
    ///   2. Calls TransferStateFrom(this) on the new dragon so it
    ///      immediately continues the same patrol without a reset.
    ///   3. Mounts the soldier on the rider variant's DragonRiderSeat.
    ///   4. Deactivates this plain dragon (preserved for reuse on dismount).
    ///
    /// ── If riderVariantPrefab is NULL (fallback) ──────────────────────────────
    ///   Falls back to the original in-place behaviour: soldier is mounted on
    ///   this dragon's own DragonRiderSeat (classic system).
    ///
    /// CALL ORDER: SoldierDragDrop must save _mountHomeParent and _mountHomePos
    /// BEFORE calling this method.
    /// </summary>
    public void PerformMount(SoldierDragDrop soldier, DragonRiderSeat fallbackSeat)
    {
        if (riderVariantPrefab == null)
        {
            // No swap configured — classic in-place mount.
            fallbackSeat.MountSoldier(soldier);
            return;
        }

        // ── Spawn rider variant ───────────────────────────────────────────────
        var riderGO = Instantiate(riderVariantPrefab, transform.parent);
        var riderRT = riderGO.GetComponent<RectTransform>();
        riderRT.anchoredPosition = _rt.anchoredPosition;
        riderGO.transform.localScale = transform.localScale;
        riderGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // ── Transfer patrol state ─────────────────────────────────────────────
        var riderDC = riderGO.GetComponent<DragonController>();
        if (riderDC != null)
            riderDC.TransferStateFrom(this);

        // ── Mount soldier on the rider variant's seat ─────────────────────────
        var riderSeat = riderGO.GetComponentInChildren<DragonRiderSeat>();
        if (riderSeat != null)
        {
            riderSeat.MountSoldier(soldier);
        }
        else
        {
            Debug.LogError("[DragonController] Rider variant prefab has no DragonRiderSeat " +
                           "child! Add a DragonRiderSeat child to the rider dragon prefab.", riderGO);
        }

        // ── Hide plain dragon (keep alive for potential pool reuse) ───────────
        gameObject.SetActive(false);
        Debug.Log($"[DragonController] '{name}' swapped → rider variant for '{soldier.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PREFAB SWAP — DISMOUNT  (called by SoldierDragDrop after soldier leaves)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by SoldierDragDrop when the soldier leaves this (rider) dragon.
    ///
    /// IMPORTANT — call this ONLY after the soldier has been reparented away
    /// from the seat (e.g. to the root canvas or to their ground home). If
    /// called while the soldier is still a child of this dragon, the soldier
    /// will be destroyed along with this GameObject.
    ///
    /// ── If plainVariantPrefab IS assigned (normal path) ───────────────────────
    ///   1. Instantiates the plain dragon at the same parent, position, scale.
    ///   2. Calls TransferStateFrom(this) so patrol resumes seamlessly.
    ///   3. Destroys this rider dragon.
    ///
    /// ── If plainVariantPrefab is NULL ─────────────────────────────────────────
    ///   Logs a warning and does nothing — set it in the rider dragon's Inspector.
    /// </summary>
    public void PerformDismount()
    {
        if (plainVariantPrefab == null)
        {
            Debug.LogWarning("[DragonController] PerformDismount: plainVariantPrefab is not " +
                             "set on this rider variant. Assign it in the Inspector.", this);
            return;
        }

        // ── Spawn plain dragon ────────────────────────────────────────────────
        var plainGO = Instantiate(plainVariantPrefab, transform.parent);
        var plainRT = plainGO.GetComponent<RectTransform>();
        plainRT.anchoredPosition = _rt.anchoredPosition;
        plainGO.transform.localScale = transform.localScale;
        plainGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // ── Transfer patrol state ─────────────────────────────────────────────
        var plainDC = plainGO.GetComponent<DragonController>();
        if (plainDC != null)
            plainDC.TransferStateFrom(this);

        // ── Remove rider dragon ───────────────────────────────────────────────
        Debug.Log($"[DragonController] '{name}' swapped → plain variant.");
        Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE TRANSFER  (shared by PerformMount and PerformDismount)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Copies all patrol state from <paramref name="source"/> and immediately
    /// enters the matching animation state (Idle or Flying).
    ///
    /// Sets _stateTransferred = true so Start() does not override the state
    /// with its own EnterIdle() call on the next frame.
    ///
    /// Called on the newly spawned dragon immediately after Instantiate(),
    /// before Start() has fired, so the one-frame lag is avoided entirely.
    /// </summary>
    public void TransferStateFrom(DragonController source)
    {
        _stateTransferred = true;

        homeSlot = source.homeSlot;
        _currentZone = source._currentZone;
        _patrolDir = source._patrolDir;

        // Sync position and scale — the caller sets these too, but doing it
        // here as well guards against any future call-order changes.
        _rt.anchoredPosition = source._rt.anchoredPosition;
        transform.localScale = source.transform.localScale;

        if (source.State == DragonState.Flying)
        {
            State = DragonState.Flying;
            _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
            TriggerAnim(dragonData?.dragonFlyTrigger);
        }
        else
        {
            EnterIdle();
        }

        Debug.Log($"[DragonController] '{name}' received state from '{source.name}' " +
                  $"(State={source.State}, Zone={source._currentZone?.name ?? "none"}, " +
                  $"Dir={source._patrolDir}).");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Flip the sprite by negating localScale.x.</summary>
    private void FlipHorizontal()
    {
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    /// <summary>Restore the RectTransform to its pre-drag parent, position, and depth.</summary>
    private void ReturnToHome()
    {
        if (_savedParent == null) return;
        _rt.SetParent(_savedParent, worldPositionStays: false);
        _rt.SetSiblingIndex(_savedSiblingIndex);
        _rt.anchoredPosition = _savedAnchoredPos;
    }

    /// <summary>Invalid drop: put the dragon back where it was and resume its old state.</summary>
    private void SnapBack()
    {
        ReturnToHome();

        if (_currentZone != null)
        {
            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
            State = DragonState.Flying;
            Debug.Log("[DragonController] SnapBack → resume Flying");
        }
        else
        {
            EnterIdle();
            Debug.Log("[DragonController] SnapBack → resume Idle");
        }
    }

    /// <summary>Fire an Animator trigger by name with warnings for common misconfigurations.</summary>
    private void TriggerAnim(string trigger)
    {
        if (_anim == null)
        {
            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
            return;
        }
        if (dragonData == null)
        {
            Debug.LogWarning("[DragonController] DragonData is not assigned. " +
                             "Drag your DragonData ScriptableObject into the Inspector.", this);
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