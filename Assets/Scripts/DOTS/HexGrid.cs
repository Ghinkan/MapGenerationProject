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
        
        private void Start() 
        {
            Cells = new NativeArray<HexCellData>(HexMetrics.Width * HexMetrics.Height, Allocator.Persistent);
            GenerateHexGridJob generateHexGridJob = new GenerateHexGridJob
            {
                JobCells = Cells,
                Width = HexMetrics.Width,
            };
            JobHandle generateHexGridHandle = generateHexGridJob.Schedule(Cells.Length, 64);
            generateHexGridHandle.Complete();
            
            _onGridCreated.RaiseEvent();
        }
        
        private void OnDestroy()
        {
            Cells.Dispose();
        }
        
        [BurstCompile]
        private struct GenerateHexGridJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<HexCellData> JobCells;
            public int Width;

            public void Execute(int index)
            {
                int z = index / Width;
                int x = index % Width;

                JobCells[index] = CreateCell(x, z);
            }
        
            private static HexCellData CreateCell(int x, int z) 
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
                    Elevation = 0,
                };
                
                return cell;
            } 
        } 
    }
}