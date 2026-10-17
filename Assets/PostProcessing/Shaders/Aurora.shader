Shader "Custom/AuroraMoving"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color1 ("Color 1", Color) = (0,2,0.5,1) // зелёный HDR
        _Color2 ("Color 2", Color) = (1,0,2,1)   // фиолетовый HDR
        _Speed ("Speed", Float) = 0.3
        _Intensity ("Intensity", Float) = 3.0
        _WaveStrength ("Wave Strength", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _Color1;
            float4 _Color2;
            float _Speed;
            float _Intensity;
            float _WaveStrength;

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

                // Волны по Y
                float wave1 = sin(v.vertex.x * 2 + _Time.y * 2 * _Speed) * _WaveStrength;
                float wave2 = cos(v.vertex.x * 1.5 + _Time.y * 1.5 * _Speed) * (_WaveStrength * 0.6);

                float3 pos = v.vertex.xyz;
                pos.y += wave1 + wave2;

                o.vertex = UnityObjectToClipPos(float4(pos,1));

                // Движение UV для текстуры
                o.uv = v.uv + float2(_Time.y * _Speed, _Time.y * _Speed * 0.3);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Считываем шум
                float noise = tex2D(_MainTex, i.uv).r;

                // Градиент по вертикали
                float gradient = smoothstep(0.0, 1.0, i.uv.y);

                // Смена цвета
                float colorShift = sin(_Time.y * 0.5) * 0.5 + 0.5;
                float4 color = lerp(_Color1, _Color2, colorShift + noise * 0.3);

                // Итоговый альфа + интенсивность
                float alpha = noise * gradient;
                return color * alpha * _Intensity;
            }
            ENDCG
        }
    }
}