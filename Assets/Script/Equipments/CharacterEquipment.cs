using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AREA FORGE - CharacterEquipment
///
/// Tracks what is currently equipped per slot.
/// Attach to the root Player GameObject.
/// </summary>
public class CharacterEquipment : MonoBehaviour
{
    [Header("Default Loadout (drag your DefaultLoadout asset here)")]
    [SerializeField] private CharacterLoadout defaultLoadout;

    public event System.Action<EquipmentSlot, EquipmentItem> OnEquipmentChanged;

    private readonly Dictionary<EquipmentSlot, EquipmentItem> _equipped = new();
    private CharacterVisuals _visuals;
    private SoldierStats _stats;
    private SpriteLayerAnimator _animator;

    public BodyType CurrentBodyType { get; private set; } = BodyType.Normal;

    private void Awake()
    {
        _visuals = GetComponent<CharacterVisuals>();
        _stats = GetComponent<SoldierStats>();
        _animator = GetComponent<SpriteLayerAnimator>();
    }

    private void Start()
    {
        if (defaultLoadout != null)
            ApplyLoadout(defaultLoadout);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void Equip(EquipmentItem item)
    {
        if (item == null) return;

        if (_equipped.ContainsKey(item.slot))
            RemoveSlot(item.slot, fireEvent: false);

        _equipped[item.slot] = item;

        if (item.slot == EquipmentSlot.BodyType)
            SyncBodyType(item);

        // Show frame 0 immediately — animator takes over next Update
        var sprites = item.GetSprites(_visuals?.CurrentState ?? AnimationState.Idle, CurrentBodyType);
        _visuals?.SetSprite(item.slot, sprites?.Length > 0 ? sprites[0] : null);

        _stats?.ApplyEquipmentBonus(item.healthBonus, item.abilityBonus, item.damageBonus);
        OnEquipmentChanged?.Invoke(item.slot, item);
    }

    public void Unequip(EquipmentSlot slot) => RemoveSlot(slot, fireEvent: true);

    public EquipmentItem GetEquipped(EquipmentSlot slot)
    {
        _equipped.TryGetValue(slot, out var item);
        return item;
    }

    public bool IsEquipped(EquipmentItem item)
    {
        if (item == null) return false;
        return _equipped.TryGetValue(item.slot, out var cur) && cur == item;
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void ApplyLoadout(CharacterLoadout l)
    {
        TryEquip(l.defaultBodyType);
        TryEquip(l.defaultFace);
        TryEquip(l.defaultHair);
        TryEquip(l.defaultArmor);
        TryEquip(l.defaultHelmet);   // after hair so hide-hair rule fires correctly
        TryEquip(l.defaultWeapon);
    }

    private void TryEquip(EquipmentItem i) { if (i != null) Equip(i); }

    private void RemoveSlot(EquipmentSlot slot, bool fireEvent)
    {
        if (!_equipped.TryGetValue(slot, out var old)) return;
        _stats?.ApplyEquipmentBonus(-old.healthBonus, -old.abilityBonus, -old.damageBonus);
        _equipped.Remove(slot);
        _visuals?.SetSprite(slot, null);
        if (fireEvent) OnEquipmentChanged?.Invoke(slot, null);
    }

    private void SyncBodyType(EquipmentItem item)
    {
        if (item.itemName.Contains("Skinny")) CurrentBodyType = BodyType.Skinny;
        else if (item.itemName.Contains("Chubby")) CurrentBodyType = BodyType.Chubby;
        else if (item.itemName.Contains("Muscular")) CurrentBodyType = BodyType.Muscular;
        else CurrentBodyType = BodyType.Normal;

        // Refresh variant-sensitive layers
        foreach (var s in new[]{ EquipmentSlot.Face, EquipmentSlot.Hair,
                                  EquipmentSlot.Armor, EquipmentSlot.Helmet })
        {
            var it = GetEquipped(s);
            if (it == null) continue;
            var sprites = it.GetSprites(_visuals?.CurrentState ?? AnimationState.Idle, CurrentBodyType);
            _visuals?.SetSprite(s, sprites?.Length > 0 ? sprites[0] : null);
        }
    }
}