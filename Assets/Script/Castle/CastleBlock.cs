using UnityEngine;
using System;

public class CastleBlock : MonoBehaviour
{
    // ─── Stats ───────────────────────────────────────
    [Header("Block Stats")]
    public float maxHealth = 100f;
    public float maxShield = 50f;
    public float maxDurability = 100f;

    private float _health;
    private float _shield;
    private float _durability;

    public float Health => _health;
    public float Shield => _shield;
    public float Durability => _durability;

    // ─── Events ──────────────────────────────────────
    public event Action<CastleBlock> OnStatsChanged;
    public event Action<CastleBlock> OnBlockDestroyed;

    // ─── Slots ───────────────────────────────────────
    [Header("Slots")]
    public CannonSlot cannonSlot;
    public SoldierSlot soldierSlot;

    // ─── HUD ─────────────────────────────────────────
    [Header("HUD")]
    public CastleBlockHUD hud;

    // ─── Grid Reference ───────────────────────────────
    private GridCell _gridCell;

    // ─── Block Info ───────────────────────────────────
    [Header("Block Info")]
    public string blockName = "Stone Block";
    public int blockCost = 30;

    private void Awake()
    {
        _health = maxHealth;
        _shield = maxShield;
        _durability = maxDurability;
    }

    private void Start()
    {
        if (hud != null) hud.Bind(this);
    }

    public void SetGridCell(GridCell cell)
    {
        _gridCell = cell;
    }

    // ─── Damage ──────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (_shield > 0)
        {
            float absorbed = Mathf.Min(_shield, amount);
            _shield -= absorbed;
            amount -= absorbed;
        }

        if (amount > 0)
        {
            _health -= amount;
            _health = Mathf.Max(_health, 0f);
        }

        // Durability degrades with each hit
        _durability -= amount * 0.5f;
        _durability = Mathf.Max(_durability, 0f);

        OnStatsChanged?.Invoke(this);

        if (_health <= 0f) DestroyBlock();
    }

    public void RepairHealth(float amount)
    {
        _health = Mathf.Min(_health + amount, maxHealth);
        OnStatsChanged?.Invoke(this);
    }

    public void RechargeShield(float amount)
    {
        _shield = Mathf.Min(_shield + amount, maxShield);
        OnStatsChanged?.Invoke(this);
    }

    public void RepairDurability(float amount)
    {
        _durability = Mathf.Min(_durability + amount, maxDurability);
        OnStatsChanged?.Invoke(this);
    }

    // ─── Destroy ─────────────────────────────────────

    void DestroyBlock()
    {
        Debug.Log($"[CastleBlock] {blockName} destroyed!");
        OnBlockDestroyed?.Invoke(this);

        if (_gridCell != null) _gridCell.ClearBlock();

        // TODO: Play destruction VFX/SFX here
        Destroy(gameObject);
    }

    // ─── Normalized Getters (0-1) for HUD bars ───────

    public float HealthNormalized => _health / maxHealth;
    public float ShieldNormalized => _shield / maxShield;
    public float DurabilityNormalized => _durability / maxDurability;
}