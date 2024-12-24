using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.Serialization;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private VoidEventChannel _refreshMesh;

        private Mesh _hexMesh;
        private MeshCollider _meshCollider;

        private NativeArray<Vector3> _vertices;
        private NativeArray<int> _triangles;
        private NativeArray<Color> _colors;

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
            int centerHexCount = cells.Length * 18;
            int connectionVerticesCount = cells.Length * 6 * 4;
            int connectionTrianglesCount = cells.Length * 6 * 6;
            
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
            };

            GenerateConnectionHexMeshJob generateConnectionHexMeshJob = new GenerateConnectionHexMeshJob
            {
                Cells = cells,
                Vertices = _vertices.GetSubArray(centerHexCount, connectionVerticesCount),
                Triangles = _triangles.GetSubArray(centerHexCount, connectionTrianglesCount),
                Colors = _colors.GetSubArray(centerHexCount, connectionVerticesCount),
                BaseTriangleOffset = centerHexCount,
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

        [BurstCompile]
        private struct GenerateCenterHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;

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
            
            private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color cellColor)
            {
                Vertices[_baseVertexIndex] = v1;
                Vertices[_baseVertexIndex + 1] = v2;
                Vertices[_baseVertexIndex + 2] = v3;

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
                
                _vertexIndex = index * 6 * 4;
                _trianglesIndex = index * 6 * 6;
                
                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.SE; direction++)
                {
                    if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) continue;
                        
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
                    Vector3 bridge = HexMetrics.GetBridge(direction);
                    Vector3 v3 = v1 + bridge;
                    Vector3 v4 = v2 + bridge;
                    
                    AddQuad(v1, v2, v3, v4, cell.Color, neighbor.Color);
   
                    if (direction <= HexDirection.E && HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
                    {
                        Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
                    
                        AddTriangle(v2, v4, v5, cell.Color, neighbor.Color, nextNeighbor.Color);
                    }
                }
            }

            private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color cellColor, Color neighborColor, Color nextNeighbor)
            {
                Vertices[_vertexIndex] = v1;
                Vertices[_vertexIndex + 1] = v2;
                Vertices[_vertexIndex + 2] = v3;

                Colors[_vertexIndex] = cellColor;
                Colors[_vertexIndex + 1] = neighborColor;
                Colors[_vertexIndex + 2] = nextNeighbor;
                        
                Triangles[_trianglesIndex] = BaseTriangleOffset + _vertexIndex;
                Triangles[_trianglesIndex + 1] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 2] = BaseTriangleOffset + _vertexIndex + 2;
                        
                _vertexIndex += 3;
                _trianglesIndex += 3;
            }
            
            private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color cellColor, Color neighborColor) 
            {
                Vertices[_vertexIndex] = v1;
                Vertices[_vertexIndex + 1] = v2;
                Vertices[_vertexIndex + 2] = v3;
                Vertices[_vertexIndex + 3] = v4;
                
                Colors[_vertexIndex] = cellColor;
                Colors[_vertexIndex + 1] = cellColor;
                Colors[_vertexIndex + 2] = neighborColor;
                Colors[_vertexIndex + 3] = neighborColor;
                    
                Triangles[_trianglesIndex] = BaseTriangleOffset + _vertexIndex;
                Triangles[_trianglesIndex + 1] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 2] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 3] = BaseTriangleOffset + _vertexIndex + 1;
                Triangles[_trianglesIndex + 4] = BaseTriangleOffset + _vertexIndex + 2;
                Triangles[_trianglesIndex + 5] = BaseTriangleOffset + _vertexIndex + 3;
                
                _vertexIndex += 4;
                _trianglesIndex += 6;
            }
        }
    }
}