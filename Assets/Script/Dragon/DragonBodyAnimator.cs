using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — DragonBodyAnimator
///
/// Attach to the DragonBody child GameObject (the one that has the body Image).
/// DragonController finds this automatically via GetComponentInChildren and calls
/// SetState() whenever the dragon transitions between Idle and Flying.
///
/// ════════════════════════════════════════════════════════════════════
///  HOW IT WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  Two sprite arrays live on this component — one for each state:
///    idleSprites[]  — body at rest (dragon sitting in DragonArea)
///    flySprites[]   — body in flight (dragon patrolling a FlyZone)
///
///  The animator runs a frame loop in Update(). When DragonController
///  calls SetState(Idle) or SetState(Fly), the animator resets to
///  frame 0 and begins cycling the matching array.
///
///  Because the body is a CHILD of the dragon root, it:
///    • Moves with the dragon automatically (no extra code)
///    • Flips with the dragon automatically (localScale.x negation propagates)
///    • Renders behind the soldier because of the sibling order enforced
///      by DragonLayeredVisual (DragonBody[0] → RiderSeat[1] → DragonWing[2])
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP (Inspector)
/// ════════════════════════════════════════════════════════════════════
///
///  bodyImage    Drag the Image component from THIS GameObject here.
///               (Or leave null — it will be found via GetComponent.)
///
///  idleSprites  Body frames for the idle/rest state.
///               Tip: 3-4 frames of a gentle breathing cycle.
///
///  flySprites   Body frames for the fly/patrol state.
///               Tip: 6-8 frames synced to the wing flap cycle.
///
///  idleFps      Playback speed while idle (try 4–6 fps).
///  flyFps       Playback speed while flying (try 8–12 fps).
///
/// ════════════════════════════════════════════════════════════════════
///  PREFAB HIERARCHY
/// ════════════════════════════════════════════════════════════════════
///
///   Dragon (root)              DragonController + CanvasGroup + DragonLayeredVisual
///   ├── DragonBody [0]         Image + DragonBodyAnimator  ◄ THIS SCRIPT
///   ├── RiderSeat  [1]         DragonRiderSeat
///   │   └── DragonRiderVisual  DragonRiderVisual
///   └── DragonWing [2]         Image + DragonWingAnimator
///
/// ════════════════════════════════════════════════════════════════════
///  DRAGONCONTROLLER INTEGRATION
/// ════════════════════════════════════════════════════════════════════
///
///  DragonController calls:
///    EnterIdle()   → _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Idle)
///    EnterFlying() → _bodyAnimator?.SetState(DragonBodyAnimator.BodyState.Fly)
/// </summary>
[RequireComponent(typeof(Image))]
public class DragonBodyAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Body Image (auto-found if blank)")]
    [SerializeField] private Image bodyImage;

    [Header("Idle Animation")]
    [Tooltip("Sprite frames for the body at rest (dragon sitting in DragonArea).")]
    public Sprite[] idleSprites;

    [Tooltip("Frames per second while idle. Try 4–6 for a gentle breathing look.")]
    [Min(1f)]
    [SerializeField] private float idleFps = 5f;

    [Header("Fly Animation")]
    [Tooltip("Sprite frames for the body in flight (dragon patrolling a FlyZone).")]
    public Sprite[] flySprites;

    [Tooltip("Frames per second while flying. Try 8–12 to match the wing flap.")]
    [Min(1f)]
    [SerializeField] private float flyFps = 10f;

    // ── Body state ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The two states the body can be in.
    /// Matches DragonController.DragonState (Idle → BodyState.Idle,
    /// Flying → BodyState.Fly). Dragging keeps whatever state the dragon
    /// was in before, so the body animation doesn't glitch mid-drag.
    /// </summary>
    public enum BodyState { Idle, Fly }

    // ── Runtime ───────────────────────────────────────────────────────────────

    private BodyState _state = BodyState.Idle;
    private float _timer;
    private int _frame;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (bodyImage == null)
            bodyImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Show frame 0 of idle immediately on spawn.
        ShowFrame();
    }

    private void Update()
    {
        float fps = _state == BodyState.Fly ? flyFps : idleFps;
        _timer += Time.deltaTime;

        if (_timer < 1f / fps) return;

        _timer = 0f;
        _frame++;
        ShowFrame();
    }

    // ── Public API — called by DragonController ───────────────────────────────

    /// <summary>
    /// Switch the body to a new animation state.
    /// Call this from DragonController.EnterIdle() and DragonController.EnterFlying().
    ///
    ///   DragonController.EnterIdle()   → SetState(BodyState.Idle)
    ///   DragonController.EnterFlying() → SetState(BodyState.Fly)
    ///
    /// Resets to frame 0 so the cycle always starts cleanly on a state change.
    /// </summary>
    public void SetState(BodyState newState)
    {
        if (_state == newState) return;
        _state = newState;
        _frame = 0;
        _timer = 0f;
        ShowFrame();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ShowFrame()
    {
        if (bodyImage == null) return;

        Sprite[] sprites = _state == BodyState.Fly ? flySprites : idleSprites;

        if (sprites == null || sprites.Length == 0)
        {
            // No sprites assigned for this state — leave the current sprite as-is.
            return;
        }

        bodyImage.sprite = sprites[_frame % sprites.Length];
        bodyImage.enabled = true;
    }
}