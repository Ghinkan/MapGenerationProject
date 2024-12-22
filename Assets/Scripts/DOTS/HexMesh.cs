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
        private NativeArray<Vector3> _normals;
        private NativeArray<Color> _colors;
        private NativeArray<Vector3> _hexCorners;
        
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
            
            int vertexCount = cells.Length * 6 * 3;
            if (_vertices.IsCreated && vertexCount > _vertices.Length) 
                DisposeBuffers();

            if (!_vertices.IsCreated)
            {
                _hexCorners = new NativeArray<Vector3>(HexMetrics.Corners.Length, Allocator.Persistent);
                for (int i = 0; i < HexMetrics.Corners.Length; i++) 
                    _hexCorners[i] = HexMetrics.Corners[i];
                
                _vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                _triangles = new NativeArray<int>(vertexCount, Allocator.Persistent);
                _normals = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                _colors = new NativeArray<Color>(vertexCount, Allocator.Persistent);
            }
            
            GenerateHexMeshJob meshJob = new GenerateHexMeshJob 
            {
                Cells = cells,
                HexCorners = _hexCorners,
                Vertices = _vertices,
                Triangles = _triangles, 
                Normals = _normals,
                Colors = _colors,
            };
            JobHandle handle = meshJob.Schedule(cells.Length, 64);
            handle.Complete();
            
            _hexMesh.SetVertices(_vertices);
            _hexMesh.SetIndexBufferParams(_triangles.Length, IndexFormat.UInt32);
            _hexMesh.SetIndexBufferData(_triangles, 0, 0, _triangles.Length);
            _hexMesh.subMeshCount = 1;
            _hexMesh.SetSubMesh(0, new SubMeshDescriptor(0, _triangles.Length));
            _hexMesh.SetNormals(_normals);
            _hexMesh.SetColors(_colors);

            _meshCollider.sharedMesh = _hexMesh;
        }
        
        private void DisposeBuffers()
        {
            if (_hexCorners.IsCreated) _hexCorners.Dispose();
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
            [ReadOnly] public NativeArray<Vector3> HexCorners;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Vertices;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> Triangles;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector3> Normals;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Color> Colors;

            public void Execute(int index)
            {
                HexCellData cell = Cells[index];
                Vector3 center = cell.Position;
                Color cellColor = cell.Color;

                Vector3 normal = Vector3.up;
                // Cada celda tiene 18 vértices, triángulos y colores (6 triángulos * 3 vértices)
                int vertexOffset = index * 18; 

                for (int i = 0; i < 6; i++)
                {
                    // Índices base para la celda actual
                    int baseIndex = vertexOffset + i * 3;

                    // Calcular los vértices para este triángulo
                    Vertices[baseIndex] = center;
                    Vertices[baseIndex + 1] = center + HexCorners[i];
                    Vertices[baseIndex + 2] = center + HexCorners[i + 1];

                    // Configurar triángulos para este triángulo
                    Triangles[baseIndex] = baseIndex;
                    Triangles[baseIndex + 1] = baseIndex + 1;
                    Triangles[baseIndex + 2] = baseIndex + 2;
                    
                    Normals[baseIndex] = normal;
                    Normals[baseIndex + 1] = normal;
                    Normals[baseIndex + 2] = normal;

                    // Configurar colores
                    Colors[baseIndex] = cellColor;
                    Colors[baseIndex + 1] = cellColor;
                    Colors[baseIndex + 2] = cellColor;
                }
            }
        }
    }
}