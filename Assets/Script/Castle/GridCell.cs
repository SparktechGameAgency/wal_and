//using UnityEngine;
//using UnityEngine.UI;

//[RequireComponent(typeof(Image))]
//public class GridCell : MonoBehaviour
//{
//    [Header("Cell Colors")]
//    [SerializeField] private Color colorEmpty = new Color(1f, 1f, 1f, 0.15f);
//    [SerializeField] private Color colorHighlight = new Color(0.2f, 1f, 0.3f, 0.45f);
//    [SerializeField] private Color colorOccupied = new Color(0.2f, 0.2f, 0.2f, 0.0f);

//    [HideInInspector] public int row;
//    [HideInInspector] public int col;

//    public bool IsOccupied => _occupyingBlock != null;
//    public CastleBlock OccupyingBlock => _occupyingBlock;

//    private Image _img;
//    private CastleBlock _occupyingBlock;

//    private void Awake()
//    {
//        _img = GetComponent<Image>();
//        SetColor(colorEmpty);
//    }

//    public void SetHighlight(bool on)
//    {
//        if (IsOccupied) return;
//        SetColor(on ? colorHighlight : colorEmpty);
//    }

//    public void PlaceBlock(CastleBlock block)
//    {
//        _occupyingBlock = block;
//        SetColor(colorOccupied);
//    }

//    public void ClearBlock()
//    {
//        _occupyingBlock = null;
//        SetColor(colorEmpty);
//    }

//    private void SetColor(Color c)
//    {
//        if (_img != null) _img.color = c;
//    }
//}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCell : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool HasBlock => _block != null;

    private CastleBlock _block;
    private GameObject _expansionSlotInstance;
    private CastleGrid _grid;

    public void Init(int row, int col, CastleGrid grid)
    {
        Row = row;
        Col = col;
        _grid = grid;

        GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // invisible
    }

    public void PlaceBlock(CastleBlock block)
    {
        _block = block;
        HideExpansionSlot();
    }

    public void ClearBlock()
    {
        _block = null;
        _grid.RefreshExpansionSlots();
    }

    public void ShowExpansionSlot(GameObject slotPrefab)
    {
        if (_expansionSlotInstance != null) return;

        _expansionSlotInstance = Instantiate(slotPrefab, transform);

        RectTransform rt = _expansionSlotInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        ExpansionSlot slot = _expansionSlotInstance.GetComponent<ExpansionSlot>();
        if (slot != null) slot.Init(Row, Col, _grid);
    }

    public void HideExpansionSlot()
    {
        if (_expansionSlotInstance != null)
        {
            Destroy(_expansionSlotInstance);
            _expansionSlotInstance = null;
        }
    }
}