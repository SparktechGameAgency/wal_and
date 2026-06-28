using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HORSE DETACH BUTTON
///
/// Place this anywhere inside the HorseArea GameObject (or assign horseArea
/// in the Inspector). The button:
///
///   • Is VISIBLE  when at least one equipped HorseSlot has a soldier mounted.
///   • Is HIDDEN   when no equipped horse has a rider.
///   • On click    → detaches ONE soldier (the first occupied slot found).
///                   If two horses have soldiers, click once per horse.
///
/// Wiring:
///   A) Assign 'horseArea' in the Inspector  — works anywhere in the scene.
///   B) Leave it empty and place this button as a child of the HorseArea
///      GameObject — it finds the area automatically via GetComponentInParent.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class HorseDetachButton : MonoBehaviour
{
    [Tooltip("The HorseArea that owns the slots to check. " +
             "Leave empty to find automatically via GetComponentInParent.")]
    [SerializeField] private HorseArea horseArea;

    private Button _button;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (horseArea == null)
            horseArea = GetComponentInParent<HorseArea>();

        if (horseArea == null)
            Debug.LogError("[HorseDetachButton] No HorseArea found. " +
                           "Assign it in the Inspector or place this button inside the HorseArea GameObject.", this);

        _button.onClick.AddListener(OnDetachClicked);
        SetVisible(false);
    }

    private void Update()
    {
        SetVisible(FindFirstMountedSlotHorse() != null);
    }

    private void OnDetachClicked()
    {
        HorseController horse = FindFirstMountedSlotHorse();

        if (horse == null)
        {
            Debug.LogWarning("[HorseDetachButton] No horse with a mounted soldier found in slots.", this);
            return;
        }

        horse.PerformDismount();
        Debug.Log($"[HorseDetachButton] Soldier detached from '{horse.name}'.");
    }

    private HorseController FindFirstMountedSlotHorse()
    {
        if (horseArea == null) return null;

        foreach (HorseSlot slot in horseArea.Slots)
        {
            if (slot == null) continue;
            HorseController horse = slot.Horse;
            if (horse != null && horse.IsOccupied)
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