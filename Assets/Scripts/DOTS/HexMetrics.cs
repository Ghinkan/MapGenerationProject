using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;
        
        public static readonly int Width;
        public static readonly int Height;
        
        public const float SolidFactor = 0.75f;
        public const float BlendFactor = 1f - SolidFactor;
		
        public static readonly Vector3[] Corners = 
        {
            new Vector3(0f, 0f, OuterRadius),
            new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(0f, 0f, -OuterRadius),
            new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(0f, 0f, OuterRadius),
        };
        
        static HexMetrics()
        {
            Width = HexGrid.Width;
            Height = HexGrid.Height;
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
            if (z < 0 || z >= 10 || x < 0 || x >= 10)
            {
                cell = default(HexCellData);
                return false;
            }

            cell = cells[x + z * 10];
            return true;
        }
    }
}