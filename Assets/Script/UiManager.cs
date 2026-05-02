////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// AREA FORGE - UIManager (Singleton)
/////// Wires all UI buttons to GameManager actions.
/////// Assign button references in the Inspector, then this script does the rest.
/////// MULTIPLAYER NOTE: UI is always local — no sync needed here.
/////// </summary>
////public class UIManager : MonoBehaviour
////{
////    // ─── Singleton ────────────────────────────────────────────────────────────
////    public static UIManager Instance { get; private set; }

////    // ─── Main Navigation Buttons (always visible on Village panel) ────────────
////    [Header("Main Navigation Buttons")]
////    [SerializeField] private Button settingsButton;
////    [SerializeField] private Button armyButton;
////    [SerializeField] private Button cannonButton;
////    [SerializeField] private Button castleButton;

////    // ─── Close / Back Buttons (one per panel) ─────────────────────────────────
////    [Header("Panel Close Buttons")]
////    [SerializeField] private Button closeArmyButton;
////    [SerializeField] private Button closeCannonButton;
////    [SerializeField] private Button closeCastleButton;
////    [SerializeField] private Button closeSettingsButton;

////    // ─── Army Panel Buttons ───────────────────────────────────────────────────
////    [Header("Army Panel")]
////    [SerializeField] private Button buyBasicSoldierButton;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
////    private void Awake()
////    {
////        if (Instance != null && Instance != this)
////        {
////            Destroy(gameObject);
////            return;
////        }
////        Instance = this;
////    }

////    private void Start()
////    {
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
////        // ── Navigation buttons ──────────────────────────────────────────────
////        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
////        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
////        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
////        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

////        // ── Close / Back buttons ─────────────────────────────────────────────
////        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
////        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());

////        // ── Army Panel actions ───────────────────────────────────────────────
////        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
////    }

////    // ─── Army Panel Logic ─────────────────────────────────────────────────────

////    private void OnBuySoldierClicked()
////    {
////        // Spawn the soldier in the village
////        GameManager.Instance.SpawnBasicSoldier();

////        // Close the army panel — return to village to see the soldier
////        GameManager.Instance.CloseCurrentPanel();

////        Debug.Log("[UIManager] Buy button clicked — soldier spawned, returning to village.");
////    }

////    // ─── Event Subscriptions ─────────────────────────────────────────────────

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
////        // You can update button interactability, highlight active tab, etc.
////        // Example: disable the army button while in army panel
////        if (armyButton != null)
////            armyButton.interactable = (newState != GameManager.GameState.Army);
////    }

////    // ─── Utility ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Safe AddListener — skips null buttons with a warning.
////    /// </summary>
////    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
////    {
////        if (button != null)
////            button.onClick.AddListener(action);
////        else
////            Debug.LogWarning($"[UIManager] A button reference is null. Check Inspector assignments.");
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - UIManager (Singleton)
///// Wires all UI buttons to GameManager actions.
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
//    [SerializeField] private Button closeCastleButton;
//    [SerializeField] private Button closeSettingsButton;

//    /// <summary>
//    /// The close button INSIDE the Horse panel.
//    /// Drag the HorsePanle close button here — it calls CloseCurrentPanel()
//    /// so GameManager tracks the state change correctly.
//    /// </summary>
//    [SerializeField] private Button closeHorseButton;

//    // ─── Army Panel Buttons ───────────────────────────────────────────────────
//    [Header("Army Panel")]
//    [SerializeField] private Button buyBasicSoldierButton;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//    }

//    private void Start()
//    {
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
//        // Navigation
//        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
//        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
//        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
//        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

//        // Close / Back — all route through CloseCurrentPanel so GameManager
//        // always owns the state transition (handles overlays correctly)
//        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
//        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

//        // Army
//        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
//    }

//    // ─── Army Panel Logic ─────────────────────────────────────────────────────

//    private void OnBuySoldierClicked()
//    {
//        GameManager.Instance.SpawnBasicSoldier();
//        GameManager.Instance.CloseCurrentPanel();
//        Debug.Log("[UIManager] Soldier spawned, returning to village.");
//    }

//    // ─── Event Subscriptions ─────────────────────────────────────────────────

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
//        // Disable nav buttons for the panel that's currently active
//        // so the player can't re-open a panel they're already in
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
//            Debug.LogWarning("[UIManager] A button reference is null. Check Inspector assignments.");
//    }
//}

using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Main Navigation Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button armyButton;
    [SerializeField] private Button cannonButton;
    [SerializeField] private Button castleButton;

    [Header("Panel Close Buttons")]
    [SerializeField] private Button closeArmyButton;
    [SerializeField] private Button closeCannonButton;
    [SerializeField] private Button closeCastleButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button closeHorseButton;

    [Header("Army Panel")]
    [SerializeField] private Button buyBasicSoldierButton;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // ── Wire buttons in Awake, not Start ──────────────────────────────────
        // Start() has execution-order problems — other managers may hide panels
        // before Start() runs, causing listeners to never be registered.
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
        // Navigation
        AddListener(settingsButton, () => GameManager.Instance.OpenSettingsPanel());
        AddListener(armyButton, () => GameManager.Instance.OpenArmyPanel());
        AddListener(cannonButton, () => GameManager.Instance.OpenCannonPanel());
        AddListener(castleButton, () => GameManager.Instance.OpenCastlePanel());

        // Close / Back
        AddListener(closeArmyButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeCannonButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeCastleButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeSettingsButton, () => GameManager.Instance.CloseCurrentPanel());
        AddListener(closeHorseButton, () => GameManager.Instance.CloseCurrentPanel());

        // Army
        AddListener(buyBasicSoldierButton, OnBuySoldierClicked);
    }

    // ─── Army ─────────────────────────────────────────────────────────────────

    private void OnBuySoldierClicked()
    {
        GameManager.Instance.SpawnBasicSoldier();
        GameManager.Instance.CloseCurrentPanel();
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