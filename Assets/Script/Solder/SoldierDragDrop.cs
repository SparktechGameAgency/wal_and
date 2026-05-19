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
/////               top of all panels. If riding a dragon or stationed at
/////               a cannon slot, releases the position first.
/////
/////  OnDrag       Moves the soldier under the pointer.
/////
/////  OnEndDrag    Raycasts under the pointer:
/////                 → DragonController with free seat → PerformMount()
/////                 → CannonSlot                      → PlaceAtCannonSlot()
/////                 → Occupied seat                   → SnapBack
/////                 → Empty space                     → SnapBack
/////
///// ════════════════════════════════════════════════════════════════════
/////  CANNON SLOT
///// ════════════════════════════════════════════════════════════════════
/////
/////  Drag a soldier onto a CannonSlot to place them behind the cannon.
/////  CurrentlyDragging (static) is set in OnBeginDrag and cleared in
/////  OnEndDrag — CannonSlot.OnDrop reads it to accept the soldier.
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
//             "Create via: right-click Project → Create → AreaForge → Armor Helmet Table.\n" +
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

//    private bool _isLocked = false;

//    /// <summary>True while this soldier is sitting on a dragon seat.</summary>
//    public bool IsRiding => _currentSeat != null;

//    /// <summary>True while this soldier is locked to a dragon seat by the Attach button.</summary>
//    public bool IsLocked => _isLocked;

//    // ══════════════════════════════════════════════════════════════════════════
//    // CANNON SLOT STATE  (NEW)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
//    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
//    /// </summary>
//    public static SoldierDragDrop CurrentlyDragging { get; private set; }

//    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
//    private CannonSlot _currentCannonSlot;

//    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
//    public bool IsAtCannon => _currentCannonSlot != null;

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

//        // Locked to the dragon — drag disabled until Attach is toggled off.
//        if (_isLocked) return;

//        // ── Expose this instance for CannonSlot.OnDrop ────────────────────────
//        CurrentlyDragging = this;

//        // ── Release cannon slot if stationed there ────────────────────────────
//        if (_currentCannonSlot != null)
//        {
//            _currentCannonSlot.ReleaseSoldier(notify: false);
//            _currentCannonSlot = null;
//        }

//        // ── Capture mounted dragon before clearing the seat ───────────────────
//        bool wasMounted = _currentSeat != null;
//        DragonController mountedDragonDC = null;

//        if (wasMounted)
//        {
//            mountedDragonDC = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            _animator?.SetState(AnimationState.Idle);
//        }

//        // Re-find root canvas every drag.
//        _rootCanvas = GetComponentInParent<Canvas>();
//        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
//            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

//        if (_rootCanvas == null)
//        {
//            Debug.LogError("[SoldierDragDrop] No root Canvas found. " +
//                           "Make sure the soldier is inside a Canvas.");
//            CurrentlyDragging = null;
//            return;
//        }

//        // If dismounting, override home with the ground position saved at mount time.
//        RecordHome();
//        if (wasMounted && _mountHomeParent != null)
//        {
//            _homeParent = _mountHomeParent;
//            _homeAnchoredPosition = _mountHomePos;
//            _mountHomeParent = null;
//        }

//        _isDragging = true;
//        _controller?.SetPatrolling(false);

//        // Restore own visuals before reparenting (in case coming from dragon).
//        if (wasMounted)
//            ShowOwnVisuals();

//        // Reparent to root canvas so the soldier draws above all panels.
//        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
//        transform.SetAsLastSibling();

//        _canvasGroup.alpha = 0.75f;
//        _canvasGroup.blocksRaycasts = false;

//        // Notify dragon to hide its rider visual.
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
//        // CanvasGroup does not shadow targets sitting underneath.

//        var results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, results);

//        DragonRiderSeat targetSeat = null;
//        DragonController targetDC = null;
//        CannonSlot targetCannon = null;   // NEW

//        foreach (var r in results)
//        {
//            // ── Check for cannon slot ─────────────────────────────────────────
//            if (targetCannon == null)
//                targetCannon = r.gameObject.GetComponentInParent<CannonSlot>();

//            // ── Check for dragon ──────────────────────────────────────────────
//            if (targetDC == null)
//            {
//                var dragon = r.gameObject.GetComponentInParent<DragonController>();
//                if (dragon != null)
//                {
//                    targetDC = dragon;
//                    targetSeat = dragon.GetComponentInChildren<DragonRiderSeat>();
//                }
//            }

//            if (targetCannon != null && targetDC != null) break;
//        }

//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.alpha = 1f;

//        // ── Clear the static drag reference ───────────────────────────────────
//        CurrentlyDragging = null;

//        // ── Cannon slot drop (NEW) ────────────────────────────────────────────
//        if (targetCannon != null)
//        {
//            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
//            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
//            ShowOwnVisuals();
//            _controller?.SetPatrolling(false);
//            return;
//        }

//        // ── Dragon drop (original logic) ──────────────────────────────────────
//        bool seatFree = targetSeat == null || !targetSeat.IsOccupied;

//        if (targetDC != null && targetSeat != null && seatFree)
//        {
//            _mountHomeParent = _homeParent;
//            _mountHomePos = _homeAnchoredPosition;
//            targetDC.PerformMount(this, targetSeat);
//        }
//        else if (targetSeat != null && targetSeat.IsOccupied)
//        {
//            SoldierDragDrop currentRider = targetSeat.GetComponentInChildren<SoldierDragDrop>();

//            if (currentRider != null && currentRider._isLocked)
//            {
//                Debug.Log("[SoldierDragDrop] Swap blocked — current rider is Attached.");
//                SnapBack();
//            }
//            else if (currentRider != null)
//            {
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
//    /// Called by a drop target after accepting the soldier.
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
//    // CANNON SLOT MOUNT / RELEASE  (NEW)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by CannonSlot.AssignSoldier() — reparents the soldier to the
//    /// cannon's SoldierSpawnpoint and records the new home.
//    ///
//    /// Safe to call whether the soldier was at a dragon, another cannon slot,
//    /// or the ground spawn area.
//    /// </summary>
//    public void PlaceAtCannonSlot(CannonSlot slot, Transform spawnpoint)
//    {
//        if (slot == null || spawnpoint == null) return;

//        // Release any existing dragon seat first
//        if (_currentSeat != null)
//        {
//            var dc = _currentSeat.GetComponentInParent<DragonController>();
//            _currentSeat.ReleaseSoldier();
//            _currentSeat = null;
//            dc?.PerformDismount();
//        }

//        // Release previous cannon slot without notifying (we're already moving)
//        if (_currentCannonSlot != null && _currentCannonSlot != slot)
//            _currentCannonSlot.ReleaseSoldier(notify: false);

//        _currentCannonSlot = slot;

//        // Reparent to the cannon's SoldierSpawnpoint
//        transform.SetParent(spawnpoint, worldPositionStays: false);
//        _rect.anchoredPosition = Vector2.zero;
//        _rect.localScale = Vector3.one;

//        // Record this position as home so SnapBack() returns here
//        RecordHome();

//        // Restore visuals and stop patrol
//        ShowOwnVisuals();
//        _animator?.SetState(AnimationState.Idle);
//        _controller?.ExitRidingState();

//        Debug.Log($"[SoldierDragDrop] '{name}' placed at cannon slot '{slot.name}'.");
//    }

//    /// <summary>
//    /// Called by CannonSlot when the block is destroyed or the soldier is removed.
//    /// Snaps the soldier back to their original home position.
//    /// </summary>
//    public void RemoveFromCannonSlot()
//    {
//        if (_currentCannonSlot == null) return;
//        _currentCannonSlot = null;
//        SnapBack();
//        Debug.Log($"[SoldierDragDrop] '{name}' removed from cannon slot.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON MOUNT
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Called by DragonRiderSeat.MountSoldier() when the dragon accepts this soldier.
//    /// </summary>
//    public void MountOnDragon(DragonRiderSeat seat, Vector2 seatOffset)
//    {
//        _currentSeat = seat;

//        EnsureHelmetEquipped();

//        _controller?.EnterRidingState();

//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        HideOwnVisuals();
//        _animator?.SetState(AnimationState.RiderIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on '{seat.transform.parent?.name}'.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // DRAGON DISMOUNT  (programmatic — e.g. Retrieve button)
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Returns the soldier to the ground patrol area and hides the dragon's rider visual.
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

//        transform.SetParent(_mountHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHomeParent = null;

//        riderDragonDC?.PerformDismount();

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted — returned to ground.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // ATTACH LOCK
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Locks or unlocks the soldier to the current dragon seat.
//    /// Called by DragonAttachButton.
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
//            _canvasGroup.blocksRaycasts = false;
//            _canvasGroup.interactable = false;
//        }
//        else
//        {
//            _canvasGroup.blocksRaycasts = true;
//            _canvasGroup.interactable = true;
//        }

//        Debug.Log($"[SoldierDragDrop] '{name}' is now " +
//                  $"{(locked ? "LOCKED (Attached)" : "UNLOCKED")} on dragon seat.");
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // VISUAL SHOW / HIDE
//    // ══════════════════════════════════════════════════════════════════════════

//    private void HideOwnVisuals()
//    {
//        _canvasGroup.alpha = 0f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    private void ShowOwnVisuals()
//    {
//        _canvasGroup.alpha = 1f;
//        _canvasGroup.blocksRaycasts = true;
//        _canvasGroup.interactable = true;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // HELMET AUTO-EQUIP
//    // ══════════════════════════════════════════════════════════════════════════

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

//    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
//    {
//        // Record where the soldier was standing so we can return there on dismount
//        _mountHorseHomeParent = _homeParent;
//        _mountHorseHomePos = _homeAnchoredPosition;

//        _currentHorseSeat = seat;

//        // Make sure there's a helmet (matches existing dragon behaviour)
//        EnsureHelmetEquipped();

//        // Stop patrol / flip before reparenting
//        _controller?.EnterRidingState();

//        // Sit on the horse
//        transform.SetParent(seat.transform, worldPositionStays: false);
//        _rect.anchoredPosition = seatOffset;
//        RecordHome();

//        // Hide this soldier's own canvas layers — HorseController owns the visuals
//        // from here; the soldier is just parented under the seat for transform sync.
//        // We keep alpha=1 so the soldier's own sprites ARE visible while mounted
//        // (unlike dragon mounting where the dragon shows a separate rider visual).
//        // If you want a separate horse-rider visual instead, set alpha=0 here.
//        ShowOwnVisuals();   // soldier's own sprites visible on horseback
//        _animator?.SetState(AnimationState.HorseIdle);

//        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
//    }

//    public void DismountFromHorse()
//    {
//        if (_currentHorseSeat != null)
//        {
//            _currentHorseSeat.ReleaseSoldier();
//            _currentHorseSeat = null;
//        }

//        if (_mountHorseHomeParent == null)
//        {
//            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded — snapping to current home.");
//            _animator?.SetState(AnimationState.Idle);
//            _controller?.ExitRidingState();
//            SnapBack();
//            return;
//        }

//        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
//        _rect.anchoredPosition = _mountHorseHomePos;

//        _controller?.ExitRidingState();
//        _animator?.SetState(AnimationState.Idle);
//        ShowOwnVisuals();

//        RecordHome();
//        _mountHorseHomeParent = null;

//        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
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
///               top of all panels. If riding a dragon, horse, or stationed
///               at a cannon slot, releases the position first.
///
///  OnDrag       Moves the soldier under the pointer.
///
///  OnEndDrag    Raycasts under the pointer:
///                 → HorseController with free seat  → PerformMount()
///                 → DragonController with free seat → PerformMount()
///                 → CannonSlot                      → PlaceAtCannonSlot()
///                 → Occupied seat / empty space     → SnapBack
///
/// ════════════════════════════════════════════════════════════════════
///  HORSE MOUNT
/// ════════════════════════════════════════════════════════════════════
///
///  Drop a soldier onto a Horse prefab.
///  HorseController.OnDrop → PerformMount(soldier) →
///  HorseSeat.MountSoldier → SoldierDragDrop.MountOnHorse.
///
///  The soldier's own CanvasGroup is hidden (alpha = 0).
///  HorseRiderVisual drives the 4 body-part Images (Face/Armor/Helmet/Weapon)
///  on the SoldierSeat child using the soldier's equipped EquipmentItems.
///
///  Drag the soldier off the horse → OnBeginDrag detects _currentHorseSeat
///  → calls HorseController.PerformDismount() → soldier returns home.
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
             "Used to auto-equip a helmet when a helmetless soldier mounts a dragon or horse.")]
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

    /// <summary>Dragon seat this soldier is currently riding on. Null = not on a dragon.</summary>
    private DragonRiderSeat _currentSeat;

    /// <summary>
    /// Ground parent recorded before dragon mounting so DismountFromDragon() can
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

    // ── Cannon Slot State ─────────────────────────────────────────────────────

    /// <summary>
    /// Set to this instance in OnBeginDrag, cleared in OnEndDrag.
    /// CannonSlot.OnDrop reads this to know which soldier is being dragged.
    /// </summary>
    public static SoldierDragDrop CurrentlyDragging { get; private set; }

    /// <summary>The cannon slot this soldier is currently stationed at. Null = not at a cannon.</summary>
    private CannonSlot _currentCannonSlot;

    /// <summary>True while this soldier is stationed at a cannon slot.</summary>
    public bool IsAtCannon => _currentCannonSlot != null;

    // ── Horse Rider State ─────────────────────────────────────────────────────

    /// <summary>The HorseSeat this soldier is currently riding on. Null = not on a horse.</summary>
    private HorseSeat _currentHorseSeat;

    /// <summary>Parent transform recorded before horse mounting — used to return the soldier after dismount.</summary>
    private Transform _mountHorseHomeParent;

    /// <summary>AnchoredPosition recorded before horse mounting — used to return the soldier after dismount.</summary>
    private Vector2 _mountHorseHomePos;

    /// <summary>True while this soldier is seated on a horse.</summary>
    public bool IsOnHorse => _currentHorseSeat != null;

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

        // ── Release horse seat if stationed there ─────────────────────────────
        bool wasOnHorse = _currentHorseSeat != null;
        HorseController mountedHorseHC = null;

        if (wasOnHorse)
        {
            mountedHorseHC = _currentHorseSeat.GetComponentInParent<HorseController>();
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
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

        // Record home, then override with pre-mount ground position if dismounting.
        RecordHome();

        if (wasMounted && _mountHomeParent != null)
        {
            _homeParent = _mountHomeParent;
            _homeAnchoredPosition = _mountHomePos;
            _mountHomeParent = null;
        }

        if (wasOnHorse && _mountHorseHomeParent != null)
        {
            _homeParent = _mountHorseHomeParent;
            _homeAnchoredPosition = _mountHorseHomePos;
            _mountHorseHomeParent = null;
        }

        _isDragging = true;
        _controller?.SetPatrolling(false);

        // Restore own visuals before reparenting (in case coming from dragon or horse).
        if (wasMounted || wasOnHorse)
            ShowOwnVisuals();

        // Reparent to root canvas so the soldier draws above all panels.
        transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha = 0.75f;
        _canvasGroup.blocksRaycasts = false;

        // Notify dragon to hide its rider visual.
        if (wasMounted)
            mountedDragonDC?.PerformDismount();

        // Notify horse to hide rider layers and reset to Idle.
        if (wasOnHorse)
            mountedHorseHC?.PerformDismount();
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
        CannonSlot targetCannon = null;
        HorseController targetHorse = null;

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

            // ── Check for horse ───────────────────────────────────────────────
            if (targetHorse == null)
            {
                var hc = r.gameObject.GetComponentInParent<HorseController>();
                if (hc != null && !hc.IsOccupied)
                    targetHorse = hc;
            }

            if (targetCannon != null && targetDC != null && targetHorse != null) break;
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // ── Clear the static drag reference ───────────────────────────────────
        CurrentlyDragging = null;

        // ── Cannon slot drop ──────────────────────────────────────────────────
        if (targetCannon != null)
        {
            // CannonSlot.OnDrop already fired via Unity's IDropHandler,
            // so PlaceAtCannonSlot was called there. Just ensure visuals are right.
            ShowOwnVisuals();
            _controller?.SetPatrolling(false);
            return;
        }

        // ── Horse drop ────────────────────────────────────────────────────────
        if (targetHorse != null)
        {
            _mountHorseHomeParent = _homeParent;
            _mountHorseHomePos = _homeAnchoredPosition;
            targetHorse.PerformMount(this);
            return;
        }

        // ── Dragon drop ───────────────────────────────────────────────────────
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
    // CANNON SLOT MOUNT / RELEASE
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

        // Release any existing horse seat first
        if (_currentHorseSeat != null)
        {
            var hc = _currentHorseSeat.GetComponentInParent<HorseController>();
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
            hc?.PerformDismount();
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

    // ══════════════════════════════════════════════════════════════════════════
    // HORSE MOUNT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by HorseSeat.MountSoldier() when the horse accepts this soldier.
    ///
    /// The soldier is reparented under the seat so it moves with the horse.
    /// Its own CanvasGroup is hidden (alpha = 0) — HorseRiderVisual drives
    /// the 4 body-part Images (Face/Armor/Helmet/Weapon) on the seat instead.
    /// </summary>
    public void MountOnHorse(HorseSeat seat, Vector2 seatOffset)
    {
        // Record where the soldier was standing so we can return there on dismount
        _mountHorseHomeParent = _homeParent;
        _mountHorseHomePos = _homeAnchoredPosition;

        _currentHorseSeat = seat;

        // Auto-equip a helmet if the soldier has none (mirrors dragon behaviour)
        EnsureHelmetEquipped();

        // Stop patrol / flip animations before reparenting
        _controller?.EnterRidingState();

        // Reparent under the seat so the soldier moves with the horse
        transform.SetParent(seat.transform, worldPositionStays: false);
        _rect.anchoredPosition = seatOffset;
        RecordHome();

        // Hide the soldier's own canvas — HorseRiderVisual owns the visual display.
        // The CanvasGroup is still blocking raycasts so the soldier can be dragged off.
        HideOwnVisuals();

        // Tell the SpriteLayerAnimator the soldier is now in HorseIdle
        // (HorseController.SetState will also call this after mount, but setting
        //  it here removes any single-frame flash on mount).
        _animator?.SetState(AnimationState.HorseIdle);

        Debug.Log($"[SoldierDragDrop] '{name}' mounted on horse seat '{seat.name}'.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HORSE DISMOUNT  (programmatic — e.g. Dismount button or drag-off)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the soldier to the ground and restores its own visuals.
    /// Called by HorseController.PerformDismount() or drag-off via OnBeginDrag.
    /// </summary>
    public void DismountFromHorse()
    {
        if (_currentHorseSeat != null)
        {
            _currentHorseSeat.ReleaseSoldier();
            _currentHorseSeat = null;
        }

        if (_mountHorseHomeParent == null)
        {
            Debug.LogWarning("[SoldierDragDrop] DismountFromHorse: no mount home recorded " +
                             "— snapping to current home.");
            _animator?.SetState(AnimationState.Idle);
            _controller?.ExitRidingState();
            ShowOwnVisuals();
            SnapBack();
            return;
        }

        transform.SetParent(_mountHorseHomeParent, worldPositionStays: false);
        _rect.anchoredPosition = _mountHorseHomePos;

        _controller?.ExitRidingState();
        _animator?.SetState(AnimationState.Idle);
        ShowOwnVisuals();

        RecordHome();
        _mountHorseHomeParent = null;

        Debug.Log($"[SoldierDragDrop] '{name}' dismounted from horse — returned to ground.");
    }
}