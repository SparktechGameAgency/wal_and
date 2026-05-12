using UnityEngine;

/// <summary>
/// AREA FORGE — DragonLayeredVisual
///
/// Attach to the Dragon root GameObject (alongside DragonController).
///
/// Enforces the Canvas sibling order that makes the soldier appear
/// sandwiched between the dragon body and the front wing:
///
///   Dragon (root)
///   ├── DragonBody   [sibling 0]  ← Image: dragon body — renders BEHIND
///   ├── RiderSeat    [sibling 1]  ← DragonRiderSeat + soldier child — renders MIDDLE
///   └── DragonWing   [sibling 2]  ← Image: front wing — renders ON TOP
///
/// Because Unity Canvas renders children in sibling-index order (0 = behind),
/// this single script guarantees the wing covers part of the soldier regardless
/// of which armor the soldier is wearing — no per-armor special casing needed.
///
/// ── SETUP ────────────────────────────────────────────────────────────────────
///  1. Add this component to the Dragon root GameObject.
///  2. Drag the three child references into the Inspector fields below.
///  3. That's it. Sibling order is locked in Awake() every time the dragon spawns.
///
/// ── INSPECTOR FIELDS ─────────────────────────────────────────────────────────
///  dragonBody    Child that holds the dragon body Image layer.
///  riderSeat     Child that holds the DragonRiderSeat component.
///  dragonWing    Child that holds the front wing Image layer.
/// </summary>
public class DragonLayeredVisual : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Layer References (drag children here)")]
    [Tooltip("The child GameObject that holds the dragon body Image (renders behind rider).")]
    [SerializeField] private Transform dragonBody;

    [Tooltip("The child GameObject that holds DragonRiderSeat (soldier is reparented here).")]
    [SerializeField] private Transform riderSeat;

    [Tooltip("The child GameObject that holds the front wing Image (renders in front of rider).")]
    [SerializeField] private Transform dragonWing;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnforceSiblingOrder();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Re-applies the correct sibling order: Body(0) → RiderSeat(1) → Wing(2).
    ///
    /// Called automatically in Awake(). You can also call this manually if you
    /// rearrange the dragon hierarchy at runtime for any reason.
    /// </summary>
    public void EnforceSiblingOrder()
    {
        bool ok = true;

        if (dragonBody == null)
        {
            Debug.LogError("[DragonLayeredVisual] 'Dragon Body' reference is not set! " +
                           "Drag the body child into the Inspector.", this);
            ok = false;
        }

        if (riderSeat == null)
        {
            Debug.LogError("[DragonLayeredVisual] 'Rider Seat' reference is not set! " +
                           "Drag the RiderSeat child into the Inspector.", this);
            ok = false;
        }

        if (dragonWing == null)
        {
            Debug.LogError("[DragonLayeredVisual] 'Dragon Wing' reference is not set! " +
                           "Drag the wing child into the Inspector.", this);
            ok = false;
        }

        if (!ok) return;

        // SetSiblingIndex forces precise ordering even if the artist rearranges
        // things in the Hierarchy panel at edit time.
        dragonBody.SetSiblingIndex(0);   // renders first  = behind everything
        riderSeat.SetSiblingIndex(1);    // renders second = soldier visible above body
        dragonWing.SetSiblingIndex(2);   // renders last   = wing covers part of soldier

        Debug.Log($"[DragonLayeredVisual] Sibling order locked: " +
                  $"{dragonBody.name}(0) → {riderSeat.name}(1) → {dragonWing.name}(2).", this);
    }

    // ── Editor Gizmo ──────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw a faint label next to each layer reference so you can visually
        // confirm the order in the Scene view without entering Play mode.
        if (dragonBody != null)
            UnityEditor.Handles.Label(dragonBody.position + Vector3.down * 10f,
                "[0] Dragon Body");

        if (riderSeat != null)
            UnityEditor.Handles.Label(riderSeat.position,
                "[1] Rider Seat");

        if (dragonWing != null)
            UnityEditor.Handles.Label(dragonWing.position + Vector3.up * 10f,
                "[2] Dragon Wing (on top)");
    }
#endif
}