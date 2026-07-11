using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleUnit
///
/// Attach to every unit prefab used in the Battle scene (player and bot).
/// Handles movement toward the enemy side, finding a target, attacking,
/// taking damage, and dying.
///
/// All movement is anchoredPosition-based (Screen Space Overlay canvas).
/// </summary>
// Implements IDamageable so the SAME CannonAutoShooter/ArcherUnit components
// carried over from the Village (still scanning for a target every frame)
// can fire on a BattleUnit here in the Battle scene, exactly like they fire
// on an EnemyUnit back in the Village — see IDamageable.cs.
[RequireComponent(typeof(RectTransform))]
public class BattleUnit : MonoBehaviour, IDamageable
{
    // ── Configuration ─────────────────────────────────────────────────────────
    [Header("Unit Config")]
    public bool isPlayerUnit = true;
    public BattleUnitType unitType = BattleUnitType.Soldier;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float moveSpeed = 80f;   // pixels per second (anchoredPosition)
    public float attackRange = 60f;   // pixels — when to stop and attack
    public float attackRate = 1f;    // attacks per second

    [Header("References")]
    [Tooltip("Optional HP bar slider parented under this unit.")]
    public Slider hpBar;

    // ── State ─────────────────────────────────────────────────────────────────
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>IDamageable — the Transform for cannons/archers to aim at.</summary>
    public Transform DamageableTransform => transform;

    // Cannons / archers don't walk — set this false.
    public bool canMove = true;

    private RectTransform _rt;
    private Canvas _canvas;
    private BattleUnit _target;
    private float _attackTimer;

    // Left/right walk limit, converted once into THIS unit's own parent's
    // local anchoredPosition space (soldiers/horses live under different
    // parents — PlayerArmyRoot, BotArmyRoot, or a castle slot — so the same
    // world-space bound converts to a different local X for each). Only
    // canMove units (Soldier/Horse) ever use these; Cannon/Archer/Dragon
    // don't walk here. float.MinValue/MaxValue (no clamp) until Start()
    // computes real values from BattleManager.BattlefieldBounds, so a
    // missing reference fails safe to the old unbounded behaviour instead
    // of trapping every unit at x=0.
    private float _minLocalX = float.MinValue;
    private float _maxLocalX = float.MaxValue;

    // Drives Walk/Fight/Idle for on-foot units (Soldier/Archer) that carry
    // their own SpriteLayerAnimator directly on this GameObject. Horse/Dragon
    // units use a different animation system on their own root (Animator
    // component / DragonBodyAnimator), so GetComponent (non-recursive) simply
    // finds nothing there and this safely no-ops for them.
    private SpriteLayerAnimator _animator;
    private AnimationState _currentAnimState = AnimationState.Idle;

    // Drives Idle/Run/Fight/Dead for Horse units. HorseController lives on
    // the same root as this BattleUnit; on-foot units simply have none, so
    // this stays null and every call below safely no-ops for them.
    private HorseController _horseController;

    // Cached so ClimbThroughDoor can hide/show the soldier's normal layered
    // visuals while the CastleDoor overlay plays, without a GetComponent
    // lookup every climb. Null for every unit type except Soldier — no-ops
    // safely everywhere below.
    private SoldierController _soldierController;

    // Cached so ClimbThroughDoor can read which of the 6 armor variations this
    // soldier currently has equipped and ask the door for the matching
    // enter/exit frame set (CastleDoor.GetFrames). A flat-army Soldier is the
    // SAME live GameObject carried over from the Village (not a respawned
    // snapshot), so its real CharacterEquipment is already sitting right here.
    // Null for every unit type except Soldier — no-ops safely everywhere below.
    private CharacterEquipment _equipment;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _animator = GetComponent<SpriteLayerAnimator>();
        _horseController = GetComponent<HorseController>();
        _soldierController = GetComponent<SoldierController>();
        _equipment = GetComponent<CharacterEquipment>();
        CurrentHealth = maxHealth;
        UpdateHPBar();
    }

    private void Start()
    {
        // Only walking units (Soldier/Horse) ever need this — Cannon/Archer
        // never move and Dragon is driven entirely by BattleDragonFlight's
        // own separate bounds. Skip the conversion for anything that can't
        // walk off the battlefield in the first place.
        if (!canMove) return;

        RectTransform bounds = BattleManager.Instance?.BattlefieldBounds;
        if (bounds == null) return; // Unassigned — fails safe, no clamp.

        ComputeHorizontalBounds(bounds, out _minLocalX, out _maxLocalX);
    }

    /// <summary>
    /// Converts the battlefield RectTransform's left/right edges into THIS
    /// unit's own parent's local anchoredPosition space, so Update()'s walk
    /// step has real walls to clamp pos.x against. Same technique as
    /// BattleDragonFlight.ComputeMaxAltitudeY/ComputeHorizontalBounds — each
    /// unit converts independently because soldiers/horses can live under
    /// different parents (PlayerArmyRoot, BotArmyRoot, or a seated castle
    /// slot), so the same world-space edge maps to a different local X for
    /// each one.
    /// </summary>
    private void ComputeHorizontalBounds(RectTransform bounds, out float minX, out float maxX)
    {
        if (_rt.parent == null)
        {
            minX = float.MinValue;
            maxX = float.MaxValue;
            return;
        }

        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        Vector3 leftWorld = bounds.TransformPoint(new Vector2(bounds.rect.xMin, bounds.rect.center.y));
        Vector3 rightWorld = bounds.TransformPoint(new Vector2(bounds.rect.xMax, bounds.rect.center.y));

        Vector2 leftScreen = RectTransformUtility.WorldToScreenPoint(cam, leftWorld);
        Vector2 rightScreen = RectTransformUtility.WorldToScreenPoint(cam, rightWorld);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, leftScreen, cam, out Vector2 leftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, rightScreen, cam, out Vector2 rightLocal);

        // Don't assume left edge → smaller local X — a bot-side unit's
        // parent can be mirrored (flipHorizontally on a bot castle cell),
        // which flips which converted value ends up smaller.
        minX = Mathf.Min(leftLocal.x, rightLocal.x);
        maxX = Mathf.Max(leftLocal.x, rightLocal.x);
    }

    // Temporary diagnostics — set true on the soldier's BattleUnit in the
    // Inspector (or leave the default here) to print target/distance/position
    // once a second, to pin down exactly why a unit isn't moving.
    [Header("Debug")]
    public bool debugLog = false;
    private float _debugTimer;

    // ── Castle Door Climbing (Soldier only) ─────────────────────────────────
    // See CastleDoor.cs and BattleManager.GetCastleDoorForClimb. Cannon/Archer
    // targets sit on a castle block far above the flat ground row, but
    // FindNearestEnemy/WorldX only ever compared horizontal distance — so a
    // soldier used to walk straight at a cannon it could never actually
    // reach and just stood there in range on the wrong floor. This makes a
    // Soldier climb the castle's own doors floor-by-floor instead.

    [Header("Castle Door Climbing (Soldier only)")]
    [Tooltip("Child Image (start it disabled in the prefab) used to play the door " +
             "enter/exit frame sequence. Leave unassigned to still climb doors, just " +
             "without the visual — the soldier will pause in place for each transition instead.")]
    public Image castleDoorOverlayImage;

    /// <summary>Which castle grid row (floor) THIS unit's target sits on. -1 = not on a
    /// castle block at all (a flat-army Soldier/Horse/Dragon, or simply unassigned) —
    /// BattleUnit.Update reads that as "nothing to climb toward, use the old flat walk."
    /// Set via SetCastleRow() for Cannon/Archer units seated on a castle block.</summary>
    public int CastleRow { get; private set; } = -1;

    public void SetCastleRow(int row) => CastleRow = row;

    /// <summary>Which floor THIS soldier is currently standing on. 0 = ground / outside
    /// the castle. Only ever increases — see ClimbThroughDoor.</summary>
    private int _currentFloor = 0;

    /// <summary>True while a ClimbThroughDoor coroutine owns this unit's movement and
    /// animation — Update()'s normal walk/attack logic is skipped entirely until it clears.</summary>
    private bool _isClimbing;
    private Coroutine _climbRoutine;

    private void Update()
    {
        if (IsDead) return;

        // A ClimbThroughDoor coroutine owns movement/animation for the
        // duration of a door transition — don't fight it with the normal
        // walk/attack logic below.
        if (_isClimbing) return;

        // Always retarget to whoever is currently nearest — a unit shouldn't
        // stay locked onto its first target for the whole fight if a closer
        // enemy shows up (e.g. after moving, or another enemy approaching
        // from the other direction).
        _target = BattleManager.Instance?.FindNearestEnemy(this);

        if (debugLog)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= 1f)
            {
                _debugTimer = 0f;
                Debug.Log($"[BattleUnit] '{name}' isPlayerUnit={isPlayerUnit} canMove={canMove} " +
                          $"target={(_target != null ? _target.name : "NULL")} " +
                          $"anchoredPos={_rt.anchoredPosition} worldX={WorldX} parent={transform.parent?.name}");
            }
        }

        if (_target == null) return;

        // ── Castle door climbing (Soldier only) ─────────────────────────
        // The current target sits on a castle floor higher than this
        // soldier has climbed to yet — walk to that floor's door and climb
        // instead of trying to close a WorldX gap that's actually a wall
        // away. Once _currentFloor catches up to the target's floor, this
        // is skipped and the normal flat walk/attack below just works,
        // since by then both units are effectively at the same height.
        if (unitType == BattleUnitType.Soldier && canMove && _target.CastleRow > _currentFloor)
        {
            CastleDoor door = BattleManager.Instance?.GetCastleDoorForClimb(isPlayerUnit, _currentFloor);
            if (door != null)
            {
                ApproachDoor(door);
                return;
            }
            // No door configured for this floor (castleDoorPrefab unassigned,
            // or this is the unsupported bot-attacks-player-castle direction)
            // — fails safe to the old flat walk below instead of the soldier
            // getting stuck here forever.
        }

        float dist = Mathf.Abs(_target.WorldX - WorldX);

        if (dist > attackRange)
        {
            // Walk toward enemy.
            if (canMove)
            {
                float dir = isPlayerUnit ? 1f : -1f;
                Vector2 pos = _rt.anchoredPosition;
                pos.x += dir * moveSpeed * Time.deltaTime;

                // Stop at the battlefield edge instead of walking straight
                // off it — without this, a unit chasing a target that's
                // slow to come into range (or unreachable, e.g. blocked by
                // other units bunched up) just keeps walking past wherever
                // the visible battlefield actually ends.
                pos.x = Mathf.Clamp(pos.x, _minLocalX, _maxLocalX);

                _rt.anchoredPosition = pos;

                // Flip sprite to face the right direction.
                Vector3 scale = _rt.localScale;
                scale.x = isPlayerUnit ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                _rt.localScale = scale;

                SetAnimState(AnimationState.Run);
            }
            else
            {
                // Stationary unit (Cannon/Archer) with no target in range yet.
                SetAnimState(AnimationState.Idle);
            }
        }
        else
        {
            // Attack.
            SetAnimState(AnimationState.Fight);

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= 1f / attackRate)
            {
                _attackTimer = 0f;
                _target.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// Switches the on-foot animation state, but only actually calls into
    /// SpriteLayerAnimator when the state is changing — SetState() resets
    /// every layer back to frame 0, so calling it every single frame with
    /// the same state (e.g. "Fight" while repeatedly attacking) would freeze
    /// the animation on frame 0 instead of playing it.
    /// </summary>
    private void SetAnimState(AnimationState state)
    {
        bool changed = _currentAnimState != state;
        if (changed)
        {
            _currentAnimState = state;
            _animator?.SetState(state);
        }

        // HorseController tracks its own state (HorseState, a separate enum)
        // and already no-ops correctly when told to re-enter the state it's
        // already in (via CurrentState), so this check is independent of the
        // SpriteLayerAnimator guard above — it fires whenever the HORSE's
        // state actually needs to change, not just when this unit's on-foot
        // state changes.
        if (_horseController == null) return;

        HorseState horseState = MapToHorseState(state);
        if (_horseController.CurrentState == horseState) return;

        switch (horseState)
        {
            case HorseState.Run: _horseController.SetRun(); break;
            case HorseState.Fight: _horseController.SetFight(); break;
            case HorseState.Dead: _horseController.SetDead(); break;
            default: _horseController.SetIdle(); break;
        }
    }

    private static HorseState MapToHorseState(AnimationState state) => state switch
    {
        AnimationState.Run => HorseState.Run,
        AnimationState.Fight => HorseState.Fight,
        AnimationState.Death => HorseState.Dead,
        _ => HorseState.Idle,
    };

    // ── Castle Door Climbing (Soldier only) ─────────────────────────────────

    /// <summary>
    /// Walks toward <paramref name="door"/>'s screen X exactly like the
    /// normal enemy-chase walk (same moveSpeed/bounds-clamp/facing-flip),
    /// then hands off to ClimbThroughDoor once close enough — so the
    /// approach looks identical to normal combat movement right up until
    /// the soldier reaches the door.
    /// </summary>
    private void ApproachDoor(CastleDoor door)
    {
        float doorDist = Mathf.Abs(door.WorldX - WorldX);

        if (doorDist > attackRange)
        {
            float dir = isPlayerUnit ? 1f : -1f;
            Vector2 pos = _rt.anchoredPosition;
            pos.x += dir * moveSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, _minLocalX, _maxLocalX);
            _rt.anchoredPosition = pos;

            Vector3 scale = _rt.localScale;
            scale.x = isPlayerUnit ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _rt.localScale = scale;

            SetAnimState(AnimationState.Run);
        }
        else if (_climbRoutine == null)
        {
            _climbRoutine = StartCoroutine(ClimbThroughDoor(door));
        }
    }

    /// <summary>
    /// Plays the door's enterFrames, snaps this soldier to the matching door
    /// one floor up (same side), then plays that door's exitFrames there.
    /// _isClimbing blocks Update()'s normal logic for the whole sequence.
    /// Runs again automatically on the next Update() if the soldier's real
    /// target is still further up — this is what makes climbing repeat
    /// floor-by-floor instead of just once.
    /// </summary>
    private IEnumerator ClimbThroughDoor(CastleDoor door)
    {
        _isClimbing = true;
        SetAnimState(AnimationState.Idle);

        // Which of the 6 armor variations THIS soldier is wearing right now —
        // null-safe: soldiers with no CharacterEquipment (shouldn't happen for
        // a real Soldier, but fails safe) or no armor equipped both fall
        // through to the door's fallback frames.
        EquipmentItem armor = _equipment != null ? _equipment.GetEquipped(EquipmentSlot.Armor) : null;
        door.GetFrames(armor, out Sprite[] enterFrames, out Sprite[] exitFrames);

        yield return PlayDoorFrames(enterFrames, door.frameInterval);

        int nextFloor = _currentFloor + 1;
        CastleDoor exitDoor = BattleManager.Instance?.GetCastleDoorForClimb(isPlayerUnit, nextFloor);
        if (exitDoor != null)
            _rt.anchoredPosition = ConvertToLocalPosition(exitDoor.transform);

        _currentFloor = nextFloor;

        // Re-resolve frames against exitDoor — it's a different CastleDoor
        // instance (one per floor) and may have its own armorFrames table, so
        // re-running the same armor lookup here (rather than reusing
        // exitFrames from the entry door above) keeps the exit animation
        // correct even if a per-floor door's table differs from the one
        // the soldier just entered.
        CastleDoor frameSourceForExit = exitDoor != null ? exitDoor : door;
        frameSourceForExit.GetFrames(armor, out _, out Sprite[] exitFramesForThisDoor);

        yield return PlayDoorFrames(exitFramesForThisDoor, frameSourceForExit.frameInterval);

        _isClimbing = false;
        _climbRoutine = null;
    }

    /// <summary>
    /// Steps castleDoorOverlayImage through frames while hiding the soldier's
    /// normal layered visuals underneath it, then restores them. No-ops (just
    /// waits nothing / returns immediately) if frames is empty — climbing
    /// still works, there's just no door visual, e.g. before an overlay Image
    /// is wired up on the prefab.
    /// </summary>
    private IEnumerator PlayDoorFrames(Sprite[] frames, float interval)
    {
        if (frames == null || frames.Length == 0) yield break;

        bool hasOverlay = castleDoorOverlayImage != null;
        if (hasOverlay)
        {
            castleDoorOverlayImage.enabled = true;
            _soldierController?.SetVisualRootActive(false);
        }

        foreach (var frame in frames)
        {
            if (hasOverlay) castleDoorOverlayImage.sprite = frame;
            yield return new WaitForSeconds(interval);
        }

        if (hasOverlay)
        {
            castleDoorOverlayImage.enabled = false;
            _soldierController?.SetVisualRootActive(true);
        }
    }

    /// <summary>
    /// Converts a world Transform's position into THIS unit's own parent's
    /// local anchoredPosition space — same WorldToScreenPoint →
    /// ScreenPointToLocalPointInRectangle technique ComputeHorizontalBounds
    /// already uses, so a soldier reappearing at a door lands at the exact
    /// on-screen spot regardless of Canvas scale or how deeply the door and
    /// this soldier are each nested under their own (different) parents.
    /// </summary>
    private Vector2 ConvertToLocalPosition(Transform worldTarget)
    {
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, screenPoint, cam, out Vector2 localPoint);

        return localPoint;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Vector2 RectPos => _rt.anchoredPosition;

    // Exposes the same target BattleUnit.Update() already recomputes every
    // frame via BattleManager.FindNearestEnemy — read by BattleDragonFlight
    // so the dragon flies toward and hovers over the EXACT unit this
    // BattleUnit will deal its attack-tick damage to, instead of running a
    // second, possibly-different target search of its own.
    public BattleUnit CurrentTarget => _target;

    // Player units live under PlayerArmyRoot and bot units live under
    // BotArmyRoot — two different RectTransforms positioned at different
    // places on screen. Their anchoredPosition values are LOCAL to those
    // different parents, so comparing RectPos.x across player vs bot units
    // does not represent real screen distance.
    //
    // Raw _rt.position (world space) looked like the fix, but the Canvas is
    // scaled way down by the Canvas Scaler, so ALL units end up bunched
    // within a few world-units of each other regardless of how far apart
    // they actually are on screen — e.g. a soldier at anchoredPos x=-577
    // and an enemy at anchoredPos x=0 came out as worldX -9.78 vs 2.34, a
    // "distance" of ~12, well under attackRange (60), so the soldier locked
    // into "attack" the instant it spawned instead of walking over.
    //
    // RectTransformUtility.WorldToScreenPoint converts to actual on-screen
    // PIXEL coordinates, which stay consistent regardless of Canvas scale,
    // scale factor, or how deeply a unit is nested — exactly the frame
    // attackRange/moveSpeed were tuned against.
    public float WorldX
    {
        get
        {
            Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;
            return RectTransformUtility.WorldToScreenPoint(cam, _rt.position).x;
        }
    }

    /// <param name="skipFacingFlip">
    /// Set true only for units parented under an already-mirrored container
    /// (e.g. a bot castle cell, which carries flipHorizontally's -1 scale).
    /// Flipping this unit's OWN scale on top of that would double-negate and
    /// visually un-mirror it. Flat-army units (the normal case) leave this
    /// false so they get their own explicit left/right flip immediately at
    /// spawn instead of waiting for the first Update() walk tick.
    /// </param>
    public void Init(BattleUnitData data, bool playerUnit, bool skipFacingFlip = false)
    {
        isPlayerUnit = playerUnit;
        maxHealth = data.health > 0 ? data.health : maxHealth;
        CurrentHealth = maxHealth;
        damage = data.damage > 0 ? data.damage : damage;
        moveSpeed = data.moveSpeed > 0 ? data.moveSpeed : moveSpeed;
        unitType = data.unitType;
        // Cannon/Archer never move. Dragon ALSO never uses this class's own
        // straight-line ground walk — BattleDragonFlight (sibling component
        // on the same prefab) drives its position instead (rise → fly over
        // → hover near the target), leaving this class to just find the
        // target, run the attack-range check, and tick damage — the exact
        // same code path already used for every other unit type.
        canMove = data.unitType != BattleUnitType.Cannon &&
                        data.unitType != BattleUnitType.Archer &&
                        data.unitType != BattleUnitType.Dragon;
        UpdateHPBar();

        if (!skipFacingFlip)
        {
            // Face the correct direction immediately on spawn — player units
            // face right (toward the bot), bot units face left (toward the
            // player) — instead of only flipping once Update() first moves
            // this unit toward its target (which never happened at all for
            // Cannon/Archer, since they never enter the move branch).
            Vector3 scale = _rt.localScale;
            scale.x = playerUnit ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _rt.localScale = scale;

            // Soldiers carried over from the Village patrol drive their own
            // visual facing through a separate CHILD transform (SoldierController
            // .visualRoot), completely independent of this root RectTransform's
            // scale. Flipping the root above does NOT correct a soldier that
            // was mid-patrol facing the "wrong" way when the battle started —
            // its child flip still wins. Force it explicitly here so a soldier
            // always turns to face the enemy before it starts running.
            SoldierController soldierController = GetComponent<SoldierController>();
            if (soldierController != null)
                soldierController.SetBattleFacing(faceRight: playerUnit);
        }

        // Horse units need their animation data initialised — HorseController
        // starts with _data == null until Setup() is called, so without this
        // it silently never plays Idle/Run/Fight (PATH=NONE in its own diagnostics).
        if (_horseController != null && data.horseType != null)
            _horseController.Setup(data.horseType);

        ApplyRiderVisuals(data);
    }

    /// <summary>
    /// Horse and Dragon prefabs carry Face/Helmet/Armor/Weapon child Images
    /// driven by HorseRiderVisual / DragonRiderVisual, exactly like the
    /// Village scene. Those components need a live CharacterEquipment to
    /// read from, so we build a throwaway one here and feed it the
    /// snapshotted items (player's real loadout, or the bot's random one).
    /// </summary>
    private void ApplyRiderVisuals(BattleUnitData data)
    {
        bool hasRiderData = data.riderFace != null || data.riderArmor != null ||
                             data.riderHelmet != null || data.riderWeapon != null;
        if (!hasRiderData) return;

        CharacterEquipment equipment = gameObject.AddComponent<CharacterEquipment>();
        if (data.riderFace != null) equipment.Equip(data.riderFace);
        if (data.riderArmor != null) equipment.Equip(data.riderArmor);
        if (data.riderHelmet != null) equipment.Equip(data.riderHelmet);
        if (data.riderWeapon != null) equipment.Equip(data.riderWeapon);

        HorseRiderVisual horseRider = GetComponentInChildren<HorseRiderVisual>(true);
        if (horseRider != null) horseRider.ShowRider(equipment);

        DragonRiderVisual dragonRider = GetComponentInChildren<DragonRiderVisual>(true);
        if (dragonRider != null) dragonRider.ShowForSoldier(equipment);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        UpdateHPBar();

        if (CurrentHealth <= 0f)
            Die();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void Die()
    {
        IsDead = true;
        BattleManager.Instance?.OnUnitDied(this);
        SetAnimState(AnimationState.Death);
        // Simple fade-out / immediate destroy — replace with animation as needed.
        Destroy(gameObject, 0.3f);
    }

    private void UpdateHPBar()
    {
        if (hpBar != null)
            hpBar.value = maxHealth > 0 ? CurrentHealth / maxHealth : 0f;
    }
}