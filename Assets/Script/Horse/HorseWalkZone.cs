//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// HorseWalkZone
/////
///// Attach to the HorseWall (or whichever GameObject is the "walk zone").
///// The GameObject needs an Image component with Raycast Target = ON so
///// Unity's EventSystem can detect the drop.
/////
///// ── What happens on drop ──────────────────────────────────────────────────
/////   1. The dragged horse's prefab is spawned inside this zone.
/////   2. HorseController.SetupWalk() is called → walk animation plays.
/////   3. After walkCyclesBeforeIdle full walk cycles the controller
/////      automatically switches to the idle animation (looping).
/////
///// ── Setup in Inspector ───────────────────────────────────────────────────
/////   • SpawnPoint  — optional RectTransform child; horse is placed here.
/////                   If left empty the zone's own transform is used.
/////   • Only one horse is shown at a time; dropping a new one replaces the old.
///// </summary>
//public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    [Header("Spawn")]
//    [Tooltip("Where the horse is placed inside the walk zone. Leave empty to use this transform.")]
//    [SerializeField] private RectTransform spawnPoint;

//    [Header("Highlight (optional)")]
//    [Tooltip("Image on this GameObject — tinted green while a horse is dragged over it")]
//    [SerializeField] private Image zoneHighlight;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseController _currentHorse;

//    // ── IDropHandler ──────────────────────────────────────────────────────────

//    /// <summary>Called by Unity when a drag operation ends over this object.</summary>
//    public void OnDrop(PointerEventData eventData)
//    {
//        // Must have been dragged by our custom handler
//        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//        if (drag == null || drag.horseData == null) return;

//        SpawnWalkingHorse(drag.horseData);
//        ResetHighlight();
//    }

//    // ── Hover highlight ───────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        // Only highlight when something is being dragged
//        if (eventData.pointerDrag == null) return;
//        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
//        SetHighlight(true);
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        SetHighlight(false);
//    }

//    // ── Private helpers ───────────────────────────────────────────────────────

//    private void SpawnWalkingHorse(HorseData data)
//    {
//        if (data.prefab == null)
//        {
//            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
//            return;
//        }

//        // Destroy the horse that was here before
//        if (_currentHorse != null)
//        {
//            Destroy(_currentHorse.gameObject);
//            _currentHorse = null;
//        }

//        // Spawn inside the zone
//        Transform parent = (spawnPoint != null) ? spawnPoint : transform;
//        GameObject go    = Instantiate(data.prefab, parent);

//        // Centre the horse in the spawn point
//        RectTransform rt = go.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale       = Vector3.one;

//            // Match the prefab's designed size
//            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
//            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
//        }

//        _currentHorse = go.GetComponent<HorseController>();
//        if (_currentHorse != null)
//        {
//            // SetupWalk → walk anim plays, then auto-switches to idle
//            _currentHorse.SetupWalk(data);
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseWalkZone] Prefab for '{data.horseName}' has no HorseController!");
//        }

//        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking...");
//    }

//    private void SetHighlight(bool on)
//    {
//        if (zoneHighlight == null) return;
//        zoneHighlight.color = on
//            ? new Color(0.4f, 1f, 0.4f, 0.35f)   // green tint while hovering
//            : new Color(1f,   1f,   1f,   0f);    // invisible when not hovering
//    }

//    private void ResetHighlight() => SetHighlight(false);
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HorseWalkZone
///
/// Attach to the HorseWall (or whichever GameObject is the "walk zone").
/// The GameObject needs an Image component with Raycast Target = ON.
///
/// ── Drop: Slot horse → Walk Zone ─────────────────────────────────────────
///   1. The dragged horse's prefab is spawned inside this zone.
///   2. HorseController.SetupWalk() → walk animation plays, then
///      auto-switches to idle after walkCyclesBeforeIdle cycles.
///   3. A HorseDragHandler (destroyOnSuccessfulDrop = true) is added to
///      the spawned horse so it can be dragged back to a HorseSlot.
///
/// ── Drop: Walk-Zone horse → Walk Zone (re-drop) ──────────────────────────
///   Ignored — the horse stays in the zone and keeps animating.
/// </summary>
public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spawn")]
    [Tooltip("Where the horse is placed inside the walk zone. Leave empty to use this transform.")]
    [SerializeField] private RectTransform spawnPoint;

    [Header("Highlight (optional)")]
    [Tooltip("Image on this GameObject tinted green while a SLOT horse is dragged over it.")]
    [SerializeField] private Image zoneHighlight;

    // ── Private state ─────────────────────────────────────────────────────────

    private HorseController _currentHorse;

    // ── IDropHandler ──────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
        if (drag == null || drag.horseData == null) return;

        // Ignore walk-zone horses dropped back onto the zone
        if (drag.destroyOnSuccessfulDrop) return;

        SpawnWalkingHorse(drag.horseData);
        ResetHighlight();
    }

    // ── Hover highlight ───────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        HorseDragHandler drag = eventData.pointerDrag.GetComponent<HorseDragHandler>();
        // Only highlight for slot horses, not walk-zone horses being re-dropped
        if (drag == null || drag.destroyOnSuccessfulDrop) return;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by HorseSlot when the walk-zone horse is successfully dropped on a slot.
    /// Clears the internal reference so the zone is ready for the next horse.
    /// </summary>
    public void NotifyHorseLeft() => _currentHorse = null;

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SpawnWalkingHorse(HorseData data)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
            return;
        }

        // Destroy any horse already in the zone
        if (_currentHorse != null)
        {
            Destroy(_currentHorse.gameObject);
            _currentHorse = null;
        }

        // Spawn inside the zone
        Transform parent = spawnPoint != null ? spawnPoint : transform;
        GameObject go = Instantiate(data.prefab, parent);

        // Centre in spawn point, keep prefab size
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
        }

        // Start walk animation
        _currentHorse = go.GetComponent<HorseController>();
        if (_currentHorse != null)
            _currentHorse.SetupWalk(data);
        else
            Debug.LogWarning($"[HorseWalkZone] Prefab for '{data.horseName}' has no HorseController!");

        // Make the walk-zone horse draggable back to a slot
        HorseDragHandler drag = go.GetComponent<HorseDragHandler>()
                             ?? go.AddComponent<HorseDragHandler>();
        drag.horseData = data;
        drag.destroyOnSuccessfulDrop = true; // destroys self when a slot accepts it

        // Tag with a zone reference so HorseSlot can notify us on departure
        WalkZoneOwner owner = go.AddComponent<WalkZoneOwner>();
        owner.Zone = this;

        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking.");
    }

    private void SetHighlight(bool on)
    {
        if (zoneHighlight == null) return;
        zoneHighlight.color = on
            ? new Color(0.4f, 1f, 0.4f, 0.35f)
            : new Color(1f, 1f, 1f, 0f);
    }

    private void ResetHighlight() => SetHighlight(false);
}