//////using System.Collections;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// DRAGON AREA — DragonEggSlot
/////////
///////// Attach to the DragonArea root GameObject inside VillagePanel.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  STATES
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  Empty      No egg. EmptySlot (with AddEggButton) is shown.
/////////             Timer display is hidden.
/////////
/////////  Hatching   Egg sits in the slot. Timer counts down at the TOP of
/////////             the area. Nothing else happens until it hits zero.
/////////
/////////  Cracking   Timer hits zero → timer display hides → egg crack
/////////             animation plays for crackAnimationDuration seconds.
/////////
/////////  Hatched    Crack animation done → egg hidden → dragon prefab
/////////             instantiated (or pre-placed DragonObject activated) →
/////////             dragon idle animation plays.
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  REQUIRED HIERARCHY
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////   DragonArea              ← DragonEggSlot.cs lives here
/////////   ├── TimerDisplay        ← sits at the TOP of the area
/////////   │   └── TimerText       TextMeshProUGUI  "00:45"
/////////   ├── EmptySlot           shown while no egg is placed
/////////   │   └── AddEggButton    Button the player taps
/////////   └── EggObject           egg sprite + Animator (crack clip)
/////////
/////////  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
/////////  child of DragonArea when hatching finishes.
/////////  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
/////////   of dragonPrefab — the script handles both.)
/////////
///////// ════════════════════════════════════════════════════════════════════
/////////  INSPECTOR FIELDS
///////// ════════════════════════════════════════════════════════════════════
/////////
/////////  dragonData            DragonData ScriptableObject asset
/////////
/////////  timerDisplay          parent GO of the timer (sits at top of area)
/////////  timerText             TMP label inside timerDisplay
/////////
/////////  emptySlotRoot         EmptySlot GO
/////////  addEggButton          Button inside EmptySlot
/////////
/////////  eggRoot               EggObject GO  (has an Animator)
/////////  eggAnimator           Animator on EggObject
/////////
/////////  dragonPrefab          prefab to Instantiate when hatched
/////////  dragonSpawnPoint      Transform where the prefab is placed
/////////                        (defaults to DragonArea centre if left blank)
/////////
/////////  dragonObjectOverride  optional: pre-placed DragonObject to activate
/////////                        instead of instantiating a prefab
///////// </summary>
//////public class DragonEggSlot : MonoBehaviour
//////{
//////    // ── Data ───────────────────────────────────────────────────────────────────
//////    [Header("Dragon Data")]
//////    [SerializeField] private DragonData dragonData;

//////    // ── Timer (top of area) ────────────────────────────────────────────────────
//////    [Header("Timer — placed at the top of DragonArea")]
//////    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
//////    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

//////    // ── Empty Slot ─────────────────────────────────────────────────────────────
//////    [Header("Empty Slot")]
//////    [SerializeField] private GameObject emptySlotRoot;
//////    [SerializeField] private Button addEggButton;

//////    // ── Egg ────────────────────────────────────────────────────────────────────
//////    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
//////    [SerializeField] private GameObject eggRoot;
//////    [SerializeField] private Animator eggAnimator;

//////    // ── Dragon ─────────────────────────────────────────────────────────────────
//////    [Header("Dragon — prefab spawned when hatched")]
//////    [Tooltip("Prefab to Instantiate when the egg hatches.")]
//////    [SerializeField] private GameObject dragonPrefab;
//////    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
//////    [SerializeField] private Transform dragonSpawnPoint;
//////    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
//////    [SerializeField] private GameObject dragonObjectOverride;

//////    // ── State ──────────────────────────────────────────────────────────────────
//////    public enum SlotState { Empty, Hatching, Cracking, Hatched }
//////    public SlotState CurrentState { get; private set; } = SlotState.Empty;

//////    private float _hatchEndTime;
//////    private Coroutine _hatchCoroutine;
//////    private GameObject _spawnedDragon;   // reference to the live dragon instance

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UNITY LIFECYCLE
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        AutoWireChildren();
//////        addEggButton?.onClick.AddListener(OnAddEggClicked);
//////    }

//////    private void Start()
//////    {
//////        EnterEmpty();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void AutoWireChildren()
//////    {
//////        Transform t = transform;

//////        // Timer
//////        if (timerDisplay == null)
//////        {
//////            var td = t.Find("TimerDisplay");
//////            if (td != null)
//////            {
//////                timerDisplay = td.gameObject;
//////                if (timerText == null)
//////                {
//////                    var tt = td.Find("TimerText");
//////                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
//////                }
//////            }
//////        }

//////        // Empty Slot
//////        if (emptySlotRoot == null)
//////        {
//////            var es = t.Find("EmptySlot");
//////            if (es != null)
//////            {
//////                emptySlotRoot = es.gameObject;
//////                if (addEggButton == null)
//////                {
//////                    var btn = es.Find("AddEggButton");
//////                    if (btn != null) addEggButton = btn.GetComponent<Button>();
//////                }
//////            }
//////        }

//////        // Egg
//////        if (eggRoot == null)
//////        {
//////            var egg = t.Find("EggObject");
//////            if (egg != null)
//////            {
//////                eggRoot = egg.gameObject;
//////                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
//////            }
//////        }
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // BUTTON CALLBACK
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void OnAddEggClicked()
//////    {
//////        if (CurrentState != SlotState.Empty) return;

//////        if (dragonData == null)
//////        {
//////            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
//////            return;
//////        }

//////        EnterHatching();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — EMPTY
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterEmpty()
//////    {
//////        CurrentState = SlotState.Empty;

//////        Show(emptySlotRoot, true);
//////        Show(eggRoot, false);
//////        Show(timerDisplay, false);

//////        // Hide any existing dragon
//////        if (_spawnedDragon != null) Destroy(_spawnedDragon);
//////        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

//////        Debug.Log("[DragonEggSlot] → Empty");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — HATCHING  (egg visible, timer counting down at top)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterHatching()
//////    {
//////        CurrentState = SlotState.Hatching;
//////        _hatchEndTime = Time.time + dragonData.hatchDuration;

//////        Show(emptySlotRoot, false);  // hide the empty slot
//////        Show(eggRoot, true);   // show the egg in the slot
//////        Show(timerDisplay, true);   // show timer at top of area

//////        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
//////        _hatchCoroutine = StartCoroutine(HatchCountdown());

//////        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — CRACKING  (timer done, crack animation plays)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterCracking()
//////    {
//////        CurrentState = SlotState.Cracking;

//////        // Hide the timer — it's served its purpose
//////        Show(timerDisplay, false);
//////        if (timerText != null) timerText.text = "00:00";

//////        // Fire the crack animation on the egg
//////        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
//////            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

//////        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

//////        // Wait for the crack clip to finish, then hatch
//////        StartCoroutine(WaitForCrackThenHatch());
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void EnterHatched()
//////    {
//////        CurrentState = SlotState.Hatched;

//////        // Hide egg
//////        Show(eggRoot, false);

//////        // Spawn or activate the dragon
//////        if (dragonObjectOverride != null)
//////        {
//////            // Pre-placed GO path
//////            dragonObjectOverride.SetActive(true);
//////            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
//////            _spawnedDragon = dragonObjectOverride;
//////        }
//////        else if (dragonPrefab != null)
//////        {
//////            // Prefab instantiation path
//////            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
//////            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
//////            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());
//////        }
//////        else
//////        {
//////            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
//////        }

//////        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // COROUTINES
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// Counts down and updates the timer label every frame.
//////    /// When time is up it moves to Cracking state.
//////    private IEnumerator HatchCountdown()
//////    {
//////        while (true)
//////        {
//////            float remaining = _hatchEndTime - Time.time;

//////            if (remaining <= 0f)
//////            {
//////                UpdateTimerLabel(0f);
//////                break;
//////            }

//////            UpdateTimerLabel(remaining);
//////            yield return null;
//////        }

//////        _hatchCoroutine = null;
//////        EnterCracking();
//////    }

//////    /// Waits for the crack animation clip to finish, then hatches.
//////    private IEnumerator WaitForCrackThenHatch()
//////    {
//////        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
//////        EnterHatched();
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // HELPERS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void UpdateTimerLabel(float seconds)
//////    {
//////        if (timerText == null) return;
//////        float s = Mathf.Max(0f, seconds);
//////        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//////    }

//////    private void TriggerDragonIdle(Animator anim)
//////    {
//////        if (anim == null) return;
//////        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
//////            anim.SetTrigger(dragonData.dragonIdleTrigger);
//////        // If dragonIdleTrigger is blank the idle state plays automatically on entry
//////    }

//////    private static void Show(GameObject go, bool visible)
//////    {
//////        if (go != null) go.SetActive(visible);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // PUBLIC API
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
//////    public void ResetSlot()
//////    {
//////        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
//////        StopAllCoroutines();
//////        EnterEmpty();
//////    }

//////    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
//////    public float GetRemainingHatchTime()
//////        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // EDITOR
//////    // ══════════════════════════════════════════════════════════════════════════

//////#if UNITY_EDITOR
//////    private void OnValidate()
//////    {
//////        if (dragonData == null)
//////            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
//////        if (dragonPrefab == null && dragonObjectOverride == null)
//////            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
//////    }
//////#endif
//////}

////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// DRAGON AREA — DragonEggSlot
///////
/////// Attach to the DragonArea root GameObject inside VillagePanel.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  STATES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Empty      No egg. EmptySlot (with AddEggButton) is shown.
///////             Timer display is hidden.
///////
///////  Hatching   Egg sits in the slot. Timer counts down at the TOP of
///////             the area. Nothing else happens until it hits zero.
///////
///////  Cracking   Timer hits zero → timer display hides → egg crack
///////             animation plays for crackAnimationDuration seconds.
///////
///////  Hatched    Crack animation done → egg hidden → dragon prefab
///////             instantiated (or pre-placed DragonObject activated) →
///////             dragon idle animation plays.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  REQUIRED HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   DragonArea              ← DragonEggSlot.cs lives here
///////   ├── TimerDisplay        ← sits at the TOP of the area
///////   │   └── TimerText       TextMeshProUGUI  "00:45"
///////   ├── EmptySlot           shown while no egg is placed
///////   │   └── AddEggButton    Button the player taps
///////   └── EggObject           egg sprite + Animator (crack clip)
///////
///////  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
///////  child of DragonArea when hatching finishes.
///////  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
///////   of dragonPrefab — the script handles both.)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  INSPECTOR FIELDS
/////// ════════════════════════════════════════════════════════════════════
///////
///////  dragonData            DragonData ScriptableObject asset
///////
///////  timerDisplay          parent GO of the timer (sits at top of area)
///////  timerText             TMP label inside timerDisplay
///////
///////  emptySlotRoot         EmptySlot GO
///////  addEggButton          Button inside EmptySlot
///////
///////  eggRoot               EggObject GO  (has an Animator)
///////  eggAnimator           Animator on EggObject
///////
///////  dragonPrefab          prefab to Instantiate when hatched
///////  dragonSpawnPoint      Transform where the prefab is placed
///////                        (defaults to DragonArea centre if left blank)
///////
///////  dragonObjectOverride  optional: pre-placed DragonObject to activate
///////                        instead of instantiating a prefab
/////// </summary>
////public class DragonEggSlot : MonoBehaviour
////{
////    // ── Data ───────────────────────────────────────────────────────────────────
////    [Header("Dragon Data")]
////    [SerializeField] private DragonData dragonData;

////    // ── Timer (top of area) ────────────────────────────────────────────────────
////    [Header("Timer — placed at the top of DragonArea")]
////    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
////    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

////    // ── Empty Slot ─────────────────────────────────────────────────────────────
////    [Header("Empty Slot")]
////    [SerializeField] private GameObject emptySlotRoot;
////    [SerializeField] private Button addEggButton;

////    // ── Egg ────────────────────────────────────────────────────────────────────
////    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
////    [SerializeField] private GameObject eggRoot;
////    [SerializeField] private Animator eggAnimator;

////    // ── Fence ──────────────────────────────────────────────────────────────────
////    [Header("Fence — activates with the egg, deactivates when dragon hatches")]
////    [SerializeField] private GameObject fenceRoot;

////    // ── Dragon ─────────────────────────────────────────────────────────────────
////    [Header("Dragon — prefab spawned when hatched")]
////    [Tooltip("Prefab to Instantiate when the egg hatches.")]
////    [SerializeField] private GameObject dragonPrefab;
////    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
////    [SerializeField] private Transform dragonSpawnPoint;
////    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
////    [SerializeField] private GameObject dragonObjectOverride;

////    // ── State ──────────────────────────────────────────────────────────────────
////    public enum SlotState { Empty, Hatching, Cracking, Hatched }
////    public SlotState CurrentState { get; private set; } = SlotState.Empty;

////    private float _hatchEndTime;
////    private Coroutine _hatchCoroutine;
////    private GameObject _spawnedDragon;   // reference to the live dragon instance

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        AutoWireChildren();
////        addEggButton?.onClick.AddListener(OnAddEggClicked);
////    }

////    private void Start()
////    {
////        EnterEmpty();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void AutoWireChildren()
////    {
////        Transform t = transform;

////        // Timer
////        if (timerDisplay == null)
////        {
////            var td = t.Find("TimerDisplay");
////            if (td != null)
////            {
////                timerDisplay = td.gameObject;
////                if (timerText == null)
////                {
////                    var tt = td.Find("TimerText");
////                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
////                }
////            }
////        }

////        // Empty Slot
////        if (emptySlotRoot == null)
////        {
////            var es = t.Find("EmptySlot");
////            if (es != null)
////            {
////                emptySlotRoot = es.gameObject;
////                if (addEggButton == null)
////                {
////                    var btn = es.Find("AddEggButton");
////                    if (btn != null) addEggButton = btn.GetComponent<Button>();
////                }
////            }
////        }

////        // Egg
////        if (eggRoot == null)
////        {
////            var egg = t.Find("EggObject");
////            if (egg != null)
////            {
////                eggRoot = egg.gameObject;
////                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
////            }
////        }

////        // Fence
////        if (fenceRoot == null)
////        {
////            var fence = t.Find("Fench");
////            if (fence != null) fenceRoot = fence.gameObject;
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BUTTON CALLBACK
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnAddEggClicked()
////    {
////        if (CurrentState != SlotState.Empty) return;

////        if (dragonData == null)
////        {
////            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
////            return;
////        }

////        EnterHatching();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — EMPTY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterEmpty()
////    {
////        CurrentState = SlotState.Empty;

////        Show(emptySlotRoot, true);
////        Show(eggRoot, false);
////        Show(fenceRoot, false);
////        Show(timerDisplay, false);

////        // Hide any existing dragon
////        if (_spawnedDragon != null) Destroy(_spawnedDragon);
////        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

////        Debug.Log("[DragonEggSlot] → Empty");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — HATCHING  (egg visible, timer counting down at top)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterHatching()
////    {
////        CurrentState = SlotState.Hatching;
////        _hatchEndTime = Time.time + dragonData.hatchDuration;

////        Show(emptySlotRoot, false);  // hide the empty slot
////        Show(eggRoot, true);   // show the egg in the slot
////        Show(fenceRoot, true);   // show fence around the egg
////        Show(timerDisplay, true);   // show timer at top of area

////        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
////        _hatchCoroutine = StartCoroutine(HatchCountdown());

////        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — CRACKING  (timer done, crack animation plays)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterCracking()
////    {
////        CurrentState = SlotState.Cracking;

////        // Hide the timer — it's served its purpose
////        Show(timerDisplay, false);
////        if (timerText != null) timerText.text = "00:00";

////        // Fire the crack animation on the egg
////        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
////            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

////        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

////        // Wait for the crack clip to finish, then hatch
////        StartCoroutine(WaitForCrackThenHatch());
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterHatched()
////    {
////        CurrentState = SlotState.Hatched;

////        // Hide egg and fence
////        Show(eggRoot, false);
////        Show(fenceRoot, false);

////        // Spawn or activate the dragon
////        if (dragonObjectOverride != null)
////        {
////            // Pre-placed GO path
////            dragonObjectOverride.SetActive(true);
////            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
////            _spawnedDragon = dragonObjectOverride;
////        }
////        else if (dragonPrefab != null)
////        {
////            // Prefab instantiation path
////            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
////            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
////            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());
////        }
////        else
////        {
////            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
////        }

////        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // COROUTINES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Counts down and updates the timer label every frame.
////    /// When time is up it moves to Cracking state.
////    private IEnumerator HatchCountdown()
////    {
////        while (true)
////        {
////            float remaining = _hatchEndTime - Time.time;

////            if (remaining <= 0f)
////            {
////                UpdateTimerLabel(0f);
////                break;
////            }

////            UpdateTimerLabel(remaining);
////            yield return null;
////        }

////        _hatchCoroutine = null;
////        EnterCracking();
////    }

////    /// Waits for the crack animation clip to finish, then hatches.
////    private IEnumerator WaitForCrackThenHatch()
////    {
////        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
////        EnterHatched();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void UpdateTimerLabel(float seconds)
////    {
////        if (timerText == null) return;
////        float s = Mathf.Max(0f, seconds);
////        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
////    }

////    private void TriggerDragonIdle(Animator anim)
////    {
////        if (anim == null) return;
////        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
////            anim.SetTrigger(dragonData.dragonIdleTrigger);
////        // If dragonIdleTrigger is blank the idle state plays automatically on entry
////    }

////    private static void Show(GameObject go, bool visible)
////    {
////        if (go != null) go.SetActive(visible);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PUBLIC API
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
////    public void ResetSlot()
////    {
////        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
////        StopAllCoroutines();
////        EnterEmpty();
////    }

////    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
////    public float GetRemainingHatchTime()
////        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

////    // ══════════════════════════════════════════════════════════════════════════
////    // EDITOR
////    // ══════════════════════════════════════════════════════════════════════════

////#if UNITY_EDITOR
////    private void OnValidate()
////    {
////        if (dragonData == null)
////            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
////        if (dragonPrefab == null && dragonObjectOverride == null)
////            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
////    }
////#endif
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// DRAGON AREA — DragonEggSlot
/////
///// Attach to the DragonArea root GameObject inside VillagePanel.
/////
///// ════════════════════════════════════════════════════════════════════
/////  STATES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Empty      No egg. EmptySlot (with AddEggButton) is shown.
/////             Timer display is hidden.
/////
/////  Hatching   Egg sits in the slot. Timer counts down at the TOP of
/////             the area. Nothing else happens until it hits zero.
/////
/////  Cracking   Timer hits zero → timer display hides → egg crack
/////             animation plays for crackAnimationDuration seconds.
/////
/////  Hatched    Crack animation done → egg hidden → dragon prefab
/////             instantiated (or pre-placed DragonObject activated) →
/////             dragon idle animation plays.
/////
///// ════════════════════════════════════════════════════════════════════
/////  REQUIRED HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   DragonArea              ← DragonEggSlot.cs lives here
/////   ├── TimerDisplay        ← sits at the TOP of the area
/////   │   └── TimerText       TextMeshProUGUI  "00:45"
/////   ├── EmptySlot           shown while no egg is placed
/////   │   └── AddEggButton    Button the player taps
/////   └── EggObject           egg sprite + Animator (crack clip)
/////
/////  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
/////  child of DragonArea when hatching finishes.
/////  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
/////   of dragonPrefab — the script handles both.)
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR FIELDS
///// ════════════════════════════════════════════════════════════════════
/////
/////  dragonData            DragonData ScriptableObject asset
/////
/////  timerDisplay          parent GO of the timer (sits at top of area)
/////  timerText             TMP label inside timerDisplay
/////
/////  emptySlotRoot         EmptySlot GO
/////  addEggButton          Button inside EmptySlot
/////
/////  eggRoot               EggObject GO  (has an Animator)
/////  eggAnimator           Animator on EggObject
/////
/////  dragonPrefab          prefab to Instantiate when hatched
/////  dragonSpawnPoint      Transform where the prefab is placed
/////                        (defaults to DragonArea centre if left blank)
/////
/////  dragonObjectOverride  optional: pre-placed DragonObject to activate
/////                        instead of instantiating a prefab
///// </summary>
//public class DragonEggSlot : MonoBehaviour
//{
//    // ── Data ───────────────────────────────────────────────────────────────────
//    [Header("Dragon Data")]
//    [SerializeField] private DragonData dragonData;

//    // ── Timer (top of area) ────────────────────────────────────────────────────
//    [Header("Timer — placed at the top of DragonArea")]
//    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
//    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

//    // ── Empty Slot ─────────────────────────────────────────────────────────────
//    [Header("Empty Slot")]
//    [SerializeField] private GameObject emptySlotRoot;
//    [SerializeField] private Button addEggButton;

//    // ── Egg ────────────────────────────────────────────────────────────────────
//    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
//    [SerializeField] private GameObject eggRoot;
//    [SerializeField] private Animator eggAnimator;

//    // ── Fence ──────────────────────────────────────────────────────────────────
//    [Header("Fence — activates with the egg, deactivates when dragon hatches")]
//    [SerializeField] private GameObject fenceRoot;

//    // ── Nests ──────────────────────────────────────────────────────────────────
//    [Header("Nests — both visible in Empty and Hatching, deactivated when dragon hatches")]
//    [SerializeField] private GameObject nestRoot1;
//    [SerializeField] private GameObject nestRoot2;

//    // ── Dragon ─────────────────────────────────────────────────────────────────
//    [Header("Dragon — prefab spawned when hatched")]
//    [Tooltip("Prefab to Instantiate when the egg hatches.")]
//    [SerializeField] private GameObject dragonPrefab;
//    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
//    [SerializeField] private Transform dragonSpawnPoint;
//    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
//    [SerializeField] private GameObject dragonObjectOverride;

//    // ── State ──────────────────────────────────────────────────────────────────
//    public enum SlotState { Empty, Hatching, Cracking, Hatched }
//    public SlotState CurrentState { get; private set; } = SlotState.Empty;

//    private float _hatchEndTime;
//    private Coroutine _hatchCoroutine;
//    private GameObject _spawnedDragon;   // reference to the live dragon instance

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        AutoWireChildren();
//        addEggButton?.onClick.AddListener(OnAddEggClicked);
//    }

//    private void Start()
//    {
//        EnterEmpty();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void AutoWireChildren()
//    {
//        Transform t = transform;

//        // Timer
//        if (timerDisplay == null)
//        {
//            var td = t.Find("TimerDisplay");
//            if (td != null)
//            {
//                timerDisplay = td.gameObject;
//                if (timerText == null)
//                {
//                    var tt = td.Find("TimerText");
//                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
//                }
//            }
//        }

//        // Empty Slot
//        if (emptySlotRoot == null)
//        {
//            var es = t.Find("EmptySlot");
//            if (es != null)
//            {
//                emptySlotRoot = es.gameObject;
//                if (addEggButton == null)
//                {
//                    var btn = es.Find("AddEggButton");
//                    if (btn != null) addEggButton = btn.GetComponent<Button>();
//                }
//            }
//        }

//        // Egg
//        if (eggRoot == null)
//        {
//            var egg = t.Find("EggObject");
//            if (egg != null)
//            {
//                eggRoot = egg.gameObject;
//                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
//            }
//        }

//        // Fence
//        if (fenceRoot == null)
//        {
//            var fence = t.Find("Fench");
//            if (fence != null) fenceRoot = fence.gameObject;
//        }

//        // Nests
//        if (nestRoot1 == null)
//        {
//            var n1 = t.Find("Nest1");
//            if (n1 != null) nestRoot1 = n1.gameObject;
//        }
//        if (nestRoot2 == null)
//        {
//            var n2 = t.Find("Nest2");
//            if (n2 != null) nestRoot2 = n2.gameObject;
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BUTTON CALLBACK
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnAddEggClicked()
//    {
//        if (CurrentState != SlotState.Empty) return;

//        if (dragonData == null)
//        {
//            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
//            return;
//        }

//        EnterHatching();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — EMPTY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterEmpty()
//    {
//        CurrentState = SlotState.Empty;

//        Show(emptySlotRoot, true);
//        Show(nestRoot1, true);
//        Show(nestRoot2, true);
//        Show(eggRoot, false);
//        Show(fenceRoot, false);
//        Show(timerDisplay, false);

//        // Hide any existing dragon
//        if (_spawnedDragon != null) Destroy(_spawnedDragon);
//        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

//        Debug.Log("[DragonEggSlot] → Empty");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — HATCHING  (egg visible, timer counting down at top)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterHatching()
//    {
//        CurrentState = SlotState.Hatching;
//        _hatchEndTime = Time.time + dragonData.hatchDuration;

//        Show(emptySlotRoot, false);  // hide the empty slot
//        Show(nestRoot1, true);   // keep both nests visible under the egg
//        Show(nestRoot2, true);
//        Show(eggRoot, true);   // show the egg in the slot
//        Show(fenceRoot, true);   // show fence around the egg
//        Show(timerDisplay, true);   // show timer at top of area

//        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
//        _hatchCoroutine = StartCoroutine(HatchCountdown());

//        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — CRACKING  (timer done, crack animation plays)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterCracking()
//    {
//        CurrentState = SlotState.Cracking;

//        // Hide the timer — it's served its purpose
//        Show(timerDisplay, false);
//        if (timerText != null) timerText.text = "00:00";

//        // Fire the crack animation on the egg
//        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
//            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

//        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

//        // Wait for the crack clip to finish, then hatch
//        StartCoroutine(WaitForCrackThenHatch());
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterHatched()
//    {
//        CurrentState = SlotState.Hatched;

//        // Hide egg, fence, nests and timer when dragon appears
//        Show(eggRoot, false);
//        Show(fenceRoot, false);
//        Show(nestRoot1, false);
//        Show(nestRoot2, false);
//        Show(timerDisplay, false);

//        // Spawn or activate the dragon
//        if (dragonObjectOverride != null)
//        {
//            // Pre-placed GO path
//            dragonObjectOverride.SetActive(true);
//            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
//            _spawnedDragon = dragonObjectOverride;
//        }
//        else if (dragonPrefab != null)
//        {
//            // Prefab instantiation path
//            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
//            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
//            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());
//        }
//        else
//        {
//            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
//        }

//        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // COROUTINES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Counts down and updates the timer label every frame.
//    /// When time is up it moves to Cracking state.
//    private IEnumerator HatchCountdown()
//    {
//        while (true)
//        {
//            float remaining = _hatchEndTime - Time.time;

//            if (remaining <= 0f)
//            {
//                UpdateTimerLabel(0f);
//                break;
//            }

//            UpdateTimerLabel(remaining);
//            yield return null;
//        }

//        _hatchCoroutine = null;
//        EnterCracking();
//    }

//    /// Waits for the crack animation clip to finish, then hatches.
//    private IEnumerator WaitForCrackThenHatch()
//    {
//        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
//        EnterHatched();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void UpdateTimerLabel(float seconds)
//    {
//        if (timerText == null) return;
//        float s = Mathf.Max(0f, seconds);
//        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//    }

//    private void TriggerDragonIdle(Animator anim)
//    {
//        if (anim == null) return;
//        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
//            anim.SetTrigger(dragonData.dragonIdleTrigger);
//        // If dragonIdleTrigger is blank the idle state plays automatically on entry
//    }

//    private static void Show(GameObject go, bool visible)
//    {
//        if (go != null) go.SetActive(visible);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC API
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
//    public void ResetSlot()
//    {
//        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
//        StopAllCoroutines();
//        EnterEmpty();
//    }

//    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
//    public float GetRemainingHatchTime()
//        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

//    // ══════════════════════════════════════════════════════════════════════════
//    // EDITOR
//    // ══════════════════════════════════════════════════════════════════════════

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (dragonData == null)
//            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
//        if (dragonPrefab == null && dragonObjectOverride == null)
//            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
//    }
//#endif
//}

////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// DRAGON AREA — DragonEggSlot
///////
/////// Attach to the DragonArea root GameObject inside VillagePanel.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  STATES
/////// ════════════════════════════════════════════════════════════════════
///////
///////  Empty      No egg. EmptySlot (with AddEggButton) is shown.
///////             Timer display is hidden.
///////
///////  Hatching   Egg sits in the slot. Timer counts down at the TOP of
///////             the area. Nothing else happens until it hits zero.
///////
///////  Cracking   Timer hits zero → timer display hides → egg crack
///////             animation plays for crackAnimationDuration seconds.
///////
///////  Hatched    Crack animation done → egg hidden → dragon prefab
///////             instantiated (or pre-placed DragonObject activated) →
///////             dragon idle animation plays.
///////
/////// ════════════════════════════════════════════════════════════════════
///////  REQUIRED HIERARCHY
/////// ════════════════════════════════════════════════════════════════════
///////
///////   DragonArea              ← DragonEggSlot.cs lives here
///////   ├── TimerDisplay        ← sits at the TOP of the area
///////   │   └── TimerText       TextMeshProUGUI  "00:45"
///////   ├── EmptySlot           shown while no egg is placed
///////   │   └── AddEggButton    Button the player taps
///////   └── EggObject           egg sprite + Animator (crack clip)
///////
///////  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
///////  child of DragonArea when hatching finishes.
///////  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
///////   of dragonPrefab — the script handles both.)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  INSPECTOR FIELDS
/////// ════════════════════════════════════════════════════════════════════
///////
///////  dragonData            DragonData ScriptableObject asset
///////
///////  timerDisplay          parent GO of the timer (sits at top of area)
///////  timerText             TMP label inside timerDisplay
///////
///////  emptySlotRoot         EmptySlot GO
///////  addEggButton          Button inside EmptySlot
///////
///////  eggRoot               EggObject GO  (has an Animator)
///////  eggAnimator           Animator on EggObject
///////
///////  dragonPrefab          prefab to Instantiate when hatched
///////  dragonSpawnPoint      Transform where the prefab is placed
///////                        (defaults to DragonArea centre if left blank)
///////
///////  dragonObjectOverride  optional: pre-placed DragonObject to activate
///////                        instead of instantiating a prefab
/////// </summary>
////public class DragonEggSlot : MonoBehaviour
////{
////    // ── Data ───────────────────────────────────────────────────────────────────
////    [Header("Dragon Data")]
////    [SerializeField] private DragonData dragonData;

////    // ── Timer (top of area) ────────────────────────────────────────────────────
////    [Header("Timer — placed at the top of DragonArea")]
////    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
////    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

////    // ── Empty Slot ─────────────────────────────────────────────────────────────
////    [Header("Empty Slot")]
////    [SerializeField] private GameObject emptySlotRoot;
////    [SerializeField] private Button addEggButton;

////    // ── Egg ────────────────────────────────────────────────────────────────────
////    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
////    [SerializeField] private GameObject eggRoot;
////    [SerializeField] private Animator eggAnimator;

////    // ── Dragon ─────────────────────────────────────────────────────────────────
////    [Header("Dragon — prefab spawned when hatched")]
////    [Tooltip("Prefab to Instantiate when the egg hatches.")]
////    [SerializeField] private GameObject dragonPrefab;
////    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
////    [SerializeField] private Transform dragonSpawnPoint;
////    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
////    [SerializeField] private GameObject dragonObjectOverride;

////    // ── State ──────────────────────────────────────────────────────────────────
////    public enum SlotState { Empty, Hatching, Cracking, Hatched }
////    public SlotState CurrentState { get; private set; } = SlotState.Empty;

////    private float _hatchEndTime;
////    private Coroutine _hatchCoroutine;
////    private GameObject _spawnedDragon;   // reference to the live dragon instance

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY LIFECYCLE
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        AutoWireChildren();
////        addEggButton?.onClick.AddListener(OnAddEggClicked);
////    }

////    private void Start()
////    {
////        EnterEmpty();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void AutoWireChildren()
////    {
////        Transform t = transform;

////        // Timer
////        if (timerDisplay == null)
////        {
////            var td = t.Find("TimerDisplay");
////            if (td != null)
////            {
////                timerDisplay = td.gameObject;
////                if (timerText == null)
////                {
////                    var tt = td.Find("TimerText");
////                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
////                }
////            }
////        }

////        // Empty Slot
////        if (emptySlotRoot == null)
////        {
////            var es = t.Find("EmptySlot");
////            if (es != null)
////            {
////                emptySlotRoot = es.gameObject;
////                if (addEggButton == null)
////                {
////                    var btn = es.Find("AddEggButton");
////                    if (btn != null) addEggButton = btn.GetComponent<Button>();
////                }
////            }
////        }

////        // Egg
////        if (eggRoot == null)
////        {
////            var egg = t.Find("EggObject");
////            if (egg != null)
////            {
////                eggRoot = egg.gameObject;
////                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
////            }
////        }
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // BUTTON CALLBACK
////    // ══════════════════════════════════════════════════════════════════════════

////    private void OnAddEggClicked()
////    {
////        if (CurrentState != SlotState.Empty) return;

////        if (dragonData == null)
////        {
////            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
////            return;
////        }

////        EnterHatching();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — EMPTY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterEmpty()
////    {
////        CurrentState = SlotState.Empty;

////        Show(emptySlotRoot, true);
////        Show(eggRoot, false);
////        Show(timerDisplay, false);

////        // Hide any existing dragon
////        if (_spawnedDragon != null) Destroy(_spawnedDragon);
////        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

////        Debug.Log("[DragonEggSlot] → Empty");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — HATCHING  (egg visible, timer counting down at top)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterHatching()
////    {
////        CurrentState = SlotState.Hatching;
////        _hatchEndTime = Time.time + dragonData.hatchDuration;

////        Show(emptySlotRoot, false);  // hide the empty slot
////        Show(eggRoot, true);   // show the egg in the slot
////        Show(timerDisplay, true);   // show timer at top of area

////        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
////        _hatchCoroutine = StartCoroutine(HatchCountdown());

////        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — CRACKING  (timer done, crack animation plays)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterCracking()
////    {
////        CurrentState = SlotState.Cracking;

////        // Hide the timer — it's served its purpose
////        Show(timerDisplay, false);
////        if (timerText != null) timerText.text = "00:00";

////        // Fire the crack animation on the egg
////        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
////            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

////        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

////        // Wait for the crack clip to finish, then hatch
////        StartCoroutine(WaitForCrackThenHatch());
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
////    // ══════════════════════════════════════════════════════════════════════════

////    private void EnterHatched()
////    {
////        CurrentState = SlotState.Hatched;

////        // Hide egg
////        Show(eggRoot, false);

////        // Spawn or activate the dragon
////        if (dragonObjectOverride != null)
////        {
////            // Pre-placed GO path
////            dragonObjectOverride.SetActive(true);
////            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
////            _spawnedDragon = dragonObjectOverride;
////        }
////        else if (dragonPrefab != null)
////        {
////            // Prefab instantiation path
////            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
////            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
////            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());
////        }
////        else
////        {
////            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
////        }

////        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // COROUTINES
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Counts down and updates the timer label every frame.
////    /// When time is up it moves to Cracking state.
////    private IEnumerator HatchCountdown()
////    {
////        while (true)
////        {
////            float remaining = _hatchEndTime - Time.time;

////            if (remaining <= 0f)
////            {
////                UpdateTimerLabel(0f);
////                break;
////            }

////            UpdateTimerLabel(remaining);
////            yield return null;
////        }

////        _hatchCoroutine = null;
////        EnterCracking();
////    }

////    /// Waits for the crack animation clip to finish, then hatches.
////    private IEnumerator WaitForCrackThenHatch()
////    {
////        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
////        EnterHatched();
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void UpdateTimerLabel(float seconds)
////    {
////        if (timerText == null) return;
////        float s = Mathf.Max(0f, seconds);
////        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
////    }

////    private void TriggerDragonIdle(Animator anim)
////    {
////        if (anim == null) return;
////        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
////            anim.SetTrigger(dragonData.dragonIdleTrigger);
////        // If dragonIdleTrigger is blank the idle state plays automatically on entry
////    }

////    private static void Show(GameObject go, bool visible)
////    {
////        if (go != null) go.SetActive(visible);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PUBLIC API
////    // ══════════════════════════════════════════════════════════════════════════

////    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
////    public void ResetSlot()
////    {
////        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
////        StopAllCoroutines();
////        EnterEmpty();
////    }

////    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
////    public float GetRemainingHatchTime()
////        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

////    // ══════════════════════════════════════════════════════════════════════════
////    // EDITOR
////    // ══════════════════════════════════════════════════════════════════════════

////#if UNITY_EDITOR
////    private void OnValidate()
////    {
////        if (dragonData == null)
////            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
////        if (dragonPrefab == null && dragonObjectOverride == null)
////            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
////    }
////#endif
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// DRAGON AREA — DragonEggSlot
/////
///// Attach to the DragonArea root GameObject inside VillagePanel.
/////
///// ════════════════════════════════════════════════════════════════════
/////  STATES
///// ════════════════════════════════════════════════════════════════════
/////
/////  Empty      No egg. EmptySlot (with AddEggButton) is shown.
/////             Timer display is hidden.
/////
/////  Hatching   Egg sits in the slot. Timer counts down at the TOP of
/////             the area. Nothing else happens until it hits zero.
/////
/////  Cracking   Timer hits zero → timer display hides → egg crack
/////             animation plays for crackAnimationDuration seconds.
/////
/////  Hatched    Crack animation done → egg hidden → dragon prefab
/////             instantiated (or pre-placed DragonObject activated) →
/////             dragon idle animation plays.
/////
///// ════════════════════════════════════════════════════════════════════
/////  REQUIRED HIERARCHY
///// ════════════════════════════════════════════════════════════════════
/////
/////   DragonArea              ← DragonEggSlot.cs lives here
/////   ├── TimerDisplay        ← sits at the TOP of the area
/////   │   └── TimerText       TextMeshProUGUI  "00:45"
/////   ├── EmptySlot           shown while no egg is placed
/////   │   └── AddEggButton    Button the player taps
/////   └── EggObject           egg sprite + Animator (crack clip)
/////
/////  The dragon is a PREFAB. DragonEggSlot.Instantiate()s it as a
/////  child of DragonArea when hatching finishes.
/////  (If you prefer a pre-placed GO, assign dragonObjectOverride instead
/////   of dragonPrefab — the script handles both.)
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR FIELDS
///// ════════════════════════════════════════════════════════════════════
/////
/////  dragonData            DragonData ScriptableObject asset
/////
/////  timerDisplay          parent GO of the timer (sits at top of area)
/////  timerText             TMP label inside timerDisplay
/////
/////  emptySlotRoot         EmptySlot GO
/////  addEggButton          Button inside EmptySlot
/////
/////  eggRoot               EggObject GO  (has an Animator)
/////  eggAnimator           Animator on EggObject
/////
/////  dragonPrefab          prefab to Instantiate when hatched
/////  dragonSpawnPoint      Transform where the prefab is placed
/////                        (defaults to DragonArea centre if left blank)
/////
/////  dragonObjectOverride  optional: pre-placed DragonObject to activate
/////                        instead of instantiating a prefab
///// </summary>
//public class DragonEggSlot : MonoBehaviour
//{
//    // ── Data ───────────────────────────────────────────────────────────────────
//    [Header("Dragon Data")]
//    [SerializeField] private DragonData dragonData;

//    // ── Timer (top of area) ────────────────────────────────────────────────────
//    [Header("Timer — placed at the top of DragonArea")]
//    [SerializeField] private GameObject timerDisplay;   // parent GO to show/hide
//    [SerializeField] private TextMeshProUGUI timerText;      // "00:45"

//    // ── Empty Slot ─────────────────────────────────────────────────────────────
//    [Header("Empty Slot")]
//    [SerializeField] private GameObject emptySlotRoot;
//    [SerializeField] private Button addEggButton;

//    // ── Egg ────────────────────────────────────────────────────────────────────
//    [Header("Egg Object  (Animator plays crack clip at the end of the timer)")]
//    [SerializeField] private GameObject eggRoot;
//    [SerializeField] private Animator eggAnimator;

//    // ── Fence ──────────────────────────────────────────────────────────────────
//    [Header("Fence — activates with the egg, deactivates when dragon hatches")]
//    [SerializeField] private GameObject fenceRoot;

//    // ── Dragon ─────────────────────────────────────────────────────────────────
//    [Header("Dragon — prefab spawned when hatched")]
//    [Tooltip("Prefab to Instantiate when the egg hatches.")]
//    [SerializeField] private GameObject dragonPrefab;
//    [Tooltip("Where the dragon prefab is placed. Defaults to this Transform if blank.")]
//    [SerializeField] private Transform dragonSpawnPoint;
//    [Tooltip("Optional: assign a pre-placed DragonObject instead of using a prefab.")]
//    [SerializeField] private GameObject dragonObjectOverride;

//    // ── State ──────────────────────────────────────────────────────────────────
//    public enum SlotState { Empty, Hatching, Cracking, Hatched }
//    public SlotState CurrentState { get; private set; } = SlotState.Empty;

//    private float _hatchEndTime;
//    private Coroutine _hatchCoroutine;
//    private GameObject _spawnedDragon;   // reference to the live dragon instance

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        AutoWireChildren();
//        addEggButton?.onClick.AddListener(OnAddEggClicked);
//    }

//    private void Start()
//    {
//        EnterEmpty();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // AUTO-WIRE  (finds named children when Inspector fields are left blank)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void AutoWireChildren()
//    {
//        Transform t = transform;

//        // Timer
//        if (timerDisplay == null)
//        {
//            var td = t.Find("TimerDisplay");
//            if (td != null)
//            {
//                timerDisplay = td.gameObject;
//                if (timerText == null)
//                {
//                    var tt = td.Find("TimerText");
//                    if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
//                }
//            }
//        }

//        // Empty Slot
//        if (emptySlotRoot == null)
//        {
//            var es = t.Find("EmptySlot");
//            if (es != null)
//            {
//                emptySlotRoot = es.gameObject;
//                if (addEggButton == null)
//                {
//                    var btn = es.Find("AddEggButton");
//                    if (btn != null) addEggButton = btn.GetComponent<Button>();
//                }
//            }
//        }

//        // Egg
//        if (eggRoot == null)
//        {
//            var egg = t.Find("EggObject");
//            if (egg != null)
//            {
//                eggRoot = egg.gameObject;
//                if (eggAnimator == null) eggAnimator = egg.GetComponent<Animator>();
//            }
//        }

//        // Fence
//        if (fenceRoot == null)
//        {
//            var fence = t.Find("Fench");
//            if (fence != null) fenceRoot = fence.gameObject;
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // BUTTON CALLBACK
//    // ══════════════════════════════════════════════════════════════════════════

//    private void OnAddEggClicked()
//    {
//        if (CurrentState != SlotState.Empty) return;

//        if (dragonData == null)
//        {
//            Debug.LogWarning("[DragonEggSlot] DragonData not assigned!", this);
//            return;
//        }

//        EnterHatching();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — EMPTY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterEmpty()
//    {
//        CurrentState = SlotState.Empty;

//        Show(emptySlotRoot, true);
//        Show(eggRoot, false);
//        Show(fenceRoot, false);
//        Show(timerDisplay, false);

//        // Hide any existing dragon
//        if (_spawnedDragon != null) Destroy(_spawnedDragon);
//        if (dragonObjectOverride != null) dragonObjectOverride.SetActive(false);

//        Debug.Log("[DragonEggSlot] → Empty");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — HATCHING  (egg visible, timer counting down at top)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterHatching()
//    {
//        CurrentState = SlotState.Hatching;
//        _hatchEndTime = Time.time + dragonData.hatchDuration;

//        Show(emptySlotRoot, false);  // hide the empty slot
//        Show(eggRoot, true);   // show the egg in the slot
//        Show(fenceRoot, true);   // show fence around the egg
//        Show(timerDisplay, true);   // show timer at top of area

//        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
//        _hatchCoroutine = StartCoroutine(HatchCountdown());

//        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — CRACKING  (timer done, crack animation plays)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterCracking()
//    {
//        CurrentState = SlotState.Cracking;

//        // Hide the timer — it's served its purpose
//        Show(timerDisplay, false);
//        if (timerText != null) timerText.text = "00:00";

//        // Fire the crack animation on the egg
//        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
//            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

//        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

//        // Wait for the crack clip to finish, then hatch
//        StartCoroutine(WaitForCrackThenHatch());
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
//    // ══════════════════════════════════════════════════════════════════════════

//    private void EnterHatched()
//    {
//        CurrentState = SlotState.Hatched;

//        // Hide egg and fence
//        Show(eggRoot, false);
//        Show(fenceRoot, false);

//        // Spawn or activate the dragon
//        if (dragonObjectOverride != null)
//        {
//            // Pre-placed GO path
//            dragonObjectOverride.SetActive(true);
//            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
//            _spawnedDragon = dragonObjectOverride;
//        }
//        else if (dragonPrefab != null)
//        {
//            // Prefab instantiation path
//            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
//            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
//            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());
//        }
//        else
//        {
//            Debug.LogWarning("[DragonEggSlot] Neither dragonPrefab nor dragonObjectOverride assigned!", this);
//        }

//        Debug.Log($"[DragonEggSlot] → Hatched  — {dragonData?.dragonName} appeared!");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // COROUTINES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Counts down and updates the timer label every frame.
//    /// When time is up it moves to Cracking state.
//    private IEnumerator HatchCountdown()
//    {
//        while (true)
//        {
//            float remaining = _hatchEndTime - Time.time;

//            if (remaining <= 0f)
//            {
//                UpdateTimerLabel(0f);
//                break;
//            }

//            UpdateTimerLabel(remaining);
//            yield return null;
//        }

//        _hatchCoroutine = null;
//        EnterCracking();
//    }

//    /// Waits for the crack animation clip to finish, then hatches.
//    private IEnumerator WaitForCrackThenHatch()
//    {
//        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
//        EnterHatched();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void UpdateTimerLabel(float seconds)
//    {
//        if (timerText == null) return;
//        float s = Mathf.Max(0f, seconds);
//        timerText.text = $"{(int)(s / 60f):00}:{(int)(s % 60f):00}";
//    }

//    private void TriggerDragonIdle(Animator anim)
//    {
//        if (anim == null) return;
//        if (!string.IsNullOrEmpty(dragonData?.dragonIdleTrigger))
//            anim.SetTrigger(dragonData.dragonIdleTrigger);
//        // If dragonIdleTrigger is blank the idle state plays automatically on entry
//    }

//    private static void Show(GameObject go, bool visible)
//    {
//        if (go != null) go.SetActive(visible);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PUBLIC API
//    // ══════════════════════════════════════════════════════════════════════════

//    /// Resets the slot to Empty (e.g. after the dragon is sent to battle).
//    public void ResetSlot()
//    {
//        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
//        StopAllCoroutines();
//        EnterEmpty();
//    }

//    /// Remaining hatch seconds. Returns 0 if not in Hatching state.
//    public float GetRemainingHatchTime()
//        => CurrentState == SlotState.Hatching ? Mathf.Max(0f, _hatchEndTime - Time.time) : 0f;

//    // ══════════════════════════════════════════════════════════════════════════
//    // EDITOR
//    // ══════════════════════════════════════════════════════════════════════════

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (dragonData == null)
//            Debug.LogWarning("[DragonEggSlot] DragonData ScriptableObject not assigned!", this);
//        if (dragonPrefab == null && dragonObjectOverride == null)
//            Debug.LogWarning("[DragonEggSlot] Assign either dragonPrefab or dragonObjectOverride.", this);
//    }
//#endif
//}

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

    private float _hatchEndTime;
    private Coroutine _hatchCoroutine;
    private GameObject _spawnedDragon;   // reference to the live dragon instance

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
        Show(timerDisplay, false);

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

        Show(emptySlotRoot, false);  // hide the empty slot
        Show(nestRoot1, true);   // keep both nests visible under the egg
        Show(nestRoot2, true);
        Show(eggRoot, true);   // show the egg in the slot
        Show(fenceRoot, true);   // show fence around the egg
        Show(timerDisplay, true);   // show timer at top of area

        if (_hatchCoroutine != null) StopCoroutine(_hatchCoroutine);
        _hatchCoroutine = StartCoroutine(HatchCountdown());

        Debug.Log($"[DragonEggSlot] → Hatching  ({dragonData.hatchDuration}s)");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — CRACKING  (timer done, crack animation plays)
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterCracking()
    {
        CurrentState = SlotState.Cracking;

        // Hide the timer — it's served its purpose
        Show(timerDisplay, false);
        if (timerText != null) timerText.text = "00:00";

        // Fire the crack animation on the egg
        if (eggAnimator != null && !string.IsNullOrEmpty(dragonData.eggCrackTrigger))
            eggAnimator.SetTrigger(dragonData.eggCrackTrigger);

        Debug.Log($"[DragonEggSlot] → Cracking  ({dragonData.crackAnimationDuration}s)");

        // Wait for the crack clip to finish, then hatch
        StartCoroutine(WaitForCrackThenHatch());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATE — HATCHED  (dragon prefab spawned, idle animation plays)
    // ══════════════════════════════════════════════════════════════════════════

    private void EnterHatched()
    {
        CurrentState = SlotState.Hatched;

        // Hide egg, fence, nests and timer when dragon appears
        Show(eggRoot, false);
        Show(fenceRoot, false);
        Show(nestRoot1, false);
        Show(nestRoot2, false);
        Show(timerDisplay, false);

        // Spawn or activate the dragon
        if (dragonObjectOverride != null)
        {
            // Pre-placed GO path
            dragonObjectOverride.SetActive(true);
            TriggerDragonIdle(dragonObjectOverride.GetComponent<Animator>());
            _spawnedDragon = dragonObjectOverride;

            // Tell the dragon which slot it belongs to (enables drag-back home)
            var dc = _spawnedDragon.GetComponent<DragonController>();
            if (dc != null) dc.homeSlot = this;
        }
        else if (dragonPrefab != null)
        {
            // Prefab instantiation path
            Transform spawnAt = dragonSpawnPoint != null ? dragonSpawnPoint : transform;
            _spawnedDragon = Instantiate(dragonPrefab, spawnAt.position, spawnAt.rotation, transform);
            TriggerDragonIdle(_spawnedDragon.GetComponent<Animator>());

            // Tell the dragon which slot it belongs to (enables drag-back home)
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
    // COROUTINES
    // ══════════════════════════════════════════════════════════════════════════

    /// Counts down and updates the timer label every frame.
    /// When time is up it moves to Cracking state.
    private IEnumerator HatchCountdown()
    {
        while (true)
        {
            float remaining = _hatchEndTime - Time.time;

            if (remaining <= 0f)
            {
                UpdateTimerLabel(0f);
                break;
            }

            UpdateTimerLabel(remaining);
            yield return null;
        }

        _hatchCoroutine = null;
        EnterCracking();
    }

    /// Waits for the crack animation clip to finish, then hatches.
    private IEnumerator WaitForCrackThenHatch()
    {
        yield return new WaitForSeconds(dragonData.crackAnimationDuration);
        EnterHatched();
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
        // If dragonIdleTrigger is blank the idle state plays automatically on entry
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
        if (_hatchCoroutine != null) { StopCoroutine(_hatchCoroutine); _hatchCoroutine = null; }
        StopAllCoroutines();
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