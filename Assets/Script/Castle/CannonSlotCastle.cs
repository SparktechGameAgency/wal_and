//////////using UnityEngine;

//////////public class CannonSlotCastle : MonoBehaviour
//////////{
//////////    [Header("Slot State")]
//////////    public bool hasCannon = false;

//////////    [Header("Visuals")]
//////////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
//////////    public Transform slotTransform;         // Where to spawn the cannon

//////////    private GameObject _cannonInstance;

//////////    public void PlaceCannon(GameObject cannonPrefab = null)
//////////    {
//////////        if (hasCannon)
//////////        {
//////////            Debug.Log("[CannonSlot] Already has a cannon.");
//////////            return;
//////////        }

//////////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//////////        if (prefab != null)
//////////        {
//////////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
//////////        }

//////////        hasCannon = true;
//////////        Debug.Log("[CannonSlot] Cannon placed.");
//////////    }

//////////    public void RemoveCannon()
//////////    {
//////////        if (!hasCannon) return;
//////////        if (_cannonInstance != null) Destroy(_cannonInstance);
//////////        hasCannon = false;
//////////    }

//////////    private void OnMouseDown()
//////////    {
//////////        // Click to toggle cannon (for testing)
//////////        if (!hasCannon) PlaceCannon();
//////////        else RemoveCannon();
//////////    }
//////////}

////////using UnityEngine;
////////using UnityEngine.UI;

/////////// <summary>
/////////// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
/////////// It is a prefab that lives on every castle block's exposed unit slot.
///////////
/////////// The CannonZone has a Button component (added manually in the Inspector).
/////////// Clicking it opens the cannon assignment panel.
///////////
/////////// IMPORTANT — click-blocking fix:
///////////   Invisible castle block Images below this zone must NOT be raycast targets.
///////////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
///////////   but the real fix is also applied in GridCell and CastleGrid:
///////////   the GridCell background Image has raycastTarget = false (it is just layout),
///////////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
///////////   so Button clicks always reach this zone.
/////////// </summary>
////////[RequireComponent(typeof(Button))]
////////public class CannonSlotCastle : MonoBehaviour
////////{
////////    [Header("Slot State")]
////////    public bool hasCannon = false;

////////    [Header("Visuals")]
////////    public GameObject cannonVisualPrefab;
////////    public Transform slotTransform;

////////    private GameObject _cannonInstance;
////////    private Button _button;

////////    private void Awake()
////////    {
////////        _button = GetComponent<Button>();
////////        _button.onClick.AddListener(OnCannonZoneClicked);

////////        // Make sure no ancestor invisible Image swallows the click.
////////        EnsureRaycastPassthrough();
////////    }

////////    // ── Click handler ─────────────────────────────────────────────

////////    private void OnCannonZoneClicked()
////////    {
////////        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
////////        // TODO: open cannon assignment panel here.
////////    }

////////    // ── Cannon management ─────────────────────────────────────────

////////    public void PlaceCannon(GameObject cannonPrefab = null)
////////    {
////////        if (hasCannon)
////////        {
////////            Debug.Log("[CannonSlot] Already has a cannon.");
////////            return;
////////        }

////////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////////        if (prefab != null && slotTransform != null)
////////            _cannonInstance = Instantiate(prefab, slotTransform.position,
////////                                          Quaternion.identity, slotTransform);

////////        hasCannon = true;
////////        Debug.Log("[CannonSlot] Cannon placed.");
////////    }

////////    public void RemoveCannon()
////////    {
////////        if (!hasCannon) return;
////////        if (_cannonInstance != null) Destroy(_cannonInstance);
////////        hasCannon = false;
////////    }

////////    // ── Raycast passthrough ───────────────────────────────────────

////////    /// <summary>
////////    /// Walks up the transform hierarchy and sets raycastTarget = false on every
////////    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
////////    /// invisible decorations or layout helpers; they should never eat pointer
////////    /// events that are meant for this CannonZone Button.
////////    ///
////////    /// This is the runtime complement to the prefab-level fix:
////////    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
////////    ///   • CastleBlock and ExpansionSlot transparent Images should also have
////////    ///     raycastTarget = false in the prefab, but this call catches anything
////////    ///     that was missed in the editor.
////////    /// </summary>
////////    private void EnsureRaycastPassthrough()
////////    {
////////        Transform t = transform.parent;
////////        while (t != null)
////////        {
////////            Image img = t.GetComponent<Image>();
////////            if (img != null && img.color.a == 0f)
////////                img.raycastTarget = false;

////////            t = t.parent;
////////        }
////////    }
////////}

////////using UnityEngine;

////////public class CannonSlotCastle : MonoBehaviour
////////{
////////    [Header("Slot State")]
////////    public bool hasCannon = false;

////////    [Header("Visuals")]
////////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
////////    public Transform slotTransform;         // Where to spawn the cannon

////////    private GameObject _cannonInstance;

////////    public void PlaceCannon(GameObject cannonPrefab = null)
////////    {
////////        if (hasCannon)
////////        {
////////            Debug.Log("[CannonSlot] Already has a cannon.");
////////            return;
////////        }

////////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////////        if (prefab != null)
////////        {
////////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
////////        }

////////        hasCannon = true;
////////        Debug.Log("[CannonSlot] Cannon placed.");
////////    }

////////    public void RemoveCannon()
////////    {
////////        if (!hasCannon) return;
////////        if (_cannonInstance != null) Destroy(_cannonInstance);
////////        hasCannon = false;
////////    }

////////    private void OnMouseDown()
////////    {
////////        // Click to toggle cannon (for testing)
////////        if (!hasCannon) PlaceCannon();
////////        else RemoveCannon();
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
///////// It is a prefab that lives on every castle block's exposed unit slot.
/////////
///////// The CannonZone has a Button component (added manually in the Inspector).
///////// Clicking it opens the cannon assignment panel.
/////////
///////// IMPORTANT — click-blocking fix:
/////////   Invisible castle block Images below this zone must NOT be raycast targets.
/////////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
/////////   but the real fix is also applied in GridCell and CastleGrid:
/////////   the GridCell background Image has raycastTarget = false (it is just layout),
/////////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
/////////   so Button clicks always reach this zone.
///////// </summary>
//////[RequireComponent(typeof(Button))]
//////public class CannonSlotCastle : MonoBehaviour
//////{
//////    [Header("Slot State")]
//////    public bool hasCannon = false;

//////    [Header("Visuals")]
//////    public GameObject cannonVisualPrefab;
//////    public Transform slotTransform;

//////    private GameObject _cannonInstance;
//////    private Button _button;

//////    private void Awake()
//////    {
//////        _button = GetComponent<Button>();
//////        _button.onClick.AddListener(OnCannonZoneClicked);

//////        // Make sure no ancestor invisible Image swallows the click.
//////        EnsureRaycastPassthrough();
//////    }

//////    // ── Click handler ─────────────────────────────────────────────

//////    private void OnCannonZoneClicked()
//////    {
//////        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
//////        CannonPanelManager.Instance?.OpenFromCastleSlot(this);
//////    }

//////    // ── Cannon management ─────────────────────────────────────────

//////    public void PlaceCannon(GameObject cannonPrefab = null)
//////    {
//////        if (hasCannon)
//////        {
//////            Debug.Log("[CannonSlot] Already has a cannon.");
//////            return;
//////        }

//////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//////        if (prefab != null && slotTransform != null)
//////            _cannonInstance = Instantiate(prefab, slotTransform.position,
//////                                          Quaternion.identity, slotTransform);

//////        hasCannon = true;
//////        Debug.Log("[CannonSlot] Cannon placed.");
//////    }

//////    public void RemoveCannon()
//////    {
//////        if (!hasCannon) return;
//////        if (_cannonInstance != null) Destroy(_cannonInstance);
//////        hasCannon = false;
//////    }

//////    // ── Raycast passthrough ───────────────────────────────────────

//////    /// <summary>
//////    /// Walks up the transform hierarchy and sets raycastTarget = false on every
//////    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
//////    /// invisible decorations or layout helpers; they should never eat pointer
//////    /// events that are meant for this CannonZone Button.
//////    ///
//////    /// This is the runtime complement to the prefab-level fix:
//////    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
//////    ///   • CastleBlock and ExpansionSlot transparent Images should also have
//////    ///     raycastTarget = false in the prefab, but this call catches anything
//////    ///     that was missed in the editor.
//////    /// </summary>
//////    private void EnsureRaycastPassthrough()
//////    {
//////        Transform t = transform.parent;
//////        while (t != null)
//////        {
//////            Image img = t.GetComponent<Image>();
//////            if (img != null && img.color.a == 0f)
//////                img.raycastTarget = false;

//////            t = t.parent;
//////        }
//////    }
//////}

//////using UnityEngine;

//////public class CannonSlotCastle : MonoBehaviour
//////{
//////    [Header("Slot State")]
//////    public bool hasCannon = false;

//////    [Header("Visuals")]
//////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
//////    public Transform slotTransform;         // Where to spawn the cannon

//////    private GameObject _cannonInstance;

//////    public void PlaceCannon(GameObject cannonPrefab = null)
//////    {
//////        if (hasCannon)
//////        {
//////            Debug.Log("[CannonSlot] Already has a cannon.");
//////            return;
//////        }

//////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//////        if (prefab != null)
//////        {
//////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
//////        }

//////        hasCannon = true;
//////        Debug.Log("[CannonSlot] Cannon placed.");
//////    }

//////    public void RemoveCannon()
//////    {
//////        if (!hasCannon) return;
//////        if (_cannonInstance != null) Destroy(_cannonInstance);
//////        hasCannon = false;
//////    }

//////    private void OnMouseDown()
//////    {
//////        // Click to toggle cannon (for testing)
//////        if (!hasCannon) PlaceCannon();
//////        else RemoveCannon();
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
/////// It is a prefab that lives on every castle block's exposed unit slot.
///////
/////// The CannonZone has a Button component (added manually in the Inspector).
/////// Clicking it opens the cannon assignment panel.
///////
/////// IMPORTANT — click-blocking fix:
///////   Invisible castle block Images below this zone must NOT be raycast targets.
///////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
///////   but the real fix is also applied in GridCell and CastleGrid:
///////   the GridCell background Image has raycastTarget = false (it is just layout),
///////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
///////   so Button clicks always reach this zone.
/////// </summary>
////[RequireComponent(typeof(Button))]
////public class CannonSlotCastle : MonoBehaviour
////{
////    [Header("Slot State")]
////    public bool hasCannon = false;

////    [Header("Visuals")]
////    public GameObject cannonVisualPrefab;
////    public Transform slotTransform;

////    private GameObject _cannonInstance;
////    private Button _button;

////    private void Awake()
////    {
////        _button = GetComponent<Button>();
////        _button.onClick.AddListener(OnCannonZoneClicked);

////        // The CannonZone GameObject itself has a CanvasGroup (visible in the Inspector).
////        // Ensure it is fully interactive — if interactable or blocksRaycasts is false
////        // the Button will never receive clicks regardless of what parent groups do.
////        CanvasGroup cg = GetComponent<CanvasGroup>();
////        if (cg != null)
////        {
////            cg.interactable = true;
////            cg.blocksRaycasts = true;
////        }

////        // Make sure no ancestor invisible Image swallows the click.
////        EnsureRaycastPassthrough();
////    }

////    // ── Click handler ─────────────────────────────────────────────

////    private void OnCannonZoneClicked()
////    {
////        Debug.Log("[CannonSlotCastle] CannonZone clicked — opening Cannon Panel.");
////        CannonPanelManager.Instance?.OpenFromCastleSlot(this);
////    }

////    // ── Cannon management ─────────────────────────────────────────

////    public void PlaceCannon(GameObject cannonPrefab = null)
////    {
////        if (hasCannon)
////        {
////            Debug.Log("[CannonSlot] Already has a cannon.");
////            return;
////        }

////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////        if (prefab != null && slotTransform != null)
////            _cannonInstance = Instantiate(prefab, slotTransform.position,
////                                          Quaternion.identity, slotTransform);

////        hasCannon = true;
////        Debug.Log("[CannonSlot] Cannon placed.");
////    }

////    public void RemoveCannon()
////    {
////        if (!hasCannon) return;
////        if (_cannonInstance != null) Destroy(_cannonInstance);
////        hasCannon = false;
////    }

////    // ── Raycast passthrough ───────────────────────────────────────

////    /// <summary>
////    /// Walks up the transform hierarchy and sets raycastTarget = false on every
////    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
////    /// invisible decorations or layout helpers; they should never eat pointer
////    /// events that are meant for this CannonZone Button.
////    ///
////    /// This is the runtime complement to the prefab-level fix:
////    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
////    ///   • CastleBlock and ExpansionSlot transparent Images should also have
////    ///     raycastTarget = false in the prefab, but this call catches anything
////    ///     that was missed in the editor.
////    /// </summary>
////    private void EnsureRaycastPassthrough()
////    {
////        Transform t = transform.parent;
////        while (t != null)
////        {
////            // Invisible Images (alpha == 0) must not eat pointer events.
////            Image img = t.GetComponent<Image>();
////            if (img != null && img.color.a == 0f)
////                img.raycastTarget = false;

////            // Any ancestor CanvasGroup with blocksRaycasts = false will silently
////            // swallow all pointer events for this entire subtree, including Button clicks.
////            // Force every ancestor group to pass raycasts through.
////            CanvasGroup cg = t.GetComponent<CanvasGroup>();
////            if (cg != null)
////            {
////                cg.blocksRaycasts = true;
////                cg.interactable = true;
////            }

////            t = t.parent;
////        }
////    }
////}

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

//        // The CannonZone GameObject itself has a CanvasGroup (visible in the Inspector).
//        // Ensure it is fully interactive — if interactable or blocksRaycasts is false
//        // the Button will never receive clicks regardless of what parent groups do.
//        CanvasGroup cg = GetComponent<CanvasGroup>();
//        if (cg != null)
//        {
//            cg.interactable = true;
//            cg.blocksRaycasts = true;
//        }

//        // Make sure no ancestor invisible Image swallows the click.
//        EnsureRaycastPassthrough();
//    }

//    // ── Click handler ─────────────────────────────────────────────

//    private void OnCannonZoneClicked()
//    {
//        Debug.Log("[CannonSlotCastle] CannonZone clicked — opening Cannon Panel.");
//        GameManager.Instance?.OpenCannonPanel();
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
//            // Invisible Images (alpha == 0) must not eat pointer events.
//            Image img = t.GetComponent<Image>();
//            if (img != null && img.color.a == 0f)
//                img.raycastTarget = false;

//            // Any ancestor CanvasGroup with blocksRaycasts = false will silently
//            // swallow all pointer events for this entire subtree, including Button clicks.
//            // Force every ancestor group to pass raycasts through.
//            CanvasGroup cg = t.GetComponent<CanvasGroup>();
//            if (cg != null)
//            {
//                cg.blocksRaycasts = true;
//                cg.interactable = true;
//            }

//            t = t.parent;
//        }
//    }
//}

////////using UnityEngine;

////////public class CannonSlotCastle : MonoBehaviour
////////{
////////    [Header("Slot State")]
////////    public bool hasCannon = false;

////////    [Header("Visuals")]
////////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
////////    public Transform slotTransform;         // Where to spawn the cannon

////////    private GameObject _cannonInstance;

////////    public void PlaceCannon(GameObject cannonPrefab = null)
////////    {
////////        if (hasCannon)
////////        {
////////            Debug.Log("[CannonSlot] Already has a cannon.");
////////            return;
////////        }

////////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////////        if (prefab != null)
////////        {
////////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
////////        }

////////        hasCannon = true;
////////        Debug.Log("[CannonSlot] Cannon placed.");
////////    }

////////    public void RemoveCannon()
////////    {
////////        if (!hasCannon) return;
////////        if (_cannonInstance != null) Destroy(_cannonInstance);
////////        hasCannon = false;
////////    }

////////    private void OnMouseDown()
////////    {
////////        // Click to toggle cannon (for testing)
////////        if (!hasCannon) PlaceCannon();
////////        else RemoveCannon();
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;

///////// <summary>
///////// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
///////// It is a prefab that lives on every castle block's exposed unit slot.
/////////
///////// The CannonZone has a Button component (added manually in the Inspector).
///////// Clicking it opens the cannon assignment panel.
/////////
///////// IMPORTANT — click-blocking fix:
/////////   Invisible castle block Images below this zone must NOT be raycast targets.
/////////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
/////////   but the real fix is also applied in GridCell and CastleGrid:
/////////   the GridCell background Image has raycastTarget = false (it is just layout),
/////////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
/////////   so Button clicks always reach this zone.
///////// </summary>
//////[RequireComponent(typeof(Button))]
//////public class CannonSlotCastle : MonoBehaviour
//////{
//////    [Header("Slot State")]
//////    public bool hasCannon = false;

//////    [Header("Visuals")]
//////    public GameObject cannonVisualPrefab;
//////    public Transform slotTransform;

//////    private GameObject _cannonInstance;
//////    private Button _button;

//////    private void Awake()
//////    {
//////        _button = GetComponent<Button>();
//////        _button.onClick.AddListener(OnCannonZoneClicked);

//////        // Make sure no ancestor invisible Image swallows the click.
//////        EnsureRaycastPassthrough();
//////    }

//////    // ── Click handler ─────────────────────────────────────────────

//////    private void OnCannonZoneClicked()
//////    {
//////        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
//////        // TODO: open cannon assignment panel here.
//////    }

//////    // ── Cannon management ─────────────────────────────────────────

//////    public void PlaceCannon(GameObject cannonPrefab = null)
//////    {
//////        if (hasCannon)
//////        {
//////            Debug.Log("[CannonSlot] Already has a cannon.");
//////            return;
//////        }

//////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//////        if (prefab != null && slotTransform != null)
//////            _cannonInstance = Instantiate(prefab, slotTransform.position,
//////                                          Quaternion.identity, slotTransform);

//////        hasCannon = true;
//////        Debug.Log("[CannonSlot] Cannon placed.");
//////    }

//////    public void RemoveCannon()
//////    {
//////        if (!hasCannon) return;
//////        if (_cannonInstance != null) Destroy(_cannonInstance);
//////        hasCannon = false;
//////    }

//////    // ── Raycast passthrough ───────────────────────────────────────

//////    /// <summary>
//////    /// Walks up the transform hierarchy and sets raycastTarget = false on every
//////    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
//////    /// invisible decorations or layout helpers; they should never eat pointer
//////    /// events that are meant for this CannonZone Button.
//////    ///
//////    /// This is the runtime complement to the prefab-level fix:
//////    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
//////    ///   • CastleBlock and ExpansionSlot transparent Images should also have
//////    ///     raycastTarget = false in the prefab, but this call catches anything
//////    ///     that was missed in the editor.
//////    /// </summary>
//////    private void EnsureRaycastPassthrough()
//////    {
//////        Transform t = transform.parent;
//////        while (t != null)
//////        {
//////            Image img = t.GetComponent<Image>();
//////            if (img != null && img.color.a == 0f)
//////                img.raycastTarget = false;

//////            t = t.parent;
//////        }
//////    }
//////}

//////using UnityEngine;

//////public class CannonSlotCastle : MonoBehaviour
//////{
//////    [Header("Slot State")]
//////    public bool hasCannon = false;

//////    [Header("Visuals")]
//////    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
//////    public Transform slotTransform;         // Where to spawn the cannon

//////    private GameObject _cannonInstance;

//////    public void PlaceCannon(GameObject cannonPrefab = null)
//////    {
//////        if (hasCannon)
//////        {
//////            Debug.Log("[CannonSlot] Already has a cannon.");
//////            return;
//////        }

//////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
//////        if (prefab != null)
//////        {
//////            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
//////        }

//////        hasCannon = true;
//////        Debug.Log("[CannonSlot] Cannon placed.");
//////    }

//////    public void RemoveCannon()
//////    {
//////        if (!hasCannon) return;
//////        if (_cannonInstance != null) Destroy(_cannonInstance);
//////        hasCannon = false;
//////    }

//////    private void OnMouseDown()
//////    {
//////        // Click to toggle cannon (for testing)
//////        if (!hasCannon) PlaceCannon();
//////        else RemoveCannon();
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;

/////// <summary>
/////// Attach this to the CannonZone prefab (the child of CastleBlockUnitSlot).
/////// It is a prefab that lives on every castle block's exposed unit slot.
///////
/////// The CannonZone has a Button component (added manually in the Inspector).
/////// Clicking it opens the cannon assignment panel.
///////
/////// IMPORTANT — click-blocking fix:
///////   Invisible castle block Images below this zone must NOT be raycast targets.
///////   Call EnsureRaycastPassthrough() from Awake to guarantee this at runtime,
///////   but the real fix is also applied in GridCell and CastleGrid:
///////   the GridCell background Image has raycastTarget = false (it is just layout),
///////   and SetUnitSlotVillageMode now keeps blocksRaycasts = TRUE in both modes
///////   so Button clicks always reach this zone.
/////// </summary>
////[RequireComponent(typeof(Button))]
////public class CannonSlotCastle : MonoBehaviour
////{
////    [Header("Slot State")]
////    public bool hasCannon = false;

////    [Header("Visuals")]
////    public GameObject cannonVisualPrefab;
////    public Transform slotTransform;

////    private GameObject _cannonInstance;
////    private Button _button;

////    private void Awake()
////    {
////        _button = GetComponent<Button>();
////        _button.onClick.AddListener(OnCannonZoneClicked);

////        // Make sure no ancestor invisible Image swallows the click.
////        EnsureRaycastPassthrough();
////    }

////    // ── Click handler ─────────────────────────────────────────────

////    private void OnCannonZoneClicked()
////    {
////        Debug.Log("[CannonSlotCastle] CannonZone clicked.");
////        CannonPanelManager.Instance?.OpenFromCastleSlot(this);
////    }

////    // ── Cannon management ─────────────────────────────────────────

////    public void PlaceCannon(GameObject cannonPrefab = null)
////    {
////        if (hasCannon)
////        {
////            Debug.Log("[CannonSlot] Already has a cannon.");
////            return;
////        }

////        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
////        if (prefab != null && slotTransform != null)
////            _cannonInstance = Instantiate(prefab, slotTransform.position,
////                                          Quaternion.identity, slotTransform);

////        hasCannon = true;
////        Debug.Log("[CannonSlot] Cannon placed.");
////    }

////    public void RemoveCannon()
////    {
////        if (!hasCannon) return;
////        if (_cannonInstance != null) Destroy(_cannonInstance);
////        hasCannon = false;
////    }

////    // ── Raycast passthrough ───────────────────────────────────────

////    /// <summary>
////    /// Walks up the transform hierarchy and sets raycastTarget = false on every
////    /// Image that has a fully-transparent (alpha == 0) color.  These objects are
////    /// invisible decorations or layout helpers; they should never eat pointer
////    /// events that are meant for this CannonZone Button.
////    ///
////    /// This is the runtime complement to the prefab-level fix:
////    ///   • GridCell.Init() already sets its own Image.raycastTarget = false.
////    ///   • CastleBlock and ExpansionSlot transparent Images should also have
////    ///     raycastTarget = false in the prefab, but this call catches anything
////    ///     that was missed in the editor.
////    /// </summary>
////    private void EnsureRaycastPassthrough()
////    {
////        Transform t = transform.parent;
////        while (t != null)
////        {
////            Image img = t.GetComponent<Image>();
////            if (img != null && img.color.a == 0f)
////                img.raycastTarget = false;

////            t = t.parent;
////        }
////    }
////}

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

//        // The CannonZone GameObject itself has a CanvasGroup (visible in the Inspector).
//        // Ensure it is fully interactive — if interactable or blocksRaycasts is false
//        // the Button will never receive clicks regardless of what parent groups do.
//        CanvasGroup cg = GetComponent<CanvasGroup>();
//        if (cg != null)
//        {
//            cg.interactable = true;
//            cg.blocksRaycasts = true;
//        }

//        // Make sure no ancestor invisible Image swallows the click.
//        EnsureRaycastPassthrough();
//    }

//    // ── Click handler ─────────────────────────────────────────────

//    private void OnCannonZoneClicked()
//    {
//        Debug.Log("[CannonSlotCastle] CannonZone clicked — opening Cannon Panel.");
//        CannonPanelManager.Instance?.OpenFromCastleSlot(this);
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
//            // Invisible Images (alpha == 0) must not eat pointer events.
//            Image img = t.GetComponent<Image>();
//            if (img != null && img.color.a == 0f)
//                img.raycastTarget = false;

//            // Any ancestor CanvasGroup with blocksRaycasts = false will silently
//            // swallow all pointer events for this entire subtree, including Button clicks.
//            // Force every ancestor group to pass raycasts through.
//            CanvasGroup cg = t.GetComponent<CanvasGroup>();
//            if (cg != null)
//            {
//                cg.blocksRaycasts = true;
//                cg.interactable = true;
//            }

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

    // Tracks which inventory entry is placed here so it can be unequipped later.
    [HideInInspector] public CannonInventoryEntry equippedEntry;

    [Header("Visuals")]
    public GameObject cannonVisualPrefab;
    public Transform slotTransform;

    private GameObject _cannonInstance;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnCannonZoneClicked);

        // The CannonZone GameObject itself has a CanvasGroup (visible in the Inspector).
        // Ensure it is fully interactive — if interactable or blocksRaycasts is false
        // the Button will never receive clicks regardless of what parent groups do.
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Make sure no ancestor invisible Image swallows the click.
        EnsureRaycastPassthrough();
    }

    // ── Click handler ─────────────────────────────────────────────

    private void OnCannonZoneClicked()
    {
        Debug.Log("[CannonSlotCastle] CannonZone clicked — opening Cannon Panel.");
        // Pass this castle slot so the panel knows where to place the cannon prefab.
        if (CannonPanelManager.Instance != null)
            CannonPanelManager.Instance.OpenFromCastleSlot(this);
        else
            GameManager.Instance?.OpenCannonPanel();
    }

    // ── Cannon management ─────────────────────────────────────────

    /// <summary>
    /// Places a cannon prefab into this slot and records which inventory entry owns it.
    /// Called by CannonPanelManager.OnEquipClicked when opened from a CannonZone.
    /// </summary>
    public void PlaceCannon(GameObject cannonPrefab = null, CannonInventoryEntry entry = null)
    {
        if (hasCannon)
        {
            Debug.Log("[CannonSlotCastle] Already has a cannon.");
            return;
        }

        // Resolve which prefab to spawn: argument > inspector fallback.
        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
        Transform parent = slotTransform != null ? slotTransform : transform;

        if (prefab != null)
        {
            _cannonInstance = Instantiate(prefab, parent);
            // Fit perfectly over the slot using RectTransform if this is a UI prefab.
            RectTransform rt = _cannonInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            else
            {
                _cannonInstance.transform.position = parent.position;
                _cannonInstance.transform.localScale = Vector3.one;
            }
        }

        hasCannon = true;
        equippedEntry = entry;

        // Mark the entry as equipped so the panel knows it is in use.
        if (entry != null)
        {
            entry.isEquipped = true;
            // equippedSlot is a CannonSlot reference used by the old slot system;
            // we leave it null here since this is a CannonSlotCastle, not a CannonSlot.
        }

        Debug.Log($"[CannonSlotCastle] Cannon placed: {(prefab != null ? prefab.name : "no prefab")}");
    }

    public void RemoveCannon()
    {
        if (!hasCannon) return;

        if (_cannonInstance != null) { Destroy(_cannonInstance); _cannonInstance = null; }

        if (equippedEntry != null)
        {
            equippedEntry.isEquipped = false;
            equippedEntry.equippedSlot = null;
            CannonPanelManager.Instance?.RefreshAfterUnequip();
            equippedEntry = null;
        }

        hasCannon = false;
        Debug.Log("[CannonSlotCastle] Cannon removed.");
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
            // Invisible Images (alpha == 0) must not eat pointer events.
            Image img = t.GetComponent<Image>();
            if (img != null && img.color.a == 0f)
                img.raycastTarget = false;

            // Any ancestor CanvasGroup with blocksRaycasts = false will silently
            // swallow all pointer events for this entire subtree, including Button clicks.
            // Force every ancestor group to pass raycasts through.
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            t = t.parent;
        }
    }
}