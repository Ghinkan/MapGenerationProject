using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.EventSystems;
namespace MapGenerationProject.DOTS
{
    public class HexSelectionManager : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;
        
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
                IHexSelectable hexSelected = hit.transform.gameObject.GetComponentInParent<IHexSelectable>();
                if (hexSelected != null)
                {
                    HexCoordinates coordinates = HexCoordinates.FromPosition(hit.point);
                    hexSelected.ColorCell(coordinates, _activeColor);
                }
            }
        }
        
        public void SelectColor(int index) 
        {
            _activeColor = _colors[index];
        }
    }
}