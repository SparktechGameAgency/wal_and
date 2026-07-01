using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// One shared set of unit prefabs — used for BOTH the player and bot sides.
/// Assign these once on BattleManager; BotArmyGenerator receives the same
/// references at Start(), so there is only ever one prefab per unit type
/// in the whole project.
/// </summary>
[System.Serializable]
public class BattleUnitPrefabs
{
    public GameObject soldierPrefab;
    public GameObject archerPrefab;
    public GameObject dragonPrefab;  // dragon with rider visuals wired in

    [Tooltip("All 3 HorseData assets (e.g. Brown/Black/White). Each one's own " +
             ".prefab field is used directly — no separate battle-only horse prefabs needed.")]
    public HorseData[] horseTypes;

    [Tooltip("All 3 CannonData assets. Each one's own .prefab field is used " +
             "directly — no separate battle-only cannon prefabs needed.")]
    public CannonData[] cannonTypes;
}

/// <summary>
/// Pool of equipment items the bot side picks randomly from when spawning a
/// Horse or Dragon unit, so bot riders look varied instead of either bare
/// or an exact clone of the player's own soldier.
/// </summary>
[System.Serializable]
public class RiderLoadoutPool
{
    public EquipmentItem[] faces;
    public EquipmentItem[] armors;
    public EquipmentItem[] helmets;
    public EquipmentItem[] weapons;
}

/// <summary>
/// BattleManager
///
/// Master controller for the Battle scene. Place on a single empty
/// GameObject called "BattleManager" in the Battle scene.
///
/// Responsibilities:
///   • Reads BattleSaveData and spawns the player army (left side).
///   • Tells BotCastleGenerator and BotArmyGenerator to build the right side.
///   • Provides FindNearestEnemy() so BattleUnit.Update can target.
///   • Tracks alive units and declares Win / Lose when one side is gone.
///   • Handles the Win/Lose panel UI and the Return button.
///
/// ════════════════════════════════════════════════════════════════════════
///  REQUIRED HIERARCHY in Battle scene Canvas
/// ════════════════════════════════════════════════════════════════════════
///
///  Canvas
///  ├── PlayerSide              ← RectTransform, anchored left half
///  │   ├── PlayerCastleRoot    ← BotCastleGenerator (flipHorizontally OFF)
///  │   └── PlayerArmyRoot      ← player units spawn here
///  ├── BotSide                 ← RectTransform, anchored right half
///  │   ├── BotCastleRoot       ← BotCastleGenerator (flipHorizontally ON)
///  │   └── BotArmyRoot         ← BotArmyGenerator
///  └── ResultPanel             ← starts inactive
///      ├── WinText
///      ├── LoseText
///      └── ReturnButton
///
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────────────────

    [Header("Player Side")]
    [SerializeField] private Transform playerArmyRoot;
    [Tooltip("BotCastleGenerator on PlayerCastleRoot — flipHorizontally OFF, " +
             "builds the exact player block count (no randomization).")]
    [SerializeField] private BotCastleGenerator playerCastleGenerator;
    [Tooltip("Shared prefabs — also passed to BotArmyGenerator for the bot side.")]
    [SerializeField] private BattleUnitPrefabs unitPrefabs;
    [Tooltip("Random rider looks the bot side picks from for Horse/Dragon units.")]
    [SerializeField] private RiderLoadoutPool botRiderLoadouts;

    [Header("Spawn Layout (Player)")]
    [SerializeField] private float playerStartX = -400f;
    [SerializeField] private float playerUnitSpacingX = 80f;
    [SerializeField] private float playerUnitSpacingY = 60f;
    [SerializeField] private int playerUnitsPerRow = 3;

    [Header("Bot Side")]
    [SerializeField] private BotCastleGenerator botCastleGenerator;
    [SerializeField] private BotArmyGenerator botArmyGenerator;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;

    [Header("Scene")]
    [SerializeField] private string villageSceneName = "Village";

    // ── State ─────────────────────────────────────────────────────────────────

    private List<BattleUnit> _playerUnits = new List<BattleUnit>();
    private List<BattleUnit> _botUnits = new List<BattleUnit>();
    private bool _battleOver;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void Start()
    {
        SpawnPlayerArmy();

        // Player castle — exact block count, gate facing right (not flipped).
        playerCastleGenerator?.GenerateExact(BattleSaveData.PlayerBlockCount);

        // Bot side — castle first, then army.
        // Reuses the SAME unitPrefabs as the player (no separate bot prefabs).
        botCastleGenerator?.Generate(BattleSaveData.PlayerBlockCount);
        botArmyGenerator?.Generate(
            BattleSaveData.PlayerUnits.Count,
            unitPrefabs,
            botRiderLoadouts);

        // Collect bot units after generation.
        if (botArmyGenerator != null)
            _botUnits.AddRange(botArmyGenerator.SpawnedUnits);
    }

    // ── Player Army Spawning ──────────────────────────────────────────────────

    private void SpawnPlayerArmy()
    {
        if (playerArmyRoot == null)
        {
            Debug.LogError("[BattleManager] playerArmyRoot not assigned!");
            return;
        }

        for (int i = 0; i < BattleSaveData.PlayerUnits.Count; i++)
        {
            BattleUnitData data = BattleSaveData.PlayerUnits[i];
            GameObject prefab = GetPrefabFor(data);
            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, playerArmyRoot);

            // Grid layout — left to right, then upward.
            int col = i % playerUnitsPerRow;
            int row = i / playerUnitsPerRow;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                playerStartX + col * playerUnitSpacingX,
                row * playerUnitSpacingY);

            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu != null)
            {
                bu.Init(data, playerUnit: true);
                _playerUnits.Add(bu);
            }
        }

        Debug.Log($"[BattleManager] Spawned {_playerUnits.Count} player units.");
    }

    /// <summary>
    /// Resolves the correct prefab for a unit. Cannons use their own
    /// CannonData.prefab (one of the 3 types); everything else comes from
    /// the shared unitPrefabs set.
    /// </summary>
    private GameObject GetPrefabFor(BattleUnitData data)
    {
        if (data.unitType == BattleUnitType.Cannon)
            return data.cannonType != null ? data.cannonType.prefab : null;

        if (data.unitType == BattleUnitType.Horse)
            return data.horseType != null ? data.horseType.prefab : null;

        return data.unitType switch
        {
            BattleUnitType.Soldier => unitPrefabs.soldierPrefab,
            BattleUnitType.Archer => unitPrefabs.archerPrefab,
            BattleUnitType.Dragon => unitPrefabs.dragonPrefab,
            _ => unitPrefabs.soldierPrefab,
        };
    }

    // ── Enemy Targeting ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by each BattleUnit every frame to find its nearest living enemy.
    /// </summary>
    public BattleUnit FindNearestEnemy(BattleUnit asker)
    {
        List<BattleUnit> enemies = asker.isPlayerUnit ? _botUnits : _playerUnits;

        BattleUnit closest = null;
        float closestDist = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            float d = Mathf.Abs(e.RectPos.x - asker.RectPos.x);
            if (d < closestDist)
            {
                closestDist = d;
                closest = e;
            }
        }

        return closest;
    }

    // ── Death Tracking ────────────────────────────────────────────────────────

    public void OnUnitDied(BattleUnit unit)
    {
        if (_battleOver) return;

        // Check if an entire side has been wiped.
        bool playerAlive = HasAliveUnit(_playerUnits);
        bool botAlive = HasAliveUnit(_botUnits);

        if (!playerAlive || !botAlive)
            StartCoroutine(EndBattle(!playerAlive ? false : true));
    }

    private bool HasAliveUnit(List<BattleUnit> list)
    {
        foreach (var u in list)
            if (u != null && !u.IsDead)
                return true;
        return false;
    }

    // ── Win / Lose ────────────────────────────────────────────────────────────

    private IEnumerator EndBattle(bool playerWon)
    {
        _battleOver = true;

        // Brief pause so the final blow lands visually.
        yield return new WaitForSeconds(1f);

        if (resultPanel != null) resultPanel.SetActive(true);
        if (winText != null) winText.SetActive(playerWon);
        if (loseText != null) loseText.SetActive(!playerWon);

        Debug.Log($"[BattleManager] Battle over — player {(playerWon ? "WON" : "LOST")}.");
    }

    // ── Result Panel Button ───────────────────────────────────────────────────

    /// <summary>Wire the Return button's OnClick → BattleManager.OnReturnClicked.</summary>
    public void OnReturnClicked()
    {
        SceneManager.LoadScene(villageSceneName);
    }
}