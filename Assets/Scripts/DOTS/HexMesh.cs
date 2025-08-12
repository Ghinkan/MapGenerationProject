using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
namespace MapGenerationProject.DOTS
{
    public class HexMesh : MonoBehaviour, IDisposable
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshCollider _meshCollider;

        private Mesh _hexMesh;
        private HexMeshGridData _meshGridData;
        private NativeList<Vector3> _vertices;
        private NativeList<int> _triangles;
        private NativeList<Color> _colors;
        
        private JobHandle _currentJobHandle;
        private NativeArray<HexCellData> _currentChunkCells;
        private bool _isJobRunning;
        
        private void Awake()
        {
            _hexMesh = new Mesh { name = "Hex Mesh" };
            _hexMesh.MarkDynamic();
            _meshFilter.mesh = _hexMesh;

            InitializeMeshData();
        }

        private void InitializeMeshData()
        {
            int cellsPerChunk = HexMetrics.ChunkCellSizeX * HexMetrics.ChunkCellSizeZ;
            int estimatedVerticesPerChunk = cellsPerChunk * HexMetrics.EstimatedVerticesPerCell;
            int estimatedTrianglesPerChunk = cellsPerChunk * HexMetrics.EstimatedTrianglesPerCell;
            
            _vertices = new NativeList<Vector3>(estimatedVerticesPerChunk * 3, Allocator.Persistent);
            _triangles = new NativeList<int>(estimatedTrianglesPerChunk * 3, Allocator.Persistent);
            _colors = new NativeList<Color>(estimatedVerticesPerChunk * 3, Allocator.Persistent);
            _meshGridData = new HexMeshGridData(_vertices, _triangles, _colors, HexMetrics.NoiseData);
            _currentChunkCells = new NativeArray<HexCellData>(HexGrid.Cells.Length, Allocator.Persistent);
        }

        private void ClearMeshData()
        {
            if (_vertices.IsCreated) _vertices.Clear();
            if (_triangles.IsCreated) _triangles.Clear();
            if (_colors.IsCreated) _colors.Clear();
        }

        public void TriangulateChunk(ChunkData chunkData)
        {
            if (_isJobRunning)
            {
                _currentJobHandle.Complete();
                _isJobRunning = false;
            }
            
            ClearMeshData();
            
            _currentChunkCells.CopyFrom(HexGrid.Cells);
            
            GenerateCenterHexMeshJob generateCenterHexMeshJob = new GenerateCenterHexMeshJob 
            {
                Cells = _currentChunkCells,
                ChunkData = chunkData,
                MeshGridData = _meshGridData,
            };
            
            _currentJobHandle = generateCenterHexMeshJob.Schedule();
            _isJobRunning = true;
        }
        
        private void TryCompleteJob()
        {
            if (!_isJobRunning) return;

            if (_currentJobHandle.IsCompleted)
            {
                _currentJobHandle.Complete();
                _isJobRunning = false;
                
                UpdateMeshData();
            }
        }
        
        private void Update()
        {
            TryCompleteJob();
        }

        private void UpdateMeshData()
        {
            int vertexCount = _vertices.Length;
            int indexCount = _triangles.Length;

            // Crear contenedor de malla
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            // Establecer los atributos de vértice (posición y color). Usamos 2 streams separados para evitar problemas de stride
            NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
            vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0);
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, stream: 1);

            // Configurar buffers de vértices
            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);

            // Copiar datos a los streams correctos
            meshData.GetVertexData<Vector3>(0).CopyFrom(_vertices.AsArray()); // stream 0: posición
            meshData.GetVertexData<Color>(1).CopyFrom(_colors.AsArray()); // stream 1: color

            // Configurar índice
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            meshData.GetIndexData<int>().CopyFrom(_triangles.AsArray());

            // Configurar submesh
            meshData.subMeshCount = 1;
            SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor(0, indexCount) 
            {
                topology = MeshTopology.Triangles,
            };
            meshData.SetSubMesh(0, subMeshDescriptor);

            // Aplicar y reemplazar el mesh
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _hexMesh, MeshUpdateFlags.DontRecalculateBounds);
            _hexMesh.RecalculateBounds();
            _hexMesh.RecalculateNormals();
            
            _meshCollider.sharedMesh = _hexMesh;
        }
        
        [BurstCompile]
        private struct GenerateCenterHexMeshJob : IJob
        {
            [ReadOnly] public NativeArray<HexCellData> Cells;
            [ReadOnly] public ChunkData ChunkData;
            public HexMeshGridData MeshGridData;

            public void Execute()
            {
                for (int i = 0; i < ChunkData.CellsIndex.Length; i++)
                {
                    int index = ChunkData.CellsIndex[i];
                    HexCellData cell = Cells[index];
                    Vector3 center = cell.Position;

                    for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                    {
                        EdgeVertices e = new EdgeVertices(
                            center + HexMetrics.GetFirstSolidCorner(direction),
                            center + HexMetrics.GetSecondSolidCorner(direction));

                        HexMeshTriangulationUtils.TriangulateEdgeFan(center, e, cell.Color, ref MeshGridData);

                        if (direction <= HexDirection.SE)
                        {
                            HexEdgeUtils.CreateConnection(cell, direction, e, Cells, ref MeshGridData);
                        }
                    }
                }
            }
        }
        
        public void Dispose()
        {
            if (_vertices.IsCreated) _vertices.Dispose();
            if (_triangles.IsCreated) _triangles.Dispose();
            if (_colors.IsCreated) _colors.Dispose();
            if (_currentChunkCells.IsCreated) _currentChunkCells.Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}