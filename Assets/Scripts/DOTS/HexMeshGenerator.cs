using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexMeshGenerator : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private VoidEventChannel _refreshMesh;

        private Mesh _hexMesh;
        private MeshCollider _meshCollider;
        
        private NativeList<Vector3> _vertices;
        private NativeList<int> _triangles;
        private NativeList<Color> _colors;
        
        private NativeArray<HexMeshData> _outputData;
        
        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = GetComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
        }
        
        private void OnEnable()
        {
            _onGridCreated.GameEvent += Triangulate;
            _refreshMesh.GameEvent += Triangulate;
        }

        private void OnDisable()
        { 
            _onGridCreated.GameEvent -= Triangulate;
            _refreshMesh.GameEvent -= Triangulate;
        }

        private void Triangulate()
        {
            _hexMesh.Clear();
            NativeArray<HexCellData> cells = HexGrid.Cells;
            _outputData = new NativeArray<HexMeshData>(cells.Length, Allocator.Persistent);
            
            GenerateCenterHexMeshJob generateCenterHexMeshJob = new GenerateCenterHexMeshJob 
            {
                Cells = cells,
                TextureData = HexMetrics.NoiseData,
                OutputData = _outputData,
            };
            
            JobHandle generateCenterHexMeshDataHandle = generateCenterHexMeshJob.Schedule(cells.Length, 64);
            generateCenterHexMeshDataHandle.Complete();
            
            _vertices = new NativeList<Vector3>(Allocator.Persistent);
            _triangles = new NativeList<int>(Allocator.Persistent);
            _colors = new NativeList<Color>(Allocator.Persistent);
            
            int globalBaseIndex = 0;

            for (int i = 0; i < _outputData.Length; i++)
            {
                HexMeshData localMesh = _outputData[i];

                // Agregar vértices y ajustar índices.
                _vertices.AddRange(localMesh.Vertices.AsArray());
                for (int t = 0; t < localMesh.Triangles.Length; t++)
                {
                    _triangles.Add(localMesh.Triangles[t] + globalBaseIndex);
                }
                _colors.AddRange(localMesh.Colors.AsArray());

                globalBaseIndex += localMesh.Vertices.Length;
                
                localMesh.Dispose();
            }
            
            List<int> triangleList = new List<int>(_triangles.Length);
            triangleList.AddRange(_triangles.AsArray());
            
            _hexMesh.SetVertices(_vertices.AsArray());
            _hexMesh.SetTriangles(triangleList, 0);
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
            
            _outputData.Dispose();
        }
        
        [BurstCompile]
        private struct GenerateCenterHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            [ReadOnly] public TextureData TextureData;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<HexMeshData> OutputData;

            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                HexMeshData meshData = new HexMeshData
                {
                    Vertices = new NativeList<Vector3>(Allocator.TempJob),
                    Triangles = new NativeList<int>(Allocator.TempJob),
                    Colors = new NativeList<Color>(Allocator.TempJob),
                    TextureData = TextureData,
                };

                Vector3 center = cell.Position;

                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction), center + HexMetrics.GetSecondSolidCorner(direction));

                    TriangulateEdgeFan(meshData, center, e, cell.Color);
                    
                    if (direction <= HexDirection.SE)
                        TriangulateConnection(meshData, cell, direction, e);
                }

                OutputData[index] = meshData;
            }

            private void TriangulateConnection(HexMeshData meshData, HexCellData cell, HexDirection direction, EdgeVertices e1)
            {
                if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) return;

                Vector3 bridge = HexMetrics.GetBridge(direction);
                bridge.y = neighbor.Position.y - cell.Position.y;
                EdgeVertices e2 = new EdgeVertices(e1.V1 + bridge, e1.V4 + bridge);

                if (HexMetrics.GetEdgeType(cell.Elevation, neighbor.Elevation) == HexEdgeType.Slope)
                    TriangulateEdgeTerraces(meshData, e1, cell, e2, neighbor);
                else
                    TriangulateEdgeStrip(meshData, e1, e2, cell.Color, neighbor.Color);

                if (direction <= HexDirection.E && HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
                {
                    Vector3 v5 = e1.V4 + HexMetrics.GetBridge(direction.Next());
                    v5.y = nextNeighbor.Position.y;

                    if (cell.Elevation <= neighbor.Elevation)
                    {
                        if (cell.Elevation <= nextNeighbor.Elevation)
                            TriangulateCorner(meshData, e1.V4, cell, e2.V4, neighbor, v5, nextNeighbor);
                        else
                            TriangulateCorner(meshData, v5, nextNeighbor, e1.V4, cell, e2.V4, neighbor);
                    }
                    else if (neighbor.Elevation <= nextNeighbor.Elevation)
                        TriangulateCorner(meshData, e2.V4, neighbor, v5, nextNeighbor, e1.V4, cell);
                    else
                        TriangulateCorner(meshData, v5, nextNeighbor, e1.V4, cell, e2.V4, neighbor);
                } 
            }
            
            private void TriangulateEdgeFan(HexMeshData meshData, Vector3 center, EdgeVertices edge, Color color)
            {
                meshData.AddTriangle(center, edge.V1, edge.V2, color);
                meshData.AddTriangle(center, edge.V2, edge.V3, color);
                meshData.AddTriangle(center, edge.V3, edge.V4, color);
            }

            private void TriangulateEdgeStrip(HexMeshData meshData, EdgeVertices e1, EdgeVertices e2, Color c1, Color c2)
            {
                meshData.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2, c1, c2);
                meshData.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3, c1, c2);
                meshData.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4, c1, c2);
            }
            
            private void TriangulateEdgeTerraces(HexMeshData meshData, EdgeVertices begin, HexCellData beginCell, EdgeVertices end, HexCellData endCell)
            {
                EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
                Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, 1);

                TriangulateEdgeStrip(meshData, begin, e2, beginCell.Color, c2);

                for (int i = 2; i < HexMetrics.TerraceSteps; i++)
                {
                    EdgeVertices e1 = e2;
                    Color c1 = c2;
                    e2 = EdgeVertices.TerraceLerp(begin, end, i);
                    c2 = HexMetrics.TerraceColorLerp(beginCell.Color, endCell.Color, i);
                    TriangulateEdgeStrip( meshData, e1, e2, c1, c2);
                }

                TriangulateEdgeStrip(meshData, e2, end, c2, endCell.Color);
            }
            
            private void TriangulateCorner(HexMeshData meshData, Vector3 bottom, HexCellData bottomCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                HexEdgeType leftEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, leftCell.Elevation);
                HexEdgeType rightEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, rightCell.Elevation);

                if (leftEdgeType == HexEdgeType.Slope)
                {
                    if (rightEdgeType == HexEdgeType.Slope)
                        TriangulateCornerTerraces(meshData, bottom, bottomCell, left, leftCell, right, rightCell);
                    else if (rightEdgeType == HexEdgeType.Flat)
                        TriangulateCornerTerraces(meshData, left, leftCell, right, rightCell, bottom, bottomCell);
                    else
                        TriangulateCornerTerracesCliff(meshData, bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdgeType == HexEdgeType.Slope)
                {
                    if (leftEdgeType == HexEdgeType.Flat)
                        TriangulateCornerTerraces(meshData, right, rightCell, bottom, bottomCell, left, leftCell);
                    else
                        TriangulateCornerCliffTerraces(meshData, bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                {
                    if (leftCell.Elevation < rightCell.Elevation)
                        TriangulateCornerCliffTerraces(meshData, right, rightCell, bottom, bottomCell, left, leftCell);
                    else
                        TriangulateCornerTerracesCliff(meshData, left, leftCell, right, rightCell, bottom, bottomCell);
                }
                else
                {
                    meshData.AddTriangle(bottom, left, right, bottomCell.Color, leftCell.Color, rightCell.Color);
                }
            }
            
            private void TriangulateCornerTerraces(HexMeshData meshData, Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
                Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
                Color c3 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
                Color c4 = HexMetrics.TerraceColorLerp(beginCell.Color, rightCell.Color, 1);

                meshData.AddTriangle(begin, v3, v4, beginCell.Color, c3, c4);

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
                    
                    meshData.AddQuad(v1, v2, v3, v4, c1, c2, c3, c4);
                }

                meshData.AddQuad(v3, v4, left, right, c3, c4, leftCell.Color, rightCell.Color);
            }
            
            private void TriangulateCornerCliffTerraces(HexMeshData meshData, Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));
            
                Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
                Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

                TriangulateBoundaryTriangle(meshData, right, rightCell, begin, beginCell, boundary, boundaryColor);

                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope)
                    TriangulateBoundaryTriangle(meshData, left, leftCell, right, rightCell, boundary, boundaryColor);
                else
                    meshData.AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary, leftCell.Color, rightCell.Color, boundaryColor);
            }
            
            private void TriangulateCornerTerracesCliff(HexMeshData meshData, Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));
            
                Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
                Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);
                
                TriangulateBoundaryTriangle(meshData, begin, beginCell, left, leftCell, boundary, boundaryColor);
                
                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope) 
                {
                    TriangulateBoundaryTriangle(meshData, left, leftCell, right, rightCell, boundary, boundaryColor);
                }
                else 
                {
                    meshData.AddTriangleUnperturbed(left, right, boundary , leftCell.Color, rightCell.Color, boundaryColor);
                }
            }
            
            private void TriangulateBoundaryTriangle(HexMeshData meshData, Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 boundary, Color boundaryColor)
            {
                Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
                Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);

                meshData.AddTriangleUnperturbed(Perturb(begin), v2, boundary, beginCell.Color, c2, boundaryColor);

                for (int i = 2; i < HexMetrics.TerraceSteps; i++)
                {
                    Vector3 v1 = v2;
                    Color c1 = c2;
                    v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
                    c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, i);
                    
                    meshData.AddTriangleUnperturbed(v1, v2, boundary, c1, c2, boundaryColor);
                }
                meshData.AddTriangleUnperturbed(v2, Perturb(left), boundary, c2, leftCell.Color, boundaryColor);
            }

            private Vector3 Perturb(Vector3 position)
            {
                Vector4 sample = HexMetrics.SampleNoise(position, TextureData);
                position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
                position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
                return position;
            }
        }
    }
}