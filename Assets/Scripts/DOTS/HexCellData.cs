using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexCellData
    {
        public Vector3 Position;
        public HexCoordinates Coordinates;
        public Color Color;
        private int _elevation;
        public int Elevation
        {
            get 
            {
                return _elevation;
            }
            set 
            {
                _elevation = value;
                Position.y = value * HexMetrics.ElevationStep;
                Position.y += (HexMetrics.SampleNoise(Position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            }
        }
    }
}