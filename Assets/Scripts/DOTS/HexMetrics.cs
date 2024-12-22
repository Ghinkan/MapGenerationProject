﻿using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;
        
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
    }
}