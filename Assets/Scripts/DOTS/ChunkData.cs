using System;
using Unity.Collections;
namespace MapGenerationProject.DOTS
{
    public struct ChunkData: IDisposable
    {
        public NativeArray<int> CellsIndex;
        public int ChunkIndex;

        public ChunkData(int cellCount, Allocator allocator)
        {
            CellsIndex = new NativeArray<int>(cellCount, allocator);
            ChunkIndex = 0;
        }

        public void Dispose()
        {
            if (CellsIndex.IsCreated)
                CellsIndex.Dispose();
        }
    }
}