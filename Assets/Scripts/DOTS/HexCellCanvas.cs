using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexCellCanvas: MonoBehaviour
    {
        [SerializeField] private HexCellDataEventChannel _onMeshCreated;
        [SerializeField] private TMP_Text _cellLabelPrefab;

        private Canvas _gridCanvas;

        private void Awake()
        {
            _gridCanvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            _onMeshCreated.GameEvent += InstantiateCellLabels;
        }
        
        private void OnDisable()
        {
            _onMeshCreated.GameEvent -= InstantiateCellLabels;
        }
        
        private void InstantiateCellLabels(NativeArray<HexCellData> cells)
        {
            foreach (HexCellData hexCellData in cells)
            {
                TMP_Text label = Instantiate(_cellLabelPrefab, _gridCanvas.transform, false);
                label.rectTransform.anchoredPosition = new Vector2(hexCellData.Position.x, hexCellData.Position.z);
                label.text = hexCellData.Coordinates.ToStringOnSeparateLines();
            }
        }
    }
}