using UnityEngine;

/// <summary>
/// CastleDoor
///
/// Marks the "last castle grid" doorway on one floor (grid row) of a
/// castle. A CastleDoor lives as a CHILD of the castleBlockPrefab itself
/// (added directly on that prefab in the Editor, start it SetActive(false)
/// there) — CastleGrid.PlaceBlockAt (player castle) and
/// BotCastleGenerator.BuildGridCells (bot castle) both re-enable it only on
/// the block sitting at COLUMN 0 of each occupied row (grid_0_0, grid_1_0,
/// grid_2_0, ...) and leave it disabled everywhere else. Leave the block
/// prefab without a CastleDoor child at all to skip door generation
/// entirely — soldiers keep the old straight-line WorldX walk (fails safe,
/// same convention as every other optional reference in this project).
///
/// A climbing BattleUnit (Soldier only — see BattleUnit.Update /
/// ApproachDoor / ClimbThroughDoor) treats the SAME CastleDoor instance two
/// ways depending on which direction it's being used:
///   • As the point it walks to and plays `enterFrames` at when leaving the
///     floor below (this floor's door = "the last castle grid door" of
///     that lower floor).
///   • As the point it reappears at and plays `exitFrames` from when it
///     has just climbed up from the floor below ("come through the last
///     castle grid of the [next] floor").
///
/// There's deliberately no link to a "next floor" door here — BattleUnit
/// just asks BattleManager for whatever floor it needs next
/// (currentFloor, then currentFloor + 1), so climbing naturally repeats
/// floor-by-floor for as many floors as the castle actually has.
///
/// Wire armorFrames (one entry per armor variation) directly on the door
/// PREFAB in the Inspector — every instance spawned from it (one per floor)
/// shares the same table. BattleUnit reads the climbing soldier's currently
/// equipped armor and calls GetFrames() to pick the matching enter/exit
/// sequence. There is no fallback — every armor variation the soldier can
/// wear needs its own entry, or that soldier plays no frames for the door
/// it's missing an entry for.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CastleDoor : MonoBehaviour
{
    [Header("Door Animation Per Armor (one entry per armor variation)")]
    [Tooltip("Add one entry per armor variation (you have 6) so the door plays the " +
             "climbing frames that actually match what the soldier is wearing.\n\n" +
             "Entries[0]  armor = IronArmor     enter/exitFrames = Iron soldier climbing\n" +
             "Entries[1]  armor = SteelArmor    enter/exitFrames = Steel soldier climbing\n" +
             "...and so on — same one-row-per-armor convention as ArmorHelmetTable.\n\n" +
             "Every armor a soldier can wear needs its own entry here — there is no " +
             "fallback, so a soldier whose armor has no matching entry plays no frames.")]
    public ArmorDoorFrames[] armorFrames;

    [Tooltip("Seconds each frame is held for.")]
    public float frameInterval = 0.08f;

    /// <summary>
    /// Looks up the enter/exit frame sequences that match <paramref name="armor"/>.
    /// No fallback — if no entry's armor matches, both out arrays are null and
    /// PlayDoorFrames simply no-ops (soldier pauses in place for that transition).
    /// </summary>
    public void GetFrames(EquipmentItem armor, out Sprite[] enter, out Sprite[] exit)
    {
        enter = null;
        exit = null;

        if (armorFrames == null || armor == null) return;

        foreach (var e in armorFrames)
        {
            if (e.armor != armor) continue;
            enter = e.enterFrames;
            exit = e.exitFrames;
            return;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (armorFrames == null) return;
        for (int i = 0; i < armorFrames.Length; i++)
        {
            for (int j = i + 1; j < armorFrames.Length; j++)
            {
                if (armorFrames[i].armor != null && armorFrames[i].armor == armorFrames[j].armor)
                {
                    Debug.LogWarning($"[CastleDoor] '{name}' has duplicate armor entry for " +
                                     $"'{armorFrames[i].armor.itemName}' at indices {i} and {j}. " +
                                     "Only the first will be used.", this);
                }
            }
        }
    }
#endif

    /// <summary>Which grid row (floor) this door belongs to. Set by whichever generator spawns it.</summary>
    public int Row { get; private set; }

    /// <summary>Call once right after Instantiate — see BotCastleGenerator.BuildGridCells.</summary>
    public void Init(int row) => Row = row;

    private RectTransform _rt;
    private Canvas _canvas;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Screen-space X — same WorldToScreenPoint convention BattleUnit.WorldX
    /// already uses, so a climbing soldier can compare distance to this door
    /// exactly like it compares distance to an enemy BattleUnit, regardless
    /// of Canvas scale or how deeply this door is nested.
    /// </summary>
    public float WorldX
    {
        get
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;
            return RectTransformUtility.WorldToScreenPoint(cam, _rt.position).x;
        }
    }
}

// ─── Supporting data type ─────────────────────────────────────────────────────

/// <summary>
/// One row in CastleDoor.armorFrames — pairs an armor EquipmentItem with the
/// enter/exit frame sequences that should play when a soldier wearing that
/// armor climbs through this door. Same pattern as ArmorHelmetEntry.
/// </summary>
[System.Serializable]
public class ArmorDoorFrames
{
    [Tooltip("The armor EquipmentItem this entry covers. Drag the armor asset " +
             "from your Project window here.")]
    public EquipmentItem armor;

    [Tooltip("Frames the SOLDIER plays while entering this door (disappearing " +
             "into the castle from the floor below), for a soldier wearing this armor.")]
    public Sprite[] enterFrames;

    [Tooltip("Frames the SOLDIER plays when it reappears at this SAME door, having " +
             "just climbed up from the floor below, for a soldier wearing this armor.")]
    public Sprite[] exitFrames;
}