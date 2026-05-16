////using UnityEngine;

////public class ExpansionCoin : MonoBehaviour
////{
////    [Header("Cost to expand")]
////    public int expansionCost = 50;

////    private ExpansionDirection _direction;
////    private CastleGrid _grid;
////    private bool _setup = false;

////    // Simple bobbing animation
////    private Vector3 _startPos;
////    [Header("Animation")]
////    public float bobSpeed = 2f;
////    public float bobAmount = 0.1f;

////    public void Setup(ExpansionDirection direction, CastleGrid grid)
////    {
////        _direction = direction;
////        _grid = grid;
////        _setup = true;
////        _startPos = transform.position;
////    }

////    private void Update()
////    {
////        // Bob up and down
////        float y = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
////        transform.position = _startPos + new Vector3(0f, y, 0f);
////    }

////    private void OnMouseDown()
////    {
////        if (!_setup) return;

////        if (CurrencyManager.Instance.SpendCoins(expansionCost))
////        {
////            _grid.ExpandGrid(_direction);
////        }
////        else
////        {
////            Debug.Log("[ExpansionCoin] Not enough coins to expand!");
////            // TODO: Play a "no coins" animation/sound
////        }
////    }

////    private void OnMouseEnter()
////    {
////        // Highlight the coin on hover (scale up)
////        transform.localScale = Vector3.one * 1.2f;
////    }

////    private void OnMouseExit()
////    {
////        transform.localScale = Vector3.one;
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using TMPro;
//using System.Collections;

//public class ExpansionSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
//{
//    [Header("Cost")]
//    public int woodCost = 1300;

//    [Header("UI References")]
//    public TextMeshProUGUI costLabel;
//    public Image costIcon;
//    public Image borderImage;

//    [Header("Colors")]
//    public Color normalColor = Color.white;
//    public Color hoverColor = new Color(1f, 0.9f, 0.4f);
//    public Color blockedColor = new Color(1f, 0.3f, 0.3f);

//    private int _row, _col;
//    private CastleGrid _grid;

//    public void Init(int row, int col, CastleGrid grid)
//    {
//        _row = row;
//        _col = col;
//        _grid = grid;
//        RefreshDisplay();
//    }

//    private void RefreshDisplay()
//    {
//        if (costLabel != null)
//            costLabel.text = woodCost.ToString("N0");

//        bool canAfford = CurrencyManager.Instance != null &&
//                         CurrencyManager.Instance.Coins >= woodCost;

//        if (borderImage != null)
//            borderImage.color = canAfford ? normalColor : blockedColor;
//    }

//    public void OnPointerClick(PointerEventData eventData)
//    {
//        if (CurrencyManager.Instance.SpendCoins(woodCost))
//        {
//            _grid.PlaceBlockAt(_row, _col, _grid.castleBlockPrefab);
//        }
//        else
//        {
//            Debug.Log("[ExpansionSlot] Not enough coins!");
//            StartCoroutine(FlashRed());
//        }
//    }

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        transform.localScale = Vector3.one * 1.05f;
//        if (borderImage != null) borderImage.color = hoverColor;
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        transform.localScale = Vector3.one;
//        RefreshDisplay();
//    }

//    private IEnumerator FlashRed()
//    {
//        if (borderImage == null) yield break;
//        borderImage.color = Color.red;
//        yield return new WaitForSeconds(0.3f);
//        RefreshDisplay();
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ExpansionSlot : MonoBehaviour, IPointerClickHandler,
                                            IPointerEnterHandler,
                                            IPointerExitHandler
{
    [Header("Cost")]
    public int blockCost = 100;

    [Header("UI References")]
    public Image borderImage;
    public TextMeshProUGUI costLabel;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.7f);
    public Color hoverColor = new Color(1f, 0.9f, 0.3f, 1f);
    public Color cantAffordColor = new Color(1f, 0.3f, 0.3f, 0.7f);

    private int _row, _col;
    private CastleGrid _grid;
    private bool _initialized = false;
    private bool _isProcessing = false; // prevent double-click

    public void Init(int row, int col, CastleGrid grid)
    {
        _row = row;
        _col = col;
        _grid = grid;
        _initialized = true;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (costLabel != null)
            costLabel.text = blockCost.ToString("N0");
        RefreshBorderColor();
    }

    private void RefreshBorderColor()
    {
        if (borderImage == null) return;
        if (CurrencyManager.Instance == null) return;
        bool canAfford = CurrencyManager.Instance.Coins >= blockCost;
        borderImage.color = canAfford ? normalColor : cantAffordColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Guard: prevent processing if already handled or not ready
        if (_isProcessing) return;
        if (!_initialized)
        {
            Debug.LogWarning("[ExpansionSlot] Clicked but not initialized!");
            return;
        }
        if (_grid == null)
        {
            Debug.LogWarning("[ExpansionSlot] CastleGrid reference is null!");
            return;
        }
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[ExpansionSlot] CurrencyManager not found!");
            return;
        }
        if (_grid.castleBlockPrefab == null)
        {
            Debug.LogWarning("[ExpansionSlot] castleBlockPrefab not assigned on CastleGrid!");
            return;
        }

        _isProcessing = true;

        if (!CurrencyManager.Instance.SpendCoins(blockCost))
        {
            Debug.Log("[ExpansionSlot] Not enough coins!");
            StartCoroutine(FlashRed());
            _isProcessing = false;
            return;
        }

        Debug.Log($"[ExpansionSlot] Placing block at row={_row} col={_col}");
        _grid.PlaceBlockAt(_row, _col, _grid.castleBlockPrefab);

        // _isProcessing stays true — this GameObject gets destroyed
        // by RefreshExpansionSlots right after PlaceBlockAt
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.08f;
        if (borderImage != null) borderImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
        RefreshBorderColor();
    }

    private IEnumerator FlashRed()
    {
        if (borderImage == null) yield break;
        borderImage.color = Color.red;
        yield return new WaitForSeconds(0.25f);
        RefreshBorderColor();
    }
}