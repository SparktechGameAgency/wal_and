////using System.Collections;
////using UnityEngine;

/////// <summary>
/////// AREA FORGE - SoldierController
/////// Controls soldier patrol behaviour inside the Village panel.
/////// The soldier walks left → right → left, flipping to face the direction of movement.
/////// Periodically stops mid-patrol to play an Idle (rest) animation, then resumes walking.
///////
/////// ── Animator setup required ──────────────────────────────────────────────────
///////   Parameters (all Bool):
///////     • IsWalking  — true while patrolling
///////     • IsIdle     — true while resting (NEW)
///////     • IsDead     — true on death
///////
///////   Transitions:
///////     Entry     → Walk   (no condition — plays immediately on spawn)
///////     Walk      → Idle   condition: IsIdle = true
///////     Idle      → Walk   condition: IsWalking = true  (and IsIdle = false)
///////     Any State → Dead   condition: IsDead = true
/////// ─────────────────────────────────────────────────────────────────────────────
/////// </summary>
////[RequireComponent(typeof(Animator))]
////[RequireComponent(typeof(SoldierStats))]
////public class SoldierController : MonoBehaviour
////{
////    // ─── Patrol Settings ──────────────────────────────────────────────────────

////    [Header("Patrol Area")]
////    [Tooltip("Left boundary X position in world space")]
////    [SerializeField] private float patrolLeftX = -4f;
////    [Tooltip("Right boundary X position in world space")]
////    [SerializeField] private float patrolRightX = 4f;
////    [Tooltip("Movement speed (units per second)")]
////    [SerializeField] private float moveSpeed = 0.5f;

////    // ─── Rest / Idle Settings ─────────────────────────────────────────────────

////    [Header("Rest Behaviour")]
////    [Tooltip("Minimum seconds the soldier walks before resting")]
////    [SerializeField] private float restIntervalMin = 3f;
////    [Tooltip("Maximum seconds the soldier walks before resting")]
////    [SerializeField] private float restIntervalMax = 7f;
////    [Tooltip("Minimum seconds the soldier stays idle")]
////    [SerializeField] private float restDurationMin = 1.5f;
////    [Tooltip("Maximum seconds the soldier stays idle")]
////    [SerializeField] private float restDurationMax = 3.5f;

////    // ─── Animation Parameter Names ────────────────────────────────────────────
////    // Must match EXACTLY the names in your Animator Controller Parameters tab.
////    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
////    private static readonly int AnimIsIdle = Animator.StringToHash("IsIdle");
////    private static readonly int AnimIsDead = Animator.StringToHash("IsDead");

////    // ─── Private State ────────────────────────────────────────────────────────

////    private Animator _animator;
////    private SoldierStats _stats;
////    private int _direction = 1;
////    private bool _isDead = false;
////    private bool _isPatrolling = true;
////    private bool _isResting = false;
////    private bool _animatorReady = false;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _animator = GetComponent<Animator>();
////        _stats = GetComponent<SoldierStats>();

////        if (_animator.runtimeAnimatorController == null)
////        {
////            Debug.LogError(
////                $"[SoldierController] '{name}' has an Animator but NO Controller assigned!\n" +
////                "→ Prefab → Animator → Controller field → assign SolderAnimator.\n" +
////                "Movement will still work but animations will NOT play.");
////            _animatorReady = false;
////        }
////        else
////        {
////            _animatorReady = true;
////        }

////        _stats.OnSoldierDied += HandleDeath;
////    }

////    private void OnDestroy()
////    {
////        if (_stats != null)
////            _stats.OnSoldierDied -= HandleDeath;
////    }

////    private void Start()
////    {
////        // Clamp spawn position into the patrol area
////        Vector3 pos = transform.position;
////        pos.x = Mathf.Clamp(pos.x, patrolLeftX, patrolRightX);
////        transform.position = pos;

////        ApplyFlip(_direction);
////        StartPatrol();

////        // Kick off the rest cycle loop
////        StartCoroutine(RestCycle());
////    }

////    private void Update()
////    {
////        // Skip movement while dead OR while resting
////        if (_isDead || !_isPatrolling || _isResting) return;
////        MovePatrol();
////    }

////    // ─── Patrol Logic ─────────────────────────────────────────────────────────

////    private void StartPatrol()
////    {
////        _isPatrolling = true;
////        _isResting = false;
////        SetAnimBool(AnimIsWalking, true);
////        SetAnimBool(AnimIsIdle, false);
////    }

////    private void StopPatrol()
////    {
////        _isPatrolling = false;
////        SetAnimBool(AnimIsWalking, false);
////    }

////    private void MovePatrol()
////    {
////        float step = _direction * moveSpeed * Time.deltaTime;
////        transform.Translate(step, 0f, 0f);

////        float x = transform.position.x;

////        if (_direction == 1 && x >= patrolRightX)
////        {
////            ClampX(patrolRightX);
////            SetDirection(-1);
////        }
////        else if (_direction == -1 && x <= patrolLeftX)
////        {
////            ClampX(patrolLeftX);
////            SetDirection(1);
////        }
////    }

////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////    /// <summary>
////    /// Loops forever:
////    ///   1. Walk for a random interval  (restIntervalMin – restIntervalMax seconds)
////    ///   2. Stop → play Idle animation  (restDurationMin – restDurationMax seconds)
////    ///   3. Resume walking
////    /// The coroutine exits cleanly when the soldier dies.
////    /// </summary>
////    private IEnumerator RestCycle()
////    {
////        while (!_isDead)
////        {
////            // ── 1. Walk for a random interval ─────────────────────────────────
////            float walkTime = Random.Range(restIntervalMin, restIntervalMax);
////            yield return new WaitForSeconds(walkTime);

////            if (_isDead) yield break;

////            // ── 2. Stop and play Idle ─────────────────────────────────────────
////            _isResting = true;                      // pauses MovePatrol()
////            SetAnimBool(AnimIsWalking, false);
////            SetAnimBool(AnimIsIdle, true);

////            float restTime = Random.Range(restDurationMin, restDurationMax);
////            yield return new WaitForSeconds(restTime);

////            if (_isDead) yield break;

////            // ── 3. Resume walking ─────────────────────────────────────────────
////            SetAnimBool(AnimIsIdle, false);
////            SetAnimBool(AnimIsWalking, true);
////            _isResting = false;                     // resumes MovePatrol()
////        }
////    }

////    // ─── Direction & Flip ─────────────────────────────────────────────────────

////    private void SetDirection(int dir)
////    {
////        _direction = dir;
////        ApplyFlip(dir);
////    }

////    /// <summary>
////    /// Mirrors the soldier using localScale.x.
////    /// Works for both UI Image and SpriteRenderer components.
////    /// </summary>
////    private void ApplyFlip(int dir)
////    {
////        Vector3 s = transform.localScale;
////        s.x = Mathf.Abs(s.x) * dir;   // dir 1 = normal, -1 = mirrored
////        transform.localScale = s;
////    }

////    private void ClampX(float x)
////    {
////        Vector3 pos = transform.position;
////        pos.x = x;
////        transform.position = pos;
////    }

////    // ─── Death ────────────────────────────────────────────────────────────────

////    private void HandleDeath(SoldierStats stats)
////    {
////        _isDead = true;
////        StopPatrol();
////        SetAnimBool(AnimIsIdle, false);
////        SetAnimBool(AnimIsDead, true);

////        Debug.Log($"[SoldierController] Soldier '{name}' has died.");
////    }

////    // ─── Animator Helper ──────────────────────────────────────────────────────

////    private void SetAnimBool(int hash, bool value)
////    {
////        if (_animatorReady)
////            _animator.SetBool(hash, value);
////    }

////    // ─── Public Controls ─────────────────────────────────────────────────────

////    /// <summary>Pause/resume patrol externally (e.g. during combat).</summary>
////    public void SetPatrolling(bool active)
////    {
////        if (_isDead) return;
////        if (active) StartPatrol();
////        else StopPatrol();
////    }

////    /// <summary>Reposition the patrol area at runtime.</summary>
////    public void SetPatrolBounds(float leftX, float rightX)
////    {
////        patrolLeftX = leftX;
////        patrolRightX = rightX;
////    }

////    // ─── Editor Gizmos ───────────────────────────────────────────────────────
////#if UNITY_EDITOR
////    private void OnDrawGizmosSelected()
////    {
////        Gizmos.color = Color.cyan;
////        float y = transform.position.y;
////        float z = transform.position.z;
////        Gizmos.DrawLine(new Vector3(patrolLeftX, y, z), new Vector3(patrolRightX, y, z));
////        Gizmos.color = Color.yellow;
////        Gizmos.DrawSphere(new Vector3(patrolLeftX, y, z), 0.15f);
////        Gizmos.DrawSphere(new Vector3(patrolRightX, y, z), 0.15f);
////    }
////#endif
////}

//using System.Collections;
//using UnityEngine;

///// <summary>
///// AREA FORGE - SoldierController
/////
///// Controls patrol movement, idle resting, flipping, and death.
///// Tells SpriteLayerAnimator which animation state is active so all
///// equipment layers animate in sync — no per-item Animator needed.
/////
///// ── What drives what ────────────────────────────────────────────────────────
/////   SoldierController  → calls _spriteAnim.SetState(Walk / Idle / Death)
/////   SpriteLayerAnimator→ steps frames on every equipped Image layer
/////   Animator (Unity)   → ONLY drives position/movement (the root transform)
/////                        NOT used for sprite swapping anymore
///// ────────────────────────────────────────────────────────────────────────────
///// </summary>
//[RequireComponent(typeof(SoldierStats))]
//[RequireComponent(typeof(SpriteLayerAnimator))]
//public class SoldierController : MonoBehaviour
//{
//    // ─── Patrol Settings ──────────────────────────────────────────────────────

//    [Header("Patrol Area")]
//    [SerializeField] private float patrolLeftX = -4f;
//    [SerializeField] private float patrolRightX = 4f;
//    [SerializeField] private float moveSpeed = 0.5f;

//    // ─── Rest Settings ────────────────────────────────────────────────────────

//    [Header("Rest Behaviour")]
//    [SerializeField] private float restIntervalMin = 3f;
//    [SerializeField] private float restIntervalMax = 7f;
//    [SerializeField] private float restDurationMin = 1.5f;
//    [SerializeField] private float restDurationMax = 3.5f;

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private SoldierStats _stats;
//    private SpriteLayerAnimator _spriteAnim;

//    private int _direction = 1;
//    private bool _isDead = false;
//    private bool _isPatrolling = true;
//    private bool _isResting = false;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _stats = GetComponent<SoldierStats>();
//        _spriteAnim = GetComponent<SpriteLayerAnimator>();

//        _stats.OnSoldierDied += HandleDeath;
//    }

//    private void OnDestroy()
//    {
//        if (_stats != null)
//            _stats.OnSoldierDied -= HandleDeath;
//    }

//    private void Start()
//    {
//        // Clamp spawn inside patrol area
//        Vector3 pos = transform.position;
//        pos.x = Mathf.Clamp(pos.x, patrolLeftX, patrolRightX);
//        transform.position = pos;

//        ApplyFlip(_direction);
//        StartPatrol();
//        StartCoroutine(RestCycle());
//    }

//    private void Update()
//    {
//        if (_isDead || !_isPatrolling || _isResting) return;
//        MovePatrol();
//    }

//    // ─── Patrol ───────────────────────────────────────────────────────────────

//    private void StartPatrol()
//    {
//        _isPatrolling = true;
//        _isResting = false;

//        // Tell SpriteLayerAnimator: switch all layers to Walk sprites
//        _spriteAnim.SetState(AnimationState.Walk);
//    }

//    private void StopPatrol()
//    {
//        _isPatrolling = false;

//        // Tell SpriteLayerAnimator: switch all layers to Idle sprites
//        _spriteAnim.SetState(AnimationState.Idle);
//    }

//    private void MovePatrol()
//    {
//        transform.Translate(_direction * moveSpeed * Time.deltaTime, 0f, 0f);

//        float x = transform.position.x;

//        if (_direction == 1 && x >= patrolRightX)
//        {
//            ClampX(patrolRightX);
//            SetDirection(-1);
//        }
//        else if (_direction == -1 && x <= patrolLeftX)
//        {
//            ClampX(patrolLeftX);
//            SetDirection(1);
//        }
//    }

//    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//    private IEnumerator RestCycle()
//    {
//        while (!_isDead)
//        {
//            // Walk for a random interval
//            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//            if (_isDead) yield break;

//            // Begin rest — stop movement, switch to Idle animation
//            _isResting = true;
//            _spriteAnim.SetState(AnimationState.Idle);

//            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//            if (_isDead) yield break;

//            // Resume walk — switch back to Walk animation
//            _spriteAnim.SetState(AnimationState.Walk);
//            _isResting = false;
//        }
//    }

//    // ─── Death ────────────────────────────────────────────────────────────────

//    private void HandleDeath(SoldierStats stats)
//    {
//        _isDead = true;
//        StopPatrol();

//        // Tell SpriteLayerAnimator: switch all layers to Death sprites
//        // Each EquipmentItem's deathSprites array will play once
//        _spriteAnim.SetState(AnimationState.Death);

//        Debug.Log($"[SoldierController] '{name}' died.");
//    }

//    // ─── Flip ─────────────────────────────────────────────────────────────────

//    private void SetDirection(int dir)
//    {
//        _direction = dir;
//        ApplyFlip(dir);
//    }

//    /// <summary>
//    /// Mirrors the soldier using localScale.x — works for UI Image and SpriteRenderer.
//    /// </summary>
//    private void ApplyFlip(int dir)
//    {
//        Vector3 s = transform.localScale;
//        s.x = Mathf.Abs(s.x) * dir;
//        transform.localScale = s;
//    }

//    private void ClampX(float x)
//    {
//        Vector3 pos = transform.position;
//        pos.x = x;
//        transform.position = pos;
//    }

//    // ─── Public Controls ─────────────────────────────────────────────────────

//    public void SetPatrolling(bool active)
//    {
//        if (_isDead) return;
//        if (active) StartPatrol();
//        else StopPatrol();
//    }

//    public void SetPatrolBounds(float leftX, float rightX)
//    {
//        patrolLeftX = leftX;
//        patrolRightX = rightX;
//    }

//    // ─── Gizmos ───────────────────────────────────────────────────────────────
//#if UNITY_EDITOR
//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.cyan;
//        float y = transform.position.y;
//        float z = transform.position.z;
//        Gizmos.DrawLine(new Vector3(patrolLeftX, y, z), new Vector3(patrolRightX, y, z));
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawSphere(new Vector3(patrolLeftX, y, z), 0.15f);
//        Gizmos.DrawSphere(new Vector3(patrolRightX, y, z), 0.15f);
//    }
//#endif
//}

using System.Collections;
using UnityEngine;

/// <summary>
/// AREA FORGE - SoldierController
///
/// ── BUG FIX: Flip never triggered ─────────────────────────────────────────────
///   OLD: used transform.position.x (world/screen-pixel coords on a UI Canvas).
///        patrolLeftX = -4, patrolRightX = 4 never matched screen pixel values
///        like 300, 600 → direction check always false → soldier never flipped.
///
///   FIX: All movement and bounds checks now use transform.localPosition.x
///        (local canvas units, consistent with the small patrol bound values).
///        SetPatrolBoundsFromCurrentPosition() auto-calculates left/right offset
///        from wherever the soldier spawns, so you never need to tweak bounds.
///
/// ── Patrol pattern ─────────────────────────────────────────────────────────────
///   Walk right → reach right bound → flip left → walk left → reach left bound
///   → flip right → ... plus random idle rests in between.
///
/// ── Animation ─────────────────────────────────────────────────────────────────
///   No Unity Animator used for sprites.
///   SpriteLayerAnimator handles all per-layer frame stepping.
///   This script only calls SetState(Walk / Idle / Death).
/// </summary>
[RequireComponent(typeof(SoldierStats))]
[RequireComponent(typeof(SpriteLayerAnimator))]
public class SoldierController : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Patrol Area (local canvas units — relative to spawn position)")]
    [Tooltip("How far LEFT of the spawn point the soldier walks (positive value).")]
    [SerializeField] private float patrolHalfWidth = 80f;   // canvas units each side

    [Tooltip("Movement speed in local canvas units per second.")]
    [SerializeField] private float moveSpeed = 40f;

    [Header("Rest Behaviour")]
    [SerializeField] private float restIntervalMin = 3f;
    [SerializeField] private float restIntervalMax = 7f;
    [SerializeField] private float restDurationMin = 1.5f;
    [SerializeField] private float restDurationMax = 3.5f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;
    private SpriteLayerAnimator _spriteAnim;
    private RectTransform _rect;

    // Patrol bounds in LOCAL space (set in Start from spawn position)
    private float _leftBound;
    private float _rightBound;

    private int _direction = 1;       // 1 = right, -1 = left
    private bool _isDead = false;
    private bool _isPatrolling = true;
    private bool _isResting = false;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _stats = GetComponent<SoldierStats>();
        _spriteAnim = GetComponent<SpriteLayerAnimator>();
        _rect = GetComponent<RectTransform>();

        _stats.OnSoldierDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnSoldierDied -= HandleDeath;
    }

    private void Start()
    {
        // ── Calculate patrol bounds in LOCAL space from spawn position ─────────
        // This means the soldier patrols ±patrolHalfWidth canvas units around
        // wherever it spawns — no manual bound tweaking needed.
        float spawnLocalX = _rect != null
            ? _rect.anchoredPosition.x
            : transform.localPosition.x;

        _leftBound = spawnLocalX - patrolHalfWidth;
        _rightBound = spawnLocalX + patrolHalfWidth;

        ApplyFlip(_direction);
        StartPatrol();
        StartCoroutine(RestCycle());
    }

    private void Update()
    {
        if (_isDead || !_isPatrolling || _isResting) return;
        MovePatrol();
    }

    // ─── Patrol ───────────────────────────────────────────────────────────────

    private void StartPatrol()
    {
        _isPatrolling = true;
        _isResting = false;
        _spriteAnim.SetState(AnimationState.Walk);
    }

    private void StopPatrol()
    {
        _isPatrolling = false;
        _spriteAnim.SetState(AnimationState.Idle);
    }

    private void MovePatrol()
    {
        // ── Move in LOCAL space ────────────────────────────────────────────────
        float step = _direction * moveSpeed * Time.deltaTime;

        if (_rect != null)
        {
            // UI RectTransform: move via anchoredPosition
            _rect.anchoredPosition += new Vector2(step, 0f);
            float x = _rect.anchoredPosition.x;

            if (_direction == 1 && x >= _rightBound)
            {
                var pos = _rect.anchoredPosition;
                pos.x = _rightBound;
                _rect.anchoredPosition = pos;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var pos = _rect.anchoredPosition;
                pos.x = _leftBound;
                _rect.anchoredPosition = pos;
                SetDirection(1);
            }
        }
        else
        {
            // Fallback for non-UI (SpriteRenderer) soldiers
            transform.Translate(step, 0f, 0f);
            float x = transform.localPosition.x;

            if (_direction == 1 && x >= _rightBound)
            {
                var pos = transform.localPosition; pos.x = _rightBound;
                transform.localPosition = pos;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var pos = transform.localPosition; pos.x = _leftBound;
                transform.localPosition = pos;
                SetDirection(1);
            }
        }
    }

    // ─── Rest Cycle ───────────────────────────────────────────────────────────

    private IEnumerator RestCycle()
    {
        while (!_isDead)
        {
            yield return new WaitForSeconds(
                Random.Range(restIntervalMin, restIntervalMax));
            if (_isDead) yield break;

            // Begin rest
            _isResting = true;
            _spriteAnim.SetState(AnimationState.Idle);

            yield return new WaitForSeconds(
                Random.Range(restDurationMin, restDurationMax));
            if (_isDead) yield break;

            // Resume walk
            _spriteAnim.SetState(AnimationState.Walk);
            _isResting = false;
        }
    }

    // ─── Death ────────────────────────────────────────────────────────────────

    private void HandleDeath(SoldierStats _)
    {
        _isDead = true;
        StopPatrol();
        _spriteAnim.SetState(AnimationState.Death);
        Debug.Log($"[SoldierController] '{name}' died.");
    }

    // ─── Flip ─────────────────────────────────────────────────────────────────

    private void SetDirection(int dir)
    {
        _direction = dir;
        ApplyFlip(dir);
    }

    /// <summary>
    /// Flips the soldier by negating localScale.x.
    /// Works for both UI Image layers and SpriteRenderer.
    /// dir = 1  → facing right (normal)
    /// dir = -1 → facing left  (mirrored)
    /// </summary>
    private void ApplyFlip(int dir)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    // ─── Public Controls ─────────────────────────────────────────────────────

    /// <summary>
    /// Pause (false) or resume (true) patrol — called by SoldierDragDrop
    /// and any other system that needs to freeze the soldier.
    /// </summary>
    public void SetPatrolling(bool active)
    {
        if (_isDead) return;
        if (active) StartPatrol();
        else StopPatrol();
    }

    /// <summary>
    /// Override the patrol half-width at runtime (e.g. after spawning in
    /// a different-sized area).
    /// </summary>
    public void SetPatrolHalfWidth(float halfWidth)
    {
        float centre = (_leftBound + _rightBound) * 0.5f;
        _leftBound = centre - halfWidth;
        _rightBound = centre + halfWidth;
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Show patrol bounds in Scene view (works in world space approximation)
        float y = transform.position.y;
        float z = transform.position.z;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z),
            new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z));
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(
            new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z), 0.1f);
        Gizmos.DrawSphere(
            new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z), 0.1f);
    }
#endif
}