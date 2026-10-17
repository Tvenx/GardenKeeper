Shader "Custom/HorrorInteractableGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.3,0.5,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,1)) = 0.1
        _FresnelPower ("Fresnel Power", Range(1,5)) = 2
        _PulseSpeed ("Pulse Speed", Range(0,5)) = 1
        _NoiseScale ("Noise Scale", Range(0,10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;

        float4 _GlowColor;
        float _GlowIntensity;
        float _FresnelPower;
        float _PulseSpeed;
        float _NoiseScale;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        float hash(float3 p)
        {
            return frac(sin(dot(p, float3(12.9898,78.233,45.164))) * 43758.5453);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;

            // Fresnel
            float fresnel = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), _FresnelPower);

            // Пульс
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;

            // Лёгкий шум
            float noise = hash(IN.worldPos * _NoiseScale + _Time.y);

            float glow = fresnel * (_GlowIntensity + pulse * 0.05 + noise * 0.05);

            o.Emission = _GlowColor.rgb * glow;
        }
        ENDCG
    }
    FallBack "Standard"
}