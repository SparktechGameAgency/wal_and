using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BattleStarter
///
/// Attach to the "Start Battle" button's GameObject in the Village panel,
/// then wire the button's OnClick → BattleStarter.OnStartBattleClicked().
/// </summary>
public class BattleStarter : MonoBehaviour
{
    [Tooltip("Exact name of the Battle scene as registered in Build Settings.")]
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("Default Unit Stats")]
    [SerializeField] private float soldierHP = 100f;
    [SerializeField] private float soldierDmg = 10f;
    [SerializeField] private float soldierSpeed = 80f;
    [SerializeField] private float mountedHP = 150f;
    [SerializeField] private float mountedDmg = 15f;
    [SerializeField] private float mountedSpeed = 120f;
    [SerializeField] private float archerHP = 80f;
    [SerializeField] private float archerDmg = 12f;
    [SerializeField] private float archerSpeed = 70f;
    [SerializeField] private float dragonHP = 300f;
    [SerializeField] private float dragonDmg = 40f;
    [SerializeField] private float dragonSpeed = 150f;

    public void OnStartBattleClicked()
    {
        BattleSaveData.Clear();
        GatherArmyData();
        CarryCastleIntoBattle();
        CarrySoldiersIntoBattle();
        CarryDragonsIntoBattle();
        SceneManager.LoadScene(battleSceneName);
    }

    /// <summary>
    /// Detaches the live CastleGridPanel and flags it to survive the scene
    /// load, so the ACTUAL castle (blocks, cannons, archers, all of it)
    /// shifts into the Battle scene instead of a data-driven rebuild.
    /// BattleManager reparents it into PlayerCastleRoot on the other side.
    /// </summary>
    private void CarryCastleIntoBattle()
    {
        if (CastleGrid.Instance == null)
        {
            Debug.LogWarning("[BattleStarter] No CastleGrid.Instance found — castle will not carry over.");
            return;
        }

        GameObject panel = CastleGrid.Instance.gameObject;
        Debug.Log($"[BattleStarter] Carrying '{panel.name}' into battle. " +
                  $"Parent before detach: {(panel.transform.parent != null ? panel.transform.parent.name : "none")}");

        CastleGrid.Instance.SetBattleMode(true);
        CastleGrid.Instance.PrepareForSceneCarry();

        Debug.Log($"[BattleStarter] '{panel.name}' detached. Parent after detach: " +
                  $"{(panel.transform.parent != null ? panel.transform.parent.name : "none (root)")}");
    }

    /// <summary>
    /// Detaches the live SoldierSpawnArea (the same container every village
    /// soldier is instantiated into and reparented between Village/Castle
    /// panels — see CastleGridMover) and flags it to survive the scene load,
    /// exactly like CarryCastleIntoBattle() does for the CastleGrid. This
    /// carries the ACTUAL soldier GameObjects — equipment, animation state
    /// and all — into the Battle scene instead of a data-driven respawn.
    /// BattleManager reparents it into PlayerSide/PlayerArmyRoot on the
    /// other side (ReceivePlayerSoldiers()).
    /// </summary>
    private void CarrySoldiersIntoBattle()
    {
        RectTransform spawnArea = CastleGridMover.Instance != null
            ? CastleGridMover.Instance.soldierSpawnArea
            : null;

        if (spawnArea == null)
        {
            Debug.LogWarning("[BattleStarter] No SoldierSpawnArea found — foot soldiers will not carry over.");
            return;
        }

        Debug.Log($"[BattleStarter] Carrying '{spawnArea.name}' into battle. " +
                  $"Parent before detach: {(spawnArea.parent != null ? spawnArea.parent.name : "none")}.");

        // DontDestroyOnLoad only works on root GameObjects, so unparent first —
        // same pattern as CastleGrid.PrepareForSceneCarry().
        spawnArea.SetParent(null, true);
        DontDestroyOnLoad(spawnArea.gameObject);

        // CastleGridMover itself does NOT survive the scene change (only
        // CastleGrid and this SoldierSpawnArea do), so its static Instance
        // goes null once the Battle scene loads. Stash the reference on the
        // plain-static BattleSaveData instead so BattleManager can still find
        // the carried GameObject on the other side.
        BattleSaveData.CarriedSoldierSpawnArea = spawnArea;

        Debug.Log($"[BattleStarter] '{spawnArea.name}' detached and marked to survive the scene load.");
    }

    /// <summary>
    /// Detaches every live FlyZone that currently has a mounted dragon in it
    /// and flags it to survive the scene load — same DontDestroyOnLoad
    /// pattern as CarryCastleIntoBattle()/CarrySoldiersIntoBattle(). This
    /// carries the ACTUAL FlyZone GameObject, with the real dragon (equipment,
    /// rider visuals, everything) still parented inside it, into the Battle
    /// scene instead of spawning a fresh prefab copy there.
    ///
    /// An empty FlyZone (no dragon, or an unmounted one) is left behind in
    /// the Village exactly as it is — only occupied ones travel.
    /// </summary>
    private void CarryDragonsIntoBattle()
    {
        var flyZones = FindObjectsByType<FlyZone>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var zone in flyZones)
        {
            DragonController dragon = zone.GetComponentInChildren<DragonController>(true);
            if (dragon == null) continue;
            if (dragon.RiderSeat == null || !dragon.RiderSeat.IsOccupied) continue;

            RectTransform zoneRt = zone.GetComponent<RectTransform>();
            if (zoneRt == null) continue;

            Debug.Log($"[BattleStarter] Carrying FlyZone '{zoneRt.name}' (mounted dragon " +
                      $"'{dragon.name}') into battle.");

            // DontDestroyOnLoad only works on root GameObjects — unparent first,
            // same as CastleGrid.PrepareForSceneCarry() / CarrySoldiersIntoBattle().
            zoneRt.SetParent(null, true);
            DontDestroyOnLoad(zoneRt.gameObject);
            BattleSaveData.CarriedDragonFlyZones.Add(zoneRt);
        }
    }

    private void GatherArmyData()
    {
        // ── Castle shape (exact block positions, not just a count) ──────────────
        BattleSaveData.PlayerBlockPositions = CastleGrid.Instance != null
            ? CastleGrid.Instance.GetPlacedBlockPositions()
            : new System.Collections.Generic.List<Vector2Int>();

        BattleSaveData.PlayerBlockCount = BattleSaveData.PlayerBlockPositions.Count > 0
            ? BattleSaveData.PlayerBlockPositions.Count
            : 1;

        // ── Horses (with a soldier mounted) ─────────────────────────────────────
        var horses = FindObjectsByType<HorseController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var h in horses)
        {
            if (!h.IsOccupied) continue;

            // Pull stats from this horse's own HorseData asset so each of the
            // 3 horse types (different level/ScriptableObject) carries its own
            // HP/Damage/Speed into battle instead of one flat value for all
            // horses. Falls back to the inspector defaults only if Data is
            // somehow unassigned (e.g. horse spawned without Setup()).
            HorseData horseType = h.Data;
            float hp = horseType != null ? horseType.health : mountedHP;
            float dmg = horseType != null ? horseType.damage : mountedDmg;
            // HorseData has no dedicated "speed" stat — ability is the closest
            // per-type stat available, so it doubles as move speed here.
            float speed = horseType != null ? horseType.ability : mountedSpeed;

            var unit = new BattleUnitData(
                BattleUnitType.Horse,
                hp, dmg, speed);
            unit.horseType = horseType;

            // Snapshot the rider's real equipment so the Battle scene shows
            // the same soldier look as the Village scene.
            CharacterEquipment riderEquipment = h.MountedRiderEquipment;
            if (riderEquipment != null)
            {
                unit.riderFace = riderEquipment.GetEquipped(EquipmentSlot.Face);
                unit.riderArmor = riderEquipment.GetEquipped(EquipmentSlot.Armor);
                unit.riderHelmet = riderEquipment.GetEquipped(EquipmentSlot.Helmet);
                unit.riderWeapon = riderEquipment.GetEquipped(EquipmentSlot.Weapon);
            }

            BattleSaveData.PlayerUnits.Add(unit);
        }

        // ── Foot soldiers ─────────────────────────────────────────────────────
        // SoldierController has no mounted flag, so we count every active
        // SoldierController that is NOT sitting on a horse.
        // A soldier that is mounted has its GameObject hidden by HorseController
        // (SetActive(false) in EnterRidingState), so activeInHierarchy catches this.
        var soldiers = FindObjectsByType<SoldierController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var s in soldiers)
        {
            // Mounted soldiers are deactivated by HorseController — skip them.
            if (!s.gameObject.activeInHierarchy) continue;

            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.Soldier,
                soldierHP, soldierDmg, soldierSpeed));
        }

        // ── Archers (stationed in ArcherZoneCastle slots) ────────────────────
        var archerZones = FindObjectsByType<ArcherZoneCastle>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var az in archerZones)
        {
            if (!az.IsOccupied) continue;

            var archerUnit = new BattleUnitData(
                BattleUnitType.Archer,
                archerHP, archerDmg, archerSpeed);

            // Record which castle block this archer is stationed on so it
            // spawns seated on the matching block in the Battle scene,
            // instead of the flat army row.
            GridCell cell = az.GetComponentInParent<GridCell>();
            if (cell != null)
            {
                archerUnit.hasGridPosition = true;
                archerUnit.gridPosition = new Vector2Int(cell.Row, cell.Col);
            }

            BattleSaveData.PlayerUnits.Add(archerUnit);
        }

        // ── Cannons (placed via CannonSlotCastle / CannonPanelManager) ───────
        var cannonSlots = FindObjectsByType<CannonSlotCastle>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var cs in cannonSlots)
        {
            if (!cs.hasCannon || cs.equippedEntry == null || cs.equippedEntry.data == null) continue;

            var entry = cs.equippedEntry;
            var cannonUnit = new BattleUnitData(
                BattleUnitType.Cannon,
                entry.CurrentHealth, entry.CurrentDamage, 0f);
            cannonUnit.cannonType = entry.data;

            // Record which castle block this cannon is mounted on so it
            // spawns seated on the matching block in the Battle scene,
            // instead of the flat army row.
            GridCell cell = cs.GetComponentInParent<GridCell>();
            if (cell != null)
            {
                cannonUnit.hasGridPosition = true;
                cannonUnit.gridPosition = new Vector2Int(cell.Row, cell.Col);
            }

            BattleSaveData.PlayerUnits.Add(cannonUnit);
        }

        // ── Dragons (with a soldier mounted) ─────────────────────────────────
        var dragons = FindObjectsByType<DragonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        BattleSaveData.DragonCount = 0;
        foreach (var d in dragons)
        {
            if (!d.gameObject.activeInHierarchy) continue;
            if (d.RiderSeat == null || !d.RiderSeat.IsOccupied) continue;

            BattleSaveData.DragonCount++;

            var unit = new BattleUnitData(
                BattleUnitType.Dragon,
                dragonHP, dragonDmg, dragonSpeed);

            CharacterEquipment riderEquipment = d.RiderSeat.MountedSoldier != null
                ? d.RiderSeat.MountedSoldier.GetComponent<CharacterEquipment>()
                : null;
            if (riderEquipment != null)
            {
                unit.riderFace = riderEquipment.GetEquipped(EquipmentSlot.Face);
                unit.riderArmor = riderEquipment.GetEquipped(EquipmentSlot.Armor);
                unit.riderHelmet = riderEquipment.GetEquipped(EquipmentSlot.Helmet);
                unit.riderWeapon = riderEquipment.GetEquipped(EquipmentSlot.Weapon);
            }

            BattleSaveData.PlayerUnits.Add(unit);
        }

        Debug.Log($"[BattleStarter] Packed {BattleSaveData.PlayerUnits.Count} units, " +
                  $"{BattleSaveData.PlayerBlockCount} castle blocks.");
    }
}