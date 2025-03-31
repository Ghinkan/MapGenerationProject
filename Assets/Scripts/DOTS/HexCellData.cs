using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexCellData
    {
        public int ChunkIndex;
        public Vector3 Position;
        public HexCoordinates Coordinates;
        public Color Color;
        public int Elevation { get; private set; }
        
        public void SetElevation(int elevation, Vector3 position) 
        {
            Elevation = elevation;
            Position = position;
        }
    }
}