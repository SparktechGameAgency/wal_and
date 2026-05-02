using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - CharacterVisuals  (UI Image version)
///
/// Manages all UI Image layers on the player prefab inside the Canvas.
/// Attach to the root Player GameObject.
///
/// YOUR EXISTING HIERARCHY (from your screenshot) maps like this:
///
///   Player              ← Add CharacterVisuals HERE (drag children into fields)
///     ├── Head          ← drag into "Face Image" field    (face / skin)
///     ├── Hair          ← drag into "Hair Image" field
///     ├── Helmet        ← drag into "Helmet Image" field
///     └── Armor         ← drag into "Armor Image" field
///
/// You also need to add two more children yourself:
///     ├── Body          ← NEW — base body silhouette (bottommost layer)
///     └── Weapon        ← NEW — held weapon (topmost layer)
///
/// UI Image draw order is controlled by sibling index (top of hierarchy = back).
/// Order from top to bottom in Hierarchy = back to front on screen:
///   Body → Face/Head → Hair → Armor → Helmet → Weapon
/// </summary>
public class CharacterVisuals : MonoBehaviour
{
    // ─── Inspector: drag your Image children in here ─────────────────────────

    [Header("UI Image Layers — drag each child GameObject's Image here")]
    [Tooltip("Base body silhouette — bottommost layer")]
    [SerializeField] private Image bodyImage;

    [Tooltip("Face / skin colour — your 'Head' child")]
    [SerializeField] private Image faceImage;

    [Tooltip("Hair — your 'Hair' child")]
    [SerializeField] private Image hairImage;

    [Tooltip("Armor — your 'Armor' child")]
    [SerializeField] private Image armorImage;

    [Tooltip("Helmet — your 'Helmet' child (hides hair when worn)")]
    [SerializeField] private Image helmetImage;

    [Tooltip("Weapon — topmost layer")]
    [SerializeField] private Image weaponImage;

    // ─── Internal state ───────────────────────────────────────────────────────

    [HideInInspector] public AnimationState CurrentState = AnimationState.Idle;

    // ─── Layer Access ─────────────────────────────────────────────────────────

    public Image GetImage(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.BodyType => bodyImage,
        EquipmentSlot.Face => faceImage,
        EquipmentSlot.Hair => hairImage,
        EquipmentSlot.Armor => armorImage,
        EquipmentSlot.Helmet => helmetImage,
        EquipmentSlot.Weapon => weaponImage,
        _ => null
    };

    /// <summary>
    /// Swaps the sprite on one layer.
    /// Pass null to hide that layer (e.g. "no helmet equipped").
    /// </summary>
    public void SetSprite(EquipmentSlot slot, Sprite sprite)
    {
        var img = GetImage(slot);
        if (img == null) return;

        img.sprite = sprite;
        img.enabled = sprite != null;

        // Hide hair automatically when a helmet is worn
        if (slot == EquipmentSlot.Helmet && hairImage != null)
            hairImage.enabled = (sprite == null);
    }

    public void HideAll()
    {
        foreach (EquipmentSlot s in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var img = GetImage(s);
            if (img != null) img.enabled = false;
        }
    }
}