using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// AREA FORGE — HorseAnimationSO  (ScriptableObject)
///
/// HOW TO CREATE ONE IN UNITY:
///   Right-click in Project window →
///     Create → AreaForge → Horse Animation
///
/// Assign the created asset to the HorseController Inspector field on the Horse prefab.
///
/// ════════════════════════════════════════════════════════════════════
///  PURPOSE
/// ════════════════════════════════════════════════════════════════════
///
///  Stores all frame-by-frame sprite data for one horse variant.
///  HorseController reads this SO every Update tick to animate the
///  horse's Image layer(s).
///
///  Four states are supported:
///    Idle   → horse is standing still (no rider, or rider just mounted)
///    Run    → horse is galloping (HorseController.SetRun() called)
///    Fight  → horse rears / combat stance
///    Dead   → horse death animation (one-shot, no loop)
///
/// ════════════════════════════════════════════════════════════════════
///  HOW TO FILL IN THE INSPECTOR
/// ════════════════════════════════════════════════════════════════════
///
///  1. Create the SO asset (instructions above).
///  2. For each of the four HorseClip entries, set:
///       • state      → Idle / Run / Fight / Dead
///       • frames[]   → drag sprites IN ORDER from your sprite sheet
///       • fps        → how fast to play (try 8 for idle, 12 for run)
///       • loop       → true for Idle/Run/Fight;  false for Dead
///  3. Leave any state's frames[] empty to fall back to Idle.
///
/// ════════════════════════════════════════════════════════════════════
///  BODY LAYER SPLIT  (optional — for multi-layer horses)
/// ════════════════════════════════════════════════════════════════════
///
///  If your horse prefab has TWO Image layers (e.g. a base body + a
///  separate saddle/bridle layer), add a second HorseAnimationSO for
///  the saddle and assign it to HorseController.saddleSO.
///  Both SOs share the same HorseState so they always stay in sync.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Horse Animation", fileName = "HorseAnimation_Default")]
public class HorseAnimationSO : ScriptableObject
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Horse Name / Variant")]
    [Tooltip("Cosmetic label — used in Debug.Log messages only.")]
    public string horseName = "DefaultHorse";

    [Header("Animation Clips")]
    [Tooltip("Four clips — one per HorseState (Idle, Run, Fight, Dead).\n\n" +
             "Missing states fall back to the Idle clip automatically.")]
    public HorseClip[] clips;

    // ── Runtime cache ─────────────────────────────────────────────────────────

    private Dictionary<HorseState, HorseClip> _lookup;

    /// <summary>
    /// Returns the clip for <paramref name="state"/>.
    /// Falls back to Idle if the state has no clip, or to null if Idle is also missing.
    /// </summary>
    public HorseClip GetClip(HorseState state)
    {
        BuildLookup();

        if (_lookup.TryGetValue(state, out var clip) &&
            clip.frames != null && clip.frames.Length > 0)
            return clip;

        // Fallback → Idle
        if (state != HorseState.Idle &&
            _lookup.TryGetValue(HorseState.Idle, out var idle) &&
            idle.frames != null && idle.frames.Length > 0)
        {
            Debug.LogWarning($"[HorseAnimationSO] '{horseName}': no frames for {state}, " +
                             "falling back to Idle.", this);
            return idle;
        }

        Debug.LogWarning($"[HorseAnimationSO] '{horseName}': no clip for {state} " +
                         "and no Idle fallback. Horse will freeze.", this);
        return null;
    }

    private void BuildLookup()
    {
        if (_lookup != null) return;
        _lookup = new Dictionary<HorseState, HorseClip>();
        if (clips == null) return;
        foreach (var c in clips)
            _lookup[c.state] = c;
    }

    // Reset cache when the SO is reloaded in the Editor
    private void OnEnable() => _lookup = null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lookup = null;   // force rebuild after any Inspector edit

        if (clips == null || clips.Length == 0)
            Debug.LogWarning($"[HorseAnimationSO] '{name}' has no clips. " +
                             "Add at least an Idle clip.", this);
    }
#endif
}

// ─── Supporting types ─────────────────────────────────────────────────────────

/// <summary>States the horse (and its mounted soldier) can be in.</summary>
public enum HorseState
{
    Idle,
    Run,
    Fight,
    Dead
}

/// <summary>One animation clip — a state name, sprite frames, speed, and loop flag.</summary>
[Serializable]
public class HorseClip
{
    [Tooltip("Which state this clip plays for.")]
    public HorseState state;

    [Tooltip("Sprites in order, frame 0 first.")]
    public Sprite[] frames;

    [Tooltip("Frames per second. Try 8 (idle), 12 (run), 10 (fight), 7 (dead).")]
    [Range(1f, 30f)]
    public float fps = 8f;

    [Tooltip("True for Idle/Run/Fight. False for Dead (plays once then freezes on last frame).")]
    public bool loop = true;
}