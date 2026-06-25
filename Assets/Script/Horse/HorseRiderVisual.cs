using UnityEngine;
using UnityEngine.UI;


public class HorseRiderVisual : MonoBehaviour
{
    // ── Inspector — Layer Images ───────────────────────────────────────────────

    [Header("Rider Body-Part Images  (auto-found by child name if left null)")]
    [Tooltip("Image on the 'Face' child.   Raycast Target = OFF.")]
    [SerializeField] private Image faceImage;

    [Tooltip("Image on the 'Armor' child.  Raycast Target = OFF.")]
    [SerializeField] private Image armorImage;

    [Tooltip("Image on the 'Helmet' child. Raycast Target = OFF.")]
    [SerializeField] private Image helmetImage;

    [Tooltip("Image on the 'Weapon' child. Raycast Target = OFF.")]
    [SerializeField] private Image weaponImage;

    // ── Inspector — Playback Speed ─────────────────────────────────────────────

    [Header("Playback Speed (frames per second)")]
    [Tooltip("FPS while the horse stands still. Try 6–8.")]
    [Min(1f)][SerializeField] private float horseIdleFps = 6f;

    [Tooltip("FPS while the horse gallops. Try 10–12.")]
    [Min(1f)][SerializeField] private float horseRunFps = 12f;

    [Tooltip("FPS during mounted combat. Try 10.")]
    [Min(1f)][SerializeField] private float horseFightFps = 10f;

    [Tooltip("FPS for the death animation (slow, dramatic). Try 6–7.")]
    [Min(1f)][SerializeField] private float horseDeadFps = 6f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private CharacterEquipment _equipment;
    private AnimationState _state = AnimationState.HorseIdle;
    private bool _active = false;

    // Per-layer timers and frame counters
    private float _faceTimer, _armorTimer, _helmetTimer, _weaponTimer;
    private int _faceFrame, _armorFrame, _helmetFrame, _weaponFrame;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (faceImage == null) faceImage = FindChildImage("Face");
        if (armorImage == null) armorImage = FindChildImage("Armor");
        if (helmetImage == null) helmetImage = FindChildImage("Helmet");
        if (weaponImage == null) weaponImage = FindChildImage("Weapon");

        HideRider();
    }

    private void Update()
    {
        if (!_active || _equipment == null) return;

        float fps = FpsForState(_state);
        float dt = Time.deltaTime;
        BodyType bodyType = _equipment.CurrentBodyType;

        // ── DIAGNOSTIC: log every ~60 frames so we can see runtime state ──────
        bool doLog = (Time.frameCount % 60 == 0);
        if (doLog)
        {
            LogLayerDiag("Face", faceImage, _equipment.GetEquipped(EquipmentSlot.Face), bodyType);
            LogLayerDiag("Armor", armorImage, _equipment.GetEquipped(EquipmentSlot.Armor), bodyType);
            LogLayerDiag("Helmet", helmetImage, _equipment.GetEquipped(EquipmentSlot.Helmet), bodyType);
            LogLayerDiag("Weapon", weaponImage, _equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);
        }
        // ── END DIAGNOSTIC ────────────────────────────────────────────────────

        TickLayer(ref _faceFrame, ref _faceTimer, faceImage,
                  _equipment.GetEquipped(EquipmentSlot.Face), bodyType, fps, dt);

        TickLayer(ref _armorFrame, ref _armorTimer, armorImage,
                  _equipment.GetEquipped(EquipmentSlot.Armor), bodyType, fps, dt);

        TickLayer(ref _helmetFrame, ref _helmetTimer, helmetImage,
                  _equipment.GetEquipped(EquipmentSlot.Helmet), bodyType, fps, dt);

        TickLayer(ref _weaponFrame, ref _weaponTimer, weaponImage,
                  _equipment.GetEquipped(EquipmentSlot.Weapon), bodyType, fps, dt);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowRider(CharacterEquipment equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[HorseRiderVisual] ShowRider called with null equipment.", this);
            return;
        }

        _equipment = equipment;
        _active = true;

        EnableLayer(faceImage, true);
        EnableLayer(armorImage, true);
        EnableLayer(helmetImage, true);
        EnableLayer(weaponImage, true);

        SetRiderStateInternal(AnimationState.HorseIdle, force: true);
    }

    public void HideRider()
    {
        _active = false;
        _equipment = null;

        EnableLayer(faceImage, false);
        EnableLayer(armorImage, false);
        EnableLayer(helmetImage, false);
        EnableLayer(weaponImage, false);
    }

    public void SetRiderState(AnimationState newState)
    {
        if (!_active || _equipment == null) return;
        SetRiderStateInternal(newState, force: false);
    }

    // ── Internal animation engine ─────────────────────────────────────────────

    private void SetRiderStateInternal(AnimationState newState, bool force)
    {
        if (!force && _state == newState) return;

        Debug.Log($"[HorseRiderVisual] '{name}' SetRiderStateInternal: {_state} → {newState} (force={force})");

        _state = newState;

        _faceTimer = _armorTimer = _helmetTimer = _weaponTimer = 0f;
        _faceFrame = _armorFrame = _helmetFrame = _weaponFrame = 0;

        BodyType bodyType = _equipment.CurrentBodyType;

        ShowFirstFrame(faceImage, _equipment.GetEquipped(EquipmentSlot.Face), bodyType);
        ShowFirstFrame(armorImage, _equipment.GetEquipped(EquipmentSlot.Armor), bodyType);
        ShowFirstFrame(helmetImage, _equipment.GetEquipped(EquipmentSlot.Helmet), bodyType);
        ShowFirstFrame(weaponImage, _equipment.GetEquipped(EquipmentSlot.Weapon), bodyType);
    }

    private void TickLayer(ref int frame, ref float timer, Image img,
                           EquipmentItem item, BodyType bodyType,
                           float fps, float dt)
    {
        if (img == null || item == null) return;

        Sprite[] sprites = item.GetSprites(_state, bodyType);
        if (sprites == null || sprites.Length == 0) return;

        if (!img.enabled) img.enabled = true;

        // Single-frame — just ensure correct sprite is set, no timer needed
        if (sprites.Length == 1)
        {
            img.sprite = sprites[0];
            return;
        }

        timer += dt;
        float frameDuration = 1f / fps;
        if (timer < frameDuration) return;

        timer -= frameDuration;

        if (_state == AnimationState.HorseDead)
        {
            if (frame < sprites.Length - 1) frame++;
        }
        else
        {
            frame = (frame + 1) % sprites.Length;
        }

        img.sprite = sprites[frame];
    }

    private void ShowFirstFrame(Image img, EquipmentItem item, BodyType bodyType)
    {
        if (img == null || item == null) return;

        Sprite[] sprites = item.GetSprites(_state, bodyType);
        if (sprites != null && sprites.Length > 0)
        {
            img.sprite = sprites[0];
            img.enabled = true;
        }
    }

    // ── Diagnostic helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Logs one line per layer per ~second so you can see exactly what
    /// GetSprites() is returning at runtime.
    ///
    /// READ THE LOG LIKE THIS:
    ///   frames=1  → GetSprites returned 1 sprite  (idle fallback being served)
    ///   frames=8  → GetSprites returned 8 sprites (run array being served — correct)
    ///   frames=-1 → GetSprites returned null/empty (item not equipped or array missing)
    ///   enabled=False → the Image component is disabled (invisible even if animated)
    ///   item=None → slot is not equipped at all
    /// </summary>
    private void LogLayerDiag(string slotName, Image img, EquipmentItem item, BodyType bodyType)
    {
        if (item == null)
        {
            Debug.Log($"[RiderDiag] {slotName}: item=None  state={_state}  active={_active}");
            return;
        }

        Sprite[] sprites = item.GetSprites(_state, bodyType);
        int frameCount = sprites?.Length ?? -1;

        // Also check what horseRunSprites looks like directly on the item
        // so we can see if the array is populated regardless of fallback logic
        int directRunCount = item.horseRunSprites?.Length ?? -1;

        Debug.Log($"[RiderDiag] {slotName}: item='{item.itemName}'  state={_state}  " +
                  $"bodyType={bodyType}  frames={frameCount}  " +
                  $"directRunSprites={directRunCount}  " +
                  $"imgEnabled={img?.enabled}  imgNull={img == null}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnableLayer(Image img, bool enabled)
    {
        if (img != null) img.enabled = enabled;
    }

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[HorseRiderVisual] '{name}': child '{childName}' not found.", this);
            return null;
        }
        Image img = child.GetComponent<Image>();
        if (img == null)
            Debug.LogWarning($"[HorseRiderVisual] '{name}': child '{childName}' has no Image.", this);
        return img;
    }

    private float FpsForState(AnimationState state) => state switch
    {
        AnimationState.HorseRun => horseRunFps,
        AnimationState.HorseFight => horseFightFps,
        AnimationState.HorseDead => horseDeadFps,
        _ => horseIdleFps,
    };
}