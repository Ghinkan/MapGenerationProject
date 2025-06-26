using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace MapGenerationProject.DOTS
{
    public static class UnsafeHelper 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Reserve<T>(ref NativeList<T>.ParallelWriter list, int count) where T : unmanaged 
        {
            UnsafeList<T>* listData = list.ListData;
            return Interlocked.Add(ref listData->m_length, count) - count;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int AddWithIndex<T>(ref NativeList<T>.ParallelWriter list, in T element) where T : unmanaged 
        {
            UnsafeList<T>* listData = list.ListData;
            int idx = Interlocked.Increment(ref listData->m_length) - 1;
            UnsafeUtility.WriteArrayElement(listData->Ptr, idx, element);
        
            return idx;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Add<T>(ref NativeList<T>.ParallelWriter list, in T element) where T : unmanaged 
        {
            UnsafeList<T>* listData = list.ListData;
            int idx = Interlocked.Increment(ref listData->m_length) - 1;
            UnsafeUtility.WriteArrayElement(listData->Ptr, idx, element);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetRef<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
            }
        }
        
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static int WriteWithIndex<T>(ref NativeStream.Writer writer, ref int vertexCounter, in T element) where T : unmanaged
        // {
        //     int idx = Interlocked.Increment(ref vertexCounter) - 1;
        //     writer.Write(element);
        //     return idx;
        // }
    }
}