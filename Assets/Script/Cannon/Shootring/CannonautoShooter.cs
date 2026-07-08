using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-detects enemies in range and fires the cannon automatically.
/// Replaces CannonControllerShot — no fire button needed.
///
/// ── Battle scene support ───────────────────────────────────────────────────
/// This same component lives on the carried-over castle and keeps running
/// when the whole CastleGrid is reparented into the Battle scene
/// (BattleManager.ReceivePlayerCastle). Previously it only ever scanned
/// EnemyUnit.All — the Village's wave-defense enemy list — so in the Battle
/// scene (where there are no EnemyUnit instances at all; opponents are
/// BattleUnit instances tracked by BattleManager) it never found a target
/// and never fired. It now checks BattleManager.Instance: if set, it reads
/// the target off the sibling BattleUnit component instead (the exact same
/// unit BattleUnit.Update()/BattleManager.FindNearestEnemy() already
/// computes every frame) — same pattern BattleDragonFlight already uses for
/// dragons. See IDamageable.cs.
///
/// ── Arc height fix ─────────────────────────────────────────────────────────
/// arcHeight is now written directly onto the ProjectileArc component field
/// before Launch() is called. This means:
///   • Changing arcHeight HERE applies to every shot.
///   • The ProjectileArc component on the prefab shows the live value.
///   • You can also set arcHeight directly on the ProjectileArc prefab —
///     CannonAutoShooter will overwrite it at fire-time with its own value.
///
/// ── Inspector wiring ───────────────────────────────────────────────────────
///   cannonAnimator    → SpriteAnimator on the cannon Image (auto-found)
///   projectileSpawner → Empty RectTransform at the barrel mouth
///   projectilePrefab  → Prefab with Image + ProjectileArc + ProjectileBlast
///   projectileSize    → Pixel size of the projectile in flight (default 40x40)
///   detectionRadius   → Radius in world units; yellow gizmo shows it in Scene
///   fireInterval      → Seconds between shots
///   damage            → HP removed per hit
///   arcHeight         → Peak height above the straight line (pixels). LOW values
///                       (5-30) give a nearly flat trajectory.
///   flightDuration    → Seconds for the projectile to reach the target
/// </summary>
public class CannonAutoShooter : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("References (auto-found if left empty)")]
    public SpriteAnimator cannonAnimator;
    public RectTransform projectileSpawner;
    public GameObject projectilePrefab;

    [Header("Projectile Visuals")]
    [Tooltip("Pixel size (width x height) of the projectile while in flight.")]
    public Vector2 projectileSize = new Vector2(40f, 40f);

    [Header("Combat")]
    [Tooltip("Detection radius in world units (pixels in Screen Space Overlay).")]
    public float detectionRadius = 300f;

    [Tooltip("Seconds between shots.")]
    public float fireInterval = 2f;

    [Tooltip("Damage per hit.")]
    public float damage = 50f;

    [Header("Projectile Arc")]
    // Arc height is hardcoded to 20 pixels (nearly flat trajectory).
    // Using a const prevents stale serialized values in old prefabs/scenes
    // from silently overriding the intended value.
    private const float arcHeight = 0.5f;

    [Tooltip("Seconds for the projectile to reach the target.")]
    public float flightDuration = 1.2f;

    [Header("Debug")]
    public bool showGizmo = true;

    // ── Private ───────────────────────────────────────────────────
    private bool _isFiring = false;
    private float _fireCooldown = 0f;
    // Was "EnemyUnit _lockedTarget" — now IDamageable so this same field
    // holds either an EnemyUnit (Village) or a BattleUnit (Battle scene).
    private IDamageable _lockedTarget = null;
    private Canvas _rootCanvas = null;

    private CastleUnitDraggable _draggable;

    // Present only once this cannon is carried into the Battle scene and
    // BattleManager attaches a BattleUnit to it (see BattleManager.
    // FindExistingCastleUnit / SpawnPlayerArmy). Null in the Village.
    private BattleUnit _battleUnit;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _draggable = GetComponent<CastleUnitDraggable>();
        _battleUnit = GetComponent<BattleUnit>();
        AutoFindReferences();
    }

    private void Start()
    {
        WireAnimator();
        _rootCanvas = FindRootCanvas();
        _fireCooldown = Random.Range(0f, fireInterval);
    }

    /// <summary>
    /// Called by Unity when this GameObject is disabled (e.g. panel opens,
    /// cannon is reparented, or CanvasGroup.blocksRaycasts changes).
    /// Resets firing state so the cannon never gets stuck after interruption.
    /// </summary>
    private void OnDisable()
    {
        s_all.Remove(this);
        if (_isFiring && cannonAnimator != null)
            cannonAnimator.Stop();
        _isFiring = false;
        _lockedTarget = null;
    }

    /// <summary>
    /// Called by Unity when this GameObject is re-enabled.
    /// Re-wires the animator callbacks (they are cleared by Stop/ForceReset)
    /// and adds a small random delay before the first shot so cannons don't
    /// all fire simultaneously when the panel closes.
    /// </summary>
    private void OnEnable()
    {
        if (!s_all.Contains(this)) s_all.Add(this);
        WireAnimator();
        // Stagger re-entry so it doesn't fire the instant it re-enables
        _fireCooldown = Random.Range(0.2f, fireInterval);
    }

    private void Update()
    {
        if (_isFiring) return;
        if (IsDragging()) return;

        // This GameObject's Awake() already ran back in the Village (long
        // before BattleManager exists), so the BattleUnit component this
        // cannon gets in the Battle scene — added AFTER carry-over by
        // BattleManager.FindExistingCastleUnit — did not exist yet when
        // Awake() cached _battleUnit. Keep checking until it's found (once
        // found it never goes away, so this only actually runs GetComponent
        // for the handful of frames after the scene loads).
        if (_battleUnit == null)
            _battleUnit = GetComponent<BattleUnit>();

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown > 0f) return;

        IDamageable target = FindTargetInRange();
        if (target == null) return;

        _lockedTarget = target;
        _isFiring = true;
        _fireCooldown = fireInterval;

        if (cannonAnimator != null && cannonAnimator.frames != null && cannonAnimator.frames.Length > 0)
            cannonAnimator.Play();
        else
            SpawnProjectile();
    }

    // ── Gizmo ─────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    // ── Enemy Detection ───────────────────────────────────────────

    /// <summary>
    /// Battle scene: reuses the nearest-enemy result the sibling BattleUnit
    /// already computes every frame (BattleManager.FindNearestEnemy) instead
    /// of scanning EnemyUnit.All, which is always empty in the Battle scene.
    /// Village: unchanged — scans EnemyUnit.All exactly as before.
    /// </summary>
    private IDamageable FindTargetInRange()
    {
        if (BattleManager.Instance != null && _battleUnit != null)
        {
            BattleUnit target = _battleUnit.CurrentTarget;
            if (target == null || target.IsDead) return null;

            float distBattle = Vector3.Distance(transform.position, target.transform.position);
            return distBattle <= detectionRadius ? (IDamageable)target : null;
        }

        EnemyUnit nearest = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (EnemyUnit enemy in EnemyUnit.All)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(myPos, enemy.transform.position);
            if (dist <= detectionRadius && dist < bestDist)
            {
                bestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // ── Spawn Projectile ──────────────────────────────────────────

    private void SpawnProjectile()
    {
        if (_lockedTarget == null || _lockedTarget.IsDead)
        {
            Debug.Log("[CannonAutoShooter] Target gone before projectile spawned.");
            _lockedTarget = null;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("[CannonAutoShooter] projectilePrefab not assigned!");
            return;
        }

        Vector3 spawnPos = projectileSpawner != null
            ? projectileSpawner.position
            : transform.position;

        if (_rootCanvas == null) _rootCanvas = FindRootCanvas();
        Transform canvasT = _rootCanvas != null ? _rootCanvas.transform : transform.root;

        GameObject projObj = Instantiate(projectilePrefab, canvasT);

        // Force center anchors + explicit pixel size so Image is always visible
        RectTransform projRt = projObj.GetComponent<RectTransform>();
        if (projRt != null)
        {
            projRt.anchorMin = new Vector2(0.5f, 0.5f);
            projRt.anchorMax = new Vector2(0.5f, 0.5f);
            projRt.pivot = new Vector2(0.5f, 0.5f);
            projRt.sizeDelta = projectileSize;
            projRt.position = spawnPos;
            projRt.SetAsLastSibling();
        }

        // Ensure Image is enabled and opaque
        Image img = projObj.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = true;
            Color c = img.color;
            c.a = 1f;
            img.color = c;
            img.raycastTarget = false;
        }

        // Write arcHeight onto the component so the Inspector reflects it,
        // then call Launch — this is what makes Inspector edits take effect.
        // ── Resolve end position in the same coordinate space as spawnPos ──────
        // spawnPos is always in screen pixels (SSO canvas RectTransform.position).
        // If the enemy has a RectTransform it is a UI element and its position is
        // already in screen pixels.  If it is a world-space GameObject its position
        // is in world units — mixing both spaces causes Lerp to throw the projectile
        // far off-screen no matter how small arcHeight is.
        // IDamageable only guarantees a Transform (DamageableTransform), not
        // Component-specific members — GetComponent<RectTransform>() needs
        // to go through that Transform rather than _lockedTarget directly.
        // Works identically for an EnemyUnit (Village) or a BattleUnit
        // (Battle scene) either way, since both are UI RectTransforms.
        Transform targetTransform = _lockedTarget.DamageableTransform;
        Vector3 endPos;
        bool enemyIsUI = targetTransform.GetComponent<RectTransform>() != null;
        if (enemyIsUI)
        {
            // Was targetTransform.position — the raw RectTransform pivot,
            // which sits at the enemy's FEET for ground units (bottom
            // pivot, so they sit correctly on the ground line). That put
            // every cannonball at the target's feet instead of its body.
            // See AimPointUtil for the full explanation.
            endPos = AimPointUtil.GetBodyCenter(targetTransform);
        }
        else
        {
            Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            endPos = (cam != null)
                ? cam.WorldToScreenPoint(targetTransform.position)
                : targetTransform.position;
            endPos.z = 0f;   // canvas lives at z = 0
        }
        Debug.Log($"[CannonAutoShooter] Fire | spawn={spawnPos} end={endPos} enemyIsUI={enemyIsUI}");

        ProjectileArc arc = projObj.GetComponent<ProjectileArc>();
        if (arc != null)
        {
            arc.Launch(
                spawnPos,
                endPos,             // same-space position (screen pixels)
                arcHeight,
                flightDuration,
                _lockedTarget,
                damage
            );
        }
        else
        {
            Debug.LogError("[CannonAutoShooter] ProjectileArc missing on projectile prefab!");
            Destroy(projObj);
        }

        _lockedTarget = null;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private bool IsDragging()
    {
        if (_draggable == null) return false;
        return CastleUnitDraggable.CurrentlyDragging == _draggable;
    }

    private Canvas FindRootCanvas()
    {
        Canvas[] parents = GetComponentsInParent<Canvas>(includeInactive: false);
        if (parents != null && parents.Length > 0) return parents[parents.Length - 1];
        return Object.FindFirstObjectByType<Canvas>();
    }

    private void AutoFindReferences()
    {
        if (cannonAnimator == null)
        {
            cannonAnimator = GetComponent<SpriteAnimator>()
                          ?? GetComponentInChildren<SpriteAnimator>();

            if (cannonAnimator != null)
                Debug.Log("[CannonAutoShooter] Auto-found SpriteAnimator: "
                          + cannonAnimator.gameObject.name);
            else
                Debug.LogWarning("[CannonAutoShooter] SpriteAnimator not found.");
        }

        if (projectileSpawner == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string lw = child.name.ToLower();
                if (lw.Contains("spawner") || lw.Contains("barrel") || lw.Contains("muzzle"))
                {
                    projectileSpawner = child.GetComponent<RectTransform>();
                    Debug.Log("[CannonAutoShooter] Auto-found spawner: " + child.name);
                    break;
                }
            }
        }

        if (projectilePrefab == null)
            Debug.LogWarning("[CannonAutoShooter] projectilePrefab not assigned.");
    }

    private void WireAnimator()
    {
        if (cannonAnimator == null) return;
        cannonAnimator.onSpawnFrame = SpawnProjectile;
        cannonAnimator.onComplete = () => _isFiring = false;
    }
    // Static pause/resume for all active shooters

    private static readonly System.Collections.Generic.List<CannonAutoShooter> s_all =
        new System.Collections.Generic.List<CannonAutoShooter>();

    /// <summary>
    /// Stop every active cannon fire animation before a panel opens.
    /// Prevents _isFiring from getting stuck when coroutines are interrupted.
    /// </summary>
    public static void PauseAll()
    {
        foreach (var shooter in s_all)
        {
            if (shooter == null) continue;
            if (shooter._isFiring && shooter.cannonAnimator != null)
                shooter.cannonAnimator.Stop();
            shooter._isFiring = false;
            shooter._lockedTarget = null;
        }
    }

    /// <summary>
    /// Re-wire animators and stagger cooldowns after a panel closes.
    /// </summary>
    public static void ResumeAll()
    {
        foreach (var shooter in s_all)
        {
            if (shooter == null) continue;
            shooter.WireAnimator();
            shooter._fireCooldown = Random.Range(0.1f, shooter.fireInterval);
        }
    }

}