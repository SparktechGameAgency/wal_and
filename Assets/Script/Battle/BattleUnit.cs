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
    private BattleUnit _target;
    private float _attackTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        CurrentHealth = maxHealth;
        UpdateHPBar();
    }

    private void Update()
    {
        if (IsDead) return;

        // Try to keep a valid target.
        if (_target == null || _target.IsDead)
            _target = BattleManager.Instance?.FindNearestEnemy(this);

        if (_target == null) return;

        float dist = Mathf.Abs(_target.RectPos.x - RectPos.x);

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
            }
        }
        else
        {
            // Attack.
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= 1f / attackRate)
            {
                _attackTimer = 0f;
                _target.TakeDamage(damage);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Vector2 RectPos => _rt.anchoredPosition;

    public void Init(BattleUnitData data, bool playerUnit)
    {
        isPlayerUnit = playerUnit;
        maxHealth = data.health > 0 ? data.health : maxHealth;
        CurrentHealth = maxHealth;
        damage = data.damage > 0 ? data.damage : damage;
        moveSpeed = data.moveSpeed > 0 ? data.moveSpeed : moveSpeed;
        unitType = data.unitType;
        canMove = data.unitType != BattleUnitType.Cannon &&
                        data.unitType != BattleUnitType.Archer;
        UpdateHPBar();

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
        // Simple fade-out / immediate destroy — replace with animation as needed.
        Destroy(gameObject, 0.3f);
    }

    private void UpdateHPBar()
    {
        if (hpBar != null)
            hpBar.value = maxHealth > 0 ? CurrentHealth / maxHealth : 0f;
    }
}