//////////using UnityEngine;
//////////using UnityEngine.UI;
//////////using TMPro;

///////////// <summary>
///////////// AREA FORGE - SoldierHUD
///////////// Displays a soldier's stats on a world-space or screen-space HUD canvas.
///////////// Health bar uses Image.fillAmount (no Slider component needed).
/////////////
///////////// Inspector setup for the health bar:
/////////////   1. Create a Panel → name it "HealthBarBG"  (dark background colour)
/////////////   2. Inside it, add an Image → name it "HealthBarFill"
/////////////      • Image Type  : Filled
/////////////      • Fill Method : Horizontal
/////////////      • Fill Origin : Left
/////////////   3. Assign HealthBarFill to the healthBarFill field below.
/////////////   4. Assign a TextMeshProUGUI for healthText — it will show "85 / 100".
/////////////
///////////// MULTIPLAYER NOTE: HUD reads from SyncVar values — no changes needed here.
///////////// </summary>
//////////public class SoldierHUD : MonoBehaviour
//////////{
//////////    // ─── HUD References ───────────────────────────────────────────────────────

//////////    [Header("Health Bar (Image — Fill type, no Slider)")]
//////////    [Tooltip("The filled Image that acts as the health bar. Set Image Type → Filled → Horizontal.")]
//////////    [SerializeField] private Image healthBarFill;

//////////    [Header("Stat Labels (TextMeshPro)")]
//////////    [Tooltip("Shows  'currentHP / maxHP'  e.g.  '85 / 100'")]
//////////    [SerializeField] private TextMeshProUGUI healthText;   // FIX: now shows "85 / 100"
//////////    [SerializeField] private TextMeshProUGUI abilityText;  // e.g. "AP  10"
//////////    [SerializeField] private TextMeshProUGUI damageText;   // e.g. "AD  15"

//////////    [Header("Name Tag (optional)")]
//////////    [SerializeField] private TextMeshProUGUI nameText;

//////////    [Header("Health Bar Colours")]
//////////    [Tooltip("Bar colour when health is above 50 %")]
//////////    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f); // green
//////////    [Tooltip("Bar colour when health is at or below 50 %")]
//////////    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f); // red

//////////    // ─── Private ──────────────────────────────────────────────────────────────

//////////    private SoldierStats _stats;

//////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        _stats = GetComponentInParent<SoldierStats>();

//////////        if (_stats == null)
//////////        {
//////////            Debug.LogError("[SoldierHUD] SoldierStats not found on parent. " +
//////////                           "Place SoldierHUD as a child of the soldier prefab.");
//////////            return;
//////////        }

//////////        _stats.OnStatsChanged += RefreshHUD;
//////////        _stats.OnSoldierDied += HandleDeath;
//////////    }

//////////    private void Start()
//////////    {
//////////        if (_stats != null)
//////////            RefreshHUD(_stats);

//////////        if (nameText != null)
//////////            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
//////////    }

//////////    private void OnDestroy()
//////////    {
//////////        if (_stats != null)
//////////        {
//////////            _stats.OnStatsChanged -= RefreshHUD;
//////////            _stats.OnSoldierDied -= HandleDeath;
//////////        }
//////////    }

//////////    // ─── HUD Update ───────────────────────────────────────────────────────────

//////////    private void RefreshHUD(SoldierStats stats)
//////////    {
//////////        float pct = stats.HealthPercent; // 0.0 – 1.0

//////////        // ── Health bar fill ──────────────────────────────────────────────────
//////////        if (healthBarFill != null)
//////////        {
//////////            healthBarFill.fillAmount = pct;
//////////            healthBarFill.color = (pct > 0.5f) ? colorHealthy : colorDanger;
//////////        }

//////////        // ── Health text — FIX: shows "currentHP / maxHP" e.g. "85 / 100" ───
//////////        // Previously this only showed the current HP number with no max,
//////////        // so the player had no context for how much health was left.
//////////        if (healthText != null)
//////////            healthText.text = $"{Mathf.CeilToInt(stats.CurrentHealth)} / {Mathf.CeilToInt(stats.MaxHealth)}";

//////////        // ── Ability power ─────────────────────────────────────────────────────
//////////        if (abilityText != null)
//////////            abilityText.text = $"AP  {stats.AbilityPower:F0}";

//////////        // ── Attack damage ─────────────────────────────────────────────────────
//////////        if (damageText != null)
//////////            damageText.text = $"AD  {stats.AttackDamage:F0}";
//////////    }

//////////    private void HandleDeath(SoldierStats stats)
//////////    {
//////////        gameObject.SetActive(false);
//////////    }

//////////    // ─── World-Space Billboard ───────────────────────────────────────────────

//////////    /// <summary>
//////////    /// If your HUD Canvas is World Space, this keeps it facing the camera.
//////////    /// Delete this method if you're using Screen Space - Overlay.
//////////    /// </summary>
//////////    //private void LateUpdate()
//////////    //{
//////////    //    if (Camera.main != null)
//////////    //        transform.rotation = Camera.main.transform.rotation;
//////////    //}

//////////    private void LateUpdate()
//////////    {
//////////        // Counter the parent soldier's localScale.x flip so HUD text never mirrors.
//////////        // The billboard camera rotation is removed — it is not needed for UI Canvas.
//////////        Vector3 s = transform.localScale;
//////////        s.x = Mathf.Abs(s.x);   // always keep X positive, regardless of soldier direction
//////////        transform.localScale = s;
//////////        transform.localScale.x = Mathf.Abs(s.x); // always positive → never mirrors
//////////    }

//////////    //    private void LateUpdate()
//////////    //{
//////////    //    // Counter-scale so the HUD never mirrors with the parent soldier
//////////    //    Vector3 s = transform.localScale;
//////////    //    s.x = Mathf.Abs(s.x); // always keep HUD scale positive X
//////////    //    transform.localScale = s;

//////////    //    // Keep HUD facing camera (for World Space canvas)
//////////    //    if (Camera.main != null)
//////////    //        transform.rotation = Camera.main.transform.rotation;
//////////    //}
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;
////////using TMPro;

/////////// <summary>
/////////// AREA FORGE - SoldierHUD
/////////// Displays health bar + stat labels above the soldier.
/////////// Attached to HealthhudBG (child of SolderPrefab).
///////////
/////////// FIX: LateUpdate now counter-scales localScale.x so the HUD text never
/////////// mirrors when the parent soldier flips direction.
/////////// </summary>
////////public class SoldierHUD : MonoBehaviour
////////{
////////    // ─── HUD References ───────────────────────────────────────────────────────

////////    [Header("Health Bar")]
////////    [Tooltip("The filled Image acting as health bar — Image Type: Filled, Horizontal, Left")]
////////    [SerializeField] private Image healthBarFill;

////////    [Header("Stat Labels (TextMeshPro — all optional)")]
////////    [SerializeField] private TextMeshProUGUI healthText;
////////    [SerializeField] private TextMeshProUGUI abilityText;
////////    [SerializeField] private TextMeshProUGUI damageText;
////////    [SerializeField] private TextMeshProUGUI nameText;

////////    [Header("Health Bar Colours")]
////////    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
////////    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

////////    // ─── Private ──────────────────────────────────────────────────────────────

////////    private SoldierStats _stats;

////////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        _stats = GetComponentInParent<SoldierStats>();

////////        if (_stats == null)
////////        {
////////            Debug.LogError("[SoldierHUD] SoldierStats not found on parent. " +
////////                           "HealthhudBG must be a child of the soldier prefab.");
////////            return;
////////        }

////////        _stats.OnStatsChanged += RefreshHUD;
////////        _stats.OnSoldierDied += HandleDeath;
////////    }

////////    private void Start()
////////    {
////////        if (_stats != null) RefreshHUD(_stats);

////////        if (nameText != null)
////////            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
////////    }

////////    private void OnDestroy()
////////    {
////////        if (_stats == null) return;
////////        _stats.OnStatsChanged -= RefreshHUD;
////////        _stats.OnSoldierDied -= HandleDeath;
////////    }

////////    /// <summary>
////////    /// FIX: Counter-scale localScale.x every frame so the HUD never mirrors
////////    /// when the parent soldier's localScale.x flips to -1.
////////    ///
////////    /// Why the text was flipping:
////////    ///   SoldierController sets transform.localScale.x = -1 to mirror the soldier.
////////    ///   Because HealthhudBG is a CHILD, Unity inherits that scale, so all text
////////    ///   inside it also gets mirrored. Forcing localScale.x = Abs keeps it upright.
////////    ///
////////    /// Note: The camera billboard rotation that was here before has been removed.
////////    ///   It is not needed for a UI Canvas and was what caused the "rotating text" bug.
////////    /// </summary>
////////    private void LateUpdate()
////////    {
////////        Vector3 s = transform.localScale;
////////        s.x = Mathf.Abs(s.x);      // always positive → never mirrors
////////        transform.localScale = s;
////////    }

////////    // ─── HUD Update ───────────────────────────────────────────────────────────

////////    private void RefreshHUD(SoldierStats stats)
////////    {
////////        float pct = stats.HealthPercent;

////////        if (healthBarFill != null)
////////        {
////////            healthBarFill.fillAmount = pct;
////////            healthBarFill.color = pct > 0.5f ? colorHealthy : colorDanger;
////////        }

////////        if (healthText != null)
////////            healthText.text = $"{Mathf.CeilToInt(stats.CurrentHealth)} / {Mathf.CeilToInt(stats.MaxHealth)}";

////////        if (abilityText != null)
////////            abilityText.text = $"AP  {stats.AbilityPower:F0}";

////////        if (damageText != null)
////////            damageText.text = $"AD  {stats.AttackDamage:F0}";
////////    }

////////    private void HandleDeath(SoldierStats stats) => gameObject.SetActive(false);
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// AREA FORGE - SoldierHUD
/////////
///////// Shows Health / Ability / Damage bars and numbers on the player.
///////// Attach to a child of the soldier prefab (e.g. HealthhudBG).
///////// Subscribes to SoldierStats.OnStatsChanged — updates automatically
///////// whenever equipment is equipped or unequipped.
/////////
///////// ── Inspector wiring ────────────────────────────────────────────────────────
/////////   healthBarFill  → Image (Filled, Horizontal, Left) for health
/////////   abilityBarFill → Image (Filled, Horizontal, Left) for ability
/////////   damageBarFill  → Image (Filled, Horizontal, Left) for damage
/////////
/////////   healthText  → TMP label  (shows current number)
/////////   abilityText → TMP label
/////////   damageText  → TMP label
/////////
/////////   maxAbilityDisplay → value that = 100% full ability bar (default 100)
/////////   maxDamageDisplay  → value that = 100% full damage  bar (default 100)
/////////
///////// ── Reset on Play ────────────────────────────────────────────────────────────
/////////   SoldierStats now keeps bonuses in non-serialized fields, so stopping and
/////////   restarting play mode always resets bars back to the base Inspector values.
///////// </summary>
//////public class SoldierHUD : MonoBehaviour
//////{
//////    // ─── Bars ─────────────────────────────────────────────────────────────────

//////    [Header("Stat Bars (Image Type: Filled → Horizontal → Left)")]
//////    [SerializeField] private Image healthBarFill;
//////    [SerializeField] private Image abilityBarFill;
//////    [SerializeField] private Image damageBarFill;

//////    // ─── Labels ───────────────────────────────────────────────────────────────

//////    [Header("Stat Labels (TextMeshPro — all optional)")]
//////    [SerializeField] private TextMeshProUGUI healthText;
//////    [SerializeField] private TextMeshProUGUI abilityText;
//////    [SerializeField] private TextMeshProUGUI damageText;
//////    [SerializeField] private TextMeshProUGUI nameText;

//////    // ─── Bar Max Values ───────────────────────────────────────────────────────

//////    [Header("Bar Max Reference Values")]
//////    [Tooltip("Ability value that fills the bar to 100%. Default 100.")]
//////    [SerializeField] private float maxAbilityDisplay = 100f;
//////    [Tooltip("Damage value that fills the bar to 100%. Default 100.")]
//////    [SerializeField] private float maxDamageDisplay = 100f;

//////    // ─── Colours ──────────────────────────────────────────────────────────────

//////    [Header("Health Bar Colours")]
//////    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
//////    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

//////    // ─── Private ──────────────────────────────────────────────────────────────

//////    private SoldierStats _stats;

//////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        _stats = GetComponentInParent<SoldierStats>();

//////        if (_stats == null)
//////        {
//////            Debug.LogError("[SoldierHUD] SoldierStats not found on parent. " +
//////                           "This GO must be a child of the soldier prefab.");
//////            return;
//////        }

//////        _stats.OnStatsChanged += RefreshHUD;
//////        _stats.OnSoldierDied += HandleDeath;
//////    }

//////    private void Start()
//////    {
//////        if (_stats != null) RefreshHUD(_stats);

//////        if (nameText != null)
//////            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
//////    }

//////    private void OnDestroy()
//////    {
//////        if (_stats == null) return;
//////        _stats.OnStatsChanged -= RefreshHUD;
//////        _stats.OnSoldierDied -= HandleDeath;
//////    }

//////    private void LateUpdate()
//////    {
//////        // Prevent HUD from mirroring when soldier flips direction (localScale.x = -1)
//////        Vector3 s = transform.localScale;
//////        s.x = Mathf.Abs(s.x);
//////        transform.localScale = s;
//////    }

//////    // ─── Refresh ─────────────────────────────────────────────────────────────

//////    private void RefreshHUD(SoldierStats stats)
//////    {
//////        // ── Health ────────────────────────────────────────────────────────────
//////        float hp = stats.HealthPercent;   // 0–1
//////        if (healthBarFill != null)
//////        {
//////            healthBarFill.fillAmount = hp;
//////            healthBarFill.color = hp > 0.5f ? colorHealthy : colorDanger;
//////        }
//////        if (healthText != null)
//////            healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

//////        // ── Ability Power ─────────────────────────────────────────────────────
//////        float ap = Mathf.Clamp01(stats.AbilityPower / maxAbilityDisplay);
//////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

//////        // ── Attack Damage ─────────────────────────────────────────────────────
//////        float ad = Mathf.Clamp01(stats.AttackDamage / maxDamageDisplay);
//////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
//////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//////    }

//////    private void HandleDeath(SoldierStats stats) => gameObject.SetActive(false);
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// AREA FORGE - SoldierHUD
///////
/////// Displays Health / Ability / Damage bars above the soldier.
/////// Attach to a child of the soldier prefab (e.g. HealthhudBG).
///////
/////// ── Inspector wiring ─────────────────────────────────────────────────────────
///////   healthBarFill  → Image (Filled, Horizontal, Left)
///////   abilityBarFill → Image (Filled, Horizontal, Left)
///////   damageBarFill  → Image (Filled, Horizontal, Left)
///////   healthText     → TMP label  (shows MaxHealth number)
///////   abilityText    → TMP label
///////   damageText     → TMP label
///////
/////// ── Bar Max Reference Values ──────────────────────────────────────────────────
///////   maxAbilityDisplay / maxDamageDisplay control what value = 100% bar fill.
///////   Set these in the Inspector to match your game's expected max stats.
///////
/////// ── Fix: SoldierStats lookup ──────────────────────────────────────────────────
///////   Tries GetComponentInParent first (correct setup: HUD is child of soldier).
///////   Falls back to FindObjectOfType if that fails, so the HUD still works even
///////   when temporarily placed outside the prefab hierarchy during development.
/////// </summary>
////public class SoldierHUD : MonoBehaviour
////{
////    // ─── Bars ─────────────────────────────────────────────────────────────────

////    [Header("Stat Bars (Image Type: Filled → Horizontal → Left)")]
////    [SerializeField] private Image healthBarFill;
////    [SerializeField] private Image abilityBarFill;
////    [SerializeField] private Image damageBarFill;

////    // ─── Labels ───────────────────────────────────────────────────────────────

////    [Header("Stat Labels (TextMeshPro — all optional)")]
////    [SerializeField] private TextMeshProUGUI healthText;
////    [SerializeField] private TextMeshProUGUI abilityText;
////    [SerializeField] private TextMeshProUGUI damageText;
////    [SerializeField] private TextMeshProUGUI nameText;

////    // ─── Bar Max Reference Values ─────────────────────────────────────────────

////    [Header("Bar Max Reference Values")]
////    [Tooltip("Ability value that fills the bar to 100 %. Default 100.")]
////    [SerializeField] private float maxAbilityDisplay = 100f;
////    [Tooltip("Damage value that fills the bar to 100 %. Default 100.")]
////    [SerializeField] private float maxDamageDisplay = 100f;

////    // ─── Colours ──────────────────────────────────────────────────────────────

////    [Header("Health Bar Colours")]
////    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
////    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

////    // ─── Private ──────────────────────────────────────────────────────────────

////    private SoldierStats _stats;

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        // Primary: correct setup — SoldierHUD is a child of the soldier prefab.
////        _stats = GetComponentInParent<SoldierStats>();

////        // Fallback: if placed outside the hierarchy (e.g. during dev / testing).
////        if (_stats == null)
////        {
////            _stats = FindObjectOfType<SoldierStats>();
////            if (_stats != null)
////                Debug.LogWarning("[SoldierHUD] SoldierStats not found in parent. " +
////                                 "Using FindObjectOfType fallback. " +
////                                 "For correct setup, make this GO a child of the soldier prefab.");
////        }

////        if (_stats == null)
////        {
////            Debug.LogError("[SoldierHUD] No SoldierStats found in scene. " +
////                           "Attach SoldierStats to the soldier prefab root.");
////            return;
////        }

////        _stats.OnStatsChanged += RefreshHUD;
////        _stats.OnSoldierDied += HandleDeath;
////    }

////    private void Start()
////    {
////        if (_stats != null) RefreshHUD(_stats);

////        if (nameText != null)
////            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
////    }

////    private void OnDestroy()
////    {
////        if (_stats == null) return;
////        _stats.OnStatsChanged -= RefreshHUD;
////        _stats.OnSoldierDied -= HandleDeath;
////    }

////    /// <summary>
////    /// Prevents the HUD text from mirroring when the soldier flips direction
////    /// (parent localScale.x = -1).
////    /// </summary>
////    private void LateUpdate()
////    {
////        Vector3 s = transform.localScale;
////        s.x = Mathf.Abs(s.x);
////        transform.localScale = s;
////    }

////    // ─── HUD Refresh ─────────────────────────────────────────────────────────

////    /// <summary>
////    /// Called automatically via OnStatsChanged whenever equipment is
////    /// equipped or unequipped, or whenever health changes.
////    /// </summary>
////    private void RefreshHUD(SoldierStats stats)
////    {
////        // ── Health ────────────────────────────────────────────────────────────
////        float hp = stats.HealthPercent;   // 0–1
////        if (healthBarFill != null)
////        {
////            healthBarFill.fillAmount = hp;
////            healthBarFill.color = hp > 0.5f ? colorHealthy : colorDanger;
////        }
////        if (healthText != null)
////            healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

////        // ── Ability Power ─────────────────────────────────────────────────────
////        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

////        // ── Attack Damage ─────────────────────────────────────────────────────
////        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
////        if (damageBarFill != null) damageBarFill.fillAmount = ad;
////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
////    }

////    private void HandleDeath(SoldierStats stats) => gameObject.SetActive(false);
////}

//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - SoldierHUD  (FIXED)
/////
///// Displays Health / Ability / Damage bars in the Customize panel.
/////
///// ── Why it wasn't updating ────────────────────────────────────────────────────
/////   The HUD lives under PlayerStats/HUD — it is NOT a child of the Player GO.
/////   GetComponentInParent therefore always returned null, and the FindObjectOfType
/////   fallback only ran once in Awake (before the soldier was fully initialised).
/////   Result: _stats stayed null → bars never received OnStatsChanged events.
/////
///// ── What changed ──────────────────────────────────────────────────────────────
/////   1. TryFindStats() extracted so both Awake AND Start can attempt the lookup.
/////   2. Start() is now the primary wiring point — everything is guaranteed to
/////      exist by then, so FindObjectOfType always succeeds on the first frame.
/////   3. GameManager.OnSoldierSpawned hook added so the HUD re-links automatically
/////      when a new soldier is spawned at runtime (future-proof).
/////   4. LateUpdate anti-mirror fix kept (prevents text flipping when soldier turns).
/////
///// ── Inspector wiring ──────────────────────────────────────────────────────────
/////   Health Bar Fill   → HealthhudGreen  Image  (Filled / Horizontal / Left)
/////   Ability Bar Fill  → AbilityBrown    Image  (Filled / Horizontal / Left)
/////   Damage Bar Fill   → DamagehudRed    Image  (Filled / Horizontal / Left)
/////   Health Text       → HealthNumber    TextMeshProUGUI
/////   Ability Text      → AbilityNumber   TextMeshProUGUI
/////   Damage Text       → DamageNumber    TextMeshProUGUI
/////   Max Ability Display → 100  (value that fills ability bar to 100%)
/////   Max Damage Display  → 100  (value that fills damage  bar to 100%)
///// </summary>
//public class SoldierHUD : MonoBehaviour
//{
//    // ─── Bars ─────────────────────────────────────────────────────────────────

//    [Header("Stat Bars  (Image Type → Filled → Horizontal → Left)")]
//    [SerializeField] private Image healthBarFill;
//    [SerializeField] private Image abilityBarFill;
//    [SerializeField] private Image damageBarFill;

//    // ─── Labels ───────────────────────────────────────────────────────────────

//    [Header("Stat Labels  (TextMeshPro — all optional)")]
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private TextMeshProUGUI abilityText;
//    [SerializeField] private TextMeshProUGUI damageText;

//    // ─── Bar Max Reference Values ─────────────────────────────────────────────

//    [Header("Bar Max Reference Values")]
//    [Tooltip("Ability value that fills the bar to 100%. Default 100.")]
//    [SerializeField] private float maxAbilityDisplay = 100f;
//    [Tooltip("Damage value that fills the bar to 100%. Default 100.")]
//    [SerializeField] private float maxDamageDisplay = 100f;

//    // ─── Colours ──────────────────────────────────────────────────────────────

//    [Header("Health Bar Colours")]
//    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
//    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private SoldierStats _stats;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        // Subscribe to the spawner so we re-link if a NEW soldier is created later.
//        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//    }

//    private void Start()
//    {
//        // Start() runs after ALL Awake() calls in the scene, so every component
//        // (including SoldierStats) is guaranteed to exist here.
//        TryFindAndLink();
//    }

//    private void OnDestroy()
//    {
//        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//        Unlink();
//    }

//    // ─── Soldier Spawn Callback ───────────────────────────────────────────────

//    /// <summary>
//    /// Called by GameManager when SpawnBasicSoldier() creates a new soldier.
//    /// Re-links the HUD to the fresh SoldierStats component.
//    /// </summary>
//    private void OnSoldierSpawned(GameObject soldierGO)
//    {
//        var newStats = soldierGO.GetComponent<SoldierStats>();
//        if (newStats == null) return;

//        Unlink();
//        _stats = newStats;
//        Link();
//        RefreshHUD(_stats);

//        Debug.Log($"[SoldierHUD] Re-linked to spawned soldier '{soldierGO.name}'.");
//    }

//    // ─── Find & Link ─────────────────────────────────────────────────────────

//    /// <summary>
//    /// Tries to find SoldierStats and subscribes to its events.
//    /// Checks the parent hierarchy first (correct setup), then falls back
//    /// to FindObjectOfType (HUD placed outside the soldier — development mode).
//    /// </summary>
//    private void TryFindAndLink()
//    {
//        if (_stats != null) return; // already linked

//        // 1. Correct setup: HUD is a child of the soldier prefab root.
//        _stats = GetComponentInParent<SoldierStats>();

//        // 2. Fallback: HUD lives elsewhere in the scene (e.g. under PlayerStats/HUD).
//        if (_stats == null)
//        {
//            _stats = FindObjectOfType<SoldierStats>();

//            if (_stats != null)
//                Debug.LogWarning("[SoldierHUD] SoldierStats found via FindObjectOfType. " +
//                                 "For best practice, make SoldierHUD a child of the soldier prefab root.");
//        }

//        if (_stats == null)
//        {
//            Debug.LogError("[SoldierHUD] No SoldierStats found in scene! " +
//                           "Add the SoldierStats component to your Player GameObject.");
//            return;
//        }

//        Link();
//        RefreshHUD(_stats); // draw the bars immediately on first frame
//    }

//    private void Link()
//    {
//        if (_stats == null) return;
//        _stats.OnStatsChanged += RefreshHUD;
//        _stats.OnSoldierDied += HandleDeath;
//    }

//    private void Unlink()
//    {
//        if (_stats == null) return;
//        _stats.OnStatsChanged -= RefreshHUD;
//        _stats.OnSoldierDied -= HandleDeath;
//        _stats = null;
//    }

//    // ─── Anti-Mirror Fix ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Prevents the HUD from mirroring when the parent soldier flips direction
//    /// (parent localScale.x becomes -1). Forces X scale to stay positive.
//    /// </summary>
//    private void LateUpdate()
//    {
//        Vector3 s = transform.localScale;
//        s.x = Mathf.Abs(s.x);
//        transform.localScale = s;
//    }

//    // ─── HUD Refresh ─────────────────────────────────────────────────────────

//    /// <summary>
//    /// Fires automatically via OnStatsChanged whenever:
//    ///   • An item is equipped or unequipped (CharacterEquipment → SoldierStats.ApplyEquipmentBonus)
//    ///   • The soldier takes damage or heals
//    /// </summary>
//    private void RefreshHUD(SoldierStats stats)
//    {
//        // ── Health ────────────────────────────────────────────────────────────
//        float hp = stats.HealthPercent;   // 0 – 1
//        if (healthBarFill != null)
//        {
//            healthBarFill.fillAmount = hp;
//            healthBarFill.color = hp > 0.5f ? colorHealthy : colorDanger;
//        }
//        if (healthText != null)
//            healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

//        // ── Ability Power ─────────────────────────────────────────────────────
//        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
//        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

//        // ── Attack Damage ─────────────────────────────────────────────────────
//        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
//        if (damageBarFill != null) damageBarFill.fillAmount = ad;
//        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//    }

//    private void HandleDeath(SoldierStats stats)
//    {
//        gameObject.SetActive(false);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - SoldierHUD  (FIXED v3)
///
/// ROOT CAUSE OF HEALTH BAR NOT UPDATING:
///   Old code used stats.HealthPercent = CurrentHealth / MaxHealth.
///   When you equip armor, BOTH CurrentHealth and MaxHealth increase by the same
///   amount — so the ratio stays 1.0 (100%) forever. The bar was always full.
///
///   FIX: Health bar now uses  stats.MaxHealth / maxHealthDisplay
///   (same pattern as ability and damage bars).
///   Set maxHealthDisplay = the maximum possible MaxHealth in your game (e.g. 200).
///   Equipping armor increases MaxHealth → bar visibly grows. ✓
///
/// Inspector wiring:
///   Health Bar Fill     → HealthhudGreen  (Image Type: Filled / Horizontal / Left)
///   Ability Bar Fill    → AbilityBrown    (Image Type: Filled / Horizontal / Left)
///   Damage Bar Fill     → DamagehudRed    (Image Type: Filled / Horizontal / Left)
///   Health Text         → HealthNumber    (TextMeshPro)
///   Ability Text        → AbilityNumber   (TextMeshPro)
///   Damage Text         → DamageNumber    (TextMeshPro)
///   Max Health Display  → 200
///   Max Ability Display → 100
///   Max Damage Display  → 100
/// </summary>
public class SoldierHUD : MonoBehaviour
{
    // ─── Stat Bars ────────────────────────────────────────────────────────────

    [Header("Stat Bars — Image Type → Filled → Horizontal → Left")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image abilityBarFill;
    [SerializeField] private Image damageBarFill;

    // ─── Stat Labels ──────────────────────────────────────────────────────────

    [Header("Stat Labels (TextMeshPro — optional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;

    // ─── Bar Max Reference Values ─────────────────────────────────────────────

    [Header("Bar Max Reference Values")]
    [Tooltip("MaxHealth value that fills the health bar to 100%. E.g. if max possible HP is 200, set 200.")]
    [SerializeField] private float maxHealthDisplay = 100f;
    [Tooltip("AbilityPower value that fills the ability bar to 100%.")]
    [SerializeField] private float maxAbilityDisplay = 100f;
    [Tooltip("AttackDamage value that fills the damage bar to 100%.")]
    [SerializeField] private float maxDamageDisplay = 100f;

    // ─── Colours ──────────────────────────────────────────────────────────────

    [Header("Health Bar Colours")]
    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        GameManager.OnSoldierSpawned += OnSoldierSpawned;
    }

    private void Start()
    {
        TryFindAndLink();
    }

    private void OnDestroy()
    {
        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
        Unlink();
    }

    // ─── Spawn Callback ───────────────────────────────────────────────────────

    private void OnSoldierSpawned(GameObject soldierGO)
    {
        var newStats = soldierGO.GetComponent<SoldierStats>();
        if (newStats == null) return;
        Unlink();
        _stats = newStats;
        Link();
        RefreshHUD(_stats);
        Debug.Log($"[SoldierHUD] Re-linked to spawned soldier '{soldierGO.name}'.");
    }

    // ─── Link / Unlink ────────────────────────────────────────────────────────

    private void TryFindAndLink()
    {
        if (_stats != null) return;

        _stats = GetComponentInParent<SoldierStats>();

        if (_stats == null)
        {
            // AFTER (fix):
            _stats = FindFirstObjectByType<SoldierStats>();
            if (_stats != null)
                Debug.LogWarning("[SoldierHUD] Found SoldierStats via FindObjectOfType — fallback mode.");
        }

        if (_stats == null)
        {
            Debug.LogError("[SoldierHUD] No SoldierStats in scene! " +
                           "Add SoldierStats component to your Player GameObject.");
            return;
        }

        Link();
        RefreshHUD(_stats);
        Debug.Log($"[SoldierHUD] Linked to '{_stats.gameObject.name}'. " +
                  $"HP:{_stats.MaxHealth} AP:{_stats.AbilityPower} AD:{_stats.AttackDamage}");
    }

    private void Link()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged += RefreshHUD;
        _stats.OnSoldierDied += HandleDeath;
    }

    private void Unlink()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged -= RefreshHUD;
        _stats.OnSoldierDied -= HandleDeath;
        _stats = null;
    }

    // ─── Anti-Mirror (LateUpdate) ─────────────────────────────────────────────

    private void LateUpdate()
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // ─── HUD Refresh ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called automatically via OnStatsChanged every time an item is
    /// equipped / unequipped or the soldier takes damage / heals.
    ///
    /// KEY FIX: All three bars now use (value / maxDisplay) so each bar
    /// visibly grows when equipment bonuses increase a stat.
    /// Previously health used HealthPercent (CurrentHP/MaxHP) which is
    /// always 1.0 when the soldier is at full health — bar never moved.
    /// </summary>
    private void RefreshHUD(SoldierStats stats)
    {
        // ── Health ─────────────────────────────────────────────────────────────
        //  Fill = MaxHealth / maxHealthDisplay
        //  e.g. base 100 HP, maxHealthDisplay=200 → bar at 50%
        //       equip +10 armor → MaxHealth=110 → bar grows to 55% ✓
        float hpFill = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = hpFill;
            //healthBarFill.color = hpFill > 0.5f ? colorHealthy : colorDanger;
        }
        if (healthText != null)
            healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();

        // ── Ability Power ──────────────────────────────────────────────────────
        float apFill = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
        if (abilityBarFill != null)
            abilityBarFill.fillAmount = apFill;
        if (abilityText != null)
            abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();

        // ── Attack Damage ──────────────────────────────────────────────────────
        float adFill = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));
        if (damageBarFill != null)
            damageBarFill.fillAmount = adFill;
        if (damageText != null)
            damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();

        // Debug log — remove once confirmed working
        Debug.Log($"[SoldierHUD] RefreshHUD → " +
                  $"HP:{stats.MaxHealth} fill:{hpFill:F2} | " +
                  $"AP:{stats.AbilityPower} fill:{apFill:F2} | " +
                  $"AD:{stats.AttackDamage} fill:{adFill:F2}");
    }

    private void HandleDeath(SoldierStats stats)
    {
        gameObject.SetActive(false);
    }

    // ─── Editor Test ──────────────────────────────────────────────────────────

    [ContextMenu("Force Refresh HUD")]
    private void EditorForceRefresh()
    {
        // AFTER:
        if (_stats == null) _stats = FindFirstObjectByType<SoldierStats>();
        if (_stats != null) RefreshHUD(_stats);
        else Debug.LogWarning("[SoldierHUD] No SoldierStats found to refresh.");
    }
}