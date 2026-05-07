////////////////////using System.Collections;
////////////////////using UnityEngine;

/////////////////////// <summary>
/////////////////////// AREA FORGE - SoldierController
/////////////////////// Controls soldier patrol behaviour inside the Village panel.
/////////////////////// The soldier walks left → right → left, flipping to face the direction of movement.
/////////////////////// Periodically stops mid-patrol to play an Idle (rest) animation, then resumes walking.
///////////////////////
/////////////////////// ── Animator setup required ──────────────────────────────────────────────────
///////////////////////   Parameters (all Bool):
///////////////////////     • IsWalking  — true while patrolling
///////////////////////     • IsIdle     — true while resting (NEW)
///////////////////////     • IsDead     — true on death
///////////////////////
///////////////////////   Transitions:
///////////////////////     Entry     → Walk   (no condition — plays immediately on spawn)
///////////////////////     Walk      → Idle   condition: IsIdle = true
///////////////////////     Idle      → Walk   condition: IsWalking = true  (and IsIdle = false)
///////////////////////     Any State → Dead   condition: IsDead = true
/////////////////////// ─────────────────────────────────────────────────────────────────────────────
/////////////////////// </summary>
////////////////////[RequireComponent(typeof(Animator))]
////////////////////[RequireComponent(typeof(SoldierStats))]
////////////////////public class SoldierController : MonoBehaviour
////////////////////{
////////////////////    // ─── Patrol Settings ──────────────────────────────────────────────────────

////////////////////    [Header("Patrol Area")]
////////////////////    [Tooltip("Left boundary X position in world space")]
////////////////////    [SerializeField] private float patrolLeftX = -4f;
////////////////////    [Tooltip("Right boundary X position in world space")]
////////////////////    [SerializeField] private float patrolRightX = 4f;
////////////////////    [Tooltip("Movement speed (units per second)")]
////////////////////    [SerializeField] private float moveSpeed = 0.5f;

////////////////////    // ─── Rest / Idle Settings ─────────────────────────────────────────────────

////////////////////    [Header("Rest Behaviour")]
////////////////////    [Tooltip("Minimum seconds the soldier walks before resting")]
////////////////////    [SerializeField] private float restIntervalMin = 3f;
////////////////////    [Tooltip("Maximum seconds the soldier walks before resting")]
////////////////////    [SerializeField] private float restIntervalMax = 7f;
////////////////////    [Tooltip("Minimum seconds the soldier stays idle")]
////////////////////    [SerializeField] private float restDurationMin = 1.5f;
////////////////////    [Tooltip("Maximum seconds the soldier stays idle")]
////////////////////    [SerializeField] private float restDurationMax = 3.5f;

////////////////////    // ─── Animation Parameter Names ────────────────────────────────────────────
////////////////////    // Must match EXACTLY the names in your Animator Controller Parameters tab.
////////////////////    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
////////////////////    private static readonly int AnimIsIdle = Animator.StringToHash("IsIdle");
////////////////////    private static readonly int AnimIsDead = Animator.StringToHash("IsDead");

////////////////////    // ─── Private State ────────────────────────────────────────────────────────

////////////////////    private Animator _animator;
////////////////////    private SoldierStats _stats;
////////////////////    private int _direction = 1;
////////////////////    private bool _isDead = false;
////////////////////    private bool _isPatrolling = true;
////////////////////    private bool _isResting = false;
////////////////////    private bool _animatorReady = false;

////////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////////    private void Awake()
////////////////////    {
////////////////////        _animator = GetComponent<Animator>();
////////////////////        _stats = GetComponent<SoldierStats>();

////////////////////        if (_animator.runtimeAnimatorController == null)
////////////////////        {
////////////////////            Debug.LogError(
////////////////////                $"[SoldierController] '{name}' has an Animator but NO Controller assigned!\n" +
////////////////////                "→ Prefab → Animator → Controller field → assign SolderAnimator.\n" +
////////////////////                "Movement will still work but animations will NOT play.");
////////////////////            _animatorReady = false;
////////////////////        }
////////////////////        else
////////////////////        {
////////////////////            _animatorReady = true;
////////////////////        }

////////////////////        _stats.OnSoldierDied += HandleDeath;
////////////////////    }

////////////////////    private void OnDestroy()
////////////////////    {
////////////////////        if (_stats != null)
////////////////////            _stats.OnSoldierDied -= HandleDeath;
////////////////////    }

////////////////////    private void Start()
////////////////////    {
////////////////////        // Clamp spawn position into the patrol area
////////////////////        Vector3 pos = transform.position;
////////////////////        pos.x = Mathf.Clamp(pos.x, patrolLeftX, patrolRightX);
////////////////////        transform.position = pos;

////////////////////        ApplyFlip(_direction);
////////////////////        StartPatrol();

////////////////////        // Kick off the rest cycle loop
////////////////////        StartCoroutine(RestCycle());
////////////////////    }

////////////////////    private void Update()
////////////////////    {
////////////////////        // Skip movement while dead OR while resting
////////////////////        if (_isDead || !_isPatrolling || _isResting) return;
////////////////////        MovePatrol();
////////////////////    }

////////////////////    // ─── Patrol Logic ─────────────────────────────────────────────────────────

////////////////////    private void StartPatrol()
////////////////////    {
////////////////////        _isPatrolling = true;
////////////////////        _isResting = false;
////////////////////        SetAnimBool(AnimIsWalking, true);
////////////////////        SetAnimBool(AnimIsIdle, false);
////////////////////    }

////////////////////    private void StopPatrol()
////////////////////    {
////////////////////        _isPatrolling = false;
////////////////////        SetAnimBool(AnimIsWalking, false);
////////////////////    }

////////////////////    private void MovePatrol()
////////////////////    {
////////////////////        float step = _direction * moveSpeed * Time.deltaTime;
////////////////////        transform.Translate(step, 0f, 0f);

////////////////////        float x = transform.position.x;

////////////////////        if (_direction == 1 && x >= patrolRightX)
////////////////////        {
////////////////////            ClampX(patrolRightX);
////////////////////            SetDirection(-1);
////////////////////        }
////////////////////        else if (_direction == -1 && x <= patrolLeftX)
////////////////////        {
////////////////////            ClampX(patrolLeftX);
////////////////////            SetDirection(1);
////////////////////        }
////////////////////    }

////////////////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////////////////////    /// <summary>
////////////////////    /// Loops forever:
////////////////////    ///   1. Walk for a random interval  (restIntervalMin – restIntervalMax seconds)
////////////////////    ///   2. Stop → play Idle animation  (restDurationMin – restDurationMax seconds)
////////////////////    ///   3. Resume walking
////////////////////    /// The coroutine exits cleanly when the soldier dies.
////////////////////    /// </summary>
////////////////////    private IEnumerator RestCycle()
////////////////////    {
////////////////////        while (!_isDead)
////////////////////        {
////////////////////            // ── 1. Walk for a random interval ─────────────────────────────────
////////////////////            float walkTime = Random.Range(restIntervalMin, restIntervalMax);
////////////////////            yield return new WaitForSeconds(walkTime);

////////////////////            if (_isDead) yield break;

////////////////////            // ── 2. Stop and play Idle ─────────────────────────────────────────
////////////////////            _isResting = true;                      // pauses MovePatrol()
////////////////////            SetAnimBool(AnimIsWalking, false);
////////////////////            SetAnimBool(AnimIsIdle, true);

////////////////////            float restTime = Random.Range(restDurationMin, restDurationMax);
////////////////////            yield return new WaitForSeconds(restTime);

////////////////////            if (_isDead) yield break;

////////////////////            // ── 3. Resume walking ─────────────────────────────────────────────
////////////////////            SetAnimBool(AnimIsIdle, false);
////////////////////            SetAnimBool(AnimIsWalking, true);
////////////////////            _isResting = false;                     // resumes MovePatrol()
////////////////////        }
////////////////////    }

////////////////////    // ─── Direction & Flip ─────────────────────────────────────────────────────

////////////////////    private void SetDirection(int dir)
////////////////////    {
////////////////////        _direction = dir;
////////////////////        ApplyFlip(dir);
////////////////////    }

////////////////////    /// <summary>
////////////////////    /// Mirrors the soldier using localScale.x.
////////////////////    /// Works for both UI Image and SpriteRenderer components.
////////////////////    /// </summary>
////////////////////    private void ApplyFlip(int dir)
////////////////////    {
////////////////////        Vector3 s = transform.localScale;
////////////////////        s.x = Mathf.Abs(s.x) * dir;   // dir 1 = normal, -1 = mirrored
////////////////////        transform.localScale = s;
////////////////////    }

////////////////////    private void ClampX(float x)
////////////////////    {
////////////////////        Vector3 pos = transform.position;
////////////////////        pos.x = x;
////////////////////        transform.position = pos;
////////////////////    }

////////////////////    // ─── Death ────────────────────────────────────────────────────────────────

////////////////////    private void HandleDeath(SoldierStats stats)
////////////////////    {
////////////////////        _isDead = true;
////////////////////        StopPatrol();
////////////////////        SetAnimBool(AnimIsIdle, false);
////////////////////        SetAnimBool(AnimIsDead, true);

////////////////////        Debug.Log($"[SoldierController] Soldier '{name}' has died.");
////////////////////    }

////////////////////    // ─── Animator Helper ──────────────────────────────────────────────────────

////////////////////    private void SetAnimBool(int hash, bool value)
////////////////////    {
////////////////////        if (_animatorReady)
////////////////////            _animator.SetBool(hash, value);
////////////////////    }

////////////////////    // ─── Public Controls ─────────────────────────────────────────────────────

////////////////////    /// <summary>Pause/resume patrol externally (e.g. during combat).</summary>
////////////////////    public void SetPatrolling(bool active)
////////////////////    {
////////////////////        if (_isDead) return;
////////////////////        if (active) StartPatrol();
////////////////////        else StopPatrol();
////////////////////    }

////////////////////    /// <summary>Reposition the patrol area at runtime.</summary>
////////////////////    public void SetPatrolBounds(float leftX, float rightX)
////////////////////    {
////////////////////        patrolLeftX = leftX;
////////////////////        patrolRightX = rightX;
////////////////////    }

////////////////////    // ─── Editor Gizmos ───────────────────────────────────────────────────────
////////////////////#if UNITY_EDITOR
////////////////////    private void OnDrawGizmosSelected()
////////////////////    {
////////////////////        Gizmos.color = Color.cyan;
////////////////////        float y = transform.position.y;
////////////////////        float z = transform.position.z;
////////////////////        Gizmos.DrawLine(new Vector3(patrolLeftX, y, z), new Vector3(patrolRightX, y, z));
////////////////////        Gizmos.color = Color.yellow;
////////////////////        Gizmos.DrawSphere(new Vector3(patrolLeftX, y, z), 0.15f);
////////////////////        Gizmos.DrawSphere(new Vector3(patrolRightX, y, z), 0.15f);
////////////////////    }
////////////////////#endif
////////////////////}

//////////////////using System.Collections;
//////////////////using UnityEngine;

///////////////////// <summary>
///////////////////// AREA FORGE - SoldierController
/////////////////////
///////////////////// Controls patrol movement, idle resting, flipping, and death.
///////////////////// Tells SpriteLayerAnimator which animation state is active so all
///////////////////// equipment layers animate in sync — no per-item Animator needed.
/////////////////////
///////////////////// ── What drives what ────────────────────────────────────────────────────────
/////////////////////   SoldierController  → calls _spriteAnim.SetState(Walk / Idle / Death)
/////////////////////   SpriteLayerAnimator→ steps frames on every equipped Image layer
/////////////////////   Animator (Unity)   → ONLY drives position/movement (the root transform)
/////////////////////                        NOT used for sprite swapping anymore
///////////////////// ────────────────────────────────────────────────────────────────────────────
///////////////////// </summary>
//////////////////[RequireComponent(typeof(SoldierStats))]
//////////////////[RequireComponent(typeof(SpriteLayerAnimator))]
//////////////////public class SoldierController : MonoBehaviour
//////////////////{
//////////////////    // ─── Patrol Settings ──────────────────────────────────────────────────────

//////////////////    [Header("Patrol Area")]
//////////////////    [SerializeField] private float patrolLeftX = -4f;
//////////////////    [SerializeField] private float patrolRightX = 4f;
//////////////////    [SerializeField] private float moveSpeed = 0.5f;

//////////////////    // ─── Rest Settings ────────────────────────────────────────────────────────

//////////////////    [Header("Rest Behaviour")]
//////////////////    [SerializeField] private float restIntervalMin = 3f;
//////////////////    [SerializeField] private float restIntervalMax = 7f;
//////////////////    [SerializeField] private float restDurationMin = 1.5f;
//////////////////    [SerializeField] private float restDurationMax = 3.5f;

//////////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////////    private SoldierStats _stats;
//////////////////    private SpriteLayerAnimator _spriteAnim;

//////////////////    private int _direction = 1;
//////////////////    private bool _isDead = false;
//////////////////    private bool _isPatrolling = true;
//////////////////    private bool _isResting = false;

//////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////////    private void Awake()
//////////////////    {
//////////////////        _stats = GetComponent<SoldierStats>();
//////////////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();

//////////////////        _stats.OnSoldierDied += HandleDeath;
//////////////////    }

//////////////////    private void OnDestroy()
//////////////////    {
//////////////////        if (_stats != null)
//////////////////            _stats.OnSoldierDied -= HandleDeath;
//////////////////    }

//////////////////    private void Start()
//////////////////    {
//////////////////        // Clamp spawn inside patrol area
//////////////////        Vector3 pos = transform.position;
//////////////////        pos.x = Mathf.Clamp(pos.x, patrolLeftX, patrolRightX);
//////////////////        transform.position = pos;

//////////////////        ApplyFlip(_direction);
//////////////////        StartPatrol();
//////////////////        StartCoroutine(RestCycle());
//////////////////    }

//////////////////    private void Update()
//////////////////    {
//////////////////        if (_isDead || !_isPatrolling || _isResting) return;
//////////////////        MovePatrol();
//////////////////    }

//////////////////    // ─── Patrol ───────────────────────────────────────────────────────────────

//////////////////    private void StartPatrol()
//////////////////    {
//////////////////        _isPatrolling = true;
//////////////////        _isResting = false;

//////////////////        // Tell SpriteLayerAnimator: switch all layers to Walk sprites
//////////////////        _spriteAnim.SetState(AnimationState.Walk);
//////////////////    }

//////////////////    private void StopPatrol()
//////////////////    {
//////////////////        _isPatrolling = false;

//////////////////        // Tell SpriteLayerAnimator: switch all layers to Idle sprites
//////////////////        _spriteAnim.SetState(AnimationState.Idle);
//////////////////    }

//////////////////    private void MovePatrol()
//////////////////    {
//////////////////        transform.Translate(_direction * moveSpeed * Time.deltaTime, 0f, 0f);

//////////////////        float x = transform.position.x;

//////////////////        if (_direction == 1 && x >= patrolRightX)
//////////////////        {
//////////////////            ClampX(patrolRightX);
//////////////////            SetDirection(-1);
//////////////////        }
//////////////////        else if (_direction == -1 && x <= patrolLeftX)
//////////////////        {
//////////////////            ClampX(patrolLeftX);
//////////////////            SetDirection(1);
//////////////////        }
//////////////////    }

//////////////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//////////////////    private IEnumerator RestCycle()
//////////////////    {
//////////////////        while (!_isDead)
//////////////////        {
//////////////////            // Walk for a random interval
//////////////////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//////////////////            if (_isDead) yield break;

//////////////////            // Begin rest — stop movement, switch to Idle animation
//////////////////            _isResting = true;
//////////////////            _spriteAnim.SetState(AnimationState.Idle);

//////////////////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//////////////////            if (_isDead) yield break;

//////////////////            // Resume walk — switch back to Walk animation
//////////////////            _spriteAnim.SetState(AnimationState.Walk);
//////////////////            _isResting = false;
//////////////////        }
//////////////////    }

//////////////////    // ─── Death ────────────────────────────────────────────────────────────────

//////////////////    private void HandleDeath(SoldierStats stats)
//////////////////    {
//////////////////        _isDead = true;
//////////////////        StopPatrol();

//////////////////        // Tell SpriteLayerAnimator: switch all layers to Death sprites
//////////////////        // Each EquipmentItem's deathSprites array will play once
//////////////////        _spriteAnim.SetState(AnimationState.Death);

//////////////////        Debug.Log($"[SoldierController] '{name}' died.");
//////////////////    }

//////////////////    // ─── Flip ─────────────────────────────────────────────────────────────────

//////////////////    private void SetDirection(int dir)
//////////////////    {
//////////////////        _direction = dir;
//////////////////        ApplyFlip(dir);
//////////////////    }

//////////////////    /// <summary>
//////////////////    /// Mirrors the soldier using localScale.x — works for UI Image and SpriteRenderer.
//////////////////    /// </summary>
//////////////////    private void ApplyFlip(int dir)
//////////////////    {
//////////////////        Vector3 s = transform.localScale;
//////////////////        s.x = Mathf.Abs(s.x) * dir;
//////////////////        transform.localScale = s;
//////////////////    }

//////////////////    private void ClampX(float x)
//////////////////    {
//////////////////        Vector3 pos = transform.position;
//////////////////        pos.x = x;
//////////////////        transform.position = pos;
//////////////////    }

//////////////////    // ─── Public Controls ─────────────────────────────────────────────────────

//////////////////    public void SetPatrolling(bool active)
//////////////////    {
//////////////////        if (_isDead) return;
//////////////////        if (active) StartPatrol();
//////////////////        else StopPatrol();
//////////////////    }

//////////////////    public void SetPatrolBounds(float leftX, float rightX)
//////////////////    {
//////////////////        patrolLeftX = leftX;
//////////////////        patrolRightX = rightX;
//////////////////    }

//////////////////    // ─── Gizmos ───────────────────────────────────────────────────────────────
//////////////////#if UNITY_EDITOR
//////////////////    private void OnDrawGizmosSelected()
//////////////////    {
//////////////////        Gizmos.color = Color.cyan;
//////////////////        float y = transform.position.y;
//////////////////        float z = transform.position.z;
//////////////////        Gizmos.DrawLine(new Vector3(patrolLeftX, y, z), new Vector3(patrolRightX, y, z));
//////////////////        Gizmos.color = Color.yellow;
//////////////////        Gizmos.DrawSphere(new Vector3(patrolLeftX, y, z), 0.15f);
//////////////////        Gizmos.DrawSphere(new Vector3(patrolRightX, y, z), 0.15f);
//////////////////    }
//////////////////#endif
//////////////////}

////////////////using System.Collections;
////////////////using UnityEngine;

/////////////////// <summary>
/////////////////// AREA FORGE - SoldierController
///////////////////
/////////////////// ── BUG FIX: Flip never triggered ─────────────────────────────────────────────
///////////////////   OLD: used transform.position.x (world/screen-pixel coords on a UI Canvas).
///////////////////        patrolLeftX = -4, patrolRightX = 4 never matched screen pixel values
///////////////////        like 300, 600 → direction check always false → soldier never flipped.
///////////////////
///////////////////   FIX: All movement and bounds checks now use transform.localPosition.x
///////////////////        (local canvas units, consistent with the small patrol bound values).
///////////////////        SetPatrolBoundsFromCurrentPosition() auto-calculates left/right offset
///////////////////        from wherever the soldier spawns, so you never need to tweak bounds.
///////////////////
/////////////////// ── Patrol pattern ─────────────────────────────────────────────────────────────
///////////////////   Walk right → reach right bound → flip left → walk left → reach left bound
///////////////////   → flip right → ... plus random idle rests in between.
///////////////////
/////////////////// ── Animation ─────────────────────────────────────────────────────────────────
///////////////////   No Unity Animator used for sprites.
///////////////////   SpriteLayerAnimator handles all per-layer frame stepping.
///////////////////   This script only calls SetState(Walk / Idle / Death).
/////////////////// </summary>
////////////////[RequireComponent(typeof(SoldierStats))]
////////////////[RequireComponent(typeof(SpriteLayerAnimator))]
////////////////public class SoldierController : MonoBehaviour
////////////////{
////////////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////////////    [Header("Patrol Area (local canvas units — relative to spawn position)")]
////////////////    [Tooltip("How far LEFT of the spawn point the soldier walks (positive value).")]
////////////////    [SerializeField] private float patrolHalfWidth = 80f;   // canvas units each side

////////////////    [Tooltip("Movement speed in local canvas units per second.")]
////////////////    [SerializeField] private float moveSpeed = 40f;

////////////////    [Header("Rest Behaviour")]
////////////////    [SerializeField] private float restIntervalMin = 3f;
////////////////    [SerializeField] private float restIntervalMax = 7f;
////////////////    [SerializeField] private float restDurationMin = 1.5f;
////////////////    [SerializeField] private float restDurationMax = 3.5f;

////////////////    // ─── Private ──────────────────────────────────────────────────────────────

////////////////    private SoldierStats _stats;
////////////////    private SpriteLayerAnimator _spriteAnim;
////////////////    private RectTransform _rect;

////////////////    // Patrol bounds in LOCAL space (set in Start from spawn position)
////////////////    private float _leftBound;
////////////////    private float _rightBound;

////////////////    private int _direction = 1;       // 1 = right, -1 = left
////////////////    private bool _isDead = false;
////////////////    private bool _isPatrolling = true;
////////////////    private bool _isResting = false;

////////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////////    private void Awake()
////////////////    {
////////////////        _stats = GetComponent<SoldierStats>();
////////////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
////////////////        _rect = GetComponent<RectTransform>();

////////////////        _stats.OnSoldierDied += HandleDeath;
////////////////    }

////////////////    private void OnDestroy()
////////////////    {
////////////////        if (_stats != null)
////////////////            _stats.OnSoldierDied -= HandleDeath;
////////////////    }

////////////////    private void Start()
////////////////    {
////////////////        // ── Calculate patrol bounds in LOCAL space from spawn position ─────────
////////////////        // This means the soldier patrols ±patrolHalfWidth canvas units around
////////////////        // wherever it spawns — no manual bound tweaking needed.
////////////////        float spawnLocalX = _rect != null
////////////////            ? _rect.anchoredPosition.x
////////////////            : transform.localPosition.x;

////////////////        _leftBound = spawnLocalX - patrolHalfWidth;
////////////////        _rightBound = spawnLocalX + patrolHalfWidth;

////////////////        ApplyFlip(_direction);
////////////////        StartPatrol();
////////////////        StartCoroutine(RestCycle());
////////////////    }

////////////////    private void Update()
////////////////    {
////////////////        if (_isDead || !_isPatrolling || _isResting) return;
////////////////        MovePatrol();
////////////////    }

////////////////    // ─── Patrol ───────────────────────────────────────────────────────────────

////////////////    private void StartPatrol()
////////////////    {
////////////////        _isPatrolling = true;
////////////////        _isResting = false;
////////////////        _spriteAnim.SetState(AnimationState.Walk);
////////////////    }

////////////////    private void StopPatrol()
////////////////    {
////////////////        _isPatrolling = false;
////////////////        _spriteAnim.SetState(AnimationState.Idle);
////////////////    }

////////////////    private void MovePatrol()
////////////////    {
////////////////        // ── Move in LOCAL space ────────────────────────────────────────────────
////////////////        float step = _direction * moveSpeed * Time.deltaTime;

////////////////        if (_rect != null)
////////////////        {
////////////////            // UI RectTransform: move via anchoredPosition
////////////////            _rect.anchoredPosition += new Vector2(step, 0f);
////////////////            float x = _rect.anchoredPosition.x;

////////////////            if (_direction == 1 && x >= _rightBound)
////////////////            {
////////////////                var pos = _rect.anchoredPosition;
////////////////                pos.x = _rightBound;
////////////////                _rect.anchoredPosition = pos;
////////////////                SetDirection(-1);
////////////////            }
////////////////            else if (_direction == -1 && x <= _leftBound)
////////////////            {
////////////////                var pos = _rect.anchoredPosition;
////////////////                pos.x = _leftBound;
////////////////                _rect.anchoredPosition = pos;
////////////////                SetDirection(1);
////////////////            }
////////////////        }
////////////////        else
////////////////        {
////////////////            // Fallback for non-UI (SpriteRenderer) soldiers
////////////////            transform.Translate(step, 0f, 0f);
////////////////            float x = transform.localPosition.x;

////////////////            if (_direction == 1 && x >= _rightBound)
////////////////            {
////////////////                var pos = transform.localPosition; pos.x = _rightBound;
////////////////                transform.localPosition = pos;
////////////////                SetDirection(-1);
////////////////            }
////////////////            else if (_direction == -1 && x <= _leftBound)
////////////////            {
////////////////                var pos = transform.localPosition; pos.x = _leftBound;
////////////////                transform.localPosition = pos;
////////////////                SetDirection(1);
////////////////            }
////////////////        }
////////////////    }

////////////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////////////////    private IEnumerator RestCycle()
////////////////    {
////////////////        while (!_isDead)
////////////////        {
////////////////            yield return new WaitForSeconds(
////////////////                Random.Range(restIntervalMin, restIntervalMax));
////////////////            if (_isDead) yield break;

////////////////            // Begin rest
////////////////            _isResting = true;
////////////////            _spriteAnim.SetState(AnimationState.Idle);

////////////////            yield return new WaitForSeconds(
////////////////                Random.Range(restDurationMin, restDurationMax));
////////////////            if (_isDead) yield break;

////////////////            // Resume walk
////////////////            _spriteAnim.SetState(AnimationState.Walk);
////////////////            _isResting = false;
////////////////        }
////////////////    }

////////////////    // ─── Death ────────────────────────────────────────────────────────────────

////////////////    private void HandleDeath(SoldierStats _)
////////////////    {
////////////////        _isDead = true;
////////////////        StopPatrol();
////////////////        _spriteAnim.SetState(AnimationState.Death);
////////////////        Debug.Log($"[SoldierController] '{name}' died.");
////////////////    }

////////////////    // ─── Flip ─────────────────────────────────────────────────────────────────

////////////////    private void SetDirection(int dir)
////////////////    {
////////////////        _direction = dir;
////////////////        ApplyFlip(dir);
////////////////    }

////////////////    /// <summary>
////////////////    /// Flips the soldier by negating localScale.x.
////////////////    /// Works for both UI Image layers and SpriteRenderer.
////////////////    /// dir = 1  → facing right (normal)
////////////////    /// dir = -1 → facing left  (mirrored)
////////////////    /// </summary>
////////////////    private void ApplyFlip(int dir)
////////////////    {
////////////////        Vector3 s = transform.localScale;
////////////////        s.x = Mathf.Abs(s.x) * dir;
////////////////        transform.localScale = s;
////////////////    }

////////////////    // ─── Public Controls ─────────────────────────────────────────────────────

////////////////    /// <summary>
////////////////    /// Pause (false) or resume (true) patrol — called by SoldierDragDrop
////////////////    /// and any other system that needs to freeze the soldier.
////////////////    /// </summary>
////////////////    public void SetPatrolling(bool active)
////////////////    {
////////////////        if (_isDead) return;
////////////////        if (active) StartPatrol();
////////////////        else StopPatrol();
////////////////    }

////////////////    /// <summary>
////////////////    /// Override the patrol half-width at runtime (e.g. after spawning in
////////////////    /// a different-sized area).
////////////////    /// </summary>
////////////////    public void SetPatrolHalfWidth(float halfWidth)
////////////////    {
////////////////        float centre = (_leftBound + _rightBound) * 0.5f;
////////////////        _leftBound = centre - halfWidth;
////////////////        _rightBound = centre + halfWidth;
////////////////    }

////////////////    // ─── Gizmos ───────────────────────────────────────────────────────────────
////////////////#if UNITY_EDITOR
////////////////    private void OnDrawGizmosSelected()
////////////////    {
////////////////        // Show patrol bounds in Scene view (works in world space approximation)
////////////////        float y = transform.position.y;
////////////////        float z = transform.position.z;
////////////////        Gizmos.color = Color.cyan;
////////////////        Gizmos.DrawLine(
////////////////            new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z),
////////////////            new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z));
////////////////        Gizmos.color = Color.yellow;
////////////////        Gizmos.DrawSphere(
////////////////            new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z), 0.1f);
////////////////        Gizmos.DrawSphere(
////////////////            new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z), 0.1f);
////////////////    }
////////////////#endif
////////////////}




//////////////using System.Collections;
//////////////using UnityEngine;

///////////////// <summary>
///////////////// AREA FORGE - SoldierController
/////////////////
///////////////// ── BUG FIX: Soldier never flips direction ─────────────────────────────────────
/////////////////   ROOT CAUSE:
/////////////////     Flipping uses transform.localScale.x. Every time SoldierDragDrop calls
/////////////////     SetParent() with worldPositionStays = true (drag → rootCanvas, retrieve →
/////////////////     spawnParent), Unity recalculates localScale to preserve world scale across
/////////////////     parents that may have different Canvas Scaler factors. This corrupts the
/////////////////     sign of localScale.x that the flip logic relies on.
/////////////////
/////////////////   FIX: Added public RefreshFlip() — call this from SoldierDragDrop after
/////////////////        every SetParent so localScale.x is always re-derived from _direction
/////////////////        (the authoritative flip state) rather than a mangled intermediate value.
/////////////////
///////////////// ── BUG FIX: Patrol area shifts position after retrieve ────────────────────────
/////////////////   ROOT CAUSE:
/////////////////     WizardBox.AcceptSoldier does SetParent(wizardBox, false)
/////////////////     (worldPositionStays = FALSE), which zeroes the soldier's local position.
/////////////////     On retrieve, SetParent(spawnParent, true) converts that world position
/////////////////     into spawn-area local space — placing the soldier at a DIFFERENT
/////////////////     anchoredPosition than it originally had. The patrol bounds were calculated
/////////////////     once in Start() from the original spawn X, so the soldier now patrols a
/////////////////     range that is completely offset from where it actually stands.
/////////////////
/////////////////   FIX: SetPatrolling(true) now always calls
/////////////////        RecalculatePatrolBoundsFromCurrentPosition() before resuming. This
/////////////////        re-centres the patrol range on wherever the soldier actually is,
/////////////////        so retrieve → patrol always looks correct.
/////////////////
///////////////// ── Animation ─────────────────────────────────────────────────────────────────
/////////////////   No Unity Animator used for sprites.
/////////////////   SpriteLayerAnimator handles all per-layer frame stepping.
/////////////////   This script only calls SetState(Walk / Idle / Death).
///////////////// </summary>
//////////////[RequireComponent(typeof(SoldierStats))]
//////////////[RequireComponent(typeof(SpriteLayerAnimator))]
//////////////public class SoldierController : MonoBehaviour
//////////////{
//////////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////////    [Header("Patrol Area (local canvas units — relative to spawn position)")]
//////////////    [Tooltip("How far LEFT and RIGHT of the current position the soldier walks.")]
//////////////    [SerializeField] private float patrolHalfWidth = 80f;

//////////////    [Tooltip("Movement speed in local canvas units per second.")]
//////////////    [SerializeField] private float moveSpeed = 40f;

//////////////    [Header("Rest Behaviour")]
//////////////    [SerializeField] private float restIntervalMin = 3f;
//////////////    [SerializeField] private float restIntervalMax = 7f;
//////////////    [SerializeField] private float restDurationMin = 1.5f;
//////////////    [SerializeField] private float restDurationMax = 3.5f;

//////////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////////    private SoldierStats _stats;
//////////////    private SpriteLayerAnimator _spriteAnim;
//////////////    private RectTransform _rect;

//////////////    private float _leftBound;
//////////////    private float _rightBound;

//////////////    private int _direction = 1;       // 1 = right, -1 = left  (authoritative flip state)
//////////////    private bool _isDead = false;
//////////////    private bool _isPatrolling = true;
//////////////    private bool _isResting = false;

//////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////////    private void Awake()
//////////////    {
//////////////        _stats = GetComponent<SoldierStats>();
//////////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
//////////////        _rect = GetComponent<RectTransform>();

//////////////        _stats.OnSoldierDied += HandleDeath;
//////////////    }

//////////////    private void OnDestroy()
//////////////    {
//////////////        if (_stats != null)
//////////////            _stats.OnSoldierDied -= HandleDeath;
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        // Calculate initial patrol bounds from spawn position
//////////////        RecalculatePatrolBoundsFromCurrentPosition();

//////////////        ApplyFlip(_direction);
//////////////        StartPatrol();
//////////////        StartCoroutine(RestCycle());
//////////////    }

//////////////    private void Update()
//////////////    {
//////////////        if (_isDead || !_isPatrolling || _isResting) return;
//////////////        MovePatrol();
//////////////    }

//////////////    // ─── Patrol ───────────────────────────────────────────────────────────────

//////////////    private void StartPatrol()
//////////////    {
//////////////        _isPatrolling = true;
//////////////        _isResting = false;
//////////////        _spriteAnim.SetState(AnimationState.Walk);
//////////////    }

//////////////    private void StopPatrol()
//////////////    {
//////////////        _isPatrolling = false;
//////////////        _spriteAnim.SetState(AnimationState.Idle);
//////////////    }

//////////////    private void MovePatrol()
//////////////    {
//////////////        float step = _direction * moveSpeed * Time.deltaTime;

//////////////        if (_rect != null)
//////////////        {
//////////////            _rect.anchoredPosition += new Vector2(step, 0f);
//////////////            float x = _rect.anchoredPosition.x;

//////////////            if (_direction == 1 && x >= _rightBound)
//////////////            {
//////////////                var pos = _rect.anchoredPosition;
//////////////                pos.x = _rightBound;
//////////////                _rect.anchoredPosition = pos;
//////////////                SetDirection(-1);
//////////////            }
//////////////            else if (_direction == -1 && x <= _leftBound)
//////////////            {
//////////////                var pos = _rect.anchoredPosition;
//////////////                pos.x = _leftBound;
//////////////                _rect.anchoredPosition = pos;
//////////////                SetDirection(1);
//////////////            }
//////////////        }
//////////////        else
//////////////        {
//////////////            transform.Translate(step, 0f, 0f);
//////////////            float x = transform.localPosition.x;

//////////////            if (_direction == 1 && x >= _rightBound)
//////////////            {
//////////////                var pos = transform.localPosition; pos.x = _rightBound;
//////////////                transform.localPosition = pos;
//////////////                SetDirection(-1);
//////////////            }
//////////////            else if (_direction == -1 && x <= _leftBound)
//////////////            {
//////////////                var pos = transform.localPosition; pos.x = _leftBound;
//////////////                transform.localPosition = pos;
//////////////                SetDirection(1);
//////////////            }
//////////////        }
//////////////    }

//////////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//////////////    private IEnumerator RestCycle()
//////////////    {
//////////////        while (!_isDead)
//////////////        {
//////////////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//////////////            if (_isDead) yield break;

//////////////            _isResting = true;
//////////////            _spriteAnim.SetState(AnimationState.Idle);

//////////////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//////////////            if (_isDead) yield break;

//////////////            _spriteAnim.SetState(AnimationState.Walk);
//////////////            _isResting = false;
//////////////        }
//////////////    }

//////////////    // ─── Death ────────────────────────────────────────────────────────────────

//////////////    private void HandleDeath(SoldierStats _)
//////////////    {
//////////////        _isDead = true;
//////////////        StopPatrol();
//////////////        _spriteAnim.SetState(AnimationState.Death);
//////////////        Debug.Log($"[SoldierController] '{name}' died.");
//////////////    }

//////////////    // ─── Flip ─────────────────────────────────────────────────────────────────

//////////////    private void SetDirection(int dir)
//////////////    {
//////////////        _direction = dir;
//////////////        ApplyFlip(dir);
//////////////    }

//////////////    /// <summary>
//////////////    /// Flips the soldier by setting localScale.x sign.
//////////////    /// Uses the stored _direction value so it is always consistent.
//////////////    /// dir =  1 → facing right (positive scale)
//////////////    /// dir = -1 → facing left  (negative scale)
//////////////    /// </summary>
//////////////    private void ApplyFlip(int dir)
//////////////    {
//////////////        Vector3 s = transform.localScale;
//////////////        // Mathf.Abs isolates the magnitude so Canvas Scaler values are preserved,
//////////////        // and we only control the sign (flip direction).
//////////////        s.x = Mathf.Abs(s.x) * dir;
//////////////        transform.localScale = s;
//////////////    }

//////////////    // ─── Bounds ───────────────────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Re-centres the patrol range on the soldier's CURRENT local position.
//////////////    ///
//////////////    /// Call this any time the soldier is moved to a new position before resuming
//////////////    /// patrol (e.g. after retrieve from WizardBox), so the patrol area doesn't
//////////////    /// appear to drift or be offset from where the soldier is standing.
//////////////    /// </summary>
//////////////    private void RecalculatePatrolBoundsFromCurrentPosition()
//////////////    {
//////////////        float currentX = _rect != null
//////////////            ? _rect.anchoredPosition.x
//////////////            : transform.localPosition.x;

//////////////        _leftBound = currentX - patrolHalfWidth;
//////////////        _rightBound = currentX + patrolHalfWidth;
//////////////    }

//////////////    // ─── Public Controls ─────────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Pause (false) or resume (true) patrol.
//////////////    /// Called by SoldierDragDrop and any other system that needs to freeze the soldier.
//////////////    ///
//////////////    /// On resume, patrol bounds are recalculated from the soldier's current position.
//////////////    /// This fixes the "patrol area drifts after retrieve" bug — the bounds always
//////////////    /// re-centre on wherever the soldier actually is, not where it was at spawn.
//////////////    /// </summary>
//////////////    public void SetPatrolling(bool active)
//////////////    {
//////////////        if (_isDead) return;

//////////////        if (active)
//////////////        {
//////////////            // ── FIX: recalculate bounds from current position before resuming ─────
//////////////            // Without this, if the soldier was moved (e.g. retrieved from WizardBox
//////////////            // to a different anchoredPosition), it would patrol an area offset from
//////////////            // where it actually stands.
//////////////            RecalculatePatrolBoundsFromCurrentPosition();
//////////////            StartPatrol();
//////////////        }
//////////////        else
//////////////        {
//////////////            StopPatrol();
//////////////        }
//////////////    }

//////////////    /// <summary>
//////////////    /// Re-applies the flip for the current _direction.
//////////////    ///
//////////////    /// Call this from SoldierDragDrop after every SetParent operation.
//////////////    ///
//////////////    /// WHY: SetParent(newParent, worldPositionStays: true) makes Unity recompute
//////////////    /// localScale to preserve world scale. If the old and new parents have
//////////////    /// different Canvas Scaler factors, this changes localScale.x — corrupting
//////////////    /// the flip sign. Calling RefreshFlip() restores the correct sign from the
//////////////    /// authoritative _direction field.
//////////////    /// </summary>
//////////////    public void RefreshFlip()
//////////////    {
//////////////        ApplyFlip(_direction);
//////////////    }

//////////////    /// <summary>
//////////////    /// Override the patrol half-width at runtime (e.g. after spawning in a
//////////////    /// different-sized area). Recalculates bounds from current position.
//////////////    /// </summary>
//////////////    public void SetPatrolHalfWidth(float halfWidth)
//////////////    {
//////////////        patrolHalfWidth = halfWidth;
//////////////        RecalculatePatrolBoundsFromCurrentPosition();
//////////////    }

//////////////    // ─── Gizmos ───────────────────────────────────────────────────────────────
//////////////#if UNITY_EDITOR
//////////////    private void OnDrawGizmosSelected()
//////////////    {
//////////////        float y = transform.position.y;
//////////////        float z = transform.position.z;
//////////////        Gizmos.color = Color.cyan;
//////////////        Gizmos.DrawLine(
//////////////            new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z),
//////////////            new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z));
//////////////        Gizmos.color = Color.yellow;
//////////////        Gizmos.DrawSphere(new Vector3(transform.position.x - patrolHalfWidth * 0.01f, y, z), 0.1f);
//////////////        Gizmos.DrawSphere(new Vector3(transform.position.x + patrolHalfWidth * 0.01f, y, z), 0.1f);
//////////////    }
//////////////#endif
//////////////}

////////////using System.Collections;
////////////using UnityEngine;

/////////////// <summary>
/////////////// AREA FORGE - SoldierController
///////////////
/////////////// Handles patrol movement, idle resting, directional flipping, and death.
/////////////// Drives SpriteLayerAnimator for per-layer animation state (Walk/Idle/Death).
///////////////
/////////////// ── FIX 1: Patrol bounds calculated too early ──────────────────────────────
///////////////   OLD: RecalculatePatrolBoundsFromCurrentPosition() was called in Start().
///////////////        Unity's Canvas layout engine finalises RectTransform.anchoredPosition
///////////////        at the END of the first frame — AFTER Start() has already run.
///////////////        Spawning via Instantiate() means anchoredPosition is still 0 when
///////////////        Start() reads it, so _leftBound and _rightBound are both ~0.
///////////////        The soldier hits a boundary on the very first Update() tick and
///////////////        appears frozen.
///////////////
///////////////   FIX: InitPatrol() is an IEnumerator that yields one frame before reading
///////////////        anchoredPosition. By then the Canvas has settled and the position is
///////////////        correct. StartCoroutine(InitPatrol()) replaces the direct calls.
///////////////
/////////////// ── FIX 2: Flip state corrupted after Instantiate / SetParent ──────────────
///////////////   OLD: ApplyFlip() used Mathf.Abs(transform.localScale.x) as the magnitude.
///////////////        Instantiate(prefab, worldPos, rot, parent) — especially under a Canvas
///////////////        with Scale With Screen Size — causes Unity to recompute localScale to
///////////////        preserve world scale. The resulting localScale.x can be a fractional
///////////////        value that drifts every time the parent changes. Using Mathf.Abs of
///////////////        a drifting value accumulates error and the flip eventually stops working.
///////////////
///////////////   FIX: _baseScaleX is captured once in Awake() as Mathf.Abs(localScale.x).
///////////////        ApplyFlip() always uses this stored magnitude — so the flip is always
///////////////        exactly ±_baseScaleX regardless of what parent rescaling has done.
///////////////        RefreshFlip() (called by SoldierDragDrop after SetParent) re-applies
///////////////        the same magnitude × direction.
/////////////// </summary>
////////////[RequireComponent(typeof(SoldierStats))]
////////////[RequireComponent(typeof(SpriteLayerAnimator))]
////////////public class SoldierController : MonoBehaviour
////////////{
////////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////////    [Header("Patrol Area")]
////////////    [Tooltip("How far LEFT and RIGHT of the spawn position the soldier walks (canvas units).")]
////////////    [SerializeField] private float patrolHalfWidth = 80f;

////////////    [Tooltip("Movement speed in canvas units per second.")]
////////////    [SerializeField] private float moveSpeed = 40f;

////////////    [Header("Rest Behaviour")]
////////////    [SerializeField] private float restIntervalMin = 3f;
////////////    [SerializeField] private float restIntervalMax = 7f;
////////////    [SerializeField] private float restDurationMin = 1.5f;
////////////    [SerializeField] private float restDurationMax = 3.5f;

////////////    // ─── Private State ────────────────────────────────────────────────────────

////////////    private SoldierStats _stats;
////////////    private SpriteLayerAnimator _spriteAnim;
////////////    private RectTransform _rect;

////////////    private float _leftBound;
////////////    private float _rightBound;

////////////    private int  _direction  = 1;     // 1 = right, -1 = left  (authoritative)
////////////    private bool _isDead     = false;
////////////    private bool _isResting  = false;
////////////    private bool _isPatrolling = false;

////////////    // FIX 2: stored once in Awake — never recalculated from a potentially
////////////    // corrupted localScale.x.
////////////    private float _baseScaleX;

////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////////    private void Awake()
////////////    {
////////////        _stats     = GetComponent<SoldierStats>();
////////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
////////////        _rect      = GetComponent<RectTransform>();

////////////        // FIX 2: capture the prefab's authored scale magnitude right now,
////////////        // before any parent-rescaling from Instantiate can touch it.
////////////        _baseScaleX = Mathf.Abs(transform.localScale.x);
////////////        if (_baseScaleX < 0.001f) _baseScaleX = 1f; // safety — never zero

////////////        _stats.OnSoldierDied += HandleDeath;
////////////    }

////////////    private void OnDestroy()
////////////    {
////////////        if (_stats != null)
////////////            _stats.OnSoldierDied -= HandleDeath;
////////////    }

////////////    private void Start()
////////////    {
////////////        // FIX 1: wait one frame so the Canvas layout engine has finalised
////////////        // anchoredPosition before we try to read it for patrol bounds.
////////////        StartCoroutine(InitPatrol());
////////////    }

////////////    private void Update()
////////////    {
////////////        if (_isDead || !_isPatrolling || _isResting) return;
////////////        MovePatrol();
////////////    }

////////////    // ─── Initialisation ───────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Waits one frame (so Canvas layout settles), then starts patrol.
////////////    /// This is the FIX for "soldier never moves" on spawn.
////////////    /// </summary>
////////////    private IEnumerator InitPatrol()
////////////    {
////////////        // yield null = skip to the end of this frame; Canvas positions are
////////////        // written by the layout engine before the NEXT frame's Update().
////////////        yield return null;

////////////        RecalculatePatrolBoundsFromCurrentPosition();
////////////        ApplyFlip(_direction);
////////////        StartWalking();
////////////        StartCoroutine(RestCycle());

////////////        Debug.Log($"[SoldierController] '{name}' patrol started. " +
////////////                  $"Bounds: {_leftBound:F0} → {_rightBound:F0}  " +
////////////                  $"(anchorX={CurrentX():F0})");
////////////    }

////////////    // ─── Patrol ───────────────────────────────────────────────────────────────

////////////    private void StartWalking()
////////////    {
////////////        _isPatrolling = true;
////////////        _isResting    = false;
////////////        _spriteAnim.SetState(AnimationState.Walk);
////////////    }

////////////    private void StopWalking()
////////////    {
////////////        _isPatrolling = false;
////////////        _spriteAnim.SetState(AnimationState.Idle);
////////////    }

////////////    private void MovePatrol()
////////////    {
////////////        float step = _direction * moveSpeed * Time.deltaTime;

////////////        if (_rect != null)
////////////        {
////////////            // ── UI soldier (RectTransform) ─────────────────────────────────────
////////////            _rect.anchoredPosition += new Vector2(step, 0f);
////////////            float x = _rect.anchoredPosition.x;

////////////            if (_direction == 1 && x >= _rightBound)
////////////            {
////////////                var p = _rect.anchoredPosition; p.x = _rightBound;
////////////                _rect.anchoredPosition = p;
////////////                SetDirection(-1);
////////////            }
////////////            else if (_direction == -1 && x <= _leftBound)
////////////            {
////////////                var p = _rect.anchoredPosition; p.x = _leftBound;
////////////                _rect.anchoredPosition = p;
////////////                SetDirection(1);
////////////            }
////////////        }
////////////        else
////////////        {
////////////            // ── World-space soldier (SpriteRenderer / plain Transform) ─────────
////////////            transform.Translate(step, 0f, 0f, Space.Self);
////////////            float x = transform.localPosition.x;

////////////            if (_direction == 1 && x >= _rightBound)
////////////            {
////////////                var pos = transform.localPosition; pos.x = _rightBound;
////////////                transform.localPosition = pos;
////////////                SetDirection(-1);
////////////            }
////////////            else if (_direction == -1 && x <= _leftBound)
////////////            {
////////////                var pos = transform.localPosition; pos.x = _leftBound;
////////////                transform.localPosition = pos;
////////////                SetDirection(1);
////////////            }
////////////        }
////////////    }

////////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////////////    private IEnumerator RestCycle()
////////////    {
////////////        while (!_isDead)
////////////        {
////////////            // Walk for a random interval, then rest, then resume.
////////////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
////////////            if (_isDead) yield break;

////////////            _isResting = true;
////////////            _spriteAnim.SetState(AnimationState.Idle);

////////////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
////////////            if (_isDead) yield break;

////////////            _spriteAnim.SetState(AnimationState.Walk);
////////////            _isResting = false;
////////////        }
////////////    }

////////////    // ─── Death ────────────────────────────────────────────────────────────────

////////////    private void HandleDeath(SoldierStats _)
////////////    {
////////////        _isDead = true;
////////////        StopWalking();
////////////        _spriteAnim.SetState(AnimationState.Death);
////////////        Debug.Log($"[SoldierController] '{name}' died.");
////////////    }

////////////    // ─── Flip ─────────────────────────────────────────────────────────────────

////////////    private void SetDirection(int dir)
////////////    {
////////////        _direction = dir;
////////////        ApplyFlip(dir);
////////////    }

////////////    /// <summary>
////////////    /// Sets localScale.x to ±_baseScaleX.
////////////    /// Uses the magnitude captured in Awake so Canvas Scaler rescaling
////////////    /// can never drift or corrupt the flip.
////////////    ///   dir =  1 → faces right (positive scale)
////////////    ///   dir = -1 → faces left  (mirrored / negative scale)
////////////    /// </summary>
////////////    private void ApplyFlip(int dir)
////////////    {
////////////        Vector3 s = transform.localScale;
////////////        s.x = _baseScaleX * dir;          // FIX 2: always use stored magnitude
////////////        transform.localScale = s;
////////////    }

////////////    // ─── Bounds ───────────────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Centres the patrol range on the soldier's CURRENT local position.
////////////    /// Always call this before resuming patrol after any parent/position change.
////////////    /// </summary>
////////////    private void RecalculatePatrolBoundsFromCurrentPosition()
////////////    {
////////////        float x = CurrentX();
////////////        _leftBound  = x - patrolHalfWidth;
////////////        _rightBound = x + patrolHalfWidth;
////////////    }

////////////    /// <summary>Returns the soldier's current X in local (canvas) space.</summary>
////////////    private float CurrentX()
////////////        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

////////////    // ─── Public API ───────────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Pause (false) or resume (true) patrol.
////////////    /// Called by SoldierDragDrop and any external system.
////////////    /// On resume, bounds are re-centred on the current position — this fixes
////////////    /// patrol drift after a drag-and-retrieve operation.
////////////    /// </summary>
////////////    public void SetPatrolling(bool active)
////////////    {
////////////        if (_isDead) return;

////////////        if (active)
////////////        {
////////////            RecalculatePatrolBoundsFromCurrentPosition();
////////////            StartWalking();
////////////        }
////////////        else
////////////        {
////////////            StopWalking();
////////////        }
////////////    }

////////////    /// <summary>
////////////    /// Re-applies the flip using the stored _direction.
////////////    /// Call this from SoldierDragDrop after every SetParent() so any
////////////    /// Canvas-Scaler-induced localScale.x changes don't kill the flip.
////////////    /// </summary>
////////////    public void RefreshFlip() => ApplyFlip(_direction);

////////////    /// <summary>
////////////    /// Override patrol half-width at runtime (e.g. different-sized spawn area).
////////////    /// Recalculates bounds from the current position immediately.
////////////    /// </summary>
////////////    public void SetPatrolHalfWidth(float halfWidth)
////////////    {
////////////        patrolHalfWidth = halfWidth;
////////////        RecalculatePatrolBoundsFromCurrentPosition();
////////////    }

////////////    // ─── Editor Gizmos ───────────────────────────────────────────────────────
////////////#if UNITY_EDITOR
////////////    private void OnDrawGizmosSelected()
////////////    {
////////////        // Approximation in world space — useful for rough visual debugging.
////////////        float y = transform.position.y;
////////////        float z = transform.position.z;
////////////        float half = patrolHalfWidth * 0.01f; // canvas units → rough world scale

////////////        Gizmos.color = Color.cyan;
////////////        Gizmos.DrawLine(
////////////            new Vector3(transform.position.x - half, y, z),
////////////            new Vector3(transform.position.x + half, y, z));

////////////        Gizmos.color = Color.yellow;
////////////        Gizmos.DrawSphere(new Vector3(transform.position.x - half, y, z), 0.1f);
////////////        Gizmos.DrawSphere(new Vector3(transform.position.x + half, y, z), 0.1f);
////////////    }
////////////#endif
////////////}

//////////using System.Collections;
//////////using UnityEngine;

///////////// <summary>
///////////// AREA FORGE - SoldierController
/////////////
///////////// The soldier walks from its SPAWN position to a DESTINATION X you set in
///////////// the Inspector, then turns around and walks back — forever.
/////////////
///////////// Turning around is done by rotating 180 ° around the Y axis (no scale tricks).
///////////// This works correctly for both UI Images and world-space SpriteRenderers.
/////////////
///////////// ── Inspector setup ───────────────────────────────────────────────────────────
/////////////   Patrol → Destination X   : absolute anchored/local X of the turn-around point.
/////////////                              Set a different value on every prefab instance so
/////////////                              each soldier has a unique patrol range.
/////////////   Patrol → Move Speed      : canvas units per second (try 80–150 for UI).
/////////////   Rest Behaviour           : random walk/idle intervals.
/////////////
///////////// ── How the flip works ────────────────────────────────────────────────────────
/////////////   On Start  the soldier faces RIGHT  (Y rotation = 0 °).
/////////////   On arrival at either boundary it calls:
/////////////       transform.Rotate(0f, 180f, 0f, Space.Self)
/////////////   which toggles between 0 ° and 180 ° each time — visually turning around.
/////////////
///////////// ── Animation ─────────────────────────────────────────────────────────────────
/////////////   No Unity Animator is used for sprites.
/////////////   SpriteLayerAnimator handles per-layer frame stepping.
/////////////   This script only calls SetState(Walk / Idle / Death).
///////////// </summary>
//////////[RequireComponent(typeof(SoldierStats))]
//////////[RequireComponent(typeof(SpriteLayerAnimator))]
//////////public class SoldierController : MonoBehaviour
//////////{
//////////    // ─── Inspector ────────────────────────────────────────────────────────────

//////////    [Header("Patrol")]
//////////    [Tooltip("Absolute X position (local / anchored) where the soldier turns around.\n" +
//////////             "The other turn-around point is wherever the soldier spawns.\n" +
//////////             "Set a UNIQUE value per prefab instance to get different patrol ranges.")]
//////////    [SerializeField] private float destinationX = 300f;

//////////    [Tooltip("Movement speed in local canvas units per second.")]
//////////    [SerializeField] private float moveSpeed = 80f;

//////////    [Header("Rest Behaviour")]
//////////    [Tooltip("Min seconds walking before the soldier pauses for an idle rest.")]
//////////    [SerializeField] private float restIntervalMin = 3f;
//////////    [Tooltip("Max seconds walking before the soldier pauses for an idle rest.")]
//////////    [SerializeField] private float restIntervalMax = 7f;
//////////    [Tooltip("Min seconds the soldier stays idle.")]
//////////    [SerializeField] private float restDurationMin = 1.5f;
//////////    [Tooltip("Max seconds the soldier stays idle.")]
//////////    [SerializeField] private float restDurationMax = 3.5f;

//////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////    private SoldierStats _stats;
//////////    private SpriteLayerAnimator _spriteAnim;
//////////    private RectTransform _rect;          // non-null when the soldier is a UI element

//////////    // Patrol bounds in local / anchored X space — computed in Start from spawn + destination.
//////////    private float _spawnX;
//////////    private float _leftBound;
//////////    private float _rightBound;

//////////    // +1 = moving toward the right bound; -1 = moving toward the left bound.
//////////    private int _direction = 1;

//////////    private bool _isDead = false;
//////////    private bool _isPatrolling = true;
//////////    private bool _isResting = false;

//////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        _stats = GetComponent<SoldierStats>();
//////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
//////////        _rect = GetComponent<RectTransform>();

//////////        _stats.OnSoldierDied += HandleDeath;
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        if (_stats != null)
//////////            _stats.OnSoldierDied -= HandleDeath;
//////////    }

//////////    private void Start()
//////////    {
//////////        // ── Store spawn position, derive patrol bounds ─────────────────────────
//////////        // The soldier patrols between wherever it spawns (spawnX) and destinationX.
//////////        // The soldier always faces RIGHT at spawn (Y rotation = 0).
//////////        // When it reaches either end it rotates 180 ° on Y to turn around.
//////////        _spawnX = CurrentLocalX();

//////////        // Left and right bounds are just whichever of the two points is smaller/larger.
//////////        _leftBound = Mathf.Min(_spawnX, destinationX);
//////////        _rightBound = Mathf.Max(_spawnX, destinationX);

//////////        // Pick starting direction: toward destinationX from spawn.
//////////        _direction = (destinationX >= _spawnX) ? 1 : -1;

//////////        // Face the starting direction — Y rotation is 0 (facing right) by default.
//////////        // If the soldier is heading LEFT on spawn, flip now so it faces that way.
//////////        if (_direction == -1)
//////////            transform.Rotate(0f, 180f, 0f, Space.Self);

//////////        // Wait one frame for Canvas layout to settle before walking.
//////////        StartCoroutine(InitPatrol());
//////////    }

//////////    private void Update()
//////////    {
//////////        if (_isDead || !_isPatrolling || _isResting) return;
//////////        MovePatrol();
//////////    }

//////////    // ─── Initialisation ───────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Waits one frame so Canvas layout finishes placing the object,
//////////    /// then starts the patrol and rest loops.
//////////    /// </summary>
//////////    private IEnumerator InitPatrol()
//////////    {
//////////        yield return null;   // let Canvas layout settle

//////////        // Reread local X after layout (position may shift one frame on spawn).
//////////        _spawnX = CurrentLocalX();
//////////        _leftBound = Mathf.Min(_spawnX, destinationX);
//////////        _rightBound = Mathf.Max(_spawnX, destinationX);

//////////        StartWalking();
//////////        StartCoroutine(RestCycle());

//////////        Debug.Log($"[SoldierController] '{name}' started patrol. " +
//////////                  $"Bounds: {_leftBound:F0} ↔ {_rightBound:F0}  " +
//////////                  $"(spawnX={_spawnX:F0}, destinationX={destinationX:F0})");
//////////    }

//////////    // ─── Patrol ───────────────────────────────────────────────────────────────

//////////    private void StartWalking()
//////////    {
//////////        _isPatrolling = true;
//////////        _isResting = false;
//////////        _spriteAnim.SetState(AnimationState.Walk);
//////////    }

//////////    private void StopWalking()
//////////    {
//////////        _isPatrolling = false;
//////////        _spriteAnim.SetState(AnimationState.Idle);
//////////    }

//////////    private void MovePatrol()
//////////    {
//////////        float step = _direction * moveSpeed * Time.deltaTime;

//////////        if (_rect != null)
//////////        {
//////////            // ── UI soldier (RectTransform) ─────────────────────────────────────
//////////            _rect.anchoredPosition += new Vector2(step, 0f);
//////////            float x = _rect.anchoredPosition.x;

//////////            if (_direction == 1 && x >= _rightBound)
//////////            {
//////////                // Reached right bound — clamp and turn around.
//////////                var p = _rect.anchoredPosition;
//////////                p.x = _rightBound;
//////////                _rect.anchoredPosition = p;
//////////                TurnAround();
//////////            }
//////////            else if (_direction == -1 && x <= _leftBound)
//////////            {
//////////                // Reached left bound — clamp and turn around.
//////////                var p = _rect.anchoredPosition;
//////////                p.x = _leftBound;
//////////                _rect.anchoredPosition = p;
//////////                TurnAround();
//////////            }
//////////        }
//////////        else
//////////        {
//////////            // ── World-space soldier (SpriteRenderer / plain Transform) ─────────
//////////            transform.Translate(step, 0f, 0f, Space.Self);
//////////            float x = transform.localPosition.x;

//////////            if (_direction == 1 && x >= _rightBound)
//////////            {
//////////                var pos = transform.localPosition;
//////////                pos.x = _rightBound;
//////////                transform.localPosition = pos;
//////////                TurnAround();
//////////            }
//////////            else if (_direction == -1 && x <= _leftBound)
//////////            {
//////////                var pos = transform.localPosition;
//////////                pos.x = _leftBound;
//////////                transform.localPosition = pos;
//////////                TurnAround();
//////////            }
//////////        }
//////////    }

//////////    // ─── Turn-Around ──────────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Reverses the movement direction and rotates the soldier 180 ° on the
//////////    /// Y axis so it visually faces the new direction.
//////////    ///
//////////    /// Each call toggles between Y=0 ° (facing right) and Y=180 ° (facing left).
//////////    /// No localScale manipulation — keeps UI layout and Canvas Scaler happy.
//////////    /// </summary>
//////////    private void TurnAround()
//////////    {
//////////        _direction = -_direction;
//////////        transform.Rotate(0f, 180f, 0f, Space.Self);
//////////    }

//////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Loops forever:
//////////    ///   1. Walk for a random interval.
//////////    ///   2. Stop → Idle animation.
//////////    ///   3. Resume walking.
//////////    /// Exits cleanly when the soldier dies.
//////////    /// </summary>
//////////    private IEnumerator RestCycle()
//////////    {
//////////        while (!_isDead)
//////////        {
//////////            // Walk for a random duration.
//////////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//////////            if (_isDead) yield break;

//////////            // Begin rest.
//////////            _isResting = true;
//////////            _spriteAnim.SetState(AnimationState.Idle);

//////////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//////////            if (_isDead) yield break;

//////////            // Resume walk.
//////////            _spriteAnim.SetState(AnimationState.Walk);
//////////            _isResting = false;
//////////        }
//////////    }

//////////    // ─── Death ────────────────────────────────────────────────────────────────

//////////    private void HandleDeath(SoldierStats _)
//////////    {
//////////        _isDead = true;
//////////        StopWalking();
//////////        _spriteAnim.SetState(AnimationState.Death);
//////////        Debug.Log($"[SoldierController] '{name}' died.");
//////////    }

//////////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////////    /// <summary>Returns the soldier's current local / anchored X position.</summary>
//////////    private float CurrentLocalX()
//////////        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

//////////    // ─── Public API ───────────────────────────────────────────────────────────

//////////    /// <summary>
//////////    /// Pause (false) or resume (true) patrol — called by SoldierDragDrop, combat, etc.
//////////    /// On resume, patrol bounds are re-anchored to the current position so the
//////////    /// soldier doesn't try to walk back to an old location after being dragged.
//////////    /// </summary>
//////////    public void SetPatrolling(bool active)
//////////    {
//////////        if (_isDead) return;

//////////        if (active)
//////////        {
//////////            // Re-anchor patrol so the soldier doesn't drift back to its old spawn point
//////////            // after being repositioned (e.g. drag-and-drop).
//////////            _spawnX = CurrentLocalX();
//////////            _leftBound = Mathf.Min(_spawnX, destinationX);
//////////            _rightBound = Mathf.Max(_spawnX, destinationX);
//////////            StartWalking();
//////////        }
//////////        else
//////////        {
//////////            StopWalking();
//////////        }
//////////    }

//////////    /// <summary>
//////////    /// Override the destination X at runtime if you need to reposition the
//////////    /// patrol range without re-instantiating the prefab.
//////////    /// </summary>
//////////    public void SetDestinationX(float newDestinationX)
//////////    {
//////////        destinationX = newDestinationX;
//////////        _leftBound = Mathf.Min(CurrentLocalX(), destinationX);
//////////        _rightBound = Mathf.Max(CurrentLocalX(), destinationX);
//////////    }

//////////    // ─── Editor Gizmos ────────────────────────────────────────────────────────
//////////#if UNITY_EDITOR
//////////    private void OnDrawGizmosSelected()
//////////    {
//////////        // In Edit mode _spawnX is 0; approximate with current position.
//////////        float spawnApprox = Application.isPlaying ? _spawnX : CurrentLocalX();
//////////        float destX = destinationX;

//////////        // Convert local-space X to world space for gizmo drawing.
//////////        // For a UI canvas this is approximate but good enough for debugging.
//////////        float y = transform.position.y;
//////////        float z = transform.position.z;

//////////        // Draw the patrol line.
//////////        Gizmos.color = Color.cyan;
//////////        Vector3 leftWorld = transform.parent != null
//////////            ? transform.parent.TransformPoint(new Vector3(Mathf.Min(spawnApprox, destX), 0f, 0f))
//////////            : new Vector3(Mathf.Min(spawnApprox, destX), y, z);
//////////        Vector3 rightWorld = transform.parent != null
//////////            ? transform.parent.TransformPoint(new Vector3(Mathf.Max(spawnApprox, destX), 0f, 0f))
//////////            : new Vector3(Mathf.Max(spawnApprox, destX), y, z);
//////////        Gizmos.DrawLine(leftWorld, rightWorld);

//////////        // Spawn marker (yellow).
//////////        Gizmos.color = Color.yellow;
//////////        Vector3 spawnWorld = transform.parent != null
//////////            ? transform.parent.TransformPoint(new Vector3(spawnApprox, 0f, 0f))
//////////            : new Vector3(spawnApprox, y, z);
//////////        Gizmos.DrawSphere(spawnWorld, 6f);

//////////        // Destination marker (green).
//////////        Gizmos.color = Color.green;
//////////        Vector3 destWorld = transform.parent != null
//////////            ? transform.parent.TransformPoint(new Vector3(destX, 0f, 0f))
//////////            : new Vector3(destX, y, z);
//////////        Gizmos.DrawSphere(destWorld, 6f);

//////////        // Label in Scene view (Editor only).
//////////        UnityEditor.Handles.color = Color.white;
//////////        UnityEditor.Handles.Label(spawnWorld + Vector3.up * 12f, "Spawn");
//////////        UnityEditor.Handles.Label(destWorld + Vector3.up * 12f, "Destination");
//////////    }
//////////#endif
//////////}

////////using System.Collections;
////////using UnityEngine;

/////////// <summary>
/////////// AREA FORGE - SoldierController
///////////
/////////// The soldier walks from its SPAWN position to a DESTINATION X you set in
/////////// the Inspector, then turns around and walks back — forever.
///////////
/////////// Turning around is done by rotating 180 ° around the Y axis (no scale tricks).
/////////// This works correctly for both UI Images and world-space SpriteRenderers.
///////////
/////////// ── Inspector setup ───────────────────────────────────────────────────────────
///////////   Patrol → Destination X   : absolute anchored/local X of the turn-around point.
///////////                              Set a different value on every prefab instance so
///////////                              each soldier has a unique patrol range.
///////////   Patrol → Move Speed      : canvas units per second (try 80–150 for UI).
///////////   Rest Behaviour           : random walk/idle intervals.
///////////
/////////// ── How the flip works ────────────────────────────────────────────────────────
///////////   On Start  the soldier faces RIGHT  (Y rotation = 0 °).
///////////   On arrival at either boundary it calls:
///////////       transform.Rotate(0f, 180f, 0f, Space.Self)
///////////   which toggles between 0 ° and 180 ° each time — visually turning around.
///////////
/////////// ── Animation ─────────────────────────────────────────────────────────────────
///////////   No Unity Animator is used for sprites.
///////////   SpriteLayerAnimator handles per-layer frame stepping.
///////////   This script only calls SetState(Walk / Idle / Death).
/////////// </summary>
////////[RequireComponent(typeof(SoldierStats))]
////////[RequireComponent(typeof(SpriteLayerAnimator))]
////////public class SoldierController : MonoBehaviour
////////{
////////    // ─── Inspector ────────────────────────────────────────────────────────────

////////    [Header("Patrol")]
////////    [Tooltip("Absolute X position (local / anchored) where the soldier turns around.\n" +
////////             "The other turn-around point is wherever the soldier spawns.\n" +
////////             "Set a UNIQUE value per prefab instance to get different patrol ranges.")]
////////    [SerializeField] private float destinationX = 300f;

////////    [Tooltip("Movement speed in local canvas units per second.")]
////////    [SerializeField] private float moveSpeed = 80f;

////////    [Header("Rest Behaviour")]
////////    [Tooltip("Min seconds walking before the soldier pauses for an idle rest.")]
////////    [SerializeField] private float restIntervalMin = 3f;
////////    [Tooltip("Max seconds walking before the soldier pauses for an idle rest.")]
////////    [SerializeField] private float restIntervalMax = 7f;
////////    [Tooltip("Min seconds the soldier stays idle.")]
////////    [SerializeField] private float restDurationMin = 1.5f;
////////    [Tooltip("Max seconds the soldier stays idle.")]
////////    [SerializeField] private float restDurationMax = 3.5f;

////////    // ─── Private ──────────────────────────────────────────────────────────────

////////    private SoldierStats _stats;
////////    private SpriteLayerAnimator _spriteAnim;
////////    private RectTransform _rect;          // non-null when the soldier is a UI element

////////    // Patrol bounds in local / anchored X space — computed in Start from spawn + destination.
////////    private float _spawnX;
////////    private float _leftBound;
////////    private float _rightBound;

////////    // +1 = moving toward the right bound; -1 = moving toward the left bound.
////////    private int _direction = 1;

////////    private bool _isDead = false;
////////    private bool _isPatrolling = true;
////////    private bool _isResting = false;

////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        _stats = GetComponent<SoldierStats>();
////////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
////////        _rect = GetComponent<RectTransform>();

////////        _stats.OnSoldierDied += HandleDeath;
////////    }

////////    private void OnDestroy()
////////    {
////////        if (_stats != null)
////////            _stats.OnSoldierDied -= HandleDeath;
////////    }

////////    private void Start()
////////    {
////////        // ── Store spawn position, derive patrol bounds ─────────────────────────
////////        // The soldier patrols between wherever it spawns (spawnX) and destinationX.
////////        // The soldier always faces RIGHT at spawn (Y rotation = 0).
////////        // When it reaches either end it rotates 180 ° on Y to turn around.
////////        _spawnX = CurrentLocalX();

////////        // Left and right bounds are just whichever of the two points is smaller/larger.
////////        _leftBound = Mathf.Min(_spawnX, destinationX);
////////        _rightBound = Mathf.Max(_spawnX, destinationX);

////////        // Pick starting direction: toward destinationX from spawn.
////////        _direction = (destinationX >= _spawnX) ? 1 : -1;

////////        // Face the starting direction using localScale.x.
////////        // Positive = facing right (default), negative = facing left.
////////        if (_direction == -1)
////////        {
////////            Vector3 s = transform.localScale;
////////            s.x = -Mathf.Abs(s.x);
////////            transform.localScale = s;
////////        }

////////        // Wait one frame for Canvas layout to settle before walking.
////////        StartCoroutine(InitPatrol());
////////    }

////////    private void Update()
////////    {
////////        if (_isDead || !_isPatrolling || _isResting) return;
////////        MovePatrol();
////////    }

////////    // ─── Initialisation ───────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Waits one frame so Canvas layout finishes placing the object,
////////    /// then starts the patrol and rest loops.
////////    /// </summary>
////////    private IEnumerator InitPatrol()
////////    {
////////        yield return null;   // let Canvas layout settle

////////        // Reread local X after layout (position may shift one frame on spawn).
////////        _spawnX = CurrentLocalX();
////////        _leftBound = Mathf.Min(_spawnX, destinationX);
////////        _rightBound = Mathf.Max(_spawnX, destinationX);

////////        StartWalking();
////////        StartCoroutine(RestCycle());

////////        Debug.Log($"[SoldierController] '{name}' started patrol. " +
////////                  $"Bounds: {_leftBound:F0} ↔ {_rightBound:F0}  " +
////////                  $"(spawnX={_spawnX:F0}, destinationX={destinationX:F0})");
////////    }

////////    // ─── Patrol ───────────────────────────────────────────────────────────────

////////    private void StartWalking()
////////    {
////////        _isPatrolling = true;
////////        _isResting = false;
////////        _spriteAnim.SetState(AnimationState.Walk);
////////    }

////////    private void StopWalking()
////////    {
////////        _isPatrolling = false;
////////        _spriteAnim.SetState(AnimationState.Idle);
////////    }

////////    private void MovePatrol()
////////    {
////////        float step = _direction * moveSpeed * Time.deltaTime;

////////        if (_rect != null)
////////        {
////////            // ── UI soldier (RectTransform) ─────────────────────────────────────
////////            _rect.anchoredPosition += new Vector2(step, 0f);
////////            float x = _rect.anchoredPosition.x;

////////            if (_direction == 1 && x >= _rightBound)
////////            {
////////                // Reached right bound — clamp and turn around.
////////                var p = _rect.anchoredPosition;
////////                p.x = _rightBound;
////////                _rect.anchoredPosition = p;
////////                TurnAround();
////////            }
////////            else if (_direction == -1 && x <= _leftBound)
////////            {
////////                // Reached left bound — clamp and turn around.
////////                var p = _rect.anchoredPosition;
////////                p.x = _leftBound;
////////                _rect.anchoredPosition = p;
////////                TurnAround();
////////            }
////////        }
////////        else
////////        {
////////            // ── World-space soldier (SpriteRenderer / plain Transform) ─────────
////////            transform.Translate(step, 0f, 0f, Space.Self);
////////            float x = transform.localPosition.x;

////////            if (_direction == 1 && x >= _rightBound)
////////            {
////////                var pos = transform.localPosition;
////////                pos.x = _rightBound;
////////                transform.localPosition = pos;
////////                TurnAround();
////////            }
////////            else if (_direction == -1 && x <= _leftBound)
////////            {
////////                var pos = transform.localPosition;
////////                pos.x = _leftBound;
////////                transform.localPosition = pos;
////////                TurnAround();
////////            }
////////        }
////////    }

////////    // ─── Turn-Around ──────────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Reverses the movement direction and flips the soldier to face it.
////////    ///
////////    /// Uses localScale.x *= -1 instead of Y-axis rotation.
////////    /// Y rotation makes the RectTransform face away from the camera in Camera/World
////////    /// Space canvases, which breaks EventSystem raycasts — the soldier becomes
////////    /// unclickable and un-draggable when walking left.
////////    /// localScale.x flip achieves the same visual result with no raycast side-effects.
////////    /// </summary>
////////    private void TurnAround()
////////    {
////////        _direction = -_direction;

////////        Vector3 s = transform.localScale;
////////        s.x = -s.x;            // positive = facing right, negative = facing left
////////        transform.localScale = s;
////////    }

////////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Loops forever:
////////    ///   1. Walk for a random interval.
////////    ///   2. Stop → Idle animation.
////////    ///   3. Resume walking.
////////    /// Exits cleanly when the soldier dies.
////////    /// </summary>
////////    private IEnumerator RestCycle()
////////    {
////////        while (!_isDead)
////////        {
////////            // Walk for a random duration.
////////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
////////            if (_isDead) yield break;

////////            // Begin rest.
////////            _isResting = true;
////////            _spriteAnim.SetState(AnimationState.Idle);

////////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
////////            if (_isDead) yield break;

////////            // Resume walk.
////////            _spriteAnim.SetState(AnimationState.Walk);
////////            _isResting = false;
////////        }
////////    }

////////    // ─── Death ────────────────────────────────────────────────────────────────

////////    private void HandleDeath(SoldierStats _)
////////    {
////////        _isDead = true;
////////        StopWalking();
////////        _spriteAnim.SetState(AnimationState.Death);
////////        Debug.Log($"[SoldierController] '{name}' died.");
////////    }

////////    // ─── Helpers ──────────────────────────────────────────────────────────────

////////    /// <summary>Returns the soldier's current local / anchored X position.</summary>
////////    private float CurrentLocalX()
////////        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

////////    // ─── Public API ───────────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Pause (false) or resume (true) patrol — called by SoldierDragDrop, combat, etc.
////////    /// On resume, patrol bounds are re-anchored to the current position so the
////////    /// soldier doesn't try to walk back to an old location after being dragged.
////////    /// </summary>
////////    public void SetPatrolling(bool active)
////////    {
////////        if (_isDead) return;

////////        if (active)
////////        {
////////            // Re-anchor patrol so the soldier doesn't drift back to its old spawn point
////////            // after being repositioned (e.g. drag-and-drop).
////////            _spawnX = CurrentLocalX();
////////            _leftBound = Mathf.Min(_spawnX, destinationX);
////////            _rightBound = Mathf.Max(_spawnX, destinationX);
////////            StartWalking();
////////        }
////////        else
////////        {
////////            StopWalking();
////////        }
////////    }

////////    /// <summary>
////////    /// Override the destination X at runtime if you need to reposition the
////////    /// patrol range without re-instantiating the prefab.
////////    /// </summary>
////////    public void SetDestinationX(float newDestinationX)
////////    {
////////        destinationX = newDestinationX;
////////        _leftBound = Mathf.Min(CurrentLocalX(), destinationX);
////////        _rightBound = Mathf.Max(CurrentLocalX(), destinationX);
////////    }

////////    // ─── Editor Gizmos ────────────────────────────────────────────────────────
////////#if UNITY_EDITOR
////////    private void OnDrawGizmosSelected()
////////    {
////////        // In Edit mode _spawnX is 0; approximate with current position.
////////        float spawnApprox = Application.isPlaying ? _spawnX : CurrentLocalX();
////////        float destX = destinationX;

////////        // Convert local-space X to world space for gizmo drawing.
////////        // For a UI canvas this is approximate but good enough for debugging.
////////        float y = transform.position.y;
////////        float z = transform.position.z;

////////        // Draw the patrol line.
////////        Gizmos.color = Color.cyan;
////////        Vector3 leftWorld = transform.parent != null
////////            ? transform.parent.TransformPoint(new Vector3(Mathf.Min(spawnApprox, destX), 0f, 0f))
////////            : new Vector3(Mathf.Min(spawnApprox, destX), y, z);
////////        Vector3 rightWorld = transform.parent != null
////////            ? transform.parent.TransformPoint(new Vector3(Mathf.Max(spawnApprox, destX), 0f, 0f))
////////            : new Vector3(Mathf.Max(spawnApprox, destX), y, z);
////////        Gizmos.DrawLine(leftWorld, rightWorld);

////////        // Spawn marker (yellow).
////////        Gizmos.color = Color.yellow;
////////        Vector3 spawnWorld = transform.parent != null
////////            ? transform.parent.TransformPoint(new Vector3(spawnApprox, 0f, 0f))
////////            : new Vector3(spawnApprox, y, z);
////////        Gizmos.DrawSphere(spawnWorld, 6f);

////////        // Destination marker (green).
////////        Gizmos.color = Color.green;
////////        Vector3 destWorld = transform.parent != null
////////            ? transform.parent.TransformPoint(new Vector3(destX, 0f, 0f))
////////            : new Vector3(destX, y, z);
////////        Gizmos.DrawSphere(destWorld, 6f);

////////        // Label in Scene view (Editor only).
////////        UnityEditor.Handles.color = Color.white;
////////        UnityEditor.Handles.Label(spawnWorld + Vector3.up * 12f, "Spawn");
////////        UnityEditor.Handles.Label(destWorld + Vector3.up * 12f, "Destination");
////////    }
////////#endif
////////}

//////using System.Collections;
//////using UnityEngine;

///////// <summary>
///////// AREA FORGE - SoldierController
/////////
///////// The soldier walks from its SPAWN position to a DESTINATION X you set in
///////// the Inspector, then turns around and walks back — forever.
/////////
///////// Turning around is done by rotating 180 ° around the Y axis (no scale tricks).
///////// This works correctly for both UI Images and world-space SpriteRenderers.
/////////
///////// ── Inspector setup ───────────────────────────────────────────────────────────
/////////   Patrol → Destination X   : absolute anchored/local X of the turn-around point.
/////////                              Set a different value on every prefab instance so
/////////                              each soldier has a unique patrol range.
/////////   Patrol → Move Speed      : canvas units per second (try 80–150 for UI).
/////////   Rest Behaviour           : random walk/idle intervals.
/////////
///////// ── How the flip works ────────────────────────────────────────────────────────
/////////   On Start  the soldier faces RIGHT  (Y rotation = 0 °).
/////////   On arrival at either boundary it calls:
/////////       transform.Rotate(0f, 180f, 0f, Space.Self)
/////////   which toggles between 0 ° and 180 ° each time — visually turning around.
/////////
///////// ── Animation ─────────────────────────────────────────────────────────────────
/////////   No Unity Animator is used for sprites.
/////////   SpriteLayerAnimator handles per-layer frame stepping.
/////////   This script only calls SetState(Walk / Idle / Death).
///////// </summary>
//////[RequireComponent(typeof(SoldierStats))]
//////[RequireComponent(typeof(SpriteLayerAnimator))]
//////public class SoldierController : MonoBehaviour
//////{
//////    // ─── Inspector ────────────────────────────────────────────────────────────

//////    [Header("Patrol")]
//////    [Tooltip("Absolute X position (local / anchored) where the soldier turns around.\n" +
//////             "The other turn-around point is wherever the soldier spawns.\n" +
//////             "Set a UNIQUE value per prefab instance to get different patrol ranges.")]
//////    [SerializeField] private float destinationX = 300f;

//////    [Tooltip("Movement speed in local canvas units per second.")]
//////    [SerializeField] private float moveSpeed = 80f;

//////    [Header("Flip")]
//////    [Tooltip("Tick ON  if your sprite sheet has the character facing RIGHT by default.\n" +
//////             "Tick OFF if your sprite sheet has the character facing LEFT  by default.\n" +
//////             "If the soldier moonwalks, flip this toggle.")]
//////    [SerializeField] private bool spriteDefaultFacingRight = false;

//////    [Header("Rest Behaviour")]
//////    [Tooltip("Min seconds walking before the soldier pauses for an idle rest.")]
//////    [SerializeField] private float restIntervalMin = 3f;
//////    [Tooltip("Max seconds walking before the soldier pauses for an idle rest.")]
//////    [SerializeField] private float restIntervalMax = 7f;
//////    [Tooltip("Min seconds the soldier stays idle.")]
//////    [SerializeField] private float restDurationMin = 1.5f;
//////    [Tooltip("Max seconds the soldier stays idle.")]
//////    [SerializeField] private float restDurationMax = 3.5f;

//////    // ─── Private ──────────────────────────────────────────────────────────────

//////    private SoldierStats _stats;
//////    private SpriteLayerAnimator _spriteAnim;
//////    private RectTransform _rect;          // non-null when the soldier is a UI element

//////    // Patrol bounds in local / anchored X space — computed in Start from spawn + destination.
//////    private float _spawnX;
//////    private float _leftBound;
//////    private float _rightBound;

//////    // +1 = moving toward the right bound; -1 = moving toward the left bound.
//////    private int _direction = 1;

//////    private bool _isDead = false;
//////    private bool _isPatrolling = true;
//////    private bool _isResting = false;

//////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _stats = GetComponent<SoldierStats>();
//////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
//////        _rect = GetComponent<RectTransform>();

//////        _stats.OnSoldierDied += HandleDeath;
//////    }

//////    private void OnDestroy()
//////    {
//////        if (_stats != null)
//////            _stats.OnSoldierDied -= HandleDeath;
//////    }

//////    private void Start()
//////    {
//////        // ── Store spawn position, derive patrol bounds ─────────────────────────
//////        // The soldier patrols between wherever it spawns (spawnX) and destinationX.
//////        // The soldier always faces RIGHT at spawn (Y rotation = 0).
//////        // When it reaches either end it rotates 180 ° on Y to turn around.
//////        _spawnX = CurrentLocalX();

//////        // Left and right bounds are just whichever of the two points is smaller/larger.
//////        _leftBound = Mathf.Min(_spawnX, destinationX);
//////        _rightBound = Mathf.Max(_spawnX, destinationX);

//////        // Pick starting direction: toward destinationX from spawn.
//////        _direction = (destinationX >= _spawnX) ? 1 : -1;

//////        // Align initial scale with the movement direction.
//////        // spriteDefaultFacingRight tells us which way localScale.x > 0 points.
//////        //   e.g. if sprite faces LEFT by default and soldier starts going RIGHT,
//////        //   we need scale.x negative so it faces right.
//////        ApplyFacingScale(_direction);

//////        // Wait one frame for Canvas layout to settle before walking.
//////        StartCoroutine(InitPatrol());
//////    }

//////    private void Update()
//////    {
//////        if (_isDead || !_isPatrolling || _isResting) return;
//////        MovePatrol();
//////    }

//////    // ─── Initialisation ───────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Waits one frame so Canvas layout finishes placing the object,
//////    /// then starts the patrol and rest loops.
//////    /// </summary>
//////    private IEnumerator InitPatrol()
//////    {
//////        yield return null;   // let Canvas layout settle

//////        // Reread local X after layout (position may shift one frame on spawn).
//////        _spawnX = CurrentLocalX();
//////        _leftBound = Mathf.Min(_spawnX, destinationX);
//////        _rightBound = Mathf.Max(_spawnX, destinationX);

//////        StartWalking();
//////        StartCoroutine(RestCycle());

//////        Debug.Log($"[SoldierController] '{name}' started patrol. " +
//////                  $"Bounds: {_leftBound:F0} ↔ {_rightBound:F0}  " +
//////                  $"(spawnX={_spawnX:F0}, destinationX={destinationX:F0})");
//////    }

//////    // ─── Patrol ───────────────────────────────────────────────────────────────

//////    private void StartWalking()
//////    {
//////        _isPatrolling = true;
//////        _isResting = false;
//////        _spriteAnim.SetState(AnimationState.Walk);
//////    }

//////    private void StopWalking()
//////    {
//////        _isPatrolling = false;
//////        _spriteAnim.SetState(AnimationState.Idle);
//////    }

//////    private void MovePatrol()
//////    {
//////        float step = _direction * moveSpeed * Time.deltaTime;

//////        if (_rect != null)
//////        {
//////            // ── UI soldier (RectTransform) ─────────────────────────────────────
//////            _rect.anchoredPosition += new Vector2(step, 0f);
//////            float x = _rect.anchoredPosition.x;

//////            if (_direction == 1 && x >= _rightBound)
//////            {
//////                // Reached right bound — clamp and turn around.
//////                var p = _rect.anchoredPosition;
//////                p.x = _rightBound;
//////                _rect.anchoredPosition = p;
//////                TurnAround();
//////            }
//////            else if (_direction == -1 && x <= _leftBound)
//////            {
//////                // Reached left bound — clamp and turn around.
//////                var p = _rect.anchoredPosition;
//////                p.x = _leftBound;
//////                _rect.anchoredPosition = p;
//////                TurnAround();
//////            }
//////        }
//////        else
//////        {
//////            // ── World-space soldier (SpriteRenderer / plain Transform) ─────────
//////            transform.Translate(step, 0f, 0f, Space.Self);
//////            float x = transform.localPosition.x;

//////            if (_direction == 1 && x >= _rightBound)
//////            {
//////                var pos = transform.localPosition;
//////                pos.x = _rightBound;
//////                transform.localPosition = pos;
//////                TurnAround();
//////            }
//////            else if (_direction == -1 && x <= _leftBound)
//////            {
//////                var pos = transform.localPosition;
//////                pos.x = _leftBound;
//////                transform.localPosition = pos;
//////                TurnAround();
//////            }
//////        }
//////    }

//////    // ─── Turn-Around ──────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Reverses movement direction and flips the sprite to face it.
//////    /// Uses localScale.x so the RectTransform rotation stays at zero —
//////    /// Y-axis rotation breaks EventSystem raycasts in Camera/World Space canvases.
//////    /// </summary>
//////    private void TurnAround()
//////    {
//////        _direction = -_direction;
//////        ApplyFacingScale(_direction);
//////    }

//////    /// <summary>
//////    /// Sets localScale.x so the sprite faces the given direction.
//////    ///   dir  +1 = moving right → sprite must face right
//////    ///   dir  -1 = moving left  → sprite must face left
//////    /// spriteDefaultFacingRight tells us which sign of scale.x = facing right.
//////    /// </summary>
//////    private void ApplyFacingScale(int dir)
//////    {
//////        // facingRightIsPositive: if true, scale.x > 0 means facing right.
//////        // When the sprite sheet faces LEFT by default, scale.x > 0 = facing LEFT,
//////        // so we invert: scale.x < 0 = facing right.
//////        float abs = Mathf.Abs(transform.localScale.x);
//////        float sign;
//////        if (spriteDefaultFacingRight)
//////            sign = (dir == 1) ? 1f : -1f;   // positive → right  | negative → left
//////        else
//////            sign = (dir == 1) ? -1f : 1f;   // negative → right  | positive → left

//////        Vector3 s = transform.localScale;
//////        s.x = abs * sign;
//////        transform.localScale = s;
//////    }

//////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Loops forever:
//////    ///   1. Walk for a random interval.
//////    ///   2. Stop → Idle animation.
//////    ///   3. Resume walking.
//////    /// Exits cleanly when the soldier dies.
//////    /// </summary>
//////    private IEnumerator RestCycle()
//////    {
//////        while (!_isDead)
//////        {
//////            // Walk for a random duration.
//////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//////            if (_isDead) yield break;

//////            // Begin rest.
//////            _isResting = true;
//////            _spriteAnim.SetState(AnimationState.Idle);

//////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//////            if (_isDead) yield break;

//////            // Resume walk.
//////            _spriteAnim.SetState(AnimationState.Walk);
//////            _isResting = false;
//////        }
//////    }

//////    // ─── Death ────────────────────────────────────────────────────────────────

//////    private void HandleDeath(SoldierStats _)
//////    {
//////        _isDead = true;
//////        StopWalking();
//////        _spriteAnim.SetState(AnimationState.Death);
//////        Debug.Log($"[SoldierController] '{name}' died.");
//////    }

//////    // ─── Helpers ──────────────────────────────────────────────────────────────

//////    /// <summary>Returns the soldier's current local / anchored X position.</summary>
//////    private float CurrentLocalX()
//////        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

//////    // ─── Public API ───────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Pause (false) or resume (true) patrol — called by SoldierDragDrop, combat, etc.
//////    /// On resume, patrol bounds are re-anchored to the current position so the
//////    /// soldier doesn't try to walk back to an old location after being dragged.
//////    /// </summary>
//////    public void SetPatrolling(bool active)
//////    {
//////        if (_isDead) return;

//////        if (active)
//////        {
//////            // Re-anchor patrol so the soldier doesn't drift back to its old spawn point
//////            // after being repositioned (e.g. drag-and-drop).
//////            _spawnX = CurrentLocalX();
//////            _leftBound = Mathf.Min(_spawnX, destinationX);
//////            _rightBound = Mathf.Max(_spawnX, destinationX);
//////            StartWalking();
//////        }
//////        else
//////        {
//////            StopWalking();
//////        }
//////    }

//////    /// <summary>
//////    /// Override the destination X at runtime if you need to reposition the
//////    /// patrol range without re-instantiating the prefab.
//////    /// </summary>
//////    public void SetDestinationX(float newDestinationX)
//////    {
//////        destinationX = newDestinationX;
//////        _leftBound = Mathf.Min(CurrentLocalX(), destinationX);
//////        _rightBound = Mathf.Max(CurrentLocalX(), destinationX);
//////    }

//////    // ─── Editor Gizmos ────────────────────────────────────────────────────────
//////#if UNITY_EDITOR
//////    private void OnDrawGizmosSelected()
//////    {
//////        // In Edit mode _spawnX is 0; approximate with current position.
//////        float spawnApprox = Application.isPlaying ? _spawnX : CurrentLocalX();
//////        float destX = destinationX;

//////        // Convert local-space X to world space for gizmo drawing.
//////        // For a UI canvas this is approximate but good enough for debugging.
//////        float y = transform.position.y;
//////        float z = transform.position.z;

//////        // Draw the patrol line.
//////        Gizmos.color = Color.cyan;
//////        Vector3 leftWorld = transform.parent != null
//////            ? transform.parent.TransformPoint(new Vector3(Mathf.Min(spawnApprox, destX), 0f, 0f))
//////            : new Vector3(Mathf.Min(spawnApprox, destX), y, z);
//////        Vector3 rightWorld = transform.parent != null
//////            ? transform.parent.TransformPoint(new Vector3(Mathf.Max(spawnApprox, destX), 0f, 0f))
//////            : new Vector3(Mathf.Max(spawnApprox, destX), y, z);
//////        Gizmos.DrawLine(leftWorld, rightWorld);

//////        // Spawn marker (yellow).
//////        Gizmos.color = Color.yellow;
//////        Vector3 spawnWorld = transform.parent != null
//////            ? transform.parent.TransformPoint(new Vector3(spawnApprox, 0f, 0f))
//////            : new Vector3(spawnApprox, y, z);
//////        Gizmos.DrawSphere(spawnWorld, 6f);

//////        // Destination marker (green).
//////        Gizmos.color = Color.green;
//////        Vector3 destWorld = transform.parent != null
//////            ? transform.parent.TransformPoint(new Vector3(destX, 0f, 0f))
//////            : new Vector3(destX, y, z);
//////        Gizmos.DrawSphere(destWorld, 6f);

//////        // Label in Scene view (Editor only).
//////        UnityEditor.Handles.color = Color.white;
//////        UnityEditor.Handles.Label(spawnWorld + Vector3.up * 12f, "Spawn");
//////        UnityEditor.Handles.Label(destWorld + Vector3.up * 12f, "Destination");
//////    }
//////#endif
//////}


////using System.Collections;
////using UnityEngine;

/////// <summary>
/////// AREA FORGE - SoldierController
///////
/////// Patrol movement, idle resting, directional flip, and death.
/////// Drives SpriteLayerAnimator for per-layer animation (Walk / Idle / Death).
///////
/////// ── FIX 1: Flip never worked ─────────────────────────────────────────────────
///////   ROOT CAUSE: After Instantiate() or SetParent(), Canvas Scaler rewrites
///////   localScale.x. Reading Mathf.Abs(localScale.x) after that gives a wrong
///////   magnitude that drifts every time the parent changes.
///////
///////   FIX: _baseScaleX is captured in Awake() before any reparenting.
///////   ApplyFlip() always writes exactly ±_baseScaleX — never drifts.
///////
/////// ── FIX 2: Spawn / patrol point moves after drag ──────────────────────────────
///////   ROOT CAUSE: SetPatrolling(true) was recalculating patrol bounds from the
///////   soldier's current position, so every drag re-anchored the home point.
///////
///////   FIX: _leftBound and _rightBound are calculated ONCE inside InitPatrol()
///////   and NEVER changed again. SetPatrolling(true) just resumes walking.
/////// </summary>
////[RequireComponent(typeof(SoldierStats))]
////[RequireComponent(typeof(SpriteLayerAnimator))]
////public class SoldierController : MonoBehaviour
////{
////    // ─── Inspector ────────────────────────────────────────────────────────────

////    [Header("Patrol")]
////    [Tooltip("Absolute anchored/local X where the soldier turns around.\n" +
////             "The other turn-around point is the soldier's spawn position.\n" +
////             "Example: spawn X=100, Destination=400 → patrols between 100 and 400.")]
////    [SerializeField] private float destinationX = 300f;

////    [Tooltip("Speed in canvas units per second. Try 80-150 for UI.")]
////    [SerializeField] private float moveSpeed = 80f;

////    [Header("Rest Behaviour")]
////    [SerializeField] private float restIntervalMin = 3f;
////    [SerializeField] private float restIntervalMax = 7f;
////    [SerializeField] private float restDurationMin = 1.5f;
////    [SerializeField] private float restDurationMax = 3.5f;

////    // ─── Private ──────────────────────────────────────────────────────────────

////    private SoldierStats _stats;
////    private SpriteLayerAnimator _spriteAnim;
////    private RectTransform _rect;

////    // FIX 1: captured once in Awake — never recalculated
////    private float _baseScaleX;

////    // FIX 2: set once in InitPatrol — never recalculated
////    private float _leftBound;
////    private float _rightBound;

////    private int _direction = 1;
////    private bool _isDead = false;
////    private bool _isPatrolling = false;
////    private bool _isResting = false;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _stats = GetComponent<SoldierStats>();
////        _spriteAnim = GetComponent<SpriteLayerAnimator>();
////        _rect = GetComponent<RectTransform>();

////        // FIX 1: capture scale magnitude RIGHT NOW, before any SetParent/Canvas
////        // Scaler can change localScale.x. This value is the permanent source of
////        // truth for the flip — ApplyFlip() always uses this, never re-reads localScale.
////        _baseScaleX = Mathf.Abs(transform.localScale.x);
////        if (_baseScaleX < 0.001f) _baseScaleX = 1f;

////        _stats.OnSoldierDied += HandleDeath;
////    }

////    private void OnDestroy()
////    {
////        if (_stats != null)
////            _stats.OnSoldierDied -= HandleDeath;
////    }

////    private void Start()
////    {
////        // Yield one frame so Canvas layout writes the final anchoredPosition.
////        // Reading anchoredPosition in Start() returns 0 for spawned UI objects —
////        // Canvas only places them at the correct spot at the end of the first frame.
////        StartCoroutine(InitPatrol());
////    }

////    private void Update()
////    {
////        if (_isDead || !_isPatrolling || _isResting) return;
////        MovePatrol();
////    }

////    // ─── Initialisation ───────────────────────────────────────────────────────

////    private IEnumerator InitPatrol()
////    {
////        yield return null;   // one frame — Canvas writes final anchoredPosition here

////        float spawnX = CurrentLocalX();

////        // FIX 2: patrol bounds are LOCKED here and never changed again.
////        // The soldier patrols between its spawn position and destinationX.
////        _leftBound = Mathf.Min(spawnX, destinationX);
////        _rightBound = Mathf.Max(spawnX, destinationX);

////        // Start facing toward destinationX.
////        _direction = destinationX >= spawnX ? 1 : -1;
////        ApplyFlip(_direction);

////        StartWalking();
////        StartCoroutine(RestCycle());

////        Debug.Log($"[SoldierController] '{name}' patrol locked: " +
////                  $"{_leftBound:F0} to {_rightBound:F0}  (spawnX={spawnX:F0})");
////    }

////    // ─── Patrol ───────────────────────────────────────────────────────────────

////    private void StartWalking()
////    {
////        _isPatrolling = true;
////        _isResting = false;
////        _spriteAnim.SetState(AnimationState.Walk);
////    }

////    private void StopWalking()
////    {
////        _isPatrolling = false;
////        _spriteAnim.SetState(AnimationState.Idle);
////    }

////    private void MovePatrol()
////    {
////        float step = _direction * moveSpeed * Time.deltaTime;

////        if (_rect != null)
////        {
////            _rect.anchoredPosition += new Vector2(step, 0f);
////            float x = _rect.anchoredPosition.x;

////            if (_direction == 1 && x >= _rightBound)
////            {
////                var p = _rect.anchoredPosition;
////                p.x = _rightBound;
////                _rect.anchoredPosition = p;
////                SetDirection(-1);
////            }
////            else if (_direction == -1 && x <= _leftBound)
////            {
////                var p = _rect.anchoredPosition;
////                p.x = _leftBound;
////                _rect.anchoredPosition = p;
////                SetDirection(1);
////            }
////        }
////        else
////        {
////            transform.Translate(step, 0f, 0f, Space.Self);
////            float x = transform.localPosition.x;

////            if (_direction == 1 && x >= _rightBound)
////            {
////                var pos = transform.localPosition;
////                pos.x = _rightBound;
////                transform.localPosition = pos;
////                SetDirection(-1);
////            }
////            else if (_direction == -1 && x <= _leftBound)
////            {
////                var pos = transform.localPosition;
////                pos.x = _leftBound;
////                transform.localPosition = pos;
////                SetDirection(1);
////            }
////        }
////    }

////    // ─── Rest Cycle ───────────────────────────────────────────────────────────

////    private IEnumerator RestCycle()
////    {
////        while (!_isDead)
////        {
////            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
////            if (_isDead) yield break;

////            _isResting = true;
////            _spriteAnim.SetState(AnimationState.Idle);

////            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
////            if (_isDead) yield break;

////            _spriteAnim.SetState(AnimationState.Walk);
////            _isResting = false;
////        }
////    }

////    // ─── Death ────────────────────────────────────────────────────────────────

////    private void HandleDeath(SoldierStats _)
////    {
////        _isDead = true;
////        StopWalking();
////        _spriteAnim.SetState(AnimationState.Death);
////        Debug.Log($"[SoldierController] '{name}' died.");
////    }

////    // ─── Flip ─────────────────────────────────────────────────────────────────

////    private void SetDirection(int dir)
////    {
////        _direction = dir;
////        ApplyFlip(dir);
////    }

////    /// <summary>
////    /// Flips by writing ±_baseScaleX to localScale.x.
////    ///
////    /// NEVER reads Mathf.Abs(transform.localScale.x) — that value drifts
////    /// every time a parent change triggers Canvas Scaler rescaling.
////    /// _baseScaleX is the original prefab scale from Awake() and never changes.
////    ///
////    ///   dir =  1 → faces right (+_baseScaleX)
////    ///   dir = -1 → faces left  (-_baseScaleX)
////    /// </summary>
////    private void ApplyFlip(int dir)
////    {
////        Vector3 s = transform.localScale;
////        s.x = _baseScaleX * dir;
////        transform.localScale = s;
////    }

////    private float CurrentLocalX()
////        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

////    // ─── Public API ───────────────────────────────────────────────────────────

////    /// <summary>
////    /// Pause (false) or resume (true) patrol.
////    ///
////    /// FIX 2: does NOT recalculate patrol bounds.
////    /// The soldier always patrols its original fixed range from InitPatrol().
////    /// </summary>
////    public void SetPatrolling(bool active)
////    {
////        if (_isDead) return;
////        if (active) StartWalking();
////        else StopWalking();
////    }

////    /// <summary>
////    /// Re-applies the flip from the current _direction.
////    /// Call from SoldierDragDrop after SetParent() as a safety net.
////    /// </summary>
////    public void RefreshFlip() => ApplyFlip(_direction);

////    /// <summary>
////    /// Changes destination X and recalculates bounds from current position.
////    /// Use this only if you intentionally want to move the patrol range.
////    /// </summary>
////    public void SetDestinationX(float newX)
////    {
////        destinationX = newX;
////        float cx = CurrentLocalX();
////        _leftBound = Mathf.Min(cx, destinationX);
////        _rightBound = Mathf.Max(cx, destinationX);
////    }

////    // ─── Editor Gizmos ────────────────────────────────────────────────────────
////#if UNITY_EDITOR
////    private void OnDrawGizmosSelected()
////    {
////        float spawnApprox = Application.isPlaying ? _leftBound : CurrentLocalX();

////        Vector3 leftWorld = transform.parent != null
////            ? transform.parent.TransformPoint(new Vector3(Mathf.Min(spawnApprox, destinationX), 0f, 0f))
////            : new Vector3(Mathf.Min(spawnApprox, destinationX), transform.position.y, 0f);

////        Vector3 rightWorld = transform.parent != null
////            ? transform.parent.TransformPoint(new Vector3(Mathf.Max(spawnApprox, destinationX), 0f, 0f))
////            : new Vector3(Mathf.Max(spawnApprox, destinationX), transform.position.y, 0f);

////        Gizmos.color = Color.cyan;
////        Gizmos.DrawLine(leftWorld, rightWorld);

////        Gizmos.color = Color.yellow;
////        Gizmos.DrawSphere(leftWorld, 6f);
////        UnityEditor.Handles.Label(leftWorld + Vector3.up * 12f, "Spawn");

////        Gizmos.color = Color.green;
////        Gizmos.DrawSphere(rightWorld, 6f);
////        UnityEditor.Handles.Label(rightWorld + Vector3.up * 12f, "Destination");
////    }
////#endif
////}

//using System.Collections;
//using UnityEngine;

///// <summary>
///// AREA FORGE - SoldierController
/////
///// ── Why flipping is done on a CHILD Visual object, not the root ───────────────
/////
/////   The root GameObject is reparented every time the soldier is dragged
/////   (SoldierDragDrop moves it to the root Canvas, then back to the spawn panel).
/////   Every SetParent(x, worldPositionStays: true) causes Unity to recompute
/////   localScale on the root so the world scale is preserved across parents
/////   that may have different Canvas Scaler factors.
/////
/////   This means root.localScale.x is overwritten on EVERY drag — so any flip
/////   value we write there gets destroyed the moment the player drags the soldier.
/////
/////   FIX: We never touch root.localScale. Instead we flip a dedicated child
/////   called "Visual" (drag it into the Visual Root field in the Inspector).
/////   The Visual child's localScale is relative to the root and is NEVER
/////   affected by SetParent on the root — it always keeps the value we set.
/////
///// ── Spawn point is fixed ──────────────────────────────────────────────────────
/////   Patrol bounds are calculated ONCE in InitPatrol() and never changed.
/////   SetPatrolling(true) just resumes walking — it does not move the home point.
///// </summary>
//[RequireComponent(typeof(SoldierStats))]
//[RequireComponent(typeof(SpriteLayerAnimator))]
//public class SoldierController : MonoBehaviour
//{
//    // ─── Inspector ────────────────────────────────────────────────────────────

//    [Header("Visual Flip Root")]
//    [Tooltip("Drag the 'Visual' child GameObject here.\n" +
//             "All Image layers (Body, Head, Hair, Armor, Helmet, Weapon) must be\n" +
//             "children of this object. Only this Transform is flipped — never the root.")]
//    [SerializeField] private Transform visualRoot;

//    [Header("Patrol")]
//    [Tooltip("Absolute anchored/local X where the soldier turns around.\n" +
//             "The other turn-around point is the soldier's spawn position.\n" +
//             "Example: spawn at X=0, Destination=300 → patrols 0 to 300.")]
//    [SerializeField] private float destinationX = 300f;

//    [Tooltip("Speed in canvas units per second. Try 80-150 for UI.")]
//    [SerializeField] private float moveSpeed = 80f;

//    [Header("Rest Behaviour")]
//    [SerializeField] private float restIntervalMin = 3f;
//    [SerializeField] private float restIntervalMax = 7f;
//    [SerializeField] private float restDurationMin = 1.5f;
//    [SerializeField] private float restDurationMax = 3.5f;

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private SoldierStats _stats;
//    private SpriteLayerAnimator _spriteAnim;
//    private RectTransform _rect;

//    // The base scale of the Visual child — captured once in Awake.
//    // Never recalculated. ApplyFlip always uses this magnitude.
//    private float _visualBaseScaleX;

//    // Patrol bounds — set ONCE in InitPatrol, never changed after that.
//    private float _leftBound;
//    private float _rightBound;

//    private int _direction = 1;
//    private bool _isDead = false;
//    private bool _isPatrolling = false;
//    private bool _isResting = false;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _stats = GetComponent<SoldierStats>();
//        _spriteAnim = GetComponent<SpriteLayerAnimator>();
//        _rect = GetComponent<RectTransform>();

//        if (visualRoot == null)
//        {
//            Debug.LogError($"[SoldierController] '{name}': Visual Root is not assigned! " +
//                           "Create a child GameObject called 'Visual', put all Image layers " +
//                           "inside it, then drag it into the Visual Root field.");
//        }
//        else
//        {
//            // Capture the Visual child's scale magnitude right now.
//            // The root may be reparented later (drag/drop) which changes root.localScale,
//            // but the Visual child's localScale is unaffected by root reparenting.
//            _visualBaseScaleX = Mathf.Abs(visualRoot.localScale.x);
//            if (_visualBaseScaleX < 0.001f) _visualBaseScaleX = 1f;
//        }

//        _stats.OnSoldierDied += HandleDeath;
//    }

//    private void OnDestroy()
//    {
//        if (_stats != null)
//            _stats.OnSoldierDied -= HandleDeath;
//    }

//    private void Start()
//    {
//        // Yield one frame so Canvas layout writes the final anchoredPosition.
//        // anchoredPosition is 0 in Start() for freshly spawned UI objects —
//        // the Canvas places them at the correct spot at end of the first frame.
//        StartCoroutine(InitPatrol());
//    }

//    private void Update()
//    {
//        if (_isDead || !_isPatrolling || _isResting) return;
//        MovePatrol();
//    }

//    // ─── Initialisation ───────────────────────────────────────────────────────

//    private IEnumerator InitPatrol()
//    {
//        yield return null;  // wait one frame for Canvas layout to settle

//        float spawnX = CurrentLocalX();

//        // Lock patrol bounds permanently from spawn position + destination.
//        _leftBound = Mathf.Min(spawnX, destinationX);
//        _rightBound = Mathf.Max(spawnX, destinationX);

//        // Face toward destinationX on spawn.
//        _direction = destinationX >= spawnX ? 1 : -1;
//        ApplyFlip(_direction);

//        StartWalking();
//        StartCoroutine(RestCycle());

//        Debug.Log($"[SoldierController] '{name}' patrol locked: " +
//                  $"{_leftBound:F0} to {_rightBound:F0}  (spawnX={spawnX:F0})");
//    }

//    // ─── Patrol ───────────────────────────────────────────────────────────────

//    private void StartWalking()
//    {
//        _isPatrolling = true;
//        _isResting = false;
//        _spriteAnim.SetState(AnimationState.Walk);
//    }

//    private void StopWalking()
//    {
//        _isPatrolling = false;
//        _spriteAnim.SetState(AnimationState.Idle);
//    }

//    private void MovePatrol()
//    {
//        float step = _direction * moveSpeed * Time.deltaTime;

//        if (_rect != null)
//        {
//            _rect.anchoredPosition += new Vector2(step, 0f);
//            float x = _rect.anchoredPosition.x;

//            if (_direction == 1 && x >= _rightBound)
//            {
//                var p = _rect.anchoredPosition;
//                p.x = _rightBound;
//                _rect.anchoredPosition = p;
//                SetDirection(-1);
//            }
//            else if (_direction == -1 && x <= _leftBound)
//            {
//                var p = _rect.anchoredPosition;
//                p.x = _leftBound;
//                _rect.anchoredPosition = p;
//                SetDirection(1);
//            }
//        }
//        else
//        {
//            transform.Translate(step, 0f, 0f, Space.Self);
//            float x = transform.localPosition.x;

//            if (_direction == 1 && x >= _rightBound)
//            {
//                var pos = transform.localPosition;
//                pos.x = _rightBound;
//                transform.localPosition = pos;
//                SetDirection(-1);
//            }
//            else if (_direction == -1 && x <= _leftBound)
//            {
//                var pos = transform.localPosition;
//                pos.x = _leftBound;
//                transform.localPosition = pos;
//                SetDirection(1);
//            }
//        }
//    }

//    // ─── Rest Cycle ───────────────────────────────────────────────────────────

//    private IEnumerator RestCycle()
//    {
//        while (!_isDead)
//        {
//            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
//            if (_isDead) yield break;

//            _isResting = true;
//            _spriteAnim.SetState(AnimationState.Idle);

//            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
//            if (_isDead) yield break;

//            _spriteAnim.SetState(AnimationState.Walk);
//            _isResting = false;
//        }
//    }

//    // ─── Death ────────────────────────────────────────────────────────────────

//    private void HandleDeath(SoldierStats _)
//    {
//        _isDead = true;
//        StopWalking();
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
//    /// Flips the Visual child's localScale.x — never the root.
//    ///
//    /// The root is reparented during drag/drop, which causes Unity to rewrite
//    /// root.localScale to preserve world scale. By flipping only the Visual
//    /// child, the flip value is completely isolated from drag reparenting.
//    ///
//    ///   dir =  1 → faces right (+_visualBaseScaleX)
//    ///   dir = -1 → faces left  (-_visualBaseScaleX, mirrored)
//    /// </summary>
//    private void ApplyFlip(int dir)
//    {
//        if (visualRoot == null) return;
//        Vector3 s = visualRoot.localScale;
//        s.x = _visualBaseScaleX * dir;
//        visualRoot.localScale = s;
//    }

//    private float CurrentLocalX()
//        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

//    // ─── Public API ───────────────────────────────────────────────────────────

//    /// <summary>
//    /// Pause (false) or resume (true) patrol.
//    /// Does NOT recalculate patrol bounds — the home point never changes.
//    /// </summary>
//    public void SetPatrolling(bool active)
//    {
//        if (_isDead) return;
//        if (active) StartWalking();
//        else StopWalking();
//    }

//    /// <summary>
//    /// Override destination X at runtime if you need to change the patrol range.
//    /// </summary>
//    public void SetDestinationX(float newX)
//    {
//        destinationX = newX;
//        float cx = CurrentLocalX();
//        _leftBound = Mathf.Min(cx, destinationX);
//        _rightBound = Mathf.Max(cx, destinationX);
//    }

//    // ─── Editor Gizmos ────────────────────────────────────────────────────────
//#if UNITY_EDITOR
//    private void OnDrawGizmosSelected()
//    {
//        float spawnApprox = Application.isPlaying ? _leftBound : CurrentLocalX();

//        Vector3 leftWorld = transform.parent != null
//            ? transform.parent.TransformPoint(
//                new Vector3(Mathf.Min(spawnApprox, destinationX), 0f, 0f))
//            : new Vector3(Mathf.Min(spawnApprox, destinationX), transform.position.y, 0f);

//        Vector3 rightWorld = transform.parent != null
//            ? transform.parent.TransformPoint(
//                new Vector3(Mathf.Max(spawnApprox, destinationX), 0f, 0f))
//            : new Vector3(Mathf.Max(spawnApprox, destinationX), transform.position.y, 0f);

//        Gizmos.color = Color.cyan;
//        Gizmos.DrawLine(leftWorld, rightWorld);

//        Gizmos.color = Color.yellow;
//        Gizmos.DrawSphere(leftWorld, 6f);
//        UnityEditor.Handles.Label(leftWorld + Vector3.up * 12f, "Spawn");

//        Gizmos.color = Color.green;
//        Gizmos.DrawSphere(rightWorld, 6f);
//        UnityEditor.Handles.Label(rightWorld + Vector3.up * 12f, "Destination");
//    }
//#endif
//}

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SoldierStats))]
[RequireComponent(typeof(SpriteLayerAnimator))]
public class SoldierController : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Visual Flip Root")]
    [Tooltip("Drag the 'VisualFlip' child GameObject here.\n" +
             "All Image layers (Body, Head, Hair, Armor, Helmet, Weapon) must live\n" +
             "inside it. Only this Transform is ever flipped — never the root.")]
    [SerializeField] private Transform visualRoot;

    [Header("Flip")]
    [Tooltip("Tick ON  if your sprite sheet faces LEFT  by default (most pixel art).\n" +
             "Tick OFF if your sprite sheet faces RIGHT by default.\n\n" +
             "If the soldier walks right but FACES LEFT (moonwalking), toggle this.")]
    [SerializeField] private bool spriteDefaultFacingLeft = true;

    [Header("Patrol")]
    [Tooltip("Absolute anchored/local X where the soldier turns around.\n" +
             "The other turn-around point is the soldier's spawn position.\n" +
             "Example: spawn at X=0, Destination=300 -> patrols 0 to 300.")]
    [SerializeField] private float destinationX = 300f;

    [Tooltip("Speed in canvas units per second. Try 80-150 for UI.")]
    [SerializeField] private float moveSpeed = 80f;

    [Header("Rest Behaviour")]
    [SerializeField] private float restIntervalMin = 3f;
    [SerializeField] private float restIntervalMax = 7f;
    [SerializeField] private float restDurationMin = 1.5f;
    [SerializeField] private float restDurationMax = 3.5f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;
    private SpriteLayerAnimator _spriteAnim;
    private RectTransform _rect;

    private float _visualBaseScaleX;

    private float _leftBound;
    private float _rightBound;

    private int _direction = 1;
    private bool _isDead = false;
    private bool _isPatrolling = false;
    private bool _isResting = false;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _stats = GetComponent<SoldierStats>();
        _spriteAnim = GetComponent<SpriteLayerAnimator>();
        _rect = GetComponent<RectTransform>();

        if (visualRoot == null)
        {
            Debug.LogError($"[SoldierController] '{name}': Visual Root is not assigned! " +
                           "Drag the 'VisualFlip' child into the Visual Root field.");
        }
        else
        {
            _visualBaseScaleX = Mathf.Abs(visualRoot.localScale.x);
            if (_visualBaseScaleX < 0.001f) _visualBaseScaleX = 1f;
        }

        _stats.OnSoldierDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnSoldierDied -= HandleDeath;
    }

    private void Start()
    {
        StartCoroutine(InitPatrol());
    }

    private void Update()
    {
        if (_isDead || !_isPatrolling || _isResting) return;
        MovePatrol();
    }

    // ─── Initialisation ───────────────────────────────────────────────────────

    private IEnumerator InitPatrol()
    {
        yield return null;

        float spawnX = CurrentLocalX();
        _leftBound = Mathf.Min(spawnX, destinationX);
        _rightBound = Mathf.Max(spawnX, destinationX);

        _direction = destinationX >= spawnX ? 1 : -1;
        ApplyFlip(_direction);

        StartWalking();
        StartCoroutine(RestCycle());

        Debug.Log($"[SoldierController] '{name}' patrol: {_leftBound:F0} to {_rightBound:F0}  spawnX={spawnX:F0}");
    }

    // ─── Patrol ───────────────────────────────────────────────────────────────

    private void StartWalking()
    {
        _isPatrolling = true;
        _isResting = false;
        _spriteAnim.SetState(AnimationState.Walk);
    }

    private void StopWalking()
    {
        _isPatrolling = false;
        _spriteAnim.SetState(AnimationState.Idle);
    }

    private void MovePatrol()
    {
        float step = _direction * moveSpeed * Time.deltaTime;

        if (_rect != null)
        {
            _rect.anchoredPosition += new Vector2(step, 0f);
            float x = _rect.anchoredPosition.x;

            if (_direction == 1 && x >= _rightBound)
            {
                var p = _rect.anchoredPosition;
                p.x = _rightBound;
                _rect.anchoredPosition = p;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var p = _rect.anchoredPosition;
                p.x = _leftBound;
                _rect.anchoredPosition = p;
                SetDirection(1);
            }
        }
        else
        {
            transform.Translate(step, 0f, 0f, Space.Self);
            float x = transform.localPosition.x;

            if (_direction == 1 && x >= _rightBound)
            {
                var pos = transform.localPosition;
                pos.x = _rightBound;
                transform.localPosition = pos;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var pos = transform.localPosition;
                pos.x = _leftBound;
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
            yield return new WaitForSeconds(Random.Range(restIntervalMin, restIntervalMax));
            if (_isDead) yield break;

            _isResting = true;
            _spriteAnim.SetState(AnimationState.Idle);

            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
            if (_isDead) yield break;

            _spriteAnim.SetState(AnimationState.Walk);
            _isResting = false;
        }
    }

    // ─── Death ────────────────────────────────────────────────────────────────

    private void HandleDeath(SoldierStats _)
    {
        _isDead = true;
        StopWalking();
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
    /// Flips the VisualFlip child to face the movement direction.
    ///
    /// spriteDefaultFacingLeft = true (most pixel art faces left by default):
    ///   positive scale.x = sprite faces LEFT
    ///   negative scale.x = sprite faces RIGHT (mirrored)
    ///   So: dir=1 (moving right) needs NEGATIVE scale
    ///       dir=-1 (moving left) needs POSITIVE scale
    ///
    /// spriteDefaultFacingLeft = false (sprite faces right by default):
    ///   dir=1 needs POSITIVE scale — no inversion needed.
    /// </summary>
    private void ApplyFlip(int dir)
    {
        if (visualRoot == null) return;

        // Invert sign when sprite naturally faces left,
        // so the soldier always faces the direction it moves.
        float sign = spriteDefaultFacingLeft ? -dir : dir;

        Vector3 s = visualRoot.localScale;
        s.x = _visualBaseScaleX * sign;
        visualRoot.localScale = s;
    }

    private float CurrentLocalX()
        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Pause (false) or resume (true) patrol.
    /// Does NOT recalculate bounds — patrol range is fixed from spawn.
    /// </summary>
    public void SetPatrolling(bool active)
    {
        if (_isDead) return;
        if (active) StartWalking();
        else StopWalking();
    }

    /// <summary>
    /// Re-applies the flip for the current direction on the VisualFlip child.
    /// Called by SoldierDragDrop after SetParent() as a safety net.
    /// Only touches visualRoot.localScale — never root.localScale.
    /// </summary>
    public void RefreshFlip() => ApplyFlip(_direction);

    /// <summary>
    /// Override destination X to change the patrol range at runtime.
    /// </summary>
    public void SetDestinationX(float newX)
    {
        destinationX = newX;
        float cx = CurrentLocalX();
        _leftBound = Mathf.Min(cx, destinationX);
        _rightBound = Mathf.Max(cx, destinationX);
    }

    // ─── Editor Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float spawnApprox = Application.isPlaying ? _leftBound : CurrentLocalX();

        Vector3 leftWorld = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(Mathf.Min(spawnApprox, destinationX), 0f, 0f))
            : new Vector3(Mathf.Min(spawnApprox, destinationX), transform.position.y, 0f);

        Vector3 rightWorld = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(Mathf.Max(spawnApprox, destinationX), 0f, 0f))
            : new Vector3(Mathf.Max(spawnApprox, destinationX), transform.position.y, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(leftWorld, rightWorld);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(leftWorld, 6f);
        UnityEditor.Handles.Label(leftWorld + Vector3.up * 12f, "Spawn");

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(rightWorld, 6f);
        UnityEditor.Handles.Label(rightWorld + Vector3.up * 12f, "Destination");
    }
#endif
}