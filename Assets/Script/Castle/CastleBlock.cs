using UnityEngine;
using UnityEngine.UI;
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

    /// <summary>Fired whenever this block's wall tier changes via ApplyWallUpgrade.</summary>
    public event Action<CastleBlock, CastleWallData> OnWallChanged;

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

    // ─── Wall Visual ─────────────────────────────────
    [Header("Wall Visual")]
    [Tooltip("The Image showing this block's wall sprite. Swapped by " +
             "CastleWallUpgrader/ApplyWallUpgrade whenever the wall's tier changes. " +
             "Auto-found in children by name (\"WallArt\" or \"WallSprite\") if left blank.")]
    public Image wallArtImage;

    private void Awake()
    {
        _health = maxHealth;
        _shield = maxShield;
        _durability = maxDurability;

        if (wallArtImage == null)
        {
            // Try both common naming conventions so prefabs using either
            // "WallArt" or "WallSprite" as the child name work out of the box.
            Transform found = transform.Find("WallArt") ?? transform.Find("WallSprite");
            if (found != null) wallArtImage = found.GetComponent<Image>();
        }

        if (wallArtImage == null)
            Debug.LogWarning($"[CastleBlock] '{blockName}': wallArtImage is unassigned — " +
                              "wall sprite swaps from ApplyWallUpgrade() will silently do nothing. " +
                              "Assign it in the Inspector or name the child \"WallArt\"/\"WallSprite\".", this);

        DisableArtRaycasts();
    }

    /// <summary>
    /// CastleBlock renders ON TOP of its sibling CastleBlockUnitSlot
    /// (see GridCell.ShowUnitSlot — the slot is pushed to the first sibling
    /// so the wall art draws over the cannon/archer, making them appear
    /// "inside" the castle). That means every Image making up this block's
    /// visuals would otherwise intercept pointer events meant for the
    /// CannonZone/ArcherZone buttons sitting behind it. Disable raycastTarget
    /// on all purely decorative Images here so clicks pass through.
    /// </summary>
    private void DisableArtRaycasts()
    {
        foreach (var img in GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            // Leave any Image that's part of an actual interactive element alone —
            // only strip raycasting from pure decoration (wall sprites, HUD bars).
            if (img.GetComponent<UnityEngine.UI.Button>() != null) continue;
            img.raycastTarget = false;
        }
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

    // ─── Wall Upgrade ────────────────────────────────

    /// <summary>
    /// Swaps this block over to a new CastleWallData tier — called by
    /// CastleWallUpgrader once its Update-button timer finishes. Updates
    /// name/cost/stat caps, refills health/shield/durability to the new
    /// (usually higher) max, and swaps the visible wall sprite.
    /// </summary>
    public void ApplyWallUpgrade(CastleWallData data)
    {
        if (data == null) return;

        blockName = data.wallName;
        blockCost = data.updateCost;

        maxHealth = data.maxHealth;
        maxShield = data.maxShield;
        maxDurability = data.maxDurability;

        // A freshly-completed wall starts at full stats rather than
        // carrying over whatever fraction of health/shield/durability the
        // previous tier happened to be at.
        _health = maxHealth;
        _shield = maxShield;
        _durability = maxDurability;

        if (wallArtImage != null && data.wallSprite != null)
            wallArtImage.sprite = data.wallSprite;

        if (hud != null && hud.blockNameLabel != null)
            hud.blockNameLabel.text = blockName;

        OnStatsChanged?.Invoke(this);
        OnWallChanged?.Invoke(this, data);
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