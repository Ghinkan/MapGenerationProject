using UnityEngine;
using UnityEngine.EventSystems;
namespace MapGenerationProject.Base
{
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Color[] colors;
        private Color activeColor;

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
                hexGrid.ColorCell(hit.point, activeColor);
            }
        }

        public void SelectColor (int index) 
        {
            activeColor = colors[index];
        }
    }
}