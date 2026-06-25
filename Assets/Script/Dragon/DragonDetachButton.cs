using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DETACH BUTTON — place this on a Button inside the DragonArea (DragonEggSlot).
/// No Inspector wiring needed — finds the dragon and rider automatically at runtime.
///
/// Visible only when the dragon is Idle AND a soldier is mounted.
/// Click → DismountFromDragon() → soldier returns to VillageSoldierSlot + patrol.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class DragonDetachButton : MonoBehaviour
{
    private Button _button;
    private CanvasGroup _canvasGroup;
    private DragonEggSlot _eggSlot;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _eggSlot = GetComponentInParent<DragonEggSlot>();

        if (_eggSlot == null)
            Debug.LogWarning("[DragonDetachButton] No DragonEggSlot found in parents. " +
                             "Place this button inside the DragonArea GameObject.", this);

        _button.onClick.AddListener(OnDetachClicked);
        SetVisible(false);
    }

    private void Update()
    {
        DragonController dragon = GetDragon();
        DragonRiderSeat seat = GetSeat(dragon);

        // Dragon must be idle in the area AND the seat must be occupied.
        bool show = dragon != null
                 && dragon.State == DragonController.DragonState.Idle
                 && seat != null
                 && seat.IsOccupied;

        SetVisible(show);
    }

    private void OnDetachClicked()
    {
        DragonController dragon = GetDragon();
        DragonRiderSeat seat = GetSeat(dragon);

        if (seat == null || !seat.IsOccupied)
        {
            Debug.LogWarning("[DragonDetachButton] No mounted soldier to detach.", this);
            return;
        }

        SoldierDragDrop soldier = seat.MountedSoldier;
        if (soldier == null)
        {
            Debug.LogWarning("[DragonDetachButton] Seat reports occupied but MountedSoldier is null.", this);
            return;
        }

        // Handles everything: releases seat, re-parents soldier to its
        // VillageSoldierSlot home, SetActive(true), ShowOwnVisuals(),
        // ExitRidingState(), SetPatrolling(true), PerformDismount() on dragon.
        soldier.DismountFromDragon();

        Debug.Log("[DragonDetachButton] Soldier detached — returning to village patrol.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Finds the DragonController living inside the DragonEggSlot at runtime.
    private DragonController GetDragon()
    {
        if (_eggSlot == null) return null;
        return _eggSlot.GetComponentInChildren<DragonController>(includeInactive: true);
    }

    /// Finds the DragonRiderSeat inside the given dragon.
    private DragonRiderSeat GetSeat(DragonController dragon)
    {
        if (dragon == null) return null;
        return dragon.GetComponentInChildren<DragonRiderSeat>(includeInactive: true);
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}