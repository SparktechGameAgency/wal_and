using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HorseWalkZone
///
/// Attach to HorseWall. Requires an Image with Raycast Target = ON.
///
/// ── MULTI-HORSE VERSION ──────────────────────────────────────────────────
///   The zone now holds an unlimited number of horses (with or without a
///   mounted soldier) at the same time, each animating and walking
///   independently via its own coroutine. There is no more "the" horse —
///   every horse dropped here is ADDED to the list, never replaces another.
///
/// ── Drop behaviour ────────────────────────────────────────────────────────
///   Slot horse → zone               : horse is ADDED to the zone and starts
///                                      its own idle/walk cycle.
///   Walk-zone horse → zone (re-drop): ignored (snaps back to where it was).
///   Walk-zone horse → occupied slot : HorseSlot.OnDrop calls SpawnWalkingHorse
///                                     to add the displaced slot horse here.
///
/// ── Equip button recall ───────────────────────────────────────────────────
///   HorsePanelManager calls RecallToSlot(slot, inventoryIndex) to pull a
///   SPECIFIC horse (identified by inventory index) out of the zone without
///   touching any other horse currently walking there.
///
/// ── Movement + Flip ───────────────────────────────────────────────────────
///   While in the Run phase each horse moves horizontally across the zone at
///   walkSpeed (UI units/sec), independently of the others. When it hits a
///   zone edge it flips (localScale.x negated) and reverses direction. Each
///   horse stands still during its own Idle phase.
/// </summary>
public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spawn")]
    [Tooltip("Child RectTransform where horses are placed. Leave empty to use this transform.")]
    [SerializeField] private RectTransform spawnPoint;

    [Header("Highlight (optional)")]
    [Tooltip("Assign this zone's Image. Tinted green while a horse is dragged over it.")]
    [SerializeField] private Image zoneHighlight;

    [Header("Walk Cycle Timing")]
    [Tooltip("Seconds a horse stands idle before breaking into a run.")]
    [SerializeField] private float idleDuration = 3f;

    [Tooltip("Seconds a horse runs before returning to idle.")]
    [SerializeField] private float runDuration = 4f;

    [Header("Movement")]
    [Tooltip("How fast horses move across the zone while running (UI units / second).")]
    [SerializeField] private float walkSpeed = 80f;

    [Tooltip("FALLBACK: If the zone RectTransform width cannot be read at runtime (common with\n" +
             "Layout Groups), set this to the zone's pixel width manually.\n" +
             "Leave 0 to auto-detect from RectTransform.")]
    [SerializeField] private float zoneWidthOverride = 0f;

    // ── Per-horse state ───────────────────────────────────────────────────────

    /// <summary>Tracks one horse currently walking in this zone.</summary>
    private class WalkingHorse
    {
        public HorseController controller;
        public RectTransform rectTransform;
        public int inventoryIndex;
        public Coroutine cycleCoroutine;
        public float moveDir = 1f;
        public WalkZoneOwner owner;
    }

    private readonly List<WalkingHorse> _horses = new List<WalkingHorse>();

    public bool HasHorse => _horses.Count > 0;
    public int HorseCount => _horses.Count;

    // ── Resume patrol after panel reactivation ────────────────────────────────
    //
    // GameManager.SetPanelVisible() calls panel.SetActive(false)/(true) when
    // switching screens (e.g. Army → buy soldier → back to Village). Unity
    // KILLS every running coroutine on a GameObject when it's deactivated, and
    // does NOT auto-restart them on reactivation — only Update() resumes on
    // its own. That's why HorseController kept animating (Update-driven) but
    // never moved or changed state again (coroutine-driven): WalkCycleRoutine
    // was dead and nothing restarted it.
    //
    // Fix: whenever this zone re-enables, restart the patrol coroutine for
    // every horse still tracked in _horses. Harmless on the very first enable
    // too, since _horses is empty at that point.
    private void OnEnable()
    {
        foreach (var entry in _horses)
            entry.cycleCoroutine = StartCoroutine(WalkCycleRoutine(entry));
    }

    /// <summary>Legacy single-horse accessor — returns the FIRST horse in the zone (or -1 / null if empty).</summary>
    public int CurrentInventoryIndex => _horses.Count > 0 ? _horses[0].inventoryIndex : -1;
    public HorseData CurrentHorseData => _horses.Count > 0 ? _horses[0].controller?.Data : null;

    /// <summary>True if a horse with this inventory index is currently walking in the zone.</summary>
    public bool ContainsInventoryIndex(int inventoryIndex) => FindByIndex(inventoryIndex) != null;

    // ── Detach button support ─────────────────────────────────────────────────

    /// <summary>True if ANY horse currently in this zone has a soldier mounted.</summary>
    public bool HasMountedHorse
    {
        get
        {
            foreach (var entry in _horses)
                if (entry.controller != null && entry.controller.IsOccupied)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Returns the first HorseController in this zone that has a soldier mounted,
    /// or null if none are occupied. Used by HorseDetachButton.
    /// </summary>
    public HorseController GetFirstMountedHorse()
    {
        foreach (var entry in _horses)
            if (entry.controller != null && entry.controller.IsOccupied)
                return entry.controller;
        return null;
    }

    private WalkingHorse FindByIndex(int inventoryIndex)
    {
        for (int i = 0; i < _horses.Count; i++)
            if (_horses[i].inventoryIndex == inventoryIndex) return _horses[i];
        return null;
    }

    private WalkingHorse FindByOwner(WalkZoneOwner owner)
    {
        for (int i = 0; i < _horses.Count; i++)
            if (_horses[i].owner == owner) return _horses[i];
        return null;
    }

    // ── IDropHandler ──────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
        if (drag == null || drag.horseData == null) return;

        // Walk-zone horse re-dropped onto its own zone — ignore, it never left.
        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

        HorseController dragHC = drag.GetComponent<HorseController>();
        SoldierDragDrop transferSoldier = dragHC != null
            ? dragHC.ExtractRiderForTransfer()
            : null;

        // ── SLOT HORSE → WALK ZONE: always ADDS a new walking horse ──────────
        drag.RegisterSuccessfulDrop();
        drag.ownerSlot?.ClearHorseRef();
        HorseController spawnedHC = SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
        if (transferSoldier != null && spawnedHC != null)
            spawnedHC.PerformMount(transferSoldier);

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
    /// Spawns a NEW horse in this zone and starts its own walk-cycle coroutine.
    /// Existing horses already in the zone are left completely untouched.
    /// </summary>
    public HorseController SpawnWalkingHorse(HorseData data, int inventoryIndex = -1)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
            return null;
        }

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

        // Force canvas layout so rect dimensions are valid before the coroutine reads them.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        HorseController hc = go.GetComponent<HorseController>();

        var entry = new WalkingHorse
        {
            controller = hc,
            rectTransform = rt,
            inventoryIndex = inventoryIndex,
            moveDir = 1f
        };

        // IMPORTANT: add to the list BEFORE starting the coroutine.
        // StartCoroutine runs synchronously up to its first yield, and if the
        // zone width is already measurable (likely here, since we just forced
        // a layout rebuild above), WalkCycleRoutine's main while-loop condition
        // (`_horses.Contains(entry)`) gets checked before this Add() would have
        // run — causing the patrol loop to exit immediately without ever
        // calling SetRun(). Adding first guarantees the entry is already in
        // the list by the time the coroutine checks for it.
        _horses.Add(entry);

        if (hc != null)
        {
            hc.Setup(data);
            hc.ExternallyControlled = true; // zone owns Idle/Run timing — stop Path B auto-revert
            entry.cycleCoroutine = StartCoroutine(WalkCycleRoutine(entry));
        }
        else
        {
            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");
        }

        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
        if (drag != null)
        {
            drag.horseData = data;
            drag.destroyOnSuccessfulDrop = true;
            drag.ownerSlot = null;
            drag.inventoryIndex = inventoryIndex;
            drag.onRemovedFromSlot = null;
        }
        else
        {
            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseDragHandler — " +
                             "add it to the prefab so the horse can be dragged back to a slot.");
        }

        WalkZoneOwner owner = go.GetComponent<WalkZoneOwner>() ?? go.AddComponent<WalkZoneOwner>();
        owner.Zone = this;
        entry.owner = owner;

        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' (idx={inventoryIndex}). " +
                  $"Zone now holds {_horses.Count} horse(s).");

        return hc;
    }

    /// <summary>
    /// Moves the SPECIFIC horse identified by inventoryIndex from the walk zone
    /// directly into a slot. Other horses in the zone are unaffected.
    /// </summary>
    public bool RecallToSlot(HorseSlot targetSlot, int inventoryIndex)
    {
        if (targetSlot == null) return false;
        WalkingHorse entry = FindByIndex(inventoryIndex);
        return entry != null && RecallEntry(entry, targetSlot);
    }

    /// <summary>
    /// Legacy overload — recalls the FIRST horse currently in the zone.
    /// </summary>
    public bool RecallToSlot(HorseSlot targetSlot)
    {
        if (targetSlot == null || _horses.Count == 0) return false;
        return RecallEntry(_horses[0], targetSlot);
    }

    private bool RecallEntry(WalkingHorse entry, HorseSlot targetSlot)
    {
        if (entry.cycleCoroutine != null)
        {
            StopCoroutine(entry.cycleCoroutine);
            entry.cycleCoroutine = null;
        }

        HorseData data = entry.controller != null ? entry.controller.Data : null;
        int idx = entry.inventoryIndex;

        entry.controller?.EjectRiderBeforeDestroy();
        if (entry.controller != null)
            Destroy(entry.controller.gameObject);

        _horses.Remove(entry);

        if (data != null)
            targetSlot.Equip(data, idx);

        Debug.Log($"[HorseWalkZone] Recalled '{data?.horseName}' (idx={idx}) → {targetSlot.name}. " +
                  $"Zone now holds {_horses.Count} horse(s).");
        return true;
    }

    /// <summary>
    /// Called by HorseSlot.OnDrop when ONE SPECIFIC walk-zone horse is accepted
    /// by a slot. Only that horse's entry is removed.
    /// </summary>
    public void NotifyHorseLeft(WalkZoneOwner owner)
    {
        WalkingHorse entry = FindByOwner(owner);
        if (entry == null) return;

        if (entry.cycleCoroutine != null)
        {
            StopCoroutine(entry.cycleCoroutine);
            entry.cycleCoroutine = null;
        }

        _horses.Remove(entry);
    }

    // ── Walk Cycle Coroutine (one instance per horse) ────────────────────────

    private IEnumerator WalkCycleRoutine(WalkingHorse entry)
    {
        RectTransform zoneRT = GetComponent<RectTransform>();

        // ── FIX: use GetLocalCorners to measure zone width reliably ──────────
        // rect.width returns 0 inside LayoutGroups until the layout pass runs.
        // GetLocalCorners reads the ACTUAL rendered corners post-layout and is
        // always correct, even on the very first frame after Instantiate.
        // We still wait up to 30 frames as a safety net, but in practice
        // GetLocalCorners will return a valid non-zero width immediately.
        float zoneW = 0f;
        int waited = 0;
        while (zoneW <= 0f && waited < 30)
        {
            zoneW = GetZoneWidth(zoneRT);
            if (zoneW <= 0f)
            {
                waited++;
                yield return null;
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        if (entry.controller == null) yield break;

        float horseW = entry.rectTransform != null
            ? GetRectWidth(entry.rectTransform)
            : 0f;

        if (zoneW <= 0f)
        {
            Debug.LogError($"[HorseWalkZone] '{name}': zone width is still 0 after {waited} frames. " +
                           "Movement is disabled.\n" +
                           "FIX: Set 'Zone Width Override' in the Inspector to the pixel width " +
                           "of this walk zone (e.g. 400).", this);
        }

        Debug.Log($"[HorseWalkZone] WalkCycleRoutine started for idx={entry.inventoryIndex} — " +
                  $"zoneW={zoneW:F1}  horseW={horseW:F1}  " +
                  $"speed={walkSpeed}  idle={idleDuration}s  run={runDuration}s  " +
                  $"waited={waited} frames");

        while (entry.controller != null && _horses.Contains(entry))
        {
            // ── Idle phase ────────────────────────────────────────────────────
            entry.controller.SetIdle();
            float t = idleDuration;
            while (t > 0f && entry.controller != null && _horses.Contains(entry))
            {
                t -= Time.deltaTime;
                yield return null;
            }
            if (entry.controller == null || !_horses.Contains(entry)) yield break;

            // ── Run phase ─────────────────────────────────────────────────────
            entry.controller.SetRun();
            t = runDuration;
            while (t > 0f && entry.controller != null && _horses.Contains(entry))
            {
                MoveHorse(zoneW, entry);
                t -= Time.deltaTime;
                yield return null;
            }
        }
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the zone's pixel width using GetLocalCorners — reliable even
    /// inside LayoutGroups where rect.width may still be 0.
    /// Falls back to zoneWidthOverride if set in the Inspector.
    /// </summary>
    private float GetZoneWidth(RectTransform zoneRT)
    {
        if (zoneWidthOverride > 0f)
            return zoneWidthOverride;

        // GetLocalCorners fills corners[0..3] in local space:
        //   [0] = bottom-left, [1] = top-left, [2] = top-right, [3] = bottom-right
        Vector3[] corners = new Vector3[4];
        zoneRT.GetLocalCorners(corners);
        float w = corners[2].x - corners[0].x; // top-right.x − bottom-left.x
        return w;
    }

    /// <summary>
    /// Returns the horse's pixel width using GetLocalCorners for the same reason.
    /// </summary>
    private float GetRectWidth(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetLocalCorners(corners);
        return corners[2].x - corners[0].x;
    }

    /// <summary>
    /// Moves ONE horse horizontally each frame and flips it when it reaches
    /// either edge of the zone. Receives the pre-computed zoneW so we don't
    /// re-read it every frame (corners are stable once layout has settled).
    /// </summary>
    private void MoveHorse(float zoneW, WalkingHorse entry)
    {
        if (entry.controller == null || entry.rectTransform == null) return;

        float halfZone = zoneW * 0.5f;
        float halfHorse = GetRectWidth(entry.rectTransform) * 0.5f;

        float leftBound = -halfZone + halfHorse;
        float rightBound = halfZone - halfHorse;

        // Zone too narrow for this horse — skip movement but keep animating.
        if (leftBound >= rightBound) return;

        float newX = entry.rectTransform.anchoredPosition.x
                     + entry.moveDir * walkSpeed * Time.deltaTime;

        if (newX >= rightBound)
        {
            newX = rightBound;
            entry.moveDir = -1f;
            FlipHorse(entry.rectTransform);
        }
        else if (newX <= leftBound)
        {
            newX = leftBound;
            entry.moveDir = 1f;
            FlipHorse(entry.rectTransform);
        }

        entry.rectTransform.anchoredPosition =
            new Vector2(newX, entry.rectTransform.anchoredPosition.y);
    }

    /// <summary>Negates localScale.x to mirror the sprite.</summary>
    private void FlipHorse(RectTransform rt)
    {
        if (rt == null) return;
        Vector3 s = rt.localScale;
        s.x = -s.x;
        rt.localScale = s;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetHighlight(bool on)
    {
        if (zoneHighlight == null) return;
        zoneHighlight.color = on
            ? new Color(0.4f, 1f, 0.4f, 0.35f)
            : new Color(1f, 1f, 1f, 0f);
    }

    public void ReRegisterHorse(HorseController hc, int inventoryIndex = -1)
    {
        if (hc == null) return;

        // Guard: don't double-add
        foreach (var existing in _horses)
            if (existing.controller == hc) return;

        RectTransform rt = hc.GetComponent<RectTransform>();

        WalkZoneOwner owner = hc.GetComponent<WalkZoneOwner>();
        if (owner == null)
            owner = hc.gameObject.AddComponent<WalkZoneOwner>();
        owner.Zone = this;

        var entry = new WalkingHorse
        {
            controller = hc,
            rectTransform = rt,
            inventoryIndex = inventoryIndex,
            moveDir = 1f,
            owner = owner
        };

        _horses.Add(entry);
        hc.ExternallyControlled = true;
        entry.cycleCoroutine = StartCoroutine(WalkCycleRoutine(entry));

        Debug.Log($"[HorseWalkZone] '{hc.name}' re-registered after combat. " +
                  $"Zone now holds {_horses.Count} horse(s).");
    }
}