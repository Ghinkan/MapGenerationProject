using TMPro;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexCellCanvas: MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private IntEventChannel _hexSelected;
        [SerializeField] private TMP_Text _cellLabelPrefab;

        private Canvas _gridCanvas;
        private TMP_Text[] _cellLabels;

        private void Awake()
        {
            _gridCanvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            _onGridCreated.GameEvent += InstantiateCellLabels;
            _hexSelected.GameEvent += RefreshLabelPosition;
        }
        
        private void OnDisable()
        {
            _onGridCreated.GameEvent -= InstantiateCellLabels;
            _hexSelected.GameEvent -= RefreshLabelPosition;
        }
        
        private void InstantiateCellLabels()
        {
            _cellLabels = new TMP_Text[HexGrid.Cells.Length];
            for (int i = 0; i < HexGrid.Cells.Length; i++)
            {
                HexCellData hexCellData = HexGrid.Cells[i];
                TMP_Text label = Instantiate(_cellLabelPrefab, _gridCanvas.transform, false);
                label.rectTransform.anchoredPosition = new Vector2(hexCellData.Position.x, hexCellData.Position.z);
                label.text = hexCellData.Coordinates.ToStringOnSeparateLines();
                _cellLabels[i] = label;
            }
        }

        private void RefreshLabelPosition(int index)
        {
            Vector3 uiPosition = _cellLabels[index].rectTransform.localPosition;
            uiPosition.z = HexGrid.Cells[index].Elevation * -HexMetrics.ElevationStep;
            _cellLabels[index].rectTransform.localPosition = uiPosition;
        }
    }
}