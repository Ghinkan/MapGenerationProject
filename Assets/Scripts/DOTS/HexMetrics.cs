using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;
        private static readonly Vector3[] Corners = 
        {
            new Vector3(0f, 0f, OuterRadius),
            new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(0f, 0f, -OuterRadius),
            new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(0f, 0f, OuterRadius),
        };

        public const int ChunkCellSizeX = 10;
        public const int ChunkCellSizeZ = 10;
        public const int ChunkCountX = 5;
        public const int ChunkCountZ = 5;
        public const int Width = ChunkCellSizeX * ChunkCountX;
        public const int Height = ChunkCellSizeZ * ChunkCountZ;
        
        public const float ElevationStep = 5f;
        public const int TerracesPerSlope = 2;
        public const int TerraceSteps = TerracesPerSlope * 2 + 1;
        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

        private const float SolidFactor = 0.75f;
        private const float BlendFactor = 1f - SolidFactor;
        
        // Constants for mesh estimation
        private const int VerticesPerTriangle = 3;
        private const int TrianglesPerHexSide = 3; // 1 fan triangle per corner vertex (3 vertices per side)
        private const int TrianglesPerTerraceStrip = 2 * (TerraceSteps - 1); // 2 triangles per terrace step
        private const int TrianglesPerConnection = TrianglesPerTerraceStrip + 2; // +2 for the end caps
        private const int MaxConnectionsPerCell = 6; // Hexagons have 6 sides

        // Estimated vertices and triangles per cell
        public const int EstimatedVerticesPerCell = (TrianglesPerHexSide * VerticesPerTriangle) + // Center fan
            (MaxConnectionsPerCell * TrianglesPerConnection * VerticesPerTriangle); // Connections
        public const int EstimatedTrianglesPerCell = TrianglesPerHexSide + // Center fan
            (MaxConnectionsPerCell * TrianglesPerConnection); // Connections

        public static TextureData NoiseData; //TODO: Comprobar si puedo hacerlo readonly y settear todas las variables en un scriptable object
        public const float NoiseScale = 0.003f;
        public const float CellPerturbStrength = 5f;
        public const float ElevationPerturbStrength = 1.5f;
        
        public static Vector4 SampleNoise(Vector3 position, TextureData noiseData)
        {
            // Convertir coordenadas del mundo a coordenadas normalizadas [0, 1]
            float u = position.x * NoiseScale % 1f;
            float v = position.z * NoiseScale % 1f;
            if (u < 0) u += 1f;
            if (v < 0) v += 1f;
                
            Vector4 sample = TextureUtils.SampleBilinear(noiseData, u, v);
            return sample;
        }
        
        public static Vector3 GetFirstSolidCorner(HexDirection direction) 
        {
            return Corners[(int)direction] * SolidFactor;
        }

        public static Vector3 GetSecondSolidCorner(HexDirection direction) 
        {
            return Corners[(int)direction + 1] * SolidFactor;
        }
		
        public static Vector3 GetBridge(HexDirection direction) 
        {
            return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
        }
        
        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) 
        {
            float h = step * HorizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;
            float v = (step + 1) / 2 * VerticalTerraceStepSize;
            a.y += (b.y - a.y) * v;
            return a;
        }
        
        public static Color TerraceColorLerp(Color a, Color b, int step) 
        {
            float h = step * HorizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }
        
        public static HexEdgeType GetEdgeType(int elevation1, int elevation2) 
        {
            int delta = Mathf.Abs(elevation2 - elevation1);
            return delta switch 
            {
                0 => HexEdgeType.Flat,
                1 => HexEdgeType.Slope,
                _ => HexEdgeType.Cliff,
            };
        }
        
        public static int GetCellIndex(HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= Height || x < 0 || x >= Width)
                return 0;

            return x + z * Width;
        }
        
        public static bool TryGetCellIndex(HexCoordinates coordinates, out int index)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= Height || x < 0 || x >= Width)
            {
                index = 0;
                return false;
            }

            index = x + z * Width;
            return true;
        }
        
        public static HexCellData GetCell(NativeArray<HexCellData> cells, HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= Height || x < 0 || x >= Width)
                return default(HexCellData);

            return cells[x + z * Width];
        }
        
        public static bool TryGetCell(NativeArray<HexCellData> cells, HexCoordinates coordinates, out HexCellData cell)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= Height || x < 0 || x >= Width)
            {
                cell = default(HexCellData);
                return false;
            }

            cell = cells[x + z * Width];
            return true;
        }

        public static NativeArray<HexCellData> GetNeighbors(NativeArray<HexCellData> cells, HexCellData cell)
        {
            NativeArray<HexCellData> neighbors = new NativeArray<HexCellData>(6, Allocator.Temp);
            int i = 0;
            
            for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                if (TryGetCell(cells, cell.Coordinates.Step(direction), out HexCellData neighbor))
                {
                    neighbors[i] = neighbor;
                    i++;
                }
            }
            
            return neighbors;
        }
    }
}