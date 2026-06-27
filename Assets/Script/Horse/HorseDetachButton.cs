using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DETACH BUTTON — can live anywhere in the scene.
///
/// Wiring (pick ONE):
///   A) Assign walkZone in the Inspector — most reliable, works regardless of hierarchy.
///   B) Leave walkZone empty and place this button as a child of the HorseWalkZone
///      GameObject — it will find the zone automatically via GetComponentInParent.
///
/// Behaviour:
///   • Visible when ANY HorseController under the zone has a soldier mounted.
///   • Hidden when no soldier is mounted.
///   • Click → dismounts the first occupied horse → soldier returns to drag origin.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class HorseDetachButton : MonoBehaviour
{
    [Tooltip("Assign the HorseWalkZone that owns the horse(s) this button controls. " +
             "If left empty, the button will search its own parent chain at runtime.")]
    [SerializeField] private HorseWalkZone walkZone;

    private Button _button;
    private CanvasGroup _canvasGroup;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();

        // Auto-find if not wired in Inspector
        if (walkZone == null)
            walkZone = GetComponentInParent<HorseWalkZone>();

        if (walkZone == null)
            Debug.LogError("[HorseDetachButton] No HorseWalkZone found. " +
                           "Assign it in the Inspector or place this button inside the HorseWalkZone GameObject.", this);

        _button.onClick.AddListener(OnDetachClicked);
        SetVisible(false);
    }

    private void Update()
    {
        SetVisible(FindMountedHorse() != null);
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    private void OnDetachClicked()
    {
        HorseController horse = FindMountedHorse();

        if (horse == null)
        {
            Debug.LogWarning("[HorseDetachButton] No mounted soldier found.", this);
            return;
        }

        horse.PerformDismount();
        Debug.Log("[HorseDetachButton] Soldier detached — returned to drag origin.");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private HorseController FindMountedHorse()
    {
        if (walkZone == null) return null;

        HorseController[] horses =
            walkZone.GetComponentsInChildren<HorseController>(includeInactive: true);

        foreach (HorseController horse in horses)
        {
            if (horse.IsOccupied)
                return horse;
        }

        return null;
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}