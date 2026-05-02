using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AREA FORGE - WizardBox
/// A UI panel (the "wizard box") that accepts a dragged soldier.
///
/// When a soldier is dropped here:
///   1. The soldier is hidden and a wizard animation plays in a loop.
///   2. Every 5 seconds, Alixar Coins increase by 10.
///   3. A "Retrieve" button lets the player take the soldier back.
///
/// Setup:
///   1. Create a Panel in your Canvas — name it "WizardBox".
///   2. Add this script to it.
///   3. Inside WizardBox create:
///        • An Image child named "WizardImage"
///            – Assign your wizard sprite sheet and an Animator Controller
///              that loops a "WizardIdle" or "Brewing" animation.
///        • A TextMeshProUGUI child named "CoinText"  (shows coin count)
///        • A Button child named "RetrieveButton"     (gives the soldier back)
///        • An Image child named "DropHighlight"      (optional glow on hover)
///   4. Wire all those into the Inspector fields below.
///   5. Make sure the WizardBox panel has an Image component (needed for raycasts)
///      with Raycast Target = ON.
/// </summary>
public class WizardBox : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Wizard Animation")]
    [Tooltip("The Image + Animator inside WizardBox that plays the wizard animation")]
    [SerializeField] private Animator wizardAnimator;

    [Tooltip("Animator bool parameter name for the brewing/idle loop")]
    [SerializeField] private string wizardAnimParam = "IsBrewing";

    [Header("Alixar Coins")]
    [Tooltip("Coins awarded every tick")]
    [SerializeField] private int coinsPerTick = 10;
    [Tooltip("Seconds between each coin award")]
    [SerializeField] private float tickInterval = 5f;
    [Tooltip("TextMeshPro label that shows current coin count")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("UI")]
    [Tooltip("Button that returns the soldier to the spawn area")]
    [SerializeField] private Button retrieveButton;
    [Tooltip("Optional highlight image shown when hovering with a dragged soldier")]
    [SerializeField] private Image dropHighlight;

    // ─── Private State ────────────────────────────────────────────────────────

    private int _alixarCoins = 0;
    private GameObject _soldierInBox = null;
    private SoldierDragDrop _soldierDragDrop = null;
    private Coroutine _coinCoroutine = null;
    private bool _occupied = false;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Wizard animation is hidden until a soldier is dropped in
        if (wizardAnimator != null)
            wizardAnimator.gameObject.SetActive(false);

        // Highlight off by default
        if (dropHighlight != null)
            dropHighlight.enabled = false;

        // Wire the retrieve button
        if (retrieveButton != null)
        {
            retrieveButton.gameObject.SetActive(false);
            retrieveButton.onClick.AddListener(RetrieveSoldier);
        }

        RefreshCoinText();
    }

    // ─── IDropHandler ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by Unity's EventSystem when the player releases a dragged UI
    /// object over this panel.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (_occupied)
        {
            Debug.Log("[WizardBox] Already occupied — rejecting drop.");
            return;
        }

        // Check the dragged object is actually a soldier
        SoldierDragDrop soldier = eventData.pointerDrag?.GetComponent<SoldierDragDrop>();
        if (soldier == null) return;

        AcceptSoldier(soldier);
    }

    // ─── Hover Highlight ──────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_occupied && dropHighlight != null)
            dropHighlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropHighlight != null)
            dropHighlight.enabled = false;
    }

    // ─── Accept / Retrieve ────────────────────────────────────────────────────

    private void AcceptSoldier(SoldierDragDrop soldier)
    {
        _soldierDragDrop = soldier;
        _soldierInBox = soldier.gameObject;
        _occupied = true;

        // Notify the drag script that the drop succeeded (stops snap-back)
        soldier.OnSuccessfulDrop();

        // Hide the soldier inside the box — the wizard does the visual work
        _soldierInBox.SetActive(false);

        // Reparent the soldier under WizardBox so it stays organised
        _soldierInBox.transform.SetParent(transform, false);

        // Show wizard animation
        if (wizardAnimator != null)
        {
            wizardAnimator.gameObject.SetActive(true);
            wizardAnimator.SetBool(wizardAnimParam, true);
        }

        // Show retrieve button
        if (retrieveButton != null)
            retrieveButton.gameObject.SetActive(true);

        // Hide hover highlight
        if (dropHighlight != null)
            dropHighlight.enabled = false;

        // Start earning coins
        _coinCoroutine = StartCoroutine(CoinTick());

        Debug.Log("[WizardBox] Soldier entered the wizard box. Brewing started!");
    }

    /// <summary>
    /// Returns the soldier to the spawn area and stops the wizard animation.
    /// Wired to the Retrieve button.
    /// </summary>
    private void RetrieveSoldier()
    {
        if (_soldierInBox == null) return;

        // Stop coin generation
        if (_coinCoroutine != null)
        {
            StopCoroutine(_coinCoroutine);
            _coinCoroutine = null;
        }

        // Stop wizard animation
        if (wizardAnimator != null)
        {
            wizardAnimator.SetBool(wizardAnimParam, false);
            wizardAnimator.gameObject.SetActive(false);
        }

        // Return soldier to its original spawn-area parent and re-enable it
        _soldierInBox.SetActive(true);
        _soldierDragDrop?.SnapBack();    // restores original parent + resumes patrol

        // Hide retrieve button
        if (retrieveButton != null)
            retrieveButton.gameObject.SetActive(false);

        _soldierInBox = null;
        _soldierDragDrop = null;
        _occupied = false;

        Debug.Log("[WizardBox] Soldier retrieved. Brewing stopped.");
    }

    // ─── Coin Tick ────────────────────────────────────────────────────────────

    /// <summary>
    /// Awards coinsPerTick Alixar Coins every tickInterval seconds
    /// while a soldier is inside the wizard box.
    /// </summary>
    private IEnumerator CoinTick()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);
            _alixarCoins += coinsPerTick;
            RefreshCoinText();
            Debug.Log($"[WizardBox] +{coinsPerTick} Alixar Coins → Total: {_alixarCoins}");
        }
    }

    private void RefreshCoinText()
    {
        if (coinText != null)
            coinText.text = $"{_alixarCoins}";
    }

    // ─── Public Coin API ─────────────────────────────────────────────────────

    /// <summary>Total coins earned so far.</summary>
    public int AlixarCoins => _alixarCoins;
}