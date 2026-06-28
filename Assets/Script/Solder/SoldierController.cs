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
    /// Fully restarts patrol AND the rest-cycle coroutine.
    ///
    /// WHY THIS EXISTS: any code path that does gameObject.SetActive(false)
    /// on this soldier (e.g. SoldierDragDrop.StationOnArcherSlot) silently
    /// kills every running coroutine on this component — including the
    /// RestCycle() coroutine started back in Start()/InitPatrol(). Calling
    /// SetPatrolling(true) alone only flips the _isPatrolling flag; it does
    /// NOT relaunch RestCycle(), so the soldier is left with a permanently
    /// dead rest-cycle after being recalled from an archer slot (and would
    /// have the same problem after a horse/cannon slot, if those paths ever
    /// disable the GameObject too).
    ///
    /// This mirrors the exact fix already applied to HorseController/
    /// HorseWalkZone for the "patrolling horses freeze after SetActive(false)"
    /// bug — restart every coroutine explicitly on re-enable rather than
    /// assuming it survived.
    /// </summary>
    public void RestartPatrol()
    {
        if (_isDead) return;

        StopAllCoroutines();   // clear out any stale/duplicate coroutine state
        _isResting = false;
        StartWalking();
        StartCoroutine(RestCycle());

        Debug.Log($"[SoldierController] '{name}' patrol fully restarted (coroutines relaunched).");
    }

    public void EnterRidingState()
    {
        if (_isDead) return;

        // Stop patrol and rest cycle
        StopAllCoroutines();
        _isPatrolling = false;
        _isResting = false;

        // Neutralise the soldier's own flip — the dragon's localScale flip
        // propagates down the hierarchy, so the soldier automatically faces
        // whichever direction the dragon is flying.
        ResetFlipForMount();

        // Play the sitting-on-dragon animation on every equipped layer.
        // SpriteLayerAnimator.AdvanceAllLayers() calls item.GetSprites(Riding, bodyType)
        // for each slot, so the correct sprites are shown regardless of which
        // of the 6 armors is currently equipped.
        _spriteAnim.SetState(AnimationState.RiderIdle);

        Debug.Log($"[SoldierController] '{name}' entered riding state.");
    }

    public void ExitRidingState()
    {
        if (_isDead) return;

        // Re-apply the patrol direction flip now that we're back on the ground.
        RefreshFlip();

        // Resume walking — this also calls _spriteAnim.SetState(Walk).
        StartWalking();

        // Restart the random rest cycle that EnterRidingState stopped.
        StartCoroutine(RestCycle());

        Debug.Log($"[SoldierController] '{name}' exited riding state.");
    }

    /// <summary>
    /// Re-applies the flip for the current direction on the VisualFlip child.
    /// Called by SoldierDragDrop after SetParent() as a safety net.
    /// Only touches visualRoot.localScale — never root.localScale.
    /// </summary>
    public void RefreshFlip() => ApplyFlip(_direction);

    /// <summary>
    /// Resets the visualRoot flip to its natural (un-mirrored) state.
    /// Called when mounting a dragon so the dragon's own localScale flip
    /// drives the facing direction through the hierarchy — if the soldier's
    /// visualRoot kept its own flip, the two would cancel each other out.
    /// </summary>
    public void ResetFlipForMount()
    {
        if (visualRoot == null) return;
        Vector3 s = visualRoot.localScale;
        // Restore to the base absolute scale — no sign applied.
        // Dragon's parent flip in the hierarchy handles direction from here.
        s.x = Mathf.Abs(_visualBaseScaleX);
        visualRoot.localScale = s;
        Debug.Log("[SoldierController] Flip reset for dragon mount.");
    }

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