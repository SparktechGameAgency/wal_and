using UnityEngine;

/// <summary>
/// WalkZoneOwner
///
/// Added automatically by HorseWalkZone to every horse it spawns.
/// Stores a back-reference to the zone so HorseSlot can call
/// NotifyHorseLeft() when the horse is successfully dragged away.
///
/// Never add or configure this manually.
/// </summary>
public class WalkZoneOwner : MonoBehaviour
{
    /// <summary>The HorseWalkZone that spawned this horse.</summary>
    public HorseWalkZone Zone { get; set; }
}