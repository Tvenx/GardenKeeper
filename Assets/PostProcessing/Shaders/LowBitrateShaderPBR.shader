Shader "Custom/LowBitratePBR"
{
    Properties
    {
        // Основные свойства
        [Header(Main Settings)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // Эффекты низкого битрейта
        [Header(Low Bitrate Effects)]
        _BlockCount ("Block Count", Float) = 64
        _ColorDepth ("Color Depth", Float) = 8
        _CompressionArtifacts ("Compression Artifacts", Float) = 0.5
        _ChromaSubsampling ("Chroma Subsampling", Float) = 0.5
        _Dithering ("Dithering", Float) = 0.3
        
        // Шум и помехи
       // [Header(Noise & Interference)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity", Float) = 0.1
        _NoiseTiling ("Noise Tiling", Float) = 4
        _ScanlineSpeed ("Scanline Speed", Float) = 1.0
        _ScanlineIntensity ("Scanline Intensity", Float) = 0.1
        
        // PBR параметры
        [Header(PBR Settings)]
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _AO ("Ambient Occlusion", 2D) = "white" {}
        
        // Эмиссия
        [Header(Emission)]
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 0
    }
    
    SubShader
    {
        Tags { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry"
        }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
            float2 uv_NormalMap;
            float2 uv_AO;
            float2 uv_EmissionMap;
            float3 worldPos;
            float3 viewDir;
        };
        
        // Основные текстуры
        sampler2D _MainTex;
        sampler2D _NoiseTex;
        sampler2D _NormalMap;
        sampler2D _AO;
        sampler2D _EmissionMap;
        
        // Основные параметры
        float4 _Color;
        float _BlockCount;
        float _ColorDepth;
        float _NoiseIntensity;
        float _NoiseTiling;
        float _CompressionArtifacts;
        float _ChromaSubsampling;
        float _Dithering;
        float _ScanlineSpeed;
        float _ScanlineIntensity;
        float _Metallic;
        float _Smoothness;
        float _NormalStrength;
        float _EmissionIntensity;
        
        // Цвета
        float4 _EmissionColor;
        
        // Функция для создания блочности (макроблоки)
        float2 createBlockiness(float2 uv, float effectMask)
        {
            float blockCount = lerp(4096.0, _BlockCount, effectMask);
            float2 blockSize = float2(1.0 / blockCount, 1.0 / blockCount);
            float2 blockPos = floor(uv / blockSize) * blockSize;
            blockPos += blockSize * 0.5;
            return blockPos;
        }
        
        // Функция для квантования цвета
        float3 quantizeColor(float3 color, float depth, float mask)
        {
            float effectiveDepth = lerp(256.0, depth, mask);
            float3 quantized = floor(color * effectiveDepth) / effectiveDepth;
            return quantized;
        }
        
        // Функция Bayer матрицы для дизеринга
        float bayerDither(float2 uv)
        {
            int x = int(fmod(uv.x * _BlockCount, 4.0));
            int y = int(fmod(uv.y * _BlockCount, 4.0));
            
            // Bayer 4x4 матрица
            const float bayerMatrix[16] = {
                0.0/16.0, 8.0/16.0, 2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0, 6.0/16.0,
                3.0/16.0, 11.0/16.0, 1.0/16.0, 9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0, 5.0/16.0
            };
            
            return bayerMatrix[x + y * 4];
        }
        
        // Функция для субдискретизации цветности
        float3 applyChromaSubsampling(float2 uv, float3 color, float amount, float mask)
        {
            if (amount <= 0.0) return color;
            
            float effectiveAmount = amount * mask;
            
            // Преобразование в YCbCr
            float y = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
            float cb = -0.1687 * color.r - 0.3313 * color.g + 0.5 * color.b + 0.5;
            float cr = 0.5 * color.r - 0.4187 * color.g - 0.0813 * color.b + 0.5;
            
            // Уменьшаем разрешение для цветностных компонентов
            float subsampleFactor = lerp(1.0, 0.5, effectiveAmount);
            float2 chromaUV = floor(uv * _BlockCount * subsampleFactor) / (_BlockCount * subsampleFactor);
            
            // Читаем усредненные значения цветности
            float4 avgColor = tex2D(_MainTex, chromaUV);
            float avgCb = -0.1687 * avgColor.r - 0.3313 * avgColor.g + 0.5 * avgColor.b + 0.5;
            float avgCr = 0.5 * avgColor.r - 0.4187 * avgColor.g - 0.0813 * avgColor.b + 0.5;
            
            // Смешиваем с исходной цветностью
            cb = lerp(cb, avgCb, effectiveAmount);
            cr = lerp(cr, avgCr, effectiveAmount);
            
            // Обратное преобразование в RGB
            float3 result;
            result.r = y + 1.402 * (cr - 0.5);
            result.g = y - 0.344136 * (cb - 0.5) - 0.714136 * (cr - 0.5);
            result.b = y + 1.772 * (cb - 0.5);
            
            return saturate(result);
        }
        
        // Функция для добавления артефактов сжатия
        float3 addCompressionArtifacts(float2 uv, float3 color, float amount, float mask)
        {
            float effectiveAmount = amount * mask;
            if (effectiveAmount <= 0.0) return color;
            
            // Добавляем блочные артефакты по краям блоков
            float2 blockCoord = frac(uv * _BlockCount);
            float edgeFactor = smoothstep(0.0, 0.1, min(blockCoord.x, blockCoord.y));
            edgeFactor *= smoothstep(1.0, 0.9, max(blockCoord.x, blockCoord.y));
            
            // Артефакты сжатия (полосы)
            float artifact = sin(blockCoord.x * 3.14159 * 4.0) * sin(blockCoord.y * 3.14159 * 4.0);
            artifact *= effectiveAmount * 0.3;
            
            color += artifact * edgeFactor;
            
            return saturate(color);
        }
        
        // Функция для сканирующих линий
        float scanlineEffect(float2 uv, float time)
        {
            float scanline = sin(uv.y * 300.0 + time * _ScanlineSpeed) * 0.5 + 0.5;
            return 1.0 - scanline * _ScanlineIntensity;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Маска эффектов (упрощенная версия)
            float effectMask = 1.0;
            
            // 1. Применяем блочность
            float2 blockUV = createBlockiness(IN.uv_MainTex, effectMask);
            
            // 2. Берем исходный цвет
            float4 col = tex2D(_MainTex, blockUV) * _Color;
            
            // 3. Применяем субдискретизацию цветности
            if (_ChromaSubsampling > 0.0)
            {
                col.rgb = applyChromaSubsampling(IN.uv_MainTex, col.rgb, _ChromaSubsampling, effectMask);
            }
            
            // 4. Квантуем цвет
            col.rgb = quantizeColor(col.rgb, _ColorDepth, effectMask);
            
            // 5. Добавляем дизеринг
            if (_Dithering > 0.0)
            {
                float dither = bayerDither(IN.uv_MainTex) * _Dithering * effectMask;
                col.rgb += dither / _ColorDepth;
            }
            
            // 6. Добавляем артефакты сжатия
            col.rgb = addCompressionArtifacts(IN.uv_MainTex, col.rgb, _CompressionArtifacts, effectMask);
            
            // 7. Добавляем сканирующие линии
            if (_ScanlineIntensity > 0.0)
            {
                float scanline = scanlineEffect(IN.uv_MainTex, _Time.y);
                col.rgb *= scanline;
            }
            
            // 8. Добавляем шум
            float2 noiseUV = IN.uv_MainTex * _NoiseTiling;
            float4 noise = tex2D(_NoiseTex, noiseUV + float2(_Time.y * 0.1, 0.0));
            col.rgb += (noise.r * 2.0 - 1.0) * _NoiseIntensity * effectMask;
            
            // 9. Применяем gamma коррекцию
            col.rgb = pow(col.rgb, 2.2);
            
            // Устанавливаем альбедо
            o.Albedo = col.rgb;
            o.Alpha = col.a;
            
            // PBR свойства
            o.Metallic = _Metallic;
            
            // Шероховатость с эффектом низкого битрейта
            float roughness = 1.0 - _Smoothness;
            float quantizedRoughness = floor(roughness * 4.0) / 4.0;
            o.Smoothness = 1.0 - lerp(roughness, quantizedRoughness, effectMask * 0.5);
            
            // Нормали
            if (_NormalStrength > 0.0)
            {
                float4 normalTex = tex2D(_NormalMap, IN.uv_NormalMap);
                float3 normal = UnpackNormal(normalTex);
                normal.xy *= _NormalStrength;
                o.Normal = normalize(normal);
            }
            
            // Ambient Occlusion
            float ao = tex2D(_AO, IN.uv_AO).r;
            float aoQuantized = floor(ao * 2.0) / 2.0;
            o.Occlusion = lerp(ao, aoQuantized, effectMask * 0.3);
            
            // Эмиссия
            float4 emissionCol = tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionColor;
            emissionCol.rgb = floor(emissionCol.rgb * 4.0) / 4.0;
            o.Emission = emissionCol.rgb * _EmissionIntensity;
        }
        ENDCG
    }
    
    FallBack "Standard"
}