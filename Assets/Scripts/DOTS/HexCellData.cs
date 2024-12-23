using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexCellData
    {
        public Vector3 Position;
        public HexCoordinates Coordinates;
        public Color Color;
        
        public HexCellData GetNeighbor(NativeArray<HexCellData> cells, HexDirection direction) 
        { 
            return HexMetrics.GetCell(cells,Coordinates.Step(direction));
        }
        
        public readonly bool TryGetNeighbor(NativeArray<HexCellData> cells, HexDirection direction, out HexCellData cell)
        {
            return HexMetrics.TryGetCell(cells, Coordinates.Step(direction), out cell);
        }
    }
}