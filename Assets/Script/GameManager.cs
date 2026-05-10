////////using UnityEngine;

/////////// <summary>
/////////// AREA FORGE - GameManager (Singleton)
/////////// Core game state manager. Designed for easy multiplayer expansion (Photon/Mirror).
/////////// MULTIPLAYER NOTE: In multiplayer, this will sync game state via NetworkManager.
///////////
/////////// NOTE: The Army panel IS the Customize / InventoryPanel.
/////////// Drag your InventoryPanel GameObject into the armyPanel field in the Inspector.
/////////// </summary>
////////public class GameManager : MonoBehaviour
////////{
////////    // ─── Singleton ────────────────────────────────────────────────────────────
////////    public static GameManager Instance { get; private set; }

////////    // ─── Game State ───────────────────────────────────────────────────────────
////////    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
////////    public GameState CurrentState { get; private set; } = GameState.Village;

////////    /// <summary>True while the Settings overlay is open.</summary>
////////    public bool IsSettingsOpen { get; private set; } = false;

////////    /// <summary>True while the Horse panel overlay is open.</summary>
////////    public bool IsHorsePanelOpen { get; private set; } = false;

////////    // ─── References ───────────────────────────────────────────────────────────
////////    [Header("Panel References")]
////////    [SerializeField] private GameObject villagePanel;

////////    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
////////    [SerializeField] private GameObject armyPanel;

////////    [SerializeField] private GameObject cannonPanel;
////////    [SerializeField] private GameObject castlePanel;
////////    [SerializeField] private GameObject settingsPanel;

////////    [Header("Village Overlays")]
////////    [Tooltip("Drag HorsePanel here — it lives ON TOP of the Village panel.")]
////////    [SerializeField] private GameObject horsePanel;

////////    [Header("Village")]
////////    [SerializeField] private Transform villageSpawnPoint;
////////    [SerializeField] private GameObject basicSoldierPrefab;

////////    // ─── Events ───────────────────────────────────────────────────────────────
////////    public static event System.Action<GameState> OnGameStateChanged;
////////    public static event System.Action<GameObject> OnSoldierSpawned;

////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;
////////        DontDestroyOnLoad(gameObject);
////////    }

////////    private void Start()
////////    {
////////        ShowVillagePanel();
////////    }

////////    // ─── Panel Navigation ─────────────────────────────────────────────────────

////////    /// <summary>
////////    /// Called by ALL "Close" / "Back" buttons.
////////    /// Priority: Horse overlay → Settings overlay → return to Village.
////////    /// </summary>
////////    public void CloseCurrentPanel()
////////    {
////////        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
////////        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }
////////        ShowVillagePanel();
////////    }

////////    /// <summary>Opens the Army / Customize (InventoryPanel) panel.</summary>
////////    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
////////    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
////////    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

////////    // ─── Settings overlay ─────────────────────────────────────────────────────

////////    public void OpenSettingsPanel()
////////    {
////////        if (IsSettingsOpen) return;
////////        IsSettingsOpen = true;
////////        SetPanelVisible(settingsPanel, true);
////////        OnGameStateChanged?.Invoke(GameState.Settings);
////////    }

////////    public void CloseSettingsOverlay()
////////    {
////////        if (!IsSettingsOpen) return;
////////        IsSettingsOpen = false;
////////        SetPanelVisible(settingsPanel, false);
////////        OnGameStateChanged?.Invoke(CurrentState);
////////    }

////////    // ─── Horse panel overlay ──────────────────────────────────────────────────

////////    public void OpenHorsePanel()
////////    {
////////        if (IsHorsePanelOpen) return;
////////        if (IsSettingsOpen) CloseSettingsOverlay();

////////        IsHorsePanelOpen = true;
////////        SetPanelVisible(horsePanel, true);
////////        OnGameStateChanged?.Invoke(GameState.Horse);
////////    }

////////    public void CloseHorsePanel()
////////    {
////////        if (!IsHorsePanelOpen) return;
////////        IsHorsePanelOpen = false;
////////        SetPanelVisible(horsePanel, false);
////////        OnGameStateChanged?.Invoke(CurrentState);
////////    }

////////    // ─── Private helpers ──────────────────────────────────────────────────────

////////    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

////////    private void SetActivePanel(GameState newState)
////////    {
////////        // Silently close overlays when navigating between full panels
////////        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
////////        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

////////        SetPanelVisible(villagePanel, false);
////////        SetPanelVisible(armyPanel, false);
////////        SetPanelVisible(cannonPanel, false);
////////        SetPanelVisible(castlePanel, false);

////////        CurrentState = newState;

////////        switch (newState)
////////        {
////////            case GameState.Village: SetPanelVisible(villagePanel, true); break;
////////            case GameState.Army: SetPanelVisible(armyPanel, true); break;
////////            case GameState.Cannon: SetPanelVisible(cannonPanel, true); break;
////////            case GameState.Castle: SetPanelVisible(castlePanel, true); break;
////////        }

////////        OnGameStateChanged?.Invoke(newState);
////////    }

////////    private void SetPanelVisible(GameObject panel, bool visible)
////////    {
////////        if (panel != null) panel.SetActive(visible);
////////    }

////////    // ─── Soldier Spawning ─────────────────────────────────────────────────────

////////    //public void SpawnBasicSoldier()
////////    //{
////////    //    if (basicSoldierPrefab == null) { Debug.LogError("[GameManager] basicSoldierPrefab not assigned!"); return; }
////////    //    if (villageSpawnPoint == null) { Debug.LogError("[GameManager] villageSpawnPoint not assigned!"); return; }

////////    //    Transform spawnParent = villageSpawnPoint.parent != null
////////    //        ? villageSpawnPoint.parent
////////    //        : villageSpawnPoint;

////////    //    GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, spawnParent);
////////    //    OnSoldierSpawned?.Invoke(soldier);

////////    //    Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
////////    //}


////////    public void SpawnBasicSoldier()
////////    {
////////        if (basicSoldierPrefab == null)
////////        {
////////            Debug.LogError("[GameManager] basicSoldierPrefab not assigned!");
////////            return;
////////        }
////////        if (villageSpawnPoint == null)
////////        {
////////            Debug.LogError("[GameManager] villageSpawnPoint not assigned!");
////////            return;
////////        }

////////        Transform spawnParent = villageSpawnPoint.parent != null
////////            ? villageSpawnPoint.parent
////////            : villageSpawnPoint;

////////        GameObject soldier = Instantiate(
////////            basicSoldierPrefab,
////////            villageSpawnPoint.position,
////////            Quaternion.identity,
////////            spawnParent);

////////        // ── Apply the customization the player chose in the Army panel ────────────
////////        InventoryPanel invPanel = armyPanel != null
////////            ? armyPanel.GetComponentInChildren<InventoryPanel>(true)
////////            : null;

////////        CharacterEquipment soldierEquip = soldier.GetComponent<CharacterEquipment>();

////////        if (invPanel != null && soldierEquip != null)
////////        {
////////            invPanel.ApplySelectionToSoldier(soldierEquip);
////////            Debug.Log("[GameManager] Customization applied to spawned soldier.");
////////        }
////////        else
////////        {
////////            Debug.LogWarning(
////////                $"[GameManager] Could NOT apply loadout.\n" +
////////                $"  InventoryPanel found: {invPanel != null} " +
////////                $"(check armyPanel field or InventoryPanel component)\n" +
////////                $"  CharacterEquipment on prefab: {soldierEquip != null} " +
////////                $"(add CharacterEquipment to SolderPrefab)");
////////        }
////////        // ─────────────────────────────────────────────────────────────────────────

////////        OnSoldierSpawned?.Invoke(soldier);
////////        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
////////    }
////////}

//////using UnityEngine;

///////// <summary>
///////// AREA FORGE - GameManager (Singleton)
/////////
///////// SpawnBasicSoldier() just Instantiates and fires OnSoldierSpawned.
///////// InventoryPanel listens to OnSoldierSpawned and automatically transfers
///////// the user's selected items to the new soldier — no extra call needed here.
///////// </summary>
//////public class GameManager : MonoBehaviour
//////{
//////    // ─── Singleton ────────────────────────────────────────────────────────────
//////    public static GameManager Instance { get; private set; }

//////    // ─── Game State ───────────────────────────────────────────────────────────
//////    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
//////    public GameState CurrentState { get; private set; } = GameState.Village;

//////    public bool IsSettingsOpen   { get; private set; } = false;
//////    public bool IsHorsePanelOpen { get; private set; } = false;

//////    // ─── References ───────────────────────────────────────────────────────────
//////    [Header("Panel References")]
//////    [SerializeField] private GameObject villagePanel;

//////    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
//////    [SerializeField] private GameObject armyPanel;

//////    [SerializeField] private GameObject cannonPanel;
//////    [SerializeField] private GameObject castlePanel;
//////    [SerializeField] private GameObject settingsPanel;

//////    [Header("Village Overlays")]
//////    [Tooltip("Drag HorsePanel here — it lives ON TOP of the Village panel.")]
//////    [SerializeField] private GameObject horsePanel;

//////    [Header("Village")]
//////    [SerializeField] private Transform  villageSpawnPoint;
//////    [SerializeField] private GameObject basicSoldierPrefab;

//////    // ─── Events ───────────────────────────────────────────────────────────────
//////    public static event System.Action<GameState>  OnGameStateChanged;
//////    public static event System.Action<GameObject> OnSoldierSpawned;

//////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;
//////        DontDestroyOnLoad(gameObject);
//////    }

//////    private void Start() => ShowVillagePanel();

//////    // ─── Panel Navigation ─────────────────────────────────────────────────────

//////    public void CloseCurrentPanel()
//////    {
//////        if (IsHorsePanelOpen) { CloseHorsePanel();      return; }
//////        if (IsSettingsOpen)   { CloseSettingsOverlay(); return; }
//////        ShowVillagePanel();
//////    }

//////    public void OpenArmyPanel()   => SetActivePanel(GameState.Army);
//////    public void OpenCannonPanel() => SetActivePanel(GameState.Cannon);
//////    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

//////    // ─── Settings Overlay ─────────────────────────────────────────────────────

//////    public void OpenSettingsPanel()
//////    {
//////        if (IsSettingsOpen) return;
//////        IsSettingsOpen = true;
//////        SetPanelVisible(settingsPanel, true);
//////        OnGameStateChanged?.Invoke(GameState.Settings);
//////    }

//////    public void CloseSettingsOverlay()
//////    {
//////        if (!IsSettingsOpen) return;
//////        IsSettingsOpen = false;
//////        SetPanelVisible(settingsPanel, false);
//////        OnGameStateChanged?.Invoke(CurrentState);
//////    }

//////    // ─── Horse Panel Overlay ──────────────────────────────────────────────────

//////    public void OpenHorsePanel()
//////    {
//////        if (IsHorsePanelOpen) return;
//////        if (IsSettingsOpen) CloseSettingsOverlay();
//////        IsHorsePanelOpen = true;
//////        SetPanelVisible(horsePanel, true);
//////        OnGameStateChanged?.Invoke(GameState.Horse);
//////    }

//////    public void CloseHorsePanel()
//////    {
//////        if (!IsHorsePanelOpen) return;
//////        IsHorsePanelOpen = false;
//////        SetPanelVisible(horsePanel, false);
//////        OnGameStateChanged?.Invoke(CurrentState);
//////    }

//////    // ─── Private Helpers ──────────────────────────────────────────────────────

//////    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

//////    private void SetActivePanel(GameState newState)
//////    {
//////        if (IsSettingsOpen)   { IsSettingsOpen   = false; SetPanelVisible(settingsPanel, false); }
//////        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel,    false); }

//////        SetPanelVisible(villagePanel, false);
//////        SetPanelVisible(armyPanel,    false);
//////        SetPanelVisible(cannonPanel,  false);
//////        SetPanelVisible(castlePanel,  false);

//////        CurrentState = newState;

//////        switch (newState)
//////        {
//////            case GameState.Village: SetPanelVisible(villagePanel, true); break;
//////            case GameState.Army:    SetPanelVisible(armyPanel,    true); break;
//////            case GameState.Cannon:  SetPanelVisible(cannonPanel,  true); break;
//////            case GameState.Castle:  SetPanelVisible(castlePanel,  true); break;
//////        }

//////        OnGameStateChanged?.Invoke(newState);
//////    }

//////    private void SetPanelVisible(GameObject panel, bool visible)
//////    {
//////        if (panel != null) panel.SetActive(visible);
//////    }

//////    // ─── Soldier Spawning ─────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Instantiates the soldier and fires OnSoldierSpawned.
//////    /// InventoryPanel subscribes to OnSoldierSpawned and automatically
//////    /// applies the user's selected items — nothing extra needed here.
//////    /// </summary>
//////    public void SpawnBasicSoldier()
//////    {
//////        if (basicSoldierPrefab == null)
//////        {
//////            Debug.LogError("[GameManager] basicSoldierPrefab not assigned!");
//////            return;
//////        }
//////        if (villageSpawnPoint == null)
//////        {
//////            Debug.LogError("[GameManager] villageSpawnPoint not assigned!");
//////            return;
//////        }

//////        Transform spawnParent = villageSpawnPoint.parent != null
//////            ? villageSpawnPoint.parent
//////            : villageSpawnPoint;

//////        GameObject soldier = Instantiate(
//////            basicSoldierPrefab,
//////            villageSpawnPoint.position,
//////            Quaternion.identity,
//////            spawnParent);

//////        // InventoryPanel.OnSoldierSpawned() handles equipment transfer automatically
//////        OnSoldierSpawned?.Invoke(soldier);

//////        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
//////    }
//////}

////using UnityEngine;

/////// <summary>
/////// AREA FORGE - GameManager (Singleton)
///////
/////// SpawnBasicSoldier() just Instantiates and fires OnSoldierSpawned.
/////// InventoryPanel listens to OnSoldierSpawned and automatically transfers
/////// the user's selected items to the new soldier — no extra call needed here.
///////
/////// ═══════════════════════════════════════════════════════════════════════
/////// PANEL HIERARCHY REQUIREMENT — READ THIS IF BUTTONS DEACTIVATE EVERYTHING
/////// ═══════════════════════════════════════════════════════════════════════
/////// All five panel GameObjects MUST be at the same sibling level inside the
/////// Canvas (or at least siblings of each other).  They must NEVER be children
/////// of one another.  Placing cannonPanel inside castlePanel, for example, means
/////// SetActivePanel() deactivates the parent (castlePanel) and then tries to
/////// re-activate the child — but a child of an inactive parent cannot be seen
/////// regardless of its own SetActive state.
///////
/////// Correct flat structure:
///////   Canvas
///////   ├── VillagePanel
///////   ├── ArmyPanel
///////   ├── CannonPanel        ← CannonPanelManager lives here
///////   ├── CastlePanel
///////   ├── SettingsPanel
///////   └── HorsePanel
///////
/////// ALL six fields below must be assigned in the Inspector.  OnValidate()
/////// will print a warning for every unassigned slot while in Edit mode.
/////// </summary>
////public class GameManager : MonoBehaviour
////{
////    // ─── Singleton ────────────────────────────────────────────────────────────

////    public static GameManager Instance { get; private set; }

////    // ─── Game State ───────────────────────────────────────────────────────────

////    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
////    public GameState CurrentState { get; private set; } = GameState.Village;

////    public bool IsSettingsOpen { get; private set; } = false;
////    public bool IsHorsePanelOpen { get; private set; } = false;

////    // ─── Inspector References ─────────────────────────────────────────────────

////    [Header("Panel References  ← ALL must be assigned or buttons will hide everything")]
////    [SerializeField] private GameObject villagePanel;

////    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
////    [SerializeField] private GameObject armyPanel;

////    [Tooltip("The root of the Cannon panel.  CannonPanelManager must live on this or a child.")]
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

////    private void Start() => ShowVillagePanel();

////    // ─── Inspector Validation (Edit-mode only) ────────────────────────────────

////#if UNITY_EDITOR
////    private void OnValidate()
////    {
////        // Warn for every unassigned panel so the developer spots the problem
////        // before pressing Play.
////        if (villagePanel == null) Debug.LogWarning("[GameManager] villagePanel is not assigned.", this);
////        if (armyPanel == null) Debug.LogWarning("[GameManager] armyPanel is not assigned.", this);
////        if (cannonPanel == null) Debug.LogWarning("[GameManager] cannonPanel is not assigned. " +
////            "Clicking 'Add Cannon' will deactivate everything without re-activating anything!", this);
////        if (castlePanel == null) Debug.LogWarning("[GameManager] castlePanel is not assigned.", this);
////        if (settingsPanel == null) Debug.LogWarning("[GameManager] settingsPanel is not assigned.", this);
////        if (horsePanel == null) Debug.LogWarning("[GameManager] horsePanel is not assigned.", this);
////    }
////#endif

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

////    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
////    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

////    /// <summary>
////    /// Opens the Cannon panel and resets CannonPanelManager to Buy mode.
////    /// Wire the "Add Cannon" button in your scene to call THIS method —
////    /// do NOT wire it to CannonPanelManager directly.
////    /// </summary>
////    public void OpenCannonPanel()
////    {
////        // Guard: tell the developer clearly if the reference is missing.
////        if (cannonPanel == null)
////        {
////            Debug.LogError(
////                "[GameManager] OpenCannonPanel() — cannonPanel is not assigned in the Inspector!\n" +
////                "All panels were deactivated but nothing was re-activated.\n" +
////                "Drag your CannonPanel GameObject into the 'Cannon Panel' slot on GameManager.",
////                this);
////            return;   // Abort — do not deactivate everything for nothing.
////        }

////        SetActivePanel(GameState.Cannon);

////        // Tell CannonPanelManager to reset to Buy mode.
////        // Safe even if CannonPanelManager.Instance is null (first open before Awake fires).
////        CannonPanelManager.Instance?.OnPanelOpened();
////    }

////    // ─── Settings Overlay ─────────────────────────────────────────────────────

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

////    // ─── Horse Panel Overlay ──────────────────────────────────────────────────

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

////    // ─── Private Helpers ──────────────────────────────────────────────────────

////    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

////    /// <summary>
////    /// Deactivates every panel then activates only the requested one.
////    ///
////    /// IMPORTANT: all panel references must be siblings in the hierarchy.
////    /// If panel B is a child of panel A, deactivating A will hide B even
////    /// after calling SetActive(true) on B, because a child of an inactive
////    /// parent is never rendered.
////    /// </summary>
////    private void SetActivePanel(GameState newState)
////    {
////        // Close any open overlays silently when switching main panels.
////        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
////        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

////        // Hide all main panels first.
////        SetPanelVisible(villagePanel, false);
////        SetPanelVisible(armyPanel, false);
////        SetPanelVisible(cannonPanel, false);
////        SetPanelVisible(castlePanel, false);

////        CurrentState = newState;

////        // Show only the requested panel.
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

////    /// <summary>
////    /// Instantiates the soldier and fires OnSoldierSpawned.
////    /// InventoryPanel subscribes to OnSoldierSpawned and automatically
////    /// applies the user's selected items — nothing extra needed here.
////    /// </summary>
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

////        OnSoldierSpawned?.Invoke(soldier);

////        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
////    }
////}

//using UnityEngine;

///// <summary>
///// AREA FORGE — GameManager (Singleton)
/////
///// Owns the shared gold wallet used by ALL panels (Horse, Cannon, etc.).
///// Panels call SpendGold() / AddGold() — they never hold their own gold.
/////
///// GOLD API
///// ─────────────────────────────────────────────────────────────────────
/////   GameManager.Instance.Gold              → current amount (read-only)
/////   GameManager.Instance.SpendGold(cost)   → returns true + deducts if affordable
/////   GameManager.Instance.AddGold(amount)   → adds gold (rewards / refunds / debug)
/////   GameManager.OnGoldChanged              → event fired after every change (int newAmount)
/////
///// PANEL NAVIGATION
///// ─────────────────────────────────────────────────────────────────────
/////   OpenCannonPanel()  → shows CannonPanle, fires OnCannonPanelOpened
/////   OpenHorsePanel()   → overlay on Village, fires OnHorsePanelOpened
/////   CloseCurrentPanel()→ closes overlays first, then returns to Village
/////
///// NOTE: The Cannon Panel opens itself via CannonSlot.AddButton →
/////       CannonPanelManager.OpenPanel(slot). GameManager.OpenCannonPanel()
/////       is still available for a dedicated Cannon button on the HUD.
///// </summary>
//public class GameManager : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────────────────
//    public static GameManager Instance { get; private set; }

//    // ── Game State ────────────────────────────────────────────────────────────
//    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
//    public GameState CurrentState { get; private set; } = GameState.Village;
//    public bool IsSettingsOpen { get; private set; } = false;
//    public bool IsHorsePanelOpen { get; private set; } = false;

//    // ── Shared Gold Wallet ────────────────────────────────────────────────────
//    [Header("Shared Gold Wallet")]
//    [SerializeField] private int startingGold = 500;

//    private int _gold;

//    /// <summary>Current gold (read-only outside GameManager).</summary>
//    public int Gold => _gold;

//    /// <summary>Fired after every gold change. Arg = new total.</summary>
//    public static event System.Action<int> OnGoldChanged;

//    /// <summary>
//    /// Attempts to spend <paramref name="cost"/> gold.
//    /// Returns true and deducts if affordable; false and does nothing otherwise.
//    /// </summary>
//    public bool SpendGold(int cost)
//    {
//        if (cost < 0)
//        {
//            Debug.LogWarning("[GameManager] SpendGold called with negative cost — ignored.");
//            return false;
//        }
//        if (_gold < cost) return false;

//        _gold -= cost;
//        OnGoldChanged?.Invoke(_gold);
//        Debug.Log($"[GameManager] Spent {cost} gold. Remaining: {_gold}");
//        return true;
//    }

//    /// <summary>Adds gold (rewards, refunds, debug).</summary>
//    public void AddGold(int amount)
//    {
//        if (amount <= 0) return;
//        _gold += amount;
//        OnGoldChanged?.Invoke(_gold);
//        Debug.Log($"[GameManager] Added {amount} gold. Total: {_gold}");
//    }

//    // ── Panel References ──────────────────────────────────────────────────────
//    [Header("Panel References")]
//    [SerializeField] private GameObject villagePanel;

//    [Tooltip("InventoryPanel / Customize panel — this IS the Army panel.")]
//    [SerializeField] private GameObject armyPanel;

//    [SerializeField] private GameObject cannonPanel;
//    [SerializeField] private GameObject castlePanel;
//    [SerializeField] private GameObject settingsPanel;

//    [Header("Village Overlays")]
//    [Tooltip("HorsePanel lives ON TOP of the Village panel.")]
//    [SerializeField] private GameObject horsePanel;

//    [Header("Village")]
//    [SerializeField] private Transform villageSpawnPoint;
//    [SerializeField] private GameObject basicSoldierPrefab;

//    // ── Events ────────────────────────────────────────────────────────────────
//    public static event System.Action<GameState> OnGameStateChanged;
//    public static event System.Action<GameObject> OnSoldierSpawned;

//    /// <summary>Fired just after the Cannon panel becomes visible.</summary>
//    public static event System.Action OnCannonPanelOpened;

//    /// <summary>Fired just after the Horse panel overlay becomes visible.</summary>
//    public static event System.Action OnHorsePanelOpened;

//    // ── Unity Lifecycle ───────────────────────────────────────────────────────
//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//        _gold = startingGold;
//    }

//    private void Start() => ShowVillagePanel();

//    // ── Panel Navigation ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by Back / Close buttons.
//    /// Priority: Horse overlay → Settings overlay → Village.
//    /// </summary>
//    public void CloseCurrentPanel()
//    {
//        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
//        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }
//        ShowVillagePanel();
//    }

//    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
//    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

//    /// <summary>
//    /// Opens the Cannon panel and fires OnCannonPanelOpened.
//    /// This is for a dedicated Cannon nav button. The CannonSlot.AddButton
//    /// calls CannonPanelManager.OpenPanel(slot) directly instead.
//    /// </summary>
//    public void OpenCannonPanel()
//    {
//        SetActivePanel(GameState.Cannon);
//        OnCannonPanelOpened?.Invoke();
//    }

//    // ── Settings Overlay ──────────────────────────────────────────────────────

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

//    // ── Horse Panel Overlay ───────────────────────────────────────────────────

//    public void OpenHorsePanel()
//    {
//        if (IsHorsePanelOpen) return;
//        if (IsSettingsOpen) CloseSettingsOverlay();
//        IsHorsePanelOpen = true;
//        SetPanelVisible(horsePanel, true);
//        OnGameStateChanged?.Invoke(GameState.Horse);
//        OnHorsePanelOpened?.Invoke();
//    }

//    public void CloseHorsePanel()
//    {
//        if (!IsHorsePanelOpen) return;
//        IsHorsePanelOpen = false;
//        SetPanelVisible(horsePanel, false);
//        OnGameStateChanged?.Invoke(CurrentState);
//    }

//    // ── Private Helpers ───────────────────────────────────────────────────────

//    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

//    private void SetActivePanel(GameState newState)
//    {
//        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
//        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

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
//        if (panel != null) panel.SetActive(visible);
//    }

//    // ── Soldier Spawning ──────────────────────────────────────────────────────

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

//        OnSoldierSpawned?.Invoke(soldier);
//        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
//    }
//}

using UnityEngine;

/// <summary>
/// AREA FORGE — GameManager (Singleton)
///
/// Handles top-level panel navigation only.
/// Gold is managed inside CannonPanelManager (self-contained).
///
/// ════════════════════════════════════════════════════════════════════
///  REQUIRED HIERARCHY — panels must be SIBLINGS inside the Canvas
/// ════════════════════════════════════════════════════════════════════
///
///   Canvas
///   ├── VillagePanel     (starts active)
///   ├── CannonPanel      (starts inactive)  ← CannonPanelManager here
///   ├── ArmyPanel        (starts inactive)
///   ├── CastlePanel      (starts inactive)
///   ├── SettingsPanel    (starts inactive)
///   └── HorsePanel       (starts inactive)
///
///  NEVER nest one panel inside another — deactivating a parent hides
///  its children even after calling SetActive(true) on them.
///
/// ════════════════════════════════════════════════════════════════════
///  BUTTON WIRING GUIDE
/// ════════════════════════════════════════════════════════════════════
///  CannonSlot.AddButton   → CannonPanelManager.Instance.OpenPanel(slot)
///                           (already wired inside CannonSlot.Awake)
///
///  "Back" / "Close" btns → GameManager.CloseCurrentPanel()
///  "Army" nav button     → GameManager.OpenArmyPanel()
///  "Castle" nav button   → GameManager.OpenCastlePanel()
///
///  NOTE: CannonPanelManager.OpenPanel() calls gameObject.SetActive(true)
///  directly — no need to call GameManager.OpenCannonPanel() from the
///  AddButton. GameManager is only needed for Back navigation.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── Game State ────────────────────────────────────────────────────────────

    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
    public GameState CurrentState { get; private set; } = GameState.Village;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action<GameObject> OnSoldierSpawned;

    // ── Inspector References ──────────────────────────────────────────────────

    [Header("Panels  (ALL must be assigned — see hierarchy comment above)")]
    [SerializeField] private GameObject villagePanel;
    [SerializeField] private GameObject armyPanel;

    [Tooltip("Root of the Cannon panel. CannonPanelManager lives here. " +
             "Must NOT be a child of any other panel.")]
    [SerializeField] private GameObject cannonPanel;

    [SerializeField] private GameObject castlePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject horsePanel;

    [Header("Village Spawning")]
    [SerializeField] private Transform villageSpawnPoint;
    [SerializeField] private GameObject basicSoldierPrefab;

    // ── Overlay State ─────────────────────────────────────────────────────────

    public bool IsSettingsOpen { get; private set; }
    public bool IsHorsePanelOpen { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => ShowVillagePanel();

    // ── Public Navigation ─────────────────────────────────────────────────────

    /// All "Back" / "Close" buttons call this.
    public void CloseCurrentPanel()
    {
        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }
        ShowVillagePanel();
    }

    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

    /// <summary>
    /// Optional — call this if you want GameManager to control panel switching
    /// for the cannon panel. CannonSlot.AddButton already calls
    /// CannonPanelManager.OpenPanel() directly, so this is only needed if you
    /// have a separate "Cannon" nav button on a HUD or menu.
    /// </summary>
    public void OpenCannonPanel()
    {
        if (cannonPanel == null)
        {
            Debug.LogError(
                "[GameManager] cannonPanel is not assigned in the Inspector!\n" +
                "Assign the CannonPanel root GameObject here, or leave this method " +
                "unwired and let CannonSlot.AddButton call CannonPanelManager.OpenPanel() directly.",
                this);
            return;
        }
        SetActivePanel(GameState.Cannon);
        //CannonPanelManager.Instance?.OnPanelOpened();
        CannonPanelManager.Instance?.OnPanelOpened(null);
    }

    // ── Settings Overlay ──────────────────────────────────────────────────────

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

    // ── Horse Panel Overlay ───────────────────────────────────────────────────

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

    // ── Soldier Spawning ──────────────────────────────────────────────────────

    public void SpawnBasicSoldier()
    {
        if (basicSoldierPrefab == null) { Debug.LogError("[GameManager] basicSoldierPrefab not assigned!"); return; }
        if (villageSpawnPoint == null) { Debug.LogError("[GameManager] villageSpawnPoint not assigned!"); return; }

        Transform parent = villageSpawnPoint.parent ?? villageSpawnPoint;
        GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, parent);
        OnSoldierSpawned?.Invoke(soldier);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

    private void SetActivePanel(GameState newState)
    {
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

    // ── Editor Validation ─────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (villagePanel == null) Debug.LogWarning("[GameManager] villagePanel not assigned.", this);
        if (armyPanel == null) Debug.LogWarning("[GameManager] armyPanel not assigned.", this);
        if (castlePanel == null) Debug.LogWarning("[GameManager] castlePanel not assigned.", this);
        if (settingsPanel == null) Debug.LogWarning("[GameManager] settingsPanel not assigned.", this);
        if (horsePanel == null) Debug.LogWarning("[GameManager] horsePanel not assigned.", this);
        // cannonPanel is optional — CannonSlot can open it directly without GameManager.
    }
#endif
}