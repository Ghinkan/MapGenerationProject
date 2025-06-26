using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexGrid : MonoBehaviour
    {
        private const int ChunkCount = HexMetrics.ChunkCountX * HexMetrics.ChunkCountZ;
        public static NativeArray<ChunkData> Chunks;
        public static NativeArray<HexCellData> Cells;
        
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private HexGridChunk _chunkPrefab;
        [SerializeField] private Texture2D _noiseSource;
        
        private TextureData _noiseData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            const int chunkCount = HexMetrics.ChunkCountX * HexMetrics.ChunkCountZ;
            Chunks = new NativeArray<ChunkData>(chunkCount, Allocator.Persistent);
            Cells = new NativeArray<HexCellData>(HexMetrics.Width * HexMetrics.Height, Allocator.Persistent);
        }
        
        private void Awake()
        {
            _noiseData = new TextureData(TextureUtils.ConvertTexture2DToNativeArray(_noiseSource, Allocator.Persistent), _noiseSource.width, _noiseSource.height);
            HexMetrics.NoiseData = _noiseData;
        }

        private void OnEnable()
        {
            HexMetrics.NoiseData = _noiseData;
        }
        
        private void Start() 
        {
            for (int i = 0; i < ChunkCount; i++)
            {
                Chunks[i] = new ChunkData(HexMetrics.ChunkCellSizeX * HexMetrics.ChunkCellSizeZ, Allocator.Persistent);
            }

            GenerateHexGridJob generateHexGridJob = new GenerateHexGridJob
            {
                JobChunks = Chunks,
                JobCells = Cells,
                Width = HexMetrics.Width,
                TextureData = _noiseData,
            };
            JobHandle generateHexGridHandle = generateHexGridJob.Schedule(Cells.Length, 64);
            generateHexGridHandle.Complete();
            
             CreateChunks();
             
            _onGridCreated.RaiseEvent();
        }

        private void CreateChunks()
        {
            for (int z = 0, i = 0; z < HexMetrics.ChunkCountZ; z++)
            {
                for (int x = 0; x < HexMetrics.ChunkCountX; x++)
                {
                    HexGridChunk chunk = Instantiate(_chunkPrefab, transform);
                    chunk.ChunkData = Chunks[i++];
                }
            }
        }
        
        private void OnDestroy()
        {
            for (int i = 0; i < Chunks.Length; i++)
                Chunks[i].Dispose();
            
            Chunks.Dispose();
            Cells.Dispose();
            _noiseData.Dispose();
        }
        
        [BurstCompile]
        private struct GenerateHexGridJob : IJobParallelFor
        {
            [ReadOnly] public TextureData TextureData; 
            [ReadOnly] public int Width;

            [NativeDisableContainerSafetyRestriction] public NativeArray<ChunkData> JobChunks;
            [WriteOnly] public NativeArray<HexCellData> JobCells;

            public void Execute(int index)
            {
                int z = index / Width;
                int x = index % Width;
                
                int chunkX = x / HexMetrics.ChunkCellSizeX;
                int chunkZ = z / HexMetrics.ChunkCellSizeZ;
                int chunkIndex = chunkX + chunkZ * HexMetrics.ChunkCountX;
                
                HexCellData cell = CreateCell(x, z, chunkIndex);
                JobCells[index] = cell;
                AddCellToChunk(x, z, index, chunkIndex);
            }
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private HexCellData CreateCell(int x, int z, int chunkIndex) 
            {
                float3 position;
                float xOffset = (x + z * 0.5f - z / 2);
                position.x = xOffset * (HexMetrics.InnerRadius * 2f);
                position.y = 0f;
                position.z = z * (HexMetrics.OuterRadius * 1.5f);
                
                HexCellData cell = new HexCellData
                {
                    Coordinates = HexCoordinates.FromOffsetCoordinates(x, z),
                    Position = position,
                    Color = Color.white,
                    ChunkIndex = chunkIndex,
                };
                
                Vector4 sample = HexMetrics.SampleNoise(position, TextureData);
                position.y += (sample.y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                cell.SetElevation(0, position);
                
                return cell;
            } 
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddCellToChunk(int x, int z, int index, int chunkIndex)
            {
                ChunkData chunk = JobChunks[chunkIndex];
                
                int localX = x % HexMetrics.ChunkCellSizeX;
                int localZ = z % HexMetrics.ChunkCellSizeZ;
                int localIndex = localX + localZ * HexMetrics.ChunkCellSizeX;
                
                chunk.CellsIndex[localIndex] = index;
                chunk.ChunkIndex = chunkIndex;
                
                JobChunks[chunkIndex] = chunk;
            }
        } 
    }
}