using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class HexMeshTriangulationUtils
    {
        /// <summary>
        /// Triangulates an edge fan from a center point to edge vertices.
        /// </summary>
        public static void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color, ref HexMeshGridData meshGridData)
        {
            meshGridData.AddTriangle(center, edge.V1, edge.V2, color);
            meshGridData.AddTriangle(center, edge.V2, edge.V3, color);
            meshGridData.AddTriangle(center, edge.V3, edge.V4, color);
        }

        /// <summary>
        /// Triangulates a strip between two edges with color interpolation.
        /// </summary>
        public static void TriangulateEdgeStrip(EdgeVertices e1, EdgeVertices e2, Color c1, Color c2, ref HexMeshGridData meshGridData)
        {
            meshGridData.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2, c1, c2);
            meshGridData.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3, c1, c2);
            meshGridData.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4, c1, c2);
        }

        /// <summary>
        /// Triangulates a triangle with individual vertex colors.
        /// </summary>
        public static void TriangulateTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3, ref HexMeshGridData meshGridData)
        {
            meshGridData.AddTriangle(v1, v2, v3, c1, c2, c3);
        }

        /// <summary>
        /// Triangulates a quad with individual vertex colors.
        /// </summary>
        public static void TriangulateQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2, Color c3, Color c4, ref HexMeshGridData meshGridData)
        {
            meshGridData.AddQuad(v1, v2, v3, v4, c1, c2, c3, c4);
        }

        /// <summary>
        /// Triangulates a triangle without applying perturbation.
        /// </summary>
        public static void TriangulateTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3, ref HexMeshGridData meshGridData)
        {
            meshGridData.AddTriangleUnperturbed(v1, v2, v3, c1, c2, c3);
        }
    }
    
    public static class HexEdgeUtils
    {
        /// <summary>
        /// Triangulates edge terraces between two edges with smooth color transitions.
        /// </summary>
        private static void TriangulateEdgeTerraces(EdgeVertices begin, HexCellData beginCell, EdgeVertices end, HexCellData endCell, ref HexMeshGridData meshGridData)
        {
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, 1);

            HexMeshTriangulationUtils.TriangulateEdgeStrip(begin, e2, beginCell.Color, c2, ref meshGridData);

            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = c2;
                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, i);
                HexMeshTriangulationUtils.TriangulateEdgeStrip(e1, e2, c1, c2, ref meshGridData);
            }

            HexMeshTriangulationUtils.TriangulateEdgeStrip(e2, end, c2, endCell.Color, ref meshGridData);
        }

        /// <summary>
        /// Gets the appropriate edge type between two cells based on their elevations.
        /// </summary>
        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            int delta = elevation2 - elevation1;
            if (delta == 0)
            {
                return HexEdgeType.Flat;
            }
            
            int step = delta > 0 ? 1 : -1;
            if (Mathf.Abs(delta) == 1)
            {
                return HexEdgeType.Slope;
            }
            
            return HexEdgeType.Cliff;
        }

        /// <summary>
        /// Creates a connection between two cells with appropriate edge types.
        /// </summary>
        public static void CreateConnection(HexCellData cell, HexDirection direction, EdgeVertices e1, NativeArray<HexCellData> cells, ref HexMeshGridData meshGridData)
        {
            if (!HexMetrics.TryGetCell(cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) 
                return;

            Vector3 bridge = HexMetrics.GetBridge(direction);
            bridge.y = neighbor.Position.y - cell.Position.y;
            EdgeVertices e2 = new EdgeVertices(e1.V1 + bridge, e1.V4 + bridge);

            if (GetEdgeType(cell.Elevation, neighbor.Elevation) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(e1, cell, e2, neighbor, ref meshGridData);
            }
            else
            {
                HexMeshTriangulationUtils.TriangulateEdgeStrip(e1, e2, cell.Color, neighbor.Color, ref meshGridData);
            }

            // Handle corner connections if needed
            if (direction <= HexDirection.E && HexMetrics.TryGetCell(cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
            {
                Vector3 v5 = e1.V4 + HexMetrics.GetBridge(direction.Next());
                v5.y = nextNeighbor.Position.y;

                HexCornerUtils.TriangulateCorner(e1.V4, cell, e2.V4, neighbor, v5, nextNeighbor, ref meshGridData);
            }
        }
    }
    
    public static class HexCornerUtils
    {
        /// <summary>
        /// Triangulates a corner between three cells with appropriate terrain types.
        /// </summary>
        public static void TriangulateCorner(Vector3 bottom, HexCellData bottomCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell, ref HexMeshGridData meshGridData)
        {
            HexEdgeType leftEdgeType = HexEdgeUtils.GetEdgeType(bottomCell.Elevation, leftCell.Elevation);
            HexEdgeType rightEdgeType = HexEdgeUtils.GetEdgeType(bottomCell.Elevation, rightCell.Elevation);

            if (leftEdgeType == HexEdgeType.Slope)
            {
                if (rightEdgeType == HexEdgeType.Slope)
                {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell, ref meshGridData);
                }
                else if (rightEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell, ref meshGridData);
                }
                else
                {
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell, ref meshGridData);
                }
            }
            else if (rightEdgeType == HexEdgeType.Slope)
            {
                if (leftEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell, ref meshGridData);
                }
                else
                {
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell, ref meshGridData);
                }
            }
            else if (HexEdgeUtils.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
            {
                if (leftCell.Elevation < rightCell.Elevation)
                {
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell, ref meshGridData);
                }
                else
                {
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell, ref meshGridData);
                }
            }
            else
            {
                HexMeshTriangulationUtils.TriangulateTriangle(bottom, left, right, bottomCell.Color, leftCell.Color, rightCell.Color, ref meshGridData);
            }
        }

        /// <summary>
        /// Triangulates a corner with terraces on both sides.
        /// </summary>
        private static void TriangulateCornerTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell, ref HexMeshGridData meshGridData)
        {
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
            Color c4 = HexMetrics.TerraceColorLerp(beginCell.Color, rightCell.Color, 1);

            HexMeshTriangulationUtils.TriangulateTriangle(begin, v3, v4, beginCell.Color, c3, c4, ref meshGridData);

            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v3;
                Vector3 v2 = v4;
                Color c1 = c3;
                Color c2 = c4;
                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, i);
                c4 = HexMetrics.TerraceColorLerp(beginCell.Color, rightCell.Color, i);

                HexMeshTriangulationUtils.TriangulateQuad(v1, v2, v3, v4, c1, c2, c3, c4, ref meshGridData);
            }

            HexMeshTriangulationUtils.TriangulateQuad(v3, v4, left, right, c3, c4, leftCell.Color, rightCell.Color, ref meshGridData);
        }

        /// <summary>
        /// Triangulates a corner where there's a cliff on one side and terraces on the other.
        /// </summary>
        private static void TriangulateCornerCliffTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell, ref HexMeshGridData meshGridData)
        {
            float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));
        
            Vector3 boundary = Vector3.Lerp(Perturb(begin, ref meshGridData), Perturb(left, ref meshGridData), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);
        
            TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor, ref meshGridData);
        
            if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor, ref meshGridData);
            else
                HexMeshTriangulationUtils.TriangulateTriangleUnperturbed(Perturb(left, ref meshGridData), Perturb(right, ref meshGridData), boundary, leftCell.Color, rightCell.Color, boundaryColor, ref meshGridData);
        }
        
        /// <summary>
        /// Triangulates a corner where there's a terrace on one side and a cliff on the other.
        /// </summary>
        private static void TriangulateCornerTerracesCliff(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell, ref HexMeshGridData meshGridData)
        {
            float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));
        
            Vector3 boundary = Vector3.Lerp(Perturb(begin, ref meshGridData), Perturb(right, ref meshGridData), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);
        
            TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor, ref meshGridData);
        
            if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor, ref meshGridData);
            }
            else
            {
                HexMeshTriangulationUtils.TriangulateTriangleUnperturbed(Perturb(left, ref meshGridData), Perturb(right, ref meshGridData), boundary, leftCell.Color, rightCell.Color, boundaryColor, ref meshGridData);
            }
        }
        
        /// <summary>
        /// Triangulates a boundary triangle between three points with smooth color transitions.
        /// </summary>
        private static void TriangulateBoundaryTriangle(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 boundary, Color boundaryColor, ref HexMeshGridData meshGridData)
        {
            Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1), ref meshGridData);
            Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
        
            HexMeshTriangulationUtils.TriangulateTriangleUnperturbed(Perturb(begin, ref meshGridData), v2, boundary, beginCell.Color, c2, boundaryColor, ref meshGridData);
        
            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i), ref meshGridData);
                c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, i);
        
                HexMeshTriangulationUtils.TriangulateTriangleUnperturbed(v1, v2, boundary, c1, c2, boundaryColor, ref meshGridData);
            }
            HexMeshTriangulationUtils.TriangulateTriangleUnperturbed(v2, Perturb(left, ref meshGridData), boundary, c2, leftCell.Color, boundaryColor, ref meshGridData);
        }
        
        private static Vector3 Perturb(Vector3 position, ref HexMeshGridData meshGridData)
        {
            Vector4 sample = HexMetrics.SampleNoise(position, meshGridData.TextureData);
            position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
            return position;
        }
    }
}