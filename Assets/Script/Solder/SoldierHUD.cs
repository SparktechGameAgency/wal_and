//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - SoldierHUD
///// Displays a soldier's stats on a world-space or screen-space HUD canvas.
///// Health bar uses Image.fillAmount (no Slider component needed).
/////
///// Inspector setup for the health bar:
/////   1. Create a Panel → name it "HealthBarBG"  (dark background colour)
/////   2. Inside it, add an Image → name it "HealthBarFill"
/////      • Image Type  : Filled
/////      • Fill Method : Horizontal
/////      • Fill Origin : Left
/////   3. Assign HealthBarFill to the healthBarFill field below.
/////   4. Assign a TextMeshProUGUI for healthText — it will show "85 / 100".
/////
///// MULTIPLAYER NOTE: HUD reads from SyncVar values — no changes needed here.
///// </summary>
//public class SoldierHUD : MonoBehaviour
//{
//    // ─── HUD References ───────────────────────────────────────────────────────

//    [Header("Health Bar (Image — Fill type, no Slider)")]
//    [Tooltip("The filled Image that acts as the health bar. Set Image Type → Filled → Horizontal.")]
//    [SerializeField] private Image healthBarFill;

//    [Header("Stat Labels (TextMeshPro)")]
//    [Tooltip("Shows  'currentHP / maxHP'  e.g.  '85 / 100'")]
//    [SerializeField] private TextMeshProUGUI healthText;   // FIX: now shows "85 / 100"
//    [SerializeField] private TextMeshProUGUI abilityText;  // e.g. "AP  10"
//    [SerializeField] private TextMeshProUGUI damageText;   // e.g. "AD  15"

//    [Header("Name Tag (optional)")]
//    [SerializeField] private TextMeshProUGUI nameText;

//    [Header("Health Bar Colours")]
//    [Tooltip("Bar colour when health is above 50 %")]
//    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f); // green
//    [Tooltip("Bar colour when health is at or below 50 %")]
//    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f); // red

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private SoldierStats _stats;

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _stats = GetComponentInParent<SoldierStats>();

//        if (_stats == null)
//        {
//            Debug.LogError("[SoldierHUD] SoldierStats not found on parent. " +
//                           "Place SoldierHUD as a child of the soldier prefab.");
//            return;
//        }

//        _stats.OnStatsChanged += RefreshHUD;
//        _stats.OnSoldierDied += HandleDeath;
//    }

//    private void Start()
//    {
//        if (_stats != null)
//            RefreshHUD(_stats);

//        if (nameText != null)
//            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
//    }

//    private void OnDestroy()
//    {
//        if (_stats != null)
//        {
//            _stats.OnStatsChanged -= RefreshHUD;
//            _stats.OnSoldierDied -= HandleDeath;
//        }
//    }

//    // ─── HUD Update ───────────────────────────────────────────────────────────

//    private void RefreshHUD(SoldierStats stats)
//    {
//        float pct = stats.HealthPercent; // 0.0 – 1.0

//        // ── Health bar fill ──────────────────────────────────────────────────
//        if (healthBarFill != null)
//        {
//            healthBarFill.fillAmount = pct;
//            healthBarFill.color = (pct > 0.5f) ? colorHealthy : colorDanger;
//        }

//        // ── Health text — FIX: shows "currentHP / maxHP" e.g. "85 / 100" ───
//        // Previously this only showed the current HP number with no max,
//        // so the player had no context for how much health was left.
//        if (healthText != null)
//            healthText.text = $"{Mathf.CeilToInt(stats.CurrentHealth)} / {Mathf.CeilToInt(stats.MaxHealth)}";

//        // ── Ability power ─────────────────────────────────────────────────────
//        if (abilityText != null)
//            abilityText.text = $"AP  {stats.AbilityPower:F0}";

//        // ── Attack damage ─────────────────────────────────────────────────────
//        if (damageText != null)
//            damageText.text = $"AD  {stats.AttackDamage:F0}";
//    }

//    private void HandleDeath(SoldierStats stats)
//    {
//        gameObject.SetActive(false);
//    }

//    // ─── World-Space Billboard ───────────────────────────────────────────────

//    /// <summary>
//    /// If your HUD Canvas is World Space, this keeps it facing the camera.
//    /// Delete this method if you're using Screen Space - Overlay.
//    /// </summary>
//    //private void LateUpdate()
//    //{
//    //    if (Camera.main != null)
//    //        transform.rotation = Camera.main.transform.rotation;
//    //}

//    private void LateUpdate()
//    {
//        // Counter the parent soldier's localScale.x flip so HUD text never mirrors.
//        // The billboard camera rotation is removed — it is not needed for UI Canvas.
//        Vector3 s = transform.localScale;
//        s.x = Mathf.Abs(s.x);   // always keep X positive, regardless of soldier direction
//        transform.localScale = s;
//        transform.localScale.x = Mathf.Abs(s.x); // always positive → never mirrors
//    }

//    //    private void LateUpdate()
//    //{
//    //    // Counter-scale so the HUD never mirrors with the parent soldier
//    //    Vector3 s = transform.localScale;
//    //    s.x = Mathf.Abs(s.x); // always keep HUD scale positive X
//    //    transform.localScale = s;

//    //    // Keep HUD facing camera (for World Space canvas)
//    //    if (Camera.main != null)
//    //        transform.rotation = Camera.main.transform.rotation;
//    //}
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - SoldierHUD
/// Displays health bar + stat labels above the soldier.
/// Attached to HealthhudBG (child of SolderPrefab).
///
/// FIX: LateUpdate now counter-scales localScale.x so the HUD text never
/// mirrors when the parent soldier flips direction.
/// </summary>
public class SoldierHUD : MonoBehaviour
{
    // ─── HUD References ───────────────────────────────────────────────────────

    [Header("Health Bar")]
    [Tooltip("The filled Image acting as health bar — Image Type: Filled, Horizontal, Left")]
    [SerializeField] private Image healthBarFill;

    [Header("Stat Labels (TextMeshPro — all optional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Health Bar Colours")]
    [SerializeField] private Color colorHealthy = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color colorDanger = new Color(0.85f, 0.15f, 0.15f);

    // ─── Private ──────────────────────────────────────────────────────────────

    private SoldierStats _stats;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _stats = GetComponentInParent<SoldierStats>();

        if (_stats == null)
        {
            Debug.LogError("[SoldierHUD] SoldierStats not found on parent. " +
                           "HealthhudBG must be a child of the soldier prefab.");
            return;
        }

        _stats.OnStatsChanged += RefreshHUD;
        _stats.OnSoldierDied += HandleDeath;
    }

    private void Start()
    {
        if (_stats != null) RefreshHUD(_stats);

        if (nameText != null)
            nameText.text = transform.parent != null ? transform.parent.name : "Soldier";
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged -= RefreshHUD;
        _stats.OnSoldierDied -= HandleDeath;
    }

    /// <summary>
    /// FIX: Counter-scale localScale.x every frame so the HUD never mirrors
    /// when the parent soldier's localScale.x flips to -1.
    ///
    /// Why the text was flipping:
    ///   SoldierController sets transform.localScale.x = -1 to mirror the soldier.
    ///   Because HealthhudBG is a CHILD, Unity inherits that scale, so all text
    ///   inside it also gets mirrored. Forcing localScale.x = Abs keeps it upright.
    ///
    /// Note: The camera billboard rotation that was here before has been removed.
    ///   It is not needed for a UI Canvas and was what caused the "rotating text" bug.
    /// </summary>
    private void LateUpdate()
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x);      // always positive → never mirrors
        transform.localScale = s;
    }

    // ─── HUD Update ───────────────────────────────────────────────────────────

    private void RefreshHUD(SoldierStats stats)
    {
        float pct = stats.HealthPercent;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = pct;
            healthBarFill.color = pct > 0.5f ? colorHealthy : colorDanger;
        }

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(stats.CurrentHealth)} / {Mathf.CeilToInt(stats.MaxHealth)}";

        if (abilityText != null)
            abilityText.text = $"AP  {stats.AbilityPower:F0}";

        if (damageText != null)
            damageText.text = $"AD  {stats.AttackDamage:F0}";
    }

    private void HandleDeath(SoldierStats stats) => gameObject.SetActive(false);
}