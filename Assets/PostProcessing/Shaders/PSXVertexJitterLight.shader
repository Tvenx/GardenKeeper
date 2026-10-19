Shader "Custom/PSXVertexJitterLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _JitterStrength ("Jitter Strength", Range(0,0.01)) = 0.002
        _JitterSpeed ("Jitter Speed", Float) = 20
        _Snap ("Vertex Snap", Float) = 200
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        float _JitterStrength;
        float _JitterSpeed;
        float _Snap;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert (inout appdata_full v)
        {
            float fps = 12;
            float t = floor(_Time.y * fps) / fps;

            float noise = sin(t * _JitterSpeed + dot(v.vertex.xy, float2(12.9898,78.233)));

            v.vertex.xyz += noise * _JitterStrength;

            v.vertex.xyz = floor(v.vertex.xyz * _Snap) / _Snap;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            
            tex *= _Color;

            o.Albedo = tex.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}