using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// AREA FORGE — CastleWallData  (ScriptableObject)
///
/// One asset per wall type/tier — e.g. WoodWall, StoneWall, IronWall …
/// Create via: Right-click Project → Create → AreaForge → Castle Wall Data
///
/// CastleWallUpgrader holds an ordered List&lt;CastleWallData&gt; (wallTiers).
/// Add as many of these assets as you like and drag them into that list in
/// whatever order you want the wall to progress through — nothing in code
/// needs to change to add a new tier.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Castle Wall Data", fileName = "NewCastleWall")]
public class CastleWallData : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────

    [Header("Identity")]
    public string wallName = "Stone Wall";

    [Tooltip("Purely informational — the actual progression order is whatever " +
             "order you drop assets into CastleWallUpgrader.wallTiers, not this number.")]
    public int tier = 1;

    // ── Visual ────────────────────────────────────────────────────────────────

    [Header("Visual")]
    [Tooltip("The crenellated/battlement top edge piece — swapped onto " +
             "CastleBlock.topWallArtImage the moment this tier finishes building.")]
    public Sprite topWallSprite;

    [Tooltip("The repeating wall body texture below the top piece — swapped " +
             "onto CastleBlock.wallArtImage the moment this tier finishes building.")]
    [FormerlySerializedAs("wallSprite")]
    public Sprite bodyWallSprite;

    // ── Stats ─────────────────────────────────────────────────────────────────

    [Header("Stats")]
    public float maxHealth;
    public float maxShield;
    public float maxDurability;

    // ── Update (upgrade) ─────────────────────────────────────────────────────

    [Header("Update Button")]
    [Tooltip("Real-time seconds the Update button waits before the wall actually changes to this tier.")]
    public float updateDuration = 10f;

    [Tooltip("Coins/currency cost to start updating into this tier. 0 = free.")]
    public int updateCost = 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (topWallSprite == null)
            Debug.LogWarning($"[CastleWallData] '{name}': topWallSprite is empty — " +
                              "the wall's top edge will appear blank once this tier is applied.", this);

        if (bodyWallSprite == null)
            Debug.LogWarning($"[CastleWallData] '{name}': bodyWallSprite is empty — " +
                              "the wall body will appear blank once this tier is applied.", this);

        if (updateDuration < 0f)
            updateDuration = 0f;
    }
#endif
}