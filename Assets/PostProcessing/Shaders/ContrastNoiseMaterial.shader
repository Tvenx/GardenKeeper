Shader "Custom/ContrastNoiseMaterial"
{
    Properties
    {
        // Основные свойства
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // Настройки контраста и резкости
        _Contrast ("Contrast", Range(0, 30)) = 1
        _Brightness ("Brightness", Range(-1, 10)) = 0
        _Saturation ("Saturation", Range(0, 20)) = 1
        _Sharpness ("Sharpness", Range(0, 50)) = 1
        _SharpnessRadius ("Sharpness Radius", Range(0.001, 1)) = 0.005
        
        // Настройки шума
        _NoiseIntensity ("Noise Intensity", Range(0, 10)) = 0.1
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 10
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
        _NoiseAffectsRoughness ("Noise Affects Roughness", Range(0, 1)) = 0
        
        // Настройки PBR (блики и шероховатость)
        [Header(PBR Settings)]
        _Metallic ("Metallic", Range(0, 10)) = 0
        [Toggle(_USE_SMOOTHNESS)]_UseSmoothness("Use Smoothness (instead of Roughness)", Float) = 1
        _Smoothness ("Smoothness", Range(0, 10)) = 0.5
        _Roughness ("Roughness", Range(0, 10)) = 0.5
        _SpecularColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        
        // Дополнительные настройки
        [Toggle]_UseRandomNoise ("Use Random Noise", Float) = 1
        _NoiseTexture ("Noise Texture", 2D) = "gray" {}
        [Toggle]_UseRoughnessMap ("Use Roughness Map", Float) = 0
        [Toggle]_UseSharpness ("Enable Sharpness", Float) = 1
        
        // Эмиссия (свечение)
        [Header(Emission)]
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma shader_feature _USE_SMOOTHNESS
        
        sampler2D _MainTex;
        sampler2D _NoiseTexture;
        sampler2D _RoughnessMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };
        
        fixed4 _Color;
        float _Contrast;
        float _Brightness;
        float _Saturation;
        float _Sharpness;
        float _SharpnessRadius;
        float _NoiseIntensity;
        float _NoiseScale;
        float _NoiseSpeed;
        float _NoiseAffectsRoughness;
        float _UseRandomNoise;
        float _UseRoughnessMap;
        float _UseSharpness;
        float _Metallic;
        float _Smoothness;
        float _Roughness;
        float _EmissionIntensity;
        fixed4 _SpecularColor;
        fixed4 _EmissionColor;
        
        // Функция генерации случайного шума
        float random(float2 st)
        {
            return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
        }
        
        // Функция для шума Перлина (упрощенная)
        float noise(float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            
            float a = random(i);
            float b = random(i + float2(1.0, 0.0));
            float c = random(i + float2(0.0, 1.0));
            float d = random(i + float2(1.0, 1.0));
            
            float2 u = f * f * (3.0 - 2.0 * f);
            
            return lerp(a, b, u.x) +
                   (c - a) * u.y * (1.0 - u.x) +
                   (d - b) * u.x * u.y;
        }
        
        // Функция коррекции контраста
        float4 ApplyContrast(float4 color, float contrast, float brightness, float saturation)
        {
            // Применяем яркость
            color.rgb += brightness;
            
            // Применяем контраст
            color.rgb = (color.rgb - 0.5) * contrast + 0.5;
            
            // Применяем насыщенность
            float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
            color.rgb = lerp(float3(luminance, luminance, luminance), color.rgb, saturation);
            
            return saturate(color);
        }
        
        // Функция повышения резкости (фильтр unsharp mask)
        float4 ApplySharpness(sampler2D tex, float2 uv, float sharpness, float radius)
        {
            if (sharpness <= 0) return tex2D(tex, uv);
            
            // Размер текселя для расчета смещений
            float2 texelSize = float2(ddx(uv.x), ddy(uv.y)) * radius;
            
            // Собираем соседние пиксели
            float4 center = tex2D(tex, uv);
            float4 up = tex2D(tex, uv + float2(0, texelSize.y));
            float4 down = tex2D(tex, uv - float2(0, texelSize.y));
            float4 left = tex2D(tex, uv - float2(texelSize.x, 0));
            float4 right = tex2D(tex, uv + float2(texelSize.x, 0));
            
            // Дополнительные пиксели для более качественного размытия
            float4 upLeft = tex2D(tex, uv + float2(-texelSize.x, texelSize.y));
            float4 upRight = tex2D(tex, uv + float2(texelSize.x, texelSize.y));
            float4 downLeft = tex2D(tex, uv + float2(-texelSize.x, -texelSize.y));
            float4 downRight = tex2D(tex, uv + float2(texelSize.x, -texelSize.y));
            
            // Вычисляем размытую версию (среднее значение)
            float4 blurred = (center + up + down + left + right + 
                             upLeft + upRight + downLeft + downRight) / 9.0;
            
            // Применяем unsharp mask формулу: original + (original - blurred) * sharpness
            float4 sharpened = center + (center - blurred) * sharpness;
            
            return saturate(sharpened);
        }
        
        // Альтернативная функция резкости через фильтр Лапласа
        float4 ApplyLaplacianSharpness(sampler2D tex, float2 uv, float sharpness, float radius)
        {
            if (sharpness <= 0) return tex2D(tex, uv);
            
            float2 texelSize = float2(ddx(uv.x), ddy(uv.y)) * radius;
            
            // Матрица Лапласа
            float4 center = tex2D(tex, uv) * 8.0;
            float4 up = tex2D(tex, uv + float2(0, texelSize.y));
            float4 down = tex2D(tex, uv - float2(0, texelSize.y));
            float4 left = tex2D(tex, uv - float2(texelSize.x, 0));
            float4 right = tex2D(tex, uv + float2(texelSize.x, 0));
            float4 upLeft = tex2D(tex, uv + float2(-texelSize.x, texelSize.y));
            float4 upRight = tex2D(tex, uv + float2(texelSize.x, texelSize.y));
            float4 downLeft = tex2D(tex, uv + float2(-texelSize.x, -texelSize.y));
            float4 downRight = tex2D(tex, uv + float2(texelSize.x, -texelSize.y));
            
            // Вычисляем лапласиан
            float4 laplacian = center - (up + down + left + right + upLeft + upRight + downLeft + downRight);
            
            // Применяем резкость
            float4 sharpened = tex2D(tex, uv) + laplacian * sharpness * 0.5;
            
            return saturate(sharpened);
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Основной цвет текстуры
            float4 color;
            
            if (_UseSharpness > 0.5 && _Sharpness > 0)
            {
                // Применяем резкость к текстуре
                color = ApplySharpness(_MainTex, IN.uv_MainTex, _Sharpness, _SharpnessRadius) * _Color;
            }
            else
            {
                // Без резкости
                color = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            }
            
            // Применяем контраст, яркость и насыщенность
            color = ApplyContrast(color, _Contrast, _Brightness, _Saturation);
            
            // Генерация шума
            float noiseValue;
            
            if (_UseRandomNoise > 0.5)
            {
                // Используем процедурный шум
                float2 noiseUV = IN.worldPos.xz * _NoiseScale;
                noiseUV += _Time.y * _NoiseSpeed;
                noiseValue = noise(noiseUV);
            }
            else
            {
                // Используем текстуру шума
                float2 noiseUV = IN.uv_MainTex * _NoiseScale;
                noiseUV += _Time.y * _NoiseSpeed;
                noiseValue = tex2D(_NoiseTexture, noiseUV).r;
            }
            
            // Применяем шум к цвету
            noiseValue = (noiseValue - 0.5) * 2.0; // Преобразуем в диапазон [-1, 1]
            color.rgb += noiseValue * _NoiseIntensity;
            
            // Настройки PBR
            float smoothness = _Smoothness;
            float roughness = _Roughness;
            
            // Преобразуем между smoothness и roughness в зависимости от выбора
            #ifdef _USE_SMOOTHNESS
                // Используем Smoothness (по умолчанию)
                roughness = 1.0 - smoothness;
            #else
                // Используем Roughness
                smoothness = 1.0 - roughness;
            #endif
            
            // Если используем карту шероховатости
            if (_UseRoughnessMap > 0.5)
            {
                float roughnessFromMap = tex2D(_RoughnessMap, IN.uv_MainTex).r;
                roughness = roughnessFromMap;
                smoothness = 1.0 - roughness;
            }
            
            // Применяем шум к шероховатости, если нужно
            if (_NoiseAffectsRoughness > 0)
            {
                float noiseEffect = lerp(0.5, noiseValue * 0.5 + 0.5, _NoiseAffectsRoughness);
                roughness = lerp(roughness, roughness * noiseEffect, _NoiseAffectsRoughness);
                smoothness = 1.0 - roughness;
            }
            
            // Устанавливаем результат
            o.Albedo = color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = smoothness;
            o.Alpha = color.a;
            o.Emission = _EmissionColor * _EmissionIntensity;
        }
        ENDCG
    }
    
    FallBack "Standard"
    CustomEditor "ContrastNoiseShaderGUI"
}