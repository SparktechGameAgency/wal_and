//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;

//[System.Serializable]
//public class CastleBlockData
//{
//    public string blockName = "Stone Block";
//    public int cost = 30;
//    public Sprite icon;
//    public GameObject prefab;
//    [TextArea] public string description = "A sturdy stone block.";
//}

//public class CastlePanel : MonoBehaviour
//{
//    [Header("Block Catalogue")]
//    public List<CastleBlockData> availableBlocks;

//    [Header("UI References")]
//    public Transform itemListParent;   // Vertical layout group parent
//    public GameObject itemRowPrefab;    // Row prefab with icon, name, cost, buy button
//    public TextMeshProUGUI coinDisplay;

//    private void Start()
//    {
//        PopulateShop();
//        CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
//        UpdateCoinDisplay(CurrencyManager.Instance.Coins);
//    }

//    void PopulateShop()
//    {
//        foreach (Transform child in itemListParent)
//            Destroy(child.gameObject);

//        foreach (var data in availableBlocks)
//        {
//            GameObject row = Instantiate(itemRowPrefab, itemListParent);
//            CastlePanelRow rowScript = row.GetComponent<CastlePanelRow>();
//            if (rowScript != null) rowScript.Setup(data, OnBuyClicked);
//        }
//    }

//    void OnBuyClicked(CastleBlockData data)
//    {
//        if (!CastleGrid.Instance.HasEmptyCell())
//        {
//            Debug.Log("[CastlePanel] No empty cell! Expand the castle first.");
//            return;
//        }

//        if (CurrencyManager.Instance.SpendCoins(data.cost))
//        {
//            CastleGrid.Instance.BeginPlacingBlock(data.prefab);
//            Debug.Log($"[CastlePanel] Buying {data.blockName}. Click an empty cell to place.");
//        }
//        else
//        {
//            Debug.Log($"[CastlePanel] Not enough coins for {data.blockName}.");
//        }
//    }

//    void UpdateCoinDisplay(int coins)
//    {
//        if (coinDisplay != null) coinDisplay.text = $"Coins: {coins}";
//    }

//    private void OnDestroy()
//    {
//        if (CurrencyManager.Instance != null)
//            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class CastleBlockData
{
    public string blockName = "Stone Block";
    public int cost = 30;
    public Sprite icon;
    public GameObject prefab;
    [TextArea] public string description = "A sturdy stone block.";
}

public class CastlePanel : MonoBehaviour
{
    [Header("Block Catalogue")]
    public List<CastleBlockData> availableBlocks;

    [Header("UI References")]
    public Transform itemListParent;
    public GameObject itemRowPrefab;
    public TextMeshProUGUI coinDisplay;

    private void Start()
    {
        PopulateShop();
        CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
        UpdateCoinDisplay(CurrencyManager.Instance.Coins);
    }

    void PopulateShop()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var data in availableBlocks)
        {
            GameObject row = Instantiate(itemRowPrefab, itemListParent);
            CastlePanelRow rowScript = row.GetComponent<CastlePanelRow>();
            if (rowScript != null) rowScript.Setup(data, OnBuyClicked);
        }
    }

    void OnBuyClicked(CastleBlockData data)
    {
        if (!CurrencyManager.Instance.SpendCoins(data.cost))
        {
            Debug.Log($"[CastlePanel] Not enough coins for {data.blockName}.");
            return;
        }

        // Coins spent — player now clicks an expansion slot to place
        Debug.Log($"[CastlePanel] Bought {data.blockName}. Click an expansion slot.");
    }

    void UpdateCoinDisplay(int coins)
    {
        if (coinDisplay != null) coinDisplay.text = $"Coins: {coins}";
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
    }
}