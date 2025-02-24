using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public static class TextureUtils
    {
        public static NativeArray<Color32> ConvertTexture2DToNativeArray(Texture2D texture, Allocator allocator)
        {
            return texture.GetRawTextureData<Color32>();
        }
        
        private static float4 GetPixelColor(TextureData texture, int x, int y)
        {
            int index = x + y * texture.Width;
            Color color = texture.Colors[index];
            return new float4(color.r, color.g, color.b, color.a);
        }
        
        public static Vector4 SampleBilinear(TextureData texture, float u, float v)
        {
            // Asegurarse de que las coordenadas estén dentro del rango [0, 1]
            u = math.clamp(u, 0f, 1f);
            v = math.clamp(v, 0f, 1f);

            // Escalar las coordenadas normalizadas a índices de píxel
            float x = u * (texture.Width - 1);
            float y = v * (texture.Height - 1);

            // Obtener las coordenadas enteras más cercanas
            int x0 = (int)math.floor(x);
            int y0 = (int)math.floor(y);
            int x1 = math.clamp(x0 + 1, 0, texture.Width - 1);
            int y1 = math.clamp(y0 + 1, 0, texture.Height - 1);

            // Calcular las fracciones decimales
            float tx = x - x0;
            float ty = y - y0;

            // Obtener los colores de los cuatro píxeles vecinos
            float4 c00 = GetPixelColor(texture, x0, y0);
            float4 c10 = GetPixelColor(texture, x1, y0);
            float4 c01 = GetPixelColor(texture, x0, y1);
            float4 c11 = GetPixelColor(texture, x1, y1);

            // Interpolación bilineal
            float4 c0 = math.lerp(c00, c10, tx);
            float4 c1 = math.lerp(c01, c11, tx);
            return math.lerp(c0, c1, ty);
        }
    }
}