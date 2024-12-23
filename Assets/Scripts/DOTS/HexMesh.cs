using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour
    {
        [SerializeField] private HexCellDataEventChannel _onGridCreated;

        private Mesh _hexMesh;
        private MeshCollider _meshCollider;

        private NativeArray<Vector3> _centerVertices;
        private NativeArray<int> _centerTriangles;
        private NativeArray<Color> _centerColors;
        
        private NativeArray<Vector3> _connectionVertices;
        private NativeArray<int> _connectionTriangles;
        private NativeArray<Color> _connectionColors;
        
        NativeArray<Vector3> _totalVertices;
        NativeArray<int> _totalTriangles;
        NativeArray<Color> _totalColors;

        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = GetComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
        }

        private void OnEnable()
        {
            _onGridCreated.GameEvent += Triangulate;
        }

        private void OnDisable()
        {
            _onGridCreated.GameEvent -= Triangulate;
        }

        private void Triangulate(NativeArray<HexCellData> cells)
        {
            _hexMesh.Clear();
            
            int centerHexCount = cells.Length * 18;
            _centerVertices = new NativeArray<Vector3>(centerHexCount, Allocator.Persistent);
            _centerTriangles = new NativeArray<int>(centerHexCount, Allocator.Persistent);
            _centerColors = new NativeArray<Color>(centerHexCount, Allocator.Persistent);
            GenerateCenterHexMeshJob generateCenterHexMeshJob = new GenerateCenterHexMeshJob 
            {
                Cells = cells,
                Vertices = _centerVertices,
                Triangles = _centerTriangles,
                Colors = _centerColors,
            };
            JobHandle generateCenterHandle = generateCenterHexMeshJob.Schedule(cells.Length, 64);
            
            int connectionVerticesCount = cells.Length * 6 * 4;
            int connectionTrianglesCount = cells.Length * 6 * 6;
            _connectionVertices = new NativeArray<Vector3>(connectionVerticesCount, Allocator.Persistent);
            _connectionTriangles = new NativeArray<int>(connectionTrianglesCount, Allocator.Persistent);
            _connectionColors = new NativeArray<Color>(connectionVerticesCount, Allocator.Persistent);
            GenerateConnectionHexMeshJob generateConnectionHexMeshJob = new GenerateConnectionHexMeshJob 
            {
                Cells = cells,
                Vertices = _connectionVertices,
                Triangles = _connectionTriangles,
                Colors = _connectionColors,
            };
            JobHandle generateConnectionHandle = generateConnectionHexMeshJob.Schedule(cells.Length, 64);
            JobHandle combinedHandle = JobHandle.CombineDependencies(generateCenterHandle, generateConnectionHandle);
            combinedHandle.Complete();
            
            // Ajustar índices de triángulos de las conexiones
            for (int i = 0; i < _connectionTriangles.Length; i++)
            {
                _connectionTriangles[i] += _centerVertices.Length;
            }
            
            _totalVertices = new NativeArray<Vector3>(_centerVertices.Length + _connectionVertices.Length, Allocator.Persistent);
            NativeArray<Vector3>.Copy(_centerVertices, 0, _totalVertices, 0, _centerVertices.Length);
            NativeArray<Vector3>.Copy(_connectionVertices, 0, _totalVertices, _centerVertices.Length, _connectionVertices.Length);
            
            _totalTriangles = new NativeArray<int>(_centerTriangles.Length + _connectionTriangles.Length, Allocator.Persistent);
            NativeArray<int>.Copy(_centerTriangles, 0, _totalTriangles, 0, _centerTriangles.Length);
            NativeArray<int>.Copy(_connectionTriangles, 0, _totalTriangles, _centerTriangles.Length, _connectionTriangles.Length);
            
            _totalColors = new NativeArray<Color>(_centerColors.Length + _connectionColors.Length, Allocator.Persistent);
            NativeArray<Color>.Copy(_centerColors, 0, _totalColors, 0, _centerColors.Length);
            NativeArray<Color>.Copy(_connectionColors, 0, _totalColors, _centerColors.Length, _connectionColors.Length);

            _hexMesh.SetVertices(_totalVertices);
            _hexMesh.SetTriangles(_totalTriangles.ToArray(), 0);
            _hexMesh.SetColors(_totalColors);
            _hexMesh.RecalculateNormals();
            
            _meshCollider.sharedMesh = _hexMesh;

            DisposeBuffers();
        }

        private void DisposeBuffers()
        {
            _centerVertices.Dispose();
            _centerTriangles.Dispose();
            _centerColors.Dispose();

            _connectionVertices.Dispose();
            _connectionTriangles.Dispose();
            _connectionColors.Dispose();
            
            _totalVertices.Dispose();
            _totalTriangles.Dispose();
            _totalColors.Dispose();
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
            
            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                
                int baseVertexIndex = index * 6 * 4;
                int baseTrianglesIndex = index * 6 * 6;
                
                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.SE; direction++)
                {
                    if (!HexMetrics.TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) continue;
                    // if (!TryGetCell(Cells, cell.Coordinates.Step(direction), out HexCellData neighbor)) continue;
                        
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
                    
                    Triangles[baseTrianglesIndex] = baseVertexIndex;
                    Triangles[baseTrianglesIndex + 1] = baseVertexIndex + 2;
                    Triangles[baseTrianglesIndex + 2] = baseVertexIndex + 1;
                    Triangles[baseTrianglesIndex + 3] = baseVertexIndex + 1;
                    Triangles[baseTrianglesIndex + 4] = baseVertexIndex + 2;
                    Triangles[baseTrianglesIndex + 5] = baseVertexIndex + 3;

                    baseVertexIndex += 4;
                    baseTrianglesIndex += 6;
                    
                    if (direction <= HexDirection.E && cell.TryGetNeighbor(Cells, direction.Next(), out HexCellData nextNeighbor))
                    {
                        Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
                    
                        Vertices[baseVertexIndex] = v2;
                        Vertices[baseVertexIndex + 1] = v4;
                        Vertices[baseVertexIndex + 2] = v5;
                    
                        Colors[baseVertexIndex] = cell.Color;
                        Colors[baseVertexIndex + 1] = neighbor.Color;
                        Colors[baseVertexIndex + 2] = nextNeighbor.Color;
                    
                        Triangles[baseTrianglesIndex] = baseVertexIndex;
                        Triangles[baseTrianglesIndex + 1] = baseVertexIndex + 1;
                        Triangles[baseTrianglesIndex + 2] = baseVertexIndex + 2;
                    
                        baseVertexIndex += 3;
                        baseTrianglesIndex += 3;
                    }
                }
            }
        }
    }
}