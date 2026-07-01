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
            BattleSaveData.PlayerUnits.Add(new BattleUnitData(
                BattleUnitType.Archer,
                archerHP, archerDmg, archerSpeed));
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