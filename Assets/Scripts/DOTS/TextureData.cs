using System;
using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct TextureData: IDisposable
    {
        public NativeArray<Color32> Colors;
        public readonly int Width;
        public readonly int Height;
        
        public TextureData(NativeArray<Color32> colors, int width, int height)
        {
            Colors = colors;
            Width = width;
            Height = height;
        }

        public void Dispose()
        {
            if (Colors.IsCreated)
                Colors.Dispose();
        }
    }
}