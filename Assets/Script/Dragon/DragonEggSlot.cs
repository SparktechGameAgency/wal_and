using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DRAGON AREA — DragonEggSlot
///
/// Attach to the DragonArea root GameObject inside VillagePanel.
///
/// ════════════════════════════════════════════════════════════════════
///  STATES
/// ════════════════════════════════════════════════════════════════════
///
///  Empty      No egg. EmptySlot (with AddEggButton) is shown.
///             Timer display is hidden.
///
///  Hatching   Egg sits in the slot. Timer counts down at the TOP of
///             the area. Nothing else happens until it hits zero.
///
///  Cracking   Timer hits zero → timer display hides → egg crack
///             animation plays for crackAnimationDuration seconds.
///
///  Hatched    Crack animation done → egg hidden → dragon prefab
///             instantiated (or pre-placed DragonObject activated) →
///             dragon idle animation plays.
///
/// ════════════════════════════════════════════════════════════════════
///  REQUIRED HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   DragonArea              ← DragonEggSlot.cs lives here
///   ├── TimerDisplay        ← sits at the TOP of the area
///   │   └── TimerText       TextMeshProUGUI  "00:45"
///   ├── EmptySlot           shown while no egg is placed
///   │   └── AddEggButton    Button the player taps
///   └── EggObject           egg sprite + Animator (crack clip)
///
///  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
///  child of DragonArea when hatching finishes.
///  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
///   of dragonPrefab — the script handles both.)
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR FIELDS
/// ════════════════════════════════════════════════════════════════════
///
///  dragonData            DragonData ScriptableObject asset
///
///  timerDisplay          parent GO of the timer (sits at top of area)
///  timerText             TMP label inside timerDisplay
///
///  emptySlotRoot         EmptySlot GO
///  addEggButton          Button inside EmptySlot
///
///  eggRoot               EggObject GO  (has an Animator)
///  eggAnimator           Animator on EggObject
///
///  dragonPrefab          prefab to Instantiate when hatched
///  dragonSpawnPoint      Transform where the prefab is placed
///                        (defaults to DragonArea centre if left blank)
///
///  dragonObjectOverride  optional: pre-placed DragonObject to activate
///                        instead of instantiating a prefab
/// </summary>
public class DragonEggSlot : MonoBehaviour
{
    // ── Data ───────────────────────────────────────────────────────────────────
    [Header("Dragon Data")]
    [SerializeField] private DragonData dragonData;

    // ── Timer (top of area) ────────────────────────────────────────────────────
    [Header("Timer — placed at the top of DragonArea")]
    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

    // ── Empty Slot ─────────────────────────────────────────────────────────────
    [Header("Empty Slot")]
    [SerializeField] private GameObject emptySlotRoot;
    [SerializeField] private Button addEggButton;

    // ── Egg ────────────────────────────────────────────────────────────────────
    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
    [SerializeField] private GameObject eggRoot;
    [SerializeField] private Animator eggAnimator;

    // ── Fence ──────────────────────────────────────────────────────────────────
    [Header("Fence — activates with the egg, deactivates when dragon hatches")]
    [SerializeField] private GameObject fenceRoot;

    // ── Nests ──────────────────────────────────────────────────────────────────
    [Header("Nests — both visible in Empty and Hatching, deactivated when dragon hatches")]
    [SerializeField] private GameObject nestRoot1;
    [SerializeField] private GameObject nestRoot2;

    // ── Dragon ─────────────────────────────────────────────────────────────────
    [Header("Dragon — prefab spawned when hatched")]
    [Tooltip("Prefab to Instantiate when the egg hatches.")]
    [SerializeField] private GameObject dragonPrefab;
    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
    [SerializeField] private Transform dragonSpawnPoint;
    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
    [SerializeField] private GameObject dragonObjectOverride;

    // ── State ──────────────────────────────────────────────────────────────────
    public enum SlotState { Empty, Hatching, Cracking, Hatched }
    public SlotState CurrentState { get; private set; } = SlotState.Empty;

    // All timing is driven by Update() using real wall-clock Time.time so that
    // villagePanel.SetActive(false/true) panel switches cannot kill a coroutine
    // and freeze the timer or skip the hatch.
    private float _hatchEndTime;       // Time.time when hatching finishes
    private float _crackEndTime;       // Time.time when crack animation finishes
    private bool _crackTriggered;     // true once the crack Animator trigger has fired

    private GameObject _spawnedDragon;

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        AutoWireChildren();
        addEggButton?.onClick.AddListener(OnAddEggClicked);
    }

    private void Start()
    {
        EnterEmpty();
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case SlotState.Hatching:
                float remaining = _hatchEndTime - Time.time;
                if (remaining <= 0f)
                {
                    UpdateTimerLabel(0f);
                    EnterCracking();
                }
                else
                {
                    UpdateTimerLabel(remaining);
                }
                break;

            case SlotState.Cracking:
                // Fire the crack trigger exactly once on the first Update after
                // entering this state (guarantees the Animator has been active
                // for at least one frame before we poke it).
                if (!_crackTriggered)
                {
                    if (eggAnimator != null && dragonData != null &&
                        !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
                        eggAnimator.SetTrigger(dragonData.eggCrackTrigger);
                    _crackTriggered = true;
                }

                if (Time.time >= _crackEndTime)
                    EnterHatched();
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
    // ══════════════════════════════════════════════════════════════════════════

    private void AutoWireChildren()
    {
        Transform t = transform;

        // Timer
        if (timerDisplay == null)
        {
            var td = t.Find("TimerDisplay");
            if (td != null)
            {
                timerDisplay = td.gameObject;
                if (timerText == null)
                {
                    var tt = td.Find("TimerText");
                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Empty Slot
        if (emptySlotRoot == null)
        {
            var es = t.Find("EmptySlot");
            if (es != null)
            {
                emptySlotRoot = es.gameObject;
                if (addEggButton == null)
                {
                    var btn = es.Find("AddEggButton");
                    if (btn != null) addEggButton = btn.GetComponent<Button>();
                }
            }
        }

        // Egg
        if (eggRoot == null)
        {
            var egg = t.Find("EggObject");
            if (egg != null)
            {
                eggRoot = egg.gameObject;
                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
            }
        }

        // Fence
        if (fenceRoot == null)
        {
            var fence = t.Find("Fench");
            if (fence != null) fenceRoot = fence.gameObject;
        }

        // Nests
        if (nestRoot1 == null)
        {
            var n1 = t.Find("Nest1");
            if (n1 != null) nestRoot1 = n1.gameObject;
        }
        if (nestRoot2 == null)
        {
            var n2 = t.Find("Nest2");
            if (n2 != null) nestRoot2 = n2.gameObject;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUTTON CALLBACK
    // ══════════════════════════════════════════════════════════════════════════

    private void OnAddEggClicked()
    {
        if (CurrentState != SlotState.Empty) return;

        if (dragonData == null)
        {
            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
            return;
        }

        EnterHatching();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — EMPTY
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterEmpty()
    {
        CurrentState = SlotState.Empty;

        Show(emptySlotRoot, true);
        Show(nestRoot1, true);
        Show(nestRoot2, true);
        Show(eggRoot, false);
        Show(fenceRoot, false);

        // Show the dragon name label in empty state
        if (timerText != null)
            timerText.text = dragonData != null && !string.IsNullOrEmpty(dragonData.dragonName)
                ? dragonData.dragonName
                : "Dragon";
        Show(timerDisplay, true);

        // Hide any existing dragon
        if (_spawnedDragon != null) Destroy(_spawnedDragon);
        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

        Debug.Log("[DragonEggSlot] → Empty");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — HATCHING  (egg visible, timer counting down at top)
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterHatching()
    {
        CurrentState = SlotState.Hatching;
        _hatchEndTime = Time.time + dragonData.hatchDuration;

        Show(emptySlotRoot, false);
        Show(nestRoot1, true);
        Show(nestRoot2, true);
        Show(eggRoot, true);
        Show(fenceRoot, true);
        Show(timerDisplay, true);

        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — CRACKING  (timer done, crack animation plays)
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterCracking()
    {
        CurrentState = SlotState.Cracking;
        _crackTriggered = false;  // Update() will fire the trigger next frame
        _crackEndTime = Time.time + (dragonData != null ? dragonData.crackAnimationDuration : 1f);

        // Switch back to the dragon name — never show "00:00" to the player
        if (timerText != null)
            timerText.text = dragonData != null && !string.IsNullOrEmpty(dragonData.dragonName)
                ? dragonData.dragonName
                : "Dragon";
        Show(timerDisplay, true);

        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData?.crackAnimationDuration}s)");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterHatched()
    {
        CurrentState = SlotState.Hatched;

        // Hide egg, fence, nests when dragon appears
        Show(eggRoot, false);
        Show(fenceRoot, false);
        Show(nestRoot1, false);
        Show(nestRoot2, false);

        // Switch the label to the dragon name and keep the display visible
        if (timerText != null)
            timerText.text = dragonData != null && !string.IsNullOrEmpty(dragonData.dragonName)
                ? dragonData.dragonName
                : "Dragon";
        Show(timerDisplay, true);

        // Spawn or activate the dragon
        if (dragonObjectOverride != null)
        {
            dragonObjectOverride.SetActive(true);
            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
            _spawnedDragon = dragonObjectOverride;

            var dc = _spawnedDragon.GetComponent<DragonController>();
            if (dc != null) dc.homeSlot = this;
        }
        else if (dragonPrefab != null)
        {
            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());

            var dc = _spawnedDragon.GetComponent<DragonController>();
            if (dc != null) dc.homeSlot = this;
        }
        else
        {
            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
        }

        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void UpdateTimerLabel(float seconds)
    {
        if (timerText == null) return;
        float s = Mathf.Max(0f, seconds);
        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
    }

    private void TriggerDragonIdle(Animator anim)
    {
        if (anim == null) return;
        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
            anim.SetTrigger(dragonData.dragonIdleTrigger);
    }

    private static void Show(GameObject go, bool visible)
    {
        if (go != null) go.SetActive(visible);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════════

    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
    public void ResetSlot()
    {
        EnterEmpty();
    }

    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
    public float GetRemainingHatchTime()
        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

    // ══════════════════════════════════════════════════════════════════════════
    // EDITOR
    // ══════════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dragonData == null)
            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
        if (dragonPrefab == null && dragonObjectOverride == null)
            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
    }
#endif
}