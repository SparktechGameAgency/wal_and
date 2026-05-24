using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class DragonAttachButton : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dragon Seat")]
    [Tooltip("The DragonRiderSeat this button controls.\n" +
             "Drag the DragonRiderSeat component (child of the dragon) here.")]
    [SerializeField] private DragonRiderSeat seat;

    [Header("Label Text")]
    [Tooltip("Button label when no soldier is attached yet.")]
    [SerializeField] private string labelAttach = "Attach";

    [Tooltip("Button label after the soldier has been locked to the seat.")]
    [SerializeField] private string labelAttached = "Attached";

    // ── Private ───────────────────────────────────────────────────────────────

    private Button _button;
    private CanvasGroup _canvasGroup;
    private TMP_Text _label;
    private DragonController _dragonController;

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _button = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _label = GetComponentInChildren<TMP_Text>();

        // DragonController sits on the root dragon GameObject — this button
        // is a child of it, so GetComponentInParent always finds it without
        // any extra Inspector wiring.
        _dragonController = GetComponentInParent<DragonController>();

        if (_label == null)
            Debug.LogWarning("[DragonAttachButton] No TMP_Text found in children. " +
                             "Add a TextMeshPro child to the Button.", this);

        if (seat == null)
            Debug.LogWarning("[DragonAttachButton] 'seat' is not assigned. " +
                             "Drag a DragonRiderSeat into the Inspector field.", this);

        if (_dragonController == null)
            Debug.LogWarning("[DragonAttachButton] No DragonController found in parents. " +
                             "Make sure this Button is a child of the dragon prefab.", this);

        _button.onClick.AddListener(OnClicked);

        // Start hidden — becomes visible once the dragon enters Idle.
        SetVisible(false);
    }

    private void Update()
    {
        // ── Visibility: show only while dragon is idle in the dragon area ─────
        bool dragonIsIdle = _dragonController != null &&
                            _dragonController.State == DragonController.DragonState.Idle;

        SetVisible(dragonIsIdle);

        if (!dragonIsIdle) return;   // skip label/interactable update while hidden

        // ── Interactability: only clickable when a soldier is seated ──────────
        SoldierDragDrop rider = GetCurrentRider();
        _button.interactable = rider != null;

        // ── Label: always reflects the real lock state ────────────────────────
        // Handles swap edge case: new rider starts unlocked so label resets
        // automatically without any extra code in the swap path.
        if (_label != null)
            _label.text = (rider != null && rider.IsLocked) ? labelAttached : labelAttach;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CLICK
    // ══════════════════════════════════════════════════════════════════════════

    private void OnClicked()
    {
        SoldierDragDrop rider = GetCurrentRider();
        if (rider == null) return;

        bool nowLocked = !rider.IsLocked;
        rider.SetLocked(nowLocked);

        // Update label immediately to avoid a one-frame flicker before Update runs.
        if (_label != null)
            _label.text = nowLocked ? labelAttached : labelAttach;

        Debug.Log($"[DragonAttachButton] '{rider.name}' → {(nowLocked ? labelAttached : labelAttach)}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows or hides the button via CanvasGroup — NOT SetActive — so that
    /// Update() keeps running and can restore visibility when the dragon
    /// returns to Idle after a flight.
    /// </summary>
    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// Returns the SoldierDragDrop currently sitting in the seat, or null.
    /// Works because MountOnDragon() reparents the soldier under seat.transform,
    /// so GetComponentInChildren finds them without extra bookkeeping.
    /// </summary>
    private SoldierDragDrop GetCurrentRider()
    {
        if (seat == null) return null;
        return seat.GetComponentInChildren<SoldierDragDrop>();
    }
}