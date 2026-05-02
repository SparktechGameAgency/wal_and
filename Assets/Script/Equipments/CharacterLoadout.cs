using UnityEngine;

/// <summary>
/// AREA FORGE - CharacterLoadout (ScriptableObject)
///
/// Defines the DEFAULT equipment the soldier spawns with.
/// Create via: Right-click Project window → AreaForge → Character Loadout
///
/// Drag this onto the CharacterEquipment component in the soldier prefab Inspector.
/// Leave any slot null to spawn with nothing equipped in that slot.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Character Loadout", fileName = "DefaultLoadout")]
public class CharacterLoadout : ScriptableObject
{
    [Header("Default Equipment Per Slot")]
    public EquipmentItem defaultBodyType;   // e.g. "Normal Body"
    public EquipmentItem defaultFace;       // e.g. "Default Face"
    public EquipmentItem defaultHair;       // e.g. "Short Hair"
    public EquipmentItem defaultHelmet;     // null = no helmet on spawn
    public EquipmentItem defaultArmor;      // e.g. "Leather Armor"
    public EquipmentItem defaultWeapon;     // e.g. "Iron Sword"
}