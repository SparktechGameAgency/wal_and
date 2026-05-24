using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArcherUnit
///
/// Attach to the Archer prefab root alongside Image + SpriteAnimator.
///
/// ── Behaviour ──────────────────────────────────────────────────────────────
///   • Stays idle (playing idle frames) while no enemy is in range.
///   • When an enemy enters detectionRadius, locks onto the closest one
///     and plays the shoot animation.
///   • On the spawnFrame of the shoot animation an ArrowProjectile is fired.
///   • After the shoot animation finishes the unit waits fireInterval seconds
///     before shooting again.
///   • If the locked target dies or leaves range the unit returns to idle.
///
/// ── Inspector wiring ───────────────────────────────────────────────────────
///   idleAnimator    → SpriteAnimator component driving the idle sprite sheet.
///   shootAnimator   → SpriteAnimator component driving the shoot sprite sheet.
///                     Set loop = false on the shoot animator.
///   arrowSpawner    → Empty RectTransform at the bow / hand position
///                     (child of this GameObject).
///   arrowPrefab     → Prefab with an ArrowProjectile component.
///   arrowSize       → Pixel size of the in-flight arrow (default 32 × 8).
///   detectionRadius → World-unit radius to scan for EnemyUnit instances.
///   fireInterval    → Minimum seconds between shots.
///   damage          → HP removed per arrow hit.
///
/// ── Child hierarchy (auto-wired by name) ───────────────────────────────────
///   ArcherUnit
///   ├── IdleImage     Image driven by idleAnimator
///   ├── ShootImage    Image driven by shootAnimator (hidden while idle)
///   └── Spawnpoint    RectTransform — arrow origin
///
/// ── Dependencies ───────────────────────────────────────────────────────────
///   SpriteAnimator   (Script/Cannon/Shootring/SpriteAnimator.cs)
///   EnemyUnit        (Script/Cannon/Shootring/EnemyUnit.cs)
///   ArrowProjectile  (Script/Castle/ArrowProjectile.cs)
/// </summary>
public class ArcherUnit : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────

    [Header("Animators (auto-found if left empty)")]
    [Tooltip("SpriteAnimator for idle frames — loops continuously.")]
    public SpriteAnimator idleAnimator;

    [Tooltip("SpriteAnimator for shoot frames — plays once per shot.")]
    public SpriteAnimator shootAnimator;

    [Header("Arrow")]
    [Tooltip("RectTransform at the bow tip. Auto-found by name 'Spawnpoint' if empty.")]
    public RectTransform arrowSpawner;

    [Tooltip("Prefab with an ArrowProjectile component.")]
    public GameObject arrowPrefab;

    [Tooltip("Pixel size (width x height) of the arrow while in flight.")]
    public Vector2 arrowSize = new Vector2(32f, 8f);

    [Header("Combat")]
    [Tooltip("World-unit detection radius.  Yellow gizmo shown in Scene view.")]
    public float detectionRadius = 250f;

    [Tooltip("Seconds between shots (after the shoot animation finishes).")]
    public float fireInterval = 1.8f;

    [Tooltip("Damage dealt to the enemy per arrow hit.")]
    public float damage = 40f;

    [Header("Projectile Arc")]
    [Tooltip("Peak height above straight line, in pixels. Keep low (0-30) for arrows.")]
    public float arcHeight = 10f;

    [Tooltip("Seconds for the arrow to reach the target.")]
    public float flightDuration = 0.6f;

    [Header("Debug")]
    public bool showGizmo = true;

    // ── Private ───────────────────────────────────────────────────

    private ArcherSlot _ownerSlot;
    private EnemyUnit _lockedTarget;
    private bool _isShooting = false;
    private float _cooldown = 0f;
    private Canvas _rootCanvas;

    // Child GameObjects toggled when switching idle ↔ shoot
    private GameObject _idleImageGO;
    private GameObject _shootImageGO;

    // ── Init (called by ArcherSlot) ───────────────────────────────

    /// <summary>Called by ArcherSlot.PlaceArcher() immediately after instantiation.</summary>
    public void Init(ArcherSlot ownerSlot)
    {
        _ownerSlot = ownerSlot;
    }

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        AutoFindReferences();
        WireAnimators();
    }

    private void Start()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        PlayIdle();
    }

    private void Update()
    {
        if (_isShooting) return;

        // Tick down cooldown
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
            return;
        }

        // Find nearest enemy in range
        EnemyUnit target = FindClosestEnemy();

        if (target == null)
        {
            // No enemy — ensure idle is playing
            if (_lockedTarget != null)
            {
                _lockedTarget = null;
                PlayIdle();
            }
            return;
        }

        // Enemy found — shoot
        _lockedTarget = target;
        Shoot();
    }

    // ── Combat ────────────────────────────────────────────────────

    private void Shoot()
    {
        if (_isShooting || _lockedTarget == null) return;
        _isShooting = true;

        ShowShootImage();

        if (shootAnimator != null)
        {
            // Arrow fires on shootAnimator.spawnOnFrame
            shootAnimator.onSpawnFrame = SpawnArrow;
            shootAnimator.onComplete = OnShootAnimComplete;
            shootAnimator.Play();
        }
        else
        {
            // No animator — fire immediately and reset
            SpawnArrow();
            OnShootAnimComplete();
        }
    }

    private void SpawnArrow()
    {
        if (_lockedTarget == null || _lockedTarget.IsDead)
        {
            // Target lost before projectile spawns
            return;
        }

        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArcherUnit] arrowPrefab not assigned — cannot fire.");
            return;
        }

        // Choose a parent canvas for the projectile
        Transform projectileParent = _rootCanvas != null
            ? _rootCanvas.transform
            : transform.root;

        Vector3 spawnPos = arrowSpawner != null
            ? arrowSpawner.position
            : transform.position;

        Vector3 targetPos = _lockedTarget.transform.position;

        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, projectileParent);

        // Size the arrow
        RectTransform rt = arrowGO.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = arrowSize;

        // Launch
        ArrowProjectile arrow = arrowGO.GetComponent<ArrowProjectile>();
        if (arrow != null)
            arrow.Launch(spawnPos, targetPos, arcHeight, flightDuration, _lockedTarget, damage);
        else
            Debug.LogWarning("[ArcherUnit] arrowPrefab is missing an ArrowProjectile component.");
    }

    private void OnShootAnimComplete()
    {
        _isShooting = false;
        _cooldown = fireInterval;
        _lockedTarget = null;   // re-evaluate next frame

        PlayIdle();
    }

    // ── Enemy detection ───────────────────────────────────────────

    private EnemyUnit FindClosestEnemy()
    {
        EnemyUnit closest = null;
        float bestDist = float.MaxValue;

        foreach (EnemyUnit enemy in EnemyUnit.All)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= detectionRadius && dist < bestDist)
            {
                bestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    // ── Animation helpers ─────────────────────────────────────────

    private void PlayIdle()
    {
        ShowIdleImage();

        if (idleAnimator != null)
        {
            idleAnimator.loop = true;
            idleAnimator.Play();
        }
    }

    private void ShowIdleImage()
    {
        _idleImageGO?.SetActive(true);
        _shootImageGO?.SetActive(false);
    }

    private void ShowShootImage()
    {
        _idleImageGO?.SetActive(false);
        _shootImageGO?.SetActive(true);
    }

    // ── Auto-wire ─────────────────────────────────────────────────

    private void AutoFindReferences()
    {
        // Wire child GameObjects by name
        Transform idleT = transform.Find("IdleImage");
        Transform shootT = transform.Find("ShootImage");
        Transform spawnT = transform.Find("Spawnpoint");

        if (idleT != null) _idleImageGO = idleT.gameObject;
        if (shootT != null) _shootImageGO = shootT.gameObject;
        if (spawnT != null && arrowSpawner == null)
            arrowSpawner = spawnT.GetComponent<RectTransform>();

        // Wire animators by component if not set in Inspector
        if (idleAnimator == null && _idleImageGO != null)
            idleAnimator = _idleImageGO.GetComponent<SpriteAnimator>();

        if (shootAnimator == null && _shootImageGO != null)
            shootAnimator = _shootImageGO.GetComponent<SpriteAnimator>();
    }

    private void WireAnimators()
    {
        // Ensure shoot animator doesn't loop (it plays once per shot)
        if (shootAnimator != null)
            shootAnimator.loop = false;
    }

    // ── Gizmos ───────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}