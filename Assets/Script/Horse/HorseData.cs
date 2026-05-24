//using UnityEngine;

//[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
//public class HorseData : ScriptableObject
//{
//    [Header("Identity")]
//    public string horseName = "Brown Horse";
//    public int level = 1;
//    public int cost = 100;
//    [Tooltip("Horse age shown in the preview panel")]
//    public int age = 3;

//    [Header("Prefab — drag the matching horse prefab here")]
//    [Tooltip("Each HorseData owns its prefab so HorseArea doesn't need one per level")]
//    public GameObject prefab;

//    [Header("Idle Animation")]
//    public Sprite[] idleSprites;
//    public float idleFPS = 6f;

//    [Header("Walk Animation")]
//    [Tooltip("Drag walk animation frames in order")]
//    public Sprite[] walkSprites;
//    public float walkFPS = 8f;
//    [Tooltip("How many full walk cycles to play before switching to idle. 0 = walk forever.")]
//    public int walkCyclesBeforeIdle = 2;

//    [Header("Base Stats")]
//    public float health = 80f;
//    public float ability = 50f;
//    public float damage = 20f;

//    [Header("Upgrade (3 upgrades max)")]
//    public float upgradeHealthGain = 7f;
//    public float upgradeAbilityGain = 5f;
//    public float upgradeDamageGain = 4f;

//    [Tooltip("Real-time seconds the upgrade takes to complete")]
//    public float upgradeDuration = 10f;
//}

using UnityEngine;

/// <summary>
/// AREA FORGE — HorseData  (ScriptableObject)
///
/// One asset per horse level — e.g. Level1Horse, Level2Horse …
/// Create via: Right-click Project → Create → AreaForge → Horse Data
///
/// ════════════════════════════════════════════════════════════════════
///  ANIMATION OVERVIEW
/// ════════════════════════════════════════════════════════════════════
///
///  Four animation clips live directly on this asset so each horse
///  level is fully self-contained without a separate HorseAnimationSO:
///
///    Idle   — loops continuously while the horse stands in a slot.
///    Run    — plays while the horse is in the walk/run zone.
///             runCyclesBeforeIdle > 0 → auto-returns to Idle after N loops.
///             runCyclesBeforeIdle = 0 → loops Run forever.
///    Attack — plays during a fight / combat trigger.
///             attackCyclesBeforeIdle > 0 → auto-returns to Idle after N loops.
///             attackCyclesBeforeIdle = 0 → loops Attack forever.
///    Dead   — plays once and freezes on the last frame.
///
///  HorseController Path B reads these arrays as a fallback when no
///  HorseAnimationSO is assigned on the prefab Inspector.
///  Path A (HorseAnimationSO) always wins if assigned.
///
/// ════════════════════════════════════════════════════════════════════
///  UPGRADE
/// ════════════════════════════════════════════════════════════════════
///
///  Up to 3 upgrades per horse. Each upgrade adds the *Gain values to
///  the base stats. upgradeDuration is the real-time wait in seconds.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
public class HorseData : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────

    [Header("Identity")]
    public string horseName = "Brown Horse";
    public int level = 1;
    public int cost = 100;

    [Tooltip("Horse age shown in the preview panel")]
    public int age = 3;

    [Header("Prefab")]
    [Tooltip("Each HorseData owns its prefab so HorseArea doesn't need one per level.")]
    public GameObject prefab;

    // ── Idle Animation ────────────────────────────────────────────────────────

    [Header("Idle Animation")]
    [Tooltip("Frames played while the horse stands in a slot. Loops forever.")]
    public Sprite[] idleSprites;

    [Tooltip("Frames per second for the idle clip.")]
    public float idleFPS = 6f;

    // ── Run Animation ─────────────────────────────────────────────────────────

    [Header("Run Animation")]
    [Tooltip("Frames played while the horse is in the walk / run zone.\n" +
             "Falls back to idleSprites if left empty.")]
    public Sprite[] runSprites;

    [Tooltip("Frames per second for the run clip.")]
    public float runFPS = 10f;

    [Tooltip("How many full run cycles to play before switching back to Idle.\n" +
             "0 = loop the run animation forever (zone controls the transition).")]
    public int runCyclesBeforeIdle = 0;

    // ── Attack Animation ──────────────────────────────────────────────────────

    [Header("Attack Animation")]
    [Tooltip("Frames played during a combat / fight trigger.\n" +
             "Falls back to idleSprites if left empty.")]
    public Sprite[] attackSprites;

    [Tooltip("Frames per second for the attack clip.")]
    public float attackFPS = 12f;

    [Tooltip("How many full attack cycles to play before switching back to Idle.\n" +
             "0 = loop the attack animation forever (external system controls the transition).")]
    public int attackCyclesBeforeIdle = 1;

    // ── Dead Animation ────────────────────────────────────────────────────────

    [Header("Dead Animation")]
    [Tooltip("Frames played when the horse dies. Plays once and freezes on the last frame.\n" +
             "Falls back to idleSprites if left empty.")]
    public Sprite[] deadSprites;

    [Tooltip("Frames per second for the death clip.")]
    public float deadFPS = 8f;

    // ── Base Stats ────────────────────────────────────────────────────────────

    [Header("Base Stats")]
    public float health = 80f;
    public float ability = 50f;
    public float damage = 20f;

    // ── Upgrade ───────────────────────────────────────────────────────────────

    [Header("Upgrade (3 upgrades max)")]
    [Tooltip("Stat gained per upgrade step.")]
    public float upgradeHealthGain = 7f;
    public float upgradeAbilityGain = 5f;
    public float upgradeDamageGain = 4f;

    [Tooltip("Real-time seconds each upgrade takes to complete.")]
    public float upgradeDuration = 10f;

    // ── Convenience helpers (read by HorseController Path B) ─────────────────

    /// <summary>
    /// Returns the sprite array for <paramref name="state"/>,
    /// falling back to <see cref="idleSprites"/> if the requested array is empty.
    /// </summary>
    public Sprite[] GetSprites(HorseState state)
    {
        switch (state)
        {
            case HorseState.Run:
                return (runSprites != null && runSprites.Length > 0)
                    ? runSprites : idleSprites;

            case HorseState.Fight:
                return (attackSprites != null && attackSprites.Length > 0)
                    ? attackSprites : idleSprites;

            case HorseState.Dead:
                return (deadSprites != null && deadSprites.Length > 0)
                    ? deadSprites : idleSprites;

            default: // Idle
                return idleSprites;
        }
    }

    /// <summary>
    /// Returns the playback FPS for <paramref name="state"/>.
    /// </summary>
    public float GetFPS(HorseState state)
    {
        switch (state)
        {
            case HorseState.Run:
                return (runSprites != null && runSprites.Length > 0) ? runFPS : idleFPS;

            case HorseState.Fight:
                return (attackSprites != null && attackSprites.Length > 0) ? attackFPS : idleFPS;

            case HorseState.Dead:
                return (deadSprites != null && deadSprites.Length > 0) ? deadFPS : idleFPS;

            default:
                return idleFPS;
        }
    }

    /// <summary>
    /// Returns how many full cycles to play before returning to Idle.
    /// 0 means loop forever. Dead always returns 0 (plays once — handled externally).
    /// </summary>
    public int GetCyclesBeforeIdle(HorseState state)
    {
        switch (state)
        {
            case HorseState.Run: return runCyclesBeforeIdle;
            case HorseState.Fight: return attackCyclesBeforeIdle;
            default: return 0;   // Idle and Dead don't use this
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (idleSprites == null || idleSprites.Length == 0)
            Debug.LogWarning($"[HorseData] '{name}': idleSprites is empty — " +
                             "the horse will appear blank in slots.", this);

        if (runSprites == null || runSprites.Length == 0)
            Debug.LogWarning($"[HorseData] '{name}': runSprites is empty — " +
                             "will fall back to idleSprites during Run.", this);

        if (attackSprites == null || attackSprites.Length == 0)
            Debug.LogWarning($"[HorseData] '{name}': attackSprites is empty — " +
                             "will fall back to idleSprites during Attack.", this);

        if (deadSprites == null || deadSprites.Length == 0)
            Debug.LogWarning($"[HorseData] '{name}': deadSprites is empty — " +
                             "will fall back to idleSprites when Dead.", this);

        if (prefab == null)
            Debug.LogWarning($"[HorseData] '{name}': no prefab assigned. " +
                             "HorseArea will not be able to spawn this horse.", this);
    }
#endif
}