//////using System.Collections;
//////using System.Collections.Generic;
//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// ArcherUnit
/////////
///////// Attach to the Archer prefab root alongside Image + SpriteAnimator.
/////////
///////// ── Behaviour ──────────────────────────────────────────────────────────────
/////////   • Stays idle (playing idle frames) while no enemy is in range.
/////////   • When an enemy enters detectionRadius, locks onto the closest one
/////////     and plays the shoot animation.
/////////   • On the spawnFrame of the shoot animation an ArrowProjectile is fired.
/////////   • After the shoot animation finishes the unit waits fireInterval seconds
/////////     before shooting again.
/////////   • If the locked target dies or leaves range the unit returns to idle.
/////////
///////// ── Inspector wiring ───────────────────────────────────────────────────────
/////////   idleAnimator    → SpriteAnimator component driving the idle sprite sheet.
/////////   shootAnimator   → SpriteAnimator component driving the shoot sprite sheet.
/////////                     Set loop = false on the shoot animator.
/////////   arrowSpawner    → Empty RectTransform at the bow / hand position
/////////                     (child of this GameObject).
/////////   arrowPrefab     → Prefab with an ArrowProjectile component.
/////////   arrowSize       → Pixel size of the in-flight arrow (default 32 × 8).
/////////   detectionRadius → World-unit radius to scan for EnemyUnit instances.
/////////   fireInterval    → Minimum seconds between shots.
/////////   damage          → HP removed per arrow hit.
/////////
///////// ── Child hierarchy (auto-wired by name) ───────────────────────────────────
/////////   ArcherUnit
/////////   ├── IdleImage     Image driven by idleAnimator
/////////   ├── ShootImage    Image driven by shootAnimator (hidden while idle)
/////////   └── Spawnpoint    RectTransform — arrow origin
/////////
///////// ── Dependencies ───────────────────────────────────────────────────────────
/////////   SpriteAnimator   (Script/Cannon/Shootring/SpriteAnimator.cs)
/////////   EnemyUnit        (Script/Cannon/Shootring/EnemyUnit.cs)
/////////   ArrowProjectile  (Script/Castle/ArrowProjectile.cs)
///////// </summary>
//////public class ArcherUnit : MonoBehaviour
//////{
//////    // ── Inspector ─────────────────────────────────────────────────

//////    [Header("Animators (auto-found if left empty)")]
//////    [Tooltip("SpriteAnimator for idle frames — loops continuously.")]
//////    public SpriteAnimator idleAnimator;

//////    [Tooltip("SpriteAnimator for shoot frames — plays once per shot.")]
//////    public SpriteAnimator shootAnimator;

//////    [Header("Arrow")]
//////    [Tooltip("RectTransform at the bow tip. Auto-found by name 'Spawnpoint' if empty.")]
//////    public RectTransform arrowSpawner;

//////    [Tooltip("Prefab with an ArrowProjectile component.")]
//////    public GameObject arrowPrefab;

//////    [Tooltip("Pixel size (width x height) of the arrow while in flight.")]
//////    public Vector2 arrowSize = new Vector2(8f, 32f);   // sprite faces up: width narrow, height tall

//////    [Header("Combat")]
//////    [Tooltip("World-unit detection radius.  Yellow gizmo shown in Scene view.")]
//////    public float detectionRadius = 250f;

//////    [Tooltip("Seconds between shots (after the shoot animation finishes).")]
//////    public float fireInterval = 1.8f;

//////    [Tooltip("Damage dealt to the enemy per arrow hit.")]
//////    public float damage = 40f;

//////    [Header("Projectile Arc")]
//////    [Tooltip("Peak height above straight line, in pixels. Keep low (0-30) for arrows.")]
//////    public float arcHeight = 10f;

//////    [Tooltip("Seconds for the arrow to reach the target.")]
//////    public float flightDuration = 0.6f;

//////    [Header("Debug")]
//////    public bool showGizmo = true;

//////    // ── Private ───────────────────────────────────────────────────

//////    private ArcherSlot _ownerSlot;
//////    private EnemyUnit _lockedTarget;
//////    private bool _isShooting = false;
//////    private float _cooldown = 0f;
//////    private Canvas _rootCanvas;

//////    // Child GameObjects toggled when switching idle ↔ shoot
//////    private GameObject _idleImageGO;
//////    private GameObject _shootImageGO;

//////    // ── Init (called by ArcherSlot) ───────────────────────────────

//////    /// <summary>Called by ArcherSlot.PlaceArcher() immediately after instantiation.</summary>
//////    public void Init(ArcherSlot ownerSlot)
//////    {
//////        _ownerSlot = ownerSlot;
//////    }

//////    // ── Lifecycle ─────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        AutoFindReferences();
//////        WireAnimators();

//////        // Always start with idle visible, shoot hidden.
//////        // Done in Awake so the shoot image is never briefly visible on frame 0.
//////        _idleImageGO?.SetActive(true);
//////        _shootImageGO?.SetActive(false);
//////    }

//////    private void Start()
//////    {
//////        _rootCanvas = GetComponentInParent<Canvas>();
//////        PlayIdle();
//////    }

//////    private void Update()
//////    {
//////        if (_isShooting) return;

//////        // Tick down cooldown
//////        if (_cooldown > 0f)
//////        {
//////            _cooldown -= Time.deltaTime;
//////            return;
//////        }

//////        // Find nearest enemy in range
//////        EnemyUnit target = FindClosestEnemy();

//////        if (target == null)
//////        {
//////            // No enemy — ensure idle is playing
//////            if (_lockedTarget != null)
//////            {
//////                _lockedTarget = null;
//////                PlayIdle();
//////            }
//////            return;
//////        }

//////        // Enemy found — shoot
//////        _lockedTarget = target;
//////        Shoot();
//////    }

//////    // ── Combat ────────────────────────────────────────────────────

//////    private void Shoot()
//////    {
//////        if (_isShooting || _lockedTarget == null) return;
//////        _isShooting = true;

//////        // Stop idle animation before switching to shoot.
//////        idleAnimator?.Stop();

//////        ShowShootImage();

//////        if (shootAnimator != null)
//////        {
//////            // Arrow fires on shootAnimator.spawnOnFrame
//////            shootAnimator.onSpawnFrame = SpawnArrow;
//////            shootAnimator.onComplete = OnShootAnimComplete;
//////            shootAnimator.Play();
//////        }
//////        else
//////        {
//////            // No animator — fire immediately and reset
//////            SpawnArrow();
//////            OnShootAnimComplete();
//////        }
//////    }

//////    private void SpawnArrow()
//////    {
//////        if (_lockedTarget == null || _lockedTarget.IsDead)
//////        {
//////            // Target lost before projectile spawns
//////            return;
//////        }

//////        if (arrowPrefab == null)
//////        {
//////            Debug.LogWarning("[ArcherUnit] arrowPrefab not assigned — cannot fire.");
//////            return;
//////        }

//////        // Choose a parent canvas for the projectile
//////        Transform projectileParent = _rootCanvas != null
//////            ? _rootCanvas.transform
//////            : transform.root;

//////        Vector3 spawnPos = arrowSpawner != null
//////            ? arrowSpawner.position
//////            : transform.position;

//////        Vector3 targetPos = _lockedTarget.transform.position;

//////        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, projectileParent);

//////        // Size the arrow
//////        RectTransform rt = arrowGO.GetComponent<RectTransform>();
//////        if (rt != null) rt.sizeDelta = arrowSize;

//////        // Launch
//////        ArrowProjectile arrow = arrowGO.GetComponent<ArrowProjectile>();
//////        if (arrow != null)
//////            arrow.Launch(spawnPos, targetPos, arcHeight, flightDuration, _lockedTarget, damage);
//////        else
//////            Debug.LogWarning("[ArcherUnit] arrowPrefab is missing an ArrowProjectile component.");
//////    }

//////    private void OnShootAnimComplete()
//////    {
//////        _isShooting = false;
//////        _cooldown = fireInterval;
//////        _lockedTarget = null;   // re-evaluate next frame

//////        PlayIdle();
//////    }

//////    // ── Enemy detection ───────────────────────────────────────────

//////    private EnemyUnit FindClosestEnemy()
//////    {
//////        EnemyUnit closest = null;
//////        float bestDist = float.MaxValue;

//////        foreach (EnemyUnit enemy in EnemyUnit.All)
//////        {
//////            if (enemy == null || enemy.IsDead) continue;

//////            float dist = Vector3.Distance(transform.position, enemy.transform.position);
//////            if (dist <= detectionRadius && dist < bestDist)
//////            {
//////                bestDist = dist;
//////                closest = enemy;
//////            }
//////        }

//////        return closest;
//////    }

//////    // ── Animation helpers ─────────────────────────────────────────

//////    private void PlayIdle()
//////    {
//////        // Stop shoot animation before switching to idle.
//////        shootAnimator?.Stop();

//////        ShowIdleImage();

//////        if (idleAnimator != null)
//////        {
//////            idleAnimator.loop = true;
//////            idleAnimator.Play();
//////        }
//////    }

//////    private void ShowIdleImage()
//////    {
//////        _idleImageGO?.SetActive(true);
//////        _shootImageGO?.SetActive(false);
//////    }

//////    private void ShowShootImage()
//////    {
//////        _idleImageGO?.SetActive(false);
//////        _shootImageGO?.SetActive(true);
//////    }

//////    // ── Auto-wire ─────────────────────────────────────────────────

//////    private void AutoFindReferences()
//////    {
//////        // Wire child GameObjects by name
//////        Transform idleT = transform.Find("IdleImage");
//////        Transform shootT = transform.Find("ShootImage");
//////        Transform spawnT = transform.Find("Spawnpoint");

//////        if (idleT != null) _idleImageGO = idleT.gameObject;
//////        if (shootT != null) _shootImageGO = shootT.gameObject;
//////        if (spawnT != null && arrowSpawner == null)
//////            arrowSpawner = spawnT.GetComponent<RectTransform>();

//////        // Wire animators by component if not set in Inspector
//////        if (idleAnimator == null && _idleImageGO != null)
//////            idleAnimator = _idleImageGO.GetComponent<SpriteAnimator>();

//////        if (shootAnimator == null && _shootImageGO != null)
//////            shootAnimator = _shootImageGO.GetComponent<SpriteAnimator>();
//////    }

//////    private void WireAnimators()
//////    {
//////        // Ensure shoot animator doesn't loop (it plays once per shot)
//////        if (shootAnimator != null)
//////            shootAnimator.loop = false;
//////    }

//////    // ── Gizmos ───────────────────────────────────────────────────

//////    private void OnDrawGizmosSelected()
//////    {
//////        if (!showGizmo) return;
//////        Gizmos.color = Color.yellow;
//////        Gizmos.DrawWireSphere(transform.position, detectionRadius);
//////    }
//////}

////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// ArcherUnit
///////
/////// Behaviour:
///////   - On spawn: IdleImage shown, ShootImage hidden, idle animation loops.
///////   - Enemy detected: hide IdleImage, show ShootImage, play shoot once,
///////     fire arrow on spawnOnFrame, then return to idle.
///////
/////// Child hierarchy (auto-wired by name):
///////   ArcherUnit
///////   |-- IdleImage    Image + SpriteAnimator  (loop = true)
///////   |-- ShootImage   Image + SpriteAnimator  (loop = false)
///////   +-- Spawnpoint   RectTransform, arrow origin
/////// </summary>
////public class ArcherUnit : MonoBehaviour
////{
////    [Header("Animators (auto-found if left empty)")]
////    public SpriteAnimator idleAnimator;
////    public SpriteAnimator shootAnimator;

////    [Header("Arrow")]
////    public RectTransform arrowSpawner;
////    public GameObject arrowPrefab;

////    [Tooltip("Pixel size (width x height) of the arrow while in flight.")]
////    public Vector2 arrowSize = new Vector2(8f, 32f);

////    [Header("Combat")]
////    public float detectionRadius = 250f;
////    public float fireInterval = 1.8f;
////    public float damage = 40f;

////    [Header("Projectile Arc")]
////    public float arcHeight = 10f;
////    public float flightDuration = 0.6f;

////    [Header("Debug")]
////    public bool showGizmo = true;

////    // ── Private state ─────────────────────────────────────────────

////    private ArcherSlot _ownerSlot;
////    private EnemyUnit _lockedTarget;
////    private bool _isShooting = false;
////    private float _cooldown = 0f;
////    private Canvas _rootCanvas;

////    private GameObject _idleImageGO;
////    private GameObject _shootImageGO;

////    // ── Init ──────────────────────────────────────────────────────

////    public void Init(ArcherSlot ownerSlot)
////    {
////        _ownerSlot = ownerSlot;
////    }

////    // ── Lifecycle ─────────────────────────────────────────────────

////    private void Awake()
////    {
////        // Find children by name
////        Transform idleT = transform.Find("IdleImage");
////        Transform shootT = transform.Find("ShootImage");
////        Transform spawnT = transform.Find("Spawnpoint");

////        if (idleT != null) _idleImageGO = idleT.gameObject;
////        if (shootT != null) _shootImageGO = shootT.gameObject;
////        if (spawnT != null && arrowSpawner == null)
////            arrowSpawner = spawnT.GetComponent<RectTransform>();

////        if (idleAnimator == null && _idleImageGO != null)
////            idleAnimator = _idleImageGO.GetComponent<SpriteAnimator>();
////        if (shootAnimator == null && _shootImageGO != null)
////            shootAnimator = _shootImageGO.GetComponent<SpriteAnimator>();

////        // Shoot must never loop
////        if (shootAnimator != null) shootAnimator.loop = false;

////        // --- Set initial visibility BEFORE any Play() calls ---
////        // ShootImage off first, then IdleImage on.
////        // SetActive(false) kills coroutines on that GameObject, so always
////        // deactivate the one you don't want BEFORE activating the one you do.
////        if (_shootImageGO != null) _shootImageGO.SetActive(false);
////        if (_idleImageGO != null) _idleImageGO.SetActive(true);
////    }

////    private void Start()
////    {
////        _rootCanvas = GetComponentInParent<Canvas>();
////        PlayIdle();
////    }

////    // ── Update ────────────────────────────────────────────────────

////    private void Update()
////    {
////        if (_isShooting) return;

////        if (_cooldown > 0f)
////        {
////            _cooldown -= Time.deltaTime;
////            return;
////        }

////        EnemyUnit target = FindClosestEnemy();

////        if (target == null)
////        {
////            if (_lockedTarget != null)
////            {
////                _lockedTarget = null;
////                PlayIdle();
////            }
////            return;
////        }

////        _lockedTarget = target;
////        Shoot();
////    }

////    // ── Combat ────────────────────────────────────────────────────

////    private void Shoot()
////    {
////        if (_isShooting || _lockedTarget == null) return;
////        _isShooting = true;

////        // Step 1: deactivate IdleImage — this kills the idle coroutine cleanly
////        if (_idleImageGO != null) _idleImageGO.SetActive(false);

////        // Step 2: activate ShootImage — coroutine can now run on an active object
////        if (_shootImageGO != null) _shootImageGO.SetActive(true);

////        // Step 3: play shoot animation (non-looping)
////        if (shootAnimator != null)
////        {
////            shootAnimator.onSpawnFrame = SpawnArrow;
////            shootAnimator.onComplete = OnShootAnimComplete;
////            shootAnimator.Play();
////        }
////        else
////        {
////            SpawnArrow();
////            OnShootAnimComplete();
////        }
////    }

////    private void SpawnArrow()
////    {
////        if (_lockedTarget == null || _lockedTarget.IsDead) return;

////        if (arrowPrefab == null)
////        {
////            Debug.LogWarning("[ArcherUnit] arrowPrefab not assigned.");
////            return;
////        }

////        Transform projectileParent = _rootCanvas != null ? _rootCanvas.transform : transform.root;

////        Vector3 spawnPos = arrowSpawner != null ? arrowSpawner.position : transform.position;
////        Vector3 targetPos = _lockedTarget.transform.position;

////        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, projectileParent);

////        RectTransform rt = arrowGO.GetComponent<RectTransform>();
////        if (rt != null) rt.sizeDelta = arrowSize;

////        ArrowProjectile arrow = arrowGO.GetComponent<ArrowProjectile>();
////        if (arrow != null)
////            arrow.Launch(spawnPos, targetPos, arcHeight, flightDuration, _lockedTarget, damage);
////        else
////            Debug.LogWarning("[ArcherUnit] arrowPrefab missing ArrowProjectile component.");
////    }

////    private void OnShootAnimComplete()
////    {
////        _isShooting = false;
////        _cooldown = fireInterval;
////        _lockedTarget = null;
////        PlayIdle();
////    }

////    // ── Idle ──────────────────────────────────────────────────────

////    private void PlayIdle()
////    {
////        // Step 1: deactivate ShootImage — kills its coroutine cleanly
////        if (_shootImageGO != null) _shootImageGO.SetActive(false);

////        // Step 2: activate IdleImage — coroutine can now run on an active object
////        if (_idleImageGO != null) _idleImageGO.SetActive(true);

////        // Step 3: play looping idle animation
////        if (idleAnimator != null)
////        {
////            idleAnimator.loop = true;
////            idleAnimator.Play();
////        }
////    }

////    // ── Enemy detection ───────────────────────────────────────────

////    private EnemyUnit FindClosestEnemy()
////    {
////        EnemyUnit closest = null;
////        float bestDist = float.MaxValue;

////        foreach (EnemyUnit enemy in EnemyUnit.All)
////        {
////            if (enemy == null || enemy.IsDead) continue;
////            float dist = Vector3.Distance(transform.position, enemy.transform.position);
////            if (dist <= detectionRadius && dist < bestDist)
////            {
////                bestDist = dist;
////                closest = enemy;
////            }
////        }
////        return closest;
////    }

////    // ── Gizmos ────────────────────────────────────────────────────

////    private void OnDrawGizmosSelected()
////    {
////        if (!showGizmo) return;
////        Gizmos.color = Color.yellow;
////        Gizmos.DrawWireSphere(transform.position, detectionRadius);
////    }
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// ArcherUnit — attach to the ArcherPrefab root.
/////
///// Child hierarchy (names must match exactly):
/////   ArcherUnit
/////   |-- IdleImage    Image + SpriteAnimator  (set loop=true in Inspector)
/////   |-- ShootImage   Image + SpriteAnimator  (set loop=false in Inspector)
/////   +-- Spawnpoint   empty RectTransform, arrow origin
/////
///// IMPORTANT — in your ArcherPrefab:
/////   Set ShootImage GameObject to INACTIVE before saving the prefab.
/////   This avoids any race between Awake() calls.
///// </summary>
//[DefaultExecutionOrder(10)]   // run AFTER SpriteAnimator (default order 0)
//public class ArcherUnit : MonoBehaviour
//{
//    [Header("Animators (auto-found by child name if left empty)")]
//    public SpriteAnimator idleAnimator;
//    public SpriteAnimator shootAnimator;

//    [Header("Arrow")]
//    public RectTransform arrowSpawner;
//    public GameObject arrowPrefab;

//    [Tooltip("Width x Height of the arrow in pixels. Sprite faces UP so height > width.")]
//    public Vector2 arrowSize = new Vector2(8f, 32f);

//    [Header("Combat")]
//    public float detectionRadius = 250f;
//    public float fireInterval = 1.8f;
//    public float damage = 40f;

//    [Header("Projectile Arc")]
//    public float arcHeight = 10f;
//    public float flightDuration = 0.6f;

//    [Header("Debug")]
//    public bool showGizmo = true;

//    // ── Private ───────────────────────────────────────────────────

//    private ArcherSlot _ownerSlot;
//    private EnemyUnit _lockedTarget;
//    private bool _isShooting = false;
//    private float _cooldown = 0f;
//    private Canvas _rootCanvas;

//    private GameObject _idleGO;
//    private GameObject _shootGO;

//    // ── Init ──────────────────────────────────────────────────────

//    public void Init(ArcherSlot ownerSlot) { _ownerSlot = ownerSlot; }

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Awake()
//    {
//        // --- Find children ---
//        Transform idleT = transform.Find("IdleImage");
//        Transform shootT = transform.Find("ShootImage");
//        Transform spawnT = transform.Find("Spawnpoint");

//        if (idleT != null) _idleGO = idleT.gameObject;
//        if (shootT != null) _shootGO = shootT.gameObject;
//        if (spawnT != null && arrowSpawner == null)
//            arrowSpawner = spawnT.GetComponent<RectTransform>();

//        if (idleAnimator == null && _idleGO != null)
//            idleAnimator = _idleGO.GetComponent<SpriteAnimator>();
//        if (shootAnimator == null && _shootGO != null)
//            shootAnimator = _shootGO.GetComponent<SpriteAnimator>();

//        if (shootAnimator != null) shootAnimator.loop = false;
//        if (idleAnimator != null) idleAnimator.loop = true;

//        // Force correct initial visibility.
//        // ShootImage MUST be off. Do this here as a safety net even if the
//        // prefab already has it inactive.
//        if (_shootGO != null) _shootGO.SetActive(false);
//        if (_idleGO != null) _idleGO.SetActive(true);
//    }

//    private void Start()
//    {
//        _rootCanvas = GetComponentInParent<Canvas>();
//        EnterIdle();
//    }

//    // ── Update ────────────────────────────────────────────────────

//    private void Update()
//    {
//        if (_isShooting) return;

//        if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }

//        EnemyUnit target = FindClosestEnemy();

//        if (target == null)
//        {
//            if (_lockedTarget != null) { _lockedTarget = null; EnterIdle(); }
//            return;
//        }

//        _lockedTarget = target;
//        EnterShoot();
//    }

//    // ── State: Idle ───────────────────────────────────────────────

//    private void EnterIdle()
//    {
//        // 1. Hide shoot (SetActive false kills its coroutine automatically)
//        if (_shootGO != null) _shootGO.SetActive(false);

//        // 2. Show idle
//        if (_idleGO != null) _idleGO.SetActive(true);

//        // 3. Play — object is active now so coroutine can start
//        if (idleAnimator != null)
//        {
//            idleAnimator.loop = true;
//            idleAnimator.Play();
//        }
//    }

//    // ── State: Shoot ──────────────────────────────────────────────

//    private void EnterShoot()
//    {
//        if (_isShooting || _lockedTarget == null) return;
//        _isShooting = true;

//        // 1. Hide idle (kills its coroutine)
//        if (_idleGO != null) _idleGO.SetActive(false);

//        // 2. Show shoot
//        if (_shootGO != null) _shootGO.SetActive(true);

//        // 3. Play shoot animation (non-looping, fires arrow on spawnOnFrame)
//        if (shootAnimator != null)
//        {
//            shootAnimator.onSpawnFrame = SpawnArrow;
//            shootAnimator.onComplete = OnShootComplete;
//            shootAnimator.Play();
//        }
//        else
//        {
//            SpawnArrow();
//            OnShootComplete();
//        }
//    }

//    private void OnShootComplete()
//    {
//        _isShooting = false;
//        _cooldown = fireInterval;
//        _lockedTarget = null;
//        EnterIdle();
//    }

//    // ── Arrow ─────────────────────────────────────────────────────

//    private void SpawnArrow()
//    {
//        if (_lockedTarget == null || _lockedTarget.IsDead) return;
//        if (arrowPrefab == null) { Debug.LogWarning("[ArcherUnit] arrowPrefab not set."); return; }

//        Transform parent = _rootCanvas != null ? _rootCanvas.transform : transform.root;
//        Vector3 spawnPos = arrowSpawner != null ? arrowSpawner.position : transform.position;
//        Vector3 targetPos = _lockedTarget.transform.position;

//        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, parent);

//        // Override size — anchors must be point (0.5,0.5) or sizeDelta is ignored
//        RectTransform rt = arrowGO.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchorMin = new Vector2(0.5f, 0.5f);
//            rt.anchorMax = new Vector2(0.5f, 0.5f);
//            rt.pivot = new Vector2(0.5f, 0.5f);
//            rt.sizeDelta = arrowSize;
//            rt.localScale = Vector3.one;
//        }

//        ArrowProjectile arrow = arrowGO.GetComponent<ArrowProjectile>();
//        if (arrow != null)
//            arrow.Launch(spawnPos, targetPos, arcHeight, flightDuration, _lockedTarget, damage);
//        else
//            Debug.LogWarning("[ArcherUnit] arrowPrefab missing ArrowProjectile component.");
//    }

//    // ── Enemy scan ────────────────────────────────────────────────

//    private EnemyUnit FindClosestEnemy()
//    {
//        EnemyUnit closest = null;
//        float bestDist = float.MaxValue;

//        foreach (EnemyUnit e in EnemyUnit.All)
//        {
//            if (e == null || e.IsDead) continue;
//            float d = Vector3.Distance(transform.position, e.transform.position);
//            if (d <= detectionRadius && d < bestDist) { bestDist = d; closest = e; }
//        }
//        return closest;
//    }

//    // ── Gizmos ────────────────────────────────────────────────────

//    private void OnDrawGizmosSelected()
//    {
//        if (!showGizmo) return;
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, detectionRadius);
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArcherUnit — attach to the ArcherPrefab root.
///
/// Child hierarchy (names must match exactly):
///   ArcherUnit
///   |-- IdleImages   Image + SpriteAnimator  (set loop=true in Inspector)
///   |-- ShootImages  Image + SpriteAnimator  (set loop=false in Inspector)
///   +-- Spawnpoint   empty RectTransform, arrow origin
///
/// IMPORTANT — in your ArcherPrefab:
///   Set ShootImages GameObject to INACTIVE before saving the prefab.
///   This avoids any race between Awake() calls.
/// </summary>
[DefaultExecutionOrder(10)]   // run AFTER SpriteAnimator (default order 0)
public class ArcherUnit : MonoBehaviour
{
    [Header("Animators (auto-found by child name if left empty)")]
    public SpriteAnimator idleAnimator;
    public SpriteAnimator shootAnimator;

    [Header("Arrow")]
    public RectTransform arrowSpawner;
    public GameObject arrowPrefab;

    [Tooltip("Width x Height of the arrow in pixels. Sprite faces UP so height > width.")]
    public Vector2 arrowSize = new Vector2(8f, 32f);

    [Header("Combat")]
    public float detectionRadius = 250f;
    public float fireInterval = 1.8f;
    public float damage = 40f;

    [Header("Projectile Arc")]
    public float arcHeight = 10f;
    public float flightDuration = 0.6f;

    [Header("Debug")]
    public bool showGizmo = true;

    // ── Private ───────────────────────────────────────────────────

    private ArcherSlot _ownerSlot;
    private EnemyUnit _lockedTarget;
    private bool _isShooting = false;
    private float _cooldown = 0f;
    private Canvas _rootCanvas;

    private GameObject _idleGO;
    private GameObject _shootGO;

    // ── Init ──────────────────────────────────────────────────────

    public void Init(ArcherSlot ownerSlot) { _ownerSlot = ownerSlot; }

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        // --- Find children ---
        Transform idleT = transform.Find("IdleImages");
        Transform shootT = transform.Find("ShootImages");
        Transform spawnT = transform.Find("Spawnpoint");

        if (idleT != null) _idleGO = idleT.gameObject;
        if (shootT != null) _shootGO = shootT.gameObject;
        if (spawnT != null && arrowSpawner == null)
            arrowSpawner = spawnT.GetComponent<RectTransform>();

        if (idleAnimator == null && _idleGO != null)
            idleAnimator = _idleGO.GetComponent<SpriteAnimator>();
        if (shootAnimator == null && _shootGO != null)
            shootAnimator = _shootGO.GetComponent<SpriteAnimator>();

        if (shootAnimator != null) shootAnimator.loop = false;
        if (idleAnimator != null) idleAnimator.loop = true;

        // Force correct initial visibility.
        // ShootImage MUST be off. Do this here as a safety net even if the
        // prefab already has it inactive.
        if (_shootGO != null) _shootGO.SetActive(false);
        if (_idleGO != null) _idleGO.SetActive(true);
    }

    private void Start()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        EnterIdle();
    }

    // ── Update ────────────────────────────────────────────────────

    private void Update()
    {
        if (_isShooting) return;

        if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }

        EnemyUnit target = FindClosestEnemy();

        if (target == null)
        {
            if (_lockedTarget != null) { _lockedTarget = null; EnterIdle(); }
            return;
        }

        _lockedTarget = target;
        EnterShoot();
    }

    // ── State: Idle ───────────────────────────────────────────────

    private void EnterIdle()
    {
        // 1. Hide shoot (SetActive false kills its coroutine automatically)
        if (_shootGO != null) _shootGO.SetActive(false);

        // 2. Show idle
        if (_idleGO != null) _idleGO.SetActive(true);

        // 3. Play — object is active now so coroutine can start
        if (idleAnimator != null)
        {
            idleAnimator.loop = true;
            idleAnimator.Play();
        }
    }

    // ── State: Shoot ──────────────────────────────────────────────

    private void EnterShoot()
    {
        if (_isShooting || _lockedTarget == null) return;
        _isShooting = true;

        // 1. Hide idle (kills its coroutine)
        if (_idleGO != null) _idleGO.SetActive(false);

        // 2. Show shoot
        if (_shootGO != null) _shootGO.SetActive(true);

        // 3. Play shoot animation (non-looping, fires arrow on spawnOnFrame)
        if (shootAnimator != null)
        {
            shootAnimator.onSpawnFrame = SpawnArrow;
            shootAnimator.onComplete = OnShootComplete;
            shootAnimator.Play();
        }
        else
        {
            SpawnArrow();
            OnShootComplete();
        }
    }

    private void OnShootComplete()
    {
        _isShooting = false;
        _cooldown = fireInterval;
        _lockedTarget = null;
        EnterIdle();
    }

    // ── Arrow ─────────────────────────────────────────────────────

    private void SpawnArrow()
    {
        if (_lockedTarget == null || _lockedTarget.IsDead) return;
        if (arrowPrefab == null) { Debug.LogWarning("[ArcherUnit] arrowPrefab not set."); return; }

        Transform parent = _rootCanvas != null ? _rootCanvas.transform : transform.root;
        Vector3 spawnPos = arrowSpawner != null ? arrowSpawner.position : transform.position;
        Vector3 targetPos = _lockedTarget.transform.position;

        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, parent);

        // Override size — anchors must be point (0.5,0.5) or sizeDelta is ignored
        RectTransform rt = arrowGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = arrowSize;
            rt.localScale = Vector3.one;
        }

        ArrowProjectile arrow = arrowGO.GetComponent<ArrowProjectile>();
        if (arrow != null)
            arrow.Launch(spawnPos, targetPos, arcHeight, flightDuration, _lockedTarget, damage);
        else
            Debug.LogWarning("[ArcherUnit] arrowPrefab missing ArrowProjectile component.");
    }

    // ── Enemy scan ────────────────────────────────────────────────

    private EnemyUnit FindClosestEnemy()
    {
        EnemyUnit closest = null;
        float bestDist = float.MaxValue;

        foreach (EnemyUnit e in EnemyUnit.All)
        {
            if (e == null || e.IsDead) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d <= detectionRadius && d < bestDist) { bestDist = d; closest = e; }
        }
        return closest;
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}