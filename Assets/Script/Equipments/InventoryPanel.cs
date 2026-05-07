////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// AREA FORGE - InventoryPanel
///////
/////// ── ROOT CAUSE FIX ────────────────────────────────────────────────────────────
///////   BUG: OnSoldierSpawned() called ApplyDefaultSelections() AFTER switching
///////   soldierEquipment to the new soldier. This wiped every user selection
///////   (golden armor, custom helmet, sword) before they were ever applied.
///////
///////   FIX: OnSoldierSpawned() now:
///////     1. Saves which buttons are selected BEFORE switching soldier
///////     2. Switches soldierEquipment to the new soldier
///////     3. Calls ApplyDefaultSelections() for visual button reset
///////     4. Re-equips the saved items on the NEW soldier  ← THE FIX
///////
/////// ── How equipping works ───────────────────────────────────────────────────────
///////   InventorySlotButton.Select()
///////     → CharacterEquipment.Equip(item)           (applies stat bonuses)
///////     → CharacterVisuals.SetSprite(slot, frame0) (swaps the Image sprite)
///////     → SpriteLayerAnimator advances frames       (runs idle/walk animation)
///////   SoldierStats fires OnStatsChanged
///////     → InventoryPanel.RefreshStats() → bars + text update live
///////
/////// ── Inspector wiring ─────────────────────────────────────────────────────────
///////   soldierEquipment → leave EMPTY — found automatically at runtime
///////
///////   Item Groups → drag each slot's button-parent GO:
///////     groupPlayer  = BodyType buttons parent
///////     groupHead    = Face buttons parent
///////     groupHair    = Hair buttons parent
///////     groupHelmet  = Helmet buttons parent
///////     groupArmor   = Armor buttons parent
///////     groupWeapon  = Weapon buttons parent
///////
///////   Tab Buttons → drag each tab Button (one per slot)
///////   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
///////
///////   ── Stats Panel ──────────────────────────────────────────────────────────
///////   healthBarFill  → Image (Filled, Horizontal, Left) for health
///////   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
///////   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
///////   healthText     → TextMeshProUGUI (shows MaxHealth number)
///////   abilityText    → TextMeshProUGUI (shows AbilityPower number)
///////   damageText     → TextMeshProUGUI (shows AttackDamage number)
/////// </summary>
////public class InventoryPanel : MonoBehaviour
////{
////    // ─── Inspector — Soldier ──────────────────────────────────────────────────

////    [Header("Soldier — leave empty, found automatically")]
////    [SerializeField] private CharacterEquipment soldierEquipment;

////    // ─── Inspector — Item Groups ──────────────────────────────────────────────

////    [Header("Item Groups — one parent GO per slot")]
////    [SerializeField] private GameObject groupPlayer;   // BodyType
////    [SerializeField] private GameObject groupHead;     // Face
////    [SerializeField] private GameObject groupHair;
////    [SerializeField] private GameObject groupHelmet;
////    [SerializeField] private GameObject groupArmor;
////    [SerializeField] private GameObject groupWeapon;

////    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

////    [Header("Helmet hides Hair")]
////    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
////    [SerializeField] private GameObject hairVisualRoot;

////    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

////    [Header("Tab Buttons")]
////    [SerializeField] private Button tabPlayer;
////    [SerializeField] private Button tabHead;
////    [SerializeField] private Button tabHair;
////    [SerializeField] private Button tabHelmet;
////    [SerializeField] private Button tabArmor;
////    [SerializeField] private Button tabWeapon;

////    [Header("Tab Colours")]
////    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
////    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

////    // ─── Inspector — Stat Bars ────────────────────────────────────────────────

////    [Header("Stats Bars — wire to see live stat changes on equip")]
////    [SerializeField] private Image healthBarFill;
////    [SerializeField] private Image abilityBarFill;
////    [SerializeField] private Image damageBarFill;

////    [Header("Stats Labels (TextMeshPro — optional)")]
////    [SerializeField] private TextMeshProUGUI healthText;
////    [SerializeField] private TextMeshProUGUI abilityText;
////    [SerializeField] private TextMeshProUGUI damageText;

////    [Header("Bar Max Reference Values")]
////    [SerializeField] private float maxHealthDisplay = 200f;
////    [SerializeField] private float maxAbilityDisplay = 100f;
////    [SerializeField] private float maxDamageDisplay = 100f;

////    // ─── Private ──────────────────────────────────────────────────────────────

////    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
////    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

////    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

////    private void Awake()
////    {
////        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
////        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
////        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
////        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
////        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
////        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

////        GameManager.OnSoldierSpawned += OnSoldierSpawned;
////    }

////    private void OnDestroy()
////    {
////        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
////        UnsubscribeStats();
////    }

////    private void OnEnable()
////    {
////        if (soldierEquipment == null)
////            soldierEquipment = FindFirstObjectByType<CharacterEquipment>();

////        SubscribeStats();
////        ForceAllGroupsActive(true);
////        BuildGroupCache();
////        InitAllButtons();
////        ApplyDefaultSelections();
////        ShowSlot(_activeSlot);
////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    private void OnDisable()
////    {
////        UnsubscribeStats();
////    }

////    // ─── Soldier Spawn Callback  ──────────────────────────────────────────────
////    //
////    // THIS IS THE KEY FIX.
////    //
////    // Old behaviour (broken):
////    //   soldierEquipment = newSoldier
////    //   ApplyDefaultSelections()    ← wiped golden armor, custom helmet, sword
////    //   // selected items NEVER applied to soldier
////    //
////    // New behaviour (fixed):
////    //   savedItems = GetSelectedItems()   ← capture user's choices FIRST
////    //   soldierEquipment = newSoldier
////    //   ApplyDefaultSelections()           ← resets button visuals (OK)
////    //   ApplyItemsToSoldier(savedItems)    ← re-equips choices on new soldier ✓

////    private void OnSoldierSpawned(GameObject soldierGO)
////    {
////        var newEquip = soldierGO.GetComponent<CharacterEquipment>();
////        if (newEquip == null)
////        {
////            Debug.LogWarning("[InventoryPanel] Spawned soldier has no CharacterEquipment — " +
////                             "add CharacterEquipment to the SolderPrefab.");
////            return;
////        }

////        // ── 1. Save current user selections BEFORE switching ──────────────────
////        var savedItems = GetSelectedItems();
////        Debug.Log($"[InventoryPanel] BUY pressed — saving {savedItems.Count} selected items.");

////        // ── 2. Switch to the new soldier ──────────────────────────────────────
////        UnsubscribeStats();
////        soldierEquipment = newEquip;

////        if (!gameObject.activeInHierarchy) return;

////        SubscribeStats();
////        ForceAllGroupsActive(true);
////        InitAllButtons();

////        // ── 3. Reset button visuals to defaults (nothing selected in armor/helmet/weapon) ──
////        ApplyDefaultSelections();

////        // ── 4. Re-equip saved items on the NEW soldier ← THE FIX ─────────────
////        if (savedItems.Count > 0)
////        {
////            ApplyItemsToSoldier(soldierEquipment, savedItems);
////            Debug.Log($"[InventoryPanel] Applied {savedItems.Count} items to spawned soldier.");
////        }
////        else
////        {
////            Debug.Log("[InventoryPanel] No items selected — soldier uses DefaultLoadout.");
////        }

////        ShowSlot(_activeSlot);
////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    // ─── Public API ───────────────────────────────────────────────────────────

////    /// <summary>
////    /// Returns one selected EquipmentItem per slot (skips slots with nothing chosen).
////    /// Called by OnSoldierSpawned before the soldier reference switches.
////    /// </summary>
////    public Dictionary<EquipmentSlot, EquipmentItem> GetSelectedItems()
////    {
////        var result = new Dictionary<EquipmentSlot, EquipmentItem>();

////        foreach (var kvp in _groups)
////        {
////            foreach (var btn in kvp.Value)
////            {
////                if (btn.IsSelected && btn.Item != null)
////                {
////                    result[kvp.Key] = btn.Item;
////                    break;   // one per slot
////                }
////            }
////        }

////        return result;
////    }

////    /// <summary>
////    /// Equips a dictionary of items (slot → item) onto a CharacterEquipment target.
////    /// Also updates the matching buttons to IsSelected = true so the panel
////    /// reflects the soldier's loadout correctly.
////    /// </summary>
////    private void ApplyItemsToSoldier(CharacterEquipment target,
////                                     Dictionary<EquipmentSlot, EquipmentItem> items)
////    {
////        if (target == null) return;

////        foreach (var kvp in items)
////        {
////            EquipmentSlot slot = kvp.Key;
////            EquipmentItem item = kvp.Value;

////            // Apply to soldier's CharacterEquipment
////            // This updates visuals via CharacterVisuals.SetSprite() AND stats
////            target.Equip(item);

////            // Sync button highlight so panel matches the soldier
////            if (_groups.TryGetValue(slot, out var list))
////            {
////                foreach (var btn in list)
////                {
////                    if (btn.Item == item)
////                    {
////                        btn.Select();   // highlight this button
////                    }
////                    else if (btn.IsSelected && !btn.IsDefault)
////                    {
////                        // Silently deselect others in this slot without unequipping
////                        // (the item was already replaced above)
////                        btn.ForceDeselect();
////                    }
////                }
////            }

////            Debug.Log($"[InventoryPanel] Equipped '{item.itemName}' ({slot}) on " +
////                      $"'{target.gameObject.name}'.");
////        }
////    }

////    /// <summary>
////    /// Called by InventorySlotButton — selects btn, deselects others in same slot.
////    /// </summary>
////    public void SelectButton(InventorySlotButton btn)
////    {
////        var list = FindGroup(btn);
////        if (list == null) return;

////        foreach (var b in list)
////        {
////            if (b == btn) b.Select();
////            else b.Deselect();
////        }

////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    /// <summary>
////    /// Called by InventorySlotButton — deselects btn only.
////    /// </summary>
////    public void DeselectButton(InventorySlotButton btn)
////    {
////        if (btn.IsDefault) return;
////        btn.Deselect();
////        ApplyHairHelmetRule();
////        RefreshStats();
////    }

////    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

////    private void ApplyHairHelmetRule()
////    {
////        if (hairVisualRoot == null) return;

////        bool helmetOn = false;
////        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
////            foreach (var btn in helmetGroup)
////                if (btn.IsSelected) { helmetOn = true; break; }

////        hairVisualRoot.SetActive(!helmetOn);
////    }

////    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

////    private void ShowSlot(EquipmentSlot slot)
////    {
////        _activeSlot = slot;
////        UpdateTabColours();
////        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
////        SetActive(groupHead, slot == EquipmentSlot.Face);
////        SetActive(groupHair, slot == EquipmentSlot.Hair);
////        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
////        SetActive(groupArmor, slot == EquipmentSlot.Armor);
////        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
////    }

////    private void ForceAllGroupsActive(bool active)
////    {
////        SetActive(groupPlayer, active);
////        SetActive(groupHead, active);
////        SetActive(groupHair, active);
////        SetActive(groupHelmet, active);
////        SetActive(groupArmor, active);
////        SetActive(groupWeapon, active);
////    }

////    private static void SetActive(GameObject go, bool active)
////    {
////        if (go != null) go.SetActive(active);
////    }

////    // ─── Tab Colours ──────────────────────────────────────────────────────────

////    private void UpdateTabColours()
////    {
////        Tint(tabPlayer, EquipmentSlot.BodyType);
////        Tint(tabHead, EquipmentSlot.Face);
////        Tint(tabHair, EquipmentSlot.Hair);
////        Tint(tabHelmet, EquipmentSlot.Helmet);
////        Tint(tabArmor, EquipmentSlot.Armor);
////        Tint(tabWeapon, EquipmentSlot.Weapon);
////    }

////    private void Tint(Button btn, EquipmentSlot slot)
////    {
////        if (btn == null) return;
////        var img = btn.GetComponent<Image>();
////        if (img != null)
////            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
////    }

////    // ─── Stats Refresh ────────────────────────────────────────────────────────

////    private void RefreshStats()
////    {
////        var stats = GetStats();
////        if (stats == null) return;

////        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
////        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
////        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

////        if (healthBarFill != null) healthBarFill.fillAmount = hp;
////        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
////        if (damageBarFill != null) damageBarFill.fillAmount = ad;

////        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
////        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
////        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
////    }

////    // ─── Stats Subscription ───────────────────────────────────────────────────

////    private void SubscribeStats()
////    {
////        var s = GetStats();
////        if (s != null) s.OnStatsChanged += OnStatsChanged;
////    }

////    private void UnsubscribeStats()
////    {
////        var s = GetStats();
////        if (s != null) s.OnStatsChanged -= OnStatsChanged;
////    }

////    private void OnStatsChanged(SoldierStats _) => RefreshStats();

////    private SoldierStats GetStats() =>
////        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

////    // ─── Group Cache ──────────────────────────────────────────────────────────

////    private void BuildGroupCache()
////    {
////        _groups.Clear();
////        Cache(EquipmentSlot.BodyType, groupPlayer);
////        Cache(EquipmentSlot.Face, groupHead);
////        Cache(EquipmentSlot.Hair, groupHair);
////        Cache(EquipmentSlot.Helmet, groupHelmet);
////        Cache(EquipmentSlot.Armor, groupArmor);
////        Cache(EquipmentSlot.Weapon, groupWeapon);
////    }

////    private void Cache(EquipmentSlot slot, GameObject group)
////    {
////        var list = new List<InventorySlotButton>();
////        if (group != null)
////            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
////        _groups[slot] = list;
////    }

////    // ─── Button Init ──────────────────────────────────────────────────────────

////    private void InitAllButtons()
////    {
////        foreach (var kvp in _groups)
////            foreach (var btn in kvp.Value)
////                btn.Init(this, soldierEquipment);
////    }

////    // ─── Default Selections ───────────────────────────────────────────────────

////    private void ApplyDefaultSelections()
////    {
////        foreach (var kvp in _groups)
////        {
////            var slot = kvp.Key;
////            var list = kvp.Value;
////            if (list.Count == 0) continue;

////            if (slot == EquipmentSlot.BodyType)
////            {
////                foreach (var btn in list)
////                {
////                    if (btn.IsDefault) btn.Select();
////                    else btn.Deselect();
////                }
////            }
////            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
////            {
////                for (int i = 0; i < list.Count; i++)
////                {
////                    if (i == 0) list[i].Select();
////                    else list[i].Deselect();
////                }
////            }
////            else
////            {
////                // Armor, Helmet, Weapon — nothing selected by default
////                foreach (var btn in list) btn.Deselect();
////            }
////        }
////    }

////    // ─── Helpers ──────────────────────────────────────────────────────────────

////    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
////    {
////        foreach (var kvp in _groups)
////            foreach (var btn in kvp.Value)
////                if (btn == target) return kvp.Value;
////        return null;
////    }

////    // ─── Open / Close ─────────────────────────────────────────────────────────

////    public void Open() => gameObject.SetActive(true);
////    public void Close() => gameObject.SetActive(false);
////    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// AREA FORGE - InventoryPanel
/////
///// ── ROOT CAUSE FIX ────────────────────────────────────────────────────────────
/////   BUG: OnSoldierSpawned() called ApplyDefaultSelections() AFTER switching
/////   soldierEquipment to the new soldier. This wiped every user selection
/////   (golden armor, custom helmet, sword) before they were ever applied.
/////
/////   FIX: OnSoldierSpawned() now:
/////     1. Saves which buttons are selected BEFORE switching soldier
/////     2. Switches soldierEquipment to the new soldier
/////     3. Calls ApplyDefaultSelections() for visual button reset
/////     4. Re-equips the saved items on the NEW soldier  ← THE FIX
/////
///// ── How equipping works ───────────────────────────────────────────────────────
/////   InventorySlotButton.Select()
/////     → CharacterEquipment.Equip(item)           (applies stat bonuses)
/////     → CharacterVisuals.SetSprite(slot, frame0) (swaps the Image sprite)
/////     → SpriteLayerAnimator advances frames       (runs idle/walk animation)
/////   SoldierStats fires OnStatsChanged
/////     → InventoryPanel.RefreshStats() → bars + text update live
/////
///// ── Inspector wiring ─────────────────────────────────────────────────────────
/////   soldierEquipment → leave EMPTY — found automatically at runtime
/////
/////   Item Groups → drag each slot's button-parent GO:
/////     groupPlayer  = BodyType buttons parent
/////     groupHead    = Face buttons parent
/////     groupHair    = Hair buttons parent
/////     groupHelmet  = Helmet buttons parent
/////     groupArmor   = Armor buttons parent
/////     groupWeapon  = Weapon buttons parent
/////
/////   Tab Buttons → drag each tab Button (one per slot)
/////   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
/////
/////   ── Stats Panel ──────────────────────────────────────────────────────────
/////   healthBarFill  → Image (Filled, Horizontal, Left) for health
/////   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
/////   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
/////   healthText     → TextMeshProUGUI (shows MaxHealth number)
/////   abilityText    → TextMeshProUGUI (shows AbilityPower number)
/////   damageText     → TextMeshProUGUI (shows AttackDamage number)
///// </summary>
//public class InventoryPanel : MonoBehaviour
//{
//    // ─── Inspector — Soldier ──────────────────────────────────────────────────

//    [Header("Soldier — leave empty, found automatically")]
//    [SerializeField] private CharacterEquipment soldierEquipment;

//    // ─── Inspector — Item Groups ──────────────────────────────────────────────

//    [Header("Item Groups — one parent GO per slot")]
//    [SerializeField] private GameObject groupPlayer;   // BodyType
//    [SerializeField] private GameObject groupHead;     // Face
//    [SerializeField] private GameObject groupHair;
//    [SerializeField] private GameObject groupHelmet;
//    [SerializeField] private GameObject groupArmor;
//    [SerializeField] private GameObject groupWeapon;

//    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

//    [Header("Helmet hides Hair")]
//    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
//    [SerializeField] private GameObject hairVisualRoot;

//    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

//    [Header("Tab Buttons")]
//    [SerializeField] private Button tabPlayer;
//    [SerializeField] private Button tabHead;
//    [SerializeField] private Button tabHair;
//    [SerializeField] private Button tabHelmet;
//    [SerializeField] private Button tabArmor;
//    [SerializeField] private Button tabWeapon;

//    [Header("Tab Colours")]
//    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
//    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

//    // ─── Inspector — Stat Bars ────────────────────────────────────────────────

//    [Header("Stats Bars — wire to see live stat changes on equip")]
//    [SerializeField] private Image healthBarFill;
//    [SerializeField] private Image abilityBarFill;
//    [SerializeField] private Image damageBarFill;

//    [Header("Stats Labels (TextMeshPro — optional)")]
//    [SerializeField] private TextMeshProUGUI healthText;
//    [SerializeField] private TextMeshProUGUI abilityText;
//    [SerializeField] private TextMeshProUGUI damageText;

//    [Header("Bar Max Reference Values")]
//    [SerializeField] private float maxHealthDisplay = 200f;
//    [SerializeField] private float maxAbilityDisplay = 100f;
//    [SerializeField] private float maxDamageDisplay = 100f;

//    // ─── Private ──────────────────────────────────────────────────────────────

//    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
//    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

//    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

//    private void Awake()
//    {
//        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
//        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
//        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
//        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
//        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
//        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

//        GameManager.OnSoldierSpawned += OnSoldierSpawned;
//    }

//    private void OnDestroy()
//    {
//        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
//        UnsubscribeStats();
//    }

//    private void OnEnable()
//    {
//        if (soldierEquipment == null)
//            soldierEquipment = FindFirstObjectByType<CharacterEquipment>();

//        SubscribeStats();
//        ForceAllGroupsActive(true);
//        BuildGroupCache();
//        InitAllButtons();
//        ApplyDefaultSelections();
//        ShowSlot(_activeSlot);
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    private void OnDisable()
//    {
//        UnsubscribeStats();
//    }

//    // ─── Soldier Spawn Callback  ──────────────────────────────────────────────
//    //
//    // THIS IS THE KEY FIX.
//    //
//    // Old behaviour (broken):
//    //   soldierEquipment = newSoldier
//    //   ApplyDefaultSelections()    ← wiped golden armor, custom helmet, sword
//    //   // selected items NEVER applied to soldier
//    //
//    // New behaviour (fixed):
//    //   savedItems = GetSelectedItems()   ← capture user's choices FIRST
//    //   soldierEquipment = newSoldier
//    //   ApplyDefaultSelections()           ← resets button visuals (OK)
//    //   ApplyItemsToSoldier(savedItems)    ← re-equips choices on new soldier ✓

//    private void OnSoldierSpawned(GameObject soldierGO)
//    {
//        var newEquip = soldierGO.GetComponent<CharacterEquipment>();
//        if (newEquip == null)
//        {
//            Debug.LogWarning("[InventoryPanel] Spawned soldier has no CharacterEquipment — " +
//                             "add CharacterEquipment to the SolderPrefab.");
//            return;
//        }

//        // ── 1. Save current user selections BEFORE switching ──────────────────
//        var savedItems = GetSelectedItems();
//        Debug.Log($"[InventoryPanel] BUY pressed — saving {savedItems.Count} selected items.");

//        // ── 2. Switch to the new soldier ──────────────────────────────────────
//        UnsubscribeStats();
//        soldierEquipment = newEquip;

//        if (!gameObject.activeInHierarchy) return;

//        SubscribeStats();
//        ForceAllGroupsActive(true);
//        InitAllButtons();

//        // ── 3. Reset button visuals to defaults (nothing selected in armor/helmet/weapon) ──
//        ApplyDefaultSelections();

//        // ── 4. Re-equip saved items on the NEW soldier ← THE FIX ─────────────
//        if (savedItems.Count > 0)
//        {
//            ApplyItemsToSoldier(soldierEquipment, savedItems);
//            Debug.Log($"[InventoryPanel] Applied {savedItems.Count} items to spawned soldier.");
//        }
//        else
//        {
//            Debug.Log("[InventoryPanel] No items selected — soldier uses DefaultLoadout.");
//        }

//        ShowSlot(_activeSlot);
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    // ─── Public API ───────────────────────────────────────────────────────────

//    /// <summary>
//    /// Returns one selected EquipmentItem per slot (skips slots with nothing chosen).
//    /// Called by OnSoldierSpawned before the soldier reference switches.
//    /// </summary>
//    public Dictionary<EquipmentSlot, EquipmentItem> GetSelectedItems()
//    {
//        var result = new Dictionary<EquipmentSlot, EquipmentItem>();

//        foreach (var kvp in _groups)
//        {
//            foreach (var btn in kvp.Value)
//            {
//                if (btn.IsSelected && btn.Item != null)
//                {
//                    result[kvp.Key] = btn.Item;
//                    break;   // one per slot
//                }
//            }
//        }

//        return result;
//    }

//    /// <summary>
//    /// Equips a dictionary of items (slot → item) onto a CharacterEquipment target.
//    /// Also updates the matching buttons to IsSelected = true so the panel
//    /// reflects the soldier's loadout correctly.
//    /// </summary>
//    private void ApplyItemsToSoldier(CharacterEquipment target,
//                                     Dictionary<EquipmentSlot, EquipmentItem> items)
//    {
//        if (target == null) return;

//        foreach (var kvp in items)
//        {
//            EquipmentSlot slot = kvp.Key;
//            EquipmentItem item = kvp.Value;

//            // Apply to soldier's CharacterEquipment
//            // This updates visuals via CharacterVisuals.SetSprite() AND stats
//            target.Equip(item);

//            // ── Force-deselect ALL buttons for this slot ─────────────────────
//            // We deliberately do NOT call btn.Select() here.
//            // Calling Select() would leave the panel showing the previous
//            // purchase's armor/helmet/weapon as selected, so the NEXT time the
//            // user opens the panel and buys without changing anything, those items
//            // would be picked up by GetSelectedItems() and applied to the new
//            // soldier — even though the user wanted nothing.
//            // ForceDeselect() clears the IsSelected flag and hides the visual
//            // without calling Unequip (the item is already equipped above).
//            if (_groups.TryGetValue(slot, out var list))
//                foreach (var btn in list)
//                    btn.ForceDeselect();

//            Debug.Log($"[InventoryPanel] Equipped '{item.itemName}' ({slot}) on " +
//                      $"'{target.gameObject.name}'.");
//        }
//    }

//    /// <summary>
//    /// Called by InventorySlotButton — selects btn, deselects others in same slot.
//    /// </summary>
//    public void SelectButton(InventorySlotButton btn)
//    {
//        var list = FindGroup(btn);
//        if (list == null) return;

//        // ── Deselect others FIRST, then Select the target LAST ────────────────
//        // Calling Select() first then Deselect() on remaining buttons causes any
//        // shared playerVisualObject to be hidden again by the later Deselect()
//        // calls — making weapons 1-7 invisible while only the last (weapon 8) works.
//        foreach (var b in list)
//            if (b != btn) b.Deselect();

//        btn.Select(); // always the final call so SetActive(true) is never overwritten

//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    /// <summary>
//    /// Called by InventorySlotButton — deselects btn only.
//    /// </summary>
//    public void DeselectButton(InventorySlotButton btn)
//    {
//        if (btn.IsDefault) return;
//        btn.Deselect();
//        ApplyHairHelmetRule();
//        RefreshStats();
//    }

//    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

//    private void ApplyHairHelmetRule()
//    {
//        if (hairVisualRoot == null) return;

//        bool helmetOn = false;
//        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
//            foreach (var btn in helmetGroup)
//                if (btn.IsSelected) { helmetOn = true; break; }

//        hairVisualRoot.SetActive(!helmetOn);
//    }

//    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

//    private void ShowSlot(EquipmentSlot slot)
//    {
//        _activeSlot = slot;
//        UpdateTabColours();
//        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
//        SetActive(groupHead, slot == EquipmentSlot.Face);
//        SetActive(groupHair, slot == EquipmentSlot.Hair);
//        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
//        SetActive(groupArmor, slot == EquipmentSlot.Armor);
//        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
//    }

//    private void ForceAllGroupsActive(bool active)
//    {
//        SetActive(groupPlayer, active);
//        SetActive(groupHead, active);
//        SetActive(groupHair, active);
//        SetActive(groupHelmet, active);
//        SetActive(groupArmor, active);
//        SetActive(groupWeapon, active);
//    }

//    private static void SetActive(GameObject go, bool active)
//    {
//        if (go != null) go.SetActive(active);
//    }

//    // ─── Tab Colours ──────────────────────────────────────────────────────────

//    private void UpdateTabColours()
//    {
//        Tint(tabPlayer, EquipmentSlot.BodyType);
//        Tint(tabHead, EquipmentSlot.Face);
//        Tint(tabHair, EquipmentSlot.Hair);
//        Tint(tabHelmet, EquipmentSlot.Helmet);
//        Tint(tabArmor, EquipmentSlot.Armor);
//        Tint(tabWeapon, EquipmentSlot.Weapon);
//    }

//    private void Tint(Button btn, EquipmentSlot slot)
//    {
//        if (btn == null) return;
//        var img = btn.GetComponent<Image>();
//        if (img != null)
//            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
//    }

//    // ─── Stats Refresh ────────────────────────────────────────────────────────

//    private void RefreshStats()
//    {
//        var stats = GetStats();
//        if (stats == null) return;

//        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
//        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
//        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

//        if (healthBarFill != null) healthBarFill.fillAmount = hp;
//        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
//        if (damageBarFill != null) damageBarFill.fillAmount = ad;

//        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
//        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
//        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
//    }

//    // ─── Stats Subscription ───────────────────────────────────────────────────

//    private void SubscribeStats()
//    {
//        var s = GetStats();
//        if (s != null) s.OnStatsChanged += OnStatsChanged;
//    }

//    private void UnsubscribeStats()
//    {
//        var s = GetStats();
//        if (s != null) s.OnStatsChanged -= OnStatsChanged;
//    }

//    private void OnStatsChanged(SoldierStats _) => RefreshStats();

//    private SoldierStats GetStats() =>
//        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

//    // ─── Group Cache ──────────────────────────────────────────────────────────

//    private void BuildGroupCache()
//    {
//        _groups.Clear();
//        Cache(EquipmentSlot.BodyType, groupPlayer);
//        Cache(EquipmentSlot.Face, groupHead);
//        Cache(EquipmentSlot.Hair, groupHair);
//        Cache(EquipmentSlot.Helmet, groupHelmet);
//        Cache(EquipmentSlot.Armor, groupArmor);
//        Cache(EquipmentSlot.Weapon, groupWeapon);
//    }

//    private void Cache(EquipmentSlot slot, GameObject group)
//    {
//        var list = new List<InventorySlotButton>();
//        if (group != null)
//            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
//        _groups[slot] = list;
//    }

//    // ─── Button Init ──────────────────────────────────────────────────────────

//    private void InitAllButtons()
//    {
//        foreach (var kvp in _groups)
//            foreach (var btn in kvp.Value)
//                btn.Init(this, soldierEquipment);
//    }

//    // ─── Default Selections ───────────────────────────────────────────────────

//    private void ApplyDefaultSelections()
//    {
//        foreach (var kvp in _groups)
//        {
//            var slot = kvp.Key;
//            var list = kvp.Value;
//            if (list.Count == 0) continue;

//            if (slot == EquipmentSlot.BodyType)
//            {
//                foreach (var btn in list)
//                {
//                    if (btn.IsDefault) btn.Select();
//                    else btn.Deselect();
//                }
//            }
//            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
//            {
//                for (int i = 0; i < list.Count; i++)
//                {
//                    if (i == 0) list[i].Select();
//                    else list[i].Deselect();
//                }
//            }
//            else
//            {
//                // Armor, Helmet, Weapon — nothing selected by default
//                foreach (var btn in list) btn.Deselect();
//            }
//        }
//    }

//    // ─── Helpers ──────────────────────────────────────────────────────────────

//    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
//    {
//        foreach (var kvp in _groups)
//            foreach (var btn in kvp.Value)
//                if (btn == target) return kvp.Value;
//        return null;
//    }

//    // ─── Open / Close ─────────────────────────────────────────────────────────

//    public void Open() => gameObject.SetActive(true);
//    public void Close() => gameObject.SetActive(false);
//    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - InventoryPanel  (v3 — per-soldier independence fix)
///
/// ── THE CORE RULE ─────────────────────────────────────────────────────────────
///   soldierEquipment ALWAYS equals previewSoldierEquipment.
///   It is set once in OnEnable and never changed again.
///   Deployed soldiers are NEVER pointed at by this field.
///
/// ── WHY THE PREVIOUS VERSION BROKE ───────────────────────────────────────────
///   OnSoldierSpawned() was switching soldierEquipment to the newly spawned
///   soldier. From that point onward, every button click in the panel called
///   Equip() on the deployed soldier — not the preview — causing:
///     • The previous soldier's appearance to change live mid-battle.
///     • The soldier to get "stuck" (SpriteLayerAnimator frame state corrupted
///       by an external Equip() call while the soldier was already patrolling).
///     • The first soldier to appear as the default (the panel was inactive when
///       OnSoldierSpawned ran, so the early-return guard fired and
///       ApplyItemsToSoldier was skipped entirely).
///     • Every purchase sharing equipment references with previous soldiers.
///
/// ── HOW THE FIX WORKS ─────────────────────────────────────────────────────────
///   OnSoldierSpawned():
///     1. Snapshots GetSelectedItems() while soldierEquipment is still the
///        preview player.
///     2. Stamps the snapshot directly onto the new soldier via
///        ApplyItemsToNewSoldier() — soldierEquipment is NEVER changed.
///     3. Calls ResetPanelForNextPurchase() to restore the preview player
///        to its default look and clear button states.
///
///   Each deployed soldier is independently configured from the snapshot taken
///   at purchase time. EquipmentItems are ScriptableObject assets (read-only
///   shared data), so multiple soldiers safely reference the same asset — their
///   independence comes from each CharacterEquipment having its own
///   Dictionary&lt;EquipmentSlot, EquipmentItem&gt; instance.
///
/// ── Inspector wiring ──────────────────────────────────────────────────────────
///   previewSoldierEquipment → drag the panel's preview CharacterEquipment here
///                             (the character visible inside the Army panel)
///
///   Item Groups → drag each slot's button-parent GO:
///     groupPlayer  = BodyType buttons parent
///     groupHead    = Face buttons parent
///     groupHair    = Hair buttons parent
///     groupHelmet  = Helmet buttons parent
///     groupArmor   = Armor buttons parent
///     groupWeapon  = Weapon buttons parent
///
///   Tab Buttons → drag each tab Button (one per slot)
///   hairVisualRoot → drag Player/Hair (hidden when a helmet is equipped)
///
///   ── Stats Panel ──────────────────────────────────────────────────────────
///   healthBarFill  → Image (Filled, Horizontal, Left) for health
///   abilityBarFill → Image (Filled, Horizontal, Left) for ability power
///   damageBarFill  → Image (Filled, Horizontal, Left) for attack damage
///   healthText     → TextMeshProUGUI (shows MaxHealth number)
///   abilityText    → TextMeshProUGUI (shows AbilityPower number)
///   damageText     → TextMeshProUGUI (shows AttackDamage number)
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    // ─── Inspector — Preview Soldier ──────────────────────────────────────────
    //
    //  !! IMPORTANT — SET THIS IN THE INSPECTOR !!
    //
    //  Drag the CharacterEquipment component of the soldier preview character
    //  that lives INSIDE this panel here.
    //
    //  This is the ONLY soldier the panel ever modifies during customization.
    //  Deployed soldiers are stamped with a one-time snapshot at purchase and
    //  then completely left alone forever.
    //
    [Header("Preview Soldier — REQUIRED: drag the panel's preview CharacterEquipment here")]
    [Tooltip("The character shown inside this panel for customization preview.\n\n" +
             "ONLY this object is ever modified by clicking item buttons.\n" +
             "Deployed soldiers receive a one-time equipment snapshot at purchase\n" +
             "and are then permanently independent — never touched by this panel again.")]
    [SerializeField] private CharacterEquipment previewSoldierEquipment;

    // Runtime-only reference. Always equals previewSoldierEquipment after OnEnable.
    // Private (no SerializeField) so it cannot accidentally be wired to a deployed soldier.
    private CharacterEquipment soldierEquipment;

    // ─── Inspector — Item Groups ──────────────────────────────────────────────

    [Header("Item Groups — one parent GO per slot")]
    [SerializeField] private GameObject groupPlayer;   // BodyType
    [SerializeField] private GameObject groupHead;     // Face
    [SerializeField] private GameObject groupHair;
    [SerializeField] private GameObject groupHelmet;
    [SerializeField] private GameObject groupArmor;
    [SerializeField] private GameObject groupWeapon;

    // ─── Inspector — Helmet/Hair rule ─────────────────────────────────────────

    [Header("Helmet hides Hair")]
    [Tooltip("Drag Player/Hair here — hidden when any helmet is selected.")]
    [SerializeField] private GameObject hairVisualRoot;

    // ─── Inspector — Tab Buttons ──────────────────────────────────────────────

    [Header("Tab Buttons")]
    [SerializeField] private Button tabPlayer;
    [SerializeField] private Button tabHead;
    [SerializeField] private Button tabHair;
    [SerializeField] private Button tabHelmet;
    [SerializeField] private Button tabArmor;
    [SerializeField] private Button tabWeapon;

    [Header("Tab Colours")]
    [SerializeField] private Color tabActiveColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f);

    // ─── Inspector — Stat Bars ────────────────────────────────────────────────

    [Header("Stats Bars — wire to see live stat changes on equip")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image abilityBarFill;
    [SerializeField] private Image damageBarFill;

    [Header("Stats Labels (TextMeshPro — optional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Bar Max Reference Values")]
    [SerializeField] private float maxHealthDisplay = 200f;
    [SerializeField] private float maxAbilityDisplay = 100f;
    [SerializeField] private float maxDamageDisplay = 100f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private EquipmentSlot _activeSlot = EquipmentSlot.Armor;
    private readonly Dictionary<EquipmentSlot, List<InventorySlotButton>> _groups = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        tabPlayer?.onClick.AddListener(() => ShowSlot(EquipmentSlot.BodyType));
        tabHead?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Face));
        tabHair?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Hair));
        tabHelmet?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Helmet));
        tabArmor?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Armor));
        tabWeapon?.onClick.AddListener(() => ShowSlot(EquipmentSlot.Weapon));

        GameManager.OnSoldierSpawned += OnSoldierSpawned;
    }

    private void OnDestroy()
    {
        GameManager.OnSoldierSpawned -= OnSoldierSpawned;
        UnsubscribeStats();
    }

    private void OnEnable()
    {
        // ── FIX: always restore to the preview player, NEVER a deployed soldier ──
        //
        // Previous code did:
        //   if (soldierEquipment == null)
        //       soldierEquipment = FindFirstObjectByType<CharacterEquipment>();
        //
        // After the first purchase OnSoldierSpawned() set soldierEquipment to the
        // deployed soldier, so the null-check never triggered and the panel kept
        // pointing at the deployed soldier for all subsequent opens.
        //
        // Now: unconditionally set to the preview player every time the panel opens.
        soldierEquipment = previewSoldierEquipment;

        if (soldierEquipment == null)
        {
            // Fallback: if the inspector field is not wired yet, find any
            // CharacterEquipment. Log a warning so the developer knows to fix it.
            soldierEquipment = FindFirstObjectByType<CharacterEquipment>();
            if (soldierEquipment != null)
                Debug.LogWarning(
                    "[InventoryPanel] previewSoldierEquipment is not assigned in the Inspector!\n" +
                    "Drag the panel's preview CharacterEquipment into the 'Preview Soldier' field.\n" +
                    "Using FindFirstObjectByType as a fallback — this may grab a deployed soldier.");
        }

        SubscribeStats();
        ForceAllGroupsActive(true);
        BuildGroupCache();
        InitAllButtons();
        ApplyDefaultSelections();
        ShowSlot(_activeSlot);
        ApplyHairHelmetRule();
        RefreshStats();
    }

    private void OnDisable()
    {
        UnsubscribeStats();
    }

    // ─── Soldier Spawn Callback ───────────────────────────────────────────────
    //
    //  Called by GameManager.OnSoldierSpawned when the player clicks Buy.
    //
    //  CONTRACT:
    //    • soldierEquipment is the preview player throughout this method.
    //    • soldierEquipment is NEVER reassigned here or anywhere else.
    //    • The new soldier receives a one-time snapshot and is then left alone.
    //
    private void OnSoldierSpawned(GameObject soldierGO)
    {
        var newEquip = soldierGO.GetComponent<CharacterEquipment>();
        if (newEquip == null)
        {
            Debug.LogWarning("[InventoryPanel] Spawned soldier has no CharacterEquipment — " +
                             "add CharacterEquipment to the soldier prefab.");
            return;
        }

        // ── Step 1: Snapshot the user's selections from the panel. ────────────
        //    soldierEquipment is the preview player here — GetSelectedItems()
        //    reads IsSelected from the buttons, not from soldierEquipment, so
        //    this is correct regardless of panel active state.
        var snapshot = GetSelectedItems();
        Debug.Log($"[InventoryPanel] Purchase — snapshotted {snapshot.Count} item(s).");

        // ── Step 2: Stamp the snapshot onto the new soldier. ─────────────────
        //    This is a one-way write. soldierEquipment (preview player) is
        //    NEVER changed. The new soldier is permanently independent.
        ApplyItemsToNewSoldier(newEquip, snapshot);

        // ── Step 3: Reset the panel for the next purchase. ───────────────────
        //    Unequip Armor/Helmet/Weapon from the preview player so it shows
        //    the default look again. Re-apply default button selections.
        //    Only runs when the panel is visible — if the panel was closed
        //    before Buy fired, the reset happens the next time OnEnable runs.
        if (gameObject.activeInHierarchy)
            ResetPanelForNextPurchase();
    }

    // ─── Internal: Stamp snapshot onto a new soldier ─────────────────────────

    /// <summary>
    /// Stamps a dictionary of (slot → item) pairs onto a newly spawned soldier.
    ///
    /// This is a one-way write — the panel never references this soldier again.
    /// EquipmentItems are ScriptableObject assets (shared read-only data), so
    /// multiple soldiers can safely reference the same asset. Each soldier's
    /// CharacterEquipment has its own Dictionary&lt;EquipmentSlot, EquipmentItem&gt;
    /// instance, which is what makes them fully independent.
    ///
    /// Calling Equip() here also sets _customLoadoutApplied = true on the new
    /// soldier's CharacterEquipment, so its Start() skips ApplyLoadout() and
    /// never overwrites the custom equipment with the defaultLoadout.
    /// </summary>
    private void ApplyItemsToNewSoldier(CharacterEquipment target,
                                        Dictionary<EquipmentSlot, EquipmentItem> items)
    {
        if (target == null) return;

        foreach (var kvp in items)
        {
            target.Equip(kvp.Value);
            Debug.Log($"[InventoryPanel] → '{kvp.Value.itemName}' ({kvp.Key})" +
                      $" stamped on '{target.gameObject.name}'.");
        }
    }

    // ─── Internal: Reset panel after purchase ────────────────────────────────

    /// <summary>
    /// Restores the preview player to its default loadout appearance and resets
    /// button states so the panel is clean for the next soldier purchase.
    ///
    /// soldierEquipment (preview player) remains the panel's target throughout.
    /// Only the equipment on that preview player is changed.
    /// </summary>
    private void ResetPanelForNextPurchase()
    {
        // Unequip the customizable slots from the preview player so it goes
        // back to looking like an unequipped soldier.
        // Body / Face / Hair are re-applied by ApplyDefaultSelections() below.
        soldierEquipment?.Unequip(EquipmentSlot.Armor);
        soldierEquipment?.Unequip(EquipmentSlot.Helmet);
        soldierEquipment?.Unequip(EquipmentSlot.Weapon);

        // Rebuild the button cache and restore default visual state.
        InitAllButtons();
        ApplyDefaultSelections();
        ShowSlot(_activeSlot);
        ApplyHairHelmetRule();
        RefreshStats();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns one selected EquipmentItem per slot (skips slots with nothing chosen).
    /// Reads IsSelected flags from buttons — does NOT depend on soldierEquipment.
    /// </summary>
    public Dictionary<EquipmentSlot, EquipmentItem> GetSelectedItems()
    {
        var result = new Dictionary<EquipmentSlot, EquipmentItem>();

        foreach (var kvp in _groups)
        {
            foreach (var btn in kvp.Value)
            {
                if (btn.IsSelected && btn.Item != null)
                {
                    result[kvp.Key] = btn.Item;
                    break;   // one per slot
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Called by InventorySlotButton — selects btn, deselects others in same slot.
    /// Affects only the preview player (soldierEquipment).
    /// </summary>
    public void SelectButton(InventorySlotButton btn)
    {
        var list = FindGroup(btn);
        if (list == null) return;

        // Deselect others FIRST, then Select the target LAST.
        // Calling Select() first then Deselect() on remaining buttons causes any
        // shared playerVisualObject to be hidden again by the later Deselect()
        // calls — making item visuals invisible while only the last button works.
        foreach (var b in list)
            if (b != btn) b.Deselect();

        btn.Select(); // always the final call so SetActive(true) is never overwritten

        ApplyHairHelmetRule();
        RefreshStats();
    }

    /// <summary>
    /// Called by InventorySlotButton — deselects btn only.
    /// </summary>
    public void DeselectButton(InventorySlotButton btn)
    {
        if (btn.IsDefault) return;
        btn.Deselect();
        ApplyHairHelmetRule();
        RefreshStats();
    }

    // ─── Helmet / Hair Rule ───────────────────────────────────────────────────

    private void ApplyHairHelmetRule()
    {
        if (hairVisualRoot == null) return;

        bool helmetOn = false;
        if (_groups.TryGetValue(EquipmentSlot.Helmet, out var helmetGroup))
            foreach (var btn in helmetGroup)
                if (btn.IsSelected) { helmetOn = true; break; }

        hairVisualRoot.SetActive(!helmetOn);
    }

    // ─── Tab / Show Logic ─────────────────────────────────────────────────────

    private void ShowSlot(EquipmentSlot slot)
    {
        _activeSlot = slot;
        UpdateTabColours();
        SetActive(groupPlayer, slot == EquipmentSlot.BodyType);
        SetActive(groupHead, slot == EquipmentSlot.Face);
        SetActive(groupHair, slot == EquipmentSlot.Hair);
        SetActive(groupHelmet, slot == EquipmentSlot.Helmet);
        SetActive(groupArmor, slot == EquipmentSlot.Armor);
        SetActive(groupWeapon, slot == EquipmentSlot.Weapon);
    }

    private void ForceAllGroupsActive(bool active)
    {
        SetActive(groupPlayer, active);
        SetActive(groupHead, active);
        SetActive(groupHair, active);
        SetActive(groupHelmet, active);
        SetActive(groupArmor, active);
        SetActive(groupWeapon, active);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ─── Tab Colours ──────────────────────────────────────────────────────────

    private void UpdateTabColours()
    {
        Tint(tabPlayer, EquipmentSlot.BodyType);
        Tint(tabHead, EquipmentSlot.Face);
        Tint(tabHair, EquipmentSlot.Hair);
        Tint(tabHelmet, EquipmentSlot.Helmet);
        Tint(tabArmor, EquipmentSlot.Armor);
        Tint(tabWeapon, EquipmentSlot.Weapon);
    }

    private void Tint(Button btn, EquipmentSlot slot)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = (_activeSlot == slot) ? tabActiveColor : tabInactiveColor;
    }

    // ─── Stats Refresh ────────────────────────────────────────────────────────

    private void RefreshStats()
    {
        var stats = GetStats();
        if (stats == null) return;

        float hp = Mathf.Clamp01(stats.MaxHealth / Mathf.Max(1f, maxHealthDisplay));
        float ap = Mathf.Clamp01(stats.AbilityPower / Mathf.Max(1f, maxAbilityDisplay));
        float ad = Mathf.Clamp01(stats.AttackDamage / Mathf.Max(1f, maxDamageDisplay));

        if (healthBarFill != null) healthBarFill.fillAmount = hp;
        if (abilityBarFill != null) abilityBarFill.fillAmount = ap;
        if (damageBarFill != null) damageBarFill.fillAmount = ad;

        if (healthText != null) healthText.text = Mathf.RoundToInt(stats.MaxHealth).ToString();
        if (abilityText != null) abilityText.text = Mathf.RoundToInt(stats.AbilityPower).ToString();
        if (damageText != null) damageText.text = Mathf.RoundToInt(stats.AttackDamage).ToString();
    }

    // ─── Stats Subscription ───────────────────────────────────────────────────

    private void SubscribeStats()
    {
        var s = GetStats();
        if (s != null) s.OnStatsChanged += OnStatsChanged;
    }

    private void UnsubscribeStats()
    {
        var s = GetStats();
        if (s != null) s.OnStatsChanged -= OnStatsChanged;
    }

    private void OnStatsChanged(SoldierStats _) => RefreshStats();

    private SoldierStats GetStats() =>
        soldierEquipment != null ? soldierEquipment.GetComponent<SoldierStats>() : null;

    // ─── Group Cache ──────────────────────────────────────────────────────────

    private void BuildGroupCache()
    {
        _groups.Clear();
        Cache(EquipmentSlot.BodyType, groupPlayer);
        Cache(EquipmentSlot.Face, groupHead);
        Cache(EquipmentSlot.Hair, groupHair);
        Cache(EquipmentSlot.Helmet, groupHelmet);
        Cache(EquipmentSlot.Armor, groupArmor);
        Cache(EquipmentSlot.Weapon, groupWeapon);
    }

    private void Cache(EquipmentSlot slot, GameObject group)
    {
        var list = new List<InventorySlotButton>();
        if (group != null)
            list.AddRange(group.GetComponentsInChildren<InventorySlotButton>(true));
        _groups[slot] = list;
    }

    // ─── Button Init ──────────────────────────────────────────────────────────

    private void InitAllButtons()
    {
        // soldierEquipment is always the preview player here — buttons will
        // only ever drive the preview character, not any deployed soldier.
        foreach (var kvp in _groups)
            foreach (var btn in kvp.Value)
                btn.Init(this, soldierEquipment);
    }

    // ─── Default Selections ───────────────────────────────────────────────────

    private void ApplyDefaultSelections()
    {
        foreach (var kvp in _groups)
        {
            var slot = kvp.Key;
            var list = kvp.Value;
            if (list.Count == 0) continue;

            if (slot == EquipmentSlot.BodyType)
            {
                foreach (var btn in list)
                {
                    if (btn.IsDefault) btn.Select();
                    else btn.Deselect();
                }
            }
            else if (slot == EquipmentSlot.Face || slot == EquipmentSlot.Hair)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == 0) list[i].Select();
                    else list[i].Deselect();
                }
            }
            else
            {
                // Armor, Helmet, Weapon — nothing selected by default.
                // The preview player had these slots unequipped by ResetPanelForNextPurchase().
                foreach (var btn in list) btn.Deselect();
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private List<InventorySlotButton> FindGroup(InventorySlotButton target)
    {
        foreach (var kvp in _groups)
            foreach (var btn in kvp.Value)
                if (btn == target) return kvp.Value;
        return null;
    }

    // ─── Open / Close ─────────────────────────────────────────────────────────

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
}