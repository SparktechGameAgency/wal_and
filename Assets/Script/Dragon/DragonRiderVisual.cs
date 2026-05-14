////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// DRAGON RIDER VISUAL
///////
/////// Attach to a child of RiderSeat named "DragonRiderVisual".
/////// This GameObject holds one Image per equipment slot and is hidden
/////// by default. When a soldier mounts, DragonController calls
/////// ShowForSoldier() which reads the soldier's CharacterEquipment and
/////// sets every layer to that item's ridingSprites[0] (idle as fallback).
/////// When the soldier dismounts, DragonController calls Hide().
///////
/////// ════════════════════════════════════════════════════════════════════
///////  PREFAB HIERARCHY  (single dragon prefab — no swap needed)
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Dragon (root)              ← DragonController, CanvasGroup, DragonLayeredVisual
///////   ├── DragonBody [0]         ← Image: dragon body
///////   ├── RiderSeat  [1]         ← DragonRiderSeat
///////   │   └── DragonRiderVisual  ← THIS script  (hidden by default)
///////   │       ├── BodyLayer      ← Image for body / armor
///////   │       ├── FaceLayer      ← Image for face
///////   │       ├── HairLayer      ← Image for hair (hidden when helmet is equipped)
///////   │       ├── HelmetLayer    ← Image for helmet
///////   │       └── WeaponLayer    ← Image for weapon
///////   └── DragonWing [2]         ← Image: front wing (renders on top)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  INSPECTOR SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Add a child called "DragonRiderVisual" inside RiderSeat.
///////  2. Add child Images for each layer (BodyLayer, HelmetLayer, …).
///////  3. Attach this script to DragonRiderVisual and drag each Image
///////     into the matching Inspector field.
///////  4. Leave the GameObject active in the prefab — visibility is
///////     controlled by CanvasGroup.alpha, NOT SetActive.
///////     (SetActive stops the CanvasGroup from being found in Awake.)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SPRITE FALLBACK CHAIN  (per equipment slot)
/////// ════════════════════════════════════════════════════════════════════
///////
///////   1. Item.ridingSprites[0]   — preferred: explicit riding frame
///////   2. Item.idleSprites[0]     — fallback:  idle frame while seated
///////   3. Layer disabled          — item slot is empty or has no sprites
/////// </summary>
////public class DragonRiderVisual : MonoBehaviour
////{
////    // ── Inspector — Image layers ──────────────────────────────────────────────

////    [Header("Equipment Image Layers (drag child Images here)")]
////    [Tooltip("Image that shows the body or armor sprite while riding.")]
////    [SerializeField] private Image bodyLayer;

////    [Tooltip("Image that shows the face sprite while riding.")]
////    [SerializeField] private Image faceLayer;

////    [Tooltip("Image that shows the hair sprite while riding. " +
////             "Automatically hidden when a helmet is equipped.")]
////    [SerializeField] private Image hairLayer;

////    [Tooltip("Image that shows the helmet sprite while riding.")]
////    [SerializeField] private Image helmetLayer;

////    [Tooltip("Image that shows the weapon sprite while riding. " +
////             "Leave blank to omit the weapon layer.")]
////    [SerializeField] private Image weaponLayer;

////    // ── Private ───────────────────────────────────────────────────────────────

////    private CanvasGroup _cg;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _cg = GetComponent<CanvasGroup>();
////        if (_cg == null)
////            _cg = gameObject.AddComponent<CanvasGroup>();

////        // Start fully hidden; does NOT block raycasts so the dragon seat
////        // underneath remains reachable by the EventSystem.
////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;
////    }

////    // ── Public API — called by DragonController ───────────────────────────────

////    /// <summary>
////    /// Reads the soldier's equipped items and displays their riding sprites
////    /// (falling back to idle if no riding sprites are assigned).
////    ///
////    /// Call this from DragonController.PerformMount() AFTER the soldier
////    /// has been reparented to the seat.
////    /// </summary>
////    public void ShowForSoldier(CharacterEquipment equipment)
////    {
////        if (equipment == null)
////        {
////            Debug.LogWarning("[DragonRiderVisual] ShowForSoldier: equipment is null — " +
////                             "rider visual will remain hidden.", this);
////            return;
////        }

////        BodyType bodyType = equipment.CurrentBodyType;

////        // ── Armor / Body ──────────────────────────────────────────────────────
////        // Prefer Armor over BodyType so armored soldiers show armor while riding.
////        var armor = equipment.GetEquipped(EquipmentSlot.Armor);
////        var body = equipment.GetEquipped(EquipmentSlot.BodyType);
////        ApplyLayer(bodyLayer, armor ?? body, bodyType);

////        // ── Face ──────────────────────────────────────────────────────────────
////        ApplyLayer(faceLayer, equipment.GetEquipped(EquipmentSlot.Face), bodyType);

////        // ── Helmet & Hair ─────────────────────────────────────────────────────
////        var helmet = equipment.GetEquipped(EquipmentSlot.Helmet);
////        ApplyLayer(helmetLayer, helmet, bodyType);

////        // Hair is hidden when a helmet is equipped (helmet covers the hair).
////        var hair = equipment.GetEquipped(EquipmentSlot.Hair);
////        if (hairLayer != null)
////        {
////            if (helmet != null)
////            {
////                hairLayer.enabled = false;   // helmet is on → hide hair
////            }
////            else
////            {
////                ApplyLayer(hairLayer, hair, bodyType);
////            }
////        }

////        // ── Weapon ────────────────────────────────────────────────────────────
////        ApplyLayer(weaponLayer, equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);

////        // ── Make visible ──────────────────────────────────────────────────────
////        _cg.alpha = 1f;
////        _cg.blocksRaycasts = false;   // never block — seat must stay raycastable
////        _cg.interactable = false;

////        Debug.Log("[DragonRiderVisual] Rider visual shown " +
////                  $"(armor: {armor?.itemName ?? "none"}, " +
////                  $"helmet: {helmet?.itemName ?? "none"}).");
////    }

////    /// <summary>
////    /// Hides the rider visual. Call from DragonController.PerformDismount()
////    /// after the soldier has been reparented away from the seat.
////    /// </summary>
////    public void Hide()
////    {
////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;

////        // Clear all layer sprites so no stale frame bleeds through on
////        // the next ShowForSoldier() call before sprites are reassigned.
////        ClearLayer(bodyLayer);
////        ClearLayer(faceLayer);
////        ClearLayer(hairLayer);
////        ClearLayer(helmetLayer);
////        ClearLayer(weaponLayer);

////        Debug.Log("[DragonRiderVisual] Rider visual hidden.");
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Sets <paramref name="image"/> to the first riding sprite (or idle fallback)
////    /// for <paramref name="item"/>. Disables the Image if the item is null or
////    /// has no usable sprites.
////    /// </summary>
////    private void ApplyLayer(Image image, EquipmentItem item, BodyType bodyType)
////    {
////        if (image == null) return;

////        if (item == null)
////        {
////            image.enabled = false;
////            return;
////        }

////        // Riding sprites take priority; idle is the graceful fallback.
////        Sprite[] sprites = item.GetSprites(AnimationState.Riding, bodyType);

////        if (sprites == null || sprites.Length == 0)
////            sprites = item.GetSprites(AnimationState.Idle, bodyType);

////        if (sprites != null && sprites.Length > 0 && sprites[0] != null)
////        {
////            image.sprite = sprites[0];
////            image.enabled = true;
////        }
////        else
////        {
////            image.enabled = false;
////        }
////    }

////    /// <summary>Disables the layer and clears its sprite reference.</summary>
////    private void ClearLayer(Image image)
////    {
////        if (image == null) return;
////        image.sprite = null;
////        image.enabled = false;
////    }
////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// DRAGON RIDER VISUAL
///////
/////// Attach to a child of RiderSeat named "DragonRiderVisual".
/////// This GameObject holds one Image per equipment slot and is hidden
/////// by default. When a soldier mounts, DragonController calls
/////// ShowForSoldier() which reads the soldier's CharacterEquipment and
/////// sets every layer to that item's ridingSprites[0] (idle as fallback).
/////// When the soldier dismounts, DragonController calls Hide().
///////
/////// ════════════════════════════════════════════════════════════════════
///////  PREFAB HIERARCHY  (single dragon prefab — no swap needed)
/////// ════════════════════════════════════════════════════════════════════
///////
///////   Dragon (root)              ← DragonController, CanvasGroup, DragonLayeredVisual
///////   ├── DragonBody [0]         ← Image: dragon body
///////   ├── RiderSeat  [1]         ← DragonRiderSeat
///////   │   └── DragonRiderVisual  ← THIS script  (hidden by default)
///////   │       ├── BodyLayer      ← Image for body / armor
///////   │       ├── FaceLayer      ← Image for face
///////   │       ├── HairLayer      ← Image for hair (hidden when helmet is equipped)
///////   │       ├── HelmetLayer    ← Image for helmet
///////   │       └── WeaponLayer    ← Image for weapon
///////   └── DragonWing [2]         ← Image: front wing (renders on top)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  INSPECTOR SETUP
/////// ════════════════════════════════════════════════════════════════════
///////
///////  1. Add a child called "DragonRiderVisual" inside RiderSeat.
///////  2. Add child Images for each layer (BodyLayer, HelmetLayer, …).
///////  3. Attach this script to DragonRiderVisual and drag each Image
///////     into the matching Inspector field.
///////  4. Leave the GameObject active in the prefab — visibility is
///////     controlled by CanvasGroup.alpha, NOT SetActive.
///////     (SetActive stops the CanvasGroup from being found in Awake.)
///////
/////// ════════════════════════════════════════════════════════════════════
///////  SPRITE FALLBACK CHAIN  (per equipment slot)
/////// ════════════════════════════════════════════════════════════════════
///////
///////   1. Item.ridingSprites[0]   — preferred: explicit riding frame
///////   2. Item.idleSprites[0]     — fallback:  idle frame while seated
///////   3. Layer disabled          — item slot is empty or has no sprites
/////// </summary>
////public class DragonRiderVisual : MonoBehaviour
////{
////    // ── Inspector — Image layers ──────────────────────────────────────────────

////    [Header("Equipment Image Layers (drag child Images here)")]
////    [Tooltip("Image that shows the body or armor sprite while riding.")]
////    [SerializeField] private Image bodyLayer;

////    [Tooltip("Image that shows the face sprite while riding.")]
////    [SerializeField] private Image faceLayer;

////    [Tooltip("Image that shows the hair sprite while riding. " +
////             "Automatically hidden when a helmet is equipped.")]
////    [SerializeField] private Image hairLayer;

////    [Tooltip("Image that shows the helmet sprite while riding.")]
////    [SerializeField] private Image helmetLayer;

////    [Tooltip("Image that shows the weapon sprite while riding. " +
////             "Leave blank to omit the weapon layer.")]
////    [SerializeField] private Image weaponLayer;

////    // ── Private ───────────────────────────────────────────────────────────────

////    private CanvasGroup _cg;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _cg = GetComponent<CanvasGroup>();
////        if (_cg == null)
////            _cg = gameObject.AddComponent<CanvasGroup>();

////        // Start fully hidden; does NOT block raycasts so the dragon seat
////        // underneath remains reachable by the EventSystem.
////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;
////    }

////    // ── Public API — called by DragonController ───────────────────────────────

////    /// <summary>
////    /// Reads the soldier's equipped items and displays their riding sprites
////    /// (falling back to idle if no riding sprites are assigned).
////    ///
////    /// Call this from DragonController.PerformMount() AFTER the soldier
////    /// has been reparented to the seat.
////    /// </summary>
////    public void ShowForSoldier(CharacterEquipment equipment)
////    {
////        if (equipment == null)
////        {
////            Debug.LogWarning("[DragonRiderVisual] ShowForSoldier: equipment is null — " +
////                             "rider visual will remain hidden.", this);
////            return;
////        }

////        BodyType bodyType = equipment.CurrentBodyType;

////        // ── Armor / Body ──────────────────────────────────────────────────────
////        // Prefer Armor over BodyType so armored soldiers show armor while riding.
////        var armor = equipment.GetEquipped(EquipmentSlot.Armor);
////        var body = equipment.GetEquipped(EquipmentSlot.BodyType);
////        ApplyLayer(bodyLayer, armor ?? body, bodyType);

////        // ── Face ──────────────────────────────────────────────────────────────
////        ApplyLayer(faceLayer, equipment.GetEquipped(EquipmentSlot.Face), bodyType);

////        // ── Helmet & Hair ─────────────────────────────────────────────────────
////        var helmet = equipment.GetEquipped(EquipmentSlot.Helmet);
////        ApplyLayer(helmetLayer, helmet, bodyType);

////        // Hair is hidden when a helmet is equipped (helmet covers the hair).
////        var hair = equipment.GetEquipped(EquipmentSlot.Hair);
////        if (hairLayer != null)
////        {
////            if (helmet != null)
////            {
////                hairLayer.enabled = false;   // helmet is on → hide hair
////            }
////            else
////            {
////                ApplyLayer(hairLayer, hair, bodyType);
////            }
////        }

////        // ── Weapon ────────────────────────────────────────────────────────────
////        ApplyLayer(weaponLayer, equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);

////        // ── Make visible ──────────────────────────────────────────────────────
////        _cg.alpha = 1f;
////        _cg.blocksRaycasts = false;   // never block — seat must stay raycastable
////        _cg.interactable = false;

////        Debug.Log("[DragonRiderVisual] Rider visual shown " +
////                  $"(armor: {armor?.itemName ?? "none"}, " +
////                  $"helmet: {helmet?.itemName ?? "none"}).");
////    }

////    /// <summary>
////    /// Hides the rider visual. Call from DragonController.PerformDismount()
////    /// after the soldier has been reparented away from the seat.
////    /// </summary>
////    public void Hide()
////    {
////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;

////        // Clear all layer sprites so no stale frame bleeds through on
////        // the next ShowForSoldier() call before sprites are reassigned.
////        ClearLayer(bodyLayer);
////        ClearLayer(faceLayer);
////        ClearLayer(hairLayer);
////        ClearLayer(helmetLayer);
////        ClearLayer(weaponLayer);

////        Debug.Log("[DragonRiderVisual] Rider visual hidden.");
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Sets <paramref name="image"/> to the first riding sprite (or idle fallback)
////    /// for <paramref name="item"/>. Disables the Image if the item is null or
////    /// has no usable sprites.
////    /// </summary>
////    private void ApplyLayer(Image image, EquipmentItem item, BodyType bodyType)
////    {
////        if (image == null) return;

////        if (item == null)
////        {
////            image.enabled = false;
////            return;
////        }

////        // Riding sprites take priority; idle is the graceful fallback.
////        Sprite[] sprites = item.GetSprites(AnimationState.Riding, bodyType);

////        if (sprites == null || sprites.Length == 0)
////            sprites = item.GetSprites(AnimationState.Idle, bodyType);

////        if (sprites != null && sprites.Length > 0 && sprites[0] != null)
////        {
////            image.sprite = sprites[0];
////            image.enabled = true;
////        }
////        else
////        {
////            image.enabled = false;
////        }
////    }

////    /// <summary>Disables the layer and clears its sprite reference.</summary>
////    private void ClearLayer(Image image)
////    {
////        if (image == null) return;
////        image.sprite = null;
////        image.enabled = false;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// DRAGON RIDER VISUAL
/////
///// Attach to a child of RiderSeat named "DragonRiderVisual".
///// This GameObject holds one Image per equipment slot and is hidden
///// by default. When a soldier mounts, DragonController calls
///// ShowForSoldier() which caches sprite arrays for BOTH rider states
///// (RiderIdle and RiderFly). DragonController then calls SetRiderState()
///// each time the dragon transitions between Idle ↔ Flying so the rider's
///// animation automatically matches the dragon's state.
/////
///// ════════════════════════════════════════════════════════════════════
/////  RIDER ANIMATION STATES
///// ════════════════════════════════════════════════════════════════════
/////
/////  RiderIdle  Dragon is resting in the DragonArea. Rider plays the
/////             riderIdleSprites (EquipmentItem) — the soldier sits still.
/////
/////  RiderFly   Dragon is patrolling a FlyZone. Rider plays the
/////             riderFlySprites (EquipmentItem) — the soldier leans forward.
/////
/////  DragonController calls SetRiderState(AnimationState.RiderIdle) inside
/////  EnterIdle() and SetRiderState(AnimationState.RiderFly) inside EnterFlying().
/////
///// ════════════════════════════════════════════════════════════════════
/////  PREFAB HIERARCHY  (single dragon prefab)
///// ════════════════════════════════════════════════════════════════════
/////
/////   Dragon (root)              ← DragonController, CanvasGroup
/////   ├── DragonBody [0]         ← Image: dragon body
/////   ├── RiderSeat  [1]         ← DragonRiderSeat
/////   │   └── DragonRiderVisual  ← THIS script  (hidden by default)
/////   │       ├── BodyLayer      ← Image for body / armor
/////   │       ├── FaceLayer      ← Image for face
/////   │       ├── HairLayer      ← Image for hair (hidden when helmet equipped)
/////   │       ├── HelmetLayer    ← Image for helmet
/////   │       └── WeaponLayer    ← Image for weapon
/////   └── DragonWing [2]         ← Image: front wing (renders on top)
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Add a child called "DragonRiderVisual" inside RiderSeat.
/////  2. Add child Images for each layer (BodyLayer, HelmetLayer, …).
/////  3. Attach this script to DragonRiderVisual and drag each Image
/////     into the matching Inspector field.
/////  4. Leave the GameObject active in the prefab — visibility is
/////     controlled by CanvasGroup.alpha, NOT SetActive.
/////     (SetActive stops the CanvasGroup from being found in Awake.)
///// </summary>
////public class DragonRiderVisual : MonoBehaviour
////{
////    // ── Inspector — Image layers ──────────────────────────────────────────────

////    [Header("Equipment Image Layers (drag child Images here)")]
////    [Tooltip("Image that shows the body or armor sprite while riding.")]
////    [SerializeField] private Image bodyLayer;

////    [Tooltip("Image that shows the face sprite while riding.")]
////    [SerializeField] private Image faceLayer;

////    [Tooltip("Image that shows the hair sprite while riding. " +
////             "Automatically hidden when a helmet is equipped.")]
////    [SerializeField] private Image hairLayer;

////    [Tooltip("Image that shows the helmet sprite while riding.")]
////    [SerializeField] private Image helmetLayer;

////    [Tooltip("Image that shows the weapon sprite while riding. " +
////             "Leave blank to omit the weapon layer.")]
////    [SerializeField] private Image weaponLayer;

////    [Header("Animation")]
////    [Tooltip("Frames per second for the riding animation. " +
////             "All layers advance together so every sprite stays in sync.")]
////    [Min(1f)]
////    [SerializeField] private float ridingFps = 8f;

////    // ── Private — cached sprite arrays for BOTH states ────────────────────────

////    // RiderIdle caches (dragon is resting)
////    private Sprite[] _bodyIdleSprites;
////    private Sprite[] _faceIdleSprites;
////    private Sprite[] _hairIdleSprites;
////    private Sprite[] _helmetIdleSprites;
////    private Sprite[] _weaponIdleSprites;

////    // RiderFly caches (dragon is patrolling)
////    private Sprite[] _bodyFlySprites;
////    private Sprite[] _faceFlySprites;
////    private Sprite[] _hairFlySprites;
////    private Sprite[] _helmetFlySprites;
////    private Sprite[] _weaponFlySprites;

////    // ── Private — active arrays (pointer to whichever state is current) ───────

////    private Sprite[] _bodySprites;
////    private Sprite[] _faceSprites;
////    private Sprite[] _hairSprites;
////    private Sprite[] _helmetSprites;
////    private Sprite[] _weaponSprites;

////    // ── Private — animation state ─────────────────────────────────────────────

////    private CanvasGroup _cg;
////    private bool _riding;   // true only while a soldier is mounted
////    private float _timer;
////    private int _frame;
////    private AnimationState _riderState = AnimationState.RiderIdle;

////    // ── Lifecycle ─────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _cg = GetComponent<CanvasGroup>();
////        if (_cg == null)
////            _cg = gameObject.AddComponent<CanvasGroup>();

////        // Start fully hidden; does NOT block raycasts so the dragon seat
////        // underneath remains reachable by the EventSystem.
////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;
////    }

////    private void Update()
////    {
////        if (!_riding) return;

////        _timer += Time.deltaTime;
////        if (_timer < 1f / ridingFps) return;

////        _timer = 0f;
////        _frame++;
////        ApplyFrame();
////    }

////    // ── Public API — called by DragonController ───────────────────────────────

////    /// <summary>
////    /// Reads the soldier's equipped items, caches sprite arrays for BOTH rider
////    /// states (RiderIdle and RiderFly), then shows frame 0 in RiderIdle by
////    /// default. DragonController calls SetRiderState() immediately after to
////    /// sync the rider to the dragon's current state.
////    ///
////    /// Call this from DragonController.PerformMount() AFTER the soldier
////    /// has been reparented to the seat.
////    /// </summary>
////    public void ShowForSoldier(CharacterEquipment equipment)
////    {
////        if (equipment == null)
////        {
////            Debug.LogWarning("[DragonRiderVisual] ShowForSoldier: equipment is null — " +
////                             "rider visual will remain hidden.", this);
////            return;
////        }

////        BodyType bodyType = equipment.CurrentBodyType;

////        var armor = equipment.GetEquipped(EquipmentSlot.Armor);
////        var body = equipment.GetEquipped(EquipmentSlot.BodyType);
////        var helmet = equipment.GetEquipped(EquipmentSlot.Helmet);
////        var hair = equipment.GetEquipped(EquipmentSlot.Hair);

////        // ── Cache RiderIdle arrays ────────────────────────────────────────────
////        _bodyIdleSprites = GetStateSprites(armor ?? body, AnimationState.RiderIdle, bodyType);
////        _faceIdleSprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Face), AnimationState.RiderIdle, bodyType);
////        _helmetIdleSprites = GetStateSprites(helmet, AnimationState.RiderIdle, bodyType);
////        _weaponIdleSprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Weapon), AnimationState.RiderIdle, bodyType);
////        _hairIdleSprites = helmet != null ? null
////                           : GetStateSprites(hair, AnimationState.RiderIdle, bodyType);

////        // ── Cache RiderFly arrays ─────────────────────────────────────────────
////        _bodyFlySprites = GetStateSprites(armor ?? body, AnimationState.RiderFly, bodyType);
////        _faceFlySprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Face), AnimationState.RiderFly, bodyType);
////        _helmetFlySprites = GetStateSprites(helmet, AnimationState.RiderFly, bodyType);
////        _weaponFlySprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Weapon), AnimationState.RiderFly, bodyType);
////        _hairFlySprites = helmet != null ? null
////                           : GetStateSprites(hair, AnimationState.RiderFly, bodyType);

////        // ── Start in RiderIdle — DragonController syncs to current dragon state ─
////        _riding = true;
////        ApplyRiderState(AnimationState.RiderIdle);

////        // ── Make visible ──────────────────────────────────────────────────────
////        _cg.alpha = 1f;
////        _cg.blocksRaycasts = false;   // never block — seat must stay raycastable
////        _cg.interactable = false;

////        Debug.Log("[DragonRiderVisual] Rider visual shown " +
////                  $"(armor: {(armor ?? body)?.itemName ?? "none"}, " +
////                  $"helmet: {helmet?.itemName ?? "none"}).");
////    }

////    /// <summary>
////    /// Switches the active riding animation to match the dragon's current state.
////    ///
////    /// Call from DragonController.EnterIdle()   with AnimationState.RiderIdle.
////    /// Call from DragonController.EnterFlying() with AnimationState.RiderFly.
////    ///
////    /// Safe to call when no soldier is mounted — does nothing in that case.
////    /// Resets to frame 0 so the new animation always starts cleanly.
////    /// </summary>
////    public void SetRiderState(AnimationState state)
////    {
////        if (!_riding) return;
////        if (state != AnimationState.RiderIdle && state != AnimationState.RiderFly) return;
////        if (_riderState == state) return;   // already in this state, skip the reset

////        ApplyRiderState(state);
////        Debug.Log($"[DragonRiderVisual] Rider state → {state}");
////    }

////    /// <summary>
////    /// Hides the rider visual and stops the animation.
////    /// Call from DragonController.PerformDismount() after the soldier
////    /// has been reparented away from the seat.
////    /// </summary>
////    public void Hide()
////    {
////        _riding = false;
////        _frame = 0;
////        _timer = 0f;

////        _cg.alpha = 0f;
////        _cg.blocksRaycasts = false;
////        _cg.interactable = false;

////        // Clear display layers.
////        ClearLayer(bodyLayer);
////        ClearLayer(faceLayer);
////        ClearLayer(hairLayer);
////        ClearLayer(helmetLayer);
////        ClearLayer(weaponLayer);

////        // Clear all cached arrays so no stale frame bleeds through on the
////        // next ShowForSoldier() call before sprites are reassigned.
////        _bodyIdleSprites = _bodyFlySprites = _bodySprites = null;
////        _faceIdleSprites = _faceFlySprites = _faceSprites = null;
////        _hairIdleSprites = _hairFlySprites = _hairSprites = null;
////        _helmetIdleSprites = _helmetFlySprites = _helmetSprites = null;
////        _weaponIdleSprites = _weaponFlySprites = _weaponSprites = null;

////        Debug.Log("[DragonRiderVisual] Rider visual hidden.");
////    }

////    // ── Internal helpers ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Switches the active sprite arrays to the given rider state and resets
////    /// the frame counter so the new animation starts cleanly from frame 0.
////    /// </summary>
////    private void ApplyRiderState(AnimationState state)
////    {
////        _riderState = state;
////        _frame = 0;
////        _timer = 0f;

////        if (state == AnimationState.RiderFly)
////        {
////            _bodySprites = _bodyFlySprites;
////            _faceSprites = _faceFlySprites;
////            _hairSprites = _hairFlySprites;
////            _helmetSprites = _helmetFlySprites;
////            _weaponSprites = _weaponFlySprites;
////        }
////        else   // RiderIdle (default)
////        {
////            _bodySprites = _bodyIdleSprites;
////            _faceSprites = _faceIdleSprites;
////            _hairSprites = _hairIdleSprites;
////            _helmetSprites = _helmetIdleSprites;
////            _weaponSprites = _weaponIdleSprites;
////        }

////        ApplyFrame();
////    }

////    /// <summary>
////    /// Pushes the current _frame to every active image layer.
////    /// Each layer wraps its frame index against its own sprite count so
////    /// layers with different array lengths still loop correctly.
////    /// </summary>
////    private void ApplyFrame()
////    {
////        SetLayerFrame(bodyLayer, _bodySprites);
////        SetLayerFrame(faceLayer, _faceSprites);
////        SetLayerFrame(hairLayer, _hairSprites);
////        SetLayerFrame(helmetLayer, _helmetSprites);
////        SetLayerFrame(weaponLayer, _weaponSprites);
////    }

////    /// <summary>
////    /// Sets <paramref name="image"/> to frame <see cref="_frame"/> of
////    /// <paramref name="sprites"/>, wrapping with modulo.
////    /// Disables the image if the array is null or empty.
////    /// </summary>
////    private void SetLayerFrame(Image image, Sprite[] sprites)
////    {
////        if (image == null) return;

////        if (sprites == null || sprites.Length == 0)
////        {
////            image.enabled = false;
////            return;
////        }

////        image.sprite = sprites[_frame % sprites.Length];
////        image.enabled = true;
////    }

////    /// <summary>
////    /// Returns the sprites for <paramref name="item"/> at the requested
////    /// <paramref name="state"/>. EquipmentItem.GetSprites() handles the full
////    /// fallback chain (RiderFly → RiderIdle → Idle).
////    /// Returns null if the item is null or has no usable sprites at all.
////    /// </summary>
////    private Sprite[] GetStateSprites(EquipmentItem item, AnimationState state, BodyType bodyType)
////    {
////        if (item == null) return null;

////        Sprite[] sprites = item.GetSprites(state, bodyType);
////        return (sprites != null && sprites.Length > 0) ? sprites : null;
////    }

////    /// <summary>Disables the layer and clears its sprite reference.</summary>
////    private void ClearLayer(Image image)
////    {
////        if (image == null) return;
////        image.sprite = null;
////        image.enabled = false;
////    }
////}
/////
///// ════════════════════════════════════════════════════════════════════
/////  PREFAB HIERARCHY  (single dragon prefab — no swap needed)
///// ════════════════════════════════════════════════════════════════════
/////
/////   Dragon (root)              ← DragonController, CanvasGroup, DragonLayeredVisual
/////   ├── DragonBody [0]         ← Image: dragon body
/////   ├── RiderSeat  [1]         ← DragonRiderSeat
/////   │   └── DragonRiderVisual  ← THIS script  (hidden by default)
/////   │       ├── BodyLayer      ← Image for body / armor
/////   │       ├── FaceLayer      ← Image for face
/////   │       ├── HairLayer      ← Image for hair (hidden when helmet is equipped)
/////   │       ├── HelmetLayer    ← Image for helmet
/////   │       └── WeaponLayer    ← Image for weapon
/////   └── DragonWing [2]         ← Image: front wing (renders on top)
/////
///// ════════════════════════════════════════════════════════════════════
/////  INSPECTOR SETUP
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Add a child called "DragonRiderVisual" inside RiderSeat.
/////  2. Add child Images for each layer (BodyLayer, HelmetLayer, …).
/////  3. Attach this script to DragonRiderVisual and drag each Image
/////     into the matching Inspector field.
/////  4. Leave the GameObject active in the prefab — visibility is
/////     controlled by CanvasGroup.alpha, NOT SetActive.
/////     (SetActive stops the CanvasGroup from being found in Awake.)
/////
///// ════════════════════════════════════════════════════════════════════
/////  SPRITE FALLBACK CHAIN  (per equipment slot)
///// ════════════════════════════════════════════════════════════════════
/////
/////   1. Item.ridingSprites[]   — preferred: explicit riding frames (animated)
/////   2. Item.idleSprites[]     — fallback:  idle frames (animated)
/////   3. Layer disabled          — item slot is empty or has no sprites
/////
///// ════════════════════════════════════════════════════════════════════
/////  ANIMATION
///// ════════════════════════════════════════════════════════════════════
/////
/////  All layers share a single frame counter so every equipment piece
/////  stays perfectly in sync (body, face, hair, helmet, weapon all
/////  advance together at ridingFps).
/////
/////  The animation only runs while a soldier is mounted (_riding = true).
/////  When Hide() is called the counter resets to 0 so the next mount
/////  always starts cleanly from frame 0.
///// </summary>
//public class DragonRiderVisual : MonoBehaviour
//{
//    // ── Inspector — Image layers ──────────────────────────────────────────────

//    [Header("Equipment Image Layers (drag child Images here)")]
//    [Tooltip("Image that shows the body or armor sprite while riding.")]
//    [SerializeField] private Image bodyLayer;

//    [Tooltip("Image that shows the face sprite while riding.")]
//    [SerializeField] private Image faceLayer;

//    [Tooltip("Image that shows the hair sprite while riding. " +
//             "Automatically hidden when a helmet is equipped.")]
//    [SerializeField] private Image hairLayer;

//    [Tooltip("Image that shows the helmet sprite while riding.")]
//    [SerializeField] private Image helmetLayer;

//    [Tooltip("Image that shows the weapon sprite while riding. " +
//             "Leave blank to omit the weapon layer.")]
//    [SerializeField] private Image weaponLayer;

//    [Header("Animation")]
//    [Tooltip("Frames per second for the riding animation. " +
//             "All layers advance together so every sprite stays in sync.")]
//    [Min(1f)]
//    [SerializeField] private float ridingFps = 8f;

//    // ── Private — cached sprite arrays (set by ShowForSoldier) ────────────────

//    private Sprite[] _bodySprites;
//    private Sprite[] _faceSprites;
//    private Sprite[] _hairSprites;
//    private Sprite[] _helmetSprites;
//    private Sprite[] _weaponSprites;

//    // ── Private — animation state ─────────────────────────────────────────────

//    private CanvasGroup _cg;
//    private bool _riding;   // true only while a soldier is mounted
//    private float _timer;
//    private int _frame;

//    // ── Lifecycle ─────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _cg = GetComponent<CanvasGroup>();
//        if (_cg == null)
//            _cg = gameObject.AddComponent<CanvasGroup>();

//        // Start fully hidden; does NOT block raycasts so the dragon seat
//        // underneath remains reachable by the EventSystem.
//        _cg.alpha = 0f;
//        _cg.blocksRaycasts = false;
//        _cg.interactable = false;
//    }

//    private void Update()
//    {
//        if (!_riding) return;

//        _timer += Time.deltaTime;
//        if (_timer < 1f / ridingFps) return;

//        _timer = 0f;
//        _frame++;
//        ApplyFrame();
//    }

//    // ── Public API — called by DragonController ───────────────────────────────

//    /// <summary>
//    /// Reads the soldier's equipped items, caches their riding sprite arrays,
//    /// shows frame 0 immediately, then lets Update() animate from there.
//    ///
//    /// Call this from DragonController.PerformMount() AFTER the soldier
//    /// has been reparented to the seat.
//    /// </summary>
//    public void ShowForSoldier(CharacterEquipment equipment)
//    {
//        if (equipment == null)
//        {
//            Debug.LogWarning("[DragonRiderVisual] ShowForSoldier: equipment is null — " +
//                             "rider visual will remain hidden.", this);
//            return;
//        }

//        BodyType bodyType = equipment.CurrentBodyType;

//        // ── Cache sprite arrays ───────────────────────────────────────────────

//        // Armor overrides the plain body layer; fall back to BodyType item.
//        var armor = equipment.GetEquipped(EquipmentSlot.Armor);
//        var body = equipment.GetEquipped(EquipmentSlot.BodyType);
//        _bodySprites = GetRidingSprites(armor ?? body, bodyType);

//        _faceSprites = GetRidingSprites(equipment.GetEquipped(EquipmentSlot.Face), bodyType);
//        _helmetSprites = GetRidingSprites(equipment.GetEquipped(EquipmentSlot.Helmet), bodyType);
//        _weaponSprites = GetRidingSprites(equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);

//        // Hair: only cache if no helmet is equipped.
//        var helmet = equipment.GetEquipped(EquipmentSlot.Helmet);
//        _hairSprites = helmet != null
//            ? null
//            : GetRidingSprites(equipment.GetEquipped(EquipmentSlot.Hair), bodyType);

//        // ── Reset animation and show frame 0 ──────────────────────────────────
//        _frame = 0;
//        _timer = 0f;
//        _riding = true;

//        ApplyFrame();

//        // ── Make visible ──────────────────────────────────────────────────────
//        _cg.alpha = 1f;
//        _cg.blocksRaycasts = false;   // never block — seat must stay raycastable
//        _cg.interactable = false;

//        Debug.Log("[DragonRiderVisual] Rider visual shown " +
//                  $"(armor: {(armor ?? body)?.itemName ?? "none"}, " +
//                  $"helmet: {helmet?.itemName ?? "none"}).");
//    }

//    /// <summary>
//    /// Hides the rider visual and stops the animation.
//    /// Call from DragonController.PerformDismount() after the soldier
//    /// has been reparented away from the seat.
//    /// </summary>
//    public void Hide()
//    {
//        _riding = false;
//        _frame = 0;
//        _timer = 0f;

//        _cg.alpha = 0f;
//        _cg.blocksRaycasts = false;
//        _cg.interactable = false;

//        // Clear all layer sprites so no stale frame bleeds through on
//        // the next ShowForSoldier() call before sprites are reassigned.
//        ClearLayer(bodyLayer);
//        ClearLayer(faceLayer);
//        ClearLayer(hairLayer);
//        ClearLayer(helmetLayer);
//        ClearLayer(weaponLayer);

//        // Clear cached arrays.
//        _bodySprites = null;
//        _faceSprites = null;
//        _hairSprites = null;
//        _helmetSprites = null;
//        _weaponSprites = null;

//        Debug.Log("[DragonRiderVisual] Rider visual hidden.");
//    }

//    // ── Internal helpers ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Pushes the current _frame to every active image layer.
//    /// Each layer wraps its frame index against its own sprite count so
//    /// layers with different array lengths still loop correctly.
//    /// </summary>
//    private void ApplyFrame()
//    {
//        SetLayerFrame(bodyLayer, _bodySprites);
//        SetLayerFrame(faceLayer, _faceSprites);
//        SetLayerFrame(hairLayer, _hairSprites);
//        SetLayerFrame(helmetLayer, _helmetSprites);
//        SetLayerFrame(weaponLayer, _weaponSprites);
//    }

//    /// <summary>
//    /// Sets <paramref name="image"/> to frame <see cref="_frame"/> of
//    /// <paramref name="sprites"/>, wrapping with modulo.
//    /// Disables the image if the array is null or empty.
//    /// </summary>
//    private void SetLayerFrame(Image image, Sprite[] sprites)
//    {
//        if (image == null) return;

//        if (sprites == null || sprites.Length == 0)
//        {
//            image.enabled = false;
//            return;
//        }

//        image.sprite = sprites[_frame % sprites.Length];
//        image.enabled = true;
//    }

//    /// <summary>
//    /// Returns the riding sprites for <paramref name="item"/>, falling back
//    /// to idle sprites if no riding sprites are assigned.
//    /// Returns null if the item is null or has no usable sprites at all.
//    /// </summary>
//    private Sprite[] GetRidingSprites(EquipmentItem item, BodyType bodyType)
//    {
//        if (item == null) return null;

//        Sprite[] sprites = item.GetSprites(AnimationState.Riding, bodyType);

//        if (sprites == null || sprites.Length == 0)
//            sprites = item.GetSprites(AnimationState.Idle, bodyType);

//        return (sprites != null && sprites.Length > 0) ? sprites : null;
//    }

//    /// <summary>Disables the layer and clears its sprite reference.</summary>
//    private void ClearLayer(Image image)
//    {
//        if (image == null) return;
//        image.sprite = null;
//        image.enabled = false;
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DRAGON RIDER VISUAL
///
/// Attach to a child of RiderSeat named "DragonRiderVisual".
/// This GameObject holds one Image per equipment slot and is hidden
/// by default. When a soldier mounts, DragonController calls
/// ShowForSoldier() which caches sprite arrays for BOTH rider states
/// (RiderIdle and RiderFly). DragonController then calls SetRiderState()
/// each time the dragon transitions between Idle ↔ Flying so the rider's
/// animation automatically matches the dragon's state.
///
/// ════════════════════════════════════════════════════════════════════
///  RIDER ANIMATION STATES
/// ════════════════════════════════════════════════════════════════════
///
///  RiderIdle  Dragon is resting in the DragonArea. Rider plays the
///             riderIdleSprites (EquipmentItem) — the soldier sits still.
///
///  RiderFly   Dragon is patrolling a FlyZone. Rider plays the
///             riderFlySprites (EquipmentItem) — the soldier leans forward.
///
///  DragonController calls SetRiderState(AnimationState.RiderIdle) inside
///  EnterIdle() and SetRiderState(AnimationState.RiderFly) inside EnterFlying().
///
/// ════════════════════════════════════════════════════════════════════
///  PREFAB HIERARCHY  (single dragon prefab)
/// ════════════════════════════════════════════════════════════════════
///
///   Dragon (root)              ← DragonController, CanvasGroup
///   ├── DragonBody [0]         ← Image: dragon body
///   ├── RiderSeat  [1]         ← DragonRiderSeat
///   │   └── DragonRiderVisual  ← THIS script  (hidden by default)
///   │       ├── BodyLayer      ← Image for body / armor
///   │       ├── FaceLayer      ← Image for face
///   │       ├── HairLayer      ← Image for hair (hidden when helmet equipped)
///   │       ├── HelmetLayer    ← Image for helmet
///   │       └── WeaponLayer    ← Image for weapon
///   └── DragonWing [2]         ← Image: front wing (renders on top)
///
/// ════════════════════════════════════════════════════════════════════
///  INSPECTOR SETUP
/// ════════════════════════════════════════════════════════════════════
///
///  1. Add a child called "DragonRiderVisual" inside RiderSeat.
///  2. Add child Images for each layer (BodyLayer, HelmetLayer, …).
///  3. Attach this script to DragonRiderVisual and drag each Image
///     into the matching Inspector field.
///  4. Leave the GameObject active in the prefab — visibility is
///     controlled by CanvasGroup.alpha, NOT SetActive.
///     (SetActive stops the CanvasGroup from being found in Awake.)
/// </summary>
public class DragonRiderVisual : MonoBehaviour
{
    // ── Inspector — Image layers ──────────────────────────────────────────────

    [Header("Equipment Image Layers (drag child Images here)")]
    [Tooltip("Image that shows the body or armor sprite while riding.")]
    [SerializeField] private Image bodyLayer;

    [Tooltip("Image that shows the face sprite while riding.")]
    [SerializeField] private Image faceLayer;

    [Tooltip("Image that shows the hair sprite while riding. " +
             "Automatically hidden when a helmet is equipped.")]
    [SerializeField] private Image hairLayer;

    [Tooltip("Image that shows the helmet sprite while riding.")]
    [SerializeField] private Image helmetLayer;

    [Tooltip("Image that shows the weapon sprite while riding. " +
             "Leave blank to omit the weapon layer.")]
    [SerializeField] private Image weaponLayer;

    [Header("Animation")]
    [Tooltip("Frames per second for the riding animation. " +
             "All layers advance together so every sprite stays in sync.")]
    [Min(1f)]
    [SerializeField] private float ridingFps = 8f;

    // ── Private — cached sprite arrays for BOTH states ────────────────────────

    // RiderIdle caches (dragon is resting)
    private Sprite[] _bodyIdleSprites;
    private Sprite[] _faceIdleSprites;
    private Sprite[] _hairIdleSprites;
    private Sprite[] _helmetIdleSprites;
    private Sprite[] _weaponIdleSprites;

    // RiderFly caches (dragon is patrolling)
    private Sprite[] _bodyFlySprites;
    private Sprite[] _faceFlySprites;
    private Sprite[] _hairFlySprites;
    private Sprite[] _helmetFlySprites;
    private Sprite[] _weaponFlySprites;

    // ── Private — active arrays (points to whichever state is current) ────────

    private Sprite[] _bodySprites;
    private Sprite[] _faceSprites;
    private Sprite[] _hairSprites;
    private Sprite[] _helmetSprites;
    private Sprite[] _weaponSprites;

    // ── Private — animation state ─────────────────────────────────────────────

    private CanvasGroup _cg;
    private bool _riding;   // true only while a soldier is mounted
    private float _timer;
    private int _frame;
    private AnimationState _riderState = AnimationState.RiderIdle;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();

        // Start fully hidden; does NOT block raycasts so the dragon seat
        // underneath remains reachable by the EventSystem.
        _cg.alpha = 0f;
        _cg.blocksRaycasts = false;
        _cg.interactable = false;
    }

    private void Update()
    {
        if (!_riding) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / ridingFps) return;

        _timer = 0f;
        _frame++;
        ApplyFrame();
    }

    // ── Public API — called by DragonController ───────────────────────────────

    /// <summary>
    /// Reads the soldier's equipped items, caches sprite arrays for BOTH rider
    /// states (RiderIdle and RiderFly), then shows frame 0 in RiderIdle by
    /// default. DragonController calls SetRiderState() immediately after to
    /// sync the rider to the dragon's current state.
    ///
    /// Call this from DragonController.PerformMount() AFTER the soldier
    /// has been reparented to the seat.
    /// </summary>
    public void ShowForSoldier(CharacterEquipment equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[DragonRiderVisual] ShowForSoldier: equipment is null — " +
                             "rider visual will remain hidden.", this);
            return;
        }

        BodyType bodyType = equipment.CurrentBodyType;

        var armor = equipment.GetEquipped(EquipmentSlot.Armor);
        var body = equipment.GetEquipped(EquipmentSlot.BodyType);
        var helmet = equipment.GetEquipped(EquipmentSlot.Helmet);
        var hair = equipment.GetEquipped(EquipmentSlot.Hair);

        // ── Cache RiderIdle arrays ────────────────────────────────────────────
        _bodyIdleSprites = GetStateSprites(armor ?? body, AnimationState.RiderIdle, bodyType);
        _faceIdleSprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Face), AnimationState.RiderIdle, bodyType);
        _helmetIdleSprites = GetStateSprites(helmet, AnimationState.RiderIdle, bodyType);
        _weaponIdleSprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Weapon), AnimationState.RiderIdle, bodyType);
        _hairIdleSprites = helmet != null ? null
                           : GetStateSprites(hair, AnimationState.RiderIdle, bodyType);

        // ── Cache RiderFly arrays ─────────────────────────────────────────────
        _bodyFlySprites = GetStateSprites(armor ?? body, AnimationState.RiderFly, bodyType);
        _faceFlySprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Face), AnimationState.RiderFly, bodyType);
        _helmetFlySprites = GetStateSprites(helmet, AnimationState.RiderFly, bodyType);
        _weaponFlySprites = GetStateSprites(equipment.GetEquipped(EquipmentSlot.Weapon), AnimationState.RiderFly, bodyType);
        _hairFlySprites = helmet != null ? null
                           : GetStateSprites(hair, AnimationState.RiderFly, bodyType);

        // ── Start in RiderIdle — DragonController syncs to current dragon state ─
        _riding = true;
        ApplyRiderState(AnimationState.RiderIdle);

        // ── Make visible ──────────────────────────────────────────────────────
        _cg.alpha = 1f;
        _cg.blocksRaycasts = false;   // never block — seat must stay raycastable
        _cg.interactable = false;

        Debug.Log("[DragonRiderVisual] Rider visual shown " +
                  $"(armor: {(armor ?? body)?.itemName ?? "none"}, " +
                  $"helmet: {helmet?.itemName ?? "none"}).");
    }

    /// <summary>
    /// Switches the active riding animation to match the dragon's current state.
    ///
    /// Call from DragonController.EnterIdle()   with AnimationState.RiderIdle.
    /// Call from DragonController.EnterFlying() with AnimationState.RiderFly.
    ///
    /// Safe to call when no soldier is mounted — does nothing in that case.
    /// Resets to frame 0 so the new animation always starts cleanly.
    /// </summary>
    public void SetRiderState(AnimationState state)
    {
        if (!_riding) return;
        if (state != AnimationState.RiderIdle && state != AnimationState.RiderFly) return;
        if (_riderState == state) return;   // already in this state, skip the reset

        ApplyRiderState(state);
        Debug.Log($"[DragonRiderVisual] Rider state → {state}");
    }

    /// <summary>
    /// Hides the rider visual and stops the animation.
    /// Call from DragonController.PerformDismount() after the soldier
    /// has been reparented away from the seat.
    /// </summary>
    public void Hide()
    {
        _riding = false;
        _frame = 0;
        _timer = 0f;

        _cg.alpha = 0f;
        _cg.blocksRaycasts = false;
        _cg.interactable = false;

        // Clear display layers.
        ClearLayer(bodyLayer);
        ClearLayer(faceLayer);
        ClearLayer(hairLayer);
        ClearLayer(helmetLayer);
        ClearLayer(weaponLayer);

        // Clear all cached arrays so no stale frame bleeds through on the
        // next ShowForSoldier() call before sprites are reassigned.
        _bodyIdleSprites = _bodyFlySprites = _bodySprites = null;
        _faceIdleSprites = _faceFlySprites = _faceSprites = null;
        _hairIdleSprites = _hairFlySprites = _hairSprites = null;
        _helmetIdleSprites = _helmetFlySprites = _helmetSprites = null;
        _weaponIdleSprites = _weaponFlySprites = _weaponSprites = null;

        Debug.Log("[DragonRiderVisual] Rider visual hidden.");
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Switches the active sprite arrays to the given rider state and resets
    /// the frame counter so the new animation starts cleanly from frame 0.
    /// </summary>
    private void ApplyRiderState(AnimationState state)
    {
        _riderState = state;
        _frame = 0;
        _timer = 0f;

        if (state == AnimationState.RiderFly)
        {
            _bodySprites = _bodyFlySprites;
            _faceSprites = _faceFlySprites;
            _hairSprites = _hairFlySprites;
            _helmetSprites = _helmetFlySprites;
            _weaponSprites = _weaponFlySprites;
        }
        else   // RiderIdle (default)
        {
            _bodySprites = _bodyIdleSprites;
            _faceSprites = _faceIdleSprites;
            _hairSprites = _hairIdleSprites;
            _helmetSprites = _helmetIdleSprites;
            _weaponSprites = _weaponIdleSprites;
        }

        ApplyFrame();
    }

    /// <summary>
    /// Pushes the current _frame to every active image layer.
    /// Each layer wraps its frame index against its own sprite count so
    /// layers with different array lengths still loop correctly.
    /// </summary>
    private void ApplyFrame()
    {
        SetLayerFrame(bodyLayer, _bodySprites);
        SetLayerFrame(faceLayer, _faceSprites);
        SetLayerFrame(hairLayer, _hairSprites);
        SetLayerFrame(helmetLayer, _helmetSprites);
        SetLayerFrame(weaponLayer, _weaponSprites);
    }

    /// <summary>
    /// Sets <paramref name="image"/> to frame <see cref="_frame"/> of
    /// <paramref name="sprites"/>, wrapping with modulo.
    /// Disables the image if the array is null or empty.
    /// </summary>
    private void SetLayerFrame(Image image, Sprite[] sprites)
    {
        if (image == null) return;

        if (sprites == null || sprites.Length == 0)
        {
            image.enabled = false;
            return;
        }

        image.sprite = sprites[_frame % sprites.Length];
        image.enabled = true;
    }

    /// <summary>
    /// Returns the sprites for <paramref name="item"/> at the requested
    /// <paramref name="state"/>. EquipmentItem.GetSprites() handles the full
    /// fallback chain (RiderFly → RiderIdle → Idle).
    /// Returns null if the item is null or has no usable sprites at all.
    /// </summary>
    private Sprite[] GetStateSprites(EquipmentItem item, AnimationState state, BodyType bodyType)
    {
        if (item == null) return null;

        Sprite[] sprites = item.GetSprites(state, bodyType);
        return (sprites != null && sprites.Length > 0) ? sprites : null;
    }

    /// <summary>Disables the layer and clears its sprite reference.</summary>
    private void ClearLayer(Image image)
    {
        if (image == null) return;
        image.sprite = null;
        image.enabled = false;
    }
}