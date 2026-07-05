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
[RequireComponent(typeof(RectTransform))]
public class BattleUnit : MonoBehaviour
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

    // Cannons / archers don't walk — set this false.
    public bool canMove = true;

    private RectTransform _rt;
    private Canvas _canvas;
    private BattleUnit _target;
    private float _attackTimer;

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

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _animator = GetComponent<SpriteLayerAnimator>();
        _horseController = GetComponent<HorseController>();
        CurrentHealth = maxHealth;
        UpdateHPBar();
    }

    // Temporary diagnostics — set true on the soldier's BattleUnit in the
    // Inspector (or leave the default here) to print target/distance/position
    // once a second, to pin down exactly why a unit isn't moving.
    [Header("Debug")]
    public bool debugLog = false;
    private float _debugTimer;

    private void Update()
    {
        if (IsDead) return;

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

        float dist = Mathf.Abs(_target.WorldX - WorldX);

        if (dist > attackRange)
        {
            // Walk toward enemy.
            if (canMove)
            {
                float dir = isPlayerUnit ? 1f : -1f;
                Vector2 pos = _rt.anchoredPosition;
                pos.x += dir * moveSpeed * Time.deltaTime;
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