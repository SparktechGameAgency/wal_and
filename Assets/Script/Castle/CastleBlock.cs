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
    [Tooltip("The Image showing this block's wall TOP (crenellated edge) sprite. " +
             "Swapped by CastleWallUpgrader/ApplyWallUpgrade whenever the wall's tier " +
             "changes. Auto-found in children by name (\"TopWallSprite\", \"WallTop\", or \"TopWallArt\") if left blank.")]
    public Image topWallArtImage;

    [Tooltip("The Image showing this block's wall BODY sprite. Swapped by " +
             "CastleWallUpgrader/ApplyWallUpgrade whenever the wall's tier changes. " +
             "Auto-found in children by name (\"BodyWallSprite\", \"WallArt\", \"WallSprite\", or \"WallBody\") if left blank.")]
    public Image wallArtImage;

    private void Awake()
    {
        _health = maxHealth;
        _shield = maxShield;
        _durability = maxDurability;

        if (topWallArtImage == null)
        {
            // transform.Find only checks DIRECT children — won't reach an
            // Image nested deeper. Search recursively by name instead.
            topWallArtImage = FindImageInChildrenByName("TopWallSprite", "WallTop", "TopWallArt");
        }

        if (wallArtImage == null)
        {
            // Same recursive search, covering both naming conventions.
            wallArtImage = FindImageInChildrenByName("BodyWallSprite", "WallArt", "WallSprite", "WallBody");
        }

        // NOTE: it's expected/by-design for a block to have only ONE of
        // these two assigned, not both — diagonal blocks only carry a top
        // (crenellated) piece, regular blocks only carry a body piece. Don't
        // guess at a substitute Image when one is missing; ApplyWallUpgrade
        // already null-checks each one independently and simply skips
        // whichever isn't present, which is the correct behavior here.

        // No warnings here if one is null — see the note above. A block that
        // truly has neither (both null) would just render no wall art at
        // all, which is a real misconfiguration, so that case alone is worth
        // flagging.
        if (topWallArtImage == null && wallArtImage == null)
            Debug.LogWarning($"[CastleBlock] '{blockName}': neither topWallArtImage nor wallArtImage " +
                              "is assigned/found — this block will show no wall art at all. " +
                              "Assign at least one in the Inspector.", this);

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
    /// <summary>
    /// Recursively searches all descendants (any depth, not just direct
    /// children like transform.Find) for the first Image whose GameObject
    /// name case-insensitively matches any of the given candidate names.
    /// </summary>
    private Image FindImageInChildrenByName(params string[] candidateNames)
    {
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            foreach (var name in candidateNames)
            {
                if (string.Equals(img.gameObject.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return img;
            }
        }
        return null;
    }

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

    // ─── Top-Of-Column Visual ─────────────────────────

    /// <summary>
    /// Only one of topWallArtImage / wallArtImage should ever be VISIBLE at
    /// once on a given block — they're full-size siblings, so having both
    /// active means whichever is the later sibling (TopWallSprite, in the
    /// current prefab) always renders over and completely hides the other,
    /// everywhere, regardless of which one "should" be showing.
    ///
    /// Called by CastleGrid.RefreshUnitSlots() with the same isTop == true
    /// condition it already uses for "isExposed" (no block above this one) —
    /// the top-most block in each column shows the crenellated top piece,
    /// every block under it shows the plain body piece instead.
    /// </summary>
    public void SetTopOfColumn(bool isTop)
    {
        if (topWallArtImage != null)
            topWallArtImage.gameObject.SetActive(isTop);

        if (wallArtImage != null)
            wallArtImage.gameObject.SetActive(!isTop);
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

        if (topWallArtImage != null && data.topWallSprite != null)
            topWallArtImage.sprite = data.topWallSprite;

        if (wallArtImage != null && data.bodyWallSprite != null)
            wallArtImage.sprite = data.bodyWallSprite;

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