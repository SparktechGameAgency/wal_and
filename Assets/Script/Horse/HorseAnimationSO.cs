using UnityEngine;
using System;
using System.Collections.Generic;

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