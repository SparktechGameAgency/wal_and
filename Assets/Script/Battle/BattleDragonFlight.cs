using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleDragonFlight
///
/// Drives the Dragon unit's MOVEMENT in the Battle scene. BattleUnit still
/// owns health, targeting (FindNearestEnemy), the attack-range check, and
/// damage ticking — exactly like every other unit type. This component only
/// decides WHERE the dragon's RectTransform sits each frame, because a
/// dragon doesn't walk in a straight line along the ground like a
/// Soldier/Archer/Horse does.
///
/// Sequence (matches the requested behaviour):
///   1. RISE    — the moment the dragon spawns, it climbs straight up to a
///                cruising height above its spawn point ("flies to the top").
///   2. APPROACH — once a live enemy exists (BattleUnit.CurrentTarget), the
///                 dragon flies toward a hover point just above/beside it.
///   3. ENGAGE  — once close enough to that hover point, the dragon holds
///                position and breathes fire (DragonFireBreath) at the
///                target. BattleUnit's own attack-range/attack-rate check
///                is what actually ticks damage into the target — this
///                component only has to get the dragon close enough for
///                that check to start passing, and plays the matching VFX.
///
/// Requires: BattleUnit on the same GameObject (set canMove = false for
/// Dragon in BattleUnit.Init — see comment there).
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BattleUnit))]
public class BattleDragonFlight : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Sprite Orientation")]
    [Tooltip("Tick if the dragon sprite naturally faces LEFT at localScale.x = +1, " +
             "same meaning as DragonController's equivalent field.")]
    [SerializeField] private bool spriteDefaultFacesLeft = true;

    [Header("Rise (spawn → cruise altitude)")]
    [Tooltip("How much higher than its spawn Y the dragon climbs before " +
             "heading for the enemy.")]
    [SerializeField] private float riseHeight = 220f;

    [Tooltip("Canvas units per second while climbing.")]
    [SerializeField] private float riseSpeed = 220f;

    [Header("Approach / Chase")]
    [Tooltip("Canvas units per second while flying toward the hover point over the enemy.")]
    [SerializeField] private float chaseSpeed = 260f;

    [Tooltip("Where the dragon hovers relative to its target while breathing fire. " +
             "X: distance to keep back from the enemy (sign auto-flipped to whichever " +
             "side the dragon is approaching from). Y: height above the enemy to hover at.")]
    [SerializeField] private Vector2 hoverOffset = new Vector2(70f, 120f);

    [Tooltip("How close (canvas units) to the hover point counts as \"arrived\" — " +
             "close enough to stop and start breathing fire.")]
    [SerializeField] private float arrivalThreshold = 10f;

    [Header("Combat — Fire Breath")]
    [Tooltip("ParticleSystem-equivalent on a child GameObject, positioned at the " +
             "dragon's mouth. Auto-found on children if left blank.")]
    [SerializeField] private DragonFireBreath fireBreath;

    // ── Components ───────────────────────────────────────────────────────────

    private RectTransform _rt;
    private BattleUnit _battleUnit;
    private Canvas _rootCanvas;
    private RectTransform _canvasRt;

    private DragonWingAnimator _wingAnimator;
    private DragonBodyAnimator _bodyAnimator;

    // ── State ────────────────────────────────────────────────────────────────

    private float _cruiseY;
    private bool _hasReachedCruise;
    private bool _isBreathingFire;

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _battleUnit = GetComponent<BattleUnit>();
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null) _canvasRt = _rootCanvas.GetComponent<RectTransform>();

        _wingAnimator = GetComponentInChildren<DragonWingAnimator>(includeInactive: true);
        _bodyAnimator = GetComponentInChildren<DragonBodyAnimator>(includeInactive: true);

        if (fireBreath == null)
            fireBreath = GetComponentInChildren<DragonFireBreath>(includeInactive: true);

        if (fireBreath == null)
            Debug.LogWarning("[BattleDragonFlight] No DragonFireBreath found in children — " +
                              "no fire VFX will play in battle.", this);
    }

    private void OnEnable()
    {
        // This component can live on the SAME dragon prefab asset that's used
        // in the Village (DragonEggSlot / FlyZone), since Battle re-instantiates
        // that prefab as-is. BattleManager.Instance only exists in the Battle
        // scene — if it's null, this copy of the dragon is sitting in the
        // Village panel (patrolling/idle via DragonController), so this
        // script must do nothing there and let DragonController keep full
        // control, instead of climbing/chasing on top of it.
        if (BattleManager.Instance == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (BattleManager.Instance == null) return; // Village copy — see OnEnable().

        // Spawn point becomes the base of the climb — works the same whether
        // the dragon landed in the flat army row or a seated castle slot.
        _cruiseY = _rt.anchoredPosition.y + riseHeight;
        _hasReachedCruise = false;

        // "Appear, then fly to the top" — start the fly animation immediately
        // on spawn instead of waiting for the first Approach/Engage tick.
        _wingAnimator?.SetState(DragonWingAnimator.WingState.Fly);
        _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly);
    }

    private void Update()
    {
        if (_battleUnit == null || _battleUnit.IsDead)
        {
            StopFire();
            return;
        }

        if (!_hasReachedCruise)
        {
            RiseToCruise();
            return;
        }

        BattleUnit target = _battleUnit.CurrentTarget;

        if (target == null || target.IsDead)
        {
            // Nothing to fight yet/anymore — hold at cruise altitude instead
            // of drifting, and make sure fire isn't left burning on a dead
            // or vanished target.
            StopFire();
            return;
        }

        Vector2 hoverPoint = GetHoverPoint(target);
        float distToHover = Vector2.Distance(_rt.anchoredPosition, hoverPoint);

        if (distToHover > arrivalThreshold)
        {
            // Still closing the distance — flying, not attacking yet.
            StopFire();
            FlyToward(hoverPoint);
        }
        else
        {
            // Arrived — hover in place and breathe fire. BattleUnit's own
            // Update() is doing the actual damage tick once its attackRange
            // check passes (hoverOffset.x should be tuned close to the
            // dragon prefab's BattleUnit.attackRange so the two line up).
            //
            // Facing while hovering uses the SAME fixed side convention as
            // GetHoverPoint (player units approach from the left, bot units
            // from the right) instead of recomputing from live positions.
            // The target BattleUnit keeps walking toward ITS OWN nearest
            // enemy while the dragon breathes fire on it, so a live
            // target.x - dragon.x comparison can drift to ~0 or flip sign as
            // the target passes underneath/beside the dragon — each flip
            // re-mirrors the sprite via FaceDirection(), which is the exact
            // "continuously rotating" glitch described above, just triggered
            // from the hover/fire branch instead of the approach branch.
            FaceDirection(_battleUnit.isPlayerUnit ? 1f : -1f);
            StartFire(target);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RISE
    // ══════════════════════════════════════════════════════════════════════════

    private void RiseToCruise()
    {
        Vector2 pos = _rt.anchoredPosition;
        float newY = Mathf.MoveTowards(pos.y, _cruiseY, riseSpeed * Time.deltaTime);
        _rt.anchoredPosition = new Vector2(pos.x, newY);

        if (Mathf.Approximately(newY, _cruiseY))
            _hasReachedCruise = true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // APPROACH
    // ══════════════════════════════════════════════════════════════════════════

    private void FlyToward(Vector2 destination)
    {
        Vector2 pos = _rt.anchoredPosition;
        Vector2 newPos = Vector2.MoveTowards(pos, destination, chaseSpeed * Time.deltaTime);
        _rt.anchoredPosition = newPos;

        FaceDirection(destination.x - pos.x);
    }

    /// <summary>
    /// Converts the target's world position into a hover point in THIS
    /// dragon's own anchoredPosition space (its parent's local space), offset
    /// so the dragon stops beside/above the enemy instead of on top of it.
    ///
    /// The side to hover on is fixed by which battlefield side this dragon
    /// belongs to (player = approaches from the left, bot = from the right),
    /// NOT by comparing live positions every frame. Comparing live positions
    /// flips sign back and forth once the dragon gets close to the target's
    /// X — each flip re-mirrors the sprite via FaceDirection(), which is what
    /// looked like the dragon "continuously rotating" instead of holding
    /// still and breathing fire.
    /// </summary>
    private Vector2 GetHoverPoint(BattleUnit target)
    {
        Vector2 targetLocalPos = WorldToLocalAnchoredPos(target.transform.position);

        // Player units approach from the left → hover just short of the
        // target on its left (negative offset). Bot units approach from the
        // right → hover just short of it on its right (positive offset).
        float facingSign = _battleUnit.isPlayerUnit ? -1f : 1f;
        return targetLocalPos + new Vector2(hoverOffset.x * facingSign, hoverOffset.y);
    }

    private Vector2 WorldToLocalAnchoredPos(Vector3 worldPos)
    {
        if (_canvasRt == null || _rt.parent == null) return _rt.anchoredPosition;

        Camera cam = (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _rootCanvas.worldCamera
            : null;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, screenPos, cam, out Vector2 local);
        return local;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ENGAGE — fire breath
    // ══════════════════════════════════════════════════════════════════════════

    private void StartFire(BattleUnit target)
    {
        if (_isBreathingFire)
        {
            // Already breathing — just keep the aim locked onto a moving target.
            fireBreath?.SetTarget(target.transform);
            return;
        }

        _isBreathingFire = true;
        if (fireBreath != null)
        {
            fireBreath.SetTarget(target.transform);
            fireBreath.Play();
        }
    }

    private void StopFire()
    {
        if (!_isBreathingFire) return;

        _isBreathingFire = false;
        if (fireBreath != null)
        {
            fireBreath.Stop();
            fireBreath.ClearTarget();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FACING
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets absolute facing from a movement-direction sign, same convention
    /// as DragonController.FaceDirection — idempotent, safe to call every frame.
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
}