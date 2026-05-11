using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AREA FORGE - CharacterEquipment
///
/// Tracks what is currently equipped per slot.
/// Attach to the root Player GameObject.
///
/// ── Fix: skipping default loadout on spawn ───────────────────────────────────
/// When GameManager.SpawnBasicSoldier() calls ApplySelectionToSoldier() right
/// after Instantiate(), the custom items are equipped BEFORE Start() runs.
/// Without the flag below, Start() would call ApplyLoadout(defaultLoadout) and
/// wipe the custom equipment.
///
/// Solution: Equip() sets _customLoadoutApplied = true.
/// Start() checks the flag and skips ApplyLoadout() if it's already true.
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

    // ── Fix: set to true the first time Equip() is called ────────────────────
    private bool _customLoadoutApplied = false;

    public BodyType CurrentBodyType { get; private set; } = BodyType.Normal;

    private void Awake()
    {
        _visuals = GetComponent<CharacterVisuals>();
        _stats = GetComponent<SoldierStats>();
        _animator = GetComponent<SpriteLayerAnimator>();
    }

    private void Start()
    {
        // Called AFTER OnSoldierSpawned has already equipped any custom items,
        // so _equipped accurately reflects what the player chose in the panel.
        //
        // Step 1 — fill empty slots from the default loadout.
        //   e.g. body/face/hair always load; armor/helmet/weapon only load if
        //   the player didn't select something custom AND the loadout has a default.
        if (defaultLoadout != null)
        {
            TryEquipDefault(defaultLoadout.defaultBodyType);
            TryEquipDefault(defaultLoadout.defaultFace);
            TryEquipDefault(defaultLoadout.defaultHair);
            TryEquipDefault(defaultLoadout.defaultHelmet);
            TryEquipDefault(defaultLoadout.defaultArmor);
            TryEquipDefault(defaultLoadout.defaultWeapon);
        }

        // Step 2 — hide Image layers for slots that are still empty after defaults.
        //   Default soldier → armor/helmet/weapon have no item → Images disabled.
        //   Custom soldier  → Equip() already enabled their Images → skipped here.
        HideIfEmpty(EquipmentSlot.Armor);
        HideIfEmpty(EquipmentSlot.Helmet);
        HideIfEmpty(EquipmentSlot.Weapon);

        // Step 3 — re-enforce visual hide rules.
        //   TryEquipDefault(body) calls SetSprite(BodyType) which sets bodyImage.enabled = true,
        //   overriding the armor→body hide rule that fired during OnSoldierSpawned.
        //   We re-apply all rules here after all equips are final.
        EnforceVisualRules();
    }

    /// <summary>
    /// Re-applies all cross-slot visual hide rules after equipment is finalised.
    ///   Armor equipped  → hide body  (armor covers the body sprite)
    ///   Helmet equipped → hide hair  (helmet covers the hair sprite)
    /// Called at end of Start() so it always runs after TryEquipDefault().
    /// </summary>
    private void EnforceVisualRules()
    {
        if (_visuals == null) return;

        // Armor → hide body
        bool hasArmor = _equipped.ContainsKey(EquipmentSlot.Armor);
        var bodyImg = _visuals.GetImage(EquipmentSlot.BodyType);
        if (bodyImg != null) bodyImg.enabled = !hasArmor;

        // Helmet → hide hair
        bool hasHelmet = _equipped.ContainsKey(EquipmentSlot.Helmet);
        var hairImg = _visuals.GetImage(EquipmentSlot.Hair);
        if (hairImg != null) hairImg.enabled = !hasHelmet;
    }

    /// <summary>Equips item only if the slot is still empty.</summary>
    private void TryEquipDefault(EquipmentItem item)
    {
        if (item == null) return;
        if (_equipped.ContainsKey(item.slot)) return;  // custom item already there
        Equip(item);
    }

    /// <summary>
    /// Disables the Image layer for a slot that has nothing equipped.
    /// Called in Start() after all default and custom items are applied,
    /// so _equipped is final and we know which layers should be invisible.
    /// </summary>
    private void HideIfEmpty(EquipmentSlot slot)
    {
        if (!_equipped.ContainsKey(slot))
            _visuals?.SetSprite(slot, null);   // SetSprite(null) → img.enabled = false
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

    // ─── Internal ─────────────────────────────────────────────────────────────

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
        foreach (var s in new[] { EquipmentSlot.Face, EquipmentSlot.Hair,
                                   EquipmentSlot.Armor, EquipmentSlot.Helmet })
        {
            var it = GetEquipped(s);
            if (it == null) continue;
            var sprites = it.GetSprites(_visuals?.CurrentState ?? AnimationState.Idle, CurrentBodyType);
            _visuals?.SetSprite(s, sprites?.Length > 0 ? sprites[0] : null);
        }
    }
}