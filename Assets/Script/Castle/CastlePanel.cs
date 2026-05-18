
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