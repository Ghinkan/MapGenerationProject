using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private VoidEventChannel _refreshMesh;
        [SerializeField] private Texture2D _noiseSource;

        private Mesh _hexMesh;
        private MeshCollider _meshCollider;

        private TextureData _textureData;
        private NativeArray<Vector3> _vertices;
        private NativeArray<int> _triangles;
        private NativeArray<Color> _colors;

        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = GetComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
            
            _textureData = new TextureData(TextureUtils.ConvertTexture2DToNativeArray(_noiseSource, Allocator.Persistent), _noiseSource.width, _noiseSource.height);
            HexMetrics.NoiseData = _textureData;
        }
        
        private void OnEnable()
        {
            HexMetrics.NoiseData = _textureData;
            
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
            int centerHexCount = cells.Length * 18;
            int connectionVerticesCount = cells.Length * 6 * 4 * (5);   //TODO:Hace falta?
            int connectionTrianglesCount = cells.Length * 6 * 6 * (5);  //TODO:Hace falta?
            
            int totalVerticesCount = centerHexCount + connectionVerticesCount;
            int totalTrianglesCount = centerHexCount + connectionTrianglesCount;
            int totalColorsCount = centerHexCount + connectionVerticesCount;

            _vertices = new NativeArray<Vector3>(totalVerticesCount, Allocator.Persistent);
            _triangles = new NativeArray<int>(totalTrianglesCount, Allocator.Persistent);
            _colors = new NativeArray<Color>(totalColorsCount, Allocator.Persistent);

            GenerateCenterHexMeshJob generateCenterHexMeshJob = new GenerateCenterHexMeshJob
            {
                Cells = cells,
                Vertices = _vertices.GetSubArray(0, centerHexCount),
                Triangles = _triangles.GetSubArray(0, centerHexCount),
                Colors = _colors.GetSubArray(0, centerHexCount),
                TextureData = _textureData,
            };

            GenerateConnectionHexMeshJob generateConnectionHexMeshJob = new GenerateConnectionHexMeshJob
            {
                Cells = cells,
                Vertices = _vertices.GetSubArray(centerHexCount, connectionVerticesCount),
                Triangles = _triangles.GetSubArray(centerHexCount, connectionTrianglesCount),
                Colors = _colors.GetSubArray(centerHexCount, connectionVerticesCount),
                BaseTriangleOffset = centerHexCount,
                TextureData = _textureData,
            };

            JobHandle generateCenterHandle = generateCenterHexMeshJob.Schedule(cells.Length, 64);
            JobHandle generateConnectionHandle = generateConnectionHexMeshJob.Schedule(cells.Length, 64, generateCenterHandle);
            JobHandle combinedHandle = JobHandle.CombineDependencies(generateCenterHandle, generateConnectionHandle);
            combinedHandle.Complete();

            _hexMesh.SetVertices(_vertices);
            _hexMesh.SetTriangles(_triangles.ToArray(), 0);
            _hexMesh.SetColors(_colors);
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

        private void OnDestroy()
        {
            DisposeBuffers(); 
            _textureData.Dispose();
        }

        [BurstCompile]
        private struct GenerateCenterHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            [ReadOnly] public TextureData TextureData;
            
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;

            private int _baseVertexIndex;

            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                
                _baseVertexIndex = index * 18;

                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
                    AddTriangle(center, v1, v2, cell.Color);
                }
            }
            
            private Vector3 Perturb(Vector3 position)
            {
                // Convertir coordenadas del mundo a coordenadas normalizadas [0, 1]
                float u = position.x * HexMetrics.NoiseScale % 1f;
                float v = position.z * HexMetrics.NoiseScale % 1f;
                if (u < 0) u += 1f;
                if (v < 0) v += 1f;
                
                Vector4 sample = TextureUtils.SampleBilinear(TextureData, u, v);
                position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
                // position.y += (sample.y * 2f - 1f) * HexMetrics.CellPerturbStrength;
                position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
                return position;
            }
            
            private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color cellColor)
            {
                Vertices[_baseVertexIndex] = Perturb(v1);
                Vertices[_baseVertexIndex + 1] = Perturb(v2);
                Vertices[_baseVertexIndex + 2] = Perturb(v3);

                Colors[_baseVertexIndex] = cellColor;
                Colors[_baseVertexIndex + 1] = cellColor;
                Colors[_baseVertexIndex + 2] = cellColor;
                    
                Triangles[_baseVertexIndex] = _baseVertexIndex;
                Triangles[_baseVertexIndex + 1] = _baseVertexIndex + 1;
                Triangles[_baseVertexIndex + 2] = _baseVertexIndex + 2;

                _baseVertexIndex += 3;
            }
        }
        
        [BurstCompile]
        private struct GenerateConnectionHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            [ReadOnly] public TextureData TextureData;
            [ReadOnly] public int BaseTriangleOffset;
            
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;

            private int _vertexIndex;
            private int _trianglesIndex;
            
            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                
                _vertexIndex = index * 6 * 4 * (5);     //TODO:Hace falta?
                _trianglesIndex = index * 6 * 6 * (5);  //TODO:Hace falta?
                
                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.SE; direction++)
                {
                    if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) continue;
                        
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
                    Vector3 bridge = HexMetrics.GetBridge(direction);
                    Vector3 v3 = v1 + bridge;
                    Vector3 v4 = v2 + bridge;
                    v3.y = v4.y = neighbor.Position.y;

                    if (HexMetrics.GetEdgeType(cell.Elevation, neighbor.Elevation) == HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(v1, v2, cell.Color, v3, v4, neighbor.Color);
                    }
                    else 
                        AddQuad(v1, v2, v3, v4, cell.Color, neighbor.Color);
   
                    if (direction <= HexDirection.E && HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
                    {
                        Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
                        v5.y = nextNeighbor.Position.y;
                        
                        if (cell.Elevation <= neighbor.Elevation) 
                        {
                            if (cell.Elevation <= nextNeighbor.Elevation) 
                            {
                                TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                            }
                            else 
                            {
                                TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                            }
                        }
                        else if (neighbor.Elevation <= nextNeighbor.Elevation) 
                        {
                            TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
                        }
                        else 
                        {
                            TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                        }
                    }
                }
            }
            
            private Vector3 Perturb(Vector3 position)
            {
                // Convertir coordenadas del mundo a coordenadas normalizadas [0, 1]
                float u = position.x * HexMetrics.NoiseScale % 1f;
                float v = position.z * HexMetrics.NoiseScale % 1f;
                if (u < 0) u += 1f;
                if (v < 0) v += 1f;
                
                Vector4 sample = TextureUtils.SampleBilinear(TextureData, u, v);
                position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
                // position.y += (sample.y * 2f - 1f) * HexMetrics.CellPerturbStrength;
                position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
                return position;
            }
            
            private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2) 
            {
                Vertices[_vertexIndex] = Perturb(v1);
                Vertices[_vertexIndex + 1] = Perturb(v2);
                Vertices[_vertexIndex + 2] = Perturb(v3);
                Vertices[_vertexIndex + 3] = Perturb(v4);
                
                Colors[_vertexIndex] = c1;
                Colors[_vertexIndex + 1] = c1;
                Colors[_vertexIndex + 2] = c2;
                Colors[_vertexIndex + 3] = c2;
                
                Triangles[_trianglesIndex] = BaseTriangleOffset + _vertexIndex;
                Triangles[_trianglesIndex + 1] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 2] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 3] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 4] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 5] = BaseTriangleOffset + _vertexIndex + 3;
                
                _vertexIndex += 4;
                _trianglesIndex += 6;
            }
            
            private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2, Color c3, Color c4) 
            {
                Vertices[_vertexIndex] = Perturb(v1);
                Vertices[_vertexIndex + 1] = Perturb(v2);
                Vertices[_vertexIndex + 2] = Perturb(v3);
                Vertices[_vertexIndex + 3] = Perturb(v4);
                
                Colors[_vertexIndex] = c1;
                Colors[_vertexIndex + 1] = c2;
                Colors[_vertexIndex + 2] = c3;
                Colors[_vertexIndex + 3] = c4;
                
                Triangles[_trianglesIndex] = BaseTriangleOffset + _vertexIndex;
                Triangles[_trianglesIndex + 1] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 2] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 3] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 4] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 5] = BaseTriangleOffset + _vertexIndex + 3;
                
                _vertexIndex += 4;
                _trianglesIndex += 6;
            }

            private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color cellColor, Color neighborColor, Color nextNeighbor)
            {
                Vertices[_vertexIndex] = Perturb(v1);
                Vertices[_vertexIndex + 1] = Perturb(v2);
                Vertices[_vertexIndex + 2] = Perturb(v3);

                Colors[_vertexIndex] = cellColor;
                Colors[_vertexIndex + 1] = neighborColor;
                Colors[_vertexIndex + 2] = nextNeighbor;
                        
                Triangles[_trianglesIndex] = BaseTriangleOffset + _vertexIndex;
                Triangles[_trianglesIndex + 1] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 2] = BaseTriangleOffset + _vertexIndex + 2;
                        
                _vertexIndex += 3;
                _trianglesIndex += 3;
            }
            
            private void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, Color beginCellColor, Vector3 endLeft, Vector3 endRight, Color endCellColor) 
            {
                Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
                Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
                Color c2 = HexMetrics.TerraceColorLerp(beginCellColor, endCellColor, 1);
                
                AddQuad(beginLeft, beginRight, v3, v4, beginCellColor, c2);
                
                for (int i = 2; i < HexMetrics.TerraceSteps; i++) 
                {
                    Vector3 v1 = v3;
                    Vector3 v2 = v4;
                    Color c1 = c2;
                    v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
                    v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
                    c2 = HexMetrics.TerraceColorLerp(beginCellColor, endCellColor, i);
                    AddQuad(v1, v2, v3, v4,c1,c2);
                }
                
                AddQuad(v3, v4, endLeft, endRight, c2, endCellColor);
            }
            
            private void TriangulateCorner(Vector3 bottom, HexCellData bottomCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell) 
            {
                HexEdgeType leftEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, leftCell.Elevation);
                HexEdgeType rightEdgeType = HexMetrics.GetEdgeType(bottomCell.Elevation, rightCell.Elevation);
                
                if (leftEdgeType == HexEdgeType.Slope) 
                {
                    if (rightEdgeType == HexEdgeType.Slope) 
                    {
                        TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                    }
                    else if (rightEdgeType == HexEdgeType.Flat) 
                    {
                        TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                    }
                    else 
                    {
                        TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
                    }
                }
                else if (rightEdgeType == HexEdgeType.Slope) 
                {
                    if (leftEdgeType == HexEdgeType.Flat) 
                    {
                        TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                    }
                    else {
                        TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                    }
                }
                else if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope) 
                {
                    if (leftCell.Elevation < rightCell.Elevation) 
                    {
                        TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                    }
                    else 
                    {
                        TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                    }
                }
                else 
                {
                    AddTriangle(bottom, left, right, bottomCell.Color, leftCell.Color, rightCell.Color);
                }
            }
            
            private void TriangulateCornerTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell)
            {
                Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
                Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
                Color c3 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
                Color c4 = HexMetrics.TerraceColorLerp(beginCell.Color, rightCell.Color, 1);

                AddTriangle(begin, v3, v4, beginCell.Color, c3, c4);
                
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
                    AddQuad(v1, v2, v3, v4, c1, c2, c3, c4);
                }

                AddQuad(v3, v4, left, right,c3, c4, leftCell.Color, rightCell.Color);
            }
            
            private void TriangulateCornerTerracesCliff(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell) 
            {
                float b = math.abs(1f / (rightCell.Elevation - beginCell.Elevation));
                Vector3 boundary = Vector3.Lerp(begin, right, b);
                Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);
                
                TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);
                
                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope) 
                {
                    TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
                }
                else 
                {
                    AddTriangle(left, right, boundary , leftCell.Color, rightCell.Color, boundaryColor);
                }
            }
            
            private void TriangulateCornerCliffTerraces(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 right, HexCellData rightCell) 
            {
                float b = math.abs(1f / (leftCell.Elevation - beginCell.Elevation));
                Vector3 boundary = Vector3.Lerp(begin, left, b);
                Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);
                
                TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);
                
                if (HexMetrics.GetEdgeType(leftCell.Elevation, rightCell.Elevation) == HexEdgeType.Slope) 
                {
                    TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
                }
                else 
                {
                    AddTriangle(left, right, boundary , leftCell.Color, rightCell.Color, boundaryColor);
                }
            }

            private void TriangulateBoundaryTriangle(Vector3 begin, HexCellData beginCell, Vector3 left, HexCellData leftCell, Vector3 boundary, Color boundaryColor)
            {
                Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
                Color c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, 1);
                
                AddTriangle(begin, v2, boundary, beginCell.Color, c2, boundaryColor);
                
                for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
                    Vector3 v1 = v2;
                    Color c1 = c2;
                    v2 = HexMetrics.TerraceLerp(begin, left, i);
                    c2 = HexMetrics.TerraceColorLerp(beginCell.Color, leftCell.Color, i);
                    AddTriangle(v1, v2, boundary, c1, c2, boundaryColor);
                }
                
                AddTriangle(v2, left, boundary, c2, leftCell.Color, boundaryColor);
            }
        }
    }
}