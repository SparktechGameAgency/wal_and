//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// HorseWalkZone
/////
///// Attach to HorseWall. Requires an Image with Raycast Target = ON.
/////
///// ── Drop behaviour ────────────────────────────────────────────────────────
/////   Slot horse → zone              : horse walks left↔right, then idles.
/////   Walk-zone horse → zone (re-drop): ignored.
/////   Walk-zone horse → occupied slot : HorseSlot.OnDrop calls SpawnWalkingHorse
/////                                     to place the displaced slot horse here.
/////
///// ── Equip button recall ───────────────────────────────────────────────────
/////   HorsePanelManager calls RecallToSlot when the player presses Equip while
/////   the horse is already in the walk zone, so no duplicate is spawned.
/////
///// ── Movement + Flip ───────────────────────────────────────────────────────
/////   While in the Run phase the horse moves horizontally across the zone at
/////   walkSpeed (UI units/sec). When it hits a zone edge it flips (localScale.x
/////   negated) and reverses direction. The horse stands still during Idle.
///// </summary>
//public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    [Header("Spawn")]
//    [Tooltip("Child RectTransform where the horse is placed. Leave empty to use this transform.")]
//    [SerializeField] private RectTransform spawnPoint;

//    [Header("Highlight (optional)")]
//    [Tooltip("Assign this zone's Image. Tinted green while a slot horse is dragged over it.")]
//    [SerializeField] private Image zoneHighlight;

//    [Header("Walk Cycle Timing")]
//    [Tooltip("Seconds the horse stands idle before breaking into a run.")]
//    [SerializeField] private float idleDuration = 3f;

//    [Tooltip("Seconds the horse runs before returning to idle.")]
//    [SerializeField] private float runDuration = 4f;

//    [Header("Movement")]
//    [Tooltip("How fast the horse moves across the zone while running (UI units / second).")]
//    [SerializeField] private float walkSpeed = 80f;

//    [Tooltip("FALLBACK: If the zone RectTransform width cannot be read at runtime (common with\n" +
//             "Layout Groups), set this to the zone's pixel width manually.\n" +
//             "Leave 0 to auto-detect from RectTransform.")]
//    [SerializeField] private float zoneWidthOverride = 0f;

//    // ── State ─────────────────────────────────────────────────────────────────

//    private HorseController _currentHorse;
//    private RectTransform _currentHorseRT;
//    private int _currentInventoryIndex = -1;
//    private Coroutine _walkCycleCoroutine;

//    /// <summary>+1 = moving right, -1 = moving left.</summary>
//    private float _moveDir = 1f;

//    public bool HasHorse => _currentHorse != null;
//    public int CurrentInventoryIndex => _currentInventoryIndex;
//    public HorseData CurrentHorseData => _currentHorse?.Data;

//    // ── IDropHandler ──────────────────────────────────────────────────────────

//    public void OnDrop(PointerEventData eventData)
//    {
//        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
//        if (drag == null || drag.horseData == null) return;

//        // Walk-zone horse re-dropped onto the zone — ignore
//        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

//        // ── SLOT HORSE → OCCUPIED WALK ZONE: swap ─────────────────────────────
//        if (HasHorse && drag.ownerSlot != null)
//        {
//            HorseSlot sourceSlot = drag.ownerSlot;
//            HorseData zoneData = CurrentHorseData;
//            int zoneIdx = _currentInventoryIndex;

//            sourceSlot.ClearHorseRef();
//            drag.RegisterSuccessfulDrop();
//            SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
//            sourceSlot.Equip(zoneData, zoneIdx);

//            SetHighlight(false);
//            return;
//        }

//        // ── SLOT HORSE → EMPTY WALK ZONE: simple move ─────────────────────────
//        drag.RegisterSuccessfulDrop();
//        drag.ownerSlot?.ClearHorseRef();
//        SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
//        SetHighlight(false);
//    }

//    // ── Hover highlight ───────────────────────────────────────────────────────

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (eventData.pointerDrag == null) return;
//        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;
//        if (eventData.pointerDrag.GetComponent<HorseDragHandler>() == null) return;
//        SetHighlight(true);
//    }

//    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

//    // ── Public API ────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Spawns (or replaces) a horse in this zone and starts the walk-cycle coroutine.
//    /// </summary>
//    public void SpawnWalkingHorse(HorseData data, int inventoryIndex = -1)
//    {
//        if (data.prefab == null)
//        {
//            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
//            return;
//        }

//        // Stop any running cycle before replacing the horse
//        if (_walkCycleCoroutine != null)
//        {
//            StopCoroutine(_walkCycleCoroutine);
//            _walkCycleCoroutine = null;
//        }

//        if (_currentHorse != null)
//        {
//            Destroy(_currentHorse.gameObject);
//            _currentHorse = null;
//            _currentHorseRT = null;
//        }

//        _currentInventoryIndex = inventoryIndex;
//        _moveDir = 1f;

//        Transform parent = spawnPoint != null ? spawnPoint : transform;
//        GameObject go = Instantiate(data.prefab, parent);

//        // ── Set up the RectTransform FIRST, then force Canvas layout ──────────
//        // ForceUpdateCanvases must come AFTER sizeDelta is set, not before.
//        // If called before, the Canvas computes bounds with the wrong (or default)
//        // size and zoneRT.rect.width can remain 0 when MoveHorse() reads it.
//        RectTransform rt = go.GetComponent<RectTransform>();
//        if (rt != null)
//        {
//            rt.anchoredPosition = Vector2.zero;
//            rt.localScale = Vector3.one;
//            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
//            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
//        }

//        // Force the Canvas to recompute ALL layout NOW so rect.width is valid
//        // when WalkCycleRoutine runs on the very next frame.
//        Canvas.ForceUpdateCanvases();

//        // Also force the zone RT specifically — needed when the zone uses a
//        // LayoutGroup (HorizontalLayoutGroup, VerticalLayoutGroup, etc.) because
//        // those are rebuilt separately from the general canvas pass.
//        RectTransform zoneRT = spawnPoint != null ? spawnPoint : GetComponent<RectTransform>();
//        LayoutRebuilder.ForceRebuildLayoutImmediate(zoneRT);
//        // ──────────────────────────────────────────────────────────────────────

//        _currentHorse = go.GetComponent<HorseController>();
//        _currentHorseRT = rt;

//        if (_currentHorse != null)
//        {
//            _currentHorse.Setup(data);
//            _walkCycleCoroutine = StartCoroutine(WalkCycleRoutine());
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");
//        }

//        // Make this horse draggable back to a slot
//        HorseDragHandler drag = go.GetComponent<HorseDragHandler>();
//        if (drag != null)
//        {
//            drag.horseData = data;
//            drag.destroyOnSuccessfulDrop = true;
//            drag.ownerSlot = null;
//            drag.inventoryIndex = inventoryIndex;
//            drag.onRemovedFromSlot = null;
//        }
//        else
//        {
//            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseDragHandler — " +
//                             "add it to the prefab so the horse can be dragged back to a slot.");
//        }

//        WalkZoneOwner owner = go.GetComponent<WalkZoneOwner>() ?? go.AddComponent<WalkZoneOwner>();
//        owner.Zone = this;

//        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' (idx={inventoryIndex}).");
//    }

//    /// <summary>
//    /// Moves the horse from the walk zone directly into a slot.
//    /// Returns true if a horse was recalled, false if the zone was empty.
//    /// </summary>
//    public bool RecallToSlot(HorseSlot targetSlot)
//    {
//        if (!HasHorse || targetSlot == null) return false;

//        if (_walkCycleCoroutine != null)
//        {
//            StopCoroutine(_walkCycleCoroutine);
//            _walkCycleCoroutine = null;
//        }

//        HorseData data = _currentHorse.Data;
//        int idx = _currentInventoryIndex;

//        Destroy(_currentHorse.gameObject);
//        _currentHorse = null;
//        _currentHorseRT = null;
//        _currentInventoryIndex = -1;

//        targetSlot.Equip(data, idx);

//        Debug.Log($"[HorseWalkZone] Recalled '{data.horseName}' (idx={idx}) → {targetSlot.name}.");
//        return true;
//    }

//    /// <summary>
//    /// Called by HorseSlot.OnDrop when the walk-zone horse is accepted by a slot.
//    /// Clears the reference so the zone is ready for the next horse.
//    /// </summary>
//    public void NotifyHorseLeft()
//    {
//        if (_walkCycleCoroutine != null)
//        {
//            StopCoroutine(_walkCycleCoroutine);
//            _walkCycleCoroutine = null;
//        }
//        _currentHorse = null;
//        _currentHorseRT = null;
//        _currentInventoryIndex = -1;
//    }

//    // ── Walk Cycle Coroutine ──────────────────────────────────────────────────

//    /// <summary>
//    /// Loops indefinitely:
//    ///   1. Idle for idleDuration seconds  — horse stands still.
//    ///   2. Run  for runDuration  seconds  — horse moves left↔right.
//    ///   3. Repeat.
//    /// </summary>
//    private System.Collections.IEnumerator WalkCycleRoutine()
//    {
//        RectTransform zoneRT = spawnPoint != null ? spawnPoint : GetComponent<RectTransform>();

//        // Wait until the zone rect has a real width.
//        // Canvas.ForceUpdateCanvases + LayoutRebuilder in SpawnWalkingHorse handles
//        // most cases, but we wait up to 30 frames as a hard safety net.
//        int waited = 0;
//        while (GetZoneHalfWidth(zoneRT) <= 0f && waited < 30)
//        {
//            waited++;
//            yield return null;
//        }

//        if (_currentHorse == null) yield break;

//        float zoneW = GetZoneHalfWidth(zoneRT) * 2f;
//        float horseW = _currentHorseRT != null ? _currentHorseRT.rect.width : 0f;

//        if (zoneW <= 0f)
//        {
//            // Zone width is still 0 — this means the RectTransform is not set up
//            // correctly in the scene. Set 'Zone Width Override' in the Inspector
//            // to the pixel width of your walk zone as a manual fallback.
//            Debug.LogError($"[HorseWalkZone] '{name}': zone width is 0 after {waited} frames. " +
//                           "Movement is disabled.\n" +
//                           "FIX: Set 'Zone Width Override' in the Inspector to the pixel width " +
//                           "of this walk zone (e.g. 400).", this);
//            // Still run idle/run animation switching even without movement
//        }

//        Debug.Log($"[HorseWalkZone] WalkCycleRoutine started — " +
//                  $"zoneW={zoneW:F1}  horseW={horseW:F1}  " +
//                  $"speed={walkSpeed}  idle={idleDuration}s  run={runDuration}s  " +
//                  $"waited={waited} frames");

//        while (_currentHorse != null)
//        {
//            // ── Idle phase — stand still ──────────────────────────────────────
//            _currentHorse.SetIdle();
//            float t = idleDuration;
//            while (t > 0f && _currentHorse != null)
//            {
//                t -= Time.deltaTime;
//                yield return null;
//            }
//            if (_currentHorse == null) yield break;

//            // ── Run phase — animate + move across zone ────────────────────────
//            _currentHorse.SetRun();
//            t = runDuration;
//            while (t > 0f && _currentHorse != null)
//            {
//                MoveHorse(zoneRT);
//                t -= Time.deltaTime;
//                yield return null;
//            }
//        }
//    }

//    // ── Movement helpers ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns the half-width to use for zone bounds.
//    /// Uses zoneWidthOverride if set, otherwise reads from the RectTransform.
//    /// </summary>
//    private float GetZoneHalfWidth(RectTransform zoneRT)
//    {
//        if (zoneWidthOverride > 0f)
//            return zoneWidthOverride * 0.5f;

//        return zoneRT.rect.width * 0.5f;
//    }

//    /// <summary>
//    /// Moves the horse horizontally each frame and flips it when it reaches
//    /// either edge of the zone.
//    /// </summary>
//    private void MoveHorse(RectTransform zoneRT)
//    {
//        if (_currentHorse == null || _currentHorseRT == null) return;

//        float halfZone = GetZoneHalfWidth(zoneRT);
//        float halfHorse = _currentHorseRT.rect.width * 0.5f;

//        float leftBound = -halfZone + halfHorse;
//        float rightBound = halfZone - halfHorse;

//        // Zone is too narrow, or width is still 0 — skip movement.
//        if (leftBound >= rightBound) return;

//        float newX = _currentHorseRT.anchoredPosition.x + _moveDir * walkSpeed * Time.deltaTime;

//        if (newX >= rightBound)
//        {
//            newX = rightBound;
//            _moveDir = -1f;
//            FlipHorse();
//        }
//        else if (newX <= leftBound)
//        {
//            newX = leftBound;
//            _moveDir = 1f;
//            FlipHorse();
//        }

//        _currentHorseRT.anchoredPosition = new Vector2(newX, _currentHorseRT.anchoredPosition.y);
//    }

//    /// <summary>Negates localScale.x to mirror the sprite.</summary>
//    private void FlipHorse()
//    {
//        if (_currentHorseRT == null) return;
//        Vector3 s = _currentHorseRT.localScale;
//        s.x = -s.x;
//        _currentHorseRT.localScale = s;
//    }

//    // ── Private helpers ───────────────────────────────────────────────────────

//    private void SetHighlight(bool on)
//    {
//        if (zoneHighlight == null) return;
//        zoneHighlight.color = on
//            ? new Color(0.4f, 1f, 0.4f, 0.35f)
//            : new Color(1f, 1f, 1f, 0f);
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
///   Slot horse → zone              : horse walks left↔right, then idles.
///   Walk-zone horse → zone (re-drop): ignored.
///   Walk-zone horse → occupied slot : HorseSlot.OnDrop calls SpawnWalkingHorse
///                                     to place the displaced slot horse here.
///
/// ── Equip button recall ───────────────────────────────────────────────────
///   HorsePanelManager calls RecallToSlot when the player presses Equip while
///   the horse is already in the walk zone, so no duplicate is spawned.
///
/// ── Movement + Flip ───────────────────────────────────────────────────────
///   While in the Run phase the horse moves horizontally across the zone at
///   walkSpeed (UI units/sec). When it hits a zone edge it flips (localScale.x
///   negated) and reverses direction. The horse stands still during Idle.
/// </summary>
public class HorseWalkZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spawn")]
    [Tooltip("Child RectTransform where the horse is placed. Leave empty to use this transform.")]
    [SerializeField] private RectTransform spawnPoint;

    [Header("Highlight (optional)")]
    [Tooltip("Assign this zone's Image. Tinted green while a slot horse is dragged over it.")]
    [SerializeField] private Image zoneHighlight;

    [Header("Walk Cycle Timing")]
    [Tooltip("Seconds the horse stands idle before breaking into a run.")]
    [SerializeField] private float idleDuration = 3f;

    [Tooltip("Seconds the horse runs before returning to idle.")]
    [SerializeField] private float runDuration = 4f;

    [Header("Movement")]
    [Tooltip("How fast the horse moves across the zone while running (UI units / second).")]
    [SerializeField] private float walkSpeed = 80f;

    [Tooltip("FALLBACK: If the zone RectTransform width cannot be read at runtime (common with\n" +
             "Layout Groups), set this to the zone's pixel width manually.\n" +
             "Leave 0 to auto-detect from RectTransform.")]
    [SerializeField] private float zoneWidthOverride = 0f;

    // ── State ─────────────────────────────────────────────────────────────────

    private HorseController _currentHorse;
    private RectTransform _currentHorseRT;
    private int _currentInventoryIndex = -1;
    private Coroutine _walkCycleCoroutine;

    /// <summary>+1 = moving right, -1 = moving left.</summary>
    private float _moveDir = 1f;

    public bool HasHorse => _currentHorse != null;
    public int CurrentInventoryIndex => _currentInventoryIndex;
    public HorseData CurrentHorseData => _currentHorse?.Data;

    // ── IDropHandler ──────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        HorseDragHandler drag = eventData.pointerDrag?.GetComponent<HorseDragHandler>();
        if (drag == null || drag.horseData == null) return;

        if (eventData.pointerDrag.GetComponent<WalkZoneOwner>() != null) return;

        HorseController dragHC = drag.GetComponent<HorseController>();
        SoldierDragDrop transferSoldier = dragHC != null
            ? dragHC.ExtractRiderForTransfer()
            : null;

        // ── SLOT HORSE → OCCUPIED WALK ZONE: swap ────────────────────────────
        if (HasHorse && drag.ownerSlot != null)
        {
            HorseSlot sourceSlot = drag.ownerSlot;
            HorseData zoneData = CurrentHorseData;
            int zoneIdx = _currentInventoryIndex;

            sourceSlot.ClearHorseRef();
            drag.RegisterSuccessfulDrop();
            HorseController newHC = SpawnWalkingHorse(drag.horseData, drag.inventoryIndex);
            if (transferSoldier != null && newHC != null)
                newHC.PerformMount(transferSoldier);

            sourceSlot.Equip(zoneData, zoneIdx);
            SetHighlight(false);
            return;
        }

        // ── SLOT HORSE → EMPTY WALK ZONE: simple move ────────────────────────
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
    /// Spawns (or replaces) a horse in this zone and starts the walk-cycle coroutine.
    /// </summary>
    public HorseController SpawnWalkingHorse(HorseData data, int inventoryIndex = -1)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[HorseWalkZone] '{data.horseName}' has no prefab assigned!");
            return null;
        }

        // Stop any running cycle before replacing the horse
        if (_walkCycleCoroutine != null)
        {
            StopCoroutine(_walkCycleCoroutine);
            _walkCycleCoroutine = null;
        }

        if (_currentHorse != null)
        {
            Destroy(_currentHorse.gameObject);
            _currentHorse = null;
            _currentHorseRT = null;
        }

        _currentInventoryIndex = inventoryIndex;
        _moveDir = 1f;

        Transform parent = spawnPoint != null ? spawnPoint : transform;
        GameObject go = Instantiate(data.prefab, parent);

        // ── Set up the RectTransform FIRST, then force Canvas layout ──────────
        // ForceUpdateCanvases must come AFTER sizeDelta is set, not before.
        // If called before, the Canvas computes bounds with the wrong (or default)
        // size and zoneRT.rect.width can remain 0 when MoveHorse() reads it.
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            RectTransform prefabRt = data.prefab.GetComponent<RectTransform>();
            if (prefabRt != null) rt.sizeDelta = prefabRt.sizeDelta;
        }

        // Force the Canvas to recompute ALL layout NOW so rect.width is valid
        // when WalkCycleRoutine runs on the very next frame.
        Canvas.ForceUpdateCanvases();

        // Also force the zone RT specifically — needed when the zone uses a
        // LayoutGroup (HorizontalLayoutGroup, VerticalLayoutGroup, etc.) because
        // those are rebuilt separately from the general canvas pass.
        RectTransform zoneRT = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(zoneRT);
        // ──────────────────────────────────────────────────────────────────────

        _currentHorse = go.GetComponent<HorseController>();
        _currentHorseRT = rt;

        if (_currentHorse != null)
        {
            _currentHorse.Setup(data);
            _walkCycleCoroutine = StartCoroutine(WalkCycleRoutine());
        }
        else
        {
            Debug.LogWarning($"[HorseWalkZone] Prefab '{data.horseName}' has no HorseController!");
        }

        // Make this horse draggable back to a slot
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

        Debug.Log($"[HorseWalkZone] Spawned '{data.horseName}' (idx={inventoryIndex}).");

        return _currentHorse;
    }

    /// <summary>
    /// Moves the horse from the walk zone directly into a slot.
    /// Returns true if a horse was recalled, false if the zone was empty.
    /// </summary>
    public bool RecallToSlot(HorseSlot targetSlot)
    {
        if (!HasHorse || targetSlot == null) return false;

        if (_walkCycleCoroutine != null)
        {
            StopCoroutine(_walkCycleCoroutine);
            _walkCycleCoroutine = null;
        }

        HorseData data = _currentHorse.Data;
        int idx = _currentInventoryIndex;

        Destroy(_currentHorse.gameObject);
        _currentHorse = null;
        _currentHorseRT = null;
        _currentInventoryIndex = -1;

        targetSlot.Equip(data, idx);

        Debug.Log($"[HorseWalkZone] Recalled '{data.horseName}' (idx={idx}) → {targetSlot.name}.");
        return true;
    }

    /// <summary>
    /// Called by HorseSlot.OnDrop when the walk-zone horse is accepted by a slot.
    /// Clears the reference so the zone is ready for the next horse.
    /// </summary>
    public void NotifyHorseLeft()
    {
        if (_walkCycleCoroutine != null)
        {
            StopCoroutine(_walkCycleCoroutine);
            _walkCycleCoroutine = null;
        }
        _currentHorse = null;
        _currentHorseRT = null;
        _currentInventoryIndex = -1;
    }

    // ── Walk Cycle Coroutine ──────────────────────────────────────────────────

    /// <summary>
    /// Loops indefinitely:
    ///   1. Idle for idleDuration seconds  — horse stands still.
    ///   2. Run  for runDuration  seconds  — horse moves left↔right.
    ///   3. Repeat.
    /// </summary>
    private System.Collections.IEnumerator WalkCycleRoutine()
    {
        RectTransform zoneRT = GetComponent<RectTransform>();

        // Wait until the zone rect has a real width.
        // Canvas.ForceUpdateCanvases + LayoutRebuilder in SpawnWalkingHorse handles
        // most cases, but we wait up to 30 frames as a hard safety net.
        int waited = 0;
        while (GetZoneHalfWidth(zoneRT) <= 0f && waited < 30)
        {
            waited++;
            yield return null;
        }

        if (_currentHorse == null) yield break;

        float zoneW = GetZoneHalfWidth(zoneRT) * 2f;
        float horseW = _currentHorseRT != null ? _currentHorseRT.rect.width : 0f;

        if (zoneW <= 0f)
        {
            // Zone width is still 0 — this means the RectTransform is not set up
            // correctly in the scene. Set 'Zone Width Override' in the Inspector
            // to the pixel width of your walk zone as a manual fallback.
            Debug.LogError($"[HorseWalkZone] '{name}': zone width is 0 after {waited} frames. " +
                           "Movement is disabled.\n" +
                           "FIX: Set 'Zone Width Override' in the Inspector to the pixel width " +
                           "of this walk zone (e.g. 400).", this);
            // Still run idle/run animation switching even without movement
        }

        Debug.Log($"[HorseWalkZone] WalkCycleRoutine started — " +
                  $"zoneW={zoneW:F1}  horseW={horseW:F1}  " +
                  $"speed={walkSpeed}  idle={idleDuration}s  run={runDuration}s  " +
                  $"waited={waited} frames");

        while (_currentHorse != null)
        {
            // ── Idle phase — stand still ──────────────────────────────────────
            _currentHorse.SetIdle();
            float t = idleDuration;
            while (t > 0f && _currentHorse != null)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            if (_currentHorse == null) yield break;

            // ── Run phase — animate + move across zone ────────────────────────
            _currentHorse.SetRun();
            t = runDuration;
            while (t > 0f && _currentHorse != null)
            {
                MoveHorse(zoneRT);
                t -= Time.deltaTime;
                yield return null;
            }
        }
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the half-width to use for zone bounds.
    /// Uses zoneWidthOverride if set, otherwise reads from the RectTransform.
    /// </summary>
    private float GetZoneHalfWidth(RectTransform zoneRT)
    {
        if (zoneWidthOverride > 0f)
            return zoneWidthOverride * 0.5f;

        return zoneRT.rect.width * 0.5f;
    }

    /// <summary>
    /// Moves the horse horizontally each frame and flips it when it reaches
    /// either edge of the zone.
    /// </summary>
    private void MoveHorse(RectTransform zoneRT)
    {
        if (_currentHorse == null || _currentHorseRT == null) return;

        float halfZone = GetZoneHalfWidth(zoneRT);
        float halfHorse = _currentHorseRT.rect.width * 0.5f;

        float leftBound = -halfZone + halfHorse;
        float rightBound = halfZone - halfHorse;

        // Zone is too narrow, or width is still 0 — skip movement.
        if (leftBound >= rightBound) return;

        float newX = _currentHorseRT.anchoredPosition.x + _moveDir * walkSpeed * Time.deltaTime;

        if (newX >= rightBound)
        {
            newX = rightBound;
            _moveDir = -1f;
            FlipHorse();
        }
        else if (newX <= leftBound)
        {
            newX = leftBound;
            _moveDir = 1f;
            FlipHorse();
        }

        _currentHorseRT.anchoredPosition = new Vector2(newX, _currentHorseRT.anchoredPosition.y);
    }

    /// <summary>Negates localScale.x to mirror the sprite.</summary>
    private void FlipHorse()
    {
        if (_currentHorseRT == null) return;
        Vector3 s = _currentHorseRT.localScale;
        s.x = -s.x;
        _currentHorseRT.localScale = s;
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