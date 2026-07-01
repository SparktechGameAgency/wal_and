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
        SceneManager.LoadScene(battleSceneName);
    }

    private void GatherArmyData()
    {
        // ── Castle block count ────────────────────────────────────────────────
        BattleSaveData.PlayerBlockCount = CastleGrid.Instance != null
            ? CastleGrid.Instance.GetPlacedBlockCount()
            : 1;

        // ── Mounted soldiers (horse with rider) ───────────────────────────────
        // HorseController.IsOccupied is the correct property name.
        var horses = FindObjectsByType<HorseController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var h in horses)
        {
            if (!h.IsOccupied) continue;
            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.MountedSoldier,
                mountedHP, mountedDmg, mountedSpeed));
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
            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.Archer,
                archerHP, archerDmg, archerSpeed));
        }

        // ── Cannons ───────────────────────────────────────────────────────────
        var cannonZones = FindObjectsByType<CastleUnitDropZone>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var cz in cannonZones)
        {
            if (!cz.HasUnit) continue;
            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.Cannon,
                0f, 30f, 0f));
        }

        // ── Dragons ───────────────────────────────────────────────────────────
        var dragons = FindObjectsByType<DragonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        BattleSaveData.DragonCount = 0;
        foreach (var d in dragons)
        {
            if (!d.gameObject.activeInHierarchy) continue;
            BattleSaveData.DragonCount++;
            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.Dragon,
                dragonHP, dragonDmg, dragonSpeed));
        }

        Debug.Log($"[BattleStarter] Packed {BattleSaveData.PlayerUnits.Count} units, " +
                  $"{BattleSaveData.PlayerBlockCount} castle blocks.");
    }
}