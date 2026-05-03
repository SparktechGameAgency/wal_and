using UnityEngine;

/// <summary>
/// AREA FORGE - GameManager (Singleton)
/// Core game state manager. Designed for easy multiplayer expansion (Photon/Mirror).
/// MULTIPLAYER NOTE: In multiplayer, this will sync game state via NetworkManager.
///
/// NOTE: The Army panel IS the Customize / InventoryPanel.
/// Drag your InventoryPanel GameObject into the armyPanel field in the Inspector.
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

    /// <summary>True while the Horse panel overlay is open.</summary>
    public bool IsHorsePanelOpen { get; private set; } = false;

    // ─── References ───────────────────────────────────────────────────────────
    [Header("Panel References")]
    [SerializeField] private GameObject villagePanel;

    [Tooltip("Drag your InventoryPanel (Customize panel) here — this IS the Army panel.")]
    [SerializeField] private GameObject armyPanel;

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

    /// <summary>Opens the Army / Customize (InventoryPanel) panel.</summary>
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

    // ─── Private helpers ──────────────────────────────────────────────────────

    private void ShowVillagePanel() => SetActivePanel(GameState.Village);

    private void SetActivePanel(GameState newState)
    {
        // Silently close overlays when navigating between full panels
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

        Transform spawnParent = villageSpawnPoint.parent != null
            ? villageSpawnPoint.parent
            : villageSpawnPoint;

        GameObject soldier = Instantiate(basicSoldierPrefab, villageSpawnPoint.position, Quaternion.identity, spawnParent);
        OnSoldierSpawned?.Invoke(soldier);

        Debug.Log($"[GameManager] Soldier spawned under '{spawnParent.name}'.");
    }
}