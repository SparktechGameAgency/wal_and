//////using UnityEngine;

///////// <summary>
///////// DRAGON AREA — DragonData  (ScriptableObject)
/////////
///////// Right-click Project window → Create → Area Forge → Dragon Data
///////// Assign the created asset to DragonEggSlot in the Inspector.
///////// </summary>
//////[CreateAssetMenu(menuName = "Area Forge/Dragon Data", fileName = "DragonData")]
//////public class DragonData : ScriptableObject
//////{
//////    [Header("Identity")]
//////    public string dragonName = "Fire Dragon";

//////    [Header("Timer — how long the egg sits before cracking")]
//////    [Tooltip("Seconds the countdown runs before the crack animation plays.")]
//////    [Min(1f)]
//////    public float hatchDuration = 30f;

//////    [Header("Egg Crack Animation")]
//////    [Tooltip("Trigger parameter name on the Egg Animator that starts the crack clip.")]
//////    public string eggCrackTrigger = "Crack";

//////    [Tooltip("How long the crack animation clip lasts (seconds). " +
//////             "The dragon appears after this delay once the timer hits zero.")]
//////    [Min(0.1f)]
//////    public float crackAnimationDuration = 1.5f;

//////    [Header("Dragon Idle Animation")]
//////    [Tooltip("Trigger parameter on the Dragon Animator for the idle state. " +
//////             "Leave blank if the idle state plays automatically on entry.")]
//////    public string dragonIdleTrigger = "Idle";
//////}

////using UnityEngine;

/////// <summary>
/////// DRAGON AREA — DragonData  (ScriptableObject)
///////
/////// Right-click Project window → Create → Area Forge → Dragon Data
/////// Assign the created asset to DragonEggSlot in the Inspector.
/////// </summary>
////[CreateAssetMenu(menuName = "AreaForge/Dragon Data", fileName = "DragonData")]
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
///// Right-click Project window → Create → AreaForge → Dragon Data
///// Assign the created asset to DragonEggSlot in the Inspector.
///// </summary>
//[CreateAssetMenu(menuName = "AreaForge/Dragon Data", fileName = "DragonData")]
//public class DragonData : ScriptableObject
//{
//    // ── Identity ───────────────────────────────────────────────────────────────
//    [Header("Identity")]
//    public string dragonName = "Fire Dragon";

//    // ── Hatch timer ───────────────────────────────────────────────────────────
//    [Header("Timer — how long the egg sits before cracking")]
//    [Tooltip("Seconds the countdown runs before the crack animation plays.")]
//    [Min(1f)]
//    public float hatchDuration = 5f;

//    // ── Egg crack animation ───────────────────────────────────────────────────
//    [Header("Egg Crack Animation")]
//    [Tooltip("Trigger parameter name on the Egg Animator that starts the crack clip.")]
//    public string eggCrackTrigger = "Crack";

//    [Tooltip("How long the crack animation clip lasts (seconds). " +
//             "The dragon appears after this delay once the timer hits zero.")]
//    [Min(0.1f)]
//    public float crackAnimationDuration = 1.5f;

//    // ── Dragon animations ─────────────────────────────────────────────────────
//    [Header("Dragon Animations")]
//    [Tooltip("Trigger parameter on the Dragon Animator for the idle state. " +
//             "Leave blank if the idle state plays automatically on entry.")]
//    public string dragonIdleTrigger = "Idle";

//    [Tooltip("Trigger parameter on the Dragon Animator for the fly / patrol state. " +
//             "Set this to whatever trigger name starts the fly animation clip.")]
//    public string dragonFlyTrigger = "Fly";

//    // ── Patrol ────────────────────────────────────────────────────────────────
//    [Header("Patrol (used when the dragon is in a Fly Zone)")]
//    [Tooltip("Movement speed in canvas units per second while patrolling.")]
//    [Min(1f)]
//    public float patrolSpeed = 80f;
//}

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
    public float hatchDuration = 5f;

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

    // ── Combat ────────────────────────────────────────────────────────────────
    [Header("Combat — detection")]
    [Tooltip("How far (canvas units) the dragon can spot an EnemyUnit while flying.")]
    [Min(10f)]
    public float detectionRadius = 500f;

    [Tooltip("How close the dragon must get to the enemy before it stops and breathes fire.")]
    [Min(1f)]
    public float attackRange = 150f;

    [Header("Combat — chase")]
    [Tooltip("Movement speed in canvas units per second while chasing a target.")]
    [Min(1f)]
    public float chaseSpeed = 160f;

    [Header("Combat — fire breath")]
    [Tooltip("Damage dealt to the target on every damage tick while breathing fire.")]
    [Min(0f)]
    public float fireDamage = 15f;

    [Tooltip("Seconds between each damage tick while the fire breath is active.")]
    [Min(0.05f)]
    public float damageTickInterval = 0.5f;

    [Tooltip("Trigger parameter on the Dragon Animator for the fire-breath state. " +
             "Leave blank if no dedicated attack animation exists.")]
    public string dragonAttackTrigger = "Attack";
}