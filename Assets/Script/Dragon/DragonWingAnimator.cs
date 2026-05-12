using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE — DragonWingAnimator
///
/// Attach to the DragonWing child GameObject (the one that has the wing Image).
/// DragonController finds this automatically via GetComponentInChildren and calls
/// SetState() whenever the dragon transitions between Idle and Flying.
///
/// ════════════════════════════════════════════════════════════════════
///  HOW IT WORKS
/// ════════════════════════════════════════════════════════════════════
///
///  Two sprite arrays live on this component — one for each state:
///    idleSprites[]  — wing at rest, slight gentle bob (dragon is sitting)
///    flySprites[]   — wing flapping, driven by patrol speed
///
///  The animator runs a frame loop in Update() identical to SpriteLayerAnimator.
///  When DragonController calls SetState(Idle) or SetState(Fly), the animator
///  resets to frame 0 and begins cycling the matching array.
///
///  Because the wing is a CHILD of the dragon root, it:
///    • Moves with the dragon automatically (no extra code)
///    • Flips with the dragon automatically (localScale.x negation propagates)
///    • Renders on top of the soldier because of the sibling order enforced
///      by DragonLayeredVisual (RiderSeat[1] → DragonWing[2])
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP (Inspector)
/// ════════════════════════════════════════════════════════════════════
///
///  wingImage    Drag the Image component from THIS GameObject here.
///               (Or leave null — it will be found via GetComponent.)
///
///  idleSprites  Wing frames for the idle/rest state.
///               Tip: 3-4 frames of a gentle droop or slow fold looks natural.
///
///  flySprites   Wing frames for the fly/patrol state.
///               Tip: 6-8 frames of a full flap cycle.
///
///  idleFps      Playback speed while idle (try 4–6 fps).
///  flyFps       Playback speed while flying (try 8–12 fps).
///               Keeping the two FPS values separate lets idle feel slow
///               and lazy while the flying flap feels energetic.
/// </summary>
[RequireComponent(typeof(Image))]
public class DragonWingAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Wing Image (auto-found if blank)")]
    [SerializeField] private Image wingImage;

    [Header("Idle Animation")]
    [Tooltip("Sprite frames for the wing at rest (dragon sitting in DragonArea).")]
    public Sprite[] idleSprites;

    [Tooltip("Frames per second while idle. Try 4–6 for a gentle bob.")]
    [Min(1f)]
    [SerializeField] private float idleFps = 5f;

    [Header("Fly Animation")]
    [Tooltip("Sprite frames for the wing flapping (dragon patrolling a FlyZone).")]
    public Sprite[] flySprites;

    [Tooltip("Frames per second while flying. Try 8–12 for a snappy flap.")]
    [Min(1f)]
    [SerializeField] private float flyFps = 10f;

    // ── Wing state ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The two states the wing can be in.
    /// Matches DragonController.DragonState (Idle → WingState.Idle,
    /// Flying → WingState.Fly). Dragging keeps whatever state the dragon
    /// was in before, so the wing animation doesn't glitch mid-drag.
    /// </summary>
    public enum WingState { Idle, Fly }

    // ── Runtime ───────────────────────────────────────────────────────────────

    private WingState _state = WingState.Idle;
    private float _timer;
    private int _frame;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (wingImage == null)
            wingImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Show frame 0 of idle immediately on spawn
        ShowFrame();
    }

    private void Update()
    {
        float fps = _state == WingState.Fly ? flyFps : idleFps;
        _timer += Time.deltaTime;

        if (_timer < 1f / fps) return;

        _timer = 0f;
        _frame++;
        ShowFrame();
    }

    // ── Public API — called by DragonController ───────────────────────────────

    /// <summary>
    /// Switch the wing to a new animation state.
    /// Call this from DragonController.EnterIdle() and DragonController.EnterFlying().
    ///
    ///   DragonController.EnterIdle()   → SetState(WingState.Idle)
    ///   DragonController.EnterFlying() → SetState(WingState.Fly)
    ///
    /// Resets to frame 0 so the cycle always starts cleanly on a state change.
    /// </summary>
    public void SetState(WingState newState)
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
        if (wingImage == null) return;

        Sprite[] sprites = _state == WingState.Fly ? flySprites : idleSprites;

        if (sprites == null || sprites.Length == 0)
        {
            // No sprites assigned for this state — leave the current sprite as-is.
            return;
        }

        wingImage.sprite = sprites[_frame % sprites.Length];
        wingImage.enabled = true;
    }
}