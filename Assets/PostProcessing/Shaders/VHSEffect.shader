Shader "Custom/VHSEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        _ScanlineSize ("Scanline Size", Range(1, 10)) = 3
        _ScanlineSpeed ("Scanline Speed", Range(-5, 5)) = 0.5
        
        _NoiseIntensity ("Noise Intensity", Range(0, 0.5)) = 0.1
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1
        _NoiseSpeed ("Noise Speed", Range(-10, 10)) = 2
        _NoiseThreshold ("Noise Threshold", Range(0, 1)) = 0.95
        
        _JitterAmount ("Jitter Amount", Range(0, 0.05)) = 0.02
        _JitterSpeed ("Jitter Speed", Range(0, 10)) = 3
        
        _RGBSplit ("RGB Split", Range(0, 0.05)) = 0.01
        _RGBSplitSpeed ("RGB Split Speed", Range(0, 10)) = 2
        
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.5
        _VignetteRadius ("Vignette Radius", Range(0, 1)) = 0.8
        
        _SyncOffset ("Sync Offset", Range(-0.1, 0.1)) = 0
        _SyncSpeed ("Sync Speed", Range(0, 10)) = 3
        _SyncThreshold ("Sync Threshold", Range(0, 1)) = 0.95
        
        _BrightnessSpeed ("Brightness Speed", Range(0, 5)) = 1.3
        _BrightnessAmount ("Brightness Amount", Range(0, 0.2)) = 0.1
        
        _GlobalTimeScale ("Global Time Scale", Range(0, 5)) = 1
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _NoiseTexture;
            
            float _ScanlineIntensity;
            float _ScanlineSize;
            float _ScanlineSpeed;
            
            float _NoiseIntensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseThreshold;
            
            float _JitterAmount;
            float _JitterSpeed;
            
            float _RGBSplit;
            float _RGBSplitSpeed;
            
            float _VignetteIntensity;
            float _VignetteRadius;
            
            float _SyncOffset;
            float _SyncSpeed;
            float _SyncThreshold;
            
            float _BrightnessSpeed;
            float _BrightnessAmount;
            
            float _GlobalTimeScale;

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float globalTime = _Time.y * _GlobalTimeScale;
                
                float scanlineTime = globalTime * _ScanlineSpeed;
                float noiseTime = globalTime * _NoiseSpeed;
                float jitterTime = globalTime * _JitterSpeed;
                float splitTime = globalTime * _RGBSplitSpeed;
                float syncTime = globalTime * _SyncSpeed;
                float brightnessTime = globalTime * _BrightnessSpeed;

                float jitterX = (random(float2(floor(jitterTime * 10), 0.0)) - 0.5) * _JitterAmount;
                float jitterY = (random(float2(floor(jitterTime * 10), 1.0)) - 0.5) * _JitterAmount * 0.5;

                float syncWave = sin(syncTime * 3.0) * 0.5 + 0.5;
                float syncOffsetY = _SyncOffset * step(_SyncThreshold, syncWave) * 
                                    (random(float2(floor(syncTime * 2.0), 2.0)) - 0.5) * 0.1;

                float2 uv = i.uv + float2(jitterX, jitterY + syncOffsetY);
                uv = clamp(uv, 0.0, 1.0);

                float split = _RGBSplit * (0.5 + 0.5 * sin(splitTime * 2.0));

                float r = tex2D(_MainTex, uv + float2(split, 0.0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(split, 0.0)).b;

                fixed4 col = fixed4(r, g, b, 1.0);

                float scanline = sin(uv.y * _ScreenParams.y / _ScanlineSize * 3.14159 + scanlineTime * 2.0);
                scanline = abs(scanline);
                scanline = lerp(1.0, scanline, _ScanlineIntensity);
                col.rgb *= scanline;

                float2 noiseUV = uv * _NoiseScale + float2(noiseTime * 0.5, noiseTime * 0.3);
                float noiseTex = tex2D(_NoiseTexture, noiseUV).r;
                
                float dynamicThreshold = _NoiseThreshold + 0.02 * sin(noiseTime * 1.7);
                float noisePattern = step(dynamicThreshold, noiseTex) * _NoiseIntensity;
                
                float bandNoise = tex2D(_NoiseTexture, float2(noiseTime * 0.1, uv.y * 2.0)).r;
                float band = step(0.98, bandNoise) * _NoiseIntensity * 0.5;
                
                col.rgb += noisePattern + band;

                float2 center = uv - 0.5;
                float dist = length(center);
                float vignette = 1.0 - smoothstep(_VignetteRadius, 1.0, dist) * _VignetteIntensity;
                col.rgb *= vignette;

                float brightnessShift = 1.0 - _BrightnessAmount + _BrightnessAmount * (0.5 + 0.5 * sin(brightnessTime * 1.3));
                col.rgb *= brightnessShift;

                col.r *= 1.02;
                col.b *= 0.98;

                return col;
            }
            ENDCG
        }
    }
}