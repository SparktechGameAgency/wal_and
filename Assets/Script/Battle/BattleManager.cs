using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
///  │   ├── PlayerCastleRoot    ← BotCastleGenerator (re-used for player visuals)
///  │   └── PlayerArmyRoot      ← player units spawn here
///  ├── BotSide                 ← RectTransform, anchored right half
///  │   ├── BotCastleRoot       ← BotCastleGenerator
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
    [Tooltip("Prefab for each player unit type. Index matches BattleUnitType enum.")]
    [SerializeField] private GameObject playerSoldierPrefab;
    [SerializeField] private GameObject playerMountedPrefab;
    [SerializeField] private GameObject playerArcherPrefab;
    [SerializeField] private GameObject playerDragonPrefab;
    [SerializeField] private GameObject playerCannonPrefab;

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

        // Bot side — castle first, then army.
        botCastleGenerator?.Generate(BattleSaveData.PlayerBlockCount);
        botArmyGenerator?.Generate(BattleSaveData.PlayerUnits.Count);

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
            GameObject prefab = GetPlayerPrefab(data.unitType);
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

    private GameObject GetPlayerPrefab(BattleUnitType type)
    {
        return type switch
        {
            BattleUnitType.Soldier => playerSoldierPrefab,
            BattleUnitType.MountedSoldier => playerMountedPrefab,
            BattleUnitType.Archer => playerArcherPrefab,
            BattleUnitType.Dragon => playerDragonPrefab,
            BattleUnitType.Cannon => playerCannonPrefab,
            _ => playerSoldierPrefab,
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