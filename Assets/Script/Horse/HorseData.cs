//////using UnityEngine;

///////// <summary>
///////// AREA FORGE - HorseData (ScriptableObject)
/////////
///////// One asset per horse level — e.g. Level1Horse, Level2Horse ...
///////// Create via: Right-click Project → Create → AreaForge → Horse Data
///////// </summary>
//////[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
//////public class HorseData : ScriptableObject
//////{
//////    [Header("Identity")]
//////    public string horseName = "Brown Horse";
//////    public int level = 1;
//////    public int cost = 100;

//////    [Header("Idle Animation Sprites")]
//////    [Tooltip("Drag idle animation frames in order")]
//////    public Sprite[] idleSprites;
//////    public float idleFPS = 6f;

//////    [Header("Stats shown in the Horse Panel HUD")]
//////    public float health = 80f;
//////    public float ability = 50f;
//////    public float damage = 20f;
//////}

////using UnityEngine;

////[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
////public class HorseData : ScriptableObject
////{
////    [Header("Identity")]
////    public string horseName = "Brown Horse";
////    public int level = 1;
////    public int cost = 100;

////    [Header("Prefab — drag the matching horse prefab here")]
////    [Tooltip("Each HorseData owns its prefab so HorseArea doesn't need one per level")]
////    public GameObject prefab;

////    [Header("Idle Animation")]
////    public Sprite[] idleSprites;
////    public float idleFPS = 6f;

////    [Header("Stats")]
////    public float health = 80f;
////    public float ability = 50f;
////    public float damage = 20f;
////}


////using UnityEngine;

/////// <summary>
/////// AREA FORGE - HorseData (ScriptableObject)
///////
/////// One asset per horse level — e.g. Level1Horse, Level2Horse ...
/////// Create via: Right-click Project → Create → AreaForge → Horse Data
/////// </summary>
////[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
////public class HorseData : ScriptableObject
////{
////    [Header("Identity")]
////    public string horseName = "Brown Horse";
////    public int level = 1;
////    public int cost = 100;

////    [Header("Idle Animation Sprites")]
////    [Tooltip("Drag idle animation frames in order")]
////    public Sprite[] idleSprites;
////    public float idleFPS = 6f;

////    [Header("Stats shown in the Horse Panel HUD")]
////    public float health = 80f;
////    public float ability = 50f;
////    public float damage = 20f;
////}

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

//    [Header("Stats")]
//    public float health = 80f;
//    public float ability = 50f;
//    public float damage = 20f;
//}


using UnityEngine;

[CreateAssetMenu(menuName = "AreaForge/Horse Data", fileName = "NewHorse")]
public class HorseData : ScriptableObject
{
    [Header("Identity")]
    public string horseName = "Brown Horse";
    public int level = 1;
    public int cost = 100;
    [Tooltip("Horse age shown in the preview panel")]
    public int age = 3;

    [Header("Prefab — drag the matching horse prefab here")]
    [Tooltip("Each HorseData owns its prefab so HorseArea doesn't need one per level")]
    public GameObject prefab;

    [Header("Idle Animation")]
    public Sprite[] idleSprites;
    public float idleFPS = 6f;

    [Header("Base Stats")]
    public float health = 80f;
    public float ability = 50f;
    public float damage = 20f;

    [Header("Upgrade (3 upgrades max)")]
    [Tooltip("Stat gained per upgrade")]
    public float upgradeHealthGain = 7f;
    public float upgradeAbilityGain = 5f;
    public float upgradeDamageGain = 4f;

    [Tooltip("Real-time seconds the upgrade takes to complete")]
    public float upgradeDuration = 10f;
}