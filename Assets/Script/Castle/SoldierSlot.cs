using UnityEngine;

public class SoldierSlot : MonoBehaviour
{
    [Header("Slot State")]
    public bool hasSoldier = false;

    [Header("Visuals")]
    public GameObject soldierVisualPrefab;
    public Transform slotTransform;

    private GameObject _soldierInstance;

    public void PlaceSoldier(GameObject soldierPrefab = null)
    {
        if (hasSoldier)
        {
            Debug.Log("[SoldierSlot] Already has a soldier.");
            return;
        }

        GameObject prefab = soldierPrefab ?? soldierVisualPrefab;
        if (prefab != null)
        {
            _soldierInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity, slotTransform);
        }

        hasSoldier = true;
        Debug.Log("[SoldierSlot] Soldier placed.");
    }

    public void RemoveSoldier()
    {
        if (!hasSoldier) return;
        if (_soldierInstance != null) Destroy(_soldierInstance);
        hasSoldier = false;
    }

    private void OnMouseDown()
    {
        if (!hasSoldier) PlaceSoldier();
        else RemoveSoldier();
    }
}