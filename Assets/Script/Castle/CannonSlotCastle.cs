using UnityEngine;

public class CannonSlotCastle : MonoBehaviour
{
    [Header("Slot State")]
    public bool hasCannon = false;

    [Header("Visuals")]
    public GameObject cannonVisualPrefab;   // Assign cannon sprite/mesh prefab
    public Transform slotTransform;         // Where to spawn the cannon

    private GameObject _cannonInstance;

    public void PlaceCannon(GameObject cannonPrefab = null)
    {
        if (hasCannon)
        {
            Debug.Log("[CannonSlot] Already has a cannon.");
            return;
        }

        GameObject prefab = cannonPrefab ?? cannonVisualPrefab;
        if (prefab != null)
        {
            _cannonInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
        }

        hasCannon = true;
        Debug.Log("[CannonSlot] Cannon placed.");
    }

    public void RemoveCannon()
    {
        if (!hasCannon) return;
        if (_cannonInstance != null) Destroy(_cannonInstance);
        hasCannon = false;
    }

    private void OnMouseDown()
    {
        // Click to toggle cannon (for testing)
        if (!hasCannon) PlaceCannon();
        else RemoveCannon();
    }
}