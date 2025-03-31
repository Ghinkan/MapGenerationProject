using TMPro;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexCellCanvas: MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private IntEventChannel _hexSelected;
        [SerializeField] private HexGridChunk _chunk;
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
            int chunkSize = _chunk.ChunkData.CellsIndex.Length;
            _cellLabels = new TMP_Text[chunkSize];
            for (int i = 0; i < chunkSize; i++)
            {
                int cellIndex = _chunk.ChunkData.CellsIndex[i];
                HexCellData hexCellData = HexGrid.Cells[cellIndex];
                TMP_Text label = Instantiate(_cellLabelPrefab, _gridCanvas.transform, false);
                label.rectTransform.anchoredPosition = new Vector2(hexCellData.Position.x, hexCellData.Position.z);
                label.text = hexCellData.Coordinates.ToStringOnSeparateLines();
                _cellLabels[i] = label;
            }
        }

        private void RefreshLabelPosition(int index)
        {
            for (int i = 0; i < _chunk.ChunkData.CellsIndex.Length; i++)
            {
                if (index == _chunk.ChunkData.CellsIndex[i])
                {
                    Vector3 uiPosition = _cellLabels[i].rectTransform.localPosition;
                    uiPosition.z = HexGrid.Cells[index].Elevation * -HexMetrics.ElevationStep;
                    _cellLabels[i].rectTransform.localPosition = uiPosition;
                    break;
                }
            }
        }
    }
}