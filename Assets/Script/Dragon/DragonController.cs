//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// DRAGON CONTROLLER
/////
///// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
///// DragonEggSlot assigns homeSlot after it instantiates this prefab.
/////
///// ════════════════════════════════════════════════════════════════════
/////  STATES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Idle      Dragon sits inside DragonArea playing its idle animation.
/////            It can be picked up and dragged.
/////
/////  Dragging  Dragon follows the pointer at canvas-root level,
/////            semi-transparent, raycasts pass through it.
/////
/////  Flying    Dragon was dropped on a FlyZone.
/////            It plays the fly animation and patrols left↔right
/////            inside the zone, flipping its sprite at each edge.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DROP RULES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
/////  Drop on DragonArea   → EnterIdle    (reparented to saved home)
/////  Drop anywhere else   → SnapBack     (return to previous state)
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Add this script to your dragon prefab.
/////  2. Give the prefab a CanvasGroup component (auto-created if missing).
/////  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
/////     (e.g. a transparent Image) so the EventSystem can raycast it.
/////  4. In DragonEggSlot.EnterHatched(), after spawning, add:
/////        var dc = _spawnedDragon.GetComponent<DragonController>();
/////        if (dc != null) dc.homeSlot = this;
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
//    [Tooltip("Tick this if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
//             "The controller flips the scale to match the patrol direction.")]
//    [SerializeField] private bool spriteDefaultFacesLeft = true;

//    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
//    /// <summary>The DragonArea slot this dragon hatched from.</summary>
//    [HideInInspector] public DragonEggSlot homeSlot;

//    // ── Private ───────────────────────────────────────────────────────────────
//    private RectTransform _rt;
//    private Animator _anim;
//    private CanvasGroup _cg;

//    // Saved before every drag so we can snap back on an invalid drop
//    private Transform _savedParent;
//    private Vector2 _savedAnchoredPos;
//    private int _savedSiblingIndex;

//    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
//    private FlyZone _currentZone;

//    // Drag offset — keeps the dragon under the grab point, not the pointer centre
//    private Vector2 _dragOffset;

//    // Patrol bookkeeping
//    private float _patrolDir = 1f;   // +1 right, -1 left

//    // ── State ─────────────────────────────────────────────────────────────────
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
//        // Snapshot current position so we can snap back if the drop is invalid
//        _savedParent = _rt.parent;
//        _savedAnchoredPos = _rt.anchoredPosition;
//        _savedSiblingIndex = _rt.GetSiblingIndex();

//        // Move to canvas root so the dragon draws on top of all panels
//        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
//        _rt.SetAsLastSibling();

//        // Calculate grab offset AFTER reparenting so anchoredPosition is already
//        // in canvas space. Prevents the dragon jumping on the first drag frame.
//        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null : rootCanvas.worldCamera;
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            rootCanvas.transform as RectTransform,
//            eventData.position,
//            uiCamBegin,
//            out Vector2 pointerCanvasPos);
//        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

//        // Semi-transparent while dragging; disable raycasts so zones are hit
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
//        // Convert screen-space pointer to canvas-local position
//        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//            ? null
//            : rootCanvas.worldCamera;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            rootCanvas.transform as RectTransform,
//            eventData.position,
//            uiCam,
//            out Vector2 localPos);

//        _rt.anchoredPosition = localPos + _dragOffset;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END  (zone detection + state transition)
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
//        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
//        // the FlyZone underneath is never detected.
//        _cg.alpha = 1f;

//        // ── Raycast everything under the pointer ──────────────────────────────
//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        // Now safe to restore — raycast is already done
//        _cg.blocksRaycasts = true;

//        FlyZone hitFlyZone = null;
//        DragonEggSlot hitAreaSlot = null;

//        foreach (var r in results)
//        {
//            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponent<FlyZone>();
//            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponent<DragonEggSlot>();
//            if (hitFlyZone != null && hitAreaSlot != null) break;
//        }

//        // ── Decide destination ────────────────────────────────────────────────
//        if (hitFlyZone != null)
//        {
//            // Dropped onto a Fly Zone → start flying + patrol
//            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
//            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
//            _currentZone = hitFlyZone;
//            EnterFlying();
//        }
//        else if (hitAreaSlot != null)
//        {
//            // Dropped onto any DragonArea (preferably its home) → back to idle
//            ReturnToHome();
//            _currentZone = null;
//            EnterIdle();
//        }
//        else
//        {
//            // Invalid drop → snap back to wherever it was before the drag
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — IDLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterIdle()
//    {
//        State = DragonState.Idle;
//        TriggerAnim(dragonData?.dragonIdleTrigger);
//        Debug.Log("[DragonController] → Idle");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — FLYING + PATROL
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterFlying()
//    {
//        State = DragonState.Flying;
//        _patrolDir = -1f;  // start moving left

//        // Set localScale.x so the sprite FACES LEFT on entry (matching patrolDir -1).
//        //   spriteDefaultFacesLeft = true  → positive scale = faces left  → Abs (natural)
//        //   spriteDefaultFacesLeft = false → positive scale = faces right → negate to face left
//        Vector3 s = transform.localScale;
//        s.x = spriteDefaultFacesLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
//        transform.localScale = s;

//        TriggerAnim(dragonData?.dragonFlyTrigger);
//        Debug.Log($"[DragonController] → Flying  zone={_currentZone?.name}");
//    }

//    private void DoPatrol()
//    {
//        if (_currentZone == null) return;

//        float speed = dragonData != null ? dragonData.patrolSpeed : 80f;
//        float halfWidth = _currentZone.PatrolHalfWidth;
//        float currentX = _rt.anchoredPosition.x;
//        float newX = currentX + _patrolDir * speed * Time.deltaTime;

//        // Bounce at patrol edges
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
//    // HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
//    private void FlipHorizontal()
//    {
//        Vector3 s = transform.localScale;
//        s.x = -s.x;
//        transform.localScale = s;
//    }

//    /// Restore the RectTransform to its pre-drag parent, position and depth.
//    private void ReturnToHome()
//    {
//        if (_savedParent == null) return;
//        _rt.SetParent(_savedParent, worldPositionStays: false);
//        _rt.SetSiblingIndex(_savedSiblingIndex);
//        _rt.anchoredPosition = _savedAnchoredPos;
//    }

//    /// Invalid drop: put the dragon back where it was and resume its old state.
//    private void SnapBack()
//    {
//        ReturnToHome();

//        // Resume previous state without re-triggering animations
//        if (_currentZone != null)
//        {
//            // Was flying before the drag — re-parent to the zone and resume
//            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
//            State = DragonState.Flying;   // keep flying, patrol continues in Update
//            Debug.Log("[DragonController] SnapBack → resume Flying");
//        }
//        else
//        {
//            EnterIdle();
//            Debug.Log("[DragonController] SnapBack → resume Idle");
//        }
//    }

//    /// Fire an Animator trigger by name with warnings for common misconfigurations.
//    private void TriggerAnim(string trigger)
//    {
//        if (_anim == null)
//        {
//            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
//            return;
//        }
//        if (dragonData == null)
//        {
//            Debug.LogWarning("[DragonController] DragonData is not assigned on the dragon prefab! " +
//                             "Drag your DragonData ScriptableObject into the DragonController Inspector field.", this);
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
/// DRAGON CONTROLLER
///
/// Attach to the dragon prefab (must have RectTransform + Animator + CanvasGroup).
/// DragonEggSlot assigns homeSlot after it instantiates this prefab.
///
/// ════════════════════════════════════════════════════════════════════
///  STATES
/// ════════════════════════════════════════════════════════════════════
///
///  Idle      Dragon sits inside DragonArea playing its idle animation.
///            It can be picked up and dragged.
///
///  Dragging  Dragon follows the pointer at canvas-root level,
///            semi-transparent, raycasts pass through it.
///
///  Flying    Dragon was dropped on a FlyZone.
///            It plays the fly animation and patrols left↔right
///            inside the zone, flipping its sprite at each edge.
///
/// ════════════════════════════════════════════════════════════════════
///  DROP RULES
/// ════════════════════════════════════════════════════════════════════
///
///  Drop on FlyZone      → EnterFlying  (reparented to FlyZone)
///  Drop on DragonArea   → EnterIdle    (reparented to saved home)
///  Drop anywhere else   → SnapBack     (return to previous state)
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP
/// ════════════════════════════════════════════════════════════════════
///
///  1. Add this script to your dragon prefab.
///  2. Give the prefab a CanvasGroup component (auto-created if missing).
///  3. Add FlyZone.cs to your FlyZone GameObject — it needs a Graphic
///     (e.g. a transparent Image) so the EventSystem can raycast it.
///  4. In DragonEggSlot.EnterHatched(), after spawning, add:
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
    [Tooltip("Tick this if your dragon sprite naturally faces LEFT at localScale.x = +1. " +
             "The controller flips the scale to match the patrol direction.")]
    [SerializeField] private bool spriteDefaultFacesLeft = true;

    // ── Assigned by DragonEggSlot after instantiation ─────────────────────────
    /// <summary>The DragonArea slot this dragon hatched from.</summary>
    [HideInInspector] public DragonEggSlot homeSlot;

    // ── Private ───────────────────────────────────────────────────────────────
    private DragonWingAnimator _wingAnimator;
    private RectTransform _rt;
    private Animator _anim;
    private CanvasGroup _cg;

    // Saved before every drag so we can snap back on an invalid drop
    private Transform _savedParent;
    private Vector2 _savedAnchoredPos;
    private int _savedSiblingIndex;

    // Which FlyZone the dragon is currently patrolling (null = idle in DragonArea)
    private FlyZone _currentZone;

    // Drag offset — keeps the dragon under the grab point, not the pointer centre
    private Vector2 _dragOffset;

    // Patrol bookkeeping
    private float _patrolDir = 1f;   // +1 right, -1 left

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

        // Find the wing animator on any child (the DragonWing child GameObject)
        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);

        if (_wingAnimator == null)
            Debug.LogWarning("[DragonController] No DragonWingAnimator found in children. " +
                             "Add DragonWingAnimator to the DragonWing child GameObject.", this);
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
        // Snapshot current position so we can snap back if the drop is invalid
        _savedParent = _rt.parent;
        _savedAnchoredPos = _rt.anchoredPosition;
        _savedSiblingIndex = _rt.GetSiblingIndex();

        // Move to canvas root so the dragon draws on top of all panels
        _rt.SetParent(rootCanvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        // Calculate grab offset AFTER reparenting so anchoredPosition is already
        // in canvas space. Prevents the dragon jumping on the first drag frame.
        Camera uiCamBegin = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            uiCamBegin,
            out Vector2 pointerCanvasPos);
        _dragOffset = _rt.anchoredPosition - pointerCanvasPos;

        // Semi-transparent while dragging; disable raycasts so zones are hit
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
        // Convert screen-space pointer to canvas-local position
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

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
        // Restore opacity first, but keep blocksRaycasts FALSE until AFTER the
        // raycast — otherwise the dragon's own CanvasGroup blocks the hit and
        // the FlyZone underneath is never detected.
        _cg.alpha = 1f;

        // ── Raycast everything under the pointer ──────────────────────────────
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Now safe to restore — raycast is already done
        _cg.blocksRaycasts = true;

        FlyZone hitFlyZone = null;
        DragonEggSlot hitAreaSlot = null;

        foreach (var r in results)
        {
            // GetComponentInParent so hitting any child of the zone/area still counts
            if (hitFlyZone == null) hitFlyZone = r.gameObject.GetComponentInParent<FlyZone>();
            if (hitAreaSlot == null) hitAreaSlot = r.gameObject.GetComponentInParent<DragonEggSlot>();
            if (hitFlyZone != null && hitAreaSlot != null) break;
        }

        // ── Decide destination ────────────────────────────────────────────────
        if (hitFlyZone != null)
        {
            // Dropped onto a Fly Zone → start flying + patrol
            _rt.SetParent(hitFlyZone.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;   // centre inside the zone
            _currentZone = hitFlyZone;
            EnterFlying();
        }
        else if (hitAreaSlot != null)
        {
            // Dropped onto DragonArea → reparent directly to the slot, not to
            // _savedParent (which would be the FlyZone when dragging from patrol).
            _rt.SetParent(hitAreaSlot.transform, worldPositionStays: false);
            _rt.anchoredPosition = Vector2.zero;
            _currentZone = null;
            EnterIdle();
        }
        else
        {
            // Invalid drop → snap back to wherever it was before the drag
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
        // ── CHANGE: tell the wing to play its idle animation
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
        // ── CHANGE: tell the wing to play its fly animation
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

        // Bounce at patrol edges
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
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// Flip the sprite by negating localScale.x (no need for a separate SpriteRenderer flip).
    private void FlipHorizontal()
    {
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    /// Restore the RectTransform to its pre-drag parent, position and depth.
    private void ReturnToHome()
    {
        if (_savedParent == null) return;
        _rt.SetParent(_savedParent, worldPositionStays: false);
        _rt.SetSiblingIndex(_savedSiblingIndex);
        _rt.anchoredPosition = _savedAnchoredPos;
    }

    /// Invalid drop: put the dragon back where it was and resume its old state.
    private void SnapBack()
    {
        ReturnToHome();

        // Resume previous state without re-triggering animations
        if (_currentZone != null)
        {
            // Was flying before the drag — re-parent to the zone and resume
            _rt.SetParent(_currentZone.transform, worldPositionStays: false);
            State = DragonState.Flying;   // keep flying, patrol continues in Update
            Debug.Log("[DragonController] SnapBack → resume Flying");
        }
        else
        {
            EnterIdle();
            Debug.Log("[DragonController] SnapBack → resume Idle");
        }
    }

    /// Fire an Animator trigger by name with warnings for common misconfigurations.
    private void TriggerAnim(string trigger)
    {
        if (_anim == null)
        {
            Debug.LogWarning("[DragonController] No Animator found on the dragon prefab!", this);
            return;
        }
        if (dragonData == null)
        {
            Debug.LogWarning("[DragonController] DragonData is not assigned on the dragon prefab! " +
                             "Drag your DragonData ScriptableObject into the DragonController Inspector field.", this);
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