using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexGrid : MonoBehaviour
    {
        public static NativeArray<HexCellData> Cells;
        [SerializeField] private VoidEventChannel _onGridCreated;
        [SerializeField] private Texture2D _noiseSource;
        private TextureData _noiseData;
        
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
            Cells = new NativeArray<HexCellData>(HexMetrics.Width * HexMetrics.Height, Allocator.Persistent);
            GenerateHexGridJob generateHexGridJob = new GenerateHexGridJob
            {
                JobCells = Cells,
                Width = HexMetrics.Width,
                TextureData = _noiseData,
            };
            JobHandle generateHexGridHandle = generateHexGridJob.Schedule(Cells.Length, 64);
            generateHexGridHandle.Complete();
            
            _onGridCreated.RaiseEvent();
        }
        
        private void OnDestroy()
        {
            Cells.Dispose();
            _noiseData.Dispose();
        }
        
        [BurstCompile]
        private struct GenerateHexGridJob : IJobParallelFor
        {
            [ReadOnly] public TextureData TextureData;
            [WriteOnly] public NativeArray<HexCellData> JobCells;
            public int Width;

            public void Execute(int index)
            {
                int z = index / Width;
                int x = index % Width;

                JobCells[index] = CreateCell(x, z);
            }
        
            private HexCellData CreateCell(int x, int z) 
            {
                float3 position;
                position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
                position.y = 0f;
                position.z = z * (HexMetrics.OuterRadius * 1.5f);
                
                HexCellData cell = new HexCellData
                {
                    Coordinates = HexCoordinates.FromOffsetCoordinates(x, z),
                    Position = position,
                    Color = Color.white,
                };
                
                Vector4 sample = HexMetrics.SampleNoise(position, TextureData);
                position.y = 0 * HexMetrics.ElevationStep;
                position.y += (sample.y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                cell.SetElevation(0, position);
                
                return cell;
            } 
        } 
    }
}