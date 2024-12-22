using Sirenix.OdinInspector;
using UnityEngine;
namespace MapGenerationProject.Base
{
    public class HexCell : MonoBehaviour
    {
        [HideLabel] public HexCoordinates coordinates;
        [SerializeField] private HexCell[] neighbors;
        public Color color;
        
        public HexCell GetNeighbor(HexDirection direction) 
        {
            return neighbors[(int)direction];
        }
        
        public void SetNeighbor(HexDirection direction, HexCell cell) 
        {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
    }
}