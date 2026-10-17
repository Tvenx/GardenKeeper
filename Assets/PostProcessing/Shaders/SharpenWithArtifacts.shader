Shader "Custom/SharpenWithArtifacts"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Sharpness ("Sharpness", Range(0, 50)) = 1
        _PixelSize ("Pixel Size", Range(1, 512)) = 1
        _Posterize ("Posterize Levels", Range(2, 640)) = 64
        _NoiseAmount ("Noise Amount", Range(0, 10)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _Sharpness;
            float _PixelSize;
            float _Posterize;
            float _NoiseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---------- Pixelation ----------
                float2 pixelUV = i.uv;
                if (_PixelSize > 1)
                {
                    float2 size = _PixelSize * _MainTex_TexelSize.xy;
                    pixelUV = floor(pixelUV / size) * size;
                }

                // ---------- Sharpen (Laplacian) ----------
                float2 t = _MainTex_TexelSize.xy;

                float3 c = tex2D(_MainTex, pixelUV).rgb;
                float3 n = tex2D(_MainTex, pixelUV + float2(0,  t.y)).rgb;
                float3 s = tex2D(_MainTex, pixelUV - float2(0,  t.y)).rgb;
                float3 e = tex2D(_MainTex, pixelUV + float2( t.x, 0)).rgb;
                float3 w = tex2D(_MainTex, pixelUV - float2( t.x, 0)).rgb;

                float3 edge = (n + s + e + w - 4 * c);
                float3 sharpened = c - edge * _Sharpness;

                // ---------- Posterization ----------
                sharpened = floor(sharpened * _Posterize) / _Posterize;

                // ---------- Noise ----------
                float noise = rand(i.uv + _Time.y) * 2 - 1;
                sharpened += noise * _NoiseAmount;

                return fixed4(saturate(sharpened), 1);
            }
            ENDCG
        }
    }
}
