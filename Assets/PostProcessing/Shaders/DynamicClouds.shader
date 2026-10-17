Shader "Custom/DynamicClouds"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color ("Cloud Color", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 0.05
        _Intensity ("Intensity", Float) = 1.5
        _Scale ("Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _Color;
            float _Speed;
            float _Intensity;
            float _Scale;

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

            v2f vert(appdata v)
            {
                v2f o;

                // Смещаем UV для движения облаков
                o.uv = v.uv * _Scale + float2(_Time.y * _Speed, _Time.y * _Speed * 0.3);

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Получаем значение шума
                float n = tex2D(_MainTex, i.uv).r;

                // Плавная градация альфа
                float alpha = smoothstep(0.3, 0.7, n) * _Intensity;

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}