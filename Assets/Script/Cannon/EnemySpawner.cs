using UnityEngine;

/// <summary>
/// Spawns enemy prefabs into the scene at a set interval.
///
/// ── Quick setup ────────────────────────────────────────────────────────────
/// 1. Create an empty GameObject in your Canvas (or scene) and name it "EnemySpawner".
/// 2. Attach this script to it.
/// 3. Assign your enemy prefab to <see cref="enemyPrefab"/>.
/// 4. Assign a <see cref="spawnPoint"/> — an empty Transform where enemies appear.
/// 5. Hit Play. Enemies will walk in automatically.
///
/// ── Canvas vs World ────────────────────────────────────────────────────────
/// If your enemies are UI objects (Image + RectTransform), place the spawner
/// inside your Canvas and tick <see cref="spawnInsideCanvas"/> ON.
/// If they are world-space GameObjects, untick it and set spawnPoint anywhere.
///
/// ── Inspector fields ───────────────────────────────────────────────────────
///   enemyPrefab        → the enemy prefab with EnemyUnit attached
///   spawnPoint         → where enemies appear (right edge of screen, etc.)
///   spawnInsideCanvas  → parent spawned enemies to the Canvas so they render in UI
///   spawnInterval      → seconds between each spawn (default 3)
///   maxEnemiesAtOnce   → cap on simultaneous live enemies (0 = unlimited)
///   spawnOnStart       → if true, spawns one enemy immediately at Start
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Your enemy prefab — must have EnemyUnit attached.")]
    public GameObject enemyPrefab;

    [Header("Spawn Point")]
    [Tooltip("Empty Transform that marks where enemies appear. " +
             "Place it off the right (or top) edge of the screen.")]
    public Transform spawnPoint;

    [Header("Canvas Parenting")]
    [Tooltip("TRUE  → enemies are parented to the root Canvas (needed for UI enemies).\n" +
             "FALSE → enemies are spawned as scene-root objects (for world-space enemies).")]
    public bool spawnInsideCanvas = true;

    [Header("Timing")]
    [Tooltip("Seconds between spawns.")]
    public float spawnInterval = 3f;

    [Tooltip("Maximum number of enemies alive at the same time. 0 = no cap.")]
    public int maxEnemiesAtOnce = 5;

    [Tooltip("Spawn one enemy immediately when the game starts.")]
    public bool spawnOnStart = true;

    // ── Private ───────────────────────────────────────────────────
    private float _timer = 0f;
    private Canvas _rootCanvas = null;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Start()
    {
        _rootCanvas = FindRootCanvas();

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab is not assigned! " +
                           "Drag your enemy prefab into the Inspector field.");
            enabled = false;
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[EnemySpawner] spawnPoint is not assigned. " +
                             "Using this GameObject's position as the spawn point.");
            spawnPoint = transform;
        }

        if (spawnOnStart)
            SpawnEnemy();

        // Start the timer offset so the first timed spawn comes after the
        // correct interval even when spawnOnStart is true.
        _timer = spawnInterval;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = spawnInterval;
        SpawnEnemy();
    }

    // ── Spawn ─────────────────────────────────────────────────────

    private void SpawnEnemy()
    {
        // Respect the live enemy cap
        if (maxEnemiesAtOnce > 0 && EnemyUnit.All.Count >= maxEnemiesAtOnce)
        {
            Debug.Log($"[EnemySpawner] Cap reached ({maxEnemiesAtOnce}). " +
                      "Skipping spawn until an enemy dies.");
            return;
        }

        // Choose parent
        Transform parent = null;
        if (spawnInsideCanvas && _rootCanvas != null)
            parent = _rootCanvas.transform;

        // Instantiate
        GameObject enemy = (parent != null)
            ? Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, parent)
            : Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        enemy.name = $"Enemy_{EnemyUnit.All.Count}";

        // Verify the prefab has EnemyUnit
        if (enemy.GetComponent<EnemyUnit>() == null)
        {
            Debug.LogError("[EnemySpawner] The spawned prefab has no EnemyUnit component! " +
                           "Add EnemyUnit to your enemy prefab.");
        }

        Debug.Log($"[EnemySpawner] Spawned '{enemy.name}' at {spawnPoint.position}. " +
                  $"Live enemies: {EnemyUnit.All.Count}");
    }

    // ── Helper ────────────────────────────────────────────────────

    private Canvas FindRootCanvas()
    {
        Canvas[] parents = GetComponentsInParent<Canvas>(includeInactive: false);
        if (parents != null && parents.Length > 0) return parents[parents.Length - 1];
        return Object.FindFirstObjectByType<Canvas>();
    }
}