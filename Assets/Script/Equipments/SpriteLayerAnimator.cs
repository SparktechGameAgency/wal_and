using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - SpriteLayerAnimator  (UI Image version)
///
/// Advances animation frames on ALL UI Image layers simultaneously.
/// Attach to the root Player GameObject alongside CharacterVisuals + CharacterEquipment.
///
/// Every tick it reads the current equipped item's sprite array and
/// sets the matching Image.sprite — so changing equipment mid-game
/// just works with no Animator changes needed.
/// </summary>
public class SpriteLayerAnimator : MonoBehaviour
{
    [Header("Animation Speed")]
    [Tooltip("Frames per second — match your sprite sheet's intended FPS (usually 6–12)")]
    [SerializeField] private float fps = 8f;

    private CharacterVisuals _visuals;
    private CharacterEquipment _equipment;
    private float _timer = 0f;
    private int _frame = 0;
    private AnimationState _state = AnimationState.Idle;

    private void Awake()
    {
        _visuals = GetComponent<CharacterVisuals>();
        _equipment = GetComponent<CharacterEquipment>();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;
        _timer = 0f;
        _frame++;
        AdvanceAllLayers();
    }

    // ─── Called by SoldierController ─────────────────────────────────────────

    /// <summary>
    /// Call this from SoldierController when animation state changes:
    ///   Walk starts  → SetState(AnimationState.Walk)
    ///   Rest starts  → SetState(AnimationState.Idle)
    ///   Death        → SetState(AnimationState.Death)
    /// </summary>
    public void SetState(AnimationState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _visuals.CurrentState = newState;
        _frame = 0;
        _timer = 0f;
        AdvanceAllLayers();
    }

    // ─── Frame stepping ───────────────────────────────────────────────────────

    private void AdvanceAllLayers()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var item = _equipment.GetEquipped(slot);
            if (item == null) continue;

            var sprites = item.GetSprites(_state, _equipment.CurrentBodyType);
            if (sprites == null || sprites.Length == 0) continue;

            int idx = _frame % sprites.Length;
            var img = _visuals.GetImage(slot);
            if (img != null && img.enabled)
                img.sprite = sprites[idx];
        }
    }
}