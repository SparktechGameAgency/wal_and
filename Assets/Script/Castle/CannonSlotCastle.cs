////using UnityEngine;

////public class CannonSlotCastle : MonoBehaviour
////{
////    [Header("Slot State")]
////    public bool hasCannon = false;

////    [Header("Visuals")]
////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
////    public Transform slotTransform;         // Where to spawn the cannon

////    private GameObject _cannonInstance;

////    public void PlaceCannon(GameObject cannonPrefab = null)
////    {
////        if (hasCannon)
////        {
////            Debug.Log("[CannonSlot] Already has a cannon.");
////            return;
////        }

////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////        if (prefab != null)
////        {
////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
////        }

////        hasCannon = true;
////        Debug.Log("[CannonSlot] Cannon placed.");
////    }

////    public void RemoveCannon()
////    {
////        if (!hasCannon) return;
////        if (_cannonInstance != null) Destroy(_cannonInstance);
////        hasCannon = false;
////    }

////    private void OnMouseDown()
////    {
////        // Click to toggle cannon (for testing)
////        if (!hasCannon) PlaceCannon();
////        else RemoveCannon();
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
///// It is a prefab that lives on every castle block's exposed unit slot.
/////
///// The CannonZone has a Button component (added manually in the Inspector).
///// Clicking it opens the cannon assignment panel.
/////
///// IMPORTANT — click-blocking fix:
/////   Invisible castle block Images below this zone must NOT be raycast targets.
/////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
/////   but the real fix is also applied in GridCell and CastleGrid:
/////   the GridCell background Image has raycastTarget = false (it is just layout),
/////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
/////   so Button clicks always reach this zone.
///// </summary>
//[RequireComponent(typeof(Button))]
//public class CannonSlotCastle : MonoBehaviour
//{
//    [Header("Slot State")]
//    public bool hasCannon = false;

//    [Header("Visuals")]
//    public GameObject cannonVisualPrefab;
//    public Transform slotTransform;

//    private GameObject _cannonInstance;
//    private Button _button;

//    private void Awake()
//    {
//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnCannonZoneClicked);

//        // Make sure no ancestor invisible Image swallows the click.
//        EnsureRaycastPassthrough();
//    }

//    // ── Click handler ─────────────────────────────────────────────

//    private void OnCannonZoneClicked()
//    {
//        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
//        // TODO: open cannon assignment panel here.
//    }

//    // ── Cannon management ─────────────────────────────────────────

//    public void PlaceCannon(GameObject cannonPrefab = null)
//    {
//        if (hasCannon)
//        {
//            Debug.Log("[CannonSlot] Already has a cannon.");
//            return;
//        }

//        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//        if (prefab != null && slotTransform != null)
//            _cannonInstance = Instantiate(prefab, slotTransform.position,
//                                          Quaternion.identity, slotTransform);

//        hasCannon = true;
//        Debug.Log("[CannonSlot] Cannon placed.");
//    }

//    public void RemoveCannon()
//    {
//        if (!hasCannon) return;
//        if (_cannonInstance != null) Destroy(_cannonInstance);
//        hasCannon = false;
//    }

//    // ── Raycast passthrough ───────────────────────────────────────

//    /// <summary>
//    /// Walks up the transform hierarchy and sets raycastTarget = false on every
//    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
//    /// invisible decorations or layout helpers; they should never eat pointer
//    /// events that are meant for this CannonZone Button.
//    ///
//    /// This is the runtime complement to the prefab-level fix:
//    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
//    ///   • CastleBlock and ExpansionSlot transparent Images should also have
//    ///     raycastTarget = false in the prefab, but this call catches anything
//    ///     that was missed in the editor.
//    /// </summary>
//    private void EnsureRaycastPassthrough()
//    {
//        Transform t = transform.parent;
//        while (t != null)
//        {
//            Image img = t.GetComponent<Image>();
//            if (img != null && img.color.a == 0f)
//                img.raycastTarget = false;

//            t = t.parent;
//        }
//    }
//}

//using UnityEngine;

//public class CannonSlotCastle : MonoBehaviour
//{
//    [Header("Slot State")]
//    public bool hasCannon = false;

//    [Header("Visuals")]
//    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
//    public Transform slotTransform;         // Where to spawn the cannon

//    private GameObject _cannonInstance;

//    public void PlaceCannon(GameObject cannonPrefab = null)
//    {
//        if (hasCannon)
//        {
//            Debug.Log("[CannonSlot] Already has a cannon.");
//            return;
//        }

//        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//        if (prefab != null)
//        {
//            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
//        }

//        hasCannon = true;
//        Debug.Log("[CannonSlot] Cannon placed.");
//    }

//    public void RemoveCannon()
//    {
//        if (!hasCannon) return;
//        if (_cannonInstance != null) Destroy(_cannonInstance);
//        hasCannon = false;
//    }

//    private void OnMouseDown()
//    {
//        // Click to toggle cannon (for testing)
//        if (!hasCannon) PlaceCannon();
//        else RemoveCannon();
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
/// It is a prefab that lives on every castle block's exposed unit slot.
///
/// The CannonZone has a Button component (added manually in the Inspector).
/// Clicking it opens the cannon assignment panel.
///
/// IMPORTANT — click-blocking fix:
///   Invisible castle block Images below this zone must NOT be raycast targets.
///   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
///   but the real fix is also applied in GridCell and CastleGrid:
///   the GridCell background Image has raycastTarget = false (it is just layout),
///   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
///   so Button clicks always reach this zone.
/// </summary>
[RequireComponent(typeof(Button))]
public class CannonSlotCastle : MonoBehaviour
{
    [Header("Slot State")]
    public bool hasCannon = false;

    [Header("Visuals")]
    public GameObject cannonVisualPrefab;
    public Transform slotTransform;

    private GameObject _cannonInstance;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnCannonZoneClicked);

        // Make sure no ancestor invisible Image swallows the click.
        EnsureRaycastPassthrough();
    }

    // ── Click handler ─────────────────────────────────────────────

    private void OnCannonZoneClicked()
    {
        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
        CannonPanelManager.Instance?.OpenFromCastleSlot(this);
    }

    // ── Cannon management ─────────────────────────────────────────

    public void PlaceCannon(GameObject cannonPrefab = null)
    {
        if (hasCannon)
        {
            Debug.Log("[CannonSlot] Already has a cannon.");
            return;
        }

        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
        if (prefab != null && slotTransform != null)
            _cannonInstance = Instantiate(prefab, slotTransform.position,
                                          Quaternion.identity, slotTransform);

        hasCannon = true;
        Debug.Log("[CannonSlot] Cannon placed.");
    }

    public void RemoveCannon()
    {
        if (!hasCannon) return;
        if (_cannonInstance != null) Destroy(_cannonInstance);
        hasCannon = false;
    }

    // ── Raycast passthrough ───────────────────────────────────────

    /// <summary>
    /// Walks up the transform hierarchy and sets raycastTarget = false on every
    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
    /// invisible decorations or layout helpers; they should never eat pointer
    /// events that are meant for this CannonZone Button.
    ///
    /// This is the runtime complement to the prefab-level fix:
    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
    ///   • CastleBlock and ExpansionSlot transparent Images should also have
    ///     raycastTarget = false in the prefab, but this call catches anything
    ///     that was missed in the editor.
    /// </summary>
    private void EnsureRaycastPassthrough()
    {
        Transform t = transform.parent;
        while (t != null)
        {
            Image img = t.GetComponent<Image>();
            if (img != null && img.color.a == 0f)
                img.raycastTarget = false;

            t = t.parent;
        }
    }
}