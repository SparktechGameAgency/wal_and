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

    [Header("Screen Bounds")]
    [Tooltip("How far inside the Battlefield's edges (BattleManager." +
             "BattlefieldBounds) the dragon must stay, in canvas-space units. " +
             "Without a ceiling, a dragon retargeting to a Cannon/Archer " +
             "seated high up on the castle's TopSlots staircase flies to " +
             "(target Y + hoverOffset.y) with no limit — which can land " +
             "outside the battlefield, looking like the dragon flew away. " +
             "Falls back to the full canvas rect if BattlefieldBounds isn't " +
             "assigned. Cruise altitude and every hover point (both X and Y) " +
             "are clamped to this margin inside the battlefield.")]
    [SerializeField] private float topMargin = 40f;

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

    // Highest Y this dragon is allowed to fly to, in ITS OWN parent's local
    // anchoredPosition space — computed once from the canvas's actual visible
    // top edge (see ComputeMaxAltitudeY). float.MaxValue (i.e. no ceiling)
    // until Start() computes a real value, so nothing clamps before that.
    private float _maxAltitudeY = float.MaxValue;

    // Left/right canvas edges in the dragon's parent's local anchoredPosition
    // space, with the same topMargin used as a side margin. Without these,
    // GetHoverPoint's X (targetLocalPos.x + hoverOffset.x * facingSign) has
    // no limit — a target near the left/right edge of the battlefield pushes
    // the hover point past the visible canvas and the dragon flies off the
    // side of the screen, same class of bug as the altitude ceiling above.
    // float.MinValue/MaxValue (no clamp) until Start() computes real values.
    private float _minX = float.MinValue;
    private float _maxX = float.MaxValue;

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

        // A dragon already dropped into a FlyZone before Start Battle was
        // pressed is carried into the Battle scene as the SAME live
        // GameObject (BattleManager.ReceivePlayerDragons re-enables this
        // component here). Its Awake() only ever ran once, back in the
        // Village — _rootCanvas/_canvasRt were cached pointing at the
        // VILLAGE Canvas, which gets destroyed when that scene unloads.
        // WorldToLocalAnchoredPos() silently falls back to "return the
        // dragon's own current position" whenever _canvasRt is a destroyed
        // reference, which collapses every bounds calculation (altitude,
        // left/right edges, hover point) down to wherever the dragon
        // already is — exactly the "stuck in place" symptom. Re-resolving
        // here, every time this component actually proceeds into battle
        // mode, fixes both the carried-over case and is a harmless no-op
        // for a freshly-spawned dragon whose canvas was already correct.
        _rootCanvas = GetComponentInParent<Canvas>();
        _canvasRt = _rootCanvas != null ? _rootCanvas.GetComponent<RectTransform>() : null;
    }

    private void Start()
    {
        if (BattleManager.Instance == null) return; // Village copy — see OnEnable().
        ActivateForBattle();
    }

    /// <summary>
    /// Runs the same initialization Start() used to do inline, but as a
    /// public, idempotent method BattleManager can call directly and
    /// explicitly — instead of depending on Unity's "Start() only ever runs
    /// once per component instance" lifecycle rule lining up correctly for
    /// a dragon that gets disabled in the Village (OnEnable bails out before
    /// Start ever fires there) and later re-enabled here in the Battle
    /// scene. That reliance is fragile: if Start() somehow already ran once
    /// (edge cases around enable/disable/SetActive cycling), re-enabling the
    /// component a second time would silently skip re-computing bounds/
    /// cruise altitude entirely, leaving the dragon permanently stuck at
    /// whatever cruise/hover state it last had — which reads exactly like
    /// "the dragon just floats there and never breathes fire". Calling this
    /// explicitly after flight.enabled = true removes that guesswork.
    /// Safe to call multiple times — every field it touches is fully
    /// recomputed from scratch each call, no partial/leftover state.
    /// </summary>
    public void ActivateForBattle()
    {
        // Canvas refs can be stale (see OnEnable's comment) if this dragon
        // is a carried-over live GameObject — re-resolve every time this is
        // called, not just once, since ActivateForBattle can now run from
        // more than one caller.
        _rootCanvas = GetComponentInParent<Canvas>();
        _canvasRt = _rootCanvas != null ? _rootCanvas.GetComponent<RectTransform>() : null;

        // Compute the real ceiling from the canvas's own visible top edge —
        // this adapts automatically to whatever Canvas Scaler resolution the
        // game is actually running at, instead of a guessed flat number.
        _maxAltitudeY = ComputeMaxAltitudeY();
        ComputeHorizontalBounds(out _minX, out _maxX);

        Debug.Log($"[BattleDragonFlight] '{name}' bounds check — " +
                  $"maxAltitudeY={_maxAltitudeY:F1} minX={_minX:F1} maxX={_maxX:F1} " +
                  $"spawnPos={_rt.anchoredPosition} topMargin={topMargin}");

        // Spawn point becomes the base of the climb — works the same whether
        // the dragon landed in the flat army row or a seated castle slot.
        // Clamped to the ceiling too, in case riseHeight alone would already
        // push cruise altitude above what's visible.
        _cruiseY = Mathf.Min(_rt.anchoredPosition.y + riseHeight, _maxAltitudeY);
        _hasReachedCruise = false;
        _isBreathingFire = false;

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

        // Was FaceDirection(destination.x - pos.x) — comparing live positions.
        // destination (the hover point) is recomputed every frame from a
        // MOVING target, so once the dragon gets close, small frame-to-frame
        // drift in the target's position flips the sign of
        // (destination.x - pos.x) back and forth, re-mirroring the sprite
        // every frame — the exact "continuously rotating" glitch. Use the
        // same fixed-side convention as the ENGAGE branch below instead:
        // which side a dragon approaches from is already fully determined
        // by which team it's on, never by live position comparison.
        FaceDirection(_battleUnit.isPlayerUnit ? 1f : -1f);
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

        // Clamp Y to the altitude ceiling — without this, a target seated
        // high on the castle's TopSlots staircase (a Cannon/Archer) plus
        // hoverOffset.y puts the hover point above the visible canvas, and
        // the dragon dutifully flies straight there, looking like it left
        // the game scene. Hovering at the ceiling directly above a
        // high-seated target still keeps the target in range for
        // BattleUnit's own attack-range check as long as attackRange
        // comfortably covers the gap — same as any other Y offset here.
        float hoverY = Mathf.Min(targetLocalPos.y + hoverOffset.y, _maxAltitudeY);

        // Clamp X to the canvas's visible left/right edges — without this, a
        // target near either edge of the battlefield (e.g. a cannon/archer
        // on the outer end of the castle's TopSlots row) pushes the hover
        // point past the canvas boundary and the dragon flies off the side
        // of the screen instead of stopping at the edge.
        float hoverX = Mathf.Clamp(targetLocalPos.x + hoverOffset.x * facingSign, _minX, _maxX);

        return new Vector2(hoverX, hoverY);
    }

    /// <summary>
    /// Converts the battlefield's own top edge into THIS dragon's parent's
    /// local anchoredPosition space, minus topMargin, so RiseToCruise/
    /// GetHoverPoint have a real ceiling to clamp against instead of flying
    /// to whatever Y the maths produces. Uses BattleManager.BattlefieldBounds
    /// (the same play-area rect BattleUnit's ground units are clamped to) so
    /// the dragon can't fly above the battlefield even when the battlefield
    /// is smaller than the full canvas (e.g. leaving room for a HUD at the
    /// top). Falls back to the full canvas rect if BattlefieldBounds isn't
    /// assigned, so nothing changes until you wire it up in the Inspector.
    /// </summary>
    private float ComputeMaxAltitudeY()
    {
        RectTransform bounds = BattleManager.Instance?.BattlefieldBounds ?? _canvasRt;
        if (bounds == null) return float.MaxValue;

        Vector3 topWorld = bounds.TransformPoint(
            new Vector2(bounds.rect.center.x, bounds.rect.yMax));

        return WorldToLocalAnchoredPos(topWorld).y - topMargin;
    }

    /// <summary>
    /// Converts the battlefield's own left/right edges into THIS dragon's
    /// parent's local anchoredPosition space, inset by topMargin, so
    /// GetHoverPoint has real left/right walls to clamp against instead of
    /// flying to whatever X the hover-offset maths produces. Same
    /// BattlefieldBounds-with-canvas-fallback approach as ComputeMaxAltitudeY.
    /// </summary>
    private void ComputeHorizontalBounds(out float minX, out float maxX)
    {
        RectTransform bounds = BattleManager.Instance?.BattlefieldBounds ?? _canvasRt;
        if (bounds == null)
        {
            minX = float.MinValue;
            maxX = float.MaxValue;
            return;
        }

        Vector3 leftWorld = bounds.TransformPoint(
            new Vector2(bounds.rect.xMin, bounds.rect.center.y));
        Vector3 rightWorld = bounds.TransformPoint(
            new Vector2(bounds.rect.xMax, bounds.rect.center.y));

        float leftLocalX = WorldToLocalAnchoredPos(leftWorld).x;
        float rightLocalX = WorldToLocalAnchoredPos(rightWorld).x;

        // Don't assume left edge → smaller local X — a bot-side dragon's
        // parent can be mirrored (flipHorizontally on a bot castle cell),
        // which flips which converted value ends up smaller.
        minX = Mathf.Min(leftLocalX, rightLocalX) + topMargin;
        maxX = Mathf.Max(leftLocalX, rightLocalX) - topMargin;
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