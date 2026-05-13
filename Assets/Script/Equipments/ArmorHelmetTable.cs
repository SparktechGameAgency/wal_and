using UnityEngine;
using System;

/// <summary>
/// AREA FORGE — ArmorHelmetTable  (ScriptableObject)
///
/// HOW TO CREATE ONE IN UNITY:
///   Right-click in Project window →
///     Create → AreaForge → Armor Helmet Table
///
/// Assign the created asset to the SoldierDragDrop Inspector field
/// on the Soldier prefab.
///
/// ════════════════════════════════════════════════════════════════════
///  PURPOSE
/// ════════════════════════════════════════════════════════════════════
///
///  When a helmetless soldier is dropped onto a dragon, the mounting
///  system checks whether they have a Helmet equipped. If they don't,
///  it calls GetDefaultHelmet(armor) on this table to find the correct
///  default and equips it automatically via CharacterEquipment.Equip().
///
///  This guarantees every mounted soldier looks intentional and
///  complete, even if the player assembled a character in the UI
///  without selecting a helmet.
///
/// ════════════════════════════════════════════════════════════════════
///  HOW TO FILL THE TABLE
/// ════════════════════════════════════════════════════════════════════
///
///  You have 6 armor variations (and may add more later). Add one
///  ArmorHelmetEntry for each:
///
///    Entries[0]   armor = IronArmor      defaultHelmet = IronHelmet
///    Entries[1]   armor = SteelArmor     defaultHelmet = SteelHelmet
///    Entries[2]   armor = GoldenArmor    defaultHelmet = GoldenHelmet
///    Entries[3]   armor = LeatherArmor   defaultHelmet = LeatherCap
///    Entries[4]   armor = ShadowArmor    defaultHelmet = ShadowHood
///    Entries[5]   armor = CrystalArmor   defaultHelmet = CrystalHelmet
///
///  Also set fallbackHelmet to a generic helmet that suits any soldier
///  who has NO armor at all (e.g. a plain iron helmet).
///
/// ════════════════════════════════════════════════════════════════════
///  LOOKUP LOGIC   (in GetDefaultHelmet)
/// ════════════════════════════════════════════════════════════════════
///
///  1. If entries has a matching armor entry → return its defaultHelmet.
///  2. If no match found → return fallbackHelmet  (any armor w/ no entry,
///     or the soldier has no armor at all).
///  3. If fallbackHelmet is also null → return null and SoldierDragDrop
///     logs a warning; soldier mounts without a helmet.
///
/// ════════════════════════════════════════════════════════════════════
///  EXTENDING THE TABLE
/// ════════════════════════════════════════════════════════════════════
///
///  Add new armors to entries[] in the Inspector at any time — no code
///  changes needed. The lookup is a simple linear scan so even 50 entries
///  runs in negligible time (it fires once per mount event, not per frame).
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Armor Helmet Table", fileName = "ArmorHelmetTable")]
public class ArmorHelmetTable : ScriptableObject
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Armor → Default Helmet Mappings")]
    [Tooltip("Add one entry per armor variation.\n\n" +
             "When a helmetless soldier mounts a dragon, their equipped armor is " +
             "looked up here and the paired default helmet is auto-equipped.\n\n" +
             "Leave an entry's defaultHelmet blank to fall through to fallbackHelmet.")]
    public ArmorHelmetEntry[] entries;

    [Header("Fallback")]
    [Tooltip("Helmet used when:\n" +
             "  • The soldier has no armor equipped, OR\n" +
             "  • The soldier's armor has no entry in the table above, OR\n" +
             "  • An entry exists but its defaultHelmet field is empty.\n\n" +
             "Set this to a universal 'plain helmet' that looks fine on any soldier.")]
    public EquipmentItem fallbackHelmet;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the default helmet for the given <paramref name="armor"/>.
    ///
    /// Lookup order:
    ///   1. Entry whose armor field matches <paramref name="armor"/>
    ///      and whose defaultHelmet is non-null.
    ///   2. <see cref="fallbackHelmet"/> (any armor with no matching entry,
    ///      or a matching entry with a null defaultHelmet).
    ///   3. null  (both paths failed — SoldierDragDrop logs a warning).
    ///
    /// Passing null for <paramref name="armor"/> (soldier has no armor) goes
    /// straight to step 2.
    /// </summary>
    public EquipmentItem GetDefaultHelmet(EquipmentItem armor)
    {
        if (entries != null && armor != null)
        {
            foreach (var entry in entries)
            {
                if (entry.armor != armor) continue;
                if (entry.defaultHelmet != null)
                    return entry.defaultHelmet;

                // Entry found but defaultHelmet unset → fall through to fallback.
                break;
            }
        }

        return fallbackHelmet;   // may be null; caller handles that case
    }

    // ── Editor validation ─────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning($"[ArmorHelmetTable] '{name}' has no entries. " +
                             "Add one row per armor variation in the Inspector.", this);
        }

        if (fallbackHelmet == null)
        {
            Debug.LogWarning($"[ArmorHelmetTable] '{name}' has no fallbackHelmet. " +
                             "Set a generic helmet so soldiers without matching armor " +
                             "still get one on mount.", this);
        }

        // Check for duplicate armor entries (would silently pick the first match).
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            for (int j = i + 1; j < entries.Length; j++)
            {
                if (entries[i].armor != null &&
                    entries[i].armor == entries[j].armor)
                {
                    Debug.LogWarning($"[ArmorHelmetTable] '{name}' has duplicate armor " +
                                     $"entry for '{entries[i].armor.itemName}' " +
                                     $"at indices {i} and {j}. Only the first will be used.", this);
                }
            }
        }
    }
#endif
}

// ─── Supporting data type ─────────────────────────────────────────────────────

/// <summary>
/// One row in ArmorHelmetTable — pairs an armor EquipmentItem with
/// the default helmet that should be auto-equipped when that armor is
/// worn by a soldier who mounts a dragon without a helmet.
/// </summary>
[Serializable]
public class ArmorHelmetEntry
{
    [Tooltip("The armor EquipmentItem this entry covers.\n" +
             "Drag the armor asset from your Project window here.")]
    public EquipmentItem armor;

    [Tooltip("The helmet auto-equipped when the soldier wears this armor " +
             "but has no helmet selected.\n\n" +
             "Leave blank to fall through to ArmorHelmetTable.fallbackHelmet.")]
    public EquipmentItem defaultHelmet;
}