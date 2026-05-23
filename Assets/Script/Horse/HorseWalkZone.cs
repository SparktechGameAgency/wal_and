////////using UnityEngine;
////////using UnityEngine.EventSystems;
////////using UnityEngine.UI;

/////////// <summary>
/////////// HorseWalkZone
///////////
/////////// Attach to the HorseWall (or whichever GameObject is the "walk zone").
/////////// The GameObject needs an Image component with Raycast Target = ON so
/////////// Unity's EventSystem can detect the drop.
///////////
/////////// ── What happens on drop ──────────────────────────────────────────────────
///////////   1. The dragged horse's prefab is spawned inside this zone.
///////////   2. HorseController.SetupWalk() is called → walk animation plays.
///////////   3. After walkCyclesBeforeIdle full walk cycles the controller
///////////      automatically switches to the idle animation (looping).
///////////
/////////// ── Setup in Inspector ───────────────────────────────────────────────────
///////////   • SpawnPoint  — optional RectTransform child; horse is placed here.
///////////                   If left empty the zone's own transform is used.
///////////   • Only one horse is shown at a time; dropping a new one replaces the old.
/////////// </summary>
////////public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
////////{
////////    [Header("Spawn")]
////////    [Tooltip("Where the horse is placed inside the walk zone. Leave empty to use this transform.")]
////////    [SerializeField] private RectTransform spawnPoint;

////////    [Header("Highlight (optional)")]
////////    [Tooltip("Image on this GameObject — tinted green while a horse is dragged over it")]
////////    [SerializeField] private Image zoneHighlight;

////////    // ── Private state ─────────────────────────────────────────────────────────

////////    private HorseController _currentHorse;

////////    // ── IDropHandler ──────────────────────────────────────────────────────────

////////    /// <summary>Called by Unity when a drag operation ends over this object.</summary>
////////    public void OnDrop(PointerEventData eventData)
////////    {
////////        // Must have been dragged by our custom handler
////////        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
////////        if (drag == null || drag.horseData == null) return;

////////        SpawnWalkingHorse(drag.horseData);
////////        ResetHighlight();
////////    }

////////    // ── Hover highlight ───────────────────────────────────────────────────────

////////    public void OnPointerEnter(PointerEventData eventData)
////////    {
////////        // Only highlight when something is being dragged
////////        if (eventData.pointerDrag == null) return;
////////        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
////////        SetHighlight(true);
////////    }

////////    public void OnPointerExit(PointerEventData eventData)
////////    {
////////        SetHighlight(false);
////////    }

////////    // ── Private helpers ───────────────────────────────────────────────────────

////////    private void SpawnWalkingHorse(HorseData data)
////////    {
////////        if (data.prefab == null)
////////        {
////////            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
////////            return;
////////        }

////////        // Destroy the horse that was here before
////////        if (_currentHorse != null)
////////        {
////////            Destroy(_currentHorse.gameObject);
////////            _currentHorse = null;
////////        }

////////        // Spawn inside the zone
////////        Transform parent = (spawnPoint != null) ? spawnPoint : transform;
////////        GameObject go    = Instantiate(data.prefab, parent);

////////        // Centre the horse in the spawn point
////////        RectTransform rt = go.GetComponent<RectTransform>();
////////        if (rt != null)
////////        {
////////            rt.anchoredPosition = Vector2.zero;
////////            rt.localScale       = Vector3.one;

////////            // Match the prefab's designed size
////////            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
////////            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
////////        }

////////        _currentHorse = go.GetComponent<HorseController>();
////////        if (_currentHorse != null)
////////        {
////////            // SetupWalk → walk anim plays, then auto-switches to idle
////////            _currentHorse.SetupWalk(data);
////////        }
////////        else
////////        {
////////            Debug.LogWarning($"[HorseWalkZone] Prefab for '{data.horseName}' has no HorseController!");
////////        }

////////        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking...");
////////    }

////////    private void SetHighlight(bool on)
////////    {
////////        if (zoneHighlight == null) return;
////////        zoneHighlight.color = on
////////            ? new Color(0.4f, 1f, 0.4f, 0.35f)   // green tint while hovering
////////            : new Color(1f,   1f,   1f,   0f);    // invisible when not hovering
////////    }

////////    private void ResetHighlight() => SetHighlight(false);
////////}

//////using UnityEngine;
//////using UnityEngine.EventSystems;
//////using UnityEngine.UI;

///////// <summary>
///////// HorseWalkZone
/////////
///////// Attach to the HorseWall (or whichever GameObject is the "walk zone").
///////// The GameObject needs an Image component with Raycast Target = ON.
/////////
///////// ── Drop: Slot horse → Walk Zone ─────────────────────────────────────────
/////////   1. The dragged horse's prefab is spawned inside this zone.
/////////   2. HorseController.SetupWalk() → walk animation plays, then
/////////      auto-switches to idle after walkCyclesBeforeIdle cycles.
/////////   3. A HorseDragHandler (destroyOnSuccessfulDrop = true) is added to
/////////      the spawned horse so it can be dragged back to a HorseSlot.
/////////
///////// ── Drop: Walk-Zone horse → Walk Zone (re-drop) ──────────────────────────
/////////   Ignored — the horse stays in the zone and keeps animating.
///////// </summary>
//////public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//////{
//////    [Header("Spawn")]
//////    [Tooltip("Where the horse is placed inside the walk zone. Leave empty to use this transform.")]
//////    [SerializeField] private RectTransform spawnPoint;

//////    [Header("Highlight (optional)")]
//////    [Tooltip("Image on this GameObject tinted green while a SLOT horse is dragged over it.")]
//////    [SerializeField] private Image zoneHighlight;

//////    // ── Private state ─────────────────────────────────────────────────────────

//////    private HorseController _currentHorse;

//////    // ── IDropHandler ──────────────────────────────────────────────────────────

//////    public void OnDrop(PointerEventData eventData)
//////    {
//////        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//////        if (drag == null || drag.horseData == null) return;

//////        // Ignore walk-zone horses dropped back onto the zone
//////        if (drag.destroyOnSuccessfulDrop) return;

//////        SpawnWalkingHorse(drag.horseData);
//////        ResetHighlight();
//////    }

//////    // ── Hover highlight ───────────────────────────────────────────────────────

//////    public void OnPointerEnter(PointerEventData eventData)
//////    {
//////        if (eventData.pointerDrag == null) return;
//////        HorseDragHandler drag = eventData.pointerDrag.GetComponent<HorseDragHandler>();
//////        // Only highlight for slot horses, not walk-zone horses being re-dropped
//////        if (drag == null || drag.destroyOnSuccessfulDrop) return;
//////        SetHighlight(true);
//////    }

//////    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

//////    // ── Public API ────────────────────────────────────────────────────────────

//////    /// <summary>
//////    /// Called by HorseSlot when the walk-zone horse is successfully dropped on a slot.
//////    /// Clears the internal reference so the zone is ready for the next horse.
//////    /// </summary>
//////    public void NotifyHorseLeft() => _currentHorse = null;

//////    // ── Private helpers ───────────────────────────────────────────────────────

//////    private void SpawnWalkingHorse(HorseData data)
//////    {
//////        if (data.prefab == null)
//////        {
//////            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
//////            return;
//////        }

//////        // Destroy any horse already in the zone
//////        if (_currentHorse != null)
//////        {
//////            Destroy(_currentHorse.gameObject);
//////            _currentHorse = null;
//////        }

//////        // Spawn inside the zone
//////        Transform parent = spawnPoint != null ? spawnPoint : transform;
//////        GameObject go = Instantiate(data.prefab, parent);

//////        // Centre in spawn point, keep prefab size
//////        RectTransform rt = go.GetComponent<RectTransform>();
//////        if (rt != null)
//////        {
//////            rt.anchoredPosition = Vector2.zero;
//////            rt.localScale = Vector3.one;

//////            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
//////            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
//////        }

//////        // Start walk animation
//////        _currentHorse = go.GetComponent<HorseController>();
//////        if (_currentHorse != null)
//////            _currentHorse.SetupWalk(data);
//////        else
//////            Debug.LogWarning($"[HorseWalkZone] Prefab for '{data.horseName}' has no HorseController!");

//////        // Make the walk-zone horse draggable back to a slot
//////        HorseDragHandler drag = go.GetComponent<HorseDragHandler>()
//////                             ?? go.AddComponent<HorseDragHandler>();
//////        drag.horseData = data;
//////        drag.destroyOnSuccessfulDrop = true; // destroys self when a slot accepts it

//////        // Tag with a zone reference so HorseSlot can notify us on departure
//////        WalkZoneOwner owner = go.AddComponent<WalkZoneOwner>();
//////        owner.Zone = this;

//////        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking.");
//////    }

//////    private void SetHighlight(bool on)
//////    {
//////        if (zoneHighlight == null) return;
//////        zoneHighlight.color = on
//////            ? new Color(0.4f, 1f, 0.4f, 0.35f)
//////            : new Color(1f, 1f, 1f, 0f);
//////    }

//////    private void ResetHighlight() => SetHighlight(false);
//////}


////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;

/////// <summary>
/////// HorseWalkZone
///////
/////// Attach to HorseWall. Requires an Image with Raycast Target = ON.
///////
/////// ── Drop: Slot horse → Walk Zone ─────────────────────────────────────────
///////   1. The dragged horse's prefab is spawned inside this zone.
///////   2. HorseController.SetupWalk() → walk animation plays, then
///////      auto-switches to idle after walkCyclesBeforeIdle cycles.
///////   3. The spawned horse's HorseDragHandler (already on the prefab) is
///////      configured so it can be dragged back to a HorseSlot.
///////
/////// ── Drop: Walk-Zone horse → Walk Zone (re-drop) ──────────────────────────
///////   Ignored — the horse stays in the zone and keeps animating.
/////// </summary>
////public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
////{
////    [Header("Spawn")]
////    [Tooltip("Where the horse is placed inside the walk zone. Leave empty to use this transform.")]
////    [SerializeField] private RectTransform spawnPoint;

////    [Header("Highlight (optional)")]
////    [Tooltip("Image tinted green while a slot horse is dragged over this zone")]
////    [SerializeField] private Image zoneHighlight;

////    // ── Private state ─────────────────────────────────────────────────────────

////    private HorseController _currentHorse;

////    // ── IDropHandler ──────────────────────────────────────────────────────────

////    public void OnDrop(PointerEventData eventData)
////    {
////        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
////        if (drag == null || drag.horseData == null) return;

////        // Ignore walk-zone horses dropped back onto this zone
////        if (drag.destroyOnSuccessfulDrop) return;

////        SpawnWalkingHorse(drag.horseData);
////        ResetHighlight();
////    }

////    // ── Hover highlight ───────────────────────────────────────────────────────

////    public void OnPointerEnter(PointerEventData eventData)
////    {
////        if (eventData.pointerDrag == null) return;
////        HorseDragHandler drag = eventData.pointerDrag.GetComponent<HorseDragHandler>();
////        // Only highlight for slot/panel icons, not walk-zone horses being re-dropped
////        if (drag == null || drag.destroyOnSuccessfulDrop) return;
////        SetHighlight(true);
////    }

////    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

////    // ── Public API ────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Called by HorseSlot.OnDrop when the walk-zone horse is successfully
////    /// dragged to a slot. Clears the reference so the zone is ready for the next horse.
////    /// </summary>
////    public void NotifyHorseLeft() => _currentHorse = null;

////    // ── Private helpers ───────────────────────────────────────────────────────

////    private void SpawnWalkingHorse(HorseData data)
////    {
////        if (data.prefab == null)
////        {
////            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
////            return;
////        }

////        // Destroy any horse already in the zone
////        if (_currentHorse != null)
////        {
////            Destroy(_currentHorse.gameObject);
////            _currentHorse = null;
////        }

////        // Spawn inside the zone
////        Transform parent = spawnPoint != null ? spawnPoint : transform;
////        GameObject go = Instantiate(data.prefab, parent);

////        // Centre in spawn point, keep prefab's designed size
////        RectTransform rt = go.GetComponent<RectTransform>();
////        if (rt != null)
////        {
////            rt.anchoredPosition = Vector2.zero;
////            rt.localScale = Vector3.one;
////            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
////            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
////        }

////        // Start walk animation
////        _currentHorse = go.GetComponent<HorseController>();
////        if (_currentHorse != null)
////            _currentHorse.SetupWalk(data);
////        else
////            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");

////        // Configure the HorseDragHandler that lives on the prefab so this
////        // walk-zone horse can be dragged back to a HorseSlot.
////        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
////        if (drag != null)
////        {
////            drag.horseData = data;
////            drag.destroyOnSuccessfulDrop = true; // removes itself when a slot accepts it
////        }
////        else
////        {
////            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseDragHandler. " +
////                             "Add it to the prefab so the horse can be dragged back to a slot.");
////        }

////        // Tag with a back-reference so HorseSlot can notify us on departure
////        WalkZoneOwner owner = go.GetComponent<WalkZoneOwner>() ?? go.AddComponent<WalkZoneOwner>();
////        owner.Zone = this;

////        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking.");
////    }

////    private void SetHighlight(bool on)
////    {
////        if (zoneHighlight == null) return;
////        zoneHighlight.color = on
////            ? new Color(0.4f, 1f, 0.4f, 0.35f)
////            : new Color(1f, 1f, 1f, 0f);
////    }

////    private void ResetHighlight() => SetHighlight(false);
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// HorseWalkZone
/////
///// Attach to the walk zone GameObject. Requires an Image with Raycast Target = ON.
/////
///// ── Drop: Slot horse → Walk Zone ─────────────────────────────────────────
/////   1. RegisterSuccessfulDrop() is called on the dragged horse's
/////      HorseDragHandler — it will Destroy itself in OnEndDrag.
/////   2. A new prefab instance is spawned here and SetupWalk() is called
/////      → walk animation plays, then auto-switches to idle.
/////   3. The spawned horse's HorseDragHandler is configured so it can be
/////      dragged back to a HorseSlot.
/////
///// ── Drop: Walk-Zone horse → Walk Zone (re-drop) ──────────────────────────
/////   Detected via the WalkZoneOwner component — ignored.
/////
///// ── Highlight ────────────────────────────────────────────────────────────
/////   Assign the zone's own Image to zoneHighlight. It turns green whenever
/////   a slot horse is dragged over this zone.
///// </summary>
//public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    [Header("Spawn")]
//    [Tooltip("Child RectTransform where the horse is placed. Leave empty to use this transform.")]
//    [SerializeField] private RectTransform spawnPoint;

//    [Header("Highlight (optional)")]
//    [Tooltip("Assign this zone's Image. Tinted green while a slot horse is dragged over it.")]
//    [SerializeField] private Image zoneHighlight;

//    // ── Private state ─────────────────────────────────────────────────────────

//    private HorseController _currentHorse;

//    // ── IDropHandler ──────────────────────────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//        if (drag == null || drag.horseData == null) return;

//        // If the dragged object has a WalkZoneOwner it already lives in a walk
//        // zone — don't re-accept it here.
//        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

//        // Tell the drag handler the drop succeeded — it will Destroy itself in OnEndDrag.
//        drag.RegisterSuccessfulDrop();

//        SpawnWalkingHorse(drag.horseData);
//        SetHighlight(false);
//    }

//    // ── Hover highlight ───────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (eventData.pointerDrag == null) return;

//        // Only highlight for slot horses (no WalkZoneOwner), not walk-zone re-drops
//        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;
//        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;

//        SetHighlight(true);
//    }

//    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

//    // ── Public API ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by HorseSlot.OnDrop when the walk-zone horse is dragged to a slot.
//    /// Clears the internal reference so the zone is ready for the next horse.
//    /// </summary>
//    public void NotifyHorseLeft() => _currentHorse = null;

//    // ── Private helpers ───────────────────────────────────────────────────────

//    private void SpawnWalkingHorse(HorseData data)
//    {
//        if (data.prefab == null)
//        {
//            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
//            return;
//        }

//        // Remove any horse already in the zone
//        if (_currentHorse != null)
//        {
//            Destroy(_currentHorse.gameObject);
//            _currentHorse = null;
//        }

//        // Spawn inside the zone
//        Transform parent = spawnPoint != null ? spawnPoint : transform;
//        GameObject go = Instantiate(data.prefab, parent);

//        // Centre in spawn point, keep the prefab's designed size
//        RectTransform rt = go.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale = Vector3.one;
//            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
//            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
//        }

//        // Start walk animation
//        _currentHorse = go.GetComponent<HorseController>();
//        if (_currentHorse != null)
//            _currentHorse.SetupWalk(data);
//        else
//            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");

//        // Configure drag so this horse can be dragged back to a HorseSlot
//        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
//        if (drag != null)
//        {
//            drag.horseData = data;
//            drag.destroyOnSuccessfulDrop = true;
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseDragHandler — " +
//                             "add it to the prefab so the horse can be dragged back to a slot.");
//        }

//        // Tag with a back-reference so HorseSlot.OnDrop can notify this zone
//        WalkZoneOwner owner = go.GetComponent<WalkZoneOwner>() ?? go.AddComponent<WalkZoneOwner>();
//        owner.Zone = this;

//        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' — walking.");
//    }

//    private void SetHighlight(bool on)
//    {
//        if (zoneHighlight == null) return;
//        zoneHighlight.color = on
//            ? new Color(0.4f, 1f, 0.4f, 0.35f)   // green tint while hovering
//            : new Color(1f, 1f, 1f, 0f);           // transparent when idle
//    }
//}


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HorseWalkZone
///
/// Attach to HorseWall. Requires an Image with Raycast Target = ON.
///
/// ── Drop behaviour ────────────────────────────────────────────────────────
///   Slot horse → zone              : horse walks, then idles. Zone gets the horse.
///   Walk-zone horse → zone (re-drop): ignored.
///   Walk-zone horse → occupied slot : HorseSlot.OnDrop calls SpawnWalkingHorse
///                                     to place the displaced slot horse here.
///
/// ── Equip button recall ───────────────────────────────────────────────────
///   HorsePanelManager calls RecallToSlot when the player presses Equip while
///   the horse is already in the walk zone, so no duplicate is spawned.
/// </summary>
public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spawn")]
    [Tooltip("Child RectTransform where the horse is placed. Leave empty to use this transform.")]
    [SerializeField] private RectTransform spawnPoint;

    [Header("Highlight (optional)")]
    [Tooltip("Assign this zone's Image. Tinted green while a slot horse is dragged over it.")]
    [SerializeField] private Image zoneHighlight;

    // ── State ─────────────────────────────────────────────────────────────────

    private HorseController _currentHorse;
    private int _currentInventoryIndex = -1;

    /// <summary>True when a horse is currently in the walk zone.</summary>
    public bool HasHorse => _currentHorse != null;

    /// <summary>The inventory index of the horse currently in the zone (-1 if empty).</summary>
    public int CurrentInventoryIndex => _currentInventoryIndex;

    /// <summary>The HorseData of the horse currently in the zone (null if empty).</summary>
    public HorseData CurrentHorseData => _currentHorse?.Data;

    // ── IDropHandler ──────────────────────────────────────────────────────────

    //public void OnDrop(PointerEventData eventData)
    //{
    //    HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
    //    if (drag == null || drag.horseData == null) return;

    //    // Walk-zone horse re-dropped onto the zone — ignore
    //    if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

    //    drag.RegisterSuccessfulDrop();
    //    SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
    //    SetHighlight(false);
    //}

    // ── IDropHandler ──────────────────────────────────────────────────────────

    //public void OnDrop(PointerEventData eventData)
    //{
    //    HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
    //    if (drag == null || drag.horseData == null) return;

    //    // Walk-zone horse re-dropped onto the zone — ignore
    //    if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

    //    drag.RegisterSuccessfulDrop();

    //    // ── FIX ──────────────────────────────────────────────────────────────
    //    // If the dragged horse came from a slot, clear that slot's _horse
    //    // reference NOW (before OnEndDrag fires). Without this, the slot's
    //    // _horse still points to the (not-yet-Destroy'd) GO when RefreshUI
    //    // runs inside OnEndDrag, so IsOccupied is true and the emptyGroup
    //    // stays hidden — making the slot look permanently occupied.
    //    drag.ownerSlot?.ClearHorseRef();
    //    // ─────────────────────────────────────────────────────────────────────

    //    SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
    //    SetHighlight(false);
    //}

    //public void OnDrop(PointerEventData eventData)
    //{
    //    HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
    //    if (drag == null || drag.horseData == null) return;

    //    if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

    //    drag.RegisterSuccessfulDrop();
    //    drag.ownerSlot?.ClearHorseRef(); // ← ADD: same fix, same reason
    //    SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
    //    SetHighlight(false);
    //}

    public void OnDrop(PointerEventData eventData)
    {
        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
        if (drag == null || drag.horseData == null) return;

        // Walk-zone horse re-dropped onto the zone — ignore
        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

        // ── SLOT HORSE → OCCUPIED WALK ZONE: swap ─────────────────────────────
        // The zone already has a horse. Send it to the source slot, and put the
        // dragged horse into the zone.
        if (HasHorse && drag.ownerSlot != null)
        {
            HorseSlot sourceSlot = drag.ownerSlot;

            // Snapshot the zone horse's data BEFORE SpawnWalkingHorse destroys it
            HorseData zoneData = CurrentHorseData;
            int zoneIdx = _currentInventoryIndex;

            // Clear the source slot ref now, before OnEndDrag fires
            sourceSlot.ClearHorseRef();

            drag.RegisterSuccessfulDrop();

            // Replace zone horse with the dragged horse
            SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);

            // Send the displaced zone horse into the now-empty source slot
            sourceSlot.Equip(zoneData, zoneIdx);

            SetHighlight(false);
            return;
        }

        // ── SLOT HORSE → EMPTY WALK ZONE: simple move ─────────────────────────
        drag.RegisterSuccessfulDrop();
        drag.ownerSlot?.ClearHorseRef();
        SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
        SetHighlight(false);
    }

    // ── Hover highlight ───────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;
        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns (or replaces) a horse in this zone playing the walk animation.
    /// Called by HorseSlot.OnDrop when swapping a walk-zone horse with a slot horse.
    /// inventoryIndex tracks which slot/inventory entry this horse came from.
    /// </summary>
    public void SpawnWalkingHorse(HorseData data, int inventoryIndex = -1)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
            return;
        }

        // Remove the previous horse if any
        if (_currentHorse != null)
        {
            Destroy(_currentHorse.gameObject);
            _currentHorse = null;
        }

        _currentInventoryIndex = inventoryIndex;

        Transform parent = spawnPoint != null ? spawnPoint : transform;
        GameObject go = Instantiate(data.prefab, parent);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
        }

        _currentHorse = go.GetComponent<HorseController>();
        if (_currentHorse != null)
            _currentHorse.SetupWalk(data);
        else
            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");

        // Make this horse draggable back to a slot
        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
        if (drag != null)
        {
            drag.horseData = data;
            drag.destroyOnSuccessfulDrop = true;
            drag.ownerSlot = null;   // not owned by a slot
            drag.inventoryIndex = inventoryIndex;
            drag.onRemovedFromSlot = null;
        }
        else
        {
            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseDragHandler — " +
                             "add it to the prefab so the horse can be dragged back to a slot.");
        }

        // Tag with a back-reference so HorseSlot.OnDrop can call NotifyHorseLeft/SpawnWalkingHorse
        WalkZoneOwner owner = go.GetComponent<WalkZoneOwner>() ?? go.AddComponent<WalkZoneOwner>();
        owner.Zone = this;

        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' (idx={inventoryIndex}) — walking.");
    }

    /// <summary>
    /// Moves the horse from the walk zone directly into a slot.
    /// Called by HorsePanelManager when the player clicks Equip while the
    /// horse is already in this zone — prevents a duplicate from being spawned.
    /// Returns true if a horse was recalled, false if the zone was empty.
    /// </summary>
    public bool RecallToSlot(HorseSlot targetSlot)
    {
        if (!HasHorse || targetSlot == null) return false;

        HorseData data = _currentHorse.Data;
        int idx = _currentInventoryIndex;

        Destroy(_currentHorse.gameObject);
        _currentHorse = null;
        _currentInventoryIndex = -1;

        targetSlot.Equip(data, idx);

        Debug.Log($"[HorseWalkZone] Recalled '{data.horseName}' (idx={idx}) → {targetSlot.name}.");
        return true;
    }

    /// <summary>
    /// Called by HorseSlot.OnDrop when the walk-zone horse is accepted by a slot
    /// (and the zone is not getting a horse back). Clears the reference.
    /// </summary>
    public void NotifyHorseLeft()
    {
        _currentHorse = null;
        _currentInventoryIndex = -1;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetHighlight(bool on)
    {
        if (zoneHighlight == null) return;
        zoneHighlight.color = on
            ? new Color(0.4f, 1f, 0.4f, 0.35f)
            : new Color(1f, 1f, 1f, 0f);
    }
}