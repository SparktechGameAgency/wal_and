using System.Collections;
using UnityEngine;

/// <summary>
/// HorseCombat — Attach to the same GameObject as HorseController.
///
/// BEHAVIOUR
/// ─────────
/// • Every <see cref="detectionInterval"/> seconds the script scans
///   <see cref="EnemyUnit.All"/> for the nearest living enemy within
///   <see cref="detectionRadius"/> (canvas / world units).
///
/// • When an enemy is found AND the horse has a mounted soldier
///   (<see cref="HorseController.IsOccupied"/> == true):
///     1. HorseWalkZone patrol is SUSPENDED (<see cref="IHorseCombatOwner"/>
///        is notified so the zone can pause its coroutine, or you can wire
///        it yourself via the Inspector event).
///     2. The horse plays the RUN animation and moves toward the enemy
///        each frame until it is within <see cref="attackRange"/>.
///     3. On reaching attack range the horse switches to FIGHT animation
///        and the mounted soldier deals <see cref="damagePerHit"/> to the
///        enemy every <see cref="attackInterval"/> seconds.
///     4. When the enemy dies the horse returns to IDLE, the zone patrol
///        resumes, and the system looks for a new target.
///
/// SETUP
/// ─────
/// 1. Add this component to the horse prefab (same object as HorseController).
/// 2. Assign <c>horseAnimSO</c> on HorseController with Fight + Run clips.
/// 3. The Fight clip sprites are read from the SO automatically — no extra
///    wiring needed here.
/// 4. If the horse lives in a HorseWalkZone, assign the zone in the Inspector
///    (<see cref="ownerZone"/>) so patrol is properly paused/resumed.
///
/// ENEMY LAYER / CANVAS SPACE
/// ──────────────────────────
/// EnemyUnit.All uses world-space Transform positions, so
/// <see cref="GetEnemyWorldPos"/> converts them via
/// <c>Camera.main.WorldToScreenPoint → RectTransformUtility.ScreenPointToLocalPointInRectangle</c>
/// when the horse is in Screen-Space Overlay canvas.  Set
/// <see cref="useWorldSpace"/> = true if your canvas is World Space.
/// </summary>
[RequireComponent(typeof(HorseController))]
public class HorseCombat : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Detection")]
    [Tooltip("How often (seconds) to scan for nearby enemies. Lower = more responsive but more CPU.")]
    [SerializeField] private float detectionInterval = 0.25f;

    [Tooltip("Maximum distance (canvas units) to detect an enemy.")]
    [SerializeField] private float detectionRadius = 400f;

    [Header("Combat")]
    [Tooltip("Distance (canvas units) at which the horse stops moving and starts attacking.")]
    [SerializeField] private float attackRange = 60f;

    [Tooltip("Damage dealt to the enemy per hit.")]
    [SerializeField] private float damagePerHit = 20f;

    [Tooltip("Seconds between each attack hit.")]
    [SerializeField] private float attackInterval = 0.8f;

    [Header("Movement to Enemy")]
    [Tooltip("Speed (canvas units/sec) at which the horse charges toward an enemy.")]
    [SerializeField] private float chargeSpeed = 120f;

    [Header("Canvas (Screen-Space Overlay only)")]
    [Tooltip("Set TRUE if your Canvas is in World Space. " +
             "FALSE (default) converts enemy world positions to canvas local positions.")]
    [SerializeField] private bool useWorldSpace = false;

    // ownerZone and rootCanvas are found automatically at runtime — no Inspector drag needed.
    private HorseWalkZone ownerZone;
    private Canvas rootCanvas;

    // ── Internal state ────────────────────────────────────────────────────────

    private HorseController _hc;
    private RectTransform _rt;

    private EnemyUnit _target;
    private bool _inCombat;
    private Coroutine _detectionLoop;
    private Coroutine _attackLoop;
    private Coroutine _chargeRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _hc = GetComponent<HorseController>();
        _rt = GetComponent<RectTransform>();

        if (_rt == null)
            Debug.LogWarning($"[HorseCombat] '{name}': no RectTransform found. " +
                             "Distance checks will use world-space positions.", this);

        // Auto-find the walk zone this horse lives in (assigned by HorseWalkZone.SpawnWalkingHorse)
        ownerZone = GetComponentInParent<HorseWalkZone>();

        // Auto-find the root Canvas for screen-space position conversion
        if (!useWorldSpace)
        {
            Canvas[] canvases = GetComponentsInParent<Canvas>();
            foreach (var c in canvases)
            {
                if (c.isRootCanvas) { rootCanvas = c; break; }
            }
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnEnable()
    {
        _detectionLoop = StartCoroutine(DetectionLoop());
    }

    private void OnDisable()
    {
        StopAllCombatCoroutines();
        _inCombat = false;
        _target = null;
    }

    // ── Detection loop ────────────────────────────────────────────────────────

    private IEnumerator DetectionLoop()
    {
        var wait = new WaitForSeconds(detectionInterval);

        while (true)
        {
            yield return wait;

            // Only engage if a soldier is mounted
            if (!_hc.IsOccupied)
            {
                if (_inCombat) ExitCombat();
                continue;
            }

            // If already fighting, keep tracking the same target unless it died
            if (_inCombat)
            {
                if (_target == null || _target.IsDead)
                    ExitCombat();
                continue;
            }

            // Scan for the nearest enemy
            EnemyUnit nearest = FindNearestEnemy();
            if (nearest != null)
                EnterCombat(nearest);
        }
    }

    // ── Combat enter / exit ───────────────────────────────────────────────────

    private void EnterCombat(EnemyUnit enemy)
    {
        _target = enemy;
        _inCombat = true;

        // Subscribe to death so we can react immediately (not just on next scan)
        _target.OnDied += OnTargetDied;

        // Pause the walk-zone patrol so it doesn't fight HorseController.SetRun
        SuspendZonePatrol();

        // Start charging toward the enemy, then attacking
        _chargeRoutine = StartCoroutine(ChargeAndAttack());

        Debug.Log($"[HorseCombat] '{name}' → COMBAT with '{enemy.name}'.");
    }

    private void ExitCombat()
    {
        StopAllCombatCoroutines();

        if (_target != null)
        {
            _target.OnDied -= OnTargetDied;
            _target = null;
        }

        _inCombat = false;

        // Return to idle, then let the zone resume its own patrol timing
        _hc.SetIdle();
        ResumeZonePatrol();

        Debug.Log($"[HorseCombat] '{name}' → combat ended, resuming patrol.");
    }

    private void OnTargetDied(EnemyUnit dead)
    {
        // Fired directly by EnemyUnit.Die() — respond immediately
        ExitCombat();
    }

    // ── Charge + Attack coroutine ─────────────────────────────────────────────

    private IEnumerator ChargeAndAttack()
    {
        // Play RUN animation while moving toward enemy
        _hc.SetRun();

        // ── Phase 1: Charge toward enemy ──────────────────────────────────────
        while (_target != null && !_target.IsDead)
        {
            float dist = GetDistanceToTarget();

            if (dist <= attackRange)
                break; // close enough — start fighting

            // Move this horse's RectTransform toward the enemy
            MoveTowardTarget();

            // Flip sprite to face the enemy
            FaceTarget();

            yield return null;
        }

        if (_target == null || _target.IsDead)
        {
            ExitCombat();
            yield break;
        }

        // ── Phase 2: Attack loop ──────────────────────────────────────────────
        _hc.SetFight();
        _attackLoop = StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        var wait = new WaitForSeconds(attackInterval);

        while (_target != null && !_target.IsDead)
        {
            // Stay facing the enemy and keep FIGHT animation playing
            FaceTarget();

            // Deal damage
            _target.TakeDamage(damagePerHit);

            Debug.Log($"[HorseCombat] '{name}' hit '{_target?.name}' for {damagePerHit} dmg.");

            yield return wait;
        }

        // Target died during the attack loop — ExitCombat handles cleanup
        // (OnTargetDied fires from EnemyUnit.Die, so ExitCombat may have
        //  already been called; the null-check above is the safety net)
        if (_inCombat)
            ExitCombat();
    }

    // ── Movement & facing ─────────────────────────────────────────────────────

    private void MoveTowardTarget()
    {
        if (_rt == null || _target == null) return;

        Vector2 targetPos = GetTargetLocalPosition();
        Vector2 currentPos = _rt.anchoredPosition;

        // Only chase on the X axis — horses are ground units and must not
        // float up/down toward an enemy's Y position.
        float dx = targetPos.x - currentPos.x;
        float moveX = Mathf.MoveTowards(0f, dx, chargeSpeed * Time.deltaTime);

        _rt.anchoredPosition = new Vector2(currentPos.x + moveX, currentPos.y);
    }

    private void FaceTarget()
    {
        if (_rt == null || _target == null) return;

        Vector2 targetPos = GetTargetLocalPosition();
        float dx = targetPos.x - _rt.anchoredPosition.x;

        Vector3 scale = _rt.localScale;
        float absX = Mathf.Abs(scale.x);

        // Positive scale.x = facing right; negative = facing left
        if (dx > 0f && scale.x < 0f)
            scale.x = absX;
        else if (dx < 0f && scale.x > 0f)
            scale.x = -absX;

        _rt.localScale = scale;
    }

    // ── Enemy detection helpers ───────────────────────────────────────────────

    private EnemyUnit FindNearestEnemy()
    {
        EnemyUnit nearest = null;
        float best = detectionRadius * detectionRadius; // squared for perf

        foreach (EnemyUnit enemy in EnemyUnit.All)
        {
            if (enemy == null || enemy.IsDead) continue;

            float sqDist = GetSquaredDistanceTo(enemy);
            if (sqDist < best)
            {
                best = sqDist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private float GetDistanceToTarget()
    {
        if (_target == null || _rt == null) return float.MaxValue;

        // Distance is X-only to match our X-only movement toward the enemy.
        Vector2 targetPos = GetTargetLocalPosition();
        return Mathf.Abs(targetPos.x - _rt.anchoredPosition.x);
    }

    private float GetSquaredDistanceTo(EnemyUnit enemy)
    {
        if (_rt == null)
        {
            // World-space fallback
            return (enemy.transform.position - transform.position).sqrMagnitude;
        }

        Vector2 myPos = _rt.anchoredPosition;
        Vector2 enemyPos = GetTargetLocalPosition(enemy);
        return (enemyPos - myPos).sqrMagnitude;
    }

    // ── Canvas-space conversion ───────────────────────────────────────────────

    /// <summary>Returns the current target's position in this RectTransform's parent space.</summary>
    private Vector2 GetTargetLocalPosition() => GetTargetLocalPosition(_target);

    private Vector2 GetTargetLocalPosition(EnemyUnit enemy)
    {
        if (enemy == null) return Vector2.zero;

        if (useWorldSpace || rootCanvas == null || _rt == null)
            return (Vector2)enemy.transform.position;

        // Screen-Space Overlay: convert world position → screen → canvas local
        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(enemy.transform.position)
            : (Vector3)(Vector2)enemy.transform.position;

        RectTransform parentRT = _rt.parent as RectTransform;
        if (parentRT == null) return (Vector2)enemy.transform.position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT,
            screenPos,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out Vector2 localPoint);

        return localPoint;
    }

    // ── Zone patrol control ───────────────────────────────────────────────────

    private void SuspendZonePatrol()
    {
        if (ownerZone == null) return;

        // Tell the zone this specific horse is now in combat.
        // HorseWalkZone manages its coroutines per-horse internally;
        // we stop the cycle for our entry by calling the public notify path.
        // The simplest approach: disable ExternallyControlled override so
        // HorseController won't fight our Fight/Run calls, and disable the
        // zone's movement ticking for this horse via the owner component.
        var owner = GetComponent<WalkZoneOwner>();
        if (owner != null)
            ownerZone.NotifyHorseLeft(owner); // removes from zone list → stops its coroutine

        // Keep ExternallyControlled = true so HorseController Path B
        // doesn't auto-revert states while we're in charge
        _hc.ExternallyControlled = true;
    }

    private void ResumeZonePatrol()
    {
        if (ownerZone == null) return;

        // Find our inventory index from the drag handler (best-effort - only
        // used for HorseWalkZone bookkeeping; ReRegisterHorse works without it).
        var drag = GetComponent<HorseDragHandler>();
        int idx = drag != null ? drag.inventoryIndex : -1;

        // Re-add this horse to the zone so its WalkCycleRoutine coroutine
        // actually restarts. SuspendZonePatrol's NotifyHorseLeft() removed us
        // from the zone's list, which killed that coroutine - without this
        // call the horse was left with nothing ever driving its Idle/Run
        // timer again, so it sat in Idle forever after its first fight,
        // looking "stuck" even though it keeps killing enemies via
        // DetectionLoop. ReRegisterHorse() sets ExternallyControlled = true
        // itself and starts a fresh WalkCycleRoutine (beginning with its
        // Idle phase), so we don't set state here ourselves.
        ownerZone.ReRegisterHorse(_hc, idx);

        Debug.Log($"[HorseCombat] '{name}': patrol resumed via ReRegisterHorse (idx={idx}).");
    }

    private void StopAllCombatCoroutines()
    {
        if (_chargeRoutine != null) { StopCoroutine(_chargeRoutine); _chargeRoutine = null; }
        if (_attackLoop != null) { StopCoroutine(_attackLoop); _attackLoop = null; }
    }

    // ── Gizmos (editor visualisation) ────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Detection radius — yellow sphere
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attack range — red sphere
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}