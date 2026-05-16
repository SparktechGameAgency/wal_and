using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CastlePanelRow : MonoBehaviour
{
    [Header("Row UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI costLabel;
    public TextMeshProUGUI descLabel;
    public Button buyButton;

    private CastleBlockData _data;
    private Action<CastleBlockData> _onBuy;

    public void Setup(CastleBlockData data, Action<CastleBlockData> onBuy)
    {
        _data = data;
        _onBuy = onBuy;

        if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;
        if (nameLabel != null) nameLabel.text = data.blockName;
        if (costLabel != null) costLabel.text = $"{data.cost} coins";
        if (descLabel != null) descLabel.text = data.description;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => _onBuy?.Invoke(_data));
    }
}