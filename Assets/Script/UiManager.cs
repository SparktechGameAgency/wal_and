//////////////using UnityEngine;
//////////////using UnityEngine.UI;

///////////////// <summary>
///////////////// AREA FORGE - UIManager (Singleton)
///////////////// Wires all UI buttons to GameManager actions.
///////////////// Assign button references in the Inspector, then this script does the rest.
///////////////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
///////////////// </summary>
//////////////public class UIManager : MonoBehaviour
//////////////{
//////////////    // ─── Singleton ────────────────────────────────────────────────────────────
//////////////    public static UIManager Instance { get; private set; }

//////////////    // ─── Main Navigation Buttons (always visible on Village panel) ────────────
//////////////    [Header("Main Navigation Buttons")]
//////////////    [SerializeField] private Button settingsButton;
//////////////    [SerializeField] private Button armyButton;
//////////////    [SerializeField] private Button cannonButton;
//////////////    [SerializeField] private Button castleButton;

//////////////    // ─── Close / Back Buttons (one per panel) ─────────────────────────────────
//////////////    [Header("Panel Close Buttons")]
//////////////    [SerializeField] private Button closeArmyButton;
//////////////    [SerializeField] private Button closeCannonButton;
//////////////    [SerializeField] private Button closeCastleButton;
//////////////    [SerializeField] private Button closeSettingsButton;

//////////////    // ─── Army Panel Buttons ───────────────────────────────────────────────────
//////////////    [Header("Army Panel")]
//////////////    [SerializeField] private Button buyBasicSoldierButton;

//////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//////////////    private void Awake()
//////////////    {
//////////////        if (Instance != null && Instance != this)
//////////////        {
//////////////            Destroy(gameObject);
//////////////            return;
//////////////        }
//////////////        Instance = this;
//////////////    }

//////////////    private void Start()
//////////////    {
//////////////        WireButtons();
//////////////        SubscribeToEvents();
//////////////    }

//////////////    private void OnDestroy()
//////////////    {
//////////////        UnsubscribeFromEvents();
//////////////    }

//////////////    // ─── Button Wiring ────────────────────────────────────────────────────────

//////////////    private void WireButtons()
//////////////    {
//////////////        // ── Navigation buttons ──────────────────────────────────────────────
//////////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//////////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//////////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//////////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//////////////        // ── Close / Back buttons ─────────────────────────────────────────────
//////////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//////////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//////////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//////////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());

//////////////        // ── Army Panel actions ───────────────────────────────────────────────
//////////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//////////////    }

//////////////    // ─── Army Panel Logic ─────────────────────────────────────────────────────

//////////////    private void OnBuySoldierClicked()
//////////////    {
//////////////        // Spawn the soldier in the village
//////////////        GameManager.Instance.SpawnBasicSoldier();

//////////////        // Close the army panel — return to village to see the soldier
//////////////        GameManager.Instance.CloseCurrentPanel();

//////////////        Debug.Log("[UIManager] Buy button clicked — soldier spawned, returning to village.");
//////////////    }

//////////////    // ─── Event Subscriptions ─────────────────────────────────────────────────

//////////////    private void SubscribeToEvents()
//////////////    {
//////////////        GameManager.OnGameStateChanged += HandleStateChanged;
//////////////    }

//////////////    private void UnsubscribeFromEvents()
//////////////    {
//////////////        GameManager.OnGameStateChanged -= HandleStateChanged;
//////////////    }

//////////////    private void HandleStateChanged(GameManager.GameState newState)
//////////////    {
//////////////        // You can update button interactability, highlight active tab, etc.
//////////////        // Example: disable the army button while in army panel
//////////////        if (armyButton != null)
//////////////            armyButton.interactable = (newState != GameManager.GameState.Army);
//////////////    }

//////////////    // ─── Utility ─────────────────────────────────────────────────────────────

//////////////    /// <summary>
//////////////    /// Safe AddListener — skips null buttons with a warning.
//////////////    /// </summary>
//////////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//////////////    {
//////////////        if (button != null)
//////////////            button.onClick.AddListener(action);
//////////////        else
//////////////            Debug.LogWarning($"[UIManager] A button reference is null. Check Inspector assignments.");
//////////////    }
//////////////}

////////////using UnityEngine;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// AREA FORGE - UIManager (Singleton)
/////////////// Wires all UI buttons to GameManager actions.
/////////////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////////////// </summary>
////////////public class UIManager : MonoBehaviour
////////////{
////////////    public static UIManager Instance { get; private set; }

////////////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
////////////    [Header("Main Navigation Buttons")]
////////////    [SerializeField] private Button settingsButton;
////////////    [SerializeField] private Button armyButton;
////////////    [SerializeField] private Button cannonButton;
////////////    [SerializeField] private Button castleButton;

////////////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
////////////    [Header("Panel Close Buttons")]
////////////    [SerializeField] private Button closeArmyButton;
////////////    [SerializeField] private Button closeCannonButton;
////////////    [SerializeField] private Button closeCastleButton;
////////////    [SerializeField] private Button closeSettingsButton;

////////////    /// <summary>
////////////    /// The close button INSIDE the Horse panel.
////////////    /// Drag the HorsePanle close button here — it calls CloseCurrentPanel()
////////////    /// so GameManager tracks the state change correctly.
////////////    /// </summary>
////////////    [SerializeField] private Button closeHorseButton;

////////////    // ─── Army Panel Buttons ───────────────────────────────────────────────────
////////////    [Header("Army Panel")]
////////////    [SerializeField] private Button buyBasicSoldierButton;

////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
////////////    private void Awake()
////////////    {
////////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////////        Instance = this;
////////////    }

////////////    private void Start()
////////////    {
////////////        WireButtons();
////////////        SubscribeToEvents();
////////////    }

////////////    private void OnDestroy()
////////////    {
////////////        UnsubscribeFromEvents();
////////////    }

////////////    // ─── Button Wiring ────────────────────────────────────────────────────────

////////////    private void WireButtons()
////////////    {
////////////        // Navigation
////////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////////////        // Close / Back — all route through CloseCurrentPanel so GameManager
////////////        // always owns the state transition (handles overlays correctly)
////////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

////////////        // Army
////////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////////////    }

////////////    // ─── Army Panel Logic ─────────────────────────────────────────────────────

////////////    private void OnBuySoldierClicked()
////////////    {
////////////        GameManager.Instance.SpawnBasicSoldier();
////////////        GameManager.Instance.CloseCurrentPanel();
////////////        Debug.Log("[UIManager] Soldier spawned, returning to village.");
////////////    }

////////////    // ─── Event Subscriptions ─────────────────────────────────────────────────

////////////    private void SubscribeToEvents()
////////////    {
////////////        GameManager.OnGameStateChanged += HandleStateChanged;
////////////    }

////////////    private void UnsubscribeFromEvents()
////////////    {
////////////        GameManager.OnGameStateChanged -= HandleStateChanged;
////////////    }

////////////    private void HandleStateChanged(GameManager.GameState newState)
////////////    {
////////////        // Disable nav buttons for the panel that's currently active
////////////        // so the player can't re-open a panel they're already in
////////////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
////////////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
////////////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
////////////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
////////////    }

////////////    // ─── Utility ─────────────────────────────────────────────────────────────

////////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////////////    {
////////////        if (button != null)
////////////            button.onClick.AddListener(action);
////////////        else
////////////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector assignments.");
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;

//////////public class UIManager : MonoBehaviour
//////////{
//////////    public static UIManager Instance { get; private set; }

//////////    [Header("Main Navigation Buttons")]
//////////    [SerializeField] private Button settingsButton;
//////////    [SerializeField] private Button armyButton;
//////////    [SerializeField] private Button cannonButton;
//////////    [SerializeField] private Button castleButton;

//////////    [Header("Panel Close Buttons")]
//////////    [SerializeField] private Button closeArmyButton;
//////////    [SerializeField] private Button closeCannonButton;
//////////    [SerializeField] private Button closeCastleButton;
//////////    [SerializeField] private Button closeSettingsButton;
//////////    [SerializeField] private Button closeHorseButton;

//////////    [Header("Army Panel")]
//////////    [SerializeField] private Button buyBasicSoldierButton;

//////////    // ─── Lifecycle ────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////        Instance = this;

//////////        // ── Wire buttons in Awake, not Start ──────────────────────────────────
//////////        // Start() has execution-order problems — other managers may hide panels
//////////        // before Start() runs, causing listeners to never be registered.
//////////        WireButtons();
//////////        SubscribeToEvents();
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        UnsubscribeFromEvents();
//////////    }

//////////    // ─── Button Wiring ────────────────────────────────────────────────────────

//////////    private void WireButtons()
//////////    {
//////////        // Navigation
//////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//////////        // Close / Back
//////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//////////        // Army
//////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//////////    }

//////////    // ─── Army ─────────────────────────────────────────────────────────────────

//////////    private void OnBuySoldierClicked()
//////////    {
//////////        GameManager.Instance.SpawnBasicSoldier();
//////////        GameManager.Instance.CloseCurrentPanel();
//////////    }

//////////    // ─── Events ───────────────────────────────────────────────────────────────

//////////    private void SubscribeToEvents()
//////////    {
//////////        GameManager.OnGameStateChanged += HandleStateChanged;
//////////    }

//////////    private void UnsubscribeFromEvents()
//////////    {
//////////        GameManager.OnGameStateChanged -= HandleStateChanged;
//////////    }

//////////    private void HandleStateChanged(GameManager.GameState newState)
//////////    {
//////////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//////////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//////////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//////////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
//////////    }

//////////    // ─── Utility ─────────────────────────────────────────────────────────────

//////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//////////    {
//////////        if (button != null)
//////////            button.onClick.AddListener(action);
//////////        else
//////////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

/////////// <summary>
/////////// AREA FORGE - UIManager (Singleton)
/////////// Wires all UI buttons to GameManager actions.
/////////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////////// </summary>
////////public class UIManager : MonoBehaviour
////////{
////////    public static UIManager Instance { get; private set; }

////////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
////////    [Header("Main Navigation Buttons")]
////////    [SerializeField] private Button settingsButton;
////////    [SerializeField] private Button armyButton;
////////    [SerializeField] private Button cannonButton;
////////    [SerializeField] private Button castleButton;

////////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
////////    [Header("Panel Close Buttons")]
////////    [SerializeField] private Button closeArmyButton;
////////    [SerializeField] private Button closeCannonButton;
////////    [SerializeField] private Button closeCastleButton;
////////    [SerializeField] private Button closeSettingsButton;
////////    [SerializeField] private Button closeHorseButton;

////////    [Tooltip("The X / Close button INSIDE the InventoryPanel.")]
////////    [SerializeField] private Button closeCustomizeButton;

////////    // ─── Customize Panel ──────────────────────────────────────────────────────
////////    [Header("Customize Panel")]
////////    [Tooltip("The button in the HUD / Village that opens the Customize panel.\n" +
////////             "In your screenshot this is the 'PLAYER' button on the left.")]
////////    [SerializeField] private Button openCustomizeButton;

////////    // ─── Army Panel ───────────────────────────────────────────────────────────
////////    [Header("Army Panel")]
////////    [SerializeField] private Button buyBasicSoldierButton;

////////    // ─── Lifecycle ────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;

////////        // Wire in Awake — avoids execution-order issues with Start()
////////        WireButtons();
////////        SubscribeToEvents();
////////    }

////////    private void OnDestroy()
////////    {
////////        UnsubscribeFromEvents();
////////    }

////////    // ─── Button Wiring ────────────────────────────────────────────────────────

////////    private void WireButtons()
////////    {
////////        // ── Navigation ──────────────────────────────────────────────────────
////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////////        // ── Close / Back — all routed through CloseCurrentPanel ─────────────
////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeCustomizeButton, () => GameManager.Instance.CloseCurrentPanel());

////////        // ── Customize panel ──────────────────────────────────────────────────
////////        AddListener(openCustomizeButton, () => GameManager.Instance.OpenCustomizePanel());

////////        // ── Army ─────────────────────────────────────────────────────────────
////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////////    }

////////    // ─── Army ─────────────────────────────────────────────────────────────────

////////    private void OnBuySoldierClicked()
////////    {
////////        GameManager.Instance.SpawnBasicSoldier();
////////        GameManager.Instance.CloseCurrentPanel();
////////    }

////////    // ─── Events ───────────────────────────────────────────────────────────────

////////    private void SubscribeToEvents()
////////    {
////////        GameManager.OnGameStateChanged += HandleStateChanged;
////////    }

////////    private void UnsubscribeFromEvents()
////////    {
////////        GameManager.OnGameStateChanged -= HandleStateChanged;
////////    }

////////    private void HandleStateChanged(GameManager.GameState newState)
////////    {
////////        // Disable nav buttons for whichever panel is currently active
////////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
////////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
////////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
////////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);

////////        // Disable the open-customize button while the panel is already open
////////        if (openCustomizeButton != null)
////////            openCustomizeButton.interactable = (newState != GameManager.GameState.Customize);
////////    }

////////    // ─── Utility ─────────────────────────────────────────────────────────────

////////    /// <summary>Safe AddListener — logs a warning for any unassigned button.</summary>
////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////////    {
////////        if (button != null)
////////            button.onClick.AddListener(action);
////////        else
////////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE - UIManager (Singleton)
///////// Wires all UI buttons to GameManager actions.
///////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////////
///////// NOTE: armyButton opens the Customize / InventoryPanel (they are the same panel).
///////// Drag the button that opens your Customize panel into the armyButton field.
///////// </summary>
//////public class UIManager : MonoBehaviour
//////{
//////    public static UIManager Instance { get; private set; }

//////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
//////    [Header("Main Navigation Buttons")]
//////    [SerializeField] private Button settingsButton;

//////    [Tooltip("This opens the Customize / InventoryPanel. Drag your Army / Customize open button here.")]
//////    [SerializeField] private Button armyButton;

//////    [SerializeField] private Button cannonButton;
//////    [SerializeField] private Button castleButton;

//////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
//////    [Header("Panel Close Buttons")]
//////    [Tooltip("The close / back button inside the Customize (Army) panel.")]
//////    [SerializeField] private Button closeArmyButton;

//////    [SerializeField] private Button closeCannonButton;
//////    [SerializeField] private Button closeCastleButton;
//////    [SerializeField] private Button closeSettingsButton;
//////    [SerializeField] private Button closeHorseButton;

//////    // ─── Army Panel ───────────────────────────────────────────────────────────
//////    [Header("Army Panel")]
//////    [SerializeField] private Button buyBasicSoldierButton;

//////    // ─── Lifecycle ────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;

//////        // Wire in Awake — avoids execution-order issues with Start()
//////        WireButtons();
//////        SubscribeToEvents();
//////    }

//////    private void OnDestroy()
//////    {
//////        UnsubscribeFromEvents();
//////    }

//////    // ─── Button Wiring ────────────────────────────────────────────────────────

//////    private void WireButtons()
//////    {
//////        // ── Navigation ──────────────────────────────────────────────────────
//////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());   // opens Customize panel
//////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//////        // ── Close / Back — all routed through CloseCurrentPanel ─────────────
//////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//////        // ── Army / Customize ─────────────────────────────────────────────────
//////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//////    }

//////    // ─── Army / Customize Logic ───────────────────────────────────────────────

//////    private void OnBuySoldierClicked()
//////    {
//////        GameManager.Instance.SpawnBasicSoldier();
//////        GameManager.Instance.CloseCurrentPanel();
//////    }

//////    // ─── Events ───────────────────────────────────────────────────────────────

//////    private void SubscribeToEvents()
//////    {
//////        GameManager.OnGameStateChanged += HandleStateChanged;
//////    }

//////    private void UnsubscribeFromEvents()
//////    {
//////        GameManager.OnGameStateChanged -= HandleStateChanged;
//////    }

//////    private void HandleStateChanged(GameManager.GameState newState)
//////    {
//////        // Disable nav buttons for whichever panel is currently active
//////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
//////    }

//////    // ─── Utility ─────────────────────────────────────────────────────────────

//////    /// <summary>Safe AddListener — logs a warning for any unassigned button.</summary>
//////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//////    {
//////        if (button != null)
//////            button.onClick.AddListener(action);
//////        else
//////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - UIManager (Singleton)
/////// Wires all UI buttons to GameManager actions.
/////// Also drives CastleGridMover when the Castle Panel opens or closes.
/////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////// </summary>
////public class UIManager : MonoBehaviour
////{
////    public static UIManager Instance { get; private set; }

////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
////    [Header("Main Navigation Buttons")]
////    [SerializeField] private Button settingsButton;
////    [SerializeField] private Button armyButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button castleButton;

////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
////    [Header("Panel Close Buttons")]
////    [SerializeField] private Button closeArmyButton;
////    [SerializeField] private Button closeCannonButton;
////    [SerializeField] private Button closeCastleButton;   // also closes Castle Panel grid
////    [SerializeField] private Button closeSettingsButton;
////    [SerializeField] private Button closeHorseButton;

////    // ─── Army Panel ───────────────────────────────────────────────────────────
////    [Header("Army Panel")]
////    [SerializeField] private Button buyBasicSoldierButton;

////    // ─── Lifecycle ────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        WireButtons();
////        SubscribeToEvents();
////    }

////    private void OnDestroy()
////    {
////        UnsubscribeFromEvents();
////    }

////    // ─── Button Wiring ────────────────────────────────────────────────────────

////    private void WireButtons()
////    {
////        // ── Navigation ──────────────────────────────────────────────────────
////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());

////        // Castle button: open GameManager state AND move the grid into Castle Panel
////        AddListener(castleButton, () =>
////        {
////            GameManager.Instance.OpenCastlePanel();
////            if (CastleGridMover.Instance != null)
////                CastleGridMover.Instance.OpenCastlePanel();
////            else
////                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move.");
////        });

////        // ── Close / Back buttons ─────────────────────────────────────────────
////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

////        // Castle close: restore game state AND move grid back to Village Panel
////        AddListener(closeCastleButton, () =>
////        {
////            GameManager.Instance.CloseCurrentPanel();
////            if (CastleGridMover.Instance != null)
////                CastleGridMover.Instance.OpenVillagePanel();
////            else
////                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move back.");
////        });

////        // ── Army ─────────────────────────────────────────────────────────────
////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////    }

////    // ─── Army Logic ───────────────────────────────────────────────────────────

////    private void OnBuySoldierClicked()
////    {
////        GameManager.Instance.SpawnBasicSoldier();
////        GameManager.Instance.CloseCurrentPanel();
////    }

////    // ─── Events ───────────────────────────────────────────────────────────────

////    private void SubscribeToEvents()
////    {
////        GameManager.OnGameStateChanged += HandleStateChanged;
////    }

////    private void UnsubscribeFromEvents()
////    {
////        GameManager.OnGameStateChanged -= HandleStateChanged;
////    }

////    private void HandleStateChanged(GameManager.GameState newState)
////    {
////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
////    }

////    // ─── Utility ─────────────────────────────────────────────────────────────

////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////    {
////        if (button != null)
////            button.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - UIManager (Singleton)
///// Wires all UI buttons to GameManager actions.
///// Also drives CastleGridMover when the Castle Panel opens or closes.
/////
///// ── FIX: Buying a cannon or soldier UNIT (the draggable castle units) must
/////         NOT call OpenCastlePanel. BuyCastleCannonUnit / BuyCastleSoldierUnit
/////         stay on the current panel so the player keeps their context.
/////         Wire the buy buttons for those units to these methods.
///// </summary>
//public class UIManager : MonoBehaviour
//{
//    public static UIManager Instance { get; private set; }

//    // ─── Main Navigation Buttons ──────────────────────────────────────────────
//    [Header("Main Navigation Buttons")]
//    [SerializeField] private Button settingsButton;
//    [SerializeField] private Button armyButton;
//    [SerializeField] private Button cannonButton;
//    [SerializeField] private Button castleButton;

//    // ─── Close / Back Buttons ─────────────────────────────────────────────────
//    [Header("Panel Close Buttons")]
//    [SerializeField] private Button closeArmyButton;
//    [SerializeField] private Button closeCannonButton;
//    [SerializeField] private Button closeCastleButton;
//    [SerializeField] private Button closeSettingsButton;
//    [SerializeField] private Button closeHorseButton;

//    // ─── Army Panel ───────────────────────────────────────────────────────────
//    [Header("Army Panel")]
//    [SerializeField] private Button buyBasicSoldierButton;

//    // ─── Castle Unit Shop ─────────────────────────────────────────────────────
//    // These are the draggable cannon / soldier UNIT buy buttons that live in
//    // whatever panel shows unit inventory (VillagePanel, ArmyPanel, etc.).
//    //
//    // FIX: Wire these here instead of wiring them directly to
//    //      GameManager.OpenCastlePanel() in the Inspector — that's what caused
//    //      buying a unit to accidentally navigate to the Castle Panel.
//    [Header("Castle Unit Buy Buttons (stay on current panel)")]
//    [Tooltip("Button that purchases / spawns a Cannon unit draggable. " +
//             "Does NOT navigate — remove any OpenCastlePanel call from its Inspector onClick.")]
//    [SerializeField] private Button buyCastleCannonButton;

//    [Tooltip("Button that purchases / spawns a Soldier unit draggable. " +
//             "Does NOT navigate — remove any OpenCastlePanel call from its Inspector onClick.")]
//    [SerializeField] private Button buyCastleSoldierButton;

//    // ─── Lifecycle ────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        WireButtons();
//        SubscribeToEvents();
//    }

//    private void OnDestroy()
//    {
//        UnsubscribeFromEvents();
//    }

//    // ─── Button Wiring ────────────────────────────────────────────────────────

//    private void WireButtons()
//    {
//        // ── Navigation ──────────────────────────────────────────────────────
//        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());

//        // Castle button: open GameManager state AND move the grid into view
//        AddListener(castleButton, () =>
//        {
//            GameManager.Instance.OpenCastlePanel();
//            if (CastleGridMover.Instance != null)
//                CastleGridMover.Instance.OpenCastlePanel();
//            else
//                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move.");
//        });

//        // ── Close / Back ─────────────────────────────────────────────────────
//        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//        // Castle close: restore game state AND move grid back
//        AddListener(closeCastleButton, () =>
//        {
//            GameManager.Instance.CloseCurrentPanel();
//            if (CastleGridMover.Instance != null)
//                CastleGridMover.Instance.OpenVillagePanel();
//            else
//                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move back.");
//        });

//        // ── Army ─────────────────────────────────────────────────────────────
//        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);

//        // ── Castle unit shop ─────────────────────────────────────────────────
//        // FIX: These handlers stay on the current panel.  Do NOT add
//        //      GameManager.OpenCastlePanel() here or in the Inspector onClick.
//        AddListener(buyCastleCannonButton, OnBuyCastleCannonClicked);
//        AddListener(buyCastleSoldierButton, OnBuyCastleSoldierClicked);
//    }

//    // ─── Army Logic ───────────────────────────────────────────────────────────

//    private void OnBuySoldierClicked()
//    {
//        GameManager.Instance.SpawnBasicSoldier();
//        GameManager.Instance.CloseCurrentPanel();
//    }

//    // ─── Castle Unit Buy Logic ────────────────────────────────────────────────

//    /// <summary>
//    /// Called when the player buys a Cannon UNIT (draggable for the castle grid).
//    /// Stays on the current panel — does NOT navigate to the Castle Panel.
//    /// Add your spawn / inventory logic here (e.g. CastleUnitInventory.Add(Cannon)).
//    /// </summary>
//    private void OnBuyCastleCannonClicked()
//    {
//        // TODO: replace with your actual cannon-unit grant logic, e.g.:
//        // CastleUnitInventory.Instance.AddUnit(CastleUnitType.Cannon);
//        Debug.Log("[UIManager] Cannon unit purchased — staying on current panel.");

//        // Deliberately NOT calling GameManager.Instance.OpenCastlePanel() here.
//        // The player remains wherever they were when they clicked buy.
//    }

//    /// <summary>
//    /// Called when the player buys a Soldier UNIT (draggable for the castle grid).
//    /// Stays on the current panel — does NOT navigate to the Castle Panel.
//    /// </summary>
//    private void OnBuyCastleSoldierClicked()
//    {
//        // TODO: replace with your actual soldier-unit grant logic, e.g.:
//        // CastleUnitInventory.Instance.AddUnit(CastleUnitType.Soldier);
//        Debug.Log("[UIManager] Soldier unit purchased — staying on current panel.");

//        // Deliberately NOT calling GameManager.Instance.OpenCastlePanel() here.
//    }

//    // ─── Events ───────────────────────────────────────────────────────────────

//    private void SubscribeToEvents()
//    {
//        GameManager.OnGameStateChanged += HandleStateChanged;
//    }

//    private void UnsubscribeFromEvents()
//    {
//        GameManager.OnGameStateChanged -= HandleStateChanged;
//    }

//    private void HandleStateChanged(GameManager.GameState newState)
//    {
//        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
//    }

//    // ─── Utility ─────────────────────────────────────────────────────────────

//    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//    {
//        if (button != null)
//            button.onClick.AddListener(action);
//        else
//            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
//    }
//}

////////////using UnityEngine;
////////////using UnityEngine.UI;

/////////////// <summary>
/////////////// AREA FORGE - UIManager (Singleton)
/////////////// Wires all UI buttons to GameManager actions.
/////////////// Assign button references in the Inspector, then this script does the rest.
/////////////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////////////// </summary>
////////////public class UIManager : MonoBehaviour
////////////{
////////////    // ─── Singleton ────────────────────────────────────────────────────────────
////////////    public static UIManager Instance { get; private set; }

////////////    // ─── Main Navigation Buttons (always visible on Village panel) ────────────
////////////    [Header("Main Navigation Buttons")]
////////////    [SerializeField] private Button settingsButton;
////////////    [SerializeField] private Button armyButton;
////////////    [SerializeField] private Button cannonButton;
////////////    [SerializeField] private Button castleButton;

////////////    // ─── Close / Back Buttons (one per panel) ─────────────────────────────────
////////////    [Header("Panel Close Buttons")]
////////////    [SerializeField] private Button closeArmyButton;
////////////    [SerializeField] private Button closeCannonButton;
////////////    [SerializeField] private Button closeCastleButton;
////////////    [SerializeField] private Button closeSettingsButton;

////////////    // ─── Army Panel Buttons ───────────────────────────────────────────────────
////////////    [Header("Army Panel")]
////////////    [SerializeField] private Button buyBasicSoldierButton;

////////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
////////////    private void Awake()
////////////    {
////////////        if (Instance != null && Instance != this)
////////////        {
////////////            Destroy(gameObject);
////////////            return;
////////////        }
////////////        Instance = this;
////////////    }

////////////    private void Start()
////////////    {
////////////        WireButtons();
////////////        SubscribeToEvents();
////////////    }

////////////    private void OnDestroy()
////////////    {
////////////        UnsubscribeFromEvents();
////////////    }

////////////    // ─── Button Wiring ────────────────────────────────────────────────────────

////////////    private void WireButtons()
////////////    {
////////////        // ── Navigation buttons ──────────────────────────────────────────────
////////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////////////        // ── Close / Back buttons ─────────────────────────────────────────────
////////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());

////////////        // ── Army Panel actions ───────────────────────────────────────────────
////////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////////////    }

////////////    // ─── Army Panel Logic ─────────────────────────────────────────────────────

////////////    private void OnBuySoldierClicked()
////////////    {
////////////        // Spawn the soldier in the village
////////////        GameManager.Instance.SpawnBasicSoldier();

////////////        // Close the army panel — return to village to see the soldier
////////////        GameManager.Instance.CloseCurrentPanel();

////////////        Debug.Log("[UIManager] Buy button clicked — soldier spawned, returning to village.");
////////////    }

////////////    // ─── Event Subscriptions ─────────────────────────────────────────────────

////////////    private void SubscribeToEvents()
////////////    {
////////////        GameManager.OnGameStateChanged += HandleStateChanged;
////////////    }

////////////    private void UnsubscribeFromEvents()
////////////    {
////////////        GameManager.OnGameStateChanged -= HandleStateChanged;
////////////    }

////////////    private void HandleStateChanged(GameManager.GameState newState)
////////////    {
////////////        // You can update button interactability, highlight active tab, etc.
////////////        // Example: disable the army button while in army panel
////////////        if (armyButton != null)
////////////            armyButton.interactable = (newState != GameManager.GameState.Army);
////////////    }

////////////    // ─── Utility ─────────────────────────────────────────────────────────────

////////////    /// <summary>
////////////    /// Safe AddListener — skips null buttons with a warning.
////////////    /// </summary>
////////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////////////    {
////////////        if (button != null)
////////////            button.onClick.AddListener(action);
////////////        else
////////////            Debug.LogWarning($"[UIManager] A button reference is null. Check Inspector assignments.");
////////////    }
////////////}

//////////using UnityEngine;
//////////using UnityEngine.UI;

///////////// <summary>
///////////// AREA FORGE - UIManager (Singleton)
///////////// Wires all UI buttons to GameManager actions.
///////////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
///////////// </summary>
//////////public class UIManager : MonoBehaviour
//////////{
//////////    public static UIManager Instance { get; private set; }

//////////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
//////////    [Header("Main Navigation Buttons")]
//////////    [SerializeField] private Button settingsButton;
//////////    [SerializeField] private Button armyButton;
//////////    [SerializeField] private Button cannonButton;
//////////    [SerializeField] private Button castleButton;

//////////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
//////////    [Header("Panel Close Buttons")]
//////////    [SerializeField] private Button closeArmyButton;
//////////    [SerializeField] private Button closeCannonButton;
//////////    [SerializeField] private Button closeCastleButton;
//////////    [SerializeField] private Button closeSettingsButton;

//////////    /// <summary>
//////////    /// The close button INSIDE the Horse panel.
//////////    /// Drag the HorsePanle close button here — it calls CloseCurrentPanel()
//////////    /// so GameManager tracks the state change correctly.
//////////    /// </summary>
//////////    [SerializeField] private Button closeHorseButton;

//////////    // ─── Army Panel Buttons ───────────────────────────────────────────────────
//////////    [Header("Army Panel")]
//////////    [SerializeField] private Button buyBasicSoldierButton;

//////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//////////    private void Awake()
//////////    {
//////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////        Instance = this;
//////////    }

//////////    private void Start()
//////////    {
//////////        WireButtons();
//////////        SubscribeToEvents();
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        UnsubscribeFromEvents();
//////////    }

//////////    // ─── Button Wiring ────────────────────────────────────────────────────────

//////////    private void WireButtons()
//////////    {
//////////        // Navigation
//////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//////////        // Close / Back — all route through CloseCurrentPanel so GameManager
//////////        // always owns the state transition (handles overlays correctly)
//////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//////////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//////////        // Army
//////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//////////    }

//////////    // ─── Army Panel Logic ─────────────────────────────────────────────────────

//////////    private void OnBuySoldierClicked()
//////////    {
//////////        GameManager.Instance.SpawnBasicSoldier();
//////////        GameManager.Instance.CloseCurrentPanel();
//////////        Debug.Log("[UIManager] Soldier spawned, returning to village.");
//////////    }

//////////    // ─── Event Subscriptions ─────────────────────────────────────────────────

//////////    private void SubscribeToEvents()
//////////    {
//////////        GameManager.OnGameStateChanged += HandleStateChanged;
//////////    }

//////////    private void UnsubscribeFromEvents()
//////////    {
//////////        GameManager.OnGameStateChanged -= HandleStateChanged;
//////////    }

//////////    private void HandleStateChanged(GameManager.GameState newState)
//////////    {
//////////        // Disable nav buttons for the panel that's currently active
//////////        // so the player can't re-open a panel they're already in
//////////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//////////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//////////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//////////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
//////////    }

//////////    // ─── Utility ─────────────────────────────────────────────────────────────

//////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//////////    {
//////////        if (button != null)
//////////            button.onClick.AddListener(action);
//////////        else
//////////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector assignments.");
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

////////public class UIManager : MonoBehaviour
////////{
////////    public static UIManager Instance { get; private set; }

////////    [Header("Main Navigation Buttons")]
////////    [SerializeField] private Button settingsButton;
////////    [SerializeField] private Button armyButton;
////////    [SerializeField] private Button cannonButton;
////////    [SerializeField] private Button castleButton;

////////    [Header("Panel Close Buttons")]
////////    [SerializeField] private Button closeArmyButton;
////////    [SerializeField] private Button closeCannonButton;
////////    [SerializeField] private Button closeCastleButton;
////////    [SerializeField] private Button closeSettingsButton;
////////    [SerializeField] private Button closeHorseButton;

////////    [Header("Army Panel")]
////////    [SerializeField] private Button buyBasicSoldierButton;

////////    // ─── Lifecycle ────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;

////////        // ── Wire buttons in Awake, not Start ──────────────────────────────────
////////        // Start() has execution-order problems — other managers may hide panels
////////        // before Start() runs, causing listeners to never be registered.
////////        WireButtons();
////////        SubscribeToEvents();
////////    }

////////    private void OnDestroy()
////////    {
////////        UnsubscribeFromEvents();
////////    }

////////    // ─── Button Wiring ────────────────────────────────────────────────────────

////////    private void WireButtons()
////////    {
////////        // Navigation
////////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////////        // Close / Back
////////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
////////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

////////        // Army
////////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////////    }

////////    // ─── Army ─────────────────────────────────────────────────────────────────

////////    private void OnBuySoldierClicked()
////////    {
////////        GameManager.Instance.SpawnBasicSoldier();
////////        GameManager.Instance.CloseCurrentPanel();
////////    }

////////    // ─── Events ───────────────────────────────────────────────────────────────

////////    private void SubscribeToEvents()
////////    {
////////        GameManager.OnGameStateChanged += HandleStateChanged;
////////    }

////////    private void UnsubscribeFromEvents()
////////    {
////////        GameManager.OnGameStateChanged -= HandleStateChanged;
////////    }

////////    private void HandleStateChanged(GameManager.GameState newState)
////////    {
////////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
////////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
////////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
////////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
////////    }

////////    // ─── Utility ─────────────────────────────────────────────────────────────

////////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////////    {
////////        if (button != null)
////////            button.onClick.AddListener(action);
////////        else
////////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// AREA FORGE - UIManager (Singleton)
///////// Wires all UI buttons to GameManager actions.
///////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
///////// </summary>
//////public class UIManager : MonoBehaviour
//////{
//////    public static UIManager Instance { get; private set; }

//////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
//////    [Header("Main Navigation Buttons")]
//////    [SerializeField] private Button settingsButton;
//////    [SerializeField] private Button armyButton;
//////    [SerializeField] private Button cannonButton;
//////    [SerializeField] private Button castleButton;

//////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
//////    [Header("Panel Close Buttons")]
//////    [SerializeField] private Button closeArmyButton;
//////    [SerializeField] private Button closeCannonButton;
//////    [SerializeField] private Button closeCastleButton;
//////    [SerializeField] private Button closeSettingsButton;
//////    [SerializeField] private Button closeHorseButton;

//////    [Tooltip("The X / Close button INSIDE the InventoryPanel.")]
//////    [SerializeField] private Button closeCustomizeButton;

//////    // ─── Customize Panel ──────────────────────────────────────────────────────
//////    [Header("Customize Panel")]
//////    [Tooltip("The button in the HUD / Village that opens the Customize panel.\n" +
//////             "In your screenshot this is the 'PLAYER' button on the left.")]
//////    [SerializeField] private Button openCustomizeButton;

//////    // ─── Army Panel ───────────────────────────────────────────────────────────
//////    [Header("Army Panel")]
//////    [SerializeField] private Button buyBasicSoldierButton;

//////    // ─── Lifecycle ────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;

//////        // Wire in Awake — avoids execution-order issues with Start()
//////        WireButtons();
//////        SubscribeToEvents();
//////    }

//////    private void OnDestroy()
//////    {
//////        UnsubscribeFromEvents();
//////    }

//////    // ─── Button Wiring ────────────────────────────────────────────────────────

//////    private void WireButtons()
//////    {
//////        // ── Navigation ──────────────────────────────────────────────────────
//////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//////        // ── Close / Back — all routed through CloseCurrentPanel ─────────────
//////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());
//////        AddListener(closeCustomizeButton, () => GameManager.Instance.CloseCurrentPanel());

//////        // ── Customize panel ──────────────────────────────────────────────────
//////        AddListener(openCustomizeButton, () => GameManager.Instance.OpenCustomizePanel());

//////        // ── Army ─────────────────────────────────────────────────────────────
//////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//////    }

//////    // ─── Army ─────────────────────────────────────────────────────────────────

//////    private void OnBuySoldierClicked()
//////    {
//////        GameManager.Instance.SpawnBasicSoldier();
//////        GameManager.Instance.CloseCurrentPanel();
//////    }

//////    // ─── Events ───────────────────────────────────────────────────────────────

//////    private void SubscribeToEvents()
//////    {
//////        GameManager.OnGameStateChanged += HandleStateChanged;
//////    }

//////    private void UnsubscribeFromEvents()
//////    {
//////        GameManager.OnGameStateChanged -= HandleStateChanged;
//////    }

//////    private void HandleStateChanged(GameManager.GameState newState)
//////    {
//////        // Disable nav buttons for whichever panel is currently active
//////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);

//////        // Disable the open-customize button while the panel is already open
//////        if (openCustomizeButton != null)
//////            openCustomizeButton.interactable = (newState != GameManager.GameState.Customize);
//////    }

//////    // ─── Utility ─────────────────────────────────────────────────────────────

//////    /// <summary>Safe AddListener — logs a warning for any unassigned button.</summary>
//////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//////    {
//////        if (button != null)
//////            button.onClick.AddListener(action);
//////        else
//////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - UIManager (Singleton)
/////// Wires all UI buttons to GameManager actions.
/////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
///////
/////// NOTE: armyButton opens the Customize / InventoryPanel (they are the same panel).
/////// Drag the button that opens your Customize panel into the armyButton field.
/////// </summary>
////public class UIManager : MonoBehaviour
////{
////    public static UIManager Instance { get; private set; }

////    // ─── Main Navigation Buttons ──────────────────────────────────────────────
////    [Header("Main Navigation Buttons")]
////    [SerializeField] private Button settingsButton;

////    [Tooltip("This opens the Customize / InventoryPanel. Drag your Army / Customize open button here.")]
////    [SerializeField] private Button armyButton;

////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button castleButton;

////    // ─── Close / Back Buttons ─────────────────────────────────────────────────
////    [Header("Panel Close Buttons")]
////    [Tooltip("The close / back button inside the Customize (Army) panel.")]
////    [SerializeField] private Button closeArmyButton;

////    [SerializeField] private Button closeCannonButton;
////    [SerializeField] private Button closeCastleButton;
////    [SerializeField] private Button closeSettingsButton;
////    [SerializeField] private Button closeHorseButton;

////    // ─── Army Panel ───────────────────────────────────────────────────────────
////    [Header("Army Panel")]
////    [SerializeField] private Button buyBasicSoldierButton;

////    // ─── Lifecycle ────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        // Wire in Awake — avoids execution-order issues with Start()
////        WireButtons();
////        SubscribeToEvents();
////    }

////    private void OnDestroy()
////    {
////        UnsubscribeFromEvents();
////    }

////    // ─── Button Wiring ────────────────────────────────────────────────────────

////    private void WireButtons()
////    {
////        // ── Navigation ──────────────────────────────────────────────────────
////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());   // opens Customize panel
////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////        // ── Close / Back — all routed through CloseCurrentPanel ─────────────
////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

////        // ── Army / Customize ─────────────────────────────────────────────────
////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////    }

////    // ─── Army / Customize Logic ───────────────────────────────────────────────

////    private void OnBuySoldierClicked()
////    {
////        GameManager.Instance.SpawnBasicSoldier();
////        GameManager.Instance.CloseCurrentPanel();
////    }

////    // ─── Events ───────────────────────────────────────────────────────────────

////    private void SubscribeToEvents()
////    {
////        GameManager.OnGameStateChanged += HandleStateChanged;
////    }

////    private void UnsubscribeFromEvents()
////    {
////        GameManager.OnGameStateChanged -= HandleStateChanged;
////    }

////    private void HandleStateChanged(GameManager.GameState newState)
////    {
////        // Disable nav buttons for whichever panel is currently active
////        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
////        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
////        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
////        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
////    }

////    // ─── Utility ─────────────────────────────────────────────────────────────

////    /// <summary>Safe AddListener — logs a warning for any unassigned button.</summary>
////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////    {
////        if (button != null)
////            button.onClick.AddListener(action);
////        else
////            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - UIManager (Singleton)
///// Wires all UI buttons to GameManager actions.
///// Also drives CastleGridMover when the Castle Panel opens or closes.
///// MULTIPLAYER NOTE: UI is always local — no sync needed here.
///// </summary>
//public class UIManager : MonoBehaviour
//{
//    public static UIManager Instance { get; private set; }

//    // ─── Main Navigation Buttons ──────────────────────────────────────────────
//    [Header("Main Navigation Buttons")]
//    [SerializeField] private Button settingsButton;
//    [SerializeField] private Button armyButton;
//    [SerializeField] private Button cannonButton;
//    [SerializeField] private Button castleButton;

//    // ─── Close / Back Buttons ─────────────────────────────────────────────────
//    [Header("Panel Close Buttons")]
//    [SerializeField] private Button closeArmyButton;
//    [SerializeField] private Button closeCannonButton;
//    [SerializeField] private Button closeCastleButton;   // also closes Castle Panel grid
//    [SerializeField] private Button closeSettingsButton;
//    [SerializeField] private Button closeHorseButton;

//    // ─── Army Panel ───────────────────────────────────────────────────────────
//    [Header("Army Panel")]
//    [SerializeField] private Button buyBasicSoldierButton;

//    // ─── Lifecycle ────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        WireButtons();
//        SubscribeToEvents();
//    }

//    private void OnDestroy()
//    {
//        UnsubscribeFromEvents();
//    }

//    // ─── Button Wiring ────────────────────────────────────────────────────────

//    private void WireButtons()
//    {
//        // ── Navigation ──────────────────────────────────────────────────────
//        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());

//        // Castle button: open GameManager state AND move the grid into Castle Panel
//        AddListener(castleButton, () =>
//        {
//            GameManager.Instance.OpenCastlePanel();
//            if (CastleGridMover.Instance != null)
//                CastleGridMover.Instance.OpenCastlePanel();
//            else
//                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move.");
//        });

//        // ── Close / Back buttons ─────────────────────────────────────────────
//        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//        // Castle close: restore game state AND move grid back to Village Panel
//        AddListener(closeCastleButton, () =>
//        {
//            GameManager.Instance.CloseCurrentPanel();
//            if (CastleGridMover.Instance != null)
//                CastleGridMover.Instance.OpenVillagePanel();
//            else
//                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move back.");
//        });

//        // ── Army ─────────────────────────────────────────────────────────────
//        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//    }

//    // ─── Army Logic ───────────────────────────────────────────────────────────

//    private void OnBuySoldierClicked()
//    {
//        GameManager.Instance.SpawnBasicSoldier();
//        GameManager.Instance.CloseCurrentPanel();
//    }

//    // ─── Events ───────────────────────────────────────────────────────────────

//    private void SubscribeToEvents()
//    {
//        GameManager.OnGameStateChanged += HandleStateChanged;
//    }

//    private void UnsubscribeFromEvents()
//    {
//        GameManager.OnGameStateChanged -= HandleStateChanged;
//    }

//    private void HandleStateChanged(GameManager.GameState newState)
//    {
//        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
//        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
//        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
//        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
//    }

//    // ─── Utility ─────────────────────────────────────────────────────────────

//    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
//    {
//        if (button != null)
//            button.onClick.AddListener(action);
//        else
//            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - UIManager (Singleton)
/// Wires all UI buttons to GameManager actions.
/// Also drives CastleGridMover when the Castle Panel opens or closes.
///
/// ── FIX: Buying a cannon or soldier UNIT (the draggable castle units) must
///         NOT call OpenCastlePanel. BuyCastleCannonUnit / BuyCastleSoldierUnit
///         stay on the current panel so the player keeps their context.
///         Wire the buy buttons for those units to these methods.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ─── Main Navigation Buttons ──────────────────────────────────────────────
    [Header("Main Navigation Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button armyButton;
    [SerializeField] private Button cannonButton;
    [SerializeField] private Button castleButton;

    // ─── Close / Back Buttons ─────────────────────────────────────────────────
    [Header("Panel Close Buttons")]
    [SerializeField] private Button closeArmyButton;
    [SerializeField] private Button closeCannonButton;
    [SerializeField] private Button closeCastleButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button closeHorseButton;

    // ─── Army Panel ───────────────────────────────────────────────────────────
    [Header("Army Panel")]
    [SerializeField] private Button buyBasicSoldierButton;

    // ─── Castle Unit Shop ─────────────────────────────────────────────────────
    // These are the draggable cannon / soldier UNIT buy buttons that live in
    // whatever panel shows unit inventory (VillagePanel, ArmyPanel, etc.).
    //
    // FIX: Wire these here instead of wiring them directly to
    //      GameManager.OpenCastlePanel() in the Inspector — that's what caused
    //      buying a unit to accidentally navigate to the Castle Panel.
    [Header("Castle Unit Buy Buttons (stay on current panel)")]
    [Tooltip("Button that purchases / spawns a Cannon unit draggable. " +
             "Does NOT navigate — remove any OpenCastlePanel call from its Inspector onClick.")]
    [SerializeField] private Button buyCastleCannonButton;

    [Tooltip("Button that purchases / spawns a Soldier unit draggable. " +
             "Does NOT navigate — remove any OpenCastlePanel call from its Inspector onClick.")]
    [SerializeField] private Button buyCastleSoldierButton;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        WireButtons();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ─── Button Wiring ────────────────────────────────────────────────────────

    private void WireButtons()
    {
        // ── Navigation ──────────────────────────────────────────────────────
        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());

        // Castle button: open GameManager state AND move the grid into view
        AddListener(castleButton, () =>
        {
            GameManager.Instance.OpenCastlePanel();
            if (CastleGridMover.Instance != null)
                CastleGridMover.Instance.OpenCastlePanel();
            else
                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move.");
        });

        // ── Close / Back ─────────────────────────────────────────────────────
        AddListener(closeArmyButton, () =>
        {
            if (ArcherZoneCastle.PendingArcherBuy)
            {
                // Came from clicking an ArcherZone — go back to Castle + Archer tab,
                // not the Village panel.
                ArcherZoneCastle.ClearPendingArcherBuy();
                GameManager.Instance.OpenCastlePanelWithArcherTab();
            }
            else
            {
                GameManager.Instance.CloseCurrentPanel();
            }
        });
        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

        // Castle close: restore game state AND move grid back
        AddListener(closeCastleButton, () =>
        {
            GameManager.Instance.CloseCurrentPanel();
            if (CastleGridMover.Instance != null)
                CastleGridMover.Instance.OpenVillagePanel();
            else
                Debug.LogWarning("[UIManager] CastleGridMover.Instance is null — grid won't move back.");
        });

        // ── Army ─────────────────────────────────────────────────────────────
        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);

        // ── Castle unit shop ─────────────────────────────────────────────────
        // FIX: These handlers stay on the current panel.  Do NOT add
        //      GameManager.OpenCastlePanel() here or in the Inspector onClick.
        AddListener(buyCastleCannonButton, OnBuyCastleCannonClicked);
        AddListener(buyCastleSoldierButton, OnBuyCastleSoldierClicked);
    }

    // ─── Army Logic ───────────────────────────────────────────────────────────

    private void OnBuySoldierClicked()
    {
        GameManager.Instance.SpawnBasicSoldier();

        if (ArcherZoneCastle.PendingArcherBuy)
        {
            // Player came from clicking an ArcherZone → return to Castle panel
            // with the Archer tab active so they can drag the new soldier in.
            ArcherZoneCastle.ClearPendingArcherBuy();
            GameManager.Instance.OpenCastlePanelWithArcherTab();
        }
        else
        {
            // Normal army-panel buy → return to village.
            GameManager.Instance.CloseCurrentPanel();
        }
    }

    // ─── Castle Unit Buy Logic ────────────────────────────────────────────────

    /// <summary>
    /// Called when the player buys a Cannon UNIT (draggable for the castle grid).
    /// Stays on the current panel — does NOT navigate to the Castle Panel.
    /// Add your spawn / inventory logic here (e.g. CastleUnitInventory.Add(Cannon)).
    /// </summary>
    private void OnBuyCastleCannonClicked()
    {
        // TODO: replace with your actual cannon-unit grant logic, e.g.:
        // CastleUnitInventory.Instance.AddUnit(CastleUnitType.Cannon);
        Debug.Log("[UIManager] Cannon unit purchased — staying on current panel.");

        // Deliberately NOT calling GameManager.Instance.OpenCastlePanel() here.
        // The player remains wherever they were when they clicked buy.
    }

    /// <summary>
    /// Called when the player buys a Soldier UNIT (draggable for the castle grid).
    /// Stays on the current panel — does NOT navigate to the Castle Panel.
    /// </summary>
    private void OnBuyCastleSoldierClicked()
    {
        // TODO: replace with your actual soldier-unit grant logic, e.g.:
        // CastleUnitInventory.Instance.AddUnit(CastleUnitType.Soldier);
        Debug.Log("[UIManager] Soldier unit purchased — staying on current panel.");

        // Deliberately NOT calling GameManager.Instance.OpenCastlePanel() here.
    }

    // ─── Events ───────────────────────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
    }

    private void UnsubscribeFromEvents()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState newState)
    {
        if (armyButton != null) armyButton.interactable = (newState != GameManager.GameState.Army);
        if (cannonButton != null) cannonButton.interactable = (newState != GameManager.GameState.Cannon);
        if (castleButton != null) castleButton.interactable = (newState != GameManager.GameState.Castle);
        if (settingsButton != null) settingsButton.interactable = (newState != GameManager.GameState.Settings);
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
        else
            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector.");
    }
}