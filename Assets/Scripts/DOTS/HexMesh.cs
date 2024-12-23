using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventChannels;
using UnityEngine.Rendering;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour
    {
        [SerializeField] private HexCellDataEventChannel _onGridCreated;

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
        }

        private void OnDisable()
        {
            _onGridCreated.GameEvent -= Triangulate;
        }

        private void Triangulate(NativeArray<HexCellData> cells)
        {
            _hexMesh.Clear();

            // verticesPerNeighborConnection = maxConnections * verticesPerQuad
            int verticesPerNeighborConnection = 6 * 4;
            // trianglesPerNeighborConnection = maxConnections * trianglesPerQuad;
            int trianglesPerNeighborConnection = 6 * 6;

            // vertexCount = cells * (verticesPerCell + verticesPerNeighborConnection);
            int vertexCount = cells.Length * (18 + verticesPerNeighborConnection);
            // triangleCount = cells * (trianglesPerCell + trianglesPerNeighborConnection);
            int triangleCount = cells.Length * (18 + trianglesPerNeighborConnection);

            if (_vertices.IsCreated && vertexCount > _vertices.Length)
                DisposeBuffers();

            if (!_vertices.IsCreated)
            {
                _vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                _triangles = new NativeArray<int>(triangleCount, Allocator.Persistent);
                _colors = new NativeArray<Color>(vertexCount, Allocator.Persistent);
            }

            GenerateHexMeshJob meshJob = new GenerateHexMeshJob {
                Cells = cells,
                Vertices = _vertices,
                Triangles = _triangles,
                Colors = _colors,
            };
            JobHandle handle = meshJob.Schedule(cells.Length, 64);
            handle.Complete();

            _hexMesh.SetVertices(_vertices);
            _hexMesh.SetIndexBufferParams(_triangles.Length, IndexFormat.UInt32);
            _hexMesh.SetIndexBufferData(_triangles, 0, 0, _triangles.Length);
            _hexMesh.subMeshCount = 1;
            _hexMesh.SetSubMesh(0, new SubMeshDescriptor(0, _triangles.Length));
            // _hexMesh.SetNormals(_normals);
            _hexMesh.RecalculateNormals();
            _hexMesh.SetColors(_colors);

            _meshCollider.sharedMesh = _hexMesh;
        }

        private void DisposeBuffers()
        {
            if (_vertices.IsCreated) _vertices.Dispose();
            if (_triangles.IsCreated) _triangles.Dispose();
            if (_colors.IsCreated) _colors.Dispose();
        }

        private void OnDestroy()
        {
            DisposeBuffers();
        }

        [BurstCompile]
        private struct GenerateHexMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;

            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;

                // Índice base para esta celda
                int baseVertexIndex = index * 18; // Cada celda tiene 6 triángulos = 18 vértices (6 direcciones x 3 vértices)

                for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
                    Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

                    // Añadir los vértices del triángulo principal
                    Vertices[baseVertexIndex] = center;
                    Vertices[baseVertexIndex + 1] = v1;
                    Vertices[baseVertexIndex + 2] = v2;

                    // Añadir colores
                    Color color = cell.Color;
                    Colors[baseVertexIndex] = color;
                    Colors[baseVertexIndex + 1] = color;
                    Colors[baseVertexIndex + 2] = color;

                    // Añadir los triángulos
                    Triangles[baseVertexIndex] = baseVertexIndex;
                    Triangles[baseVertexIndex + 1] = baseVertexIndex + 1;
                    Triangles[baseVertexIndex + 2] = baseVertexIndex + 2;

                    baseVertexIndex += 3; // Avanza al siguiente conjunto de vértices

                    // Conexiones con los vecinos
                    if (direction <= HexDirection.SE)
                    {
                        baseVertexIndex = TriangulateConnection(direction, cell, v1, v2, baseVertexIndex);
                    }
                }
            }
            
            private int TriangulateConnection(HexDirection direction, HexCellData cell, Vector3 v1, Vector3 v2, int baseVertexIndex)
            {
                if (!cell.TryGetNeighbor(Cells, direction, out HexCellData neighbor)) return baseVertexIndex;
                
                Vector3 bridge = HexMetrics.GetBridge(direction);
                Vector3 v3 = v1 + bridge;
                Vector3 v4 = v2 + bridge;

                // Agregar los vértices para el quad
                Vertices[baseVertexIndex] = v1;
                Vertices[baseVertexIndex + 1] = v2;
                Vertices[baseVertexIndex + 2] = v3;
                Vertices[baseVertexIndex + 3] = v4;

                // Agregar colores
                Color cellColor = cell.Color;
                Color neighborColor = neighbor.Color;
                Colors[baseVertexIndex] = cellColor;
                Colors[baseVertexIndex + 1] = cellColor;
                Colors[baseVertexIndex + 2] = neighborColor;
                Colors[baseVertexIndex + 3] = neighborColor;

                // Agregar los triángulos
                Triangles[baseVertexIndex] = baseVertexIndex;
                Triangles[baseVertexIndex + 1] = baseVertexIndex + 2;
                Triangles[baseVertexIndex + 2] = baseVertexIndex + 1;
                Triangles[baseVertexIndex + 3] = baseVertexIndex + 1;
                Triangles[baseVertexIndex + 4] = baseVertexIndex + 2;
                Triangles[baseVertexIndex + 5] = baseVertexIndex + 3;

                baseVertexIndex += 4; // Mueve el índice para el próximo quad

                // Agregar el triángulo entre vecinos, si aplica
                if (direction <= HexDirection.E && cell.TryGetNeighbor(Cells, direction.Next(), out HexCellData nextNeighbor))
                {
                    Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());

                    Vertices[baseVertexIndex] = v2;
                    Vertices[baseVertexIndex + 1] = v4;
                    Vertices[baseVertexIndex + 2] = v5;

                    Colors[baseVertexIndex] = cell.Color;
                    Colors[baseVertexIndex + 1] = neighbor.Color;
                    Colors[baseVertexIndex + 2] = nextNeighbor.Color;

                    Triangles[baseVertexIndex] = baseVertexIndex;
                    Triangles[baseVertexIndex + 1] = baseVertexIndex + 1;
                    Triangles[baseVertexIndex + 2] = baseVertexIndex + 2;

                    baseVertexIndex += 3; // Mueve el índice para el próximo triángulo
                }

                return baseVertexIndex;
            }
        }

    // [BurstCompile]
        // private struct GenerateHexMeshJob : IJobParallelFor
        // {
        //     [ReadOnly] public NativeArray<HexCellData> Cells;
        //     [ReadOnly] public NativeArray<Vector3> HexCorners;
        //
        //     [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
        //     [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
        //     [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Normals;
        //     [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;
        //
        //     public void Execute(int index)
        //     {
        //         HexCellData cell = Cells[index];
        //         Vector3 center = cell.Position;
        //         Color cellColor = cell.Color;
        //
        //         Vector3 normal = Vector3.up;
        //         // Cada celda tiene 18 vértices, triángulos y colores (6 triángulos * 3 vértices)
        //         int vertexOffset = index * 18; 
        //
        //         for (int i = 0; i < 6; i++)
        //         {
        //             // Índices base para la celda actual
        //             int baseIndex = vertexOffset + i * 3;
        //
        //             // Calcular los vértices para este triángulo
        //             Vertices[baseIndex] = center;
        //             Vertices[baseIndex + 1] = center + HexCorners[i];
        //             Vertices[baseIndex + 2] = center + HexCorners[i + 1];
        //
        //             // Configurar triángulos para este triángulo
        //             Triangles[baseIndex] = baseIndex;
        //             Triangles[baseIndex + 1] = baseIndex + 1;
        //             Triangles[baseIndex + 2] = baseIndex + 2;
        //             
        //             Normals[baseIndex] = normal;
        //             Normals[baseIndex + 1] = normal;
        //             Normals[baseIndex + 2] = normal;
        //
        //             // Configurar colores
        //             Colors[baseIndex] = cellColor;
        //             Colors[baseIndex + 1] = cellColor;
        //             Colors[baseIndex + 2] = cellColor;
        //         }
        //     }
        // }
    }
}