using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour
    {
        private Mesh _hexMesh;
        private MeshCollider _meshCollider;
        private HexMeshGridData _meshGridData;
        private NativeList<Vector3> _vertices;
        private NativeList<int> _triangles;
        private NativeList<Color> _colors;

        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = GetComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
        }

        public void TriangulateChunk(ChunkData chunkData)
        {
            _hexMesh.Clear();
            
            NativeArray<HexCellData> cells = HexGrid.Cells;
            
            int estimatedVertices = cells.Length * 200;
            int estimatedTriangles = cells.Length * 250;
            int estimatedColors = estimatedVertices;
            
            _vertices = new NativeList<Vector3>(estimatedVertices,Allocator.Persistent);
            _triangles = new NativeList<int>(estimatedTriangles,Allocator.Persistent);
            _colors = new NativeList<Color>(estimatedColors,Allocator.Persistent);
            _meshGridData = new HexMeshGridData(_vertices, _triangles, _colors, HexMetrics.NoiseData);
            
            GenerateCenterHexMeshJob generateCenterHexMeshJob = new GenerateCenterHexMeshJob 
            {
                Cells = cells,
                ChunkData = chunkData,
                MeshGridData = _meshGridData,
            };
            
            JobHandle generateCenterHexMeshDataHandle = generateCenterHexMeshJob.Schedule(chunkData.CellsIndex.Length, 64);
            generateCenterHexMeshDataHandle.Complete();
            
            //TODO: utilizar nueva API MeshData para asignar vertices.
            _hexMesh.SetVertices(_vertices.AsArray());
            _hexMesh.SetTriangles(_triangles.AsArray().ToArray(), 0);
            _hexMesh.SetColors(_colors.AsArray());
            _hexMesh.RecalculateNormals();
            
            _meshCollider.sharedMesh = _hexMesh;

            DisposeBuffers();
        }

        private void DisposeBuffers()
        {
            _vertices.Dispose();
            _triangles.Dispose();
            _colors.Dispose();
        }

        [BurstCompile]
        private struct GenerateCenterHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            [ReadOnly] public ChunkData ChunkData;
            public HexMeshGridData MeshGridData;
            
            public void Execute(int index)
            {
                index = ChunkData.CellsIndex[index];
                
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;

                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction), center + HexMetrics.GetSecondSolidCorner(direction));

                    TriangulateEdgeFan(center, e, cell.Color);

                    if (direction <= HexDirection.SE)
                        TriangulateConnection(cell, direction, e);
                }
            }

            private void TriangulateConnection(HexCellData cell, HexDirection direction, EdgeVertices e1)
            {
                if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) return;

                Vector3 bridge = HexMetrics.GetBridge(direction);
                bridge.y = neighbor.Position.y - cell.Position.y;
                EdgeVertices e2 = new EdgeVertices(e1.V1 + bridge, e1.V4 + bridge);

                if (HexMetrics.GetEdgeType(cell.Elevation, neighbor.Elevation) == HexEdgeType.Slope)
                    TriangulateEdgeTerraces(e1, cell, e2, neighbor);
                else
                    TriangulateEdgeStrip(e1, e2, cell.Color, neighbor.Color);

                if (direction <= HexDirection.E && HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
                {
                    Vector3 v5 = e1.V4 + HexMetrics.GetBridge(direction.Next());
                    v5.y = nextNeighbor.Position.y;

                    if (cell.Elevation <= neighbor.Elevation)
                    {
                        if (cell.Elevation <= nextNeighbor.Elevation)
                            TriangulateCorner(e1.V4, cell, e2.V4, neighbor, v5, nextNeighbor);
                        else
                            TriangulateCorner(v5, nextNeighbor, e1.V4, cell, e2.V4, neighbor);
                    }
                    else if (neighbor.Elevation <= nextNeighbor.Elevation)
                        TriangulateCorner(e2.V4, neighbor, v5, nextNeighbor, e1.V4, cell);
                    else
                        TriangulateCorner(v5, nextNeighbor, e1.V4, cell, e2.V4, neighbor);
                }
            }

            private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
            {
                MeshGridData.AddTriangle(center, edge.V1, edge.V2, color);
                MeshGridData.AddTriangle(center, edge.V2, edge.V3, color);
                MeshGridData.AddTriangle(center, edge.V3, edge.V4, color);
            }

            private void TriangulateEdgeStrip(EdgeVertices e1, EdgeVertices e2, Color c1, Color c2)
            {
                MeshGridData.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2, c1, c2);
                MeshGridData.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3, c1, c2);
                MeshGridData.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4, c1, c2);
            }

            private void TriangulateEdgeTerraces(EdgeVertices begin, HexCellData beginCell, EdgeVertices end, HexCellData endCell)
            {
                EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
                Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, 1);

                TriangulateEdgeStrip(begin, e2, beginCell.Color, c2);

                for (int i = 2; i < HexMetrics.TerraceSteps; i++)
                {
                    EdgeVertices e1 = e2;
                    Color c1 = c2;
                    e2 = EdgeVertices.TerraceLerp(begin, end, i);
                    c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, i);
                    TriangulateEdgeStrip(e1, e2, c1, c2);
                }

                TriangulateEdgeStrip(e2, end, c2, endCell.Color);
            }

            private void TriangulateCorner(Vector3 bottom, HexCellData bottomCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                HexEdgeType leftEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, leftCell.Elevation);
                HexEdgeType rightEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, rightCell.Elevation);

                if (leftEdgeType == HexEdgeType.Slope)
                {
                    if (rightEdgeType == HexEdgeType.Slope)
                        TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                    else if (rightEdgeType == HexEdgeType.Flat)
                        TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                    else
                        TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdgeType == HexEdgeType.Slope)
                {
                    if (leftEdgeType == HexEdgeType.Flat)
                        TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                    else
                        TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                {
                    if (leftCell.Elevation < rightCell.Elevation)
                        TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                    else
                        TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                }
                else
                {
                    MeshGridData.AddTriangle(bottom, left, right, bottomCell.Color, leftCell.Color, rightCell.Color);
                }
            }

            private void TriangulateCornerTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
                Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
                Color c3 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
                Color c4 = HexMetrics.TerraceColorLerp(beginCell.Color, rightCell.Color, 1);

                MeshGridData.AddTriangle(begin, v3, v4, beginCell.Color, c3, c4);

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

                    MeshGridData.AddQuad(v1, v2, v3, v4, c1, c2, c3, c4);
                }

                MeshGridData.AddQuad(v3, v4, left, right, c3, c4, leftCell.Color, rightCell.Color);
            }

            private void TriangulateCornerCliffTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));

                Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
                Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

                TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                    TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
                else
                    MeshGridData.AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary, leftCell.Color, rightCell.Color, boundaryColor);
            }

            private void TriangulateCornerTerracesCliff(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));

                Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
                Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

                TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                {
                    TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
                }
                else
                {
                    MeshGridData.AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary, leftCell.Color, rightCell.Color, boundaryColor);
                }
            }

            private void TriangulateBoundaryTriangle(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 boundary, Color boundaryColor)
            {
                Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
                Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);

                MeshGridData.AddTriangleUnperturbed(Perturb(begin), v2, boundary, beginCell.Color, c2, boundaryColor);

                for (int i = 2; i < HexMetrics.TerraceSteps; i++)
                {
                    Vector3 v1 = v2;
                    Color c1 = c2;
                    v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
                    c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, i);

                    MeshGridData.AddTriangleUnperturbed(v1, v2, boundary, c1, c2, boundaryColor);
                }
                MeshGridData.AddTriangleUnperturbed(v2, Perturb(left), boundary, c2, leftCell.Color, boundaryColor);
            }

            private Vector3 Perturb(Vector3 position)
            {
                Vector4 sample = HexMetrics.SampleNoise(position, MeshGridData.TextureData);
                position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
                position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
                return position;
            }
        }
    }
}