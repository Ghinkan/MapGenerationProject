using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
namespace MapGenerationProject.DOTS
{
    public class HexSelectionManager : MonoBehaviour
    {
        [FormerlySerializedAs("_onHexSelected")]
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
            
            cell.Color = _activeColor;
            cell.Elevation = _activeElevation;

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