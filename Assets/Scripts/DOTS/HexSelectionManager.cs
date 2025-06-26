using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.EventSystems;
namespace MapGenerationProject.DOTS
{
    public class HexSelectionManager : MonoBehaviour
    {
        [SerializeField] private IntEventChannel _hexSelected;
        [SerializeField] private IntEventChannel _refreshChunkMesh;
        [SerializeField] private BoolEventChannel _showLabels;
        [SerializeField] private Color[] _colors;
        private int _activeElevation;
        
        private Camera _camera;
        private HexCoordinates? _lastEditedCellCoordinates;

        private bool _canApplyColor;
        private Color _activeColor;

        private bool _canApplyElevation = true;
        
        private int _brushSize;
        
        private void Awake()
        {
            _camera = Camera.main;
            SelectColor(0);
        }
        
        private void Update() 
        {
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) 
            {
                HandleInput();
            }
            if (Input.GetMouseButtonUp(0))
            {
                _lastEditedCellCoordinates = null;
            }
        }

        private void HandleInput() 
        {
            Ray inputRay = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit))
            {
                HexCoordinates editCellCoordinates = HexCoordinates.FromPosition(hit.point);
                
                if (!_lastEditedCellCoordinates.Equals(editCellCoordinates))
                {
                    EditCells(editCellCoordinates);
                    _lastEditedCellCoordinates = editCellCoordinates;
                }
            }
        }
        
        private void EditCells(HexCoordinates center) 
        {
            int centerX = center.X;
            int centerZ = center.Z;
            
            for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++)
            {
                for (int x = centerX - r; x <= centerX + _brushSize; x++)
                {
                    EditCell(new HexCoordinates(x, z));
                }
            }
            
            for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++)
            {
                for (int x = centerX - _brushSize; x <= centerX + r; x++)
                {
                    EditCell(new HexCoordinates(x, z));
                }
            }
        }
        
        private void EditCell(HexCoordinates coordinates) 
        {
            if(!HexMetrics.TryGetCellIndex(coordinates, out int index)) return;
            HexCellData cell = HexGrid.Cells[index];

            if (_canApplyElevation)
            {
                Vector3 position = cell.Position;
                Vector4 sample = HexMetrics.SampleNoise(position, HexMetrics.NoiseData);
                position.y = _activeElevation * HexMetrics.ElevationStep;
                position.y += (sample.y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                cell.SetElevation(_activeElevation, position);
            }
            
            if(_canApplyColor)
                cell.Color = _activeColor;

            HexGrid.Cells[index] = cell;
            _hexSelected.RaiseEvent(index);
            _refreshChunkMesh.RaiseEvent(cell.ChunkIndex);
        }
        
        public void SelectColor(int index) 
        {
            _canApplyColor = index >= 0;
            if (_canApplyColor)
                _activeColor = _colors[index];
        }
        
        public void SetElevation(float elevation) 
        {
            _activeElevation = (int)elevation;
        }
        
        public void SetApplyElevation(bool toggle)
        {
            _canApplyElevation = toggle;
        }
        
        public void SetBrushSize (float size)
        {
            _brushSize = (int)size;
        }
        
        public void ShowUI(bool visible)
        {
            _showLabels.RaiseEvent(visible);
        }
    }
}