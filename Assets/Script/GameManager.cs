//using UnityEngine;

///// <summary>
///// AREA FORGE — GameManager (Singleton)
/////
///// Handles top-level panel navigation only.
///// Gold is managed inside CannonPanelManager (self-contained).
/////
///// ════════════════════════════════════════════════════════════════════
/////  REQUIRED HIERARCHY — panels must be SIBLINGS inside the Canvas
///// ════════════════════════════════════════════════════════════════════
/////
/////   Canvas
/////   ├── VillagePanel     (starts active)
/////   ├── CannonPanel      (starts inactive)  ← CannonPanelManager here
/////   ├── ArmyPanel        (starts inactive)
/////   ├── CastlePanel      (starts inactive)
/////   ├── SettingsPanel    (starts inactive)
/////   └── HorsePanel       (starts inactive)
/////
/////  NEVER nest one panel inside another — deactivating a parent hides
/////  its children even after calling SetActive(true) on them.
/////
///// ════════════════════════════════════════════════════════════════════
/////  BUTTON WIRING GUIDE
///// ════════════════════════════════════════════════════════════════════
/////  CannonSlot.AddButton   → CannonPanelManager.Instance.OpenPanel(slot)
/////                           (already wired inside CannonSlot.Awake)
/////
/////  "Back" / "Close" btns → GameManager.CloseCurrentPanel()
/////  "Army" nav button     → GameManager.OpenArmyPanel()
/////  "Castle" nav button   → GameManager.OpenCastlePanel()
/////
/////  NOTE: CannonPanelManager.OpenPanel() calls gameObject.SetActive(true)
/////  directly — no need to call GameManager.OpenCannonPanel() from the
/////  AddButton. GameManager is only needed for Back navigation.
///// </summary>
//public class GameManager : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────────────────

//    public static GameManager Instance { get; private set; }

//    // ── Game State ────────────────────────────────────────────────────────────

//    public enum GameState { Village, Army, Cannon, Castle, Settings, Horse }
//    public GameState CurrentState { get; private set; } = GameState.Village;

//    // ── Events ────────────────────────────────────────────────────────────────

//    public static event System.Action<GameState> OnGameStateChanged;
//    public static event System.Action<GameObject> OnSoldierSpawned;

//    // ── Inspector References ──────────────────────────────────────────────────

//    [Header("Panels  (ALL must be assigned — see hierarchy comment above)")]
//    [SerializeField] private GameObject villagePanel;
//    [SerializeField] private GameObject armyPanel;

//    [Tooltip("Root of the Cannon panel. CannonPanelManager lives here. " +
//             "Must NOT be a child of any other panel.")]
//    [SerializeField] private GameObject cannonPanel;

//    [SerializeField] private GameObject castlePanel;
//    [SerializeField] private GameObject settingsPanel;
//    [SerializeField] private GameObject horsePanel;

//    [Header("Village Spawning")]
//    [SerializeField] private Transform villageSpawnPoint;
//    [SerializeField] private GameObject basicSoldierPrefab;

//    // ── Overlay State ─────────────────────────────────────────────────────────

//    public bool IsSettingsOpen { get; private set; }
//    public bool IsHorsePanelOpen { get; private set; }

//    // ── Lifecycle ─────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    private void Start() => ShowVillagePanel();

//    // ── Public Navigation ─────────────────────────────────────────────────────

//    /// All "Back" / "Close" buttons call this.
//    public void CloseCurrentPanel()
//    {
//        if (IsHorsePanelOpen) { CloseHorsePanel(); return; }
//        if (IsSettingsOpen) { CloseSettingsOverlay(); return; }

//        ShowVillagePanel();
//    }

//    public void OpenArmyPanel() => SetActivePanel(GameState.Army);
//    public void OpenCastlePanel() => SetActivePanel(GameState.Castle);

//    /// <summary>
//    /// Optional — call this if you want GameManager to control panel switching
//    /// for the cannon panel. CannonSlot.AddButton already calls
//    /// CannonPanelManager.OpenPanel() directly, so this is only needed if you
//    /// have a separate "Cannon" nav button on a HUD or menu.
//    /// </summary>
//    public void OpenCannonPanel()
//    {
//        // If cannonPanel isn't assigned in the Inspector, find it at runtime.
//        if (cannonPanel == null)
//        {
//            var mgr = FindObjectOfType<CannonPanelManager>(includeInactive: true);
//            if (mgr != null)
//                cannonPanel = mgr.gameObject;
//            else
//            {
//                Debug.LogError("[GameManager] cannonPanel not assigned and CannonPanelManager not found in scene!", this);
//                return;
//            }
//        }
//        SetActivePanel(GameState.Cannon);
//        CannonPanelManager.Instance?.OnPanelOpened(null);
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
//    }

//    public void CloseHorsePanel()
//    {
//        if (!IsHorsePanelOpen) return;
//        IsHorsePanelOpen = false;
//        SetPanelVisible(horsePanel, false);
//        OnGameStateChanged?.Invoke(CurrentState);
//    }

//    // ── Soldier Spawning ──────────────────────────────────────────────────────

//    public void SpawnBasicSoldier()
//    {
//        if (basicSoldierPrefab == null) { Debug.LogError("[GameManager] basicSoldierPrefab not assigned!"); return; }
//        if (villageSpawnPoint == null) { Debug.LogError("[GameManager] villageSpawnPoint not assigned!"); return; }

//        Transform parent = villageSpawnPoint.parent ?? villageSpawnPoint;
//        GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, parent);
//        OnSoldierSpawned?.Invoke(soldier);
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
//            case GameState.Village:
//                SetPanelVisible(villagePanel, true);
//                CastleUnitDropZone.SetCannonZonesVisible(false);
//                break;
//            case GameState.Army:
//                SetPanelVisible(armyPanel, true);
//                CastleUnitDropZone.SetCannonZonesVisible(false);
//                break;
//            case GameState.Cannon:
//                SetPanelVisible(cannonPanel, true);
//                // Reveal empty cannon zones so the player can tap them.
//                CastleUnitDropZone.SetCannonZonesVisible(true);
//                break;
//            case GameState.Castle:
//                SetPanelVisible(castlePanel, true);
//                // Hide cannon zones — only shown when in the Cannon section.
//                CastleUnitDropZone.SetCannonZonesVisible(false);
//                break;
//        }

//        OnGameStateChanged?.Invoke(newState);
//    }

//    private void SetPanelVisible(GameObject panel, bool visible)
//    {
//        if (panel != null) panel.SetActive(visible);
//    }

//    // ── Editor Validation ─────────────────────────────────────────────────────

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (villagePanel == null) Debug.LogWarning("[GameManager] villagePanel not assigned.", this);
//        if (armyPanel == null) Debug.LogWarning("[GameManager] armyPanel not assigned.", this);
//        if (castlePanel == null) Debug.LogWarning("[GameManager] castlePanel not assigned.", this);
//        if (settingsPanel == null) Debug.LogWarning("[GameManager] settingsPanel not assigned.", this);
//        if (horsePanel == null) Debug.LogWarning("[GameManager] horsePanel not assigned.", this);
//        // cannonPanel is optional — CannonSlot can open it directly without GameManager.
//    }
//#endif
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

    public void ReturnToCannonZones()
    {
        if (IsSettingsOpen) { IsSettingsOpen = false; SetPanelVisible(settingsPanel, false); }
        if (IsHorsePanelOpen) { IsHorsePanelOpen = false; SetPanelVisible(horsePanel, false); }

        SetPanelVisible(villagePanel, false);
        SetPanelVisible(armyPanel, false);
        SetPanelVisible(cannonPanel, false);
        SetPanelVisible(castlePanel, true);

        CurrentState = GameState.Cannon;
        CastleUnitDropZone.SetCannonZonesVisible(true);
        OnGameStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// Optional — call this if you want GameManager to control panel switching
    /// for the cannon panel. CannonSlot.AddButton already calls
    /// CannonPanelManager.OpenPanel() directly, so this is only needed if you
    /// have a separate "Cannon" nav button on a HUD or menu.
    /// </summary>
    public void OpenCannonPanel()
    {
        // If cannonPanel isn't assigned in the Inspector, find it at runtime.
        if (cannonPanel == null)
        {
            var mgr = FindObjectOfType<CannonPanelManager>(includeInactive: true);
            if (mgr != null)
                cannonPanel = mgr.gameObject;
            else
            {
                Debug.LogError("[GameManager] cannonPanel not assigned and CannonPanelManager not found in scene!", this);
                return;
            }
        }
        SetActivePanel(GameState.Cannon);
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
            case GameState.Village:
                SetPanelVisible(villagePanel, true);
                CastleUnitDropZone.SetCannonZonesVisible(false);
                break;
            case GameState.Army:
                SetPanelVisible(armyPanel, true);
                CastleUnitDropZone.SetCannonZonesVisible(false);
                break;
            case GameState.Cannon:
                SetPanelVisible(cannonPanel, true);
                // Reveal empty cannon zones so the player can tap them.
                CastleUnitDropZone.SetCannonZonesVisible(true);
                break;
            case GameState.Castle:
                SetPanelVisible(castlePanel, true);
                // Hide cannon zones — only shown when in the Cannon section.
                CastleUnitDropZone.SetCannonZonesVisible(false);
                break;
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