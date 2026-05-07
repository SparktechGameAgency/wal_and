/// <summary>
/// AREA FORGE - Equipment Enums
/// Central place for all equipment-related enums.
/// Add new slot types here when expanding the game.
/// </summary>

public enum EquipmentSlot
{
    BodyType,   // Skinny / Chubby / Muscular
    Face,       // Skin colour / face style
    Hair,       // Hair style
    Helmet,     // Helmet / headgear
    Armor,      // Chest armour
    Weapon      // Sword / axe / staff
}

public enum BodyType
{
    Normal,     // ← FIRST so new BodyTypeVariants default to Normal in the Inspector
    Skinny,
    Chubby,
    Muscular
}