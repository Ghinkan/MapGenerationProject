using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexGrid : MonoBehaviour, IHexSelectable
    {
        [SerializeField] private int _width = 6;
        [SerializeField] private int _height = 6;

        private static int Width { get; set; }
        private static int Height { get; set; }
        
        
        private static NativeArray<HexCellData> _cells;
        
        [SerializeField] private HexCellDataEventChannel _onGridCreated;
        [SerializeField] private HexCellDataEventChannel _onMeshCreated;
        
        private void Awake()
        {
            Width = _width;
            Height = _height;
        }
        
        private void Start() 
        {
            _cells = new NativeArray<HexCellData>(Width * Height, Allocator.Persistent);
            GenerateHexGridJob generateHexGridJob = new GenerateHexGridJob
            {
                Cells = _cells,
                Width = Width,
            };
            JobHandle generateHexGridHandle = generateHexGridJob.Schedule(_cells.Length, 64);
            generateHexGridHandle.Complete();
            
            _onGridCreated.RaiseEvent(_cells);
            _onMeshCreated.RaiseEvent(_cells);
        }

        public void ColorCell(HexCoordinates coordinates, Color color)
        {
            int index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
            HexCellData cell = _cells[index];
            cell.Color = color;
            _cells[index] = cell;
            
            _onGridCreated.RaiseEvent(_cells);
        }
        
        public static HexCellData GetCell(HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= Height || x < 0 || x >= Width)
                return default(HexCellData);

            return _cells[x + z * Width];
        }
        
        // public static bool TryGetCell(HexCoordinates coordinates, out HexCellData cell)
        // {
        //     int z = coordinates.Z;
        //     int x = coordinates.X + z / 2;
        //     if (z < 0 || z >= Height || x < 0 || x >= Width)
        //     {
        //         cell = default(HexCellData);
        //         return false;
        //     }
        //     
        //     cell = _cells[x + z * Width];
        //     return true;
        // }
        
        public static bool TryGetCell(HexCoordinates coordinates, NativeArray<HexCellData> cells, out HexCellData cell)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= 10 || x < 0 || x >= 10)
            {
                cell = default(HexCellData);
                return false;
            }

            cell = cells[x + z * 10];
            return true;
        }
        
        private void OnDestroy()
        {
            _cells.Dispose();
        }
        
        [BurstCompile]
        private struct GenerateHexGridJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<HexCellData> Cells;
            public int Width;

            public void Execute(int index)
            {
                int z = index / Width;
                int x = index % Width;

                Cells[index] = CreateCell(x, z);
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
                };
                
                return cell;
            } 
        } 
    }
}