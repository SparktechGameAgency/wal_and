using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(CanvasGroup))]
public class HorseController : MonoBehaviour, IDropHandler
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Animation Data")]
    [Tooltip("ScriptableObject with horse animation clips (Idle, Run, Fight, Dead).\n" +
             "Leave NULL to drive animation from HorseData sprite arrays (backward-compatible).")]
    [SerializeField] private HorseAnimationSO horseAnimSO;

    [Tooltip("Optional second SO for the saddle/bridle Image layer.")]
    [SerializeField] private HorseAnimationSO saddleAnimSO;

    [Header("Image Layers")]
    [Tooltip("Main horse body Image. Auto-found via GetComponent if left empty.")]
    [SerializeField] private Image horseImage;

    [Tooltip("Optional saddle / bridle Image on a child object. Leave null if unused.")]
    [SerializeField] private Image saddleImage;

    [Header("Seat & Rider")]
    [Tooltip("HorseSeat child component. Auto-found in children if left empty.")]
    [SerializeField] private HorseSeat seat;

    [Tooltip("HorseRiderVisual on the SoldierSeat child.\n" +
             "Auto-found in children if left empty.\n" +
             "Drives the Face / Helmet / Weapon / Armor Images from the soldier's equipment.")]
    [SerializeField] private HorseRiderVisual riderVisual;

    // ── Patrol / External Control ────────────────────────────────────────────

    [Header("Patrol / External Control")]
    [Tooltip("Set to TRUE by HorseWalkZone (or any external patrol controller).\n" +
             "When true, the HorseData (Path B) 'cycles before idle' auto-revert is " +
             "skipped, so an external system's own Idle/Run timer is the only thing " +
             "that changes state. Prevents the horse flipping back to Idle mid-run.")]
    [SerializeField] private bool externallyControlled = false;

    /// <summary>True when an external controller (e.g. HorseWalkZone) owns the Idle/Run timing.</summary>
    public bool ExternallyControlled
    {
        get => externallyControlled;
        set => externallyControlled = value;
    }

    // ── Private state ─────────────────────────────────────────────────────────

    private HorseState _state = HorseState.Idle;

    private float _horseTimer;
    private float _saddleTimer;
    private int _horseFrame;
    private int _saddleFrame;
    private int _dataCyclesCompleted;

    private SoldierDragDrop _mountedSoldier;
    private SpriteLayerAnimator _riderAnimator;
    private CanvasGroup _soldierCanvasGroup;
    private HorseData _data;

    // ── Diagnostics ───────────────────────────────────────────────────────────
    // Set to true once to dump a one-time log of the animation path being used.
    private bool _animDiagDone = false;

    // ── Public queries ────────────────────────────────────────────────────────

    public HorseData Data => _data;
    public HorseState CurrentState => _state;
    public bool IsOccupied => seat != null && seat.IsOccupied;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (horseImage == null)
            horseImage = GetComponent<Image>();

        if (seat == null)
            seat = GetComponentInChildren<HorseSeat>();

        if (riderVisual == null)
            riderVisual = GetComponentInChildren<HorseRiderVisual>();

        if (horseImage == null)
            Debug.LogError($"[HorseController] '{name}': no horseImage found.", this);

        if (seat == null)
            Debug.LogError($"[HorseController] '{name}': no HorseSeat child found.", this);

        if (riderVisual == null)
            Debug.LogWarning($"[HorseController] '{name}': no HorseRiderVisual found. " +
                             "Face/Helmet/Weapon/Armor Images will not be shown.", this);
    }

    private void Start()
    {
        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO);

        if (seat == null || !seat.IsOccupied)
            riderVisual?.HideRider();
    }

    private void Update()
    {
        // ── ONE-TIME DIAGNOSTIC on first Run state ────────────────────────────
        if (!_animDiagDone && _state == HorseState.Run)
        {
            _animDiagDone = true;
            bool usingSOPath = horseAnimSO != null;
            bool usingDataPath = !usingSOPath && _data != null;

            if (usingSOPath)
            {
                HorseClip clip = horseAnimSO.GetClip(HorseState.Run);
                Debug.Log($"[HorseDiag] '{name}' PATH=SO  " +
                          $"clip={(clip == null ? "NULL" : "found")}  " +
                          $"frames={(clip?.frames?.Length ?? 0)}  " +
                          $"loop={clip?.loop}  fps={clip?.fps}  " +
                          $"horseImage={(horseImage == null ? "NULL" : horseImage.name)}  " +
                          $"imgEnabled={horseImage?.enabled}", this);
            }
            else if (usingDataPath)
            {
                Sprite[] sprites = _data.GetSprites(HorseState.Run);
                Debug.Log($"[HorseDiag] '{name}' PATH=Data  " +
                          $"runSprites={(sprites?.Length ?? 0)}  " +
                          $"horseImage={(horseImage == null ? "NULL" : horseImage.name)}  " +
                          $"imgEnabled={horseImage?.enabled}", this);
            }
            else
            {
                Debug.LogError($"[HorseDiag] '{name}' PATH=NONE — horseAnimSO is null AND _data is null! " +
                               "Animation cannot play. Call Setup(data) or assign horseAnimSO in Inspector.", this);
            }
        }
        // ── END DIAGNOSTIC ────────────────────────────────────────────────────

        TickLayer(horseAnimSO, horseImage, ref _horseFrame, ref _horseTimer, isMainLayer: true);

        if (saddleImage != null && saddleAnimSO != null)
            TickLayer(saddleAnimSO, saddleImage, ref _saddleFrame, ref _saddleTimer, isMainLayer: false);
    }

    // ── Animation Engine ──────────────────────────────────────────────────────

    private void TickLayer(HorseAnimationSO so, Image img,
                           ref int frame, ref float timer,
                           bool isMainLayer)
    {
        if (img == null) return;

        // Ensure image is visible (guard against it being disabled elsewhere)
        if (!img.enabled) img.enabled = true;

        // PATH A: SO-driven
        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            // Single-frame — just pin the sprite, no timer needed
            if (clip.frames.Length == 1)
            {
                img.sprite = clip.frames[0];
                return;
            }

            timer += Time.deltaTime;
            if (timer < 1f / clip.fps) return;
            timer -= 1f / clip.fps;

            if (clip.loop)
                frame = (frame + 1) % clip.frames.Length;
            else if (frame < clip.frames.Length - 1)
                frame++;

            img.sprite = clip.frames[frame];
            return;
        }

        // PATH B: HorseData fallback (main layer only)
        if (!isMainLayer || _data == null) return;

        Sprite[] sprites = _data.GetSprites(_state);
        if (sprites == null || sprites.Length == 0) return;

        // Single-frame
        if (sprites.Length == 1)
        {
            img.sprite = sprites[0];
            return;
        }

        float fps = _data.GetFPS(_state);
        timer += Time.deltaTime;
        if (timer < 1f / fps) return;
        timer -= 1f / fps;

        switch (_state)
        {
            case HorseState.Dead:
                if (frame < sprites.Length - 1) frame++;
                break;

            case HorseState.Run:
            case HorseState.Fight:
                frame++;
                if (frame >= sprites.Length)
                {
                    frame = 0;

                    // Skip the auto-revert-to-Idle entirely when an external
                    // controller (e.g. HorseWalkZone) owns the Idle/Run timing.
                    // Otherwise this would fight the zone's own timer and cause
                    // the horse to flip back to Idle mid-run.
                    if (!externallyControlled)
                    {
                        int maxCycles = _data.GetCyclesBeforeIdle(_state);
                        if (maxCycles > 0)
                        {
                            _dataCyclesCompleted++;
                            if (_dataCyclesCompleted >= maxCycles)
                                SetState(HorseState.Idle);
                        }
                    }
                }
                break;

            default:
                frame = (frame + 1) % sprites.Length;
                break;
        }

        img.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
    }

    private void ApplyFrame(ref int frame, Image img, HorseAnimationSO so,
                            bool isMainLayer = true)
    {
        if (img == null) return;
        frame = 0;

        if (so != null)
        {
            HorseClip clip = so.GetClip(_state);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            img.sprite = clip.frames[0];
            return;
        }

        if (!isMainLayer || _data == null) return;
        Sprite[] sprites = _data.GetSprites(_state);
        if (sprites != null && sprites.Length > 0)
            img.sprite = sprites[0];
    }

    // ── Public API — State ────────────────────────────────────────────────────

    public void SetState(HorseState newState)
    {
        _state = newState;

        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;

        // Reset diag flag so it fires again on the next Run entry
        if (newState == HorseState.Run) _animDiagDone = false;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        AnimationState riderState = MapToRiderState(newState);
        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] '{name}' → {newState}");
    }

    public void SetIdle() => SetState(HorseState.Idle);
    public void SetRun() => SetState(HorseState.Run);
    public void SetFight() => SetState(HorseState.Fight);
    public void SetDead() => SetState(HorseState.Dead);

    // ── Legacy API — HorseSlot / HorseWalkZone compatibility ─────────────────

    public void Setup(HorseData data)
    {
        _data = data;
        _state = HorseState.Idle;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;
        _animDiagDone = false;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        riderVisual?.SetRiderState(MapToRiderState(HorseState.Idle));

        Debug.Log($"[HorseController] Setup → '{data?.horseName}'");
    }

    public void SetupWalk(HorseData data)
    {
        _data = data;
        _state = HorseState.Run;
        _horseFrame = _saddleFrame = 0;
        _horseTimer = _saddleTimer = 0f;
        _dataCyclesCompleted = 0;
        _animDiagDone = false;

        ApplyFrame(ref _horseFrame, horseImage, horseAnimSO, isMainLayer: true);
        if (saddleImage != null && saddleAnimSO != null)
            ApplyFrame(ref _saddleFrame, saddleImage, saddleAnimSO, isMainLayer: false);

        AnimationState riderState = MapToRiderState(HorseState.Run);

        if (_mountedSoldier != null)
        {
            _mountedSoldier.gameObject.SetActive(false);
            _riderAnimator = null;
            var equipment = _mountedSoldier.GetComponent<CharacterEquipment>();
            riderVisual?.ShowRider(equipment);
        }

        riderVisual?.SetRiderState(riderState);
        NotifySoldierAnimator(riderState);

        Debug.Log($"[HorseController] SetupWalk → '{data?.horseName}'");
    }

    // ── Public API — Eject before destroy ────────────────────────────────────

    public void EjectRiderBeforeDestroy()
    {
        if (seat == null || !seat.IsOccupied) return;

        SoldierDragDrop soldier = seat.MountedSoldier;

        riderVisual?.HideRider();

        if (soldier != null)
            soldier.gameObject.SetActive(true);

        soldier?.ReturnHomeFromDestroyedHorse();

        seat.ReleaseSoldier();
        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider ejected before horse destroy.");
    }

    public SoldierDragDrop ExtractRiderForTransfer()
    {
        if (seat == null || !seat.IsOccupied) return null;

        SoldierDragDrop soldier = seat.MountedSoldier;

        riderVisual?.HideRider();

        seat.ReleaseSoldier();
        soldier?.ClearHorseSeatForTransfer();

        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        Debug.Log($"[HorseController] '{name}': rider '{soldier?.name}' extracted for transfer (stays inactive).");
        return soldier;
    }

    public void CounterFlipSeat()
    {
        if (seat == null) return;
        Vector3 s = seat.transform.localScale;
        s.x = -s.x;
        seat.transform.localScale = s;
        Debug.Log($"[HorseController] '{name}': SoldierSeat counter-flipped (x={s.x:F2}).");
    }

    // ── Public API — Mount / Dismount ─────────────────────────────────────────

    public void PerformMount(SoldierDragDrop soldier)
    {
        if (seat == null)
        {
            Debug.LogWarning($"[HorseController] '{name}': no seat — cannot mount.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log($"[HorseController] '{name}': seat already occupied.", this);
            return;
        }

        if (soldier == null) return;

        _mountedSoldier = soldier;
        var equipment = soldier.GetComponent<CharacterEquipment>();

        bool wasActive = soldier.gameObject.activeSelf;
        if (!wasActive) soldier.gameObject.SetActive(true);

        seat.MountSoldier(soldier);

        var soldierRT = soldier.GetComponent<RectTransform>();
        if (soldierRT != null)
        {
            soldierRT.SetParent(seat.transform, false);
            soldierRT.anchoredPosition = Vector2.zero;
            soldierRT.localScale = Vector3.one;
        }
        else
        {
            soldier.transform.SetParent(seat.transform, false);
            soldier.transform.localPosition = Vector3.zero;
            soldier.transform.localScale = Vector3.one;
        }

        _mountedSoldier.gameObject.SetActive(false);
        _riderAnimator = null;

        riderVisual?.ShowRider(equipment);
        riderVisual?.SetRiderState(MapToRiderState(_state));

        Debug.Log($"[HorseController] '{name}': '{soldier.name}' mounted. Rider state → {_state}.");
    }

    public void PerformDismount()
    {
        if (seat == null || !seat.IsOccupied) return;

        riderVisual?.HideRider();

        if (_mountedSoldier != null)
        {
            _riderAnimator = _mountedSoldier.GetComponent<SpriteLayerAnimator>();
            _mountedSoldier.gameObject.SetActive(true);
        }

        seat.MountedSoldier.DismountFromHorse();
        seat.ReleaseSoldier();

        _mountedSoldier = null;
        _riderAnimator = null;
        _soldierCanvasGroup = null;

        SetState(HorseState.Idle);

        Debug.Log($"[HorseController] '{name}': rider dismounted.");
    }

    // ── IDropHandler ──────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        var soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
        if (soldier == null) return;

        if (seat == null)
        {
            Debug.LogWarning($"[HorseController] '{name}': no seat configured.", this);
            return;
        }

        if (seat.IsOccupied)
        {
            Debug.Log($"[HorseController] '{name}': seat occupied — ignoring drop.");
            return;
        }

        PerformMount(soldier);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void NotifySoldierAnimator(AnimationState riderState)
    {
        _riderAnimator?.SetState(riderState);
    }

    private static AnimationState MapToRiderState(HorseState state) => state switch
    {
        HorseState.Idle => AnimationState.HorseIdle,
        HorseState.Run => AnimationState.HorseRun,
        HorseState.Fight => AnimationState.HorseFight,
        HorseState.Dead => AnimationState.HorseDead,
        _ => AnimationState.HorseIdle,
    };
}