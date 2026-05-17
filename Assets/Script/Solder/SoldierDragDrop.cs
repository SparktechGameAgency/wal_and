//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE — SoldierDragDrop
/////
///// Attach to the Soldier prefab root alongside:
/////   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DRAG BEHAVIOUR
///// ════════════════════════════════════════════════════════════════════
/////
/////  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
/////               top of all panels.
/////               If the soldier was riding a dragon, the seat is released,
/////               the soldier's own visuals are restored (alpha 1), and
/////               DragonController.PerformDismount() hides the rider visual
/////               — all AFTER the soldier is safely at canvas-root.
/////
/////  OnDrag       Moves the soldier under the pointer.
/////
/////  OnEndDrag    Raycasts under the pointer:
/////                 → DragonController with free seat → PerformMount()
/////                 → Occupied seat (unlocked rider)  → Swap riders
/////                 → Occupied seat (locked rider)    → SnapBack
/////                 → Empty space                    → SnapBack
/////
///// ════════════════════════════════════════════════════════════════════
/////  MOUNT FLOW
///// ════════════════════════════════════════════════════════════════════
/////
/////  SoldierDragDrop.OnEndDrag
/////    └─ DragonController.PerformMount(soldier, seat)
/////         ├─ DragonRiderSeat.MountSoldier(soldier)
/////         │    └─ soldier.MountOnDragon(seat, offset)
/////         │         ├─ EnsureHelmetEquipped()
/////         │         ├─ Reparent soldier under seat
/////         │         ├─ HideOwnVisuals()    ← alpha=0, blocksRaycasts=true, interactable=true
/////         │         └─ SpriteLayerAnimator → RiderIdle
/////         └─ DragonRiderVisual.ShowForSoldier()  ← dragon shows armored rider
/////
///// ════════════════════════════════════════════════════════════════════
/////  ATTACH / LOCK SYSTEM
///// ════════════════════════════════════════════════════════════════════
/////
/////  After mount, the soldier is UNLOCKED by default:
/////    blocksRaycasts = true  → player can click the rider area and drag
/////                              the soldier off the dragon normally.
/////
/////  DragonController.OnBeginDrag blocks dragon drag while rider is unlocked,
/////  so clicking the dragon accidentally doesn't move it.
/////
/////  After the player clicks the Attach button, SetLocked(true) is called:
/////    blocksRaycasts = false → clicks pass through the invisible soldier
/////                              down to the dragon body image, letting
/////                              the player drag the whole dragon+rider unit
/////                              to a FlyZone.
/////    interactable   = false → OnBeginDrag on the soldier is suppressed.
/////
/////  Clicking Attached again calls SetLocked(false) — restores draggability.
/////  DismountFromDragon() always resets to unlocked.
/////
///// ════════════════════════════════════════════════════════════════════
/////  DISMOUNT FLOW
///// ════════════════════════════════════════════════════════════════════
/////
/////  SoldierDragDrop.OnBeginDrag  (soldier dragged off dragon)
/////    ├─ seat.ReleaseSoldier()
/////    ├─ ShowOwnVisuals()                         ← soldier turns visible again
/////    ├─ Reparent soldier to canvas root
/////    └─ DragonController.PerformDismount()       ← dragon hides rider visual
/////
/////  SoldierDragDrop.DismountFromDragon()          (programmatic, e.g. Retrieve button)
/////    ├─ seat.ReleaseSoldier()
/////    ├─ Reparent soldier to ground home
/////    ├─ ShowOwnVisuals()
/////    └─ DragonController.PerformDismount()
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Attach to SoldierPrefab root alongside CanvasGroup,
/////     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
/////  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
/////  3. Root Canvas must have a GraphicRaycaster.
/////  4. An EventSystem must exist in the scene.
/////  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
/////     No Layout Group — it overrides anchoredPosition every frame.
///// </summary>
//[RequireComponent(typeof(CanvasGroup))]
//public class SoldierDragDrop : MonoBehaviour,
//    IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Dragon Mount Settings")]
//    [Tooltip("Maps each armor to its default helmet.\n" +
//             "Create via: right-click Project -> Create -> AreaForge -> Armor Helmet Table.\n" +
//             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
//    [SerializeField] private ArmorHelmetTable helmetTable;

//    // ── Component References ──────────────────────────────────────────────────

//    private CanvasGroup _canvasGroup;
//    private RectTransform _rect;
//    private SoldierController _controller;    // optional — patrol + flip
//    private CharacterEquipment _equipment;    // tracks equipped items
//    private SpriteLayerAnimator _animator;    // drives per-layer animation

//    // ── Drag State ────────────────────────────────────────────────────────────

//    private Canvas _rootCanvas;
//    private Transform _homeParent;
//    private Vector2 _homeAnchoredPosition;
//    private bool _isDragging;

//    // ── Dragon Rider State ────────────────────────────────────────────────────

//    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
//    private DragonRiderSeat _currentSeat;

//    /// <summary>
//    /// Ground parent recorded before mounting so DismountFromDragon() can
//    /// return the soldier to its patrol area, not back to the seat.
//    /// </summary>
//    private Transform _mountHomeParent;
//    private Vector2 _mountHomePos;

//    // ── Lock State ────────────────────────────────────────────────────────────

//    // When true the soldier cannot be dragged off the seat.
//    // Toggled by DragonAttachButton via SetLocked(). Reset to false on any dismount.
//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

//    // ══════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        _canvasGroup = GetComponent<CanvasGroup>();
//        _rect = GetComponent<RectTransform>();
//        _controller = GetComponent<SoldierController>();   // optional
//        _equipment = GetComponent<CharacterEquipment>();
//        _animator = GetComponent<SpriteLayerAnimator>();

//        if (_equipment == null)
//            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
//                             $"'{name}'. Helmet auto-equip will be skipped.", this);
//        if (_animator == null)
//            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
//                             $"'{name}'. Riding animation will not play.", this);
//        if (helmetTable == null)
//            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
//                             $"'{name}'. Soldiers will mount without a helmet.", this);
//    }

//    private void Start()
//    {
//        RecordHome();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — BEGIN
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (_isDragging) return;

//        // Locked to the dragon — drag is disabled until the player clicks
//        // Attached (which calls SetLocked(false)). With _isLocked=true the
//        // CanvasGroup has interactable=false so Unity shouldn't route drag
//        // events here at all, but this is a safety guard.
//        if (_isLocked) return;

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            // Restore sprite layers before they become visible again.
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Re-find root canvas every drag — cached value breaks after Retrieve
//        // re-parents the soldier to a different panel.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            return;
//        }

//        // If dismounting, override home with the ground position saved at mount time.
//        RecordHome();
//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;   // consumed — prevent stale reuse
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Restore soldier's own visuals BEFORE reparenting so alpha=1 when visible.
//        if (wasMounted)
//            ShowOwnVisuals();

//        // Reparent to root canvas so the soldier draws above all panels.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // Notify dragon to hide its rider visual now that the soldier is safe
//        // at canvas-root (never call this while soldier is still a child of dragon).
//        if (wasMounted)
//            mountedDragonDC?.PerformDismount();
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — MOVE
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_rootCanvas == null) return;
//        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAG — END
//    // ══════════════════════════════════════════════════════════════════════════

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _isDragging = false;
//        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
//        // CanvasGroup does not shadow the dragon sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;

//        foreach (var r in results)
//        {
//            var dragon = r.gameObject.GetComponentInParent<DragonController>();
//            if (dragon == null) continue;

//            targetDC = dragon;
//            targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//            break;
//        }

//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.alpha = 1f;

//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            // Valid drop on an unoccupied dragon.
//            // Save ground home BEFORE mounting so DismountFromDragon() can return here.
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;

//            targetDC.PerformMount(this, targetSeat);
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                // Rider is locked (Attached) — swap blocked. Snap this soldier back.
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
//                // Swap: return current rider home, then mount this soldier.
//                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
//                _mountHomeParent = _homeParent;
//                _mountHomePos = _homeAnchoredPosition;
//                currentRider.DismountFromDragon();
//                targetDC.PerformMount(this, targetSeat);
//            }
//            else
//            {
//                SnapBack();
//            }
//        }
//        else
//        {
//            // Dropped on empty space — return to patrol area.
//            SnapBack();
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DROP OUTCOMES
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
//    public void SnapBack()
//    {
//        transform.SetParent(_homeParent, worldPositionStays: true);
//        _rect.anchoredPosition = _homeAnchoredPosition;
//        ShowOwnVisuals();
//        _controller?.SetPatrolling(true);
//        Debug.Log("[SoldierDragDrop] Snapped back to home.");
//    }

//    /// <summary>
//    /// Called by a drop target (WizardBox) after accepting the soldier.
//    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
//    /// </summary>
//    public void OnSuccessfulDrop()
//    {
//        _isDragging = false;
//        _canvasGroup.blocksRaycasts = true;
//        _controller?.SetPatrolling(false);
//        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
//    }

//    /// <summary>
//    /// Re-parents the soldier to spawnParent and resumes patrol.
//    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
//    /// </summary>
//    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
//    {
//        if (spawnParent == null)
//        {
//            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
//            return;
//        }

//        transform.SetParent(spawnParent, worldPositionStays: true);

//        if (spawnPosition.HasValue)
//            _rect.anchoredPosition = spawnPosition.Value;

//        _canvasGroup.blocksRaycasts = true;
//        _isDragging = false;

//        ShowOwnVisuals();
//        RecordHome();
//        _controller?.SetPatrolling(true);

//        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    ///
//    /// Order:
//    ///   1. Auto-equip helmet if missing.
//    ///   2. Stop patrol and freeze facing direction.
//    ///   3. Reparent soldier under the seat at seatOffset.
//    ///   4. Hide the soldier's own visuals (dragon's rider visual takes over).
//    ///      HideOwnVisuals sets blocksRaycasts=true — the UNLOCKED default,
//    ///      meaning the player can drag the soldier off immediately without
//    ///      needing to click Attach first.
//    ///   5. Switch SpriteLayerAnimator to RiderIdle state.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        // 1. Auto-equip helmet BEFORE reparenting.
//        EnsureHelmetEquipped();

//        // 2. Stop patrol.
//        _controller?.EnterRidingState();

//        // 3. Reparent under the seat.
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        // 4. Hide soldier's own visuals — dragon's rider visual shows instead.
//        //    blocksRaycasts=true is set here → UNLOCKED default state.
//        HideOwnVisuals();

//        // 5. Switch animation state.
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's
//    /// rider visual.
//    ///
//    /// Safe order:
//    ///   1. Capture dragon DC before clearing _currentSeat.
//    ///   2. Release seat.
//    ///   3. Reparent soldier to ground home.
//    ///   4. Restore soldier's visuals + unlock.
//    ///   5. Call PerformDismount() on the dragon.
//    /// </summary>
//    public void DismountFromDragon()
//    {
//        DragonController riderDragonDC = null;
//        if (_currentSeat != null)
//        {
//            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//        }

//        if (_mountHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
//                             "recorded — snapping to current home.");
//            ShowOwnVisuals();
//            SnapBack();
//            riderDragonDC?.PerformDismount();
//            return;
//        }

//        // Reparent soldier to ground home.
//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        // Restore patrol and facing direction.
//        _controller?.ExitRidingState();

//        // Restore sprite layers to idle.
//        _animator?.SetState(AnimationState.Idle);

//        // Unlock and restore full visibility + raycast blocking.
//        _isLocked = false;
//        ShowOwnVisuals();   // sets alpha=1, blocksRaycasts=true, interactable=true

//        RecordHome();
//        _mountHomeParent = null;   // consumed — prevent stale reuse

//        // Notify dragon to hide its rider visual.
//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton when the player clicks Attach / Attached.
//    ///
//    ///   locked = true
//    ///     • _isLocked = true         → OnBeginDrag guard activates
//    ///     • interactable   = false   → EventSystem ignores drag events on soldier
//    ///     • blocksRaycasts = false   → clicks pass THROUGH to the dragon body
//    ///                                  so DragonController.OnBeginDrag fires,
//    ///                                  letting the player drag dragon to FlyZone
//    ///
//    ///   locked = false
//    ///     • _isLocked = false        → OnBeginDrag guard deactivates
//    ///     • interactable   = true    → drag events fire on soldier again
//    ///     • blocksRaycasts = true    → clicks land on the soldier, not dragon,
//    ///                                  so the player can drag the soldier off
//    ///
//    /// Has no effect if the soldier is not currently mounted.
//    /// </summary>
//    public void SetLocked(bool locked)
//    {
//        if (_currentSeat == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
//            return;
//        }

//        _isLocked = locked;

//        if (locked)
//        {
//            // Clicks pass through the invisible soldier to the dragon below.
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.interactable = false;
//        }
//        else
//        {
//            // Clicks land on the soldier — player can drag them off the dragon.
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.interactable = true;
//        }

//        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Hides the soldier's own sprite layers (alpha 0).
//    /// blocksRaycasts=true and interactable=true are preserved so the soldier
//    /// is in the UNLOCKED mounted state — draggable off the dragon by default.
//    /// Called by MountOnDragon(). The dragon's rider visual displays instead.
//    /// </summary>
//    private void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;   // UNLOCKED default: soldier is draggable
//        _canvasGroup.interactable = true;
//    }

//    /// <summary>
//    /// Restores the soldier's own sprite layers to fully visible.
//    /// Also ensures the CanvasGroup is in the correct unlocked state.
//    /// Called on dismount (drag-off or programmatic).
//    /// </summary>
//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// If the soldier has no Helmet equipped, looks up the default helmet for
//    /// their Armor in ArmorHelmetTable and equips it automatically.
//    /// This ensures the dragon's rider visual shows the correct helmet sprite.
//    /// </summary>
//    private void EnsureHelmetEquipped()
//    {
//        if (_equipment == null) return;
//        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

//        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
//        EquipmentItem helmet = helmetTable != null
//            ? helmetTable.GetDefaultHelmet(armor)
//            : null;

//        if (helmet != null)
//        {
//            _equipment.Equip(helmet);
//            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
//                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
//        }
//        else
//        {
//            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
//                             $"armor '{armor?.itemName ?? "none"}'. " +
//                             "Set fallbackHelmet in ArmorHelmetTable.", this);
//        }
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELPER
//    // ══════════════════════════════════════════════════════════════════════════

//    private void RecordHome()
//    {
//        _homeParent = transform.parent;
//        _homeAnchoredPosition = _rect.anchoredPosition;
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — SoldierDragDrop
///
/// Attach to the Soldier prefab root alongside:
///   CanvasGroup, CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///
/// ════════════════════════════════════════════════════════════════════
///  DRAG BEHAVIOUR
/// ════════════════════════════════════════════════════════════════════
///
///  OnBeginDrag  Lifts the soldier to canvas-root level so it draws on
///               top of all panels. If riding a dragon or stationed at
///               a cannon slot, releases the position first.
///
///  OnDrag       Moves the soldier under the pointer.
///
///  OnEndDrag    Raycasts under the pointer:
///                 → DragonController with free seat → PerformMount()
///                 → CannonSlot                      → PlaceAtCannonSlot()
///                 → Occupied seat                   → SnapBack
///                 → Empty space                     → SnapBack
///
/// ════════════════════════════════════════════════════════════════════
///  CANNON SLOT
/// ════════════════════════════════════════════════════════════════════
///
///  Drag a soldier onto a CannonSlot to place them behind the cannon.
///  CurrentlyDragging (static) is set in OnBeginDrag and cleared in
///  OnEndDrag — CannonSlot.OnDrop reads it to accept the soldier.
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP
/// ════════════════════════════════════════════════════════════════════
///
///  1. Attach to SoldierPrefab root alongside CanvasGroup,
///     CharacterEquipment, CharacterVisuals, SpriteLayerAnimator.
///  2. Drag your ArmorHelmetTable ScriptableObject into helmetTable.
///  3. Root Canvas must have a GraphicRaycaster.
///  4. An EventSystem must exist in the scene.
///  5. The spawn panel must be a RectTransform + Image (Raycast Target ON).
///     No Layout Group — it overrides anchoredPosition every frame.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SoldierDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dragon Mount Settings")]
    [Tooltip("Maps each armor to its default helmet.\n" +
             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon.")]
    [SerializeField] private ArmorHelmetTable helmetTable;

    // ── Component References ──────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private SoldierController _controller;    // optional — patrol + flip
    private CharacterEquipment _equipment;    // tracks equipped items
    private SpriteLayerAnimator _animator;    // drives per-layer animation

    // ── Drag State ────────────────────────────────────────────────────────────

    private Canvas _rootCanvas;
    private Transform _homeParent;
    private Vector2 _homeAnchoredPosition;
    private bool _isDragging;

    // ── Dragon Rider State ────────────────────────────────────────────────────

    /// <summary>Seat this soldier is currently riding on. Null = on the ground.</summary>
    private DragonRiderSeat _currentSeat;

    /// <summary>
    /// Ground parent recorded before mounting so DismountFromDragon() can
    /// return the soldier to its patrol area, not back to the seat.
    /// </summary>
    private Transform _mountHomeParent;
    private Vector2 _mountHomePos;

    // ── Lock State ────────────────────────────────────────────────────────────

    private bool _isLocked = false;

    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
    public bool IsRiding => _currentSeat != null;

    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
    public bool IsLocked => _isLocked;

    // ══════════════════════════════════════════════════════════════════════════
    // CANNON SLOT STATE  (NEW)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
    /// </summary>
    public static SoldierDragDrop CurrentlyDragging { get; private set; }

    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
    private CannonSlot _currentCannonSlot;

    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
    public bool IsAtCannon => _currentCannonSlot != null;

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _controller = GetComponent<SoldierController>();   // optional
        _equipment = GetComponent<CharacterEquipment>();
        _animator = GetComponent<SpriteLayerAnimator>();

        if (_equipment == null)
            Debug.LogWarning("[SoldierDragDrop] CharacterEquipment not found on " +
                             $"'{name}'. Helmet auto-equip will be skipped.", this);
        if (_animator == null)
            Debug.LogWarning("[SoldierDragDrop] SpriteLayerAnimator not found on " +
                             $"'{name}'. Riding animation will not play.", this);
        if (helmetTable == null)
            Debug.LogWarning("[SoldierDragDrop] helmetTable is not assigned on " +
                             $"'{name}'. Soldiers will mount without a helmet.", this);
    }

    private void Start()
    {
        RecordHome();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — BEGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        // Locked to the dragon — drag disabled until Attach is toggled off.
        if (_isLocked) return;

        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
        CurrentlyDragging = this;

        // ── Release cannon slot if stationed there ────────────────────────────
        if (_currentCannonSlot != null)
        {
            _currentCannonSlot.ReleaseSoldier(notify: false);
            _currentCannonSlot = null;
        }

        // ── Capture mounted dragon before clearing the seat ───────────────────
        bool wasMounted = _currentSeat != null;
        DragonController mountedDragonDC = null;

        if (wasMounted)
        {
            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
            _animator?.SetState(AnimationState.Idle);
        }

        // Re-find root canvas every drag.
        _rootCanvas = GetComponentInParent<Canvas>();
        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

        if (_rootCanvas == null)
        {
            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
                           "Make sure the soldier is inside a Canvas.");
            CurrentlyDragging = null;
            return;
        }

        // If dismounting, override home with the ground position saved at mount time.
        RecordHome();
        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // Restore own visuals before reparenting (in case coming from dragon).
        if (wasMounted)
            ShowOwnVisuals();

        // Reparent to root canvas so the soldier draws above all panels.
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha = 0.75f;
        _canvasGroup.blocksRaycasts = false;

        // Notify dragon to hide its rider visual.
        if (wasMounted)
            mountedDragonDC?.PerformDismount();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — MOVE
    // ══════════════════════════════════════════════════════════════════════════

    public void OnDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;
        _rect.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAG — END
    // ══════════════════════════════════════════════════════════════════════════

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        // Keep blocksRaycasts FALSE until AFTER the raycast so the soldier's
        // CanvasGroup does not shadow targets sitting underneath.

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragonRiderSeat targetSeat = null;
        DragonController targetDC = null;
        CannonSlot targetCannon = null;   // NEW

        foreach (var r in results)
        {
            // ── Check for cannon slot ─────────────────────────────────────────
            if (targetCannon == null)
                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

            // ── Check for dragon ──────────────────────────────────────────────
            if (targetDC == null)
            {
                var dragon = r.gameObject.GetComponentInParent<DragonController>();
                if (dragon != null)
                {
                    targetDC = dragon;
                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
                }
            }

            if (targetCannon != null && targetDC != null) break;
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // ── Clear the static drag reference ───────────────────────────────────
        CurrentlyDragging = null;

        // ── Cannon slot drop (NEW) ────────────────────────────────────────────
        if (targetCannon != null)
        {
            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
            ShowOwnVisuals();
            _controller?.SetPatrolling(false);
            return;
        }

        // ── Dragon drop (original logic) ──────────────────────────────────────
        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

        if (targetDC != null && targetSeat != null && seatFree)
        {
            _mountHomeParent = _homeParent;
            _mountHomePos = _homeAnchoredPosition;
            targetDC.PerformMount(this, targetSeat);
        }
        else if (targetSeat != null && targetSeat.IsOccupied)
        {
            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

            if (currentRider != null && currentRider._isLocked)
            {
                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
                SnapBack();
            }
            else if (currentRider != null)
            {
                Debug.Log($"[SoldierDragDrop] Swapping '{currentRider.name}' out for '{name}'.");
                _mountHomeParent = _homeParent;
                _mountHomePos = _homeAnchoredPosition;
                currentRider.DismountFromDragon();
                targetDC.PerformMount(this, targetSeat);
            }
            else
            {
                SnapBack();
            }
        }
        else
        {
            SnapBack();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DROP OUTCOMES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Returns the soldier to its home position and resumes patrol.</summary>
    public void SnapBack()
    {
        transform.SetParent(_homeParent, worldPositionStays: true);
        _rect.anchoredPosition = _homeAnchoredPosition;
        ShowOwnVisuals();
        _controller?.SetPatrolling(true);
        Debug.Log("[SoldierDragDrop] Snapped back to home.");
    }

    /// <summary>
    /// Called by a drop target after accepting the soldier.
    /// Resets flags because SetActive(false) prevents OnEndDrag from firing.
    /// </summary>
    public void OnSuccessfulDrop()
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _controller?.SetPatrolling(false);
        Debug.Log("[SoldierDragDrop] Accepted by drop target.");
    }

    /// <summary>
    /// Re-parents the soldier to spawnParent and resumes patrol.
    /// Call from WizardBox "Retrieve" instead of calling SetParent directly.
    /// </summary>
    public void Retrieve(Transform spawnParent, Vector2? spawnPosition = null)
    {
        if (spawnParent == null)
        {
            Debug.LogError("[SoldierDragDrop] Retrieve: spawnParent is null.");
            return;
        }

        transform.SetParent(spawnParent, worldPositionStays: true);

        if (spawnPosition.HasValue)
            _rect.anchoredPosition = spawnPosition.Value;

        _canvasGroup.blocksRaycasts = true;
        _isDragging = false;

        ShowOwnVisuals();
        RecordHome();
        _controller?.SetPatrolling(true);

        Debug.Log($"[SoldierDragDrop] Retrieved to '{spawnParent.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CANNON SLOT MOUNT / RELEASE  (NEW)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
    /// cannon's SoldierSpawnpoint and records the new home.
    ///
    /// Safe to call whether the soldier was at a dragon, another cannon slot,
    /// or the ground spawn area.
    /// </summary>
    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
    {
        if (slot == null || spawnpoint == null) return;

        // Release any existing dragon seat first
        if (_currentSeat != null)
        {
            var dc = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
            dc?.PerformDismount();
        }

        // Release previous cannon slot without notifying (we're already moving)
        if (_currentCannonSlot != null && _currentCannonSlot != slot)
            _currentCannonSlot.ReleaseSoldier(notify: false);

        _currentCannonSlot = slot;

        // Reparent to the cannon's SoldierSpawnpoint
        transform.SetParent(spawnpoint, worldPositionStays: false);
        _rect.anchoredPosition = Vector2.zero;
        _rect.localScale = Vector3.one;

        // Record this position as home so SnapBack() returns here
        RecordHome();

        // Restore visuals and stop patrol
        ShowOwnVisuals();
        _animator?.SetState(AnimationState.Idle);
        _controller?.ExitRidingState();

        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
    }

    /// <summary>
    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
    /// Snaps the soldier back to their original home position.
    /// </summary>
    public void RemoveFromCannonSlot()
    {
        if (_currentCannonSlot == null) return;
        _currentCannonSlot = null;
        SnapBack();
        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
    /// </summary>
    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
    {
        _currentSeat = seat;

        EnsureHelmetEquipped();

        _controller?.EnterRidingState();

        transform.SetParent(seat.transform, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        RecordHome();

        HideOwnVisuals();
        _animator?.SetState(AnimationState.RiderIdle);

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
    /// </summary>
    public void DismountFromDragon()
    {
        DragonController riderDragonDC = null;
        if (_currentSeat != null)
        {
            riderDragonDC = _currentSeat.GetComponentInParent<DragonController>();
            _currentSeat.ReleaseSoldier();
            _currentSeat = null;
        }

        if (_mountHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] DismountFromDragon: no mount home " +
                             "recorded — snapping to current home.");
            ShowOwnVisuals();
            SnapBack();
            riderDragonDC?.PerformDismount();
            return;
        }

        transform.SetParent(_mountHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHomePos;

        _controller?.ExitRidingState();
        _animator?.SetState(AnimationState.Idle);
        ShowOwnVisuals();

        RecordHome();
        _mountHomeParent = null;

        riderDragonDC?.PerformDismount();

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ATTACH LOCK
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Locks or unlocks the soldier to the current dragon seat.
    /// Called by DragonAttachButton.
    /// </summary>
    public void SetLocked(bool locked)
    {
        if (_currentSeat == null)
        {
            Debug.LogWarning("[SoldierDragDrop] SetLocked called but soldier is not mounted.", this);
            return;
        }

        _isLocked = locked;

        if (locked)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        else
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // VISUAL SHOW / HIDE
    // ══════════════════════════════════════════════════════════════════════════

    private void HideOwnVisuals()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    private void ShowOwnVisuals()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELMET AUTO-EQUIP
    // ══════════════════════════════════════════════════════════════════════════

    private void EnsureHelmetEquipped()
    {
        if (_equipment == null) return;
        if (_equipment.GetEquipped(EquipmentSlot.Helmet) != null) return;

        EquipmentItem armor = _equipment.GetEquipped(EquipmentSlot.Armor);
        EquipmentItem helmet = helmetTable != null
            ? helmetTable.GetDefaultHelmet(armor)
            : null;

        if (helmet != null)
        {
            _equipment.Equip(helmet);
            Debug.Log($"[SoldierDragDrop] Auto-equipped '{helmet.itemName}' " +
                      $"(armor: '{armor?.itemName ?? "none"}') on mount.", this);
        }
        else
        {
            Debug.LogWarning($"[SoldierDragDrop] No default helmet found for " +
                             $"armor '{armor?.itemName ?? "none"}'. " +
                             "Set fallbackHelmet in ArmorHelmetTable.", this);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPER
    // ══════════════════════════════════════════════════════════════════════════

    private void RecordHome()
    {
        _homeParent = transform.parent;
        _homeAnchoredPosition = _rect.anchoredPosition;
    }
}