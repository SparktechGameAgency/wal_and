using UnityEngine;

/// <summary>
/// AREA FORGE - DamageZone
/// Place this on any GameObject with a Collider2D (set Is Trigger = true).
/// When a soldier walks into it, their health is reduced by a set amount.
///
/// Inspector options:
///   Damage Amount     — flat HP to remove per hit  (e.g. 10)
///   Damage Interval   — seconds between hits while inside (e.g. 1.0 = every 1 sec)
///   Damage On Enter   — deal one hit the moment the soldier steps in
///   Damage Over Time  — keep ticking damage while inside
///   Soldier Tag       — tag on your soldier prefab (default: "Soldier")
///
/// Setup in Unity:
///   1. Create an empty GameObject in the Village scene → name it "DamageZone"
///   2. Add a BoxCollider2D (or any Collider2D) → tick "Is Trigger"
///   3. Attach this script
///   4. Size and position the collider over the damage area in the Scene view
///   5. Make sure your soldier prefab Tag is set to "Soldier" (or change soldierTag)
///
/// MULTIPLAYER NOTE: Damage is applied via SoldierStats.TakeDamage().
/// In multiplayer, wrap TakeDamage() in a [Command] so only the server applies it.
/// </summary>
public class DamageZone : MonoBehaviour
{
    // ─── Inspector Settings ───────────────────────────────────────────────────

    [Header("Damage")]
    [Tooltip("How much HP to remove per hit")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Seconds between damage ticks while the soldier stays inside")]
    [SerializeField] private float damageInterval = 1f;

    [Tooltip("Deal one hit the instant the soldier enters the zone")]
    [SerializeField] private bool damageOnEnter = true;

    [Tooltip("Keep dealing damage every Damage Interval while inside")]
    [SerializeField] private bool damageOverTime = true;

    [Header("Targeting")]
    [Tooltip("Tag assigned to soldier prefabs — must match the prefab's Tag field")]
    [SerializeField] private string soldierTag = "Soldier";

    [Header("Visual Feedback (optional)")]
    [Tooltip("Renderer whose colour flashes red when a soldier is inside")]
    [SerializeField] private SpriteRenderer zoneRenderer;
    [SerializeField] private Color normalColor = new Color(1f, 0.3f, 0.3f, 0.25f);
    [SerializeField] private Color activeColor = new Color(1f, 0.1f, 0.1f, 0.55f);

    // ─── Private State ────────────────────────────────────────────────────────

    // Tracks every soldier currently inside + their damage timer
    private System.Collections.Generic.Dictionary<SoldierStats, float> _soldiersInside
        = new System.Collections.Generic.Dictionary<SoldierStats, float>();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Make sure this zone has a trigger collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[DamageZone] No Collider2D found. Add one and tick 'Is Trigger'.");
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning("[DamageZone] Collider2D is not a trigger. Setting isTrigger = true.");
            col.isTrigger = true;
        }

        if (zoneRenderer != null)
            zoneRenderer.color = normalColor;
    }

    private void Update()
    {
        if (!damageOverTime || _soldiersInside.Count == 0) return;

        // Tick damage for every soldier inside
        var keys = new System.Collections.Generic.List<SoldierStats>(_soldiersInside.Keys);
        foreach (SoldierStats stats in keys)
        {
            if (stats == null)
            {
                _soldiersInside.Remove(stats);
                continue;
            }

            _soldiersInside[stats] -= Time.deltaTime;

            if (_soldiersInside[stats] <= 0f)
            {
                ApplyDamage(stats);
                _soldiersInside[stats] = damageInterval; // reset timer
            }
        }
    }

    // ─── Trigger Callbacks ────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(soldierTag)) return;

        SoldierStats stats = other.GetComponent<SoldierStats>();
        if (stats == null) return;

        // Add to tracking dictionary
        if (!_soldiersInside.ContainsKey(stats))
            _soldiersInside.Add(stats, damageInterval);

        // Immediate hit on entry
        if (damageOnEnter)
            ApplyDamage(stats);

        UpdateVisual();
        Debug.Log($"[DamageZone] {other.name} entered. Dealing {damageAmount} damage.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(soldierTag)) return;

        SoldierStats stats = other.GetComponent<SoldierStats>();
        if (stats != null && _soldiersInside.ContainsKey(stats))
            _soldiersInside.Remove(stats);

        UpdateVisual();
        Debug.Log($"[DamageZone] {other.name} left the zone.");
    }

    // ─── Damage Application ───────────────────────────────────────────────────

    private void ApplyDamage(SoldierStats stats)
    {
        if (stats == null) return;

        // MULTIPLAYER NOTE: In multiplayer, replace this with a [Command] call
        // so only the server authorises the damage:
        // CmdApplyDamage(stats.gameObject, damageAmount);
        stats.TakeDamage(damageAmount);

        Debug.Log($"[DamageZone] Hit! Dealt {damageAmount} dmg → HP now {stats.CurrentHealth}");
    }

    // ─── Visual Feedback ──────────────────────────────────────────────────────

    private void UpdateVisual()
    {
        if (zoneRenderer == null) return;
        zoneRenderer.color = (_soldiersInside.Count > 0) ? activeColor : normalColor;
    }

    // ─── Gizmos (visible in Scene view even without a SpriteRenderer) ─────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(circle.offset, circle.radius);
        }

        // Label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.6f,
            $"DMG  -{damageAmount}\n{damageInterval}s tick"
        );
    }
#endif
}