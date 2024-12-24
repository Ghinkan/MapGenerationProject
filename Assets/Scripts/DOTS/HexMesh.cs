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

            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                
                int baseVertexIndex = index * 18; // Cada celda tiene 6 triángulos = 18 vértices (6 direcciones x 3 vértices)

                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
                    
                    Vertices[baseVertexIndex] = center;
                    Vertices[baseVertexIndex + 1] = v1;
                    Vertices[baseVertexIndex + 2] = v2;

                    Color color = cell.Color;
                    Colors[baseVertexIndex] = color;
                    Colors[baseVertexIndex + 1] = color;
                    Colors[baseVertexIndex + 2] = color;
                    
                    Triangles[baseVertexIndex] = baseVertexIndex;
                    Triangles[baseVertexIndex + 1] = baseVertexIndex + 1;
                    Triangles[baseVertexIndex + 2] = baseVertexIndex + 2;

                    baseVertexIndex += 3;
                }
            }
        }
        
        [BurstCompile]
        private struct GenerateConnectionHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;
            
            public int BaseTriangleOffset;
            
            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                
                int baseVertexIndex = index * 6 * 4;
                int baseTrianglesIndex = index * 6 * 6;
                
                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.SE; direction++)
                {
                    if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) continue;
                        
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
                    Vector3 bridge = HexMetrics.GetBridge(direction);
                    Vector3 v3 = v1 + bridge;
                    Vector3 v4 = v2 + bridge;
                    
                    Vertices[baseVertexIndex] = v1;
                    Vertices[baseVertexIndex + 1] = v2;
                    Vertices[baseVertexIndex + 2] = v3;
                    Vertices[baseVertexIndex + 3] = v4;
                    
                    Color cellColor = cell.Color;
                    Color neighborColor = neighbor.Color;
                    Colors[baseVertexIndex] = cellColor;
                    Colors[baseVertexIndex + 1] = cellColor;
                    Colors[baseVertexIndex + 2] = neighborColor;
                    Colors[baseVertexIndex + 3] = neighborColor;
                    
                    Triangles[baseTrianglesIndex] = BaseTriangleOffset + baseVertexIndex;
                    Triangles[baseTrianglesIndex + 1] = BaseTriangleOffset + baseVertexIndex + 2;
                    Triangles[baseTrianglesIndex + 2] = BaseTriangleOffset + baseVertexIndex + 1;
                    Triangles[baseTrianglesIndex + 3] = BaseTriangleOffset + baseVertexIndex + 1;
                    Triangles[baseTrianglesIndex + 4] = BaseTriangleOffset + baseVertexIndex + 2;
                    Triangles[baseTrianglesIndex + 5] = BaseTriangleOffset + baseVertexIndex + 3;


                    baseVertexIndex += 4;
                    baseTrianglesIndex += 6;
                    
                    if (direction <= HexDirection.E && HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction.Next()), out HexCellData nextNeighbor))
                    {
                        Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
                    
                        Vertices[baseVertexIndex] = v2;
                        Vertices[baseVertexIndex + 1] = v4;
                        Vertices[baseVertexIndex + 2] = v5;
                    
                        Colors[baseVertexIndex] = cell.Color;
                        Colors[baseVertexIndex + 1] = neighbor.Color;
                        Colors[baseVertexIndex + 2] = nextNeighbor.Color;
                        
                        Triangles[baseTrianglesIndex] = BaseTriangleOffset + baseVertexIndex;
                        Triangles[baseTrianglesIndex + 1] = BaseTriangleOffset + baseVertexIndex + 1;
                        Triangles[baseTrianglesIndex + 2] = BaseTriangleOffset + baseVertexIndex + 2;
                        
                        baseVertexIndex += 3;
                        baseTrianglesIndex += 3;
                    }
                }
            }
        }
    }
}