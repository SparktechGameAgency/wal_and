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

    [Header("Enemy Combat")]
    [Tooltip("Radius (canvas units) in which this soldier detects enemies.\n" +
             "No collider or layer setup needed on the enemy — uses a direct scene scan.")]
    [SerializeField] private float detectionRadius = 200f;

    [Tooltip("Canvas units per second when chasing an enemy. Usually faster than patrol.")]
    [SerializeField] private float chaseSpeed = 130f;

    [Tooltip("Canvas units — how close the soldier must get before starting to attack.")]
    [SerializeField] private float attackRange = 30f;

    [Tooltip("Seconds between each attack hit.")]
    [SerializeField] private float attackInterval = 1f;

    [Tooltip("Damage dealt to the enemy per hit (uses SoldierStats.AttackDamage when > 0).")]
    [SerializeField] private float attackDamageOverride = 0f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;
    private SpriteLayerAnimator _spriteAnim;
    private RectTransform _rect;

    private float _visualBaseScaleX;

    private float _leftBound;
    private float _rightBound;
    private float _originalLeftBound;
    private float _originalRightBound;

    private int _direction = 1;
    private bool _isDead = false;
    private bool _isPatrolling = false;
    private bool _isResting = false;

    // Combat state
    private EnemyUnit _target;          // currently tracked enemy
    private bool _isInCombat = false;   // true while chasing or fighting

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

    private void OnEnable()
    {
        // Coroutines are killed when the GameObject (or any parent) is deactivated.
        // When the panel comes back, we need to restart whatever was running.
        if (_isDead) return;
        if (!Application.isPlaying) return;

        // _stats won't be assigned yet on the very first OnEnable (Awake hasn't run).
        // Guard by checking if init has happened (bounds will be 0 at that point).
        bool initialized = (_originalRightBound != 0f || _originalLeftBound != 0f);
        if (!initialized) return;

        StopAllCoroutines(); // clear any half-dead state

        if (_isInCombat && _target != null && !_target.IsDead)
        {
            // Resume chasing the enemy
            _spriteAnim.SetState(AnimationState.Run);
            StartCoroutine(CombatLoop());
            Debug.Log($"[SoldierController] '{name}' OnEnable: resuming combat.");
        }
        else if (_isInCombat)
        {
            // Target gone while panel was hidden — return to patrol
            ReturnToPatrol();
        }
        else if (_isPatrolling)
        {
            // Resume patrol + rest cycle
            _spriteAnim.SetState(AnimationState.Walk);
            StartCoroutine(RestCycle());
            Debug.Log($"[SoldierController] '{name}' OnEnable: resuming patrol.");
        }
    }

    private void Start()
    {
        StartCoroutine(InitPatrol());
    }

    private void Update()
    {
        if (_isDead) return;

        // ── Combat runs in CombatLoop coroutine — skip patrol while active ──
        if (_isInCombat) return;

        // ── Scan for enemies while patrolling / resting ────────────────────
        if (TryFindEnemy(out EnemyUnit found))
        {
            EngageEnemy(found);
            return;
        }

        // ── Normal patrol ─────────────────────────────────────────────────
        if (!_isPatrolling || _isResting) return;
        MovePatrol();
    }

    // ─── Initialisation ───────────────────────────────────────────────────────

    private IEnumerator InitPatrol()
    {
        yield return null;

        float spawnX = CurrentLocalX();
        _leftBound = Mathf.Min(spawnX, destinationX);
        _rightBound = Mathf.Max(spawnX, destinationX);
        _originalLeftBound = _leftBound;
        _originalRightBound = _rightBound;

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
                var p = _rect.anchoredPosition; p.x = _rightBound; _rect.anchoredPosition = p;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var p = _rect.anchoredPosition; p.x = _leftBound; _rect.anchoredPosition = p;
                SetDirection(1);
            }
        }
        else
        {
            transform.Translate(step, 0f, 0f, Space.Self);
            float x = transform.localPosition.x;

            if (_direction == 1 && x >= _rightBound)
            {
                var pos = transform.localPosition; pos.x = _rightBound; transform.localPosition = pos;
                SetDirection(-1);
            }
            else if (_direction == -1 && x <= _leftBound)
            {
                var pos = transform.localPosition; pos.x = _leftBound; transform.localPosition = pos;
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
            if (_isDead || _isInCombat) { yield return null; continue; }

            _isResting = true;
            _spriteAnim.SetState(AnimationState.Idle);

            yield return new WaitForSeconds(Random.Range(restDurationMin, restDurationMax));
            if (_isDead) yield break;

            if (!_isInCombat)
                _spriteAnim.SetState(AnimationState.Walk);
            _isResting = false;
        }
    }

    // ─── Enemy Detection ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds the nearest living EnemyUnit within detectionRadius.
    /// Does NOT require a Collider2D or any layer setup on the enemy —
    /// it scans all active EnemyUnit components in the scene directly.
    /// </summary>
    private bool TryFindEnemy(out EnemyUnit found)
    {
        found = null;
        float bestDist = detectionRadius;

        foreach (var enemy in EnemyUnit.All)
        {
            if (enemy == null || enemy.IsDead) continue;

            // Convert enemy world position to this soldier's parent local space for consistent X comparison
            float enemyLocalX = GetWorldToLocalX(enemy.transform.position);
            float myLocalX = _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;
            float dist = Mathf.Abs(enemyLocalX - myLocalX);
            if (dist < bestDist)
            {
                bestDist = dist;
                found = enemy;
            }
        }

        return found != null;
    }

    // ─── Combat ───────────────────────────────────────────────────────────────

    private void EngageEnemy(EnemyUnit enemy)
    {
        _target = enemy;
        _isInCombat = true;
        _isPatrolling = false;
        _isResting = false;

        // Subscribe so we know when the enemy dies
        _target.OnDied += OnTargetDied;

        StopAllCoroutines();           // pause rest cycle while in combat
        _spriteAnim.SetState(AnimationState.Run);
        StartCoroutine(CombatLoop());

        Debug.Log($"[SoldierController] '{name}' engaging enemy '{enemy.name}'.");
    }

    private IEnumerator CombatLoop()
    {
        bool _wasChasing = false;
        bool _wasFighting = false;

        // Always start in Run state when engaging
        if (_spriteAnim != null)
            _spriteAnim.SetState(AnimationState.Run);
        _wasChasing = true;

        while (!_isDead && _target != null && !_target.IsDead)
        {
            float dist = DistanceToTarget();

            if (dist > attackRange)
            {
                // ── Chase: run toward the enemy ──────────────────────────────────────
                if (!_wasChasing)
                {
                    if (_spriteAnim != null)
                        _spriteAnim.SetState(AnimationState.Run);
                    _wasChasing = true;
                    _wasFighting = false;
                }

                MoveTowardTarget();
                yield return null;
            }
            else
            {
                // ── Fight: face the enemy and attack ─────────────────────────────────
                if (!_wasFighting)
                {
                    FaceTarget();
                    if (_spriteAnim != null)
                        _spriteAnim.SetState(AnimationState.Fight);
                    _wasFighting = true;
                    _wasChasing = false;
                }

                float dmg = attackDamageOverride > 0f
                    ? attackDamageOverride
                    : _stats.AttackDamage;

                _target.TakeDamage(dmg);
                Debug.Log($"[SoldierController] '{{name}}' hit '{{_target.name}}' for {{dmg}} damage.");

                yield return new WaitForSeconds(attackInterval);
            }
        }
        // Enemy is dead or gone — return to patrol
        ReturnToPatrol();
    }

    private void MoveTowardTarget()
    {
        if (_target == null) return;

        float dx = GetTargetDeltaX();
        int dir = dx >= 0f ? 1 : -1;
        SetDirection(dir);

        float step = chaseSpeed * Time.deltaTime;

        if (_rect != null)
            _rect.anchoredPosition += new Vector2(dir * step, 0f);
        else
            transform.Translate(dir * step, 0f, 0f, Space.Self);
    }

    private void FaceTarget()
    {
        if (_target == null) return;
        SetDirection(GetTargetDeltaX() >= 0f ? 1 : -1);
    }

    private float DistanceToTarget()
    {
        if (_target == null) return float.MaxValue;
        return Mathf.Abs(GetTargetDeltaX());
    }

    private void OnTargetDied(EnemyUnit dead)
    {
        dead.OnDied -= OnTargetDied;
        _target = null;
        // CombatLoop will exit on the next iteration; ReturnToPatrol() is called there.
    }

    private void ReturnToPatrol()
    {
        if (_isDead) return;

        _isInCombat = false;
        _target = null;

        // Restore the original patrol bounds
        _leftBound = _originalLeftBound;
        _rightBound = _originalRightBound;

        // Clamp soldier back inside bounds in case it chased beyond them
        if (_rect != null)
        {
            Vector2 p = _rect.anchoredPosition;
            p.x = Mathf.Clamp(p.x, _leftBound, _rightBound);
            _rect.anchoredPosition = p;
        }
        else
        {
            Vector3 p = transform.localPosition;
            p.x = Mathf.Clamp(p.x, _leftBound, _rightBound);
            transform.localPosition = p;
        }

        // Point direction toward the opposite end so the soldier walks the full range
        float cx = CurrentLocalX();
        float distToLeft = cx - _leftBound;
        float distToRight = _rightBound - cx;
        SetDirection(distToLeft >= distToRight ? -1 : 1);

        Debug.Log($"[SoldierController] '{name}' returning to patrol. Bounds: {_leftBound:F0} to {_rightBound:F0}, pos: {cx:F0}, dir: {_direction}");

        StartWalking();
        StartCoroutine(RestCycle());
    }

    // ─── Death ────────────────────────────────────────────────────────────────

    private void HandleDeath(SoldierStats _)
    {
        _isDead = true;
        _isInCombat = false;

        if (_target != null)
        {
            _target.OnDied -= OnTargetDied;
            _target = null;
        }

        StopAllCoroutines();
        _spriteAnim.SetState(AnimationState.Death);
        Debug.Log($"[SoldierController] '{name}' died.");
    }

    // ─── Flip ─────────────────────────────────────────────────────────────────

    // ─── Canvas-space helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the X delta from soldier to target in the soldier's parent local space.
    /// Works correctly regardless of canvas scale or hierarchy depth.
    /// </summary>
    private float GetTargetDeltaX()
    {
        if (_target == null) return 0f;
        float myLocalX = _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;
        float targetLocalX = GetWorldToLocalX(_target.transform.position);
        return targetLocalX - myLocalX;
    }

    /// <summary>
    /// Converts a world position's X into this soldier's parent RectTransform local X,
    /// matching the coordinate space of anchoredPosition.
    /// </summary>
    private float GetWorldToLocalX(Vector3 worldPos)
    {
        if (_rect != null && _rect.parent != null)
        {
            // InverseTransformPoint converts world → local of the parent
            return _rect.parent.InverseTransformPoint(worldPos).x;
        }
        // Fallback: no parent, use world X directly
        return worldPos.x;
    }

    private void SetDirection(int dir)
    {
        _direction = dir;
        ApplyFlip(dir);
    }

    private void ApplyFlip(int dir)
    {
        if (visualRoot == null) return;
        float sign = spriteDefaultFacingLeft ? -dir : dir;
        Vector3 s = visualRoot.localScale;
        s.x = _visualBaseScaleX * sign;
        visualRoot.localScale = s;
    }

    private float CurrentLocalX()
        => _rect != null ? _rect.anchoredPosition.x : transform.localPosition.x;

    // ─── Public API ───────────────────────────────────────────────────────────

    public void SetPatrolling(bool active)
    {
        if (_isDead || _isInCombat) return;
        if (active) StartWalking();
        else StopWalking();
    }

    /// <summary>
    /// Fully restarts patrol AND the rest-cycle coroutine.
    /// Call this after SetActive(false/true) to revive killed coroutines.
    /// </summary>
    public void RestartPatrol()
    {
        if (_isDead) return;

        _isInCombat = false;
        _target = null;

        StopAllCoroutines();
        _isResting = false;
        StartWalking();
        StartCoroutine(RestCycle());

        Debug.Log($"[SoldierController] '{name}' patrol fully restarted.");
    }

    public void EnterRidingState()
    {
        if (_isDead) return;

        StopAllCoroutines();
        _isPatrolling = false;
        _isResting = false;
        _isInCombat = false;

        if (_target != null) { _target.OnDied -= OnTargetDied; _target = null; }

        ResetFlipForMount();
        _spriteAnim.SetState(AnimationState.RiderIdle);

        Debug.Log($"[SoldierController] '{name}' entered riding state.");
    }

    public void ExitRidingState()
    {
        if (_isDead) return;
        RefreshFlip();
        StartWalking();
        StartCoroutine(RestCycle());
        Debug.Log($"[SoldierController] '{name}' exited riding state.");
    }

    public void RefreshFlip() => ApplyFlip(_direction);

    /// <summary>
    /// Forces the soldier's visual facing directly to right (toward the bot
    /// side) or left (toward the player side), ignoring whatever direction
    /// the Village patrol AI last left it in. Call this when handing a
    /// carried-over soldier off to BattleUnit — the patrol AI's own flip
    /// lives on <see cref="visualRoot"/>, a CHILD transform, completely
    /// separate from BattleUnit's own root RectTransform.localScale flip.
    /// Without this, a soldier that was mid-patrol facing left in the
    /// Village keeps facing left in Battle even after BattleUnit flips the
    /// root, because the child's own flip still overrides it.
    /// Safe to call even while this component is disabled (enabled = false
    /// only stops Update/coroutines — public methods still run normally).
    /// </summary>
    public void SetBattleFacing(bool faceRight)
    {
        _direction = faceRight ? 1 : -1;
        ApplyFlip(_direction);
    }

    public void ResetFlipForMount()
    {
        if (visualRoot == null) return;
        Vector3 s = visualRoot.localScale;
        s.x = Mathf.Abs(_visualBaseScaleX);
        visualRoot.localScale = s;
        Debug.Log("[SoldierController] Flip reset for mount.");
    }

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
        // Detection radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol bounds
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