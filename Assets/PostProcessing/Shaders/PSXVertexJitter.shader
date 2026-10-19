Shader "Custom/PSXVertexJitter_Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        _JitterStrength ("Jitter Strength", Range(0,0.01)) = 0.002
        _JitterSpeed ("Jitter Speed", Float) = 20
        _Snap ("Vertex Snap", Float) = 200
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
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
            fixed4 _Color;

            float _JitterStrength;
            float _JitterSpeed;
            float _Snap;

            v2f vert (appdata v)
            {
                v2f o;

                // Оптимизированное получение времени для эффекта "дрожания"
                float fps = 12;
                float t = floor(_Time.y * fps) / fps;

                float noise = sin(t * _JitterSpeed + dot(v.vertex.xy, float2(12.9898, 78.233)));

                // Применяем смещение
                v.vertex.xyz += noise * _JitterStrength;

                // Округление позиции для эффекта PS1
                v.vertex.xyz = floor(v.vertex.xyz * _Snap) / _Snap;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                tex *= _Color;
                return tex;
            }
            ENDCG
        }
    }
}