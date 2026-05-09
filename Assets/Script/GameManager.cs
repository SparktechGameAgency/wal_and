////using UnityEngine;

/////// <summary>
/////// AREA FORGE - GameManager (Singleton)
/////// Core game state manager. Designed for easy multiplayer expansion (Photon/Mirror).
/////// MULTIPLAYER NOTE: In multiplayer, this will sync game state via NetworkManager.
///////
/////// NOTE: The Army panel IS the Customize / InventoryPanel.
/////// Drag your InventoryPanel GameObject into the armyPanel field in the Inspector.
/////// </summary>
////public class GameManager : MonoBehaviour
////{
////    // ─── Singleton ────────────────────────────────────────────────────────────
////    public static GameManager Instance { get; private set; }

////    // ─── Game State ───────────────────────────────────────────────────────────
////    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
////    public GameState CurrentState { get; private set; } = GameState.Village;

////    /// <summary>True while the Settings overlay is open.</summary>
////    public bool IsSettingsOpen { get; private set; } = false;

////    /// <summary>True while the Horse panel overlay is open.</summary>
////    public bool IsHorsePanelOpen { get; private set; } = false;

////    // ─── References ───────────────────────────────────────────────────────────
////    [Header("Panel References")]
////    [SerializeField] private GameObject villagePanel;

////    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
////    [SerializeField] private GameObject armyPanel;

////    [SerializeField] private GameObject cannonPanel;
////    [SerializeField] private GameObject castlePanel;
////    [SerializeField] private GameObject settingsPanel;

////    [Header("Village Overlays")]
////    [Tooltip("Drag HorsePanel here — it lives ON TOP of the Village panel.")]
////    [SerializeField] private GameObject horsePanel;

////    [Header("Village")]
////    [SerializeField] private Transform villageSpawnPoint;
////    [SerializeField] private GameObject basicSoldierPrefab;

////    // ─── Events ───────────────────────────────────────────────────────────────
////    public static event System.Action<GameState> OnGameStateChanged;
////    public static event System.Action<GameObject> OnSoldierSpawned;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;
////        DontDestroyOnLoad(gameObject);
////    }

////    private void Start()
////    {
////        ShowVillagePanel();
////    }

////    // ─── Panel Navigation ─────────────────────────────────────────────────────

////    /// <summary>
////    /// Called by ALL "Close" / "Back" buttons.
////    /// Priority: Horse overlay → Settings overlay → return to Village.
////    /// </summary>
////    public void CloseCurrentPanel()
////    {
////        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
////        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }
////        ShowVillagePanel();
////    }

////    /// <summary>Opens the Army / Customize (InventoryPanel) panel.</summary>
////    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
////    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
////    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

////    // ─── Settings overlay ─────────────────────────────────────────────────────

////    public void OpenSettingsPanel()
////    {
////        if (IsSettingsOpen) return;
////        IsSettingsOpen = true;
////        SetPanelVisible(settingsPanel, true);
////        OnGameStateChanged?.Invoke(GameState.Settings);
////    }

////    public void CloseSettingsOverlay()
////    {
////        if (!IsSettingsOpen) return;
////        IsSettingsOpen = false;
////        SetPanelVisible(settingsPanel, false);
////        OnGameStateChanged?.Invoke(CurrentState);
////    }

////    // ─── Horse panel overlay ──────────────────────────────────────────────────

////    public void OpenHorsePanel()
////    {
////        if (IsHorsePanelOpen) return;
////        if (IsSettingsOpen) CloseSettingsOverlay();

////        IsHorsePanelOpen = true;
////        SetPanelVisible(horsePanel, true);
////        OnGameStateChanged?.Invoke(GameState.Horse);
////    }

////    public void CloseHorsePanel()
////    {
////        if (!IsHorsePanelOpen) return;
////        IsHorsePanelOpen = false;
////        SetPanelVisible(horsePanel, false);
////        OnGameStateChanged?.Invoke(CurrentState);
////    }

////    // ─── Private helpers ──────────────────────────────────────────────────────

////    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

////    private void SetActivePanel(GameState newState)
////    {
////        // Silently close overlays when navigating between full panels
////        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
////        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

////        SetPanelVisible(villagePanel, false);
////        SetPanelVisible(armyPanel, false);
////        SetPanelVisible(cannonPanel, false);
////        SetPanelVisible(castlePanel, false);

////        CurrentState = newState;

////        switch (newState)
////        {
////            case GameState.Village: SetPanelVisible(villagePanel, true); break;
////            case GameState.Army: SetPanelVisible(armyPanel, true); break;
////            case GameState.Cannon: SetPanelVisible(cannonPanel, true); break;
////            case GameState.Castle: SetPanelVisible(castlePanel, true); break;
////        }

////        OnGameStateChanged?.Invoke(newState);
////    }

////    private void SetPanelVisible(GameObject panel, bool visible)
////    {
////        if (panel != null) panel.SetActive(visible);
////    }

////    // ─── Soldier Spawning ─────────────────────────────────────────────────────

////    //public void SpawnBasicSoldier()
////    //{
////    //    if (basicSoldierPrefab == null) { Debug.LogError("[GameManager] basicSoldierPrefab not assigned!"); return; }
////    //    if (villageSpawnPoint == null) { Debug.LogError("[GameManager] villageSpawnPoint not assigned!"); return; }

////    //    Transform spawnParent = villageSpawnPoint.parent != null
////    //        ? villageSpawnPoint.parent
////    //        : villageSpawnPoint;

////    //    GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, spawnParent);
////    //    OnSoldierSpawned?.Invoke(soldier);

////    //    Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
////    //}


////    public void SpawnBasicSoldier()
////    {
////        if (basicSoldierPrefab == null)
////        {
////            Debug.LogError("[GameManager] basicSoldierPrefab not assigned!");
////            return;
////        }
////        if (villageSpawnPoint == null)
////        {
////            Debug.LogError("[GameManager] villageSpawnPoint not assigned!");
////            return;
////        }

////        Transform spawnParent = villageSpawnPoint.parent != null
////            ? villageSpawnPoint.parent
////            : villageSpawnPoint;

////        GameObject soldier = Instantiate(
////            basicSoldierPrefab,
////            villageSpawnPoint.position,
////            Quaternion.identity,
////            spawnParent);

////        // ── Apply the customization the player chose in the Army panel ────────────
////        InventoryPanel invPanel = armyPanel != null
////            ? armyPanel.GetComponentInChildren<InventoryPanel>(true)
////            : null;

////        CharacterEquipment soldierEquip = soldier.GetComponent<CharacterEquipment>();

////        if (invPanel != null && soldierEquip != null)
////        {
////            invPanel.ApplySelectionToSoldier(soldierEquip);
////            Debug.Log("[GameManager] Customization applied to spawned soldier.");
////        }
////        else
////        {
////            Debug.LogWarning(
////                $"[GameManager] Could NOT apply loadout.\n" +
////                $"  InventoryPanel found: {invPanel != null} " +
////                $"(check armyPanel field or InventoryPanel component)\n" +
////                $"  CharacterEquipment on prefab: {soldierEquip != null} " +
////                $"(add CharacterEquipment to SolderPrefab)");
////        }
////        // ─────────────────────────────────────────────────────────────────────────

////        OnSoldierSpawned?.Invoke(soldier);
////        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
////    }
////}

//using UnityEngine;

///// <summary>
///// AREA FORGE - GameManager (Singleton)
/////
///// SpawnBasicSoldier() just Instantiates and fires OnSoldierSpawned.
///// InventoryPanel listens to OnSoldierSpawned and automatically transfers
///// the user's selected items to the new soldier — no extra call needed here.
///// </summary>
//public class GameManager : MonoBehaviour
//{
//    // ─── Singleton ────────────────────────────────────────────────────────────
//    public static GameManager Instance { get; private set; }

//    // ─── Game State ───────────────────────────────────────────────────────────
//    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
//    public GameState CurrentState { get; private set; } = GameState.Village;

//    public bool IsSettingsOpen   { get; private set; } = false;
//    public bool IsHorsePanelOpen { get; private set; } = false;

//    // ─── References ───────────────────────────────────────────────────────────
//    [Header("Panel References")]
//    [SerializeField] private GameObject villagePanel;

//    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
//    [SerializeField] private GameObject armyPanel;

//    [SerializeField] private GameObject cannonPanel;
//    [SerializeField] private GameObject castlePanel;
//    [SerializeField] private GameObject settingsPanel;

//    [Header("Village Overlays")]
//    [Tooltip("Drag HorsePanel here — it lives ON TOP of the Village panel.")]
//    [SerializeField] private GameObject horsePanel;

//    [Header("Village")]
//    [SerializeField] private Transform  villageSpawnPoint;
//    [SerializeField] private GameObject basicSoldierPrefab;

//    // ─── Events ───────────────────────────────────────────────────────────────
//    public static event System.Action<GameState>  OnGameStateChanged;
//    public static event System.Action<GameObject> OnSoldierSpawned;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    private void Start() => ShowVillagePanel();

//    // ─── Panel Navigation ─────────────────────────────────────────────────────

//    public void CloseCurrentPanel()
//    {
//        if (IsHorsePanelOpen) { CloseHorsePanel();      return; }
//        if (IsSettingsOpen)   { CloseSettingsOverlay(); return; }
//        ShowVillagePanel();
//    }

//    public void OpenArmyPanel()   => SetActivePanel(GameState.Army);
//    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
//    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

//    // ─── Settings Overlay ─────────────────────────────────────────────────────

//    public void OpenSettingsPanel()
//    {
//        if (IsSettingsOpen) return;
//        IsSettingsOpen = true;
//        SetPanelVisible(settingsPanel, true);
//        OnGameStateChanged?.Invoke(GameState.Settings);
//    }

//    public void CloseSettingsOverlay()
//    {
//        if (!IsSettingsOpen) return;
//        IsSettingsOpen = false;
//        SetPanelVisible(settingsPanel, false);
//        OnGameStateChanged?.Invoke(CurrentState);
//    }

//    // ─── Horse Panel Overlay ──────────────────────────────────────────────────

//    public void OpenHorsePanel()
//    {
//        if (IsHorsePanelOpen) return;
//        if (IsSettingsOpen) CloseSettingsOverlay();
//        IsHorsePanelOpen = true;
//        SetPanelVisible(horsePanel, true);
//        OnGameStateChanged?.Invoke(GameState.Horse);
//    }

//    public void CloseHorsePanel()
//    {
//        if (!IsHorsePanelOpen) return;
//        IsHorsePanelOpen = false;
//        SetPanelVisible(horsePanel, false);
//        OnGameStateChanged?.Invoke(CurrentState);
//    }

//    // ─── Private Helpers ──────────────────────────────────────────────────────

//    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

//    private void SetActivePanel(GameState newState)
//    {
//        if (IsSettingsOpen)   { IsSettingsOpen   = false; SetPanelVisible(settingsPanel, false); }
//        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel,    false); }

//        SetPanelVisible(villagePanel, false);
//        SetPanelVisible(armyPanel,    false);
//        SetPanelVisible(cannonPanel,  false);
//        SetPanelVisible(castlePanel,  false);

//        CurrentState = newState;

//        switch (newState)
//        {
//            case GameState.Village: SetPanelVisible(villagePanel, true); break;
//            case GameState.Army:    SetPanelVisible(armyPanel,    true); break;
//            case GameState.Cannon:  SetPanelVisible(cannonPanel,  true); break;
//            case GameState.Castle:  SetPanelVisible(castlePanel,  true); break;
//        }

//        OnGameStateChanged?.Invoke(newState);
//    }

//    private void SetPanelVisible(GameObject panel, bool visible)
//    {
//        if (panel != null) panel.SetActive(visible);
//    }

//    // ─── Soldier Spawning ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Instantiates the soldier and fires OnSoldierSpawned.
//    /// InventoryPanel subscribes to OnSoldierSpawned and automatically
//    /// applies the user's selected items — nothing extra needed here.
//    /// </summary>
//    public void SpawnBasicSoldier()
//    {
//        if (basicSoldierPrefab == null)
//        {
//            Debug.LogError("[GameManager] basicSoldierPrefab not assigned!");
//            return;
//        }
//        if (villageSpawnPoint == null)
//        {
//            Debug.LogError("[GameManager] villageSpawnPoint not assigned!");
//            return;
//        }

//        Transform spawnParent = villageSpawnPoint.parent != null
//            ? villageSpawnPoint.parent
//            : villageSpawnPoint;

//        GameObject soldier = Instantiate(
//            basicSoldierPrefab,
//            villageSpawnPoint.position,
//            Quaternion.identity,
//            spawnParent);

//        // InventoryPanel.OnSoldierSpawned() handles equipment transfer automatically
//        OnSoldierSpawned?.Invoke(soldier);

//        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
//    }
//}

using UnityEngine;

/// <summary>
/// AREA FORGE - GameManager (Singleton)
///
/// SpawnBasicSoldier() just Instantiates and fires OnSoldierSpawned.
/// InventoryPanel listens to OnSoldierSpawned and automatically transfers
/// the user's selected items to the new soldier — no extra call needed here.
///
/// ═══════════════════════════════════════════════════════════════════════
/// PANEL HIERARCHY REQUIREMENT — READ THIS IF BUTTONS DEACTIVATE EVERYTHING
/// ═══════════════════════════════════════════════════════════════════════
/// All five panel GameObjects MUST be at the same sibling level inside the
/// Canvas (or at least siblings of each other).  They must NEVER be children
/// of one another.  Placing cannonPanel inside castlePanel, for example, means
/// SetActivePanel() deactivates the parent (castlePanel) and then tries to
/// re-activate the child — but a child of an inactive parent cannot be seen
/// regardless of its own SetActive state.
///
/// Correct flat structure:
///   Canvas
///   ├── VillagePanel
///   ├── ArmyPanel
///   ├── CannonPanel        ← CannonPanelManager lives here
///   ├── CastlePanel
///   ├── SettingsPanel
///   └── HorsePanel
///
/// ALL six fields below must be assigned in the Inspector.  OnValidate()
/// will print a warning for every unassigned slot while in Edit mode.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ─── Game State ───────────────────────────────────────────────────────────

    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
    public GameState CurrentState { get; private set; } = GameState.Village;

    public bool IsSettingsOpen { get; private set; } = false;
    public bool IsHorsePanelOpen { get; private set; } = false;

    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Panel References  ← ALL must be assigned or buttons will hide everything")]
    [SerializeField] private GameObject villagePanel;

    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
    [SerializeField] private GameObject armyPanel;

    [Tooltip("The root of the Cannon panel.  CannonPanelManager must live on this or a child.")]
    [SerializeField] private GameObject cannonPanel;

    [SerializeField] private GameObject castlePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Village Overlays")]
    [Tooltip("Drag HorsePanel here — it lives ON TOP of the Village panel.")]
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

    private void Start() => ShowVillagePanel();

    // ─── Inspector Validation (Edit-mode only) ────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Warn for every unassigned panel so the developer spots the problem
        // before pressing Play.
        if (villagePanel == null) Debug.LogWarning("[GameManager] villagePanel is not assigned.", this);
        if (armyPanel == null) Debug.LogWarning("[GameManager] armyPanel is not assigned.", this);
        if (cannonPanel == null) Debug.LogWarning("[GameManager] cannonPanel is not assigned. " +
            "Clicking 'Add Cannon' will deactivate everything without re-activating anything!", this);
        if (castlePanel == null) Debug.LogWarning("[GameManager] castlePanel is not assigned.", this);
        if (settingsPanel == null) Debug.LogWarning("[GameManager] settingsPanel is not assigned.", this);
        if (horsePanel == null) Debug.LogWarning("[GameManager] horsePanel is not assigned.", this);
    }
#endif

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
    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

    /// <summary>
    /// Opens the Cannon panel and resets CannonPanelManager to Buy mode.
    /// Wire the "Add Cannon" button in your scene to call THIS method —
    /// do NOT wire it to CannonPanelManager directly.
    /// </summary>
    public void OpenCannonPanel()
    {
        // Guard: tell the developer clearly if the reference is missing.
        if (cannonPanel == null)
        {
            Debug.LogError(
                "[GameManager] OpenCannonPanel() — cannonPanel is not assigned in the Inspector!\n" +
                "All panels were deactivated but nothing was re-activated.\n" +
                "Drag your CannonPanel GameObject into the 'Cannon Panel' slot on GameManager.",
                this);
            return;   // Abort — do not deactivate everything for nothing.
        }

        SetActivePanel(GameState.Cannon);

        // Tell CannonPanelManager to reset to Buy mode.
        // Safe even if CannonPanelManager.Instance is null (first open before Awake fires).
        CannonPanelManager.Instance?.OnPanelOpened();
    }

    // ─── Settings Overlay ─────────────────────────────────────────────────────

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

    // ─── Horse Panel Overlay ──────────────────────────────────────────────────

    public void OpenHorsePanel()
    {
        if (IsHorsePanelOpen) return;
        if (IsSettingsOpen) CloseSettingsOverlay();
        IsHorsePanelOpen = true;
        SetPanelVisible(horsePanel, true);
        OnGameStateChanged?.Invoke(GameState.Horse);
    }

    public void CloseHorsePanel()
    {
        if (!IsHorsePanelOpen) return;
        IsHorsePanelOpen = false;
        SetPanelVisible(horsePanel, false);
        OnGameStateChanged?.Invoke(CurrentState);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

    /// <summary>
    /// Deactivates every panel then activates only the requested one.
    ///
    /// IMPORTANT: all panel references must be siblings in the hierarchy.
    /// If panel B is a child of panel A, deactivating A will hide B even
    /// after calling SetActive(true) on B, because a child of an inactive
    /// parent is never rendered.
    /// </summary>
    private void SetActivePanel(GameState newState)
    {
        // Close any open overlays silently when switching main panels.
        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

        // Hide all main panels first.
        SetPanelVisible(villagePanel, false);
        SetPanelVisible(armyPanel, false);
        SetPanelVisible(cannonPanel, false);
        SetPanelVisible(castlePanel, false);

        CurrentState = newState;

        // Show only the requested panel.
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

    /// <summary>
    /// Instantiates the soldier and fires OnSoldierSpawned.
    /// InventoryPanel subscribes to OnSoldierSpawned and automatically
    /// applies the user's selected items — nothing extra needed here.
    /// </summary>
    public void SpawnBasicSoldier()
    {
        if (basicSoldierPrefab == null)
        {
            Debug.LogError("[GameManager] basicSoldierPrefab not assigned!");
            return;
        }
        if (villageSpawnPoint == null)
        {
            Debug.LogError("[GameManager] villageSpawnPoint not assigned!");
            return;
        }

        Transform spawnParent = villageSpawnPoint.parent != null
            ? villageSpawnPoint.parent
            : villageSpawnPoint;

        GameObject soldier = Instantiate(
            basicSoldierPrefab,
            villageSpawnPoint.position,
            Quaternion.identity,
            spawnParent);

        OnSoldierSpawned?.Invoke(soldier);

        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
    }
}