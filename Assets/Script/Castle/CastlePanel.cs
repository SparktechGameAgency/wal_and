
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;
////using System.Collections.Generic;

////[System.Serializable]
////public class CastleBlockData
////{
////    public string blockName = "Stone Block";
////    public int cost = 30;
////    public Sprite icon;
////    public GameObject prefab;
////    [TextArea] public string description = "A sturdy stone block.";
////}

////public class CastlePanel : MonoBehaviour
////{
////    [Header("Block Catalogue")]
////    public List<CastleBlockData> availableBlocks;

////    [Header("UI References")]
////    public Transform itemListParent;
////    public GameObject itemRowPrefab;
////    public TextMeshProUGUI coinDisplay;

////    private void Start()
////    {
////        PopulateShop();
////        CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
////        UpdateCoinDisplay(CurrencyManager.Instance.Coins);
////    }

////    void PopulateShop()
////    {
////        foreach (Transform child in itemListParent)
////            Destroy(child.gameObject);

////        foreach (var data in availableBlocks)
////        {
////            GameObject row = Instantiate(itemRowPrefab, itemListParent);
////            CastlePanelRow rowScript = row.GetComponent<CastlePanelRow>();
////            if (rowScript != null) rowScript.Setup(data, OnBuyClicked);
////        }
////    }

////    void OnBuyClicked(CastleBlockData data)
////    {
////        if (!CurrencyManager.Instance.SpendCoins(data.cost))
////        {
////            Debug.Log($"[CastlePanel] Not enough coins for {data.blockName}.");
////            return;
////        }

////        // Coins spent — player now clicks an expansion slot to place
////        Debug.Log($"[CastlePanel] Bought {data.blockName}. Click an expansion slot.");
////    }

////    void UpdateCoinDisplay(int coins)
////    {
////        if (coinDisplay != null) coinDisplay.text = $"Coins: {coins}";
////    }

////    private void OnDestroy()
////    {
////        if (CurrencyManager.Instance != null)
////            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
////    }
////}

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
//    public Transform itemListParent;
//    public GameObject itemRowPrefab;
//    public TextMeshProUGUI coinDisplay;

//    // ── Lifecycle ─────────────────────────────────────────────────

//    private void Start()
//    {
//        PopulateShop();
//        CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
//        UpdateCoinDisplay(CurrencyManager.Instance.Coins);
//    }

//    /// <summary>
//    /// Called by your panel-switching system when this panel becomes active.
//    /// Tells CastleGrid to show expansion slots.
//    /// </summary>
//    private void OnEnable()
//    {
//        // CastleGrid may not exist yet during the very first frame;
//        // the null-check keeps things safe.
//        if (CastleGrid.Instance != null)
//            CastleGrid.Instance.SetExpansionSlotsVisible(true);
//    }

//    /// <summary>
//    /// Called when the panel is hidden / deactivated.
//    /// Expansion slots are turned off so they don't show in other panels.
//    /// </summary>
//    private void OnDisable()
//    {
//        if (CastleGrid.Instance != null)
//            CastleGrid.Instance.SetExpansionSlotsVisible(false);
//    }

//    private void OnDestroy()
//    {
//        if (CurrencyManager.Instance != null)
//            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
//    }

//    // ── Shop ──────────────────────────────────────────────────────

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
//        if (!CurrencyManager.Instance.SpendCoins(data.cost))
//        {
//            Debug.Log($"[CastlePanel] Not enough coins for {data.blockName}.");
//            return;
//        }

//        // Coins spent — player now clicks an expansion slot to place
//        Debug.Log($"[CastlePanel] Bought {data.blockName}. Click an expansion slot.");
//    }

//    void UpdateCoinDisplay(int coins)
//    {
//        if (coinDisplay != null) coinDisplay.text = $"Coins: {coins}";
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

/// <summary>
/// Manages the Castle Panel shop list and coin display.
/// Grid movement and expansion-slot toggling are handled by CastleGridMover.
/// </summary>
public class CastlePanel : MonoBehaviour
{
    [Header("Block Catalogue")]
    public List<CastleBlockData> availableBlocks;

    [Header("UI References")]
    public Transform itemListParent;
    public GameObject itemRowPrefab;
    public TextMeshProUGUI coinDisplay;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Start()
    {
        PopulateShop();
        CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
        UpdateCoinDisplay(CurrencyManager.Instance.Coins);
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
    }

    // ── Shop ──────────────────────────────────────────────────────

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

        Debug.Log($"[CastlePanel] Bought {data.blockName}. Click an expansion slot.");
    }

    void UpdateCoinDisplay(int coins)
    {
        if (coinDisplay != null) coinDisplay.text = $"Coins: {coins}";
    }
}