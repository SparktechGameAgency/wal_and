using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DETACH BUTTON — sits inside HorseArea (parent of both horse slots).
///
/// No Inspector wiring needed. Finds HorseArea automatically via GetComponentInParent.
///
/// Behaviour:
///   • Visible when ANY of the two slots has a soldier mounted.
///   • Hidden when no soldier is mounted in either slot.
///   • Click → finds the slot with a mounted soldier → PerformDismount()
///             → soldier returns to its walk-zone position and resumes patrol.
///
/// If both slots have a soldier mounted, the first occupied one is dismounted.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class HorseDetachButton : MonoBehaviour
{
    private Button _button;
    private CanvasGroup _canvasGroup;
    private HorseArea _horseArea;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _horseArea = GetComponentInParent<HorseArea>();

        if (_horseArea == null)
            Debug.LogError("[HorseDetachButton] HorseArea not found in parents. " +
                           "Make sure this button is inside the HorseArea GameObject.", this);

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
        Debug.Log("[HorseDetachButton] Soldier detached — returned to walk-zone.");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// Searches all horse slots in HorseArea and returns the first HorseController
    /// that has a soldier currently mounted. Returns null if none.
    private HorseController FindMountedHorse()
    {
        if (_horseArea == null) return null;

        // HorseArea exposes its slots array via GetComponentsInChildren at runtime.
        // We search for every HorseController under HorseArea and check IsOccupied.
        HorseController[] horses =
            _horseArea.GetComponentsInChildren<HorseController>(includeInactive: true);

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