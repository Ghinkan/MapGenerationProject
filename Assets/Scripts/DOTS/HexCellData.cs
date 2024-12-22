using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexCellData
    {
        public Vector3 Position;
        public HexCoordinates Coordinates;
        public Color Color;
        
        public HexCellData GetNeighbor(HexDirection direction) 
        { 
            return HexGrid.GetCell(Coordinates.Step(direction));
        }
        
        public readonly bool TryGetNeighbor(HexDirection direction, NativeArray<HexCellData> cells, out HexCellData cell)
        {
            return HexGrid.TryGetCell(Coordinates.Step(direction), cells, out cell);
        }
    }
}