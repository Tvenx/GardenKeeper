Shader "Custom/LowBitrate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlockCount ("Block Count", Float) = 64
        _ColorDepth ("Color Depth", Float) = 8
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity", Float) = 0.1
        _NoiseTiling ("Noise Tiling", Float) = 4
        _CompressionArtifacts ("Compression Artifacts", Float) = 0.5
        _ChromaSubsampling ("Chroma Subsampling", Float) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float _BlockCount;
            float _ColorDepth;
            float _NoiseIntensity;
            float _NoiseTiling;
            float _CompressionArtifacts;
            float _ChromaSubsampling;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Функция для создания блочности (макроблоки)
            float2 createBlockiness(float2 uv)
            {
                float2 blockSize = float2(1.0 / _BlockCount, 1.0 / _BlockCount);
                float2 blockPos = floor(uv / blockSize) * blockSize;
                
                // Добавляем смещение для центра блока
                blockPos += blockSize * 0.5;
                
                return blockPos;
            }

            // Функция для квантования цвета
            float3 quantizeColor(float3 color)
            {
                float3 quantized = floor(color * _ColorDepth) / _ColorDepth;
                return quantized;
            }

            // Функция для субдискретизации цветности (Chroma Subsampling)
            float3 applyChromaSubsampling(float2 uv, float3 color)
            {
                // Преобразование в YCbCr
                float y = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
                float cb = -0.1687 * color.r - 0.3313 * color.g + 0.5 * color.b + 0.5;
                float cr = 0.5 * color.r - 0.4187 * color.g - 0.0813 * color.b + 0.5;
                
                // Уменьшаем разрешение для цветностных компонентов
                float2 chromaUV = floor(uv * _BlockCount * 0.5) / (_BlockCount * 0.5);
                cb = tex2D(_MainTex, chromaUV).b;
                cr = tex2D(_MainTex, chromaUV).r;
                
                // Обратное преобразование в RGB
                float3 result;
                result.r = y + 1.402 * (cr - 0.5);
                result.g = y - 0.344136 * (cb - 0.5) - 0.714136 * (cr - 0.5);
                result.b = y + 1.772 * (cb - 0.5);
                
                return saturate(result);
            }

            // Функция для добавления артефактов сжатия
            float3 addCompressionArtifacts(float2 uv, float3 color)
            {
                // Добавляем блочные артефакты по краям блоков
                float2 blockCoord = frac(uv * _BlockCount);
                float edgeFactor = smoothstep(0.0, 0.1, min(blockCoord.x, blockCoord.y));
                edgeFactor *= smoothstep(1.0, 0.9, max(blockCoord.x, blockCoord.y));
                
                // Артефакты проявляются как темные/светлые полосы
                float artifact = sin(blockCoord.x * 3.14159 * 4) * sin(blockCoord.y * 3.14159 * 4);
                artifact *= _CompressionArtifacts * 0.3;
                
                color += artifact * edgeFactor;
                
                return saturate(color);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Применяем блочность
                float2 blockUV = createBlockiness(i.uv);
                
                // 2. Берем исходный цвет
                fixed4 col = tex2D(_MainTex, blockUV);
                
                // 3. Применяем субдискретизацию цветности
                if (_ChromaSubsampling > 0)
                {
                    col.rgb = lerp(col.rgb, applyChromaSubsampling(i.uv, col.rgb), _ChromaSubsampling);
                }
                
                // 4. Квантуем цвет
                col.rgb = quantizeColor(col.rgb);
                
                // 5. Добавляем артефакты сжатия
                col.rgb = addCompressionArtifacts(i.uv, col.rgb);
                
                // 6. Добавляем шум для имитации цифровых помех
                float2 noiseUV = i.uv * _NoiseTiling;
                fixed4 noise = tex2D(_NoiseTex, noiseUV);
                col.rgb += noise.r * _NoiseIntensity;
                
                return col;
            }
            ENDCG
        }
    }
}
