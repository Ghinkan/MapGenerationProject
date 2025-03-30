using Unity.Collections;
namespace MapGenerationProject.DOTS
{
    public struct ChunkData
    {
        public NativeArray<HexCellData> Cells;

        public ChunkData(int cellCount, Allocator allocator)
        {
            Cells = new NativeArray<HexCellData>(cellCount, allocator);
        }

        public void Dispose()
        {
            if (Cells.IsCreated)
                Cells.Dispose();
        }
    }
}