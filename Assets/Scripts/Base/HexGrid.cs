using TMPro;
using UnityEngine;
namespace MapGenerationProject.Base
{
    public class HexGrid : MonoBehaviour
    {
        public int width = 6;
        public int height = 6;

        public HexCell cellPrefab;
        
        HexCell[] cells;
        
        public TMP_Text cellLabelPrefab;

        Canvas gridCanvas;
        HexMesh hexMesh;
        
        public Color defaultColor = Color.white;

        private void Awake() 
        {
            gridCanvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();
        }
        
        private void Start() 
        {
            cells = new HexCell[height * width];
            int index = 0;
            for (int z = 0; z < height; z++) 
            {
                for (int x = 0; x < width; x++) 
                {
                    CreateCell(x, z, index++);
                }
            }
            hexMesh.Triangulate(cells);
        }
	
        private void CreateCell(int x, int z, int index) 
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.OuterRadius * 1.5f);

            HexCell cell = cells[index] = Instantiate(cellPrefab);
            cell.transform.SetParent(transform, false);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.color = defaultColor;
            
            if (x > 0) 
            {
                cell.SetNeighbor(HexDirection.W, cells[index - 1]);
            }
            if (z > 0)
            {
                if ((z & 1) == 0) 
                {
                    cell.SetNeighbor(HexDirection.SE, cells[index - width]);
                    if (x > 0) 
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[index - width - 1]);
                    }
                }
                else 
                {
                    cell.SetNeighbor(HexDirection.SW, cells[index - width]);
                    if (x < width - 1) 
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[index - width + 1]);
                    }
                }
            }
            
            TMP_Text label = Instantiate(cellLabelPrefab, gridCanvas.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();
            cell.uiRect = label.rectTransform;
        }
        
        public HexCell GetCell(Vector3 position) 
        {
            position = transform.InverseTransformPoint(position);
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
            return cells[index];
        }
        
        public void Refresh() 
        {
            hexMesh.Triangulate(cells);
        }
    }
}