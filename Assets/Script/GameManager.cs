//using UnityEngine;

///// <summary>
///// AREA FORGE - GameManager (Singleton)
///// Core game state manager. Designed for easy multiplayer expansion (Photon/Mirror).
///// MULTIPLAYER NOTE: In multiplayer, this will sync game state via NetworkManager.
///// </summary>
//public class GameManager : MonoBehaviour
//{
//    // ─── Singleton ────────────────────────────────────────────────────────────
//    public static GameManager Instance { get; private set; }

//    // ─── Game State ───────────────────────────────────────────────────────────
//    public enum GameState { Village, Army, Cannon, Castle, Settings }
//    public GameState CurrentState { get; private set; } = GameState.Village;

//    /// <summary>
//    /// True while the Settings overlay is open.
//    /// Village panel stays active underneath — only the overlay toggles.
//    /// </summary>
//    public bool IsSettingsOpen { get; private set; } = false;

//    // ─── References ───────────────────────────────────────────────────────────
//    [Header("Panel References")]
//    [SerializeField] private GameObject villagePanel;
//    [SerializeField] private GameObject armyPanel;
//    [SerializeField] private GameObject cannonPanel;
//    [SerializeField] private GameObject castlePanel;
//    [SerializeField] private GameObject settingsPanel;

//    [Header("Village")]
//    [Tooltip("The Transform inside the Village Panel where soldiers will be spawned. " +
//             "Its parent must be the Village Panel itself (or any canvas child) so " +
//             "the instantiated soldier becomes part of the correct hierarchy.")]
//    [SerializeField] private Transform villageSpawnPoint;   // Where soldiers spawn in village
//    [SerializeField] private GameObject basicSoldierPrefab; // Basic soldier prefab

//    // ─── Events (subscribe from UI, HUD, etc.) ────────────────────────────────
//    public static event System.Action<GameState> OnGameStateChanged;
//    public static event System.Action<GameObject> OnSoldierSpawned;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//    private void Awake()
//    {
//        // Singleton enforcement
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(gameObject); // Persist across scenes
//    }

//    private void Start()
//    {
//        // Start in village — all other panels hidden
//        ShowVillagePanel();
//    }

//    // ─── Panel Navigation ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by ALL "Close" / "Back" buttons.
//    /// • If Settings overlay is open  → close only the overlay, village stays visible.
//    /// • Otherwise                    → hide the current full panel, show Village.
//    /// </summary>
//    public void CloseCurrentPanel()
//    {
//        if (IsSettingsOpen)
//        {
//            CloseSettingsOverlay();
//            return;
//        }
//        ShowVillagePanel();
//    }

//    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
//    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
//    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

//    /// <summary>
//    /// Settings slides over the Village panel — Village stays active underneath.
//    /// Can be called from any state; the overlay stacks on top.
//    /// </summary>
//    public void OpenSettingsPanel()
//    {
//        if (IsSettingsOpen) return;
//        IsSettingsOpen = true;
//        SetPanelVisible(settingsPanel, true);
//        OnGameStateChanged?.Invoke(GameState.Settings);
//    }

//    /// <summary>
//    /// Closes the Settings overlay without touching the panel beneath it.
//    /// </summary>
//    public void CloseSettingsOverlay()
//    {
//        if (!IsSettingsOpen) return;
//        IsSettingsOpen = false;
//        SetPanelVisible(settingsPanel, false);
//        OnGameStateChanged?.Invoke(CurrentState); // re-broadcast the underlying state
//    }

//    private void ShowVillagePanel()
//    {
//        SetActivePanel(GameState.Village);
//    }

//    /// <summary>
//    /// Switches between the full-screen panels (Village / Army / Cannon / Castle).
//    /// Settings is NOT handled here — it's an overlay (see OpenSettingsPanel).
//    /// MULTIPLAYER NOTE: panels are local UI only, never synced over the network.
//    /// </summary>
//    private void SetActivePanel(GameState newState)
//    {
//        // Close settings overlay silently whenever we navigate away
//        if (IsSettingsOpen)
//        {
//            IsSettingsOpen = false;
//            SetPanelVisible(settingsPanel, false);
//        }

//        // Hide all full-screen panels
//        SetPanelVisible(villagePanel, false);
//        SetPanelVisible(armyPanel, false);
//        SetPanelVisible(cannonPanel, false);
//        SetPanelVisible(castlePanel, false);

//        CurrentState = newState;

//        switch (newState)
//        {
//            case GameState.Village: SetPanelVisible(villagePanel, true); break;
//            case GameState.Army: SetPanelVisible(armyPanel, true); break;
//            case GameState.Cannon: SetPanelVisible(cannonPanel, true); break;
//            case GameState.Castle: SetPanelVisible(castlePanel, true); break;
//        }

//        OnGameStateChanged?.Invoke(newState);
//    }

//    private void SetPanelVisible(GameObject panel, bool visible)
//    {
//        if (panel != null)
//            panel.SetActive(visible);
//    }

//    // ─── Soldier Spawning ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Spawns a basic soldier inside the Village panel hierarchy.
//    ///
//    /// FIX: The soldier is now instantiated as a child of the villageSpawnPoint's
//    /// parent (i.e. the Village Panel itself).  Without a parent the soldier was
//    /// created at the scene root in world-space and appeared outside the canvas.
//    ///
//    /// Inspector setup:
//    ///   • villageSpawnPoint  → an empty GameObject placed INSIDE the Village Panel.
//    ///     Position it where you want the soldier to appear (e.g. centre of the panel).
//    ///   • basicSoldierPrefab → your soldier prefab with SoldierStats + SoldierController.
//    ///
//    /// MULTIPLAYER NOTE: Replace Instantiate with PhotonNetwork.Instantiate() or
//    /// NetworkServer.Spawn() for multiplayer. Only the host/server should spawn.
//    /// </summary>
//    public void SpawnBasicSoldier()
//    {
//        if (basicSoldierPrefab == null)
//        {
//            Debug.LogError("[GameManager] basicSoldierPrefab is not assigned!");
//            return;
//        }

//        if (villageSpawnPoint == null)
//        {
//            Debug.LogError("[GameManager] villageSpawnPoint is not assigned! " +
//                           "Create an empty GameObject inside the Village Panel and assign it.");
//            return;
//        }

//        // ── FIX: parent = villageSpawnPoint.parent so the soldier is spawned ──
//        // ── inside the Village Panel, not at the scene root.                  ──
//        //
//        // We use the spawn point's PARENT (the panel) as the container, and the
//        // spawn point's world position as the starting location.  This way you can
//        // move the spawn marker around freely without the soldier being a child of
//        // the marker itself (which would make it move with the marker).
//        Transform spawnParent = villageSpawnPoint.parent != null
//            ? villageSpawnPoint.parent
//            : villageSpawnPoint;   // fallback: use the point itself as parent

//        Vector3 spawnPos = villageSpawnPoint.position;

//        GameObject soldier = Instantiate(
//            basicSoldierPrefab,
//            spawnPos,
//            Quaternion.identity,
//            spawnParent          // <── THIS is the critical fix
//        );

//        // MULTIPLAYER NOTE: After NetworkServer.Spawn(), call an RPC to
//        // initialize stats on all clients.
//        OnSoldierSpawned?.Invoke(soldier);

//        Debug.Log($"[GameManager] Basic soldier spawned at {spawnPos} " +
//                  $"under parent '{spawnParent.name}'.");
//    }
//}

using UnityEngine;

/// <summary>
/// AREA FORGE - GameManager (Singleton)
/// Core game state manager. Designed for easy multiplayer expansion (Photon/Mirror).
/// MULTIPLAYER NOTE: In multiplayer, this will sync game state via NetworkManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── Game State ───────────────────────────────────────────────────────────
    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
    public GameState CurrentState { get; private set; } = GameState.Village;

    /// <summary>True while the Settings overlay is open.</summary>
    public bool IsSettingsOpen { get; private set; } = false;

    /// <summary>
    /// True while the Horse panel overlay is open.
    /// Village panel stays active underneath — only the overlay toggles.
    /// </summary>
    public bool IsHorsePanelOpen { get; private set; } = false;

    // ─── References ───────────────────────────────────────────────────────────
    [Header("Panel References")]
    [SerializeField] private GameObject villagePanel;
    [SerializeField] private GameObject armyPanel;
    [SerializeField] private GameObject cannonPanel;
    [SerializeField] private GameObject castlePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Village Overlays")]
    [Tooltip("Drag HorsePanle here — it lives ON TOP of the Village panel.")]
    [SerializeField] private GameObject horsePanel;

    [Header("Village")]
    [SerializeField] private Transform villageSpawnPoint;
    [SerializeField] private GameObject basicSoldierPrefab;

    // ─── Events ───────────────────────────────────────────────────────────────
    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action<GameObject> OnSoldierSpawned;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ShowVillagePanel();
    }

    // ─── Panel Navigation ─────────────────────────────────────────────────────

    /// <summary>
    /// Called by ALL "Close" / "Back" buttons.
    /// Priority: Horse overlay → Settings overlay → return to Village.
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }
        ShowVillagePanel();
    }

    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

    // ─── Settings overlay ─────────────────────────────────────────────────────

    public void OpenSettingsPanel()
    {
        if (IsSettingsOpen) return;
        IsSettingsOpen = true;
        SetPanelVisible(settingsPanel, true);
        OnGameStateChanged?.Invoke(GameState.Settings);
    }

    public void CloseSettingsOverlay()
    {
        if (!IsSettingsOpen) return;
        IsSettingsOpen = false;
        SetPanelVisible(settingsPanel, false);
        OnGameStateChanged?.Invoke(CurrentState);
    }

    // ─── Horse panel overlay ──────────────────────────────────────────────────

    /// <summary>
    /// Opens the Horse panel overlay while keeping the Village panel visible.
    /// Called by HorsePanelManager after it has set up which slot was clicked.
    /// </summary>
    public void OpenHorsePanel()
    {
        if (IsHorsePanelOpen) return;

        // Close Settings overlay if it was open (can't have two overlays at once)
        if (IsSettingsOpen) CloseSettingsOverlay();

        IsHorsePanelOpen = true;
        SetPanelVisible(horsePanel, true);
        OnGameStateChanged?.Invoke(GameState.Horse);
    }

    /// <summary>
    /// Closes the Horse panel overlay, returns to the underlying state.
    /// </summary>
    public void CloseHorsePanel()
    {
        if (!IsHorsePanelOpen) return;
        IsHorsePanelOpen = false;
        SetPanelVisible(horsePanel, false);
        OnGameStateChanged?.Invoke(CurrentState); // re-broadcast underlying state
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

    private void SetActivePanel(GameState newState)
    {
        // Silently close any overlays when navigating between full panels
        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

        SetPanelVisible(villagePanel, false);
        SetPanelVisible(armyPanel, false);
        SetPanelVisible(cannonPanel, false);
        SetPanelVisible(castlePanel, false);

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Village: SetPanelVisible(villagePanel, true); break;
            case GameState.Army: SetPanelVisible(armyPanel, true); break;
            case GameState.Cannon: SetPanelVisible(cannonPanel, true); break;
            case GameState.Castle: SetPanelVisible(castlePanel, true); break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    private void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    // ─── Soldier Spawning ─────────────────────────────────────────────────────

    public void SpawnBasicSoldier()
    {
        if (basicSoldierPrefab == null) { Debug.LogError("[GameManager] basicSoldierPrefab not assigned!"); return; }
        if (villageSpawnPoint == null) { Debug.LogError("[GameManager] villageSpawnPoint not assigned!"); return; }

        Transform spawnParent = villageSpawnPoint.parent != null ? villageSpawnPoint.parent : villageSpawnPoint;

        GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, spawnParent);
        OnSoldierSpawned?.Invoke(soldier);

        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
    }
}