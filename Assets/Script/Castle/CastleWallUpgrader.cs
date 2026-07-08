using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CastleWallUpgrader
///
/// Drives a single CastleBlock's wall through an ordered list of
/// CastleWallData tiers. Add one CastleWallUpgrader per CastleBlock
/// (same GameObject), wire up an "Update" Button in the Inspector, and drop
/// as many CastleWallData assets into wallTiers as you want — add a new
/// wall type any time by creating another asset (Create → AreaForge →
/// Castle Wall Data) and dragging it into the list. No code changes needed
/// to add more tiers.
///
/// ════════════════════════════════════════════════════════════════════
///  FLOW
/// ════════════════════════════════════════════════════════════════════
///  1. The Update button, the countdown label, and the progress slider are
///     only ever shown while the Castle panel's "Expand" tab is active
///     (CastleTabController.OnTabChanged). In every other tab they're
///     fully hidden.
///  2. Within the Expand tab: player taps Update (OnUpdateButtonClicked).
///     If there's a next tier and no upgrade is already running,
///     StartUpgrade() begins a REAL-TIME countdown of
///     wallTiers[nextIndex].updateDuration seconds that keeps running even
///     while the player is looking at a different tab/section — only the
///     visuals hide, the timer itself doesn't pause. The button hides and
///     the label/slider show and fill over that time.
///  3. If the player leaves the Expand tab mid-upgrade and comes back, the
///     label/slider reappear showing the upgrade's current real-time
///     progress (not reset).
///  4. When the timer completes, CastleBlock.ApplyWallUpgrade() swaps in
///     the new sprite/stats. The button reappears — but ONLY if the Expand
///     tab is still active AND there's another tier left to build.
///
///  Nothing here assumes a fixed number of tiers or hardcodes wall types —
///  everything specific to a wall (name, sprite, stats, how long it takes,
///  what it costs) lives on the CastleWallData asset itself.
/// </summary>
[RequireComponent(typeof(CastleBlock))]
public class CastleWallUpgrader : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Wall Tiers (in progression order)")]
    [Tooltip("Index 0 is the wall this block starts as. Add as many " +
             "CastleWallData assets as you like — drag them in, in order.")]
    [SerializeField] private List<CastleWallData> wallTiers = new List<CastleWallData>();

    [Tooltip("Which tier index this block starts at (usually 0).")]
    [SerializeField] private int startingTierIndex = 0;

    [Header("UI — Update Button (auto-found on this GameObject if left blank)")]
    [SerializeField] private Button updateButton;

    [Header("UI — Optional Progress Feedback")]
    [Tooltip("Fills 0→1 over updateDuration while upgrading. Leave blank to skip.")]
    [SerializeField] private Slider progressBar;

    [Tooltip("Shows a countdown / status message while upgrading. Leave blank to skip.")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    // ── State ────────────────────────────────────────────────────────────────

    private CastleBlock _block;
    private int _currentTierIndex;
    private Coroutine _upgradeRoutine;

    // Tracked separately from _upgradeRoutine on purpose: StartCoroutine()
    // runs a coroutine synchronously up to its first "yield" BEFORE it
    // returns, so "_upgradeRoutine = StartCoroutine(...)" isn't actually
    // assigned yet during that first synchronous chunk. RefreshVisuals()
    // gets called from inside UpgradeRoutine() before that assignment
    // happens, so IsUpgrading must not depend on _upgradeRoutine's value —
    // otherwise the very first RefreshVisuals() call of an upgrade sees
    // IsUpgrading == false and shows the button / hides the progress UI,
    // which then never gets corrected until the upgrade finishes.
    private bool _isUpgrading;

    // Whether the Castle panel is currently on the "Expand" tab. All three UI
    // pieces (button, label, slider) are gated behind this — they belong to
    // the expansion section only.
    private bool _isExpandTabActive;

    public bool IsUpgrading => _isUpgrading;
    public CastleWallData CurrentWallData =>
        (_currentTierIndex >= 0 && _currentTierIndex < wallTiers.Count) ? wallTiers[_currentTierIndex] : null;
    public bool HasNextTier => _currentTierIndex + 1 < wallTiers.Count;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _block = GetComponent<CastleBlock>();

        if (updateButton == null)
            updateButton = GetComponentInChildren<Button>(true);
    }

    private void Start()
    {
        _currentTierIndex = Mathf.Clamp(startingTierIndex, 0, Mathf.Max(0, wallTiers.Count - 1));

        // Apply the starting tier immediately so the block's stats/sprite
        // match wallTiers[startingTierIndex] from the very first frame,
        // instead of whatever was left on the prefab by hand.
        if (CurrentWallData != null)
            _block.ApplyWallUpgrade(CurrentWallData);

        if (updateButton != null)
            updateButton.onClick.AddListener(OnUpdateButtonClicked);

        // Pick up whatever tab is already active (e.g. panel opened directly
        // into Expand) instead of assuming hidden until the first tab event.
        // If there's no CastleTabController in the scene at all (e.g. a
        // standalone test scene with just the CastleBlock prefab), don't
        // gate on tabs — default to visible instead of permanently hidden.
        _isExpandTabActive = CastleTabController.Instance == null ||
                              CastleTabController.Instance.ActiveTab == CastleTabController.CastleTab.Expand;

        RefreshVisuals();
    }

    private void OnEnable()
    {
        CastleTabController.OnTabChanged += HandleTabChanged;
    }

    private void OnDisable()
    {
        CastleTabController.OnTabChanged -= HandleTabChanged;
    }

    private void OnDestroy()
    {
        if (updateButton != null)
            updateButton.onClick.RemoveListener(OnUpdateButtonClicked);
    }

    // ── Tab handling ─────────────────────────────────────────────────────────

    private void HandleTabChanged(CastleTabController.CastleTab tab)
    {
        _isExpandTabActive = tab == CastleTabController.CastleTab.Expand;

        // Just toggles what's shown — never touches the running timer, so
        // real-time progress keeps counting in the background and picks up
        // right where it left off when the player returns to Expand.
        RefreshVisuals();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Wire this to the Update Button's OnClick in the Inspector, or let Awake auto-wire it.</summary>
    public void OnUpdateButtonClicked()
    {
        if (IsUpgrading) return;
        if (!HasNextTier)
        {
            if (statusLabel != null && _isExpandTabActive)
            {
                statusLabel.gameObject.SetActive(true);
                statusLabel.text = "Max Level";
            }
            return;
        }

        _isUpgrading = true;
        _upgradeRoutine = StartCoroutine(UpgradeRoutine(wallTiers[_currentTierIndex + 1]));
    }

    // ── Upgrade Timer ────────────────────────────────────────────────────────

    private IEnumerator UpgradeRoutine(CastleWallData nextData)
    {
        if (progressBar != null) progressBar.value = 0f;
        RefreshVisuals();

        float duration = Mathf.Max(0f, nextData.updateDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            // Keep updating values every frame regardless of section — this
            // is what lets the slider/label show correct progress the moment
            // the player switches back to the Expand tab.
            if (progressBar != null) progressBar.value = t;
            if (statusLabel != null)
            {
                float remaining = Mathf.Max(0f, duration - elapsed);
                statusLabel.text = $"Building {nextData.wallName}… {remaining:F1}s";
            }

            yield return null;
        }

        _block.ApplyWallUpgrade(nextData);
        _currentTierIndex++;

        if (statusLabel != null) statusLabel.text = string.Empty;

        _isUpgrading = false;
        _upgradeRoutine = null;
        RefreshVisuals();
    }

    // ── UI Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Single source of truth for what's visible right now: button, label,
    /// and slider are all gated on being in the Expand tab, then split
    /// between "idle" (button) and "upgrading" (label + slider) states.
    /// </summary>
    private void RefreshVisuals()
    {
        bool showButton = _isExpandTabActive && !IsUpgrading && HasNextTier;
        bool showProgress = _isExpandTabActive && IsUpgrading;

        if (updateButton != null)
        {
            updateButton.gameObject.SetActive(showButton);
            updateButton.interactable = showButton;
        }

        if (progressBar != null)
            progressBar.gameObject.SetActive(showProgress);

        if (statusLabel != null)
            statusLabel.gameObject.SetActive(showProgress);
    }
}