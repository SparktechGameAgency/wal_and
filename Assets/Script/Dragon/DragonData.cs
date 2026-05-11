////using UnityEngine;

/////// <summary>
/////// DRAGON AREA — DragonData  (ScriptableObject)
///////
/////// Right-click Project window → Create → Area Forge → Dragon Data
/////// Assign the created asset to DragonEggSlot in the Inspector.
/////// </summary>
////[CreateAssetMenu(menuName = "Area Forge/Dragon Data", fileName = "DragonData")]
////public class DragonData : ScriptableObject
////{
////    [Header("Identity")]
////    public string dragonName = "Fire Dragon";

////    [Header("Timer — how long the egg sits before cracking")]
////    [Tooltip("Seconds the countdown runs before the crack animation plays.")]
////    [Min(1f)]
////    public float hatchDuration = 30f;

////    [Header("Egg Crack Animation")]
////    [Tooltip("Trigger parameter name on the Egg Animator that starts the crack clip.")]
////    public string eggCrackTrigger = "Crack";

////    [Tooltip("How long the crack animation clip lasts (seconds). " +
////             "The dragon appears after this delay once the timer hits zero.")]
////    [Min(0.1f)]
////    public float crackAnimationDuration = 1.5f;

////    [Header("Dragon Idle Animation")]
////    [Tooltip("Trigger parameter on the Dragon Animator for the idle state. " +
////             "Leave blank if the idle state plays automatically on entry.")]
////    public string dragonIdleTrigger = "Idle";
////}

//using UnityEngine;

///// <summary>
///// DRAGON AREA — DragonData  (ScriptableObject)
/////
///// Right-click Project window → Create → Area Forge → Dragon Data
///// Assign the created asset to DragonEggSlot in the Inspector.
///// </summary>
//[CreateAssetMenu(menuName = "AreaForge/Dragon Data", fileName = "DragonData")]
//public class DragonData : ScriptableObject
//{
//    [Header("Identity")]
//    public string dragonName = "Fire Dragon";

//    [Header("Timer — how long the egg sits before cracking")]
//    [Tooltip("Seconds the countdown runs before the crack animation plays.")]
//    [Min(1f)]
//    public float hatchDuration = 30f;

//    [Header("Egg Crack Animation")]
//    [Tooltip("Trigger parameter name on the Egg Animator that starts the crack clip.")]
//    public string eggCrackTrigger = "Crack";

//    [Tooltip("How long the crack animation clip lasts (seconds). " +
//             "The dragon appears after this delay once the timer hits zero.")]
//    [Min(0.1f)]
//    public float crackAnimationDuration = 1.5f;

//    [Header("Dragon Idle Animation")]
//    [Tooltip("Trigger parameter on the Dragon Animator for the idle state. " +
//             "Leave blank if the idle state plays automatically on entry.")]
//    public string dragonIdleTrigger = "Idle";
//}

using UnityEngine;

/// <summary>
/// DRAGON AREA — DragonData  (ScriptableObject)
///
/// Right-click Project window → Create → AreaForge → Dragon Data
/// Assign the created asset to DragonEggSlot in the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "AreaForge/Dragon Data", fileName = "DragonData")]
public class DragonData : ScriptableObject
{
    // ── Identity ───────────────────────────────────────────────────────────────
    [Header("Identity")]
    public string dragonName = "Fire Dragon";

    // ── Hatch timer ───────────────────────────────────────────────────────────
    [Header("Timer — how long the egg sits before cracking")]
    [Tooltip("Seconds the countdown runs before the crack animation plays.")]
    [Min(1f)]
    public float hatchDuration = 30f;

    // ── Egg crack animation ───────────────────────────────────────────────────
    [Header("Egg Crack Animation")]
    [Tooltip("Trigger parameter name on the Egg Animator that starts the crack clip.")]
    public string eggCrackTrigger = "Crack";

    [Tooltip("How long the crack animation clip lasts (seconds). " +
             "The dragon appears after this delay once the timer hits zero.")]
    [Min(0.1f)]
    public float crackAnimationDuration = 1.5f;

    // ── Dragon animations ─────────────────────────────────────────────────────
    [Header("Dragon Animations")]
    [Tooltip("Trigger parameter on the Dragon Animator for the idle state. " +
             "Leave blank if the idle state plays automatically on entry.")]
    public string dragonIdleTrigger = "Idle";

    [Tooltip("Trigger parameter on the Dragon Animator for the fly / patrol state. " +
             "Set this to whatever trigger name starts the fly animation clip.")]
    public string dragonFlyTrigger = "Fly";

    // ── Patrol ────────────────────────────────────────────────────────────────
    [Header("Patrol (used when the dragon is in a Fly Zone)")]
    [Tooltip("Movement speed in canvas units per second while patrolling.")]
    [Min(1f)]
    public float patrolSpeed = 80f;
}