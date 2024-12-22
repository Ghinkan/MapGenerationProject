Shader"Custom/HexCellShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Entrada del vértice
            struct Attributes
            {
                float4 positionOS : POSITION; // Posición del vértice
                float4 color : COLOR;        // Color del vértice
            };

            // Salida del vértice al fragmento
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Posición en espacio de clip
                float4 color : COLOR;            // Color que pasa al fragmento
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz); // Convierte float4 a float3
                output.color = input.color; // Pasar el color del vértice al fragmento
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return input.color; // Pintar el fragmento con el color del vértice
            }
            ENDHLSL
        }
    }
}
