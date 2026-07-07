using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// One shared set of unit prefabs — used for BOTH the player and bot sides.
/// Assign these once on BattleManager; BotArmyGenerator receives the same
/// references at Start(), so there is only ever one prefab per unit type
/// in the whole project.
/// </summary>
[System.Serializable]
public class BattleUnitPrefabs
{
    public GameObject soldierPrefab;
    public GameObject archerPrefab;
    public GameObject dragonPrefab;  // dragon with rider visuals wired in

    [Tooltip("All 3 HorseData assets (e.g. Brown/Black/White). Each one's own " +
             ".prefab field is used directly — no separate battle-only horse prefabs needed.")]
    public HorseData[] horseTypes;

    [Tooltip("All 3 CannonData assets. Each one's own .prefab field is used " +
             "directly — no separate battle-only cannon prefabs needed.")]
    public CannonData[] cannonTypes;
}

/// <summary>
/// Pool of equipment items the bot side picks randomly from when spawning a
/// Horse or Dragon unit, so bot riders look varied instead of either bare
/// or an exact clone of the player's own soldier.
/// </summary>
[System.Serializable]
public class RiderLoadoutPool
{
    public EquipmentItem[] faces;
    public EquipmentItem[] armors;
    public EquipmentItem[] helmets;
    public EquipmentItem[] weapons;
}

/// <summary>
/// BattleManager
///
/// Master controller for the Battle scene. Place on a single empty
/// GameObject called "BattleManager" in the Battle scene.
///
/// Responsibilities:
///   • Reads BattleSaveData and spawns the player army (left side).
///   • Tells BotCastleGenerator and BotArmyGenerator to build the right side.
///   • Provides FindNearestEnemy() so BattleUnit.Update can target.
///   • Tracks alive units and declares Win / Lose when one side is gone.
///   • Handles the Win/Lose panel UI and the Return button.
///
/// ════════════════════════════════════════════════════════════════════════
///  REQUIRED HIERARCHY in Battle scene Canvas
/// ════════════════════════════════════════════════════════════════════════
///
///  Canvas
///  ├── PlayerSide              ← RectTransform, anchored left half
///  │   ├── PlayerCastleRoot    ← PlayerCastleBuilder (rebuilds the EXACT
///  │   │                          Village castle shape, cannons/archers
///  │   │                          included)
///  │   └── PlayerArmyRoot      ← soldiers / horses / dragons spawn here
///  ├── BotSide                 ← RectTransform, anchored right half
///  │   ├── BotCastleRoot       ← BotCastleGenerator (flipHorizontally ON)
///  │   └── BotArmyRoot         ← BotArmyGenerator
///  └── ResultPanel             ← starts inactive
///      ├── WinText
///      ├── LoseText
///      └── ReturnButton
///
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────────────────

    [Header("Player Side")]
    [SerializeField] private Transform playerArmyRoot;
    [Tooltip("Where the carried-over CastleGridPanel gets reparented into. " +
             "BattlePanel → PlayerSide → PlayerCastleRoot.")]
    [SerializeField] private RectTransform playerCastleRoot;
    [Tooltip("The fixed anchoredPosition SoldierSpawnArea is placed at once it's " +
             "reparented into PlayerArmyRoot (X = -603, Y = -53 by default).")]
    [SerializeField] private Vector2 soldierSpawnAreaPosition = new Vector2(-603f, -53f);
    [Tooltip("Shared prefabs — also passed to BotArmyGenerator for the bot side.")]
    [SerializeField] private BattleUnitPrefabs unitPrefabs;
    [Tooltip("Random rider looks the bot side picks from for Horse/Dragon units.")]
    [SerializeField] private RiderLoadoutPool botRiderLoadouts;

    [Header("Spawn Layout (Player)")]
    [SerializeField] private float playerStartX = -400f;
    [SerializeField] private float playerUnitSpacingX = 80f;
    [SerializeField] private float playerUnitSpacingY = 60f;
    [SerializeField] private int playerUnitsPerRow = 3;
    [Tooltip("Extra Y offset (pixels) applied only to freshly-spawned Horse units. " +
             "Horses are the only unit type that both (a) isn't carried over as a " +
             "live GameObject like Soldiers, and (b) doesn't immediately reposition " +
             "itself like the Dragon's rise-off-ground animation — so a pivot that " +
             "isn't bottom-anchored on the horse prefab shows up here as 'floating'. " +
             "Tune this negative (e.g. -30) to drop the horse down onto the ground " +
             "line instead of editing the prefab's RectTransform pivot.")]
    [SerializeField] private float horseGroundOffsetY = 0f;

    /// <summary>
    /// Public read access so BotArmyGenerator can reuse this EXACT value
    /// instead of keeping its own separate SerializeField copy. Two copies
    /// of the same tuning value silently drift apart the moment one is
    /// edited and the other isn't — which is exactly how the bot-side horse
    /// ended up sitting at y=0 (its own copy was still at the pre-fix
    /// default of 0) while the player-side horse correctly used -30.
    /// </summary>
    public float HorseGroundOffsetY => horseGroundOffsetY;

    [Header("Bot Side")]
    [SerializeField] private BotCastleGenerator botCastleGenerator;
    [SerializeField] private BotArmyGenerator botArmyGenerator;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;

    [Header("Scene")]
    [SerializeField] private string villageSceneName = "Village";

    [Header("Battlefield Bounds")]
    [Tooltip("Empty RectTransform marking the play area units are allowed to " +
             "walk within (sibling of PlayerSide/BotSide under Canvas — see " +
             "setup notes above BattleUnit's bounds-clamping code). Without " +
             "this, a moving unit (Soldier/Horse) chasing a target has no " +
             "limit and can walk straight off the visible battlefield. Left " +
             "unassigned = no clamp (fails safe to old behaviour).")]
    [SerializeField] private RectTransform battlefieldBounds;

    /// <summary>
    /// Public read access so BattleUnit can clamp its own walk movement
    /// against the same bounds every unit shares, instead of each unit
    /// needing its own separate reference wired in the Inspector.
    /// </summary>
    public RectTransform BattlefieldBounds => battlefieldBounds;

    // ── State ─────────────────────────────────────────────────────────────────

    private List<BattleUnit> _playerUnits = new List<BattleUnit>();
    private List<BattleUnit> _botUnits = new List<BattleUnit>();
    private bool _battleOver;

    /// <summary>
    /// How many Dragon-type BattleUnitData entries ReceivePlayerDragons()
    /// actually consumed via a carried-over live FlyZone. Set at the end of
    /// that method. SpawnPlayerArmy() uses this to tell "already handled"
    /// dragons apart from ones that never made it into a FlyZone before
    /// battle started — those still have their data sitting in
    /// BattleSaveData.PlayerUnits but were previously just silently skipped.
    /// </summary>
    private int _dragonsCarriedLive = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void Start()
    {
        // Player castle — the ACTUAL CastleGridPanel the player built in the
        // Village, carried over via DontDestroyOnLoad and reparented here.
        // Must run before SpawnPlayerArmy() so cannons/archers can find their block.
        // x = rows, y = cols — the real dimensions of the player's castle.
        Vector2Int playerDimensions = ReceivePlayerCastle();

        // Foot soldiers — the ACTUAL soldier GameObjects the player placed in
        // the Village, carried over via DontDestroyOnLoad and reparented here.
        // Must run before SpawnPlayerArmy() so the Soldier entries in
        // BattleSaveData.PlayerUnits are consumed here instead of spawning
        // duplicate prefabs.
        ReceivePlayerSoldiers();

        // Live dragons (with a mounted rider) the player parked in a FlyZone —
        // carried over as real GameObjects by BattleStarter.CarryDragonsIntoBattle(),
        // same as the castle and soldiers above. Must run before SpawnPlayerArmy()
        // so its Dragon entries in BattleSaveData.PlayerUnits are consumed here
        // instead of spawning a duplicate fresh prefab.
        ReceivePlayerDragons();

        SpawnPlayerArmy();

        // Bot side — castle first, then army.
        // Reuses the SAME unitPrefabs as the player (no separate bot prefabs).
        botCastleGenerator?.Generate(playerDimensions.x, playerDimensions.y);
        botArmyGenerator?.Generate(
            BattleSaveData.PlayerUnits.Count,
            unitPrefabs,
            botRiderLoadouts,
            // Bot cannons/archers seat directly into these generated
            // CastleBlockUnitSlot zones — the SAME CastleUnitDropZone /
            // ArcherZoneCastle components the player's castle uses.
            botCastleGenerator != null ? botCastleGenerator.GeneratedUnitSlots : null);

        // Collect bot units after generation.
        if (botArmyGenerator != null)
            _botUnits.AddRange(botArmyGenerator.SpawnedUnits);
    }

    /// <summary>
    /// Reparents the carried-over CastleGrid into PlayerCastleRoot and
    /// centers it there. Returns the player's real castle dimensions
    /// (x = rows, y = cols) so the bot castle can be sized to match.
    /// Falls back to BattleSaveData.PlayerBlockPositions if nothing carried
    /// over (e.g. testing the Battle scene directly without going through
    /// the Village).
    /// </summary>
    private Vector2Int ReceivePlayerCastle()
    {
        CastleGrid grid = CastleGrid.Instance;

        Debug.Log($"[BattleManager] ReceivePlayerCastle — CastleGrid.Instance is " +
                  $"{(grid == null ? "NULL" : grid.gameObject.name)}, " +
                  $"playerCastleRoot is {(playerCastleRoot == null ? "NULL" : playerCastleRoot.name)}.");

        if (grid == null || playerCastleRoot == null)
        {
            Debug.LogWarning("[BattleManager] No carried CastleGrid found — falling back to saved block positions only.");
            return GetCastleDimensions(BattleSaveData.PlayerBlockPositions);
        }

        grid.transform.SetParent(playerCastleRoot, false);

        RectTransform rt = grid.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        Debug.Log($"[BattleManager] Reparented '{grid.gameObject.name}' under " +
                  $"'{playerCastleRoot.name}'. Actual parent now: {grid.transform.parent.name}. " +
                  $"Block count: {grid.GetPlacedBlockCount()}.");

        return GetCastleDimensions(grid.GetPlacedBlockPositions());
    }

    /// <summary>
    /// Reparents the carried-over SoldierSpawnArea (detached and
    /// DontDestroyOnLoad'd by BattleStarter.CarrySoldiersIntoBattle()) into
    /// PlayerArmyRoot at the fixed battle position, then turns every real
    /// soldier GameObject inside it into a live BattleUnit — same soldier,
    /// same equipment, same visuals, just handed off from village
    /// patrol/drag behaviour to battle combat behaviour.
    ///
    /// Falls back to doing nothing (SpawnPlayerArmy's data-driven path takes
    /// over) if no SoldierSpawnArea was carried — e.g. testing the Battle
    /// scene directly without going through the Village.
    /// </summary>
    private void ReceivePlayerSoldiers()
    {
        // NOTE: CastleGridMover.Instance is NOT valid here — CastleGridMover
        // lived in the Village scene and was destroyed with it on the scene
        // change (it never DontDestroyOnLoad's itself, only the SoldierSpawnArea
        // it hands off does). The reference has to come from BattleSaveData,
        // which BattleStarter populated right before the scene load.
        RectTransform spawnArea = BattleSaveData.CarriedSoldierSpawnArea;

        if (spawnArea == null || playerArmyRoot == null)
        {
            Debug.LogWarning("[BattleManager] No carried SoldierSpawnArea found — " +
                              "falling back to data-driven soldier spawn only.");
            return;
        }

        spawnArea.SetParent(playerArmyRoot, false);
        spawnArea.anchorMin = new Vector2(0.5f, 0.5f);
        spawnArea.anchorMax = new Vector2(0.5f, 0.5f);
        spawnArea.pivot = new Vector2(0.5f, 0.5f);
        spawnArea.anchoredPosition = soldierSpawnAreaPosition;
        spawnArea.localScale = Vector3.one;

        Debug.Log($"[BattleManager] Reparented '{spawnArea.name}' under " +
                  $"'{playerArmyRoot.name}' at {soldierSpawnAreaPosition}.");

        // Matching stat entries gathered by BattleStarter — consumed in
        // order so each carried soldier still gets its configured HP/damage/
        // speed instead of duplicating those inspector fields here.
        var soldierData = BattleSaveData.PlayerUnits.FindAll(
            u => u.unitType == BattleUnitType.Soldier);
        int nextData = 0;

        var soldiers = spawnArea.GetComponentsInChildren<SoldierController>(true);
        foreach (var soldier in soldiers)
        {
            GameObject go = soldier.gameObject;

            // Mounted soldiers are hidden (SetActive(false)) by HorseController/
            // DragonController and travel as part of the Horse/Dragon unit
            // instead — skip them here, BattleStarter already packed them as
            // Horse/Dragon BattleUnitData entries.
            if (!go.activeInHierarchy) continue;

            // Pull the soldier OUT of SoldierSpawnArea and directly onto
            // PlayerArmyRoot, preserving its current visual spot
            // (worldPositionStays: true recomputes anchoredPosition for the
            // new parent). NOTE: distance/targeting itself no longer relies
            // on anchoredPosition at all (see BattleUnit.WorldX) — player and
            // bot units sit under two different roots (PlayerArmyRoot /
            // BotArmyRoot) at different screen positions, so their local
            // anchoredPosition was never comparable in the first place. This
            // reparent is still done for a clean, sane hierarchy and correct
            // *movement* origin, not for the distance math.
            go.transform.SetParent(playerArmyRoot, worldPositionStays: true);

            // Ground line — every soldier sits at a fixed Y regardless of
            // whatever Y its Village patrol happened to be at the moment
            // Start Battle was pressed (worldPositionStays above preserves
            // that original spot, X included, but Y needs to snap to the
            // battle ground line instead of wherever the soldier was
            // standing/patrolling in the Village).
            RectTransform soldierRt = go.GetComponent<RectTransform>();
            Vector2 pos = soldierRt.anchoredPosition;
            pos.y = -8f;
            soldierRt.anchoredPosition = pos;

            // Village-only interaction must be turned off so the soldier can't
            // be dragged or resume patrolling mid-battle.
            SoldierDragDrop dragDrop = go.GetComponent<SoldierDragDrop>();
            if (dragDrop != null) dragDrop.enabled = false;

            // enabled = false only stops Update() — it does NOT stop coroutines
            // SoldierController already started in OnEnable() (RestCycle /
            // CombatLoop / InitPatrol). Those keep writing to this soldier's
            // RectTransform every frame in the background, fighting BattleUnit
            // for control and making the soldier look stuck/pinned in place.
            // StopAllCoroutines() kills them for good before handing movement
            // over to BattleUnit.
            soldier.StopAllCoroutines();
            soldier.enabled = false;

            // Any drag/mount CanvasGroup.alpha left below 1 (mid-drag = 0.75,
            // hidden-for-mount = 0) must be restored, or the soldier is
            // invisible in battle despite existing in the hierarchy.
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            BattleUnitData data = nextData < soldierData.Count
                ? soldierData[nextData++]
                : new BattleUnitData(BattleUnitType.Soldier, 100f, 10f, 80f);

            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu == null) bu = go.AddComponent<BattleUnit>();
            bu.Init(data, playerUnit: true);
            _playerUnits.Add(bu);

            Debug.Log($"[BattleManager] Soldier '{go.name}' ready — " +
                      $"activeSelf={go.activeSelf}, enabled(SoldierController)={soldier.enabled}, " +
                      $"BattleUnit.enabled={bu.enabled}, parent={go.transform.parent?.name}, " +
                      $"anchoredPos={go.GetComponent<RectTransform>().anchoredPosition}");
        }

        Debug.Log($"[BattleManager] Carried over {_playerUnits.Count} live soldier unit(s).");

        // SoldierSpawnArea was only a carrier for the trip over — every
        // soldier has now been reparented directly onto playerArmyRoot, so
        // the (now empty) container is no longer needed in the Battle scene.
        Destroy(spawnArea.gameObject);
    }

    /// <summary>
    /// Pulls each carried-over dragon OUT of its FlyZone and reparents it
    /// directly onto PlayerArmyRoot — same hand-off idea as
    /// ReceivePlayerSoldiers() pulling soldiers out of SoldierSpawnArea, just
    /// for dragons. The FlyZone was only a carrier for the trip over (it has
    /// no meaning in the Battle scene, where BattleDragonFlight — not
    /// FlyZone — owns the dragon's movement), so once the dragon is
    /// extracted the now-empty FlyZone is destroyed, leaving the dragon a
    /// direct child of PlayerArmyRoot exactly like every other unit type.
    /// Falls back to doing nothing per entry if the FlyZone or its dragon
    /// didn't survive the trip (e.g. testing the Battle scene directly
    /// without going through the Village).
    /// </summary>
    private void ReceivePlayerDragons()
    {
        if (playerArmyRoot == null || BattleSaveData.CarriedDragonFlyZones.Count == 0)
            return;

        // Matching stat entries gathered by BattleStarter (health/damage/speed
        // for each dragon) — consumed in order, same pattern ReceivePlayerSoldiers()
        // uses for soldierData, so each carried dragon still gets its configured stats.
        var dragonData = BattleSaveData.PlayerUnits.FindAll(
            u => u.unitType == BattleUnitType.Dragon);
        int nextData = 0;

        foreach (RectTransform zoneRt in BattleSaveData.CarriedDragonFlyZones)
        {
            if (zoneRt == null) continue;

            DragonController dragon = zoneRt.GetComponentInChildren<DragonController>(true);
            if (dragon == null)
            {
                Destroy(zoneRt.gameObject);
                continue;
            }

            GameObject go = dragon.gameObject;

            // Pull the dragon OUT of the FlyZone and directly onto
            // PlayerArmyRoot. worldPositionStays: true (preserving the
            // dragon's exact on-screen spot through the reparent) is NOT
            // used here — the dragon's actual journey is FlyZone (nested
            // under the Village's Canvas) → unparented to scene root for
            // DontDestroyOnLoad → reparented again onto PlayerArmyRoot here.
            // If the scale factors along that chain don't cancel out
            // cleanly, worldPositionStays back-solves anchoredPosition into
            // an enormous garbage value (seen in practice: Y landing around
            // -2,480,058) instead of a sane on-screen spot — exactly why the
            // dragon "spawns" but is nowhere in the visible Game view. Soldiers
            // don't hit this because their carry-over path never passes
            // through a nested, differently-scaled container like a FlyZone.
            //
            // Fix: reparent WITHOUT preserving world position, then assign a
            // known-good battle position explicitly instead of trusting the
            // math to land somewhere reasonable. BattleDragonFlight's Rise
            // phase takes over from there, same as for a freshly-Instantiated
            // dragon — it only cares about anchoredPosition.y at Start(),
            // not about matching the Village's on-screen spot.
            go.transform.SetParent(playerArmyRoot, worldPositionStays: false);
            RectTransform dragonRt = go.GetComponent<RectTransform>();
            dragonRt.anchoredPosition = new Vector2(playerStartX, 0f);
            dragonRt.localScale = Vector3.one;
            Destroy(zoneRt.gameObject);

            // Village-only drag/patrol/combat behaviour must stop so it can't
            // fight BattleUnit/BattleDragonFlight for control mid-battle —
            // same reasoning as disabling SoldierDragDrop/SoldierController above.
            dragon.enabled = false;

            BattleUnitData data = nextData < dragonData.Count
                ? dragonData[nextData++]
                : new BattleUnitData(BattleUnitType.Dragon, 300f, 40f, 150f);

            // This dragon is the REAL, live GameObject — its rider visuals
            // (Face/Helmet/Armor/Weapon) are already showing correctly from
            // being mounted in the Village. BattleUnit.Init() would otherwise
            // add a second CharacterEquipment and re-apply them on top of the
            // ones already there, so strip the snapshot before handing it off.
            data.riderFace = null;
            data.riderArmor = null;
            data.riderHelmet = null;
            data.riderWeapon = null;

            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu == null) bu = go.AddComponent<BattleUnit>();
            bu.Init(data, playerUnit: true);
            _playerUnits.Add(bu);

            // BattleDragonFlight self-disabled in the Village (see its
            // OnEnable — BattleManager.Instance was null there). Re-enabling
            // it re-fires OnEnable. That USED to be enough on its own —
            // relying on Unity's Start() to fire once, now that the
            // component is enabled — but Start() only ever runs once per
            // component instance for the object's whole lifetime; if it
            // somehow already fired before (enable/disable edge cases),
            // re-enabling here would silently skip recomputing bounds/cruise
            // altitude, leaving this exact dragon (and only this one —
            // freshly-Instantiated bot/player dragons never hit this path)
            // permanently stuck and never breathing fire. Call
            // ActivateForBattle() explicitly so initialization always runs,
            // regardless of Start()'s own history.
            BattleDragonFlight flight = go.GetComponent<BattleDragonFlight>();
            if (flight != null)
            {
                flight.enabled = true;
                flight.ActivateForBattle();
            }

            Debug.Log($"[BattleManager] Carried-over dragon '{go.name}' ready for battle.");
        }

        // Remember exactly how many Dragon entries were consumed above so
        // SpawnPlayerArmy() can tell which (if any) leftover Dragon entries
        // in BattleSaveData.PlayerUnits were NOT carried — e.g. a dragon
        // that had a soldier mounted but was never dragged into a FlyZone
        // before Start Battle was pressed, so CarryDragonsIntoBattle() never
        // found it. Those leftovers get a fresh-spawned fallback instead of
        // silently vanishing (see SpawnPlayerArmy).
        _dragonsCarriedLive = nextData;
    }

    /// <summary>
    /// Turns a list of placed (row, col) block positions into the castle's
    /// real dimensions — x = rows, y = cols — by taking the bounding box of
    /// the staircase (max row + 1, max col + 1). Returns (1,1) if empty so
    /// the bot castle generator always has something sane to work with.
    /// </summary>
    private Vector2Int GetCastleDimensions(List<Vector2Int> blockPositions)
    {
        if (blockPositions == null || blockPositions.Count == 0)
            return new Vector2Int(1, 1);

        int maxRow = 0, maxCol = 0;
        foreach (var pos in blockPositions)
        {
            if (pos.x > maxRow) maxRow = pos.x;
            if (pos.y > maxCol) maxCol = pos.y;
        }

        return new Vector2Int(maxRow + 1, maxCol + 1);
    }

    // ── Player Army Spawning ──────────────────────────────────────────────────

    private void SpawnPlayerArmy()
    {
        if (playerArmyRoot == null)
        {
            Debug.LogError("[BattleManager] playerArmyRoot not assigned!");
            return;
        }

        // Separate counter for the flat army row — cannons/archers seated
        // directly on a castle block don't consume a row/col slot here, so
        // the remaining foot units/horses/dragons stay tightly packed.
        int flatIndex = 0;

        // How many Dragon-type entries we've walked past so far in this
        // loop — compared against _dragonsCarriedLive to tell an
        // already-carried dragon apart from one that never made it into a
        // FlyZone before battle started (see ReceivePlayerDragons).
        int dragonEntryIndex = 0;

        for (int i = 0; i < BattleSaveData.PlayerUnits.Count; i++)
        {
            BattleUnitData data = BattleSaveData.PlayerUnits[i];

            // Foot soldiers already carried over as live GameObjects and
            // turned into BattleUnits by ReceivePlayerSoldiers() — skip here
            // to avoid spawning a duplicate prefab copy.
            if (data.unitType == BattleUnitType.Soldier) continue;

            // Dragons: the first _dragonsCarriedLive entries (in list order)
            // were already carried over as live GameObjects and turned into
            // BattleUnits by ReceivePlayerDragons() — skip those to avoid a
            // duplicate. Any Dragon entry BEYOND that count means the
            // dragon had a soldier mounted but was never dragged into a
            // FlyZone before Start Battle was pressed, so it never got
            // carried — fall through to the generic Instantiate path below
            // instead of silently dropping it.
            if (data.unitType == BattleUnitType.Dragon)
            {
                dragonEntryIndex++;
                if (dragonEntryIndex <= _dragonsCarriedLive) continue;

                Debug.LogWarning($"[BattleManager] Dragon entry #{dragonEntryIndex} was never " +
                                  "carried over as a live FlyZone (probably wasn't dragged into " +
                                  "the airspace before Start Battle) — spawning a fresh copy instead.");
                // Falls through to the generic Instantiate block below.
            }

            // Cannons/archers already exist as real, live GameObjects on the
            // carried-over castle (they traveled over with it) — turn THOSE
            // into the combat unit instead of spawning a duplicate prefab.
            if (data.unitType == BattleUnitType.Cannon || data.unitType == BattleUnitType.Archer)
            {
                GameObject existing = FindExistingCastleUnit(data);
                if (existing == null)
                {
                    Debug.LogWarning($"[BattleManager] Couldn't find the live {data.unitType} " +
                                      $"at ({data.gridPosition.x},{data.gridPosition.y}) on the carried castle — skipping.");
                    continue;
                }

                BattleUnit existingBu = existing.GetComponent<BattleUnit>();
                if (existingBu == null) existingBu = existing.AddComponent<BattleUnit>();
                existingBu.Init(data, playerUnit: true);
                _playerUnits.Add(existingBu);
                continue;
            }

            GameObject prefab = GetPrefabFor(data);
            if (prefab == null)
            {
                // Silently continuing here used to make a unit (most often a
                // Dragon that never got carried over as a live FlyZone, so it
                // fell through to this generic spawn path) vanish with zero
                // trace in the console — looking exactly like "I added it in
                // the Village but it never shows up in Battle". Log WHY so
                // it's obvious this is a missing Inspector reference
                // (unitPrefabs.dragonPrefab / a HorseData.prefab / a
                // CannonData.prefab) rather than a mysterious disappearance.
                Debug.LogWarning($"[BattleManager] No prefab found for player {data.unitType} " +
                                  "unit — check that unitPrefabs (or the HorseData/CannonData " +
                                  "asset) has its prefab field assigned. Unit was skipped.");
                continue;
            }

            GameObject go = Instantiate(prefab, playerArmyRoot);

            // Grid layout — left to right, then upward.
            int col = flatIndex % playerUnitsPerRow;
            int row = flatIndex / playerUnitsPerRow;
            flatIndex++;

            RectTransform rt = go.GetComponent<RectTransform>();

            // Horses always sit flat on the ground line — never stacked into
            // a row like foot units. Using "row * spacing" here (like every
            // other type) made a horse's Y depend on how many other units
            // happened to spawn before it in this particular battle, so the
            // SAME horse could land at y=0 in one battle and y=60/120/etc. in
            // another purely by chance of spawn order. Forcing y=0 (instead
            // of row * playerUnitSpacingY + horseGroundOffsetY) keeps every
            // horse on the same ground line regardless of order.
            float y = data.unitType == BattleUnitType.Horse
                ? 0f
                : row * playerUnitSpacingY;

            rt.anchoredPosition = new Vector2(
                playerStartX + col * playerUnitSpacingX, y);

            // FIX — was previously "if (bu != null) { ... }", which meant
            // that if a prefab (e.g. a HorseData.prefab) didn't already
            // have a BattleUnit component sitting on it in the editor,
            // Init() silently never ran at all: no HorseController.Setup(),
            // no ApplyRiderVisuals(). That's exactly why a horse could show
            // up in battle with its default idle sprite and no visible
            // rider — every OTHER spawn path here (ReceivePlayerSoldiers,
            // FindExistingCastleUnit below) already adds the component if
            // it's missing; this generic path just hadn't matched that
            // pattern. Now it does.
            BattleUnit bu = go.GetComponent<BattleUnit>();
            if (bu == null) bu = go.AddComponent<BattleUnit>();
            bu.Init(data, playerUnit: true);
            _playerUnits.Add(bu);

            // A freshly-Instantiated Dragon prefab still carries its own
            // Village-only DragonController (drag/patrol/combat), same
            // class of conflict already handled for carried-over soldiers
            // (SoldierController) and carried-over dragons in
            // ReceivePlayerDragons(). This generic fallback path spawns a
            // Dragon that was never dragged into a FlyZone before Start
            // Battle, so ReceivePlayerDragons() never touched it — without
            // this, it's the one Dragon-type unit left with DragonController
            // still enabled in the Battle scene. No-ops safely for every
            // other unit type, which has no DragonController.
            if (data.unitType == BattleUnitType.Dragon)
            {
                var freshDragonController = go.GetComponent<DragonController>();
                if (freshDragonController != null)
                    freshDragonController.enabled = false;
            }
        }

        Debug.Log($"[BattleManager] Spawned {_playerUnits.Count} player units.");
    }

    /// <summary>
    /// Finds the actual cannon/archer GameObject already placed on the
    /// carried-over castle at the unit's saved grid position, so BattleManager
    /// can turn the real Village object into a combat unit instead of
    /// instantiating a fresh prefab on top of it.
    /// </summary>
    private GameObject FindExistingCastleUnit(BattleUnitData data)
    {
        if (!data.hasGridPosition || CastleGrid.Instance == null) return null;

        GridCell cell = CastleGrid.Instance.GetCell(data.gridPosition.x, data.gridPosition.y);
        if (cell == null) return null;

        if (data.unitType == BattleUnitType.Cannon)
        {
            foreach (var zone in cell.GetComponentsInChildren<CastleUnitDropZone>(true))
                if (zone.acceptedType == CastleUnitType.Cannon && zone.HasUnit)
                    return zone.PlacedInstance;
        }
        else if (data.unitType == BattleUnitType.Archer)
        {
            foreach (var zone in cell.GetComponentsInChildren<ArcherZoneCastle>(true))
                if (zone.IsOccupied)
                    return zone.ArcherInstance;
        }

        return null;
    }

    /// <summary>
    /// Resolves the correct prefab for a unit. Cannons use their own
    /// CannonData.prefab (one of the 3 types); everything else comes from
    /// the shared unitPrefabs set.
    /// </summary>
    private GameObject GetPrefabFor(BattleUnitData data)
    {
        if (data.unitType == BattleUnitType.Cannon)
            return data.cannonType != null ? data.cannonType.prefab : null;

        if (data.unitType == BattleUnitType.Horse)
            return data.horseType != null ? data.horseType.prefab : null;

        return data.unitType switch
        {
            BattleUnitType.Soldier => unitPrefabs.soldierPrefab,
            BattleUnitType.Archer => unitPrefabs.archerPrefab,
            BattleUnitType.Dragon => unitPrefabs.dragonPrefab,
            _ => unitPrefabs.soldierPrefab,
        };
    }

    // ── Enemy Targeting ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by each BattleUnit every frame to find its nearest living enemy.
    /// </summary>
    public BattleUnit FindNearestEnemy(BattleUnit asker)
    {
        List<BattleUnit> enemies = asker.isPlayerUnit ? _botUnits : _playerUnits;

        BattleUnit closest = null;
        float closestDist = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            float d = Mathf.Abs(e.WorldX - asker.WorldX);
            if (d < closestDist)
            {
                closestDist = d;
                closest = e;
            }
        }

        return closest;
    }

    // ── Death Tracking ────────────────────────────────────────────────────────

    public void OnUnitDied(BattleUnit unit)
    {
        if (_battleOver) return;

        // Check if an entire side has been wiped.
        bool playerAlive = HasAliveUnit(_playerUnits);
        bool botAlive = HasAliveUnit(_botUnits);

        if (!playerAlive || !botAlive)
            StartCoroutine(EndBattle(!playerAlive ? false : true));
    }

    private bool HasAliveUnit(List<BattleUnit> list)
    {
        foreach (var u in list)
            if (u != null && !u.IsDead)
                return true;
        return false;
    }

    // ── Win / Lose ────────────────────────────────────────────────────────────

    private IEnumerator EndBattle(bool playerWon)
    {
        _battleOver = true;

        // Brief pause so the final blow lands visually.
        yield return new WaitForSeconds(1f);

        if (resultPanel != null) resultPanel.SetActive(true);
        if (winText != null) winText.SetActive(playerWon);
        if (loseText != null) loseText.SetActive(!playerWon);

        Debug.Log($"[BattleManager] Battle over — player {(playerWon ? "WON" : "LOST")}.");
    }

    // ── Result Panel Button ───────────────────────────────────────────────────

    /// <summary>Wire the Return button's OnClick → BattleManager.OnReturnClicked.</summary>
    public void OnReturnClicked()
    {
        SceneManager.LoadScene(villageSceneName);
    }
}