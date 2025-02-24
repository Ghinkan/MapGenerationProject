using UnityEngine;
using UnityEngine.EventSystems;
namespace MapGenerationProject.Base
{
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Color[] colors;
        private Color activeColor;
        int activeElevation;

        private void Awake() 
        {
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
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit)) 
            {
                EditCell(hexGrid.GetCell(hit.point));
            }
        }
        
        public void EditCell(HexCell cell) 
        {
            cell.Color = activeColor;
            cell.Elevation = activeElevation;
        }

        public void SelectColor(int index) 
        {
            activeColor = colors[index];
        }
        
        public void SetElevation(float elevation) 
        {
            activeElevation = (int)elevation;
        }
    }
}