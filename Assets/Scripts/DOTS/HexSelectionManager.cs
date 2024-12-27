using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.EventSystems;
namespace MapGenerationProject.DOTS
{
    public class HexSelectionManager : MonoBehaviour
    {
        [SerializeField] private IntEventChannel _hexSelected;
        [SerializeField] private VoidEventChannel _refreshMesh;
        [SerializeField] private Color[] _colors;
        private int _activeElevation;
        
        private Camera _camera;
        private Color _activeColor;
        
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
        }

        private void HandleInput() 
        {
            Ray inputRay = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit)) 
            {
                HexCoordinates coordinates = HexCoordinates.FromPosition(hit.point);
                EditCell(coordinates);
            }
        }
        
        private void EditCell(HexCoordinates coordinates) 
        {
            int index = HexMetrics.GetCellIndex(coordinates);
            HexCellData cell = HexGrid.Cells[index];

            Vector3 position = cell.Position;
            Vector4 sample = HexMetrics.SampleNoise(position, HexMetrics.NoiseData);
            position.y = _activeElevation * HexMetrics.ElevationStep;
            position.y += (sample.y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            cell.SetElevation(_activeElevation, position);
            
            cell.Color = _activeColor;

            HexGrid.Cells[index] = cell;
            _hexSelected.RaiseEvent(index);
            _refreshMesh.RaiseEvent();
        }
        
        public void SelectColor(int index) 
        {
            _activeColor = _colors[index];
        }
        
        public void SetElevation(float elevation) 
        {
            _activeElevation = (int)elevation;
        }
    }
}