﻿using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;

        public const int Width = 20;
        public const int Height = 20;
        
        public const float ElevationStep = 5f;
        public const int TerracesPerSlope = 2;
        public const int TerraceSteps = TerracesPerSlope * 2 + 1;
        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

        private const float SolidFactor = 0.75f;
        private const float BlendFactor = 1f - SolidFactor;

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
    }
}